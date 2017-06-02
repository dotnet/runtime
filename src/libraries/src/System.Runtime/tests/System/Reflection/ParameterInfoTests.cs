// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
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

        [Theory]
        [MemberData(nameof(VerifyParameterInfoGetRealObjectWorks_TestData))]
        public static void VerifyParameterInfoGetRealObjectWorks(MemberInfo pretendMember, int pretendPosition, string expectedParameterName)
        {
            // Regression test for https://github.com/dotnet/corefx/issues/20574
            //
            // It's easy to forget that ParameterInfo's and runtime-implemented ParameterInfo's are different objects and just because the
            // latter doesn't support serialization doesn't mean other providers won't either. 
            //
            // For historical reasons, ParameterInfo contains some serialization support that subtypes can optionally hang off. This
            // test ensures that support doesn't get vaporized.

            // Just pretend that we're BinaryFormatter and are deserializing a Parameter...
            IObjectReference podParameter = new PodPersonParameterInfo(pretendMember, pretendPosition);
            StreamingContext sc = new StreamingContext(StreamingContextStates.Clone);
            ParameterInfo result = (ParameterInfo)(podParameter.GetRealObject(sc));

            Assert.Equal(pretendPosition, result.Position);
            Assert.Equal(expectedParameterName, result.Name);
            Assert.Equal(pretendMember.Name, result.Member.Name);
        }

        public static IEnumerable<object[]> VerifyParameterInfoGetRealObjectWorks_TestData
        {
            get
            {
                Type t = typeof(PretendParent);
                ConstructorInfo ctor = t.GetConstructor(new Type[] { typeof(int), typeof(int) });
                MethodInfo method = t.GetMethod(nameof(PretendParent.PretendMethod));
                PropertyInfo property = t.GetProperty("Item");

                yield return new object[] { ctor, 0, "a" };
                yield return new object[] { ctor, 1, "b" };
                yield return new object[] { method, -1, null };
                yield return new object[] { method, 0, "x" };
                yield return new object[] { method, 1, "y" };
                yield return new object[] { property, 0, "index1" };
                yield return new object[] { property, 1, "index2" };
            }
        }

        private sealed class PretendParent
        {
            public PretendParent(int a, int b) { }
            public void PretendMethod(int x, int y) { }
            public int this[int index1, int index2] { get { throw null; } }
        }

        private sealed class PodPersonParameterInfo : MockParameterInfo
        {
            public PodPersonParameterInfo(MemberInfo pretendMember, int pretendPosition)
            {
                // Serialization can recreate a ParameterInfo from just these two pieces of data. Of course, this is just a test and no one 
                // ever told this Member that it was adopting a counterfeit Parameter, but this is just a test...
                MemberImpl = pretendMember;
                PositionImpl = pretendPosition;
            }
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