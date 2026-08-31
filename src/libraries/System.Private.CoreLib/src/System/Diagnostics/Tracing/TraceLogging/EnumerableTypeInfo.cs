// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
    internal sealed class EnumerableTypeInfo : TraceLoggingTypeInfo
    {
        private readonly TraceLoggingTypeInfo elementInfo;

        public EnumerableTypeInfo(Type type, TraceLoggingTypeInfo elementInfo)
            : base(type)
        {
            this.elementInfo = elementInfo;
        }

        internal TraceLoggingTypeInfo ElementInfo { get { return elementInfo; } }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string? name,
            EventFieldFormat format)
        {
            collector.BeginBufferedArray();
            this.elementInfo.WriteMetadata(collector, name, format);
            collector.EndBufferedArray();
        }

        public override void WriteData(PropertyValue value)
        {
            int bookmark = TraceLoggingDataCollector.BeginBufferedArray();

            int count = 0;
            IEnumerable? enumerable = (IEnumerable?)value.ReferenceValue;
            if (enumerable != null)
            {
                foreach (object? element in enumerable)
                {
                    this.elementInfo.WriteData(elementInfo.PropertyValueFactory(element));
                    count++;
                }
            }

            TraceLoggingDataCollector.EndBufferedArray(bookmark, count);
        }

        public override object? GetData(object? value)
        {
            Debug.Assert(value != null, "null accepted only for some overrides");
            var iterType = (IEnumerable)value;
            List<object?> serializedEnumerable = new List<object?>();
            foreach (object? element in iterType)
            {
                serializedEnumerable.Add(elementInfo.GetData(element));
            }
            return serializedEnumerable.ToArray();
        }
    }
}
