// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Tests
{
    public static class CustomAttributeDataTests
    {
        [Fact]
        [My]
        public static void Test_CustomAttributeData_ConstructorNullary()
        {
            MethodInfo m = (MethodInfo)MethodBase.GetCurrentMethod();
            foreach (CustomAttributeData cad in m.CustomAttributes)
            {
                if (cad.AttributeType == typeof(MyAttribute))
                {
                    ConstructorInfo c = cad.Constructor;
                    Assert.False(c.IsStatic);
                    Assert.Equal(typeof(MyAttribute), c.DeclaringType);
                    ParameterInfo[] p = c.GetParameters();
                    Assert.Equal(0, p.Length);
                    return;
                }
            }

            Assert.Fail("Expected to find MyAttribute");
        }

        [Fact]
        [My((short)5)]
        public static void Test_CustomAttributeData_Constructor1()
        {
            MethodInfo m = (MethodInfo)MethodBase.GetCurrentMethod();
            foreach (CustomAttributeData cad in m.CustomAttributes)
            {
                if (cad.AttributeType == typeof(MyAttribute))
                {
                    ConstructorInfo c = cad.Constructor;
                    Assert.False(c.IsStatic);
                    Assert.Equal(typeof(MyAttribute), c.DeclaringType);
                    ParameterInfo[] p = c.GetParameters();
                    Assert.Equal(1, p.Length);
                    Assert.Equal(typeof(int), p[0].ParameterType);
                    return;
                }
            }

            Assert.Fail("Expected to find MyAttribute");
        }

        [Fact]
        public static void Test_CustomAttribute_Constructor_CrossAssembly1()
        {
            foreach (CustomAttributeData cad in typeof(MyEnum).CustomAttributes)
            {
                if (cad.AttributeType == typeof(FlagsAttribute))
                {
                    ConstructorInfo c = cad.Constructor;
                    Assert.False(c.IsStatic);
                    Assert.Equal(typeof(FlagsAttribute), c.DeclaringType);
                    ParameterInfo[] p = c.GetParameters();
                    Assert.Equal(0, p.Length);
                    return;
                }
            }

            Assert.Fail("Expected to find FlagsAttribute");
        }

        [Fact]
        [ComVisible(false)]
        [ActiveIssue("https://github.com/dotnet/linker/issues/2078", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming))
            /* Descriptors tell us to remove ComVisibleAttribute */]
        public static void Test_CustomAttribute_Constructor_CrossAssembly2()
        {
            MethodInfo m = (MethodInfo)MethodBase.GetCurrentMethod();
            foreach (CustomAttributeData cad in m.CustomAttributes)
            {
                if (cad.AttributeType == typeof(ComVisibleAttribute))
                {
                    ConstructorInfo c = cad.Constructor;
                    Assert.False(c.IsStatic);
                    Assert.Equal(typeof(ComVisibleAttribute), c.DeclaringType);
                    ParameterInfo[] p = c.GetParameters();
                    Assert.Equal(1, p.Length);
                    Assert.Equal(typeof(bool), p[0].ParameterType);
                    return;
                }
            }

            Assert.Fail("Expected to find ComVisibleAttribute");
        }

        [Fact]
        public static void Test_CustomAttribute_Constructor_PseudoCa()
        {
            FieldInfo f = typeof(MyExplicitClass).GetTypeInfo().GetDeclaredField(nameof(MyExplicitClass.X));
            foreach (CustomAttributeData cad in f.CustomAttributes)
            {
                if (cad.AttributeType == typeof(FieldOffsetAttribute))
                {
                    ConstructorInfo c = cad.Constructor;
                    Assert.False(c.IsStatic);
                    Assert.Equal(typeof(FieldOffsetAttribute), c.DeclaringType);
                    ParameterInfo[] p = c.GetParameters();
                    Assert.Equal(1, p.Length);
                    Assert.Equal(typeof(int), p[0].ParameterType);
                    return;
                }
            }

            Assert.Fail("Expected to find FieldOffsetAttribute");
        }

        [Fact]
        public static void Test_EqualsMethod()
        {
            Assert.Equal(1, typeof(MyEnum).CustomAttributes.Count());
            CustomAttributeData cad1 = typeof(MyEnum).CustomAttributes.First();
            CustomAttributeData cad2 = typeof(MyEnum).CustomAttributes.First();
            Assert.True(cad1.Equals(cad1));
            Assert.False(cad1.Equals(cad2));
        }

        [Fact]
        [My(3)]
        public static void Test_CustomAttributeData_ToString()
        {
            MethodInfo m = (MethodInfo)MethodBase.GetCurrentMethod();
            foreach (CustomAttributeData cad in m.CustomAttributes)
            {
                if (cad.AttributeType == typeof(MyAttribute))
                {
                    Assert.NotNull(cad.ToString());
                    return;
                }
            }

            Assert.Fail("Expected to find MyAttribute");
        }

        [Fact]
        [MyEnumArray(MyTestEnum.Value, null, [], [MyTestEnum.Value, MyTestEnum.Value])]
        public static void Test_CustomAttributeData_EnumArray()
        {
            MethodInfo m = (MethodInfo)MethodBase.GetCurrentMethod();
            foreach (CustomAttributeData cad in m.CustomAttributes)
            {
                if (cad.AttributeType == typeof(MyEnumArrayAttribute))
                {
                    Assert.Equal(4, cad.ConstructorArguments.Count);
                    Assert.Equal(typeof(MyTestEnum), cad.ConstructorArguments[0].ArgumentType);
                    Assert.Equal((long)MyTestEnum.Value, cad.ConstructorArguments[0].Value);
                    Assert.Equal(typeof(MyTestEnum[]), cad.ConstructorArguments[1].ArgumentType);
                    Assert.Null(cad.ConstructorArguments[1].Value);
                    Assert.Equal(typeof(MyTestEnum[]), cad.ConstructorArguments[2].ArgumentType);
                    ReadOnlyCollection<CustomAttributeTypedArgument> emptyArrayValue = (ReadOnlyCollection<CustomAttributeTypedArgument>)cad.ConstructorArguments[2].Value;
                    Assert.Equal(0, emptyArrayValue.Count);
                    Assert.Equal(typeof(MyTestEnum[]), cad.ConstructorArguments[3].ArgumentType);
                    ReadOnlyCollection<CustomAttributeTypedArgument> arrayValue = (ReadOnlyCollection<CustomAttributeTypedArgument>)cad.ConstructorArguments[3].Value;
                    Assert.Equal(2, arrayValue.Count);
                    Assert.Equal((long)MyTestEnum.Value, arrayValue[0].Value);
                    Assert.Equal((long)MyTestEnum.Value, arrayValue[1].Value);
                    return;
                }
            }

            Assert.Fail("Expected to find MyEnumArrayAttribute");
        }

        [Flags]
        private enum MyEnum { }

        private enum MyTestEnum : long
        {
            Value = 0x1234567890
        }

        private class MyAttribute : Attribute
        {
            internal MyAttribute() { }
            internal MyAttribute(int i) { }
            internal MyAttribute(string s) { }
            internal MyAttribute(int i, int j) { }

            static MyAttribute() { }
        }

        private class MyEnumArrayAttribute : Attribute
        {
            internal MyEnumArrayAttribute(MyTestEnum value, MyTestEnum[] nullArrayValue, MyTestEnum[] emptyArrayValue, MyTestEnum[] arrayValue)
            {
                Value = value;
                NullArrayValue = nullArrayValue;
                EmptyArrayValue = emptyArrayValue;
                ArrayValue = arrayValue;
            }

            public MyTestEnum Value { get; }
            public MyTestEnum[] NullArrayValue { get; }
            public MyTestEnum[] EmptyArrayValue { get; }
            public MyTestEnum[] ArrayValue { get; }
        }

        [StructLayout(LayoutKind.Explicit)]
        private class MyExplicitClass
        {
            [FieldOffset(40)]
            public int X;
        }
    }
}
