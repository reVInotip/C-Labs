global using Interface.Strategy;
global using Philosophers.Services;

using System;
using System.Diagnostics;
using System.Threading;
using Interface;
using Interface.Channel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Philosophers.Core.HostedServices.Philosophers;
using Philosophers.Core.HostedServices;
using Philosophers.Services.Channels.Items;
using Philosophers.Services.Channels;
using Philosophers.Services.Strategy;
using Philosophers.Services.Utils;
using Philosophers.Services.DB.Service;
using Philosophers.DB;
using Microsoft.EntityFrameworkCore;

bool helpOnly = false;

try
{
    ParseArgs(out string pathToConf, out helpOnly);

    if (helpOnly) return;

    IHost host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((hostContext, configuration) =>
        {
            configuration.AddJsonFile(pathToConf, optional: false, reloadOnChange: false);
        })
    .ConfigureServices((hostContext, services) =>
        {
            var s = hostContext.Configuration.GetValue<string>("ConnectionStrings:PostgresConnection");
            services.AddDbContext<RunsContext>(options => options.UseNpgsql(s));

            services.AddSingleton<IChannel<PhilosopherToAnalyzerChannelItem>, PhilosopherToAnalyzerChannel>();
            services.AddSingleton<IChannel<PhilosopherToPrinterChannelItem>, PhilosopherToPrinterChannel>();
            services.AddSingleton<IStrategy, LeftRightStrategy>();
            services.AddSingleton<ILogger<PhilosopherService>, Logger<PhilosopherService>>();
            services.AddSingleton<ILogger<DeadlockAnalyzer>, Logger<DeadlockAnalyzer>>();
            services.AddSingleton<ILogger<SimulationManager>, Logger<SimulationManager>>();
            services.AddSingleton<CompletionCoordinator>();

            services.AddScoped<ISimulationDatabaseProcessor, SimulationDatabaseProcessor>();

            services.AddTransient<IForksFactory, ForksFactory>();

            services.AddHostedService<Aristotel>();
            services.AddHostedService<Engels>();
            services.AddHostedService<Kant>();
            services.AddHostedService<Marks>();
            services.AddHostedService<Platon>();

            services.AddHostedService<DeadlockAnalyzer>();
            services.AddHostedService<SimulationManager>();

            var root = hostContext.Configuration;
            services.Configure<PhilosopherConfiguration>(root.GetSection(nameof(PhilosopherConfiguration)));
        })
        .Build();
    
    using (var scope = host.Services.CreateScope())
    {
        scope.ServiceProvider.GetRequiredService<RunsContext>().Database.EnsureCreated();
    }

    host.Run();
}

catch (ApplicationException e)
{
    Console.WriteLine(e.Message);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Simulation was cancelled.");
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
    Console.Write(e.StackTrace);
}

static void ParseArgs(out string pathToConf, out bool helpOnly)
{
    pathToConf = "./philosophers.json";
    helpOnly = false;

    bool wasConfigPath = false;

    bool confPathFlag = false;

    var args = Environment.GetCommandLineArgs();

    foreach (var arg in args)
    {
        if (confPathFlag)
        {
            if (wasConfigPath)
                throw new ArgumentException("Double set path");

            pathToConf = arg;
            wasConfigPath = true;
            confPathFlag = false;
        }

        if (arg == "-c" || arg == "--config_path")
            confPathFlag = true;
        else if (arg == "-h" || arg == "--help")
        {
            Console.Write(
                """
                This is lab1 of NSU C# course.

                *DESCRIPTION*
                In this lab, I solved the Dining Philosophers problem using .Net Generic Host and EntityFramework.

                *ARGUMENTS*
                -c or --config_path - relative or full path to config file. Current directory used by default.
                -h or --help - see this page
                """
            );

            helpOnly = true;
        }
    }
}