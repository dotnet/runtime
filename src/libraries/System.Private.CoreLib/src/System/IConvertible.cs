// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System
{
    // The IConvertible interface represents an object that contains a value. This
    // interface is implemented by the following types in the System namespace:
    // Boolean, Char, SByte, Byte, Int16, UInt16, Int32, UInt32, Int64, UInt64,
    // Single, Double, Decimal, DateTime, and String. The interface may
    // be implemented by other types that are to be considered values. For example,
    // a library of nullable database types could implement IConvertible.
    //
    // Note: The interface was originally proposed as IValue.
    //
    // The implementations of IConvertible provided by the System.XXX value classes
    // simply forward to the appropriate Value.ToXXX(YYY) methods (a description of
    // the Value class follows below). In cases where a Value.ToXXX(YYY) method
    // does not exist (because the particular conversion is not supported), the
    // IConvertible implementation should simply throw an InvalidCastException.

    [CLSCompliant(false)]
    public interface IConvertible
    {
        // Returns the type code of this object. An implementation of this method
        // must not return TypeCode.Empty (which represents a null reference) or
        // TypeCode.Object (which represents an object that doesn't implement the
        // IConvertible interface). An implementation of this method should return
        // TypeCode.DBNull if the value of this object is a database null. For
        // example, a nullable integer type should return TypeCode.DBNull if the
        // value of the object is the database null. Otherwise, an implementation
        // of this method should return the TypeCode that best describes the
        // internal representation of the object.

        TypeCode GetTypeCode();

        // The ToXXX methods convert the value of the underlying object to the
        // given type. If a particular conversion is not supported, the
        // implementation must throw an InvalidCastException. If the value of the
        // underlying object is not within the range of the target type, the
        // implementation must throw an OverflowException.  The 
        // IFormatProvider? will be used to get a NumberFormatInfo or similar
        // appropriate service object, and may safely be null.

        bool ToBoolean(IFormatProvider? provider);
        char ToChar(IFormatProvider? provider);
        sbyte ToSByte(IFormatProvider? provider);
        byte ToByte(IFormatProvider? provider);
        short ToInt16(IFormatProvider? provider);
        ushort ToUInt16(IFormatProvider? provider);
        int ToInt32(IFormatProvider? provider);
        uint ToUInt32(IFormatProvider? provider);
        long ToInt64(IFormatProvider? provider);
        ulong ToUInt64(IFormatProvider? provider);
        float ToSingle(IFormatProvider? provider);
        double ToDouble(IFormatProvider? provider);
        decimal ToDecimal(IFormatProvider? provider);
        DateTime ToDateTime(IFormatProvider? provider);
        string ToString(IFormatProvider? provider);
        object ToType(Type conversionType, IFormatProvider? provider);
    }
}

