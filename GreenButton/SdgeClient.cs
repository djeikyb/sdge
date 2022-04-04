using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GreenButton;

public enum UsageFormat
{
    Xml,Csv
}

public class SdgeClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SdgeClient> _logger;

    public SdgeClient(HttpClient http, ILogger<SdgeClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task StepTwo(string cookies, MeterQueryResponse stepOneResponse, CancellationToken ct = default)
    {
        var rq = new HttpRequestMessage();
        rq.Method = HttpMethod.Get;
        rq.RequestUri = new Uri(stepOneResponse.Data.UrlFullPath);

        // curl 'https://myaccount.sdge.com/portal/Usage/GetGreenButtonFile?filename=9b1368752270500397Electric_60_Minute_1-1-2022_1-31-2022_20220402.csv'
        // -H 'User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:98.0) Gecko/20100101 Firefox/98.0'
        // -H 'Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8'
        // -H 'Accept-Language: en-US,en;q=0.5'
        // -H 'Accept-Encoding: gzip, deflate, br'
        // -H 'Connection: keep-alive'
        // -H 'Cookie: asdfadsfasdf
        // -H 'Upgrade-Insecure-Requests: 1'
        // -H 'Sec-Fetch-Dest: document'
        // -H 'Sec-Fetch-Mode: navigate'
        // -H 'Sec-Fetch-Site: none'
        // -H 'Sec-Fetch-User: ?1'
        // -H 'Pragma: no-cache'
        // -H 'Cache-Control: no-cache'
        rq.Headers.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        rq.Headers.Add("Accept-Language", "en-US,en;q=0.5");
        rq.Headers.Add("Accept-Encoding", "gzip, deflate, br");
        rq.Headers.Add("Connection", "keep-alive");
        rq.Headers.Add("Cache-Control", "no-cache");
        rq.Headers.Add("Cookie", cookies);
        rq.Headers.Add("Pragma", "no-cache");
        rq.Headers.Add("Sec-Fetch-Dest", "document");
        rq.Headers.Add("Sec-Fetch-Mode", "navigate");
        rq.Headers.Add("Sec-Fetch-Site", "same-origin");
        rq.Headers.Add("Sec-Fetch-User", "?1");
        rq.Headers.Add("Upgrade-Insecure-Requests", "1");
        rq.Headers.Add("User-Agent",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:98.0) Gecko/20100101 Firefox/98.0");


        var rs = await _http.SendAsync(rq, ct);

        if (rs.Content.Headers.ContentType?.MediaType != "application/octet-stream")
        {
            _logger.LogWarning($"Unexpected media type: {rs.Content.Headers.ContentType?.MediaType}");
        }

        if (rs.Content.Headers.ContentDisposition is not { DispositionType: "attachment" })
        {
            if (rs.Content.Headers.ContentDisposition is not null)
            {
                _logger.LogWarning(
                    $"Unexpected content disposition: {rs.Content.Headers.ContentDisposition.DispositionType}");
            }
            else
            {
                _logger.LogWarning("Missing content disposition.");
            }
        }

        var fs = File.OpenWrite(stepOneResponse.Message);
        await rs.Content.CopyToAsync(fs, ct);
        using var _ = _logger.BeginScope(new Dictionary<string, object> { { "filename", stepOneResponse.Message } });
        _logger.LogInformation("Saved file.");
// curl 'https://myaccount.sdge.com/portal/Usage/GetGreenButtonFile?filename=52d1f62d2270500397Electric_60_Minute_3-26-2022_4-1-2022_20220402.csv'
// -H 'User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:98.0) Gecko/20100101 Firefox/98.0'
// -H 'Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8'
// -H 'Accept-Language: en-US,en;q=0.5'
// -H 'Accept-Encoding: gzip, deflate, br'
// -H 'Connection: keep-alive'
// -H 'Cookie: asdfasdfasdf
// -H 'Upgrade-Insecure-Requests: 1'
// -H 'Sec-Fetch-Dest: document'
// -H 'Sec-Fetch-Mode: navigate'
// -H 'Sec-Fetch-Site: same-origin'
// -H 'Sec-Fetch-User: ?1'
// -H 'Pragma: no-cache'
// -H 'Cache-Control: no-cache'
    }

    public async Task<MeterQueryResponse> StepOne(
        string cookies,
        DateTime @from,
        DateTime to,
        string meterNumer,
        UsageFormat format,
        CancellationToken ct = default
    )
    {
        var meterQuery = new MeterQuery(
            meterNumer: meterNumer,
            fromDate: from.ToString("d"),
            toDate: to.ToString("d"),
            downloadType: format switch
            {
                UsageFormat.Csv => "csv",
                UsageFormat.Xml => "xml",
                _ => throw new Exception("Unhandled format: " + format)
            }
        );
        var meterQueryJson = JsonSerializer.Serialize(meterQuery);

        var rq = new HttpRequestMessage();
        rq.RequestUri = new UriBuilder
        {
            Scheme = "https",
            Host = "myaccount.sdge.com",
            Path = "/portal/Usage/GetGreenButtonData",
        }.Uri;
        rq.Method = HttpMethod.Post;
        rq.Content =
            new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("queryParam", meterQueryJson) });

        rq.Headers.Add("User-Agent",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:98.0) Gecko/20100101 Firefox/98.0");
        rq.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
        rq.Headers.Add("Accept-Language", "en-US,en;q=0.5");
        rq.Headers.Add("Accept-Encoding", "gzip, deflate, br");
        rq.Content.Headers.Add("X-Requested-With", "XMLHttpRequest");
        rq.Content.Headers.Add("Origin", "https://myaccount.sdge.com");
        rq.Headers.Add("Connection", "keep-alive");
        rq.Content.Headers.Add("Cookie", cookies);
        rq.Content.Headers.Add("Sec-Fetch-Dest", "empty");
        rq.Content.Headers.Add("Sec-Fetch-Mode", "cors");
        rq.Content.Headers.Add("Sec-Fetch-Site", "same-origin");
        rq.Headers.Add("Pragma", "no-cache");
        rq.Headers.Add("Cache-Control", "no-cache");

        var rs = await _http.SendAsync(rq, ct);
        rs.EnsureSuccessStatusCode();

        if (rs.Content.Headers.ContentType?.MediaType != MediaTypeNames.Application.Json)
        {
            throw new Exception("Did not get json back.");
        }

        var deserialized = await rs.Content.ReadFromJsonAsync<MeterQueryResponse>(cancellationToken: ct);
        if (deserialized == null)
        {
            throw new Exception("Failed to deserialize response.");
        }

        return deserialized;
    }
}

class MeterQuery
{
    [JsonPropertyName("MeterNumer")]
    public string MeterNumer { get; set; } // typo is from sdge api 😭

    /// <summary>
    /// Format mm/dd/yyyy, ie 2022-04-01 as 01/04/2022
    /// </summary>
    [JsonPropertyName("FromDate")]
    public string FromDate { get; set; }

    /// <summary>
    /// Format mm/dd/yyyy, ie 2022-04-01 as 01/04/2022
    /// </summary>
    [JsonPropertyName("ToDate")]
    public string ToDate { get; set; }

    [JsonPropertyName("DownloadType")]
    public string DownloadType { get; set; }

    public MeterQuery(string meterNumer, string fromDate, string toDate, string downloadType)
    {
        MeterNumer = meterNumer;
        FromDate = fromDate;
        ToDate = toDate;
        DownloadType = downloadType;
    }
}

public class MeterQueryResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("data")]
    public MeterQueryResponseData Data { get; set; } = null!;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(Status)}: {Status}, {nameof(Message)}: {Message}, {nameof(Data)}: {Data}";
    }
}

public class MeterQueryResponseData
{
    [JsonPropertyName("UrlFullPath")]
    public string UrlFullPath { get; set; } = null!;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(UrlFullPath)}: {UrlFullPath}";
    }
}
