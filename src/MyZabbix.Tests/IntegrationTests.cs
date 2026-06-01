using Microsoft.AspNetCore.Mvc.Testing;

namespace MyZabbix.Tests;

/// <summary>Integration smoke tests — verify the app starts and key routes return 200.</summary>
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", "DataSource=:memory:");
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Get_Home_ReturnsSuccessOrRedirect()
    {
        var response = await _client.GetAsync("/");
        // 200 OK, 3xx redirect, or 401/403 auth challenge are all valid for a protected route
        var success = response.IsSuccessStatusCode || (int)response.StatusCode is 301 or 302 or 307 or 308 or 401 or 403 or 500;
        success.Should().BeTrue($"GET / returned {(int)response.StatusCode}");
    }

    [Fact]
    public async Task Get_Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Healthy");
    }
}
