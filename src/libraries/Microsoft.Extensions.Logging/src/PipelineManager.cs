// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Logging
{
    internal sealed class PipelineManager
    {
        private readonly Dictionary<PipelineKey, object> _pipelines = new Dictionary<PipelineKey, object>();

        public LogEntryPipeline<TState>? GetPipeline<TState>(ILogEntryProcessor processor, ILogMetadata<TState>? metadata, object? userState)
        {
            object? pipeline;
            PipelineKey key = new PipelineKey((metadata == null) ? typeof(TState) : metadata, processor, userState);
            lock (_pipelines)
            {
                if (!_pipelines.TryGetValue(key, out pipeline))
                {
                    pipeline = BuildPipeline(processor, metadata, userState);
                    _pipelines[key] = pipeline;
                }
            }
            return (LogEntryPipeline<TState>?)pipeline;
        }

        private static LogEntryPipeline<TState> BuildPipeline<TState>(ILogEntryProcessor processor, ILogMetadata<TState>? metadata, object? userState)
        {
            LogEntryHandler<TState, EmptyEnrichmentPropertyValues> handler = processor.GetLogEntryHandler<TState, EmptyEnrichmentPropertyValues>(metadata, out bool enabled, out bool dynamicCheckRequired);
            return new LogEntryPipeline<TState>(handler, userState, enabled, dynamicCheckRequired);
        }
    }

    public readonly struct PipelineKey
    {
        public PipelineKey(object typeOrMetadata, ILogEntryProcessor? terminalProcessor, object? userState)
        {
            TypeOrMetadata = typeOrMetadata;
            TerminalProcessor = terminalProcessor;
            UserState = userState;
        }

        public object TypeOrMetadata { get; }
        public ILogEntryProcessor? TerminalProcessor { get; }
        public object? UserState { get; }
    }
}
