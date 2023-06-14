// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        public LogPropertyInfo(string name, object[]? metadata = null)
        {
            Name = name;
            Metadata = metadata;
        }
        public string Name { get; }
        public object[]? Metadata { get; internal set; }
    }

    public interface ILogMetadata<TState>
    {
        LogLevel LogLevel { get; }
        EventId EventId { get; }
        string OriginalFormat { get; }
        int PropertyCount { get; }
        LogPropertyInfo GetPropertyInfo(int index);
        VisitPropertyListAction<TState, TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> propertyVisitorFactory);
    }

    public delegate void VisitPropertyListAction<TState, TCookie>(ref TState state, ref Span<byte> spanCookie, ref TCookie cookie);
    public delegate void VisitPropertyAction<PropType, TCookie>(int propIndex, PropType propValue, ref Span<byte> spanCookie, ref TCookie cookie);
    public delegate void VisitSpanPropertyAction<TCookie>(int propIndex, scoped ReadOnlySpan<char> propValue, ref Span<byte> spanCookie, ref TCookie cookie);

    public interface IPropertyVisitorFactory<TCookie>
    {
        VisitPropertyAction<PropType, TCookie> GetPropertyVisitor<PropType>();
        VisitSpanPropertyAction<TCookie> GetSpanPropertyVisitor();
    }
}
