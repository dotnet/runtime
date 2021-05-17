// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        public void DefineMethodOverride_GenericInterface_Succeeds()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.AddInterfaceImplementation(typeof(GenericInterface<string>));

            MethodBuilder method = type.DefineMethod(nameof(GenericInterface<string>.Method), MethodAttributes.Public | MethodAttributes.Virtual, typeof(string), new Type[0]);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldstr, "Hello World");
            ilGenerator.Emit(OpCodes.Ret);

            MethodInfo interfaceMethod = typeof(GenericInterface<string>).GetMethod(nameof(GenericInterface<string>.Method));
            type.DefineMethodOverride(method, interfaceMethod);

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.Equal("Hello World", interfaceMethod.Invoke(Activator.CreateInstance(createdType), null));
        }

        [Fact]
        public void DefineMethodOverride_CalledTwiceWithSameBodies_Works()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, 2);
            ilGenerator.Emit(OpCodes.Ret);

            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));
            MethodInfo decleration = typeof(DefineMethodOverrideInterface).GetMethod("M");
            type.DefineMethodOverride(method, decleration);
            type.DefineMethodOverride(method, decleration);

            Type createdType = type.CreateTypeInfo().AsType();

            MethodInfo createdMethod = typeof(DefineMethodOverrideInterface).GetMethod("M");
            Assert.Equal(2, createdMethod.Invoke(Activator.CreateInstance(createdType), null));
        }

        [Fact]
        public void DefineMethodOverride_NullMethodInfoBody_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodInfo method = typeof(DefineMethodOverrideClass).GetMethod("M");
            AssertExtensions.Throws<ArgumentNullException>("methodInfoBody", () => type.DefineMethodOverride(null, method));
        }

        [Fact]
        public void DefineMethodOverride_NullMethodInfoDeclaration_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodInfo method = typeof(DefineMethodOverrideInterface).GetMethod("M");
            AssertExtensions.Throws<ArgumentNullException>("methodInfoDeclaration", () => type.DefineMethodOverride(method, null));
        }

        [Fact]
        public void DefineMethodOverride_MethodNotInClass_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodInfo body = typeof(DefineMethodOverrideInterface).GetMethod("M");
            MethodInfo decleration = typeof(DefineMethodOverrideClass).GetMethod("M");

            AssertExtensions.Throws<ArgumentException>(null, () => type.DefineMethodOverride(body, decleration));
        }

        [Fact]
        public void DefineMethodOverride_GlobalMethod_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            MethodBuilder globalMethod = module.DefineGlobalMethod("GlobalMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[0]);
            globalMethod.GetILGenerator().Emit(OpCodes.Ret);

            TypeBuilder type = module.DefineType("Name");
            AssertExtensions.Throws<ArgumentException>(null, () => type.DefineMethodOverride(globalMethod, typeof(DefineMethodOverrideInterface).GetMethod(nameof(DefineMethodOverrideInterface.M))));
        }

        [Fact]
        public void DefineMethodOverride_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            method.GetILGenerator().Emit(OpCodes.Ret);
            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));

            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo body = createdType.GetMethod(method.Name);
            MethodInfo declaration = typeof(DefineMethodOverrideInterface).GetMethod(method.Name);

            Assert.Throws<InvalidOperationException>(() => type.DefineMethodOverride(body, declaration));
        }

        [Fact]
        public void DefineMethodOverride_MethodNotVirtual_ThrowsTypeLoadExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public, typeof(int), null);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, 2);
            ilGenerator.Emit(OpCodes.Ret);

            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));
            MethodInfo decleration = typeof(DefineMethodOverrideInterface).GetMethod(method.Name);
            type.DefineMethodOverride(method, decleration);

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineMethodOverride_NothingToOverride_ThrowsTypeLoadExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            method.GetILGenerator().Emit(OpCodes.Ret);

            type.DefineMethodOverride(method, typeof(DefineMethodOverrideInterface).GetMethod(nameof(DefineMethodOverrideInterface.M)));

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [InlineData(typeof(GenericInterface<>), nameof(GenericInterface<string>.Method))]
        [InlineData(typeof(DefineMethodOverrideInterface), nameof(DefineMethodOverrideInterface.M))]
        public void DefineMethodOverride_ClassDoesNotImplementOrInheritMethod_ThrowsTypeLoadExceptionOnCreation(Type methodType, string methodName)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.AddInterfaceImplementation(typeof(GenericInterface<string>));

            MethodBuilder method = type.DefineMethod("Name", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new Type[0]);
            method.GetILGenerator().Emit(OpCodes.Ret);

            type.DefineMethodOverride(method, methodType.GetMethod(methodName));

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineMethodOverride_BodyAndDeclarationTheSame_ThrowsTypeLoadExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            method.GetILGenerator().Emit(OpCodes.Ret);
            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));

            MethodInfo declaration = typeof(DefineMethodOverrideInterface).GetMethod(method.Name);
            type.DefineMethodOverride(method, method);

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineMethodOverride_CalledTwiceWithDifferentBodies_ThrowsTypeLoadExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method1 = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilGenerator1 = method1.GetILGenerator();
            ilGenerator1.Emit(OpCodes.Ldc_I4, 2);
            ilGenerator1.Emit(OpCodes.Ret);

            MethodBuilder method2 = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilGenerator2 = method2.GetILGenerator();
            ilGenerator2.Emit(OpCodes.Ldc_I4, 2);
            ilGenerator2.Emit(OpCodes.Ret);

            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));
            MethodInfo decleration = typeof(DefineMethodOverrideInterface).GetMethod("M");
            type.DefineMethodOverride(method1, decleration);
            type.DefineMethodOverride(method2, decleration);

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [InlineData(typeof(int), new Type[0])]
        [InlineData(typeof(int), new Type[] { typeof(int), typeof(int) })]
        [InlineData(typeof(int), new Type[] { typeof(string), typeof(string) })]
        [InlineData(typeof(int), new Type[] { typeof(int), typeof(string), typeof(bool) })]
        [InlineData(typeof(string), new Type[] { typeof(string), typeof(int) })]
        public void DefineMethodOverride_BodyAndDeclarationHaveDifferentSignatures_ThrowsTypeLoadExceptionOnCreation(Type returnType, Type[] parameterTypes)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            TypeBuilder type = module.DefineType("GenericType", TypeAttributes.Public);

            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, returnType, parameterTypes);
            method.GetILGenerator().Emit(OpCodes.Ret);
            type.AddInterfaceImplementation(typeof(InterfaceWithMethod));

            MethodInfo declaration = typeof(InterfaceWithMethod).GetMethod(nameof(InterfaceWithMethod.Method));
            type.DefineMethodOverride(method, declaration);

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        public interface GenericInterface<T>
        {
            T Method();
        }

        public interface InterfaceWithMethod
        {
            int Method(string s, int i);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49904", TestRuntimes.Mono)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Static virtual interface methods not supported on .NET Framework")]
        public void DefineMethodOverride_StaticVirtualInterfaceMethod()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");

            TypeBuilder interfaceType = module.DefineType("InterfaceType", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract, parent: null);
            MethodBuilder svmInterface = interfaceType.DefineMethod("StaticVirtualMethod", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Static | MethodAttributes.Abstract, CallingConventions.Standard, typeof(void), Array.Empty<Type>());
            MethodBuilder vmInterface = interfaceType.DefineMethod("NormalInterfaceMethod", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Abstract, CallingConventions.HasThis, typeof(void), Array.Empty<Type>());
            Type interfaceTypeActual = interfaceType.CreateType();

            TypeBuilder implType = module.DefineType("ImplType", TypeAttributes.Public, parent: typeof(object), new Type[]{interfaceTypeActual});
            MethodBuilder svmImpl = implType.DefineMethod("StaticVirtualMethodImpl", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), Array.Empty<Type>());
            ILGenerator ilGenerator = svmImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            implType.DefineMethodOverride(svmImpl, svmInterface);

            MethodBuilder vmImpl = implType.DefineMethod("NormalVirtualMethodImpl", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(void), Array.Empty<Type>());
            ilGenerator = vmImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            implType.DefineMethodOverride(vmImpl, vmInterface);

            Type implTypeActual = implType.CreateType();
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49904", TestRuntimes.Mono)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Static virtual interface methods not supported on .NET Framework")]
        public void DefineMethodOverride_StaticVirtualInterfaceMethod_VerifyWithInterfaceMap()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");

            TypeBuilder interfaceType = module.DefineType("InterfaceType", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract, parent: null);
            MethodBuilder svmInterface = interfaceType.DefineMethod("StaticVirtualMethod", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Static | MethodAttributes.Abstract, CallingConventions.Standard, typeof(void), Array.Empty<Type>());
            MethodBuilder vmInterface = interfaceType.DefineMethod("NormalInterfaceMethod", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Abstract, CallingConventions.HasThis, typeof(void), Array.Empty<Type>());
            Type interfaceTypeActual = interfaceType.CreateType();
            MethodInfo svmInterfaceActual = interfaceTypeActual.GetMethod("StaticVirtualMethod");
            Assert.NotNull(svmInterfaceActual);
            MethodInfo vmInterfaceActual = interfaceTypeActual.GetMethod("NormalInterfaceMethod");
            Assert.NotNull(vmInterfaceActual);

            TypeBuilder implType = module.DefineType("ImplType", TypeAttributes.Public, parent: typeof(object), new Type[]{interfaceTypeActual});
            MethodBuilder svmImpl = implType.DefineMethod("StaticVirtualMethodImpl", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), Array.Empty<Type>());
            ILGenerator ilGenerator = svmImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            implType.DefineMethodOverride(svmImpl, svmInterface);

            MethodBuilder vmImpl = implType.DefineMethod("NormalVirtualMethodImpl", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(void), Array.Empty<Type>());
            ilGenerator = vmImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            implType.DefineMethodOverride(vmImpl, vmInterface);

            Type implTypeActual = implType.CreateType();
            MethodInfo svmImplActual = implTypeActual.GetMethod("StaticVirtualMethodImpl");
            MethodInfo vmImplActual = implTypeActual.GetMethod("NormalVirtualMethodImpl");
            Assert.NotNull(svmImplActual);
            Assert.NotNull(vmImplActual);

            InterfaceMapping actualMapping = implTypeActual.GetInterfaceMap(interfaceTypeActual);
            Assert.Equal(2, actualMapping.InterfaceMethods.Length);
            Assert.Equal(2, actualMapping.TargetMethods.Length);

            Assert.Contains(svmInterfaceActual, actualMapping.InterfaceMethods);
            Assert.True(svmInterfaceActual.IsVirtual);
            Assert.Contains(vmInterfaceActual, actualMapping.InterfaceMethods);

            Assert.Contains(svmImplActual, actualMapping.TargetMethods);
            Assert.False(svmImplActual.IsVirtual);
            Assert.Contains(vmImplActual, actualMapping.TargetMethods);

            Assert.Equal(Array.IndexOf(actualMapping.InterfaceMethods, svmInterfaceActual), Array.IndexOf(actualMapping.TargetMethods, svmImplActual));
            Assert.Equal(Array.IndexOf(actualMapping.InterfaceMethods, vmInterfaceActual), Array.IndexOf(actualMapping.TargetMethods, vmImplActual));
        }
    }
}
