// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
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

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedTypeAndModifierArrays_ArraysReplaceModifiersOfTheirKind()
        {
            Type parameterType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsConst)], [typeof(IsLong)]);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Abstract);
            type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, CallingConventions.Standard,
                typeof(void), null, null,
                [parameterType], [[typeof(IsVolatile), typeof(IsBoxed)]], null);
            Type created = type.CreateType().GetMethod("M").GetParameters()[0].GetModifiedParameterType();

            // The array replaces the type's own modreqs and is read in the same order, the order reflection returns.
            Assert.Equal([typeof(IsVolatile), typeof(IsBoxed)], created.GetRequiredCustomModifiers());
            Assert.Equal([typeof(IsLong)], created.GetOptionalCustomModifiers());
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_LocalOfModifiedSignatureType_Runs()
        {
            Type localType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsVolatile)], null);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int)]);
            ILGenerator il = method.GetILGenerator();
            LocalBuilder local = il.DeclareLocal(localType);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stloc, local);
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Ret);

            Assert.Equal(42, type.CreateType().GetMethod("M").Invoke(null, [42]));
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
            int typeSpecToken = BitConverter.ToInt32(created.GetMethodBody().GetILAsByteArray(), 1);
            Assert.Equal((byte)0x15, created.Module.ResolveSignature(typeSpecToken)[0]);
        }

        public sealed class VolatileField
        {
            public volatile int Value;
        }

        [Fact]
        [SkipOnMono("Mono's runtime Reflection.Emit does not support modified types.")]
        public void DefineMethod_ModifiedTypeAsILOperand_UsesUnmodifiedType()
        {
            Type reflectedType = typeof(VolatileField).GetField(nameof(VolatileField.Value)).GetModifiedFieldType();
            Type signatureType = Type.MakeModifiedSignatureType(typeof(int), [typeof(IsConst)], [typeof(IsLong)]);
            Assert.Equal([typeof(IsVolatile)], reflectedType.GetRequiredCustomModifiers());

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            DefineLdtoken("LdtokenReflected", reflectedType);
            DefineLdtoken("LdtokenSignature", signatureType);
            DefineLdtoken("LdtokenArray", signatureType.MakeArrayType());
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
            Type created = type.CreateType();

            Assert.Equal(typeof(int), created.GetMethod("LdtokenReflected").Invoke(null, null));
            Assert.Equal(typeof(int), created.GetMethod("LdtokenSignature").Invoke(null, null));
            Assert.Equal(typeof(int[]), created.GetMethod("LdtokenArray").Invoke(null, null));
            Assert.Equal(42, created.GetMethod("UnboxReflected").Invoke(null, [42]));
            Assert.Equal(42, created.GetMethod("UnboxSignature").Invoke(null, [42]));
            Assert.Equal(42, created.GetMethod("CallReflected").Invoke(null, [42]));
            Assert.Equal(42, created.GetMethod("CallSignature").Invoke(null, [42]));

            // The array's type signature is SZARRAY I4, as for typeof(int[]).
            MethodInfo ldtokenArray = created.GetMethod("LdtokenArray");
            int typeSpecToken = BitConverter.ToInt32(ldtokenArray.GetMethodBody().GetILAsByteArray(), 1);
            Assert.Equal(new byte[] { 0x1D, 0x08 }, ldtokenArray.Module.ResolveSignature(typeSpecToken));

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
