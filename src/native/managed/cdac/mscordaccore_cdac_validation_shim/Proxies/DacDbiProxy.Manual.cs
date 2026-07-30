// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Hand-written members of <see cref="DacDbiProxy"/>.
/// </summary>
internal sealed unsafe partial class DacDbiProxy
{
    /// <summary>
    /// Cache management, mirrored to both sides unconditionally so the legacy DBI's caches stay in
    /// step with the cDAC's - exactly as the pre-refactor cDAC DBI did.
    /// </summary>
    int IDacDbiInterface.FlushCache()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.FlushCache() : HResults.E_NOTIMPL;

        if (_legacy is not null)
        {
            _legacy.FlushCache();
        }

        return hr;
    }

    private const int MaxContextBufferSize = 4096;

    private static delegate* unmanaged[Thiscall]<nint, char*, int> GetAssignCopyFnPtr(nint stringHolder)
    {
        nint vtable = *(nint*)stringHolder;
        return (delegate* unmanaged[Thiscall]<nint, char*, int>)(*(nint*)vtable);
    }

    private static int StringHolderAssignCopy(nint stringHolder, string str)
    {
        fixed (char* pStr = str)
        {
            return GetAssignCopyFnPtr(stringHolder)(stringHolder, pStr);
        }
    }

    private sealed unsafe class NativeStringHolder : IDisposable
    {
        private readonly IntPtr _objectPtr;
        private readonly IntPtr _vtablePtr;
        private readonly GCHandle _delegateHandle;
        private readonly nint _forwardPtr;
        private bool _disposed;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int AssignCopyDelegate(IntPtr thisPtr, IntPtr psz);

        internal string? Value { get; private set; }

        internal NativeStringHolder(nint forwardPtr = 0)
        {
            _forwardPtr = forwardPtr;
            AssignCopyDelegate assignCopy = AssignCopyImpl;
            _delegateHandle = GCHandle.Alloc(assignCopy);
            IntPtr fnPtr = Marshal.GetFunctionPointerForDelegate(assignCopy);
            _vtablePtr = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(_vtablePtr, fnPtr);
            _objectPtr = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(_objectPtr, _vtablePtr);
        }

        internal nint Ptr => _objectPtr;

        private int AssignCopyImpl(IntPtr thisPtr, IntPtr psz)
        {
            Value = Marshal.PtrToStringUni(psz);
            return _forwardPtr != 0 ? GetAssignCopyFnPtr(_forwardPtr)(_forwardPtr, (char*)psz) : HResults.S_OK;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Marshal.FreeHGlobal(_objectPtr);
                Marshal.FreeHGlobal(_vtablePtr);
                _delegateHandle.Free();
                _disposed = true;
            }
        }
    }

    private sealed unsafe class ULongEnumerationState(delegate* unmanaged<ulong, nint, void> callback, nint userData)
    {
        internal List<ulong> Values { get; } = [];
        internal delegate* unmanaged<ulong, nint, void> Callback { get; } = callback;
        internal nint UserData { get; } = userData;
    }

    private sealed unsafe class StubFrameEnumerationState(delegate* unmanaged<Debugger_STRData*, void*, void> callback, nint userData)
    {
        internal List<Debugger_STRData> Values { get; } = [];
        internal delegate* unmanaged<Debugger_STRData*, void*, void> Callback { get; } = callback;
        internal nint UserData { get; } = userData;
    }

    private sealed unsafe class FieldEnumerationState(delegate* unmanaged<FieldData*, void*, void> callback, nint userData)
    {
        internal List<FieldData> Values { get; } = [];
        internal delegate* unmanaged<FieldData*, void*, void> Callback { get; } = callback;
        internal nint UserData { get; } = userData;
    }

    private sealed unsafe class ExpandedTypeEnumerationState(delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> callback, nint userData)
    {
        internal List<DebuggerIPCE_ExpandedTypeData> Values { get; } = [];
        internal delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> Callback { get; } = callback;
        internal nint UserData { get; } = userData;
    }

    private sealed unsafe class ExceptionFrameEnumerationState(delegate* unmanaged<ulong, ulong, ulong, uint, Interop.BOOL, nint, void> callback, nint userData)
    {
        internal List<(ulong VmAppDomain, ulong VmAssembly, ulong Ip, uint MethodDef, Interop.BOOL IsLastForeignExceptionFrame)> Values { get; } = [];
        internal delegate* unmanaged<ulong, ulong, ulong, uint, Interop.BOOL, nint, void> Callback { get; } = callback;
        internal nint UserData { get; } = userData;
    }

    private sealed unsafe class HeapSegmentEnumerationState(delegate* unmanaged<ulong, ulong, int, uint, nint, void> callback, nint userData)
    {
        internal List<(ulong Start, ulong End, int Generation, uint Heap)> Values { get; } = [];
        internal delegate* unmanaged<ulong, ulong, int, uint, nint, void> Callback { get; } = callback;
        internal nint UserData { get; } = userData;
    }

    private sealed unsafe class AsyncLocalEnumerationState(delegate* unmanaged<AsyncLocalData*, nint, void> callback, nint userData)
    {
        internal List<AsyncLocalData> Values { get; } = [];
        internal delegate* unmanaged<AsyncLocalData*, nint, void> Callback { get; } = callback;
        internal nint UserData { get; } = userData;
    }

    private sealed unsafe class DebugNativeCodeData(delegate* unmanaged<NativeVarInfo*, void*, void> varInfoCallback, delegate* unmanaged<DbiOffsetMapping*, void*, void> seqPointCallback, nint userData)
    {
        internal List<NativeVarInfo> VarInfos { get; } = [];
        internal List<DbiOffsetMapping> SeqPoints { get; } = [];
        internal delegate* unmanaged<NativeVarInfo*, void*, void> VarInfoCallback { get; } = varInfoCallback;
        internal delegate* unmanaged<DbiOffsetMapping*, void*, void> SeqPointCallback { get; } = seqPointCallback;
        internal nint UserData { get; } = userData;
    }


#if DEBUG
    private static void ValidateExpandedTypeData(DebuggerIPCE_ExpandedTypeData* cdac, DebuggerIPCE_ExpandedTypeData* dac)
    {
        Debug.Assert(cdac->elementType == dac->elementType,
            $"cDAC elementType: {cdac->elementType}, DAC: {dac->elementType}");
        switch ((CorElementType)ReadLittleEndian(cdac->elementType))
        {
            case CorElementType.Class:
            case CorElementType.ValueType:
                Debug.Assert(cdac->ClassTypeData_metadataToken == dac->ClassTypeData_metadataToken,
                    $"cDAC ClassTypeData.metadataToken: {cdac->ClassTypeData_metadataToken:x}, DAC: {dac->ClassTypeData_metadataToken:x}");
                Debug.Assert(cdac->ClassTypeData_vmAssembly == dac->ClassTypeData_vmAssembly,
                    $"cDAC ClassTypeData.vmAssembly: {cdac->ClassTypeData_vmAssembly:x}, DAC: {dac->ClassTypeData_vmAssembly:x}");
                Debug.Assert(cdac->ClassTypeData_typeHandle == dac->ClassTypeData_typeHandle,
                    $"cDAC ClassTypeData.typeHandle: {cdac->ClassTypeData_typeHandle:x}, DAC: {dac->ClassTypeData_typeHandle:x}");
                break;
            case CorElementType.Array:
            case CorElementType.SzArray:
                Debug.Assert(cdac->ArrayTypeData_arrayRank == dac->ArrayTypeData_arrayRank,
                    $"cDAC ArrayTypeData.arrayRank: {cdac->ArrayTypeData_arrayRank}, DAC: {dac->ArrayTypeData_arrayRank}");
                Debug.Assert(cdac->ArrayTypeData_arrayTypeArg.elementType == dac->ArrayTypeData_arrayTypeArg.elementType,
                    $"cDAC ArrayTypeData.arrayTypeArg.elementType: {cdac->ArrayTypeData_arrayTypeArg.elementType}, DAC: {dac->ArrayTypeData_arrayTypeArg.elementType}");
                Debug.Assert(cdac->ArrayTypeData_arrayTypeArg.metadataToken == dac->ArrayTypeData_arrayTypeArg.metadataToken,
                    $"cDAC ArrayTypeData.arrayTypeArg.metadataToken: {cdac->ArrayTypeData_arrayTypeArg.metadataToken:x}, DAC: {dac->ArrayTypeData_arrayTypeArg.metadataToken:x}");
                Debug.Assert(cdac->ArrayTypeData_arrayTypeArg.vmAssembly == dac->ArrayTypeData_arrayTypeArg.vmAssembly,
                    $"cDAC ArrayTypeData.arrayTypeArg.vmAssembly: {cdac->ArrayTypeData_arrayTypeArg.vmAssembly:x}, DAC: {dac->ArrayTypeData_arrayTypeArg.vmAssembly:x}");
                Debug.Assert(cdac->ArrayTypeData_arrayTypeArg.vmTypeHandle == dac->ArrayTypeData_arrayTypeArg.vmTypeHandle,
                    $"cDAC ArrayTypeData.arrayTypeArg.vmTypeHandle: {cdac->ArrayTypeData_arrayTypeArg.vmTypeHandle:x}, DAC: {dac->ArrayTypeData_arrayTypeArg.vmTypeHandle:x}");
                break;
            case CorElementType.Ptr:
            case CorElementType.Byref:
                Debug.Assert(cdac->UnaryTypeData_unaryTypeArg.elementType == dac->UnaryTypeData_unaryTypeArg.elementType,
                    $"cDAC UnaryTypeData.unaryTypeArg.elementType: {cdac->UnaryTypeData_unaryTypeArg.elementType}, DAC: {dac->UnaryTypeData_unaryTypeArg.elementType}");
                Debug.Assert(cdac->UnaryTypeData_unaryTypeArg.metadataToken == dac->UnaryTypeData_unaryTypeArg.metadataToken,
                    $"cDAC UnaryTypeData.unaryTypeArg.metadataToken: {cdac->UnaryTypeData_unaryTypeArg.metadataToken:x}, DAC: {dac->UnaryTypeData_unaryTypeArg.metadataToken:x}");
                Debug.Assert(cdac->UnaryTypeData_unaryTypeArg.vmAssembly == dac->UnaryTypeData_unaryTypeArg.vmAssembly,
                    $"cDAC UnaryTypeData.unaryTypeArg.vmAssembly: {cdac->UnaryTypeData_unaryTypeArg.vmAssembly:x}, DAC: {dac->UnaryTypeData_unaryTypeArg.vmAssembly:x}");
                Debug.Assert(cdac->UnaryTypeData_unaryTypeArg.vmTypeHandle == dac->UnaryTypeData_unaryTypeArg.vmTypeHandle,
                    $"cDAC UnaryTypeData.unaryTypeArg.vmTypeHandle: {cdac->UnaryTypeData_unaryTypeArg.vmTypeHandle:x}, DAC: {dac->UnaryTypeData_unaryTypeArg.vmTypeHandle:x}");
                break;
            case CorElementType.FnPtr:
                Debug.Assert(cdac->NaryTypeData_typeHandle == dac->NaryTypeData_typeHandle,
                    $"cDAC NaryTypeData.typeHandle: {cdac->NaryTypeData_typeHandle:x}, DAC: {dac->NaryTypeData_typeHandle:x}");
                break;
        }
    }

    private static T ReadLittleEndian<T>(T value) where T : unmanaged, IBinaryInteger<T>
    {
        if (BitConverter.IsLittleEndian)
            return value;
        MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)).Reverse();
        return value;
    }

    private static string FormatExpandedTypeData(DebuggerIPCE_ExpandedTypeData e) =>
        $"elementType={e.elementType}, " +
        $"token=0x{e.ClassTypeData_metadataToken:x}, " +
        $"vmAssembly=0x{e.ClassTypeData_vmAssembly:x}, " +
        $"vmTypeHandle=0x{e.ClassTypeData_typeHandle:x}";

    private static uint CountHandlePrefix(DacGcReference[] buffer, uint length)
    {
        for (uint j = 0; j < length; j++)
        {
            CorGCReferenceType dwType = buffer[j].dwType;
            if (dwType == CorGCReferenceType.CorReferenceStack)
            {
                return j;
            }
        }
        return length;
    }
#endif


#if DEBUG
    [UnmanagedCallersOnly]
    private static void CollectEnumerationCallback(ulong value, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ((List<ulong>)handle.Target!).Add(value);
    }

    [UnmanagedCallersOnly]
    private static void RecordULongAndForwardCallback(ulong value, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ULongEnumerationState state = (ULongEnumerationState)handle.Target!;
        state.Values.Add(value);
        if (state.Callback != null)
            state.Callback(value, state.UserData);
    }

    [UnmanagedCallersOnly]
    private static void CollectStubFrameCallback(Debugger_STRData* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        ((List<Debugger_STRData>)handle.Target!).Add(*data);
    }

    [UnmanagedCallersOnly]
    private static void RecordStubFrameAndForwardCallback(Debugger_STRData* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        StubFrameEnumerationState state = (StubFrameEnumerationState)handle.Target!;
        state.Values.Add(*data);
        if (state.Callback != null)
            state.Callback(data, (void*)state.UserData);
    }

    [UnmanagedCallersOnly]
    private static void CollectFieldDataCallback(FieldData* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        ((List<FieldData>)handle.Target!).Add(*data);
    }

    [UnmanagedCallersOnly]
    private static void RecordFieldDataAndForwardCallback(FieldData* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        FieldEnumerationState state = (FieldEnumerationState)handle.Target!;
        state.Values.Add(*data);
        if (state.Callback != null)
            state.Callback(data, (void*)state.UserData);
    }

    [UnmanagedCallersOnly]
    private static void CollectExpandedTypeCallback(DebuggerIPCE_ExpandedTypeData* data, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ((ExpandedTypeEnumerationState)handle.Target!).Values.Add(*data);
    }

    [UnmanagedCallersOnly]
    private static void RecordExpandedTypeAndForwardCallback(DebuggerIPCE_ExpandedTypeData* data, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ExpandedTypeEnumerationState state = (ExpandedTypeEnumerationState)handle.Target!;
        state.Values.Add(*data);
        if (state.Callback != null)
            state.Callback(data, state.UserData);
    }

    [UnmanagedCallersOnly]
    private static void CollectExceptionFrameCallback(ulong vmAppDomain, ulong vmAssembly, ulong ip, uint methodDef, Interop.BOOL isLastForeignExceptionFrame, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ((ExceptionFrameEnumerationState)handle.Target!).Values.Add((vmAppDomain, vmAssembly, ip, methodDef, isLastForeignExceptionFrame));
    }

    [UnmanagedCallersOnly]
    private static void RecordExceptionFrameAndForwardCallback(ulong vmAppDomain, ulong vmAssembly, ulong ip, uint methodDef, Interop.BOOL isLastForeignExceptionFrame, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ExceptionFrameEnumerationState state = (ExceptionFrameEnumerationState)handle.Target!;
        state.Values.Add((vmAppDomain, vmAssembly, ip, methodDef, isLastForeignExceptionFrame));
        if (state.Callback != null)
            state.Callback(vmAppDomain, vmAssembly, ip, methodDef, isLastForeignExceptionFrame, state.UserData);
    }

    [UnmanagedCallersOnly]
    private static void CollectHeapSegmentCallback(ulong start, ulong end, int generation, uint heap, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ((HeapSegmentEnumerationState)handle.Target!).Values.Add((start, end, generation, heap));
    }

    [UnmanagedCallersOnly]
    private static void RecordHeapSegmentAndForwardCallback(ulong start, ulong end, int generation, uint heap, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        HeapSegmentEnumerationState state = (HeapSegmentEnumerationState)handle.Target!;
        state.Values.Add((start, end, generation, heap));
        if (state.Callback != null)
            state.Callback(start, end, generation, heap, state.UserData);
    }

    [UnmanagedCallersOnly]
    private static void CollectAsyncLocalCallback(AsyncLocalData* data, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ((AsyncLocalEnumerationState)handle.Target!).Values.Add(*data);
    }

    [UnmanagedCallersOnly]
    private static void RecordAsyncLocalAndForwardCallback(AsyncLocalData* data, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        AsyncLocalEnumerationState state = (AsyncLocalEnumerationState)handle.Target!;
        state.Values.Add(*data);
        if (state.Callback != null)
            state.Callback(data, state.UserData);
    }

    [UnmanagedCallersOnly]
    private static void CollectNativeVarInfoCallback(NativeVarInfo* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        ((DebugNativeCodeData)handle.Target!).VarInfos.Add(*data);
    }

    [UnmanagedCallersOnly]
    private static void RecordNativeVarInfoAndForwardCallback(NativeVarInfo* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        DebugNativeCodeData state = (DebugNativeCodeData)handle.Target!;
        state.VarInfos.Add(*data);
        if (state.VarInfoCallback != null)
            state.VarInfoCallback(data, (void*)state.UserData);
    }

    [UnmanagedCallersOnly]
    private static void CollectOffsetMappingCallback(DbiOffsetMapping* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        ((DebugNativeCodeData)handle.Target!).SeqPoints.Add(*data);
    }

    [UnmanagedCallersOnly]
    private static void RecordOffsetMappingAndForwardCallback(DbiOffsetMapping* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        DebugNativeCodeData state = (DebugNativeCodeData)handle.Target!;
        state.SeqPoints.Add(*data);
        if (state.SeqPointCallback != null)
            state.SeqPointCallback(data, (void*)state.UserData);
    }
#endif


#if DEBUG
    private delegate int LegacyEnumerateFieldsFn(nuint* pObjectSize, nint pUserData);

    private static void ValidateEnumerateFieldsAgainstLegacy(string label, nuint cdacObjectSize, List<FieldData>? cdacFields, int hr, LegacyEnumerateFieldsFn legacyEnumerate)
    {
        List<FieldData> dacFields = new();
        GCHandle dacHandle = GCHandle.Alloc(dacFields);
        nuint dacObjectSize = 0;
        int hrLocal;
        try
        {
            hrLocal = legacyEnumerate(&dacObjectSize, GCHandle.ToIntPtr(dacHandle));
        }
        finally
        {
            dacHandle.Free();
        }
        Debug.ValidateHResult(hr, hrLocal);
        if (hr == HResults.S_OK)
        {
            Debug.Assert(cdacObjectSize == dacObjectSize, $"{label} object size mismatch - cDAC: {cdacObjectSize}, DAC: {dacObjectSize}");
            AssertFieldListsEqual(cdacFields, dacFields, label);
        }
    }

    private static void AssertFieldListsEqual(List<FieldData>? cdacFields, List<FieldData> dacFields, string label)
    {
        Debug.Assert(cdacFields!.Count == dacFields.Count, $"{label} field count mismatch - cDAC: {cdacFields!.Count}, DAC: {dacFields.Count}");
        int n = Math.Min(cdacFields!.Count, dacFields.Count);
        for (int i = 0; i < n; i++)
        {
            FieldData c = cdacFields![i];
            FieldData d = dacFields[i];
            Debug.Assert(c.m_fldMetadataToken == d.m_fldMetadataToken, $"{label} field[{i}] m_fldMetadataToken mismatch - cDAC: 0x{c.m_fldMetadataToken:x}, DAC: 0x{d.m_fldMetadataToken:x}");
            Debug.Assert(c.m_fFldStorageAvailable == d.m_fFldStorageAvailable, $"{label} field[{i}] m_fFldStorageAvailable mismatch - cDAC: {c.m_fFldStorageAvailable}, DAC: {d.m_fFldStorageAvailable}");
            Debug.Assert(c.m_fFldIsStatic == d.m_fFldIsStatic, $"{label} field[{i}] m_fFldIsStatic mismatch - cDAC: {c.m_fFldIsStatic}, DAC: {d.m_fFldIsStatic}");
            Debug.Assert(c.m_fFldIsRVA == d.m_fFldIsRVA, $"{label} field[{i}] m_fFldIsRVA mismatch - cDAC: {c.m_fFldIsRVA}, DAC: {d.m_fFldIsRVA}");
            Debug.Assert(c.m_fFldIsTLS == d.m_fFldIsTLS, $"{label} field[{i}] m_fFldIsTLS mismatch - cDAC: {c.m_fFldIsTLS}, DAC: {d.m_fFldIsTLS}");
            Debug.Assert(c.m_fFldIsPrimitive == d.m_fFldIsPrimitive, $"{label} field[{i}] m_fFldIsPrimitive mismatch - cDAC: {c.m_fFldIsPrimitive}, DAC: {d.m_fFldIsPrimitive}");
            Debug.Assert(c.m_fFldIsCollectibleStatic == d.m_fFldIsCollectibleStatic, $"{label} field[{i}] m_fFldIsCollectibleStatic mismatch - cDAC: {c.m_fFldIsCollectibleStatic}, DAC: {d.m_fFldIsCollectibleStatic}");
            Debug.Assert(c.m_fldInstanceOffset == d.m_fldInstanceOffset, $"{label} field[{i}] m_fldInstanceOffset mismatch - cDAC: 0x{c.m_fldInstanceOffset:x}, DAC: 0x{d.m_fldInstanceOffset:x}");
            Debug.Assert(c.m_pFldStaticAddress == d.m_pFldStaticAddress, $"{label} field[{i}] m_pFldStaticAddress mismatch - cDAC: 0x{c.m_pFldStaticAddress:x}, DAC: 0x{d.m_pFldStaticAddress:x}");
            Debug.Assert(c.m_vmFieldDesc == d.m_vmFieldDesc, $"{label} field[{i}] m_vmFieldDesc mismatch - cDAC: 0x{c.m_vmFieldDesc:x}, DAC: 0x{d.m_vmFieldDesc:x}");
        }
    }

    private void ValidateNativeCodeInfoAgainstLegacy(
        ulong vmMethodDesc,
        ulong startAddress,
        Interop.BOOL fCodeAvailable,
        uint* pFixedArgCount,
        List<NativeVarInfo> cdacVarInfos,
        List<DbiOffsetMapping> cdacSeqPoints,
        int hr,
        bool varInfoRequested,
        bool seqPointsRequested)
    {
        uint dacFixedArgCount = 0;
        DebugNativeCodeData dacData = new(null, null, 0);
        GCHandle dacHandle = GCHandle.Alloc(dacData);
        int hrLocal;
        try
        {
            hrLocal = _legacy!.GetNativeCodeSequencePointsAndVarInfo(
                vmMethodDesc, startAddress, fCodeAvailable, &dacFixedArgCount,
                (delegate* unmanaged<NativeVarInfo*, void*, void>)&CollectNativeVarInfoCallback,
                (delegate* unmanaged<DbiOffsetMapping*, void*, void>)&CollectOffsetMappingCallback,
                GCHandle.ToIntPtr(dacHandle));
        }
        finally
        {
            dacHandle.Free();
        }

        Debug.ValidateHResult(hr, hrLocal);
        if (hr == HResults.S_OK)
        {
            if (pFixedArgCount != null)
            {
                Debug.Assert(*pFixedArgCount == dacFixedArgCount,
                    $"fixedArgCount mismatch - cDAC: {*pFixedArgCount}, DAC: {dacFixedArgCount}");
            }

            if (seqPointsRequested)
                AssertSeqPointsEqual(cdacSeqPoints, dacData.SeqPoints);
            if (varInfoRequested)
                AssertVarInfosEqual(cdacVarInfos, dacData.VarInfos);
        }
    }

    private static void AssertSeqPointsEqual(List<DbiOffsetMapping> cdac, List<DbiOffsetMapping> dac)
    {
        Debug.Assert(cdac.Count == dac.Count,
            $"SeqPoint count mismatch - cDAC: {cdac.Count}, DAC: {dac.Count}");
        int n = Math.Min(cdac.Count, dac.Count);
        for (int i = 0; i < n; i++)
        {
            DbiOffsetMapping c = cdac[i];
            DbiOffsetMapping d = dac[i];
            Debug.Assert(c.nativeOffset == d.nativeOffset,
                $"SeqPoint[{i}] nativeOffset mismatch - cDAC: {c.nativeOffset}, DAC: {d.nativeOffset}");
            Debug.Assert(c.ilOffset == d.ilOffset,
                $"SeqPoint[{i}] ilOffset mismatch - cDAC: {c.ilOffset}, DAC: {d.ilOffset}");
            Debug.Assert(c.source == d.source,
                $"SeqPoint[{i}] source mismatch - cDAC: 0x{c.source:X}, DAC: 0x{d.source:X}");
        }
    }

    private static void AssertVarInfosEqual(List<NativeVarInfo> cdac, List<NativeVarInfo> dac)
    {
        Debug.Assert(cdac.Count == dac.Count,
            $"VarInfo count mismatch - cDAC: {cdac.Count}, DAC: {dac.Count}");
        int n = Math.Min(cdac.Count, dac.Count);
        for (int i = 0; i < n; i++)
        {
            NativeVarInfo c = cdac[i];
            NativeVarInfo d = dac[i];
            Debug.Assert(c.startOffset == d.startOffset,
                $"VarInfo[{i}] startOffset mismatch - cDAC: {c.startOffset}, DAC: {d.startOffset}");
            Debug.Assert(c.endOffset == d.endOffset,
                $"VarInfo[{i}] endOffset mismatch - cDAC: {c.endOffset}, DAC: {d.endOffset}");
            Debug.Assert(c.callReturnValueILOffset == d.callReturnValueILOffset,
                $"VarInfo[{i}] callReturnValueILOffset mismatch - cDAC: {c.callReturnValueILOffset}, DAC: {d.callReturnValueILOffset}");
            Debug.Assert(c.varNumber == d.varNumber,
                $"VarInfo[{i}] varNumber mismatch - cDAC: {c.varNumber}, DAC: {d.varNumber}");
            Debug.Assert(c.loc.vlType == d.loc.vlType,
                $"VarInfo[{i}] vlType mismatch - cDAC: {c.loc.vlType}, DAC: {d.loc.vlType}");

            switch (c.loc.vlType)
            {
                case VarLocType.VLT_REG:
                case VarLocType.VLT_REG_FP:
                case VarLocType.VLT_REG_BYREF:
                    Debug.Assert(c.loc.vlrReg == d.loc.vlrReg,
                        $"VarInfo[{i}] vlrReg mismatch - cDAC: {c.loc.vlrReg}, DAC: {d.loc.vlrReg}");
                    break;
                case VarLocType.VLT_STK:
                case VarLocType.VLT_STK_BYREF:
                case VarLocType.VLT_STK2:
                    Debug.Assert(c.loc.vlsBaseReg == d.loc.vlsBaseReg,
                        $"VarInfo[{i}] vlsBaseReg mismatch - cDAC: {c.loc.vlsBaseReg}, DAC: {d.loc.vlsBaseReg}");
                    Debug.Assert(c.loc.vlsOffset == d.loc.vlsOffset,
                        $"VarInfo[{i}] vlsOffset mismatch - cDAC: {c.loc.vlsOffset}, DAC: {d.loc.vlsOffset}");
                    break;
                case VarLocType.VLT_REG_REG:
                    Debug.Assert(c.loc.vlrrReg1 == d.loc.vlrrReg1,
                        $"VarInfo[{i}] vlrrReg1 mismatch - cDAC: {c.loc.vlrrReg1}, DAC: {d.loc.vlrrReg1}");
                    Debug.Assert(c.loc.vlrrReg2 == d.loc.vlrrReg2,
                        $"VarInfo[{i}] vlrrReg2 mismatch - cDAC: {c.loc.vlrrReg2}, DAC: {d.loc.vlrrReg2}");
                    break;
                case VarLocType.VLT_REG_STK:
                    Debug.Assert(c.loc.vlrsReg == d.loc.vlrsReg,
                        $"VarInfo[{i}] vlrsReg mismatch - cDAC: {c.loc.vlrsReg}, DAC: {d.loc.vlrsReg}");
                    Debug.Assert(c.loc.vlrssBaseReg == d.loc.vlrssBaseReg,
                        $"VarInfo[{i}] vlrssBaseReg mismatch - cDAC: {c.loc.vlrssBaseReg}, DAC: {d.loc.vlrssBaseReg}");
                    Debug.Assert(c.loc.vlrssOffset == d.loc.vlrssOffset,
                        $"VarInfo[{i}] vlrssOffset mismatch - cDAC: {c.loc.vlrssOffset}, DAC: {d.loc.vlrssOffset}");
                    break;
                case VarLocType.VLT_STK_REG:
                    Debug.Assert(c.loc.vlsrsBaseReg == d.loc.vlsrsBaseReg,
                        $"VarInfo[{i}] vlsrsBaseReg mismatch - cDAC: {c.loc.vlsrsBaseReg}, DAC: {d.loc.vlsrsBaseReg}");
                    Debug.Assert(c.loc.vlsrsOffset == d.loc.vlsrsOffset,
                        $"VarInfo[{i}] vlsrsOffset mismatch - cDAC: {c.loc.vlsrsOffset}, DAC: {d.loc.vlsrsOffset}");
                    Debug.Assert(c.loc.vlsrReg == d.loc.vlsrReg,
                        $"VarInfo[{i}] vlsrReg mismatch - cDAC: {c.loc.vlsrReg}, DAC: {d.loc.vlsrReg}");
                    break;
                case VarLocType.VLT_FPSTK:
                    Debug.Assert(c.loc.vlfReg == d.loc.vlfReg,
                        $"VarInfo[{i}] vlfReg mismatch - cDAC: {c.loc.vlfReg}, DAC: {d.loc.vlfReg}");
                    break;
                case VarLocType.VLT_FIXED_VA:
                    Debug.Assert(c.loc.vlfvOffset == d.loc.vlfvOffset,
                        $"VarInfo[{i}] vlfvOffset mismatch - cDAC: {c.loc.vlfvOffset}, DAC: {d.loc.vlfvOffset}");
                    break;
            }
        }
    }
#endif


#if DEBUG
    private static void AssertExpandedTypeLists(List<DebuggerIPCE_ExpandedTypeData> entries, List<DebuggerIPCE_ExpandedTypeData> legacyEntries)
    {
        if (!entries.SequenceEqual(legacyEntries))
        {
            Debug.Assert(entries.Count == legacyEntries.Count,
                $"cDAC param count: {entries.Count}, DAC: {legacyEntries.Count}");

            int compareCount = Math.Min(entries.Count, legacyEntries.Count);
            for (int i = 0; i < compareCount; i++)
            {
                Debug.Assert(entries[i].Equals(legacyEntries[i]),
                    $"Type param {i} mismatch{Environment.NewLine}" +
                    $"  cDAC: ({FormatExpandedTypeData(entries[i])}){Environment.NewLine}" +
                    $"  DAC:  ({FormatExpandedTypeData(legacyEntries[i])})");
            }
        }
    }
#endif

}
