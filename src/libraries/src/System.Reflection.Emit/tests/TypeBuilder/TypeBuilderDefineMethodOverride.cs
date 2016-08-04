// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public interface DefineMethodOverrideInterface
    {
        int M();
    }

    public class DefineMethodOverrideClass : DefineMethodOverrideInterface
    {
        public virtual int M() => 1;
    }

    public class MethodBuilderDefineMethodOverride
    {
        [Fact]
        public void DefineMethodOverride_InterfaceMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, 2);
            ilGenerator.Emit(OpCodes.Ret);
            
            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));
            MethodInfo decleration = typeof(DefineMethodOverrideInterface).GetMethod("M");
            type.DefineMethodOverride(method, decleration);

            Type createdType = type.CreateTypeInfo().AsType();

            MethodInfo createdMethod = typeof(DefineMethodOverrideInterface).GetMethod("M");
            Assert.Equal(2, createdMethod.Invoke(Activator.CreateInstance(createdType), null));
        }

        [Fact]
        public void DefineMethodOverride_InterfaceMethodWithConflictingName()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.SetParent(typeof(DefineMethodOverrideClass));
            MethodBuilder method = type.DefineMethod("M2", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, 2);
            ilGenerator.Emit(OpCodes.Ret);
            
            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));
            MethodInfo decleration = typeof(DefineMethodOverrideInterface).GetMethod("M");
            type.DefineMethodOverride(method, decleration);

            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createdMethod = typeof(DefineMethodOverrideInterface).GetMethod("M");
   
            ConstructorInfo createdTypeCtor = createdType.GetConstructor(new Type[0]);
            object instance = createdTypeCtor.Invoke(new object[0]);
            Assert.Equal(2, createdMethod.Invoke(instance, null));
            Assert.Equal(1, createdMethod.Invoke(Activator.CreateInstance(typeof(DefineMethodOverrideClass)), null));
        }

        [Fact]
        public void DefineMethodOverride_NullMethodInfoBody_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodInfo method = typeof(DefineMethodOverrideClass).GetMethod("M");
            Assert.Throws<ArgumentNullException>("methodInfoBody", () => type.DefineMethodOverride(null, method));
        }

        [Fact]
        public void DefineMethodOverride_NullMethodInfoDeclaration_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodInfo method = typeof(DefineMethodOverrideInterface).GetMethod("M");
            Assert.Throws<ArgumentNullException>("methodInfoDeclaration", () => type.DefineMethodOverride(method, null));
        }

        [Fact]
        public void DefineMethodOverride_MethodNotInClass_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodInfo body = typeof(DefineMethodOverrideInterface).GetMethod("M");
            MethodInfo decleration = typeof(DefineMethodOverrideClass).GetMethod("M");

            Assert.Throws<ArgumentException>(null, () => type.DefineMethodOverride(body, decleration));
        }

        [Fact]
        public void DefineMethodOverride_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));

            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo body = createdType.GetMethod("M");
            MethodInfo declaration = typeof(DefineMethodOverrideInterface).GetMethod("M");

            Assert.Throws<InvalidOperationException>(() => type.DefineMethodOverride(body, declaration));
        }
    }
}
