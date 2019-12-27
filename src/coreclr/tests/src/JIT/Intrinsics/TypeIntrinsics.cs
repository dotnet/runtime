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

        ThrowsNRE(() => { IsValueType(_varNullableIntNull); });
        ThrowsNRE(() => { IsValueType(_varStringNull); });
        ThrowsNRE(() => { IsValueTypeRef(ref _varNullableIntNull); });
        ThrowsNRE(() => { IsValueTypeRef(ref _varStringNull); });

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