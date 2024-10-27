using Microsoft.Azure.Cosmos;
using System.Diagnostics;
using System.Linq;
using System.Threading;

class Client
{
    private readonly Container container;
    private readonly int id;
    private readonly Options options;
    private int successCounter = 0;

    public Client(Container container, int id, Options options)
    {
        this.container = container;
        this.id = id;
        this.options = options;
    }

    private async Task InsertDocumentAsync(int i)
    {
        var requestInfo = new RequestInfo
        {
            Attempts = i,
            ClientId = id,
            Successes = Volatile.Read(ref successCounter),
        };

        // Set a timeout for the operations in case the SDK gets stuck.
        using CancellationTokenSource cts = new CancellationTokenSource(options.CancellationToken);

        try
        {
            var response = await container.CreateItemAsync(Book.Build(), cancellationToken: cts.Token);
            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                Interlocked.Increment(ref successCounter);
                requestInfo.Successes++;
            }
            requestInfo.Status = (int)response.StatusCode;
            requestInfo.ContactedRegions = response.Diagnostics.GetContactedRegions();
            requestInfo.RequestLatency = response.Diagnostics.GetClientElapsedTime();
            if (response.Diagnostics.GetClientElapsedTime() > TimeSpan.FromMilliseconds(115))
            {
                requestInfo.Diagnostics = response.Diagnostics.ToString();
            }

        }
        catch (CosmosException ce)
        {
            requestInfo.Status = (int)ce.StatusCode;
            requestInfo.Substatus = ce.SubStatusCode;
            requestInfo.Diagnostics = ce.Diagnostics.ToString();
        }
        catch (CosmosOperationCanceledException oce)
        {
            requestInfo.ErrorMessage = oce.Message;
            requestInfo.Diagnostics = oce.Diagnostics.ToString();
        }
        catch (Exception e)
        {
            requestInfo.ErrorMessage = e.Message.ToString();
        }
        Trace.TraceInformation(requestInfo.ToString());
    }

    public async Task StartAsync()
    {
        var tasks = new List<Task>();
        var i = 1;
        var endTime = DateTime.UtcNow.Add(options.RunningTime);
        while (i <= options.Count || DateTime.UtcNow < endTime)
        {
            tasks.Add(Task.Run(() => InsertDocumentAsync(i)));
            await Task.Delay(options.SleepTime);
            i += 1;
        }
        await Task.WhenAll(tasks);
    }
}
