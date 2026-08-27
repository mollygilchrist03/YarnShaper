namespace YarnShaper.Web;

internal static class QueryStringHelper
{
    public static string? GetParam(string uri, string name)
    {
        var query = new Uri(uri).Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (Uri.UnescapeDataString(parts[0]) != name) continue;
            return parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
        }

        return null;
    }
}
