// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Tests
{
    public static class ParameterInfoTests
    {
        [Fact]
        public static void RawDefaultValue()
        {
            MethodInfo m =  GetMethod(typeof(ParameterInfoTests), "Foo1");
            ParameterInfo p = m.GetParameters()[0];
            object raw = p.RawDefaultValue;
            Assert.Equal(typeof(int), raw.GetType());
            Assert.Equal<int>((int)raw, (int)BindingFlags.DeclaredOnly);
        }

        [Fact]
        public static void RawDefaultValueFromAttribute()
        {
            MethodInfo m = GetMethod(typeof(ParameterInfoTests), "Foo2");
            ParameterInfo p = m.GetParameters()[0];
            object cooked = p.DefaultValue;
            object raw = p.RawDefaultValue;
    
            Assert.Equal(typeof(int), raw.GetType());
            Assert.Equal<int>((int)raw, (int)BindingFlags.IgnoreCase);
        }

        [Fact]
        public static void RawDefaultValue_MetadataTrumpsAttribute()
        {
            MethodInfo m = GetMethod(typeof(ParameterInfoTests), "Foo3");
            ParameterInfo p = m.GetParameters()[0];
            object cooked = p.DefaultValue;
            object raw = p.RawDefaultValue;
    
            Assert.Equal(typeof(int), raw.GetType());
            Assert.Equal<int>((int)raw, (int)BindingFlags.FlattenHierarchy);
        }

        [Fact]
        public static void VerifyGetCustomAttributesData()
        {
            MethodInfo m = GetMethod(typeof(MyClass), "Method1");
            ParameterInfo p = m.GetParameters()[0];

            foreach (CustomAttributeData cad in p.GetCustomAttributesData())
            {
                if(cad.AttributeType == typeof(MyAttribute))
                {
                    ConstructorInfo c = cad.Constructor;
                    Assert.False(c.IsStatic);
                    Assert.False(c.IsPublic);
                    ParameterInfo[] paramInfo = c.GetParameters();
                    Assert.Equal(1, paramInfo.Length);
                    Assert.Equal(typeof(int), paramInfo[0].ParameterType);
                    return;
                }
            }

            Assert.True(false, "Expected to find MyAttribute");
        }

        private static void Foo1(BindingFlags bf = BindingFlags.DeclaredOnly) { }

        private static void Foo2([CustomBindingFlags(Value = BindingFlags.IgnoreCase)] BindingFlags bf) { }

        private static void Foo3([CustomBindingFlags(Value = BindingFlags.DeclaredOnly)] BindingFlags bf = BindingFlags.FlattenHierarchy ) { }

        //Gets MethodInfo object from a Type
        public static MethodInfo GetMethod(Type t, string method)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<MethodInfo> alldefinedMethods = ti.DeclaredMethods.GetEnumerator();
            MethodInfo mi = null;

            while (alldefinedMethods.MoveNext())
            {
                if (alldefinedMethods.Current.Name.Equals(method))
                {
                    //found method
                    mi = alldefinedMethods.Current;
                    break;
                }
            }
            return mi;
        }

        // Class For Reflection Metadata
        public class MyClass
        {
            public void Method1([My(2)]String str, int iValue, long lValue)
            {
            }
        }

        private class MyAttribute : Attribute
        {
            internal MyAttribute(int i) { }
        }
    }

    internal sealed class CustomBindingFlagsAttribute : UsableCustomConstantAttribute
    {
        public new object Value { get { return RealValue; } set { RealValue = value; } }
    }

    internal abstract class UsableCustomConstantAttribute : CustomConstantAttribute
    {
        public sealed override object Value => RealValue;
        protected object RealValue { get; set; }
    }
}