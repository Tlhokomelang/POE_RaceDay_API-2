using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using POE_RaceDay_API.DTOs;
namespace POE_RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrolmentController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EnrolmentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("RaceDayConnection"));
        }

       

        // GET: api/enrolment
        //  View enrolments
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEnrolments()
        {
            var enrolments = new List<object>();

            using var connection = GetConnection();
            await connection.OpenAsync();

            string sql = @"
                SELECT
                    en.EnrolmentID,
                    en.ParticipantID,
                    u.FullName,
                    en.CategoryID,
                    c.CategoryName,
                    c.Distance,
                    c.EventID,
                    e.EventName,
                    en.EnrolmentDate
                FROM dbo.Enrolments en
                INNER JOIN dbo.Users u
                    ON en.ParticipantID = u.UserID
                INNER JOIN dbo.Categories c
                    ON en.CategoryID = c.CategoryID
                INNER JOIN dbo.Events e
                    ON c.EventID = e.EventID
                ORDER BY en.EnrolmentDate DESC";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                enrolments.Add(new
                {
                    EnrolmentID = reader.GetInt32(0),
                    ParticipantID = reader.GetInt32(1),
                    ParticipantName = reader.GetString(2),
                    CategoryID = reader.GetInt32(3),
                    CategoryName = reader.GetString(4),
                    Distance = reader.GetDecimal(5),
                    EventID = reader.GetInt32(6),
                    EventName = reader.GetString(7),
                    EnrolmentDate = reader.GetDateTime(8)
                });
            }

            return Ok(enrolments);
        }

        // POST: api/enrolment
        // Participant: Register for a category
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CreateEnrolment(
            [FromBody] EnrolmentRequest request)
        {
            if (request.CategoryID <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid CategoryID is required."
                });
            }

            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userId, out int participantId))
            {
                return Unauthorized(new
                {
                    message = "A valid participant ID was not found in the token."
                });
            }

            using var connection = GetConnection();
            await connection.OpenAsync();

            // Check that the category exists and its event is upcoming.
            string checkSql = @"
                SELECT COUNT(1)
                FROM dbo.Categories c
                INNER JOIN dbo.Events e
                    ON c.EventID = e.EventID
                WHERE c.CategoryID = @CategoryID
                  AND e.EventDate > @Now";

            using var checkCommand = new SqlCommand(
                checkSql, connection);

            checkCommand.Parameters.Add(
                "@CategoryID", SqlDbType.Int).Value =
                request.CategoryID;

            checkCommand.Parameters.Add(
                "@Now", SqlDbType.DateTime2).Value =
                DateTime.UtcNow;

            int categoryExists = Convert.ToInt32(
                await checkCommand.ExecuteScalarAsync());

            if (categoryExists == 0)
            {
                return BadRequest(new
                {
                    message = "Category not found or its event is no longer upcoming."
                });
            }

            // Prevent duplicate enrolments.
            string duplicateSql = @"
                SELECT COUNT(1)
                FROM dbo.Enrolments
                WHERE ParticipantID = @ParticipantID
                  AND CategoryID = @CategoryID";

            using var duplicateCommand = new SqlCommand(
                duplicateSql, connection);

            duplicateCommand.Parameters.Add(
                "@ParticipantID", SqlDbType.Int).Value =
                participantId;

            duplicateCommand.Parameters.Add(
                "@CategoryID", SqlDbType.Int).Value =
                request.CategoryID;

            int alreadyEnrolled = Convert.ToInt32(
                await duplicateCommand.ExecuteScalarAsync());

            if (alreadyEnrolled > 0)
            {
                return Conflict(new
                {
                    message = "You are already enrolled in this category."
                });
            }

            // Create enrolment.
            string insertSql = @"
                INSERT INTO dbo.Enrolments
                    (ParticipantID, CategoryID)
                OUTPUT INSERTED.EnrolmentID
                VALUES
                    (@ParticipantID, @CategoryID)";

            using var insertCommand = new SqlCommand(
                insertSql, connection);

            insertCommand.Parameters.Add(
                "@ParticipantID", SqlDbType.Int).Value =
                participantId;

            insertCommand.Parameters.Add(
                "@CategoryID", SqlDbType.Int).Value =
                request.CategoryID;

            int enrolmentId = Convert.ToInt32(
                await insertCommand.ExecuteScalarAsync());

            return StatusCode(201, new
            {
                message = "Enrolment successful.",
                enrolmentID = enrolmentId,
                participantID = participantId,
                categoryID = request.CategoryID
            });
        }
    }
}