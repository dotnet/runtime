// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Nrbf;
using System.Runtime.Serialization.Formatters.Binary;
using System.Resources.Extensions.BinaryFormat;
using System.Resources.Extensions.Tests.Common;
using PrimitiveType = System.Formats.Nrbf.PrimitiveType;

namespace System.Resources.Extensions.Tests.FormattedObject;

public class PrimitiveTypeTests : SerializationTest<FormattedObjectSerializer>
{
    public static TheoryData<byte, object> RoundTrip_Data => new()
    {
        { (byte)PrimitiveType.Int64, 0L },
        { (byte)PrimitiveType.Int64, -1L },
        { (byte)PrimitiveType.Int64, 1L },
        { (byte)PrimitiveType.Int64, long.MaxValue },
        { (byte)PrimitiveType.Int64, long.MinValue },
        { (byte)PrimitiveType.UInt64, ulong.MaxValue },
        { (byte)PrimitiveType.UInt64, ulong.MinValue },
        { (byte)PrimitiveType.Int32, 0 },
        { (byte)PrimitiveType.Int32, -1 },
        { (byte)PrimitiveType.Int32, 1 },
        { (byte)PrimitiveType.Int32, int.MaxValue },
        { (byte)PrimitiveType.Int32, int.MinValue },
        { (byte)PrimitiveType.UInt32, uint.MaxValue },
        { (byte)PrimitiveType.UInt32, uint.MinValue },
        { (byte)PrimitiveType.Int16, (short)0 },
        { (byte)PrimitiveType.Int16, (short)-1 },
        { (byte)PrimitiveType.Int16, (short)1 },
        { (byte)PrimitiveType.Int16, short.MaxValue },
        { (byte)PrimitiveType.Int16, short.MinValue },
        { (byte)PrimitiveType.UInt16, ushort.MaxValue },
        { (byte)PrimitiveType.UInt16, ushort.MinValue },
        { (byte)PrimitiveType.SByte, (sbyte)0 },
        { (byte)PrimitiveType.SByte, (sbyte)-1 },
        { (byte)PrimitiveType.SByte, (sbyte)1 },
        { (byte)PrimitiveType.SByte, sbyte.MaxValue },
        { (byte)PrimitiveType.SByte, sbyte.MinValue },
        { (byte)PrimitiveType.Byte, byte.MinValue },
        { (byte)PrimitiveType.Byte, byte.MaxValue },
        { (byte)PrimitiveType.Boolean, true },
        { (byte)PrimitiveType.Boolean, false },
        { (byte)PrimitiveType.Single, 0.0f },
        { (byte)PrimitiveType.Single, -1.0f },
        { (byte)PrimitiveType.Single, 1.0f },
        { (byte)PrimitiveType.Single, float.MaxValue },
        { (byte)PrimitiveType.Single, float.MinValue },
        { (byte)PrimitiveType.Single, float.NegativeZero },
        { (byte)PrimitiveType.Single, float.NaN },
        { (byte)PrimitiveType.Single, float.NegativeInfinity },
        { (byte)PrimitiveType.Double, 0.0d },
        { (byte)PrimitiveType.Double, -1.0d },
        { (byte)PrimitiveType.Double, 1.0d },
        { (byte)PrimitiveType.Double, double.MaxValue },
        { (byte)PrimitiveType.Double, double.MinValue },
        { (byte)PrimitiveType.Double, double.NegativeZero },
        { (byte)PrimitiveType.Double, double.NaN },
        { (byte)PrimitiveType.Double, double.NegativeInfinity },
        { (byte)PrimitiveType.TimeSpan, TimeSpan.MinValue },
        { (byte)PrimitiveType.TimeSpan, TimeSpan.MaxValue },
        { (byte)PrimitiveType.DateTime, DateTime.MinValue },
        { (byte)PrimitiveType.DateTime, DateTime.MaxValue },
    };

    [Theory]
    [MemberData(nameof(Primitive_Data))]
    public void PrimitiveTypeMemberName(object value)
    {
        BinaryFormattedObject format = new(Serialize(value));
        VerifyNonGeneric(value, format[format.RootRecord.Id]);
    }

    [Theory]
    [MemberData(nameof(Primitive_Data))]
    [MemberData(nameof(Primitive_ExtendedData))]
    public void BinaryFormattedObject_ReadPrimitive(object value)
    {
        BinaryFormattedObject formattedObject = new(Serialize(value));
        Assert.Equal(value, ((PrimitiveTypeRecord)formattedObject.RootRecord).Value);
    }

    public static TheoryData<object> Primitive_Data => new()
    {
        int.MaxValue,
        uint.MaxValue,
        long.MaxValue,
        ulong.MaxValue,
        short.MaxValue,
        ushort.MaxValue,
        byte.MaxValue,
        sbyte.MaxValue,
        true,
        float.MaxValue,
        double.MaxValue,
        char.MaxValue
    };

    public static TheoryData<object> Primitive_ExtendedData => new()
    {
        TimeSpan.MaxValue,
        DateTime.MaxValue,
        decimal.MaxValue,
        (nint)1918,
        (nuint)2020,
        "Roundabout"
    };

    private static void VerifyNonGeneric(object value, SerializationRecord record)
    {
        typeof(PrimitiveTypeTests)
            .GetMethod(nameof(VerifyGeneric), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(value.GetType()).Invoke(null, [value, record]);
    }

    private static void VerifyGeneric<T>(T value, SerializationRecord record) where T : unmanaged
    {
        Assert.Equal(value, ((PrimitiveTypeRecord)record).Value);
        Assert.Equal(value, ((PrimitiveTypeRecord<T>)record).Value);
    }
}
