// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Tests
{
    public class MemberInfoTests
    {
        [Fact]
        public static void TestReflectedType()
        {
            Type t = typeof(Derived);
            MemberInfo[] members = t.GetMembers();
            foreach (MemberInfo member in members)
            {
                Assert.Equal(t, member.ReflectedType);
            }
        }

        [Fact]
        public static void TestPropertyReflectedType()
        {
            Type t = typeof(Base);
            PropertyInfo p = t.GetProperty(nameof(Base.MyProperty1));
            Assert.Equal(t, p.ReflectedType);
            Assert.NotNull(p.GetMethod);
            Assert.NotNull(p.SetMethod);
        }

        [Fact]
        public static void TestInheritedPropertiesHidePrivateAccessorMethods()
        {
            Type t = typeof(Derived);
            PropertyInfo p = t.GetProperty(nameof(Base.MyProperty1));
            Assert.Equal(t, p.ReflectedType);
            Assert.NotNull(p.GetMethod);
            Assert.Null(p.SetMethod);
        }

        [Fact]
        public static void TestGenericMethodsInheritTheReflectedTypeOfTheirTemplate()
        {
            Type t = typeof(Derived);
            MethodInfo moo = t.GetMethod("Moo");
            Assert.Equal(t, moo.ReflectedType);
            MethodInfo mooInst = moo.MakeGenericMethod(typeof(int));
            Assert.Equal(t, mooInst.ReflectedType);
        }

        [Fact]
        public static void TestDeclaringMethodOfTypeParametersOfInheritedMethods()
        {
            Type t = typeof(Derived);
            MethodInfo moo = t.GetMethod("Moo");
            Assert.Equal(t, moo.ReflectedType);
            Type theM = moo.GetGenericArguments()[0];
            MethodBase moo1 = theM.DeclaringMethod;
            Type reflectedTypeOfMoo1 = moo1.ReflectedType;
            Assert.Equal(typeof(Base), reflectedTypeOfMoo1);
        }

        [Fact]
        public static void TestDeclaringMethodOfTypeParametersOfInheritedMethods2()
        {
            Type t = typeof(GDerived<int>);
            MethodInfo moo = t.GetMethod("Moo");
            Assert.Equal(t, moo.ReflectedType);
            Type theM = moo.GetGenericArguments()[0];
            MethodBase moo1 = theM.DeclaringMethod;
            Type reflectedTypeOfMoo1 = moo1.ReflectedType;
            Assert.Equal(typeof(GBase<>), reflectedTypeOfMoo1);
        }

        [Fact]
        public static void TestInheritedPropertyAccessors()
        {
            Type t = typeof(Derived);
            PropertyInfo p = t.GetProperty(nameof(Base.MyProperty));
            MethodInfo getter = p.GetMethod;
            MethodInfo setter = p.SetMethod;
            Assert.Equal(t, getter.ReflectedType);
            Assert.Equal(t, setter.ReflectedType);
        }

        [Fact]
        public static void TestInheritedEventAccessors()
        {
            Type t = typeof(Derived);
            EventInfo e = t.GetEvent(nameof(Base.MyEvent));
            MethodInfo adder = e.AddMethod;
            MethodInfo remover = e.RemoveMethod;
            Assert.Equal(t, adder.ReflectedType);
            Assert.Equal(t, remover.ReflectedType);
        }

        [Fact]
        public static void TestReflectedTypeIsPartOfIdentity()
        {
            Type b = typeof(Base);
            Type d = typeof(Derived);

            {
                EventInfo e = b.GetEvent(nameof(Base.MyEvent));
                EventInfo ei = d.GetEvent(nameof(Derived.MyEvent));
                Assert.False(e.Equals(ei));
            }

            {
                FieldInfo f = b.GetField(nameof(Base.MyField));
                FieldInfo fi = d.GetField(nameof(Derived.MyField));
                Assert.False(f.Equals(fi));
            }

            {
                MethodInfo m = b.GetMethod(nameof(Base.Moo));
                MethodInfo mi = d.GetMethod(nameof(Derived.Moo));
                Assert.False(m.Equals(mi));
            }

            {
                PropertyInfo p = b.GetProperty(nameof(Base.MyProperty));
                PropertyInfo pi = d.GetProperty(nameof(Derived.MyProperty));
                Assert.False(p.Equals(pi));
            }
        }

        [Fact]
        public static void TestFieldInfoReflectedTypeDoesNotSurviveRuntimeHandles()
        {
            Type t = typeof(Derived);
            FieldInfo f = t.GetField(nameof(Base.MyField));
            Assert.Equal(typeof(Derived), f.ReflectedType);
            RuntimeFieldHandle h = f.FieldHandle;
            FieldInfo f2 = FieldInfo.GetFieldFromHandle(h);
            Assert.Equal(typeof(Base), f2.ReflectedType);
        }

        [Fact]
        public static void TestMethodInfoReflectedTypeDoesNotSurviveRuntimeHandles()
        {
            Type t = typeof(Derived);
            MethodInfo m = t.GetMethod(nameof(Base.Moo));
            Assert.Equal(typeof(Derived), m.ReflectedType);
            RuntimeMethodHandle h = m.MethodHandle;
            MethodBase m2 = MethodBase.GetMethodFromHandle(h);
            Assert.Equal(typeof(Base), m2.ReflectedType);
        }

        [Fact]
        public void TestGetCustomAttributesData()
        {
            MemberInfo[] m = typeof(MemberInfoTests).GetMember("SampleClass");
            Assert.Equal(1, m.Count());
            foreach(CustomAttributeData cad in m[0].GetCustomAttributesData())
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

        public static IEnumerable<object[]> EqualityOperator_TestData()
        {
            yield return new object[] { typeof(SampleClass) };
            yield return new object[] { new MemberInfoTests().GetType() };
            yield return new object[] { typeof(int) };
            yield return new object[] { typeof(Dictionary<,>) };
        }

        [Theory]
        [MemberData(nameof(EqualityOperator_TestData))]
        public void EqualityOperator_Equal_ReturnsTrue(Type type)
        {
            MemberInfo[] members1 = GetOrderedMembers(type);
            MemberInfo[] members2 = GetOrderedMembers(type);

            Assert.Equal(members1.Length, members2.Length);
            for (int i = 0; i < members1.Length; i++)
            {
                Assert.True(members1[i] == members2[i]);
                Assert.False(members1[i] != members2[i]);
            }
        }

        private MemberInfo[] GetOrderedMembers(Type type) => GetMembers(type).OrderBy(member => member.Name).ToArray();

        private MemberInfo[] GetMembers(Type type)
        {
            return type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private class Base
        {
            public event Action MyEvent { add { } remove { } }
#pragma warning disable 0649
            public int MyField;
#pragma warning restore 0649
            public int MyProperty { get; set; }

            public int MyProperty1 { get; private set; }
            public int MyProperty2 { private get; set; }

            public void Moo<M>() { }
        }

        private class Derived : Base
        {
        }

        private class GBase<T>
        {
            public void Moo<M>() { }
        }

        private class GDerived<T> : GBase<T>
        {
        }

#pragma warning disable 0067, 0169
        [ComVisible(false)]
        public class SampleClass
        {
            public int PublicField;
            private int PrivateField;

            public SampleClass(bool y) { }
            private SampleClass(int x) { }

            public void PublicMethod() { }
            private void PrivateMethod() { }

            public int PublicProp { get; set; }
            private int PrivateProp { get; set; }

            public event EventHandler PublicEvent;
            private event EventHandler PrivateEvent;
        }
#pragma warning restore 0067, 0169
    }
}