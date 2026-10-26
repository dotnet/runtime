// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineMethodTests
    {
        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { "Name", MethodAttributes.Abstract, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Assembly, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.CheckAccessOnOverride, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.FamANDAssem, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Family, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.FamORAssem, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Final, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.HasSecurity, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.HideBySig, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.MemberAccessMask, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.NewSlot, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.PinvokeImpl, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Private, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.PrivateScope, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.RequireSecObject, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.ReuseSlot, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.RTSpecialName, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.SpecialName, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.UnmanagedExport, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Virtual, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.VtableLayoutMask, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Abstract | MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual, CallingConventions.Standard, null, null };
            yield return new object[] { "A!?123C\"", MethodAttributes.Final | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.Static, CallingConventions.Standard, null, null };

            // Static
            yield return new object[] { "`Name", MethodAttributes.Static, CallingConventions.Any, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.ExplicitThis, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.HasThis, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.VarArgs, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.Any | CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.Any | CallingConventions.VarArgs, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.HasThis | CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.HasThis | CallingConventions.ExplicitThis, null, null };
            yield return new object[] { "\uD800\uDC00", MethodAttributes.Static, (CallingConventions)(-1), null, null };

            // Instance
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.Any, null, null };
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.ExplicitThis, null, null };
            yield return new object[] { "Na.me", MethodAttributes.Public, CallingConventions.HasThis, null, null };
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.Standard, null, null };
            yield return new object[] { "Na{m}e", MethodAttributes.Public, CallingConventions.VarArgs, null, null };
            yield return new object[] { "Na*me", MethodAttributes.Public, CallingConventions.Any | CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.Any | CallingConventions.VarArgs, null, null };
            yield return new object[] { "Name$", MethodAttributes.Public, CallingConventions.HasThis | CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.HasThis | CallingConventions.ExplicitThis, null, null };
            yield return new object[] { "42", MethodAttributes.Public, (CallingConventions)(-1), null, null };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            bool defaultReturnTypeAndParameters = returnType == null && parameterTypes == null;
            if (callingConvention == CallingConventions.Standard)
            {
                if (defaultReturnTypeAndParameters)
                {
                    // Use DefineMethod(string, MethodAttributes)
                    TypeBuilder type1 = Helpers.DynamicType(TypeAttributes.Public);
                    MethodBuilder method1 = type1.DefineMethod(name, attributes);
                    VerifyMethod(type1, method1, name, attributes, callingConvention, returnType, parameterTypes);
                }
                // Use DefineMethod(string, MethodAttributes, Type, Type[])
                TypeBuilder type2 = Helpers.DynamicType(TypeAttributes.Public);
                MethodBuilder method2 = type2.DefineMethod(name, attributes, returnType, parameterTypes);
                VerifyMethod(type2, method2, name, attributes, callingConvention, returnType, parameterTypes);
            }
            if (defaultReturnTypeAndParameters)
            {
                // Use DefineMethod(string, MethodAttributes, CallingConventions)
                TypeBuilder type3 = Helpers.DynamicType(TypeAttributes.Public);
                MethodBuilder method3 = type3.DefineMethod(name, attributes, callingConvention);
                VerifyMethod(type3, method3, name, attributes, callingConvention, returnType, parameterTypes);
            }
            // Use DefineMethod(string, MethodAttributes, CallingConventions, Type, Type[])
            TypeBuilder type4 = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method4 = type4.DefineMethod(name, attributes, callingConvention, returnType, parameterTypes);
            VerifyMethod(type4, method4, name, attributes, callingConvention, returnType, parameterTypes);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        [MemberData(nameof(TestData))]
        public void DefineMethodPersistedAssembly(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            bool defaultReturnTypeAndParameters = returnType == null && parameterTypes == null;
            if (callingConvention == CallingConventions.Standard)
            {
                if (defaultReturnTypeAndParameters)
                {
                    // Use DefineMethod(string, MethodAttributes)
                    MethodBuilder method1 = type.DefineMethod(name, attributes);
                    VerifyMethod(type, method1, name, attributes, callingConvention, returnType, parameterTypes);
                }
                // Use DefineMethod(string, MethodAttributes, Type, Type[])
                MethodBuilder method2 = type.DefineMethod(name, attributes, returnType, parameterTypes);
                VerifyMethod(type, method2, name, attributes, callingConvention, returnType, parameterTypes);
            }
            if (defaultReturnTypeAndParameters)
            {
                // Use DefineMethod(string, MethodAttributes, CallingConventions)
                MethodBuilder method3 = type.DefineMethod(name, attributes, callingConvention);
                VerifyMethod(type, method3, name, attributes, callingConvention, returnType, parameterTypes);
            }
            // Use DefineMethod(string, MethodAttributes, CallingConventions, Type, Type[])
            MethodBuilder method4 = type.DefineMethod(name, attributes, callingConvention, returnType, parameterTypes);
            VerifyMethod(type, method4, name, attributes, callingConvention, returnType, parameterTypes);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineMethod_65536Methods()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            for (int i = 0; i < 65536; i++)
            {
                type.DefineMethod($"M_{i}", MethodAttributes.Public).GetILGenerator().Emit(OpCodes.Ret);
            }

            // System.TypeLoadException : Type 'TestType' from assembly 'TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
            // contains more methods than the current implementation allows.
            Assert.Throws<TypeLoadException>(() => type.CreateType());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineMethod_65536MethodsPersistedAssembly()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("DefineMethod_65536MethodsPersistedAssembly"));
            TypeBuilder type = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public);
            for (int i = 0; i < 65536; i++)
            {
                MethodBuilder method = type.DefineMethod($"M_{i}", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
                ILGenerator il = method.GetILGenerator();
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ret);
            }
            type.CreateType();

            using (var stream = new MemoryStream())
            {
                ab.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
                // System.TypeLoadException : Type 'MyType' from assembly 'MyDynamicAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null'
                // contains more methods than the current implementation allows.
                Assert.Throws<TypeLoadException>(() => assembly.GetType("MyType"));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type createdType = mlc.LoadFromStream(stream).GetType("MyType"); // Loads with MLC
                    MethodInfo[] methods = createdType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    Assert.Equal(65536, methods.Length);
                    Assert.Equal("M_65535", methods[65535].Name);
                }
            }
        }

        [Fact]
        public void DefineMethod_MultipleOverloads_Works()
        {
            const string Name = "Name";
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method1 = type.DefineMethod(Name, MethodAttributes.Public);
            Assert.Equal(Name, method1.Name);

            MethodBuilder method2 = type.DefineMethod(Name, MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), new Type[] { typeof(int) });
            Assert.Equal(Name, method2.Name);
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedSignatureTypes_ModifiersRoundTrip()
        {
            Type parameterType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsConst), typeof(IsVolatile)], [typeof(IsLong), typeof(IsBoxed)]);
            Type returnType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsVolatile), typeof(IsConst)], null);
            Type byRefType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsConst)], null).MakeByRefType();
            Type genericType = Type.MakeGenericSignatureType(typeof(List<>), Type.MakeModifiedSignatureType(typeof(int), null, [typeof(IsLong)]));

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, returnType, [parameterType, byRefType, genericType]);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);

            MethodInfo created = type.CreateType().GetMethod("M");
            ParameterInfo[] parameters = created.GetParameters();
            Type createdParameterType = parameters[0].GetModifiedParameterType();
            Assert.Equal([typeof(IsConst), typeof(IsVolatile)], createdParameterType.GetRequiredCustomModifiers());
            Assert.Equal([typeof(IsLong), typeof(IsBoxed)], createdParameterType.GetOptionalCustomModifiers());
            Assert.Equal([typeof(IsVolatile), typeof(IsConst)], created.ReturnParameter.GetModifiedParameterType().GetRequiredCustomModifiers());
            Assert.Equal([typeof(IsConst)], parameters[1].GetModifiedParameterType().GetElementType().GetRequiredCustomModifiers());
            Assert.Equal(typeof(int).MakeByRefType(), parameters[1].ParameterType);
            Assert.Equal([typeof(IsLong)], parameters[2].GetModifiedParameterType().GetGenericArguments()[0].GetOptionalCustomModifiers());
            Assert.Equal(typeof(List<int>), parameters[2].ParameterType);
            Assert.Equal(42, created.Invoke(null, [42, 0, null]));
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedParameterTypes_CopiesSignature()
        {
            Type pointerType = Type.MakeModifiedSignatureType(typeof(int), null, [typeof(IsConst)]).MakePointerType();
            Type genericType = Type.MakeGenericSignatureType(typeof(List<>), Type.MakeModifiedSignatureType(typeof(int), null, [typeof(IsLong)]));

            TypeBuilder sourceType = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Abstract);
            sourceType.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, CallingConventions.Standard,
                typeof(int), null, [typeof(IsLong), typeof(IsBoxed)],
                [typeof(int), pointerType, genericType], [[typeof(IsConst), typeof(IsVolatile)], null, null], null);
            MethodInfo source = sourceType.CreateType().GetMethod("M");
            ParameterInfo[] sourceParameters = source.GetParameters();

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Abstract, assemblyName: "CopyAssembly");
            type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, CallingConventions.Standard,
                source.ReturnParameter.GetModifiedParameterType(),
                [sourceParameters[0].GetModifiedParameterType(), sourceParameters[1].GetModifiedParameterType(), sourceParameters[2].GetModifiedParameterType()]);
            MethodInfo copy = type.CreateType().GetMethod("M");
            ParameterInfo[] copyParameters = copy.GetParameters();

            // Reflection returns the modifiers of each kind in the reverse of their signature order.
            Assert.Equal([typeof(IsVolatile), typeof(IsConst)], sourceParameters[0].GetModifiedParameterType().GetRequiredCustomModifiers());
            Assert.Equal([typeof(IsVolatile), typeof(IsConst)], copyParameters[0].GetModifiedParameterType().GetRequiredCustomModifiers());
            Assert.Equal([typeof(IsBoxed), typeof(IsLong)], copy.ReturnParameter.GetModifiedParameterType().GetOptionalCustomModifiers());
            Assert.Equal(typeof(int).MakePointerType(), copyParameters[1].ParameterType);
            Assert.Equal([typeof(IsConst)], copyParameters[1].GetModifiedParameterType().GetElementType().GetOptionalCustomModifiers());
            Assert.Equal(typeof(List<int>), copyParameters[2].ParameterType);
            Assert.Equal([typeof(IsLong)], copyParameters[2].GetModifiedParameterType().GetGenericArguments()[0].GetOptionalCustomModifiers());
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedParameterTypeWithoutSignature_WritesUnmodifiedType()
        {
            // A DynamicMethod's parameters have no signature to read modifiers from.
            Type parameterType = new DynamicMethod("Source", typeof(void), [typeof(int)]).GetParameters()[0].GetModifiedParameterType();

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [parameterType]).GetILGenerator().Emit(OpCodes.Ret);
            ParameterInfo parameter = type.CreateType().GetMethod("M").GetParameters()[0];

            Assert.Equal(typeof(int), parameter.ParameterType);
            Assert.Empty(parameter.GetModifiedParameterType().GetRequiredCustomModifiers());
            Assert.Empty(parameter.GetModifiedParameterType().GetOptionalCustomModifiers());
        }

        public class GenericParameterHolder<T>
        {
            public static void M(T value) { }
            public static void N<U>(U value) { }
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedParameterTypeFromGenericParameter_WritesUnmodifiedTypes()
        {
            // A parameter declared as T or U has no signature for the element and generic argument types substituted for it.
            Type[] parameterTypes =
            [
                typeof(GenericParameterHolder<List<int>>).GetMethod(nameof(GenericParameterHolder<int>.M)).GetParameters()[0].GetModifiedParameterType(),
                typeof(GenericParameterHolder<int[]>).GetMethod(nameof(GenericParameterHolder<int>.M)).GetParameters()[0].GetModifiedParameterType(),
                typeof(GenericParameterHolder<int>).GetMethod(nameof(GenericParameterHolder<int>.N)).MakeGenericMethod(typeof(List<int>)).GetParameters()[0].GetModifiedParameterType(),
            ];

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(void), parameterTypes).GetILGenerator().Emit(OpCodes.Ret);
            ParameterInfo[] parameters = type.CreateType().GetMethod("M").GetParameters();

            Assert.Equal([typeof(List<int>), typeof(int[]), typeof(List<int>)], Array.ConvertAll(parameters, p => p.ParameterType));
            Type[] nestedTypes =
            [
                parameters[0].GetModifiedParameterType().GetGenericArguments()[0],
                parameters[1].GetModifiedParameterType().GetElementType(),
                parameters[2].GetModifiedParameterType().GetGenericArguments()[0],
            ];
            foreach (Type nestedType in nestedTypes)
            {
                Assert.Equal(typeof(int), nestedType.UnderlyingSystemType);
                Assert.Empty(nestedType.GetRequiredCustomModifiers());
                Assert.Empty(nestedType.GetOptionalCustomModifiers());
            }
        }

        public interface IInParameter
        {
            int M(in int value);
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_InParameterTypeFromInterface_ImplementsInterfaceMethod()
        {
            // The C# compiler puts modreq(InAttribute) on the by-ref of an interface method's in parameter.
            Type parameterType = typeof(IInParameter).GetMethod(nameof(IInParameter.M)).GetParameters()[0].GetModifiedParameterType();
            Assert.Equal([typeof(System.Runtime.InteropServices.InAttribute)], parameterType.GetRequiredCustomModifiers());

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.AddInterfaceImplementation(typeof(IInParameter));
            ILGenerator il = type.DefineMethod(nameof(IInParameter.M), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final | MethodAttributes.HideBySig, typeof(int), [parameterType]).GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Ret);
            Type created = type.CreateType();

            Assert.Equal(42, ((IInParameter)Activator.CreateInstance(created)).M(42));
            Assert.Equal([typeof(System.Runtime.InteropServices.InAttribute)], created.GetMethod(nameof(IInParameter.M)).GetParameters()[0].GetModifiedParameterType().GetRequiredCustomModifiers());
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedParameterTypes_KeepOptionalBeforeRequiredModifiers()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder interfaceType = module.DefineType("I", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            // Modifier arrays are written optional modifiers first.
            interfaceType.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                CallingConventions.HasThis, typeof(int), [typeof(IsConst)], [typeof(IsLong)],
                [typeof(int)], [[typeof(IsVolatile)]], [[typeof(IsBoxed)]]);
            Type createdInterface = interfaceType.CreateType();
            MethodInfo source = createdInterface.GetMethod("M");

            TypeBuilder type = module.DefineType("C", TypeAttributes.Public, typeof(object), [createdInterface]);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                CallingConventions.HasThis, source.ReturnParameter.GetModifiedParameterType(), [source.GetParameters()[0].GetModifiedParameterType()]);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ret);
            type.DefineMethodOverride(method, source);
            Type created = type.CreateType();
            MethodInfo copy = created.GetMethod("M");

            Assert.Equal(source.Module.ResolveSignature(source.MetadataToken), copy.Module.ResolveSignature(copy.MetadataToken));
            Assert.Equal(42, source.Invoke(Activator.CreateInstance(created), [42]));
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedParameterTypeWithFourByteModifierToken_KeepsOptionalBeforeRequired()
        {
            // A TypeDef row of 4096 or more takes a four-byte compressed token in a signature.
            ModuleBuilder module = Helpers.DynamicModule();
            for (int i = 0; i < 4096; i++)
                module.DefineType("Filler" + i);
            Type modifier = module.DefineType("Modifier", TypeAttributes.Public).CreateType();
            TypeBuilder interfaceType = module.DefineType("I", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            interfaceType.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                CallingConventions.HasThis, typeof(void), null, null,
                [typeof(int)], [[typeof(IsConst)]], [[modifier]]);
            Type createdInterface = interfaceType.CreateType();
            MethodInfo source = createdInterface.GetMethod("M");

            TypeBuilder type = Helpers.DynamicModule().DefineType("C", TypeAttributes.Public, typeof(object), [createdInterface]);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                CallingConventions.HasThis, typeof(void), [source.GetParameters()[0].GetModifiedParameterType()]);
            method.GetILGenerator().Emit(OpCodes.Ret);
            type.DefineMethodOverride(method, source);

            // Modifier arrays are written optional modifiers first; the type fails to load if the copy writes the
            // required one first.
            Assert.Contains(createdInterface, type.CreateType().GetInterfaces());
        }

        [Theory]
        [InlineData("ModifierOrder.ITop")]
        [InlineData("ModifierOrder.IPointer")]
        [InlineData("ModifierOrder.IByRef")]
        [InlineData("ModifierOrder.IArray")]
        [InlineData("ModifierOrder.IGenericArgument")]
        [InlineData("ModifierOrder.IReturnPointer")]
        [InlineData("ModifierOrder.ITwoLevels")]
        [InlineData("ModifierOrder.ITwoByteTokens")]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedTypesFromSignature_KeepModifierOrder(string interfaceName)
        {
            // The interface methods (System.Reflection.Emit.TestModifierOrder.il) have optional modifiers before
            // required ones, interleaved, nested in element and generic argument types, or with two-byte tokens.
            Type interfaceType = Assembly.Load("System.Reflection.Emit.TestModifierOrder").GetType(interfaceName, throwOnError: true);
            MethodInfo source = interfaceType.GetMethod("M");

            TypeBuilder type = Helpers.DynamicModule().DefineType("C", TypeAttributes.Public, typeof(object), [interfaceType]);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                CallingConventions.HasThis, source.ReturnParameter.GetModifiedParameterType(), Array.ConvertAll(source.GetParameters(), p => p.GetModifiedParameterType()));
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Throw);
            type.DefineMethodOverride(method, source);

            // The type fails to load if the override's signature differs from the interface method's.
            Assert.Contains(interfaceType, type.CreateType().GetInterfaces());
        }

        public static IEnumerable<object[]> ModifierArrays_TestData()
        {
            yield return [new[] { typeof(IsVolatile), typeof(IsBoxed) }, null];
            yield return [null, new[] { typeof(IsCopyConstructed), typeof(IsUdtReturn) }];
            yield return [new[] { typeof(IsVolatile), typeof(IsBoxed) }, new[] { typeof(IsCopyConstructed), typeof(IsUdtReturn) }];
            yield return [Type.EmptyTypes, null];
            yield return [null, Type.EmptyTypes];
            yield return [null, null];
        }

        [Theory]
        [MemberData(nameof(ModifierArrays_TestData))]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedTypeAndModifierArrays_ArraysReplaceModifiersOfTheirKind(Type[] requiredModifiers, Type[] optionalModifiers)
        {
            Type parameterType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsConst), typeof(IsSignUnspecifiedByte)], [typeof(IsLong), typeof(IsExplicitlyDereferenced)]);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Abstract);
            type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, CallingConventions.Standard,
                typeof(void), null, null,
                [parameterType], [requiredModifiers], [optionalModifiers]);
            MethodInfo method = type.CreateType().GetMethod("M");
            Type created = method.GetParameters()[0].GetModifiedParameterType();

            // An array replaces the type's own modifiers of its kind and is read in the order reflection returns.
            Type[] expectedRequired = requiredModifiers ?? parameterType.GetRequiredCustomModifiers();
            Type[] expectedOptional = optionalModifiers ?? parameterType.GetOptionalCustomModifiers();
            Assert.Equal(expectedRequired, created.GetRequiredCustomModifiers());
            Assert.Equal(expectedOptional, created.GetOptionalCustomModifiers());

            // Required modifiers come first: HASTHIS, one parameter, VOID, then the parameter's modifiers.
            byte[] signature = method.Module.ResolveSignature(method.MetadataToken);
            for (int i = 0; i < expectedRequired.Length + expectedOptional.Length; i++)
                Assert.Equal((byte)(i < expectedRequired.Length ? SignatureTypeCode.RequiredModifier : SignatureTypeCode.OptionalModifier), signature[3 + 2 * i]);
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedTypeAndNullInModifierArray_ThrowsArgumentNullException()
        {
            Type parameterType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsConst)], null);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Abstract);
            AssertExtensions.Throws<ArgumentNullException>("requiredCustomModifiers", () =>
            {
                type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, CallingConventions.Standard,
                    typeof(void), null, null,
                    [parameterType], [[null]], null);
                type.CreateType();
            });
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_LocalOfModifiedSignatureType_Runs()
        {
            Type localType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsConst)], [typeof(IsLong)]);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int)]);
            ILGenerator il = method.GetILGenerator();
            LocalBuilder local = il.DeclareLocal(localType);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stloc, local);
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Ret);
            MethodInfo created = type.CreateType().GetMethod("M");

            Assert.Equal(42, created.Invoke(null, [42]));
            byte[] localSignature = created.Module.ResolveSignature(created.GetMethodBody().LocalSignatureMetadataToken);
            Assert.Equal(7, localSignature.Length);
            Assert.Equal((byte)SignatureKind.LocalVariables, localSignature[0]);
            Assert.Equal(1, localSignature[1]);
            AssertModifiedInt32(created.Module, localSignature, 2);
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_CalliWithModifiedTypes_KeepsModifiers()
        {
            Type modifiedType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsConst)], [typeof(IsLong)]);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder target = type.DefineMethod("Target", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int)]);
            ILGenerator targetIL = target.GetILGenerator();
            targetIL.Emit(OpCodes.Ldarg_0);
            targetIL.Emit(OpCodes.Ret);
            ILGenerator managedIL = type.DefineMethod("Managed", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int)]).GetILGenerator();
            managedIL.Emit(OpCodes.Ldarg_0);
            managedIL.Emit(OpCodes.Ldftn, target);
            managedIL.EmitCalli(OpCodes.Calli, CallingConventions.Standard, modifiedType, [modifiedType], null);
            managedIL.Emit(OpCodes.Ret);
            ILGenerator unmanagedIL = type.DefineMethod("Unmanaged", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int), typeof(IntPtr)]).GetILGenerator();
            unmanagedIL.Emit(OpCodes.Ldarg_0);
            unmanagedIL.Emit(OpCodes.Ldarg_1);
            unmanagedIL.EmitCalli(OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.Cdecl, modifiedType, [modifiedType]);
            unmanagedIL.Emit(OpCodes.Ret);
            Type created = type.CreateType();

            Assert.Equal(42, created.GetMethod("Managed").Invoke(null, [42]));
            AssertCalliSignature("Managed", OpCodes.Ldarg_0.Size + OpCodes.Ldftn.Size + sizeof(int), (byte)SignatureCallingConvention.Default);
            AssertCalliSignature("Unmanaged", OpCodes.Ldarg_0.Size + OpCodes.Ldarg_1.Size, (byte)SignatureCallingConvention.CDecl);

            // The calli's signature: its calling convention, one parameter, then the return and parameter types.
            void AssertCalliSignature(string name, int calliOffset, byte callingConvention)
            {
                MethodInfo method = created.GetMethod(name);
                byte[] signature = method.Module.ResolveSignature(BitConverter.ToInt32(method.GetMethodBody().GetILAsByteArray(), calliOffset + OpCodes.Calli.Size));
                Assert.Equal(12, signature.Length);
                Assert.Equal(callingConvention, signature[0]);
                Assert.Equal(1, signature[1]);
                AssertModifiedInt32(created.Module, signature, 2);
                AssertModifiedInt32(created.Module, signature, 7);
            }
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_LdtokenOfModifiedSignatureType_LoadsUnmodifiedType()
        {
            Type modifiedType = Type.MakeModifiedSignatureType(typeof(List<int>), [typeof(IsConst)], null);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), Type.EmptyTypes);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldtoken, modifiedType);
            il.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
            il.Emit(OpCodes.Ret);

            MethodInfo created = type.CreateType().GetMethod("M");
            Assert.Equal(typeof(List<int>), created.Invoke(null, null));

            // The type signature starts with GENERICINST, not with the modifier.
            int typeSpecToken = BitConverter.ToInt32(created.GetMethodBody().GetILAsByteArray(), OpCodes.Ldtoken.Size);
            Assert.Equal((byte)SignatureTypeCode.GenericTypeInstance, created.Module.ResolveSignature(typeSpecToken)[0]);
        }

        public sealed class VolatileField
        {
            public volatile int Value;
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedTypeAsILOperand_LoadsUnmodifiedType()
        {
            Type reflectedType = typeof(VolatileField).GetField(nameof(VolatileField.Value)).GetModifiedFieldType();
            Type signatureType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsConst)], [typeof(IsLong)]);
            Assert.Equal([typeof(IsVolatile)], reflectedType.GetRequiredCustomModifiers());

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            DefineLdtoken("LdtokenReflected", reflectedType);
            DefineLdtoken("LdtokenSignature", signatureType);
            DefineLdtoken("LdtokenArray", signatureType.MakeArrayType());
            DefineLdtoken("LdtokenGeneric", Type.MakeGenericSignatureType(typeof(List<>), signatureType));
            DefineUnbox("UnboxReflected", reflectedType);
            DefineUnbox("UnboxSignature", signatureType);
            MethodBuilder identity = type.DefineMethod("Identity", MethodAttributes.Public | MethodAttributes.Static);
            GenericTypeParameterBuilder typeParameter = identity.DefineGenericParameters("T")[0];
            identity.SetSignature(typeParameter, null, null, [typeParameter], null, null);
            ILGenerator identityIL = identity.GetILGenerator();
            identityIL.Emit(OpCodes.Ldarg_0);
            identityIL.Emit(OpCodes.Ret);
            DefineCall("CallReflected", reflectedType);
            DefineCall("CallSignature", signatureType);
            ILGenerator callArrayIL = type.DefineMethod("CallArray", MethodAttributes.Public | MethodAttributes.Static, typeof(object), Type.EmptyTypes).GetILGenerator();
            callArrayIL.Emit(OpCodes.Ldnull);
            callArrayIL.Emit(OpCodes.Call, identity.MakeGenericMethod(signatureType.MakeArrayType()));
            callArrayIL.Emit(OpCodes.Ret);
            Type created = type.CreateType();

            Assert.Equal(typeof(int), created.GetMethod("LdtokenReflected").Invoke(null, null));
            Assert.Equal(typeof(int), created.GetMethod("LdtokenSignature").Invoke(null, null));
            Assert.Equal(typeof(int[]), created.GetMethod("LdtokenArray").Invoke(null, null));
            Assert.Equal(typeof(List<int>), created.GetMethod("LdtokenGeneric").Invoke(null, null));
            Assert.Equal(42, created.GetMethod("UnboxReflected").Invoke(null, [42]));
            Assert.Equal(42, created.GetMethod("UnboxSignature").Invoke(null, [42]));
            Assert.Equal(42, created.GetMethod("CallReflected").Invoke(null, [42]));
            Assert.Equal(42, created.GetMethod("CallSignature").Invoke(null, [42]));
            Assert.Null(created.GetMethod("CallArray").Invoke(null, null));

            // An array element or type argument keeps its modifiers: SZARRAY, or GENERICINST CLASS List`1 1, then
            // CMOD_REQD IsConst CMOD_OPT IsLong I4.
            byte[] arraySpec = GetLdtokenTypeSpec("LdtokenArray");
            Assert.Equal(6, arraySpec.Length);
            Assert.Equal((byte)SignatureTypeCode.SZArray, arraySpec[0]);
            AssertModifiedInt32(created.Module, arraySpec, 1);

            byte[] genericSpec = GetLdtokenTypeSpec("LdtokenGeneric");
            Assert.Equal(9, genericSpec.Length);
            Assert.Equal((byte)SignatureTypeCode.GenericTypeInstance, genericSpec[0]);
            Assert.Equal((byte)SignatureTypeKind.Class, genericSpec[1]);
            Assert.Equal(typeof(List<>), ResolveSignatureToken(created.Module, genericSpec[2]));
            Assert.Equal(1, genericSpec[3]);
            AssertModifiedInt32(created.Module, genericSpec, 4);

            byte[] GetLdtokenTypeSpec(string name)
            {
                MethodInfo method = created.GetMethod(name);
                return method.Module.ResolveSignature(BitConverter.ToInt32(method.GetMethodBody().GetILAsByteArray(), OpCodes.Ldtoken.Size));
            }

            void DefineLdtoken(string name, Type operand)
            {
                ILGenerator il = type.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, typeof(Type), Type.EmptyTypes).GetILGenerator();
                il.Emit(OpCodes.Ldtoken, operand);
                il.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
                il.Emit(OpCodes.Ret);
            }

            void DefineUnbox(string name, Type operand)
            {
                ILGenerator il = type.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(object)]).GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Unbox_Any, operand);
                il.Emit(OpCodes.Ret);
            }

            void DefineCall(string name, Type typeArgument)
            {
                ILGenerator il = type.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int)]).GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, identity.MakeGenericMethod(typeArgument));
                il.Emit(OpCodes.Ret);
            }
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedTypeInTypeToken_MatchesSameModifiedTypeInSignature()
        {
            Type reflectedType = typeof(VolatileField).GetField(nameof(VolatileField.Value)).GetModifiedFieldType();
            Type signatureType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsConst)], null);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.AddInterfaceImplementation(typeof(IEquatable<>).MakeGenericType(reflectedType));
            ILGenerator equalsIL = type.DefineMethod(nameof(IEquatable<int>.Equals), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final | MethodAttributes.HideBySig, typeof(bool), [reflectedType]).GetILGenerator();
            equalsIL.Emit(OpCodes.Ldc_I4_1);
            equalsIL.Emit(OpCodes.Ret);
            MethodInfo arrayGet = ((ModuleBuilder)type.Module).GetArrayMethod(signatureType.MakeArrayType(2), "Get", CallingConventions.HasThis, signatureType, [typeof(int), typeof(int)]);
            ILGenerator getIL = type.DefineMethod("Get", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int[,])]).GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldc_I4_0);
            getIL.Emit(OpCodes.Ldc_I4_0);
            getIL.Emit(OpCodes.Call, arrayGet);
            getIL.Emit(OpCodes.Ret);
            Type created = type.CreateType();

            Assert.True(((IEquatable<int>)Activator.CreateInstance(created)).Equals(5));
            Assert.Equal(7, created.GetMethod("Get").Invoke(null, [new int[,] { { 7 } }]));
        }

        // The type a one-byte TypeDefOrRefOrSpecEncoded token in a signature blob refers to.
        private static Type ResolveSignatureToken(Module module, byte encoded) =>
            module.ResolveType((int)((encoded & 3) switch { 0 => TableIndex.TypeDef, 1 => TableIndex.TypeRef, _ => TableIndex.TypeSpec }) << 24 | (encoded >> 2));

        // Asserts that blob holds CMOD_REQD IsConst CMOD_OPT IsLong I4 at offset.
        private static void AssertModifiedInt32(Module module, byte[] blob, int offset)
        {
            Assert.Equal((byte)SignatureTypeCode.RequiredModifier, blob[offset]);
            Assert.Equal(typeof(IsConst), ResolveSignatureToken(module, blob[offset + 1]));
            Assert.Equal((byte)SignatureTypeCode.OptionalModifier, blob[offset + 2]);
            Assert.Equal(typeof(IsLong), ResolveSignatureToken(module, blob[offset + 3]));
            Assert.Equal((byte)SignatureTypeCode.Int32, blob[offset + 4]);
        }

        private static void VerifyMethod(TypeBuilder type, MethodBuilder method, string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            CallingConventions expectedCallingConvention = callingConvention;
            if ((attributes & MethodAttributes.Static) == 0)
            {
                expectedCallingConvention |= CallingConventions.HasThis;
            }

            Assert.Equal(type.AsType(), method.DeclaringType);
            Assert.Equal(name, method.Name);
            Assert.Equal(attributes, method.Attributes);
            Assert.Equal(expectedCallingConvention, method.CallingConvention);
            Assert.Equal(returnType ?? typeof(void), method.ReturnType);
        }
    }
}
