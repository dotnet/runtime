using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Program
{
    private static int _errors = 0;

    public static int Main(string[] args)
    {
        IsTrue(typeof(int).IsPrimitive, "Case1");
        IsTrue(typeof(int).IsValueType, "Case2");
        IsFalse(typeof(int).IsClass, "Case3");

        IsFalse(typeof(int?).IsPrimitive, "Case4");
        IsTrue(typeof(int?).IsValueType, "Case5");
        IsFalse(typeof(int?).IsClass, "Case6");

        IsFalse(typeof(int*).IsPrimitive, "Case7");
        IsFalse(typeof(int*).IsValueType, "Case8");
        IsTrue(typeof(int*).IsClass, "Case9");

        IsFalse(typeof(void*).IsPrimitive, "Case10");
        IsFalse(typeof(void*).IsValueType, "Case11");
        IsTrue(typeof(void*).IsClass, "Case12");

        IsFalse(typeof(decimal).IsPrimitive, "Case13");
        IsTrue(typeof(decimal).IsValueType, "Case14");
        IsFalse(typeof(decimal).IsClass, "Case15");

        IsFalse(typeof(string).IsPrimitive, "Case16");
        IsFalse(typeof(string).IsValueType, "Case17");
        IsTrue(typeof(string).IsClass, "Case18");

        IsFalse(typeof(object).IsPrimitive, "Case19");
        IsFalse(typeof(object).IsValueType, "Case20");
        IsTrue(typeof(object).IsClass, "Case21");

        IsFalse(typeof(IEnumerable<int>).IsPrimitive, "Case22");
        IsFalse(typeof(IEnumerable<int>).IsValueType, "Case23");
        IsFalse(typeof(IEnumerable<int>).IsClass, "Case24");

        IsFalse(typeof(Action<int>).IsPrimitive, "Case25");
        IsFalse(typeof(Action<int>).IsValueType, "Case26");
        IsTrue(typeof(Action<int>).IsClass, "Case27");

        IsFalse(typeof(GenericStruct<int>).IsPrimitive, "Case28");
        IsTrue(typeof(GenericStruct<int>).IsValueType, "Case29");
        IsFalse(typeof(GenericStruct<int>).IsClass, "Case30");

        IsFalse(typeof(GenericStruct<string>).IsPrimitive, "Case31");
        IsTrue(typeof(GenericStruct<string>).IsValueType, "Case32");
        IsFalse(typeof(GenericStruct<string>).IsClass, "Case33");


        IsTrue(IsPrimitive<int>(42), "Case34");
        IsTrue(IsPrimitive<int?>(new Nullable<int>(42)), "Case35");
        IsFalse(IsPrimitive<decimal>(42M), "Case36");
        IsFalse(IsPrimitive<string>("42"), "Case37");
        IsFalse(IsPrimitive<object>(new object()), "Case38");
        IsFalse(IsPrimitive<IEnumerable<int>>(new int[10]), "Case39");
        IsFalse(IsPrimitive<Action<int>>(_ => { }), "Case40");
        IsFalse(IsPrimitive<GenericStruct<int>>(default), "Case41");
        IsFalse(IsPrimitive<GenericStruct<string>>(default), "Case42");
        IsTrue(IsPrimitive(CreateDynamic1()), "Case43");
        IsFalse(IsPrimitive(CreateDynamic2()), "Case44");

        IsTrue(IsValueType<int>(42), "Case45");
        IsTrue(IsValueType<int?>(new Nullable<int>(42)), "Case46");
        IsTrue(IsValueType<decimal>(42M), "Case47");
        IsFalse(IsValueType<string>("42"), "Case48");
        IsFalse(IsValueType<object>(new object()), "Case49");
        IsFalse(IsValueType<IEnumerable<int>>(new int[10]), "Case50");
        IsFalse(IsValueType<Action<int>>(_ => { }), "Case51");
        IsTrue(IsValueType<GenericStruct<int>>(default), "Case52");
        IsTrue(IsValueType<GenericStruct<string>>(default), "Case53");
        IsTrue(IsValueType(CreateDynamic1()), "Case54");
        IsFalse(IsValueType(CreateDynamic2()), "Case55");

        IsFalse(IsClass<int>(42), "Case56");
        IsFalse(IsClass<int?>(new Nullable<int>(42)), "Case57");
        IsFalse(IsClass<decimal>(42M), "Case58");
        IsTrue(IsClass<string>("42"), "Case59");
        IsTrue(IsClass<object>(new object()), "Case60");
        IsTrue(IsClass<IEnumerable<int>>(new int[10]), "Case61");
        IsTrue(IsClass<Action<int>>(_ => { }), "Case62");
        IsFalse(IsClass<GenericStruct<int>>(default), "Case63");
        IsFalse(IsClass<GenericStruct<string>>(default), "Case64");
        IsFalse(IsClass(CreateDynamic1()), "Case65");
        IsTrue(IsClass(CreateDynamic2()), "Case66");


        IsTrue(IsPrimitiveObj(42), "Case67");
        IsTrue(IsPrimitiveObj(new Nullable<int>(42)), "Case68");
        IsTrue(IsPrimitiveObj(new decimal(42)), "Case69");
        IsFalse(IsPrimitiveObj("42"), "Case70");
        IsFalse(IsPrimitiveObj(new object()), "Case71");
        IsFalse(IsPrimitiveObj(new int[10]), "Case72");
        IsFalse(IsPrimitiveObj((Action<int>)(_ => { })), "Case73");
        IsTrue(IsPrimitiveObj(new GenericStruct<int>()), "Case74");
        IsTrue(IsPrimitiveObj(new GenericStruct<string>()), "Case75");
        IsTrue(IsPrimitiveObj(CreateDynamic1()), "Case76");
        IsFalse(IsPrimitiveObj(CreateDynamic2()), "Case77");

        IsTrue(IsValueTypeObj(42), "Case78");
        IsTrue(IsValueTypeObj(new Nullable<int>(42)), "Case79");
        IsTrue(IsValueTypeObj(42M), "Case80");
        IsFalse(IsValueTypeObj("42"), "Case81");
        IsFalse(IsValueTypeObj(new object()), "Case82");
        IsFalse(IsValueTypeObj(new int[10]), "Case83");
        IsFalse(IsValueTypeObj((Action<int>)(_ => { })), "Case84");
        IsTrue(IsValueTypeObj(new GenericStruct<int>()), "Case85");
        IsTrue(IsValueTypeObj(new GenericStruct<string>()), "Case86");
        IsTrue(IsValueTypeObj(CreateDynamic1()), "Case87");
        IsFalse(IsValueTypeObj(CreateDynamic2()), "Case88");

        IsTrue(IsClassObj(42), "Case89");
        IsTrue(IsClassObj(new Nullable<int>(42)), "Case90");
        IsTrue(IsClassObj(42M), "Case91");
        IsFalse(IsClassObj("42"), "Case92");
        IsFalse(IsClassObj(new object()), "Case93");
        IsFalse(IsClassObj(new int[10]), "Case94");
        IsFalse(IsClassObj((Action<int>)(_ => { })), "Case95");
        IsTrue(IsClassObj(new GenericStruct<int>()), "Case96");
        IsTrue(IsClassObj(new GenericStruct<string>()), "Case97");
        IsTrue(IsClassObj(CreateDynamic1()), "Case98");
        IsFalse(IsClassObj(CreateDynamic2()), "Case99");

        
        IsTrue(IsPrimitiveRef(ref _varInt), "Case100");
        IsTrue(IsPrimitiveRef(ref _varNullableInt), "Case101");
        IsFalse(IsPrimitiveRef(ref _varDecimal), "Case102");
        IsFalse(IsPrimitiveRef(ref _varString), "Case103");
        IsFalse(IsPrimitiveRef(ref _varObject), "Case104");
        IsFalse(IsPrimitiveRef(ref _varArrayOfInt), "Case105");
        IsFalse(IsPrimitiveRef(ref _varAction), "Case106");
        IsFalse(IsPrimitiveRef(ref _varGenericStructInt), "Case107");
        IsFalse(IsPrimitiveRef(ref _varGenericStructStr), "Case108");

        IsTrue(IsValueTypeRef(ref _varInt), "Case109");
        IsTrue(IsValueTypeRef(ref _varNullableInt), "Case110");
        IsTrue(IsValueTypeRef(ref _varDecimal), "Case111");
        IsFalse(IsValueTypeRef(ref _varString), "Case112");
        IsFalse(IsValueTypeRef(ref _varObject), "Case113");
        IsFalse(IsValueTypeRef(ref _varArrayOfInt), "Case114");
        IsFalse(IsValueTypeRef(ref _varAction), "Case115");
        IsTrue(IsValueTypeRef(ref _varGenericStructInt), "Case116");
        IsTrue(IsValueTypeRef(ref _varGenericStructStr), "Case117");

        IsFalse(IsClassRef(ref _varInt), "Case118");
        IsFalse(IsClassRef(ref _varNullableInt), "Case119");
        IsFalse(IsClassRef(ref _varDecimal), "Case120");
        IsTrue(IsClassRef(ref _varString), "Case121");
        IsTrue(IsClassRef(ref _varObject), "Case122");
        IsTrue(IsClassRef(ref _varArrayOfInt), "Case123");
        IsTrue(IsClassRef(ref _varAction), "Case124");
        IsFalse(IsClassRef(ref _varGenericStructInt), "Case125");
        IsFalse(IsClassRef(ref _varGenericStructStr), "Case126");

        // make sure optimization won't hide NRE check
        // e.g. `_varStringNull.GetType().IsPrimitive` => optimize to just `false`
        ThrowsNRE(() => { IsPrimitive(_varNullableIntNull); }, "Case127");
        ThrowsNRE(() => { IsPrimitive(_varStringNull); }, "Case128");
        ThrowsNRE(() => { IsClassRef(ref _varNullableIntNull); }, "Case129");
        ThrowsNRE(() => { IsClassRef(ref _varStringNull); }, "Case130");

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

    private static int? _varNullableIntNull = null;
    private static string _varStringNull = null;

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
    private static bool IsPrimitiveObj(object val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsValueTypeObj(object val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsClassObj(object val) => val.GetType().IsValueType;


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static dynamic CreateDynamic1() => 42;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static dynamic CreateDynamic2() => new { Name = "Test" };


    static void IsTrue(bool expression, string caseName)
    {
        if (!expression)
        {
            Console.WriteLine($"{caseName} failed.");
            _errors++;
        }
    }

    static void IsFalse(bool expression, string caseName)
    {
        if (expression)
        {
            Console.WriteLine($"{caseName} failed.");
            _errors++;
        }
    }

    static void ThrowsNRE(Action action, string caseName)
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
            Console.WriteLine($"{caseName}: {exc}");
        }
        Console.WriteLine($"{caseName} didn't throw NRE.");
    }
}

public struct GenericStruct<T>
{
    public T field;
}