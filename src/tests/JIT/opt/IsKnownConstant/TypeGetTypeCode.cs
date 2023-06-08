// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        AreSame(Type.GetTypeCode(null),             TypeCode.Empty);
        AreSame(Type.GetTypeCode(typeof(void*)),    TypeCode.Object);
        AreSame(Type.GetTypeCode(typeof(nint)),     TypeCode.Object);
        AreSame(Type.GetTypeCode(typeof(nuint)),    TypeCode.Object);
        AreSame(Type.GetTypeCode(typeof(IntPtr)),   TypeCode.Object);
        AreSame(Type.GetTypeCode(typeof(UIntPtr)),  TypeCode.Object);
        AreSame(Type.GetTypeCode(typeof(bool)),     TypeCode.Boolean);
        AreSame(Type.GetTypeCode(typeof(char)),     TypeCode.Char);
        AreSame(Type.GetTypeCode(typeof(sbyte)),    TypeCode.SByte);
        AreSame(Type.GetTypeCode(typeof(byte)),     TypeCode.Byte);
        AreSame(Type.GetTypeCode(typeof(short)),    TypeCode.Int16);
        AreSame(Type.GetTypeCode(typeof(ushort)),   TypeCode.UInt16);
        AreSame(Type.GetTypeCode(typeof(int)),      TypeCode.Int32);
        AreSame(Type.GetTypeCode(typeof(uint)),     TypeCode.UInt32);
        AreSame(Type.GetTypeCode(typeof(long)),     TypeCode.Int64);
        AreSame(Type.GetTypeCode(typeof(ulong)),    TypeCode.UInt64);
        AreSame(Type.GetTypeCode(typeof(float)),    TypeCode.Single);
        AreSame(Type.GetTypeCode(typeof(double)),   TypeCode.Double);
        AreSame(Type.GetTypeCode(typeof(decimal)),  TypeCode.Decimal);
        AreSame(Type.GetTypeCode(typeof(string)),   TypeCode.String);
        AreSame(Type.GetTypeCode(typeof(object)),   TypeCode.Object);
        AreSame(Type.GetTypeCode(typeof(DateTime)), TypeCode.DateTime);

        AreSame(Type.GetTypeCode(typeof(GenericEnumClass<>)),                          TypeCode.Object);
        AreSame(Type.GetTypeCode(typeof(GenericEnumClass<IntEnum>)),                   TypeCode.Object);
        AreSame(Type.GetTypeCode(typeof(GenericEnumClass<>).GetGenericArguments()[0]), TypeCode.Object);

        AreSame(Type.GetTypeCode(typeof(SByteEnum)),  TypeCode.SByte);
        AreSame(Type.GetTypeCode(typeof(ByteEnum)),   TypeCode.Byte);
        AreSame(Type.GetTypeCode(typeof(ShortEnum)),  TypeCode.Int16);
        AreSame(Type.GetTypeCode(typeof(UShortEnum)), TypeCode.UInt16);
        AreSame(Type.GetTypeCode(typeof(IntEnum)),    TypeCode.Int32);
        AreSame(Type.GetTypeCode(typeof(UIntEnum)),   TypeCode.UInt32);
        AreSame(Type.GetTypeCode(typeof(LongEnum)),   TypeCode.Int64);
        AreSame(Type.GetTypeCode(typeof(ULongEnum)),  TypeCode.UInt64);

        AreSame(Type.GetTypeCode(typeof(CharEnum)),    TypeCode.Char);
        AreSame(Type.GetTypeCode(typeof(BoolEnum)),    TypeCode.Boolean);
        AreSame(Type.GetTypeCode(typeof(FloatEnum)),   TypeCode.Single);
        AreSame(Type.GetTypeCode(typeof(DoubleEnum)),  TypeCode.Double);
        AreSame(Type.GetTypeCode(typeof(IntPtrEnum)),  TypeCode.Object);
        AreSame(Type.GetTypeCode(typeof(UIntPtrEnum)), TypeCode.Object);

        AreSame(Type.GetTypeCode(NoInline(typeof(string))),     TypeCode.String);
        AreSame(Type.GetTypeCode(NoInline(typeof(int))),        TypeCode.Int32);
        AreSame(Type.GetTypeCode(NoInline(typeof(IntEnum))),    TypeCode.Int32);
        AreSame(Type.GetTypeCode(NoInline(typeof(CharEnum))),   TypeCode.Char);
        AreSame(Type.GetTypeCode(NoInline(typeof(IntPtrEnum))), TypeCode.Object);

        AreSame(Type.GetTypeCode(__reftype(__makeref(_varInt))),    TypeCode.Int32);
        AreSame(Type.GetTypeCode(__reftype(__makeref(_varObject))), TypeCode.Object);

        return 100;
    }

    private static int _varInt = 42;
    private static object _varObject = new object();

    public enum SByteEnum : sbyte {}
    public enum ByteEnum : byte {}
    public enum ShortEnum : short {}
    public enum UShortEnum : ushort {}
    public enum IntEnum {}
    public enum UIntEnum : uint {}
    public enum LongEnum : long {}
    public enum ULongEnum : ulong {}

    public class GenericEnumClass<T> where T : Enum
    {
        public T field;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type NoInline(Type type) => type;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AreSame(TypeCode a, TypeCode b, [CallerLineNumber] int line = 0)
    {
        if (a != b)
        {
            throw new InvalidOperationException($"Invalid TypeCode, expected {b}, got {a} at line {line}");
        }
    }
}
