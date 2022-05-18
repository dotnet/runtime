// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public interface TestInterface
    {
        int PInterface1 => 1;
        short MInterface1() => 1;
        float PInterface2 => 2f;
        static double fInterface1 = 1;
        string PInterface3 { get; set; }
        bool MInterface2();
        static decimal fInterface2 = 1;
    }

    public abstract class TestBase : TestInterface
    {
        public static decimal fInterface2 = 5;
        public virtual string PBase { get; set; }
        public string PInterface3 { get; set; }
        public int PInterface1 => 1;

        public bool MInterface2() => throw new NotImplementedException();
    }

#pragma warning disable CS0067
    public class Test : TestBase
    {
        public int field1;
        public void Method1() { }
        public void Method2() { }
        public delegate void Delegate1();
        public int field2;
        public event EventHandler Event1;
        public int field3;
        public int field4;
        public void Method3() { }
        public void Method3(string a) { }
        public Test(int value) { }
        public Test() { }
        public Test(string value) { }
        public void Method3(int i) { }
        public string Property1 => string.Empty;
        public event EventHandler Event2;
        public void Method4() { decimal a = fInterface2; }
        public delegate void Delegate2(int i);
        public delegate void EventHandler(object sender, EventArgs e);
        public void Method5() { }
        public string Property2 { get; set; }
        public delegate void Delegate3(int i, int j);
        public event EventHandler Event3;
        public string Property3 => string.Empty;
        public string Property4 => string.Empty;
        public int field5;
    }
#pragma warning restore CS0067

    public class RuntimeTypeMemberCacheTests
    {
        [Fact]
        public static void TypeGetMethodsReturnsInDeclaredOrderTest()
        {
            Type t = typeof(Test);
            MethodInfo method2 = t.GetMethod("Method2");
            MethodInfo mInterface2 = t.GetMethod("MInterface2");
            Assert.NotNull(method2);
            Assert.NotNull(mInterface2);
            MethodInfo[] methods = t.GetMethods();
            Assert.Equal("Method1", methods[0].Name);
            Assert.Equal(method2, methods[1]);
            Assert.Equal("add_Event1", methods[2].Name);
            Assert.Equal(mInterface2, methods[23]);
        }

        [Fact]
        public static void TypeGetMemberByNameReturnsInDeclaredOrderTest()
        {
            Type t = typeof(Test);
            MethodInfo method3Int = t.GetMethod("Method3", new Type[] { typeof(int) });
            MethodInfo method3String = t.GetMethod("Method3", new Type[] { typeof(string) });
            MemberInfo[] methods = t.GetMember("Method3");
            Assert.NotNull(method3Int);
            Assert.NotNull(method3String);
            Assert.Equal(0, ((MethodInfo)methods[0]).GetParameters().Length);
            Assert.Equal(method3String, methods[1]);
            Assert.Equal(method3Int, methods[2]);
        }

        [Fact]
        public static void TypeGetConstructorsReturnsInDeclaredOrderTest()
        {
            Type t = typeof(Test);
            ConstructorInfo constructor3 = t.GetConstructor(new Type[] { typeof(string) });
            Assert.NotNull(constructor3);
            ConstructorInfo[] constuctors = t.GetConstructors();
            Assert.Equal(typeof(int), constuctors[0].GetParameters()[0].ParameterType);
            Assert.Equal(0, constuctors[1].GetParameters().Length);
            Assert.Equal(constructor3, constuctors[2]);
        }

        [Fact]
        public static void TypeGetEventsReturnsInDeclaredOrderTest()
        {
            Type t = typeof(Test);
            EventInfo event2 = t.GetEvent("Event2");
            Assert.NotNull(event2);
            EventInfo[] events = t.GetEvents();
            Assert.Equal("Event1", events[0].Name);
            Assert.Equal(event2, events[1]);
            Assert.Equal("Event3", events[2].Name);
        }

        [Fact]
        public static void TypeGetPropertiesReturnsInDeclaredOrderTest()
        {
            Type t = typeof(Test);
            PropertyInfo propertyBase = t.GetProperty("PBase");
            PropertyInfo property3 = t.GetProperty("Property3");
            Assert.NotNull(propertyBase);
            Assert.NotNull(property3);
            PropertyInfo[] properties = t.GetProperties();
            Assert.Equal("Property1", properties[0].Name);
            Assert.Equal("Property2", properties[1].Name);
            Assert.Equal(property3, properties[2]);
            Assert.Equal(propertyBase, properties[4]);
        }

        [Fact]
        public static void TypeGetFieldsReturnsInDeclaredOrderTest()
        {
            Type t = typeof(Test);
            FieldInfo field3 = t.GetField("field3");
            Assert.NotNull(field3);
            FieldInfo[] fields = t.GetFields();
            Assert.Equal("field1", fields[0].Name);
            Assert.Equal("field2", fields[1].Name);
            Assert.Equal(field3, fields[2]);
        }
    }
}
