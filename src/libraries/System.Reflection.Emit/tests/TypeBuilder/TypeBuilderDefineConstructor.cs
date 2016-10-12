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
        [InlineData(MethodAttributes.Public, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.Family, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.Assembly, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.Private, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.PrivateScope, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.FamORAssem, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.FamANDAssem, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.Final | MethodAttributes.Public, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.Final | MethodAttributes.Family, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.SpecialName | MethodAttributes.Family, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.UnmanagedExport | MethodAttributes.Family, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.RTSpecialName | MethodAttributes.Family, CallingConventions.HasThis)]
        [InlineData(MethodAttributes.HideBySig | MethodAttributes.Family, CallingConventions.HasThis)]
        public void DefineConstructor(MethodAttributes methodAttributes, CallingConventions callingConvention)
        {
            DefineConstructor(methodAttributes, new Type[] { typeof(int), typeof(int) }, callingConvention);
        }

        [Theory]
        [InlineData(MethodAttributes.Static, new Type[0], CallingConventions.Standard)]
        public void DefineConstructor(MethodAttributes methodAttributes, Type[] parameterTypes, CallingConventions callingConvention)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            FieldBuilder fieldBuilderA = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
            FieldBuilder fieldBuilderB = type.DefineField("TestField", typeof(int), FieldAttributes.Private);

            ConstructorBuilder constructor = type.DefineConstructor(methodAttributes, callingConvention, parameterTypes);
            ILGenerator ctorIlGenerator = constructor.GetILGenerator();
            if (parameterTypes.Length != 0)
            {
                // Calling base class constructor
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
            Helpers.VerifyConstructor(constructor, type, methodAttributes, parameterTypes);
        }

        [Fact]
        public void DefineConstructor_StaticConstructorOnInterface()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, new Type[0]);
            constructor.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.Equal(1, createdType.GetConstructors(BindingFlags.Static | BindingFlags.NonPublic).Length);
        }

        [Fact]
        public void DefineConstructor_CalledTwice_Works()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);

            type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]).GetILGenerator().Emit(OpCodes.Ret);
            type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]).GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();
            ConstructorInfo[] constructors = createdType.GetConstructors();
            Assert.Equal(2, constructors.Length);
            Assert.Equal(constructors[0].GetParameters(), constructors[1].GetParameters());
        }

        [Fact]
        public void DefineConstructor_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]));
        }

        [Fact]
        public void DefineConstructor_InstanceOnInterface_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            Assert.Throws<InvalidOperationException>(() => type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]));
        }
        
        [Fact]
        public void DefineConstructor_ConstructorNotCreated_ThrowsInvalidOperationExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            Assert.Throws<InvalidOperationException>(() => type.CreateTypeInfo());
        }

        [Theory]
        [InlineData(CallingConventions.Any)]
        [InlineData(CallingConventions.VarArgs)]
        [InlineData(CallingConventions.HasThis)]
        [InlineData(CallingConventions.ExplicitThis | CallingConventions.HasThis)]
        public void DefineConstructor_HasThisCallingConventionsForStaticMethod_ThrowsTypeLoadExceptionOnCreation(CallingConventions conventions)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Static, conventions, new Type[0]);
            constructor.GetILGenerator().Emit(OpCodes.Ret);

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Fact]
        public void GetConstructor_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetConstructor(new Type[0]));
        }

        [Fact]
        public void GetConstructors_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetConstructors());
        }
    }
}
