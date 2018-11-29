// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics;

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
    public abstract class Enum : ValueType, IComparable, IFormattable, IConvertible
    {
        #region Private Constants
        private const char enumSeparatorChar = ',';
        #endregion

        #region Private Static Methods
        private static TypeValuesAndNames GetCachedValuesAndNames(RuntimeType enumType, bool getNames)
        {
            TypeValuesAndNames entry = enumType.GenericCache as TypeValuesAndNames;

            if (entry == null || (getNames && entry.Names == null))
            {
                ulong[] values = null;
                string[] names = null;
                bool isFlags = enumType.IsDefined(typeof(System.FlagsAttribute), false);

                GetEnumValuesAndNames(
                    enumType.GetTypeHandleInternal(),
                    JitHelpers.GetObjectHandleOnStack(ref values),
                    JitHelpers.GetObjectHandleOnStack(ref names),
                    getNames);

                entry = new TypeValuesAndNames(isFlags, values, names);
                enumType.GenericCache = entry;
            }

            return entry;
        }

        private unsafe string InternalFormattedHexString()
        {
            fixed (void* pValue = &JitHelpers.GetPinningHelper(this).m_data)
            {
                switch (InternalGetCorElementType())
                {
                    case CorElementType.I1:
                    case CorElementType.U1:
                        return (*(byte*)pValue).ToString("X2", null);
                    case CorElementType.Boolean:
                        // direct cast from bool to byte is not allowed
                        return Convert.ToByte(*(bool*)pValue).ToString("X2", null);
                    case CorElementType.I2:
                    case CorElementType.U2:
                    case CorElementType.Char:
                        return (*(ushort*)pValue).ToString("X4", null);
                    case CorElementType.I4:
                    case CorElementType.U4:
                        return (*(uint*)pValue).ToString("X8", null);
                    case CorElementType.I8:
                    case CorElementType.U8:
                        return (*(ulong*)pValue).ToString("X16", null);
                    default:
                        throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
                }
            }
        }

        private static string InternalFormattedHexString(object value)
        {
            TypeCode typeCode = Convert.GetTypeCode(value);

            switch (typeCode)
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

        internal static string GetEnumName(RuntimeType eT, ulong ulValue)
        {
            Debug.Assert(eT != null);
            ulong[] ulValues = Enum.InternalGetValues(eT);
            int index = Array.BinarySearch(ulValues, ulValue);

            if (index >= 0)
            {
                string[] names = Enum.InternalGetNames(eT);
                return names[index];
            }

            return null; // return null so the caller knows to .ToString() the input
        }

        private static string InternalFormat(RuntimeType eT, ulong value)
        {
            Debug.Assert(eT != null);

            // These values are sorted by value. Don't change this
            TypeValuesAndNames entry = GetCachedValuesAndNames(eT, true);

            if (!entry.IsFlag) // Not marked with Flags attribute
            {
                return Enum.GetEnumName(eT, value);
            }
            else // These are flags OR'ed together (We treat everything as unsigned types)
            {
                return InternalFlagsFormat(eT, entry, value);
            }
        }

        private static string InternalFlagsFormat(RuntimeType eT, ulong result)
        {
            // These values are sorted by value. Don't change this
            TypeValuesAndNames entry = GetCachedValuesAndNames(eT, true);

            return InternalFlagsFormat(eT, entry, result);
        }

        private static string InternalFlagsFormat(RuntimeType eT, TypeValuesAndNames entry, ulong resultValue)
        {
            Debug.Assert(eT != null);

            string[] names = entry.Names;
            ulong[] values = entry.Values;
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

            Span<char> resultSpan = MemoryMarshal.CreateSpan(ref result.GetRawStringData(), result.Length);
            string name = names[foundItems[--foundItemsCount]];
            name.AsSpan().CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(name.Length);
            while (--foundItemsCount >= 0)
            {
                resultSpan[0] = ',';
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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int InternalCompareTo(object o1, object o2);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType InternalGetUnderlyingType(RuntimeType enumType);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetEnumValuesAndNames(RuntimeTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, bool getNames);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object InternalBoxEnum(RuntimeType enumType, long value);
        #endregion

        #region Public Static Methods
        private enum ParseFailureKind
        {
            None = 0,
            Argument = 1,
            ArgumentNull = 2,
            ArgumentWithParameter = 3,
            UnhandledException = 4
        }

        // This will store the result of the parsing.
        private struct EnumResult
        {
            internal object parsedEnum;
            internal bool canThrow;
            internal ParseFailureKind m_failure;
            internal string m_failureMessageID;
            internal string m_failureParameter;
            internal object m_failureMessageFormatArgument;
            internal Exception m_innerException;

            internal void SetFailure(Exception unhandledException)
            {
                m_failure = ParseFailureKind.UnhandledException;
                m_innerException = unhandledException;
            }
            internal void SetFailure(ParseFailureKind failure, string failureParameter)
            {
                m_failure = failure;
                m_failureParameter = failureParameter;
                if (canThrow)
                    throw GetEnumParseException();
            }
            internal void SetFailure(ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument)
            {
                m_failure = failure;
                m_failureMessageID = failureMessageID;
                m_failureMessageFormatArgument = failureMessageFormatArgument;
                if (canThrow)
                    throw GetEnumParseException();
            }
            internal Exception GetEnumParseException()
            {
                switch (m_failure)
                {
                    case ParseFailureKind.Argument:
                        return new ArgumentException(SR.GetResourceString(m_failureMessageID));

                    case ParseFailureKind.ArgumentNull:
                        return new ArgumentNullException(m_failureParameter);

                    case ParseFailureKind.ArgumentWithParameter:
                        return new ArgumentException(SR.Format(SR.GetResourceString(m_failureMessageID), m_failureMessageFormatArgument));

                    case ParseFailureKind.UnhandledException:
                        return m_innerException;

                    default:
                        Debug.Fail("Unknown EnumParseFailure: " + m_failure);
                        return new ArgumentException(SR.Arg_EnumValueNotFound);
                }
            }
        }

        public static bool TryParse(Type enumType, string value, out object result)
        {
            return TryParse(enumType, value, false, out result);
        }

        public static bool TryParse(Type enumType, string value, bool ignoreCase, out object result)
        {
            result = null;
            EnumResult parseResult = new EnumResult();
            bool retValue;

            if (retValue = TryParseEnum(enumType, value, ignoreCase, ref parseResult))
                result = parseResult.parsedEnum;
            return retValue;
        }

        public static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct
        {
            return TryParse(value, false, out result);
        }

        public static bool TryParse<TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct
        {
            result = default;
            EnumResult parseResult = new EnumResult();
            bool retValue;

            if (retValue = TryParseEnum(typeof(TEnum), value, ignoreCase, ref parseResult))
                result = (TEnum)parseResult.parsedEnum;
            return retValue;
        }

        public static object Parse(Type enumType, string value)
        {
            return Parse(enumType, value, false);
        }

        public static object Parse(Type enumType, string value, bool ignoreCase)
        {
            EnumResult parseResult = new EnumResult() { canThrow = true };
            if (TryParseEnum(enumType, value, ignoreCase, ref parseResult))
                return parseResult.parsedEnum;
            else
                throw parseResult.GetEnumParseException();
        }

        public static TEnum Parse<TEnum>(string value) where TEnum : struct
        {
            return Parse<TEnum>(value, false);
        }

        public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct
        {
            EnumResult parseResult = new EnumResult() { canThrow = true };
            if (TryParseEnum(typeof(TEnum), value, ignoreCase, ref parseResult))
                return (TEnum)parseResult.parsedEnum;
            else
                throw parseResult.GetEnumParseException();
        }

        private static bool TryParseEnum(Type enumType, string value, bool ignoreCase, ref EnumResult parseResult)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));

            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));

            if (value == null)
            {
                parseResult.SetFailure(ParseFailureKind.ArgumentNull, nameof(value));
                return false;
            }

            int firstNonWhitespaceIndex = -1;
            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                {
                    firstNonWhitespaceIndex = i;
                    break;
                }
            }
            if (firstNonWhitespaceIndex == -1)
            {
                parseResult.SetFailure(ParseFailureKind.Argument, nameof(SR.Arg_MustContainEnumInfo), null);
                return false;
            }

            // We have 2 code paths here. One if they are values else if they are Strings.
            // values will have the first character as as number or a sign.
            ulong result = 0;

            char firstNonWhitespaceChar = value[firstNonWhitespaceIndex];
            if (char.IsDigit(firstNonWhitespaceChar) || firstNonWhitespaceChar == '-' || firstNonWhitespaceChar == '+')
            {
                Type underlyingType = GetUnderlyingType(enumType);
                object temp;

                try
                {
                    value = value.Trim();
                    temp = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
                    parseResult.parsedEnum = ToObject(enumType, temp);
                    return true;
                }
                catch (FormatException)
                { // We need to Parse this as a String instead. There are cases
                  // when you tlbimp enums that can have values of the form "3D".
                  // Don't fix this code.
                }
                catch (Exception ex)
                {
                    if (parseResult.canThrow)
                        throw;
                    else
                    {
                        parseResult.SetFailure(ex);
                        return false;
                    }
                }
            }

            // Find the field. Let's assume that these are always static classes 
            // because the class is an enum.
            TypeValuesAndNames entry = GetCachedValuesAndNames(rtType, true);
            string[] enumNames = entry.Names;
            ulong[] enumValues = entry.Values;

            StringComparison comparison = ignoreCase ?
                StringComparison.OrdinalIgnoreCase :
                StringComparison.Ordinal;

            int valueIndex = firstNonWhitespaceIndex;
            while (valueIndex <= value.Length) // '=' is to handle invalid case of an ending comma
            {
                // Find the next separator, if there is one, otherwise the end of the string.
                int endIndex = value.IndexOf(enumSeparatorChar, valueIndex);
                if (endIndex == -1)
                {
                    endIndex = value.Length;
                }

                // Shift the starting and ending indices to eliminate whitespace
                int endIndexNoWhitespace = endIndex;
                while (valueIndex < endIndex && char.IsWhiteSpace(value[valueIndex])) valueIndex++;
                while (endIndexNoWhitespace > valueIndex && char.IsWhiteSpace(value[endIndexNoWhitespace - 1])) endIndexNoWhitespace--;
                int valueSubstringLength = endIndexNoWhitespace - valueIndex;

                // Try to match this substring against each enum name
                bool success = false;
                for (int i = 0; i < enumNames.Length; i++)
                {
                    if (enumNames[i].Length == valueSubstringLength &&
                        string.Compare(enumNames[i], 0, value, valueIndex, valueSubstringLength, comparison) == 0)
                    {
                        result |= enumValues[i];
                        success = true;
                        break;
                    }
                }

                // If we couldn't find a match, throw an argument exception.
                if (!success)
                {
                    // Not found, throw an argument exception.
                    parseResult.SetFailure(ParseFailureKind.ArgumentWithParameter, nameof(SR.Arg_EnumValueNotFound), value);
                    return false;
                }

                // Move our pointer to the ending index to go again.
                valueIndex = endIndex + 1;
            }

            try
            {
                parseResult.parsedEnum = ToObject(enumType, result);
                return true;
            }
            catch (Exception ex)
            {
                if (parseResult.canThrow)
                    throw;
                else
                {
                    parseResult.SetFailure(ex);
                    return false;
                }
            }
        }

        public static Type GetUnderlyingType(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumUnderlyingType();
        }

        public static Array GetValues(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumValues();
        }

        internal static ulong[] InternalGetValues(RuntimeType enumType)
        {
            // Get all of the values
            return GetCachedValuesAndNames(enumType, false).Values;
        }

        public static string GetName(Type enumType, object value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumName(value);
        }

        public static string[] GetNames(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumNames();
        }

        internal static string[] InternalGetNames(RuntimeType enumType)
        {
            // Get all of the names
            return GetCachedValuesAndNames(enumType, true).Names;
        }

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

        public static bool IsDefined(Type enumType, object value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.IsEnumDefined(value);
        }

        public static string Format(Type enumType, object value, string format)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (format == null)
                throw new ArgumentNullException(nameof(format));

            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));

            // Check if both of them are of the same type
            Type valueType = value.GetType();

            Type underlyingType = GetUnderlyingType(enumType);

            // If the value is an Enum then we need to extract the underlying value from it
            if (valueType.IsEnum)
            {
                if (!valueType.IsEquivalentTo(enumType))
                    throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, valueType.ToString(), enumType.ToString()));

                if (format.Length != 1)
                {
                    // all acceptable format string are of length 1
                    throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
                }
                return ((Enum)value).ToString(format);
            }
            // The value must be of the same type as the Underlying type of the Enum
            else if (valueType != underlyingType)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumFormatUnderlyingTypeAndObjectMustBeSameType, valueType.ToString(), underlyingType.ToString()));
            }
            if (format.Length != 1)
            {
                // all acceptable format string are of length 1
                throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
            }

            char formatCh = format[0];
            if (formatCh == 'G' || formatCh == 'g')
                return GetEnumName(rtType, ToUInt64(value)) ?? value.ToString();

            if (formatCh == 'D' || formatCh == 'd')
                return value.ToString();

            if (formatCh == 'X' || formatCh == 'x')
                return InternalFormattedHexString(value);

            if (formatCh == 'F' || formatCh == 'f')
                return Enum.InternalFlagsFormat(rtType, ToUInt64(value)) ?? value.ToString();

            throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
        }

        #endregion

        #region Definitions
        private class TypeValuesAndNames
        {
            // Each entry contains a list of sorted pair of enum field names and values, sorted by values
            public TypeValuesAndNames(bool isFlag, ulong[] values, string[] names)
            {
                this.IsFlag = isFlag;
                this.Values = values;
                this.Names = names;
            }

            public bool IsFlag;
            public ulong[] Values;
            public string[] Names;
        }
        #endregion

        #region Private Methods
        internal unsafe object GetValue()
        {
            fixed (void* pValue = &JitHelpers.GetPinningHelper(this).m_data)
            {
                switch (InternalGetCorElementType())
                {
                    case CorElementType.I1:
                        return *(sbyte*)pValue;
                    case CorElementType.U1:
                        return *(byte*)pValue;
                    case CorElementType.Boolean:
                        return *(bool*)pValue;
                    case CorElementType.I2:
                        return *(short*)pValue;
                    case CorElementType.U2:
                        return *(ushort*)pValue;
                    case CorElementType.Char:
                        return *(char*)pValue;
                    case CorElementType.I4:
                        return *(int*)pValue;
                    case CorElementType.U4:
                        return *(uint*)pValue;
                    case CorElementType.R4:
                        return *(float*)pValue;
                    case CorElementType.I8:
                        return *(long*)pValue;
                    case CorElementType.U8:
                        return *(ulong*)pValue;
                    case CorElementType.R8:
                        return *(double*)pValue;
                    case CorElementType.I:
                        return *(IntPtr*)pValue;
                    case CorElementType.U:
                        return *(UIntPtr*)pValue;
                    default:
                        Debug.Fail("Invalid primitive type");
                        return null;
                }
            }
        }

        private unsafe ulong ToUInt64()
        {
            fixed (void* pValue = &JitHelpers.GetPinningHelper(this).m_data)
            {
                switch (InternalGetCorElementType())
                {
                    case CorElementType.I1:
                        return (ulong)*(sbyte*)pValue;
                    case CorElementType.U1:
                        return *(byte*)pValue;
                    case CorElementType.Boolean:
                        return Convert.ToUInt64(*(bool*)pValue, CultureInfo.InvariantCulture);
                    case CorElementType.I2:
                        return (ulong)*(short*)pValue;
                    case CorElementType.U2:
                    case CorElementType.Char:
                        return *(ushort*)pValue;
                    case CorElementType.I4:
                        return (ulong)*(int*)pValue;
                    case CorElementType.U4:
                    case CorElementType.R4:
                        return *(uint*)pValue;
                    case CorElementType.I8:
                        return (ulong)*(long*)pValue;
                    case CorElementType.U8:
                    case CorElementType.R8:
                        return *(ulong*)pValue;
                    case CorElementType.I:
                        if (IntPtr.Size == 8)
                        {
                            return *(ulong*)pValue;
                        }
                        else
                        {
                            return (ulong)*(int*)pValue;
                        }
                    case CorElementType.U:
                        if (IntPtr.Size == 8)
                        {
                            return *(ulong*)pValue;
                        }
                        else
                        {
                            return *(uint*)pValue;
                        }
                    default:
                        Debug.Fail("Invalid primitive type");
                        return 0;
                }
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern bool InternalHasFlag(Enum flags);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern CorElementType InternalGetCorElementType();

        #endregion

        #region Object Overrides
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern override bool Equals(object obj);

        public override unsafe int GetHashCode()
        {
            // CONTRACT with the runtime: GetHashCode of enum types is implemented as GetHashCode of the underlying type.
            // The runtime can bypass calls to Enum::GetHashCode and call the underlying type's GetHashCode directly
            // to avoid boxing the enum.

            fixed (void* pValue = &JitHelpers.GetPinningHelper(this).m_data)
            {
                switch (InternalGetCorElementType())
                {
                    case CorElementType.I1:
                        return (*(sbyte*)pValue).GetHashCode();
                    case CorElementType.U1:
                        return (*(byte*)pValue).GetHashCode();
                    case CorElementType.Boolean:
                        return (*(bool*)pValue).GetHashCode();
                    case CorElementType.I2:
                        return (*(short*)pValue).GetHashCode();
                    case CorElementType.U2:
                        return (*(ushort*)pValue).GetHashCode();
                    case CorElementType.Char:
                        return (*(char*)pValue).GetHashCode();
                    case CorElementType.I4:
                        return (*(int*)pValue).GetHashCode();
                    case CorElementType.U4:
                        return (*(uint*)pValue).GetHashCode();
                    case CorElementType.R4:
                        return (*(float*)pValue).GetHashCode();
                    case CorElementType.I8:
                        return (*(long*)pValue).GetHashCode();
                    case CorElementType.U8:
                        return (*(ulong*)pValue).GetHashCode();
                    case CorElementType.R8:
                        return (*(double*)pValue).GetHashCode();
                    case CorElementType.I:
                        return (*(IntPtr*)pValue).GetHashCode();
                    case CorElementType.U:
                        return (*(UIntPtr*)pValue).GetHashCode();
                    default:
                        Debug.Fail("Invalid primitive type");
                        return 0;
                }
            }
        }

        public override string ToString()
        {
            // Returns the value in a human readable format.  For PASCAL style enums who's value maps directly the name of the field is returned.
            // For PASCAL style enums who's values do not map directly the decimal value of the field is returned.
            // For BitFlags (indicated by the Flags custom attribute): If for each bit that is set in the value there is a corresponding constant
            //(a pure power of 2), then the  OR string (ie "Red | Yellow") is returned. Otherwise, if the value is zero or if you can't create a string that consists of
            // pure powers of 2 OR-ed together, you return a hex value


            // Try to see if its one of the enum values, then we return a String back else the value
            return Enum.InternalFormat((RuntimeType)GetType(), ToUInt64()) ?? GetValue().ToString();
        }
        #endregion

        #region IFormattable
        [Obsolete("The provider argument is not used. Please use ToString(String).")]
        public string ToString(string format, IFormatProvider provider)
        {
            return ToString(format);
        }
        #endregion

        #region IComparable
        public int CompareTo(object target)
        {
            const int retIncompatibleMethodTables = 2;  // indicates that the method tables did not match
            const int retInvalidEnumType = 3; // indicates that the enum was of an unknown/unsupported underlying type

            if (this == null)
                throw new NullReferenceException();

            int ret = InternalCompareTo(this, target);

            if (ret < retIncompatibleMethodTables)
            {
                // -1, 0 and 1 are the normal return codes
                return ret;
            }
            else if (ret == retIncompatibleMethodTables)
            {
                Type thisType = this.GetType();
                Type targetType = target.GetType();

                throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, targetType.ToString(), thisType.ToString()));
            }
            else
            {
                // assert valid return code (3)
                Debug.Assert(ret == retInvalidEnumType, "Enum.InternalCompareTo return code was invalid");

                throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }
        #endregion

        #region Public Methods
        public string ToString(string format)
        {
            char formatCh;
            if (format == null || format.Length == 0)
                formatCh = 'G';
            else if (format.Length != 1)
                throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
            else
                formatCh = format[0];

            if (formatCh == 'G' || formatCh == 'g')
                return ToString();

            if (formatCh == 'D' || formatCh == 'd')
                return GetValue().ToString();

            if (formatCh == 'X' || formatCh == 'x')
                return InternalFormattedHexString();

            if (formatCh == 'F' || formatCh == 'f')
                return InternalFlagsFormat((RuntimeType)GetType(), ToUInt64()) ?? GetValue().ToString();

            throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
        }

        [Obsolete("The provider argument is not used. Please use ToString().")]
        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        [Intrinsic]
        public bool HasFlag(Enum flag)
        {
            if (flag == null)
                throw new ArgumentNullException(nameof(flag));

            if (!this.GetType().IsEquivalentTo(flag.GetType()))
            {
                throw new ArgumentException(SR.Format(SR.Argument_EnumTypeDoesNotMatch, flag.GetType(), this.GetType()));
            }

            return InternalHasFlag(flag);
        }

        #endregion

        #region IConvertable
        public TypeCode GetTypeCode()
        {
            switch (InternalGetCorElementType())
            {
                case CorElementType.I1:
                    return TypeCode.SByte;
                case CorElementType.U1:
                    return TypeCode.Byte;
                case CorElementType.Boolean:
                    return TypeCode.Boolean;
                case CorElementType.I2:
                    return TypeCode.Int16;
                case CorElementType.U2:
                    return TypeCode.UInt16;
                case CorElementType.Char:
                    return TypeCode.Char;
                case CorElementType.I4:
                    return TypeCode.Int32;
                case CorElementType.U4:
                    return TypeCode.UInt32;
                case CorElementType.I8:
                    return TypeCode.Int64;
                case CorElementType.U8:
                    return TypeCode.UInt64;
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(GetValue(), CultureInfo.CurrentCulture);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(GetValue(), CultureInfo.CurrentCulture);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(GetValue(), CultureInfo.CurrentCulture);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(GetValue(), CultureInfo.CurrentCulture);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(GetValue(), CultureInfo.CurrentCulture);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(GetValue(), CultureInfo.CurrentCulture);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(GetValue(), CultureInfo.CurrentCulture);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(GetValue(), CultureInfo.CurrentCulture);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(GetValue(), CultureInfo.CurrentCulture);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(GetValue(), CultureInfo.CurrentCulture);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(GetValue(), CultureInfo.CurrentCulture);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(GetValue(), CultureInfo.CurrentCulture);
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(GetValue(), CultureInfo.CurrentCulture);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Enum", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
        #endregion

        #region ToObject
        [CLSCompliant(false)]
        public static object ToObject(Type enumType, sbyte value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        public static object ToObject(Type enumType, short value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        public static object ToObject(Type enumType, int value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        public static object ToObject(Type enumType, byte value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        [CLSCompliant(false)]
        public static object ToObject(Type enumType, ushort value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        [CLSCompliant(false)]
        public static object ToObject(Type enumType, uint value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        public static object ToObject(Type enumType, long value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        [CLSCompliant(false)]
        public static object ToObject(Type enumType, ulong value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, unchecked((long)value));
        }

        private static object ToObject(Type enumType, char value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        private static object ToObject(Type enumType, bool value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value ? 1 : 0);
        }
        #endregion
    }
}
