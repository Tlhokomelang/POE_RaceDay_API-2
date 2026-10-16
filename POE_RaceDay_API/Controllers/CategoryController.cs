
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using POE_RaceDay_API.DTOs;
namespace POE_RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CategoryController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("RaceDayConnection"));
        }

      

        // GET: api/category
        // View all categories
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            var categories = new List<object>();

            using var connection = GetConnection();
            await connection.OpenAsync();

            string sql = @"
                SELECT
                    c.CategoryID,
                    c.CategoryName,
                    c.Distance,
                    c.EntryFee,
                    c.EventID,
                    e.EventName,
                    e.EventDate,
                    e.Location
                FROM dbo.Categories c
                INNER JOIN dbo.Events e
                    ON c.EventID = e.EventID
                ORDER BY e.EventDate ASC, c.Distance ASC";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categories.Add(new
                {
                    CategoryID = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    Distance = reader.GetDecimal(2),
                    EntryFee = reader.GetDecimal(3),
                    EventID = reader.GetInt32(4),
                    EventName = reader.GetString(5),
                    EventDate = reader.GetDateTime(6),
                    Location = reader.GetString(7)
                });
            }

            return Ok(categories);
        }

        // POST: api/category
        // Organiser only: Add a category to an event
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateCategory(
            [FromBody] CategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CategoryName))
            {
                return BadRequest(new
                {
                    message = "Category name is required."
                });
            }

            if (request.Distance <= 0)
            {
                return BadRequest(new
                {
                    message = "Distance must be greater than zero."
                });
            }

            if (request.EntryFee < 0)
            {
                return BadRequest(new
                {
                    message = "Entry fee cannot be negative."
                });
            }

            if (request.EventID <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid EventID is required."
                });
            }

            using var connection = GetConnection();
            await connection.OpenAsync();

            // Verify that the event exists and belongs to this organiser.
            string? userId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(userId, out int organiserId))
            {
                return Unauthorized(new
                {
                    message = "A valid organiser ID was not found in the token."
                });
            }

            string checkSql = @"
                SELECT COUNT(1)
                FROM dbo.Events
                WHERE EventID = @EventID
                  AND OrganiserID = @OrganiserID";

            using var checkCommand = new SqlCommand(
                checkSql, connection);

            checkCommand.Parameters.Add(
                "@EventID", SqlDbType.Int).Value = request.EventID;

            checkCommand.Parameters.Add(
                "@OrganiserID", SqlDbType.Int).Value = organiserId;

            int eventExists = Convert.ToInt32(
                await checkCommand.ExecuteScalarAsync());

            if (eventExists == 0)
            {
                return NotFound(new
                {
                    message = "Event not found or you do not own this event."
                });
            }

            string insertSql = @"
                INSERT INTO dbo.Categories
                    (CategoryName, Distance, EntryFee, EventID)
                OUTPUT INSERTED.CategoryID
                VALUES
                    (@CategoryName, @Distance, @EntryFee, @EventID)";

            using var insertCommand = new SqlCommand(
                insertSql, connection);

            insertCommand.Parameters.Add(
                "@CategoryName", SqlDbType.NVarChar, 100).Value =
                request.CategoryName.Trim();

            insertCommand.Parameters.Add(
                "@Distance", SqlDbType.Decimal).Value =
                request.Distance;

            insertCommand.Parameters["@Distance"].Precision = 8;
            insertCommand.Parameters["@Distance"].Scale = 2;

            insertCommand.Parameters.Add(
                "@EntryFee", SqlDbType.Decimal).Value =
                request.EntryFee;

            insertCommand.Parameters["@EntryFee"].Precision = 10;
            insertCommand.Parameters["@EntryFee"].Scale = 2;

            insertCommand.Parameters.Add(
                "@EventID", SqlDbType.Int).Value =
                request.EventID;

            int categoryId = Convert.ToInt32(
                await insertCommand.ExecuteScalarAsync());

            return StatusCode(201, new
            {
                message = "Category created successfully.",
                categoryID = categoryId,
                categoryName = request.CategoryName.Trim(),
                distance = request.Distance,
                entryFee = request.EntryFee,
                eventID = request.EventID
            });
        }
    }
}