// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Globalization;
using System.IO;

namespace ILLink.CacheTool;

internal static class Program
{
    private static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var command = new RootCommand("Experimental ILLink task cache maintenance. Commands and cache formats may change without notice.");
        var directory = new Option<DirectoryInfo>("--cache-directory")
        {
            Description = "Cache root directory. No builds may use this cache while purging.",
            Required = true
        }.AcceptExistingOnly();
        var before = new Option<DateTimeOffset>("--before")
        {
            Description = "Delete entries last used before this UTC ISO 8601 timestamp (Z or +00:00).",
            Required = true,
            CustomParser = result =>
            {
                string value = result.Tokens[0].Value;
                if (DateTimeOffset.TryParseExact(value,
                    new[] { "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz" },
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset cutoff) &&
                    cutoff.Offset == TimeSpan.Zero)
                {
                    return cutoff;
                }

                result.AddError("--before must be a UTC ISO 8601 timestamp ending in Z or +00:00.");
                return default;
            }
        };
        var purge = new Command("purge", "Delete unused entries from an idle ILLink task cache.");
        purge.Options.Add(directory);
        purge.Options.Add(before);
        purge.SetAction(result => CachePurge.Run(result.GetValue(directory)!.FullName, result.GetValue(before), output, error));
        command.Subcommands.Add(purge);
        return command.Parse(args).Invoke(new()
        {
            EnableDefaultExceptionHandler = false,
            Output = output,
            Error = error
        });
    }
}
