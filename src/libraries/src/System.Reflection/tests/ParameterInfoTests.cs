// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        [InlineData(typeof(GenericClass<int>), "GenericMethodWithDefault", 1, true)]
        [InlineData(typeof(ParameterInfoMetadata), "Method1", 1, false)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithRefParameter", 0, false)]
        [InlineData(typeof(ParameterInfoMetadata), "MethodWithOutParameter", 1, false)]
        [InlineData(typeof(GenericClass<int>), "GenericMethod", 0, false)]
        public void HasDefaultValue(Type type, string name, int index, bool expected)
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
        public void DefaultValue(Type type, string name, int index, object expected)
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

        public static IEnumerable<object[]> s_CustomAttributesTestData
        {
            get
            {
                yield return new object[] { typeof(OptionalAttribute) };
                yield return new object[] { typeof(OutAttribute) };
                yield return new object[] { typeof(InAttribute) };
                if (!PlatformDetection.IsNetNative) // Native Metadata format does not expose FieldMarshal info: https://github.com/dotnet/corert/issues/3366
                {
                    yield return new object[] { typeof(MarshalAsAttribute) };
                }
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

            public int MethodWithOptionalAndNoDefault([Optional] object o) { return 1; }
            public int MethodWithOptionalDefaultOutInMarshalParam([MarshalAs(UnmanagedType.LPWStr)][Out][In] string str = "") { return 1; }
        }

        public class GenericClass<T>
        {
            public void GenericMethod(T t) { }
            public string GenericMethodWithDefault(int i, T t = default(T)) { return "somestring"; }
        }
    }
}
