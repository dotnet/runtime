// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class ParameterTests
    {
        [Fact]
        public static void TestRawDefaultValue1()
        {
            ParameterInfo p = typeof(ParametersWithDefaultValues).Project().GetTypeInfo().GetDeclaredMethod("Foo1").GetParameters()[0];
            Assert.False(p.HasDefaultValue);
            Assert.Equal(DBNull.Value, p.RawDefaultValue);
        }

        [Fact]
        public static void TestRawDefaultValue2()
        {
            ParameterInfo p = typeof(ParametersWithDefaultValues).Project().GetTypeInfo().GetDeclaredMethod("Foo2").GetParameters()[0];
            Assert.False(p.HasDefaultValue);
            Assert.Equal(Missing.Value, p.RawDefaultValue);
        }

        [Fact]
        public static void TestRawDefaultValue3()
        {
            ParameterInfo p = typeof(ParametersWithDefaultValues).Project().GetTypeInfo().GetDeclaredMethod("Foo3").GetParameters()[0];
            Assert.True(p.HasDefaultValue);
            object dv = p.RawDefaultValue;
            Assert.True(dv is int);
            Assert.Equal(42, (int)dv);
        }

        [Fact]
        public static void TestRawDefaultValue4()
        {
            ParameterInfo p = typeof(ParametersWithDefaultValues).Project().GetTypeInfo().GetDeclaredMethod("Foo4").GetParameters()[0];
            Assert.True(p.HasDefaultValue);
            object dv = p.RawDefaultValue;
            Assert.True(dv is short);
            Assert.Equal(-34, (short)dv);
        }

        [Fact]
        public static void TestRawDefaultValue5()
        {
            ParameterInfo p = typeof(ParametersWithDefaultValues).Project().GetTypeInfo().GetDeclaredMethod("Foo5").GetParameters()[0];
            Assert.True(p.HasDefaultValue);
            object dv = p.RawDefaultValue;
            Assert.True(dv is decimal);
            Assert.Equal(1234m, (decimal)dv);
        }

        [Fact]
        public static void TestRawDefaultValue6()
        {
            ParameterInfo p = typeof(ParametersWithDefaultValues).Project().GetTypeInfo().GetDeclaredMethod("Foo6").GetParameters()[0];
            Assert.True(p.HasDefaultValue);
            object dv = p.RawDefaultValue;
            Assert.True(dv is DateTime);
            Assert.Equal(8736726782, ((DateTime)dv).Ticks);
        }

        [Fact]
        public static void TestRawDefaultValue7()
        {
            var parameters = typeof(ParametersWithDefaultValues).Project().GetTypeInfo().GetDeclaredMethod("Foo7").GetParameters();
            ParameterInfo p1 = parameters[0];
            Assert.True(p1.HasDefaultValue);
            object dv1 = p1.RawDefaultValue;
            Assert.Equal("foo", dv1);
            ParameterInfo p2 = parameters[1];
            Assert.True(p2.HasDefaultValue);
            object dv2 = p2.RawDefaultValue;
            Assert.Equal("", dv2);
            ParameterInfo p3 = parameters[2];
            Assert.True(p3.HasDefaultValue);
            Assert.Null(p3.RawDefaultValue);
        }

        [Fact]
        public static void TestRawDefaultValue8()
        {
            ParameterInfo p = typeof(ParametersWithDefaultValues).Project().GetTypeInfo().GetDeclaredMethod("Foo8").GetParameters()[0];
            Assert.True(p.HasDefaultValue);
            object dv = p.RawDefaultValue;
            Assert.Null(dv);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15340", TestRuntimes.Mono)]
        public static void TestPseudoCustomAttributes()
        {
            MethodInfo m = typeof(ParametersWithPseudoCustomtAttributes).Project().GetTypeInfo().GetDeclaredMethod("Foo");
            ParameterInfo[] pis = m.GetParameters();
            {
                ParameterInfo p = pis[0];
                CustomAttributeData cad = p.CustomAttributes.Single(c => c.AttributeType == typeof(InAttribute).Project());
                InAttribute i = cad.UnprojectAndInstantiate<InAttribute>();
            }

            {
                ParameterInfo p = pis[1];
                CustomAttributeData cad = p.CustomAttributes.Single(c => c.AttributeType == typeof(OutAttribute).Project());
                OutAttribute o = cad.UnprojectAndInstantiate<OutAttribute>();
            }

            {
                ParameterInfo p = pis[2];
                CustomAttributeData cad = p.CustomAttributes.Single(c => c.AttributeType == typeof(OptionalAttribute).Project());
                OptionalAttribute o = cad.UnprojectAndInstantiate<OptionalAttribute>();
            }

            {
                ParameterInfo p = pis[3];
                CustomAttributeData cad = p.CustomAttributes.Single(c => c.AttributeType == typeof(MarshalAsAttribute).Project());
                MarshalAsAttribute ma = cad.UnprojectAndInstantiate<MarshalAsAttribute>();
                Assert.Equal(UnmanagedType.I4, ma.Value);
            }
        }

#if NET
        [Theory]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(Dictionary<string, List<int[]>>))]
        public static void TestModifiedParameterTypeOfSubstitutedGenericParameter(Type argumentType)
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            // ClassWithMethods1<T>.Method1(int x, T t) with T substituted by the argument type.
            Type projectedArgument = argumentType.Project();
            Type t = typeof(ClassWithMethods1<>).Project().MakeGenericType(projectedArgument);
            ParameterInfo classParameter = t.GetMethod("Method1", bf).GetParameters()[1];
            Assert.Equal(projectedArgument, classParameter.ParameterType);
            VerifyModifiedType(projectedArgument, classParameter.GetModifiedParameterType());

            // GenericClassWithGenericMethods1<T, U>.GenericMethod1<M, N>(GenericClass5<N, M[], IEnumerable<U>, T[,], int> g)
            // with both the type and the method generic parameters substituted.
            Type gt = typeof(GenericClassWithGenericMethods1<,>).Project().MakeGenericType(projectedArgument, typeof(string).Project());
            MethodInfo gm = gt.GetMethod("GenericMethod1", bf).MakeGenericMethod(typeof(object).Project(), projectedArgument);
            ParameterInfo methodParameter = gm.GetParameters()[0];
            Type expected = typeof(GenericClass5<,,,,>).Project().MakeGenericType(
                projectedArgument,
                typeof(object).Project().MakeArrayType(),
                typeof(IEnumerable<>).Project().MakeGenericType(typeof(string).Project()),
                projectedArgument.MakeArrayType(2),
                typeof(int).Project());
            Assert.Equal(expected, methodParameter.ParameterType);
            VerifyModifiedType(expected, methodParameter.GetModifiedParameterType());
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(List<int>))]
        public static void TestModifiersOnSubstitutedGenericParameters(Type argumentType)
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new CoreMetadataAssemblyResolver(), "mscorlib"))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SubstitutedGenericModifiersImage);
                Type genericModifiers = a.GetType("GenericModifiers", throwOnError: true);
                Type foo = a.GetType("FooModifier", throwOnError: true);
                Type bar = a.GetType("BarModifier", throwOnError: true);
                Assembly core = lc.CoreAssembly;
                Type argument = argumentType.IsGenericType
                    ? core.GetType(argumentType.GetGenericTypeDefinition().FullName, throwOnError: true).MakeGenericType(core.GetType(typeof(int).FullName, throwOnError: true))
                    : core.GetType(argumentType.FullName, throwOnError: true);
                Type intType = core.GetType(typeof(int).FullName, throwOnError: true);
                Type listDefinition = core.GetType(typeof(List<>).FullName, throwOnError: true);

                Type Modified(string name) =>
                    genericModifiers.GetMethod(name).MakeGenericMethod(argument).GetParameters()[0].GetModifiedParameterType();

                // The modifier on the generic parameter itself survives substitution.
                Type modOpt = Modified("ModOptOnGenericParameter");
                Assert.Equal(argument, modOpt.UnderlyingSystemType);
                Assert.Equal<Type>(new[] { foo }, modOpt.GetOptionalCustomModifiers());
                Assert.Empty(modOpt.GetRequiredCustomModifiers());
                if (argument.IsGenericType)
                {
                    // The arguments of a substituted type have no signature of their own and no modifiers.
                    Type inner = modOpt.GetGenericArguments()[0];
                    Assert.Equal(intType, inner.UnderlyingSystemType);
                    Assert.Empty(inner.GetOptionalCustomModifiers());
                }

                // A modifier inside a generic instantiation stays on the substituted argument. The
                // instantiation decoded from the signature is a distinct instance from one made with
                // MakeGenericType, so compare it by definition and argument.
                Type modOptInArgument = Modified("ModOptInGenericArgument");
                Assert.Equal(listDefinition, modOptInArgument.UnderlyingSystemType.GetGenericTypeDefinition());
                Assert.Empty(modOptInArgument.GetOptionalCustomModifiers());
                Type listArgument = modOptInArgument.GetGenericArguments()[0];
                Assert.Equal(argument, listArgument.UnderlyingSystemType);
                Assert.Equal<Type>(new[] { foo }, listArgument.GetOptionalCustomModifiers());

                // A required modifier under a byref stays on the element.
                Type modReqByRef = Modified("ModReqOnByRefGenericParameter");
                Assert.True(modReqByRef.UnderlyingSystemType.IsByRef);
                Type element = modReqByRef.GetElementType();
                Assert.Equal(argument, element.UnderlyingSystemType);
                Assert.Equal<Type>(new[] { bar }, element.GetRequiredCustomModifiers());
                Assert.Empty(element.GetOptionalCustomModifiers());
            }
        }

        private static void VerifyModifiedType(Type expected, Type modified)
        {
            Assert.NotNull(modified);
            Assert.NotSame(expected, modified);
            Assert.Same(expected, modified.UnderlyingSystemType);
            Assert.Empty(modified.GetRequiredCustomModifiers());
            Assert.Empty(modified.GetOptionalCustomModifiers());
            if (expected.HasElementType)
            {
                VerifyModifiedType(expected.GetElementType(), modified.GetElementType());
            }
            else if (expected.IsGenericType)
            {
                Type[] expectedArguments = expected.GetGenericArguments();
                Type[] modifiedArguments = modified.GetGenericArguments();
                Assert.Equal(expectedArguments.Length, modifiedArguments.Length);
                for (int i = 0; i < expectedArguments.Length; i++)
                    VerifyModifiedType(expectedArguments[i], modifiedArguments[i]);
            }
        }
#endif
    }
}
