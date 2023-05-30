// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;

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
        LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired);
        ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled) where TState : notnull;
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

    public abstract class LogEntryHandler<TState>
    {
        public abstract bool IsEnabled(LogLevel level);
        public abstract void HandleLogEntry(ref LogEntry<TState> logEntry);
    }

    public abstract class ScopeHandler<TState> where TState : notnull
    {
        public abstract bool IsEnabled(LogLevel level);
        public abstract IDisposable? HandleBeginScope(ref TState state);
    }

    public struct LogPropertyInfo
    {
        public LogPropertyInfo(string name, object[]? metadata)
        {
            Name = name;
            Metadata = metadata;
        }
        public string Name { get; }
        public object[]? Metadata { get; }
    }

    public interface ILogMetadata<TState>
    {
        LogLevel LogLevel { get; }
        EventId EventId { get; }
        string OriginalFormat { get; }
        int PropertyCount { get; }
        LogPropertyInfo GetPropertyInfo(int index);
        void AppendFormattedMessage(in TState state, IBufferWriter<char> buffer);
        Action<TState, IBufferWriter<char>> GetMessageFormatter(PropertyCustomFormatter[] customFormatters);
        FormatPropertyListAction<TState> GetPropertyListFormatter(IPropertyFormatterFactory propertyFormatterFactory);
        Func<TState, Exception?, string> GetStringMessageFormatter();
    }

    public delegate void FormatPropertyListAction<TState>(in TState state, ref BufferWriter<byte> bufferWriter);
    public delegate void FormatPropertyAction<PropType>(PropType propertyValue, ref BufferWriter<byte> bufferWriter);
    public delegate void FormatSpanPropertyAction(scoped ReadOnlySpan<char> propertyValue, ref BufferWriter<byte> bufferWriter);

    public interface IPropertyFormatterFactory
    {
        FormatPropertyAction<PropType> GetPropertyFormatter<PropType>(int propertyIndex, LogPropertyInfo metadata);
        FormatSpanPropertyAction GetSpanPropertyFormatter(int propertyIndex, LogPropertyInfo metadata);
    }

    public abstract class PropertyCustomFormatter
    {
        //TODO: we can expand this with overrides for other commonly logged value types
        public virtual void AppendFormatted(int index, ReadOnlySpan<char> value, IBufferWriter<char> buffer) => AppendFormatted(index, value.ToString(), buffer);
        public virtual void AppendFormatted(int index, int value, IBufferWriter<char> buffer) => AppendFormatted<int>(index, value, buffer);
        public virtual void AppendFormatted(int index, string value, IBufferWriter<char> buffer) => AppendFormatted<string>(index, value, buffer);
        public abstract void AppendFormatted<T>(int index, T value, IBufferWriter<char> buffer);
    }
}
