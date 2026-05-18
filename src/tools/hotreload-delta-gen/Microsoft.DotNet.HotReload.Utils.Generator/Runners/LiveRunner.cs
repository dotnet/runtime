// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.HotReload.Utils.Generator.Runners;

/// Generate deltas by watching for changes to the source files of the project
internal class LiveRunner : Runner {
    public LiveRunner (Config config) : base (config) { }

    protected override Task PrepareToRun (CancellationToken ct = default) => Task.CompletedTask;

    private protected override IAsyncEnumerable<Delta> SetupDeltas (BaselineArtifacts baselineArtifacts, CancellationToken ct = default)
    {
        return Livecoding (baselineArtifacts, config.LiveCodingWatchDir, config.LiveCodingWatchPattern, ct);
    }

    protected override bool PrepareCapabilitiesCore (out EnC.EditAndContinueCapabilities caps) {
        caps = EnC.EditAndContinueCapabilities.None;
        return false;
    }
    private static async IAsyncEnumerable<Delta> Livecoding (BaselineArtifacts baselineArtifacts, string watchDir, string pattern, [EnumeratorCancellation] CancellationToken cancellationToken= default) {
        var last = DateTime.UtcNow;
        var interval = TimeSpan.FromMilliseconds(250); /* FIXME: make this configurable */
        var docResolver = baselineArtifacts.DocResolver;
        var baselineProjectId = baselineArtifacts.BaselineProjectId;

        using var fswgen = new Util.FSWGen (watchDir, pattern);
        await foreach (var fsevent in fswgen.Watch(cancellationToken).ConfigureAwait(false)) {
            if ((fsevent.ChangeType & WatcherChangeTypes.Changed) != 0) {
                var e = DateTime.UtcNow;
                Console.WriteLine($"change in {fsevent.FullPath} is a {fsevent.ChangeType} at {e}");
                if (e - last < interval) {
                    Console.WriteLine($"too soon {e-last}");
                    continue;
                }
                Console.WriteLine($"more than 250ms since last change");
                last = e;
                var fp = fsevent.FullPath;
                if (!docResolver.TryResolveDocumentId(fp, out var id)) {
                    Console.WriteLine ($"ignoring change in {fp} which is not in {baselineProjectId}");
                    continue;
                }

                yield return new Delta (Plan.Change.Create(id, fp));
            }
        }
    }


}
