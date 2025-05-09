using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using apbd_lab08.Models;

namespace apbd_lab08.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public TripsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult GetAllTrips()
    {
        var trips = new List<Trip>();
        var connString = _configuration.GetConnectionString("DefaultConnection");

        using var conn = new SqlConnection(connString);
        using var cmd = new SqlCommand(@"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS Country
            FROM Trip t
            JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
            JOIN Country c ON ct.IdCountry = c.IdCountry", conn);

        conn.Open();
        var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            trips.Add(new Trip
            {
                IdTrip = (int)reader["IdTrip"],
                Name = reader["Name"].ToString(),
                Description = reader["Description"].ToString(),
                DateFrom = (DateTime)reader["DateFrom"],
                DateTo = (DateTime)reader["DateTo"],
                MaxPeople = (int)reader["MaxPeople"],
                Country = new Country
                {
                    Name = reader["Country"].ToString()
                }
            });
        }

        return Ok(trips);
    }
}