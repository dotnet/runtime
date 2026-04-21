// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class MetaDataImportImplTests
{
    // Build a minimal assembly metadata with types, methods, and fields.
    private static (MetadataReader reader, MetadataReaderProvider provider) CreateTestMetadata()
    {
        MetadataBuilder mb = new();

        // Module
        mb.AddModule(0, mb.GetOrAddString("TestModule"), mb.GetOrAddGuid(Guid.NewGuid()), default, default);

        // Assembly with a public key blob (to test afPublicKey flag OR)
        byte[] publicKey = [0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00];
        mb.AddAssembly(mb.GetOrAddString("TestAssembly"), new Version(1, 0, 0, 0),
            default, mb.GetOrAddBlob(publicKey), AssemblyFlags.PublicKey, AssemblyHashAlgorithm.Sha1);

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

        // Create a field signature blob: string
        BlobBuilder stringFieldSigBlob = new();
        new BlobEncoder(stringFieldSigBlob).Field().Type().String();
        BlobHandle stringFieldSig = mb.GetOrAddBlob(stringFieldSigBlob);

        // TypeDef: <Module> (required, row 1) — owns the global method (method 1) and no fields
        mb.AddTypeDefinition(default, default, mb.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        // Global method on <Module>: GlobalHelper (void) — method row 1
        mb.AddMethodDefinition(MethodAttributes.Static | MethodAttributes.Public, MethodImplAttributes.IL,
            mb.GetOrAddString("GlobalHelper"), voidMethodSig,
            -1, MetadataTokens.ParameterHandle(1));

        // TypeDef: TestNamespace.TestClass (row 2) — owns field 1+, method 2+
        TypeDefinitionHandle testClassHandle = mb.AddTypeDefinition(
            TypeAttributes.Public | TypeAttributes.Class,
            mb.GetOrAddString("TestNamespace"),
            mb.GetOrAddString("TestClass"),
            objectRef,
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(2));

        // FieldDef: _value (int) — field row 1, no constant
        mb.AddFieldDefinition(FieldAttributes.Private, mb.GetOrAddString("_value"), intFieldSig);

        // FieldDef: StringConst (string) — field row 2, with string constant
        FieldDefinitionHandle stringConstField = mb.AddFieldDefinition(
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault,
            mb.GetOrAddString("StringConst"), stringFieldSig);

        // Add a string constant value for StringConst — MetadataBuilder encodes as UTF-16LE
        mb.AddConstant(stringConstField, "test");

        // MethodDef: DoWork (void) with no parameters — method row 2
        mb.AddMethodDefinition(MethodAttributes.Public, MethodImplAttributes.IL,
            mb.GetOrAddString("DoWork"), voidMethodSig,
            -1, MetadataTokens.ParameterHandle(1));

        // Parameter: "arg0" at sequence 1 (associated with DoWork for parameter enumeration testing)
        mb.AddParameter(ParameterAttributes.None, mb.GetOrAddString("arg0"), 1);

        // Interface implementation: TestClass : IDisposable
        mb.AddInterfaceImplementation(testClassHandle, disposableRef);

        // TypeDef: TestNamespace.TestClass+NestedType (row 3)
        TypeDefinitionHandle nestedHandle = mb.AddTypeDefinition(
            TypeAttributes.NestedPublic | TypeAttributes.Class,
            default,
            mb.GetOrAddString("NestedType"),
            objectRef,
            MetadataTokens.FieldDefinitionHandle(3),
            MetadataTokens.MethodDefinitionHandle(3));

        // Nested class relationship
        mb.AddNestedType(nestedHandle, testClassHandle);

        // Generic parameter on TestClass
        mb.AddGenericParameter(testClassHandle, GenericParameterAttributes.None, mb.GetOrAddString("T"), 0);

        // MemberRef: Object.ToString() on objectRef
        BlobBuilder memberRefSig = new();
        new BlobEncoder(memberRefSig).MethodSignature().Parameters(0, returnType => returnType.Type().String(), parameters => { });
        mb.AddMemberReference(objectRef, mb.GetOrAddString("ToString"), mb.GetOrAddBlob(memberRefSig));

        // ModuleRef: "NativeLib"
        mb.AddModuleReference(mb.GetOrAddString("NativeLib"));

        // TypeSpec: a simple type spec (int[])
        BlobBuilder typeSpecSig = new();
        new BlobEncoder(typeSpecSig).TypeSpecificationSignature().SZArray().Int32();
        mb.AddTypeSpecification(mb.GetOrAddBlob(typeSpecSig));

        // UserString: "Hello, World!"
        _ = mb.GetOrAddUserString("Hello, World!");

        // TypeDef with explicit layout for GetClassLayout testing (row 4)
        mb.AddTypeDefinition(
            TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Class,
            mb.GetOrAddString("TestNamespace"),
            mb.GetOrAddString("LayoutClass"),
            objectRef,
            MetadataTokens.FieldDefinitionHandle(3),
            MetadataTokens.MethodDefinitionHandle(3));
        mb.AddTypeLayout(MetadataTokens.TypeDefinitionHandle(4), 8, 32);

        // Custom attribute on TestClass: [System.ObsoleteAttribute("test message")]
        MemberReferenceHandle obsoleteCtor = mb.AddMemberReference(
            mb.AddTypeReference(mscorlibRef, mb.GetOrAddString("System"), mb.GetOrAddString("ObsoleteAttribute")),
            mb.GetOrAddString(".ctor"),
            mb.GetOrAddBlob(new byte[] { 0x20, 0x01, 0x0E, 0x00 })); // instance void(string)
        mb.AddCustomAttribute(testClassHandle, obsoleteCtor,
            mb.GetOrAddBlob(new byte[] { 0x01, 0x00, 0x0C, 0x74, 0x65, 0x73, 0x74, 0x20, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00, 0x00 }));

        // Serialize
        BlobBuilder metadataBlob = new();
        MetadataRootBuilder root = new(mb);
        root.Serialize(metadataBlob, 0, 0);
        byte[] bytes = metadataBlob.ToArray();

        // Create provider and reader — provider must stay alive as long as the reader is used
        MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(bytes));
        return (provider.GetMetadataReader(), provider);
    }

    // Provider is stored alongside wrapper to prevent GC from collecting pinned metadata memory.
    private static MetadataReaderProvider? _testProvider;

    private static IMetaDataImport2 CreateWrapper()
    {
        (MetadataReader reader, MetadataReaderProvider provider) = CreateTestMetadata();
        _testProvider = provider;
        return new MetaDataImportImpl(reader);
    }

    [Fact]
    public void EnumFields_Pagination()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        nint hEnum = 0;
        uint token;
        uint count;

        int hr = wrapper.EnumFields(&hEnum, 0x02000002, &token, 1, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(1u, count);
        Assert.Equal(0x04000001u, token);

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void GetTypeDefProps_ReturnsNameAndFlags()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

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
        IMetaDataImport2 wrapper = CreateWrapper();

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
        IMetaDataImport2 wrapper = CreateWrapper();

        // DoWork should be MethodDef row 2 = 0x06000002
        uint methodToken = 0x06000002;
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
        IMetaDataImport2 wrapper = CreateWrapper();

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
        IMetaDataImport2 wrapper = CreateWrapper();

        uint parentClass;
        char* nameBuf = stackalloc char[256];
        uint nameLen;

        // Method token (DoWork = 0x06000002)
        int hr = wrapper.GetMemberProps(0x06000002, &parentClass, nameBuf, 256, &nameLen,
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
    public void EnumFields_ReturnsFieldsForType()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        nint hEnum = 0;
        uint* tokens = stackalloc uint[10];
        uint count;
        int hr = wrapper.EnumFields(&hEnum, 0x02000002, tokens, 10, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.True(count >= 1);
        Assert.Contains(0x04000001u, new ReadOnlySpan<uint>(tokens, (int)count).ToArray()); // _value

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void EnumInterfaceImpls_ReturnsImplementations()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

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
        IMetaDataImport2 wrapper = CreateWrapper();

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
        IMetaDataImport2 wrapper = CreateWrapper();

        // NestedType is TypeDef row 3 = 0x02000003
        uint enclosingClass;
        int hr = wrapper.GetNestedClassProps(0x02000003, &enclosingClass);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0x02000002u, enclosingClass); // TestClass
    }

    [Fact]
    public void GetNestedClassProps_NonNestedReturnsRecordNotFound()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        // TestClass (row 2) is not nested
        uint enclosingClass;
        int hr = wrapper.GetNestedClassProps(0x02000002, &enclosingClass);
        Assert.True(hr < 0); // CLDB_E_RECORD_NOTFOUND
    }

    [Fact]
    public void EnumGenericParams_ReturnsParams()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

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
        IMetaDataImport2 wrapper = CreateWrapper();

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
        IMetaDataImport2 wrapper = CreateWrapper();

        // Valid TypeDef token
        Assert.Equal(1, wrapper.IsValidToken(0x02000001));
        Assert.Equal(1, wrapper.IsValidToken(0x02000002));

        // Invalid - RID 0
        Assert.Equal(0, wrapper.IsValidToken(0x02000000));

        // Invalid - RID too high
        Assert.Equal(0, wrapper.IsValidToken(0x020000FF));
    }

    [Fact]
    public void InvalidToken_ReturnsError()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        // Invalid TypeDef token (way out of range)
        char* nameBuf = stackalloc char[256];
        uint nameLen;
        int hr = wrapper.GetTypeDefProps(0x020000FF, nameBuf, 256, &nameLen, null, null);
        Assert.True(hr < 0); // Should return an error HRESULT
    }

    [Fact]
    public void NotImplementedMethods_ReturnENotImpl()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        Assert.Equal(HResults.E_NOTIMPL, wrapper.GetScopeProps(null, 0, null, null));
        Assert.Equal(HResults.E_NOTIMPL, wrapper.ResolveTypeRef(0, null, null, null));
        Assert.Equal(HResults.E_NOTIMPL, wrapper.EnumTypeDefs(null, null, 0, null));
        Assert.Equal(HResults.E_NOTIMPL, wrapper.EnumTypeRefs(null, null, 0, null));
    }

    [Fact]
    public void FindTypeDefByName_FindsType()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint td;
        fixed (char* name = "TestNamespace.TestClass")
        {
            int hr = wrapper.FindTypeDefByName(name, 0, &td);
            Assert.Equal(HResults.S_OK, hr);
            Assert.Equal(0x02000002u, td);
        }
    }

    [Fact]
    public void FindTypeDefByName_NotFound()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint td;
        fixed (char* name = "DoesNotExist")
        {
            int hr = wrapper.FindTypeDefByName(name, 0, &td);
            Assert.True(hr < 0); // CLDB_E_RECORD_NOTFOUND
        }
    }

    [Fact]
    public void GetMemberRefProps_ReturnsNameAndParent()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint memberRefToken = 0x0A000001; // MemberRef row 1
        uint parentToken;
        char* nameBuf = stackalloc char[256];
        uint nameLen;
        byte* sigBlob;
        uint sigLen;

        int hr = wrapper.GetMemberRefProps(memberRefToken, &parentToken, nameBuf, 256, &nameLen, &sigBlob, &sigLen);
        Assert.Equal(HResults.S_OK, hr);

        string name = new string(nameBuf, 0, (int)nameLen - 1);
        Assert.Equal("ToString", name);
        Assert.NotEqual(0u, parentToken);
        Assert.True(sigLen > 0);
    }

    [Fact]
    public void GetModuleRefProps_ReturnsName()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint moduleRefToken = 0x1A000001; // ModuleRef row 1
        char* nameBuf = stackalloc char[256];
        uint nameLen;

        int hr = wrapper.GetModuleRefProps(moduleRefToken, nameBuf, 256, &nameLen);
        Assert.Equal(HResults.S_OK, hr);

        string name = new string(nameBuf, 0, (int)nameLen - 1);
        Assert.Equal("NativeLib", name);
    }

    [Fact]
    public void GetTypeSpecFromToken_ReturnsSig()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint typeSpecToken = 0x1B000001; // TypeSpec row 1
        byte* sigBlob;
        uint sigLen;

        int hr = wrapper.GetTypeSpecFromToken(typeSpecToken, &sigBlob, &sigLen);
        Assert.Equal(HResults.S_OK, hr);
        Assert.True(sigLen > 0);
        Assert.True(sigBlob is not null);
    }

    [Fact]
    public void GetUserString_ReturnsString()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint userStringToken = 0x70000001; // UserString heap offset 1
        char* strBuf = stackalloc char[256];
        uint strLen;

        int hr = wrapper.GetUserString(userStringToken, strBuf, 256, &strLen);
        Assert.Equal(HResults.S_OK, hr);

        string value = new string(strBuf, 0, (int)strLen);
        Assert.Equal("Hello, World!", value);
    }

    [Fact]
    public void GetParamProps_ReturnsNameAndSequence()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint paramToken = 0x08000001; // Param row 1
        uint parentMethod;
        uint sequence;
        char* nameBuf = stackalloc char[256];
        uint nameLen;
        uint attrs;

        int hr = wrapper.GetParamProps(paramToken, &parentMethod, &sequence, nameBuf, 256, &nameLen,
            &attrs, null, null, null);
        Assert.Equal(HResults.S_OK, hr);

        string name = new string(nameBuf, 0, (int)nameLen - 1);
        Assert.Equal("arg0", name);
        Assert.Equal(1u, sequence);
        Assert.Equal(0x06000002u, parentMethod); // DoWork
    }

    [Fact]
    public void GetParamForMethodIndex_FindsParam()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint paramToken;
        int hr = wrapper.GetParamForMethodIndex(0x06000002, 1, &paramToken);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0x08000001u, paramToken);
    }

    [Fact]
    public void GetParamForMethodIndex_NotFound()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint paramToken;
        int hr = wrapper.GetParamForMethodIndex(0x06000002, 99, &paramToken);
        Assert.True(hr < 0); // CLDB_E_RECORD_NOTFOUND
    }

    [Fact]
    public void GetClassLayout_ReturnsLayout()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint packSize;
        uint classSize;
        int hr = wrapper.GetClassLayout(0x02000004, &packSize, null, 0, null, &classSize);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(8u, packSize);
        Assert.Equal(32u, classSize);
    }

    [Fact]
    public void GetClassLayout_NoLayout_ReturnsRecordNotFound()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint packSize;
        uint classSize;
        int hr = wrapper.GetClassLayout(0x02000002, &packSize, null, 0, null, &classSize);
        Assert.True(hr < 0); // CLDB_E_RECORD_NOTFOUND
    }

    [Fact]
    public void ReaderOnly_MethodsWork()
    {
        // When only reader is available (no legacy), implemented methods should still work
        (MetadataReader reader, MetadataReaderProvider provider) = CreateTestMetadata();
        _testProvider = provider;
        IMetaDataImport2 wrapper = new MetaDataImportImpl(reader, legacyImport: null);

        uint flags;
        char* nameBuf = stackalloc char[256];
        uint nameLen;

        int hr = wrapper.GetTypeDefProps(0x02000002, nameBuf, 256, &nameLen, &flags, null);
        Assert.Equal(HResults.S_OK, hr);

        string name = new string(nameBuf, 0, (int)nameLen - 1);
        Assert.Equal("TestNamespace.TestClass", name);
    }

    [Fact]
    public void GetRVA_MethodDef_ReturnsRVA()
    {
        (MetadataReader reader, MetadataReaderProvider provider) = CreateTestMetadata();
        _testProvider = provider;
        IMetaDataImport2 wrapper = new MetaDataImportImpl(reader, legacyImport: null);

        uint rva, implFlags;
        // DoWork is MethodDef token 0x06000002
        int hr = wrapper.GetRVA(0x06000002, &rva, &implFlags);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal((uint)MethodImplAttributes.IL, implFlags);
    }

    [Fact]
    public void GetRVA_InvalidTable_ReturnsEInvalidArg()
    {
        (MetadataReader reader, MetadataReaderProvider provider) = CreateTestMetadata();
        _testProvider = provider;
        IMetaDataImport2 wrapper = new MetaDataImportImpl(reader, legacyImport: null);

        uint rva;
        // TypeDef token (0x02) is not MethodDef or FieldDef
        int hr = wrapper.GetRVA(0x02000001, &rva, null);
        Assert.Equal(HResults.E_INVALIDARG, hr);
    }

    [Fact]
    public void GetCustomAttributeByName_Found_ReturnsSok()
    {
        (MetadataReader reader, MetadataReaderProvider provider) = CreateTestMetadata();
        _testProvider = provider;
        IMetaDataImport2 wrapper = new MetaDataImportImpl(reader, legacyImport: null);

        void* pData;
        uint cbData;
        fixed (char* attrName = "System.ObsoleteAttribute")
        {
            // TestClass (0x02000002) has [Obsolete("test message")]
            int hr = wrapper.GetCustomAttributeByName(0x02000002, attrName, &pData, &cbData);
            Assert.Equal(HResults.S_OK, hr);
            Assert.True(pData is not null);
            Assert.True(cbData > 0);
        }
    }

    [Fact]
    public void GetCustomAttributeByName_NotFound_ReturnsSFalse()
    {
        (MetadataReader reader, MetadataReaderProvider provider) = CreateTestMetadata();
        _testProvider = provider;
        IMetaDataImport2 wrapper = new MetaDataImportImpl(reader, legacyImport: null);

        void* pData;
        uint cbData;
        fixed (char* attrName = "System.NonExistentAttribute")
        {
            int hr = wrapper.GetCustomAttributeByName(0x02000002, attrName, &pData, &cbData);
            Assert.Equal(HResults.S_FALSE, hr);
            Assert.True(pData is null);
            Assert.Equal(0u, cbData);
        }
    }

    [Fact]
    public void GetAssemblyFromScope_ReturnsAssemblyToken()
    {
        (MetadataReader reader, MetadataReaderProvider provider) = CreateTestMetadata();
        _testProvider = provider;
        IMetaDataImport2 wrapper = new MetaDataImportImpl(reader, legacyImport: null);
        IMetaDataAssemblyImport assemblyImport = (IMetaDataAssemblyImport)wrapper;

        uint tkAssembly;
        int hr = assemblyImport.GetAssemblyFromScope(&tkAssembly);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0x20000001u, tkAssembly);
    }

    [Fact]
    public void GetAssemblyProps_ReturnsNameAndVersion()
    {
        (MetadataReader reader, MetadataReaderProvider provider) = CreateTestMetadata();
        _testProvider = provider;
        IMetaDataImport2 wrapper = new MetaDataImportImpl(reader, legacyImport: null);
        IMetaDataAssemblyImport assemblyImport = (IMetaDataAssemblyImport)wrapper;

        char* nameBuf = stackalloc char[256];
        uint nameLen;
        ASSEMBLYMETADATA metadata = default;
        uint flags;

        int hr = assemblyImport.GetAssemblyProps(0x20000001, null, null, null, nameBuf, 256, &nameLen, &metadata, &flags);
        Assert.Equal(HResults.S_OK, hr);

        string name = new string(nameBuf, 0, (int)nameLen - 1);
        Assert.Equal("TestAssembly", name);
        Assert.Equal(1, metadata.usMajorVersion);
        Assert.Equal(0, metadata.usMinorVersion);
        Assert.Equal(0, metadata.usBuildNumber);
        Assert.Equal(0, metadata.usRevisionNumber);
        Assert.Equal((uint)AssemblyFlags.PublicKey, flags);
    }

    [Fact]
    public void GetAssemblyRefProps_ReturnsRefNameAndVersion()
    {
        (MetadataReader reader, MetadataReaderProvider provider) = CreateTestMetadata();
        _testProvider = provider;
        IMetaDataImport2 wrapper = new MetaDataImportImpl(reader, legacyImport: null);
        IMetaDataAssemblyImport assemblyImport = (IMetaDataAssemblyImport)wrapper;

        // mscorlib assembly ref is token 0x23000001
        char* nameBuf = stackalloc char[256];
        uint nameLen;
        ASSEMBLYMETADATA metadata = default;
        uint flags;

        int hr = assemblyImport.GetAssemblyRefProps(0x23000001, null, null, nameBuf, 256, &nameLen, &metadata, null, null, &flags);
        Assert.Equal(HResults.S_OK, hr);

        string name = new string(nameBuf, 0, (int)nameLen - 1);
        Assert.Equal("mscorlib", name);
        Assert.Equal(4, metadata.usMajorVersion);
        Assert.Equal(0, metadata.usMinorVersion);
        Assert.Equal(0, metadata.usBuildNumber);
        Assert.Equal(0, metadata.usRevisionNumber);
    }

    [Fact]
    public void GetAssemblyProps_SmallBuffer_Truncates()
    {
        (MetadataReader reader, MetadataReaderProvider provider) = CreateTestMetadata();
        _testProvider = provider;
        IMetaDataImport2 wrapper = new MetaDataImportImpl(reader, legacyImport: null);
        IMetaDataAssemblyImport assemblyImport = (IMetaDataAssemblyImport)wrapper;

        char* nameBuf = stackalloc char[5];
        uint nameLen;

        int hr = assemblyImport.GetAssemblyProps(0x20000001, null, null, null, nameBuf, 5, &nameLen, null, null);
        Assert.Equal(0x00131106, hr); // CLDB_S_TRUNCATION
        // Full name is "TestAssembly" (12 chars + null = 13)
        Assert.Equal(13u, nameLen);
        // Buffer should contain "Test\0"
        string truncated = new string(nameBuf, 0, 4);
        Assert.Equal("Test", truncated);
    }

    [Fact]
    public void GetAssemblyProps_InvalidToken_ReturnsRecordNotFound()
    {
        var builder = new MetadataBuilder();
        builder.AddModule(0, builder.GetOrAddString("TestModule"), builder.GetOrAddGuid(Guid.NewGuid()), default, default);
        builder.AddAssembly(
            builder.GetOrAddString("TestAssembly"),
            new Version(1, 0, 0, 0),
            default,
            default,
            default,
            default);

        var metadataBuilder = new BlobBuilder();
        new MetadataRootBuilder(builder).Serialize(metadataBuilder, 0, 0);
        var metadata = metadataBuilder.ToImmutableArray();

        fixed (byte* ptr = metadata.AsSpan())
        {
            var reader = new MetadataReader(ptr, metadata.Length);
            IMetaDataImport2 impl = new MetaDataImportImpl(reader);
            var assemblyImport = (IMetaDataAssemblyImport)impl;

            // Pass an invalid assembly token (wrong RID)
            int hr = assemblyImport.GetAssemblyProps(0x20000002, null, null, null, null, 0, null, null, null);
            Assert.Equal(unchecked((int)0x80131130), hr); // CLDB_E_RECORD_NOTFOUND
        }
    }

    [Fact]
    public void GetFieldProps_NoConstant_ReturnsElementTypeVoid()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        // _value (field row 1) has no constant
        uint fieldToken = 0x04000001;
        uint cplusTypeFlag;
        void* pValue;
        uint cchValue;

        int hr = wrapper.GetFieldProps(fieldToken, null, null, 0, null, null, null, null, &cplusTypeFlag, &pValue, &cchValue);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(1u, cplusTypeFlag); // ELEMENT_TYPE_VOID
        Assert.True(pValue is null);
        Assert.Equal(0u, cchValue);
    }

    [Fact]
    public void GetFieldProps_StringConstant_ReturnsCharCount()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        // StringConst (field row 2) has a string constant "test"
        uint fieldToken = 0x04000002;
        uint cplusTypeFlag;
        void* pValue;
        uint cchValue;

        int hr = wrapper.GetFieldProps(fieldToken, null, null, 0, null, null, null, null, &cplusTypeFlag, &pValue, &cchValue);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0x0Eu, cplusTypeFlag); // ELEMENT_TYPE_STRING
        Assert.True(pValue is not null);
        Assert.Equal(4u, cchValue); // "test" = 4 characters, not 8 bytes
    }

    [Fact]
    public void GetMethodProps_GlobalMethod_ReturnsMdTypeDefNil()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        // GlobalHelper (method row 1) is on <Module> — parent should be mdTypeDefNil (0)
        uint methodToken = 0x06000001;
        uint parentClass;

        int hr = wrapper.GetMethodProps(methodToken, &parentClass, null, 0, null, null, null, null, null, null);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0u, parentClass); // mdTypeDefNil, not 0x02000001
    }

    [Fact]
    public void GetMethodProps_NonGlobalMethod_ReturnsParentClass()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        // DoWork (method row 2) is on TestClass — parent should be TestClass token
        uint methodToken = 0x06000002;
        uint parentClass;

        int hr = wrapper.GetMethodProps(methodToken, &parentClass, null, 0, null, null, null, null, null, null);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0x02000002u, parentClass); // TestClass
    }

    [Fact]
    public void GetFieldProps_GlobalField_ReturnsMdTypeDefNil()
    {
        // Create metadata with a global field on <Module>
        MetadataBuilder mb = new();
        mb.AddModule(0, mb.GetOrAddString("Mod"), mb.GetOrAddGuid(Guid.NewGuid()), default, default);
        mb.AddAssembly(mb.GetOrAddString("Asm"), new Version(1, 0, 0, 0), default, default, default, default);

        BlobBuilder fieldSig = new();
        new BlobEncoder(fieldSig).Field().Type().Int32();
        BlobHandle intFieldSig = mb.GetOrAddBlob(fieldSig);

        // <Module> owns field 1
        mb.AddTypeDefinition(default, default, mb.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        mb.AddFieldDefinition(FieldAttributes.Static | FieldAttributes.Public, mb.GetOrAddString("GlobalField"), intFieldSig);

        BlobBuilder blob = new();
        new MetadataRootBuilder(mb).Serialize(blob, 0, 0);
        var bytes = blob.ToImmutableArray();

        fixed (byte* ptr = bytes.AsSpan())
        {
            var reader = new MetadataReader(ptr, bytes.Length);
            IMetaDataImport2 impl = new MetaDataImportImpl(reader);

            uint parentClass;
            int hr = impl.GetFieldProps(0x04000001, &parentClass, null, 0, null, null, null, null, null, null, null);
            Assert.Equal(HResults.S_OK, hr);
            Assert.Equal(0u, parentClass); // mdTypeDefNil
        }
    }

    [Fact]
    public void GetUserString_ReturnsCharCountWithoutNull()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint userStringToken = 0x70000001;
        uint pchString;

        // Query length only (no buffer)
        int hr = wrapper.GetUserString(userStringToken, null, 0, &pchString);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(13u, pchString); // "Hello, World!" = 13 chars, NO null terminator
    }

    [Fact]
    public void GetAssemblyProps_IncludesAfPublicKeyFlag()
    {
        IMetaDataImport2 wrapper = CreateWrapper();
        var assemblyImport = (IMetaDataAssemblyImport)wrapper;

        uint flags;
        byte* pubKey;
        uint pubKeyLen;

        int hr = assemblyImport.GetAssemblyProps(0x20000001, &pubKey, &pubKeyLen, null, null, 0, null, null, &flags);
        Assert.Equal(HResults.S_OK, hr);
        Assert.True(pubKeyLen > 0);
        Assert.True((flags & 0x0001) != 0, "afPublicKey flag should be set when public key blob is non-empty");
    }

    [Fact]
    public void GetParamProps_NoConstant_ReturnsElementTypeVoid()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        // arg0 (param row 1) has no constant
        uint paramToken = 0x08000001;
        uint cplusTypeFlag;
        void* pValue;
        uint cchValue;

        int hr = wrapper.GetParamProps(paramToken, null, null, null, 0, null, null, &cplusTypeFlag, &pValue, &cchValue);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(1u, cplusTypeFlag); // ELEMENT_TYPE_VOID
        Assert.True(pValue is null);
        Assert.Equal(0u, cchValue);
    }

    // Regression test: ClrMD QIs for IMetaDataImport but accesses IMetaDataImport2 vtable
    // slots beyond the IMetaDataImport boundary (EnumGenericParams is slot 65, past the
    // 65-slot IMetaDataImport vtable). With [GeneratedComInterface] CCWs, each interface
    // gets its own vtable. The ICustomQueryInterface on MetaDataImportImpl must redirect
    // IMetaDataImport QIs to IMetaDataImport2 so all 73 slots are accessible.
    [Fact]
    public void QueryInterfaceForIMetaDataImport_ReturnsIMetaDataImport2VtableWithExtendedSlots()
    {
        var (reader, provider) = CreateTestMetadata();
        using var _ = provider;

        IMetaDataImport2 wrapper = new MetaDataImportImpl(reader);

        nint pUnk = (nint)ComInterfaceMarshaller<IMetaDataImport2>.ConvertToUnmanaged(wrapper);

        try
        {
            // Simulate ClrMD: QI for IMetaDataImport
            Guid iidImport = typeof(IMetaDataImport).GUID;
            int hr = Marshal.QueryInterface(pUnk, in iidImport, out nint pImport);
            Assert.Equal(0, hr);
            Assert.NotEqual(nint.Zero, pImport);

            try
            {
                // Simulate ClrMD's CallableCOMWrapper: QI the returned pointer AGAIN for IMetaDataImport.
                // This second QI goes to MetaDataImportImpl's CCW. Without the ICustomQueryInterface
                // redirect, this would return the shorter IMetaDataImport vtable (65 slots) and accessing
                // slot 65 (EnumGenericParams) would AV.
                Guid iidImportAgain = typeof(IMetaDataImport).GUID;
                hr = Marshal.QueryInterface(pImport, in iidImportAgain, out nint pImportAgain);
                Assert.Equal(0, hr);
                Assert.NotEqual(nint.Zero, pImportAgain);

                try
                {
                    // Verify the returned pointer has IMetaDataImport2 slots accessible.
                    // QI the result for IMetaDataImport2 to verify COM identity is correct.
                    Guid iidImport2 = typeof(IMetaDataImport2).GUID;
                    hr = Marshal.QueryInterface(pImportAgain, in iidImport2, out nint pImport2);
                    Assert.Equal(0, hr);
                    Assert.NotEqual(nint.Zero, pImport2);
                    Marshal.Release(pImport2);

                    // The critical check: read the vtable and verify EnumGenericParams (slot 65)
                    // is a valid function pointer, not garbage from reading past the vtable end.
                    // IMetaDataImport has 62 methods, IMetaDataImport2 adds 8 = 70 total.
                    // Vtable: [0]=QI, [1]=AddRef, [2]=Release, [3..64]=IMetaDataImport, [65..72]=IMetaDataImport2
                    nint* vtable = *(nint**)pImportAgain;
                    nint enumGenericParams = vtable[65]; // EnumGenericParams
                    Assert.NotEqual(nint.Zero, enumGenericParams);

                    // Call EnumGenericParams with a null enumerator to verify it doesn't AV.
                    // Use the COM function pointer directly, like ClrMD does.
                    nint hEnum = 0;
                    uint count = 0;
                    // delegate* unmanaged[Stdcall]<nint, nint*, uint, uint*, uint, uint*, int>
                    var fn = (delegate* unmanaged[Stdcall]<nint, nint*, uint, uint*, uint, uint*, int>)enumGenericParams;
                    hr = fn(pImportAgain, &hEnum, 0x02000002, null, 0, &count);
                    // Should succeed (or return no results) without AV
                    Assert.True(hr >= 0 || hr == unchecked((int)0x80131130)); // S_OK or CLDB_E_RECORD_NOTFOUND
                }
                finally
                {
                    Marshal.Release(pImportAgain);
                }
            }
            finally
            {
                Marshal.Release(pImport);
            }
        }
        finally
        {
            ComInterfaceMarshaller<IMetaDataImport2>.Free((void*)pUnk);
        }
    }

    [Fact]
    public void CountEnum_ReturnsCountForCdacEnum()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        nint hEnum = 0;
        uint* tokens = stackalloc uint[10];
        uint count;

        int hr = wrapper.EnumFields(&hEnum, 0x02000002, tokens, 10, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.True(count > 0);
        uint expectedCount = count;

        uint enumCount;
        hr = wrapper.CountEnum(hEnum, &enumCount);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(expectedCount, enumCount);

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void CountEnum_NullHandle_ReturnsZero()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        uint count = 42;
        int hr = wrapper.CountEnum(0, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0u, count);
    }

    [Fact]
    public void ResetEnum_ResetsPositionForCdacEnum()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        nint hEnum = 0;
        uint token;
        uint count;

        int hr = wrapper.EnumFields(&hEnum, 0x02000002, &token, 1, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(1u, count);
        uint firstToken = token;

        hr = wrapper.ResetEnum(hEnum, 0);
        Assert.Equal(HResults.S_OK, hr);

        hr = wrapper.EnumFields(&hEnum, 0x02000002, &token, 1, &count);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(1u, count);
        Assert.Equal(firstToken, token);

        wrapper.CloseEnum(hEnum);
    }

    [Fact]
    public void ResetEnum_NullHandle_ReturnsOk()
    {
        IMetaDataImport2 wrapper = CreateWrapper();

        int hr = wrapper.ResetEnum(0, 0);
        Assert.Equal(HResults.S_OK, hr);
    }
}
