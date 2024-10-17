// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class CodeVersionsTests
{

    internal class MockMethodDesc
    {
        public TargetPointer Address { get; private set; }
        public bool IsVersionable { get; private set; }

        // only non-null if IsVersionable is false
        public TargetCodePointer NativeCode { get; private set; }

        // only non-null if IsVersionable is true
        public TargetPointer MethodDescVersioningState { get; private set; }

        public static MockMethodDesc CreateNonVersionable (TargetPointer selfAddress, TargetCodePointer nativeCode)
        {
            return new MockMethodDesc() {
                Address = selfAddress,
                IsVersionable = false,
                NativeCode = nativeCode,
                MethodDescVersioningState = TargetPointer.Null,
            };
        }

        public static MockMethodDesc CreateVersionable (TargetPointer selfAddress, TargetPointer methodDescVersioningState)
        {
            return new MockMethodDesc() {
                Address = selfAddress,
                IsVersionable = true,
                NativeCode = TargetCodePointer.Null,
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

        CodeBlockHandle? IExecutionManager.GetCodeBlockHandle(TargetCodePointer ip)
        {
            if (ip == TargetCodePointer.Null)
            {
                return null;
            }
            foreach (var block in _codeBlocks)
            {
                if (block.Contains(ip))
                {
                    return new CodeBlockHandle(ip.AsTargetPointer);
                }
            }
            return null;
        }

        TargetCodePointer IExecutionManager.GetStartAddress(CodeBlockHandle codeInfoHandle)
        {
            foreach (var block in _codeBlocks)
            {
                if (block.Contains(codeInfoHandle.Address))
                {
                    return block.StartAddress;
                }
            }
            return TargetCodePointer.Null;
        }

        TargetPointer IExecutionManager.GetMethodDesc(CodeBlockHandle codeInfoHandle)
        {
            foreach (var block in _codeBlocks)
            {
                if (block.Contains(codeInfoHandle.Address))
                {
                    return block.MethodDescAddress;
                }
            }
            return TargetPointer.Null;
        }
    }

    internal class MockRuntimeTypeSystem : IRuntimeTypeSystem
    {
        IReadOnlyCollection<MockMethodDesc> _methodDescs;
        public MockRuntimeTypeSystem(IReadOnlyCollection<MockMethodDesc> methodDescs)
        {
            _methodDescs = methodDescs;
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

        private MockMethodDesc FindMethodDesc(TargetPointer targetPointer) => TryFindMethodDesc(targetPointer) ?? throw new InvalidOperationException($"MethodDesc not found for {targetPointer}");

        MethodDescHandle IRuntimeTypeSystem.GetMethodDescHandle(TargetPointer targetPointer) => new MethodDescHandle(FindMethodDesc(targetPointer).Address);

        bool IRuntimeTypeSystem.IsVersionable(MethodDescHandle methodDesc) => FindMethodDesc(methodDesc.Address).IsVersionable;
        TargetCodePointer IRuntimeTypeSystem.GetNativeCode(MethodDescHandle methodDesc) => FindMethodDesc(methodDesc.Address).NativeCode;
        TargetPointer IRuntimeTypeSystem.GetMethodDescVersioningState(MethodDescHandle methodDesc) => FindMethodDesc(methodDesc.Address).MethodDescVersioningState;

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

    }

    internal class CVTestTarget : TestPlaceholderTarget
    {
        public CVTestTarget(MockTarget.Architecture arch, IReadOnlyCollection<MockMethodDesc>? methodDescs = null,
                            IReadOnlyCollection<MockCodeBlockStart>? codeBlocks = null,
                            ReadFromTargetDelegate reader = null,
                            Dictionary<DataType, TypeInfo>? typeInfoCache = null) : base(arch) {
            IContractFactory<ICodeVersions> cvfactory = new CodeVersionsFactory();
            IExecutionManager mockExecutionManager = new MockExecutionManager(codeBlocks ?? []);
            IRuntimeTypeSystem mockRuntimeTypeSystem = new MockRuntimeTypeSystem(methodDescs ?? []);
            if (reader != null)
                SetDataReader(reader);
            if (typeInfoCache != null)
                SetTypeInfoCache(typeInfoCache);
            SetDataCache(new DefaultDataCache(this));
            SetContracts(new TestRegistry() {
                CodeVersionsContract = new (() => cvfactory.CreateContract(this, 1)),
                ExecutionManagerContract = new (() => mockExecutionManager),
                RuntimeTypeSystemContract = new (() => mockRuntimeTypeSystem),
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

        var target = new CVTestTarget(arch, [oneMethod], [oneBlock]);
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
        TargetPointer methodDescVersioningStateAddress = builder.AddMethodDescVersioningState(TargetPointer.Null/*FIXME*/);
        TargetCodePointer codeBlockStart = new TargetCodePointer(0x0a0a_0000);
        MockMethodDesc oneMethod = MockMethodDesc.CreateVersionable(selfAddress: new TargetPointer(0x1a0a_0000), methodDescVersioningState: methodDescVersioningStateAddress);
        MockCodeBlockStart oneBlock = new MockCodeBlockStart()
        {
            StartAddress = codeBlockStart,
            Length = 0x100,
            MethodDesc = oneMethod,
        };

        builder.MarkCreated();
        var target = new CVTestTarget(arch, [oneMethod], [oneBlock], builder.Builder.GetReadContext().ReadFromTarget, builder.TypeInfoCache);

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

}
