// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.implem.cnstraintegereger01.cnstraintegereger01
{
    // <Area>variance</Area>
    // <Title> Constraint checking</Title>
    // <Description> Constraint checking</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success> </Expects>
    // <Code>
    public interface iVariance<out T>
    {
        T Boo();
    }

    public class Variance<T> : iVariance<T> where T : Mammal
    {
        public T Boo()
        {
            return default(T);
        }
    }

    public class Animal
    {
    }

    public class Mammal : Animal
    {
    }

    public class Tiger : Mammal
    {
    }

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Variance<Tiger> v11 = new Variance<Tiger>();
            dynamic v12 = (iVariance<Animal>)v11;
            var x1 = v12.Boo();
            dynamic v21 = new Variance<Tiger>();
            dynamic v22 = (iVariance<Animal>)v21;
            var x2 = v22.Boo();
            dynamic v31 = new Variance<Tiger>();
            iVariance<Animal> v32 = v31;
            var x3 = v32.Boo();
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.Variance.implem.cnstraintegereger05.cnstraintegereger05
{
    // <Title>Generic constraints</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class P
    {
        public void Foo<T, S>() where T : S
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var x = new P();
            x.Foo<I<string>, I<object>>();
            dynamic y = x;
            y.Foo<I<string>, I<object>>();
            return 0;
        }
    }

    public interface I<out T>
    {
    }
    // </Code>
}
