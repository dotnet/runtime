// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
    private const uint DefaultAppDomainId = 1;

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

    public int FlushCache()
    {
        _target.Flush(FlushScope.All);
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
            *pRetVal = vmAppDomain == 0 ? 0u : DefaultAppDomainId;
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
    {
        int hr = HResults.S_OK;
        bool resolved = false;
        TargetPointer targetAssembly = TargetPointer.Null;
        uint targetTypeDef = 0;
        pTargetRefInfo->vmAssembly = 0;
        pTargetRefInfo->typeToken = 0;
        try
        {
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle referencingModule = loader.GetModuleHandleFromAssemblyPtr(pTypeRefInfo->vmAssembly);

            uint typeToken = pTypeRefInfo->typeToken;
            uint tokenType = typeToken & EcmaMetadataUtils.TokenTypeMask;

            // It's a TypeDef already
            if (tokenType == (uint)EcmaMetadataUtils.TokenType.mdtTypeDef)
            {
                targetAssembly = loader.GetAssembly(referencingModule);
                targetTypeDef = typeToken;
                resolved = true;
            }
            else if (tokenType == (uint)EcmaMetadataUtils.TokenType.mdtTypeRef)
            {
                Contracts.IEcmaMetadata ecmaMetadata = _target.Contracts.EcmaMetadata;

                // The TypeRef is already cached in the referencing module's TypeRef->MethodTable map
                Contracts.ModuleLookupTables tables = loader.GetLookupTables(referencingModule);
                TargetPointer methodTable = loader.GetModuleLookupMapElement(tables.TypeRefToMethodTable, typeToken, out _);
                if (methodTable != TargetPointer.Null)
                {
                    Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                    Contracts.TypeHandle typeHandle = rts.GetTypeHandle(methodTable);
                    TargetPointer typeDefModulePtr = rts.GetModule(typeHandle);
                    Contracts.ModuleHandle typeDefModule = loader.GetModuleHandleFromModulePtr(typeDefModulePtr);
                    targetAssembly = loader.GetAssembly(typeDefModule);
                    targetTypeDef = rts.GetTypeDefToken(typeHandle);
                    resolved = true;
                }

                // Resolve the TypeRef via metadata (resolution scope + type-forwarder chain).
                else if (EcmaMetadataUtils.TryResolveTypeRef(loader, ecmaMetadata, referencingModule, typeToken, out targetAssembly, out targetTypeDef))
                {
                    resolved = true;
                }
            }

            if (resolved)
            {
                pTargetRefInfo->vmAssembly = targetAssembly.Value;
                pTargetRefInfo->typeToken = targetTypeDef;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        if (!resolved && hr == HResults.S_OK)
        {
            hr = CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED;
        }

#if DEBUG
        if (resolved && _legacy is not null)
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

    public int GetModulePath(ulong vmModule, nint pStrFilename, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));
            string path = string.Empty;
            try
            {
                path = loader.GetPath(handle);
            }
            catch (VirtualReadException)
            {
                path = loader.GetFileName(handle);
            }

            if (string.IsNullOrEmpty(path))
            {
                path = loader.GetFileName(handle);
            }
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
    public int GetModuleForAssembly(ulong vmAssembly, ulong* pModule, Interop.BOOL* pIsModuleLoaded)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pModule == null)
                throw new ArgumentNullException(nameof(pModule));

            *pModule = 0;
            if (pIsModuleLoaded != null)
                *pIsModuleLoaded = Interop.BOOL.FALSE;

            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));
            TargetPointer modulePtr = loader.GetModule(handle);
            *pModule = modulePtr.Value;

            if (pIsModuleLoaded != null)
                *pIsModuleLoaded = loader.IsAssemblyLoaded(handle) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    public int IsManagedCode(ulong address, Interop.BOOL* pIsManaged)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pIsManaged == null)
                throw new ArgumentNullException(nameof(pIsManaged));
            *pIsManaged = Interop.BOOL.FALSE;
            IExecutionManager eman = _target.Contracts.ExecutionManager;
            if (_target.TryRead(address, out byte _) && eman.GetCodeBlockHandle(address) is not null)
            {
                *pIsManaged = Interop.BOOL.TRUE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    // EXCEPTION_MAXIMUM_PARAMETERS from the Windows SDK.
    private const int ExceptionMaximumParameters = 15;

    // Full size of a native EXCEPTION_RECORD for the target's pointer size: the header
    // (two DWORDs + two pointers + NumberParameters DWORD, aligned up to the pointer size)
    // followed by the fixed ExceptionInformation[EXCEPTION_MAXIMUM_PARAMETERS] array.
    private static int ExceptionRecordFullSize(int ptrSize)
    {
        int unaligned = sizeof(uint) + sizeof(uint) + ptrSize + ptrSize + sizeof(uint);
        int header = (unaligned + (ptrSize - 1)) & ~(ptrSize - 1);
        return header + (ExceptionMaximumParameters * ptrSize);
    }

    public int Hijack(ulong vmThread, uint dwThreadId, nint pRecord, nint pOriginalContext, uint cbSizeContext, int reason, nint pUserData, ulong* pRemoteContextAddr)
    {
        // Hijack mutates live target state (it writes to the thread's stack and sets the thread context).
        // It therefore cannot be cross-checked against the legacy implementation in DEBUG builds.
        // See https://github.com/dotnet/runtime/blob/0d1a20fb14109f277df06ebee3f83c964f9dcc61/src/coreclr/debug/daccess/dacdbiimpl.cpp#L4907 for more algorithm detail.
        int hr = HResults.S_OK;
        try
        {
            // Read the thread's current context.
            IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(_target);
            byte[] contextBuffer = new byte[ctx.Size];
            if (!_target.TryGetThreadContext(dwThreadId, ctx.AllContextFlags, contextBuffer))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            // If the caller requested it, copy back the original (pre-hijack) context.
            if (pOriginalContext != 0)
            {
                if (cbSizeContext != contextBuffer.Length)
                    throw Marshal.GetExceptionForHR(HResults.E_INVALIDARG)!;
                contextBuffer.AsSpan().CopyTo(new Span<byte>((void*)pOriginalContext, (int)cbSizeContext));
            }

            byte[]? recordBytes = null;
            if (pRecord != 0)
            {
                int recordSize = ExceptionRecordFullSize(_target.PointerSize);
                recordBytes = new byte[recordSize];
                new ReadOnlySpan<byte>((void*)pRecord, recordSize).CopyTo(recordBytes);
            }

            TargetPointer espContext = _target.Contracts.Debugger.PrepareExceptionHijack(
                contextBuffer,
                new TargetPointer(vmThread),
                recordBytes,
                reason,
                new TargetPointer((ulong)pUserData));

            if (pRemoteContextAddr is not null)
                *pRemoteContextAddr = espContext.Value;

            // Commit the modified context to the thread.
            if (!_target.TrySetThreadContext(dwThreadId, contextBuffer))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        return hr;
    }

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
    {
        int hr = HResults.S_OK;
        try
        {
            *pRetVal = default;
            CorDebugUserState partialState;
            hr = GetPartialUserState(vmThread, &partialState);
            if (hr != HResults.S_OK)
                throw Marshal.GetExceptionForHR(hr)!;

            CorDebugUserState result = partialState;

            TargetPointer threadPtr = new TargetPointer(vmThread);
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(threadPtr);

            IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);
            byte[] contextBytes = _target.Contracts.StackWalk.GetContext(threadData, ThreadContextSource.Debugger, context.FullContextFlags);
            context.FillFromBuffer(contextBytes);
            if (!_target.Contracts.ExecutionManager.IsGcSafe(context.InstructionPointer))
                result |= CorDebugUserState.USER_UNSAFE_POINT;

            *pRetVal = (int)result;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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
            TargetPointer defaultAppDomain = _target.Contracts.Loader.GetAppDomain();
            *pRetVal = defaultAppDomain;
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

    public int GetNativeCodeSequencePointsAndVarInfo(
        ulong vmMethodDesc,
        ulong startAddress,
        Interop.BOOL fCodeAvailable,
        uint* pFixedArgCount,
        delegate* unmanaged<NativeVarInfo*, void*, void> fpVarInfoCallback,
        delegate* unmanaged<DbiOffsetMapping*, void*, void> fpSeqPointCallback,
        nint pUserData)
    {
        // Fully materialize both arrays before invoking any callback to avoid delivering partial results on failure.
        List<NativeVarInfo> cdacVarInfos = new();
        List<DbiOffsetMapping> cdacSeqPoints = new();
        int hr = HResults.S_OK;
        if (pFixedArgCount != null)
            *pFixedArgCount = 0;
        try
        {
            Debug.Assert(vmMethodDesc != 0, $"vmMethodDesc is null");
            Debug.Assert(fCodeAvailable != 0, $"fCodeAvailable is false");

            Contracts.IDebugInfo debugInfo = _target.Contracts.DebugInfo;
            TargetCodePointer codePointer = new TargetCodePointer(startAddress);

            if (pFixedArgCount != null)
                *pFixedArgCount = GetArgCount(vmMethodDesc);

            bool hasDebugInfo = debugInfo.HasDebugInfo(codePointer);
            if (!hasDebugInfo && (fpVarInfoCallback != null || fpSeqPointCallback != null))
            {
                hr = HResults.E_FAIL;
            }

            if (fpVarInfoCallback != null && hasDebugInfo)
            {
                IEnumerable<DebugVarInfo> varInfos = debugInfo.GetMethodVarInfo(codePointer, out _);
                foreach (DebugVarInfo varInfo in varInfos)
                {
                    cdacVarInfos.Add(ConvertToNativeVarInfo(varInfo));
                }
            }

            if (fpSeqPointCallback != null && hasDebugInfo)
            {
                IEnumerable<Contracts.OffsetMapping> sequencePoints = debugInfo.GetMethodNativeMap(codePointer, preferUninstrumented: true, out _);
                foreach (Contracts.OffsetMapping mapping in sequencePoints)
                {
                    cdacSeqPoints.Add(ConvertToDbiOffsetMapping(mapping));
                }
            }

            foreach (NativeVarInfo nvi in cdacVarInfos)
            {
                NativeVarInfo entry = nvi;
                fpVarInfoCallback(&entry, (void*)pUserData);
            }

            foreach (DbiOffsetMapping mapping in cdacSeqPoints)
            {
                DbiOffsetMapping entry = mapping;
                fpSeqPointCallback(&entry, (void*)pUserData);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ValidateNativeCodeInfoAgainstLegacy(
                vmMethodDesc, startAddress, fCodeAvailable,
                pFixedArgCount, cdacVarInfos, cdacSeqPoints, hr,
                varInfoRequested: fpVarInfoCallback != null,
                seqPointsRequested: fpSeqPointCallback != null);
        }
#endif
        return hr;
    }

    public int GetManagedStoppedContext(ulong vmThread, ulong* pRetVal)
    {
        int hr = HResults.S_OK;
        try
        {
            *pRetVal = 0;
            Contracts.IThread threadContract = _target.Contracts.Thread;
            Contracts.ThreadData threadData = threadContract.GetThreadData(vmThread);

            if (!threadData.IsInteropDebuggingHijacked)
            {
                TargetPointer filterContext = threadData.DebuggerFilterContext;
                if (filterContext != TargetPointer.Null)
                {
                    *pRetVal = filterContext.Value;
                }
                else
                {
                    IStackWalk sw = _target.Contracts.StackWalk;
                    TargetPointer redirectedContext = sw.GetRedirectedContextPointer(threadData);
                    if (redirectedContext != TargetPointer.Null)
                    {
                        *pRetVal = redirectedContext.Value;
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
            ulong pRetValLocal;
            int hrLocal = _legacy.GetManagedStoppedContext(vmThread, &pRetValLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pRetVal == pRetValLocal, $"cDAC: {*pRetVal:x}, DAC: {pRetValLocal:x}");
        }
#endif
        return hr;
    }

    private void SeedHandleFromNativeContext(StackWalkHandleData handleData, byte* pContext, bool isFirst)
    {
        uint contextSize = IPlatformAgnosticContext.GetContextForPlatform(_target).Size;
        byte[] contextBuf = new Span<byte>(pContext, (int)contextSize).ToArray();
        handleData.Reset(contextBuf, isFirst);
    }

#if DEBUG
    private string DescribeContextDiff(ReadOnlySpan<byte> cdac, ReadOnlySpan<byte> legacy)
    {
        System.Text.StringBuilder sb = new();
        sb.Append("CONTEXT byte mismatch (cdac vs legacy):");
        int diffs = 0;
        for (int i = 0; i < cdac.Length && diffs < 16; i++)
        {
            if (cdac[i] != legacy[i])
            {
                sb.Append($" @0x{i:X3} cdac=0x{cdac[i]:X2} dac=0x{legacy[i]:X2};");
                diffs++;
            }
        }

        try
        {
            IPlatformAgnosticContext cdacCtx = IPlatformAgnosticContext.GetContextForPlatform(_target);
            IPlatformAgnosticContext dacCtx = IPlatformAgnosticContext.GetContextForPlatform(_target);
            cdacCtx.FillFromBuffer(new Span<byte>(cdac.ToArray()));
            dacCtx.FillFromBuffer(new Span<byte>(legacy.ToArray()));
            sb.Append($" | flags cdac=0x{cdacCtx.RawContextFlags:X8} dac=0x{dacCtx.RawContextFlags:X8}");
            sb.Append($" | IP   cdac=0x{cdacCtx.InstructionPointer.Value:X16} dac=0x{dacCtx.InstructionPointer.Value:X16}");
            sb.Append($" | SP   cdac=0x{cdacCtx.StackPointer.Value:X16} dac=0x{dacCtx.StackPointer.Value:X16}");
            sb.Append($" | FP   cdac=0x{cdacCtx.FramePointer.Value:X16} dac=0x{dacCtx.FramePointer.Value:X16}");
        }
        catch (System.Exception ex)
        {
            sb.Append($" | (decode failed: {ex.GetType().Name}: {ex.Message})");
        }

        return sb.ToString();
    }
#endif

    public int CreateStackWalk(ulong vmThread, byte* pInternalContextBuffer, nuint* ppSFIHandle)
    {
        if (ppSFIHandle is null)
            return HResults.E_POINTER;
        if (pInternalContextBuffer == null)
            return HResults.E_POINTER;
        *ppSFIHandle = 0;

        int hr = HResults.S_OK;
        StackWalkHandleData? handleData = null;
        try
        {
            IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(_target);
            uint allFlags = ctx.AllContextFlags;
            ThreadData threadData = _target.Contracts.Thread.GetThreadData(new TargetPointer(vmThread));
            byte[] seedContext = _target.Contracts.StackWalk.GetContext(threadData, ThreadContextSource.Debugger, allFlags);
            seedContext.AsSpan().CopyTo(new Span<byte>(pInternalContextBuffer, seedContext.Length));

            handleData = new StackWalkHandleData(_target.Contracts.StackWalk, threadData);
            SeedHandleFromNativeContext(handleData, pInternalContextBuffer, isFirst: true);
            *ppSFIHandle = handleData.GetHandle();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        // Mirror the create onto the legacy DBI
        if (_legacy is not null && LegacyFallbackHelper.CanFallback())
        {
            uint contextSize = IPlatformAgnosticContext.GetContextForPlatform(_target).Size;
            nuint legacyHandle = 0;
            byte* pLocal = (byte*)NativeMemory.AlignedAlloc(contextSize, 16);
            try
            {
                new Span<byte>(pLocal, (int)contextSize).Clear();
                int hrLocal = _legacy.CreateStackWalk(vmThread, pLocal, &legacyHandle);
                Debug.ValidateHResult(hr, hrLocal);

                if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
                {
#if DEBUG
                    ReadOnlySpan<byte> cdacBytes = new(pInternalContextBuffer, (int)contextSize);
                    ReadOnlySpan<byte> legacyBytes = new(pLocal, (int)contextSize);
                    if (!cdacBytes.SequenceEqual(legacyBytes))
                        Debug.Fail(DescribeContextDiff(cdacBytes, legacyBytes));
#endif
                    if (handleData is not null)
                        handleData.LegacyHandle = legacyHandle;
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

    public int DeleteStackWalk(nuint ppSFIHandle)
    {
        if (ppSFIHandle == 0)
            return HResults.S_OK;

        int hr = HResults.S_OK;
        nuint legacyHandle = 0;
        try
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((nint)ppSFIHandle);
            if (gcHandle.Target is not StackWalkHandleData handleData)
                throw new ArgumentException("Invalid stack walk handle", nameof(ppSFIHandle));
            legacyHandle = handleData.LegacyHandle;
            handleData.Dispose();
            gcHandle.Free();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        if (_legacy is not null && LegacyFallbackHelper.CanFallback() && legacyHandle != 0)
        {
            int hrLocal = _legacy.DeleteStackWalk(legacyHandle);
            Debug.ValidateHResult(hr, hrLocal);
        }
        return hr;
    }

    public int GetStackWalkCurrentContext(nuint pSFIHandle, byte* pContext)
    {
        if (pSFIHandle == 0)
            return HResults.E_INVALIDARG;
        if (pContext == null)
            return HResults.E_POINTER;

        int hr = HResults.S_OK;
        nuint legacyHandle = 0;
        uint contextSize = IPlatformAgnosticContext.GetContextForPlatform(_target).Size;
        try
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((nint)pSFIHandle);
            if (gcHandle.Target is not StackWalkHandleData handleData)
                throw new ArgumentException("Invalid stack walk handle", nameof(pSFIHandle));
            legacyHandle = handleData.LegacyHandle;

            if (handleData.IsValid)
            {
                IStackWalk sw = _target.Contracts.StackWalk;
                byte[] context = sw.GetRawContext(handleData.Current);

                // See https://github.com/dotnet/runtime/blob/ad50b412069ee7f274c585d191df797ac5548525/src/coreclr/debug/daccess/dacdbiimplstackwalk.cpp#L184
                RuntimeInfoArchitecture arch = _target.Contracts.RuntimeInfo.GetTargetArchitecture();
                if (arch is RuntimeInfoArchitecture.X86 or RuntimeInfoArchitecture.X64)
                {
                    IPlatformAgnosticContext stripped = IPlatformAgnosticContext.GetContextForPlatform(_target);
                    stripped.FillFromBuffer(context);
                    stripped.RawContextFlags &= ~0x40u;
                    context = stripped.GetBytes();
                }

                context.AsSpan().CopyTo(new Span<byte>(pContext, context.Length));
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null && legacyHandle != 0)
        {
            byte* pLocal = (byte*)NativeMemory.AlignedAlloc(contextSize, 16);
            try
            {
                new Span<byte>(pLocal, (int)contextSize).Clear();
                int hrLocal = _legacy.GetStackWalkCurrentContext(legacyHandle, pLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    ReadOnlySpan<byte> cdacBytes = new(pContext, (int)contextSize);
                    ReadOnlySpan<byte> legacyBytes = new(pLocal, (int)contextSize);
                    if (!cdacBytes.SequenceEqual(legacyBytes))
                        Debug.Fail(DescribeContextDiff(cdacBytes, legacyBytes));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(pLocal);
            }
        }
#endif
        return hr;
    }

    public int SetStackWalkCurrentContext(ulong vmThread, nuint pSFIHandle, int flag, byte* pContext)
    {
        if (pSFIHandle == 0)
            return HResults.E_INVALIDARG;
        if (pContext == null)
            return HResults.E_POINTER;

        int hr = HResults.S_OK;
        nuint legacyHandle = 0;
        try
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((nint)pSFIHandle);
            if (gcHandle.Target is not StackWalkHandleData handleData)
                throw new ArgumentException("Invalid stack walk handle", nameof(pSFIHandle));
            legacyHandle = handleData.LegacyHandle;

            SeedHandleFromNativeContext(handleData, pContext, isFirst: flag == (int)CorDebugSetContextFlags.SET_CONTEXT_FLAG_ACTIVE_FRAME);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        // Mirror to legacy DBI in all builds so the legacy walker tracks the cDAC walker
        if (_legacy is not null && LegacyFallbackHelper.CanFallback() && legacyHandle != 0)
        {
            int hrLocal = _legacy.SetStackWalkCurrentContext(vmThread, legacyHandle, flag, pContext);
            Debug.ValidateHResult(hr, hrLocal);
        }
        return hr;
    }

    public int UnwindStackWalkFrame(nuint pSFIHandle, Interop.BOOL* pResult)
    {
        if (pSFIHandle == 0)
            return HResults.E_INVALIDARG;
        if (pResult == null)
            return HResults.E_POINTER;

        *pResult = Interop.BOOL.FALSE;

        int hr = HResults.S_OK;
        nuint legacyHandle = 0;
        try
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((nint)pSFIHandle);
            if (gcHandle.Target is not StackWalkHandleData handleData)
                throw new ArgumentException("Invalid stack walk handle", nameof(pSFIHandle));
            legacyHandle = handleData.LegacyHandle;

            // If the walker is already invalid, treat as end-of-stack.
            if (!handleData.IsValid)
            {
                *pResult = Interop.BOOL.FALSE;
            }
            else
            {
                handleData.Advance();

                bool atEndOfStack = !handleData.IsValid;
                *pResult = atEndOfStack ? Interop.BOOL.FALSE : Interop.BOOL.TRUE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        // Mirror to legacy DBI in all builds so the legacy walker tracks the cDAC walker
        if (_legacy is not null && LegacyFallbackHelper.CanFallback() && legacyHandle != 0)
        {
            Interop.BOOL localResult;
            int hrLocal = _legacy.UnwindStackWalkFrame(legacyHandle, &localResult);
            Debug.ValidateHResult(hr, hrLocal);
#if DEBUG
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pResult == localResult, $"cDAC: {*pResult}, DAC: {localResult}");
            }
#endif
        }
        return hr;
    }

    public int CheckContext(ulong vmThread, byte* pContext)
    {
        int hr = HResults.S_OK;
        try
        {
            IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(_target);
            ctx.FillFromBuffer(new Span<byte>(pContext, (int)ctx.Size));

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
    {
        if (pSFIHandle == 0)
            return HResults.E_INVALIDARG;

        nuint legacyHandle = TryGetLegacyHandle(pSFIHandle);
        return _legacy is not null && LegacyFallbackHelper.CanFallback() && legacyHandle != 0
            ? _legacy.GetStackWalkCurrentFrameInfo(legacyHandle, pFrameData, pRetVal)
            : HResults.E_NOTIMPL;
    }

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
            TargetPointer currentAppDomain = _target.Contracts.Loader.GetAppDomain();
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

                // ctx is intentionally left as 0 (consumer does not read for cStubFrame).
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

    private static nuint TryGetLegacyHandle(nuint pSFIHandle)
    {
        try
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((nint)pSFIHandle);
            return gcHandle.Target is StackWalkHandleData handleData ? handleData.LegacyHandle : 0;
        }
        catch
        {
            return 0;
        }
    }

    public int IsLeafFrame(ulong vmThread, byte* pContext, Interop.BOOL* pResult)
    {
        *pResult = Interop.BOOL.FALSE;
        int hr = HResults.S_OK;
        try
        {
            IPlatformAgnosticContext leafCtx = IPlatformAgnosticContext.GetContextForPlatform(_target);
            uint allFlags = leafCtx.AllContextFlags;
            ThreadData threadData = _target.Contracts.Thread.GetThreadData(new TargetPointer(vmThread));
            byte[] leafContext = _target.Contracts.StackWalk.GetContext(threadData, ThreadContextSource.None, allFlags);
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
            ThreadData threadData = _target.Contracts.Thread.GetThreadData(new TargetPointer(vmThread));
            byte[] context = _target.Contracts.StackWalk.GetContext(threadData, ThreadContextSource.Debugger, allFlags);

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
    {
        int hr = HResults.S_OK;
        try
        {
            *pTargetBuffer = default;
            *pLocalSigToken = (uint)EcmaMetadataUtils.TokenType.mdtSignature;
            ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));

            MetadataReader mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)
                ?? throw new InvalidOperationException("Module has no metadata.");
            MethodDefinitionHandle mdMethodHandle = MetadataTokens.MethodDefinitionHandle((int)EcmaMetadataUtils.GetRowId(functionToken));
            MethodDefinition methodDef = mdReader.GetMethodDefinition(mdMethodHandle);

            // Reject anything whose metadata CodeType isn't IL.
            if ((methodDef.ImplAttributes & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_FUNCTION_NOT_IL)!;

            ModuleLookupTables lookupTables = loader.GetLookupTables(moduleHandle);
            TargetPointer methodDescPtr = loader.GetModuleLookupMapElement(lookupTables.MethodDefToDesc, functionToken, out _);
            if (methodDescPtr != TargetPointer.Null)
            {
                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
                if (!rts.IsIL(mdHandle))
                    throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_FUNCTION_NOT_IL)!;
            }
            else if (methodDef.RelativeVirtualAddress == 0)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_FUNCTION_NOT_IL)!;

            TargetPointer headerPtr = loader.GetILHeader(moduleHandle, functionToken);
            if (headerPtr != TargetPointer.Null)
            {
                int headerSize = HeaderReaderHelpers.GetHeaderSize(_target, headerPtr);
                int codeSize = HeaderReaderHelpers.GetCodeSize(_target, headerPtr);

                if (HeaderReaderHelpers.TryGetLocalVarSigToken(_target, headerPtr, out int localToken) && localToken != 0)
                {
                    *pLocalSigToken = (uint)localToken;
                }

                pTargetBuffer->pAddress = headerPtr.Value + (ulong)headerSize;
                pTargetBuffer->cbSize = (uint)codeSize;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    public int EnumerateClassFields(ulong thExact, nuint* pObjectSize, delegate* unmanaged<FieldData*, void*, void> fpCallback, nint pUserData)
    {
        if (pObjectSize != null)
            *pObjectSize = 0;

        nuint cdacObjectSize = 0;
        List<FieldData>? cdacFields = null;
#if DEBUG
        if (_legacy is not null)
            cdacFields = new();
#endif
        int hr = HResults.S_OK;
        try
        {
            if (fpCallback == null)
                throw new ArgumentNullException(nameof(fpCallback));

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle thExactHandle = rts.GetTypeHandle(thExact);
            // Native semantics: thApprox is the same TypeHandle that was passed in.
            if (thExactHandle.IsNull)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;

            TypeHandle thApprox = thExactHandle;

            // For Generic classes the object size only comes through with an instantiated type.
            cdacObjectSize = 0;
            if (rts.GetInstantiation(thApprox).Length == 0)
            {
                cdacObjectSize = rts.GetNumInstanceFieldBytes(thApprox);
            }

            CollectFieldsForDbi(rts, thExactHandle, thApprox, fpCallback, pUserData, cdacFields);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        if (hr == HResults.S_OK && pObjectSize != null)
            *pObjectSize = cdacObjectSize;

#if DEBUG
        if (_legacy is not null)
        {
            ValidateEnumerateFieldsAgainstLegacy(
                nameof(IDacDbiInterface.EnumerateClassFields),
                cdacObjectSize,
                cdacFields,
                hr,
                (pSize, pUser) => _legacy!.EnumerateClassFields(thExact, pSize, (delegate* unmanaged<FieldData*, void*, void>)&CollectFieldDataCallback, pUser));
        }
#endif
        return hr;
    }

    public int EnumerateInstantiationFields(ulong vmAssembly, ulong vmThExact, ulong vmThApprox, nuint* pObjectSize, delegate* unmanaged<FieldData*, void*, void> fpCallback, nint pUserData)
    {
        if (pObjectSize != null)
            *pObjectSize = 0;

        nuint cdacObjectSize = 0;
        List<FieldData>? cdacFields = null;
#if DEBUG
        if (_legacy is not null)
            cdacFields = new();
#endif
        int hr = HResults.S_OK;
        try
        {
            if (fpCallback == null)
                throw new ArgumentNullException(nameof(fpCallback));

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle thExactHandle = rts.GetTypeHandle(vmThExact);
            TypeHandle thApproxHandle = rts.GetTypeHandle(vmThApprox);
            if (thApproxHandle.IsNull)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;

            cdacObjectSize = rts.GetNumInstanceFieldBytes(thApproxHandle);

            CollectFieldsForDbi(rts, thExactHandle, thApproxHandle, fpCallback, pUserData, cdacFields);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        if (hr == HResults.S_OK && pObjectSize != null)
            *pObjectSize = cdacObjectSize;

#if DEBUG
        if (_legacy is not null)
        {
            ValidateEnumerateFieldsAgainstLegacy(
                nameof(IDacDbiInterface.EnumerateInstantiationFields),
                cdacObjectSize,
                cdacFields,
                hr,
                (pSize, pUser) => _legacy!.EnumerateInstantiationFields(vmAssembly, vmThExact, vmThApprox, pSize, (delegate* unmanaged<FieldData*, void*, void>)&CollectFieldDataCallback, pUser));
        }
#endif
        return hr;
    }

    // Mirrors native DacDbiInterfaceImpl::CollectFields. Iterates the regular FieldDescs first
    // then EnC-added instance fields, then EnC-added static fields.
    private void CollectFieldsForDbi(
        IRuntimeTypeSystem rts,
        TypeHandle thExact,
        TypeHandle thApprox,
        delegate* unmanaged<FieldData*, void*, void> fpCallback,
        nint pUserData,
        List<FieldData>? cdacFields)
    {
        TargetPointer gcStaticsBase = TargetPointer.Null;
        TargetPointer nonGCStaticsBase = TargetPointer.Null;
        if (!thExact.IsNull && !rts.IsCollectible(thExact))
        {
            gcStaticsBase = rts.GetGCStaticsBasePointer(thExact);
            nonGCStaticsBase = rts.GetNonGCStaticsBasePointer(thExact);
        }

        IRuntimeMutableTypeSystem? mts = _target.Contracts.TryGetContract<IRuntimeMutableTypeSystem>(out IRuntimeMutableTypeSystem enc) ? enc : null;

        foreach (TargetPointer fdPtr in rts.GetFieldDescList(thApprox))
            EmitFieldData(rts, mts, fdPtr, gcStaticsBase, nonGCStaticsBase, fpCallback, pUserData, cdacFields);

        if (mts is not null)
        {
            foreach (TargetPointer fdPtr in mts.EnumerateAddedFieldDescs(thApprox, staticFields: false))
                EmitFieldData(rts, mts, fdPtr, gcStaticsBase, nonGCStaticsBase, fpCallback, pUserData, cdacFields);
            foreach (TargetPointer fdPtr in mts.EnumerateAddedFieldDescs(thApprox, staticFields: true))
                EmitFieldData(rts, mts, fdPtr, gcStaticsBase, nonGCStaticsBase, fpCallback, pUserData, cdacFields);
        }
    }

    // Mirrors native DacDbiInterfaceImpl::ComputeFieldData for one FieldDesc and then invokes the
    // user callback.
    private static void EmitFieldData(
        IRuntimeTypeSystem rts,
        IRuntimeMutableTypeSystem? mts,
        TargetPointer fdPtr,
        TargetPointer gcStaticsBase,
        TargetPointer nonGCStaticsBase,
        delegate* unmanaged<FieldData*, void*, void> fpCallback,
        nint pUserData,
        List<FieldData>? cdacFields)
    {
        bool isStatic = rts.IsFieldDescStatic(fdPtr);
        CorElementType type = rts.GetFieldDescType(fdPtr);
        bool isPrimitive = IsPrimitiveType(type);

        FieldData fd = default;
        fd.m_fldMetadataToken = rts.GetFieldDescMemberDef(fdPtr);
        fd.m_fFldIsStatic = isStatic ? (byte)1 : (byte)0;
        fd.m_fFldIsPrimitive = isPrimitive ? (byte)1 : (byte)0;
        fd.m_fldSignatureCache = 0;
        fd.m_fldSignatureCacheSize = 0;
        fd.m_vmFieldDesc = fdPtr.Value;

        bool isEnCNew = mts?.IsFieldDescEnCNew(fdPtr) ?? false;
        if (isEnCNew)
        {
            // Mirrors native: storage not yet available; carry the FieldDesc pointer through
            // m_vmFieldDesc and clear the address-related flags (TLS/RVA/collectible).
            fd.m_fFldStorageAvailable = Interop.BOOL.FALSE;
            fd.m_fFldIsTLS = 0;
            fd.m_fFldIsRVA = 0;
            fd.m_fFldIsCollectibleStatic = 0;
        }
        if (!isEnCNew)
        {
            fd.m_fFldStorageAvailable = Interop.BOOL.TRUE;
            bool isTLS = rts.IsFieldDescThreadStatic(fdPtr);
            bool isRVA = rts.IsFieldDescRVA(fdPtr);
            bool isCollectibleStatic = false;
            if (isStatic)
            {
                TargetPointer enclosingMT = rts.GetMTOfEnclosingClass(fdPtr);
                if (enclosingMT != TargetPointer.Null)
                {
                    TypeHandle enclosingTh = rts.GetTypeHandle(enclosingMT);
                    isCollectibleStatic = rts.IsCollectible(enclosingTh);
                }
            }

            fd.m_fFldIsTLS = isTLS ? (byte)1 : (byte)0;
            fd.m_fFldIsRVA = isRVA ? (byte)1 : (byte)0;
            fd.m_fFldIsCollectibleStatic = isCollectibleStatic ? (byte)1 : (byte)0;

            if (isStatic)
            {
                if (isRVA)
                {
                    TargetPointer addr = rts.GetFieldDescStaticAddress(fdPtr, unboxValueTypes: false);
                    fd.m_pFldStaticAddress = addr.Value;
                }
                else if (!isTLS && !isCollectibleStatic)
                {
                    TargetPointer baseAddr = isPrimitive ? nonGCStaticsBase : gcStaticsBase;
                    if (baseAddr != TargetPointer.Null)
                    {
                        uint offset = rts.GetFieldDescOffset(fdPtr, null);
                        fd.m_pFldStaticAddress = baseAddr + offset;
                    }
                }
            }
            else
            {
                // instance: store the offset
                uint offset = rts.GetFieldDescOffset(fdPtr, null);
                fd.m_fldInstanceOffset = offset;
            }
        }

#if DEBUG
        cdacFields?.Add(fd);
#endif
        fpCallback(&fd, (void*)pUserData);
    }

    private static bool IsPrimitiveType(CorElementType type)
    {
        return (type < CorElementType.Ptr)
            || type == CorElementType.I
            || type == CorElementType.U;
    }

#if DEBUG
    [UnmanagedCallersOnly]
    private static void CollectFieldDataCallback(FieldData* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        ((List<FieldData>)handle.Target!).Add(*data);
    }

    private delegate int LegacyEnumerateFieldsFn(nuint* pObjectSize, nint pUserData);

    private static void ValidateEnumerateFieldsAgainstLegacy(string label, nuint cdacObjectSize, List<FieldData>? cdacFields, int hr, LegacyEnumerateFieldsFn legacyEnumerate)
    {
        List<FieldData> dacFields = new();
        GCHandle dacHandle = GCHandle.Alloc(dacFields);
        nuint dacObjectSize = 0;
        int hrLocal = legacyEnumerate(&dacObjectSize, GCHandle.ToIntPtr(dacHandle));
        dacHandle.Free();
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
#endif

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

    public int GetApproxTypeHandle(TypeInfoList* pTypeData, ulong* pRetVal)
    {
        if (pTypeData == null || pRetVal == null)
            return HResults.E_POINTER;
        *pRetVal = 0;
        int hr = HResults.S_OK;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            TargetPointer canonMtPtr = rts.GetWellKnownMethodTable(WellKnownMethodTable.Canon);
            if (canonMtPtr == TargetPointer.Null)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
            TypeHandle canonTh = rts.GetTypeHandle(canonMtPtr);

            TypeDataWalk walk = new TypeDataWalk(_target, rts, canonTh, pTypeData->m_pList, (uint)pTypeData->m_nEntries);
            TypeHandle th = walk.ReadLoadedTypeHandle();
            if (th.IsNull)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
            *pRetVal = th.Address.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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


    public int GetExactTypeHandle(DebuggerIPCE_ExpandedTypeData* pTypeData, ArgInfoList* pArgInfo, ulong* pVmTypeHandle)
    {
        if (pVmTypeHandle == null || pTypeData == null || pArgInfo == null)
            return HResults.E_POINTER;
        *pVmTypeHandle = 0;
        int hr = HResults.S_OK;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle th = default;
            CorElementType et = (CorElementType)ReadLittleEndian(pTypeData->elementType);
            switch (et)
            {
                case CorElementType.Array:
                case CorElementType.SzArray:
                    th = GetExactArrayTypeHandle(rts, pTypeData, pArgInfo);
                    break;
                case CorElementType.Ptr:
                case CorElementType.Byref:
                    th = GetExactPtrOrByRefTypeHandle(rts, pTypeData, pArgInfo);
                    break;
                case CorElementType.Class:
                case CorElementType.ValueType:
                    th = GetExactClassTypeHandle(rts, pTypeData, pArgInfo);
                    break;
                case CorElementType.FnPtr:
                    th = GetExactFnPtrTypeHandle(rts, pArgInfo);
                    break;
                default:
                    th = rts.GetPrimitiveType(et);
                    break;
            }
            if (th.Address == TargetPointer.Null)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
            *pVmTypeHandle = th.Address.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    private TypeHandle BasicTypeInfoToTypeHandle(IRuntimeTypeSystem rts, DebuggerIPCE_BasicTypeData* pData)
    {
        CorElementType et = (CorElementType)ReadLittleEndian(pData->elementType);
        TypeHandle th;
        switch (et)
        {
            case CorElementType.Array:
            case CorElementType.SzArray:
            case CorElementType.Ptr:
            case CorElementType.Byref:
            case CorElementType.FnPtr:
                ulong vmTh = ReadLittleEndian(pData->vmTypeHandle);
                if (vmTh == 0)
                    throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
                th = rts.GetTypeHandle(new TargetPointer(vmTh));
                break;
            case CorElementType.Class:
            case CorElementType.ValueType:
                th = GetClassOrValueTypeHandle(rts, pData);
                break;
            default:
                th = rts.GetPrimitiveType(et);
                break;
        }
        if (th.Address == TargetPointer.Null)
            throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
        return th;
    }

    private TypeHandle GetClassOrValueTypeHandle(IRuntimeTypeSystem rts, DebuggerIPCE_BasicTypeData* pData)
    {
        ulong vmTh = ReadLittleEndian(pData->vmTypeHandle);
        if (vmTh != 0)
            return rts.GetTypeHandle(new TargetPointer(vmTh));

        ulong vmAssembly = ReadLittleEndian(pData->vmAssembly);
        uint metadataToken = ReadLittleEndian(pData->metadataToken);
        return LookupTypeDefOrRefInAssembly(vmAssembly, metadataToken);
    }

    private TypeHandle GetExactArrayTypeHandle(IRuntimeTypeSystem rts, DebuggerIPCE_ExpandedTypeData* pTopLevel, ArgInfoList* pArgInfo)
    {
        if (pArgInfo->m_nEntries != 1)
            throw new ArgumentException($"Array type with arg count: {pArgInfo->m_nEntries}");
        TypeHandle elementType = BasicTypeInfoToTypeHandle(rts, &pArgInfo->m_pList[0]);
        CorElementType et = (CorElementType)ReadLittleEndian(pTopLevel->elementType);
        int rank = (int)ReadLittleEndian(pTopLevel->ArrayTypeData_arrayRank);
        return rts.GetConstructedType(elementType, et, rank, ImmutableArray<TypeHandle>.Empty);
    }

    private TypeHandle GetExactPtrOrByRefTypeHandle(IRuntimeTypeSystem rts, DebuggerIPCE_ExpandedTypeData* pTopLevel, ArgInfoList* pArgInfo)
    {
        if (pArgInfo->m_nEntries != 1)
            throw new ArgumentException($"Pointer or byref type with arg count: {pArgInfo->m_nEntries}");
        TypeHandle referent = BasicTypeInfoToTypeHandle(rts, &pArgInfo->m_pList[0]);
        CorElementType et = (CorElementType)ReadLittleEndian(pTopLevel->elementType);
        return rts.GetConstructedType(referent, et, 0, ImmutableArray<TypeHandle>.Empty);
    }

    private TypeHandle GetExactClassTypeHandle(IRuntimeTypeSystem rts, DebuggerIPCE_ExpandedTypeData* pTopLevel, ArgInfoList* pArgInfo)
    {
        ulong vmAssembly = ReadLittleEndian(pTopLevel->ClassTypeData_vmAssembly);
        uint metadataToken = ReadLittleEndian(pTopLevel->ClassTypeData_metadataToken);
        TypeHandle typeConstructor = LookupTypeDefOrRefInAssembly(vmAssembly, metadataToken);

        int argCount = pArgInfo->m_nEntries;
        if (argCount == 0)
            return typeConstructor;

        ImmutableArray<TypeHandle>.Builder builder = ImmutableArray.CreateBuilder<TypeHandle>(argCount);
        for (int i = 0; i < argCount; i++)
            builder.Add(BasicTypeInfoToTypeHandle(rts, &pArgInfo->m_pList[i]));

        return rts.GetConstructedType(typeConstructor, CorElementType.GenericInst, 0, builder.MoveToImmutable());
    }

    private TypeHandle GetExactFnPtrTypeHandle(IRuntimeTypeSystem rts, ArgInfoList* pArgInfo)
    {
        int argCount = pArgInfo->m_nEntries;
        ImmutableArray<TypeHandle>.Builder builder = ImmutableArray.CreateBuilder<TypeHandle>(argCount);
        for (int i = 0; i < argCount; i++)
            builder.Add(BasicTypeInfoToTypeHandle(rts, &pArgInfo->m_pList[i]));

        // Non-default calling conventions are not supported.
        // Currently passes callConv=0 to match native DAC.
        return rts.GetConstructedType(default, CorElementType.FnPtr, 0, builder.MoveToImmutable());
    }

    public int EnumerateMethodDescParams(ulong vmMethodDesc, ulong genericsToken, uint* pcGenericClassTypeParams,
        delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> fpCallback, nint pUserData)
    {
        int hr = HResults.S_OK;
#if DEBUG
        List<DebuggerIPCE_ExpandedTypeData> entries = new();
#endif
        uint cClassParams = 0;
        try
        {
            if (vmMethodDesc == 0)
                throw new ArgumentException("MethodDesc cannot be null", nameof(vmMethodDesc));
            if (pcGenericClassTypeParams == null)
                throw new ArgumentNullException(nameof(pcGenericClassTypeParams));
            if (fpCallback == null)
                throw new ArgumentNullException(nameof(fpCallback));

            *pcGenericClassTypeParams = 0;
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            Contracts.MethodDescHandle pRepMethod = rts.GetMethodDescHandle(vmMethodDesc);
            TypeHandle thRepMt = rts.GetTypeHandle(rts.GetMethodTable(pRepMethod));

            // Try to resolve exact instantiations using the generics token. Fall back
            // to canonical when the token is unavailable, the method isn't shared, or any
            // resolution step fails (analogous to native's SanityCheck path).
            Contracts.MethodDescHandle pSpecificMethod = pRepMethod;
            TypeHandle thSpecificClass = thRepMt;
            bool isExact = false;

            GenericContextLoc ctxLoc = rts.GetGenericContextLoc(pRepMethod);
            if (ctxLoc == GenericContextLoc.None)
            {
                isExact = true;
            }
            else if (genericsToken != 0)
            {
                try
                {
                    if (ctxLoc == GenericContextLoc.InstArgMethodDesc)
                    {
                        // RequiresInstMethodDescArg: token is a MethodDesc*.
                        pSpecificMethod = rts.GetMethodDescHandle(new TargetPointer(genericsToken));
                        thSpecificClass = rts.GetTypeHandle(rts.GetMethodTable(pSpecificMethod));
                        isExact = true;
                    }
                    else if (ctxLoc == GenericContextLoc.InstArgMethodTable)
                    {
                        // RequiresInstMethodTableArg: token is a MethodTable*.
                        thSpecificClass = rts.GetTypeHandle(new TargetPointer(genericsToken));
                        isExact = true;
                    }
                    else
                    {
                        // AcquiresInstMethodTableFromThis: token is some MethodTable*; it may be a
                        // subclass, so walk the parent chain to find the exact declaring class.
                        TypeHandle thFromThis = rts.GetTypeHandle(new TargetPointer(genericsToken));
                        TypeHandle thMatch = GetMethodTableMatchingParentClass(rts, thFromThis, thRepMt);
                        if (!thMatch.IsNull)
                        {
                            thSpecificClass = thMatch;
                            isExact = true;
                        }
                    }
                }
                catch (VirtualReadException)
                {
                    // Any failure resolving the exact token: fall back to canonical.
                    isExact = false;
                }
            }

            if (!isExact)
            {
                pSpecificMethod = pRepMethod;
                thSpecificClass = thRepMt;
            }

            // Project the specific class onto the method's declaring class to get the class instantiation.
            TargetPointer specMethodMtPtr = rts.GetMethodTable(pSpecificMethod);
            TypeHandle thSpecMethodMt = rts.GetTypeHandle(specMethodMtPtr);
            TypeHandle thMatchingParent = GetMethodTableMatchingParentClass(rts, thSpecificClass, thSpecMethodMt);
            ReadOnlySpan<TypeHandle> classInst = thMatchingParent.IsNull
                ? ReadOnlySpan<TypeHandle>.Empty
                : rts.GetInstantiation(thMatchingParent);
            ReadOnlySpan<TypeHandle> methodInst = rts.GetGenericMethodInstantiation(pSpecificMethod);

            cClassParams = (uint)classInst.Length;
            *pcGenericClassTypeParams = cClassParams;

            // Resolve the System.__Canon TypeHandle for per-parameter fallback.
            TargetPointer canonMtPtr = rts.GetWellKnownMethodTable(WellKnownMethodTable.Canon);
            TypeHandle thCanon = rts.GetTypeHandle(canonMtPtr);

            DebuggerIPCE_ExpandedTypeData entry;
            for (int i = 0; i < classInst.Length; i++)
            {
                FillExpandedTypeDataWithCanonFallback(rts, classInst[i], thCanon, &entry);
#if DEBUG
                entries.Add(entry);
#endif
                fpCallback(&entry, pUserData);
            }
            for (int i = 0; i < methodInst.Length; i++)
            {
                FillExpandedTypeDataWithCanonFallback(rts, methodInst[i], thCanon, &entry);
#if DEBUG
                entries.Add(entry);
#endif
                fpCallback(&entry, pUserData);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            DebugExpandedTypeInfo.Clear();
            uint cClassParamsLocal = 0;
            delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> debugCallbackPtr = &EnumExpandedTypeInfoCallback;
            int hrLocal = _legacy.EnumerateMethodDescParams(vmMethodDesc, genericsToken, &cClassParamsLocal, debugCallbackPtr, 0);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(cClassParams == cClassParamsLocal,
                    $"cDAC class params: {cClassParams}, DAC: {cClassParamsLocal}");

                List<DebuggerIPCE_ExpandedTypeData> legacyEntries = DebugExpandedTypeInfo;
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
            DebugExpandedTypeInfo.Clear();
        }
#endif
        return hr;
    }

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

    public int GetEnCHangingFieldInfo(EnCHangingFieldInfo* pEnCFieldInfo, FieldData* pFieldData)
    {
        int hr = HResults.S_OK;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            IRuntimeMutableTypeSystem mrts = _target.Contracts.RuntimeMutableTypeSystem;
            Contracts.ILoader loader = _target.Contracts.Loader;

            // Resolve the assembly and module
            ulong vmAssembly = ReadLittleEndian(pEnCFieldInfo->objectTypeData.vmAssembly);
            uint metadataToken = ReadLittleEndian(pEnCFieldInfo->objectTypeData.metadataToken);
            uint fldToken = pEnCFieldInfo->fldToken;

            _ = LookupTypeDefOrRefInAssembly(vmAssembly, metadataToken);
            Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));
            Contracts.ModuleLookupTables lookupTables = loader.GetLookupTables(moduleHandle);
            TargetPointer fieldDescPointer = loader.GetModuleLookupMapElement(lookupTables.FieldDefToDesc, fldToken, out _);
            if (fieldDescPointer == TargetPointer.Null || mrts.DoesEnCFieldDescNeedFixup(fieldDescPointer))
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_ENC_HANGING_FIELD)!;

            bool isStatic = rts.IsFieldDescStatic(fieldDescPointer);
            TargetPointer fieldAddress;

            if (isStatic)
            {
                fieldAddress = mrts.GetEnCStaticFieldDataAddress(fieldDescPointer);
            }
            else
            {
                TargetPointer objectPointer = new TargetPointer(pEnCFieldInfo->vmObject);
                fieldAddress = mrts.GetEnCInstanceFieldAddress(objectPointer, fieldDescPointer);
            }

            if (fieldAddress == TargetPointer.Null)
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_ENC_HANGING_FIELD)!;

            // Fill the FieldData output struct
            *pFieldData = default;
            pFieldData->m_fldMetadataToken = fldToken;
            pFieldData->m_fFldStorageAvailable = Interop.BOOL.TRUE;
            pFieldData->m_fFldIsStatic = isStatic ? (byte)1 : (byte)0;
            pFieldData->m_vmFieldDesc = fieldDescPointer.Value;

            if (isStatic)
            {
                pFieldData->m_pFldStaticAddress = fieldAddress.Value;
            }
            else
            {
                // Instance offset is: fieldAddress - (objectAddress + offsetToVars)
                ulong objectAddr = pEnCFieldInfo->vmObject;
                ulong offsetToVars = pEnCFieldInfo->offsetToVars;
                pFieldData->m_fldInstanceOffset = fieldAddress.Value - (objectAddr + offsetToVars);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    internal TypeHandle LookupTypeDefOrRefInAssembly(ulong vmAssembly, uint metadataToken)
    {
        TypeHandle th = TryLookupTypeDefOrRefInAssembly(vmAssembly, metadataToken);
        if (th.IsNull)
            throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED)!;
        return th;
    }

    internal TypeHandle TryLookupTypeDefOrRefInAssembly(ulong vmAssembly, uint metadataToken)
    {
        ILoader loader = _target.Contracts.Loader;
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));
        ModuleLookupTables lookupTables = loader.GetLookupTables(moduleHandle);
        TargetPointer mt;
        switch ((EcmaMetadataUtils.TokenType)(metadataToken & EcmaMetadataUtils.TokenTypeMask))
        {
            case EcmaMetadataUtils.TokenType.mdtTypeDef:
                mt = loader.GetModuleLookupMapElement(lookupTables.TypeDefToMethodTable, metadataToken, out _);
                break;
            case EcmaMetadataUtils.TokenType.mdtTypeRef:
                mt = loader.GetModuleLookupMapElement(lookupTables.TypeRefToMethodTable, metadataToken, out _);
                break;
            default:
                return default;
        }
        if (mt == TargetPointer.Null)
            return default;
        return rts.GetTypeHandle(mt);
    }

    public int EnumerateTypeHandleParams(ulong vmTypeHandle,
        delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> fpCallback, nint pUserData)
    {
        int hr = HResults.S_OK;
#if DEBUG
        List<DebuggerIPCE_ExpandedTypeData> entries = new();
#endif
        try
        {
            if (fpCallback == null)
                throw new ArgumentNullException(nameof(fpCallback));

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle typeHandle = rts.GetTypeHandle(new TargetPointer(vmTypeHandle));
            ReadOnlySpan<TypeHandle> instantiation = rts.GetInstantiation(typeHandle);

            DebuggerIPCE_ExpandedTypeData entry;
            for (int i = 0; i < instantiation.Length; i++)
            {
                TypeHandleToExpandedTypeInfoImpl(rts, AreValueTypesBoxed.NoValueTypeBoxing, instantiation[i], &entry);
                fpCallback(&entry, pUserData);
#if DEBUG
                entries.Add(entry);
#endif
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            DebugExpandedTypeInfo.Clear();
            delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> debugCallbackPtr = &EnumExpandedTypeInfoCallback;
            int hrLocal = _legacy.EnumerateTypeHandleParams(vmTypeHandle, debugCallbackPtr, 0);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                List<DebuggerIPCE_ExpandedTypeData> legacyEntries = DebugExpandedTypeInfo;
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
            DebugExpandedTypeInfo.Clear();
        }
#endif
        return hr;
    }

#if DEBUG
    [ThreadStatic]
    private static List<DebuggerIPCE_ExpandedTypeData>? _debugExpandedTypeInfo;

    private static List<DebuggerIPCE_ExpandedTypeData> DebugExpandedTypeInfo
        => _debugExpandedTypeInfo ??= new();

    [UnmanagedCallersOnly]
    private static void EnumExpandedTypeInfoCallback(DebuggerIPCE_ExpandedTypeData* pTypeData, nint _)
    {
        DebugExpandedTypeInfo.Add(*pTypeData);
    }

    private static string FormatExpandedTypeData(DebuggerIPCE_ExpandedTypeData e) =>
        $"elementType={e.elementType}, " +
        $"token=0x{e.ClassTypeData_metadataToken:x}, " +
        $"vmAssembly=0x{e.ClassTypeData_vmAssembly:x}, " +
        $"vmTypeHandle=0x{e.ClassTypeData_typeHandle:x}";
#endif

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
            TargetPointer exceptionMT = rts.GetWellKnownMethodTable(WellKnownMethodTable.Exception);

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

#if DEBUG
    [ThreadStatic]
    private static List<(ulong VmAppDomain, ulong VmAssembly, ulong Ip, uint MethodDef, Interop.BOOL IsLastForeignExceptionFrame)>? _debugEnumerateStackFramesFromException;

    private static List<(ulong VmAppDomain, ulong VmAssembly, ulong Ip, uint MethodDef, Interop.BOOL IsLastForeignExceptionFrame)> DebugEnumerateStackFramesFromException
        => _debugEnumerateStackFramesFromException ??= new();

    [UnmanagedCallersOnly]
    private static void EnumerateStackFramesFromExceptionDebugCallback(ulong vmAppDomain, ulong vmAssembly, ulong ip, uint methodDef, Interop.BOOL isLastForeignExceptionFrame, nint _)
    {
        DebugEnumerateStackFramesFromException.Add((vmAppDomain, vmAssembly, ip, methodDef, isLastForeignExceptionFrame));
    }
#endif

    public int EnumerateStackFramesFromException(ulong vmObject, delegate* unmanaged<ulong, ulong, ulong, uint, Interop.BOOL, nint, void> fpCallback, nint pUserData)
    {
        int hr = HResults.S_OK;
#if DEBUG
        List<(ulong VmAppDomain, ulong VmAssembly, ulong Ip, uint MethodDef, Interop.BOOL IsLastForeignExceptionFrame)> frames = new();
#endif
        try
        {
            if (fpCallback is null)
                throw new ArgumentNullException(nameof(fpCallback));

            ulong vmAppDomain = _target.Contracts.Loader.GetAppDomain().Value;

            IException exceptionContract = _target.Contracts.Exception;
            foreach (ExceptionStackFrameInfo frame in exceptionContract.GetExceptionStackFrames(new TargetPointer(vmObject)))
            {
                ResolveStubFrameAssemblyAndToken(frame.MethodDesc, out TargetPointer vmAssembly, out uint methodDef);
                Interop.BOOL isLastForeign = frame.IsLastForeignExceptionFrame ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
#if DEBUG
                frames.Add((vmAppDomain, vmAssembly.Value, frame.Ip.Value, methodDef, isLastForeign));
#endif
                fpCallback(vmAppDomain, vmAssembly.Value, frame.Ip.Value, methodDef, isLastForeign, pUserData);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacy is not null)
        {
            DebugEnumerateStackFramesFromException.Clear();
            delegate* unmanaged<ulong, ulong, ulong, uint, Interop.BOOL, nint, void> debugCallbackPtr = &EnumerateStackFramesFromExceptionDebugCallback;
            int hrLocal = _legacy.EnumerateStackFramesFromException(vmObject, debugCallbackPtr, 0);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                List<(ulong VmAppDomain, ulong VmAssembly, ulong Ip, uint MethodDef, Interop.BOOL IsLastForeignExceptionFrame)> legacyFrames = DebugEnumerateStackFramesFromException;
                static string FormatFrame((ulong VmAppDomain, ulong VmAssembly, ulong Ip, uint MethodDef, Interop.BOOL IsLastForeignExceptionFrame) f)
                    => $"(AppDomain=0x{f.VmAppDomain:x}, Assembly=0x{f.VmAssembly:x}, Ip=0x{f.Ip:x}, MethodDef=0x{f.MethodDef:x}, IsLastForeignExceptionFrame={f.IsLastForeignExceptionFrame})";
                Debug.Assert(frames.SequenceEqual(legacyFrames),
                    $"Exception stack frame enumeration mismatch - "
                    + $"cDAC: [{string.Join(",", frames.Select(FormatFrame))}], "
                    + $"DAC: [{string.Join(",", legacyFrames.Select(FormatFrame))}]");
            }
            DebugEnumerateStackFramesFromException.Clear();
        }
#endif
        return hr;
    }

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

    public int GetTypedByRefInfo(ulong pTypedByRef, ulong* pObjRef, DebuggerIPCE_BasicTypeData* pTypedByRefType)
    {
        *pObjRef = 0;
        *pTypedByRefType = default;
        int hr = HResults.S_OK;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypedByRefInfo info = rts.GetTypedByRefInfo(pTypedByRef);
            TypeHandle th = rts.GetTypeHandle(info.TypeHandle);
            FillBasicTypeInfo(rts, th, out DebuggerIPCE_BasicTypeData typeData);
            *pTypedByRefType = typeData;
            *pObjRef = info.Data.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    public int GetStringData(ulong objectAddress, uint* pLength, uint* pOffsetToStringBase)
    {
        int hr = HResults.S_OK;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TargetPointer mtAddr = _target.Contracts.Object.GetMethodTableAddress(objectAddress);
            TypeHandle th = rts.GetTypeHandle(mtAddr);
            if (!rts.IsString(th))
            {
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_TARGET_INCONSISTENT)!;
            }
            _target.Contracts.Object.GetStringData(objectAddress, out uint length, out uint offset);
            *pLength = length;
            *pOffsetToStringBase = offset;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    public int GetArrayData(ulong objectAddress, Interop.BOOL* pIsValidArray, DacDbiArrayInfo* pArrayInfo)
    {
        *pIsValidArray = Interop.BOOL.FALSE;
        *pArrayInfo = default;
        int hr = HResults.S_OK;
        try
        {
            IObject objectContract = _target.Contracts.Object;
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TargetPointer mt = objectContract.GetMethodTableAddress(objectAddress);
            TypeHandle th = rts.GetTypeHandle(mt);
            if (rts.IsArray(th, out uint rank))
            {
                TargetPointer dataStart = objectContract.GetArrayData(objectAddress, out uint numComponents, out TargetPointer boundsStart, out TargetPointer lowerBounds);
                *pIsValidArray = Interop.BOOL.TRUE;

                uint offsetToArrayBase = (uint)(dataStart - objectAddress);
                uint offsetToUpperBounds = 0;
                uint offsetToLowerBounds = 0;
                if (rts.GetSignatureCorElementType(th) == CorElementType.Array)
                {
                    offsetToUpperBounds = (uint)(boundsStart - objectAddress);
                    offsetToLowerBounds = (uint)(lowerBounds - objectAddress);
                }

                pArrayInfo->rank = rank;
                pArrayInfo->componentCount = numComponents;
                pArrayInfo->offsetToArrayBase = offsetToArrayBase;
                pArrayInfo->offsetToUpperBounds = offsetToUpperBounds;
                pArrayInfo->offsetToLowerBounds = offsetToLowerBounds;
                pArrayInfo->elementSize = rts.GetComponentSize(th);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    public int GetBasicObjectInfo(ulong objectAddress, Interop.BOOL* pIsValidRef, uint* pObjSize, uint* pObjOffsetToVars, DebuggerIPCE_ExpandedTypeData* pObjTypeData)
    {
        int hr = HResults.S_OK;
        try
        {
            *pIsValidRef = Interop.BOOL.TRUE;
            *pObjSize = 0;
            *pObjOffsetToVars = 0;
            *pObjTypeData = default;
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            // verify the object reference is readable and has a valid MethodTable
            TypeHandle th = default;
            try
            {
                TargetPointer mt = _target.Contracts.Object.GetMethodTableAddress(objectAddress);
                th = rts.GetTypeHandle(mt);
            }
            catch
            {
                *pIsValidRef = Interop.BOOL.FALSE;
            }

            if (*pIsValidRef == Interop.BOOL.TRUE)
            {
                // objOffsetToVars = offset from the object base to the first field = sizeof(Object) = pointer size
                *pObjOffsetToVars = (uint)_target.GetTypeInfo(DataType.Object).Size!.Value;
                *pObjSize = (uint)_target.Contracts.Object.GetSize(objectAddress);
                TypeHandleToExpandedTypeInfoImpl(rts, AreValueTypesBoxed.AllBoxed, th, pObjTypeData);

                // If this is a string, force elementType to ELEMENT_TYPE_STRING
                if (rts.IsString(th))
                {
                    WriteLittleEndian(ref pObjTypeData->elementType, (int)CorElementType.String);
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

    public int GetThreadOwningMonitorLock(ulong vmObject, DacDbiMonitorLockInfo* pRetVal)
    {
        int hr = HResults.S_OK;
        *pRetVal = default;
        try
        {
            DacDbiMonitorLockInfo info = default;
            uint threadId = 0;
            uint recursionCount = 0;
            TargetPointer syncBlock = _target.Contracts.Object.GetSyncBlockAddress(vmObject);

            if (syncBlock == TargetPointer.Null || !_target.Contracts.SyncBlock.TryGetLockInfo(syncBlock, out threadId, out recursionCount))
            {
                *pRetVal = info;
            }
            else
            {
                TargetPointer threadPtr = _target.Contracts.Thread.IdToThread(threadId);
                Debug.Assert(threadPtr != TargetPointer.Null, "A thread should have been found");
                if (threadPtr != TargetPointer.Null)
                {
                    info.lockOwner = threadPtr;
                    info.acquisitionCount = recursionCount + 1; // The runtime tracks recursion count starting at 0, but diagnostics users expect it to start at 1.
                }
                *pRetVal = info;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    public int EnumerateMonitorEventWaitList(ulong vmObject, nint fpCallback, nint pUserData) => HResults.E_NOTIMPL;

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

    public int GetModuleMetaDataFileInfo(ulong vmModule, uint* dwTimeStamp, uint* dwImageSize, nint pStrFilename, Interop.BOOL* pResult)
    {
        int hr = HResults.S_OK;
        string path = string.Empty;
        try
        {
            if (dwTimeStamp is null || dwImageSize is null || pStrFilename == 0 || pResult is null)
                throw new NullReferenceException("One or more parameters are null");
            *pResult = Interop.BOOL.FALSE;
            *dwTimeStamp = 0;
            *dwImageSize = 0;
            if (vmModule == 0)
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(vmModule);
            bool result = loader.GetFileHeadersInfo(moduleHandle, out uint timeStamp, out uint imageSize);
            if (result)
            {
                *dwTimeStamp = timeStamp;
                *dwImageSize = imageSize;
            }
            try
            {
                path = loader.GetPath(moduleHandle);
            }
            catch (VirtualReadException)
            {
                path = loader.GetFileName(moduleHandle);
            }
            if (string.IsNullOrEmpty(path))
            {
                path = loader.GetFileName(moduleHandle);
            }
            hr = StringHolderAssignCopy(pStrFilename, path);
            *pResult = result ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    public int IsThreadSuspendedOrHijacked(ulong vmThread, Interop.BOOL* pResult)
        => LegacyFallbackHelper.CanFallback() && _legacy is not null ? _legacy.IsThreadSuspendedOrHijacked(vmThread, pResult) : HResults.E_NOTIMPL;

    public int CreateHeapWalk(nuint* pHandle)
    {
        int hr = HResults.S_OK;
        if (pHandle is null)
            return HResults.E_POINTER;
        *pHandle = 0;
        HeapWalk? walk = null;
        try
        {
            walk = new HeapWalk(_target);
            *pHandle = (nuint)((IEnum<COR_HEAPOBJECT>)walk).GetHandle();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            nuint legacyHandle = 0;
            int hrLocal = _legacy.CreateHeapWalk(&legacyHandle);
            // The cDAC walker uses a lazy C# iterator and doesn't pre-validate objects at construction time;
            // the legacy walker eagerly validates the heap-start object and can refuse if it's corrupt.
            Debug.ValidateHResult(hr, hrLocal, HResultValidationMode.AllowCdacSuccess);
            if (hrLocal == HResults.S_OK && walk is not null)
                walk.LegacyHandle = legacyHandle;
            else if (hrLocal == HResults.S_OK)
                _legacy.DeleteHeapWalk(legacyHandle);
        }
#endif
        return hr;
    }

    public int DeleteHeapWalk(nuint handle)
    {
        if (handle == 0)
            return HResults.S_OK;

        int hr = HResults.S_OK;
        nuint legacyHandle = 0;
        try
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((nint)handle);
            if (gcHandle.Target is not HeapWalk walk)
                throw new ArgumentException("Invalid heap walk handle", nameof(handle));
            legacyHandle = walk.LegacyHandle;
            ((IEnum<COR_HEAPOBJECT>)walk).Dispose();
            gcHandle.Free();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null && legacyHandle != 0)
        {
            int hrLocal = _legacy.DeleteHeapWalk(legacyHandle);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    // Should be called repeatedly until it returns S_FALSE. E_FAIL is not fatal, just indicates partial heap corruption.
    public int WalkHeap(nuint handle, uint count, COR_HEAPOBJECT* objects, uint* fetched)
    {
        if (fetched is null)
            return HResults.E_INVALIDARG;
        *fetched = 0;
        if (objects is null && count > 0)
            return HResults.E_INVALIDARG;
        if (handle == 0)
            return HResults.E_INVALIDARG;

        HeapWalk walk;
        try
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((nint)handle);
            if (gcHandle.Target is not HeapWalk hw)
                throw new ArgumentException("Invalid heap walk handle", nameof(handle));
            walk = hw;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        int hr = HResults.S_OK;
        uint i = 0;
        try
        {
            while (i < count && walk.Enumerator.MoveNext())
            {
                COR_HEAPOBJECT current = walk.Enumerator.Current;
                // Sentinel value indicates invalid object.
                if (current.address == 0)
                {
                    hr = HResults.E_FAIL;
                    break;
                }
                objects[i++] = current;
            }

            // A clean batch reports S_FALSE iff we couldn't fill the caller's request.
            if (hr == HResults.S_OK && i < count)
                hr = HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        *fetched = i;
#if DEBUG
        if (_legacy is not null && walk.LegacyHandle != 0)
        {
            COR_HEAPOBJECT[] objectsLocal = new COR_HEAPOBJECT[count];
            uint fetchedLocal = 0;
            int hrLocal;
            fixed (COR_HEAPOBJECT* objectsLocalPtr = objectsLocal)
            {
                hrLocal = _legacy.WalkHeap(walk.LegacyHandle, count, objectsLocalPtr, &fetchedLocal);
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

                // GetTypeHandle validation performs the MethodTable -> EEClass -> MethodTable
                // round-trip check (the port of MethodTable::ValidateWithPossibleAV), so a
                // successfully resolved type handle means the object's MethodTable is self-consistent.
                rts.GetTypeHandle(mt);
                isValid = Interop.BOOL.TRUE;
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

    public int CreateRefWalk(nuint* pHandle, Interop.BOOL walkStacks, CorGCReferenceType handleWalkMask)
    {
        int hr = HResults.S_OK;
        RefWalk? walk = null;
        try
        {
            if (pHandle is null)
                throw new NullReferenceException(nameof(pHandle));

            *pHandle = 0;
            walk = new RefWalk(_target, walkStacks != Interop.BOOL.FALSE, handleWalkMask);
            *pHandle = (nuint)((IEnum<DacGcReference>)walk).GetHandle();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            nuint legacyHandle = 0;
            int hrLocal = _legacy.CreateRefWalk(&legacyHandle, walkStacks, handleWalkMask);
            Debug.ValidateHResult(hr, hrLocal);
            if (hrLocal == HResults.S_OK && walk is not null)
                walk.LegacyHandle = legacyHandle;
            else if (hrLocal == HResults.S_OK)
                _legacy.DeleteRefWalk(legacyHandle);
        }
#endif
        return hr;
    }

    public int DeleteRefWalk(nuint handle)
    {
        if (handle == 0)
            return HResults.S_OK;

        int hr = HResults.S_OK;
        nuint legacyHandle = 0;
        try
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((nint)handle);
            if (gcHandle.Target is not RefWalk walk)
                throw new ArgumentException("Handle does not reference a valid RefWalk instance.", nameof(handle));
            legacyHandle = walk.LegacyHandle;
            ((IEnum<DacGcReference>)walk).Dispose();
            gcHandle.Free();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null && legacyHandle != 0)
        {
            int hrLocal = _legacy.DeleteRefWalk(legacyHandle);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    // Should be called repeatedly until it returns S_FALSE.
    public int WalkRefs(nuint handle, uint count, [In, MarshalUsing(CountElementName = "count"), Out] DacGcReference[] refs, uint* pFetched)
    {
        RefWalk walk;
        try
        {
            if (pFetched is null)
                throw new NullReferenceException(nameof(pFetched));
            if (handle == 0)
                throw new ArgumentException("Handle is invalid.", nameof(handle));
            GCHandle gcHandle = GCHandle.FromIntPtr((nint)handle);
            if (gcHandle.Target is not RefWalk rw)
                throw new ArgumentException("Handle does not reference a valid RefWalk instance.", nameof(handle));
            walk = rw;
            *pFetched = 0;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        int hr = HResults.S_OK;
        uint i = 0;
        try
        {
            while (i < count && walk.Enumerator.MoveNext())
                refs[i++] = walk.Enumerator.Current;

            // A clean batch reports S_FALSE iff we couldn't fill the caller's request.
            if (i < count)
                hr = HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        *pFetched = i;

#if DEBUG
        if (_legacy is not null && walk.LegacyHandle != 0 && count > 0)
        {
            // Parity check covers the handle prefix only.
            DacGcReference[] legacyRefs = new DacGcReference[(int)count];
            uint legacyFetched = 0;
            int hrLocal = _legacy.WalkRefs(walk.LegacyHandle, count, legacyRefs, &legacyFetched);
            // The number of reported stack refs is not guaranteed to match between cDAC and legacy DAC.
            // If this is the case, the cDAC may report S_FALSE while the legacy DAC reports S_OK, or vice versa.
            // Allow divergent success codes, but still validate the rest of the results.
            Debug.ValidateHResult(hr, hrLocal, HResultValidationMode.AllowDivergentSuccess);
            uint cdacHandlePrefix = CountHandlePrefix(refs, i);
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

        static uint CountHandlePrefix(DacGcReference[] buffer, uint length)
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

        return hr;
    }

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

                bool isReferenceType = rts.IsCorElementTypeObjRef(rts.GetInternalCorElementType(typeHandle));
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
            CorElementType componentType = rts.IsString(typeHandle)
                ? CorElementType.String
                : rts.GetInternalCorElementType(typeHandle);
            pLayout->type = (int)componentType;
            pLayout->boxOffset = rts.IsCorElementTypeObjRef(componentType) ? 0u : (uint)_target.PointerSize;
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
    {
        int hr = HResults.S_OK;
        try
        {
            if (vmModule == 0)
                throw new ArgumentException("Module pointer cannot be null.", nameof(vmModule));

            if (pOptimizationsDisabled is null)
                throw new ArgumentException("Output pointer cannot be null.", nameof(pOptimizationsDisabled));

            if ((EcmaMetadataUtils.TokenType)(methodTk & EcmaMetadataUtils.TokenTypeMask) != EcmaMetadataUtils.TokenType.mdtMethodDef)
                throw new ArgumentException("methodTk must be a MethodDef token.", nameof(methodTk));

            *pOptimizationsDisabled = Interop.BOOL.FALSE;
            if (_target.Contracts.TryGetContract<IReJIT>(out IReJIT rejit))
            {
                ILoader loader = _target.Contracts.Loader;
                Contracts.ModuleHandle module = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));
                ModuleLookupTables lookupTables = loader.GetLookupTables(module);
                TargetPointer methodDesc = loader.GetModuleLookupMapElement(lookupTables.MethodDefToDesc, methodTk, out _);

                if (methodDesc != TargetPointer.Null)
                {
                    ICodeVersions codeVersions = _target.Contracts.CodeVersions;
                    ILCodeVersionHandle ilCodeVersion = codeVersions.GetActiveILCodeVersion(methodDesc);
                    if (rejit.IsDeoptimized(ilCodeVersion))
                    {
                        *pOptimizationsDisabled = Interop.BOOL.TRUE;
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
            Interop.BOOL localPOptimizationsDisabled;
            int hrLocal = _legacy.AreOptimizationsDisabled(vmModule, methodTk, &localPOptimizationsDisabled);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*pOptimizationsDisabled == localPOptimizationsDisabled);
        }
#endif

        return hr;
    }

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
    {
        int hr = HResults.S_OK;
        try
        {
            if (pData is null)
                throw new ArgumentException("Output pointer cannot be null.", nameof(pData));
            *pData = default;
            if (_target.Contracts.TryGetContract<IReJIT>(out _))
            {
                ICodeVersions codeVersions = _target.Contracts.CodeVersions;
                ILCodeVersionHandle ilCodeVersion = ILCodeVersionHandle.CreateExplicit(ilCodeVersionNode);

                pData->pbIL = codeVersions.GetIL(ilCodeVersion).Value;
                if (codeVersions.TryGetInstrumentedILMap(ilCodeVersion, out uint mapEntryCount, out TargetPointer mapEntries))
                {
                    pData->cInstrumentedMapEntries = mapEntryCount;
                    pData->rgInstrumentedMapEntries = mapEntries.Value;
                }
                else
                {
                    pData->cInstrumentedMapEntries = 0;
                    pData->rgInstrumentedMapEntries = 0;
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
    {
        int hr = HResults.S_OK;
        try
        {
            if (vmObject == 0)
            {
                *pResult = Interop.BOOL.FALSE;
            }
            else
            {
                *pResult = IsDelegateHelper(vmObject) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    public int GetDelegateFunctionData(ulong delegateObject, ulong* ppFunctionAssembly, uint* pMethodDef)
    {
        int hr = HResults.S_OK;
        try
        {
            if (ppFunctionAssembly == null)
                throw new ArgumentNullException(nameof(ppFunctionAssembly));
            if (pMethodDef == null)
                throw new ArgumentNullException(nameof(pMethodDef));
            if (!IsDelegateHelper(delegateObject))
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_UNSUPPORTED_DELEGATE)!;

            DelegateInfo delegateInfo = _target.Contracts.Object.GetDelegateInfo(new TargetPointer(delegateObject));

            // Only closed/open delegates expose a single managed target method via this API.
            // Multicast, unmanaged-fptr, wrapper, and special-sig delegates are not supported.
            if (delegateInfo.DelegateType is not (DelegateType.Closed or DelegateType.Open))
            {
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_UNSUPPORTED_DELEGATE)!;
            }

            IExecutionManager eman = _target.Contracts.ExecutionManager;
            TargetPointer methodDescPtr = eman.NonVirtualEntry2MethodDesc(delegateInfo.TargetMethodPtr);

            if (methodDescPtr == TargetPointer.Null)
            {
                throw new ArgumentException("Unable to find MethodDesc for the delegate's target method.", nameof(delegateObject));
            }

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
            *pMethodDef = rts.GetMethodToken(mdHandle);

            TargetPointer mtPtr = rts.GetMethodTable(mdHandle);
            TypeHandle typeHandle = rts.GetTypeHandle(mtPtr);
            TargetPointer modulePtr = rts.GetModule(typeHandle);
            Contracts.ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePtr);
            *ppFunctionAssembly = _target.Contracts.Loader.GetAssembly(moduleHandle).Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    public int GetDelegateTargetObject(ulong delegateObject, ulong* ppTargetObj)
    {
        int hr = HResults.S_OK;
        try
        {
            if (ppTargetObj == null)
                throw new ArgumentNullException(nameof(ppTargetObj));
            if (!IsDelegateHelper(delegateObject))
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_UNSUPPORTED_DELEGATE)!;

            DelegateInfo delegateInfo = _target.Contracts.Object.GetDelegateInfo(new TargetPointer(delegateObject));
            if (delegateInfo.DelegateType is not (DelegateType.Closed or DelegateType.Open))
            {
                throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_UNSUPPORTED_DELEGATE)!;
            }

            *ppTargetObj = delegateInfo.TargetObject.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    private bool IsDelegateHelper(ulong vmObject)
    {
        TargetPointer mt = _target.Contracts.Object.GetMethodTableAddress(vmObject);
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        TypeHandle typeHandle = rts.GetTypeHandle(mt);
        return rts.IsDelegate(typeHandle);
    }

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
    {
        int hr = HResults.S_OK;
        try
        {
            if (pVmAssembly == null)
                throw new ArgumentNullException(nameof(pVmAssembly));

            *pVmAssembly = 0;

            if (vmModule == 0)
                throw new ArgumentException("Module pointer must not be zero.", nameof(vmModule));

            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));
            TargetPointer assemblyPtr = loader.GetAssembly(handle);
            *pVmAssembly = assemblyPtr.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

    public int ParseContinuation(ulong continuationAddress, ulong* pDiagnosticIP, ulong* pNextContinuation, uint* pState)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pDiagnosticIP is null || pNextContinuation is null || pState is null)
                throw new ArgumentException("Output pointers must not be null.");

            ContinuationInfo info = _target.Contracts.Object.GetContinuationInfo(new TargetPointer(continuationAddress));
            *pDiagnosticIP = info.DiagnosticIP.Value;
            *pNextContinuation = info.Next.Value;
            *pState = info.State;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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

#if DEBUG
    [ThreadStatic]
    private static List<AsyncLocalData>? _debugEnumerateAsyncLocals;

    private static List<AsyncLocalData> DebugEnumerateAsyncLocals
        => _debugEnumerateAsyncLocals ??= new();

    [UnmanagedCallersOnly]
    private static void EnumerateAsyncLocalsDebugCallback(AsyncLocalData* pLocal, nint _)
    {
        DebugEnumerateAsyncLocals.Add(*pLocal);
    }
#endif

    public int EnumerateAsyncLocals(ulong vmMethod, ulong codeAddr, uint state,
        delegate* unmanaged<AsyncLocalData*, nint, void> fpCallback, nint pUserData)
    {
        int hr = HResults.S_OK;
#if DEBUG
        List<AsyncLocalData> locals = new();
#endif
        try
        {
            if (vmMethod == 0)
                throw new ArgumentException("vmMethod must not be zero.", nameof(vmMethod));
            if (fpCallback is null)
                throw new ArgumentNullException(nameof(fpCallback));

            Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            Contracts.MethodDescHandle md = rts.GetMethodDescHandle(new TargetPointer(vmMethod));

            if (!rts.IsAsyncThunkMethod(md))
            {
                TargetCodePointer pCode;
                if (codeAddr != 0)
                {
                    Contracts.ICodeVersions cv = _target.Contracts.CodeVersions;
                    NativeCodeVersionHandle ncvh = cv.GetNativeCodeVersionForIP(new TargetCodePointer(codeAddr));
                    pCode = ncvh.Valid ? cv.GetNativeCode(ncvh) : TargetCodePointer.Null;
                }
                else
                {
                    pCode = rts.GetNativeCode(md);
                }

                if (pCode != TargetCodePointer.Null)
                {
                    IReadOnlyList<AsyncSuspensionInfo> suspensionPoints = _target.Contracts.DebugInfo.GetAsyncSuspensionPoints(pCode);
                    if (state < (uint)suspensionPoints.Count)
                    {
                        IReadOnlyList<AsyncLocalInfo> localInfos = suspensionPoints[(int)state].Locals;
                        int varCount = localInfos.Count;
                        for (int i = 0; i < varCount; i++)
                        {
                            AsyncLocalData local = new()
                            {
                                Offset = localInfos[i].Offset,
                                IlVarNum = localInfos[i].ILVarNumber,
                            };
#if DEBUG
                            locals.Add(local);
#endif
                            fpCallback(&local, pUserData);
                        }
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
            DebugEnumerateAsyncLocals.Clear();
            delegate* unmanaged<AsyncLocalData*, nint, void> debugCallbackPtr = &EnumerateAsyncLocalsDebugCallback;
            int hrLocal = _legacy.EnumerateAsyncLocals(vmMethod, codeAddr, state, debugCallbackPtr, 0);
            Debug.ValidateHResult(hr, hrLocal);

            if (hr == HResults.S_OK)
            {
                List<AsyncLocalData> legacyLocals = DebugEnumerateAsyncLocals;
                Debug.Assert(locals.Count == legacyLocals.Count,
                    $"cDAC: {locals.Count} async locals, DAC: {legacyLocals.Count}");
                for (int i = 0; i < locals.Count; i++)
                {
                    Debug.Assert(locals[i].Offset == legacyLocals[i].Offset,
                        $"cDAC[{i}].Offset {locals[i].Offset} != DAC {legacyLocals[i].Offset}");
                    Debug.Assert(locals[i].IlVarNum == legacyLocals[i].IlVarNum,
                        $"cDAC[{i}].IlVarNum {locals[i].IlVarNum} != DAC {legacyLocals[i].IlVarNum}");
                }
            }
            DebugEnumerateAsyncLocals.Clear();
        }
#endif
        return hr;
    }

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
                case GenericContextLoc.InstArgMethodDesc:
                case GenericContextLoc.InstArgMethodTable:
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

    // Fills a DebuggerIPCE_ExpandedTypeData entry for a single type parameter, falling back to System.__Canon on failure.
    private void FillExpandedTypeDataWithCanonFallback(IRuntimeTypeSystem rts, TypeHandle typeHandle, TypeHandle thCanon, DebuggerIPCE_ExpandedTypeData* pTypeInfo)
    {
        try
        {
            TypeHandleToExpandedTypeInfoImpl(rts, AreValueTypesBoxed.NoValueTypeBoxing, typeHandle, pTypeInfo);
        }
        catch (VirtualReadException)
        {
            TypeHandleToExpandedTypeInfoImpl(rts, AreValueTypesBoxed.NoValueTypeBoxing, thCanon, pTypeInfo);
        }
    }

    // True if `a` and `b` share the same non-zero TypeDef RID and Module.
    // Mirrors native MethodTable::HasSameTypeDefAs.
    private static bool HasSameTypeDefAs(IRuntimeTypeSystem rts, TypeHandle a, TypeHandle b)
    {
        if (a.Address == b.Address)
            return true;
        uint ridA = EcmaMetadataUtils.GetRowId(rts.GetTypeDefToken(a));
        uint ridB = EcmaMetadataUtils.GetRowId(rts.GetTypeDefToken(b));
        if (ridA == 0 || ridA != ridB)
            return false;
        return rts.GetModule(a) == rts.GetModule(b);
    }

    // Walks the parent chain of `start` and returns the first MethodTable whose TypeDef matches `parent`,
    // or default if no match is found. The walk is bounded by a hard iteration cap to defend against
    // cycles observed in corrupt dumps. Mirrors native MethodTable::GetMethodTableMatchingParentClass.
    private static TypeHandle GetMethodTableMatchingParentClass(IRuntimeTypeSystem rts, TypeHandle start, TypeHandle parent)
    {
        TypeHandle current = start;
        TargetPointer prev = TargetPointer.Null;
        for (int i = 0; i < 1000 && !current.IsNull; i++)
        {
            if (HasSameTypeDefAs(rts, current, parent))
                return current;
            TargetPointer next = rts.GetParentMethodTable(current);
            if (next == TargetPointer.Null || next == prev || next == current.Address)
                break;
            prev = current.Address;
            current = rts.GetTypeHandle(next);
        }
        return default;
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
    // and ptr/byref referent types. Exposed as internal so tests can build the ArgInfoList
    // needed to round-trip a TypeHandle through GetExactTypeHandle.
    internal void FillBasicTypeInfo(IRuntimeTypeSystem rts, TypeHandle typeHandle, out DebuggerIPCE_BasicTypeData typeInfo)
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

    internal static T ReadLittleEndian<T>(T value) where T : unmanaged, IBinaryInteger<T>
    {
        if (BitConverter.IsLittleEndian)
            return value;
        MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)).Reverse();
        return value;
    }

}
