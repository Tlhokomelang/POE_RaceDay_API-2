using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace POE_RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly string _connectionString;

        public ProfileController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("RaceDayConnection")
                ?? throw new InvalidOperationException(
                    "RaceDayConnection is missing from configuration.");
        }

        // GET: api/profile
        // Returns the profile of the currently logged-in user.
        [HttpGet]
        public IActionResult GetProfile()
        {
            // Get the user ID from the JWT token.
            string? userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user token."
                });
            }

            using var connection =
                new SqlConnection(_connectionString);

            connection.Open();

            const string sql = @"
                SELECT UserID, FullName, Email, Role,
                       PhoneNumber, CreatedAt
                FROM dbo.Users
                WHERE UserID = @UserID;";

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@UserID", SqlDbType.Int).Value = userId;

            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return NotFound(new
                {
                    message = "User profile not found."
                });
            }

            var profile = new
            {
                UserID = reader.GetInt32(
                    reader.GetOrdinal("UserID")),

                FullName = reader.GetString(
                    reader.GetOrdinal("FullName")),

                Email = reader.GetString(
                    reader.GetOrdinal("Email")),

                Role = reader.GetString(
                    reader.GetOrdinal("Role")),

                PhoneNumber = reader.IsDBNull(
                    reader.GetOrdinal("PhoneNumber"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal("PhoneNumber")),

                CreatedAt = reader.GetDateTime(
                    reader.GetOrdinal("CreatedAt"))
            };

            return Ok(new
            {
                message = "Profile retrieved successfully.",
                profile
            });
        }
    }
}