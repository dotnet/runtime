// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderGetILGenerator
    {
        [Theory]
        [InlineData(MethodAttributes.Assembly)]
        [InlineData(MethodAttributes.CheckAccessOnOverride)]
        [InlineData(MethodAttributes.FamANDAssem)]
        [InlineData(MethodAttributes.Family)]
        [InlineData(MethodAttributes.FamORAssem)]
        [InlineData(MethodAttributes.Final)]
        [InlineData(MethodAttributes.HasSecurity)]
        [InlineData(MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.MemberAccessMask)]
        [InlineData(MethodAttributes.NewSlot)]
        [InlineData(MethodAttributes.Private)]
        [InlineData(MethodAttributes.Public)]
        [InlineData(MethodAttributes.RequireSecObject)]
        [InlineData(MethodAttributes.ReuseSlot)]
        [InlineData(MethodAttributes.RTSpecialName)]
        [InlineData(MethodAttributes.SpecialName)]
        [InlineData(MethodAttributes.Static)]
        [InlineData(MethodAttributes.UnmanagedExport)]
        [InlineData(MethodAttributes.Virtual)]
        public void GetILGenerator_ReturnsNonNull(MethodAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            ConstructorBuilder constructor = type.DefineConstructor(attributes, CallingConventions.Standard, new Type[0]);
            Assert.NotNull(constructor.GetILGenerator());
            Assert.NotNull(constructor.GetILGenerator(10));
        }

        [Fact]
        public void GetILGenerator_NoMethodBodyAttribute_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.PinvokeImpl, CallingConventions.Standard, new Type[0]);
            Assert.Throws<InvalidOperationException>(() => constructor.GetILGenerator());
            Assert.Throws<InvalidOperationException>(() => constructor.GetILGenerator(10));
        }

        [Fact]
        public void GetILGenerator_DefaultConstructor_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            ConstructorBuilder constructor = type.DefineDefaultConstructor(MethodAttributes.Public);
            Assert.Throws<InvalidOperationException>(() => constructor.GetILGenerator());
            Assert.Throws<InvalidOperationException>(() => constructor.GetILGenerator(10));
        }

        [Fact]
        public void HasDefaultValueShouldBeFalseWhenParameterDoNotDefineDefaultValue()
        {
            var builder = Helpers.DynamicModule();
            var type = builder.DefineType("MyProxy", TypeAttributes.Public);

            var constructorBuilder = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(Version) });
            var il = constructorBuilder.GetILGenerator();
            il.Emit(OpCodes.Ret);

            var typeInfo = type.CreateTypeInfo();
            var constructor = typeInfo.GetConstructor(new[] { typeof(Version) });
            var parameters = constructor.GetParameters();
            Assert.False(parameters[0].HasDefaultValue);
        }

        [Fact]
        public void HasDefaultValueShouldBeTrueWhenParameterDoDefineDefaultValue()
        {
            var builder = Helpers.DynamicModule();
            var type = builder.DefineType("MyProxy", TypeAttributes.Public);

            var constructorBuilder = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(Version) });
            ParameterBuilder parameter = constructorBuilder.DefineParameter(1, ParameterAttributes.Optional | ParameterAttributes.HasDefault, "param1");
            parameter.SetConstant(default(Version));
            var il = constructorBuilder.GetILGenerator();
            il.Emit(OpCodes.Ret);

            var typeInfo = type.CreateTypeInfo();
            var constructor = typeInfo.GetConstructor(new[] { typeof(Version) });
            var parameters = constructor.GetParameters();
            Assert.True(parameters[0].HasDefaultValue);
            Assert.Null(parameters[0].DefaultValue);
        }
    }
}
