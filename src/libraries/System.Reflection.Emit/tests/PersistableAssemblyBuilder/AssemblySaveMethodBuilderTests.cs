// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveMethodBuilderTests
    {
        [Fact]
        public void DefineMethodOverride_InterfaceMethod()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                MethodBuilder method = type.DefineMethod("MImpl", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
                ILGenerator ilGenerator = method.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldc_I4, 2);
                ilGenerator.Emit(OpCodes.Ret);
                type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));
                MethodInfo declaration = typeof(DefineMethodOverrideInterface).GetMethod("M");
                type.DefineMethodOverride(method, declaration);
                type.CreateType();
                saveMethod.Invoke(ab, [file.Path]);

                InterfaceMapping im = type.GetInterfaceMap(typeof(DefineMethodOverrideInterface));
                Assert.Equal(type, im.TargetType);
                Assert.Equal(typeof(DefineMethodOverrideInterface), im.InterfaceType);
                Assert.Equal(1, im.InterfaceMethods.Length);
                Assert.Equal(declaration, im.InterfaceMethods[0]);
                Assert.Equal(method, im.TargetMethods[0]);

                Type typeFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path).GetType("MyType");
                MethodInfo methodFromDisk = typeFromDisk.GetMethod("MImpl");
                Assert.True(methodFromDisk.IsVirtual);
            }
        }

        [Fact]
        public void DefineMethodOverride_BaseTypeImplementation()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                type.SetParent(typeof(DefineMethodOverrideClass));
                MethodBuilder method = type.DefineMethod("M2", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
                ILGenerator ilGenerator = method.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldc_I4, 2);
                ilGenerator.Emit(OpCodes.Ret);
                MethodInfo declaration = typeof(DefineMethodOverrideClass).GetMethod("M");
                type.DefineMethodOverride(method, declaration);
                Type createdType = type.CreateType();
                saveMethod.Invoke(ab, [file.Path]);

                Type typeFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path).GetType("MyType");
                Assert.True(typeFromDisk.GetMethod("M2").IsVirtual);
            }
        }

        [Fact]
        public void DefineMethodOverride_GenericInterface_Succeeds()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                type.AddInterfaceImplementation(typeof(GenericInterface<string>));
                MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Virtual, typeof(string), Type.EmptyTypes);
                ILGenerator ilGenerator = method.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldstr, "Hello World");
                ilGenerator.Emit(OpCodes.Ret);
                type.DefineMethodOverride(method, typeof(GenericInterface<string>).GetMethod("Method"));
                Type createdType = type.CreateType();
                saveMethod.Invoke(ab, [file.Path]);

                Type typeFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path).GetType("MyType");
                MethodInfo methodFromDisk = typeFromDisk.GetMethod("Method");
                Assert.True(methodFromDisk.IsVirtual);

                InterfaceMapping im = type.GetInterfaceMap(typeof(GenericInterface<string>));
                Assert.Equal(type, im.TargetType);
                Assert.Equal(typeof(GenericInterface<string>), im.InterfaceType);
                Assert.Equal(1, im.InterfaceMethods.Length);
                Assert.Equal(typeof(GenericInterface<string>).GetMethod("Method"), im.InterfaceMethods[0]);
                Assert.Equal(method, im.TargetMethods[0]);
            }
        }

        [Fact]
        public void DefineMethodOverride_NullMethodInfoBody_ThrowsArgumentNullException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            MethodInfo method = typeof(DefineMethodOverrideClass).GetMethod("M");
            MethodInfo imethod = typeof(DefineMethodOverrideInterface).GetMethod("M");

            AssertExtensions.Throws<ArgumentNullException>("methodInfoDeclaration", () => type.DefineMethodOverride(method, null));
            AssertExtensions.Throws<ArgumentNullException>("methodInfoBody", () => type.DefineMethodOverride(null, imethod));
        }

        [Fact]
        public void DefineMethodOverride_MethodNotInClass_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            MethodInfo body = typeof(DefineMethodOverrideInterface).GetMethod("M");
            MethodInfo declaration = typeof(DefineMethodOverrideClass).GetMethod("M");

            AssertExtensions.Throws<ArgumentException>(null, () => type.DefineMethodOverride(body, declaration));
        }

        [Fact]
        public void DefineMethodOverride_TypeCreated_ThrowsInvalidOperationException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            method.GetILGenerator().Emit(OpCodes.Ret);
            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));

            Type createdType = type.CreateType();
            MethodInfo declaration = typeof(DefineMethodOverrideInterface).GetMethod(method.Name);

            Assert.Throws<InvalidOperationException>(() => type.DefineMethodOverride(method, declaration));
        }

        [Fact]
        public void DefineMethodOverride_MethodNotVirtual_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public, typeof(int), null);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, 2);
            ilGenerator.Emit(OpCodes.Ret);

            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));
            MethodInfo declaration = typeof(DefineMethodOverrideInterface).GetMethod(method.Name);

            Assert.Throws<ArgumentException>("methodInfoBody", () => type.DefineMethodOverride(method, declaration));
        }

        [Fact]
        public void DefineMethodOverride_TypeDoesNotImplementOrInheritMethod_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            method.GetILGenerator().Emit(OpCodes.Ret);
            MethodInfo interfaceMethod = typeof(DefineMethodOverrideInterface).GetMethod("M");
            MethodInfo baseTypeMethod = typeof(DefineMethodOverrideClass).GetMethod("M");

            Assert.Throws<ArgumentException>("methodInfoBody", () => type.DefineMethodOverride(method, interfaceMethod));
            Assert.Throws<ArgumentException>("methodInfoBody", () => type.DefineMethodOverride(method, baseTypeMethod));

            type.AddInterfaceImplementation(typeof(GenericInterface<string>));
            MethodInfo implementingMethod = typeof(GenericInterface<string>).GetMethod(nameof(GenericInterface<string>.Method));
            Assert.Throws<ArgumentException>("methodInfoBody", () => type.DefineMethodOverride(method, implementingMethod));
        }

        [Fact]
        public void DefineMethodOverride_CalledAgainWithSameDeclaration_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            MethodBuilder method1 = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilGenerator1 = method1.GetILGenerator();
            ilGenerator1.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator1.Emit(OpCodes.Ret);

            MethodBuilder method2 = type.DefineMethod("M2", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilGenerator2 = method2.GetILGenerator();
            ilGenerator2.Emit(OpCodes.Ldc_I4, 2);
            ilGenerator2.Emit(OpCodes.Ret);

            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));
            MethodInfo declaration = typeof(DefineMethodOverrideInterface).GetMethod("M");
            type.DefineMethodOverride(method1, declaration);

            Assert.Throws<ArgumentException>(() => type.DefineMethodOverride(method1, declaration));
            Assert.Throws<ArgumentException>(() => type.DefineMethodOverride(method2, declaration));
        }

        [Theory]
        [InlineData(typeof(int), new Type[0])]
        [InlineData(typeof(int), new Type[] { typeof(int), typeof(int) })]
        [InlineData(typeof(int), new Type[] { typeof(string), typeof(string) })]
        [InlineData(typeof(int), new Type[] { typeof(int), typeof(string), typeof(bool) })]
        [InlineData(typeof(string), new Type[] { typeof(string), typeof(int) })]
        public void DefineMethodOverride_BodyAndDeclarationHaveDifferentSignatures_ThrowsArgumentException(Type returnType, Type[] parameterTypes)
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, returnType, parameterTypes);
            method.GetILGenerator().Emit(OpCodes.Ret);
            type.AddInterfaceImplementation(typeof(InterfaceWithMethod));

            MethodInfo declaration = typeof(InterfaceWithMethod).GetMethod(nameof(InterfaceWithMethod.Method));

            Assert.Throws<ArgumentException>(() => type.DefineMethodOverride(method, declaration));
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
        public void DefineMethodOverride_StaticVirtualInterfaceMethodWorks()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            ModuleBuilder module = ab.GetDynamicModule("MyModule");

            TypeBuilder interfaceType = module.DefineType("InterfaceType", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract, parent: null);
            MethodBuilder svmInterface = interfaceType.DefineMethod("StaticVirtualMethod", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Static | MethodAttributes.Abstract, CallingConventions.Standard, typeof(void), Type.EmptyTypes);
            MethodBuilder vmInterface = interfaceType.DefineMethod("NormalInterfaceMethod", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Abstract, CallingConventions.HasThis, typeof(void), Type.EmptyTypes);
            Type interfaceTypeActual = interfaceType.CreateType();

            TypeBuilder implType = module.DefineType("ImplType", TypeAttributes.Public, parent: typeof(object), [interfaceTypeActual]);
            MethodBuilder svmImpl = implType.DefineMethod("StaticVirtualMethodImpl", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), Type.EmptyTypes);
            ILGenerator ilGenerator = svmImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            implType.DefineMethodOverride(svmImpl, svmInterface);

            MethodBuilder vmImpl = implType.DefineMethod("NormalVirtualMethodImpl", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(void), Type.EmptyTypes);
            ilGenerator = vmImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            implType.DefineMethodOverride(vmImpl, vmInterface);

            implType.CreateType();
        }

        public abstract class Impl : InterfaceWithMethod
        {
            public int Method(string s, int i) => 2;
        }

        [Fact]
        public void GetInterfaceMap_WithImplicitOverride_DefineMethodOverride()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            ModuleBuilder module = ab.GetDynamicModule("MyModule");

            TypeBuilder interfaceType = module.DefineType("InterfaceType", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract, parent: null);
            MethodBuilder svmInterface = interfaceType.DefineMethod("InterfaceMethod1", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Abstract, CallingConventions.Standard, typeof(int), Type.EmptyTypes);
            MethodBuilder mInterface = interfaceType.DefineMethod("InterfaceMethod2", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Abstract, typeof(string), Array.Empty<Type>());
            MethodBuilder vmInterface = interfaceType.DefineMethod("InterfaceMethod3", MethodAttributes.Assembly | MethodAttributes.Virtual | MethodAttributes.Abstract, CallingConventions.HasThis, typeof(void), [typeof(bool)]);
            Type interfaceTypeActual = interfaceType.CreateType();

            // Implicit implementations (same name, signatures)
            TypeBuilder implType = module.DefineType("ImplType", TypeAttributes.Public, parent: typeof(Impl), new Type[] { interfaceTypeActual });
            MethodBuilder mImpl = implType.DefineMethod("InterfaceMethod2", MethodAttributes.Public | MethodAttributes.Virtual, typeof(string), Array.Empty<Type>());
            ILGenerator ilGenerator = mImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldstr, "Hello");
            ilGenerator.Emit(OpCodes.Ret);
            MethodBuilder m2Impl = implType.DefineMethod("InterfaceMethod3", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), [typeof(bool)]);
            ilGenerator = m2Impl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Ret);

            // Explicit implementations with DefineMethodOverride, will override the implicit implementations if there is any
            MethodBuilder svmImpl = implType.DefineMethod("InterfaceMethod1Impl", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(int), Type.EmptyTypes);
            ilGenerator = svmImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            implType.DefineMethodOverride(svmImpl, svmInterface);
            MethodBuilder vmImpl = implType.DefineMethod("InterfaceMethod3Impl", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(void), [typeof(bool)]);
            ilGenerator = vmImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            implType.DefineMethodOverride(vmImpl, vmInterface);

            Type implTypeActual = implType.CreateType();

            InterfaceMapping actualMapping = implTypeActual.GetInterfaceMap(interfaceTypeActual);
            Assert.Equal(3, actualMapping.InterfaceMethods.Length);
            Assert.Equal(3, actualMapping.TargetMethods.Length);
            Assert.Contains(svmInterface, actualMapping.InterfaceMethods);
            Assert.Contains(mInterface, actualMapping.InterfaceMethods);
            Assert.Contains(vmInterface, actualMapping.InterfaceMethods);
            Assert.Contains(svmImpl, actualMapping.TargetMethods);
            Assert.Contains(mImpl, actualMapping.TargetMethods);
            Assert.Contains(vmImpl, actualMapping.TargetMethods);
            Assert.DoesNotContain(m2Impl, actualMapping.TargetMethods); // overwritten by vmImpl
            Assert.Equal(svmImpl, actualMapping.TargetMethods[0]);
            Assert.Equal(mImpl, actualMapping.TargetMethods[1]);
            Assert.Equal(vmImpl, actualMapping.TargetMethods[2]);
            actualMapping = implTypeActual.GetInterfaceMap(typeof(InterfaceWithMethod));
            Assert.Equal(1, actualMapping.InterfaceMethods.Length);
            Assert.Equal(1, actualMapping.TargetMethods.Length);
            Assert.Equal(typeof(Impl).GetMethod("Method"), actualMapping.TargetMethods[0]);
        }

        [Fact]
        public void GetInterfaceMap_Validations()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));

            Assert.Throws<NotSupportedException>(() => type.GetInterfaceMap(typeof(Impl))); // concreteTypeWithAbstractMethod not created
            type.DefineMethod("M", MethodAttributes.Public, typeof(int), null).GetILGenerator().Emit(OpCodes.Ret);
            type.CreateType();

            Assert.Throws<ArgumentNullException>(() => type.GetInterfaceMap(null));
            Assert.Throws<ArgumentException>(() => type.GetInterfaceMap(typeof(Impl))); // not interface
            Assert.Throws<ArgumentException>(() => type.GetInterfaceMap(typeof(InterfaceWithMethod))); // not implemented
        }

        public interface InterfaceDerivedFromOtherInterface : DefineMethodOverrideInterface
        {
            public string M2(int a);
        }

        public abstract class PartialImplementation : InterfaceDerivedFromOtherInterface
        {
            public int M() => 1;
            public abstract string M2(int a);
        }

        [Fact]
        public void CreateType_ValidateAllAbstractMethodsAreImplemented()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder typeNotImplementedIfaceMethod, out MethodInfo _);
            typeNotImplementedIfaceMethod.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));
            ModuleBuilder module = ab.GetDynamicModule("MyModule");
            TypeBuilder partiallyImplementedType = module.DefineType("Type2", TypeAttributes.Public);
            partiallyImplementedType.AddInterfaceImplementation(typeof(InterfaceDerivedFromOtherInterface));
            partiallyImplementedType.DefineMethod("M2", MethodAttributes.Public, typeof(string), [typeof(int)]).GetILGenerator().Emit(OpCodes.Ret);
            TypeBuilder baseTypeImplementedTheInterfaceMethod = module.DefineType("Type3", TypeAttributes.Public, parent: typeof(DefineMethodOverrideClass));
            baseTypeImplementedTheInterfaceMethod.AddInterfaceImplementation(typeof(InterfaceDerivedFromOtherInterface));
            baseTypeImplementedTheInterfaceMethod.DefineMethod("M2", MethodAttributes.Public, typeof(string), [typeof(int)]).GetILGenerator().Emit(OpCodes.Ret);
            TypeBuilder baseTypePartiallyImplemented = module.DefineType("Type4", TypeAttributes.Public, parent: typeof(PartialImplementation));
            baseTypePartiallyImplemented.AddInterfaceImplementation(typeof(InterfaceDerivedFromOtherInterface));

            Assert.Throws<TypeLoadException>(() => typeNotImplementedIfaceMethod.CreateType());
            Assert.Throws<TypeLoadException>(() => partiallyImplementedType.CreateType());
            baseTypeImplementedTheInterfaceMethod.CreateType(); // succeeds
            Assert.Throws<TypeLoadException>(() => baseTypePartiallyImplemented.CreateType());
        }

        [Fact]
        public void CreateType_ValidateMethods()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder concreteTypeWithAbstractMethod, out MethodInfo _);
            concreteTypeWithAbstractMethod.DefineMethod("AbstractMethod", MethodAttributes.Public | MethodAttributes.Abstract);
            Assert.Throws<InvalidOperationException>(() => concreteTypeWithAbstractMethod.CreateType()); // Type must be declared abstract if any of its methods are abstract.

            ModuleBuilder module = ab.GetDynamicModule("MyModule");
            TypeBuilder abstractType = module.DefineType("AbstractType", TypeAttributes.Public | TypeAttributes.Abstract);
            MethodBuilder abstractMethod = abstractType.DefineMethod("AbstractMethod", MethodAttributes.Public | MethodAttributes.Abstract);
            abstractType.DefineMethod("PinvokeMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.PinvokeImpl);
            Assert.Throws<InvalidOperationException>(() => abstractMethod.GetILGenerator()); 
            abstractType.CreateType(); // succeeds

            TypeBuilder concreteTypeWithNativeAndPinvokeMethod = module.DefineType("Type3", TypeAttributes.Public);
            concreteTypeWithNativeAndPinvokeMethod.DefineMethod("PinvokeMethod", MethodAttributes.Public | MethodAttributes.PinvokeImpl);
            MethodBuilder dllImportMethod = concreteTypeWithNativeAndPinvokeMethod.DefineMethod("DllImportMethod", MethodAttributes.Public);
            dllImportMethod.SetCustomAttribute(new CustomAttributeBuilder(typeof(DllImportAttribute).GetConstructor([typeof(string)]), ["kernel32.dll"]));
            MethodBuilder implFlagsSetMethod = concreteTypeWithNativeAndPinvokeMethod.DefineMethod("InternalCall", MethodAttributes.Public);
            implFlagsSetMethod.SetImplementationFlags(MethodImplAttributes.InternalCall);

            MethodBuilder methodNeedsIL = concreteTypeWithNativeAndPinvokeMethod.DefineMethod("MethodNeedsIL", MethodAttributes.Public);
            Assert.Throws<InvalidOperationException>(() => concreteTypeWithNativeAndPinvokeMethod.CreateType()); // Method 'MethodNeedsIL' does not have a method body.
            methodNeedsIL.GetILGenerator().Emit(OpCodes.Ret);
            concreteTypeWithNativeAndPinvokeMethod.CreateType(); // succeeds
        }

        [Fact]
        public void GetMethodsGetMethodImpl_Tests()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
            MethodBuilder voidPublicMethod = type.DefineMethod("VoidMethod", MethodAttributes.Public, typeof(void), [typeof(int)]);
            MethodBuilder voidAssemblyStaticMethod = type.DefineMethod("VoidMethod", MethodAttributes.Assembly | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            MethodBuilder voidFamilyOrAssemblyMethod = type.DefineMethod("VoidMethod", MethodAttributes.FamORAssem, typeof(void), Type.EmptyTypes);
            MethodBuilder voidFamilyMethod = type.DefineMethod("VoidMethod", MethodAttributes.Family, typeof(void), [typeof(int), typeof(string)]);
            MethodBuilder voidPublicMethodOverload = type.DefineMethod("VoidMethod", MethodAttributes.Public, typeof(void), [typeof(int), typeof(long)]);

            voidPublicMethod.GetILGenerator().Emit(OpCodes.Ret);
            voidAssemblyStaticMethod.GetILGenerator().Emit(OpCodes.Ret);
            voidFamilyMethod.GetILGenerator().Emit(OpCodes.Ret);
            voidFamilyOrAssemblyMethod.GetILGenerator().Emit(OpCodes.Ret);
            voidPublicMethodOverload.GetILGenerator().Emit(OpCodes.Ret);
            type.CreateType();

            Assert.Equal(8, type.GetMethods().Length);
            Assert.Equal(5, type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Length);
            Assert.Equal(4, type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length);
            Assert.Equal(2, type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Length);
            Assert.Equal(2, type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance).Length);
            Assert.Equal(0, type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static).Length);
            Assert.Equal(1, type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Static).Length);
            Assert.NotNull(type.GetMethod("VoidMethod", [typeof(int)]));
            Assert.NotNull(type.GetMethod("VoidMethod", [typeof(int), typeof(long)]));
            Assert.NotNull(type.GetMethod("VoidMethod", BindingFlags.NonPublic | BindingFlags.Static));
            Assert.NotNull(type.GetMethod("VoidMethod", BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes));
            Assert.NotNull(type.GetMethod("VoidMethod", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(int), typeof(string)]));
            Assert.Throws<AmbiguousMatchException>(() => type.GetMethod("VoidMethod"));
            Assert.Throws<AmbiguousMatchException>(() => type.GetMethod("VoidMethod", BindingFlags.NonPublic | BindingFlags.Instance));
        }
    }
}
