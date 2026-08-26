// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload.Utils.Generator;

public abstract class Runner {
    readonly protected Config config;

    protected Runner(Config config)
    {
        this.config = config;
    }

    public static Runner Make (Config config)
    {
        if (config.Live)
            return new Runners.LiveRunner (config);
        else
            return new Runners.ScriptRunner (config);
    }

    public async Task Run (CancellationToken ct = default) {
        await PrepareToRun(ct);
        var capabilities = PrepareCapabilities();
        var baselineArtifacts = await SetupBaseline (capabilities, ct);

        var deltaProject = new DeltaProject (baselineArtifacts);
        var derivedInputs = SetupDeltas (baselineArtifacts, ct);

        await GenerateDeltas (deltaProject, derivedInputs, makeOutputs: MakeOutputs, outputsReady: OutputsReady, ct: ct);
        // FIXME: do something for LiveRunner
        if (OutputsDone != null)
            await OutputsDone (ct);
    }

    /// Delegate that is called to create the delta output streams.
    /// If not set, a default is used that writes the deltas to files.
    protected  Func<DeltaNaming,DeltaOutputStreams>? MakeOutputs {get; set; } = null;

    /// Delegate that is called after the outputs have been emitted.
    /// If not set, a default is used that does nothing.
    protected  Action<DeltaNaming,DeltaOutputStreams>? OutputsReady {get; set; } = null;

    /// Called when all the outputs have been emitted.
    protected Func<CancellationToken,Task>? OutputsDone {get; set;} = null;

    private async Task<BaselineArtifacts> SetupBaseline (EnC.EditAndContinueCapabilities capabilities, CancellationToken ct = default) {
        BaselineProject baselineProject = await BaselineProject.Make (config, capabilities, ct);

        var baselineArtifacts = await baselineProject.PrepareBaseline(ct);

        Console.WriteLine ("baseline ready");
        return baselineArtifacts;
    }

    /// Called just before we start generating deltas.
    protected abstract Task PrepareToRun(CancellationToken ct = default);

    /// Returns true if the runner has capabilities for the project, or false to use the config defaults.
    protected abstract bool PrepareCapabilitiesCore (out EnC.EditAndContinueCapabilities capabilities);

    protected EnC.EditAndContinueCapabilities PrepareCapabilities() {
        EnC.EditAndContinueCapabilities configCaps = EnC.EditAndContinueCapabilities.None;
        (var configuredCaps, var unknowns) = EditAndContinueCapabilitiesParser.Parse (config.EditAndContinueCapabilities);
        foreach (var c in configuredCaps) {
            configCaps |= c;
        }
        bool projectHasCaps = PrepareCapabilitiesCore (out var runnerCaps);
        var totalCaps = configCaps | runnerCaps;
        // If the project explicitly sets no capabilities, use None
        if (totalCaps == EnC.EditAndContinueCapabilities.None && !projectHasCaps)
            totalCaps = DefaultCapabilities ();
        if (!config.NoWarnUnknownCapabilities) {
            foreach (var unk in unknowns) {
                Console.WriteLine ("Unknown EnC capability '{0}', ignored.", unk);
            }
        }
        return totalCaps;
    }

    protected EnC.EditAndContinueCapabilities DefaultCapabilities ()
    {
        var allCaps = EnC.EditAndContinueCapabilities.Baseline
            | EnC.EditAndContinueCapabilities.AddMethodToExistingType
            | EnC.EditAndContinueCapabilities.AddStaticFieldToExistingType
            | EnC.EditAndContinueCapabilities.AddInstanceFieldToExistingType
            | EnC.EditAndContinueCapabilities.NewTypeDefinition
            | EnC.EditAndContinueCapabilities.ChangeCustomAttributes
            | EnC.EditAndContinueCapabilities.UpdateParameters
            | EnC.EditAndContinueCapabilities.GenericAddMethodToExistingType
            | EnC.EditAndContinueCapabilities.GenericUpdateMethod
            | EnC.EditAndContinueCapabilities.GenericAddFieldToExistingType
            | EnC.EditAndContinueCapabilities.AddExplicitInterfaceImplementation
            | EnC.EditAndContinueCapabilities.AddFieldRva
            ;
        return allCaps;
    }

    private protected abstract IAsyncEnumerable<Delta> SetupDeltas (BaselineArtifacts baselineArtifacts, CancellationToken ct = default);

    private async Task GenerateDeltas (DeltaProject deltaProject, IAsyncEnumerable<Delta> deltas,
                                        Func<DeltaNaming,DeltaOutputStreams>? makeOutputs = null,
                                        Action<DeltaNaming, DeltaOutputStreams>? outputsReady = null,
                                        CancellationToken ct =  default)
    {
        await foreach (var delta in deltas.WithCancellation(ct)) {
            Console.WriteLine ("got a change");
            /* fixme: why does FSW sometimes queue up 2 events in quick succession after a single save? */
            deltaProject = await deltaProject.BuildDelta (delta, ignoreUnchanged: config.Live, makeOutputs: makeOutputs, outputsReady: outputsReady, ct: ct);
        }
    }

}

