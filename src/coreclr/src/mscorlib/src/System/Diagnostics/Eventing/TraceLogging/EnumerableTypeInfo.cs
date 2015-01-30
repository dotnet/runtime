// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    internal sealed class EnumerableTypeInfo<IterableType, ElementType>
        : TraceLoggingTypeInfo<IterableType>
        where IterableType : IEnumerable<ElementType>
    {
        private readonly TraceLoggingTypeInfo<ElementType> elementInfo;

        public EnumerableTypeInfo(TraceLoggingTypeInfo<ElementType> elementInfo)
        {
            this.elementInfo = elementInfo;
        }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.BeginBufferedArray();
            this.elementInfo.WriteMetadata(collector, name, format);
            collector.EndBufferedArray();
        }

        public override void WriteData(
            TraceLoggingDataCollector collector,
            ref IterableType value)
        {
            var bookmark = collector.BeginBufferedArray();

            var count = 0;
            if (value != null)
            {
                foreach (var element in value)
                {
                    var el = element;
                    this.elementInfo.WriteData(collector, ref el);
                    count++;
                }
            }

            collector.EndBufferedArray(bookmark, count);
        }

        public override object GetData(object value)
        {
            var iterType = (IterableType)value;
            List<object> serializedEnumerable = new List<object>();
            foreach (var element in iterType)
            {
                serializedEnumerable.Add(elementInfo.GetData(element));
            }
            return serializedEnumerable.ToArray();
        }
    }
}
