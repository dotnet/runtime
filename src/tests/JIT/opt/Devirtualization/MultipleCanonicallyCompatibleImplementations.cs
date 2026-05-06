using System;
using System.Runtime.CompilerServices;
using Xunit;


public class MultipleCanonicallyCompatibleImplementations
{
    [Fact]
    public static int TestEntryPoint()
    {
        string atom1Call = Foo<Atom1>.Call();
        string atom2Call = Foo<Atom2>.Call();
        Console.WriteLine($"Atom1Call `{atom1Call}`");
        Console.WriteLine($"Atom2Call `{atom2Call}`");

        if (atom1Call != "FooBaseFooBaseFoo")
        {
            Console.WriteLine("Atom1Call should be FooBaseFooBaseFoo");
            return 1;
        }
        if (atom2Call != "FooFooFooBaseFoo")
        {
            Console.WriteLine("Atom2Call should be FooFooFooBaseFoo");
            return 2;
        }

        return 100;
    }
}

interface IFooable<T>
{
    public string DoFoo(T x);
}

class Base : IFooable<Atom2>
{
    string IFooable<Atom2>.DoFoo(Atom2 x) => "Base";
}

sealed class Foo<T> : Base, IFooable<T>
{
    string IFooable<T>.DoFoo(T x) => "Foo";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string Call()
    {
        var f = new Foo<T>();
        var fA1 = new Foo<Atom1>();
        var fA2 = new Foo<Atom2>();
        return ((IFooable<T>)f).DoFoo(default) + ((IFooable<Atom2>)f).DoFoo(null)
             + ((IFooable<Atom1>)fA1).DoFoo(default) + ((IFooable<Atom2>)fA1).DoFoo(null)
             + ((IFooable<Atom2>)fA2).DoFoo(default);
    }
}

class Atom1 { }
class Atom2 { }
