using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Quartz;
using tobeh.Avallone.Server.Authentication;
using tobeh.Avallone.Server.Hubs;
using tobeh.Avallone.Server.Quartz.GuildLobbyUpdater;
using tobeh.Avallone.Server.Quartz.SkribblLobbyUpdater;
using tobeh.Avallone.Server.Service;
using tobeh.Valmar.Client.Util;

namespace tobeh.Avallone.Server;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Avallone SignalR Server");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        // create host and run
        var host = CreateHost(args);
        SetupRoutes(host);
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogDebug("Initialized app");
        
        await host.RunAsync();
    }
    
    private static WebApplication CreateHost(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddValmarGrpc(builder.Configuration.GetValue<string>("Grpc:ValmarAddress"))
            /*.AddQuartz(GuildLobbiesUpdaterConfiguration.Configure)*/
            .AddQuartz(SkribblLobbyUpdaterConfiguration.Configure)
            .AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; })
            .AddSingleton<GuildLobbiesStore>()
            .AddSingleton<LobbyContextStore>()
            .AddSingleton<LobbyStore>()
            .AddScoped<LobbyService>()
            .AddCors()
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TypoTokenDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = TypoTokenDefaults.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TypoTokenHandler>(TypoTokenDefaults.AuthenticationScheme, null).Services
            .AddSignalR().Services
            .AddLogging(loggingBuilder => loggingBuilder
                .AddConfiguration(builder.Configuration.GetSection("Logging"))
                .AddConsole())
            .BuildServiceProvider();
        
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(builder.Configuration.GetRequiredSection("SignalR").GetValue<int>("HostPort"));
        });

        return builder.Build();
    }
    
    private static void SetupRoutes(WebApplication app)
    {
        app.MapHub<GuildLobbiesHub>("/guildLobbies");
        app.MapHub<LobbyHub>("/lobby");
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseCors(options =>
        {
            options.WithOrigins("*").DisallowCredentials().WithHeaders("*").WithMethods("*");
        });
    }
}