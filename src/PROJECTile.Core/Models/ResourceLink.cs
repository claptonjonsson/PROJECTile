namespace PROJECTile.Core.Models;

public sealed class ResourceLink
{
    public string ResourceId { get; set; } = "";
    public ResourceLinkTargetType TargetType { get; set; }
    public string TargetId { get; set; } = "";
}
