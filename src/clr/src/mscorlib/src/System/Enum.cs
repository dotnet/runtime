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
using System.Diagnostics.Contracts;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract class Enum : ValueType, IComparable, IFormattable, IConvertible
    {
        #region Private Constants
        private const char enumSeparatorChar = ',';
        private const String enumSeparatorString = ", ";
        #endregion

        #region Private Static Methods
        private static TypeValuesAndNames GetCachedValuesAndNames(RuntimeType enumType, bool getNames)
        {
            TypeValuesAndNames entry = enumType.GenericCache as TypeValuesAndNames;

            if (entry == null || (getNames && entry.Names == null))
            {
                ulong[] values = null;
                String[] names = null;
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

        private unsafe String InternalFormattedHexString()
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

        private static String InternalFormattedHexString(object value)
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
                    return ((UInt16)(Int16)value).ToString("X4", null);
                case TypeCode.UInt16:
                    return ((UInt16)value).ToString("X4", null);
                case TypeCode.Char:
                    return ((UInt16)(Char)value).ToString("X4", null);
                case TypeCode.UInt32:
                    return ((UInt32)value).ToString("X8", null);
                case TypeCode.Int32:
                    return ((UInt32)(Int32)value).ToString("X8", null);
                case TypeCode.UInt64:
                    return ((UInt64)value).ToString("X16", null);
                case TypeCode.Int64:
                    return ((UInt64)(Int64)value).ToString("X16", null);
                // All unsigned types will be directly cast
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }

        internal static String GetEnumName(RuntimeType eT, ulong ulValue)
        {
            Contract.Requires(eT != null);
            ulong[] ulValues = Enum.InternalGetValues(eT);
            int index = Array.BinarySearch(ulValues, ulValue);

            if (index >= 0)
            {
                string[] names = Enum.InternalGetNames(eT);
                return names[index];
            }

            return null; // return null so the caller knows to .ToString() the input
        }

        private static String InternalFormat(RuntimeType eT, ulong value)
        {
            Contract.Requires(eT != null);

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

        private static String InternalFlagsFormat(RuntimeType eT, ulong result)
        {
            // These values are sorted by value. Don't change this
            TypeValuesAndNames entry = GetCachedValuesAndNames(eT, true);

            return InternalFlagsFormat(eT, entry, result);
        }

        private static String InternalFlagsFormat(RuntimeType eT, TypeValuesAndNames entry, ulong result)
        {
            Contract.Requires(eT != null);

            String[] names = entry.Names;
            ulong[] values = entry.Values;
            Debug.Assert(names.Length == values.Length);

            int index = values.Length - 1;
            StringBuilder sb = StringBuilderCache.Acquire();
            bool firstTime = true;
            ulong saveResult = result;

            // We will not optimize this code further to keep it maintainable. There are some boundary checks that can be applied
            // to minimize the comparsions required. This code works the same for the best/worst case. In general the number of
            // items in an enum are sufficiently small and not worth the optimization.
            while (index >= 0)
            {
                if ((index == 0) && (values[index] == 0))
                    break;

                if ((result & values[index]) == values[index])
                {
                    result -= values[index];
                    if (!firstTime)
                        sb.Insert(0, enumSeparatorString);

                    sb.Insert(0, names[index]);
                    firstTime = false;
                }

                index--;
            }

            string returnString;
            if (result != 0)
            {
                // We were unable to represent this number as a bitwise or of valid flags
                returnString = null; // return null so the caller knows to .ToString() the input
            }
            else if (saveResult == 0)
            {
                // For the cases when we have zero
                if (values.Length > 0 && values[0] == 0)
                {
                    returnString = names[0]; // Zero was one of the enum values.
                }
                else
                {
                    returnString = "0";
                }
            }
            else
            {
                returnString = sb.ToString(); // Return the string representation
            }

            StringBuilderCache.Release(sb);
            return returnString;
        }

        internal static ulong ToUInt64(Object value)
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
                    result = (ulong)(Int16)value;
                    break;
                case TypeCode.UInt16:
                    result = (UInt16)value;
                    break;
                case TypeCode.Char:
                    result = (UInt16)(Char)value;
                    break;
                case TypeCode.UInt32:
                    result = (UInt32)value;
                    break;
                case TypeCode.Int32:
                    result = (ulong)(int)value;
                    break;
                case TypeCode.UInt64:
                    result = (ulong)value;
                    break;
                case TypeCode.Int64:
                    result = (ulong)(Int64)value;
                    break;
                // All unsigned types will be directly cast
                default:
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }

            return result;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int InternalCompareTo(Object o1, Object o2);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType InternalGetUnderlyingType(RuntimeType enumType);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        private static extern void GetEnumValuesAndNames(RuntimeTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, bool getNames);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Object InternalBoxEnum(RuntimeType enumType, long value);
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
                        Debug.Assert(false, "Unknown EnumParseFailure: " + m_failure);
                        return new ArgumentException(SR.Arg_EnumValueNotFound);
                }
            }
        }

        public static bool TryParse(Type enumType, String value, out Object result)
        {
            return TryParse(enumType, value, false, out result);
        }

        public static bool TryParse(Type enumType, String value, bool ignoreCase, out Object result)
        {
            result = null;
            EnumResult parseResult = new EnumResult();
            bool retValue;

            if (retValue = TryParseEnum(enumType, value, ignoreCase, ref parseResult))
                result = parseResult.parsedEnum;
            return retValue;
        }

        public static bool TryParse<TEnum>(String value, out TEnum result) where TEnum : struct
        {
            return TryParse(value, false, out result);
        }

        public static bool TryParse<TEnum>(String value, bool ignoreCase, out TEnum result) where TEnum : struct
        {
            result = default(TEnum);
            EnumResult parseResult = new EnumResult();
            bool retValue;

            if (retValue = TryParseEnum(typeof(TEnum), value, ignoreCase, ref parseResult))
                result = (TEnum)parseResult.parsedEnum;
            return retValue;
        }

        public static Object Parse(Type enumType, String value)
        {
            return Parse(enumType, value, false);
        }

        public static Object Parse(Type enumType, String value, bool ignoreCase)
        {
            EnumResult parseResult = new EnumResult() { canThrow = true };
            if (TryParseEnum(enumType, value, ignoreCase, ref parseResult))
                return parseResult.parsedEnum;
            else
                throw parseResult.GetEnumParseException();
        }

        public static TEnum Parse<TEnum>(String value) where TEnum : struct
        {
            return Parse<TEnum>(value, false);
        }

        public static TEnum Parse<TEnum>(String value, bool ignoreCase) where TEnum : struct
        {
            EnumResult parseResult = new EnumResult() { canThrow = true };
            if (TryParseEnum(typeof(TEnum), value, ignoreCase, ref parseResult))
                return (TEnum)parseResult.parsedEnum;
            else
                throw parseResult.GetEnumParseException();
        }

        private static bool TryParseEnum(Type enumType, String value, bool ignoreCase, ref EnumResult parseResult)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            Contract.EndContractBlock();

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
                if (!Char.IsWhiteSpace(value[i]))
                {
                    firstNonWhitespaceIndex = i;
                    break;
                }
            }
            if (firstNonWhitespaceIndex == -1)
            {
                parseResult.SetFailure(ParseFailureKind.Argument, "Arg_MustContainEnumInfo", null);
                return false;
            }

            // We have 2 code paths here. One if they are values else if they are Strings.
            // values will have the first character as as number or a sign.
            ulong result = 0;

            char firstNonWhitespaceChar = value[firstNonWhitespaceIndex];
            if (Char.IsDigit(firstNonWhitespaceChar) || firstNonWhitespaceChar == '-' || firstNonWhitespaceChar == '+')
            {
                Type underlyingType = GetUnderlyingType(enumType);
                Object temp;

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
            String[] enumNames = entry.Names;
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
                while (valueIndex < endIndex && Char.IsWhiteSpace(value[valueIndex])) valueIndex++;
                while (endIndexNoWhitespace > valueIndex && Char.IsWhiteSpace(value[endIndexNoWhitespace - 1])) endIndexNoWhitespace--;
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
                    parseResult.SetFailure(ParseFailureKind.ArgumentWithParameter, "Arg_EnumValueNotFound", value);
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
            Contract.Ensures(Contract.Result<Type>() != null);
            Contract.EndContractBlock();

            return enumType.GetEnumUnderlyingType();
        }

        public static Array GetValues(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.EndContractBlock();

            return enumType.GetEnumValues();
        }

        internal static ulong[] InternalGetValues(RuntimeType enumType)
        {
            // Get all of the values
            return GetCachedValuesAndNames(enumType, false).Values;
        }

        public static String GetName(Type enumType, Object value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            Contract.EndContractBlock();

            return enumType.GetEnumName(value);
        }

        public static String[] GetNames(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return enumType.GetEnumNames();
        }

        internal static String[] InternalGetNames(RuntimeType enumType)
        {
            // Get all of the names
            return GetCachedValuesAndNames(enumType, true).Names;
        }

        public static Object ToObject(Type enumType, Object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Contract.EndContractBlock();

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

        [Pure]
        public static bool IsDefined(Type enumType, Object value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            Contract.EndContractBlock();

            return enumType.IsEnumDefined(value);
        }

        public static String Format(Type enumType, Object value, String format)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (format == null)
                throw new ArgumentNullException(nameof(format));
            Contract.EndContractBlock();

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
            public TypeValuesAndNames(bool isFlag, ulong[] values, String[] names)
            {
                this.IsFlag = isFlag;
                this.Values = values;
                this.Names = names;
            }

            public bool IsFlag;
            public ulong[] Values;
            public String[] Names;
        }
        #endregion

        #region Private Methods
        internal unsafe Object GetValue()
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
                        Debug.Assert(false, "Invalid primitive type");
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
                        Debug.Assert(false, "Invalid primitive type");
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
        public extern override bool Equals(Object obj);

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
                        Debug.Assert(false, "Invalid primitive type");
                        return 0;
                }
            }
        }

        public override String ToString()
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
        public String ToString(String format, IFormatProvider provider)
        {
            return ToString(format);
        }
        #endregion

        #region IComparable
        public int CompareTo(Object target)
        {
            const int retIncompatibleMethodTables = 2;  // indicates that the method tables did not match
            const int retInvalidEnumType = 3; // indicates that the enum was of an unknown/unsupported underlying type

            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

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
        public String ToString(String format)
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
        public String ToString(IFormatProvider provider)
        {
            return ToString();
        }

        public Boolean HasFlag(Enum flag)
        {
            if (flag == null)
                throw new ArgumentNullException(nameof(flag));
            Contract.EndContractBlock();

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

        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(GetValue(), CultureInfo.CurrentCulture);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Enum", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
        #endregion

        #region ToObject
        [CLSCompliant(false)]
        public static Object ToObject(Type enumType, sbyte value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, short value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, int value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, byte value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        [CLSCompliant(false)]
        public static Object ToObject(Type enumType, ushort value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        [CLSCompliant(false)]
        public static Object ToObject(Type enumType, uint value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, long value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        [CLSCompliant(false)]
        public static Object ToObject(Type enumType, ulong value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, unchecked((long)value));
        }

        private static Object ToObject(Type enumType, char value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value);
        }

        private static Object ToObject(Type enumType, bool value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return InternalBoxEnum(rtType, value ? 1 : 0);
        }
        #endregion
    }
}
