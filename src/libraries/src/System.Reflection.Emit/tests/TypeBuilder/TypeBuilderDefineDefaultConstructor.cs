// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineDefaultConstructor
    {
        [Theory]
        [InlineData(MethodAttributes.Public, BindingFlags.Public | BindingFlags.Instance)]
        [InlineData(MethodAttributes.Static, BindingFlags.Static | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.Assembly, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.Private, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.PrivateScope, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.FamORAssem, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.FamANDAssem, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.Final | MethodAttributes.Public, BindingFlags.Instance | BindingFlags.Public)]
        [InlineData(MethodAttributes.Final | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.SpecialName | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.UnmanagedExport | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.RTSpecialName | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.HideBySig | MethodAttributes.Family, BindingFlags.Instance | BindingFlags.NonPublic)]
        public void DefineDefaultConstructor(MethodAttributes attributes, BindingFlags bindingFlags)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineDefaultConstructor(attributes);

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.NotNull(createdType.GetConstructors(bindingFlags).FirstOrDefault());
        }

        [Fact]
        public void DefineDefaultConstructor_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => type.DefineDefaultConstructor(MethodAttributes.Public));
        }

        [Fact]
        public void DefineDefaultConstructor_Interface_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);

            Assert.Throws<InvalidOperationException>(() => type.DefineDefaultConstructor(MethodAttributes.Public));
        }

        [Fact]
        public void DefineDefaultConstructor_NoDefaultConstructor_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Class);

            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Family);

            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new Type[] { typeof(int) });

            ILGenerator constructorIlGenerator = constructor.GetILGenerator();

            constructorIlGenerator.Emit(OpCodes.Ldarg_0);
            constructorIlGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));

            constructorIlGenerator.Emit(OpCodes.Ldarg_0);
            constructorIlGenerator.Emit(OpCodes.Ldarg_1);
            constructorIlGenerator.Emit(OpCodes.Stfld, field);

            constructorIlGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();
            TypeBuilder nestedType = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Class);
            nestedType.SetParent(createdType);

            Assert.Throws<NotSupportedException>(() => nestedType.DefineDefaultConstructor(MethodAttributes.Public));
        }

        [Theory]
        [InlineData(MethodAttributes.Private)]
        [InlineData(MethodAttributes.PrivateScope)]
        public void DefineDefaultConstructor_PrivateDefaultConstructor_ThrowsNotSupportedException(MethodAttributes attributes)
        {
            TypeBuilder baseType = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Class);

            ConstructorBuilder constructor = baseType.DefineConstructor(attributes, CallingConventions.HasThis, new Type[] { typeof(int) });

            ILGenerator baseCtorIL = constructor.GetILGenerator();
            baseCtorIL.Emit(OpCodes.Ret);

            Type baseTestType = baseType.CreateTypeInfo().AsType();

            TypeBuilder nestedType = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Class);
            nestedType.SetParent(baseTestType);
            Assert.Throws<NotSupportedException>(() => nestedType.DefineDefaultConstructor(MethodAttributes.Public));
        }
    }
}
