using Hora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Hora.WebApi.Controllers;

[AllowAnonymous]
public sealed class HomeController : Controller
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HomeController(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        string dbStatus;
        try
        {
            await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync();
            await using var cmd = new NpgsqlCommand("SELECT 1;", conn);
            await cmd.ExecuteScalarAsync();
            dbStatus = "Connected";
        }
        catch
        {
            dbStatus = "Unavailable";
        }

        ViewBag.DbStatus = dbStatus;
        return View();
    }
}
