
using CommandLine;
using Microsoft.Azure.Cosmos;

class Options
{
    [Option('n', "account-name", Required = true)]
    public string AccountName { get; set; } = String.Empty;

    [Option('k', "primary-master-key", Required = true)]
    public string PrimaryMasterKey { get; set; } = String.Empty;

    // We need to pass the DocumentEndpoint in for environments higher than Test
    // Different environments have different endpoints
    // Test environment has document endpoint
    //   <>.documents-test.windows-int.net
    // Stage environment has document endpoint
    //   <>.documents-staging.windows-ppe.net
    [Option('e', "document-endpoint", Required = true)]
    public string DocumentEndpoint { get; set; } = String.Empty;

    [Option('d', "database-name", Required = false)]
    public string DatabaseName { get; set; } = "db";

    [Option('C', "container-name", Required = false)]
    public string ContainerName { get; set; } = "ct";

    [Option('p', "number-of-partitions", Required = false)]
    public int NumberOfPartitions { get; set; } = 10;

    [Option('c', "count", Required = false)]
    public int Count { get; set; } = 1;

    [Option('t', "request-time-out")]
    public int _requestTimeOutInSeconds { get; set; } = 5;

    [Option("cancellation-token", Required = false)]
    public int _cancellationTokenInSeconds { get; set; } = 7;

    // Client running time. Default runs forever unless the count is greater than 1.
    // Time unit can be specify for seconds (s), minutes (m), hours (h) or and days (d).
    // For example, -r 30s will run the client for 30 seconds.
    // If not specified it will use minutes: -r 3 will run the client for 3 minutes.
    [Option('r', "running-time", Required = false)]
    public string _RunningTimeStr { get; set; } = "";

    [Option("number-of-threads", Required = false)]
    public int NumberOfThreads { get; set; } = 1;

    [Option('o', "output-directory", Required = false)]
    public string OutputDirectory { get; set; } = "./";

    // Client telemetry is enabled by default. Requires account configs to be enabled.
    [Option("client-telemetry", Required = false)]
    public bool ClientTelemetryEnabled { get; set; } = true;

    [Option("update-ru-limit", Required = false)]
    public bool UpdateRULimit { get; set; } = false;

    [Option("partition-key-path", Required = false)]
    public string PartitionKeyPath { get; set; } = "/id"; // in ms

    [Option('s', "sleep-time", Required = false)]
    public int SleepTime { get; set; } = 1000;

    [Option('l', "max-length", Required = false)]
    public int MaxLength { get; set; } = 120;

    [Option("do-not-truncate", Required = false)]
    public bool DoNotTruncate { get; set; } = false;

    // Set to 0 to disable TTL. Default is 5 minutes for TTL.
    [Option("container-ttl-sec", Required = false)]
    public int? ContainerTtlSeconds { get; set; } = 5 * 60;

    public int Throughput { get { return NumberOfPartitions * 10000; } } // 10k per partition

    public System.Net.HttpStatusCode ThroughputResponse { get; set; }

    public string OutputPath { get { return $"{OutputDirectory}/{BaseFilename}.log"; } }

    public string InfoFile { get { return $"{OutputDirectory}/{BaseFilename}-info1.txt"; } }

    public override string ToString()
    {
        string[] info = {
            $"Targeting writes on {NumberOfPartitions} partitions",
            $"Document endpoint={DocumentEndpoint}",
            $"Key={PrimaryMasterKey}",
            $"Database={DatabaseName}",
            $"Container={ContainerName}",
        };
        if (UpdateRULimit)
        {
            info.Append($"Updating RUs. Attempting to use {Throughput} RUs container");
            info.Append($"Response={ThroughputResponse}");
        }
        return info.Aggregate((content, newLine) => $"{content}\n{newLine}");
    }

    public TimeSpan RunningTime
    {
        get
        {
            // Default to run forever if count is not specified.
            if (string.IsNullOrWhiteSpace(_RunningTimeStr))
            {
                if (Count <= 1)
                {
                    return TimeSpan.FromDays(99999);
                }

                return TimeSpan.Zero;
            }

            var value = _RunningTimeStr;
            var lastChar = value[value.Length - 1];
            var conversionFactor = 60; // if not specified, use minutes
            if (!Char.IsDigit(lastChar))
            {
                value = value.Substring(0, value.Length - 1);
                switch (lastChar)
                {
                    case 's': conversionFactor = 1; break;
                    case 'm': conversionFactor = 60; break;
                    case 'h': conversionFactor = 3600; break;
                    case 'd': conversionFactor = 86400; break;
                    default: throw new ArgumentException("Unknown time unit");
                }
            }
            return TimeSpan.FromSeconds(conversionFactor * Int32.Parse(value));
        }
    }

    public TimeSpan RequestTimeout
    {
        get
        {
            return new TimeSpan(0, 0, _requestTimeOutInSeconds);
        }
    }

    public TimeSpan CancellationToken
    {
        get
        {
            return new TimeSpan(0, 0, _cancellationTokenInSeconds);
        }
    }

    private string BaseFilename { get { return $"{AccountName}-{DateTime.UtcNow.ToString("yyMMdd-HHmm")}"; } }
}
