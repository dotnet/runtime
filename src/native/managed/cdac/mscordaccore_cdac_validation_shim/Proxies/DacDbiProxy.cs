// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Validation-shim proxy. Each method calls the production cDAC first (its result is always the one
// returned to the caller), then calls the legacy DAC and compares. The `#if DEBUG` comparison blocks
// are the pre-refactor cDAC blocks, recovered verbatim from the implementations that hosted the
// legacy DAC before the production decoupling; `hr` is the production cDAC result and the `_legacy*`
// fields are the legacy DAC's interfaces, exactly as they were in the original code.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// IDacDbiInterface surface of a paired cDAC/DAC DBI object.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class DacDbiProxy
    : ShimProxy, ICustomQueryInterface, IDacDbiInterface
{
    private readonly IDacDbiInterface? _cdac;
    private readonly IDacDbiInterface? _legacy;

    internal DacDbiProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdac = cdacObject as IDacDbiInterface;
        _legacy = dacObject as IDacDbiInterface;
    }

    /// <summary>
    /// Mirrors the production cDAC object's QueryInterface surface exactly: an interface is only
    /// exposed to the caller when the object being proxied exposes it, so consumers cannot observe
    /// a capability the cDAC does not actually have.
    /// </summary>
    public CustomQueryInterfaceResult GetInterface(ref Guid iid, out nint ppv)
    {
        ppv = default;
        CustomQueryInterfaceResult? custom = null;
        GetCustomInterface(ref iid, ref ppv, ref custom);
        if (custom is not null)
            return custom.Value;

        if (iid == typeof(IDacDbiInterface).GUID)
            return Support(_cdac, _legacy);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IDacDbiInterface
    int IDacDbiInterface.DacSetTargetConsistencyChecks(Interop.BOOL fEnableAsserts)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.DacSetTargetConsistencyChecks(fEnableAsserts) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacy is not null && LegacyFallbackHelper.CanFallback("DacSetTargetConsistencyChecks", "DacDbiImpl.cs"))
        {
            return _legacy.DacSetTargetConsistencyChecks(fEnableAsserts);
        }
        return hr;
    }

    int IDacDbiInterface.IsLeftSideInitialized(Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsLeftSideInitialized(pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetAppDomainId(ulong vmAppDomain, uint* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetAppDomainId(vmAppDomain, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetAppDomainFullName(ulong vmAppDomain, nint pStrName)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetAppDomainFullName(vmAppDomain, pStrName) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.GetAppDomainFullName(vmAppDomain, pStrName);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetModuleSimpleName(ulong vmModule, nint pStrFilename)
    {
        using ShimCall shimCall = ShimCall.Enter();
        using NativeStringHolder cdacHolder = new(pStrFilename);
        int hr = _cdac is not null ? _cdac.GetModuleSimpleName(vmModule, cdacHolder.Ptr) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            using var legacyHolder = new NativeStringHolder();
            int hrLocal = _legacy.GetModuleSimpleName(vmModule, legacyHolder.Ptr);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(
                    string.Equals(cdacHolder.Value, legacyHolder.Value, System.StringComparison.Ordinal),
                    $"GetModuleSimpleName string mismatch - cDAC: '{cdacHolder.Value}', DAC: '{legacyHolder.Value}'");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetAssemblyPath(ulong vmAssembly, nint pStrFilename, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetAssemblyPath(vmAssembly, pStrFilename, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.ResolveTypeReference(DacDbiTypeRefData* pTypeRefInfo, DacDbiTypeRefData* pTargetRefInfo)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.ResolveTypeReference(pTypeRefInfo, pTargetRefInfo) : HResults.E_NOTIMPL;
#if DEBUG
        if (hr == HResults.S_OK && _legacy is not null)
        {
            DacDbiTypeRefData targetLocal = default;
            int hrLocal = _legacy.ResolveTypeReference(pTypeRefInfo, &targetLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pTargetRefInfo->vmAssembly == targetLocal.vmAssembly, $"cDAC: {pTargetRefInfo->vmAssembly:x}, DAC: {targetLocal.vmAssembly:x}");
                Debug.Assert(pTargetRefInfo->typeToken == targetLocal.typeToken, $"cDAC: {pTargetRefInfo->typeToken:x}, DAC: {targetLocal.typeToken:x}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetModulePath(ulong vmModule, nint pStrFilename, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetModulePath(vmModule, pStrFilename, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetMetadata(ulong vmModule, DacDbiTargetBuffer* pTargetBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetMetadata(vmModule, pTargetBuffer) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            DacDbiTargetBuffer pTargetBufferLocal = default;
            int hrLocal = _legacy.GetMetadata(vmModule, pTargetBuffer == null ? null : &pTargetBufferLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pTargetBuffer->pAddress == pTargetBufferLocal.pAddress, $"pAddress: cDAC: {pTargetBuffer->pAddress:x}, DAC: {pTargetBufferLocal.pAddress:x}");
                Debug.Assert(pTargetBuffer->cbSize == pTargetBufferLocal.cbSize, $"cbSize: cDAC: {pTargetBuffer->cbSize}, DAC: {pTargetBufferLocal.cbSize}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetSymbolsBuffer(ulong vmModule, DacDbiTargetBuffer* pTargetBuffer, SymbolFormat* pSymbolFormat)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetSymbolsBuffer(vmModule, pTargetBuffer, pSymbolFormat) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetModuleData(ulong vmModule, DacDbiModuleInfo* pData)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetModuleData(vmModule, pData) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetModuleForAssembly(ulong vmAssembly, ulong* pModule, Interop.BOOL* pIsModuleLoaded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetModuleForAssembly(vmAssembly, pModule, pIsModuleLoaded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            ulong moduleLocal;
            Interop.BOOL isModuleLoadedLocal;
            int hrLocal = _legacy.GetModuleForAssembly(vmAssembly, &moduleLocal, &isModuleLoadedLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pModule == moduleLocal, $"cDAC: {*pModule:x}, DAC: {moduleLocal:x}");
                if (pIsModuleLoaded != null)
                    Debug.Assert(*pIsModuleLoaded == isModuleLoadedLocal, $"cDAC: {*pIsModuleLoaded}, DAC: {isModuleLoadedLocal}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.IsManagedCode(ulong address, Interop.BOOL* pIsManaged)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsManagedCode(address, pIsManaged) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL isManagedLocal;
            int hrLocal = _legacy.IsManagedCode(address, &isManagedLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pIsManaged == isManagedLocal, $"cDAC: {*pIsManaged}, DAC: {isManagedLocal}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetCompilerFlags(ulong vmAssembly, Interop.BOOL* pfAllowJITOpts, Interop.BOOL* pfEnableEnC)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetCompilerFlags(vmAssembly, pfAllowJITOpts, pfEnableEnC) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.SetCompilerFlags(ulong vmAssembly, Interop.BOOL fAllowJitOpts, Interop.BOOL fEnableEnC)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.SetCompilerFlags(vmAssembly, fAllowJitOpts, fEnableEnC) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.SetCompilerFlags(vmAssembly, fAllowJitOpts, fEnableEnC);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.EnumerateAssembliesInAppDomain(ulong vmAppDomain, delegate* unmanaged<ulong, nint, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        ULongEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<ulong, nint, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordULongAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateAssembliesInAppDomain(vmAppDomain, cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
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
                        cdacState!.Values.SequenceEqual(dacAssemblies),
                        $"Assembly enumeration mismatch - "
                        + $"cDAC: [{string.Join(",", cdacState!.Values.Select(a => $"0x{a:x}"))}], "
                        + $"DAC: [{string.Join(",", dacAssemblies.Select(a => $"0x{a:x}"))}]");
                }
            }
            finally
            {
                dacHandle.Free();
            }
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateAssembliesInAppDomain(vmAppDomain, fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.RequestSyncAtEvent()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.RequestSyncAtEvent() : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.RequestSyncAtEvent();
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.SetSendExceptionsOutsideOfJMC(Interop.BOOL sendExceptionsOutsideOfJMC)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.SetSendExceptionsOutsideOfJMC(sendExceptionsOutsideOfJMC) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.SetSendExceptionsOutsideOfJMC(sendExceptionsOutsideOfJMC);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.MarkDebuggerAttachPending()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.MarkDebuggerAttachPending() : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.MarkDebuggerAttachPending();
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.MarkDebuggerAttached(Interop.BOOL fAttached)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.MarkDebuggerAttached(fAttached) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.MarkDebuggerAttached(fAttached);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.Hijack(ulong vmThread, uint dwThreadId, nint pRecord, nint pOriginalContext, uint cbSizeContext, int reason, nint pUserData, ulong* pRemoteContextAddr)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.Hijack(vmThread, dwThreadId, pRecord, pOriginalContext, cbSizeContext, reason, pUserData, pRemoteContextAddr) : HResults.E_NOTIMPL;
        return hr;
    }

    int IDacDbiInterface.EnumerateThreads(delegate* unmanaged<ulong, nint, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        ULongEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<ulong, nint, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordULongAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateThreads(cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null && cdacState is not null)
        {
            List<ulong> dacThreads = new();
            GCHandle dacHandle = GCHandle.Alloc(dacThreads);
            try
            {
                int hrLocal = _legacy.EnumerateThreads(&CollectEnumerationCallback, GCHandle.ToIntPtr(dacHandle));
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    Debug.Assert(
                        cdacState!.Values.SequenceEqual(dacThreads),
                        $"Thread enumeration mismatch - cDAC: [{string.Join(",", cdacState!.Values.Select(t => $"0x{t:x}"))}], DAC: [{string.Join(",", dacThreads.Select(t => $"0x{t:x}"))}]");
                }
            }
            finally
            {
                dacHandle.Free();
            }
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateThreads(fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.IsThreadMarkedDead(ulong vmThread, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsThreadMarkedDead(vmThread, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetThreadHandle(ulong vmThread, void** pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetThreadHandle(vmThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetThreadObject(ulong vmThread, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetThreadObject(vmThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetThreadAllocInfo(ulong vmThread, DacDbiThreadAllocInfo* pThreadAllocInfo)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetThreadAllocInfo(vmThread, pThreadAllocInfo) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.SetDebugState(ulong vmThread, int debugState)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.SetDebugState(vmThread, debugState) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.SetDebugState(vmThread, debugState);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.HasUnhandledException(ulong vmThread, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.HasUnhandledException(vmThread, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetUserState(ulong vmThread, int* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetUserState(vmThread, pRetVal) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            int retValLocal;
            int hrLocal = _legacy.GetUserState(vmThread, &retValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == retValLocal, $"cDAC: {*pRetVal}, DAC: {retValLocal}");
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetPartialUserState(ulong vmThread, CorDebugUserState* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetPartialUserState(vmThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetConnectionID(ulong vmThread, uint* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetConnectionID(vmThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetTaskID(ulong vmThread, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetTaskID(vmThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.TryGetVolatileOSThreadID(ulong vmThread, uint* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.TryGetVolatileOSThreadID(vmThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetUniqueThreadID(ulong vmThread, uint* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetUniqueThreadID(vmThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetCurrentException(ulong vmThread, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetCurrentException(vmThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetObjectForCCW(ulong ccwPtr, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetObjectForCCW(ccwPtr, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetCurrentCustomDebuggerNotification(ulong vmThread, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetCurrentCustomDebuggerNotification(vmThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetCurrentAppDomain(ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetCurrentAppDomain(pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.ResolveAssembly(ulong vmScope, uint tkAssemblyRef, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.ResolveAssembly(vmScope, tkAssemblyRef, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetNativeCodeSequencePointsAndVarInfo(ulong vmMethodDesc,
        ulong startAddress,
        Interop.BOOL fCodeAvailable,
        uint* pFixedArgCount,
        delegate* unmanaged<NativeVarInfo*, void*, void> fpVarInfoCallback,
        delegate* unmanaged<DbiOffsetMapping*, void*, void> fpSeqPointCallback,
        nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        DebugNativeCodeData? cdacData = _legacy is not null ? new(fpVarInfoCallback, fpSeqPointCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<NativeVarInfo*, void*, void> varInfoCallback = fpVarInfoCallback;
        delegate* unmanaged<DbiOffsetMapping*, void*, void> seqPointCallback = fpSeqPointCallback;
        nint cdacUserData = pUserData;
        if (cdacData is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacData);
            if (fpVarInfoCallback != null)
                varInfoCallback = &RecordNativeVarInfoAndForwardCallback;
            if (fpSeqPointCallback != null)
                seqPointCallback = &RecordOffsetMappingAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.GetNativeCodeSequencePointsAndVarInfo(vmMethodDesc, startAddress, fCodeAvailable, pFixedArgCount, varInfoCallback, seqPointCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null)
        {
            ValidateNativeCodeInfoAgainstLegacy(
                vmMethodDesc, startAddress, fCodeAvailable,
                pFixedArgCount, cdacData!.VarInfos, cdacData.SeqPoints, hr,
                varInfoRequested: fpVarInfoCallback != null,
                seqPointsRequested: fpSeqPointCallback != null);
        }
#else
        int hr = _cdac is not null ? _cdac.GetNativeCodeSequencePointsAndVarInfo(vmMethodDesc, startAddress, fCodeAvailable, pFixedArgCount, fpVarInfoCallback, fpSeqPointCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.GetManagedStoppedContext(ulong vmThread, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetManagedStoppedContext(vmThread, pRetVal) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            ulong pRetValLocal;
            int hrLocal = _legacy.GetManagedStoppedContext(vmThread, &pRetValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == pRetValLocal, $"cDAC: {*pRetVal:x}, DAC: {pRetValLocal:x}");
        }
#endif
        return hr;
    }

    int IDacDbiInterface.CreateStackWalk(ulong vmThread, byte* pInternalContextBuffer, nuint* ppSFIHandle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        nuint cdacHandle = 0;
        int hr = _cdac is not null ? _cdac.CreateStackWalk(vmThread, pInternalContextBuffer, ppSFIHandle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        if (ppSFIHandle is not null)
            *ppSFIHandle = cdacHandle;
        if (_legacy is not null && LegacyFallbackHelper.CanFallback("CreateStackWalk", "DacDbiImpl.cs"))
        {
            nuint legacyHandle = 0;
            byte* pLocal = (byte*)NativeMemory.AlignedAlloc(MaxContextBufferSize, 16);
            try
            {
                new Span<byte>(pLocal, MaxContextBufferSize).Clear();
                int hrLocal = _legacy.CreateStackWalk(vmThread, pLocal, &legacyHandle);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK && hrLocal == HResults.S_OK && ppSFIHandle is not null)
                {
                    *ppSFIHandle = (nuint)_session.RegisterHandle((ulong)cdacHandle, (ulong)legacyHandle, hasDacHandle: true);
                }
                else if (hrLocal == HResults.S_OK)
                {
                    _legacy.DeleteStackWalk(legacyHandle);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(pLocal);
            }
        }
        return hr;
    }

    int IDacDbiInterface.DeleteStackWalk(nuint ppSFIHandle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle((ulong)ppSFIHandle);
        nuint cdacHandle = pair is not null ? (nuint)pair.CDacHandle : ppSFIHandle;
        int hr = _cdac is not null ? _cdac.DeleteStackWalk(cdacHandle) : HResults.E_NOTIMPL;
        if (_legacy is not null && LegacyFallbackHelper.CanFallback("DeleteStackWalk", "DacDbiImpl.cs") && pair is not null && pair.HasDacHandle)
        {
            int hrLocal = _legacy.DeleteStackWalk((nuint)pair.DacHandle);
            Debug.ValidateHResult(hr, hrLocal);
        }
        else if (hr == HResults.E_NOTIMPL && _legacy is not null && LegacyFallbackHelper.CanFallback("DeleteStackWalk", "DacDbiImpl.cs"))
        {
            return _legacy.DeleteStackWalk(ppSFIHandle);
        }
        return hr;
    }

    int IDacDbiInterface.GetStackWalkCurrentContext(nuint pSFIHandle, byte* pContext)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.LookupHandle((ulong)pSFIHandle);
        nuint cdacHandle = pair is not null ? (nuint)pair.CDacHandle : pSFIHandle;
        int hr = _cdac is not null ? _cdac.GetStackWalkCurrentContext(cdacHandle, pContext) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null && pair is not null && pair.HasDacHandle)
        {
            byte* pLocal = (byte*)NativeMemory.AlignedAlloc(MaxContextBufferSize, 16);
            try
            {
                new Span<byte>(pLocal, MaxContextBufferSize).Clear();
                int hrLocal = _legacy.GetStackWalkCurrentContext((nuint)pair.DacHandle, pLocal);
                Debug.ValidateHResult(hr, hrLocal);
            }
            finally
            {
                NativeMemory.AlignedFree(pLocal);
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.SetStackWalkCurrentContext(ulong vmThread, nuint pSFIHandle, int flag, byte* pContext)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.LookupHandle((ulong)pSFIHandle);
        nuint cdacHandle = pair is not null ? (nuint)pair.CDacHandle : pSFIHandle;
        int hr = _cdac is not null ? _cdac.SetStackWalkCurrentContext(vmThread, cdacHandle, flag, pContext) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacy is not null && LegacyFallbackHelper.CanFallback("SetStackWalkCurrentContext", "DacDbiImpl.cs"))
        {
            return _legacy.SetStackWalkCurrentContext(vmThread, pair is not null && pair.HasDacHandle ? (nuint)pair.DacHandle : pSFIHandle, flag, pContext);
        }
        return hr;
    }

    int IDacDbiInterface.UnwindStackWalkFrame(nuint pSFIHandle, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.LookupHandle((ulong)pSFIHandle);
        nuint cdacHandle = pair is not null ? (nuint)pair.CDacHandle : pSFIHandle;
        int hr = _cdac is not null ? _cdac.UnwindStackWalkFrame(cdacHandle, pResult) : HResults.E_NOTIMPL;
        if (_legacy is not null && LegacyFallbackHelper.CanFallback("UnwindStackWalkFrame", "DacDbiImpl.cs") && pair is not null && pair.HasDacHandle)
        {
            Interop.BOOL localResult;
            int hrLocal = _legacy.UnwindStackWalkFrame((nuint)pair.DacHandle, &localResult);
            Debug.ValidateHResult(hr, hrLocal);
#if DEBUG
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pResult == localResult, $"cDAC: {*pResult}, DAC: {localResult}");
            }
#endif
        }
        else if (hr == HResults.E_NOTIMPL && _legacy is not null && LegacyFallbackHelper.CanFallback("UnwindStackWalkFrame", "DacDbiImpl.cs"))
        {
            return _legacy.UnwindStackWalkFrame(pSFIHandle, pResult);
        }
        return hr;
    }

    int IDacDbiInterface.CheckContext(ulong vmThread, byte* pContext)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.CheckContext(vmThread, pContext) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.CheckContext(vmThread, pContext);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetStackWalkCurrentFrameInfo(nuint pSFIHandle, nint pFrameData, FrameType* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.LookupHandle((ulong)pSFIHandle);
        nuint cdacHandle = pair is not null ? (nuint)pair.CDacHandle : pSFIHandle;
        int hr = _cdac is not null ? _cdac.GetStackWalkCurrentFrameInfo(cdacHandle, pFrameData, pRetVal) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null && pair is not null && pair.HasDacHandle)
        {
            byte* pLegacyCtx = (byte*)NativeMemory.AlignedAlloc(MaxContextBufferSize, 16);
            try
            {
                new Span<byte>(pLegacyCtx, MaxContextBufferSize).Clear();
                Debugger_STRData legacyData = default;
                legacyData.ctx = (nuint)pLegacyCtx;
                FrameType legacyRetVal = FrameType.Invalid;
                int hrLocal = _legacy.GetStackWalkCurrentFrameInfo((nuint)pair.DacHandle, (nint)(&legacyData), &legacyRetVal);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    FrameType ftResult = pRetVal is not null ? *pRetVal : FrameType.Invalid;
                    Debug.Assert(ftResult == legacyRetVal, $"FrameType mismatch - cDAC: {ftResult}, DAC: {legacyRetVal}");
                    if (pFrameData != 0)
                    {
                        Debugger_STRData* pcdac = (Debugger_STRData*)pFrameData;
                        Debug.Assert(pcdac->fp == legacyData.fp, $"fp mismatch - cDAC: 0x{pcdac->fp:x}, DAC: 0x{legacyData.fp:x}");
                        Debug.Assert(pcdac->vmCurrentAppDomainToken == legacyData.vmCurrentAppDomainToken, $"appDomain mismatch - cDAC: 0x{pcdac->vmCurrentAppDomainToken:x}, DAC: 0x{legacyData.vmCurrentAppDomainToken:x}");
                        Debug.Assert(pcdac->eType == legacyData.eType, $"eType mismatch - cDAC: {pcdac->eType}, DAC: {legacyData.eType}");
                        if (ftResult == FrameType.ManagedStackFrame)
                        {
                            DebuggerIPCE_STRData_MethodFrame cv = pcdac->v;
                            DebuggerIPCE_STRData_MethodFrame lv = legacyData.v;
                            Debug.Assert(cv.mapping == lv.mapping, $"mapping mismatch - cDAC: {cv.mapping}, DAC: {lv.mapping}");
                            Debug.Assert(cv.fVarArgs == lv.fVarArgs, $"fVarArgs mismatch - cDAC: {cv.fVarArgs}, DAC: {lv.fVarArgs}");
                            Debug.Assert(cv.fNoMetadata == lv.fNoMetadata, $"fNoMetadata mismatch - cDAC: {cv.fNoMetadata}, DAC: {lv.fNoMetadata}");
                            Debug.Assert(cv.taAmbientESP == lv.taAmbientESP, $"taAmbientESP mismatch - cDAC: 0x{cv.taAmbientESP:x}, DAC: 0x{lv.taAmbientESP:x}");
                            Debug.Assert(cv.exactGenericArgsToken == lv.exactGenericArgsToken, $"exactGenericArgsToken mismatch - cDAC: 0x{cv.exactGenericArgsToken:x}, DAC: 0x{lv.exactGenericArgsToken:x}");
                            Debug.Assert(cv.dwExactGenericArgsTokenIndex == lv.dwExactGenericArgsTokenIndex, $"dwExactGenericArgsTokenIndex mismatch - cDAC: 0x{cv.dwExactGenericArgsTokenIndex:x}, DAC: 0x{lv.dwExactGenericArgsTokenIndex:x}");
                            Debug.Assert(cv.funcData.funcMetadataToken == lv.funcData.funcMetadataToken, $"funcMetadataToken mismatch - cDAC: 0x{cv.funcData.funcMetadataToken:x}, DAC: 0x{lv.funcData.funcMetadataToken:x}");
                            Debug.Assert(cv.funcData.vmAssembly == lv.funcData.vmAssembly, $"vmAssembly mismatch - cDAC: 0x{cv.funcData.vmAssembly:x}, DAC: 0x{lv.funcData.vmAssembly:x}");
                            Debug.Assert(cv.jitFuncData.nativeStartAddressPtr == lv.jitFuncData.nativeStartAddressPtr, $"nativeStartAddressPtr mismatch - cDAC: 0x{cv.jitFuncData.nativeStartAddressPtr:x}, DAC: 0x{lv.jitFuncData.nativeStartAddressPtr:x}");
                            Debug.Assert(cv.jitFuncData.nativeOffset == lv.jitFuncData.nativeOffset, $"nativeOffset mismatch - cDAC: 0x{cv.jitFuncData.nativeOffset:x}, DAC: 0x{lv.jitFuncData.nativeOffset:x}");
                            Debug.Assert(cv.jitFuncData.vmNativeCodeMethodDescToken == lv.jitFuncData.vmNativeCodeMethodDescToken, $"vmNativeCodeMethodDescToken mismatch - cDAC: 0x{cv.jitFuncData.vmNativeCodeMethodDescToken:x}, DAC: 0x{lv.jitFuncData.vmNativeCodeMethodDescToken:x}");
                            Debug.Assert(cv.jitFuncData.fIsFilterFrame == lv.jitFuncData.fIsFilterFrame, $"fIsFilterFrame mismatch - cDAC: {cv.jitFuncData.fIsFilterFrame}, DAC: {lv.jitFuncData.fIsFilterFrame}");
                            Debug.Assert(cv.jitFuncData.isInstantiatedGeneric == lv.jitFuncData.isInstantiatedGeneric, $"isInstantiatedGeneric mismatch - cDAC: {cv.jitFuncData.isInstantiatedGeneric}, DAC: {lv.jitFuncData.isInstantiatedGeneric}");
                            Debug.Assert(cv.jitFuncData.fpParentOrSelf == lv.jitFuncData.fpParentOrSelf, $"fpParentOrSelf mismatch - cDAC: 0x{cv.jitFuncData.fpParentOrSelf:x}, DAC: 0x{lv.jitFuncData.fpParentOrSelf:x}");
                            Debug.Assert(cv.jitFuncData.parentNativeOffset == lv.jitFuncData.parentNativeOffset, $"parentNativeOffset mismatch - cDAC: 0x{cv.jitFuncData.parentNativeOffset:x}, DAC: 0x{lv.jitFuncData.parentNativeOffset:x}");
                        }
                    }
                }
            }
            finally
            {
                NativeMemory.AlignedFree(pLegacyCtx);
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetCountOfInternalFrames(ulong vmThread, uint* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetCountOfInternalFrames(vmThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.EnumerateInternalFrames(ulong vmThread, delegate* unmanaged<Debugger_STRData*, void*, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        StubFrameEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<Debugger_STRData*, void*, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordStubFrameAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateInternalFrames(vmThread, cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null && cdacState is not null)
        {
            List<Debugger_STRData> dacFrames = new();
            GCHandle dacHandle = GCHandle.Alloc(dacFrames);
            try
            {
                int hrLocal = _legacy.EnumerateInternalFrames(vmThread, (delegate* unmanaged<Debugger_STRData*, void*, void>)&CollectStubFrameCallback, GCHandle.ToIntPtr(dacHandle));
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    Debug.Assert(cdacState!.Values.Count == dacFrames.Count, $"Internal frame count mismatch - cDAC: {cdacState!.Values.Count}, DAC: {dacFrames.Count}");
                    int n = Math.Min(cdacState!.Values.Count, dacFrames.Count);
                    for (int i = 0; i < n; i++)
                    {
                        Debugger_STRData c = cdacState!.Values[i];
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
            finally
            {
                dacHandle.Free();
            }
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateInternalFrames(vmThread, fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.GetStackParameterSize(ulong controlPC, uint* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetStackParameterSize(controlPC, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.IsLeafFrame(ulong vmThread, byte* pContext, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsLeafFrame(vmThread, pContext, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetContext(ulong vmThread, byte* pContextBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetContext(vmThread, pContextBuffer) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            byte* pLocal = (byte*)NativeMemory.AlignedAlloc(MaxContextBufferSize, 16);
            try
            {
                new Span<byte>(pLocal, MaxContextBufferSize).Clear();
                int hrLocal = _legacy.GetContext(vmThread, pLocal);
                Debug.ValidateHResult(hr, hrLocal);
            }
            finally
            {
                NativeMemory.AlignedFree(pLocal);
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.IsDiagnosticsHiddenOrLCGMethod(ulong vmMethodDesc, int* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsDiagnosticsHiddenOrLCGMethod(vmMethodDesc, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetVarArgSig(ulong VASigCookieAddr, ulong* pArgBase, DacDbiTargetBuffer* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetVarArgSig(VASigCookieAddr, pArgBase, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.RequiresAlign8(ulong thExact, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.RequiresAlign8(thExact, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.ResolveExactGenericArgsToken(uint dwExactGenericArgsTokenIndex, ulong rawToken, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.ResolveExactGenericArgsToken(dwExactGenericArgsTokenIndex, rawToken, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetILCodeAndSig(ulong vmAssembly, uint functionToken, DacDbiTargetBuffer* pTargetBuffer, uint* pLocalSigToken)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetILCodeAndSig(vmAssembly, functionToken, pTargetBuffer, pLocalSigToken) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            DacDbiTargetBuffer bufferLocal = default;
            uint sigLocal;
            int hrLocal = _legacy.GetILCodeAndSig(vmAssembly, functionToken, &bufferLocal, &sigLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pTargetBuffer->pAddress == bufferLocal.pAddress, $"cDAC ILAddr: 0x{pTargetBuffer->pAddress:X}, DAC ILAddr: 0x{bufferLocal.pAddress:X}");
                Debug.Assert(pTargetBuffer->cbSize == bufferLocal.cbSize, $"cDAC ILSize: {pTargetBuffer->cbSize}, DAC ILSize: {bufferLocal.cbSize}");
                Debug.Assert(*pLocalSigToken == sigLocal, $"cDAC LocalSig: 0x{*pLocalSigToken:X}, DAC LocalSig: 0x{sigLocal:X}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetNativeCodeInfo(ulong vmAssembly, uint functionToken, nint pJitManagerList)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetNativeCodeInfo(vmAssembly, functionToken, pJitManagerList) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacy is not null && LegacyFallbackHelper.CanFallback("GetNativeCodeInfo", "DacDbiImpl.cs"))
        {
            return _legacy.GetNativeCodeInfo(vmAssembly, functionToken, pJitManagerList);
        }
        return hr;
    }

    int IDacDbiInterface.GetNativeCodeInfoForAddr(ulong codeAddress, nint pCodeInfo, ulong* pVmModule, uint* pFunctionToken)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetNativeCodeInfoForAddr(codeAddress, pCodeInfo, pVmModule, pFunctionToken) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacy is not null && LegacyFallbackHelper.CanFallback("GetNativeCodeInfoForAddr", "DacDbiImpl.cs"))
        {
            return _legacy.GetNativeCodeInfoForAddr(codeAddress, pCodeInfo, pVmModule, pFunctionToken);
        }
        return hr;
    }

    int IDacDbiInterface.IsValueType(ulong vmTypeHandle, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsValueType(vmTypeHandle, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.HasTypeParams(ulong vmTypeHandle, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.HasTypeParams(vmTypeHandle, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.EnumerateClassFields(ulong thExact, nuint* pObjectSize, delegate* unmanaged<FieldData*, void*, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        FieldEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<FieldData*, void*, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordFieldDataAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateClassFields(thExact, pObjectSize, cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null && cdacState is not null)
        {
            ValidateEnumerateFieldsAgainstLegacy(
                nameof(IDacDbiInterface.EnumerateClassFields),
                pObjectSize is null ? 0 : *pObjectSize,
                cdacState!.Values,
                hr,
                (pSize, pUser) => _legacy!.EnumerateClassFields(thExact, pSize, (delegate* unmanaged<FieldData*, void*, void>)&CollectFieldDataCallback, pUser));
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateClassFields(thExact, pObjectSize, fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.EnumerateInstantiationFields(ulong vmAssembly, ulong vmThExact, ulong vmThApprox, nuint* pObjectSize, delegate* unmanaged<FieldData*, void*, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        FieldEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<FieldData*, void*, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordFieldDataAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateInstantiationFields(vmAssembly, vmThExact, vmThApprox, pObjectSize, cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null && cdacState is not null)
        {
            ValidateEnumerateFieldsAgainstLegacy(
                nameof(IDacDbiInterface.EnumerateInstantiationFields),
                pObjectSize is null ? 0 : *pObjectSize,
                cdacState!.Values,
                hr,
                (pSize, pUser) => _legacy!.EnumerateInstantiationFields(vmAssembly, vmThExact, vmThApprox, pSize, (delegate* unmanaged<FieldData*, void*, void>)&CollectFieldDataCallback, pUser));
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateInstantiationFields(vmAssembly, vmThExact, vmThApprox, pObjectSize, fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.TypeHandleToExpandedTypeInfo(AreValueTypesBoxed boxed, ulong vmTypeHandle, DebuggerIPCE_ExpandedTypeData* pTypeInfo)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.TypeHandleToExpandedTypeInfo(boxed, vmTypeHandle, pTypeInfo) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetObjectExpandedTypeInfo(AreValueTypesBoxed boxed, ulong addr, DebuggerIPCE_ExpandedTypeData* pTypeInfo)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetObjectExpandedTypeInfo(boxed, addr, pTypeInfo) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetTypeHandle(ulong vmModule, uint metadataToken, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetTypeHandle(vmModule, metadataToken, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetApproxTypeHandle(TypeInfoList* pTypeData, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetApproxTypeHandle(pTypeData, pRetVal) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            ulong vmLocal;
            int hrLocal = _legacy.GetApproxTypeHandle(pTypeData, &vmLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == vmLocal, $"cDAC: {*pRetVal:x}, DAC: {vmLocal:x}");
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetExactTypeHandle(DebuggerIPCE_ExpandedTypeData* pTypeData, ArgInfoList* pArgInfo, ulong* pVmTypeHandle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetExactTypeHandle(pTypeData, pArgInfo, pVmTypeHandle) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            ulong vmLocal;
            int hrLocal = _legacy.GetExactTypeHandle(pTypeData, pArgInfo, &vmLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pVmTypeHandle == vmLocal, $"cDAC: {*pVmTypeHandle:x}, DAC: {vmLocal:x}");
        }
#endif
        return hr;
    }

    int IDacDbiInterface.EnumerateMethodDescParams(ulong vmMethodDesc, ulong genericsToken, uint* pcGenericClassTypeParams,
        delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        ExpandedTypeEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordExpandedTypeAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateMethodDescParams(vmMethodDesc, genericsToken, pcGenericClassTypeParams, cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null && cdacState is not null)
        {
            uint cClassParamsLocal = 0;
            ExpandedTypeEnumerationState legacyState = new(null, 0);
            GCHandle legacyHandle = GCHandle.Alloc(legacyState);
            try
            {
                int hrLocal = _legacy.EnumerateMethodDescParams(vmMethodDesc, genericsToken, &cClassParamsLocal, &CollectExpandedTypeCallback, GCHandle.ToIntPtr(legacyHandle));
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    if (pcGenericClassTypeParams is not null)
                    {
                        Debug.Assert(*pcGenericClassTypeParams == cClassParamsLocal,
                            $"cDAC class params: {*pcGenericClassTypeParams}, DAC: {cClassParamsLocal}");
                    }
                    AssertExpandedTypeLists(cdacState!.Values, legacyState.Values);
                }
            }
            finally
            {
                legacyHandle.Free();
            }
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateMethodDescParams(vmMethodDesc, genericsToken, pcGenericClassTypeParams, fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.GetThreadStaticAddress(ulong vmField, ulong vmRuntimeThread, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetThreadStaticAddress(vmField, vmRuntimeThread, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetCollectibleTypeStaticAddress(ulong vmField, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetCollectibleTypeStaticAddress(vmField, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetEnCHangingFieldInfo(EnCHangingFieldInfo* pEnCFieldInfo, FieldData* pFieldData)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetEnCHangingFieldInfo(pEnCFieldInfo, pFieldData) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            FieldData fieldDataLocal;
            int hrLocal = _legacy.GetEnCHangingFieldInfo(pEnCFieldInfo, &fieldDataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pFieldData->m_fldMetadataToken == fieldDataLocal.m_fldMetadataToken,
                    $"cDAC m_fldMetadataToken: {pFieldData->m_fldMetadataToken:X}, DAC: {fieldDataLocal.m_fldMetadataToken:X}");
                Debug.Assert(pFieldData->m_fFldStorageAvailable == fieldDataLocal.m_fFldStorageAvailable,
                    $"cDAC m_fFldStorageAvailable: {pFieldData->m_fFldStorageAvailable}, DAC: {fieldDataLocal.m_fFldStorageAvailable}");
                Debug.Assert(pFieldData->m_fFldIsStatic == fieldDataLocal.m_fFldIsStatic,
                    $"cDAC m_fFldIsStatic: {pFieldData->m_fFldIsStatic}, DAC: {fieldDataLocal.m_fFldIsStatic}");
                Debug.Assert(pFieldData->m_fFldIsRVA == fieldDataLocal.m_fFldIsRVA,
                    $"cDAC m_fFldIsRVA: {pFieldData->m_fFldIsRVA}, DAC: {fieldDataLocal.m_fFldIsRVA}");
                Debug.Assert(pFieldData->m_fFldIsTLS == fieldDataLocal.m_fFldIsTLS,
                    $"cDAC m_fFldIsTLS: {pFieldData->m_fFldIsTLS}, DAC: {fieldDataLocal.m_fFldIsTLS}");
                Debug.Assert(pFieldData->m_fFldIsPrimitive == fieldDataLocal.m_fFldIsPrimitive,
                    $"cDAC m_fFldIsPrimitive: {pFieldData->m_fFldIsPrimitive}, DAC: {fieldDataLocal.m_fFldIsPrimitive}");
                Debug.Assert(pFieldData->m_fFldIsCollectibleStatic == fieldDataLocal.m_fFldIsCollectibleStatic,
                    $"cDAC m_fFldIsCollectibleStatic: {pFieldData->m_fFldIsCollectibleStatic}, DAC: {fieldDataLocal.m_fFldIsCollectibleStatic}");
                Debug.Assert(pFieldData->m_vmFieldDesc == fieldDataLocal.m_vmFieldDesc,
                    $"cDAC m_vmFieldDesc: {pFieldData->m_vmFieldDesc:X}, DAC: {fieldDataLocal.m_vmFieldDesc:X}");
                if (pFieldData->m_fFldIsStatic != 0)
                    Debug.Assert(pFieldData->m_pFldStaticAddress == fieldDataLocal.m_pFldStaticAddress,
                        $"cDAC static addr: {pFieldData->m_pFldStaticAddress:X}, DAC: {fieldDataLocal.m_pFldStaticAddress:X}");
                else
                    Debug.Assert(pFieldData->m_fldInstanceOffset == fieldDataLocal.m_fldInstanceOffset,
                        $"cDAC instance offset: {pFieldData->m_fldInstanceOffset:X}, DAC: {fieldDataLocal.m_fldInstanceOffset:X}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.EnumerateTypeHandleParams(ulong vmTypeHandle,
        delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        ExpandedTypeEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordExpandedTypeAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateTypeHandleParams(vmTypeHandle, cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null && cdacState is not null)
        {
            ExpandedTypeEnumerationState legacyState = new(null, 0);
            GCHandle legacyHandle = GCHandle.Alloc(legacyState);
            try
            {
                int hrLocal = _legacy.EnumerateTypeHandleParams(vmTypeHandle, &CollectExpandedTypeCallback, GCHandle.ToIntPtr(legacyHandle));
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    AssertExpandedTypeLists(cdacState!.Values, legacyState.Values);
                }
            }
            finally
            {
                legacyHandle.Free();
            }
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateTypeHandleParams(vmTypeHandle, fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.GetSimpleType(int simpleType, uint* pMetadataToken, ulong* pVmModule)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetSimpleType(simpleType, pMetadataToken, pVmModule) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.IsExceptionObject(ulong vmObject, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsExceptionObject(vmObject, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.EnumerateStackFramesFromException(ulong vmObject, delegate* unmanaged<ulong, ulong, ulong, uint, Interop.BOOL, nint, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        ExceptionFrameEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<ulong, ulong, ulong, uint, Interop.BOOL, nint, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordExceptionFrameAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateStackFramesFromException(vmObject, cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null && cdacState is not null)
        {
            ExceptionFrameEnumerationState legacyState = new(null, 0);
            GCHandle legacyHandle = GCHandle.Alloc(legacyState);
            try
            {
                int hrLocal = _legacy.EnumerateStackFramesFromException(vmObject, &CollectExceptionFrameCallback, GCHandle.ToIntPtr(legacyHandle));
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    static string FormatFrame((ulong VmAppDomain, ulong VmAssembly, ulong Ip, uint MethodDef, Interop.BOOL IsLastForeignExceptionFrame) f)
                        => $"(AppDomain=0x{f.VmAppDomain:x}, Assembly=0x{f.VmAssembly:x}, Ip=0x{f.Ip:x}, MethodDef=0x{f.MethodDef:x}, IsLastForeignExceptionFrame={f.IsLastForeignExceptionFrame})";
                    Debug.Assert(cdacState!.Values.SequenceEqual(legacyState.Values),
                        $"Exception stack frame enumeration mismatch - "
                        + $"cDAC: [{string.Join(",", cdacState!.Values.Select(FormatFrame))}], "
                        + $"DAC: [{string.Join(",", legacyState.Values.Select(FormatFrame))}]");
                }
            }
            finally
            {
                legacyHandle.Free();
            }
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateStackFramesFromException(vmObject, fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.IsRcw(ulong vmObject, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsRcw(vmObject, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.EnumerateRcwCachedInterfacePointers(ulong vmObject, delegate* unmanaged<ulong, nint, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        ULongEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<ulong, nint, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordULongAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateRcwCachedInterfacePointers(vmObject, cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null && cdacState is not null)
        {
            List<ulong> legacyItfPtrs = new();
            GCHandle legacyHandle = GCHandle.Alloc(legacyItfPtrs);
            try
            {
                int hrLocal = _legacy.EnumerateRcwCachedInterfacePointers(vmObject, &CollectEnumerationCallback, GCHandle.ToIntPtr(legacyHandle));
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    Debug.Assert(cdacState!.Values.SequenceEqual(legacyItfPtrs),
                        $"cDAC: [{string.Join(",", cdacState!.Values.Select(ptr => $"0x{ptr:x}"))}], DAC: [{string.Join(",", legacyItfPtrs.Select(ptr => $"0x{ptr:x}"))}]");
                }
            }
            finally
            {
                legacyHandle.Free();
            }
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateRcwCachedInterfacePointers(vmObject, fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.GetTypedByRefInfo(ulong pTypedByRef, ulong* pObjRef, DebuggerIPCE_BasicTypeData* pTypedByRefType)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetTypedByRefInfo(pTypedByRef, pObjRef, pTypedByRefType) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            ulong objRefLocal = 0;
            DebuggerIPCE_BasicTypeData typeLocal;
            int hrLocal = _legacy.GetTypedByRefInfo(pTypedByRef, &objRefLocal, &typeLocal);
            Debug.ValidateHResult(hr, hrLocal);
            Debug.Assert(*pObjRef == objRefLocal, $"cDAC objRef: 0x{*pObjRef:x}, DAC: 0x{objRefLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pTypedByRefType->elementType == typeLocal.elementType,
                    $"cDAC elementType: {pTypedByRefType->elementType}, DAC: {typeLocal.elementType}");
                Debug.Assert(pTypedByRefType->metadataToken == typeLocal.metadataToken,
                    $"cDAC metadataToken: 0x{pTypedByRefType->metadataToken:x}, DAC: 0x{typeLocal.metadataToken:x}");
                Debug.Assert(pTypedByRefType->vmAssembly == typeLocal.vmAssembly,
                    $"cDAC vmAssembly: 0x{pTypedByRefType->vmAssembly:x}, DAC: 0x{typeLocal.vmAssembly:x}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetStringData(ulong objectAddress, uint* pLength, uint* pOffsetToStringBase)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetStringData(objectAddress, pLength, pOffsetToStringBase) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            uint lengthLocal, offsetLocal;
            int hrLocal = _legacy.GetStringData(objectAddress, &lengthLocal, &offsetLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pLength == lengthLocal, $"cDAC length: {*pLength}, DAC: {lengthLocal}");
                Debug.Assert(*pOffsetToStringBase == offsetLocal, $"cDAC offsetToStringBase: {*pOffsetToStringBase}, DAC: {offsetLocal}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetArrayData(ulong objectAddress, Interop.BOOL* pIsValidArray, DacDbiArrayInfo* pArrayInfo)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetArrayData(objectAddress, pIsValidArray, pArrayInfo) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL isValidLocal;
            DacDbiArrayInfo arrayInfoLocal;
            int hrLocal = _legacy.GetArrayData(objectAddress, &isValidLocal, &arrayInfoLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pIsValidArray == isValidLocal, $"cDAC isValidArray: {*pIsValidArray}, DAC: {isValidLocal}");
                if (*pIsValidArray == Interop.BOOL.TRUE)
                {
                    Debug.Assert(pArrayInfo->rank == arrayInfoLocal.rank, $"cDAC rank: {pArrayInfo->rank}, DAC: {arrayInfoLocal.rank}");
                    Debug.Assert(pArrayInfo->componentCount == arrayInfoLocal.componentCount, $"cDAC componentCount: {pArrayInfo->componentCount}, DAC: {arrayInfoLocal.componentCount}");
                    Debug.Assert(pArrayInfo->offsetToArrayBase == arrayInfoLocal.offsetToArrayBase, $"cDAC offsetToArrayBase: {pArrayInfo->offsetToArrayBase}, DAC: {arrayInfoLocal.offsetToArrayBase}");
                    Debug.Assert(pArrayInfo->offsetToUpperBounds == arrayInfoLocal.offsetToUpperBounds, $"cDAC offsetToUpperBounds: {pArrayInfo->offsetToUpperBounds}, DAC: {arrayInfoLocal.offsetToUpperBounds}");
                    Debug.Assert(pArrayInfo->offsetToLowerBounds == arrayInfoLocal.offsetToLowerBounds, $"cDAC offsetToLowerBounds: {pArrayInfo->offsetToLowerBounds}, DAC: {arrayInfoLocal.offsetToLowerBounds}");
                    Debug.Assert(pArrayInfo->elementSize == arrayInfoLocal.elementSize, $"cDAC elementSize: {pArrayInfo->elementSize}, DAC: {arrayInfoLocal.elementSize}");
                }
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetBasicObjectInfo(ulong objectAddress, Interop.BOOL* pIsValidRef, uint* pObjSize, uint* pObjOffsetToVars, DebuggerIPCE_ExpandedTypeData* pObjTypeData)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetBasicObjectInfo(objectAddress, pIsValidRef, pObjSize, pObjOffsetToVars, pObjTypeData) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL isValidLocal;
            uint objSizeLocal, objOffsetLocal;
            DebuggerIPCE_ExpandedTypeData typeDataLocal;
            int hrLocal = _legacy.GetBasicObjectInfo(objectAddress, &isValidLocal, &objSizeLocal, &objOffsetLocal, &typeDataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pIsValidRef == isValidLocal, $"cDAC isValidRef: {*pIsValidRef}, DAC: {isValidLocal}");
                if (*pIsValidRef == Interop.BOOL.TRUE)
                {
                    Debug.Assert(*pObjSize == objSizeLocal, $"cDAC objSize: {*pObjSize}, DAC: {objSizeLocal}");
                    Debug.Assert(*pObjOffsetToVars == objOffsetLocal, $"cDAC objOffsetToVars: {*pObjOffsetToVars}, DAC: {objOffsetLocal}");
                    Debug.Assert(pObjTypeData->elementType == typeDataLocal.elementType,
                        $"cDAC elementType: {pObjTypeData->elementType}, DAC: {typeDataLocal.elementType}");
                }
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetDebuggerControlBlockAddress(ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetDebuggerControlBlockAddress(pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetObjectFromRefPtr(ulong ptr, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetObjectFromRefPtr(ptr, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetObject(ulong ptr, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetObject(ptr, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetVmObjectHandle(ulong handleAddress, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetVmObjectHandle(handleAddress, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.IsVmObjectHandleValid(ulong vmHandle, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsVmObjectHandleValid(vmHandle, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetHandleAddressFromVmHandle(ulong vmHandle, ulong* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetHandleAddressFromVmHandle(vmHandle, pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetThreadOwningMonitorLock(ulong vmObject, DacDbiMonitorLockInfo* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetThreadOwningMonitorLock(vmObject, pRetVal) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            DacDbiMonitorLockInfo pRetValLocal;
            int hrLocal = _legacy.GetThreadOwningMonitorLock(vmObject, &pRetValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pRetVal->lockOwner == pRetValLocal.lockOwner,
                    $"lockOwner mismatch: cDAC={pRetVal->lockOwner}, DAC={pRetValLocal.lockOwner}");
                Debug.Assert(pRetVal->acquisitionCount == pRetValLocal.acquisitionCount,
                    $"acquisitionCount mismatch: cDAC={pRetVal->acquisitionCount}, DAC={pRetValLocal.acquisitionCount}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.EnumerateMonitorEventWaitList(ulong vmObject, nint fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.EnumerateMonitorEventWaitList(vmObject, fpCallback, pUserData) : HResults.E_NOTIMPL;
        return hr;
    }

    int IDacDbiInterface.GetAttachStateFlags(int* pRetVal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetAttachStateFlags(pRetVal) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetModuleMetaDataFileInfo(ulong vmModule, uint* dwTimeStamp, uint* dwImageSize, nint pStrFilename, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        using NativeStringHolder cdacHolder = new(pStrFilename);
        int hr = _cdac is not null ? _cdac.GetModuleMetaDataFileInfo(vmModule, dwTimeStamp, dwImageSize, cdacHolder.Ptr, pResult) : HResults.E_NOTIMPL;
        string path = cdacHolder.Value ?? string.Empty;
#if DEBUG
        if (_legacy is not null)
        {
            uint timeStampLocal;
            uint imageSizeLocal;
            Interop.BOOL resultLocal;
            using var legacyHolder = new NativeStringHolder();
            int hrLocal = _legacy.GetModuleMetaDataFileInfo(vmModule, &timeStampLocal, &imageSizeLocal, legacyHolder.Ptr, &resultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pResult == resultLocal, $"GetModuleMetaDataFileInfo result mismatch - cDAC: {*pResult}, DAC: {resultLocal}");
                if (*pResult == Interop.BOOL.TRUE)
                {
                    Debug.Assert(*dwTimeStamp == timeStampLocal, $"GetModuleMetaDataFileInfo timestamp mismatch - cDAC: {*dwTimeStamp}, DAC: {timeStampLocal}");
                    Debug.Assert(*dwImageSize == imageSizeLocal, $"GetModuleMetaDataFileInfo image size mismatch - cDAC: {*dwImageSize}, DAC: {imageSizeLocal}");
                    Debug.Assert(
                        string.Equals(path, legacyHolder.Value, System.StringComparison.Ordinal),
                        $"GetModuleMetaDataFileInfo path mismatch - cDAC: '{path}', DAC: '{legacyHolder.Value}'");
                }
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.IsThreadSuspendedOrHijacked(ulong vmThread, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsThreadSuspendedOrHijacked(vmThread, pResult) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL resultLocal = Interop.BOOL.FALSE;
            Interop.BOOL* resultLocalPtr = pResult is null ? null : &resultLocal;
            int hrLocal = _legacy.IsThreadSuspendedOrHijacked(vmThread, resultLocalPtr);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == resultLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.CreateHeapWalk(nuint* pHandle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        nuint cdacHandle = 0;
        int hr = _cdac is not null ? _cdac.CreateHeapWalk(pHandle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        if (pHandle is not null)
            *pHandle = cdacHandle;
#if DEBUG
        if (_legacy is not null)
        {
            nuint legacyHandle = 0;
            int hrLocal = _legacy.CreateHeapWalk(&legacyHandle);
            Debug.ValidateHResult(hr, hrLocal, HResultValidationMode.AllowCdacSuccess);
            if (hrLocal == HResults.S_OK && hr == HResults.S_OK && pHandle is not null)
                *pHandle = (nuint)_session.RegisterHandle((ulong)cdacHandle, (ulong)legacyHandle, hasDacHandle: true);
            else if (hrLocal == HResults.S_OK)
                _legacy.DeleteHeapWalk(legacyHandle);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.DeleteHeapWalk(nuint handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle((ulong)handle);
        nuint cdacHandle = pair is not null ? (nuint)pair.CDacHandle : handle;
        int hr = _cdac is not null ? _cdac.DeleteHeapWalk(cdacHandle) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null && pair is not null && pair.HasDacHandle)
        {
            int hrLocal = _legacy.DeleteHeapWalk((nuint)pair.DacHandle);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.WalkHeap(nuint handle, uint count, COR_HEAPOBJECT* objects, uint* fetched)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.LookupHandle((ulong)handle);
        nuint cdacHandle = pair is not null ? (nuint)pair.CDacHandle : handle;
        int hr = _cdac is not null ? _cdac.WalkHeap(cdacHandle, count, objects, fetched) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null && pair is not null && pair.HasDacHandle)
        {
            COR_HEAPOBJECT[] objectsLocal = new COR_HEAPOBJECT[checked((int)count)];
            uint fetchedLocal = 0;
            int hrLocal;
            fixed (COR_HEAPOBJECT* objectsLocalPtr = objectsLocal)
            {
                hrLocal = _legacy.WalkHeap((nuint)pair.DacHandle, count, objectsLocalPtr, &fetchedLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= HResults.S_OK)
            {
                Debug.Assert(*fetched == fetchedLocal,
                    $"cDAC WalkHeap fetched {*fetched}, legacy fetched {fetchedLocal}");
                for (uint k = 0; k < fetchedLocal; k++)
                {
                    Debug.Assert(objects[k].address == objectsLocal[k].address,
                        $"cDAC[{k}].address=0x{objects[k].address:x}, legacy=0x{objectsLocal[k].address:x}");
                    Debug.Assert(objects[k].size == objectsLocal[k].size,
                        $"cDAC[{k}].size=0x{objects[k].size:x}, legacy=0x{objectsLocal[k].size:x} (addr 0x{objects[k].address:x})");
                    Debug.Assert(objects[k].type.token1 == objectsLocal[k].type.token1,
                        $"cDAC[{k}].type.token1=0x{objects[k].type.token1:x}, legacy=0x{objectsLocal[k].type.token1:x} (addr 0x{objects[k].address:x})");
                }
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.EnumerateHeapSegments(delegate* unmanaged<ulong, ulong, int, uint, nint, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        HeapSegmentEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<ulong, ulong, int, uint, nint, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordHeapSegmentAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateHeapSegments(cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null && cdacState is not null)
        {
            HeapSegmentEnumerationState legacyState = new(null, 0);
            GCHandle legacyHandle = GCHandle.Alloc(legacyState);
            try
            {
                int hrLocal = _legacy.EnumerateHeapSegments(&CollectHeapSegmentCallback, GCHandle.ToIntPtr(legacyHandle));
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK && hrLocal == HResults.S_OK && !cdacState!.Values.SequenceEqual(legacyState.Values))
                {
                    Debug.Assert(cdacState!.Values.Count == legacyState.Values.Count,
                        $"cDAC: {cdacState!.Values.Count} segments, DAC: {legacyState.Values.Count} segments");
                    int compareCount = Math.Min(cdacState!.Values.Count, legacyState.Values.Count);
                    for (int i = 0; i < compareCount; i++)
                    {
                        Debug.Assert(cdacState!.Values[i] == legacyState.Values[i],
                            $"Segment {i} mismatch - cDAC: (0x{cdacState!.Values[i].Start:x}, 0x{cdacState!.Values[i].End:x}, gen={cdacState!.Values[i].Generation}, heap={cdacState!.Values[i].Heap}), DAC: (0x{legacyState.Values[i].Start:x}, 0x{legacyState.Values[i].End:x}, gen={legacyState.Values[i].Generation}, heap={legacyState.Values[i].Heap})");
                    }
                }
            }
            finally
            {
                legacyHandle.Free();
            }
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateHeapSegments(fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.IsValidObject(ulong obj, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsValidObject(obj, pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.CreateRefWalk(nuint* pHandle, Interop.BOOL walkStacks, CorGCReferenceType handleWalkMask)
    {
        using ShimCall shimCall = ShimCall.Enter();
        nuint cdacHandle = 0;
        int hr = _cdac is not null ? _cdac.CreateRefWalk(pHandle is null ? null : &cdacHandle, walkStacks, handleWalkMask) : HResults.E_NOTIMPL;
        if (pHandle is not null)
            *pHandle = cdacHandle;
#if DEBUG
        if (_legacy is not null)
        {
            nuint legacyHandle = 0;
            int hrLocal = _legacy.CreateRefWalk(&legacyHandle, walkStacks, handleWalkMask);
            Debug.ValidateHResult(hr, hrLocal);
            if (hrLocal == HResults.S_OK && hr == HResults.S_OK && pHandle is not null)
                *pHandle = (nuint)_session.RegisterHandle((ulong)cdacHandle, (ulong)legacyHandle, hasDacHandle: true);
            else if (hrLocal == HResults.S_OK)
                _legacy.DeleteRefWalk(legacyHandle);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.DeleteRefWalk(nuint handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle((ulong)handle);
        nuint cdacHandle = pair is not null ? (nuint)pair.CDacHandle : handle;
        int hr = _cdac is not null ? _cdac.DeleteRefWalk(cdacHandle) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null && pair is not null && pair.HasDacHandle)
        {
            int hrLocal = _legacy.DeleteRefWalk((nuint)pair.DacHandle);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.WalkRefs(nuint handle, uint count, [In, MarshalUsing(CountElementName = "count"), Out] DacGcReference[] refs, uint* pFetched)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.LookupHandle((ulong)handle);
        nuint cdacHandle = pair is not null ? (nuint)pair.CDacHandle : handle;
        int hr = _cdac is not null ? _cdac.WalkRefs(cdacHandle, count, refs, pFetched) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null && pair is not null && pair.HasDacHandle && count > 0)
        {
            DacGcReference[] legacyRefs = new DacGcReference[(int)count];
            uint legacyFetched = 0;
            int hrLocal = _legacy.WalkRefs((nuint)pair.DacHandle, count, legacyRefs, &legacyFetched);
            Debug.ValidateHResult(hr, hrLocal, HResultValidationMode.AllowDivergentSuccess);
            uint cdacFetched = pFetched is null ? 0 : *pFetched;
            uint cdacHandlePrefix = CountHandlePrefix(refs, cdacFetched);
            uint legacyHandlePrefix = CountHandlePrefix(legacyRefs, legacyFetched);
            Debug.Assert(
                cdacHandlePrefix == legacyHandlePrefix,
                $"cDAC handle-prefix count {cdacHandlePrefix}, legacy {legacyHandlePrefix}");

            uint compare = Math.Min(cdacHandlePrefix, legacyHandlePrefix);
            for (uint j = 0; j < compare; j++)
            {
                Debug.Assert(refs[j].dwType == legacyRefs[j].dwType,
                    $"refs[{j}].dwType cDAC={refs[j].dwType:X}, legacy={legacyRefs[j].dwType:X}");
                Debug.Assert(refs[j].vmDomain == legacyRefs[j].vmDomain,
                    $"refs[{j}].vmDomain cDAC=0x{refs[j].vmDomain:X}, legacy=0x{legacyRefs[j].vmDomain:X}");
                Debug.Assert(refs[j].objHnd == legacyRefs[j].objHnd,
                    $"refs[{j}].objHnd cDAC=0x{refs[j].objHnd:X}, legacy=0x{legacyRefs[j].objHnd:X}");
                Debug.Assert(refs[j].i64ExtraData == legacyRefs[j].i64ExtraData,
                    $"refs[{j}].i64ExtraData cDAC=0x{refs[j].i64ExtraData:X}, legacy=0x{legacyRefs[j].i64ExtraData:X}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetTypeID(ulong obj, COR_TYPEID* pType)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetTypeID(obj, pType) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetTypeIDForType(ulong vmTypeHandle, COR_TYPEID* pId)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetTypeIDForType(vmTypeHandle, pId) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetObjectFields(ulong id, uint celt, COR_FIELD* layout, uint* pceltFetched)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetObjectFields(id, celt, layout, pceltFetched) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            uint fetchedLocal = 0;
            COR_FIELD[] localFields = new COR_FIELD[celt == 0 ? 1 : checked((int)celt)];
            fixed (COR_FIELD* localFieldsPtr = localFields)
            {
                int hrLocal = _legacy.GetObjectFields(id, celt, layout == null ? null : localFieldsPtr, &fetchedLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr >= HResults.S_OK && hrLocal >= HResults.S_OK)
                {
                    Debug.Assert(*pceltFetched == fetchedLocal, $"cDAC: {*pceltFetched}, DAC: {fetchedLocal}");
                    uint written = layout == null || pceltFetched is null ? 0 : Math.Min(celt, *pceltFetched);
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

    int IDacDbiInterface.GetTypeLayout(ulong id, COR_TYPE_LAYOUT* pLayout)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetTypeLayout(id, pLayout) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetArrayLayout(ulong id, COR_ARRAY_LAYOUT* pLayout)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetArrayLayout(id, pLayout) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetGCHeapInformation(COR_HEAPINFO* pHeapInfo)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetGCHeapInformation(pHeapInfo) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetPEFileMDInternalRW(ulong vmPEAssembly, ulong* pAddrMDInternalRW)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetPEFileMDInternalRW(vmPEAssembly, pAddrMDInternalRW) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacy is not null && LegacyFallbackHelper.CanFallback("GetPEFileMDInternalRW", "DacDbiImpl.cs"))
        {
            return _legacy.GetPEFileMDInternalRW(vmPEAssembly, pAddrMDInternalRW);
        }
        return hr;
    }

    int IDacDbiInterface.AreOptimizationsDisabled(ulong vmModule, uint methodTk, Interop.BOOL* pOptimizationsDisabled)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.AreOptimizationsDisabled(vmModule, methodTk, pOptimizationsDisabled) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL localPOptimizationsDisabled;
            int hrLocal = _legacy.AreOptimizationsDisabled(vmModule, methodTk, &localPOptimizationsDisabled);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pOptimizationsDisabled == localPOptimizationsDisabled);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetDefinesBitField(uint* pDefines)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetDefinesBitField(pDefines) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetMDStructuresVersion(uint* pMDStructuresVersion)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetMDStructuresVersion(pMDStructuresVersion) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetActiveRejitILCodeVersionNode(ulong vmModule, uint methodTk, ulong* pVmILCodeVersionNode)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetActiveRejitILCodeVersionNode(vmModule, methodTk, pVmILCodeVersionNode) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetNativeCodeVersionNode(ulong vmMethod, ulong codeStartAddress, ulong* pVmNativeCodeVersionNode)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetNativeCodeVersionNode(vmMethod, codeStartAddress, pVmNativeCodeVersionNode) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetILCodeVersionNode(ulong vmNativeCodeVersionNode, ulong* pVmILCodeVersionNode)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetILCodeVersionNode(vmNativeCodeVersionNode, pVmILCodeVersionNode) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetILCodeVersionNodeData(ulong ilCodeVersionNode, DacDbiSharedReJitInfo* pData)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetILCodeVersionNodeData(ilCodeVersionNode, pData) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            DacDbiSharedReJitInfo dataLocal = default;
            int hrLocal = _legacy.GetILCodeVersionNodeData(ilCodeVersionNode, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pData->pbIL == dataLocal.pbIL, $"cDAC: {pData->pbIL:x}, DAC: {dataLocal.pbIL:x}");
                Debug.Assert(pData->cInstrumentedMapEntries == dataLocal.cInstrumentedMapEntries, $"cDAC: {pData->cInstrumentedMapEntries:x}, DAC: {dataLocal.cInstrumentedMapEntries:x}");
                Debug.Assert(pData->rgInstrumentedMapEntries == dataLocal.rgInstrumentedMapEntries, $"cDAC: {pData->rgInstrumentedMapEntries:x}, DAC: {dataLocal.rgInstrumentedMapEntries:x}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.EnableGCNotificationEvents(Interop.BOOL fEnable)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.EnableGCNotificationEvents(fEnable) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            int hrLocal = _legacy.EnableGCNotificationEvents(fEnable);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IDacDbiInterface.IsDelegate(ulong vmObject, Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsDelegate(vmObject, pResult) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            Interop.BOOL pResultLocal;
            int hrLocal = _legacy.IsDelegate(vmObject, &pResultLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pResult == pResultLocal, $"cDAC: {*pResult}, DAC: {pResultLocal}");
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetDelegateFunctionData(ulong delegateObject, ulong* ppFunctionAssembly, uint* pMethodDef)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetDelegateFunctionData(delegateObject, ppFunctionAssembly, pMethodDef) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            ulong asmLocal;
            uint methodDefLocal;
            int hrLocal = _legacy.GetDelegateFunctionData(delegateObject, &asmLocal, &methodDefLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*ppFunctionAssembly == asmLocal, $"cDAC: {*ppFunctionAssembly:x}, DAC: {asmLocal:x}");
                Debug.Assert(*pMethodDef == methodDefLocal, $"cDAC: {*pMethodDef:x}, DAC: {methodDefLocal:x}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.GetDelegateTargetObject(ulong delegateObject, ulong* ppTargetObj)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetDelegateTargetObject(delegateObject, ppTargetObj) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            ulong targetObjLocal;
            int hrLocal = _legacy.GetDelegateTargetObject(delegateObject, &targetObjLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*ppTargetObj == targetObjLocal, $"cDAC: {*ppTargetObj:x}, DAC: {targetObjLocal:x}");
        }
#endif
        return hr;
    }

    int IDacDbiInterface.IsModuleMapped(ulong pModule, Interop.BOOL* isModuleMapped)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.IsModuleMapped(pModule, isModuleMapped) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.MetadataUpdatesApplied(Interop.BOOL* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.MetadataUpdatesApplied(pResult) : HResults.E_NOTIMPL;
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

    int IDacDbiInterface.GetAssemblyFromModule(ulong vmModule, ulong* pVmAssembly)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetAssemblyFromModule(vmModule, pVmAssembly) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            ulong assemblyLocal;
            int hrLocal = _legacy.GetAssemblyFromModule(vmModule, &assemblyLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pVmAssembly == assemblyLocal, $"cDAC: {*pVmAssembly:x}, DAC: {assemblyLocal:x}");
        }
#endif
        return hr;
    }

    int IDacDbiInterface.ParseContinuation(ulong continuationAddress, ulong* pDiagnosticIP, ulong* pNextContinuation, uint* pState)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.ParseContinuation(continuationAddress, pDiagnosticIP, pNextContinuation, pState) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacy is not null)
        {
            ulong diagnosticIPLocal;
            ulong nextLocal;
            uint stateLocal;
            int hrLocal = _legacy.ParseContinuation(continuationAddress, &diagnosticIPLocal, &nextLocal, &stateLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pDiagnosticIP == diagnosticIPLocal, $"cDAC: {*pDiagnosticIP:x}, DAC: {diagnosticIPLocal:x}");
                Debug.Assert(*pNextContinuation == nextLocal, $"cDAC: {*pNextContinuation:x}, DAC: {nextLocal:x}");
                Debug.Assert(*pState == stateLocal, $"cDAC: {*pState}, DAC: {stateLocal}");
            }
        }
#endif
        return hr;
    }

    int IDacDbiInterface.EnumerateAsyncLocals(ulong vmMethod, ulong codeAddr, uint state,
        delegate* unmanaged<AsyncLocalData*, nint, void> fpCallback, nint pUserData)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        AsyncLocalEnumerationState? cdacState = _legacy is not null && fpCallback != null ? new(fpCallback, pUserData) : null;
        GCHandle cdacHandle = default;
        delegate* unmanaged<AsyncLocalData*, nint, void> cdacCallback = fpCallback;
        nint cdacUserData = pUserData;
        if (cdacState is not null)
        {
            cdacHandle = GCHandle.Alloc(cdacState);
            cdacCallback = &RecordAsyncLocalAndForwardCallback;
            cdacUserData = GCHandle.ToIntPtr(cdacHandle);
        }
        int hr;
        try
        {
            hr = _cdac is not null ? _cdac.EnumerateAsyncLocals(vmMethod, codeAddr, state, cdacCallback, cdacUserData) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (cdacHandle.IsAllocated)
                cdacHandle.Free();
        }
        if (_legacy is not null && cdacState is not null)
        {
            AsyncLocalEnumerationState legacyState = new(null, 0);
            GCHandle legacyHandle = GCHandle.Alloc(legacyState);
            try
            {
                int hrLocal = _legacy.EnumerateAsyncLocals(vmMethod, codeAddr, state, &CollectAsyncLocalCallback, GCHandle.ToIntPtr(legacyHandle));
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    Debug.Assert(cdacState!.Values.Count == legacyState.Values.Count,
                        $"cDAC: {cdacState!.Values.Count} async locals, DAC: {legacyState.Values.Count}");
                    for (int i = 0; i < cdacState!.Values.Count; i++)
                    {
                        Debug.Assert(cdacState!.Values[i].Offset == legacyState.Values[i].Offset,
                            $"cDAC[{i}].Offset {cdacState!.Values[i].Offset} != DAC {legacyState.Values[i].Offset}");
                        Debug.Assert(cdacState!.Values[i].IlVarNum == legacyState.Values[i].IlVarNum,
                            $"cDAC[{i}].IlVarNum {cdacState!.Values[i].IlVarNum} != DAC {legacyState.Values[i].IlVarNum}");
                    }
                }
            }
            finally
            {
                legacyHandle.Free();
            }
        }
#else
        int hr = _cdac is not null ? _cdac.EnumerateAsyncLocals(vmMethod, codeAddr, state, fpCallback, pUserData) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int IDacDbiInterface.GetGenericArgTokenIndex(ulong vmMethod, uint* pIndex)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdac is not null ? _cdac.GetGenericArgTokenIndex(vmMethod, pIndex) : HResults.E_NOTIMPL;
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

    #endregion IDacDbiInterface

}
