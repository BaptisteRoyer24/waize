using WaizeRoutingServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration des services
builder.Services.AddControllers(); // Ajout des contrôleurs REST
builder.Services.AddHttpClient(); // Pour les appels HTTP externes
builder.Services.AddSingleton<IRoutingService, RoutingService>(); // Injection du service métier
builder.Services.AddSingleton<IAutocompleteService, AutocompleteService>(); // Injection du service métier
builder.Services.AddSingleton<IApacheService, ApacheService>(); // Injection du service métier

// Ajout de ProxyClient avec une URL de service configurée
builder.Services.AddSingleton<ProxyClient>(sp => 
    new ProxyClient("http://localhost:5001/ProxyCacheService"));

// Étape 1 : Ajouter CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()  // Autorise toutes les origines
            .AllowAnyHeader()  // Autorise tous les en-têtes
            .AllowAnyMethod(); // Autorise toutes les méthodes (GET, POST, etc.)
    });
});

builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

// Configuration du pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Page d'erreurs pour le développement
}

// Étape 2 : Activer CORS
app.UseCors();

app.UseRouting(); // Routage des requêtes

app.MapControllers(); // Enregistre automatiquement les routes des contrôleurs REST

// Démarrer l'application
app.Run();