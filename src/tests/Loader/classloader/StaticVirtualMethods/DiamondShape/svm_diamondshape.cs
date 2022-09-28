// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

interface IFoo
{
    virtual static int Foo(int a)
    {
        return a;
    }
}

interface IFoo2 : IFoo
{
    static int IFoo.Foo(int a)
    {
        Console.WriteLine("At IFoo2.Foo");
        return a + 1;
    }
}

interface IFooEx : IFoo
{
    static int IFoo.Foo(int a)
    {
        Console.WriteLine("At IFooEx.Foo");
        return a + 2;
    }
}

interface IFooExReabstract : IFooEx
{
    abstract static int IFoo.Foo(int a);
}

class FooClass : IFoo2, IFoo, IFooEx
{
}

class FooClassReabstract : IFoo2, IFoo, IFooExReabstract
{
}

struct FooStruct : IFoo2, IFoo, IFooEx
{
}

interface I1
{
    static int Func(int a);
}

interface I2 : I1
{
    static int I1.Func(int a)
    {
        Console.WriteLine("At I2.Func");
        return a + 2;
    }
}

interface I3 : I1
{
    static int I1.Func(int a)
    {
        Console.WriteLine("At I3.Func");
        return a + 3;
    }
}

interface I4 : I2, I1, I3
{
    static int I1.Func(int a)
    {
        Console.WriteLine("At I4.Func");
        return a + 4;
    }
}

interface I4Reabstract : I4
{
    abstract static int I1.Func(int a);
}

interface I4Reimplement : I4Reabstract
{
    static int I1.Func(int a)
    {
        Console.WriteLine("At I4Reimplement.Func");
        return a + 17;
    }
}

class I4Class : I4, I2, I1, I3
{
}

class I4ReimplementClass : I4Reimplement, I2, I1, I3
{
}

struct I4Struct : I4, I2, I1, I3
{
}

interface I5 : I1
{
    static int I1.Func(int a)
    {
        Console.WriteLine("At I5.Func");
        return a + 5;
    }
}

interface I6 : I1
{
    static int I1.Func(int a)
    {
        Console.WriteLine("At I6.Func");
        return a + 6;
    }
}

interface I7 : I5, I6
{
    static int I1.Func(int a)
    {
        Console.WriteLine("At I7.Func");
        return a + 7;
    }
}

interface I8 : I4, I2, I1, I3, I7, I5, I6
{
    static int I1.Func(int a)
    {
        Console.WriteLine("At I8.Func");
        return a + 8;
    }
}

class I47Class : I4, I2, I1, I3, I7, I5, I6
{
}

struct I47Struct : I4, I2, I1, I3, I7, I5, I6
{
}

class I8Class : I8, I4, I2, I1, I3, I7, I5, I6
{
}

struct I8Struct : I8, I4, I2, I1, I3, I7, I5, I6
{
}

interface GI1<T>
{
    static int Func<S>(out Type[] types);
}

interface GI2<T> : GI1<T>
{
    static int GI1<T>.Func<S>(out Type[] types)
    {
        Console.WriteLine(typeof(T) + ", " + typeof(S) + ", GI2");
        types = new Type[] { typeof(T), typeof(S) };
        return 2;
    }

}

interface GI3<T> : GI1<T>
{
    static int GI1<T>.Func<S>(out Type[] types)
    {
        Console.WriteLine(typeof(T) + ", " + typeof(S) + ", GI3");
        types = new Type[] { typeof(T), typeof(S) };
        return 3;
    }
}

interface GI4<T> : GI2<T>, GI1<T>, GI3<T>
{
    static int GI1<T>.Func<S>(out Type[] types)
    {
        Console.WriteLine(typeof(T) + ", " + typeof(S) + ", GI4");
        types = new Type[] { typeof(T), typeof(S) };
        return 4;
    }
}

class GI23Class<T> : GI2<T>, GI1<T>, GI3<T>
{
}

struct GI23Struct<T> : GI2<T>, GI1<T>, GI3<T>
{
}

class GI4Class<T> : GI4<T>, GI2<T>, GI1<T>, GI3<T>
{
}

struct GI4Struct<T> : GI4<T>, GI2<T>, GI1<T>, GI3<T>
{
}

interface IResolutionAtRuntime<T>
{
    virtual abstract static int Func(int a);
}

class ResolutionAtRuntimeBase : IResolutionAtRuntime<object>, IResolutionAtRuntime<string>
{
    static int IResolutionAtRuntime<object>.Func(int a)
    {
        Console.WriteLine("At ResolutionAtRuntimeBase.FuncObject");
        return a + 19;
    }

    static int IResolutionAtRuntime<string>.Func(int a)
    {
        Console.WriteLine("At ResolutionAtRuntimeBase.FuncString");
        return a + 23;
    }
}

class ResolutionAtRuntimeThisObj<T, V> : T is IResolutionAtRuntime<V>
{
    public int RuntimeResolvedFunc(int a)
    {
        Console.WriteLine("At ResolutionAtRuntimeThisObj.RuntimeResolvedFunc");
        return T.Func(a);
    }
}

class ResolutionAtRuntimeClassParam<T, V> : T is IResolutionAtRuntime<V>
{
    public static int RuntimeResolvedFunc(int a)
    {
        Console.WriteLine("At ResolutionAtRuntimeClassParam.RuntimeResolvedFunc");
        return T.Func(a);
    }
}

class ResolutionAtRuntimeMethodParam
{
    public static int RuntimeResolvedFunc<T, V>(int a)
        : T is IResolutionAtRuntime<V>
    {
        Console.WriteLine("At ResolutionAtRuntimeMethodParam.RuntimeResolvedFunc");
        return T.Func(a);
    }
}

class Program
{
    private static void CallFoo<T>(int value)
        : T is IFoo
    {
        T.Foo(value);
    }

    private static Func<int, int> GetFooDelegate<T>()
    {
        return new Func<int, int>(T.Foo);
    }

    private static void CallI1Func<T>(int value)
        : T is I1
    {
        T.Func(value);
    }

    private static Func<int, int> GetI1FuncDelegate<T>()
        : T is I1
    {
        return new Func<int, int>(T.Func);
    }

    private static void CallGI1Func<T, U, V>(out Type[] types)
        : T is GI1<U>
    {
        T.Func<V>(out types);
    }

    private delegate static int GI1Delegate(out Type[]);

    private static GI1Delegate GetGI1FuncDelegate<T, U, V>()
        : T is GI1<U>
    {
        return T.Func<V>;
    }

    public static void Negative()
    {
        Console.WriteLine("Calling IFoo.Foo on FooClass - expecting exception.");
        try
        {
            CallFoo<FooClass>(10);
            Test.Assert(false, "Expecting exception on FooClass");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Resolving delegate IFoo.Foo on FooClass - expecting exception.");
        try
        {
            GetFooDelegate<FooClass>();
            Test.Assert(false, "Expecting exception on FooClass");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Calling IFoo.Foo on FooClassReabstract - expecting exception.");
        try
        {
            CallFoo<FooClassReabstract>(10);
            Test.Assert(false, "Expecting exception on FooClassReabstract");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Calling IFoo.Foo on FooStruct - expecting exception.");
        try
        {
            CallFoo<FooStruct>(10);
            Test.Assert(false, "Expecting exception on FooStruct");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Resolving delegate IFoo.Foo on FooStruct - expecting exception.");
        try
        {
            GetFooDelegate<FooStruct>();
            Test.Assert(false, "Expecting exception on FooStruct");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Calling I1.Func on I47Class - expecting exception");
        try
        {
            CallI1Func<I47Class>(10);
            Test.Assert(false, "Expecting exception on I47Class");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Resolving delegate I1.Func on I47Class - expecting exception");
        try
        {
            GetI1FuncDelegate<I47Class>();
            Test.Assert(false, "Expecting exception on I47Class");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Calling I1.Func on I47Struct - expecting exception");
        try
        {
            CallI1Func<I47Struct>(10);
            Test.Assert(false, "Expecting exception on I47Struct");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Resolving delegate I1.Func on I47Struct - expecting exception");
        try
        {
            GetI1FuncDelegate<I47Struct>();
            Test.Assert(false, "Expecting exception on I47Struct");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Calling GI1<T>.Func on GI23Class<S> - expecting exception");
        try
        {
            Type[] types;
            CallGI1Func<GI23Class<object>, GT1<object>, string>(out types);
            Test.Assert(false, "Expecting exception on GI23Class");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Resolving delegate GI1<T>.Func on GI23Class<S> - expecting exception");
        try
        {
            GetGI1FuncDelegate<GI23Class<object>, GT1<object>, string>();
            Test.Assert(false, "Expecting exception on GI23Class");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Calling GI1<T>.Func on GI23Struct<S> - expecting exception");
        try
        {
            Type[] types;
            CallGI1Func<GI23Struct<object>, GT1<object>, string>(out types);
            Test.Assert(false, "Expecting exception on GI23Struct");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }

        Console.WriteLine("Resolving delegate GI1<T>.Func on GI23Struct<S> - expecting exception");
        try
        {
            GetGI1FuncDelegate<GI23Struct<object>, GT1<object>, string>();
            Test.Assert(false, "Expecting exception on GI23Struct");
        }
        catch(Exception ex)
        {
            Console.WriteLine("Exception caught: " + ex.ToString());
        }
    }

    public static void Positive()
    {
        Console.WriteLine("Calling I1.Func on I4Class - expecting I4.Func");

        Test.Assert(CallI1Func<I4Class>(10) == 14, "Expecting I1.Func to land on I4.Func");

        Console.WriteLine("Calling I1.Func on I4ReimplementClass - expecting I4Reimplement.Func");

        Test.Assert(CallI1Func<I4ReimplementClass>(10) == 27, "Expecting I1.Func to land on I4Reimplement.Func");

        Console.WriteLine("Calling I1.Func on I4Class as a delegate - expecting I4.Func");

        Test.Assert(GetI1FuncDelegate<I4Class>()(10) == 14, "Expecting I1.Func to land on I4.Func");

        Console.WriteLine("Calling I1.Func on I4Struct - expecting I4.Func");

        Test.Assert(CallI1Func<I4Struct>(10) == 14, "Expecting I1.Func to land on I4.Func");

        Console.WriteLine("Calling I1.Func on I4Struct as a delegate - expecting I4.Func");

        Test.Assert(GetI1FuncDelegate<I4Struct>()(10) == 14, "Expecting I1.Func to land on I4.Func");

        Console.WriteLine("Calling I1.Func on I8Class - expecting I8.Func");

        Test.Assert(CallI1Func<I8Class>(10) == 18, "Expecting I1.Func to land on I8.Func");

        Console.WriteLine("Calling I1.Func on I8Class as a delegate - expecting I8.Func");

        Test.Assert(GetI1FuncDelegate<I8Class>()(10) == 18, "Expecting I1.Func to land on I8.Func");

        Console.WriteLine("Calling I1.Func on I8Struct - expecting I8.Func");

        Test.Assert(CallI1Func<I8Struct>(10) == 18, "Expecting I1.Func to land on I8.Func");

        Console.WriteLine("Calling I1.Func on I8Struct as a delegate - expecting I8.Func");

        Test.Assert(GetI1FuncDelegate<I8Struct>()(10) == 18, "Expecting I1.Func to land on I8.Func");

        Type[] types;

        Console.WriteLine("Calling GI1.Func on GI4Class<object> - expecting GI4.Func<S>");

        Test.Assert(CallGI1Func<GI4Class<object>, GI1<object>, string>(out types) == 4, "Expecting GI1<T>.Func to land on GII4<T>.Func<S>");
        Test.Assert(types[0] == typeof(object), "T must be object");
        Test.Assert(types[1] == typeof(string), "S must be string");

        Console.WriteLine("Calling GI1.Func on GI4Class<object> as a delegate - expecting GI4.Func<S>");

        Test.Assert(GetGI1FuncDelegate<GI4Class<object>, GI1<object>, string>()(out types) == 4, "Expecting GI1<T>.Func to land on GII4<T>.Func<S>");
        Test.Assert(types[0] == typeof(object), "T must be object");
        Test.Assert(types[1] == typeof(string), "S must be string");

        Console.WriteLine("Calling GI1.Func on GI4Struct<object> - expecting GI4.Func<S>");

        Test.Assert(CallGI1Func<GI4Struct<object>, GI1<object>, string>(out types) == 4, "Expecting GI1<T>.Func to land on GII4<T>.Func<S>");
        Test.Assert(types[0] == typeof(object), "T must be object");
        Test.Assert(types[1] == typeof(string), "S must be string");

        Console.WriteLine("Calling GI1.Func on GI4Struct<object> as a delegate - expecting GI4.Func<S>");

        Test.Assert(GetGI1FuncDelegate<GI4Struct<object>, GI1<object>, string>()(out types) == 4, "Expecting GI1<T>.Func to land on GII4<T>.Func<S>");
        Test.Assert(types[0] == typeof(object), "T must be object");
        Test.Assert(types[1] == typeof(string), "S must be string");

        Console.WriteLine("Calling ResolutionAtRuntimeThisObj<ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc - expecting ResolutionAtRuntimeBase.FuncObject");
        Test.Assert(new ResolutionAtRuntimeThisObj<ResolutionAtRuntimeBase, object>().RuntimeResolvedFunc(200) == 219, "Expecting ResolutionAtRuntimeThisObj<ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc to land on ResolutionAtRuntimeBase.FuncObject");

        Console.WriteLine("Calling ResolutionAtRuntimeThisObj<ResolutionAtRuntimeBase, string>::RuntimeResolvedFunc - expecting ResolutionAtRuntimeBase.FuncString");
        Test.Assert(new ResolutionAtRuntimeThisObj<ResolutionAtRuntimeBase, string>().RuntimeResolvedFunc(200) == 223, "Expecting ResolutionAtRuntimeThisObj<ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc to land on ResolutionAtRuntimeBase.FuncString");

        Console.WriteLine("Calling ResolutionAtRuntimeClassParam<ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc - expecting ResolutionAtRuntimeBase.FuncObject");
        Test.Assert(ResolutionAtRuntimeClassParam<ResolutionAtRuntimeBase, object>.RuntimeResolvedFunc(200) == 219, "Expecting ResolutionAtRuntimeClassParam<ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc to land on ResolutionAtRuntimeBase.FuncObject");

        Console.WriteLine("Calling ResolutionAtRuntimeClassParam<ResolutionAtRuntimeBase, string>::RuntimeResolvedFunc - expecting ResolutionAtRuntimeBase.FuncString");
        Test.Assert(ResolutionAtRuntimeClassParam<ResolutionAtRuntimeBase, string>.RuntimeResolvedFunc(200) == 223, "Expecting ResolutionAtRuntimeClassParam<ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc to land on ResolutionAtRuntimeBase.FuncString");

        Console.WriteLine("Calling ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc<ResolutionAtRuntimeBase, object> - expecting ResolutionAtRuntimeBase.FuncObject");
        Test.Assert(ResolutionAtRuntimeClassParam.RuntimeResolvedFunc<ResolutionAtRuntimeBase, object>(100) == 119, "Expecting ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc<ResolutionAtRuntimeBase, object> to land on ResolutionAtRuntimeBase.FuncObject");

        Console.WriteLine("Calling ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc<ResolutionAtRuntimeBase, string> - expecting ResolutionAtRuntimeBase.FuncString");
        Test.Assert(ResolutionAtRuntimeClassParam.RuntimeResolvedFunc<ResolutionAtRuntimeBase, string>(100) == 123, "Expecting ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc<ResolutionAtRuntimeBase, object> to land on ResolutionAtRuntimeBase.FuncString");
    }

    public static int Main()
    {
        Negative();
        Positive();
        return Test.Ret();
    }
}

class Test
{
    private static bool Pass = true;

    public static int Ret()
    {
        return Pass ? 100 : 101;
    }

    public static void Assert(bool cond, string msg)
    {
        if (cond)
        {
            Console.WriteLine("PASS");
        }
        else
        {
            Console.WriteLine("FAIL: " + msg);
            Pass = false;
        }
    }
}
