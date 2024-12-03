using CoreWCF;
using CoreWCF.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddServiceModelServices();
builder.WebHost.UseUrls("http://localhost:5001");

var app = builder.Build();

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<ProxyCacheService>(serviceOptions =>
    {
        serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = true;
    });
    serviceBuilder.AddServiceEndpoint<ProxyCacheService, IProxyCacheService>(
        new BasicHttpBinding(), "/ProxyCacheService");
});

app.Run();