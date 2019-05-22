// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// AttributeTest.cs - NUnit Test Cases for the System.Attribute class
//
// Authors:
//  Duco Fijma (duco@lorentz.xs4all.nl)
//  Gonzalo Paniagua (gonzalo@ximian.com)
//  Gert Driesen (drieseng@users.sourceforge.net)
//
//  (C) 2002 Duco Fijma
//  (c) 2004 Novell, Inc. (http://www.novell.com)
//

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

[module:Debuggable(true,false)]
namespace System.Tests
{
    public class AttributeIsDefinedTests
    {
        [Fact]
        public void TestIsDefined()
        {
            Assert.True(Attribute.IsDefined(typeof(MyDerivedClass), typeof(MyCustomAttribute)));
            Assert.True(Attribute.IsDefined(typeof(MyDerivedClass), typeof(YourCustomAttribute)));
            Assert.False(Attribute.IsDefined(typeof(MyDerivedClass), typeof(UnusedAttribute)));
            Assert.True(Attribute.IsDefined(typeof(MyDerivedClass), typeof(MyCustomAttribute), true));
            Assert.True(Attribute.IsDefined(typeof(MyDerivedClass), typeof(YourCustomAttribute), true));
            Assert.False(Attribute.IsDefined(typeof(MyDerivedClass), typeof(UnusedAttribute), false));
            Assert.True(Attribute.IsDefined(typeof(MyDerivedClass), typeof(MyCustomAttribute), false));
            Assert.False(Attribute.IsDefined(typeof(MyDerivedClass), typeof(YourCustomAttribute), false));
            Assert.False(Attribute.IsDefined(typeof(MyDerivedClass), typeof(UnusedAttribute), false));
            Assert.True(Attribute.IsDefined(typeof(MyClass).GetMethod("ParamsMethod").GetParameters()[0], typeof(ParamArrayAttribute), false));
            Assert.False(Attribute.IsDefined(typeof(MyDerivedClassNoAttribute), typeof(MyCustomAttribute)));
        }

        [Fact]
        public void IsDefined_PropertyInfo()
        {
            PropertyInfo pi = typeof(TestBase).GetProperty("PropBase3");
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute)));
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute), false));
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute), true));
            Assert.False(Attribute.IsDefined(pi, typeof(ComVisibleAttribute)));
            Assert.False(Attribute.IsDefined(pi, typeof(ComVisibleAttribute), false));
            Assert.False(Attribute.IsDefined(pi, typeof(ComVisibleAttribute), true));

            pi = typeof(TestBase).GetProperty("PropBase2");
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute)));
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute), false));
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute), true));
            Assert.True(Attribute.IsDefined(pi, typeof(ComVisibleAttribute)));
            Assert.True(Attribute.IsDefined(pi, typeof(ComVisibleAttribute), false));
            Assert.True(Attribute.IsDefined(pi, typeof(ComVisibleAttribute), true));

            pi = typeof(TestSub).GetProperty("PropBase2");
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute)));
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute), false));
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute), true));
            Assert.True(Attribute.IsDefined(pi, typeof(ComVisibleAttribute)));
            Assert.True(Attribute.IsDefined(pi, typeof(ComVisibleAttribute), false));
            Assert.True(Attribute.IsDefined(pi, typeof(ComVisibleAttribute), true));
        }

        [Fact]
        public void IsDefined_PropertyInfo_Override()
        {
            PropertyInfo pi = typeof(TestSub).GetProperty("PropBase3");
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute)));
            Assert.False(Attribute.IsDefined(pi, typeof(PropTestAttribute), false));
            Assert.True(Attribute.IsDefined(pi, typeof(PropTestAttribute), true));
            Assert.False(Attribute.IsDefined(pi, typeof(ComVisibleAttribute)));
            Assert.False(Attribute.IsDefined(pi, typeof(ComVisibleAttribute), false));
            Assert.False(Attribute.IsDefined(pi, typeof(ComVisibleAttribute), true));
        }
    }

    public static class AttributeGetCustomAttributes
    {
        public static bool PositiveTest(Attribute[] attrs, int[] expected, string id)
        {
            bool result = true;
            for (int i = 0; i < attrs.Length; i++)
            {
                if (attrs[i] is TestAttribute)
                {
                    bool found = false;
                    for (int j = 0; j < expected.Length; j++)
                    {
                        if (expected[j] == ((TestAttribute)attrs[i]).TestVal)
                        {
                            found = true;
                            expected[j] = int.MinValue;
                            break;
                        }
                    }
                    if (!found)
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        [Fact]
        public static void RunPosTests()
        {
            Type clsType2 = typeof(TestClass2);
            Type clsType3 = typeof(TestClass3);
            Assembly assem = clsType2.Assembly;

            Assert.True(PositiveTest(Attribute.GetCustomAttributes(assem), new int[] { 0 }, "00A"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetMethod("TestMethod2")), new int[] { 14, 4 }, "00B"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetMethod("TestMethod2").GetParameters()[0]), new int[] { 16 }, "00C"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetEvent("TestEvent2")), new int[] { 15 }, "00D"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetProperty("TestProperty2")), new int[] { 13 }, "00E"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetField("TestField2")), new int[] { 12 }, "00F"));

            Assert.True(PositiveTest(Attribute.GetCustomAttributes(assem, false), new int[] { 0 }, "00A1"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetMethod("TestMethod2"), false), new int[] { 14, 4 }, "00B1"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetMethod("TestMethod2").GetParameters()[0], false), new int[] { 16 }, "00C1"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetEvent("TestEvent2"), false), new int[] { 15 }, "00D1"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetProperty("TestProperty2"), false), new int[] { 13 }, "00E1"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetField("TestField2"), false), new int[] { 12 }, "00F1"));

            Assert.True(PositiveTest(Attribute.GetCustomAttributes(assem, typeof(TestAttribute)), new int[] { 0 }, "00A2"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetMethod("TestMethod2"), typeof(TestAttribute)), new int[] { 14, 4 }, "00B2"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetMethod("TestMethod2").GetParameters()[0], typeof(TestAttribute)), new int[] { 16 }, "00C2"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetEvent("TestEvent2"), typeof(TestAttribute)), new int[] { 15 }, "00D2"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetProperty("TestProperty2"), typeof(TestAttribute)), new int[] { 13 }, "00E2"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetField("TestField2"), typeof(TestAttribute)), new int[] { 12 }, "00F2"));

            Assert.True(PositiveTest(Attribute.GetCustomAttributes(assem, typeof(TestAttribute), false), new int[] { 0 }, "00A3"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetMethod("TestMethod2"), typeof(TestAttribute), false), new int[] { 14, 4 }, "00B3"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetMethod("TestMethod2").GetParameters()[0], typeof(TestAttribute), false), new int[] { 16 }, "00C3"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetEvent("TestEvent2"), typeof(TestAttribute), false), new int[] { 15 }, "00D3"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetProperty("TestProperty2"), typeof(TestAttribute), false), new int[] { 13 }, "00E3"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType2.GetField("TestField2"), typeof(TestAttribute), false), new int[] { 12 }, "00F3"));

            Assert.True(PositiveTest(Attribute.GetCustomAttributes(assem), new int[] { 0 }, "00A"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetMethod("TestMethod3")), new int[0], "00B"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetMethod("TestMethod3").GetParameters()[0]), new int[0], "00C"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetEvent("TestEvent3")), new int[0], "00D"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetProperty("TestProperty3")), new int[0], "00E"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetField("TestField3")), new int[0], "00F"));

            Assert.True(PositiveTest(Attribute.GetCustomAttributes(assem, false), new int[] { 0 }, "00A1"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetMethod("TestMethod3"), false), new int[0], "00B1"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetMethod("TestMethod3").GetParameters()[0], false), new int[0], "00C1"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetEvent("TestEvent3"), false), new int[0], "00D1"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetProperty("TestProperty3"), false), new int[0], "00E1"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetField("TestField3"), false), new int[0], "00F1"));

            Assert.True(PositiveTest(Attribute.GetCustomAttributes(assem, typeof(TestAttribute)), new int[] { 0 }, "00A2"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetMethod("TestMethod3"), typeof(TestAttribute)), new int[0], "00B2"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetMethod("TestMethod3").GetParameters()[0], typeof(TestAttribute)), new int[0], "00C2"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetEvent("TestEvent3"), typeof(TestAttribute)), new int[0], "00D2"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetProperty("TestProperty3"), typeof(TestAttribute)), new int[0], "00E2"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetField("TestField3"), typeof(TestAttribute)), new int[0], "00F2"));

            Assert.True(PositiveTest(Attribute.GetCustomAttributes(assem, typeof(TestAttribute), false), new int[] { 0 }, "00A3"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetMethod("TestMethod3"), typeof(TestAttribute), false), new int[0], "00B3"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetMethod("TestMethod3").GetParameters()[0], typeof(TestAttribute), false), new int[0], "00C3"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetEvent("TestEvent3"), typeof(TestAttribute), false), new int[0], "00D3"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetProperty("TestProperty3"), typeof(TestAttribute), false), new int[0], "00E3"));
            Assert.True(PositiveTest(Attribute.GetCustomAttributes(clsType3.GetField("TestField3"), typeof(TestAttribute), false), new int[0], "00F3"));
        }

        [Fact]
        public static void NegTest()
        {
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((Assembly)null));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((Module)null));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((MemberInfo)null));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((ParameterInfo)null));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((Assembly)null, false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((Module)null, false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((MemberInfo)null, false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((ParameterInfo)null, false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((Assembly)null, typeof(TestAttribute)));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((Module)null, typeof(TestAttribute)));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((MemberInfo)null, typeof(TestAttribute)));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((ParameterInfo)null, typeof(TestAttribute)));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((Assembly)null, typeof(TestAttribute), false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((Module)null, typeof(TestAttribute), false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((MemberInfo)null, typeof(TestAttribute), false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes((ParameterInfo)null, typeof(TestAttribute), false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes(Assembly.GetExecutingAssembly(), null, false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes(typeof(TestClass2).GetEvent("TestEvent2"), null, false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes(typeof(TestClass2).GetMethod("TestMethod2"), null, false));
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttributes(typeof(TestClass2).GetMethod("TestMethod2").GetParameters()[0], null, false));
        }

        [Fact]
        public static void MultipleAttributesTest()
        {
            Assert.Equal("System.Tests.MyCustomAttribute System.Tests.MyCustomAttribute", string.Join(" ", typeof(MultipleAttributes).GetCustomAttributes(inherit: false)));
            Assert.Equal("System.Tests.MyCustomAttribute System.Tests.MyCustomAttribute", string.Join(" ", typeof(MultipleAttributes).GetCustomAttributes(inherit: true)));
        }
    }
    public static class GetCustomAttribute
    {

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "CustomAttributes on Modules not supported in UapAot")]
        public static void customAttributeCount()
        {
            List<CustomAttributeData> customAttributes =  typeof(GetCustomAttribute).Module.CustomAttributes.ToList();
            // [System.Security.UnverifiableCodeAttribute()]
            // [TestAttributes.FooAttribute()]
            // [TestAttributes.ComplicatedAttribute((Int32)1, Stuff = 2)]
            // [System.Diagnostics.DebuggableAttribute((Boolean)True, (Boolean)False)]
            Assert.Equal(4, customAttributes.Count);
        }
        [Fact]
        public static void PositiveTest1()
        {
            Assembly element = typeof(AttributeGetCustomAttributes).Assembly;
            Type attributeType = typeof(AssemblyDescriptionAttribute);
            AssemblyDescriptionAttribute attributeVal = (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(element, attributeType);
            Assert.True(attributeVal != null);

            attributeType = typeof(AssemblyCopyrightAttribute);
            AssemblyCopyrightAttribute attributeVal1 = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(element, attributeType);
            Assert.True(attributeVal1 != null);

            attributeType = typeof(AssemblyCompanyAttribute);
            AssemblyCompanyAttribute attributeVal2 = (AssemblyCompanyAttribute)Attribute.GetCustomAttribute(element, attributeType);
            Assert.True(attributeVal2 != null);

            attributeType = typeof(AssemblyKeyNameAttribute);
            Attribute attributeVal3 = Attribute.GetCustomAttribute(element, attributeType);
            Assert.True(attributeVal3 == null);

            attributeType = typeof(AssemblyInformationalVersionAttribute);
            attributeVal3 = Attribute.GetCustomAttribute(element, attributeType);
            Assert.True(attributeVal3 != null);
        }

        [Fact]
        public static void NegTest1()
        {
            Assembly element = null;
            Type attributeType = typeof(DerivedAttribute1);
            Type clsType = typeof(DerivedClass);

            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttribute(element, attributeType));
            attributeType = null;
            element = typeof(AttributeGetCustomAttributes).Assembly; ;
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttribute(element, attributeType));
            attributeType = typeof(myClass);
            AssertExtensions.Throws<ArgumentException>(null, () => Attribute.GetCustomAttribute(element, attributeType));
            attributeType = typeof(Attribute);
            Assert.Throws<AmbiguousMatchException>(() => Attribute.GetCustomAttribute(element, attributeType));

        }
        [Fact]
        public static void PositiveTest2()
        {
            Assembly element = typeof(AttributeGetCustomAttributes).Assembly;
            Type attributeType = typeof(AssemblyDescriptionAttribute);
            AssemblyDescriptionAttribute attributeVal = (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(element, attributeType, false);
            Assert.True(attributeVal != null);

            attributeType = typeof(AssemblyCopyrightAttribute);
            AssemblyCopyrightAttribute attributeVal1 = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(element, attributeType, false);
            Assert.True(attributeVal1 != null);

            attributeType = typeof(AssemblyCompanyAttribute);
            AssemblyCompanyAttribute attributeVal2 = (AssemblyCompanyAttribute)Attribute.GetCustomAttribute(element, attributeType, false);
            Assert.True(attributeVal2 != null);

            attributeType = typeof(AssemblyKeyNameAttribute);
            Attribute attributeVal3 = Attribute.GetCustomAttribute(element, attributeType, false);
            Assert.True(attributeVal3 == null);

            attributeType = typeof(AssemblyInformationalVersionAttribute);
            attributeVal3 = Attribute.GetCustomAttribute(element, attributeType, false);
            Assert.True(attributeVal3 != null);
        }

        [Fact]
        public static void NegTest2()
        {
            Assembly element = null;
            Type attributeType = typeof(DerivedAttribute1);
            Type clsType = typeof(DerivedClass);

            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttribute(element, attributeType, true));
            attributeType = null;
            element = typeof(AttributeGetCustomAttributes).Assembly; ;
            Assert.Throws<ArgumentNullException>(() => Attribute.GetCustomAttribute(element, attributeType, false));
            attributeType = typeof(myClass);
            AssertExtensions.Throws<ArgumentException>(null, () => Attribute.GetCustomAttribute(element, attributeType, true));
            attributeType = typeof(Attribute);
            Assert.Throws<AmbiguousMatchException>(() => Attribute.GetCustomAttribute(element, attributeType, true));

        }
        [Fact]
        public static void PositiveTest3()
        {
            Type clsType = typeof(TestClass);
            MethodInfo mInfo = clsType.GetMethod("method1");
            Type attributeType = typeof(ObsoleteAttribute);
            ObsoleteAttribute obsAttr = (ObsoleteAttribute)Attribute.GetCustomAttribute(mInfo, attributeType);
            Assert.True(obsAttr != null);

            attributeType = typeof(AssemblyDescriptionAttribute);
            obsAttr = (ObsoleteAttribute)Attribute.GetCustomAttribute(mInfo, attributeType);
            Assert.True(obsAttr == null);

            attributeType = typeof(ObsoleteAttribute);
            MemberInfo[] memberinfos = clsType.GetMember("method*");
            obsAttr = (ObsoleteAttribute)Attribute.GetCustomAttribute(memberinfos[0], attributeType);
            Assert.True(obsAttr != null);

        }

        [Fact]
        public static void NegTest3()
        {
            MemberInfo element = null;
            Type attributeType = typeof(ObsoleteAttribute);
            Type clsType = typeof(DerivedClass);

            Assert.Throws<ArgumentNullException>(() => (ObsoleteAttribute)Attribute.GetCustomAttribute(element, attributeType));
            attributeType = null;
            element = typeof(TestClass).GetMethod("method1");
            Assert.Throws<ArgumentNullException>(() => (ObsoleteAttribute)Attribute.GetCustomAttribute(element, attributeType));
            attributeType = typeof(object);
            AssertExtensions.Throws<ArgumentException>(null, () => Attribute.GetCustomAttribute(element, attributeType));

            Assert.Throws<AmbiguousMatchException>(() => Attribute.GetCustomAttribute(typeof(Attribute).GetMethod("GetCustomAttribute"), typeof(Attribute)));

        }
        [Fact]
        public static void PositiveTest4()
        {
            Type clsType = typeof(TestClass);
            MethodInfo mInfo = clsType.GetMethod("method1");
            Type attributeType = typeof(ObsoleteAttribute);
            ObsoleteAttribute obsAttr = (ObsoleteAttribute)Attribute.GetCustomAttribute(mInfo, attributeType, false);
            Assert.True(obsAttr != null);

            attributeType = typeof(AssemblyDescriptionAttribute);
            obsAttr = (ObsoleteAttribute)Attribute.GetCustomAttribute(mInfo, attributeType, false);
            Assert.True(obsAttr == null);

            attributeType = typeof(ObsoleteAttribute);
            MemberInfo[] memberinfos = clsType.GetMember("method*");
            obsAttr = (ObsoleteAttribute)Attribute.GetCustomAttribute(memberinfos[0], attributeType, false);
            Assert.True(obsAttr != null);

        }

        [Fact]
        public static void NegTest4()
        {
            MemberInfo element = null;
            Type attributeType = typeof(ObsoleteAttribute);
            Type clsType = typeof(DerivedClass);

            Assert.Throws<ArgumentNullException>(() => (ObsoleteAttribute)Attribute.GetCustomAttribute(element, attributeType, false));
            attributeType = null;
            element = typeof(TestClass).GetMethod("method1");
            Assert.Throws<ArgumentNullException>(() => (ObsoleteAttribute)Attribute.GetCustomAttribute(element, attributeType, false));
            attributeType = typeof(object);
            AssertExtensions.Throws<ArgumentException>(null, () => Attribute.GetCustomAttribute(element, attributeType, false));

            Assert.Throws<AmbiguousMatchException>(() => Attribute.GetCustomAttribute(typeof(Attribute).GetMethod("GetCustomAttribute"), typeof(Attribute), false));

        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "CustomAttributes on Modules not supported in UapAot")]
        public static void PositiveTest5()
        {
            Type clsType = typeof(GetCustomAttribute);
            Type attributeType;

            attributeType = typeof(DebuggableAttribute);
            DebuggableAttribute dbgAttr = (DebuggableAttribute)Attribute.GetCustomAttribute(clsType.Module, attributeType);
            Assert.True(dbgAttr != null);

            attributeType = typeof(AssemblyDescriptionAttribute);
            dbgAttr = (DebuggableAttribute)Attribute.GetCustomAttribute(clsType.Module, attributeType);
            Assert.True(dbgAttr == null);

            AssemblyDescriptionAttribute asmdbgAttr = (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(clsType.Module, attributeType);
            Assert.True(asmdbgAttr == null);
        }

        [Fact]
        public static void NegTest5()
        {
            ParameterInfo element = null;
            Type attributeType = typeof(DebuggableAttribute);
            Type clsType = typeof(DerivedClass);

            Assert.Throws<ArgumentNullException>(() => (DebuggableAttribute)Attribute.GetCustomAttribute(element, attributeType));
            attributeType = null;
            Assert.Throws<ArgumentNullException>(() => (DebuggableAttribute)Attribute.GetCustomAttribute(clsType.Module, attributeType));
            attributeType = typeof(object);
            AssertExtensions.Throws<ArgumentException>(null, () => (DebuggableAttribute)Attribute.GetCustomAttribute(clsType.Module, attributeType));

        }
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "CustomAttributes on Modules not supported in UapAot")]
        public static void PositiveTest6()
        {
            Type clsType = typeof(GetCustomAttribute);
            Type attributeType;

            attributeType = typeof(DebuggableAttribute);
            DebuggableAttribute dbgAttr = (DebuggableAttribute)Attribute.GetCustomAttribute(clsType.Module, attributeType, false);
            Assert.True(dbgAttr != null);

            attributeType = typeof(AssemblyDescriptionAttribute);
            dbgAttr = (DebuggableAttribute)Attribute.GetCustomAttribute(clsType.Module, attributeType, false);
            Assert.True(dbgAttr == null);

            AssemblyDescriptionAttribute asmdbgAttr = (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(clsType.Module, attributeType, false);
            Assert.True(asmdbgAttr == null);
        }

        [Fact]
        public static void NegTest6()
        {
            ParameterInfo element = null;
            Type attributeType = typeof(DebuggableAttribute);
            Type clsType = typeof(DerivedClass);

            Assert.Throws<ArgumentNullException>(() => (DebuggableAttribute)Attribute.GetCustomAttribute(element, attributeType, false));
            attributeType = null;
            Assert.Throws<ArgumentNullException>(() => (DebuggableAttribute)Attribute.GetCustomAttribute(clsType.Module, attributeType, false));
            attributeType = typeof(object);
            AssertExtensions.Throws<ArgumentException>(null, () => (DebuggableAttribute)Attribute.GetCustomAttribute(clsType.Module, attributeType, false));

        }
        [Fact]
        public static void PositiveTest7()
        {
            Type clsType = typeof(DerivedClass);
            MethodInfo minfo = clsType.GetMethod("TestMethod");
            ParameterInfo[] paramInfos = minfo.GetParameters();


            ArgumentUsageAttribute usageAttr = (ArgumentUsageAttribute)Attribute.GetCustomAttribute(paramInfos[0], typeof(ArgumentUsageAttribute));
            Assert.True(usageAttr != null && usageAttr.Message == "for test");

            usageAttr = (ArgumentUsageAttribute)Attribute.GetCustomAttribute(paramInfos[1], typeof(ArgumentUsageAttribute));
            Assert.True(usageAttr != null && usageAttr.Message == "for test again");

            AssemblyFileVersionAttribute assemFileAttr = (AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(paramInfos[0], typeof(AssemblyFileVersionAttribute));
            Assert.True(assemFileAttr == null);

            Assert.True(usageAttr.TypeId == ArgumentUsageAttribute._guid);

        }

        [Fact]
        public static void NegTest7()
        {
            ParameterInfo element = null;
            Type attributeType = typeof(ArgumentUsageAttribute);
            Type clsType = typeof(DerivedClass);
            MethodInfo minfo = clsType.GetMethod("TestMethod");
            ParameterInfo[] paramInfos = minfo.GetParameters();

            Assert.Throws<ArgumentNullException>(() => (ArgumentUsageAttribute)Attribute.GetCustomAttribute(element, attributeType));
            attributeType = null;
            Assert.Throws<ArgumentNullException>(() => (ArgumentUsageAttribute)Attribute.GetCustomAttribute(paramInfos[0], attributeType));
            attributeType = typeof(object);
            AssertExtensions.Throws<ArgumentException>(null, () => (ArgumentUsageAttribute)Attribute.GetCustomAttribute(paramInfos[0], attributeType));

        }
        [Fact]
        public static void PositiveTest8()
        {
            Type clsType = typeof(DerivedClass);
            MethodInfo minfo = clsType.GetMethod("TestMethod");
            ParameterInfo[] paramInfos = minfo.GetParameters();

            ArgumentUsageAttribute usageAttr = (ArgumentUsageAttribute)Attribute.GetCustomAttribute(paramInfos[0], typeof(ArgumentUsageAttribute), false);
            Assert.True(usageAttr == null);

            usageAttr = (ArgumentUsageAttribute)Attribute.GetCustomAttribute(paramInfos[0], typeof(ArgumentUsageAttribute), true);
            Assert.True(usageAttr != null && usageAttr.Message == "for test");

            usageAttr = (ArgumentUsageAttribute)Attribute.GetCustomAttribute(paramInfos[1], typeof(ArgumentUsageAttribute), false);
            Assert.True(usageAttr != null && usageAttr.Message == "for test again");

            usageAttr = (ArgumentUsageAttribute)Attribute.GetCustomAttribute(paramInfos[1], typeof(ArgumentUsageAttribute), true);
            Assert.True(usageAttr != null && usageAttr.Message == "for test again");

            AssemblyFileVersionAttribute assemFileAttr = (AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(paramInfos[0], typeof(AssemblyFileVersionAttribute), false);
            Assert.True(assemFileAttr == null);
        }

        [Fact]
        public static void NegTest8()
        {
            ParameterInfo element = null;
            Type attributeType = typeof(ArgumentUsageAttribute);
            Type clsType = typeof(DerivedClass);
            MethodInfo minfo = clsType.GetMethod("TestMethod");
            ParameterInfo[] paramInfos = minfo.GetParameters();

            Assert.Throws<ArgumentNullException>(() => (ArgumentUsageAttribute)Attribute.GetCustomAttribute(element, attributeType, false));
            attributeType = null;
            Assert.Throws<ArgumentNullException>(() => (ArgumentUsageAttribute)Attribute.GetCustomAttribute(paramInfos[0], attributeType, false));
            attributeType = typeof(object);
            AssertExtensions.Throws<ArgumentException>(null, () => (ArgumentUsageAttribute)Attribute.GetCustomAttribute(paramInfos[0], attributeType, false));

        }
    }
    [AttributeUsage(AttributeTargets.All)]
    public class TestAttribute : Attribute
    {
        public int TestVal;

        public TestAttribute(int i) { TestVal = i; }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class TestAttribute2 : TestAttribute
    {
        public int TestVal2;

        public TestAttribute2(int i) : base(i + 10) { }
    }

    [TestAttribute(1)]
    public class TestClass
    {

        [Obsolete("This method is obsolete.use method2 instead")]
        public void method1() { }
        public void method2() { }
        public string method3(string strVal)
        {
            return strVal;
        }

        [TestAttribute(2)]
        public int TestField = 0;

        [TestAttribute(3)]
        public int TestProperty
        {
            get { return TestField; }
            set { TestField = value; }
        }

        [TestAttribute(4)]
        public int TestMethod([TestAttribute(6)] bool boolean) { return 1; }

        public TestClass() { }
    }

    [TestAttribute2(1)]
    public class TestClass2 : TestClass
    {
        [TestAttribute2(2)]
        public int TestField2 = 0;

        [TestAttribute2(3)]
        public int TestProperty2
        {
            get { return TestField2; }
            set { TestField2 = value; }
        }

        [TestAttribute2(4)]
        [TestAttribute(4)]
        public int TestMethod2([TestAttribute2(6)] bool boolean) { return 2; }

        public delegate void TestEventHandler2(object sender, int i);

        [TestAttribute2(5)]
        public event TestEventHandler2 TestEvent2;

        public TestClass2() { }

        public void UseTestEvent2() // just to avoid a build warning
        {
            if (TestEvent2 != null)
            {
                TestEvent2(this, TestField2);
            }
        }
    }

    public class TestClass3 : TestClass
    {
        public int TestField3 = 0;

        public int TestProperty3
        {
            get { return TestField3; }
            set { TestField3 = value; }
        }

        public int TestMethod3(bool boolean) { return 2; }

        public delegate void TestEventHandler3(object sender, int i);

        public event TestEventHandler3 TestEvent3;

        public TestClass3() { }

        public void UseTestEvent3() // just to avoid a build warning
        {
            if (TestEvent3 != null)
            {
                TestEvent3(this, TestField3);
            }
        }
    }
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ArgumentUsageAttribute : Attribute
    {
        public static object _guid = 0xdead;
        protected string usageMsg;
        public ArgumentUsageAttribute(string UsageMsg)
        {
            this.usageMsg = UsageMsg;
        }
        public string Message
        {
            get { return usageMsg; }
            set { usageMsg = value; }
        }

        // Override TypeId to provide a unique identifier for the instance.
        public override object TypeId
        {
            get { return _guid; }
        }
    }
    public class BaseClass
    {
        public virtual void TestMethod([ArgumentUsage("for test")]string[] strArray, params string[] strList)
        {
        }
    }
    public class DerivedClass : BaseClass
    {
        public override void TestMethod(string[] strArray,
            [ArgumentUsage("for test again")]
            params string[] strList)
        {

        }
    }
    public class DerivedAttribute1 : Attribute
    {
        public DerivedAttribute1()
        {
        }
    }
    public class myClass
    {
    }

    [MyCustomAttribute("MyBaseClass"), YourCustomAttribute(37)]
    internal class MyClass
    {
        int Value
        {
            get { return 42; }
        }

        public static void ParamsMethod(params object[] args)
        {
        }
    }

    [MyCustomAttribute("MyDerivedClass")]
    internal class MyDerivedClass : MyClass
    {
        public void Do()
        {
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal class MyCustomAttribute : Attribute
    {
        private string _info;

        public MyCustomAttribute(string info)
        {
            _info = info;
        }

        public string Info
        {
            get
            {
                return _info;
            }
        }
    }
    [AttributeUsage(AttributeTargets.Class)]
    internal class YourCustomAttribute : Attribute
    {
        private int _value;

        public YourCustomAttribute(int value)
        {
            _value = value;
        }

        public int Value
        {
            get
            {
                return _value;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PropTestAttribute : Attribute
    {
        public PropTestAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal class UnusedAttribute : Attribute
    {
    }

    class MyDerivedClassNoAttribute : MyClass
    {
    }

    public class TestBase
    {
        [PropTest]
        public int PropBase1
        {
            get { return 0; }
            set { }
        }

        [PropTest]
        [ComVisible(false)]
        public string PropBase2
        {
            get { return ""; }
            set { }
        }

        [PropTest]
        public virtual string PropBase3
        {
            get { return ""; }
            set { }
        }
    }

    public class TestSub : TestBase
    {
        [PropTest]
        public int PropSub1
        {
            get { return 0; }
            set { }
        }

        [PropTest]
        public string PropSub2
        {
            get { return ""; }
            set { }
        }

        public override string PropBase3
        {
            get { return ""; }
            set { }
        }
    }

    [MyCustomAttribute("Test")]
    [MyCustomAttribute("Test")]
    class MultipleAttributes
    {
    }
}
