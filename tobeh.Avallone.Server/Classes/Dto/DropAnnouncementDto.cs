using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record DropAnnouncementDto(string DropToken, int? EventDropId, int Position);