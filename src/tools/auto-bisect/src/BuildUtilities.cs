using System;
using System.Threading;
using System.Threading.Tasks;
using AutoBisect;

namespace AutoBisect;

internal static class BuildUtilities
{
    public static void PrintBuildInfo(Build build)
    {
        var shortSha = build.SourceVersion?.Substring(0, Math.Min(12, build.SourceVersion?.Length ?? 0)) ?? "unknown";
        Console.WriteLine($"  Build {build.Id}:");
        Console.WriteLine($"    Status:   {build.Status}");
        Console.WriteLine($"    Result:   {build.Result}");
        Console.WriteLine($"    Commit:   {shortSha}");
        Console.WriteLine($"    Queued:   {build.QueueTime}");
        Console.WriteLine($"    Started:  {build.StartTime}");
        Console.WriteLine($"    Finished: {build.FinishTime}");
        Console.WriteLine($"    URL:      {build.Links?.Web?.Href}");
        Console.WriteLine();
    }

    public static async Task<Build?> WaitForBuildAsync(AzDoClient client, int buildId, int pollIntervalSeconds, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var build = await client.GetBuildAsync(buildId, cancellationToken);
            if (build == null)
            {
                return null;
            }

            if (build.Status == BuildStatus.Completed)
            {
                return build;
            }

            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"         Waiting for build... ({elapsed:hh\\:mm\\:ss} elapsed, status: {build.Status})");

            await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken);
        }
    }
}
