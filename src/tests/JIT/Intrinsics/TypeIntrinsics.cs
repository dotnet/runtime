// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public partial class Program
{
    private static int _errors = 0;

    [Fact]
    public static int TestEntryPoint()
    {
        IsTrue (typeof(byte).IsValueType);
        IsTrue (typeof(int).IsValueType);
        IsTrue (typeof(int?).IsValueType);
        IsFalse(typeof(int*).IsValueType);
        IsFalse(typeof(int**).IsValueType);
        IsFalse(typeof(void*).IsValueType);
        IsFalse(typeof(void**).IsValueType);
        IsFalse(typeof(GenericStruct<int>*).IsValueType);
        IsTrue (typeof(IntPtr).IsValueType);
        IsTrue (typeof(decimal).IsValueType);
        IsTrue (typeof(double).IsValueType);
        IsFalse(typeof(string).IsValueType);
        IsFalse(typeof(object).IsValueType);
        IsFalse(typeof(object[]).IsValueType);
        IsFalse(typeof(int[]).IsValueType);
        IsFalse(typeof(int[,,]).IsValueType);
        IsFalse(typeof(IEnumerable<int>).IsValueType);
        IsFalse(typeof(Action<int>).IsValueType);
        IsTrue (typeof(GenericStruct<int>).IsValueType);
        IsTrue (typeof(GenericStruct<string>).IsValueType);
        IsTrue (typeof(GenericStruct<string>).IsValueType);
        IsTrue (typeof(KeyValuePair<int, string>).IsValueType);
        IsTrue (typeof(KeyValuePair<Program, string>).IsValueType);
        IsTrue (typeof(SimpleEnum).IsValueType);
        IsTrue (typeof(void).IsValueType);
        IsFalse(typeof(ValueType).IsValueType);
        IsFalse(typeof(List<>).IsValueType);
        IsFalse(typeof(IDictionary<,>).IsValueType);
        IsTrue (typeof(Vector128<>).IsValueType);
        IsTrue (typeof(Vector128<byte>).IsValueType);

        // Test __Canon
        IsFalse(IsValueType<IEnumerable<int>>());
        IsFalse(IsValueType<IEnumerable<string>>());
        IsFalse(IsValueType<IEnumerable<IDisposable>>());
        IsFalse(IsValueType<IDictionary<int, string>>());
        IsFalse(IsValueType<IDictionary<IConvertible, IComparer<int>>>());
        IsFalse(IsValueType<Dictionary<int, int>>());
        IsFalse(IsValueType<Dictionary<string, IEnumerable>>());

        // Test `x.GetType().IsX`
        IsTrue (IsValueType<int>(42));
        IsTrue (IsValueType<int?>(new Nullable<int>(42)));
        IsTrue (IsValueType<decimal>(42M));
        IsFalse(IsValueType<string>("42"));
        IsFalse(IsValueType<object>(new object()));
        IsFalse(IsValueType<IEnumerable<int>>(new int[10]));
        IsFalse(IsValueType<Action<int>>(_ => { }));
        IsTrue (IsValueType<GenericStruct<int>>(default));
        IsTrue (IsValueType<GenericStruct<string>>(default));
        IsTrue (IsValueType(SimpleEnum.B));
        IsTrue (IsValueType(CreateDynamic1()));
        IsFalse(IsValueType(CreateDynamic2()));

        IsTrue (IsValueTypeObj(42));
        IsTrue (IsValueTypeObj(new Nullable<int>(42)));
        IsTrue (IsValueTypeObj(42M));
        IsFalse(IsValueTypeObj("42"));
        IsFalse(IsValueTypeObj(new object()));
        IsFalse(IsValueTypeObj(new int[10]));
        IsFalse(IsValueTypeObj((Action<int>)(_ => { })));
        IsTrue (IsValueTypeObj(new GenericStruct<int>()));
        IsTrue (IsValueTypeObj(new GenericStruct<string>()));
        IsTrue (IsValueTypeObj(SimpleEnum.B));
        IsTrue (IsValueTypeObj(CreateDynamic1()));
        IsFalse(IsValueTypeObj(CreateDynamic2()));

        IsTrue (IsValueTypeRef(ref _varInt));
        IsTrue (IsValueTypeRef(ref _varNullableInt));
        IsTrue (IsValueTypeRef(ref _varDecimal));
        IsFalse(IsValueTypeRef(ref _varString));
        IsFalse(IsValueTypeRef(ref _varObject));
        IsFalse(IsValueTypeRef(ref _varArrayOfInt));
        IsFalse(IsValueTypeRef(ref _varAction));
        IsTrue (IsValueTypeRef(ref _varGenericStructInt));
        IsTrue (IsValueTypeRef(ref _varGenericStructStr));
        IsTrue (IsValueTypeRef(ref _varEnum));

        // test __reftype
        IsTrue (__reftype(__makeref(_varInt)).IsValueType);
        IsFalse(__reftype(__makeref(_varObject)).IsValueType);

        ThrowsNRE(() => { IsValueType(_varNullableIntNull); });
        ThrowsNRE(() => { IsValueType(_varStringNull); });
        ThrowsNRE(() => { IsValueTypeRef(ref _varNullableIntNull); });
        ThrowsNRE(() => { IsValueTypeRef(ref _varStringNull); });
        ThrowsNRE(() => { _ = Type.GetTypeFromHandle(default).IsValueType; });
        ThrowsNRE(() => { _ = Type.GetTypeFromHandle(new RuntimeTypeHandle()).IsValueType; });
        ThrowsNRE(() => { _ = __reftype(default).IsValueType; });

        TestIsAssignableFrom();
        TestIsAssignableTo();

        IsFalse(typeof(byte).IsEnum);
        IsFalse(typeof(int).IsEnum);
        IsFalse(typeof(int?).IsEnum);
        IsFalse(typeof(int*).IsEnum);
        IsFalse(typeof(nint).IsEnum);
        IsFalse(typeof(void).IsEnum);
        IsFalse(typeof(object).IsEnum);
        IsFalse(typeof(Enum).IsEnum);
        IsFalse(typeof(ValueType).IsEnum);
        IsFalse(typeof(GenericStruct<int>).IsEnum);
        IsFalse(typeof(SimpleStruct).IsEnum);
        IsTrue (typeof(SimpleEnum).IsEnum);
        IsTrue (typeof(CharEnum).IsEnum);
        IsTrue (typeof(BoolEnum).IsEnum);
        IsTrue (typeof(FloatEnum).IsEnum);
        IsTrue (typeof(DoubleEnum).IsEnum);
        IsTrue (typeof(IntPtrEnum).IsEnum);
        IsTrue (typeof(UIntPtrEnum).IsEnum);

        IsTrue(typeof(GenericEnumClass<>).GetGenericArguments()[0].IsEnum);

        GetEnumUnderlyingType.TestGetEnumUnderlyingType();

        IsPrimitiveTests();
        IsGenericTypeTests();
        GetGenericTypeDefinitionTests();

        return 100 + _errors;
    }

    private static void IsPrimitiveTests()
    {
        IsTrue(typeof(bool).IsPrimitive);
        IsTrue(typeof(char).IsPrimitive);
        IsTrue(typeof(sbyte).IsPrimitive);
        IsTrue(typeof(byte).IsPrimitive);
        IsTrue(typeof(short).IsPrimitive);
        IsTrue(typeof(ushort).IsPrimitive);
        IsTrue(typeof(int).IsPrimitive);
        IsTrue(typeof(uint).IsPrimitive);
        IsTrue(typeof(long).IsPrimitive);
        IsTrue(typeof(ulong).IsPrimitive);
        IsTrue(typeof(float).IsPrimitive);
        IsTrue(typeof(double).IsPrimitive);
        IsTrue(typeof(nint).IsPrimitive);
        IsTrue(typeof(nuint).IsPrimitive);
        IsTrue(typeof(IntPtr).IsPrimitive);
        IsTrue(typeof(UIntPtr).IsPrimitive);

        IsFalse(typeof(Enum).IsPrimitive);
        IsFalse(typeof(ValueType).IsPrimitive);
        IsFalse(typeof(SimpleEnum).IsPrimitive);
        IsFalse(typeof(IntPtrEnum).IsPrimitive);
        IsFalse(typeof(FloatEnum).IsPrimitive);
        IsFalse(typeof(SimpleEnum?).IsPrimitive);
        IsFalse(typeof(int?).IsPrimitive);
        IsFalse(typeof(IntPtr?).IsPrimitive);
        IsFalse(typeof(decimal).IsPrimitive);
        IsFalse(typeof(TimeSpan).IsPrimitive);
        IsFalse(typeof(DateTime).IsPrimitive);
        IsFalse(typeof(DateTimeOffset).IsPrimitive);
        IsFalse(typeof(Guid).IsPrimitive);
        IsFalse(typeof(Half).IsPrimitive);
        IsFalse(typeof(DateOnly).IsPrimitive);
        IsFalse(typeof(TimeOnly).IsPrimitive);
        IsFalse(typeof(Int128).IsPrimitive);
        IsFalse(typeof(UInt128).IsPrimitive);
        IsFalse(typeof(string).IsPrimitive);
        IsFalse(typeof(object).IsPrimitive);
        IsFalse(typeof(RuntimeArgumentHandle).IsPrimitive);
        IsFalse(typeof(int[]).IsPrimitive);
        IsFalse(typeof(int[,]).IsPrimitive);
        IsFalse(typeof(int*).IsPrimitive);
        IsFalse(typeof(void*).IsPrimitive);
        IsFalse(typeof(delegate*<int>).IsPrimitive);
        IsFalse(typeof(Nullable<>).IsPrimitive);
        IsFalse(typeof(Dictionary<,>).IsPrimitive);
    }

    private static void IsGenericTypeTests()
    {
        IsFalse(typeof(bool).IsGenericType);
        IsFalse(typeof(char).IsGenericType);
        IsFalse(typeof(sbyte).IsGenericType);
        IsFalse(typeof(byte).IsGenericType);
        IsFalse(typeof(short).IsGenericType);
        IsFalse(typeof(ushort).IsGenericType);
        IsFalse(typeof(int).IsGenericType);
        IsFalse(typeof(uint).IsGenericType);
        IsFalse(typeof(long).IsGenericType);
        IsFalse(typeof(ulong).IsGenericType);
        IsFalse(typeof(float).IsGenericType);
        IsFalse(typeof(double).IsGenericType);
        IsFalse(typeof(nint).IsGenericType);
        IsFalse(typeof(nuint).IsGenericType);
        IsFalse(typeof(IntPtr).IsGenericType);
        IsFalse(typeof(UIntPtr).IsGenericType);
        IsFalse(typeof(Enum).IsGenericType);
        IsFalse(typeof(ValueType).IsGenericType);
        IsFalse(typeof(SimpleEnum).IsGenericType);
        IsFalse(typeof(IntPtrEnum).IsGenericType);
        IsFalse(typeof(FloatEnum).IsGenericType);
        IsFalse(typeof(decimal).IsGenericType);
        IsFalse(typeof(TimeSpan).IsGenericType);
        IsFalse(typeof(DateTime).IsGenericType);
        IsFalse(typeof(DateTimeOffset).IsGenericType);
        IsFalse(typeof(Guid).IsGenericType);
        IsFalse(typeof(Half).IsGenericType);
        IsFalse(typeof(DateOnly).IsGenericType);
        IsFalse(typeof(TimeOnly).IsGenericType);
        IsFalse(typeof(Int128).IsGenericType);
        IsFalse(typeof(UInt128).IsGenericType);
        IsFalse(typeof(string).IsGenericType);
        IsFalse(typeof(object).IsGenericType);
        IsFalse(typeof(RuntimeArgumentHandle).IsGenericType);
        IsFalse(typeof(DerivedGenericSimpleClass).IsGenericType);
        IsFalse(typeof(int[]).IsGenericType);
        IsFalse(typeof(int[,]).IsGenericType);
        IsFalse(typeof(int*).IsGenericType);
        IsFalse(typeof(void*).IsGenericType);
        IsFalse(typeof(delegate*<int>).IsGenericType);
        IsFalse(new ClassUsingIsGenericTypeOnT<char>().IsGenericType());
        IsFalse(new ClassUsingIsGenericTypeOnT<string>().IsGenericType());
        IsFalse(new ClassUsingIsGenericTypeOnT<object>().IsGenericType());
        IsFalse(new ClassUsingIsGenericTypeOnT<int[]>().IsGenericType());
        IsFalse(new ClassUsingIsGenericTypeOnT<SimpleStruct>().IsGenericType());
        IsFalse(new ClassUsingIsGenericTypeOnT<char>().IsGenericTypeFromArray());
        IsFalse(new ClassUsingIsGenericTypeOnT<string>().IsGenericTypeFromArray());
        IsFalse(new ClassUsingIsGenericTypeOnT<object>().IsGenericTypeFromArray());
        IsFalse(new ClassUsingIsGenericTypeOnT<int[]>().IsGenericTypeFromArray());
        IsFalse(new ClassUsingIsGenericTypeOnT<GenericSimpleClass<int>>().IsGenericTypeFromArray());
        IsFalse(new ClassUsingIsGenericTypeOnT<IGenericInterface<string>>().IsGenericTypeFromArray());
        IsFalse(new ClassUsingIsGenericTypeOnT<List<object>>().IsGenericTypeFromArray());
        IsFalse(new ClassUsingIsGenericTypeOnT<Action<string>>().IsGenericTypeFromArray());

        IsTrue(typeof(GenericSimpleClass<int>).IsGenericType);
        IsTrue(typeof(GenericSimpleClass<>).IsGenericType);
        IsTrue(typeof(GenericSimpleClass<int>.Nested).IsGenericType);
        IsTrue(typeof(GenericSimpleClass<>.Nested).IsGenericType);
        IsTrue(typeof(GenericEnumClass<SimpleEnum>).IsGenericType);
        IsTrue(typeof(GenericEnumClass<>).IsGenericType);
        IsTrue(typeof(IGenericInterface<string>).IsGenericType);
        IsTrue(typeof(IGenericInterface<>).IsGenericType);
        IsTrue(typeof(GenericStruct<string>).IsGenericType);
        IsTrue(typeof(GenericStruct<>).IsGenericType);
        IsTrue(typeof(SimpleEnum?).IsGenericType);
        IsTrue(typeof(int?).IsGenericType);
        IsTrue(typeof(IntPtr?).IsGenericType);
        IsTrue(typeof(Nullable<>).IsGenericType);
        IsTrue(typeof(Dictionary<int,string>).IsGenericType);
        IsTrue(typeof(Dictionary<,>).IsGenericType);
        IsTrue(typeof(List<string>).IsGenericType);
        IsTrue(typeof(List<>).IsGenericType);
        IsTrue(typeof(Action<>).IsGenericType);
        IsTrue(typeof(Action<string>).IsGenericType);
        IsTrue(typeof(Func<string, int>).IsGenericType);
        IsTrue(typeof(Func<,>).IsGenericType);
        IsTrue(new ClassUsingIsGenericTypeOnT<List<string>>().IsGenericType());
        IsTrue(new ClassUsingIsGenericTypeOnT<List<object>>().IsGenericType());
        IsTrue(new ClassUsingIsGenericTypeOnT<GenericSimpleClass<int>>().IsGenericType());
        IsTrue(new ClassUsingIsGenericTypeOnT<int?>().IsGenericType());
        IsTrue(new ClassUsingIsGenericTypeOnT<Action<string>>().IsGenericType());
        IsTrue(new ClassUsingIsGenericTypeOnT<char>().IsGenericTypeFromOtherGenericType());
        IsTrue(new ClassUsingIsGenericTypeOnT<string>().IsGenericTypeFromOtherGenericType());
        IsTrue(new ClassUsingIsGenericTypeOnT<object>().IsGenericTypeFromOtherGenericType());
        IsTrue(new ClassUsingIsGenericTypeOnT<int[]>().IsGenericTypeFromOtherGenericType());
    }

    private static void GetGenericTypeDefinitionTests()
    {
        AreEqual(typeof(GenericEnumClass<SimpleEnum>).GetGenericTypeDefinition(), typeof(GenericEnumClass<>));
        AreEqual(typeof(GenericEnumClass<>).GetGenericTypeDefinition(), typeof(GenericEnumClass<>));
        AreEqual(typeof(IGenericInterface<string>).GetGenericTypeDefinition(), typeof(IGenericInterface<>));
        AreEqual(typeof(IGenericInterface<>).GetGenericTypeDefinition(), typeof(IGenericInterface<>));
        AreEqual(typeof(GenericStruct<string>).GetGenericTypeDefinition(), typeof(GenericStruct<>));
        AreEqual(typeof(GenericStruct<>).GetGenericTypeDefinition(), typeof(GenericStruct<>));
        AreEqual(typeof(SimpleEnum?).GetGenericTypeDefinition(), typeof(Nullable<>));
        AreEqual(typeof(int?).GetGenericTypeDefinition(), typeof(Nullable<>));
        AreEqual(typeof(IntPtr?).GetGenericTypeDefinition(), typeof(Nullable<>));
        AreEqual(typeof(Nullable<>).GetGenericTypeDefinition(), typeof(Nullable<>));
        AreEqual(typeof(KeyValuePair<int,string>).GetGenericTypeDefinition(), typeof(KeyValuePair<,>));
        AreEqual(typeof(KeyValuePair<,>).GetGenericTypeDefinition(), typeof(KeyValuePair<,>));
        AreEqual(typeof(Dictionary<int,string>).GetGenericTypeDefinition(), typeof(Dictionary<,>));
        AreEqual(typeof(Dictionary<,>).GetGenericTypeDefinition(), typeof(Dictionary<,>));
        AreEqual(typeof(List<string>).GetGenericTypeDefinition(), typeof(List<>));
        AreEqual(typeof(List<>).GetGenericTypeDefinition(), typeof(List<>));
        AreEqual(typeof(Action<>).GetGenericTypeDefinition(), typeof(Action<>));
        AreEqual(typeof(Action<string>).GetGenericTypeDefinition(), typeof(Action<>));
        AreEqual(typeof(Func<string, int>).GetGenericTypeDefinition(), typeof(Func<,>));
        AreEqual(typeof(Func<,>).GetGenericTypeDefinition(), typeof(Func<,>));

        // Test for __Canon
        AreEqual(GetGenericTypeDefinition<GenericEnumClass<SimpleEnum>>(), typeof(GenericEnumClass<>));
        AreEqual(GetGenericTypeDefinition<IGenericInterface<string>>(), typeof(IGenericInterface<>));
        AreEqual(GetGenericTypeDefinition<GenericStruct<string>>(), typeof(GenericStruct<>));
        AreEqual(GetGenericTypeDefinition<Dictionary<int,string>>(), typeof(Dictionary<,>));
        AreEqual(GetGenericTypeDefinition<List<string>>(), typeof(List<>));
        AreEqual(GetGenericTypeDefinition<Action<string>>(), typeof(Action<>));
        AreEqual(GetGenericTypeDefinition<Func<string, int>>(), typeof(Func<,>));
    }

    private static int _varInt = 42;
    private static int? _varNullableInt = 42;
    private static decimal _varDecimal = 42M;
    private static string _varString = "42";
    private static object _varObject = new object();
    private static int[] _varArrayOfInt = new int[10];
    private static Action<int> _varAction = _ => { };
    private static GenericStruct<int> _varGenericStructInt = new GenericStruct<int> { field = 42 };
    private static GenericStruct<string> _varGenericStructStr = new GenericStruct<string> { field = "42" };
    private static SimpleEnum _varEnum = SimpleEnum.B;

    private static int? _varNullableIntNull = null;
    private static string _varStringNull = null;


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsValueType<T>() => typeof(T).IsValueType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValueType<T>(T val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsValueTypeRef<T>(ref T val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsValueTypeObj(object val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static dynamic CreateDynamic1() => 42;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static dynamic CreateDynamic2() => new { Name = "Test" };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type GetGenericTypeDefinition<T>() => typeof(T).GetGenericTypeDefinition();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void IsTrue(bool expression, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
    {
        if (!expression)
        {
            Console.WriteLine($"{file}:L{line} test failed (expected: true).");
            _errors++;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void IsFalse(bool expression, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
    {
        if (expression)
        {
            Console.WriteLine($"{file}:L{line} test failed (expected: false).");
            _errors++;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AreEqual(Type left, Type right, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
    {
        if (left != right)
        {
            Console.WriteLine($"{file}:L{line} test failed (expected: '{left}' to be equal to '{right}').");
            _errors++;
        }
    }

    static void ThrowsNRE(Action action, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
    {
        try
        {
            action();
        }
        catch (NullReferenceException)
        {
            return;
        }
        catch (Exception exc)
        {
            Console.WriteLine($"{file}:L{line} {exc}");
        }
        Console.WriteLine($"Line {line}: test failed (expected: NullReferenceException)");
        _errors++;
    }
}

public class ClassUsingIsGenericTypeOnT<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool IsGenericType() => typeof(T).IsGenericType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool IsGenericTypeFromArray() => typeof(T[]).IsGenericType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool IsGenericTypeFromOtherGenericType() => typeof(GenericSimpleClass<T>).IsGenericType;
}

public class GenericSimpleClass<T>
{
    public class Nested
    {
    }
}

public class DerivedGenericSimpleClass : GenericSimpleClass<string>
{
}

public class GenericEnumClass<T> where T : Enum
{
    public T field;
}

public interface IGenericInterface<T>
{
}

public struct ImplementingStruct1 : IGenericInterface<ImplementingStruct1>
{
}

public struct ImplementingStruct2 : IGenericInterface<ImplementingStruct2>
{
}

public struct GenericStruct<T>
{
    public T field;
}

public struct SimpleStruct
{
    public int field;
}

public enum SimpleEnum
{
    A,B,C
}
