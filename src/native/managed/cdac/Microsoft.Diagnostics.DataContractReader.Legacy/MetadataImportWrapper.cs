// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class MetadataImportWrapper : IMetaDataImport2
{
    private const int CLDB_E_RECORD_NOTFOUND = unchecked((int)0x80131130);
    private readonly MetadataReader _reader;

    public MetadataImportWrapper(MetadataReader reader)
    {
        _reader = reader;
    }

    private static int CatchHR(Func<int> action)
    {
        try
        {
            return action();
        }
        catch (BadImageFormatException)
        {
            return HResults.COR_E_BADIMAGEFORMAT;
        }
        catch (ArgumentOutOfRangeException)
        {
            return HResults.E_INVALIDARG;
        }
        catch (InvalidOperationException)
        {
            return HResults.E_FAIL;
        }
        catch (Exception ex) when (ex.HResult < 0)
        {
            return ex.HResult;
        }
        catch
        {
            return HResults.E_FAIL;
        }
    }

    // Helper: get the full name of a type definition (Namespace.Name).
    private string GetTypeDefFullName(TypeDefinition typeDef)
    {
        string name = _reader.GetString(typeDef.Name);
        string ns = _reader.GetString(typeDef.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    // Helper: get the full name of a type reference (Namespace.Name).
    private string GetTypeRefFullName(TypeReference typeRef)
    {
        string name = _reader.GetString(typeRef.Name);
        string ns = _reader.GetString(typeRef.Namespace);
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
        => HResults.E_NOTIMPL;

    public int ResetEnum(nint hEnum, uint ulPos)
        => HResults.E_NOTIMPL;

    public int EnumTypeDefs(nint* phEnum, uint* rTypeDefs, uint cMax, uint* pcTypeDefs)
        => HResults.E_NOTIMPL;

    public int EnumInterfaceImpls(nint* phEnum, uint td, uint* rImpls, uint cMax, uint* pcImpls)
    {
        return CatchHR(() =>
        {
            if (phEnum is not null && *phEnum != 0)
                return FillEnum(phEnum, GetEnum(*phEnum)!.Tokens, rImpls, cMax, pcImpls);

            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle((int)(td & 0x00FFFFFF));
            TypeDefinition typeDef = _reader.GetTypeDefinition(typeHandle);
            List<uint> tokens = new();
            foreach (InterfaceImplementationHandle h in typeDef.GetInterfaceImplementations())
                tokens.Add((uint)MetadataTokens.GetToken(h));
            return FillEnum(phEnum, tokens, rImpls, cMax, pcImpls);
        });
    }

    public int EnumTypeRefs(nint* phEnum, uint* rTypeRefs, uint cMax, uint* pcTypeRefs)
        => HResults.E_NOTIMPL;

    public int EnumMembers(nint* phEnum, uint cl, uint* rMembers, uint cMax, uint* pcTokens)
        => HResults.E_NOTIMPL;

    public int EnumMethods(nint* phEnum, uint cl, uint* rMethods, uint cMax, uint* pcTokens)
        => HResults.E_NOTIMPL;

    public int EnumFields(nint* phEnum, uint cl, uint* rFields, uint cMax, uint* pcTokens)
    {
        return CatchHR(() =>
        {
            if (phEnum is not null && *phEnum != 0)
                return FillEnum(phEnum, GetEnum(*phEnum)!.Tokens, rFields, cMax, pcTokens);

            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle((int)(cl & 0x00FFFFFF));
            TypeDefinition typeDef = _reader.GetTypeDefinition(typeHandle);
            List<uint> tokens = new();
            foreach (FieldDefinitionHandle h in typeDef.GetFields())
                tokens.Add((uint)MetadataTokens.GetToken(h));
            return FillEnum(phEnum, tokens, rFields, cMax, pcTokens);
        });
    }

    public int EnumCustomAttributes(nint* phEnum, uint tk, uint tkType, uint* rCustomAttributes, uint cMax, uint* pcCustomAttributes)
        => HResults.E_NOTIMPL;

    public int EnumGenericParams(nint* phEnum, uint tk, uint* rGenericParams, uint cMax, uint* pcGenericParams)
    {
        return CatchHR(() =>
        {
            if (phEnum is not null && *phEnum != 0)
                return FillEnum(phEnum, GetEnum(*phEnum)!.Tokens, rGenericParams, cMax, pcGenericParams);

            EntityHandle owner = MetadataTokens.EntityHandle((int)tk);
            GenericParameterHandleCollection genericParams;

            if (owner.Kind == HandleKind.TypeDefinition)
                genericParams = _reader.GetTypeDefinition((TypeDefinitionHandle)owner).GetGenericParameters();
            else if (owner.Kind == HandleKind.MethodDefinition)
                genericParams = _reader.GetMethodDefinition((MethodDefinitionHandle)owner).GetGenericParameters();
            else
                return HResults.E_INVALIDARG;

            List<uint> tokens = new();
            foreach (GenericParameterHandle h in genericParams)
                tokens.Add((uint)MetadataTokens.GetToken(h));
            return FillEnum(phEnum, tokens, rGenericParams, cMax, pcGenericParams);
        });
    }

    public int GetTypeDefProps(uint td, char* szTypeDef, uint cchTypeDef, uint* pchTypeDef, uint* pdwTypeDefFlags, uint* ptkExtends)
    {
        return CatchHR(() =>
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle((int)(td & 0x00FFFFFF));
            TypeDefinition typeDef = _reader.GetTypeDefinition(typeHandle);

            string fullName = GetTypeDefFullName(typeDef);
            OutputBufferHelpers.CopyStringToBuffer(szTypeDef, cchTypeDef, pchTypeDef, fullName);

            if (pdwTypeDefFlags is not null)
                *pdwTypeDefFlags = (uint)typeDef.Attributes;

            if (ptkExtends is not null)
            {
                EntityHandle baseType = typeDef.BaseType;
                *ptkExtends = baseType.IsNil ? 0 : (uint)MetadataTokens.GetToken(baseType);
            }

            return HResults.S_OK;
        });
    }

    public int GetTypeRefProps(uint tr, uint* ptkResolutionScope, char* szName, uint cchName, uint* pchName)
    {
        return CatchHR(() =>
        {
            TypeReferenceHandle refHandle = MetadataTokens.TypeReferenceHandle((int)(tr & 0x00FFFFFF));
            TypeReference typeRef = _reader.GetTypeReference(refHandle);

            string fullName = GetTypeRefFullName(typeRef);
            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, fullName);

            if (ptkResolutionScope is not null)
            {
                EntityHandle scope = typeRef.ResolutionScope;
                *ptkResolutionScope = scope.IsNil ? 0 : (uint)MetadataTokens.GetToken(scope);
            }

            return HResults.S_OK;
        });
    }

    public int GetMethodProps(uint mb, uint* pClass, char* szMethod, uint cchMethod, uint* pchMethod,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags)
    {
        return CatchHR(() =>
        {
            MethodDefinitionHandle methodHandle = MetadataTokens.MethodDefinitionHandle((int)(mb & 0x00FFFFFF));
            MethodDefinition methodDef = _reader.GetMethodDefinition(methodHandle);

            string name = _reader.GetString(methodDef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szMethod, cchMethod, pchMethod, name);

            if (pClass is not null)
                *pClass = (uint)MetadataTokens.GetToken(methodDef.GetDeclaringType());

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

            return HResults.S_OK;
        });
    }

    public int GetFieldProps(uint mb, uint* pClass, char* szField, uint cchField, uint* pchField,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pdwCPlusTypeFlag,
        void** ppValue, uint* pcchValue)
    {
        return CatchHR(() =>
        {
            FieldDefinitionHandle fieldHandle = MetadataTokens.FieldDefinitionHandle((int)(mb & 0x00FFFFFF));
            FieldDefinition fieldDef = _reader.GetFieldDefinition(fieldHandle);

            string name = _reader.GetString(fieldDef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szField, cchField, pchField, name);

            if (pClass is not null)
                *pClass = (uint)MetadataTokens.GetToken(fieldDef.GetDeclaringType());

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
                *pdwCPlusTypeFlag = 0;
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
                        *pcchValue = (uint)valueReader.Length;
                }
            }

            return HResults.S_OK;
        });
    }

    public int GetMemberProps(uint mb, uint* pClass, char* szMember, uint cchMember, uint* pchMember,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags,
        uint* pdwCPlusTypeFlag, void** ppValue, uint* pcchValue)
    {
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
        return CatchHR(() =>
        {
            InterfaceImplementationHandle implHandle = MetadataTokens.InterfaceImplementationHandle((int)(iiImpl & 0x00FFFFFF));
            InterfaceImplementation impl = _reader.GetInterfaceImplementation(implHandle);

            if (pClass is not null)
            {
                *pClass = 0;
                foreach (TypeDefinitionHandle tdh in _reader.TypeDefinitions)
                {
                    TypeDefinition td = _reader.GetTypeDefinition(tdh);
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

            return HResults.S_OK;
        });
    }

    public int GetNestedClassProps(uint tdNestedClass, uint* ptdEnclosingClass)
    {
        return CatchHR(() =>
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle((int)(tdNestedClass & 0x00FFFFFF));
            TypeDefinition typeDef = _reader.GetTypeDefinition(typeHandle);
            TypeDefinitionHandle declaringType = typeDef.GetDeclaringType();

            if (ptdEnclosingClass is not null)
                *ptdEnclosingClass = declaringType.IsNil ? 0 : (uint)MetadataTokens.GetToken(declaringType);

            return declaringType.IsNil ? CLDB_E_RECORD_NOTFOUND : HResults.S_OK;
        });
    }

    public int GetGenericParamProps(uint gp, uint* pulParamSeq, uint* pdwParamFlags, uint* ptOwner,
        uint* reserved, char* wzname, uint cchName, uint* pchName)
    {
        return CatchHR(() =>
        {
            GenericParameterHandle gpHandle = MetadataTokens.GenericParameterHandle((int)(gp & 0x00FFFFFF));
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
            OutputBufferHelpers.CopyStringToBuffer(wzname, cchName, pchName, name);

            return HResults.S_OK;
        });
    }

    public int GetRVA(uint tk, uint* pulCodeRVA, uint* pdwImplFlags)
    {
        return CatchHR(() =>
        {
            uint tableIndex = tk >> 24;
            if (tableIndex == 0x06) // MethodDef
            {
                MethodDefinitionHandle methodHandle = MetadataTokens.MethodDefinitionHandle((int)(tk & 0x00FFFFFF));
                MethodDefinition methodDef = _reader.GetMethodDefinition(methodHandle);
                if (pulCodeRVA is not null)
                    *pulCodeRVA = (uint)methodDef.RelativeVirtualAddress;
                if (pdwImplFlags is not null)
                    *pdwImplFlags = (uint)methodDef.ImplAttributes;
                return HResults.S_OK;
            }

            if (tableIndex == 0x04) // FieldDef
            {
                FieldDefinitionHandle fieldHandle = MetadataTokens.FieldDefinitionHandle((int)(tk & 0x00FFFFFF));
                FieldDefinition fieldDef = _reader.GetFieldDefinition(fieldHandle);
                if (pulCodeRVA is not null)
                    *pulCodeRVA = (uint)fieldDef.GetRelativeVirtualAddress();
                if (pdwImplFlags is not null)
                    *pdwImplFlags = 0;
                return HResults.S_OK;
            }

            return HResults.E_INVALIDARG;
        });
    }

    public int GetSigFromToken(uint mdSig, byte** ppvSig, uint* pcbSig)
    {
        return CatchHR(() =>
        {
            StandaloneSignatureHandle sigHandle = MetadataTokens.StandaloneSignatureHandle((int)(mdSig & 0x00FFFFFF));
            StandaloneSignature sig = _reader.GetStandaloneSignature(sigHandle);
            BlobReader blobReader = _reader.GetBlobReader(sig.Signature);

            if (ppvSig is not null)
                *ppvSig = blobReader.StartPointer;
            if (pcbSig is not null)
                *pcbSig = (uint)blobReader.Length;

            return HResults.S_OK;
        });
    }

    public int GetCustomAttributeByName(uint tkObj, char* szName, void** ppData, uint* pcbData)
    {
        return CatchHR(() =>
        {
            if (ppData is not null)
                *ppData = null;
            if (pcbData is not null)
                *pcbData = 0;

            string targetName = new string(szName);
            EntityHandle parent = MetadataTokens.EntityHandle((int)tkObj);

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
                    return HResults.S_OK;
                }
            }

            return HResults.S_FALSE;
        });
    }

    public int IsValidToken(uint tk)
    {
        int rid = (int)(tk & 0x00FFFFFF);
        int table = (int)(tk >> 24);

        if (rid == 0)
            return 0; // FALSE

        if (table < 0 || table > (int)TableIndex.CustomDebugInformation)
            return 0; // FALSE

        int rowCount = _reader.GetTableRowCount((TableIndex)table);
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

    public int FindTypeDefByName(char* szTypeDef, uint tkEnclosingClass, uint* ptd)
    {
        return CatchHR(() =>
        {
            if (ptd is not null)
                *ptd = 0;

            string targetName = new string(szTypeDef);

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

                return HResults.S_OK;
            }

            return CLDB_E_RECORD_NOTFOUND;
        });
    }

    public int GetScopeProps(char* szName, uint cchName, uint* pchName, Guid* pmvid)
        => HResults.E_NOTIMPL;

    public int GetModuleFromScope(uint* pmd)
        => HResults.E_NOTIMPL;

    public int ResolveTypeRef(uint tr, Guid* riid, void** ppIScope, uint* ptd)
        => HResults.E_NOTIMPL;

    public int EnumMembersWithName(nint* phEnum, uint cl, char* szName, uint* rMembers, uint cMax, uint* pcTokens)
        => HResults.E_NOTIMPL;

    public int EnumMethodsWithName(nint* phEnum, uint cl, char* szName, uint* rMethods, uint cMax, uint* pcTokens)
        => HResults.E_NOTIMPL;

    public int EnumFieldsWithName(nint* phEnum, uint cl, char* szName, uint* rFields, uint cMax, uint* pcTokens)
        => HResults.E_NOTIMPL;

    public int EnumParams(nint* phEnum, uint mb, uint* rParams, uint cMax, uint* pcTokens)
        => HResults.E_NOTIMPL;

    public int EnumMemberRefs(nint* phEnum, uint tkParent, uint* rMemberRefs, uint cMax, uint* pcTokens)
        => HResults.E_NOTIMPL;

    public int EnumMethodImpls(nint* phEnum, uint td, uint* rMethodBody, uint* rMethodDecl, uint cMax, uint* pcTokens)
        => HResults.E_NOTIMPL;

    public int EnumPermissionSets(nint* phEnum, uint tk, uint dwActions, uint* rPermission, uint cMax, uint* pcTokens)
        => HResults.E_NOTIMPL;

    public int FindMember(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
        => HResults.E_NOTIMPL;

    public int FindMethod(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
        => HResults.E_NOTIMPL;

    public int FindField(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb)
        => HResults.E_NOTIMPL;

    public int FindMemberRef(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmr)
        => HResults.E_NOTIMPL;

    public int GetMemberRefProps(uint mr, uint* ptk, char* szMember, uint cchMember, uint* pchMember,
        byte** ppvSigBlob, uint* pbSig)
    {
        return CatchHR(() =>
        {
            MemberReferenceHandle refHandle = MetadataTokens.MemberReferenceHandle((int)(mr & 0x00FFFFFF));
            MemberReference memberRef = _reader.GetMemberReference(refHandle);

            string name = _reader.GetString(memberRef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szMember, cchMember, pchMember, name);

            if (ptk is not null)
                *ptk = (uint)MetadataTokens.GetToken(memberRef.Parent);

            if (ppvSigBlob is not null || pbSig is not null)
            {
                BlobReader blobReader = _reader.GetBlobReader(memberRef.Signature);
                if (ppvSigBlob is not null)
                    *ppvSigBlob = blobReader.StartPointer;
                if (pbSig is not null)
                    *pbSig = (uint)blobReader.Length;
            }

            return HResults.S_OK;
        });
    }

    public int EnumProperties(nint* phEnum, uint td, uint* rProperties, uint cMax, uint* pcProperties)
        => HResults.E_NOTIMPL;

    public int EnumEvents(nint* phEnum, uint td, uint* rEvents, uint cMax, uint* pcEvents)
        => HResults.E_NOTIMPL;

    public int GetEventProps(uint ev, uint* pClass, char* szEvent, uint cchEvent, uint* pchEvent,
        uint* pdwEventFlags, uint* ptkEventType, uint* pmdAddOn, uint* pmdRemoveOn, uint* pmdFire,
        uint* rmdOtherMethod, uint cMax, uint* pcOtherMethod)
        => HResults.E_NOTIMPL;

    public int EnumMethodSemantics(nint* phEnum, uint mb, uint* rEventProp, uint cMax, uint* pcEventProp)
        => HResults.E_NOTIMPL;

    public int GetMethodSemantics(uint mb, uint tkEventProp, uint* pdwSemanticsFlags)
        => HResults.E_NOTIMPL;

    public int GetClassLayout(uint td, uint* pdwPackSize, void* rFieldOffset, uint cMax, uint* pcFieldOffset, uint* pulClassSize)
    {
        return CatchHR(() =>
        {
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle((int)(td & 0x00FFFFFF));
            TypeLayout layout = _reader.GetTypeDefinition(typeHandle).GetLayout();

            if (layout.IsDefault)
                return CLDB_E_RECORD_NOTFOUND;

            if (pdwPackSize is not null)
                *pdwPackSize = (uint)layout.PackingSize;

            if (pulClassSize is not null)
                *pulClassSize = (uint)layout.Size;

            if (pcFieldOffset is not null)
                *pcFieldOffset = 0;

            return HResults.S_OK;
        });
    }

    public int GetFieldMarshal(uint tk, byte** ppvNativeType, uint* pcbNativeType)
        => HResults.E_NOTIMPL;

    public int GetPermissionSetProps(uint pm, uint* pdwAction, void** ppvPermission, uint* pcbPermission)
        => HResults.E_NOTIMPL;

    public int GetModuleRefProps(uint mur, char* szName, uint cchName, uint* pchName)
    {
        return CatchHR(() =>
        {
            ModuleReferenceHandle modRefHandle = MetadataTokens.ModuleReferenceHandle((int)(mur & 0x00FFFFFF));
            ModuleReference modRef = _reader.GetModuleReference(modRefHandle);

            string name = _reader.GetString(modRef.Name);
            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, name);

            return HResults.S_OK;
        });
    }

    public int EnumModuleRefs(nint* phEnum, uint* rModuleRefs, uint cmax, uint* pcModuleRefs)
        => HResults.E_NOTIMPL;

    public int GetTypeSpecFromToken(uint typespec, byte** ppvSig, uint* pcbSig)
    {
        return CatchHR(() =>
        {
            TypeSpecificationHandle tsHandle = MetadataTokens.TypeSpecificationHandle((int)(typespec & 0x00FFFFFF));
            TypeSpecification typeSpec = _reader.GetTypeSpecification(tsHandle);
            BlobReader blobReader = _reader.GetBlobReader(typeSpec.Signature);

            if (ppvSig is not null)
                *ppvSig = blobReader.StartPointer;
            if (pcbSig is not null)
                *pcbSig = (uint)blobReader.Length;

            return HResults.S_OK;
        });
    }

    public int GetNameFromToken(uint tk, byte** pszUtf8NamePtr)
        => HResults.E_NOTIMPL;

    public int EnumUnresolvedMethods(nint* phEnum, uint* rMethods, uint cMax, uint* pcTokens)
        => HResults.E_NOTIMPL;

    public int GetUserString(uint stk, char* szString, uint cchString, uint* pchString)
    {
        return CatchHR(() =>
        {
            UserStringHandle usHandle = MetadataTokens.UserStringHandle((int)(stk & 0x00FFFFFF));
            string value = _reader.GetUserString(usHandle);
            OutputBufferHelpers.CopyStringToBuffer(szString, cchString, pchString, value);

            return HResults.S_OK;
        });
    }

    public int GetPinvokeMap(uint tk, uint* pdwMappingFlags, char* szImportName, uint cchImportName,
        uint* pchImportName, uint* pmrImportDLL)
        => HResults.E_NOTIMPL;

    public int EnumSignatures(nint* phEnum, uint* rSignatures, uint cmax, uint* pcSignatures)
        => HResults.E_NOTIMPL;

    public int EnumTypeSpecs(nint* phEnum, uint* rTypeSpecs, uint cmax, uint* pcTypeSpecs)
        => HResults.E_NOTIMPL;

    public int EnumUserStrings(nint* phEnum, uint* rStrings, uint cmax, uint* pcStrings)
        => HResults.E_NOTIMPL;

    public int GetParamForMethodIndex(uint md, uint ulParamSeq, uint* ppd)
    {
        return CatchHR(() =>
        {
            if (ppd is not null)
                *ppd = 0;

            MethodDefinitionHandle methodHandle = MetadataTokens.MethodDefinitionHandle((int)(md & 0x00FFFFFF));
            MethodDefinition methodDef = _reader.GetMethodDefinition(methodHandle);

            foreach (ParameterHandle ph in methodDef.GetParameters())
            {
                Parameter param = _reader.GetParameter(ph);
                if (param.SequenceNumber == (int)ulParamSeq)
                {
                    if (ppd is not null)
                        *ppd = (uint)MetadataTokens.GetToken(ph);
                    return HResults.S_OK;
                }
            }

            return CLDB_E_RECORD_NOTFOUND;
        });
    }

    public int GetCustomAttributeProps(uint cv, uint* ptkObj, uint* ptkType, void** ppBlob, uint* pcbSize)
        => HResults.E_NOTIMPL;

    public int FindTypeRef(uint tkResolutionScope, char* szName, uint* ptr)
        => HResults.E_NOTIMPL;

    public int GetPropertyProps(uint prop, uint* pClass, char* szProperty, uint cchProperty, uint* pchProperty,
        uint* pdwPropFlags, byte** ppvSig, uint* pbSig, uint* pdwCPlusTypeFlag,
        void** ppDefaultValue, uint* pcchDefaultValue, uint* pmdSetter, uint* pmdGetter,
        uint* rmdOtherMethod, uint cMax, uint* pcOtherMethod)
        => HResults.E_NOTIMPL;

    public int GetParamProps(uint tk, uint* pmd, uint* pulSequence, char* szName, uint cchName, uint* pchName,
        uint* pdwAttr, uint* pdwCPlusTypeFlag, void** ppValue, uint* pcchValue)
    {
        return CatchHR(() =>
        {
            ParameterHandle paramHandle = MetadataTokens.ParameterHandle((int)(tk & 0x00FFFFFF));
            Parameter param = _reader.GetParameter(paramHandle);

            string name = _reader.GetString(param.Name);
            OutputBufferHelpers.CopyStringToBuffer(szName, cchName, pchName, name);

            if (pmd is not null)
            {
                *pmd = 0;
                foreach (TypeDefinitionHandle tdh in _reader.TypeDefinitions)
                {
                    TypeDefinition td = _reader.GetTypeDefinition(tdh);
                    foreach (MethodDefinitionHandle mdh in td.GetMethods())
                    {
                        MethodDefinition method = _reader.GetMethodDefinition(mdh);
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
                Constant constant = _reader.GetConstant(constHandle);
                if (pdwCPlusTypeFlag is not null)
                    *pdwCPlusTypeFlag = (uint)constant.TypeCode;
                if (ppValue is not null || pcchValue is not null)
                {
                    BlobReader valueReader = _reader.GetBlobReader(constant.Value);
                    if (ppValue is not null)
                        *ppValue = valueReader.StartPointer;
                    if (pcchValue is not null)
                        *pcchValue = (uint)valueReader.Length;
                }
            }

            return HResults.S_OK;
        });
    }

    public int GetNativeCallConvFromSig(void* pvSig, uint cbSig, uint* pCallConv)
        => HResults.E_NOTIMPL;

    public int IsGlobal(uint pd, int* pbGlobal)
        => HResults.E_NOTIMPL;

    // IMetaDataImport2 stubs
    public int GetMethodSpecProps(uint mi, uint* tkParent, byte** ppvSigBlob, uint* pcbSigBlob)
        => HResults.E_NOTIMPL;

    public int EnumGenericParamConstraints(nint* phEnum, uint tk, uint* rGenericParamConstraints, uint cMax, uint* pcGenericParamConstraints)
        => HResults.E_NOTIMPL;

    public int GetGenericParamConstraintProps(uint gpc, uint* ptGenericParam, uint* ptkConstraintType)
        => HResults.E_NOTIMPL;

    public int GetPEKind(uint* pdwPEKind, uint* pdwMachine)
        => HResults.E_NOTIMPL;

    public int GetVersionString(char* pwzBuf, uint ccBufSize, uint* pccBufSize)
        => HResults.E_NOTIMPL;

    public int EnumMethodSpecs(nint* phEnum, uint tk, uint* rMethodSpecs, uint cMax, uint* pcMethodSpecs)
        => HResults.E_NOTIMPL;

}
