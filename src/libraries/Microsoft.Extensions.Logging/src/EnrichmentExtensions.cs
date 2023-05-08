// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleApp31.Prototype
{
    /*
    public static class EnrichmentExtensions
    {
        public static ILoggingBuilder Enrich<T>(this ILoggingBuilder builder, string propertyName, Func<T> valueFunc)
        {
            builder.AddProcessor((sp, next) =>
            {
                IEnumerable<EnrichmentProperty> props = sp.GetServices<EnrichmentProperty>();
                EnrichmentPropertiesCollection collection = new EnrichmentPropertiesCollection();
                foreach(var prop in props)
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

        internal class EnrichmentProperty<T> : EnrichmentProperty
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

    internal class EnrichmentPropertiesCollection
    {
        internal virtual EnrichmentPropertiesCollection AddProperty<T>(string propertyName, Func<T> getValue)
        {
            var collection = new EnrichmentPropertiesCollection<T>();
            collection.Name0 = propertyName;
            collection.GetValue0 = getValue;
            return collection;
        }

        internal virtual LogEntryHandler<TState, TEnrichmentProperties> GetLogEntryHandler<TState, TEnrichmentProperties>(ILogEntryProcessor nextProcessor, ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            return nextProcessor.GetLogEntryHandler<TState, TEnrichmentProperties>(metadata, out enabled, out dynamicEnabledCheckRequired);
        }
    }

    internal class EnrichmentProcessor : ILogEntryProcessor
    {
        private ILogEntryProcessor _nextProcessor;
        private EnrichmentPropertiesCollection _propCollection;

        public EnrichmentProcessor(EnrichmentPropertiesCollection collection, ILogEntryProcessor nextProcessor)
        {
            _propCollection = collection;
            _nextProcessor = nextProcessor;
        }
        public LogEntryHandler<TState, TEnrichmentProperties> GetLogEntryHandler<TState, TEnrichmentProperties>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            return _propCollection.GetLogEntryHandler<TState, TEnrichmentProperties>(_nextProcessor, metadata, out enabled, out dynamicEnabledCheckRequired);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _nextProcessor.IsEnabled(logLevel);
        }
    }

    internal class EnrichmentPropertiesCollection<T0> : EnrichmentPropertiesCollection
    {
        internal string Name0;
        internal Func<T0> GetValue0;

        internal override EnrichmentPropertiesCollection AddProperty<T>(string propertyName, Func<T> getValue)
        {
            var collection = new EnrichmentPropertiesCollection<T0, T>();
            collection.Name0 = Name0;
            collection.GetValue0 = GetValue0;
            collection.Name1 = propertyName;
            collection.GetValue1 = getValue;
            return collection;
        }

        internal override LogEntryHandler<TState, TEnrichmentProperties> GetLogEntryHandler<TState, TEnrichmentProperties>(ILogEntryProcessor nextProcessor, ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            LogEntryHandler<TState, EnrichmentPropertyValues<TEnrichmentProperties, T0>> nextHandler =
                nextProcessor.GetLogEntryHandler<TState, EnrichmentPropertyValues<TEnrichmentProperties, T0>>(metadata, out enabled, out dynamicEnabledCheckRequired);
            return new EnrichmentHandler<TState, TEnrichmentProperties>(nextHandler, this);
        }

        private class EnrichmentHandler<TState, TEnrichmentProperties> : LogEntryHandler<TState, TEnrichmentProperties>
        {
            private LogEntryHandler<TState, EnrichmentPropertyValues<TEnrichmentProperties, T0>> _nextHandler;
            private EnrichmentPropertiesCollection<T0> _propertyCollection;

            public EnrichmentHandler(LogEntryHandler<TState, EnrichmentPropertyValues<TEnrichmentProperties, T0>> nextHandler, EnrichmentPropertiesCollection<T0> propertyCollection)
            {
                _nextHandler = nextHandler;
                _propertyCollection = propertyCollection;
            }

            public override void HandleLogEntry(ref LogEntry<TState, TEnrichmentProperties> logEntry)
            {
#pragma warning disable SA1129 // Do not use default value type constructor
                EnrichmentPropertyValues<TEnrichmentProperties, T0> enrichmentProperties = new EnrichmentPropertyValues<TEnrichmentProperties, T0>();
#pragma warning restore SA1129 // Do not use default value type constructor
                enrichmentProperties.NestedProperties = logEntry.EnrichmentProperties;
                enrichmentProperties.Value0 = _propertyCollection.GetValue0();
                var newLogEntry = new LogEntry<TState, EnrichmentPropertyValues<TEnrichmentProperties, T0>>(logEntry.LogLevel, logEntry.EventId, ref logEntry.State, ref enrichmentProperties, logEntry.Exception, logEntry.Formatter);
                _nextHandler.HandleLogEntry(ref newLogEntry);
            }

            public override bool IsEnabled(LogLevel level) => _nextHandler.IsEnabled(level);
        }
    }

    internal class EnrichmentPropertiesCollection<T0, T1> : EnrichmentPropertiesCollection
    {
        internal string Name0;
        internal Func<T0> GetValue0;
        internal string Name1;
        internal Func<T1> GetValue1;

        internal override EnrichmentPropertiesCollection AddProperty<T>(string propertyName, Func<T> getValue)
        {
            var collection = new UnboundedEnrichmentPropertiesCollection<T0, T1>();
            collection.Name0 = Name0;
            collection.GetValue0 = GetValue0;
            collection.Name1 = Name1;
            collection.GetValue1 = GetValue1;
            collection.OverflowProperties.Add((propertyName, () => getValue()));
            return collection;
        }

        internal override LogEntryHandler<TState, TEnrichmentProperties> GetLogEntryHandler<TState, TEnrichmentProperties>(ILogEntryProcessor nextProcessor, ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            LogEntryHandler<TState, EnrichmentPropertyValues<TEnrichmentProperties, T0, T1>> nextHandler =
                nextProcessor.GetLogEntryHandler<TState, EnrichmentPropertyValues<TEnrichmentProperties, T0, T1>>(metadata, out enabled, out dynamicEnabledCheckRequired);
            return new EnrichmentHandler<TState, TEnrichmentProperties>(nextHandler, this);
        }

        private class EnrichmentHandler<TState, TEnrichmentProperties> : LogEntryHandler<TState, TEnrichmentProperties>
        {
            private LogEntryHandler<TState, EnrichmentPropertyValues<TEnrichmentProperties, T0, T1>> _nextHandler;
            private EnrichmentPropertiesCollection<T0, T1> _propertyCollection;

            public EnrichmentHandler(LogEntryHandler<TState, EnrichmentPropertyValues<TEnrichmentProperties, T0, T1>> nextHandler, EnrichmentPropertiesCollection<T0, T1> propertyCollection)
            {
                _nextHandler = nextHandler;
                _propertyCollection = propertyCollection;
            }

            public override void HandleLogEntry(ref LogEntry<TState, TEnrichmentProperties> logEntry)
            {
#pragma warning disable SA1129 // Do not use default value type constructor
                EnrichmentPropertyValues<TEnrichmentProperties, T0, T1> enrichmentProperties = new EnrichmentPropertyValues<TEnrichmentProperties, T0, T1>();
#pragma warning restore SA1129 // Do not use default value type constructor
                enrichmentProperties.NestedProperties = logEntry.EnrichmentProperties;
                enrichmentProperties.Value0 = _propertyCollection.GetValue0();
                enrichmentProperties.Value1 = _propertyCollection.GetValue1();
                var newLogEntry = new LogEntry<TState, EnrichmentPropertyValues<TEnrichmentProperties, T0, T1>>(logEntry.LogLevel, logEntry.EventId, ref logEntry.State, ref enrichmentProperties, logEntry.Exception, logEntry.Formatter);
                _nextHandler.HandleLogEntry(ref newLogEntry);
            }

            public override bool IsEnabled(LogLevel level) => _nextHandler.IsEnabled(level);
        }
    }

    internal class UnboundedEnrichmentPropertiesCollection<T0, T1> : EnrichmentPropertiesCollection
    {
        internal string Name0;
        internal Func<T0> GetValue0;
        internal string Name1;
        internal Func<T1> GetValue1;
        internal List<(string, Func<object?>)> OverflowProperties = new List<(string, Func<object>)>();

        internal override EnrichmentPropertiesCollection AddProperty<T>(string propertyName, Func<T> getValue)
        {
            OverflowProperties.Add((propertyName, () => getValue()));
            return this;
        }

        internal override LogEntryHandler<TState, TEnrichmentProperties> GetLogEntryHandler<TState, TEnrichmentProperties>(ILogEntryProcessor nextProcessor, ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            LogEntryHandler<TState, UnboundedEnrichmentPropertyValues<TEnrichmentProperties, T0, T1>> nextHandler =
                nextProcessor.GetLogEntryHandler<TState, UnboundedEnrichmentPropertyValues<TEnrichmentProperties, T0, T1>>(metadata, out enabled, out dynamicEnabledCheckRequired);
            return new EnrichmentHandler<TState, TEnrichmentProperties>(nextHandler, this);
        }

        private class EnrichmentHandler<TState, TEnrichmentProperties> : LogEntryHandler<TState, TEnrichmentProperties>
        {
            private LogEntryHandler<TState, UnboundedEnrichmentPropertyValues<TEnrichmentProperties, T0, T1>> _nextHandler;
            private UnboundedEnrichmentPropertiesCollection<T0, T1> _propertyCollection;

            public EnrichmentHandler(LogEntryHandler<TState, UnboundedEnrichmentPropertyValues<TEnrichmentProperties, T0, T1>> nextHandler, UnboundedEnrichmentPropertiesCollection<T0, T1> propertyCollection)
            {
                _nextHandler = nextHandler;
                _propertyCollection = propertyCollection;
            }

            public override void HandleLogEntry(ref LogEntry<TState, TEnrichmentProperties> logEntry)
            {
#pragma warning disable SA1129 // Do not use default value type constructor
                UnboundedEnrichmentPropertyValues<TEnrichmentProperties, T0, T1> enrichmentProperties = new UnboundedEnrichmentPropertyValues<TEnrichmentProperties, T0, T1>();
#pragma warning disable SA1129 // Do not use default value type constructor
                enrichmentProperties.NestedProperties = logEntry.EnrichmentProperties;
                enrichmentProperties.Value0 = _propertyCollection.GetValue0();
                enrichmentProperties.Value1 = _propertyCollection.GetValue1();
                int overflowProps = _propertyCollection.OverflowProperties.Count;
                enrichmentProperties.ExtraValues = ArrayPool<object?>.Shared.Rent(overflowProps);
                for (int i = 0; i < overflowProps; i++)
                {
                    enrichmentProperties.ExtraValues[i] = _propertyCollection.OverflowProperties[i].Item2();
                }
                var newLogEntry = new LogEntry<TState, UnboundedEnrichmentPropertyValues<TEnrichmentProperties, T0, T1>>(logEntry.LogLevel, logEntry.EventId, ref logEntry.State, ref enrichmentProperties, logEntry.Exception, logEntry.Formatter);
                _nextHandler.HandleLogEntry(ref newLogEntry);
            }

            public override bool IsEnabled(LogLevel level) => _nextHandler.IsEnabled(level);
        }
    }

    internal struct EmptyEnrichmentPropertyValues
    {
    }

    internal struct EnrichmentPropertyValues<TEnrichmentProperties, T0>
    {
        internal TEnrichmentProperties NestedProperties;
        internal T0 Value0;
    }

    internal struct EnrichmentPropertyValues<TEnrichmentProperties, T0, T1>
    {
        internal TEnrichmentProperties NestedProperties;
        internal T0 Value0;
        internal T1 Value1;
    }

    internal struct UnboundedEnrichmentPropertyValues<TEnrichmentProperties, T0, T1>
    {
        internal TEnrichmentProperties NestedProperties;
        internal T0 Value0;
        internal T1 Value1;
        internal object?[] ExtraValues;
    }
    */
}
