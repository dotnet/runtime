// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineGenericParameters
    {
        public static IEnumerable<object[]> DefineGenericParameters_TestData()
        {
            yield return new object[] { new string[] { "TFirst" } };
            yield return new object[] { new string[] { "T", "U" } };
            yield return new object[] { new string[] { "T1", "T2", "T3" } };
            yield return new object[] { new string[] { "\uD800\uDC00", "\0", "a\0\b" } };
        }

        [Theory]
        [MemberData(nameof(DefineGenericParameters_TestData))]
        public void DefineGenericParameters(string[] typeParamNames)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            GenericTypeParameterBuilder[] genericParameters = type.DefineGenericParameters(typeParamNames);
            Assert.True(type.IsGenericType);
            Assert.True(type.IsGenericTypeDefinition);
            for (int i = 0; i < typeParamNames.Length; i++)
            {
                GenericTypeParameterBuilder parameter = genericParameters[i];
                Assert.Equal(typeParamNames[i], parameter.Name);
                Assert.Null(parameter.Namespace);

                Assert.Null(parameter.FullName);
                Assert.Null(parameter.AssemblyQualifiedName);

                Assert.Equal(type.AsType(), parameter.DeclaringType);
                Assert.Null(parameter.DeclaringMethod);
                Assert.Equal(type.Module, parameter.Module);
                Assert.Equal(type.Assembly, parameter.Assembly);

                Assert.Null(parameter.BaseType);

                Assert.Equal(i, parameter.GenericParameterPosition);
                Assert.Equal(GenericParameterAttributes.None, parameter.GenericParameterAttributes);

                Assert.True(parameter.IsGenericParameter);
                Assert.False(parameter.IsGenericType);
                Assert.False(parameter.IsGenericTypeDefinition);
                Assert.True(parameter.ContainsGenericParameters);
            }
        }

        [Fact]
        public void DefineGenericParameters_NullNames_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("names", () => type.DefineGenericParameters(null));
        }

        [Fact]
        public void DefineGenericParameters_EmptyNames_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentException>(null, () => type.DefineGenericParameters(new string[0]));
        }

        [Fact]
        public void DefineGenericParameters_NullName_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("names", () => type.DefineGenericParameters(new string[] { null }));
        }

        [Fact]
        public void DefineGenericParameters_AlreadyDefinedGenericParameters_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.DefineGenericParameters("T");
            Assert.Throws<InvalidOperationException>(() => type.DefineGenericParameters("T"));
        }
    }
}
