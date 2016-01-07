// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

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
            string name,
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
            IEnumerable enumerable = (IEnumerable)value.ReferenceValue;
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

        public override object GetData(object value)
        {
            var iterType = (IEnumerable)value;
            List<object> serializedEnumerable = new List<object>();
            foreach (var element in iterType)
            {
                serializedEnumerable.Add(elementInfo.GetData(element));
            }
            return serializedEnumerable.ToArray();
        }
    }
}
