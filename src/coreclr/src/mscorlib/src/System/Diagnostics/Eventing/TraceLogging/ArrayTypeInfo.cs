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
    internal sealed class ArrayTypeInfo<ElementType>
        : TraceLoggingTypeInfo<ElementType[]>
    {
        private readonly TraceLoggingTypeInfo<ElementType> elementInfo;

        public ArrayTypeInfo(TraceLoggingTypeInfo<ElementType> elementInfo)
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
            ref ElementType[] value)
        {
            var bookmark = collector.BeginBufferedArray();

            var count = 0;
            if (value != null)
            {
                count = value.Length;
                for (int i = 0; i < value.Length; i++)
                {
                    this.elementInfo.WriteData(collector, ref value[i]);
                }
            }

            collector.EndBufferedArray(bookmark, count);
        }

        public override object GetData(object value)
        {
            var array = (ElementType[])value;
            var serializedArray = new object[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                serializedArray[i] = this.elementInfo.GetData(array[i]);
            }
            return serializedArray;
        }
    }
}
