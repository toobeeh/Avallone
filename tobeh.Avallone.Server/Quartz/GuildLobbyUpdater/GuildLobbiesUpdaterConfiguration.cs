using Quartz;

namespace tobeh.Avallone.Server.Quartz.GuildLobbyUpdater;

public static class GuildLobbiesUpdaterConfiguration
{
    public static void Configure(IServiceCollectionQuartzConfigurator configurator)
    {
        var jobId = new JobKey($"Guild Lobbies Updater");

        configurator.AddJob<GuildLobbiesUpdaterJob>(job => job
            .WithIdentity(jobId));

        configurator.AddTrigger(trigger => trigger
            .ForJob(jobId)
            .StartNow()
            .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(2).RepeatForever()));
    }
}