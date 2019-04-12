// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.Serialization
{
    [CLSCompliant(false)]
    public interface IFormatterConverter
    {
        object Convert(object value, Type type);
        object Convert(object value, TypeCode typeCode);
        bool ToBoolean(object value);
        char ToChar(object value);
        sbyte ToSByte(object value);
        byte ToByte(object value);
        short ToInt16(object value);
        ushort ToUInt16(object value);
        int ToInt32(object value);
        uint ToUInt32(object value);
        long ToInt64(object value);
        ulong ToUInt64(object value);
        float ToSingle(object value);
        double ToDouble(object value);
        decimal ToDecimal(object value);
        DateTime ToDateTime(object value);
        string? ToString(object value);
    }
}
