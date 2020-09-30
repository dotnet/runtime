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
        Test.Eval(i.HasValue);
        Test.Eval(i.Value, 1);
        Test.Eval(s.HasValue);
        Test.Eval(s.Value, default(Struct));
        Test.Eval(imps.HasValue);
        Test.Eval(imps.Value, default(ImplStruct));
        Test.Eval(genfoo.HasValue);
        Test.Eval(genfoo.Value, default(OpenGenImplStruct<Foo>));
        Test.Eval(genint.HasValue);
        Test.Eval(genint.Value, default(CloseGenImplStruct));
    }
}

class NullableTests
{
    public static void Run()
    {
        NullableTest1.Run();
    }
}
