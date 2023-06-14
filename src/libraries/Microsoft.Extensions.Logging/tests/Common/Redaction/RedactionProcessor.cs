// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Prototype redaction processor uses APIs not available on .NET Framework.
// The prototype redaction could probably be updated to support .NET Framework if required.

#if NET8_0_OR_GREATER

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging.Tests.Redaction
{
    public static class RedactionExtensions
    {
        public static ILoggingBuilder AddRedactionProcessor(this ILoggingBuilder builder)
        {
            builder.AddProcessor((serviceProvider, processor) => new RedactionProcessor(processor, serviceProvider.GetService<IRedactorProvider>()));
            return builder;
        }
    }

    internal interface IRedactorProvider
    {
        IRedactor GetRedactor(DataClass dataClass);
    }

    internal interface IRedactor
    {
        string Redact(ReadOnlySpan<char> source);
        int Redact(ReadOnlySpan<char> source, Span<char> destination);
        int GetRedactedLength(ReadOnlySpan<char> source);
    }

    internal enum DataClass
    {
        Unknown,
        EUPI
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
    internal abstract class DataClassificationAttribute : Attribute
    {
        public string Notes { get; set; } = string.Empty;
        public DataClass DataClass { get; }

        protected DataClassificationAttribute(DataClass dataClass)
        {
            DataClass = dataClass;
        }
    }

    internal class RedactionProcessor : ILogEntryProcessor
    {
        private readonly ILogEntryProcessor _nextProcessor;
        private readonly IRedactorProvider? _redactorProvider;

        public RedactionProcessor(ILogEntryProcessor nextProcessor, IRedactorProvider? provider)
        {
            _nextProcessor = nextProcessor;
            _redactorProvider = provider;
        }

        public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicCheckRequired)
        {
            if (metadata != null && _redactorProvider != null)
            {
                PropertyRedaction[] redactions = GetPolicyRedactions(metadata);
                if (redactions.Length > 0)
                {
                    RedactedLogMetadata<TState> redactedMetadata = new RedactedLogMetadata<TState>(metadata, redactions);
                    return new RedactionHandler<TState>(redactedMetadata,
                        _nextProcessor.GetLogEntryHandler<RedactedValues<TState>>(redactedMetadata, out enabled, out dynamicCheckRequired));
                }
            }
            return _nextProcessor.GetLogEntryHandler<TState>(metadata, out enabled, out dynamicCheckRequired);
        }

        PropertyRedaction[] GetPolicyRedactions<TState>(ILogMetadata<TState> metadata)
        {
            List<PropertyRedaction> redactions = new List<PropertyRedaction>();
            for (int i = 0; i < metadata.PropertyCount; i++)
            {
                LogPropertyInfo propMetadata = metadata.GetPropertyInfo(i);
                if (propMetadata.Metadata == null)
                {
                    continue;
                }
                DataClassificationAttribute? dataClassAttr = propMetadata.Metadata.OfType<DataClassificationAttribute>().FirstOrDefault();
                if (dataClassAttr != null)
                {
                    redactions.Add(new PropertyRedaction(i, _redactorProvider!.GetRedactor(dataClassAttr.DataClass)));
                }
            }
            return redactions.ToArray();
        }

        public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled) where TState : notnull
        {
            return _nextProcessor.GetScopeHandler<TState>(metadata, out enabled);
        }

        public bool IsEnabled(LogLevel level) => _nextProcessor.IsEnabled(level);

        class RedactionHandler<TState> : LogEntryHandler<TState>
        {
            LogEntryHandler<RedactedValues<TState>> _nextHandler;
            RedactedLogMetadata<TState> _metadata;

            public RedactionHandler(RedactedLogMetadata<TState> metadata, LogEntryHandler<RedactedValues<TState>> nextHandler)
            {
                _metadata = metadata;
                _nextHandler = nextHandler;
            }
            public override void HandleLogEntry(ref LogEntry<TState> logEntry)
            {
                RedactedValues<TState> redactedValuesState = new RedactedValues<TState>(_metadata, logEntry.State);
                LogEntry<RedactedValues<TState>> copy = new LogEntry<RedactedValues<TState>>(logEntry.LogLevel, category: null!, logEntry.EventId, redactedValuesState, logEntry.Exception, RedactedValues<TState>.Callback);
                _nextHandler.HandleLogEntry(ref copy);
            }

            public override bool IsEnabled(LogLevel level)
            {
                return _nextHandler.IsEnabled(level);
            }
        }
    }

    internal struct PropertyRedaction
    {
        public PropertyRedaction(int index, IRedactor redactor)
        {
            Index = index;
            Redactor = redactor;
        }
        public int Index;
        public IRedactor Redactor;
    }

    internal class RedactedLogMetadata<T> : ILogMetadata<RedactedValues<T>>
    {
        private readonly ILogMetadata<T> _originalMetadata;
        private readonly PropertyRedaction[] _redactions;
        private Func<RedactedValues<T>, Exception?, string> _formatter;

        public RedactedLogMetadata(ILogMetadata<T> metadata, PropertyRedaction[] redactions)
        {
            _originalMetadata = metadata;
            _redactions = redactions;
        }

        public IRedactor? GetPropertyRedactor(int index)
        {
            for (var i = 0; i < _redactions.Length; i++)
            {
                if (_redactions[i].Index == index)
                {
                    return _redactions[i].Redactor;
                }
            }
            return null;
        }

        public ILogMetadata<T> OriginalMetadata => _originalMetadata;

        public LogLevel LogLevel => _originalMetadata.LogLevel;

        public EventId EventId => _originalMetadata.EventId;

        public string OriginalFormat => _originalMetadata.OriginalFormat;

        public int PropertyCount => _originalMetadata.PropertyCount;

        public LogPropertyInfo GetPropertyInfo(int index) => _originalMetadata.GetPropertyInfo(index);

        internal string FormatMessage(in RedactedValues<T> state)
        {
            if(_formatter == null)
            {
                // multiple threads could race to set this and overwrite one another
                // but it doesn't matter.
                _formatter = this.CreateStringMessageFormatter();
            }
            return _formatter(state, null);
        }

        class RedactedValuePropertyVisitorFactory<TCookie> : IPropertyVisitorFactory<TCookie>
        {
            const int MaxStackAllocChars = 256;

            RedactedLogMetadata<T> _metadata;
            IPropertyVisitorFactory<TCookie> _innerFactory;
            VisitPropertyAction<string, TCookie> _stringVisitor;
            VisitSpanPropertyAction<TCookie> _spanVisitor;

            public RedactedValuePropertyVisitorFactory(RedactedLogMetadata<T> metadata, IPropertyVisitorFactory<TCookie> innerFactory)
            {
                _metadata = metadata;
                _innerFactory = innerFactory;
                _stringVisitor = _innerFactory.GetPropertyVisitor<string>();
                _spanVisitor = _innerFactory.GetSpanPropertyVisitor();
            }

            public VisitPropertyAction<PropType, TCookie> GetPropertyVisitor<PropType>()
            {
                VisitPropertyAction<PropType, TCookie> unredactedVisit = _innerFactory.GetPropertyVisitor<PropType>();
                return Visit;

                void Visit(int propIndex, PropType value, ref Span<byte> spanCookie, ref TCookie cookie)
                {
                    IRedactor? redactor = _metadata.GetPropertyRedactor(propIndex);
                    if(redactor == null)
                    {
                        unredactedVisit(propIndex, value, ref spanCookie, ref cookie);
                        return;
                    }

                    Span<char> unredactedBuffer = stackalloc char[MaxStackAllocChars];
                    if (TryGetUnredactedBuffer(value, ref unredactedBuffer))
                    {
                        int len2 = redactor.GetRedactedLength(unredactedBuffer);
                        if (len2 <= MaxStackAllocChars)
                        {
                            Span<char> redactedBuffer2 = stackalloc char[len2];
                            redactor.Redact(unredactedBuffer, redactedBuffer2);
                            _spanVisitor(propIndex, redactedBuffer2, ref spanCookie, ref cookie);
                        }
                        else
                        {
                            string redactedValue = redactor.Redact(unredactedBuffer);
                            _stringVisitor(propIndex, redactedValue, ref spanCookie, ref cookie);
                        }
                        return;
                    }

                    string? unredactedValue = value.ToString();
                    int len = redactor.GetRedactedLength(unredactedValue);
                    if (len <= MaxStackAllocChars)
                    {
                        Span<char> redactedBuffer = stackalloc char[len];
                        redactor.Redact(unredactedValue, redactedBuffer);
                        _spanVisitor(propIndex, redactedBuffer, ref spanCookie, ref cookie);
                    }
                    else
                    {
                        string redactedValue = redactor.Redact(unredactedValue);
                        _stringVisitor(propIndex, redactedValue, ref spanCookie, ref cookie);
                    }
                }
            }

            private bool TryGetUnredactedBuffer<TProp>(TProp value, ref Span<char> unredactedBuffer)
            {
                if (value == null)
                {
                    unredactedBuffer = default;
                    return true;
                }
                else if (value is ISpanFormattable)
                {
                    if (((ISpanFormattable)value).TryFormat(unredactedBuffer, out int charsWritten2, null, null))
                    {
                        unredactedBuffer = unredactedBuffer.Slice(0, charsWritten2);
                        return true;
                    }
                }
                return false;
            }

            public VisitSpanPropertyAction<TCookie> GetSpanPropertyVisitor()
            {
                return Visit;

                void Visit(int propIndex, scoped ReadOnlySpan<char> value, ref Span<byte> spanCookie, ref TCookie cookie)
                {
                    IRedactor? redactor = _metadata.GetPropertyRedactor(propIndex);
                    if (redactor == null)
                    {
                        _spanVisitor(propIndex, value, ref spanCookie, ref cookie);
                        return;
                    }

                    int len = redactor.GetRedactedLength(value);
                    if (len <= MaxStackAllocChars)
                    {
                        Span<char> redactedBuffer2 = stackalloc char[len];
                        redactor.Redact(value, redactedBuffer2);
                        _spanVisitor(propIndex, redactedBuffer2, ref spanCookie, ref cookie);
                    }
                    else
                    {
                        string redactedValue = redactor.Redact(value);
                        _stringVisitor(propIndex, redactedValue, ref spanCookie, ref cookie);
                    }
                }
            }
        }

        public VisitPropertyListAction<RedactedValues<T>, TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> visitor)
        {
            VisitPropertyListAction<T,TCookie> innerListVisitor = _originalMetadata.CreatePropertyListVisitor(new RedactedValuePropertyVisitorFactory<TCookie>(this, visitor));
            return VisitPropertyList;

            void VisitPropertyList(ref RedactedValues<T> tState, ref Span<byte> spanCookie, ref TCookie cookie)
            {
                innerListVisitor(ref tState.OriginalState, ref spanCookie, ref cookie);
            }
        }
    }

    internal struct RedactedValues<T> : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public RedactedValues(RedactedLogMetadata<T> metadata, in T originalState)
        {
            Metadata = metadata;
            OriginalState = originalState;
        }

        public readonly RedactedLogMetadata<T> Metadata;
        public T OriginalState;

        public override string ToString() => Metadata.FormatMessage(this);

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            var nested = OriginalState as IReadOnlyList<KeyValuePair<string, object?>>;
            if (nested == null)
            {
                yield break;
            }
            for (var i = 0; i < nested.Count; i++)
            {
                yield return GetRedactedValue(i, nested[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count
        {
            get
            {
                var nested = OriginalState as IReadOnlyList<KeyValuePair<string, object?>>;
                return nested?.Count ?? 0;
            }
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                var nested = OriginalState as IReadOnlyList<KeyValuePair<string, object?>>;
                if (nested == null)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }
                var originalValue = nested[index];
                return GetRedactedValue(index, originalValue);
            }
        }

        private readonly KeyValuePair<string, object?> GetRedactedValue(int index, KeyValuePair<string, object?> originalValue)
        {
            var redactor = Metadata.GetPropertyRedactor(index);
            if (redactor == null)
            {
                return originalValue;
            }
            string? unredactedValue = originalValue.Value?.ToString();
            return new KeyValuePair<string, object?>(originalValue.Key, redactor.Redact(unredactedValue));
        }

        public static readonly Func<RedactedValues<T>, Exception?, string> Callback = (state, e) => state.ToString();
    }
}

#endif
