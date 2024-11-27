using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using WaizeRoutingServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddScoped<ISoapItineraryService, SoapItineraryService>();
builder.Services.AddScoped<ItineraryService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<SoapItineraryService>();
    serviceBuilder.AddServiceEndpoint<SoapItineraryService, ISoapItineraryService>(
        new BasicHttpBinding(),
        "/SoapItineraryService"
    );

    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
    serviceMetadataBehavior.HttpGetUrl = new Uri("http://localhost:5100/SoapItineraryService?wsdl");
});

app.MapControllers();

app.Run();