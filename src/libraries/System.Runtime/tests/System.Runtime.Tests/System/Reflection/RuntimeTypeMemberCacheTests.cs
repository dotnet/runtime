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
            ConstructorInfo[] constructors = t.GetConstructors();
            Assert.Equal(typeof(int), constructors[0].GetParameters()[0].ParameterType);
            Assert.Equal(0, constructors[1].GetParameters().Length);
            Assert.Equal(constructor3, constructors[2]);
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

        [Fact]
        public static void BigTypeGetMethodsReturnsInDeclaredOrderTest()
        {
            Type t = typeof(BigTestType);
            MethodInfo method129 = t.GetMethod("Method129");
            MethodInfo method57 = t.GetMethod("Method57");
            FieldInfo field = t.GetField("field2");
            Assert.NotNull(method129);
            Assert.NotNull(method57);

            // A new chunk added around this range, add some methods in inverse order
            for (int i = 110; i > 60; i--)
            {
                MethodInfo m = t.GetMethod($"Method{i}");
            }

            MemberInfo[] methods = t.GetMethods();
            Assert.Equal(method129, methods[129]);
            Assert.Equal(method57, methods[57]);

            for (int i = 0; i < 180; i++)
            {
                Assert.Equal(methods[i].Name, $"Method{i}");
            }
        }
    }

    public class BigTestType : TestBase
    {
        public void Method0() { }
        public void Method1() { }
        public void Method2(int a) { }
        public int Method3() => 1;
        public string Method4() => string.Empty;
        public Test Method5() => null;
        public int Method6(int a) => a;
        public int Method7(string b) => b.Length;
        public int Method8() => 1;
        public int Method9(int a) => a;
        public int Method10() => 1;
        public int Method11() => 1;
        public int Method12(int a) => a;
        public int Method13(int a) => a;
        public int Method14(int a) => a;
        public int Method15(int a) => a;
        public int Method16() => 1;
        public int Method17() => 1;
        public int Method18() => 1;
        public int Method19() => 1;
        public int Method20() => 1;
        public BigTestType(int value) { }
        public BigTestType() { }
        public BigTestType(string value) { }
        public void Method21() { }
        public void Method22(int a) { }
        public int Method23() => 1;
        public string Method24() => string.Empty;
        public Test Method25() => null;
        public int Method26(int a) => a;
        public int Method27(string b) => b.Length;
        public int Method28() => 1;

        public BigTestType(long value) { }
        public BigTestType(double value) { }
        public BigTestType(short value) { }
        public BigTestType(bool value) { }
        public BigTestType(decimal value) { }
        public int Method29(int a) => a;
        public int Method30() => 1;
        public int Method31() => 1;
        public int Method32(int a) => a;
        public int Method33(int a) => a;
        public int Method34(int a) => a;
        public int Method35(int a) => a;
        public int Method36() => 1;
        public int Method37() => 1;
        public int Method38() => 1;
        public int Method39() => 1;
        public int Method40() => 1;
        public BigTestType(Test value) { }
        public BigTestType(byte value) { }
        public int Method41() => 1;
        public int Method42(int a) => a;
        public int Method43(int a) => a;
        public int Method44(int a) => a;
        public int Method45(int a) => a;
        public int Method46() => 1;
        public int Method47() => 1;
        public int Method48() => 1;
        public int Method49() => 1;
        public int Method50() => 1;
        public BigTestType(ulong value) { }
        public void Method51() { }
        public void Method52(int a) { }
        public int Method53() => 1;
        public string Method54() => string.Empty;
        public Test Method55() => null;
        public int Method56(int a) => a;
        public int Method57(string b) => b.Length;
        public int Method58() => 1;
        public int Method59(int a) => a;
        public int Method60() => 1;
        public int Method61() => 1;
        public int Method62(int a) => a;
        public int Method63(int a) => a;
        public int Method64(int a) => a;
        public int Method65(int a) => a;
        public int Method66() => field1;
        public int Method67() => 1;
        public int Method68() => 1;
        public int Method69() => 1;
        public int Method70() => 1;
        public int field1;
        public int field2;
        public int field3;
        public void Method71() { }
        public void Method72(int a) { }
        public int Method73() => 1;
        public string Method74() => string.Empty;
        public Test Method75() => null;
        public int Method76(int a) => a;
        public void Method77(string b) { }
        public void Method78() { }
        public void Method79(int a) { }
        public int Method80() => 1;
        public void Method81() { }
        public void Method82(int a) { }
        public int Method83() => 1;
        public string Method84() => string.Empty;
        public Test Method85() => null;
        public int Method86(int a) => a;
        public void Method87(string b) { }
        public void Method88() { }
        public void Method89(int a) { }
        public int Method90() => 1;
        public void Method91() { }
        public void Method92(int a) { }
        public int Method93() => 1;
        public string Method94() => string.Empty;
        public Test Method95() => null;
        public int Method96(int a) => a;
        public void Method97(string b) { }
        public void Method98() { }
        public void Method99(int a) { }
        public int Method100() => 1;
        public void Method101() { }
        public void Method102(int a) { }
        public int Method103() => 1;
        public string Method104() => string.Empty;
        public Test Method105() => null;
        public int Method106(int a) => a;
        public void Method107(string b) { }
        public void Method108() { }
        public void Method109(int a) { }
        public int Method110() => 1;
        public void Method111() { }
        public void Method112(int a) { }
        public int Method113() => 1;
        public string Method114() => string.Empty;
        public Test Method115() => null;
        public int Method116(int a) => a;
        public void Method117(string b) { }
        public void Method118() { }
        public void Method119(int a) { }
        public int Method120() => 1;
        public void Method121() { }
        public void Method122(int a) { }
        public int Method123() => 1;
        public string Method124() => string.Empty;
        public Test Method125() => null;
        public int Method126(int a) => a;
        public void Method127(string b) { }
        public void Method128() { }
        public void Method129(int a) { }
        public int Method130() => 1;
        public void Method131() { }
        public void Method132(int a) { }
        public int Method133() => 1;
        public string Method134() => string.Empty;
        public Test Method135() => null;
        public int Method136(int a) => a;
        public void Method137(string b) { }
        public void Method138() { }
        public void Method139(int a) { }
        public int Method140() => 1;
        public void Method141() { }
        public void Method142(int a) { }
        public int Method143() => 1;
        public string Method144() => string.Empty;
        public Test Method145() => null;
        public int Method146(int a) => a;
        public void Method147(string b) { }
        public void Method148() { }
        public void Method149(int a) { }
        public int Method150() => 1;
        public void Method151() { }
        public void Method152(int a) { }
        public int Method153() => 1;
        public string Method154() => string.Empty;
        public Test Method155() => null;
        public int Method156(int a) => a;
        public void Method157(string b) { }
        public void Method158() { }
        public void Method159(int a) { }
        public int Method160() => 1;
        public void Method161() { }
        public void Method162(int a) { }
        public int Method163() => 1;
        public string Method164() => string.Empty;
        public Test Method165() => null;
        public int Method166(int a) => a;
        public void Method167(string b) { }
        public void Method168() { }
        public void Method169(int a) { }
        public int Method170() => 1;
        public void Method171() { }
        public void Method172(int a) { }
        public int Method173() => 1;
        public string Method174() => string.Empty;
        public Test Method175() => null;
        public int Method176(int a) => a;
        public void Method177(string b) { }
        public void Method178() { }
        public void Method179(int a) { }
        public int Method180() => 1;
    }
}
