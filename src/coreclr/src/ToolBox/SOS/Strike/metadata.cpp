// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
#include "strike.h"
#include "util.h"
#include "genericstackprobe.h"

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to find the name of a TypeDef using       *  
*    metadata API.                                                     *
*                                                                      *
\**********************************************************************/
// Caller should guard against exception
// !!! mdName should have at least mdNameLen WCHAR
static HRESULT NameForTypeDef_s(mdTypeDef tkTypeDef, IMetaDataImport *pImport,
                              __out_ecount (capacity_mdName) WCHAR *mdName, size_t capacity_mdName)
{
    DWORD flags;
    ULONG nameLen;
    
    HRESULT hr = pImport->GetTypeDefProps(tkTypeDef, mdName,
                                          mdNameLen, &nameLen,
                                          &flags, NULL);
    if (hr != S_OK) {
        return hr;
    }

    if (!IsTdNested(flags)) {
        return hr;
    }
    mdTypeDef tkEnclosingClass;
    hr = pImport->GetNestedClassProps(tkTypeDef, &tkEnclosingClass);
    if (hr != S_OK) {
        return hr;
    }
    WCHAR *name = (WCHAR*)_alloca((nameLen+1)*sizeof(WCHAR));
    wcscpy_s (name, nameLen+1, mdName);
    hr = NameForTypeDef_s(tkEnclosingClass,pImport,mdName, capacity_mdName);
    if (hr != S_OK) {
        return hr;
    }
    size_t Len = _wcslen (mdName);
    if (Len < mdNameLen-2) {
        mdName[Len++] = L'+';
        mdName[Len] = L'\0';
    }
    Len = mdNameLen-1 - Len;
    if (Len > nameLen) {
        Len = nameLen;
    }
    wcsncat_s (mdName,capacity_mdName,name,Len);
    return hr;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to find the name of a TypeDef using       *  
*    metadata API.                                                     *
*                                                                      *
\**********************************************************************/
// Caller should guard against exception
// !!! mdName should have at least mdNameLen WCHAR
/*
static HRESULT NameForTypeDefNew(mdTypeDef tkTypeDef, IMDInternalImport *pImport,
                              WCHAR *mdName)
{
    DWORD flags;
    ULONG nameLen;
    char *name = (char *)_alloca((mdNameLen+1)*sizeof(char));
    char *namesp = (char *)_alloca((mdNameLen+1)*sizeof(char));
    
    HRESULT hr = pImport->GetNameOfTypeDef(tkTypeDef, name, namesp);
    if (FAILED(hr))
    {
        return hr;
    }

    strcpy (namesp, ".");
    strcpy (namesp, name);
    MultiByteToWideChar (CP_ACP,0,namesp,-1,mdName,mdNameLen);
    return hr;
}
*/

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    Find the Module MD Importer given the name of the Module.         *
*                                                                      *
\**********************************************************************/
IMetaDataImport* MDImportForModule(DacpModuleData* pModule)
{
    IMetaDataImport *pRet = NULL;
    ToRelease<IXCLRDataModule> module;
    HRESULT hr = g_sos->GetModule(pModule->Address, &module);
    
    if (SUCCEEDED(hr))
        hr = module->QueryInterface(IID_IMetaDataImport, (LPVOID *) &pRet);

    if (SUCCEEDED(hr))
        return pRet;
    
    return NULL;
}

IMetaDataImport* MDImportForModule(DWORD_PTR pModule)
{
    DacpModuleData moduleData;
    if(moduleData.Request(g_sos, TO_CDADDR(pModule))==S_OK)
        return MDImportForModule(&moduleData);
    else
        return NULL;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    Find the name for a metadata token given an importer.             *
*                                                                      *
\**********************************************************************/
HRESULT NameForToken_s(mdTypeDef mb, IMetaDataImport *pImport, __out_ecount (capacity_mdName) WCHAR *mdName, size_t capacity_mdName,
                     bool bClassName)
{
    mdName[0] = L'\0';
    if ((mb & 0xff000000) != mdtTypeDef
        && (mb & 0xff000000) != mdtFieldDef
        && (mb & 0xff000000) != mdtMethodDef)
    {
        //ExtOut("unsupported\n");
        return E_FAIL;
    }
    
    HRESULT hr = E_FAIL;
    
    PAL_CPP_TRY
    {
        static WCHAR name[MAX_CLASSNAME_LENGTH];
        if ((mb & 0xff000000) == mdtTypeDef)
        {
            hr = NameForTypeDef_s (mb, pImport, mdName, capacity_mdName);
        }
        else if ((mb & 0xff000000) ==  mdtFieldDef)
        {
            mdTypeDef mdClass;
            ULONG size;
            hr = pImport->GetMemberProps(mb, &mdClass,
                                         name, sizeof(name)/sizeof(WCHAR)-1, &size,
                                         NULL, NULL, NULL, NULL,
                                         NULL, NULL, NULL, NULL);
            if (SUCCEEDED (hr))
            {
                if (mdClass != mdTypeDefNil && bClassName)
                {
                    hr = NameForTypeDef_s (mdClass, pImport, mdName, capacity_mdName);
                    wcscat_s (mdName, capacity_mdName, W("."));
                }
                name[size] = L'\0';
                wcscat_s (mdName, capacity_mdName, name);
            }
        }
        else if ((mb & 0xff000000) ==  mdtMethodDef)
        {
            mdTypeDef mdClass;
            ULONG size;
            hr = pImport->GetMethodProps(mb, &mdClass,
                                         name, sizeof(name)/sizeof(WCHAR)-1, &size,
                                         NULL, NULL, NULL, NULL, NULL);
            if (SUCCEEDED (hr))
            {
                if (mdClass != mdTypeDefNil && bClassName)
                {
                    hr = NameForTypeDef_s (mdClass, pImport, mdName, capacity_mdName);
                    wcscat_s (mdName, capacity_mdName, W("."));
                }
                name[size] = L'\0';
                wcscat_s (mdName, capacity_mdName, name);
            }
        }
        else
        {
            ExtOut ("Unsupported token type\n");
            hr = E_FAIL;
        }
    }
    PAL_CPP_CATCH_ALL
    {
            hr = E_FAIL;
    }
    PAL_CPP_ENDTRY
    return hr;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    Find the name for a metadata token given an importer.             *
*                                                                      *
\**********************************************************************/
/*
HRESULT NameForTokenNew(mdTypeDef mb, IMDInternalImport *pImport, WCHAR *mdName, size_t capacity_mdName, 
                     bool bClassName)
{

   // TODO: Change calls to use the secure versions (string as well as to functions defined here)
   //       Simply uncommenting this function will not result in a clean compile
   //                                   --chirayuk @ 11/23/2004

    mdName[0] = L'\0';
    if ((mb & 0xff000000) != mdtTypeDef
        && (mb & 0xff000000) != mdtFieldDef
        && (mb & 0xff000000) != mdtMethodDef)
    {
        //ExtOut("unsupported\n");
        return E_FAIL;
    }
    
    HRESULT hr;
    
    __try
        {
            static WCHAR name[MAX_CLASSNAME_LENGTH];
            if (TypeFromToken(mb) == mdtTypeDef)
            {
                hr = NameForTypeDefNew (mb, pImport, mdName);
            }
            else if (TypeFromToken(mb) ==  mdtFieldDef)
            {
                mdTypeDef mdClass;
                ULONG size;
                MultiByteToWideChar (CP_ACP,0,pImport->GetNameOfFieldDef(mb),-1,name,MAX_CLASSNAME_LENGTH);

                hr = pImport->GetParentToken (mb, &mdClass);                
                if (SUCCEEDED (hr))
                {
                    if (mdClass != mdTypeDefNil && bClassName)
                    {
                        hr = NameForTypeDefNew (mdClass, pImport, mdName);
                        _wcscat (mdName, W("."));
                    }
                    name[size] = L'\0';
                    _wcscat (mdName, name);
                }
            }
            else if (TypeFromToken(mb) ==  mdtMethodDef)
            {
                mdTypeDef mdClass;
                ULONG size;

                MultiByteToWideChar (CP_ACP,0,pImport->GetNameOfMethodDef(mb),-1,name,MAX_CLASSNAME_LENGTH);                
                hr = pImport->GetParentToken (mb, &mdClass);                
                if (SUCCEEDED (hr))
                {
                    if (mdClass != mdTypeDefNil && bClassName)
                    {
                        hr = NameForTypeDefNew (mdClass, pImport, mdName);
                        _wcscat (mdName, W("."));
                    }
                    name[size] = L'\0';
                    _wcscat (mdName, name);
                }
            }
            else
            {
                ExtOut ("Unsupported token type\n");
                hr = E_FAIL;
            }
        }
    __except (EXCEPTION_EXECUTE_HANDLER)
        {
            //ExtOut("Metadata operation failure\n");
            hr = E_FAIL;
        }
    return hr;
}
*/

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to find the name of a metadata token      *  
*    using metadata API.                                               *
*                                                                      *
\**********************************************************************/
void NameForToken_s(DWORD_PTR ModuleAddr, mdTypeDef mb, __out_ecount (capacity_mdName) WCHAR *mdName, size_t capacity_mdName,
                  bool bClassName)
{
    DacpModuleData ModuleData;
    mdName[0] = L'\0';
    if(ModuleData.Request(g_sos, TO_CDADDR(ModuleAddr))==S_OK)
        NameForToken_s(&ModuleData,mb,mdName,capacity_mdName,bClassName);
}

BOOL IsValidToken(DWORD_PTR ModuleAddr, mdTypeDef mb)
{
    DacpModuleData ModuleData;
    if(ModuleData.Request(g_sos, TO_CDADDR(ModuleAddr))==S_OK)
    {
        ToRelease<IMetaDataImport> pImport = MDImportForModule(&ModuleData);
        if (pImport)
        {
            if (pImport->IsValidToken (mb))
                return TRUE;
        }
    }
    return FALSE;
}

void NameForToken_s(DacpModuleData *pModule, mdTypeDef mb, __out_ecount (capacity_mdName) WCHAR *mdName, size_t capacity_mdName,
                  bool bClassName)
{
    mdName[0] = L'\0';
    HRESULT hr = 0;
    ToRelease<IMetaDataImport> pImport = MDImportForModule(pModule);
    if (pImport)
    {
        hr = NameForToken_s (mb, pImport, mdName, capacity_mdName, bClassName);
    }
    
    if (!pImport || !SUCCEEDED (hr))
    {
        const SIZE_T capacity_moduleName = mdNameLen+19;
        LPWSTR moduleName = (LPWSTR)alloca(capacity_moduleName * sizeof(WCHAR)); // for the "Dynamic Module In " below
        FileNameForModule(pModule,moduleName);
        if (moduleName[0] == L'\0') {
            DacpAssemblyData assembly;
            assembly.Request(g_sos,pModule->Assembly);
            if (assembly.isDynamic) {
                wcscpy_s(moduleName, capacity_moduleName, W("Dynamic "));
            }
            wcscat_s (moduleName, capacity_moduleName, W("Module in "));            
            if(g_sos->GetAssemblyName(pModule->Assembly, mdNameLen, g_mdName, NULL)==S_OK)
            {
                wcscat_s(moduleName, capacity_moduleName, g_mdName);
            }
        }
        swprintf_s (mdName, capacity_mdName,
                   W(" mdToken: %08x (%ws)"),
                   mb,
                   moduleName[0] ? moduleName : W("Unknown Module") );
    }
}

#define STRING_BUFFER_LEN 1024

class MDInfo
{
public:
    MDInfo (DWORD_PTR ModuleAddr)
    {
        m_pImport = MDImportForModule(ModuleAddr);
        if (!m_pImport)
            ExtOut("Unable to get IMetaDataImport for module %p\n", ModuleAddr);
        m_pSigBuf = NULL;
    }

    MDInfo (IMetaDataImport * pImport)
    {
        m_pImport = pImport;
        m_pImport->AddRef();
        m_pSigBuf = NULL;
    }

    void GetMethodName(mdTypeDef token, CQuickBytes *fullName);
    GetSignatureStringResults GetMethodSignature(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, CQuickBytes *fullName);
    GetSignatureStringResults GetSignature(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, CQuickBytes *fullName);

    LPCWSTR TypeDefName(mdTypeDef inTypeDef);
    LPCWSTR TypeRefName(mdTypeRef tr);
    LPCWSTR TypeDeforRefName(mdToken inToken);
private:
    // helper to init signature buffer
    void InitSigBuffer()
    {
        ((LPWSTR)m_pSigBuf->Ptr())[0] = L'\0';
    }

    HRESULT AddToSigBuffer(LPCWSTR string);

    HRESULT GetFullNameForMD(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, LONG *plSigBlobRemaining OPTIONAL);
    HRESULT GetOneElementType(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, ULONG *pcb);

    ToRelease<IMetaDataImport> m_pImport;
    // Signature buffer.
    CQuickBytes     *m_pSigBuf;

    // temporary buffer for TypeDef or TypeRef name. Consume immediately
    // because other functions may overwrite it.
    static WCHAR            m_szTempBuf[MAX_CLASSNAME_LENGTH];

    static WCHAR            m_szName[MAX_CLASSNAME_LENGTH];
};

WCHAR MDInfo::m_szTempBuf[MAX_CLASSNAME_LENGTH];
WCHAR MDInfo::m_szName[MAX_CLASSNAME_LENGTH];

GetSignatureStringResults GetMethodSignatureString (PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, DWORD_PTR dwModuleAddr, CQuickBytes *sigString)
{
    MDInfo mdInfo(dwModuleAddr);

    return mdInfo.GetMethodSignature(pbSigBlob, ulSigBlob, sigString);
}


GetSignatureStringResults GetSignatureString (PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, DWORD_PTR dwModuleAddr, CQuickBytes *sigString)
{
    MDInfo mdInfo(dwModuleAddr);

    return mdInfo.GetSignature(pbSigBlob, ulSigBlob, sigString);
}

void GetMethodName(mdMethodDef methodDef, IMetaDataImport * pImport, CQuickBytes *fullName)
{
    MDInfo mdInfo(pImport);

    mdInfo.GetMethodName(methodDef, fullName);
}


// Tables for mapping element type to text
const WCHAR *g_wszMapElementType[] = 
{
    W("End"),          // 0x0
    W("Void"),         // 0x1
    W("Boolean"),
    W("Char"), 
    W("I1"),
    W("UI1"),
    W("I2"),           // 0x6
    W("UI2"),
    W("I4"),
    W("UI4"),
    W("I8"),
    W("UI8"),
    W("R4"),
    W("R8"),
    W("String"),
    W("Ptr"),          // 0xf
    W("ByRef"),        // 0x10
    W("ValueClass"),
    W("Class"),
    W("CopyCtor"),
    W("MDArray"),      // 0x14
    W("GENArray"),
    W("TypedByRef"),
    W("VALUEARRAY"),
    W("I"),
    W("U"),
    W("R"),            // 0x1a
    W("FNPTR"),
    W("Object"),
    W("SZArray"),
    W("GENERICArray"),
    W("CMOD_REQD"),
    W("CMOD_OPT"),
    W("INTERNAL"),
};
 
const WCHAR *g_wszCalling[] = 
{   
    W("[DEFAULT]"),
    W("[C]"),
    W("[STDCALL]"),
    W("[THISCALL]"),
    W("[FASTCALL]"),
    W("[VARARG]"),
    W("[FIELD]"),
    W("[LOCALSIG]"),
    W("[PROPERTY]"),
    W("[UNMANAGED]"),
};

void MDInfo::GetMethodName(mdTypeDef token, CQuickBytes *fullName)
{
    if (m_pImport == NULL) {
        return;
    }

    HRESULT hr;
    mdTypeDef memTypeDef;
    ULONG nameLen;
    DWORD flags;
    PCCOR_SIGNATURE pbSigBlob;
    ULONG ulSigBlob;
    ULONG ulCodeRVA;
    ULONG ulImplFlags;

    m_pSigBuf = fullName;
    InitSigBuffer();

    WCHAR szFunctionName[1024];

    hr = m_pImport->GetMethodProps(token, &memTypeDef, 
                                   szFunctionName, _countof(szFunctionName), &nameLen, 
                                   &flags, &pbSigBlob, &ulSigBlob, &ulCodeRVA, &ulImplFlags);
    if (FAILED (hr))
    {
        return;
    }
    
    szFunctionName[nameLen] = L'\0';
    m_szName[0] = L'\0';
    if (memTypeDef != mdTypeDefNil)
    {
        hr = NameForTypeDef_s (memTypeDef, m_pImport, m_szName, _countof(m_szName));
        if (SUCCEEDED (hr)) {
            wcscat_s (m_szName, _countof(m_szName), W("."));
        }
    }
    wcscat_s (m_szName, _countof(m_szName), szFunctionName);

    LONG lSigBlobRemaining;
    hr = GetFullNameForMD(pbSigBlob, ulSigBlob, &lSigBlobRemaining);

    // We should have consumed all signature blob.  If not, dump the sig in hex.
    //  Also dump in hex if so requested.
    if (lSigBlobRemaining != 0)
    {
        // Did we not consume enough, or try to consume too much?
        if (lSigBlobRemaining < 0)
            ExtOut("ERROR IN SIGNATURE:  Signature should be larger.\n");
        else
            ExtOut("ERROR IN SIGNATURE:  Not all of signature blob was consumed.  %d byte(s) remain\n", lSigBlobRemaining);
    }

    if (FAILED(hr))
        ExtOut("ERROR!! Bad signature blob value!");
}


GetSignatureStringResults MDInfo::GetMethodSignature(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, CQuickBytes *fullName)
{
    if (!m_pImport)
        return GSS_ERROR;

    m_pSigBuf = fullName;
    InitSigBuffer();

    m_szName[0] = '\0';

    LONG lSigBlobRemaining;
    if (FAILED(GetFullNameForMD(pbSigBlob, ulSigBlob, &lSigBlobRemaining)))
        return GSS_ERROR;
    
    if (lSigBlobRemaining < 0)
        return GSS_INSUFFICIENT_DATA;

    return GSS_SUCCESS;
}


GetSignatureStringResults MDInfo::GetSignature(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, CQuickBytes *fullName)
{
    if (!m_pImport)
        return GSS_ERROR;

    m_pSigBuf = fullName;
    InitSigBuffer();

    m_szName[0] = '\0';

    ULONG cb;
    if (FAILED(GetOneElementType(pbSigBlob, ulSigBlob, &cb)))
    {
        if (cb > ulSigBlob)
            return GSS_INSUFFICIENT_DATA;
        else
            return GSS_ERROR;
    }

    return GSS_SUCCESS;
}


inline bool isCallConv(unsigned sigByte, CorCallingConvention conv)
{
    return ((sigByte & IMAGE_CEE_CS_CALLCONV_MASK) == (unsigned) conv); 
}

#ifndef IfFailGoto
#define IfFailGoto(EXPR, LABEL) \
do { hr = (EXPR); if(FAILED(hr)) { goto LABEL; } } while (0)
#endif

#ifndef IfFailGo
#define IfFailGo(EXPR) IfFailGoto(EXPR, ErrExit)
#endif

#ifndef IfFailRet
#define IfFailRet(EXPR) do { hr = (EXPR); if(FAILED(hr)) { return (hr); } } while (0)
#endif

#ifndef _ASSERTE
#define _ASSERTE(expr)
#endif

HRESULT MDInfo::GetFullNameForMD(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, LONG *plSigBlobRemaining)
{
    ULONG       cbCur = 0;
    ULONG       cb;
    ULONG       ulData = NULL;
    ULONG       ulArgs;
    HRESULT     hr = NOERROR;

    cb = CorSigUncompressData(pbSigBlob, &ulData);

    // 0 is a valid calling convention byte (IMAGE_CEE_CS_CALLCONV_DEFAULT w/ no flags)
    //if (ulData == NULL)
    //    goto ErrExit;

    AddToSigBuffer (g_wszCalling[ulData & IMAGE_CEE_CS_CALLCONV_MASK]);
    if (cb>ulSigBlob) 
        goto ErrExit;
    cbCur += cb;
    ulSigBlob -= cb;

    if (ulData & IMAGE_CEE_CS_CALLCONV_HASTHIS)
        AddToSigBuffer ( W(" [hasThis]"));
    if (ulData & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS)
        AddToSigBuffer ( W(" [explicit]"));

    AddToSigBuffer (W(" "));
    if ( isCallConv(ulData,IMAGE_CEE_CS_CALLCONV_FIELD) )
    {
        // display field type
        if (FAILED(hr = GetOneElementType(&pbSigBlob[cbCur], ulSigBlob, &cb)))
            goto ErrExit;
        AddToSigBuffer ( W(" "));
        AddToSigBuffer ( m_szName);
        if (cb>ulSigBlob) 
            goto ErrExit;
        cbCur += cb;
        ulSigBlob -= cb;
    }
    else 
    {
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulArgs);
        if (cb>ulSigBlob) 
            goto ErrExit;
        cbCur += cb;
        ulSigBlob -= cb;

        if (ulData != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG)
        {
            // display return type when it is not a local varsig
            if (FAILED(hr = GetOneElementType(&pbSigBlob[cbCur], ulSigBlob, &cb)))
                goto ErrExit;
            AddToSigBuffer (W(" "));
            AddToSigBuffer (m_szName);
            AddToSigBuffer ( W("("));
            if (cb>ulSigBlob) 
                goto ErrExit;
            cbCur += cb;
            ulSigBlob -= cb;
        }

        ULONG       i = 0;
        while (i < ulArgs && ulSigBlob > 0)
        {
            ULONG       ulDataUncompress;

            // Handle the sentinal for varargs because it isn't counted in the args.
            CorSigUncompressData(&pbSigBlob[cbCur], &ulDataUncompress);
            ++i;

            if (FAILED(hr = GetOneElementType(&pbSigBlob[cbCur], ulSigBlob, &cb)))
                goto ErrExit;
            if (i != ulArgs) {
                AddToSigBuffer ( W(","));
            }
            if (cb>ulSigBlob) 
                goto ErrExit;

            cbCur += cb;
            ulSigBlob -= cb;
        }
        AddToSigBuffer ( W(")"));
    }

    // Nothing consumed but not yet counted.
    cb = 0;

ErrExit:

    if (plSigBlobRemaining)
        *plSigBlobRemaining = (ulSigBlob - cb);

    return hr;
}

LPCWSTR MDInfo::TypeDefName(mdTypeDef inTypeDef)
{
    if (m_pImport == NULL) {
        return W("");
    }

    HRESULT hr;

    hr = m_pImport->GetTypeDefProps(
                            // [IN] The import scope.
        inTypeDef,              // [IN] TypeDef token for inquiry.
        m_szTempBuf,            // [OUT] Put name here.
        MAX_CLASSNAME_LENGTH ,      // [IN] size of name buffer in wide chars.
        NULL,                   // [OUT] put size of name (wide chars) here.
        NULL,                   // [OUT] Put flags here.
        NULL);                  // [OUT] Put base class TypeDef/TypeRef here.

    if (FAILED(hr)) return (W("NoName"));
    return (m_szTempBuf);
} // LPCWSTR MDInfo::TypeDefName()
LPCWSTR MDInfo::TypeRefName(mdTypeRef tr)
{
    if (m_pImport == NULL) {
        return W("");
    }

    HRESULT hr;
    
    hr = m_pImport->GetTypeRefProps(           
        tr,                 // The class ref token.
        NULL,               // Resolution scope.
        m_szTempBuf,             // Put the name here.
        MAX_CLASSNAME_LENGTH,             // Size of the name buffer, wide chars.
        NULL);              // Put actual size of name here.
    if (FAILED(hr)) return (W("NoName"));

    return (m_szTempBuf);
} // LPCWSTR MDInfo::TypeRefName()

LPCWSTR MDInfo::TypeDeforRefName(mdToken inToken)
{
    if (RidFromToken(inToken))
    {
        if (TypeFromToken(inToken) == mdtTypeDef)
            return (TypeDefName((mdTypeDef) inToken));
        else if (TypeFromToken(inToken) == mdtTypeRef)
            return (TypeRefName((mdTypeRef) inToken));
        else
            return (W("[InvalidReference]"));
    }
    else
        return (W(""));
} // LPCWSTR MDInfo::TypeDeforRefName()


HRESULT MDInfo::AddToSigBuffer(LPCWSTR string)
{
    HRESULT     hr;
    IfFailRet(m_pSigBuf->ReSize((_wcslen((LPWSTR)m_pSigBuf->Ptr()) + _wcslen(string) + 1) * sizeof(WCHAR)));
    wcscat_s((LPWSTR)m_pSigBuf->Ptr(), m_pSigBuf->Size()/sizeof(WCHAR),string);
    return NOERROR;
}

HRESULT MDInfo::GetOneElementType(PCCOR_SIGNATURE pbSigBlob, ULONG ulSigBlob, ULONG *pcb)
{
    HRESULT     hr = S_OK;              // A result.
    ULONG       cbCur = 0;
    ULONG       cb;
    ULONG       ulData;
    ULONG       ulTemp;
    int         iTemp = 0;
    mdToken     tk;
    const size_t capacity_buffer = 9;

    cb = CorSigUncompressData(pbSigBlob, &ulData);

    if (cb == ULONG(-1)) {
        hr = E_FAIL;
        goto ErrExit;
    }

    cbCur += cb;

    // Handle the modifiers.
    if (ulData & ELEMENT_TYPE_MODIFIER)
    {
        if (ulData == ELEMENT_TYPE_SENTINEL)
            IfFailGo(AddToSigBuffer(W("<ELEMENT_TYPE_SENTINEL> ")));
        else if (ulData == ELEMENT_TYPE_PINNED)
            IfFailGo(AddToSigBuffer(W("PINNED ")));
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
        IfFailGo(AddToSigBuffer(g_wszMapElementType[ulData]));
        IfFailGo(AddToSigBuffer(W(" ")));
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulData);
        cbCur += cb;
    }

    // Generics
    if (ulData == ELEMENT_TYPE_VAR)
    {
        IfFailGo(AddToSigBuffer(W("__Canon")));

        // The next byte represents which generic parameter is referred to.  We
        // do not currently use this information, so just bypass this byte.
        cbCur++;
        
        goto ErrExit;
    }

    // A generic instance, e.g. IEnumerable<String>
    if (ulData == ELEMENT_TYPE_GENERICINST)
    {
        // Print out the base type.
        IfFailGo(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb));
        cbCur += cb;

        // Get the number of generic arguments.
        ULONG numParams = 0;
        IfFailGo(CorSigUncompressData(&pbSigBlob[cbCur], 1, &numParams, &cb));
        cbCur += cb;

        // Print out the list of arguments
        IfFailGo(AddToSigBuffer(W("<")));
        for (ULONG i = 0; i < numParams; i++)
        {
            if (i > 0) 
                IfFailGo(AddToSigBuffer(W(",")));
            
            IfFailGo(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb));
            cbCur += cb;
        }
        IfFailGo(AddToSigBuffer(W(">")));
        goto ErrExit;
    }
    
    // Past this point we must have something which directly maps to a value in g_wszMapElementType.
    IfFailGo(AddToSigBuffer(g_wszMapElementType[ulData]));
    if (CorIsPrimitiveType((CorElementType)ulData) || 
        ulData == ELEMENT_TYPE_TYPEDBYREF ||
        ulData == ELEMENT_TYPE_OBJECT ||
        ulData == ELEMENT_TYPE_I ||
        ulData == ELEMENT_TYPE_U)
    {
        // If this is a primitive type, we are done
        goto ErrExit;
    }

    AddToSigBuffer(W(" "));
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
            IfFailGo(AddToSigBuffer(TypeDeforRefName(tk)));
        }
        else
        {
            _ASSERTE(TypeFromToken(tk) == mdtTypeSpec);
            WCHAR buffer[capacity_buffer];
            _itow_s (tk, buffer, capacity_buffer, 16);
            IfFailGo(AddToSigBuffer(buffer));
        }
        if (ulData == ELEMENT_TYPE_CMOD_REQD ||
            ulData == ELEMENT_TYPE_CMOD_OPT)
        {
            IfFailGo(AddToSigBuffer(W(" ")));
            if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
                goto ErrExit;
            cbCur += cb;
        }

        goto ErrExit;
    }
    if (ulData == ELEMENT_TYPE_SZARRAY)
    {
        // display the base type of SZARRAY or GENERICARRAY
        if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
            goto ErrExit;
        cbCur += cb;
        goto ErrExit;
    }
    if (ulData == ELEMENT_TYPE_FNPTR) 
    {
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulData);
        cbCur += cb;
        if (ulData & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS)
            IfFailGo(AddToSigBuffer(W("[explicit] ")));
        if (ulData & IMAGE_CEE_CS_CALLCONV_HASTHIS)
            IfFailGo(AddToSigBuffer(W("[hasThis] ")));

        IfFailGo(AddToSigBuffer(g_wszCalling[ulData & IMAGE_CEE_CS_CALLCONV_MASK]));

            // Get number of args
        ULONG numArgs;
        cb = CorSigUncompressData(&pbSigBlob[cbCur], &numArgs);
        cbCur += cb;

            // do return type
        if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
            goto ErrExit;
        cbCur += cb;

        IfFailGo(AddToSigBuffer(W("(")));
        while (numArgs > 0) 
        {
            if (cbCur > ulSigBlob)
                goto ErrExit;
            if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
                goto ErrExit;
            cbCur += cb;
            --numArgs;
            if (numArgs > 0) 
                IfFailGo(AddToSigBuffer(W(",")));
        }
        IfFailGo(AddToSigBuffer(W(")")));
        goto ErrExit;
    }

    if (ulData == ELEMENT_TYPE_INTERNAL)
    {
        IfFailGo(AddToSigBuffer(W("MT ")));

        void *pvMethodTable;
        cb = CorSigUncompressPointer(&pbSigBlob[cbCur], (void**)&pvMethodTable);
        cbCur += cb;

        const size_t capacity_szMethodTableValue = 10;
        WCHAR szMethodTableValue[10];
        itow_s_ptr((INT_PTR)pvMethodTable, szMethodTableValue, capacity_szMethodTableValue, 16);

        IfFailGo(AddToSigBuffer(szMethodTableValue));
        IfFailGo(AddToSigBuffer(W(" ")));

        IfFailGo(g_sos->GetMethodTableName(TO_CDADDR(pvMethodTable), mdNameLen, g_mdName, NULL));
        IfFailGo(AddToSigBuffer(g_mdName));

        goto ErrExit;
    }

        
    if(ulData != ELEMENT_TYPE_ARRAY) return E_FAIL;

    // display the base type of SDARRAY
    if (FAILED(GetOneElementType(&pbSigBlob[cbCur], ulSigBlob-cbCur, &cb)))
        goto ErrExit;
    cbCur += cb;

    IfFailGo(AddToSigBuffer(W(" ")));
    // display the rank of MDARRAY
    cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulData);
    cbCur += cb;
    WCHAR buffer[capacity_buffer];
    _itow_s (ulData, buffer, capacity_buffer, 10);
    IfFailGo(AddToSigBuffer(buffer));
    if (ulData == 0)
        // we are done if no rank specified
        goto ErrExit;

    IfFailGo(AddToSigBuffer(W(" ")));
    // how many dimensions have size specified?
    cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulData);
    cbCur += cb;
    _itow_s (ulData, buffer, capacity_buffer, 10);
    IfFailGo(AddToSigBuffer(buffer));
    if (ulData == 0) {
        IfFailGo(AddToSigBuffer(W(" ")));
    }
    while (ulData)
    {

        cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulTemp);
        _itow_s (ulTemp, buffer, capacity_buffer, 10);
        IfFailGo(AddToSigBuffer(buffer));
        IfFailGo(AddToSigBuffer(W(" ")));
        cbCur += cb;
        ulData--;
    }
    // how many dimensions have lower bounds specified?
    cb = CorSigUncompressData(&pbSigBlob[cbCur], &ulData);
    cbCur += cb;
    _itow_s (ulData, buffer, capacity_buffer, 10);
    IfFailGo(AddToSigBuffer(buffer));
    while (ulData)
    {

        cb = CorSigUncompressSignedInt(&pbSigBlob[cbCur], &iTemp);
        _itow_s (iTemp, buffer, capacity_buffer, 10);
        IfFailGo(AddToSigBuffer(buffer));
        IfFailGo(AddToSigBuffer(W(" ")));
        cbCur += cb;
        ulData--;
    }
    
ErrExit:
    if (cbCur > ulSigBlob)
        hr = E_FAIL;
    *pcb = cbCur;
    return hr;
}

//*****************************************************************************
// Used when the method is tiny (< 64 bytes), and there are no local vars
//*****************************************************************************
typedef struct tagCOR_ILMETHOD_TINY : IMAGE_COR_ILMETHOD_TINY
{
    bool     IsTiny() const         { return((Flags_CodeSize & (CorILMethod_FormatMask >> 1)) == CorILMethod_TinyFormat); } 
    DWORD    GetLocalVarSigTok() const  { return(0); }  
} COR_ILMETHOD_TINY;


//*****************************************************************************
// This strucuture is the 'fat' layout, where no compression is attempted. 
// Note that this structure can be added on at the end, thus making it extensible
//*****************************************************************************
typedef struct tagCOR_ILMETHOD_FAT : IMAGE_COR_ILMETHOD_FAT
{
    bool     IsFat() const              { return((Flags & CorILMethod_FormatMask) == CorILMethod_FatFormat); }  
    mdToken  GetLocalVarSigTok() const      { return(LocalVarSigTok); } 
} COR_ILMETHOD_FAT;
