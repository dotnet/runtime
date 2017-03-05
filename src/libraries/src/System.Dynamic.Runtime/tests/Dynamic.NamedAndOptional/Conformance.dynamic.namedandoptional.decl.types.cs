// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst01a.cnst01a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? e = +0)
        {
            return (int)e;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst01b.cnst01b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? e = +0)
        {
            return (int)e - 1;
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
            dynamic p = new Parent();
            dynamic d = 1;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst02a.cnst02a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? e = -0)
        {
            return (int)e;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst02b.cnst02b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? e = -0)
        {
            return (int)e + 1;
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
            dynamic p = new Parent();
            dynamic d = -1;
            return p.Foo(e: d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst03a.cnst03a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool? e = true)
        {
            if ((bool)e)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst03b.cnst03b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool? e = true)
        {
            if ((bool)e)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            dynamic d = true;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst04a.cnst04a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool? e = false)
        {
            if (!(bool)e)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst04b.cnst04b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool? e = false)
        {
            if (!(bool)e)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            dynamic d = false;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst05a.cnst05a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool? e = !true)
        {
            if (!(bool)e)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst05b.cnst05b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool? e = !true)
        {
            if (!(bool)e)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            dynamic d = false;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst06a.cnst06a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool? e = !true)
        {
            if (!(bool)e)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst06b.cnst06b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool? e = !true)
        {
            if (e == null)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            dynamic d = null;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst07a.cnst07a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? e = (0))
        {
            return (int)e;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst07b.cnst07b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? e = (0))
        {
            if (e == null)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            dynamic d = null;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst08a.cnst08a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(uint? e = (uint)(0))
        {
            return (int)e.Value;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst08b.cnst08b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(short? e = 0)
        {
            return (int)e.Value;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst08c.cnst08c
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(ulong? e = 0)
        {
            return (int)e.Value;
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
            dynamic p = new Parent();
            dynamic d = (ulong)0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst09a.cnst09a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? e = checked(0))
        {
            return (int)e;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst09b.cnst09b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? e = checked(0))
        {
            return (int)e;
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
            dynamic p = new Parent();
            dynamic d = 0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst10a.cnst10a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? e = unchecked(0))
        {
            return (int)e;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst10b.cnst10b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? e = unchecked(0))
        {
            return (int)e;
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
            dynamic p = new Parent();
            dynamic d = 0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst11a.cnst11a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool? e = (1 + 1) == 2 ? 45 < 65 && 23 > 12 : 1 != 2 * 3 | false)
        {
            if ((bool)e)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst11b.cnst11b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description>Testing different const expressions</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(bool? e = (1 + 1) == 2 ? 45 < 65 && 23 > 12 : 1 != 2 * 3 | false)
        {
            if (e.Value)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            dynamic d = true;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst12.cnst12
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>The default value is null</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    public enum E
    {
        e1,
        e2,
        e3
    }

    public struct S
    {
    }

    public struct GS<T>
    {
    }

    public interface I
    {
    }

    public interface GI<T>
    {
    }

    public delegate void D();
    public delegate void GD<T>();
    public class Parent<T>
        where T : class
    {
        public int M1(object p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M2(string p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M3(Test p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M4(I p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M5(D p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M6(T p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M7(Parent<T> p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M8(GI<T> p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M9(GD<T> p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M10(int[] p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M11(object[] p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M12(string[] p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M13(System.Collections.Generic.List<int> p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M20(int? p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M21(decimal? p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M22(System.DateTime? p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M23(E? p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M24(S? p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }

        public int M25(GS<T>? p1 = null)
        {
            if (p1 == null)
                return 0;
            else
                return 1;
        }
    }

    public class Test
    {
        public static int FailCount = 0;
        public static void Eval(dynamic result, string comment)
        {
            if ((int)result != 0)
            {
                FailCount++;
                System.Console.WriteLine("Test failed at {0}", comment);
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent<string>();
            Eval(p.M1(), "object typed parameter");
            Eval(p.M2(), "string typed parameter");
            Eval(p.M3(), "user defined class typed parameter");
            Eval(p.M4(), "user defined interface typed parameter");
            Eval(p.M5(), "delegate typed parameter");
            Eval(p.M6(), "type parameter typed parameter");
            Eval(p.M7(), "user defined generic class typed parameter");
            Eval(p.M8(), "user defined generic interface typed parameter");
            Eval(p.M9(), "user defined generic delegate typed parameter");
            Eval(p.M10(), "int[] typed parameter");
            Eval(p.M11(), "object[] typed parameter");
            Eval(p.M12(), "string[] typed parameter");
            Eval(p.M13(), "List<int> typed parameter");
            Eval(p.M20(), "int? typed parameter");
            Eval(p.M21(), "decimal? typed parameter");
            Eval(p.M22(), "Datetime? typed parameter");
            Eval(p.M23(), "enum nullable typed parameter");
            Eval(p.M24(), "struct nullable typed parameter");
            Eval(p.M25(), "generic struct nullable typed parameter");
            return FailCount;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.cnst12b.cnst12b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>The default value is null</Title>
    // <Description>The type of optional parameter is pointer type</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    //using System;
    //using Microsoft.CSharp.RuntimeBinder;
    //public enum E { e1, e2, e3 }
    //public struct S {}
    //public struct GS<T> {}
    //public unsafe class Parent
    //{
    //public int M1(void* p1 = null) { if (p1 == null) return 0; else return 1; }
    //public int M2(int* p1 = null) { if (p1 == null) return 0; else return 1; }
    //public int M3(E* p1 = null) { if (p1 == null) return 0; else return 1; }
    //public int M4(int** p1 = null) { if (p1 == null) return 0; else return 1; }
    //public int M5(S* p1 = null) { if (p1 == null) return 0; else return 1; }
    //}
    //[TestClass]public unsafe class Test
    //{
    //[Test][Priority(Priority.Priority2)]public void DynamicCSharpRunTest(){Assert.AreEqual(0, MainMethod());} public static int MainMethod()
    //{
    //int FailCount = 0;
    //dynamic p = new Parent();
    //try
    //{
    //p.M1();
    //FailCount ++;
    //System.Console.WriteLine("Test fail at void* typed parameter");
    //}
    //catch (RuntimeBinderException e)
    //{
    //bool ret = ErrorVerifier.Verify(ErrorMessageId.UnsafeNeeded, e.Message);
    //if (!ret)
    //{
    //FailCount++;
    //System.Console.WriteLine("Test fail at void* typed parameter[Error Message]");
    //}
    //}
    //try
    //{
    //p.M2();
    //FailCount ++;
    //System.Console.WriteLine("Test fail at int* typed parameter");
    //}
    //catch (RuntimeBinderException e)
    //{
    //bool ret = ErrorVerifier.Verify(ErrorMessageId.UnsafeNeeded, e.Message);
    //if (!ret)
    //{
    //FailCount++;
    //System.Console.WriteLine("Test fail at int* typed parameter[Error Message]");
    //}
    //}
    //try
    //{
    //p.M3();
    //FailCount ++;
    //System.Console.WriteLine("Test fail at pointer to enum typed parameter");
    //}
    //catch (RuntimeBinderException e)
    //{
    //bool ret = ErrorVerifier.Verify(ErrorMessageId.UnsafeNeeded, e.Message);
    //if (!ret)
    //{
    //FailCount++;
    //System.Console.WriteLine("Test fail at pointer to enum typed parameter[Error Message]");
    //}
    //}
    //try
    //{
    //p.M4();
    //FailCount ++;
    //System.Console.WriteLine("Test fail at int** typed parameter");
    //}
    //catch (RuntimeBinderException e)
    //{
    //bool ret = ErrorVerifier.Verify(ErrorMessageId.UnsafeNeeded, e.Message);
    //if (!ret)
    //{
    //FailCount++;
    //System.Console.WriteLine("Test fail at int** typed parameter[Error Message]");
    //}
    //}
    //try
    //{
    //p.M5();
    //FailCount ++;
    //System.Console.WriteLine("Test fail at pointer to struct typed parameter");
    //}
    //catch (RuntimeBinderException e)
    //{
    //bool ret = ErrorVerifier.Verify(ErrorMessageId.UnsafeNeeded, e.Message);
    //if (!ret)
    //{
    //FailCount++;
    //System.Console.WriteLine("Test fail at pointer to struct typed parameter[Error Message]");
    //}
    //}
    //return FailCount;
    //}
    //}
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable01.nullable01
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? d = 0)
        {
            return (int)d;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable01a.nullable01a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? d = 0)
        {
            return (int)d;
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
            dynamic p = new Parent();
            dynamic d = 0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable02.nullable02
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? d = null)
        {
            return (int)0;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable02a.nullable02a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(int? d = null)
        {
            return (int)0;
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
            dynamic p = new Parent();
            dynamic d = null;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable03.nullable03
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(long? d = 0)
        {
            return (int)d.Value;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable03a.nullable03a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(long? d = 0)
        {
            return (int)d.Value;
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
            dynamic p = new Parent();
            dynamic d = 0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable04.nullable04
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(long? d = null)
        {
            return (int)0;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable04a.nullable04a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(long? d = null)
        {
            return (int)0;
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
            dynamic p = new Parent();
            dynamic d = null;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable05.nullable05
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(uint? d = 0)
        {
            return (int)d.Value;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable05a.nullable05a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(uint? d = 0)
        {
            return (int)d.Value;
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
            dynamic p = new Parent();
            dynamic d = (uint)0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable06.nullable06
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(uint? d = null)
        {
            return (int)0;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable06a.nullable06a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(uint? d = null)
        {
            return (int)0;
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
            dynamic p = new Parent();
            dynamic d = null;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable07.nullable07
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(long? d = default(long?))
        {
            return (int)0;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable07a.nullable07a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(long? d = default(long?))
        {
            return (int)0;
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
            dynamic p = new Parent();
            dynamic d = default(long?);
            return p.Foo(d: d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable09.nullable09
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct S
    {
    }

    public class Parent
    {
        public int Foo<T>(T d = default(T))
        {
            return (int)0;
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
            dynamic p = new Parent();
            return p.Foo<S?>();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable09a.nullable09a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public struct S
    {
    }

    public class Parent
    {
        public int Foo<T>(T d = default(T))
        {
            return (int)0;
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
            dynamic p = new Parent();
            dynamic d = default(S?);
            return p.Foo<S?>(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable10.nullable10
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,12\).*CS0649</Expects>
    //<Expects Status=warning>\(12,13\).*CS0649</Expects>
    //<Expects Status=warning>\(13,15\).*CS0649</Expects>
    public struct S
    {
        public int x;
        public long y;
        public string s;
    }

    public class Parent
    {
        public int Foo<T>(T d = default(T))
        {
            return (int)0;
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
            dynamic p = new Parent();
            return p.Foo<S?>();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.nullable10a.nullable10a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,12\).*CS0649</Expects>
    //<Expects Status=warning>\(12,13\).*CS0649</Expects>
    //<Expects Status=warning>\(13,15\).*CS0649</Expects>
    public struct S
    {
        public int x;
        public long y;
        public string s;
    }

    public class Parent
    {
        public int Foo<T>(T d = default(T))
        {
            return (int)0;
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
            dynamic p = new Parent();
            dynamic d = default(S?);
            return p.Foo<S?>(d: d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type01.type01
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(double d = 0)
        {
            return (int)d;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type01a.type01a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(double d = 0)
        {
            return (int)d;
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
            Parent p = new Parent();
            dynamic d = 0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type01b.type01b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(double d = 0)
        {
            return (int)d - 1;
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
            Parent p = new Parent();
            dynamic d = 1;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type02.type02
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(double d = 0.0)
        {
            return (int)d;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type02a.type02a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(double d = 0.0)
        {
            return (int)d;
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
            Parent p = new Parent();
            dynamic d = 0.0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type02b.type02b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(double d = 0.0)
        {
            return (int)d - 1;
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
            Parent p = new Parent();
            dynamic d = 1.0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type03.type03
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(float d = 0.0f)
        {
            return (int)d;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type03a.type03a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(float d = 0.0f)
        {
            return (int)d;
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
            Parent p = new Parent();
            dynamic d = 0.0f;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type03b.type03b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(float d = 0.0f)
        {
            return (int)d - 1;
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
            Parent p = new Parent();
            dynamic d = 1.0f;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type04.type04
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(float d = 0)
        {
            return (int)d;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type04a.type04a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(float d = 0)
        {
            return (int)d;
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
            Parent p = new Parent();
            dynamic d = 0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type04b.type04b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(float d = 0)
        {
            return (int)d - 1;
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
            Parent p = new Parent();
            dynamic d = 1;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type05.type05
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(decimal d = 0)
        {
            return (int)d;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type05a.type05a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(decimal d = 0)
        {
            return (int)d;
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
            Parent p = new Parent();
            dynamic d = 0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type05b.type05b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(decimal d = 0)
        {
            return (int)d - 1;
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
            Parent p = new Parent();
            dynamic d = 1;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type06.type06
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(decimal d = 0.0m)
        {
            return (int)d;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type06a.type06a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(decimal d = 0.0m)
        {
            return (int)d;
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
            Parent p = new Parent();
            dynamic d = 0.0m;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type06b.type06b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(decimal d = 0.0m)
        {
            return (int)d - 1;
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
            Parent p = new Parent();
            dynamic d = 1.0m;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type07.type07
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public System.Guid Foo(int d = 0, System.Guid g = default(System.Guid))
        {
            return g;
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
            dynamic p = new Parent();
            var x = p.Foo();
            if (x == default(System.Guid))
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type07a.type07a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public System.Guid Foo(int d = 0, System.Guid g = default(System.Guid))
        {
            return g;
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
            Parent p = new Parent();
            dynamic d = default(System.Guid);
            var x = p.Foo(g: d);
            if (x == default(System.Guid))
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type07b.type07b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public System.Guid Foo(int d = 0, System.Guid g = default(System.Guid))
        {
            return g;
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
            Parent p = new Parent();
            dynamic d = System.Guid.NewGuid();
            var x = p.Foo(d: 1, g: d);
            if (x == (System.Guid)d)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type08.type08
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public enum E
    {
        e1,
        e2,
        e3
    }

    public class Parent
    {
        public int Foo(E e = E.e1)
        {
            return (int)e;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type08a.type08a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public enum E
    {
        e1,
        e2,
        e3
    }

    public class Parent
    {
        public int Foo(E e = E.e1)
        {
            return (int)e;
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
            dynamic d = E.e1;
            Parent p = new Parent();
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type08b.type08b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public enum E
    {
        e1,
        e2,
        e3
    }

    public class Parent
    {
        public int Foo(E e = E.e1)
        {
            return (int)e - 1;
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
            dynamic d = E.e2;
            Parent p = new Parent();
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type09.type09
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public enum E
    {
        e1,
        e2,
        e3
    }

    public class Parent
    {
        public int Foo(E e = 0)
        {
            return (int)e;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type09a.type09a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public enum E
    {
        e1,
        e2,
        e3
    }

    public class Parent
    {
        public int Foo(E e = 0)
        {
            return (int)e;
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
            Parent p = new Parent();
            dynamic d = E.e1;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type09b.type09b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public enum E
    {
        e1,
        e2,
        e3
    }

    public class Parent
    {
        public int Foo(E e = 0)
        {
            return (int)e - 1;
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
            Parent p = new Parent();
            dynamic d = (E)1;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type10.type10
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public enum E
    {
        e1,
        e2,
        e3
    }

    public class Parent
    {
        public int Foo(E e = (E)0)
        {
            return (int)e;
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
            dynamic p = new Parent();
            return p.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type10a.type10a
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public enum E
    {
        e1,
        e2,
        e3
    }

    public class Parent
    {
        public int Foo(E e = (E)0)
        {
            return (int)e;
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
            Parent p = new Parent();
            dynamic d = (E)0;
            return p.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.types.type10b.type10b
{
    // <Area>Different types of allowed Optionals</Area>
    // <Title>Testing different types that are allowed as default parameters</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    public enum E
    {
        e1,
        e2,
        e3
    }

    public class Parent
    {
        public int Foo(E e = (E)0)
        {
            return (int)e - 1;
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
            Parent p = new Parent();
            dynamic d = (E)1;
            return p.Foo(d);
        }
    }
    // </Code>
}
