// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract partial class Enum : ValueType, IComparable, IFormattable, IConvertible
    {
        #region Private Constants
        private const char EnumSeparatorChar = ',';
        #endregion

        #region Private Static Methods

        private string AsNumberToString()
        {
            ref byte data = ref this.GetRawData();
            return InternalGetCorElementType() switch
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // optimizes into a single TryFormat call for all types expressible in C#
        private static bool TryAsNumberToString<TEnum>(TEnum value, Span<char> destination, out int charsWritten)
        {
            Type underlyingType = GetUnderlyingType(typeof(TEnum));

            if (underlyingType == typeof(int)) return Unsafe.As<TEnum, int>(ref value).TryFormat(destination, out charsWritten);
            if (underlyingType == typeof(uint)) return Unsafe.As<TEnum, uint>(ref value).TryFormat(destination, out charsWritten);

            if (underlyingType == typeof(byte)) return Unsafe.As<TEnum, byte>(ref value).TryFormat(destination, out charsWritten);
            if (underlyingType == typeof(sbyte)) return Unsafe.As<TEnum, sbyte>(ref value).TryFormat(destination, out charsWritten);

            if (underlyingType == typeof(long)) return Unsafe.As<TEnum, long>(ref value).TryFormat(destination, out charsWritten);
            if (underlyingType == typeof(ulong)) return Unsafe.As<TEnum, ulong>(ref value).TryFormat(destination, out charsWritten);

            if (underlyingType == typeof(short)) return Unsafe.As<TEnum, short>(ref value).TryFormat(destination, out charsWritten);
            if (underlyingType == typeof(ushort)) return Unsafe.As<TEnum, ushort>(ref value).TryFormat(destination, out charsWritten);

            if (underlyingType == typeof(nint)) return Unsafe.As<TEnum, nint>(ref value).TryFormat(destination, out charsWritten);
            if (underlyingType == typeof(nuint)) return Unsafe.As<TEnum, nuint>(ref value).TryFormat(destination, out charsWritten);

            if (underlyingType == typeof(float)) return Unsafe.As<TEnum, float>(ref value).TryFormat(destination, out charsWritten);
            if (underlyingType == typeof(double)) return Unsafe.As<TEnum, double>(ref value).TryFormat(destination, out charsWritten);

            if (underlyingType == typeof(bool)) return Unsafe.As<TEnum, bool>(ref value).TryFormat(destination, out charsWritten);
            if (underlyingType == typeof(char))
            {
                if (!destination.IsEmpty)
                {
                    destination[0] = Unsafe.As<TEnum, char>(ref value);
                    charsWritten = 1;
                    return true;
                }
                charsWritten = 0;
                return false;
            }

            throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
        }

        private string AsNumberToHexString()
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // optimizes into a single TryFormat call for all types expressible in C#
        private static bool TryAsNumberToHexString<TEnum>(TEnum value, Span<char> destination, out int charsWritten)
        {
            Type underlyingType = GetUnderlyingType(typeof(TEnum));

            if (underlyingType == typeof(int) || underlyingType == typeof(uint)
#if TARGET_32BIT
                || underlyingType == typeof(nint) || underlyingType == typeof(nuint)
#endif
                )
            {
                return Unsafe.As<TEnum, uint>(ref value).TryFormat(destination, out charsWritten, "X8");
            }

            if (underlyingType == typeof(byte) || underlyingType == typeof(sbyte))
            {
                return Unsafe.As<TEnum, byte>(ref value).TryFormat(destination, out charsWritten, "X2");
            }

            if (underlyingType == typeof(long) || underlyingType == typeof(ulong)
#if TARGET_64BIT
                || underlyingType == typeof(nint) || underlyingType == typeof(nuint)
#endif
                )
            {
                return Unsafe.As<TEnum, ulong>(ref value).TryFormat(destination, out charsWritten, "X16");
            }

            if (underlyingType == typeof(short) || underlyingType == typeof(ushort) || underlyingType == typeof(char))
            {
                return Unsafe.As<TEnum, ushort>(ref value).TryFormat(destination, out charsWritten, "X4");
            }

            if (underlyingType == typeof(bool))
            {
                bool copied = (Unsafe.As<TEnum, bool>(ref value) ? "01" : "00").TryCopyTo(destination);
                charsWritten = copied ? 2 : 0;
                return copied;
            }

            throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
        }

        private static string AsNumberToHexString(object value) =>
            Convert.GetTypeCode(value) switch
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

        internal static string? GetEnumName(RuntimeType enumType, ulong ulValue) =>
            GetEnumName(GetEnumInfo(enumType), ulValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? GetEnumName(EnumInfo enumInfo, ulong ulValue)
        {
            string[] names = enumInfo.Names;
            if (enumInfo.ValuesAreSequentialFromZero)
            {
                if (ulValue < (ulong)names.Length)
                {
                    return names[(uint)ulValue];
                }
            }
            else
            {
                int index = FindDefinedIndex(enumInfo.Values, ulValue);
                if ((uint)index < (uint)names.Length)
                {
                    return enumInfo.Names[index];
                }
            }

            return null; // return null so the caller knows to .ToString() the input
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? GetEnumName<TEnum>(ulong ulValue)
        {
            string[] names = GenericEnumInfo<TEnum>.Names;
            if (GenericEnumInfo<TEnum>.ValuesAreSequentialFromZero)
            {
                if (ulValue < (ulong)names.Length)
                {
                    return names[(uint)ulValue];
                }
            }
            else
            {
                int index = FindDefinedIndex(GenericEnumInfo<TEnum>.Values, ulValue);
                if ((uint)index < (uint)names.Length)
                {
                    return names[index];
                }
            }

            return null; // return null so the caller knows to .ToString() the input
        }

        private static string? FormatSingleNameOrFlagNames(RuntimeType enumType, ulong value)
        {
            EnumInfo enumInfo = GetEnumInfo(enumType);

            if (!enumInfo.HasFlagsAttribute)
            {
                return GetEnumName(enumInfo, value);
            }

            // These are flags OR'ed together (We treat everything as unsigned types)
            return FormatFlagNames(enumInfo, value);
        }

        private static string? FormatFlagNames(EnumInfo enumInfo, ulong resultValue)
        {
            string[] names = enumInfo.Names;
            ulong[] values = enumInfo.Values;
            Debug.Assert(names.Length == values.Length);

            string? result = GetSingleFlagsEnumNameForValue(resultValue, names, values, out int index);
            if (result is null)
            {
                // With a ulong result value, regardless of the enum's base type, the maximum
                // possible number of consistent name/values we could have is 64, since every
                // value is made up of one or more bits, and when we see values and incorporate
                // their names, we effectively switch off those bits.
                Span<int> foundItems = stackalloc int[64];
                if (TryFindFlagsNames(resultValue, names, values, index, foundItems, out int resultLength, out int foundItemsCount))
                {
                    foundItems = foundItems.Slice(0, foundItemsCount);
                    int length = GetMultipleEnumsFlagsFormatResultLength(resultLength, foundItemsCount);

                    result = string.FastAllocateString(length);
                    WriteMultipleFoundFlagsNames(names, foundItems, new Span<char>(ref result.GetRawStringData(), result.Length));
                }
            }

            return result;
        }

        private static bool TryFormatFlagNames(EnumInfo enumInfo, ulong resultValue, Span<char> destination, out int charsWritten, ref bool isDestinationTooSmall)
        {
            Debug.Assert(!isDestinationTooSmall);

            string[] names = enumInfo.Names;
            ulong[] values = enumInfo.Values;
            Debug.Assert(names.Length == values.Length);

            if (GetSingleFlagsEnumNameForValue(resultValue, names, values, out int index) is string singleEnumFlagsFormat)
            {
                if (singleEnumFlagsFormat.TryCopyTo(destination))
                {
                    charsWritten = singleEnumFlagsFormat.Length;
                    return true;
                }

                isDestinationTooSmall = true;
            }
            else
            {
                // With a ulong result value, regardless of the enum's base type, the maximum
                // possible number of consistent name/values we could have is 64, since every
                // value is made up of one or more bits, and when we see values and incorporate
                // their names, we effectively switch off those bits.
                Span<int> foundItems = stackalloc int[64];
                if (TryFindFlagsNames(resultValue, names, values, index, foundItems, out int resultLength, out int foundItemsCount))
                {
                    foundItems = foundItems.Slice(0, foundItemsCount);
                    int length = GetMultipleEnumsFlagsFormatResultLength(resultLength, foundItemsCount);

                    if (length <= destination.Length)
                    {
                        charsWritten = length;
                        WriteMultipleFoundFlagsNames(names, foundItems, destination);
                        return true;
                    }

                    isDestinationTooSmall = true;
                }
            }

            charsWritten = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // used twice, once from string-based and once from span-based code path
        private static int GetMultipleEnumsFlagsFormatResultLength(int resultLength, int foundItemsCount)
        {
            Debug.Assert(foundItemsCount >= 2);
            Debug.Assert(foundItemsCount <= 64);

            const int SeparatorStringLength = 2; // ", "
            int allSeparatorsLength = SeparatorStringLength * (foundItemsCount - 1);
            return checked(resultLength + allSeparatorsLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // used twice, once from string-based and once from span-based code path
        private static string? GetSingleFlagsEnumNameForValue(ulong resultValue, string[] names, ulong[] values, out int index)
        {
            // Values are sorted, so if the incoming value is 0, we can check to see whether
            // the first entry matches it, in which case we can return its name; otherwise,
            // we can just return "0".
            if (resultValue == 0)
            {
                index = 0;
                return values.Length > 0 && values[0] == 0 ?
                    names[0] :
                    "0";
            }

            // Walk from largest to smallest. It's common to have a flags enum with a single
            // value that matches a single entry, in which case we can just return the existing
            // name string.
            int i;
            for (i = values.Length - 1; (uint)i < (uint)values.Length; i--)
            {
                if (values[i] <= resultValue)
                {
                    if (values[i] == resultValue)
                    {
                        index = i;
                        return names[i];
                    }

                    break;
                }
            }

            index = i;
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // used twice, once from string-based and once from span-based code path
        private static bool TryFindFlagsNames(ulong resultValue, string[] names, ulong[] values, int index, Span<int> foundItems, out int resultLength, out int foundItemsCount)
        {
            // Now look for multiple matches, storing the indices of the values
            // into our span.
            resultLength = 0;
            foundItemsCount = 0;

            while (true)
            {
                if ((uint)index >= (uint)values.Length)
                {
                    break;
                }

                ulong currentValue = values[index];
                if (((uint)index | currentValue) == 0)
                {
                    break;
                }

                if ((resultValue & currentValue) == currentValue)
                {
                    resultValue -= currentValue;
                    foundItems[foundItemsCount] = index;
                    foundItemsCount++;
                    resultLength = checked(resultLength + names[index].Length);
                }

                index--;
            }

            // If we exhausted looking through all the values and we still have
            // a non-zero result, we couldn't match the result to only named values.
            // In that case, we return null and let the call site just generate
            // a string for the integral value if it desires.
            return resultValue == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // used twice, once from string-based and once from span-based code path
        private static void WriteMultipleFoundFlagsNames(string[] names, ReadOnlySpan<int> foundItems, Span<char> destination)
        {
            Debug.Assert(foundItems.Length >= 2);

            for (int i = foundItems.Length - 1; i != 0; i--)
            {
                string name = names[foundItems[i]];
                name.CopyTo(destination);
                destination = destination.Slice(name.Length);
                Span<char> afterSeparator = destination.Slice(2); // done before copying ", " to eliminate those two bounds checks
                destination[0] = EnumSeparatorChar;
                destination[1] = ' ';
                destination = afterSeparator;
            }

            names[foundItems[0]].CopyTo(destination);
        }

        internal static ulong ToUInt64(object value) =>
            // Helper function to silently convert the value to UInt64 from the other base types for enum without throwing an exception.
            // This is need since the Convert functions do overflow checks.
            Convert.GetTypeCode(value) switch
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

        private static ulong ToUInt64<TEnum>(TEnum value)
        {
            Type underlyingType = GetUnderlyingType(typeof(TEnum));

            if (underlyingType == typeof(int)) return (ulong)Unsafe.As<TEnum, int>(ref value);
            if (underlyingType == typeof(uint)) return Unsafe.As<TEnum, uint>(ref value);

            if (underlyingType == typeof(byte)) return Unsafe.As<TEnum, byte>(ref value);
            if (underlyingType == typeof(sbyte)) return (ulong)Unsafe.As<TEnum, sbyte>(ref value);

            if (underlyingType == typeof(long)) return (ulong)Unsafe.As<TEnum, long>(ref value);
            if (underlyingType == typeof(ulong)) return Unsafe.As<TEnum, ulong>(ref value);

            if (underlyingType == typeof(short)) return (ulong)Unsafe.As<TEnum, short>(ref value);
            if (underlyingType == typeof(ushort)) return Unsafe.As<TEnum, ushort>(ref value);

            if (underlyingType == typeof(nint)) return (ulong)Unsafe.As<TEnum, nint>(ref value);
            if (underlyingType == typeof(nuint)) return Unsafe.As<TEnum, nuint>(ref value);

            if (underlyingType == typeof(bool)) return Unsafe.As<TEnum, bool>(ref value) ? 1UL : 0UL;
            if (underlyingType == typeof(char)) return Unsafe.As<TEnum, char>(ref value);

            throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
        }
#endregion

        #region Public Static Methods
        public static string? GetName<TEnum>(TEnum value) where TEnum : struct, Enum =>
            GetEnumName<TEnum>(ToUInt64(value));

        public static string? GetName(Type enumType, object value)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.GetEnumName(value);
        }

        public static string[] GetNames<TEnum>() where TEnum : struct, Enum =>
            new ReadOnlySpan<string>(GenericEnumInfo<TEnum>.Names).ToArray();

        public static string[] GetNames(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.GetEnumNames();
        }

        internal static string[] InternalGetNames(RuntimeType enumType) =>
            // Get all of the names
            GetEnumInfo(enumType).Names;

        public static Type GetUnderlyingType(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.GetEnumUnderlyingType();
        }

#if !NATIVEAOT
        public static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum =>
            (TEnum[])GetValues(typeof(TEnum));
#endif

        [RequiresDynamicCode("It might not be possible to create an array of the enum type at runtime. Use the GetValues<TEnum> overload or the GetValuesAsUnderlyingType method instead.")]
        public static Array GetValues(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.GetEnumValues();
        }

        /// <summary>
        /// Retrieves an array of the values of the underlying type constants in a specified enumeration type.
        /// </summary>
        /// <typeparam name="TEnum">An enumeration type.</typeparam>
        /// /// <remarks>
        /// You can use this method to get enumeration values when it's hard to create an array of the enumeration type.
        /// For example, you might use this method for the <see cref="T:System.Reflection.MetadataLoadContext" /> enumeration or on a platform where run-time code generation is not available.
        /// </remarks>
        /// <returns>An array that contains the values of the underlying type constants in <typeparamref name="TEnum" />.</returns>
        public static Array GetValuesAsUnderlyingType<TEnum>() where TEnum : struct, Enum =>
            typeof(TEnum).GetEnumValuesAsUnderlyingType();

        /// <summary>
        /// Retrieves an array of the values of the underlying type constants in a specified enumeration.
        /// </summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <remarks>
        /// You can use this method to get enumeration values when it's hard to create an array of the enumeration type.
        /// For example, you might use this method for the <see cref="T:System.Reflection.MetadataLoadContext" /> enumeration or on a platform where run-time code generation is not available.
        /// </remarks>
        /// <returns>An array that contains the values of the underlying type constants in  <paramref name="enumType" />.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="enumType" /> is null.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="enumType" /> is not an enumeration type.</exception>
        public static Array GetValuesAsUnderlyingType(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.GetEnumValuesAsUnderlyingType();
        }

        [Intrinsic]
        public bool HasFlag(Enum flag)
        {
            ArgumentNullException.ThrowIfNull(flag);

            if (GetType() != flag.GetType() && !GetType().IsEquivalentTo(flag.GetType()))
                throw new ArgumentException(SR.Format(SR.Argument_EnumTypeDoesNotMatch, flag.GetType(), GetType()));

            ref byte pThisValue = ref this.GetRawData();
            ref byte pFlagsValue = ref flag.GetRawData();

            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                case CorElementType.ELEMENT_TYPE_U1:
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    {
                        byte flagsValue = pFlagsValue;
                        return (pThisValue & flagsValue) == flagsValue;
                    }

                case CorElementType.ELEMENT_TYPE_I2:
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    {
                        ushort flagsValue = Unsafe.As<byte, ushort>(ref pFlagsValue);
                        return (Unsafe.As<byte, ushort>(ref pThisValue) & flagsValue) == flagsValue;
                    }

                case CorElementType.ELEMENT_TYPE_I4:
                case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R4:
                    {
                        uint flagsValue = Unsafe.As<byte, uint>(ref pFlagsValue);
                        return (Unsafe.As<byte, uint>(ref pThisValue) & flagsValue) == flagsValue;
                    }

                case CorElementType.ELEMENT_TYPE_I8:
                case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R8:
                    {
                        ulong flagsValue = Unsafe.As<byte, ulong>(ref pFlagsValue);
                        return (Unsafe.As<byte, ulong>(ref pThisValue) & flagsValue) == flagsValue;
                    }

                default:
                    Debug.Fail("Unknown enum underlying type");
                    return false;
            }
        }

        internal static ulong[] InternalGetValues(RuntimeType enumType)
        {
            // Get all of the values
            return GetEnumInfo(enumType, getNames: false).Values;
        }

        public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            // If the enum's values are all sequentially numbered starting from 0, then we can
            // just return if the requested index is in range. Otherwise, search for the value.
            ulong ulValue = ToUInt64(value);
            return
                GenericEnumInfo<TEnum>.ValuesAreSequentialFromZero ? ulValue < (ulong)GenericEnumInfo<TEnum>.Values.Length :
                FindDefinedIndex(GenericEnumInfo<TEnum>.Values, ulValue) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindDefinedIndex(ulong[] ulValues, ulong ulValue)
        {
            // Binary searching has a higher constant overhead than linear.
            // For smaller enums, use IndexOf.  For larger enums, use BinarySearch.
            // This threshold can be tweaked over time as optimizations evolve.
            const int NumberOfValuesThreshold = 32;

            int ulValuesLength = ulValues.Length;
            ref ulong start = ref MemoryMarshal.GetArrayDataReference(ulValues);
            return ulValuesLength <= NumberOfValuesThreshold ?
                SpanHelpers.IndexOfValueType(ref Unsafe.As<ulong, long>(ref start), (long)ulValue, ulValuesLength) :
                SpanHelpers.BinarySearch(ref start, ulValuesLength, ulValue);
        }

        public static bool IsDefined(Type enumType, object value)
        {
            ArgumentNullException.ThrowIfNull(enumType);

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

        public static bool TryParse(Type enumType, string? value, [NotNullWhen(true)] out object? result) =>
            TryParse(enumType, value, ignoreCase: false, out result);

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The span representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryParse(Type enumType, ReadOnlySpan<char> value, [NotNullWhen(true)] out object? result) =>
          TryParse(enumType, value, ignoreCase: false, out result);

        public static bool TryParse(Type enumType, string? value, bool ignoreCase, [NotNullWhen(true)] out object? result) =>
            TryParse(enumType, value, ignoreCase, throwOnFailure: false, out result);

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object. A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The span representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="ignoreCase"><see langword="true"/> to read <paramref name="enumType"/> in case insensitive mode; <see langword="false"/> to read <paramref name="enumType"/> in case sensitive mode.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, [NotNullWhen(true)] out object? result) =>
            TryParse(enumType, value, ignoreCase, throwOnFailure: false, out result);

        private static bool TryParse(Type enumType, string? value, bool ignoreCase, bool throwOnFailure, [NotNullWhen(true)] out object? result)
        {
            if (value == null)
            {
                if (throwOnFailure)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
                }

                result = null;
                return false;
            }

            return TryParse(enumType, value.AsSpan(), ignoreCase, throwOnFailure, out result);
        }

        private static bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, [NotNullWhen(true)] out object? result)
        {
            // Validation on the enum type itself.  Failures here are considered non-parsing failures
            // and thus always throw rather than returning false.
            RuntimeType rt = ValidateRuntimeType(enumType);
            value = value.TrimStart();
            if (value.Length == 0)
            {
                if (throwOnFailure)
                {
                    ThrowInvalidEmptyParseArgument();
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
                    ArgumentNullException.Throw(nameof(value));
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
            if (!typeof(TEnum).IsEnum) // with IsEnum being an intrinsic, this whole block will be eliminated for all meaningful cases
            {
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(TEnum));
            }

            value = value.TrimStart();
            if (value.IsEmpty)
            {
                if (throwOnFailure)
                {
                    ThrowInvalidEmptyParseArgument();
                }

                result = default;
                return false;
            }

            int intResult;
            uint uintResult;
            bool parsed;
            RuntimeType rt = (RuntimeType)typeof(TEnum);

            Type underlyingType = GetUnderlyingType(typeof(TEnum));

            if (underlyingType == typeof(int))
            {
                parsed = TryParseInt32Enum(rt, value, int.MinValue, int.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int32, out intResult);
                result = Unsafe.As<int, TEnum>(ref intResult);
                return parsed;
            }

            if (underlyingType == typeof(uint))
            {
                parsed = TryParseUInt32Enum(rt, value, uint.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt32, out uintResult);
                result = Unsafe.As<uint, TEnum>(ref uintResult);
                return parsed;
            }

            if (underlyingType == typeof(byte))
            {
                parsed = TryParseUInt32Enum(rt, value, byte.MaxValue, ignoreCase, throwOnFailure, TypeCode.Byte, out uintResult);
                byte byteResult = (byte)uintResult;
                result = Unsafe.As<byte, TEnum>(ref byteResult);
                return parsed;
            }

            if (underlyingType == typeof(sbyte))
            {
                parsed = TryParseInt32Enum(rt, value, sbyte.MinValue, sbyte.MaxValue, ignoreCase, throwOnFailure, TypeCode.SByte, out intResult);
                sbyte sbyteResult = (sbyte)intResult;
                result = Unsafe.As<sbyte, TEnum>(ref sbyteResult);
                return parsed;
            }

            if (underlyingType == typeof(long))
            {
                parsed = TryParseInt64Enum(rt, value, ignoreCase, throwOnFailure, out long longResult);
                result = Unsafe.As<long, TEnum>(ref longResult);
                return parsed;
            }

            if (underlyingType == typeof(ulong))
            {
                parsed = TryParseUInt64Enum(rt, value, ignoreCase, throwOnFailure, out ulong ulongResult);
                result = Unsafe.As<ulong, TEnum>(ref ulongResult);
                return parsed;
            }

            if (underlyingType == typeof(short))
            {
                parsed = TryParseInt32Enum(rt, value, short.MinValue, short.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int16, out intResult);
                short shortResult = (short)intResult;
                result = Unsafe.As<short, TEnum>(ref shortResult);
                return parsed;
            }

            if (underlyingType == typeof(ushort))
            {
                parsed = TryParseUInt32Enum(rt, value, ushort.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt16, out uintResult);
                ushort ushortResult = (ushort)uintResult;
                result = Unsafe.As<ushort, TEnum>(ref ushortResult);
                return parsed;
            }

            parsed = TryParseRareEnum(rt, value, ignoreCase, throwOnFailure, out object? objectResult);
            result = parsed ? (TEnum)objectResult! : default;
            return parsed;
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
            ReadOnlySpan<char> originalValue = value;

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
                if (endIndex < 0)
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
                throw new ArgumentException(SR.Format(SR.Arg_EnumValueNotFound, originalValue.ToString()));
            }

            result = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool StartsNumber(char c) => char.IsAsciiDigit(c) || c == '-' || c == '+';

        public static object ToObject(Type enumType, object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            // Delegate rest of error checking to the other functions
            return Convert.GetTypeCode(value) switch
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

        public static string Format(Type enumType, object value, [StringSyntax(StringSyntaxAttribute.EnumFormat)] string format)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(format);

            RuntimeType rtType = ValidateRuntimeType(enumType);

            // If the value is an Enum then we need to extract the underlying value from it
            Type valueType = value.GetType();
            if (valueType.IsEnum)
            {
                if (!valueType.IsEquivalentTo(enumType))
                    throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, valueType, enumType));

                if (format.Length != 1)
                {
                    // all acceptable format string are of length 1
                    throw CreateInvalidFormatSpecifierException();
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
                switch (format[0] | 0x20)
                {
                    case 'g':
                        return FormatSingleNameOrFlagNames(rtType, ToUInt64(value)) ?? value.ToString()!;

                    case 'd':
                        return value.ToString()!;

                    case 'x':
                        return AsNumberToHexString(value);

                    case 'f':
                        return FormatFlagNames(GetEnumInfo(rtType), ToUInt64(value)) ?? value.ToString()!;
                }
            }

            throw CreateInvalidFormatSpecifierException();
        }

        /// <summary>Tries to format the value of the enumerated type instance into the provided span of characters.</summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <param name="destination">The span into which to write the instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the character that represents the standard format string that defines the acceptable format of destination. This may be empty, or "g", "d", "f", or "x".</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/> if the destination span wasn't large enough to contain the formatted value.</returns>
        /// <exception cref="FormatException">The format parameter contains an invalid value.</exception>
        public static bool TryFormat<TEnum>(TEnum value, Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.EnumFormat)] ReadOnlySpan<char> format = default) where TEnum : struct, Enum =>
            TryFormatUnconstrained(value, destination, out charsWritten, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // format is most frequently a constant, and we want it exposed to the implementation; this should be inlined automatically, anyway
        internal static bool TryFormatUnconstrained<TEnum>(TEnum value, Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.EnumFormat)] ReadOnlySpan<char> format = default)
        {
            Debug.Assert(typeof(TEnum).IsEnum);
            Debug.Assert(value is not null);

            return format.IsEmpty ?
                TryFormatDefault(value, destination, out charsWritten) :
                TryFormatNonDefault(value, destination, out charsWritten, format);

            static bool TryFormatDefault(TEnum value, Span<char> destination, out int charsWritten)
            {
                Debug.Assert(typeof(TEnum).IsEnum);
                Debug.Assert(value is not null);

                ulong ulongValue = ToUInt64(value);

                if (!GenericEnumInfo<TEnum>.HasFlagsAttribute)
                {
                    if (GetEnumName<TEnum>(ulongValue) is string enumName)
                    {
                        if (enumName.TryCopyTo(destination))
                        {
                            charsWritten = enumName.Length;
                            return true;
                        }

                        charsWritten = 0;
                        return false;
                    }
                }
                else
                {
                    bool destinationIsTooSmall = false;
                    if (TryFormatFlagNames(GenericEnumInfo<TEnum>.EnumInfo, ulongValue, destination, out charsWritten, ref destinationIsTooSmall) || destinationIsTooSmall)
                    {
                        return !destinationIsTooSmall;
                    }
                }

                return TryAsNumberToString(value, destination, out charsWritten);
            }

            static bool TryFormatNonDefault(TEnum value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format)
            {
                Debug.Assert(typeof(TEnum).IsEnum);
                Debug.Assert(value is not null);

                if (format.Length == 1)
                {
                    switch (format[0] | 0x20)
                    {
                        case 'g':
                            return TryFormatDefault(value, destination, out charsWritten);

                        case 'd':
                            return TryAsNumberToString(value, destination, out charsWritten);

                        case 'x':
                            return TryAsNumberToHexString(value, destination, out charsWritten);

                        case 'f':
                            bool destinationIsTooSmall = false;
                            if (TryFormatFlagNames(GenericEnumInfo<TEnum>.EnumInfo, ToUInt64(value), destination, out charsWritten, ref destinationIsTooSmall) ||
                                destinationIsTooSmall)
                            {
                                return !destinationIsTooSmall;
                            }
                            goto case 'd';
                    }
                }

                throw CreateInvalidFormatSpecifierException();
            }
        }
        #endregion

        #region Private Methods
        internal object GetValue()
        {
            ref byte data = ref this.GetRawData();
            return InternalGetCorElementType() switch
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_I:
#endif
                    return (ulong)Unsafe.As<byte, int>(ref data);

                case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R4:
                    return Unsafe.As<byte, uint>(ref data);

                case CorElementType.ELEMENT_TYPE_I8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_I:
#endif
                    return (ulong)Unsafe.As<byte, long>(ref data);

                case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R8:
                    return Unsafe.As<byte, ulong>(ref data);

                default:
                    Debug.Fail("Unknown enum underlying type");
                    return 0;
            }
        }

        #endregion

        #region Object Overrides

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null)
                return false;

            if (this == obj)
                return true;

            if (this.GetType() != obj.GetType())
                return false;

            ref byte pThisValue = ref this.GetRawData();
            ref byte pOtherValue = ref obj.GetRawData();

            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                case CorElementType.ELEMENT_TYPE_U1:
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return pThisValue == pOtherValue;

                case CorElementType.ELEMENT_TYPE_I2:
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return Unsafe.As<byte, ushort>(ref pThisValue) == Unsafe.As<byte, ushort>(ref pOtherValue);

                case CorElementType.ELEMENT_TYPE_I4:
                case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R4:
                    return Unsafe.As<byte, uint>(ref pThisValue) == Unsafe.As<byte, uint>(ref pOtherValue);

                case CorElementType.ELEMENT_TYPE_I8:
                case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R8:
                    return Unsafe.As<byte, ulong>(ref pThisValue) == Unsafe.As<byte, ulong>(ref pOtherValue);

                default:
                    Debug.Fail("Unknown enum underlying type");
                    return false;
            }
        }

        public override int GetHashCode()
        {
            // CONTRACT with the runtime: GetHashCode of enum types is implemented as GetHashCode of the underlying type.
            // The runtime can bypass calls to Enum::GetHashCode and call the underlying type's GetHashCode directly
            // to avoid boxing the enum.
            ref byte data = ref this.GetRawData();
            return InternalGetCorElementType() switch
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
            // Try to see if its one of the enum values, then we return a String back else the value
            return
                FormatSingleNameOrFlagNames((RuntimeType)GetType(), ToUInt64()) ??
                AsNumberToString();
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
                    Debug.Fail("Unknown enum underlying type");
                    return 0;
            }
        }
        #endregion

        #region IFormattable
        [Obsolete("The provider argument is not used. Use ToString(String) instead.")]
        public string ToString([StringSyntax(StringSyntaxAttribute.EnumFormat)] string? format, IFormatProvider? provider)
        {
            return ToString(format);
        }
        #endregion

        #region Public Methods
        public string ToString([StringSyntax(StringSyntaxAttribute.EnumFormat)] string? format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return ToString();
            }

            if (format.Length == 1)
            {
                switch (format[0] | 0x20)
                {
                    case 'g':
                        return ToString();

                    case 'd':
                        return AsNumberToString();

                    case 'x':
                        return AsNumberToHexString();

                    case 'f':
                        return FormatFlagNames(GetEnumInfo((RuntimeType)GetType()), ToUInt64()) ?? AsNumberToString();
                }
            }

            throw CreateInvalidFormatSpecifierException();
        }

        [Obsolete("The provider argument is not used. Use ToString() instead.")]
        public string ToString(IFormatProvider? provider)
        {
            return ToString();
        }

        #endregion

        #region IConvertible
        public TypeCode GetTypeCode() =>
            InternalGetCorElementType() switch
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
            ArgumentNullException.ThrowIfNull(enumType);

            if (enumType is not RuntimeType rtType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            if (!rtType.IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));

#if NATIVEAOT
            // Check for the unfortunate "typeof(Outer<>.InnerEnum)" corner case.
            // https://github.com/dotnet/runtime/issues/7976
            if (enumType.ContainsGenericParameters)
                throw new InvalidOperationException(SR.Format(SR.Arg_OpenType, enumType.ToString()));
#endif

            return rtType;
        }

        private static void ThrowInvalidEmptyParseArgument() =>
            throw new ArgumentException(SR.Arg_MustContainEnumInfo, "value");

        [MethodImpl(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/78300
        private static Exception CreateInvalidFormatSpecifierException() =>
            new FormatException(SR.Format_InvalidEnumFormatSpecification);

        private static class GenericEnumInfo<TEnum>
        {
            public static readonly EnumInfo EnumInfo = GetEnumInfo((RuntimeType)typeof(TEnum));
            public static readonly bool HasFlagsAttribute = EnumInfo.HasFlagsAttribute;
            public static readonly string[] Names = EnumInfo.Names;
            public static readonly ulong[] Values = EnumInfo.Values;
            public static readonly bool ValuesAreSequentialFromZero = EnumInfo.ValuesAreSequentialFromZero;
        }
    }
}
