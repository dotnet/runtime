// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class MetadataImportWrapperTests
{
    // Build a minimal assembly metadata with types, methods, and fields.
    private static MetadataReader CreateTestMetadata()
    {
        MetadataBuilder mb = new();

        // Module
        mb.AddModule(0, mb.GetOrAddString("TestModule"), mb.GetOrAddGuid(Guid.NewGuid()), default, default);

        // Assembly
        mb.AddAssembly(mb.GetOrAddString("TestAssembly"), new Version(1, 0, 0, 0),
            default, default, AssemblyFlags.PublicKey, AssemblyHashAlgorithm.None);

        // mscorlib assembly ref (for System.Object base type)
        AssemblyReferenceHandle mscorlibRef = mb.AddAssemblyReference(
            mb.GetOrAddString("mscorlib"), new Version(4, 0, 0, 0),
            default, mb.GetOrAddBlob(new byte[] { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 }),
            default, default);

        // System.Object type ref
        TypeReferenceHandle objectRef = mb.AddTypeReference(mscorlibRef,
            mb.GetOrAddString("System"), mb.GetOrAddString("Object"));

        // IDisposable type ref (for interface impl testing)
        TypeReferenceHandle disposableRef = mb.AddTypeReference(mscorlibRef,
            mb.GetOrAddString("System"), mb.GetOrAddString("IDisposable"));

        // Create a method signature blob: void ()
        BlobBuilder sigBlob = new();
        new BlobEncoder(sigBlob).MethodSignature().Parameters(0, returnType => returnType.Void(), parameters => { });
        BlobHandle voidMethodSig = mb.GetOrAddBlob(sigBlob);

        // Create a field signature blob: int
        BlobBuilder fieldSigBlob = new();
        new BlobEncoder(fieldSigBlob).Field().Type().Int32();
        BlobHandle intFieldSig = mb.GetOrAddBlob(fieldSigBlob);

        // TypeDef: <Module> (required, row 1)
        mb.AddTypeDefinition(default, default, mb.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        // TypeDef: TestNamespace.TestClass (row 2)
        TypeDefinitionHandle testClassHandle = mb.AddTypeDefinition(
            TypeAttributes.Public | TypeAttributes.Class,
            mb.GetOrAddString("TestNamespace"),
            mb.GetOrAddString("TestClass"),
            objectRef,
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(1));

        // FieldDef: _value (int)
        mb.AddFieldDefinition(FieldAttributes.Private, mb.GetOrAddString("_value"), intFieldSig);

        // MethodDef: DoWork (void)
        mb.AddMethodDefinition(MethodAttributes.Public, MethodImplAttributes.IL,
            mb.GetOrAddString("DoWork"), voidMethodSig,
            -1, MetadataTokens.ParameterHandle(1));

        // Interface implementation: TestClass : IDisposable
        mb.AddInterfaceImplementation(testClassHandle, disposableRef);

        // TypeDef: TestNamespace.TestClass+NestedType (row 3)
        TypeDefinitionHandle nestedHandle = mb.AddTypeDefinition(
            TypeAttributes.NestedPublic | TypeAttributes.Class,
            default,
            mb.GetOrAddString("NestedType"),
            objectRef,
            MetadataTokens.FieldDefinitionHandle(2),
            MetadataTokens.MethodDefinitionHandle(2));

        // Nested class relationship
        mb.AddNestedType(nestedHandle, testClassHandle);

        // Generic parameter on TestClass
        mb.AddGenericParameter(testClassHandle, GenericParameterAttributes.None, mb.GetOrAddString("T"), 0);

        // Serialize
        BlobBuilder metadataBlob = new();
        MetadataRootBuilder root = new(mb);
        root.Serialize(metadataBlob, 0, 0);
        byte[] bytes = metadataBlob.ToArray();

        // Create provider and reader
        MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(bytes));
        return provider.GetMetadataReader();
    }

    private static MetadataImportWrapper CreateWrapper()
    {
        MetadataReader reader = CreateTestMetadata();
        return new MetadataImportWrapper(reader);
    }

    [Fact]
    public void EnumTypeDefs_ReturnsAllTypes()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        nint hEnum = 0;
        uint* tokens = stackalloc uint[10];
        uint count;
        int hr = wrapper.EnumTypeDefs(&hEnum, tokens, 10, &count);

        Assert.Equal(HResults.S_OK, hr);
        Assert.True(count >= 3); // <Module>, TestClass, NestedType

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void EnumTypeDefs_Pagination()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        nint hEnum = 0;
        uint token;
        uint count;

        // Get one at a time
        int hr = wrapper.EnumTypeDefs(&hEnum, &token, 1, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(1u, count);

        hr = wrapper.EnumTypeDefs(&hEnum, &token, 1, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(1u, count);

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void CountEnum_ReturnsCorrectCount()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        nint hEnum = 0;
        uint* tokens = stackalloc uint[10];
        uint count;
        wrapper.EnumTypeDefs(&hEnum, tokens, 10, &count);

        uint enumCount;
        int hr = wrapper.CountEnum(hEnum, &enumCount);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(count, enumCount);

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void ResetEnum_ResetsPosition()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        nint hEnum = 0;
        uint firstToken;
        uint count;
        wrapper.EnumTypeDefs(&hEnum, &firstToken, 1, &count);

        // Advance past first
        uint secondToken;
        wrapper.EnumTypeDefs(&hEnum, &secondToken, 1, &count);

        // Reset to 0
        wrapper.ResetEnum(hEnum, 0);

        // Should get first token again
        uint resetToken;
        wrapper.EnumTypeDefs(&hEnum, &resetToken, 1, &count);
        Assert.Equal(firstToken, resetToken);

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void GetTypeDefProps_ReturnsNameAndFlags()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        // Find TestClass token (should be row 2 = 0x02000002)
        uint testClassToken = 0x02000002;
        char* nameBuf = stackalloc char[256];
        uint nameLen;
        uint flags;
        uint extends;

        int hr = wrapper.GetTypeDefProps(testClassToken, nameBuf, 256, &nameLen, &flags, &extends);
        Assert.Equal(HResults.S_OK, hr);

        string name = new string(nameBuf, 0, (int)nameLen - 1); // minus null terminator
        Assert.Equal("TestNamespace.TestClass", name);
        Assert.True((flags & (uint)TypeAttributes.Public) != 0);
        Assert.NotEqual(0u, extends); // Should have a base type (Object)
    }

    [Fact]
    public void GetTypeRefProps_ReturnsName()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        // System.Object TypeRef should be row 1 = 0x01000001
        uint objectRefToken = 0x01000001;
        char* nameBuf = stackalloc char[256];
        uint nameLen;
        uint scope;

        int hr = wrapper.GetTypeRefProps(objectRefToken, &scope, nameBuf, 256, &nameLen);
        Assert.Equal(HResults.S_OK, hr);

        string name = new string(nameBuf, 0, (int)nameLen - 1);
        Assert.Equal("System.Object", name);
    }

    [Fact]
    public void GetMethodProps_ReturnsNameAndSignature()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        // DoWork should be MethodDef row 1 = 0x06000001
        uint methodToken = 0x06000001;
        uint parentClass;
        char* nameBuf = stackalloc char[256];
        uint nameLen;
        uint attrs;
        byte* sigBlob;
        uint sigLen;
        uint rva;
        uint implFlags;

        int hr = wrapper.GetMethodProps(methodToken, &parentClass, nameBuf, 256, &nameLen,
            &attrs, &sigBlob, &sigLen, &rva, &implFlags);
        Assert.Equal(HResults.S_OK, hr);

        string name = new string(nameBuf, 0, (int)nameLen - 1);
        Assert.Equal("DoWork", name);
        Assert.True(sigLen > 0);
        Assert.True(sigBlob is not null);
    }

    [Fact]
    public void GetFieldProps_ReturnsNameAndSignature()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        // _value should be FieldDef row 1 = 0x04000001
        uint fieldToken = 0x04000001;
        uint parentClass;
        char* nameBuf = stackalloc char[256];
        uint nameLen;
        uint attrs;
        byte* sigBlob;
        uint sigLen;

        int hr = wrapper.GetFieldProps(fieldToken, &parentClass, nameBuf, 256, &nameLen,
            &attrs, &sigBlob, &sigLen, null, null, null);
        Assert.Equal(HResults.S_OK, hr);

        string name = new string(nameBuf, 0, (int)nameLen - 1);
        Assert.Equal("_value", name);
        Assert.True(sigLen > 0);
    }

    [Fact]
    public void GetMemberProps_DispatchesToMethodOrField()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        uint parentClass;
        char* nameBuf = stackalloc char[256];
        uint nameLen;

        // Method token
        int hr = wrapper.GetMemberProps(0x06000001, &parentClass, nameBuf, 256, &nameLen,
            null, null, null, null, null, null, null, null);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal("DoWork", new string(nameBuf, 0, (int)nameLen - 1));

        // Field token
        hr = wrapper.GetMemberProps(0x04000001, &parentClass, nameBuf, 256, &nameLen,
            null, null, null, null, null, null, null, null);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal("_value", new string(nameBuf, 0, (int)nameLen - 1));

        // Invalid table
        hr = wrapper.GetMemberProps(0xFF000001, null, null, 0, null,
            null, null, null, null, null, null, null, null);
        Assert.Equal(HResults.E_INVALIDARG, hr);
    }

    [Fact]
    public void EnumMethods_ReturnsMethodsForType()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        nint hEnum = 0;
        uint* tokens = stackalloc uint[10];
        uint count;
        int hr = wrapper.EnumMethods(&hEnum, 0x02000002, tokens, 10, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.True(count >= 1);
        Assert.Equal(0x06000001u, tokens[0]); // DoWork

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void EnumFields_ReturnsFieldsForType()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        nint hEnum = 0;
        uint* tokens = stackalloc uint[10];
        uint count;
        int hr = wrapper.EnumFields(&hEnum, 0x02000002, tokens, 10, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.True(count >= 1);
        Assert.Equal(0x04000001u, tokens[0]); // _value

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void EnumInterfaceImpls_ReturnsImplementations()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        nint hEnum = 0;
        uint* tokens = stackalloc uint[10];
        uint count;
        int hr = wrapper.EnumInterfaceImpls(&hEnum, 0x02000002, tokens, 10, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(1u, count);

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void GetInterfaceImplProps_ReturnsClassAndInterface()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        // Get the interface impl token first
        nint hEnum = 0;
        uint implToken;
        uint count;
        wrapper.EnumInterfaceImpls(&hEnum, 0x02000002, &implToken, 1, &count);
        wrapper.CloseEnum(hEnum);

        uint parentClass;
        uint interfaceToken;
        int hr = wrapper.GetInterfaceImplProps(implToken, &parentClass, &interfaceToken);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0x02000002u, parentClass); // TestClass
        Assert.NotEqual(0u, interfaceToken);
    }

    [Fact]
    public void GetNestedClassProps_ReturnsEnclosingClass()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        // NestedType is TypeDef row 3 = 0x02000003
        uint enclosingClass;
        int hr = wrapper.GetNestedClassProps(0x02000003, &enclosingClass);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0x02000002u, enclosingClass); // TestClass
    }

    [Fact]
    public void GetNestedClassProps_NonNestedReturnsRecordNotFound()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        // TestClass (row 2) is not nested
        uint enclosingClass;
        int hr = wrapper.GetNestedClassProps(0x02000002, &enclosingClass);
        Assert.True(hr < 0); // CLDB_E_RECORD_NOTFOUND
    }

    [Fact]
    public void EnumGenericParams_ReturnsParams()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        nint hEnum = 0;
        uint* tokens = stackalloc uint[10];
        uint count;
        int hr = wrapper.EnumGenericParams(&hEnum, 0x02000002, tokens, 10, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(1u, count);

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void GetGenericParamProps_ReturnsNameAndOwner()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        // Get generic param token
        nint hEnum = 0;
        uint gpToken;
        uint count;
        wrapper.EnumGenericParams(&hEnum, 0x02000002, &gpToken, 1, &count);
        wrapper.CloseEnum(hEnum);

        uint seq;
        uint flags;
        uint owner;
        char* nameBuf = stackalloc char[256];
        uint nameLen;

        int hr = wrapper.GetGenericParamProps(gpToken, &seq, &flags, &owner, null, nameBuf, 256, &nameLen);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0u, seq);
        Assert.Equal(0x02000002u, owner); // TestClass
        Assert.Equal("T", new string(nameBuf, 0, (int)nameLen - 1));
    }

    [Fact]
    public void IsValidToken_ValidAndInvalid()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        // Valid TypeDef token
        Assert.Equal(1, wrapper.IsValidToken(0x02000001));
        Assert.Equal(1, wrapper.IsValidToken(0x02000002));

        // Invalid - RID 0
        Assert.Equal(0, wrapper.IsValidToken(0x02000000));

        // Invalid - RID too high
        Assert.Equal(0, wrapper.IsValidToken(0x020000FF));
    }

    [Fact]
    public void EnumTypeRefs_ReturnsTypeRefs()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        nint hEnum = 0;
        uint* tokens = stackalloc uint[10];
        uint count;
        int hr = wrapper.EnumTypeRefs(&hEnum, tokens, 10, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.True(count >= 2); // System.Object and System.IDisposable

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void InvalidToken_ReturnsError()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        // Invalid TypeDef token (way out of range)
        char* nameBuf = stackalloc char[256];
        uint nameLen;
        int hr = wrapper.GetTypeDefProps(0x020000FF, nameBuf, 256, &nameLen, null, null);
        Assert.True(hr < 0); // Should return an error HRESULT
    }

    [Fact]
    public void NotImplementedMethods_ReturnENotImpl()
    {
        MetadataImportWrapper wrapper = CreateWrapper();

        Assert.Equal(HResults.E_NOTIMPL, wrapper.FindTypeDefByName(null, 0, null));
        Assert.Equal(HResults.E_NOTIMPL, wrapper.GetScopeProps(null, 0, null, null));
        Assert.Equal(HResults.E_NOTIMPL, wrapper.ResolveTypeRef(0, null, null, null));
    }
}
