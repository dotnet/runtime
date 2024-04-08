// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0067  // Unused events
#pragma warning disable 0649  // Uninitialized fields

namespace System.Reflection.Tests
{
    public static class TypeTests_PrefixingOnlyAllowedOnGetMember
    {
        [Fact]
        public static void TestGetEvent()
        {
            MemberInfo member;
            Type t = typeof(TestClass).Project();
            member = t.GetEvent("My*", BindingFlags.Public | BindingFlags.Instance);
            Assert.Null(member);
        }

        [Fact]
        public static void TestGetField()
        {
            MemberInfo member;
            Type t = typeof(TestClass).Project();
            member = t.GetField("My*", BindingFlags.Public | BindingFlags.Instance);
            Assert.Null(member);
        }

        [Fact]
        public static void TestGetMethod()
        {
            MemberInfo member;
            Type t = typeof(TestClass).Project();
            member = t.GetMethod("My*", BindingFlags.Public | BindingFlags.Instance);
            Assert.Null(member);
        }

        [Fact]
        public static void TestGetNestedType()
        {
            MemberInfo member;
            Type t = typeof(TestClass).Project();
            member = t.GetNestedType("My*", BindingFlags.Public | BindingFlags.Instance);
            Assert.Null(member);
        }

        [Fact]
        public static void TestGetProperty()
        {
            MemberInfo member;
            Type t = typeof(TestClass).Project();
            member = t.GetProperty("My*", BindingFlags.Public | BindingFlags.Instance);
            Assert.Null(member);
        }

        [Fact]
        public static void TestGetMemberAll()
        {
            Type t = typeof(TestClass).Project();

            MemberInfo[] members = t.GetMember("My*", BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(5, members.Length);
        }

        [Fact]
        public static void TestGetMemberEvent()
        {
            Type t = typeof(TestClass).Project();

            MemberInfo[] members = t.GetMember("My*", MemberTypes.Event, BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(1, members.Length);
            Assert.Equal("MyEvent", members[0].Name);
        }

        [Fact]
        public static void TestGetMemberField()
        {
            Type t = typeof(TestClass).Project();

            MemberInfo[] members = t.GetMember("My*", MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(1, members.Length);
            Assert.Equal("MyField", members[0].Name);
        }

        [Fact]
        public static void TestGetMemberMethod()
        {
            Type t = typeof(TestClass).Project();

            MemberInfo[] members = t.GetMember("My*", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(1, members.Length);
            Assert.Equal("MyMethod", members[0].Name);
        }

        [Fact]
        public static void TestGetMemberNestedType()
        {
            Type t = typeof(TestClass).Project();

            MemberInfo[] members = t.GetMember("My*", MemberTypes.NestedType, BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(1, members.Length);
            Assert.Equal("MyNestedType", members[0].Name);
        }

        [Fact]
        public static void TestGetMemberProperty()
        {
            Type t = typeof(TestClass).Project();

            MemberInfo[] members = t.GetMember("My*", MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(1, members.Length);
            Assert.Equal("MyProperty", members[0].Name);
        }

        private class TestClass
        {
            public event Action MyEvent { add { } remove { } }
            public int MyField;
            public void MyMethod() { }
            public class MyNestedType { }
            public int MyProperty { get; }
        }
    }


    public static class TypeTests_HiddenEvents
    {
        [Fact]
        public static void GetEventHidesEventsBySimpleNameCompare()
        {
            Type t = typeof(Derived).Project();
            EventInfo[] es = t.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            Assert.Equal(4, es.Length);
            int count = 0;
            foreach (EventInfo e in es)
            {
                if (e.DeclaringType.Equals(typeof(Base).Project()))
                    count++;
            }
            Assert.Equal(0, count);
        }

        private class Base
        {
            public event Action MyEvent { add { } remove { } }
            public static event Action MyStaticEvent { add { } remove { } }
            public event Action MyEventInstanceStatic { add { } remove { } }
            public static event Action MyEventStaticInstance { add { } remove { } }
        }

        private class Derived : Base
        {
            public new event Action<int> MyEvent { add { } remove { } }
            public static new event Action<double> MyStaticEvent { add { } remove { } }
            public static new event Action<float> MyEventInstanceStatic { add { } remove { } }
            public new event Action<long> MyEventStaticInstance { add { } remove { } }
        }
    }

    public static class TypeTests_HiddenFields
    {
        [Fact]
        public static void GetFieldDoesNotHideHiddenFields()
        {
            Type t = typeof(Derived).Project();
            FieldInfo[] fs = t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            Assert.Equal(4, fs.Length);
            int count = 0;
            foreach (FieldInfo f in fs)
            {
                if (f.DeclaringType.Equals(typeof(Base).Project()))
                    count++;
            }
            Assert.Equal(2, count);
        }

        private class Base
        {
            public int MyField;
            public static int MyStaticField;
        }

        private class Derived : Base
        {
            public new int MyField;
            public static new int MyStaticField;
        }
    }


    public static class TypeTests_HiddenMethods
    {
        [Fact]
        public static void GetMethodDoesNotHideHiddenMethods()
        {
            Type t = typeof(Derived).Project();
            MethodInfo[] ms = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            int count = 0;
            foreach (MethodInfo m in ms)
            {
                if (m.DeclaringType.Equals(typeof(Base).Project()))
                    count++;
            }
            Assert.Equal(2, count);
        }

        private class Base
        {
            public int MyMethod() { throw null!; }
            public static int MyStaticMethod() { throw null!; }
        }

        private class Derived : Base
        {
            public new int MyMethod() { throw null!; }
            public static new int MyStaticMethod() { throw null!; }
        }
    }

    public static class TypeTests_HiddenProperties
    {
        [Fact]
        public static void GetPropertyHidesPropertiesByNameAndSigAndCallingConventionCompare()
        {
            Type t = typeof(Derived).Project();
            PropertyInfo[] ps = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            List<string> names = new List<string>();
            foreach (PropertyInfo p in ps)
            {
                if (p.DeclaringType.Equals(typeof(Base).Project()))
                {
                    names.Add(p.Name);
                }
            }

            names.Sort();
            string[] expected = [ "Item", nameof(Base.MyInstanceThenStaticProp), nameof(Base.MyStaticThenInstanceProp), nameof(Base.MyStringThenDoubleProp) ];
            Assert.Equal(expected, names.ToArray());
        }

        private abstract class Base
        {
            public int MyProp { get; }  // will get hidden
            public static int MyStaticProp { get; }  // will get hidden
            public int MyInstanceThenStaticProp { get; } // won't get hidden (calling convention mismatch)
            public static int MyStaticThenInstanceProp { get; } // won't get hidden (calling convention mismatch)
            public string MyStringThenDoubleProp { get; } // won't get hidden (signature mismatch on return type)
            public abstract int this[int x] { get; } // won't get hidden (signature mismatch on parameter type)
        }

        private abstract class Derived : Base
        {
            public new int MyProp { get; }
            public static new int MyStaticProp { get; }
            public static new int MyInstanceThenStaticProp { get; }
            public new int MyStaticThenInstanceProp { get; }
            public new double MyStringThenDoubleProp { get; }
            public abstract int this[double x] { get; }
        }
    }

    public static class TypeTests_HiddenTestingOrder
    {
        [Fact]
        public static void HideDetectionHappensBeforeBindingFlagChecks()
        {
            // Hiding members suppress results even if the hiding member itself is filtered out by the binding flags.
            Type derived = typeof(Derived).Project();
            EventInfo[] events = derived.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            Assert.Equal(0, events.Length);

            PropertyInfo[] properties = derived.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            Assert.Equal(0, properties.Length);
        }

        [Fact]
        public static void HideDetectionHappensAfterPrivateInBaseClassChecks()
        {
            // Hiding members won't suppress results if the hiding member is filtered out due to being a private member in a base class.
            Type derived2 = typeof(Derived2).Project();
            EventInfo[] events = derived2.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Assert.Equal(1, events.Length);
            Assert.Equal(typeof(Base).Project(), events[0].DeclaringType);
            Assert.Equal(nameof(Base.MyEvent), events[0].Name);

            PropertyInfo[] properties = derived2.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Assert.Equal(1, properties.Length);
            Assert.Equal(typeof(Base).Project(), properties[0].DeclaringType);
            Assert.Equal(nameof(Base.MyProp), properties[0].Name);
        }

        [Fact]
        public static void HideDetectionHappensBeforeStaticInNonFlattenedHierarchyChecks()
        {
            // Hiding members suppress results even if the hiding member is filtered out due to being a static member in a base class (and BindingFlags.FlattenHierarchy not being specified.)
            //  (that check is actually just another bindingflags check.)
            Type staticDerived2 = typeof(StaticDerived2).Project();
            EventInfo[] events = staticDerived2.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Assert.Equal(0, events.Length);
        }

        private abstract class Base
        {
            public event Action MyEvent { add { } remove { } }
            public int MyProp { get; }
        }

        private abstract class Derived : Base
        {
            private new event Action MyEvent { add { } remove { } }
            private new int MyProp { get; }
        }

        private abstract class Derived2 : Derived
        {
        }

        private class StaticBase
        {
            public event Action MyEvent { add { } remove { } }
        }

        private class StaticDerived : StaticBase
        {
            public static new event Action MyEvent { add { } remove { } }
        }

        private class StaticDerived2 : StaticDerived
        {
        }
    }


    public static class TypeTests_AmbiguityResolution_NoParameterBinding
    {
        [Fact]
        public static void EventsThrowAlways()
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;
            Type t = typeof(Derived).Project();

            Assert.Throws<AmbiguousMatchException>(() => t.GetEvent("myevent", bf));
        }

        [Fact]
        public static void NestedTypesThrowAlways()
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;
            Type t = typeof(Derived).Project();

            Assert.Throws<AmbiguousMatchException>(() => t.GetNestedType("myinner", bf));
        }

        [Fact]
        public static void PropertiesThrowAlways()
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;
            Type t = typeof(Derived).Project();

            Assert.Throws<AmbiguousMatchException>(() => t.GetProperty("myprop", bf));
        }

        [Fact]
        public static void FieldsThrowIfDeclaringTypeIsSame()
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;
            Type t = typeof(Derived).Project();

            // Fields return the most derived match.
            FieldInfo f = t.GetField("myfield", bf);
            Assert.Equal("MyField", f.Name);

            // Unless two of them are both the most derived match...
            Assert.Throws<AmbiguousMatchException>(() => t.GetField("myfield2", bf));
        }

        [Fact]
        public static void MethodsThrowIfDeclaringTypeIsSameAndSigIsDifferent()
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;
            Type t = typeof(Derived).Project();

            // Methods return the most derived match, provided all their signatures are the same.
            MethodInfo m1 = t.GetMethod("mymethod1", bf);
            Assert.Equal("MyMethod1", m1.Name);
            MethodInfo m2 = t.GetMethod("mymethod2", bf);
            Assert.Equal("MyMethod2", m2.Name);

            // Unless two of them are both the most derived match...
            Assert.Throws<AmbiguousMatchException>(() => t.GetMethod("mymethod3", bf));

            // or they have different sigs.
            Assert.Throws<AmbiguousMatchException>(() => t.GetMethod("mymethod4", bf));
        }

        private class Base
        {
            public event Action myevent;
            public int myprop { get; }

            public int myfield;

            public void mymethod1(int x) { }
            public static void mymethod2(int x, int y) { }

            public void mymethod4(int x) { }
        }

        private class Derived : Base
        {
            public event Action MyEvent;

            public class @myinner { }
            public class MyInner { }
            public int MyProp { get; }

            public int MyField;

            public int MyField2;
            public int myfield2;

            public void MyMethod1(int x) { }
            public void MyMethod2(int x, int y) { }

            public static void mymethod3(int x, int y, double z) { }
            public void MyMethod3(int x, int y, double z) { }

            public void mymethod4(string x) { }
        }
    }
}
