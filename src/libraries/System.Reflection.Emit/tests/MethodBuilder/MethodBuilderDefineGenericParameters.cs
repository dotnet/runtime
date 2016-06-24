// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderDefineGenericParameters
    {
        [Fact]
        public void DefineGenericParameters_SingleTypeParameter()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            string[] typeParamNames = new string[] { "T" };
            GenericTypeParameterBuilder[] parameters = method.DefineGenericParameters(typeParamNames);

            Assert.True(method.IsGenericMethod);
            Assert.True(method.IsGenericMethodDefinition);
        }

        [Fact]
        public void DefineGenericParameters_TwoTypeParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            string[] typeParamNames = new string[] { "T", "U" };
            GenericTypeParameterBuilder[] parameters = method.DefineGenericParameters(typeParamNames);

            Assert.True(method.IsGenericMethod);
            Assert.True(method.IsGenericMethodDefinition);
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

            Assert.Throws<InvalidOperationException>(() => method.DefineGenericParameters(new string[] { "T", "U" }));
        }

        [Fact]
        public void DefineGenericParameters_SingleTypeParameter_AlreadyDefined_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            string[] typeParamNames = new string[] { "T" };
            GenericTypeParameterBuilder[] parameters = method.DefineGenericParameters(typeParamNames);
            Assert.Throws<InvalidOperationException>(() => method.DefineGenericParameters(typeParamNames));
        }


        [Fact]
        public void DefineGenericParameters_TwoTypeParameters_AlreadyDefined_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static);

            method.DefineGenericParameters(new string[] { "T", "U" });
            Assert.Throws<InvalidOperationException>(() => method.DefineGenericParameters(new string[] { "M", "K" }));
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic, MethodAttributes.Public)]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public | MethodAttributes.Static)]
        public void DefineGenericParameters_NullNames_ThrowsArgumentNullException(TypeAttributes typeAttributes, MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", methodAttributes);
            Assert.Throws<ArgumentNullException>("names", () => method.DefineGenericParameters(null));
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic, MethodAttributes.Public)]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public | MethodAttributes.Static)]
        public void DefineGenericParameters_NamesContainsNull_ThrowsArgumentNullException(TypeAttributes typeAttributes, MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Test", methodAttributes);
            string[] typeParamNames = new string[] { "T", null, "U" };
            Assert.Throws<ArgumentNullException>("names", () => method.DefineGenericParameters(typeParamNames));
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic, MethodAttributes.Public)]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public | MethodAttributes.Static)]
        public void DefineGenericParameters_EmptyNames_ThrowsArgumentException(TypeAttributes typeAttributes, MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder builder = type.DefineMethod("TestMethod", methodAttributes);
            string[] typeParamNames = new string[0];
            Assert.Throws<ArgumentException>("names", () => builder.DefineGenericParameters(typeParamNames));
        }

        [Fact]
        public void DefineGenericParameters_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static);

            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            Type resultType = type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => method.DefineGenericParameters(new string[] { "T", "U" }));
        }
    }
}
