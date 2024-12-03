using WaizeRoutingServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IRoutingService, RoutingService>();
builder.Services.AddSingleton<IAutocompleteService, AutocompleteService>();
builder.Services.AddSingleton<IApacheService, ApacheService>();

builder.Services.AddSingleton<ProxyClient>(sp => 
    new ProxyClient("http://localhost:5001/ProxyCacheService"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors();

app.UseRouting();

app.MapControllers();

app.Run();