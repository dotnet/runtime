// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using DACF = Microsoft.Diagnostics.DataContractReader.Contracts.DebuggerAssemblyControlFlags;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class DacDbiImpl : IDacDbiInterface
{
    private readonly Target _target;
    private readonly IDacDbiInterface? _legacy;

    // IStringHolder is a native C++ abstract class (not COM) with a single virtual method:
    //   virtual HRESULT AssignCopy(const WCHAR* psz) = 0;
    // The nint we receive is a pointer to the object, whose first field is the vtable pointer.
    // The vtable has a single entry: a function pointer for AssignCopy.
    // Use Thiscall because this is a C++ virtual method (thiscall on x86, no-op on x64/arm64).
    private delegate* unmanaged[Thiscall]<nint, char*, int> GetAssignCopyFnPtr(nint stringHolder)
    {
        // stringHolder -> vtable ptr -> first slot is AssignCopy
        nint vtable = *(nint*)stringHolder;
        return (delegate* unmanaged[Thiscall]<nint, char*, int>)(*(nint*)vtable);
    }

    private int StringHolderAssignCopy(nint stringHolder, string str)
    {
        fixed (char* pStr = str)
        {
            return GetAssignCopyFnPtr(stringHolder)(stringHolder, pStr);
        }
    }

    private bool CORProfilerPresent()
    {
        if (!_target.TryReadGlobalPointer(Constants.Globals.ProfilerControlBlock, out TargetPointer? profControlBlockAddress))
            return false;

        Target.TypeInfo type = _target.GetTypeInfo(DataType.ProfControlBlock);
        TargetPointer mainProfInterface = _target.ReadPointerField(profControlBlockAddress.Value, type, "MainProfilerProfInterface");
        int notificationCount = _target.ReadField<int>(profControlBlockAddress.Value, type, "NotificationProfilerCount");
        return mainProfInterface != TargetPointer.Null || notificationCount > 0;
    }

    public DacDbiImpl(Target target, object? legacyObj)
    {
        _target = target;
        _legacy = legacyObj as IDacDbiInterface;
    }

    public int CheckDbiVersion(DbiVersion* pVersion)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.CheckDbiVersion(pVersion) : HResults.E_NOTIMPL;

    public int FlushCache()
    {
        _target.Flush();
        return _legacy is not null ? _legacy.FlushCache() : HResults.S_OK;
    }

    public int DacSetTargetConsistencyChecks(Interop.BOOL fEnableAsserts)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.DacSetTargetConsistencyChecks(fEnableAsserts) : HResults.E_NOTIMPL;

    public int IsLeftSideInitialized(Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            *pResult = _target.Contracts.Debugger.TryGetDebuggerData(out Contracts.DebuggerData data) && data.IsLeftSideInitialized ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.IsLeftSideInitialized(&resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal);
        }
#endif

        return hr;
    }

    public int GetAppDomainId(ulong vmAppDomain, uint* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            *pRetVal = vmAppDomain == 0 ? 0u : _target.ReadGlobal<uint>(Constants.Globals.DefaultADID);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            uint retValLocal;
            int hrLocal = _legacy.GetAppDomainId(vmAppDomain, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetAppDomainFullName(ulong vmAppDomain, nint pStrName)
    {
        int hr = HResults.S_OK;
        try
        {
            string name = _target.Contracts.Loader.GetAppDomainFriendlyName();
            hr = StringHolderAssignCopy(pStrName, name);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.GetAppDomainFullName(vmAppDomain, pStrName);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    public int GetModuleSimpleName(ulong vmModule, nint pStrFilename)
    {
        int hr = HResults.S_OK;
        string? cdacSimpleName = null;
        try
        {
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));
            cdacSimpleName = loader.GetSimpleName(handle);
            hr = StringHolderAssignCopy(pStrFilename, cdacSimpleName);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            using var legacyHolder = new NativeStringHolder();
            int hrLocal = _legacy.GetModuleSimpleName(vmModule, legacyHolder.Ptr);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(
                    string.Equals(cdacSimpleName, legacyHolder.Value, System.StringComparison.Ordinal),
                    $"GetModuleSimpleName string mismatch - cDAC: '{cdacSimpleName}', DAC: '{legacyHolder.Value}'");
            }
        }
#endif
        return hr;
    }

    public int GetAssemblyPath(ulong vmAssembly, nint pStrFilename, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));
            string path = loader.GetPath(handle);
            if (string.IsNullOrEmpty(path))
            {
                *pResult = Interop.BOOL.FALSE;
            }
            else
            {
                hr = StringHolderAssignCopy(pStrFilename, path);
                *pResult = Interop.BOOL.TRUE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.GetAssemblyPath(vmAssembly, pStrFilename, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int ResolveTypeReference(DacDbiTypeRefData* pTypeRefInfo, DacDbiTypeRefData* pTargetRefInfo)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.ResolveTypeReference(pTypeRefInfo, pTargetRefInfo) : HResults.E_NOTIMPL;

    public int GetModulePath(ulong vmModule, nint pStrFilename, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));
            string path = loader.GetPath(handle);
            if (string.IsNullOrEmpty(path))
            {
                *pResult = Interop.BOOL.FALSE;
            }
            else
            {
                hr = StringHolderAssignCopy(pStrFilename, path);
                *pResult = Interop.BOOL.TRUE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.GetModulePath(vmModule, pStrFilename, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int GetMetadata(ulong vmModule, DacDbiTargetBuffer* pTargetBuffer)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetMetadata(vmModule, pTargetBuffer) : HResults.E_NOTIMPL;

    public int GetSymbolsBuffer(ulong vmModule, DacDbiTargetBuffer* pTargetBuffer, SymbolFormat* pSymbolFormat)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pTargetBuffer == null)
                throw new ArgumentNullException(nameof(pTargetBuffer));

            if (pSymbolFormat == null)
                throw new ArgumentNullException(nameof(pSymbolFormat));

            *pTargetBuffer = default;
            *pSymbolFormat = SymbolFormat.None;

            if (vmModule == 0)
                throw new ArgumentException("Module pointer must be non-zero.", nameof(vmModule));

            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));

            if (loader.TryGetSymbolStream(handle, out TargetPointer buffer, out uint size) && size != 0)
            {
                pTargetBuffer->pAddress = buffer.Value;
                pTargetBuffer->cbSize = size;
                *pSymbolFormat = SymbolFormat.Pdb;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            DacDbiTargetBuffer bufferLocal;
            SymbolFormat formatLocal;
            int hrLocal = _legacy.GetSymbolsBuffer(vmModule, &bufferLocal, &formatLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pTargetBuffer->pAddress == bufferLocal.pAddress, $"pAddress: cDAC: {pTargetBuffer->pAddress:x}, DAC: {bufferLocal.pAddress:x}");
                Debug.Assert(pTargetBuffer->cbSize == bufferLocal.cbSize, $"cbSize: cDAC: {pTargetBuffer->cbSize}, DAC: {bufferLocal.cbSize}");
                Debug.Assert(*pSymbolFormat == formatLocal, $"pSymbolFormat: cDAC: {*pSymbolFormat}, DAC: {formatLocal}");
            }
        }
#endif
        return hr;
    }

    public int GetModuleData(ulong vmModule, DacDbiModuleInfo* pData)
    {
        int hr = HResults.S_OK;
        try
        {
            if (vmModule == 0)
                throw new ArgumentException("Module pointer must be non-zero.", nameof(vmModule));

            if (pData == null)
                throw new ArgumentNullException(nameof(pData));

            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));

            *pData = default;
            pData->vmAssembly = loader.GetAssembly(handle).Value;
            pData->vmPEAssembly = loader.GetPEAssembly(handle).Value;
            bool isDynamic = loader.IsDynamic(handle);
            pData->fIsDynamic = isDynamic ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
            string path = loader.GetPath(handle);
            pData->fInMemory = string.IsNullOrEmpty(path) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
            if (!isDynamic && loader.TryGetLoadedImageContents(handle, out TargetPointer baseAddress, out uint size, out uint _))
            {
                pData->pPEBaseAddress = baseAddress.Value;
                pData->nPESize = size;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            DacDbiModuleInfo dataLocal;
            int hrLocal = _legacy.GetModuleData(vmModule, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pData->vmAssembly == dataLocal.vmAssembly, $"vmAssembly: cDAC: {pData->vmAssembly:x}, DAC: {dataLocal.vmAssembly:x}");
                Debug.Assert(pData->vmPEAssembly == dataLocal.vmPEAssembly, $"vmPEAssembly: cDAC: {pData->vmPEAssembly:x}, DAC: {dataLocal.vmPEAssembly:x}");
                Debug.Assert(pData->fIsDynamic == dataLocal.fIsDynamic, $"fIsDynamic: cDAC: {pData->fIsDynamic}, DAC: {dataLocal.fIsDynamic}");
                Debug.Assert(pData->fInMemory == dataLocal.fInMemory, $"fInMemory: cDAC: {pData->fInMemory}, DAC: {dataLocal.fInMemory}");
                Debug.Assert(pData->pPEBaseAddress == dataLocal.pPEBaseAddress, $"pPEBaseAddress: cDAC: {pData->pPEBaseAddress:x}, DAC: {dataLocal.pPEBaseAddress:x}");
                Debug.Assert(pData->nPESize == dataLocal.nPESize, $"nPESize: cDAC: {pData->nPESize}, DAC: {dataLocal.nPESize}");
            }
        }
#endif
        return hr;
    }
    public int GetModuleForAssembly(ulong vmAssembly, ulong* pModule)
    {
        *pModule = 0;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));
            TargetPointer modulePtr = loader.GetModule(handle);
            *pModule = modulePtr.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong moduleLocal;
            int hrLocal = _legacy.GetModuleForAssembly(vmAssembly, &moduleLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pModule == moduleLocal, $"cDAC: {*pModule:x}, DAC: {moduleLocal:x}");
        }
#endif
        return hr;
    }

    public int GetAddressType(ulong address, int* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetAddressType(address, pRetVal) : HResults.E_NOTIMPL;

    public int GetCompilerFlags(ulong vmAssembly, Interop.BOOL* pfAllowJITOpts, Interop.BOOL* pfEnableEnC)
    {
        *pfAllowJITOpts = Interop.BOOL.FALSE;
        *pfEnableEnC = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));
            Contracts.ModuleFlags flags = loader.GetFlags(handle);
            *pfAllowJITOpts = (flags & Contracts.ModuleFlags.JitOptimizationDisabled) == 0
                ? Interop.BOOL.TRUE
                : Interop.BOOL.FALSE;
            *pfEnableEnC = (flags & Contracts.ModuleFlags.EditAndContinue) != 0
                ? Interop.BOOL.TRUE
                : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL allowJITOptsLocal;
            Interop.BOOL enableEnCLocal;
            int hrLocal = _legacy.GetCompilerFlags(vmAssembly, &allowJITOptsLocal, &enableEnCLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pfAllowJITOpts == allowJITOptsLocal, $"cDAC: {*pfAllowJITOpts}, DAC: {allowJITOptsLocal}");
                Debug.Assert(*pfEnableEnC == enableEnCLocal, $"cDAC: {*pfEnableEnC}, DAC: {enableEnCLocal}");
            }
        }
#endif
        return hr;
    }

    public int SetCompilerFlags(ulong vmAssembly, Interop.BOOL fAllowJitOpts, Interop.BOOL fEnableEnC)
    {
        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));

            DACF debuggerInfoBits = loader.GetDebuggerInfoBits(handle);
            DACF controlFlags = debuggerInfoBits & ~(DACF.DACF_ALLOW_JIT_OPTS | DACF.DACF_ENC_ENABLED);
            controlFlags &= DACF.DACF_CONTROL_FLAGS_MASK;

            if (fAllowJitOpts != Interop.BOOL.FALSE)
            {
                controlFlags |= DACF.DACF_ALLOW_JIT_OPTS;
            }

            if (fEnableEnC != Interop.BOOL.FALSE)
            {
                bool fIgnorePdbs = (debuggerInfoBits & DACF.DACF_IGNORE_PDBS) != 0;
                bool canSetEnC = (loader.GetFlags(handle) & Contracts.ModuleFlags.EncCapable) != 0 && !CORProfilerPresent() && fIgnorePdbs;
                if (canSetEnC)
                {
                    controlFlags |= DACF.DACF_ENC_ENABLED;
                }
                else
                {
                    hr = CorDbgHResults.CORDBG_S_NOT_ALL_BITS_SET;
                }
            }

            loader.SetDebuggerInfoBits(handle, controlFlags);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.SetCompilerFlags(vmAssembly, fAllowJitOpts, fEnableEnC);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    public int EnumerateAssembliesInAppDomain(ulong vmAppDomain, delegate* unmanaged<ulong, nint, void> fpCallback, nint pUserData)
    {
        int hr = HResults.S_OK;
#if DEBUG
        List<ulong>? cdacAssemblies = _legacy is not null ? new() : null;
#endif
        try
        {
            if (fpCallback == null)
            {
                throw new ArgumentNullException(nameof(fpCallback));
            }

            if (vmAppDomain == 0)
            {
                return hr;
            }

            Contracts.ILoader loader = _target.Contracts.Loader;
            foreach (Contracts.ModuleHandle handle in loader.GetModuleHandles(
                new TargetPointer(vmAppDomain),
                AssemblyIterationFlags.IncludeLoading | AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution))
            {
                TargetPointer assembly = loader.GetAssembly(handle);
                fpCallback(assembly.Value, pUserData);
#if DEBUG
                cdacAssemblies?.Add(assembly.Value);
#endif
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null && fpCallback != null)
        {
            List<ulong> dacAssemblies = new();
            GCHandle dacHandle = GCHandle.Alloc(dacAssemblies);
            try
            {
                int hrLocal = _legacy.EnumerateAssembliesInAppDomain(vmAppDomain, &CollectEnumerationCallback, GCHandle.ToIntPtr(dacHandle));
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    Debug.Assert(
                        cdacAssemblies!.SequenceEqual(dacAssemblies),
                        $"Assembly enumeration mismatch - "
                        + $"cDAC: [{string.Join(",", cdacAssemblies!.Select(a => $"0x{a:x}"))}], "
                        + $"DAC: [{string.Join(",", dacAssemblies.Select(a => $"0x{a:x}"))}]");
                }
            }
            finally
            {
                dacHandle.Free();
            }
        }
#endif
        return hr;
    }

    public int EnumerateModulesInAssembly(ulong vmAssembly, nint fpCallback, nint pUserData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.EnumerateModulesInAssembly(vmAssembly, fpCallback, pUserData) : HResults.E_NOTIMPL;

    public int RequestSyncAtEvent()
    {
        int hr = HResults.S_OK;
        try
        {
            _target.Contracts.Debugger.RequestSyncAtEvent();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.RequestSyncAtEvent();
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    public int SetSendExceptionsOutsideOfJMC(Interop.BOOL sendExceptionsOutsideOfJMC)
    {
        int hr = HResults.S_OK;
        try
        {
            _target.Contracts.Debugger.SetSendExceptionsOutsideOfJMC(sendExceptionsOutsideOfJMC != Interop.BOOL.FALSE);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.SetSendExceptionsOutsideOfJMC(sendExceptionsOutsideOfJMC);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    public int MarkDebuggerAttachPending()
    {
        int hr = HResults.S_OK;
        try
        {
            Contracts.IDebugger debugger = _target.Contracts.Debugger;
            if (debugger.TryGetDebuggerData(out _))
            {
                debugger.MarkDebuggerAttachPending();
            }
            else
            {
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_NOTREADY)!;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.MarkDebuggerAttachPending();
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif

        return hr;
    }

    public int MarkDebuggerAttached(Interop.BOOL fAttached)
    {
        int hr = HResults.S_OK;
        try
        {
            Contracts.IDebugger debugger = _target.Contracts.Debugger;
            if (debugger.TryGetDebuggerData(out _))
            {
                debugger.MarkDebuggerAttached(fAttached != Interop.BOOL.FALSE);
            }
            else if (fAttached != Interop.BOOL.FALSE)
            {
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_NOTREADY)!;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.MarkDebuggerAttached(fAttached);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif

        return hr;
    }

    public int Hijack(ulong vmThread, uint dwThreadId, nint pRecord, nint pOriginalContext, uint cbSizeContext, int reason, nint pUserData, ulong* pRemoteContextAddr)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.Hijack(vmThread, dwThreadId, pRecord, pOriginalContext, cbSizeContext, reason, pUserData, pRemoteContextAddr) : HResults.E_NOTIMPL;

    public int EnumerateThreads(delegate* unmanaged<ulong, nint, void> fpCallback, nint pUserData)
    {
        int hr = HResults.S_OK;
#if DEBUG
        List<ulong>? cdacThreads = _legacy is not null ? new() : null;
#endif
        try
        {
            if (fpCallback == null)
                throw new ArgumentNullException(nameof(fpCallback));
            Contracts.IThread threadContract = _target.Contracts.Thread;
            Contracts.ThreadStoreData threadStore = threadContract.GetThreadStoreData();
            TargetPointer currentThread = threadStore.FirstThread;
            while (currentThread != TargetPointer.Null)
            {
                Contracts.ThreadData threadData = threadContract.GetThreadData(currentThread);
                // Match native: skip stopped and unstarted threads
                if ((threadData.State & (Contracts.ThreadState.Stopped | Contracts.ThreadState.Unstarted)) == 0)
                {
                    fpCallback(currentThread.Value, pUserData);
#if DEBUG
                    cdacThreads?.Add(currentThread.Value);
#endif
                }
                currentThread = threadData.NextThread;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            List<ulong> dacThreads = new();
            GCHandle dacHandle = GCHandle.Alloc(dacThreads);
            int hrLocal = _legacy.EnumerateThreads(&CollectEnumerationCallback, GCHandle.ToIntPtr(dacHandle));
            dacHandle.Free();
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(
                    cdacThreads!.SequenceEqual(dacThreads),
                    $"Thread enumeration mismatch - cDAC: [{string.Join(",", cdacThreads!.Select(t => $"0x{t:x}"))}], DAC: [{string.Join(",", dacThreads.Select(t => $"0x{t:x}"))}]");
            }
        }
#endif
        return hr;
    }

#if DEBUG
    [UnmanagedCallersOnly]
    private static void CollectEnumerationCallback(ulong value, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ((List<ulong>)handle.Target!).Add(value);
    }
#endif

    public int IsThreadMarkedDead(ulong vmThread, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(new TargetPointer(vmThread));
            *pResult = (threadData.State & Contracts.ThreadState.Stopped) != 0 ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.IsThreadMarkedDead(vmThread, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int GetThreadHandle(ulong vmThread, void** pRetVal)
    {
        int hr = HResults.S_OK;
        try
        {
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(new TargetPointer(vmThread));
            *pRetVal = (void*)threadData.ThreadHandle.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            void* retValLocal = null;
            int hrLocal = _legacy.GetThreadHandle(vmThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {(nuint)(*pRetVal):x}, DAC: {(nuint)retValLocal:x}");
        }
#endif
        return hr;
    }

    public int GetThreadObject(ulong vmThread, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(new TargetPointer(vmThread));
            if ((threadData.State & (Contracts.ThreadState.Stopped | Contracts.ThreadState.Unstarted | Contracts.ThreadState.Detached)) != 0)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_BAD_THREAD_STATE)!;

            *pRetVal = threadData.ExposedObjectHandle.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetThreadObject(vmThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal:x}, DAC: {retValLocal:x}");
        }
#endif
        return hr;
    }

    public int GetThreadAllocInfo(ulong vmThread, DacDbiThreadAllocInfo* pThreadAllocInfo)
    {
        int hr = HResults.S_OK;
        try
        {
            TargetPointer threadPtr = new TargetPointer(vmThread);
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(threadPtr);
            _target.Contracts.Thread.GetThreadAllocContext(threadPtr, out long allocBytes, out long allocBytesLoh);

            ulong limit = threadData.AllocContextLimit.Value;
            ulong pointer = threadData.AllocContextPointer.Value;
            pThreadAllocInfo->allocBytesSOH = (ulong)allocBytes - (limit - pointer);
            pThreadAllocInfo->allocBytesUOH = (ulong)allocBytesLoh;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            DacDbiThreadAllocInfo allocInfoLocal = default;
            int hrLocal = _legacy.GetThreadAllocInfo(vmThread, &allocInfoLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pThreadAllocInfo->allocBytesSOH == allocInfoLocal.allocBytesSOH, $"cDAC: {pThreadAllocInfo->allocBytesSOH}, DAC: {allocInfoLocal.allocBytesSOH}");
                Debug.Assert(pThreadAllocInfo->allocBytesUOH == allocInfoLocal.allocBytesUOH, $"cDAC: {pThreadAllocInfo->allocBytesUOH}, DAC: {allocInfoLocal.allocBytesUOH}");
            }
        }
#endif
        return hr;
    }

    public int SetDebugState(ulong vmThread, int debugState)
    {
        int hr = HResults.S_OK;
        try
        {
            TargetPointer threadPtr = new TargetPointer(vmThread);

            if (debugState == (int)CorDebugThreadState.ThreadSuspend)
            {
                _target.Contracts.Thread.SetDebuggerControlledThreadState(threadPtr, Contracts.DebuggerControlledThreadState.UserSuspend);
            }
            else if (debugState == (int)CorDebugThreadState.ThreadRun)
            {
                _target.Contracts.Thread.ResetDebuggerControlledThreadState(threadPtr, Contracts.DebuggerControlledThreadState.UserSuspend);
            }
            else
            {
                throw new ArgumentException("Invalid debug state value.", nameof(debugState));
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.SetDebugState(vmThread, debugState);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    public int HasUnhandledException(ulong vmThread, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(new TargetPointer(vmThread));
            *pResult = threadData.HasUnhandledException ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.HasUnhandledException(vmThread, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int GetUserState(ulong vmThread, int* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetUserState(vmThread, pRetVal) : HResults.E_NOTIMPL;

    public int GetPartialUserState(ulong vmThread, CorDebugUserState* pRetVal)
    {
        *pRetVal = default;
        int hr = HResults.S_OK;
        try
        {
            TargetPointer threadPtr = new TargetPointer(vmThread);
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(threadPtr);
            Contracts.ThreadState threadState = threadData.State;

            CorDebugUserState result = default;
            if ((threadState & Contracts.ThreadState.Background) != 0)
                result |= CorDebugUserState.USER_BACKGROUND;

            if ((threadState & Contracts.ThreadState.Unstarted) != 0)
                result |= CorDebugUserState.USER_UNSTARTED;

            if ((threadState & Contracts.ThreadState.Stopped) != 0)
                result |= CorDebugUserState.USER_STOPPED;

            if ((threadState & Contracts.ThreadState.WaitSleepJoin) != 0)
                result |= CorDebugUserState.USER_WAIT_SLEEP_JOIN;

            if ((threadState & Contracts.ThreadState.ThreadPoolWorker) != 0)
                result |= CorDebugUserState.USER_THREADPOOL;

            *pRetVal = result;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            CorDebugUserState retValLocal;
            int hrLocal = _legacy.GetPartialUserState(vmThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetConnectionID(ulong vmThread, uint* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
#if DEBUG
        if (_legacy is not null)
        {
            uint retValLocal;
            int hrLocal = _legacy.GetConnectionID(vmThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetTaskID(ulong vmThread, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetTaskID(vmThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int TryGetVolatileOSThreadID(ulong vmThread, uint* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(new TargetPointer(vmThread));
            uint osId = (uint)threadData.OSId.Value;
            // Match native: SWITCHED_OUT_FIBER_OSID (0xbaadf00d) means thread is switched out
            const uint SWITCHED_OUT_FIBER_OSID = 0xbaadf00d;
            *pRetVal = osId == SWITCHED_OUT_FIBER_OSID ? 0 : osId;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            uint retValLocal;
            int hrLocal = _legacy.TryGetVolatileOSThreadID(vmThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetUniqueThreadID(ulong vmThread, uint* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(new TargetPointer(vmThread));
            *pRetVal = (uint)threadData.OSId.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            uint retValLocal;
            int hrLocal = _legacy.GetUniqueThreadID(vmThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetCurrentException(ulong vmThread, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            TargetPointer threadPtr = new TargetPointer(vmThread);
            TargetPointer exceptionHandle = _target.Contracts.Thread.GetCurrentExceptionHandle(threadPtr);
            if (exceptionHandle == TargetPointer.Null)
            {
                ThreadData data = _target.Contracts.Thread.GetThreadData(threadPtr);
                if (data.LastThrownObjectIsUnhandled)
                    exceptionHandle = data.LastThrownObjectHandle;
            }
            *pRetVal = exceptionHandle.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetCurrentException(vmThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal:x}, DAC: {retValLocal:x}");
        }
#endif
        return hr;
    }

    public int GetObjectForCCW(ulong ccwPtr, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            TargetPointer objectHandle = TargetPointer.Null;
            TargetPointer ccwAddress = new(ccwPtr);
            bool comWrappersSuccess = false;

            if (_target.Contracts.TryGetContract<IComWrappers>(out IComWrappers? comWrappers))
            {
                TargetPointer managedObjectWrapper = comWrappers.GetManagedObjectWrapperFromCCW(ccwAddress);
                if (managedObjectWrapper != TargetPointer.Null)
                {
                    comWrappersSuccess = _target.TryReadPointer(managedObjectWrapper, out objectHandle);
                }
            }

            if (!comWrappersSuccess && _target.Contracts.TryGetContract<IBuiltInCOM>(out IBuiltInCOM? builtInCOM))
            {
                TargetPointer ccw = builtInCOM.GetCCWFromInterfacePointer(ccwAddress);
                if (ccw == TargetPointer.Null)
                {
                    ccw = ccwAddress;
                }
                ccw = builtInCOM.GetStartWrapper(ccw);
                objectHandle = builtInCOM.GetObjectHandle(ccw);
            }

            *pRetVal = objectHandle.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetObjectForCCW(ccwPtr, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal:x}, DAC: {retValLocal:x}");
        }
#endif
        return hr;
    }

    public int GetCurrentCustomDebuggerNotification(ulong vmThread, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(new TargetPointer(vmThread));
            *pRetVal = threadData.CurrentCustomDebuggerNotificationHandle.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetCurrentCustomDebuggerNotification(vmThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal:x}, DAC: {retValLocal:x}");
        }
#endif
        return hr;
    }

    public int GetCurrentAppDomain(ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            TargetPointer appDomainPtr = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
            *pRetVal = _target.ReadPointer(appDomainPtr);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetCurrentAppDomain(&retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal:x}, DAC: {retValLocal:x}");
        }
#endif
        return hr;
    }

    public int ResolveAssembly(ulong vmScope, uint tkAssemblyRef, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle scopeModule = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmScope));
            Contracts.ModuleLookupTables lookupTables = loader.GetLookupTables(scopeModule);
            TargetPointer referencedModule = loader.GetModuleLookupMapElement(lookupTables.ManifestModuleReferences, tkAssemblyRef, out _);
            if (referencedModule != TargetPointer.Null)
            {
                Contracts.ModuleHandle referencedModuleHandle = loader.GetModuleHandleFromModulePtr(referencedModule);
                *pRetVal = loader.GetAssembly(referencedModuleHandle).Value;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.ResolveAssembly(vmScope, tkAssemblyRef, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetNativeCodeSequencePointsAndVarInfo(ulong vmMethodDesc, ulong startAddress, Interop.BOOL fCodeAvailable, nint pNativeVarData, nint pSequencePoints)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetNativeCodeSequencePointsAndVarInfo(vmMethodDesc, startAddress, fCodeAvailable, pNativeVarData, pSequencePoints) : HResults.E_NOTIMPL;

    public int GetManagedStoppedContext(ulong vmThread, ulong* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetManagedStoppedContext(vmThread, pRetVal) : HResults.E_NOTIMPL;

    public int CreateStackWalk(ulong vmThread, nint pInternalContextBuffer, nuint* ppSFIHandle)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.CreateStackWalk(vmThread, pInternalContextBuffer, ppSFIHandle) : HResults.E_NOTIMPL;

    public int DeleteStackWalk(nuint ppSFIHandle)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.DeleteStackWalk(ppSFIHandle) : HResults.E_NOTIMPL;

    public int GetStackWalkCurrentContext(nuint pSFIHandle, nint pContext)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetStackWalkCurrentContext(pSFIHandle, pContext) : HResults.E_NOTIMPL;

    public int SetStackWalkCurrentContext(ulong vmThread, nuint pSFIHandle, int flag, nint pContext)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.SetStackWalkCurrentContext(vmThread, pSFIHandle, flag, pContext) : HResults.E_NOTIMPL;

    public int UnwindStackWalkFrame(nuint pSFIHandle, Interop.BOOL* pResult)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.UnwindStackWalkFrame(pSFIHandle, pResult) : HResults.E_NOTIMPL;

    public int CheckContext(ulong vmThread, nint pContext)
    {
        int hr = HResults.S_OK;
        try
        {
            IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(_target);
            ctx.FillFromBuffer(new Span<byte>((void*)pContext, (int)ctx.Size));

            if ((ctx.RawContextFlags & ctx.ContextControlFlags) != 0)
            {
                _target.Contracts.Thread.GetStackLimitData(new TargetPointer(vmThread), out TargetPointer stackBase, out TargetPointer stackLimit, out _);
                TargetPointer sp = ctx.StackPointer;
                if (sp < stackLimit || stackBase <= sp)
                {
                    hr = CorDbgHResults.CORDBG_E_NON_MATCHING_CONTEXT;
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.CheckContext(vmThread, pContext);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    public int GetStackWalkCurrentFrameInfo(nuint pSFIHandle, nint pFrameData, int* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetStackWalkCurrentFrameInfo(pSFIHandle, pFrameData, pRetVal) : HResults.E_NOTIMPL;

    // Filter used by GetCountOfInternalFrames and EnumerateInternalFrames to decide
    // which entries from IStackWalk.GetFrames should be surfaced to the DBI as internal frames.
    private static bool IsReportedInternalFrame(IStackWalk stackwalk, Contracts.StackFrameData frame)
    {
        return frame.InternalFrameType != Contracts.InternalFrameType.None
            && stackwalk.GetFrameName(frame.FrameIdentifier) != "InterpreterFrame"
            && !stackwalk.IsExceptionHandlingHelperInlinedCallFrame(frame.FrameAddress);
    }

    public int GetCountOfInternalFrames(ulong vmThread, uint* pRetVal)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pRetVal == null)
                throw new ArgumentNullException(nameof(pRetVal));

            uint count = 0;
            IStackWalk stackwalk = _target.Contracts.StackWalk;
            foreach (Contracts.StackFrameData frame in stackwalk.GetFrames(new TargetPointer(vmThread)))
            {
                if (IsReportedInternalFrame(stackwalk, frame))
                    count++;
            }
            *pRetVal = count;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            uint dacCount;
            int hrLocal = _legacy.GetCountOfInternalFrames(vmThread, &dacCount);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == dacCount, $"Internal frame count mismatch - cDAC: {*pRetVal}, DAC: {dacCount}");
        }
#endif
        return hr;
    }

    public int EnumerateInternalFrames(ulong vmThread, delegate* unmanaged<Debugger_STRData*, void*, void> fpCallback, nint pUserData)
    {
        int hr = HResults.S_OK;
#if DEBUG
        List<Debugger_STRData>? cdacFrames = _legacy is not null ? new() : null;
#endif
        try
        {
            TargetPointer threadPtr = new TargetPointer(vmThread);
            TargetPointer currentAppDomain = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.AppDomain));
            IStackWalk stackwalk = _target.Contracts.StackWalk;

            foreach (Contracts.StackFrameData frame in stackwalk.GetFrames(threadPtr))
            {
                if (!IsReportedInternalFrame(stackwalk, frame))
                    continue;

                TargetPointer vmAssembly;
                uint funcMetadataToken;
                TargetPointer vmMethodDesc;
                if (frame.InternalFrameType == Contracts.InternalFrameType.FuncEval)
                {
                    Contracts.DebuggerEvalData evalData = stackwalk.GetDebuggerEvalData(frame.FrameAddress);
                    funcMetadataToken = evalData.MethodToken;
                    vmAssembly = evalData.AssemblyPtr;
                    vmMethodDesc = TargetPointer.Null;
                }
                else
                {
                    TargetPointer methodDescPtr = stackwalk.GetMethodDescPtr(frame.FrameAddress);
                    ResolveStubFrameAssemblyAndToken(methodDescPtr, out vmAssembly, out funcMetadataToken);
                    vmMethodDesc = methodDescPtr;
                }

                // ctx and rd are intentionally left as 0 (consumer does not read for cStubFrame).
                Debugger_STRData data = default;
                data.fp = frame.FrameAddress.Value;
                data.vmCurrentAppDomainToken = currentAppDomain.Value;
                data.eType = Debugger_STRData.EType.cStubFrame;
                data.stubFrame.funcMetadataToken = funcMetadataToken;
                data.stubFrame.vmAssembly = vmAssembly.Value;
                data.stubFrame.vmMethodDesc = vmMethodDesc.Value;
                data.stubFrame.frameType = (int)ToCorDebugInternalFrameType(frame.InternalFrameType);
#if DEBUG
                cdacFrames?.Add(data);
#endif
                fpCallback(&data, (void*)pUserData);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            List<Debugger_STRData> dacFrames = new();
            GCHandle dacHandle = GCHandle.Alloc(dacFrames);
            int hrLocal = _legacy.EnumerateInternalFrames(vmThread, (delegate* unmanaged<Debugger_STRData*, void*, void>)&CollectStubFrameCallback, GCHandle.ToIntPtr(dacHandle));
            dacHandle.Free();
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(cdacFrames!.Count == dacFrames.Count, $"Internal frame count mismatch - cDAC: {cdacFrames!.Count}, DAC: {dacFrames.Count}");
                int n = Math.Min(cdacFrames!.Count, dacFrames.Count);
                for (int i = 0; i < n; i++)
                {
                    Debugger_STRData c = cdacFrames![i];
                    Debugger_STRData d = dacFrames[i];
                    Debug.Assert(c.fp == d.fp, $"Frame[{i}] fp mismatch - cDAC: 0x{c.fp:x}, DAC: 0x{d.fp:x}");
                    Debug.Assert(c.vmCurrentAppDomainToken == d.vmCurrentAppDomainToken, $"Frame[{i}] vmCurrentAppDomainToken mismatch - cDAC: 0x{c.vmCurrentAppDomainToken:x}, DAC: 0x{d.vmCurrentAppDomainToken:x}");
                    Debug.Assert(c.eType == d.eType, $"Frame[{i}] eType mismatch - cDAC: {c.eType}, DAC: {d.eType}");
                    Debug.Assert(c.stubFrame.funcMetadataToken == d.stubFrame.funcMetadataToken, $"Frame[{i}] funcMetadataToken mismatch - cDAC: 0x{c.stubFrame.funcMetadataToken:x}, DAC: 0x{d.stubFrame.funcMetadataToken:x}");
                    Debug.Assert(c.stubFrame.vmAssembly == d.stubFrame.vmAssembly, $"Frame[{i}] vmAssembly mismatch - cDAC: 0x{c.stubFrame.vmAssembly:x}, DAC: 0x{d.stubFrame.vmAssembly:x}");
                    Debug.Assert(c.stubFrame.vmMethodDesc == d.stubFrame.vmMethodDesc, $"Frame[{i}] vmMethodDesc mismatch - cDAC: 0x{c.stubFrame.vmMethodDesc:x}, DAC: 0x{d.stubFrame.vmMethodDesc:x}");
                    Debug.Assert(c.stubFrame.frameType == d.stubFrame.frameType, $"Frame[{i}] frameType mismatch - cDAC: {c.stubFrame.frameType}, DAC: {d.stubFrame.frameType}");
                }
            }
        }
#endif
        return hr;
    }

#if DEBUG
    [UnmanagedCallersOnly]
    private static void CollectStubFrameCallback(Debugger_STRData* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        ((List<Debugger_STRData>)handle.Target!).Add(*data);
    }
#endif

    private void ResolveStubFrameAssemblyAndToken(TargetPointer methodDescPtr, out TargetPointer vmAssembly, out uint funcMetadataToken)
    {
        vmAssembly = TargetPointer.Null;
        funcMetadataToken = 0; // mdTokenNil
        if (methodDescPtr == TargetPointer.Null)
            return;

        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
        funcMetadataToken = rts.GetMethodToken(mdHandle);

        TargetPointer mtPtr = rts.GetMethodTable(mdHandle);
        if (mtPtr == TargetPointer.Null)
            return;
        TypeHandle typeHandle = rts.GetTypeHandle(mtPtr);
        TargetPointer modulePtr = rts.GetModule(typeHandle);
        if (modulePtr == TargetPointer.Null)
            return;

        ILoader loader = _target.Contracts.Loader;
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);
        vmAssembly = loader.GetAssembly(moduleHandle);
    }

    private static CorDebugInternalFrameType ToCorDebugInternalFrameType(Contracts.InternalFrameType frameType)
        => frameType switch
        {
            Contracts.InternalFrameType.None => CorDebugInternalFrameType.STUBFRAME_NONE,
            Contracts.InternalFrameType.M2U => CorDebugInternalFrameType.STUBFRAME_M2U,
            Contracts.InternalFrameType.U2M => CorDebugInternalFrameType.STUBFRAME_U2M,
            Contracts.InternalFrameType.FuncEval => CorDebugInternalFrameType.STUBFRAME_FUNC_EVAL,
            Contracts.InternalFrameType.InternalCall => CorDebugInternalFrameType.STUBFRAME_INTERNALCALL,
            Contracts.InternalFrameType.ClassInit => CorDebugInternalFrameType.STUBFRAME_CLASS_INIT,
            Contracts.InternalFrameType.Exception => CorDebugInternalFrameType.STUBFRAME_EXCEPTION,
            Contracts.InternalFrameType.JitCompilation => CorDebugInternalFrameType.STUBFRAME_JIT_COMPILATION,
            _ => CorDebugInternalFrameType.STUBFRAME_NONE,
        };

    public int GetStackParameterSize(ulong controlPC, uint* pRetVal)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pRetVal == null)
                throw new ArgumentNullException(nameof(pRetVal));

            *pRetVal = 0;
            IExecutionManager eman = _target.Contracts.ExecutionManager;
            if (eman.GetCodeBlockHandle(new TargetCodePointer(controlPC)) is not CodeBlockHandle cbh)
                throw new InvalidOperationException($"No code block found for controlPC 0x{controlPC:x}");

            *pRetVal = eman.GetStackParameterSize(cbh);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            uint retValLocal;
            int hrLocal = _legacy.GetStackParameterSize(controlPC, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetFramePointer(nuint pSFIHandle, ulong* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetFramePointer(pSFIHandle, pRetVal) : HResults.E_NOTIMPL;

    public int IsLeafFrame(ulong vmThread, byte* pContext, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            IPlatformAgnosticContext leafCtx = IPlatformAgnosticContext.GetContextForPlatform(_target);
            uint allFlags = leafCtx.AllContextFlags;
            byte[] leafContext = _target.Contracts.Thread.GetContext(new TargetPointer(vmThread), ThreadContextSource.None, allFlags);
            leafCtx.FillFromBuffer(leafContext);

            // Read the given context from the native buffer.
            IPlatformAgnosticContext givenCtx = IPlatformAgnosticContext.GetContextForPlatform(_target);
            givenCtx.FillFromBuffer(new Span<byte>(pContext, leafContext.Length));

            *pResult = givenCtx.StackPointer == leafCtx.StackPointer
                && givenCtx.InstructionPointer == leafCtx.InstructionPointer
                ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.IsLeafFrame(vmThread, pContext, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int GetContext(ulong vmThread, byte* pContextBuffer)
    {
        int hr = HResults.S_OK;
        try
        {
            uint allFlags = IPlatformAgnosticContext.GetContextForPlatform(_target).AllContextFlags;
            byte[] context = _target.Contracts.Thread.GetContext(new TargetPointer(vmThread), ThreadContextSource.Debugger, allFlags);

            context.AsSpan().CopyTo(new Span<byte>(pContextBuffer, context.Length));
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            uint contextSize = IPlatformAgnosticContext.GetContextForPlatform(_target).Size;
            byte[] localContextBuf = new byte[contextSize];
            fixed (byte* pLocal = localContextBuf)
            {
                int hrLocal = _legacy.GetContext(vmThread, pLocal);
                Debug.ValidateHResult(hr, hrLocal);

                if (hr == HResults.S_OK)
                {
                    IPlatformAgnosticContext contextStruct = IPlatformAgnosticContext.GetContextForPlatform(_target);
                    IPlatformAgnosticContext localContextStruct = IPlatformAgnosticContext.GetContextForPlatform(_target);
                    contextStruct.FillFromBuffer(new Span<byte>(pContextBuffer, (int)contextSize));
                    localContextStruct.FillFromBuffer(localContextBuf);

                    Debug.Assert(contextStruct.Equals(localContextStruct));
                }
            }
        }
#endif
        return hr;
    }

    public int ConvertContextToDebuggerRegDisplay(nint pInContext, nint pOutDRD, Interop.BOOL fActive)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.ConvertContextToDebuggerRegDisplay(pInContext, pOutDRD, fActive) : HResults.E_NOTIMPL;

    public int IsDiagnosticsHiddenOrLCGMethod(ulong vmMethodDesc, int* pRetVal)
    {
        *pRetVal = (int)DynamicMethodType.kNone;
        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            Contracts.MethodDescHandle md = rts.GetMethodDescHandle(new TargetPointer(vmMethodDesc));
            if (rts.IsILStub(md) || rts.IsAsyncThunkMethod(md) || rts.IsWrapperStub(md))
            {
                *pRetVal = (int)DynamicMethodType.kDiagnosticHidden;
            }
            else if (rts.IsDynamicMethod(md))
            {
                *pRetVal = (int)DynamicMethodType.kLCGMethod;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            int resultLocal;
            int hrLocal = _legacy.IsDiagnosticsHiddenOrLCGMethod(vmMethodDesc, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == resultLocal, $"cDAC: {*pRetVal}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int GetVarArgSig(ulong VASigCookieAddr, ulong* pArgBase, DacDbiTargetBuffer* pRetVal)
    {
        *pArgBase = 0;
        *pRetVal = default;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ISignature signature = _target.Contracts.Signature;
            TargetPointer argBase = signature.GetVarArgArgsBase(new TargetPointer(VASigCookieAddr));
            signature.GetVarArgSignature(new TargetPointer(VASigCookieAddr), out TargetPointer sigAddr, out uint sigLen);

            *pArgBase = argBase.Value;
            *pRetVal = new DacDbiTargetBuffer { pAddress = sigAddr.Value, cbSize = sigLen };
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong argBaseLocal;
            DacDbiTargetBuffer retValLocal = default;
            int hrLocal = _legacy.GetVarArgSig(VASigCookieAddr, &argBaseLocal, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pArgBase == argBaseLocal, $"cDAC argBase: 0x{*pArgBase:X}, DAC argBase: 0x{argBaseLocal:X}");
                Debug.Assert(pRetVal->pAddress == retValLocal.pAddress, $"cDAC sigAddr: 0x{pRetVal->pAddress:X}, DAC sigAddr: 0x{retValLocal.pAddress:X}");
                Debug.Assert(pRetVal->cbSize == retValLocal.cbSize, $"cDAC sigLen: {pRetVal->cbSize}, DAC sigLen: {retValLocal.cbSize}");
            }
        }
#endif
        return hr;
    }

    public int RequiresAlign8(ulong thExact, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        RuntimeInfoArchitecture arch = _target.Contracts.RuntimeInfo.GetTargetArchitecture();
        try
        {
            // Some 32-bit platform ABIs require 64-bit alignment (FEATURE_64BIT_ALIGNMENT).
            if (arch == RuntimeInfoArchitecture.Arm || arch == RuntimeInfoArchitecture.Wasm)
            {
                Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                Contracts.TypeHandle th = rts.GetTypeHandle(new TargetPointer(thExact));
                *pResult = rts.RequiresAlign8(th) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.RequiresAlign8(thExact, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int ResolveExactGenericArgsToken(uint dwExactGenericArgsTokenIndex, ulong rawToken, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            if (dwExactGenericArgsTokenIndex == 0)
            {
                // In a rare case of VS4Mac debugging VS4Mac ARM64 optimized code we get a null
                // generics argument token. We aren't sure why the token is null, it may be a bug
                // or it may be by design in the runtime. In the interest of time we are working
                // around the issue rather than investigating the root cause.
                if (rawToken == 0)
                {
                    *pRetVal = rawToken;
                }
                else
                {
                    // The real generics type token is the MethodTable of the "this" object.
                    // The incoming rawToken is a target address of the 'this' Object.
                    *pRetVal = _target.Contracts.Object.GetMethodTableAddress(new TargetPointer(rawToken)).Value;
                }
            }
            else if (dwExactGenericArgsTokenIndex == unchecked((uint)IlNum.TYPECTXT_ILNUM))
            {
                // rawToken is already the real generics type token. Nothing to do.
                *pRetVal = rawToken;
            }
            else
            {
                // The index of the generics type token should not be anything else.
                Debug.Fail($"Unexpected generics type token index: {dwExactGenericArgsTokenIndex}");
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_TARGET_INCONSISTENT)!;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.ResolveExactGenericArgsToken(dwExactGenericArgsTokenIndex, rawToken, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetILCodeAndSig(ulong vmAssembly, uint functionToken, DacDbiTargetBuffer* pTargetBuffer, uint* pLocalSigToken)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetILCodeAndSig(vmAssembly, functionToken, pTargetBuffer, pLocalSigToken) : HResults.E_NOTIMPL;

    public int GetNativeCodeInfo(ulong vmAssembly, uint functionToken, nint pJitManagerList)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetNativeCodeInfo(vmAssembly, functionToken, pJitManagerList) : HResults.E_NOTIMPL;

    public int GetNativeCodeInfoForAddr(ulong codeAddress, nint pCodeInfo, ulong* pVmModule, uint* pFunctionToken)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetNativeCodeInfoForAddr(codeAddress, pCodeInfo, pVmModule, pFunctionToken) : HResults.E_NOTIMPL;

    public int IsValueType(ulong vmTypeHandle, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            Contracts.TypeHandle th = rts.GetTypeHandle(new TargetPointer(vmTypeHandle));
            *pResult = rts.IsValueType(th) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.IsValueType(vmTypeHandle, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int HasTypeParams(ulong vmTypeHandle, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle typeHandle = rts.GetTypeHandle(new TargetPointer(vmTypeHandle));
            *pResult = rts.ContainsGenericVariables(typeHandle) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.HasTypeParams(vmTypeHandle, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int GetClassInfo(ulong thExact, nint pData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetClassInfo(thExact, pData) : HResults.E_NOTIMPL;

    public int GetInstantiationFieldInfo(ulong vmAssembly, ulong vmTypeHandle, ulong vmExactMethodTable, nint pFieldList, nuint* pObjectSize)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetInstantiationFieldInfo(vmAssembly, vmTypeHandle, vmExactMethodTable, pFieldList, pObjectSize) : HResults.E_NOTIMPL;

    public int TypeHandleToExpandedTypeInfo(AreValueTypesBoxed boxed, ulong vmTypeHandle, DebuggerIPCE_ExpandedTypeData* pTypeInfo)
    {
        int hr = HResults.S_OK;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle th = rts.GetTypeHandle(new TargetPointer(vmTypeHandle));
            TypeHandleToExpandedTypeInfoImpl(rts, boxed, th, pTypeInfo);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            DebuggerIPCE_ExpandedTypeData dataLocal;
            int hrLocal = _legacy.TypeHandleToExpandedTypeInfo(boxed, vmTypeHandle, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                ValidateExpandedTypeData(pTypeInfo, &dataLocal);
            }
        }
#endif
        return hr;
    }

    public int GetObjectExpandedTypeInfo(AreValueTypesBoxed boxed, ulong addr, DebuggerIPCE_ExpandedTypeData* pTypeInfo)
    {
        int hr = HResults.S_OK;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TargetPointer mtAddr = _target.Contracts.Object.GetMethodTableAddress(new TargetPointer(addr));
            TypeHandle th = rts.GetTypeHandle(mtAddr);
            TypeHandleToExpandedTypeInfoImpl(rts, boxed, th, pTypeInfo);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            DebuggerIPCE_ExpandedTypeData dataLocal;
            int hrLocal = _legacy.GetObjectExpandedTypeInfo(boxed, addr, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                ValidateExpandedTypeData(pTypeInfo, &dataLocal);
            }
        }
#endif
        return hr;
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
#endif

    public int GetTypeHandle(ulong vmModule, uint metadataToken, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader loader = _target.Contracts.Loader;
            TargetPointer module = new TargetPointer(vmModule);
            Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(module);
            Contracts.ModuleLookupTables lookupTables = loader.GetLookupTables(moduleHandle);
            switch ((EcmaMetadataUtils.TokenType)(metadataToken & EcmaMetadataUtils.TokenTypeMask))
            {
                case EcmaMetadataUtils.TokenType.mdtTypeDef:
                    *pRetVal = loader.GetModuleLookupMapElement(lookupTables.TypeDefToMethodTable, metadataToken, out var _).Value;
                    break;
                case EcmaMetadataUtils.TokenType.mdtTypeRef:
                    *pRetVal = loader.GetModuleLookupMapElement(lookupTables.TypeRefToMethodTable, metadataToken, out var _).Value;
                    break;
                default:
                    throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
            }
            if (*pRetVal == 0)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetTypeHandle(vmModule, metadataToken, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetApproxTypeHandle(nint pTypeData, ulong* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetApproxTypeHandle(pTypeData, pRetVal) : HResults.E_NOTIMPL;

    public int GetExactTypeHandle(nint pTypeData, nint pArgInfo, ulong* pVmTypeHandle)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetExactTypeHandle(pTypeData, pArgInfo, pVmTypeHandle) : HResults.E_NOTIMPL;

    public int GetMethodDescParams(ulong vmMethodDesc, ulong genericsToken, uint* pcGenericClassTypeParams, nint pGenericTypeParams)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetMethodDescParams(vmMethodDesc, genericsToken, pcGenericClassTypeParams, pGenericTypeParams) : HResults.E_NOTIMPL;

    public int GetThreadStaticAddress(ulong vmField, ulong vmRuntimeThread, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TargetPointer fd = new TargetPointer(vmField);
            if (vmRuntimeThread == 0)
                throw new ArgumentException("vmRuntimeThread cannot be null for thread static fields");
            if (!rts.IsFieldDescThreadStatic(fd))
            {
                throw new NotImplementedException();
            }
            *pRetVal = rts.GetFieldDescThreadStaticAddress(fd, new TargetPointer(vmRuntimeThread)).Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetThreadStaticAddress(vmField, vmRuntimeThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetCollectibleTypeStaticAddress(ulong vmField, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TargetPointer fd = new TargetPointer(vmField);
            *pRetVal = rts.GetFieldDescStaticAddress(fd, unboxValueTypes: false).Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetCollectibleTypeStaticAddress(vmField, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    public int GetEnCHangingFieldInfo(nint pEnCFieldInfo, nint pFieldData, Interop.BOOL* pfStatic)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetEnCHangingFieldInfo(pEnCFieldInfo, pFieldData, pfStatic) : HResults.E_NOTIMPL;

    public int GetTypeHandleParams(ulong vmTypeHandle, nint pParams)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetTypeHandleParams(vmTypeHandle, pParams) : HResults.E_NOTIMPL;

    public int GetSimpleType(int simpleType, uint* pMetadataToken, ulong* pVmModule)
    {
        Debug.Assert(pVmModule != null);
        *pVmModule = 0;
        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            Contracts.TypeHandle typeHandle = rts.GetPrimitiveType((CorElementType)simpleType);

            if (typeHandle.IsNull)
            {
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
            }

            Debug.Assert(pMetadataToken != null);
            *pMetadataToken = rts.GetTypeDefToken(typeHandle);

            TargetPointer module = rts.GetModule(typeHandle);
            if (module == TargetPointer.Null)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_TARGET_INCONSISTENT)!;

            *pVmModule = module.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            uint metadataTokenLocal;
            ulong vmModuleLocal;
            int hrLocal = _legacy.GetSimpleType(simpleType, &metadataTokenLocal, &vmModuleLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pMetadataToken == metadataTokenLocal, $"cDAC: {*pMetadataToken}, DAC: {metadataTokenLocal}");
                Debug.Assert(*pVmModule == vmModuleLocal, $"cDAC: {*pVmModule}, DAC: {vmModuleLocal}");
            }
        }
#endif
        return hr;
    }

    public int IsExceptionObject(ulong vmObject, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TargetPointer objectAddress = new TargetPointer(vmObject);
            TargetPointer parentMT = _target.Contracts.Object.GetMethodTableAddress(objectAddress);
            TargetPointer exceptionMT = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.ExceptionMethodTable));

            while (parentMT != TargetPointer.Null)
            {
                if (parentMT == exceptionMT)
                {
                    *pResult = Interop.BOOL.TRUE;
                    break;
                }

                TypeHandle typeHandle = rts.GetTypeHandle(parentMT);
                parentMT = rts.GetParentMethodTable(typeHandle);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.IsExceptionObject(vmObject, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int GetStackFramesFromException(ulong vmObject, nint pDacStackFrames)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetStackFramesFromException(vmObject, pDacStackFrames) : HResults.E_NOTIMPL;

    public int IsRcw(ulong vmObject, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            IObject obj = _target.Contracts.Object;
            _ = obj.GetBuiltInComData(new TargetPointer(vmObject), out TargetPointer rcw, out _, out _);
            *pResult = rcw != TargetPointer.Null ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.IsRcw(vmObject, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

#if DEBUG
    [ThreadStatic]
    private static List<ulong>? _debugEnumerateRcwCachedInterfacePointers;

    private static List<ulong> DebugEnumerateRcwCachedInterfacePointers
        => _debugEnumerateRcwCachedInterfacePointers ??= new();

    [UnmanagedCallersOnly]
    private static void EnumerateRcwCachedInterfacePointersDebugCallback(ulong itfPtr, nint _)
    {
        DebugEnumerateRcwCachedInterfacePointers.Add(itfPtr);
    }
#endif

    public int EnumerateRcwCachedInterfacePointers(ulong vmObject, delegate* unmanaged<ulong, nint, void> fpCallback, nint pUserData)
    {
        int hr = HResults.S_OK;
        List<ulong> itfPtrs = new();
        try
        {
            if (fpCallback is null)
                throw new ArgumentNullException(nameof(fpCallback));

            IObject obj = _target.Contracts.Object;
            _ = obj.GetBuiltInComData(new TargetPointer(vmObject), out TargetPointer rcw, out _, out _);
            if (rcw != TargetPointer.Null)
            {
                IBuiltInCOM builtInCom = _target.Contracts.BuiltInCOM;
                foreach ((TargetPointer methodTable, TargetPointer unknown) in builtInCom.GetRCWInterfaces(rcw))
                {
                    if (methodTable != TargetPointer.Null && unknown != TargetPointer.Null)
                        itfPtrs.Add(unknown.Value);
                }
            }

            foreach (ulong itfPtr in itfPtrs)
                fpCallback(itfPtr, pUserData);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            DebugEnumerateRcwCachedInterfacePointers.Clear();
            delegate* unmanaged<ulong, nint, void> debugCallbackPtr = &EnumerateRcwCachedInterfacePointersDebugCallback;
            int hrLocal = _legacy.EnumerateRcwCachedInterfacePointers(vmObject, debugCallbackPtr, 0);
            Debug.ValidateHResult(hr, hrLocal);

            if (hr == HResults.S_OK)
            {
                List<ulong> legacyItfPtrs = DebugEnumerateRcwCachedInterfacePointers;
                Debug.Assert(itfPtrs.SequenceEqual(legacyItfPtrs),
                    $"cDAC: [{string.Join(",", itfPtrs.Select(p => $"0x{p:x}"))}], DAC: [{string.Join(",", legacyItfPtrs.Select(p => $"0x{p:x}"))}]");
            }
            DebugEnumerateRcwCachedInterfacePointers.Clear();
        }
#endif
        return hr;
    }

    public int GetTypedByRefInfo(ulong pTypedByRef, nint pObjectData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetTypedByRefInfo(pTypedByRef, pObjectData) : HResults.E_NOTIMPL;

    public int GetStringData(ulong objectAddress, nint pObjectData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetStringData(objectAddress, pObjectData) : HResults.E_NOTIMPL;

    public int GetArrayData(ulong objectAddress, nint pObjectData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetArrayData(objectAddress, pObjectData) : HResults.E_NOTIMPL;

    public int GetBasicObjectInfo(ulong objectAddress, int type, nint pObjectData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetBasicObjectInfo(objectAddress, type, pObjectData) : HResults.E_NOTIMPL;

    public int GetDebuggerControlBlockAddress(ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            *pRetVal = _target.Contracts.Debugger.GetDebuggerControlBlockAddress().Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetDebuggerControlBlockAddress(&retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal:x}, DAC: {retValLocal:x}");
        }
#endif
        return hr;
    }

    public int GetObjectFromRefPtr(ulong ptr, ulong* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            *pRetVal = _target.ReadPointer(new TargetPointer(ptr)).Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetObjectFromRefPtr(ptr, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal:x}, DAC: {retValLocal:x}");
        }
#endif
        return hr;
    }

    public int GetObject(ulong ptr, ulong* pRetVal)
    {
        // Native GetObject wraps the address directly in a VMPTR_Object without dereferencing.
        *pRetVal = ptr;
        int hr = HResults.S_OK;
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetObject(ptr, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal:x}, DAC: {retValLocal:x}");
        }
#endif
        return hr;
    }

    public int GetVmObjectHandle(ulong handleAddress, ulong* pRetVal)
    {
        *pRetVal = handleAddress;
        int hr = HResults.S_OK;
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetVmObjectHandle(handleAddress, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal:x}, DAC: {retValLocal:x}");
        }
#endif
        return hr;
    }

    public int IsVmObjectHandleValid(ulong vmHandle, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            TargetPointer obj = _target.ReadPointer(new TargetPointer(vmHandle));
            *pResult = obj != TargetPointer.Null ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.IsVmObjectHandleValid(vmHandle, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int GetHandleAddressFromVmHandle(ulong vmHandle, ulong* pRetVal)
    {
        *pRetVal = vmHandle;
        int hr = HResults.S_OK;
#if DEBUG
        if (_legacy is not null)
        {
            ulong retValLocal;
            int hrLocal = _legacy.GetHandleAddressFromVmHandle(vmHandle, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal:x}, DAC: {retValLocal:x}");
        }
#endif
        return hr;
    }

    public int GetObjectContents(ulong obj, DacDbiTargetBuffer* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetObjectContents(obj, pRetVal) : HResults.E_NOTIMPL;

    public int GetThreadOwningMonitorLock(ulong vmObject, DacDbiMonitorLockInfo* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetThreadOwningMonitorLock(vmObject, pRetVal) : HResults.E_NOTIMPL;

    public int EnumerateMonitorEventWaitList(ulong vmObject, nint fpCallback, nint pUserData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.EnumerateMonitorEventWaitList(vmObject, fpCallback, pUserData) : HResults.E_NOTIMPL;

    public int GetAttachStateFlags(int* pRetVal)
    {
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            *pRetVal = _target.Contracts.Debugger.GetAttachStateFlags();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            int resultLocal;
            int hrLocal = _legacy.GetAttachStateFlags(&resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == resultLocal);
        }
#endif

        return hr;
    }

    public int GetMetaDataFileInfoFromPEFile(ulong vmPEAssembly, uint* dwTimeStamp, uint* dwImageSize, nint pStrFilename, Interop.BOOL* pResult)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetMetaDataFileInfoFromPEFile(vmPEAssembly, dwTimeStamp, dwImageSize, pStrFilename, pResult) : HResults.E_NOTIMPL;

    public int IsThreadSuspendedOrHijacked(ulong vmThread, Interop.BOOL* pResult)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.IsThreadSuspendedOrHijacked(vmThread, pResult) : HResults.E_NOTIMPL;

    public int CreateHeapWalk(nuint* pHandle)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.CreateHeapWalk(pHandle) : HResults.E_NOTIMPL;

    public int DeleteHeapWalk(nuint handle)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.DeleteHeapWalk(handle) : HResults.E_NOTIMPL;

    public int WalkHeap(nuint handle, uint count, COR_HEAPOBJECT* objects, uint* fetched)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.WalkHeap(handle, count, objects, fetched) : HResults.E_NOTIMPL;

#if DEBUG
    [ThreadStatic]
    private static List<(ulong Start, ulong End, int Generation, uint Heap)>? _debugEnumerateHeapSegments;

    private static List<(ulong Start, ulong End, int Generation, uint Heap)> DebugEnumerateHeapSegments
        => _debugEnumerateHeapSegments ??= new();

    [UnmanagedCallersOnly]
    private static void EnumerateHeapSegmentsDebugCallback(ulong start, ulong end, int generation, uint heap, nint _)
    {
        DebugEnumerateHeapSegments.Add((start, end, generation, heap));
    }
#endif

    public int EnumerateHeapSegments(delegate* unmanaged<ulong, ulong, int, uint, nint, void> fpCallback, nint pUserData)
    {
        int hr = HResults.S_OK;
        List<(ulong Start, ulong End, int Generation, uint Heap)> segments = new();
        try
        {
            if (fpCallback is null)
                throw new ArgumentNullException(nameof(fpCallback));

            IGC gc = _target.Contracts.GC;
            string[] gcIdentifiers = gc.GetGCIdentifiers();
            bool regions = gcIdentifiers.Contains(GCIdentifiers.Regions);
            bool isWorkstation = gcIdentifiers.Contains(GCIdentifiers.Workstation);

            uint heapIndex = 0;
            foreach (GCHeapData heapData in EnumerateHeaps(gc, isWorkstation))
            {
                TargetPointer gen0AllocStart = heapData.GenerationTable[0].AllocationStart;
                TargetPointer gen1AllocStart = heapData.GenerationTable[1].AllocationStart;

                // In segments mode, Gen0 lives outside the segment list - synthesize it as a
                // heap-level entry bracketed by [gen0.AllocationStart, alloc_allocated).
                if (!regions)
                    segments.Add((gen0AllocStart.Value, heapData.AllocAllocated.Value, (int)CorDebugGenerationTypes.CorDebug_Gen0, heapIndex));

                foreach (GCHeapSegmentInfo raw in gc.EnumerateHeapSegments(heapData))
                {
                    if (raw.Generation != GCSegmentClassification.Ephemeral)
                    {
                        segments.Add((raw.Start.Value, raw.End.Value, (int)ToCorDebugGenerationType(raw.Generation), heapIndex));
                    }
                    else
                    {
                        // Segments mode only: split the ephemeral marker into the Gen1 piece
                        // ([gen1.AllocationStart, gen0.AllocationStart)) plus an optional Gen2
                        // prefix ([raw.Start, gen1.AllocationStart)).
                        segments.Add((gen1AllocStart.Value, gen0AllocStart.Value, (int)CorDebugGenerationTypes.CorDebug_Gen1, heapIndex));
                        if (raw.Start != gen1AllocStart)
                            segments.Add((raw.Start.Value, gen1AllocStart.Value, (int)CorDebugGenerationTypes.CorDebug_Gen2, heapIndex));
                    }
                }

                heapIndex++;
            }

            foreach ((ulong start, ulong end, int generation, uint heap) in segments)
                fpCallback(start, end, generation, heap, pUserData);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            DebugEnumerateHeapSegments.Clear();
            delegate* unmanaged<ulong, ulong, int, uint, nint, void> debugCallbackPtr = &EnumerateHeapSegmentsDebugCallback;
            int hrLocal = _legacy.EnumerateHeapSegments(debugCallbackPtr, 0);
            Debug.ValidateHResult(hr, hrLocal);

            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
            {
                List<(ulong Start, ulong End, int Generation, uint Heap)> legacySegments = DebugEnumerateHeapSegments;
                if (!segments.SequenceEqual(legacySegments))
                {
                    Debug.Assert(segments.Count == legacySegments.Count,
                        $"cDAC: {segments.Count} segments, DAC: {legacySegments.Count} segments");

                    int compareCount = Math.Min(segments.Count, legacySegments.Count);
                    for (int i = 0; i < compareCount; i++)
                    {
                        Debug.Assert(segments[i] == legacySegments[i],
                            $"Segment {i} mismatch - cDAC: (0x{segments[i].Start:x}, 0x{segments[i].End:x}, gen={segments[i].Generation}, heap={segments[i].Heap}), DAC: (0x{legacySegments[i].Start:x}, 0x{legacySegments[i].End:x}, gen={legacySegments[i].Generation}, heap={legacySegments[i].Heap})");
                    }
                }
            }
            DebugEnumerateHeapSegments.Clear();
        }
#endif
        return hr;
    }

    private static IEnumerable<GCHeapData> EnumerateHeaps(IGC gc, bool isWorkstation)
    {
        if (isWorkstation)
        {
            yield return gc.GetHeapData();
        }
        else
        {
            foreach (TargetPointer heapAddress in gc.GetGCHeaps())
                yield return gc.GetHeapData(heapAddress);
        }
    }

    private static CorDebugGenerationTypes ToCorDebugGenerationType(GCSegmentClassification generation) => generation switch
    {
        GCSegmentClassification.Gen0 => CorDebugGenerationTypes.CorDebug_Gen0,
        GCSegmentClassification.Gen1 => CorDebugGenerationTypes.CorDebug_Gen1,
        GCSegmentClassification.Gen2 => CorDebugGenerationTypes.CorDebug_Gen2,
        GCSegmentClassification.LOH => CorDebugGenerationTypes.CorDebug_LOH,
        GCSegmentClassification.POH => CorDebugGenerationTypes.CorDebug_POH,
        GCSegmentClassification.NonGC => CorDebugGenerationTypes.CorDebug_NonGC,
        // Ephemeral is an internal marker that must be split by the caller; it never appears in
        // emitted output.
        _ => throw new ArgumentOutOfRangeException(nameof(generation), generation, null),
    };

    public int IsValidObject(ulong obj, Interop.BOOL* pResult)
    {
        int hr = HResults.S_OK;
        Interop.BOOL isValid = Interop.BOOL.FALSE;

        if (obj != 0 && obj != ulong.MaxValue)
        {
            try
            {
                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                TargetPointer mt = _target.Contracts.Object.GetMethodTableAddress(new TargetPointer(obj));
                TypeHandle th = rts.GetTypeHandle(mt);
                TargetPointer canonMT = rts.GetCanonicalMethodTable(th);

                if (mt == canonMT)
                {
                    isValid = Interop.BOOL.TRUE;
                }
                else if (!rts.IsCanonicalMethodTable(th) || rts.IsContinuationWithoutMetadata(th))
                {
                    TargetPointer cls = rts.GetClassPointer(th);
                    TypeHandle canonTh = rts.GetTypeHandle(canonMT);
                    TargetPointer canonCls = rts.GetClassPointer(canonTh);
                    if (canonCls == cls)
                        isValid = Interop.BOOL.TRUE;
                }
            }
            catch (System.Exception)
            {
                isValid = Interop.BOOL.FALSE;
            }
        }
        *pResult = isValid;

#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.IsValidObject(obj, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
            }
        }
#endif

        return hr;
    }

    public int CreateRefWalk(nuint* pHandle, Interop.BOOL walkStacks, Interop.BOOL walkFQ, uint handleWalkMask)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.CreateRefWalk(pHandle, walkStacks, walkFQ, handleWalkMask) : HResults.E_NOTIMPL;

    public int DeleteRefWalk(nuint handle)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.DeleteRefWalk(handle) : HResults.E_NOTIMPL;

    public int WalkRefs(nuint handle, uint count, nint refs, uint* pFetched)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.WalkRefs(handle, count, refs, pFetched) : HResults.E_NOTIMPL;

    public int GetTypeID(ulong obj, COR_TYPEID* pType)
    {
        *pType = default;
        int hr = HResults.S_OK;
        try
        {
            TargetPointer mt = _target.Contracts.Object.GetMethodTableAddress(new TargetPointer(obj));
            pType->token1 = mt.Value;
            pType->token2 = 0;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            COR_TYPEID resultLocal;
            int hrLocal = _legacy.GetTypeID(obj, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pType->token1 == resultLocal.token1);
                Debug.Assert(pType->token2 == resultLocal.token2);
            }
        }
#endif

        return hr;
    }

    public int GetTypeIDForType(ulong vmTypeHandle, COR_TYPEID* pId)
    {
        *pId = default;
        int hr = HResults.S_OK;
        try
        {
            pId->token1 = vmTypeHandle;
            pId->token2 = 0;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            COR_TYPEID resultLocal;
            int hrLocal = _legacy.GetTypeIDForType(vmTypeHandle, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pId->token1 == resultLocal.token1);
                Debug.Assert(pId->token2 == resultLocal.token2);
            }
        }
#endif

        return hr;
    }

    public int GetObjectFields(ulong id, uint celt, COR_FIELD* layout, uint* pceltFetched)
    {
        int hr = HResults.S_OK;
        uint cFields = 0;
        try
        {
            if (pceltFetched == null)
                throw new NullReferenceException(nameof(pceltFetched));

            if (id == 0)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle typeHandle = rts.GetTypeHandle(new TargetPointer(id));

            if (rts.IsTypeDesc(typeHandle))
                throw new ArgumentException("TypeDescs are not supported", nameof(id));

            typeHandle = UpCastTypeIfNeeded(rts, typeHandle);

            // Number of introduced instance fields = NumInstanceFields - parent's NumInstanceFields.
            cFields = rts.GetNumInstanceFields(typeHandle);
            TargetPointer parentMT = rts.GetParentMethodTable(typeHandle);
            if (parentMT != TargetPointer.Null)
            {
                TypeHandle parentHandle = rts.GetTypeHandle(parentMT);
                cFields -= rts.GetNumInstanceFields(parentHandle);
            }

            // Caller may pass a null layout buffer to query the number of fields.
            if (layout == null)
            {
                *pceltFetched = cFields;
                hr = HResults.S_FALSE;
            }
            else
            {
                if (celt < cFields)
                {
                    cFields = celt;
                    hr = HResults.S_FALSE;
                }

                // Match native DAC: pceltFetched is set to celt (the input capacity), not the
                // count actually written. Preserve this behavior for compatibility w/ICorDebug.
                *pceltFetched = celt;

                bool isReferenceType = rts.IsObjRef(typeHandle);
                uint firstFieldOffset = isReferenceType ? _target.GetTypeInfo(DataType.Object).Size!.Value : 0;

                TargetPointer[] fieldDescList = rts.GetFieldDescList(typeHandle).Take((int)cFields).ToArray();

                IEcmaMetadata ecmaMetadataContract = _target.Contracts.EcmaMetadata;
                ISignature signature = _target.Contracts.Signature;

                for (uint i = 0; i < cFields; ++i)
                {
                    TargetPointer fieldDescPtr = fieldDescList[i];
                    COR_FIELD* corField = layout + i;

                    uint memberDef = rts.GetFieldDescMemberDef(fieldDescPtr);
                    corField->token = memberDef;

                    // Resolve metadata for this field's enclosing class (for offset lookup and
                    // signature decoding context).
                    TargetPointer enclosingMT = rts.GetMTOfEnclosingClass(fieldDescPtr);
                    TypeHandle enclosingTypeHandle = rts.GetTypeHandle(enclosingMT);
                    TargetPointer enclosingModulePtr = rts.GetModule(enclosingTypeHandle);
                    Contracts.ModuleHandle enclosingModuleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(enclosingModulePtr);
                    MetadataReader enclosingMdReader = ecmaMetadataContract.GetMetadata(enclosingModuleHandle)!;
                    FieldDefinitionHandle fieldDefHandle = (FieldDefinitionHandle)MetadataTokens.Handle((int)memberDef);
                    FieldDefinition fieldDef = enclosingMdReader.GetFieldDefinition(fieldDefHandle);

                    corField->offset = rts.GetFieldDescOffset(fieldDescPtr, fieldDef) + firstFieldOffset;

                    // Resolve the field's type. If we cannot decode the signature (e.g. corrupt
                    // metadata or a type that cannot be loaded), zero out the type id and
                    // fieldType, matching native DAC behavior when LookupFieldTypeHandle returns
                    // a null TypeHandle.
                    try
                    {
                        TypeHandle fieldTypeHandle = signature.DecodeFieldSignature(fieldDef.Signature, enclosingModuleHandle, enclosingTypeHandle);
                        if (fieldTypeHandle.IsNull)
                        {
                            corField->id = default;
                            corField->fieldType = 0;
                            continue;
                        }
                        CorElementType signatureType = rts.GetSignatureCorElementType(fieldTypeHandle);
                        if (signatureType == CorElementType.Byref)
                        {
                            corField->fieldType = (int)CorElementType.Byref;
                            // All ByRefs intentionally return IntPtr's MethodTable.
                            corField->id.token1 = rts.GetPrimitiveType(CorElementType.I).Address.Value;
                            corField->id.token2 = 0;
                        }
                        else
                        {
                            //   - Pointer/FnPtr typedescs report ELEMENT_TYPE_U's MethodTable.
                            TypeHandle mtHandle = (signatureType == CorElementType.Ptr || signatureType == CorElementType.FnPtr)
                                ? rts.GetPrimitiveType(CorElementType.U)
                                : fieldTypeHandle;

                            corField->fieldType = (int)rts.GetInternalCorElementType(mtHandle);
                            corField->id.token1 = mtHandle.Address.Value;
                            corField->id.token2 = 0;
                        }
                    }
                    catch (System.Exception)
                    {
                        // Field type could not be resolved - mirror native's null-TypeHandle path.
                        corField->id = default;
                        corField->fieldType = 0;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            uint fetchedLocal = 0;
            // Allocate at least one element so the `fixed` pointer is valid even when celt is 0.
            COR_FIELD[] localFields = new COR_FIELD[celt == 0 ? 1 : celt];
            fixed (COR_FIELD* localFieldsPtr = localFields)
            {
                int hrLocal = _legacy.GetObjectFields(id, celt, layout == null ? null : localFieldsPtr, &fetchedLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr >= HResults.S_OK && hrLocal >= HResults.S_OK)
                {
                    Debug.Assert(*pceltFetched == fetchedLocal, $"cDAC: {*pceltFetched}, DAC: {fetchedLocal}");
                    uint written = layout == null ? 0 : Math.Min(celt, cFields);
                    for (uint i = 0; i < written; ++i)
                    {
                        Debug.Assert(layout[i].token == localFieldsPtr[i].token, $"field[{i}].token cDAC: {layout[i].token:x}, DAC: {localFieldsPtr[i].token:x}");
                        Debug.Assert(layout[i].offset == localFieldsPtr[i].offset, $"field[{i}].offset cDAC: {layout[i].offset}, DAC: {localFieldsPtr[i].offset}");
                        Debug.Assert(layout[i].fieldType == localFieldsPtr[i].fieldType, $"field[{i}].fieldType cDAC: {layout[i].fieldType}, DAC: {localFieldsPtr[i].fieldType}");
                        Debug.Assert(layout[i].id.token1 == localFieldsPtr[i].id.token1, $"field[{i}].id.token1 cDAC: {layout[i].id.token1:x}, DAC: {localFieldsPtr[i].id.token1:x}");
                        Debug.Assert(layout[i].id.token2 == localFieldsPtr[i].id.token2, $"field[{i}].id.token2 cDAC: {layout[i].id.token2:x}, DAC: {localFieldsPtr[i].id.token2:x}");
                    }
                }
            }
        }
#endif

        return hr;
    }

    public int GetTypeLayout(ulong id, COR_TYPE_LAYOUT* pLayout)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pLayout is null)
                throw new NullReferenceException(nameof(pLayout));

            if (id == 0)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle typeHandle = rts.GetTypeHandle(new TargetPointer((ulong)id));

            TargetPointer parentMT = rts.GetParentMethodTable(typeHandle);
            pLayout->parentID.token1 = parentMT.Value;
            pLayout->parentID.token2 = 0;
            pLayout->objectSize = rts.GetBaseSize(typeHandle);
            ushort numInstanceFields = rts.GetNumInstanceFields(typeHandle);
            if (parentMT != TargetPointer.Null)
            {
                TypeHandle parentHandle = rts.GetTypeHandle(parentMT);
                numInstanceFields -= rts.GetNumInstanceFields(parentHandle);
            }
            pLayout->numFields = numInstanceFields;
            pLayout->boxOffset = rts.IsObjRef(typeHandle) ? 0u : (uint)_target.PointerSize;
            pLayout->type = (int)(rts.IsString(typeHandle) ? CorElementType.String : rts.GetInternalCorElementType(typeHandle));
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            COR_TYPE_LAYOUT resultLocal;
            int hrLocal = _legacy.GetTypeLayout(id, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pLayout->parentID.token1 == resultLocal.parentID.token1, $"cDAC: {pLayout->parentID.token1:x}, DAC: {resultLocal.parentID.token1:x}");
                Debug.Assert(pLayout->parentID.token2 == resultLocal.parentID.token2, $"cDAC: {pLayout->parentID.token2:x}, DAC: {resultLocal.parentID.token2:x}");
                Debug.Assert(pLayout->objectSize == resultLocal.objectSize, $"cDAC: {pLayout->objectSize}, DAC: {resultLocal.objectSize}");
                Debug.Assert(pLayout->numFields == resultLocal.numFields, $"cDAC: {pLayout->numFields}, DAC: {resultLocal.numFields}");
                Debug.Assert(pLayout->boxOffset == resultLocal.boxOffset, $"cDAC: {pLayout->boxOffset}, DAC: {resultLocal.boxOffset}");
                Debug.Assert(pLayout->type == resultLocal.type, $"cDAC: {pLayout->type}, DAC: {resultLocal.type}");
            }
        }
#endif

        return hr;
    }

    public int GetArrayLayout(ulong id, COR_ARRAY_LAYOUT* pLayout)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pLayout is null)
                throw new NullReferenceException(nameof(pLayout));

            if (id == 0)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle arrayOrStringTypeHandle = rts.GetTypeHandle(new TargetPointer(id));
            uint pointerSize = (uint)_target.PointerSize;

            if (rts.IsString(arrayOrStringTypeHandle))
            {
                TypeHandle charTypeHandle = rts.GetPrimitiveType(CorElementType.Char);
                pLayout->componentID.token1 = charTypeHandle.Address.Value;
                pLayout->componentID.token2 = 0;
                pLayout->componentType = CorElementType.Char;
                pLayout->firstElementOffset = pointerSize + 4;
                pLayout->elementSize = sizeof(char);
                pLayout->countOffset = pointerSize;
                pLayout->rankSize = 4;
                pLayout->numRanks = 1;
                pLayout->rankOffset = pointerSize;
            }
            else
            {
                if (!rts.IsArray(arrayOrStringTypeHandle, out uint rank))
                    throw Marshal.GetExceptionForHR(HResults.E_INVALIDARG)!;

                TypeHandle componentTypeHandle = rts.GetTypeParam(arrayOrStringTypeHandle);
                CorElementType componentType = rts.IsString(componentTypeHandle) ? CorElementType.String : rts.GetInternalCorElementType(componentTypeHandle);
                pLayout->componentID.token1 = componentTypeHandle.Address.Value;
                pLayout->componentID.token2 = 0;
                pLayout->componentType = componentType;
                Target.TypeInfo objectHeaderTypeInfo = _target.GetTypeInfo(DataType.ObjectHeader);
                uint objectHeaderSize = (uint)objectHeaderTypeInfo.Size!.Value;
                pLayout->firstElementOffset = rts.GetBaseSize(arrayOrStringTypeHandle) - objectHeaderSize;
                pLayout->elementSize = rts.GetComponentSize(arrayOrStringTypeHandle);
                pLayout->countOffset = pointerSize;
                pLayout->rankSize = 4;
                pLayout->numRanks = rank;
                pLayout->rankOffset = rank > 1 ? pointerSize * 2 : pointerSize;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            COR_ARRAY_LAYOUT resultLocal;
            int hrLocal = _legacy.GetArrayLayout(id, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pLayout->componentID.token1 == resultLocal.componentID.token1, $"cDAC: {pLayout->componentID.token1:x}, DAC: {resultLocal.componentID.token1:x}");
                Debug.Assert(pLayout->componentID.token2 == resultLocal.componentID.token2, $"cDAC: {pLayout->componentID.token2:x}, DAC: {resultLocal.componentID.token2:x}");
                Debug.Assert(pLayout->componentType == resultLocal.componentType, $"cDAC: {pLayout->componentType}, DAC: {resultLocal.componentType}");
                Debug.Assert(pLayout->firstElementOffset == resultLocal.firstElementOffset, $"cDAC: {pLayout->firstElementOffset}, DAC: {resultLocal.firstElementOffset}");
                Debug.Assert(pLayout->elementSize == resultLocal.elementSize, $"cDAC: {pLayout->elementSize}, DAC: {resultLocal.elementSize}");
                Debug.Assert(pLayout->countOffset == resultLocal.countOffset, $"cDAC: {pLayout->countOffset}, DAC: {resultLocal.countOffset}");
                Debug.Assert(pLayout->rankSize == resultLocal.rankSize, $"cDAC: {pLayout->rankSize}, DAC: {resultLocal.rankSize}");
                Debug.Assert(pLayout->numRanks == resultLocal.numRanks, $"cDAC: {pLayout->numRanks}, DAC: {resultLocal.numRanks}");
                Debug.Assert(pLayout->rankOffset == resultLocal.rankOffset, $"cDAC: {pLayout->rankOffset}, DAC: {resultLocal.rankOffset}");
            }
        }
#endif

        return hr;
    }

    public int GetGCHeapInformation(COR_HEAPINFO* pHeapInfo)
    {
        *pHeapInfo = default;
        int hr = HResults.S_OK;
        try
        {
            Contracts.IGC gc = _target.Contracts.GC;
            pHeapInfo->areGCStructuresValid = gc.GetGCStructuresValid() ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
            pHeapInfo->numHeaps = gc.GetGCHeapCount();
            pHeapInfo->pointerSize = (uint)_target.PointerSize;

            string[] identifiers = gc.GetGCIdentifiers();
            bool isServer = identifiers.Contains(GCIdentifiers.Server);
            pHeapInfo->gcType = isServer ? 1 : 0; // CorDebugServerGC = 1, CorDebugWorkstationGC = 0
            pHeapInfo->concurrent = identifiers.Contains(GCIdentifiers.Background) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            COR_HEAPINFO resultLocal;
            int hrLocal = _legacy.GetGCHeapInformation(&resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pHeapInfo->areGCStructuresValid == resultLocal.areGCStructuresValid);
                Debug.Assert(pHeapInfo->numHeaps == resultLocal.numHeaps);
                Debug.Assert(pHeapInfo->pointerSize == resultLocal.pointerSize);
                Debug.Assert(pHeapInfo->gcType == resultLocal.gcType);
                Debug.Assert(pHeapInfo->concurrent == resultLocal.concurrent);
            }
        }
#endif

        return hr;
    }

    public int GetPEFileMDInternalRW(ulong vmPEAssembly, ulong* pAddrMDInternalRW)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetPEFileMDInternalRW(vmPEAssembly, pAddrMDInternalRW) : HResults.E_NOTIMPL;

    public int AreOptimizationsDisabled(ulong vmModule, uint methodTk, Interop.BOOL* pOptimizationsDisabled)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.AreOptimizationsDisabled(vmModule, methodTk, pOptimizationsDisabled) : HResults.E_NOTIMPL;

    public int GetDefinesBitField(uint* pDefines)
    {
        *pDefines = 0;
        int hr = HResults.S_OK;
        try
        {
            if (!_target.Contracts.Debugger.TryGetDebuggerData(out Contracts.DebuggerData data))
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_NOTREADY)!;
            *pDefines = data.DefinesBitField;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            uint resultLocal;
            int hrLocal = _legacy.GetDefinesBitField(&resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pDefines == resultLocal);
        }
#endif

        return hr;
    }

    public int GetMDStructuresVersion(uint* pMDStructuresVersion)
    {
        *pMDStructuresVersion = 0;
        int hr = HResults.S_OK;
        try
        {
            if (!_target.Contracts.Debugger.TryGetDebuggerData(out Contracts.DebuggerData data))
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_NOTREADY)!;
            *pMDStructuresVersion = data.MDStructuresVersion;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            uint resultLocal;
            int hrLocal = _legacy.GetMDStructuresVersion(&resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pMDStructuresVersion == resultLocal);
        }
#endif

        return hr;
    }

    public int GetActiveRejitILCodeVersionNode(ulong vmModule, uint methodTk, ulong* pVmILCodeVersionNode)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pVmILCodeVersionNode is null)
                throw new ArgumentException("Output pointer cannot be null.", nameof(pVmILCodeVersionNode));

            *pVmILCodeVersionNode = 0;

            if (!_target.Contracts.TryGetContract<IReJIT>(out IReJIT rejit))
                return hr;

            ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle module = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));
            ModuleLookupTables lookupTables = loader.GetLookupTables(module);
            TargetPointer methodDesc = TargetPointer.Null;

            if ((EcmaMetadataUtils.TokenType)(methodTk & EcmaMetadataUtils.TokenTypeMask) != EcmaMetadataUtils.TokenType.mdtMethodDef)
                throw new ArgumentException("methodTk must be a MethodDef token.", nameof(methodTk));
            methodDesc = loader.GetModuleLookupMapElement(lookupTables.MethodDefToDesc, methodTk, out _);

            if (methodDesc != TargetPointer.Null)
            {
                ICodeVersions codeVersions = _target.Contracts.CodeVersions;
                ILCodeVersionHandle ilCodeVersion = codeVersions.GetActiveILCodeVersion(methodDesc);
                if (ilCodeVersion.IsValid
                    && ilCodeVersion.IsExplicit
                    && rejit.GetRejitState(ilCodeVersion) == RejitState.Active)
                {
                    *pVmILCodeVersionNode = ilCodeVersion.ILCodeVersionNode.Value;
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            ulong resultLocal;
            int hrLocal = _legacy.GetActiveRejitILCodeVersionNode(vmModule, methodTk, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pVmILCodeVersionNode == resultLocal, $"cDAC: {*pVmILCodeVersionNode:x}, DAC: {resultLocal:x}");
        }
#endif

        return hr;
    }

    public int GetNativeCodeVersionNode(ulong vmMethod, ulong codeStartAddress, ulong* pVmNativeCodeVersionNode)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pVmNativeCodeVersionNode is null)
                throw new ArgumentException("Output pointer cannot be null.", nameof(pVmNativeCodeVersionNode));

            *pVmNativeCodeVersionNode = 0;

            TargetCodePointer codeAddress = new TargetCodePointer(codeStartAddress);
            ICodeVersions codeVersions = _target.Contracts.CodeVersions;

            NativeCodeVersionHandle nativeCodeVersion = codeVersions.GetNativeCodeVersionForIP(codeAddress);
            if (nativeCodeVersion.Valid && nativeCodeVersion.IsExplicit)
            {
                *pVmNativeCodeVersionNode = nativeCodeVersion.CodeVersionNodeAddress.Value;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            ulong resultLocal;
            int hrLocal = _legacy.GetNativeCodeVersionNode(vmMethod, codeStartAddress, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pVmNativeCodeVersionNode == resultLocal, $"cDAC: {*pVmNativeCodeVersionNode:x}, DAC: {resultLocal:x}");
        }
#endif

        return hr;
    }

    public int GetILCodeVersionNode(ulong vmNativeCodeVersionNode, ulong* pVmILCodeVersionNode)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pVmILCodeVersionNode is null)
                throw new ArgumentException("Output pointer cannot be null.", nameof(pVmILCodeVersionNode));

            *pVmILCodeVersionNode = 0;

            ICodeVersions codeVersions = _target.Contracts.CodeVersions;
            NativeCodeVersionHandle nativeCodeVersion = NativeCodeVersionHandle.CreateExplicit(new TargetPointer(vmNativeCodeVersionNode));
            ILCodeVersionHandle ilCodeVersion = codeVersions.GetILCodeVersion(nativeCodeVersion);
            if (ilCodeVersion.IsValid && ilCodeVersion.IsExplicit)
            {
                *pVmILCodeVersionNode = ilCodeVersion.ILCodeVersionNode.Value;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            ulong resultLocal;
            int hrLocal = _legacy.GetILCodeVersionNode(vmNativeCodeVersionNode, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pVmILCodeVersionNode == resultLocal, $"cDAC: {*pVmILCodeVersionNode:x}, DAC: {resultLocal:x}");
        }
#endif

        return hr;
    }

    public int GetILCodeVersionNodeData(ulong ilCodeVersionNode, DacDbiSharedReJitInfo* pData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetILCodeVersionNodeData(ilCodeVersionNode, pData) : HResults.E_NOTIMPL;

    public int EnableGCNotificationEvents(Interop.BOOL fEnable)
    {
        int hr = HResults.S_OK;
        try
        {
            _target.Contracts.Debugger.EnableGCNotificationEvents(fEnable != Interop.BOOL.FALSE);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.EnableGCNotificationEvents(fEnable);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    public int IsDelegate(ulong vmObject, Interop.BOOL* pResult)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.IsDelegate(vmObject, pResult) : HResults.E_NOTIMPL;

    public int GetDelegateType(ulong delegateObject, int* delegateType)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetDelegateType(delegateObject, delegateType) : HResults.E_NOTIMPL;

    public int GetDelegateFunctionData(int delegateType, ulong delegateObject, ulong* ppFunctionAssembly, uint* pMethodDef)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetDelegateFunctionData(delegateType, delegateObject, ppFunctionAssembly, pMethodDef) : HResults.E_NOTIMPL;

    public int GetDelegateTargetObject(int delegateType, ulong delegateObject, ulong* ppTargetObj, ulong* ppTargetAppDomain)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetDelegateTargetObject(delegateType, delegateObject, ppTargetObj, ppTargetAppDomain) : HResults.E_NOTIMPL;

    public int IsModuleMapped(ulong pModule, Interop.BOOL* isModuleMapped)
    {
        int hr = HResults.S_FALSE;
        try
        {
            if (pModule == 0)
                throw new ArgumentException("Module pointer must not be zero.", nameof(pModule));

            if (isModuleMapped == null)
                throw new ArgumentNullException(nameof(isModuleMapped));

            *isModuleMapped = Interop.BOOL.FALSE;
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromModulePtr(new TargetPointer(pModule));
            if (loader.TryGetLoadedImageContents(handle, out _, out _, out _))
            {
                *isModuleMapped = loader.IsModuleMapped(handle) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
                hr = HResults.S_OK;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL isModuleMappedLocal;
            int hrLocal = _legacy.IsModuleMapped(pModule, &isModuleMappedLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*isModuleMapped == isModuleMappedLocal, $"cDAC: {*isModuleMapped}, DAC: {isModuleMappedLocal}");
        }
#endif
        return hr;
    }

    public int MetadataUpdatesApplied(Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            *pResult = _target.Contracts.Debugger.MetadataUpdatesApplied() ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.MetadataUpdatesApplied(&resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal);
        }
#endif

        return hr;
    }

    public int GetAssemblyFromModule(ulong vmModule, ulong* pVmAssembly)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetAssemblyFromModule(vmModule, pVmAssembly) : HResults.E_NOTIMPL;

    public int ParseContinuation(ulong continuationAddress, ulong* pDiagnosticIP, ulong* pNextContinuation, uint* pState)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.ParseContinuation(continuationAddress, pDiagnosticIP, pNextContinuation, pState) : HResults.E_NOTIMPL;

    public int GetAsyncLocals(ulong vmMethod, ulong codeAddr, uint state, nint pAsyncLocals)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetAsyncLocals(vmMethod, codeAddr, state, pAsyncLocals) : HResults.E_NOTIMPL;

    public int GetGenericArgTokenIndex(ulong vmMethod, uint* pIndex)
    {
        int hr = HResults.S_OK;
        try
        {
            if (vmMethod == 0)
                throw new ArgumentException("vmMethod must not be zero.", nameof(vmMethod));
            if (pIndex is null)
                throw new ArgumentException("pIndex must not be null.", nameof(pIndex));
            Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            Contracts.MethodDescHandle md = rts.GetMethodDescHandle(new TargetPointer(vmMethod));

            switch (rts.GetGenericContextLoc(md))
            {
                case GenericContextLoc.None:
                    hr = HResults.S_FALSE;
                    break;
                case GenericContextLoc.InstArg:
                    *pIndex = unchecked((uint)IlNum.TYPECTXT_ILNUM);
                    break;
                case GenericContextLoc.ThisPtr:
                    *pIndex = 0u;
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected generic context location: {rts.GetGenericContextLoc(md)}");
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            uint indexLocal;
            int hrLocal = _legacy.GetGenericArgTokenIndex(vmMethod, &indexLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pIndex == indexLocal, $"cDAC: {*pIndex}, DAC: {indexLocal}");
        }
#endif
        return hr;
    }

    // Shared core implementation for TypeHandleToExpandedTypeInfo and GetObjectExpandedTypeInfo.
    private void TypeHandleToExpandedTypeInfoImpl(IRuntimeTypeSystem rts, AreValueTypesBoxed boxed, TypeHandle typeHandle, DebuggerIPCE_ExpandedTypeData* pTypeInfo)
    {
        *pTypeInfo = default;
        CorElementType elementType = GetElementType(rts, typeHandle);
        WriteLittleEndian(ref pTypeInfo->elementType, (int)elementType);

        switch (elementType)
        {
            case CorElementType.Array:
            case CorElementType.SzArray:
                FillArrayTypeInfo(rts, typeHandle, pTypeInfo);
                break;

            case CorElementType.Ptr:
            case CorElementType.Byref:
                FillPtrTypeInfo(rts, boxed, typeHandle, pTypeInfo);
                break;

            case CorElementType.ValueType:
                if (boxed == AreValueTypesBoxed.OnlyPrimitivesUnboxed || boxed == AreValueTypesBoxed.AllBoxed)
                {
                    WriteLittleEndian(ref pTypeInfo->elementType, (int)CorElementType.Class);
                }
                FillClassTypeInfo(rts, typeHandle, pTypeInfo);
                break;

            case CorElementType.Class:
                FillClassTypeInfo(rts, typeHandle, pTypeInfo);
                break;

            case CorElementType.FnPtr:
                FillFnPtrTypeInfo(rts, boxed, typeHandle, pTypeInfo);
                break;

            default:
                if (boxed == AreValueTypesBoxed.AllBoxed)
                {
                    WriteLittleEndian(ref pTypeInfo->elementType, (int)CorElementType.Class);
                    FillClassTypeInfo(rts, typeHandle, pTypeInfo);
                }
                break;
        }
    }

    // Determines the CorElementType for a type handle, mapping System.Object and System.String
    // to their specific element types (the runtime's GetSignatureCorElementType returns E_T_CLASS
    // for both Object and String).
    private static CorElementType GetElementType(IRuntimeTypeSystem rts, TypeHandle typeHandle)
    {
        if (typeHandle.IsNull)
            return CorElementType.Void;

        if (rts.IsString(typeHandle))
            return CorElementType.String;

        if (rts.IsObject(typeHandle))
            return CorElementType.Object;

        return rts.GetSignatureCorElementType(typeHandle);
    }

    // Mirrors native TypeHandle::UpCastTypeIfNeeded — for continuation types, returns the
    // parent (continuation base) type handle instead.
    private static TypeHandle UpCastTypeIfNeeded(IRuntimeTypeSystem rts, TypeHandle typeHandle)
    {
        if (rts.IsContinuationWithoutMetadata(typeHandle))
        {
            TargetPointer parentMT = rts.GetParentMethodTable(typeHandle);
            if (parentMT != TargetPointer.Null)
                return rts.GetTypeHandle(parentMT);
        }
        return typeHandle;
    }

    // Fills ArrayTypeData for E_T_ARRAY and E_T_SZARRAY.
    // Mirrors native DacDbiInterfaceImpl::GetArrayTypeInfo.
    private void FillArrayTypeInfo(IRuntimeTypeSystem rts, TypeHandle typeHandle, DebuggerIPCE_ExpandedTypeData* pTypeInfo)
    {
        Debug.Assert(rts.IsArray(typeHandle, out _));
        rts.IsArray(typeHandle, out uint rank);
        WriteLittleEndian(ref pTypeInfo->ArrayTypeData_arrayRank, rank);
        TypeHandle elemTypeHandle = rts.GetTypeParam(typeHandle);
        FillBasicTypeInfo(rts, elemTypeHandle, out pTypeInfo->ArrayTypeData_arrayTypeArg);
    }

    // Fills UnaryTypeData for E_T_PTR and E_T_BYREF (or ClassTypeData if AllBoxed).
    private void FillPtrTypeInfo(IRuntimeTypeSystem rts, AreValueTypesBoxed boxed, TypeHandle typeHandle, DebuggerIPCE_ExpandedTypeData* pTypeInfo)
    {
        if (boxed == AreValueTypesBoxed.AllBoxed)
        {
            FillClassTypeInfo(rts, typeHandle, pTypeInfo);
        }
        else
        {
            TypeHandle paramTypeHandle = rts.GetTypeParam(typeHandle);
            FillBasicTypeInfo(rts, paramTypeHandle, out pTypeInfo->UnaryTypeData_unaryTypeArg);
        }
    }

    // Fills ClassTypeData for E_T_CLASS and E_T_VALUETYPE.
    private void FillClassTypeInfo(IRuntimeTypeSystem rts, TypeHandle typeHandle, DebuggerIPCE_ExpandedTypeData* pTypeInfo)
    {
        typeHandle = UpCastTypeIfNeeded(rts, typeHandle);

        TargetPointer modulePtr = rts.GetModule(typeHandle);
        Contracts.ILoader loader = _target.Contracts.Loader;
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);

        ReadOnlySpan<TypeHandle> instantiation = rts.GetInstantiation(typeHandle);
        if (instantiation.Length > 0)
        {
            // Generic instantiation — set the type handle so the debugger can fetch type arguments
            WriteLittleEndian(ref pTypeInfo->ClassTypeData_typeHandle, typeHandle.Address.Value);
        }
        // else: non-generic — typeHandle stays null

        WriteLittleEndian(ref pTypeInfo->ClassTypeData_metadataToken, rts.GetTypeDefToken(typeHandle));

        Debug.Assert(modulePtr != TargetPointer.Null);
        WriteLittleEndian(ref pTypeInfo->ClassTypeData_vmAssembly, loader.GetAssembly(moduleHandle).Value);
    }

    // Fills NaryTypeData for E_T_FNPTR (or ClassTypeData if AllBoxed).
    private void FillFnPtrTypeInfo(IRuntimeTypeSystem rts, AreValueTypesBoxed boxed, TypeHandle typeHandle, DebuggerIPCE_ExpandedTypeData* pTypeInfo)
    {
        if (boxed == AreValueTypesBoxed.AllBoxed)
        {
            FillClassTypeInfo(rts, typeHandle, pTypeInfo);
        }
        else
        {
            WriteLittleEndian(ref pTypeInfo->NaryTypeData_typeHandle, typeHandle.Address.Value);
        }
    }

    // Fills a DebuggerIPCE_BasicTypeData for a type handle — used for array element types
    // and ptr/byref referent types.
    private void FillBasicTypeInfo(IRuntimeTypeSystem rts, TypeHandle typeHandle, out DebuggerIPCE_BasicTypeData typeInfo)
    {
        typeInfo = default;
        CorElementType elementType = GetElementType(rts, typeHandle);
        WriteLittleEndian(ref typeInfo.elementType, (int)elementType);

        switch (elementType)
        {
            case CorElementType.Array:
            case CorElementType.SzArray:
            case CorElementType.FnPtr:
            case CorElementType.Ptr:
            case CorElementType.Byref:
                WriteLittleEndian(ref typeInfo.vmTypeHandle, typeHandle.Address.Value);
                // metadataToken and vmAssembly stay zero
                break;

            case CorElementType.Class:
            case CorElementType.ValueType:
            {
                typeHandle = UpCastTypeIfNeeded(rts, typeHandle);

                TargetPointer modulePtr = rts.GetModule(typeHandle);
                Contracts.ILoader loader = _target.Contracts.Loader;
                Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);

                ReadOnlySpan<TypeHandle> instantiation = rts.GetInstantiation(typeHandle);
                if (instantiation.Length > 0)
                {
                    WriteLittleEndian(ref typeInfo.vmTypeHandle, typeHandle.Address.Value);
                }
                // else: vmTypeHandle stays null

                WriteLittleEndian(ref typeInfo.metadataToken, rts.GetTypeDefToken(typeHandle));
                Debug.Assert(modulePtr != TargetPointer.Null);
                WriteLittleEndian(ref typeInfo.vmAssembly, loader.GetAssembly(moduleHandle).Value);
                break;
            }

            default:
                // All fields zero
                break;
        }
    }

    // Little-endian read/write helpers for IPCE structs.
    // Native IPCE structs use Portable<T>, which stores data in little-endian format.
    // These helpers ensure managed reads/writes match that convention.
    private static void WriteLittleEndian<T>(ref T dest, T value) where T : unmanaged, IBinaryInteger<T>
    {
        if (BitConverter.IsLittleEndian)
        {
            dest = value;
        }
        else
        {
            Span<byte> destBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref dest, 1));
            value.WriteLittleEndian(destBytes);
        }
    }

    private static T ReadLittleEndian<T>(T value) where T : unmanaged, IBinaryInteger<T>
    {
        if (BitConverter.IsLittleEndian)
            return value;
        MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)).Reverse();
        return value;
    }

}
