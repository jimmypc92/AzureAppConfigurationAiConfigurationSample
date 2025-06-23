using Azure.Identity;
using Microsoft.FeatureManagement;
using AzureAppConfigurationChatBot;
using AzureAppConfigurationChatBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureAppConfiguration(options =>
{
    var credential = new DefaultAzureCredential();

    options.Connect(new Uri(builder.Configuration["AppConfig:Endpoint"]), credential)
        .ConfigureRefresh(refresh =>
        {
            refresh.RegisterAll();
            refresh.SetRefreshInterval(TimeSpan.FromSeconds(10));
        })
        .ConfigureKeyVault(kv =>
        {
            kv.SetCredential(credential);
        })
        .UseFeatureFlags(ff =>
        {
            ff.SetRefreshInterval(TimeSpan.FromSeconds(10));
        });
});

builder.Services.Configure<AzureOpenAIConnectionInfo>(
    builder.Configuration.GetSection("AzureOpenAI"));

// Add services to the container
builder.Services.AddControllers();

builder.Services.AddApplicationInsightsTelemetry();

// Add Azure App Configuration services
builder.Services.AddAzureAppConfiguration();

// Add Feature Management services
builder.Services.AddFeatureManagement()
    .WithTargeting()
    .AddApplicationInsightsTelemetry();

// Set up query string authentication for demonstration
builder.Services.AddAuthentication(defaultScheme: Schemes.QueryString)
        .AddQueryString();

// Register OpenAI service
builder.Services.AddSingleton<IOpenAIService, AzureOpenAIService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteClient", builder =>
    {
        builder.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Use Azure App Configuration middleware
app.UseAzureAppConfiguration();
//
// Add Targeting Id to HttpContext
app.UseMiddleware<TargetingHttpContextMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowViteClient");
app.UseAuthorization();
app.MapControllers();

app.Run();
