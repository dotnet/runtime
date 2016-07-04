// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineConstructor
    {
        [Theory]
        [InlineData(MethodAttributes.Public, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.Public)]
        [InlineData(MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.Assembly, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.Private, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.PrivateScope, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.FamORAssem, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.FamANDAssem, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.Final | MethodAttributes.Public, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.Public)]
        [InlineData(MethodAttributes.Final | MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.SpecialName | MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.UnmanagedExport | MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.RTSpecialName | MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        [InlineData(MethodAttributes.HideBySig | MethodAttributes.Family, CallingConventions.HasThis, BindingFlags.Instance | BindingFlags.NonPublic)]
        public void DefineConstructor(MethodAttributes methodAttributes, CallingConventions callingConvention, BindingFlags bindingFlags)
        {
            DefineConstructor(methodAttributes, new Type[] { typeof(int), typeof(int) }, callingConvention, bindingFlags);
        }

        [Theory]
        [InlineData(MethodAttributes.Static, new Type[0], CallingConventions.Standard, BindingFlags.Static | BindingFlags.NonPublic)]
        public void DefineConstructor(MethodAttributes methodAttributes, Type[] parameterTypes, CallingConventions callingConvention, BindingFlags bindingFlags)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            FieldBuilder fieldBuilderA = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
            FieldBuilder fieldBuilderB = type.DefineField("TestField", typeof(int), FieldAttributes.Private);

            ConstructorBuilder ctorBuilder = type.DefineConstructor(methodAttributes, callingConvention, parameterTypes);
            Assert.Equal(type.Module, ctorBuilder.Module);
            Assert.Equal(type.AsType(), ctorBuilder.DeclaringType);
            Assert.Throws<NotSupportedException>(() => ctorBuilder.Invoke(null));
            Assert.Throws<NotSupportedException>(() => ctorBuilder.Invoke(null, null));

            ILGenerator ctorIlGenerator = ctorBuilder.GetILGenerator();

            if (parameterTypes.Length != 0)
            {
                //Calling base class constructor
                ctorIlGenerator.Emit(OpCodes.Ldarg_0);
                ctorIlGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));

                ctorIlGenerator.Emit(OpCodes.Ldarg_0);
                ctorIlGenerator.Emit(OpCodes.Ldarg_1);
                ctorIlGenerator.Emit(OpCodes.Stfld, fieldBuilderA);

                ctorIlGenerator.Emit(OpCodes.Ldarg_0);
                ctorIlGenerator.Emit(OpCodes.Ldarg_2);
                ctorIlGenerator.Emit(OpCodes.Stfld, fieldBuilderB);
            }

            ctorIlGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.NotNull(createdType.GetConstructors(bindingFlags).FirstOrDefault());
        }


        [Fact]
        public void DefineConstructor_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new Type[0]));
        }

        [Fact]
        public void DefineConstructor_Interface_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            Assert.Throws<InvalidOperationException>(() => type.DefineConstructor(MethodAttributes.Public,CallingConventions.HasThis, new Type[0]));
        }
    }
}
