﻿

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TradingBot.Data.Config;
using TradingBot.Services;
using TradingDataBaseLib;
using TradingDataBaseLib.Services;

namespace TradingBot;

public static class Programm
{
    // Define a static async method named Main, which is the entry point for the application.
    public static async Task Main(string[] args)
    {
        IHost host = AppStartup();
        Application app = ActivatorUtilities.CreateInstance<Application>(host.Services);
        await app.Start();
    }

    // Define a static method named ConfigSetup to set up the configuration for the application.
    public static void ConfigSetup(IConfigurationBuilder builder)
    {
        // Set the base path for the configuration file to the current directory.
        builder.SetBasePath(Directory.GetCurrentDirectory())
            // Add the appsettings.json file to the configuration, require the file to exist, and reload on change.
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            // Add environment variables to the configuration.
            .AddEnvironmentVariables();
    }

    // Define a static method named AppStartup to initialize the application and return an instance of the host.
    static IHost AppStartup()
    {
        // Create a new configuration builder.
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();

        // Call the ConfigSetup method to configure the builder.
        ConfigSetup(configBuilder);

        IConfiguration config = configBuilder.Build();
        // Create a new logger using the configuration built by the ConfigurationBuilder.
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configBuilder.Build())
            .Enrich.FromLogContext()
            .CreateLogger();

        DataBaseConfig dbConfig = config.GetSection("DataBase").Get<DataBaseConfig>() ?? throw new Exception("Can not get 'DataBase' appsettings section");

        // Create a new instance of the host with the default settings, configure services, add Serilog, and build the host.
        IHost host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
        {
            services.AddSingleton<Application>();

            services.AddDbContext<TradingContext>(x =>
            {
                x.UseMySQL(dbConfig.Connection);
            });

            services.AddTransient<TradingActionService>();
            services.AddTransient<WebhookService>();
            services.AddTransient<TelegramService>();
            services.AddTransient<BrokerService>();
            services.AddTransient<TradingViewService>();
        }).UseSerilog()
        .Build();

        // Return the host instance.
        return host;
    }
}