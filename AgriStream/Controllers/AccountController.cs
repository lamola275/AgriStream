using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ST10506644_Agristream.Controllers;   // keep YOUR project's namespace

public class AccountController : Controller
{
    private readonly string _connStr;

    public AccountController(IConfiguration config)
    {
        _connStr = config["SqlConnectionString"]
            ?? throw new Exception("SqlConnectionString app setting is missing!");
    }

    // GET /Account/Login - shows the login page (KEEP THIS AS IS)
    [HttpGet]
    public IActionResult Login() => View();

    // POST /Account/Login - handles the form submit (REPLACE WITH THE NEW VERSION)
    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        // Empty form? Show a message, never touch the database.
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Please enter your username and password.";
            return View();
        }

        using var conn = new SqlConnection(_connStr);
        conn.Open();

        var cmd = new SqlCommand(
            "SELECT Role FROM Users WHERE Username = @u AND Password = @p", conn);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@p", password);

        var role = cmd.ExecuteScalar() as string;

        if (role == null)
        {
            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        HttpContext.Session.SetString("username", username);
        HttpContext.Session.SetString("role", role);
        return RedirectToAction("Index", "Dashboard");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}