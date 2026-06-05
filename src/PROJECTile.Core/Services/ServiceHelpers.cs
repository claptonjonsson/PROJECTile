namespace PROJECTile.Core.Services;

internal static class ServiceHelpers
{
    public static string CleanRequired(string value, string name)
    {
        string trimmed = value.Trim();
        return trimmed.Length == 0
            ? throw new ArgumentException($"{name} is required.", nameof(value))
            : trimmed;
    }

    public static string NewId()
    {
        return Guid.NewGuid().ToString("N");
    }

    public static T FindRequired<T>(T? value, string entityName, string id)
        where T : class
    {
        return value ?? throw new InvalidOperationException($"{entityName} '{id}' was not found.");
    }
}
