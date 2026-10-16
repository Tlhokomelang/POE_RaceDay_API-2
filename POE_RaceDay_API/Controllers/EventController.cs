
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using POE_RaceDay_API.DTOs;

namespace POE_RaceDay_API.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EventController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("RaceDayConnection"));
        }

       

        // GET: api/events
        // View upcoming events
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvents()
        {
            var events = new List<object>();

            using var connection = GetConnection();
            await connection.OpenAsync();

            string sql = @"
                SELECT EventID, EventName, Description,
                       EventDate, Location, OrganiserID
                FROM dbo.Events
                WHERE EventDate >= @Now
                ORDER BY EventDate ASC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Now", SqlDbType.DateTime2).Value =
                DateTime.UtcNow;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                events.Add(new
                {
                    EventID = reader.GetInt32(0),
                    EventName = reader.GetString(1),
                    Description = reader.IsDBNull(2)
                        ? null : reader.GetString(2),
                    EventDate = reader.GetDateTime(3),
                    Location = reader.GetString(4),
                    OrganiserID = reader.GetInt32(5)
                });
            }

            return Ok(events);
        }

        // POST: api/events
        // Organiser only
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateEvent(
            [FromBody] EventRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EventName) ||
                string.IsNullOrWhiteSpace(request.Location))
            {
                return BadRequest(new
                {
                    message = "Event name and location are required."
                });
            }

            if (request.EventDate <= DateTime.UtcNow)
            {
                return BadRequest(new
                {
                    message = "Event date must be in the future."
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

            string sql = @"
                INSERT INTO dbo.Events
                    (EventName, Description, EventDate,
                     Location, OrganiserID)
                OUTPUT INSERTED.EventID
                VALUES
                    (@EventName, @Description, @EventDate,
                     @Location, @OrganiserID)";

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 150)
                .Value = request.EventName.Trim();

            command.Parameters.Add("@Description", SqlDbType.NVarChar, 1000)
                .Value = string.IsNullOrWhiteSpace(request.Description)
                    ? DBNull.Value
                    : request.Description.Trim();

            command.Parameters.Add("@EventDate", SqlDbType.DateTime2)
                .Value = request.EventDate;

            command.Parameters.Add("@Location", SqlDbType.NVarChar, 250)
                .Value = request.Location.Trim();

            command.Parameters.Add("@OrganiserID", SqlDbType.Int)
                .Value = organiserId;

            int eventId = Convert.ToInt32(
                await command.ExecuteScalarAsync());

            return StatusCode(201, new
            {
                message = "Event created successfully.",
                eventId
            });
        }

        // PUT: api/events/{id}
        // Organiser only; only the owner may update
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromBody] EventRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EventName) ||
                string.IsNullOrWhiteSpace(request.Location))
            {
                return BadRequest(new
                {
                    message = "Event name and location are required."
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

            string sql = @"
                UPDATE dbo.Events
                SET EventName = @EventName,
                    Description = @Description,
                    EventDate = @EventDate,
                    Location = @Location
                WHERE EventID = @EventID
                  AND OrganiserID = @OrganiserID";

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 150)
                .Value = request.EventName.Trim();

            command.Parameters.Add("@Description", SqlDbType.NVarChar, 1000)
                .Value = string.IsNullOrWhiteSpace(request.Description)
                    ? DBNull.Value
                    : request.Description.Trim();

            command.Parameters.Add("@EventDate", SqlDbType.DateTime2)
                .Value = request.EventDate;

            command.Parameters.Add("@Location", SqlDbType.NVarChar, 250)
                .Value = request.Location.Trim();

            command.Parameters.Add("@EventID", SqlDbType.Int).Value = id;
            command.Parameters.Add("@OrganiserID", SqlDbType.Int)
                .Value = organiserId;

            int rows = await command.ExecuteNonQueryAsync();

            if (rows == 0)
            {
                return NotFound(new
                {
                    message = "Event not found or you do not own this event."
                });
            }

            return Ok(new
            {
                message = "Event updated successfully."
            });
        }

        // DELETE: api/events/{id}
        // Organiser only; only the owner may delete
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
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

            string sql = @"
                DELETE FROM dbo.Events
                WHERE EventID = @EventID
                  AND OrganiserID = @OrganiserID";

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@EventID", SqlDbType.Int).Value = id;
            command.Parameters.Add("@OrganiserID", SqlDbType.Int)
                .Value = organiserId;

            int rows = await command.ExecuteNonQueryAsync();

            if (rows == 0)
            {
                return NotFound(new
                {
                    message = "Event not found or you do not own this event."
                });
            }

            return Ok(new
            {
                message = "Event deleted successfully."
            });
        }
    }
}