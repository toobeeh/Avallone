using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record DropAnnouncementDto(string DropToken, long DropId, int? EventDropId, int Position);