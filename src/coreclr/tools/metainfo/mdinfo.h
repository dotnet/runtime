// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _mdinfo_h
#define _mdinfo_h

#include "winwrap.h"
#include "cor.h"
#include "corhlprpriv.h"

#ifdef TARGET_UNIX
#include <oleauto.h>
#endif

#define STRING_BUFFER_LEN 4096

typedef void (*strPassBackFn)(const char *str);

class MDInfo {
public:
    enum DUMP_FILTER
    {
        dumpDefault     = 0x00000000,               // Dump everything but debugger data.
        dumpSchema      = 0x00000002,               // Dump the metadata schema.
        dumpRaw         = 0x00000004,               // Dump the metadata in raw table format.
        dumpHeader      = 0x00000008,               // Dump just the metadata header info.
        dumpCSV         = 0x00000010,               // Dump the metadata header info in CSV format.
        dumpUnsat       = 0x00000020,               // Dump unresolved methods or memberref
        dumpAssem       = 0x00000040,
        dumpStats       = 0x00000080,               // Dump more statistics about tables.
        dumpMoreHex     = 0x00000100,               // Dump more things in hex.
        dumpValidate    = 0x00000200,               // Validate MetaData.
        dumpRawHeaps    = 0x00000400,               // Also dump the heaps in the raw dump.
        dumpNoLogo      = 0x00000800,               // Don't display the logo or MVID
        dumpNames       = 0x00001000,               // In a hex dump, display the names, as well as string #'s.
    };


public:
    MDInfo(IMetaDataImport2* pImport, IMetaDataAssemblyImport* pAssemblyImport, LPCWSTR szScope, strPassBackFn inPBFn, ULONG DumpFilter);
    MDInfo(IMetaDataDispenserEx *pDispenser, LPCWSTR  szScope, strPassBackFn inPBFn, ULONG DumpFilter=dumpDefault);
    MDInfo(IMetaDataDispenserEx *pDispenser, PBYTE pManifest, DWORD dwSize, strPassBackFn inPBFn, ULONG DumpFilter=dumpDefault);
    ~MDInfo();

    void DisplayMD(void);

#ifdef FEATURE_COMINTEROP
    LPCWSTR VariantAsString(VARIANT *pVariant);
#endif

    void DisplayVersionInfo(void);

    void DisplayScopeInfo(void);

    void DisplayGlobalFunctions(void);
    void DisplayGlobalFields(void);
    void DisplayFieldRVA(mdFieldDef field);
    void DisplayGlobalMemberRefs(void);

    void DisplayTypeDefs(void);
    void DisplayTypeDefInfo(mdTypeDef inTypeDef);
    void DisplayTypeDefProps(mdTypeDef inTypeDef);

    void DisplayModuleRefs(void);
    void DisplayModuleRefInfo(mdModuleRef inModuleRef);

    void DisplaySignatures(void);
    void DisplaySignatureInfo(mdSignature inSignature);

    LPCWSTR TokenName(mdToken inToken, _Out_writes_(bufLen) LPWSTR buffer, ULONG bufLen);

    LPCWSTR TypeDeforRefName(mdToken inToken, _Out_writes_(bufLen) LPWSTR buffer, ULONG bufLen);
    LPCWSTR TypeDefName(mdTypeDef inTypeDef, _Out_writes_(bufLen) LPWSTR buffer, ULONG bufLen);
    LPCWSTR TypeRefName(mdTypeRef tr, _Out_writes_(bufLen) LPWSTR buffer, ULONG bufLen);

    LPCWSTR MemberDeforRefName(mdToken inToken, _Out_writes_(bufLen) LPWSTR buffer, ULONG bufLen);
    LPCWSTR MemberRefName(mdToken inMemRef, _Out_writes_(bufLen) LPWSTR buffer, ULONG bufLen);
    LPCWSTR MemberName(mdToken inMember, _Out_writes_(bufLen) LPWSTR buffer, ULONG bufLen);

    LPCWSTR MethodName(mdMethodDef inToken, _Out_writes_(bufLen) LPWSTR buffer, ULONG bufLen);
    LPCWSTR FieldName(mdFieldDef inToken, _Out_writes_(bufLen) LPWSTR buffer, ULONG bufLen);

    char *ClassFlags(DWORD flags, _Out_writes_(STRING_BUFFER_LEN) char *sFlags);

    void DisplayTypeRefs(void);
    void DisplayTypeRefInfo(mdTypeRef tr);
    void DisplayTypeSpecs(void);
    void DisplayTypeSpecInfo(mdTypeSpec ts, const char *preFix);
    void DisplayMethodSpecs(void);
    void DisplayMethodSpecInfo(mdMethodSpec ms, const char *preFix);

    void DisplayCorNativeLink(COR_NATIVE_LINK *pCorNLnk, const char *preFix);
    void DisplayCustomAttributeInfo(mdCustomAttribute inValue, const char *preFix);
    void DisplayCustomAttributes(mdToken inToken, const char *preFix);

    void DisplayInterfaceImpls(mdTypeDef inTypeDef);
    void DisplayInterfaceImplInfo(mdInterfaceImpl inImpl);

    LPWSTR GUIDAsString(GUID inGuid, _Out_writes_(bufLen) LPWSTR guidString, ULONG bufLen);

    const char *TokenTypeName(mdToken inToken);

    void DisplayMemberInfo(mdToken inMember);
    void DisplayMethodInfo(mdMethodDef method, DWORD *pflags = 0);
    void DisplayFieldInfo(mdFieldDef field, DWORD *pflags = 0);

    void DisplayMethods(mdTypeDef inTypeDef);
    void DisplayFields(mdTypeDef inTypeDef, COR_FIELD_OFFSET *rFieldOffset, ULONG cFieldOffset);

    void DisplaySignature(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, const char *preFix);
    HRESULT GetOneElementType(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, ULONG *pcb);

    void DisplayMemberRefs(mdToken tkParent, const char *preFix);
    void DisplayMemberRefInfo(mdMemberRef inMemRef, const char *preFix);

    void DisplayMethodImpls(mdTypeDef inTypeDef);

    void DisplayParams(mdMethodDef inMthDef);
    void DisplayParamInfo(mdParamDef inParam);

    void DisplayGenericParams(mdToken tk, const char *prefix);
    void DisplayGenericParamInfo(mdGenericParam tkparam, const char *prefix);

    void DisplayPropertyInfo(mdProperty inProp);
    void DisplayProperties(mdTypeDef inTypeDef);

    void DisplayEventInfo(mdEvent inEvent);
    void DisplayEvents(mdTypeDef inTypeDef);

    void DisplayPermissions(mdToken tk, const char *preFix);
    void DisplayPermissionInfo(mdPermission inPermission, const char *preFix);

    void DisplayFieldMarshal(mdToken inToken);

    void DisplayPinvokeInfo(mdToken inToken);

    void DisplayAssembly();

    void DisplayAssemblyInfo();

    void DisplayAssemblyRefs();
    void DisplayAssemblyRefInfo(mdAssemblyRef inAssemblyRef);

    void DisplayFiles();
    void DisplayFileInfo(mdFile inFile);

    void DisplayExportedTypes();
    void DisplayExportedTypeInfo(mdExportedType inExportedType);

    void DisplayManifestResources();
    void DisplayManifestResourceInfo(mdManifestResource inManifestResource);

    void DisplayASSEMBLYMETADATA(ASSEMBLYMETADATA *pMetaData);

    void DisplayUserStrings();

    void DisplayUnsatInfo();

    void DisplayRaw();
    void DumpRawHeaps();
    void DumpRaw(int iDump=1, bool bStats=false);
    void DumpRawCSV();
    void DumpRawCol(ULONG ixTbl, ULONG ixCol, ULONG rid, bool bStats);
    ULONG DumpRawColStats(ULONG ixTbl, ULONG ixCol, ULONG cRows);
    const char *DumpRawNameOfType(ULONG ulType);

    static void Error(const char *szError, HRESULT hr = S_OK);
private:
    void Init(strPassBackFn inPBFn, DUMP_FILTER DumpFilter); // Common initialization code.

    int DumpHex(const char *szPrefix, const void *pvData, ULONG cbData, int bText=true, ULONG nLine=16);

    int Write(_In_z_ const char *str);
    int WriteLine(_In_z_ const char *str);

    int VWrite(_In_z_ const char *str, ...);
    int VWriteLine(_In_z_ const char *str, ...);
    int VWriteMarker(_In_z_ const char *str, va_list marker);

    void InitSigBuffer();
    HRESULT AddToSigBuffer(_In_z_ const char *string);

    IMetaDataImport2 *m_pRegImport;
    IMetaDataImport2 *m_pImport;
    IMetaDataAssemblyImport *m_pAssemblyImport;
    strPassBackFn m_pbFn;
    IMetaDataTables *m_pTables;
    IMetaDataTables2 *m_pTables2;

    CQuickBytes m_output;
    DUMP_FILTER m_DumpFilter;

    // temporary buffer for TypeDef or TypeRef name. Consume immediately
    // because other functions may overwrite it.
    WCHAR           m_szTempBuf[STRING_BUFFER_LEN];

    // temporary buffer for formatted string. Consume immediately before any function calls.
    char            m_tempFormatBuffer[STRING_BUFFER_LEN];

    // Signature buffer.
    CQuickBytes     m_sigBuf;
};

#endif
