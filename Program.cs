/*
 * Basic usage: The following command will start 10 clients and perform 600
 * writes for each client. Because the default sleep time is 1 s, it will take
 * about 10 minutes.
 *
 * dotnet run -- -n $AccountName -k $Key -c 600 -t 10
 *
 */

using CommandLine;
using Microsoft.Azure.Cosmos;
using System.Diagnostics;
using System.Reflection;
using System.Security.Authentication;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        var options = Parser.Default.ParseArguments<Options>(args).Value;
        if (options is null)
        {
            throw new Exception("Arguments provided are null. Please ensure that you are using the flags correctly. See the example in the top of the source code.\nSample Usage: dotnet run -- -n $AccountName -k $PrimaryMasterKey -e $DocumentEndpoint");
        }

        Trace.Listeners.Add(new Diagnostics.CustomTextWriterTraceListener(options.OutputPath));
        Trace.Listeners.Add(new Diagnostics.CustomConsoleTraceListener(options.MaxLength, options.DoNotTruncate));

        System.Environment.SetEnvironmentVariable("AZURE_COSMOS_PARTITION_LEVEL_FAILOVER_ENABLED", "FALSE");
        var preferredRegions = await GetPreferredRegions(options);
        System.Environment.SetEnvironmentVariable("AZURE_COSMOS_PARTITION_LEVEL_FAILOVER_ENABLED", "TRUE");

        CosmosClientOptions cosmosClientOptions = new CosmosClientOptions()
        {
            ApplicationPreferredRegions = preferredRegions,
            RequestTimeout = options.RequestTimeout,
            MaxRetryAttemptsOnRateLimitedRequests = 0,
            EnableTcpConnectionEndpointRediscovery = true,
            IdleTcpConnectionTimeout = new TimeSpan(0, 10, 0),
            OpenTcpConnectionTimeout = new TimeSpan(0, 0, 1),
            ApplicationName = "",
        };

        // Only works if the service has the account enabled.
        // Required to make Client Telemetry work.
        HttpClientHandler httpClientHandler = new HttpClientHandler();
        httpClientHandler.SslProtocols = SslProtocols.Tls12;
        using HttpClient httpClient = new HttpClient(httpClientHandler);
        if (options.ClientTelemetryEnabled)
        {
            cosmosClientOptions.CosmosClientTelemetryOptions = new CosmosClientTelemetryOptions()
            {
                DisableSendingMetricsToService = false
            };
            cosmosClientOptions.HttpClientFactory = () => httpClient;
        }

        //Type type = typeof(CosmosClientOptions);
        //PropertyInfo propertyInfo = type.GetProperty("EnablePartitionLevelFailover", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        //propertyInfo.SetValue(cosmosClientOptions, true);

        using var client = new CosmosClient(
            options.DocumentEndpoint,
            options.PrimaryMasterKey,
            cosmosClientOptions);

        var db = await client.CreateDatabaseIfNotExistsAsync(options.DatabaseName);

        ContainerProperties containerProperties = new ContainerProperties(options.ContainerName, options.PartitionKeyPath);
        if (options.ContainerTtlSeconds != 0)
        {
            containerProperties.DefaultTimeToLive = options.ContainerTtlSeconds;
        }

        Container container = await db.Database.CreateContainerIfNotExistsAsync(containerProperties, options.Throughput);

        if (options.UpdateRULimit)
        {
            var response = await container.ReplaceThroughputAsync(ThroughputProperties.CreateManualThroughput(options.Throughput));
            options.ThroughputResponse = response.StatusCode;
        }

        // Validate PPAF is on.
        Type type = typeof(CosmosClientOptions);
        PropertyInfo? propertyInfo = type.GetProperty("EnablePartitionLevelFailover", BindingFlags.NonPublic | BindingFlags.Instance);
        if (propertyInfo == null || (bool?)propertyInfo.GetValue(client.ClientOptions) == false)
        {
            throw new ArgumentException("Partition Level Failover is not enabled. Please enable it in the CosmosClientOptions.");
        }

        using (FileStream fs = File.OpenWrite(options.InfoFile))
        {
            var info = new UTF8Encoding(true).GetBytes(options.ToString());
            fs.Write(info, 0, info.Length);
        }

        var tasks = new List<Task>();
        for (var i = 0; i < options.NumberOfThreads; i++)
        {
            var c = new Client(container, i, options);
            tasks.Add(Task.Run(() => c.StartAsync()));
        }
        await Task.WhenAll(tasks);
        Trace.Flush();
    }

    // Create a temp instant to get the regions from the service. This way the preferred region list is always accurate.
    // This will be an issue if gateway is down during the start of the program.
    public static async Task<List<string>> GetPreferredRegions(Options options)
    {
        try
        {
            List<string> preferredRegions = new List<string>();
            using (var tempClient = new CosmosClient(
              options.DocumentEndpoint,
              options.PrimaryMasterKey))
            {
                var accountInfo = await tempClient.ReadAccountAsync();
                preferredRegions.AddRange(accountInfo.WritableRegions.Select(x => x.Name));
                preferredRegions.AddRange(accountInfo.ReadableRegions.Select(x => x.Name));
            }

            // Remove duplicate regions caused
            return preferredRegions.Distinct().ToList();
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to get the preferred regions. Exception: {e}");
            // Fallback to hard coded copy of regions.
            return options.DocumentEndpoint.Contains("test")
               ? new List<string>() { Regions.NorthCentralUS, Regions.WestUS, Regions.EastAsia }
               : new List<string>() { Regions.WestUS2, Regions.EastUS2, Regions.NorthCentralUS };
        }
    }
}
