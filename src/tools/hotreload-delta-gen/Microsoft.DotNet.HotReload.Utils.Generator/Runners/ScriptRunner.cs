// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.HotReload.Utils.Generator.Runners;

/// Generate deltas by reading a script from a configuration file
/// listing the changed versions of the project source files.
internal class ScriptRunner : Runner {

    Script.ParsedScript? parsedScript;
    public ScriptRunner (Config config) : base (config) {
        if (!string.IsNullOrEmpty(config.OutputSummaryPath)) {
            var writer = new JsonSummaryWriter(config.OutputSummaryPath);
            OutputsReady = writer.OutputsReady;
            OutputsDone = writer.OutputsDone;
        }
    }

    private class JsonSummaryWriter {
        private string OutputPath {get; }
        private readonly List<OutputSummary.Delta> deltas;
        public JsonSummaryWriter(string outputPath) {
            OutputPath = outputPath;
            deltas = new List<OutputSummary.Delta>();
        }
        internal void OutputsReady(DeltaNaming names, DeltaOutputStreams _streams) {
            // FIXME: propagate the name of the updated assembly
            deltas.Add(new OutputSummary.Delta("", names.Dmeta, names.Dil, names.Dpdb));
        }

        internal async Task OutputsDone(CancellationToken ct = default) {
            using var s = File.OpenWrite(OutputPath);
            var summary = new OutputSummary.OutputSummary(deltas.ToArray());
            await System.Text.Json.JsonSerializer.SerializeAsync(s, summary, cancellationToken: ct);
        }

    }

    protected override async Task PrepareToRun(CancellationToken ct = default)
    {
        var scriptPath = config.ScriptPath;
        var parser = new Microsoft.DotNet.HotReload.Utils.Generator.Script.Json.Parser(scriptPath);
        Script.ParsedScript parsed;
        using (var scriptStream = new FileStream(scriptPath, FileMode.Open)) {
            parsed = await parser.ReadAsync (scriptStream, ct);
        }
        parsedScript = parsed;
    }

    protected override bool PrepareCapabilitiesCore (out EnC.EditAndContinueCapabilities capabilities) {
        capabilities = EnC.EditAndContinueCapabilities.None;
        if (parsedScript == null || parsedScript.Capabilities == null)
            return false;
        capabilities = parsedScript.Capabilities.Value;
        if (!config.NoWarnUnknownCapabilities) {
            foreach (var unk in parsedScript.UnknownCapabilities) {
                Console.WriteLine ($"Unknown EnC capability '{unk}' in '{config.ScriptPath}', ignored.");
            }
        }
        return true;
    }

    private protected override IAsyncEnumerable<Delta> SetupDeltas (BaselineArtifacts baselineArtifacts, CancellationToken ct = default)
    {
        if (parsedScript == null)
            return Util.AsyncEnumerableExtras.Empty<Delta>();
        return ScriptedPlanInputs (parsedScript, baselineArtifacts, ct);
    }

    private static async IAsyncEnumerable<Delta> ScriptedPlanInputs (Script.ParsedScript parsedScript, BaselineArtifacts baselineArtifacts, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask; // to make compiler happy
        var resolver = baselineArtifacts.DocResolver;
        var artifacts = parsedScript.Changes.Select(c => new Delta(Plan.Change.Create(ResolveForScript(resolver, c.Document), c.Update)));
        foreach (var a in artifacts) {
            yield return a;
            if (ct.IsCancellationRequested)
                break;
        }
    }
    private static DocumentId ResolveForScript (DocResolver resolver, string relativePath) {
        if (resolver.TryResolveDocumentId(relativePath, out var id))
            return id;
        throw new DiffyException($"Could not find {relativePath} in {resolver.Project.Name}", exitStatus: 12);
    }

}
