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

            if (value is not IConvertible v)
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

            if (value is not IConvertible ic)
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
            return value != null && ((IConvertible)value).ToBoolean(null);
        }

        public static bool ToBoolean([NotNullWhen(true)] object? value, IFormatProvider? provider)
        {
            return value != null && ((IConvertible)value).ToBoolean(provider);
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
        // by 0x or 0X; any other leading or trailing characters cause an error.
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
        // by 0x or 0X; any other leading or trailing characters cause an error.
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
        // by 0x or 0X; any other leading or trailing characters cause an error.
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
        // by 0x or 0X; any other leading or trailing characters cause an error.
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
        // by 0x or 0X; any other leading or trailing characters cause an error.
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
        // by 0x or 0X; any other leading or trailing characters cause an error.
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
        // by 0x or 0X; any other leading or trailing characters cause an error.
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
        // by 0x or 0X; any other leading or trailing characters cause an error.
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

        // Convert the byte value to a string in base toBase
        public static string ToString(byte value, int toBase) =>
            ToString((int)value, toBase);

        // Convert the Int16 value to a string in base toBase
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

            return Base64.EncodeToString(inArray);
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

            if (!insertLineBreaks)
            {
                return Base64.EncodeToString(bytes);
            }

            int outputLength = ToBase64_CalculateAndValidateOutputLength(bytes.Length, insertLineBreaks: true);

            return string.Create(outputLength, bytes, static (buffer, bytes) =>
            {
                int charsWritten = ConvertToBase64WithLineBreaks(buffer, bytes);
                Debug.Assert(buffer.Length == charsWritten, $"Expected {buffer.Length} == {charsWritten}");
            });
        }

        public static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut)
        {
            return ToBase64CharArray(inArray, offsetIn, length, outArray, offsetOut, Base64FormattingOptions.None);
        }

        public static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut, Base64FormattingOptions options)
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

            if (length == 0)
                return 0;

            // This is the maximally required length that must be available in the char array
            int outArrayLength = outArray.Length;

            // Length of the char buffer required
            bool insertLineBreaks = options == Base64FormattingOptions.InsertLineBreaks;
            int charLengthRequired = ToBase64_CalculateAndValidateOutputLength(length, insertLineBreaks);

            ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetOut, outArrayLength - charLengthRequired);

            if (!insertLineBreaks)
            {
                int charsWritten = Base64.EncodeToChars(new ReadOnlySpan<byte>(inArray, offsetIn, length), outArray.AsSpan(offsetOut));
                Debug.Assert(charsWritten == charLengthRequired);
            }
            else
            {
                int converted = ConvertToBase64WithLineBreaks(outArray.AsSpan(offsetOut), new ReadOnlySpan<byte>(inArray, offsetIn, length));
                Debug.Assert(converted == charLengthRequired);
            }

            return charLengthRequired;
        }

        public static bool TryToBase64Chars(ReadOnlySpan<byte> bytes, Span<char> chars, out int charsWritten, Base64FormattingOptions options = Base64FormattingOptions.None)
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

            if (!insertLineBreaks)
            {
                int written = Base64.EncodeToChars(bytes, chars);
                Debug.Assert(written == charLengthRequired);
            }
            else
            {
                int converted = ConvertToBase64WithLineBreaks(chars, bytes);
                Debug.Assert(converted == charLengthRequired);
            }

            charsWritten = charLengthRequired;
            return true;
        }

        private static int ConvertToBase64WithLineBreaks(Span<char> destination, ReadOnlySpan<byte> source)
        {
            Debug.Assert(destination.Length >= ToBase64_CalculateAndValidateOutputLength(source.Length, insertLineBreaks: true));

            int writeOffset = 0;

            while (true)
            {
                int chunkSize = Math.Min(source.Length, Base64LineBreakPosition / 4 * 3); // 76 base64 chars == 57 bytes

                OperationStatus status = Base64.EncodeToChars(source.Slice(0, chunkSize), destination.Slice(writeOffset), out int bytesConsumed, out int charsWritten);
                Debug.Assert(status == OperationStatus.Done && bytesConsumed == chunkSize);

                source = source.Slice(chunkSize);
                writeOffset += charsWritten;

                if (source.IsEmpty)
                {
                    break;
                }

                destination[writeOffset++] = '\r';
                destination[writeOffset++] = '\n';
            }

            return writeOffset;
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

            return Base64.DecodeFromChars(s);
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
            OperationStatus status = Base64.DecodeFromChars(chars, bytes, out _, out bytesWritten);
            if (status == OperationStatus.Done)
            {
                return true;
            }

            bytesWritten = 0;
            return false;
        }

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

            return Base64.DecodeFromChars(new ReadOnlySpan<char>(inArray, offset, length));
        }

        /// <summary>Converts the specified string, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer array.</summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>An array of 8-bit unsigned integers that is equivalent to <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <code>null</code>.</exception>
        /// <exception cref="FormatException">The length of <paramref name="s"/>, is not zero or a multiple of 2.</exception>
        /// <exception cref="FormatException">The format of <paramref name="s"/> is invalid. <paramref name="s"/> contains a non-hex character.</exception>
        public static byte[] FromHexString(string s)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return FromHexString(s.AsSpan());
        }

        /// <summary>Converts the span, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer array.</summary>
        /// <param name="chars">The span to convert.</param>
        /// <returns>An array of 8-bit unsigned integers that is equivalent to <paramref name="chars"/>.</returns>
        /// <exception cref="FormatException">The length of <paramref name="chars"/>, is not zero or a multiple of 2.</exception>
        /// <exception cref="FormatException">The format of <paramref name="chars"/> is invalid. <paramref name="chars"/> contains a non-hex character.</exception>
        public static byte[] FromHexString(ReadOnlySpan<char> chars)
        {
            if (chars.Length == 0)
            {
                return [];
            }

            if (!int.IsEvenInteger(chars.Length))
            {
                ThrowHelper.ThrowFormatException_BadHexLength();
            }

            byte[] result = GC.AllocateUninitializedArray<byte>(chars.Length / 2);

            if (!HexConverter.TryDecodeFromUtf16(chars, result, out _))
            {
                ThrowHelper.ThrowFormatException_BadHexChar();
            }
            return result;
        }

        /// <summary>Converts the span, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer array.</summary>
        /// <param name="utf8Source">The UTF-8 span to convert.</param>
        /// <returns>An array of 8-bit unsigned integers that is equivalent to <paramref name="utf8Source"/>.</returns>
        /// <exception cref="FormatException">The length of <paramref name="utf8Source"/>, is not zero or a multiple of 2.</exception>
        /// <exception cref="FormatException">The format of <paramref name="utf8Source"/> is invalid. <paramref name="utf8Source"/> contains a non-hex character.</exception>
        public static byte[] FromHexString(ReadOnlySpan<byte> utf8Source)
        {
            if (utf8Source.Length == 0)
            {
                return [];
            }

            if (!int.IsEvenInteger(utf8Source.Length))
            {
                ThrowHelper.ThrowFormatException_BadHexLength();
            }

            byte[] result = GC.AllocateUninitializedArray<byte>(utf8Source.Length / 2);

            if (!HexConverter.TryDecodeFromUtf8(utf8Source, result, out _))
            {
                ThrowHelper.ThrowFormatException_BadHexChar();
            }
            return result;
        }

        /// <summary>Converts the string, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer span.</summary>
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

        /// <summary>Converts the span of chars, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer span.</summary>
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

                return (remainder == 1) ? OperationStatus.NeedMoreData : OperationStatus.Done;
            }

            OperationStatus result;

            if (destination.Length < quotient)
            {
                source = source.Slice(0, destination.Length * 2);
                quotient = destination.Length;
                result = OperationStatus.DestinationTooSmall;
            }
            else
            {
                if (remainder == 1)
                {
                    source = source.Slice(0, source.Length - 1);
                    result = OperationStatus.NeedMoreData;
                }
                else
                {
                    result = OperationStatus.Done;
                }

                destination = destination.Slice(0, quotient);
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

        /// <summary>Converts the span of UTF-8 chars, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer span.</summary>
        /// <param name="utf8Source">The span to convert.</param>
        /// <param name="destination">
        /// The span in which to write the converted 8-bit unsigned integers. When this method returns value different than <see cref="OperationStatus.Done"/>,
        /// either the span remains unmodified or contains an incomplete conversion of <paramref name="utf8Source"/>,
        /// up to the last valid character.
        /// </param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes that were written to <paramref name="destination"/>.</param>
        /// <param name="bytesConsumed">When this method returns, contains the number of bytes that were consumed from <paramref name="utf8Source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        public static OperationStatus FromHexString(ReadOnlySpan<byte> utf8Source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
        {
            (int quotient, int remainder) = Math.DivRem(utf8Source.Length, 2);

            if (quotient == 0)
            {
                bytesConsumed = 0;
                bytesWritten = 0;

                return (remainder == 1) ? OperationStatus.NeedMoreData : OperationStatus.Done;
            }

            OperationStatus result;

            if (destination.Length < quotient)
            {
                utf8Source = utf8Source.Slice(0, destination.Length * 2);
                quotient = destination.Length;
                result = OperationStatus.DestinationTooSmall;
            }
            else
            {
                if (remainder == 1)
                {
                    utf8Source = utf8Source.Slice(0, utf8Source.Length - 1);
                    result = OperationStatus.NeedMoreData;
                }
                else
                {
                    result = OperationStatus.Done;
                }

                destination = destination.Slice(0, quotient);
            }

            if (!HexConverter.TryDecodeFromUtf8(utf8Source, destination, out bytesConsumed))
            {
                bytesWritten = bytesConsumed / 2;
                return OperationStatus.InvalidData;
            }

            bytesWritten = quotient;
            bytesConsumed = utf8Source.Length;
            return result;
        }

        /// <summary>Converts an array of 8-bit unsigned integers to its equivalent string representation that is encoded with uppercase hex characters.</summary>
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

        /// <summary>Converts a span of 8-bit unsigned integers to its equivalent string representation that is encoded with uppercase hex characters.</summary>
        /// <param name="bytes">A span of 8-bit unsigned integers.</param>
        /// <returns>The string representation in hex of the elements in <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bytes"/> is too large to be encoded.</exception>
        public static string ToHexString(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
            {
                return string.Empty;
            }
            ArgumentOutOfRangeException.ThrowIfGreaterThan(bytes.Length, int.MaxValue / 2, nameof(bytes));

            return HexConverter.ToString(bytes, HexConverter.Casing.Upper);
        }

        /// <summary>Converts a span of 8-bit unsigned integers to its equivalent span representation that is encoded with uppercase hex characters.</summary>
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
            else if ((source.Length > (int.MaxValue / 2)) || (destination.Length < (source.Length * 2)))
            {
                charsWritten = 0;
                return false;
            }

            HexConverter.EncodeToUtf16(source, destination);
            charsWritten = source.Length * 2;
            return true;
        }

        /// <summary>Converts a span of 8-bit unsigned integers to its equivalent UTF-8 span representation that is encoded with uppercase hex characters.</summary>
        /// <param name="source">A span of 8-bit unsigned integers.</param>
        /// <param name="utf8Destination">The UTF-8 span representation in hex of the elements in <paramref name="source"/>.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
        /// <returns>true if the conversion was successful; otherwise, false.</returns>
        public static bool TryToHexString(ReadOnlySpan<byte> source, Span<byte> utf8Destination, out int bytesWritten)
        {
            if (source.Length == 0)
            {
                bytesWritten = 0;
                return true;
            }
            else if ((source.Length > (int.MaxValue / 2)) || (utf8Destination.Length < (source.Length * 2)))
            {
                bytesWritten = 0;
                return false;
            }

            HexConverter.EncodeToUtf8(source, utf8Destination);
            bytesWritten = source.Length * 2;
            return true;
        }

        /// <summary>Converts an array of 8-bit unsigned integers to its equivalent string representation that is encoded with lowercase hex characters.</summary>
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

        /// <summary>Converts a span of 8-bit unsigned integers to its equivalent string representation that is encoded with lowercase hex characters.</summary>
        /// <param name="bytes">A span of 8-bit unsigned integers.</param>
        /// <returns>The string representation in hex of the elements in <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bytes"/> is too large to be encoded.</exception>
        public static string ToHexStringLower(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
            {
                return string.Empty;
            }
            ArgumentOutOfRangeException.ThrowIfGreaterThan(bytes.Length, int.MaxValue / 2, nameof(bytes));

            return HexConverter.ToString(bytes, HexConverter.Casing.Lower);
        }

        /// <summary>Converts a span of 8-bit unsigned integers to its equivalent span representation that is encoded with lowercase hex characters.</summary>
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
            else if ((source.Length > (int.MaxValue / 2)) || (destination.Length < (source.Length * 2)))
            {
                charsWritten = 0;
                return false;
            }

            HexConverter.EncodeToUtf16(source, destination, HexConverter.Casing.Lower);
            charsWritten = source.Length * 2;
            return true;
        }

        /// <summary>Converts a span of 8-bit unsigned integers to its equivalent UTF-8 span representation that is encoded with lowercase hex characters.</summary>
        /// <param name="source">A span of 8-bit unsigned integers.</param>
        /// <param name="utf8Destination">The UTF-8 span representation in hex of the elements in <paramref name="source"/>.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
        /// <returns>true if the conversion was successful; otherwise, false.</returns>
        public static bool TryToHexStringLower(ReadOnlySpan<byte> source, Span<byte> utf8Destination, out int bytesWritten)
        {
            if (source.Length == 0)
            {
                bytesWritten = 0;
                return true;
            }
            else if ((source.Length > (int.MaxValue / 2)) || (utf8Destination.Length < (source.Length * 2)))
            {
                bytesWritten = 0;
                return false;
            }

            HexConverter.EncodeToUtf8(source, utf8Destination, HexConverter.Casing.Lower);
            bytesWritten = source.Length * 2;
            return true;
        }
    }
}
