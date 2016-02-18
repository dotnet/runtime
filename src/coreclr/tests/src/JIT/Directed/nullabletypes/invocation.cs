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
        Test.Eval(s.Value.Foo() == 0x0001);
        Test.Eval(((Struct)((object)s)).Foo() == 0x0001);
        Test.Eval(((Struct)((ValueType)s)).Foo() == 0x0001);

        Test.Eval(imps.Value.Foo() == 0x0010);
        Test.Eval(((ImplStruct)(object)imps).Foo() == 0x0010);
        Test.Eval(((ImplStruct)(ValueType)imps).Foo() == 0x0010);
        Test.Eval(((BaseInter)imps).Foo() == 0x0010);

        Test.Eval(ogis.Value.Foo() == 0x0100);
        Test.Eval(((OpenGenImplStruct<int>)(object)ogis).Foo() == 0x0100);
        Test.Eval(((OpenGenImplStruct<int>)(ValueType)ogis).Foo() == 0x0100);
        Test.Eval(((GenInter<int>)ogis).Foo() == 0x0100);

        Test.Eval(cgis.Value.Foo() == 0x1000);
        Test.Eval(((CloseGenImplStruct)(object)cgis).Foo() == 0x1000);
        Test.Eval(((CloseGenImplStruct)(ValueType)cgis).Foo() == 0x1000);
        Test.Eval(((GenInter<int>)cgis).Foo() == 0x1000);

        Test.Eval(cgiis.Value.Foo() == 0x1001);
        Test.Eval(((CloseGenImplGenAndImplStruct<int>)(object)cgiis).Foo() == 0x1001);
        Test.Eval(((CloseGenImplGenAndImplStruct<int>)(ValueType)cgiis).Foo() == 0x1001);
        Test.Eval(((GenInter<int>)cgiis).Foo() == 0x1001);
        Test.Eval(((BaseInter)cgiis).Foo() == 0x0110);
    }
}
