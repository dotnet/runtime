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
        Test_nullabletypes.IsFalse(i.HasValue);
        i = null;
        Test_nullabletypes.IsFalse(i.HasValue);
        Test_nullabletypes.IsFalse(s.HasValue);
        s = null;
        Test_nullabletypes.IsFalse(s.HasValue);
        Test_nullabletypes.IsFalse(imps.HasValue);
        imps = null;
        Test_nullabletypes.IsFalse(imps.HasValue);
        Test_nullabletypes.IsFalse(genfoo.HasValue);
        genfoo = null;
        Test_nullabletypes.IsFalse(genfoo.HasValue);
        Test_nullabletypes.IsFalse(genint.HasValue);
        genint = null;
        Test_nullabletypes.IsFalse(genint.HasValue);
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
        Test_nullabletypes.Eval(i.HasValue);
        Test_nullabletypes.Eval(s.HasValue);
        Test_nullabletypes.Eval(imps.HasValue);
        Test_nullabletypes.Eval(genfoo.HasValue);
        Test_nullabletypes.Eval(genint.HasValue);
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
