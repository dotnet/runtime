// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//<Title>Nullable types have the HasValue property</Title>
//<Description>
// If the nullable type has a null value, HasValue is false
//</Description>


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
    static int? i;
    static Struct? s;
    static ImplStruct? imps;
    static OpenGenImplStruct<Foo>? genfoo;
    static CloseGenImplStruct? genint;

    public static void Run()
    {
        Test.IsFalse(i.HasValue);
        i = null;
        Test.IsFalse(i.HasValue);
        Test.IsFalse(s.HasValue);
        s = null;
        Test.IsFalse(s.HasValue);
        Test.IsFalse(imps.HasValue);
        imps = null;
        Test.IsFalse(imps.HasValue);
        Test.IsFalse(genfoo.HasValue);
        genfoo = null;
        Test.IsFalse(genfoo.HasValue);
        Test.IsFalse(genint.HasValue);
        genint = null;
        Test.IsFalse(genint.HasValue);
    }
}

class NullableTest2
{
    static int? i = 1;
    static Struct? s = new Struct();
    static ImplStruct? imps = new ImplStruct();
    static OpenGenImplStruct<Foo>? genfoo = new OpenGenImplStruct<Foo>();
    static CloseGenImplStruct? genint = new CloseGenImplStruct();


    public static void Run()
    {
        Test.Eval(i.HasValue);
        Test.Eval(s.HasValue);
        Test.Eval(imps.HasValue);
        Test.Eval(genfoo.HasValue);
        Test.Eval(genint.HasValue);
    }
}

class NullableTests
{
    public static void Run()
    {
        NullableTest1.Run();
        NullableTest2.Run();
    }
}
