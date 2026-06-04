using System.Text.Json.Serialization;

namespace PROJECTile.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ResourceLinkTargetType>))]
public enum ResourceLinkTargetType
{
    Task,
    CodeTodo
}
