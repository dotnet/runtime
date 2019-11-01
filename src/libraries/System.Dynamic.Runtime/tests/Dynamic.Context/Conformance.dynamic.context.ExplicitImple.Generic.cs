// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace Dynamic.Tests
{
    public class ExplicitlyImplementedGenericInterfaceTests
    {
        [Fact]
        public void NonGenericInterface_WithGenericMember1()
        {
            dynamic d = new ExplicitlyImplementedInterface1();
            Assert.Throws<RuntimeBinderException>(() => d.Foo<int>());

            var x = Helpers.Cast<InterfaceWithGenericMember>(d);
        }

        [Fact]
        public void NonGenericInterface_WithGenericMember2()
        {
            dynamic d = new ExplicitlyImplementedInterface2();
            Assert.Throws<RuntimeBinderException>(() => d.Foo<int>());
        }

        [Fact]
        public void GenericInterface_WithNonGenericMember()
        {
            dynamic d = new ExplicitlyImplementedGenericInterface();
            Assert.Equal(2, d.Foo());
            var x = Helpers.Cast<GenericInterfaceWithNonGenericMember<int>>(d);

            Assert.Throws<InvalidCastException>(() => Helpers.Cast<GenericInterfaceWithNonGenericMember<double>>(d));
        }

        [Fact]
        public void GenericInterface_WithGenericMember1()
        {
            dynamic d = new ExplicitlyImplementedGenericInterfaceWithGenericMember1();
            Assert.Throws<RuntimeBinderException>(() => d.Foo(1));

            var x = Helpers.Cast<GenericInterfaceWithGenericMember1<int>>(d);

            Assert.Throws<InvalidCastException>(() => Helpers.Cast<GenericInterfaceWithGenericMember1<double>>(d));
        }

        [Fact]
        public void GenericInterface_WithGenericMember2()
        {
            dynamic d = new ExplicitlyImplementedGenericInterfaceWithGenericMember2();
            Assert.Throws<RuntimeBinderException>(() => d.Foo<int>());

            var x = Helpers.Cast<GenericInterfaceWithGenericMember2<int>>(d);

            Assert.Throws<InvalidCastException>(() => Helpers.Cast<GenericInterfaceWithGenericMember2<double>>(d));
        }

        [Fact]
        public void GenericInterface_WithInGenericParameter_BaseClass()
        {
            dynamic d = new ExplicitlyImplementedGenericInInterface<BaseClass>();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            var x = Helpers.Cast<GenericInInterface<SubClass>>(d);
            var y = Helpers.Cast<GenericInInterface<BaseClass>>(d);
        }

        [Fact]
        public void GenericInterface_WithInGenericParameter_SubClass()
        {
            dynamic d = new ExplicitlyImplementedGenericInInterface<SubClass>();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            Assert.Throws<InvalidCastException>(() => Helpers.Cast<GenericInInterface<BaseClass>>(d));

            var y = Helpers.Cast<GenericInInterface<SubClass>>(d);
            Assert.Throws<InvalidCastException>(() => ((GenericInInterface<BaseClass>)d).Foo(new BaseClass()));
        }

        [Fact]
        public void GenericInterface_WithOutGenericParameter_BaseClass()
        {
            dynamic d = new ExplicitlyImplementedGenericOutInterface<BaseClass>();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            Assert.Throws<InvalidCastException>(() => Helpers.Cast<GenericOutInterface<SubClass>>(d));

            var y = Helpers.Cast<GenericOutInterface<BaseClass>>(d);

            Assert.Throws<InvalidCastException>(() => ((GenericOutInterface<SubClass>)d).Foo());
        }

        [Fact]
        public void GenericInterface_WithOutGenericParameter_SubClass()
        {
            dynamic d = new ExplicitlyImplementedGenericOutInterface<SubClass>();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            var x = Helpers.Cast<GenericOutInterface<SubClass>>(d);
            var y = Helpers.Cast<GenericOutInterface<BaseClass>>(d);
        }
    }

    public interface InterfaceWithGenericMember
    {
        int Foo<T>();
        int Bar();
    }

    public class ExplicitlyImplementedInterface1 : InterfaceWithGenericMember
    {
        int InterfaceWithGenericMember.Foo<T>() => 0;
        public int Bar() => 1;
    }

    public class ExplicitlyImplementedInterface2 : InterfaceWithGenericMember
    {
        int InterfaceWithGenericMember.Foo<T>() => 0;
        public int Foo() => 2;
        public int Bar() => 1;
    }

    public interface GenericInterfaceWithNonGenericMember<T>
    {
        int Foo();
        int Bar();
    }

    public class ExplicitlyImplementedGenericInterface : GenericInterfaceWithNonGenericMember<int>
    {
        int GenericInterfaceWithNonGenericMember<int>.Foo() => 0;
        public int Foo() => 2;
        public int Bar() => 1;
    }

    public interface GenericInterfaceWithGenericMember1<T>
    {
        int Foo(T t);
        int Bar();
    }

    public class ExplicitlyImplementedGenericInterfaceWithGenericMember1 : GenericInterfaceWithGenericMember1<int>
    {
        int GenericInterfaceWithGenericMember1<int>.Foo(int i) => 0;
        public int Bar() => 1;
    }

    public interface GenericInterfaceWithGenericMember2<T>
    {
        int Foo<U>();
        int Bar();
    }

    public class ExplicitlyImplementedGenericInterfaceWithGenericMember2 : GenericInterfaceWithGenericMember2<int>
    {
        int GenericInterfaceWithGenericMember2<int>.Foo<U>() => 0;
        public int Bar() => 1;
    }

    public class BaseClass { }
    public class SubClass : BaseClass { }

    public interface GenericInInterface<in T>
    {
        int Foo(T t);
    }

    public class ExplicitlyImplementedGenericInInterface<T> : GenericInInterface<T>
    {
        int GenericInInterface<T>.Foo(T t) => 0;
    }

    public interface GenericOutInterface<out T>
    {
        T Foo();
    }

    public class ExplicitlyImplementedGenericOutInterface<T> : GenericOutInterface<T> where T : new()
    {
        T GenericOutInterface<T>.Foo() => new T();
    }
}
