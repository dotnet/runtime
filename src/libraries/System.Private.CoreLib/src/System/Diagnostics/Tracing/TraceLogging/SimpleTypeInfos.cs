// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// TraceLogging: Type handler for empty or unsupported types.
    /// </summary>
    internal sealed class NullTypeInfo : TraceLoggingTypeInfo
    {
        private static NullTypeInfo? s_instance;

        public NullTypeInfo() : base(typeof(EmptyStruct)) { }

        public static TraceLoggingTypeInfo Instance() => s_instance ??= new NullTypeInfo();

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string? name,
            EventFieldFormat format)
        {
            collector.AddGroup(name);
        }

        public override void WriteData(PropertyValue value)
        {
        }

        public override object? GetData(object? value)
        {
            return null;
        }
    }

    /// <summary>
    /// Type handler for simple scalar types.
    /// </summary>
    internal sealed class ScalarTypeInfo : TraceLoggingTypeInfo
    {
        private static ScalarTypeInfo? s_boolean;
        private static ScalarTypeInfo? s_byte;
        private static ScalarTypeInfo? s_sbyte;
        private static ScalarTypeInfo? s_char;
        private static ScalarTypeInfo? s_int16;
        private static ScalarTypeInfo? s_uint16;
        private static ScalarTypeInfo? s_int32;
        private static ScalarTypeInfo? s_uint32;
        private static ScalarTypeInfo? s_int64;
        private static ScalarTypeInfo? s_uint64;
        private static ScalarTypeInfo? s_intptr;
        private static ScalarTypeInfo? s_uintptr;
        private static ScalarTypeInfo? s_single;
        private static ScalarTypeInfo? s_double;
        private static ScalarTypeInfo? s_guid;

        private readonly TraceLoggingDataType nativeFormat;

        private ScalarTypeInfo(
            Type type,
            TraceLoggingDataType nativeFormat)
            : base(type)
        {
            this.nativeFormat = nativeFormat;
        }

        public override void WriteMetadata(TraceLoggingMetadataCollector collector, string? name, EventFieldFormat format)
        {
            collector.AddScalar(name!, Statics.FormatScalar(format, nativeFormat));
        }

        public override void WriteData(PropertyValue value)
        {
            TraceLoggingDataCollector.AddScalar(value);
        }

        public static TraceLoggingTypeInfo Boolean() => s_boolean ??= new ScalarTypeInfo(typeof(bool), TraceLoggingDataType.Boolean8);
        public static TraceLoggingTypeInfo Byte() => s_byte ??= new ScalarTypeInfo(typeof(byte), TraceLoggingDataType.UInt8);
        public static TraceLoggingTypeInfo SByte() => s_sbyte ??= new ScalarTypeInfo(typeof(sbyte), TraceLoggingDataType.Int8);
        public static TraceLoggingTypeInfo Char() => s_char ??= new ScalarTypeInfo(typeof(char), TraceLoggingDataType.Char16);
        public static TraceLoggingTypeInfo Int16() => s_int16 ??= new ScalarTypeInfo(typeof(short), TraceLoggingDataType.Int16);
        public static TraceLoggingTypeInfo UInt16() => s_uint16 ??= new ScalarTypeInfo(typeof(ushort), TraceLoggingDataType.UInt16);
        public static TraceLoggingTypeInfo Int32() => s_int32 ??= new ScalarTypeInfo(typeof(int), TraceLoggingDataType.Int32);
        public static TraceLoggingTypeInfo UInt32() => s_uint32 ??= new ScalarTypeInfo(typeof(uint), TraceLoggingDataType.UInt32);
        public static TraceLoggingTypeInfo Int64() => s_int64 ??= new ScalarTypeInfo(typeof(long), TraceLoggingDataType.Int64);
        public static TraceLoggingTypeInfo UInt64() => s_uint64 ??= new ScalarTypeInfo(typeof(ulong), TraceLoggingDataType.UInt64);
        public static TraceLoggingTypeInfo IntPtr() => s_intptr ??= new ScalarTypeInfo(typeof(IntPtr), Statics.IntPtrType);
        public static TraceLoggingTypeInfo UIntPtr() => s_uintptr ??= new ScalarTypeInfo(typeof(UIntPtr), Statics.UIntPtrType);
        public static TraceLoggingTypeInfo Single() => s_single ??= new ScalarTypeInfo(typeof(float), TraceLoggingDataType.Float);
        public static TraceLoggingTypeInfo Double() => s_double ??= new ScalarTypeInfo(typeof(double), TraceLoggingDataType.Double);
        public static TraceLoggingTypeInfo Guid() => s_guid ??= new ScalarTypeInfo(typeof(Guid), TraceLoggingDataType.Guid);
    }

    /// <summary>
    /// Type handler for arrays of scalars
    /// </summary>
    internal sealed class ScalarArrayTypeInfo : TraceLoggingTypeInfo
    {
        private static ScalarArrayTypeInfo? s_boolean;
        private static ScalarArrayTypeInfo? s_byte;
        private static ScalarArrayTypeInfo? s_sbyte;
        private static ScalarArrayTypeInfo? s_char;
        private static ScalarArrayTypeInfo? s_int16;
        private static ScalarArrayTypeInfo? s_uint16;
        private static ScalarArrayTypeInfo? s_int32;
        private static ScalarArrayTypeInfo? s_uint32;
        private static ScalarArrayTypeInfo? s_int64;
        private static ScalarArrayTypeInfo? s_uint64;
        private static ScalarArrayTypeInfo? s_intptr;
        private static ScalarArrayTypeInfo? s_uintptr;
        private static ScalarArrayTypeInfo? s_single;
        private static ScalarArrayTypeInfo? s_double;
        private static ScalarArrayTypeInfo? s_guid;

        private readonly TraceLoggingDataType nativeFormat;
        private readonly int elementSize;

        private ScalarArrayTypeInfo(
            Type type,
            TraceLoggingDataType nativeFormat,
            int elementSize)
            : base(type)
        {
            this.nativeFormat = nativeFormat;
            this.elementSize = elementSize;
        }

        public override void WriteMetadata(TraceLoggingMetadataCollector collector, string? name, EventFieldFormat format)
        {
            collector.AddArray(name!, Statics.FormatScalar(format, nativeFormat));
        }

        public override void WriteData(PropertyValue value)
        {
            TraceLoggingDataCollector.AddArray(value, elementSize);
        }

        public static TraceLoggingTypeInfo Boolean() => s_boolean ??= new ScalarArrayTypeInfo(typeof(bool[]), TraceLoggingDataType.Boolean8, sizeof(bool));
        public static TraceLoggingTypeInfo Byte() => s_byte ??= new ScalarArrayTypeInfo(typeof(byte[]), TraceLoggingDataType.UInt8, sizeof(byte));
        public static TraceLoggingTypeInfo SByte() => s_sbyte ??= new ScalarArrayTypeInfo(typeof(sbyte[]), TraceLoggingDataType.Int8, sizeof(sbyte));
        public static TraceLoggingTypeInfo Char() => s_char ??= new ScalarArrayTypeInfo(typeof(char[]), TraceLoggingDataType.Char16, sizeof(char));
        public static TraceLoggingTypeInfo Int16() => s_int16 ??= new ScalarArrayTypeInfo(typeof(short[]), TraceLoggingDataType.Int16, sizeof(short));
        public static TraceLoggingTypeInfo UInt16() => s_uint16 ??= new ScalarArrayTypeInfo(typeof(ushort[]), TraceLoggingDataType.UInt16, sizeof(ushort));
        public static TraceLoggingTypeInfo Int32() => s_int32 ??= new ScalarArrayTypeInfo(typeof(int[]), TraceLoggingDataType.Int32, sizeof(int));
        public static TraceLoggingTypeInfo UInt32() => s_uint32 ??= new ScalarArrayTypeInfo(typeof(uint[]), TraceLoggingDataType.UInt32, sizeof(uint));
        public static TraceLoggingTypeInfo Int64() => s_int64 ??= new ScalarArrayTypeInfo(typeof(long[]), TraceLoggingDataType.Int64, sizeof(long));
        public static TraceLoggingTypeInfo UInt64() => s_uint64 ??= new ScalarArrayTypeInfo(typeof(ulong[]), TraceLoggingDataType.UInt64, sizeof(ulong));
        public static TraceLoggingTypeInfo IntPtr() => s_intptr ??= new ScalarArrayTypeInfo(typeof(IntPtr[]), Statics.IntPtrType, System.IntPtr.Size);
        public static TraceLoggingTypeInfo UIntPtr() => s_uintptr ??= new ScalarArrayTypeInfo(typeof(UIntPtr[]), Statics.UIntPtrType, System.IntPtr.Size);
        public static TraceLoggingTypeInfo Single() => s_single ??= new ScalarArrayTypeInfo(typeof(float[]), TraceLoggingDataType.Float, sizeof(float));
        public static TraceLoggingTypeInfo Double() => s_double ??= new ScalarArrayTypeInfo(typeof(double[]), TraceLoggingDataType.Double, sizeof(double));
        public static unsafe TraceLoggingTypeInfo Guid() => s_guid ??= new ScalarArrayTypeInfo(typeof(Guid[]), TraceLoggingDataType.Guid, sizeof(Guid));
    }

    /// <summary>
    /// TraceLogging: Type handler for String.
    /// </summary>
    internal sealed class StringTypeInfo : TraceLoggingTypeInfo
    {
        private static StringTypeInfo? s_instance;

        public StringTypeInfo() : base(typeof(string)) { }

        public static TraceLoggingTypeInfo Instance() => s_instance ??= new StringTypeInfo();

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string? name,
            EventFieldFormat format)
        {
            // name can be null if the string was used as a top-level object in an event.
            // In that case, use 'message' as the name of the field.
            name ??= "message";

            collector.AddNullTerminatedString(name, Statics.MakeDataType(TraceLoggingDataType.Utf16String, format));
        }

        public override void WriteData(PropertyValue value)
        {
            TraceLoggingDataCollector.AddNullTerminatedString((string?)value.ReferenceValue);
        }

        public override object GetData(object? value)
        {
            if (value == null)
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
        private static DateTimeTypeInfo? s_instance;

        public DateTimeTypeInfo() : base(typeof(DateTime)) { }

        public static TraceLoggingTypeInfo Instance() => s_instance ??= new DateTimeTypeInfo();

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string? name,
            EventFieldFormat format)
        {
            collector.AddScalar(name!, Statics.MakeDataType(TraceLoggingDataType.FileTime, format));
        }

        public override void WriteData(PropertyValue value)
        {
            DateTime dateTime = value.ScalarValue.AsDateTime;
            const long UTCMinTicks = 504911232000000000;
            long dateTimeTicks = 0;
            // We cannot translate dates sooner than 1/1/1601 in UTC.
            // To avoid getting an ArgumentOutOfRangeException we compare with 1/1/1601 DateTime ticks
            if (dateTime.Ticks > UTCMinTicks)
                dateTimeTicks = dateTime.ToFileTimeUtc();
            TraceLoggingDataCollector.AddScalar(dateTimeTicks);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for DateTimeOffset.
    /// </summary>
    internal sealed class DateTimeOffsetTypeInfo : TraceLoggingTypeInfo
    {
        private static DateTimeOffsetTypeInfo? s_instance;

        public DateTimeOffsetTypeInfo() : base(typeof(DateTimeOffset)) { }

        public static TraceLoggingTypeInfo Instance() => s_instance ??= new DateTimeOffsetTypeInfo();

        public override void WriteMetadata(TraceLoggingMetadataCollector collector, string? name, EventFieldFormat format)
        {
            TraceLoggingMetadataCollector group = collector.AddGroup(name);
            group.AddScalar("Ticks", Statics.MakeDataType(TraceLoggingDataType.FileTime, format));
            group.AddScalar("Offset", TraceLoggingDataType.Int64);
        }

        public override void WriteData(PropertyValue value)
        {
            DateTimeOffset dateTimeOffset = value.ScalarValue.AsDateTimeOffset;
            long ticks = dateTimeOffset.Ticks;
            TraceLoggingDataCollector.AddScalar(ticks < 504911232000000000 ? 0 : ticks - 504911232000000000);
            TraceLoggingDataCollector.AddScalar(dateTimeOffset.Offset.Ticks);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for TimeSpan.
    /// </summary>
    internal sealed class TimeSpanTypeInfo : TraceLoggingTypeInfo
    {
        private static TimeSpanTypeInfo? s_instance;

        public TimeSpanTypeInfo() : base(typeof(TimeSpan)) { }

        public static TraceLoggingTypeInfo Instance() => s_instance ??= new TimeSpanTypeInfo();

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string? name,
            EventFieldFormat format)
        {
            collector.AddScalar(name!, Statics.MakeDataType(TraceLoggingDataType.Int64, format));
        }

        public override void WriteData(PropertyValue value)
        {
            TraceLoggingDataCollector.AddScalar(value.ScalarValue.AsTimeSpan.Ticks);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for decimal. (Note: not full-fidelity, exposed as Double.)
    /// </summary>
    internal sealed class DecimalTypeInfo : TraceLoggingTypeInfo
    {
        private static DecimalTypeInfo? s_instance;

        public DecimalTypeInfo() : base(typeof(decimal)) { }

        public static TraceLoggingTypeInfo Instance() => s_instance ??= new DecimalTypeInfo();

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string? name,
            EventFieldFormat format)
        {
            collector.AddScalar(name!, Statics.MakeDataType(TraceLoggingDataType.Double, format));
        }

        public override void WriteData(PropertyValue value)
        {
            TraceLoggingDataCollector.AddScalar((double)value.ScalarValue.AsDecimal);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Nullable.
    /// </summary>
    internal sealed class NullableTypeInfo : TraceLoggingTypeInfo
    {
        private readonly TraceLoggingTypeInfo valueInfo;

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
        public NullableTypeInfo(Type type, List<Type> recursionCheck)
            : base(type)
        {
            Type[] typeArgs = type.GenericTypeArguments;
            Debug.Assert(typeArgs.Length == 1);
            this.valueInfo = TraceLoggingTypeInfo.GetInstance(typeArgs[0], recursionCheck);
        }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string? name,
            EventFieldFormat format)
        {
            TraceLoggingMetadataCollector group = collector.AddGroup(name);
            group.AddScalar("HasValue", TraceLoggingDataType.Boolean8);
            this.valueInfo.WriteMetadata(group, "Value", format);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern",
                Justification = "The underlying type of Nullable<T> must be defaultable")]
        public override void WriteData(PropertyValue value)
        {
            object? refVal = value.ReferenceValue;
            bool hasValue = refVal is not null;
            TraceLoggingDataCollector.AddScalar(hasValue);
            PropertyValue val = valueInfo.PropertyValueFactory(hasValue
                ? refVal
                : RuntimeHelpers.GetUninitializedObject(valueInfo.DataType));
            this.valueInfo.WriteData(val);
        }
    }
}
