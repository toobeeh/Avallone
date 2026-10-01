using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using tobeh.Avallone.Server.Authentication;
using tobeh.Avallone.Server.Config;
using tobeh.Avallone.Server.Hubs;
using tobeh.Avallone.Server.Quartz.DecoyAnnouncer;
using tobeh.Avallone.Server.Quartz.DropAnnouncer;
using tobeh.Avallone.Server.Quartz.GuildLobbyUpdater;
using tobeh.Avallone.Server.Quartz.OnlineItemsUpdater;
using tobeh.Avallone.Server.Quartz.SkribblLobbyUpdater;
using tobeh.Avallone.Server.Service;
using tobeh.Avallone.Server.Util;
using tobeh.Valmar.Client.Util;

namespace tobeh.Avallone.Server;

class Program
{
    static async Task Main(string[] args)
    {
        /*CryptoService.TestEncryption();*/
        
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
            .AddQuartz(GuildLobbiesUpdaterConfiguration.Configure)
            .AddQuartz(SkribblLobbyUpdaterConfiguration.Configure)
            .AddQuartz(OnlineItemsUpdaterConfiguration.Configure)
            /*.AddQuartz(DropAnnouncerConfiguration.Configure)
            .AddQuartz(DecoyAnnouncerConfiguration.Configure)*/
            .Configure<CryptoConfig>(builder.Configuration.GetSection("Crypto"))
            .AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; })
            .AddSingleton<CryptoService>()
            .AddSingleton<GuildLobbiesStore>()
            .AddSingleton<LobbyContextStore>()
            .AddSingleton<LobbyStore>()
            .AddSingleton<OnlineItemsStore>()
            .AddScoped<LobbyService>()
            .AddScoped<MemberContext>()
            .AddSingleton<MemberContextCache>()
            .AddHttpContextAccessor()
            .AddCors()
            
            
            /*.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TypoTokenDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = TypoTokenDefaults.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TypoTokenHandler>(TypoTokenDefaults.AuthenticationScheme, null).Services*/
            
            /* support legacy and jwt tokens */
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "HybridScheme";
                options.DefaultChallengeScheme = "HybridScheme";
            })
            .AddJwtBearer("Jwt", jwtOptions =>
            {
                jwtOptions.Authority = "https://api.typo.rip/openid";
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = "https://api.typo.rip"
                };
                
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            .AddScheme<AuthenticationSchemeOptions, TypoTokenHandler>(TypoTokenDefaults.AuthenticationScheme, null)
            .AddPolicyScheme("HybridScheme", "JWT or Legacy", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var token = context.Request.Query["access_token"].FirstOrDefault() ?? context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                    return token is null || token.Contains('.') ? // crude check for JWT
                        "Jwt" : TypoTokenDefaults.AuthenticationScheme;
                };
            }).Services
                
                
            .AddSignalR().AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new SafeJsonStringConverter());
            }).Services
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
        app.MapHub<OnlineItemsHub>("/onlineItems");

        app.UseCors(options =>
        {
            options.WithOrigins("*").DisallowCredentials().WithHeaders("*").WithMethods("*");
        });
        
        app.UseAuthentication();
        app.UseAuthorization();
    }
}