// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    
            Assert.True(false, "Expected to find MyAttribute");
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
    
            Assert.True(false, "Expected to find MyAttribute");
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
    
            Assert.True(false, "Expected to find FlagsAttribute");
        }
    
        [Fact]
        [ComVisible(false)]
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
    
            Assert.True(false, "Expected to find ComVisibleAttribute");
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
    
            Assert.True(false, "Expected to find FieldOffsetAttribute");
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
    
            Assert.True(false, "Expected to find MyAttribute");
        }

        [Flags]
        private enum MyEnum { } 
    
        private class MyAttribute : Attribute
        {
            internal MyAttribute() { }
            internal MyAttribute(int i) { }
            internal MyAttribute(string s) { }
            internal MyAttribute(int i, int j) { }
    
            static MyAttribute() { }
        }
    
        [StructLayout(LayoutKind.Explicit)]
        private class MyExplicitClass
        {
            [FieldOffset(40)]
            public int X;
        }
    }
}