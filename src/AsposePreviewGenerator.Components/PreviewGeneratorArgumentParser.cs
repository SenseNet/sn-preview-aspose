using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AsposePreviewGenerator.Components;

public class Arguments
{
    public int ContentId { get; set; }
    public string Version { get; set; }
    public int StartIndex { get; set; }
    public int MaxPreviewCount { get; set; }
    public string SiteUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string ApiKey { get; set; }
    public bool IsCorrect { get; set; }
}

public class PreviewGeneratorArgumentParser
{
    public bool TryParse(string[] args, out Arguments result)
    {
        result = new Arguments();

        foreach (var arg in args)
        {
            if (arg.StartsWith("USERNAME:", StringComparison.OrdinalIgnoreCase))
            {
                result.Username = GetParameterValue(arg);
            }
            else if (arg.StartsWith("PASSWORD:", StringComparison.OrdinalIgnoreCase))
            {
                result.Password = GetParameterValue(arg);
            }
            else if (arg.StartsWith("APIKEY:", StringComparison.OrdinalIgnoreCase))
            {
                result.ApiKey = GetParameterValue(arg);
            }
            else if (arg.StartsWith("DATA:", StringComparison.OrdinalIgnoreCase))
            {
                var data = GetParameterValue(arg).Replace("\"\"", "\"");

                var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
                var serializer = JsonSerializer.Create(settings);
                var reader = new JsonTextReader(new StringReader(data));
                dynamic previewData = serializer.Deserialize(reader) as JObject;

                result.ContentId = previewData.Id;
                result.Version = previewData.Version;
                result.StartIndex = previewData.StartIndex;
                result.MaxPreviewCount = previewData.MaxPreviewCount;
                result.SiteUrl = previewData.CommunicationUrl;
            }
        }

        result.IsCorrect = result.ContentId > 0 && !string.IsNullOrEmpty(result.Version) && result.StartIndex >= 0 &&
                               result.MaxPreviewCount > 0 && !string.IsNullOrEmpty(result.SiteUrl);
        return result.IsCorrect;
    }
    private string GetParameterValue(string arg)
    {
        return arg.Substring(arg.IndexOf(":", StringComparison.Ordinal) + 1).TrimStart('\'', '"').TrimEnd('\'', '"');
    }

}