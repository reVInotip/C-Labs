using Core.Models;
using InterfaceContracts.Channel;
using Interface.Strategy;
using Services.Strategy;
using Services.Channels;
using Services.Channels.Items;
using Interface;
using Services;
using Services.Network;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// Add Services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient("fork-client", cfg =>
{
    string uri = builder.Configuration["HOST_URI"]!;
    cfg.BaseAddress = new Uri(uri);
    cfg.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient("registration-client", cfg =>
{
    string uri = builder.Configuration["HOST_URI"]!;
    cfg.BaseAddress = new Uri(uri);
    cfg.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddSingleton<IChannel<PhilosopherToControllerChannelItem>,
    PhilosopherToControllerChannel<PhilosopherToControllerChannelItem>>();
builder.Services.AddSingleton<IChannel<PhilosopherActionItem>,
    PhilosopherToControllerChannel<PhilosopherActionItem>>();
builder.Services.AddSingleton<IChannel<ApplicationStopItem>,
    PhilosopherToControllerChannel<ApplicationStopItem>>();
builder.Services.AddSingleton<IStrategy, LeftRightStrategy>();
builder.Services.AddSingleton<ILogger<PhilosopherService>, Logger<PhilosopherService>>();
builder.Services.AddSingleton<IRegistration, RegistrationService>();

builder.Services.AddSingleton<IFork, ForkService>();
builder.Services.AddSingleton<IFork, ForkService>();

builder.Services.AddHostedService<PhilosopherService>();

builder.Services.Configure<PhilosopherConfiguration>(builder.Configuration.GetSection(nameof(PhilosopherConfiguration)));
builder.Services.Configure<ServicesConfiguration>(builder.Configuration.GetSection(nameof(ServicesConfiguration)));

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
