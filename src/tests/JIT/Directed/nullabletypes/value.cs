// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// If the nullable type has a null value, Value throws a System.InvalidOperationException

#pragma warning disable 0168, 0649

using System;


interface BaseInter { }
interface GenInter<T> { }

struct Struct { }
struct ImplStruct : BaseInter { }
struct OpenGenImplStruct<T> : GenInter<T> { }
struct CloseGenImplStruct : GenInter<int> { }

class Foo { }

class NullableTest1
{
    public static int exceptionCounter = 0;
    //Nullable types with ?
    static int? i;
    static Struct? s;
    static ImplStruct? imps;
    static OpenGenImplStruct<Foo>? genfoo;
    static CloseGenImplStruct? genint;


    public static void Run()
    {
        try
        {
            Console.WriteLine(i.Value);
            Console.WriteLine("Test Failed at location {0}", exceptionCounter);
            exceptionCounter++;
        }
        catch (System.InvalidOperationException e) { }

        try
        {
            Console.WriteLine(s.Value);
            Console.WriteLine("Test Failed at location {0}", exceptionCounter);
            exceptionCounter++;
        }
        catch (System.InvalidOperationException e) { }

        try
        {
            Console.WriteLine(imps.Value);
            Console.WriteLine("Test Failed at location {0}", exceptionCounter);
            exceptionCounter++;
        }
        catch (System.InvalidOperationException e) { }

        try
        {
            Console.WriteLine(genfoo.Value);
            Console.WriteLine("Test Failed at location {0}", exceptionCounter);
            exceptionCounter++;
        }
        catch (System.InvalidOperationException e) { }

        try
        {
            Console.WriteLine(genint.Value);
            Console.WriteLine("Test Failed at location {0}", exceptionCounter);
            exceptionCounter++;
        }
        catch (System.InvalidOperationException e) { }
    }
}

class NullableTest3
{
    //Nullable types with ?
    static int? i = default(int);
    static Struct? s = new Struct();
    static ImplStruct? imps = new ImplStruct();
    static OpenGenImplStruct<Foo>? genfoo = new OpenGenImplStruct<Foo>();
    static CloseGenImplStruct? genint = new CloseGenImplStruct();


    public static void Run()
    {
        Test.Eval(i.Value, default(int));
        Test.Eval(s.Value, default(Struct));
        Test.Eval(imps.Value, default(ImplStruct));
        Test.Eval(genfoo.Value, default(OpenGenImplStruct<Foo>));
        Test.Eval(genint.Value, default(CloseGenImplStruct));
    }
}

class NullableTests
{
    public static void Run()
    {
        NullableTest1.Run();
        NullableTest3.Run();
    }
}
//</Code>

