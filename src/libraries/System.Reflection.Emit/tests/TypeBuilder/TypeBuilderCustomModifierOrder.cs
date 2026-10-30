// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderCustomModifierOrderTests
    {
        private static readonly Type[] s_required = [typeof(IsConst), typeof(IsVolatile)];
        private static readonly Type[] s_optional = [typeof(IsLong), typeof(IsBoxed)];

        private static readonly (bool Required, Type Modifier)[] s_signatureModifiers =
            [(true, typeof(IsVolatile)), (true, typeof(IsConst)), (false, typeof(IsBoxed)), (false, typeof(IsLong))];

        [Fact]
        [SkipOnMono("Mono's AssemblyBuilder writes each custom modifier array in the order given.")]
        public void RuntimeAssemblyBuilder_CustomModifierOrder()
        {
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("CustomModifierOrderRuntime"), AssemblyBuilderAccess.Run);
            Type type = DefineType(ab.DefineDynamicModule("CustomModifierOrderRuntime")).CreateType();

            AssertCustomModifiers(type);
        }

        [Fact]
        [SkipOnMono("Mono's Module.ResolveSignature resolves only standalone signature tokens.")]
        public void PersistedAssemblyBuilder_CustomModifierOrder()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("CustomModifierOrderPersisted"));
            DefineType(ab.DefineDynamicModule("CustomModifierOrderPersisted")).CreateType();
            using MemoryStream stream = new MemoryStream();
            ab.Save(stream);
            stream.Position = 0;

            TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();
            try
            {
                AssertCustomModifiers(tlc.LoadFromStream(stream).GetType("CustomModifierOrder"));
            }
            finally
            {
                tlc.Unload();
            }
        }

        private static TypeBuilder DefineType(ModuleBuilder module)
        {
            TypeBuilder type = module.DefineType("CustomModifierOrder", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            type.DefineField("Field", typeof(int), s_required, s_optional, FieldAttributes.Public | FieldAttributes.Static);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                typeof(int), s_required, s_optional, [typeof(int)], [s_required], [s_optional]);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);
            return type;
        }

        // GetRequiredCustomModifiers and GetOptionalCustomModifiers return each kind separately, so only the
        // signature shows where the required modifiers sit relative to the optional ones.
        private static unsafe void AssertCustomModifiers(Type type)
        {
            FieldInfo field = type.GetField("Field");
            byte[] fieldSignature = type.Module.ResolveSignature(field.MetadataToken);
            fixed (byte* start = fieldSignature)
            {
                BlobReader reader = new BlobReader(start, fieldSignature.Length);
                Assert.Equal(SignatureKind.Field, reader.ReadSignatureHeader().Kind);
                AssertModifiedInt32(type.Module, ref reader);
            }

            Assert.Equal(s_required, field.GetRequiredCustomModifiers());
            Assert.Equal(s_optional, field.GetOptionalCustomModifiers());

            MethodInfo method = type.GetMethod("Method");
            byte[] methodSignature = type.Module.ResolveSignature(method.MetadataToken);
            fixed (byte* start = methodSignature)
            {
                BlobReader reader = new BlobReader(start, methodSignature.Length);
                Assert.Equal(SignatureKind.Method, reader.ReadSignatureHeader().Kind);
                Assert.Equal(1, reader.ReadCompressedInteger());
                AssertModifiedInt32(type.Module, ref reader);
                AssertModifiedInt32(type.Module, ref reader);
            }

            foreach (ParameterInfo parameter in new[] { method.ReturnParameter, method.GetParameters()[0] })
            {
                Assert.Equal(s_required, parameter.GetRequiredCustomModifiers());
                Assert.Equal(s_optional, parameter.GetOptionalCustomModifiers());
            }
        }

        private static void AssertModifiedInt32(Module module, ref BlobReader reader)
        {
            List<(bool Required, Type Modifier)> modifiers = new();
            SignatureTypeCode typeCode;
            while ((typeCode = reader.ReadSignatureTypeCode()) is SignatureTypeCode.RequiredModifier or SignatureTypeCode.OptionalModifier)
            {
                modifiers.Add((typeCode == SignatureTypeCode.RequiredModifier, module.ResolveType(MetadataTokens.GetToken(reader.ReadTypeHandle()))));
            }

            Assert.Equal(s_signatureModifiers, modifiers);
            Assert.Equal(SignatureTypeCode.Int32, typeCode);
        }
    }
}
