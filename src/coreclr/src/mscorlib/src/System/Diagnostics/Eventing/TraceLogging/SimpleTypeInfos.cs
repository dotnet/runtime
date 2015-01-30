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
    #region NullTypeInfo

    /// <summary>
    /// TraceLogging: Type handler for empty or unsupported types.
    /// </summary>
    /// <typeparam name="DataType">The type to handle.</typeparam>
    internal sealed class NullTypeInfo<DataType>
        : TraceLoggingTypeInfo<DataType>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddGroup(name);
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref DataType value)
        {
            return;
        }

        public override object GetData(object value)
        {
            return null;
        }
    }

    #endregion

    #region Primitive scalars

    /// <summary>
    /// TraceLogging: Type handler for Boolean.
    /// </summary>
    internal sealed class BooleanTypeInfo
        : TraceLoggingTypeInfo<Boolean>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format8(format, TraceLoggingDataType.Boolean8));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Boolean value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Byte.
    /// </summary>
    internal sealed class ByteTypeInfo
        : TraceLoggingTypeInfo<Byte>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format8(format, TraceLoggingDataType.UInt8));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Byte value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for SByte.
    /// </summary>
    internal sealed class SByteTypeInfo
        : TraceLoggingTypeInfo<SByte>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format8(format, TraceLoggingDataType.Int8));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref SByte value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Int16.
    /// </summary>
    internal sealed class Int16TypeInfo
        : TraceLoggingTypeInfo<Int16>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format16(format, TraceLoggingDataType.Int16));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Int16 value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for UInt16.
    /// </summary>
    internal sealed class UInt16TypeInfo
        : TraceLoggingTypeInfo<UInt16>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format16(format, TraceLoggingDataType.UInt16));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref UInt16 value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Int32.
    /// </summary>
    internal sealed class Int32TypeInfo
        : TraceLoggingTypeInfo<Int32>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format32(format, TraceLoggingDataType.Int32));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Int32 value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for UInt32.
    /// </summary>
    internal sealed class UInt32TypeInfo
        : TraceLoggingTypeInfo<UInt32>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format32(format, TraceLoggingDataType.UInt32));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref UInt32 value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Int64.
    /// </summary>
    internal sealed class Int64TypeInfo
        : TraceLoggingTypeInfo<Int64>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format64(format, TraceLoggingDataType.Int64));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Int64 value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for UInt64.
    /// </summary>
    internal sealed class UInt64TypeInfo
        : TraceLoggingTypeInfo<UInt64>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format64(format, TraceLoggingDataType.UInt64));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref UInt64 value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for IntPtr.
    /// </summary>
    internal sealed class IntPtrTypeInfo
        : TraceLoggingTypeInfo<IntPtr>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.FormatPtr(format, Statics.IntPtrType));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref IntPtr value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for UIntPtr.
    /// </summary>
    internal sealed class UIntPtrTypeInfo
        : TraceLoggingTypeInfo<UIntPtr>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.FormatPtr(format, Statics.UIntPtrType));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref UIntPtr value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Double.
    /// </summary>
    internal sealed class DoubleTypeInfo
        : TraceLoggingTypeInfo<Double>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format64(format, TraceLoggingDataType.Double));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Double value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Single.
    /// </summary>
    internal sealed class SingleTypeInfo
        : TraceLoggingTypeInfo<Single>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format32(format, TraceLoggingDataType.Float));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Single value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Char.
    /// </summary>
    internal sealed class CharTypeInfo
        : TraceLoggingTypeInfo<Char>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format16(format, TraceLoggingDataType.Char16));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Char value)
        {
            collector.AddScalar(value);
        }
    }

    #endregion

    #region Primitive arrays

    /// <summary>
    /// TraceLogging: Type handler for Boolean[].
    /// </summary>
    internal sealed class BooleanArrayTypeInfo
        : TraceLoggingTypeInfo<Boolean[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format8(format, TraceLoggingDataType.Boolean8));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Boolean[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Byte[].
    /// </summary>
    internal sealed class ByteArrayTypeInfo
        : TraceLoggingTypeInfo<Byte[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            switch (format)
            {
                default:
                    collector.AddBinary(name, Statics.MakeDataType(TraceLoggingDataType.Binary, format));
                    break;
                case EventFieldFormat.String:
                    collector.AddBinary(name, TraceLoggingDataType.CountedMbcsString);
                    break;
                case EventFieldFormat.Xml:
                    collector.AddBinary(name, TraceLoggingDataType.CountedMbcsXml);
                    break;
                case EventFieldFormat.Json:
                    collector.AddBinary(name, TraceLoggingDataType.CountedMbcsJson);
                    break;
                case EventFieldFormat.Boolean:
                    collector.AddArray(name, TraceLoggingDataType.Boolean8);
                    break;
                case EventFieldFormat.Hexadecimal:
                    collector.AddArray(name, TraceLoggingDataType.HexInt8);
                    break;
#if false 
                case EventSourceFieldFormat.Signed:
                    collector.AddArray(name, TraceLoggingDataType.Int8);
                    break;
                case EventSourceFieldFormat.Unsigned:
                    collector.AddArray(name, TraceLoggingDataType.UInt8);
                    break;
#endif 
            }
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Byte[] value)
        {
            collector.AddBinary(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for SByte[].
    /// </summary>
    internal sealed class SByteArrayTypeInfo
        : TraceLoggingTypeInfo<SByte[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format8(format, TraceLoggingDataType.Int8));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref SByte[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Int16[].
    /// </summary>
    internal sealed class Int16ArrayTypeInfo
        : TraceLoggingTypeInfo<Int16[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format16(format, TraceLoggingDataType.Int16));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Int16[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for UInt16[].
    /// </summary>
    internal sealed class UInt16ArrayTypeInfo
        : TraceLoggingTypeInfo<UInt16[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format16(format, TraceLoggingDataType.UInt16));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref UInt16[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Int32[].
    /// </summary>
    internal sealed class Int32ArrayTypeInfo
        : TraceLoggingTypeInfo<Int32[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format32(format, TraceLoggingDataType.Int32));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Int32[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for UInt32[].
    /// </summary>
    internal sealed class UInt32ArrayTypeInfo
        : TraceLoggingTypeInfo<UInt32[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format32(format, TraceLoggingDataType.UInt32));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref UInt32[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Int64[].
    /// </summary>
    internal sealed class Int64ArrayTypeInfo
        : TraceLoggingTypeInfo<Int64[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format64(format, TraceLoggingDataType.Int64));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Int64[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for UInt64[].
    /// </summary>
    internal sealed class UInt64ArrayTypeInfo
        : TraceLoggingTypeInfo<UInt64[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format64(format, TraceLoggingDataType.UInt64));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref UInt64[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for IntPtr[].
    /// </summary>
    internal sealed class IntPtrArrayTypeInfo
        : TraceLoggingTypeInfo<IntPtr[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.FormatPtr(format, Statics.IntPtrType));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref IntPtr[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for UIntPtr[].
    /// </summary>
    internal sealed class UIntPtrArrayTypeInfo
        : TraceLoggingTypeInfo<UIntPtr[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.FormatPtr(format, Statics.UIntPtrType));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref UIntPtr[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Char[].
    /// </summary>
    internal sealed class CharArrayTypeInfo
        : TraceLoggingTypeInfo<Char[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format16(format, TraceLoggingDataType.Char16));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Char[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Double[].
    /// </summary>
    internal sealed class DoubleArrayTypeInfo
        : TraceLoggingTypeInfo<Double[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format64(format, TraceLoggingDataType.Double));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Double[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Single[].
    /// </summary>
    internal sealed class SingleArrayTypeInfo
    : TraceLoggingTypeInfo<Single[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.Format32(format, TraceLoggingDataType.Float));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Single[] value)
        {
            collector.AddArray(value);
        }
    }

    #endregion

    #region Enum scalars

    internal sealed class EnumByteTypeInfo<EnumType>
        : TraceLoggingTypeInfo<EnumType>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format8(format, TraceLoggingDataType.UInt8));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
        {
            collector.AddScalar(EnumHelper<Byte>.Cast(value));
        }

        public override object GetData(object value)
        {
            return (Byte)value;
        }
    }

    internal sealed class EnumSByteTypeInfo<EnumType>
        : TraceLoggingTypeInfo<EnumType>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format8(format, TraceLoggingDataType.Int8));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
        {
            collector.AddScalar(EnumHelper<SByte>.Cast(value));
        }

        public override object GetData(object value)
        {
            return (SByte)value;
        }
    }

    internal sealed class EnumInt16TypeInfo<EnumType>
        : TraceLoggingTypeInfo<EnumType>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format16(format, TraceLoggingDataType.Int16));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
        {
            collector.AddScalar(EnumHelper<Int16>.Cast(value));
        }

        public override object GetData(object value)
        {
            return (Int16)value;
        }
    }

    internal sealed class EnumUInt16TypeInfo<EnumType>
        : TraceLoggingTypeInfo<EnumType>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format16(format, TraceLoggingDataType.UInt16));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
        {
            collector.AddScalar(EnumHelper<UInt16>.Cast(value));
        }

        public override object GetData(object value)
        {
            return (UInt16)value;
        }
    }

    internal sealed class EnumInt32TypeInfo<EnumType>
        : TraceLoggingTypeInfo<EnumType>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format32(format, TraceLoggingDataType.Int32));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
        {
            collector.AddScalar(EnumHelper<Int32>.Cast(value));
        }

        public override object GetData(object value)
        {
            return (Int32)value;
        }
    }

    internal sealed class EnumUInt32TypeInfo<EnumType>
        : TraceLoggingTypeInfo<EnumType>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format32(format, TraceLoggingDataType.UInt32));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
        {
            collector.AddScalar(EnumHelper<UInt32>.Cast(value));
        }

        public override object GetData(object value)
        {
            return (UInt32)value;
        }
    }

    internal sealed class EnumInt64TypeInfo<EnumType>
        : TraceLoggingTypeInfo<EnumType>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format64(format, TraceLoggingDataType.Int64));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
        {
            collector.AddScalar(EnumHelper<Int64>.Cast(value));
        }

        public override object GetData(object value)
        {
            return (Int64)value;
        }
    }

    internal sealed class EnumUInt64TypeInfo<EnumType>
        : TraceLoggingTypeInfo<EnumType>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.Format64(format, TraceLoggingDataType.UInt64));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
        {
            collector.AddScalar(EnumHelper<UInt64>.Cast(value));
        }

        public override object GetData(object value)
        {
            return (UInt64)value;
        }
    }

    #endregion

    #region Other built-in types

    /// <summary>
    /// TraceLogging: Type handler for String.
    /// </summary>
    internal sealed class StringTypeInfo
        : TraceLoggingTypeInfo<String>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddBinary(name, Statics.MakeDataType(TraceLoggingDataType.CountedUtf16String, format));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref String value)
        {
            collector.AddBinary(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Guid.
    /// </summary>
    internal sealed class GuidTypeInfo
        : TraceLoggingTypeInfo<Guid>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.Guid, format));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Guid value)
        {
            collector.AddScalar(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Guid[].
    /// </summary>
    internal sealed class GuidArrayTypeInfo
    : TraceLoggingTypeInfo<Guid[]>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddArray(name, Statics.MakeDataType(TraceLoggingDataType.Guid, format));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref Guid[] value)
        {
            collector.AddArray(value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for DateTime.
    /// </summary>
    internal sealed class DateTimeTypeInfo
        : TraceLoggingTypeInfo<DateTime>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.FileTime, format));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref DateTime value)
        {
            var ticks = value.Ticks;
            collector.AddScalar(ticks < 504911232000000000 ? 0 : ticks - 504911232000000000);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for DateTimeOffset.
    /// </summary>
    internal sealed class DateTimeOffsetTypeInfo
        : TraceLoggingTypeInfo<DateTimeOffset>
    {
        public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
        {
            var group = collector.AddGroup(name);
            group.AddScalar("Ticks", Statics.MakeDataType(TraceLoggingDataType.FileTime, format));
            group.AddScalar("Offset", TraceLoggingDataType.Int64);
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref DateTimeOffset value)
        {
            var ticks = value.Ticks;
            collector.AddScalar(ticks < 504911232000000000 ? 0 : ticks - 504911232000000000);
            collector.AddScalar(value.Offset.Ticks);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for TimeSpan.
    /// </summary>
    internal sealed class TimeSpanTypeInfo
        : TraceLoggingTypeInfo<TimeSpan>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.Int64, format));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref TimeSpan value)
        {
            collector.AddScalar(value.Ticks);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Decimal. (Note: not full-fidelity, exposed as Double.)
    /// </summary>
    internal sealed class DecimalTypeInfo
        : TraceLoggingTypeInfo<Decimal>
    {
        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.Double, format));
        }

        public override void WriteData(TraceLoggingDataCollector collector, ref decimal value)
        {
            collector.AddScalar((double)value);
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for KeyValuePair.
    /// </summary>
    /// <typeparam name="K">Type of the KeyValuePair's Key property.</typeparam>
    /// <typeparam name="V">Type of the KeyValuePair's Value property.</typeparam>
    internal sealed class KeyValuePairTypeInfo<K, V>
        : TraceLoggingTypeInfo<KeyValuePair<K, V>>
    {
        private readonly TraceLoggingTypeInfo<K> keyInfo;
        private readonly TraceLoggingTypeInfo<V> valueInfo;

        public KeyValuePairTypeInfo(List<Type> recursionCheck)
        {
            this.keyInfo = TraceLoggingTypeInfo<K>.GetInstance(recursionCheck);
            this.valueInfo = TraceLoggingTypeInfo<V>.GetInstance(recursionCheck);
        }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            var group = collector.AddGroup(name);
            this.keyInfo.WriteMetadata(group, "Key", EventFieldFormat.Default);
            this.valueInfo.WriteMetadata(group, "Value", format);
        }

        public override void WriteData(
            TraceLoggingDataCollector collector,
            ref KeyValuePair<K, V> value)
        {
            var key = value.Key;
            var val = value.Value;
            this.keyInfo.WriteData(collector, ref key);
            this.valueInfo.WriteData(collector, ref val);
        }

        public override object GetData(object value)
        {
            var serializedType = new Dictionary<string, object>();
            var keyValuePair = (KeyValuePair<K, V>) value;
            serializedType.Add("Key", this.keyInfo.GetData(keyValuePair.Key));
            serializedType.Add("Value", this.valueInfo.GetData(keyValuePair.Value));
            return serializedType;
        }
    }

    /// <summary>
    /// TraceLogging: Type handler for Nullable.
    /// </summary>
    /// <typeparam name="T">Type of the Nullable's Value property.</typeparam>
    internal sealed class NullableTypeInfo<T>
        : TraceLoggingTypeInfo<Nullable<T>>
        where T : struct
    {
        private readonly TraceLoggingTypeInfo<T> valueInfo;

        public NullableTypeInfo(List<Type> recursionCheck)
        {
            this.valueInfo = TraceLoggingTypeInfo<T>.GetInstance(recursionCheck);
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

        public override void WriteData(
            TraceLoggingDataCollector collector,
            ref Nullable<T> value)
        {
            var hasValue = value.HasValue;
            collector.AddScalar(hasValue);
            var val = hasValue ? value.Value : default(T);
            this.valueInfo.WriteData(collector, ref val);
        }
    }

    #endregion
}
