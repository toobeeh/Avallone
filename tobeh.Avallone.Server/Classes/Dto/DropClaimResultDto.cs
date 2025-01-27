using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record DropClaimResultDto(string Username, bool FirstClaim, bool ClearedDrop, int CatchTime, double LeagueWeight, long DropId, bool LeagueMode);