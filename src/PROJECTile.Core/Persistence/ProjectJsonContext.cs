using System.Text.Json.Serialization;
using PROJECTile.Core.Models;

namespace PROJECTile.Core.Persistence;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ProjectDocument))]
public partial class ProjectJsonContext : JsonSerializerContext;
