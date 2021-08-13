// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

#if CORERT
using RuntimeType = System.Type;
using EnumInfo = Internal.Runtime.Augments.EnumInfo;
#endif

// The code below includes partial support for float/double and
// pointer sized enums.
//
// The type loader does not prohibit such enums, and older versions of
// the ECMA spec include them as possible enum types.
//
// However there are many things broken throughout the stack for
// float/double/intptr/uintptr enums. There was a conscious decision
// made to not fix the whole stack to work well for them because of
// the right behavior is often unclear, and it is hard to test and
// very low value because of such enums cannot be expressed in C#.

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract partial class Enum : ValueType, IComparable, IFormattable, IConvertible
    {
        #region Private Constants
        private const char EnumSeparatorChar = ',';
        #endregion

        #region Private Static Methods

        private string ValueToString()
        {
            ref byte data = ref this.GetRawData();
            return (InternalGetCorElementType()) switch
            {
                CorElementType.ELEMENT_TYPE_I1 => Unsafe.As<byte, sbyte>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_U1 => data.ToString(),
                CorElementType.ELEMENT_TYPE_BOOLEAN => Unsafe.As<byte, bool>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_I2 => Unsafe.As<byte, short>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_U2 => Unsafe.As<byte, ushort>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_CHAR => Unsafe.As<byte, char>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_I4 => Unsafe.As<byte, int>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_U4 => Unsafe.As<byte, uint>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_R4 => Unsafe.As<byte, float>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_I8 => Unsafe.As<byte, long>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_U8 => Unsafe.As<byte, ulong>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_R8 => Unsafe.As<byte, double>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_I => Unsafe.As<byte, IntPtr>(ref data).ToString(),
                CorElementType.ELEMENT_TYPE_U => Unsafe.As<byte, UIntPtr>(ref data).ToString(),
                _ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType),
            };
        }

        private string ValueToHexString()
        {
            ref byte data = ref this.GetRawData();
            Span<byte> bytes = stackalloc byte[8];
            int length;
            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                case CorElementType.ELEMENT_TYPE_U1:
                    bytes[0] = data;
                    length = 1;
                    break;
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return data != 0 ? "01" : "00";
                case CorElementType.ELEMENT_TYPE_I2:
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    BinaryPrimitives.WriteUInt16BigEndian(bytes, Unsafe.As<byte, ushort>(ref data));
                    length = 2;
                    break;
                case CorElementType.ELEMENT_TYPE_I4:
                case CorElementType.ELEMENT_TYPE_U4:
                    BinaryPrimitives.WriteUInt32BigEndian(bytes, Unsafe.As<byte, uint>(ref data));
                    length = 4;
                    break;
                case CorElementType.ELEMENT_TYPE_I8:
                case CorElementType.ELEMENT_TYPE_U8:
                    BinaryPrimitives.WriteUInt64BigEndian(bytes, Unsafe.As<byte, ulong>(ref data));
                    length = 8;
                    break;
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }

            return HexConverter.ToString(bytes.Slice(0, length), HexConverter.Casing.Upper);
        }

        private static string ValueToHexString(object value)
        {
            return (Convert.GetTypeCode(value)) switch
            {
                TypeCode.SByte => ((byte)(sbyte)value).ToString("X2", null),
                TypeCode.Byte => ((byte)value).ToString("X2", null),
                TypeCode.Boolean => ((bool)value) ? "01" : "00",
                TypeCode.Int16 => ((ushort)(short)value).ToString("X4", null),
                TypeCode.UInt16 => ((ushort)value).ToString("X4", null),
                TypeCode.Char => ((ushort)(char)value).ToString("X4", null),
                TypeCode.UInt32 => ((uint)value).ToString("X8", null),
                TypeCode.Int32 => ((uint)(int)value).ToString("X8", null),
                TypeCode.UInt64 => ((ulong)value).ToString("X16", null),
                TypeCode.Int64 => ((ulong)(long)value).ToString("X16", null),
                _ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType),
            };
        }

        internal static string? GetEnumName(RuntimeType enumType, ulong ulValue)
        {
            return GetEnumName(GetEnumInfo(enumType), ulValue);
        }

        private static string? GetEnumName(EnumInfo enumInfo, ulong ulValue)
        {
            int index = Array.BinarySearch(enumInfo.Values, ulValue);
            if (index >= 0)
            {
                return enumInfo.Names[index];
            }

            return null; // return null so the caller knows to .ToString() the input
        }

        private static string? InternalFormat(RuntimeType enumType, ulong value)
        {
            EnumInfo enumInfo = GetEnumInfo(enumType);

            if (!enumInfo.HasFlagsAttribute)
            {
                return GetEnumName(enumInfo, value);
            }
            else // These are flags OR'ed together (We treat everything as unsigned types)
            {
                return InternalFlagsFormat(enumInfo, value);
            }
        }

        private static string? InternalFlagsFormat(RuntimeType enumType, ulong result)
        {
            return InternalFlagsFormat(GetEnumInfo(enumType), result);
        }

        private static string? InternalFlagsFormat(EnumInfo enumInfo, ulong resultValue)
        {
            string[] names = enumInfo.Names;
            ulong[] values = enumInfo.Values;
            Debug.Assert(names.Length == values.Length);

            // Values are sorted, so if the incoming value is 0, we can check to see whether
            // the first entry matches it, in which case we can return its name; otherwise,
            // we can just return "0".
            if (resultValue == 0)
            {
                return values.Length > 0 && values[0] == 0 ?
                    names[0] :
                    "0";
            }

            // With a ulong result value, regardless of the enum's base type, the maximum
            // possible number of consistent name/values we could have is 64, since every
            // value is made up of one or more bits, and when we see values and incorporate
            // their names, we effectively switch off those bits.
            Span<int> foundItems = stackalloc int[64];

            // Walk from largest to smallest. It's common to have a flags enum with a single
            // value that matches a single entry, in which case we can just return the existing
            // name string.
            int index = values.Length - 1;
            while (index >= 0)
            {
                if (values[index] == resultValue)
                {
                    return names[index];
                }

                if (values[index] < resultValue)
                {
                    break;
                }

                index--;
            }

            // Now look for multiple matches, storing the indices of the values
            // into our span.
            int resultLength = 0, foundItemsCount = 0;
            while (index >= 0)
            {
                ulong currentValue = values[index];
                if (index == 0 && currentValue == 0)
                {
                    break;
                }

                if ((resultValue & currentValue) == currentValue)
                {
                    resultValue -= currentValue;
                    foundItems[foundItemsCount++] = index;
                    resultLength = checked(resultLength + names[index].Length);
                }

                index--;
            }

            // If we exhausted looking through all the values and we still have
            // a non-zero result, we couldn't match the result to only named values.
            // In that case, we return null and let the call site just generate
            // a string for the integral value.
            if (resultValue != 0)
            {
                return null;
            }

            // We know what strings to concatenate.  Do so.

            Debug.Assert(foundItemsCount > 0);
            const int SeparatorStringLength = 2; // ", "
            string result = string.FastAllocateString(checked(resultLength + (SeparatorStringLength * (foundItemsCount - 1))));

            Span<char> resultSpan = new Span<char>(ref result.GetRawStringData(), result.Length);
            string name = names[foundItems[--foundItemsCount]];
            name.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(name.Length);
            while (--foundItemsCount >= 0)
            {
                resultSpan[0] = EnumSeparatorChar;
                resultSpan[1] = ' ';
                resultSpan = resultSpan.Slice(2);

                name = names[foundItems[foundItemsCount]];
                name.CopyTo(resultSpan);
                resultSpan = resultSpan.Slice(name.Length);
            }
            Debug.Assert(resultSpan.IsEmpty);

            return result;
        }

        internal static ulong ToUInt64(object value)
        {
            // Helper function to silently convert the value to UInt64 from the other base types for enum without throwing an exception.
            // This is need since the Convert functions do overflow checks.
            TypeCode typeCode = Convert.GetTypeCode(value);
            ulong result = typeCode switch
            {
                TypeCode.SByte => (ulong)(sbyte)value,
                TypeCode.Byte => (byte)value,
                TypeCode.Boolean => (bool)value ? 1UL : 0UL,
                TypeCode.Int16 => (ulong)(short)value,
                TypeCode.UInt16 => (ushort)value,
                TypeCode.Char => (char)value,
                TypeCode.UInt32 => (uint)value,
                TypeCode.Int32 => (ulong)(int)value,
                TypeCode.UInt64 => (ulong)value,
                TypeCode.Int64 => (ulong)(long)value,
                _ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType),
            };
            return result;
        }

        private static ulong ToUInt64<TEnum>(TEnum value) where TEnum : struct, Enum =>
            Type.GetTypeCode(typeof(TEnum)) switch
            {
                TypeCode.SByte => (ulong)Unsafe.As<TEnum, sbyte>(ref value),
                TypeCode.Byte => Unsafe.As<TEnum, byte>(ref value),
                TypeCode.Boolean => Unsafe.As<TEnum, bool>(ref value) ? 1UL : 0UL,
                TypeCode.Int16 => (ulong)Unsafe.As<TEnum, short>(ref value),
                TypeCode.UInt16 => Unsafe.As<TEnum, ushort>(ref value),
                TypeCode.Char => Unsafe.As<TEnum, char>(ref value),
                TypeCode.UInt32 => Unsafe.As<TEnum, uint>(ref value),
                TypeCode.Int32 => (ulong)Unsafe.As<TEnum, int>(ref value),
                TypeCode.UInt64 => Unsafe.As<TEnum, ulong>(ref value),
                TypeCode.Int64 => (ulong)Unsafe.As<TEnum, long>(ref value),
                _ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType),
            };
        #endregion

        #region Public Static Methods
        public static string? GetName<TEnum>(TEnum value) where TEnum : struct, Enum
            => GetEnumName((RuntimeType)typeof(TEnum), ToUInt64(value));

        public static string? GetName(Type enumType, object value)
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumName(value);
        }

        public static string[] GetNames<TEnum>() where TEnum : struct, Enum
            => new ReadOnlySpan<string>(InternalGetNames((RuntimeType)typeof(TEnum))).ToArray();

        public static string[] GetNames(Type enumType)
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumNames();
        }

        internal static string[] InternalGetNames(RuntimeType enumType)
        {
            // Get all of the names
            return GetEnumInfo(enumType, true).Names;
        }

        public static Type GetUnderlyingType(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumUnderlyingType();
        }

        public static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum
            => (TEnum[])GetValues(typeof(TEnum));

        public static Array GetValues(Type enumType)
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumValues();
        }

        [Intrinsic]
        public bool HasFlag(Enum flag)
        {
            if (flag is null)
                throw new ArgumentNullException(nameof(flag));
            if (!GetType().IsEquivalentTo(flag.GetType()))
                throw new ArgumentException(SR.Format(SR.Argument_EnumTypeDoesNotMatch, flag.GetType(), GetType()));

            return InternalHasFlag(flag);
        }

        internal static ulong[] InternalGetValues(RuntimeType enumType)
        {
            // Get all of the values
            return GetEnumInfo(enumType, false).Values;
        }

        public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            RuntimeType enumType = (RuntimeType)typeof(TEnum);
            ulong[] ulValues = Enum.InternalGetValues(enumType);
            ulong ulValue = Enum.ToUInt64(value);

            return Array.BinarySearch(ulValues, ulValue) >= 0;
        }

        public static bool IsDefined(Type enumType, object value)
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.IsEnumDefined(value);
        }

        public static object Parse(Type enumType, string value) =>
            Parse(enumType, value, ignoreCase: false);

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <returns>
        /// An object of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an <see cref="Enum"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is either an empty string or only contains white space.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying type of <paramref name="enumType"/></exception>
        public static object Parse(Type enumType, ReadOnlySpan<char> value) =>
            Parse(enumType, value, ignoreCase: false);

        public static object Parse(Type enumType, string value, bool ignoreCase)
        {
            bool success = TryParse(enumType, value, ignoreCase, throwOnFailure: true, out object? result);
            Debug.Assert(success);
            return result!;
        }

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object. A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <param name="ignoreCase"><see langword="true"/> to ignore case; <see langword="false"/> to regard case.</param>
        /// <returns>
        /// An object of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an <see cref="Enum"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is either an empty string or only contains white space.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying type of <paramref name="enumType"/></exception>
        public static object Parse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase)
        {
            bool success = TryParse(enumType, value, ignoreCase, throwOnFailure: true, out object? result);
            Debug.Assert(success);
            return result!;
        }

        public static TEnum Parse<TEnum>(string value) where TEnum : struct =>
            Parse<TEnum>(value, ignoreCase: false);

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants specified by <typeparamref name="TEnum"/> to an equivalent enumerated object.
        /// </summary>
        /// <typeparam name="TEnum">An enumeration type.</typeparam>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <returns><typeparamref name="TEnum"/> An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an <see cref="Enum"/> type</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> does not contain enumeration information</exception>
        public static TEnum Parse<TEnum>(ReadOnlySpan<char> value) where TEnum : struct =>
           Parse<TEnum>(value, ignoreCase: false);

        public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct
        {
            bool success = TryParse<TEnum>(value, ignoreCase, throwOnFailure: true, out TEnum result);
            Debug.Assert(success);
            return result;
        }

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants specified by <typeparamref name="TEnum"/> to an equivalent enumerated object. A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">An enumeration type.</typeparam>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <param name="ignoreCase"><see langword="true"/> to ignore case; <see langword="false"/> to regard case.</param>
        /// <returns><typeparamref name="TEnum"/> An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an <see cref="Enum"/> type</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> does not contain enumeration information</exception>
        public static TEnum Parse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase) where TEnum : struct
        {
            bool success = TryParse<TEnum>(value, ignoreCase, throwOnFailure: true, out TEnum result);
            Debug.Assert(success);
            return result;
        }

        public static bool TryParse(Type enumType, string? value, out object? result) =>
            TryParse(enumType, value, ignoreCase: false, out result);

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The span representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryParse(Type enumType, ReadOnlySpan<char> value, out object? result) =>
          TryParse(enumType, value, ignoreCase: false, out result);

        public static bool TryParse(Type enumType, string? value, bool ignoreCase, out object? result) =>
            TryParse(enumType, value, ignoreCase, throwOnFailure: false, out result);

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object. A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The span representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="ignoreCase"><see langword="true"/> to read <paramref name="enumType"/> in case insensitive mode; <see langword="false"/> to read <paramref name="enumType"/> in case sensitive mode.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, out object? result) =>
            TryParse(enumType, value, ignoreCase, throwOnFailure: false, out result);

        private static bool TryParse(Type enumType, string? value, bool ignoreCase, bool throwOnFailure, out object? result)
        {
            if (value == null)
            {
                if (throwOnFailure)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                result = null;
                return false;
            }

            return TryParse(enumType, value.AsSpan(), ignoreCase, throwOnFailure, out result);
        }

        private static bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out object? result)
        {
            // Validation on the enum type itself.  Failures here are considered non-parsing failures
            // and thus always throw rather than returning false.
            RuntimeType rt = ValidateRuntimeType(enumType);
            value = value.TrimStart();
            if (value.Length == 0)
            {
                if (throwOnFailure)
                {
                    throw new ArgumentException(SR.Arg_MustContainEnumInfo, nameof(value));
                }
                result = null;
                return false;
            }

            int intResult;
            uint uintResult;
            bool parsed;

            switch (Type.GetTypeCode(rt))
            {
                case TypeCode.SByte:
                    parsed = TryParseInt32Enum(rt, value, sbyte.MinValue, sbyte.MaxValue, ignoreCase, throwOnFailure, TypeCode.SByte, out intResult);
                    result = parsed ? InternalBoxEnum(rt, intResult) : null;
                    return parsed;

                case TypeCode.Int16:
                    parsed = TryParseInt32Enum(rt, value, short.MinValue, short.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int16, out intResult);
                    result = parsed ? InternalBoxEnum(rt, intResult) : null;
                    return parsed;

                case TypeCode.Int32:
                    parsed = TryParseInt32Enum(rt, value, int.MinValue, int.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int32, out intResult);
                    result = parsed ? InternalBoxEnum(rt, intResult) : null;
                    return parsed;

                case TypeCode.Byte:
                    parsed = TryParseUInt32Enum(rt, value, byte.MaxValue, ignoreCase, throwOnFailure, TypeCode.Byte, out uintResult);
                    result = parsed ? InternalBoxEnum(rt, uintResult) : null;
                    return parsed;

                case TypeCode.UInt16:
                    parsed = TryParseUInt32Enum(rt, value, ushort.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt16, out uintResult);
                    result = parsed ? InternalBoxEnum(rt, uintResult) : null;
                    return parsed;

                case TypeCode.UInt32:
                    parsed = TryParseUInt32Enum(rt, value, uint.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt32, out uintResult);
                    result = parsed ? InternalBoxEnum(rt, uintResult) : null;
                    return parsed;

                case TypeCode.Int64:
                    parsed = TryParseInt64Enum(rt, value, ignoreCase, throwOnFailure, out long longResult);
                    result = parsed ? InternalBoxEnum(rt, longResult) : null;
                    return parsed;

                case TypeCode.UInt64:
                    parsed = TryParseUInt64Enum(rt, value, ignoreCase, throwOnFailure, out ulong ulongResult);
                    result = parsed ? InternalBoxEnum(rt, (long)ulongResult) : null;
                    return parsed;

                default:
                    return TryParseRareEnum(rt, value, ignoreCase, throwOnFailure, out result);
            }
        }

        public static bool TryParse<TEnum>([NotNullWhen(true)] string? value, out TEnum result) where TEnum : struct =>
            TryParse<TEnum>(value, ignoreCase: false, out result);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value">The span representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an enumeration type</exception>
        public static bool TryParse<TEnum>(ReadOnlySpan<char> value, out TEnum result) where TEnum : struct =>
            TryParse<TEnum>(value, ignoreCase: false, out result);

        public static bool TryParse<TEnum>([NotNullWhen(true)] string? value, bool ignoreCase, out TEnum result) where TEnum : struct =>
            TryParse<TEnum>(value, ignoreCase, throwOnFailure: false, out result);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object. A parameter specifies whether the operation is case-sensitive. The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value">The span representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="ignoreCase"><see langword="true"/> to ignore case; <see langword="false"/> to consider case.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an enumeration type</exception>
        public static bool TryParse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase, out TEnum result) where TEnum : struct =>
            TryParse<TEnum>(value, ignoreCase, throwOnFailure: false, out result);

        private static bool TryParse<TEnum>(string? value, bool ignoreCase, bool throwOnFailure, out TEnum result) where TEnum : struct
        {
            if (value == null)
            {
                if (throwOnFailure)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                result = default;
                return false;
            }

            return TryParse(value.AsSpan(), ignoreCase, throwOnFailure, out result);
        }

        private static bool TryParse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out TEnum result) where TEnum : struct
        {
            // Validation on the enum type itself.  Failures here are considered non-parsing failures
            // and thus always throw rather than returning false.
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(TEnum));
            }

            value = value.TrimStart();
            if (value.Length == 0)
            {
                if (throwOnFailure)
                {
                    throw new ArgumentException(SR.Arg_MustContainEnumInfo, nameof(value));
                }
                result = default;
                return false;
            }

            int intResult;
            uint uintResult;
            bool parsed;
            RuntimeType rt = (RuntimeType)typeof(TEnum);

            switch (Type.GetTypeCode(typeof(TEnum)))
            {
                case TypeCode.SByte:
                    parsed = TryParseInt32Enum(rt, value, sbyte.MinValue, sbyte.MaxValue, ignoreCase, throwOnFailure, TypeCode.SByte, out intResult);
                    sbyte sbyteResult = (sbyte)intResult;
                    result = Unsafe.As<sbyte, TEnum>(ref sbyteResult);
                    return parsed;

                case TypeCode.Int16:
                    parsed = TryParseInt32Enum(rt, value, short.MinValue, short.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int16, out intResult);
                    short shortResult = (short)intResult;
                    result = Unsafe.As<short, TEnum>(ref shortResult);
                    return parsed;

                case TypeCode.Int32:
                    parsed = TryParseInt32Enum(rt, value, int.MinValue, int.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int32, out intResult);
                    result = Unsafe.As<int, TEnum>(ref intResult);
                    return parsed;

                case TypeCode.Byte:
                    parsed = TryParseUInt32Enum(rt, value, byte.MaxValue, ignoreCase, throwOnFailure, TypeCode.Byte, out uintResult);
                    byte byteResult = (byte)uintResult;
                    result = Unsafe.As<byte, TEnum>(ref byteResult);
                    return parsed;

                case TypeCode.UInt16:
                    parsed = TryParseUInt32Enum(rt, value, ushort.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt16, out uintResult);
                    ushort ushortResult = (ushort)uintResult;
                    result = Unsafe.As<ushort, TEnum>(ref ushortResult);
                    return parsed;

                case TypeCode.UInt32:
                    parsed = TryParseUInt32Enum(rt, value, uint.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt32, out uintResult);
                    result = Unsafe.As<uint, TEnum>(ref uintResult);
                    return parsed;

                case TypeCode.Int64:
                    parsed = TryParseInt64Enum(rt, value, ignoreCase, throwOnFailure, out long longResult);
                    result = Unsafe.As<long, TEnum>(ref longResult);
                    return parsed;

                case TypeCode.UInt64:
                    parsed = TryParseUInt64Enum(rt, value, ignoreCase, throwOnFailure, out ulong ulongResult);
                    result = Unsafe.As<ulong, TEnum>(ref ulongResult);
                    return parsed;

                default:
                    parsed = TryParseRareEnum(rt, value, ignoreCase, throwOnFailure, out object? objectResult);
                    result = parsed ? (TEnum)objectResult! : default;
                    return parsed;
            }
        }

        /// <summary>Tries to parse the value of an enum with known underlying types that fit in an Int32 (Int32, Int16, and SByte).</summary>
        private static bool TryParseInt32Enum(
            RuntimeType enumType, ReadOnlySpan<char> value, int minInclusive, int maxInclusive, bool ignoreCase, bool throwOnFailure, TypeCode type, out int result)
        {
            Debug.Assert(
                enumType.GetEnumUnderlyingType() == typeof(sbyte) ||
                enumType.GetEnumUnderlyingType() == typeof(short) ||
                enumType.GetEnumUnderlyingType() == typeof(int));

            Number.ParsingStatus status = default;
            if (StartsNumber(value[0]))
            {
                status = Number.TryParseInt32IntegerStyle(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture.NumberFormat, out result);
                if (status == Number.ParsingStatus.OK)
                {
                    if ((uint)(result - minInclusive) <= (uint)(maxInclusive - minInclusive))
                    {
                        return true;
                    }

                    status = Number.ParsingStatus.Overflow;
                }
            }

            if (status == Number.ParsingStatus.Overflow)
            {
                if (throwOnFailure)
                {
                    Number.ThrowOverflowException(type);
                }
            }
            else if (TryParseByName(enumType, value, ignoreCase, throwOnFailure, out ulong ulongResult))
            {
                result = (int)ulongResult;
                Debug.Assert(result >= minInclusive && result <= maxInclusive);
                return true;
            }

            result = 0;
            return false;
        }

        /// <summary>Tries to parse the value of an enum with known underlying types that fit in a UInt32 (UInt32, UInt16, and Byte).</summary>
        private static bool TryParseUInt32Enum(RuntimeType enumType, ReadOnlySpan<char> value, uint maxInclusive, bool ignoreCase, bool throwOnFailure, TypeCode type, out uint result)
        {
            Debug.Assert(
                enumType.GetEnumUnderlyingType() == typeof(byte) ||
                enumType.GetEnumUnderlyingType() == typeof(ushort) ||
                enumType.GetEnumUnderlyingType() == typeof(uint));

            Number.ParsingStatus status = default;
            if (StartsNumber(value[0]))
            {
                status = Number.TryParseUInt32IntegerStyle(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture.NumberFormat, out result);
                if (status == Number.ParsingStatus.OK)
                {
                    if (result <= maxInclusive)
                    {
                        return true;
                    }

                    status = Number.ParsingStatus.Overflow;
                }
            }

            if (status == Number.ParsingStatus.Overflow)
            {
                if (throwOnFailure)
                {
                    Number.ThrowOverflowException(type);
                }
            }
            else if (TryParseByName(enumType, value, ignoreCase, throwOnFailure, out ulong ulongResult))
            {
                result = (uint)ulongResult;
                Debug.Assert(result <= maxInclusive);
                return true;
            }

            result = 0;
            return false;
        }

        /// <summary>Tries to parse the value of an enum with Int64 as the underlying type.</summary>
        private static bool TryParseInt64Enum(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out long result)
        {
            Debug.Assert(enumType.GetEnumUnderlyingType() == typeof(long));

            Number.ParsingStatus status = default;
            if (StartsNumber(value[0]))
            {
                status = Number.TryParseInt64IntegerStyle(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture.NumberFormat, out result);
                if (status == Number.ParsingStatus.OK)
                {
                    return true;
                }
            }

            if (status == Number.ParsingStatus.Overflow)
            {
                if (throwOnFailure)
                {
                    Number.ThrowOverflowException(TypeCode.Int64);
                }
            }
            else if (TryParseByName(enumType, value, ignoreCase, throwOnFailure, out ulong ulongResult))
            {
                result = (long)ulongResult;
                return true;
            }

            result = 0;
            return false;
        }

        /// <summary>Tries to parse the value of an enum with UInt64 as the underlying type.</summary>
        private static bool TryParseUInt64Enum(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out ulong result)
        {
            Debug.Assert(enumType.GetEnumUnderlyingType() == typeof(ulong));

            Number.ParsingStatus status = default;
            if (StartsNumber(value[0]))
            {
                status = Number.TryParseUInt64IntegerStyle(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture.NumberFormat, out result);
                if (status == Number.ParsingStatus.OK)
                {
                    return true;
                }
            }

            if (status == Number.ParsingStatus.Overflow)
            {
                if (throwOnFailure)
                {
                    Number.ThrowOverflowException(TypeCode.UInt64);
                }
            }
            else if (TryParseByName(enumType, value, ignoreCase, throwOnFailure, out result))
            {
                return true;
            }

            result = 0;
            return false;
        }

        /// <summary>Tries to parse the value of an enum with an underlying type that can't be expressed in C# (e.g. char, bool, double, etc.)</summary>
        private static bool TryParseRareEnum(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, [NotNullWhen(true)] out object? result)
        {
            Debug.Assert(
                enumType.GetEnumUnderlyingType() != typeof(sbyte) &&
                enumType.GetEnumUnderlyingType() != typeof(byte) &&
                enumType.GetEnumUnderlyingType() != typeof(short) &&
                enumType.GetEnumUnderlyingType() != typeof(ushort) &&
                enumType.GetEnumUnderlyingType() != typeof(int) &&
                enumType.GetEnumUnderlyingType() != typeof(uint) &&
                enumType.GetEnumUnderlyingType() != typeof(long) &&
                enumType.GetEnumUnderlyingType() != typeof(ulong),
                "Should only be used when parsing enums with rare underlying types, those that can't be expressed in C#.");

            if (StartsNumber(value[0]))
            {
                Type underlyingType = GetUnderlyingType(enumType);
                try
                {
                    result = ToObject(enumType, Convert.ChangeType(value.ToString(), underlyingType, CultureInfo.InvariantCulture)!);
                    return true;
                }
                catch (FormatException)
                {
                    // We need to Parse this as a String instead. There are cases
                    // when you tlbimp enums that can have values of the form "3D".
                }
                catch when (!throwOnFailure)
                {
                    result = null;
                    return false;
                }
            }

            if (TryParseByName(enumType, value, ignoreCase, throwOnFailure, out ulong ulongResult))
            {
                try
                {
                    result = ToObject(enumType, ulongResult);
                    return true;
                }
                catch when (!throwOnFailure) { }
            }

            result = null;
            return false;
        }

        private static bool TryParseByName(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out ulong result)
        {
            // Find the field. Let's assume that these are always static classes because the class is an enum.
            EnumInfo enumInfo = GetEnumInfo(enumType);
            string[] enumNames = enumInfo.Names;
            ulong[] enumValues = enumInfo.Values;

            bool parsed = true;
            ulong localResult = 0;
            while (value.Length > 0)
            {
                // Find the next separator.
                ReadOnlySpan<char> subvalue;
                int endIndex = value.IndexOf(EnumSeparatorChar);
                if (endIndex == -1)
                {
                    // No next separator; use the remainder as the next value.
                    subvalue = value.Trim();
                    value = default;
                }
                else if (endIndex != value.Length - 1)
                {
                    // Found a separator before the last char.
                    subvalue = value.Slice(0, endIndex).Trim();
                    value = value.Slice(endIndex + 1);
                }
                else
                {
                    // Last char was a separator, which is invalid.
                    parsed = false;
                    break;
                }

                // Try to match this substring against each enum name
                bool success = false;
                if (ignoreCase)
                {
                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        if (subvalue.EqualsOrdinalIgnoreCase(enumNames[i]))
                        {
                            localResult |= enumValues[i];
                            success = true;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        if (subvalue.EqualsOrdinal(enumNames[i]))
                        {
                            localResult |= enumValues[i];
                            success = true;
                            break;
                        }
                    }
                }

                if (!success)
                {
                    parsed = false;
                    break;
                }
            }

            if (parsed)
            {
                result = localResult;
                return true;
            }

            if (throwOnFailure)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumValueNotFound, value.ToString()));
            }

            result = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool StartsNumber(char c) => char.IsInRange(c, '0', '9') || c == '-' || c == '+';

        public static object ToObject(Type enumType, object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Delegate rest of error checking to the other functions
            TypeCode typeCode = Convert.GetTypeCode(value);

            return typeCode switch
            {
                TypeCode.Int32 => ToObject(enumType, (int)value),
                TypeCode.SByte => ToObject(enumType, (sbyte)value),
                TypeCode.Int16 => ToObject(enumType, (short)value),
                TypeCode.Int64 => ToObject(enumType, (long)value),
                TypeCode.UInt32 => ToObject(enumType, (uint)value),
                TypeCode.Byte => ToObject(enumType, (byte)value),
                TypeCode.UInt16 => ToObject(enumType, (ushort)value),
                TypeCode.UInt64 => ToObject(enumType, (ulong)value),
                TypeCode.Char => ToObject(enumType, (char)value),
                TypeCode.Boolean => ToObject(enumType, (bool)value),
                _ => throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, nameof(value)),
            };
        }

        public static string Format(Type enumType, object value, string format)
        {
            RuntimeType rtType = ValidateRuntimeType(enumType);

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (format == null)
                throw new ArgumentNullException(nameof(format));

            // If the value is an Enum then we need to extract the underlying value from it
            Type valueType = value.GetType();
            if (valueType.IsEnum)
            {
                if (!valueType.IsEquivalentTo(enumType))
                    throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, valueType, enumType));

                if (format.Length != 1)
                {
                    // all acceptable format string are of length 1
                    throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
                }
                return ((Enum)value).ToString(format);
            }

            // The value must be of the same type as the Underlying type of the Enum
            Type underlyingType = GetUnderlyingType(enumType);
            if (valueType != underlyingType)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumFormatUnderlyingTypeAndObjectMustBeSameType, valueType, underlyingType));
            }

            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'G':
                    case 'g':
                        return InternalFormat(rtType, ToUInt64(value)) ?? value.ToString()!;

                    case 'D':
                    case 'd':
                        return value.ToString()!;

                    case 'X':
                    case 'x':
                        return ValueToHexString(value);

                    case 'F':
                    case 'f':
                        return InternalFlagsFormat(rtType, ToUInt64(value)) ?? value.ToString()!;
                }
            }

            throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
        }
        #endregion

        #region Private Methods
        internal object GetValue()
        {
            ref byte data = ref this.GetRawData();
            return (InternalGetCorElementType()) switch
            {
                CorElementType.ELEMENT_TYPE_I1 => Unsafe.As<byte, sbyte>(ref data),
                CorElementType.ELEMENT_TYPE_U1 => data,
                CorElementType.ELEMENT_TYPE_BOOLEAN => Unsafe.As<byte, bool>(ref data),
                CorElementType.ELEMENT_TYPE_I2 => Unsafe.As<byte, short>(ref data),
                CorElementType.ELEMENT_TYPE_U2 => Unsafe.As<byte, ushort>(ref data),
                CorElementType.ELEMENT_TYPE_CHAR => Unsafe.As<byte, char>(ref data),
                CorElementType.ELEMENT_TYPE_I4 => Unsafe.As<byte, int>(ref data),
                CorElementType.ELEMENT_TYPE_U4 => Unsafe.As<byte, uint>(ref data),
                CorElementType.ELEMENT_TYPE_R4 => Unsafe.As<byte, float>(ref data),
                CorElementType.ELEMENT_TYPE_I8 => Unsafe.As<byte, long>(ref data),
                CorElementType.ELEMENT_TYPE_U8 => Unsafe.As<byte, ulong>(ref data),
                CorElementType.ELEMENT_TYPE_R8 => Unsafe.As<byte, double>(ref data),
                CorElementType.ELEMENT_TYPE_I => Unsafe.As<byte, IntPtr>(ref data),
                CorElementType.ELEMENT_TYPE_U => Unsafe.As<byte, UIntPtr>(ref data),
                _ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType),
            };
        }

        private ulong ToUInt64()
        {
            ref byte data = ref this.GetRawData();
            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                    return (ulong)Unsafe.As<byte, sbyte>(ref data);
                case CorElementType.ELEMENT_TYPE_U1:
                    return data;
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return data != 0 ? 1UL : 0UL;
                case CorElementType.ELEMENT_TYPE_I2:
                    return (ulong)Unsafe.As<byte, short>(ref data);
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return Unsafe.As<byte, ushort>(ref data);
                case CorElementType.ELEMENT_TYPE_I4:
                    return (ulong)Unsafe.As<byte, int>(ref data);
                case CorElementType.ELEMENT_TYPE_U4:
                case CorElementType.ELEMENT_TYPE_R4:
                    return Unsafe.As<byte, uint>(ref data);
                case CorElementType.ELEMENT_TYPE_I8:
                    return (ulong)Unsafe.As<byte, long>(ref data);
                case CorElementType.ELEMENT_TYPE_U8:
                case CorElementType.ELEMENT_TYPE_R8:
                    return Unsafe.As<byte, ulong>(ref data);
                case CorElementType.ELEMENT_TYPE_I:
                    return (ulong)Unsafe.As<byte, IntPtr>(ref data);
                case CorElementType.ELEMENT_TYPE_U:
                    return (ulong)Unsafe.As<byte, UIntPtr>(ref data);
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }

        #endregion

        #region Object Overrides

        public override int GetHashCode()
        {
            // CONTRACT with the runtime: GetHashCode of enum types is implemented as GetHashCode of the underlying type.
            // The runtime can bypass calls to Enum::GetHashCode and call the underlying type's GetHashCode directly
            // to avoid boxing the enum.
            ref byte data = ref this.GetRawData();
            return (InternalGetCorElementType()) switch
            {
                CorElementType.ELEMENT_TYPE_I1 => Unsafe.As<byte, sbyte>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_U1 => data.GetHashCode(),
                CorElementType.ELEMENT_TYPE_BOOLEAN => Unsafe.As<byte, bool>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_I2 => Unsafe.As<byte, short>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_U2 => Unsafe.As<byte, ushort>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_CHAR => Unsafe.As<byte, char>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_I4 => Unsafe.As<byte, int>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_U4 => Unsafe.As<byte, uint>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_R4 => Unsafe.As<byte, float>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_I8 => Unsafe.As<byte, long>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_U8 => Unsafe.As<byte, ulong>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_R8 => Unsafe.As<byte, double>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_I => Unsafe.As<byte, IntPtr>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_U => Unsafe.As<byte, UIntPtr>(ref data).GetHashCode(),
                _ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType),
            };
        }

        public override string ToString()
        {
            // Returns the value in a human readable format.  For PASCAL style enums who's value maps directly the name of the field is returned.
            // For PASCAL style enums who's values do not map directly the decimal value of the field is returned.
            // For BitFlags (indicated by the Flags custom attribute): If for each bit that is set in the value there is a corresponding constant
            // (a pure power of 2), then the OR string (ie "Red, Yellow") is returned. Otherwise, if the value is zero or if you can't create a string that consists of
            // pure powers of 2 OR-ed together, you return a hex value

            // Try to see if its one of the enum values, then we return a String back else the value
            return InternalFormat((RuntimeType)GetType(), ToUInt64()) ?? ValueToString();
        }

        public int CompareTo(object? target)
        {
            if (target == this)
                return 0;

            if (target == null)
                return 1; // all values are greater than null

            if (GetType() != target.GetType())
                throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, target.GetType(), GetType()));

            ref byte pThisValue = ref this.GetRawData();
            ref byte pTargetValue = ref target.GetRawData();

            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                    return Unsafe.As<byte, sbyte>(ref pThisValue).CompareTo(Unsafe.As<byte, sbyte>(ref pTargetValue));
                case CorElementType.ELEMENT_TYPE_U1:
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return pThisValue.CompareTo(pTargetValue);
                case CorElementType.ELEMENT_TYPE_I2:
                    return Unsafe.As<byte, short>(ref pThisValue).CompareTo(Unsafe.As<byte, short>(ref pTargetValue));
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return Unsafe.As<byte, ushort>(ref pThisValue).CompareTo(Unsafe.As<byte, ushort>(ref pTargetValue));
                case CorElementType.ELEMENT_TYPE_I4:
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_I:
#endif
                    return Unsafe.As<byte, int>(ref pThisValue).CompareTo(Unsafe.As<byte, int>(ref pTargetValue));
                case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_U:
#endif
                    return Unsafe.As<byte, uint>(ref pThisValue).CompareTo(Unsafe.As<byte, uint>(ref pTargetValue));
                case CorElementType.ELEMENT_TYPE_I8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_I:
#endif
                    return Unsafe.As<byte, long>(ref pThisValue).CompareTo(Unsafe.As<byte, long>(ref pTargetValue));
                case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_U:
#endif
                    return Unsafe.As<byte, ulong>(ref pThisValue).CompareTo(Unsafe.As<byte, ulong>(ref pTargetValue));
                case CorElementType.ELEMENT_TYPE_R4:
                    return Unsafe.As<byte, float>(ref pThisValue).CompareTo(Unsafe.As<byte, float>(ref pTargetValue));
                case CorElementType.ELEMENT_TYPE_R8:
                    return Unsafe.As<byte, double>(ref pThisValue).CompareTo(Unsafe.As<byte, double>(ref pTargetValue));
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }
        #endregion

        #region IFormattable
        [Obsolete("The provider argument is not used. Use ToString(String) instead.")]
        public string ToString(string? format, IFormatProvider? provider)
        {
            return ToString(format);
        }
        #endregion

        #region Public Methods
        public string ToString(string? format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return ToString();
            }

            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'G':
                    case 'g':
                        return ToString();

                    case 'D':
                    case 'd':
                        return ValueToString();

                    case 'X':
                    case 'x':
                        return ValueToHexString();

                    case 'F':
                    case 'f':
                        return InternalFlagsFormat((RuntimeType)GetType(), ToUInt64()) ?? ValueToString();
                }
            }

            throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
        }

        [Obsolete("The provider argument is not used. Use ToString() instead.")]
        public string ToString(IFormatProvider? provider)
        {
            return ToString();
        }

        #endregion

        #region IConvertible
        public TypeCode GetTypeCode()
        {
            return (InternalGetCorElementType()) switch
            {
                CorElementType.ELEMENT_TYPE_I1 => TypeCode.SByte,
                CorElementType.ELEMENT_TYPE_U1 => TypeCode.Byte,
                CorElementType.ELEMENT_TYPE_BOOLEAN => TypeCode.Boolean,
                CorElementType.ELEMENT_TYPE_I2 => TypeCode.Int16,
                CorElementType.ELEMENT_TYPE_U2 => TypeCode.UInt16,
                CorElementType.ELEMENT_TYPE_CHAR => TypeCode.Char,
                CorElementType.ELEMENT_TYPE_I4 => TypeCode.Int32,
                CorElementType.ELEMENT_TYPE_U4 => TypeCode.UInt32,
                CorElementType.ELEMENT_TYPE_I8 => TypeCode.Int64,
                CorElementType.ELEMENT_TYPE_U8 => TypeCode.UInt64,
                _ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType),
            };
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            return Convert.ToBoolean(GetValue());
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            return Convert.ToChar(GetValue());
        }

        sbyte IConvertible.ToSByte(IFormatProvider? provider)
        {
            return Convert.ToSByte(GetValue());
        }

        byte IConvertible.ToByte(IFormatProvider? provider)
        {
            return Convert.ToByte(GetValue());
        }

        short IConvertible.ToInt16(IFormatProvider? provider)
        {
            return Convert.ToInt16(GetValue());
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
        {
            return Convert.ToUInt16(GetValue());
        }

        int IConvertible.ToInt32(IFormatProvider? provider)
        {
            return Convert.ToInt32(GetValue());
        }

        uint IConvertible.ToUInt32(IFormatProvider? provider)
        {
            return Convert.ToUInt32(GetValue());
        }

        long IConvertible.ToInt64(IFormatProvider? provider)
        {
            return Convert.ToInt64(GetValue());
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            return Convert.ToUInt64(GetValue());
        }

        float IConvertible.ToSingle(IFormatProvider? provider)
        {
            return Convert.ToSingle(GetValue());
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            return Convert.ToDouble(GetValue());
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return Convert.ToDecimal(GetValue());
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Enum", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType(this, type, provider);
        }
        #endregion

        #region ToObject
        [CLSCompliant(false)]
        public static object ToObject(Type enumType, sbyte value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        public static object ToObject(Type enumType, short value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        public static object ToObject(Type enumType, int value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        public static object ToObject(Type enumType, byte value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        [CLSCompliant(false)]
        public static object ToObject(Type enumType, ushort value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        [CLSCompliant(false)]
        public static object ToObject(Type enumType, uint value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        public static object ToObject(Type enumType, long value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        [CLSCompliant(false)]
        public static object ToObject(Type enumType, ulong value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), unchecked((long)value));

        private static object ToObject(Type enumType, char value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        private static object ToObject(Type enumType, bool value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value ? 1L : 0L);

        #endregion

        private static RuntimeType ValidateRuntimeType(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            if (!(enumType is RuntimeType rtType))
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return rtType;
        }
    }
}
