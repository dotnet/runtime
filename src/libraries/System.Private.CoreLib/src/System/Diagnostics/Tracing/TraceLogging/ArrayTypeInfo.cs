// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Tracing
{
    internal sealed class ArrayTypeInfo : TraceLoggingTypeInfo
    {
        private readonly TraceLoggingTypeInfo elementInfo;

        public ArrayTypeInfo(Type type, TraceLoggingTypeInfo elementInfo)
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

        public override void WriteData(PropertyValue value)
        {
            int bookmark = TraceLoggingDataCollector.BeginBufferedArray();

            int count = 0;
            Array? array = (Array?)value.ReferenceValue;
            if (array != null)
            {
                count = array.Length;
                for (int i = 0; i < array.Length; i++)
                {
                    this.elementInfo.WriteData(elementInfo.PropertyValueFactory(array.GetValue(i)));
                }
            }

            TraceLoggingDataCollector.EndBufferedArray(bookmark, count);
        }

        public override object? GetData(object? value)
        {
            Debug.Assert(value != null, "null accepted only for some overrides");
            var array = (Array)value;
            var serializedArray = new object?[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                serializedArray[i] = this.elementInfo.GetData(array.GetValue(i));
            }
            return serializedArray;
        }
    }
}
