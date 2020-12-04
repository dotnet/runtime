// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

public partial class Program
{
    public static void TestIsAssignableFrom()
    {
        // Primitive types
        IsTrue (typeof(void).IsAssignableFrom(typeof(void)));
        IsTrue (typeof(byte).IsAssignableFrom(typeof(byte)));
        IsTrue (typeof(int).IsAssignableFrom(typeof(int)));
        IsTrue (typeof(float).IsAssignableFrom(typeof(float)));
        IsTrue (typeof(double).IsAssignableFrom(typeof(double)));
        IsTrue (typeof(byte*).IsAssignableFrom(typeof(byte*)));
        IsTrue (typeof(sbyte*).IsAssignableFrom(typeof(byte*)));
        IsTrue (typeof(void*).IsAssignableFrom(typeof(void*)));
        IsTrue (typeof(byte**).IsAssignableFrom(typeof(byte**)));
        IsFalse(typeof(byte).IsAssignableFrom(typeof(sbyte)));
        IsFalse(typeof(sbyte).IsAssignableFrom(typeof(byte)));
        IsFalse(typeof(int).IsAssignableFrom(typeof(long)));
        IsFalse(typeof(int).IsAssignableFrom(typeof(void)));
        IsFalse(typeof(void).IsAssignableFrom(typeof(long)));
        IsFalse(typeof(long).IsAssignableFrom(typeof(int)));
        IsFalse(typeof(float).IsAssignableFrom(typeof(double)));
        IsFalse(typeof(double).IsAssignableFrom(typeof(float)));
        IsFalse(typeof(double).IsAssignableFrom(typeof(long)));
        IsFalse(typeof(int).IsAssignableFrom(typeof(float)));
        IsFalse(typeof(sbyte*).IsAssignableFrom(typeof(ulong*)));
        IsFalse(typeof(sbyte*).IsAssignableFrom(typeof(void*)));
        IsFalse(typeof(void*).IsAssignableFrom(typeof(ulong*)));
        IsFalse(typeof(sbyte*).IsAssignableFrom(typeof(IntPtr)));
        IsFalse(typeof(IntPtr).IsAssignableFrom(typeof(sbyte*)));
        IsFalse(typeof(byte**).IsAssignableFrom(typeof(byte*)));
        IsFalse(typeof(byte*).IsAssignableFrom(typeof(byte**)));

        // Nullable
        IsTrue (typeof(int?).IsAssignableFrom(typeof(int)));
        IsTrue (typeof(int?).IsAssignableFrom(typeof(int?)));
        IsTrue (typeof(GenericStruct1<int>?).IsAssignableFrom(typeof(GenericStruct1<int>?)));
        IsTrue (typeof(GenericStruct1<string>?).IsAssignableFrom(typeof(GenericStruct1<string>?)));
        IsTrue (typeof(SimpleEnum_int?).IsAssignableFrom(typeof(SimpleEnum_int?)));
        IsFalse(typeof(int).IsAssignableFrom(typeof(int?)));
        IsFalse(typeof(uint?).IsAssignableFrom(typeof(int?)));
        IsFalse(typeof(int?).IsAssignableFrom(typeof(uint?)));
        IsFalse(typeof(SimpleEnum_uint?).IsAssignableFrom(typeof(SimpleEnum_int?)));
        IsFalse(typeof(SimpleEnum_int?).IsAssignableFrom(typeof(SimpleEnum_uint?)));
        IsFalse(typeof(GenericStruct1<int>?).IsAssignableFrom(typeof(GenericStruct1<uint>?)));

        // Enums
        IsTrue (typeof(SimpleEnum_int).IsAssignableFrom(typeof(SimpleEnum_int)));
        IsTrue (typeof(SimpleEnum_int?).IsAssignableFrom(typeof(SimpleEnum_int?)));
        IsTrue (typeof(SimpleEnum_uint).IsAssignableFrom(typeof(SimpleEnum_uint)));
        IsTrue (typeof(SimpleEnum_byte).IsAssignableFrom(typeof(SimpleEnum_byte)));
        IsTrue (typeof(ValueType).IsAssignableFrom(typeof(SimpleEnum_uint)));
        IsFalse(typeof(SimpleEnum_int).IsAssignableFrom(typeof(SimpleEnum_uint)));
        IsFalse(typeof(SimpleEnum_uint).IsAssignableFrom(typeof(SimpleEnum_int)));
        IsFalse(typeof(SimpleEnum_uint).IsAssignableFrom(typeof(SimpleEnum_byte)));
        IsFalse(typeof(SimpleEnum_byte).IsAssignableFrom(typeof(SimpleEnum_uint)));
        IsFalse(typeof(SimpleEnum_byte).IsAssignableFrom(typeof(byte)));
        IsFalse(typeof(byte).IsAssignableFrom(typeof(SimpleEnum_byte)));
        IsFalse(typeof(SimpleEnum_int).IsAssignableFrom(typeof(int)));
        IsFalse(typeof(int).IsAssignableFrom(typeof(SimpleEnum_int)));
        IsFalse(typeof(SimpleEnum_uint).IsAssignableFrom(typeof(float)));
        IsFalse(typeof(float).IsAssignableFrom(typeof(SimpleEnum_uint)));
        IsFalse(typeof(SimpleEnum_uint).IsAssignableFrom(typeof(ValueType)));

        // Covariance/Contravariance 
        IsTrue (typeof(IEnumerable<object>).IsAssignableFrom(typeof(List<string>)));
        IsTrue (typeof(IEnumerable<ClassA>).IsAssignableFrom(typeof(List<ClassB>)));
        IsTrue (typeof(IEnumerable<ClassA>).IsAssignableFrom(typeof(IList<ClassB>)));
        IsTrue (typeof(IEnumerable<ClassA>).IsAssignableFrom(typeof(IList<ClassD>)));
        IsTrue (typeof(IEnumerable<ClassA>).IsAssignableFrom(typeof(IList<ClassA>)));
        IsTrue (typeof(Action<string>).IsAssignableFrom(typeof(Action<object>)));
        IsTrue (typeof(object[]).IsAssignableFrom(typeof(string[])));
        IsTrue (typeof(object[,]).IsAssignableFrom(typeof(string[,])));
        IsTrue (typeof(SimpleEnum_int[,]).IsAssignableFrom(typeof(SimpleEnum_uint[,])));
        IsFalse(typeof(string[,]).IsAssignableFrom(typeof(object[,])));
        IsFalse(typeof(object[,]).IsAssignableFrom(typeof(string[,,])));
        IsFalse(typeof(IDictionary<ClassA, int>).IsAssignableFrom(typeof(IDictionary<ClassB, int>)));
        IsFalse(typeof(IDictionary<ClassA, int>).IsAssignableFrom(typeof(Dictionary<ClassB, int>)));
        IsFalse(typeof(Action<object>).IsAssignableFrom(typeof(Action<string>)));
        IsFalse(typeof(Action<object>).IsAssignableFrom(typeof(Action<Guid>)));
        IsFalse(typeof(List<string>).IsAssignableFrom(typeof(IEnumerable<object>)));
        IsFalse(typeof(Action<Guid>).IsAssignableFrom(typeof(Action<object>)));
        IsFalse(typeof(List<ClassB>).IsAssignableFrom(typeof(IEnumerable<ClassA>)));
        IsFalse(typeof(IList<ClassB>).IsAssignableFrom(typeof(IEnumerable<ClassA>)));
        IsFalse(typeof(IList<ClassD>).IsAssignableFrom(typeof(IEnumerable<ClassA>)));
        IsFalse(typeof(IList<ClassA>).IsAssignableFrom(typeof(IEnumerable<ClassA>)));

        // Arrays
        IsTrue(typeof(byte[]).IsAssignableFrom(typeof(sbyte[])));
        IsTrue(typeof(sbyte[]).IsAssignableFrom(typeof(byte[])));
        IsTrue(typeof(short[]).IsAssignableFrom(typeof(ushort[])));
        IsTrue(typeof(ushort[]).IsAssignableFrom(typeof(short[])));
        IsTrue(typeof(int[]).IsAssignableFrom(typeof(uint[])));
        IsTrue(typeof(uint[]).IsAssignableFrom(typeof(int[])));
        IsTrue(typeof(long[]).IsAssignableFrom(typeof(ulong[])));
        IsTrue(typeof(ulong[]).IsAssignableFrom(typeof(long[])));
        IsTrue(typeof(long[,]).IsAssignableFrom(typeof(ulong[,])));
        IsTrue(typeof(ulong[,,]).IsAssignableFrom(typeof(long[,,])));
        IsTrue(typeof(Struct1[]).IsAssignableFrom(typeof(Struct1[])));
        IsFalse(typeof(int[]).IsAssignableFrom(typeof(byte[])));
        IsFalse(typeof(int[]).IsAssignableFrom(typeof(sbyte[])));
        IsFalse(typeof(int[]).IsAssignableFrom(typeof(short[])));
        IsFalse(typeof(int[]).IsAssignableFrom(typeof(ushort[])));
        IsFalse(typeof(int[]).IsAssignableFrom(typeof(float[])));
        IsFalse(typeof(int[]).IsAssignableFrom(typeof(double[])));
        IsFalse(typeof(long[]).IsAssignableFrom(typeof(double[])));
        IsFalse(typeof(Struct1[]).IsAssignableFrom(typeof(Struct2[])));
        IsFalse(typeof(Struct1[]).IsAssignableFrom(typeof(GenericStruct1<int>[])));
        IsFalse(typeof(GenericStruct1<uint>[]).IsAssignableFrom(typeof(GenericStruct1<int>[])));

        // Misc
        IsTrue (typeof(object).IsAssignableFrom(typeof(byte)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(int)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(float)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(SimpleEnum_uint)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(IDisposable)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(IDictionary<string, string>)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(List<int>)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(List<>)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(Action<>)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(Action<int>)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(Vector128<float>)));
        IsTrue (typeof(object).IsAssignableFrom(typeof(Vector256<int>)));
        IsTrue (typeof(ClassA).IsAssignableFrom(typeof(ClassA)));
        IsTrue (typeof(ClassA).IsAssignableFrom(typeof(ClassB)));
        IsTrue (typeof(ClassA).IsAssignableFrom(typeof(ClassC)));
        IsTrue (typeof(decimal).IsAssignableFrom(typeof(decimal)));
        IsTrue (typeof(Struct1).IsAssignableFrom(typeof(Struct1)));
        IsTrue (typeof(IDisposable).IsAssignableFrom(typeof(Struct3)));
        IsTrue (typeof(Dictionary<,>).IsAssignableFrom(typeof(Dictionary<,>)));
        IsTrue (typeof(IDictionary<,>).IsAssignableFrom(typeof(IDictionary<,>)));
        IsTrue (typeof(GenericStruct1<>).IsAssignableFrom(typeof(GenericStruct1<>)));
        IsTrue (typeof(GenericStruct1<int>).IsAssignableFrom(typeof(GenericStruct1<int>)));
        IsTrue (typeof(GenericStruct1<string>).IsAssignableFrom(typeof(GenericStruct1<string>)));
        IsFalse(typeof(byte).IsAssignableFrom(typeof(IDisposable)));
        IsFalse(typeof(IDisposable).IsAssignableFrom(typeof(IEnumerable)));
        IsFalse(typeof(IDictionary<string, string>).IsAssignableFrom(typeof(IDictionary<string, int>)));
        IsFalse(typeof(List<int>).IsAssignableFrom(typeof(IList<int>)));
        IsFalse(typeof(List<>).IsAssignableFrom(typeof(List<IDisposable>)));
        IsFalse(typeof(Action<>).IsAssignableFrom(typeof(Action<int>)));
        IsFalse(typeof(Action<>).IsAssignableFrom(typeof(Func<int>)));
        IsFalse(typeof(Action).IsAssignableFrom(typeof(CustomAction)));
        IsFalse(typeof(Action<int>).IsAssignableFrom(typeof(void)));
        IsFalse(typeof(ClassB).IsAssignableFrom(typeof(ClassD)));
        IsFalse(typeof(Dictionary<,>).IsAssignableFrom(typeof(Dictionary<int,int>)));
        IsFalse(typeof(GenericStruct1<ClassA>).IsAssignableFrom(typeof(GenericStruct1<ClassB>)));
        IsFalse(typeof(Struct1).IsAssignableFrom(typeof(Struct2)));
        IsFalse(typeof(GenericStruct1<>).IsAssignableFrom(typeof(GenericStruct2<>)));
        IsFalse(typeof(GenericStruct1<int>).IsAssignableFrom(typeof(GenericStruct2<int>)));
        IsFalse(typeof(object).IsAssignableFrom(typeof(byte*)));
        IsFalse(typeof(object).IsAssignableFrom(typeof(byte**)));
        IsFalse(typeof(Vector128<double>).IsAssignableFrom(typeof(Vector128<float>)));
        IsFalse(typeof(Vector128<float>).IsAssignableFrom(typeof(Vector128<int>)));
        IsFalse(typeof(Vector128<int>).IsAssignableFrom(typeof(Vector128<float>)));
        IsFalse(typeof(Vector4).IsAssignableFrom(typeof(Vector128<float>)));
        IsFalse(typeof(Vector128<float>).IsAssignableFrom(typeof(Vector4)));
        IsFalse(typeof(Vector128<float>).IsAssignableFrom(typeof(Vector<float>)));
        IsFalse(typeof(Vector256<float>).IsAssignableFrom(typeof(Vector<float>)));

        // System.__Canon
        IsTrue (IsAssignableFrom<KeyValuePair<IDisposable, IDisposable>, KeyValuePair<IDisposable, IDisposable>>());
        IsTrue (IsAssignableFrom<KeyValuePair<IDisposable, object>, KeyValuePair<IDisposable, object>>());
        IsTrue (IsAssignableFrom<IDictionary<IDisposable, IDisposable>, IDictionary<IDisposable, IDisposable>>());
        IsTrue (IsAssignableFrom<IDictionary<IDisposable, object>, IDictionary<IDisposable, object>>());
        IsTrue (IsAssignableFrom<Dictionary<IDisposable, IDisposable>, Dictionary<IDisposable, IDisposable>>());
        IsTrue (IsAssignableFrom<Dictionary<IDisposable, object>, Dictionary<IDisposable, object>>());
        IsTrue (IsAssignableFrom<KeyValuePair<int, int>, KeyValuePair<int, int>>());
        IsTrue (IsAssignableFrom<KeyValuePair<IEnumerable<int>, IEnumerable<int>>, KeyValuePair<IEnumerable<int>, IEnumerable<int>>>());
        IsFalse(IsAssignableFrom<KeyValuePair<IDisposable, IDisposable>, KeyValuePair<IDisposable, object>>());
        IsFalse(IsAssignableFrom<KeyValuePair<IDisposable, int>, KeyValuePair<IDisposable, object>>());
        IsFalse(IsAssignableFrom<IDictionary<IDisposable, IDisposable>, IDictionary<IDisposable, object>>());
        IsFalse(IsAssignableFrom<IDictionary<IDisposable, int>, IDictionary<IDisposable, object>>());
        IsFalse(IsAssignableFrom<Dictionary<IDisposable, IDisposable>, Dictionary<IDisposable, object>>());
        IsFalse(IsAssignableFrom<Dictionary<IDisposable, int>, Dictionary<IDisposable, object>>());
        IsFalse(IsAssignableFrom<KeyValuePair<int, int>, KeyValuePair<int, object>>());
        IsFalse(IsAssignableFrom<KeyValuePair<IEnumerable<int>, IEnumerable<int>>, KeyValuePair<IEnumerable<int>, IEnumerable<uint>>>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsAssignableFrom<TTo, TTFrom>() => typeof(TTo).IsAssignableFrom(typeof(TTFrom));
}

public struct Struct1
{
    public int field1;
}

public struct Struct2
{
    public int field1;
}

public struct Struct3 : IDisposable
{
    public int field1;
    public void Dispose(){}
}

public struct GenericStruct1<T>
{
    public T field;
}

public struct GenericStruct2<T>
{
    public T field;
}

public enum SimpleEnum_byte : byte
{
    A, B, C
}

public enum SimpleEnum_int : int
{
    A,B,C
}

public enum SimpleEnum_uint : uint
{
    D,E
}

public class ClassA
{
}

public class ClassB : ClassA
{
}

public class ClassC : ClassB
{
}

public class ClassD : ClassA
{
}

public delegate void CustomAction();
