using CoreWCF;
using CoreWCF.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Ajouter CoreWCF au conteneur de services
builder.Services.AddServiceModelServices();

var app = builder.Build();

// Configurer CoreWCF
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<ProxyCacheService>(serviceOptions =>
    {
        serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = true;
    });
    serviceBuilder.AddServiceEndpoint<ProxyCacheService, IProxyCacheService>(
        new BasicHttpBinding(), "/ProxyCacheService");
});

// Lancer l'application
app.Run();