using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using POE_RaceDay_API.DTOs;
using POE_RaceDay_API.Model;

namespace POE_RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;

            _connectionString =
                configuration.GetConnectionString("RaceDayConnection")
                ?? throw new InvalidOperationException(
                    "RaceDayConnection is missing from configuration.");
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterUserDto dto)
        {
            string fullName = dto.FullName.Trim();
            string email = dto.Email.Trim().ToLowerInvariant();
            string role = dto.Role.Trim();

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new
                {
                    message = "Name, email and password are required."
                });
            }

            // Hash the password. Never store the plain-text password.
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(
                dto.Password,
                workFactor: 12);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string sql = @"
                    INSERT INTO dbo.Users
                        (FullName, Email, PasswordHash, Role, PhoneNumber)
                    OUTPUT INSERTED.UserID, INSERTED.CreatedAt
                    VALUES
                        (@FullName, @Email, @PasswordHash, @Role, @PhoneNumber);";

                using var command = new SqlCommand(sql, connection);

                command.Parameters.Add(
                    "@FullName", SqlDbType.NVarChar, 150).Value = fullName;

                command.Parameters.Add(
                    "@Email", SqlDbType.NVarChar, 254).Value = email;

                command.Parameters.Add(
                    "@PasswordHash", SqlDbType.NVarChar, 255)
                    .Value = passwordHash;

                command.Parameters.Add(
                    "@Role", SqlDbType.NVarChar, 20).Value = role;

                command.Parameters.Add(
                    "@PhoneNumber", SqlDbType.NVarChar, 20).Value =
                    string.IsNullOrWhiteSpace(dto.PhoneNumber)
                        ? DBNull.Value
                        : dto.PhoneNumber.Trim();

                int userId;
                DateTime createdAt;

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return StatusCode(500, new
                        {
                            message = "User registration failed."
                        });
                    }

                    userId = reader.GetInt32(0);
                    createdAt = reader.GetDateTime(1);
                }

                return StatusCode(201, new
                {
                    message = "Registration successful.",
                    user = new
                    {
                        UserID = userId,
                        FullName = fullName,
                        Email = email,
                        Role = role,
                        PhoneNumber = dto.PhoneNumber,
                        CreatedAt = createdAt
                    }
                });
            }
            catch (SqlException ex) when (
                ex.Number == 2601 || ex.Number == 2627)
            {
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            string email = dto.Email.Trim().ToLowerInvariant();

            User? user = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                const string sql = @"
                    SELECT UserID, FullName, Email, PasswordHash,
                           Role, PhoneNumber, CreatedAt
                    FROM dbo.Users
                    WHERE Email = @Email;";

                using var command = new SqlCommand(sql, connection);

                command.Parameters.Add(
                    "@Email", SqlDbType.NVarChar, 254).Value = email;

                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    user = new User
                    {
                        UserID = reader.GetInt32(0),
                        FullName = reader.GetString(1),
                        Email = reader.GetString(2),
                        PasswordHash = reader.GetString(3),
                        Role = reader.GetString(4),
                        PhoneNumber = reader.IsDBNull(5)
                            ? null
                            : reader.GetString(5),
                        CreatedAt = reader.GetDateTime(6)
                    };
                }
            }

            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            string token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login successful.",
                token,
                tokenType = "Bearer",
                expiresInMinutes = GetJwtExpiryMinutes(),
                user = new
                {
                    user.UserID,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.PhoneNumber,
                    user.CreatedAt
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            string key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT key is missing from configuration.");

            string issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT issuer is missing from configuration.");

            string audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT audience is missing from configuration.");

            if (Encoding.UTF8.GetByteCount(key) < 32)
            {
                throw new InvalidOperationException(
                    "The JWT key must be at least 32 bytes for HS256.");
            }

            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(GetJwtExpiryMinutes()),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private int GetJwtExpiryMinutes()
        {
            int minutes = _configuration.GetValue<int?>(
                "Jwt:ExpiryMinutes") ?? 60;

            if (minutes <= 0)
            {
                throw new InvalidOperationException(
                    "JWT expiry must be greater than zero.");
            }

            return minutes;
        }
    }
}