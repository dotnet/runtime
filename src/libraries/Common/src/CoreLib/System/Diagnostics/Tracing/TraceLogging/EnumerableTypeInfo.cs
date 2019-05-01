// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    internal sealed class EnumerableTypeInfo : TraceLoggingTypeInfo
    {
        private readonly TraceLoggingTypeInfo elementInfo;

        public EnumerableTypeInfo(Type type, TraceLoggingTypeInfo elementInfo)
            : base(type)
        {
            this.elementInfo = elementInfo;
        }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string? name,
            EventFieldFormat format)
        {
            collector.BeginBufferedArray();
            this.elementInfo.WriteMetadata(collector, name, format);
            collector.EndBufferedArray();
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            var bookmark = collector.BeginBufferedArray();

            var count = 0;
            IEnumerable? enumerable = (IEnumerable?)value.ReferenceValue;
            if (enumerable != null)
            {
                foreach (var element in enumerable)
                {
                    this.elementInfo.WriteData(collector, elementInfo.PropertyValueFactory(element));
                    count++;
                }
            }

            collector.EndBufferedArray(bookmark, count);
        }

        public override object? GetData(object? value)
        {
            Debug.Assert(value != null, "null accepted only for some overrides");
            var iterType = (IEnumerable)value;
            List<object?> serializedEnumerable = new List<object?>();
            foreach (var element in iterType)
            {
                serializedEnumerable.Add(elementInfo.GetData(element));
            }
            return serializedEnumerable.ToArray();
        }
    }
}
