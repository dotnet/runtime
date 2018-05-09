// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using Encoding = System.Text.Encoding;

using Microsoft.Reflection;

#if ES_BUILD_STANDALONE
using Environment = Microsoft.Diagnostics.Tracing.Internal.Environment;
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
        public static void EncodeTags(int tags, ref int pos, byte[] metadata)
        {
            // We transmit the low 28 bits of tags, high bits first, 7 bits at a time.
            var tagsLeft = tags & 0xfffffff;
            bool more;
            do
            {
                byte current = (byte)((tagsLeft >> 21) & 0x7f);
                more = (tagsLeft & 0x1fffff) != 0;
                current |= (byte)(more ? 0x80 : 0x00);
                tagsLeft = tagsLeft << 7;

                if (metadata != null)
                {
                    metadata[pos] = current;
                }
                pos += 1;
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

        public static void CheckName(string name)
        {
            if (name != null && 0 <= name.IndexOf('\0'))
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
        }

        public static bool ShouldOverrideFieldName(string fieldName)
        {
            return (fieldName.Length <= 2 && fieldName[0] == '_');
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
            switch (format)
            {
                case EventFieldFormat.Default:
                    return native;
                case EventFieldFormat.String:
                    return TraceLoggingDataType.Char8;
                case EventFieldFormat.Boolean:
                    return TraceLoggingDataType.Boolean8;
                case EventFieldFormat.Hexadecimal:
                    return TraceLoggingDataType.HexInt8;
#if false 
                case EventSourceFieldFormat.Signed:
                    return TraceLoggingDataType.Int8;
                case EventSourceFieldFormat.Unsigned:
                    return TraceLoggingDataType.UInt8;
#endif
                default:
                    return MakeDataType(native, format);
            }
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
            switch (format)
            {
                case EventFieldFormat.Default:
                    return native;
                case EventFieldFormat.String:
                    return TraceLoggingDataType.Char16;
                case EventFieldFormat.Hexadecimal:
                    return TraceLoggingDataType.HexInt16;
#if false
                case EventSourceFieldFormat.Port:
                    return TraceLoggingDataType.Port;
                case EventSourceFieldFormat.Signed:
                    return TraceLoggingDataType.Int16;
                case EventSourceFieldFormat.Unsigned:
                    return TraceLoggingDataType.UInt16;
#endif
                default:
                    return MakeDataType(native, format);
            }
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
            switch (format)
            {
                case EventFieldFormat.Default:
                    return native;
                case EventFieldFormat.Boolean:
                    return TraceLoggingDataType.Boolean32;
                case EventFieldFormat.Hexadecimal:
                    return TraceLoggingDataType.HexInt32;
#if false 
                case EventSourceFieldFormat.Ipv4Address:
                    return TraceLoggingDataType.Ipv4Address;
                case EventSourceFieldFormat.ProcessId:
                    return TraceLoggingDataType.ProcessId;
                case EventSourceFieldFormat.ThreadId:
                    return TraceLoggingDataType.ThreadId;
                case EventSourceFieldFormat.Win32Error:
                    return TraceLoggingDataType.Win32Error;
                case EventSourceFieldFormat.NTStatus:
                    return TraceLoggingDataType.NTStatus;
#endif
                case EventFieldFormat.HResult:
                    return TraceLoggingDataType.HResult;
#if false 
                case EventSourceFieldFormat.Signed:
                    return TraceLoggingDataType.Int32;
                case EventSourceFieldFormat.Unsigned:
                    return TraceLoggingDataType.UInt32;
#endif
                default:
                    return MakeDataType(native, format);
            }
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
            switch (format)
            {
                case EventFieldFormat.Default:
                    return native;
                case EventFieldFormat.Hexadecimal:
                    return TraceLoggingDataType.HexInt64;
#if false 
                case EventSourceFieldFormat.FileTime:
                    return TraceLoggingDataType.FileTime;
                case EventSourceFieldFormat.Signed:
                    return TraceLoggingDataType.Int64;
                case EventSourceFieldFormat.Unsigned:
                    return TraceLoggingDataType.UInt64;
#endif
                default:
                    return MakeDataType(native, format);
            }
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
            switch (format)
            {
                case EventFieldFormat.Default:
                    return native;
                case EventFieldFormat.Hexadecimal:
                    return HexIntPtrType;
#if false 
                case EventSourceFieldFormat.Signed:
                    return IntPtrType;
                case EventSourceFieldFormat.Unsigned:
                    return UIntPtrType;
#endif
                default:
                    return MakeDataType(native, format);
            }
        }

        #endregion

        #region Reflection helpers

        /*
        All TraceLogging use of reflection APIs should go through wrappers here.
        This helps with portability, and it also makes it easier to audit what
        kinds of reflection operations are being done.
        */

        public static object CreateInstance(Type type, params object[] parameters)
        {
            return Activator.CreateInstance(type, parameters);
        }

        public static bool IsValueType(Type type)
        {
            bool result = type.IsValueType();
            return result;
        }

        public static bool IsEnum(Type type)
        {
            bool result = type.IsEnum();
            return result;
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            IEnumerable<PropertyInfo> result = type.GetProperties();
            return result;
        }

        public static MethodInfo GetGetMethod(PropertyInfo propInfo)
        {
            MethodInfo result = propInfo.GetGetMethod();
            return result;
        }

        public static MethodInfo GetDeclaredStaticMethod(Type declaringType, string name)
        {
            MethodInfo result;
#if (ES_BUILD_PCL || ES_BUILD_PN)
            result = declaringType.GetTypeInfo().GetDeclaredMethod(name);
#else
            result = declaringType.GetMethod(
                name,
                BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
#endif
            return result;
        }

        public static bool HasCustomAttribute(
            PropertyInfo propInfo,
            Type attributeType)
        {
            bool result;
#if (ES_BUILD_PCL || ES_BUILD_PN)
            result = propInfo.IsDefined(attributeType);
#else
            var attributes = propInfo.GetCustomAttributes(
                attributeType,
                false);
            result = attributes.Length != 0;
#endif
            return result;
        }

        public static AttributeType GetCustomAttribute<AttributeType>(PropertyInfo propInfo)
            where AttributeType : Attribute
        {
            AttributeType result = null;
#if (ES_BUILD_PCL || ES_BUILD_PN)
            foreach (var attrib in propInfo.GetCustomAttributes<AttributeType>(false))
            {
                result = attrib;
                break;
            }
#else
            var attributes = propInfo.GetCustomAttributes(typeof(AttributeType), false);
            if (attributes.Length != 0)
            {
                result = (AttributeType)attributes[0];
            }
#endif
            return result;
        }

        public static AttributeType GetCustomAttribute<AttributeType>(Type type)
            where AttributeType : Attribute
        {
            AttributeType result = null;
#if (ES_BUILD_PCL || ES_BUILD_PN)
            foreach (var attrib in type.GetTypeInfo().GetCustomAttributes<AttributeType>(false))
            {
                result = attrib;
                break;
            }
#else
            var attributes = type.GetCustomAttributes(typeof(AttributeType), false);
            if (attributes.Length != 0)
            {
                result = (AttributeType)attributes[0];
            }
#endif
            return result;
        }

        public static Type[] GetGenericArguments(Type type)
        {
            return type.GetGenericArguments();
        }

        public static Type FindEnumerableElementType(Type type)
        {
            Type elementType = null;

            if (IsGenericMatch(type, typeof(IEnumerable<>)))
            {
                elementType = GetGenericArguments(type)[0];
            }
            else
            {
#if (ES_BUILD_PCL || ES_BUILD_PN)
                var ifaceTypes = type.GetTypeInfo().ImplementedInterfaces;
#else
                var ifaceTypes = type.FindInterfaces(IsGenericMatch, typeof(IEnumerable<>));
#endif

                foreach (var ifaceType in ifaceTypes)
                {
#if (ES_BUILD_PCL || ES_BUILD_PN)
                    if (!IsGenericMatch(ifaceType, typeof(IEnumerable<>)))
                    {
                        continue;
                    }
#endif

                    if (elementType != null)
                    {
                        // ambiguous match. report no match at all.
                        elementType = null;
                        break;
                    }

                    elementType = GetGenericArguments(ifaceType)[0];
                }
            }

            return elementType;
        }

        public static bool IsGenericMatch(Type type, object openType)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == (Type)openType;
        }

        public static Delegate CreateDelegate(Type delegateType, MethodInfo methodInfo)
        {
            Delegate result;
#if (ES_BUILD_PCL || ES_BUILD_PN)
            result = methodInfo.CreateDelegate(
                delegateType);
#else
            result = Delegate.CreateDelegate(
                delegateType,
                methodInfo);
#endif
            return result;
        }

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

            var eventAttrib = Statics.GetCustomAttribute<EventDataAttribute>(dataType);
            if (eventAttrib != null ||
                Statics.GetCustomAttribute<CompilerGeneratedAttribute>(dataType) != null ||
                IsGenericMatch(dataType, typeof(KeyValuePair<,>)))
            {
                var analysis = new TypeAnalysis(dataType, eventAttrib, recursionCheck);
                result = new InvokeTypeInfo(dataType, analysis);
            }
            else if (dataType.IsArray)
            {
                var elementType = dataType.GetElementType();
                if (elementType == typeof(Boolean))
                {
                    result = ScalarArrayTypeInfo.Boolean();
                }
                else if (elementType == typeof(Byte))
                {
                    result = ScalarArrayTypeInfo.Byte();
                }
                else if (elementType == typeof(SByte))
                {
                    result = ScalarArrayTypeInfo.SByte();
                }
                else if (elementType == typeof(Int16))
                {
                    result = ScalarArrayTypeInfo.Int16();
                }
                else if (elementType == typeof(UInt16))
                {
                    result = ScalarArrayTypeInfo.UInt16();
                }
                else if (elementType == typeof(Int32))
                {
                    result = ScalarArrayTypeInfo.Int32();
                }
                else if (elementType == typeof(UInt32))
                {
                    result = ScalarArrayTypeInfo.UInt32();
                }
                else if (elementType == typeof(Int64))
                {
                    result = ScalarArrayTypeInfo.Int64();
                }
                else if (elementType == typeof(UInt64))
                {
                    result = ScalarArrayTypeInfo.UInt64();
                }
                else if (elementType == typeof(Char))
                {
                    result = ScalarArrayTypeInfo.Char();
                }
                else if (elementType == typeof(Double))
                {
                    result = ScalarArrayTypeInfo.Double();
                }
                else if (elementType == typeof(Single))
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
                if (Statics.IsEnum(dataType))
                    dataType = Enum.GetUnderlyingType(dataType);

                if (dataType == typeof(String))
                {
                    result = new StringTypeInfo();
                }
                else if (dataType == typeof(Boolean))
                {
                    result = ScalarTypeInfo.Boolean();
                }
                else if (dataType == typeof(Byte))
                {
                    result = ScalarTypeInfo.Byte();
                }
                else if (dataType == typeof(SByte))
                {
                    result = ScalarTypeInfo.SByte();
                }
                else if (dataType == typeof(Int16))
                {
                    result = ScalarTypeInfo.Int16();
                }
                else if (dataType == typeof(UInt16))
                {
                    result = ScalarTypeInfo.UInt16();
                }
                else if (dataType == typeof(Int32))
                {
                    result = ScalarTypeInfo.Int32();
                }
                else if (dataType == typeof(UInt32))
                {
                    result = ScalarTypeInfo.UInt32();
                }
                else if (dataType == typeof(Int64))
                {
                    result = ScalarTypeInfo.Int64();
                }
                else if (dataType == typeof(UInt64))
                {
                    result = ScalarTypeInfo.UInt64();
                }
                else if (dataType == typeof(Char))
                {
                    result = ScalarTypeInfo.Char();
                }
                else if (dataType == typeof(Double))
                {
                    result = ScalarTypeInfo.Double();
                }
                else if (dataType == typeof(Single))
                {
                    result = ScalarTypeInfo.Single();
                }
                else if (dataType == typeof(DateTime))
                {
                    result = new DateTimeTypeInfo();
                }
                else if (dataType == typeof(Decimal))
                {
                    result = new DecimalTypeInfo();
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
                    result = new TimeSpanTypeInfo();
                }
                else if (dataType == typeof(DateTimeOffset))
                {
                    result = new DateTimeOffsetTypeInfo();
                }
                else if (dataType == typeof(EmptyStruct))
                {
                    result = new NullTypeInfo();
                }
                else if (IsGenericMatch(dataType, typeof(Nullable<>)))
                {
                    result = new NullableTypeInfo(dataType, recursionCheck);
                }
                else
                {
                    var elementType = FindEnumerableElementType(dataType);
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
