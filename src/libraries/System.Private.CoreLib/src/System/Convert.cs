// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace System
{
    [Flags]
    public enum Base64FormattingOptions
    {
        None = 0,
        InsertLineBreaks = 1
    }

    // The Convert class provides conversion and querying methods for values. The
    // Convert class contains static members only, and it is not possible to create
    // instances of the class.
    //
    // The statically typed conversion methods provided by the Convert class are all
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
    // For dynamic conversions, the Convert class provides a set of methods of the
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
    // same as TypeCode.Empty), in which case it returns the default value for type
    // XXX. Otherwise, a cast to IConvertible is performed, and the appropriate ToXXX()
    // method is invoked on the object. An InvalidCastException is thrown if the
    // cast to IConvertible fails, and that exception is simply allowed to propagate out
    // of the conversion method.

    public static partial class Convert
    {
        private const int Base64LineBreakPosition = 76;
        private const int Base64VectorizationLengthThreshold = 16;

        // Constant representing the database null value. This value is used in
        // database applications to indicate the absence of a known value. Note
        // that Convert.DBNull is NOT the same as a null object reference, which is
        // represented by TypeCode.Empty.
        //
        // When passed Convert.DBNull, the Convert.GetTypeCode() method returns
        // TypeCode.DBNull.
        //
        // When passed Convert.DBNull, all the Convert.ToXXX() methods except ToString()
        // throw an InvalidCastException.
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
        public static bool IsDBNull([NotNullWhen(true)] object? value)
        {
            if (value == System.DBNull.Value) return true;
            return value is IConvertible convertible ? convertible.GetTypeCode() == TypeCode.DBNull : false;
        }

        // Converts the given object to the given type. In general, this method is
        // equivalent to calling ((IConvertible)value).ToXXX(CultureInfo.CurrentCulture) for the given
        // typeCode and boxing the result.
        //
        // The method first checks if the given object implements IConvertible. If not,
        // the only permitted conversion is from a null to TypeCode.Empty/TypeCode.String/TypeCode.Object, the
        // result of which is null.
        [return: NotNullIfNotNull(nameof(value))]
        public static object? ChangeType(object? value, TypeCode typeCode)
        {
            return ChangeType(value, typeCode, CultureInfo.CurrentCulture);
        }

        [return: NotNullIfNotNull(nameof(value))]
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
            return typeCode switch
            {
                TypeCode.Boolean => v.ToBoolean(provider),
                TypeCode.Char => v.ToChar(provider),
                TypeCode.SByte => v.ToSByte(provider),
                TypeCode.Byte => v.ToByte(provider),
                TypeCode.Int16 => v.ToInt16(provider),
                TypeCode.UInt16 => v.ToUInt16(provider),
                TypeCode.Int32 => v.ToInt32(provider),
                TypeCode.UInt32 => v.ToUInt32(provider),
                TypeCode.Int64 => v.ToInt64(provider),
                TypeCode.UInt64 => v.ToUInt64(provider),
                TypeCode.Single => v.ToSingle(provider),
                TypeCode.Double => v.ToDouble(provider),
                TypeCode.Decimal => v.ToDecimal(provider),
                TypeCode.DateTime => v.ToDateTime(provider),
                TypeCode.String => v.ToString(provider),
                TypeCode.Object => value,
                TypeCode.DBNull => throw new InvalidCastException(SR.InvalidCast_DBNull),
                TypeCode.Empty => throw new InvalidCastException(SR.InvalidCast_Empty),
                _ => throw new ArgumentException(SR.Arg_UnknownTypeCode),
            };
        }

        internal static object DefaultToType(IConvertible value, Type targetType, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(targetType);

            Debug.Assert(value != null, "[Convert.DefaultToType]value!=null");

            if (ReferenceEquals(value.GetType(), targetType))
            {
                return value;
            }

            if (ReferenceEquals(targetType, typeof(bool)))
                return value.ToBoolean(provider);
            if (ReferenceEquals(targetType, typeof(char)))
                return value.ToChar(provider);
            if (ReferenceEquals(targetType, typeof(sbyte)))
                return value.ToSByte(provider);
            if (ReferenceEquals(targetType, typeof(byte)))
                return value.ToByte(provider);
            if (ReferenceEquals(targetType, typeof(short)))
                return value.ToInt16(provider);
            if (ReferenceEquals(targetType, typeof(ushort)))
                return value.ToUInt16(provider);
            if (ReferenceEquals(targetType, typeof(int)))
                return value.ToInt32(provider);
            if (ReferenceEquals(targetType, typeof(uint)))
                return value.ToUInt32(provider);
            if (ReferenceEquals(targetType, typeof(long)))
                return value.ToInt64(provider);
            if (ReferenceEquals(targetType, typeof(ulong)))
                return value.ToUInt64(provider);
            if (ReferenceEquals(targetType, typeof(float)))
                return value.ToSingle(provider);
            if (ReferenceEquals(targetType, typeof(double)))
                return value.ToDouble(provider);
            if (ReferenceEquals(targetType, typeof(decimal)))
                return value.ToDecimal(provider);
            if (ReferenceEquals(targetType, typeof(DateTime)))
                return value.ToDateTime(provider);
            if (ReferenceEquals(targetType, typeof(string)))
                return value.ToString(provider);
            if (ReferenceEquals(targetType, typeof(object)))
                return (object)value;
            // Need to special case Enum because typecode will be underlying type, e.g. Int32
            if (ReferenceEquals(targetType, typeof(Enum)))
                return (Enum)value;
            if (ReferenceEquals(targetType, typeof(DBNull)))
                throw new InvalidCastException(SR.InvalidCast_DBNull);
            if (ReferenceEquals(targetType, typeof(Empty)))
                throw new InvalidCastException(SR.InvalidCast_Empty);

            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, value.GetType().FullName, targetType.FullName));
        }

        [return: NotNullIfNotNull(nameof(value))]
        public static object? ChangeType(object? value, Type conversionType)
        {
            return ChangeType(value, conversionType, CultureInfo.CurrentCulture);
        }

        [return: NotNullIfNotNull(nameof(value))]
        public static object? ChangeType(object? value, Type conversionType, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(conversionType);

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

            if (ReferenceEquals(conversionType, typeof(bool)))
                return ic.ToBoolean(provider);
            if (ReferenceEquals(conversionType, typeof(char)))
                return ic.ToChar(provider);
            if (ReferenceEquals(conversionType, typeof(sbyte)))
                return ic.ToSByte(provider);
            if (ReferenceEquals(conversionType, typeof(byte)))
                return ic.ToByte(provider);
            if (ReferenceEquals(conversionType, typeof(short)))
                return ic.ToInt16(provider);
            if (ReferenceEquals(conversionType, typeof(ushort)))
                return ic.ToUInt16(provider);
            if (ReferenceEquals(conversionType, typeof(int)))
                return ic.ToInt32(provider);
            if (ReferenceEquals(conversionType, typeof(uint)))
                return ic.ToUInt32(provider);
            if (ReferenceEquals(conversionType, typeof(long)))
                return ic.ToInt64(provider);
            if (ReferenceEquals(conversionType, typeof(ulong)))
                return ic.ToUInt64(provider);
            if (ReferenceEquals(conversionType, typeof(float)))
                return ic.ToSingle(provider);
            if (ReferenceEquals(conversionType, typeof(double)))
                return ic.ToDouble(provider);
            if (ReferenceEquals(conversionType, typeof(decimal)))
                return ic.ToDecimal(provider);
            if (ReferenceEquals(conversionType, typeof(DateTime)))
                return ic.ToDateTime(provider);
            if (ReferenceEquals(conversionType, typeof(string)))
                return ic.ToString(provider);
            if (ReferenceEquals(conversionType, typeof(object)))
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
        public static bool ToBoolean([NotNullWhen(true)] object? value)
        {
            return value == null ? false : ((IConvertible)value).ToBoolean(null);
        }

        public static bool ToBoolean([NotNullWhen(true)] object? value, IFormatProvider? provider)
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

        public static bool ToBoolean([NotNullWhen(true)] string? value)
        {
            if (value == null)
                return false;
            return bool.Parse(value);
        }

        public static bool ToBoolean([NotNullWhen(true)] string? value, IFormatProvider? provider)
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

        public static char ToChar(int value) => ToChar((uint)value);

        [CLSCompliant(false)]
        public static char ToChar(uint value)
        {
            if (value > char.MaxValue) ThrowCharOverflowException();
            return (char)value;
        }

        public static char ToChar(long value) => ToChar((ulong)value);

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
            ArgumentNullException.ThrowIfNull(value);

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
            if (value > (uint)sbyte.MaxValue) ThrowSByteOverflowException();
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
            return sbyte.Parse(value);
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(string value, IFormatProvider? provider)
        {
            return sbyte.Parse(value, provider);
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
            if (value < 0) ThrowByteOverflowException();
            return (byte)value;
        }

        public static byte ToByte(short value)
        {
            if ((uint)value > byte.MaxValue) ThrowByteOverflowException();
            return (byte)value;
        }

        [CLSCompliant(false)]
        public static byte ToByte(ushort value)
        {
            if (value > byte.MaxValue) ThrowByteOverflowException();
            return (byte)value;
        }

        public static byte ToByte(int value) => ToByte((uint)value);

        [CLSCompliant(false)]
        public static byte ToByte(uint value)
        {
            if (value > byte.MaxValue) ThrowByteOverflowException();
            return (byte)value;
        }

        public static byte ToByte(long value) => ToByte((ulong)value);

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
            return byte.Parse(value);
        }

        public static byte ToByte(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return byte.Parse(value, provider);
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
            if (value > (uint)short.MaxValue) ThrowInt16OverflowException();
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
            return short.Parse(value);
        }

        public static short ToInt16(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return short.Parse(value, provider);
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
        public static ushort ToUInt16(int value) => ToUInt16((uint)value);

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
        public static ushort ToUInt16(long value) => ToUInt16((ulong)value);

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
            return ushort.Parse(value);
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return ushort.Parse(value, provider);
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
            if ((int)value < 0) ThrowInt32OverflowException();
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
            return int.Parse(value);
        }

        public static int ToInt32(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return int.Parse(value, provider);
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
        public static uint ToUInt32(long value) => ToUInt32((ulong)value);

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
            return uint.Parse(value);
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return uint.Parse(value, provider);
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
            if ((long)value < 0) ThrowInt64OverflowException();
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
            return long.Parse(value);
        }

        public static long ToInt64(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return long.Parse(value, provider);
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
            return ulong.Parse(value);
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return ulong.Parse(value, provider);
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
            return float.Parse(value);
        }

        public static float ToSingle(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return float.Parse(value, provider);
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
            return double.Parse(value);
        }

        public static double ToDouble(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0;
            return double.Parse(value, provider);
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
            return decimal.Parse(value);
        }

        public static decimal ToDecimal(string? value, IFormatProvider? provider)
        {
            if (value == null)
                return 0m;
            return decimal.Parse(value, provider);
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
            return DateTime.Parse(value);
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
            return value.ToString();
        }

        [CLSCompliant(false)]
        public static string ToString(sbyte value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(byte value)
        {
            return value.ToString();
        }

        public static string ToString(byte value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(short value)
        {
            return value.ToString();
        }

        public static string ToString(short value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        [CLSCompliant(false)]
        public static string ToString(ushort value)
        {
            return value.ToString();
        }

        [CLSCompliant(false)]
        public static string ToString(ushort value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(int value)
        {
            return value.ToString();
        }

        public static string ToString(int value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        [CLSCompliant(false)]
        public static string ToString(uint value)
        {
            return value.ToString();
        }

        [CLSCompliant(false)]
        public static string ToString(uint value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(long value)
        {
            return value.ToString();
        }

        public static string ToString(long value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        [CLSCompliant(false)]
        public static string ToString(ulong value)
        {
            return value.ToString();
        }

        [CLSCompliant(false)]
        public static string ToString(ulong value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(float value)
        {
            return value.ToString();
        }

        public static string ToString(float value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(double value)
        {
            return value.ToString();
        }

        public static string ToString(double value, IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public static string ToString(decimal value)
        {
            return value.ToString();
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

        [return: NotNullIfNotNull(nameof(value))]
        public static string? ToString(string? value)
        {
            return value;
        }

        [return: NotNullIfNotNull(nameof(value))]
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
                ThrowInvalidBase();
            }

            if (value == null)
            {
                return 0;
            }

            int r = ParseNumbers.StringToInt(value.AsSpan(), fromBase, ParseNumbers.IsTight | ParseNumbers.TreatAsUnsigned);
            if ((uint)r > byte.MaxValue)
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
                ThrowInvalidBase();
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
                ThrowInvalidBase();
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
                ThrowInvalidBase();
            }

            if (value == null)
            {
                return 0;
            }

            int r = ParseNumbers.StringToInt(value.AsSpan(), fromBase, ParseNumbers.IsTight | ParseNumbers.TreatAsUnsigned);
            if ((uint)r > ushort.MaxValue)
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
                ThrowInvalidBase();
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
                ThrowInvalidBase();
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
                ThrowInvalidBase();
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
                ThrowInvalidBase();
            }
            return value != null ?
                (ulong)ParseNumbers.StringToLong(value.AsSpan(), fromBase, ParseNumbers.TreatAsUnsigned | ParseNumbers.IsTight) :
                0;
        }

        // Convert the byte value to a string in base fromBase
        public static string ToString(byte value, int toBase) =>
            ToString((int)value, toBase);

        // Convert the Int16 value to a string in base fromBase
        public static string ToString(short value, int toBase)
        {
            string format = "d";

            switch (toBase)
            {
                case 2:
                    format = "b";
                    break;

                case 8:
                    return ToOctalString((ushort)value);

                case 10:
                    break;

                case 16:
                    format = "x";
                    break;

                default:
                    ThrowInvalidBase();
                    break;
            };

            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        // Convert the Int32 value to a string in base toBase
        public static string ToString(int value, int toBase)
        {
            string format = "d";

            switch (toBase)
            {
                case 2:
                    format = "b";
                    break;

                case 8:
                    return ToOctalString((uint)value);

                case 10:
                    break;

                case 16:
                    format = "x";
                    break;

                default:
                    ThrowInvalidBase();
                    break;
            };

            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        // Convert the Int64 value to a string in base toBase
        public static string ToString(long value, int toBase)
        {
            string format = "d";

            switch (toBase)
            {
                case 2:
                    format = "b";
                    break;

                case 8:
                    return ToOctalString((ulong)value);

                case 10:
                    break;

                case 16:
                    format = "x";
                    break;

                default:
                    ThrowInvalidBase();
                    break;
            };

            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static void ThrowInvalidBase() => throw new ArgumentException(SR.Arg_InvalidBase);

        private static string ToOctalString(ulong value)
        {
            Span<char> chars = stackalloc char[22]; // max length of a ulong in octal

            int i = chars.Length;
            do
            {
                chars[--i] = (char)('0' + (value & 7));
                value >>= 3;
            }
            while (value != 0);

            return chars.Slice(i).ToString();
        }

        public static string ToBase64String(byte[] inArray)
        {
            ArgumentNullException.ThrowIfNull(inArray);

            return ToBase64String(new ReadOnlySpan<byte>(inArray), Base64FormattingOptions.None);
        }

        public static string ToBase64String(byte[] inArray, Base64FormattingOptions options)
        {
            ArgumentNullException.ThrowIfNull(inArray);

            return ToBase64String(new ReadOnlySpan<byte>(inArray), options);
        }

        public static string ToBase64String(byte[] inArray, int offset, int length)
        {
            return ToBase64String(inArray, offset, length, Base64FormattingOptions.None);
        }

        public static string ToBase64String(byte[] inArray, int offset, int length, Base64FormattingOptions options)
        {
            ArgumentNullException.ThrowIfNull(inArray);

            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, inArray.Length - length);

            return ToBase64String(new ReadOnlySpan<byte>(inArray, offset, length), options);
        }

        public static string ToBase64String(ReadOnlySpan<byte> bytes, Base64FormattingOptions options = Base64FormattingOptions.None)
        {
            if ((uint)options > (uint)Base64FormattingOptions.InsertLineBreaks)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)options), nameof(options));
            }

            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            bool insertLineBreaks = (options == Base64FormattingOptions.InsertLineBreaks);
            int outputLength = ToBase64_CalculateAndValidateOutputLength(bytes.Length, insertLineBreaks);

            string result = string.FastAllocateString(outputLength);

            if (Vector128.IsHardwareAccelerated && !insertLineBreaks && bytes.Length >= Base64VectorizationLengthThreshold)
            {
                ToBase64CharsLargeNoLineBreaks(bytes, new Span<char>(ref result.GetRawStringData(), result.Length), result.Length);
            }
            else
            {
                unsafe
                {
                    fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                    fixed (char* charsPtr = result)
                    {
                        int charsWritten = ConvertToBase64Array(charsPtr, bytesPtr, 0, bytes.Length, insertLineBreaks);
                        Debug.Assert(result.Length == charsWritten, $"Expected {result.Length} == {charsWritten}");
                    }
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
            ArgumentNullException.ThrowIfNull(inArray);
            ArgumentNullException.ThrowIfNull(outArray);

            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfNegative(offsetIn);
            ArgumentOutOfRangeException.ThrowIfNegative(offsetOut);
            if (options < Base64FormattingOptions.None || options > Base64FormattingOptions.InsertLineBreaks)
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)options), nameof(options));

            int inArrayLength = inArray.Length;

            ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetIn, inArrayLength - length);

            if (inArrayLength == 0)
                return 0;

            // This is the maximally required length that must be available in the char array
            int outArrayLength = outArray.Length;

            // Length of the char buffer required
            bool insertLineBreaks = options == Base64FormattingOptions.InsertLineBreaks;
            int charLengthRequired = ToBase64_CalculateAndValidateOutputLength(length, insertLineBreaks);

            ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetOut, outArrayLength - charLengthRequired);

            if (Vector128.IsHardwareAccelerated && !insertLineBreaks && length >= Base64VectorizationLengthThreshold)
            {
                ToBase64CharsLargeNoLineBreaks(new ReadOnlySpan<byte>(inArray, offsetIn, length), outArray.AsSpan(offsetOut), charLengthRequired);
            }
            else
            {
                fixed (char* outChars = &outArray[offsetOut])
                fixed (byte* inData = &inArray[0])
                {
                    int converted = ConvertToBase64Array(outChars, inData, offsetIn, length, insertLineBreaks);
                    Debug.Assert(converted == charLengthRequired);
                }
            }

            return charLengthRequired;
        }

        public static unsafe bool TryToBase64Chars(ReadOnlySpan<byte> bytes, Span<char> chars, out int charsWritten, Base64FormattingOptions options = Base64FormattingOptions.None)
        {
            if ((uint)options > (uint)Base64FormattingOptions.InsertLineBreaks)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)options), nameof(options));
            }

            if (bytes.Length == 0)
            {
                charsWritten = 0;
                return true;
            }

            bool insertLineBreaks = options == Base64FormattingOptions.InsertLineBreaks;

            int charLengthRequired = ToBase64_CalculateAndValidateOutputLength(bytes.Length, insertLineBreaks);
            if (charLengthRequired > chars.Length)
            {
                charsWritten = 0;
                return false;
            }

            if (Vector128.IsHardwareAccelerated && !insertLineBreaks && bytes.Length >= Base64VectorizationLengthThreshold)
            {
                ToBase64CharsLargeNoLineBreaks(bytes, chars, charLengthRequired);
            }
            else
            {
                fixed (char* outChars = &MemoryMarshal.GetReference(chars))
                fixed (byte* inData = &MemoryMarshal.GetReference(bytes))
                {
                    int converted = ConvertToBase64Array(outChars, inData, 0, bytes.Length, insertLineBreaks);
                    Debug.Assert(converted == charLengthRequired);
                }
            }

            charsWritten = charLengthRequired;
            return true;
        }

        /// <summary>Base64 encodes the bytes from <paramref name="bytes"/> into <paramref name="chars"/>.</summary>
        /// <param name="bytes">The bytes to encode.</param>
        /// <param name="chars">The destination buffer large enough to handle the encoded chars.</param>
        /// <param name="charLengthRequired">The pre-calculated, exact number of chars that will be written.</param>
        private static unsafe void ToBase64CharsLargeNoLineBreaks(ReadOnlySpan<byte> bytes, Span<char> chars, int charLengthRequired)
        {
            // For large enough inputs, it's beneficial to use the vectorized UTF8-based Base64 encoding
            // and then widen the resulting bytes into chars.
            Debug.Assert(bytes.Length >= Base64VectorizationLengthThreshold);
            Debug.Assert(chars.Length >= charLengthRequired);
            Debug.Assert(charLengthRequired % 4 == 0);

            // Base64-encode the bytes directly into the destination char buffer (reinterpreted as a byte buffer).
            OperationStatus status = Base64.EncodeToUtf8(bytes, MemoryMarshal.AsBytes(chars), out _, out int bytesWritten);
            Debug.Assert(status == OperationStatus.Done && charLengthRequired == bytesWritten);

            // Now widen the ASCII bytes in-place to chars (if the vectorized Ascii.WidenAsciiToUtf16 is ever updated
            // to support in-place updates, it should be used here instead). Since the base64 bytes are all valid ASCII, the byte
            // data is guaranteed to be 1/2 as long as the char data, and we can widen in-place.
            ref ushort dest = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(chars));
            ref byte src = ref Unsafe.As<ushort, byte>(ref dest);
            ref byte srcBeginning = ref src;

            // We process the bytes/chars from right to left to avoid overwriting the remaining unprocessed data.
            // The refs start out pointing just past the end of the data, and each iteration of a loop bumps
            // the refs back the apropriate amount and performs the copy/widening.
            dest = ref Unsafe.Add(ref dest, charLengthRequired);
            src = ref Unsafe.Add(ref src, charLengthRequired);

            // Handle 32 bytes at a time.
            if (Vector256.IsHardwareAccelerated)
            {
                ref byte srcBeginningPlus31 = ref Unsafe.Add(ref srcBeginning, 31);
                while (Unsafe.IsAddressGreaterThan(ref src, ref srcBeginningPlus31))
                {
                    src = ref Unsafe.Subtract(ref src, 32);
                    dest = ref Unsafe.Subtract(ref dest, 32);

                    (Vector256<ushort> utf16Lower, Vector256<ushort> utf16Upper) = Vector256.Widen(Vector256.LoadUnsafe(ref src));

                    utf16Lower.StoreUnsafe(ref dest);
                    utf16Upper.StoreUnsafe(ref dest, 16);
                }
            }

            // Handle 16 bytes at a time.
            if (Vector128.IsHardwareAccelerated)
            {
                ref byte srcBeginningPlus15 = ref Unsafe.Add(ref srcBeginning, 15);
                while (Unsafe.IsAddressGreaterThan(ref src, ref srcBeginningPlus15))
                {
                    src = ref Unsafe.Subtract(ref src, 16);
                    dest = ref Unsafe.Subtract(ref dest, 16);

                    (Vector128<ushort> utf16Lower, Vector128<ushort> utf16Upper) = Vector128.Widen(Vector128.LoadUnsafe(ref src));

                    utf16Lower.StoreUnsafe(ref dest);
                    utf16Upper.StoreUnsafe(ref dest, 8);
                }
            }

            // Handle 4 bytes at a time.
            ref byte srcBeginningPlus3 = ref Unsafe.Add(ref srcBeginning, 3);
            while (Unsafe.IsAddressGreaterThan(ref src, ref srcBeginningPlus3))
            {
                dest = ref Unsafe.Subtract(ref dest, 4);
                src = ref Unsafe.Subtract(ref src, 4);
                Ascii.WidenFourAsciiBytesToUtf16AndWriteToBuffer(ref Unsafe.As<ushort, char>(ref dest), Unsafe.ReadUnaligned<uint>(ref src));
            }

            // The length produced by Base64 encoding is always a multiple of 4, so we don't need to handle
            // 1 byte at a time as is common in other vectorized operations, as nothing will remain after
            // the 4-byte loop.

            Debug.Assert(Unsafe.AreSame(ref srcBeginning, ref src));
            Debug.Assert(Unsafe.AreSame(ref srcBeginning, ref Unsafe.As<ushort, byte>(ref dest)),
                "The two references should have ended up exactly at the beginning");
        }

        private static unsafe int ConvertToBase64Array(char* outChars, byte* inData, int offset, int length, bool insertLineBreaks)
        {
            int lengthmod3 = length % 3;
            int calcLength = offset + (length - lengthmod3);
            int j = 0;
            int charcount = 0;
            // Convert three bytes at a time to base64 notation.  This will consume 4 chars.
            int i;

            // get a pointer to the base64 table to avoid unnecessary range checking
            fixed (byte* base64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/="u8)
            {
                for (i = offset; i < calcLength; i += 3)
                {
                    if (insertLineBreaks)
                    {
                        if (charcount == Base64LineBreakPosition)
                        {
                            outChars[j++] = '\r';
                            outChars[j++] = '\n';
                            charcount = 0;
                        }
                        charcount += 4;
                    }
                    outChars[j] = (char)base64[(inData[i] & 0xfc) >> 2];
                    outChars[j + 1] = (char)base64[((inData[i] & 0x03) << 4) | ((inData[i + 1] & 0xf0) >> 4)];
                    outChars[j + 2] = (char)base64[((inData[i + 1] & 0x0f) << 2) | ((inData[i + 2] & 0xc0) >> 6)];
                    outChars[j + 3] = (char)base64[inData[i + 2] & 0x3f];
                    j += 4;
                }

                // Where we left off before
                i = calcLength;

                if (insertLineBreaks && (lengthmod3 != 0) && (charcount == Base64LineBreakPosition))
                {
                    outChars[j++] = '\r';
                    outChars[j++] = '\n';
                }

                switch (lengthmod3)
                {
                    case 2: // One character padding needed
                        outChars[j] = (char)base64[(inData[i] & 0xfc) >> 2];
                        outChars[j + 1] = (char)base64[((inData[i] & 0x03) << 4) | ((inData[i + 1] & 0xf0) >> 4)];
                        outChars[j + 2] = (char)base64[(inData[i + 1] & 0x0f) << 2];
                        outChars[j + 3] = (char)base64[64]; // Pad
                        j += 4;
                        break;
                    case 1: // Two character padding needed
                        outChars[j] = (char)base64[(inData[i] & 0xfc) >> 2];
                        outChars[j + 1] = (char)base64[(inData[i] & 0x03) << 4];
                        outChars[j + 2] = (char)base64[64]; // Pad
                        outChars[j + 3] = (char)base64[64]; // Pad
                        j += 4;
                        break;
                }
            }

            return j;
        }

        private static int ToBase64_CalculateAndValidateOutputLength(int inputLength, bool insertLineBreaks)
        {
            // the base length - we want integer division here, at most 4 more chars for the remainder
            uint outlen = ((uint)inputLength + 2) / 3 * 4;

            if (outlen == 0)
                return 0;

            if (insertLineBreaks)
            {
                (uint newLines, uint remainder) = Math.DivRem(outlen, Base64LineBreakPosition);
                if (remainder == 0)
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
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

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
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
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
                    while (true)
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
            ArgumentNullException.ThrowIfNull(inArray);
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, inArray.Length - length);

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

        /// <summary>
        /// Converts the specified string, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer array.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>An array of 8-bit unsigned integers that is equivalent to <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <code>null</code>.</exception>
        /// <exception cref="FormatException">The length of <paramref name="s"/>, is not zero or a multiple of 2.</exception>
        /// <exception cref="FormatException">The format of <paramref name="s"/> is invalid. <paramref name="s"/> contains a non-hex character.</exception>
        public static byte[] FromHexString(string s)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            return FromHexString(s.AsSpan());
        }

        /// <summary>
        /// Converts the span, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer array.
        /// </summary>
        /// <param name="chars">The span to convert.</param>
        /// <returns>An array of 8-bit unsigned integers that is equivalent to <paramref name="chars"/>.</returns>
        /// <exception cref="FormatException">The length of <paramref name="chars"/>, is not zero or a multiple of 2.</exception>
        /// <exception cref="FormatException">The format of <paramref name="chars"/> is invalid. <paramref name="chars"/> contains a non-hex character.</exception>
        public static byte[] FromHexString(ReadOnlySpan<char> chars)
        {
            if (chars.Length == 0)
                return Array.Empty<byte>();
            if ((uint)chars.Length % 2 != 0)
                throw new FormatException(SR.Format_BadHexLength);

            byte[] result = GC.AllocateUninitializedArray<byte>(chars.Length >> 1);

            if (!HexConverter.TryDecodeFromUtf16(chars, result, out _))
                throw new FormatException(SR.Format_BadHexChar);

            return result;
        }

        /// <summary>
        /// Converts the string, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer span.
        /// </summary>
        /// <param name="source">The string to convert.</param>
        /// <param name="destination">
        /// The span in which to write the converted 8-bit unsigned integers. When this method returns value different than <see cref="OperationStatus.Done"/>,
        /// either the span remains unmodified or contains an incomplete conversion of <paramref name="source"/>,
        /// up to the last valid character.
        /// </param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes that were written to <paramref name="destination"/>.</param>
        /// <param name="charsConsumed">When this method returns, contains the number of characters that were consumed from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        /// <exception cref="ArgumentNullException">Passed string <paramref name="source"/> is null.</exception>
        public static OperationStatus FromHexString(string source, Span<byte> destination, out int charsConsumed, out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(source);

            return FromHexString(source.AsSpan(), destination, out charsConsumed, out bytesWritten);
        }

        /// <summary>
        /// Converts the span of chars, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer span.
        /// </summary>
        /// <param name="source">The span to convert.</param>
        /// <param name="destination">
        /// The span in which to write the converted 8-bit unsigned integers. When this method returns value different than <see cref="OperationStatus.Done"/>,
        /// either the span remains unmodified or contains an incomplete conversion of <paramref name="source"/>,
        /// up to the last valid character.
        /// </param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes that were written to <paramref name="destination"/>.</param>
        /// <param name="charsConsumed">When this method returns, contains the number of characters that were consumed from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        public static OperationStatus FromHexString(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten)
        {
            (int quotient, int remainder) = Math.DivRem(source.Length, 2);

            if (quotient == 0)
            {
                charsConsumed = 0;
                bytesWritten = 0;

                return remainder == 1 ? OperationStatus.NeedMoreData : OperationStatus.Done;
            }

            var result = OperationStatus.Done;

            if (destination.Length < quotient)
            {
                source = source.Slice(0, destination.Length * 2);
                quotient = destination.Length;
                result = OperationStatus.DestinationTooSmall;
            }
            else if (remainder == 1)
            {
                source = source.Slice(0, source.Length - 1);
                destination = destination.Slice(0, destination.Length - 1);
                result = OperationStatus.NeedMoreData;
            }

            if (!HexConverter.TryDecodeFromUtf16(source, destination, out charsConsumed))
            {
                bytesWritten = charsConsumed / 2;
                return OperationStatus.InvalidData;
            }

            bytesWritten = quotient;
            charsConsumed = source.Length;
            return result;
        }

        /// <summary>
        /// Converts an array of 8-bit unsigned integers to its equivalent string representation that is encoded with uppercase hex characters.
        /// </summary>
        /// <param name="inArray">An array of 8-bit unsigned integers.</param>
        /// <returns>The string representation in hex of the elements in <paramref name="inArray"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inArray"/> is <code>null</code>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inArray"/> is too large to be encoded.</exception>
        public static string ToHexString(byte[] inArray)
        {
            ArgumentNullException.ThrowIfNull(inArray);

            return ToHexString(new ReadOnlySpan<byte>(inArray));
        }

        /// <summary>
        /// Converts a subset of an array of 8-bit unsigned integers to its equivalent string representation that is encoded with uppercase hex characters.
        /// Parameters specify the subset as an offset in the input array and the number of elements in the array to convert.
        /// </summary>
        /// <param name="inArray">An array of 8-bit unsigned integers.</param>
        /// <param name="offset">An offset in <paramref name="inArray"/>.</param>
        /// <param name="length">The number of elements of <paramref name="inArray"/> to convert.</param>
        /// <returns>The string representation in hex of <paramref name="length"/> elements of <paramref name="inArray"/>, starting at position <paramref name="offset"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inArray"/> is <code>null</code>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> plus <paramref name="length"/> is greater than the length of <paramref name="inArray"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inArray"/> is too large to be encoded.</exception>
        public static string ToHexString(byte[] inArray, int offset, int length)
        {
            ArgumentNullException.ThrowIfNull(inArray);

            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, inArray.Length - length);

            return ToHexString(new ReadOnlySpan<byte>(inArray, offset, length));
        }

        /// <summary>
        /// Converts a span of 8-bit unsigned integers to its equivalent string representation that is encoded with uppercase hex characters.
        /// </summary>
        /// <param name="bytes">A span of 8-bit unsigned integers.</param>
        /// <returns>The string representation in hex of the elements in <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bytes"/> is too large to be encoded.</exception>
        public static string ToHexString(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
                return string.Empty;
            ArgumentOutOfRangeException.ThrowIfGreaterThan(bytes.Length, int.MaxValue / 2, nameof(bytes));

            return HexConverter.ToString(bytes, HexConverter.Casing.Upper);
        }

        /// <summary>
        /// Converts a span of 8-bit unsigned integers to its equivalent span representation that is encoded with uppercase hex characters.
        /// </summary>
        /// <param name="source">A span of 8-bit unsigned integers.</param>
        /// <param name="destination">The span representation in hex of the elements in <paramref name="source"/>.</param>
        /// <param name="charsWritten">When this method returns, contains the number of chars that were written in <paramref name="destination"/>.</param>
        /// <returns>true if the conversion was successful; otherwise, false.</returns>
        public static bool TryToHexString(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
        {
            if (source.Length == 0)
            {
                charsWritten = 0;
                return true;
            }
            else if (source.Length > int.MaxValue / 2 || destination.Length > source.Length * 2)
            {
                charsWritten = 0;
                return false;
            }

            HexConverter.EncodeToUtf16(source, destination);
            charsWritten = source.Length * 2;
            return true;
        }

        /// <summary>
        /// Converts an array of 8-bit unsigned integers to its equivalent string representation that is encoded with lowercase hex characters.
        /// </summary>
        /// <param name="inArray">An array of 8-bit unsigned integers.</param>
        /// <returns>The string representation in hex of the elements in <paramref name="inArray"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inArray"/> is <code>null</code>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inArray"/> is too large to be encoded.</exception>
        public static string ToHexStringLower(byte[] inArray)
        {
            ArgumentNullException.ThrowIfNull(inArray);

            return ToHexStringLower(new ReadOnlySpan<byte>(inArray));
        }

        /// <summary>
        /// Converts a subset of an array of 8-bit unsigned integers to its equivalent string representation that is encoded with lowercase hex characters.
        /// Parameters specify the subset as an offset in the input array and the number of elements in the array to convert.
        /// </summary>
        /// <param name="inArray">An array of 8-bit unsigned integers.</param>
        /// <param name="offset">An offset in <paramref name="inArray"/>.</param>
        /// <param name="length">The number of elements of <paramref name="inArray"/> to convert.</param>
        /// <returns>The string representation in hex of <paramref name="length"/> elements of <paramref name="inArray"/>, starting at position <paramref name="offset"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inArray"/> is <code>null</code>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> plus <paramref name="length"/> is greater than the length of <paramref name="inArray"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inArray"/> is too large to be encoded.</exception>
        public static string ToHexStringLower(byte[] inArray, int offset, int length)
        {
            ArgumentNullException.ThrowIfNull(inArray);

            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, inArray.Length - length);

            return ToHexStringLower(new ReadOnlySpan<byte>(inArray, offset, length));
        }

        /// <summary>
        /// Converts a span of 8-bit unsigned integers to its equivalent string representation that is encoded with lowercase hex characters.
        /// </summary>
        /// <param name="bytes">A span of 8-bit unsigned integers.</param>
        /// <returns>The string representation in hex of the elements in <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bytes"/> is too large to be encoded.</exception>
        public static string ToHexStringLower(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
                return string.Empty;
            ArgumentOutOfRangeException.ThrowIfGreaterThan(bytes.Length, int.MaxValue / 2, nameof(bytes));

            return HexConverter.ToString(bytes, HexConverter.Casing.Lower);
        }

        /// <summary>
        /// Converts a span of 8-bit unsigned integers to its equivalent span representation that is encoded with lowercase hex characters.
        /// </summary>
        /// <param name="source">A span of 8-bit unsigned integers.</param>
        /// <param name="destination">The span representation in hex of the elements in <paramref name="source"/>.</param>
        /// <param name="charsWritten">When this method returns, contains the number of chars that were written in <paramref name="destination"/>.</param>
        /// <returns>true if the conversion was successful; otherwise, false.</returns>
        public static bool TryToHexStringLower(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
        {
            if (source.Length == 0)
            {
                charsWritten = 0;
                return true;
            }
            else if (source.Length > int.MaxValue / 2 || destination.Length > source.Length * 2)
            {
                charsWritten = 0;
                return false;
            }

            HexConverter.EncodeToUtf16(source, destination, HexConverter.Casing.Lower);
            charsWritten = source.Length * 2;
            return true;
        }
    }  // class Convert
}  // namespace
