// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Threading;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System
{
    [Flags]
    public enum Base64FormattingOptions
    {
        None = 0,
        InsertLineBreaks = 1
    }

    // Returns the type code of this object. An implementation of this method
    // must not return TypeCode.Empty (which represents a null reference) or
    // TypeCode.Object (which represents an object that doesn't implement the
    // IConvertible interface). An implementation of this method should return
    // TypeCode.DBNull if the value of this object is a database null. For
    // example, a nullable integer type should return TypeCode.DBNull if the
    // value of the object is the database null. Otherwise, an implementation
    // of this method should return the TypeCode that best describes the
    // internal representation of the object.
    // The Value class provides conversion and querying methods for values. The
    // Value class contains static members only, and it is not possible to create
    // instances of the class.
    //
    // The statically typed conversion methods provided by the Value class are all
    // of the form:
    //
    //    public static XXX ToXXX(YYY value)
    //
    // where XXX is the target type and YYY is the source type. The matrix below
    // shows the set of supported conversions. The set of conversions is symmetric
    // such that for every ToXXX(YYY) there is also a ToYYY(XXX).
    //
    // From:  To: Bol Chr SBy Byt I16 U16 I32 U32 I64 U64 Sgl Dbl Dec Dat Str
    // ----------------------------------------------------------------------
    // Boolean     x       x   x   x   x   x   x   x   x   x   x   x       x
    // Char            x   x   x   x   x   x   x   x   x                   x
    // SByte       x   x   x   x   x   x   x   x   x   x   x   x   x       x
    // Byte        x   x   x   x   x   x   x   x   x   x   x   x   x       x
    // Int16       x   x   x   x   x   x   x   x   x   x   x   x   x       x
    // UInt16      x   x   x   x   x   x   x   x   x   x   x   x   x       x
    // Int32       x   x   x   x   x   x   x   x   x   x   x   x   x       x
    // UInt32      x   x   x   x   x   x   x   x   x   x   x   x   x       x
    // Int64       x   x   x   x   x   x   x   x   x   x   x   x   x       x
    // UInt64      x   x   x   x   x   x   x   x   x   x   x   x   x       x
    // Single      x       x   x   x   x   x   x   x   x   x   x   x       x
    // Double      x       x   x   x   x   x   x   x   x   x   x   x       x
    // Decimal     x       x   x   x   x   x   x   x   x   x   x   x       x
    // DateTime                                                        x   x
    // String      x   x   x   x   x   x   x   x   x   x   x   x   x   x   x
    // ----------------------------------------------------------------------
    //
    // For dynamic conversions, the Value class provides a set of methods of the
    // form:
    //
    //    public static XXX ToXXX(object value)
    //
    // where XXX is the target type (Boolean, Char, SByte, Byte, Int16, UInt16,
    // Int32, UInt32, Int64, UInt64, Single, Double, Decimal, DateTime,
    // or String). The implementations of these methods all take the form:
    //
    //    public static XXX toXXX(object value) {
    //        return value == null? XXX.Default: ((IConvertible)value).ToXXX();
    //    }
    //
    // The code first checks if the given value is a null reference (which is the
    // same as Value.Empty), in which case it returns the default value for type
    // XXX. Otherwise, a cast to IConvertible is performed, and the appropriate ToXXX()
    // method is invoked on the object. An InvalidCastException is thrown if the
    // cast to IConvertible fails, and that exception is simply allowed to propagate out
    // of the conversion method.

    // Constant representing the database null value. This value is used in
    // database applications to indicate the absence of a known value. Note
    // that Value.DBNull is NOT the same as a null object reference, which is
    // represented by Value.Empty.
    //
    // The Equals() method of DBNull always returns false, even when the
    // argument is itself DBNull.
    //
    // When passed Value.DBNull, the Value.GetTypeCode() method returns
    // TypeCode.DBNull.
    //
    // When passed Value.DBNull, the Value.ToXXX() methods all throw an
    // InvalidCastException.

    public static partial class Convert
    {
        //A typeof operation is fairly expensive (does a system call), so we'll cache these here
        //statically.  These are exactly lined up with the TypeCode, eg. ConvertType[TypeCode.Int16]
        //will give you the type of an short.
        internal static readonly Type[] ConvertTypes = {
            typeof(System.Empty),
            typeof(object),
            typeof(System.DBNull),
            typeof(bool),
            typeof(char),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(DateTime),
            typeof(object), //TypeCode is discontinuous so we need a placeholder.
            typeof(string)
        };

        // Need to special case Enum because typecode will be underlying type, e.g. Int32
        private static readonly Type EnumType = typeof(Enum);

        internal static readonly char[] base64Table = {'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
                                                       'P','Q','R','S','T','U','V','W','X','Y','Z','a','b','c','d',
                                                       'e','f','g','h','i','j','k','l','m','n','o','p','q','r','s',
                                                       't','u','v','w','x','y','z','0','1','2','3','4','5','6','7',
                                                       '8','9','+','/','=' };

        private const int base64LineBreakPosition = 76;

#if DEBUG
        private static bool TriggerAsserts = DoAsserts();
        private static bool DoAsserts()
        {
            Debug.Assert(ConvertTypes != null, "[Convert.cctor]ConvertTypes!=null");
            Debug.Assert(ConvertTypes.Length == ((int)TypeCode.String + 1), "[Convert.cctor]ConvertTypes.Length == ((int)TypeCode.String + 1)");
            Debug.Assert(ConvertTypes[(int)TypeCode.Empty] == typeof(System.Empty),
                            "[Convert.cctor]ConvertTypes[(int)TypeCode.Empty]==typeof(System.Empty)");
            Debug.Assert(ConvertTypes[(int)TypeCode.String] == typeof(string),
                            "[Convert.cctor]ConvertTypes[(int)TypeCode.String]==typeof(System.String)");
            Debug.Assert(ConvertTypes[(int)TypeCode.Int32] == typeof(int),
                            "[Convert.cctor]ConvertTypes[(int)TypeCode.Int32]==typeof(int)");
            return true;
        }
#endif

        public static readonly object DBNull = System.DBNull.Value;

        // Returns the type code for the given object. If the argument is null,
        // the result is TypeCode.Empty. If the argument is not a value (i.e. if
        // the object does not implement IConvertible), the result is TypeCode.Object.
        // Otherwise, the result is the type code of the object, as determined by
        // the object's implementation of IConvertible.
        public static TypeCode GetTypeCode(object? value)
        {
            if (value == null) return TypeCode.Empty;
            if (value is IConvertible temp)
            {
                return temp.GetTypeCode();
            }
            return TypeCode.Object;
        }

        // Returns true if the given object is a database null. This operation
        // corresponds to "value.GetTypeCode() == TypeCode.DBNull".
        public static bool IsDBNull(object? value)
        {
            if (value == System.DBNull.Value) return true;
            return value is IConvertible convertible ? convertible.GetTypeCode() == TypeCode.DBNull : false;
        }

        // Converts the given object to the given type. In general, this method is
        // equivalent to calling the Value.ToXXX(value) method for the given
        // typeCode and boxing the result.
        //
        // The method first checks if the given object implements IConvertible. If not,
        // the only permitted conversion is from a null to TypeCode.Empty, the
        // result of which is null.
        //
        // If the object does implement IConvertible, a check is made to see if the
        // object already has the given type code, in which case the object is
        // simply returned. Otherwise, the appropriate ToXXX() is invoked on the
        // object's implementation of IConvertible.
        [return: NotNullIfNotNull("value")]
        public static object? ChangeType(object? value, TypeCode typeCode)
        {
            return ChangeType(value, typeCode, CultureInfo.CurrentCulture);
        }

        [return: NotNullIfNotNull("value")]
        public static object? ChangeType(object? value, TypeCode typeCode, IFormatProvider? provider)
        {
            if (value == null && (typeCode == TypeCode.Empty || typeCode == TypeCode.String || typeCode == TypeCode.Object))
            {
                return null;
            }

            if (!(value is IConvertible v))
            {
                throw new InvalidCastException(SR.InvalidCast_IConvertible);
            }

            // This line is invalid for things like Enums that return a TypeCode
            // of int, but the object can't actually be cast to an int.
            //            if (v.GetTypeCode() == typeCode) return value;
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return v.ToBoolean(provider);
                case TypeCode.Char:
                    return v.ToChar(provider);
                case TypeCode.SByte:
                    return v.ToSByte(provider);
                case TypeCode.Byte:
                    return v.ToByte(provider);
                case TypeCode.Int16:
                    return v.ToInt16(provider);
                case TypeCode.UInt16:
                    return v.ToUInt16(provider);
                case TypeCode.Int32:
                    return v.ToInt32(provider);
                case TypeCode.UInt32:
                    return v.ToUInt32(provider);
                case TypeCode.Int64:
                    return v.ToInt64(provider);
                case TypeCode.UInt64:
                    return v.ToUInt64(provider);
                case TypeCode.Single:
                    return v.ToSingle(provider);
                case TypeCode.Double:
                    return v.ToDouble(provider);
                case TypeCode.Decimal:
                    return v.ToDecimal(provider);
                case TypeCode.DateTime:
                    return v.ToDateTime(provider);
                case TypeCode.String:
                    return v.ToString(provider);
                case TypeCode.Object:
                    return value;
                case TypeCode.DBNull:
                    throw new InvalidCastException(SR.InvalidCast_DBNull);
                case TypeCode.Empty:
                    throw new InvalidCastException(SR.InvalidCast_Empty);
                default:
                    throw new ArgumentException(SR.Arg_UnknownTypeCode);
            }
        }

        internal static object DefaultToType(IConvertible value, Type targetType, IFormatProvider? provider)
        {
            Debug.Assert(value != null, "[Convert.DefaultToType]value!=null");
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (ReferenceEquals(value.GetType(), targetType))
            {
                return value;
            }

            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Boolean]))
                return value.ToBoolean(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Char]))
                return value.ToChar(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.SByte]))
                return value.ToSByte(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Byte]))
                return value.ToByte(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Int16]))
                return value.ToInt16(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.UInt16]))
                return value.ToUInt16(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Int32]))
                return value.ToInt32(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.UInt32]))
                return value.ToUInt32(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Int64]))
                return value.ToInt64(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.UInt64]))
                return value.ToUInt64(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Single]))
                return value.ToSingle(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Double]))
                return value.ToDouble(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Decimal]))
                return value.ToDecimal(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.DateTime]))
                return value.ToDateTime(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.String]))
                return value.ToString(provider);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Object]))
                return (object)value;
            //  Need to special case Enum because typecode will be underlying type, e.g. Int32
            if (ReferenceEquals(targetType, EnumType))
                return (Enum)value;
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.DBNull]))
                throw new InvalidCastException(SR.InvalidCast_DBNull);
            if (ReferenceEquals(targetType, ConvertTypes[(int)TypeCode.Empty]))
                throw new InvalidCastException(SR.InvalidCast_Empty);

            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, value.GetType().FullName, targetType.FullName));
        }

        [return: NotNullIfNotNull("value")]
        public static object? ChangeType(object? value, Type conversionType)
        {
            return ChangeType(value, conversionType, CultureInfo.CurrentCulture);
        }

        [return: NotNullIfNotNull("value")]
        public static object? ChangeType(object? value, Type conversionType, IFormatProvider? provider)
        {
            if (conversionType is null)
            {
                throw new ArgumentNullException(nameof(conversionType));
            }

            if (value == null)
            {
                if (conversionType.IsValueType)
                {
                    throw new InvalidCastException(SR.InvalidCast_CannotCastNullToValueType);
                }
                return null;
            }

            if (!(value is IConvertible ic))
            {
                if (value.GetType() == conversionType)
                {
                    return value;
                }
                throw new InvalidCastException(SR.InvalidCast_IConvertible);
            }

            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.Boolean]))
                return ic.ToBoolean(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.Char]))
                return ic.ToChar(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.SByte]))
                return ic.ToSByte(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.Byte]))
                return ic.ToByte(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.Int16]))
                return ic.ToInt16(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.UInt16]))
                return ic.ToUInt16(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.Int32]))
                return ic.ToInt32(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.UInt32]))
                return ic.ToUInt32(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.Int64]))
                return ic.ToInt64(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.UInt64]))
                return ic.ToUInt64(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.Single]))
                return ic.ToSingle(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.Double]))
                return ic.ToDouble(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.Decimal]))
                return ic.ToDecimal(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.DateTime]))
                return ic.ToDateTime(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.String]))
                return ic.ToString(provider);
            if (ReferenceEquals(conversionType, ConvertTypes[(int)TypeCode.Object]))
                return (object)value;

            return ic.ToType(conversionType, provider);
        }

        [DoesNotReturn]
        private static void ThrowCharOverflowException() { throw new OverflowException(SR.Overflow_Char); }

        [DoesNotReturn]
        private static void ThrowByteOverflowException() { throw new OverflowException(SR.Overflow_Byte); }

        [DoesNotReturn]
        private static void ThrowSByteOverflowException() { throw new OverflowException(SR.Overflow_SByte); }

        [DoesNotReturn]
        private static void ThrowInt16OverflowException() { throw new OverflowException(SR.Overflow_Int16); }

        [DoesNotReturn]
        private static void ThrowUInt16OverflowException() { throw new OverflowException(SR.Overflow_UInt16); }

        [DoesNotReturn]
        private static void ThrowInt32OverflowException() { throw new OverflowException(SR.Overflow_Int32); }

        [DoesNotReturn]
        private static void ThrowUInt32OverflowException() { throw new OverflowException(SR.Overflow_UInt32); }

        [DoesNotReturn]
        private static void ThrowInt64OverflowException() { throw new OverflowException(SR.Overflow_Int64); }

        [DoesNotReturn]
        private static void ThrowUInt64OverflowException() { throw new OverflowException(SR.Overflow_UInt64); }

        // Conversions to Boolean
        public static bool ToBoolean(object? value)
        {
            return value == null ? false : ((IConvertible)value).ToBoolean(null);
        }

        public static bool ToBoolean(object? value, IFormatProvider? provider)
        {
            return value == null ? false : ((IConvertible)value).ToBoolean(provider);
        }


        public static bool ToBoolean(bool value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static bool ToBoolean(sbyte value)
        {
            return value != 0;
        }

        // To be consistent with IConvertible in the base data types else we get different semantics
        // with widening operations. Without this operator this widen succeeds,with this API the widening throws.
        public static bool ToBoolean(char value)
        {
            return ((IConvertible)value).ToBoolean(null);
        }

        public static bool ToBoolean(byte value)
        {
            return value != 0;
        }


        public static bool ToBoolean(short value)
        {
            return value != 0;
        }

        [CLSCompliant(false)]
        public static bool ToBoolean(ushort value)
        {
            return value != 0;
        }

        public static bool ToBoolean(int value)
        {
            return value != 0;
        }

        [CLSCompliant(false)]
        public static bool ToBoolean(uint value)
        {
            return value != 0;
        }

        public static bool ToBoolean(long value)
        {
            return value != 0;
        }

        [CLSCompliant(false)]
        public static bool ToBoolean(ulong value)
        {
            return value != 0;
        }

        public static bool ToBoolean(string? value)
        {
            if (value == null)
                return false;
            return bool.Parse(value);
        }

        public static bool ToBoolean(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return false;
            return bool.Parse(value);
        }

        public static bool ToBoolean(float value)
        {
            return value != 0;
        }

        public static bool ToBoolean(double value)
        {
            return value != 0;
        }

        public static bool ToBoolean(decimal value)
        {
            return value != 0;
        }

        public static bool ToBoolean(DateTime value)
        {
            return ((IConvertible)value).ToBoolean(null);
        }

        // Disallowed conversions to Boolean
        // public static bool ToBoolean(TimeSpan value)

        // Conversions to Char


        public static char ToChar(object? value)
        {
            return value == null ? (char)0 : ((IConvertible)value).ToChar(null);
        }

        public static char ToChar(object? value, IFormatProvider? provider)
        {
            return value == null ? (char)0 : ((IConvertible)value).ToChar(provider);
        }

        public static char ToChar(bool value)
        {
            return ((IConvertible)value).ToChar(null);
        }

        public static char ToChar(char value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static char ToChar(sbyte value)
        {
            if (value < 0) ThrowCharOverflowException();
            return (char)value;
        }

        public static char ToChar(byte value)
        {
            return (char)value;
        }

        public static char ToChar(short value)
        {
            if (value < 0) ThrowCharOverflowException();
            return (char)value;
        }

        [CLSCompliant(false)]
        public static char ToChar(ushort value)
        {
            return (char)value;
        }

        public static char ToChar(int value)
        {
            if (value < 0 || value > char.MaxValue) ThrowCharOverflowException();
            return (char)value;
        }

        [CLSCompliant(false)]
        public static char ToChar(uint value)
        {
            if (value > char.MaxValue) ThrowCharOverflowException();
            return (char)value;
        }

        public static char ToChar(long value)
        {
            if (value < 0 || value > char.MaxValue) ThrowCharOverflowException();
            return (char)value;
        }

        [CLSCompliant(false)]
        public static char ToChar(ulong value)
        {
            if (value > char.MaxValue) ThrowCharOverflowException();
            return (char)value;
        }

        //
        // @VariantSwitch
        // Remove FormatExceptions;
        //
        public static char ToChar(string value)
        {
            return ToChar(value, null);
        }

        public static char ToChar(string value, IFormatProvider? provider)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value.Length != 1)
                throw new FormatException(SR.Format_NeedSingleChar);

            return value[0];
        }

        // To be consistent with IConvertible in the base data types else we get different semantics
        // with widening operations. Without this operator this widen succeeds,with this API the widening throws.
        public static char ToChar(float value)
        {
            return ((IConvertible)value).ToChar(null);
        }

        // To be consistent with IConvertible in the base data types else we get different semantics
        // with widening operations. Without this operator this widen succeeds,with this API the widening throws.
        public static char ToChar(double value)
        {
            return ((IConvertible)value).ToChar(null);
        }

        // To be consistent with IConvertible in the base data types else we get different semantics
        // with widening operations. Without this operator this widen succeeds,with this API the widening throws.
        public static char ToChar(decimal value)
        {
            return ((IConvertible)value).ToChar(null);
        }

        public static char ToChar(DateTime value)
        {
            return ((IConvertible)value).ToChar(null);
        }


        // Disallowed conversions to Char
        // public static char ToChar(TimeSpan value)

        // Conversions to SByte

        [CLSCompliant(false)]
        public static sbyte ToSByte(object? value)
        {
            return value == null ? (sbyte)0 : ((IConvertible)value).ToSByte(null);
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(object? value, IFormatProvider? provider)
        {
            return value == null ? (sbyte)0 : ((IConvertible)value).ToSByte(provider);
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(bool value)
        {
            return value ? (sbyte)bool.True : (sbyte)bool.False;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(sbyte value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(char value)
        {
            if (value > sbyte.MaxValue) ThrowSByteOverflowException();
            return (sbyte)value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(byte value)
        {
            if (value > sbyte.MaxValue) ThrowSByteOverflowException();
            return (sbyte)value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(short value)
        {
            if (value < sbyte.MinValue || value > sbyte.MaxValue) ThrowSByteOverflowException();
            return (sbyte)value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(ushort value)
        {
            if (value > sbyte.MaxValue) ThrowSByteOverflowException();
            return (sbyte)value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(int value)
        {
            if (value < sbyte.MinValue || value > sbyte.MaxValue) ThrowSByteOverflowException();
            return (sbyte)value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(uint value)
        {
            if (value > sbyte.MaxValue) ThrowSByteOverflowException();
            return (sbyte)value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(long value)
        {
            if (value < sbyte.MinValue || value > sbyte.MaxValue) ThrowSByteOverflowException();
            return (sbyte)value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(ulong value)
        {
            if (value > (ulong)sbyte.MaxValue) ThrowSByteOverflowException();
            return (sbyte)value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(float value)
        {
            return ToSByte((double)value);
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(double value)
        {
            return ToSByte(ToInt32(value));
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(decimal value)
        {
            return decimal.ToSByte(decimal.Round(value, 0));
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(string? value)
        {
            if (value == null)
                return 0;
            return sbyte.Parse(value, CultureInfo.CurrentCulture);
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(string value, IFormatProvider? provider)
        {
            return sbyte.Parse(value, NumberStyles.Integer, provider);
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(DateTime value)
        {
            return ((IConvertible)value).ToSByte(null);
        }

        // Disallowed conversions to SByte
        // public static sbyte ToSByte(TimeSpan value)

        // Conversions to Byte

        public static byte ToByte(object? value)
        {
            return value == null ? (byte)0 : ((IConvertible)value).ToByte(null);
        }

        public static byte ToByte(object? value, IFormatProvider? provider)
        {
            return value == null ? (byte)0 : ((IConvertible)value).ToByte(provider);
        }

        public static byte ToByte(bool value)
        {
            return value ? (byte)bool.True : (byte)bool.False;
        }

        public static byte ToByte(byte value)
        {
            return value;
        }

        public static byte ToByte(char value)
        {
            if (value > byte.MaxValue) ThrowByteOverflowException();
            return (byte)value;
        }

        [CLSCompliant(false)]
        public static byte ToByte(sbyte value)
        {
            if (value < byte.MinValue) ThrowByteOverflowException();
            return (byte)value;
        }

        public static byte ToByte(short value)
        {
            if (value < byte.MinValue || value > byte.MaxValue) ThrowByteOverflowException();
            return (byte)value;
        }

        [CLSCompliant(false)]
        public static byte ToByte(ushort value)
        {
            if (value > byte.MaxValue) ThrowByteOverflowException();
            return (byte)value;
        }

        public static byte ToByte(int value)
        {
            if (value < byte.MinValue || value > byte.MaxValue) ThrowByteOverflowException();
            return (byte)value;
        }

        [CLSCompliant(false)]
        public static byte ToByte(uint value)
        {
            if (value > byte.MaxValue) ThrowByteOverflowException();
            return (byte)value;
        }

        public static byte ToByte(long value)
        {
            if (value < byte.MinValue || value > byte.MaxValue) ThrowByteOverflowException();
            return (byte)value;
        }

        [CLSCompliant(false)]
        public static byte ToByte(ulong value)
        {
            if (value > byte.MaxValue) ThrowByteOverflowException();
            return (byte)value;
        }

        public static byte ToByte(float value)
        {
            return ToByte((double)value);
        }

        public static byte ToByte(double value)
        {
            return ToByte(ToInt32(value));
        }

        public static byte ToByte(decimal value)
        {
            return decimal.ToByte(decimal.Round(value, 0));
        }

        public static byte ToByte(string? value)
        {
            if (value == null)
                return 0;
            return byte.Parse(value, CultureInfo.CurrentCulture);
        }

        public static byte ToByte(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return byte.Parse(value, NumberStyles.Integer, provider);
        }

        public static byte ToByte(DateTime value)
        {
            return ((IConvertible)value).ToByte(null);
        }


        // Disallowed conversions to Byte
        // public static byte ToByte(TimeSpan value)

        // Conversions to Int16

        public static short ToInt16(object? value)
        {
            return value == null ? (short)0 : ((IConvertible)value).ToInt16(null);
        }

        public static short ToInt16(object? value, IFormatProvider? provider)
        {
            return value == null ? (short)0 : ((IConvertible)value).ToInt16(provider);
        }

        public static short ToInt16(bool value)
        {
            return value ? (short)bool.True : (short)bool.False;
        }

        public static short ToInt16(char value)
        {
            if (value > short.MaxValue) ThrowInt16OverflowException();
            return (short)value;
        }

        [CLSCompliant(false)]
        public static short ToInt16(sbyte value)
        {
            return value;
        }

        public static short ToInt16(byte value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static short ToInt16(ushort value)
        {
            if (value > short.MaxValue) ThrowInt16OverflowException();
            return (short)value;
        }

        public static short ToInt16(int value)
        {
            if (value < short.MinValue || value > short.MaxValue) ThrowInt16OverflowException();
            return (short)value;
        }

        [CLSCompliant(false)]
        public static short ToInt16(uint value)
        {
            if (value > short.MaxValue) ThrowInt16OverflowException();
            return (short)value;
        }

        public static short ToInt16(short value)
        {
            return value;
        }

        public static short ToInt16(long value)
        {
            if (value < short.MinValue || value > short.MaxValue) ThrowInt16OverflowException();
            return (short)value;
        }

        [CLSCompliant(false)]
        public static short ToInt16(ulong value)
        {
            if (value > (ulong)short.MaxValue) ThrowInt16OverflowException();
            return (short)value;
        }

        public static short ToInt16(float value)
        {
            return ToInt16((double)value);
        }

        public static short ToInt16(double value)
        {
            return ToInt16(ToInt32(value));
        }

        public static short ToInt16(decimal value)
        {
            return decimal.ToInt16(decimal.Round(value, 0));
        }

        public static short ToInt16(string? value)
        {
            if (value == null)
                return 0;
            return short.Parse(value, CultureInfo.CurrentCulture);
        }

        public static short ToInt16(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return short.Parse(value, NumberStyles.Integer, provider);
        }

        public static short ToInt16(DateTime value)
        {
            return ((IConvertible)value).ToInt16(null);
        }


        // Disallowed conversions to Int16
        // public static short ToInt16(TimeSpan value)

        // Conversions to UInt16

        [CLSCompliant(false)]
        public static ushort ToUInt16(object? value)
        {
            return value == null ? (ushort)0 : ((IConvertible)value).ToUInt16(null);
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(object? value, IFormatProvider? provider)
        {
            return value == null ? (ushort)0 : ((IConvertible)value).ToUInt16(provider);
        }


        [CLSCompliant(false)]
        public static ushort ToUInt16(bool value)
        {
            return value ? (ushort)bool.True : (ushort)bool.False;
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(char value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(sbyte value)
        {
            if (value < 0) ThrowUInt16OverflowException();
            return (ushort)value;
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(byte value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(short value)
        {
            if (value < 0) ThrowUInt16OverflowException();
            return (ushort)value;
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(int value)
        {
            if (value < 0 || value > ushort.MaxValue) ThrowUInt16OverflowException();
            return (ushort)value;
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(ushort value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(uint value)
        {
            if (value > ushort.MaxValue) ThrowUInt16OverflowException();
            return (ushort)value;
        }


        [CLSCompliant(false)]
        public static ushort ToUInt16(long value)
        {
            if (value < 0 || value > ushort.MaxValue) ThrowUInt16OverflowException();
            return (ushort)value;
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(ulong value)
        {
            if (value > ushort.MaxValue) ThrowUInt16OverflowException();
            return (ushort)value;
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(float value)
        {
            return ToUInt16((double)value);
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(double value)
        {
            return ToUInt16(ToInt32(value));
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(decimal value)
        {
            return decimal.ToUInt16(decimal.Round(value, 0));
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(string? value)
        {
            if (value == null)
                return 0;
            return ushort.Parse(value, CultureInfo.CurrentCulture);
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return ushort.Parse(value, NumberStyles.Integer, provider);
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(DateTime value)
        {
            return ((IConvertible)value).ToUInt16(null);
        }

        // Disallowed conversions to UInt16
        // public static ushort ToUInt16(TimeSpan value)

        // Conversions to Int32

        public static int ToInt32(object? value)
        {
            return value == null ? 0 : ((IConvertible)value).ToInt32(null);
        }

        public static int ToInt32(object? value, IFormatProvider? provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToInt32(provider);
        }


        public static int ToInt32(bool value)
        {
            return value ? bool.True : bool.False;
        }

        public static int ToInt32(char value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static int ToInt32(sbyte value)
        {
            return value;
        }

        public static int ToInt32(byte value)
        {
            return value;
        }

        public static int ToInt32(short value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static int ToInt32(ushort value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static int ToInt32(uint value)
        {
            if (value > int.MaxValue) ThrowInt32OverflowException();
            return (int)value;
        }

        public static int ToInt32(int value)
        {
            return value;
        }

        public static int ToInt32(long value)
        {
            if (value < int.MinValue || value > int.MaxValue) ThrowInt32OverflowException();
            return (int)value;
        }

        [CLSCompliant(false)]
        public static int ToInt32(ulong value)
        {
            if (value > int.MaxValue) ThrowInt32OverflowException();
            return (int)value;
        }

        public static int ToInt32(float value)
        {
            return ToInt32((double)value);
        }

        public static int ToInt32(double value)
        {
            if (value >= 0)
            {
                if (value < 2147483647.5)
                {
                    int result = (int)value;
                    double dif = value - result;
                    if (dif > 0.5 || dif == 0.5 && (result & 1) != 0) result++;
                    return result;
                }
            }
            else
            {
                if (value >= -2147483648.5)
                {
                    int result = (int)value;
                    double dif = value - result;
                    if (dif < -0.5 || dif == -0.5 && (result & 1) != 0) result--;
                    return result;
                }
            }
            throw new OverflowException(SR.Overflow_Int32);
        }

        public static int ToInt32(decimal value)
        {
            return decimal.ToInt32(decimal.Round(value, 0));
        }

        public static int ToInt32(string? value)
        {
            if (value == null)
                return 0;
            return int.Parse(value, CultureInfo.CurrentCulture);
        }

        public static int ToInt32(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return int.Parse(value, NumberStyles.Integer, provider);
        }

        public static int ToInt32(DateTime value)
        {
            return ((IConvertible)value).ToInt32(null);
        }


        // Disallowed conversions to Int32
        // public static int ToInt32(TimeSpan value)

        // Conversions to UInt32

        [CLSCompliant(false)]
        public static uint ToUInt32(object? value)
        {
            return value == null ? 0 : ((IConvertible)value).ToUInt32(null);
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(object? value, IFormatProvider? provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToUInt32(provider);
        }


        [CLSCompliant(false)]
        public static uint ToUInt32(bool value)
        {
            return value ? (uint)bool.True : (uint)bool.False;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(char value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(sbyte value)
        {
            if (value < 0) ThrowUInt32OverflowException();
            return (uint)value;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(byte value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(short value)
        {
            if (value < 0) ThrowUInt32OverflowException();
            return (uint)value;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(ushort value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(int value)
        {
            if (value < 0) ThrowUInt32OverflowException();
            return (uint)value;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(uint value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(long value)
        {
            if (value < 0 || value > uint.MaxValue) ThrowUInt32OverflowException();
            return (uint)value;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(ulong value)
        {
            if (value > uint.MaxValue) ThrowUInt32OverflowException();
            return (uint)value;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(float value)
        {
            return ToUInt32((double)value);
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(double value)
        {
            if (value >= -0.5 && value < 4294967295.5)
            {
                uint result = (uint)value;
                double dif = value - result;
                if (dif > 0.5 || dif == 0.5 && (result & 1) != 0) result++;
                return result;
            }
            throw new OverflowException(SR.Overflow_UInt32);
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(decimal value)
        {
            return decimal.ToUInt32(decimal.Round(value, 0));
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(string? value)
        {
            if (value == null)
                return 0;
            return uint.Parse(value, CultureInfo.CurrentCulture);
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return uint.Parse(value, NumberStyles.Integer, provider);
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(DateTime value)
        {
            return ((IConvertible)value).ToUInt32(null);
        }

        // Disallowed conversions to UInt32
        // public static uint ToUInt32(TimeSpan value)

        // Conversions to Int64

        public static long ToInt64(object? value)
        {
            return value == null ? 0 : ((IConvertible)value).ToInt64(null);
        }

        public static long ToInt64(object? value, IFormatProvider? provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToInt64(provider);
        }


        public static long ToInt64(bool value)
        {
            return value ? bool.True : bool.False;
        }

        public static long ToInt64(char value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static long ToInt64(sbyte value)
        {
            return value;
        }

        public static long ToInt64(byte value)
        {
            return value;
        }

        public static long ToInt64(short value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static long ToInt64(ushort value)
        {
            return value;
        }

        public static long ToInt64(int value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static long ToInt64(uint value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static long ToInt64(ulong value)
        {
            if (value > long.MaxValue) ThrowInt64OverflowException();
            return (long)value;
        }

        public static long ToInt64(long value)
        {
            return value;
        }


        public static long ToInt64(float value)
        {
            return ToInt64((double)value);
        }

        public static long ToInt64(double value)
        {
            return checked((long)Math.Round(value));
        }

        public static long ToInt64(decimal value)
        {
            return decimal.ToInt64(decimal.Round(value, 0));
        }

        public static long ToInt64(string? value)
        {
            if (value == null)
                return 0;
            return long.Parse(value, CultureInfo.CurrentCulture);
        }

        public static long ToInt64(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return long.Parse(value, NumberStyles.Integer, provider);
        }

        public static long ToInt64(DateTime value)
        {
            return ((IConvertible)value).ToInt64(null);
        }

        // Disallowed conversions to Int64
        // public static long ToInt64(TimeSpan value)

        // Conversions to UInt64

        [CLSCompliant(false)]
        public static ulong ToUInt64(object? value)
        {
            return value == null ? 0 : ((IConvertible)value).ToUInt64(null);
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(object? value, IFormatProvider? provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToUInt64(provider);
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(bool value)
        {
            return value ? (ulong)bool.True : (ulong)bool.False;
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(char value)
        {
            return value;
        }


        [CLSCompliant(false)]
        public static ulong ToUInt64(sbyte value)
        {
            if (value < 0) ThrowUInt64OverflowException();
            return (ulong)value;
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(byte value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(short value)
        {
            if (value < 0) ThrowUInt64OverflowException();
            return (ulong)value;
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(ushort value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(int value)
        {
            if (value < 0) ThrowUInt64OverflowException();
            return (ulong)value;
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(uint value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(long value)
        {
            if (value < 0) ThrowUInt64OverflowException();
            return (ulong)value;
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(ulong value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(float value)
        {
            return ToUInt64((double)value);
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(double value)
        {
            return checked((ulong)Math.Round(value));
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(decimal value)
        {
            return decimal.ToUInt64(decimal.Round(value, 0));
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(string? value)
        {
            if (value == null)
                return 0;
            return ulong.Parse(value, CultureInfo.CurrentCulture);
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return ulong.Parse(value, NumberStyles.Integer, provider);
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(DateTime value)
        {
            return ((IConvertible)value).ToUInt64(null);
        }

        // Disallowed conversions to UInt64
        // public static ulong ToUInt64(TimeSpan value)

        // Conversions to Single

        public static float ToSingle(object? value)
        {
            return value == null ? 0 : ((IConvertible)value).ToSingle(null);
        }

        public static float ToSingle(object? value, IFormatProvider? provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToSingle(provider);
        }

        [CLSCompliant(false)]
        public static float ToSingle(sbyte value)
        {
            return value;
        }

        public static float ToSingle(byte value)
        {
            return value;
        }

        public static float ToSingle(char value)
        {
            return ((IConvertible)value).ToSingle(null);
        }

        public static float ToSingle(short value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static float ToSingle(ushort value)
        {
            return value;
        }

        public static float ToSingle(int value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static float ToSingle(uint value)
        {
            return value;
        }

        public static float ToSingle(long value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static float ToSingle(ulong value)
        {
            return value;
        }

        public static float ToSingle(float value)
        {
            return value;
        }

        public static float ToSingle(double value)
        {
            return (float)value;
        }

        public static float ToSingle(decimal value)
        {
            return (float)value;
        }

        public static float ToSingle(string? value)
        {
            if (value == null)
                return 0;
            return float.Parse(value, CultureInfo.CurrentCulture);
        }

        public static float ToSingle(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return float.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, provider);
        }


        public static float ToSingle(bool value)
        {
            return value ? bool.True : bool.False;
        }

        public static float ToSingle(DateTime value)
        {
            return ((IConvertible)value).ToSingle(null);
        }

        // Disallowed conversions to Single
        // public static float ToSingle(TimeSpan value)

        // Conversions to Double

        public static double ToDouble(object? value)
        {
            return value == null ? 0 : ((IConvertible)value).ToDouble(null);
        }

        public static double ToDouble(object? value, IFormatProvider? provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToDouble(provider);
        }


        [CLSCompliant(false)]
        public static double ToDouble(sbyte value)
        {
            return value;
        }

        public static double ToDouble(byte value)
        {
            return value;
        }

        public static double ToDouble(short value)
        {
            return value;
        }

        public static double ToDouble(char value)
        {
            return ((IConvertible)value).ToDouble(null);
        }

        [CLSCompliant(false)]
        public static double ToDouble(ushort value)
        {
            return value;
        }

        public static double ToDouble(int value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static double ToDouble(uint value)
        {
            return value;
        }

        public static double ToDouble(long value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static double ToDouble(ulong value)
        {
            return value;
        }

        public static double ToDouble(float value)
        {
            return value;
        }

        public static double ToDouble(double value)
        {
            return value;
        }

        public static double ToDouble(decimal value)
        {
            return (double)value;
        }

        public static double ToDouble(string? value)
        {
            if (value == null)
                return 0;
            return double.Parse(value, CultureInfo.CurrentCulture);
        }

        public static double ToDouble(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return double.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, provider);
        }

        public static double ToDouble(bool value)
        {
            return value ? bool.True : bool.False;
        }

        public static double ToDouble(DateTime value)
        {
            return ((IConvertible)value).ToDouble(null);
        }

        // Disallowed conversions to Double
        // public static double ToDouble(TimeSpan value)

        // Conversions to Decimal

        public static decimal ToDecimal(object? value)
        {
            return value == null ? 0 : ((IConvertible)value).ToDecimal(null);
        }

        public static decimal ToDecimal(object? value, IFormatProvider? provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToDecimal(provider);
        }

        [CLSCompliant(false)]
        public static decimal ToDecimal(sbyte value)
        {
            return value;
        }

        public static decimal ToDecimal(byte value)
        {
            return value;
        }

        public static decimal ToDecimal(char value)
        {
            return ((IConvertible)value).ToDecimal(null);
        }

        public static decimal ToDecimal(short value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static decimal ToDecimal(ushort value)
        {
            return value;
        }

        public static decimal ToDecimal(int value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static decimal ToDecimal(uint value)
        {
            return value;
        }

        public static decimal ToDecimal(long value)
        {
            return value;
        }

        [CLSCompliant(false)]
        public static decimal ToDecimal(ulong value)
        {
            return value;
        }

        public static decimal ToDecimal(float value)
        {
            return (decimal)value;
        }

        public static decimal ToDecimal(double value)
        {
            return (decimal)value;
        }

        public static decimal ToDecimal(string? value)
        {
            if (value == null)
                return 0m;
            return decimal.Parse(value, CultureInfo.CurrentCulture);
        }

        public static decimal ToDecimal(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0m;
            return decimal.Parse(value, NumberStyles.Number, provider);
        }

        public static decimal ToDecimal(decimal value)
        {
            return value;
        }

        public static decimal ToDecimal(bool value)
        {
            return value ? bool.True : bool.False;
        }

        public static decimal ToDecimal(DateTime value)
        {
            return ((IConvertible)value).ToDecimal(null);
        }

        // Disallowed conversions to Decimal
        // public static decimal ToDecimal(TimeSpan value)

        // Conversions to DateTime

        public static DateTime ToDateTime(DateTime value)
        {
            return value;
        }

        public static DateTime ToDateTime(object? value)
        {
            return value == null ? DateTime.MinValue : ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(object? value, IFormatProvider? provider)
        {
            return value == null ? DateTime.MinValue : ((IConvertible)value).ToDateTime(provider);
        }

        public static DateTime ToDateTime(string? value)
        {
            if (value == null)
                return new DateTime(0);
            return DateTime.Parse(value, CultureInfo.CurrentCulture);
        }

        public static DateTime ToDateTime(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return new DateTime(0);
            return DateTime.Parse(value, provider);
        }

        [CLSCompliant(false)]
        public static DateTime ToDateTime(sbyte value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(byte value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(short value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        [CLSCompliant(false)]
        public static DateTime ToDateTime(ushort value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(int value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        [CLSCompliant(false)]
        public static DateTime ToDateTime(uint value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(long value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        [CLSCompliant(false)]
        public static DateTime ToDateTime(ulong value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(bool value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(char value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(float value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(double value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(decimal value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        // Disallowed conversions to DateTime
        // public static DateTime ToDateTime(TimeSpan value)

        // Conversions to String

        public static string? ToString(object? value)
        {
            return ToString(value, null);
        }

        public static string? ToString(object? value, IFormatProvider? provider)
        {
            if (value is IConvertible ic)
                return ic.ToString(provider);
            if (value is IFormattable formattable)
                return formattable.ToString(null, provider);
            return value == null ? string.Empty : value.ToString();
        }

        public static string ToString(bool value)
        {
            return value.ToString();
        }

        public static string ToString(bool value, IFormatProvider? provider)
        {
            return value.ToString();
        }

        public static string ToString(char value)
        {
            return char.ToString(value);
        }

        public static string ToString(char value, IFormatProvider? provider)
        {
            return value.ToString();
        }

        [CLSCompliant(false)]
        public static string ToString(sbyte value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        [CLSCompliant(false)]
        public static string ToString(sbyte value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(byte value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(byte value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(short value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(short value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        [CLSCompliant(false)]
        public static string ToString(ushort value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        [CLSCompliant(false)]
        public static string ToString(ushort value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(int value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(int value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        [CLSCompliant(false)]
        public static string ToString(uint value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        [CLSCompliant(false)]
        public static string ToString(uint value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(long value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(long value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        [CLSCompliant(false)]
        public static string ToString(ulong value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        [CLSCompliant(false)]
        public static string ToString(ulong value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(float value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(float value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(double value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(double value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(decimal value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(decimal value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(DateTime value)
        {
            return value.ToString();
        }

        public static string ToString(DateTime value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        [return: NotNullIfNotNull("value")]
        public static string? ToString(string? value)
        {
            return value;
        }

        [return: NotNullIfNotNull("value")]
        public static string? ToString(string? value, IFormatProvider? provider)
        {
            return value;
        }


        //
        // Conversions which understand Base XXX numbers.
        //
        // Parses value in base base.  base can only
        // be 2, 8, 10, or 16.  If base is 16, the number may be preceded
        // by 0x; any other leading or trailing characters cause an error.
        //
        public static byte ToByte(string? value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }

            if (value == null)
            {
                return 0;
            }

            int r = ParseNumbers.StringToInt(value.AsSpan(), fromBase, ParseNumbers.IsTight | ParseNumbers.TreatAsUnsigned);
            if (r < byte.MinValue || r > byte.MaxValue)
                ThrowByteOverflowException();
            return (byte)r;
        }

        // Parses value in base fromBase.  fromBase can only
        // be 2, 8, 10, or 16.  If fromBase is 16, the number may be preceded
        // by 0x; any other leading or trailing characters cause an error.
        //
        [CLSCompliant(false)]
        public static sbyte ToSByte(string? value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }

            if (value == null)
            {
                return 0;
            }

            int r = ParseNumbers.StringToInt(value.AsSpan(), fromBase, ParseNumbers.IsTight | ParseNumbers.TreatAsI1);
            if (fromBase != 10 && r <= byte.MaxValue)
                return (sbyte)r;

            if (r < sbyte.MinValue || r > sbyte.MaxValue)
                ThrowSByteOverflowException();
            return (sbyte)r;
        }

        // Parses value in base fromBase.  fromBase can only
        // be 2, 8, 10, or 16.  If fromBase is 16, the number may be preceded
        // by 0x; any other leading or trailing characters cause an error.
        //
        public static short ToInt16(string? value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }

            if (value == null)
            {
                return 0;
            }

            int r = ParseNumbers.StringToInt(value.AsSpan(), fromBase, ParseNumbers.IsTight | ParseNumbers.TreatAsI2);
            if (fromBase != 10 && r <= ushort.MaxValue)
                return (short)r;

            if (r < short.MinValue || r > short.MaxValue)
                ThrowInt16OverflowException();
            return (short)r;
        }

        // Parses value in base fromBase.  fromBase can only
        // be 2, 8, 10, or 16.  If fromBase is 16, the number may be preceded
        // by 0x; any other leading or trailing characters cause an error.
        //
        [CLSCompliant(false)]
        public static ushort ToUInt16(string? value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }

            if (value == null)
            {
                return 0;
            }

            int r = ParseNumbers.StringToInt(value.AsSpan(), fromBase, ParseNumbers.IsTight | ParseNumbers.TreatAsUnsigned);
            if (r < ushort.MinValue || r > ushort.MaxValue)
                ThrowUInt16OverflowException();
            return (ushort)r;
        }

        // Parses value in base fromBase.  fromBase can only
        // be 2, 8, 10, or 16.  If fromBase is 16, the number may be preceded
        // by 0x; any other leading or trailing characters cause an error.
        //
        public static int ToInt32(string? value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }
            return value != null ?
                ParseNumbers.StringToInt(value.AsSpan(), fromBase, ParseNumbers.IsTight) :
                0;
        }

        // Parses value in base fromBase.  fromBase can only
        // be 2, 8, 10, or 16.  If fromBase is 16, the number may be preceded
        // by 0x; any other leading or trailing characters cause an error.
        //
        [CLSCompliant(false)]
        public static uint ToUInt32(string? value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }
            return value != null ?
                (uint)ParseNumbers.StringToInt(value.AsSpan(), fromBase, ParseNumbers.TreatAsUnsigned | ParseNumbers.IsTight) :
                0;
        }

        // Parses value in base fromBase.  fromBase can only
        // be 2, 8, 10, or 16.  If fromBase is 16, the number may be preceded
        // by 0x; any other leading or trailing characters cause an error.
        //
        public static long ToInt64(string? value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }
            return value != null ?
                ParseNumbers.StringToLong(value.AsSpan(), fromBase, ParseNumbers.IsTight) :
                0;
        }

        // Parses value in base fromBase.  fromBase can only
        // be 2, 8, 10, or 16.  If fromBase is 16, the number may be preceded
        // by 0x; any other leading or trailing characters cause an error.
        //
        [CLSCompliant(false)]
        public static ulong ToUInt64(string? value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }
            return value != null ?
                (ulong)ParseNumbers.StringToLong(value.AsSpan(), fromBase, ParseNumbers.TreatAsUnsigned | ParseNumbers.IsTight) :
                0;
        }

        // Convert the byte value to a string in base fromBase
        public static string ToString(byte value, int toBase)
        {
            if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }
            return ParseNumbers.IntToString((int)value, toBase, -1, ' ', ParseNumbers.PrintAsI1);
        }

        // Convert the Int16 value to a string in base fromBase
        public static string ToString(short value, int toBase)
        {
            if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }
            return ParseNumbers.IntToString((int)value, toBase, -1, ' ', ParseNumbers.PrintAsI2);
        }

        // Convert the Int32 value to a string in base toBase
        public static string ToString(int value, int toBase)
        {
            if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }
            return ParseNumbers.IntToString(value, toBase, -1, ' ', 0);
        }

        // Convert the Int64 value to a string in base toBase
        public static string ToString(long value, int toBase)
        {
            if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }
            return ParseNumbers.LongToString(value, toBase, -1, ' ', 0);
        }

        public static string ToBase64String(byte[] inArray)
        {
            if (inArray == null)
            {
                throw new ArgumentNullException(nameof(inArray));
            }
            return ToBase64String(new ReadOnlySpan<byte>(inArray), Base64FormattingOptions.None);
        }

        public static string ToBase64String(byte[] inArray, Base64FormattingOptions options)
        {
            if (inArray == null)
            {
                throw new ArgumentNullException(nameof(inArray));
            }
            return ToBase64String(new ReadOnlySpan<byte>(inArray), options);
        }

        public static string ToBase64String(byte[] inArray, int offset, int length)
        {
            return ToBase64String(inArray, offset, length, Base64FormattingOptions.None);
        }

        public static string ToBase64String(byte[] inArray, int offset, int length, Base64FormattingOptions options)
        {
            if (inArray == null)
                throw new ArgumentNullException(nameof(inArray));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_Index);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_GenericPositive);
            if (offset > (inArray.Length - length))
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_OffsetLength);

            return ToBase64String(new ReadOnlySpan<byte>(inArray, offset, length), options);
        }

        public static string ToBase64String(ReadOnlySpan<byte> bytes, Base64FormattingOptions options = Base64FormattingOptions.None)
        {
            if (options < Base64FormattingOptions.None || options > Base64FormattingOptions.InsertLineBreaks)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)options), nameof(options));
            }

            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            bool insertLineBreaks = (options == Base64FormattingOptions.InsertLineBreaks);
            string result = string.FastAllocateString(ToBase64_CalculateAndValidateOutputLength(bytes.Length, insertLineBreaks));

            unsafe
            {
                fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                fixed (char* charsPtr = result)
                {
                    int charsWritten = ConvertToBase64Array(charsPtr, bytesPtr, 0, bytes.Length, insertLineBreaks);
                    Debug.Assert(result.Length == charsWritten, $"Expected {result.Length} == {charsWritten}");
                }
            }

            return result;
        }

        public static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut)
        {
            return ToBase64CharArray(inArray, offsetIn, length, outArray, offsetOut, Base64FormattingOptions.None);
        }

        public static unsafe int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut, Base64FormattingOptions options)
        {
            //Do data verfication
            if (inArray == null)
                throw new ArgumentNullException(nameof(inArray));
            if (outArray == null)
                throw new ArgumentNullException(nameof(outArray));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_Index);
            if (offsetIn < 0)
                throw new ArgumentOutOfRangeException(nameof(offsetIn), SR.ArgumentOutOfRange_GenericPositive);
            if (offsetOut < 0)
                throw new ArgumentOutOfRangeException(nameof(offsetOut), SR.ArgumentOutOfRange_GenericPositive);

            if (options < Base64FormattingOptions.None || options > Base64FormattingOptions.InsertLineBreaks)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)options), nameof(options));
            }


            int retVal;

            int inArrayLength;
            int outArrayLength;
            int numElementsToCopy;

            inArrayLength = inArray.Length;

            if (offsetIn > (int)(inArrayLength - length))
                throw new ArgumentOutOfRangeException(nameof(offsetIn), SR.ArgumentOutOfRange_OffsetLength);

            if (inArrayLength == 0)
                return 0;

            bool insertLineBreaks = (options == Base64FormattingOptions.InsertLineBreaks);
            //This is the maximally required length that must be available in the char array
            outArrayLength = outArray.Length;

            // Length of the char buffer required
            numElementsToCopy = ToBase64_CalculateAndValidateOutputLength(length, insertLineBreaks);

            if (offsetOut > (int)(outArrayLength - numElementsToCopy))
                throw new ArgumentOutOfRangeException(nameof(offsetOut), SR.ArgumentOutOfRange_OffsetOut);

            fixed (char* outChars = &outArray[offsetOut])
            {
                fixed (byte* inData = &inArray[0])
                {
                    retVal = ConvertToBase64Array(outChars, inData, offsetIn, length, insertLineBreaks);
                }
            }

            return retVal;
        }

        public static unsafe bool TryToBase64Chars(ReadOnlySpan<byte> bytes, Span<char> chars, out int charsWritten, Base64FormattingOptions options = Base64FormattingOptions.None)
        {
            if (options < Base64FormattingOptions.None || options > Base64FormattingOptions.InsertLineBreaks)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)options), nameof(options));
            }

            if (bytes.Length == 0)
            {
                charsWritten = 0;
                return true;
            }

            bool insertLineBreaks = (options == Base64FormattingOptions.InsertLineBreaks);

            int charLengthRequired = ToBase64_CalculateAndValidateOutputLength(bytes.Length, insertLineBreaks);
            if (charLengthRequired > chars.Length)
            {
                charsWritten = 0;
                return false;
            }

            fixed (char* outChars = &MemoryMarshal.GetReference(chars))
            fixed (byte* inData = &MemoryMarshal.GetReference(bytes))
            {
                charsWritten = ConvertToBase64Array(outChars, inData, 0, bytes.Length, insertLineBreaks);
                return true;
            }
        }

        private static unsafe int ConvertToBase64Array(char* outChars, byte* inData, int offset, int length, bool insertLineBreaks)
        {
            int lengthmod3 = length % 3;
            int calcLength = offset + (length - lengthmod3);
            int j = 0;
            int charcount = 0;
            //Convert three bytes at a time to base64 notation.  This will consume 4 chars.
            int i;

            // get a pointer to the base64Table to avoid unnecessary range checking
            fixed (char* base64 = &base64Table[0])
            {
                for (i = offset; i < calcLength; i += 3)
                {
                    if (insertLineBreaks)
                    {
                        if (charcount == base64LineBreakPosition)
                        {
                            outChars[j++] = '\r';
                            outChars[j++] = '\n';
                            charcount = 0;
                        }
                        charcount += 4;
                    }
                    outChars[j] = base64[(inData[i] & 0xfc) >> 2];
                    outChars[j + 1] = base64[((inData[i] & 0x03) << 4) | ((inData[i + 1] & 0xf0) >> 4)];
                    outChars[j + 2] = base64[((inData[i + 1] & 0x0f) << 2) | ((inData[i + 2] & 0xc0) >> 6)];
                    outChars[j + 3] = base64[(inData[i + 2] & 0x3f)];
                    j += 4;
                }

                //Where we left off before
                i = calcLength;

                if (insertLineBreaks && (lengthmod3 != 0) && (charcount == base64LineBreakPosition))
                {
                    outChars[j++] = '\r';
                    outChars[j++] = '\n';
                }

                switch (lengthmod3)
                {
                    case 2: //One character padding needed
                        outChars[j] = base64[(inData[i] & 0xfc) >> 2];
                        outChars[j + 1] = base64[((inData[i] & 0x03) << 4) | ((inData[i + 1] & 0xf0) >> 4)];
                        outChars[j + 2] = base64[(inData[i + 1] & 0x0f) << 2];
                        outChars[j + 3] = base64[64]; //Pad
                        j += 4;
                        break;
                    case 1: // Two character padding needed
                        outChars[j] = base64[(inData[i] & 0xfc) >> 2];
                        outChars[j + 1] = base64[(inData[i] & 0x03) << 4];
                        outChars[j + 2] = base64[64]; //Pad
                        outChars[j + 3] = base64[64]; //Pad
                        j += 4;
                        break;
                }
            }

            return j;
        }

        private static int ToBase64_CalculateAndValidateOutputLength(int inputLength, bool insertLineBreaks)
        {
            long outlen = ((long)inputLength) / 3 * 4;          // the base length - we want integer division here. 
            outlen += ((inputLength % 3) != 0) ? 4 : 0;         // at most 4 more chars for the remainder

            if (outlen == 0)
                return 0;

            if (insertLineBreaks)
            {
                long newLines = outlen / base64LineBreakPosition;
                if ((outlen % base64LineBreakPosition) == 0)
                {
                    --newLines;
                }
                outlen += newLines * 2;              // the number of line break chars we'll add, "\r\n"
            }

            // If we overflow an int then we cannot allocate enough
            // memory to output the value so throw
            if (outlen > int.MaxValue)
                throw new OutOfMemoryException();

            return (int)outlen;
        }


        /// <summary>
        /// Converts the specified string, which encodes binary data as Base64 digits, to the equivalent byte array.
        /// </summary>
        /// <param name="s">The string to convert</param>
        /// <returns>The array of bytes represented by the specified Base64 string.</returns>
        public static byte[] FromBase64String(string s)
        {
            // "s" is an unfortunate parameter name, but we need to keep it for backward compat.

            if (s == null)
                throw new ArgumentNullException(nameof(s));


            unsafe
            {
                fixed (char* sPtr = s)
                {
                    return FromBase64CharPtr(sPtr, s.Length);
                }
            }
        }

        public static bool TryFromBase64String(string s, Span<byte> bytes, out int bytesWritten)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return TryFromBase64Chars(s.AsSpan(), bytes, out bytesWritten);
        }

        public static bool TryFromBase64Chars(ReadOnlySpan<char> chars, Span<byte> bytes, out int bytesWritten)
        {
            // This is actually local to one of the nested blocks but is being declared at the top as we don't want multiple stackallocs
            // for each iteraton of the loop. 
            Span<char> tempBuffer = stackalloc char[4];  // Note: The tempBuffer size could be made larger than 4 but the size must be a multiple of 4.

            bytesWritten = 0;

            while (chars.Length != 0)
            {
                // Attempt to decode a segment that doesn't contain whitespace.
                bool complete = TryDecodeFromUtf16(chars, bytes, out int consumedInThisIteration, out int bytesWrittenInThisIteration);
                bytesWritten += bytesWrittenInThisIteration;
                if (complete)
                    return true;

                chars = chars.Slice(consumedInThisIteration);
                bytes = bytes.Slice(bytesWrittenInThisIteration);

                Debug.Assert(chars.Length != 0); // If TryDecodeFromUtf16() consumed the entire buffer, it could not have returned false.
                if (chars[0].IsSpace())
                {
                    // If we got here, the very first character not consumed was a whitespace. We can skip past any consecutive whitespace, then continue decoding.

                    int indexOfFirstNonSpace = 1;
                    for (; ; )
                    {
                        if (indexOfFirstNonSpace == chars.Length)
                            break;
                        if (!chars[indexOfFirstNonSpace].IsSpace())
                            break;
                        indexOfFirstNonSpace++;
                    }

                    chars = chars.Slice(indexOfFirstNonSpace);

                    if ((bytesWrittenInThisIteration % 3) != 0 && chars.Length != 0)
                    {
                        // If we got here, the last successfully decoded block encountered an end-marker, yet we have trailing non-whitespace characters.
                        // That is not allowed.
                        bytesWritten = default;
                        return false;
                    }

                    // We now loop again to decode the next run of non-space characters. 
                }
                else
                {
                    Debug.Assert(chars.Length != 0 && !chars[0].IsSpace());

                    // If we got here, it is possible that there is whitespace that occurred in the middle of a 4-byte chunk. That is, we still have
                    // up to three Base64 characters that were left undecoded by the fast-path helper because they didn't form a complete 4-byte chunk.
                    // This is hopefully the rare case (multiline-formatted base64 message with a non-space character width that's not a multiple of 4.)
                    // We'll filter out whitespace and copy the remaining characters into a temporary buffer.
                    CopyToTempBufferWithoutWhiteSpace(chars, tempBuffer, out int consumedFromChars, out int charsWritten);
                    if ((charsWritten & 0x3) != 0)
                    {
                        // Even after stripping out whitespace, the number of characters is not divisible by 4. This cannot be a legal Base64 string.
                        bytesWritten = default;
                        return false;
                    }

                    tempBuffer = tempBuffer.Slice(0, charsWritten);
                    if (!TryDecodeFromUtf16(tempBuffer, bytes, out int consumedFromTempBuffer, out int bytesWrittenFromTempBuffer))
                    {
                        bytesWritten = default;
                        return false;
                    }
                    bytesWritten += bytesWrittenFromTempBuffer;
                    chars = chars.Slice(consumedFromChars);
                    bytes = bytes.Slice(bytesWrittenFromTempBuffer);

                    if ((bytesWrittenFromTempBuffer % 3) != 0)
                    {
                        // If we got here, this decode contained one or more padding characters ('='). We can accept trailing whitespace after this
                        // but nothing else.
                        for (int i = 0; i < chars.Length; i++)
                        {
                            if (!chars[i].IsSpace())
                            {
                                bytesWritten = default;
                                return false;
                            }
                        }
                        return true;
                    }

                    // We now loop again to decode the next run of non-space characters. 
                }
            }

            return true;
        }

        private static void CopyToTempBufferWithoutWhiteSpace(ReadOnlySpan<char> chars, Span<char> tempBuffer, out int consumed, out int charsWritten)
        {
            Debug.Assert(tempBuffer.Length != 0); // We only bound-check after writing a character to the tempBuffer.

            charsWritten = 0;
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!c.IsSpace())
                {
                    tempBuffer[charsWritten++] = c;
                    if (charsWritten == tempBuffer.Length)
                    {
                        consumed = i + 1;
                        return;
                    }
                }
            }
            consumed = chars.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSpace(this char c) => c == ' ' || c == '\t' || c == '\r' || c == '\n';

        /// <summary>
        /// Converts the specified range of a Char array, which encodes binary data as Base64 digits, to the equivalent byte array.     
        /// </summary>
        /// <param name="inArray">Chars representing Base64 encoding characters</param>
        /// <param name="offset">A position within the input array.</param>
        /// <param name="length">Number of element to convert.</param>
        /// <returns>The array of bytes represented by the specified Base64 encoding characters.</returns>
        public static byte[] FromBase64CharArray(char[] inArray, int offset, int length)
        {
            if (inArray == null)
                throw new ArgumentNullException(nameof(inArray));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_Index);

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_GenericPositive);

            if (offset > inArray.Length - length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_OffsetLength);


            if (inArray.Length == 0)
            {
                return Array.Empty<byte>();
            }

            unsafe
            {
                fixed (char* inArrayPtr = &inArray[0])
                {
                    return FromBase64CharPtr(inArrayPtr + offset, length);
                }
            }
        }

        /// <summary>
        /// Convert Base64 encoding characters to bytes:
        ///  - Compute result length exactly by actually walking the input;
        ///  - Allocate new result array based on computation;
        ///  - Decode input into the new array;
        /// </summary>
        /// <param name="inputPtr">Pointer to the first input char</param>
        /// <param name="inputLength">Number of input chars</param>
        /// <returns></returns>
        private static unsafe byte[] FromBase64CharPtr(char* inputPtr, int inputLength)
        {
            // The validity of parameters much be checked by callers, thus we are Critical here.

            Debug.Assert(0 <= inputLength);

            // We need to get rid of any trailing white spaces.
            // Otherwise we would be rejecting input such as "abc= ":
            while (inputLength > 0)
            {
                int lastChar = inputPtr[inputLength - 1];
                if (lastChar != (int)' ' && lastChar != (int)'\n' && lastChar != (int)'\r' && lastChar != (int)'\t')
                    break;
                inputLength--;
            }

            // Compute the output length:
            int resultLength = FromBase64_ComputeResultLength(inputPtr, inputLength);

            Debug.Assert(0 <= resultLength);

            // resultLength can be zero. We will still enter FromBase64_Decode and process the input.
            // It may either simply write no bytes (e.g. input = " ") or throw (e.g. input = "ab").

            // Create result byte blob:
            byte[] decodedBytes = new byte[resultLength];

            // Convert Base64 chars into bytes:
            if (!TryFromBase64Chars(new ReadOnlySpan<char>(inputPtr, inputLength), decodedBytes, out int _))
                throw new FormatException(SR.Format_BadBase64Char);

            // Note that the number of bytes written can differ from resultLength if the caller is modifying the array
            // as it is being converted. Silently ignore the failure.
            // Consider throwing exception in an non in-place release.

            // We are done:
            return decodedBytes;
        }

        /// <summary>
        /// Compute the number of bytes encoded in the specified Base 64 char array:
        /// Walk the entire input counting white spaces and padding chars, then compute result length
        /// based on 3 bytes per 4 chars.
        /// </summary>
        private static unsafe int FromBase64_ComputeResultLength(char* inputPtr, int inputLength)
        {
            const uint intEq = (uint)'=';
            const uint intSpace = (uint)' ';

            Debug.Assert(0 <= inputLength);

            char* inputEndPtr = inputPtr + inputLength;
            int usefulInputLength = inputLength;
            int padding = 0;

            while (inputPtr < inputEndPtr)
            {
                uint c = (uint)(*inputPtr);
                inputPtr++;

                // We want to be as fast as possible and filter out spaces with as few comparisons as possible.
                // We end up accepting a number of illegal chars as legal white-space chars.
                // This is ok: as soon as we hit them during actual decode we will recognise them as illegal and throw.
                if (c <= intSpace)
                    usefulInputLength--;

                else if (c == intEq)
                {
                    usefulInputLength--;
                    padding++;
                }
            }

            Debug.Assert(0 <= usefulInputLength);

            // For legal input, we can assume that 0 <= padding < 3. But it may be more for illegal input.
            // We will notice it at decode when we see a '=' at the wrong place.
            Debug.Assert(0 <= padding);

            // Perf: reuse the variable that stored the number of '=' to store the number of bytes encoded by the
            // last group that contains the '=':
            if (padding != 0)
            {
                if (padding == 1)
                    padding = 2;
                else if (padding == 2)
                    padding = 1;
                else
                    throw new FormatException(SR.Format_BadBase64Char);
            }

            // Done:
            return (usefulInputLength / 4) * 3 + padding;
        }
    }  // class Convert
}  // namespace

