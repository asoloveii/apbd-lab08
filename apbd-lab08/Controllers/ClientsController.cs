using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using apbd_lab08.Models;

namespace apbd_lab08.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ClientsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost]
    public IActionResult CreateClient([FromBody] Client client)
    {
        if (string.IsNullOrWhiteSpace(client.FirstName) || string.IsNullOrWhiteSpace(client.Pesel))
            return BadRequest("Missing required fields.");

        var connString = _configuration.GetConnectionString("DefaultConnection");

        using var conn = new SqlConnection(connString);
        using var cmd = new SqlCommand(@"
            INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
            OUTPUT INSERTED.IdClient
            VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)", conn);

        cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
        cmd.Parameters.AddWithValue("@LastName", client.LastName);
        cmd.Parameters.AddWithValue("@Email", client.Email);
        cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
        cmd.Parameters.AddWithValue("@Pesel", client.Pesel);

        conn.Open();
        var id = (int)cmd.ExecuteScalar();

        return Created($"/api/clients/{id}", new { Id = id });
    }

    [HttpGet("{id}/trips")]
    public IActionResult GetClientTrips(int id)
    {
        var connString = _configuration.GetConnectionString("DefaultConnection");

        using var conn = new SqlConnection(connString);
        using var cmd = new SqlCommand(@"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, ct.PaymentDate
            FROM Client_Trip ct
            JOIN Trip t ON t.IdTrip = ct.IdTrip
            WHERE ct.IdClient = @IdClient", conn);

        cmd.Parameters.AddWithValue("@IdClient", id);
        conn.Open();
        var reader = cmd.ExecuteReader();

        if (!reader.HasRows) return NotFound("Client has no trips or does not exist.");

        var result = new List<object>();
        while (reader.Read())
        {
            result.Add(new
            {
                TripId = reader["IdTrip"],
                Name = reader["Name"],
                Description = reader["Description"],
                DateFrom = reader["DateFrom"],
                DateTo = reader["DateTo"],
                MaxPeople = reader["MaxPeople"],
                RegisteredAt = reader["RegisteredAt"],
                PaymentDate = reader["PaymentDate"]
            });
        }

        return Ok(result);
    }

    [HttpPut("{id}/trips/{tripId}")]
    public IActionResult RegisterClientToTrip(int id, int tripId)
    {
        var connString = _configuration.GetConnectionString("DefaultConnection");

        using var conn = new SqlConnection(connString);
        conn.Open();

        var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Client WHERE IdClient = @Id", conn);
        checkCmd.Parameters.AddWithValue("@Id", id);
        if ((int)checkCmd.ExecuteScalar() == 0) return NotFound("Client not found.");

        checkCmd = new SqlCommand("SELECT COUNT(*) FROM Trip WHERE IdTrip = @TripId", conn);
        checkCmd.Parameters.AddWithValue("@TripId", tripId);
        if ((int)checkCmd.ExecuteScalar() == 0) return NotFound("Trip not found.");

        checkCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @Id AND IdTrip = @TripId", conn);
        checkCmd.Parameters.AddWithValue("@TripId", tripId);
        if ((int)checkCmd.ExecuteScalar() > 0) return Conflict("Client already registered.");

        var insertCmd = new SqlCommand(@"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
            VALUES (@IdClient, @IdTrip, GETDATE())", conn);

        insertCmd.Parameters.AddWithValue("@IdClient", id);
        insertCmd.Parameters.AddWithValue("@IdTrip", tripId);
        insertCmd.ExecuteNonQuery();

        return Ok("Client registered successfully.");
    }

    [HttpDelete("{id}/trips/{tripId}")]
    public IActionResult DeleteClientTrip(int id, int tripId)
    {
        var connString = _configuration.GetConnectionString("DefaultConnection");

        using var conn = new SqlConnection(connString);
        conn.Open();

        var checkCmd = new SqlCommand(@"
            SELECT COUNT(*) FROM Client_Trip 
            WHERE IdClient = @Id AND IdTrip = @TripId", conn);
        checkCmd.Parameters.AddWithValue("@Id", id);
        checkCmd.Parameters.AddWithValue("@TripId", tripId);

        if ((int)checkCmd.ExecuteScalar() == 0) return NotFound("Registration not found.");

        var deleteCmd = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @Id AND IdTrip = @TripId", conn);
        deleteCmd.Parameters.AddWithValue("@Id", id);
        deleteCmd.Parameters.AddWithValue("@TripId", tripId);
        deleteCmd.ExecuteNonQuery();

        return Ok("Registration deleted.");
    }
}
