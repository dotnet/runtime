// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Validation-shim proxy. Each method calls the production cDAC first (its result is always the one
// returned to the caller), then calls the legacy DAC and compares. The `#if DEBUG` comparison blocks
// are the pre-refactor cDAC blocks, recovered verbatim from the implementations that hosted the
// legacy DAC before the production decoupling; `hr` is the production cDAC result and the `_legacy*`
// fields are the legacy DAC's interfaces, exactly as they were in the original code.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Paired cDAC/DAC proxy for IMetaDataImport2.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class MetaDataImportProxy
    : ShimProxy, ICustomQueryInterface, IMetaDataImport2, IMetaDataAssemblyImport
{
    private readonly IMetaDataImport? _cdacImport;
    private readonly IMetaDataImport? _legacyImport;
    private readonly IMetaDataImport2? _cdacImport2;
    private readonly IMetaDataImport2? _legacyImport2;
    private readonly IMetaDataAssemblyImport? _cdacAssemblyImport;
    private readonly IMetaDataAssemblyImport? _legacyAssemblyImport;
    private readonly ConcurrentDictionary<nint, byte> _legacyEnumHandles = new();

    internal MetaDataImportProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImport = cdacObject as IMetaDataImport;
        _legacyImport = dacObject as IMetaDataImport;
        _cdacImport2 = cdacObject as IMetaDataImport2;
        _legacyImport2 = dacObject as IMetaDataImport2;
        _cdacAssemblyImport = cdacObject as IMetaDataAssemblyImport;
        _legacyAssemblyImport = dacObject as IMetaDataAssemblyImport;
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

        if (iid == typeof(IMetaDataImport).GUID)
            return Support(_cdacImport, _legacyImport);
        if (iid == typeof(IMetaDataImport2).GUID)
            return Support(_cdacImport2, _legacyImport2);
        if (iid == typeof(IMetaDataAssemblyImport).GUID)
            return Support(_cdacAssemblyImport, _legacyAssemblyImport);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    private void RegisterLegacyEnum(nint* phEnum, int hr)
    {
        if (hr >= 0 && phEnum is not null && *phEnum != 0)
            _legacyEnumHandles.TryAdd(*phEnum, 0);
    }

#if DEBUG
    private static void ValidateBlobsEqual(byte* cdacBlob, uint cdacLen, byte* dacBlob, uint dacLen, string name)
    {
        Debug.Assert(cdacLen == dacLen, $"{name} length mismatch: cDAC={cdacLen}, DAC={dacLen}");
        if (cdacLen == dacLen && cdacLen > 0 && cdacBlob is not null && dacBlob is not null)
        {
            ReadOnlySpan<byte> cdacSpan = new(cdacBlob, (int)cdacLen);
            ReadOnlySpan<byte> dacSpan = new(dacBlob, (int)dacLen);
            Debug.Assert(cdacSpan.SequenceEqual(dacSpan), $"{name} content mismatch (length={cdacLen})");
        }
    }
#endif

    #region IMetaDataImport
    void IMetaDataImport.CloseEnum(nint hEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        _cdacImport?.CloseEnum(hEnum);
        if (_legacyEnumHandles.TryRemove(hEnum, out _))
            _legacyImport?.CloseEnum(hEnum);
    }

    int IMetaDataImport.CountEnum(nint hEnum, uint* pulCount)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.CountEnum(hEnum, pulCount) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null && _legacyEnumHandles.ContainsKey(hEnum))
            return _legacyImport.CountEnum(hEnum, pulCount);
        return hr;
    }

    int IMetaDataImport.ResetEnum(nint hEnum, uint ulPos)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.ResetEnum(hEnum, ulPos) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null && _legacyEnumHandles.ContainsKey(hEnum))
            return _legacyImport.ResetEnum(hEnum, ulPos);
        return hr;
    }

    int IMetaDataImport.EnumTypeDefs(nint* phEnum, uint* rTypeDefs, uint cMax, uint* pcTypeDefs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumTypeDefs(phEnum, rTypeDefs, cMax, pcTypeDefs) : HResults.E_NOTIMPL;
        return hr;
    }

    int IMetaDataImport.EnumInterfaceImpls(nint* phEnum, uint td, uint* rImpls, uint cMax, uint* pcImpls)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumInterfaceImpls(phEnum, td, rImpls, cMax, pcImpls) : HResults.E_NOTIMPL;
        return hr;
    }

    int IMetaDataImport.EnumTypeRefs(nint* phEnum, uint* rTypeRefs, uint cMax, uint* pcTypeRefs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumTypeRefs(phEnum, rTypeRefs, cMax, pcTypeRefs) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumTypeRefs(phEnum, rTypeRefs, cMax, pcTypeRefs);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.FindTypeDefByName(char* szTypeDef, uint tkEnclosingClass, uint* ptd)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.FindTypeDefByName(szTypeDef, tkEnclosingClass, ptd) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint tdLocal = 0;
            int hrLegacy = _legacyImport.FindTypeDefByName(szTypeDef, tkEnclosingClass, &tdLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0 && ptd is not null)
                Debug.Assert(*ptd == tdLocal, $"TypeDef mismatch: cDAC=0x{*ptd:X}, DAC=0x{tdLocal:X}");
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetScopeProps(char* szName, uint cchName, uint* pchName, Guid* pmvid)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetScopeProps(szName, cchName, pchName, pmvid) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetScopeProps(szName, cchName, pchName, pmvid);
        return hr;
    }

    int IMetaDataImport.GetModuleFromScope(uint* pmd)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetModuleFromScope(pmd) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetModuleFromScope(pmd);
        return hr;
    }

    int IMetaDataImport.GetTypeDefProps(uint td, char* szTypeDef, uint cchTypeDef, uint* pchTypeDef, uint* pdwTypeDefFlags, uint* ptkExtends)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetTypeDefProps(td, szTypeDef, cchTypeDef, pchTypeDef, pdwTypeDefFlags, ptkExtends) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint flagsLocal = 0, extendsLocal = 0, pchLocal = 0;
            char* szLocal = stackalloc char[(int)cchTypeDef];
            int hrLegacy = _legacyImport.GetTypeDefProps(td, szLocal, cchTypeDef, &pchLocal, &flagsLocal, &extendsLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pdwTypeDefFlags is not null)
                    Debug.Assert(*pdwTypeDefFlags == flagsLocal, $"TypeDefFlags mismatch: cDAC=0x{*pdwTypeDefFlags:X}, DAC=0x{flagsLocal:X}");
                if (ptkExtends is not null)
                    Debug.Assert(*ptkExtends == extendsLocal, $"Extends mismatch: cDAC=0x{*ptkExtends:X}, DAC=0x{extendsLocal:X}");
                if (pchTypeDef is not null)
                    Debug.Assert(*pchTypeDef == pchLocal, $"Name length mismatch: cDAC={*pchTypeDef}, DAC={pchLocal}");
                if (szTypeDef is not null && cchTypeDef > 0)
                {
                    string cdacName = new string(szTypeDef);
                    string dacName = new string(szLocal);
                    Debug.Assert(cdacName == dacName, $"TypeDef name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetInterfaceImplProps(uint iiImpl, uint* pClass, uint* ptkIface)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetInterfaceImplProps(iiImpl, pClass, ptkIface) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint classLocal = 0, ifaceLocal = 0;
            int hrLegacy = _legacyImport.GetInterfaceImplProps(iiImpl, &classLocal, &ifaceLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pClass is not null)
                    Debug.Assert(*pClass == classLocal, $"Class mismatch: cDAC=0x{*pClass:X}, DAC=0x{classLocal:X}");
                if (ptkIface is not null)
                    Debug.Assert(*ptkIface == ifaceLocal, $"Interface mismatch: cDAC=0x{*ptkIface:X}, DAC=0x{ifaceLocal:X}");
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetTypeRefProps(uint tr, uint* ptkResolutionScope, char* szName, uint cchName, uint* pchName)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetTypeRefProps(tr, ptkResolutionScope, szName, cchName, pchName) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint scopeLocal = 0, pchLocal = 0;
            char* szLocal = stackalloc char[(int)cchName];
            int hrLegacy = _legacyImport.GetTypeRefProps(tr, &scopeLocal, szLocal, cchName, &pchLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (ptkResolutionScope is not null)
                    Debug.Assert(*ptkResolutionScope == scopeLocal, $"ResolutionScope mismatch: cDAC=0x{*ptkResolutionScope:X}, DAC=0x{scopeLocal:X}");
                if (pchName is not null)
                    Debug.Assert(*pchName == pchLocal, $"Name length mismatch: cDAC={*pchName}, DAC={pchLocal}");
                if (szName is not null && cchName > 0)
                {
                    string cdacName = new string(szName);
                    string dacName = new string(szLocal);
                    Debug.Assert(cdacName == dacName, $"TypeRef name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.ResolveTypeRef(uint tr, Guid* riid, void** ppIScope, uint* ptd)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.ResolveTypeRef(tr, riid, ppIScope, ptd) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.ResolveTypeRef(tr, riid, ppIScope, ptd);
        return hr;
    }

    int IMetaDataImport.EnumMembers(nint* phEnum, uint cl, uint* rMembers, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumMembers(phEnum, cl, rMembers, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumMembers(phEnum, cl, rMembers, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.EnumMembersWithName(nint* phEnum, uint cl, char* szName, uint* rMembers, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumMembersWithName(phEnum, cl, szName, rMembers, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumMembersWithName(phEnum, cl, szName, rMembers, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.EnumMethods(nint* phEnum, uint cl, uint* rMethods, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumMethods(phEnum, cl, rMethods, cMax, pcTokens) : HResults.E_NOTIMPL;
        return hr;
    }

    int IMetaDataImport.EnumMethodsWithName(nint* phEnum, uint cl, char* szName, uint* rMethods, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumMethodsWithName(phEnum, cl, szName, rMethods, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumMethodsWithName(phEnum, cl, szName, rMethods, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.EnumFields(nint* phEnum, uint cl, uint* rFields, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumFields(phEnum, cl, rFields, cMax, pcTokens) : HResults.E_NOTIMPL;
        return hr;
    }

    int IMetaDataImport.EnumFieldsWithName(nint* phEnum, uint cl, char* szName, uint* rFields, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumFieldsWithName(phEnum, cl, szName, rFields, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumFieldsWithName(phEnum, cl, szName, rFields, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.EnumParams(nint* phEnum, uint mb, uint* rParams, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumParams(phEnum, mb, rParams, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumParams(phEnum, mb, rParams, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.EnumMemberRefs(nint* phEnum, uint tkParent, uint* rMemberRefs, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumMemberRefs(phEnum, tkParent, rMemberRefs, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumMemberRefs(phEnum, tkParent, rMemberRefs, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.EnumMethodImpls(nint* phEnum, uint td, uint* rMethodBody, uint* rMethodDecl, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumMethodImpls(phEnum, td, rMethodBody, rMethodDecl, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumMethodImpls(phEnum, td, rMethodBody, rMethodDecl, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.EnumPermissionSets(nint* phEnum, uint tk, uint dwActions, uint* rPermission, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumPermissionSets(phEnum, tk, dwActions, rPermission, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumPermissionSets(phEnum, tk, dwActions, rPermission, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.FindMember(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.FindMember(td, szName, pvSigBlob, cbSigBlob, pmb) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.FindMember(td, szName, pvSigBlob, cbSigBlob, pmb);
        return hr;
    }

    int IMetaDataImport.FindMethod(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.FindMethod(td, szName, pvSigBlob, cbSigBlob, pmb) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.FindMethod(td, szName, pvSigBlob, cbSigBlob, pmb);
        return hr;
    }

    int IMetaDataImport.FindField(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.FindField(td, szName, pvSigBlob, cbSigBlob, pmb) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.FindField(td, szName, pvSigBlob, cbSigBlob, pmb);
        return hr;
    }

    int IMetaDataImport.FindMemberRef(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmr)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.FindMemberRef(td, szName, pvSigBlob, cbSigBlob, pmr) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.FindMemberRef(td, szName, pvSigBlob, cbSigBlob, pmr);
        return hr;
    }

    int IMetaDataImport.GetMethodProps(uint mb, uint* pClass, char* szMethod, uint cchMethod, uint* pchMethod,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetMethodProps(mb, pClass, szMethod, cchMethod, pchMethod, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint classLocal = 0, attrLocal = 0, rvaLocal = 0, implLocal = 0, pchLocal = 0, cbSigLocal = 0;
            byte* sigLocal = null;
            char* szLocal = stackalloc char[(int)cchMethod];
            int hrLegacy = _legacyImport.GetMethodProps(mb, &classLocal, szLocal, cchMethod, &pchLocal, &attrLocal, &sigLocal, &cbSigLocal, &rvaLocal, &implLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pClass is not null)
                    Debug.Assert(*pClass == classLocal, $"Class mismatch: cDAC=0x{*pClass:X}, DAC=0x{classLocal:X}");
                if (pdwAttr is not null)
                    Debug.Assert(*pdwAttr == attrLocal, $"Attr mismatch: cDAC=0x{*pdwAttr:X}, DAC=0x{attrLocal:X}");
                if (pchMethod is not null)
                    Debug.Assert(*pchMethod == pchLocal, $"Name length mismatch: cDAC={*pchMethod}, DAC={pchLocal}");
                if (szMethod is not null && cchMethod > 0)
                {
                    string cdacName = new string(szMethod);
                    string dacName = new string(szLocal);
                    Debug.Assert(cdacName == dacName, $"Method name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
                if (pulCodeRVA is not null)
                    Debug.Assert(*pulCodeRVA == rvaLocal, $"RVA mismatch: cDAC=0x{*pulCodeRVA:X}, DAC=0x{rvaLocal:X}");
                if (pdwImplFlags is not null)
                    Debug.Assert(*pdwImplFlags == implLocal, $"ImplFlags mismatch: cDAC=0x{*pdwImplFlags:X}, DAC=0x{implLocal:X}");
                if (ppvSigBlob is not null)
                    ValidateBlobsEqual(*ppvSigBlob, pcbSigBlob is not null ? *pcbSigBlob : cbSigLocal, sigLocal, cbSigLocal, "MethodSig");
                else if (pcbSigBlob is not null)
                    Debug.Assert(*pcbSigBlob == cbSigLocal, $"SigBlob length mismatch: cDAC={*pcbSigBlob}, DAC={cbSigLocal}");
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetMemberRefProps(uint mr, uint* ptk, char* szMember, uint cchMember, uint* pchMember,
        byte** ppvSigBlob, uint* pbSig)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetMemberRefProps(mr, ptk, szMember, cchMember, pchMember, ppvSigBlob, pbSig) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint tkLocal = 0, pchLocal = 0, cbSigLocal = 0;
            byte* sigLocal = null;
            char* szLocal = stackalloc char[(int)cchMember];
            int hrLegacy = _legacyImport.GetMemberRefProps(mr, &tkLocal, szLocal, cchMember, &pchLocal, &sigLocal, &cbSigLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (ptk is not null)
                    Debug.Assert(*ptk == tkLocal, $"Parent mismatch: cDAC=0x{*ptk:X}, DAC=0x{tkLocal:X}");
                if (pchMember is not null)
                    Debug.Assert(*pchMember == pchLocal, $"Name length mismatch: cDAC={*pchMember}, DAC={pchLocal}");
                if (szMember is not null && cchMember > 0)
                {
                    string cdacName = new string(szMember);
                    string dacName = new string(szLocal);
                    Debug.Assert(cdacName == dacName, $"MemberRef name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
                if (ppvSigBlob is not null)
                    ValidateBlobsEqual(*ppvSigBlob, pbSig is not null ? *pbSig : cbSigLocal, sigLocal, cbSigLocal, "MemberRefSig");
                else if (pbSig is not null)
                    Debug.Assert(*pbSig == cbSigLocal, $"SigBlob length mismatch: cDAC={*pbSig}, DAC={cbSigLocal}");
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.EnumProperties(nint* phEnum, uint td, uint* rProperties, uint cMax, uint* pcProperties)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumProperties(phEnum, td, rProperties, cMax, pcProperties) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumProperties(phEnum, td, rProperties, cMax, pcProperties);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.EnumEvents(nint* phEnum, uint td, uint* rEvents, uint cMax, uint* pcEvents)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumEvents(phEnum, td, rEvents, cMax, pcEvents) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumEvents(phEnum, td, rEvents, cMax, pcEvents);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.GetEventProps(uint ev, uint* pClass, char* szEvent, uint cchEvent, uint* pchEvent,
        uint* pdwEventFlags, uint* ptkEventType, uint* pmdAddOn, uint* pmdRemoveOn, uint* pmdFire,
        uint* rmdOtherMethod, uint cMax, uint* pcOtherMethod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetEventProps(ev, pClass, szEvent, cchEvent, pchEvent, pdwEventFlags, ptkEventType, pmdAddOn, pmdRemoveOn, pmdFire, rmdOtherMethod, cMax, pcOtherMethod) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetEventProps(ev, pClass, szEvent, cchEvent, pchEvent, pdwEventFlags, ptkEventType, pmdAddOn, pmdRemoveOn, pmdFire, rmdOtherMethod, cMax, pcOtherMethod);
        return hr;
    }

    int IMetaDataImport.EnumMethodSemantics(nint* phEnum, uint mb, uint* rEventProp, uint cMax, uint* pcEventProp)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumMethodSemantics(phEnum, mb, rEventProp, cMax, pcEventProp) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumMethodSemantics(phEnum, mb, rEventProp, cMax, pcEventProp);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.GetMethodSemantics(uint mb, uint tkEventProp, uint* pdwSemanticsFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetMethodSemantics(mb, tkEventProp, pdwSemanticsFlags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetMethodSemantics(mb, tkEventProp, pdwSemanticsFlags);
        return hr;
    }

    int IMetaDataImport.GetClassLayout(uint td, uint* pdwPackSize, void* rFieldOffset, uint cMax, uint* pcFieldOffset, uint* pulClassSize)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetClassLayout(td, pdwPackSize, rFieldOffset, cMax, pcFieldOffset, pulClassSize) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint packLocal = 0, sizeLocal = 0, fieldCountLocal = 0;
            int hrLegacy = _legacyImport.GetClassLayout(td, &packLocal, null, 0, &fieldCountLocal, &sizeLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pdwPackSize is not null)
                    Debug.Assert(*pdwPackSize == packLocal, $"PackSize mismatch: cDAC={*pdwPackSize}, DAC={packLocal}");
                if (pulClassSize is not null)
                    Debug.Assert(*pulClassSize == sizeLocal, $"ClassSize mismatch: cDAC={*pulClassSize}, DAC={sizeLocal}");
                if (pcFieldOffset is not null)
                    Debug.Assert(*pcFieldOffset == fieldCountLocal, $"FieldOffset count mismatch: cDAC={*pcFieldOffset}, DAC={fieldCountLocal}");
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetFieldMarshal(uint tk, byte** ppvNativeType, uint* pcbNativeType)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetFieldMarshal(tk, ppvNativeType, pcbNativeType) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetFieldMarshal(tk, ppvNativeType, pcbNativeType);
        return hr;
    }

    int IMetaDataImport.GetRVA(uint tk, uint* pulCodeRVA, uint* pdwImplFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetRVA(tk, pulCodeRVA, pdwImplFlags) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint rvaLocal = 0, implLocal = 0;
            int hrLegacy = _legacyImport.GetRVA(tk, &rvaLocal, &implLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pulCodeRVA is not null)
                    Debug.Assert(*pulCodeRVA == rvaLocal, $"RVA mismatch: cDAC=0x{*pulCodeRVA:X}, DAC=0x{rvaLocal:X}");
                if (pdwImplFlags is not null)
                    Debug.Assert(*pdwImplFlags == implLocal, $"ImplFlags mismatch: cDAC=0x{*pdwImplFlags:X}, DAC=0x{implLocal:X}");
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetPermissionSetProps(uint pm, uint* pdwAction, void** ppvPermission, uint* pcbPermission)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetPermissionSetProps(pm, pdwAction, ppvPermission, pcbPermission) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetPermissionSetProps(pm, pdwAction, ppvPermission, pcbPermission);
        return hr;
    }

    int IMetaDataImport.GetSigFromToken(uint mdSig, byte** ppvSig, uint* pcbSig)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetSigFromToken(mdSig, ppvSig, pcbSig) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint cbLocal = 0;
            byte* sigLocal = null;
            int hrLegacy = _legacyImport.GetSigFromToken(mdSig, &sigLocal, &cbLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (ppvSig is not null)
                    ValidateBlobsEqual(*ppvSig, pcbSig is not null ? *pcbSig : cbLocal, sigLocal, cbLocal, "StandaloneSig");
                else if (pcbSig is not null)
                    Debug.Assert(*pcbSig == cbLocal, $"Sig length mismatch: cDAC={*pcbSig}, DAC={cbLocal}");
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetModuleRefProps(uint mur, char* szName, uint cchName, uint* pchName)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetModuleRefProps(mur, szName, cchName, pchName) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint pchLocal = 0;
            char* szLocal = stackalloc char[(int)cchName];
            int hrLegacy = _legacyImport.GetModuleRefProps(mur, szLocal, cchName, &pchLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pchName is not null)
                    Debug.Assert(*pchName == pchLocal, $"Name length mismatch: cDAC={*pchName}, DAC={pchLocal}");
                if (szName is not null && cchName > 0)
                {
                    string cdacName = new string(szName);
                    string dacName = new string(szLocal);
                    Debug.Assert(cdacName == dacName, $"ModuleRef name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.EnumModuleRefs(nint* phEnum, uint* rModuleRefs, uint cmax, uint* pcModuleRefs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumModuleRefs(phEnum, rModuleRefs, cmax, pcModuleRefs) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumModuleRefs(phEnum, rModuleRefs, cmax, pcModuleRefs);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.GetTypeSpecFromToken(uint typespec, byte** ppvSig, uint* pcbSig)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetTypeSpecFromToken(typespec, ppvSig, pcbSig) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint cbLocal = 0;
            byte* sigLocal = null;
            int hrLegacy = _legacyImport.GetTypeSpecFromToken(typespec, &sigLocal, &cbLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (ppvSig is not null)
                    ValidateBlobsEqual(*ppvSig, pcbSig is not null ? *pcbSig : cbLocal, sigLocal, cbLocal, "TypeSpec");
                else if (pcbSig is not null)
                    Debug.Assert(*pcbSig == cbLocal, $"Sig length mismatch: cDAC={*pcbSig}, DAC={cbLocal}");
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetNameFromToken(uint tk, byte** pszUtf8NamePtr)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetNameFromToken(tk, pszUtf8NamePtr) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetNameFromToken(tk, pszUtf8NamePtr);
        return hr;
    }

    int IMetaDataImport.EnumUnresolvedMethods(nint* phEnum, uint* rMethods, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumUnresolvedMethods(phEnum, rMethods, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumUnresolvedMethods(phEnum, rMethods, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.GetUserString(uint stk, char* szString, uint cchString, uint* pchString)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetUserString(stk, szString, cchString, pchString) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint pchLocal = 0;
            char* szLocal = stackalloc char[(int)cchString];
            int hrLegacy = _legacyImport.GetUserString(stk, szLocal, cchString, &pchLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pchString is not null)
                    Debug.Assert(*pchString == pchLocal, $"String length mismatch: cDAC={*pchString}, DAC={pchLocal}");
                if (szString is not null && cchString > 0)
                {
                    // GetUserString does not null-terminate its output buffer (matching native behavior),
                    // so we must use length-bounded string construction instead of new string(char*).
                    int compareLen = Math.Min((int)pchLocal, (int)cchString);
                    string cdacStr = new string(szString, 0, compareLen);
                    string dacStr = new string(szLocal, 0, compareLen);
                    Debug.Assert(cdacStr == dacStr, $"UserString content mismatch: cDAC='{cdacStr}', DAC='{dacStr}'");
                }
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetPinvokeMap(uint tk, uint* pdwMappingFlags, char* szImportName, uint cchImportName,
        uint* pchImportName, uint* pmrImportDLL)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetPinvokeMap(tk, pdwMappingFlags, szImportName, cchImportName, pchImportName, pmrImportDLL) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetPinvokeMap(tk, pdwMappingFlags, szImportName, cchImportName, pchImportName, pmrImportDLL);
        return hr;
    }

    int IMetaDataImport.EnumSignatures(nint* phEnum, uint* rSignatures, uint cmax, uint* pcSignatures)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumSignatures(phEnum, rSignatures, cmax, pcSignatures) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumSignatures(phEnum, rSignatures, cmax, pcSignatures);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.EnumTypeSpecs(nint* phEnum, uint* rTypeSpecs, uint cmax, uint* pcTypeSpecs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumTypeSpecs(phEnum, rTypeSpecs, cmax, pcTypeSpecs) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumTypeSpecs(phEnum, rTypeSpecs, cmax, pcTypeSpecs);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.EnumUserStrings(nint* phEnum, uint* rStrings, uint cmax, uint* pcStrings)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumUserStrings(phEnum, rStrings, cmax, pcStrings) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumUserStrings(phEnum, rStrings, cmax, pcStrings);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.GetParamForMethodIndex(uint md, uint ulParamSeq, uint* ppd)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetParamForMethodIndex(md, ulParamSeq, ppd) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint pdLocal = 0;
            int hrLegacy = _legacyImport.GetParamForMethodIndex(md, ulParamSeq, &pdLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0 && ppd is not null)
                Debug.Assert(*ppd == pdLocal, $"Param token mismatch: cDAC=0x{*ppd:X}, DAC=0x{pdLocal:X}");
        }
#endif
        return hr;
    }

    int IMetaDataImport.EnumCustomAttributes(nint* phEnum, uint tk, uint tkType, uint* rCustomAttributes, uint cMax, uint* pcCustomAttributes)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.EnumCustomAttributes(phEnum, tk, tkType, rCustomAttributes, cMax, pcCustomAttributes) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
        {
            hr = _legacyImport.EnumCustomAttributes(phEnum, tk, tkType, rCustomAttributes, cMax, pcCustomAttributes);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport.GetCustomAttributeProps(uint cv, uint* ptkObj, uint* ptkType, void** ppBlob, uint* pcbSize)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetCustomAttributeProps(cv, ptkObj, ptkType, ppBlob, pcbSize) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetCustomAttributeProps(cv, ptkObj, ptkType, ppBlob, pcbSize);
        return hr;
    }

    int IMetaDataImport.FindTypeRef(uint tkResolutionScope, char* szName, uint* ptr)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.FindTypeRef(tkResolutionScope, szName, ptr) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.FindTypeRef(tkResolutionScope, szName, ptr);
        return hr;
    }

    int IMetaDataImport.GetMemberProps(uint mb, uint* pClass, char* szMember, uint cchMember, uint* pchMember,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags,
        uint* pdwCPlusTypeFlag, void** ppValue, uint* pcchValue)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetMemberProps(mb, pClass, szMember, cchMember, pchMember, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags, pdwCPlusTypeFlag, ppValue, pcchValue) : HResults.E_NOTIMPL;
        return hr;
    }

    int IMetaDataImport.GetFieldProps(uint mb, uint* pClass, char* szField, uint cchField, uint* pchField,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pdwCPlusTypeFlag,
        void** ppValue, uint* pcchValue)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetFieldProps(mb, pClass, szField, cchField, pchField, pdwAttr, ppvSigBlob, pcbSigBlob, pdwCPlusTypeFlag, ppValue, pcchValue) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint classLocal = 0, attrLocal = 0, pchLocal = 0, cbSigLocal = 0, cpTypeLocal = 0, cchValueLocal = 0;
            byte* sigLocal = null;
            void* valueLocal = null;
            char* szLocal = stackalloc char[(int)cchField];
            int hrLegacy = _legacyImport.GetFieldProps(mb, &classLocal, szLocal, cchField, &pchLocal, &attrLocal, &sigLocal, &cbSigLocal, &cpTypeLocal, &valueLocal, &cchValueLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pClass is not null)
                    Debug.Assert(*pClass == classLocal, $"Class mismatch: cDAC=0x{*pClass:X}, DAC=0x{classLocal:X}");
                if (pdwAttr is not null)
                    Debug.Assert(*pdwAttr == attrLocal, $"Attr mismatch: cDAC=0x{*pdwAttr:X}, DAC=0x{attrLocal:X}");
                if (pchField is not null)
                    Debug.Assert(*pchField == pchLocal, $"Name length mismatch: cDAC={*pchField}, DAC={pchLocal}");
                if (szField is not null && cchField > 0)
                {
                    string cdacName = new string(szField);
                    string dacName = new string(szLocal);
                    Debug.Assert(cdacName == dacName, $"Field name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
                if (pdwCPlusTypeFlag is not null)
                    Debug.Assert(*pdwCPlusTypeFlag == cpTypeLocal, $"CPlusTypeFlag mismatch: cDAC=0x{*pdwCPlusTypeFlag:X}, DAC=0x{cpTypeLocal:X}");
                if (ppvSigBlob is not null)
                    ValidateBlobsEqual(*ppvSigBlob, pcbSigBlob is not null ? *pcbSigBlob : cbSigLocal, sigLocal, cbSigLocal, "FieldSig");
                else if (pcbSigBlob is not null)
                    Debug.Assert(*pcbSigBlob == cbSigLocal, $"SigBlob length mismatch: cDAC={*pcbSigBlob}, DAC={cbSigLocal}");
                if (ppValue is not null)
                    ValidateBlobsEqual((byte*)*ppValue, pcchValue is not null ? *pcchValue : cchValueLocal, (byte*)valueLocal, cchValueLocal, "FieldConstant");
                else if (pcchValue is not null)
                    Debug.Assert(*pcchValue == cchValueLocal, $"Constant length mismatch: cDAC={*pcchValue}, DAC={cchValueLocal}");
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetPropertyProps(uint prop, uint* pClass, char* szProperty, uint cchProperty, uint* pchProperty,
        uint* pdwPropFlags, byte** ppvSig, uint* pbSig, uint* pdwCPlusTypeFlag,
        void** ppDefaultValue, uint* pcchDefaultValue, uint* pmdSetter, uint* pmdGetter,
        uint* rmdOtherMethod, uint cMax, uint* pcOtherMethod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetPropertyProps(prop, pClass, szProperty, cchProperty, pchProperty, pdwPropFlags, ppvSig, pbSig, pdwCPlusTypeFlag, ppDefaultValue, pcchDefaultValue, pmdSetter, pmdGetter, rmdOtherMethod, cMax, pcOtherMethod) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetPropertyProps(prop, pClass, szProperty, cchProperty, pchProperty, pdwPropFlags, ppvSig, pbSig, pdwCPlusTypeFlag, ppDefaultValue, pcchDefaultValue, pmdSetter, pmdGetter, rmdOtherMethod, cMax, pcOtherMethod);
        return hr;
    }

    int IMetaDataImport.GetParamProps(uint tk, uint* pmd, uint* pulSequence, char* szName, uint cchName, uint* pchName,
        uint* pdwAttr, uint* pdwCPlusTypeFlag, void** ppValue, uint* pcchValue)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetParamProps(tk, pmd, pulSequence, szName, cchName, pchName, pdwAttr, pdwCPlusTypeFlag, ppValue, pcchValue) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint mdLocal = 0, seqLocal = 0, attrLocal = 0, pchLocal = 0;
            char* szLocal = stackalloc char[(int)cchName];
            int hrLegacy = _legacyImport.GetParamProps(tk, &mdLocal, &seqLocal, szLocal, cchName, &pchLocal, &attrLocal, null, null, null);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pmd is not null)
                    Debug.Assert(*pmd == mdLocal, $"Method mismatch: cDAC=0x{*pmd:X}, DAC=0x{mdLocal:X}");
                if (pulSequence is not null)
                    Debug.Assert(*pulSequence == seqLocal, $"Sequence mismatch: cDAC={*pulSequence}, DAC={seqLocal}");
                if (pdwAttr is not null)
                    Debug.Assert(*pdwAttr == attrLocal, $"Attr mismatch: cDAC=0x{*pdwAttr:X}, DAC=0x{attrLocal:X}");
                if (pchName is not null)
                    Debug.Assert(*pchName == pchLocal, $"Name length mismatch: cDAC={*pchName}, DAC={pchLocal}");
                if (szName is not null && cchName > 0)
                {
                    string cdacName = new string(szName);
                    string dacName = new string(szLocal);
                    Debug.Assert(cdacName == dacName, $"Param name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetCustomAttributeByName(uint tkObj, char* szName, void** ppData, uint* pcbData)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetCustomAttributeByName(tkObj, szName, ppData, pcbData) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint cbLocal = 0;
            void* dataLocal = null;
            int hrLegacy = _legacyImport.GetCustomAttributeByName(tkObj, szName, &dataLocal, &cbLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (ppData is not null)
                    ValidateBlobsEqual((byte*)*ppData, pcbData is not null ? *pcbData : cbLocal, (byte*)dataLocal, cbLocal, "CustomAttribute");
                else if (pcbData is not null)
                    Debug.Assert(*pcbData == cbLocal, $"CustomAttribute length mismatch: cDAC={*pcbData}, DAC={cbLocal}");
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.IsValidToken(uint tk)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.IsValidToken(tk) : HResults.E_NOTIMPL;
        return hr;
    }

    int IMetaDataImport.GetNestedClassProps(uint tdNestedClass, uint* ptdEnclosingClass)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetNestedClassProps(tdNestedClass, ptdEnclosingClass) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport is not null)
        {
            uint enclosingLocal = 0;
            int hrLegacy = _legacyImport.GetNestedClassProps(tdNestedClass, &enclosingLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0 && ptdEnclosingClass is not null)
                Debug.Assert(*ptdEnclosingClass == enclosingLocal, $"Enclosing class mismatch: cDAC=0x{*ptdEnclosingClass:X}, DAC=0x{enclosingLocal:X}");
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetNativeCallConvFromSig(void* pvSig, uint cbSig, uint* pCallConv)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.GetNativeCallConvFromSig(pvSig, cbSig, pCallConv) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.GetNativeCallConvFromSig(pvSig, cbSig, pCallConv);
        return hr;
    }

    int IMetaDataImport.IsGlobal(uint pd, int* pbGlobal)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport is not null ? _cdacImport.IsGlobal(pd, pbGlobal) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport is not null)
            return _legacyImport.IsGlobal(pd, pbGlobal);
        return hr;
    }

    #endregion IMetaDataImport

    #region IMetaDataImport2
    int IMetaDataImport2.EnumGenericParams(nint* phEnum, uint tk, uint* rGenericParams, uint cMax, uint* pcGenericParams)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport2 is not null ? _cdacImport2.EnumGenericParams(phEnum, tk, rGenericParams, cMax, pcGenericParams) : HResults.E_NOTIMPL;
        return hr;
    }

    int IMetaDataImport2.GetGenericParamProps(uint gp, uint* pulParamSeq, uint* pdwParamFlags, uint* ptOwner,
        uint* reserved, char* wzname, uint cchName, uint* pchName)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport2 is not null ? _cdacImport2.GetGenericParamProps(gp, pulParamSeq, pdwParamFlags, ptOwner, reserved, wzname, cchName, pchName) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImport2 is not null)
        {
            uint seqLocal = 0, flagsLocal = 0, ownerLocal = 0, pchLocal = 0;
            char* szLocal = stackalloc char[(int)cchName];
            int hrLegacy = _legacyImport2.GetGenericParamProps(gp, &seqLocal, &flagsLocal, &ownerLocal, null, szLocal, cchName, &pchLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pulParamSeq is not null)
                    Debug.Assert(*pulParamSeq == seqLocal, $"ParamSeq mismatch: cDAC={*pulParamSeq}, DAC={seqLocal}");
                if (pdwParamFlags is not null)
                    Debug.Assert(*pdwParamFlags == flagsLocal, $"ParamFlags mismatch: cDAC=0x{*pdwParamFlags:X}, DAC=0x{flagsLocal:X}");
                if (ptOwner is not null)
                    Debug.Assert(*ptOwner == ownerLocal, $"Owner mismatch: cDAC=0x{*ptOwner:X}, DAC=0x{ownerLocal:X}");
                if (pchName is not null)
                    Debug.Assert(*pchName == pchLocal, $"Name length mismatch: cDAC={*pchName}, DAC={pchLocal}");
                if (wzname is not null && cchName > 0)
                {
                    string cdacName = new string(wzname);
                    string dacName = new string(szLocal);
                    Debug.Assert(cdacName == dacName, $"GenericParam name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport2.GetMethodSpecProps(uint mi, uint* tkParent, byte** ppvSigBlob, uint* pcbSigBlob)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport2 is not null ? _cdacImport2.GetMethodSpecProps(mi, tkParent, ppvSigBlob, pcbSigBlob) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport2 is not null)
            return _legacyImport2.GetMethodSpecProps(mi, tkParent, ppvSigBlob, pcbSigBlob);
        return hr;
    }

    int IMetaDataImport2.EnumGenericParamConstraints(nint* phEnum, uint tk, uint* rGenericParamConstraints, uint cMax, uint* pcGenericParamConstraints)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport2 is not null ? _cdacImport2.EnumGenericParamConstraints(phEnum, tk, rGenericParamConstraints, cMax, pcGenericParamConstraints) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport2 is not null)
        {
            hr = _legacyImport2.EnumGenericParamConstraints(phEnum, tk, rGenericParamConstraints, cMax, pcGenericParamConstraints);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataImport2.GetGenericParamConstraintProps(uint gpc, uint* ptGenericParam, uint* ptkConstraintType)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport2 is not null ? _cdacImport2.GetGenericParamConstraintProps(gpc, ptGenericParam, ptkConstraintType) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport2 is not null)
            return _legacyImport2.GetGenericParamConstraintProps(gpc, ptGenericParam, ptkConstraintType);
        return hr;
    }

    int IMetaDataImport2.GetPEKind(uint* pdwPEKind, uint* pdwMachine)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport2 is not null ? _cdacImport2.GetPEKind(pdwPEKind, pdwMachine) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport2 is not null)
            return _legacyImport2.GetPEKind(pdwPEKind, pdwMachine);
        return hr;
    }

    int IMetaDataImport2.GetVersionString(char* pwzBuf, uint ccBufSize, uint* pccBufSize)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport2 is not null ? _cdacImport2.GetVersionString(pwzBuf, ccBufSize, pccBufSize) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport2 is not null)
            return _legacyImport2.GetVersionString(pwzBuf, ccBufSize, pccBufSize);
        return hr;
    }

    int IMetaDataImport2.EnumMethodSpecs(nint* phEnum, uint tk, uint* rMethodSpecs, uint cMax, uint* pcMethodSpecs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImport2 is not null ? _cdacImport2.EnumMethodSpecs(phEnum, tk, rMethodSpecs, cMax, pcMethodSpecs) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImport2 is not null)
        {
            hr = _legacyImport2.EnumMethodSpecs(phEnum, tk, rMethodSpecs, cMax, pcMethodSpecs);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    #endregion IMetaDataImport2

    #region IMetaDataAssemblyImport
    int IMetaDataAssemblyImport.GetAssemblyProps(uint mda, byte** ppbPublicKey, uint* pcbPublicKey,
        uint* pulHashAlgId, char* szName, uint cchName, uint* pchName,
        ASSEMBLYMETADATA* pMetaData, uint* pdwAssemblyFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.GetAssemblyProps(mda, ppbPublicKey, pcbPublicKey, pulHashAlgId, szName, cchName, pchName, pMetaData, pdwAssemblyFlags) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyAssemblyImport is not null)
        {
            uint pchLocal = 0, hashAlgLocal = 0, flagsLocal = 0, cbPublicKeyLocal = 0;
            byte* publicKeyLocal = null;
            ASSEMBLYMETADATA metaLocal = default;
            char* szLocal = stackalloc char[(int)cchName];
            int hrLegacy = _legacyAssemblyImport.GetAssemblyProps(mda, &publicKeyLocal, &cbPublicKeyLocal, &hashAlgLocal, szLocal, cchName, &pchLocal, &metaLocal, &flagsLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pchName is not null)
                    Debug.Assert(*pchName == pchLocal, $"Name length mismatch: cDAC={*pchName}, DAC={pchLocal}");
                if (szName is not null && cchName > 0)
                {
                    string cdacName = new string(szName);
                    string dacName = new string(szLocal);
                    Debug.Assert(cdacName == dacName, $"Assembly name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
                if (pulHashAlgId is not null)
                    Debug.Assert(*pulHashAlgId == hashAlgLocal, $"HashAlgId mismatch: cDAC=0x{*pulHashAlgId:X}, DAC=0x{hashAlgLocal:X}");
                if (pdwAssemblyFlags is not null)
                    Debug.Assert(*pdwAssemblyFlags == flagsLocal, $"Flags mismatch: cDAC=0x{*pdwAssemblyFlags:X}, DAC=0x{flagsLocal:X}");
                if (ppbPublicKey is not null)
                    ValidateBlobsEqual(*ppbPublicKey, pcbPublicKey is not null ? *pcbPublicKey : cbPublicKeyLocal, publicKeyLocal, cbPublicKeyLocal, "AssemblyPublicKey");
                else if (pcbPublicKey is not null)
                    Debug.Assert(*pcbPublicKey == cbPublicKeyLocal, $"PublicKey length mismatch: cDAC={*pcbPublicKey}, DAC={cbPublicKeyLocal}");
                if (pMetaData is not null)
                {
                    Debug.Assert(pMetaData->usMajorVersion == metaLocal.usMajorVersion, $"MajorVersion mismatch: cDAC={pMetaData->usMajorVersion}, DAC={metaLocal.usMajorVersion}");
                    Debug.Assert(pMetaData->usMinorVersion == metaLocal.usMinorVersion, $"MinorVersion mismatch: cDAC={pMetaData->usMinorVersion}, DAC={metaLocal.usMinorVersion}");
                    Debug.Assert(pMetaData->usBuildNumber == metaLocal.usBuildNumber, $"BuildNumber mismatch: cDAC={pMetaData->usBuildNumber}, DAC={metaLocal.usBuildNumber}");
                    Debug.Assert(pMetaData->usRevisionNumber == metaLocal.usRevisionNumber, $"RevisionNumber mismatch: cDAC={pMetaData->usRevisionNumber}, DAC={metaLocal.usRevisionNumber}");
                }
            }
        }
#endif
        return hr;
    }

    int IMetaDataAssemblyImport.GetAssemblyRefProps(uint mdar, byte** ppbPublicKeyOrToken, uint* pcbPublicKeyOrToken,
        char* szName, uint cchName, uint* pchName, ASSEMBLYMETADATA* pMetaData,
        byte** ppbHashValue, uint* pcbHashValue, uint* pdwAssemblyRefFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.GetAssemblyRefProps(mdar, ppbPublicKeyOrToken, pcbPublicKeyOrToken, szName, cchName, pchName, pMetaData, ppbHashValue, pcbHashValue, pdwAssemblyRefFlags) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyAssemblyImport is not null)
        {
            uint pchLocal = 0, flagsLocal = 0, cbPublicKeyLocal = 0, cbHashLocal = 0;
            byte* publicKeyLocal = null, hashLocal = null;
            ASSEMBLYMETADATA metaLocal = default;
            char* szLocal = stackalloc char[(int)cchName];
            int hrLegacy = _legacyAssemblyImport.GetAssemblyRefProps(mdar, &publicKeyLocal, &cbPublicKeyLocal, szLocal, cchName, &pchLocal, &metaLocal, &hashLocal, &cbHashLocal, &flagsLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pchName is not null)
                    Debug.Assert(*pchName == pchLocal, $"Name length mismatch: cDAC={*pchName}, DAC={pchLocal}");
                if (szName is not null && cchName > 0)
                {
                    string cdacName = new string(szName);
                    string dacName = new string(szLocal);
                    Debug.Assert(cdacName == dacName, $"AssemblyRef name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
                if (pdwAssemblyRefFlags is not null)
                    Debug.Assert(*pdwAssemblyRefFlags == flagsLocal, $"Flags mismatch: cDAC=0x{*pdwAssemblyRefFlags:X}, DAC=0x{flagsLocal:X}");
                if (ppbPublicKeyOrToken is not null)
                    ValidateBlobsEqual(*ppbPublicKeyOrToken, pcbPublicKeyOrToken is not null ? *pcbPublicKeyOrToken : cbPublicKeyLocal, publicKeyLocal, cbPublicKeyLocal, "AssemblyRefPublicKey");
                else if (pcbPublicKeyOrToken is not null)
                    Debug.Assert(*pcbPublicKeyOrToken == cbPublicKeyLocal, $"PublicKey length mismatch: cDAC={*pcbPublicKeyOrToken}, DAC={cbPublicKeyLocal}");
                if (ppbHashValue is not null)
                    ValidateBlobsEqual(*ppbHashValue, pcbHashValue is not null ? *pcbHashValue : cbHashLocal, hashLocal, cbHashLocal, "AssemblyRefHash");
                else if (pcbHashValue is not null)
                    Debug.Assert(*pcbHashValue == cbHashLocal, $"Hash length mismatch: cDAC={*pcbHashValue}, DAC={cbHashLocal}");
                if (pMetaData is not null)
                {
                    Debug.Assert(pMetaData->usMajorVersion == metaLocal.usMajorVersion, $"MajorVersion mismatch: cDAC={pMetaData->usMajorVersion}, DAC={metaLocal.usMajorVersion}");
                    Debug.Assert(pMetaData->usMinorVersion == metaLocal.usMinorVersion, $"MinorVersion mismatch: cDAC={pMetaData->usMinorVersion}, DAC={metaLocal.usMinorVersion}");
                    Debug.Assert(pMetaData->usBuildNumber == metaLocal.usBuildNumber, $"BuildNumber mismatch: cDAC={pMetaData->usBuildNumber}, DAC={metaLocal.usBuildNumber}");
                    Debug.Assert(pMetaData->usRevisionNumber == metaLocal.usRevisionNumber, $"RevisionNumber mismatch: cDAC={pMetaData->usRevisionNumber}, DAC={metaLocal.usRevisionNumber}");
                }
            }
        }
#endif
        return hr;
    }

    int IMetaDataAssemblyImport.GetFileProps(uint mdf, char* szName, uint cchName, uint* pchName,
        byte** ppbHashValue, uint* pcbHashValue, uint* pdwFileFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.GetFileProps(mdf, szName, cchName, pchName, ppbHashValue, pcbHashValue, pdwFileFlags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyAssemblyImport is not null)
            return _legacyAssemblyImport.GetFileProps(mdf, szName, cchName, pchName, ppbHashValue, pcbHashValue, pdwFileFlags);
        return hr;
    }

    int IMetaDataAssemblyImport.GetExportedTypeProps(uint mdct, char* szName, uint cchName, uint* pchName,
        uint* ptkImplementation, uint* ptkTypeDef, uint* pdwExportedTypeFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.GetExportedTypeProps(mdct, szName, cchName, pchName, ptkImplementation, ptkTypeDef, pdwExportedTypeFlags) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyAssemblyImport is not null)
        {
            char* szNameLocal = stackalloc char[(int)cchName];
            uint pchNameLocal = 0;
            uint tkImplementationLocal = 0;
            uint tkTypeDefLocal = 0;
            uint dwExportedTypeFlagsLocal = 0;
            int hrLegacy = _legacyAssemblyImport.GetExportedTypeProps(mdct, szNameLocal, cchName, &pchNameLocal,
                &tkImplementationLocal, &tkTypeDefLocal, &dwExportedTypeFlagsLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (szName is not null && szNameLocal is not null && cchName > 0)
                {
                    string cdacName = new string(szName);
                    string dacName = new string(szNameLocal);
                    Debug.Assert(cdacName == dacName, $"ExportedType name mismatch: cDAC='{cdacName}', DAC='{dacName}'");
                }
                if (pchName is not null)
                    Debug.Assert(*pchName == pchNameLocal, $"ExportedType name length mismatch: cDAC={*pchName}, DAC={pchNameLocal}");
                if (ptkImplementation is not null)
                    Debug.Assert(*ptkImplementation == tkImplementationLocal, $"ExportedType implementation mismatch: cDAC=0x{*ptkImplementation:X}, DAC=0x{tkImplementationLocal:X}");
                if (ptkTypeDef is not null)
                    Debug.Assert(*ptkTypeDef == tkTypeDefLocal, $"ExportedType typeDef mismatch: cDAC=0x{*ptkTypeDef:X}, DAC=0x{tkTypeDefLocal:X}");
                if (pdwExportedTypeFlags is not null)
                    Debug.Assert(*pdwExportedTypeFlags == dwExportedTypeFlagsLocal, $"ExportedType flags mismatch: cDAC=0x{*pdwExportedTypeFlags:X}, DAC=0x{dwExportedTypeFlagsLocal:X}");
            }
        }
#endif
        return hr;
    }

    int IMetaDataAssemblyImport.GetManifestResourceProps(uint mdmr, char* szName, uint cchName, uint* pchName,
        uint* ptkImplementation, uint* pdwOffset, uint* pdwResourceFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.GetManifestResourceProps(mdmr, szName, cchName, pchName, ptkImplementation, pdwOffset, pdwResourceFlags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyAssemblyImport is not null)
            return _legacyAssemblyImport.GetManifestResourceProps(mdmr, szName, cchName, pchName, ptkImplementation, pdwOffset, pdwResourceFlags);
        return hr;
    }

    int IMetaDataAssemblyImport.EnumAssemblyRefs(nint* phEnum, uint* rAssemblyRefs, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.EnumAssemblyRefs(phEnum, rAssemblyRefs, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyAssemblyImport is not null)
        {
            hr = _legacyAssemblyImport.EnumAssemblyRefs(phEnum, rAssemblyRefs, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataAssemblyImport.EnumFiles(nint* phEnum, uint* rFiles, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.EnumFiles(phEnum, rFiles, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyAssemblyImport is not null)
        {
            hr = _legacyAssemblyImport.EnumFiles(phEnum, rFiles, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataAssemblyImport.EnumExportedTypes(nint* phEnum, uint* rExportedTypes, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.EnumExportedTypes(phEnum, rExportedTypes, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyAssemblyImport is not null)
        {
            hr = _legacyAssemblyImport.EnumExportedTypes(phEnum, rExportedTypes, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataAssemblyImport.EnumManifestResources(nint* phEnum, uint* rManifestResources, uint cMax, uint* pcTokens)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.EnumManifestResources(phEnum, rManifestResources, cMax, pcTokens) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyAssemblyImport is not null)
        {
            hr = _legacyAssemblyImport.EnumManifestResources(phEnum, rManifestResources, cMax, pcTokens);
            RegisterLegacyEnum(phEnum, hr);
            return hr;
        }
        return hr;
    }

    int IMetaDataAssemblyImport.GetAssemblyFromScope(uint* ptkAssembly)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.GetAssemblyFromScope(ptkAssembly) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyAssemblyImport is not null)
        {
            uint tkLocal = 0;
            int hrLegacy = _legacyAssemblyImport.GetAssemblyFromScope(&tkLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0 && ptkAssembly is not null)
                Debug.Assert(*ptkAssembly == tkLocal, $"Assembly token mismatch: cDAC=0x{*ptkAssembly:X}, DAC=0x{tkLocal:X}");
        }
#endif
        return hr;
    }

    int IMetaDataAssemblyImport.FindExportedTypeByName(char* szName, uint mdtExportedType, uint* ptkExportedType)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.FindExportedTypeByName(szName, mdtExportedType, ptkExportedType) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyAssemblyImport is not null)
        {
            uint tkExportedTypeLocal = 0;
            int hrLegacy = _legacyAssemblyImport.FindExportedTypeByName(szName, mdtExportedType, &tkExportedTypeLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0 && ptkExportedType is not null)
                Debug.Assert(*ptkExportedType == tkExportedTypeLocal, $"ExportedType mismatch: cDAC=0x{*ptkExportedType:X}, DAC=0x{tkExportedTypeLocal:X}");
        }
#endif
        return hr;
    }

    int IMetaDataAssemblyImport.FindManifestResourceByName(char* szName, uint* ptkManifestResource)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.FindManifestResourceByName(szName, ptkManifestResource) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyAssemblyImport is not null)
            return _legacyAssemblyImport.FindManifestResourceByName(szName, ptkManifestResource);
        return hr;
    }

    void IMetaDataAssemblyImport.CloseEnum(nint hEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        _cdacAssemblyImport?.CloseEnum(hEnum);
        if (_legacyEnumHandles.TryRemove(hEnum, out _))
            _legacyAssemblyImport?.CloseEnum(hEnum);
    }

    int IMetaDataAssemblyImport.FindAssembliesByName(char* szAppBase, char* szPrivateBin, char* szAssemblyName,
        nint* ppIUnk, uint cMax, uint* pcAssemblies)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacAssemblyImport is not null ? _cdacAssemblyImport.FindAssembliesByName(szAppBase, szPrivateBin, szAssemblyName, ppIUnk, cMax, pcAssemblies) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyAssemblyImport is not null)
            return _legacyAssemblyImport.FindAssembliesByName(szAppBase, szPrivateBin, szAssemblyName, ppIUnk, cMax, pcAssemblies);
        return hr;
    }

    #endregion IMetaDataAssemblyImport

}
