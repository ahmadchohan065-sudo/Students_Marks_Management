namespace MarksApi.Common;

public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiryMinutes { get; set; }
}

public class MarkingOptions
{
    public int PassMark { get; set; }   // value comes from appsettings.json, not from code
}