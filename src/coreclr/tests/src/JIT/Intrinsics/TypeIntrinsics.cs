using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

public class Program
{
    private static int _errors = 0;

    public static int Main(string[] args)
    {
        IsTrue (typeof(byte).IsPrimitive);
        IsTrue (typeof(byte).IsValueType);
        IsFalse(typeof(byte).IsClass);
        IsTrue (typeof(int).IsPrimitive);
        IsTrue (typeof(int).IsValueType);
        IsFalse(typeof(int).IsClass);
        IsFalse(typeof(int?).IsPrimitive);
        IsTrue (typeof(int?).IsValueType);
        IsFalse(typeof(int?).IsClass);
        IsFalse(typeof(int*).IsPrimitive);
        IsFalse(typeof(int*).IsValueType);
        IsTrue (typeof(int*).IsClass);
        IsFalse(typeof(void*).IsPrimitive);
        IsFalse(typeof(void*).IsValueType);
        IsTrue (typeof(void*).IsClass);
        IsFalse(typeof(GenericStruct<int>*).IsPrimitive);
        IsFalse(typeof(GenericStruct<int>*).IsValueType);
        IsTrue (typeof(GenericStruct<int>*).IsClass);
        IsTrue (typeof(IntPtr).IsPrimitive);
        IsTrue (typeof(IntPtr).IsValueType);
        IsFalse(typeof(IntPtr).IsClass);
        IsFalse(typeof(decimal).IsPrimitive);
        IsTrue (typeof(decimal).IsValueType);
        IsFalse(typeof(decimal).IsClass);
        IsTrue (typeof(double).IsPrimitive);
        IsTrue (typeof(double).IsValueType);
        IsFalse(typeof(double).IsClass);
        IsFalse(typeof(string).IsPrimitive);
        IsFalse(typeof(string).IsValueType);
        IsTrue (typeof(string).IsClass);
        IsFalse(typeof(object).IsPrimitive);
        IsFalse(typeof(object).IsValueType);
        IsTrue (typeof(object).IsClass);
        IsFalse(typeof(object[]).IsPrimitive);
        IsFalse(typeof(object[]).IsValueType);
        IsTrue (typeof(object[]).IsClass);
        IsFalse(typeof(int[]).IsPrimitive);
        IsFalse(typeof(int[]).IsValueType);
        IsTrue (typeof(int[]).IsClass);
        IsFalse(typeof(int[,,]).IsPrimitive);
        IsFalse(typeof(int[,,]).IsValueType);
        IsTrue (typeof(int[,,]).IsClass);
        IsFalse(typeof(IEnumerable<int>).IsPrimitive);
        IsFalse(typeof(IEnumerable<int>).IsValueType);
        IsFalse(typeof(IEnumerable<int>).IsClass);
        IsFalse(typeof(Action<int>).IsPrimitive);
        IsFalse(typeof(Action<int>).IsValueType);
        IsTrue (typeof(Action<int>).IsClass);
        IsFalse(typeof(GenericStruct<int>).IsPrimitive);
        IsTrue (typeof(GenericStruct<int>).IsValueType);
        IsFalse(typeof(GenericStruct<int>).IsClass);
        IsFalse(typeof(GenericStruct<string>).IsPrimitive);
        IsTrue (typeof(GenericStruct<string>).IsValueType);
        IsFalse(typeof(GenericStruct<string>).IsClass);
        IsFalse(typeof(GenericStruct<string>).IsPrimitive);
        IsTrue (typeof(GenericStruct<string>).IsValueType);
        IsFalse(typeof(GenericStruct<string>).IsClass);
        IsFalse(typeof(KeyValuePair<int, string>).IsPrimitive);
        IsTrue (typeof(KeyValuePair<int, string>).IsValueType);
        IsFalse(typeof(KeyValuePair<int, string>).IsClass);
        IsFalse(typeof(KeyValuePair<Program, string>).IsPrimitive);
        IsTrue (typeof(KeyValuePair<Program, string>).IsValueType);
        IsFalse(typeof(KeyValuePair<Program, string>).IsClass);
        IsFalse(typeof(SimpleEnum).IsPrimitive);
        IsTrue (typeof(SimpleEnum).IsValueType);
        IsFalse(typeof(SimpleEnum).IsClass);
        IsFalse(typeof(void).IsPrimitive);
        IsTrue (typeof(void).IsValueType);
        IsFalse(typeof(void).IsClass);
        IsFalse(typeof(ValueType).IsPrimitive);
        IsFalse(typeof(ValueType).IsValueType);
        IsTrue (typeof(ValueType).IsClass);
        IsFalse(typeof(List<>).IsPrimitive);
        IsFalse(typeof(List<>).IsValueType);
        IsTrue (typeof(List<>).IsClass);
        IsFalse(typeof(IDictionary<,>).IsPrimitive);
        IsFalse(typeof(IDictionary<,>).IsValueType);
        IsFalse(typeof(IDictionary<,>).IsClass);
        IsFalse(typeof(Vector128<>).IsPrimitive);
        IsTrue (typeof(Vector128<>).IsValueType);
        IsFalse(typeof(Vector128<>).IsClass);
        IsFalse(typeof(Vector128<byte>).IsPrimitive);
        IsTrue (typeof(Vector128<byte>).IsValueType);
        IsFalse(typeof(Vector128<byte>).IsClass);

        // Test __Canon
        IsFalse(IsPrimitive<IEnumerable<int>>());
        IsFalse(IsPrimitive<IEnumerable<string>>());
        IsFalse(IsPrimitive<IEnumerable<IDisposable>>());
        IsFalse(IsPrimitive<IDictionary<int, string>>());
        IsFalse(IsPrimitive<IDictionary<IConvertible, IComparer<int>>>());
        IsFalse(IsPrimitive<Dictionary<int, int>>());
        IsFalse(IsPrimitive<Dictionary<string, IEnumerable>>());

        IsFalse(IsValueType<IEnumerable<int>>());
        IsFalse(IsValueType<IEnumerable<string>>());
        IsFalse(IsValueType<IEnumerable<IDisposable>>());
        IsFalse(IsValueType<IDictionary<int, string>>());
        IsFalse(IsValueType<IDictionary<IConvertible, IComparer<int>>>());
        IsFalse(IsValueType<Dictionary<int, int>>());
        IsFalse(IsValueType<Dictionary<string, IEnumerable>>());

        IsFalse(IsClass<IEnumerable<int>>());
        IsFalse(IsClass<IEnumerable<string>>());
        IsFalse(IsClass<IEnumerable<IDisposable>>());
        IsFalse(IsClass<IDictionary<int, string>>());
        IsFalse(IsClass<IDictionary<IConvertible, IComparer<int>>>());
        IsTrue (IsClass<Dictionary<int, int>>());
        IsTrue (IsClass<Dictionary<string, IEnumerable>>());

        // Test `x.GetType().IsX`
        IsTrue (IsPrimitive<int>(42));
        IsTrue (IsPrimitive<int?>(new Nullable<int>(42)));
        IsFalse(IsPrimitive<decimal>(42M));
        IsFalse(IsPrimitive<string>("42"));
        IsFalse(IsPrimitive<object>(new object()));
        IsFalse(IsPrimitive<IEnumerable<int>>(new int[10]));
        IsFalse(IsPrimitive<Action<int>>(_ => { }));
        IsFalse(IsPrimitive<GenericStruct<int>>(default));
        IsFalse(IsPrimitive<GenericStruct<string>>(default));
        IsFalse(IsPrimitive(SimpleEnum.B));
        IsTrue (IsPrimitive(CreateDynamic1()));
        IsFalse(IsPrimitive(CreateDynamic2()));

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

        IsFalse(IsClass<int>(42));
        IsFalse(IsClass<int?>(new Nullable<int>(42)));
        IsFalse(IsClass<decimal>(42M));
        IsTrue (IsClass<string>("42"));
        IsTrue (IsClass<object>(new object()));
        IsTrue (IsClass<IEnumerable<int>>(new int[10]));
        IsTrue (IsClass<Action<int>>(_ => { }));
        IsFalse(IsClass<GenericStruct<int>>(default));
        IsFalse(IsClass<GenericStruct<string>>(default));
        IsFalse(IsClass(SimpleEnum.B));
        IsFalse(IsClass(CreateDynamic1()));
        IsTrue (IsClass(CreateDynamic2()));

        // boxing
        IsTrue (IsPrimitiveObj(42));
        IsTrue (IsPrimitiveObj(new Nullable<int>(42)));
        IsFalse(IsPrimitiveObj(new decimal(42)));
        IsFalse(IsPrimitiveObj("42"));
        IsFalse(IsPrimitiveObj(new object()));
        IsFalse(IsPrimitiveObj(new int[10]));
        IsFalse(IsPrimitiveObj((Action<int>)(_ => { })));
        IsFalse(IsPrimitiveObj(new GenericStruct<int>()));
        IsFalse(IsPrimitiveObj(new GenericStruct<string>()));
        IsFalse(IsPrimitiveObj(SimpleEnum.B));
        IsTrue (IsPrimitiveObj(CreateDynamic1()));
        IsFalse(IsPrimitiveObj(CreateDynamic2()));

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

        IsFalse(IsClassObj(42));
        IsFalse(IsClassObj(new Nullable<int>(42)));
        IsFalse(IsClassObj(42M));
        IsTrue (IsClassObj("42"));
        IsTrue (IsClassObj(new object()));
        IsTrue (IsClassObj(new int[10]));
        IsTrue (IsClassObj((Action<int>)(_ => { })));
        IsFalse(IsClassObj(new GenericStruct<int>()));
        IsFalse(IsClassObj(new GenericStruct<string>()));
        IsFalse(IsClassObj(SimpleEnum.B));
        IsFalse(IsClassObj(CreateDynamic1()));
        IsTrue (IsClassObj(CreateDynamic2()));

        // ByRef
        IsTrue (IsPrimitiveRef(ref _varInt));
        IsTrue (IsPrimitiveRef(ref _varNullableInt));
        IsFalse(IsPrimitiveRef(ref _varDecimal));
        IsFalse(IsPrimitiveRef(ref _varString));
        IsFalse(IsPrimitiveRef(ref _varObject));
        IsFalse(IsPrimitiveRef(ref _varArrayOfInt));
        IsFalse(IsPrimitiveRef(ref _varAction));
        IsFalse(IsPrimitiveRef(ref _varGenericStructInt));
        IsFalse(IsPrimitiveRef(ref _varGenericStructStr));
        IsFalse(IsPrimitiveRef(ref _varEnum));

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

        IsFalse(IsClassRef(ref _varInt));
        IsFalse(IsClassRef(ref _varNullableInt));
        IsFalse(IsClassRef(ref _varDecimal));
        IsTrue (IsClassRef(ref _varString));
        IsTrue (IsClassRef(ref _varObject));
        IsTrue (IsClassRef(ref _varArrayOfInt));
        IsTrue (IsClassRef(ref _varAction));
        IsFalse(IsClassRef(ref _varGenericStructInt));
        IsFalse(IsClassRef(ref _varGenericStructStr));
        IsFalse(IsClassRef(ref _varEnum));


        // make sure optimization won't hide NRE check
        // e.g. `_varStringNull.GetType().IsPrimitive` => optimize to just `false`
        ThrowsNRE(() => { IsPrimitive(_varNullableIntNull); });
        ThrowsNRE(() => { IsPrimitive(_varStringNull); });
        ThrowsNRE(() => { IsClassRef(ref _varNullableIntNull); });
        ThrowsNRE(() => { IsClassRef(ref _varStringNull); });

        Console.WriteLine(_errors);
        Console.ReadKey();
        return 100 + _errors;
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
    private static bool IsPrimitive<T>() => typeof(T).IsPrimitive;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsValueType<T>() => typeof(T).IsValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsClass<T>() => typeof(T).IsClass;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPrimitive<T>(T val) => val.GetType().IsPrimitive;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValueType<T>(T val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsClass<T>(T val) => val.GetType().IsClass;


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsPrimitiveRef<T>(ref T val) => val.GetType().IsPrimitive;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsValueTypeRef<T>(ref T val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsClassRef<T>(ref T val) => val.GetType().IsClass;


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsPrimitiveObj(object val) => val.GetType().IsPrimitive;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsValueTypeObj(object val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsClassObj(object val) => val.GetType().IsClass;


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static dynamic CreateDynamic1() => 42;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static dynamic CreateDynamic2() => new { Name = "Test" };


    static void IsTrue(bool expression, [CallerLineNumber] int line = 0)
    {
        if (!expression)
        {
            Console.WriteLine($"Line {line}: test failed (expected: true).");
            _errors++;
        }
    }

    static void IsFalse(bool expression, [CallerLineNumber] int line = 0)
    {
        if (expression)
        {
            Console.WriteLine($"Line {line}: test failed (expected: false).");
            _errors++;
        }
    }

    static void ThrowsNRE(Action action, [CallerLineNumber] int line = 0)
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
            Console.WriteLine($"Line {line}: {exc}");
        }
        Console.WriteLine($"Line {line}: test failed (expected: NullReferenceException)");
    }
}

public struct GenericStruct<T>
{
    public T field;
}

public enum SimpleEnum
{
    A,B,C
}