// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose: The interface provides the connection between an
** instance of SerializationInfo and the formatter-provided
** class which knows how to parse the data inside the 
** SerializationInfo.
**
**
============================================================*/
namespace System.Runtime.Serialization {
    using System;

    [CLSCompliant(false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IFormatterConverter {
        Object Convert(Object value, Type type);
        Object Convert(Object value, TypeCode typeCode);
        bool   ToBoolean(Object value);
        char   ToChar(Object value);
        sbyte  ToSByte(Object value);
        byte   ToByte(Object value);
        short  ToInt16(Object value);
        ushort ToUInt16(Object value);
        int    ToInt32(Object value);
        uint   ToUInt32(Object value);
        long   ToInt64(Object value);
        ulong  ToUInt64(Object value);
        float  ToSingle(Object value);
        double ToDouble(Object value);
        Decimal ToDecimal(Object value);
        DateTime ToDateTime(Object value);
        String   ToString(Object value);
    }
}
