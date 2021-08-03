// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: Constants and utility functions.
    /// </summary>
    internal static class Statics
    {
        #region Constants

        public const byte DefaultLevel = 5;
        public const byte TraceLoggingChannel = 0xb;
        public const byte InTypeMask = 31;
        public const byte InTypeFixedCountFlag = 32;
        public const byte InTypeVariableCountFlag = 64;
        public const byte InTypeCustomCountFlag = 96;
        public const byte InTypeCountMask = 96;
        public const byte InTypeChainFlag = 128;
        public const byte OutTypeMask = 127;
        public const byte OutTypeChainFlag = 128;
        public const EventTags EventTagsMask = (EventTags)0xfffffff;

        public static readonly TraceLoggingDataType IntPtrType = IntPtr.Size == 8
            ? TraceLoggingDataType.Int64
            : TraceLoggingDataType.Int32;
        public static readonly TraceLoggingDataType UIntPtrType = IntPtr.Size == 8
            ? TraceLoggingDataType.UInt64
            : TraceLoggingDataType.UInt32;
        public static readonly TraceLoggingDataType HexIntPtrType = IntPtr.Size == 8
            ? TraceLoggingDataType.HexInt64
            : TraceLoggingDataType.HexInt32;

        #endregion

        #region Metadata helpers

        /// <summary>
        /// A complete metadata chunk can be expressed as:
        /// length16 + prefix + null-terminated-utf8-name + suffix + additionalData.
        /// We assume that excludedData will be provided by some other means,
        /// but that its size is known. This function returns a blob containing
        /// length16 + prefix + name + suffix, with prefix and suffix initialized
        /// to 0's. The length16 value is initialized to the length of the returned
        /// blob plus additionalSize, so that the concatenation of the returned blob
        /// plus a blob of size additionalSize constitutes a valid metadata blob.
        /// </summary>
        /// <param name="name">
        /// The name to include in the blob.
        /// </param>
        /// <param name="prefixSize">
        /// Amount of space to reserve before name. For provider or field blobs, this
        /// should be 0. For event blobs, this is used for the tags field and will vary
        /// from 1 to 4, depending on how large the tags field needs to be.
        /// </param>
        /// <param name="suffixSize">
        /// Amount of space to reserve after name. For example, a provider blob with no
        /// traits would reserve 0 extra bytes, but a provider blob with a single GroupId
        /// field would reserve 19 extra bytes.
        /// </param>
        /// <param name="additionalSize">
        /// Amount of additional data in another blob. This value will be counted in the
        /// blob's length field, but will not be included in the returned byte[] object.
        /// The complete blob would then be the concatenation of the returned byte[] object
        /// with another byte[] object of length additionalSize.
        /// </param>
        /// <returns>
        /// A byte[] object with the length and name fields set, with room reserved for
        /// prefix and suffix. If additionalSize was 0, the byte[] object is a complete
        /// blob. Otherwise, another byte[] of size additionalSize must be concatenated
        /// with this one to form a complete blob.
        /// </returns>
        public static byte[] MetadataForString(
            string name,
            int prefixSize,
            int suffixSize,
            int additionalSize)
        {
            Statics.CheckName(name);
            int metadataSize = Encoding.UTF8.GetByteCount(name) + 3 + prefixSize + suffixSize;
            var metadata = new byte[metadataSize];
            ushort totalSize = checked((ushort)(metadataSize + additionalSize));
            metadata[0] = unchecked((byte)totalSize);
            metadata[1] = unchecked((byte)(totalSize >> 8));
            Encoding.UTF8.GetBytes(name, 0, name.Length, metadata, 2 + prefixSize);
            return metadata;
        }

        /// <summary>
        /// Serialize the low 28 bits of the tags value into the metadata stream,
        /// starting at the index given by pos. Updates pos. Writes 1 to 4 bytes,
        /// depending on the value of the tags variable. Usable for event tags and
        /// field tags.
        ///
        /// Note that 'metadata' can be null, in which case it only updates 'pos'.
        /// This is useful for a two pass approach where you figure out how big to
        /// make the array, and then you fill it in.
        /// </summary>
        public static void EncodeTags(int tags, ref int pos, byte[]? metadata)
        {
            // We transmit the low 28 bits of tags, high bits first, 7 bits at a time.
            int tagsLeft = tags & 0xfffffff;
            bool more;
            do
            {
                byte current = (byte)((tagsLeft >> 21) & 0x7f);
                more = (tagsLeft & 0x1fffff) != 0;
                current |= (byte)(more ? 0x80 : 0x00);
                tagsLeft <<= 7;

                if (metadata != null)
                {
                    metadata[pos] = current;
                }
                pos++;
            }
            while (more);
        }

        public static byte Combine(
            int settingValue,
            byte defaultValue)
        {
            unchecked
            {
                return (byte)settingValue == settingValue
                    ? (byte)settingValue
                    : defaultValue;
            }
        }

        public static byte Combine(
            int settingValue1,
            int settingValue2,
            byte defaultValue)
        {
            unchecked
            {
                return (byte)settingValue1 == settingValue1
                    ? (byte)settingValue1
                    : (byte)settingValue2 == settingValue2
                    ? (byte)settingValue2
                    : defaultValue;
            }
        }

        public static int Combine(
            int settingValue1,
            int settingValue2)
        {
            unchecked
            {
                return (byte)settingValue1 == settingValue1
                    ? settingValue1
                    : settingValue2;
            }
        }

        public static void CheckName(string? name)
        {
            if (name != null && 0 <= name.IndexOf('\0'))
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
        }

        public static bool ShouldOverrideFieldName(string fieldName)
        {
            return fieldName.Length <= 2 && fieldName[0] == '_';
        }

        public static TraceLoggingDataType MakeDataType(
            TraceLoggingDataType baseType,
            EventFieldFormat format)
        {
            return (TraceLoggingDataType)(((int)baseType & 0x1f) | ((int)format << 8));
        }

        /// <summary>
        /// Adjusts the native type based on format.
        /// - If format is default, return native.
        /// - If format is recognized, return the canonical type for that format.
        /// - Otherwise remove existing format from native and apply the requested format.
        /// </summary>
        public static TraceLoggingDataType Format8(
            EventFieldFormat format,
            TraceLoggingDataType native)
        {
            return format switch
            {
                EventFieldFormat.Default => native,
                EventFieldFormat.String => TraceLoggingDataType.Char8,
                EventFieldFormat.Boolean => TraceLoggingDataType.Boolean8,
                EventFieldFormat.Hexadecimal => TraceLoggingDataType.HexInt8,
#if false
                EventSourceFieldFormat.Signed => TraceLoggingDataType.Int8,
                EventSourceFieldFormat.Unsigned => TraceLoggingDataType.UInt8,
#endif
                _ => MakeDataType(native, format),
            };
        }

        /// <summary>
        /// Adjusts the native type based on format.
        /// - If format is default, return native.
        /// - If format is recognized, return the canonical type for that format.
        /// - Otherwise remove existing format from native and apply the requested format.
        /// </summary>
        public static TraceLoggingDataType Format16(
            EventFieldFormat format,
            TraceLoggingDataType native)
        {
            return format switch
            {
                EventFieldFormat.Default => native,
                EventFieldFormat.String => TraceLoggingDataType.Char16,
                EventFieldFormat.Hexadecimal => TraceLoggingDataType.HexInt16,
#if false
                EventSourceFieldFormat.Port => TraceLoggingDataType.Port,
                EventSourceFieldFormat.Signed => TraceLoggingDataType.Int16,
                EventSourceFieldFormat.Unsigned => TraceLoggingDataType.UInt16,
#endif
                _ => MakeDataType(native, format),
            };
        }

        /// <summary>
        /// Adjusts the native type based on format.
        /// - If format is default, return native.
        /// - If format is recognized, return the canonical type for that format.
        /// - Otherwise remove existing format from native and apply the requested format.
        /// </summary>
        public static TraceLoggingDataType Format32(
            EventFieldFormat format,
            TraceLoggingDataType native)
        {
            return format switch
            {
                EventFieldFormat.Default => native,
                EventFieldFormat.Boolean => TraceLoggingDataType.Boolean32,
                EventFieldFormat.Hexadecimal => TraceLoggingDataType.HexInt32,
#if false
                EventSourceFieldFormat.Ipv4Address => TraceLoggingDataType.Ipv4Address,
                EventSourceFieldFormat.ProcessId => TraceLoggingDataType.ProcessId,
                EventSourceFieldFormat.ThreadId => TraceLoggingDataType.ThreadId,
                EventSourceFieldFormat.Win32Error => TraceLoggingDataType.Win32Error,
                EventSourceFieldFormat.NTStatus => TraceLoggingDataType.NTStatus,
#endif
                EventFieldFormat.HResult => TraceLoggingDataType.HResult,
#if false
                case EventSourceFieldFormat.Signed:
                    return TraceLoggingDataType.Int32;
                case EventSourceFieldFormat.Unsigned:
                    return TraceLoggingDataType.UInt32;
#endif
                _ => MakeDataType(native, format),
            };
        }

        /// <summary>
        /// Adjusts the native type based on format.
        /// - If format is default, return native.
        /// - If format is recognized, return the canonical type for that format.
        /// - Otherwise remove existing format from native and apply the requested format.
        /// </summary>
        public static TraceLoggingDataType Format64(
            EventFieldFormat format,
            TraceLoggingDataType native)
        {
            return format switch
            {
                EventFieldFormat.Default => native,
                EventFieldFormat.Hexadecimal => TraceLoggingDataType.HexInt64,
#if false
                EventSourceFieldFormat.FileTime => TraceLoggingDataType.FileTime,
                EventSourceFieldFormat.Signed => TraceLoggingDataType.Int64,
                EventSourceFieldFormat.Unsigned => TraceLoggingDataType.UInt64,
#endif
                _ => MakeDataType(native, format),
            };
        }

        /// <summary>
        /// Adjusts the native type based on format.
        /// - If format is default, return native.
        /// - If format is recognized, return the canonical type for that format.
        /// - Otherwise remove existing format from native and apply the requested format.
        /// </summary>
        public static TraceLoggingDataType FormatPtr(
            EventFieldFormat format,
            TraceLoggingDataType native)
        {
            return format switch
            {
                EventFieldFormat.Default => native,
                EventFieldFormat.Hexadecimal => HexIntPtrType,
#if false
                EventSourceFieldFormat.Signed => IntPtrType,
                EventSourceFieldFormat.Unsigned => UIntPtrType,
#endif
                _ => MakeDataType(native, format),
            };
        }

        public static TraceLoggingDataType FormatScalar(EventFieldFormat format, TraceLoggingDataType nativeFormat) =>
            nativeFormat switch
            {
                TraceLoggingDataType.Boolean8 or TraceLoggingDataType.Int8 or TraceLoggingDataType.UInt8 => Format8(format, nativeFormat),
                TraceLoggingDataType.Char16 or TraceLoggingDataType.Int16 or TraceLoggingDataType.UInt16 => Format16(format, nativeFormat),
                TraceLoggingDataType.Int32 or TraceLoggingDataType.UInt32 or TraceLoggingDataType.Float => Format32(format, nativeFormat),
                TraceLoggingDataType.Int64 or TraceLoggingDataType.UInt64 or TraceLoggingDataType.Double => Format64(format, nativeFormat),
                _ => MakeDataType(nativeFormat, format),
            };

        #endregion

        #region Reflection helpers

        public static bool HasCustomAttribute(
            PropertyInfo propInfo,
            Type attributeType)
        {
            return propInfo.IsDefined(attributeType, false);
        }

        public static AttributeType? GetCustomAttribute<AttributeType>(PropertyInfo propInfo)
            where AttributeType : Attribute
        {
            AttributeType? result = null;
            object[] attributes = propInfo.GetCustomAttributes(typeof(AttributeType), false);
            if (attributes.Length != 0)
            {
                result = (AttributeType)attributes[0];
            }
            return result;
        }

        public static AttributeType? GetCustomAttribute<AttributeType>(Type type)
            where AttributeType : Attribute
        {
            AttributeType? result = null;
            object[] attributes = type.GetCustomAttributes(typeof(AttributeType), false);
            if (attributes.Length != 0)
            {
                result = (AttributeType)attributes[0];
            }
            return result;
        }

        public static Type? FindEnumerableElementType(
#if !ES_BUILD_STANDALONE
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
            Type type)
        {
            Type? elementType = null;

            if (IsGenericMatch(type, typeof(IEnumerable<>)))
            {
                elementType = type.GetGenericArguments()[0];
            }
            else
            {
                Type[] ifaceTypes = type.FindInterfaces(IsGenericMatch, typeof(IEnumerable<>));

                foreach (Type ifaceType in ifaceTypes)
                {
                    if (elementType != null)
                    {
                        // ambiguous match. report no match at all.
                        elementType = null;
                        break;
                    }

                    elementType = ifaceType.GetGenericArguments()[0];
                }
            }

            return elementType;
        }

        public static bool IsGenericMatch(Type type, object? openType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == (Type?)openType;
        }

#if !ES_BUILD_STANDALONE
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
#endif
        public static TraceLoggingTypeInfo CreateDefaultTypeInfo(
            Type dataType,
            List<Type> recursionCheck)
        {
            TraceLoggingTypeInfo result;

            if (recursionCheck.Contains(dataType))
            {
                throw new NotSupportedException(SR.EventSource_RecursiveTypeDefinition);
            }

            recursionCheck.Add(dataType);

            EventDataAttribute? eventAttrib = Statics.GetCustomAttribute<EventDataAttribute>(dataType);
            if (eventAttrib != null ||
                Statics.GetCustomAttribute<CompilerGeneratedAttribute>(dataType) != null ||
                IsGenericMatch(dataType, typeof(KeyValuePair<,>)))
            {
                var analysis = new TypeAnalysis(dataType, eventAttrib, recursionCheck);
                result = new InvokeTypeInfo(dataType, analysis);
            }
            else if (dataType.IsArray)
            {
                Type elementType = dataType.GetElementType()!;
                if (elementType == typeof(bool))
                {
                    result = ScalarArrayTypeInfo.Boolean();
                }
                else if (elementType == typeof(byte))
                {
                    result = ScalarArrayTypeInfo.Byte();
                }
                else if (elementType == typeof(sbyte))
                {
                    result = ScalarArrayTypeInfo.SByte();
                }
                else if (elementType == typeof(short))
                {
                    result = ScalarArrayTypeInfo.Int16();
                }
                else if (elementType == typeof(ushort))
                {
                    result = ScalarArrayTypeInfo.UInt16();
                }
                else if (elementType == typeof(int))
                {
                    result = ScalarArrayTypeInfo.Int32();
                }
                else if (elementType == typeof(uint))
                {
                    result = ScalarArrayTypeInfo.UInt32();
                }
                else if (elementType == typeof(long))
                {
                    result = ScalarArrayTypeInfo.Int64();
                }
                else if (elementType == typeof(ulong))
                {
                    result = ScalarArrayTypeInfo.UInt64();
                }
                else if (elementType == typeof(char))
                {
                    result = ScalarArrayTypeInfo.Char();
                }
                else if (elementType == typeof(double))
                {
                    result = ScalarArrayTypeInfo.Double();
                }
                else if (elementType == typeof(float))
                {
                    result = ScalarArrayTypeInfo.Single();
                }
                else if (elementType == typeof(IntPtr))
                {
                    result = ScalarArrayTypeInfo.IntPtr();
                }
                else if (elementType == typeof(UIntPtr))
                {
                    result = ScalarArrayTypeInfo.UIntPtr();
                }
                else if (elementType == typeof(Guid))
                {
                    result = ScalarArrayTypeInfo.Guid();
                }
                else
                {
                    result = new ArrayTypeInfo(dataType, TraceLoggingTypeInfo.GetInstance(elementType, recursionCheck));
                }
            }
            else
            {
                if (dataType.IsEnum)
                    dataType = Enum.GetUnderlyingType(dataType);

                if (dataType == typeof(string))
                {
                    result = StringTypeInfo.Instance();
                }
                else if (dataType == typeof(bool))
                {
                    result = ScalarTypeInfo.Boolean();
                }
                else if (dataType == typeof(byte))
                {
                    result = ScalarTypeInfo.Byte();
                }
                else if (dataType == typeof(sbyte))
                {
                    result = ScalarTypeInfo.SByte();
                }
                else if (dataType == typeof(short))
                {
                    result = ScalarTypeInfo.Int16();
                }
                else if (dataType == typeof(ushort))
                {
                    result = ScalarTypeInfo.UInt16();
                }
                else if (dataType == typeof(int))
                {
                    result = ScalarTypeInfo.Int32();
                }
                else if (dataType == typeof(uint))
                {
                    result = ScalarTypeInfo.UInt32();
                }
                else if (dataType == typeof(long))
                {
                    result = ScalarTypeInfo.Int64();
                }
                else if (dataType == typeof(ulong))
                {
                    result = ScalarTypeInfo.UInt64();
                }
                else if (dataType == typeof(char))
                {
                    result = ScalarTypeInfo.Char();
                }
                else if (dataType == typeof(double))
                {
                    result = ScalarTypeInfo.Double();
                }
                else if (dataType == typeof(float))
                {
                    result = ScalarTypeInfo.Single();
                }
                else if (dataType == typeof(DateTime))
                {
                    result = DateTimeTypeInfo.Instance();
                }
                else if (dataType == typeof(decimal))
                {
                    result = DecimalTypeInfo.Instance();
                }
                else if (dataType == typeof(IntPtr))
                {
                    result = ScalarTypeInfo.IntPtr();
                }
                else if (dataType == typeof(UIntPtr))
                {
                    result = ScalarTypeInfo.UIntPtr();
                }
                else if (dataType == typeof(Guid))
                {
                    result = ScalarTypeInfo.Guid();
                }
                else if (dataType == typeof(TimeSpan))
                {
                    result = TimeSpanTypeInfo.Instance();
                }
                else if (dataType == typeof(DateTimeOffset))
                {
                    result = DateTimeOffsetTypeInfo.Instance();
                }
                else if (dataType == typeof(EmptyStruct))
                {
                    result = NullTypeInfo.Instance();
                }
                else if (IsGenericMatch(dataType, typeof(Nullable<>)))
                {
                    result = new NullableTypeInfo(dataType, recursionCheck);
                }
                else
                {
                    Type? elementType = FindEnumerableElementType(dataType);
                    if (elementType != null)
                    {
                        result = new EnumerableTypeInfo(dataType, TraceLoggingTypeInfo.GetInstance(elementType, recursionCheck));
                    }
                    else
                    {
                        throw new ArgumentException(SR.Format(SR.EventSource_NonCompliantTypeError, dataType.Name));
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
