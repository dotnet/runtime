// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using HResults = System.HResults;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class IXCLRDataProcessTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void AppDomains(MockTarget.Architecture arch)
    {
        TargetPointer appDomainAddress = new(0x1000);
        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetAppDomain()).Returns(appDomainAddress);
        IXCLRDataProcess process = CreateProcess(arch, loader: loader.Object);

        ulong handle;
        int hr = process.StartEnumAppDomains(&handle);
        Assert.Equal(HResults.S_OK, hr);
        Assert.NotEqual(0ul, handle);

        try
        {
            DacComNullableByRef<IXCLRDataAppDomain> appDomainOut = new(isNullRef: false);
            hr = process.EnumAppDomain(&handle, appDomainOut);
            Assert.Equal(HResults.S_OK, hr);
            ClrDataAppDomain appDomain = Assert.IsType<ClrDataAppDomain>(appDomainOut.Interface);
            Assert.Equal(appDomainAddress, appDomain.Address);

            DacComNullableByRef<IXCLRDataAppDomain> endOut = new(isNullRef: false);
            hr = process.EnumAppDomain(&handle, endOut);
            Assert.Equal(HResults.S_FALSE, hr);
        }
        finally
        {
            hr = process.EndEnumAppDomains(handle);
            Assert.Equal(HResults.S_OK, hr);
        }

        DacComNullableByRef<IXCLRDataAppDomain> byIdOut = new(isNullRef: false);
        hr = process.GetAppDomainByUniqueID(ClrDataAppDomain.DefaultAppDomainId, byIdOut);
        Assert.Equal(HResults.S_OK, hr);
        Assert.IsType<ClrDataAppDomain>(byIdOut.Interface);

        DacComNullableByRef<IXCLRDataAppDomain> invalidIdOut = new(isNullRef: false);
        hr = process.GetAppDomainByUniqueID(ClrDataAppDomain.DefaultAppDomainId + 1, invalidIdOut);
        Assert.Equal(HResults.E_INVALIDARG, hr);

        ulong emptyHandle = 0;
        DacComNullableByRef<IXCLRDataAppDomain> emptyOut = new(isNullRef: false);
        Assert.Equal(HResults.S_FALSE, process.EnumAppDomain(&emptyHandle, emptyOut));
        Assert.Equal(HResults.S_OK, process.EndEnumAppDomains(emptyHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void Modules(MockTarget.Architecture arch)
    {
        TargetPointer appDomainAddress = new(0x1000);
        ModuleHandle firstModule = new(new TargetPointer(0x2000));
        ModuleHandle secondModule = new(new TargetPointer(0x3000));
        IReadOnlyList<ModuleHandle> modules = [firstModule, secondModule];
        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetAppDomain()).Returns(appDomainAddress);
        loader.Setup(l => l.GetModuleHandles(
            appDomainAddress,
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution)).Returns(modules);
        TargetPointer firstBase = new(0x10000);
        uint firstSize = 0x100;
        uint firstFlags = 0;
        loader.Setup(l => l.TryGetLoadedImageContents(firstModule, out firstBase, out firstSize, out firstFlags)).Returns(false);
        TargetPointer secondBase = new(0x20000);
        uint secondSize = 0x200;
        uint secondFlags = 0;
        loader.Setup(l => l.TryGetLoadedImageContents(secondModule, out secondBase, out secondSize, out secondFlags)).Returns(true);
        IXCLRDataProcess process = CreateProcess(arch, loader: loader.Object);

        ulong handle;
        int hr = process.StartEnumModules(&handle);
        Assert.Equal(HResults.S_OK, hr);
        Assert.NotEqual(0ul, handle);

        try
        {
            foreach (ModuleHandle expected in modules)
            {
                DacComNullableByRef<IXCLRDataModule> moduleOut = new(isNullRef: false);
                hr = process.EnumModule(&handle, moduleOut);
                Assert.Equal(HResults.S_OK, hr);
                ClrDataModule module = Assert.IsType<ClrDataModule>(moduleOut.Interface);
                Assert.Equal(expected.Address, module.Address);
            }

            DacComNullableByRef<IXCLRDataModule> endOut = new(isNullRef: false);
            hr = process.EnumModule(&handle, endOut);
            Assert.Equal(HResults.S_FALSE, hr);
        }
        finally
        {
            hr = process.EndEnumModules(handle);
            Assert.Equal(HResults.S_OK, hr);
        }

        DacComNullableByRef<IXCLRDataModule> byAddressOut = new(isNullRef: false);
        hr = process.GetModuleByAddress(secondBase.Value + 0x80, byAddressOut);
        Assert.Equal(HResults.S_OK, hr);
        ClrDataModule byAddress = Assert.IsType<ClrDataModule>(byAddressOut.Interface);
        Assert.Equal(secondModule.Address, byAddress.Address);

        DacComNullableByRef<IXCLRDataModule> missingOut = new(isNullRef: false);
        hr = process.GetModuleByAddress(secondBase.Value + secondSize, missingOut);
        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Equal(HResults.S_OK, process.EndEnumModules(0));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ModuleVersionId(MockTarget.Architecture arch)
    {
        TargetPointer modulePointer = new(0x1000);
        ModuleHandle moduleHandle = new(new TargetPointer(0x2000));
        Guid expected = Guid.NewGuid();
        using MetadataReaderProvider provider = CreateMetadata(expected);
        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetModuleHandleFromModulePtr(modulePointer)).Returns(moduleHandle);
        Mock<IEcmaMetadata> metadata = new(MockBehavior.Strict);
        metadata.Setup(m => m.GetMetadata(moduleHandle)).Returns(provider.GetMetadataReader());
        IXCLRDataModule module = CreateModule(arch, modulePointer, loader.Object, metadata.Object);

        Guid actual;
        Assert.Equal(HResults.S_OK, module.GetVersionId(&actual));
        Assert.Equal(expected, actual);
        Assert.Equal(HResults.E_POINTER, module.GetVersionId(null));

        Mock<IEcmaMetadata> missingMetadata = new(MockBehavior.Strict);
        missingMetadata.Setup(m => m.GetMetadata(moduleHandle)).Returns((MetadataReader?)null);
        module = CreateModule(arch, modulePointer, loader.Object, missingMetadata.Object);
        Assert.Equal(HResults.E_FAIL, module.GetVersionId(&actual));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ModuleEnumerations(MockTarget.Architecture arch)
    {
        const uint ModuleTypeToken = 0x02000001;
        const uint TypeToken = 0x02000002;
        const uint MethodToken = 0x06000001;
        const uint FieldToken = 0x04000001;
        const uint ThreadStaticFieldToken = 0x04000002;
        const uint ValueTypeFieldToken = 0x04000003;
        TargetPointer modulePointer = new(0x1000);
        ModuleHandle moduleHandle = new(new TargetPointer(0x2000));
        TargetPointer typeMap = new(0x3000);
        TargetPointer methodMap = new(0x3100);
        TargetPointer fieldMap = new(0x3200);
        TargetPointer methodTable = new(0x4000);
        TargetPointer methodDesc = new(0x5000);
        TargetPointer fieldDesc = new(0x6000);
        TargetPointer threadStaticFieldDesc = new(0x6100);
        TargetPointer valueTypeFieldDesc = new(0x6200);
        TargetPointer fieldAddress = new(0x7000);
        TargetPointer threadStaticFieldAddress = new(0x7100);
        TargetPointer valueTypeFieldAddress = new(0x7200);
        TargetPointer appDomain = new(0x8000);
        TargetPointer thread = new(0x8100);
        TypeHandle typeHandle = new(methodTable);
        MethodDescHandle methodHandle = new(methodDesc);
        ModuleLookupTables maps = new(
            fieldMap,
            TargetPointer.Null,
            TargetPointer.Null,
            methodMap,
            typeMap,
            TargetPointer.Null,
            TargetPointer.Null,
            0);

        using MetadataReaderProvider provider = CreateEnumerationMetadata();
        MetadataReader reader = provider.GetMetadataReader();
        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetModuleHandleFromModulePtr(modulePointer)).Returns(moduleHandle);
        loader.Setup(l => l.GetLookupTables(moduleHandle)).Returns(maps);
        loader.Setup(l => l.GetAppDomain()).Returns(appDomain);
        loader.Setup(l => l.GetModuleLookupMapElement(typeMap, ModuleTypeToken, out It.Ref<TargetNUInt>.IsAny)).Returns(TargetPointer.Null);
        loader.Setup(l => l.GetModuleLookupMapElement(typeMap, TypeToken, out It.Ref<TargetNUInt>.IsAny)).Returns(methodTable);
        loader.Setup(l => l.GetModuleLookupMapElement(methodMap, MethodToken, out It.Ref<TargetNUInt>.IsAny)).Returns(methodDesc);
        loader.Setup(l => l.GetModuleLookupMapElement(fieldMap, FieldToken, out It.Ref<TargetNUInt>.IsAny)).Returns(fieldDesc);
        loader.Setup(l => l.GetModuleLookupMapElement(fieldMap, ThreadStaticFieldToken, out It.Ref<TargetNUInt>.IsAny)).Returns(threadStaticFieldDesc);
        loader.Setup(l => l.GetModuleLookupMapElement(fieldMap, ValueTypeFieldToken, out It.Ref<TargetNUInt>.IsAny)).Returns(valueTypeFieldDesc);

        Mock<IEcmaMetadata> metadata = new(MockBehavior.Strict);
        metadata.Setup(m => m.GetMetadata(moduleHandle)).Returns(reader);

        IRuntimeTypeSystem rts = new EnumerationRuntimeTypeSystem(
            modulePointer,
            typeHandle,
            TypeToken,
            methodHandle,
            MethodToken,
            fieldDesc,
            fieldAddress,
            threadStaticFieldDesc,
            threadStaticFieldAddress,
            valueTypeFieldDesc,
            valueTypeFieldAddress);

        ILCodeVersionHandle ilCodeVersion = ILCodeVersionHandle.CreateSynthetic(modulePointer, MethodToken);
        NativeCodeVersionHandle nativeCodeVersion = NativeCodeVersionHandle.CreateSynthetic(methodDesc);
        Mock<ICodeVersions> codeVersions = new();
        codeVersions.Setup(c => c.GetILCodeVersions(methodDesc)).Returns([ilCodeVersion]);
        codeVersions.Setup(c => c.GetNativeCodeVersions(methodDesc, ilCodeVersion)).Returns([nativeCodeVersion]);
        codeVersions.Setup(c => c.GetNativeCode(nativeCodeVersion)).Returns(new TargetCodePointer(0x9000));

        IXCLRDataModule module = CreateModule(
            arch,
            modulePointer,
            loader.Object,
            metadata.Object,
            rts,
            codeVersions.Object);

        ulong handle;
        Assert.Equal(HResults.S_OK, module.StartEnumTypeDefinitions(&handle));
        foreach (uint expectedToken in new[] { ModuleTypeToken, TypeToken })
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> definitionOut = new(isNullRef: false);
            Assert.Equal(HResults.S_OK, module.EnumTypeDefinition(&handle, definitionOut));
            IXCLRDataTypeDefinition definition = Assert.IsType<ClrDataTypeDefinition>(definitionOut.Interface);
            uint actualToken;
            Assert.Equal(
                HResults.S_OK,
                definition.GetTokenAndScope(&actualToken, new DacComNullableByRef<IXCLRDataModule>(isNullRef: true)));
            Assert.Equal(expectedToken, actualToken);
        }
        Assert.Equal(
            HResults.S_FALSE,
            module.EnumTypeDefinition(&handle, new DacComNullableByRef<IXCLRDataTypeDefinition>(isNullRef: false)));
        Assert.Equal(HResults.S_OK, module.EndEnumTypeDefinitions(handle));

        Assert.Equal(HResults.S_OK, module.StartEnumTypeInstances(null, &handle));
        DacComNullableByRef<IXCLRDataTypeInstance> typeOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, module.EnumTypeInstance(&handle, typeOut));
        IXCLRDataTypeInstance type = Assert.IsType<ClrDataTypeInstance>(typeOut.Interface);
        DacComNullableByRef<IXCLRDataTypeDefinition> loadedDefinitionOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, type.GetDefinition(loadedDefinitionOut));
        uint loadedToken;
        Assert.Equal(
            HResults.S_OK,
            loadedDefinitionOut.Interface!.GetTokenAndScope(
                &loadedToken,
                new DacComNullableByRef<IXCLRDataModule>(isNullRef: true)));
        Assert.Equal(TypeToken, loadedToken);
        Assert.Equal(
            HResults.S_FALSE,
            module.EnumTypeInstance(&handle, new DacComNullableByRef<IXCLRDataTypeInstance>(isNullRef: false)));
        Assert.Equal(HResults.S_OK, module.EndEnumTypeInstances(handle));

        fixed (char* methodName = "TestNamespace.TestType.Method")
        {
            Assert.Equal(HResults.S_OK, module.StartEnumMethodInstancesByName(methodName, 0, null, &handle));
        }
        DacComNullableByRef<IXCLRDataMethodInstance> methodOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, module.EnumMethodInstanceByName(&handle, methodOut));
        IXCLRDataMethodInstance method = Assert.IsType<ClrDataMethodInstance>(methodOut.Interface);
        uint actualMethodToken;
        Assert.Equal(
            HResults.S_OK,
            method.GetTokenAndScope(
                &actualMethodToken,
                new DacComNullableByRef<IXCLRDataModule>(isNullRef: true)));
        Assert.Equal(MethodToken, actualMethodToken);
        Assert.Equal(
            HResults.S_FALSE,
            module.EnumMethodInstanceByName(
                &handle,
                new DacComNullableByRef<IXCLRDataMethodInstance>(isNullRef: false)));
        Assert.Equal(HResults.S_OK, module.EndEnumMethodInstancesByName(handle));

        fixed (char* fieldName = "TestNamespace.TestType.StaticField")
        {
            Assert.Equal(HResults.S_OK, module.StartEnumDataByName(fieldName, 0, null, null, &handle));
        }
        DacComNullableByRef<IXCLRDataValue> valueOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, module.EnumDataByName(&handle, valueOut));
        IXCLRDataValue value = Assert.IsType<ClrDataValue>(valueOut.Interface);
        ClrDataAddress actualAddress;
        ulong actualSize;
        uint actualFlags;
        Assert.Equal(HResults.S_OK, value.GetAddress(&actualAddress));
        Assert.Equal(HResults.S_OK, value.GetSize(&actualSize));
        Assert.Equal(HResults.S_OK, value.GetFlags(&actualFlags));
        Assert.Equal(fieldAddress.Value, actualAddress.Value);
        Assert.Equal(4ul, actualSize);
        Assert.Equal((uint)ClrDataValueFlag.IS_PRIMITIVE | 0x00000800u, actualFlags);
        Assert.Equal(
            HResults.S_FALSE,
            module.EnumDataByName(&handle, new DacComNullableByRef<IXCLRDataValue>(isNullRef: false)));
        Assert.Equal(HResults.S_OK, module.EndEnumDataByName(handle));

        ClrDataTask task = new(thread, new TestPlaceholderTarget.Builder(arch).Build(), legacyImpl: null);
        fixed (char* fieldName = "TestNamespace.TestType.ThreadStaticField")
        {
            Assert.Equal(HResults.S_OK, module.StartEnumDataByName(fieldName, 0, null, task, &handle));
        }
        valueOut = new DacComNullableByRef<IXCLRDataValue>(isNullRef: false);
        Assert.Equal(HResults.S_OK, module.EnumDataByName(&handle, valueOut));
        value = Assert.IsType<ClrDataValue>(valueOut.Interface);
        Assert.Equal(HResults.S_OK, value.GetAddress(&actualAddress));
        Assert.Equal(HResults.S_OK, value.GetFlags(&actualFlags));
        Assert.Equal(threadStaticFieldAddress.Value, actualAddress.Value);
        Assert.Equal((uint)ClrDataValueFlag.IS_PRIMITIVE | 0x00000400u, actualFlags);
        Assert.Equal(HResults.S_OK, module.EndEnumDataByName(handle));

        fixed (char* fieldName = "TestNamespace.TestType.ValueTypeField")
        {
            Assert.Equal(HResults.S_OK, module.StartEnumDataByName(fieldName, 0, null, null, &handle));
        }
        valueOut = new DacComNullableByRef<IXCLRDataValue>(isNullRef: false);
        Assert.Equal(HResults.S_OK, module.EnumDataByName(&handle, valueOut));
        value = Assert.IsType<ClrDataValue>(valueOut.Interface);
        Assert.Equal(HResults.S_OK, value.GetAddress(&actualAddress));
        Assert.Equal(HResults.S_OK, value.GetSize(&actualSize));
        Assert.Equal(HResults.S_OK, value.GetFlags(&actualFlags));
        Assert.Equal(valueTypeFieldAddress.Value, actualAddress.Value);
        Assert.Equal(3ul, actualSize);
        Assert.Equal((uint)ClrDataValueFlag.IS_VALUE_TYPE | 0x00000800u, actualFlags);
        Assert.Equal(HResults.S_OK, module.EndEnumDataByName(handle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTaskByUniqueID(MockTarget.Architecture arch)
    {
        TargetPointer threadAddress = new(0x5000);
        Mock<IThread> thread = new(MockBehavior.Strict);
        thread.Setup(t => t.IdToThread(42)).Returns(threadAddress);
        thread.Setup(t => t.IdToThread(43)).Returns(TargetPointer.Null);
        IXCLRDataProcess process = CreateProcess(arch, thread: thread.Object);
        DacComNullableByRef<IXCLRDataTask> taskOut = new(isNullRef: false);

        int hr = process.GetTaskByUniqueID(0x1_0000_002a, taskOut);

        Assert.Equal(HResults.S_OK, hr);
        Assert.IsType<ClrDataTask>(taskOut.Interface);

        DacComNullableByRef<IXCLRDataTask> missingOut = new(isNullRef: false);
        hr = process.GetTaskByUniqueID(43, missingOut);
        Assert.Equal(HResults.E_INVALIDARG, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetAddressType(MockTarget.Architecture arch)
    {
        const ulong CodeAddress = 0x7000;
        (CodeKind Kind, uint Expected)[] cases =
        [
            (CodeKind.Unknown, 0),
            (CodeKind.Jitted, 1),
            (CodeKind.ReadyToRun, 1),
            (CodeKind.Interpreter, 1),
            (CodeKind.JumpStub, 6),
            (CodeKind.StubPrecode, 6),
            (CodeKind.VSD_DispatchStub, 6),
        ];

        foreach ((CodeKind kind, uint expected) in cases)
        {
            Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
            executionManager.Setup(e => e.GetCodeKind(new TargetCodePointer(CodeAddress))).Returns(kind);
            IXCLRDataProcess process = CreateProcess(
                arch,
                executionManager: executionManager.Object,
                readableAddress: CodeAddress);
            uint type;

            int hr = process.GetAddressType(CodeAddress, &type);

            Assert.Equal(HResults.S_OK, hr);
            Assert.Equal(expected, type);
        }

        IXCLRDataProcess unreadableProcess = CreateProcess(arch);
        uint unreadableType;
        int unreadableHr = unreadableProcess.GetAddressType(CodeAddress, &unreadableType);
        Assert.Equal(HResults.S_OK, unreadableHr);
        Assert.Equal(0u, unreadableType);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetDataByAddress(MockTarget.Architecture arch)
    {
        IXCLRDataProcess process = CreateProcess(arch);
        DacComNullableByRef<IXCLRDataValue> value = new(isNullRef: false);

        int hr = process.GetDataByAddress(0, 0, null, null, 0, null, null, value, null);
        Assert.Equal(HResults.E_NOTIMPL, hr);

        hr = process.GetDataByAddress(0, 1, null, null, 0, null, null, value, null);
        Assert.Equal(HResults.E_INVALIDARG, hr);
    }

    private static IXCLRDataProcess CreateProcess(
        MockTarget.Architecture arch,
        ILoader? loader = null,
        IThread? thread = null,
        IExecutionManager? executionManager = null,
        ulong? readableAddress = null)
    {
        TestPlaceholderTarget.Builder builder = new(arch);
        if (loader is not null)
            builder.AddMockContract(loader);
        if (thread is not null)
            builder.AddMockContract(thread);
        if (executionManager is not null)
            builder.AddMockContract(executionManager);
        if (readableAddress is not null)
        {
            builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
            {
                Address = readableAddress.Value,
                Data = [0],
                Name = nameof(readableAddress),
            });
        }

        return new SOSDacImpl(builder.Build(), legacyObj: null);
    }

    private static IXCLRDataModule CreateModule(
        MockTarget.Architecture arch,
        TargetPointer modulePointer,
        ILoader loader,
        IEcmaMetadata metadata,
        IRuntimeTypeSystem? runtimeTypeSystem = null,
        ICodeVersions? codeVersions = null)
    {
        TestPlaceholderTarget.Builder builder = new(arch);
        builder.AddMockContract(loader);
        builder.AddMockContract(metadata);
        if (runtimeTypeSystem is not null)
            builder.AddMockContract(runtimeTypeSystem);
        if (codeVersions is not null)
            builder.AddMockContract(codeVersions);
        return new ClrDataModule(modulePointer, builder.Build(), legacyImpl: null);
    }

    private static MetadataReaderProvider CreateMetadata(Guid mvid)
    {
        MetadataBuilder builder = new();
        builder.AddModule(0, builder.GetOrAddString("TestModule"), builder.GetOrAddGuid(mvid), default, default);
        BlobBuilder blob = new();
        new MetadataRootBuilder(builder).Serialize(blob, 0, 0);
        return MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(blob.ToArray()));
    }

    private static MetadataReaderProvider CreateEnumerationMetadata()
    {
        MetadataBuilder builder = new();
        builder.AddModule(0, builder.GetOrAddString("TestModule"), builder.GetOrAddGuid(Guid.Empty), default, default);

        BlobBuilder methodSignature = new();
        methodSignature.WriteByte(0x00);
        methodSignature.WriteCompressedInteger(0);
        methodSignature.WriteByte((byte)SignatureTypeCode.Void);
        BlobHandle methodSignatureHandle = builder.GetOrAddBlob(methodSignature);

        BlobBuilder fieldSignature = new();
        fieldSignature.WriteByte(0x06);
        fieldSignature.WriteByte((byte)SignatureTypeCode.Int32);
        BlobHandle fieldSignatureHandle = builder.GetOrAddBlob(fieldSignature);

        builder.AddTypeDefinition(
            default,
            default,
            builder.GetOrAddString("<Module>"),
            default,
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(1));
        builder.AddTypeDefinition(
            TypeAttributes.Public,
            builder.GetOrAddString("TestNamespace"),
            builder.GetOrAddString("TestType"),
            default,
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(1));
        builder.AddFieldDefinition(
            FieldAttributes.Public | FieldAttributes.Static,
            builder.GetOrAddString("StaticField"),
            fieldSignatureHandle);
        builder.AddFieldDefinition(
            FieldAttributes.Public | FieldAttributes.Static,
            builder.GetOrAddString("ThreadStaticField"),
            fieldSignatureHandle);
        builder.AddFieldDefinition(
            FieldAttributes.Public | FieldAttributes.Static,
            builder.GetOrAddString("ValueTypeField"),
            fieldSignatureHandle);
        builder.AddMethodDefinition(
            MethodAttributes.Public | MethodAttributes.Static,
            MethodImplAttributes.IL,
            builder.GetOrAddString("Method"),
            methodSignatureHandle,
            -1,
            MetadataTokens.ParameterHandle(1));

        BlobBuilder blob = new();
        new MetadataRootBuilder(builder).Serialize(blob, 0, 0);
        return MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(blob.ToArray()));
    }

    private sealed class EnumerationRuntimeTypeSystem : IRuntimeTypeSystem
    {
        private readonly TargetPointer _module;
        private readonly TypeHandle _typeHandle;
        private readonly uint _typeToken;
        private readonly MethodDescHandle _methodHandle;
        private readonly uint _methodToken;
        private readonly TargetPointer _fieldDesc;
        private readonly TargetPointer _fieldAddress;
        private readonly TargetPointer _threadStaticFieldDesc;
        private readonly TargetPointer _threadStaticFieldAddress;
        private readonly TargetPointer _valueTypeFieldDesc;
        private readonly TargetPointer _valueTypeFieldAddress;

        public EnumerationRuntimeTypeSystem(
            TargetPointer module,
            TypeHandle typeHandle,
            uint typeToken,
            MethodDescHandle methodHandle,
            uint methodToken,
            TargetPointer fieldDesc,
            TargetPointer fieldAddress,
            TargetPointer threadStaticFieldDesc,
            TargetPointer threadStaticFieldAddress,
            TargetPointer valueTypeFieldDesc,
            TargetPointer valueTypeFieldAddress)
        {
            _module = module;
            _typeHandle = typeHandle;
            _typeToken = typeToken;
            _methodHandle = methodHandle;
            _methodToken = methodToken;
            _fieldDesc = fieldDesc;
            _fieldAddress = fieldAddress;
            _threadStaticFieldDesc = threadStaticFieldDesc;
            _threadStaticFieldAddress = threadStaticFieldAddress;
            _valueTypeFieldDesc = valueTypeFieldDesc;
            _valueTypeFieldAddress = valueTypeFieldAddress;
        }

        public TypeHandle GetTypeHandle(TargetPointer targetPointer) => _typeHandle;
        public TargetPointer GetModule(TypeHandle typeHandle) => _module;
        public uint GetTypeDefToken(TypeHandle typeHandle) => _typeToken;
        public TargetPointer GetParentMethodTable(TypeHandle typeHandle) => TargetPointer.Null;
        public ReadOnlySpan<TypeHandle> GetInstantiation(TypeHandle typeHandle) => [];
        public MethodDescHandle GetMethodDescHandle(TargetPointer targetPointer) => _methodHandle;
        public TargetPointer GetMethodTable(MethodDescHandle methodDesc) => _typeHandle.Address;
        public bool IsGenericMethodDefinition(MethodDescHandle methodDesc) => false;
        public ReadOnlySpan<TypeHandle> GetGenericMethodInstantiation(MethodDescHandle methodDesc) => [];
        public uint GetMethodToken(MethodDescHandle methodDesc) => _methodToken;
        public bool ContainsGenericVariables(TypeHandle typeHandle) => false;
        public bool IsFieldDescThreadStatic(TargetPointer fieldDescPointer)
            => fieldDescPointer == _threadStaticFieldDesc;
        public TargetPointer GetFieldDescStaticAddress(TargetPointer fieldDescPointer, bool unboxValueTypes = true)
            => fieldDescPointer == _fieldDesc
                ? _fieldAddress
                : fieldDescPointer == _valueTypeFieldDesc ? _valueTypeFieldAddress : TargetPointer.Null;
        public TargetPointer GetFieldDescThreadStaticAddress(TargetPointer fieldDescPointer, TargetPointer thread, bool unboxValueTypes = true)
            => fieldDescPointer == _threadStaticFieldDesc ? _threadStaticFieldAddress : TargetPointer.Null;
        public CorElementType GetFieldDescType(TargetPointer fieldDescPointer)
            => fieldDescPointer == _valueTypeFieldDesc ? CorElementType.ValueType : CorElementType.I4;
        public TypeHandle GetFieldDescApproxTypeHandle(TargetPointer fieldDescPointer) => _typeHandle;
        public uint GetNumInstanceFieldBytes(TypeHandle typeHandle) => 3;
        public bool IsEnum(TypeHandle typeHandle) => false;
    }
}
