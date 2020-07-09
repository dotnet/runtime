// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace Dynamic.Tests
{
    public class ExplicitlyImplementedBasicTests
    {
        [Fact]
        public void NonGenericInterface_OneInterface_Class()
        {
            dynamic d = new OneExplicitlyImplementedNonGenericInterface1();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());
        }

        [Fact]
        public void NonGenericInterface_OneInterface_Struct()
        {
            dynamic d = new OneExplicitlyImplementedNonGenericInterface2();
            Assert.Equal(2, d.Foo());
        }

        [Fact]
        public void NonGenericInterface_TwoInterfaces_Class1()
        {
            dynamic d = new TwoExplicitlyImplementedNonGenericInterface1();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            var x = Helpers.Cast<NonGenericInterface2>(d);
        }

        [Fact]
        public void NonGenericInterface_TwoInterfaces_Class2()
        {
            dynamic d = new TwoExplicitlyImplementedNonGenericInterface2();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            var x = Helpers.Cast<NonGenericInterface3>(d);
        }

        [Fact]
        public void NonGenericInterface_OneInterfaceClass2()
        {
            dynamic d = new OneExplicitlyImplementedNonGenericInterface3();
            Assert.Equal(2, d.Foo());

            Assert.Throws<InvalidCastException>(() => Helpers.Cast<NonGenericInterface3>(d));
            Assert.Throws<InvalidCastException>(() => ((NonGenericInterface3)d).Foo());
        }

        [Fact]
        public void NonGenericInterface_WithGetter()
        {
            dynamic d = new ExplicitlyImplementedInterfaceWithGetter();
            Assert.Throws<RuntimeBinderException>(() => d.Property);

            var x = Helpers.Cast<InterfaceWithGetter>(d);
        }

        [Fact]
        public void NonGenericInterface_WithSetter()
        {
            dynamic d = new ExplicitlyImplementedInterfaceWithSetter();
            Assert.Throws<RuntimeBinderException>(() => d.Prop = 1);

            var x = Helpers.Cast<InterfaceWithSetter>(d);
        }

        [Fact]
        public void NonGenericInterface_WithGetterAndSetter()
        {
            dynamic d = new ExplicitlyImplementedInterfaceWithGetterAndSetter();
            Assert.Throws<RuntimeBinderException>(() => d.Property);
            Assert.Throws<RuntimeBinderException>(() => d.Property = 1);

            var x = Helpers.Cast<InterfaceWithGetterAndSetter>(d);
        }

        [Fact]
        public void NonGenericInterface_WithEvent()
        {
            dynamic d = new ExplicitlyImplementedInterfaceWithEvent();
            Assert.Throws<RuntimeBinderException>(() => d.Event += (Func<int, int>)(y => y));
            Assert.Throws<RuntimeBinderException>(() => d.Event -= (Func<int, int>)(y => y));
            var x = Helpers.Cast<InterfaceWithEvent>(d);
        }

        [Fact]
        public void NonGenericInterface_WithIndexer()
        {
            dynamic d = new ExplicitlyImplementedInterfaceWithIndexer();
            Assert.Throws<RuntimeBinderException>(() => d[0]);
            Assert.Throws<RuntimeBinderException>(() => d[0] = 1);

            var x = Helpers.Cast<InterfaceWithIndexer>(d);
        }
    }

    public interface NonGenericInterface1
    {
        int Foo();
        int Bar();
    }

    public interface NonGenericInterface2
    {
        int Bar();
    }

    public interface NonGenericInterface3
    {
        int Foo();
    }

    public class OneExplicitlyImplementedNonGenericInterface1 : NonGenericInterface1
    {
        int NonGenericInterface1.Foo() => 0;
        public int Bar() => 1;
    }

    public struct OneExplicitlyImplementedNonGenericInterface2 : NonGenericInterface1
    {
        int NonGenericInterface1.Foo() => 0;
        public int Foo() => 2;
        public int Bar() => 1;
    }

    public class OneExplicitlyImplementedNonGenericInterface3 : NonGenericInterface1
    {
        int NonGenericInterface1.Foo() => 0;
        public int Foo() => 2;
        public int Bar() => 1;
    }

    public class TwoExplicitlyImplementedNonGenericInterface1 : NonGenericInterface1, NonGenericInterface2
    {
        int NonGenericInterface1.Foo() => 0;
        public int Bar() => 1;
    }

    public class TwoExplicitlyImplementedNonGenericInterface2 : NonGenericInterface1, NonGenericInterface3
    {
        int NonGenericInterface1.Foo() => 0;
        int NonGenericInterface3.Foo() => 2;
        public int Bar() => 1;
    }

    public interface InterfaceWithSetter
    {
        int Property { set; }
    }

    public class ExplicitlyImplementedInterfaceWithSetter : InterfaceWithSetter
    {
        public static bool s_setterCalled;
        int InterfaceWithSetter.Property
        {
            set { s_setterCalled = true; }
        }
    }

    public interface InterfaceWithGetter
    {
        int Property { get; }
    }

    public class ExplicitlyImplementedInterfaceWithGetter : InterfaceWithGetter
    {
        public static bool s_getterCalled;
        int InterfaceWithGetter.Property
        {
            get { s_getterCalled = true;  return 2; }
        }
    }

    public interface InterfaceWithGetterAndSetter
    {
        int Property { get; set; }
    }

    public class ExplicitlyImplementedInterfaceWithGetterAndSetter : InterfaceWithGetterAndSetter
    {
        public static bool s_getterCalled;
        public static bool s_setterCalled;
        int InterfaceWithGetterAndSetter.Property
        {
            get { s_getterCalled = true; return 1; }
            set { s_setterCalled = true; }
        }
    }

    public interface InterfaceWithEvent
    {
        event Func<int, int> Event;
    }

    public class ExplicitlyImplementedInterfaceWithEvent : InterfaceWithEvent
    {
        public static bool s_addCalled;
        public static bool s_removeCalled;
        event Func<int, int> InterfaceWithEvent.Event
        {
            add { s_addCalled = true; }
            remove { s_addCalled = true; }
        }
    }

    public interface InterfaceWithIndexer
    {
        long this[byte index] { get; set; }
    }

    public class ExplicitlyImplementedInterfaceWithIndexer : InterfaceWithIndexer
    {
        public static bool s_getCalled;
        public static bool s_setCalled;
        long InterfaceWithIndexer.this[byte index]
        {
            get { s_getCalled = true; return 1; }
            set { s_setCalled = true; }
        }
    }
}
