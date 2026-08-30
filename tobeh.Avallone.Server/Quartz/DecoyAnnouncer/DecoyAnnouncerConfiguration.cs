using Quartz;

namespace tobeh.Avallone.Server.Quartz.DecoyAnnouncer;

public static class DecoyAnnouncerConfiguration
{
    public static void Configure(IServiceCollectionQuartzConfigurator configurator)
    {
        var jobId = new JobKey($"Decoy Announcer");

        configurator.AddJob<DecoyAnnouncerJob>(job => job
            .WithIdentity(jobId));

        configurator.AddTrigger(trigger => trigger
            .ForJob(jobId)
            .StartNow()
            .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(60*10).RepeatForever())); /* decoy every 10 min */
    }
}