// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

public class Interfaces
{
    const int Pass = 100;
    const int Fail = -1;

    public static int Run()
    {
        if (TestInterfaceCache() == Fail)
            return Fail;

        if (TestAVInInterfaceCache() == Fail)
            return Fail;

        if (TestMultipleInterfaces() == Fail)
            return Fail;

        if (TestArrayInterfaces() == Fail)
            return Fail;

        if (TestVariantInterfaces() == Fail)
            return Fail;

        if (TestSpecialArrayInterfaces() == Fail)
            return Fail;

        if (TestIterfaceCallOptimization() == Fail)
            return Fail;

        TestPublicAndNonpublicDifference.Run();
        TestDefaultInterfaceMethods.Run();
        TestDefaultInterfaceVariance.Run();
        TestVariantInterfaceOptimizations.Run();
        TestSharedInterfaceMethods.Run();
        TestGenericAnalysis.Run();
        TestCovariantReturns.Run();
        TestDynamicInterfaceCastable.Run();
        TestStaticInterfaceMethodsAnalysis.Run();
        TestStaticInterfaceMethods.Run();
        TestSimpleStaticDefaultInterfaceMethods.Run();
        TestSimpleDynamicStaticVirtualMethods.Run();
        TestGenericDynamicStaticVirtualMethods.Run();
        TestVariantGenericDynamicStaticVirtualMethods.Run();
        TestStaticDefaultMethodAmbiguity.Run();
        TestMoreConstraints.Run();
        TestSimpleNonGeneric.Run();
        TestSimpleGeneric.Run();
        TestDefaultDynamicStaticNonGeneric.Run();
        TestDefaultDynamicStaticGeneric.Run();
        TestDynamicStaticGenericVirtualMethods.Run();

        return Pass;
    }

    private static MyInterface[] MakeInterfaceArray()
    {
        MyInterface[] itfs = new MyInterface[50];
        itfs[0] = new Foo0();
        itfs[1] = new Foo1();
        itfs[2] = new Foo2();
        itfs[3] = new Foo3();
        itfs[4] = new Foo4();
        itfs[5] = new Foo5();
        itfs[6] = new Foo6();
        itfs[7] = new Foo7();
        itfs[8] = new Foo8();
        itfs[9] = new Foo9();
        itfs[10] = new Foo10();
        itfs[11] = new Foo11();
        itfs[12] = new Foo12();
        itfs[13] = new Foo13();
        itfs[14] = new Foo14();
        itfs[15] = new Foo15();
        itfs[16] = new Foo16();
        itfs[17] = new Foo17();
        itfs[18] = new Foo18();
        itfs[19] = new Foo19();
        itfs[20] = new Foo20();
        itfs[21] = new Foo21();
        itfs[22] = new Foo22();
        itfs[23] = new Foo23();
        itfs[24] = new Foo24();
        itfs[25] = new Foo25();
        itfs[26] = new Foo26();
        itfs[27] = new Foo27();
        itfs[28] = new Foo28();
        itfs[29] = new Foo29();
        itfs[30] = new Foo30();
        itfs[31] = new Foo31();
        itfs[32] = new Foo32();
        itfs[33] = new Foo33();
        itfs[34] = new Foo34();
        itfs[35] = new Foo35();
        itfs[36] = new Foo36();
        itfs[37] = new Foo37();
        itfs[38] = new Foo38();
        itfs[39] = new Foo39();
        itfs[40] = new Foo40();
        itfs[41] = new Foo41();
        itfs[42] = new Foo42();
        itfs[43] = new Foo43();
        itfs[44] = new Foo44();
        itfs[45] = new Foo45();
        itfs[46] = new Foo46();
        itfs[47] = new Foo47();
        itfs[48] = new Foo48();
        itfs[49] = new Foo49();
        return itfs;
    }

    #region Interface Dispatch Cache Test

    private static int TestInterfaceCache()
    {
        MyInterface[] itfs = MakeInterfaceArray();

        StringBuilder sb = new StringBuilder();
        int counter = 0;
        for (int i = 0; i < 50; i++)
        {
            sb.Append(itfs[i].GetAString());
            counter += itfs[i].GetAnInt();
        }

        string expected = "Foo0Foo1Foo2Foo3Foo4Foo5Foo6Foo7Foo8Foo9Foo10Foo11Foo12Foo13Foo14Foo15Foo16Foo17Foo18Foo19Foo20Foo21Foo22Foo23Foo24Foo25Foo26Foo27Foo28Foo29Foo30Foo31Foo32Foo33Foo34Foo35Foo36Foo37Foo38Foo39Foo40Foo41Foo42Foo43Foo44Foo45Foo46Foo47Foo48Foo49";

        if (!expected.Equals(sb.ToString()))
        {
            Console.WriteLine("Concatenating strings from interface calls failed.");
            Console.Write("Expected: ");
            Console.WriteLine(expected);
            Console.Write(" Actual: ");
            Console.WriteLine(sb.ToString());
            return Fail;
        }

        if (counter != 1225)
        {
            Console.WriteLine("Summing ints from interface calls failed.");
            Console.WriteLine("Expected: 1225");
            Console.Write("Actual: ");
            Console.WriteLine(counter);
            return Fail;
        }

        return 100;
    }

    private static int TestAVInInterfaceCache()
    {
        MyInterface[] itfs = MakeInterfaceArray();

        MyInterface[] testArray = new MyInterface[itfs.Length * 2];

        for (int i = 0; i < itfs.Length; i++)
        {
            testArray[i * 2 + 1] = itfs[i];
        }

        int numExceptions = 0;

        // Make sure AV in dispatch helpers is translated to NullRef
        for (int i = 0; i < testArray.Length; i++)
        {
            try
            {
                testArray[i].GetAnInt();
            }
            catch (NullReferenceException)
            {
                numExceptions++;
            }
        }

        // Make sure there's no trouble with unwinding out of the dispatch helper
        InterfaceWithManyParameters testInstance = null;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                testInstance.ManyParameters(0, 0, 0, 0, 0, 0, 0, 0);
            }
            catch (NullReferenceException)
            {
                numExceptions++;
            }
            if (testInstance == null)
                testInstance = new ClassWithManyParameters();
            else
                testInstance = null;
        }

        return numExceptions == itfs.Length + 2 ? Pass : Fail;
    }

    interface MyInterface
    {
        int GetAnInt();
        string GetAString();
    }

    interface InterfaceWithManyParameters
    {
        int ManyParameters(int a, int b, int c, int d, int e, int f, int g, int h);
    }

    class ClassWithManyParameters : InterfaceWithManyParameters
    {
        public int ManyParameters(int a, int b, int c, int d, int e, int f, int g, int h) => 42;
    }

    class Foo0 : MyInterface { public int GetAnInt() { return 0; } public string GetAString() { return "Foo0"; } }
    class Foo1 : MyInterface { public int GetAnInt() { return 1; } public string GetAString() { return "Foo1"; } }
    class Foo2 : MyInterface { public int GetAnInt() { return 2; } public string GetAString() { return "Foo2"; } }
    class Foo3 : MyInterface { public int GetAnInt() { return 3; } public string GetAString() { return "Foo3"; } }
    class Foo4 : MyInterface { public int GetAnInt() { return 4; } public string GetAString() { return "Foo4"; } }
    class Foo5 : MyInterface { public int GetAnInt() { return 5; } public string GetAString() { return "Foo5"; } }
    class Foo6 : MyInterface { public int GetAnInt() { return 6; } public string GetAString() { return "Foo6"; } }
    class Foo7 : MyInterface { public int GetAnInt() { return 7; } public string GetAString() { return "Foo7"; } }
    class Foo8 : MyInterface { public int GetAnInt() { return 8; } public string GetAString() { return "Foo8"; } }
    class Foo9 : MyInterface { public int GetAnInt() { return 9; } public string GetAString() { return "Foo9"; } }
    class Foo10 : MyInterface { public int GetAnInt() { return 10; } public string GetAString() { return "Foo10"; } }
    class Foo11 : MyInterface { public int GetAnInt() { return 11; } public string GetAString() { return "Foo11"; } }
    class Foo12 : MyInterface { public int GetAnInt() { return 12; } public string GetAString() { return "Foo12"; } }
    class Foo13 : MyInterface { public int GetAnInt() { return 13; } public string GetAString() { return "Foo13"; } }
    class Foo14 : MyInterface { public int GetAnInt() { return 14; } public string GetAString() { return "Foo14"; } }
    class Foo15 : MyInterface { public int GetAnInt() { return 15; } public string GetAString() { return "Foo15"; } }
    class Foo16 : MyInterface { public int GetAnInt() { return 16; } public string GetAString() { return "Foo16"; } }
    class Foo17 : MyInterface { public int GetAnInt() { return 17; } public string GetAString() { return "Foo17"; } }
    class Foo18 : MyInterface { public int GetAnInt() { return 18; } public string GetAString() { return "Foo18"; } }
    class Foo19 : MyInterface { public int GetAnInt() { return 19; } public string GetAString() { return "Foo19"; } }
    class Foo20 : MyInterface { public int GetAnInt() { return 20; } public string GetAString() { return "Foo20"; } }
    class Foo21 : MyInterface { public int GetAnInt() { return 21; } public string GetAString() { return "Foo21"; } }
    class Foo22 : MyInterface { public int GetAnInt() { return 22; } public string GetAString() { return "Foo22"; } }
    class Foo23 : MyInterface { public int GetAnInt() { return 23; } public string GetAString() { return "Foo23"; } }
    class Foo24 : MyInterface { public int GetAnInt() { return 24; } public string GetAString() { return "Foo24"; } }
    class Foo25 : MyInterface { public int GetAnInt() { return 25; } public string GetAString() { return "Foo25"; } }
    class Foo26 : MyInterface { public int GetAnInt() { return 26; } public string GetAString() { return "Foo26"; } }
    class Foo27 : MyInterface { public int GetAnInt() { return 27; } public string GetAString() { return "Foo27"; } }
    class Foo28 : MyInterface { public int GetAnInt() { return 28; } public string GetAString() { return "Foo28"; } }
    class Foo29 : MyInterface { public int GetAnInt() { return 29; } public string GetAString() { return "Foo29"; } }
    class Foo30 : MyInterface { public int GetAnInt() { return 30; } public string GetAString() { return "Foo30"; } }
    class Foo31 : MyInterface { public int GetAnInt() { return 31; } public string GetAString() { return "Foo31"; } }
    class Foo32 : MyInterface { public int GetAnInt() { return 32; } public string GetAString() { return "Foo32"; } }
    class Foo33 : MyInterface { public int GetAnInt() { return 33; } public string GetAString() { return "Foo33"; } }
    class Foo34 : MyInterface { public int GetAnInt() { return 34; } public string GetAString() { return "Foo34"; } }
    class Foo35 : MyInterface { public int GetAnInt() { return 35; } public string GetAString() { return "Foo35"; } }
    class Foo36 : MyInterface { public int GetAnInt() { return 36; } public string GetAString() { return "Foo36"; } }
    class Foo37 : MyInterface { public int GetAnInt() { return 37; } public string GetAString() { return "Foo37"; } }
    class Foo38 : MyInterface { public int GetAnInt() { return 38; } public string GetAString() { return "Foo38"; } }
    class Foo39 : MyInterface { public int GetAnInt() { return 39; } public string GetAString() { return "Foo39"; } }
    class Foo40 : MyInterface { public int GetAnInt() { return 40; } public string GetAString() { return "Foo40"; } }
    class Foo41 : MyInterface { public int GetAnInt() { return 41; } public string GetAString() { return "Foo41"; } }
    class Foo42 : MyInterface { public int GetAnInt() { return 42; } public string GetAString() { return "Foo42"; } }
    class Foo43 : MyInterface { public int GetAnInt() { return 43; } public string GetAString() { return "Foo43"; } }
    class Foo44 : MyInterface { public int GetAnInt() { return 44; } public string GetAString() { return "Foo44"; } }
    class Foo45 : MyInterface { public int GetAnInt() { return 45; } public string GetAString() { return "Foo45"; } }
    class Foo46 : MyInterface { public int GetAnInt() { return 46; } public string GetAString() { return "Foo46"; } }
    class Foo47 : MyInterface { public int GetAnInt() { return 47; } public string GetAString() { return "Foo47"; } }
    class Foo48 : MyInterface { public int GetAnInt() { return 48; } public string GetAString() { return "Foo48"; } }
    class Foo49 : MyInterface { public int GetAnInt() { return 49; } public string GetAString() { return "Foo49"; } }

    #endregion

    #region Implicit Interface Test

    private static int TestMultipleInterfaces()
    {
        TestClass<int> testInt = new TestClass<int>(5);

        MyInterface myInterface = testInt as MyInterface;
        if (!myInterface.GetAString().Equals("TestClass"))
        {
            Console.Write("On type TestClass, MyInterface.GetAString() returned ");
            Console.Write(myInterface.GetAString());
            Console.WriteLine(" Expected: TestClass");
            return Fail;
        }


        if (myInterface.GetAnInt() != 1)
        {
            Console.Write("On type TestClass, MyInterface.GetAnInt() returned ");
            Console.Write(myInterface.GetAnInt());
            Console.WriteLine(" Expected: 1");
            return Fail;
        }

        Interface<int> itf = testInt as Interface<int>;
        if (itf.GetT() != 5)
        {
            Console.Write("On type TestClass, Interface<int>::GetT() returned ");
            Console.Write(itf.GetT());
            Console.WriteLine(" Expected: 5");
            return Fail;
        }

        return Pass;
    }

    interface Interface<T>
    {
        T GetT();
    }

    class TestClass<T> : MyInterface, Interface<T>
    {
        T _t;
        public TestClass(T t)
        {
            _t = t;
        }

        public T GetT()
        {
            return _t;
        }

        public int GetAnInt()
        {
            return 1;
        }

        public string GetAString()
        {
            return "TestClass";
        }
    }
    #endregion

    #region Array Interfaces Test
    private static int TestArrayInterfaces()
    {
        {
            object stringArray = new string[] { "A", "B", "C", "D" };

            Console.WriteLine("Testing IEnumerable<T> on array...");
            string result = String.Empty;
            foreach (var s in (System.Collections.Generic.IEnumerable<string>)stringArray)
                result += s;

            if (result != "ABCD")
            {
                Console.WriteLine("Failed.");
                return Fail;
            }
        }

        {
            object stringArray = new string[] { "A", "B", "C", "D" };

            Console.WriteLine("Testing IEnumerable on array...");
            string result = String.Empty;
            foreach (var s in (System.Collections.IEnumerable)stringArray)
                result += s;

            if (result != "ABCD")
            {
                Console.WriteLine("Failed.");
                return Fail;
            }
        }

        {
            object intArray = new int[5, 5];

            Console.WriteLine("Testing IList on MDArray...");
            if (((System.Collections.IList)intArray).Count != 25)
            {
                Console.WriteLine("Failed.");
                return Fail;
            }
        }

        return Pass;
    }
    #endregion

    #region Variant interface tests

    interface IContravariantInterface<in T>
    {
        string DoContravariant(T value);
    }

    interface ICovariantInterface<out T>
    {
        T DoCovariant(object value);
    }

    class TypeWithVariantInterfaces<T> : IContravariantInterface<T>, ICovariantInterface<T>
    {
        public string DoContravariant(T value)
        {
            return value.ToString();
        }

        public T DoCovariant(object value)
        {
            return value is T ? (T)value : default(T);
        }
    }

    static IContravariantInterface<string> s_contravariantObject = new TypeWithVariantInterfaces<object>();
    static ICovariantInterface<object> s_covariantObject = new TypeWithVariantInterfaces<string>();
    static IEnumerable<int> s_arrayCovariantObject = (IEnumerable<int>)(object)new uint[] { 5, 10, 15 };

    private static int TestVariantInterfaces()
    {
        if (s_contravariantObject.DoContravariant("Hello") != "Hello")
            return Fail;

        if (s_covariantObject.DoCovariant("World") as string != "World")
            return Fail;

        int sum = 0;
        foreach (var e in s_arrayCovariantObject)
            sum += e;

        if (sum != 30)
            return Fail;

        return Pass;
    }

    class SpecialArrayBase { }
    class SpecialArrayDerived : SpecialArrayBase { }

    // NOTE: ICollection is not a variant interface, but arrays can cast with it as if it was
    static ICollection<SpecialArrayBase> s_specialDerived = new SpecialArrayDerived[42];
    static ICollection<uint> s_specialInt = (ICollection<uint>)(object)new int[85];

    private static int TestSpecialArrayInterfaces()
    {
        if (s_specialDerived.Count != 42)
            return Fail;

        if (s_specialInt.Count != 85)
            return Fail;

        return Pass;
    }

    #endregion

    #region Interface call optimization tests

    public interface ISomeInterface
    {
        int SomeValue { get; }
    }

    public abstract class SomeAbstractBaseClass : ISomeInterface
    {
        public abstract int SomeValue { get; }
    }

    public class SomeClass : SomeAbstractBaseClass
    {
        public override int SomeValue
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return 14; }
        }
    }

    private static int TestIterfaceCallOptimization()
    {
        ISomeInterface test = new SomeClass();
        int v = test.SomeValue;
        return (v == 14) ? Pass : Fail;
    }

    #endregion

    class TestPublicAndNonpublicDifference
    {
        interface IFrobber
        {
            string Frob();
        }
        class ProtectedBase : IFrobber
        {
            string IFrobber.Frob() => "IFrobber.Frob";
            protected virtual string Frob() => "Base.Frob";
        }

        class ProtectedDerived : ProtectedBase, IFrobber
        {
            protected override string Frob() => "Derived.Frob";
        }

        class PublicBase : IFrobber
        {
            string IFrobber.Frob() => "IFrobber.Frob";
            public virtual string Frob() => "Base.Frob";
        }

        class PublicDerived : PublicBase, IFrobber
        {
            public override string Frob() => "Derived.Frob";
        }

        public static void Run()
        {
            IFrobber f1 = new PublicDerived();
            if (f1.Frob() != "Derived.Frob")
                throw new Exception();

            IFrobber f2 = new ProtectedDerived();
            if (f2.Frob() != "IFrobber.Frob")
                throw new Exception();
        }
    }

    class TestDefaultInterfaceMethods
    {
        interface IFoo
        {
            int GetNumber() => 42;
        }

        interface IBar : IFoo
        {
            int IFoo.GetNumber() => 43;
        }

        class Foo : IFoo { }
        class Bar : IBar { }

        class Baz : IFoo
        {
            public int GetNumber() => 100;
        }

        interface IFoo<T>
        {
            Type GetInterfaceType() => typeof(IFoo<T>);
        }

        class Foo<T> : IFoo<T> { }

        class Base : IFoo
        {
            int IFoo.GetNumber() => 100;
        }

        class Derived : Base, IBar { }

        public static void Run()
        {
            Console.WriteLine("Testing default interface methods...");

            if (((IFoo)new Foo()).GetNumber() != 42)
                throw new Exception();

            if (((IFoo)new Bar()).GetNumber() != 43)
                throw new Exception();

            if (((IFoo)new Baz()).GetNumber() != 100)
                throw new Exception();

            if (((IFoo)new Derived()).GetNumber() != 100)
                throw new Exception();

            if (((IFoo<object>)new Foo<object>()).GetInterfaceType() != typeof(IFoo<object>))
                throw new Exception();

            if (((IFoo<int>)new Foo<int>()).GetInterfaceType() != typeof(IFoo<int>))
                throw new Exception();
        }
    }

    class TestDefaultInterfaceVariance
    {
        class Foo : IVariant<string>, IVariant<object>
        {
            string IVariant<object>.Frob() => "Hello class";
        }

        interface IVariant<in T>
        {
            string Frob() => "Hello default";
        }

        public static void Run()
        {
            Console.WriteLine("Testing default interface variant ordering...");

            if (((IVariant<object>)new Foo()).Frob() != "Hello class")
                throw new Exception();
            if (((IVariant<string>)new Foo()).Frob() != "Hello class")
                throw new Exception();
            if (((IVariant<ValueType>)new Foo()).Frob() != "Hello class")
                throw new Exception();
        }
    }

    class TestSharedInterfaceMethods
    {
        interface IInnerValueGrabber
        {
            string GetInnerValue();
        }

        interface IFace<T> : IInnerValueGrabber
        {
            string GrabValue(T x) => $"'{GetInnerValue()}' over '{typeof(T)}' with '{x}'";
        }

        class Base<T> : IFace<T>, IInnerValueGrabber
        {
            public string InnerValue;

            public string GetInnerValue() => InnerValue;
        }

        class Derived<T, U> : Base<T>, IFace<U> { }

        struct Yadda : IFace<object>, IInnerValueGrabber
        {
            public string InnerValue;

            public string GetInnerValue() => InnerValue;
        }

        class Atom1 { public override string ToString() => "The Atom1"; }
        class Atom2 { public override string ToString() => "The Atom2"; }

        public static void Run()
        {
            Console.WriteLine("Testing default interface methods and shared code...");

            var x = new Derived<Atom1, Atom2>() { InnerValue = "My inner value" };
            string r1 = ((IFace<Atom1>)x).GrabValue(new Atom1());
            if (r1 != "'My inner value' over 'Interfaces+TestSharedInterfaceMethods+Atom1' with 'The Atom1'")
                throw new Exception();
            string r2 = ((IFace<Atom2>)x).GrabValue(new Atom2());
            if (r2 != "'My inner value' over 'Interfaces+TestSharedInterfaceMethods+Atom2' with 'The Atom2'")
                throw new Exception();

            IFace<object> o = new Yadda() { InnerValue = "SomeString" };
            string r3 = o.GrabValue("Hello there");
            if (r3 != "'SomeString' over 'System.Object' with 'Hello there'")
                throw new Exception();
        }
    }

    class TestGenericAnalysis
    {
        interface IInterface
        {
            string Method(object p);
        }

        interface IInterface<T>
        {
            string Method(T p);
        }

        class C1<T> : IInterface, IInterface<T>
        {
            public string Method(object p) => "Method(object)";
            public string Method(T p) => "Method(T)";
        }

        class C2<T> : IInterface, IInterface<T>
        {
            public string Method(object p) => "Method(object)";
            public string Method(T p) => "Method(T)";
        }

        class C3<T> : IInterface, IInterface<T>
        {
            public string Method(object p) => "Method(object)";
            public string Method(T p) => "Method(T)";
        }

        static IInterface s_c1 = new C1<object>();
        static IInterface<object> s_c2 = new C2<object>();
        static IInterface<object> s_c3a = new C3<object>();
        static IInterface s_c3b = new C3<object>();

        // Works around https://github.com/dotnet/runtime/issues/94399
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void Run()
        {
            if (s_c1.Method(null) != "Method(object)")
                throw new Exception();
            if (s_c2.Method(null) != "Method(T)")
                throw new Exception();
            if (s_c3a.Method(null) != "Method(T)")
                throw new Exception();
            if (s_c3b.Method(null) != "Method(object)")
                throw new Exception();
        }
    }

    class TestCovariantReturns
    {
        interface IFoo
        {
        }

        class Foo : IFoo
        {
            public readonly string State;
            public Foo(string state) => State = state;
        }

        class Base
        {
            public virtual IFoo GetFoo() => throw new NotImplementedException();
        }

        class Derived : Base
        {
            public override Foo GetFoo() => new Foo("Derived");
        }

        class SuperDerived : Derived
        {
            public override Foo GetFoo() => new Foo("SuperDerived");
        }

        class BaseWithUnusedVirtual
        {
            public virtual IFoo GetFoo() => throw new NotImplementedException();
        }

        class DerivedWithOverriddenUnusedVirtual : BaseWithUnusedVirtual
        {
            public override Foo GetFoo() => new Foo("DerivedWithOverriddenUnusedVirtual");
        }

        class SuperDerivedWithOverriddenUnusedVirtual : DerivedWithOverriddenUnusedVirtual
        {
            public override Foo GetFoo() => new Foo("SuperDerivedWithOverriddenUnusedVirtual");
        }

        interface IInterfaceWithCovariantReturn
        {
            IFoo GetFoo();
        }

        class ClassImplementingInterface : IInterfaceWithCovariantReturn
        {
            public virtual IFoo GetFoo() => throw new NotImplementedException();
        }

        class DerivedClassImplementingInterface : ClassImplementingInterface
        {
            public override Foo GetFoo() => new Foo("DerivedClassImplementingInterface");
        }

        public static void Run()
        {
            Console.WriteLine("Testing covariant returns...");

            {
                Base b = new Derived();
                if (((Foo)b.GetFoo()).State != "Derived")
                    throw new Exception();
            }

            {
                Base b = new SuperDerived();
                if (((Foo)b.GetFoo()).State != "SuperDerived")
                    throw new Exception();
            }

            {
                Derived d = new SuperDerived();
                if (d.GetFoo().State != "SuperDerived")
                    throw new Exception();
            }

            {
                DerivedWithOverriddenUnusedVirtual b = new DerivedWithOverriddenUnusedVirtual();
                if (b.GetFoo().State != "DerivedWithOverriddenUnusedVirtual")
                    throw new Exception();
            }

            {
                DerivedWithOverriddenUnusedVirtual b = new SuperDerivedWithOverriddenUnusedVirtual();
                if (b.GetFoo().State != "SuperDerivedWithOverriddenUnusedVirtual")
                    throw new Exception();
            }

            {
                IInterfaceWithCovariantReturn i = new DerivedClassImplementingInterface();
                if (((Foo)i.GetFoo()).State != "DerivedClassImplementingInterface")
                    throw new Exception();
            }
        }
    }

    class TestVariantInterfaceOptimizations
    {
        static IEnumerable<Other> s_others = (IEnumerable<Other>)(object)new This[3] { (This)33, (This)66, (This)1 };

        enum This : sbyte { }

        enum Other : sbyte { }

        sealed class MySealedClass { }

        interface IContravariantInterface<in T>
        {
            string DoContravariant(T value);
        }

        interface ICovariantInterface<out T>
        {
            T DoCovariant(object value);
        }

        class CoAndContravariantOverSealed : IContravariantInterface<object>, ICovariantInterface<MySealedClass>
        {
            public string DoContravariant(object value) => "Hello";
            public MySealedClass DoCovariant(object value) => null;
        }

        public static void Run()
        {
            Console.WriteLine("Testing variant optimizations...");

            int sum = 0;
            foreach (var other in s_others)
            {
                sum += (int)other;
            }

            if (sum != 100)
                throw new Exception();

            ICovariantInterface<object> i1 = new CoAndContravariantOverSealed();
            i1.DoCovariant(null);

            IContravariantInterface<MySealedClass> i2 = new CoAndContravariantOverSealed();
            i2.DoContravariant(null);
        }
    }

    class TestDynamicInterfaceCastable
    {
        class CastableClass<TInterface, TImpl> : IDynamicInterfaceCastable
        {
            RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
                => interfaceType.Equals(typeof(TInterface).TypeHandle) ? typeof(TImpl).TypeHandle : default;
            bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
                => interfaceType.Equals(typeof(TInterface).TypeHandle);
        }

        interface IInterface
        {
            string GetCookie();
        }

        [DynamicInterfaceCastableImplementation]
        interface IInterfaceCastableImpl : IInterface
        {
            string IInterface.GetCookie() => "IInterfaceCastableImpl";
        }

        [DynamicInterfaceCastableImplementation]
        interface IInterfaceCastableImpl<T> : IInterface
        {
            string IInterface.GetCookie() => typeof(T).Name;
        }

        interface IInterfaceImpl : IInterface
        {
            string IInterface.GetCookie() => "IInterfaceImpl";
        }

        [DynamicInterfaceCastableImplementation]
        interface IInterfaceIndirectCastableImpl : IInterfaceImpl { }

        public static void Run()
        {
            Console.WriteLine("Testing IDynamicInterfaceCastable...");

            {
                IInterface o = (IInterface)new CastableClass<IInterface, IInterfaceCastableImpl>();
                if (o.GetCookie() != "IInterfaceCastableImpl")
                    throw new Exception();
            }

            {
                IInterface o = (IInterface)new CastableClass<IInterface, IInterfaceImpl>();
                bool success = false;
                try
                {
                    o.GetCookie();
                }
                catch (InvalidOperationException)
                {
                    success = true;
                }
                if (!success)
                    throw new Exception();
            }

            {
                IInterface o = (IInterface)new CastableClass<IInterface, IInterfaceIndirectCastableImpl>();
                if (o.GetCookie() != "IInterfaceImpl")
                    throw new Exception();
            }

            {
                IInterface o = (IInterface)new CastableClass<IInterface, IInterfaceCastableImpl<int>>();
                if (o.GetCookie() != "Int32")
                    throw new Exception();
            }
        }
    }

    class TestStaticInterfaceMethodsAnalysis
    {
        interface IFoo
        {
            static abstract object Frob();
        }

        class Foo<T> : IFoo
        {
            static object IFoo.Frob() => new Gen<T>();
        }

        static object CallFrob<T>() where T : IFoo => T.Frob();

        class Gen<T> { }
        struct Struct1 { }
        struct Struct2 { }

        public static void Run()
        {
            CallFrob<Foo<object>>();
            Console.WriteLine(typeof(Foo<string>));

            CallFrob<Foo<Struct1>>();
            Console.WriteLine(typeof(Foo<Struct2>));
        }
    }

    class TestStaticInterfaceMethods
    {
        interface ISimple
        {
            static abstract string GetCookie();
            static abstract string GetCookieGeneric<T>();
        }

        class SimpleClass : ISimple
        {
            public static string GetCookie() => "SimpleClass";
            public static string GetCookieGeneric<T>() => $"SimpleClass.GetCookieGeneric<{typeof(T).Name}>";
        }

        struct SimpleStruct : ISimple
        {
            public static string GetCookie() => "SimpleStruct";
            public static string GetCookieGeneric<T>() => $"SimpleStruct.GetCookieGeneric<{typeof(T).Name}>";
        }

        struct SimpleGenericStruct<T> : ISimple
        {
            public static string GetCookie() => $"SimpleGenericStruct<{typeof(T).Name}>";
            public static string GetCookieGeneric<U>() => $"SimpleGenericStruct<{typeof(T).Name}>.GetCookieGeneric<{typeof(U).Name}>";
        }

        class SimpleGenericClass<T> : ISimple
        {
            public static string GetCookie() => $"SimpleGenericClass<{typeof(T).Name}>";
            public static string GetCookieGeneric<U>() => $"SimpleGenericClass<{typeof(T).Name}>.GetCookieGeneric<{typeof(U).Name}>";
        }

        interface IVariant<in T>
        {
            static abstract string WhichMethod(T param);
        }

        class SimpleVariant : IVariant<Base>
        {
            public static string WhichMethod(Base b) => "SimpleVariant.WhichMethod(Base)";
        }

        class SimpleVariantTwice : IVariant<Base>, IVariant<Mid>
        {
            public static string WhichMethod(Base b) => "SimpleVariantTwice.WhichMethod(Base)";
            public static string WhichMethod(Mid b) => "SimpleVariantTwice.WhichMethod(Mid)";
        }

        class VariantWithInheritanceBase : IVariant<Mid>
        {
            public static string WhichMethod(Mid b) => "VariantWithInheritanceBase.WhichMethod(Mid)";
        }

        class VariantWithInheritanceDerived : VariantWithInheritanceBase, IVariant<Base>
        {
            public static string WhichMethod(Base b) => "VariantWithInheritanceDerived.WhichMethod(Base)";
        }

        class GenericVariantWithInheritanceBase<T> : IVariant<T>
        {
            public static string WhichMethod(T b) => "GenericVariantWithInheritanceBase.WhichMethod(T)";
        }

        class GenericVariantWithInheritanceDerived<T> : GenericVariantWithInheritanceBase<T>, IVariant<T>
        {
            public static new string WhichMethod(T b) => $"GenericVariantWithInheritanceDerived.WhichMethod({typeof(T).Name})";
        }

        class GenericVariantWithHiddenBase : IVariant<Mid>
        {
            public static string WhichMethod(Mid b) => "GenericVariantWithHiddenBase.WhichMethod(Mid)";
        }

        class GenericVariantWithHiddenDerived<T> : GenericVariantWithHiddenBase, IVariant<T>
        {
            public static string WhichMethod(T b) => $"GenericVariantWithHiddenDerived.WhichMethod({typeof(T).Name})";
        }

        struct Struct { }
        class Base { }
        class Mid : Base { }
        class Derived : Mid { }


        static void TestSimpleInterface<T>(string expected) where T : ISimple
        {
            string actual = T.GetCookie();
            if (actual != expected)
            {
                throw new Exception($"{actual} != {expected}");
            }

            Func<string> del = T.GetCookie;
            actual = del();
            if (actual != expected)
            {
                throw new Exception($"{actual} != {expected}");
            }
        }

        static void TestSimpleInterfaceWithGenericMethod<T, U>(string expected) where T : ISimple
        {
            string actual = T.GetCookieGeneric<U>();
            if (actual != expected)
            {
                throw new Exception($"{actual} != {expected}");
            }

            Func<string> del = T.GetCookieGeneric<U>;
            actual = del();
            if (actual != expected)
            {
                throw new Exception($"{actual} != {expected}");
            }
        }

        static void TestVariantInterface<T, U>(string expected) where T : IVariant<U>
        {
            string actual = T.WhichMethod(default);
            if (actual != expected)
            {
                throw new Exception($"{actual} != {expected}");
            }

            Func<U, string> del = T.WhichMethod;
            actual = del(default);
            if (actual != expected)
            {
                throw new Exception($"{actual} != {expected}");
            }
        }

        public static void Run()
        {
            TestSimpleInterface<SimpleClass>("SimpleClass");
            TestSimpleInterface<SimpleStruct>("SimpleStruct");

            TestSimpleInterface<SimpleGenericClass<Base>>("SimpleGenericClass<Base>");
            TestSimpleInterface<SimpleGenericStruct<Base>>("SimpleGenericStruct<Base>");

            TestSimpleInterfaceWithGenericMethod<SimpleClass, Base>("SimpleClass.GetCookieGeneric<Base>");
            TestSimpleInterfaceWithGenericMethod<SimpleStruct, Base>("SimpleStruct.GetCookieGeneric<Base>");
            TestSimpleInterfaceWithGenericMethod<SimpleClass, Struct>("SimpleClass.GetCookieGeneric<Struct>");
            TestSimpleInterfaceWithGenericMethod<SimpleStruct, Struct>("SimpleStruct.GetCookieGeneric<Struct>");

            TestSimpleInterfaceWithGenericMethod<SimpleGenericClass<Base>, Base>("SimpleGenericClass<Base>.GetCookieGeneric<Base>");
            TestSimpleInterfaceWithGenericMethod<SimpleGenericStruct<Base>, Base>("SimpleGenericStruct<Base>.GetCookieGeneric<Base>");
            TestSimpleInterfaceWithGenericMethod<SimpleGenericClass<Base>, Struct>("SimpleGenericClass<Base>.GetCookieGeneric<Struct>");
            TestSimpleInterfaceWithGenericMethod<SimpleGenericStruct<Base>, Struct>("SimpleGenericStruct<Base>.GetCookieGeneric<Struct>");

            TestVariantInterface<SimpleVariant, Base>("SimpleVariant.WhichMethod(Base)");
            TestVariantInterface<SimpleVariant, Derived>("SimpleVariant.WhichMethod(Base)");

            TestVariantInterface<SimpleVariantTwice, Base>("SimpleVariantTwice.WhichMethod(Base)");
            TestVariantInterface<SimpleVariantTwice, Mid>("SimpleVariantTwice.WhichMethod(Mid)");
            TestVariantInterface<SimpleVariantTwice, Derived>("SimpleVariantTwice.WhichMethod(Base)");

            TestVariantInterface<VariantWithInheritanceDerived, Base>("VariantWithInheritanceDerived.WhichMethod(Base)");
            TestVariantInterface<VariantWithInheritanceDerived, Mid>("VariantWithInheritanceDerived.WhichMethod(Base)");
            TestVariantInterface<VariantWithInheritanceDerived, Derived>("VariantWithInheritanceDerived.WhichMethod(Base)");

            TestVariantInterface<GenericVariantWithInheritanceDerived<Base>, Base>("GenericVariantWithInheritanceDerived.WhichMethod(Base)");
            TestVariantInterface<GenericVariantWithInheritanceDerived<Base>, Mid>("GenericVariantWithInheritanceDerived.WhichMethod(Base)");
            TestVariantInterface<GenericVariantWithInheritanceDerived<Mid>, Mid>("GenericVariantWithInheritanceDerived.WhichMethod(Mid)");

            TestVariantInterface<GenericVariantWithHiddenDerived<Base>, Base>("GenericVariantWithHiddenDerived.WhichMethod(Base)");
            TestVariantInterface<GenericVariantWithHiddenDerived<Base>, Mid>("GenericVariantWithHiddenDerived.WhichMethod(Base)");
            TestVariantInterface<GenericVariantWithHiddenDerived<Mid>, Mid>("GenericVariantWithHiddenDerived.WhichMethod(Mid)");
            TestVariantInterface<GenericVariantWithHiddenDerived<Derived>, Mid>("GenericVariantWithHiddenBase.WhichMethod(Mid)");
        }
    }

    class TestSimpleStaticDefaultInterfaceMethods
    {
        interface IFoo
        {
            static virtual string GetCookie() => nameof(IFoo);
        }

        struct StructFooWithDefault : IFoo { }

        struct StructFooWithExplicit : IFoo
        {
            public static string GetCookie() => nameof(StructFooWithExplicit);
        }

        class ClassFooWithDefault : IFoo { }

        class ClassFooWithExplicit : IFoo
        {
            public static string GetCookie() => nameof(ClassFooWithExplicit);
        }

        interface IFoo<T>
        {
            static virtual string GetCookie() => $"IFoo<{typeof(T).Name}>";
        }

        struct StructFooWithDefault<T> : IFoo<T> { }

        struct StructFooWithExplicit<T> : IFoo<T>
        {
            public static string GetCookie() => $"StructFooWithExplicit<{typeof(T).Name}>";
        }

        class ClassFooWithDefault<T> : IFoo<T> { }

        class ClassFooWithExplicit<T> : IFoo<T>
        {
            public static string GetCookie() => $"ClassFooWithExplicit<{typeof(T).Name}>";
        }

        class Atom { }

        static string GetCookie<T>() where T : IFoo => T.GetCookie();
        static string GetCookie<T, U>() where T : IFoo<U> => T.GetCookie();

        public static void Run()
        {
            if (GetCookie<StructFooWithDefault>() != "IFoo")
                throw new Exception();

            if (GetCookie<StructFooWithExplicit>() != "StructFooWithExplicit")
                throw new Exception();

            if (GetCookie<ClassFooWithDefault>() != "IFoo")
                throw new Exception();

            if (GetCookie<ClassFooWithExplicit>() != "ClassFooWithExplicit")
                throw new Exception();

            if (GetCookie<StructFooWithDefault<Atom>, Atom>() != "IFoo<Atom>")
                throw new Exception();

            if (GetCookie<StructFooWithExplicit<Atom>, Atom>() != "StructFooWithExplicit<Atom>")
                throw new Exception();

            if (GetCookie<ClassFooWithDefault<Atom>, Atom>() != "IFoo<Atom>")
                throw new Exception();

            if (GetCookie<ClassFooWithExplicit<Atom>, Atom>() != "ClassFooWithExplicit<Atom>")
                throw new Exception();
        }
    }

    class TestSimpleDynamicStaticVirtualMethods
    {
        public interface IFoo
        {
            static abstract int CallMeDirect();
            static abstract int CallMeIndirect();
            static virtual int DefaultImplemented() => 42;
        }

        class Foo : IFoo
        {
            public static int CallMeDirect() => 2019;
            public static int CallMeIndirect() => 2022;
        }

        class FrobCaller<T> where T : IFoo
        {
            public static int CallDirect() => T.CallMeDirect();
            public static int CallIndirect()
            {
                Func<int> d = T.CallMeIndirect;
                return d();
            }
            public static int CallDefault() => T.DefaultImplemented();
        }

        static Type s_fooType = typeof(Foo);

        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType - Intentional")]
        public static void Run()
        {
            Type t = typeof(FrobCaller<>).MakeGenericType(s_fooType);

            {
                var mi = t.GetMethod("CallDirect");
                int result = (int)mi.Invoke(null, Array.Empty<object>());
                if (result != 2019)
                    throw new Exception();
            }

            {
                var mi = t.GetMethod("CallIndirect");
                int result = (int)mi.Invoke(null, Array.Empty<object>());
                if (result != 2022)
                    throw new Exception();
            }

            {
                var mi = t.GetMethod("CallDefault");
                int result = (int)mi.Invoke(null, Array.Empty<object>());
                if (result != 42)
                    throw new Exception();
            }
        }
    }

    class TestGenericDynamicStaticVirtualMethods
    {
        public interface IFoo<T>
        {
            static abstract (int, Type) CallMeDirect();
            static abstract (int, Type) CallMeIndirect();
            static virtual (int, Type) DefaultImplemented() => (42, typeof(T[]));
        }

        class Foo<T> : IFoo<T>
        {
            public static (int, Type) CallMeDirect() => (2019, typeof(T[,]));
            public static (int, Type) CallMeIndirect() => (2022, typeof(T[,,]));
        }

        class FrobCaller<T, U> where T : IFoo<U>
        {
            public static (int, Type) CallDirect() => T.CallMeDirect();
            public static (int, Type) CallIndirect()
            {
                Func<(int, Type)> d = T.CallMeIndirect;
                return d();
            }
            public static (int, Type) CallDefault() => T.DefaultImplemented();
        }

        class Wrapper<T>
        {
            public static (int, Type) CallDirect() => FrobCaller<Foo<T>, T>.CallDirect();
            public static (int, Type) CallIndirect() => FrobCaller<Foo<T>, T>.CallIndirect();
            public static (int, Type) CallDefault() => FrobCaller<Foo<T>, T>.CallDefault();
        }

        class Atom { }

        static Type s_atomType = typeof(Atom);

        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType - Intentional")]
        public static void Run()
        {
            Type t = typeof(Wrapper<>).MakeGenericType(s_atomType);

            {
                var mi = t.GetMethod("CallDirect");
                var result = ((int, Type))mi.Invoke(null, Array.Empty<object>());
                if (result.Item1 != 2019)
                    throw new Exception();
                if (result.Item2.GetArrayRank() != 2 || result.Item2.GetElementType() != s_atomType)
                    throw new Exception();
            }

            {
                var mi = t.GetMethod("CallIndirect");
                var result = ((int, Type))mi.Invoke(null, Array.Empty<object>());
                if (result.Item1 != 2022)
                    throw new Exception();
                if (result.Item2.GetArrayRank() != 3 || result.Item2.GetElementType() != s_atomType)
                    throw new Exception();
            }

            {
                var mi = t.GetMethod("CallDefault");
                var result = ((int, Type))mi.Invoke(null, Array.Empty<object>());
                if (result.Item1 != 42)
                    throw new Exception();
                if (result.Item2.GetArrayRank() != 1 || result.Item2.GetElementType() != s_atomType)
                    throw new Exception();
            }
        }
    }

    class TestVariantGenericDynamicStaticVirtualMethods
    {
        public interface IFoo<in T>
        {
            static abstract (int, Type) CallMeDirect();
            static abstract (int, Type) CallMeIndirect();
            static virtual (int, Type) DefaultImplemented() => (42, typeof(T[]));
        }

        class AbjectFail<T> : IFoo<T>
        {
            public static (int, Type) CallMeDirect() => (2019, typeof(T[,]));
            public static (int, Type) CallMeIndirect() => (2022, typeof(T[,,]));
        }

        class FrobCaller<T, U> where T : IFoo<U>
        {
            public static (int, Type) CallDirect() => T.CallMeDirect();
            public static (int, Type) CallIndirect()
            {
                Func<(int, Type)> d = T.CallMeIndirect;
                return d();
            }
            public static (int, Type) CallDefault() => T.DefaultImplemented();
        }

        class AtomBase { }
        class Atom : AtomBase { }

        static Type s_atomType = typeof(Atom);
        static Type s_atomBaseType = typeof(AtomBase);

        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType - Intentional")]
        public static void Run()
        {
            Type t = typeof(FrobCaller<,>).MakeGenericType(typeof(AbjectFail<AtomBase>), s_atomType);

            {
                var mi = t.GetMethod("CallDirect");
                var result = ((int, Type))mi.Invoke(null, Array.Empty<object>());
                if (result.Item1 != 2019)
                    throw new Exception();
                if (result.Item2.GetArrayRank() != 2 || result.Item2.GetElementType() != s_atomBaseType)
                    throw new Exception();
            }

            {
                var mi = t.GetMethod("CallIndirect");
                var result = ((int, Type))mi.Invoke(null, Array.Empty<object>());
                if (result.Item1 != 2022)
                    throw new Exception();
                if (result.Item2.GetArrayRank() != 3 || result.Item2.GetElementType() != s_atomBaseType)
                    throw new Exception();
            }

            {
                var mi = t.GetMethod("CallDefault");
                var result = ((int, Type))mi.Invoke(null, Array.Empty<object>());
                if (result.Item1 != 42)
                    throw new Exception();
                if (result.Item2.GetArrayRank() != 1 || result.Item2.GetElementType() != s_atomBaseType)
                    throw new Exception();
            }
        }
    }

    class TestStaticDefaultMethodAmbiguity
    {
        class Atom1 { }
        class Atom2 { }

        interface IFoo<T>
        {
            static abstract (Type, Type) DisambiguateMe();
        }

        interface IBar<T, U> : IFoo<U>
        {
            static (Type, Type) IFoo<U>.DisambiguateMe() => (typeof(T), typeof(U));
        }

        struct GenericStruct<T, U> : IBar<T[], U>
        {
        }

        static (Type, Type) CallTheCall<T, U>() where T : IFoo<U> => T.DisambiguateMe();
        static (Type, Type) DelegateTheCall<T, U>() where T : IFoo<U>
        {
            Func<(Type, Type)> d = T.DisambiguateMe;
            return d();
        }

        public static void Run()
        {
            {
                var results = CallTheCall<GenericStruct<Atom1, Atom2>, Atom2>();
                if (results.Item1 != typeof(Atom1[]) || results.Item2 != typeof(Atom2))
                    throw new Exception();
            }

            {
                var results = DelegateTheCall<GenericStruct<Atom1, Atom2>, Atom2>();
                if (results.Item1 != typeof(Atom1[]) || results.Item2 != typeof(Atom2))
                    throw new Exception();
            }
        }
    }

    class TestMoreConstraints
    {
        interface IFoo
        {
            void Frob();
        }

        struct GenericStruct<T> : IFoo
        {
            public int State;
            public void Frob() => State++;
        }

        class GenericClass<T> : IFoo
        {
            public int State;
            public void Frob() => State++;
        }

        static void DoFrob<T>(ref T theT, ref GenericStruct<T> theGenericStruct) where T : IFoo
        {
            theT.Frob();
            theGenericStruct.Frob();

            Action delT = theT.Frob;
            delT();

            Action delGenericStruct = theGenericStruct.Frob;
            delGenericStruct();
        }

        public static void Run()
        {
            GenericStruct<object> s1 = default;
            GenericStruct<GenericStruct<object>> s2 = default;

            DoFrob(ref s1, ref s2);

            if (s1.State != 1 || s2.State != 1)
                throw new Exception();

            var c1 = new GenericClass<object>();
            GenericStruct<GenericClass<object>> c2 = default;

            DoFrob(ref c1, ref c2);
            if (c1.State != 2 || c2.State != 1)
                throw new Exception();
        }
    }

    class TestSimpleNonGeneric
    {
        interface IFoo
        {
            static abstract int GetCookie(int val);
        }

        interface IBar : IFoo
        {
            static int IFoo.GetCookie(int val) => 1234 + val;
        }

        class SimpleClass : IBar { }
        struct SimpleStruct : IBar { }

        static int Call<T>(int val) where T : IFoo => T.GetCookie(val);

        static int CallIndirect<T>(int val) where T : IFoo
        {
            Func<int, int> del = T.GetCookie;
            return del(val);
        }

        public static void Run()
        {
            if (Call<SimpleClass>(1) != 1235)
                throw new Exception();
            if (Call<SimpleStruct>(2) != 1236)
                throw new Exception();
            if (CallIndirect<SimpleClass>(1) != 1235)
                throw new Exception();
            if (CallIndirect<SimpleStruct>(2) != 1236)
                throw new Exception();
        }
    }

    class TestSimpleGeneric
    {
        interface IFoo
        {
            static abstract (int, Type) GetCookie(int val);
        }

        interface IBar<T> : IFoo
        {
            static (int, Type) IFoo.GetCookie(int val) => (1234 + val, typeof(IBar<T>));
        }

        class SimpleClass : IBar<Atom1> { }
        struct SimpleStruct : IBar<Atom2> { }

        static (int, Type) Call<T>(int val) where T : IFoo => T.GetCookie(val);

        static (int, Type) CallIndirect<T>(int val) where T : IFoo
        {
            Func<int, (int, Type)> del = T.GetCookie;
            return del(val);
        }

        class Atom1 { }
        class Atom2 { }

        public static void Run()
        {
            if (Call<SimpleClass>(1) != (1235, typeof(IBar<Atom1>)))
                throw new Exception();
            if (Call<SimpleStruct>(2) != (1236, typeof(IBar<Atom2>)))
                throw new Exception();

            if (CallIndirect<SimpleClass>(1) != (1235, typeof(IBar<Atom1>)))
                throw new Exception();
            if (CallIndirect<SimpleStruct>(2) != (1236, typeof(IBar<Atom2>)))
                throw new Exception();
        }
    }

    class TestDefaultDynamicStaticNonGeneric
    {
        interface IFoo
        {
            abstract static string ImHungryGiveMeCookie();
        }

        interface IBar : IFoo
        {
            static string IFoo.ImHungryGiveMeCookie() => "IBar";
        }

        class Baz : IBar
        {
        }

        class Gen<T> where T : IFoo
        {
            public static string GrabCookie() => T.ImHungryGiveMeCookie();
        }

        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType - Intentional")]
        public static void Run()
        {
            var r = (string)typeof(Gen<>).MakeGenericType(typeof(Baz)).GetMethod("GrabCookie").Invoke(null, Array.Empty<object>());
            if (r != "IBar")
                throw new Exception(r);

            r = (string)typeof(Gen<>).MakeGenericType(typeof(IBar)).GetMethod("GrabCookie").Invoke(null, Array.Empty<object>());
            if (r != "IBar")
                throw new Exception(r);
        }
    }

    class TestDefaultDynamicStaticGeneric
    {
        class Atom1 { }
        class Atom2 { }

        interface IFoo
        {
            abstract static string ImHungryGiveMeCookie();
        }

        interface IBar<T> : IFoo
        {
            static string IFoo.ImHungryGiveMeCookie() => $"IBar<{typeof(T).Name}>";
        }

        class Baz<T> : IBar<T>
        {
        }

        class Gen<T> where T : IFoo
        {
            public static string GrabCookie() => T.ImHungryGiveMeCookie();
        }

        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType - Intentional")]
        public static void Run()
        {
            Activator.CreateInstance(typeof(Baz<>).MakeGenericType(typeof(Atom1)));

            var r = (string)typeof(Gen<>).MakeGenericType(typeof(Baz<>).MakeGenericType(typeof(Atom1))).GetMethod("GrabCookie").Invoke(null, Array.Empty<object>());
            if (r != "IBar<Atom1>")
                throw new Exception(r);

            r = (string)typeof(Gen<>).MakeGenericType(typeof(IBar<>).MakeGenericType(typeof(Atom2))).GetMethod("GrabCookie").Invoke(null, Array.Empty<object>());
            if (r != "IBar<Atom2>")
                throw new Exception(r);
        }
    }

    class TestDynamicStaticGenericVirtualMethods
    {
        interface IEntry
        {
            string Enter1<T>(string cookie) where T : ISimpleCall;
        }

        interface ISimpleCall
        {
            static virtual string Wrap<T>(string cookie) => $"ISimpleCall.Wrap<{typeof(T).Name}>({cookie})";
        }

        interface ISimpleCallOverride : ISimpleCall
        {
            static string ISimpleCall.Wrap<T>(string cookie) => $"ISimpleCallOverride.Wrap<{typeof(T).Name}>({cookie})";
        }

        interface ISimpleCallGenericOverride<U> : ISimpleCall
        {
            static string ISimpleCall.Wrap<T>(string cookie) => $"ISimpleCallGenericOverride<{typeof(U).Name}>.Wrap<{typeof(T).Name}>({cookie})";
        }

        class SimpleCallClass : ISimpleCall
        {
            public static string Wrap<T>(string cookie) => $"SimpleCall.Wrap<{typeof(T).Name}>({cookie})";
        }

        class SimpleCallGenericClass<U> : ISimpleCall
        {
            public static string Wrap<T>(string cookie) => $"SimpleCallGenericClass<{typeof(U).Name}>.Wrap<{typeof(T).Name}>({cookie})";
        }

        struct SimpleCallStruct<U> : ISimpleCall
        {
            public static string Wrap<T>(string cookie) => $"SimpleCallStruct<{typeof(U).Name}>.Wrap<{typeof(T).Name}>({cookie})";
        }

        class Entry : IEntry
        {
            public virtual string Enter1<T>(string cookie) where T : ISimpleCall
            {
                return T.Wrap<T>(cookie);
            }
        }

        class EnsureVirtualCall : Entry
        {
            public override string Enter1<T>(string cookie) => string.Empty;
        }

        static IEntry s_ensure = new EnsureVirtualCall();
        static IEntry s_entry = new Entry();

        public static void Run()
        {
            // Just to make sure this cannot be devirtualized.
            s_ensure.Enter1<ISimpleCall>("One");

            //Console.WriteLine(s_entry.Enter1<ISimpleCall>("One"));
            //Console.WriteLine(s_entry.Enter1<ISimpleCallOverride>("One"));
            //Console.WriteLine(s_entry.Enter1<ISimpleCallGenericOverride<object>>("One"));
            Console.WriteLine(s_entry.Enter1<SimpleCallClass>("One"));
            //Console.WriteLine(s_entry.Enter1<SimpleCallGenericClass<object>>("One"));
            Console.WriteLine(s_entry.Enter1<SimpleCallStruct<object>>("One"));
        }
    }
}
