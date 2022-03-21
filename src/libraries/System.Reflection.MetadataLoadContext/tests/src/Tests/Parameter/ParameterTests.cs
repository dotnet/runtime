// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
