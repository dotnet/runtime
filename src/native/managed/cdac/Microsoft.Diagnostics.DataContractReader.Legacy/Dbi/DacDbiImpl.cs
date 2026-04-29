// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
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

    public int GetAppDomainObject(ulong vmAppDomain, ulong* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetAppDomainObject(vmAppDomain, pRetVal) : HResults.E_NOTIMPL;

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
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetModuleSimpleName(vmModule, pStrFilename) : HResults.E_NOTIMPL;

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

    public int GetSymbolsBuffer(ulong vmModule, DacDbiTargetBuffer* pTargetBuffer, int* pSymbolFormat)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetSymbolsBuffer(vmModule, pTargetBuffer, pSymbolFormat) : HResults.E_NOTIMPL;

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

    public int EnumerateAssembliesInAppDomain(ulong vmAppDomain, nint fpCallback, nint pUserData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.EnumerateAssembliesInAppDomain(vmAppDomain, fpCallback, pUserData) : HResults.E_NOTIMPL;

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

    public int GetThreadHandle(ulong vmThread, nint pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetThreadHandle(vmThread, pRetVal) : HResults.E_NOTIMPL;

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
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetThreadAllocInfo(vmThread, pThreadAllocInfo) : HResults.E_NOTIMPL;

    public int SetDebugState(ulong vmThread, int debugState)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.SetDebugState(vmThread, debugState) : HResults.E_NOTIMPL;

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

    public int GetPartialUserState(ulong vmThread, int* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetPartialUserState(vmThread, pRetVal) : HResults.E_NOTIMPL;

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
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.ResolveAssembly(vmScope, tkAssemblyRef, pRetVal) : HResults.E_NOTIMPL;

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
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.CheckContext(vmThread, pContext) : HResults.E_NOTIMPL;

    public int GetStackWalkCurrentFrameInfo(nuint pSFIHandle, nint pFrameData, int* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetStackWalkCurrentFrameInfo(pSFIHandle, pFrameData, pRetVal) : HResults.E_NOTIMPL;

    public int GetCountOfInternalFrames(ulong vmThread, uint* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetCountOfInternalFrames(vmThread, pRetVal) : HResults.E_NOTIMPL;

    public int EnumerateInternalFrames(ulong vmThread, nint fpCallback, nint pUserData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.EnumerateInternalFrames(vmThread, fpCallback, pUserData) : HResults.E_NOTIMPL;

    public int GetStackParameterSize(ulong controlPC, uint* pRetVal)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetStackParameterSize(controlPC, pRetVal) : HResults.E_NOTIMPL;

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
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetVarArgSig(VASigCookieAddr, pArgBase, pRetVal) : HResults.E_NOTIMPL;

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
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.ResolveExactGenericArgsToken(dwExactGenericArgsTokenIndex, rawToken, pRetVal) : HResults.E_NOTIMPL;

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

    public int TypeHandleToExpandedTypeInfo(int boxed, ulong vmTypeHandle, nint pData)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.TypeHandleToExpandedTypeInfo(boxed, vmTypeHandle, pData) : HResults.E_NOTIMPL;

    public int GetObjectExpandedTypeInfo(int boxed, ulong addr, nint pTypeInfo)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetObjectExpandedTypeInfo(boxed, addr, pTypeInfo) : HResults.E_NOTIMPL;

    public int GetObjectExpandedTypeInfoFromID(int boxed, COR_TYPEID id, nint pTypeInfo)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetObjectExpandedTypeInfoFromID(boxed, id, pTypeInfo) : HResults.E_NOTIMPL;

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
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetCollectibleTypeStaticAddress(vmField, pRetVal) : HResults.E_NOTIMPL;

    public int GetEnCHangingFieldInfo(nint pEnCFieldInfo, nint pFieldData, Interop.BOOL* pfStatic)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetEnCHangingFieldInfo(pEnCFieldInfo, pFieldData, pfStatic) : HResults.E_NOTIMPL;

    public int GetTypeHandleParams(ulong vmTypeHandle, nint pParams)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetTypeHandleParams(vmTypeHandle, pParams) : HResults.E_NOTIMPL;

    public int GetSimpleType(int simpleType, uint* pMetadataToken, ulong* pVmModule)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetSimpleType(simpleType, pMetadataToken, pVmModule) : HResults.E_NOTIMPL;

    public int IsExceptionObject(ulong vmObject, Interop.BOOL* pResult)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.IsExceptionObject(vmObject, pResult) : HResults.E_NOTIMPL;

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

    public int GetRcwCachedInterfacePointers(ulong vmObject, Interop.BOOL bIInspectableOnly, nint pDacItfPtrs)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetRcwCachedInterfacePointers(vmObject, bIInspectableOnly, pDacItfPtrs) : HResults.E_NOTIMPL;

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

    public int IsWinRTModule(ulong vmModule, Interop.BOOL* isWinRT)
    {
        *isWinRT = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL isWinRTLocal;
            int hrLocal = _legacy.IsWinRTModule(vmModule, &isWinRTLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*isWinRT == isWinRTLocal, $"cDAC: {*isWinRT}, DAC: {isWinRTLocal}");
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

    public int AreGCStructuresValid(Interop.BOOL* pResult)
    {
        // Native DacDbiInterfaceImpl::AreGCStructuresValid always returns TRUE.
        // DacDbi callers assume the runtime is suspended, so GC structures are always valid.
        *pResult = Interop.BOOL.TRUE;
        int hr = HResults.S_OK;
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal;
            int hrLocal = _legacy.AreGCStructuresValid(&resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal, $"cDAC: {*pResult}, DAC: {resultLocal}");
        }
#endif
        return hr;
    }

    public int CreateHeapWalk(nuint* pHandle)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.CreateHeapWalk(pHandle) : HResults.E_NOTIMPL;

    public int DeleteHeapWalk(nuint handle)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.DeleteHeapWalk(handle) : HResults.E_NOTIMPL;

    public int WalkHeap(nuint handle, uint count, COR_HEAPOBJECT* objects, uint* fetched)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.WalkHeap(handle, count, objects, fetched) : HResults.E_NOTIMPL;

    public int GetHeapSegments(nint pSegments)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetHeapSegments(pSegments) : HResults.E_NOTIMPL;

    public int IsValidObject(ulong obj, Interop.BOOL* pResult)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.IsValidObject(obj, pResult) : HResults.E_NOTIMPL;

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

    public int GetObjectFields(nint id, uint celt, COR_FIELD* layout, uint* pceltFetched)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetObjectFields(id, celt, layout, pceltFetched) : HResults.E_NOTIMPL;

    public int GetTypeLayout(nint id, COR_TYPE_LAYOUT* pLayout)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetTypeLayout(id, pLayout) : HResults.E_NOTIMPL;

    public int GetArrayLayout(nint id, nint pLayout)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetArrayLayout(id, pLayout) : HResults.E_NOTIMPL;

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
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetActiveRejitILCodeVersionNode(vmModule, methodTk, pVmILCodeVersionNode) : HResults.E_NOTIMPL;

    public int GetNativeCodeVersionNode(ulong vmMethod, ulong codeStartAddress, ulong* pVmNativeCodeVersionNode)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetNativeCodeVersionNode(vmMethod, codeStartAddress, pVmNativeCodeVersionNode) : HResults.E_NOTIMPL;

    public int GetILCodeVersionNode(ulong vmNativeCodeVersionNode, ulong* pVmILCodeVersionNode)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetILCodeVersionNode(vmNativeCodeVersionNode, pVmILCodeVersionNode) : HResults.E_NOTIMPL;

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

    public int GetLoaderHeapMemoryRanges(nint pRanges)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetLoaderHeapMemoryRanges(pRanges) : HResults.E_NOTIMPL;

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
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.GetGenericArgTokenIndex(vmMethod, pIndex) : HResults.E_NOTIMPL;
}
