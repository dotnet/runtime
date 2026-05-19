// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataMethodDefinition : IXCLRDataMethodDefinition
{
    private readonly Target _target;
    private readonly TargetPointer _module;
    private readonly uint _token;
    private readonly IXCLRDataMethodDefinition? _legacyImpl;
    public ClrDataMethodDefinition(
        Target target,
        TargetPointer module,
        uint token,
        IXCLRDataMethodDefinition? legacyImpl)
    {
        _target = target;
        _module = module;
        _token = token;
        _legacyImpl = legacyImpl;
    }

    private TargetPointer TryResolveMethodDesc()
    {
        ILoader loader = _target.Contracts.Loader;
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(_module);
        ModuleLookupTables tables = loader.GetLookupTables(moduleHandle);
        TargetPointer methodDescAddr = loader.GetModuleLookupMapElement(tables.MethodDefToDesc, _token, out _);

        return methodDescAddr;
    }

    private static bool HasClassInstantiation(Target target, MethodDescHandle md)
    {
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        TargetPointer mtAddr = rts.GetMethodTable(md);
        TypeHandle mt = rts.GetTypeHandle(mtAddr);

        return !rts.GetInstantiation(mt).IsEmpty;
    }

    private static bool HasMethodInstantiation(Target target, MethodDescHandle md)
    {
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        if (rts.IsGenericMethodDefinition(md))
            return true;

        return !rts.GetGenericMethodInstantiation(md).IsEmpty;
    }

    private static bool HasClassOrMethodInstantiation(Target target, MethodDescHandle md)
    {
        return HasClassInstantiation(target, md) || HasMethodInstantiation(target, md);
    }

    private string GetFullMethodNameFromMetadata()
    {
        ILoader loader = _target.Contracts.Loader;
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(_module);
        IEcmaMetadata ecmaMetadata = _target.Contracts.EcmaMetadata;
        MetadataReader reader = ecmaMetadata.GetMetadata(moduleHandle)
            ?? throw new InvalidOperationException("Failed to get metadata reader");

        int rowId = (int)(_token & 0x00FFFFFF);
        MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle(rowId);
        MethodDefinition methodDef = reader.GetMethodDefinition(methodDefHandle);
        string methodName = reader.GetString(methodDef.Name);

        TypeDefinitionHandle typeDefHandle = methodDef.GetDeclaringType();
        if (typeDefHandle.IsNil)
            return methodName;

        TypeDefinition typeDef = reader.GetTypeDefinition(typeDefHandle);
        string typeName = reader.GetString(typeDef.Name);
        string namespaceName = reader.GetString(typeDef.Namespace);

        StringBuilder sb = new();
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.Append(namespaceName);
            sb.Append('.');
        }
        sb.Append(typeName);
        sb.Append('.');
        sb.Append(methodName);

        return sb.ToString();
    }

    int IXCLRDataMethodDefinition.GetTypeDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetTypeDefinition(typeDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.StartEnumInstances(IXCLRDataAppDomain? appDomain, ulong* handle)
    {
        int hr = HResults.S_FALSE;
        *handle = 0;

        // Start the legacy enumeration to keep it in sync with the cDAC enumeration.
        // EnumInstance passes the legacy method instance to ClrDataMethodInstance,
        // which delegates some operations to it.
        ulong legacyHandle = default;
        int hrLocal = default;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.StartEnumInstances(appDomain, &legacyHandle);
        }

        try
        {
            TargetPointer methodDescAddr = TryResolveMethodDesc();
            if (methodDescAddr != TargetPointer.Null)
            {
                SOSDacImpl.EnumMethodInstances emi = new(_target, methodDescAddr, TargetPointer.Null);
                emi.LegacyHandle = legacyHandle;

                hr = emi.Start();
                if (hr == HResults.S_OK)
                {
                    *handle = (ulong)((IEnum<MethodDescHandle>)emi).GetHandle();
                    // Legacy handle ownership transferred to emi — don't clean up below.
                    legacyHandle = default;
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        finally
        {
            // The legacy enumeration is started eagerly (before the cDAC try block) so
            // that EnumInstance can advance both enumerations in lockstep. If the cDAC
            // side fails to produce an enum (no MethodDesc, exception, or emi.Start()
            // returns S_FALSE), the legacy handle would be orphaned because the caller
            // receives *handle == 0 and has no way to call End. Clean it up here.
            if (_legacyImpl is not null && legacyHandle != default)
            {
                _legacyImpl.EndEnumInstances(legacyHandle);
            }
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif

        return hr;
    }

    int IXCLRDataMethodDefinition.EnumInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> instance)
    {
        int hr = HResults.S_OK;

        if (*handle == 0)
            return HResults.S_FALSE;

        GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)(*handle));
        if (gcHandle.Target is not SOSDacImpl.EnumMethodInstances emi)
            return HResults.E_INVALIDARG;

        // Advance the legacy enumeration to keep it in sync with the cDAC enumeration.
        // The legacy method instance is passed to ClrDataMethodInstance for delegation.
        IXCLRDataMethodInstance? legacyMethod = null;
        int hrLocal = default;
        if (_legacyImpl is not null)
        {
            ulong legacyHandle = emi.LegacyHandle;
            DacComNullableByRef<IXCLRDataMethodInstance> legacyMethodOut = new(isNullRef: false);
            hrLocal = _legacyImpl.EnumInstance(&legacyHandle, legacyMethodOut);
            legacyMethod = legacyMethodOut.Interface;
            emi.LegacyHandle = legacyHandle;
        }

        try
        {
            if (emi.Enumerator.MoveNext())
            {
                MethodDescHandle methodDesc = emi.Enumerator.Current;
                instance.Interface = new ClrDataMethodInstance(_target, methodDesc, emi._appDomain, legacyMethod);
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            // Fall back to the legacy DAC result when available, otherwise propagate the error.
            if (_legacyImpl is not null)
            {
                hr = hrLocal;
                instance.Interface = legacyMethod;
            }
            else
            {
                hr = ex.HResult;
            }
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif

        return hr;
    }

    int IXCLRDataMethodDefinition.EndEnumInstances(ulong handle)
    {
        int hr = HResults.S_OK;

        try
        {
            if (handle == 0)
                throw new ArgumentException();

            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)handle);
            if (gcHandle.Target is not SOSDacImpl.EnumMethodInstances emi)
                throw new ArgumentException();

            ((IEnum<MethodDescHandle>)emi).Dispose();
            gcHandle.Free();

            if (_legacyImpl is not null && emi.LegacyHandle != TargetPointer.Null)
            {
                int hrLocal = _legacyImpl.EndEnumInstances(emi.LegacyHandle);
                if (hrLocal < 0)
                    hr = hrLocal;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    int IXCLRDataMethodDefinition.GetName(uint flags, uint bufLen, uint* nameLen, char* name)
    {
        int hr = HResults.S_OK;

        try
        {
            if (flags != 0)
                throw new ArgumentException();

            StringBuilder sb = new();

            TargetPointer methodDescAddr = TryResolveMethodDesc();

            if (methodDescAddr != TargetPointer.Null)
            {
                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                MethodDescHandle methodDescHandle = rts.GetMethodDescHandle(methodDescAddr);
                TypeNameBuilder.AppendMethodInternal(
                    _target,
                    sb,
                    methodDescHandle,
                    TypeNameFormat.FormatSignature |
                    TypeNameFormat.FormatNamespace |
                    TypeNameFormat.FormatFullInst);
            }
            else
            {
                sb.Append(GetFullMethodNameFromMetadata());
            }

            OutputBufferHelpers.CopyStringToBuffer(name, bufLen, nameLen, sb.ToString());

            if (name is not null && bufLen < (uint)(sb.Length + 1))
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint nameLenLocal = 0;
            char[] nameBufLocal = new char[bufLen > 0 ? bufLen : 1];
            int hrLocal;
            fixed (char* pNameBufLocal = nameBufLocal)
            {
                hrLocal = _legacyImpl.GetName(flags, bufLen, &nameLenLocal, name is null ? null : pNameBufLocal);
            }

            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0 && hrLocal >= 0)
            {
                if (nameLen is not null)
                    Debug.Assert(nameLenLocal == *nameLen, $"cDAC: {*nameLen:x}, DAC: {nameLenLocal:x}");

                if (name is not null && nameLenLocal > 0)
                {
                    string dacName = new string(nameBufLocal, 0, (int)nameLenLocal - 1);
                    string cdacName = new string(name);
                    Debug.Assert(dacName == cdacName, $"cDAC: {cdacName}, DAC: {dacName}");
                }
            }
        }
#endif

        return hr;
    }

    int IXCLRDataMethodDefinition.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
    {
        int hr = HResults.S_OK;
        try
        {
            if (token is not null)
            {
                *token = _token;
            }
            if (!mod.IsNullRef)
            {
                IXCLRDataModule? legacyMod = null;
                if (_legacyImpl is not null)
                {
                    DacComNullableByRef<IXCLRDataModule> legacyModOut = new(isNullRef: false);
                    int hrLegacy = _legacyImpl.GetTokenAndScope(null, legacyModOut);
                    if (hrLegacy < 0)
                        return hrLegacy;
                    legacyMod = legacyModOut.Interface;
                }

                mod.Interface = new ClrDataModule(_module, _target, legacyMod);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            bool validateToken = token is not null;
            bool validateMod = !mod.IsNullRef;

            uint tokenLocal = 0;
            DacComNullableByRef<IXCLRDataModule> legacyModOutLocal = new(isNullRef: !validateMod);
            int hrLocal = _legacyImpl.GetTokenAndScope(validateToken ? &tokenLocal : null, legacyModOutLocal);

            Debug.ValidateHResult(hr, hrLocal);

            if (validateToken)
            {
                Debug.Assert(tokenLocal == *token, $"cDAC: {*token:x}, DAC: {tokenLocal:x}");
            }
        }
#endif

        return hr;
    }

    int IXCLRDataMethodDefinition.GetFlags(uint* flags)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.IsSameObject(IXCLRDataMethodDefinition? method)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.IsSameObject(method) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.GetLatestEnCVersion(uint* version)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetLatestEnCVersion(version) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.StartEnumExtents(ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumExtents(handle) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.EnumExtent(ulong* handle, ClrDataMethodDefinitionExtent* extent)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumExtent(handle, extent) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.EndEnumExtents(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumExtents(handle) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.GetCodeNotification(uint* flags)
    {
        int hr = HResults.S_OK;
        ICodeNotifications codeNotif = _target.Contracts.CodeNotifications;

        try
        {
            if (flags is null)
                throw new ArgumentNullException(nameof(flags));

            *flags = CodeNotificationFlagsConverter.ToCom(codeNotif.GetCodeNotification(_module, _token));
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        // No #if DEBUG validation: GetCodeNotification is a read, but both cDAC and
        // legacy DAC allocate the table on-demand when called, which would cause
        // dual-allocation. Validation is safe at a higher layer when a dump is used.

        return hr;
    }

    int IXCLRDataMethodDefinition.SetCodeNotification(uint flags)
    {
        int hr = HResults.S_OK;
        ICodeNotifications codeNotif = _target.Contracts.CodeNotifications;

        try
        {
            if (!CodeNotificationFlagsConverter.IsValid(flags))
                throw new ArgumentException("Invalid code notification flags", nameof(flags));

            codeNotif.SetCodeNotification(_module, _token, CodeNotificationFlagsConverter.FromCom(flags));
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        // No #if DEBUG validation: SetCodeNotification is a write operation.
        // Both the cDAC and legacy DAC independently allocate and write to
        // g_pNotificationTable via AllocVirtual, causing dual-write corruption.

        return hr;
    }

    int IXCLRDataMethodDefinition.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.GetRepresentativeEntryAddress(ClrDataAddress* addr)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetRepresentativeEntryAddress(addr) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.HasClassOrMethodInstantiation(int* bGeneric)
    {
        int hr = HResults.S_OK;

        try
        {
            if (bGeneric is null)
                throw new NullReferenceException();

            TargetPointer methodDescAddr = TryResolveMethodDesc();
            if (methodDescAddr == TargetPointer.Null)
                throw new System.Runtime.InteropServices.COMException(null, unchecked((int)0x8000FFFF)); // E_UNEXPECTED

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            MethodDescHandle methodDescHandle = rts.GetMethodDescHandle(methodDescAddr);
            *bGeneric = HasClassOrMethodInstantiation(_target, methodDescHandle) ? (int)Interop.BOOL.TRUE : (int)Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            int bGenericLocal = 0;
            int hrLocal = _legacyImpl.HasClassOrMethodInstantiation(&bGenericLocal);

            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0 && hrLocal >= 0 && bGeneric is not null)
            {
                Debug.Assert(bGenericLocal == *bGeneric, $"cDAC: {*bGeneric}, DAC: {bGenericLocal}");
            }
        }
#endif

        return hr;
    }
}
