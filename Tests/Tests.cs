using System.Security.Cryptography;
using NUnit.Framework;

namespace Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class Tests
{
    // On dev workstation, throughput is about 1 GB/s

    private const int
        TestCount      =    10_000, // Roughtly 20+ minutes on an AzDO agent
        IterationCount =       128, // \_ One iteration is 128 MB
        IterationSize  = 1_048_576; // /  Dev workstation does about 8 iterations per second

    [Test]
    public async Task WriteAndHashRandomData([Range(1, TestCount)] int n)
    {
        // Sorry about your SSD endurance, but we are not sure whether it is
        // disk I/O, compute, or something else that exercises the bug.

        var path = Path.GetTempFileName();

        try
        {
            using var stream = new FileStream(
                path,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: IterationSize,
                FileOptions.Asynchronous
            );

            var random = new Random(Random.Shared.Next());
            var data   = new byte[IterationSize];

            for (var i = 0; i < IterationCount; i++)
            {
                random.NextBytes(data);
                await stream.WriteAsync(data, CancellationToken.None);
            }

            await stream.FlushAsync(CancellationToken.None);

            stream.Position = 0;

            var hash = await SHA256.HashDataAsync(stream, CancellationToken.None);

            Console.WriteLine($"Test {n:D2}: {Convert.ToHexString(hash).ToLowerInvariant()}");
        }
        finally
        {
            try { File.Delete(path); } catch { } // best effort
        }
    }
}
