// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

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
        public uint MethodToken => 0x06000000 | RowId;

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

    internal class CodeVersionsBuilder
    {
        internal readonly MockMemorySpace.Builder Builder;
        internal readonly Dictionary<DataType, Target.TypeInfo> TypeInfoCache = new();

        internal struct AllocationRange
        {
            public ulong CodeVersionsRangeStart;
            public ulong CodeVersionsRangeEnd;
        }

        public  static readonly AllocationRange DefaultAllocationRange = new AllocationRange() {
            CodeVersionsRangeStart = 0x000f_c000,
            CodeVersionsRangeEnd = 0x00010_0000,
        };

        private readonly MockMemorySpace.BumpAllocator _codeVersionsAllocator;

        public CodeVersionsBuilder(MockTarget.Architecture arch, AllocationRange allocationRange) : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), allocationRange)
        {}
        public CodeVersionsBuilder(MockMemorySpace.Builder builder, AllocationRange allocationRange, Dictionary<DataType, Target.TypeInfo>? typeInfoCache = null)
        {
            Builder = builder;
            _codeVersionsAllocator = Builder.CreateAllocator(allocationRange.CodeVersionsRangeStart, allocationRange.CodeVersionsRangeEnd);
            TypeInfoCache = typeInfoCache ?? CreateTypeInfoCache(Builder.TargetTestHelpers);
        }

        internal static Dictionary<DataType, Target.TypeInfo> CreateTypeInfoCache(TargetTestHelpers targetTestHelpers)
        {
            Dictionary<DataType, Target.TypeInfo> typeInfoCache = new();
            AddToTypeInfoCache(targetTestHelpers, typeInfoCache);
            return typeInfoCache;
        }

        internal static void AddToTypeInfoCache(TargetTestHelpers targetTestHelpers, Dictionary<DataType, Target.TypeInfo> typeInfoCache)
        {
            var layout = targetTestHelpers.LayoutFields([
                (nameof(Data.MethodDescVersioningState.NativeCodeVersionNode), DataType.pointer),
                (nameof(Data.MethodDescVersioningState.Flags), DataType.uint8),
            ]);
            typeInfoCache[DataType.MethodDescVersioningState] = new Target.TypeInfo() {
                    Fields = layout.Fields,
                    Size = layout.Stride,
            };
            layout = targetTestHelpers.LayoutFields([
               (nameof(Data.NativeCodeVersionNode.Next), DataType.pointer),
               (nameof(Data.NativeCodeVersionNode.MethodDesc), DataType.pointer),
               (nameof(Data.NativeCodeVersionNode.NativeCode), DataType.pointer),
            ]);
            typeInfoCache[DataType.NativeCodeVersionNode] = new Target.TypeInfo() {
                    Fields = layout.Fields,
                    Size = layout.Stride,
            };
            layout = targetTestHelpers.LayoutFields([
                (nameof(Data.ILCodeVersioningState.ActiveVersionMethodDef), DataType.uint32),
                (nameof(Data.ILCodeVersioningState.ActiveVersionModule), DataType.pointer),
                (nameof(Data.ILCodeVersioningState.ActiveVersionKind), DataType.uint32),
                (nameof(Data.ILCodeVersioningState.ActiveVersionNode), DataType.pointer),
            ]);
            typeInfoCache[DataType.ILCodeVersioningState] = new Target.TypeInfo() {
                    Fields = layout.Fields,
                    Size = layout.Stride,
            };
        }

        public void MarkCreated() => Builder.MarkCreated();

        public TargetPointer AddMethodDescVersioningState(TargetPointer nativeCodeVersionNode)
        {
            Target.TypeInfo info = TypeInfoCache[DataType.MethodDescVersioningState];
            MockMemorySpace.HeapFragment fragment = _codeVersionsAllocator.Allocate((ulong)TypeInfoCache[DataType.MethodDescVersioningState].Size, "MethodDescVersioningState");
            Builder.AddHeapFragment(fragment);
            Span<byte> mdvs = Builder.BorrowAddressRange(fragment.Address, fragment.Data.Length);
            Builder.TargetTestHelpers.WritePointer(mdvs.Slice(info.Fields[nameof(Data.MethodDescVersioningState.NativeCodeVersionNode)].Offset, Builder.TargetTestHelpers.PointerSize), nativeCodeVersionNode);
            return fragment.Address;
        }

        public TargetPointer AddNativeCodeVersionNode()
        {
            Target.TypeInfo info = TypeInfoCache[DataType.NativeCodeVersionNode];
            MockMemorySpace.HeapFragment fragment = _codeVersionsAllocator.Allocate((ulong)TypeInfoCache[DataType.NativeCodeVersionNode].Size, "NativeCodeVersionNode");
            Builder.AddHeapFragment(fragment);
            return fragment.Address;
        }
        public void FillNativeCodeVersionNode(TargetPointer dest, TargetPointer methodDesc, TargetCodePointer nativeCode, TargetPointer next)
        {
            Target.TypeInfo info = TypeInfoCache[DataType.NativeCodeVersionNode];
            Span<byte> ncvn = Builder.BorrowAddressRange(dest, (int)info.Size!);
            Builder.TargetTestHelpers.WritePointer(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.Next)].Offset, Builder.TargetTestHelpers.PointerSize), next);
            Builder.TargetTestHelpers.WritePointer(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.MethodDesc)].Offset, Builder.TargetTestHelpers.PointerSize), methodDesc);
            Builder.TargetTestHelpers.WritePointer(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.NativeCode)].Offset, Builder.TargetTestHelpers.PointerSize), nativeCode);
        }

        public TargetPointer AddILCodeVersioningState(uint activeVersionKind, TargetPointer activeVersionNode, TargetPointer activeVersionModule, uint activeVersionMethodDef)
        {
            Target.TypeInfo info = TypeInfoCache[DataType.ILCodeVersioningState];
            MockMemorySpace.HeapFragment fragment = _codeVersionsAllocator.Allocate((ulong)TypeInfoCache[DataType.ILCodeVersioningState].Size, "ILCodeVersioningState");
            Builder.AddHeapFragment(fragment);
            Span<byte> ilcvs = Builder.BorrowAddressRange(fragment.Address, fragment.Data.Length);
            Builder.TargetTestHelpers.WritePointer(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionModule)].Offset, Builder.TargetTestHelpers.PointerSize), activeVersionModule);
            Builder.TargetTestHelpers.WritePointer(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionNode)].Offset, Builder.TargetTestHelpers.PointerSize), activeVersionNode);
            Builder.TargetTestHelpers.Write(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionMethodDef)].Offset, sizeof(uint)), activeVersionMethodDef);
            Builder.TargetTestHelpers.Write(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionKind)].Offset, sizeof(uint)), activeVersionKind);
            return fragment.Address;
        }

    }

    internal class CVTestTarget : TestPlaceholderTarget
    {
        public static CVTestTarget FromBuilder(MockTarget.Architecture arch, IReadOnlyCollection<MockMethodDesc> methodDescs, IReadOnlyCollection<MockMethodTable> methodTables, IReadOnlyCollection<MockCodeBlockStart> codeBlocks, IReadOnlyCollection<MockModule> modules, CodeVersionsBuilder builder)
        {
            builder.MarkCreated();
            return new CVTestTarget(arch, reader: builder.Builder.GetReadContext().ReadFromTarget, typeInfoCache: builder.TypeInfoCache,
                                    methodDescs: methodDescs, methodTables: methodTables, codeBlocks: codeBlocks, modules: modules);
        }

        public CVTestTarget(MockTarget.Architecture arch, IReadOnlyCollection<MockMethodDesc>? methodDescs = null,
                            IReadOnlyCollection<MockMethodTable>? methodTables = null,
                            IReadOnlyCollection<MockCodeBlockStart>? codeBlocks = null,
                            IReadOnlyCollection<MockModule>? modules = null,
                            ReadFromTargetDelegate reader = null,
                            Dictionary<DataType, TypeInfo>? typeInfoCache = null) : base(arch) {
            IExecutionManager mockExecutionManager = new MockExecutionManager(codeBlocks ?? []);
            IRuntimeTypeSystem mockRuntimeTypeSystem = new MockRuntimeTypeSystem(this, methodDescs ?? [], methodTables ?? []);
            ILoader loader = new MockLoader(modules ?? []);
            if (reader != null)
                SetDataReader(reader);
            if (typeInfoCache != null)
                SetTypeInfoCache(typeInfoCache);
            SetDataCache(new DefaultDataCache(this));
            IContractFactory<ICodeVersions> cvfactory = new CodeVersionsFactory();
            SetContracts(new TestRegistry() {
                CodeVersionsContract = new (() => cvfactory.CreateContract(this, 1)),
                ExecutionManagerContract = new (() => mockExecutionManager),
                RuntimeTypeSystemContract = new (() => mockRuntimeTypeSystem),
                LoaderContract = new (() => loader),
            });
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TestGetNativeCodeVersionNull(MockTarget.Architecture arch)
    {
        var target = new CVTestTarget(arch);
        var codeVersions = target.Contracts.CodeVersions;

        Assert.NotNull(codeVersions);

        TargetCodePointer nullPointer = TargetCodePointer.Null;

        var handle = codeVersions.GetNativeCodeVersionForIP(nullPointer);
        Assert.False(handle.Valid);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TestGetNativeCodeVersionOneVersionNonVersionable(MockTarget.Architecture arch)
    {
        TargetCodePointer codeBlockStart = new TargetCodePointer(0x0a0a_0000);
        MockMethodDesc oneMethod = MockMethodDesc.CreateNonVersionable(selfAddress: new TargetPointer(0x1a0a_0000), nativeCode: codeBlockStart);
        MockCodeBlockStart oneBlock = new MockCodeBlockStart()
        {
            StartAddress = codeBlockStart,
            Length = 0x100,
            MethodDesc = oneMethod,
        };

        var target = new CVTestTarget(arch, methodDescs: [oneMethod], codeBlocks: [oneBlock]);
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
    public void TestGetNativeCodeVersionOneVersionVersionable(MockTarget.Architecture arch)
    {
        var builder = new CodeVersionsBuilder(arch, CodeVersionsBuilder.DefaultAllocationRange);
        TargetPointer nativeCodeVersionNode = builder.AddNativeCodeVersionNode();
        TargetPointer methodDescVersioningStateAddress = builder.AddMethodDescVersioningState(nativeCodeVersionNode);
        TargetCodePointer codeBlockStart = new TargetCodePointer(0x0a0a_0000);
        MockMethodDesc oneMethod = MockMethodDesc.CreateVersionable(selfAddress: new TargetPointer(0x1a0a_0000), methodDescVersioningState: methodDescVersioningStateAddress);
        MockCodeBlockStart oneBlock = new MockCodeBlockStart()
        {
            StartAddress = codeBlockStart,
            Length = 0x100,
            MethodDesc = oneMethod,
        };
        builder.FillNativeCodeVersionNode(nativeCodeVersionNode, methodDesc: oneMethod.Address, nativeCode: codeBlockStart, next: TargetPointer.Null);

        var target = CVTestTarget.FromBuilder(arch, [oneMethod], [], [oneBlock], [], builder);

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
    public void TestGetActiveNativeCodeVersionDefaultCase(MockTarget.Architecture arch)
    {
        uint methodRowId = 0x25; // arbitrary
        TargetCodePointer expectedNativeCodePointer = new TargetCodePointer(0x0700_abc0);
        uint methodDefToken = 0x06000000 | methodRowId;
        var builder = new CodeVersionsBuilder(arch, CodeVersionsBuilder.DefaultAllocationRange);
        var methodDescAddress = new TargetPointer(0x00aa_aa00);
        var moduleAddress = new TargetPointer(0x00ca_ca00);


        TargetPointer versioningState = builder.AddILCodeVersioningState(activeVersionKind: 0/*==unknown*/, activeVersionNode: TargetPointer.Null, activeVersionModule: moduleAddress, activeVersionMethodDef: methodDefToken);
        var oneModule = new MockModule() {
            Address = moduleAddress,
            MethodDefToILCodeVersioningStateAddress = new TargetPointer(0x00da_da00),
            MethodDefToILCodeVersioningStateTable = new Dictionary<uint, TargetPointer>() {
                { methodRowId, versioningState}
            },
        };
        var oneMethodTable = new MockMethodTable() {
            Address = new TargetPointer(0x00ba_ba00),
            Module = oneModule,
        };
        var oneMethod = MockMethodDesc.CreateVersionable(selfAddress: methodDescAddress, methodDescVersioningState: TargetPointer.Null, nativeCode: expectedNativeCodePointer);
        oneMethod.MethodTable = oneMethodTable;
        oneMethod.RowId = methodRowId;

        var target = CVTestTarget.FromBuilder(arch, [oneMethod], [oneMethodTable], [], [oneModule], builder);

        // TEST

        var codeVersions = target.Contracts.CodeVersions;

        Assert.NotNull(codeVersions);

        var handle = codeVersions.GetActiveNativeCodeVersion(methodDescAddress);
        Assert.True(handle.Valid);
        var actualCodeAddress = codeVersions.GetNativeCode(handle);
        Assert.Equal(expectedNativeCodePointer, actualCodeAddress);
    }

}
