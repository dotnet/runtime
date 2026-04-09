// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

using static Microsoft.Diagnostics.DataContractReader.Tests.MockExtensions;
using MockCodeVersions = MockCodeVersionsBuilder;

internal static class MockExtensions
{
    internal class CodeVersionsMockModule
    {
        public TargetPointer Address { get; set; }
        public TargetPointer MethodDefToILCodeVersioningStateAddress { get; set; }
        public Dictionary<uint, TargetPointer> MethodDefToILCodeVersioningStateTable {get; set;}
    }

    internal class CodeVersionsMockMethodTable
    {
        public TargetPointer Address { get; set; }
        public CodeVersionsMockModule? Module {get; set; }
    }

    internal class CodeVersionsMockMethodDesc
    {
        public TargetPointer Address { get; private set; }
        public bool IsVersionable { get; private set; }

        public uint RowId { get; set; }
        public uint MethodToken => EcmaMetadataUtils.CreateMethodDef(RowId);

        // n.b. in the real RuntimeTypeSystem_1 this is more complex
        public TargetCodePointer NativeCode { get; private set; }

        // only non-null if IsVersionable is true
        public TargetPointer MethodDescVersioningState { get; private set; }

        public CodeVersionsMockMethodTable? MethodTable { get; set; }

        public static CodeVersionsMockMethodDesc CreateNonVersionable (TargetPointer selfAddress, TargetCodePointer nativeCode)
        {
            return new CodeVersionsMockMethodDesc() {
                Address = selfAddress,
                IsVersionable = false,
                NativeCode = nativeCode,
                MethodDescVersioningState = TargetPointer.Null,
            };
        }

        public static CodeVersionsMockMethodDesc CreateVersionable (TargetPointer selfAddress, TargetPointer methodDescVersioningState, TargetCodePointer nativeCode = default)
        {
            return new CodeVersionsMockMethodDesc() {
                Address = selfAddress,
                IsVersionable = true,
                NativeCode = nativeCode,
                MethodDescVersioningState = methodDescVersioningState,
            };
        }
    }

    internal class MockCodeBlockStart
    {
        public TargetCodePointer StartAddress { get; set;}
        public uint Length { get; set; }
        public CodeVersionsMockMethodDesc MethodDesc {get; set;}
    }

    public static void AddCodeBlock(this Mock<IExecutionManager> mock, MockCodeBlockStart block)
    {
        CodeBlockHandle handle = new CodeBlockHandle(block.StartAddress.AsTargetPointer);
        mock.Setup(e => e.GetCodeBlockHandle(It.Is<TargetCodePointer>(ip => ip >= block.StartAddress && ip < block.StartAddress + block.Length)))
            .Returns(handle);
        mock.Setup(e => e.GetStartAddress(handle)).Returns(block.StartAddress);
        mock.Setup(e => e.GetMethodDesc(handle)).Returns(block.MethodDesc.Address);
    }

    public static void AddModule(this Mock<ILoader> mock, CodeVersionsMockModule module)
    {
        Contracts.ModuleHandle handle = new Contracts.ModuleHandle(module.Address);
        mock.Setup(l => l.GetModuleHandleFromModulePtr(module.Address)).Returns(handle);
        mock.Setup(l => l.GetLookupTables(handle)).Returns(new ModuleLookupTables() {
            MethodDefToILCodeVersioningState = module.MethodDefToILCodeVersioningStateAddress,
        });
        mock.Setup(l => l.GetModuleLookupMapElement(module.MethodDefToILCodeVersioningStateAddress, It.IsAny<uint>(), out It.Ref<TargetNUInt>.IsAny))
        .Returns<TargetPointer, uint, TargetNUInt>((table, token, flags) =>
        {
            flags = new TargetNUInt(0);
            if (module.MethodDefToILCodeVersioningStateTable.TryGetValue(EcmaMetadataUtils.GetRowId(token), out TargetPointer value))
            {
                return value;
            }
            throw new InvalidOperationException($"No token found for 0x{token:x} in table {table}");
        });
    }

    public static void AddMethodDesc(this Mock<IRuntimeTypeSystem> mock, CodeVersionsMockMethodDesc methodDesc)
    {
        MethodDescHandle handle = new MethodDescHandle(methodDesc.Address);
        mock.Setup(r => r.GetMethodDescHandle(methodDesc.Address)).Returns(handle);
        mock.Setup(r => r.IsVersionable(handle)).Returns(methodDesc.IsVersionable);
        mock.Setup(r => r.GetNativeCode(handle)).Returns(methodDesc.NativeCode);
        mock.Setup(r => r.GetMethodDescVersioningState(handle)).Returns(methodDesc.MethodDescVersioningState);
        mock.Setup(r => r.GetMethodTable(handle)).Returns(() => methodDesc.MethodTable?.Address ?? throw new InvalidOperationException($"MethodTable not found for {methodDesc.Address}"));
        mock.Setup(r => r.GetMethodToken(handle)).Returns(methodDesc.MethodToken);
    }

    public static void AddMethodTable(this Mock<IRuntimeTypeSystem> mock, MockCodeVersions builder, CodeVersionsMockMethodTable methodTable)
    {
        TypeHandle handle = new TypeHandle(methodTable.Address);
        mock.Setup(r => r.GetTypeHandle(methodTable.Address)).Returns<TargetPointer>(address =>
        {
            // this is not quite accurate on 32 bit architectures, but it's good enough for testing
            ulong addressLowBits = (ulong)address & ((ulong)builder.Builder.TargetTestHelpers.PointerSize - 1);
            // no typedescs for now, just method tables with 0 in the low bits
            if (addressLowBits != 0)
            {
                throw new InvalidOperationException("Invalid type handle pointer");
            }
            return handle;
        });
        mock.Setup(r => r.GetModule(handle)).Returns(methodTable.Module?.Address ?? throw new InvalidOperationException($"Module not found for {handle.Address}"));
    }
}

public class CodeVersionsTests
{
    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockCodeVersions builder)
        => new()
        {
            [DataType.MethodDescVersioningState] = TargetTestHelpers.CreateTypeInfo(builder.MethodDescVersioningStateLayout),
            [DataType.NativeCodeVersionNode] = TargetTestHelpers.CreateTypeInfo(builder.NativeCodeVersionNodeLayout),
            [DataType.ILCodeVersioningState] = TargetTestHelpers.CreateTypeInfo(builder.ILCodeVersioningStateLayout),
            [DataType.ILCodeVersionNode] = TargetTestHelpers.CreateTypeInfo(builder.ILCodeVersionNodeLayout),
            [DataType.GCCoverageInfo] = TargetTestHelpers.CreateTypeInfo(builder.GCCoverageInfoLayout),
        };

    internal Target CreateTarget(
        MockTarget.Architecture arch,
        MockCodeVersions builder,
        Mock<ILoader> mockLoader = null,
        Mock<IExecutionManager> mockExecutionManager = null,
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem = null)
    {
        mockLoader ??= new Mock<ILoader>();
        mockExecutionManager ??= new Mock<IExecutionManager>();
        mockRuntimeTypeSystem ??= new Mock<IRuntimeTypeSystem>();

        TestPlaceholderTarget target = new TestPlaceholderTarget(
            arch,
            builder.Builder.GetMemoryContext().ReadFromTarget,
            CreateContractTypes(builder));

        IContractFactory<ICodeVersions> cvfactory = new CodeVersionsFactory();
        ContractRegistry reg = Mock.Of<ContractRegistry>(
            c => c.CodeVersions == cvfactory.CreateContract(target, 1)
                && c.RuntimeTypeSystem == mockRuntimeTypeSystem.Object
                && c.ExecutionManager == mockExecutionManager.Object
                && c.Loader == mockLoader.Object);
        target.SetContracts(reg);
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNativeCodeVersion_Null(MockTarget.Architecture arch)
    {
        var mockExecutionManager = new Mock<IExecutionManager>();
        mockExecutionManager.Setup(e => e.GetCodeBlockHandle(TargetCodePointer.Null)).Returns(() => null);
        var target = CreateTarget(arch, new MockCodeVersions(arch), mockExecutionManager: mockExecutionManager);
        var codeVersions = target.Contracts.CodeVersions;

        Assert.NotNull(codeVersions);

        TargetCodePointer nullPointer = TargetCodePointer.Null;

        var handle = codeVersions.GetNativeCodeVersionForIP(nullPointer);
        Assert.False(handle.Valid);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNativeCodeVersion_OneVersion_NonVersionable(MockTarget.Architecture arch)
    {
        TargetCodePointer codeBlockStart = new TargetCodePointer(0x0a0a_0000);
        CodeVersionsMockMethodDesc oneMethod = CodeVersionsMockMethodDesc.CreateNonVersionable(selfAddress: new TargetPointer(0x1a0a_0000), nativeCode: codeBlockStart);
        MockCodeBlockStart oneBlock = new MockCodeBlockStart()
        {
            StartAddress = codeBlockStart,
            Length = 0x100,
            MethodDesc = oneMethod,
        };

        Mock<IExecutionManager> mockExecutionManager = new Mock<IExecutionManager>();
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem = new Mock<IRuntimeTypeSystem>();

        mockExecutionManager.AddCodeBlock(oneBlock);
        mockRuntimeTypeSystem.AddMethodDesc(oneMethod);

        var target = CreateTarget(
            arch,
            new MockCodeVersions(arch),
            mockExecutionManager: mockExecutionManager,
            mockRuntimeTypeSystem: mockRuntimeTypeSystem);

        // TEST

        var codeVersions = target.Contracts.CodeVersions;
        Assert.NotNull(codeVersions);

        TargetCodePointer codeBlockEnd = codeBlockStart + oneBlock.Length;
        for (TargetCodePointer ip = codeBlockStart; ip < codeBlockEnd; ip++)
        {
            var handle = codeVersions.GetNativeCodeVersionForIP(ip);
            Assert.True(handle.Valid);
            // FIXME: do we want to lock this down? it's part of the algorithm details, but maybe not part of the contract
            //Assert.Equal(oneBlock.MethodDescAddress, handle.MethodDescAddress);
            TargetCodePointer actualCodeStart = codeVersions.GetNativeCode(handle);
            Assert.Equal(codeBlockStart, actualCodeStart);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNativeCodeVersion_OneVersion_Versionable(MockTarget.Architecture arch)
    {
        var builder = new MockCodeVersions(arch);
        MockNativeCodeVersionNode nativeCodeVersionNode = builder.AddNativeCodeVersionNode();
        MockMethodDescVersioningState methodDescVersioningState = builder.AddMethodDescVersioningState();
        methodDescVersioningState.NativeCodeVersionNode = nativeCodeVersionNode.Address;
        methodDescVersioningState.Flags = MockMethodDescVersioningState.IsDefaultVersionActiveChildFlag;
        TargetCodePointer codeBlockStart = new TargetCodePointer(0x0a0a_0000);
        CodeVersionsMockMethodDesc oneMethod = CodeVersionsMockMethodDesc.CreateVersionable(selfAddress: new TargetPointer(0x1a0a_0000), methodDescVersioningState: new TargetPointer(methodDescVersioningState.Address));
        MockCodeBlockStart oneBlock = new MockCodeBlockStart()
        {
            StartAddress = codeBlockStart,
            Length = 0x100,
            MethodDesc = oneMethod,
        };
        nativeCodeVersionNode.Next = 0;
        nativeCodeVersionNode.MethodDesc = oneMethod.Address;
        nativeCodeVersionNode.NativeCode = codeBlockStart;
        nativeCodeVersionNode.Flags = 0;
        nativeCodeVersionNode.ILVersionId = 0;
        nativeCodeVersionNode.GCCoverageInfo = 0;

        Mock<IExecutionManager> mockExecutionManager = new Mock<IExecutionManager>();
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem = new Mock<IRuntimeTypeSystem>();

        mockExecutionManager.AddCodeBlock(oneBlock);
        mockRuntimeTypeSystem.AddMethodDesc(oneMethod);

        var target = CreateTarget(
            arch,
            builder,
            mockExecutionManager: mockExecutionManager,
            mockRuntimeTypeSystem: mockRuntimeTypeSystem);

        // TEST

        var codeVersions = target.Contracts.CodeVersions;
        Assert.NotNull(codeVersions);

        TargetCodePointer codeBlockEnd = codeBlockStart + oneBlock.Length;
        for (TargetCodePointer ip = codeBlockStart; ip < codeBlockEnd; ip++)
        {
            var handle = codeVersions.GetNativeCodeVersionForIP(ip);
            Assert.True(handle.Valid);
            // FIXME: do we want to lock this down? it's part of the algorithm details, but maybe not part of the contract
            //Assert.Equal(oneBlock.MethodDescAddress, handle.MethodDescAddress);
            TargetCodePointer actualCodeStart = codeVersions.GetNativeCode(handle);
            Assert.Equal(codeBlockStart, actualCodeStart);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetActiveNativeCodeVersion_DefaultCase(MockTarget.Architecture arch)
    {
        uint methodRowId = 0x25; // arbitrary
        TargetCodePointer expectedNativeCodePointer = new TargetCodePointer(0x0700_abc0);
        uint methodDefToken = EcmaMetadataUtils.CreateMethodDef(methodRowId);
        var builder = new MockCodeVersions(arch);
        var methodDescAddress = new TargetPointer(0x00aa_aa00);
        var methodDescNilTokenAddress = new TargetPointer(0x00aa_bb00);
        var moduleAddress = new TargetPointer(0x00ca_ca00);

        MockILCodeVersioningState versioningState = builder.AddILCodeVersioningState();
        versioningState.ActiveVersionKind = 0;
        versioningState.ActiveVersionNode = 0;
        versioningState.ActiveVersionModule = moduleAddress.Value;
        versioningState.ActiveVersionMethodDef = methodDefToken;
        versioningState.FirstVersionNode = 0;
        var module = new CodeVersionsMockModule() {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, new TargetPointer(versioningState.Address)}
            },
        };
        var methodTable = new CodeVersionsMockMethodTable() {
            Address = new TargetPointer(0x00ba_ba00),
            Module = module,
        };
        var method = CodeVersionsMockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: TargetPointer.Null, nativeCode: expectedNativeCodePointer);
        method.MethodTable = methodTable;
        method.RowId = methodRowId;

        var methodNilToken = CodeVersionsMockMethodDesc.CreateVersionable(selfAddress: methodDescNilTokenAddress, methodDescVersioningState: TargetPointer.Null, nativeCode: expectedNativeCodePointer);
        methodNilToken.MethodTable = methodTable;

        Mock<ILoader> mockLoader = new Mock<ILoader>();
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem = new Mock<IRuntimeTypeSystem>();

        mockRuntimeTypeSystem.AddMethodDesc(method);
        mockRuntimeTypeSystem.AddMethodDesc(methodNilToken);
        mockRuntimeTypeSystem.AddMethodTable(builder, methodTable);
        mockLoader.AddModule(module);

        var target = CreateTarget(
            arch,
            builder,
            mockLoader: mockLoader,
            mockRuntimeTypeSystem: mockRuntimeTypeSystem);

        // TEST

        var codeVersions = target.Contracts.CodeVersions;
        Assert.NotNull(codeVersions);

        {
            NativeCodeVersionHandle handle = codeVersions.GetActiveNativeCodeVersion(methodDescAddress);
            Assert.True(handle.Valid);
            Assert.Equal(methodDescAddress, handle.MethodDescAddress);
            var actualCodeAddress = codeVersions.GetNativeCode(handle);
            Assert.Equal(expectedNativeCodePointer, actualCodeAddress);
        }
        {
            NativeCodeVersionHandle handle = codeVersions.GetActiveNativeCodeVersion(methodDescNilTokenAddress);
            Assert.True(handle.Valid);
            Assert.Equal(methodDescNilTokenAddress, handle.MethodDescAddress);
            var actualCodeAddress = codeVersions.GetNativeCode(handle);
            Assert.Equal(expectedNativeCodePointer, actualCodeAddress);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetActiveNativeCodeVersion_IterateVersionNodes(MockTarget.Architecture arch)
    {
        GetActiveNativeCodeVersion_IterateVersionNodes_Impl(arch, shouldFindActiveCodeVersion: true);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetActiveNativeCodeVersion_IterateVersionNodes_NoMatch(MockTarget.Architecture arch)
    {
        GetActiveNativeCodeVersion_IterateVersionNodes_Impl(arch, shouldFindActiveCodeVersion: false);
    }

    private void GetActiveNativeCodeVersion_IterateVersionNodes_Impl(MockTarget.Architecture arch, bool shouldFindActiveCodeVersion)
    {
        uint methodRowId = 0x25; // arbitrary
        TargetCodePointer expectedNativeCodePointer = new TargetCodePointer(0x0700_abc0);
        uint methodDefToken = 0x06000000 | methodRowId;
        var builder = new MockCodeVersions(arch);
        var methodDescAddress = new TargetPointer(0x00aa_aa00);
        var moduleAddress = new TargetPointer(0x00ca_ca00);

        MockILCodeVersioningState versioningState = builder.AddILCodeVersioningState();
        versioningState.ActiveVersionKind = 0;
        versioningState.ActiveVersionNode = 0;
        versioningState.ActiveVersionModule = moduleAddress.Value;
        versioningState.ActiveVersionMethodDef = methodDefToken;
        versioningState.FirstVersionNode = 0;
        var module = new CodeVersionsMockModule()
        {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, new TargetPointer(versioningState.Address)}
            },
        };
        var methodTable = new CodeVersionsMockMethodTable()
        {
            Address = new TargetPointer(0x00ba_ba00),
            Module = module,
        };

        // Add the linked list of native code version nodes
        int count = 3;
        int activeIndex = shouldFindActiveCodeVersion ? count - 1 : -1;
        (MockNativeCodeVersionNode firstNode, MockNativeCodeVersionNode? activeVersionNode) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, count, activeIndex, expectedNativeCodePointer, default);
        MockMethodDescVersioningState methodDescVersioningState = builder.AddMethodDescVersioningState();
        methodDescVersioningState.NativeCodeVersionNode = firstNode.Address;

        var methodDesc = CodeVersionsMockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: new TargetPointer(methodDescVersioningState.Address), nativeCode: expectedNativeCodePointer);
        methodDesc.MethodTable = methodTable;
        methodDesc.RowId = methodRowId;

        Mock<ILoader> mockLoader = new Mock<ILoader>();
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem = new Mock<IRuntimeTypeSystem>();

        mockLoader.AddModule(module);
        mockRuntimeTypeSystem.AddMethodDesc(methodDesc);
        mockRuntimeTypeSystem.AddMethodTable(builder, methodTable);

        var target = CreateTarget(
            arch,
            builder,
            mockLoader: mockLoader,
            mockRuntimeTypeSystem: mockRuntimeTypeSystem);

        // TEST

        var codeVersions = target.Contracts.CodeVersions;
        Assert.NotNull(codeVersions);

        NativeCodeVersionHandle handle = codeVersions.GetActiveNativeCodeVersion(methodDescAddress);
        if (shouldFindActiveCodeVersion)
        {
            Assert.True(handle.Valid);
            Assert.Equal(new TargetPointer(activeVersionNode!.Address), handle.CodeVersionNodeAddress);
            var actualCodeAddress = codeVersions.GetNativeCode(handle);
            Assert.Equal(expectedNativeCodePointer, actualCodeAddress);
        }
        else
        {
            Assert.False(handle.Valid);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetActiveNativeCodeVersion_ExplicitILCodeVersion(MockTarget.Architecture arch)
    {
        GetActiveNativeCodeVersion_ExplicitILCodeVersion_Impl(arch, shouldFindActiveCodeVersion: true);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetActiveNativeCodeVersion_ExplicitILCodeVersion_NoMatch(MockTarget.Architecture arch)
    {
        GetActiveNativeCodeVersion_ExplicitILCodeVersion_Impl(arch, shouldFindActiveCodeVersion: false);
    }

    private void GetActiveNativeCodeVersion_ExplicitILCodeVersion_Impl(MockTarget.Architecture arch, bool shouldFindActiveCodeVersion)
    {
        uint methodRowId = 0x25; // arbitrary
        TargetCodePointer expectedNativeCodePointer = new TargetCodePointer(0x0700_abc0);
        var builder = new MockCodeVersions(arch);
        var methodDescAddress = new TargetPointer(0x00aa_aa00);
        var moduleAddress = new TargetPointer(0x00ca_ca00);

        TargetNUInt ilVersionId = new TargetNUInt(5);
        MockILCodeVersionNode ilVersionNode = builder.AddILCodeVersionNode(ilVersionId.Value, /* kStateActive */ 0x00000002);
        MockILCodeVersioningState versioningState = builder.AddILCodeVersioningState();
        versioningState.ActiveVersionKind = 1;
        versioningState.ActiveVersionNode = ilVersionNode.Address;
        versioningState.ActiveVersionModule = 0;
        versioningState.ActiveVersionMethodDef = 0;
        versioningState.FirstVersionNode = ilVersionNode.Address;
        var module = new CodeVersionsMockModule()
        {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, new TargetPointer(versioningState.Address)}
            },
        };
        var methodTable = new CodeVersionsMockMethodTable()
        {
            Address = new TargetPointer(0x00ba_ba00),
            Module = module,
        };

        // Add the linked list of native code version nodes
        int count = 3;
        int activeIndex = shouldFindActiveCodeVersion ? count - 1 : -1;
        TargetNUInt activeIlVersionId = shouldFindActiveCodeVersion ? ilVersionId : default;
        (MockNativeCodeVersionNode firstNode, MockNativeCodeVersionNode? activeVersionNode) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, count, activeIndex, expectedNativeCodePointer, activeIlVersionId.Value);
        MockMethodDescVersioningState methodDescVersioningState = builder.AddMethodDescVersioningState();
        methodDescVersioningState.NativeCodeVersionNode = firstNode.Address;

        var oneMethod = CodeVersionsMockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: new TargetPointer(methodDescVersioningState.Address), nativeCode: expectedNativeCodePointer);
        oneMethod.MethodTable = methodTable;
        oneMethod.RowId = methodRowId;

        Mock<ILoader> mockLoader = new Mock<ILoader>();
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem = new Mock<IRuntimeTypeSystem>();

        mockLoader.AddModule(module);
        mockRuntimeTypeSystem.AddMethodDesc(oneMethod);
        mockRuntimeTypeSystem.AddMethodTable(builder, methodTable);

        var target = CreateTarget(
            arch,
            builder,
            mockLoader: mockLoader,
            mockRuntimeTypeSystem: mockRuntimeTypeSystem);

        // TEST

        var codeVersions = target.Contracts.CodeVersions;
        Assert.NotNull(codeVersions);

        var handle = codeVersions.GetActiveNativeCodeVersion(methodDescAddress);
        if (shouldFindActiveCodeVersion)
        {
            Assert.True(handle.Valid);
            Assert.Equal(new TargetPointer(activeVersionNode!.Address), handle.CodeVersionNodeAddress);
            var actualCodeAddress = codeVersions.GetNativeCode(handle);
            Assert.Equal(expectedNativeCodePointer, actualCodeAddress);
        }
        else
        {
            Assert.False(handle.Valid);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetILCodeVersions_SyntheticAndExplicit(MockTarget.Architecture arch)
    {
        uint methodRowId = 0x25; // arbitrary
        TargetCodePointer expectedSyntheticCodePointer = new TargetCodePointer(0x0700_abc0);
        TargetCodePointer expectedExplicitCodePointer = new TargetCodePointer(0x0780_abc0);
        var builder = new MockCodeVersions(arch);
        var methodDescAddress = new TargetPointer(0x00aa_aa00);
        var moduleAddress = new TargetPointer(0x00ca_ca00);

        TargetNUInt ilVersionId = new TargetNUInt(2);
        MockILCodeVersionNode ilVersionNode = builder.AddILCodeVersionNode(ilVersionId.Value, /* kStateActive */ 0x00000002);
        MockILCodeVersioningState versioningState = builder.AddILCodeVersioningState();
        versioningState.ActiveVersionKind = 1;
        versioningState.ActiveVersionNode = ilVersionNode.Address;
        versioningState.ActiveVersionModule = 0;
        versioningState.ActiveVersionMethodDef = 0;
        versioningState.FirstVersionNode = ilVersionNode.Address;
        var module = new CodeVersionsMockModule()
        {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, new TargetPointer(versioningState.Address)}
            },
        };
        var methodTable = new CodeVersionsMockMethodTable()
        {
            Address = new TargetPointer(0x00ba_ba00),
            Module = module,
        };

        (MockNativeCodeVersionNode firstNode, _) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, 2, 1, expectedSyntheticCodePointer, new TargetNUInt(0).Value);
        (firstNode, _) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, 2, 1, expectedExplicitCodePointer, ilVersionId.Value, firstNode);

        MockMethodDescVersioningState methodDescVersioningState = builder.AddMethodDescVersioningState();
        methodDescVersioningState.NativeCodeVersionNode = firstNode.Address;

        var oneMethod = CodeVersionsMockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: new TargetPointer(methodDescVersioningState.Address));
        oneMethod.MethodTable = methodTable;
        oneMethod.RowId = methodRowId;

        Mock<ILoader> mockLoader = new Mock<ILoader>();
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem = new Mock<IRuntimeTypeSystem>();

        mockLoader.AddModule(module);
        mockRuntimeTypeSystem.AddMethodDesc(oneMethod);
        mockRuntimeTypeSystem.AddMethodTable(builder, methodTable);

        var target = CreateTarget(
            arch,
            builder,
            mockLoader: mockLoader,
            mockRuntimeTypeSystem: mockRuntimeTypeSystem);

        // TEST

        var codeVersions = target.Contracts.CodeVersions;
        Assert.NotNull(codeVersions);

        // Get all ILCodeVersions
        List<ILCodeVersionHandle> ilCodeVersions = codeVersions.GetILCodeVersions(methodDescAddress).ToList();
        Assert.Equal(2, ilCodeVersions.Count);

        // Get the explicit ILCodeVersion and assert that it is in the list of ILCodeVersions
        ILCodeVersionHandle explicitILCodeVersion = codeVersions.GetActiveILCodeVersion(methodDescAddress);
        Assert.Contains(ilCodeVersions, ilcodeVersion => ilcodeVersion.Equals(explicitILCodeVersion));
        Assert.Equal(expectedExplicitCodePointer, codeVersions.GetNativeCode(codeVersions.GetActiveNativeCodeVersionForILCodeVersion(methodDescAddress, explicitILCodeVersion)));

        // Find the other ILCodeVersion (synthetic) and assert that it is valid.
        ILCodeVersionHandle syntheticILcodeVersion = ilCodeVersions.Find(ilCodeVersion => !ilCodeVersion.Equals(explicitILCodeVersion));
        Assert.True(syntheticILcodeVersion.IsValid);
        Assert.Equal(expectedSyntheticCodePointer, codeVersions.GetNativeCode(codeVersions.GetActiveNativeCodeVersionForILCodeVersion(methodDescAddress, syntheticILcodeVersion)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IlToNativeToIlCodeVersion_SyntheticAndExplicit(MockTarget.Architecture arch)
    {
        uint methodRowId = 0x25; // arbitrary
        TargetCodePointer expectedSyntheticCodePointer = new TargetCodePointer(0x0700_abc0);
        TargetCodePointer expectedExplicitCodePointer = new TargetCodePointer(0x0780_abc0);
        var builder = new MockCodeVersions(arch);
        var methodDescAddress = new TargetPointer(0x00aa_aa00);
        var moduleAddress = new TargetPointer(0x00ca_ca00);

        TargetNUInt ilVersionId = new TargetNUInt(2);
        MockILCodeVersionNode ilVersionNode = builder.AddILCodeVersionNode(ilVersionId.Value, /* kStateActive */ 0x00000002);
        MockILCodeVersioningState versioningState = builder.AddILCodeVersioningState();
        versioningState.ActiveVersionKind = 1;
        versioningState.ActiveVersionNode = ilVersionNode.Address;
        versioningState.ActiveVersionModule = 0;
        versioningState.ActiveVersionMethodDef = 0;
        versioningState.FirstVersionNode = ilVersionNode.Address;
        var module = new CodeVersionsMockModule()
        {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, new TargetPointer(versioningState.Address)}
            },
        };
        var methodTable = new CodeVersionsMockMethodTable()
        {
            Address = new TargetPointer(0x00ba_ba00),
            Module = module,
        };

        (MockNativeCodeVersionNode firstNode, _) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, 2, 1, expectedSyntheticCodePointer, new TargetNUInt(0).Value);
        (firstNode, _) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, 2, 1, expectedExplicitCodePointer, ilVersionId.Value, firstNode);

        MockMethodDescVersioningState methodDescVersioningState = builder.AddMethodDescVersioningState();
        methodDescVersioningState.NativeCodeVersionNode = firstNode.Address;

        var oneMethod = CodeVersionsMockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: new TargetPointer(methodDescVersioningState.Address));
        oneMethod.MethodTable = methodTable;
        oneMethod.RowId = methodRowId;

        Mock<ILoader> mockLoader = new Mock<ILoader>();
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem = new Mock<IRuntimeTypeSystem>();

        mockLoader.AddModule(module);
        mockRuntimeTypeSystem.AddMethodDesc(oneMethod);
        mockRuntimeTypeSystem.AddMethodTable(builder, methodTable);

        var target = CreateTarget(
            arch,
            builder,
            mockLoader: mockLoader,
            mockRuntimeTypeSystem: mockRuntimeTypeSystem);

        // TEST

        var codeVersions = target.Contracts.CodeVersions;
        Assert.NotNull(codeVersions);

        // Get all ILCodeVersions
        List<ILCodeVersionHandle> ilCodeVersions = codeVersions.GetILCodeVersions(methodDescAddress).ToList();
        Assert.Equal(2, ilCodeVersions.Count);

        // Get the explicit ILCodeVersion and assert that it is in the list of ILCodeVersions
        ILCodeVersionHandle explicitILCodeVersion = codeVersions.GetActiveILCodeVersion(methodDescAddress);
        Assert.Contains(ilCodeVersions, ilcodeVersion => ilcodeVersion.Equals(explicitILCodeVersion));
        Assert.True(explicitILCodeVersion.IsValid);

        // Find the other ILCodeVersion (synthetic) and assert that it is valid.
        ILCodeVersionHandle syntheticILcodeVersion = ilCodeVersions.Find(ilCodeVersion => !ilCodeVersion.Equals(explicitILCodeVersion));
        Assert.True(syntheticILcodeVersion.IsValid);

        // Verify getting ILCode is equal to ILCode from NativeCode from ILCode.
        NativeCodeVersionHandle explicitNativeCodeVersion = codeVersions.GetActiveNativeCodeVersionForILCodeVersion(methodDescAddress, explicitILCodeVersion);
        Assert.True(explicitILCodeVersion.Equals(codeVersions.GetILCodeVersion(explicitNativeCodeVersion)));

        NativeCodeVersionHandle syntheticNativeCodeVersion = codeVersions.GetActiveNativeCodeVersionForILCodeVersion(methodDescAddress, syntheticILcodeVersion);
        Assert.True(syntheticILcodeVersion.Equals(codeVersions.GetILCodeVersion(syntheticNativeCodeVersion)));
    }

    private void GetGCStressCodeCopy_Impl(MockTarget.Architecture arch, bool returnsNull)
    {
        MockCodeVersions builder = new(arch);
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem = new();

        // Setup synthetic NativeCodeVersion
        TargetPointer expectedSyntheticCodeCopyAddr = returnsNull ? TargetPointer.Null : new(0x2345_6789);
        TargetPointer syntheticMethodDescAddr = new(0x2345_8000);
        NativeCodeVersionHandle syntheticHandle = NativeCodeVersionHandle.CreateSynthetic(syntheticMethodDescAddr);
        MethodDescHandle methodDescHandle = new MethodDescHandle(syntheticMethodDescAddr);
        mockRuntimeTypeSystem.Setup(rts => rts.GetMethodDescHandle(syntheticMethodDescAddr)).Returns(methodDescHandle);
        mockRuntimeTypeSystem.Setup(rts => rts.GetGCStressCodeCopy(methodDescHandle)).Returns(expectedSyntheticCodeCopyAddr);

        // Setup explicit NativeCodeVersion
        TargetPointer? explicitGCCoverageInfoAddr = returnsNull ? TargetPointer.Null : new(0x1234_5678);
        MockNativeCodeVersionNode nativeCodeVersionNode = builder.AddNativeCodeVersionNode();
        nativeCodeVersionNode.Next = 0;
        nativeCodeVersionNode.MethodDesc = 0;
        nativeCodeVersionNode.NativeCode = 0;
        nativeCodeVersionNode.Flags = MockNativeCodeVersionNode.IsActiveChildFlag;
        nativeCodeVersionNode.ILVersionId = 1;
        nativeCodeVersionNode.GCCoverageInfo = explicitGCCoverageInfoAddr.GetValueOrDefault().Value;
        NativeCodeVersionHandle explicitHandle = NativeCodeVersionHandle.CreateExplicit(nativeCodeVersionNode.Address);

        var target = CreateTarget(arch, builder, mockRuntimeTypeSystem: mockRuntimeTypeSystem);

        // TEST
        var codeVersions = target.Contracts.CodeVersions;
        TargetPointer actualSyntheticCodeCopyAddr = codeVersions.GetGCStressCodeCopy(syntheticHandle);
        Assert.Equal(expectedSyntheticCodeCopyAddr, actualSyntheticCodeCopyAddr);

        if(returnsNull)
        {
            TargetPointer actualExplicitCodeCopyAddr = codeVersions.GetGCStressCodeCopy(explicitHandle);
            Assert.Equal(TargetPointer.Null, actualExplicitCodeCopyAddr);
        }
        else
        {
            Target.TypeInfo gcCoverageInfoType = target.GetTypeInfo(DataType.GCCoverageInfo);
            TargetPointer expectedExplicitCodeCopyAddr = explicitGCCoverageInfoAddr.Value + (ulong)gcCoverageInfoType.Fields["SavedCode"].Offset;
            TargetPointer actualExplicitCodeCopyAddr = codeVersions.GetGCStressCodeCopy(explicitHandle);
            Assert.Equal(expectedExplicitCodeCopyAddr, actualExplicitCodeCopyAddr);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCStressCodeCopy_Null(MockTarget.Architecture arch)
    {
        GetGCStressCodeCopy_Impl(arch, returnsNull: true);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCStressCodeCopy_NotNull(MockTarget.Architecture arch)
    {
        GetGCStressCodeCopy_Impl(arch, returnsNull: false);
    }

    public static IEnumerable<object[]> GetOptimizationTierValues()
    {
        foreach (var archData in new MockTarget.StdArch())
        {
            var arch = (MockTarget.Architecture)archData[0];
            yield return [arch, 0u, OptimizationTier.OptimizationTier0];
            yield return [arch, 1u, OptimizationTier.OptimizationTier1];
            yield return [arch, 2u, OptimizationTier.OptimizationTier1OSR];
            yield return [arch, 3u, OptimizationTier.OptimizationTierOptimized];
            yield return [arch, 4u, OptimizationTier.OptimizationTier0Instrumented];
            yield return [arch, 5u, OptimizationTier.OptimizationTier1Instrumented];
            yield return [arch, 0xFFFFFFFFu, OptimizationTier.OptimizationTierUnknown];
        }
    }

    [Theory]
    [MemberData(nameof(GetOptimizationTierValues))]
    public void GetOptimizationTier_Explicit(MockTarget.Architecture arch, uint nativeTier, OptimizationTier expectedTier)
    {
        MockCodeVersions builder = new(arch);

        MockNativeCodeVersionNode nativeCodeVersionNode = builder.AddNativeCodeVersionNode();
        builder.FillNativeCodeVersionNode(
            nativeCodeVersionNode,
            methodDesc: new TargetPointer(0x1a0a_0000),
            nativeCode: new TargetCodePointer(0x0a0a_0000),
            next: TargetPointer.Null,
            isActive: true,
            ilVersionId: 1,
            optimizationTier: nativeTier);

        var target = CreateTarget(arch, builder);
        var codeVersions = target.Contracts.CodeVersions;

        NativeCodeVersionHandle handle = NativeCodeVersionHandle.CreateExplicit(nativeCodeVersionNode.Address);
        OptimizationTier tier = codeVersions.GetOptimizationTier(handle);
        Assert.Equal(expectedTier, tier);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetOptimizationTier_Synthetic_DelegatesToRuntimeTypeSystem(MockTarget.Architecture arch)
    {
        MockCodeVersions builder = new(arch);
        TargetPointer methodDescAddress = new(0x1a0a_0000);

        Mock<IRuntimeTypeSystem> mockRTS = new();
        MethodDescHandle mdHandle = new(methodDescAddress);
        mockRTS.Setup(r => r.GetMethodDescHandle(methodDescAddress)).Returns(mdHandle);
        mockRTS.Setup(r => r.GetMethodDescOptimizationTier(mdHandle)).Returns(OptimizationTier.OptimizationTierOptimized);

        var target = CreateTarget(arch, builder, mockRuntimeTypeSystem: mockRTS);
        var codeVersions = target.Contracts.CodeVersions;

        NativeCodeVersionHandle handle = NativeCodeVersionHandle.CreateSynthetic(methodDescAddress);
        OptimizationTier tier = codeVersions.GetOptimizationTier(handle);
        Assert.Equal(OptimizationTier.OptimizationTierOptimized, tier);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetOptimizationTier_Synthetic_NoCodeData(MockTarget.Architecture arch)
    {
        MockCodeVersions builder = new(arch);
        TargetPointer methodDescAddress = new(0x1a0a_0000);

        Mock<IRuntimeTypeSystem> mockRTS = new();
        MethodDescHandle mdHandle = new(methodDescAddress);
        mockRTS.Setup(r => r.GetMethodDescHandle(methodDescAddress)).Returns(mdHandle);
        mockRTS.Setup(r => r.GetMethodDescOptimizationTier(mdHandle)).Returns(OptimizationTier.OptimizationTierUnknown);

        var target = CreateTarget(arch, builder, mockRuntimeTypeSystem: mockRTS);
        var codeVersions = target.Contracts.CodeVersions;

        NativeCodeVersionHandle handle = NativeCodeVersionHandle.CreateSynthetic(methodDescAddress);
        OptimizationTier tier = codeVersions.GetOptimizationTier(handle);
        Assert.Equal(OptimizationTier.OptimizationTierUnknown, tier);
    }
}
