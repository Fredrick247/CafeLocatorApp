using AspNetCoreRateLimit;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
             Endpoint = "GET:/api/Home/GetCafes",
            Limit = 10, // Allow 10 requests per minute
            Period = "1m"
        }
    };
});
builder.Services.AddInMemoryRateLimiting();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()); // Required for WebSockets
});
var app = builder.Build();
app.UseIpRateLimiting();
app.UseWebSockets();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    // ✅ Redirects root `/` to `/api/Home`
    endpoints.MapGet("/", async context =>
    {
        context.Response.Redirect("/api/Home");
    });

    // ✅ Maps API controllers (e.g., /api/Home/GetCafes)
    endpoints.MapControllers();

    // ✅ WebSocket handling for "/CafeLocatorApp"
    endpoints.Map("/CafeLocatorApp", async context =>
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await CafeLocatorApp.Services.WebSocketHandler.HandleWebSocket(webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    });
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
