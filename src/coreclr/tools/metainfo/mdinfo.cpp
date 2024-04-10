// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <windows.h>
#include <objbase.h>
#include <crtdbg.h>
#include <assert.h>
#include <algorithm>

#include <corpriv.h>
#include <cor.h>
#include "assert.h"
#include "corerror.h"
#include <winwrap.h>
#include <prettyprintsig.h>

#include <cahlpr.h>
#include <limits.h>

#include "mdinfo.h"

#define ENUM_BUFFER_SIZE 10
#define TAB_SIZE 8

#define ISFLAG(p,x) if (Is##p##x(flags)) strcat_s(szTempBuf,STRING_BUFFER_LEN, "["#x "] ");

extern HRESULT  _FillVariant(
    BYTE        bCPlusTypeFlag,
    void const  *pValue,
    ULONG       cbValue,
    VARIANT     *pvar);

// Validator declarations.
extern DWORD g_ValModuleType;

// Tables for mapping element type to text
const char *g_szMapElementType[] =
{
    "End",          // 0x0
    "Void",         // 0x1
    "Boolean",
    "Char",
    "I1",
    "UI1",
    "I2",           // 0x6
    "UI2",
    "I4",
    "UI4",
    "I8",
    "UI8",
    "R4",
    "R8",
    "String",
    "Ptr",          // 0xf
    "ByRef",        // 0x10
    "ValueClass",
    "Class",
    "Var",
    "MDArray",      // 0x14
    "GenericInst",
    "TypedByRef",
    "VALUEARRAY",
    "I",
    "U",
    "R",            // 0x1a
    "FNPTR",
    "Object",
    "SZArray",
    "MVar",
    "CMOD_REQD",
    "CMOD_OPT",
    "INTERNAL",
};

const char *g_szMapUndecorateType[] =
{
    "",                 // 0x0
    "void",
    "boolean",
    "Char",
    "byte",
    "unsigned byte",
    "short",
    "unsigned short",
    "int",
    "unsigned int",
    "long",
    "unsigned long",
    "float",
    "double",
    "String",
    "*",                // 0xf
    "ByRef",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "",
    "Function Pointer",
    "Object",
    "",
    "",
    "CMOD_REQD",
    "CMOD_OPT",
    "INTERNAL",
};

// Provide enough entries for IMAGE_CEE_CS_CALLCONV_MASK (defined in CorHdr.h)
const char *g_strCalling[] =
{
    "[DEFAULT]",
    "[C]",
    "[STDCALL]",
    "[THISCALL]",
    "[FASTCALL]",
    "[VARARG]",
    "[FIELD]",
    "[LOCALSIG]",
    "[PROPERTY]",
    "[UNMANAGED]",
    "[GENERICINST]",
    "[NATIVEVARARG]",
    "[INVALID]",
    "[INVALID]",
    "[INVALID]",
    "[INVALID]"
};

const char *g_szNativeType[] =
{
    "NATIVE_TYPE_END(DEPRECATED!)",  //         = 0x0,    //DEPRECATED
    "NATIVE_TYPE_VOID(DEPRECATED!)",  //        = 0x1,    //DEPRECATED
    "NATIVE_TYPE_BOOLEAN",  //     = 0x2,    // (4 byte boolean value: TRUE = non-zero, FALSE = 0)
    "NATIVE_TYPE_I1",  //          = 0x3,
    "NATIVE_TYPE_U1",  //          = 0x4,
    "NATIVE_TYPE_I2",  //          = 0x5,
    "NATIVE_TYPE_U2",  //          = 0x6,
    "NATIVE_TYPE_I4",  //          = 0x7,
    "NATIVE_TYPE_U4",  //          = 0x8,
    "NATIVE_TYPE_I8",  //          = 0x9,
    "NATIVE_TYPE_U8",  //          = 0xa,
    "NATIVE_TYPE_R4",  //          = 0xb,
    "NATIVE_TYPE_R8",  //          = 0xc,
    "NATIVE_TYPE_SYSCHAR(DEPRECATED!)",  //     = 0xd,    //DEPRECATED
    "NATIVE_TYPE_VARIANT(DEPRECATED!)",  //     = 0xe,    //DEPRECATED
    "NATIVE_TYPE_CURRENCY",               //    = 0xf,
    "NATIVE_TYPE_PTR(DEPRECATED!)",  //         = 0x10,   //DEPRECATED

    "NATIVE_TYPE_DECIMAL(DEPRECATED!)",  //     = 0x11,   //DEPRECATED
    "NATIVE_TYPE_DATE(DEPRECATED!)",  //        = 0x12,   //DEPRECATED
    "NATIVE_TYPE_BSTR",  //        = 0x13,
    "NATIVE_TYPE_LPSTR",  //       = 0x14,
    "NATIVE_TYPE_LPWSTR",  //      = 0x15,
    "NATIVE_TYPE_LPTSTR",  //      = 0x16,
    "NATIVE_TYPE_FIXEDSYSSTRING",  //  = 0x17,
    "NATIVE_TYPE_OBJECTREF(DEPRECATED!)",  //   = 0x18,   //DEPRECATED
    "NATIVE_TYPE_IUNKNOWN",  //    = 0x19,
    "NATIVE_TYPE_IDISPATCH",  //   = 0x1a,
    "NATIVE_TYPE_STRUCT",  //      = 0x1b,
    "NATIVE_TYPE_INTF",  //        = 0x1c,
    "NATIVE_TYPE_SAFEARRAY",  //   = 0x1d,
    "NATIVE_TYPE_FIXEDARRAY",  //  = 0x1e,
    "NATIVE_TYPE_INT",  //         = 0x1f,
    "NATIVE_TYPE_UINT",  //        = 0x20,

    "NATIVE_TYPE_NESTEDSTRUCT(DEPRECATED!)",  //  = 0x21, //DEPRECATED (use "NATIVE_TYPE_STRUCT)

    "NATIVE_TYPE_BYVALSTR",  //    = 0x22,

    "NATIVE_TYPE_ANSIBSTR",  //    = 0x23,

    "NATIVE_TYPE_TBSTR",  //       = 0x24, // select BSTR or ANSIBSTR depending on platform


    "NATIVE_TYPE_VARIANTBOOL",  // = 0x25, // (2-byte boolean value: TRUE = -1, FALSE = 0)
    "NATIVE_TYPE_FUNC",  //        = 0x26,
    "NATIVE_TYPE_LPVOID",  //      = 0x27, // blind pointer (no deep marshaling)

    "NATIVE_TYPE_ASANY",  //       = 0x28,
    "<UNDEFINED NATIVE TYPE 0x29>",
    "NATIVE_TYPE_ARRAY",  //       = 0x2a,
    "NATIVE_TYPE_LPSTRUCT",  //    = 0x2b,
    "NATIVE_TYPE_CUSTOMMARSHALER", //           = 0x2c, // Custom marshaler.
    "NATIVE_TYPE_ERROR", //        = 0x2d, // VT_HRESULT when exporting to a typelib.
};

// Maximum value needed to convert a UTF16 codepoint to a UTF8 codepoint.
#define MAX_UTF8_CVT 3

static const char* ConvertToUtf8(LPCWSTR name, _Out_writes_(bufLen) char* buffer, ULONG bufLen)
{
    int res = WszWideCharToMultiByte(CP_UTF8, 0, name, -1, buffer, bufLen, NULL, NULL);
    if (res == 0)
        buffer[bufLen] = '\0';
    return buffer;
}

size_t g_cbCoffNames = 0;

mdMethodDef g_tkEntryPoint = 0; // integration with ILDASM



// helper to init signature buffer
void MDInfo::InitSigBuffer()
{
    strcpy_s((LPSTR)m_sigBuf.Ptr(), 1, "");
} // void MDInfo::InitSigBuffer()

// helper to append a string into the signature buffer. If size of signature buffer is not big enough,
// we will grow it.
HRESULT MDInfo::AddToSigBuffer(_In_z_ const char *string)
{
    HRESULT     hr;
    size_t LL = strlen((LPSTR)m_sigBuf.Ptr()) + strlen(string) + 1;
    IfFailRet( m_sigBuf.ReSizeNoThrow(LL) );
    strcat_s((LPSTR)m_sigBuf.Ptr(), LL, string);
    return NOERROR;
} // HRESULT MDInfo::AddToSigBuffer()

MDInfo::MDInfo(IMetaDataImport2 *pImport, IMetaDataAssemblyImport *pAssemblyImport, LPCWSTR szScope, strPassBackFn inPBFn, ULONG DumpFilter)
{   // This constructor is specific to ILDASM/MetaInfo integration

    _ASSERTE(pImport != NULL);
    _ASSERTE(ARRAY_SIZE(g_szMapElementType) == ARRAY_SIZE(g_szMapUndecorateType));
    _ASSERTE(ARRAY_SIZE(g_szMapElementType) == ELEMENT_TYPE_MAX);

    Init(inPBFn, (DUMP_FILTER)DumpFilter);

    m_pImport = pImport;
    m_pImport->AddRef();
    if ((m_pAssemblyImport = pAssemblyImport))
        m_pAssemblyImport->AddRef();
    else
    {
        HRESULT hr = m_pImport->QueryInterface(IID_IMetaDataAssemblyImport, (void**) &m_pAssemblyImport);
        if (FAILED(hr))
            Error("QueryInterface failed for IID_IMetaDataAssemblyImport.", hr);
    }

} // MDInfo::MDInfo()

MDInfo::MDInfo(IMetaDataDispenserEx *pDispenser, LPCWSTR szScope, strPassBackFn inPBFn, ULONG DumpFilter)
{
    HRESULT     hr = S_OK;
    VARIANT     value;

    _ASSERTE(pDispenser != NULL && inPBFn != NULL);
    _ASSERTE(ARRAY_SIZE(g_szMapElementType) == ARRAY_SIZE(g_szMapUndecorateType));
    _ASSERTE(ARRAY_SIZE(g_szMapElementType) == ELEMENT_TYPE_MAX);

    Init(inPBFn, (DUMP_FILTER)DumpFilter);

    // Attempt to open scope on given file
    V_VT(&value) = VT_UI4;
    V_UI4(&value) = MDImportOptionAll;
    if (FAILED(hr = pDispenser->SetOption(MetaDataImportOption, &value)))
            Error("SetOption failed.", hr);

    hr = pDispenser->OpenScope(szScope, ofNoTransform, IID_IMetaDataImport2, (IUnknown**)&m_pImport);
    if (hr == CLDB_E_BADUPDATEMODE)
    {
        V_VT(&value) = VT_UI4;
        V_UI4(&value) = MDUpdateIncremental;
        if (FAILED(hr = pDispenser->SetOption(MetaDataSetUpdate, &value)))
            Error("SetOption failed.", hr);
        hr = pDispenser->OpenScope(szScope, ofNoTransform, IID_IMetaDataImport2, (IUnknown**)&m_pImport);
    }
    if (FAILED(hr))
        Error("OpenScope failed", hr);

    // Query for the IMetaDataAssemblyImport interface.
    hr = m_pImport->QueryInterface(IID_IMetaDataAssemblyImport, (void**) &m_pAssemblyImport);
    if (FAILED(hr))
        Error("QueryInterface failed for IID_IMetaDataAssemblyImport.", hr);

} // MDInfo::MDInfo()


MDInfo::MDInfo(IMetaDataDispenserEx *pDispenser, PBYTE pbMetaData, DWORD dwSize, strPassBackFn inPBFn, ULONG DumpFilter)
{
    _ASSERTE(pDispenser != NULL && inPBFn != NULL);
    _ASSERTE(ARRAY_SIZE(g_szMapElementType) == ARRAY_SIZE(g_szMapUndecorateType));
    _ASSERTE(ARRAY_SIZE(g_szMapElementType) == ELEMENT_TYPE_MAX);

    Init(inPBFn, (DUMP_FILTER)DumpFilter);

    // Attempt to open scope on manifest. It's valid for this to fail, because
    // the blob we open may just be the assembly resources (the space is
    // overloaded until we remove LM -a assemblies, at which point this
    // constructor should probably be removed too).
    HRESULT hr;
    VARIANT     value;
    V_VT(&value) = VT_UI4;
    V_UI4(&value) = MDImportOptionAll;
    if (FAILED(hr = pDispenser->SetOption(MetaDataImportOption, &value)))
            Error("SetOption failed.", hr);
    if (SUCCEEDED(hr = pDispenser->OpenScopeOnMemory(pbMetaData, dwSize, ofNoTransform,
                            IID_IMetaDataImport2, (IUnknown**)&m_pImport)))
    {
        // Query for the IMetaDataAssemblyImport interface.
        hr = m_pImport->QueryInterface(IID_IMetaDataAssemblyImport, (void**) &m_pAssemblyImport);
        if (FAILED(hr))
            Error("QueryInterface failed for IID_IMetaDataAssemblyImport.", hr);
    }

} // MDInfo::MDInfo()

void MDInfo::Init(
    strPassBackFn inPBFn,               // Callback to write text.
    DUMP_FILTER DumpFilter)             // Flags to control the dump.
{
    m_pbFn = inPBFn;
    m_DumpFilter = DumpFilter;
    m_pTables = NULL;
    m_pTables2 = NULL;
    m_pImport = NULL;
    m_pAssemblyImport = NULL;
} // void MDInfo::Init()

// Destructor
MDInfo::~MDInfo()
{
    if (m_pImport)
        m_pImport->Release();
    if (m_pAssemblyImport)
        m_pAssemblyImport->Release();
    if (m_pTables)
        m_pTables->Release();
    if (m_pTables2)
        m_pTables2->Release();
} // MDInfo::~MDInfo()

//=====================================================================================================================
// DisplayMD() function
//
// Displays the meta data content of a file

void MDInfo::DisplayMD()
{
    if ((m_DumpFilter & dumpAssem) && m_pAssemblyImport)
        DisplayAssemblyInfo();
    WriteLine("===========================================================");
    // Metadata itself: Raw or normal view
    if (m_DumpFilter & (dumpSchema | dumpHeader | dumpCSV | dumpRaw | dumpStats | dumpRawHeaps))
        DisplayRaw();
    else
    {
        DisplayVersionInfo();
        DisplayScopeInfo();
        WriteLine("===========================================================");
        DisplayGlobalFunctions();
        DisplayGlobalFields();
        DisplayGlobalMemberRefs();
        DisplayTypeDefs();
        DisplayTypeRefs();
        DisplayTypeSpecs();
        DisplayMethodSpecs();
        DisplayModuleRefs();
        DisplaySignatures();
        DisplayAssembly();
        DisplayUserStrings();

        // WriteLine("============================================================");
        // WriteLine("Unresolved MemberRefs");
        // DisplayMemberRefs(0x00000001, "\t");

        VWrite("\n\nCoff symbol name overhead:  %zu\n", g_cbCoffNames);
    }
    WriteLine("===========================================================");
    if (m_DumpFilter & dumpUnsat)
        DisplayUnsatInfo();
    WriteLine("===========================================================");
} // MDVEHandlerClass()

int MDInfo::WriteLine(_In_z_ const char *str)
{
    ULONG32 count = (ULONG32) strlen(str);

    m_pbFn(str);
    m_pbFn("\n");
    return count;
} // int MDInfo::WriteLine()

int MDInfo::Write(_In_z_ const char *str)
{
    ULONG32 count = (ULONG32) strlen(str);

    m_pbFn(str);
    return count;
} // int MDInfo::Write()

int MDInfo::VWriteLine(_In_z_ const char *str, ...)
{
    va_list marker;
    int     count;

    va_start(marker, str);
    count = VWriteMarker(str, marker);
    m_pbFn("\n");
    va_end(marker);
    return count;
} // int MDInfo::VWriteLine()

int MDInfo::VWrite(_In_z_ const char *str, ...)
{
    va_list marker;
    int     count;

    va_start(marker, str);
    count = VWriteMarker(str, marker);
    va_end(marker);
    return count;
} // int MDInfo::VWrite()

int MDInfo::VWriteMarker(_In_z_ const char *str, va_list marker)
{
    HRESULT hr;
    int count = -1;
    // Used to allocate 1K, then if not enough, 2K, then 4K.
    // Faster to allocate 32K right away and be done with it,
    // we're not running on Commodore 64
    if (FAILED(hr = m_output.ReSizeNoThrow(STRING_BUFFER_LEN * 8)))
        Error("ReSize failed.", hr);
    else
    {
        count = vsprintf_s((char *)m_output.Ptr(), STRING_BUFFER_LEN * 8, str, marker);
        m_pbFn((char *)m_output.Ptr());
    }
    return count;
} // int MDInfo::VWriteToBuffer()

// Error() function -- prints an error and returns
void MDInfo::Error(const char* szError, HRESULT hr)
{
    printf("\n%s\n",szError);
    if (hr != S_OK)
    {
        printf("Failed return code: 0x%08x\n", hr);

#ifdef FEATURE_COMINTEROP
        IErrorInfo  *pIErr = NULL;          // Error interface.
        BSTR        bstrDesc = NULL;        // Description text.

        // Try to get an error info object and display the message.
        if (GetErrorInfo(0, &pIErr) == S_OK &&
            pIErr->GetDescription(&bstrDesc) == S_OK)
        {
            MAKE_UTF8PTR_FROMWIDE(bstrDescUtf8, bstrDesc);
            printf("%s ", bstrDescUtf8);
            SysFreeString(bstrDesc);
        }

        // Free the error interface.
        if (pIErr)
            pIErr->Release();
#endif
    }
    exit(hr);
} // void MDInfo::Error()

// Print out the optional version info included in the MetaData.

void MDInfo::DisplayVersionInfo()
{
    if (!(m_DumpFilter & MDInfo::dumpNoLogo))
    {
        LPCUTF8 pVersionStr;
        HRESULT hr = S_OK;

        if (m_pTables == 0)
        {
            if (m_pImport)
                hr = m_pImport->QueryInterface(IID_IMetaDataTables, (void**)&m_pTables);
            else if (m_pAssemblyImport)
                hr = m_pAssemblyImport->QueryInterface(IID_IMetaDataTables, (void**)&m_pTables);
            else
                return;
            if (FAILED(hr))
                Error("QueryInterface failed for IID_IMetaDataTables.", hr);
        }

        hr = m_pTables->GetString(1, &pVersionStr);
        if (FAILED(hr))
            Error("GetString() failed.", hr);
        if (strstr(pVersionStr, "Version of runtime against which the binary is built : ")
                    == pVersionStr)
        {
            WriteLine(const_cast<char *>(pVersionStr));
        }
    }
} // void MDInfo::DisplayVersionInfo()

// Prints out information about the scope

void MDInfo::DisplayScopeInfo()
{
    HRESULT hr;
    mdModule mdm;
    GUID mvid;
    WCHAR scopeName[STRING_BUFFER_LEN];
    char scopeNameUtf8[ARRAY_SIZE(scopeName) * MAX_UTF8_CVT];
    char guidString[STRING_BUFFER_LEN];

    hr = m_pImport->GetScopeProps( scopeName, STRING_BUFFER_LEN, 0, &mvid);
    if (FAILED(hr)) Error("GetScopeProps failed.", hr);

    VWriteLine("ScopeName : %s",ConvertToUtf8(scopeName, scopeNameUtf8, ARRAY_SIZE(scopeNameUtf8)));

    if (!(m_DumpFilter & MDInfo::dumpNoLogo))
        VWriteLine("MVID      : %s",GUIDAsString(mvid, guidString, STRING_BUFFER_LEN));

    hr = m_pImport->GetModuleFromScope(&mdm);
    if (FAILED(hr)) Error("GetModuleFromScope failed.", hr);
    DisplayPermissions(mdm, "");
    DisplayCustomAttributes(mdm, "\t");
} // void MDInfo::DisplayScopeInfo()

void MDInfo::DisplayRaw()
{
    int         iDump;                  // Level of info to dump.

    if (m_pTables == 0)
        m_pImport->QueryInterface(IID_IMetaDataTables, (void**)&m_pTables);
    if (m_pTables == 0)
        Error("Can't get table info.");
    if (m_pTables2 == 0)
        m_pImport->QueryInterface(IID_IMetaDataTables2, (void**)&m_pTables2);

    if (m_DumpFilter & dumpCSV)
        DumpRawCSV();
    if (m_DumpFilter & (dumpSchema | dumpHeader | dumpRaw | dumpStats))
    {
        if (m_DumpFilter & dumpRaw)
            iDump = 3;
        else
        if (m_DumpFilter & dumpSchema)
            iDump = 2;
        else
            iDump = 1;

        DumpRaw(iDump, (m_DumpFilter & dumpStats) != 0);
    }
    if (m_DumpFilter & dumpRawHeaps)
        DumpRawHeaps();
} // void MDInfo::DisplayRaw()

// return the name of the type of token passed in

const char *MDInfo::TokenTypeName(mdToken inToken)
{
    switch(TypeFromToken(inToken))
    {
    case mdtTypeDef:        return "TypeDef";
    case mdtInterfaceImpl:  return "InterfaceImpl";
    case mdtMethodDef:      return "MethodDef";
    case mdtFieldDef:       return "FieldDef";
    case mdtTypeRef:        return "TypeRef";
    case mdtMemberRef:      return "MemberRef";
    case mdtCustomAttribute:return "CustomAttribute";
    case mdtParamDef:       return "ParamDef";
    case mdtProperty:       return "Property";
    case mdtEvent:          return "Event";
    case mdtTypeSpec:       return "TypeSpec";
    default:                return "[UnknownTokenType]";
    }
} // char *MDInfo::TokenTypeName()

// Prints out name of the given memberref
//

LPCSTR MDInfo::MemberRefName(mdMemberRef inMemRef, _Out_writes_(bufLen) LPSTR buffer, ULONG bufLen)
{
    HRESULT hr;

    WCHAR tempBuf[STRING_BUFFER_LEN];
    hr = m_pImport->GetMemberRefProps( inMemRef, NULL, tempBuf, ARRAY_SIZE(tempBuf),
                                    NULL, NULL, NULL);
    if (FAILED(hr)) Error("GetMemberRefProps failed.", hr);

    return ConvertToUtf8(tempBuf, buffer, bufLen);
} // LPCSTR MDInfo::MemberRefName()

// Prints out information about the given memberref
//

void MDInfo::DisplayMemberRefInfo(mdMemberRef inMemRef, const char *preFix)
{
    HRESULT hr;
    WCHAR memRefName[STRING_BUFFER_LEN];
    char memRefNameUtf8[ARRAY_SIZE(memRefName) * MAX_UTF8_CVT];
    ULONG nameLen;
    mdToken token;
    PCCOR_SIGNATURE pbSigBlob;
    ULONG ulSigBlob;
    char newPreFix[STRING_BUFFER_LEN];


    hr = m_pImport->GetMemberRefProps( inMemRef, &token, memRefName, STRING_BUFFER_LEN,
                                    &nameLen, &pbSigBlob, &ulSigBlob);
    if (FAILED(hr)) Error("GetMemberRefProps failed.", hr);

    VWriteLine("%s\t\tMember: (%8.8x) %s: ", preFix, inMemRef, ConvertToUtf8(memRefName, memRefNameUtf8, ARRAY_SIZE(memRefNameUtf8)));

    if (ulSigBlob)
        DisplaySignature(pbSigBlob, ulSigBlob, preFix);
    else
        VWriteLine("%s\t\tERROR: no valid signature ", preFix);

    sprintf_s (newPreFix, STRING_BUFFER_LEN, "\t\t%s", preFix);
    DisplayCustomAttributes(inMemRef, newPreFix);
} // void MDInfo::DisplayMemberRefInfo()

// Prints out information about all memberrefs of the given typeref
//

void MDInfo::DisplayMemberRefs(mdToken tkParent, const char *preFix)
{
    HCORENUM memRefEnum = NULL;
    HRESULT hr;
    mdMemberRef memRefs[ENUM_BUFFER_SIZE];
    ULONG count, totalCount = 1;


    while (SUCCEEDED(hr = m_pImport->EnumMemberRefs( &memRefEnum, tkParent,
                             memRefs, ARRAY_SIZE(memRefs), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("%s\tMemberRef #%d (%08x)", preFix, totalCount, memRefs[i]);
            VWriteLine("%s\t-------------------------------------------------------", preFix);
            DisplayMemberRefInfo(memRefs[i], preFix);
        }
    }
    m_pImport->CloseEnum( memRefEnum);
} // void MDInfo::DisplayMemberRefs()

// Prints out information about all resources in the com object
//

// Iterates through each typeref and prints out the information of each
//

void MDInfo::DisplayTypeRefs()
{
    HCORENUM typeRefEnum = NULL;
    mdTypeRef typeRefs[ENUM_BUFFER_SIZE];
    ULONG count, totalCount=1;
    HRESULT hr;

    while (SUCCEEDED(hr = m_pImport->EnumTypeRefs( &typeRefEnum,
                             typeRefs, ARRAY_SIZE(typeRefs), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("TypeRef #%d (%08x)", totalCount, typeRefs[i]);
            WriteLine("-------------------------------------------------------");
            DisplayTypeRefInfo(typeRefs[i]);
            DisplayMemberRefs(typeRefs[i], "");
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( typeRefEnum);
} // void MDInfo::DisplayTypeRefs()

void MDInfo::DisplayTypeSpecs()
{
    HCORENUM typespecEnum = NULL;
    mdTypeSpec typespecs[ENUM_BUFFER_SIZE];
    ULONG count, totalCount=1;
    HRESULT hr;

    while (SUCCEEDED(hr = m_pImport->EnumTypeSpecs( &typespecEnum,
                             typespecs, ARRAY_SIZE(typespecs), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("TypeSpec #%d (%08x)", totalCount, typespecs[i]);
            WriteLine("-------------------------------------------------------");
            DisplayTypeSpecInfo(typespecs[i], "");
            DisplayMemberRefs(typespecs[i], "");
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( typespecEnum);
} // void MDInfo::DisplayTypeSpecs()

void MDInfo::DisplayMethodSpecs()
{
    HCORENUM MethodSpecEnum = NULL;
    mdMethodSpec MethodSpecs[ENUM_BUFFER_SIZE];
    ULONG count, totalCount=1;
/////    HRESULT hr;


/////  HACK until I implement EnumMethodSpecs!
///// while (SUCCEEDED(hr = m_pImport->EnumMethodSpecs( &MethodSpecEnum,
/////                          MethodSpecs, ARRAY_SIZE(MethodSpecs), &count)) &&
/////         count > 0)
    for (ULONG rid=1; m_pImport->IsValidToken(TokenFromRid(rid, mdtMethodSpec)); ++rid)
    {
// More hackery
count = 1;
MethodSpecs[0] = TokenFromRid(rid, mdtMethodSpec);
// More hackery
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("MethodSpec #%d (%08x)", totalCount, MethodSpecs[i]);
            DisplayMethodSpecInfo(MethodSpecs[i], "");
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( MethodSpecEnum);
} // void MDInfo::DisplayMethodSpecs()



// Called to display the information about all typedefs in the object.
//

void MDInfo::DisplayTypeDefs()
{
    HCORENUM typeDefEnum = NULL;
    mdTypeDef typeDefs[ENUM_BUFFER_SIZE];
    ULONG count, totalCount = 1;
    HRESULT hr;

    while (SUCCEEDED(hr = m_pImport->EnumTypeDefs( &typeDefEnum,
                             typeDefs, ARRAY_SIZE(typeDefs), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("TypeDef #%d (%08x)", totalCount, typeDefs[i]);
            WriteLine("-------------------------------------------------------");
            DisplayTypeDefInfo(typeDefs[i]);
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( typeDefEnum);
} // void MDInfo::DisplayTypeDefs()

// Called to display the information about all modulerefs in the object.
//

void MDInfo::DisplayModuleRefs()
{
    HCORENUM moduleRefEnum = NULL;
    mdModuleRef moduleRefs[ENUM_BUFFER_SIZE];
    ULONG count, totalCount = 1;
    HRESULT hr;

    while (SUCCEEDED(hr = m_pImport->EnumModuleRefs( &moduleRefEnum,
                             moduleRefs, ARRAY_SIZE(moduleRefs), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("ModuleRef #%d (%08x)", totalCount, moduleRefs[i]);
            WriteLine("-------------------------------------------------------");
            DisplayModuleRefInfo(moduleRefs[i]);
            DisplayMemberRefs(moduleRefs[i], "");
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( moduleRefEnum);
} // void MDInfo::DisplayModuleRefs()

// Prints out information about the given moduleref
//

void MDInfo::DisplayModuleRefInfo(mdModuleRef inModuleRef)
{
    HRESULT hr;
    WCHAR moduleRefName[STRING_BUFFER_LEN];
    char moduleRefNameUtf8[ARRAY_SIZE(moduleRefName) * MAX_UTF8_CVT];
    ULONG nameLen;


    hr = m_pImport->GetModuleRefProps( inModuleRef, moduleRefName, STRING_BUFFER_LEN,
                                    &nameLen);
    if (FAILED(hr)) Error("GetModuleRefProps failed.", hr);

    VWriteLine("\t\tModuleRef: (%8.8x) %s: ", inModuleRef, ConvertToUtf8(moduleRefName, moduleRefNameUtf8, ARRAY_SIZE(moduleRefNameUtf8)));
    DisplayCustomAttributes(inModuleRef, "\t\t");
} // void MDInfo::DisplayModuleRefInfo()


// Called to display the information about all signatures in the object.
//

void MDInfo::DisplaySignatures()
{
    HCORENUM signatureEnum = NULL;
    mdSignature signatures[ENUM_BUFFER_SIZE];
    ULONG count, totalCount = 1;
    HRESULT hr;

    while (SUCCEEDED(hr = m_pImport->EnumSignatures( &signatureEnum,
                             signatures, ARRAY_SIZE(signatures), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("Signature #%d (%#08x)", totalCount, signatures[i]);
            WriteLine("-------------------------------------------------------");
            DisplaySignatureInfo(signatures[i]);
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( signatureEnum);
} // void MDInfo::DisplaySignatures()


// Prints out information about the given signature
//

void MDInfo::DisplaySignatureInfo(mdSignature inSignature)
{
    HRESULT hr;
    PCCOR_SIGNATURE pbSigBlob;
    ULONG   ulSigBlob;


    hr = m_pImport->GetSigFromToken( inSignature, &pbSigBlob, &ulSigBlob );
    if (FAILED(hr)) Error("GetSigFromToken failed.", hr);
    if(ulSigBlob)
        DisplaySignature(pbSigBlob, ulSigBlob, "");
    else
        VWriteLine("\t\tERROR: no valid signature ");
} // void MDInfo::DisplaySignatureInfo()


// returns the passed-in buffer which is filled with the name of the given
// member in wide characters
//

LPCSTR MDInfo::MemberName(mdToken inToken, _Out_writes_(bufLen) LPSTR buffer, ULONG bufLen)
{
    HRESULT hr;

    WCHAR tempBuf[STRING_BUFFER_LEN];
    hr = m_pImport->GetMemberProps( inToken, NULL, tempBuf, ARRAY_SIZE(tempBuf),
                            NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
    if (FAILED(hr)) Error("GetMemberProps failed.", hr);

    return ConvertToUtf8(tempBuf, buffer, bufLen);
} // LPCSTR MDInfo::MemberName()


// displays information for the given method
//

void MDInfo::DisplayMethodInfo(mdMethodDef inMethod, DWORD *pflags)
{
    HRESULT hr;
    mdTypeDef memTypeDef;
    WCHAR memberName[STRING_BUFFER_LEN];
    char memberNameUtf8[ARRAY_SIZE(memberName) * MAX_UTF8_CVT];
    ULONG nameLen;
    DWORD flags;
    PCCOR_SIGNATURE pbSigBlob;
    ULONG ulSigBlob;
    ULONG ulCodeRVA;
    ULONG ulImplFlags;


    hr = m_pImport->GetMethodProps( inMethod, &memTypeDef, memberName, STRING_BUFFER_LEN,
                            &nameLen, &flags, &pbSigBlob, &ulSigBlob, &ulCodeRVA, &ulImplFlags);
    if (FAILED(hr)) Error("GetMethodProps failed.", hr);
    if (pflags)
        *pflags = flags;

    VWriteLine("\t\tMethodName: %s (%8.8X)", ConvertToUtf8(memberName, memberNameUtf8, ARRAY_SIZE(memberNameUtf8)), inMethod);

    char szTempBuf[STRING_BUFFER_LEN];

    szTempBuf[0] = 0;
    ISFLAG(Md, Public);
    ISFLAG(Md, Private);
    ISFLAG(Md, Family);
    ISFLAG(Md, Assem);
    ISFLAG(Md, FamANDAssem);
    ISFLAG(Md, FamORAssem);
    ISFLAG(Md, PrivateScope);
    ISFLAG(Md, Static);
    ISFLAG(Md, Final);
    ISFLAG(Md, Virtual);
    ISFLAG(Md, HideBySig);
    ISFLAG(Md, ReuseSlot);
    ISFLAG(Md, NewSlot);
    ISFLAG(Md, Abstract);
    ISFLAG(Md, SpecialName);
    ISFLAG(Md, RTSpecialName);
    ISFLAG(Md, PinvokeImpl);
    ISFLAG(Md, UnmanagedExport);
    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    bool result = (((flags) & mdRTSpecialName) && !u16_strcmp((memberName), W(".ctor")));
    if (result) strcat_s(szTempBuf, STRING_BUFFER_LEN, "[.ctor] ");
    result = (((flags) & mdRTSpecialName) && !u16_strcmp((memberName), W(".cctor")));
    if (result) strcat_s(szTempBuf,STRING_BUFFER_LEN, "[.cctor] ");
    // "Reserved" flags
    ISFLAG(Md, HasSecurity);
    ISFLAG(Md, RequireSecObject);

    VWriteLine("\t\tFlags     : %s (%08x)", szTempBuf, flags);
    VWriteLine("\t\tRVA       : 0x%08x", ulCodeRVA);

    flags = ulImplFlags;
    szTempBuf[0] = 0;
    ISFLAG(Mi, Native);
    ISFLAG(Mi, IL);
    ISFLAG(Mi, OPTIL);
    ISFLAG(Mi, Runtime);
    ISFLAG(Mi, Unmanaged);
    ISFLAG(Mi, Managed);
    ISFLAG(Mi, ForwardRef);
    ISFLAG(Mi, PreserveSig);
    ISFLAG(Mi, InternalCall);
    ISFLAG(Mi, Synchronized);
    ISFLAG(Mi, NoInlining);
    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    VWriteLine("\t\tImplFlags : %s (%08x)", szTempBuf, flags);

    if (ulSigBlob)
        DisplaySignature(pbSigBlob, ulSigBlob, "");
    else
        VWriteLine("\t\tERROR: no valid signature ");

    DisplayGenericParams(inMethod, "\t\t");

} // void MDInfo::DisplayMethodInfo()

// displays the member information for the given field
//

void MDInfo::DisplayFieldInfo(mdFieldDef inField, DWORD *pdwFlags)
{
    HRESULT hr;
    mdTypeDef memTypeDef;
    WCHAR memberName[STRING_BUFFER_LEN];
    char memberNameUtf8[ARRAY_SIZE(memberName) * MAX_UTF8_CVT];
    ULONG nameLen;
    DWORD flags;
    PCCOR_SIGNATURE pbSigBlob;
    ULONG ulSigBlob;
    DWORD dwCPlusTypeFlag;
    void const *pValue;
    ULONG cbValue;
#ifdef FEATURE_COMINTEROP
    VARIANT defaultValue;

    ::VariantInit(&defaultValue);
#endif
    hr = m_pImport->GetFieldProps( inField, &memTypeDef, memberName, STRING_BUFFER_LEN,
                            &nameLen, &flags, &pbSigBlob, &ulSigBlob, &dwCPlusTypeFlag,
                            &pValue, &cbValue);
    if (FAILED(hr)) Error("GetFieldProps failed.", hr);

    if (pdwFlags)
        *pdwFlags = flags;

#ifdef FEATURE_COMINTEROP
    _FillVariant((BYTE)dwCPlusTypeFlag, pValue, cbValue, &defaultValue);
#endif

    char szTempBuf[STRING_BUFFER_LEN];

    szTempBuf[0] = 0;
    ISFLAG(Fd, Public);
    ISFLAG(Fd, Private);
    ISFLAG(Fd, Family);
    ISFLAG(Fd, Assembly);
    ISFLAG(Fd, FamANDAssem);
    ISFLAG(Fd, FamORAssem);
    ISFLAG(Fd, PrivateScope);
    ISFLAG(Fd, Static);
    ISFLAG(Fd, InitOnly);
    ISFLAG(Fd, Literal);
    ISFLAG(Fd, NotSerialized);
    ISFLAG(Fd, SpecialName);
    ISFLAG(Fd, RTSpecialName);
    ISFLAG(Fd, PinvokeImpl);
    // "Reserved" flags
    ISFLAG(Fd, HasDefault);
    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    VWriteLine("\t\tField Name: %s (%8.8X)", ConvertToUtf8(memberName, memberNameUtf8, ARRAY_SIZE(memberNameUtf8)), inField);
    VWriteLine("\t\tFlags     : %s (%08x)", szTempBuf, flags);
#ifdef FEATURE_COMINTEROP
    if (IsFdHasDefault(flags))
        VWriteLine("\tDefltValue: (%s) %s", g_szMapElementType[dwCPlusTypeFlag], VariantAsString(&defaultValue, szTempBuf, ARRAY_SIZE(szTempBuf)));
#endif
    if (!ulSigBlob) // Signature size should be non-zero for fields
        VWriteLine("\t\tERROR: no valid signature ");
    else
        DisplaySignature(pbSigBlob, ulSigBlob, "");
#ifdef FEATURE_COMINTEROP
    ::VariantClear(&defaultValue);
#endif
} // void MDInfo::DisplayFieldInfo()

// displays the RVA for the given global field.
void MDInfo::DisplayFieldRVA(mdFieldDef inFieldDef)
{
    HRESULT hr;
    ULONG   ulRVA;

    hr = m_pImport->GetRVA(inFieldDef, &ulRVA, 0);
    if (FAILED(hr) && hr != CLDB_E_RECORD_NOTFOUND) Error("GetRVA failed.", hr);

    VWriteLine("\t\tRVA       : 0x%08x", ulRVA);
} // void MDInfo::DisplayFieldRVA()

// displays information about every global function.
void MDInfo::DisplayGlobalFunctions()
{
    WriteLine("Global functions");
    WriteLine("-------------------------------------------------------");
    DisplayMethods(mdTokenNil);
    WriteLine("");
} // void MDInfo::DisplayGlobalFunctions()

// displays information about every global field.
void MDInfo::DisplayGlobalFields()
{
    WriteLine("Global fields");
    WriteLine("-------------------------------------------------------");
    DisplayFields(mdTokenNil, NULL, 0);
    WriteLine("");
} // void MDInfo::DisplayGlobalFields()

// displays information about every global memberref.
void MDInfo::DisplayGlobalMemberRefs()
{
    WriteLine("Global MemberRefs");
    WriteLine("-------------------------------------------------------");
    DisplayMemberRefs(mdTokenNil, "");
    WriteLine("");
} // void MDInfo::DisplayGlobalMemberRefs()

// displays information about every method in a given typedef
//

void MDInfo::DisplayMethods(mdTypeDef inTypeDef)
{
    HCORENUM methodEnum = NULL;
    mdToken methods[ENUM_BUFFER_SIZE];
    DWORD flags;
    ULONG count, totalCount = 1;
    HRESULT hr;


    while (SUCCEEDED(hr = m_pImport->EnumMethods( &methodEnum, inTypeDef,
                             methods, ARRAY_SIZE(methods), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("\tMethod #%d (%08x) %s", totalCount, methods[i], (methods[i] == g_tkEntryPoint) ? "[ENTRYPOINT]" : "");
            WriteLine("\t-------------------------------------------------------");
            DisplayMethodInfo(methods[i], &flags);
            DisplayParams(methods[i]);
            DisplayCustomAttributes(methods[i], "\t\t");
            DisplayPermissions(methods[i], "\t");
            DisplayMemberRefs(methods[i], "\t");

            // P-invoke data if present.
            if (IsMdPinvokeImpl(flags))
                DisplayPinvokeInfo(methods[i]);

            WriteLine("");
        }
    }
    m_pImport->CloseEnum( methodEnum);
} // void MDInfo::DisplayMethods()


// displays information about every field in a given typedef
//

void MDInfo::DisplayFields(mdTypeDef inTypeDef, COR_FIELD_OFFSET *rFieldOffset, ULONG cFieldOffset)
{
    HCORENUM fieldEnum = NULL;
    mdToken fields[ENUM_BUFFER_SIZE];
    ULONG count, totalCount = 1;
    DWORD flags;
    HRESULT hr;


    while (SUCCEEDED(hr = m_pImport->EnumFields( &fieldEnum, inTypeDef,
                             fields, ARRAY_SIZE(fields), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("\tField #%d (%08x)",totalCount, fields[i]);
            WriteLine("\t-------------------------------------------------------");
            DisplayFieldInfo(fields[i], &flags);
            DisplayCustomAttributes(fields[i], "\t\t");
            DisplayPermissions(fields[i], "\t");
            DisplayFieldMarshal(fields[i]);

            // RVA if its a global field.
            if (inTypeDef == mdTokenNil)
                DisplayFieldRVA(fields[i]);

            // P-invoke data if present.
            if (IsFdPinvokeImpl(flags))
                DisplayPinvokeInfo(fields[i]);

            // Display offset if present.
            if (cFieldOffset)
            {
                bool found = false;
                for (ULONG iLayout = 0; iLayout < cFieldOffset; ++iLayout)
                {
                    if (RidFromToken(rFieldOffset[iLayout].ridOfField) == RidFromToken(fields[i]))
                    {
                        found = true;
                        VWriteLine("\t\tOffset : 0x%08x", rFieldOffset[iLayout].ulOffset);
                        break;
                    }
                }
                _ASSERTE(found);
            }
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( fieldEnum);
} // void MDInfo::DisplayFields()


// displays information about every methodImpl in a given typedef
//

void MDInfo::DisplayMethodImpls(mdTypeDef inTypeDef)
{
    HCORENUM methodImplEnum = NULL;
    mdMethodDef rtkMethodBody[ENUM_BUFFER_SIZE];
    mdMethodDef rtkMethodDecl[ENUM_BUFFER_SIZE];

    ULONG count, totalCount=1;
    HRESULT hr;


    while (SUCCEEDED(hr = m_pImport->EnumMethodImpls( &methodImplEnum, inTypeDef,
                             rtkMethodBody, rtkMethodDecl, ARRAY_SIZE(rtkMethodBody), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("\n\tMethodImpl #%d (%08x)", totalCount, totalCount);
            WriteLine("\t-------------------------------------------------------");
            VWriteLine("\t\tMethod Body Token : 0x%08x", rtkMethodBody[i]);
            VWriteLine("\t\tMethod Declaration Token : 0x%08x", rtkMethodDecl[i]);
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( methodImplEnum);
} // void MDInfo::DisplayMethodImpls()

// displays information about the given parameter
//

void MDInfo::DisplayParamInfo(mdParamDef inParamDef)
{
    mdMethodDef md;
    ULONG num;
    WCHAR paramName[STRING_BUFFER_LEN];
    char paramNameUtf8[ARRAY_SIZE(paramName) * MAX_UTF8_CVT];
    ULONG nameLen;
    DWORD flags;
    VARIANT defValue;
    DWORD dwCPlusFlags;
    void const *pValue;
    ULONG cbValue;

#ifdef FEATURE_COMINTEROP
    ::VariantInit(&defValue);
#endif
    HRESULT hr = m_pImport->GetParamProps( inParamDef, &md, &num, paramName, ARRAY_SIZE(paramName),
                            &nameLen, &flags, &dwCPlusFlags, &pValue, &cbValue);
    if (FAILED(hr)) Error("GetParamProps failed.", hr);

#ifdef FEATURE_COMINTEROP
    _FillVariant((BYTE)dwCPlusFlags, pValue, cbValue, &defValue);
#endif

    char szTempBuf[STRING_BUFFER_LEN];
    szTempBuf[0] = 0;
    ISFLAG(Pd, In);
    ISFLAG(Pd, Out);
    ISFLAG(Pd, Optional);
    // "Reserved" flags.
    ISFLAG(Pd, HasDefault);
    ISFLAG(Pd, HasFieldMarshal);
    if (!*szTempBuf)
        strcpy_s(szTempBuf,STRING_BUFFER_LEN, "[none]");

    VWrite("\t\t\t(%ld) ParamToken : (%08x) Name : %s flags: %s (%08x)",
        num, inParamDef, ConvertToUtf8(paramName, paramNameUtf8, ARRAY_SIZE(paramNameUtf8)), szTempBuf, flags);
#ifdef FEATURE_COMINTEROP
    if (IsPdHasDefault(flags))
        VWriteLine(" Default: (%s) %s", g_szMapElementType[dwCPlusFlags], VariantAsString(&defValue, szTempBuf, ARRAY_SIZE(szTempBuf)));
    else
#endif
        VWriteLine("");
    DisplayCustomAttributes(inParamDef, "\t\t\t");

#ifdef FEATURE_COMINTEROP
    ::VariantClear(&defValue);
#endif
} // void MDInfo::DisplayParamInfo()


// displays all parameters for a given memberdef
//

void MDInfo::DisplayParams(mdMethodDef inMethodDef)
{
    HCORENUM paramEnum = NULL;
    mdParamDef params[ENUM_BUFFER_SIZE];
    ULONG count, paramCount;
    bool first = true;
    HRESULT hr;


    while (SUCCEEDED(hr = m_pImport->EnumParams( &paramEnum, inMethodDef,
                             params, ARRAY_SIZE(params), &count)) &&
            count > 0)
    {
        if (first)
        {
            m_pImport->CountEnum( paramEnum, &paramCount);
            VWriteLine("\t\t%d Parameters", paramCount);
        }
        for (ULONG i = 0; i < count; i++)
        {
            DisplayParamInfo(params[i]);
            DisplayFieldMarshal(params[i]);
        }
        first = false;
    }
    m_pImport->CloseEnum( paramEnum);
} // void MDInfo::DisplayParams()

void MDInfo::DisplayGenericParams(mdToken tk, const char *prefix)
{
    HCORENUM paramEnum = NULL;
    mdParamDef params[ENUM_BUFFER_SIZE];
    ULONG count, paramCount;
    bool first = true;
    HRESULT hr;


    while (SUCCEEDED(hr = m_pImport->EnumGenericParams( &paramEnum, tk,
                             params, ARRAY_SIZE(params), &count)) &&
            count > 0)
    {
        if (first)
        {
            m_pImport->CountEnum( paramEnum, &paramCount);
            VWriteLine("%s%d Generic Parameters", prefix, paramCount);
        }
        for (ULONG i = 0; i < count; i++)
        {
            DisplayGenericParamInfo(params[i], prefix);
        }
        first = false;
    }
    m_pImport->CloseEnum( paramEnum);
}

void MDInfo::DisplayGenericParamInfo(mdGenericParam tkParam, const char *prefix)
{
    ULONG ulSeq;
    WCHAR paramName[STRING_BUFFER_LEN];
    char paramNameUtf8[ARRAY_SIZE(paramName) * MAX_UTF8_CVT];
    ULONG nameLen;
    DWORD flags;
    mdToken tkOwner;
    char newprefix[30];
    HCORENUM constraintEnum = NULL;
    mdParamDef constraints[4];
    ULONG count, constraintCount;
    mdToken constraint;
    mdToken owner;
    bool first = true;

    HRESULT hr = m_pImport->GetGenericParamProps(tkParam, &ulSeq, &flags, &tkOwner, NULL, paramName, ARRAY_SIZE(paramName), &nameLen);
    if (FAILED(hr)) Error("GetGenericParamProps failed.", hr);

    VWriteLine("%s\t(%ld) GenericParamToken : (%08x) Name : %s flags: %08x Owner: %08x",
        prefix, ulSeq, tkParam, ConvertToUtf8(paramName, paramNameUtf8, ARRAY_SIZE(paramNameUtf8)), flags, tkOwner);

    // Any constraints for the GenericParam
    while (SUCCEEDED(hr = m_pImport->EnumGenericParamConstraints(&constraintEnum, tkParam,
                constraints, ARRAY_SIZE(constraints), &count)) &&
           count > 0)
    {
        if (first)
        {
            m_pImport->CountEnum( constraintEnum, &constraintCount);
            VWriteLine("%s\t\t%d Constraint(s)", prefix, constraintCount);
        }
        VWrite("%s\t\t", prefix);
        for (ULONG i=0; i< count; ++i)
        {
            hr = m_pImport->GetGenericParamConstraintProps(constraints[i], &owner, &constraint);
            if (owner != tkParam)
                VWrite("%08x (owner: %08x)  ", constraint, owner);
            else
                VWrite("%08x  ", constraint);
        }
        VWriteLine("");
    }
    m_pImport->CloseEnum(constraintEnum);

    sprintf_s(newprefix, 30, "%s\t", prefix);
    DisplayCustomAttributes(tkParam, newprefix);
}

// prints out name of typeref or typedef
//

LPCSTR MDInfo::TypeDeforRefName(mdToken inToken, _Out_writes_(bufLen) LPSTR buffer, ULONG bufLen)
{
    if (RidFromToken(inToken))
    {
        if (TypeFromToken(inToken) == mdtTypeDef)
            return (TypeDefName((mdTypeDef) inToken, buffer, bufLen));
        else if (TypeFromToken(inToken) == mdtTypeRef)
            return (TypeRefName((mdTypeRef) inToken, buffer, bufLen));
        else if (TypeFromToken(inToken) == mdtTypeSpec)
            return "[TypeSpec]";
        else
            return "[InvalidReference]";
    }
    else
        return "";
} // LPCSTR MDInfo::TypeDeforRefName()

LPCSTR MDInfo::MemberDeforRefName(mdToken inToken, _Out_writes_(bufLen) LPSTR buffer, ULONG bufLen)
{
    if (RidFromToken(inToken))
    {
        if (TypeFromToken(inToken) == mdtMethodDef || TypeFromToken(inToken) == mdtFieldDef)
            return (MemberName(inToken, buffer, bufLen));
        else if (TypeFromToken(inToken) == mdtMemberRef)
            return (MemberRefName((mdMemberRef) inToken, buffer, bufLen));
        else
            return "[InvalidReference]";
    }
    else
        return "";
} // LPCSTR MDInfo::MemberDeforRefName()

// prints out only the name of the given typedef
//
//
LPCSTR MDInfo::TypeDefName(mdTypeDef inTypeDef, _Out_writes_(bufLen) LPSTR buffer, ULONG bufLen)
{
    HRESULT hr;

    WCHAR tempBuf[STRING_BUFFER_LEN];
    hr = m_pImport->GetTypeDefProps(
                            // [IN] The import scope.
        inTypeDef,              // [IN] TypeDef token for inquiry.
        tempBuf,                // [OUT] Put name here.
        ARRAY_SIZE(tempBuf),    // [IN] size of name buffer in wide chars.
        NULL,                   // [OUT] put size of name (wide chars) here.
        NULL,                   // [OUT] Put flags here.
        NULL);                  // [OUT] Put base class TypeDef/TypeRef here.
    if (FAILED(hr))
    {
        strcpy_s(buffer, bufLen, "[Invalid TypeDef]");
    }
    else
    {
        (void)ConvertToUtf8(tempBuf, buffer, bufLen);
    }

    return buffer;
} // LPCSTR MDInfo::TypeDefName()

// prints out all the properties of a given typedef
//

void MDInfo::DisplayTypeDefProps(mdTypeDef inTypeDef)
{
    HRESULT hr;
    WCHAR typeDefName[STRING_BUFFER_LEN];
    char typeDefNameUtf8[ARRAY_SIZE(typeDefName) * MAX_UTF8_CVT];
    ULONG nameLen;
    DWORD flags;
    mdToken extends;
    ULONG       dwPacking;              // Packing size of class, if specified.
    ULONG       dwSize;                 // Total size of class, if specified.

    hr = m_pImport->GetTypeDefProps(
        inTypeDef,              // [IN] TypeDef token for inquiry.
        typeDefName,            // [OUT] Put name here.
        STRING_BUFFER_LEN,      // [IN] size of name buffer in wide chars.
        &nameLen,               // [OUT] put size of name (wide chars) here.
        &flags,                 // [OUT] Put flags here.
        &extends);              // [OUT] Put base class TypeDef/TypeRef here.
    if (FAILED(hr)) Error("GetTypeDefProps failed.", hr);

    char szTempBuf[STRING_BUFFER_LEN];

    VWriteLine("\tTypDefName: %s  (%8.8X)",ConvertToUtf8(typeDefName, typeDefNameUtf8, ARRAY_SIZE(typeDefNameUtf8)),inTypeDef);
    VWriteLine("\tFlags     : %s (%08x)",ClassFlags(flags, szTempBuf), flags);
    VWriteLine("\tExtends   : %8.8X [%s] %s",extends,TokenTypeName(extends),
                                 TypeDeforRefName(extends, szTempBuf, ARRAY_SIZE(szTempBuf)));

    hr = m_pImport->GetClassLayout(inTypeDef, &dwPacking, 0,0,0, &dwSize);
    if (hr == S_OK)
        VWriteLine("\tLayout    : Packing:%d, Size:%d", dwPacking, dwSize);

    if (IsTdNested(flags))
    {
        mdTypeDef   tkEnclosingClass;

        hr = m_pImport->GetNestedClassProps(inTypeDef, &tkEnclosingClass);
        if (hr == S_OK)
        {
            VWriteLine("\tEnclosingClass : %s (%8.8X)", TypeDeforRefName(tkEnclosingClass,
                                            szTempBuf, ARRAY_SIZE(szTempBuf)), tkEnclosingClass);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            WriteLine("ERROR: EnclosingClass not found for NestedClass");
        else
            Error("GetNestedClassProps failed.", hr);
    }
} // void MDInfo::DisplayTypeDefProps()

//  Prints out the name of the given TypeRef
//

LPCSTR MDInfo::TypeRefName(mdTypeRef tr, _Out_writes_(bufLen) LPSTR buffer, ULONG bufLen)
{
    HRESULT hr;

    WCHAR tempBuf[STRING_BUFFER_LEN];
    hr = m_pImport->GetTypeRefProps(
        tr,                 // The class ref token.
        NULL,               // Resolution scope.
        tempBuf,            // Put the name here.
        ARRAY_SIZE(tempBuf), // Size of the name buffer, wide chars.
        NULL);              // Put actual size of name here.
    if (FAILED(hr))
    {
        strcpy_s(buffer, bufLen, "[Invalid TypeRef]");
    }
    else
    {
        (void)ConvertToUtf8(tempBuf, buffer, bufLen);
    }

    return buffer;
} // LPCSTR MDInfo::TypeRefName()

// Prints out all the info of the given TypeRef
//

void MDInfo::DisplayTypeRefInfo(mdTypeRef tr)
{
    HRESULT hr;
    mdToken tkResolutionScope;
    WCHAR typeRefName[STRING_BUFFER_LEN];
    char typeRefNameUtf8[ARRAY_SIZE(typeRefName) * MAX_UTF8_CVT];
    ULONG nameLen;

    hr = m_pImport->GetTypeRefProps(
        tr,                 // The class ref token.
        &tkResolutionScope, // ResolutionScope.
        typeRefName,        // Put the name here.
        ARRAY_SIZE(typeRefName),  // Size of the name buffer, wide chars.
        &nameLen);          // Put actual size of name here.

    if (FAILED(hr)) Error("GetTypeRefProps failed.", hr);

    VWriteLine("Token:             0x%08x", tr);
    VWriteLine("ResolutionScope:   0x%08x", tkResolutionScope);
    VWriteLine("TypeRefName:       %s", ConvertToUtf8(typeRefName, typeRefNameUtf8, ARRAY_SIZE(typeRefNameUtf8)));

    DisplayCustomAttributes(tr, "\t");
} // void MDInfo::DisplayTypeRefInfo()


void MDInfo::DisplayTypeSpecInfo(mdTypeSpec ts, const char *preFix)
{
    HRESULT hr;
    PCCOR_SIGNATURE pvSig;
    ULONG           cbSig;
    ULONG           cb;

    InitSigBuffer();

    hr = m_pImport->GetTypeSpecFromToken(
        ts,             // The class ref token.
        &pvSig,
        &cbSig);

    if (FAILED(hr)) Error("GetTypeSpecFromToken failed.", hr);

//    DisplaySignature(pvSig, cbSig, preFix);

    if (FAILED(hr = GetOneElementType(pvSig, cbSig, &cb)))
        goto ErrExit;

    VWriteLine("%s\tTypeSpec :%s", preFix, (LPSTR)m_sigBuf.Ptr());

    // Hex, too?
    if (m_DumpFilter & dumpMoreHex)
    {
        char rcNewPrefix[80];
        sprintf_s(rcNewPrefix, 80, "%s\tSignature", preFix);
        DumpHex(rcNewPrefix, pvSig, cbSig, false, 24);
    }
ErrExit:
    return;
} // void MDInfo::DisplayTypeSpecInfo()

void MDInfo::DisplayMethodSpecInfo(mdMethodSpec ms, const char *preFix)
{
    HRESULT hr;
    PCCOR_SIGNATURE pvSig;
    ULONG           cbSig;
    mdToken         tk;

    InitSigBuffer();

    hr = m_pImport->GetMethodSpecProps(
        ms,             // The MethodSpec token
        &tk,            // The MethodDef or MemberRef
        &pvSig,         // Signature.
        &cbSig);        // Size of signature.

    VWriteLine("%s\tParent   : 0x%08x", preFix, tk);
    DisplaySignature(pvSig, cbSig, preFix);
//ErrExit:
    return;
} // void MDInfo::DisplayMethodSpecInfo()

// Return the passed-in buffer filled with a string detailing the class flags
// associated with the class.
//

char *MDInfo::ClassFlags(DWORD flags, _Out_writes_(STRING_BUFFER_LEN) char *szTempBuf)
{
    szTempBuf[0] = 0;
    ISFLAG(Td, NotPublic);
    ISFLAG(Td, Public);
    ISFLAG(Td, NestedPublic);
    ISFLAG(Td, NestedPrivate);
    ISFLAG(Td, NestedFamily);
    ISFLAG(Td, NestedAssembly);
    ISFLAG(Td, NestedFamANDAssem);
    ISFLAG(Td, NestedFamORAssem);
    ISFLAG(Td, AutoLayout);
    ISFLAG(Td, SequentialLayout);
    ISFLAG(Td, ExplicitLayout);
    ISFLAG(Td, Class);
    ISFLAG(Td, Interface);
    ISFLAG(Td, Abstract);
    ISFLAG(Td, Sealed);
    ISFLAG(Td, SpecialName);
    ISFLAG(Td, Import);
    ISFLAG(Td, Serializable);
    ISFLAG(Td, AnsiClass);
    ISFLAG(Td, UnicodeClass);
    ISFLAG(Td, AutoClass);
    ISFLAG(Td, BeforeFieldInit);
    ISFLAG(Td, Forwarder);
    // "Reserved" flags
    ISFLAG(Td, RTSpecialName);
    ISFLAG(Td, HasSecurity);
    ISFLAG(Td, WindowsRuntime);
    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    return szTempBuf;
} // char *MDInfo::ClassFlags()

// prints out all info on the given typeDef, including all information that
// is specific to a given typedef
//

void MDInfo::DisplayTypeDefInfo(mdTypeDef inTypeDef)
{
    DisplayTypeDefProps(inTypeDef);

    // Get field layout information.
    HRESULT             hr = NOERROR;
    COR_FIELD_OFFSET    *rFieldOffset = NULL;
    ULONG               cFieldOffset = 0;
    hr = m_pImport->GetClassLayout(inTypeDef, NULL, rFieldOffset, 0, &cFieldOffset, NULL);
    if (SUCCEEDED(hr) && cFieldOffset)
    {
        rFieldOffset = new COR_FIELD_OFFSET[cFieldOffset];
        if (rFieldOffset == NULL)
            Error("_calloc failed.", E_OUTOFMEMORY);
        hr = m_pImport->GetClassLayout(inTypeDef, NULL, rFieldOffset, cFieldOffset, &cFieldOffset, NULL);
        if (FAILED(hr)) { delete [] rFieldOffset; Error("GetClassLayout() failed.", hr); }
    }

    //No reason to display members if we're displaying fields and methods separately
    DisplayGenericParams(inTypeDef, "\t");
    DisplayFields(inTypeDef, rFieldOffset, cFieldOffset);
    delete [] rFieldOffset;
    DisplayMethods(inTypeDef);
    DisplayProperties(inTypeDef);
    DisplayEvents(inTypeDef);
    DisplayMethodImpls(inTypeDef);
    DisplayPermissions(inTypeDef, "");

    DisplayInterfaceImpls(inTypeDef);
    DisplayCustomAttributes(inTypeDef, "\t");
} // void MDInfo::DisplayTypeDefInfo()

// print out information about every the given typeDef's interfaceImpls
//

void MDInfo::DisplayInterfaceImpls(mdTypeDef inTypeDef)
{
    HCORENUM interfaceImplEnum = NULL;
    mdTypeRef interfaceImpls[ENUM_BUFFER_SIZE];
    ULONG count, totalCount = 1;
    HRESULT hr;

    while(SUCCEEDED(hr = m_pImport->EnumInterfaceImpls( &interfaceImplEnum,
                             inTypeDef,interfaceImpls,ARRAY_SIZE(interfaceImpls), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("\tInterfaceImpl #%d (%08x)", totalCount, interfaceImpls[i]);
            WriteLine("\t-------------------------------------------------------");
            DisplayInterfaceImplInfo(interfaceImpls[i]);
            DisplayPermissions(interfaceImpls[i], "\t");
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( interfaceImplEnum);
} // void MDInfo::DisplayInterfaceImpls()

// print the information for the given interface implementation
//

void MDInfo::DisplayInterfaceImplInfo(mdInterfaceImpl inImpl)
{
    mdTypeDef typeDef;
    mdToken token;
    HRESULT hr;

    char szTempBuf[STRING_BUFFER_LEN];

    hr = m_pImport->GetInterfaceImplProps( inImpl, &typeDef, &token);
    if (FAILED(hr)) Error("GetInterfaceImplProps failed.", hr);

    VWriteLine("\t\tClass     : %s",TypeDeforRefName(typeDef, szTempBuf, ARRAY_SIZE(szTempBuf)));
    VWriteLine("\t\tToken     : %8.8X [%s] %s",token,TokenTypeName(token), TypeDeforRefName(token, szTempBuf, ARRAY_SIZE(szTempBuf)));

    DisplayCustomAttributes(inImpl, "\t\t");
} // void MDInfo::DisplayInterfaceImplInfo()

// displays the information for a particular property
//

void MDInfo::DisplayPropertyInfo(mdProperty inProp)
{
    HRESULT     hr;
    mdTypeDef   typeDef;
    WCHAR       propName[STRING_BUFFER_LEN];
    char        propNameUtf8[ARRAY_SIZE(propName) * MAX_UTF8_CVT];
    DWORD       flags;
#ifdef FEATURE_COMINTEROP
    VARIANT     defaultValue;
#endif
    void const  *pValue;
    ULONG       cbValue;
    DWORD       dwCPlusTypeFlag;
    mdMethodDef setter, getter, otherMethod[ENUM_BUFFER_SIZE];
    ULONG       others;
    PCCOR_SIGNATURE pbSigBlob;
    ULONG       ulSigBlob;


#ifdef FEATURE_COMINTEROP
    ::VariantInit(&defaultValue);
#endif
    hr = m_pImport->GetPropertyProps(
        inProp,                 // [IN] property token
        &typeDef,               // [OUT] typedef containing the property declarion.

        propName,               // [OUT] Property name
        STRING_BUFFER_LEN,      // [IN] the count of wchar of szProperty
        NULL,                   // [OUT] actual count of wchar for property name

        &flags,                 // [OUT] property flags.

        &pbSigBlob,             // [OUT] Signature Blob.
        &ulSigBlob,             // [OUT] Number of bytes in the signature blob.

        &dwCPlusTypeFlag,       // [OUT] default value
        &pValue,
        &cbValue,

        &setter,                // [OUT] setter method of the property
        &getter,                // [OUT] getter method of the property

        otherMethod,            // [OUT] other methods of the property
        ENUM_BUFFER_SIZE,       // [IN] size of rmdOtherMethod
        &others);               // [OUT] total number of other method of this property

    if (FAILED(hr)) Error("GetPropertyProps failed.", hr);

    VWriteLine("\t\tProp.Name : %s (%8.8X)",ConvertToUtf8(propName, propNameUtf8, ARRAY_SIZE(propNameUtf8)),inProp);

    char szTempBuf[STRING_BUFFER_LEN];

    szTempBuf[0] = 0;
    ISFLAG(Pr, SpecialName);
    ISFLAG(Pr, RTSpecialName);
    ISFLAG(Pr, HasDefault);
    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    VWriteLine("\t\tFlags     : %s (%08x)", szTempBuf, flags);

    if (ulSigBlob)
        DisplaySignature(pbSigBlob, ulSigBlob, "");
    else
        VWriteLine("\t\tERROR: no valid signature ");

#ifdef FEATURE_COMINTEROP
    _FillVariant((BYTE)dwCPlusTypeFlag, pValue, cbValue, &defaultValue);
    VWriteLine("\t\tDefltValue: %s",VariantAsString(&defaultValue, szTempBuf, ARRAY_SIZE(szTempBuf)));
#endif

    VWriteLine("\t\tSetter    : (%08x) %s",setter,MemberDeforRefName(setter, szTempBuf, ARRAY_SIZE(szTempBuf)));
    VWriteLine("\t\tGetter    : (%08x) %s",getter,MemberDeforRefName(getter, szTempBuf, ARRAY_SIZE(szTempBuf)));

    // do something with others?
    VWriteLine("\t\t%u Others",others);
    DisplayCustomAttributes(inProp, "\t\t");

#ifdef FEATURE_COMINTEROP
    ::VariantClear(&defaultValue);
#endif
} // void MDInfo::DisplayPropertyInfo()

// displays info for each property
//

void MDInfo::DisplayProperties(mdTypeDef inTypeDef)
{
    HCORENUM propEnum = NULL;
    mdProperty props[ENUM_BUFFER_SIZE];
    ULONG count, totalCount = 1;
    HRESULT hr;


    while(SUCCEEDED(hr = m_pImport->EnumProperties( &propEnum,
                             inTypeDef,props,ARRAY_SIZE(props), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("\tProperty #%d (%08x)", totalCount, props[i]);
            WriteLine("\t-------------------------------------------------------");
            DisplayPropertyInfo(props[i]);
            DisplayPermissions(props[i], "\t");
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( propEnum);
} // void MDInfo::DisplayProperties()

// Display all information about a particular event
//

void MDInfo::DisplayEventInfo(mdEvent inEvent)
{
    HRESULT hr;
    mdTypeDef typeDef;
    WCHAR eventName[STRING_BUFFER_LEN];
    char eventNameUtf8[ARRAY_SIZE(eventName) * MAX_UTF8_CVT];
    DWORD flags;
    mdToken eventType;
    mdMethodDef addOn, removeOn, fire, otherMethod[ENUM_BUFFER_SIZE];
    ULONG totalOther;


    hr = m_pImport->GetEventProps(
                            // [IN] The scope.
        inEvent,                // [IN] event token
        &typeDef,               // [OUT] typedef containing the event declarion.

        eventName,              // [OUT] Event name
        STRING_BUFFER_LEN,      // [IN] the count of wchar of szEvent
        NULL,                   // [OUT] actual count of wchar for event's name

        &flags,                 // [OUT] Event flags.
        &eventType,             // [OUT] EventType class

        &addOn,                 // [OUT] AddOn method of the event
        &removeOn,              // [OUT] RemoveOn method of the event
        &fire,                  // [OUT] Fire method of the event

        otherMethod,            // [OUT] other method of the event
        ARRAY_SIZE(otherMethod),  // [IN] size of rmdOtherMethod
        &totalOther);           // [OUT] total number of other method of this event
    if (FAILED(hr)) Error("GetEventProps failed.", hr);

    VWriteLine("\t\tName      : %s (%8.8X)",ConvertToUtf8(eventName, eventNameUtf8, ARRAY_SIZE(eventNameUtf8)),inEvent);

    char szTempBuf[STRING_BUFFER_LEN];

    szTempBuf[0] = 0;
    ISFLAG(Ev, SpecialName);
    ISFLAG(Ev, RTSpecialName);
    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    VWriteLine("\t\tFlags     : %s (%08x)", szTempBuf, flags);
    VWriteLine("\t\tEventType : %8.8X [%s]",eventType,TokenTypeName(eventType));
    VWriteLine("\t\tAddOnMethd: (%08x) %s",addOn,MemberDeforRefName(addOn, szTempBuf, ARRAY_SIZE(szTempBuf)));
    VWriteLine("\t\tRmvOnMethd: (%08x) %s",removeOn,MemberDeforRefName(removeOn, szTempBuf, ARRAY_SIZE(szTempBuf)));
    VWriteLine("\t\tFireMethod: (%08x) %s",fire,MemberDeforRefName(fire, szTempBuf, ARRAY_SIZE(szTempBuf)));

    VWriteLine("\t\t%ld OtherMethods",totalOther);

    DisplayCustomAttributes(inEvent, "\t\t");
} // void MDInfo::DisplayEventInfo()

// Display information about all events in a typedef
//
void MDInfo::DisplayEvents(mdTypeDef inTypeDef)
{
    HCORENUM eventEnum = NULL;
    mdProperty events[ENUM_BUFFER_SIZE];
    ULONG count, totalCount = 1;
    HRESULT hr;


    while(SUCCEEDED(hr = m_pImport->EnumEvents( &eventEnum,
                             inTypeDef,events,ARRAY_SIZE(events), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("\tEvent #%d (%08x)", totalCount, events[i]);
            WriteLine("\t-------------------------------------------------------");
            DisplayEventInfo(events[i]);
            DisplayPermissions(events[i], "\t");
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( eventEnum);
} // void MDInfo::DisplayEvents()


// print info for the passed-in custom attribute
// This function is used to print the custom attribute information for both TypeDefs and
// MethodDefs which need slightly different formatting.  preFix helps fix it up.
//

void MDInfo::DisplayCustomAttributeInfo(mdCustomAttribute inValue, const char *preFix)
{
    const BYTE  *pValue;                // The custom value.
    ULONG       cbValue;                // Length of the custom value.
    HRESULT     hr;                     // A result.
    mdToken     tkObj;                  // Attributed object.
    mdToken     tkType;                 // Type of the custom attribute.
    mdToken     tk;                     // For name lookup.
    LPCUTF8     pMethName=0;            // Name of custom attribute ctor, if any.
    CQuickBytes qSigName;               // Buffer to pretty-print signature.
    PCCOR_SIGNATURE pSig=0;             // Signature of ctor.
    ULONG       cbSig=0;                // Size of the signature.
    BOOL        bCoffSymbol = false;    // true for coff symbol CA's.
    WCHAR       rcName[MAX_CLASS_NAME]; // Name of the type.
    char        rcNameUtf8[ARRAY_SIZE(rcName) * MAX_UTF8_CVT]; // Name of the type.

    hr = m_pImport->GetCustomAttributeProps( // S_OK or error.
        inValue,                    // The attribute.
        &tkObj,                     // The attributed object
        &tkType,                    // The attributes type.
        (const void**)&pValue,      // Put pointer to data here.
        &cbValue);                  // Put size here.
    if (FAILED(hr)) Error("GetCustomAttributeProps failed.", hr);

    VWriteLine("%s\tCustomAttribute Type: %08x", preFix, tkType);

    // Get the name of the memberref or methoddef.
    tk = tkType;
    rcName[0] = W('\0');
    rcNameUtf8[0] = '\0';
    // Get the member name, and the parent token.
    switch (TypeFromToken(tk))
    {
    case mdtMemberRef:
        hr = m_pImport->GetNameFromToken(tk, &pMethName);
        if (FAILED(hr)) Error("GetNameFromToken failed.", hr);
        hr = m_pImport->GetMemberRefProps( tk, &tk, 0, 0, 0, &pSig, &cbSig);
        if (FAILED(hr)) Error("GetMemberRefProps failed.", hr);
        break;
    case mdtMethodDef:
        hr = m_pImport->GetNameFromToken(tk, &pMethName);
        if (FAILED(hr)) Error("GetNameFromToken failed.", hr);
        hr = m_pImport->GetMethodProps(tk, &tk, 0, 0, 0, 0, &pSig, &cbSig, 0, 0);
        if (FAILED(hr)) Error("GetMethodProps failed.", hr);
        break;
    } // switch

    // Get the type name.
    switch (TypeFromToken(tk))
    {
    case mdtTypeDef:
        hr = m_pImport->GetTypeDefProps(tk, rcName,MAX_CLASS_NAME,0, 0,0);
        if (FAILED(hr)) Error("GetTypeDefProps failed.", hr);
        break;
    case mdtTypeRef:
        hr = m_pImport->GetTypeRefProps(tk, 0, rcName,MAX_CLASS_NAME,0);
        if (FAILED(hr)) Error("GetTypeRefProps failed.", hr);
        break;
    } // switch


    if (pSig && pMethName)
    {
        int iLen= 1 + (ULONG32)strlen(pMethName);
        LPWSTR pwzName = (LPWSTR)(new WCHAR[iLen]);
        if(pwzName)
        {
            WszMultiByteToWideChar(CP_UTF8,0, pMethName,-1, pwzName,iLen);
            PrettyPrintSigLegacy(pSig, cbSig, pwzName, &qSigName, m_pImport);
            delete [] pwzName;
        }
    }

    VWrite("%s\tCustomAttributeName: %s", preFix, ConvertToUtf8(rcName, rcNameUtf8, ARRAY_SIZE(rcNameUtf8)));
    if (pSig && pMethName)
    {
        int iLen = 1 + (ULONG32)qSigName.Size();
        LPSTR pzSig = (LPSTR)(new char[iLen]);
        VWrite(" :: %s", ConvertToUtf8((LPWSTR)qSigName.Ptr(), pzSig, iLen));
        delete [] pzSig;
    }

    // Keep track of coff overhead.
    if (!u16_strcmp(W("__DecoratedName"), rcName))
    {
        bCoffSymbol = true;
        g_cbCoffNames += cbValue + 6;
    }
    WriteLine("");

    VWriteLine("%s\tLength: %ld", preFix, cbValue);
    char newPreFix[40];
    sprintf_s(newPreFix, 40, "%s\tValue ", preFix);
    DumpHex(newPreFix, pValue, cbValue);
    if (bCoffSymbol)
        VWriteLine("%s\t            %s", preFix, pValue);

    // Try to decode the constructor blob.  This is incomplete, but covers the most popular cases.
    if (pSig)
    {   // Interpret the signature.
        PCCOR_SIGNATURE ps = pSig;
        ULONG cb;
        ULONG ulData;
        ULONG cParams;
        ULONG ulVal;
        UINT8 u1 = 0;
        UINT16 u2 = 0;
        UINT32 u4 = 0;
        UINT64 u8 = 0;
        unsigned __int64 uI64;
        double dblVal;
        ULONG cbVal;
        LPCUTF8 pStr;
        CustomAttributeParser CA(pValue, cbValue);
        CA.ValidateProlog();

        // Get the calling convention.
        cb = CorSigUncompressData(ps, &ulData);
        ps += cb;
        // Get the count of params.
        cb = CorSigUncompressData(ps, &cParams);
        ps += cb;
        // Get the return value.
        cb = CorSigUncompressData(ps, &ulData);
        ps += cb;
        if (ulData == ELEMENT_TYPE_VOID)
        {
            VWrite("%s\tctor args: (", preFix);
            // For each param...
            for (ULONG i=0; i<cParams; ++i)
            {   // Get the next param type.
                cb = CorSigUncompressData(ps, &ulData);
                ps += cb;
                if (i) Write(", ");
            DoObject:
                switch (ulData)
                {
                // For ET_OBJECT, the next byte in the blob is the ET of the actual data.
                case ELEMENT_TYPE_OBJECT:
                    CA.GetU1(&u1);
                    ulData = u1;
                    goto DoObject;
                case ELEMENT_TYPE_I1:
                case ELEMENT_TYPE_U1:
                    CA.GetU1(&u1);
                    ulVal = u1;
                    goto PrintVal;
                case ELEMENT_TYPE_I2:
                case ELEMENT_TYPE_U2:
                    CA.GetU2(&u2);
                    ulVal = u2;
                    goto PrintVal;
                case ELEMENT_TYPE_I4:
                case ELEMENT_TYPE_U4:
                    CA.GetU4(&u4);
                    ulVal = u4;
                PrintVal:
                VWrite("%d", ulVal);
                    break;
                case ELEMENT_TYPE_STRING:
                    CA.GetString(&pStr, &cbVal);
                    VWrite("\"%s\"", pStr);
                    break;
                // The only class type that we accept is Type, which is stored as a string.
                case ELEMENT_TYPE_CLASS:
                    // Eat the class type.
                    cb = CorSigUncompressData(ps, &ulData);
                    ps += cb;
                    // Get the name of the type.
                    CA.GetString(&pStr, &cbVal);
                    VWrite("typeof(%s)", pStr);
                    break;
                case SERIALIZATION_TYPE_TYPE:
                    CA.GetString(&pStr, &cbVal);
                    VWrite("typeof(%s)", pStr);
                    break;
                case ELEMENT_TYPE_I8:
                case ELEMENT_TYPE_U8:
                    CA.GetU8(&u8);
                    uI64 = u8;
                    VWrite("0x%llx", uI64);
                    break;
                case ELEMENT_TYPE_R4:
                    dblVal = CA.GetR4();
                    VWrite("%f", dblVal);
                    break;
                case ELEMENT_TYPE_R8:
                    dblVal = CA.GetR8();
                    VWrite("%f", dblVal);
                    break;
                default:
                    // bail...
                    i = cParams;
                    Write(" <can not decode> ");
                    break;
                }
            }
            WriteLine(")");
        }

    }
    WriteLine("");
} // void MDInfo::DisplayCustomAttributeInfo()

// Print all custom values for the given token
// This function is used to print the custom value information for all tokens.
// which need slightly different formatting.  preFix helps fix it up.
//

void MDInfo::DisplayCustomAttributes(mdToken inToken, const char *preFix)
{
    HCORENUM customAttributeEnum = NULL;
    mdTypeRef customAttributes[ENUM_BUFFER_SIZE];
    ULONG count, totalCount = 1;
    HRESULT hr;

    while(SUCCEEDED(hr = m_pImport->EnumCustomAttributes( &customAttributeEnum, inToken, 0,
                             customAttributes, ARRAY_SIZE(customAttributes), &count)) &&
          count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("%sCustomAttribute #%d (%08x)", preFix, totalCount, customAttributes[i]);
            VWriteLine("%s-------------------------------------------------------", preFix);
            DisplayCustomAttributeInfo(customAttributes[i], preFix);
        }
    }
    m_pImport->CloseEnum( customAttributeEnum);
} // void MDInfo::DisplayCustomAttributes()

//  Show the passed-in token's permissions
//
//

void MDInfo::DisplayPermissions(mdToken tk, const char *preFix)
{
    HCORENUM permissionEnum = NULL;
    mdPermission permissions[ENUM_BUFFER_SIZE];
    ULONG count, totalCount = 1;
    HRESULT hr;


    while (SUCCEEDED(hr = m_pImport->EnumPermissionSets( &permissionEnum,
                     tk, 0, permissions, ARRAY_SIZE(permissions), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("%s\tPermission #%d (%08x)", preFix, totalCount, permissions[i]);
            VWriteLine("%s\t-------------------------------------------------------", preFix);
            DisplayPermissionInfo(permissions[i], preFix);
            WriteLine("");
        }
    }
    m_pImport->CloseEnum( permissionEnum);
} // void MDInfo::DisplayPermissions()

// print properties of given rolecheck
//
//

void MDInfo::DisplayPermissionInfo(mdPermission inPermission, const char *preFix)
{
    DWORD dwAction;
    const BYTE *pvPermission;
    ULONG cbPermission;
    const char *flagDesc = NULL;
    char newPreFix[STRING_BUFFER_LEN];
    HRESULT hr;


    hr = m_pImport->GetPermissionSetProps( inPermission, &dwAction,
                                        (const void**)&pvPermission, &cbPermission);
    if (FAILED(hr)) Error("GetPermissionSetProps failed.", hr);

    switch(dwAction)
    {
    case dclActionNil:          flagDesc = "ActionNil"; break;
    case dclRequest:            flagDesc = "Request"; break;
    case dclDemand:             flagDesc = "Demand"; break;
    case dclAssert:             flagDesc = "Assert"; break;
    case dclDeny:               flagDesc = "Deny"; break;
    case dclPermitOnly:         flagDesc = "PermitOnly"; break;
    case dclLinktimeCheck:      flagDesc = "LinktimeCheck"; break;
    case dclInheritanceCheck:   flagDesc = "InheritanceCheck"; break;
    case dclRequestMinimum:     flagDesc = "RequestMinimum"; break;
    case dclRequestOptional:    flagDesc = "RequestOptional"; break;
    case dclRequestRefuse:      flagDesc = "RequestRefuse"; break;
    case dclPrejitGrant:        flagDesc = "PrejitGrant"; break;
    case dclPrejitDenied:       flagDesc = "PrejitDenied"; break;
    case dclNonCasDemand:       flagDesc = "NonCasDemand"; break;
    case dclNonCasLinkDemand:   flagDesc = "NonCasLinkDemand"; break;
    case dclNonCasInheritance:  flagDesc = "NonCasInheritance"; break;

    }
    VWriteLine("%s\t\tAction    : %s", preFix, flagDesc);
    VWriteLine("%s\t\tBlobLen   : %d", preFix, cbPermission);
    if (cbPermission)
    {
        sprintf_s(newPreFix, STRING_BUFFER_LEN, "%s\tBlob", preFix);
        DumpHex(newPreFix, pvPermission, cbPermission, false, 24);
    }

    sprintf_s (newPreFix, STRING_BUFFER_LEN, "\t\t%s", preFix);
    DisplayCustomAttributes(inPermission, newPreFix);
} // void MDInfo::DisplayPermissionInfo()


// simply prints out the given GUID in standard form

LPCSTR MDInfo::GUIDAsString(GUID inGuid, _Out_writes_(bufLen) LPSTR guidString, ULONG bufLen)
{
    GuidToLPSTR(inGuid, guidString, bufLen);
    return guidString;
} // LPCSTR MDInfo::GUIDAsString()

#ifdef FEATURE_COMINTEROP
LPCSTR MDInfo::VariantAsString(VARIANT *pVariant, _Out_writes_(bufLen) LPSTR buffer, ULONG bufLen)
{
    HRESULT hr = S_OK;
    if (V_VT(pVariant) == VT_UNKNOWN)
    {
        _ASSERTE(V_UNKNOWN(pVariant) == NULL);
        return "<NULL>";
    }
    else if (SUCCEEDED(hr = ::VariantChangeType(pVariant, pVariant, 0, VT_BSTR)))
    {
        return ConvertToUtf8(V_BSTR(pVariant), buffer, bufLen);
    }
    else if (hr == DISP_E_BADVARTYPE && V_VT(pVariant) == VT_I8)
    {
        // Set variant type to bstr.
        V_VT(pVariant) = VT_BSTR;
        sprintf_s(buffer, bufLen, "%lld", V_CY(pVariant).int64);
        return buffer;
    }
    else
    {
        return "ERROR";
    }

} // LPSTR MDInfo::VariantAsString()
#endif

bool TrySigUncompress(PCCOR_SIGNATURE pData,              // [IN] compressed data
                      ULONG       *pDataOut,              // [OUT] the expanded *pData
                      ULONG       *cbCur)
{
    ULONG ulSize = CorSigUncompressData(pData, pDataOut);
    if (ulSize == (ULONG)-1)
    {
        *cbCur = ulSize;
        return false;
    } else
    {
        *cbCur += ulSize;
        return true;
    }
}

void MDInfo::DisplayFieldMarshal(mdToken inToken)
{
    PCCOR_SIGNATURE pvNativeType;     // [OUT] native type of this field
    ULONG       cbNativeType;         // [OUT] the count of bytes of *ppvNativeType
    HRESULT hr;


    hr = m_pImport->GetFieldMarshal( inToken, &pvNativeType, &cbNativeType);
    if (FAILED(hr) && hr != CLDB_E_RECORD_NOTFOUND) Error("GetFieldMarshal failed.", hr);
    if (hr != CLDB_E_RECORD_NOTFOUND)
    {
        ULONG cbCur = 0;
        ULONG ulData;
        ULONG ulStrLoc;

        char szNTDesc[STRING_BUFFER_LEN];

        while (cbCur < cbNativeType)
        {
            ulStrLoc = 0;

            ulData = NATIVE_TYPE_MAX;
            if (!TrySigUncompress(&pvNativeType[cbCur], &ulData, &cbCur))
                continue;
            if (ulData >= sizeof(g_szNativeType)/sizeof(*g_szNativeType))
            {
                cbCur = (ULONG)-1;
                continue;
            }
            ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "%s ", g_szNativeType[ulData]);
            switch (ulData)
            {
            case NATIVE_TYPE_FIXEDSYSSTRING:
                {
                    if (cbCur < cbNativeType)
                    {
                        if (!TrySigUncompress(&pvNativeType[cbCur], &ulData, &cbCur))
                            continue;
                        ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "{StringElementCount: %d} ",ulData);
                    }
                }
                break;
            case NATIVE_TYPE_FIXEDARRAY:
                {
                    if (cbCur < cbNativeType)
                    {
                        if (!TrySigUncompress(&pvNativeType[cbCur], &ulData, &cbCur))
                            continue;
                        ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "{ArrayElementCount: %d",ulData);

                        if (cbCur < cbNativeType)
                        {
                            if (!TrySigUncompress(&pvNativeType[cbCur], &ulData, &cbCur))
                                continue;
                            ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, ", ArrayElementType(NT): %d",ulData);
                        }

                        ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc,"}");
                    }
                }
                break;
            case NATIVE_TYPE_ARRAY:
                {
                    if (cbCur < cbNativeType)
                    {
                        BOOL bElemTypeSpecified;

                        if (!TrySigUncompress(&pvNativeType[cbCur], &ulData, &cbCur))
                            continue;
                        if (ulData != NATIVE_TYPE_MAX)
                        {
                            ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "{ArrayElementType(NT): %d", ulData);
                            bElemTypeSpecified = TRUE;
                        }
                        else
                        {
                            ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "{");
                            bElemTypeSpecified = FALSE;
                        }

                        if (cbCur < cbNativeType)
                        {
                            if (bElemTypeSpecified)
                                ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, ", ");

                            if (!TrySigUncompress(&pvNativeType[cbCur], &ulData, &cbCur))
                                continue;
                            ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "SizeParamIndex: %d",ulData);

                            if (cbCur < cbNativeType)
                            {
                                if (!TrySigUncompress(&pvNativeType[cbCur], &ulData, &cbCur))
                                    continue;
                                ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, ", SizeParamMultiplier: %d",ulData);

                                if (cbCur < cbNativeType)
                                {
                                    if (!TrySigUncompress(&pvNativeType[cbCur], &ulData, &cbCur))
                                        continue;
                                    ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, ", SizeConst: %d",ulData);
                                }
                            }
                        }

                        ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "}");
                    }
                }
                break;
            case NATIVE_TYPE_SAFEARRAY:
                {
                    if (cbCur < cbNativeType)
                    {
                        if (!TrySigUncompress(&pvNativeType[cbCur], &ulData, &cbCur))
                            continue;
                        ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "{SafeArraySubType(VT): %d, ",ulData);

                        // Extract the element type name if it is specified.
                        if (cbCur < cbNativeType)
                        {
                            LPUTF8 strTemp = NULL;
                            int strLen = 0;
                            int ByteCountLength = 0;

                            strLen = CPackedLen::GetLength(&pvNativeType[cbCur], &ByteCountLength);
                            cbCur += ByteCountLength;
                            strTemp = (LPUTF8)(new char[strLen + 1]);
                            if(strTemp)
                            {
                                memcpy(strTemp, (LPUTF8)&pvNativeType[cbCur], strLen);
                                strTemp[strLen] = 0;
                                ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "ElementTypeName: %s}", strTemp);
                                cbCur += strLen;
                                _ASSERTE(cbCur == cbNativeType);
                                 delete [] strTemp;
                            }
                        }
                        else
                        {
                            ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "ElementTypeName: }");
                        }
                    }
                }
                break;
            case NATIVE_TYPE_CUSTOMMARSHALER:
                {
                    LPUTF8 strTemp = NULL;
                    int strLen = 0;
                    int ByteCountLength = 0;

                    // Extract the typelib GUID.
                    strLen = CPackedLen::GetLength(&pvNativeType[cbCur], &ByteCountLength);
                    cbCur += ByteCountLength;
                    strTemp = (LPUTF8)(new char[strLen + 1]);
                    if(strTemp)
                    {
                        memcpy(strTemp, (LPUTF8)&pvNativeType[cbCur], strLen);
                        strTemp[strLen] = 0;
                        ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "{Typelib: %s, ", strTemp);
                        cbCur += strLen;
                        _ASSERTE(cbCur < cbNativeType);
                        delete [] strTemp;
                    }
                    // Extract the name of the native type.
                    strLen = CPackedLen::GetLength(&pvNativeType[cbCur], &ByteCountLength);
                    cbCur += ByteCountLength;
                    strTemp = (LPUTF8)(new char[strLen + 1]);
                    if(strTemp)
                    {
                        memcpy(strTemp, (LPUTF8)&pvNativeType[cbCur], strLen);
                        strTemp[strLen] = 0;
                        ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "Native: %s, ", strTemp);
                        cbCur += strLen;
                        _ASSERTE(cbCur < cbNativeType);
                        delete [] strTemp;
                    }

                    // Extract the name of the custom marshaler.
                    strLen = CPackedLen::GetLength(&pvNativeType[cbCur], &ByteCountLength);
                    cbCur += ByteCountLength;
                    strTemp = (LPUTF8)(new char[strLen + 1]);
                    if(strTemp)
                    {
                        memcpy(strTemp, (LPUTF8)&pvNativeType[cbCur], strLen);
                        strTemp[strLen] = 0;
                        ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "Marshaler: %s, ", strTemp);
                        cbCur += strLen;
                        _ASSERTE(cbCur < cbNativeType);
                        delete [] strTemp;
                    }
                    // Extract the cookie string.
                    strLen = CPackedLen::GetLength(&pvNativeType[cbCur], &ByteCountLength);
                    cbCur += ByteCountLength;
                    if (strLen > 0)
                    {
                        strTemp = (LPUTF8)(new char[strLen + 1]);
                        if(strTemp)
                        {
                            memcpy(strTemp, (LPUTF8)&pvNativeType[cbCur], strLen);
                            strTemp[strLen] = 0;
                            ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "Cookie: ");

                            // Copy the cookie string and transform the embedded nulls into \0's.
                            for (int i = 0; i < strLen - 1; i++, cbCur++)
                            {
                                if (strTemp[i] == 0)
                                    ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "\\0");
                                else
                                    szNTDesc[ulStrLoc++] = strTemp[i];
                            }
                            szNTDesc[ulStrLoc++] = strTemp[strLen - 1];
                            cbCur++;
                            delete [] strTemp;
                        }
                    }
                    else
                    {
                        ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "Cookie: ");
                    }

                    // Finish the custom marshaler native type description.
                    ulStrLoc += sprintf_s(szNTDesc + ulStrLoc, STRING_BUFFER_LEN-ulStrLoc, "}");
                    _ASSERTE(cbCur <= cbNativeType);
                }
                break;
            default:
                {
                    // normal nativetype element: do nothing
                }
            }
            VWriteLine("\t\t\t\t%s",szNTDesc);
            if (ulData >= NATIVE_TYPE_MAX)
                break;
        }
        if (cbCur == (ULONG)-1)
        {
            // There was something that we didn't grok in the signature.
            // Just dump out the blob as hex
            VWrite("\t\t\t\t{", szNTDesc);
            while (cbNativeType--)
                VWrite(" %2.2X", *pvNativeType++);
            VWriteLine(" }");
        }
    }
} // void MDInfo::DisplayFieldMarshal()

void MDInfo::DisplayPinvokeInfo(mdToken inToken)
{
    HRESULT hr = NOERROR;
    DWORD flags;
    WCHAR rcImport[512];
    char rcImportUtf8[ARRAY_SIZE(rcImport) * MAX_UTF8_CVT];
    mdModuleRef tkModuleRef;

    char szTempBuf[STRING_BUFFER_LEN];

    hr = m_pImport->GetPinvokeMap(inToken, &flags, rcImport,
                                  ARRAY_SIZE(rcImport), 0, &tkModuleRef);
    if (FAILED(hr))
    {
        if (hr != CLDB_E_RECORD_NOTFOUND)
            VWriteLine("ERROR: GetPinvokeMap failed.", hr);
        return;
    }

    WriteLine("\t\tPinvoke Map Data:");
    VWriteLine("\t\tEntry point:      %s", ConvertToUtf8(rcImport, rcImportUtf8, ARRAY_SIZE(rcImportUtf8)));
    VWriteLine("\t\tModule ref:       %08x", tkModuleRef);

    szTempBuf[0] = 0;
    ISFLAG(Pm, NoMangle);
    ISFLAG(Pm, CharSetNotSpec);
    ISFLAG(Pm, CharSetAnsi);
    ISFLAG(Pm, CharSetUnicode);
    ISFLAG(Pm, CharSetAuto);
    ISFLAG(Pm, SupportsLastError);
    ISFLAG(Pm, CallConvWinapi);
    ISFLAG(Pm, CallConvCdecl);
    ISFLAG(Pm, CallConvStdcall);
    ISFLAG(Pm, CallConvThiscall);
    ISFLAG(Pm, CallConvFastcall);

    ISFLAG(Pm, BestFitEnabled);
    ISFLAG(Pm, BestFitDisabled);
    ISFLAG(Pm, BestFitUseAssem);
    ISFLAG(Pm, ThrowOnUnmappableCharEnabled);
    ISFLAG(Pm, ThrowOnUnmappableCharDisabled);
    ISFLAG(Pm, ThrowOnUnmappableCharUseAssem);
    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    VWriteLine("\t\tMapping flags:    %s (%08x)", szTempBuf, flags);
}   // void MDInfo::DisplayPinvokeInfo()


/////////////////////////////////////////////////////////////////////////
// void DisplaySignature(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob);
//
// Display COM+ signature -- taken from cordump.cpp's DumpSignature
/////////////////////////////////////////////////////////////////////////
void MDInfo::DisplaySignature(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, const char *preFix)
{
    ULONG       cbCur = 0;
    ULONG       cb;
    // 428793: Prefix complained correctly about uninitialized data.
    ULONG       ulData = (ULONG) IMAGE_CEE_CS_CALLCONV_MAX;
    ULONG       ulArgs;
    HRESULT     hr = NOERROR;
    ULONG       ulSigBlobStart = ulSigBlob;

    // initialize sigBuf
    InitSigBuffer();

    cb = CorSigUncompressData(pbSigBlob, &ulData);
    VWriteLine("%s\t\tCallCnvntn: %s", preFix, (g_strCalling[ulData & IMAGE_CEE_CS_CALLCONV_MASK]));
    if (cb>ulSigBlob)
        goto ErrExit;
    cbCur += cb;
    ulSigBlob -= cb;

    if (ulData & IMAGE_CEE_CS_CALLCONV_HASTHIS)
        VWriteLine("%s\t\thasThis ", preFix);
    if (ulData & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS)
        VWriteLine("%s\t\texplicit ", preFix);
    if (ulData & IMAGE_CEE_CS_CALLCONV_GENERIC)
        VWriteLine("%s\t\tgeneric ", preFix);

    // initialize sigBuf
    InitSigBuffer();
    if ( isCallConv(ulData,IMAGE_CEE_CS_CALLCONV_FIELD) )
    {

        // display field type
        if (FAILED(hr = GetOneElementType(&pbSigBlob[cbCur], ulSigBlob, &cb)))
            goto ErrExit;
        VWriteLine("%s\t\tField type: %s", preFix, (LPSTR)m_sigBuf.Ptr());
        if (cb>ulSigBlob)
            goto ErrExit;
        cbCur += cb;
        ulSigBlob -= cb;
    }
    else
    {
        if (ulData & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
          ULONG ulTyArgs;
          cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulTyArgs);
          if (cb>ulSigBlob)
            goto ErrExit;
          cbCur += cb;
          ulSigBlob -= cb;
          VWriteLine("%s\t\tType Arity:%d ", preFix, ulTyArgs);
    }
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulArgs);
        if (cb>ulSigBlob)
            goto ErrExit;
        cbCur += cb;
        ulSigBlob -= cb;

        if (ulData != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG && ulData != IMAGE_CEE_CS_CALLCONV_GENERICINST)
        {
            // display return type when it is not a local varsig
            if (FAILED(hr = GetOneElementType(&pbSigBlob[cbCur], ulSigBlob, &cb)))
                goto ErrExit;
            VWriteLine("%s\t\tReturnType:%s", preFix, (LPSTR)m_sigBuf.Ptr());
            if (cb>ulSigBlob)
                goto ErrExit;
            cbCur += cb;
            ulSigBlob -= cb;
        }

        // display count of argument
        // display arguments
        if (ulSigBlob)
            VWriteLine("%s\t\t%ld Arguments", preFix, ulArgs);
        else
            VWriteLine("%s\t\tNo arguments.", preFix);

        ULONG       i = 0;
        while (i < ulArgs && ulSigBlob > 0)
        {
            ULONG       ulDataTemp;

            // Handle the sentinel for varargs because it isn't counted in the args.
            CorSigUncompressData(&pbSigBlob[cbCur], &ulDataTemp);
            ++i;

            // initialize sigBuf
            InitSigBuffer();

            if (FAILED(hr = GetOneElementType(&pbSigBlob[cbCur], ulSigBlob, &cb)))
                goto ErrExit;

            VWriteLine("%s\t\t\tArgument #%ld: %s",preFix, i, (LPSTR)m_sigBuf.Ptr());

            if (cb>ulSigBlob)
                goto ErrExit;

            cbCur += cb;
            ulSigBlob -= cb;
        }
    }

    // Nothing consumed but not yet counted.
    cb = 0;

ErrExit:
    // We should have consumed all signature blob.  If not, dump the sig in hex.
    //  Also dump in hex if so requested.
    if (m_DumpFilter & dumpMoreHex || ulSigBlob != 0)
    {
        // Did we not consume enough, or try to consume too much?
        if (cb > ulSigBlob)
            WriteLine("\tERROR IN SIGNATURE:  Signature should be larger.");
        else
        if (cb < ulSigBlob)
        {
            VWrite("\tERROR IN SIGNATURE:  Not all of signature blob was consumed.  %d byte(s) remain", ulSigBlob);
            // If it is short, just append it to the end.
            if (ulSigBlob < 4)
            {
                Write(": ");
                for (; ulSigBlob; ++cbCur, --ulSigBlob)
                    VWrite("%02x ", pbSigBlob[cbCur]);
                WriteLine("");
                goto ErrExit2;
            }
            WriteLine("");
        }

        // Any appropriate error message has been issued.  Dump sig in hex, as determined
        //  by error or command line switch.
        cbCur = 0;
        ulSigBlob = ulSigBlobStart;
        char rcNewPrefix[80];
        sprintf_s(rcNewPrefix, 80, "%s\t\tSignature ", preFix);
        DumpHex(rcNewPrefix, pbSigBlob, ulSigBlob, false, 24);
    }
ErrExit2:
    if (FAILED(hr))
        Error("ERROR!! Bad signature blob value!");
    return;
} // void MDInfo::DisplaySignature()


/////////////////////////////////////////////////////////////////////////
// HRESULT GetOneElementType(mdScope tkScope, BYTE *pbSigBlob, ULONG ulSigBlob, ULONG *pcb)
//
// Adds description of element type to the end of buffer -- caller must ensure
// buffer is large enough.
/////////////////////////////////////////////////////////////////////////
HRESULT MDInfo::GetOneElementType(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, ULONG *pcb)
{
    HRESULT     hr = S_OK;              // A result.
    ULONG       cbCur = 0;
    ULONG       cb;
    ULONG       ulData = ELEMENT_TYPE_MAX;
    ULONG       ulTemp;
    int         iTemp = 0;
    mdToken     tk;

    cb = CorSigUncompressData(pbSigBlob, &ulData);
    cbCur += cb;

    // Handle the modifiers.
    if (ulData & ELEMENT_TYPE_MODIFIER)
    {
        if (ulData == ELEMENT_TYPE_SENTINEL)
            IfFailGo(AddToSigBuffer("<ELEMENT_TYPE_SENTINEL>"));
        else if (ulData == ELEMENT_TYPE_PINNED)
            IfFailGo(AddToSigBuffer("PINNED"));
        else
        {
            hr = E_FAIL;
            goto ErrExit;
        }
        if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
            goto ErrExit;
        cbCur += cb;
        goto ErrExit;
    }

    // Handle the underlying element types.
    if (ulData >= ELEMENT_TYPE_MAX)
    {
        hr = E_FAIL;
        goto ErrExit;
    }
    while (ulData == ELEMENT_TYPE_PTR || ulData == ELEMENT_TYPE_BYREF)
    {
        IfFailGo(AddToSigBuffer(" "));
        IfFailGo(AddToSigBuffer(g_szMapElementType[ulData]));
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulData);
        cbCur += cb;
    }
    IfFailGo(AddToSigBuffer(" "));
    IfFailGo(AddToSigBuffer(g_szMapElementType[ulData]));
    if (CorIsPrimitiveType((CorElementType)ulData) ||
        ulData == ELEMENT_TYPE_TYPEDBYREF ||
        ulData == ELEMENT_TYPE_OBJECT ||
        ulData == ELEMENT_TYPE_I ||
        ulData == ELEMENT_TYPE_U)
    {
        // If this is a primitive type, we are done
        goto ErrExit;
    }
    if (ulData == ELEMENT_TYPE_VALUETYPE ||
        ulData == ELEMENT_TYPE_CLASS ||
        ulData == ELEMENT_TYPE_CMOD_REQD ||
        ulData == ELEMENT_TYPE_CMOD_OPT)
    {
        cb = CorSigUncompressToken(&pbSigBlob[cbCur], &tk);
        cbCur += cb;

        // get the name of type ref. Don't care if truncated
        if (TypeFromToken(tk) == mdtTypeDef || TypeFromToken(tk) == mdtTypeRef)
        {
            sprintf_s(m_tempFormatBuffer, STRING_BUFFER_LEN, " %s",TypeDeforRefName(tk, m_szTempBuf, ARRAY_SIZE(m_szTempBuf)));
            IfFailGo(AddToSigBuffer(m_tempFormatBuffer));
        }
        else
        {
            _ASSERTE(TypeFromToken(tk) == mdtTypeSpec);
            sprintf_s(m_tempFormatBuffer, STRING_BUFFER_LEN, " %8x", tk);
            IfFailGo(AddToSigBuffer(m_tempFormatBuffer));
        }
        if (ulData == ELEMENT_TYPE_CMOD_REQD ||
            ulData == ELEMENT_TYPE_CMOD_OPT)
        {
            if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
                goto ErrExit;
            cbCur += cb;
        }

        goto ErrExit;
    }
    if (ulData == ELEMENT_TYPE_SZARRAY)
    {
        // display the base type of SZARRAY
        if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
            goto ErrExit;
        cbCur += cb;
        goto ErrExit;
    }
    // instantiated type
    if (ulData == ELEMENT_TYPE_GENERICINST)
    {
        // display the type constructor
        if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
            goto ErrExit;
        cbCur += cb;
        ULONG numArgs;
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &numArgs);
        cbCur += cb;
        IfFailGo(AddToSigBuffer("<"));

        while (numArgs > 0)
        {
            if (cbCur > ulSigBlob)
                goto ErrExit;
            if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
                goto ErrExit;
            cbCur += cb;
            --numArgs;
            if (numArgs > 0)
                      IfFailGo(AddToSigBuffer(","));
        }
        IfFailGo(AddToSigBuffer(">"));
            goto ErrExit;
    }
    if (ulData == ELEMENT_TYPE_VAR)
    {
        ULONG index;
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &index);
        cbCur += cb;
        sprintf_s(m_tempFormatBuffer, STRING_BUFFER_LEN, "!%d", index);
        IfFailGo(AddToSigBuffer(m_tempFormatBuffer));
        goto ErrExit;
    }
    if (ulData == ELEMENT_TYPE_MVAR)
    {
        ULONG index;
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &index);
        cbCur += cb;
        sprintf_s(m_tempFormatBuffer, STRING_BUFFER_LEN, "!!%d", index);
        IfFailGo(AddToSigBuffer(m_tempFormatBuffer));
        goto ErrExit;
    }
    if (ulData == ELEMENT_TYPE_FNPTR)
    {
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulData);
        cbCur += cb;
        if (ulData & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS)
            IfFailGo(AddToSigBuffer(" explicit"));
        if (ulData & IMAGE_CEE_CS_CALLCONV_HASTHIS)
            IfFailGo(AddToSigBuffer(" hasThis"));

        IfFailGo(AddToSigBuffer(" "));
        IfFailGo(AddToSigBuffer(g_strCalling[ulData & IMAGE_CEE_CS_CALLCONV_MASK]));

            // Get number of args
        ULONG numArgs;
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &numArgs);
        cbCur += cb;

            // do return type
        if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
            goto ErrExit;
        cbCur += cb;

        IfFailGo(AddToSigBuffer("("));
        while (numArgs > 0)
        {
            if (cbCur > ulSigBlob)
                goto ErrExit;
            if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
                goto ErrExit;
            cbCur += cb;
            --numArgs;
            if (numArgs > 0)
                IfFailGo(AddToSigBuffer(","));
        }
        IfFailGo(AddToSigBuffer(" )"));
        goto ErrExit;
    }

    if(ulData != ELEMENT_TYPE_ARRAY) return E_FAIL;

    // display the base type of SDARRAY
    if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
        goto ErrExit;
    cbCur += cb;

    // display the rank of MDARRAY
    cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulData);
    cbCur += cb;
    sprintf_s(m_tempFormatBuffer, STRING_BUFFER_LEN, " %d", ulData);
    IfFailGo(AddToSigBuffer(m_tempFormatBuffer));
    if (ulData == 0)
        // we are done if no rank specified
        goto ErrExit;

    // how many dimensions have size specified?
    cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulData);
    cbCur += cb;
    sprintf_s(m_tempFormatBuffer, STRING_BUFFER_LEN, " %d", ulData);
    IfFailGo(AddToSigBuffer(m_tempFormatBuffer));
    while (ulData)
    {
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulTemp);
        sprintf_s(m_tempFormatBuffer, STRING_BUFFER_LEN, " %d", ulTemp);
        IfFailGo(AddToSigBuffer(m_tempFormatBuffer));
        cbCur += cb;
        ulData--;
    }
    // how many dimensions have lower bounds specified?
    cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulData);
    cbCur += cb;
    sprintf_s(m_tempFormatBuffer, STRING_BUFFER_LEN, " %d", ulData);
    IfFailGo(AddToSigBuffer(m_tempFormatBuffer));
    while (ulData)
    {

        cb = CorSigUncompressSignedInt(&pbSigBlob[cbCur], &iTemp);
        sprintf_s(m_tempFormatBuffer, STRING_BUFFER_LEN, " %d", iTemp);
        IfFailGo(AddToSigBuffer(m_tempFormatBuffer));
        cbCur += cb;
        ulData--;
    }

ErrExit:
    if (cbCur > ulSigBlob)
        hr = E_FAIL;
    *pcb = cbCur;
    return hr;
} // HRESULT MDInfo::GetOneElementType()

// Display the fields of the N/Direct custom value structure.

void MDInfo::DisplayCorNativeLink(COR_NATIVE_LINK *pCorNLnk, const char *preFix)
{
    // Print the LinkType.
    const char *curField = "\tLink Type : ";
    switch(pCorNLnk->m_linkType)
    {
    case nltNone:
        VWriteLine("%s%s%s(%02x)", preFix, curField, "nltNone", pCorNLnk->m_linkType);
        break;
    case nltAnsi:
        VWriteLine("%s%s%s(%02x)", preFix, curField, "nltAnsi", pCorNLnk->m_linkType);
        break;
    case nltUnicode:
        VWriteLine("%s%s%s(%02x)", preFix, curField, "nltUnicode", pCorNLnk->m_linkType);
        break;
    case nltAuto:
        VWriteLine("%s%s%s(%02x)", preFix, curField, "nltAuto", pCorNLnk->m_linkType);
        break;
    default:
        _ASSERTE(!"Invalid Native Link Type!");
    }

    // Print the link flags
    curField = "\tLink Flags : ";
    switch(pCorNLnk->m_flags)
    {
    case nlfNone:
        VWriteLine("%s%s%s(%02x)", preFix, curField, "nlfNone", pCorNLnk->m_flags);
        break;
    case nlfLastError:
        VWriteLine("%s%s%s(%02x)", preFix, curField, "nlfLastError", pCorNLnk->m_flags);
            break;
    default:
        _ASSERTE(!"Invalid Native Link Flags!");
    }

    // Print the entry point.
    WCHAR memRefName[STRING_BUFFER_LEN];
    char memRefNameUtf8[ARRAY_SIZE(memRefName) * MAX_UTF8_CVT];
    HRESULT hr;
    hr = m_pImport->GetMemberRefProps( pCorNLnk->m_entryPoint, NULL, memRefName,
                                    STRING_BUFFER_LEN, NULL, NULL, NULL);
    if (FAILED(hr)) Error("GetMemberRefProps failed.", hr);
    VWriteLine("%s\tEntry Point : %s (0x%08x)",
        preFix, ConvertToUtf8(memRefName, memRefNameUtf8, ARRAY_SIZE(memRefNameUtf8)), pCorNLnk->m_entryPoint);
} // void MDInfo::DisplayCorNativeLink()

// Fills given varaint with value given in pValue and of type in bCPlusTypeFlag
//
// Taken from MetaInternal.cpp
#ifdef FEATURE_COMINTEROP
HRESULT _FillVariant(
    BYTE        bCPlusTypeFlag,
    const void  *pValue,
    ULONG cbValue,
    VARIANT     *pvar)
{
    HRESULT     hr = NOERROR;
    switch (bCPlusTypeFlag)
    {
    case ELEMENT_TYPE_BOOLEAN:
        V_VT(pvar) = VT_BOOL;
        V_BOOL(pvar) = *((BYTE*)pValue); //*((UNALIGNED VARIANT_BOOL *)pValue);
        break;
    case ELEMENT_TYPE_I1:
        V_VT(pvar) = VT_I1;
        V_I1(pvar) = *((CHAR*)pValue);
        break;
    case ELEMENT_TYPE_U1:
        V_VT(pvar) = VT_UI1;
        V_UI1(pvar) = *((BYTE*)pValue);
        break;
    case ELEMENT_TYPE_I2:
        V_VT(pvar) = VT_I2;
        V_I2(pvar) = GET_UNALIGNED_VAL16(pValue);
        break;
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
        V_VT(pvar) = VT_UI2;
        V_UI2(pvar) = GET_UNALIGNED_VAL16(pValue);
        break;
    case ELEMENT_TYPE_I4:
        V_VT(pvar) = VT_I4;
        V_I4(pvar) = GET_UNALIGNED_VAL32(pValue);
        break;
    case ELEMENT_TYPE_U4:
        V_VT(pvar) = VT_UI4;
        V_UI4(pvar) = GET_UNALIGNED_VAL32(pValue);
        break;
    case ELEMENT_TYPE_R4:
        {
            V_VT(pvar) = VT_R4;
            __int32 Value = GET_UNALIGNED_VAL32(pValue);
            V_R4(pvar) = (float &)Value;
        }
        break;
    case ELEMENT_TYPE_R8:
        {
            V_VT(pvar) = VT_R8;
            __int64 Value = GET_UNALIGNED_VAL64(pValue);
            V_R8(pvar) = (double &) Value;
        }

        break;
    case ELEMENT_TYPE_STRING:
        {
            V_VT(pvar) = VT_BSTR;
            WCHAR *TempString;;
#if BIGENDIAN
            TempString = (WCHAR *)alloca(cbValue);
            memcpy(TempString, pValue, cbValue);
            SwapStringLength(TempString, cbValue/sizeof(WCHAR));
#else
            TempString = (WCHAR *)pValue;
#endif
            // allocated bstr here
            V_BSTR(pvar) = ::SysAllocStringLen((LPWSTR)TempString, cbValue/sizeof(WCHAR));
            if (V_BSTR(pvar) == NULL)
                hr = E_OUTOFMEMORY;
        }
        break;
    case ELEMENT_TYPE_CLASS:
        V_VT(pvar) = VT_UNKNOWN;
        V_UNKNOWN(pvar) = NULL;
        // _ASSERTE( GET_UNALIGNED_VAL32(pValue) == 0);
        break;
    case ELEMENT_TYPE_I8:
        V_VT(pvar) = VT_I8;
        V_CY(pvar).int64 = GET_UNALIGNED_VAL64(pValue);
        break;
    case ELEMENT_TYPE_U8:
        V_VT(pvar) = VT_UI8;
        V_CY(pvar).int64 = GET_UNALIGNED_VAL64(pValue);
        break;
    case ELEMENT_TYPE_VOID:
        V_VT(pvar) = VT_EMPTY;
        break;
    default:
        _ASSERTE(!"bad constant value type!");
    }

    return hr;
} // HRESULT _FillVariant()
#endif // FEATURE_COMINTEROP

void MDInfo::DisplayAssembly()
{
    if (m_pAssemblyImport)
    {
        DisplayAssemblyInfo();
        DisplayAssemblyRefs();
        DisplayFiles();
        DisplayExportedTypes();
        DisplayManifestResources();
    }
} // void MDInfo::DisplayAssembly()

void MDInfo::DisplayAssemblyInfo()
{
    HRESULT         hr;
    mdAssembly      mda;
    const BYTE      *pbPublicKey;
    ULONG           cbPublicKey;
    ULONG           ulHashAlgId;
    WCHAR           szName[STRING_BUFFER_LEN];
    char            szNameUtf8[ARRAY_SIZE(szName) * MAX_UTF8_CVT];
    ASSEMBLYMETADATA MetaData;
    DWORD           dwFlags;

    hr = m_pAssemblyImport->GetAssemblyFromScope(&mda);
    if (hr == CLDB_E_RECORD_NOTFOUND)
        return;
    else if (FAILED(hr)) Error("GetAssemblyFromScope() failed.", hr);

    // Get the required sizes for the arrays of locales, processors etc.
    ZeroMemory(&MetaData, sizeof(ASSEMBLYMETADATA));
    hr = m_pAssemblyImport->GetAssemblyProps(mda,
                                             NULL, NULL,    // Public Key.
                                             NULL,          // Hash Algorithm.
                                             NULL, 0, NULL, // Name.
                                             &MetaData,
                                             NULL);         // Flags.
    if (FAILED(hr)) Error("GetAssemblyProps() failed.", hr);

    // Allocate space for the arrays in the ASSEMBLYMETADATA structure.
    if (MetaData.cbLocale)
        MetaData.szLocale = new WCHAR[MetaData.cbLocale];
    if (MetaData.ulProcessor)
        MetaData.rProcessor = new DWORD[MetaData.ulProcessor];
    if (MetaData.ulOS)
        MetaData.rOS = new OSINFO[MetaData.ulOS];

    hr = m_pAssemblyImport->GetAssemblyProps(mda,
                                             (const void **)&pbPublicKey, &cbPublicKey,
                                             &ulHashAlgId,
                                             szName, STRING_BUFFER_LEN, NULL,
                                             &MetaData,
                                             &dwFlags);
    if (FAILED(hr)) Error("GetAssemblyProps() failed.", hr);
    WriteLine("Assembly");
    WriteLine("-------------------------------------------------------");
    VWriteLine("\tToken: 0x%08x", mda);
    VWriteLine("\tName : %s", ConvertToUtf8(szName, szNameUtf8, ARRAY_SIZE(szNameUtf8)));
    DumpHex("\tPublic Key    ", pbPublicKey, cbPublicKey, false, 24);
    VWriteLine("\tHash Algorithm : 0x%08x", ulHashAlgId);
    DisplayASSEMBLYMETADATA(&MetaData);
    if(MetaData.szLocale) delete [] MetaData.szLocale;
    if(MetaData.rProcessor) delete [] MetaData.rProcessor;
    if(MetaData.rOS) delete [] MetaData.rOS;

    char szTempBuf[STRING_BUFFER_LEN];
    DWORD flags = dwFlags;

    szTempBuf[0] = 0;
    ISFLAG(Af, PublicKey);
    ISFLAG(Af, Retargetable);
    ISFLAG(AfContentType_, WindowsRuntime);

    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    VWriteLine("\tFlags : %s (%08x)", szTempBuf, dwFlags);
    DisplayCustomAttributes(mda, "\t");
    DisplayPermissions(mda, "\t");
    WriteLine("");
}   // void MDInfo::DisplayAssemblyInfo()

void MDInfo::DisplayAssemblyRefs()
{
    HCORENUM        assemblyRefEnum = NULL;
    mdAssemblyRef   AssemblyRefs[ENUM_BUFFER_SIZE];
    ULONG           count;
    ULONG           totalCount = 1;
    HRESULT         hr;

    while (SUCCEEDED(hr = m_pAssemblyImport->EnumAssemblyRefs( &assemblyRefEnum,
                             AssemblyRefs, ARRAY_SIZE(AssemblyRefs), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("AssemblyRef #%d (%08x)", totalCount, AssemblyRefs[i]);
            WriteLine("-------------------------------------------------------");
            DisplayAssemblyRefInfo(AssemblyRefs[i]);
            WriteLine("");
        }
    }
    m_pAssemblyImport->CloseEnum(assemblyRefEnum);
}   // void MDInfo::DisplayAssemblyRefs()

void MDInfo::DisplayAssemblyRefInfo(mdAssemblyRef inAssemblyRef)
{
    HRESULT         hr;
    const BYTE      *pbPublicKeyOrToken;
    ULONG           cbPublicKeyOrToken;
    WCHAR           szName[STRING_BUFFER_LEN];
    char            szNameUtf8[ARRAY_SIZE(szName) * MAX_UTF8_CVT];
    ASSEMBLYMETADATA MetaData;
    const BYTE      *pbHashValue;
    ULONG           cbHashValue;
    DWORD           dwFlags;

    VWriteLine("\tToken: 0x%08x", inAssemblyRef);

    // Get sizes for the arrays in the ASSEMBLYMETADATA structure.
    ZeroMemory(&MetaData, sizeof(ASSEMBLYMETADATA));
    hr = m_pAssemblyImport->GetAssemblyRefProps(inAssemblyRef,
                                             NULL, NULL,    // Public Key or Token.
                                             NULL, 0, NULL, // Name.
                                             &MetaData,
                                             NULL, NULL,    // HashValue.
                                             NULL);         // Flags.
    if (FAILED(hr)) Error("GetAssemblyRefProps() failed.", hr);

    // Allocate space for the arrays in the ASSEMBLYMETADATA structure.
    if (MetaData.cbLocale)
        MetaData.szLocale = new WCHAR[MetaData.cbLocale];
    if (MetaData.ulProcessor)
        MetaData.rProcessor = new DWORD[MetaData.ulProcessor];
    if (MetaData.ulOS)
        MetaData.rOS = new OSINFO[MetaData.ulOS];

    hr = m_pAssemblyImport->GetAssemblyRefProps(inAssemblyRef,
                                             (const void **)&pbPublicKeyOrToken, &cbPublicKeyOrToken,
                                             szName, STRING_BUFFER_LEN, NULL,
                                             &MetaData,
                                             (const void **)&pbHashValue, &cbHashValue,
                                             &dwFlags);
    if (FAILED(hr)) Error("GetAssemblyRefProps() failed.", hr);

    DumpHex("\tPublic Key or Token", pbPublicKeyOrToken, cbPublicKeyOrToken, false, 24);
    VWriteLine("\tName: %s", ConvertToUtf8(szName, szNameUtf8, ARRAY_SIZE(szNameUtf8)));
    DisplayASSEMBLYMETADATA(&MetaData);
    if(MetaData.szLocale) delete [] MetaData.szLocale;
    if(MetaData.rProcessor) delete [] MetaData.rProcessor;
    if(MetaData.rOS) delete [] MetaData.rOS;
    DumpHex("\tHashValue Blob", pbHashValue, cbHashValue, false, 24);

    char szTempBuf[STRING_BUFFER_LEN];
    DWORD flags = dwFlags;

    szTempBuf[0] = 0;
    ISFLAG(Af, PublicKey);
    ISFLAG(Af, Retargetable);
    ISFLAG(AfContentType_, WindowsRuntime);
#if 0
    ISFLAG(Af, LegacyLibrary);
    ISFLAG(Af, LegacyPlatform);
    ISFLAG(Af, Library);
    ISFLAG(Af, Platform);
#endif
    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    VWriteLine("\tFlags: %s (%08x)", szTempBuf, dwFlags);
    DisplayCustomAttributes(inAssemblyRef, "\t");
    WriteLine("");
}   // void MDInfo::DisplayAssemblyRefInfo()

void MDInfo::DisplayFiles()
{
    HCORENUM        fileEnum = NULL;
    mdFile          Files[ENUM_BUFFER_SIZE];
    ULONG           count;
    ULONG           totalCount = 1;
    HRESULT         hr;

    while (SUCCEEDED(hr = m_pAssemblyImport->EnumFiles( &fileEnum,
                             Files, ARRAY_SIZE(Files), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("File #%d (%08x)", totalCount, Files[i]);
            WriteLine("-------------------------------------------------------");
            DisplayFileInfo(Files[i]);
            WriteLine("");
        }
    }
    m_pAssemblyImport->CloseEnum(fileEnum);
}   // void MDInfo::DisplayFiles()

void MDInfo::DisplayFileInfo(mdFile inFile)
{
    HRESULT         hr;
    WCHAR           szName[STRING_BUFFER_LEN];
    char            szNameUtf8[ARRAY_SIZE(szName) * MAX_UTF8_CVT];
    const BYTE      *pbHashValue;
    ULONG           cbHashValue;
    DWORD           dwFlags;

    VWriteLine("\tToken: 0x%08x", inFile);

    hr = m_pAssemblyImport->GetFileProps(inFile,
                                         szName, STRING_BUFFER_LEN, NULL,
                                         (const void **)&pbHashValue, &cbHashValue,
                                         &dwFlags);
    if (FAILED(hr)) Error("GetFileProps() failed.", hr);
    VWriteLine("\tName : %s", ConvertToUtf8(szName, szNameUtf8, ARRAY_SIZE(szNameUtf8)));
    DumpHex("\tHashValue Blob ", pbHashValue, cbHashValue, false, 24);

    char szTempBuf[STRING_BUFFER_LEN];
    DWORD flags = dwFlags;

    szTempBuf[0] = 0;
    ISFLAG(Ff, ContainsMetaData);
    ISFLAG(Ff, ContainsNoMetaData);
    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    VWriteLine("\tFlags : %s (%08x)", szTempBuf, dwFlags);
    DisplayCustomAttributes(inFile, "\t");
    WriteLine("");

}   // MDInfo::DisplayFileInfo()

void MDInfo::DisplayExportedTypes()
{
    HCORENUM        comTypeEnum = NULL;
    mdExportedType       ExportedTypes[ENUM_BUFFER_SIZE];
    ULONG           count;
    ULONG           totalCount = 1;
    HRESULT         hr;

    while (SUCCEEDED(hr = m_pAssemblyImport->EnumExportedTypes( &comTypeEnum,
                             ExportedTypes, ARRAY_SIZE(ExportedTypes), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("ExportedType #%d (%08x)", totalCount, ExportedTypes[i]);
            WriteLine("-------------------------------------------------------");
            DisplayExportedTypeInfo(ExportedTypes[i]);
            WriteLine("");
        }
    }
    m_pAssemblyImport->CloseEnum(comTypeEnum);
}   // void MDInfo::DisplayExportedTypes()

void MDInfo::DisplayExportedTypeInfo(mdExportedType inExportedType)
{
    HRESULT         hr;
    WCHAR           szName[STRING_BUFFER_LEN];
    char            szNameUtf8[ARRAY_SIZE(szName) * MAX_UTF8_CVT];
    mdToken         tkImplementation;
    mdTypeDef       tkTypeDef;
    DWORD           dwFlags;
    char            szTempBuf[STRING_BUFFER_LEN];

    VWriteLine("\tToken: 0x%08x", inExportedType);

    hr = m_pAssemblyImport->GetExportedTypeProps(inExportedType,
                                            szName, STRING_BUFFER_LEN, NULL,
                                            &tkImplementation,
                                            &tkTypeDef,
                                            &dwFlags);
    if (FAILED(hr)) Error("GetExportedTypeProps() failed.", hr);
    VWriteLine("\tName: %s", ConvertToUtf8(szName, szNameUtf8, ARRAY_SIZE(szNameUtf8)));
    VWriteLine("\tImplementation token: 0x%08x", tkImplementation);
    VWriteLine("\tTypeDef token: 0x%08x", tkTypeDef);
    VWriteLine("\tFlags     : %s (%08x)",ClassFlags(dwFlags, szTempBuf), dwFlags);
    DisplayCustomAttributes(inExportedType, "\t");
    WriteLine("");
}   // void MDInfo::DisplayExportedTypeInfo()

void MDInfo::DisplayManifestResources()
{
    HCORENUM        manifestResourceEnum = NULL;
    mdManifestResource ManifestResources[ENUM_BUFFER_SIZE];
    ULONG           count;
    ULONG           totalCount = 1;
    HRESULT         hr;

    while (SUCCEEDED(hr = m_pAssemblyImport->EnumManifestResources( &manifestResourceEnum,
                             ManifestResources, ARRAY_SIZE(ManifestResources), &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            VWriteLine("ManifestResource #%d (%08x)", totalCount, ManifestResources[i]);
            WriteLine("-------------------------------------------------------");
            DisplayManifestResourceInfo(ManifestResources[i]);
            WriteLine("");
        }
    }
    m_pAssemblyImport->CloseEnum(manifestResourceEnum);
}   // void MDInfo::DisplayManifestResources()

void MDInfo::DisplayManifestResourceInfo(mdManifestResource inManifestResource)
{
    HRESULT         hr;
    WCHAR           szName[STRING_BUFFER_LEN];
    char            szNameUtf8[ARRAY_SIZE(szName) * MAX_UTF8_CVT];
    mdToken         tkImplementation;
    DWORD           dwOffset;
    DWORD           dwFlags;

    VWriteLine("\tToken: 0x%08x", inManifestResource);

    hr = m_pAssemblyImport->GetManifestResourceProps(inManifestResource,
                                                     szName, STRING_BUFFER_LEN, NULL,
                                                     &tkImplementation,
                                                     &dwOffset,
                                                     &dwFlags);
    if (FAILED(hr)) Error("GetManifestResourceProps() failed.", hr);
    VWriteLine("Name: %s", ConvertToUtf8(szName, szNameUtf8, ARRAY_SIZE(szNameUtf8)));
    VWriteLine("Implementation token: 0x%08x", tkImplementation);
    VWriteLine("Offset: 0x%08x", dwOffset);

    char szTempBuf[STRING_BUFFER_LEN];
    DWORD flags = dwFlags;

    szTempBuf[0] = 0;
    ISFLAG(Mr, Public);
    ISFLAG(Mr, Private);
    if (!*szTempBuf)
        strcpy_s(szTempBuf, STRING_BUFFER_LEN, "[none]");

    VWriteLine("\tFlags: %s (%08x)", szTempBuf, dwFlags);
    DisplayCustomAttributes(inManifestResource, "\t");
    WriteLine("");
}   // void MDInfo::DisplayManifestResourceInfo()

void MDInfo::DisplayASSEMBLYMETADATA(ASSEMBLYMETADATA *pMetaData)
{
    ULONG           i;

    char            szLocaleUtf8[STRING_BUFFER_LEN];
    VWriteLine("\tVersion: %d.%d.%d.%d", pMetaData->usMajorVersion, pMetaData->usMinorVersion, pMetaData->usBuildNumber, pMetaData->usRevisionNumber);
    VWriteLine("\tMajor Version: 0x%08x", pMetaData->usMajorVersion);
    VWriteLine("\tMinor Version: 0x%08x", pMetaData->usMinorVersion);
    VWriteLine("\tBuild Number: 0x%08x", pMetaData->usBuildNumber);
    VWriteLine("\tRevision Number: 0x%08x", pMetaData->usRevisionNumber);
    VWriteLine("\tLocale: %s", pMetaData->cbLocale ? ConvertToUtf8(pMetaData->szLocale, szLocaleUtf8, ARRAY_SIZE(szLocaleUtf8)) : "<null>");
    for (i = 0; i < pMetaData->ulProcessor; i++)
        VWriteLine("\tProcessor #%ld: 0x%08x", i+1, pMetaData->rProcessor[i]);
    for (i = 0; i < pMetaData->ulOS; i++)
    {
        VWriteLine("\tOS #%ld:", i+1);
        VWriteLine("\t\tOS Platform ID: 0x%08x", pMetaData->rOS[i].dwOSPlatformId);
        VWriteLine("\t\tOS Major Version: 0x%08x", pMetaData->rOS[i].dwOSMajorVersion);
        VWriteLine("\t\tOS Minor Version: 0x%08x", pMetaData->rOS[i].dwOSMinorVersion);
    }
}   // void MDInfo::DisplayASSEMBLYMETADATA()

void MDInfo::DisplayUserStrings()
{
    HCORENUM    stringEnum = NULL;      // string enumerator.
    mdString    Strings[ENUM_BUFFER_SIZE]; // String tokens from enumerator.
    CQuickArray<WCHAR> rUserString;     // Buffer to receive string.
    WCHAR       *szUserString;          // Working pointer into buffer.
    ULONG       chUserString;           // Size of user string.
    CQuickArray<char> rcBuf;            // Buffer to hold the BLOB version of the string.
    char        *szBuf;                 // Working pointer into buffer.
    ULONG       chBuf;                  // Saved size of the user string.
    ULONG       count;                  // Items returned from enumerator.
    ULONG       totalCount = 1;         // Running count of strings.
    bool        bUnprint = false;       // Is an unprintable character found?
    HRESULT     hr;                     // A result.
    while (SUCCEEDED(hr = m_pImport->EnumUserStrings( &stringEnum,
                             Strings, ARRAY_SIZE(Strings), &count)) &&
            count > 0)
    {
        if (totalCount == 1)
        {   // If only one, it is the NULL string, so don't print it.
            WriteLine("User Strings");
            WriteLine("-------------------------------------------------------");
        }
        for (ULONG i = 0; i < count; i++, totalCount++)
        {
            do { // Try to get the string into the existing buffer.
                hr = m_pImport->GetUserString( Strings[i], rUserString.Ptr(),(ULONG32)rUserString.MaxSize(), &chUserString);
                if (hr == CLDB_S_TRUNCATION)
                {   // Buffer wasn't big enough, try to enlarge it.
                    if (FAILED(rUserString.ReSizeNoThrow(chUserString)))
                        Error("malloc failed.", E_OUTOFMEMORY);
                    continue;
                }
            } while (hr == CLDB_S_TRUNCATION);
            if (FAILED(hr)) Error("GetUserString failed.", hr);

            szUserString = rUserString.Ptr();
            chBuf = chUserString;

            VWrite("%08x : (%2d) L\"", Strings[i], chUserString);
            for (ULONG j=0; j<chUserString; j++)
            {
                switch (*szUserString)
                {
                case 0:
                    Write("\\0"); break;
                case L'\r':
                    Write("\\r"); break;
                case L'\n':
                    Write("\\n"); break;
                case L'\t':
                    Write("\\t"); break;
                default:
                    if (iswprint(*szUserString))
                        VWrite("%lc", *szUserString);
                    else
                    {
                        bUnprint = true;
                        Write(".");
                    }
                    break;
                }
                ++szUserString;
                if((j>0)&&((j&0x7F)==0)) WriteLine("");
            }
            WriteLine("\"");

            // Print the user string as a blob if an unprintable character is found.
            if (bUnprint)
            {
                bUnprint = false;
                szUserString = rUserString.Ptr();
                if (FAILED(hr = rcBuf.ReSizeNoThrow(81))) //(chBuf * 5 + 1);
                    Error("ReSize failed.", hr);
                szBuf = rcBuf.Ptr();
                ULONG j,k;
                WriteLine("\t\tUser string has unprintables, hex format below:");
                for (j = 0,k=0; j < chBuf; j++)
                {
                    sprintf_s (&szBuf[k*5], 81, "%04x ", szUserString[j]);
                    k++;
                    if((k==16)||(j == (chBuf-1)))
                    {
                        szBuf[k*5] = '\0';
                        VWriteLine("\t\t%s", szBuf);
                        k=0;
                    }
                }
            }
        }
    }
    if (stringEnum)
        m_pImport->CloseEnum(stringEnum);
}   // void MDInfo::DisplayUserStrings()

void MDInfo::DisplayUnsatInfo()
{
    HRESULT     hr = S_OK;

    HCORENUM henum = 0;
    mdToken  tk;
    ULONG cMethods;

    Write("\nUnresolved Externals\n");
    Write("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");

    while ( (hr = m_pImport->EnumUnresolvedMethods(
        &henum,
        &tk,
        1,
        &cMethods)) == S_OK && cMethods )
    {
        if ( TypeFromToken(tk) == mdtMethodDef )
        {
            // a method definition without implementation
            DisplayMethodInfo( tk );
        }
        else if ( TypeFromToken(tk) == mdtMemberRef )
        {
            // an unresolved MemberRef to a global function
            DisplayMemberRefInfo( tk, "" );
        }
        else
        {
            _ASSERTE(!"Unknown token kind!");
        }
    }
    m_pImport->CloseEnum(henum);
} // void MDInfo::DisplayUnsatInfo()

//*******************************************************************************
// This code is used for debugging purposes only.  This will just print out the
// entire database.
//*******************************************************************************
const char *MDInfo::DumpRawNameOfType(ULONG iType)
{
    if (iType <= iRidMax)
    {
        const char *pNameTable;
        m_pTables->GetTableInfo(iType, 0,0,0,0, &pNameTable);
        return pNameTable;
    }
    else
    // Is the field a coded token?
    if (iType <= iCodedTokenMax)
    {
        int iCdTkn = iType - iCodedToken;
        const char *pNameCdTkn;
        m_pTables->GetCodedTokenInfo(iCdTkn, 0,0, &pNameCdTkn);
        return pNameCdTkn;
    }

    // Fixed type.
    switch (iType)
    {
    case iBYTE:
        return "BYTE";
    case iSHORT:
        return "short";
    case iUSHORT:
        return "USHORT";
    case iLONG:
        return "long";
    case iULONG:
        return "ULONG";
    case iSTRING:
        return "string";
    case iGUID:
        return "GUID";
    case iBLOB:
        return "blob";
    }
    // default:
    static char buf[30];
    sprintf_s(buf, 30, "unknown type 0x%02x", iType);
    return buf;
} // const char *MDInfo::DumpRawNameOfType()

void MDInfo::DumpRawCol(ULONG ixTbl, ULONG ixCol, ULONG rid, bool bStats)
{
    ULONG       ulType;                 // Type of a column.
    ULONG       ulVal;                  // Value of a column.
    LPCUTF8     pString;                // Pointer to a string.
    const void  *pBlob;                 // Pointer to a blob.
    ULONG       cb;                     // Size of something.

    m_pTables->GetColumn(ixTbl, ixCol, rid, &ulVal);
    m_pTables->GetColumnInfo(ixTbl, ixCol, 0, 0, &ulType, 0);

    if (ulType <= iRidMax)
    {
        const char *pNameTable;
        m_pTables->GetTableInfo(ulType, 0,0,0,0, &pNameTable);
        VWrite("%s[%x]", pNameTable, ulVal);
    }
    else
    // Is the field a coded token?
    if (ulType <= iCodedTokenMax)
    {
        int iCdTkn = ulType - iCodedToken;
        const char *pNameCdTkn;
        m_pTables->GetCodedTokenInfo(iCdTkn, 0,0, &pNameCdTkn);
        VWrite("%s[%08x]", pNameCdTkn, ulVal);
    }
    else
    {
        // Fixed type.
        switch (ulType)
        {
        case iBYTE:
            VWrite("%02x", ulVal);
            break;
        case iSHORT:
        case iUSHORT:
            VWrite("%04x", ulVal);
            break;
        case iLONG:
        case iULONG:
            VWrite("%08x", ulVal);
            break;
        case iSTRING:
            if (ulVal && (m_DumpFilter & dumpNames))
            {
                m_pTables->GetString(ulVal, &pString);
                VWrite("(%x)\"%s\"", ulVal, pString);
            }
            else
                VWrite("string#%x", ulVal);
            if (bStats && ulVal)
            {
                m_pTables->GetString(ulVal, &pString);
                cb = (ULONG) strlen(pString) + 1;
                VWrite("(%d)", cb);
            }
            break;
        case iGUID:
            VWrite("guid#%x", ulVal);
            if (bStats && ulVal)
            {
                VWrite("(16)");
            }
            break;
        case iBLOB:
            VWrite("blob#%x", ulVal);
            if (bStats && ulVal)
            {
                m_pTables->GetBlob(ulVal, &cb, &pBlob);
                cb += 1;
                if (cb > 128)
                    cb += 1;
                if (cb > 16535)
                    cb += 1;
                VWrite("(%d)", cb);
            }
            break;
        default:
            VWrite("unknown type 0x%04x", ulVal);
            break;
        }
    }
} // void MDInfo::DumpRawCol()

ULONG MDInfo::DumpRawColStats(ULONG ixTbl, ULONG ixCol, ULONG cRows)
{
    ULONG rslt = 0;
    ULONG       ulType;                 // Type of a column.
    ULONG       ulVal;                  // Value of a column.
    LPCUTF8     pString;                // Pointer to a string.
    const void  *pBlob;                 // Pointer to a blob.
    ULONG       cb;                     // Size of something.

    m_pTables->GetColumnInfo(ixTbl, ixCol, 0, 0, &ulType, 0);

    if (IsHeapType(ulType))
    {
        for (ULONG rid=1; rid<=cRows; ++rid)
        {
            m_pTables->GetColumn(ixTbl, ixCol, rid, &ulVal);
            // Fixed type.
            switch (ulType)
            {
            case iSTRING:
                if (ulVal)
                {
                    m_pTables->GetString(ulVal, &pString);
                    cb = (ULONG) strlen(pString);
                    rslt += cb + 1;
                }
                break;
            case iGUID:
                if (ulVal)
                    rslt += 16;
                break;
            case iBLOB:
                if (ulVal)
                {
                    m_pTables->GetBlob(ulVal, &cb, &pBlob);
                    rslt += cb + 1;
                    if (cb > 128)
                        rslt += 1;
                    if (cb > 16535)
                        rslt += 1;
                }
                break;
            default:
                break;
            }
        }
    }
    return rslt;
} // ULONG MDInfo::DumpRawColStats()

int MDInfo::DumpHex(
    const char  *szPrefix,              // String prefix for first line.
    const void  *pvData,                // The data to print.
    ULONG       cbData,                 // Bytes of data to print.
    int         bText,                  // If true, also dump text.
    ULONG       nLine)                  // Bytes per line to print.
{
    const BYTE  *pbData = static_cast<const BYTE*>(pvData);
    ULONG       i;                      // Loop control.
    ULONG       nPrint;                 // Number to print in an iteration.
    ULONG       nSpace;                 // Spacing calculations.
    ULONG       nPrefix;                // Size of the prefix.
    ULONG       nLines=0;               // Number of lines printed.
    const char  *pPrefix;               // For counting spaces in the prefix.

    // Round down to 8 characters.
    nLine = nLine & ~0x7;

    for (nPrefix=0, pPrefix=szPrefix; *pPrefix; ++pPrefix)
    {
        if (*pPrefix == '\t')
            nPrefix = (nPrefix + 8) & ~7;
        else
            ++nPrefix;
    }
    //nPrefix = strlen(szPrefix);
    do
    {   // Write the line prefix.
        if (szPrefix)
            VWrite("%s:", szPrefix);
        else
            VWrite("%*s:", nPrefix, "");
        szPrefix = 0;
        ++nLines;

        // Calculate spacing.
        nPrint = std::min(cbData, nLine);
        nSpace = nLine - nPrint;

            // dump in hex.
        for(i=0; i<nPrint; i++)
            {
            if ((i&7) == 0)
                    Write(" ");
            VWrite("%02x ", pbData[i]);
            }
        if (bText)
        {
            // Space out to the text spot.
            if (nSpace)
                VWrite("%*s", nSpace*3+nSpace/8, "");
            // Dump in text.
            Write(">");
            for(i=0; i<nPrint; i++)
                VWrite("%c", (isprint(pbData[i])) ? pbData[i] : ' ');
            // Space out the text, and finish the line.
            VWrite("%*s<", nSpace, "");
        }
        VWriteLine("");

        // Next data to print.
        cbData -= nPrint;
        pbData += nPrint;
        }
    while (cbData > 0);

    return nLines;
} // int MDInfo::DumpHex()

void MDInfo::DumpRawHeaps()
{
    HRESULT     hr;                     // A result.
    ULONG       ulSize;                 // Bytes in a heap.
    const BYTE  *pData;                 // Pointer to a blob.
    ULONG       cbData;                 // Size of a blob.
    ULONG       oData;                  // Offset of current blob.
    char        rcPrefix[30];           // To format line prefix.

    m_pTables->GetBlobHeapSize(&ulSize);
    VWriteLine("");
    VWriteLine("Blob Heap:  %d(%#x) bytes", ulSize,ulSize);
    oData = 0;
    do
    {
        m_pTables->GetBlob(oData, &cbData, (const void**)&pData);
        sprintf_s(rcPrefix, 30, "%5x,%-2x", oData, cbData);
        DumpHex(rcPrefix, pData, cbData);
        hr = m_pTables->GetNextBlob(oData, &oData);
    }
    while (hr == S_OK);

    m_pTables->GetStringHeapSize(&ulSize);
    VWriteLine("");
    VWriteLine("String Heap:  %d(%#x) bytes", ulSize,ulSize);
    oData = 0;
    const char *pString;
    do
    {
        m_pTables->GetString(oData, &pString);
        if (m_DumpFilter & dumpMoreHex)
        {
            sprintf_s(rcPrefix, 30, "%08x", oData);
            DumpHex(rcPrefix, pString, (ULONG)strlen(pString)+1);
        }
        else
        if (*pString != 0)
            VWrite("%08x: %s\n", oData, pString);
        hr = m_pTables->GetNextString(oData, &oData);
    }
    while (hr == S_OK);
    VWriteLine("");

    DisplayUserStrings();

} // void MDInfo::DumpRawHeaps()


void MDInfo::DumpRaw(int iDump, bool bunused)
{
    ULONG       cTables;                // Tables in the database.
    ULONG       cCols;                  // Columns in a table.
    ULONG       cRows;                  // Rows in a table.
    ULONG       cbRow;                  // Bytes in a row of a table.
    ULONG       iKey;                   // Key column of a table.
    const char  *pNameTable;            // Name of a table.
    ULONG       oCol;                   // Offset of a column.
    ULONG       cbCol;                  // Size of a column.
    ULONG       ulType;                 // Type of a column.
    const char  *pNameColumn;           // Name of a column.
    ULONG       ulSize;

    // Heaps is easy -- there is a specific bit for that.
    bool        bStats = (m_DumpFilter & dumpStats) != 0;
    // Rows are harder.  Was there something else that limited data?
    BOOL        bRows = (m_DumpFilter & (dumpSchema | dumpHeader)) == 0;
    BOOL        bSchema = bRows || (m_DumpFilter & dumpSchema);
    // (m_DumpFilter & (dumpSchema | dumpHeader | dumpCSV | dumpRaw | dumpStats | dumpRawHeaps))

    if (m_pTables2)
    {
        // Get the raw metadata header.
        const BYTE *pbData = NULL;
        const BYTE *pbStream = NULL;            // One of the stream.s
        const BYTE *pbMd = NULL;                // The metadata stream.
        ULONG cbData = 0;
        ULONG cbStream = 0;                     // One of the streams.
        ULONG cbMd = 0;                         // The metadata stream.
        const char *pName;
        HRESULT hr = S_OK;
        ULONG ix;

        m_pTables2->GetMetaDataStorage((const void**)&pbData, &cbData);

        // Per the ECMA spec, the section data looks like this:
        struct MDSTORAGESIGNATURE
        {
            ULONG       lSignature;             // "Magic" signature.
            USHORT      iMajorVer;              // Major file version.
            USHORT      iMinorVer;              // Minor file version.
            ULONG       iExtraData;             // Offset to next structure of information
            ULONG       iVersionString;         // Length of version string
            BYTE        pVersion[0];            // Version string
        };
        struct MDSTORAGEHEADER
        {
            BYTE        fFlags;                 // STGHDR_xxx flags.
            BYTE        pad;
            USHORT      iStreams;               // How many streams are there.
        };
        const MDSTORAGESIGNATURE *pStorage = (const MDSTORAGESIGNATURE *) pbData;
        const MDSTORAGEHEADER *pSHeader = (const MDSTORAGEHEADER *)(pbData + sizeof(MDSTORAGESIGNATURE) + pStorage->iVersionString);

        VWriteLine("Metadata section: 0x%08x, version: %d.%d, extra: %d, version len: %d, version: %s", pStorage->lSignature, pStorage->iMajorVer, pStorage->iMinorVer, pStorage->iExtraData, pStorage->iVersionString, pStorage->pVersion);
        VWriteLine("           flags: 0x%02x, streams: %d", pSHeader->fFlags, pSHeader->iStreams);
        if (m_DumpFilter & dumpMoreHex)
        {
            const BYTE *pbEnd = pbData;
            ULONG cb = sizeof(MDSTORAGESIGNATURE) + pStorage->iVersionString + sizeof(MDSTORAGEHEADER);
            hr = m_pTables2->GetMetaDataStreamInfo(0, &pName, (const void**)&pbEnd, &cbStream);
            if (hr == S_OK)
                cb = (ULONG)(pbEnd - pbData);
            DumpHex("        ", pbData, cb);
        }

        for (ix=0; hr == S_OK; ++ix)
        {
            hr = m_pTables2->GetMetaDataStreamInfo(ix, &pName, (const void**)&pbStream, &cbStream);
            if (hr != S_OK)
                break;
            if (strcmp(pName, "#~") == 0 || strcmp(pName, "#-") == 0)
            {
                pbMd = pbStream;
                cbMd = cbStream;
            }

            VWriteLine("Stream %d: name: %s, size %d", ix, pName, cbStream);
            // hex for individual stream headers in metadata section dump.  hex for
            //  the streams themselves distributed throughout the dump.
        }

        if (pbMd)
        {
            // Per ECMA, the metadata header looks like this:
            struct MD
            {
                ULONG       m_ulReserved;           // Reserved, must be zero.
                BYTE        m_major;                // Version numbers.
                BYTE        m_minor;
                BYTE        m_heaps;                // Bits for heap sizes.
                BYTE        m_rid;                  // log-base-2 of largest rid.
                unsigned __int64    m_maskvalid;            // Bit mask of present table counts.
                unsigned __int64    m_sorted;               // Bit mask of sorted tables.            };
            };

            const MD *pMd;
            pMd = (const MD *)pbMd;

            VWriteLine("Metadata header: %d.%d, heaps: 0x%02x, rid: 0x%02x, valid: 0x%016I64x, sorted: 0x%016I64x",
                       pMd->m_major, pMd->m_minor, pMd->m_heaps, pMd->m_rid,
                       (ULONGLONG)GET_UNALIGNED_VAL64(&(pMd->m_maskvalid)),
                       (ULONGLONG)GET_UNALIGNED_VAL64(&(pMd->m_sorted)));

            if (m_DumpFilter & dumpMoreHex)
            {
                DumpHex("        ", pbMd, sizeof(MD));
            }
        }
        VWriteLine("");
    }

    m_pTables->GetNumTables(&cTables);

    m_pTables->GetStringHeapSize(&ulSize);
    VWrite("Strings: %d(%#x)", ulSize, ulSize);
    m_pTables->GetBlobHeapSize(&ulSize);
    VWrite(", Blobs: %d(%#x)", ulSize, ulSize);
    m_pTables->GetGuidHeapSize(&ulSize);
    VWrite(", Guids: %d(%#x)", ulSize, ulSize);
    m_pTables->GetUserStringHeapSize(&ulSize);
    VWriteLine(", User strings: %d(%#x)", ulSize, ulSize);

    for (ULONG ixTbl = 0; ixTbl < cTables; ++ixTbl)
    {
        m_pTables->GetTableInfo(ixTbl, &cbRow, &cRows, &cCols, &iKey, &pNameTable);

        if (bRows) // when dumping rows, print a break between row data and schema
            VWriteLine("=================================================");
        VWriteLine("%2d(%#x): %-20s cRecs:%5d(%#x), cbRec:%3d(%#x), cbTable:%6d(%#x)",
            ixTbl, ixTbl, pNameTable, cRows, cRows, cbRow, cbRow, cbRow * cRows, cbRow * cRows);

        if (!bSchema && !bRows)
            continue;

        // Dump column definitions for the table.
        ULONG ixCol;
        for (ixCol=0; ixCol<cCols; ++ixCol)
        {
            m_pTables->GetColumnInfo(ixTbl, ixCol, &oCol, &cbCol, &ulType, &pNameColumn);

            VWrite("  col %2x:%c %-12s oCol:%2x, cbCol:%x, %-7s",
                ixCol, ((ixCol==iKey)?'*':' '), pNameColumn, oCol, cbCol, DumpRawNameOfType(ulType));

            if (bStats)
            {
                ulSize = DumpRawColStats(ixTbl, ixCol, cRows);
                if (ulSize)
                    VWrite("(%d)", ulSize);
            }
            VWriteLine("");
        }

        if (!bRows)
            continue;

        // Dump the rows.
        for (ULONG rid = 1; rid <= cRows; ++rid)
        {
            if (rid == 1)
                VWriteLine("-------------------------------------------------");
            VWrite(" %3x == ", rid);
            for (ixCol=0; ixCol < cCols; ++ixCol)
            {
                if (ixCol) VWrite(", ");
                VWrite("%d:", ixCol);
                DumpRawCol(ixTbl, ixCol, rid, bStats);
            }
            VWriteLine("");
        }
    }
} // void MDInfo::DumpRaw()

void MDInfo::DumpRawCSV()
{
    ULONG       cTables;                // Tables in the database.
    ULONG       cCols;                  // Columns in a table.
    ULONG       cRows;                  // Rows in a table.
    ULONG       cbRow;                  // Bytes in a row of a table.
    const char  *pNameTable;            // Name of a table.
    ULONG       ulSize;

    m_pTables->GetNumTables(&cTables);

    VWriteLine("Name,Size,cRecs,cbRec");

    m_pTables->GetStringHeapSize(&ulSize);
    VWriteLine("Strings,%d", ulSize);

    m_pTables->GetBlobHeapSize(&ulSize);
    VWriteLine("Blobs,%d", ulSize);

    m_pTables->GetGuidHeapSize(&ulSize);
    VWriteLine("Guids,%d", ulSize);

    for (ULONG ixTbl = 0; ixTbl < cTables; ++ixTbl)
    {
        m_pTables->GetTableInfo(ixTbl, &cbRow, &cRows, &cCols, NULL, &pNameTable);
        VWriteLine("%s,%d,%d,%d", pNameTable, cbRow*cRows, cRows, cbRow);
    }

} // void MDInfo::DumpRawCSV()

