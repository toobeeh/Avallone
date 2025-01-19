using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record DropAnnouncementDto(long DropId, int? EventDropId, int Position);