using Quartz;
using tobeh.Avallone.Server.Quartz.GuildLobbyUpdater;

namespace tobeh.Avallone.Server.Quartz.DropAnnouncer;

public static class DropAnnouncerConfiguration
{
    public static void Configure(IServiceCollectionQuartzConfigurator configurator)
    {
        var jobId = new JobKey($"Drop Announcer");

        configurator.AddJob<DropAnnouncerJob>(job => job
            .WithIdentity(jobId));

        configurator.AddTrigger(trigger => trigger
            .ForJob(jobId)
            .StartNow()
            .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(2))); /* run once, then calculate delay dynamically */
    }
}