// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The TypeCode enum represents the type code of an object. To obtain the
// TypeCode for a given object, use the Value.GetTypeCode() method. The
// TypeCode.Empty value represents a null object reference. The TypeCode.Object
// value represents an object that doesn't implement the IConvertible interface. The
// TypeCode.DBNull value represents the database null, which is distinct and
// different from a null reference. The other type codes represent values that
// use the given simple type encoding.
//
// Note that when an object has a given TypeCode, there is no guarantee that
// the object is an instance of the corresponding System.XXX value class. For
// example, an object with the type code TypeCode.Int32 might actually be an
// instance of a nullable 32-bit integer type (with a value that isn't the
// database null).
//
// There are no type codes for "Missing", "Error", "IDispatch", and "IUnknown".
// These types of values are instead represented as classes. When the type code
// of an object is TypeCode.Object, a further instance-of check can be used to
// determine if the object is one of these values.

#nullable enable
namespace System
{
    public enum TypeCode
    {
        Empty = 0,          // Null reference
        Object = 1,         // Instance that isn't a value
        DBNull = 2,         // Database null value
        Boolean = 3,        // Boolean
        Char = 4,           // Unicode character
        SByte = 5,          // Signed 8-bit integer
        Byte = 6,           // Unsigned 8-bit integer
        Int16 = 7,          // Signed 16-bit integer
        UInt16 = 8,         // Unsigned 16-bit integer
        Int32 = 9,          // Signed 32-bit integer
        UInt32 = 10,        // Unsigned 32-bit integer
        Int64 = 11,         // Signed 64-bit integer
        UInt64 = 12,        // Unsigned 64-bit integer
        Single = 13,        // IEEE 32-bit float
        Double = 14,        // IEEE 64-bit double
        Decimal = 15,       // Decimal
        DateTime = 16,      // DateTime
        String = 18,        // Unicode character string
    }
}
