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

        public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired) where TState : notnull
        {
            return _nextProcessor.GetScopeHandler<TState>(metadata, out enabled, out dynamicEnabledCheckRequired);
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


    internal class RedactedPropertyFormatter : PropertyCustomFormatter
    {
        const int MaxStackAllocChars = 256;
        IRedactor _redactor;

        public RedactedPropertyFormatter(IRedactor redactor)
        {
            _redactor = redactor;
        }

        public override void AppendFormatted(int index, string value, IBufferWriter<char> buffer)
        {
            int len = _redactor.GetRedactedLength(value);
            if (len != 0)
            {
                Span<char> redactedBuffer = buffer.GetSpan(len);
                _redactor.Redact(value, redactedBuffer);
                buffer.Advance(len);
            }
        }

        public override void AppendFormatted<T>(int index, T value, IBufferWriter<char> buffer)
        {
            if (value == null)
            {
                return;
            }
            if (value is ISpanFormattable)
            {
                Span<char> unredactedBuffer = stackalloc char[MaxStackAllocChars];
                if (((ISpanFormattable)value).TryFormat(unredactedBuffer, out int charsWritten2, null, null))
                {
                    unredactedBuffer = unredactedBuffer.Slice(0, charsWritten2);
                    int len2 = _redactor.GetRedactedLength(unredactedBuffer);
                    if (len2 != 0)
                    {
                        Span<char> redactedBuffer2 = buffer.GetSpan(len2);
                        _redactor.Redact(unredactedBuffer, redactedBuffer2);
                        buffer.Advance(len2);
                    }
                    return;
                }
            }
            string? unredactedValue = value.ToString();
            int len = _redactor.GetRedactedLength(unredactedValue);
            if (len != 0)
            {
                Span<char> redactedBuffer = buffer.GetSpan(len);
                _redactor.Redact(unredactedValue, redactedBuffer);
                buffer.Advance(len);
            }
        }
    }

    internal class ChainedRedactedPropertyFormatter : PropertyCustomFormatter
    {
        const int MaxStackAllocChars = 256;
        IRedactor _redactor;
        PropertyCustomFormatter _nextFormatter;

        public ChainedRedactedPropertyFormatter(IRedactor redactor, PropertyCustomFormatter nextFormatter)
        {
            _redactor = redactor;
            _nextFormatter = nextFormatter;
        }

        public override void AppendFormatted(int index, string value, IBufferWriter<char> buffer)
        {
            int len = _redactor.GetRedactedLength(value);
            if (len <= MaxStackAllocChars)
            {
                Span<char> redactedBuffer = stackalloc char[len];
                _redactor.Redact(value, redactedBuffer);
                _nextFormatter.AppendFormatted(index, redactedBuffer, buffer);
            }
            else
            {
                string redactedValue = _redactor.Redact(value);
                _nextFormatter.AppendFormatted(index, redactedValue, buffer);
            }
        }

        public override void AppendFormatted<T>(int index, T value, IBufferWriter<char> buffer)
        {
            if (value == null)
            {
                return;
            }
            else if (value is ISpanFormattable)
            {
                Span<char> unredactedBuffer = stackalloc char[MaxStackAllocChars];
                if (((ISpanFormattable)value).TryFormat(unredactedBuffer, out int charsWritten2, null, null))
                {
                    unredactedBuffer = unredactedBuffer.Slice(0, charsWritten2);
                    int len2 = _redactor.GetRedactedLength(unredactedBuffer);
                    if (len2 <= MaxStackAllocChars)
                    {
                        Span<char> redactedBuffer2 = stackalloc char[len2];
                        _redactor.Redact(unredactedBuffer, redactedBuffer2);
                        _nextFormatter.AppendFormatted(index, redactedBuffer2, buffer);
                    }
                    else
                    {
                        string redactedValue = _redactor.Redact(unredactedBuffer);
                        _nextFormatter.AppendFormatted(index, redactedValue, buffer);
                    }
                    return;
                }
            }
            string? unredactedValue = value.ToString();
            int len = _redactor.GetRedactedLength(unredactedValue);
            if (len <= MaxStackAllocChars)
            {
                Span<char> redactedBuffer = stackalloc char[len];
                _redactor.Redact(unredactedValue, redactedBuffer);
                _nextFormatter.AppendFormatted(index, redactedBuffer, buffer);
            }
            else
            {
                string redactedValue = _redactor.Redact(unredactedValue);
                _nextFormatter.AppendFormatted(index, redactedValue, buffer);
            }
        }
    }

    internal class RedactedLogMetadata<T> : ILogMetadata<RedactedValues<T>>
    {
        private readonly ILogMetadata<T> _originalMetadata;
        private readonly PropertyRedaction[] _redactions;
        private Action<T, IBufferWriter<char>>? _defaultFormatter;

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

        public void AppendFormattedMessage(in RedactedValues<T> state, IBufferWriter<char> buffer)
        {
            if (_defaultFormatter == null)
            {
                RedactedPropertyFormatter[] propertyRedactors = new RedactedPropertyFormatter[PropertyCount];
                foreach (PropertyRedaction redaction in _redactions)
                {
                    propertyRedactors[redaction.Index] = new RedactedPropertyFormatter(redaction.Redactor);
                }
                // this could be overwritten by another thread in a race but it doesn't matter
                // as any copy of this delegate will have the same functionality
                _defaultFormatter = _originalMetadata.GetMessageFormatter(propertyRedactors);
            }
            _defaultFormatter(state.OriginalState, buffer);
        }

        public Action<RedactedValues<T>, IBufferWriter<char>> GetMessageFormatter(PropertyCustomFormatter[] customFormatters)
        {
            PropertyCustomFormatter[] wrappedFormatters = new PropertyCustomFormatter[customFormatters.Length];
            Array.Copy(customFormatters, wrappedFormatters, customFormatters.Length);
            foreach (PropertyRedaction redaction in _redactions)
            {
                PropertyCustomFormatter nextFormatter = wrappedFormatters[redaction.Index];
                wrappedFormatters[redaction.Index] = nextFormatter == null ?
                    new RedactedPropertyFormatter(redaction.Redactor) :
                    new ChainedRedactedPropertyFormatter(redaction.Redactor, nextFormatter);
            }
            Action<T, IBufferWriter<char>> innerFormatter = _originalMetadata.GetMessageFormatter(wrappedFormatters);
            return (state, buffer) => innerFormatter(state.OriginalState, buffer);
        }

        public LogPropertyInfo GetPropertyInfo(int index) => _originalMetadata.GetPropertyInfo(index);

        class Slot { public ArrayBufferWriter<char>? Buffer; }
        static ThreadLocal<Slot?> t_slot = new ThreadLocal<Slot?>();

        public string FormatMessage(in RedactedValues<T> state)
        {
            Slot? tstate = t_slot.Value;
            if (tstate == null)
            {
                tstate = new Slot();
                t_slot.Value = tstate;
            }
            ArrayBufferWriter<char> arrayBuffer = tstate.Buffer ?? new ArrayBufferWriter<char>();
            tstate.Buffer = null;
            AppendFormattedMessage(state, arrayBuffer);
            string ret = new string(arrayBuffer.WrittenSpan);
            arrayBuffer.Clear();
            tstate.Buffer = arrayBuffer;
            return ret;
        }

        public Func<RedactedValues<T>, Exception?, string> GetStringMessageFormatter() => RedactedValues<T>.Callback;

        class RedactedPropertyFormatterFactory : IPropertyFormatterFactory
        {
            const int MaxStackAllocChars = 256;
            RedactedLogMetadata<T> _metadata;
            IPropertyFormatterFactory _wrappedFormatterFactory;

            public RedactedPropertyFormatterFactory(RedactedLogMetadata<T> metadata, IPropertyFormatterFactory wrappedFormatterFactory)
            {
                _metadata = metadata;
                _wrappedFormatterFactory = wrappedFormatterFactory;
            }

            public FormatPropertyAction<PropType> GetPropertyFormatter<PropType>(int propertyIndex, LogPropertyInfo metadata)
            {
                PropertyRedaction? redaction = _metadata.GetRedactionForIndex(propertyIndex);
                if (!redaction.HasValue)
                {
                    return _wrappedFormatterFactory.GetPropertyFormatter<PropType>(propertyIndex, _metadata.GetPropertyInfo(propertyIndex));
                }
                else
                {
                    return GetRedactedPropertyFormatter<PropType>(propertyIndex, metadata, redaction.Value.Redactor);
                }
            }

            public FormatSpanPropertyAction GetSpanPropertyFormatter(int propertyIndex, LogPropertyInfo metadata)
            {
                PropertyRedaction? redaction = _metadata.GetRedactionForIndex(propertyIndex);
                if (!redaction.HasValue)
                {
                    return _wrappedFormatterFactory.GetSpanPropertyFormatter(propertyIndex, _metadata.GetPropertyInfo(propertyIndex));
                }
                else
                {
                    return GetRedactedSpanPropertyFormatter(propertyIndex, metadata, redaction.Value.Redactor);
                }
            }

            private FormatSpanPropertyAction GetRedactedSpanPropertyFormatter(int propertyIndex, LogPropertyInfo metadata, IRedactor redactor)
            {
                FormatSpanPropertyAction wrappedFormatter = _wrappedFormatterFactory.GetSpanPropertyFormatter(propertyIndex, _metadata.GetPropertyInfo(propertyIndex));
                return FormatRedactedProperty;

                void FormatRedactedProperty(scoped ReadOnlySpan<char> value, ref BufferWriter<byte> writer)
                {
                    int len = redactor.GetRedactedLength(value);
                    if (len <= MaxStackAllocChars)
                    {
                        Span<char> redactedBuffer = stackalloc char[len];
                        redactor.Redact(value, redactedBuffer);
                        wrappedFormatter(redactedBuffer, ref writer);
                    }
                    else
                    {
                        string redactedValue = redactor.Redact(value);
                        wrappedFormatter(redactedValue, ref writer);
                    }
                }
            }

            private FormatPropertyAction<PropType> GetRedactedPropertyFormatter<PropType>(int propertyIndex, LogPropertyInfo metadata, IRedactor redactor)
            {
                FormatSpanPropertyAction wrappedFormatter = _wrappedFormatterFactory.GetSpanPropertyFormatter(propertyIndex, _metadata.GetPropertyInfo(propertyIndex));
                return FormatRedactedProperty;

                void FormatRedactedProperty(PropType value, ref BufferWriter<byte> writer)
                {
                    if (value == null)
                    {
                        return;
                    }
                    else if (value is ISpanFormattable)
                    {
                        Span<char> unredactedBuffer = stackalloc char[MaxStackAllocChars];
                        if (((ISpanFormattable)value).TryFormat(unredactedBuffer, out int charsWritten2, null, null))
                        {
                            unredactedBuffer = unredactedBuffer.Slice(0, charsWritten2);
                            int len2 = redactor!.GetRedactedLength(unredactedBuffer);
                            if (len2 <= MaxStackAllocChars)
                            {
                                Span<char> redactedBuffer2 = stackalloc char[len2];
                                redactor!.Redact(unredactedBuffer, redactedBuffer2);
                                wrappedFormatter(redactedBuffer2, ref writer);
                            }
                            else
                            {
                                string redactedValue = redactor.Redact(unredactedBuffer);
                                wrappedFormatter(redactedValue, ref writer);
                            }
                            return;
                        }
                    }
                    string? unredactedValue = value.ToString();
                    int len = redactor.GetRedactedLength(unredactedValue);
                    if (len <= MaxStackAllocChars)
                    {
                        Span<char> redactedBuffer = stackalloc char[len];
                        redactor.Redact(unredactedValue, redactedBuffer);
                        wrappedFormatter(redactedBuffer, ref writer);
                    }
                    else
                    {
                        string redactedValue = redactor.Redact(unredactedValue);
                        wrappedFormatter(redactedValue, ref writer);
                    }
                }
            }
        }

        public FormatPropertyListAction<RedactedValues<T>> GetPropertyListFormatter(IPropertyFormatterFactory propertyFormatterFactory)
        {
            FormatPropertyListAction<T> wrappedFormatPropertyList = _originalMetadata.GetPropertyListFormatter(new RedactedPropertyFormatterFactory(this, propertyFormatterFactory));
            return FormatPropertyList;

            void FormatPropertyList(in RedactedValues<T> state, ref BufferWriter<byte> writer)
            {
                wrappedFormatPropertyList(in state.OriginalState, ref writer);
            }
        }

        private PropertyRedaction? GetRedactionForIndex(int propIndex)
        {
            foreach (PropertyRedaction redaction in _redactions)
            {
                if (redaction.Index == propIndex)
                {
                    return redaction;
                }
            }
            return null;
        }
    }

    internal struct RedactedValues<T> : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public RedactedValues(RedactedLogMetadata<T> metadata, in T originalState)
        {
            Metadata = metadata;
            OriginalState = originalState;
        }

        public RedactedLogMetadata<T> Metadata;
        public T OriginalState;

        private IReadOnlyList<KeyValuePair<string, object?>> _originalStateValues;
        public IReadOnlyList<KeyValuePair<string, object?>> OriginalStateValues => _originalStateValues ??= OriginalState as IReadOnlyList<KeyValuePair<string, object?>>;

        public override string ToString() => Metadata.FormatMessage(this);

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => OriginalStateValues != null ? OriginalStateValues.Count : 0;

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                var originalValue = OriginalStateValues[index];
                var redactor = Metadata.GetPropertyRedactor(index);
                if (redactor == null)
                {
                    return originalValue;
                }
                string? unredactedValue = originalValue.Value?.ToString();
                return new KeyValuePair<string, object?>(originalValue.Key, redactor.Redact(unredactedValue));
            }
        }

        public static readonly Func<RedactedValues<T>, Exception?, string> Callback = (state, e) => state.ToString();
    }
}

#endif
