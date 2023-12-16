using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly DataBaseNameContext _context;
    private HttpClient _client;
    
    public WeatherController(DataBaseNameContext context)
    {
        _context = context;
        _client = new HttpClient();
        _client.BaseAddress = new Uri("http://apitwo_container:80/");
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

    }
    
    [HttpGet]
    [Produces("application/hal+json")]
    public async Task<IActionResult> Get()
    {
        return Ok(await _context.Weathers.ToListAsync());
    }

    [HttpGet("{id}")]
    [Produces("application/hal+json")]
    public async Task<IActionResult> Get(int id)
    {
        return Ok(await _context.Weathers.FindAsync(id));
    }
    
    [HttpGet("network")]
    public IActionResult NetworkRequest()
    {
        var response = _client.GetAsync("WeatherForecast").Result;
        return Ok(response.Content.ReadAsStringAsync().Result);
    } 
}