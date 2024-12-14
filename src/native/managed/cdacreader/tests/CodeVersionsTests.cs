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

using MockCodeVersions = MockDescriptors.CodeVersions;

public class CodeVersionsTests
{
    internal class MockModule
    {
        public TargetPointer Address { get; set; }
        public TargetPointer MethodDefToILCodeVersioningStateAddress { get; set; }
        public Dictionary<uint, TargetPointer> MethodDefToILCodeVersioningStateTable {get; set;}
    }
    internal class MockMethodTable
    {
        public TargetPointer Address { get; set; }
        public MockModule? Module {get; set; }
    }

    internal class MockMethodDesc
    {
        public TargetPointer Address { get; private set; }
        public bool IsVersionable { get; private set; }

        public uint RowId { get; set; }
        public uint MethodToken => EcmaMetadataUtils.CreateMethodDef(RowId);

        // n.b. in the real RuntimeTypeSystem_1 this is more complex
        public TargetCodePointer NativeCode { get; private set; }

        // only non-null if IsVersionable is true
        public TargetPointer MethodDescVersioningState { get; private set; }

        public MockMethodTable? MethodTable { get; set; }

        public static MockMethodDesc CreateNonVersionable (TargetPointer selfAddress, TargetCodePointer nativeCode)
        {
            return new MockMethodDesc() {
                Address = selfAddress,
                IsVersionable = false,
                NativeCode = nativeCode,
                MethodDescVersioningState = TargetPointer.Null,
            };
        }

        public static MockMethodDesc CreateVersionable (TargetPointer selfAddress, TargetPointer methodDescVersioningState, TargetCodePointer nativeCode = default)
        {
            return new MockMethodDesc() {
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

        public bool Contains(TargetPointer ip) =>  ip >= StartAddress && ip < StartAddress + Length;
        public bool Contains(TargetCodePointer ip) => Contains(ip.AsTargetPointer);
        public MockMethodDesc MethodDesc {get; set;}
        public TargetPointer MethodDescAddress => MethodDesc.Address;
    }

    internal class MockExecutionManager : IExecutionManager
    {
        private IReadOnlyCollection<MockCodeBlockStart> _codeBlocks;

        public MockExecutionManager(IReadOnlyCollection<MockCodeBlockStart> codeBlocks)
        {
            _codeBlocks = codeBlocks;
        }

        private MockCodeBlockStart? FindCodeBlock(TargetPointer ip)
        {
            if (ip == TargetPointer.Null)
            {
                return null;
            }
            foreach (var block in _codeBlocks)
            {
                if (block.Contains(ip))
                {
                    return block;
                }
            }
            return null;
        }

        CodeBlockHandle? IExecutionManager.GetCodeBlockHandle(TargetCodePointer ip)
        {
            var block = FindCodeBlock(ip.AsTargetPointer);
            if (block == null)
                return null;
            return new CodeBlockHandle(ip.AsTargetPointer);
        }

        TargetCodePointer IExecutionManager.GetStartAddress(CodeBlockHandle codeInfoHandle) => FindCodeBlock(codeInfoHandle.Address)?.StartAddress ?? TargetCodePointer.Null;
        TargetPointer IExecutionManager.GetMethodDesc(CodeBlockHandle codeInfoHandle) => FindCodeBlock(codeInfoHandle.Address)?.MethodDescAddress ?? TargetPointer.Null;
    }

    internal class MockRuntimeTypeSystem : IRuntimeTypeSystem
    {
        private readonly Target _target;
        IReadOnlyCollection<MockMethodDesc> _methodDescs;
        IReadOnlyCollection<MockMethodTable> _methodTables;
        public MockRuntimeTypeSystem(Target target, IReadOnlyCollection<MockMethodDesc> methodDescs, IReadOnlyCollection<MockMethodTable> methodTables)
        {
            _target = target;
            _methodDescs = methodDescs;
            _methodTables = methodTables;
        }

        private MockMethodDesc? TryFindMethodDesc(TargetPointer targetPointer)
        {
            foreach (var methodDesc in _methodDescs)
            {
                if (methodDesc.Address == targetPointer)
                {
                    return methodDesc;
                }
            }
            return null;
        }

        private MockMethodTable? TryFindMethodTable(TargetPointer targetPointer)
        {
            foreach (var methodTable in _methodTables)
            {
                if (methodTable.Address == targetPointer)
                {
                    return methodTable;
                }
            }
            return null;
        }

        private MockMethodDesc FindMethodDesc(TargetPointer targetPointer) => TryFindMethodDesc(targetPointer) ?? throw new InvalidOperationException($"MethodDesc not found for {targetPointer}");
        private MockMethodTable FindMethodTable(TargetPointer targetPointer) => TryFindMethodTable(targetPointer) ?? throw new InvalidOperationException($"MethodTable not found for {targetPointer}");

        MethodDescHandle IRuntimeTypeSystem.GetMethodDescHandle(TargetPointer targetPointer) => new MethodDescHandle(FindMethodDesc(targetPointer).Address);

        bool IRuntimeTypeSystem.IsVersionable(MethodDescHandle methodDesc) => FindMethodDesc(methodDesc.Address).IsVersionable;
        TargetCodePointer IRuntimeTypeSystem.GetNativeCode(MethodDescHandle methodDesc) => FindMethodDesc(methodDesc.Address).NativeCode;
        TargetPointer IRuntimeTypeSystem.GetMethodDescVersioningState(MethodDescHandle methodDesc) => FindMethodDesc(methodDesc.Address).MethodDescVersioningState;

        TargetPointer IRuntimeTypeSystem.GetMethodTable(MethodDescHandle methodDesc) => FindMethodDesc(methodDesc.Address).MethodTable?.Address ?? throw new InvalidOperationException($"MethodTable not found for {methodDesc.Address}");
        uint IRuntimeTypeSystem.GetMethodToken(MethodDescHandle methodDesc) => FindMethodDesc(methodDesc.Address).MethodToken;

        TypeHandle IRuntimeTypeSystem.GetTypeHandle(TargetPointer address)
        {
            ulong addressLowBits = (ulong)address & ((ulong)_target.PointerSize - 1);

            // no typedescs for now, just method tables with 0 in the low bits
            if (addressLowBits != 0)
            {
                throw new InvalidOperationException("Invalid type handle pointer");
            }
            MockMethodTable methodTable = FindMethodTable(address);
            return new TypeHandle(methodTable.Address);
        }

        TargetPointer IRuntimeTypeSystem.GetModule(TypeHandle typeHandle) => FindMethodTable(typeHandle.Address).Module?.Address ?? throw new InvalidOperationException($"Module not found for {typeHandle.Address}");
    }

    internal class MockLoader : ILoader
    {
        private readonly IReadOnlyCollection<MockModule> _modules;
        public MockLoader(IReadOnlyCollection<MockModule> modules)
        {
            _modules = modules;
        }
        private MockModule? TryFindModule(TargetPointer targetPointer)
        {
            foreach (var module in _modules)
            {
                if (module.Address == targetPointer)
                {
                    return module;
                }
            }
            return null;
        }

        private MockModule FindModule(TargetPointer targetPointer) => TryFindModule(targetPointer) ?? throw new InvalidOperationException($"Module not found for {targetPointer}");

        Contracts.ModuleHandle ILoader.GetModuleHandle(TargetPointer modulePointer) => new Contracts.ModuleHandle(FindModule(modulePointer).Address);

        ModuleLookupTables ILoader.GetLookupTables(Contracts.ModuleHandle handle)
        {
            MockModule module = FindModule(handle.Address);
            return new ModuleLookupTables() {
                MethodDefToILCodeVersioningState = module.MethodDefToILCodeVersioningStateAddress,
            };
        }

        TargetPointer ILoader.GetModuleLookupMapElement(TargetPointer tableAddress, uint token, out TargetNUInt flags)
        {
            flags = new TargetNUInt(0);
            Dictionary<uint,TargetPointer>? table = null;
            foreach (var module in _modules)
            {
                if (module.MethodDefToILCodeVersioningStateTable != null && module.MethodDefToILCodeVersioningStateAddress == tableAddress)
                {
                    table = module.MethodDefToILCodeVersioningStateTable;
                }
            }
            if (table == null) {
                throw new InvalidOperationException($"No table found with address {tableAddress} for token 0x{token:x}, {flags}");
            }
            uint rowId = EcmaMetadataUtils.GetRowId(token);
            if (table.TryGetValue(rowId, out TargetPointer value))
            {
                return value;
            }
            throw new InvalidOperationException($"No token found for 0x{token:x} in table {tableAddress}");
        }
    }

    internal static Target CreateTarget(
        MockTarget.Architecture arch,
        IReadOnlyCollection<MockMethodDesc> methodDescs = null,
        IReadOnlyCollection<MockMethodTable> methodTables = null,
        IReadOnlyCollection<MockCodeBlockStart> codeBlocks = null,
        IReadOnlyCollection<MockModule> modules = null,
        MockCodeVersions builder = null)
    {
        TestPlaceholderTarget target = builder != null
            ? new TestPlaceholderTarget(arch, builder.Builder.GetReadContext().ReadFromTarget, builder.Types)
            : new TestPlaceholderTarget(arch, null);

        IExecutionManager mockExecutionManager = new MockExecutionManager(codeBlocks ?? []);
        IRuntimeTypeSystem mockRuntimeTypeSystem = new MockRuntimeTypeSystem(target, methodDescs ?? [], methodTables ?? []);
        ILoader loader = new MockLoader(modules ?? []);
        IContractFactory<ICodeVersions> cvfactory = new CodeVersionsFactory();
        ContractRegistry reg = Mock.Of<ContractRegistry>(
            c => c.CodeVersions == cvfactory.CreateContract(target, 1)
                && c.ExecutionManager == mockExecutionManager
                && c.RuntimeTypeSystem == mockRuntimeTypeSystem
                && c.Loader == loader);
        target.SetContracts(reg);
        return target;
    }

    internal static Target CreateTarget(
        MockTarget.Architecture arch,
        MockCodeVersions builder,
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget(
            arch,
            builder.Builder.GetReadContext().ReadFromTarget,
            builder.Types);

        IContractFactory<ICodeVersions> cvfactory = new CodeVersionsFactory();
        ContractRegistry reg = Mock.Of<ContractRegistry>(
            c => c.CodeVersions == cvfactory.CreateContract(target, 1)
                && c.RuntimeTypeSystem == mockRuntimeTypeSystem.Object);
        target.SetContracts(reg);
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetNativeCodeVersion_Null(MockTarget.Architecture arch)
    {
        var target = CreateTarget(arch);
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
        MockMethodDesc oneMethod = MockMethodDesc.CreateNonVersionable(selfAddress: new TargetPointer(0x1a0a_0000), nativeCode: codeBlockStart);
        MockCodeBlockStart oneBlock = new MockCodeBlockStart()
        {
            StartAddress = codeBlockStart,
            Length = 0x100,
            MethodDesc = oneMethod,
        };

        var target = CreateTarget(arch, methodDescs: [oneMethod], codeBlocks: [oneBlock]);
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
        TargetPointer nativeCodeVersionNode = builder.AddNativeCodeVersionNode();
        TargetPointer methodDescVersioningStateAddress = builder.AddMethodDescVersioningState(nativeCodeVersionNode, true);
        TargetCodePointer codeBlockStart = new TargetCodePointer(0x0a0a_0000);
        MockMethodDesc oneMethod = MockMethodDesc.CreateVersionable(selfAddress: new TargetPointer(0x1a0a_0000), methodDescVersioningState: methodDescVersioningStateAddress);
        MockCodeBlockStart oneBlock = new MockCodeBlockStart()
        {
            StartAddress = codeBlockStart,
            Length = 0x100,
            MethodDesc = oneMethod,
        };
        builder.FillNativeCodeVersionNode(nativeCodeVersionNode, methodDesc: oneMethod.Address, nativeCode: codeBlockStart, next: TargetPointer.Null, isActive: false, ilVersionId: default);

        var target = CreateTarget(arch, [oneMethod], [], [oneBlock], [], builder);

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

        TargetPointer versioningState = builder.AddILCodeVersioningState(
            activeVersionKind: 0/*==unknown*/,
            activeVersionNode: TargetPointer.Null,
            activeVersionModule: moduleAddress,
            activeVersionMethodDef: methodDefToken,
            firstVersionNode: TargetPointer.Null);
        var module = new MockModule() {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, versioningState}
            },
        };
        var methodTable = new MockMethodTable() {
            Address = new TargetPointer(0x00ba_ba00),
            Module = module,
        };
        var method = MockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: TargetPointer.Null, nativeCode: expectedNativeCodePointer);
        method.MethodTable = methodTable;
        method.RowId = methodRowId;

        var methodNilToken = MockMethodDesc.CreateVersionable(selfAddress: methodDescNilTokenAddress, methodDescVersioningState: TargetPointer.Null, nativeCode: expectedNativeCodePointer);
        methodNilToken.MethodTable = methodTable;

        var target = CreateTarget(arch, [method, methodNilToken], [methodTable], [], [module], builder);

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

        TargetPointer versioningState = builder.AddILCodeVersioningState(
            activeVersionKind: 0/*==unknown*/,
            activeVersionNode: TargetPointer.Null,
            activeVersionModule: moduleAddress,
            activeVersionMethodDef: methodDefToken,
            firstVersionNode: TargetPointer.Null);
        var module = new MockModule()
        {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, versioningState}
            },
        };
        var methodTable = new MockMethodTable()
        {
            Address = new TargetPointer(0x00ba_ba00),
            Module = module,
        };

        // Add the linked list of native code version nodes
        int count = 3;
        int activeIndex = shouldFindActiveCodeVersion ? count - 1 : -1;
        (TargetPointer firstNode, TargetPointer activeVersionNode) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, count, activeIndex, expectedNativeCodePointer, default);
        TargetPointer methodDescVersioningStateAddress = builder.AddMethodDescVersioningState(nativeCodeVersionNode: firstNode, isDefaultVersionActive: false);

        var methodDesc = MockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: methodDescVersioningStateAddress, nativeCode: expectedNativeCodePointer);
        methodDesc.MethodTable = methodTable;
        methodDesc.RowId = methodRowId;

        var target = CreateTarget(arch, [methodDesc], [methodTable], [], [module], builder);

        // TEST

        var codeVersions = target.Contracts.CodeVersions;
        Assert.NotNull(codeVersions);

        NativeCodeVersionHandle handle = codeVersions.GetActiveNativeCodeVersion(methodDescAddress);
        if (shouldFindActiveCodeVersion)
        {
            Assert.True(handle.Valid);
            Assert.Equal(activeVersionNode, handle.CodeVersionNodeAddress);
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
        TargetPointer ilVersionNode = builder.AddILCodeVersionNode(TargetPointer.Null, ilVersionId, /* kStateActive */ 0x00000002);
        TargetPointer versioningState = builder.AddILCodeVersioningState(
            activeVersionKind: 1 /* Explicit */,
            activeVersionNode: ilVersionNode,
            activeVersionModule: TargetPointer.Null,
            activeVersionMethodDef: 0,
            firstVersionNode: ilVersionNode);
        var module = new MockModule()
        {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, versioningState}
            },
        };
        var methodTable = new MockMethodTable()
        {
            Address = new TargetPointer(0x00ba_ba00),
            Module = module,
        };

        // Add the linked list of native code version nodes
        int count = 3;
        int activeIndex = shouldFindActiveCodeVersion ? count - 1 : -1;
        TargetNUInt activeIlVersionId = shouldFindActiveCodeVersion ? ilVersionId : default;
        (TargetPointer firstNode, TargetPointer activeVersionNode) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, count, activeIndex, expectedNativeCodePointer, activeIlVersionId);
        TargetPointer methodDescVersioningStateAddress = builder.AddMethodDescVersioningState(nativeCodeVersionNode: firstNode, isDefaultVersionActive: false);

        var oneMethod = MockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: methodDescVersioningStateAddress, nativeCode: expectedNativeCodePointer);
        oneMethod.MethodTable = methodTable;
        oneMethod.RowId = methodRowId;

        var target = CreateTarget(arch, [oneMethod], [methodTable], [], [module], builder);

        // TEST

        var codeVersions = target.Contracts.CodeVersions;
        Assert.NotNull(codeVersions);

        var handle = codeVersions.GetActiveNativeCodeVersion(methodDescAddress);
        if (shouldFindActiveCodeVersion)
        {
            Assert.True(handle.Valid);
            Assert.Equal(activeVersionNode, handle.CodeVersionNodeAddress);
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
        TargetPointer ilVersionNode = builder.AddILCodeVersionNode(TargetPointer.Null, ilVersionId, /* kStateActive */ 0x00000002);
        TargetPointer versioningState = builder.AddILCodeVersioningState(
            activeVersionKind: 1 /* Explicit */,
            activeVersionNode: ilVersionNode,
            activeVersionModule: TargetPointer.Null,
            activeVersionMethodDef: 0,
            firstVersionNode: ilVersionNode);
        var module = new MockModule()
        {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, versioningState}
            },
        };
        var methodTable = new MockMethodTable()
        {
            Address = new TargetPointer(0x00ba_ba00),
            Module = module,
        };

        (TargetPointer firstNode, _) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, 2, 1, expectedSyntheticCodePointer, new TargetNUInt(0));
        (firstNode, _) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, 2, 1, expectedExplicitCodePointer, ilVersionId, firstNode);

        TargetPointer methodDescVersioningStateAddress = builder.AddMethodDescVersioningState(nativeCodeVersionNode: firstNode, isDefaultVersionActive: false);

        var oneMethod = MockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: methodDescVersioningStateAddress);
        oneMethod.MethodTable = methodTable;
        oneMethod.RowId = methodRowId;

        var target = CreateTarget(arch, [oneMethod], [methodTable], [], [module], builder);

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
        TargetPointer ilVersionNode = builder.AddILCodeVersionNode(TargetPointer.Null, ilVersionId, /* kStateActive */ 0x00000002);
        TargetPointer versioningState = builder.AddILCodeVersioningState(
            activeVersionKind: 1 /* Explicit */,
            activeVersionNode: ilVersionNode,
            activeVersionModule: TargetPointer.Null,
            activeVersionMethodDef: 0,
            firstVersionNode: ilVersionNode);
        var module = new MockModule()
        {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, versioningState}
            },
        };
        var methodTable = new MockMethodTable()
        {
            Address = new TargetPointer(0x00ba_ba00),
            Module = module,
        };

        (TargetPointer firstNode, _) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, 2, 1, expectedSyntheticCodePointer, new TargetNUInt(0));
        (firstNode, _) = builder.AddNativeCodeVersionNodesForMethod(methodDescAddress, 2, 1, expectedExplicitCodePointer, ilVersionId, firstNode);

        TargetPointer methodDescVersioningStateAddress = builder.AddMethodDescVersioningState(nativeCodeVersionNode: firstNode, isDefaultVersionActive: false);

        var oneMethod = MockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: methodDescVersioningStateAddress);
        oneMethod.MethodTable = methodTable;
        oneMethod.RowId = methodRowId;

        var target = CreateTarget(arch, [oneMethod], [methodTable], [], [module], builder);

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
        Mock<IRuntimeTypeSystem> mockRTS = new();

        // Setup synthetic NativeCodeVersion
        TargetPointer expectedSyntheticCodeCopyAddr = returnsNull ? TargetPointer.Null : new(0x2345_6789);
        TargetPointer syntheticMethodDescAddr = new(0x2345_8000);
        NativeCodeVersionHandle syntheticHandle = NativeCodeVersionHandle.CreateSynthetic(syntheticMethodDescAddr);
        MethodDescHandle methodDescHandle = new MethodDescHandle(syntheticMethodDescAddr);
        mockRTS.Setup(rts => rts.GetMethodDescHandle(syntheticMethodDescAddr)).Returns(methodDescHandle);
        mockRTS.Setup(rts => rts.GetGCStressCodeCopy(methodDescHandle)).Returns(expectedSyntheticCodeCopyAddr);

        // Setup explicit NativeCodeVersion
        TargetPointer? explicitGCCoverageInfoAddr = returnsNull ? TargetPointer.Null : new(0x1234_5678);
        TargetPointer nativeCodeVersionNode = builder.AddNativeCodeVersionNode();
        builder.FillNativeCodeVersionNode(
            nativeCodeVersionNode,
            methodDesc: TargetPointer.Null,
            nativeCode: TargetCodePointer.Null,
            next: TargetPointer.Null,
            isActive: true,
            ilVersionId: new(1),
            gcCoverageInfo: explicitGCCoverageInfoAddr);
        NativeCodeVersionHandle explicitHandle = NativeCodeVersionHandle.CreateExplicit(nativeCodeVersionNode);

        var target = CreateTarget(arch, builder, mockRTS);
        var codeVersions = target.Contracts.CodeVersions;

        // TEST
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
}
