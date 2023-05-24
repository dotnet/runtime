// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging
{
    public static class EnrichmentExtensions
    {
        public static ILoggingBuilder Enrich<T>(this ILoggingBuilder builder, string propertyName, Func<T> valueFunc)
        {
            builder.AddProcessor((sp, next) =>
            {
                IEnumerable<EnrichmentProperty> props = sp.GetServices<EnrichmentProperty>();
                EnrichmentPropertiesCollection collection = new EnrichmentPropertiesCollection();
                foreach (var prop in props)
                {
                    collection = prop.AppendTo(collection);
                }
                return new EnrichmentProcessor(collection, next);
            });
            builder.Services.AddSingleton<EnrichmentProperty>(new EnrichmentProperty<T>(propertyName, valueFunc));
            return builder;
        }

        internal abstract class EnrichmentProperty
        {
            internal abstract EnrichmentPropertiesCollection AppendTo(EnrichmentPropertiesCollection collection);
        }

        internal sealed class EnrichmentProperty<T> : EnrichmentProperty
        {
            public EnrichmentProperty(string name, Func<T> getValue)
            {
                Name = name;
                GetValue = getValue;
            }

            public string Name { get; }
            public Func<T> GetValue { get; }

            internal override EnrichmentPropertiesCollection AppendTo(EnrichmentPropertiesCollection collection)
            {
                return collection.AddProperty(Name, GetValue);
            }
        }
    }

    internal sealed class EnrichmentProcessor : ILogEntryProcessor
    {
        private readonly ILogEntryProcessor _nextProcessor;
        private readonly EnrichmentPropertiesCollection _propCollection;

        public EnrichmentProcessor(EnrichmentPropertiesCollection collection,  ILogEntryProcessor nextProcessor)
        {
            _propCollection = collection;
            _nextProcessor = nextProcessor;
        }
        public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            return _propCollection.GetLogEntryHandler<TState>(_nextProcessor, metadata, out enabled, out dynamicEnabledCheckRequired);
        }

        public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired) where TState : notnull
        {
            return _nextProcessor.GetScopeHandler<TState>(metadata, out enabled, out dynamicEnabledCheckRequired);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _nextProcessor.IsEnabled(logLevel);
        }
    }

    internal class EnrichmentPropertiesCollection
    {
        internal virtual EnrichmentPropertiesCollection AddProperty<T>(string propertyName, Func<T> getValue)
        {
            return new EnrichmentPropertiesCollection<T>(propertyName, getValue);
        }

        internal virtual LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogEntryProcessor nextProcessor, ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            return nextProcessor.GetLogEntryHandler<TState>(metadata, out enabled, out dynamicEnabledCheckRequired);
        }
    }

    internal sealed class EnrichmentPropertiesCollection<T0> : EnrichmentPropertiesCollection
    {
        internal readonly string Name0;
        internal readonly Func<T0> GetValue0;

        public EnrichmentPropertiesCollection(string name0, Func<T0> getValue0)
        {
            Name0 = name0;
            GetValue0 = getValue0;
        }

        internal override EnrichmentPropertiesCollection AddProperty<T>(string propertyName, Func<T> getValue)
        {
            return new EnrichmentPropertiesCollection<T0, T>(Name0, GetValue0, propertyName, getValue);
        }

        internal override LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogEntryProcessor nextProcessor, ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            var enrichmentMetadata = metadata != null ? new EnrichmentLogMetadata<TState>(metadata) : null;
            LogEntryHandler<EnrichmentPropertyValues<TState, T0>> nextHandler =
                nextProcessor.GetLogEntryHandler<EnrichmentPropertyValues<TState, T0>>(enrichmentMetadata, out enabled, out dynamicEnabledCheckRequired);
            return new EnrichmentHandler<TState>(nextHandler, this);
        }

        private sealed class EnrichmentHandler<TState> : LogEntryHandler<TState>
        {
            private LogEntryHandler<EnrichmentPropertyValues<TState, T0>> _nextHandler;
            private EnrichmentPropertiesCollection<T0> _propertyCollection;

            public EnrichmentHandler(LogEntryHandler<EnrichmentPropertyValues<TState, T0>> nextHandler, EnrichmentPropertiesCollection<T0> propertyCollection)
            {
                _nextHandler = nextHandler;
                _propertyCollection = propertyCollection;
            }

            public override void HandleLogEntry(ref LogEntry<TState> logEntry)
            {
                EnrichmentPropertyValues<TState, T0> enrichmentProperties = new EnrichmentPropertyValues<TState, T0>(logEntry.State, new KeyValuePair<string, object?>(_propertyCollection.Name0, _propertyCollection.GetValue0()), logEntry.Formatter);
                var newLogEntry = new LogEntry<EnrichmentPropertyValues<TState, T0>>(logEntry.LogLevel, category: null!, logEntry.EventId, enrichmentProperties, logEntry.Exception, EnrichmentPropertyValues<TState, T0>.Format);
                _nextHandler.HandleLogEntry(ref newLogEntry);
            }

            public override bool IsEnabled(LogLevel level) => _nextHandler.IsEnabled(level);
        }

        private sealed class EnrichmentLogMetadata<TState> : ILogMetadata<EnrichmentPropertyValues<TState, T0>>
        {
            private ILogMetadata<TState> _innerMetadata { get; }

            public EnrichmentLogMetadata(ILogMetadata<TState> innerMetadata)
            {
                _innerMetadata = innerMetadata;
            }

            public LogLevel LogLevel => _innerMetadata.LogLevel;
            public EventId EventId => _innerMetadata.EventId;
            public string OriginalFormat => _innerMetadata.OriginalFormat;
            public int PropertyCount => _innerMetadata.PropertyCount;
            public void AppendFormattedMessage(in EnrichmentPropertyValues<TState, T0> state, IBufferWriter<char> buffer) => _innerMetadata.AppendFormattedMessage(state.NestedProperties, buffer);
            public Action<EnrichmentPropertyValues<TState, T0>, IBufferWriter<char>> GetMessageFormatter(PropertyCustomFormatter[] customFormatters)
            {
                var formatter = _innerMetadata.GetMessageFormatter(customFormatters);
                return (e, w) => formatter(e.NestedProperties, w);
            }
            public FormatPropertyListAction<EnrichmentPropertyValues<TState, T0>> GetPropertyListFormatter(IPropertyFormatterFactory propertyFormatterFactory)
            {
                FormatPropertyListAction<TState> formatter = _innerMetadata.GetPropertyListFormatter(propertyFormatterFactory);
                return new FormatPropertyListAction<EnrichmentPropertyValues<TState, T0>>((in EnrichmentPropertyValues<TState, T0> s, ref BufferWriter<byte> w) => formatter(in s.NestedProperties, ref w));
            }
            public LogPropertyMetadata GetPropertyMetadata(int index) => _innerMetadata.GetPropertyMetadata(index);
            public Func<EnrichmentPropertyValues<TState, T0>, Exception?, string> GetStringMessageFormatter()
            {
                var formatter = _innerMetadata.GetStringMessageFormatter();
                return (e, ex) => formatter(e.NestedProperties, ex);
            }
        }
    }

    internal sealed class EnrichmentPropertiesCollection<T0, T1> : EnrichmentPropertiesCollection
    {
        internal string Name0;
        internal Func<T0> GetValue0;
        internal string Name1;
        internal Func<T1> GetValue1;

        public EnrichmentPropertiesCollection(string name0, Func<T0> getValue0, string name1, Func<T1> getValue1)
        {
            Name0 = name0;
            GetValue0 = getValue0;
            Name1 = name1;
            GetValue1 = getValue1;
        }

        internal override EnrichmentPropertiesCollection AddProperty<T>(string propertyName, Func<T> getValue)
        {
            var overflowProperties = new List<(string, Func<object?>)>();
            overflowProperties.Add((propertyName, () => getValue()));
            var collection = new UnboundedEnrichmentPropertiesCollection<T0, T1>(Name0, GetValue0, Name1, GetValue1, overflowProperties);
            return collection;
        }

        internal override LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogEntryProcessor nextProcessor, ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            var enrichmentMetadata = metadata != null ? new EnrichmentLogMetadata<TState>(metadata) : null;
            LogEntryHandler<EnrichmentPropertyValues<TState, T0, T1>> nextHandler =
                nextProcessor.GetLogEntryHandler<EnrichmentPropertyValues<TState, T0, T1>>(enrichmentMetadata, out enabled, out dynamicEnabledCheckRequired);
            return new EnrichmentHandler<TState>(nextHandler, this);
        }

        private sealed class EnrichmentHandler<TState> : LogEntryHandler<TState>
        {
            private readonly LogEntryHandler<EnrichmentPropertyValues<TState, T0, T1>> _nextHandler;
            private readonly EnrichmentPropertiesCollection<T0, T1> _propertyCollection;

            public EnrichmentHandler(LogEntryHandler<EnrichmentPropertyValues<TState, T0, T1>> nextHandler, EnrichmentPropertiesCollection<T0, T1> propertyCollection)
            {
                _nextHandler = nextHandler;
                _propertyCollection = propertyCollection;
            }

            public override void HandleLogEntry(ref LogEntry<TState> logEntry)
            {
                EnrichmentPropertyValues<TState, T0, T1> enrichmentProperties = new EnrichmentPropertyValues<TState, T0, T1>(
                    logEntry.State,
                    new KeyValuePair<string, object?>(_propertyCollection.Name0, _propertyCollection.GetValue0()),
                    new KeyValuePair<string, object?>(_propertyCollection.Name1, _propertyCollection.GetValue1()),
                    logEntry.Formatter);
                var newLogEntry = new LogEntry<EnrichmentPropertyValues<TState, T0, T1>>(logEntry.LogLevel, category: null!, logEntry.EventId, enrichmentProperties, logEntry.Exception, EnrichmentPropertyValues<TState, T0, T1>.Format);
                _nextHandler.HandleLogEntry(ref newLogEntry);
            }

            public override bool IsEnabled(LogLevel level) => _nextHandler.IsEnabled(level);
        }

        private sealed class EnrichmentLogMetadata<TState> : ILogMetadata<EnrichmentPropertyValues<TState, T0, T1>>
        {
            private ILogMetadata<TState> _innerMetadata { get; }

            public EnrichmentLogMetadata(ILogMetadata<TState> innerMetadata)
            {
                _innerMetadata = innerMetadata;
            }

            public LogLevel LogLevel => _innerMetadata.LogLevel;
            public EventId EventId => _innerMetadata.EventId;
            public string OriginalFormat => _innerMetadata.OriginalFormat;
            public int PropertyCount => _innerMetadata.PropertyCount;
            public void AppendFormattedMessage(in EnrichmentPropertyValues<TState, T0, T1> state, IBufferWriter<char> buffer) => _innerMetadata.AppendFormattedMessage(state.NestedProperties, buffer);
            public Action<EnrichmentPropertyValues<TState, T0, T1>, IBufferWriter<char>> GetMessageFormatter(PropertyCustomFormatter[] customFormatters)
            {
                var formatter = _innerMetadata.GetMessageFormatter(customFormatters);
                return (e, w) => formatter(e.NestedProperties, w);
            }
            public FormatPropertyListAction<EnrichmentPropertyValues<TState, T0, T1>> GetPropertyListFormatter(IPropertyFormatterFactory propertyFormatterFactory)
            {
                FormatPropertyListAction<TState> formatter = _innerMetadata.GetPropertyListFormatter(propertyFormatterFactory);
                return new FormatPropertyListAction<EnrichmentPropertyValues<TState, T0, T1>>((in EnrichmentPropertyValues<TState, T0, T1> s, ref BufferWriter<byte> w) => formatter(in s.NestedProperties, ref w));
            }
            public LogPropertyMetadata GetPropertyMetadata(int index) => _innerMetadata.GetPropertyMetadata(index);
            public Func<EnrichmentPropertyValues<TState, T0, T1>, Exception?, string> GetStringMessageFormatter()
            {
                var formatter = _innerMetadata.GetStringMessageFormatter();
                return (e, ex) => formatter(e.NestedProperties, ex);
            }
        }
    }

    internal sealed class UnboundedEnrichmentPropertiesCollection<T0, T1> : EnrichmentPropertiesCollection
    {
        internal readonly string Name0;
        internal readonly Func<T0> GetValue0;
        internal readonly string Name1;
        internal readonly Func<T1> GetValue1;
        internal readonly List<(string, Func<object?>)> OverflowProperties;

        public UnboundedEnrichmentPropertiesCollection(string name0, Func<T0> getValue0, string name1, Func<T1> getValue1, List<(string, Func<object?>)> overflowProperties)
        {
            Name0 = name0;
            GetValue0 = getValue0;
            Name1 = name1;
            GetValue1 = getValue1;
            OverflowProperties = overflowProperties;
        }

        internal override EnrichmentPropertiesCollection AddProperty<T>(string propertyName, Func<T> getValue)
        {
            OverflowProperties.Add((propertyName, () => getValue()));
            return this;
        }

        internal override LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogEntryProcessor nextProcessor, ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            var enrichmentMetadata = metadata != null ? new EnrichmentLogMetadata<TState>(metadata) : null;
            LogEntryHandler<UnboundedEnrichmentPropertyValues<TState, T0, T1>> nextHandler =
                nextProcessor.GetLogEntryHandler<UnboundedEnrichmentPropertyValues<TState, T0, T1>>(enrichmentMetadata, out enabled, out dynamicEnabledCheckRequired);
            return new EnrichmentHandler<TState>(nextHandler, this);
        }

        private sealed class EnrichmentHandler<TState> : LogEntryHandler<TState>
        {
            private readonly LogEntryHandler<UnboundedEnrichmentPropertyValues<TState, T0, T1>> _nextHandler;
            private readonly UnboundedEnrichmentPropertiesCollection<T0, T1> _propertyCollection;

            public EnrichmentHandler(LogEntryHandler<UnboundedEnrichmentPropertyValues<TState, T0, T1>> nextHandler, UnboundedEnrichmentPropertiesCollection<T0, T1> propertyCollection)
            {
                _nextHandler = nextHandler;
                _propertyCollection = propertyCollection;
            }

            public override void HandleLogEntry(ref LogEntry<TState> logEntry)
            {
                int overflowProps = _propertyCollection.OverflowProperties.Count;
                var extraValues = new KeyValuePair<string, object?>[overflowProps];
                for (int i = 0; i < overflowProps; i++)
                {
                    var property = _propertyCollection.OverflowProperties[i];
                    extraValues[i] = new KeyValuePair<string, object?>(property.Item1, property.Item2());
                }

                UnboundedEnrichmentPropertyValues<TState, T0, T1> enrichmentProperties = new UnboundedEnrichmentPropertyValues<TState, T0, T1>(
                    logEntry.State,
                    new KeyValuePair<string, object?>(_propertyCollection.Name0, _propertyCollection.GetValue0()),
                    new KeyValuePair<string, object?>(_propertyCollection.Name1, _propertyCollection.GetValue1()),
                    extraValues,
                    logEntry.Formatter);
                var newLogEntry = new LogEntry<UnboundedEnrichmentPropertyValues<TState, T0, T1>>(logEntry.LogLevel, category: null!, logEntry.EventId, enrichmentProperties, logEntry.Exception, UnboundedEnrichmentPropertyValues<TState, T0, T1>.Format);
                _nextHandler.HandleLogEntry(ref newLogEntry);
            }

            public override bool IsEnabled(LogLevel level) => _nextHandler.IsEnabled(level);
        }

        private sealed class EnrichmentLogMetadata<TState> : ILogMetadata<UnboundedEnrichmentPropertyValues<TState, T0, T1>>
        {
            private ILogMetadata<TState> _innerMetadata { get; }

            public EnrichmentLogMetadata(ILogMetadata<TState> innerMetadata)
            {
                _innerMetadata = innerMetadata;
            }

            public LogLevel LogLevel => _innerMetadata.LogLevel;
            public EventId EventId => _innerMetadata.EventId;
            public string OriginalFormat => _innerMetadata.OriginalFormat;
            public int PropertyCount => _innerMetadata.PropertyCount;
            public void AppendFormattedMessage(in UnboundedEnrichmentPropertyValues<TState, T0, T1> state, IBufferWriter<char> buffer) => _innerMetadata.AppendFormattedMessage(state.NestedProperties, buffer);
            public Action<UnboundedEnrichmentPropertyValues<TState, T0, T1>, IBufferWriter<char>> GetMessageFormatter(PropertyCustomFormatter[] customFormatters)
            {
                var formatter = _innerMetadata.GetMessageFormatter(customFormatters);
                return (e, w) => formatter(e.NestedProperties, w);
            }
            public FormatPropertyListAction<UnboundedEnrichmentPropertyValues<TState, T0, T1>> GetPropertyListFormatter(IPropertyFormatterFactory propertyFormatterFactory)
            {
                FormatPropertyListAction<TState> formatter = _innerMetadata.GetPropertyListFormatter(propertyFormatterFactory);
                return new FormatPropertyListAction<UnboundedEnrichmentPropertyValues<TState, T0, T1>>((in UnboundedEnrichmentPropertyValues<TState, T0, T1> s, ref BufferWriter<byte> w) => formatter(in s.NestedProperties, ref w));
            }
            public LogPropertyMetadata GetPropertyMetadata(int index) => _innerMetadata.GetPropertyMetadata(index);
            public Func<UnboundedEnrichmentPropertyValues<TState, T0, T1>, Exception?, string> GetStringMessageFormatter()
            {
                var formatter = _innerMetadata.GetStringMessageFormatter();
                return (e, ex) => formatter(e.NestedProperties, ex);
            }
        }
    }

    internal readonly struct EnrichmentPropertyValues<TEnrichmentProperties, T0> : IReadOnlyList<KeyValuePair<string, object?>>
    {
        internal readonly TEnrichmentProperties NestedProperties;
        internal readonly KeyValuePair<string, object?> Prop0;
        internal readonly Func<TEnrichmentProperties, Exception?, string> Formatter;

        public EnrichmentPropertyValues(TEnrichmentProperties nestedProperties, KeyValuePair<string, object?> prop0, Func<TEnrichmentProperties, Exception?, string> formatter)
        {
            NestedProperties = nestedProperties;
            Prop0 = prop0;
            Formatter = formatter;
        }

        public int Count
        {
            get
            {
                var nested = NestedProperties as IReadOnlyList<KeyValuePair<string, object?>>;
                return (nested?.Count ?? 0) + 1;
            }
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                var nested = NestedProperties as IReadOnlyList<KeyValuePair<string, object?>>;
                if (index == 0)
                {
                    return Prop0;
                }
                else if (nested != null)
                {
                    return nested[index - 1];
                }
                else
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }
            }
        }

        public override string ToString() => NestedProperties?.ToString() ?? string.Empty;

        public static string Format(EnrichmentPropertyValues<TEnrichmentProperties, T0> state, Exception? exception)
        {
            return state.Formatter(state.NestedProperties, exception);
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return Prop0;

            var nested = NestedProperties as IReadOnlyList<KeyValuePair<string, object?>>;
            if (nested != null)
            {
                foreach (var item in nested)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal readonly struct EnrichmentPropertyValues<TEnrichmentProperties, T0, T1> : IReadOnlyList<KeyValuePair<string, object?>>
    {
        internal readonly TEnrichmentProperties NestedProperties;
        internal readonly KeyValuePair<string, object?> Prop0;
        internal readonly KeyValuePair<string, object?> Prop1;
        internal readonly Func<TEnrichmentProperties, Exception?, string> Formatter;

        public EnrichmentPropertyValues(TEnrichmentProperties nestedProperties, KeyValuePair<string, object?> prop0, KeyValuePair<string, object?> prop1, Func<TEnrichmentProperties, Exception?, string> formatter)
        {
            NestedProperties = nestedProperties;
            Prop0 = prop0;
            Prop1 = prop1;
            Formatter = formatter;
        }

        public int Count
        {
            get
            {
                var nested = NestedProperties as IReadOnlyList<KeyValuePair<string, object?>>;
                return (nested?.Count ?? 0) + 2;
            }
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                var nested = NestedProperties as IReadOnlyList<KeyValuePair<string, object?>>;
                if (index == 0)
                {
                    return Prop0;
                }
                else if (index == 1)
                {
                    return Prop1;
                }
                else if (nested != null)
                {
                    return nested[index - 1];
                }
                else
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }
            }
        }

        public override string ToString() => NestedProperties?.ToString() ?? string.Empty;

        public static string Format(EnrichmentPropertyValues<TEnrichmentProperties, T0, T1> state, Exception? exception)
        {
            return state.Formatter(state.NestedProperties, exception);
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return Prop0;
            yield return Prop1;

            var nested = NestedProperties as IReadOnlyList<KeyValuePair<string, object?>>;
            if (nested != null)
            {
                for (var i = 0; i < nested.Count; i++)
                {
                    yield return nested[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal readonly struct UnboundedEnrichmentPropertyValues<TEnrichmentProperties, T0, T1> : IReadOnlyList<KeyValuePair<string, object?>>
    {
        internal readonly TEnrichmentProperties NestedProperties;
        internal readonly KeyValuePair<string, object?> Prop0;
        internal readonly KeyValuePair<string, object?> Prop1;
        internal readonly KeyValuePair<string, object?>[] ExtraValues;
        internal readonly Func<TEnrichmentProperties, Exception?, string> Formatter;

        public UnboundedEnrichmentPropertyValues(TEnrichmentProperties nestedProperties, KeyValuePair<string, object?> prop0, KeyValuePair<string, object?> prop1, KeyValuePair<string, object?>[] extraValues, Func<TEnrichmentProperties, Exception?, string> formatter)
        {
            NestedProperties = nestedProperties;
            Prop0 = prop0;
            Prop1 = prop1;
            ExtraValues = extraValues;
            Formatter = formatter;
        }

        public int Count
        {
            get
            {
                var nested = NestedProperties as IReadOnlyList<KeyValuePair<string, object?>>;
                return (nested?.Count ?? 0) + 2 + ExtraValues.Length;
            }
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                var nested = NestedProperties as IReadOnlyList<KeyValuePair<string, object?>>;
                if (index == 0)
                {
                    return Prop0;
                }
                else if (index == 1)
                {
                    return Prop1;
                }
                else
                {
                    var i = index - 2;
                    if (i < ExtraValues.Length)
                    {
                        return ExtraValues[i];
                    }
                    else if (nested != null)
                    {
                        return nested[i - ExtraValues.Length];
                    }
                    else
                    {
                        throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }
        }

        public override string ToString() => NestedProperties?.ToString() ?? string.Empty;

        public static string Format(UnboundedEnrichmentPropertyValues<TEnrichmentProperties, T0, T1> state, Exception? exception)
        {
            return state.Formatter(state.NestedProperties, exception);
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return Prop0;
            yield return Prop1;
            for (var i = 0; i < ExtraValues.Length; i++)
            {
                yield return ExtraValues[i];
            }

            var nested = NestedProperties as IReadOnlyList<KeyValuePair<string, object?>>;
            if (nested != null)
            {
                for (var i = 0; i < nested.Count; i++)
                {
                    yield return nested[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
