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
    public class ResultController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ResultController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("RaceDayConnection"));
        }

       

        // GET: api/Result
        // Public: View all race results
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetResults()
        {
            var results = new List<object>();

            using var connection = GetConnection();
            await connection.OpenAsync();

            string sql = @"
                SELECT
                    r.ResultID,
                    r.EnrolmentID,
                    u.FullName AS ParticipantName,
                    c.CategoryName,
                    c.Distance,
                    e.EventName,
                    r.FinishTime,
                    r.Position,
                    r.RecordedAt
                FROM dbo.Results r
                INNER JOIN dbo.Enrolments en
                    ON r.EnrolmentID = en.EnrolmentID
                INNER JOIN dbo.Users u
                    ON en.ParticipantID = u.UserID
                INNER JOIN dbo.Categories c
                    ON en.CategoryID = c.CategoryID
                INNER JOIN dbo.Events e
                    ON c.EventID = e.EventID
                ORDER BY r.Position ASC";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    ResultID = reader.GetInt32(0),
                    EnrolmentID = reader.GetInt32(1),
                    ParticipantName = reader.GetString(2),
                    CategoryName = reader.GetString(3),
                    Distance = reader.GetDecimal(4),
                    EventName = reader.GetString(5),
                    FinishTime = reader.GetTimeSpan(6).ToString(@"hh\:mm\:ss"),
                    Position = reader.GetInt32(7),
                    RecordedAt = reader.GetDateTime(8)
                });
            }

            return Ok(results);
        }

        // POST: api/Result
        // Organiser only: Record a race result
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateResult(
            [FromBody] ResultRequest request)
        {
            if (request.EnrolmentID <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid EnrolmentID is required."
                });
            }

            if (request.Position <= 0)
            {
                return BadRequest(new
                {
                    message = "Position must be greater than zero."
                });
            }

            if (!TimeSpan.TryParse(
                    request.FinishTime, out TimeSpan finishTime)
                || finishTime < TimeSpan.Zero
                || finishTime >= TimeSpan.FromDays(1))
            {
                return BadRequest(new
                {
                    message = "FinishTime must be a valid time, for example 00:28:35."
                });
            }

            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userId, out int organiserId))
            {
                return Unauthorized(new
                {
                    message = "A valid organiser ID was not found in the token."
                });
            }

            using var connection = GetConnection();
            await connection.OpenAsync();

            // Confirm the enrolment belongs to an event managed
            // by the logged-in organiser.
            string checkSql = @"
                SELECT COUNT(1)
                FROM dbo.Enrolments en
                INNER JOIN dbo.Categories c
                    ON en.CategoryID = c.CategoryID
                INNER JOIN dbo.Events e
                    ON c.EventID = e.EventID
                WHERE en.EnrolmentID = @EnrolmentID
                  AND e.OrganiserID = @OrganiserID";

            using var checkCommand = new SqlCommand(
                checkSql, connection);

            checkCommand.Parameters.Add(
                "@EnrolmentID", SqlDbType.Int).Value =
                request.EnrolmentID;

            checkCommand.Parameters.Add(
                "@OrganiserID", SqlDbType.Int).Value =
                organiserId;

            int enrolmentExists = Convert.ToInt32(
                await checkCommand.ExecuteScalarAsync());

            if (enrolmentExists == 0)
            {
                return NotFound(new
                {
                    message = "Enrolment not found or you do not manage its event."
                });
            }

            // Prevent duplicate results for the same enrolment.
            string duplicateSql = @"
                SELECT COUNT(1)
                FROM dbo.Results
                WHERE EnrolmentID = @EnrolmentID";

            using var duplicateCommand = new SqlCommand(
                duplicateSql, connection);

            duplicateCommand.Parameters.Add(
                "@EnrolmentID", SqlDbType.Int).Value =
                request.EnrolmentID;

            int resultExists = Convert.ToInt32(
                await duplicateCommand.ExecuteScalarAsync());

            if (resultExists > 0)
            {
                return Conflict(new
                {
                    message = "A result has already been recorded for this enrolment."
                });
            }

            string insertSql = @"
                INSERT INTO dbo.Results
                    (EnrolmentID, FinishTime, Position)
                OUTPUT INSERTED.ResultID
                VALUES
                    (@EnrolmentID, @FinishTime, @Position)";

            using var insertCommand = new SqlCommand(
                insertSql, connection);

            insertCommand.Parameters.Add(
                "@EnrolmentID", SqlDbType.Int).Value =
                request.EnrolmentID;

            insertCommand.Parameters.Add(
                "@FinishTime", SqlDbType.Time).Value =
                finishTime;

            insertCommand.Parameters.Add(
                "@Position", SqlDbType.Int).Value =
                request.Position;

            int resultId = Convert.ToInt32(
                await insertCommand.ExecuteScalarAsync());

            return StatusCode(201, new
            {
                message = "Result recorded successfully.",
                resultID = resultId,
                enrolmentID = request.EnrolmentID,
                finishTime = finishTime.ToString(@"hh\:mm\:ss"),
                position = request.Position
            });
        }
    }
}