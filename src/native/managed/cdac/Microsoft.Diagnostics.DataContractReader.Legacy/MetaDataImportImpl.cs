// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class MetaDataImportImpl : ICustomQueryInterface, IMetaDataImport2, IMetaDataAssemblyImport
{
    private readonly MetadataReader _reader;
    private readonly IMetaDataImport? _legacyImport;
    private readonly IMetaDataImport2? _legacyImport2;
    private readonly IMetaDataAssemblyImport? _legacyAssemblyImport;
    private Dictionary<int, uint>? _interfaceImplToTypeDef;
    private Dictionary<int, uint>? _paramToMethod;

    private static int GetRID(uint token) => (int)EcmaMetadataUtils.GetRowId(token);

    // Tracks GCHandle values allocated by AllocEnum so that CountEnum, ResetEnum,
    // and CloseEnum can distinguish cDAC-created enum handles from legacy HENUMInternal*.
    // ConcurrentDictionary is used because COM objects may be called from multiple threads.
    private readonly ConcurrentDictionary<nint, byte> _cdacEnumHandles = new();

    public MetaDataImportImpl(MetadataReader reader, IMetaDataImport? legacyImport = null)
    {
        _reader = reader;
        _legacyImport = legacyImport;
        _legacyImport2 = legacyImport as IMetaDataImport2;
        _legacyAssemblyImport = legacyImport as IMetaDataAssemblyImport;
    }

    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out nint ppv)
    {
        ppv = default;

        if (iid == typeof(IMetaDataImport).GUID)
        {
            // ConvertToUnmanaged returns an already-AddRef'd IMetaDataImport2 COM pointer.
            // Return it directly so consumers get the full IMetaDataImport2 vtable.
            ppv = (nint)ComInterfaceMarshaller<IMetaDataImport2>.ConvertToUnmanaged(this);
            return CustomQueryInterfaceResult.Handled;
        }

        return CustomQueryInterfaceResult.NotHandled;
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

    private nint AllocEnum(List<uint> tokens)
    {
        MetadataEnum e = new(tokens);
        GCHandle handle = GCHandle.Alloc(e);
        nint ptr = GCHandle.ToIntPtr(handle);
        _cdacEnumHandles.TryAdd(ptr, 0);
        return ptr;
    }

    private MetadataEnum GetEnum(nint hEnum)
    {
        if (hEnum == 0 || !_cdacEnumHandles.ContainsKey(hEnum))
            throw new ArgumentException("Invalid enum handle.", nameof(hEnum));
        GCHandle handle = GCHandle.FromIntPtr(hEnum);
        return (MetadataEnum)(handle.Target ?? throw new ArgumentException("Enum handle target is null.", nameof(hEnum)));
    }

    private int FillEnum(nint* phEnum, List<uint> tokens, uint* rTokens, uint cMax, uint* pcTokens)
    {
        if (phEnum is null)
            return HResults.E_INVALIDARG;

        if (*phEnum == 0)
            *phEnum = AllocEnum(tokens);

        MetadataEnum e = GetEnum(*phEnum);
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

    void IMetaDataImport.CloseEnum(nint hEnum)
    {
        if (hEnum == 0)
            return;

        if (_cdacEnumHandles.TryRemove(hEnum, out _))
        {
            GCHandle handle = GCHandle.FromIntPtr(hEnum);
            handle.Free();
        }
        else
        {
            _legacyImport?.CloseEnum(hEnum);
        }
    }

    int IMetaDataImport.CountEnum(nint hEnum, uint* pulCount)
    {
        if (hEnum == 0)
        {
            if (pulCount is not null)
                *pulCount = 0;
            return HResults.S_OK;
        }

        if (_cdacEnumHandles.ContainsKey(hEnum))
        {
            MetadataEnum e = GetEnum(hEnum);
            if (pulCount is not null)
                *pulCount = (uint)e.Tokens.Count;
            return HResults.S_OK;
        }

        return _legacyImport is not null ? _legacyImport.CountEnum(hEnum, pulCount) : HResults.E_NOTIMPL;
    }

    int IMetaDataImport.ResetEnum(nint hEnum, uint ulPos)
    {
        if (hEnum == 0)
            return HResults.S_OK;

        if (_cdacEnumHandles.ContainsKey(hEnum))
        {
            MetadataEnum e = GetEnum(hEnum);
            e.Position = (int)Math.Min(ulPos, (uint)e.Tokens.Count);
            return HResults.S_OK;
        }

        return _legacyImport is not null ? _legacyImport.ResetEnum(hEnum, ulPos) : HResults.E_NOTIMPL;
    }

    int IMetaDataImport.EnumTypeDefs(nint* phEnum, uint* rTypeDefs, uint cMax, uint* pcTypeDefs)
    {
        int hr = HResults.S_OK;
        List<uint>? tokens = null;
        try
        {
            if (phEnum is not null && *phEnum != 0)
            {
                hr = FillEnum(phEnum, GetEnum(*phEnum).Tokens, rTypeDefs, cMax, pcTypeDefs);
            }
            else
            {
                tokens = new();
                foreach (TypeDefinitionHandle h in _reader.TypeDefinitions)
                    tokens.Add((uint)MetadataTokens.GetToken(h));
                hr = FillEnum(phEnum, tokens, rTypeDefs, cMax, pcTypeDefs);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (tokens is not null && _legacyImport is not null)
        {
            nint hEnumLocal = 0;
            List<uint> legacyTokens = new();
            uint* buf = stackalloc uint[64];
            while (true)
            {
                uint count;
                int hrLegacy = _legacyImport.EnumTypeDefs(&hEnumLocal, buf, 64, &count);
                if (hrLegacy < 0 || count == 0) break;
                for (uint i = 0; i < count; i++)
                    legacyTokens.Add(buf[i]);
            }
            _legacyImport.CloseEnum(hEnumLocal);
            Debug.Assert(tokens.Count == legacyTokens.Count, $"EnumTypeDefs count mismatch: cDAC={tokens.Count}, DAC={legacyTokens.Count}");
            for (int i = 0; i < Math.Min(tokens.Count, legacyTokens.Count); i++)
                Debug.Assert(tokens[i] == legacyTokens[i], $"EnumTypeDefs token mismatch at [{i}]: cDAC=0x{tokens[i]:X}, DAC=0x{legacyTokens[i]:X}");
        }
#endif
        return hr;
    }

    int IMetaDataImport.EnumInterfaceImpls(nint* phEnum, uint td, uint* rImpls, uint cMax, uint* pcImpls)
    {
        int hr = HResults.S_OK;
        List<uint>? tokens = null;
        try
        {
            if (phEnum is not null && *phEnum != 0)
            {
                hr = FillEnum(phEnum, GetEnum(*phEnum).Tokens, rImpls, cMax, pcImpls);
            }
            else
            {
                TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle(GetRID(td));
                TypeDefinition typeDef = _reader.GetTypeDefinition(typeHandle);
                tokens = new();
                foreach (InterfaceImplementationHandle h in typeDef.GetInterfaceImplementations())
                    tokens.Add((uint)MetadataTokens.GetToken(h));
                hr = FillEnum(phEnum, tokens, rImpls, cMax, pcImpls);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (tokens is not null && _legacyImport is not null)
        {
            nint hEnumLocal = 0;
            List<uint> legacyTokens = new();
            uint* buf = stackalloc uint[64];
            while (true)
            {
                uint count;
                int hrLegacy = _legacyImport.EnumInterfaceImpls(&hEnumLocal, td, buf, 64, &count);
                if (hrLegacy < 0 || count == 0) break;
                for (uint i = 0; i < count; i++)
                    legacyTokens.Add(buf[i]);
            }
            _legacyImport.CloseEnum(hEnumLocal);
            Debug.Assert(tokens.Count == legacyTokens.Count, $"EnumInterfaceImpls count mismatch for 0x{td:X}: cDAC={tokens.Count}, DAC={legacyTokens.Count}");
            for (int i = 0; i < Math.Min(tokens.Count, legacyTokens.Count); i++)
                Debug.Assert(tokens[i] == legacyTokens[i], $"EnumInterfaceImpls token mismatch at [{i}] for 0x{td:X}: cDAC=0x{tokens[i]:X}, DAC=0x{legacyTokens[i]:X}");
        }
#endif
        return hr;
    }

    int IMetaDataImport.EnumTypeRefs(nint* phEnum, uint* rTypeRefs, uint cMax, uint* pcTypeRefs)
        => _legacyImport is not null ? _legacyImport.EnumTypeRefs(phEnum, rTypeRefs, cMax, pcTypeRefs) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumMembers(nint* phEnum, uint cl, uint* rMembers, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMembers(phEnum, cl, rMembers, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumMethods(nint* phEnum, uint cl, uint* rMethods, uint cMax, uint* pcTokens)
    {
        int hr = HResults.S_OK;
        List<uint>? tokens = null;
        try
        {
            if (phEnum is not null && *phEnum != 0)
            {
                hr = FillEnum(phEnum, GetEnum(*phEnum).Tokens, rMethods, cMax, pcTokens);
            }
            else
            {
                TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle(GetRID(cl));
                TypeDefinition typeDef = _reader.GetTypeDefinition(typeHandle);
                tokens = new();
                foreach (MethodDefinitionHandle h in typeDef.GetMethods())
                    tokens.Add((uint)MetadataTokens.GetToken(h));
                hr = FillEnum(phEnum, tokens, rMethods, cMax, pcTokens);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (tokens is not null && _legacyImport is not null)
        {
            nint hEnumLocal = 0;
            List<uint> legacyTokens = new();
            uint* buf = stackalloc uint[64];
            while (true)
            {
                uint count;
                int hrLegacy = _legacyImport.EnumMethods(&hEnumLocal, cl, buf, 64, &count);
                if (hrLegacy < 0 || count == 0) break;
                for (uint i = 0; i < count; i++)
                    legacyTokens.Add(buf[i]);
            }
            _legacyImport.CloseEnum(hEnumLocal);
            Debug.Assert(tokens.Count == legacyTokens.Count, $"EnumMethods count mismatch for 0x{cl:X}: cDAC={tokens.Count}, DAC={legacyTokens.Count}");
            for (int i = 0; i < Math.Min(tokens.Count, legacyTokens.Count); i++)
                Debug.Assert(tokens[i] == legacyTokens[i], $"EnumMethods token mismatch at [{i}] for 0x{cl:X}: cDAC=0x{tokens[i]:X}, DAC=0x{legacyTokens[i]:X}");
        }
#endif
        return hr;
    }

    int IMetaDataImport.EnumFields(nint* phEnum, uint cl, uint* rFields, uint cMax, uint* pcTokens)
    {
        int hr = HResults.S_OK;
        List<uint>? tokens = null;
        try
        {
            if (phEnum is not null && *phEnum != 0)
            {
                hr = FillEnum(phEnum, GetEnum(*phEnum).Tokens, rFields, cMax, pcTokens);
            }
            else
            {
                TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle(GetRID(cl));
                TypeDefinition typeDef = _reader.GetTypeDefinition(typeHandle);
                tokens = new();
                foreach (FieldDefinitionHandle h in typeDef.GetFields())
                    tokens.Add((uint)MetadataTokens.GetToken(h));
                hr = FillEnum(phEnum, tokens, rFields, cMax, pcTokens);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (tokens is not null && _legacyImport is not null)
        {
            nint hEnumLocal = 0;
            List<uint> legacyTokens = new();
            uint* buf = stackalloc uint[64];
            while (true)
            {
                uint count;
                int hrLegacy = _legacyImport.EnumFields(&hEnumLocal, cl, buf, 64, &count);
                if (hrLegacy < 0 || count == 0) break;
                for (uint i = 0; i < count; i++)
                    legacyTokens.Add(buf[i]);
            }
            _legacyImport.CloseEnum(hEnumLocal);
            Debug.Assert(tokens.Count == legacyTokens.Count, $"EnumFields count mismatch for 0x{cl:X}: cDAC={tokens.Count}, DAC={legacyTokens.Count}");
            for (int i = 0; i < Math.Min(tokens.Count, legacyTokens.Count); i++)
                Debug.Assert(tokens[i] == legacyTokens[i], $"EnumFields token mismatch at [{i}] for 0x{cl:X}: cDAC=0x{tokens[i]:X}, DAC=0x{legacyTokens[i]:X}");
        }
#endif
        return hr;
    }

    int IMetaDataImport.EnumCustomAttributes(nint* phEnum, uint tk, uint tkType, uint* rCustomAttributes, uint cMax, uint* pcCustomAttributes)
        => _legacyImport is not null ? _legacyImport.EnumCustomAttributes(phEnum, tk, tkType, rCustomAttributes, cMax, pcCustomAttributes) : HResults.E_NOTIMPL;

    int IMetaDataImport2.EnumGenericParams(nint* phEnum, uint tk, uint* rGenericParams, uint cMax, uint* pcGenericParams)
    {
        int hr = HResults.S_OK;
        List<uint>? tokens = null;
        try
        {
            if (phEnum is not null && *phEnum != 0)
            {
                hr = FillEnum(phEnum, GetEnum(*phEnum).Tokens, rGenericParams, cMax, pcGenericParams);
            }
            else
            {
                EntityHandle owner = MetadataTokens.EntityHandle((int)tk);
                GenericParameterHandleCollection genericParams;

                if (owner.Kind == HandleKind.TypeDefinition)
                    genericParams = _reader.GetTypeDefinition((TypeDefinitionHandle)owner).GetGenericParameters();
                else if (owner.Kind == HandleKind.MethodDefinition)
                    genericParams = _reader.GetMethodDefinition((MethodDefinitionHandle)owner).GetGenericParameters();
                else
                {
                    throw new ArgumentException(null, nameof(tk));
                }

                tokens = new();
                foreach (GenericParameterHandle h in genericParams)
                    tokens.Add((uint)MetadataTokens.GetToken(h));
                hr = FillEnum(phEnum, tokens, rGenericParams, cMax, pcGenericParams);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (tokens is not null && _legacyImport2 is not null)
        {
            nint hEnumLocal = 0;
            List<uint> legacyTokens = new();
            uint* buf = stackalloc uint[64];
            while (true)
            {
                uint count;
                int hrLegacy = _legacyImport2.EnumGenericParams(&hEnumLocal, tk, buf, 64, &count);
                if (hrLegacy < 0 || count == 0) break;
                for (uint i = 0; i < count; i++)
                    legacyTokens.Add(buf[i]);
            }
            _legacyImport2.CloseEnum(hEnumLocal);
            Debug.Assert(tokens.Count == legacyTokens.Count, $"EnumGenericParams count mismatch for 0x{tk:X}: cDAC={tokens.Count}, DAC={legacyTokens.Count}");
            for (int i = 0; i < Math.Min(tokens.Count, legacyTokens.Count); i++)
                Debug.Assert(tokens[i] == legacyTokens[i], $"EnumGenericParams token mismatch at [{i}] for 0x{tk:X}: cDAC=0x{tokens[i]:X}, DAC=0x{legacyTokens[i]:X}");
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetTypeDefProps(uint td, char* szTypeDef, uint cchTypeDef, uint* pchTypeDef, uint* pdwTypeDefFlags, uint* ptkExtends)
    {
        int hr = HResults.S_OK;
        try
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle(GetRID(td));
            TypeDefinition typeDef = _reader.GetTypeDefinition(typeHandle);

            string fullName = GetTypeDefFullName(typeDef);
            OutputBufferHelpers.CopyStringToBuffer(szTypeDef, cchTypeDef, pchTypeDef, fullName, out bool truncated);

            if (pdwTypeDefFlags is not null)
                *pdwTypeDefFlags = (uint)typeDef.Attributes;

            if (ptkExtends is not null)
            {
                EntityHandle baseType = typeDef.BaseType;
                *ptkExtends = baseType.IsNil ? 0 : (uint)MetadataTokens.GetToken(baseType);
            }

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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

    int IMetaDataImport.GetTypeRefProps(uint tr, uint* ptkResolutionScope, char* szName, uint cchName, uint* pchName)
    {
        int hr = HResults.S_OK;
        try
        {
            TypeReferenceHandle refHandle = MetadataTokens.TypeReferenceHandle(GetRID(tr));
            TypeReference typeRef = _reader.GetTypeReference(refHandle);

            string fullName = GetTypeRefFullName(typeRef);
            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, fullName, out bool truncated);

            if (ptkResolutionScope is not null)
            {
                EntityHandle scope = typeRef.ResolutionScope;
                *ptkResolutionScope = scope.IsNil ? 0 : (uint)MetadataTokens.GetToken(scope);
            }

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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

    int IMetaDataImport.GetMethodProps(uint mb, uint* pClass, char* szMethod, uint cchMethod, uint* pchMethod,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags)
    {
        int hr = HResults.S_OK;
        try
        {
            MethodDefinitionHandle methodHandle = MetadataTokens.MethodDefinitionHandle(GetRID(mb));
            MethodDefinition methodDef = _reader.GetMethodDefinition(methodHandle);

            string name = _reader.GetString(methodDef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szMethod, cchMethod, pchMethod, name, out bool truncated);

            if (pClass is not null)
                *pClass = MapGlobalParentToken((uint)MetadataTokens.GetToken(methodDef.GetDeclaringType()));

            if (pdwAttr is not null)
                *pdwAttr = (uint)methodDef.Attributes;

            if (ppvSigBlob is not null || pcbSigBlob is not null)
            {
                BlobHandle sigHandle = methodDef.Signature;
                BlobReader blobReader = _reader.GetBlobReader(sigHandle);
                if (ppvSigBlob is not null)
                    *ppvSigBlob = blobReader.StartPointer;
                if (pcbSigBlob is not null)
                    *pcbSigBlob = (uint)blobReader.Length;
            }

            if (pulCodeRVA is not null)
                *pulCodeRVA = (uint)methodDef.RelativeVirtualAddress;

            if (pdwImplFlags is not null)
                *pdwImplFlags = (uint)methodDef.ImplAttributes;

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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

    int IMetaDataImport.GetFieldProps(uint mb, uint* pClass, char* szField, uint cchField, uint* pchField,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pdwCPlusTypeFlag,
        void** ppValue, uint* pcchValue)
    {
        int hr = HResults.S_OK;
        try
        {
            FieldDefinitionHandle fieldHandle = MetadataTokens.FieldDefinitionHandle(GetRID(mb));
            FieldDefinition fieldDef = _reader.GetFieldDefinition(fieldHandle);

            string name = _reader.GetString(fieldDef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szField, cchField, pchField, name, out bool truncated);

            if (pClass is not null)
                *pClass = MapGlobalParentToken((uint)MetadataTokens.GetToken(fieldDef.GetDeclaringType()));

            if (pdwAttr is not null)
                *pdwAttr = (uint)fieldDef.Attributes;

            if (ppvSigBlob is not null || pcbSigBlob is not null)
            {
                BlobHandle sigHandle = fieldDef.Signature;
                BlobReader blobReader = _reader.GetBlobReader(sigHandle);
                if (ppvSigBlob is not null)
                    *ppvSigBlob = blobReader.StartPointer;
                if (pcbSigBlob is not null)
                    *pcbSigBlob = (uint)blobReader.Length;
            }

            if (pdwCPlusTypeFlag is not null)
                *pdwCPlusTypeFlag = (uint)CorElementType.Void;
            if (ppValue is not null)
                *ppValue = null;
            if (pcchValue is not null)
                *pcchValue = 0;

            ConstantHandle constHandle = fieldDef.GetDefaultValue();
            if (!constHandle.IsNil && (pdwCPlusTypeFlag is not null || ppValue is not null))
            {
                Constant constant = _reader.GetConstant(constHandle);
                if (pdwCPlusTypeFlag is not null)
                    *pdwCPlusTypeFlag = (uint)constant.TypeCode;
                if (ppValue is not null || pcchValue is not null)
                {
                    BlobReader valueReader = _reader.GetBlobReader(constant.Value);
                    if (ppValue is not null)
                        *ppValue = valueReader.StartPointer;
                    if (pcchValue is not null)
                        *pcchValue = (uint)constant.TypeCode == (uint)CorElementType.String ? (uint)valueReader.Length / sizeof(char) : (uint)valueReader.Length;
                }
            }

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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

    int IMetaDataImport.GetMemberProps(uint mb, uint* pClass, char* szMember, uint cchMember, uint* pchMember,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags,
        uint* pdwCPlusTypeFlag, void** ppValue, uint* pcchValue)
    {
        uint tableIndex = mb >> 24;
        if (tableIndex == 0x06) // MethodDef
        {
            int hr = ((IMetaDataImport)this).GetMethodProps(mb, pClass, szMember, cchMember, pchMember, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags);
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
            int hr = ((IMetaDataImport)this).GetFieldProps(mb, pClass, szMember, cchMember, pchMember, pdwAttr, ppvSigBlob, pcbSigBlob, pdwCPlusTypeFlag, ppValue, pcchValue);
            if (pulCodeRVA is not null)
                *pulCodeRVA = 0;
            if (pdwImplFlags is not null)
                *pdwImplFlags = 0;
            return hr;
        }

        return HResults.E_INVALIDARG;
    }

    int IMetaDataImport.GetInterfaceImplProps(uint iiImpl, uint* pClass, uint* ptkIface)
    {
        int hr = HResults.S_OK;
        try
        {
            InterfaceImplementationHandle implHandle = MetadataTokens.InterfaceImplementationHandle(GetRID(iiImpl));
            InterfaceImplementation impl = _reader.GetInterfaceImplementation(implHandle);

            if (pClass is not null)
            {
                _interfaceImplToTypeDef ??= BuildInterfaceImplLookup();
                *pClass = _interfaceImplToTypeDef.TryGetValue(GetRID(iiImpl), out uint ownerToken)
                    ? ownerToken : 0;
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

    int IMetaDataImport.GetNestedClassProps(uint tdNestedClass, uint* ptdEnclosingClass)
    {
        int hr = HResults.S_OK;
        try
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle(GetRID(tdNestedClass));
            TypeDefinition typeDef = _reader.GetTypeDefinition(typeHandle);
            TypeDefinitionHandle declaringType = typeDef.GetDeclaringType();

            if (ptdEnclosingClass is not null)
                *ptdEnclosingClass = declaringType.IsNil ? 0 : (uint)MetadataTokens.GetToken(declaringType);

            hr = declaringType.IsNil ? CldbHResults.CLDB_E_RECORD_NOTFOUND : HResults.S_OK;
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

    int IMetaDataImport2.GetGenericParamProps(uint gp, uint* pulParamSeq, uint* pdwParamFlags, uint* ptOwner,
        uint* reserved, char* wzname, uint cchName, uint* pchName)
    {
        int hr = HResults.S_OK;
        try
        {
            GenericParameterHandle gpHandle = MetadataTokens.GenericParameterHandle(GetRID(gp));
            GenericParameter genericParam = _reader.GetGenericParameter(gpHandle);

            if (pulParamSeq is not null)
                *pulParamSeq = (uint)genericParam.Index;

            if (pdwParamFlags is not null)
                *pdwParamFlags = (uint)genericParam.Attributes;

            if (ptOwner is not null)
                *ptOwner = (uint)MetadataTokens.GetToken(genericParam.Parent);

            if (reserved is not null)
                *reserved = 0;

            string name = _reader.GetString(genericParam.Name);
            OutputBufferHelpers.CopyStringToBuffer(wzname, cchName, pchName, name, out bool truncated);

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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

    int IMetaDataImport.GetRVA(uint tk, uint* pulCodeRVA, uint* pdwImplFlags)
    {
        int hr = HResults.S_OK;
        try
        {
            uint tableIndex = tk >> 24;
            if (tableIndex == 0x06) // MethodDef
            {
                MethodDefinitionHandle methodHandle = MetadataTokens.MethodDefinitionHandle(GetRID(tk));
                MethodDefinition methodDef = _reader.GetMethodDefinition(methodHandle);
                if (pulCodeRVA is not null)
                    *pulCodeRVA = (uint)methodDef.RelativeVirtualAddress;
                if (pdwImplFlags is not null)
                    *pdwImplFlags = (uint)methodDef.ImplAttributes;
            }
            else if (tableIndex == 0x04) // FieldDef
            {
                FieldDefinitionHandle fieldHandle = MetadataTokens.FieldDefinitionHandle(GetRID(tk));
                FieldDefinition fieldDef = _reader.GetFieldDefinition(fieldHandle);
                if (pulCodeRVA is not null)
                    *pulCodeRVA = (uint)fieldDef.GetRelativeVirtualAddress();
                if (pdwImplFlags is not null)
                    *pdwImplFlags = 0;
            }
            else
            {
                hr = HResults.E_INVALIDARG;
            }
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

    int IMetaDataImport.GetSigFromToken(uint mdSig, byte** ppvSig, uint* pcbSig)
    {
        int hr = HResults.S_OK;
        try
        {
            StandaloneSignatureHandle sigHandle = MetadataTokens.StandaloneSignatureHandle(GetRID(mdSig));
            StandaloneSignature sig = _reader.GetStandaloneSignature(sigHandle);
            BlobReader blobReader = _reader.GetBlobReader(sig.Signature);

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

    int IMetaDataImport.GetCustomAttributeByName(uint tkObj, char* szName, void** ppData, uint* pcbData)
    {
        int hr = HResults.S_OK;
        try
        {
            if (ppData is not null)
                *ppData = null;
            if (pcbData is not null)
                *pcbData = 0;

            string targetName = new string(szName);
            EntityHandle parent = MetadataTokens.EntityHandle((int)tkObj);
            bool found = false;

            foreach (CustomAttributeHandle caHandle in _reader.GetCustomAttributes(parent))
            {
                CustomAttribute ca = _reader.GetCustomAttribute(caHandle);
                string attrTypeName = GetCustomAttributeTypeName(ca.Constructor);
                if (string.Equals(attrTypeName, targetName, StringComparison.Ordinal))
                {
                    BlobReader blobReader = _reader.GetBlobReader(ca.Value);
                    if (ppData is not null)
                        *ppData = blobReader.StartPointer;
                    if (pcbData is not null)
                        *pcbData = (uint)blobReader.Length;
                    found = true;
                    break;
                }
            }

            if (!found)
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
        int rid = GetRID(tk);
        int tokenType = (int)(tk >> 24);

        if (rid == 0)
            return 0; // FALSE

        const int UserStringTokenType = 0x70;
        if (tokenType == UserStringTokenType)
        {
            int heapSize = _reader.GetHeapSize(HeapIndex.UserString);
            return rid < heapSize ? 1 : 0;
        }

        if (tokenType < 0 || tokenType > (int)TableIndex.CustomDebugInformation)
            return 0; // FALSE

        int rowCount = _reader.GetTableRowCount((TableIndex)tokenType);
        return rid <= rowCount ? 1 : 0; // TRUE or FALSE
    }

    private string GetCustomAttributeTypeName(EntityHandle constructor)
    {
        if (constructor.Kind == HandleKind.MethodDefinition)
        {
            MethodDefinition method = _reader.GetMethodDefinition((MethodDefinitionHandle)constructor);
            TypeDefinition typeDef = _reader.GetTypeDefinition(method.GetDeclaringType());
            return GetTypeDefFullName(typeDef);
        }
        if (constructor.Kind == HandleKind.MemberReference)
        {
            MemberReference memberRef = _reader.GetMemberReference((MemberReferenceHandle)constructor);
            EntityHandle parent = memberRef.Parent;
            if (parent.Kind == HandleKind.TypeReference)
            {
                TypeReference typeRef = _reader.GetTypeReference((TypeReferenceHandle)parent);
                return GetTypeRefFullName(typeRef);
            }
            if (parent.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinition typeDef = _reader.GetTypeDefinition((TypeDefinitionHandle)parent);
                return GetTypeDefFullName(typeDef);
            }
        }
        return string.Empty;
    }

    int IMetaDataImport.FindTypeDefByName(char* szTypeDef, uint tkEnclosingClass, uint* ptd)
    {
        int hr = HResults.S_OK;
        try
        {
            if (ptd is not null)
                *ptd = 0;

            string targetName = new string(szTypeDef);

            bool found = false;
            foreach (TypeDefinitionHandle tdh in _reader.TypeDefinitions)
            {
                TypeDefinition typeDef = _reader.GetTypeDefinition(tdh);
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
                hr = CldbHResults.CLDB_E_RECORD_NOTFOUND;
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

    int IMetaDataImport.GetScopeProps(char* szName, uint cchName, uint* pchName, Guid* pmvid)
        => _legacyImport is not null ? _legacyImport.GetScopeProps(szName, cchName, pchName, pmvid) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetModuleFromScope(uint* pmd)
        => _legacyImport is not null ? _legacyImport.GetModuleFromScope(pmd) : HResults.E_NOTIMPL;

    int IMetaDataImport.ResolveTypeRef(uint tr, Guid* riid, void** ppIScope, uint* ptd)
        => _legacyImport is not null ? _legacyImport.ResolveTypeRef(tr, riid, ppIScope, ptd) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumMembersWithName(nint* phEnum, uint cl, char* szName, uint* rMembers, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMembersWithName(phEnum, cl, szName, rMembers, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumMethodsWithName(nint* phEnum, uint cl, char* szName, uint* rMethods, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMethodsWithName(phEnum, cl, szName, rMethods, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumFieldsWithName(nint* phEnum, uint cl, char* szName, uint* rFields, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumFieldsWithName(phEnum, cl, szName, rFields, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumParams(nint* phEnum, uint mb, uint* rParams, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumParams(phEnum, mb, rParams, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumMemberRefs(nint* phEnum, uint tkParent, uint* rMemberRefs, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMemberRefs(phEnum, tkParent, rMemberRefs, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumMethodImpls(nint* phEnum, uint td, uint* rMethodBody, uint* rMethodDecl, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumMethodImpls(phEnum, td, rMethodBody, rMethodDecl, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumPermissionSets(nint* phEnum, uint tk, uint dwActions, uint* rPermission, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumPermissionSets(phEnum, tk, dwActions, rPermission, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataImport.FindMember(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
        => _legacyImport is not null ? _legacyImport.FindMember(td, szName, pvSigBlob, cbSigBlob, pmb) : HResults.E_NOTIMPL;

    int IMetaDataImport.FindMethod(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
        => _legacyImport is not null ? _legacyImport.FindMethod(td, szName, pvSigBlob, cbSigBlob, pmb) : HResults.E_NOTIMPL;

    int IMetaDataImport.FindField(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
        => _legacyImport is not null ? _legacyImport.FindField(td, szName, pvSigBlob, cbSigBlob, pmb) : HResults.E_NOTIMPL;

    int IMetaDataImport.FindMemberRef(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmr)
        => _legacyImport is not null ? _legacyImport.FindMemberRef(td, szName, pvSigBlob, cbSigBlob, pmr) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetMemberRefProps(uint mr, uint* ptk, char* szMember, uint cchMember, uint* pchMember,
        byte** ppvSigBlob, uint* pbSig)
    {
        int hr = HResults.S_OK;
        try
        {
            MemberReferenceHandle refHandle = MetadataTokens.MemberReferenceHandle(GetRID(mr));
            MemberReference memberRef = _reader.GetMemberReference(refHandle);

            string name = _reader.GetString(memberRef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szMember, cchMember, pchMember, name, out bool truncated);

            if (ptk is not null)
                *ptk = MapGlobalParentToken((uint)MetadataTokens.GetToken(memberRef.Parent));

            if (ppvSigBlob is not null || pbSig is not null)
            {
                BlobReader blobReader = _reader.GetBlobReader(memberRef.Signature);
                if (ppvSigBlob is not null)
                    *ppvSigBlob = blobReader.StartPointer;
                if (pbSig is not null)
                    *pbSig = (uint)blobReader.Length;
            }

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
        => _legacyImport is not null ? _legacyImport.EnumProperties(phEnum, td, rProperties, cMax, pcProperties) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumEvents(nint* phEnum, uint td, uint* rEvents, uint cMax, uint* pcEvents)
        => _legacyImport is not null ? _legacyImport.EnumEvents(phEnum, td, rEvents, cMax, pcEvents) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetEventProps(uint ev, uint* pClass, char* szEvent, uint cchEvent, uint* pchEvent,
        uint* pdwEventFlags, uint* ptkEventType, uint* pmdAddOn, uint* pmdRemoveOn, uint* pmdFire,
        uint* rmdOtherMethod, uint cMax, uint* pcOtherMethod)
        => _legacyImport is not null ? _legacyImport.GetEventProps(ev, pClass, szEvent, cchEvent, pchEvent, pdwEventFlags, ptkEventType, pmdAddOn, pmdRemoveOn, pmdFire, rmdOtherMethod, cMax, pcOtherMethod) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumMethodSemantics(nint* phEnum, uint mb, uint* rEventProp, uint cMax, uint* pcEventProp)
        => _legacyImport is not null ? _legacyImport.EnumMethodSemantics(phEnum, mb, rEventProp, cMax, pcEventProp) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetMethodSemantics(uint mb, uint tkEventProp, uint* pdwSemanticsFlags)
        => _legacyImport is not null ? _legacyImport.GetMethodSemantics(mb, tkEventProp, pdwSemanticsFlags) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetClassLayout(uint td, uint* pdwPackSize, void* rFieldOffset, uint cMax, uint* pcFieldOffset, uint* pulClassSize)
    {
        int hr = HResults.S_OK;
        try
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle(GetRID(td));
            TypeDefinition typeDef = _reader.GetTypeDefinition(typeHandle);
            TypeLayout layout = typeDef.GetLayout();

            if (layout.IsDefault)
            {
                hr = CldbHResults.CLDB_E_RECORD_NOTFOUND;
            }
            else
            {
                if (pdwPackSize is not null)
                    *pdwPackSize = (uint)layout.PackingSize;

                if (pulClassSize is not null)
                    *pulClassSize = (uint)layout.Size;

                if (rFieldOffset is not null || pcFieldOffset is not null)
                {
                    uint* fieldOffsets = (uint*)rFieldOffset;
                    uint count = 0;
                    foreach (FieldDefinitionHandle fh in typeDef.GetFields())
                    {
                        if (fieldOffsets is not null && count < cMax)
                        {
                            // Each entry is {FieldDef token (uint), ulOffset (uint)}
                            fieldOffsets[count * 2] = (uint)MetadataTokens.GetToken(fh);
                            int offset = _reader.GetFieldDefinition(fh).GetOffset();
                            fieldOffsets[count * 2 + 1] = offset >= 0 ? (uint)offset : 0xFFFFFFFF;
                        }
                        count++;
                    }
                    if (pcFieldOffset is not null)
                        *pcFieldOffset = count;
                }

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
        => _legacyImport is not null ? _legacyImport.GetFieldMarshal(tk, ppvNativeType, pcbNativeType) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetPermissionSetProps(uint pm, uint* pdwAction, void** ppvPermission, uint* pcbPermission)
        => _legacyImport is not null ? _legacyImport.GetPermissionSetProps(pm, pdwAction, ppvPermission, pcbPermission) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetModuleRefProps(uint mur, char* szName, uint cchName, uint* pchName)
    {
        int hr = HResults.S_OK;
        try
        {
            ModuleReferenceHandle modRefHandle = MetadataTokens.ModuleReferenceHandle(GetRID(mur));
            ModuleReference modRef = _reader.GetModuleReference(modRefHandle);

            string name = _reader.GetString(modRef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, name, out bool truncated);

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
        => _legacyImport is not null ? _legacyImport.EnumModuleRefs(phEnum, rModuleRefs, cmax, pcModuleRefs) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetTypeSpecFromToken(uint typespec, byte** ppvSig, uint* pcbSig)
    {
        int hr = HResults.S_OK;
        try
        {
            TypeSpecificationHandle tsHandle = MetadataTokens.TypeSpecificationHandle(GetRID(typespec));
            TypeSpecification typeSpec = _reader.GetTypeSpecification(tsHandle);
            BlobReader blobReader = _reader.GetBlobReader(typeSpec.Signature);

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
        => _legacyImport is not null ? _legacyImport.GetNameFromToken(tk, pszUtf8NamePtr) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumUnresolvedMethods(nint* phEnum, uint* rMethods, uint cMax, uint* pcTokens)
        => _legacyImport is not null ? _legacyImport.EnumUnresolvedMethods(phEnum, rMethods, cMax, pcTokens) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetUserString(uint stk, char* szString, uint cchString, uint* pchString)
    {
        int hr = HResults.S_OK;
        try
        {
            // Read the user string from raw #US heap bytes to match native behavior exactly.
            // Native does: GetUserString → check size is odd → TruncateBySize(1) → GetSize()/sizeof(WCHAR).
            // Using raw bytes avoids potential discrepancies with MetadataReader.GetUserString().
            int heapMetadataOffset = _reader.GetHeapMetadataOffset(HeapIndex.UserString);
            int heapSize = _reader.GetHeapSize(HeapIndex.UserString);
            int handleOffset = GetRID(stk);

            byte* heapBase = _reader.MetadataPointer + heapMetadataOffset;
            int remaining = heapSize - handleOffset;
            if (remaining <= 0)
                throw Marshal.GetExceptionForHR(CldbHResults.CLDB_E_FILE_CORRUPT)!;

            BlobReader blobReader = new BlobReader(heapBase + handleOffset, remaining);
            int blobSize = blobReader.ReadCompressedInteger();

            // Validate blob fits within the remaining heap to prevent out-of-bounds reads.
            if (blobSize > blobReader.RemainingBytes)
                throw Marshal.GetExceptionForHR(CldbHResults.CLDB_E_FILE_CORRUPT)!;

            // Native rejects even-sized blobs (missing terminal byte) as corrupt.
            if ((blobSize % sizeof(char)) == 0)
                throw Marshal.GetExceptionForHR(CldbHResults.CLDB_E_FILE_CORRUPT)!;

            int charCount = (blobSize - 1) / sizeof(char);

            if (pchString is not null)
                *pchString = (uint)charCount;

            if (szString is not null && cchString > 0)
            {
                char* dataPtr = (char*)blobReader.CurrentPointer;
                int copyChars = Math.Min(charCount, (int)cchString);
                new ReadOnlySpan<char>(dataPtr, copyChars).CopyTo(new Span<char>(szString, copyChars));

                if ((uint)charCount > cchString)
                {
                    szString[cchString - 1] = '\0';
                    hr = CldbHResults.CLDB_S_TRUNCATION;
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
                    string cdacStr = new string(szString);
                    string dacStr = new string(szLocal);
                    Debug.Assert(cdacStr == dacStr, $"UserString content mismatch: cDAC='{cdacStr}', DAC='{dacStr}'");
                }
            }
        }
#endif
        return hr;
    }

    int IMetaDataImport.GetPinvokeMap(uint tk, uint* pdwMappingFlags, char* szImportName, uint cchImportName,
        uint* pchImportName, uint* pmrImportDLL)
        => _legacyImport is not null ? _legacyImport.GetPinvokeMap(tk, pdwMappingFlags, szImportName, cchImportName, pchImportName, pmrImportDLL) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumSignatures(nint* phEnum, uint* rSignatures, uint cmax, uint* pcSignatures)
        => _legacyImport is not null ? _legacyImport.EnumSignatures(phEnum, rSignatures, cmax, pcSignatures) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumTypeSpecs(nint* phEnum, uint* rTypeSpecs, uint cmax, uint* pcTypeSpecs)
        => _legacyImport is not null ? _legacyImport.EnumTypeSpecs(phEnum, rTypeSpecs, cmax, pcTypeSpecs) : HResults.E_NOTIMPL;

    int IMetaDataImport.EnumUserStrings(nint* phEnum, uint* rStrings, uint cmax, uint* pcStrings)
        => _legacyImport is not null ? _legacyImport.EnumUserStrings(phEnum, rStrings, cmax, pcStrings) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetParamForMethodIndex(uint md, uint ulParamSeq, uint* ppd)
    {
        int hr = HResults.S_OK;
        try
        {
            if (ppd is not null)
                *ppd = 0;

            MethodDefinitionHandle methodHandle = MetadataTokens.MethodDefinitionHandle(GetRID(md));
            MethodDefinition methodDef = _reader.GetMethodDefinition(methodHandle);

            bool found = false;
            foreach (ParameterHandle ph in methodDef.GetParameters())
            {
                Parameter param = _reader.GetParameter(ph);
                if (param.SequenceNumber == (int)ulParamSeq)
                {
                    if (ppd is not null)
                        *ppd = (uint)MetadataTokens.GetToken(ph);
                    found = true;
                    break;
                }
            }

            if (!found)
                hr = CldbHResults.CLDB_E_RECORD_NOTFOUND;
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

    int IMetaDataImport.GetCustomAttributeProps(uint cv, uint* ptkObj, uint* ptkType, void** ppBlob, uint* pcbSize)
        => _legacyImport is not null ? _legacyImport.GetCustomAttributeProps(cv, ptkObj, ptkType, ppBlob, pcbSize) : HResults.E_NOTIMPL;

    int IMetaDataImport.FindTypeRef(uint tkResolutionScope, char* szName, uint* ptr)
        => _legacyImport is not null ? _legacyImport.FindTypeRef(tkResolutionScope, szName, ptr) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetPropertyProps(uint prop, uint* pClass, char* szProperty, uint cchProperty, uint* pchProperty,
        uint* pdwPropFlags, byte** ppvSig, uint* pbSig, uint* pdwCPlusTypeFlag,
        void** ppDefaultValue, uint* pcchDefaultValue, uint* pmdSetter, uint* pmdGetter,
        uint* rmdOtherMethod, uint cMax, uint* pcOtherMethod)
        => _legacyImport is not null ? _legacyImport.GetPropertyProps(prop, pClass, szProperty, cchProperty, pchProperty, pdwPropFlags, ppvSig, pbSig, pdwCPlusTypeFlag, ppDefaultValue, pcchDefaultValue, pmdSetter, pmdGetter, rmdOtherMethod, cMax, pcOtherMethod) : HResults.E_NOTIMPL;

    int IMetaDataImport.GetParamProps(uint tk, uint* pmd, uint* pulSequence, char* szName, uint cchName, uint* pchName,
        uint* pdwAttr, uint* pdwCPlusTypeFlag, void** ppValue, uint* pcchValue)
    {
        int hr = HResults.S_OK;
        try
        {
            ParameterHandle paramHandle = MetadataTokens.ParameterHandle(GetRID(tk));
            Parameter param = _reader.GetParameter(paramHandle);

            string name = _reader.GetString(param.Name);
            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, name, out bool truncated);

            if (pmd is not null)
            {
                _paramToMethod ??= BuildParamToMethodLookup();
                *pmd = _paramToMethod.TryGetValue(MetadataTokens.GetRowNumber(paramHandle), out uint methodToken) ? methodToken : 0;
            }

            if (pulSequence is not null)
                *pulSequence = (uint)param.SequenceNumber;

            if (pdwAttr is not null)
                *pdwAttr = (uint)param.Attributes;

            if (pdwCPlusTypeFlag is not null)
                *pdwCPlusTypeFlag = (uint)CorElementType.Void;
            if (ppValue is not null)
                *ppValue = null;
            if (pcchValue is not null)
                *pcchValue = 0;

            ConstantHandle constHandle = param.GetDefaultValue();
            if (!constHandle.IsNil && (pdwCPlusTypeFlag is not null || ppValue is not null))
            {
                Constant constant = _reader.GetConstant(constHandle);
                if (pdwCPlusTypeFlag is not null)
                    *pdwCPlusTypeFlag = (uint)constant.TypeCode;
                if (ppValue is not null || pcchValue is not null)
                {
                    BlobReader valueReader = _reader.GetBlobReader(constant.Value);
                    if (ppValue is not null)
                        *ppValue = valueReader.StartPointer;
                    if (pcchValue is not null)
                        *pcchValue = (uint)constant.TypeCode == (uint)CorElementType.String ? (uint)valueReader.Length / sizeof(char) : (uint)valueReader.Length;
                }
            }

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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

    int IMetaDataImport.GetNativeCallConvFromSig(void* pvSig, uint cbSig, uint* pCallConv)
        => _legacyImport is not null ? _legacyImport.GetNativeCallConvFromSig(pvSig, cbSig, pCallConv) : HResults.E_NOTIMPL;

    int IMetaDataImport.IsGlobal(uint pd, int* pbGlobal)
        => _legacyImport is not null ? _legacyImport.IsGlobal(pd, pbGlobal) : HResults.E_NOTIMPL;

    // IMetaDataImport2 methods — delegate to legacy via _legacyImport2
    int IMetaDataImport2.GetMethodSpecProps(uint mi, uint* tkParent, byte** ppvSigBlob, uint* pcbSigBlob)
        => _legacyImport2 is not null ? _legacyImport2.GetMethodSpecProps(mi, tkParent, ppvSigBlob, pcbSigBlob) : HResults.E_NOTIMPL;

    int IMetaDataImport2.EnumGenericParamConstraints(nint* phEnum, uint tk, uint* rGenericParamConstraints, uint cMax, uint* pcGenericParamConstraints)
        => _legacyImport2 is not null ? _legacyImport2.EnumGenericParamConstraints(phEnum, tk, rGenericParamConstraints, cMax, pcGenericParamConstraints) : HResults.E_NOTIMPL;

    int IMetaDataImport2.GetGenericParamConstraintProps(uint gpc, uint* ptGenericParam, uint* ptkConstraintType)
        => _legacyImport2 is not null ? _legacyImport2.GetGenericParamConstraintProps(gpc, ptGenericParam, ptkConstraintType) : HResults.E_NOTIMPL;

    int IMetaDataImport2.GetPEKind(uint* pdwPEKind, uint* pdwMachine)
        => _legacyImport2 is not null ? _legacyImport2.GetPEKind(pdwPEKind, pdwMachine) : HResults.E_NOTIMPL;

    int IMetaDataImport2.GetVersionString(char* pwzBuf, uint ccBufSize, uint* pccBufSize)
        => _legacyImport2 is not null ? _legacyImport2.GetVersionString(pwzBuf, ccBufSize, pccBufSize) : HResults.E_NOTIMPL;

    int IMetaDataImport2.EnumMethodSpecs(nint* phEnum, uint tk, uint* rMethodSpecs, uint cMax, uint* pcMethodSpecs)
        => _legacyImport2 is not null ? _legacyImport2.EnumMethodSpecs(phEnum, tk, rMethodSpecs, cMax, pcMethodSpecs) : HResults.E_NOTIMPL;

    // =============================================
    // IMetaDataAssemblyImport
    // =============================================

    int IMetaDataAssemblyImport.GetAssemblyProps(uint mda, byte** ppbPublicKey, uint* pcbPublicKey,
        uint* pulHashAlgId, char* szName, uint cchName, uint* pchName,
        ASSEMBLYMETADATA* pMetaData, uint* pdwAssemblyFlags)
    {
        int hr = HResults.S_OK;
        try
        {
            // Validate that the token is the assembly definition token
            if (mda != 0x20000001)
                throw Marshal.GetExceptionForHR(CldbHResults.CLDB_E_RECORD_NOTFOUND)!;

            AssemblyDefinition assemblyDef = _reader.GetAssemblyDefinition();
            string name = _reader.GetString(assemblyDef.Name);

            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, name, out bool truncated);

            if (!assemblyDef.PublicKey.IsNil)
            {
                BlobReader publicKeyReader = _reader.GetBlobReader(assemblyDef.PublicKey);
                if (ppbPublicKey is not null)
                    *ppbPublicKey = publicKeyReader.CurrentPointer;
                if (pcbPublicKey is not null)
                    *pcbPublicKey = (uint)publicKeyReader.Length;
            }
            else
            {
                if (ppbPublicKey is not null)
                    *ppbPublicKey = null;
                if (pcbPublicKey is not null)
                    *pcbPublicKey = 0;
            }
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
                OutputBufferHelpers.CopyStringToBuffer(pMetaData->szLocale, pMetaData->cbLocale, null, culture, out bool localTruncated);
                truncated |= localTruncated;
                pMetaData->cbLocale = (uint)(culture.Length + 1);
                pMetaData->ulProcessor = 0;
                pMetaData->ulOS = 0;
            }

            if (pdwAssemblyFlags is not null)
            {
                uint flags = (uint)assemblyDef.Flags;
                // Native RegMeta ORs afPublicKey (0x0001) into flags when public key blob is non-empty
                if (!assemblyDef.PublicKey.IsNil && _reader.GetBlobReader(assemblyDef.PublicKey).Length > 0)
                    flags |= 0x0001;
                *pdwAssemblyFlags = flags;
            }

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
        int hr = HResults.S_OK;
        try
        {
            AssemblyReferenceHandle refHandle = MetadataTokens.AssemblyReferenceHandle(GetRID(mdar));
            AssemblyReference assemblyRef = _reader.GetAssemblyReference(refHandle);
            string name = _reader.GetString(assemblyRef.Name);

            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, name, out bool truncated);

            if (!assemblyRef.PublicKeyOrToken.IsNil)
            {
                BlobReader publicKeyReader = _reader.GetBlobReader(assemblyRef.PublicKeyOrToken);
                if (ppbPublicKeyOrToken is not null)
                    *ppbPublicKeyOrToken = publicKeyReader.CurrentPointer;
                if (pcbPublicKeyOrToken is not null)
                    *pcbPublicKeyOrToken = (uint)publicKeyReader.Length;
            }
            else
            {
                if (ppbPublicKeyOrToken is not null)
                    *ppbPublicKeyOrToken = null;
                if (pcbPublicKeyOrToken is not null)
                    *pcbPublicKeyOrToken = 0;
            }

            if (pMetaData is not null)
            {
                System.Version version = assemblyRef.Version;
                pMetaData->usMajorVersion = (ushort)version.Major;
                pMetaData->usMinorVersion = (ushort)version.Minor;
                pMetaData->usBuildNumber = (ushort)version.Build;
                pMetaData->usRevisionNumber = (ushort)version.Revision;

                string culture = _reader.GetString(assemblyRef.Culture);
                OutputBufferHelpers.CopyStringToBuffer(pMetaData->szLocale, pMetaData->cbLocale, null, culture, out bool localTruncated);
                truncated |= localTruncated;
                pMetaData->cbLocale = (uint)(culture.Length + 1);
                pMetaData->ulProcessor = 0;
                pMetaData->ulOS = 0;
            }

            if (!assemblyRef.HashValue.IsNil)
            {
                BlobReader hashReader = _reader.GetBlobReader(assemblyRef.HashValue);
                if (ppbHashValue is not null)
                    *ppbHashValue = hashReader.CurrentPointer;
                if (pcbHashValue is not null)
                    *pcbHashValue = (uint)hashReader.Length;
            }
            else
            {
                if (ppbHashValue is not null)
                    *ppbHashValue = null;
                if (pcbHashValue is not null)
                    *pcbHashValue = 0;
            }

            if (pdwAssemblyRefFlags is not null)
                *pdwAssemblyRefFlags = (uint)assemblyRef.Flags;

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.GetFileProps(mdf, szName, cchName, pchName, ppbHashValue, pcbHashValue, pdwFileFlags) : HResults.E_NOTIMPL;

    int IMetaDataAssemblyImport.GetExportedTypeProps(uint mdct, char* szName, uint cchName, uint* pchName,
        uint* ptkImplementation, uint* ptkTypeDef, uint* pdwExportedTypeFlags)
    {
        int hr = HResults.S_OK;
        try
        {
            ExportedTypeHandle handle = MetadataTokens.ExportedTypeHandle(GetRID(mdct));
            ExportedType exportedType = _reader.GetExportedType(handle);

            string name = _reader.GetString(exportedType.Name);
            string ns = _reader.GetString(exportedType.Namespace);
            string fullName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, fullName, out bool truncated);

            if (ptkImplementation is not null)
            {
                EntityHandle impl = exportedType.Implementation;
                *ptkImplementation = impl.IsNil ? 0 : (uint)MetadataTokens.GetToken(impl);
            }

            if (ptkTypeDef is not null)
                *ptkTypeDef = (uint)exportedType.GetTypeDefinitionId();

            if (pdwExportedTypeFlags is not null)
                *pdwExportedTypeFlags = (uint)exportedType.Attributes;

            hr = truncated ? CldbHResults.CLDB_S_TRUNCATION : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
    {
        int hr = HResults.S_OK;
        try
        {
            if (ptkExportedType is not null)
                *ptkExportedType = 0;

            string targetName = new string(szName);

            bool found = false;
            foreach (ExportedTypeHandle eth in _reader.ExportedTypes)
            {
                ExportedType exportedType = _reader.GetExportedType(eth);
                string name = _reader.GetString(exportedType.Name);
                string ns = _reader.GetString(exportedType.Namespace);
                string fullName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

                if (!string.Equals(fullName, targetName, StringComparison.Ordinal))
                    continue;

                if (mdtExportedType != 0)
                {
                    EntityHandle impl = exportedType.Implementation;
                    if (impl.IsNil || (uint)MetadataTokens.GetToken(impl) != mdtExportedType)
                        continue;
                }

                if (ptkExportedType is not null)
                    *ptkExportedType = (uint)MetadataTokens.GetToken(eth);

                found = true;
                break;
            }

            if (!found)
                hr = CldbHResults.CLDB_E_RECORD_NOTFOUND;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.FindManifestResourceByName(szName, ptkManifestResource) : HResults.E_NOTIMPL;

    void IMetaDataAssemblyImport.CloseEnum(nint hEnum)
        => ((IMetaDataImport)this).CloseEnum(hEnum);

    int IMetaDataAssemblyImport.FindAssembliesByName(char* szAppBase, char* szPrivateBin, char* szAssemblyName,
        nint* ppIUnk, uint cMax, uint* pcAssemblies)
        => _legacyAssemblyImport is not null ? _legacyAssemblyImport.FindAssembliesByName(szAppBase, szPrivateBin, szAssemblyName, ppIUnk, cMax, pcAssemblies) : HResults.E_NOTIMPL;

    // Helpers and lookup builders

    private string GetTypeDefFullName(TypeDefinition typeDef)
    {
        string name = _reader.GetString(typeDef.Name);
        string ns = _reader.GetString(typeDef.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private string GetTypeRefFullName(TypeReference typeRef)
    {
        string name = _reader.GetString(typeRef.Name);
        string ns = _reader.GetString(typeRef.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    // Native RegMeta maps the global <Module> type (TypeDef RID 1) to mdTypeDefNil (0x00000000)
    // when returning parent tokens from GetMethodProps, GetFieldProps, and GetMemberRefProps.
    private static uint MapGlobalParentToken(uint token)
    {
        // TypeDef RID 1 has token 0x02000001
        return token == 0x02000001 ? 0 : token;
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

    private Dictionary<int, uint> BuildInterfaceImplLookup()
    {
        Dictionary<int, uint> lookup = new();
        foreach (TypeDefinitionHandle tdh in _reader.TypeDefinitions)
        {
            uint typeToken = (uint)MetadataTokens.GetToken(tdh);
            foreach (InterfaceImplementationHandle ih in _reader.GetTypeDefinition(tdh).GetInterfaceImplementations())
            {
                lookup[MetadataTokens.GetRowNumber(ih)] = typeToken;
            }
        }
        return lookup;
    }

    private Dictionary<int, uint> BuildParamToMethodLookup()
    {
        Dictionary<int, uint> lookup = new();
        foreach (TypeDefinitionHandle tdh in _reader.TypeDefinitions)
        {
            foreach (MethodDefinitionHandle mdh in _reader.GetTypeDefinition(tdh).GetMethods())
            {
                uint methodToken = (uint)MetadataTokens.GetToken(mdh);
                foreach (ParameterHandle ph in _reader.GetMethodDefinition(mdh).GetParameters())
                {
                    lookup[MetadataTokens.GetRowNumber(ph)] = methodToken;
                }
            }
        }
        return lookup;
    }

}
