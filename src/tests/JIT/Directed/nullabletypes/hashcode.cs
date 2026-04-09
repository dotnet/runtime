// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//<Title>Nullable types lift the GetHashCode() method from the underlying struct</Title>
//<Description>
//  A nullable type with a value returns the GetHashCode() from the underlying struct
//</Description>

#pragma warning disable 0649
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
    static int? i = new int?(1);
    static Struct? s = new Struct?(new Struct());
    static ImplStruct? imps = new ImplStruct?(new ImplStruct());
    static OpenGenImplStruct<Foo>? genfoo = new OpenGenImplStruct<Foo>?(new OpenGenImplStruct<Foo>());
    static CloseGenImplStruct? genint = new CloseGenImplStruct?(new CloseGenImplStruct());


    public static void Run()
    {
        Test_nullabletypes.Eval(i.GetHashCode(), 1.GetHashCode());
        Test_nullabletypes.Eval(s.GetHashCode(), default(Struct).GetHashCode());
        Test_nullabletypes.Eval(imps.GetHashCode(), default(ImplStruct).GetHashCode());
        Test_nullabletypes.Eval(genfoo.GetHashCode(), default(OpenGenImplStruct<Foo>).GetHashCode());
        Test_nullabletypes.Eval(genint.GetHashCode(), default(CloseGenImplStruct).GetHashCode());
    }
}

class NullableTest2
{
    static int? i;
    static Struct? s;
    static ImplStruct? imps;
    static OpenGenImplStruct<Foo>? genfoo;
    static CloseGenImplStruct? genint;


    public static void Run()
    {
        Test_nullabletypes.Eval(i.GetHashCode(), 0);
        Test_nullabletypes.Eval(s.GetHashCode(), 0);
        Test_nullabletypes.Eval(imps.GetHashCode(), 0);
        Test_nullabletypes.Eval(genfoo.GetHashCode(), 0);
        Test_nullabletypes.Eval(genint.GetHashCode(), 0);
    }
}

public class NullableTests
{
    public static void Run()
    {
        NullableTest1.Run();
        NullableTest2.Run();
    }
}
