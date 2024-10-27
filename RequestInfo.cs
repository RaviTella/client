using System.Dynamic;

class RequestInfo
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int Attempts { get; set; } = 0;

    public int Successes { get; set; } = 0;

    public string Diagnostics { get; set; } = "{}";

    public int ClientId { get; set; } = 0;

    public int Status { get; set; } = 0;

    public int Substatus { get; set; } = 0;

    public IReadOnlyList<(string regionName, Uri uri)> ContactedRegions { get; set; }

    public string ErrorMessage { get; set; } = String.Empty;

    public TimeSpan RequestLatency { get; set; } = TimeSpan.FromMilliseconds(0);

    public override string ToString()
    {
        var timestamp = Quote(Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
        var contactedRegions = ContactedRegions == null
            ? "[]"
            : $"[{String.Join(",", ContactedRegions.Select(region => Quote(region.regionName)))}]";
        var errorMessage = Quote(ErrorMessage);
        return $"{{\"timestamp\":{timestamp},\"clientId\":{ClientId},\"attempts\":{Attempts},\"successes\":{Successes},\"status\":{Status},\"substatus\":{Substatus},\"RequestLatencyMS\":{RequestLatency.Milliseconds},\"contactedRegions\":{contactedRegions},\"diagnostics\":{Diagnostics},\"errorMessage\":{errorMessage}}}";
    }

    private string Quote(string str)
    {
        return $"\"{str.Replace("\"", "\\\"")}\"";
    }
}