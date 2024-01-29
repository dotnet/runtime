// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.at_dynamic001.at_dynamic001
{
    // <Area> Implicitly Typed Local variables </Area>
    // <Title> Referring to @dynamic will always refer to the type and will give no warning </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            @dynamic x = new dynamic();
            if (!x.GetType().Equals(typeof(dynamic)))
            {
                return 1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamicfieldorlocal002.dynamicfieldorlocal002
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Can use var as the name of a local variable or field </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class C
    {
        private int _dynamic = 1;
        public int M()
        {
            _dynamic = 2;
            dynamic i = 3;
            return (int)i + _dynamic;
        }
    }

    public class D
    {
        public int M()
        {
            int dynamic = 4;
            dynamic i = 5;
            return (int)i + dynamic;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            C c = new C();
            D d = new D();
            if (c.M() != 5)
            {
                return -1;
            }

            if (d.M() != 9)
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared001.dynamictypedeclared001
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Referring to a type named var in a method body should not give a warning</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
        public int i = 0;
    }

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new dynamic();
            dynamic y = null;
            dynamic z;
            if (x.i != 0)
                return -1;
            if (y != null)
                return -1;
            JustToGetRidOfWarning(out z);
            return 0;
        }

        private static void JustToGetRidOfWarning(out dynamic z)
        {
            z = null;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared002.dynamictypedeclared002
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Returning a type named var from a function member should give no warnings</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
        public dynamic(int i)
        {
            this.i = i;
        }

        public int i = 0;
    }

    public class Test
    {
        private dynamic MyMethod(int i)
        {
            return new dynamic(i);
        }

        private dynamic _mydynamic;
        public dynamic MyProperty
        {
            get
            {
                return _mydynamic;
            }

            set
            {
                _mydynamic = value;
            }
        }

        public dynamic this[int index]
        {
            get
            {
                return new dynamic(index);
            }

            set
            { /* set the specified index to value here */
            }
        }

        public static dynamic operator +(Test i, int j)
        {
            return new dynamic(j);
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test test = new Test();
            if (test.MyMethod(1).i != 1)
            {
                return -1;
            }

            test.MyProperty = new dynamic(2);
            if (test.MyProperty.i != 2)
            {
                return -1;
            }

            if (test[3].i != 3)
            {
                return -1;
            }

            if ((test + 4).i != 4)
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared003.dynamictypedeclared003
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Function members containing formal parameters of type var should not give a warning</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
        public dynamic(int i)
        {
            this.i = i;
        }

        public int i = 0;
        public static int operator +(dynamic v1, dynamic v2)
        {
            return v1.i + v2.i;
        }
    }

    public class Test
    {
        private int MyMethod(dynamic v)
        {
            return v.i;
        }

        public int this[dynamic v]
        {
            get
            {
                return v.i;
            }

            set
            { /* set the specified index to value here */
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test test = new Test();
            @dynamic v1 = new dynamic(1);
            if (test.MyMethod(v1) != 1)
            {
                return -1;
            }

            @dynamic v2 = new dynamic(2);
            if (test[v2] != 2)
            {
                return -1;
            }

            @dynamic v3 = new dynamic(3);
            if ((v1 + v2) != 3)
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared004.dynamictypedeclared004
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type as type constraint gives no warnings</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
    }

    public class G<T>
        where T : dynamic
    {
        public R M<R>(R r) where R : dynamic
        {
            return r;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared005.dynamictypedeclared005
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type in a nullable type declaration gives no warnings </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public struct @dynamic
    {
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic? v = null;
            if (v != null)
            {
                return 1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared006.dynamictypedeclared006
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type in an array type gives no errors </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
    }

    public class D : dynamic
    {
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic[] dynamicr = new dynamic[4];
            if (dynamicr.Length != 4)
            {
                return -1;
            }

            dynamic[][] dynamicr2 = new dynamic[3][];
            if (dynamicr2.Length != 3)
            {
                return -1;
            }

            ;
            D x = new D();
            dynamic[,] dynamicr3 =
            {
            {
            x, x
            }

            , {
            x, x
            }

            , {
            x, x
            }
            }

            ;
            if (dynamicr3.Length != 6)
            {
                return -1;
            }

            ;
            if (!(dynamicr3[0, 0].Equals(x)))
            {
                return -1;
            }

            ;
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared007.dynamictypedeclared007
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type in a generic type declaration gives no warnings </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic<T>
    {
        public T GetT()
        {
            return default(T);
        }
    }

    public class @dynamic<T, S>
    {
        public T GetT()
        {
            return default(T);
        }

        public S GetS()
        {
            return default(S);
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic<int> v1 = new dynamic<int>();
            if (!v1.GetT().GetType().Equals(typeof(int)))
            {
                return -1;
            }

            dynamic<char, short> v2 = new dynamic<char, short>();
            if (!v2.GetT().GetType().Equals(typeof(char)))
            {
                return -1;
            }

            if (!v2.GetS().GetType().Equals(typeof(short)))
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared008.dynamictypedeclared008
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type in typeof expression gives no warnings</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
    }

    public class Test
    {
        private static dynamic s_v = new dynamic();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (!s_v.GetType().Equals(typeof(dynamic)))
            {
                return 1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared009.dynamictypedeclared009
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type as operand in 'as' or 'is' operator gives no warnings</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
    }

    public class Test
    {
        private static dynamic s_v1 = new dynamic();
        private static dynamic s_v2;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            s_v2 = s_v1 as dynamic;
            if (!(s_v1 is dynamic))
            {
                return -1;
            }

            if (!(s_v2 is dynamic))
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared010.dynamictypedeclared010
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type in cast expression gives no warnings</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
    }

    public class Test
    {
        private static dynamic s_v1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            object o = new dynamic();
            s_v1 = (dynamic)o;
            if (s_v1 != o)
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared011.dynamictypedeclared011
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var as a type argument gives no warnings </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
    }

    public class G<T>
        where T : new()
    {
        public T V = new T();
        public R M<R>() where R : new()
        {
            return new R();
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            G<dynamic> g = new G<dynamic>();
            if (!g.V.GetType().Equals(typeof(dynamic)))
            {
                return -1;
            }

            if (!g.M<dynamic>().GetType().Equals(typeof(dynamic)))
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared012.dynamictypedeclared012
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var as a type parameter gives no warnings</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class G<@dynamic>
        where dynamic : new()
    {
        public dynamic V = new dynamic();
    }

    public class C
    {
        public dynamic M<@dynamic>() where dynamic : new()
        {
            return new dynamic();
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            G<C> g = new G<C>();
            if (!g.V.GetType().Equals(typeof(C)))
            {
                return -1;
            }

            C c = new C();
            if (!c.M<C>().GetType().Equals(typeof(C)))
            {
                return -1;
            }

            NS.G2<NS.C2> g2 = new NS.G2<NS.C2>();
            if (!g2.V.GetType().Equals(typeof(NS.C2)))
            {
                return -1;
            }

            NS.C2 c2 = new NS.C2();
            if (!c2.M<NS.C2>().GetType().Equals(typeof(NS.C2)))
            {
                return -1;
            }

            return 0;
        }
    }

    namespace NS
    {
        public class @dynamic
        {
        }

        public class G2<@dynamic>
            where dynamic : new()
        {
            public dynamic V = new dynamic();
        }

        public class C2
        {
            public dynamic M<@dynamic>() where dynamic : new()
            {
                return new dynamic();
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared013.dynamictypedeclared013
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> When a type named var is defined, using var as a type parameter gives no warnings</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
    }

    public class G<@dynamic>
        where dynamic : new()
    {
        public dynamic V = new dynamic();
    }

    public class C
    {
        public dynamic M<@dynamic>() where dynamic : new()
        {
            return new dynamic();
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            G<C> g = new G<C>();
            if (!g.V.GetType().Equals(typeof(C)))
            {
                return -1;
            }

            C c = new C();
            if (!c.M<C>().GetType().Equals(typeof(C)))
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared014.dynamictypedeclared014
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type in a delegate type gives no warnings</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
    }

    internal delegate dynamic D(dynamic v);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            D d = delegate (dynamic v)
            {
                return new dynamic();
            }

            ;
            if (!d(new dynamic()).GetType().Equals(typeof(dynamic)))
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared015.dynamictypedeclared015
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type as the referent type in a pointer gives no warnings </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    //public struct dynamic
    //{
    //}
    //[TestClass]
    //public unsafe class Test
    //{
    //    [Test]
    //    [Priority(Priority.Priority2)]
    //    public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod()); }
    //    public static int MainMethod()
    //    {
    //        @dynamic v = new dynamic();
    //        dynamic* vp = &v;
    //        if (!vp->GetType().Equals(typeof(dynamic)))
    //        {
    //            return -1;
    //        }
    //        return 0;
    //    }
    //}
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared016.dynamictypedeclared016
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Declaring a const var gives no warning</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    internal enum @dynamic
    {
        foo,
        bar
    }

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            const dynamic x = dynamic.foo;
            if (!x.GetType().Equals(typeof(dynamic)))
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared017.dynamictypedeclared017
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type as the operand of the default operator </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class @dynamic
    {
    }

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            object v = default(dynamic);
            if (v != null)
            {
                return -1;
            }

            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared018.dynamictypedeclared018
{
    // <Area> Implicitly Typed Local Variables </Area>
    // <Title> Using var type as the type of an event </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            A a = new A();
            int c = 0;
            a.MyEvent += delegate (int i)
            {
                c += i;
            }

            ;
            a.MyEvent(1);
            a.MyEvent(2);
            if (c != 3)
            {
                return -1;
            }

            return 0;
        }

        private event dynamic MyEvent;
        private delegate void @dynamic(int i);
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared019.dynamictypedeclared019
{
    public class Test
    {
        public class @dynamic
        {
            public int Foo()
            {
                return 0;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            var d = new dynamic();
            return d.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared021.dynamictypedeclared021
{
    // <Title> Interaction between object and dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class App
    {
        public class @dynamic
        {
            public int Foo()
            {
                return 0;
            }
        }

        public class Test
        {
            public dynamic Bar()
            {
                return new dynamic();
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Test t = new Test();
            dynamic d = t.Bar();
            return d.Foo(); //This should not have anything dynamic
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamictypedeclared023.dynamictypedeclared023
{
    public class Test
    {
        public class @dynamic
        {
            public int Foo()
            {
                return 0;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new dynamic();
            return d.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.errorverifier.errorverifier
{
    public enum ErrorElementId
    {
        None,
        SK_METHOD, // method
        SK_CLASS, // type
        SK_NAMESPACE, // namespace
        SK_FIELD, // field
        SK_PROPERTY, // property
        SK_UNKNOWN, // element
        SK_VARIABLE, // variable
        SK_EVENT, // event
        SK_TYVAR, // type parameter
        SK_ALIAS, // using alias
        ERRORSYM, // <error>
        NULL, // <null>
        GlobalNamespace, // <global namespace>
        MethodGroup, // method group
        AnonMethod, // anonymous method
        Lambda, // lambda expression
        AnonymousType, // anonymous type
    }

    public enum ErrorMessageId
    {
        None,
        BadBinaryOps, // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'
        IntDivByZero, // Division by constant zero
        BadIndexLHS, // Cannot apply indexing with [] to an expression of type '{0}'
        BadIndexCount, // Wrong number of indices inside []; expected '{0}'
        BadUnaryOp, // Operator '{0}' cannot be applied to operand of type '{1}'
        NoImplicitConv, // Cannot implicitly convert type '{0}' to '{1}'
        NoExplicitConv, // Cannot convert type '{0}' to '{1}'
        ConstOutOfRange, // Constant value '{0}' cannot be converted to a '{1}'
        AmbigBinaryOps, // Operator '{0}' is ambiguous on operands of type '{1}' and '{2}'
        AmbigUnaryOp, // Operator '{0}' is ambiguous on an operand of type '{1}'
        ValueCantBeNull, // Cannot convert null to '{0}' because it is a non-nullable value type
        WrongNestedThis, // Cannot access a non-static member of outer type '{0}' via nested type '{1}'
        NoSuchMember, // '{0}' does not contain a definition for '{1}'
        ObjectRequired, // An object reference is required for the non-static field, method, or property '{0}'
        AmbigCall, // The call is ambiguous between the following methods or properties: '{0}' and '{1}'
        BadAccess, // '{0}' is inaccessible due to its protection level
        MethDelegateMismatch, // No overload for '{0}' matches delegate '{1}'
        AssgLvalueExpected, // The left-hand side of an assignment must be a variable, property or indexer
        NoConstructors, // The type '{0}' has no constructors defined
        BadDelegateConstructor, // The delegate '{0}' does not have a valid constructor
        PropertyLacksGet, // The property or indexer '{0}' cannot be used in this context because it lacks the get accessor
        ObjectProhibited, // Member '{0}' cannot be accessed with an instance reference; qualify it with a type name instead
        AssgReadonly, // A readonly field cannot be assigned to (except in a constructor or a variable initializer)
        RefReadonly, // A readonly field cannot be passed ref or out (except in a constructor)
        AssgReadonlyStatic, // A static readonly field cannot be assigned to (except in a static constructor or a variable initializer)
        RefReadonlyStatic, // A static readonly field cannot be passed ref or out (except in a static constructor)
        AssgReadonlyProp, // Property or indexer '{0}' cannot be assigned to -- it is read only
        AbstractBaseCall, // Cannot call an abstract base member: '{0}'
        RefProperty, // A property or indexer may not be passed as an out or ref parameter
        ManagedAddr, // Cannot take the address of, get the size of, or declare a pointer to a managed type ('{0}')
        FixedNotNeeded, // You cannot use the fixed statement to take the address of an already fixed expression
        UnsafeNeeded, // Dynamic calls cannot be used in conjunction with pointers
        BadBoolOp, // In order to be applicable as a short circuit operator a user-defined logical operator ('{0}') must have the same return type as the type of its 2 parameters
        MustHaveOpTF, // The type ('{0}') must contain declarations of operator true and operator false
        CheckedOverflow, // The operation overflows at compile time in checked mode
        ConstOutOfRangeChecked, // Constant value '{0}' cannot be converted to a '{1}' (use 'unchecked' syntax to override)
        AmbigMember, // Ambiguity between '{0}' and '{1}'
        SizeofUnsafe, // '{0}' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
        FieldInitRefNonstatic, // A field initializer cannot reference the non-static field, method, or property '{0}'
        CallingFinalizeDeprecated, // Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available.
        CallingBaseFinalizeDeprecated, // Do not directly call your base class Finalize method. It is called automatically from your destructor.
        BadCastInFixed, // The right hand side of a fixed statement assignment may not be a cast expression
        NoImplicitConvCast, // Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)
        InaccessibleGetter, // The property or indexer '{0}' cannot be used in this context because the get accessor is inaccessible
        InaccessibleSetter, // The property or indexer '{0}' cannot be used in this context because the set accessor is inaccessible
        BadArity, // Using the generic {1} '{0}' requires '{2}' type arguments
        BadTypeArgument, // The type '{0}' may not be used as a type argument
        TypeArgsNotAllowed, // The {1} '{0}' cannot be used with type arguments
        HasNoTypeVars, // The non-generic {1} '{0}' cannot be used with type arguments
        NewConstraintNotSatisfied, // '{2}' must be a non-abstract type with a public parameterless constructor in order to use it as parameter '{1}' in the generic type or method '{0}'
        GenericConstraintNotSatisfiedRefType, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no implicit reference conversion from '{3}' to '{1}'.
        GenericConstraintNotSatisfiedNullableEnum, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. The nullable type '{3}' does not satisfy the constraint of '{1}'.
        GenericConstraintNotSatisfiedNullableInterface, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. The nullable type '{3}' does not satisfy the constraint of '{1}'. Nullable types can not satisfy any interface constraints.
        GenericConstraintNotSatisfiedTyVar, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no boxing conversion or type parameter conversion from '{3}' to '{1}'.
        GenericConstraintNotSatisfiedValType, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no boxing conversion from '{3}' to '{1}'.
        TypeVarCantBeNull, // Cannot convert null to type parameter '{0}' because it could be a non-nullable value type. Consider using 'default({0})' instead.
        BadRetType, // '{1} {0}' has the wrong return type
        CantInferMethTypeArgs, // The type arguments for method '{0}' cannot be inferred from the usage. Try specifying the type arguments explicitly.
        MethGrpToNonDel, // Cannot convert method group '{0}' to non-delegate type '{1}'. Did you intend to invoke the method?
        RefConstraintNotSatisfied, // The type '{2}' must be a reference type in order to use it as parameter '{1}' in the generic type or method '{0}'
        ValConstraintNotSatisfied, // The type '{2}' must be a non-nullable value type in order to use it as parameter '{1}' in the generic type or method '{0}'
        CircularConstraint, // Circular constraint dependency involving '{0}' and '{1}'
        BaseConstraintConflict, // Type parameter '{0}' inherits conflicting constraints '{1}' and '{2}'
        ConWithValCon, // Type parameter '{1}' has the 'struct' constraint so '{1}' cannot be used as a constraint for '{0}'
        AmbigUDConv, // Ambiguous user defined conversions '{0}' and '{1}' when converting from '{2}' to '{3}'
        PredefinedTypeNotFound, // Predefined type '{0}' is not defined or imported
        PredefinedTypeBadType, // Predefined type '{0}' is declared incorrectly
        BindToBogus, // '{0}' is not supported by the language
        CantCallSpecialMethod, // '{0}': cannot explicitly call operator or accessor
        BogusType, // '{0}' is a type not supported by the language
        MissingPredefinedMember, // Missing compiler required member '{0}.{1}'
        LiteralDoubleCast, // Literal of type double cannot be implicitly converted to type '{1}'; use an '{0}' suffix to create a literal of this type
        UnifyingInterfaceInstantiations, // '{0}' cannot implement both '{1}' and '{2}' because they may unify for some type parameter substitutions
        ConvertToStaticClass, // Cannot convert to static type '{0}'
        GenericArgIsStaticClass, // '{0}': static types cannot be used as type arguments
        PartialMethodToDelegate, // Cannot create delegate from method '{0}' because it is a partial method without an implementing declaration
        IncrementLvalueExpected, // The operand of an increment or decrement operator must be a variable, property or indexer
        NoSuchMemberOrExtension, // '{0}' does not contain a definition for '{1}' and no extension method '{1}' accepting a first argument of type '{0}' could be found (are you missing a using directive or an assembly reference?)
        ValueTypeExtDelegate, // Extension methods '{0}' defined on value type '{1}' cannot be used to create delegates
        BadArgCount, // No overload for method '{0}' takes '{1}' arguments
        BadArgTypes, // The best overloaded method match for '{0}' has some invalid arguments
        BadArgType, // Argument '{0}': cannot convert from '{1}' to '{2}'
        RefLvalueExpected, // A ref or out argument must be an assignable variable
        BadProtectedAccess, // Cannot access protected member '{0}' via a qualifier of type '{1}'; the qualifier must be of type '{2}' (or derived from it)
        BindToBogusProp2, // Property, indexer, or event '{0}' is not supported by the language; try directly calling accessor methods '{1}' or '{2}'
        BindToBogusProp1, // Property, indexer, or event '{0}' is not supported by the language; try directly calling accessor method '{1}'
        BadDelArgCount, // Delegate '{0}' does not take '{1}' arguments
        BadDelArgTypes, // Delegate '{0}' has some invalid arguments
        AssgReadonlyLocal, // Cannot assign to '{0}' because it is read-only
        RefReadonlyLocal, // Cannot pass '{0}' as a ref or out argument because it is read-only
        ReturnNotLValue, // Cannot modify the return value of '{0}' because it is not a variable
        BadArgExtraRef, // Argument '{0}' should not be passed with the '{1}' keyword
        // DelegateOnConditional, // Cannot create delegate with '{0}' because it has a Conditional attribute (REMOVED)
        BadArgRef, // Argument '{0}' must be passed with the '{1}' keyword
        AssgReadonly2, // Members of readonly field '{0}' cannot be modified (except in a constructor or a variable initializer)
        RefReadonly2, // Members of readonly field '{0}' cannot be passed ref or out (except in a constructor)
        AssgReadonlyStatic2, // Fields of static readonly field '{0}' cannot be assigned to (except in a static constructor or a variable initializer)
        RefReadonlyStatic2, // Fields of static readonly field '{0}' cannot be passed ref or out (except in a static constructor)
        AssgReadonlyLocalCause, // Cannot assign to '{0}' because it is a '{1}'
        RefReadonlyLocalCause, // Cannot pass '{0}' as a ref or out argument because it is a '{1}'
        ThisStructNotInAnonMeth, // Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
        DelegateOnNullable, // Cannot bind delegate to '{0}' because it is a member of 'System.Nullable<T>'
        BadCtorArgCount, // '{0}' does not contain a constructor that takes '{1}' arguments
        BadExtensionArgTypes, // '{0}' does not contain a definition for '{1}' and the best extension method overload '{2}' has some invalid arguments
        BadInstanceArgType, // Instance argument: cannot convert from '{0}' to '{1}'
        BadArgTypesForCollectionAdd, // The best overloaded Add method '{0}' for the collection initializer has some invalid arguments
        InitializerAddHasParamModifiers, // The best overloaded method match '{0}' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
        NonInvocableMemberCalled, // Non-invocable member '{0}' cannot be used like a method.
        NamedArgumentSpecificationBeforeFixedArgument, // Named argument specifications must appear after all fixed arguments have been specified
        BadNamedArgument, // The best overload for '{0}' does not have a parameter named '{1}'
        BadNamedArgumentForDelegateInvoke, // The delegate '{0}' does not have a parameter named '{1}'
        DuplicateNamedArgument, // Named argument '{0}' cannot be specified multiple times
        NamedArgumentUsedInPositional, // Named argument '{0}' specifies a parameter for which a positional argument has already been given
    }

    public enum RuntimeErrorId
    {
        None,
        // RuntimeBinderInternalCompilerException
        InternalCompilerError, // An unexpected exception occurred while binding a dynamic operation
        // ArgumentException
        BindRequireArguments, // Cannot bind call with no calling object
        // RuntimeBinderException
        BindCallFailedOverloadResolution, // Overload resolution failed
        // ArgumentException
        BindBinaryOperatorRequireTwoArguments, // Binary operators must be invoked with two arguments
        // ArgumentException
        BindUnaryOperatorRequireOneArgument, // Unary operators must be invoked with one argument
        // RuntimeBinderException
        BindPropertyFailedMethodGroup, // The name '{0}' is bound to a method and cannot be used like a property
        // RuntimeBinderException
        BindPropertyFailedEvent, // The event '{0}' can only appear on the left hand side of += or -=
        // RuntimeBinderException
        BindInvokeFailedNonDelegate, // Cannot invoke a non-delegate type
        // ArgumentException
        BindImplicitConversionRequireOneArgument, // Implicit conversion takes exactly one argument
        // ArgumentException
        BindExplicitConversionRequireOneArgument, // Explicit conversion takes exactly one argument
        // ArgumentException
        BindBinaryAssignmentRequireTwoArguments, // Binary operators cannot be invoked with one argument
        // RuntimeBinderException
        BindBinaryAssignmentFailedNullReference, // Cannot perform member assignment on a null reference
        // RuntimeBinderException
        NullReferenceOnMemberException, // Cannot perform runtime binding on a null reference
        // RuntimeBinderException
        BindCallToConditionalMethod, // Cannot dynamically invoke method '{0}' because it has a Conditional attribute
        // RuntimeBinderException
        BindToVoidMethodButExpectResult, // Cannot implicitly convert type 'void' to 'object'
        // EE?
        EmptyDynamicView, // No further information on this object could be discovered
        // MissingMemberException
        GetValueonWriteOnlyProperty, // Write Only properties are not supported
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.variablenameddynamic001.variablenameddynamic001
{
    // <Title> Interaction between object and dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class App
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int dynamic = 3;
            dynamic d = dynamic;
            try
            {
                d.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.declarations.backwardscompatible.dynamicnsdeclared.dynamicnsdeclared
{
    // <Title>declare dynamic namespace</Title>
    // <Description>regression test
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(12,13\).*CS0219</Expects>
    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var x = 1; // OK
            dynamic y = 1; // error CS0118: 'dynamic' is a 'namespace' but is used like a 'type'
            return 0;
        }
    }

    namespace var
    {
    }

    namespace dynamic
    {
    }
    // </Code>
}
