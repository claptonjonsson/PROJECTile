using System.Text.Json.Serialization;

namespace PROJECTile.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ProjectTaskStatus>))]
public enum ProjectTaskStatus
{
    Todo,
    Doing,
    Done
}
