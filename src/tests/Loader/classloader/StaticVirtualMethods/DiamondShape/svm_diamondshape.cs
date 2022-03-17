// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

interface IFoo
{
    static int Foo(int a);
}

class IFoo_Impl
{
    static int Foo(int a)
    {
        return a;
    }
}

interface IFoo2 : IFoo
{
}

class IFoo2_Impl : IFoo
{
    static int IFoo.Foo(int a)
    {
        Console.WriteLine("At IFoo2.Foo");
        return a + 1;
    }
}

interface IFooEx : IFoo
{
}

class IFooEx_Impl : IFoo
{
    static int IFoo.Foo(int a)
    {
        Console.WriteLine("At IFooEx.Foo");
        return a + 2;
    }        
}

class FooClass : IFoo2, IFooEx
{
    // Dummy
    public static int Foo(int a)
    {
        return 0;
    }
}

interface I1
{
    static int Func(int a);
}

interface I2 : I1
{
    // static int I1.Func(int a) { return a + 2; }
}

interface I3 : I1
{
    // static int I1.Func(int a) { return a + 3; }
}

interface I4 : I2, I3
{
    // static int I1.Func(int a) { return a + 4; }
}

class I4Class : I4
{
    // @REMOVE
    static int I1.Func(int a)
    {
        Console.WriteLine("At I4Class.Func");
        return a + 4;
    }
}

interface I5: I1 
{
    // static int I1.Func(int a) { return a + 5; }
}

interface I6: I1 
{
    // static int I1.Func(int a) { return a + 6; }
}

interface I7: I5, I6
{
    // static int I1.Func(int a) { return a + 7; }
}

interface I8: I4, I7
{
    // static int I1.Func(int a) { return a + 8; }
}

class I47Class: I4, I7
{
    // @REMOVE
    static int I1.Func(int a)
    {
        Console.WriteLine("At I4Class.Func");
        return a + 8;
    }            

}

class I8Class: I8
{
    // @REMOVE
    static int I1.Func(int a)
    {
        Console.WriteLine("At I4Class.Func");
        return a + 8;
    }            
}

interface GI1<T>
{
    static int Func<S>(out Type[] types); 
}

interface GI2<T> : GI1<T>
{
    // static int GI1<T>.Func<S>(out Type[] types) { Console.WriteLine(typeof(T) + ", "typeof(S) + ", GI1Class"); types = new Type[] { typeof(T), typeof(S) }; return 2; }  

} 

interface GI3<T> : GI1<T>
{
    // static int GI1<T>.Func<S>(out Type[] types) { Console.WriteLine(typeof(T) + ", "typeof(S) + ", GI1Class"); types = new Type[] { typeof(T), typeof(S) }; return 3; }  
} 

interface GI4<T> : GI2<T>, GI3<T>
{
    // static int GI1<T>.Func<S>(out Type[] types) { Console.WriteLine(typeof(T) + ", "typeof(S) + ", GI1Class"); types = new Type[] { typeof(T), typeof(S) }; return 4; }  
} 

class GI23Class<T>: GI2<T>, GI3<T>
{
    // @REMOVE
    static int GI1<T>.Func<S>(out Type[] types) { Console.WriteLine(typeof(T) + ", " + typeof(S) + ", GI1Class"); types = new Type[] { typeof(T), typeof(S) }; return 4; }  
}

class GI4Class<T>: GI4<T>
{
    // @REMOVE
    static int GI1<T>.Func<S>(out Type[] types) { Console.WriteLine(typeof(T) + ", " + typeof(S) + ", GI1Class"); types = new Type[] { typeof(T), typeof(S) }; return 4; }  
}

class Program
{
    private static void CallFoo<T>(int value)
        : T is IFoo
    {
        T.Foo(value);
    }
    
    private static void CallI1Func<T>(int value)
        : T is I1
    {
        T.Func(value);
    }
    
    private static void CallGI1Func<T, U, V>(out Type[] types)
        : T is GI1<U>
    {
        T.Func<V>(out types);
    }
    
    public static void Negative()
    {
        Console.WriteLine("Calling IFoo.Foo on Foo - expecting exception.");
        try
        {
            CallFoo<FooClass>(10);
            Test.Assert(false, "Expecting exception on Foo");
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

        var gi23Class = new GI23Class<object>();
        GI1<object> gi1 = (GI1<object>) gi23Class;
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
    }

    public static void Positive()
    {
        Console.WriteLine("Calling I1.Func on I4Class - expecting I4.Func");

        Test.Assert(CallI1Func<I4Class>(10) == 14, "Expecting I1.Func to land on I4.Func");
        
        Console.WriteLine("Calling I1.Func on I8Class - expecting I8.Func");

        Test.Assert(CallI1Func<I8Class>(10) == 18, "Expecting I1.Func to land on I8.Func");

        Console.WriteLine("Calling GI1.Func on GI4Class<object> - expecting GI4.Func<S>");

        Type[] types;
        Test.Assert(CallGI1Func<GI4Class<object>, GI1<object>, string>(out types) == 4, "Expecting GI1<T>.Func to land on GII4<T>.Func<S>");
        Test.Assert(types[0] == typeof(object), "T must be object");
        Test.Assert(types[1] == typeof(string), "S must be string");  
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
        return Pass? 100 : 101;
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
