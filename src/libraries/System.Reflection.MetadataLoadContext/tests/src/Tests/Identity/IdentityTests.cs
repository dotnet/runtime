// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

#pragma warning disable 0067 // Event not used
#pragma warning disable 0649 // Field not used

namespace System.Reflection.Tests
{
    public static class IdentityTests
    {
        [Fact]
        public static void Identity_ReflectedType1()
        {
            MemberTypes mt = MemberTypes.Event | MemberTypes.Field | MemberTypes.Method | MemberTypes.Property;
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            MemberInfo[] fromBase = typeof(MemberHolderBase<int>).Project().GetMember("*", mt, bf).Where(m => m.DeclaringType != typeof(object).Project()).OrderBy(m => m.Name).ToArray();
            MemberInfo[] fromDerived = typeof(MemberHolder<int>).Project().GetMember("*", mt, bf).Where(m => m.DeclaringType != typeof(object).Project()).OrderBy(m => m.Name).ToArray();

            Assert.Equal(fromBase.Length, fromDerived.Length);
            for (int i = 0; i < fromBase.Length; i++)
            {
                // DO NOT "IMPROVE" THIS CODE BY USING ASSERT.EQUALS() OR ASSERT.NOTEQUALS() HERE OR ANYWHERE ELSE IN THIS FILE.
                // We want to exercise the actual Equals() method and all of its branches in both directions. We don't want test middleware
                // "helpfully" shortcutting that with reference equality prechecks or anything else. 
                Assert.False(fromBase[i].Equals(fromDerived[i]));
                Assert.False(fromDerived[i].Equals(fromBase[i]));
            }
        }

        [Fact]
        public static void Identity_DeclaringType1()
        {
            MemberTypes mt = MemberTypes.Constructor | MemberTypes.Event | MemberTypes.Field | MemberTypes.Method | MemberTypes.Property;
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            MemberInfo[] fromBase = typeof(MemberHolderBase<int>).Project().GetMember("*", mt, bf).Where(m => m.DeclaringType != typeof(object).Project()).OrderBy(m => m.Name).ToArray();
            MemberInfo[] fromDerived = typeof(MemberHolderBase<long>).Project().GetMember("*", mt, bf).Where(m => m.DeclaringType != typeof(object).Project()).OrderBy(m => m.Name).ToArray();

            Assert.Equal(fromBase.Length, fromDerived.Length);
            for (int i = 0; i < fromBase.Length; i++)
            {
                Assert.False(fromBase[i].Equals(fromDerived[i]));
            }
        }

        [Fact]
        public static void Identity_Handle1()
        {
            MemberTypes mt = MemberTypes.Constructor | MemberTypes.Event | MemberTypes.Field | MemberTypes.Method | MemberTypes.Property;
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            MemberInfo[] fromBase = typeof(MemberHolderBase<>).Project().GetMember("*", mt, bf | BindingFlags.DeclaredOnly).Where(m => m.DeclaringType != typeof(object).Project()).OrderBy(m => m.Name).ToArray();
            MemberInfo[] fromBaseAgain = typeof(MemberHolderBase<>).Project().GetMember("*", mt, bf).Where(m => m.DeclaringType != typeof(object).Project()).OrderBy(m => m.Name).ToArray();
            Assert.Equal(fromBase.Length, fromBaseAgain.Length);

            for (int i = 0; i < fromBase.Length; i++)
            {
                for (int j = 0; j < fromBase.Length; j++)
                {
                    if (i == j)
                    {
                        Assert.True(fromBase[i].Equals(fromBaseAgain[j]));
                        Assert.Equal(fromBase[i].GetHashCode(), fromBaseAgain[j].GetHashCode());
                    }
                    else
                    {
                        Assert.False(fromBase[i].Equals(fromBaseAgain[j]));
                        Assert.False(fromBaseAgain[j].Equals(fromBase[i]));
                    }
                }
            }
        }

        [Fact]
        public static void GenericMethodVsConstructedGenericMethod1()
        {
            MethodInfo m = typeof(MemberHolderBase<int>).Project().GetMethod("MyMethod1");
            MethodInfo mi1 = m.MakeGenericMethod(typeof(long).Project());
            MethodInfo mi2 = m.MakeGenericMethod(typeof(long).Project());
            MethodInfo mi3 = m.MakeGenericMethod(typeof(int).Project());

            Assert.False(m.Equals(mi1));
            Assert.False(mi1.Equals(m));

            Assert.True(mi1.Equals(mi2));
            Assert.True(mi2.Equals(mi1));
            Assert.Equal(mi1.GetHashCode(), mi2.GetHashCode());

            Assert.False(mi1.Equals(mi3));
            Assert.False(mi3.Equals(mi1));
        }

        [Fact]
        public static void GenericMethodVsConstructedGenericMethod2()
        {
            MethodInfo m1 = typeof(MemberHolderBase<int>).Project().GetMethod("MyMethod1");
            MethodInfo m2 = typeof(MemberHolderBase<int>).Project().GetMethod("MyMethod2");
            MethodInfo mi1 = m1.MakeGenericMethod(typeof(long).Project());
            MethodInfo mi2 = m2.MakeGenericMethod(typeof(long).Project());

            Assert.False(mi1.Equals(mi2));
            Assert.False(mi2.Equals(mi1));
        }

        [Fact]
        public static void ParameterEquality1()
        {
            MethodInfo m1 = typeof(MemberHolderBase<int>).Project().GetMethod("MyParameterizedMethod1");
            MethodInfo m2 = typeof(MemberHolderBase<int>).Project().GetMethod("MyParameterizedMethod1");

            ParameterInfo[] pis1 = m1.GetParameters();
            ParameterInfo[] pis2 = m2.GetParameters();

            Assert.Equal(pis1.Length, pis2.Length);
            for (int i = 0; i < pis1.Length; i++)
            {
                Assert.True(pis1[i].Equals(pis2[i]));
                Assert.True(pis2[i].Equals(pis1[i]));
                Assert.Equal(pis1[i].GetHashCode(), pis2[i].GetHashCode());

                for (int j = 0; j < pis1.Length; j++)
                {
                    if (i != j)
                    {
                        Assert.False(pis1[i].Equals(pis1[j]));
                        Assert.False(pis1[j].Equals(pis1[i]));
                    }
                }
            }
        }

        [Fact]
        public static void ParameterEquality2()
        {
            MethodInfo m1 = typeof(MemberHolderBase<int>).Project().GetMethod("MyParameterizedMethod1");
            MethodInfo m2 = typeof(MemberHolderBase<long>).Project().GetMethod("MyParameterizedMethod1");

            ParameterInfo[] pis1 = m1.GetParameters();
            ParameterInfo[] pis2 = m2.GetParameters();

            Assert.Equal(pis1.Length, pis2.Length);
            for (int i = 0; i < pis1.Length; i++)
            {
                Assert.False(pis1[i].Equals(pis2[i]));
                Assert.False(pis2[i].Equals(pis1[i]));
            }
        }

        [Fact]
        public static void ParameterEquality3()
        {
            MethodInfo m1 = typeof(MemberHolderBase<int>).GetMethod("MyParameterizedMethod1");  // Intentionally not projected.
            MethodInfo m2 = typeof(MemberHolderBase<int>).Project().GetMethod("MyParameterizedMethod1");

            if (object.ReferenceEquals(m1, m2))
                return; // Projection is turned off on this so this test is pointless.

            ParameterInfo[] pis1 = m1.GetParameters();
            ParameterInfo[] pis2 = m2.GetParameters();

            Assert.Equal(pis1.Length, pis2.Length);
            for (int i = 0; i < pis1.Length; i++)
            {
                // Ensure our ParameterInfo doesn't panic when it receives a runtime ParameterInfo.
                Assert.False(pis2[i].Equals(pis1[i]));
            }
        }

        private class MemberHolderBase<T>
        {
            public MemberHolderBase() { }
            public MemberHolderBase(int x) { }
            public int MyField1;
            public int MyField2;
            public event Action MyEvent1 { add { } remove { } }
            public event Action MyEvent2 { add { } remove { } }
            public void MyMethod1<M>() { }
            public void MyMethod2<M>() { }
            public void MyParameterizedMethod1(int x, int y) { }
            public void MyParameterizedMethod2(int x, int y) { }
            public int MyProperty1 { get { throw null; } set { throw null; } }
            public int MyProperty2 { get { throw null; } set { throw null; } }
        }

        private class MemberHolder<T> : MemberHolderBase<T>
        {
        }
    }
}
