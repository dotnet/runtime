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
    public static void TestIsAssignableTo()
    {
        // Primitive types
        IsTrue (typeof(void).IsAssignableTo(typeof(void)));
        IsTrue (typeof(byte).IsAssignableTo(typeof(byte)));
        IsTrue (typeof(int).IsAssignableTo(typeof(int)));
        IsTrue (typeof(float).IsAssignableTo(typeof(float)));
        IsTrue (typeof(double).IsAssignableTo(typeof(double)));
        IsTrue (typeof(byte*).IsAssignableTo(typeof(byte*)));
        IsTrue (typeof(byte*).IsAssignableTo(typeof(sbyte*)));
        IsTrue (typeof(void*).IsAssignableTo(typeof(void*)));
        IsTrue (typeof(byte**).IsAssignableTo(typeof(byte**)));
        IsFalse(typeof(sbyte).IsAssignableTo(typeof(byte)));
        IsFalse(typeof(byte).IsAssignableTo(typeof(sbyte)));
        IsFalse(typeof(long).IsAssignableTo(typeof(int)));
        IsFalse(typeof(void).IsAssignableTo(typeof(int)));
        IsFalse(typeof(long).IsAssignableTo(typeof(void)));
        IsFalse(typeof(int).IsAssignableTo(typeof(long)));
        IsFalse(typeof(double).IsAssignableTo(typeof(float)));
        IsFalse(typeof(float).IsAssignableTo(typeof(double)));
        IsFalse(typeof(long).IsAssignableTo(typeof(double)));
        IsFalse(typeof(float).IsAssignableTo(typeof(int)));
        IsFalse(typeof(ulong*).IsAssignableTo(typeof(sbyte*)));
        IsFalse(typeof(void*).IsAssignableTo(typeof(sbyte*)));
        IsFalse(typeof(ulong*).IsAssignableTo(typeof(void*)));
        IsFalse(typeof(IntPtr).IsAssignableTo(typeof(sbyte*)));
        IsFalse(typeof(sbyte*).IsAssignableTo(typeof(IntPtr)));
        IsFalse(typeof(byte*).IsAssignableTo(typeof(byte**)));
        IsFalse(typeof(byte**).IsAssignableTo(typeof(byte*)));

        // Nullable
        IsTrue (typeof(int).IsAssignableTo(typeof(int?)));
        IsTrue (typeof(int?).IsAssignableTo(typeof(int?)));
        IsTrue (typeof(GenericStruct1<int>?).IsAssignableTo(typeof(GenericStruct1<int>?)));
        IsTrue (typeof(GenericStruct1<string>?).IsAssignableTo(typeof(GenericStruct1<string>?)));
        IsTrue (typeof(SimpleEnum_int?).IsAssignableTo(typeof(SimpleEnum_int?)));
        IsFalse(typeof(int?).IsAssignableTo(typeof(int)));
        IsFalse(typeof(int?).IsAssignableTo(typeof(uint?)));
        IsFalse(typeof(uint?).IsAssignableTo(typeof(int?)));
        IsFalse(typeof(SimpleEnum_int?).IsAssignableTo(typeof(SimpleEnum_uint?)));
        IsFalse(typeof(SimpleEnum_uint?).IsAssignableTo(typeof(SimpleEnum_int?)));
        IsFalse(typeof(GenericStruct1<uint>?).IsAssignableTo(typeof(GenericStruct1<int>?)));

        // Enums
        IsTrue (typeof(SimpleEnum_int).IsAssignableTo(typeof(SimpleEnum_int)));
        IsTrue (typeof(SimpleEnum_int?).IsAssignableTo(typeof(SimpleEnum_int?)));
        IsTrue (typeof(SimpleEnum_uint).IsAssignableTo(typeof(SimpleEnum_uint)));
        IsTrue (typeof(SimpleEnum_byte).IsAssignableTo(typeof(SimpleEnum_byte)));
        IsTrue (typeof(SimpleEnum_uint).IsAssignableTo(typeof(ValueType)));
        IsFalse(typeof(SimpleEnum_uint).IsAssignableTo(typeof(SimpleEnum_int)));
        IsFalse(typeof(SimpleEnum_int).IsAssignableTo(typeof(SimpleEnum_uint)));
        IsFalse(typeof(SimpleEnum_byte).IsAssignableTo(typeof(SimpleEnum_uint)));
        IsFalse(typeof(SimpleEnum_uint).IsAssignableTo(typeof(SimpleEnum_byte)));
        IsFalse(typeof(byte).IsAssignableTo(typeof(SimpleEnum_byte)));
        IsFalse(typeof(SimpleEnum_byte).IsAssignableTo(typeof(byte)));
        IsFalse(typeof(int).IsAssignableTo(typeof(SimpleEnum_int)));
        IsFalse(typeof(SimpleEnum_int).IsAssignableTo(typeof(int)));
        IsFalse(typeof(float).IsAssignableTo(typeof(SimpleEnum_uint)));
        IsFalse(typeof(SimpleEnum_uint).IsAssignableTo(typeof(float)));
        IsFalse(typeof(ValueType).IsAssignableTo(typeof(SimpleEnum_uint)));

        // Covariance/Contravariance 
        IsTrue (typeof(List<string>).IsAssignableTo(typeof(IEnumerable<object>)));
        IsTrue (typeof(List<ClassB>).IsAssignableTo(typeof(IEnumerable<ClassA>)));
        IsTrue (typeof(IList<ClassB>).IsAssignableTo(typeof(IEnumerable<ClassA>)));
        IsTrue (typeof(IList<ClassD>).IsAssignableTo(typeof(IEnumerable<ClassA>)));
        IsTrue (typeof(IList<ClassA>).IsAssignableTo(typeof(IEnumerable<ClassA>)));
        IsTrue (typeof(Action<object>).IsAssignableTo(typeof(Action<string>)));
        IsTrue (typeof(string[]).IsAssignableTo(typeof(object[])));
        IsTrue (typeof(string[,]).IsAssignableTo(typeof(object[,])));
        IsTrue (typeof(SimpleEnum_uint[,]).IsAssignableTo(typeof(SimpleEnum_int[,])));
        IsFalse(typeof(object[,]).IsAssignableTo(typeof(string[,])));
        IsFalse(typeof(string[,,]).IsAssignableTo(typeof(object[,])));
        IsFalse(typeof(IDictionary<ClassB, int>).IsAssignableTo(typeof(IDictionary<ClassA, int>)));
        IsFalse(typeof(Dictionary<ClassB, int>).IsAssignableTo(typeof(IDictionary<ClassA, int>)));
        IsFalse(typeof(Action<string>).IsAssignableTo(typeof(Action<object>)));
        IsFalse(typeof(Action<Guid>).IsAssignableTo(typeof(Action<object>)));
        IsFalse(typeof(IEnumerable<object>).IsAssignableTo(typeof(List<string>)));
        IsFalse(typeof(Action<object>).IsAssignableTo(typeof(Action<Guid>)));
        IsFalse(typeof(IEnumerable<ClassA>).IsAssignableTo(typeof(List<ClassB>)));
        IsFalse(typeof(IEnumerable<ClassA>).IsAssignableTo(typeof(IList<ClassB>)));
        IsFalse(typeof(IEnumerable<ClassA>).IsAssignableTo(typeof(IList<ClassD>)));
        IsFalse(typeof(IEnumerable<ClassA>).IsAssignableTo(typeof(IList<ClassA>)));

        // Arrays
        IsTrue(typeof(sbyte[]).IsAssignableTo(typeof(byte[])));
        IsTrue(typeof(byte[]).IsAssignableTo(typeof(sbyte[])));
        IsTrue(typeof(ushort[]).IsAssignableTo(typeof(short[])));
        IsTrue(typeof(short[]).IsAssignableTo(typeof(ushort[])));
        IsTrue(typeof(uint[]).IsAssignableTo(typeof(int[])));
        IsTrue(typeof(int[]).IsAssignableTo(typeof(uint[])));
        IsTrue(typeof(ulong[]).IsAssignableTo(typeof(long[])));
        IsTrue(typeof(long[]).IsAssignableTo(typeof(ulong[])));
        IsTrue(typeof(ulong[,]).IsAssignableTo(typeof(long[,])));
        IsTrue(typeof(long[,,]).IsAssignableTo(typeof(ulong[,,])));
        IsTrue(typeof(Struct1[]).IsAssignableTo(typeof(Struct1[])));
        IsFalse(typeof(byte[]).IsAssignableTo(typeof(int[])));
        IsFalse(typeof(sbyte[]).IsAssignableTo(typeof(int[])));
        IsFalse(typeof(short[]).IsAssignableTo(typeof(int[])));
        IsFalse(typeof(ushort[]).IsAssignableTo(typeof(int[])));
        IsFalse(typeof(float[]).IsAssignableTo(typeof(int[])));
        IsFalse(typeof(double[]).IsAssignableTo(typeof(int[])));
        IsFalse(typeof(double[]).IsAssignableTo(typeof(long[])));
        IsFalse(typeof(Struct2[]).IsAssignableTo(typeof(Struct1[])));
        IsFalse(typeof(GenericStruct1<int>[]).IsAssignableTo(typeof(Struct1[])));
        IsFalse(typeof(GenericStruct1<int>[]).IsAssignableTo(typeof(GenericStruct1<uint>[])));

        // Misc
        IsTrue (typeof(byte).IsAssignableTo(typeof(object)));
        IsTrue (typeof(int).IsAssignableTo(typeof(object)));
        IsTrue (typeof(float).IsAssignableTo(typeof(object)));
        IsTrue (typeof(SimpleEnum_uint).IsAssignableTo(typeof(object)));
        IsTrue (typeof(IDisposable).IsAssignableTo(typeof(object)));
        IsTrue (typeof(IDictionary<string, string>).IsAssignableTo(typeof(object)));
        IsTrue (typeof(List<int>).IsAssignableTo(typeof(object)));
        IsTrue (typeof(List<>).IsAssignableTo(typeof(object)));
        IsTrue (typeof(Action<>).IsAssignableTo(typeof(object)));
        IsTrue (typeof(Action<int>).IsAssignableTo(typeof(object)));
        IsTrue (typeof(Vector128<float>).IsAssignableTo(typeof(object)));
        IsTrue (typeof(Vector256<int>).IsAssignableTo(typeof(object)));
        IsTrue (typeof(ClassA).IsAssignableTo(typeof(ClassA)));
        IsTrue (typeof(ClassB).IsAssignableTo(typeof(ClassA)));
        IsTrue (typeof(ClassC).IsAssignableTo(typeof(ClassA)));
        IsTrue (typeof(decimal).IsAssignableTo(typeof(decimal)));
        IsTrue (typeof(Struct1).IsAssignableTo(typeof(Struct1)));
        IsTrue (typeof(Struct3).IsAssignableTo(typeof(IDisposable)));
        IsTrue (typeof(Dictionary<,>).IsAssignableTo(typeof(Dictionary<,>)));
        IsTrue (typeof(IDictionary<,>).IsAssignableTo(typeof(IDictionary<,>)));
        IsTrue (typeof(GenericStruct1<>).IsAssignableTo(typeof(GenericStruct1<>)));
        IsTrue (typeof(GenericStruct1<int>).IsAssignableTo(typeof(GenericStruct1<int>)));
        IsTrue (typeof(GenericStruct1<string>).IsAssignableTo(typeof(GenericStruct1<string>)));
        IsFalse(typeof(IDisposable).IsAssignableTo(typeof(byte)));
        IsFalse(typeof(IEnumerable).IsAssignableTo(typeof(IDisposable)));
        IsFalse(typeof(IDictionary<string, int>).IsAssignableTo(typeof(IDictionary<string, string>)));
        IsFalse(typeof(IList<int>).IsAssignableTo(typeof(List<int>)));
        IsFalse(typeof(List<IDisposable>).IsAssignableTo(typeof(List<>)));
        IsFalse(typeof(Action<int>).IsAssignableTo(typeof(Action<>)));
        IsFalse(typeof(Func<int>).IsAssignableTo(typeof(Action<>)));
        IsFalse(typeof(CustomAction).IsAssignableTo(typeof(Action)));
        IsFalse(typeof(void).IsAssignableTo(typeof(Action<int>)));
        IsFalse(typeof(ClassD).IsAssignableTo(typeof(ClassB)));
        IsFalse(typeof(Dictionary<int,int>).IsAssignableTo(typeof(Dictionary<,>)));
        IsFalse(typeof(GenericStruct1<ClassB>).IsAssignableTo(typeof(GenericStruct1<ClassA>)));
        IsFalse(typeof(Struct2).IsAssignableTo(typeof(Struct1)));
        IsFalse(typeof(GenericStruct2<>).IsAssignableTo(typeof(GenericStruct1<>)));
        IsFalse(typeof(GenericStruct2<int>).IsAssignableTo(typeof(GenericStruct1<int>)));
        IsFalse(typeof(byte*).IsAssignableTo(typeof(object)));
        IsFalse(typeof(byte**).IsAssignableTo(typeof(object)));
        IsFalse(typeof(Vector128<float>).IsAssignableTo(typeof(Vector128<double>)));
        IsFalse(typeof(Vector128<int>).IsAssignableTo(typeof(Vector128<float>)));
        IsFalse(typeof(Vector128<float>).IsAssignableTo(typeof(Vector128<int>)));
        IsFalse(typeof(Vector128<float>).IsAssignableTo(typeof(Vector4)));
        IsFalse(typeof(Vector4).IsAssignableTo(typeof(Vector128<float>)));
        IsFalse(typeof(Vector<float>).IsAssignableTo(typeof(Vector128<float>)));
        IsFalse(typeof(Vector<float>).IsAssignableTo(typeof(Vector256<float>)));

        // System.__Canon
        IsTrue (IsAssignableTo<KeyValuePair<IDisposable, IDisposable>, KeyValuePair<IDisposable, IDisposable>>());
        IsTrue (IsAssignableTo<KeyValuePair<IDisposable, object>, KeyValuePair<IDisposable, object>>());
        IsTrue (IsAssignableTo<IDictionary<IDisposable, IDisposable>, IDictionary<IDisposable, IDisposable>>());
        IsTrue (IsAssignableTo<IDictionary<IDisposable, object>, IDictionary<IDisposable, object>>());
        IsTrue (IsAssignableTo<Dictionary<IDisposable, IDisposable>, Dictionary<IDisposable, IDisposable>>());
        IsTrue (IsAssignableTo<Dictionary<IDisposable, object>, Dictionary<IDisposable, object>>());
        IsTrue (IsAssignableTo<KeyValuePair<int, int>, KeyValuePair<int, int>>());
        IsTrue (IsAssignableTo<KeyValuePair<IEnumerable<int>, IEnumerable<int>>, KeyValuePair<IEnumerable<int>, IEnumerable<int>>>());
        IsFalse(IsAssignableTo<KeyValuePair<IDisposable, object>, KeyValuePair<IDisposable, IDisposable>>());
        IsFalse(IsAssignableTo<KeyValuePair<IDisposable, object>, KeyValuePair<IDisposable, int>>());
        IsFalse(IsAssignableTo<IDictionary<IDisposable, object>, IDictionary<IDisposable, IDisposable>>());
        IsFalse(IsAssignableTo<IDictionary<IDisposable, object>, IDictionary<IDisposable, int>>());
        IsFalse(IsAssignableTo<Dictionary<IDisposable, object>, Dictionary<IDisposable, IDisposable>>());
        IsFalse(IsAssignableTo<Dictionary<IDisposable, object>, Dictionary<IDisposable, int>>());
        IsFalse(IsAssignableTo<KeyValuePair<int, object>, KeyValuePair<int, int>>());
        IsFalse(IsAssignableTo<KeyValuePair<IEnumerable<int>, IEnumerable<uint>>, KeyValuePair<IEnumerable<int>, IEnumerable<int>>>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsAssignableTo<TTFrom, TTo>() => typeof(TTFrom).IsAssignableTo(typeof(TTo));
}
