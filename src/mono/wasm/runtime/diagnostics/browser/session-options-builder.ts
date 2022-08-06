// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { EventPipeSessionOptions } from "../../types";

export const eventLevel = {
    LogAlways: 0,
    Critical: 1,
    Error: 2,
    Warning: 3,
    Informational: 4,
    Verbose: 5,
} as const;

export type EventLevel = typeof eventLevel;

type UnnamedProviderConfiguration = Partial<{
    keywordMask: string | 0;
    level: number;
    args: string;
}>

/// The configuration for an individual provider.  Each provider configuration has the name of the provider,
/// the level of events to collect, and a string containing a 32-bit hexadecimal mask (without an "0x" prefix) of
/// the "keywords" to filter a subset of the events. The keyword mask may be the number 0 or "" to skips the filtering.
/// See https://docs.microsoft.com/en-us/dotnet/core/diagnostics/well-known-event-providers for a list of known providers.
/// Additional providers may be added by applications or libraries that implement an EventSource subclass.
/// See https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource?view=net-6.0
///
/// Some providers also have an "args" string in an arbitrary format.  For example the EventSource providers that
/// include EventCounters have a "EventCounterIntervalSec=NNN" argument that specified how often the counters of
/// the event source should be polled.
export interface ProviderConfiguration extends UnnamedProviderConfiguration {
    name: string;
}

const runtimeProviderName = "Microsoft-Windows-DotNETRuntime";
const runtimePrivateProviderName = "Microsoft-Windows-DotNETRuntimePrivate";
const sampleProfilerProviderName = "Microsoft-DotNETCore-SampleProfiler";

const runtimeProviderDefault: ProviderConfiguration = {
    name: runtimeProviderName,
    keywordMask: "4c14fccbd",
    level: eventLevel.Verbose,
};

const runtimePrivateProviderDefault: ProviderConfiguration = {
    name: runtimePrivateProviderName,
    keywordMask: "4002000b",
    level: eventLevel.Verbose,
};

const sampleProfilerProviderDefault: ProviderConfiguration = {
    name: sampleProfilerProviderName,
    keywordMask: "0",
    level: eventLevel.Verbose,
};

/// A helper class to create EventPipeSessionOptions
export class SessionOptionsBuilder {
    private _rundown?: boolean;
    private _providers: ProviderConfiguration[];
    /// Create an empty builder. Prefer to use SessionOptionsBuilder.Empty
    constructor() {
        this._providers = [];
    }
    /// Gets a builder with no providers.
    static get Empty(): SessionOptionsBuilder { return new SessionOptionsBuilder(); }
    /// Gets a builder with default providers and rundown events enabled.
    /// See https://docs.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables
    static get DefaultProviders(): SessionOptionsBuilder {
        return this.Empty.addRuntimeProvider().addRuntimePrivateProvider().addSampleProfilerProvider();
    }
    /// Change whether to collect rundown events.
    /// Certain providers may need rundown events to be collected in order to provide useful diagnostic information.
    setRundownEnabled(enabled: boolean): SessionOptionsBuilder {
        this._rundown = enabled;
        return this;
    }
    /// Add a provider configuration to the builder.
    addProvider(provider: ProviderConfiguration): SessionOptionsBuilder {
        this._providers.push(provider);
        return this;
    }
    /// Add the Microsoft-Windows-DotNETRuntime provider.  Use override options to change the event level or keyword mask.
    /// The default is { keywordMask: "4c14fccbd", level: eventLevel.Verbose }
    addRuntimeProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder {
        const options = { ...runtimeProviderDefault, ...overrideOptions };
        this._providers.push(options);
        return this;
    }
    /// Add the Microsoft-Windows-DotNETRuntimePrivate provider. Use override options to change the event level or keyword mask.
    /// The default is { keywordMask: "4002000b", level: eventLevel.Verbose}
    addRuntimePrivateProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder {
        const options = { ...runtimePrivateProviderDefault, ...overrideOptions };
        this._providers.push(options);
        return this;
    }
    /// Add the Microsoft-DotNETCore-SampleProfiler. Use override options to change the event level or keyword mask.
    // The default is { keywordMask: 0, level: eventLevel.Verbose }
    addSampleProfilerProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder {
        const options = { ...sampleProfilerProviderDefault, ...overrideOptions };
        this._providers.push(options);
        return this;
    }
    /// Create an EventPipeSessionOptions from the builder.
    build(): EventPipeSessionOptions {
        const providers = this._providers.map(p => {
            const name = p.name;
            const keywordMask = "" + (p?.keywordMask ?? "");
            const level = p?.level ?? eventLevel.Verbose;
            const args = p?.args ?? "";
            const maybeArgs = args != "" ? `:${args}` : "";
            return `${name}:${keywordMask}:${level}${maybeArgs}`;
        });
        return {
            collectRundownEvents: this._rundown,
            providers: providers.join(",")
        };
    }
}

