// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderDefineGenericParameters
    {
        public static IEnumerable<object[]> DefineGenericParameters_TestData()
        {
            yield return new object[] { new string[] { "T" } };
            yield return new object[] { new string[] { "T", "U" } };
            yield return new object[] { new string[] { "T1", "T2", "T3" } };
        }

        [Theory]
        [MemberData(nameof(DefineGenericParameters_TestData))]
        public void DefineGenericParameters_TwoTypeParameters(string[] names)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            GenericTypeParameterBuilder[] parameters = method.DefineGenericParameters(names);
            Assert.True(method.IsGenericMethod);
            Assert.True(method.IsGenericMethodDefinition);
            Assert.Equal(names.Length, parameters.Length);

            for (int i = 0; i < names.Length; i++)
            {
                GenericTypeParameterBuilder parameter = parameters[i];
                Assert.Equal(method, parameter.DeclaringMethod);
                Assert.Equal(names[i], parameters[i].Name);
                Assert.Equal(i, parameters[i].GenericParameterPosition);
            }
        }

        [Fact]
        public void DefineGenericParameter_MultipleParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public, typeof(int), new Type[0]);
            method.DefineGenericParameters(new string[] { "T", "U", "V" });

            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, 100);
            ilGenerator.Emit(OpCodes.Ret);

            Type resultType = type.CreateTypeInfo().AsType();
            Type[] typeArguments = { typeof(int), typeof(string), typeof(object) };
            MethodInfo constructedMethod = resultType.GetMethod("TestMethod").MakeGenericMethod(typeArguments);
            Assert.Equal(typeArguments, constructedMethod.GetGenericArguments());
        }

        [Fact]
        public void DefineGenericParameters_SingleTypeParameter_SetImplementationFlagsCalled_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            method.SetImplementationFlags(MethodImplAttributes.Managed);
            Assert.Throws<InvalidOperationException>(() => method.DefineGenericParameters("T"));
        }

        [Fact]
        public void DefineGenericParameters_TwoTypeParameters_SetImplementationFlagsCalled_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static);

            method.SetImplementationFlags(MethodImplAttributes.IL | MethodImplAttributes.Managed | MethodImplAttributes.Synchronized | MethodImplAttributes.NoInlining);

            Assert.Throws<InvalidOperationException>(() => method.DefineGenericParameters("T", "U"));
        }

        [Fact]
        public void DefineGenericParameters_SingleTypeParameter_AlreadyDefined_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            method.DefineGenericParameters("T");
            Assert.Throws<InvalidOperationException>(() => method.DefineGenericParameters("T"));
        }

        [Fact]
        public void DefineGenericParameters_TwoTypeParameters_AlreadyDefined_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static);

            method.DefineGenericParameters("T", "U");
            Assert.Throws<InvalidOperationException>(() => method.DefineGenericParameters("M", "K"));
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic, MethodAttributes.Public)]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public | MethodAttributes.Static)]
        public void DefineGenericParameters_NullNames_ThrowsArgumentNullException(TypeAttributes typeAttributes, MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", methodAttributes);
            AssertExtensions.Throws<ArgumentNullException>("names", () => method.DefineGenericParameters(null));
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic, MethodAttributes.Public)]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public | MethodAttributes.Static)]
        public void DefineGenericParameters_NamesContainsNull_ThrowsArgumentNullException(TypeAttributes typeAttributes, MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Test", methodAttributes);
            string[] typeParamNames = new string[] { "T", null, "U" };
            AssertExtensions.Throws<ArgumentNullException>("names", () => method.DefineGenericParameters(typeParamNames));
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic, MethodAttributes.Public)]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public | MethodAttributes.Static)]
        public void DefineGenericParameters_EmptyNames_ThrowsArgumentException(TypeAttributes typeAttributes, MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder builder = type.DefineMethod("TestMethod", methodAttributes);
            AssertExtensions.Throws<ArgumentException>("names", () => builder.DefineGenericParameters());
        }

        [Fact]
        public void DefineGenericParameters_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static);
            method.GetILGenerator().Emit(OpCodes.Ret);

            Type resultType = type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => method.DefineGenericParameters("T", "U"));
        }
    }
}
