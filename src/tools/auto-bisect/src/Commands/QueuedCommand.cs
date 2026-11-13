using System;
using System.Linq;
using System.Threading.Tasks;
using AutoBisect;

namespace AutoBisect.Commands;

internal static class QueuedCommand
{
    public static async Task HandleAsync(
        string org,
        string project,
        string pat,
        int definitionId,
        bool showAll
    )
    {
        using var client = new AzDoClient(org, project, pat);

        Console.WriteLine($"Fetching builds for definition {definitionId}...");
        Console.WriteLine();

        var activeBuilds = await client.GetActiveBuildsAsync(definitionId);

        if (activeBuilds.Count > 0)
        {
            Console.WriteLine($"Active builds ({activeBuilds.Count}):");
            Console.WriteLine();
            foreach (var build in activeBuilds.OrderByDescending(b => b.QueueTime))
            {
                BuildUtilities.PrintBuildInfo(build);
            }
        }
        else
        {
            Console.WriteLine("No active builds.");
            Console.WriteLine();
        }

        if (showAll)
        {
            var recentBuilds = await client.GetRecentBuildsAsync(definitionId, top: 10);
            if (recentBuilds.Count > 0)
            {
                Console.WriteLine($"Recent completed builds ({recentBuilds.Count}):");
                Console.WriteLine();
                foreach (var build in recentBuilds)
                {
                    BuildUtilities.PrintBuildInfo(build);
                }
            }
        }
    }
}
