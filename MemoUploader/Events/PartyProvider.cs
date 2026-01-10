using System.Collections.Generic;
using System.Linq;


namespace MemoUploader.Events;

public class PlayerSnapshot
{
    public uint   EntityId { get; init; }
    public string Name     { get; init; } = string.Empty;
    public string Server   { get; init; } = string.Empty;
    public uint   JobId    { get; init; }
    public uint   Level    { get; init; }
}

public interface IPartyProvider
{
    IReadOnlyCollection<PlayerSnapshot> GetPartySnapshots();
}

public class DalamudPartyProvider : IPartyProvider
{
    public IReadOnlyCollection<PlayerSnapshot> GetPartySnapshots()
    {
        if (DService.Instance().PartyList.Length >= 1)
        {
            return DService.Instance().PartyList.Select(p => new PlayerSnapshot
            {
                EntityId = p.EntityId,
                Name     = p.Name.ToString(),
                Server   = p.World.Value.Name.ToString(),
                JobId    = p.ClassJob.RowId,
                Level    = p.Level
            }).ToList();
        }

        if (DService.Instance().ObjectTable.LocalPlayer is { } local)
        {
            return
            [
                new PlayerSnapshot
                {
                    EntityId = local.EntityID,
                    Name     = local.Name.ToString(),
                    Server   = local.HomeWorld.Value.Name.ToString(),
                    JobId    = local.ClassJob.RowId,
                    Level    = local.Level
                }
            ];
        }

        return [];
    }
}
