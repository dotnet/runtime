// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//<Title>Nullable types lift the GetHashCode() method from the underlying struct</Title>
//<Description>
//  A nullable type with a value returns the GetHashCode() from the underlying struct
//</Description>


using System;


interface BaseInter
{
    int Foo();
}
interface GenInter<T> 
{
    int Foo();
}

struct Struct 
{
    public int Foo() { return 0x0001; }
}

struct ImplStruct : BaseInter 
{
    public int Foo() { return 0x0010; }
}

struct OpenGenImplStruct<T> : GenInter<T> 
{
    public int Foo() { return 0x0100; }
}

struct CloseGenImplStruct : GenInter<int>
{
    public int Foo() { return 0x1000; }
}

struct CloseGenImplGenAndImplStruct<T> : BaseInter, GenInter<int>
{
    public int Foo() { return 0x1001; }
    int BaseInter.Foo() { return 0x0110; }
}


class Foo { }

class NullableTests
{
    static Struct?  s = default(Struct);
    static ImplStruct? imps = default(ImplStruct);
    static OpenGenImplStruct<int>? ogis = default(OpenGenImplStruct<int>);
    static CloseGenImplStruct? cgis = default(CloseGenImplStruct);
    static CloseGenImplGenAndImplStruct<int>? cgiis = default(CloseGenImplGenAndImplStruct<int>);

    public static void Run()
    {
        Test_nullabletypes.Eval(s.Value.Foo() == 0x0001);
        Test_nullabletypes.Eval(((Struct)((object)s)).Foo() == 0x0001);
        Test_nullabletypes.Eval(((Struct)((ValueType)s)).Foo() == 0x0001);

        Test_nullabletypes.Eval(imps.Value.Foo() == 0x0010);
        Test_nullabletypes.Eval(((ImplStruct)(object)imps).Foo() == 0x0010);
        Test_nullabletypes.Eval(((ImplStruct)(ValueType)imps).Foo() == 0x0010);
        Test_nullabletypes.Eval(((BaseInter)imps).Foo() == 0x0010);

        Test_nullabletypes.Eval(ogis.Value.Foo() == 0x0100);
        Test_nullabletypes.Eval(((OpenGenImplStruct<int>)(object)ogis).Foo() == 0x0100);
        Test_nullabletypes.Eval(((OpenGenImplStruct<int>)(ValueType)ogis).Foo() == 0x0100);
        Test_nullabletypes.Eval(((GenInter<int>)ogis).Foo() == 0x0100);

        Test_nullabletypes.Eval(cgis.Value.Foo() == 0x1000);
        Test_nullabletypes.Eval(((CloseGenImplStruct)(object)cgis).Foo() == 0x1000);
        Test_nullabletypes.Eval(((CloseGenImplStruct)(ValueType)cgis).Foo() == 0x1000);
        Test_nullabletypes.Eval(((GenInter<int>)cgis).Foo() == 0x1000);

        Test_nullabletypes.Eval(cgiis.Value.Foo() == 0x1001);
        Test_nullabletypes.Eval(((CloseGenImplGenAndImplStruct<int>)(object)cgiis).Foo() == 0x1001);
        Test_nullabletypes.Eval(((CloseGenImplGenAndImplStruct<int>)(ValueType)cgiis).Foo() == 0x1001);
        Test_nullabletypes.Eval(((GenInter<int>)cgiis).Foo() == 0x1001);
        Test_nullabletypes.Eval(((BaseInter)cgiis).Foo() == 0x0110);
    }
}
