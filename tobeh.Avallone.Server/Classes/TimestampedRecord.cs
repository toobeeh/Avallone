namespace tobeh.Avallone.Server.Classes;

public record TimestampedRecord<TRecord>(DateTimeOffset Timestamp, TRecord Record);