// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveTypeBuilderAPIsTests
    {
        [Fact]
        public void DefineMethodOverride_InterfaceMethod()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder method = type.DefineMethod("MImpl", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
                ILGenerator ilGenerator = method.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldc_I4, 2);
                ilGenerator.Emit(OpCodes.Ret);
                type.AddInterfaceImplementation(typeof(DefineMethodOverrideInterface));
                MethodInfo declaration = typeof(DefineMethodOverrideInterface).GetMethod("M");
                type.DefineMethodOverride(method, declaration);
                type.CreateType();
                ab.Save(file.Path);

                InterfaceMapping im = type.GetInterfaceMap(typeof(DefineMethodOverrideInterface));
                Assert.Equal(type, im.TargetType);
                Assert.Equal(typeof(DefineMethodOverrideInterface), im.InterfaceType);
                Assert.Equal(1, im.InterfaceMethods.Length);
                Assert.Equal(declaration, im.InterfaceMethods[0]);
                Assert.Equal(method, im.TargetMethods[0]);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type typeFromDisk = mlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
                    MethodInfo methodFromDisk = typeFromDisk.GetMethod("MImpl");
                    Assert.True(methodFromDisk.IsVirtual);
                }
            }
        }

        [Fact]
        public void DefineMethodOverride_BaseTypeImplementation()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                type.SetParent(typeof(DefineMethodOverrideClass));
                MethodBuilder method = type.DefineMethod("M2", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
                ILGenerator ilGenerator = method.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldc_I4, 2);
                ilGenerator.Emit(OpCodes.Ret);
                MethodInfo declaration = typeof(DefineMethodOverrideClass).GetMethod("M");
                type.DefineMethodOverride(method, declaration);
                Type createdType = type.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type typeFromDisk = mlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
                    Assert.True(typeFromDisk.GetMethod("M2").IsVirtual);
                }
            }
        }

        [Fact]
        public void DefineMethodOverride_GenericInterface_Succeeds()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                type.AddInterfaceImplementation(typeof(GenericInterface<string>));
                MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Virtual, typeof(string), Type.EmptyTypes);
                ILGenerator ilGenerator = method.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldstr, "Hello World");
                ilGenerator.Emit(OpCodes.Ret);
                type.DefineMethodOverride(method, typeof(GenericInterface<string>).GetMethod("Method"));
                Type createdType = type.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type typeFromDisk = mlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
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
        }

        [Fact]
        public void DefineMethodOverride_NullMethodInfoBody_ThrowsArgumentNullException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            MethodInfo method = typeof(DefineMethodOverrideClass).GetMethod("M");
            MethodInfo imethod = typeof(DefineMethodOverrideInterface).GetMethod("M");

            AssertExtensions.Throws<ArgumentNullException>("methodInfoDeclaration", () => type.DefineMethodOverride(method, null));
            AssertExtensions.Throws<ArgumentNullException>("methodInfoBody", () => type.DefineMethodOverride(null, imethod));
        }

        [Fact]
        public void DefineMethodOverride_MethodNotInClass_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            MethodInfo body = typeof(DefineMethodOverrideInterface).GetMethod("M");
            MethodInfo declaration = typeof(DefineMethodOverrideClass).GetMethod("M");

            AssertExtensions.Throws<ArgumentException>(null, () => type.DefineMethodOverride(body, declaration));
        }

        [Fact]
        public void DefineMethodOverride_TypeCreated_ThrowsInvalidOperationException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
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
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
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
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
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
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
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
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
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
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
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
        public void DefineMethodOverride_InterfaceImplementationWithByRefArrayTypes()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            ModuleBuilder module = ab.GetDynamicModule("MyModule");

            TypeBuilder interfaceType = module.DefineType("InterfaceType", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            Type ptrType = type.MakePointerType();
            Type byrefType = type.MakeByRefType();
            Type arrayType = type.MakeArrayType(2);
            MethodBuilder methPointerArg = interfaceType.DefineMethod("M1", MethodAttributes.Public | MethodAttributes.Abstract, typeof(void), [ptrType]);
            MethodBuilder methByRefArg = interfaceType.DefineMethod("M1", MethodAttributes.Public | MethodAttributes.Abstract, typeof(int), [byrefType, typeof(string)]);
            MethodBuilder methArrArg = interfaceType.DefineMethod("M1", MethodAttributes.Public | MethodAttributes.Abstract, typeof(void), [arrayType]);
            interfaceType.CreateType();

            TypeBuilder implType = module.DefineType("ImplType", TypeAttributes.Public, parent: typeof(object), [interfaceType]);
            MethodBuilder pointerArgImpl = implType.DefineMethod("InterfaceType.M1", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), [ptrType]);
            MethodBuilder byrefArgImpl = implType.DefineMethod("InterfaceType.M1", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), [byrefType, typeof(string)]);
            MethodBuilder arrayArgImpl = implType.DefineMethod("InterfaceType.M1", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), [arrayType]);
            pointerArgImpl.GetILGenerator().Emit(OpCodes.Ret);
            arrayArgImpl.GetILGenerator().Emit(OpCodes.Ret);
            byrefArgImpl.GetILGenerator().Emit(OpCodes.Ret);

            implType.DefineMethodOverride(pointerArgImpl, methPointerArg);
            implType.DefineMethodOverride(byrefArgImpl, methByRefArg);
            implType.DefineMethodOverride(arrayArgImpl, interfaceType.GetMethod("M1", [arrayType]));

            implType.CreateType(); // succeeds
        }

        [Fact]
        public void TypeBuilderImplementsGenericInterfaceWithTypeBuilderGenericConstraint()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            ModuleBuilder module = ab.GetDynamicModule("MyModule");
            TypeBuilder ifaceType = module.DefineType("InterfaceType", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            TypeBuilder implType = module.DefineType("ImplType", TypeAttributes.Public);

            GenericTypeParameterBuilder[] gParams =  implType.DefineGenericParameters("T");
            gParams[0].SetInterfaceConstraints(ifaceType);
            Type constructedGenericInterface = typeof(IComparable<>).MakeGenericType(gParams);
            implType.AddInterfaceImplementation(constructedGenericInterface);

            MethodBuilder compareToImpl = implType.DefineMethod("CompareTo", MethodAttributes.Public, typeof(int), [gParams[0]]);

            ILGenerator ilGenerator = compareToImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Ret);

            type.CreateType();
            implType.CreateType(); // succeeds
        }

        [Fact]
        public void TypeBuilderImplementsGenericInterfaceWithTypeBuilderArgument()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            ModuleBuilder module = ab.GetDynamicModule("MyModule");
            Type constructedGenericInterface = typeof(IComparable<>).MakeGenericType(type);

            TypeBuilder implType = module.DefineType("ImplType", TypeAttributes.Public, parent: typeof(object), [constructedGenericInterface]);
            MethodBuilder compareToImpl = implType.DefineMethod("CompareTo", MethodAttributes.Public, typeof(int), [type]);

            ILGenerator ilGenerator = compareToImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Ret);

            type.CreateType();
            implType.CreateType(); // succeeds
        }

        [Fact]
        public void TypeBuilderImplementsGenericInterface()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            ModuleBuilder module = ab.GetDynamicModule("MyModule");
            TypeBuilder implType = module.DefineType("ImplType", TypeAttributes.Public);

            GenericTypeParameterBuilder[] gParams = implType.DefineGenericParameters("T");
            Type constructedGenericInterface = typeof(IComparable<>).MakeGenericType(gParams);
            implType.AddInterfaceImplementation(constructedGenericInterface);

            MethodBuilder compareToImpl = implType.DefineMethod("CompareTo", MethodAttributes.Public, typeof(int), [gParams[0]]);

            ILGenerator ilGenerator = compareToImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Ret);

            type.CreateType();
            implType.CreateType(); // succeeds
        }

        [Fact]
        public void TypeBuilderImplementsConstructedGenericInterface()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            ModuleBuilder module = ab.GetDynamicModule("MyModule");

            TypeBuilder implType = module.DefineType("ImplType", TypeAttributes.Public, parent: typeof(object), [typeof(IComparable<string>)]);
            MethodBuilder compareToImpl = implType.DefineMethod("CompareTo", MethodAttributes.Public, typeof(int), [typeof(string)]);

            ILGenerator ilGenerator = compareToImpl.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Ret);

            type.CreateType();
            implType.CreateType(); // succeeds
        }

        [Fact]
        public void GetInterfaceMap_WithImplicitOverride_DefineMethodOverride()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
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
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
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

        public interface IDefaultImplementation
        {
            void Method() => Console.WriteLine("Hello");
        }

        public interface IStaticAbstract
        {
            static abstract void Method();
        }

        [Fact]
        public void CreateType_ValidateMethods()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder concreteTypeWithAbstractMethod);
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
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
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

        [Fact]
        public void ReturnTypeAndParameterRequiredOptionalCustomModifiers()
        {
            using (TempFile file = TempFile.Create())
            {
                Type[] cmodsReq1 = [typeof(object), typeof(string)];
                Type[] cmodsReq2 = [typeof(uint)];
                Type[] cmodsOpt1 = [typeof(int)];
                Type[] cmodsOpt2 = [typeof(long), typeof(byte), typeof(bool)];
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                MethodBuilder methodAll = type.DefineMethod("AllModifiers", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                    typeof(string), [typeof(int), typeof(short)], [typeof(Version)], [typeof(int), typeof(long)], [cmodsReq1, cmodsReq2], [cmodsOpt1, cmodsOpt2]);
                ILGenerator ilGenerator = methodAll.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldstr, "Hello World");
                ilGenerator.Emit(OpCodes.Ret);
                Type createdType = type.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type typeFromDisk = mlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
                    MethodInfo allModMethod = typeFromDisk.GetMethod("AllModifiers");
                    Type[] returnReqMods = allModMethod.ReturnParameter.GetRequiredCustomModifiers();
                    Type[] returnOptMods = allModMethod.ReturnParameter.GetOptionalCustomModifiers();
                    Type[] par0RequiredMods = allModMethod.GetParameters()[0].GetRequiredCustomModifiers();
                    Type[] par0OptionalMods = allModMethod.GetParameters()[0].GetOptionalCustomModifiers();
                    Assert.Equal(2, returnReqMods.Length);
                    Assert.Equal(mlc.CoreAssembly.GetType(typeof(short).FullName), returnReqMods[0]);
                    Assert.Equal(mlc.CoreAssembly.GetType(typeof(int).FullName), returnReqMods[1]);
                    Assert.Equal(1, returnOptMods.Length);
                    Assert.Equal(mlc.CoreAssembly.GetType(typeof(Version).FullName), returnOptMods[0]);
                    Assert.Equal(cmodsReq1.Length, par0RequiredMods.Length);
                    Assert.Equal(mlc.CoreAssembly.GetType(cmodsReq1[1].FullName), par0RequiredMods[0]);
                    Assert.Equal(mlc.CoreAssembly.GetType(cmodsReq1[0].FullName), par0RequiredMods[1]);
                    Assert.Equal(cmodsOpt1.Length, par0OptionalMods.Length);
                    Assert.Equal(mlc.CoreAssembly.GetType(cmodsOpt1[0].FullName), par0OptionalMods[0]);
                    Assert.Equal(cmodsReq2.Length, allModMethod.GetParameters()[1].GetRequiredCustomModifiers().Length);
                    Assert.Equal(cmodsOpt2.Length, allModMethod.GetParameters()[1].GetOptionalCustomModifiers().Length);
                }
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public static void DefinePInvokeMethodExecution_Windows()
        {
            const string EnvironmentVariable = "COMPUTERNAME";

            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("DefinePInvokeMethodExecution_Windows"));
                TypeBuilder tb = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
                MethodBuilder mb = tb.DefinePInvokeMethod(
                    "GetEnvironmentVariableW",
                    "kernel32.dll",
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                    CallingConventions.Standard,
                    typeof(int),
                    [typeof(string), typeof(StringBuilder), typeof(int)],
                    CallingConvention.StdCall,
                    CharSet.Unicode);
                mb.SetImplementationFlags(mb.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

                Type t = tb.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Assembly assemblyFromDisk = tlc.LoadFromAssemblyPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.GetType("MyType");
                MethodInfo methodFromDisk = typeFromDisk.GetMethod("GetEnvironmentVariableW", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(methodFromDisk);

                string expected = Environment.GetEnvironmentVariable(EnvironmentVariable);

                int numCharsRequired = (int)methodFromDisk.Invoke(null, [EnvironmentVariable, null, 0]);
                if (numCharsRequired == 0)
                {
                    // Environment variable is not defined. Make sure we got that result using both techniques.
                    Assert.Null(expected);
                }
                else
                {
                    StringBuilder sb = new StringBuilder(numCharsRequired);
                    int numCharsWritten = (int)methodFromDisk.Invoke(null, [EnvironmentVariable, sb, numCharsRequired]);
                    Assert.NotEqual(0, numCharsWritten);
                    string actual = sb.ToString();
                    Assert.Equal(expected, actual);
                }
                tlc.Unload();
            }
        }

        public static IEnumerable<object[]> TestData
        {
            get
            {
                yield return [new DpmParams() { MethodName = "A1", LibName = "Foo1.dll", EntrypointName = "A1",
                    ReturnType = typeof(int), ParameterTypes = [typeof(string)] }];
                yield return [new DpmParams() { MethodName = "A2", LibName = "Foo2.dll", EntrypointName = "Wha2",
                    ReturnType = typeof(int), ParameterTypes = [typeof(int)],
                    NativeCallConv = CallingConvention.Cdecl}];
                yield return [new DpmParams() { MethodName = "A3", LibName = "Foo3.dll", EntrypointName = "Wha3",
                    ReturnType = typeof(double), ParameterTypes = [typeof(string)],
                    Charset = CharSet.Ansi, ReturnTypeOptMods = [typeof(short)]}];
                yield return [new DpmParams() { MethodName = "A4", LibName = "Foo4.dll", EntrypointName = "Wha4",
                    ReturnType = typeof(IntPtr), ParameterTypes = [typeof(string)],
                    Charset = CharSet.Auto, ReturnTypeReqMods = [typeof(bool)], NativeCallConv = CallingConvention.FastCall}];
                yield return [new DpmParams() { MethodName = "C1", LibName = "Foo5.dll", EntrypointName = "Wha5",
                    ReturnType = typeof(int), ParameterTypes = [typeof(string)], ReturnTypeReqMods = [typeof(int)],
                    ReturnTypeOptMods = [typeof(short)], ParameterTypeOptMods = [[typeof(double)]], ParameterTypeReqMods = [[typeof(float)]]}];
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void TestDefinePInvokeMethod(DpmParams p)
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                MethodBuilder mb = tb.DefinePInvokeMethod(p.MethodName, p.LibName, p.EntrypointName, p.Attributes, p.ManagedCallConv, p.ReturnType,
                    p.ReturnTypeReqMods, p.ReturnTypeOptMods, p.ParameterTypes, p.ParameterTypeReqMods, p.ParameterTypeOptMods, p.NativeCallConv, p.Charset);
                mb.SetImplementationFlags(mb.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);
                Type t = tb.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type typeFromDisk = mlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
                    MethodInfo m = typeFromDisk.GetMethod(p.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    Assert.NotNull(m);
                    VerifyPInvokeMethod(t, m, p, mlc.CoreAssembly);
                }
            }
        }

        internal static void VerifyPInvokeMethod(Type type, MethodInfo method, DpmParams p, Assembly coreAssembly)
        {
            Assert.Equal(type.FullName, method.DeclaringType.FullName);
            Assert.Equal(p.MethodName, method.Name);
            Assert.Equal(p.Attributes, method.Attributes);
            Assert.Equal(p.ManagedCallConv, method.CallingConvention);
            Assert.Equal(coreAssembly.GetType(p.ReturnType.FullName), method.ReturnType);

            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(coreAssembly.GetType(p.ParameterTypes[0].FullName), parameters[0].ParameterType);

            CustomAttributeData dllAttrData = method.GetCustomAttributesData()[0];
            if (dllAttrData.AttributeType.FullName == typeof(PreserveSigAttribute).FullName)
            {
                dllAttrData = method.GetCustomAttributesData()[1];
            }

            Assert.Equal(coreAssembly.GetType(typeof(DllImportAttribute).FullName), dllAttrData.AttributeType);
            Assert.Equal(p.LibName, dllAttrData.ConstructorArguments[0].Value);
            foreach (CustomAttributeNamedArgument namedArg in dllAttrData.NamedArguments)
            {
                if (namedArg.MemberName == "EntryPoint")
                {
                    Assert.Equal(p.EntrypointName, namedArg.TypedValue.Value);
                }
                else if (namedArg.MemberName == "CharSet")
                {
                    Assert.Equal(p.Charset, (CharSet)namedArg.TypedValue.Value);
                }
                else if (namedArg.MemberName == "SetLastError")
                {
                    Assert.Equal(false, namedArg.TypedValue.Value);
                }
                else if (namedArg.MemberName == "ExactSpelling")
                {
                    Assert.Equal(false, namedArg.TypedValue.Value);
                }
                else if (namedArg.MemberName == "BestFitMapping")
                {
                    Assert.Equal(false, namedArg.TypedValue.Value);
                }
                else if (namedArg.MemberName == "ThrowOnUnmappableChar")
                {
                    Assert.Equal(false, namedArg.TypedValue.Value);
                }
                else if (namedArg.MemberName == "PreserveSig")
                {
                    Assert.Equal(true, namedArg.TypedValue.Value);
                }
                else if (namedArg.MemberName == "CallingConvention")
                {
                    Assert.Equal(p.NativeCallConv, (CallingConvention)namedArg.TypedValue.Value);
                }
            }

            IList<Type> returnTypeOptMods = method.ReturnParameter.GetOptionalCustomModifiers();
            if (p.ReturnTypeOptMods == null)
            {
                Assert.Equal(0, returnTypeOptMods.Count);
            }
            else
            {
                Assert.Equal(coreAssembly.GetType(p.ReturnTypeOptMods[0].FullName), returnTypeOptMods[0]);
            }

            IList<Type> returnTypeReqMods = method.ReturnParameter.GetRequiredCustomModifiers();
            if (p.ReturnTypeReqMods == null)
            {
                Assert.Equal(0, returnTypeReqMods.Count);
            }
            else
            {
                Assert.Equal(coreAssembly.GetType(p.ReturnTypeReqMods[0].FullName), returnTypeReqMods[0]);
            }

            if (p.ParameterTypeOptMods == null)
            {
                foreach (ParameterInfo pi in method.GetParameters())
                {
                    Assert.Equal(0, pi.GetOptionalCustomModifiers().Length);
                }
            }
            else
            {
                Assert.Equal(parameters.Length, p.ParameterTypeOptMods.Length);
                for (int i = 0; i < p.ParameterTypeOptMods.Length; i++)
                {
                    Type[] mods = parameters[i].GetOptionalCustomModifiers();
                    Assert.Equal(coreAssembly.GetType(p.ParameterTypeOptMods[i][0].FullName), mods[0]);
                }
            }

            if (p.ParameterTypeReqMods == null)
            {
                foreach (ParameterInfo pi in method.GetParameters())
                {
                    Assert.Equal(0, pi.GetRequiredCustomModifiers().Length);
                }
            }
            else
            {
                Assert.Equal(parameters.Length, p.ParameterTypeReqMods.Length);
                for (int i = 0; i < p.ParameterTypeReqMods.Length; i++)
                {
                    Type[] mods = parameters[i].GetRequiredCustomModifiers();
                    Assert.Equal(coreAssembly.GetType(p.ParameterTypeReqMods[i][0].FullName), mods[0]);
                }
            }
        }

        [Fact]
        public void DefineTypeInitializer()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                FieldBuilder greetingField = tb.DefineField("Greeting", typeof(string), FieldAttributes.Private | FieldAttributes.Static);
                ConstructorBuilder constructor = tb.DefineTypeInitializer();
                ILGenerator constructorIlGenerator = constructor.GetILGenerator();
                constructorIlGenerator.Emit(OpCodes.Ldstr, "hello");
                constructorIlGenerator.Emit(OpCodes.Stsfld, greetingField);
                constructorIlGenerator.Emit(OpCodes.Ret);

                tb.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Type typeFromDisk = tlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
                FieldInfo createdField = typeFromDisk.GetField("Greeting", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.Equal("hello", createdField.GetValue(null));
                tlc.Unload();
            }
        }

        [Fact]
        public static void DefineUninitializedDataTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder tb);
                FieldBuilder myFieldBuilder = tb.DefineUninitializedData("MyGreeting", 4, FieldAttributes.Public);
                var loadAddressMethod = tb.DefineMethod("LoadAddress", MethodAttributes.Public | MethodAttributes.Static, typeof(IntPtr), null);
                var methodIL = loadAddressMethod.GetILGenerator();
                methodIL.Emit(OpCodes.Ldsflda, myFieldBuilder);
                methodIL.Emit(OpCodes.Ret);

                Type t = tb.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Assembly assemblyFromDisk = tlc.LoadFromAssemblyPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.GetType("MyType");
                byte[] initBytes = [4, 3, 2, 1];
                nint myIntPtr = Marshal.AllocHGlobal(4);
                nint intptrTemp = Marshal.AllocHGlobal(4);
                for (int j = 0; j < 4; j++)
                {
                    Marshal.WriteByte(myIntPtr + j, initBytes[j]);
                }
                object myObj = Marshal.PtrToStructure(myIntPtr, typeFromDisk.GetField("MyGreeting").FieldType);
                Marshal.StructureToPtr(myObj, intptrTemp, false);
                for (int j = 0; j < 4; j++)
                {
                    Assert.Equal(initBytes[j], Marshal.ReadByte(intptrTemp, j));
                }
                Marshal.FreeHGlobal(myIntPtr);
                Marshal.FreeHGlobal(intptrTemp);
                tlc.Unload();
            }
        }

        public static List<object[]> FieldTestData = new List<object[]>()
        {
            new object[] { "TestName1", typeof(object), FieldAttributes.Public, FieldAttributes.Public },
            new object[] { "A!?123C", typeof(int), FieldAttributes.Assembly, FieldAttributes.Assembly },
            new object[] { "a\0b\0c", typeof(string), FieldAttributes.FamANDAssem | FieldAttributes.Static, FieldAttributes.FamANDAssem | FieldAttributes.Static },
            new object[] { "\uD800\uDC00", Helpers.DynamicType(TypeAttributes.Public).AsType(), FieldAttributes.Family, FieldAttributes.Family },
            new object[] { "\u043F\u0440\u0438\u0432\u0435\u0442", typeof(EmptyNonGenericInterface1), FieldAttributes.FamORAssem, FieldAttributes.FamORAssem },
            new object[] { "Test Name With Spaces", typeof(EmptyEnum), FieldAttributes.Public, FieldAttributes.Public },
            new object[] { "TestName2", typeof(EmptyNonGenericClass), FieldAttributes.HasDefault, FieldAttributes.PrivateScope },
            new object[] { "TestName3", typeof(EmptyNonGenericStruct), FieldAttributes.HasFieldMarshal, FieldAttributes.PrivateScope },
            new object[] { "TestName4", typeof(EmptyGenericClass<int>), FieldAttributes.HasFieldRVA, FieldAttributes.PrivateScope },
            new object[] { "TestName5", typeof(EmptyGenericStruct<int>), FieldAttributes.Literal | FieldAttributes.Static, FieldAttributes.Literal | FieldAttributes.Static },
            new object[] { "testname5", typeof(int), FieldAttributes.NotSerialized, FieldAttributes.NotSerialized },
            new object[] { "TestName7", typeof(int[]), FieldAttributes.PinvokeImpl, FieldAttributes.PinvokeImpl },
            new object[] { "TestName8", typeof(int).MakePointerType(), FieldAttributes.Private, FieldAttributes.Private },
            new object[] { "TestName9", typeof(EmptyGenericClass<>), FieldAttributes.PrivateScope, FieldAttributes.PrivateScope },
            new object[] { "TestName10", typeof(int), FieldAttributes.Public, FieldAttributes.Public },
            new object[] { "TestName11", typeof(int), FieldAttributes.RTSpecialName, FieldAttributes.PrivateScope },
            new object[] { "TestName1", typeof(int), FieldAttributes.SpecialName, FieldAttributes.SpecialName },
            new object[] { "TestName1", typeof(int), FieldAttributes.Public | FieldAttributes.Static, FieldAttributes.Public | FieldAttributes.Static }
        };

        [Fact]
        public void GetFieldGetFieldsTest()
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            foreach(object[] fd in FieldTestData)
            {
                FieldBuilder field = type.DefineField((string)fd[0], (Type)fd[1], (FieldAttributes)fd[2]);
                Assert.Equal(fd[0], field.Name);
                Assert.Equal(fd[1], field.FieldType);
                Assert.Equal(fd[3], field.Attributes);
                Assert.Equal(type.AsType(), field.DeclaringType);
                Assert.Equal(field.Module, field.Module);
            }

            type.CreateType();
            FieldInfo[] allFields = type.GetFields(Helpers.AllFlags);
            Assert.Equal(FieldTestData.Count, allFields.Length);
            Assert.Equal(4, type.GetFields().Length);
            Assert.Equal(3, type.GetFields(BindingFlags.Public | BindingFlags.Instance).Length);
            Assert.Equal(1, type.GetFields(BindingFlags.Public | BindingFlags.Static).Length);
            Assert.Equal(12, type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Length);
            Assert.Equal(2, type.GetFields(BindingFlags.NonPublic | BindingFlags.Static).Length);

            Assert.Throws<AmbiguousMatchException>(() => type.GetField("TestName1", Helpers.AllFlags));
            Assert.Equal(allFields[0], type.GetField("TestName1", BindingFlags.Public | BindingFlags.Instance));
            Assert.Equal(allFields[allFields.Length-1], type.GetField("TestName1", BindingFlags.Public | BindingFlags.Static));
            Assert.Equal(allFields[10], type.GetField("testname5", Helpers.AllFlags));
            Assert.Equal(allFields[10], type.GetField("testname5", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase));
            Assert.Equal(allFields[9], type.GetField("testname5", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase));
        }

        [Fact]
        public void AbstractBaseMethodImplementationReturnsDifferentType()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                TypeBuilder baseType = ab.GetDynamicModule("MyModule").DefineType("Base", TypeAttributes.Public | TypeAttributes.Abstract);
                MethodBuilder getBase = baseType.DefineMethod("Get", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, baseType, null);
                type.SetParent(baseType);
                MethodBuilder getDerived = type.DefineMethod("Get", MethodAttributes.Public | MethodAttributes.Virtual, type, null);
                ILGenerator ilGenerator = getDerived.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ret);

                type.DefineMethodOverride(getDerived, getBase);
                baseType.CreateType();
                type.CreateType();
                ab.Save(file.Path);

                TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
                Type typeFromDisk = tlc.LoadFromAssemblyPath(file.Path).GetType("MyType");
                MethodInfo getFromDisk = typeFromDisk.GetMethod("Get");
                object instance = Activator.CreateInstance(typeFromDisk);
                object obj = getFromDisk.Invoke(instance, null);
                Assert.IsType(typeFromDisk, obj);
            }
        }
    }
}
