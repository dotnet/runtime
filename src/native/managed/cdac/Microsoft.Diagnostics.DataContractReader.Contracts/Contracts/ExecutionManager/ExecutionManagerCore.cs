// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed partial class ExecutionManagerCore<T> : IExecutionManager
    where T : INibbleMap
{
    internal readonly Target _target;

    // maps CodeBlockHandle.Address (which is the CodeHeaderAddress) to the CodeBlock
    private readonly Dictionary<TargetPointer, CodeBlock> _codeInfos = new();
    private readonly TargetPointer _topRangeSectionMapAddress;
    private readonly ExecutionManagerHelpers.RangeSectionMap _rangeSectionMapLookup;
    private readonly EEJitManager _eeJitManager;
    private readonly ReadyToRunJitManager _r2rJitManager;
    private readonly InterpreterJitManager _interpreterJitManager;

    private Data.RangeSectionMap _topRangeSectionMap
        => _target.ProcessedData.GetOrAdd<Data.RangeSectionMap>(_topRangeSectionMapAddress);

    public ExecutionManagerCore(Target target, TargetPointer topRangeSectionMapAddress)
    {
        _target = target;
        _topRangeSectionMapAddress = topRangeSectionMapAddress;
        _rangeSectionMapLookup = ExecutionManagerHelpers.RangeSectionMap.Create(_target);
        INibbleMap nibbleMap = T.Create(_target);
        _eeJitManager = new EEJitManager(_target, nibbleMap);
        _r2rJitManager = new ReadyToRunJitManager(_target);
        _interpreterJitManager = new InterpreterJitManager(_target, nibbleMap);
    }

    public void Flush(FlushScope scope)
    {
        _codeInfos.Clear();
    }

    // Note, because of RelativeOffset, this code info is per code pointer, not per method
    private sealed class CodeBlock
    {
        public TargetPointer StartAddress { get; }
        public TargetPointer MethodDescAddress { get; }
        public TargetPointer JitManagerAddress { get; }
        public TargetNUInt RelativeOffset { get; }
        public CodeBlock(TargetPointer startAddress, TargetPointer methodDesc, TargetNUInt relativeOffset, TargetPointer jitManagerAddress)
        {
            StartAddress = startAddress;
            MethodDescAddress = methodDesc;
            RelativeOffset = relativeOffset;
            JitManagerAddress = jitManagerAddress;
        }

        public bool Valid => JitManagerAddress != TargetPointer.Null;
    }

    [Flags]
    private enum RangeSectionFlags : int
    {
        CodeHeap = 0x02,
        RangeList = 0x04,
        Interpreter = 0x08,
    }

    // Mirrors the native CodeHeap::CodeHeapType enum in codeman.h.
    // Used to interpret the raw byte stored in the target process.
    private enum CodeHeapType : byte
    {
        LoaderCodeHeap  = 0,
        HostCodeHeap    = 1,
        UnknownCodeHeap = 0xff,
    }

    private enum ExceptionClauseFlags_1 : uint
    {
        Filter = 0x1,
        Finally = 0x2,
        Fault = 0x4,
        CachedClass = 0x10000000,
    }

    // Mirrors StubCodeBlockKind in codeman.h
    private enum StubKind : int
    {
        Unknown = 0,
        JumpStub = 1,
        DynamicHelper = 3,
        StubPrecode = 4,
        FixupPrecode = 5,
        VSDDispatchStub = 6,
        VSDResolveStub = 7,
        VSDLookupStub = 8,
        VSDVTableStub = 9,
        CallCountingStub = 10,
    }

    private abstract class JitManager
    {
        public Target Target { get; }

        protected JitManager(Target target)
        {
            Target = target;
        }

        public abstract bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out CodeBlock? info);
        public abstract void GetMethodRegionInfo(
            RangeSection rangeSection,
            TargetCodePointer jittedCodeAddress,
            out uint hotSize,
            out TargetPointer coldStart,
            out uint coldSize);
        public abstract TargetPointer GetUnwindInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress);
        public abstract TargetPointer GetDebugInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, out bool hasFlagByte);
        public abstract void GetGCInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, out TargetPointer gcInfo, out uint gcVersion);
        public abstract void GetExceptionClauses(RangeSection rangeSection, CodeBlockHandle codeInfoHandle, out TargetPointer startAddr, out TargetPointer endAddr);
        public abstract CodeKind GetCodeKind(RangeSection rangeSection, TargetCodePointer jittedCodeAddress);
    }

    private sealed class RangeSection
    {
        public readonly Data.RangeSection? Data;

        public RangeSection()
        {
            Data = default;
        }
        public RangeSection(Data.RangeSection rangeSection)
        {
            Data = rangeSection;
        }

        private bool HasFlags(RangeSectionFlags mask) => (Data!.Flags & (int)mask) != 0;
        internal bool IsRangeList => HasFlags(RangeSectionFlags.RangeList);
        internal bool IsCodeHeap => HasFlags(RangeSectionFlags.CodeHeap);
        internal bool IsInterpreter => HasFlags(RangeSectionFlags.Interpreter);

        internal bool HasR2RModule => Data!.R2RModule != TargetPointer.Null;

        internal static bool IsStubCodeBlock(Target target, TargetPointer codeHeaderIndirect)
        {
            byte stubCodeBlockLast = target.ReadGlobal<byte>(Constants.Globals.StubCodeBlockLast);
            return codeHeaderIndirect.Value <= stubCodeBlockLast;
        }

        internal static RangeSection Find(Target target, Data.RangeSectionMap topRangeSectionMap, ExecutionManagerHelpers.RangeSectionMap rangeSectionLookup, TargetCodePointer jittedCodeAddress)
        {
            TargetPointer rangeSectionFragmentPtr = rangeSectionLookup.FindFragment(target, topRangeSectionMap, jittedCodeAddress);
            // The lowest level of the range section map covers a large address space which may contain multiple small fragments.
            // Iterate over them to find the one that contains the jitted code address.
            while (rangeSectionFragmentPtr != TargetPointer.Null)
            {
                Data.RangeSectionFragment curFragment = target.ProcessedData.GetOrAdd<Data.RangeSectionFragment>(rangeSectionFragmentPtr);
                if (curFragment.Contains(jittedCodeAddress))
                {
                    break;
                }
                rangeSectionFragmentPtr = curFragment.Next;
            }
            if (rangeSectionFragmentPtr == TargetPointer.Null)
            {
                return new RangeSection();
            }
            Data.RangeSectionFragment fragment = target.ProcessedData.GetOrAdd<Data.RangeSectionFragment>(rangeSectionFragmentPtr);
            Data.RangeSection rangeSection = target.ProcessedData.GetOrAdd<Data.RangeSection>(fragment.RangeSection);
            if (rangeSection.NextForDelete != TargetPointer.Null)
            {
                return new RangeSection();
            }
            return new RangeSection(rangeSection);
        }
    }

    private JitManager? GetJitManager(RangeSection rangeSection)
    {
        if (rangeSection.IsInterpreter)
        {
            return _interpreterJitManager;
        }
        else if (rangeSection.Data!.R2RModule != TargetPointer.Null)
        {
            return _r2rJitManager;
        }
        else if (rangeSection.IsCodeHeap)
        {
            return _eeJitManager;
        }
        else
        {
            return null;
        }
    }

    private CodeBlock? GetCodeBlock(TargetCodePointer jittedCodeAddress)
    {
        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, jittedCodeAddress);
        if (range.Data == null)
        {
            return null;
        }
        JitManager? jitManager = GetJitManager(range);
        if (jitManager?.GetMethodInfo(range, jittedCodeAddress, out CodeBlock? info) == true)
        {
            return info;
        }
        else
        {
            return null;
        }
    }
    CodeBlockHandle? IExecutionManager.GetCodeBlockHandle(TargetCodePointer ip)
    {
        TargetPointer key = ip.AsTargetPointer; // FIXME: thumb bit. It's harmless (we potentialy have 2 cache entries per IP), but we should fix it
        if (_codeInfos.ContainsKey(key))
        {
            return new CodeBlockHandle(key);
        }
        CodeBlock? info = GetCodeBlock(ip);
        if (info == null || !info.Valid)
        {
            return null;
        }
        _codeInfos.TryAdd(key, info);
        return new CodeBlockHandle(key);
    }

    TargetPointer IExecutionManager.GetMethodDesc(CodeBlockHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        return info.MethodDescAddress;
    }

    TargetPointer IExecutionManager.GetStartAddress(CodeBlockHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        return info.StartAddress;
    }

    TargetPointer IExecutionManager.GetFuncletStartAddress(CodeBlockHandle codeInfoHandle)
    {
        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            throw new InvalidOperationException("Unable to get runtime function address");

        JitManager? jitManager = GetJitManager(range);
        TargetPointer runtimeFunctionPtr = jitManager?.GetUnwindInfo(range, codeInfoHandle.Address.Value) ?? TargetPointer.Null;

        if (runtimeFunctionPtr == TargetPointer.Null)
            throw new InvalidOperationException("Unable to get runtime function address");

        Data.RuntimeFunction runtimeFunction = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(runtimeFunctionPtr);

        // TODO(cdac): EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS, implement iterating over fragments until finding
        // non-fragment RuntimeFunction

        return CodePointerUtils.AddressFromCodePointer(
            new TargetCodePointer(range.Data.RangeBegin + runtimeFunction.BeginAddress), _target);
    }

    void IExecutionManager.GetMethodRegionInfo(CodeBlockHandle codeInfoHandle, out uint hotSize, out TargetPointer coldStart, out uint coldSize)
    {
        hotSize = 0;
        coldStart = TargetPointer.Null;
        coldSize = 0;

        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            throw new InvalidOperationException("Unable to get runtime function address");

        JitManager? jitManager = GetJitManager(range);

        jitManager?.GetMethodRegionInfo(range, codeInfoHandle.Address.Value, out hotSize, out coldStart, out coldSize);
    }

    TargetPointer IExecutionManager.NonVirtualEntry2MethodDesc(TargetCodePointer entrypoint)
    {
        if (_target.Contracts.FeatureFlags.IsEnabled(RuntimeFeature.PortableEntrypoints))
        {
            Data.PortableEntryPoint portableEntryPoint = _target.ProcessedData.GetOrAdd<Data.PortableEntryPoint>(entrypoint.AsTargetPointer);
            return portableEntryPoint.MethodDesc;
        }
        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, entrypoint);
        if (range.Data == null)
            return TargetPointer.Null;
        if (range.IsRangeList)
        {
            // An address may fall within a precode RangeSection without actually being a
            // valid precode (e.g., a MethodDesc address that shares the same memory range).
            // GetMethodDescFromStubAddress throws InvalidOperationException when the bytes
            // at the address don't match any known precode type. The DAC's C++ implementation
            // returns NULL in this case, so we match that behavior by returning TargetPointer.Null.
            IPrecodeStubs precodeStubs = _target.Contracts.PrecodeStubs;
            try
            {
                return precodeStubs.GetMethodDescFromStubAddress(entrypoint);
            }
            catch (InvalidOperationException)
            {
                return TargetPointer.Null;
            }
        }
        else
        {
            JitManager? jitManager = GetJitManager(range);
            if (jitManager?.GetMethodInfo(range, entrypoint, out CodeBlock? info) == true && info != null)
            {
                return info.MethodDescAddress;
            }
        }
        return TargetPointer.Null;
    }

    bool IExecutionManager.IsFunclet(CodeBlockHandle codeInfoHandle)
    {
        // Interpreter code has no native unwind info and therefore no funclets.
        TargetPointer startAddress = ((IExecutionManager)this).GetStartAddress(codeInfoHandle);
        if (((IExecutionManager)this).GetCodeKind(new TargetCodePointer(startAddress.Value)) == CodeKind.Interpreter)
            return false;

        return startAddress != ((IExecutionManager)this).GetFuncletStartAddress(codeInfoHandle);
    }

    bool IExecutionManager.IsFilterFunclet(CodeBlockHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        IExecutionManager eman = this;

        if (!eman.IsFunclet(codeInfoHandle))
            return false;

        TargetPointer funcletStartAddress = eman.GetFuncletStartAddress(codeInfoHandle);
        uint funcletStartOffset = (uint)(funcletStartAddress - info.StartAddress);

        List<ExceptionClauseInfo> clauses = eman.GetExceptionClauses(codeInfoHandle);
        foreach (ExceptionClauseInfo clause in clauses)
        {
            if (clause.ClauseType == ExceptionClauseInfo.ExceptionClauseFlags.Filter && clause.FilterOffset == funcletStartOffset)
                return true;
        }

        return false;
    }

    TargetPointer IExecutionManager.GetUnwindInfo(CodeBlockHandle codeInfoHandle)
    {
        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            return TargetPointer.Null;

        JitManager? jitManager = GetJitManager(range);

        return jitManager?.GetUnwindInfo(range, codeInfoHandle.Address.Value) ?? TargetPointer.Null;
    }

    TargetPointer IExecutionManager.GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle)
    {
        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            throw new InvalidOperationException($"{nameof(RangeSection)} not found for {codeInfoHandle.Address}");

        return range.Data.RangeBegin;
    }

    TargetPointer IExecutionManager.GetDebugInfo(CodeBlockHandle codeInfoHandle, out bool hasFlagByte)
    {
        hasFlagByte = false;
        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            return TargetPointer.Null;

        JitManager? jitManager = GetJitManager(range);
        return jitManager?.GetDebugInfo(range, codeInfoHandle.Address.Value, out hasFlagByte) ?? TargetPointer.Null;
    }

    void IExecutionManager.GetGCInfo(CodeBlockHandle codeInfoHandle, out TargetPointer gcInfo, out uint gcVersion)
    {
        gcInfo = TargetPointer.Null;
        gcVersion = 0;

        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            return;

        JitManager? jitManager = GetJitManager(range);
        jitManager?.GetGCInfo(range, codeInfoHandle.Address.Value, out gcInfo, out gcVersion);
    }


    TargetNUInt IExecutionManager.GetRelativeOffset(CodeBlockHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        return info.RelativeOffset;
    }

    bool IExecutionManager.IsGcSafe(TargetCodePointer instructionPointer)
    {
        IExecutionManager eman = this;
        if (eman.GetCodeBlockHandle(instructionPointer) is not CodeBlockHandle cbh)
            return false; // not managed code

        TargetNUInt relativeOffset = eman.GetRelativeOffset(cbh);
        eman.GetGCInfo(cbh, out TargetPointer gcInfoAddr, out uint gcVersion);
        IGCInfoHandle handle = _target.Contracts.GCInfo.DecodePlatformSpecificGCInfo(gcInfoAddr, gcVersion);

        uint offset = (uint)relativeOffset.Value;
        return _target.Contracts.GCInfo.IsGcSafe(handle, offset);
    }

    uint IExecutionManager.GetStackParameterSize(CodeBlockHandle codeInfoHandle)
    {
        IExecutionManager eman = this;
        if (_target.Contracts.RuntimeInfo.GetTargetArchitecture() is not RuntimeInfoArchitecture.X86)
            return 0;

        if (eman.IsFunclet(codeInfoHandle))
            return 0;

        eman.GetGCInfo(codeInfoHandle, out TargetPointer gcInfoAddress, out uint gcInfoVersion);
        if (gcInfoAddress == TargetPointer.Null)
            throw new InvalidOperationException($"GC info not available for {codeInfoHandle.Address}");

        IGCInfo gcInfoContract = _target.Contracts.GCInfo;
        IGCInfoHandle handle = gcInfoContract.DecodePlatformSpecificGCInfo(gcInfoAddress, gcInfoVersion);
        return gcInfoContract.GetCalleePoppedArgumentsSize(handle);
    }

    TargetPointer IExecutionManager.FindReadyToRunModule(TargetPointer address)
    {
        // Use the range section map to find the RangeSection containing the address.
        // The R2R range section covers the entire PE image (code + data), so this
        // works for import section addresses used by FindGCRefMap.
        TargetCodePointer codeAddr = CodePointerUtils.CodePointerFromAddress(address, _target);
        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, codeAddr);
        if (range.Data is null)
            return TargetPointer.Null;

        return range.Data.R2RModule;
    }

    JitManagerInfo IExecutionManager.GetEEJitManagerInfo()
    {
        TargetPointer eeJitManagerPtr = _target.ReadGlobalPointer(Constants.Globals.EEJitManagerAddress);
        TargetPointer eeJitManagerAddr = _target.ReadPointer(eeJitManagerPtr);

        Data.EEJitManager jitManager = _target.ProcessedData.GetOrAdd<Data.EEJitManager>(eeJitManagerAddr);

        return new JitManagerInfo
        {
            ManagerAddress = eeJitManagerAddr,
            CodeType = 0, // miManaged | miIL
            HeapListAddress = jitManager.AllCodeHeaps,
        };
    }

    private ICodeHeapInfo GetCodeHeapInfo(TargetPointer codeHeapAddress)
    {
        Data.CodeHeap codeHeap = _target.ProcessedData.GetOrAdd<Data.CodeHeap>(codeHeapAddress);
        return (CodeHeapType)codeHeap.HeapType switch
        {
            CodeHeapType.LoaderCodeHeap => new Contracts.LoaderCodeHeapInfo(codeHeapAddress,
                _target.ProcessedData.GetOrAdd<Data.LoaderCodeHeap>(codeHeapAddress).LoaderHeap),
            CodeHeapType.HostCodeHeap => new Contracts.HostCodeHeapInfo(codeHeapAddress,
                _target.ProcessedData.GetOrAdd<Data.HostCodeHeap>(codeHeapAddress).BaseAddress,
                _target.ProcessedData.GetOrAdd<Data.HostCodeHeap>(codeHeapAddress).CurrentAddress),
            _ => new Contracts.UnknownCodeHeapInfo(),
        };
    }

    IEnumerable<ICodeHeapInfo> IExecutionManager.GetCodeHeapInfos()
    {
        TargetPointer heapListAddress = ((IExecutionManager)this).GetEEJitManagerInfo().HeapListAddress;
        TargetPointer nodeAddr = heapListAddress;
        while (nodeAddr != TargetPointer.Null)
        {
            Data.CodeHeapListNode node = _target.ProcessedData.GetOrAdd<Data.CodeHeapListNode>(nodeAddr);
            yield return GetCodeHeapInfo(node.Heap);
            nodeAddr = node.Next;
        }
    }

    private RangeSection RangeSectionFromCodeBlockHandle(CodeBlockHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, codeInfoHandle.Address.Value);
        return range;
    }

    private static ExceptionClauseInfo.ExceptionClauseFlags GetExceptionClauseFlags(uint flags)
    {
        if ((flags & (uint)ExceptionClauseFlags_1.Fault) != 0) return ExceptionClauseInfo.ExceptionClauseFlags.Fault;
        if ((flags & (uint)ExceptionClauseFlags_1.Finally) != 0) return ExceptionClauseInfo.ExceptionClauseFlags.Finally;
        if ((flags & (uint)ExceptionClauseFlags_1.Filter) != 0) return ExceptionClauseInfo.ExceptionClauseFlags.Filter;
        return ExceptionClauseInfo.ExceptionClauseFlags.Typed;
    }

    private static bool IsFilterHandler(ExceptionClauseInfo.ExceptionClauseFlags flags) => flags == ExceptionClauseInfo.ExceptionClauseFlags.Filter;
    private static bool IsTypedHandler(ExceptionClauseInfo.ExceptionClauseFlags flags) => flags == ExceptionClauseInfo.ExceptionClauseFlags.Typed;
    private static bool HasCachedTypeHandle(IExceptionClauseData clause) => (clause.Flags & (uint)ExceptionClauseFlags_1.CachedClass) != 0;

    private bool IsObjectType(TargetPointer moduleAddr, uint classToken)
    {
        ILoader loader = _target.Contracts.Loader;
        ModuleHandle module = loader.GetModuleHandleFromModulePtr(moduleAddr);
        ModuleLookupTables tables = loader.GetLookupTables(module);

        TargetPointer resolvedMethodTable = (EcmaMetadataUtils.TokenType)(classToken & EcmaMetadataUtils.TokenTypeMask) switch
        {
            EcmaMetadataUtils.TokenType.mdtTypeDef => loader.GetModuleLookupMapElement(tables.TypeDefToMethodTable, classToken, out _),
            EcmaMetadataUtils.TokenType.mdtTypeRef => loader.GetModuleLookupMapElement(tables.TypeRefToMethodTable, classToken, out _),
            _ => TargetPointer.Null,
        };

        if (resolvedMethodTable == TargetPointer.Null)
            return false;

        TargetPointer objectMethodTable = _target.ReadPointer(
            _target.ReadGlobalPointer(Constants.Globals.ObjectMethodTable));

        return resolvedMethodTable == objectMethodTable;
    }

    List<ExceptionClauseInfo> IExecutionManager.GetExceptionClauses(CodeBlockHandle codeInfoHandle)
    {
        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            return new List<ExceptionClauseInfo>();

        JitManager? jitManager = GetJitManager(range);
        if (jitManager == null)
            return new List<ExceptionClauseInfo>();
        jitManager.GetExceptionClauses(range, codeInfoHandle, out TargetPointer startAddr, out TargetPointer endAddr);
        bool isR2R = jitManager is ReadyToRunJitManager;
        DataType clauseType = isR2R ? DataType.R2RExceptionClause : DataType.EEExceptionClause;
        uint clauseSize = _target.GetTypeInfo(clauseType).Size!.Value;
        TargetPointer methodDescPtr = ((IExecutionManager)this).GetMethodDesc(codeInfoHandle);
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
        TargetPointer mtPtr = rts.GetMethodTable(mdHandle);
        TypeHandle th = rts.GetTypeHandle(mtPtr);
        TargetPointer handleModuleAddr = rts.GetModule(th);

        List<ExceptionClauseInfo> exceptionClauses = new List<ExceptionClauseInfo>();
        for (TargetPointer addr = startAddr; addr < endAddr; addr += clauseSize)
        {
            IExceptionClauseData entry = isR2R
                ? _target.ProcessedData.GetOrAdd<R2RExceptionClause>(addr)
                : _target.ProcessedData.GetOrAdd<EEExceptionClause>(addr);

            ExceptionClauseInfo.ExceptionClauseFlags flags = GetExceptionClauseFlags(entry.Flags);
            uint? filterOffset = IsFilterHandler(flags) ? entry.FilterOffset : null;
            TargetNUInt? typeHandle = null;
            bool? isCatchAllHandler = null;
            TargetPointer? moduleAddr = null;
            uint? classToken = null;

            if (IsTypedHandler(flags))
            {
                if (HasCachedTypeHandle(entry) && !isR2R) // Dynamic method path: we only have a cached type handle, no token.
                {
                    typeHandle = ((EEExceptionClause)entry).TypeHandle;
                    TargetPointer objectMethodTable = _target.ReadPointer(
                        _target.ReadGlobalPointer(Constants.Globals.ObjectMethodTable));
                    isCatchAllHandler = typeHandle.Value.Value == objectMethodTable.Value;
                }
                else
                {
                    isCatchAllHandler = IsObjectType(handleModuleAddr, entry.ClassToken);
                    moduleAddr = handleModuleAddr;
                    classToken = entry.ClassToken;
                }
            }

            exceptionClauses.Add(new ExceptionClauseInfo
            {
                ClauseType = flags,
                IsCatchAllHandler = isCatchAllHandler,
                TryStartPC = entry.TryStartPC,
                TryEndPC = entry.TryEndPC,
                HandlerStartPC = entry.HandlerStartPC,
                HandlerEndPC = entry.HandlerEndPC,
                FilterOffset = filterOffset,
                ClassToken = classToken,
                TypeHandle = typeHandle,
                ModuleAddr = moduleAddr,
            });
        }
        return exceptionClauses;
    }

    private static CodeKind GetStubKind(StubKind stubKind)
    {
        return stubKind switch
        {
            StubKind.JumpStub => CodeKind.JumpStub,
            StubKind.DynamicHelper => CodeKind.DynamicHelper,
            StubKind.StubPrecode => CodeKind.StubPrecode,
            StubKind.FixupPrecode => CodeKind.FixupPrecode,
            StubKind.VSDDispatchStub => CodeKind.VSD_DispatchStub,
            StubKind.VSDResolveStub => CodeKind.VSD_ResolveStub,
            StubKind.VSDLookupStub => CodeKind.VSD_LookupStub,
            StubKind.VSDVTableStub => CodeKind.VSD_VTableStub,
            StubKind.CallCountingStub => CodeKind.CallCountingStub,
            _ => CodeKind.Unknown,
        };
    }

    public CodeKind GetCodeKind(TargetCodePointer codeAddress)
    {
        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, codeAddress);
        if (range.Data == null)
            return CodeKind.Unknown;

        // check if this is a stub
        JitManager? jitManager = GetJitManager(range);
        if (jitManager == null)
        {
            CodeRangeMapRangeList rangeList = _target.ProcessedData.GetOrAdd<Data.CodeRangeMapRangeList>(range.Data.RangeList);
            return GetStubKind((StubKind)rangeList.RangeListType);
        }
        return jitManager.GetCodeKind(range, codeAddress);
    }
}
