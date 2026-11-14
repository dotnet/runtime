// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class SignatureHelperGetMethodSigHelper
    {
        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [InlineData(CallingConventions.Any, typeof(int))]
        [InlineData(CallingConventions.ExplicitThis, typeof(string))]
        [InlineData(CallingConventions.HasThis, typeof(string))]
        [InlineData(CallingConventions.Standard, typeof(string))]
        [InlineData(CallingConventions.VarArgs, typeof(string))]
        public void GetMethodSigHelper_CallingConventions_Type_Length_ReturnsThree(CallingConventions callingConventions, Type type)
        {
            SignatureHelper helper = SignatureHelper.GetMethodSigHelper(callingConventions, type);
            Assert.Equal(3, helper.GetSignature().Length);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [InlineData(CallingConventions.Any, typeof(int))]
        [InlineData(CallingConventions.ExplicitThis, typeof(string))]
        [InlineData(CallingConventions.HasThis, typeof(string))]
        [InlineData(CallingConventions.Standard, typeof(string))]
        [InlineData(CallingConventions.VarArgs, typeof(string))]
        public void GetMethodSigHelper_Module_CallingConventions_Type_Length_ReturnsThree(CallingConventions callingConventions, Type type)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper1 = SignatureHelper.GetMethodSigHelper(module, callingConventions, type);
            Assert.Equal(3, helper1.GetSignature().Length);

            SignatureHelper helper2 = SignatureHelper.GetMethodSigHelper(null, callingConventions, type);
            Assert.Equal(3, helper2.GetSignature().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public void GetMethodSigHelper_Module_Type_TypeArray()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper1 = SignatureHelper.GetMethodSigHelper(module, typeof(string), new Type[] { typeof(char), typeof(int) });
            Assert.Equal(5, helper1.GetSignature().Length);

            SignatureHelper helper2 = SignatureHelper.GetMethodSigHelper(null, typeof(string), new Type[] { typeof(char), typeof(int) });
            Assert.Equal(5, helper2.GetSignature().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public void GetMethodSigHelper_Module_Type_TypeArray_NullObjectInParameterType_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("argument", () => SignatureHelper.GetMethodSigHelper(module, typeof(string), new Type[] { typeof(char), null }));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124149", TestRuntimes.Mono)]
        public void GetMethodSigHelper_FunctionPointerParameter_ThrowsNotSupportedException()
        {
            SignatureHelper sig = SignatureHelper.GetMethodSigHelper(CallingConventions.Standard, typeof(void));
            AssertExtensions.Throws<NotSupportedException>(() => sig.AddArgument(Type.MakeFunctionPointerSignatureType(typeof(int), [typeof(bool)])));
        }

        [Fact]
        [SkipOnMono("Mono's SignatureHelper does not reject function pointer types.")]
        public void GetMethodSigHelper_FunctionPointerParameterWithGenericDefinitionReturn_ThrowsNotSupportedException()
        {
            // MakeFunctionPointerType rejects a generic type definition as a return type, so the later check, which
            // rebuilds a runtime function pointer type, would throw InvalidOperationException; only the early rejection
            // of a signature type gives NotSupportedException.
            SignatureHelper sig = SignatureHelper.GetMethodSigHelper(CallingConventions.Standard, typeof(void));
            AssertExtensions.Throws<NotSupportedException>(() => sig.AddArgument(Type.MakeFunctionPointerSignatureType(typeof(List<>), [])));
        }

        [Fact]
        [SkipOnMono("Mono's SignatureHelper does not reject function pointer types.")]
        public unsafe void GetMethodSigHelper_ModuleFunctionPointerParameter_ThrowsNotSupportedException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<NotSupportedException>(() => SignatureHelper.GetMethodSigHelper(module, typeof(int), [typeof(delegate*<int, int>)]));
        }

        public static unsafe IEnumerable<object[]> FunctionPointerParameter_TestData() =>
        [
            [typeof(delegate*<int, int>), new byte[] { 0x1B, 0x00, 0x01, 0x08, 0x08 }],
            [typeof(delegate* unmanaged<int>), new byte[] { 0x1B, 0x09, 0x00, 0x08 }],
            [typeof(delegate* unmanaged[Cdecl]<int>), new byte[] { 0x1B, 0x09, 0x00, 0x08 }],
            [typeof(delegate*<delegate*<int>, void>), new byte[] { 0x1B, 0x00, 0x01, 0x01, 0x1B, 0x00, 0x00, 0x08 }],
            [typeof(delegate*<int[], ref int, int*>), new byte[] { 0x1B, 0x00, 0x02, 0x0F, 0x08, 0x1D, 0x08, 0x10, 0x08 }],
            [typeof(delegate*<Version, TimeSpan>), Concat([0x1B, 0x00, 0x01], InternalType(typeof(TimeSpan)), InternalType(typeof(Version)))],
        ];

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [MemberData(nameof(FunctionPointerParameter_TestData))]
        public void GetMethodSigHelper_FunctionPointerParameter_EncodesFunctionPointerSignature(Type functionPointerType, byte[] expected)
        {
            SignatureHelper sig = SignatureHelper.GetMethodSigHelper(null, typeof(void), [functionPointerType]);
            Assert.Equal(Concat([0x00, 0x01, 0x01], expected), sig.GetSignature());
        }

        [Theory]
        [SkipOnMono("Mono's SignatureHelper does not reject function pointer types.")]
        [InlineData((byte)0x05)] // vararg
        [InlineData((byte)0x20)] // instance
        [InlineData((byte)0x29)] // instance unmanaged
        public void GetMethodSigHelper_OtherCallingConventionFunctionPointerParameter_ThrowsNotSupportedException(byte callingConvention)
        {
            Type functionPointerType = GetFunctionPointerFieldType(callingConvention);
            Assert.True(functionPointerType.IsFunctionPointer);
            AssertExtensions.Throws<NotSupportedException>(() => SignatureHelper.GetMethodSigHelper(null, typeof(int), [functionPointerType]));
        }

        private static byte[] Concat(params byte[][] parts)
        {
            List<byte> bytes = new();
            foreach (byte[] part in parts)
                bytes.AddRange(part);
            return bytes.ToArray();
        }

        private static byte[] InternalType(Type type)
        {
            nint handle = type.TypeHandle.Value;
            return Concat([0x21], IntPtr.Size == 8 ? BitConverter.GetBytes((long)handle) : BitConverter.GetBytes((int)handle));
        }

        // C# and Type.MakeFunctionPointerType can't create a vararg or instance function pointer type, so read one
        // from a field whose signature is FIELD FNPTR <callingConvention>, no parameters, returning int.
        private static Type GetFunctionPointerFieldType(byte callingConvention)
        {
            string name = $"FunctionPointer{callingConvention:X2}";
            MetadataBuilder metadata = new();
            metadata.AddModule(0, metadata.GetOrAddString(name + ".dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
            metadata.AddAssembly(metadata.GetOrAddString(name), new Version(1, 0, 0, 0), default, default, default, AssemblyHashAlgorithm.None);
            AssemblyName coreLibName = typeof(object).Assembly.GetName();
            AssemblyReferenceHandle coreLib = metadata.AddAssemblyReference(metadata.GetOrAddString(coreLibName.Name!), coreLibName.Version!, default, default, default, default);
            TypeReferenceHandle objectType = metadata.AddTypeReference(coreLib, metadata.GetOrAddString("System"), metadata.GetOrAddString("Object"));
            metadata.AddTypeDefinition(default, default, metadata.GetOrAddString("<Module>"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
            FieldDefinitionHandle field = metadata.AddFieldDefinition(FieldAttributes.Public | FieldAttributes.Static, metadata.GetOrAddString("F"), metadata.GetOrAddBlob(new byte[] { 0x06, 0x1B, callingConvention, 0x00, 0x08 }));
            metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, default, metadata.GetOrAddString("C"), objectType, field, MetadataTokens.MethodDefinitionHandle(1));

            BlobBuilder image = new();
            new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder()).Serialize(image);

            Assembly assembly = new AssemblyLoadContext(name).LoadFromStream(new MemoryStream(image.ToArray()));
            return assembly.GetType("C", throwOnError: true)!.GetField("F")!.FieldType;
        }
    }
}
