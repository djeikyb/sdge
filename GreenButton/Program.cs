// See https://aka.ms/new-console-template for more information

using GreenButton;

using IHost host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddTransient<HttpSpy>();
        services.AddHttpClient<SdgeClient>()
            .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler { UseCookies = false })
            .AddHttpMessageHandler<HttpSpy>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🐙");

var cookies = "REPLACE ME";

if (args == null || args.Length < 1)
{
    throw new Exception("Must pass meter number as first argument.");
}
var meter = args[0];

var sdge = host.Services.GetRequiredService<SdgeClient>();
var s1 = await sdge.StepOne(
    cookies: cookies,
    @from: DateTime.Parse($"2018-01-01"),
    to: DateTime.Parse($"2022-04-01"),
    meterNumer: meter,
    format: UsageFormat.Xml
);
logger.LogInformation(s1.ToString());
await sdge.StepTwo(cookies, s1);
