// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

// Managed declarations for IMetaDataImport and IMetaDataImport2.
// See src/coreclr/inc/cor.h
// Vtable ordering must exactly match the native interface.

[GeneratedComInterface]
[Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
public unsafe partial interface IMetaDataImport
{
    [PreserveSig]
    void CloseEnum(nint hEnum);

    [PreserveSig]
    int CountEnum(nint hEnum, uint* pulCount);

    [PreserveSig]
    int ResetEnum(nint hEnum, uint ulPos);

    [PreserveSig]
    int EnumTypeDefs(nint* phEnum, uint* rTypeDefs, uint cMax, uint* pcTypeDefs);

    [PreserveSig]
    int EnumInterfaceImpls(nint* phEnum, uint td, uint* rImpls, uint cMax, uint* pcImpls);

    [PreserveSig]
    int EnumTypeRefs(nint* phEnum, uint* rTypeRefs, uint cMax, uint* pcTypeRefs);

    [PreserveSig]
    int FindTypeDefByName(char* szTypeDef, uint tkEnclosingClass, uint* ptd);

    [PreserveSig]
    int GetScopeProps(char* szName, uint cchName, uint* pchName, Guid* pmvid);

    [PreserveSig]
    int GetModuleFromScope(uint* pmd);

    [PreserveSig]
    int GetTypeDefProps(uint td, char* szTypeDef, uint cchTypeDef, uint* pchTypeDef, uint* pdwTypeDefFlags, uint* ptkExtends);

    [PreserveSig]
    int GetInterfaceImplProps(uint iiImpl, uint* pClass, uint* ptkIface);

    [PreserveSig]
    int GetTypeRefProps(uint tr, uint* ptkResolutionScope, char* szName, uint cchName, uint* pchName);

    [PreserveSig]
    int ResolveTypeRef(uint tr, Guid* riid, void** ppIScope, uint* ptd);

    [PreserveSig]
    int EnumMembers(nint* phEnum, uint cl, uint* rMembers, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumMembersWithName(nint* phEnum, uint cl, char* szName, uint* rMembers, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumMethods(nint* phEnum, uint cl, uint* rMethods, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumMethodsWithName(nint* phEnum, uint cl, char* szName, uint* rMethods, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumFields(nint* phEnum, uint cl, uint* rFields, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumFieldsWithName(nint* phEnum, uint cl, char* szName, uint* rFields, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumParams(nint* phEnum, uint mb, uint* rParams, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumMemberRefs(nint* phEnum, uint tkParent, uint* rMemberRefs, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumMethodImpls(nint* phEnum, uint td, uint* rMethodBody, uint* rMethodDecl, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumPermissionSets(nint* phEnum, uint tk, uint dwActions, uint* rPermission, uint cMax, uint* pcTokens);

    [PreserveSig]
    int FindMember(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb);

    [PreserveSig]
    int FindMethod(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb);

    [PreserveSig]
    int FindField(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmb);

    [PreserveSig]
    int FindMemberRef(uint td, char* szName, byte* pvSigBlob, uint cbSigBlob, uint* pmr);

    [PreserveSig]
    int GetMethodProps(uint mb, uint* pClass, char* szMethod, uint cchMethod, uint* pchMethod,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags);

    [PreserveSig]
    int GetMemberRefProps(uint mr, uint* ptk, char* szMember, uint cchMember, uint* pchMember,
        byte** ppvSigBlob, uint* pbSig);

    [PreserveSig]
    int EnumProperties(nint* phEnum, uint td, uint* rProperties, uint cMax, uint* pcProperties);

    [PreserveSig]
    int EnumEvents(nint* phEnum, uint td, uint* rEvents, uint cMax, uint* pcEvents);

    [PreserveSig]
    int GetEventProps(uint ev, uint* pClass, char* szEvent, uint cchEvent, uint* pchEvent,
        uint* pdwEventFlags, uint* ptkEventType, uint* pmdAddOn, uint* pmdRemoveOn, uint* pmdFire,
        uint* rmdOtherMethod, uint cMax, uint* pcOtherMethod);

    [PreserveSig]
    int EnumMethodSemantics(nint* phEnum, uint mb, uint* rEventProp, uint cMax, uint* pcEventProp);

    [PreserveSig]
    int GetMethodSemantics(uint mb, uint tkEventProp, uint* pdwSemanticsFlags);

    [PreserveSig]
    int GetClassLayout(uint td, uint* pdwPackSize, void* rFieldOffset, uint cMax, uint* pcFieldOffset, uint* pulClassSize);

    [PreserveSig]
    int GetFieldMarshal(uint tk, byte** ppvNativeType, uint* pcbNativeType);

    [PreserveSig]
    int GetRVA(uint tk, uint* pulCodeRVA, uint* pdwImplFlags);

    [PreserveSig]
    int GetPermissionSetProps(uint pm, uint* pdwAction, void** ppvPermission, uint* pcbPermission);

    [PreserveSig]
    int GetSigFromToken(uint mdSig, byte** ppvSig, uint* pcbSig);

    [PreserveSig]
    int GetModuleRefProps(uint mur, char* szName, uint cchName, uint* pchName);

    [PreserveSig]
    int EnumModuleRefs(nint* phEnum, uint* rModuleRefs, uint cmax, uint* pcModuleRefs);

    [PreserveSig]
    int GetTypeSpecFromToken(uint typespec, byte** ppvSig, uint* pcbSig);

    [PreserveSig]
    int GetNameFromToken(uint tk, byte** pszUtf8NamePtr);

    [PreserveSig]
    int EnumUnresolvedMethods(nint* phEnum, uint* rMethods, uint cMax, uint* pcTokens);

    [PreserveSig]
    int GetUserString(uint stk, char* szString, uint cchString, uint* pchString);

    [PreserveSig]
    int GetPinvokeMap(uint tk, uint* pdwMappingFlags, char* szImportName, uint cchImportName,
        uint* pchImportName, uint* pmrImportDLL);

    [PreserveSig]
    int EnumSignatures(nint* phEnum, uint* rSignatures, uint cmax, uint* pcSignatures);

    [PreserveSig]
    int EnumTypeSpecs(nint* phEnum, uint* rTypeSpecs, uint cmax, uint* pcTypeSpecs);

    [PreserveSig]
    int EnumUserStrings(nint* phEnum, uint* rStrings, uint cmax, uint* pcStrings);

    [PreserveSig]
    int GetParamForMethodIndex(uint md, uint ulParamSeq, uint* ppd);

    [PreserveSig]
    int EnumCustomAttributes(nint* phEnum, uint tk, uint tkType, uint* rCustomAttributes, uint cMax, uint* pcCustomAttributes);

    [PreserveSig]
    int GetCustomAttributeProps(uint cv, uint* ptkObj, uint* ptkType, void** ppBlob, uint* pcbSize);

    [PreserveSig]
    int FindTypeRef(uint tkResolutionScope, char* szName, uint* ptr);

    [PreserveSig]
    int GetMemberProps(uint mb, uint* pClass, char* szMember, uint cchMember, uint* pchMember,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags,
        uint* pdwCPlusTypeFlag, void** ppValue, uint* pcchValue);

    [PreserveSig]
    int GetFieldProps(uint mb, uint* pClass, char* szField, uint cchField, uint* pchField,
        uint* pdwAttr, byte** ppvSigBlob, uint* pcbSigBlob, uint* pdwCPlusTypeFlag,
        void** ppValue, uint* pcchValue);

    [PreserveSig]
    int GetPropertyProps(uint prop, uint* pClass, char* szProperty, uint cchProperty, uint* pchProperty,
        uint* pdwPropFlags, byte** ppvSig, uint* pbSig, uint* pdwCPlusTypeFlag,
        void** ppDefaultValue, uint* pcchDefaultValue, uint* pmdSetter, uint* pmdGetter,
        uint* rmdOtherMethod, uint cMax, uint* pcOtherMethod);

    [PreserveSig]
    int GetParamProps(uint tk, uint* pmd, uint* pulSequence, char* szName, uint cchName, uint* pchName,
        uint* pdwAttr, uint* pdwCPlusTypeFlag, void** ppValue, uint* pcchValue);

    [PreserveSig]
    int GetCustomAttributeByName(uint tkObj, char* szName, void** ppData, uint* pcbData);

    [PreserveSig]
    int IsValidToken(uint tk);

    [PreserveSig]
    int GetNestedClassProps(uint tdNestedClass, uint* ptdEnclosingClass);

    [PreserveSig]
    int GetNativeCallConvFromSig(void* pvSig, uint cbSig, uint* pCallConv);

    [PreserveSig]
    int IsGlobal(uint pd, int* pbGlobal);
}

[GeneratedComInterface]
[Guid("FCE5EFA0-8BBA-4f8e-A036-8F2022B08466")]
public unsafe partial interface IMetaDataImport2 : IMetaDataImport
{
    [PreserveSig]
    int EnumGenericParams(nint* phEnum, uint tk, uint* rGenericParams, uint cMax, uint* pcGenericParams);

    [PreserveSig]
    int GetGenericParamProps(uint gp, uint* pulParamSeq, uint* pdwParamFlags, uint* ptOwner,
        uint* reserved, char* wzname, uint cchName, uint* pchName);

    [PreserveSig]
    int GetMethodSpecProps(uint mi, uint* tkParent, byte** ppvSigBlob, uint* pcbSigBlob);

    [PreserveSig]
    int EnumGenericParamConstraints(nint* phEnum, uint tk, uint* rGenericParamConstraints, uint cMax, uint* pcGenericParamConstraints);

    [PreserveSig]
    int GetGenericParamConstraintProps(uint gpc, uint* ptGenericParam, uint* ptkConstraintType);

    [PreserveSig]
    int GetPEKind(uint* pdwPEKind, uint* pdwMachine);

    [PreserveSig]
    int GetVersionString(char* pwzBuf, uint ccBufSize, uint* pccBufSize);

    [PreserveSig]
    int EnumMethodSpecs(nint* phEnum, uint tk, uint* rMethodSpecs, uint cMax, uint* pcMethodSpecs);
}

// ASSEMBLYMETADATA from cor.h — version + locale info for assemblies.
// rProcessor and rOS are deprecated and always null/0 in modern usage.
// This struct contains pointer-sized fields (szLocale, rProcessor, rOS) which is safe because
// it is only used in same-process COM interop where bitness always matches the caller.
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ASSEMBLYMETADATA
{
    public ushort usMajorVersion;
    public ushort usMinorVersion;
    public ushort usBuildNumber;
    public ushort usRevisionNumber;
    public char* szLocale;
    public uint cbLocale;
    public uint* rProcessor;
    public uint ulProcessor;
    public nint rOS;
    public uint ulOS;
}

[GeneratedComInterface]
[Guid("EE62470B-E94B-424E-9B7C-2F00C9249F93")]
public unsafe partial interface IMetaDataAssemblyImport
{
    [PreserveSig]
    int GetAssemblyProps(uint mda, byte** ppbPublicKey, uint* pcbPublicKey, uint* pulHashAlgId,
        char* szName, uint cchName, uint* pchName, ASSEMBLYMETADATA* pMetaData, uint* pdwAssemblyFlags);

    [PreserveSig]
    int GetAssemblyRefProps(uint mdar, byte** ppbPublicKeyOrToken, uint* pcbPublicKeyOrToken,
        char* szName, uint cchName, uint* pchName, ASSEMBLYMETADATA* pMetaData,
        byte** ppbHashValue, uint* pcbHashValue, uint* pdwAssemblyRefFlags);

    [PreserveSig]
    int GetFileProps(uint mdf, char* szName, uint cchName, uint* pchName,
        byte** ppbHashValue, uint* pcbHashValue, uint* pdwFileFlags);

    [PreserveSig]
    int GetExportedTypeProps(uint mdct, char* szName, uint cchName, uint* pchName,
        uint* ptkImplementation, uint* ptkTypeDef, uint* pdwExportedTypeFlags);

    [PreserveSig]
    int GetManifestResourceProps(uint mdmr, char* szName, uint cchName, uint* pchName,
        uint* ptkImplementation, uint* pdwOffset, uint* pdwResourceFlags);

    [PreserveSig]
    int EnumAssemblyRefs(nint* phEnum, uint* rAssemblyRefs, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumFiles(nint* phEnum, uint* rFiles, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumExportedTypes(nint* phEnum, uint* rExportedTypes, uint cMax, uint* pcTokens);

    [PreserveSig]
    int EnumManifestResources(nint* phEnum, uint* rManifestResources, uint cMax, uint* pcTokens);

    [PreserveSig]
    int GetAssemblyFromScope(uint* ptkAssembly);

    [PreserveSig]
    int FindExportedTypeByName(char* szName, uint mdtExportedType, uint* ptkExportedType);

    [PreserveSig]
    int FindManifestResourceByName(char* szName, uint* ptkManifestResource);

    [PreserveSig]
    void CloseEnum(nint hEnum);

    [PreserveSig]
    int FindAssembliesByName(char* szAppBase, char* szPrivateBin, char* szAssemblyName,
        nint* ppIUnk, uint cMax, uint* pcAssemblies);
}

internal static class CldbHResults
{
    public const int CLDB_E_RECORD_NOTFOUND = unchecked((int)0x80131130);
    public const int CLDB_E_FILE_CORRUPT = unchecked((int)0x8013110E);
    public const int CLDB_S_TRUNCATION = 0x00131106;
}
