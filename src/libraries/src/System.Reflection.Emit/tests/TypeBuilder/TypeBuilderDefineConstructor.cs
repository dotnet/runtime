// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineConstructor
    {
        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { MethodAttributes.Public, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.Family, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.Assembly, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.Private, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.PrivateScope, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.FamORAssem, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.FamANDAssem, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.Final | MethodAttributes.Public, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.Final | MethodAttributes.Family, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.SpecialName | MethodAttributes.Family, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.UnmanagedExport | MethodAttributes.Family, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.RTSpecialName | MethodAttributes.Family, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.HideBySig | MethodAttributes.Family, new Type[] { typeof(int), typeof(int) }, CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.Static, new Type[0], CallingConventions.Standard };

            // Ignores any CallingConventions, sets to CallingConventions.Standard
            yield return new object[] { MethodAttributes.Public, new Type[0], CallingConventions.Any };
            yield return new object[] { MethodAttributes.Public, new Type[0], CallingConventions.ExplicitThis };
            yield return new object[] { MethodAttributes.Public, new Type[0], CallingConventions.HasThis };
            yield return new object[] { MethodAttributes.Public, new Type[0], CallingConventions.Standard };
            yield return new object[] { MethodAttributes.Public, new Type[0], CallingConventions.VarArgs };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void DefineConstructor(MethodAttributes attributes, Type[] parameterTypes, CallingConventions callingConvention)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            FieldBuilder fieldBuilderA = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
            FieldBuilder fieldBuilderB = type.DefineField("TestField", typeof(int), FieldAttributes.Private);

            ConstructorBuilder constructor = type.DefineConstructor(attributes, callingConvention, parameterTypes);
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
            Helpers.VerifyConstructor(constructor, type, attributes, callingConvention, parameterTypes);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void DefineConstructor_NullRequiredAndOptionalCustomModifiers(MethodAttributes attributes, Type[] parameterTypes, CallingConventions callingConvention)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(attributes, callingConvention, parameterTypes);
            constructor.GetILGenerator().Emit(OpCodes.Ret);

            Helpers.VerifyConstructor(constructor, type, attributes, callingConvention, parameterTypes);
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
