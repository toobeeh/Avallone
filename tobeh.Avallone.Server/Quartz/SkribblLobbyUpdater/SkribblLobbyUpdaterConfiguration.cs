using Quartz;

namespace tobeh.Avallone.Server.Quartz.SkribblLobbyUpdater;

public static class SkribblLobbyUpdaterConfiguration
{
    public static void Configure(IServiceCollectionQuartzConfigurator configurator)
    {
        var jobId = new JobKey($"Skribbl Lobby Updater");

        configurator.AddJob<SkribblLobbyUpdaterJob>(job => job
            .WithIdentity(jobId));

        configurator.AddTrigger(trigger => trigger
            .ForJob(jobId)
            .StartNow()
            .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(2).RepeatForever()));
    }
}