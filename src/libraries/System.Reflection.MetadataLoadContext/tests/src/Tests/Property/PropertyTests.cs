// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class PropertyTests
    {
        [Fact]
        public static void TestProperties_GetterSetter()
        {
            Type t = typeof(DerivedFromPropertyHolder1<>).Project();
            Type bt = t.BaseType;

            {
                PropertyInfo p = t.GetProperty(nameof(DerivedFromPropertyHolder1<int>.ReadOnlyProp));
                Assert.Equal(nameof(DerivedFromPropertyHolder1<int>.ReadOnlyProp), p.Name);
                Assert.Equal(bt, p.DeclaringType);
                Assert.Equal(t, p.ReflectedType);
                Assert.True(p.CanRead);
                Assert.False(p.CanWrite);

                MethodInfo getter = p.GetMethod;
                Assert.Equal("get_" + nameof(PropertyHolder1<int>.ReadOnlyProp), getter.Name);
                Assert.Equal(bt, getter.DeclaringType);
                Assert.Equal(t, getter.ReflectedType);
                Assert.Null(p.SetMethod);
                Assert.Null(p.GetSetMethod(nonPublic: true));
                Assert.Equal(0, p.GetIndexParameters().Length);
                Assert.Equal(typeof(int).Project(), p.PropertyType);
            }

            {
                PropertyInfo p = t.GetProperty(nameof(DerivedFromPropertyHolder1<int>.ReadWriteProp));
                Type theT = t.GetGenericTypeParameters()[0];

                Assert.Equal(nameof(DerivedFromPropertyHolder1<int>.ReadWriteProp), p.Name);
                Assert.Equal(bt, p.DeclaringType);
                Assert.Equal(t, p.ReflectedType);

                MethodInfo getter = p.GetMethod;
                Assert.Equal("get_" + nameof(PropertyHolder1<int>.ReadWriteProp), getter.Name);
                Assert.Equal(bt, getter.DeclaringType);
                Assert.Equal(t, getter.ReflectedType);

                MethodInfo setter = p.SetMethod;
                Assert.Equal("set_" + nameof(PropertyHolder1<int>.ReadWriteProp), setter.Name);
                Assert.Equal(bt, setter.DeclaringType);
                Assert.Equal(t, setter.ReflectedType);

                MethodInfo[] accessors = p.GetAccessors(nonPublic: false);
                Assert.Equal<MethodInfo>(new MethodInfo[] { getter, setter }, accessors);

                Assert.Equal(0, p.GetIndexParameters().Length);
                Assert.Equal(theT, p.PropertyType);
            }

            {
                PropertyInfo p = bt.GetProperty(nameof(DerivedFromPropertyHolder1<int>.PublicPrivateProp));
                Type theT = t.GetGenericTypeDefinition().GetGenericTypeParameters()[0];
                Assert.True(p.CanRead);
                Assert.True(p.CanWrite);

                Assert.Equal(nameof(DerivedFromPropertyHolder1<int>.PublicPrivateProp), p.Name);
                Assert.Equal(bt, p.DeclaringType);
                Assert.Equal(bt, p.ReflectedType);

                MethodInfo getter = p.GetMethod;
                Assert.Equal("get_" + nameof(PropertyHolder1<int>.PublicPrivateProp), getter.Name);
                Assert.Equal(bt, getter.DeclaringType);
                Assert.Equal(bt, getter.ReflectedType);

                MethodInfo setter = p.SetMethod;
                Assert.Equal("set_" + nameof(PropertyHolder1<int>.PublicPrivateProp), setter.Name);
                Assert.Equal(bt, setter.DeclaringType);
                Assert.Equal(bt, setter.ReflectedType);

                Assert.Equal(setter, p.GetSetMethod(nonPublic: true));
                Assert.Null(p.GetSetMethod(nonPublic: false));

                MethodInfo[] accessors = p.GetAccessors(nonPublic: true);
                Assert.Equal<MethodInfo>(new MethodInfo[] { getter, setter }, accessors);

                accessors = p.GetAccessors(nonPublic: false);
                Assert.Equal<MethodInfo>(new MethodInfo[] { getter }, accessors);

                Assert.Equal(0, p.GetIndexParameters().Length);

                Assert.Equal(typeof(GenericClass1<>).Project().MakeGenericType(theT), p.PropertyType);
            }

            {
                PropertyInfo p = t.GetProperty(nameof(DerivedFromPropertyHolder1<int>.PublicPrivateProp));
                Type theT = t.GetGenericTypeDefinition().GetGenericTypeParameters()[0];
                Assert.True(p.CanRead);
                Assert.False(p.CanWrite);

                Assert.Equal(nameof(DerivedFromPropertyHolder1<int>.PublicPrivateProp), p.Name);
                Assert.Equal(bt, p.DeclaringType);
                Assert.Equal(t, p.ReflectedType);

                MethodInfo getter = p.GetMethod;
                Assert.Equal("get_" + nameof(PropertyHolder1<int>.PublicPrivateProp), getter.Name);
                Assert.Equal(bt, getter.DeclaringType);
                Assert.Equal(t, getter.ReflectedType);

                Assert.Null(p.GetSetMethod(nonPublic: true));
                Assert.Null(p.GetSetMethod(nonPublic: false));

                MethodInfo[] accessors = p.GetAccessors(nonPublic: true);
                Assert.Equal<MethodInfo>(new MethodInfo[] { getter }, accessors);

                Assert.Equal(0, p.GetIndexParameters().Length);

                Assert.Equal(typeof(GenericClass1<>).Project().MakeGenericType(theT), p.PropertyType);
            }

            {
                PropertyInfo p = t.GetProperty(nameof(DerivedFromPropertyHolder1<int>.PublicInternalProp));

                Assert.Equal(nameof(DerivedFromPropertyHolder1<int>.PublicInternalProp), p.Name);
                Assert.Equal(bt, p.DeclaringType);
                Assert.Equal(t, p.ReflectedType);

                Assert.True(p.CanRead);
                Assert.True(p.CanWrite);

                MethodInfo getter = p.GetMethod;
                Assert.Equal("get_" + nameof(PropertyHolder1<int>.PublicInternalProp), getter.Name);
                Assert.Equal(bt, getter.DeclaringType);
                Assert.Equal(t, getter.ReflectedType);

                MethodInfo setter = p.SetMethod;
                Assert.Equal("set_" + nameof(PropertyHolder1<int>.PublicInternalProp), setter.Name);
                Assert.Equal(bt, setter.DeclaringType);
                Assert.Equal(t, setter.ReflectedType);

                Assert.Equal(setter, p.GetSetMethod(nonPublic: true));
                Assert.Null(p.GetSetMethod(nonPublic: false));

                MethodInfo[] accessors = p.GetAccessors(nonPublic: true);
                Assert.Equal<MethodInfo>(new MethodInfo[] { getter, setter }, accessors);

                accessors = p.GetAccessors(nonPublic: false);
                Assert.Equal<MethodInfo>(new MethodInfo[] { getter }, accessors);

                Assert.Equal(0, p.GetIndexParameters().Length);

                Assert.Equal(typeof(int).Project(), p.PropertyType);
            }

            {
                PropertyInfo p = t.GetProperty(nameof(DerivedFromPropertyHolder1<int>.PublicProtectedProp));

                Assert.Equal(nameof(DerivedFromPropertyHolder1<int>.PublicProtectedProp), p.Name);
                Assert.Equal(bt, p.DeclaringType);
                Assert.Equal(t, p.ReflectedType);

                Assert.True(p.CanRead);
                Assert.True(p.CanWrite);

                MethodInfo getter = p.GetMethod;
                Assert.Equal("get_" + nameof(PropertyHolder1<int>.PublicProtectedProp), getter.Name);
                Assert.Equal(bt, getter.DeclaringType);
                Assert.Equal(t, getter.ReflectedType);

                MethodInfo setter = p.SetMethod;
                Assert.Equal("set_" + nameof(PropertyHolder1<int>.PublicProtectedProp), setter.Name);
                Assert.Equal(bt, setter.DeclaringType);
                Assert.Equal(t, setter.ReflectedType);

                Assert.Equal(setter, p.GetSetMethod(nonPublic: true));
                Assert.Null(p.GetSetMethod(nonPublic: false));

                MethodInfo[] accessors = p.GetAccessors(nonPublic: true);
                Assert.Equal<MethodInfo>(new MethodInfo[] { getter, setter }, accessors);

                accessors = p.GetAccessors(nonPublic: false);
                Assert.Equal<MethodInfo>(new MethodInfo[] { getter }, accessors);

                Assert.Equal(0, p.GetIndexParameters().Length);

                Assert.Equal(typeof(int).Project(), p.PropertyType);
            }

            {
                PropertyInfo p = t.GetProperty("Item");
                Type theT = t.GetGenericTypeDefinition().GetGenericTypeParameters()[0];

                Assert.Equal("Item", p.Name);
                Assert.Equal(bt, p.DeclaringType);
                Assert.Equal(t, p.ReflectedType);

                MethodInfo getter = p.GetMethod;
                Assert.Equal("get_Item", getter.Name);
                Assert.Equal(bt, getter.DeclaringType);
                Assert.Equal(t, getter.ReflectedType);

                ParameterInfo[] pis = p.GetIndexParameters();
                Assert.Equal(2, pis.Length);
                for (int i = 0; i < pis.Length; i++)
                {
                    ParameterInfo pi = pis[i];
                    Assert.Equal(i, pi.Position);
                    Assert.Equal(p, pi.Member);
                }

                Assert.Equal("i", pis[0].Name);
                Assert.Equal(typeof(int).Project(), pis[0].ParameterType);
                Assert.Equal("t", pis[1].Name);
                Assert.Equal(theT, pis[1].ParameterType);
            }
        }

        [Fact]
        public unsafe static void TestCustomModifiers1()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new CoreMetadataAssemblyResolver(), "mscorlib"))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_CustomModifiersImage);
                Type t = a.GetType("N", throwOnError: true);
                Type reqA = a.GetType("ReqA", throwOnError: true);
                Type reqB = a.GetType("ReqB", throwOnError: true);
                Type reqC = a.GetType("ReqC", throwOnError: true);
                Type optA = a.GetType("OptA", throwOnError: true);
                Type optB = a.GetType("OptB", throwOnError: true);
                Type optC = a.GetType("OptC", throwOnError: true);

                PropertyInfo p = t.GetProperty("MyProperty");
                Type[] req = p.GetRequiredCustomModifiers();
                Type[] opt = p.GetOptionalCustomModifiers();

                Assert.Equal<Type>(new Type[] { reqA, reqB, reqC }, req);
                Assert.Equal<Type>(new Type[] { optA, optB, optC }, opt);

                TestUtils.AssertNewObjectReturnedEachTime(() => p.GetRequiredCustomModifiers());
                TestUtils.AssertNewObjectReturnedEachTime(() => p.GetOptionalCustomModifiers());
            }
        }
    }
}
