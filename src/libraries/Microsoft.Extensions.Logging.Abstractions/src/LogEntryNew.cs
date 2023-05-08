// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Threading;

namespace Microsoft.Extensions.Logging
{
    public interface ILogEntryProcessorFactory
    {
        //TODO: should CancellationToken be an IChangeToken or something else?
        ProcessorContext GetProcessor();
    }

    public readonly struct ProcessorContext
    {
        public ILogEntryProcessor Processor { get; }
        public CancellationToken CancellationToken { get; }

        public ProcessorContext(ILogEntryProcessor processor, CancellationToken cancellationToken)
        {
            Processor = processor;
            CancellationToken = cancellationToken;
        }
    }

    public interface ILogEntryProcessor
    {
        LogEntryHandler<TState, TEnrichmentProperties> GetLogEntryHandler<TState, TEnrichmentProperties>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired);
        bool IsEnabled(LogLevel logLevel);
    }

    //TODO: Merge with other factory? Handle cancellation?
    //Are these factories even needed or could we do it with a Func<ILogEntryProcessor,ILogEntryProcessor>?
    public interface IProcessorFactory
    {
        ILogEntryProcessor GetProcessor(ILogEntryProcessor nextProcessor);
    }

    public class ProcessorFactory<T> : IProcessorFactory where T : ILogEntryProcessor
    {
        public ProcessorFactory(Func<ILogEntryProcessor, T> getProcessor)
        {
            _getProcessor = getProcessor;
        }

        private readonly Func<ILogEntryProcessor, T> _getProcessor;
        public ILogEntryProcessor GetProcessor(ILogEntryProcessor nextProcessor) => _getProcessor(nextProcessor);
    }

    public abstract class LogEntryHandler<TState, TEnrichmentProperties>
    {
        public abstract bool IsEnabled(LogLevel level);
        public abstract void HandleLogEntry(ref LogEntry<TState, TEnrichmentProperties> logEntry);
    }

    //TODO: Not sure if we need to keep this?
    public interface ILoggerStateWithMetadata<TState>
    {
        public ILogMetadata<TState> Metadata { get; }
    }

    public struct LogPropertyMetadata
    {
        public LogPropertyMetadata(string name, string? formatSpecifier, Attribute[]? attributes)
        {
            Name = name;
            FormatSpecifier = formatSpecifier;
            Attributes = attributes;
        }
        public string Name { get; }
        public string? FormatSpecifier { get; }
        public Attribute[]? Attributes { get; }
    }

    public interface ILogMetadata<TState>
    {
        LogLevel LogLevel { get; }
        EventId EventId { get; }
        string OriginalFormat { get; }
        int PropertyCount { get; }
        LogPropertyMetadata GetPropertyMetadata(int index);
        void AppendFormattedMessage(in TState state, IBufferWriter<char> buffer);
        Action<TState, IBufferWriter<char>> GetMessageFormatter(PropertyCustomFormatter[] customFormatters);
        FormatPropertyListAction<TState> GetPropertyListFormatter(IPropertyFormatterFactory propertyFormatterFactory);
        Func<TState, Exception?, string> GetStringMessageFormatter();
    }

    public delegate void FormatPropertyListAction<TState>(ref TState state, ref BufferWriter<byte> bufferWriter);
    public delegate void FormatPropertyAction<PropType>(PropType propertyValue, ref BufferWriter<byte> bufferWriter);
    public delegate void FormatSpanPropertyAction(scoped ReadOnlySpan<char> propertyValue, ref BufferWriter<byte> bufferWriter);

    public interface IPropertyFormatterFactory
    {
        FormatPropertyAction<PropType> GetPropertyFormatter<PropType>(int propertyIndex, LogPropertyMetadata metadata);
        FormatSpanPropertyAction GetSpanPropertyFormatter(int propertyIndex, LogPropertyMetadata metadata);
    }

    public abstract class PropertyCustomFormatter
    {
        //TODO: we can expand this with overrides for other commonly logged value types
        public virtual void AppendFormatted(int index, ReadOnlySpan<char> value, IBufferWriter<char> buffer) => AppendFormatted(index, value.ToString(), buffer);
        public virtual void AppendFormatted(int index, int value, IBufferWriter<char> buffer) => AppendFormatted<int>(index, value, buffer);
        public virtual void AppendFormatted(int index, string value, IBufferWriter<char> buffer) => AppendFormatted<string>(index, value, buffer);
        public abstract void AppendFormatted<T>(int index, T value, IBufferWriter<char> buffer);
    }

    public readonly ref struct LogEntry<TState, TEnrichmentProperties>
    {
#if NET8_0_OR_GREATER
        private readonly ref TState _state;
        private readonly ref TEnrichmentProperties _enrichmentProperties;
#else
        private readonly ByReference<TState> _state;
        private readonly ByReference<TEnrichmentProperties> _enrichmentProperties;
#endif

        public LogEntry(LogLevel level, EventId eventId, ref TState state, ref TEnrichmentProperties enrichmentProperties, Exception? exception, Func<TState, Exception?, string>? formatter)
        {
            LogLevel = level;
            EventId = eventId;
#if NET8_0_OR_GREATER
            _state = ref state;
            _enrichmentProperties = ref enrichmentProperties;
#else
            _state = new ByReference<TState>(ref state);
            _enrichmentProperties = new ByReference<TEnrichmentProperties>(ref enrichmentProperties);
#endif
            Exception = exception;
            Formatter = formatter;
        }

#if NET8_0_OR_GREATER
        public ref TState State => ref _state;
        public ref TEnrichmentProperties EnrichmentProperties => ref _enrichmentProperties;
#else
        public ref TState State => ref _state.Value;
        public ref TEnrichmentProperties EnrichmentProperties => ref _enrichmentProperties.Value;
#endif
        public LogLevel LogLevel { get; }
        public EventId EventId { get; }
        public Exception? Exception { get; }
        public Func<TState, Exception?, string>? Formatter { get; }
    }
}
