// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Xunit;

namespace System.Reflection.Tests
{
    public class ParameterInfoTests
    {
        public static IEnumerable<object[]> Parameters_TestData()
        {
            yield return new object[] { typeof(ParameterInfoMetadata), "Method1", new string[] { "str", "iValue", "lValue" }, new Type[] { typeof(string), typeof(int), typeof(long) } };
            yield return new object[] { typeof(ParameterInfoMetadata), "Method2", new string[0], new Type[0] };
            yield return new object[] { typeof(ParameterInfoMetadata), "MethodWithArray", new string[] { "strArray" }, new Type[] { typeof(string[]) } };
            yield return new object[] { typeof(ParameterInfoMetadata), "VirtualMethod", new string[] { "data" }, new Type[] { typeof(long) } };
            yield return new object[] { typeof(ParameterInfoMetadata), "MethodWithRefParameter", new string[] { "str" }, new Type[] { typeof(string).MakeByRefType() } };
            yield return new object[] { typeof(ParameterInfoMetadata), "MethodWithOutParameter", new string[] { "i", "str" }, new Type[] { typeof(int), typeof(string).MakeByRefType() } };
            yield return new object[] { typeof(GenericClass<string>), "GenericMethod", new string[] { "t" }, new Type[] { typeof(string) } };
        }

        [Theory]
        [MemberData(nameof(Parameters_TestData))]
        public void Name(Type type, string methodName, string[] expectedNames, Type[] expectedTypes)
        {
            MethodInfo methodInfo = GetMethod(type, methodName);
            ParameterInfo[] parameters = methodInfo.GetParameters();

            Assert.Equal(expectedNames.Length, parameters.Length);
            for (int i = 0; i < expectedNames.Length; i++)
            {
                Assert.Equal(expectedNames[i], parameters[i].Name);
                Assert.Equal(expectedTypes[i], parameters[i].ParameterType);
                Assert.Equal(i, parameters[i].Position);
                Assert.Equal(methodInfo, parameters[i].Member);
            }
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "Method1", 0)]
        [InlineData(typeof(ParameterInfoMetadata), "VirtualMethod", 0)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithRefParameter", 0)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithOutParameter", 0)]
        [InlineData(typeof(GenericClass<string>), "GenericMethod", 0)]
        public void Member(Type type, string name, int index)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.NotNull(parameterInfo.Member);
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefault1", 1, true)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefault2", 0, true)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefault3", 0, true)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefault4", 0, true)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefaultNullableDateTime", 0, true)]
        [InlineData(typeof(GenericClass<int>), "GenericMethodWithDefault", 1, true)]
        [InlineData(typeof(ParameterInfoMetadata), "Method1", 1, false)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithRefParameter", 0, false)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithOutParameter", 1, false)]
        [InlineData(typeof(GenericClass<int>), "GenericMethod", 0, false)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithEnum", 0, true)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithNullableEnum", 0, true)]
        public void HasDefaultValue(Type type, string name, int index, bool expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.Equal(expected, parameterInfo.HasDefaultValue);
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefaultDateTime", 0, true)]
        public void HasDefaultValue_broken_on_NETFX(Type type, string name, int index, bool expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.Equal(expected, parameterInfo.HasDefaultValue);
        }

        [Fact]
        public void HasDefaultValue_ReturnParam()
        {
            ParameterInfo parameterInfo = GetMethod(typeof(ParameterInfoMetadata), "Method1").ReturnParameter;
            Assert.True(parameterInfo.HasDefaultValue);
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "Method1", 0)]
        [InlineData(typeof(ParameterInfoMetadata), "VirtualMethod", 0)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithRefParameter", 0)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithOutParameter", 0)]
        [InlineData(typeof(GenericClass<string>), "GenericMethod", 0)]
        public void RawDefaultValue(Type type, string name, int index)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.NotNull(parameterInfo.RawDefaultValue);
        }

        [Fact]
        public void RawDefaultValue_Enum()
        {
            ParameterInfo p = GetParameterInfo(typeof(ParameterInfoMetadata), "Foo1", 0);
            object raw = p.RawDefaultValue;
            Assert.Equal(typeof(int), raw.GetType());
            Assert.Equal<int>((int)BindingFlags.DeclaredOnly, (int)raw);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15037", TestRuntimes.Mono)]
        public void RawDefaultValueFromAttribute()
        {
            ParameterInfo p = GetParameterInfo(typeof(ParameterInfoMetadata), "Foo2", 0);
            object raw = p.RawDefaultValue;
            Assert.Equal(typeof(int), raw.GetType());
            Assert.Equal<int>((int)BindingFlags.IgnoreCase, (int)raw);
        }

        [Fact]
        public void RawDefaultValue_MetadataTrumpsAttribute()
        {
            ParameterInfo p = GetParameterInfo(typeof(ParameterInfoMetadata), "Foo3", 0);
            object raw = p.RawDefaultValue;
            Assert.Equal(typeof(int), raw.GetType());
            Assert.Equal<int>((int)BindingFlags.FlattenHierarchy, (int)raw);
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithRefParameter", 0, false)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithOutParameter", 1, true)]
        [InlineData(typeof(ParameterInfoMetadata), "Method1", 1, false)]
        public void IsOut(Type type, string name, int index, bool expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.Equal(expected, parameterInfo.IsOut);
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithOutParameter", 1, false)]
        public void IsIn(Type type, string name, int index, bool expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.Equal(expected, parameterInfo.IsIn);
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefault1", 1, 0)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefault2", 0, "abc")]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefault3", 0, false)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefault4", 0, '\0')]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefaultNullableDateTime", 0, null)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithEnum", 0, AttributeTargets.All)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithNullableEnum", 0, (int)AttributeTargets.All)]
        public void DefaultValue(Type type, string name, int index, object expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.Equal(expected, parameterInfo.DefaultValue);
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefaultDateTime", 0, null)]
        public void DefaultValue_broken_on_NETFX(Type type, string name, int index, object expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.Equal(expected, parameterInfo.DefaultValue);
        }

        [Fact]
        public void DefaultValue_NoDefaultValue()
        {
            ParameterInfo parameterInfo = GetParameterInfo(typeof(ParameterInfoMetadata), "MethodWithOptionalAndNoDefault", 0);
            Assert.Equal(Missing.Value, parameterInfo.DefaultValue);
        }

        [Fact]
        public void DefaultValue_ForByRefLikeArg_ReturnsNull()
        {
            ParameterInfo parameterInfo = GetParameterInfo(typeof(ParameterInfoMetadata), nameof(ParameterInfoMetadata.MethodWithByRefLikeArgWithDefault), 0);
            Assert.True(parameterInfo.HasDefaultValue);
            Assert.Null(parameterInfo.DefaultValue);
            Assert.Null(parameterInfo.RawDefaultValue);
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "Method1", 1, false)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithOutParameter", 1, false)]
        public void IsOptional(Type type, string name, int index, bool expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.Equal(expected, parameterInfo.IsOptional);
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "Method1", 1, false)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithDefault2", 0, false)]
        public void IsRetval(Type type, string name, int index, bool expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.Equal(expected, parameterInfo.IsRetval);
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithOptionalDefaultOutInMarshalParam", 0,
             ParameterAttributes.Optional | ParameterAttributes.HasDefault | ParameterAttributes.HasFieldMarshal | ParameterAttributes.Out | ParameterAttributes.In)]
        public void Attributes(Type type, string name, int index, ParameterAttributes expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);

            Assert.Equal(expected, parameterInfo.Attributes);
        }

        [Theory]
        [MemberData(nameof(s_CustomAttributesTestData))]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/830", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void CustomAttributesTest(Type attrType)
        {
            ParameterInfo parameterInfo = GetParameterInfo(typeof(ParameterInfoMetadata), "MethodWithOptionalDefaultOutInMarshalParam", 0);
            CustomAttributeData attribute = parameterInfo.CustomAttributes.SingleOrDefault(a => a.AttributeType.Equals(attrType));
            Assert.NotNull(attribute);

            Assert.NotNull(attribute);

            ICustomAttributeProvider prov = parameterInfo as ICustomAttributeProvider;
            Assert.NotNull(prov.GetCustomAttributes(attrType, false).SingleOrDefault());
            Assert.NotNull(prov.GetCustomAttributes(attrType, true).SingleOrDefault());
            Assert.NotNull(prov.GetCustomAttributes(false).SingleOrDefault(a => a.GetType().Equals(attrType)));
            Assert.NotNull(prov.GetCustomAttributes(true).SingleOrDefault(a => a.GetType().Equals(attrType)));
            Assert.True(prov.IsDefined(attrType, false));
            Assert.True(prov.IsDefined(attrType, true));
        }

        [Theory]
        [InlineData(0, true, 30)]
        [InlineData(1, false, -1)]
        [InlineData(2, true, 50)]
        public void CustomAttributesInheritanceTest(int paramIndex, bool exists, int expectedMyAttributeValue)
        {
            ParameterInfo parameterInfo = GetParameterInfo(typeof(DerivedParameterInfoMetadata), "VirtualMethodWithCustomAttributes", paramIndex);
            CustomAttributeData attribute = parameterInfo.CustomAttributes.SingleOrDefault(a => a.AttributeType.Equals(typeof(MyAttribute)));
            Assert.Equal(exists, attribute != null);

            ICustomAttributeProvider prov = parameterInfo as ICustomAttributeProvider;
            MyAttribute myAttribute = prov.GetCustomAttributes(typeof(MyAttribute), true).SingleOrDefault() as MyAttribute;
            Assert.Equal(exists, myAttribute != null);
            Assert.Equal(expectedMyAttributeValue, exists ? myAttribute.Value : expectedMyAttributeValue);
        }

        [Fact]
        public static void GetCustomAttributesOnParameterWithNullMetadataTokenReturnsArrayOfCorrectType()
        {
            var parameterWithNullMetadataToken = typeof(int[]).GetProperty(nameof(Array.Length)).GetMethod.ReturnParameter;
            Assert.Equal(typeof(Attribute[]), Attribute.GetCustomAttributes(parameterWithNullMetadataToken).GetType());
            Assert.Equal(typeof(MyAttribute[]), Attribute.GetCustomAttributes(parameterWithNullMetadataToken, typeof(MyAttribute)).GetType());
        }

        [Fact]
        public void VerifyGetCustomAttributesData()
        {
            ParameterInfo p = GetParameterInfo(typeof(ParameterInfoMetadata), "MethodWithCustomAttribute", 0);
            foreach (CustomAttributeData cad in p.GetCustomAttributesData())
            {
                if (cad.AttributeType == typeof(MyAttribute))
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

        public static IEnumerable<object[]> s_CustomAttributesTestData
        {
            get
            {
                yield return new object[] { typeof(OptionalAttribute) };
                yield return new object[] { typeof(OutAttribute) };
                yield return new object[] { typeof(InAttribute) };
                yield return new object[] { typeof(MarshalAsAttribute) };
            }
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "Method1", 0, new Type[0])]
        [InlineData(typeof(ParameterInfoMetadata), "VirtualMethod", 0, new Type[0])]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithRefParameter", 0, new Type[0])]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithOutParameter", 0, new Type[0])]
        [InlineData(typeof(GenericClass<string>), "GenericMethod", 0, new Type[0])]
        public void GetOptionalCustomModifiers(Type type, string name, int index, Type[] expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.Equal(expected, parameterInfo.GetOptionalCustomModifiers());
        }

        [Theory]
        [InlineData(typeof(ParameterInfoMetadata), "Method1", 0, new Type[0])]
        [InlineData(typeof(ParameterInfoMetadata), "VirtualMethod", 0, new Type[0])]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithRefParameter", 0, new Type[0])]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithOutParameter", 0, new Type[0])]
        [InlineData(typeof(GenericClass<string>), "GenericMethod", 0, new Type[0])]
        public void GetRequiredCustomModifiers(Type type, string name, int index, Type[] expected)
        {
            ParameterInfo parameterInfo = GetParameterInfo(type, name, index);
            Assert.Equal(expected, parameterInfo.GetRequiredCustomModifiers());
        }

        [Theory]
        [MemberData(nameof(VerifyParameterInfoGetRealObjectWorks_TestData))]
        public void VerifyParameterInfoGetRealObjectWorks(MemberInfo pretendMember, int pretendPosition, string expectedParameterName)
        {
            // Regression test for https://github.com/dotnet/runtime/issues/22081
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

        private static ParameterInfo GetParameterInfo(Type type, string name, int index)
        {
            ParameterInfo[] parameters = GetMethod(type, name).GetParameters();
            return parameters[index];
        }

        private static MethodInfo GetMethod(Type type, string name)
        {
            return type.GetTypeInfo().DeclaredMethods.FirstOrDefault(methodInfo => methodInfo.Name.Equals(name));
        }

        // Metadata for reflection
        public class ParameterInfoMetadata
        {
            public void Foo1(BindingFlags bf = BindingFlags.DeclaredOnly) { }
            public void Foo2([CustomBindingFlags(Value = BindingFlags.IgnoreCase)] BindingFlags bf) { }
            public void Foo3([CustomBindingFlags(Value = BindingFlags.DeclaredOnly)] BindingFlags bf = BindingFlags.FlattenHierarchy ) { }

            public void MethodWithCustomAttribute([My(2)]string str, int iValue, long lValue) { }
            public virtual void VirtualMethodWithCustomAttributes([My(3)]int val1, [My(4)]int val2, int val3) { }

            public void Method1(string str, int iValue, long lValue) { }
            public void Method2() { }
            public void MethodWithArray(string[] strArray) { }

            public virtual void VirtualMethod(long data) { }

            public void MethodWithRefParameter(ref string str) { str = "newstring"; }
            public void MethodWithOutParameter(int i, out string str) { str = "newstring"; }

            public int MethodWithDefault1(long lValue, int iValue = 0) { return 1; }
            public int MethodWithDefault2(string str = "abc") { return 1; }
            public int MethodWithDefault3(bool result = false) { return 1; }
            public int MethodWithDefault4(char c = '\0') { return 1; }

            public void MethodWithDefaultDateTime(DateTime arg = default(DateTime)) { }
            public void MethodWithDefaultNullableDateTime(DateTime? arg = default(DateTime?)) { }

            public void MethodWithByRefLikeArgWithDefault(MyByRefLikeStruct arg = default) { }

            public void MethodWithEnum(AttributeTargets arg = AttributeTargets.All) { }
            public void MethodWithNullableEnum(AttributeTargets? arg = AttributeTargets.All) { }

            public int MethodWithOptionalAndNoDefault([Optional] object o) { return 1; }
            public int MethodWithOptionalDefaultOutInMarshalParam([MarshalAs(UnmanagedType.LPWStr)][Out][In] string str = "") { return 1; }
        }

        public class DerivedParameterInfoMetadata : ParameterInfoMetadata
        {
            override public void VirtualMethodWithCustomAttributes([My(30)]int val1, int val2, [My(50)]int val3) { }
        }

        public class GenericClass<T>
        {
            public void GenericMethod(T t) { }
            public string GenericMethodWithDefault(int i, T t = default(T)) { return "somestring"; }
        }

        private class MyAttribute : Attribute
        {
            public int Value {get; private set;}
            internal MyAttribute(int i) { Value = i;}
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

        public ref struct MyByRefLikeStruct
        {
            public int MyInt;
        }
    }
}
