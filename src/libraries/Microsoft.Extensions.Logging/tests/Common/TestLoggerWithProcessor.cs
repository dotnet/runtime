// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Extensions.Logging.Tests
{
    internal class TestLoggerWithProcessor : ILogger, ILogEntryProcessorFactory
    {
        public TestLoggerWithProcessor(ILogEntryProcessor processor)
        {
            CurrentProcessor = processor;
        }
        public ILogEntryProcessor CurrentProcessor { get; set; }


        public ProcessorContext GetProcessor() => new ProcessorContext(CurrentProcessor, CancellationToken.None);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            throw new InvalidOperationException("Expected test to use processor APIs");
        
        public bool IsEnabled(LogLevel logLevel) =>
            throw new InvalidOperationException("Expected test to use processor APIs");
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            throw new InvalidOperationException("Expected test to use processor APIs");
    }

    class RecordingProcessor : ILogEntryProcessor
    {
        public string MetadataOriginalFormat {  get; set; }
        public List<LogPropertyInfo> MetadataLogProperties { get; set; }
        public object State { get; set; }

        public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            RecordMetadata(metadata);
            enabled = true;
            dynamicEnabledCheckRequired = false;
            return new RecordingLogEntryHandler<TState>(this);
        }

        public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled) where TState : notnull
        {
            RecordMetadata(metadata);
            enabled = true;
            return new RecordingScopeHandler<TState>(this);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        private void RecordMetadata<TState>(ILogMetadata<TState> metadata)
        {
            MetadataOriginalFormat = metadata.OriginalFormat;
            MetadataLogProperties = Enumerable.Range(0, metadata.PropertyCount).Select(metadata.GetPropertyInfo).ToList();
        }
    }

    internal class RecordingLogEntryHandler<TState> : LogEntryHandler<TState>
    {
        RecordingProcessor _processor;
        public RecordingLogEntryHandler(RecordingProcessor processor)
        {
            _processor = processor;
        }
        public override void HandleLogEntry(ref LogEntry<TState> logEntry)
        {
            _processor.State = logEntry.State;
        }
        public override bool IsEnabled(LogLevel level) => true;
    }

    internal class RecordingScopeHandler<TState> : ScopeHandler<TState>
    {
        RecordingProcessor _processor;
        public RecordingScopeHandler(RecordingProcessor processor)
        {
            _processor = processor;
        }
        public override IDisposable? HandleBeginScope(ref TState state)
        {
            _processor.State = state;
            return null;
        }
        public override bool IsEnabled(LogLevel level) => true;
    }

}
