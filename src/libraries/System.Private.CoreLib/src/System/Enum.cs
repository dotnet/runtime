// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

#if CORERT
using CorElementType = System.Runtime.RuntimeImports.RhCorElementType;
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
            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                    return Unsafe.As<byte, sbyte>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_U1:
                    return data.ToString();
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return Unsafe.As<byte, bool>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_I2:
                    return Unsafe.As<byte, short>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_U2:
                    return Unsafe.As<byte, ushort>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return Unsafe.As<byte, char>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_I4:
                    return Unsafe.As<byte, int>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_U4:
                    return Unsafe.As<byte, uint>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_R4:
                    return Unsafe.As<byte, float>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_I8:
                    return Unsafe.As<byte, long>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_U8:
                    return Unsafe.As<byte, ulong>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_R8:
                    return Unsafe.As<byte, double>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_I:
                    return Unsafe.As<byte, IntPtr>(ref data).ToString();
                case CorElementType.ELEMENT_TYPE_U:
                    return Unsafe.As<byte, UIntPtr>(ref data).ToString();
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }

        private string ValueToHexString()
        {
            ref byte data = ref this.GetRawData();
            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                case CorElementType.ELEMENT_TYPE_U1:
                    return data.ToString("X2", null);
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return Convert.ToByte(Unsafe.As<byte, bool>(ref data)).ToString("X2", null);
                case CorElementType.ELEMENT_TYPE_I2:
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return Unsafe.As<byte, ushort>(ref data).ToString("X4", null);
                case CorElementType.ELEMENT_TYPE_I4:
                case CorElementType.ELEMENT_TYPE_U4:
                    return Unsafe.As<byte, uint>(ref data).ToString("X8", null);
                case CorElementType.ELEMENT_TYPE_I8:
                case CorElementType.ELEMENT_TYPE_U8:
                    return Unsafe.As<byte, ulong>(ref data).ToString("X16", null);
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }

        private static string ValueToHexString(object value)
        {
            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.SByte:
                    return ((byte)(sbyte)value).ToString("X2", null);
                case TypeCode.Byte:
                    return ((byte)value).ToString("X2", null);
                case TypeCode.Boolean:
                    // direct cast from bool to byte is not allowed
                    return Convert.ToByte((bool)value).ToString("X2", null);
                case TypeCode.Int16:
                    return ((ushort)(short)value).ToString("X4", null);
                case TypeCode.UInt16:
                    return ((ushort)value).ToString("X4", null);
                case TypeCode.Char:
                    return ((ushort)(char)value).ToString("X4", null);
                case TypeCode.UInt32:
                    return ((uint)value).ToString("X8", null);
                case TypeCode.Int32:
                    return ((uint)(int)value).ToString("X8", null);
                case TypeCode.UInt64:
                    return ((ulong)value).ToString("X16", null);
                case TypeCode.Int64:
                    return ((ulong)(long)value).ToString("X16", null);
                // All unsigned types will be directly cast
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
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
                return InternalFlagsFormat(enumType, enumInfo, value);
            }
        }

        private static string? InternalFlagsFormat(RuntimeType enumType, ulong result)
        {
            return InternalFlagsFormat(enumType, GetEnumInfo(enumType), result);
        }

        private static string? InternalFlagsFormat(RuntimeType enumType, EnumInfo enumInfo, ulong resultValue)
        {
            Debug.Assert(enumType != null);

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
            name.AsSpan().CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(name.Length);
            while (--foundItemsCount >= 0)
            {
                resultSpan[0] = EnumSeparatorChar;
                resultSpan[1] = ' ';
                resultSpan = resultSpan.Slice(2);

                name = names[foundItems[foundItemsCount]];
                name.AsSpan().CopyTo(resultSpan);
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

            ulong result;
            switch (typeCode)
            {
                case TypeCode.SByte:
                    result = (ulong)(sbyte)value;
                    break;
                case TypeCode.Byte:
                    result = (byte)value;
                    break;
                case TypeCode.Boolean:
                    // direct cast from bool to byte is not allowed
                    result = Convert.ToByte((bool)value);
                    break;
                case TypeCode.Int16:
                    result = (ulong)(short)value;
                    break;
                case TypeCode.UInt16:
                    result = (ushort)value;
                    break;
                case TypeCode.Char:
                    result = (ushort)(char)value;
                    break;
                case TypeCode.UInt32:
                    result = (uint)value;
                    break;
                case TypeCode.Int32:
                    result = (ulong)(int)value;
                    break;
                case TypeCode.UInt64:
                    result = (ulong)value;
                    break;
                case TypeCode.Int64:
                    result = (ulong)(long)value;
                    break;
                // All unsigned types will be directly cast
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }

            return result;
        }

        #endregion

        #region Public Static Methods
        public static object Parse(Type enumType, string value) =>
            Parse(enumType, value, ignoreCase: false);

        public static object Parse(Type enumType, string value, bool ignoreCase)
        {
            bool success = TryParse(enumType, value, ignoreCase, throwOnFailure: true, out object? result);
            Debug.Assert(success);
            return result!;
        }

        public static TEnum Parse<TEnum>(string value) where TEnum : struct =>
            Parse<TEnum>(value, ignoreCase: false);

        public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct
        {
            bool success = TryParse<TEnum>(value, ignoreCase, throwOnFailure: true, out TEnum result);
            Debug.Assert(success);
            return result;
        }

        public static bool TryParse(Type enumType, string? value, out object? result) =>
            TryParse(enumType, value, ignoreCase: false, out result);

        public static bool TryParse(Type enumType, string? value, bool ignoreCase, out object? result) =>
            TryParse(enumType, value, ignoreCase, throwOnFailure: false, out result);

        private static bool TryParse(Type enumType, string? value, bool ignoreCase, bool throwOnFailure, out object? result)
        {
            // Validation on the enum type itself.  Failures here are considered non-parsing failures
            // and thus always throw rather than returning false.
            RuntimeType rt = ValidateRuntimeType(enumType);

            ReadOnlySpan<char> valueSpan = value.AsSpan().TrimStart();
            if (valueSpan.Length == 0)
            {
                if (throwOnFailure)
                {
                    throw value == null ?
                        new ArgumentNullException(nameof(value)) :
                        new ArgumentException(SR.Arg_MustContainEnumInfo, nameof(value));
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
                    parsed = TryParseInt32Enum(rt, value, valueSpan, sbyte.MinValue, sbyte.MaxValue, ignoreCase, throwOnFailure, TypeCode.SByte, out intResult);
                    result = parsed ? InternalBoxEnum(rt, intResult) : null;
                    return parsed;

                case TypeCode.Int16:
                    parsed = TryParseInt32Enum(rt, value, valueSpan, short.MinValue, short.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int16, out intResult);
                    result = parsed ? InternalBoxEnum(rt, intResult) : null;
                    return parsed;

                case TypeCode.Int32:
                    parsed = TryParseInt32Enum(rt, value, valueSpan, int.MinValue, int.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int32, out intResult);
                    result = parsed ? InternalBoxEnum(rt, intResult) : null;
                    return parsed;

                case TypeCode.Byte:
                    parsed = TryParseUInt32Enum(rt, value, valueSpan, byte.MaxValue, ignoreCase, throwOnFailure, TypeCode.Byte, out uintResult);
                    result = parsed ? InternalBoxEnum(rt, uintResult) : null;
                    return parsed;

                case TypeCode.UInt16:
                    parsed = TryParseUInt32Enum(rt, value, valueSpan, ushort.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt16, out uintResult);
                    result = parsed ? InternalBoxEnum(rt, uintResult) : null;
                    return parsed;

                case TypeCode.UInt32:
                    parsed = TryParseUInt32Enum(rt, value, valueSpan, uint.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt32, out uintResult);
                    result = parsed ? InternalBoxEnum(rt, uintResult) : null;
                    return parsed;

                case TypeCode.Int64:
                    parsed = TryParseInt64Enum(rt, value, valueSpan, ignoreCase, throwOnFailure, out long longResult);
                    result = parsed ? InternalBoxEnum(rt, longResult) : null;
                    return parsed;

                case TypeCode.UInt64:
                    parsed = TryParseUInt64Enum(rt, value, valueSpan, ignoreCase, throwOnFailure, out ulong ulongResult);
                    result = parsed ? InternalBoxEnum(rt, (long)ulongResult) : null;
                    return parsed;

                default:
                    return TryParseRareEnum(rt, value, valueSpan, ignoreCase, throwOnFailure, out result);
            }
        }

        public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct =>
            TryParse<TEnum>(value, ignoreCase: false, out result);

        public static bool TryParse<TEnum>(string? value, bool ignoreCase, out TEnum result) where TEnum : struct =>
            TryParse<TEnum>(value, ignoreCase, throwOnFailure: false, out result);

        private static bool TryParse<TEnum>(string? value, bool ignoreCase, bool throwOnFailure, out TEnum result) where TEnum : struct
        {
            // Validation on the enum type itself.  Failures here are considered non-parsing failures
            // and thus always throw rather than returning false.
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(TEnum));
            }

            ReadOnlySpan<char> valueSpan = value.AsSpan().TrimStart();
            if (valueSpan.Length == 0)
            {
                if (throwOnFailure)
                {
                    throw value == null ?
                        new ArgumentNullException(nameof(value)) :
                        new ArgumentException(SR.Arg_MustContainEnumInfo, nameof(value));
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
                    parsed = TryParseInt32Enum(rt, value, valueSpan, sbyte.MinValue, sbyte.MaxValue, ignoreCase, throwOnFailure, TypeCode.SByte, out intResult);
                    sbyte sbyteResult = (sbyte)intResult;
                    result = Unsafe.As<sbyte, TEnum>(ref sbyteResult);
                    return parsed;

                case TypeCode.Int16:
                    parsed = TryParseInt32Enum(rt, value, valueSpan, short.MinValue, short.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int16, out intResult);
                    short shortResult = (short)intResult;
                    result = Unsafe.As<short, TEnum>(ref shortResult);
                    return parsed;

                case TypeCode.Int32:
                    parsed = TryParseInt32Enum(rt, value, valueSpan, int.MinValue, int.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int32, out intResult);
                    result = Unsafe.As<int, TEnum>(ref intResult);
                    return parsed;

                case TypeCode.Byte:
                    parsed = TryParseUInt32Enum(rt, value, valueSpan, byte.MaxValue, ignoreCase, throwOnFailure, TypeCode.Byte, out uintResult);
                    byte byteResult = (byte)uintResult;
                    result = Unsafe.As<byte, TEnum>(ref byteResult);
                    return parsed;

                case TypeCode.UInt16:
                    parsed = TryParseUInt32Enum(rt, value, valueSpan, ushort.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt16, out uintResult);
                    ushort ushortResult = (ushort)uintResult;
                    result = Unsafe.As<ushort, TEnum>(ref ushortResult);
                    return parsed;

                case TypeCode.UInt32:
                    parsed = TryParseUInt32Enum(rt, value, valueSpan, uint.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt32, out uintResult);
                    result = Unsafe.As<uint, TEnum>(ref uintResult);
                    return parsed;

                case TypeCode.Int64:
                    parsed = TryParseInt64Enum(rt, value, valueSpan, ignoreCase, throwOnFailure, out long longResult);
                    result = Unsafe.As<long, TEnum>(ref longResult);
                    return parsed;

                case TypeCode.UInt64:
                    parsed = TryParseUInt64Enum(rt, value, valueSpan, ignoreCase, throwOnFailure, out ulong ulongResult);
                    result = Unsafe.As<ulong, TEnum>(ref ulongResult);
                    return parsed;

                default:
                    parsed = TryParseRareEnum(rt, value, valueSpan, ignoreCase, throwOnFailure, out object? objectResult);
                    result = parsed ? (TEnum)objectResult! : default; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/34976
                    return parsed;
            }
        }

        /// <summary>Tries to parse the value of an enum with known underlying types that fit in an Int32 (Int32, Int16, and SByte).</summary>
        private static bool TryParseInt32Enum(
            RuntimeType enumType, string? originalValueString, ReadOnlySpan<char> value, int minInclusive, int maxInclusive, bool ignoreCase, bool throwOnFailure, TypeCode type, out int result)
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
            else if (TryParseByName(enumType, originalValueString, value, ignoreCase, throwOnFailure, out ulong ulongResult))
            {
                result = (int)ulongResult;
                Debug.Assert(result >= minInclusive && result <= maxInclusive);
                return true;
            }

            result = 0;
            return false;
        }

        /// <summary>Tries to parse the value of an enum with known underlying types that fit in a UInt32 (UInt32, UInt16, and Byte).</summary>
        private static bool TryParseUInt32Enum(RuntimeType enumType, string? originalValueString, ReadOnlySpan<char> value, uint maxInclusive, bool ignoreCase, bool throwOnFailure, TypeCode type, out uint result)
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
            else if (TryParseByName(enumType, originalValueString, value, ignoreCase, throwOnFailure, out ulong ulongResult))
            {
                result = (uint)ulongResult;
                Debug.Assert(result <= maxInclusive);
                return true;
            }

            result = 0;
            return false;
        }

        /// <summary>Tries to parse the value of an enum with Int64 as the underlying type.</summary>
        private static bool TryParseInt64Enum(RuntimeType enumType, string? originalValueString, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out long result)
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
            else if (TryParseByName(enumType, originalValueString, value, ignoreCase, throwOnFailure, out ulong ulongResult))
            {
                result = (long)ulongResult;
                return true;
            }

            result = 0;
            return false;
        }

        /// <summary>Tries to parse the value of an enum with UInt64 as the underlying type.</summary>
        private static bool TryParseUInt64Enum(RuntimeType enumType, string? originalValueString, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out ulong result)
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
            else if (TryParseByName(enumType, originalValueString, value, ignoreCase, throwOnFailure, out result))
            {
                return true;
            }

            result = 0;
            return false;
        }

        /// <summary>Tries to parse the value of an enum with an underlying type that can't be expressed in C# (e.g. char, bool, double, etc.)</summary>
        private static bool TryParseRareEnum(RuntimeType enumType, string? originalValueString, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out object? result)
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

            if (TryParseByName(enumType, originalValueString, value, ignoreCase, throwOnFailure, out ulong ulongResult))
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

        private static bool TryParseByName(RuntimeType enumType, string? originalValueString, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out ulong result)
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
                throw new ArgumentException(SR.Format(SR.Arg_EnumValueNotFound, originalValueString));
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

            switch (typeCode)
            {
                case TypeCode.Int32:
                    return ToObject(enumType, (int)value);

                case TypeCode.SByte:
                    return ToObject(enumType, (sbyte)value);

                case TypeCode.Int16:
                    return ToObject(enumType, (short)value);

                case TypeCode.Int64:
                    return ToObject(enumType, (long)value);

                case TypeCode.UInt32:
                    return ToObject(enumType, (uint)value);

                case TypeCode.Byte:
                    return ToObject(enumType, (byte)value);

                case TypeCode.UInt16:
                    return ToObject(enumType, (ushort)value);

                case TypeCode.UInt64:
                    return ToObject(enumType, (ulong)value);

                case TypeCode.Char:
                    return ToObject(enumType, (char)value);

                case TypeCode.Boolean:
                    return ToObject(enumType, (bool)value);

                default:
                    // All unsigned types will be directly cast
                    throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, nameof(value));
            }
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
                        return GetEnumName(rtType, ToUInt64(value)) ?? value.ToString()!;

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
            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                    return Unsafe.As<byte, sbyte>(ref data);
                case CorElementType.ELEMENT_TYPE_U1:
                    return data;
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return Unsafe.As<byte, bool>(ref data);
                case CorElementType.ELEMENT_TYPE_I2:
                    return Unsafe.As<byte, short>(ref data);
                case CorElementType.ELEMENT_TYPE_U2:
                    return Unsafe.As<byte, ushort>(ref data);
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return Unsafe.As<byte, char>(ref data);
                case CorElementType.ELEMENT_TYPE_I4:
                    return Unsafe.As<byte, int>(ref data);
                case CorElementType.ELEMENT_TYPE_U4:
                    return Unsafe.As<byte, uint>(ref data);
                case CorElementType.ELEMENT_TYPE_R4:
                    return Unsafe.As<byte, float>(ref data);
                case CorElementType.ELEMENT_TYPE_I8:
                    return Unsafe.As<byte, long>(ref data);
                case CorElementType.ELEMENT_TYPE_U8:
                    return Unsafe.As<byte, ulong>(ref data);
                case CorElementType.ELEMENT_TYPE_R8:
                    return Unsafe.As<byte, double>(ref data);
                case CorElementType.ELEMENT_TYPE_I:
                    return Unsafe.As<byte, IntPtr>(ref data);
                case CorElementType.ELEMENT_TYPE_U:
                    return Unsafe.As<byte, UIntPtr>(ref data);
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
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
                    return Convert.ToUInt64(Unsafe.As<byte, bool>(ref data), CultureInfo.InvariantCulture);
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
            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                    return Unsafe.As<byte, sbyte>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_U1:
                    return data.GetHashCode();
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return Unsafe.As<byte, bool>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_I2:
                    return Unsafe.As<byte, short>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_U2:
                    return Unsafe.As<byte, ushort>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return Unsafe.As<byte, char>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_I4:
                    return Unsafe.As<byte, int>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_U4:
                    return Unsafe.As<byte, uint>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_R4:
                    return Unsafe.As<byte, float>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_I8:
                    return Unsafe.As<byte, long>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_U8:
                    return Unsafe.As<byte, ulong>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_R8:
                    return Unsafe.As<byte, double>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_I:
                    return Unsafe.As<byte, IntPtr>(ref data).GetHashCode();
                case CorElementType.ELEMENT_TYPE_U:
                    return Unsafe.As<byte, UIntPtr>(ref data).GetHashCode();
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
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
        #endregion

        #region IFormattable
        [Obsolete("The provider argument is not used. Please use ToString(String).")]
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

        [Obsolete("The provider argument is not used. Please use ToString().")]
        public string ToString(IFormatProvider? provider)
        {
            return ToString();
        }

        #endregion

        #region IConvertible
        public TypeCode GetTypeCode()
        {
            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                    return TypeCode.SByte;
                case CorElementType.ELEMENT_TYPE_U1:
                    return TypeCode.Byte;
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return TypeCode.Boolean;
                case CorElementType.ELEMENT_TYPE_I2:
                    return TypeCode.Int16;
                case CorElementType.ELEMENT_TYPE_U2:
                    return TypeCode.UInt16;
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return TypeCode.Char;
                case CorElementType.ELEMENT_TYPE_I4:
                    return TypeCode.Int32;
                case CorElementType.ELEMENT_TYPE_U4:
                    return TypeCode.UInt32;
                case CorElementType.ELEMENT_TYPE_I8:
                    return TypeCode.Int64;
                case CorElementType.ELEMENT_TYPE_U8:
                    return TypeCode.UInt64;
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            return Convert.ToBoolean(GetValue(), CultureInfo.CurrentCulture);
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            return Convert.ToChar(GetValue(), CultureInfo.CurrentCulture);
        }

        sbyte IConvertible.ToSByte(IFormatProvider? provider)
        {
            return Convert.ToSByte(GetValue(), CultureInfo.CurrentCulture);
        }

        byte IConvertible.ToByte(IFormatProvider? provider)
        {
            return Convert.ToByte(GetValue(), CultureInfo.CurrentCulture);
        }

        short IConvertible.ToInt16(IFormatProvider? provider)
        {
            return Convert.ToInt16(GetValue(), CultureInfo.CurrentCulture);
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
        {
            return Convert.ToUInt16(GetValue(), CultureInfo.CurrentCulture);
        }

        int IConvertible.ToInt32(IFormatProvider? provider)
        {
            return Convert.ToInt32(GetValue(), CultureInfo.CurrentCulture);
        }

        uint IConvertible.ToUInt32(IFormatProvider? provider)
        {
            return Convert.ToUInt32(GetValue(), CultureInfo.CurrentCulture);
        }

        long IConvertible.ToInt64(IFormatProvider? provider)
        {
            return Convert.ToInt64(GetValue(), CultureInfo.CurrentCulture);
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            return Convert.ToUInt64(GetValue(), CultureInfo.CurrentCulture);
        }

        float IConvertible.ToSingle(IFormatProvider? provider)
        {
            return Convert.ToSingle(GetValue(), CultureInfo.CurrentCulture);
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            return Convert.ToDouble(GetValue(), CultureInfo.CurrentCulture);
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return Convert.ToDecimal(GetValue(), CultureInfo.CurrentCulture);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Enum", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
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
            InternalBoxEnum(ValidateRuntimeType(enumType), value ? 1 : 0);

        #endregion
    }
}
