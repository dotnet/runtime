// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//<Title>Nullable types have a default single-parameter constructor</Title>
//<Description>
// A nullable type can be created with a single argument constructor
// The HasValue property will be set to true, and the Value property will get the value of the constructor
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
        Test_nullabletypes.Eval(i.HasValue);
        Test_nullabletypes.Eval(i.Value, 1);
        Test_nullabletypes.Eval(s.HasValue);
        Test_nullabletypes.Eval(s.Value, default(Struct));
        Test_nullabletypes.Eval(imps.HasValue);
        Test_nullabletypes.Eval(imps.Value, default(ImplStruct));
        Test_nullabletypes.Eval(genfoo.HasValue);
        Test_nullabletypes.Eval(genfoo.Value, default(OpenGenImplStruct<Foo>));
        Test_nullabletypes.Eval(genint.HasValue);
        Test_nullabletypes.Eval(genint.Value, default(CloseGenImplStruct));
    }
}

class NullableTests
{
    public static void Run()
    {
        NullableTest1.Run();
    }
}
