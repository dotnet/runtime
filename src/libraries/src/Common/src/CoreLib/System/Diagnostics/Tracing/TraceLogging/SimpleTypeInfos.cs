// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

#if !ES_BUILD_AGAINST_DOTNET_V35
using Contract = System.Diagnostics.Contracts.Contract;
#else
using Contract = Microsoft.Diagnostics.Contracts.Internal.Contract;
#endif

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: Type handler for empty or unsupported types.
    /// </summary>
    internal sealed class NullTypeInfo : TraceLoggingTypeInfo
    {
        public NullTypeInfo() : base(typeof(EmptyStruct)) { }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddGroup(name);
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            return;
        }

        public override object GetData(object value)
        {
            return null;
        }
    }

    /// <summary>
    /// Type handler for simple scalar types.
    /// </summary>
    sealed class ScalarTypeInfo : TraceLoggingTypeInfo
    {
        Func<EventFieldFormat, TraceLoggingDataType, TraceLoggingDataType> formatFunc;
        TraceLoggingDataType nativeFormat;

        private ScalarTypeInfo(
            Type type,
            Func<EventFieldFormat, TraceLoggingDataType, TraceLoggingDataType> formatFunc,
            TraceLoggingDataType nativeFormat) 
            : base(type)
        {
            this.formatFunc = formatFunc;
            this.nativeFormat = nativeFormat;
        }

        public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
        {
            collector.AddScalar(name, formatFunc(format, nativeFormat));
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            collector.AddScalar(value);
        }

        public static TraceLoggingTypeInfo Boolean() { return new ScalarTypeInfo(typeof(Boolean), Statics.Format8, TraceLoggingDataType.Boolean8); }
        public static TraceLoggingTypeInfo Byte() { return new ScalarTypeInfo(typeof(Byte), Statics.Format8, TraceLoggingDataType.UInt8); }
        public static TraceLoggingTypeInfo SByte() { return new ScalarTypeInfo(typeof(SByte), Statics.Format8, TraceLoggingDataType.Int8); }
        public static TraceLoggingTypeInfo Char() { return new ScalarTypeInfo(typeof(Char), Statics.Format16, TraceLoggingDataType.Char16); }
        public static TraceLoggingTypeInfo Int16() { return new ScalarTypeInfo(typeof(Int16), Statics.Format16, TraceLoggingDataType.Int16); }
        public static TraceLoggingTypeInfo UInt16() { return new ScalarTypeInfo(typeof(UInt16), Statics.Format16, TraceLoggingDataType.UInt16); }
        public static TraceLoggingTypeInfo Int32() { return new ScalarTypeInfo(typeof(Int32), Statics.Format32, TraceLoggingDataType.Int32); }
        public static TraceLoggingTypeInfo UInt32() { return new ScalarTypeInfo(typeof(UInt32), Statics.Format32, TraceLoggingDataType.UInt32); }
        public static TraceLoggingTypeInfo Int64() { return new ScalarTypeInfo(typeof(Int64), Statics.Format64, TraceLoggingDataType.Int64); }
        public static TraceLoggingTypeInfo UInt64() { return new ScalarTypeInfo(typeof(UInt64), Statics.Format64, TraceLoggingDataType.UInt64); }
        public static TraceLoggingTypeInfo IntPtr() { return new ScalarTypeInfo(typeof(IntPtr), Statics.FormatPtr, Statics.IntPtrType); }
        public static TraceLoggingTypeInfo UIntPtr() { return new ScalarTypeInfo(typeof(UIntPtr), Statics.FormatPtr, Statics.UIntPtrType); }
        public static TraceLoggingTypeInfo Single() { return new ScalarTypeInfo(typeof(Single), Statics.Format32, TraceLoggingDataType.Float); }
        public static TraceLoggingTypeInfo Double() { return new ScalarTypeInfo(typeof(Double), Statics.Format64, TraceLoggingDataType.Double); }
        public static TraceLoggingTypeInfo Guid() { return new ScalarTypeInfo(typeof(Guid), (f, t) => Statics.MakeDataType(TraceLoggingDataType.Guid, f), TraceLoggingDataType.Guid); }
    }


    /// <summary>
    /// Type handler for arrays of scalars
    /// </summary>
    internal sealed class ScalarArrayTypeInfo : TraceLoggingTypeInfo
    {
        Func<EventFieldFormat, TraceLoggingDataType, TraceLoggingDataType> formatFunc;
        TraceLoggingDataType nativeFormat;
        int elementSize;

        private ScalarArrayTypeInfo(
            Type type,
            Func<EventFieldFormat, TraceLoggingDataType, TraceLoggingDataType> formatFunc,
            TraceLoggingDataType nativeFormat,
            int elementSize) 
            : base(type)
        {
            this.formatFunc = formatFunc;
            this.nativeFormat = nativeFormat;
            this.elementSize = elementSize;
        }

        public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
        {
            collector.AddArray(name, formatFunc(format, nativeFormat));
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            collector.AddArray(value, elementSize);
        }

        public static TraceLoggingTypeInfo Boolean() { return new ScalarArrayTypeInfo(typeof(Boolean[]), Statics.Format8, TraceLoggingDataType.Boolean8, sizeof(Boolean)); }
        public static TraceLoggingTypeInfo Byte() { return new ScalarArrayTypeInfo(typeof(Byte[]), Statics.Format8, TraceLoggingDataType.UInt8, sizeof(Byte)); }
        public static TraceLoggingTypeInfo SByte() { return new ScalarArrayTypeInfo(typeof(SByte[]), Statics.Format8, TraceLoggingDataType.Int8, sizeof(SByte)); }
        public static TraceLoggingTypeInfo Char() { return new ScalarArrayTypeInfo(typeof(Char[]), Statics.Format16, TraceLoggingDataType.Char16, sizeof(Char)); }
        public static TraceLoggingTypeInfo Int16() { return new ScalarArrayTypeInfo(typeof(Int16[]), Statics.Format16, TraceLoggingDataType.Int16, sizeof(Int16)); }
        public static TraceLoggingTypeInfo UInt16() { return new ScalarArrayTypeInfo(typeof(UInt16[]), Statics.Format16, TraceLoggingDataType.UInt16, sizeof(UInt16)); }
        public static TraceLoggingTypeInfo Int32() { return new ScalarArrayTypeInfo(typeof(Int32[]), Statics.Format32, TraceLoggingDataType.Int32, sizeof(Int32)); }
        public static TraceLoggingTypeInfo UInt32() { return new ScalarArrayTypeInfo(typeof(UInt32[]), Statics.Format32, TraceLoggingDataType.UInt32, sizeof(UInt32)); }
        public static TraceLoggingTypeInfo Int64() { return new ScalarArrayTypeInfo(typeof(Int64[]), Statics.Format64, TraceLoggingDataType.Int64, sizeof(Int64)); }
        public static TraceLoggingTypeInfo UInt64() { return new ScalarArrayTypeInfo(typeof(UInt64[]), Statics.Format64, TraceLoggingDataType.UInt64, sizeof(UInt64)); }
        public static TraceLoggingTypeInfo IntPtr() { return new ScalarArrayTypeInfo(typeof(IntPtr[]), Statics.FormatPtr, Statics.IntPtrType, System.IntPtr.Size); }
        public static TraceLoggingTypeInfo UIntPtr() { return new ScalarArrayTypeInfo(typeof(UIntPtr[]), Statics.FormatPtr, Statics.UIntPtrType, System.IntPtr.Size); }
        public static TraceLoggingTypeInfo Single() { return new ScalarArrayTypeInfo(typeof(Single[]), Statics.Format32, TraceLoggingDataType.Float, sizeof(Single)); }
        public static TraceLoggingTypeInfo Double() { return new ScalarArrayTypeInfo(typeof(Double[]), Statics.Format64, TraceLoggingDataType.Double, sizeof(Double)); }
        public unsafe static TraceLoggingTypeInfo Guid() { return new ScalarArrayTypeInfo(typeof(Guid), (f, t) => Statics.MakeDataType(TraceLoggingDataType.Guid, f), TraceLoggingDataType.Guid, sizeof(Guid)); }
    }

    /// <summary>
    /// TraceLogging: Type handler for String.
    /// </summary>
    internal sealed class StringTypeInfo : TraceLoggingTypeInfo
    {
        public StringTypeInfo() : base(typeof(string)) { }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddBinary(name, Statics.MakeDataType(TraceLoggingDataType.CountedUtf16String, format));
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            collector.AddBinary((string)value.ReferenceValue);
        }
        
        public override object GetData(object value)
        {
            if(value == null)
            {
                return "";
            }
            
            return value;
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for DateTime.
    /// </summary>
    internal sealed class DateTimeTypeInfo : TraceLoggingTypeInfo
    {
        public DateTimeTypeInfo() : base(typeof(DateTime)) { }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.FileTime, format));
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            var ticks = value.ScalarValue.AsDateTime.Ticks;
            collector.AddScalar(ticks < 504911232000000000 ? 0 : ticks - 504911232000000000);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for DateTimeOffset.
    /// </summary>
    internal sealed class DateTimeOffsetTypeInfo : TraceLoggingTypeInfo
    {
        public DateTimeOffsetTypeInfo() : base(typeof(DateTimeOffset)) { }

        public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
        {
            var group = collector.AddGroup(name);
            group.AddScalar("Ticks", Statics.MakeDataType(TraceLoggingDataType.FileTime, format));
            group.AddScalar("Offset", TraceLoggingDataType.Int64);
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            var dateTimeOffset = value.ScalarValue.AsDateTimeOffset;
            var ticks = dateTimeOffset.Ticks;
            collector.AddScalar(ticks < 504911232000000000 ? 0 : ticks - 504911232000000000);
            collector.AddScalar(dateTimeOffset.Offset.Ticks);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for TimeSpan.
    /// </summary>
    internal sealed class TimeSpanTypeInfo : TraceLoggingTypeInfo
    {
        public TimeSpanTypeInfo() : base(typeof(TimeSpan)) { }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.Int64, format));
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            collector.AddScalar(value.ScalarValue.AsTimeSpan.Ticks);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Decimal. (Note: not full-fidelity, exposed as Double.)
    /// </summary>
    internal sealed class DecimalTypeInfo : TraceLoggingTypeInfo
    {
        public DecimalTypeInfo() : base(typeof(Decimal)) { }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.Double, format));
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            collector.AddScalar((double)value.ScalarValue.AsDecimal);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Nullable.
    /// </summary>
    internal sealed class NullableTypeInfo : TraceLoggingTypeInfo
    {
        private readonly TraceLoggingTypeInfo valueInfo;
        private readonly Func<PropertyValue, PropertyValue> hasValueGetter;
        private readonly Func<PropertyValue, PropertyValue> valueGetter;

        public NullableTypeInfo(Type type, List<Type> recursionCheck)
            : base(type)
        {
            var typeArgs = type.GenericTypeArguments;
            Debug.Assert(typeArgs.Length == 1);
            this.valueInfo = TraceLoggingTypeInfo.GetInstance(typeArgs[0], recursionCheck);
            this.hasValueGetter = PropertyValue.GetPropertyGetter(type.GetTypeInfo().GetDeclaredProperty("HasValue"));
            this.valueGetter = PropertyValue.GetPropertyGetter(type.GetTypeInfo().GetDeclaredProperty("Value"));
        }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            var group = collector.AddGroup(name);
            group.AddScalar("HasValue", TraceLoggingDataType.Boolean8);
            this.valueInfo.WriteMetadata(group, "Value", format);
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            var hasValue = hasValueGetter(value);
            collector.AddScalar(hasValue);
            var val = hasValue.ScalarValue.AsBoolean ? valueGetter(value) : valueInfo.PropertyValueFactory(Activator.CreateInstance(valueInfo.DataType));
            this.valueInfo.WriteData(collector, val);
        }
    }
}
