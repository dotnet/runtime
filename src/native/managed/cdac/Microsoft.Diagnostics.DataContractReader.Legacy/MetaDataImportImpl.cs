// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class MetaDataImportImpl : IMetaDataImport2, IMetaDataAssemblyImport
{
    private const int CLDB_E_RECORD_NOTFOUND = unchecked((int)0x80131130);
    private readonly MetadataReader? _reader;
    private readonly IMetaDataImport? _legacyImport;
    private readonly IMetaDataImport2? _legacyImport2;
    private readonly IMetaDataAssemblyImport? _legacyAssemblyImport;

    public MetaDataImportImpl(MetadataReader? reader, IMetaDataImport? legacyImport = null)
    {
        _reader = reader;
        _legacyImport = legacyImport;
        _legacyImport2 = legacyImport as IMetaDataImport2;
        _legacyAssemblyImport = legacyImport as IMetaDataAssemblyImport;
    }

    // Helper: get the full name of a type definition (Namespace.Name).
    // Only called when _reader is known non-null (after null guard).
    private string GetTypeDefFullName(TypeDefinition typeDef)
    {
        string name = _reader!.GetString(typeDef.Name);
        string ns = _reader!.GetString(typeDef.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    // Helper: get the full name of a type reference (Namespace.Name).
    // Only called when _reader is known non-null (after null guard).
    private string GetTypeRefFullName(TypeReference typeRef)
    {
        string name = _reader!.GetString(typeRef.Name);
        string ns = _reader!.GetString(typeRef.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private sealed class MetadataEnum
    {
        public List<uint> Tokens { get; }
        public int Position { get; set; }

        public MetadataEnum(List<uint> tokens)
        {
            Tokens = tokens;
        }
    }

    private static nint AllocEnum(List<uint> tokens)
    {
        MetadataEnum e = new(tokens);
        GCHandle handle = GCHandle.Alloc(e);
        return GCHandle.ToIntPtr(handle);
    }

    private static MetadataEnum? GetEnum(nint hEnum)
    {
        if (hEnum == 0)
            return null;
        GCHandle handle = GCHandle.FromIntPtr(hEnum);
        return (MetadataEnum?)handle.Target;
    }

    private static int FillEnum(nint* phEnum, List<uint> tokens, uint* rTokens, uint cMax, uint* pcTokens)
    {
        if (phEnum is null)
            return HResults.E_INVALIDARG;

        if (*phEnum == 0)
            *phEnum = AllocEnum(tokens);

        MetadataEnum e = GetEnum(*phEnum)!;
        uint count = 0;
        while (count < cMax && e.Position < e.Tokens.Count)
        {
            if (rTokens is not null)
                rTokens[count] = e.Tokens[e.Position];
            e.Position++;
            count++;
        }

        if (pcTokens is not null)
            *pcTokens = count;

        return count > 0 ? HResults.S_OK : HResults.S_FALSE;
    }

    public void CloseEnum(nint hEnum)
    {
        if (hEnum != 0)
        {
            GCHandle handle = GCHandle.FromIntPtr(hEnum);
            handle.Free();
        }
    }

    public int CountEnum(nint hEnum, uint* pulCount)
        => _legacyImport is not null ? _legacyImport.CountEnum(hEnum, pulCount) : HResults.E_NOTIMPL;

    public int ResetEnum(nint hEnum, uint ulPos)
        => _legacyImport is not null ? _legacyImport.ResetEnum(hEnum, ulPos) : HResults.E_NOTIMPL;

    public int EnumTypeDefs(nint* phEnum, uint* rTypeDefs, uint cMax, uint* pcTypeDefs)
        => _legacyImport is not null ? _legacyImport.EnumTypeDefs(phEnum, rTypeDefs, cMax, pcTypeDefs) : HResults.E_NOTIMPL;

    public int EnumInterfaceImpls(nint* phEnum, uint td, uint* rImpls, uint cMax, uint* pcImpls)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.EnumInterfaceImpls(phEnum, td, rImpls, cMax, pcImpls) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            if (phEnum is not null && *phEnum != 0)
            {
                hr = FillEnum(phEnum, GetEnum(*phEnum)!.Tokens, rImpls, cMax, pcImpls);
            }
            else
            {
                TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle((int)(td & 0x00FFFFFF));
                TypeDefinition typeDef = _reader!.GetTypeDefinition(typeHandle);
                List<uint> tokens = new();
                foreach (InterfaceImplementationHandle h in typeDef.GetInterfaceImplementations())
                    tokens.Add((uint)MetadataTokens.GetToken(h));
                hr = FillEnum(phEnum, tokens, rImpls, cMax, pcImpls);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    public int EnumTypeRefs(nint* phEnum, uint* rTypeRefs, uint cMax, uint* pcTypeRefs)
        => _legacyImport is not null ? _legacyImport.EnumTypeRefs(phEnum, rTypeRefs, cMax, pcTypeRefs) : HResults.E_NOTIMPL;

    public int EnumMembers(nint* phEnum, uint cl, uint* rMembers, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMembers(phEnum, cl, rMembers, cMax, pcTokens) : HResults.E_NOTIMPL;

    public int EnumMethods(nint* phEnum, uint cl, uint* rMethods, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMethods(phEnum, cl, rMethods, cMax, pcTokens) : HResults.E_NOTIMPL;

    public int EnumFields(nint* phEnum, uint cl, uint* rFields, uint cMax, uint* pcTokens)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.EnumFields(phEnum, cl, rFields, cMax, pcTokens) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            if (phEnum is not null && *phEnum != 0)
            {
                hr = FillEnum(phEnum, GetEnum(*phEnum)!.Tokens, rFields, cMax, pcTokens);
            }
            else
            {
                TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle((int)(cl & 0x00FFFFFF));
                TypeDefinition typeDef = _reader!.GetTypeDefinition(typeHandle);
                List<uint> tokens = new();
                foreach (FieldDefinitionHandle h in typeDef.GetFields())
                    tokens.Add((uint)MetadataTokens.GetToken(h));
                hr = FillEnum(phEnum, tokens, rFields, cMax, pcTokens);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    public int EnumCustomAttributes(nint* phEnum, uint tk, uint tkType, uint* rCustomAttributes, uint cMax, uint* pcCustomAttributes)
        => _legacyImport is not null ? _legacyImport.EnumCustomAttributes(phEnum, tk, tkType, rCustomAttributes, cMax, pcCustomAttributes) : HResults.E_NOTIMPL;

    public int EnumGenericParams(nint* phEnum, uint tk, uint* rGenericParams, uint cMax, uint* pcGenericParams)
    {
        if (_reader is null)
            return _legacyImport2 is not null ? _legacyImport2.EnumGenericParams(phEnum, tk, rGenericParams, cMax, pcGenericParams) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            if (phEnum is not null && *phEnum != 0)
            {
                hr = FillEnum(phEnum, GetEnum(*phEnum)!.Tokens, rGenericParams, cMax, pcGenericParams);
            }
            else
            {
                EntityHandle owner = MetadataTokens.EntityHandle((int)tk);
                GenericParameterHandleCollection genericParams;

                if (owner.Kind == HandleKind.TypeDefinition)
                    genericParams = _reader!.GetTypeDefinition((TypeDefinitionHandle)owner).GetGenericParameters();
                else if (owner.Kind == HandleKind.MethodDefinition)
                    genericParams = _reader!.GetMethodDefinition((MethodDefinitionHandle)owner).GetGenericParameters();
                else
                {
                    throw new ArgumentException(null, nameof(tk));
                }

                List<uint> tokens = new();
                foreach (GenericParameterHandle h in genericParams)
                    tokens.Add((uint)MetadataTokens.GetToken(h));
                hr = FillEnum(phEnum, tokens, rGenericParams, cMax, pcGenericParams);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    public int GetTypeDefProps(uint td, char* szTypeDef, uint cchTypeDef, uint* pchTypeDef, uint* pdwTypeDefFlags, uint* ptkExtends)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetTypeDefProps(td, szTypeDef, cchTypeDef, pchTypeDef, pdwTypeDefFlags, ptkExtends) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle((int)(td & 0x00FFFFFF));
            TypeDefinition typeDef = _reader!.GetTypeDefinition(typeHandle);

            string fullName = GetTypeDefFullName(typeDef);
            OutputBufferHelpers.CopyStringToBuffer(szTypeDef, cchTypeDef, pchTypeDef, fullName);

            if (pdwTypeDefFlags is not null)
                *pdwTypeDefFlags = (uint)typeDef.Attributes;

            if (ptkExtends is not null)
            {
                EntityHandle baseType = typeDef.BaseType;
                *ptkExtends = baseType.IsNil ? 0 : (uint)MetadataTokens.GetToken(baseType);
            }

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint flagsLocal = 0, extendsLocal = 0, pchLocal = 0;
            int hrLegacy = _legacyImport.GetTypeDefProps(td, null, 0, &pchLocal, &flagsLocal, &extendsLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pdwTypeDefFlags is not null)
                    Debug.Assert(*pdwTypeDefFlags == flagsLocal, $"TypeDefFlags mismatch: cDAC=0x{*pdwTypeDefFlags:X}, DAC=0x{flagsLocal:X}");
                if (ptkExtends is not null)
                    Debug.Assert(*ptkExtends == extendsLocal, $"Extends mismatch: cDAC=0x{*ptkExtends:X}, DAC=0x{extendsLocal:X}");
                if (pchTypeDef is not null)
                    Debug.Assert(*pchTypeDef == pchLocal, $"Name length mismatch: cDAC={*pchTypeDef}, DAC={pchLocal}");
            }
        }
#endif
        return hr;
    }

    public int GetTypeRefProps(uint tr, uint* ptkResolutionScope, char* szName, uint cchName, uint* pchName)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetTypeRefProps(tr, ptkResolutionScope, szName, cchName, pchName) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            TypeReferenceHandle refHandle = MetadataTokens.TypeReferenceHandle((int)(tr & 0x00FFFFFF));
            TypeReference typeRef = _reader!.GetTypeReference(refHandle);

            string fullName = GetTypeRefFullName(typeRef);
            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, fullName);

            if (ptkResolutionScope is not null)
            {
                EntityHandle scope = typeRef.ResolutionScope;
                *ptkResolutionScope = scope.IsNil ? 0 : (uint)MetadataTokens.GetToken(scope);
            }

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint scopeLocal = 0, pchLocal = 0;
            int hrLegacy = _legacyImport.GetTypeRefProps(tr, &scopeLocal, null, 0, &pchLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (ptkResolutionScope is not null)
                    Debug.Assert(*ptkResolutionScope == scopeLocal, $"ResolutionScope mismatch: cDAC=0x{*ptkResolutionScope:X}, DAC=0x{scopeLocal:X}");
                if (pchName is not null)
                    Debug.Assert(*pchName == pchLocal, $"Name length mismatch: cDAC={*pchName}, DAC={pchLocal}");
            }
        }
#endif
        return hr;
    }

    public int GetMethodProps(uint mb, uint* pClass, char* szMethod, uint cchMethod, uint* pchMethod,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetMethodProps(mb, pClass, szMethod, cchMethod, pchMethod, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            MethodDefinitionHandle methodHandle = MetadataTokens.MethodDefinitionHandle((int)(mb & 0x00FFFFFF));
            MethodDefinition methodDef = _reader!.GetMethodDefinition(methodHandle);

            string name = _reader!.GetString(methodDef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szMethod, cchMethod, pchMethod, name);

            if (pClass is not null)
                *pClass = (uint)MetadataTokens.GetToken(methodDef.GetDeclaringType());

            if (pdwAttr is not null)
                *pdwAttr = (uint)methodDef.Attributes;

            if (ppvSigBlob is not null || pcbSigBlob is not null)
            {
                BlobHandle sigHandle = methodDef.Signature;
                BlobReader blobReader = _reader!.GetBlobReader(sigHandle);
                if (ppvSigBlob is not null)
                    *ppvSigBlob = blobReader.StartPointer;
                if (pcbSigBlob is not null)
                    *pcbSigBlob = (uint)blobReader.Length;
            }

            if (pulCodeRVA is not null)
                *pulCodeRVA = (uint)methodDef.RelativeVirtualAddress;

            if (pdwImplFlags is not null)
                *pdwImplFlags = (uint)methodDef.ImplAttributes;

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint classLocal = 0, attrLocal = 0, rvaLocal = 0, implLocal = 0, pchLocal = 0;
            int hrLegacy = _legacyImport.GetMethodProps(mb, &classLocal, null, 0, &pchLocal, &attrLocal, null, null, &rvaLocal, &implLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pClass is not null)
                    Debug.Assert(*pClass == classLocal, $"Class mismatch: cDAC=0x{*pClass:X}, DAC=0x{classLocal:X}");
                if (pdwAttr is not null)
                    Debug.Assert(*pdwAttr == attrLocal, $"Attr mismatch: cDAC=0x{*pdwAttr:X}, DAC=0x{attrLocal:X}");
            }
        }
#endif
        return hr;
    }

    public int GetFieldProps(uint mb, uint* pClass, char* szField, uint cchField, uint* pchField,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pdwCPlusTypeFlag,
        void** ppValue, uint* pcchValue)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetFieldProps(mb, pClass, szField, cchField, pchField, pdwAttr, ppvSigBlob, pcbSigBlob, pdwCPlusTypeFlag, ppValue, pcchValue) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            FieldDefinitionHandle fieldHandle = MetadataTokens.FieldDefinitionHandle((int)(mb & 0x00FFFFFF));
            FieldDefinition fieldDef = _reader!.GetFieldDefinition(fieldHandle);

            string name = _reader!.GetString(fieldDef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szField, cchField, pchField, name);

            if (pClass is not null)
                *pClass = (uint)MetadataTokens.GetToken(fieldDef.GetDeclaringType());

            if (pdwAttr is not null)
                *pdwAttr = (uint)fieldDef.Attributes;

            if (ppvSigBlob is not null || pcbSigBlob is not null)
            {
                BlobHandle sigHandle = fieldDef.Signature;
                BlobReader blobReader = _reader!.GetBlobReader(sigHandle);
                if (ppvSigBlob is not null)
                    *ppvSigBlob = blobReader.StartPointer;
                if (pcbSigBlob is not null)
                    *pcbSigBlob = (uint)blobReader.Length;
            }

            if (pdwCPlusTypeFlag is not null)
                *pdwCPlusTypeFlag = 0;
            if (ppValue is not null)
                *ppValue = null;
            if (pcchValue is not null)
                *pcchValue = 0;

            ConstantHandle constHandle = fieldDef.GetDefaultValue();
            if (!constHandle.IsNil && (pdwCPlusTypeFlag is not null || ppValue is not null))
            {
                Constant constant = _reader!.GetConstant(constHandle);
                if (pdwCPlusTypeFlag is not null)
                    *pdwCPlusTypeFlag = (uint)constant.TypeCode;
                if (ppValue is not null || pcchValue is not null)
                {
                    BlobReader valueReader = _reader!.GetBlobReader(constant.Value);
                    if (ppValue is not null)
                        *ppValue = valueReader.StartPointer;
                    if (pcchValue is not null)
                        *pcchValue = (uint)valueReader.Length;
                }
            }

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint classLocal = 0, attrLocal = 0;
            int hrLegacy = _legacyImport.GetFieldProps(mb, &classLocal, null, 0, null, &attrLocal, null, null, null, null, null);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pClass is not null)
                    Debug.Assert(*pClass == classLocal, $"Class mismatch: cDAC=0x{*pClass:X}, DAC=0x{classLocal:X}");
                if (pdwAttr is not null)
                    Debug.Assert(*pdwAttr == attrLocal, $"Attr mismatch: cDAC=0x{*pdwAttr:X}, DAC=0x{attrLocal:X}");
            }
        }
#endif
        return hr;
    }

    public int GetMemberProps(uint mb, uint* pClass, char* szMember, uint cchMember, uint* pchMember,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags,
        uint* pdwCPlusTypeFlag, void** ppValue, uint* pcchValue)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetMemberProps(mb, pClass, szMember, cchMember, pchMember, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags, pdwCPlusTypeFlag, ppValue, pcchValue) : HResults.E_NOTIMPL;

        uint tableIndex = mb >> 24;
        if (tableIndex == 0x06) // MethodDef
        {
            int hr = GetMethodProps(mb, pClass, szMember, cchMember, pchMember, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags);
            if (pdwCPlusTypeFlag is not null)
                *pdwCPlusTypeFlag = 0;
            if (ppValue is not null)
                *ppValue = null;
            if (pcchValue is not null)
                *pcchValue = 0;
            return hr;
        }

        if (tableIndex == 0x04) // FieldDef
        {
            int hr = GetFieldProps(mb, pClass, szMember, cchMember, pchMember, pdwAttr, ppvSigBlob, pcbSigBlob, pdwCPlusTypeFlag, ppValue, pcchValue);
            if (pulCodeRVA is not null)
                *pulCodeRVA = 0;
            if (pdwImplFlags is not null)
                *pdwImplFlags = 0;
            return hr;
        }

        return HResults.E_INVALIDARG;
    }

    public int GetInterfaceImplProps(uint iiImpl, uint* pClass, uint* ptkIface)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetInterfaceImplProps(iiImpl, pClass, ptkIface) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            InterfaceImplementationHandle implHandle = MetadataTokens.InterfaceImplementationHandle((int)(iiImpl & 0x00FFFFFF));
            InterfaceImplementation impl = _reader!.GetInterfaceImplementation(implHandle);

            if (pClass is not null)
            {
                *pClass = 0;
                foreach (TypeDefinitionHandle tdh in _reader!.TypeDefinitions)
                {
                    TypeDefinition td = _reader!.GetTypeDefinition(tdh);
                    foreach (InterfaceImplementationHandle ih in td.GetInterfaceImplementations())
                    {
                        if (ih == implHandle)
                        {
                            *pClass = (uint)MetadataTokens.GetToken(tdh);
                            goto FoundClass;
                        }
                    }
                }
                FoundClass:;
            }

            if (ptkIface is not null)
                *ptkIface = (uint)MetadataTokens.GetToken(impl.Interface);

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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

    public int GetNestedClassProps(uint tdNestedClass, uint* ptdEnclosingClass)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetNestedClassProps(tdNestedClass, ptdEnclosingClass) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle((int)(tdNestedClass & 0x00FFFFFF));
            TypeDefinition typeDef = _reader!.GetTypeDefinition(typeHandle);
            TypeDefinitionHandle declaringType = typeDef.GetDeclaringType();

            if (ptdEnclosingClass is not null)
                *ptdEnclosingClass = declaringType.IsNil ? 0 : (uint)MetadataTokens.GetToken(declaringType);

            hr = declaringType.IsNil ? CLDB_E_RECORD_NOTFOUND : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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

    public int GetGenericParamProps(uint gp, uint* pulParamSeq, uint* pdwParamFlags, uint* ptOwner,
        uint* reserved, char* wzname, uint cchName, uint* pchName)
    {
        if (_reader is null)
            return _legacyImport2 is not null ? _legacyImport2.GetGenericParamProps(gp, pulParamSeq, pdwParamFlags, ptOwner, reserved, wzname, cchName, pchName) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            GenericParameterHandle gpHandle = MetadataTokens.GenericParameterHandle((int)(gp & 0x00FFFFFF));
            GenericParameter genericParam = _reader!.GetGenericParameter(gpHandle);

            if (pulParamSeq is not null)
                *pulParamSeq = (uint)genericParam.Index;

            if (pdwParamFlags is not null)
                *pdwParamFlags = (uint)genericParam.Attributes;

            if (ptOwner is not null)
                *ptOwner = (uint)MetadataTokens.GetToken(genericParam.Parent);

            if (reserved is not null)
                *reserved = 0;

            string name = _reader!.GetString(genericParam.Name);
            OutputBufferHelpers.CopyStringToBuffer(wzname, cchName, pchName, name);

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport2 is not null)
        {
            uint seqLocal = 0, flagsLocal = 0, ownerLocal = 0;
            int hrLegacy = _legacyImport2.GetGenericParamProps(gp, &seqLocal, &flagsLocal, &ownerLocal, null, null, 0, null);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pulParamSeq is not null)
                    Debug.Assert(*pulParamSeq == seqLocal, $"ParamSeq mismatch: cDAC={*pulParamSeq}, DAC={seqLocal}");
                if (pdwParamFlags is not null)
                    Debug.Assert(*pdwParamFlags == flagsLocal, $"ParamFlags mismatch: cDAC=0x{*pdwParamFlags:X}, DAC=0x{flagsLocal:X}");
                if (ptOwner is not null)
                    Debug.Assert(*ptOwner == ownerLocal, $"Owner mismatch: cDAC=0x{*ptOwner:X}, DAC=0x{ownerLocal:X}");
            }
        }
#endif
        return hr;
    }

    public int GetRVA(uint tk, uint* pulCodeRVA, uint* pdwImplFlags)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetRVA(tk, pulCodeRVA, pdwImplFlags) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            uint tableIndex = tk >> 24;
            if (tableIndex == 0x06) // MethodDef
            {
                MethodDefinitionHandle methodHandle = MetadataTokens.MethodDefinitionHandle((int)(tk & 0x00FFFFFF));
                MethodDefinition methodDef = _reader!.GetMethodDefinition(methodHandle);
                if (pulCodeRVA is not null)
                    *pulCodeRVA = (uint)methodDef.RelativeVirtualAddress;
                if (pdwImplFlags is not null)
                    *pdwImplFlags = (uint)methodDef.ImplAttributes;
                hr = HResults.S_OK;
            }

            if (tableIndex == 0x04) // FieldDef
            {
                FieldDefinitionHandle fieldHandle = MetadataTokens.FieldDefinitionHandle((int)(tk & 0x00FFFFFF));
                FieldDefinition fieldDef = _reader!.GetFieldDefinition(fieldHandle);
                if (pulCodeRVA is not null)
                    *pulCodeRVA = (uint)fieldDef.GetRelativeVirtualAddress();
                if (pdwImplFlags is not null)
                    *pdwImplFlags = 0;
                hr = HResults.S_OK;
            }

            hr = HResults.E_INVALIDARG;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint rvaLocal = 0, implLocal = 0;
            int hrLegacy = _legacyImport.GetRVA(tk, &rvaLocal, &implLocal);
            Debug.ValidateHResult(hr, hrLegacy);
        }
#endif
        return hr;
    }

    public int GetSigFromToken(uint mdSig, byte** ppvSig, uint* pcbSig)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetSigFromToken(mdSig, ppvSig, pcbSig) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            StandaloneSignatureHandle sigHandle = MetadataTokens.StandaloneSignatureHandle((int)(mdSig & 0x00FFFFFF));
            StandaloneSignature sig = _reader!.GetStandaloneSignature(sigHandle);
            BlobReader blobReader = _reader!.GetBlobReader(sig.Signature);

            if (ppvSig is not null)
                *ppvSig = blobReader.StartPointer;
            if (pcbSig is not null)
                *pcbSig = (uint)blobReader.Length;

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint cbLocal = 0;
            int hrLegacy = _legacyImport.GetSigFromToken(mdSig, null, &cbLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0 && pcbSig is not null)
                Debug.Assert(*pcbSig == cbLocal, $"Sig length mismatch: cDAC={*pcbSig}, DAC={cbLocal}");
        }
#endif
        return hr;
    }

    public int GetCustomAttributeByName(uint tkObj, char* szName, void** ppData, uint* pcbData)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetCustomAttributeByName(tkObj, szName, ppData, pcbData) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            if (ppData is not null)
                *ppData = null;
            if (pcbData is not null)
                *pcbData = 0;

            string targetName = new string(szName);
            EntityHandle parent = MetadataTokens.EntityHandle((int)tkObj);

            foreach (CustomAttributeHandle caHandle in _reader!.GetCustomAttributes(parent))
            {
                CustomAttribute ca = _reader!.GetCustomAttribute(caHandle);
                string attrTypeName = GetCustomAttributeTypeName(ca.Constructor);
                if (string.Equals(attrTypeName, targetName, StringComparison.Ordinal))
                {
                    BlobReader blobReader = _reader!.GetBlobReader(ca.Value);
                    if (ppData is not null)
                        *ppData = blobReader.StartPointer;
                    if (pcbData is not null)
                        *pcbData = (uint)blobReader.Length;
                    hr = HResults.S_OK;
                }
            }

            hr = HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint cbLocal = 0;
            int hrLegacy = _legacyImport.GetCustomAttributeByName(tkObj, szName, null, &cbLocal);
            Debug.ValidateHResult(hr, hrLegacy);
        }
#endif
        return hr;
    }

    public int IsValidToken(uint tk)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.IsValidToken(tk) : 0;

        int rid = (int)(tk & 0x00FFFFFF);
        int table = (int)(tk >> 24);

        if (rid == 0)
            return 0; // FALSE

        if (table < 0 || table > (int)TableIndex.CustomDebugInformation)
            return 0; // FALSE

        int rowCount = _reader!.GetTableRowCount((TableIndex)table);
        return rid <= rowCount ? 1 : 0; // TRUE or FALSE
    }

    private string GetCustomAttributeTypeName(EntityHandle constructor)
    {
        if (constructor.Kind == HandleKind.MethodDefinition)
        {
            MethodDefinition method = _reader!.GetMethodDefinition((MethodDefinitionHandle)constructor);
            TypeDefinition typeDef = _reader!.GetTypeDefinition(method.GetDeclaringType());
            return GetTypeDefFullName(typeDef);
        }
        if (constructor.Kind == HandleKind.MemberReference)
        {
            MemberReference memberRef = _reader!.GetMemberReference((MemberReferenceHandle)constructor);
            EntityHandle parent = memberRef.Parent;
            if (parent.Kind == HandleKind.TypeReference)
            {
                TypeReference typeRef = _reader!.GetTypeReference((TypeReferenceHandle)parent);
                return GetTypeRefFullName(typeRef);
            }
            if (parent.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinition typeDef = _reader!.GetTypeDefinition((TypeDefinitionHandle)parent);
                return GetTypeDefFullName(typeDef);
            }
        }
        return string.Empty;
    }

    public int FindTypeDefByName(char* szTypeDef, uint tkEnclosingClass, uint* ptd)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.FindTypeDefByName(szTypeDef, tkEnclosingClass, ptd) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            if (ptd is not null)
                *ptd = 0;

            string targetName = new string(szTypeDef);

            bool found = false;
            foreach (TypeDefinitionHandle tdh in _reader!.TypeDefinitions)
            {
                TypeDefinition typeDef = _reader!.GetTypeDefinition(tdh);
                string fullName = GetTypeDefFullName(typeDef);

                if (!string.Equals(fullName, targetName, StringComparison.Ordinal))
                    continue;

                if (tkEnclosingClass != 0)
                {
                    TypeDefinitionHandle declaringType = typeDef.GetDeclaringType();
                    if (declaringType.IsNil || (uint)MetadataTokens.GetToken(declaringType) != tkEnclosingClass)
                        continue;
                }

                if (ptd is not null)
                    *ptd = (uint)MetadataTokens.GetToken(tdh);

                found = true;
                break;
            }

            if (!found)
                hr = CLDB_E_RECORD_NOTFOUND;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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

    public int GetScopeProps(char* szName, uint cchName, uint* pchName, Guid* pmvid)
        => _legacyImport is not null ? _legacyImport.GetScopeProps(szName, cchName, pchName, pmvid) : HResults.E_NOTIMPL;

    public int GetModuleFromScope(uint* pmd)
        => _legacyImport is not null ? _legacyImport.GetModuleFromScope(pmd) : HResults.E_NOTIMPL;

    public int ResolveTypeRef(uint tr, Guid* riid, void** ppIScope, uint* ptd)
        => _legacyImport is not null ? _legacyImport.ResolveTypeRef(tr, riid, ppIScope, ptd) : HResults.E_NOTIMPL;

    public int EnumMembersWithName(nint* phEnum, uint cl, char* szName, uint* rMembers, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMembersWithName(phEnum, cl, szName, rMembers, cMax, pcTokens) : HResults.E_NOTIMPL;

    public int EnumMethodsWithName(nint* phEnum, uint cl, char* szName, uint* rMethods, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMethodsWithName(phEnum, cl, szName, rMethods, cMax, pcTokens) : HResults.E_NOTIMPL;

    public int EnumFieldsWithName(nint* phEnum, uint cl, char* szName, uint* rFields, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumFieldsWithName(phEnum, cl, szName, rFields, cMax, pcTokens) : HResults.E_NOTIMPL;

    public int EnumParams(nint* phEnum, uint mb, uint* rParams, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumParams(phEnum, mb, rParams, cMax, pcTokens) : HResults.E_NOTIMPL;

    public int EnumMemberRefs(nint* phEnum, uint tkParent, uint* rMemberRefs, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMemberRefs(phEnum, tkParent, rMemberRefs, cMax, pcTokens) : HResults.E_NOTIMPL;

    public int EnumMethodImpls(nint* phEnum, uint td, uint* rMethodBody, uint* rMethodDecl, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMethodImpls(phEnum, td, rMethodBody, rMethodDecl, cMax, pcTokens) : HResults.E_NOTIMPL;

    public int EnumPermissionSets(nint* phEnum, uint tk, uint dwActions, uint* rPermission, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumPermissionSets(phEnum, tk, dwActions, rPermission, cMax, pcTokens) : HResults.E_NOTIMPL;

    public int FindMember(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
        => _legacyImport is not null ? _legacyImport.FindMember(td, szName, pvSigBlob, cbSigBlob, pmb) : HResults.E_NOTIMPL;

    public int FindMethod(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
        => _legacyImport is not null ? _legacyImport.FindMethod(td, szName, pvSigBlob, cbSigBlob, pmb) : HResults.E_NOTIMPL;

    public int FindField(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
        => _legacyImport is not null ? _legacyImport.FindField(td, szName, pvSigBlob, cbSigBlob, pmb) : HResults.E_NOTIMPL;

    public int FindMemberRef(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmr)
        => _legacyImport is not null ? _legacyImport.FindMemberRef(td, szName, pvSigBlob, cbSigBlob, pmr) : HResults.E_NOTIMPL;

    public int GetMemberRefProps(uint mr, uint* ptk, char* szMember, uint cchMember, uint* pchMember,
        byte** ppvSigBlob, uint* pbSig)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetMemberRefProps(mr, ptk, szMember, cchMember, pchMember, ppvSigBlob, pbSig) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            MemberReferenceHandle refHandle = MetadataTokens.MemberReferenceHandle((int)(mr & 0x00FFFFFF));
            MemberReference memberRef = _reader!.GetMemberReference(refHandle);

            string name = _reader!.GetString(memberRef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szMember, cchMember, pchMember, name);

            if (ptk is not null)
                *ptk = (uint)MetadataTokens.GetToken(memberRef.Parent);

            if (ppvSigBlob is not null || pbSig is not null)
            {
                BlobReader blobReader = _reader!.GetBlobReader(memberRef.Signature);
                if (ppvSigBlob is not null)
                    *ppvSigBlob = blobReader.StartPointer;
                if (pbSig is not null)
                    *pbSig = (uint)blobReader.Length;
            }

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint tkLocal = 0;
            int hrLegacy = _legacyImport.GetMemberRefProps(mr, &tkLocal, null, 0, null, null, null);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0 && ptk is not null)
                Debug.Assert(*ptk == tkLocal, $"Parent mismatch: cDAC=0x{*ptk:X}, DAC=0x{tkLocal:X}");
        }
#endif
        return hr;
    }

    public int EnumProperties(nint* phEnum, uint td, uint* rProperties, uint cMax, uint* pcProperties)
        => _legacyImport is not null ? _legacyImport.EnumProperties(phEnum, td, rProperties, cMax, pcProperties) : HResults.E_NOTIMPL;

    public int EnumEvents(nint* phEnum, uint td, uint* rEvents, uint cMax, uint* pcEvents)
        => _legacyImport is not null ? _legacyImport.EnumEvents(phEnum, td, rEvents, cMax, pcEvents) : HResults.E_NOTIMPL;

    public int GetEventProps(uint ev, uint* pClass, char* szEvent, uint cchEvent, uint* pchEvent,
        uint* pdwEventFlags, uint* ptkEventType, uint* pmdAddOn, uint* pmdRemoveOn, uint* pmdFire,
        uint* rmdOtherMethod, uint cMax, uint* pcOtherMethod)
        => _legacyImport is not null ? _legacyImport.GetEventProps(ev, pClass, szEvent, cchEvent, pchEvent, pdwEventFlags, ptkEventType, pmdAddOn, pmdRemoveOn, pmdFire, rmdOtherMethod, cMax, pcOtherMethod) : HResults.E_NOTIMPL;

    public int EnumMethodSemantics(nint* phEnum, uint mb, uint* rEventProp, uint cMax, uint* pcEventProp)
        => _legacyImport is not null ? _legacyImport.EnumMethodSemantics(phEnum, mb, rEventProp, cMax, pcEventProp) : HResults.E_NOTIMPL;

    public int GetMethodSemantics(uint mb, uint tkEventProp, uint* pdwSemanticsFlags)
        => _legacyImport is not null ? _legacyImport.GetMethodSemantics(mb, tkEventProp, pdwSemanticsFlags) : HResults.E_NOTIMPL;

    public int GetClassLayout(uint td, uint* pdwPackSize, void* rFieldOffset, uint cMax, uint* pcFieldOffset, uint* pulClassSize)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetClassLayout(td, pdwPackSize, rFieldOffset, cMax, pcFieldOffset, pulClassSize) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle((int)(td & 0x00FFFFFF));
            TypeLayout layout = _reader!.GetTypeDefinition(typeHandle).GetLayout();

            if (layout.IsDefault)
            {
                hr = CLDB_E_RECORD_NOTFOUND;
            }
            else
            {
                if (pdwPackSize is not null)
                    *pdwPackSize = (uint)layout.PackingSize;

                if (pulClassSize is not null)
                    *pulClassSize = (uint)layout.Size;

                if (pcFieldOffset is not null)
                    *pcFieldOffset = 0;

                hr = HResults.S_OK;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint packLocal = 0, sizeLocal = 0;
            int hrLegacy = _legacyImport.GetClassLayout(td, &packLocal, null, 0, null, &sizeLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pdwPackSize is not null)
                    Debug.Assert(*pdwPackSize == packLocal, $"PackSize mismatch: cDAC={*pdwPackSize}, DAC={packLocal}");
                if (pulClassSize is not null)
                    Debug.Assert(*pulClassSize == sizeLocal, $"ClassSize mismatch: cDAC={*pulClassSize}, DAC={sizeLocal}");
            }
        }
#endif
        return hr;
    }

    public int GetFieldMarshal(uint tk, byte** ppvNativeType, uint* pcbNativeType)
        => _legacyImport is not null ? _legacyImport.GetFieldMarshal(tk, ppvNativeType, pcbNativeType) : HResults.E_NOTIMPL;

    public int GetPermissionSetProps(uint pm, uint* pdwAction, void** ppvPermission, uint* pcbPermission)
        => _legacyImport is not null ? _legacyImport.GetPermissionSetProps(pm, pdwAction, ppvPermission, pcbPermission) : HResults.E_NOTIMPL;

    public int GetModuleRefProps(uint mur, char* szName, uint cchName, uint* pchName)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetModuleRefProps(mur, szName, cchName, pchName) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            ModuleReferenceHandle modRefHandle = MetadataTokens.ModuleReferenceHandle((int)(mur & 0x00FFFFFF));
            ModuleReference modRef = _reader!.GetModuleReference(modRefHandle);

            string name = _reader!.GetString(modRef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, name);

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint pchLocal = 0;
            int hrLegacy = _legacyImport.GetModuleRefProps(mur, null, 0, &pchLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0 && pchName is not null)
                Debug.Assert(*pchName == pchLocal, $"Name length mismatch: cDAC={*pchName}, DAC={pchLocal}");
        }
#endif
        return hr;
    }

    public int EnumModuleRefs(nint* phEnum, uint* rModuleRefs, uint cmax, uint* pcModuleRefs)
        => _legacyImport is not null ? _legacyImport.EnumModuleRefs(phEnum, rModuleRefs, cmax, pcModuleRefs) : HResults.E_NOTIMPL;

    public int GetTypeSpecFromToken(uint typespec, byte** ppvSig, uint* pcbSig)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetTypeSpecFromToken(typespec, ppvSig, pcbSig) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            TypeSpecificationHandle tsHandle = MetadataTokens.TypeSpecificationHandle((int)(typespec & 0x00FFFFFF));
            TypeSpecification typeSpec = _reader!.GetTypeSpecification(tsHandle);
            BlobReader blobReader = _reader!.GetBlobReader(typeSpec.Signature);

            if (ppvSig is not null)
                *ppvSig = blobReader.StartPointer;
            if (pcbSig is not null)
                *pcbSig = (uint)blobReader.Length;

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint cbLocal = 0;
            int hrLegacy = _legacyImport.GetTypeSpecFromToken(typespec, null, &cbLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0 && pcbSig is not null)
                Debug.Assert(*pcbSig == cbLocal, $"Sig length mismatch: cDAC={*pcbSig}, DAC={cbLocal}");
        }
#endif
        return hr;
    }

    public int GetNameFromToken(uint tk, byte** pszUtf8NamePtr)
        => _legacyImport is not null ? _legacyImport.GetNameFromToken(tk, pszUtf8NamePtr) : HResults.E_NOTIMPL;

    public int EnumUnresolvedMethods(nint* phEnum, uint* rMethods, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumUnresolvedMethods(phEnum, rMethods, cMax, pcTokens) : HResults.E_NOTIMPL;

    public int GetUserString(uint stk, char* szString, uint cchString, uint* pchString)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetUserString(stk, szString, cchString, pchString) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            UserStringHandle usHandle = MetadataTokens.UserStringHandle((int)(stk & 0x00FFFFFF));
            string value = _reader!.GetUserString(usHandle);
            OutputBufferHelpers.CopyStringToBuffer(szString, cchString, pchString, value);

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint pchLocal = 0;
            int hrLegacy = _legacyImport.GetUserString(stk, null, 0, &pchLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0 && pchString is not null)
                Debug.Assert(*pchString == pchLocal, $"String length mismatch: cDAC={*pchString}, DAC={pchLocal}");
        }
#endif
        return hr;
    }

    public int GetPinvokeMap(uint tk, uint* pdwMappingFlags, char* szImportName, uint cchImportName,
        uint* pchImportName, uint* pmrImportDLL)
        => _legacyImport is not null ? _legacyImport.GetPinvokeMap(tk, pdwMappingFlags, szImportName, cchImportName, pchImportName, pmrImportDLL) : HResults.E_NOTIMPL;

    public int EnumSignatures(nint* phEnum, uint* rSignatures, uint cmax, uint* pcSignatures)
        => _legacyImport is not null ? _legacyImport.EnumSignatures(phEnum, rSignatures, cmax, pcSignatures) : HResults.E_NOTIMPL;

    public int EnumTypeSpecs(nint* phEnum, uint* rTypeSpecs, uint cmax, uint* pcTypeSpecs)
        => _legacyImport is not null ? _legacyImport.EnumTypeSpecs(phEnum, rTypeSpecs, cmax, pcTypeSpecs) : HResults.E_NOTIMPL;

    public int EnumUserStrings(nint* phEnum, uint* rStrings, uint cmax, uint* pcStrings)
        => _legacyImport is not null ? _legacyImport.EnumUserStrings(phEnum, rStrings, cmax, pcStrings) : HResults.E_NOTIMPL;

    public int GetParamForMethodIndex(uint md, uint ulParamSeq, uint* ppd)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetParamForMethodIndex(md, ulParamSeq, ppd) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            if (ppd is not null)
                *ppd = 0;

            MethodDefinitionHandle methodHandle = MetadataTokens.MethodDefinitionHandle((int)(md & 0x00FFFFFF));
            MethodDefinition methodDef = _reader!.GetMethodDefinition(methodHandle);

            bool found = false;
            foreach (ParameterHandle ph in methodDef.GetParameters())
            {
                Parameter param = _reader!.GetParameter(ph);
                if (param.SequenceNumber == (int)ulParamSeq)
                {
                    if (ppd is not null)
                        *ppd = (uint)MetadataTokens.GetToken(ph);
                    found = true;
                    break;
                }
            }

            if (!found)
                hr = CLDB_E_RECORD_NOTFOUND;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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

    public int GetCustomAttributeProps(uint cv, uint* ptkObj, uint* ptkType, void** ppBlob, uint* pcbSize)
        => _legacyImport is not null ? _legacyImport.GetCustomAttributeProps(cv, ptkObj, ptkType, ppBlob, pcbSize) : HResults.E_NOTIMPL;

    public int FindTypeRef(uint tkResolutionScope, char* szName, uint* ptr)
        => _legacyImport is not null ? _legacyImport.FindTypeRef(tkResolutionScope, szName, ptr) : HResults.E_NOTIMPL;

    public int GetPropertyProps(uint prop, uint* pClass, char* szProperty, uint cchProperty, uint* pchProperty,
        uint* pdwPropFlags, byte** ppvSig, uint* pbSig, uint* pdwCPlusTypeFlag,
        void** ppDefaultValue, uint* pcchDefaultValue, uint* pmdSetter, uint* pmdGetter,
        uint* rmdOtherMethod, uint cMax, uint* pcOtherMethod)
        => _legacyImport is not null ? _legacyImport.GetPropertyProps(prop, pClass, szProperty, cchProperty, pchProperty, pdwPropFlags, ppvSig, pbSig, pdwCPlusTypeFlag, ppDefaultValue, pcchDefaultValue, pmdSetter, pmdGetter, rmdOtherMethod, cMax, pcOtherMethod) : HResults.E_NOTIMPL;

    public int GetParamProps(uint tk, uint* pmd, uint* pulSequence, char* szName, uint cchName, uint* pchName,
        uint* pdwAttr, uint* pdwCPlusTypeFlag, void** ppValue, uint* pcchValue)
    {
        if (_reader is null)
            return _legacyImport is not null ? _legacyImport.GetParamProps(tk, pmd, pulSequence, szName, cchName, pchName, pdwAttr, pdwCPlusTypeFlag, ppValue, pcchValue) : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            ParameterHandle paramHandle = MetadataTokens.ParameterHandle((int)(tk & 0x00FFFFFF));
            Parameter param = _reader!.GetParameter(paramHandle);

            string name = _reader!.GetString(param.Name);
            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, name);

            if (pmd is not null)
            {
                *pmd = 0;
                foreach (TypeDefinitionHandle tdh in _reader!.TypeDefinitions)
                {
                    TypeDefinition td = _reader!.GetTypeDefinition(tdh);
                    foreach (MethodDefinitionHandle mdh in td.GetMethods())
                    {
                        MethodDefinition method = _reader!.GetMethodDefinition(mdh);
                        foreach (ParameterHandle ph in method.GetParameters())
                        {
                            if (ph == paramHandle)
                            {
                                *pmd = (uint)MetadataTokens.GetToken(mdh);
                                goto FoundMethod;
                            }
                        }
                    }
                }
                FoundMethod:;
            }

            if (pulSequence is not null)
                *pulSequence = (uint)param.SequenceNumber;

            if (pdwAttr is not null)
                *pdwAttr = (uint)param.Attributes;

            if (pdwCPlusTypeFlag is not null)
                *pdwCPlusTypeFlag = 0;
            if (ppValue is not null)
                *ppValue = null;
            if (pcchValue is not null)
                *pcchValue = 0;

            ConstantHandle constHandle = param.GetDefaultValue();
            if (!constHandle.IsNil && (pdwCPlusTypeFlag is not null || ppValue is not null))
            {
                Constant constant = _reader!.GetConstant(constHandle);
                if (pdwCPlusTypeFlag is not null)
                    *pdwCPlusTypeFlag = (uint)constant.TypeCode;
                if (ppValue is not null || pcchValue is not null)
                {
                    BlobReader valueReader = _reader!.GetBlobReader(constant.Value);
                    if (ppValue is not null)
                        *ppValue = valueReader.StartPointer;
                    if (pcchValue is not null)
                        *pcchValue = (uint)valueReader.Length;
                }
            }

            hr = HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImport is not null)
        {
            uint mdLocal = 0, seqLocal = 0, attrLocal = 0;
            int hrLegacy = _legacyImport.GetParamProps(tk, &mdLocal, &seqLocal, null, 0, null, &attrLocal, null, null, null);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pmd is not null)
                    Debug.Assert(*pmd == mdLocal, $"Method mismatch: cDAC=0x{*pmd:X}, DAC=0x{mdLocal:X}");
                if (pulSequence is not null)
                    Debug.Assert(*pulSequence == seqLocal, $"Sequence mismatch: cDAC={*pulSequence}, DAC={seqLocal}");
                if (pdwAttr is not null)
                    Debug.Assert(*pdwAttr == attrLocal, $"Attr mismatch: cDAC=0x{*pdwAttr:X}, DAC=0x{attrLocal:X}");
            }
        }
#endif
        return hr;
    }

    public int GetNativeCallConvFromSig(void* pvSig, uint cbSig, uint* pCallConv)
        => _legacyImport is not null ? _legacyImport.GetNativeCallConvFromSig(pvSig, cbSig, pCallConv) : HResults.E_NOTIMPL;

    public int IsGlobal(uint pd, int* pbGlobal)
        => _legacyImport is not null ? _legacyImport.IsGlobal(pd, pbGlobal) : HResults.E_NOTIMPL;

    // IMetaDataImport2 methods — delegate to legacy via _legacyImport2
    public int GetMethodSpecProps(uint mi, uint* tkParent, byte** ppvSigBlob, uint* pcbSigBlob)
        => _legacyImport2 is not null ? _legacyImport2.GetMethodSpecProps(mi, tkParent, ppvSigBlob, pcbSigBlob) : HResults.E_NOTIMPL;

    public int EnumGenericParamConstraints(nint* phEnum, uint tk, uint* rGenericParamConstraints, uint cMax, uint* pcGenericParamConstraints)
        => _legacyImport2 is not null ? _legacyImport2.EnumGenericParamConstraints(phEnum, tk, rGenericParamConstraints, cMax, pcGenericParamConstraints) : HResults.E_NOTIMPL;

    public int GetGenericParamConstraintProps(uint gpc, uint* ptGenericParam, uint* ptkConstraintType)
        => _legacyImport2 is not null ? _legacyImport2.GetGenericParamConstraintProps(gpc, ptGenericParam, ptkConstraintType) : HResults.E_NOTIMPL;

    public int GetPEKind(uint* pdwPEKind, uint* pdwMachine)
        => _legacyImport2 is not null ? _legacyImport2.GetPEKind(pdwPEKind, pdwMachine) : HResults.E_NOTIMPL;

    public int GetVersionString(char* pwzBuf, uint ccBufSize, uint* pccBufSize)
        => _legacyImport2 is not null ? _legacyImport2.GetVersionString(pwzBuf, ccBufSize, pccBufSize) : HResults.E_NOTIMPL;

    public int EnumMethodSpecs(nint* phEnum, uint tk, uint* rMethodSpecs, uint cMax, uint* pcMethodSpecs)
        => _legacyImport2 is not null ? _legacyImport2.EnumMethodSpecs(phEnum, tk, rMethodSpecs, cMax, pcMethodSpecs) : HResults.E_NOTIMPL;

    // =============================================
    // IMetaDataAssemblyImport
    // =============================================

    int IMetaDataAssemblyImport.GetAssemblyProps(uint mda, byte** ppbPublicKey, uint* pcbPublicKey,
        uint* pulHashAlgId, char* szName, uint cchName, uint* pchName,
        ASSEMBLYMETADATA* pMetaData, uint* pdwAssemblyFlags)
    {
        if (_reader is null)
            return _legacyAssemblyImport is not null
                ? _legacyAssemblyImport.GetAssemblyProps(mda, ppbPublicKey, pcbPublicKey, pulHashAlgId, szName, cchName, pchName, pMetaData, pdwAssemblyFlags)
                : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            // Validate that the token is the assembly definition token
            if (mda != 0x20000001)
                throw Marshal.GetExceptionForHR(CLDB_E_RECORD_NOTFOUND)!;

            AssemblyDefinition assemblyDef = _reader.GetAssemblyDefinition();
            string name = _reader.GetString(assemblyDef.Name);

            if (pchName is not null)
                *pchName = (uint)(name.Length + 1);

            if (szName is not null && cchName > 0)
            {
                int copyLen = Math.Min(name.Length, (int)cchName - 1);
                name.AsSpan(0, copyLen).CopyTo(new Span<char>(szName, copyLen));
                szName[copyLen] = '\0';
            }

            if (ppbPublicKey is not null)
                *ppbPublicKey = null;
            if (pcbPublicKey is not null)
                *pcbPublicKey = 0;
            if (pulHashAlgId is not null)
                *pulHashAlgId = (uint)assemblyDef.HashAlgorithm;

            if (pMetaData is not null)
            {
                System.Version version = assemblyDef.Version;
                pMetaData->usMajorVersion = (ushort)version.Major;
                pMetaData->usMinorVersion = (ushort)version.Minor;
                pMetaData->usBuildNumber = (ushort)version.Build;
                pMetaData->usRevisionNumber = (ushort)version.Revision;

                string culture = _reader.GetString(assemblyDef.Culture);
                if (pMetaData->szLocale is not null && pMetaData->cbLocale > 0)
                {
                    int locCopyLen = Math.Min(culture.Length, (int)pMetaData->cbLocale - 1);
                    culture.AsSpan(0, locCopyLen).CopyTo(new Span<char>(pMetaData->szLocale, locCopyLen));
                    pMetaData->szLocale[locCopyLen] = '\0';
                }
                pMetaData->cbLocale = (uint)(culture.Length + 1);
                pMetaData->ulProcessor = 0;
                pMetaData->ulOS = 0;
            }

            if (pdwAssemblyFlags is not null)
                *pdwAssemblyFlags = (uint)assemblyDef.Flags;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyAssemblyImport is not null)
        {
            uint pchLocal = 0, hashAlgLocal = 0, flagsLocal = 0;
            ASSEMBLYMETADATA metaLocal = default;
            int hrLegacy = _legacyAssemblyImport.GetAssemblyProps(mda, null, null, &hashAlgLocal, null, 0, &pchLocal, &metaLocal, &flagsLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pchName is not null)
                    Debug.Assert(*pchName == pchLocal, $"Name length mismatch: cDAC={*pchName}, DAC={pchLocal}");
                if (pulHashAlgId is not null)
                    Debug.Assert(*pulHashAlgId == hashAlgLocal, $"HashAlgId mismatch: cDAC=0x{*pulHashAlgId:X}, DAC=0x{hashAlgLocal:X}");
                if (pdwAssemblyFlags is not null)
                    Debug.Assert(*pdwAssemblyFlags == flagsLocal, $"Flags mismatch: cDAC=0x{*pdwAssemblyFlags:X}, DAC=0x{flagsLocal:X}");
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
        if (_reader is null)
            return _legacyAssemblyImport is not null
                ? _legacyAssemblyImport.GetAssemblyRefProps(mdar, ppbPublicKeyOrToken, pcbPublicKeyOrToken, szName, cchName, pchName, pMetaData, ppbHashValue, pcbHashValue, pdwAssemblyRefFlags)
                : HResults.E_NOTIMPL;

        int hr = HResults.S_OK;
        try
        {
            AssemblyReferenceHandle refHandle = MetadataTokens.AssemblyReferenceHandle((int)(mdar & 0x00FFFFFF));
            AssemblyReference assemblyRef = _reader.GetAssemblyReference(refHandle);
            string name = _reader.GetString(assemblyRef.Name);

            if (pchName is not null)
                *pchName = (uint)(name.Length + 1);

            if (szName is not null && cchName > 0)
            {
                int copyLen = Math.Min(name.Length, (int)cchName - 1);
                name.AsSpan(0, copyLen).CopyTo(new Span<char>(szName, copyLen));
                szName[copyLen] = '\0';
            }

            if (ppbPublicKeyOrToken is not null)
                *ppbPublicKeyOrToken = null;
            if (pcbPublicKeyOrToken is not null)
                *pcbPublicKeyOrToken = 0;

            if (pMetaData is not null)
            {
                System.Version version = assemblyRef.Version;
                pMetaData->usMajorVersion = (ushort)version.Major;
                pMetaData->usMinorVersion = (ushort)version.Minor;
                pMetaData->usBuildNumber = (ushort)version.Build;
                pMetaData->usRevisionNumber = (ushort)version.Revision;

                string culture = _reader.GetString(assemblyRef.Culture);
                if (pMetaData->szLocale is not null && pMetaData->cbLocale > 0)
                {
                    int locCopyLen = Math.Min(culture.Length, (int)pMetaData->cbLocale - 1);
                    culture.AsSpan(0, locCopyLen).CopyTo(new Span<char>(pMetaData->szLocale, locCopyLen));
                    pMetaData->szLocale[locCopyLen] = '\0';
                }
                pMetaData->cbLocale = (uint)(culture.Length + 1);
                pMetaData->ulProcessor = 0;
                pMetaData->ulOS = 0;
            }

            if (ppbHashValue is not null)
                *ppbHashValue = null;
            if (pcbHashValue is not null)
                *pcbHashValue = 0;

            if (pdwAssemblyRefFlags is not null)
                *pdwAssemblyRefFlags = (uint)assemblyRef.Flags;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyAssemblyImport is not null)
        {
            uint pchLocal = 0, flagsLocal = 0;
            ASSEMBLYMETADATA metaLocal = default;
            int hrLegacy = _legacyAssemblyImport.GetAssemblyRefProps(mdar, null, null, null, 0, &pchLocal, &metaLocal, null, null, &flagsLocal);
            Debug.ValidateHResult(hr, hrLegacy);
            if (hr >= 0 && hrLegacy >= 0)
            {
                if (pchName is not null)
                    Debug.Assert(*pchName == pchLocal, $"Name length mismatch: cDAC={*pchName}, DAC={pchLocal}");
                if (pdwAssemblyRefFlags is not null)
                    Debug.Assert(*pdwAssemblyRefFlags == flagsLocal, $"Flags mismatch: cDAC=0x{*pdwAssemblyRefFlags:X}, DAC=0x{flagsLocal:X}");
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
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.GetFileProps(mdf, szName, cchName, pchName, ppbHashValue, pcbHashValue, pdwFileFlags) : HResults.E_NOTIMPL;

    int IMetaDataAssemblyImport.GetExportedTypeProps(uint mdct, char* szName, uint cchName, uint* pchName,
        uint* ptkImplementation, uint* ptkTypeDef, uint* pdwExportedTypeFlags)
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.GetExportedTypeProps(mdct, szName, cchName, pchName, ptkImplementation, ptkTypeDef, pdwExportedTypeFlags) : HResults.E_NOTIMPL;

    int IMetaDataAssemblyImport.GetManifestResourceProps(uint mdmr, char* szName, uint cchName, uint* pchName,
        uint* ptkImplementation, uint* pdwOffset, uint* pdwResourceFlags)
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.GetManifestResourceProps(mdmr, szName, cchName, pchName, ptkImplementation, pdwOffset, pdwResourceFlags) : HResults.E_NOTIMPL;

    int IMetaDataAssemblyImport.EnumAssemblyRefs(nint* phEnum, uint* rAssemblyRefs, uint cMax, uint* pcTokens)
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.EnumAssemblyRefs(phEnum, rAssemblyRefs, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataAssemblyImport.EnumFiles(nint* phEnum, uint* rFiles, uint cMax, uint* pcTokens)
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.EnumFiles(phEnum, rFiles, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataAssemblyImport.EnumExportedTypes(nint* phEnum, uint* rExportedTypes, uint cMax, uint* pcTokens)
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.EnumExportedTypes(phEnum, rExportedTypes, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataAssemblyImport.EnumManifestResources(nint* phEnum, uint* rManifestResources, uint cMax, uint* pcTokens)
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.EnumManifestResources(phEnum, rManifestResources, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataAssemblyImport.GetAssemblyFromScope(uint* ptkAssembly)
    {
        if (_reader is null)
            return _legacyAssemblyImport is not null ? _legacyAssemblyImport.GetAssemblyFromScope(ptkAssembly) : HResults.E_NOTIMPL;

        if (ptkAssembly is not null)
            *ptkAssembly = 0x20000001; // TokenFromRid(1, mdtAssembly)

        int hr = HResults.S_OK;
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
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.FindExportedTypeByName(szName, mdtExportedType, ptkExportedType) : HResults.E_NOTIMPL;

    int IMetaDataAssemblyImport.FindManifestResourceByName(char* szName, uint* ptkManifestResource)
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.FindManifestResourceByName(szName, ptkManifestResource) : HResults.E_NOTIMPL;

    void IMetaDataAssemblyImport.CloseEnum(nint hEnum)
        => ((IMetaDataImport)this).CloseEnum(hEnum);

    int IMetaDataAssemblyImport.FindAssembliesByName(char* szAppBase, char* szPrivateBin, char* szAssemblyName,
        nint* ppIUnk, uint cMax, uint* pcAssemblies)
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.FindAssembliesByName(szAppBase, szPrivateBin, szAssemblyName, ppIUnk, cMax, pcAssemblies) : HResults.E_NOTIMPL;

}
