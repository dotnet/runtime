// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ildasmpch.h"
#include <crtdbg.h>
#include <utilcode.h>
#include "specstrings.h"
#include "debugmacros.h"
#include "corpriv.h"
#include "ceeload.h"
#include "dynamicarray.h"
#include <metamodelpub.h>
#include "formattype.h"

#define DECLARE_DATA
#include "dasmenum.hpp"
#include "dis.h"

#include "resource.h"
#include "dasm_sz.h"

//#define MAX_FILENAME_LENGTH         2048     //moved to dis.h

#include <corsym.h>
#include <clrversion.h>

// Disable the "initialization of static local vars is no thread safe" error
#ifdef _MSC_VER
#pragma warning(disable : 4640)
#endif

#ifdef TARGET_UNIX
#include "resourcestring.h"
#define NATIVE_STRING_RESOURCE_NAME dasm_rc
DECLARE_NATIVE_STRING_RESOURCE_TABLE(NATIVE_STRING_RESOURCE_NAME);
#endif

#include "mdfileformat.h"


struct MIDescriptor
{
    mdToken tkClass;    // defining class token
    mdToken tkDecl;     // implemented method token
    mdToken tkBody;     // implementing method token
    mdToken tkBodyParent;   // parent of the implementing method
};

ISymUnmanagedReader*        g_pSymReader = NULL;

IMDInternalImport*      g_pImport = NULL;
IMetaDataImport2*        g_pPubImport;
extern IMetaDataAssemblyImport* g_pAssemblyImport;
PELoader *              g_pPELoader;
void *                  g_pMetaData;
unsigned                g_cbMetaData;
IMAGE_COR20_HEADER *    g_CORHeader;
DynamicArray<__int32>  *g_pPtrTags = NULL;      //to keep track of all "ldptr"
DynamicArray<DWORD>    *g_pPtrSize= NULL;      //to keep track of all "ldptr"
int                     g_iPtrCount = 0;
mdToken *               g_cl_list = NULL;
mdToken *               g_cl_enclosing = NULL;
BYTE*                   g_enum_td_type = NULL;  // enum (TD) underlying types
BYTE*                   g_enum_tr_type = NULL;  // enum (TR) underlying types
IMDInternalImport**     g_asmref_import = NULL; // IMDInternalImports for external assemblies
DynamicArray<MIDescriptor>   *g_pmi_list = NULL;
DWORD                   g_NumMI;
DWORD                   g_NumClasses;
DWORD                   g_NumTypeRefs;
DWORD                   g_NumAsmRefs;
DWORD                   g_NumModules;
BOOL                    g_fDumpIL = TRUE;
BOOL                    g_fDumpHeader = FALSE;
BOOL                    g_fDumpAsmCode = TRUE;
extern BOOL             g_fDumpTokens; // declared in formatType.cpp
BOOL                    g_fDumpStats = FALSE;
BOOL                    g_fTDC = TRUE;
BOOL                    g_fShowCA = TRUE;
BOOL                    g_fCAVerbal = FALSE;
BOOL                    g_fShowRefs = FALSE;

BOOL                    g_fDumpToPerfWriter = FALSE;
HANDLE                  g_PerfDataFilePtr = NULL;

BOOL                    g_fDumpClassList = FALSE;
BOOL                    g_fDumpTypeList = FALSE;
BOOL                    g_fDumpSummary = FALSE;
BOOL                    g_fDecompile = FALSE; // still in progress
BOOL                    g_fShowBytes = FALSE;
BOOL                    g_fShowSource = FALSE;
BOOL                    g_fPrettyPrint = FALSE;
BOOL                    g_fInsertSourceLines = FALSE;
BOOL                    g_fThisIsInstanceMethod;
BOOL                    g_fTryInCode = TRUE;

BOOL                    g_fLimitedVisibility = FALSE;
BOOL                    g_fHidePub = TRUE;
BOOL                    g_fHidePriv = TRUE;
BOOL                    g_fHideFam = TRUE;
BOOL                    g_fHideAsm = TRUE;
BOOL                    g_fHideFAA = TRUE;
BOOL                    g_fHideFOA = TRUE;
BOOL                    g_fHidePrivScope = TRUE;

BOOL                    g_fProject = FALSE;  // if .winmd file, transform to .NET view

extern BOOL             g_fQuoteAllNames; // declared in formatType.cpp, init to FALSE
BOOL                    g_fForwardDecl=FALSE;

char                    g_szAsmCodeIndent[MAX_MEMBER_LENGTH];
char                    g_szNamespace[MAX_MEMBER_LENGTH];

DWORD                   g_Mode = MODE_DUMP_ALL;

char                    g_pszClassToDump[MAX_CLASSNAME_LENGTH];
char                    g_pszMethodToDump[MAX_MEMBER_LENGTH];
char                    g_pszSigToDump[MAX_SIGNATURE_LENGTH];

BOOL                    g_fCustomInstructionEncodingSystem = FALSE;

COR_FIELD_OFFSET        *g_rFieldOffset = NULL;
ULONG                   g_cFieldsMax, g_cFieldOffsets;

char*                   g_pszExeFile;
char                    g_szInputFile[MAX_FILENAME_LENGTH]; // in UTF-8
WCHAR                   g_wszFullInputFile[MAX_PATH + 1]; // in UTF-16
char                    g_szOutputFile[MAX_FILENAME_LENGTH]; // in UTF-8
char*                   g_pszObjFileName;
FILE*                   g_pFile = NULL;

mdToken                 g_tkClassToDump = 0;
mdToken                 g_tkMethodToDump = 0;

unsigned                g_uConsoleCP = CP_ACP;
unsigned                g_uCodePage = g_uConsoleCP;

char*                   g_rchCA = NULL; // dyn.allocated array of CA dumped/not flags
unsigned                g_uNCA = 0;     // num. of CAs

struct ResourceNode;
extern DynamicArray<LocalComTypeDescr*> *g_pLocalComType;
extern ULONG    g_LocalComTypeNum;

// MetaInfo integration:
#include "../tools/metainfo/mdinfo.h"

BOOL                    g_fDumpMetaInfo = FALSE;
ULONG                   g_ulMetaInfoFilter = MDInfo::dumpDefault;
// Validator module type.
DWORD g_ValModuleType = ValidatorModuleTypeInvalid;
IMetaDataDispenserEx *g_pDisp = NULL;
void DisplayFile(_In_ __nullterminated WCHAR* szFile,
                 BOOL isFile,
                 ULONG DumpFilter,
                 _In_opt_z_ WCHAR* szObjFile,
                 strPassBackFn pDisplayString);
extern mdMethodDef      g_tkEntryPoint; // integration with MetaInfo

DWORD   DumpResourceToFile(_In_ __nullterminated WCHAR*   wzFileName); // see DRES.CPP

struct VTableRef
{
    mdMethodDef tkTok;
    WORD        wEntry;
    WORD        wSlot;
};

DynamicArray<VTableRef> *g_prVTableRef = NULL;
ULONG   g_nVTableRef = 0;

struct EATableRef
{
    mdMethodDef tkTok;
    char*       pszName;
};
DynamicArray<EATableRef> *g_prEATableRef=NULL;
ULONG   g_nEATableRef = 0;
ULONG   g_nEATableBase = 0;

extern HINSTANCE g_hResources;
void DumpCustomAttributeProps(mdToken tkCA, mdToken tkType, mdToken tkOwner, BYTE*pBlob, ULONG ulLen, void *GUICookie, bool bWithOwner);

WCHAR* RstrW(unsigned id)
{
    static WCHAR buffer[1024];
    DWORD cchBuff = (DWORD)ARRAY_SIZE(buffer);
    WCHAR* buff = (WCHAR*)buffer;
    memset(buffer,0,sizeof(buffer));
    switch(id)
    {
        case IDS_E_DASMOK:
        case IDS_E_PARTDASM:
        case IDS_E_PARAMSEQNO:
        case IDS_E_MEMBRENUM:
        case IDS_E_ODDMEMBER:
        case IDS_E_ENUMINIT:
        case IDS_E_NODATA:
        case IDS_E_VTFUTABLE:
        case IDS_E_BOGUSRVA:
        case IDS_E_EATJTABLE:
        case IDS_E_EATJSIZE:
        case IDS_E_RESFLAGS:
        case IDS_E_MIHENTRY:
        case IDS_E_CODEMGRTBL:
        case IDS_E_COMIMAGE:
        case IDS_E_MDDETAILS:
        case IDS_E_MISTART:
        case IDS_E_MIEND:
        case IDS_E_ONLYITEMS:
        case IDS_E_DECOMPRESS:
        case IDS_E_COMPRESSED:
        case IDS_E_INSTRDECOD:
        case IDS_E_INSTRTYPE:
        case IDS_E_SECTHEADER:
        case IDS_E_MDAIMPORT:
        case IDS_E_MDAFROMMDI:
        case IDS_E_MDIIMPORT:
        case IDS_E_NOMANIFEST:
        case IDS_W_CREATEDW32RES:
        case IDS_E_CORRUPTW32RES:
        case IDS_E_CANTACCESSW32RES:
        case IDS_E_CANTOPENW32RES:
        case IDS_ERRORREOPENINGFILE:
            wcscpy_s(buffer,ARRAY_SIZE(buffer),W("// "));
            buff +=3;
            cchBuff -= 3;
            break;
        case IDS_E_AUTOCA:
        case IDS_E_METHBEG:
        case IDS_E_DASMNATIVE:
        case IDS_E_METHODRT:
        case IDS_E_CODESIZE:
        case IDS_W_CREATEDMRES:
        case IDS_E_READINGMRES:
            wcscpy_s(buffer,ARRAY_SIZE(buffer),W("%s// "));
            buff +=5;
            cchBuff -= 5;
            break;
        case IDS_E_NORVA:
            wcscpy_s(buffer,ARRAY_SIZE(buffer),W("/* "));
            buff += 3;
            cchBuff -= 3;
            break;
        default:
            break;
    }
#ifdef TARGET_UNIX
    LoadNativeStringResource(NATIVE_STRING_RESOURCE_TABLE(NATIVE_STRING_RESOURCE_NAME),id, buff, cchBuff, NULL);
#else
    _ASSERTE(g_hResources != NULL);
    WszLoadString(g_hResources,id,buff,cchBuff);
#endif
    if(id == IDS_E_NORVA)
        wcscat_s(buff,cchBuff,W(" */"));
    return buffer;
}

char* RstrA(unsigned n, unsigned codepage)
{
    static char buff[2048];
    WCHAR* wz = RstrW(n);
    // Unicode -> UTF-8
    memset(buff,0,sizeof(buff));
    if(!WszWideCharToMultiByte(codepage,0,(LPCWSTR)wz,-1,buff,sizeof(buff),NULL,NULL))
        buff[0] = 0;
    return buff;
}
char* RstrUTF(unsigned n)
{
    return RstrA(n,CP_UTF8);
}

char* RstrANSI(unsigned n)
{
    return RstrA(n,g_uConsoleCP);
}

#if 0
void PrintEncodingSystem()
{
    long i;

    printf("Custom opcode encoding system employed\n");
    printf("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");

    for (i = 0; i < 256; i++)
    {
        long value = g_pInstructionDecodingTable->m_SingleByteOpcodes[i];

        printf("0x%02x --> ", i);
        printf("%s\n", OpcodeInfo[value].pszName);
    }
}
#endif

// buffers for formatType functions
extern CQuickBytes *        g_szBuf_KEYWORD;
extern CQuickBytes *        g_szBuf_COMMENT;
extern CQuickBytes *        g_szBuf_ERRORMSG;
extern CQuickBytes *        g_szBuf_ANCHORPT;
extern CQuickBytes *        g_szBuf_JUMPPT;
extern CQuickBytes *        g_szBuf_UnquotedProperName;
extern CQuickBytes *        g_szBuf_ProperName;

BOOL Init()
{
    g_szBuf_KEYWORD = new CQuickBytes();
    g_szBuf_COMMENT = new CQuickBytes();
    g_szBuf_ERRORMSG = new CQuickBytes();
    g_szBuf_ANCHORPT = new CQuickBytes();
    g_szBuf_JUMPPT = new CQuickBytes();
    g_szBuf_UnquotedProperName = new CQuickBytes();
    g_szBuf_ProperName = new CQuickBytes();
    return TRUE;
} // Init

extern LPCSTR *rAsmRefName;  // decl. in formatType.cpp -- for AsmRef aliases
extern ULONG   ulNumAsmRefs; // decl. in formatType.cpp -- for AsmRef aliases

void Cleanup()
{
    if (g_pAssemblyImport != NULL)
    {
        g_pAssemblyImport->Release();
        g_pAssemblyImport = NULL;
    }
    if (g_pPubImport != NULL)
    {
        g_pPubImport->Release();
        g_pPubImport = NULL;
    }
    if (g_pImport != NULL)
    {
        g_pImport->Release();
        g_pImport = NULL;
        TokenSigDelete();
    }
    if (g_pDisp != NULL)
    {
        g_pDisp->Release();
        g_pDisp = NULL;
    }

    if (g_pSymReader != NULL)
    {
        g_pSymReader->Release();
        g_pSymReader = NULL;
    }
    if (g_pPELoader != NULL)
    {
        g_pPELoader->close();
        SDELETE(g_pPELoader);
    }
    g_iPtrCount = 0;
    g_NumClasses = 0;
    g_NumTypeRefs = 0;
    g_NumModules = 0;
    g_tkEntryPoint = 0;
    g_szAsmCodeIndent[0] = 0;
    g_szNamespace[0]=0;
    g_pszClassToDump[0]=0;
    g_pszMethodToDump[0]=0;
    g_pszSigToDump[0] = 0;
    g_NumDups = 0;
    g_NumRefs = 0;
    g_NumMI = 0;
    g_LocalComTypeNum = 0;
    g_nEATableRef = 0;

    g_fCustomInstructionEncodingSystem = FALSE;

    if (rAsmRefName != NULL)
    {
        for (int i = 0; (unsigned)i < ulNumAsmRefs; i++)
        {
            if (rAsmRefName[i] != NULL) VDELETE(rAsmRefName[i]);
        }
        VDELETE(rAsmRefName);
        ulNumAsmRefs = 0;
    }

    if (g_rchCA != NULL)
        VDELETE(g_rchCA);

    if (g_cl_list != NULL) VDELETE(g_cl_list);
    if (g_cl_enclosing != NULL) VDELETE(g_cl_enclosing);
    if (g_pmi_list != NULL) SDELETE(g_pmi_list);
    if (g_dups != NULL) SDELETE(g_dups);
    if (g_enum_td_type != NULL) VDELETE(g_enum_td_type);
    if (g_enum_tr_type != NULL) VDELETE(g_enum_tr_type);
    if (g_asmref_import != NULL)
    {
        for (DWORD i = 0; i < g_NumAsmRefs; i++)
        {
            if (g_asmref_import[i] != NULL)
                g_asmref_import[i]->Release();
        }
        VDELETE(g_asmref_import);
        g_NumAsmRefs = 0;
    }
} // Cleanup

void Uninit()
{
    if (g_pPtrTags != NULL)
    {
        SDELETE(g_pPtrTags);
    }
    if (g_pPtrSize != NULL)
    {
        SDELETE(g_pPtrSize);
    }
    if (g_pmi_list != NULL)
    {
        SDELETE(g_pmi_list);
    }
    if (g_dups != NULL) SDELETE(g_dups);
    if (g_refs != NULL) SDELETE(g_refs);
    if (g_pLocalComType != NULL)
    {
        SDELETE(g_pLocalComType);
    }
    if (g_prVTableRef != NULL)
    {
        SDELETE(g_prVTableRef);
    }
    if (g_prEATableRef != NULL)
    {
        SDELETE(g_prEATableRef);
    }
    if (g_szBuf_KEYWORD != NULL)
    {
        SDELETE(g_szBuf_KEYWORD);
    }
    if (g_szBuf_COMMENT != NULL)
    {
        SDELETE(g_szBuf_COMMENT);
    }
    if (g_szBuf_ERRORMSG != NULL)
    {
        SDELETE(g_szBuf_ERRORMSG);
    }
    if (g_szBuf_ANCHORPT != NULL)
    {
        SDELETE(g_szBuf_ANCHORPT);
    }
    if (g_szBuf_JUMPPT != NULL)
    {
        SDELETE(g_szBuf_JUMPPT);
    }
    if (g_szBuf_UnquotedProperName != NULL)
    {
        SDELETE(g_szBuf_UnquotedProperName);
    }
    if (g_szBuf_ProperName != NULL)
    {
        SDELETE(g_szBuf_ProperName);
    }
} // Uninit

HRESULT IsClassRefInScope(mdTypeRef classref)
{
    HRESULT     hr = S_OK;
    const char  *pszNameSpace;
    const char  *pszClassName;
    mdTypeDef   classdef;
    mdToken     tkRes;

    IfFailRet(g_pImport->GetNameOfTypeRef(classref, &pszNameSpace, &pszClassName));
    MAKE_NAME_IF_NONE(pszClassName,classref);
    IfFailRet(g_pImport->GetResolutionScopeOfTypeRef(classref, &tkRes));

    hr = g_pImport->FindTypeDef(pszNameSpace, pszClassName,
        (TypeFromToken(tkRes) == mdtTypeRef) ? tkRes : mdTokenNil, &classdef);

    return hr;
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
BOOL EnumClasses()
{
    HRESULT         hr;
    HENUMInternal   hEnum;
    ULONG           i = 0,j;
    //char            szString[1024];
    HENUMInternal   hBody;
    HENUMInternal   hDecl;

    if(g_cl_list) VDELETE(g_cl_list);
    if(g_cl_enclosing) VDELETE(g_cl_enclosing);
    if (g_pmi_list) SDELETE(g_pmi_list);
    if (g_dups) SDELETE(g_dups);
    if (g_enum_td_type) VDELETE(g_enum_td_type);
    if (g_enum_tr_type) VDELETE(g_enum_tr_type);
    if (g_asmref_import)
    {
        for (DWORD nIndex = 0; nIndex < g_NumAsmRefs; nIndex++)
        {
            if (g_asmref_import[nIndex] != NULL)
                g_asmref_import[nIndex]->Release();
        }
        VDELETE(g_asmref_import);
        g_NumAsmRefs = 0;
    }
    //--------------------------------------------------------------
    if (FAILED(g_pImport->EnumAllInit(mdtTypeRef,&hEnum)))
    {
        printError(g_pFile, "MetaData error: cannot enumerate all TypeRefs");
        return FALSE;
    }
    g_NumTypeRefs = g_pImport->EnumGetCount(&hEnum);
    g_pImport->EnumClose(&hEnum);

    if(g_NumTypeRefs)
    {
        g_enum_tr_type = new BYTE[g_NumTypeRefs+1];
        if(g_enum_tr_type == NULL) return FALSE;
        memset(g_enum_tr_type,0xFF,g_NumTypeRefs+1);
    }
    //--------------------------------------------------------------
    if (FAILED(g_pImport->EnumAllInit(mdtAssemblyRef, &hEnum)))
    {
        printError(g_pFile, "MetaData error: cannot enumerate all AssemblyRefs");
        return FALSE;
    }
    g_NumAsmRefs = g_pImport->EnumGetCount(&hEnum);
    g_pImport->EnumClose(&hEnum);
    if(g_NumAsmRefs)
    {
        g_asmref_import = new IMDInternalImport*[g_NumAsmRefs+1];
        if(g_asmref_import == NULL) return FALSE;
        memset(g_asmref_import,0,(g_NumAsmRefs+1)*sizeof(IMDInternalImport*));
    }
    //--------------------------------------------------------------
    hr = g_pImport->EnumTypeDefInit(
        &hEnum);
    if (FAILED(hr))
    {
        printError(g_pFile,RstrUTF(IDS_E_CLSENUM));
        return FALSE;
    }

    g_NumClasses = g_pImport->EnumGetCount(&hEnum);

    g_tkClassToDump = 0;

    g_NumMI = 0;
    g_NumDups = 0;

    if(g_NumClasses == 0) return TRUE;

    g_enum_td_type = new BYTE[g_NumClasses+1];
    if(g_enum_td_type == NULL) return FALSE;
    memset(g_enum_td_type,0xFF,g_NumClasses+1);

    g_cl_list = new mdToken[g_NumClasses];
    if(g_cl_list == NULL) return FALSE;

    g_cl_enclosing = new mdToken[g_NumClasses];
    if(g_cl_enclosing == NULL)
    {
        VDELETE(g_cl_list);
        return FALSE;
    }

    g_pmi_list = new DynamicArray<MIDescriptor>;
    if(g_pmi_list == NULL)
    {
        VDELETE(g_cl_enclosing);
        VDELETE(g_cl_list);
        return FALSE;
    }

    g_dups = new DynamicArray<mdToken>;
    if(g_dups == NULL)
    {
        SDELETE(g_pmi_list);
        VDELETE(g_cl_enclosing);
        VDELETE(g_cl_list);
        return FALSE;
    }

    // fill the list of typedef tokens
    while(g_pImport->EnumNext(&hEnum, &g_cl_list[i]))
    {
        mdToken     tkEnclosing;

        if (g_Mode == MODE_DUMP_CLASS || g_Mode == MODE_DUMP_CLASS_METHOD || g_Mode == MODE_DUMP_CLASS_METHOD_SIG)
        {
            CQuickBytes out;

            // we want plain class name without token values
            BOOL fDumpTokens = g_fDumpTokens;
            g_fDumpTokens = FALSE;

            PAL_CPP_TRY
            {
                if (strcmp(PrettyPrintClass(&out, g_cl_list[i], g_pImport), g_pszClassToDump) == 0)
                {
                    g_tkClassToDump = g_cl_list[i];
                }
            }
            PAL_CPP_CATCH_ALL
            { }
            PAL_CPP_ENDTRY;

            g_fDumpTokens = fDumpTokens;
        }
        g_cl_enclosing[i] = mdTypeDefNil;

        hr = g_pImport->GetNestedClassProps(g_cl_list[i],&tkEnclosing);
        if (SUCCEEDED(hr) && RidFromToken(tkEnclosing)) // No need to check token validity here, it's done later
            g_cl_enclosing[i] = tkEnclosing;
        if (SUCCEEDED(g_pImport->EnumMethodImplInit(g_cl_list[i],&hBody,&hDecl)))
        {
            if ((j = g_pImport->EnumMethodImplGetCount(&hBody,&hDecl)))
            {
                mdToken tkBody,tkDecl,tkBodyParent;
                for (ULONG k = 0; k < j; k++)
                {
                    if (g_pImport->EnumMethodImplNext(&hBody,&hDecl,&tkBody,&tkDecl) == S_OK)
                    {
                        if (SUCCEEDED(g_pImport->GetParentToken(tkBody,&tkBodyParent)))
                        {
                            (*g_pmi_list)[g_NumMI].tkClass = g_cl_list[i];
                            (*g_pmi_list)[g_NumMI].tkBody = tkBody;
                            (*g_pmi_list)[g_NumMI].tkDecl = tkDecl;
                            (*g_pmi_list)[g_NumMI].tkBodyParent = tkBodyParent;
                            g_NumMI++;
                        }
                    }
                }
            }
            g_pImport->EnumMethodImplClose(&hBody,&hDecl);
        }
        i++;
    }
    g_pImport->EnumClose(&hEnum);
    // check nesting consistency (circular nesting, invalid enclosers)
    for(i = 0; i < g_NumClasses; i++)
    {
        mdToken tkThis = g_cl_list[i];
        mdToken tkEncloser = g_cl_enclosing[i];
        mdToken tkPrevLevel = tkThis;
        while(tkEncloser != mdTypeDefNil)
        {
            if(tkThis == tkEncloser)
            {
                sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_SELFNSTD),tkThis);
                printError(g_pFile,szString);
                g_cl_enclosing[i] = mdTypeDefNil;
                break;
            }
            else
            {
                for(j = 0; (j < g_NumClasses)&&(tkEncloser != g_cl_list[j]); j++);
                if(j == g_NumClasses)
                {
                    sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_NOENCLOS),
                        tkPrevLevel,tkEncloser);
                    printError(g_pFile,szString);
                    g_cl_enclosing[i] = mdTypeDefNil;
                    break;
                }
                else
                {
                    tkPrevLevel = tkEncloser;
                    tkEncloser = g_cl_enclosing[j];
                }
            }
        } // end while(tkEncloser != mdTypeDefNil)
    } // end for(i = 0; i < g_NumClasses; i++)

    // register all class dups
    const char *pszClassName;
    const char *pszNamespace;
    const char *pszClassName1;
    const char *pszNamespace1;

    if (FAILED(g_pImport->GetNameOfTypeDef(
        g_cl_list[0],
        &pszClassName,
        &pszNamespace)))
    {
        char sz[2048];
        sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), g_cl_list[0]);
        printLine(g_pFile, sz);
        return FALSE;
    }
    if((g_cl_enclosing[0]==mdTypeDefNil)
      &&(0==strcmp(pszClassName,"<Module>"))
      &&(*pszNamespace == 0))
    {
        (*g_dups)[g_NumDups++] = g_cl_list[0];
    }
    for(i = 1; i < g_NumClasses; i++)
    {
        if (FAILED(g_pImport->GetNameOfTypeDef(
            g_cl_list[i],
            &pszClassName,
            &pszNamespace)))
        {
            char sz[2048];
            sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), g_cl_list[i]);
            printLine(g_pFile, sz);
            return FALSE;
        }

        for(j = 0; j < i; j++)
        {
            if (FAILED(g_pImport->GetNameOfTypeDef(
                g_cl_list[j],
                &pszClassName1,
                &pszNamespace1)))
            {
                char sz[2048];
                sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), g_cl_list[j]);
                printLine(g_pFile, sz);
                return FALSE;
            }

            if((g_cl_enclosing[i]==g_cl_enclosing[j])
              &&(0==strcmp(pszClassName,pszClassName1))
              &&(0==strcmp(pszNamespace,pszNamespace1)))
            {
                (*g_dups)[g_NumDups++] = g_cl_list[i];
                break;
            }
        }
    } // end for(i = 1; i < g_NumClasses; i++)

    //register all field and method dups
    for(i = 0; i <= g_NumClasses; i++)
    {
        HENUMInternal   hEnumMember;
        mdToken         *pMemberList = NULL;
        DWORD           NumMembers,k;

        //  methods
        if (i != 0)
        {
            hr = g_pImport->EnumInit(mdtMethodDef, g_cl_list[i-1], &hEnumMember);
        }
        else
        {
            hr = g_pImport->EnumGlobalFunctionsInit(&hEnumMember);
        }
        if (FAILED(hr))
        {
            printLine(g_pFile,RstrUTF(IDS_E_MEMBRENUM));
            return FALSE;
        }
        NumMembers = g_pImport->EnumGetCount(&hEnumMember);
        pMemberList = new mdToken[NumMembers];
        for (j = 0; g_pImport->EnumNext(&hEnumMember, &pMemberList[j]); j++);
        _ASSERTE(j == NumMembers);
        g_pImport->EnumClose(&hEnumMember);
        for (j = 1; j < NumMembers; j++)
        {
            const char *pszName;
            ULONG cSig;
            PCCOR_SIGNATURE pSig;
            if (FAILED(g_pImport->GetNameOfMethodDef(pMemberList[j], &pszName)) ||
                FAILED(g_pImport->GetSigOfMethodDef(pMemberList[j], &cSig, &pSig)))
            {
                char sz[2048];
                sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), pMemberList[j]);
                printLine(g_pFile, sz);
                return FALSE;
            }
            for (k = 0; k < j; k++)
            {
                const char *szName1;
                if (FAILED(g_pImport->GetNameOfMethodDef(pMemberList[k], &szName1)))
                {
                    char sz[2048];
                    sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), pMemberList[k]);
                    printLine(g_pFile, sz);
                    return FALSE;
                }
                if (strcmp(pszName, szName1) == 0)
                {
                    ULONG cSig1;
                    PCCOR_SIGNATURE pSig1;
                    if (FAILED(g_pImport->GetSigOfMethodDef(pMemberList[k], &cSig1, &pSig1)))
                    {
                        char sz[2048];
                        sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), pMemberList[k]);
                        printLine(g_pFile, sz);
                        return FALSE;
                    }
                    if((cSig == cSig1)&&(0==memcmp(pSig,pSig1,cSig)))
                    {
                        (*g_dups)[g_NumDups++] = pMemberList[j];
                        break;
                    }
                }
            }
        }
        VDELETE(pMemberList);

        //  fields
        if (i != 0)
        {
            hr = g_pImport->EnumInit(mdtFieldDef, g_cl_list[i-1], &hEnumMember);
        }
        else
        {
            hr = g_pImport->EnumGlobalFieldsInit(&hEnumMember);
        }
        if (FAILED(hr))
        {
            printLine(g_pFile,RstrUTF(IDS_E_MEMBRENUM));
            return FALSE;
        }
        NumMembers = g_pImport->EnumGetCount(&hEnumMember);
        pMemberList = new mdToken[NumMembers];
        for (j = 0; g_pImport->EnumNext(&hEnumMember, &pMemberList[j]); j++);
        _ASSERTE(j == NumMembers);
        g_pImport->EnumClose(&hEnumMember);
        for (j = 1; j < NumMembers; j++)
        {
            const char *pszName;
            ULONG cSig;
            PCCOR_SIGNATURE pSig;
            if (FAILED(g_pImport->GetNameOfFieldDef(pMemberList[j], &pszName)) ||
                FAILED(g_pImport->GetSigOfFieldDef(pMemberList[j], &cSig, &pSig)))
            {
                char sz[2048];
                sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), pMemberList[j]);
                printLine(g_pFile, sz);
                return FALSE;
            }
            for (k = 0; k < j; k++)
            {
                const char *szName1;
                if (FAILED(g_pImport->GetNameOfFieldDef(pMemberList[k], &szName1)))
                {
                    char sz[2048];
                    sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), pMemberList[k]);
                    printLine(g_pFile, sz);
                    return FALSE;
                }
                if (strcmp(pszName, szName1) == 0)
                {
                    ULONG cSig1;
                    PCCOR_SIGNATURE pSig1;
                    if (FAILED(g_pImport->GetSigOfFieldDef(pMemberList[k], &cSig1, &pSig1)))
                    {
                        char sz[2048];
                        sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), pMemberList[k]);
                        printLine(g_pFile, sz);
                        return FALSE;
                    }
                    if((cSig == cSig1)&&(0==memcmp(pSig,pSig1,cSig)))
                    {
                        (*g_dups)[g_NumDups++] = pMemberList[j];
                        break;
                    }
                }
            }
        }
        VDELETE(pMemberList);

    } // end for(i = 0; i <= g_NumClasses; i++)
    return TRUE;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

void DumpMscorlib(void* GUICookie)
{
    // In the CoreCLR with reference assemblies and redirection it is more difficult to determine if
    // a particular Assembly is the System assembly, like mscorlib.dll is for the Desktop CLR.
    // In the CoreCLR runtimes, the System assembly can be System.Private.CoreLib.dll, System.Runtime.dll
    // or netstandard.dll and in the future a different Assembly name could be used.
    // We now determine the identity of the System assembly by querying if the Assembly defines the
    // well known type System.Object as that type must be defined by the System assembly
    // If this type is defined then we will output the ".mscorlib" directive to indicate that this
    // assembly is the System assembly.
    //
    mdTypeDef tkObjectTypeDef = mdTypeDefNil;

    // Lookup the type System.Object and see it it has a type definition in this assembly
    if (SUCCEEDED(g_pPubImport->FindTypeDefByName(W("System.Object"), mdTypeDefNil, &tkObjectTypeDef)))
    {
        if (tkObjectTypeDef != mdTypeDefNil)
        {
            // We do have a type definition for System.Object in this assembly
            //
            DWORD dwClassAttrs = 0;
            mdToken tkExtends = mdTypeDefNil;

            // Retrieve the type def properties as well, so that we can check a few more things about
            // the System.Object type
            //
            if (SUCCEEDED(g_pPubImport->GetTypeDefProps(tkObjectTypeDef, NULL, NULL, 0, &dwClassAttrs, &tkExtends)))
            {
                bool bExtends = g_pPubImport->IsValidToken(tkExtends);
                bool isClass = ((dwClassAttrs & tdClassSemanticsMask) == tdClass);

                // We also check the type properties to make sure that we have a class and not a Value type definition
                // and that this type definition isn't extending another type.
                //
                if (isClass & !bExtends)
                {
                    // We will mark this assembly with the System assembly directive: .mscorlib
                    //
                    printLine(GUICookie, "");
                    sprintf_s(szString, SZSTRING_SIZE, "%s%s ", g_szAsmCodeIndent, KEYWORD(".mscorlib"));
                    printLine(GUICookie, szString);
                    printLine(GUICookie, "");
                }
            }
        }
    }

}
void DumpTypelist(void* GUICookie)
{
    if(g_NumClasses > 1)
    {
        DWORD i;
        CQuickBytes out;
        printLine(GUICookie,"");
        sprintf_s(szString,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".typelist"));
        printLine(GUICookie,szString);
        sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,SCOPE());
        printLine(GUICookie,szString);
        strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"  ");

        for(i = 0; i < g_NumClasses; i++)
        {
            out.Shrink(0);
            sprintf_s(szString,SZSTRING_SIZE, "%s%s",g_szAsmCodeIndent, PrettyPrintClass(&out, g_cl_list[i], g_pImport));
            printLine(GUICookie,szString);
        }
        g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
        sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,UNSCOPE());
        printLine(GUICookie,szString);
        printLine(GUICookie,"");
    }

}
#define ELEMENT_TYPE_TYPEDEF (ELEMENT_TYPE_MAX+1)
BOOL EnumTypedefs()
{
    HENUMInternal   hEnum;
    ULONG           i,l;
    mdToken         tk;
    if (g_typedefs) SDELETE(g_typedefs);
    g_typedefs = new DynamicArray<TypeDefDescr>;
    g_NumTypedefs = 0;
    if (FAILED(g_pImport->EnumAllInit(mdtTypeSpec, &hEnum)))
    {
        return FALSE;
    }
    for (i = 0; g_pImport->EnumNext(&hEnum, &tk); i++)
    {
        ULONG cSig;
        PCCOR_SIGNATURE sig;
        if (FAILED(g_pImport->GetSigFromToken(tk, &cSig, &sig)))
        {
            return FALSE;
        }
        if (*sig == ELEMENT_TYPE_TYPEDEF)
        {
            TypeDefDescr* pTDD = &((*g_typedefs)[g_NumTypedefs]);
            pTDD->szName = (char*)sig+1;
            l = 2+(ULONG)strlen((char*)sig+1);
            pTDD->tkTypeSpec = GET_UNALIGNED_VAL32(sig + l);
            pTDD->tkSelf = tk;
            if (TypeFromToken(pTDD->tkTypeSpec) == mdtTypeSpec)
            {
                if (FAILED(g_pImport->GetSigFromToken(pTDD->tkTypeSpec,&(pTDD->cb), &(pTDD->psig))))
                {
                    return FALSE;
                }
            }
            else if (TypeFromToken(pTDD->tkTypeSpec) == mdtCustomAttribute)
            {
                l += sizeof(mdToken);
                pTDD->psig = sig + l;
                pTDD->cb = cSig - l;
            }
            else
            {
                pTDD->psig = NULL;
                pTDD->cb = 0;
            }
            g_NumTypedefs++;
        }
    }
    g_pImport->EnumClose(&hEnum);
    return TRUE;
}

void DumpTypedefs(void* GUICookie)
{
    DWORD i;
    char* szptr;
    CQuickBytes out;
    printLine(GUICookie,"");
    for(i = 0; i < g_NumTypedefs; i++)
    {
        TypeDefDescr* pTDD = &((*g_typedefs)[i]);
        szptr = &szString[0];
        szString[0] = 0;
        szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,ANCHORPT(KEYWORD(".typedef"),pTDD->tkSelf));
        if(g_fDumpTokens)
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),pTDD->tkSelf);

        {
            ULONG n = g_NumTypedefs;
            DWORD tk = pTDD->tkTypeSpec;
            switch (TypeFromToken(tk))
            {
                default:
                    break;

                case mdtCustomAttribute:
                    printLine(GUICookie,szString);
                    strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"        ");
                    {
                        mdToken tkType;
                        mdToken tkOwner;
                        BYTE* pBlob=NULL;
                        ULONG uLen=0;
                        tkType = GET_UNALIGNED_VAL32(pTDD->psig);
                        tkOwner = GET_UNALIGNED_VAL32(pTDD->psig + sizeof(mdToken));
                        if(pTDD->cb > 2*sizeof(mdToken))
                        {
                            pBlob = (BYTE*)pTDD->psig + 2*sizeof(mdToken);
                            uLen = pTDD->cb - 2*sizeof(mdToken);
                        }
                        DumpCustomAttributeProps(0,tkType,tkOwner,pBlob,uLen,GUICookie,
                                                 (RidFromToken(tkOwner)!=0));

                    }
                    sprintf_s(szString,SZSTRING_SIZE,"%s %s %s", g_szAsmCodeIndent,KEYWORD("as"),
                            ProperName((*g_typedefs)[i].szName));
                    printLine(GUICookie,szString);
                    g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-8]=0;
                    continue;

                case mdtMethodDef:
                    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("method "));
                    break;

                case mdtFieldDef:
                    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("field "));
                    break;

                case mdtMemberRef:
                    {
                        PCCOR_SIGNATURE typePtr;
                        const char     *pszMemberName;
                        ULONG       cComSig;

                        if (FAILED(g_pImport->GetNameAndSigOfMemberRef(
                            tk,
                            &typePtr,
                            &cComSig,
                            &pszMemberName)))
                        {
                            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"ERROR ");
                            break;
                        }
                        unsigned callConv = CorSigUncompressData(typePtr);

                        if (isCallConv(callConv, IMAGE_CEE_CS_CALLCONV_FIELD))
                            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("field "));
                        else
                            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("method "));
                        break;
                    }
            }
            g_NumTypedefs = 0;
            PrettyPrintToken(szString, tk, g_pImport,g_pFile,0);
            g_NumTypedefs = n;
            szptr = &szString[strlen(szString)];
        }
        szptr+= sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," %s %s", KEYWORD("as"), ProperName((*g_typedefs)[i].szName));
        printLine(GUICookie,szString);
    }
}

BOOL PrintClassList()
{
    DWORD           i;
    BOOL            fSuccess = FALSE;
    //char    szString[1024];
    char*   szptr;

    if(g_NumClasses)
    {
        printLine(g_pFile,COMMENT("// Classes defined in this module:"));
        printLine(g_pFile,COMMENT("//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"));

        for (i = 0; i < g_NumClasses; i++)
        {
            const char *pszClassName;
            const char *pszNamespace;
            DWORD   dwClassAttrs;
            mdTypeRef crExtends;

            if (FAILED(g_pImport->GetNameOfTypeDef(
                g_cl_list[i],
                &pszClassName,
                &pszNamespace)))
            {
                printLine(g_pFile, COMMENT("// Invalid TypeDef record"));
                return FALSE;
            }
            MAKE_NAME_IF_NONE(pszClassName,g_cl_list[i]);
            // if this is the "<Module>" class (there is a misnomer) then skip it!
            if (FAILED(g_pImport->GetTypeDefProps(
                g_cl_list[i],
                &dwClassAttrs,
                &crExtends)))
            {
                printLine(g_pFile, COMMENT("// Invalid TypeDef record"));
                return FALSE;
            }
            szptr = &szString[0];
            szptr+=sprintf_s(szptr,SZSTRING_SIZE,"// ");
            if (IsTdInterface(dwClassAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"Interface ");
            //else if (IsTdValueType(dwClassAttrs))   szptr+=sprintf(szptr,"Value Class");
            //else if (IsTdUnmanagedValueType(dwClassAttrs)) szptr+=sprintf(szptr,"NotInGCHeap Value Class");
            else   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"Class ");

            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%-30s ", pszClassName);

            if (IsTdPublic(dwClassAttrs))           szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(public) ");
            if (IsTdAbstract(dwClassAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(abstract) ");
            if (IsTdAutoLayout(dwClassAttrs))       szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(auto) ");
            if (IsTdSequentialLayout(dwClassAttrs)) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(sequential) ");
            if (IsTdExplicitLayout(dwClassAttrs))   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(explicit) ");
            if (IsTdAnsiClass(dwClassAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(ansi) ");
            if (IsTdUnicodeClass(dwClassAttrs))     szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(unicode) ");
            if (IsTdAutoClass(dwClassAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(autochar) ");
            if (IsTdImport(dwClassAttrs))           szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(import) ");
            if (IsTdWindowsRuntime(dwClassAttrs))   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(windowsruntime) ");
            //if (IsTdEnum(dwClassAttrs))             szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(enum) ");
            if (IsTdSealed(dwClassAttrs))           szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(sealed) ");
            if (IsTdNestedPublic(dwClassAttrs))     szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(nested public) ");
            if (IsTdNestedPrivate(dwClassAttrs))    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(nested private) ");
            if (IsTdNestedFamily(dwClassAttrs))     szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(nested family) ");
            if (IsTdNestedAssembly(dwClassAttrs))   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(nested assembly) ");
            if (IsTdNestedFamANDAssem(dwClassAttrs))   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(nested famANDassem) ");
            if (IsTdNestedFamORAssem(dwClassAttrs))    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(nested famORassem) ");

            printLine(g_pFile,COMMENT(szString));
        }
        printLine(g_pFile,COMMENT("//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"));
        printLine(g_pFile,"");
    }
    else
        printLine(g_pFile,COMMENT("// No classes defined in this module"));
    fSuccess = TRUE;

    return fSuccess;
}

BOOL ValidateToken(mdToken tk, ULONG type = (ULONG) ~0)
{
    BOOL        bRtn;
    //char szString[1024];
    bRtn = g_pImport->IsValidToken(tk);
    if (!bRtn)
    {
        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_INVALIDTK), tk);
        printError(g_pFile,szString);
    }
    else if (type != (ULONG) ~0 && TypeFromToken(tk) != type)
    {
        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_UNEXPTYPE),
               TypeFromToken(type), TypeFromToken(tk));
        printError(g_pFile,szString);
        bRtn = FALSE;
    }
    return bRtn;
}


BOOL DumpModule(mdModuleRef mdMod)
{
    const char  *pszModName;
    //char szString[1024];
    if (FAILED(g_pImport->GetModuleRefProps(mdMod,&pszModName)))
    {
        pszModName = "Invalid ModuleRef record";
    }
    MAKE_NAME_IF_NONE(pszModName,mdMod);
    sprintf_s(szString,SZSTRING_SIZE,"%s%s \"%s\"",g_szAsmCodeIndent,KEYWORD(".import"),pszModName); // what about GUID and MVID?
    printLine(g_pFile,szString);
    return TRUE;
}

char* DumpPinvokeMap(DWORD   dwMappingFlags, const char  *szImportName,
                    mdModuleRef    mrImportDLL, __inout __nullterminated char* szString, void* GUICookie)
{
    const char  *szImportDLLName;
    char*   szptr = &szString[strlen(szString)];

    if (FAILED(g_pImport->GetModuleRefProps(mrImportDLL,&szImportDLLName)))
    {
        szImportDLLName = "Invalid ModuleRef record";
    }
    if(strlen(szImportDLLName) != 0)
    {
        szptr = DumpQString(GUICookie,
            (char*)szImportDLLName,
            g_szAsmCodeIndent,
            80);
    }

    //if(strlen(szImportDLLName))                   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"\"%s\"",szImportDLLName);
    //if(szImportName && strlen(szImportName))    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," as \"%s\"",szImportName);
    if(szImportName && strlen(szImportName))
    {
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD(" as "));
        szptr = DumpQString(GUICookie,
                            (char*)szImportName,
                            g_szAsmCodeIndent,
                            80);
    }
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD((char*)0));
    if(IsPmNoMangle(dwMappingFlags))            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," nomangle");
    if(IsPmCharSetAnsi(dwMappingFlags))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," ansi");
    if(IsPmCharSetUnicode(dwMappingFlags))      szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," unicode");
    if(IsPmCharSetAuto(dwMappingFlags))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," autochar");
    if(IsPmSupportsLastError(dwMappingFlags))   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," lasterr");
    if(IsPmCallConvWinapi(dwMappingFlags))      szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," winapi");
    if(IsPmCallConvCdecl(dwMappingFlags))       szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," cdecl");
    if(IsPmCallConvThiscall(dwMappingFlags))    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," thiscall");
    if(IsPmCallConvFastcall(dwMappingFlags))    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," fastcall");
    if(IsPmCallConvStdcall(dwMappingFlags))     szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," stdcall");
    if(IsPmBestFitEnabled(dwMappingFlags))      szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," bestfit:on");
    if(IsPmBestFitDisabled(dwMappingFlags))     szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," bestfit:off");
    if(IsPmThrowOnUnmappableCharEnabled(dwMappingFlags))      szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," charmaperror:on");
    if(IsPmThrowOnUnmappableCharDisabled(dwMappingFlags))     szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," charmaperror:off");
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD((char*)-1));
    return szptr;
}

void DumpByteArray(__inout __nullterminated char* szString, const BYTE* pBlob, ULONG ulLen, void* GUICookie)
{
    ULONG32 ulStrOffset = 0;
    ULONG32 j = 0;
    ULONG32 k = 0;
    ULONG32 m = 0;
    char sz[256];
    bool printsz = FALSE;
    char* szptr = NULL;
    BYTE byt = 0;


    ulStrOffset = (ULONG32) strlen(szString);
    szptr = &szString[ulStrOffset];
    if(!pBlob) ulLen = 0;
    for(j = 0, k=0, m=0; j < ulLen; j++,k++,m++)
    {
        if(k == 16)
        {
            if(printsz)
            {
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("  // %s"),sz);
            }
            printLine(GUICookie,szString);
            strcpy_s(szString,SZSTRING_SIZE,g_szAsmCodeIndent);
            for(k=(ULONG32) strlen(szString); k < ulStrOffset; k++) szString[k] = ' ';
            szString[k] = 0;
            szptr = &szString[ulStrOffset];
            k = 0;
            m = 0;
            printsz = FALSE;
        }
        bool bBreak = FALSE;
        PAL_CPP_TRY {
            byt = pBlob[j];
        }
        PAL_CPP_CATCH_ALL
        {
            strcat_s(szString, SZSTRING_SIZE,ERRORMSG("INVALID DATA ADDRESS"));
            bBreak = TRUE;
        }
        PAL_CPP_ENDTRY;

        if (bBreak)
            break;

        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%2.2X ",byt);
        if(isprint(byt))
        {
            if(g_fDumpRTF)
            {
                if((byt == '\\')||(byt=='{')||(byt=='}')) sz[m++]='\\';
                sz[m] = byt;
            }
            else if(g_fDumpHTML)
            {
                if(byt == '<') { sz[m] = 0; strcat_s(sz,256-m,LTN()); m+=(ULONG32)(strlen(LTN())); }
                else if(byt == '>') { sz[m] = 0; strcat_s(sz,256-m,GTN()); m+=(ULONG32)(strlen(GTN())); }
                else sz[m] = byt;
            }
            else sz[m] = byt;
            printsz = TRUE;
        }
        else sz[m] = '.';
        sz[m+1] = 0;
    }
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),") ");
    if(printsz)
    {
        for(j = k; j < 16; j++) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"   ");
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("// %s"),sz);
    }
}

mdToken ResolveTypeDefReflectionNotation(IMDInternalImport *pIMDI,
                                         LPCUTF8 szNamespace,
                                         __inout LPUTF8 szName,
                                         mdToken tkEncloser)
{
    mdToken tk = 0;
    LPUTF8 pch = strrchr(szName, '+');
    if(pch != NULL)
    {
        *pch = 0;
        tkEncloser = ResolveTypeDefReflectionNotation(pIMDI,szNamespace,szName,tkEncloser);
        szNamespace = "";
        szName = pch+1;
    }
    if(SUCCEEDED(pIMDI->FindTypeDef(szNamespace,szName,tkEncloser,&tk)))
        return tk;
    else
        return 0;
}

mdToken ResolveTypeRefReflectionNotation(IMDInternalImport *pIMDI,
                                         _In_ __nullterminated const char* szNamespace,
                                         __inout __nullterminated char* szName,
                                         mdToken tkResScope)
{
    mdToken tk = 0;
    char* pch = strrchr(szName, '+');
    if(pch != NULL)
    {
        *pch = 0;
        tkResScope = ResolveTypeRefReflectionNotation(pIMDI,szNamespace,szName,tkResScope);
        szNamespace = "";
        szName = pch+1;
    }
    if(SUCCEEDED(pIMDI->FindTypeRefByName((LPCSTR)szNamespace,(LPCSTR)szName,tkResScope,&tk)))
        return tk;
    else
        return 0;
}
mdToken ResolveReflectionNotation(BYTE* dataPtr,
                                  unsigned Lstr,
                                  IMDInternalImport *pIMDI,
                                  void* GUICookie)
{
    char* str = new char[Lstr+1];
    mdToken ret = 0;
    if(str)
    {
        char  szNamespaceDefault[] = "";
        char* szNamespace = szNamespaceDefault;
        char* szName = str;
        char* szAssembly = NULL;
        char  szAssemblyMscorlib[] = "mscorlib";
        char* pch;
        memcpy(str,dataPtr,Lstr);
        str[Lstr] = 0;
        //format: Namespace.Name, Assembly,...
        pch = strchr(str,',');
        if(pch)
        {
            *pch = 0;
            for(szAssembly = pch+1; *szAssembly == ' '; szAssembly++);
            pch = strchr(szAssembly,',');
            if(pch) *pch = 0;
        }
        pch = strrchr(str,'.');
        if(pch)
        {
            *pch = 0;
            szNamespace = str;
            szName = pch+1;
        }
        if(szAssembly == NULL)
        {
            // Look in TypeDefs
            mdToken tk = ResolveTypeDefReflectionNotation(pIMDI,szNamespace,szName,mdTypeDefNil);
            if(tk != 0)
                ret = tk;
            else
                // TypeDef not found, try TypeRef from mscorlib
                szAssembly = szAssemblyMscorlib;
        }
        if(szAssembly != NULL)
        {
            // Look in TypeRefs
            // First, identify resolution scope
            _ASSERTE(*szName);
            ULONG mAsmRefs = pIMDI->GetCountWithTokenKind(mdtAssemblyRef);
            if(mAsmRefs)
            {
                mdToken tkResScope = 0;
                mdToken tk=TokenFromRid(mdtAssemblyRef,1), tkmax=TokenFromRid(mdtAssemblyRef,mAsmRefs);
                LPCSTR szAsmRefName;
                // these are dummies
                const void* pPKT, *pHash;
                ULONG ulPKT,ulHash;
                AssemblyMetaDataInternal MD;
                DWORD dwFlags;

                for (;tk <= tkmax; tk++)
                {
                    if (FAILED(pIMDI->GetAssemblyRefProps(tk,&pPKT,&ulPKT,&szAsmRefName,&MD,&pHash,&ulHash,&dwFlags)))
                    {
                        continue;
                    }
                    if(0==strcmp(szAsmRefName,szAssembly))
                    {
                        tkResScope = tk;
                        break;
                    }
                }
                if(tkResScope)
                {
                    ret = ResolveTypeRefReflectionNotation(pIMDI,szNamespace,szName,tkResScope);
                }
            }
        }
    }
    VDELETE(str);
    return ret;
}

unsigned UnderlyingTypeOfEnumTypeDef(mdToken tk, IMDInternalImport *pIMDI)
{
    // make sure it's a TypeDef
    if(TypeFromToken(tk) != mdtTypeDef) return 0;

    // make sure it's an enum
    mdToken tkParent;
    DWORD dwAttr;
    if (FAILED(pIMDI->GetTypeDefProps(tk,&dwAttr,&tkParent)))
    {
        return 0;
    }
    if(RidFromToken(tkParent)==0) return 0;
    LPCSTR szName, szNamespace;
    switch(TypeFromToken(tkParent))
    {
        case mdtTypeDef:
            if (FAILED(pIMDI->GetNameOfTypeDef(tkParent, &szName, &szNamespace)))
            {
                return 0;
            }
            break;

        case mdtTypeRef:
            if (FAILED(pIMDI->GetNameOfTypeRef(tkParent, &szNamespace, &szName)))
            {
                return 0;
            }
            break;

        default:
            return 0;
    }

    if (strcmp(szName,"Enum") != 0 || strcmp(szNamespace,"System") != 0)
    {
        // the parent type is not System.Enum so this type has no underlying type
        return 0;
    }

    // OK, it's an enum; find its instance field and get its type
    HENUMInternal hEnum;
    mdToken tkField;
    if (FAILED(pIMDI->EnumInit(mdtFieldDef,tk,&hEnum)))
    {
        return 0;
    }
    while(pIMDI->EnumNext(&hEnum,&tkField))
    {
        if (FAILED(pIMDI->GetFieldDefProps(tkField, &dwAttr)))
        {
            continue;
        }
        if (IsFdStatic(dwAttr))
        {
            continue;
        }
        PCCOR_SIGNATURE psig;
        if (FAILED(pIMDI->GetSigOfFieldDef(tkField,(ULONG*)&dwAttr, &psig)))
        {
            continue;
        }
        pIMDI->EnumClose(&hEnum);
        return (unsigned) *(psig+1);
    }
    // no instance field found -- error!
    pIMDI->EnumClose(&hEnum);
    return 0;
}
mdToken TypeRefToTypeDef(mdToken tk, IMDInternalImport *pIMDI, IMDInternalImport **ppIMDInew)
{
    mdToken tkEncloser = mdTypeDefNil;
    mdToken tkTypeDef = mdTypeDefNil;
    *ppIMDInew = NULL;

    // get the resolution scope of TypeRef
    mdToken tkRS;
    if (FAILED(pIMDI->GetResolutionScopeOfTypeRef(tk, &tkRS)))
    {
        goto AssignAndReturn;
    }
    if (TypeFromToken(tkRS) == mdtTypeRef)
        tkEncloser =  TypeRefToTypeDef(tkRS,pIMDI,ppIMDInew);
    else if (TypeFromToken(tkRS) == mdtAssemblyRef)
    {
        *ppIMDInew = g_asmref_import[RidFromToken(tkRS)];
        if (*ppIMDInew == NULL)
        {
            // get that assembly and open IMDInternalImport
            IMetaDataAssemblyImport* pAssemblyImport;
            if (FAILED(g_pPubImport->QueryInterface(IID_IMetaDataAssemblyImport, (void**) &pAssemblyImport)))
                goto AssignAndReturn;

            const void *pPKT, *pHash;
            ULONG cHash,cName;
            WCHAR wzName[2048];
            ASSEMBLYMETADATA md;
            WCHAR       wzLocale[1024];
            DWORD       dwFlags;
            IUnknown* pIAMDI[64];
            memset(&md,0,sizeof(ASSEMBLYMETADATA));
            md.szLocale = wzLocale;
            md.cbLocale = 1024;

            struct Param
            {
                IMetaDataAssemblyImport* pAssemblyImport;
                WCHAR *wzName;
                IUnknown **pIAMDI;
                ULONG cPKT;
            } param;
            param.pAssemblyImport = pAssemblyImport;
            param.wzName = wzName;
            param.pIAMDI = pIAMDI;

            pAssemblyImport->GetAssemblyRefProps(tkRS,&pPKT,&param.cPKT,wzName,2048,&cName,&md,&pHash,&cHash,&dwFlags);

            PAL_TRY(Param *, pParam, &param) {
                if(FAILED(pParam->pAssemblyImport->FindAssembliesByName(NULL,NULL,(LPCWSTR)pParam->wzName,pParam->pIAMDI,64,&pParam->cPKT)))
                    pParam->cPKT=0;
            } PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER) {
                param.cPKT=0;
            } PAL_ENDTRY

            pAssemblyImport->Release();
            if(param.cPKT == 0) goto AssignAndReturn;
            _ASSERTE(pIAMDI[0] != NULL);

            IUnknown *pUnk;
            if(FAILED(pIAMDI[0]->QueryInterface(IID_IUnknown, (void**)&pUnk))) goto AssignAndReturn;

            if (FAILED(GetMetaDataInternalInterfaceFromPublic(
                pUnk,
                IID_IMDInternalImport,
                (LPVOID *)ppIMDInew)))
            {
                goto AssignAndReturn;
            }
            _ASSERTE(*ppIMDInew != NULL);
            g_asmref_import[RidFromToken(tkRS)] = *ppIMDInew;
            pUnk->Release();
            for(cHash=0; cHash<param.cPKT; cHash++)
                if(pIAMDI[cHash]) pIAMDI[cHash]->Release();
        }
    }
    if (*ppIMDInew != NULL)
    {
        LPCSTR szName, szNamespace;
        if (FAILED(pIMDI->GetNameOfTypeRef(tk, &szNamespace, &szName)))
        {
            tkTypeDef = mdTypeDefNil;
            goto AssignAndReturn;
        }

        if (FAILED((*ppIMDInew)->FindTypeDef(szNamespace,szName,tkEncloser,&tkTypeDef)))
        {
            tkTypeDef = mdTypeDefNil;
        }
    }
AssignAndReturn:
    return tkTypeDef;
}
unsigned UnderlyingTypeOfEnum(mdToken tk, IMDInternalImport *pIMDI)
{
    unsigned uRet = 0;
    unsigned ix = RidFromToken(tk);
    if(TypeFromToken(tk)==mdtTypeDef)
    {
        if(g_enum_td_type[ix] == 0xFF)
        {
            g_enum_td_type[ix] = (BYTE)UnderlyingTypeOfEnumTypeDef(tk,pIMDI);
        }
        return (unsigned)g_enum_td_type[ix];
    }
    else if(TypeFromToken(tk)==mdtTypeRef)
    {
        if(g_enum_tr_type[ix] == 0xFF)
        {
            IMDInternalImport *pIMDInew = NULL;
            mdToken tkTypeDef = TypeRefToTypeDef(tk,pIMDI, &pIMDInew);
            if((RidFromToken(tkTypeDef)!=0)&&(pIMDInew != NULL))
            {
                uRet = UnderlyingTypeOfEnumTypeDef(tkTypeDef,pIMDInew);
            }
            g_enum_tr_type[ix] = (BYTE)uRet;
        }
        return (unsigned)g_enum_tr_type[ix];
    }
    else return 0;
}

/**************************************************************************/
/* move 'ptr past the exactly one type description */

BYTE* skipType(BYTE* ptr)
{
    mdToken  tk;
AGAIN:
    switch(*ptr++) {
        case ELEMENT_TYPE_VOID         :
        case ELEMENT_TYPE_BOOLEAN      :
        case ELEMENT_TYPE_CHAR         :
        case ELEMENT_TYPE_I1           :
        case ELEMENT_TYPE_U1           :
        case ELEMENT_TYPE_I2           :
        case ELEMENT_TYPE_U2           :
        case ELEMENT_TYPE_I4           :
        case ELEMENT_TYPE_U4           :
        case ELEMENT_TYPE_I8           :
        case ELEMENT_TYPE_U8           :
        case ELEMENT_TYPE_R4           :
        case ELEMENT_TYPE_R8           :
        case ELEMENT_TYPE_U            :
        case ELEMENT_TYPE_I            :
        case ELEMENT_TYPE_STRING       :
        case ELEMENT_TYPE_OBJECT       :
        case ELEMENT_TYPE_TYPEDBYREF   :
        case ELEMENT_TYPE_SENTINEL     :
        case SERIALIZATION_TYPE_TYPE   :
        case SERIALIZATION_TYPE_TAGGED_OBJECT :
                /* do nothing */
                break;

        case SERIALIZATION_TYPE_ENUM   :
            {
                unsigned Lstr = CorSigUncompressData((PCCOR_SIGNATURE&)ptr);
                ptr += Lstr;
                break;
            }

        case ELEMENT_TYPE_VALUETYPE   :
        case ELEMENT_TYPE_CLASS        :
                ptr += CorSigUncompressToken(ptr, &tk);
                break;

        case ELEMENT_TYPE_CMOD_REQD    :
        case ELEMENT_TYPE_CMOD_OPT     :
                ptr += CorSigUncompressToken(ptr, &tk);
                goto AGAIN;

        case ELEMENT_TYPE_ARRAY         :
                {
                    ptr = skipType(ptr);                    // element Type
                    unsigned rank = CorSigUncompressData((PCCOR_SIGNATURE&) ptr);
                    if (rank != 0)
                    {
                        unsigned numSizes = CorSigUncompressData((PCCOR_SIGNATURE&) ptr);
                        while(numSizes > 0)
                        {
                            CorSigUncompressData((PCCOR_SIGNATURE&) ptr);
                                                        --numSizes;
                        }
                        unsigned numLowBounds = CorSigUncompressData((PCCOR_SIGNATURE&) ptr);
                        while(numLowBounds > 0)
                        {
                            CorSigUncompressData((PCCOR_SIGNATURE&) ptr);
                                                        --numLowBounds;
                        }
                    }
                }
                break;

                // Modifiers or depedant types
        case ELEMENT_TYPE_PINNED                :
        case ELEMENT_TYPE_PTR                   :
        case ELEMENT_TYPE_BYREF                 :
        case ELEMENT_TYPE_SZARRAY               :
                // tail recursion optimization
                // ptr = skipType(ptr, fFixupType);
                // break
                goto AGAIN;

        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
                CorSigUncompressData((PCCOR_SIGNATURE&) ptr);  // bound
                break;

        case ELEMENT_TYPE_FNPTR:
                {
                    CorSigUncompressData((PCCOR_SIGNATURE&) ptr);    // calling convention
                    unsigned argCnt = CorSigUncompressData((PCCOR_SIGNATURE&) ptr);    // arg count
                    ptr = skipType(ptr);                             // return type
                    while(argCnt > 0)
                    {
                        ptr = skipType(ptr);
                        --argCnt;
                    }
                }
                break;

        case ELEMENT_TYPE_GENERICINST:
               {
                   ptr = skipType(ptr);                 // type constructor
                   unsigned argCnt = CorSigUncompressData((PCCOR_SIGNATURE&)ptr);               // arg count
                   while(argCnt > 0) {
                       ptr = skipType(ptr);
                       --argCnt;
                   }
               }
               break;

        default:
        case ELEMENT_TYPE_END                   :
                _ASSERTE(!"Unknown Type");
                break;
    }
    return(ptr);
}


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
BYTE* PrettyPrintCABlobValue(PCCOR_SIGNATURE &typePtr,
                             BYTE* dataPtr,
                             BYTE* dataEnd,
                             CQuickBytes* out,
                             IMDInternalImport *pIMDI,
                             void* GUICookie)
{
    char str[64];
    char appendix[64];
    int typ;
    BOOL Reiterate;
    BOOL CloseParenthesis;
    unsigned numElements = 1;
    unsigned n,Lstr;
    unsigned underType;
    mdToken tk;

    appendix[0] = 0;
    do {
        if(dataPtr >= dataEnd)
        {
            _ASSERTE(!"CA blob too short");
            return FALSE;
        }
        Reiterate = FALSE;
        CloseParenthesis = TRUE;
        switch(typ = *typePtr++) {
            case ELEMENT_TYPE_VOID          :
                return NULL;
            case ELEMENT_TYPE_BOOLEAN       :
                appendStr(out,KEYWORD("bool"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    appendStr(out,(*dataPtr)? KEYWORD("true"):KEYWORD("false"));
                    dataPtr++;
                }
                break;
            case ELEMENT_TYPE_CHAR          :
                appendStr(out,KEYWORD("char"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    sprintf_s(str,64,"0x%4.4X",(WORD)GET_UNALIGNED_VAL16(dataPtr));
                    appendStr(out,str);
                    dataPtr += 2;
                }
                break;
            case ELEMENT_TYPE_I1            :
                appendStr(out,KEYWORD("int8"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    sprintf_s(str,64,"%d",*((char*)dataPtr));
                    appendStr(out,str);
                    dataPtr ++;
                }
                break;
            case ELEMENT_TYPE_U1            :
                appendStr(out,KEYWORD("uint8"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    sprintf_s(str,64,"%d",*dataPtr);
                    appendStr(out,str);
                    dataPtr ++;
                }
                break;
            case ELEMENT_TYPE_I2            :
                appendStr(out,KEYWORD("int16"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    sprintf_s(str,64,"%d",GET_UNALIGNED_VAL16(dataPtr));
                    appendStr(out,str);
                    dataPtr +=2;
                }
                break;
            case ELEMENT_TYPE_U2            :
                appendStr(out,KEYWORD("uint16"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    sprintf_s(str,64,"%d",(WORD)GET_UNALIGNED_VAL16(dataPtr));
                    appendStr(out,str);
                    dataPtr +=2;
                }
                break;
            case ELEMENT_TYPE_I4            :
                appendStr(out,KEYWORD("int32"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    sprintf_s(str,64,"%d",GET_UNALIGNED_VAL32(dataPtr));
                    appendStr(out,str);
                    dataPtr +=4;
                }
                break;
            case ELEMENT_TYPE_U4            :
                appendStr(out,KEYWORD("uint32"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    sprintf_s(str,64,"%d",(unsigned)GET_UNALIGNED_VAL32(dataPtr));
                    appendStr(out,str);
                    dataPtr +=4;
                }
                break;
            case ELEMENT_TYPE_I8            :
                appendStr(out,KEYWORD("int64"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    sprintf_s(str,64,"%I64d",GET_UNALIGNED_VAL64(dataPtr));
                    appendStr(out,str);
                    dataPtr +=8;
                }
                break;
            case ELEMENT_TYPE_U8            :
                appendStr(out,KEYWORD("uint64"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    sprintf_s(str,64,"%I64d",(ULONGLONG)GET_UNALIGNED_VAL64(dataPtr));
                    appendStr(out,str);
                    dataPtr +=8;
                }
                break;
            case ELEMENT_TYPE_R4            :
                appendStr(out,KEYWORD("float32"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    _gcvt_s(str,64,*((float*)dataPtr), 8);
                    float df = (float)atof(str);
                    // Must compare as underlying bytes, not floating point otherwise optmizier will
                    // try to enregister and comapre 80-bit precision number with 32-bit precision number!!!!
                    if((*(ULONG*)&df != (ULONG)GET_UNALIGNED_VAL32(dataPtr))||IsSpecialNumber(str))
                        sprintf_s(str, 64,"0x%08X",(ULONG)GET_UNALIGNED_VAL32(dataPtr));
                    appendStr(out,str);
                    dataPtr +=4;
                }
                break;

            case ELEMENT_TYPE_R8            :
                appendStr(out,KEYWORD("float64"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    char *pch;
                    _gcvt_s(str,64,*((double*)dataPtr), 17);
                    double df = strtod(str, &pch);
                    // Must compare as underlying bytes, not floating point otherwise optmizier will
                    // try to enregister and comapre 80-bit precision number with 64-bit precision number!!!!
                    if((*(ULONGLONG*)&df != (ULONGLONG)GET_UNALIGNED_VAL64(dataPtr))||IsSpecialNumber(str))
                        sprintf_s(str, 64, "0x%I64X",(ULONGLONG)GET_UNALIGNED_VAL64(dataPtr));
                    appendStr(out,str);
                    dataPtr +=8;
                }
                break;
            case ELEMENT_TYPE_U             :
            case ELEMENT_TYPE_I             :
                return NULL;

            case ELEMENT_TYPE_OBJECT        :
            case SERIALIZATION_TYPE_TAGGED_OBJECT:
                appendStr(out,KEYWORD("object"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    BYTE* dataPtr1 = skipType(dataPtr);
                    if(n) appendStr(out," ");

                    dataPtr = PrettyPrintCABlobValue((PCCOR_SIGNATURE&)dataPtr, dataPtr1, dataEnd, out, pIMDI,GUICookie);
                    if (dataPtr == NULL) return NULL;
                }
                break;
            case ELEMENT_TYPE_STRING        :
                appendStr(out,KEYWORD("string"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    if(*dataPtr == 0xFF)
                    {
                        appendStr(out,KEYWORD("nullref"));
                        Lstr = 1;
                    }
                    else
                    {
                        appendStr(out,"'");
                        Lstr = CorSigUncompressData((PCCOR_SIGNATURE&)dataPtr);
                        if(dataPtr + Lstr > dataEnd) return NULL;
                        appendStr(out,UnquotedProperName((char*)dataPtr,Lstr));
                        appendStr(out,"'");
                    }
                    dataPtr += Lstr;
                }
                break;
            case ELEMENT_TYPE_CLASS        :
                typePtr += CorSigUncompressToken(typePtr, &tk); //skip the following token
                FALLTHROUGH;
            case SERIALIZATION_TYPE_TYPE   :
                appendStr(out,KEYWORD("type"));
                appendStr(out,appendix);
                appendStr(out,"(");
                for(n=0; n < numElements; n++)
                {
                    if(n) appendStr(out," ");
                    if(*dataPtr == 0xFF)
                    {
                        appendStr(out,KEYWORD("nullref"));
                        Lstr = 1;
                    }
                    else
                    {
                        Lstr = CorSigUncompressData((PCCOR_SIGNATURE&)dataPtr);
                        if(dataPtr + Lstr > dataEnd) return NULL;
                        tk = ResolveReflectionNotation(dataPtr,Lstr,pIMDI,GUICookie);
                        if(IsNilToken(tk))
                        {
                            appendStr(out,KEYWORD("class "));
                            appendStr(out,"'");
                            appendStr(out,UnquotedProperName((char*)dataPtr,Lstr));
                            appendStr(out,"'");
                        }
                        else
                        {
                            PrettyPrintClass(out, tk, pIMDI);
                        }
                    }
                    dataPtr += Lstr;
                }
                break;


            case ELEMENT_TYPE_VALUETYPE    :
                typePtr += CorSigUncompressToken(typePtr, &tk);
                _ASSERTE(pIMDI->IsValidToken(tk));
                goto GetUTSize;

            case SERIALIZATION_TYPE_ENUM    :
                Lstr = CorSigUncompressData((PCCOR_SIGNATURE&)typePtr);
                tk = ResolveReflectionNotation((BYTE*)typePtr,Lstr,pIMDI,GUICookie);
                /*
                if(IsNilToken(tk))
                {
                    _ASSERTE(!"Failed to resolve Reflection notation for S_T_ENUM");
                    return NULL;
                }
                */
                typePtr += Lstr;

           GetUTSize:
                underType = UnderlyingTypeOfEnum(tk, pIMDI);
                if(underType == 0)
                {
                    // try to figure out the underlying type by its size
                    switch(dataEnd - dataPtr)
                    {
                        case 1: // bool
                            underType = ELEMENT_TYPE_BOOLEAN;
                            break;
                        case 2: // int16
                            underType = ELEMENT_TYPE_I2;
                            break;
                        case 4: // int32
                            underType = ELEMENT_TYPE_I4;
                            break;
                        case 8: // int64
                            underType = ELEMENT_TYPE_I8;
                            break;
                        default:
                            return NULL;
                    }
                    //_ASSERTE(!"Failed to find underlying type for S_T_ENUM");
                }
                {
                    PCCOR_SIGNATURE ps = (PCCOR_SIGNATURE)&underType;
                    dataPtr = PrettyPrintCABlobValue(ps, dataPtr, dataEnd, out, pIMDI,GUICookie);
                }
                CloseParenthesis = FALSE;
                break;


            case ELEMENT_TYPE_SZARRAY    :
                numElements *= (unsigned)GET_UNALIGNED_VAL32(dataPtr);
                Reiterate = TRUE;
                sprintf_s(appendix,64,"[%d]",numElements);
                if(numElements == 0xFFFFFFFF)
                    numElements = 0;
                dataPtr += 4;
                break;

            case ELEMENT_TYPE_ARRAY       :
            case ELEMENT_TYPE_VAR        :
            case ELEMENT_TYPE_MVAR        :
            case ELEMENT_TYPE_FNPTR :
            case ELEMENT_TYPE_GENERICINST :
            case ELEMENT_TYPE_TYPEDBYREF        :

#ifdef LOGGING
            case ELEMENT_TYPE_INTERNAL :
#endif // LOGGING
                return NULL;


                // Modifiers or depedent types
            case ELEMENT_TYPE_CMOD_OPT  :
            case ELEMENT_TYPE_CMOD_REQD :
            case ELEMENT_TYPE_PINNED    :
                Reiterate = TRUE;
                break;

            case ELEMENT_TYPE_PTR           :
            case ELEMENT_TYPE_BYREF         :
                return NULL;

            default:
            case ELEMENT_TYPE_SENTINEL      :
            case ELEMENT_TYPE_END           :
                _ASSERTE(!"Unknown Type");
                return NULL;
        } // end switch
    } while(Reiterate);
    if(CloseParenthesis) appendStr(out,")");
    return dataPtr;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

BOOL PrettyPrintCustomAttributeNVPairs(unsigned nPairs, BYTE* dataPtr, BYTE* dataEnd, CQuickBytes* out, void* GUICookie)
{
    IMDInternalImport *pIMDI = g_pImport; // ptr to IMDInternalImport class with ComSig
    while(dataPtr < dataEnd)
    {
        // field or property?
        switch(*dataPtr)
        {
            case SERIALIZATION_TYPE_FIELD:
                appendStr(out,KEYWORD("field "));
                break;
            case SERIALIZATION_TYPE_PROPERTY:
                appendStr(out,KEYWORD("property "));
                break;
            default:
                _ASSERTE(!"Invalid code of name/val pair in CA blob");
                return FALSE;
        }
        dataPtr++;
        if(dataPtr >= dataEnd)
        {
            _ASSERTE(!"CA blob too short");
            return FALSE;
        }
        // type of the field/property
        PCCOR_SIGNATURE dataTypePtr = (PCCOR_SIGNATURE)dataPtr;
        const char* szAppend = "";
        if(*dataPtr == ELEMENT_TYPE_SZARRAY) // Only SZARRAY modifier can occur in ser.type
        {
            szAppend = "[]";
            dataPtr++;
        }
        if(*dataPtr == SERIALIZATION_TYPE_TYPE)
        {
            appendStr(out,KEYWORD("type"));
            dataPtr++;
        }
        else if(*dataPtr == SERIALIZATION_TYPE_TAGGED_OBJECT)
        {
            appendStr(out,KEYWORD("object"));
            dataPtr++;
        }
        else if(*dataPtr == SERIALIZATION_TYPE_ENUM)
        {
            appendStr(out,KEYWORD("enum "));
            dataPtr++;
            unsigned Lstr = CorSigUncompressData((PCCOR_SIGNATURE&)dataPtr);
            if(dataPtr + Lstr > dataEnd) return FALSE;
            mdToken tk = ResolveReflectionNotation(dataPtr,Lstr,pIMDI,GUICookie);
            if(IsNilToken(tk))
            {
                appendStr(out,KEYWORD("class "));
                appendStr(out,"'");
                appendStr(out,UnquotedProperName((char*)dataPtr,Lstr));
                appendStr(out,"'");
            }
            else
            {
                PrettyPrintClass(out, tk, pIMDI);
            }
            dataPtr += Lstr;
        }
        else
        {
            szAppend = "";
            dataPtr = (BYTE*)PrettyPrintType(dataTypePtr, out, pIMDI);
        }
        if(*szAppend != 0)
            appendStr(out,szAppend);
        if(dataPtr >= dataEnd)
        {
            _ASSERTE(!"CA blob too short");
            return FALSE;
        }
        // name of the field/property
        unsigned Lstr = CorSigUncompressData((PCCOR_SIGNATURE&)dataPtr);
        if(dataPtr + Lstr > dataEnd) return FALSE;
        appendStr(out," '");
        appendStr(out,UnquotedProperName((char*)dataPtr,Lstr));
        appendStr(out,"' = ");
        dataPtr += Lstr;
        if(dataPtr >= dataEnd)
        {
            _ASSERTE(!"CA blob too short");
            return FALSE;
        }
        // value of the field/property
        dataPtr = PrettyPrintCABlobValue(dataTypePtr, dataPtr, dataEnd, out, pIMDI,GUICookie);
        if(NULL == dataPtr) return FALSE;
        appendStr(out,"\n");

        nPairs--;
    }
    _ASSERTE(nPairs == 0);
    return TRUE;
}
BOOL PrettyPrintCustomAttributeBlob(mdToken tkType, BYTE* pBlob, ULONG ulLen, void* GUICookie, __inout __nullterminated char* szString)
{
    char* initszptr = szString + strlen(szString);
    PCCOR_SIGNATURE typePtr;            // type to convert,
    ULONG typeLen;                  // the lenght of 'typePtr'
    CHECK_LOCAL_STATIC_VAR(static CQuickBytes out); // where to put the pretty printed string

    IMDInternalImport *pIMDI = g_pImport; // ptr to IMDInternalImport class with ComSig
    unsigned numArgs = 0;
    unsigned numTyArgs = 0;
    PCCOR_SIGNATURE typeEnd;
    unsigned callConv;
    BYTE* dataPtr = pBlob;
    BYTE* dataEnd = dataPtr + ulLen;
    WORD  wNumNVPairs = 0;
    unsigned numElements = 0;

    if(TypeFromToken(tkType) == mdtMemberRef)
    {
        const char *szName_Ignore;
        if (FAILED(pIMDI->GetNameAndSigOfMemberRef(tkType,&typePtr,&typeLen, &szName_Ignore)))
        {
            return FALSE;
        }
    }
    else if(TypeFromToken(tkType) == mdtMethodDef)
    {
        if (FAILED(pIMDI->GetSigOfMethodDef(tkType, &typeLen, &typePtr)))
        {
            return FALSE;
        }
    }
    else
        return FALSE;
    typeEnd = typePtr + typeLen;

    callConv = CorSigUncompressData(typePtr);

    if (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
      numTyArgs = CorSigUncompressData(typePtr);
      return FALSE; // leave generic instantiations for later
    }
    numElements = numArgs = CorSigUncompressData(typePtr);
    out.Shrink(0);
    if (!isCallConv(callConv, IMAGE_CEE_CS_CALLCONV_GENERICINST))
    {
            // skip return type
        typePtr = PrettyPrintType(typePtr, &out, pIMDI);
        out.Shrink(0);
    }
    appendStr(&out," = {");
    dataPtr += 2; // skip blob prolog 0x0001
    // dump the arguments
    while(typePtr < typeEnd)
    {
        if (*typePtr == ELEMENT_TYPE_SENTINEL)
        {
            typePtr++;
        }
        else
        {
            if (numArgs <= 0)
                break;
            dataPtr = PrettyPrintCABlobValue(typePtr, dataPtr, dataEnd-2, &out, pIMDI,GUICookie);
            if(NULL == dataPtr) return FALSE;
            appendStr(&out,"\n");
            --numArgs;
        }
    }
    _ASSERTE(numArgs == 0);
    wNumNVPairs = (WORD)GET_UNALIGNED_VAL16(dataPtr);
    dataPtr+=2;
    numElements += wNumNVPairs;
    // arguments done, now to field/property name-val pairs

    if(!PrettyPrintCustomAttributeNVPairs((unsigned) wNumNVPairs, dataPtr, dataEnd, &out, GUICookie))
        return FALSE;

    {
        char* sz = asString(&out);
        char* ch = sz;
        char* szbl;
        while((ch = strchr(ch,'\n')))
        {
            *ch = 0;
            ch++;
        }
        // if the string is too long already, begin on next line
        if((initszptr - szString) > 80)
        {
            printLine(GUICookie,szString);
            sprintf_s(szString,SZSTRING_SIZE,"%s        ",g_szAsmCodeIndent);
            initszptr = &szString[strlen(szString)];
        }
        sprintf_s(initszptr,SZSTRING_REMAINING_SIZE(initszptr), "%s", sz);
        initszptr += 4; // to compensate for " = {"
        szbl = szString + strlen(g_szAsmCodeIndent);
        for(unsigned n = 1; n < numElements; n++)
        {
            printLine(GUICookie, szString);
            sz = sz + strlen(sz) + 1;
            for(ch = szbl; ch < initszptr; ch++) *ch = ' ';
            sprintf_s(initszptr,SZSTRING_REMAINING_SIZE(initszptr), "%s", sz);
        }
    }
    strcat_s(initszptr, SZSTRING_REMAINING_SIZE(initszptr),"}");
    if(g_fShowBytes)
    {
        printLine(GUICookie,szString);
        strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"  //    ");
        sprintf_s(szString,SZSTRING_SIZE,"%s = ( ",g_szAsmCodeIndent);
        DumpByteArray(szString,pBlob,ulLen,GUICookie);
        g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-8] = 0;
    }
    return TRUE;
}

void DumpCustomAttributeProps(mdToken tkCA, mdToken tkType, mdToken tkOwner, BYTE* pBlob, ULONG ulLen, void *GUICookie, bool bWithOwner)
{
    char*           szptr = &szString[0];
    BOOL            fCommentItOut = FALSE;
    if((TypeFromToken(tkType) == mdtMemberRef)||(TypeFromToken(tkType) == mdtMethodDef))
    {
        mdToken tkParent;
        const char *    pszClassName = NULL;
        const char *    pszNamespace = NULL;
        if (TypeFromToken(tkType) == mdtMemberRef)
        {
            if (FAILED(g_pImport->GetParentOfMemberRef(tkType, &tkParent)))
            {
                szptr += sprintf_s(szptr, SZSTRING_REMAINING_SIZE(szptr), "Invalid MemberRef %08X record ", tkType);
                return;
            }
        }
        else
        {
            if (FAILED(g_pImport->GetParentToken(tkType, &tkParent)))
            {
                szptr += sprintf_s(szptr, SZSTRING_REMAINING_SIZE(szptr), "Invalid token %08X ", tkType);
                return;
            }
        }

        REGISTER_REF(tkOwner,tkType); // owner of the CA references the class amd method
        REGISTER_REF(tkOwner,tkParent);

        if (TypeFromToken(tkParent) == mdtTypeDef)
        {
            if (FAILED(g_pImport->GetNameOfTypeDef(tkParent, &pszClassName, &pszNamespace)))
            {
                szptr += sprintf_s(szptr, SZSTRING_REMAINING_SIZE(szptr), "Invalid TypeDef %08X record ", tkParent);
                return;
            }
        }
        else if (TypeFromToken(tkParent) == mdtTypeRef)
        {
            if (FAILED(g_pImport->GetNameOfTypeRef(tkParent, &pszNamespace, &pszClassName)))
            {
                szptr += sprintf_s(szptr, SZSTRING_REMAINING_SIZE(szptr), "Invalid TypeRef %08X record ", tkParent);
                return;
            }
        }
        if(pszClassName && pszNamespace
            && (strcmp(pszNamespace,"System.Diagnostics") == 0)
            && (strcmp(pszClassName,"DebuggableAttribute") == 0)) fCommentItOut = TRUE;


    }
    if(fCommentItOut)
    {
        printLine(GUICookie,COMMENT((char*)0)); // start multiline comment
        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_AUTOCA),g_szAsmCodeIndent);
        printLine(GUICookie, szString);
        strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"//  ");
    }
    szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".custom"));
    if(bWithOwner)
    {
        if(g_fDumpTokens)   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),tkCA);
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(");
        switch(TypeFromToken(tkOwner))
        {
            case mdtTypeDef :
            case mdtTypeRef :
            case mdtTypeSpec:
                    PrettyPrintToken(szString, tkOwner, g_pImport,GUICookie,0);
                break;

            case mdtMemberRef:
                {
                    PCCOR_SIGNATURE typePtr;
                    const char*     pszMemberName;
                    ULONG           cComSig;

                    if (FAILED(g_pImport->GetNameAndSigOfMemberRef(
                        tkOwner,
                        &typePtr,
                        &cComSig,
                        &pszMemberName)))
                    {
                        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"ERROR ");
                        break;
                    }
                    unsigned callConv = CorSigUncompressData(typePtr);

                    if (isCallConv(callConv, IMAGE_CEE_CS_CALLCONV_FIELD))
                        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("field "));
                    else
                        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("method "));
                    PrettyPrintToken(szString, tkOwner, g_pImport,GUICookie,0);
                }
                break;

            case mdtMethodDef:
                szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr), KEYWORD("method "));
                PrettyPrintToken(szString, tkOwner, g_pImport,GUICookie,0);
                break;

            default :
                strcat_s(szptr, SZSTRING_REMAINING_SIZE(szptr),ERRORMSG("UNKNOWN_OWNER"));
                break;
        }
        szptr = &szString[strlen(szString)];
        if(g_fDumpTokens)   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),tkOwner);
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),") ");
    }
    else
    {
        if(g_fDumpTokens)   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X:%08X*/ "),tkCA,tkType);
    }
    switch(TypeFromToken(tkType))
    {
        case mdtTypeDef :
        case mdtTypeRef :
        case mdtMemberRef:
        case mdtMethodDef:
            PrettyPrintToken(szString, tkType, g_pImport,GUICookie,0);
            break;

        default :
            strcat_s(szString, SZSTRING_SIZE,ERRORMSG("UNNAMED_CUSTOM_ATTR"));
            break;
    }
    szptr = &szString[strlen(szString)];

    if(pBlob && ulLen)
    {
        if(!g_fCAVerbal || !PrettyPrintCustomAttributeBlob(tkType, pBlob, ulLen, GUICookie, szString))
        {
            sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = ( ");
            DumpByteArray(szString,pBlob,ulLen,GUICookie);
        }
    }
    printLine(GUICookie, szString);
    if(fCommentItOut)
    {
        g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-4] = 0;
        printLine(GUICookie,COMMENT((char*)-1)); // end multiline comment
    }
}

void DumpCustomAttribute(mdCustomAttribute tkCA, void *GUICookie, bool bWithOwner)
{
    mdToken         tkType;
    BYTE*           pBlob=NULL;
    ULONG           ulLen=0;
    mdToken         tkOwner;
    static mdToken  tkMod = 0xFFFFFFFF;

    _ASSERTE((TypeFromToken(tkCA)==mdtCustomAttribute)&&(RidFromToken(tkCA)>0));
    _ASSERTE(RidFromToken(tkCA) <= g_uNCA);
    if(tkMod == 0xFFFFFFFF) tkMod = g_pImport->GetModuleFromScope();

    // can't use InternalImport here: need the tkOwner
    if (FAILED(g_pPubImport->GetCustomAttributeProps(              // S_OK or error.
                                            tkCA,       // [IN] CustomValue token.
                                            &tkOwner,   // [OUT, OPTIONAL] Object token.
                                            &tkType,    // [OUT, OPTIONAL] Put TypeDef/TypeRef token here.
                             (const void **)&pBlob,     // [OUT, OPTIONAL] Put pointer to data here.
                                            &ulLen)))   // [OUT, OPTIONAL] Put size of date here.
    {
        return;
    }

    if(!RidFromToken(tkOwner)) return;

    DWORD i;
    for(i = 0; i < g_NumTypedefs; i++)
    {
        TypeDefDescr* pTDD = &((*g_typedefs)[i]);
        if(TypeFromToken(pTDD->tkTypeSpec) == mdtCustomAttribute)
        {
            mdToken tkTypeTD;
            mdToken tkOwnerTD;
            BYTE* pBlobTD=NULL;
            ULONG uLenTD=0;
            tkTypeTD = GET_UNALIGNED_VAL32(pTDD->psig);
            if(tkTypeTD != tkType) continue;

            tkOwnerTD = GET_UNALIGNED_VAL32(pTDD->psig + sizeof(mdToken));
            if(pTDD->cb > 2*sizeof(mdToken))
            {
                pBlobTD = (BYTE*)pTDD->psig + 2*sizeof(mdToken);
                uLenTD = pTDD->cb - 2*sizeof(mdToken);
            }
            if(uLenTD != ulLen) continue;
            if(memcmp(pBlobTD,pBlob,ulLen) != 0) continue;
            char* szptr = &szString[0];
            szString[0] = 0;
            szptr += sprintf_s(szString,SZSTRING_SIZE,"%s%s", g_szAsmCodeIndent,JUMPPT(ProperName(pTDD->szName),pTDD->tkSelf));
            if(g_fDumpTokens)
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),tkCA);
            printLine(GUICookie,szString);
            break;
        }
    }
    if(i >= g_NumTypedefs)
        DumpCustomAttributeProps(tkCA,tkType,tkOwner,pBlob,ulLen,GUICookie,bWithOwner);
    _ASSERTE(g_rchCA);
    _ASSERTE(RidFromToken(tkCA) <= g_uNCA);
    g_rchCA[RidFromToken(tkCA)] = 1;
}

void DumpCustomAttributes(mdToken tkOwner, void *GUICookie)
{
    if (g_fShowCA)
    {
        HENUMInternal    hEnum;
        mdCustomAttribute tkCA;

        if (FAILED(g_pImport->EnumInit(mdtCustomAttribute, tkOwner,&hEnum)))
        {
            return;
        }
        while(g_pImport->EnumNext(&hEnum,&tkCA) && RidFromToken(tkCA))
        {
            DumpCustomAttribute(tkCA,GUICookie,false);
        }
        g_pImport->EnumClose( &hEnum);
    }
}

void DumpDefaultValue(mdToken tok, __inout __nullterminated char* szString, void* GUICookie)
{
    MDDefaultValue  MDDV;
    char*           szptr = &szString[strlen(szString)];

    if (FAILED(g_pImport->GetDefaultValue(tok, &MDDV)))
    {
        szptr += sprintf_s(szptr, SZSTRING_REMAINING_SIZE(szptr), ERRORMSG(" /* Invalid default value for %08X: */"), tok);
        return;
    }
    switch(MDDV.m_bType)
    {
        case ELEMENT_TYPE_VOID:
            strcat_s(szString, SZSTRING_SIZE," /* NO CORRESPONDING RECORD IN CONSTANTS TABLE */");
            break;
        case ELEMENT_TYPE_I1:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(0x%02X)",KEYWORD("int8"),MDDV.m_byteValue);
            break;
        case ELEMENT_TYPE_U1:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(0x%02X)",KEYWORD("uint8"),MDDV.m_byteValue);
            break;
        case ELEMENT_TYPE_I2:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(0x%04X)",KEYWORD("int16"),MDDV.m_usValue);
            break;
        case ELEMENT_TYPE_U2:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(0x%04X)",KEYWORD("uint16"),MDDV.m_usValue);
            break;
        case ELEMENT_TYPE_I4:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(0x%08X)",KEYWORD("int32"),MDDV.m_ulValue);
            break;
        case ELEMENT_TYPE_U4:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(0x%08X)",KEYWORD("uint32"),MDDV.m_ulValue);
            break;
        case ELEMENT_TYPE_CHAR:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(0x%04X)",KEYWORD("char"),MDDV.m_usValue);
            break;
        case ELEMENT_TYPE_BOOLEAN:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s",KEYWORD("bool"));
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(%s)", KEYWORD((char *)(MDDV.m_byteValue ? "true" : "false")));
            break;
        case ELEMENT_TYPE_I8:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(0x%I64X)",KEYWORD("int64"),MDDV.m_ullValue);
            break;
        case ELEMENT_TYPE_U8:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(0x%I64X)",KEYWORD("uint64"),MDDV.m_ullValue);
            break;
        case ELEMENT_TYPE_R4:
            {
                char szf[32];
                _gcvt_s(szf,32,MDDV.m_fltValue, 8);
                float df = (float)atof(szf);
                // Must compare as underlying bytes, not floating point otherwise optmizier will
                // try to enregister and comapre 80-bit precision number with 32-bit precision number!!!!
                if((*(ULONG*)&df == MDDV.m_ulValue)&&!IsSpecialNumber(szf))
                    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(%s)",KEYWORD("float32"),szf);
                else
                    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr), " = %s(0x%08X)",KEYWORD("float32"),MDDV.m_ulValue);

            }
            break;
        case ELEMENT_TYPE_R8:
            {
                char szf[32], *pch;
                _gcvt_s(szf,32,MDDV.m_dblValue, 17);
                double df = strtod(szf, &pch); //atof(szf);
                szf[31]=0;
                // Must compare as underlying bytes, not floating point otherwise optmizier will
                // try to enregister and comapre 80-bit precision number with 64-bit precision number!!!!
                if((*(ULONGLONG*)&df == MDDV.m_ullValue)&&!IsSpecialNumber(szf))
                    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s(%s)",KEYWORD("float64"),szf);
                else
                    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr), " = %s(0x%I64X) // %s",KEYWORD("float64"),MDDV.m_ullValue,szf);
            }
            break;

        case ELEMENT_TYPE_STRING:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = ");
            PAL_CPP_TRY {
                szptr = DumpUnicodeString(GUICookie,szString,(WCHAR*)MDDV.m_wzValue,MDDV.m_cbSize/sizeof(WCHAR));
            } PAL_CPP_CATCH_ALL {
                strcat_s(szString, SZSTRING_SIZE,ERRORMSG("INVALID DATA ADDRESS"));
            } PAL_CPP_ENDTRY;
            break;

        case ELEMENT_TYPE_CLASS:
            if(MDDV.m_wzValue==NULL)
            {
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = %s",KEYWORD("nullref"));
                break;
            }
            //else fall thru to default case, to report the error
            FALLTHROUGH;

        default:
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),ERRORMSG(" /* ILLEGAL CONSTANT type:0x%02X, size:%d bytes, blob: "),MDDV.m_bType,MDDV.m_cbSize);
            if(MDDV.m_wzValue)
            {
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(");
                PAL_CPP_TRY {
                    DumpByteArray(szString,(BYTE*)MDDV.m_wzValue,MDDV.m_cbSize,GUICookie);
                } PAL_CPP_CATCH_ALL {
                    szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),ERRORMSG(" Invalid blob at 0x%08X)"), MDDV.m_wzValue);
                } PAL_CPP_ENDTRY
            }
            else
            {
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"NULL");
            }
            strcat_s(szString, SZSTRING_REMAINING_SIZE(szptr), " */");
            break;
    }
}

void DumpParams(ParamDescriptor* pPD, ULONG ulParams, void* GUICookie)
{
    if(pPD)
    {
        for(ULONG i = ulParams; i<2*ulParams+1; i++) // pPD[ulParams] is return value
        {
            ULONG j = i % (ulParams+1);
            if(RidFromToken(pPD[j].tok))
            {
                HENUMInternal    hEnum;
                mdCustomAttribute tkCA;
                ULONG           ulCAs= 0;

                if(g_fShowCA)
                {
                    if (FAILED(g_pImport->EnumInit(mdtCustomAttribute, pPD[j].tok, &hEnum)))
                    {
                        sprintf_s(szString, SZSTRING_SIZE, "%sERROR: MetaData error enumerating CustomAttribute for %08X", g_szAsmCodeIndent, pPD[j].tok);
                        printLine(GUICookie, szString);
                        continue;
                    }
                    ulCAs = g_pImport->EnumGetCount(&hEnum);
                }
                if(ulCAs || IsPdHasDefault(pPD[j].attr))
                {
                    char    *szptr = &szString[0];
                    szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s [%d]",g_szAsmCodeIndent,KEYWORD(".param"),i-ulParams);
                    if(g_fDumpTokens) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),pPD[j].tok);
                    if(IsPdHasDefault(pPD[j].attr)) DumpDefaultValue(pPD[j].tok, szString, GUICookie);
                    printLine(GUICookie, szString);
                    if(ulCAs)
                    {
                        while(g_pImport->EnumNext(&hEnum,&tkCA) && RidFromToken(tkCA))
                        {
                            DumpCustomAttribute(tkCA,GUICookie,false);
                        }
                    }
                }
                if(g_fShowCA) g_pImport->EnumClose( &hEnum);
            }
        }
    }
}

BOOL DumpPermissionSetBlob(void* GUICookie,__inout __nullterminated char* szString, BYTE* pvPermission, ULONG cbPermission)
{
    if(*pvPermission == '.')
    {
        CQuickBytes out;
        pvPermission++;
        char* szptr_init = &szString[strlen(szString)];
        char* szptr = szptr_init;
        appendStr(&out," = {");
        unsigned nAttrs = CorSigUncompressData((PCCOR_SIGNATURE&)pvPermission);
        for(unsigned iAttr = 0; iAttr < nAttrs; iAttr++)
        {
            unsigned L = CorSigUncompressData((PCCOR_SIGNATURE&)pvPermission); // class name length
            mdToken  tkAttr = ResolveReflectionNotation(pvPermission,L,g_pImport,GUICookie);
            if(IsNilToken(tkAttr))
            {
                appendStr(&out,KEYWORD("class "));
                appendStr(&out,"'");
                appendStr(&out,UnquotedProperName((char*)pvPermission,L));
                appendStr(&out,"'");
            }
            else
            {
                PrettyPrintClass(&out, tkAttr, g_pImport);
            }
            pvPermission += L;
            appendStr(&out," = {");
            // dump blob
            L = CorSigUncompressData((PCCOR_SIGNATURE&)pvPermission); // blob length
            if(L > 0)
            {
                BYTE* pvEnd = pvPermission+L;
                L = CorSigUncompressData((PCCOR_SIGNATURE&)pvPermission); // number of props
                if(L > 0)
                {
                    if(!PrettyPrintCustomAttributeNVPairs(L, pvPermission, pvEnd, &out, GUICookie))
                        return FALSE;
                    out.Shrink(out.Size()-1);
                }
                pvPermission = pvEnd;
            }
            appendStr(&out, iAttr == nAttrs-1 ? "}" : "}, ");
        }
        appendStr(&out, "}");
        char* sz = asString(&out);
        while(char* pc = strstr(sz,"}, "))
        {
            *(pc+2) = 0;
            strcpy_s(szptr,SZSTRING_REMAINING_SIZE(szptr), sz);
            printLine(GUICookie,szString);
            sz = pc+3;
            if(szptr == szptr_init) szptr += 4; // to compensate for = {
            for(pc = szString; pc < szptr; pc++) *pc = ' ';
        }
        strcpy_s(szptr, SZSTRING_REMAINING_SIZE(szptr),sz);
        return TRUE;
    }
    return FALSE;
}

void DumpPermissions(mdToken tkOwner, void* GUICookie)
{
    HCORENUM hEnum = NULL;
    static mdPermission rPerm[16384];
    ULONG count;
    HRESULT hr;
    //static char    szString[4096];

    // can't use internal import here: EnumInit not impl. for mdtPrmission
    while (SUCCEEDED(hr = g_pPubImport->EnumPermissionSets( &hEnum,
                     tkOwner, 0, rPerm, 16384, &count)) &&
            count > 0)
    {
        for (ULONG i = 0; i < count; i++)
        {
            DWORD dwAction;
            const BYTE *pvPermission=NULL;
            ULONG cbPermission=0;
            const char *szAction;
            char *szptr;

            szptr = &szString[0];
            if(SUCCEEDED(hr = g_pPubImport->GetPermissionSetProps( rPerm[i], &dwAction,
                                                (const void**)&pvPermission, &cbPermission)))
            {
                szptr += sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".permissionset"));
                switch(dwAction)
                {
                    case dclActionNil:          szAction = ""; break;
                    case dclRequest:            szAction = KEYWORD("request"); break;
                    case dclDemand:             szAction = KEYWORD("demand"); break;
                    case dclAssert:             szAction = KEYWORD("assert"); break;
                    case dclDeny:               szAction = KEYWORD("deny"); break;
                    case dclPermitOnly:         szAction = KEYWORD("permitonly"); break;
                    case dclLinktimeCheck:      szAction = KEYWORD("linkcheck"); break;
                    case dclInheritanceCheck:   szAction = KEYWORD("inheritcheck"); break;
                    case dclRequestMinimum:     szAction = KEYWORD("reqmin"); break;
                    case dclRequestOptional:    szAction = KEYWORD("reqopt"); break;
                    case dclRequestRefuse:      szAction = KEYWORD("reqrefuse"); break;
                    case dclPrejitGrant:        szAction = KEYWORD("prejitgrant"); break;
                    case dclPrejitDenied:       szAction = KEYWORD("prejitdeny"); break;
                    case dclNonCasDemand:       szAction = KEYWORD("noncasdemand"); break;
                    case dclNonCasLinkDemand:   szAction = KEYWORD("noncaslinkdemand"); break;
                    case dclNonCasInheritance:  szAction = KEYWORD("noncasinheritance"); break;
                    default:                    szAction = ERRORMSG("<UNKNOWN_ACTION>"); break;
                }
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),szAction);
                if(pvPermission && cbPermission)
                {
                    printLine(GUICookie, szString);
                    sprintf_s(szString,SZSTRING_SIZE,"%s          ",g_szAsmCodeIndent);
                    if(!DumpPermissionSetBlob(GUICookie,szString,(BYTE*)pvPermission,cbPermission))
                    {
                        strcat_s(szString,SZSTRING_SIZE,KEYWORD("bytearray"));
                        strcat_s(szString,SZSTRING_SIZE," (");
                        DumpByteArray(szString, pvPermission, cbPermission, GUICookie);
                    }
                    printLine(GUICookie,szString);
                }
                else // i.e. if pvPermission == NULL or cbPermission == NULL
                {
                    sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," = ()");
                    printLine(GUICookie,szString);
                }
                DumpCustomAttributes(rPerm[i],GUICookie);
            }// end if(GetPermissionProps)
        } // end for(all permissions)
    }//end while(EnumPermissionSets)
    g_pPubImport->CloseEnum( hEnum);
}

void PrettyPrintMethodSig(__inout __nullterminated char* szString, unsigned* puStringLen, CQuickBytes* pqbMemberSig, PCCOR_SIGNATURE pComSig, ULONG cComSig,
                          __inout __nullterminated char* buff, _In_opt_z_ char* szArgPrefix, void* GUICookie)
{
    unsigned uMaxWidth = 40;
    if(g_fDumpHTML || g_fDumpRTF) uMaxWidth = 240;
    if(*buff && (strlen(szString) > (size_t)uMaxWidth))
    {
        printLine(GUICookie,szString);
        strcpy_s(szString,SZSTRING_SIZE,g_szAsmCodeIndent);
        strcat_s(szString,SZSTRING_SIZE,"        "); // to align with ".method "
    }
    appendStr(pqbMemberSig, szString);
    {
        char* pszTailSig = (char*)PrettyPrintSig(pComSig, cComSig, buff, pqbMemberSig, g_pImport, szArgPrefix);
        if(*buff)
        {
            size_t L = strlen(pszTailSig);
            char* newbuff = new char[strlen(buff)+3];
            sprintf_s(newbuff,strlen(buff)+3," %s(", buff);
            char* pszOffset = strstr(pszTailSig,newbuff);
            if(pszOffset)
            {
                char* pszTailSigRemainder = new char[L+1];
                if(pszOffset - pszTailSig > (int)uMaxWidth)
                {
                    char* pszOffset2 = strstr(pszTailSig," marshal(");
                    if(pszOffset2 && (pszOffset2 < pszOffset))
                    {
                        *pszOffset2 = 0;
                        strcpy_s(pszTailSigRemainder,L,pszOffset2+1);
                        printLine(GUICookie,pszTailSig);
                        strcpy_s(pszTailSig,L,g_szAsmCodeIndent);
                        strcat_s(pszTailSig,L,"        "); // to align with ".method "
                        strcat_s(pszTailSig,L,pszTailSigRemainder);
                        pszOffset = strstr(pszTailSig,newbuff);
                    }
                    *pszOffset = 0 ;
                    strcpy_s(pszTailSigRemainder,L,pszOffset+1);
                    printLine(GUICookie,pszTailSig);
                    strcpy_s(pszTailSig,L,g_szAsmCodeIndent);
                    strcat_s(pszTailSig,L,"        "); // to align with ".method "
                    strcat_s(pszTailSig,L,pszTailSigRemainder);
                    pszOffset = strstr(pszTailSig,newbuff);
                }
                size_t i, j, k, l, indent = pszOffset - pszTailSig + strlen(buff) + 2;
                char chAfterComma;
                char *pComma = pszTailSig+strlen(buff), *pch;
                while((pComma = strchr(pComma,',')))
                {
                    for(pch = pszTailSig, i=0, j = 0, k=0, l=0; pch < pComma; pch++)
                    {
                        if(*pch == '\\')  pch++;
                        else
                        {
                            if(*pch == '\'') j=1-j;
                            else if(*pch == '\"') k=1-k;
                            else if(j==0)
                            {
                                    if(*pch == '[') i++;
                                    else if(*pch == ']') i--;
                                    else if(strncmp(pch,LTN(),strlen(LTN()))==0) l++;
                                    else if(strncmp(pch,GTN(),strlen(GTN()))==0) l--;
                            }
                        }
                    }
                    pComma++;
                    if((i==0)&&(j==0)&&(k==0)&&(l==0))// no brackets/quotes or all opened/closed
                    {
                        chAfterComma = *pComma;
                        strcpy_s(pszTailSigRemainder,L,pComma);
                        *pComma = 0;
                        printLine(GUICookie,pszTailSig);
                        *pComma = chAfterComma;
                        for(i=0; i<indent; i++) pszTailSig[i] = ' ';
                        strcpy_s(&pszTailSig[indent],L-indent,pszTailSigRemainder);
                        pComma = pszTailSig;
                    }
                }
                if(*puStringLen < (unsigned)strlen(pszTailSig)+128)
                {
                    //free(szString);
                    *puStringLen = (unsigned)strlen(pszTailSig)+128; // need additional space for "il managed" etc.
                    //szString = (char*)malloc(*puStringLen);
                }
                VDELETE(pszTailSigRemainder);
            }
            strcpy_s(szString,SZSTRING_SIZE,pszTailSig);
            VDELETE(newbuff);
        }
        else // it's for GUI, don't split it into several lines
        {
            size_t L = strlen(szString);
            if(L < 2048)
            {
                L = 2048-L;
                strncpy_s(szString,SZSTRING_SIZE,pszTailSig,L);
            }
        }
    }
}
// helper to avoid mixing of SEH and stack objects with destructors
BOOL DisassembleWrapper(IMDInternalImport *pImport, BYTE *ILHeader,
    void *GUICookie, mdToken FuncToken, ParamDescriptor* pszArgname, ULONG ulArgs)
{
    BOOL fRet = FALSE;
    //char szString[4096];

    PAL_CPP_TRY
    {
        fRet = Disassemble(pImport, ILHeader, GUICookie, FuncToken, pszArgname, ulArgs);
    }
    PAL_CPP_CATCH_ALL
    {
        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_DASMERR),g_szAsmCodeIndent);
        printLine(GUICookie, szString);
    }
    PAL_CPP_ENDTRY

    return fRet;
}

BOOL PrettyPrintGP(                     // prints name of generic param, or returns FALSE
    mdToken tkOwner,                    // Class, method or 0
    CQuickBytes *out,                   // where to put the pretty printed generic param
    int n)                              // Index of generic param
{
    BOOL ret = FALSE;
    if(tkOwner && ((TypeFromToken(tkOwner)==mdtTypeDef)||(TypeFromToken(tkOwner)==mdtMethodDef)))
    {
        DWORD           NumTyPars;
        HENUMInternal   hEnumTyPar;

        if(SUCCEEDED(g_pImport->EnumInit(mdtGenericParam,tkOwner,&hEnumTyPar)))
        {
            NumTyPars = g_pImport->EnumGetCount(&hEnumTyPar);
            if(NumTyPars > (DWORD)n)
            {
                // need this for name dup check
                LPCSTR *pszName = new LPCSTR[NumTyPars];
                if(pszName != NULL)
                {
                    ULONG ulSequence;
                    DWORD ix,nx;
                    mdToken tk;
                    for(ix = 0, nx = 0xFFFFFFFF; ix < NumTyPars; ix++)
                    {
                        if(g_pImport->EnumNext(&hEnumTyPar,&tk))
                        {
                            if(SUCCEEDED(g_pImport->GetGenericParamProps(tk,&ulSequence,NULL,NULL,NULL,&pszName[ix])))
                            {
                                if(ulSequence == (ULONG)n)
                                    nx = ix;
                            }
                        }
                    }
                    // if there are dup names, use !0 or !!0
                    if(nx != 0xFFFFFFFF)
                    {
                        for(ix = 0; ix < nx; ix++)
                        {
                            if(strcmp(pszName[ix],pszName[nx]) == 0)
                                break;
                        }
                        if(ix >= nx)
                        {
                            for(ix = nx+1; ix < NumTyPars; ix++)
                            {
                                if(strcmp(pszName[ix],pszName[nx]) == 0)
                                    break;
                            }
                            if(ix >= NumTyPars)
                            {
                                appendStr(out, ProperName((char*)(pszName[nx])));
                                ret = TRUE;
                            }
                        }
                    } // end if(tkTyPar != 0)
                    delete [] pszName;
                } // end if(pszName != NULL)
            } // end if(NumTyPars > (DWORD)n)
        } // end if(SUCCEEDED(g_pImport->EnumInit(mdtGenericParam,tkOwner,&hEnumTyPar)))
        g_pImport->EnumClose(&hEnumTyPar);
    } // end if(tkOwner)
    return ret;
}

// Pretty-print formal type parameters for a class or method
char *DumpGenericPars(_Inout_updates_(SZSTRING_SIZE) char* szString, mdToken tok, void* GUICookie/*=NULL*/, BOOL fSplit/*=FALSE*/)
{
    WCHAR *wzArgName = wzUniBuf;
    ULONG chName;
    mdToken tkConstr[2048];

    DWORD           NumTyPars;
    DWORD           NumConstrs;
    mdGenericParam  tkTyPar;
    ULONG           ulSequence;
    DWORD           attr;
    mdToken         tkOwner;
    HCORENUM        hEnumTyPar = NULL;
    HCORENUM        hEnumTyParConstr = NULL;
    char*           szptr = &szString[strlen(szString)];
    char*           szbegin;
    unsigned i;

    if (FAILED(g_pPubImport->EnumGenericParams(&hEnumTyPar, tok, &tkTyPar, 1, &NumTyPars)))
      return NULL;
    if (NumTyPars > 0)
    {
      szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),LTN());
      szbegin = szptr;

      for (i = 1; NumTyPars != 0; i++)
      {
        g_pPubImport->GetGenericParamProps(tkTyPar, &ulSequence, &attr, &tkOwner, NULL, wzArgName, UNIBUF_SIZE/2, &chName);
        //if(wcslen(wzArgName) >= MAX_CLASSNAME_LENGTH)
        //    wzArgName[MAX_CLASSNAME_LENGTH-1] = 0;
        hEnumTyParConstr = NULL;
        if (FAILED(g_pPubImport->EnumGenericParamConstraints(&hEnumTyParConstr, tkTyPar, tkConstr, 2048, &NumConstrs)))
        {
            g_pPubImport->CloseEnum(hEnumTyPar);
            return NULL;
        }
        *szptr = 0;
        CHECK_REMAINING_SIZE;
        switch (attr & gpVarianceMask)
        {
            case gpCovariant : szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr), "+ "); break;
            case gpContravariant : szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr), "- "); break;
        }
        CHECK_REMAINING_SIZE;
        if ((attr & gpReferenceTypeConstraint) != 0)
            szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr), "class ");
        CHECK_REMAINING_SIZE;
        if ((attr & gpNotNullableValueTypeConstraint) != 0)
            szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr), "valuetype ");
        CHECK_REMAINING_SIZE;
        if ((attr & gpDefaultConstructorConstraint) != 0)
            szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr), ".ctor ");
        CHECK_REMAINING_SIZE;
        if (NumConstrs)
        {
            CQuickBytes out;
            mdToken tkConstrType,tkOwner;
            szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"(");
            DWORD ix;
            for (ix=0; ix<NumConstrs; ix++)
            {
                if (FAILED(g_pPubImport->GetGenericParamConstraintProps(tkConstr[ix], &tkOwner, &tkConstrType)))
                    return NULL;

                if(ix) szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),", ");
                CHECK_REMAINING_SIZE;
                out.Shrink(0);
                szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s",PrettyPrintClass(&out,tkConstrType,g_pImport));
                CHECK_REMAINING_SIZE;
            }
            if(ix < NumConstrs) break;
            szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),") ");
            CHECK_REMAINING_SIZE;
        }
        // re-get name, wzUniBuf may not contain it any more
        g_pPubImport->GetGenericParamProps(tkTyPar, NULL, &attr, NULL, NULL, wzArgName, UNIBUF_SIZE/2, &chName);
        //if(wcslen(wzArgName) >= MAX_CLASSNAME_LENGTH)
        //    wzArgName[MAX_CLASSNAME_LENGTH-1] = 0;
        if (chName)
        {
            char* sz = (char*)(&wzUniBuf[UNIBUF_SIZE/2]);
            WszWideCharToMultiByte(CP_UTF8,0,wzArgName,-1,sz,UNIBUF_SIZE,NULL,NULL);
            szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s",ProperName(sz));
        }
        CHECK_REMAINING_SIZE;
        if (FAILED(g_pPubImport->EnumGenericParams(&hEnumTyPar, tok, &tkTyPar, 1, &NumTyPars)))
          return NULL;
        if (NumTyPars != 0)
        {
            *szptr++ = ',';

            if(fSplit && (i == 4))
            {
                *szptr = 0;
                printLine(GUICookie,szString);
                i = 0; // mind i++ at the end of the loop
                for(szptr = szString; szptr < szbegin; szptr++) *szptr = ' ';
            }
        }
      } // end for (i = 1; NumTyPars != 0; i++)
      if(NumTyPars != 0) // all type parameters can't fit in szString, error
      {
          strcpy_s(szptr,4,"...");
          szptr += 3;
      }
      else
        szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),GTN());
    } // end if (NumTyPars > 0)
    *szptr = 0;
    if(hEnumTyPar) g_pPubImport->CloseEnum(hEnumTyPar);
    return szptr;
}

void DumpGenericParsCA(mdToken tok, void* GUICookie/*=NULL*/)
{
    DWORD           NumTyPars;
    mdGenericParam  tkTyPar;
    HCORENUM        hEnumTyPar = NULL;
    unsigned i;
    WCHAR *wzArgName = wzUniBuf;
    ULONG chName;
    DWORD           attr;

    if(g_fShowCA)
    {
        for(i=0; SUCCEEDED(g_pPubImport->EnumGenericParams(&hEnumTyPar, tok, &tkTyPar, 1, &NumTyPars))
                    &&(NumTyPars > 0); i++)
        {
            HENUMInternal    hEnum;
            mdCustomAttribute tkCA;
            ULONG           ulCAs= 0;

            if (FAILED(g_pImport->EnumInit(mdtCustomAttribute, tkTyPar, &hEnum)))
            {
                sprintf_s(szString, SZSTRING_SIZE, "%sERROR: MetaData error enumerating CustomAttribute for %08X", g_szAsmCodeIndent, tkTyPar);
                printLine(GUICookie, szString);
                return;
            }
            ulCAs = g_pImport->EnumGetCount(&hEnum);
            if(ulCAs)
            {
                char    *szptr = &szString[0];
                szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".param type"));
                if(SUCCEEDED(g_pPubImport->GetGenericParamProps(tkTyPar, NULL, &attr, NULL, NULL, wzArgName, UNIBUF_SIZE/2, &chName))
                        &&(chName > 0))
                {
                    //if(wcslen(wzArgName) >= MAX_CLASSNAME_LENGTH)
                    //    wzArgName[MAX_CLASSNAME_LENGTH-1] = 0;
                    char* sz = (char*)(&wzUniBuf[UNIBUF_SIZE/2]);
                    WszWideCharToMultiByte(CP_UTF8,0,wzArgName,-1,sz,UNIBUF_SIZE,NULL,NULL);
                    szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s ",ProperName(sz));
                }
                else
                    szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"[%d] ",i+1);
                if(g_fDumpTokens) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),tkTyPar);
                printLine(GUICookie, szString);

                strcat_s(g_szAsmCodeIndent, MAX_MEMBER_LENGTH, "  ");
                while(g_pImport->EnumNext(&hEnum,&tkCA) && RidFromToken(tkCA))
                {
                    DumpCustomAttribute(tkCA,GUICookie,false);
                }
                g_szAsmCodeIndent[strlen(g_szAsmCodeIndent) - 2] = 0;
            }
            g_pImport->EnumClose( &hEnum);  // mdtCustomAttribute

            ULONG    ulSequence;
            DWORD    attr;
            mdToken  tkOwner;
            HCORENUM hEnumTyParConstraint;
            mdToken  tkConstraint[2048];
            DWORD    NumConstraints;

            g_pPubImport->GetGenericParamProps(tkTyPar, &ulSequence, &attr, &tkOwner, NULL, wzArgName, UNIBUF_SIZE / 2, &chName);
            hEnumTyParConstraint = NULL;
            if (FAILED(g_pPubImport->EnumGenericParamConstraints(&hEnumTyParConstraint, tkTyPar, tkConstraint, 2048, &NumConstraints)))
            {
                g_pPubImport->CloseEnum(hEnumTyPar);
                return;
            }
            if (NumConstraints > 0)
            {
                CQuickBytes out;
                mdToken tkConstraintType;
                mdToken tkGenericParam;
                ULONG ulSequence;

                for (DWORD ix = 0; ix < NumConstraints; ix++)
                {
                    mdGenericParamConstraint  tkParamConstraint = tkConstraint[ix];
                    if (FAILED(g_pPubImport->GetGenericParamConstraintProps(tkParamConstraint, &tkGenericParam, &tkConstraintType)))
                    {
                        sprintf_s(szString, SZSTRING_SIZE, "%sERROR: MetaData error in GetGenericParamConstraintProps for %08X", g_szAsmCodeIndent, tkParamConstraint);
                        return;
                    }
                    if (FAILED(g_pImport->EnumInit(mdtCustomAttribute, tkParamConstraint, &hEnum)))
                    {
                        sprintf_s(szString, SZSTRING_SIZE, "%sERROR: MetaData error enumerating CustomAttribute for mdGenericParamConstraint %08X", g_szAsmCodeIndent, tkParamConstraint);
                        printLine(GUICookie, szString);
                        return;
                    }

                    ulCAs = g_pImport->EnumGetCount(&hEnum);
                    if (ulCAs)
                    {
                        char    *szptr = &szString[0];
                        szptr += sprintf_s(szptr, SZSTRING_SIZE, "%s%s ", g_szAsmCodeIndent, KEYWORD(".param constraint"));

                        if (FAILED(g_pPubImport->GetGenericParamProps(tkGenericParam, &ulSequence, &attr, NULL, NULL, wzArgName, UNIBUF_SIZE / 2, &chName)))
                        {
                            sprintf_s(szString, SZSTRING_SIZE, "%sERROR: MetaData error in GetGenericParamProps for %08X", g_szAsmCodeIndent, tkGenericParam);
                            printLine(GUICookie, szString);
                            return;
                        }
                        if (chName > 0)
                        {
                            char* sz = (char*)(&wzUniBuf[UNIBUF_SIZE / 2]);
                            WszWideCharToMultiByte(CP_UTF8, 0, wzArgName, -1, sz, UNIBUF_SIZE, NULL, NULL);
                            szptr += sprintf_s(szptr, SZSTRING_REMAINING_SIZE(szptr), "  %s", ProperName(sz));
                        }
                        else
                        {
                            szptr += sprintf_s(szptr, SZSTRING_REMAINING_SIZE(szptr), "  [%d]", ulSequence + 1);
                        }
                        if (g_fDumpTokens)
                        {
                            szptr += sprintf_s(szptr, SZSTRING_REMAINING_SIZE(szptr), COMMENT("/*%08X*/ "), tkGenericParam);
                        }

                        szptr += sprintf_s(szptr, SZSTRING_REMAINING_SIZE(szptr), ", ");

                        out.Shrink(0);
                        szptr += sprintf_s(szptr, SZSTRING_REMAINING_SIZE(szptr), "%s", PrettyPrintClass(&out, tkConstraintType, g_pImport));
                        printLine(GUICookie, szString);

                        strcat_s(g_szAsmCodeIndent, MAX_MEMBER_LENGTH, "  ");
                        while (g_pImport->EnumNext(&hEnum, &tkCA) && RidFromToken(tkCA))
                        {
                            DumpCustomAttribute(tkCA, GUICookie, false);
                        }
                        g_szAsmCodeIndent[strlen(g_szAsmCodeIndent) - 2] = 0;
                    }
                    g_pImport->EnumClose(&hEnum);  // mdtCustomAttribute
                }
            }
        } //end for(i=0;...
    } //end if(g_fShowCA)
}

// Sets *pbOverridingTypeSpec to TRUE if we are overriding a method declared by a type spec or
// if the method has a signature which does not exactly match between the overrider and overridee.
// That case is commonly caused by covariant overrides.
// In that case the syntax is slightly different (there are additional 'method' keywords).
// Refer to Expert .NET 2.0 IL Assembler page 242.
void PrettyPrintOverrideDecl(ULONG i, __inout __nullterminated char* szString, void* GUICookie, mdToken tkOverrider,
                             BOOL *pbOverridingTypeSpec)
{
    const char *    pszMemberName;
    mdToken         tkDecl,tkDeclParent=0;
    char            szBadToken[256];
    char            pszTailSigDefault[] = "";
    char*           pszTailSig = pszTailSigDefault;
    CQuickBytes     qbInstSig;
    char*           szptr = &szString[0];
    szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".override"));
    tkDecl = (*g_pmi_list)[i].tkDecl;

    *pbOverridingTypeSpec = FALSE;

    if(g_pImport->IsValidToken(tkDecl))
    {
        bool needsFullTokenPrint = false;
        bool hasTkDeclParent = false;

        // Determine if the decl is a typespec method, in which case the "method" syntax + full token print
        // must be used to generate the disassembly.
        if(SUCCEEDED(g_pImport->GetParentToken(tkDecl,&tkDeclParent)))
        {
            if(g_pImport->IsValidToken(tkDeclParent))
            {
                if(TypeFromToken(tkDeclParent) == mdtMethodDef) //get the parent's parent
                {
                    mdTypeRef cr1;
                    if(FAILED(g_pImport->GetParentToken(tkDeclParent,&cr1))) cr1 = mdTypeRefNil;
                    tkDeclParent = cr1;
                }
                if(RidFromToken(tkDeclParent))
                {
                    if(TypeFromToken(tkDeclParent)==mdtTypeSpec)
                    {
                        needsFullTokenPrint = true;
                    }
                    hasTkDeclParent = true;
                }
            }
            else
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s",ERRORMSG("INVALID OVERRIDDEN METHOD'S PARENT TOKEN"));
        }

        // Determine if the sig of the decl does not match the sig of the body
        // In that case the full "method" syntax must be used
        if ((TypeFromToken(tkOverrider) == mdtMethodDef) && !needsFullTokenPrint)
        {
            PCCOR_SIGNATURE pComSigDecl = NULL;
            ULONG cComSigDecl = 0;
            mdToken tkDeclSigTok = tkDecl;
            bool successfullyGotDeclSig = false;
            bool successfullyGotBodySig = false;

            if (TypeFromToken(tkDeclSigTok) == mdtMethodSpec)
            {
                mdToken         meth=0;
                if (SUCCEEDED(g_pImport->GetMethodSpecProps(tkDeclSigTok, &meth, NULL, NULL)))
                {
                    tkDeclSigTok = meth;
                }
            }

            if (TypeFromToken(tkDeclSigTok) == mdtMethodDef)
            {
                if (SUCCEEDED(g_pImport->GetSigOfMethodDef(tkDeclSigTok, &cComSigDecl, &pComSigDecl)))
                {
                    successfullyGotDeclSig = true;
                }
            }
            else if (TypeFromToken(tkDeclSigTok) == mdtMemberRef)
            {
                const char *pszMemberNameUnused;
                if (SUCCEEDED(g_pImport->GetNameAndSigOfMemberRef(
                    tkDeclSigTok,
                    &pComSigDecl,
                    &cComSigDecl,
                    &pszMemberNameUnused)))
                {
                    successfullyGotDeclSig = true;
                }
            }

            PCCOR_SIGNATURE pComSigBody;
            ULONG cComSigBody;
            if (SUCCEEDED(g_pImport->GetSigOfMethodDef(tkOverrider, &cComSigBody, &pComSigBody)))
            {
                successfullyGotBodySig = true;
            }

            if (successfullyGotDeclSig && successfullyGotBodySig)
            {
                if (cComSigBody != cComSigDecl)
                {
                    needsFullTokenPrint = true;
                }
                else if (memcmp(pComSigBody, pComSigDecl, cComSigBody) != 0)
                {
                    needsFullTokenPrint = true;
                }

                // Signature are binary identical, full sig printing not needed
            }
            else
            {
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s",ERRORMSG("INVALID BODY OR DECL SIG"));
            }
        }

        if (needsFullTokenPrint)
        {
            // In this case, the shortcut syntax cannot be used, and a full token must be printed.
            // Print the full token and return.
            szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr), " %s ",KEYWORD("method"));
            PrettyPrintToken(szString,tkDecl,g_pImport,GUICookie,tkOverrider);

            *pbOverridingTypeSpec = TRUE;
            return;
        }

        if (hasTkDeclParent)
        {
            // If the tkDeclParent was successfully retrieved during parent discovery print it here.
            PrettyPrintToken(szString, tkDeclParent, g_pImport,GUICookie,tkOverrider);
            strcat_s(szString, SZSTRING_SIZE,"::");
            szptr = &szString[strlen(szString)];
        }

        if(TypeFromToken(tkDecl) == mdtMethodSpec)
        {
            mdToken         meth=0;
            PCCOR_SIGNATURE pSig=NULL;
            ULONG       cSig=0;
            if (FAILED(g_pImport->GetMethodSpecProps(tkDecl, &meth, &pSig, &cSig)))
            {
                meth = mdTokenNil;
                pSig = NULL;
                cSig = 0;
            }

            if (pSig && cSig)
            {
              qbInstSig.Shrink(0);
              pszTailSig = (char*)PrettyPrintSig(pSig, cSig, "", &qbInstSig, g_pImport, NULL);
            }
            tkDecl = meth;
        }
        if(TypeFromToken(tkDecl) == mdtMethodDef)
        {
            if (FAILED(g_pImport->GetNameOfMethodDef(tkDecl, &pszMemberName)))
            {
                sprintf_s(szBadToken,256,ERRORMSG("INVALID RECORD: 0x%8.8X"),tkDecl);
                pszMemberName = (const char *)szBadToken;
            }
        }
        else if(TypeFromToken(tkDecl) == mdtMemberRef)
        {
            PCCOR_SIGNATURE pComSig;
            ULONG       cComSig;

            if (FAILED(g_pImport->GetNameAndSigOfMemberRef(
                tkDecl,
                &pComSig,
                &cComSig,
                &pszMemberName)))
            {
                sprintf_s(szBadToken,256,ERRORMSG("INVALID RECORD: 0x%8.8X"),tkDecl);
                pszMemberName = (const char *)szBadToken;
            }
        }
        else
        {
            sprintf_s(szBadToken,256,ERRORMSG("INVALID TOKEN: 0x%8.8X"),tkDecl);
            pszMemberName = (const char*)szBadToken;
        }
        MAKE_NAME_IF_NONE(pszMemberName,tkDecl);
    }
    else
    {
        sprintf_s(szBadToken,256,ERRORMSG("INVALID TOKEN: 0x%8.8X"),tkDecl);
        pszMemberName = (const char*)szBadToken;
    }
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s%s",ProperName((char*)pszMemberName),pszTailSig);

    if(g_fDumpTokens) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT(" /*%08X::%08X*/ "),tkDeclParent,(*g_pmi_list)[i].tkDecl);
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
BOOL DumpMethod(mdToken FuncToken, const char *pszClassName, DWORD dwEntryPointToken,void *GUICookie,BOOL DumpBody)
{
    const char      *pszMemberName = NULL;//[MAX_MEMBER_LENGTH];
    const char      *pszMemberSig = NULL;
    DWORD           dwAttrs = 0;
    DWORD           dwImplAttrs;
    DWORD           dwOffset;
    DWORD           dwTargetRVA;
    CQuickBytes     qbMemberSig;
    PCCOR_SIGNATURE pComSig = NULL;
    ULONG           cComSig;
    char            *buff = NULL;//[MAX_MEMBER_LENGTH];
    ParamDescriptor* pszArgname = NULL;
    ULONG           ulArgs=0;
    unsigned        retParamIx = 0;
    unsigned        uStringLen = SZSTRING_SIZE;
    char szArgPrefix[MAX_PREFIX_SIZE];
    char*           szptr = NULL;
    mdToken         tkMVarOwner = g_tkMVarOwner;

    if (FAILED(g_pImport->GetMethodDefProps(FuncToken, &dwAttrs)))
    {
        sprintf_s(szString, SZSTRING_SIZE, "%sERROR: MethodDef %08X has wrong record", g_szAsmCodeIndent, FuncToken);
        printError(GUICookie, ERRORMSG(szString));
        return FALSE;
    }
    if (g_fLimitedVisibility)
    {
            if(g_fHidePub && IsMdPublic(dwAttrs)) return FALSE;
            if(g_fHidePriv && IsMdPrivate(dwAttrs)) return FALSE;
            if(g_fHideFam && IsMdFamily(dwAttrs)) return FALSE;
            if(g_fHideAsm && IsMdAssem(dwAttrs)) return FALSE;
            if(g_fHideFOA && IsMdFamORAssem(dwAttrs)) return FALSE;
            if(g_fHideFAA && IsMdFamANDAssem(dwAttrs)) return FALSE;
            if(g_fHidePrivScope && IsMdPrivateScope(dwAttrs)) return FALSE;
    }
    if (FAILED(g_pImport->GetMethodImplProps(FuncToken, &dwOffset, &dwImplAttrs)))
    {
        sprintf_s(szString, SZSTRING_SIZE, "%sERROR: Invalid MethodImpl %08X record", g_szAsmCodeIndent, FuncToken);
        printError(GUICookie, ERRORMSG(szString));
        return FALSE;
    }
    if (FAILED(g_pImport->GetNameOfMethodDef(FuncToken, &pszMemberName)))
    {
        sprintf_s(szString, SZSTRING_SIZE, "%sERROR: MethodDef %08X has wrong record", g_szAsmCodeIndent, FuncToken);
        printError(GUICookie, ERRORMSG(szString));
        return FALSE;
    }
    MAKE_NAME_IF_NONE(pszMemberName,FuncToken);
    if (FAILED(g_pImport->GetSigOfMethodDef(FuncToken, &cComSig, &pComSig)))
    {
        pComSig = NULL;
    }

    if (cComSig == NULL)
    {
        sprintf_s(szString, SZSTRING_SIZE, "%sERROR: method '%s' has no signature", g_szAsmCodeIndent, pszMemberName);
        printError(GUICookie, ERRORMSG(szString));
        return FALSE;
    }
    bool bRet = FALSE;

    PAL_CPP_TRY {
        g_tkMVarOwner = FuncToken;
        szString[0] = 0;
        DumpGenericPars(szString,FuncToken); //,NULL,FALSE);
        pszMemberSig = PrettyPrintSig(pComSig, cComSig, szString, &qbMemberSig, g_pImport,NULL);
    } PAL_CPP_CATCH_ALL {
        printError(GUICookie,"INVALID DATA ADDRESS");
        bRet = TRUE;
    } PAL_CPP_ENDTRY;

    if (bRet)
    {
        g_tkMVarOwner = tkMVarOwner;
        return FALSE;
    }

    if (g_Mode == MODE_DUMP_CLASS_METHOD || g_Mode == MODE_DUMP_CLASS_METHOD_SIG)
    {
        if (strcmp(pszMemberName, g_pszMethodToDump) != 0) return FALSE;

        if (g_Mode == MODE_DUMP_CLASS_METHOD_SIG)
        {
            // we want plain signature without token values
            const char *pszPlainSig;
            if (g_fDumpTokens)
            {
                // temporarily disable token dumping
                g_fDumpTokens = FALSE;

                PAL_CPP_TRY
                {
                    CQuickBytes qbTempSig;
                    pszPlainSig = PrettyPrintSig(pComSig, cComSig, "", &qbTempSig, g_pImport, NULL);
                }
                PAL_CPP_CATCH_ALL
                {
                    pszPlainSig = "";
                }
                PAL_CPP_ENDTRY;

                g_fDumpTokens = TRUE;
            }
            else
            {
                pszPlainSig = pszMemberSig;
            }

            if (strcmp(pszPlainSig, g_pszSigToDump) != 0) return FALSE;
        }
    }

    if(!DumpBody)
    {
        printLine(GUICookie,(char*)pszMemberSig);
        g_tkMVarOwner = tkMVarOwner;
        return TRUE;
    }

    szptr = &szString[0];
    szString[0] = 0;
    if(DumpBody) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s%s ",g_szAsmCodeIndent,ANCHORPT(KEYWORD(".method"),FuncToken));
    else szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s ",ANCHORPT(KEYWORD(".method"),FuncToken));

    if(g_fDumpTokens)               szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),FuncToken);
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD((char*)0));
    if(IsMdPublic(dwAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"public ");
    if(IsMdPrivate(dwAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"private ");
    if(IsMdFamily(dwAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"family ");
    if(IsMdAssem(dwAttrs))          szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"assembly ");
    if(IsMdFamANDAssem(dwAttrs))    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"famandassem ");
    if(IsMdFamORAssem(dwAttrs))     szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"famorassem ");
    if(IsMdPrivateScope(dwAttrs))   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"privatescope ");
    if(IsMdHideBySig(dwAttrs))      szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"hidebysig ");
    if(IsMdNewSlot(dwAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"newslot ");
    if(IsMdSpecialName(dwAttrs))    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"specialname ");
    if(IsMdRTSpecialName(dwAttrs))  szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"rtspecialname ");
    if (IsMdStatic(dwAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"static ");
    if (IsMdAbstract(dwAttrs))      szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"abstract ");
    if (dwAttrs & 0x00000200)       szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"strict ");
    if (IsMdVirtual(dwAttrs))       szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"virtual ");
    if (IsMdFinal(dwAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"final ");
    if (IsMdUnmanagedExport(dwAttrs))      szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"unmanagedexp ");
    if(IsMdRequireSecObject(dwAttrs))      szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"reqsecobj ");
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD((char*)-1));
    if (IsMdPinvokeImpl(dwAttrs))
    {
        DWORD   dwMappingFlags;
        const char  *szImportName;
        mdModuleRef mrImportDLL;

        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s(",KEYWORD("pinvokeimpl"));
        if(FAILED(g_pImport->GetPinvokeMap(FuncToken,&dwMappingFlags,
            &szImportName,&mrImportDLL)))  szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/* No map */"));
        else
            szptr=DumpPinvokeMap(dwMappingFlags,  (strcmp(szImportName,pszMemberName)? szImportName : NULL),
                mrImportDLL,szString,GUICookie);
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),") ");
    }
    // A little hack to get the formatting we need for Assem.
    buff = new char[SZSTRING_SIZE];
    if(buff==NULL)
    {
        printError(GUICookie,"Out of memory");
        g_tkMVarOwner = tkMVarOwner;
        return FALSE;
    }
    g_fThisIsInstanceMethod = !IsMdStatic(dwAttrs);
    {
        const char *psz = NULL;
        if(IsMdPrivateScope(dwAttrs))
            sprintf_s(buff,SZSTRING_SIZE,"%s$PST%08X", pszMemberName,FuncToken );
        else
            strcpy_s(buff,SZSTRING_SIZE, pszMemberName );

        psz = ProperName(buff);
        if(psz != buff)
        {
            strcpy_s(buff,SZSTRING_SIZE,psz);
        }
    }

    DumpGenericPars(buff, FuncToken); //, NULL, FALSE);

    qbMemberSig.Shrink(0);
    // Get the argument names, if any
    strcpy_s(szArgPrefix,MAX_PREFIX_SIZE,(g_fThisIsInstanceMethod ? "A1": "A0"));
    {
        PCCOR_SIGNATURE typePtr = pComSig;
        unsigned ulCallConv = CorSigUncompressData(typePtr);  // get the calling convention out of the way
        if (ulCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
           CorSigUncompressData(typePtr); // get the num of generic args out of the way
        unsigned  numArgs = CorSigUncompressData(typePtr)+1;
        HENUMInternal    hArgEnum;
        mdParamDef  tkArg;
        if (FAILED(g_pImport->EnumInit(mdtParamDef,FuncToken,&hArgEnum)))
        {
            printError(GUICookie, "Invalid MetaDataFormat");
            g_tkMVarOwner = tkMVarOwner;
            return FALSE;
        }
        ulArgs = g_pImport->EnumGetCount(&hArgEnum);
        retParamIx = numArgs-1;
        if (ulArgs < numArgs) ulArgs = numArgs;
        if (ulArgs != 0)
        {
            pszArgname = new ParamDescriptor[ulArgs+2];
            memset(pszArgname,0,(ulArgs+2)*sizeof(ParamDescriptor));
            LPCSTR szName;
            ULONG ulSequence, ix;
            USHORT wSequence;
            DWORD dwAttr;
            ULONG j;
            for (j=0; g_pImport->EnumNext(&hArgEnum,&tkArg) && RidFromToken(tkArg); j++)
            {
                if (FAILED(g_pImport->GetParamDefProps(tkArg, &wSequence, &dwAttr, &szName)))
                {
                    char sz[256];
                    sprintf_s(sz, ARRAY_SIZE(sz), RstrUTF(IDS_E_INVALIDRECORD), tkArg);
                    printError(GUICookie, sz);
                    continue;
                }
                ulSequence = wSequence;

                if (ulSequence > ulArgs+1)
                {
                    char sz[256];
                    sprintf_s(sz,256,RstrUTF(IDS_E_PARAMSEQNO),j,ulSequence,ulSequence);
                    printError(GUICookie,sz);
                }
                else
                {
                    ix = retParamIx;
                    if (ulSequence != 0)
                    {
                        ix = ulSequence-1;
                        if (*szName != 0)
                        {
                            pszArgname[ix].name = new char[strlen(szName)+1];
                            strcpy_s(pszArgname[ix].name,strlen(szName)+1,szName);
                        }
                    }
                    pszArgname[ix].attr = dwAttr;
                    pszArgname[ix].tok = tkArg;
                }
            }// end for( along the params)
            for (j=0; j <numArgs; j++)
            {
                if(pszArgname[j].name == NULL) // we haven't got the name!
                {
                    pszArgname[j].name = new char[16];
                    *pszArgname[j].name = 0;
                }
                if(*pszArgname[j].name == 0) // we haven't got the name!
                {
                    sprintf_s(pszArgname[j].name,16,"A_%d",g_fThisIsInstanceMethod ? j+1 : j);
                }
            }// end for( along the argnames)
            sprintf_s(szArgPrefix,MAX_PREFIX_SIZE,"@%Id0",(size_t)pszArgname);
        } //end if (ulArgs)
        g_pImport->EnumClose(&hArgEnum);
    }
    g_tkRefUser = FuncToken;
    PrettyPrintMethodSig(szString, &uStringLen, &qbMemberSig, pComSig, cComSig,
                          buff, szArgPrefix, GUICookie);
    g_tkRefUser = 0;
    szptr = &szString[strlen(szString)];
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD((char*)0));
    if(IsMiNative(dwImplAttrs))             szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," native");
    if(IsMiIL(dwImplAttrs))                 szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," cil");
    if(IsMiOPTIL(dwImplAttrs))              szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," optil");
    if(IsMiRuntime(dwImplAttrs))            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," runtime");
    if(IsMiUnmanaged(dwImplAttrs))          szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," unmanaged");
    if(IsMiManaged(dwImplAttrs))            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," managed");
    if(IsMiPreserveSig(dwImplAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," preservesig");
    if(IsMiForwardRef(dwImplAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," forwardref");
    if(IsMiInternalCall(dwImplAttrs))       szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," internalcall");
    if(IsMiSynchronized(dwImplAttrs))       szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," synchronized");
    if(IsMiNoInlining(dwImplAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," noinlining");
    if(IsMiAggressiveInlining(dwImplAttrs)) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," aggressiveinlining");
    if(IsMiNoOptimization(dwImplAttrs))     szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," nooptimization");
    if(IsMiAggressiveOptimization(dwImplAttrs)) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," aggressiveoptimization");
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD((char*)-1));
    printLine(GUICookie, szString);
    VDELETE(buff);

    if(!DumpBody)
    {
        g_tkMVarOwner = tkMVarOwner;
        return TRUE;
    }

    if(g_fShowBytes)
    {
        if (FAILED(g_pImport->GetSigOfMethodDef(FuncToken, &cComSig, &pComSig)))
        {
            sprintf_s(szString,SZSTRING_SIZE,"%sERROR: method %08X has wrong record",g_szAsmCodeIndent,FuncToken);
            printError(GUICookie,ERRORMSG(szString));
            return FALSE;
        }
        const char* szt = "SIG:";
        for(ULONG i=0; i<cComSig;)
        {
            szptr = &szString[0];
            szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s// %s", g_szAsmCodeIndent, szt);
            while(i<cComSig)
            {
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," %02X",pComSig[i]);
                i++;
                if((i & 0x1F)==0) break;  // print only 32 per line
            }
            printLine(GUICookie, COMMENT(szString));
            szt = "    ";
        }
    }

    szptr = &szString[0];
    szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s", g_szAsmCodeIndent,SCOPE());
    printLine(GUICookie, szString);
    szptr = &szString[0];
    strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"  ");

    // We have recoreded the entry point token from the CLR Header.  Check to see if this
    // method is the entry point.
    if(FuncToken == static_cast<mdToken>(dwEntryPointToken))
    {
        sprintf_s(szString,SZSTRING_SIZE,"%s%s", g_szAsmCodeIndent,KEYWORD(".entrypoint"));
        printLine(GUICookie, szString);
    }
    DumpCustomAttributes(FuncToken,GUICookie);
    DumpGenericParsCA(FuncToken,GUICookie);
    DumpParams(pszArgname, retParamIx, GUICookie);
    DumpPermissions(FuncToken,GUICookie);
    // Check if the method represents entry in VTable fixups and in EATable
    {
        ULONG j;
        for(j=0; j<g_nVTableRef; j++)
        {
            if((*g_prVTableRef)[j].tkTok == FuncToken)
            {
                sprintf_s(szString,SZSTRING_SIZE,"%s%s %d : %d",
                    g_szAsmCodeIndent,KEYWORD(".vtentry"),(*g_prVTableRef)[j].wEntry+1,(*g_prVTableRef)[j].wSlot+1);
                printLine(GUICookie, szString);
                break;
            }
        }
        for(j=0; j<g_nEATableRef; j++)
        {
            if((*g_prEATableRef)[j].tkTok == FuncToken)
            {
                szptr = &szString[0];
                szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s [%d] ",
                    g_szAsmCodeIndent,KEYWORD(".export"),j+g_nEATableBase);
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s %s",
                    KEYWORD("as"), ProperName((*g_prEATableRef)[j].pszName));
                printLine(GUICookie, szString);
                break;
            }
        }
    }
    // Dump method impls of this method:
    for(ULONG i = 0; i < g_NumMI; i++)
    {
        if((*g_pmi_list)[i].tkBody == FuncToken)
        {
            BOOL bOverridingTypeSpec;
            PrettyPrintOverrideDecl(i,szString,GUICookie,FuncToken,&bOverridingTypeSpec);
            printLine(GUICookie,szString);
        }
    }
    dwTargetRVA = dwOffset;
    if (IsMdPinvokeImpl(dwAttrs))
    {
        if(dwOffset)
        {
            sprintf_s(szString,SZSTRING_SIZE,"%s// Embedded native code",g_szAsmCodeIndent);
            printLine(GUICookie, COMMENT(szString));
            goto ItsMiNative;
        }
        if(g_szAsmCodeIndent[0]) g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
        sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,UNSCOPE());
        printLine(GUICookie, szString);
        g_tkMVarOwner = tkMVarOwner;
        return TRUE;
    }

    if(IsMiManaged(dwImplAttrs))
    {
        if(IsMiIL(dwImplAttrs) || IsMiOPTIL(dwImplAttrs))
        {
            if(g_fShowBytes)
            {
                sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_METHBEG), g_szAsmCodeIndent,dwTargetRVA);
                printLine(GUICookie, COMMENT(szString));
            }
            szString[0] = 0;
            if (dwTargetRVA != 0)
            {
                void* newTarget = NULL;
                if(g_pPELoader->getVAforRVA(dwTargetRVA,&newTarget))
                {
                    DisassembleWrapper(g_pImport, (unsigned char*)newTarget, GUICookie, FuncToken,pszArgname, ulArgs);
                }
                else
                {
                    sprintf_s(szString,SZSTRING_SIZE, "INVALID METHOD ADDRESS: 0x%8.8zX (RVA: 0x%8.8X)",(size_t)newTarget,dwTargetRVA);
                    printError(GUICookie,szString);
                }
            }
        }
        else if(IsMiNative(dwImplAttrs))
        {
ItsMiNative:
            sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_DASMNATIVE), g_szAsmCodeIndent);
            printLine(GUICookie, COMMENT(szString));

            sprintf_s(szString,SZSTRING_SIZE,"%s//  Managed TargetRVA = 0x%8.8X", g_szAsmCodeIndent, dwTargetRVA);
            printLine(GUICookie, COMMENT(szString));
        }
    }
    else if(IsMiUnmanaged(dwImplAttrs)&&IsMiNative(dwImplAttrs))
    {
        _ASSERTE(IsMiNative(dwImplAttrs));
        sprintf_s(szString,SZSTRING_SIZE,"%s//  Unmanaged TargetRVA = 0x%8.8X", g_szAsmCodeIndent, dwTargetRVA);
        printLine(GUICookie, COMMENT(szString));
    }
    else if(IsMiRuntime(dwImplAttrs))
    {
        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_METHODRT), g_szAsmCodeIndent);
        printLine(GUICookie, COMMENT(szString));
    }
#ifdef _DEBUG
    else  _ASSERTE(!"Bad dwImplAttrs");
#endif

    if(g_szAsmCodeIndent[0]) g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
    {
        szptr = &szString[0];
        szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,UNSCOPE());
        if(pszClassName)
        {
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("// end of method %s::"), ProperName((char*)pszClassName));
            strcpy_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT(ProperName((char*)pszMemberName)));
        }
        else
            sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("// end of global method %s"), ProperName((char*)pszMemberName));
     }
    printLine(GUICookie, szString);
    szString[0] = 0;
    printLine(GUICookie, szString);

    if(pszArgname)
    {
        for(ULONG i=0; i < ulArgs; i++)
        {
            if(pszArgname[i].name) VDELETE(pszArgname[i].name);
        }
        VDELETE(pszArgname);
    }
    g_tkMVarOwner = tkMVarOwner;
    return TRUE;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

BOOL DumpField(mdToken FuncToken, const char *pszClassName,void *GUICookie, BOOL DumpBody)
{
    char            *pszMemberName = NULL;//[MAX_MEMBER_LENGTH];
    DWORD           dwAttrs = 0;
    CQuickBytes     qbMemberSig;
    PCCOR_SIGNATURE pComSig = NULL;
    ULONG           cComSig;
    const char     *szStr = NULL;//[1024];
    char*           szptr;

    const char *psz;
    if (FAILED(g_pImport->GetNameOfFieldDef(FuncToken, &psz)))
    {
        char sz[2048];
        sprintf_s(sz, 2048, "%sERROR: FieldDef %08X has no signature", g_szAsmCodeIndent, FuncToken);
        printError(GUICookie, sz);
        return FALSE;
    }
    MAKE_NAME_IF_NONE(psz,FuncToken);

    if (FAILED(g_pImport->GetFieldDefProps(FuncToken, &dwAttrs)))
    {
        char sz[2048];
        sprintf_s(sz, 2048, "%sERROR: FieldDef %08X record error", g_szAsmCodeIndent, FuncToken);
        printError(GUICookie, sz);
        return FALSE;
    }
    if (g_fLimitedVisibility)
    {
        if(g_fHidePub && IsFdPublic(dwAttrs)) return FALSE;
        if(g_fHidePriv && IsFdPrivate(dwAttrs)) return FALSE;
        if(g_fHideFam && IsFdFamily(dwAttrs)) return FALSE;
        if(g_fHideAsm && IsFdAssembly(dwAttrs)) return FALSE;
        if(g_fHideFOA && IsFdFamORAssem(dwAttrs)) return FALSE;
        if(g_fHideFAA && IsFdFamANDAssem(dwAttrs)) return FALSE;
        if(g_fHidePrivScope && IsFdPrivateScope(dwAttrs)) return FALSE;
    }

    {
        const char* psz1 = NULL;
        if(IsFdPrivateScope(dwAttrs))
        {
            pszMemberName = new char[strlen(psz)+15];
            sprintf_s(pszMemberName,strlen(psz)+15,"%s$PST%08X", psz,FuncToken );
        }
        else
        {
            pszMemberName = new char[strlen(psz)+3];
            strcpy_s(pszMemberName, strlen(psz)+3, psz );
        }
        psz1 = ProperName(pszMemberName);
        VDELETE(pszMemberName);
        pszMemberName = new char[strlen(psz1)+1];
        strcpy_s(pszMemberName,strlen(psz1)+1,psz1);
    }
    if (FAILED(g_pImport->GetSigOfFieldDef(FuncToken, &cComSig, &pComSig)))
    {
        pComSig = NULL;
    }
    if (cComSig == NULL)
    {
        char sz[2048];
        sprintf_s(sz,2048,"%sERROR: field '%s' has no signature",g_szAsmCodeIndent,pszMemberName);
        VDELETE(pszMemberName);
        printError(GUICookie,sz);
        return FALSE;
    }
    g_tkRefUser = FuncToken;

    bool bRet = FALSE;
    PAL_CPP_TRY {
        szStr = PrettyPrintSig(pComSig, cComSig, (DumpBody ? pszMemberName : ""), &qbMemberSig, g_pImport,NULL);
    }
    PAL_CPP_CATCH_ALL
    {
        printError(GUICookie,"INVALID ADDRESS IN FIELD SIGNATURE");
        bRet = TRUE;
    } PAL_CPP_ENDTRY;

    if (bRet)
        return FALSE;

    g_tkRefUser = 0;

    if (g_Mode == MODE_DUMP_CLASS_METHOD || g_Mode == MODE_DUMP_CLASS_METHOD_SIG)
    {
        if (strcmp(pszMemberName, g_pszMethodToDump) != 0)
        {
            VDELETE(pszMemberName);
            return FALSE;
        }

        if (g_Mode == MODE_DUMP_CLASS_METHOD_SIG)
        {
            // we want plain signature without token values and without the field name
            BOOL fDumpTokens = g_fDumpTokens;
            g_fDumpTokens = FALSE;

            const char *pszPlainSig;
            PAL_CPP_TRY
            {
                CQuickBytes qbTempSig;
                pszPlainSig = PrettyPrintSig(pComSig, cComSig, "", &qbTempSig, g_pImport, NULL);
            }
            PAL_CPP_CATCH_ALL
            {
                pszPlainSig = "";
            }
            PAL_CPP_ENDTRY;

            g_fDumpTokens = fDumpTokens;

            if (strcmp(pszPlainSig, g_pszSigToDump) != 0)
            {
                VDELETE(pszMemberName);
                return FALSE;
            }
        }
    }
    VDELETE(pszMemberName);

    szptr = &szString[0];
    if(DumpBody)
    {
        szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ", g_szAsmCodeIndent,ANCHORPT(KEYWORD(".field"),FuncToken));
        if(g_fDumpTokens) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),FuncToken);
    }

    // put offset (if any)
    for(ULONG i=0; i < g_cFieldOffsets; i++)
    {
        if(g_rFieldOffset[i].ridOfField == FuncToken)
        {
            if(g_rFieldOffset[i].ulOffset != 0xFFFFFFFF) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"[%d] ",g_rFieldOffset[i].ulOffset);
            break;
        }
    }

    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD((char*)0));
    if(IsFdPublic(dwAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"public ");
    if(IsFdPrivate(dwAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"private ");
    if(IsFdStatic(dwAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"static ");
    if(IsFdFamily(dwAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"family ");
    if(IsFdAssembly(dwAttrs))       szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"assembly ");
    if(IsFdFamANDAssem(dwAttrs))    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"famandassem ");
    if(IsFdFamORAssem(dwAttrs))     szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"famorassem ");
    if(IsFdPrivateScope(dwAttrs))   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"privatescope ");
    if(IsFdInitOnly(dwAttrs))       szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"initonly ");
    if(IsFdLiteral(dwAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"literal ");
    if(IsFdNotSerialized(dwAttrs))  szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"notserialized ");
    if(IsFdSpecialName(dwAttrs))    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"specialname ");
    if(IsFdRTSpecialName(dwAttrs))  szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"rtspecialname ");
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD((char*)-1));
    if (IsFdPinvokeImpl(dwAttrs))
    {
        DWORD   dwMappingFlags;
        const char  *szImportName;
        mdModuleRef mrImportDLL;

        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s(",KEYWORD("pinvokeimpl"));
        if(FAILED(g_pImport->GetPinvokeMap(FuncToken,&dwMappingFlags,
            &szImportName,&mrImportDLL)))  szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/* No map */"));
        else
            szptr = DumpPinvokeMap(dwMappingFlags,  (strcmp(szImportName,psz)? szImportName : NULL),
                mrImportDLL, szString,GUICookie);
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),") ");
    }
    szptr = DumpMarshaling(g_pImport,szString,SZSTRING_SIZE,FuncToken);

    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s",szStr);

    if (IsFdHasFieldRVA(dwAttrs))       // Do we have an RVA associated with this?
    {
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr), KEYWORD(" at "));

        ULONG fieldRVA;
        if (SUCCEEDED(g_pImport->GetFieldRVA(FuncToken, &fieldRVA)))
        {
            szptr = DumpDataPtr(&szString[strlen(szString)], fieldRVA, SizeOfField(FuncToken,g_pImport));
        }
        else
        {
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),RstrUTF(IDS_E_NORVA));
        }
    }

    // dump default value (if any):
    if(IsFdHasDefault(dwAttrs) && DumpBody)  DumpDefaultValue(FuncToken,szString,GUICookie);
    printLine(GUICookie, szString);

    if(DumpBody)
    {
        DumpCustomAttributes(FuncToken,GUICookie);
        DumpPermissions(FuncToken,GUICookie);
    }

    return TRUE;

}

BOOL DumpEvent(mdToken FuncToken, const char *pszClassName, DWORD dwClassAttrs, void *GUICookie, BOOL DumpBody)
{
    DWORD           dwAttrs;
    mdToken         tkEventType;
    LPCSTR          psz;
    HENUMInternal   hAssoc;
    ASSOCIATE_RECORD rAssoc[128];
    CQuickBytes     qbMemberSig;
    ULONG           nAssoc;
    char*           szptr;

    if (FAILED(g_pImport->GetEventProps(FuncToken,&psz,&dwAttrs,&tkEventType)))
    {
        char sz[2048];
        sprintf_s(sz, 2048, "%sERROR: Invalid Event %08X record", g_szAsmCodeIndent, FuncToken);
        printError(GUICookie, sz);
        return FALSE;
    }
    MAKE_NAME_IF_NONE(psz,FuncToken);
    if (g_Mode == MODE_DUMP_CLASS_METHOD || g_Mode == MODE_DUMP_CLASS_METHOD_SIG)
    {
        if (strcmp(psz, g_pszMethodToDump) != 0)  return FALSE;
    }

    if (FAILED(g_pImport->EnumAssociateInit(FuncToken,&hAssoc)))
    {
        char sz[2048];
        sprintf_s(sz, 2048, "%sERROR: MetaData error enumerating Associate for %08X", g_szAsmCodeIndent, FuncToken);
        printError(GUICookie, sz);
        return FALSE;
    }

    if ((nAssoc = hAssoc.m_ulCount))
    {
        memset(rAssoc,0,sizeof(rAssoc));
        if (FAILED(g_pImport->GetAllAssociates(&hAssoc,rAssoc,nAssoc)))
        {
            char sz[2048];
            sprintf_s(sz, 2048, "%sERROR: MetaData error enumerating all Associates", g_szAsmCodeIndent);
            printError(GUICookie, sz);
            return FALSE;
        }

        if (g_fLimitedVisibility)
        {
            unsigned i;
            for (i=0; i < nAssoc;i++)
            {
                if ((TypeFromToken(rAssoc[i].m_memberdef) == mdtMethodDef) && g_pImport->IsValidToken(rAssoc[i].m_memberdef))
                {
                    DWORD dwMethodAttrs;
                    if (FAILED(g_pImport->GetMethodDefProps(rAssoc[i].m_memberdef, &dwMethodAttrs)))
                    {
                        continue;
                    }
                    if(g_fHidePub && IsMdPublic(dwMethodAttrs)) continue;
                    if(g_fHidePriv && IsMdPrivate(dwMethodAttrs)) continue;
                    if(g_fHideFam && IsMdFamily(dwMethodAttrs)) continue;
                    if(g_fHideAsm && IsMdAssem(dwMethodAttrs)) continue;
                    if(g_fHideFOA && IsMdFamORAssem(dwMethodAttrs)) continue;
                    if(g_fHideFAA && IsMdFamANDAssem(dwMethodAttrs)) continue;
                    if(g_fHidePrivScope && IsMdPrivateScope(dwMethodAttrs)) continue;
                    break;
                }
            }
            if (i >= nAssoc) return FALSE;
        }
    }

    szptr = &szString[0];
    if (DumpBody)
    {
        szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ", g_szAsmCodeIndent,KEYWORD(".event"));
        if(g_fDumpTokens) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),FuncToken);
    }
    else
    {
        szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s : ",ProperName((char*)psz));
    }

    if(IsEvSpecialName(dwAttrs))    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("specialname "));
    if(IsEvRTSpecialName(dwAttrs))  szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("rtspecialname "));

    if(RidFromToken(tkEventType)&&g_pImport->IsValidToken(tkEventType))
    {
            switch(TypeFromToken(tkEventType))
            {
                    case mdtTypeRef:
                    case mdtTypeDef:
                    case mdtTypeSpec:
                        {
                            PrettyPrintToken(szString, tkEventType, g_pImport,GUICookie,0);
                            szptr = &szString[strlen(szString)];
                        }
                        break;
                    default:
                        break;
            }
    }

    if(!DumpBody)
    {
        printLine(GUICookie,szString);
        return TRUE;
    }


    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," %s", ProperName((char*)psz));
    printLine(GUICookie,szString);
    sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,SCOPE());
    printLine(GUICookie,szString);
    strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"  ");

    DumpCustomAttributes(FuncToken,GUICookie);
    DumpPermissions(FuncToken,GUICookie);

    if(nAssoc)
    {
        for(unsigned i=0; i < nAssoc;i++)
        {
            mdToken tk = rAssoc[i].m_memberdef;
            DWORD   sem = rAssoc[i].m_dwSemantics;

            szptr = &szString[0];
            if(IsMsAddOn(sem))          szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".addon"));
            else if(IsMsRemoveOn(sem))  szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".removeon"));
            else if(IsMsFire(sem))      szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".fire"));
            else if(IsMsOther(sem))     szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".other"));
            else szptr+=sprintf_s(szptr,SZSTRING_SIZE,ERRORMSG("UNKNOWN SEMANTICS: 0x%X "),sem);

            if(g_pImport->IsValidToken(tk))
                PrettyPrintToken(szString, tk, g_pImport,GUICookie,0);
            else szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),ERRORMSG("INVALID TOKEN 0x%8.8X"),tk);
            printLine(GUICookie,szString);
        }
    }
    if(g_szAsmCodeIndent[0]) g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
    szptr = &szString[0];
    szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,UNSCOPE());
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("// end of event %s::"),ProperName((char*)pszClassName));
    strcpy_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT(ProperName((char*)psz)));
    printLine(GUICookie,szString);
    return TRUE;

}

BOOL DumpProp(mdToken FuncToken, const char *pszClassName, DWORD dwClassAttrs, void *GUICookie, BOOL DumpBody)
{
    DWORD           dwAttrs;
    LPCSTR          psz;
    HENUMInternal   hAssoc;
    ASSOCIATE_RECORD rAssoc[128];
    CQuickBytes     qbMemberSig;
    PCCOR_SIGNATURE pComSig;
    ULONG           cComSig, nAssoc;
    unsigned        uStringLen = SZSTRING_SIZE;
    char*           szptr;

    if (FAILED(g_pImport->GetPropertyProps(FuncToken,&psz,&dwAttrs,&pComSig,&cComSig)))
    {
        char sz[2048];
        sprintf_s(sz, 2048, "%sERROR: Invalid Property %08X record", g_szAsmCodeIndent, FuncToken);
        printError(GUICookie, sz);
        return FALSE;
    }
    MAKE_NAME_IF_NONE(psz,FuncToken);
    if(cComSig == 0)
    {
        char sz[2048];
        sprintf_s(sz,2048,"%sERROR: property '%s' has no signature",g_szAsmCodeIndent,psz);
        printError(GUICookie,sz);
        return FALSE;
    }

    if (g_Mode == MODE_DUMP_CLASS_METHOD || g_Mode == MODE_DUMP_CLASS_METHOD_SIG)
    {
        if (strcmp(psz, g_pszMethodToDump) != 0)  return FALSE;
    }

    if (FAILED(g_pImport->EnumAssociateInit(FuncToken,&hAssoc)))
    {
        char sz[2048];
        sprintf_s(sz, 2048, "%sERROR: MetaData error enumerating Associate for %08X", g_szAsmCodeIndent, FuncToken);
        printError(GUICookie, sz);
        return FALSE;
    }
    if ((nAssoc = hAssoc.m_ulCount) != 0)
    {
        memset(rAssoc,0,sizeof(rAssoc));
        if (FAILED(g_pImport->GetAllAssociates(&hAssoc,rAssoc,nAssoc)))
        {
            char sz[2048];
            sprintf_s(sz, 2048, "%sERROR: MetaData error enumerating all Associates", g_szAsmCodeIndent);
            printError(GUICookie, sz);
            return FALSE;
        }

        if (g_fLimitedVisibility)
        {
            unsigned i;
            for (i=0; i < nAssoc;i++)
            {
                if ((TypeFromToken(rAssoc[i].m_memberdef) == mdtMethodDef) && g_pImport->IsValidToken(rAssoc[i].m_memberdef))
                {
                    DWORD dwMethodAttrs;
                    if (FAILED(g_pImport->GetMethodDefProps(rAssoc[i].m_memberdef, &dwMethodAttrs)))
                    {
                        continue;
                    }
                    if(g_fHidePub && IsMdPublic(dwMethodAttrs)) continue;
                    if(g_fHidePriv && IsMdPrivate(dwMethodAttrs)) continue;
                    if(g_fHideFam && IsMdFamily(dwMethodAttrs)) continue;
                    if(g_fHideAsm && IsMdAssem(dwMethodAttrs)) continue;
                    if(g_fHideFOA && IsMdFamORAssem(dwMethodAttrs)) continue;
                    if(g_fHideFAA && IsMdFamANDAssem(dwMethodAttrs)) continue;
                    if(g_fHidePrivScope && IsMdPrivateScope(dwMethodAttrs)) continue;
                    break;
                }
            }
            if( i >= nAssoc) return FALSE;
        }
    }

    szptr = &szString[0];
    if (DumpBody)
    {
        szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ", g_szAsmCodeIndent,KEYWORD(".property"));
        if(g_fDumpTokens) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),FuncToken);
    }
    else
    {
        szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s : ",ProperName((char*)psz));
    }

    if(IsPrSpecialName(dwAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("specialname "));
    if(IsPrRTSpecialName(dwAttrs))      szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("rtspecialname "));

    {
        char pchDefault[] = "";
        char *pch = pchDefault;
        if(DumpBody)
        {
            pch = szptr+1;
            strcpy_s(pch,SZSTRING_REMAINING_SIZE(pch),ProperName((char*)psz));
        }
        qbMemberSig.Shrink(0);
        PrettyPrintMethodSig(szString, &uStringLen, &qbMemberSig, pComSig, cComSig,
                              pch, NULL, GUICookie);
        if(IsPrHasDefault(dwAttrs) && DumpBody) DumpDefaultValue(FuncToken,szString,GUICookie);
    }
    printLine(GUICookie,szString);

    if(DumpBody)
    {
        sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,SCOPE());
        printLine(GUICookie,szString);
        strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"  ");

        DumpCustomAttributes(FuncToken,GUICookie);
        DumpPermissions(FuncToken,GUICookie);

        if(nAssoc)
        {
            for(unsigned i=0; i < nAssoc;i++)
            {
                mdToken tk = rAssoc[i].m_memberdef;
                DWORD   sem = rAssoc[i].m_dwSemantics;

                szptr = &szString[0];
                if(IsMsSetter(sem))         szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".set"));
                else if(IsMsGetter(sem))    szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".get"));
                else if(IsMsOther(sem))     szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".other"));
                else szptr+=sprintf_s(szptr,SZSTRING_SIZE,ERRORMSG("UNKNOWN SEMANTICS: 0x%X "),sem);

                if(g_pImport->IsValidToken(tk))
                    PrettyPrintToken(szString, tk, g_pImport,GUICookie,0);
                else szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),ERRORMSG("INVALID TOKEN 0x%8.8X"),tk);
                printLine(GUICookie,szString);
            }
        }
        if(g_szAsmCodeIndent[0]) g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
        szptr = &szString[0];
        szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,UNSCOPE());
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("// end of property %s::"),ProperName((char*)pszClassName));
        strcpy_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT(ProperName((char*)psz)));
        printLine(GUICookie,szString);
    } // end if(DumpBody)
    return TRUE;

}

BOOL DumpMembers(mdTypeDef cl, const char *pszClassNamespace, const char *pszClassName,
                 DWORD dwClassAttrs, DWORD dwEntryPointToken, void* GUICookie)
{
    HRESULT         hr;
    mdToken         *pMemberList = NULL;
    DWORD           NumMembers, NumFields,NumMethods,NumEvents,NumProps;
    DWORD           i;
    HENUMInternal   hEnumMethod;
    HENUMInternal   hEnumField;
    HENUMInternal   hEnumEvent;
    HENUMInternal   hEnumProp;
    CQuickBytes     qbMemberSig;
    BOOL            ret;

    // Get the total count of methods + fields
    hr = g_pImport->EnumInit(mdtMethodDef, cl, &hEnumMethod);
    if (FAILED(hr))
    {
FailedToEnum:
        printLine(GUICookie,RstrUTF(IDS_E_MEMBRENUM));
        ret = FALSE;
        goto CloseHandlesAndReturn;
    }
    NumMembers = NumMethods = g_pImport->EnumGetCount(&hEnumMethod);


    if (FAILED(g_pImport->EnumInit(mdtFieldDef, cl, &hEnumField)))   goto FailedToEnum;
    NumFields = g_pImport->EnumGetCount(&hEnumField);
    NumMembers += NumFields;

    if (FAILED(g_pImport->EnumInit(mdtEvent, cl, &hEnumEvent))) goto FailedToEnum;
    NumEvents = g_pImport->EnumGetCount(&hEnumEvent);
    NumMembers += NumEvents;

    if (FAILED(g_pImport->EnumInit(mdtProperty, cl, &hEnumProp))) goto FailedToEnum;
    NumProps = g_pImport->EnumGetCount(&hEnumProp);
    NumMembers += NumProps;
    ret = TRUE;

    if(NumMembers)
    {
        pMemberList = new (nothrow) mdToken[NumMembers];
        if(pMemberList == NULL) ret = FALSE;
    }
    if ((NumMembers == 0)||(pMemberList == NULL)) goto CloseHandlesAndReturn;

    for (i = 0; g_pImport->EnumNext(&hEnumField, &pMemberList[i]); i++);
    for (; g_pImport->EnumNext(&hEnumMethod, &pMemberList[i]); i++);
    for (; g_pImport->EnumNext(&hEnumEvent, &pMemberList[i]); i++);
    for (; g_pImport->EnumNext(&hEnumProp, &pMemberList[i]); i++);
    _ASSERTE(i == NumMembers);

    for (i = 0; i < NumMembers; i++)
    {
        mdToken tk = pMemberList[i];
        if(g_pImport->IsValidToken(tk))
        {
            switch (TypeFromToken(tk))
            {
                case mdtFieldDef:
                    ret = DumpField(pMemberList[i], pszClassName, GUICookie,TRUE);
                    break;

                case mdtMethodDef:
                    ret = DumpMethod(pMemberList[i], pszClassName, dwEntryPointToken,GUICookie,TRUE);
                    break;

                case mdtEvent:
                    ret = DumpEvent(pMemberList[i], pszClassName, dwClassAttrs,GUICookie,TRUE);
                    break;

                case mdtProperty:
                    ret = DumpProp(pMemberList[i], pszClassName, dwClassAttrs,GUICookie,TRUE);
                    break;

                default:
                    {
                        char szStr[4096];
                        sprintf_s(szStr,4096,RstrUTF(IDS_E_ODDMEMBER),pMemberList[i],pszClassName);
                        printLine(GUICookie,szStr);
                    }
                    ret = FALSE;
                    break;
            } // end switch
        }
        else
        {
            char szStr[256];
            sprintf_s(szStr,256,ERRORMSG("INVALID MEMBER TOKEN: 0x%8.8X"),tk);
            printLine(GUICookie,szStr);
            ret= FALSE;
        }
        if(ret && (g_Mode == MODE_DUMP_CLASS_METHOD_SIG)) break;
    } // end for
    ret = TRUE;

CloseHandlesAndReturn:
    g_pImport->EnumClose(&hEnumMethod);
    g_pImport->EnumClose(&hEnumField);
    g_pImport->EnumClose(&hEnumEvent);
    g_pImport->EnumClose(&hEnumProp);
    if(pMemberList) delete[] pMemberList;
    return ret;
}
BOOL GetClassLayout(mdTypeDef cl, ULONG* pulPackSize, ULONG* pulClassSize)
{ // Dump class layout
    HENUMInternal   hEnumField;
    BOOL ret = FALSE;

    if(g_rFieldOffset)
        VDELETE(g_rFieldOffset);
    g_cFieldOffsets = 0;
    g_cFieldsMax = 0;

    if(RidFromToken(cl)==0) return TRUE;

    if (SUCCEEDED(g_pImport->EnumInit(mdtFieldDef, cl, &hEnumField)))
    {
        g_cFieldsMax = g_pImport->EnumGetCount(&hEnumField);
        g_pImport->EnumClose(&hEnumField);
    }

    if(SUCCEEDED(g_pImport->GetClassPackSize(cl,pulPackSize))) ret = TRUE;
    else *pulPackSize = 0xFFFFFFFF;
    if(SUCCEEDED(g_pImport->GetClassTotalSize(cl,pulClassSize))) ret = TRUE;
    else *pulClassSize = 0xFFFFFFFF;

    if(g_cFieldsMax)
    {
        MD_CLASS_LAYOUT Layout;
        if(SUCCEEDED(g_pImport->GetClassLayoutInit(cl,&Layout)))
        {
            g_rFieldOffset = new COR_FIELD_OFFSET[g_cFieldsMax+1];
            if(g_rFieldOffset)
            {
                COR_FIELD_OFFSET* pFO = g_rFieldOffset;
                for(g_cFieldOffsets=0;
                    SUCCEEDED(g_pImport->GetClassLayoutNext(&Layout,&(pFO->ridOfField),(ULONG*)&(pFO->ulOffset)))
                        &&RidFromToken(pFO->ridOfField);
                    g_cFieldOffsets++, pFO++) ret = TRUE;
            }
        }
    }
    return ret;
}

BOOL IsANestedInB(mdTypeDef A, mdTypeDef B)
{
    DWORD i;
    for(i = 0; i < g_NumClasses; i++)
    {
        if(g_cl_list[i] == A)
        {
            A = g_cl_enclosing[i];
            if(A == B) return TRUE;
            if(A == mdTypeDefNil) return FALSE;
            return IsANestedInB(A,B);
        }
    }
    return FALSE;
}
mdTypeDef TopEncloser(mdTypeDef A)
{
    DWORD i;
    for(i = 0; i < g_NumClasses; i++)
    {
        if(g_cl_list[i] == A)
        {
            if(g_cl_enclosing[i] == mdTypeDefNil) return A;
            return TopEncloser(g_cl_enclosing[i]);
        }
    }
    return A;
}

BOOL DumpClass(mdTypeDef cl, DWORD dwEntryPointToken, void* GUICookie, ULONG WhatToDump)
// WhatToDump: 0-title,flags,extends,implements;
//            +1-pack,size and custom attrs;
//            +2-nested classes
//            +4-members
{
    char            *pszClassName; // name associated with this CL
    char            *pszNamespace;
    const char      *pc1,*pc2;
    DWORD           dwClassAttrs;
    mdTypeRef       crExtends;
    HRESULT         hr;
    mdInterfaceImpl ii;
    DWORD           NumInterfaces;
    DWORD           i;
    HENUMInternal   hEnumII;            // enumerator for interface impl
    //char            *szString;
    char*           szptr;

    mdToken         tkVarOwner = g_tkVarOwner;
    ULONG           WhatToDumpOrig = WhatToDump;

    if (FAILED(g_pImport->GetNameOfTypeDef(
        cl,
        &pc1,   //&pszClassName,
        &pc2))) //&pszNamespace
    {
        char sz[2048];
        sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), cl);
        printError(GUICookie, sz);
        g_tkVarOwner = tkVarOwner;
        return FALSE;
    }
    MAKE_NAME_IF_NONE(pc1,cl);

    if (g_Mode == MODE_DUMP_CLASS || g_Mode == MODE_DUMP_CLASS_METHOD || g_Mode == MODE_DUMP_CLASS_METHOD_SIG)
    {
        if(cl != g_tkClassToDump)
        {
            if(IsANestedInB(g_tkClassToDump,cl))
                WhatToDump = 2; // nested classes only
            else
                return TRUE;
        }
    }

    if (FAILED(g_pImport->GetTypeDefProps(
        cl,
        &dwClassAttrs,
        &crExtends)))
    {
        char sz[2048];
        sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), cl);
        printError(GUICookie, sz);
        g_tkVarOwner = tkVarOwner;
        return FALSE;
    }

    if(g_fLimitedVisibility)
    {
            if(g_fHidePub && (IsTdPublic(dwClassAttrs)||IsTdNestedPublic(dwClassAttrs))) return FALSE;
            if(g_fHidePriv && (IsTdNotPublic(dwClassAttrs)||IsTdNestedPrivate(dwClassAttrs))) return FALSE;
            if(g_fHideFam && IsTdNestedFamily(dwClassAttrs)) return FALSE;
            if(g_fHideAsm && IsTdNestedAssembly(dwClassAttrs)) return FALSE;
            if(g_fHideFOA && IsTdNestedFamORAssem(dwClassAttrs)) return FALSE;
            if(g_fHideFAA && IsTdNestedFamANDAssem(dwClassAttrs)) return FALSE;
    }

    g_tkVarOwner = cl;

    pszClassName = (char*)(pc1 ? pc1 : "");
    pszNamespace = (char*)(pc2 ? pc2 : "");


    szptr = &szString[0];
    szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".class"));
    if(g_fDumpTokens) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%8.8X*/ "),cl);
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD((char*)0));
    if (IsTdInterface(dwClassAttrs))                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"interface ");
    if (IsTdPublic(dwClassAttrs))                   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"public ");
    if (IsTdNotPublic(dwClassAttrs))                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"private ");
    if (IsTdAbstract(dwClassAttrs))                 szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"abstract ");
    if (IsTdAutoLayout(dwClassAttrs))               szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"auto ");
    if (IsTdSequentialLayout(dwClassAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"sequential ");
    if (IsTdExplicitLayout(dwClassAttrs))           szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"explicit ");
    if (IsTdAnsiClass(dwClassAttrs))                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"ansi ");
    if (IsTdUnicodeClass(dwClassAttrs))             szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"unicode ");
    if (IsTdAutoClass(dwClassAttrs))                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"autochar ");
    if (IsTdImport(dwClassAttrs))                   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"import ");
    if (IsTdWindowsRuntime(dwClassAttrs))           szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"windowsruntime ");
    if (IsTdSerializable(dwClassAttrs))             szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"serializable ");
    if (IsTdSealed(dwClassAttrs))                   szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"sealed ");
    if (IsTdNestedPublic(dwClassAttrs))             szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"nested public ");
    if (IsTdNestedPrivate(dwClassAttrs))            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"nested private ");
    if (IsTdNestedFamily(dwClassAttrs))             szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"nested family ");
    if (IsTdNestedAssembly(dwClassAttrs))           szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"nested assembly ");
    if (IsTdNestedFamANDAssem(dwClassAttrs))        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"nested famandassem ");
    if (IsTdNestedFamORAssem(dwClassAttrs))         szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"nested famorassem ");
    if (IsTdBeforeFieldInit(dwClassAttrs))          szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"beforefieldinit ");
    if (IsTdSpecialName(dwClassAttrs))              szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"specialname ");
    if (IsTdRTSpecialName(dwClassAttrs))            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"rtspecialname ");
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD((char*)-1));
    if(*pszNamespace != 0)
        szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s.",ProperName(pszNamespace));
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),WhatToDump > 2 ? ANCHORPT(ProperName(pszClassName),cl) : JUMPPT(ProperName(pszClassName),cl));

    szptr = DumpGenericPars(szString, cl, GUICookie,TRUE);
    if (szptr == NULL)
    {
        g_tkVarOwner = tkVarOwner;
        return FALSE;
    }

    printLine(GUICookie,szString);
    if (!IsNilToken(crExtends))
    {
        CQuickBytes out;
        szptr = szString;
        szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s       %s ",g_szAsmCodeIndent,KEYWORD("extends"));
        if(g_pImport->IsValidToken(crExtends))
            PrettyPrintToken(szString, crExtends, g_pImport,GUICookie,cl);
        else
            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),ERRORMSG("INVALID TOKEN: 0x%8.8X"),crExtends);
        printLine(GUICookie,szString);
    }

    hr = g_pImport->EnumInit(
        mdtInterfaceImpl,
        cl,
        &hEnumII);
    if (FAILED(hr))
    {
        printError(GUICookie,RstrUTF(IDS_E_ENUMINIT));
        g_tkVarOwner = tkVarOwner;
        return FALSE;
    }

    NumInterfaces = g_pImport->EnumGetCount(&hEnumII);

    if (NumInterfaces > 0)
    {
        CQuickBytes out;
        mdTypeRef   crInterface;
        for (i=0; g_pImport->EnumNext(&hEnumII, &ii); i++)
        {
            szptr = szString;
            if(i) szptr+=sprintf_s(szptr,SZSTRING_SIZE, "%s                  ",g_szAsmCodeIndent);
            else  szptr+=sprintf_s(szptr,SZSTRING_SIZE, "%s       %s ",g_szAsmCodeIndent,KEYWORD("implements"));
            if (FAILED(g_pImport->GetTypeOfInterfaceImpl(ii, &crInterface)))
            {
                char sz[2048];
                sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), ii);
                printError(GUICookie, sz);
                g_tkVarOwner = tkVarOwner;
                return FALSE;
            }
            if(g_pImport->IsValidToken(crInterface))
                PrettyPrintToken(szString, crInterface, g_pImport,GUICookie,cl);
            else
                szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),ERRORMSG("INVALID TOKEN: 0x%8.8X"),crInterface);
            if(i < NumInterfaces-1) strcat_s(szString, SZSTRING_SIZE,",");
            printLine(GUICookie,szString);
            out.Shrink(0);
        }
        // The assertion will fire if the enumerator is bad
        _ASSERTE(NumInterfaces == i);

        g_pImport->EnumClose(&hEnumII);
    }
    if(WhatToDump == 0) // 0 = title only
    {
        sprintf_s(szString,SZSTRING_SIZE,"%s%s %s",g_szAsmCodeIndent,SCOPE(),UNSCOPE());
        printLine(GUICookie,szString);
        g_tkVarOwner = tkVarOwner;
        return TRUE;
    }
    sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,SCOPE());
    printLine(GUICookie,szString);
    strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"  ");

    ULONG ulPackSize=0xFFFFFFFF,ulClassSize=0xFFFFFFFF;
    if(WhatToDump & 1)
    {
        if(GetClassLayout(cl,&ulPackSize,&ulClassSize))
        { // Dump class layout
            if(ulPackSize != 0xFFFFFFFF)
            {
                sprintf_s(szString,SZSTRING_SIZE,"%s%s %d",g_szAsmCodeIndent,KEYWORD(".pack"),ulPackSize);
                printLine(GUICookie,szString);
            }
            if(ulClassSize != 0xFFFFFFFF)
            {
                sprintf_s(szString,SZSTRING_SIZE,"%s%s %d",g_szAsmCodeIndent,KEYWORD(".size"),ulClassSize);
                printLine(GUICookie,szString);
            }
        }
        DumpCustomAttributes(cl,GUICookie);
        // Dev11 #10745
        // Dump InterfaceImpl custom attributes here
        if (NumInterfaces > 0 && g_fShowCA)
        {
            hr = g_pImport->EnumInit(
                mdtInterfaceImpl,
                cl,
                &hEnumII);
            if (FAILED(hr))
            {
                printError(GUICookie,RstrUTF(IDS_E_ENUMINIT));
                g_tkVarOwner = tkVarOwner;
                return FALSE;
            }

            ASSERT_AND_CHECK(NumInterfaces == g_pImport->EnumGetCount(&hEnumII));
            CQuickBytes out;
            mdTypeRef   crInterface;
            for (i = 0; g_pImport->EnumNext(&hEnumII, &ii); i++)
            {
                HENUMInternal    hEnum;
                mdCustomAttribute tkCA;
                bool fFirst = true;

                if (FAILED(g_pImport->EnumInit(mdtCustomAttribute, ii,&hEnum)))
                {
                    return FALSE;
                }
                while(g_pImport->EnumNext(&hEnum,&tkCA) && RidFromToken(tkCA))
                {
                    if (fFirst)
                    {
                        // Print .interfaceImpl type {type} before the custom attribute list
                        szptr = szString;
                        szptr += sprintf_s(szptr, SZSTRING_SIZE, "%s.%s ", g_szAsmCodeIndent, KEYWORD("interfaceimpl type"));
                        if (FAILED(g_pImport->GetTypeOfInterfaceImpl(ii, &crInterface)))
                        {
                            char sz[2048];
                            sprintf_s(sz, 2048, RstrUTF(IDS_E_INVALIDRECORD), ii);
                            printError(GUICookie, sz);
                            g_tkVarOwner = tkVarOwner;
                            return FALSE;
                        }
                        if(g_pImport->IsValidToken(crInterface))
                            PrettyPrintToken(szString, crInterface, g_pImport,GUICookie,cl);
                        else
                            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),ERRORMSG("INVALID TOKEN: 0x%8.8X"),crInterface);
                        printLine(GUICookie,szString);
                        out.Shrink(0);

                        szptr = szString;
                        fFirst = false;
                    }
                    DumpCustomAttribute(tkCA,GUICookie,false);
                }
                g_pImport->EnumClose( &hEnum);
            }
            // The assertion will fire if the enumerator is bad
            _ASSERTE(NumInterfaces == i);

            g_pImport->EnumClose(&hEnumII);
        }
        DumpGenericParsCA(cl,GUICookie);
        DumpPermissions(cl,GUICookie);
    }

    // Dump method impls declared in this class whose implementing methods belong somewhere else:
    if(WhatToDump & 1) // 1 - dump headers
    {
        for(i = 0; i < g_NumMI; i++)
        {
            if(((*g_pmi_list)[i].tkClass == cl)&&((*g_pmi_list)[i].tkBodyParent != cl))
            {
                BOOL bOverridingTypeSpec;
                PrettyPrintOverrideDecl(i,szString,GUICookie,cl,&bOverridingTypeSpec);
                strcat_s(szString, SZSTRING_SIZE,KEYWORD(" with "));

                if (bOverridingTypeSpec)
                {
                    // If PrettyPrintOverrideDecl printed the 'method' keyword, we need it here as well
                    // to satisfy the following grammar rule (simplified):
                    // _OVERRIDE METHOD_ ... DCOLON methodName ... WITH_ METHOD_ ... DCOLON methodName ...
                    strcat_s(szString, SZSTRING_SIZE,KEYWORD("method "));
                }

                PrettyPrintToken(szString, (*g_pmi_list)[i].tkBody, g_pImport,GUICookie,0);
                printLine(GUICookie,szString);
            }
        }
    }
    if(WhatToDump & 2) // nested classes
    {
        BOOL    fRegetClassLayout=FALSE;
        DWORD dwMode = g_Mode;

        if(g_Mode == MODE_DUMP_CLASS)
            g_Mode = MODE_DUMP_ALL;

        for(i = 0; i < g_NumClasses; i++)
        {
            if(g_cl_enclosing[i] == cl)
            {
                DumpClass(g_cl_list[i],dwEntryPointToken,GUICookie,WhatToDumpOrig);
                fRegetClassLayout = TRUE;
            }
        }
        if(fRegetClassLayout) GetClassLayout(cl,&ulPackSize,&ulClassSize);
        g_Mode = dwMode;
    }

    if(WhatToDump & 4)
    {
        DumpMembers(cl, pszNamespace, pszClassName, dwClassAttrs, dwEntryPointToken,GUICookie);
    }

    if(g_szAsmCodeIndent[0]) g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
    szptr = szString;
    szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s %s// end of class ",g_szAsmCodeIndent,UNSCOPE(),COMMENT((char*)0));
    if(*pszNamespace != 0) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s.",ProperName(pszNamespace));
    sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s%s", ProperName(pszClassName),COMMENT((char*)-1));
    printLine(GUICookie,szString);
    printLine(GUICookie,"");
    g_tkVarOwner = tkVarOwner;
    return TRUE;
}



void DumpGlobalMethods(DWORD dwEntryPointToken)
{
    HENUMInternal   hEnumMethod;
    mdToken         FuncToken;
    DWORD           i;
    CQuickBytes     qbMemberSig;

    if (FAILED(g_pImport->EnumGlobalFunctionsInit(&hEnumMethod)))
        return;

    for (i = 0; g_pImport->EnumNext(&hEnumMethod, &FuncToken); i++)
    {
        if (i == 0)
        {
            printLine(g_pFile,"");
            printLine(g_pFile,COMMENT("// ================== GLOBAL METHODS ========================="));
            printLine(g_pFile,"");
        }
        if(DumpMethod(FuncToken, NULL, dwEntryPointToken, g_pFile, TRUE)&&
            (g_Mode == MODE_DUMP_CLASS_METHOD || g_Mode == MODE_DUMP_CLASS_METHOD_SIG)) break;
    }
    g_pImport->EnumClose(&hEnumMethod);
    if(i)
    {
        printLine(g_pFile,"");
        printLine(g_pFile,COMMENT("// ============================================================="));
        printLine(g_pFile,"");
    }
}

void DumpGlobalFields()
{
    HENUMInternal   hEnum;
    mdToken         FieldToken;
    DWORD           i;
    CQuickBytes     qbMemberSig;

    if (FAILED(g_pImport->EnumGlobalFieldsInit(&hEnum)))
        return;

    for (i = 0; g_pImport->EnumNext(&hEnum, &FieldToken); i++)
    {
        if (i == 0)
        {
            printLine(g_pFile,"");
            printLine(g_pFile,COMMENT("// ================== GLOBAL FIELDS =========================="));
            printLine(g_pFile,"");
        }
        if(DumpField(FieldToken, NULL, g_pFile, TRUE)&&
            (g_Mode == MODE_DUMP_CLASS_METHOD || g_Mode == MODE_DUMP_CLASS_METHOD_SIG)) break;
    }
    g_pImport->EnumClose(&hEnum);
    if(i)
    {
        printLine(g_pFile,"");
        printLine(g_pFile,COMMENT("// ============================================================="));
        printLine(g_pFile,"");
    }
}

void DumpVTables(IMAGE_COR20_HEADER *CORHeader, void* GUICookie)
{
    IMAGE_COR_VTABLEFIXUP *pFixup,*pDummy;
    DWORD       iCount;
    DWORD       i;
    USHORT      iSlot;
    char* szStr = &szString[0];

    if (VAL32(CORHeader->VTableFixups.VirtualAddress) == 0) return;

    sprintf_s(szString,SZSTRING_SIZE,"// VTableFixup Directory:");
    printLine(GUICookie,szStr);

    // Pull back a pointer to it.
    iCount = VAL32(CORHeader->VTableFixups.Size) / sizeof(IMAGE_COR_VTABLEFIXUP);
    if ((g_pPELoader->getVAforRVA(VAL32(CORHeader->VTableFixups.VirtualAddress), (void **) &pFixup) == FALSE)
        ||(g_pPELoader->getVAforRVA(VAL32(CORHeader->VTableFixups.VirtualAddress)+VAL32(CORHeader->VTableFixups.Size)-1, (void **) &pDummy) == FALSE))
    {
        printLine(GUICookie,RstrUTF(IDS_E_VTFUTABLE));
        goto exit;
    }

    // Walk every v-table fixup entry and dump the slots.
    for (i=0;  i<iCount;  i++)
    {
        sprintf_s(szString,SZSTRING_SIZE,"//   IMAGE_COR_VTABLEFIXUP[%d]:", i);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//       RVA:               0x%08x", VAL32(pFixup->RVA));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//       Count:             0x%04x", VAL16(pFixup->Count));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//       Type:              0x%04x", VAL16(pFixup->Type));
        printLine(GUICookie,szStr);

        BYTE *pSlot;
        if (g_pPELoader->getVAforRVA(VAL32(pFixup->RVA), (void **) &pSlot) == FALSE)
        {
            printLine(GUICookie,RstrUTF(IDS_E_BOGUSRVA));
            goto NextEntry;
        }

        for (iSlot=0;  iSlot<pFixup->Count;  iSlot++)
        {
            mdMethodDef tkMethod = VAL32(*(DWORD *) pSlot);
            if (pFixup->Type & VAL16(COR_VTABLE_32BIT))
            {
                sprintf_s(szString,SZSTRING_SIZE,"//         [0x%04x]            (0x%08x)", iSlot, tkMethod);
                pSlot += sizeof(DWORD);
            }
            else
            {
                sprintf_s(szString,SZSTRING_SIZE,"//         [0x%04x]            (0x%16llx)", iSlot, VAL64(*(unsigned __int64 *) pSlot));
                pSlot += sizeof(unsigned __int64);
            }
            printLine(GUICookie,szStr);

            ValidateToken(tkMethod, mdtMethodDef);
        }

        // Pointer to next fixup entry.
NextEntry:
        ++pFixup;
    }

exit:
    printLine(GUICookie,"");
}


void DumpEATTable(IMAGE_COR20_HEADER *CORHeader, void* GUICookie)
{
    BYTE        *pFixup,*pDummy;
    DWORD       iCount;
    DWORD       BufferRVA;
    DWORD       i;
    char* szStr = &szString[0];

    sprintf_s(szString,SZSTRING_SIZE,"// Export Address Table Jumps:");
    printLine(GUICookie,szStr);

    if (VAL32(CORHeader->ExportAddressTableJumps.VirtualAddress) == 0)
    {
        printLine(GUICookie,RstrUTF(IDS_E_NODATA));
        return;
    }

    // Pull back a pointer to it.
    iCount = VAL32(CORHeader->ExportAddressTableJumps.Size) / IMAGE_COR_EATJ_THUNK_SIZE;
    if ((g_pPELoader->getVAforRVA(VAL32(CORHeader->ExportAddressTableJumps.VirtualAddress), (void **) &pFixup) == FALSE)
        ||(g_pPELoader->getVAforRVA(VAL32(CORHeader->ExportAddressTableJumps.VirtualAddress)+VAL32(CORHeader->ExportAddressTableJumps.Size)-1, (void **) &pDummy) == FALSE))
    {
        printLine(GUICookie,RstrUTF(IDS_E_EATJTABLE));
        goto exit;
    }

    // Quick sanity check on the linker.
    if (VAL32(CORHeader->ExportAddressTableJumps.Size) % IMAGE_COR_EATJ_THUNK_SIZE)
    {
        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_EATJSIZE),
                VAL32(CORHeader->ExportAddressTableJumps.Size), IMAGE_COR_EATJ_THUNK_SIZE);
        printLine(GUICookie,szStr);
    }

    // Walk every v-table fixup entry and dump the slots.
    BufferRVA = VAL32(CORHeader->ExportAddressTableJumps.VirtualAddress);
    for (i=0;  i<iCount;  i++)
    {
        ULONG ReservedFlag = VAL32(*(ULONG *) (pFixup + sizeof(ULONG)));
        sprintf_s(szString,SZSTRING_SIZE,"//   Fixup Jump Entry [%d], at RVA 0x%08x:", i, BufferRVA);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//       RVA of slot:       0x%08x", VAL32(*(ULONG *) pFixup));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//       Reserved flag:     0x%08x", ReservedFlag);
        printLine(GUICookie,szStr);
        if (ReservedFlag != 0)
        {
            printLine(GUICookie,RstrUTF(IDS_E_RESFLAGS));
        }

        pFixup += IMAGE_COR_EATJ_THUNK_SIZE;
        BufferRVA += IMAGE_COR_EATJ_THUNK_SIZE;
    }

exit:
    printLine(GUICookie,"");
}


void DumpCodeManager(IMAGE_COR20_HEADER *CORHeader, void* GUICookie)
{
    char* szStr = &szString[0];
    sprintf_s(szString,SZSTRING_SIZE,"// Code Manager Table:");
    printLine(GUICookie,szStr);
    if (!VAL32(CORHeader->CodeManagerTable.Size))
    {
        sprintf_s(szString,SZSTRING_SIZE,"//  default");
        printLine(GUICookie,szStr);
        return;
    }

    const GUID *pcm;
    if (g_pPELoader->getVAforRVA(VAL32(CORHeader->CodeManagerTable.VirtualAddress), (void **) &pcm) == FALSE)
    {
        printLine(GUICookie,RstrUTF(IDS_E_CODEMGRTBL));
        return;
    }

    sprintf_s(szString,SZSTRING_SIZE,"//   [index]         ID");
    printLine(GUICookie,szStr);
    ULONG iCount = VAL32(CORHeader->CodeManagerTable.Size) / sizeof(GUID);
    for (ULONG i=0;  i<iCount;  i++)
    {
        WCHAR        rcguid[128];
        GUID         Guid = *pcm;
        SwapGuid(&Guid);
        StringFromGUID2(Guid, rcguid, ARRAY_SIZE(rcguid));
        sprintf_s(szString,SZSTRING_SIZE,"//   [0x%08x]    %S", i, rcguid);
        printLine(GUICookie,szStr);
        pcm++;
    }
    printLine(GUICookie,"");
}

void DumpSectionHeaders(IMAGE_SECTION_HEADER* pSH, USHORT nSH, void* GUICookie)
{
    char* szStr = &szString[0];
    char name[16];
    printLine(GUICookie,"");
    strcpy_s(szString,SZSTRING_SIZE,"// Image sections:");
    printLine(GUICookie,szStr);
    for(USHORT iSH=0; iSH < nSH; iSH++,pSH++)
    {
        strncpy_s(name,16,(const char*)(pSH->Name),8);
        name[8]=0;
        sprintf_s(szString,SZSTRING_SIZE,"//              %s",name);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Virtual Size", pSH->Misc.VirtualSize);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Virtual Address", pSH->VirtualAddress);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Size of Raw Data", pSH->SizeOfRawData);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Pointer to Raw Data", pSH->PointerToRawData);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Pointer to Relocations", pSH->PointerToRelocations);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Pointer to Linenumbers", pSH->PointerToLinenumbers);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//                  0x%04x Number of Relocations", pSH->NumberOfRelocations);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//                  0x%04x Number of Linenumbers", pSH->NumberOfLinenumbers);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Characteristics", pSH->Characteristics);
        printLine(GUICookie,szStr);
        if((pSH->Characteristics & IMAGE_SCN_SCALE_INDEX))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         SCALE_INDEX");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_CNT_CODE))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         CNT_CODE");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_CNT_INITIALIZED_DATA))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         CNT_INITIALIZED_DATA");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_CNT_UNINITIALIZED_DATA))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         CNT_UNINITIALIZED_DATA");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_NO_DEFER_SPEC_EXC))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         NO_DEFER_SPEC_EXC");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_LNK_NRELOC_OVFL))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         LNK_NRELOC_OVFL");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_MEM_DISCARDABLE))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         MEM_DISCARDABLE");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_MEM_NOT_CACHED))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         MEM_NOT_CACHED");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_MEM_NOT_PAGED))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         MEM_NOT_PAGED");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_MEM_SHARED))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         MEM_SHARED");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_MEM_EXECUTE))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         MEM_EXECUTE");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_MEM_READ))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         MEM_READ");
            printLine(GUICookie,szStr);
        }
        if((pSH->Characteristics & IMAGE_SCN_MEM_WRITE))
        {
            strcpy_s(szString,SZSTRING_SIZE,"//                         MEM_WRITE");
            printLine(GUICookie,szStr);
        }
        printLine(GUICookie,"");
    }
}

void DumpBaseReloc(const char *szName, IMAGE_DATA_DIRECTORY *pDir, void* GUICookie)
{
    char* szStr = &szString[0];
    sprintf_s(szString,SZSTRING_SIZE,"// %s", szName);
    printLine(GUICookie,szStr);
    if (!VAL32(pDir->Size))
    {
        printLine(GUICookie,RstrUTF(IDS_E_NODATA));
        return;
    }
    char *pBegin, *pEnd;
    DWORD *pdw, i, Nentries;
    WORD  *pw;
    if (g_pPELoader->getVAforRVA(VAL32(pDir->VirtualAddress), (void **) &pBegin) == FALSE)
    {
        printLine(GUICookie,RstrUTF(IDS_E_IMPORTDATA));
        return;
    }
    pEnd = pBegin + VAL32(pDir->Size);
    for(pdw = (DWORD*)pBegin; pdw < (DWORD*)pEnd; )
    {
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Page RVA", *pdw);
        printLine(GUICookie,szStr);
        pdw++;
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Block Size", *pdw);
        printLine(GUICookie,szStr);
        Nentries = (*pdw - 2*sizeof(DWORD)) / sizeof(WORD);
        pdw++;
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Number of Entries", Nentries);
        printLine(GUICookie,szStr);

        for(i = 1, pw = (WORD*)pdw; i <= Nentries; i++, pw++)
        {
            sprintf_s(szString,SZSTRING_SIZE,"//              Entry %d: Type 0x%x Offset 0x%08x", i, ((*pw)>>12), ((*pw)&0x0FFF));
            printLine(GUICookie,szStr);
        }
        if((Nentries & 1)) pw++; // to make pdw DWORD-aligned
        pdw = (DWORD*)pw;
        printLine(GUICookie,"");
    }
}
void DumpIAT(const char *szName, IMAGE_DATA_DIRECTORY *pDir, void* GUICookie)
{
    char* szStr = &szString[0];

    sprintf_s(szString,SZSTRING_SIZE,"// %s", szName);
    printLine(GUICookie,szStr);
    if (!VAL32(pDir->Size))
    {
        printLine(GUICookie,RstrUTF(IDS_E_NODATA));
        return;
    }

    const char *szDLLName;
    const IMAGE_IMPORT_DESCRIPTOR *pImportDesc;

    if (g_pPELoader->getVAforRVA(VAL32(pDir->VirtualAddress), (void **) &pImportDesc) == FALSE)
    {
        printLine(GUICookie,RstrUTF(IDS_E_IMPORTDATA));
        return;
    }

    const DWORD *pImportTableID;
    while (VAL32(pImportDesc->FirstThunk))
    {
        if (g_pPELoader->getVAforRVA(VAL32(pImportDesc->Name), (void **) &szDLLName) == FALSE ||
            g_pPELoader->getVAforRVA(VAL32(pImportDesc->FirstThunk), (void **) &pImportTableID) == FALSE)
        {
            printLine(GUICookie,RstrUTF(IDS_E_IMPORTDATA));
            return;
        }

        sprintf_s(szString,SZSTRING_SIZE,"//     DLL : %s", szDLLName);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Import Address Table", VAL32(pImportDesc->FirstThunk));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Import Name Table", VAL32(pImportDesc->Name));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              %-8d   Time Date Stamp", VAL32(pImportDesc->TimeDateStamp));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              %-8d   Index of First Forwarder Reference", VAL32(pImportDesc->ForwarderChain));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//");
        printLine(GUICookie,szStr);

        for ( ; VAL32(*pImportTableID);  pImportTableID++)
        {
            if (VAL32(*pImportTableID) & 0x80000000)
                sprintf_s(szString,SZSTRING_SIZE,"//              by Ordinal %d", VAL32(*pImportTableID) & 0x7fffffff);
            else
            {
                const IMAGE_IMPORT_BY_NAME *pName;
                if(g_pPELoader->getVAforRVA(VAL32(*pImportTableID) & 0x7fffffff, (void **) &pName))
                    sprintf_s(szString,SZSTRING_SIZE,"//              0x%04x  %s", VAL16(pName->Hint), pName->Name);
                else
                    sprintf_s(szString,SZSTRING_SIZE,"//          0x%08x  bad RVA of IMAGE_IMPORT_BY_NAME", VAL32(*pImportTableID));
            }
            printLine(GUICookie,szStr);
       }
       printLine(GUICookie,"");

        // Next import descriptor.
        pImportDesc++;
    }
}

struct MDStreamHeader
{
    DWORD   Reserved;
    BYTE    Major;
    BYTE    Minor;
    BYTE    Heaps;
    BYTE    Rid;
    ULONGLONG   MaskValid;
    ULONGLONG   Sorted;
};

void DumpMetadataHeader(const char *szName, IMAGE_DATA_DIRECTORY *pDir, void* GUICookie)
{
    char* szStr = &szString[0];

    printLine(GUICookie,"");
    sprintf_s(szString,SZSTRING_SIZE,"// %s", szName);
    printLine(GUICookie,szStr);
    if (!VAL32(pDir->Size))
    {
        printLine(GUICookie,RstrUTF(IDS_E_NODATA));
        return;
    }

    const STORAGESIGNATURE *pSSig;
    char verstr[1024];

    if (g_pPELoader->getVAforRVA(VAL32(pDir->VirtualAddress), (void **) &pSSig) == FALSE)
    {
        printLine(GUICookie,RstrUTF(IDS_E_IMPORTDATA));
        return;
    }
    strcpy_s(szString,SZSTRING_SIZE,"//    Storage Signature:");
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Signature", VAL32(pSSig->lSignature));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"//                  0x%04x Major Version", VAL16(pSSig->iMajorVer));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"//                  0x%04x Minor Version", VAL16(pSSig->iMinorVer));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Extra Data Offset", VAL32(pSSig->iExtraData));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Version String Length", VAL32(pSSig->iVersionString));
    printLine(GUICookie,szStr);
    memset(verstr,0,1024);
    strncpy_s(verstr,1024,(const char*)(pSSig->pVersion),VAL32(pSSig->iVersionString));
    sprintf_s(szString,SZSTRING_SIZE,"//              '%s' Version String", verstr);
    printLine(GUICookie,szStr);

    size_t pb = (size_t)pSSig;
    pb += (3*sizeof(DWORD)+2*sizeof(WORD)+VAL32(pSSig->iVersionString)+3)&~3;
    PSTORAGEHEADER pSHdr = (PSTORAGEHEADER)pb;
    strcpy_s(szString,SZSTRING_SIZE,"//    Storage Header:");
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"//                    0x%02x Flags", pSHdr->fFlags);
    printLine(GUICookie,szStr);
    short nStr = VAL16(pSHdr->iStreams);
    sprintf_s(szString,SZSTRING_SIZE,"//                  0x%04x Number of Streams", nStr);
    if(nStr > 5)
    {
        strcat_s(szString, SZSTRING_SIZE, " -- BOGUS!");
        nStr = 5;
    }
    printLine(GUICookie,szStr);

    PSTORAGESTREAM pStr = (PSTORAGESTREAM)(pSHdr+1);
    BYTE* pbMDstream = NULL;
    size_t cbMDstream = 0;
    for(short iStr = 1; iStr <= nStr; iStr++)
    {
        sprintf_s(szString,SZSTRING_SIZE,"//    Stream %d:",iStr);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Offset", VAL32(pStr->iOffset));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Size", VAL32(pStr->iSize));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//              '%s' Name", pStr->rcName);
        printLine(GUICookie,szStr);
        if((strcmp(pStr->rcName,"#-")==0)||(strcmp(pStr->rcName,"#~")==0))
        {
            pbMDstream = (BYTE*)pSSig + VAL32(pStr->iOffset);
            cbMDstream = VAL32(pStr->iSize);
        }

        pb = (size_t)pStr;
        pb += (2*sizeof(DWORD)+strlen(pStr->rcName)+1+3)&~3;
        pStr = (PSTORAGESTREAM)pb;
    }
    if((pbMDstream)&&(cbMDstream >= sizeof(MDStreamHeader)))
    {
        printLine(GUICookie,"");
        strcpy_s(szString,SZSTRING_SIZE,"//    Metadata Stream Header:");
        printLine(GUICookie,szStr);

        MDStreamHeader* pMDSH = (MDStreamHeader*)pbMDstream;
        sprintf_s(szString,SZSTRING_SIZE,"//              0x%08x Reserved", VAL32(pMDSH->Reserved));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//                    0x%02x Major", pMDSH->Major);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//                    0x%02x Minor", pMDSH->Minor);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//                    0x%02x Heaps", pMDSH->Heaps);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//                    0x%02x Rid", pMDSH->Rid);
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//      0x%016I64x MaskValid", (ULONGLONG)GET_UNALIGNED_VAL64(&(pMDSH->MaskValid)));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"//      0x%016I64x Sorted", (ULONGLONG)GET_UNALIGNED_VAL64(&(pMDSH->Sorted)));
        printLine(GUICookie,szStr);
    }
}
void DumpEntryPoint(DWORD dwAddrOfEntryPoint,DWORD dwEntryPointSize,void* GUICookie)
{
    BYTE* pB;
    char* szStr = &szString[0];
    char* szptr = szStr+2;
    DWORD i;

    printLine(GUICookie,"");
    strcpy_s(szString,SZSTRING_SIZE,"// Entry point code:");
    printLine(GUICookie,szStr);
    if (g_pPELoader->getVAforRVA(dwAddrOfEntryPoint, (void **) &pB) == FALSE)
    {
        printLine(GUICookie,"Bad RVA of entry point");
        return;
    }
    if(dwEntryPointSize == 48) pB -= 32;
    // on IA64, AddressOfEntryPoint points at PLabelDescriptor, not at the stub itself
    for(i=0; i<dwEntryPointSize; i++)
    {
        szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%2.2X ",pB[i]);
    }
    printLine(GUICookie,szStr);
}

#define DUMP_DIRECTORY(szName, Directory) \
    sprintf_s(szString,SZSTRING_SIZE,"// 0x%08x [0x%08x] address [size] of " szName,  \
            VAL32(Directory.VirtualAddress), VAL32(Directory.Size)); \
    printLine(GUICookie,szStr)

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
void DumpHeader(IMAGE_COR20_HEADER *CORHeader, void* GUICookie)
{
    char* szStr = &szString[0];

    DWORD dwAddrOfEntryPoint=0, dwEntryPointSize=0;

    PIMAGE_DOS_HEADER pDOSHeader = g_pPELoader->dosHeader();

    strcpy_s(szString,SZSTRING_SIZE,"// ----- DOS Header:");
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Magic:                      0x%04x", VAL16(pDOSHeader->e_magic));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Bytes on last page:         0x%04x", VAL16(pDOSHeader->e_cblp));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Pages in file:              0x%04x", VAL16(pDOSHeader->e_cp));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Relocations:                0x%04x", VAL16(pDOSHeader->e_crlc));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Size of header (paragraphs):0x%04x", VAL16(pDOSHeader->e_cparhdr));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Min extra paragraphs:       0x%04x", VAL16(pDOSHeader->e_minalloc));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Max extra paragraphs:       0x%04x", VAL16(pDOSHeader->e_maxalloc));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Initial (relative) SS:      0x%04x", VAL16(pDOSHeader->e_ss));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Initial SP:                 0x%04x", VAL16(pDOSHeader->e_sp));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Checksum:                   0x%04x", VAL16(pDOSHeader->e_csum));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Initial IP:                 0x%04x", VAL16(pDOSHeader->e_ip));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Initial (relative) CS:      0x%04x", VAL16(pDOSHeader->e_ip));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// File addr. of reloc table:  0x%04x", VAL16(pDOSHeader->e_lfarlc));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Overlay number:             0x%04x", VAL16(pDOSHeader->e_ovno));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// OEM identifier:             0x%04x", VAL16(pDOSHeader->e_oemid));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// OEM info:                   0x%04x", VAL16(pDOSHeader->e_oeminfo));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// File addr. of COFF header:  0x%04x", VAL16(pDOSHeader->e_lfanew));
    printLine(GUICookie,szStr);

    strcpy_s(szString,SZSTRING_SIZE,"// ----- COFF/PE Headers:");
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Signature:                  0x%08x", VAL32(g_pPELoader->Signature()));
    printLine(GUICookie,szStr);

    strcpy_s(szString,SZSTRING_SIZE,"// ----- COFF Header:");
    printLine(GUICookie,szStr);

    PIMAGE_FILE_HEADER pCOFF = g_pPELoader->coffHeader();
    sprintf_s(szString,SZSTRING_SIZE,"// Machine:                    0x%04x", VAL16(pCOFF->Machine));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Number of sections:         0x%04x", VAL16(pCOFF->NumberOfSections));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Time-date stamp:            0x%08x", VAL32(pCOFF->TimeDateStamp));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Ptr to symbol table:        0x%08x", VAL32(pCOFF->PointerToSymbolTable));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Number of symbols:          0x%08x", VAL32(pCOFF->NumberOfSymbols));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Size of optional header:    0x%04x", VAL16(pCOFF->SizeOfOptionalHeader));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Characteristics:            0x%04x", VAL16(pCOFF->Characteristics));
    printLine(GUICookie,szStr);


    if (g_pPELoader->IsPE32())
    {
        IMAGE_NT_HEADERS32 *pNTHeader = g_pPELoader->ntHeaders32();
        IMAGE_OPTIONAL_HEADER32 *pOptHeader = &pNTHeader->OptionalHeader;

        strcpy_s(szString,SZSTRING_SIZE,"// ----- PE Optional Header (32 bit):");
        printLine(GUICookie,szStr);

        sprintf_s(szString,SZSTRING_SIZE,"// Magic:                          0x%04x", VAL16(pOptHeader->Magic));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Major linker version:           0x%02x", VAL16(pOptHeader->MajorLinkerVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Minor linker version:           0x%02x", VAL16(pOptHeader->MinorLinkerVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of code:                   0x%08x", VAL32(pOptHeader->SizeOfCode));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of init.data:              0x%08x", VAL32(pOptHeader->SizeOfInitializedData));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of uninit.data:            0x%08x", VAL32(pOptHeader->SizeOfUninitializedData));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Addr. of entry point:           0x%08x", VAL32(pOptHeader->AddressOfEntryPoint));
        printLine(GUICookie,szStr);
        dwAddrOfEntryPoint = VAL32(pOptHeader->AddressOfEntryPoint);
        dwEntryPointSize = 6;
        sprintf_s(szString,SZSTRING_SIZE,"// Base of code:                   0x%08x", VAL32(pOptHeader->BaseOfCode));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Base of data:                   0x%08x", VAL32(pOptHeader->BaseOfData));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Image base:                     0x%08x", VAL32(pOptHeader->ImageBase));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Section alignment:              0x%08x", VAL32(pOptHeader->SectionAlignment));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// File alignment:                 0x%08x", VAL32(pOptHeader->FileAlignment));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Major OS version:               0x%04x", VAL16(pOptHeader->MajorOperatingSystemVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Minor OS version:               0x%04x", VAL16(pOptHeader->MinorOperatingSystemVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Major image version:            0x%04x", VAL16(pOptHeader->MajorImageVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Minor image version:            0x%04x", VAL16(pOptHeader->MinorImageVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Major subsystem version:        0x%04x", VAL16(pOptHeader->MajorSubsystemVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Minor subsystem version:        0x%04x", VAL16(pOptHeader->MinorSubsystemVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of image:                  0x%08x", VAL32(pOptHeader->SizeOfImage));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of headers:                0x%08x", VAL32(pOptHeader->SizeOfHeaders));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Checksum:                       0x%08x", VAL32(pOptHeader->CheckSum));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Subsystem:                      0x%04x", VAL16(pOptHeader->Subsystem));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// DLL characteristics:            0x%04x", VAL16(pOptHeader->DllCharacteristics));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of stack reserve:          0x%08x", VAL32(pOptHeader->SizeOfStackReserve));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of stack commit:           0x%08x", VAL32(pOptHeader->SizeOfStackCommit));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of heap reserve:           0x%08x", VAL32(pOptHeader->SizeOfHeapReserve));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of heap commit:            0x%08x", VAL32(pOptHeader->SizeOfHeapCommit));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Loader flags:                   0x%08x", VAL32(pOptHeader->LoaderFlags));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Directories:                    0x%08x", VAL32(pOptHeader->NumberOfRvaAndSizes));
        printLine(GUICookie,szStr);
        DUMP_DIRECTORY("Export Directory:          ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT]);
        DUMP_DIRECTORY("Import Directory:          ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT]);
        DUMP_DIRECTORY("Resource Directory:        ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE]);
        DUMP_DIRECTORY("Exception Directory:       ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION]);
        DUMP_DIRECTORY("Security Directory:        ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY]);
        DUMP_DIRECTORY("Base Relocation Table:     ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC]);
        DUMP_DIRECTORY("Debug Directory:           ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG]);
        DUMP_DIRECTORY("Architecture Specific:     ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_ARCHITECTURE]);
        DUMP_DIRECTORY("Global Pointer:            ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_GLOBALPTR]);
        DUMP_DIRECTORY("TLS Directory:             ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS]);
        DUMP_DIRECTORY("Load Config Directory:     ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG]);
        DUMP_DIRECTORY("Bound Import Directory:    ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT]);
        DUMP_DIRECTORY("Import Address Table:      ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_IAT]);
        DUMP_DIRECTORY("Delay Load IAT:            ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT]);
        DUMP_DIRECTORY("CLR Header:                ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR]);
        printLine(GUICookie,"");

        DumpSectionHeaders((IMAGE_SECTION_HEADER*)(pOptHeader+1),pNTHeader->FileHeader.NumberOfSections,GUICookie);
        DumpBaseReloc("Base Relocation Table",&pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC],GUICookie);
        DumpIAT("Import Address Table", &pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT],GUICookie);
        DumpIAT("Delay Load Import Address Table", &pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT],GUICookie);
    }
    else
    {
        IMAGE_NT_HEADERS64 *pNTHeader = g_pPELoader->ntHeaders64();
        IMAGE_OPTIONAL_HEADER64 *pOptHeader = &pNTHeader->OptionalHeader;

        strcpy_s(szString,SZSTRING_SIZE,"// ----- PE Optional Header (64 bit):");
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Magic:                          0x%04x", VAL16(pOptHeader->Magic));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Major linker version:           0x%02x", VAL16(pOptHeader->MajorLinkerVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Minor linker version:           0x%02x", VAL16(pOptHeader->MinorLinkerVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of code:                   0x%08x", VAL32(pOptHeader->SizeOfCode));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of init.data:              0x%08x", VAL32(pOptHeader->SizeOfInitializedData));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of uninit.data:            0x%08x", VAL32(pOptHeader->SizeOfUninitializedData));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Addr. of entry point:           0x%08x", VAL32(pOptHeader->AddressOfEntryPoint));
        printLine(GUICookie,szStr);
        dwAddrOfEntryPoint = VAL32(pOptHeader->AddressOfEntryPoint);
        dwEntryPointSize = (VAL16(pCOFF->Machine)==IMAGE_FILE_MACHINE_IA64) ? 48 : 12;
        sprintf_s(szString,SZSTRING_SIZE,"// Base of code:                   0x%08x", VAL32(pOptHeader->BaseOfCode));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Image base:                     0x%016I64x", VAL64(pOptHeader->ImageBase));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Section alignment:              0x%08x", VAL32(pOptHeader->SectionAlignment));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// File alignment:                 0x%08x", VAL32(pOptHeader->FileAlignment));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Major OS version:               0x%04x", VAL16(pOptHeader->MajorOperatingSystemVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Minor OS version:               0x%04x", VAL16(pOptHeader->MinorOperatingSystemVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Major image version:            0x%04x", VAL16(pOptHeader->MajorImageVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Minor image version:            0x%04x", VAL16(pOptHeader->MinorImageVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Major subsystem version:        0x%04x", VAL16(pOptHeader->MajorSubsystemVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Minor subsystem version:        0x%04x", VAL16(pOptHeader->MinorSubsystemVersion));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of image:                  0x%08x", VAL32(pOptHeader->SizeOfImage));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of headers:                0x%08x", VAL32(pOptHeader->SizeOfHeaders));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Checksum:                       0x%08x", VAL32(pOptHeader->CheckSum));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Subsystem:                      0x%04x", VAL16(pOptHeader->Subsystem));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// DLL characteristics:            0x%04x", VAL16(pOptHeader->DllCharacteristics));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of stack reserve:          0x%016I64x", VAL64(pOptHeader->SizeOfStackReserve));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of stack commit:           0x%016I64x", VAL64(pOptHeader->SizeOfStackCommit));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of heap reserve:           0x%016I64x", VAL64(pOptHeader->SizeOfHeapReserve));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Size of heap commit:            0x%016I64x", VAL64(pOptHeader->SizeOfHeapCommit));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Loader flags:                   0x%08x", VAL32(pOptHeader->LoaderFlags));
        printLine(GUICookie,szStr);
        sprintf_s(szString,SZSTRING_SIZE,"// Directories:                    0x%08x", VAL32(pOptHeader->NumberOfRvaAndSizes));
        printLine(GUICookie,szStr);

        DUMP_DIRECTORY("Export Directory:          ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT]);
        DUMP_DIRECTORY("Import Directory:          ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT]);
        DUMP_DIRECTORY("Resource Directory:        ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE]);
        DUMP_DIRECTORY("Exception Directory:       ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION]);
        DUMP_DIRECTORY("Security Directory:        ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY]);
        DUMP_DIRECTORY("Base Relocation Table:     ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC]);
        DUMP_DIRECTORY("Debug Directory:           ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG]);
        DUMP_DIRECTORY("Architecture Specific:     ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_ARCHITECTURE]);
        DUMP_DIRECTORY("Global Pointer:            ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_GLOBALPTR]);
        DUMP_DIRECTORY("TLS Directory:             ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS]);
        DUMP_DIRECTORY("Load Config Directory:     ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG]);
        DUMP_DIRECTORY("Bound Import Directory:    ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT]);
        DUMP_DIRECTORY("Import Address Table:      ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_IAT]);
        DUMP_DIRECTORY("Delay Load IAT:            ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT]);
        DUMP_DIRECTORY("CLR Header:                ", pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR]);
        printLine(GUICookie,"");

        DumpSectionHeaders((IMAGE_SECTION_HEADER*)(pOptHeader+1),pNTHeader->FileHeader.NumberOfSections,GUICookie);
        DumpBaseReloc("Base Relocation Table",&pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC],GUICookie);
        DumpIAT("Import Address Table", &pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT],GUICookie);
        DumpIAT("Delay Load Import Address Table", &pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT],GUICookie);
    }
    if(dwAddrOfEntryPoint != 0) DumpEntryPoint(dwAddrOfEntryPoint,dwEntryPointSize,GUICookie);
    printLine(GUICookie,"");
    printLine(GUICookie,"");
    if (!CORHeader)
    {
        printLine(GUICookie,RstrUTF(IDS_E_COMIMAGE));
        return;
    }
    strcpy_s(szString,SZSTRING_SIZE,"// ----- CLR Header:");
    printLine(GUICookie,szStr);

    sprintf_s(szString,SZSTRING_SIZE,"// Header size:                        0x%08x", VAL32(CORHeader->cb));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Major runtime version:              0x%04x", VAL16(CORHeader->MajorRuntimeVersion));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Minor runtime version:              0x%04x", VAL16(CORHeader->MinorRuntimeVersion));
    printLine(GUICookie,szStr);
    // Metadata
    DUMP_DIRECTORY("Metadata Directory:        ", CORHeader->MetaData);
    sprintf_s(szString,SZSTRING_SIZE,"// Flags:                              0x%08x", VAL32(CORHeader->Flags));
    printLine(GUICookie,szStr);
    sprintf_s(szString,SZSTRING_SIZE,"// Entry point token:                  0x%08x",
        VAL32(IMAGE_COR20_HEADER_FIELD(*CORHeader, EntryPointToken)));
    printLine(GUICookie,szStr);
    // Binding
    DUMP_DIRECTORY("Resources Directory:       ", CORHeader->Resources);
    DUMP_DIRECTORY("Strong Name Signature:     ", CORHeader->StrongNameSignature);
    DUMP_DIRECTORY("CodeManager Table:         ", CORHeader->CodeManagerTable);

    // Fixups
    DUMP_DIRECTORY("VTableFixups Directory:    ", CORHeader->VTableFixups);
    DUMP_DIRECTORY("Export Address Table:      ", CORHeader->ExportAddressTableJumps);

    // Managed Native Code
    DUMP_DIRECTORY("Precompile Header:         ", CORHeader->ManagedNativeHeader);

    DumpMetadataHeader("Metadata Header",&(CORHeader->MetaData),GUICookie);
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif


void DumpHeaderDetails(IMAGE_COR20_HEADER *CORHeader, void* GUICookie)
{
    printLine(GUICookie,"");
    DumpCodeManager(CORHeader,GUICookie);
    printLine(GUICookie,"");
    DumpVTables(CORHeader,GUICookie);
    printLine(GUICookie,"");
    DumpEATTable(CORHeader,GUICookie);
    printLine(GUICookie,"");
}


void WritePerfData(const char *KeyDesc, const char *KeyName, const char *UnitDesc, const char *UnitName, void* Value, BOOL IsInt)
{

    DWORD BytesWritten;

    if(!g_fDumpToPerfWriter) return;

    if (!g_PerfDataFilePtr)
    {
        if((g_PerfDataFilePtr = WszCreateFile(W("c:\\temp\\perfdata.dat"), GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_ALWAYS, 0, NULL) ) == INVALID_HANDLE_VALUE)
        {
         printLine(NULL,"PefTimer::LogStoppedTime(): Unable to open the FullPath file. No performance data will be generated");
         g_fDumpToPerfWriter = FALSE;
         return;
        }
        WriteFile(g_PerfDataFilePtr,"ExecTime=0\r\n",13,&BytesWritten,NULL);
        WriteFile(g_PerfDataFilePtr,"ExecUnit=bytes\r\n",17,&BytesWritten,NULL);
        WriteFile(g_PerfDataFilePtr,"ExecUnitDescr=File Size\r\n",26,&BytesWritten,NULL);
        WriteFile(g_PerfDataFilePtr,"ExeciDirection=False\r\n",23,&BytesWritten,NULL);
    }

    char ValueStr[10];
    char TmpStr[201];

    if (IsInt)
    {
        sprintf_s(ValueStr,10,"%d",(int)*(int*)Value);
    }
    else
    {
        sprintf_s(ValueStr,10,"%5.2f",(float)*(float*)Value);
    }
    sprintf_s(TmpStr, 201, "%s=%s\r\n", KeyName, ValueStr);
    WriteFile(g_PerfDataFilePtr, TmpStr, (DWORD)strlen(TmpStr), &BytesWritten, NULL);

    sprintf_s(TmpStr, 201, "%s Descr=%s\r\n", KeyName, KeyDesc);
    WriteFile(g_PerfDataFilePtr, TmpStr, (DWORD)strlen(TmpStr), &BytesWritten, NULL);

    sprintf_s(TmpStr, 201, "%s Unit=%s\r\n", KeyName, UnitName);
    WriteFile(g_PerfDataFilePtr, TmpStr, (DWORD)strlen(TmpStr), &BytesWritten, NULL);

    sprintf_s(TmpStr, 201, "%s Unit Descr=%s\r\n", KeyName, UnitDesc);
    WriteFile(g_PerfDataFilePtr, TmpStr, (DWORD)strlen(TmpStr), &BytesWritten, NULL);

    sprintf_s(TmpStr, 201, "%s IDirection=%s\r\n", KeyName, "False");
    WriteFile(g_PerfDataFilePtr, TmpStr, (DWORD)strlen(TmpStr), &BytesWritten, NULL);
}

void WritePerfDataInt(const char *KeyDesc, const char *KeyName, const char *UnitDesc, const char *UnitName, int Value)
{
    WritePerfData(KeyDesc,KeyName,UnitDesc,UnitName, (void*)&Value, TRUE);
}
void WritePerfDataFloat(const char *KeyDesc, const char *KeyName, const char *UnitDesc, const char *UnitName, float Value)
{
    WritePerfData(KeyDesc,KeyName,UnitDesc,UnitName, (void*)&Value, FALSE);
}


IMetaDataTables *pITables = NULL;
//ULONG sizeRec, count;
//int   size, size2;
int   metaSize = 0;
__int64 fTableSeen;
inline void TableSeen(unsigned long n) { fTableSeen |= (I64(1) << n); }
inline int IsTableSeen(unsigned long n) { return (fTableSeen & (I64(1) << n)) ? 1 : 0;}
inline void TableSeenReset() { fTableSeen = 0;}

void DumpTable(unsigned long Table, const char *TableName, void* GUICookie)
{
    char *szStr = &szString[0];
    const char **ppTableName = 0;
    int   size;
    ULONG sizeRec, count;

    // Record that this table has been seen.
    TableSeen(Table);

    // If no name passed in, get from table info.
    if (!TableName)
        ppTableName = &TableName;

    pITables->GetTableInfo(Table, &sizeRec, &count, NULL, NULL, ppTableName);
    if(count > 0)
    {
        metaSize += size = count * sizeRec;
        WritePerfDataInt(TableName,TableName,"count","count",count);
        WritePerfDataInt(TableName,TableName,"bytes","bytes",size);
        sprintf_s(szString,SZSTRING_SIZE,"//   %-14s- %4d (%d bytes)", TableName, count, size);
        printLine(GUICookie,szStr);
    }
}



#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
void DumpStatistics(IMAGE_COR20_HEADER *CORHeader, void* GUICookie)
{
    int     fileSize, miscPESize, miscCOMPlusSize, methodHeaderSize, methodBodySize;
    int     methodBodies, fatHeaders, tinyHeaders, deprecatedHeaders;
    int     size, size2;
    int     fatSections, smallSections;
    ULONG   methodDefs;
    ULONG   i;
    ULONG   sizeRec, count;
    char    buf[MAX_MEMBER_LENGTH];
    char* szStr = &szString[0];

    TableSeenReset();
    metaSize = 0;

    sprintf_s(szString,SZSTRING_SIZE,"// File size            : %d", fileSize = SafeGetFileSize(g_pPELoader->getHFile(), NULL));
    printLine(GUICookie,szStr);

    WritePerfDataInt("FileSize","FileSize","standard byte","bytes",fileSize);

    if (g_pPELoader->IsPE32())
    {
        size = VAL32(((IMAGE_DOS_HEADER*) g_pPELoader->getHModule())->e_lfanew) +
            sizeof(IMAGE_NT_HEADERS32) - sizeof(IMAGE_OPTIONAL_HEADER32) +
                VAL16(g_pPELoader->ntHeaders32()->FileHeader.SizeOfOptionalHeader) +
                VAL16(g_pPELoader->ntHeaders32()->FileHeader.NumberOfSections) * sizeof(IMAGE_SECTION_HEADER);
        size2 = (size + VAL32(g_pPELoader->ntHeaders32()->OptionalHeader.FileAlignment) - 1) & ~(VAL32(g_pPELoader->ntHeaders32()->OptionalHeader.FileAlignment) - 1);
    }
    else
    {
        size = VAL32(((IMAGE_DOS_HEADER*) g_pPELoader->getHModule())->e_lfanew) +
                sizeof(IMAGE_NT_HEADERS64) - sizeof(IMAGE_OPTIONAL_HEADER64) +
                VAL16(g_pPELoader->ntHeaders64()->FileHeader.SizeOfOptionalHeader) +
                VAL16(g_pPELoader->ntHeaders64()->FileHeader.NumberOfSections) * sizeof(IMAGE_SECTION_HEADER);
        size2 = (size + VAL32(g_pPELoader->ntHeaders64()->OptionalHeader.FileAlignment) - 1) & ~(VAL32(g_pPELoader->ntHeaders64()->OptionalHeader.FileAlignment) - 1);
    }

    DWORD   sizeOfHeaders;

    if (g_pPELoader->IsPE32())
    {
        sizeOfHeaders = VAL32(g_pPELoader->ntHeaders32()->OptionalHeader.SizeOfHeaders);

        WritePerfDataInt("PE header size", "PE header size", "standard byte", "bytes", sizeOfHeaders);
        WritePerfDataInt("PE header size used", "PE header size used", "standard byte", "bytes", size);
        WritePerfDataFloat("PE header size", "PE header size", "percentage", "percentage", (float)((sizeOfHeaders * 100) / fileSize));
        sprintf_s(szString,SZSTRING_SIZE,"// PE header size       : %d (%d used)    (%5.2f%%)",
            sizeOfHeaders, size, (double) (sizeOfHeaders * 100) / fileSize);

        printLine(GUICookie,szStr);
        miscPESize = 0;

        for (i=0; i < VAL32(g_pPELoader->ntHeaders32()->OptionalHeader.NumberOfRvaAndSizes); ++i)
        {
            // Skip the CLR header.
            if (i != 15) miscPESize += (int) VAL32(g_pPELoader->ntHeaders32()->OptionalHeader.DataDirectory[i].Size);
        }
    }
    else
    {
        sizeOfHeaders = VAL32(g_pPELoader->ntHeaders64()->OptionalHeader.SizeOfHeaders);

        WritePerfDataInt("PE+ header size", "PE header size", "standard byte", "bytes", sizeOfHeaders);
        WritePerfDataInt("PE+ header size used", "PE header size used", "standard byte", "bytes", size);
        WritePerfDataFloat("PE+ header size", "PE header size", "percentage", "percentage", (float)((sizeOfHeaders * 100) / fileSize));

        sprintf_s(szString,SZSTRING_SIZE,"// PE header size       : %d (%d used)    (%5.2f%%)",
            sizeOfHeaders, size, (double) (sizeOfHeaders * 100) / fileSize);

        printLine(GUICookie,szStr);
        miscPESize = 0;

        for (i=0; i < VAL32(g_pPELoader->ntHeaders64()->OptionalHeader.NumberOfRvaAndSizes); ++i)
        {
            // Skip the CLR header.
            if (i != IMAGE_DIRECTORY_ENTRY_COMHEADER) miscPESize += (int) VAL32(g_pPELoader->ntHeaders64()->OptionalHeader.DataDirectory[i].Size);
        }
    }

    WritePerfDataInt("PE additional info", "PE additional info", "standard byte", "bytes",miscPESize);
    WritePerfDataFloat("PE additional info", "PE additional info", "percentage", "percent", (float) ((miscPESize * 100) / fileSize));

    sprintf_s(buf, MAX_MEMBER_LENGTH, "PE additional info   : %d", miscPESize);
    sprintf_s(szString,SZSTRING_SIZE,"// %-40s (%5.2f%%)", buf, (double) (miscPESize * 100) / fileSize);
    printLine(GUICookie,szStr);

    WORD    numberOfSections;
    if (g_pPELoader->IsPE32())
    {
        numberOfSections = VAL16(g_pPELoader->ntHeaders32()->FileHeader.NumberOfSections);
    }
    else
    {
        numberOfSections = VAL16(g_pPELoader->ntHeaders64()->FileHeader.NumberOfSections);
    }

    WritePerfDataInt("Num.of PE sections", "Num.of PE sections", "Nbr of sections", "sections",numberOfSections);
    sprintf_s(szString,SZSTRING_SIZE,"// Num.of PE sections   : %d", numberOfSections);

    printLine(GUICookie,szStr);

    WritePerfDataInt("CLR header size", "CLR header size", "byte", "bytes",VAL32(CORHeader->cb));
    WritePerfDataFloat("CLR header size", "CLR header size", "percentage", "percent",(float) ((VAL32(CORHeader->cb) * 100) / fileSize));

    sprintf_s(buf, MAX_MEMBER_LENGTH, "CLR header size     : %d", VAL32(CORHeader->cb));
    sprintf_s(szString,SZSTRING_SIZE,"// %-40s (%5.2f%%)", buf, (double) (VAL32(CORHeader->cb) * 100) / fileSize);
    printLine(GUICookie,szStr);

    DWORD dwMetaSize = g_cbMetaData;
    WritePerfDataInt("CLR meta-data size", "CLR meta-data size", "bytes", "bytes",dwMetaSize);
    WritePerfDataFloat("CLR meta-data size", "CLR meta-data size", "percentage", "percent",(float) ((dwMetaSize * 100) / fileSize));

    sprintf_s(buf, MAX_MEMBER_LENGTH, "CLR meta-data size  : %d", dwMetaSize);
    sprintf_s(szString,SZSTRING_SIZE,"// %-40s (%5.2f%%)", buf, (double) (dwMetaSize * 100) / fileSize);
    printLine(GUICookie,szStr);

    IMAGE_DATA_DIRECTORY *pFirst = &CORHeader->Resources;
    ULONG32 iCount = (ULONG32)((BYTE *) &CORHeader->ManagedNativeHeader - (BYTE *) &CORHeader->Resources) / sizeof(IMAGE_DATA_DIRECTORY) + 1;
    miscCOMPlusSize = 0;
    for (ULONG32 iDir=0;  iDir<iCount;  iDir++)
    {
        miscCOMPlusSize += VAL32(pFirst->Size);
        pFirst++;
    }

    WritePerfDataInt("CLR Additional info", "CLR Additional info", "bytes", "bytes",miscCOMPlusSize);
    WritePerfDataFloat("CLR Additional info", "CLR Additional info", "percentage", "percent",(float) ((miscCOMPlusSize * 100) / fileSize));

    sprintf_s(buf, MAX_MEMBER_LENGTH, "CLR additional info : %d", miscCOMPlusSize);
    sprintf_s(szString,SZSTRING_SIZE,"// %-40s (%5.2f%%)", buf, (double) (miscCOMPlusSize * 100) / fileSize);
    printLine(GUICookie,szStr);

    // Go through each method def collecting some statistics.
    methodHeaderSize = methodBodySize = 0;
    methodBodies = fatHeaders = tinyHeaders = deprecatedHeaders = fatSections = smallSections = 0;
    methodDefs = g_pImport->GetCountWithTokenKind(mdtMethodDef);
    for (i=1; i <= methodDefs; ++i) {
        ULONG   rva;
        DWORD   flags;

        if (FAILED(g_pImport->GetMethodImplProps(TokenFromRid(i, mdtMethodDef), &rva, &flags)))
        {
            continue;
        }
        if ((rva != 0)&&(IsMiIL(flags) || IsMiOPTIL(flags))) // We don't handle native yet.
        {
            ++methodBodies;

            COR_ILMETHOD_FAT *pMethod = NULL;
            g_pPELoader->getVAforRVA(rva, (void **) &pMethod);
            if (pMethod->IsFat())
            {
                ++fatHeaders;

                methodHeaderSize += pMethod->GetSize() * 4;
                methodBodySize += pMethod->GetCodeSize();

                // Add in the additional sections.
                BYTE *sectsBegin = (BYTE *) (pMethod->GetCode() + pMethod->GetCodeSize());
                const COR_ILMETHOD_SECT *pSect = pMethod->GetSect();
                const COR_ILMETHOD_SECT *pOldSect;
                if (pSect != NULL) {
                    // Keep skipping a pointer past each section.
                    do
                    {
                        pOldSect = pSect;
                        if (((COR_ILMETHOD_SECT_FAT *) pSect)->GetKind() & CorILMethod_Sect_FatFormat)
                        {
                            ++fatSections;
                            pSect = (COR_ILMETHOD_SECT *)((BYTE *) pSect + ((COR_ILMETHOD_SECT_FAT *) pSect)->GetDataSize());
                        }
                        else
                        {
                            ++smallSections;
                            pSect = (COR_ILMETHOD_SECT *)((BYTE *) pSect + ((COR_ILMETHOD_SECT_SMALL *) pSect)->DataSize);
                        }
                        pSect = (COR_ILMETHOD_SECT *) (((UINT_PTR) pSect + 3) & ~3);
                    }
                    while (pOldSect->More());

                    // Add on the section sizes.
                    methodHeaderSize += (int) ((BYTE *) pSect - sectsBegin);
                }
            }
            else if (((COR_ILMETHOD_TINY *) pMethod)->IsTiny())
            {
                ++tinyHeaders;
                methodHeaderSize += sizeof(COR_ILMETHOD_TINY);
                methodBodySize += ((COR_ILMETHOD_TINY *) pMethod)->GetCodeSize();
            }
            else
            {
                _ASSERTE(!"Unrecognized header type");
            }
        }
    }


    WritePerfDataInt("CLR method headers", "CLR method headers", "bytes", "bytes",methodHeaderSize);
    WritePerfDataFloat("CLR method headers", "CLR method headers", "percentage", "percent",(float) ((methodHeaderSize * 100) / fileSize));

    sprintf_s(buf, MAX_MEMBER_LENGTH, "CLR method headers  : %d", methodHeaderSize);
    sprintf_s(szString,SZSTRING_SIZE,"// %-40s (%5.2f%%)", buf, (double) (methodHeaderSize * 100) / fileSize);
    printLine(GUICookie,szStr);

    WritePerfDataInt("Managed code", "Managed code", "bytes", "bytes",methodBodySize);
    WritePerfDataFloat("Managed code", "Managed code", "percentage", "percent",(float) ((methodBodySize * 100) / fileSize));

    sprintf_s(buf, MAX_MEMBER_LENGTH, "Managed code         : %d", methodBodySize);
    sprintf_s(szString,SZSTRING_SIZE,"// %-40s (%5.2f%%)", buf, (double) (methodBodySize * 100) / fileSize);
    printLine(GUICookie,szStr);

   if (g_pPELoader->IsPE32())
   {
       DWORD sizeOfInitializedData = VAL32(g_pPELoader->ntHeaders32()->OptionalHeader.SizeOfInitializedData);

       WritePerfDataInt("Data", "Data", "bytes", "bytes",sizeOfInitializedData);
       WritePerfDataFloat("Data", "Data", "percentage", "percent",(float) ((sizeOfInitializedData * 100) / fileSize));

       sprintf_s(buf, MAX_MEMBER_LENGTH, "Data                 : %d", sizeOfInitializedData);
       sprintf_s(szString,SZSTRING_SIZE,"// %-40s (%5.2f%%)", buf, (double) (sizeOfInitializedData * 100) / fileSize);
       printLine(GUICookie,szStr);

       size = fileSize - g_pPELoader->ntHeaders32()->OptionalHeader.SizeOfHeaders - miscPESize - CORHeader->cb -
              g_cbMetaData - miscCOMPlusSize -
              sizeOfInitializedData -
              methodHeaderSize - methodBodySize;
   }
   else
   {
        DWORD sizeOfInitializedData = VAL32(g_pPELoader->ntHeaders64()->OptionalHeader.SizeOfInitializedData);

        WritePerfDataInt("Data", "Data", "bytes", "bytes",sizeOfInitializedData);
        WritePerfDataFloat("Data", "Data", "percentage", "percent",(float) ((sizeOfInitializedData * 100) / fileSize));

        sprintf_s(buf, MAX_MEMBER_LENGTH, "Data                 : %d", sizeOfInitializedData);
        sprintf_s(szString,SZSTRING_SIZE,"// %-40s (%5.2f%%)", buf, (double) (sizeOfInitializedData * 100) / fileSize);
        printLine(GUICookie,szStr);

        size = fileSize - g_pPELoader->ntHeaders64()->OptionalHeader.SizeOfHeaders - miscPESize - CORHeader->cb -
            g_cbMetaData - miscCOMPlusSize -
            sizeOfInitializedData -
            methodHeaderSize - methodBodySize;
   }

    WritePerfDataInt("Unaccounted", "Unaccounted", "bytes", "bytes",size);
    WritePerfDataFloat("Unaccounted", "Unaccounted", "percentage", "percent",(float) ((size * 100) / fileSize));

    sprintf_s(buf, MAX_MEMBER_LENGTH, "Unaccounted          : %d", size);
    sprintf_s(szString,SZSTRING_SIZE,"// %-40s (%5.2f%%)", buf, (double) (size * 100) / fileSize);
    printLine(GUICookie,szStr);


   // Detail...
   if (g_pPELoader->IsPE32())
   {
        numberOfSections = VAL16(g_pPELoader->ntHeaders32()->FileHeader.NumberOfSections);

        WritePerfDataInt("Num.of PE sections", "Num.of PE sections", "bytes", "bytes",numberOfSections);
        printLine(GUICookie,"");
        sprintf_s(szString,SZSTRING_SIZE,"// Num.of PE sections   : %d", numberOfSections);
        printLine(GUICookie,szStr);

        IMAGE_SECTION_HEADER *pSecHdr = IMAGE_FIRST_SECTION(g_pPELoader->ntHeaders32());

        for (i=0; i < numberOfSections; ++i)
        {
            WritePerfDataInt((char*)pSecHdr->Name,(char*)pSecHdr->Name, "bytes", "bytes",VAL32(pSecHdr->SizeOfRawData));
            sprintf_s(szString,SZSTRING_SIZE,"//   %-8s - %d", pSecHdr->Name, VAL32(pSecHdr->SizeOfRawData));
            printLine(GUICookie,szStr);
            ++pSecHdr;
        }
   }
   else
   {
        numberOfSections = VAL16(g_pPELoader->ntHeaders64()->FileHeader.NumberOfSections);

        WritePerfDataInt("Num.of PE sections", "Num.of PE sections", "bytes", "bytes",numberOfSections);
        printLine(GUICookie,"");
        sprintf_s(szString,SZSTRING_SIZE,"// Num.of PE sections   : %d", numberOfSections);
        printLine(GUICookie,szStr);

        IMAGE_SECTION_HEADER *pSecHdr = IMAGE_FIRST_SECTION(g_pPELoader->ntHeaders64());

        for (i=0; i < numberOfSections; ++i)
        {
            WritePerfDataInt((char*)pSecHdr->Name,(char*)pSecHdr->Name, "bytes", "bytes",pSecHdr->SizeOfRawData);
            sprintf_s(szString,SZSTRING_SIZE,"//   %-8s - %d", pSecHdr->Name, pSecHdr->SizeOfRawData);
                printLine(GUICookie,szStr);
            ++pSecHdr;
        }
   }

    if (FAILED(g_pPubImport->QueryInterface(IID_IMetaDataTables, (void**)&pITables)))
    {
        sprintf_s(szString,SZSTRING_SIZE,"// Unable to get IMetaDataTables interface");
        printLine(GUICookie,szStr);
        return;
    }

    if (pITables == 0)
    {
        printLine(GUICookie,RstrUTF(IDS_E_MDDETAILS));
        return;
    }
    else
    {
        DWORD   Size = g_cbMetaData;
        WritePerfDataInt("CLR meta-data size", "CLR meta-data size", "bytes", "bytes",Size);
        printLine(GUICookie,"");
        sprintf_s(szString,SZSTRING_SIZE,"// CLR meta-data size  : %d", Size);
        printLine(GUICookie,szStr);
        metaSize = 0;

        pITables->GetTableInfo(TBL_Module, &sizeRec, &count, NULL, NULL, NULL);
        TableSeen(TBL_Module);
        metaSize += size = count * sizeRec;                                     \
        WritePerfDataInt("Module (count)", "Module (count)", "count", "count",count);
        WritePerfDataInt("Module (bytes)", "Module (bytes)", "bytes", "bytes",size);
        sprintf_s(szString,SZSTRING_SIZE,"//   %-14s- %4d (%d bytes)", "Module", count, size); \
        printLine(GUICookie,szStr);

        if ((count = g_pImport->GetCountWithTokenKind(mdtTypeDef)) > 0)
        {
            int     flags, interfaces = 0, explicitLayout = 0;
            for (i=1; i <= count; ++i)
            {
                if (FAILED(g_pImport->GetTypeDefProps(TokenFromRid(i, mdtTypeDef), (ULONG *) &flags, NULL)))
                {
                    continue;
                }
                if (flags & tdInterface) ++interfaces;
                if (flags & tdExplicitLayout)   ++explicitLayout;
            }
            // Get count from table -- count reported by GetCount... doesn't include the "global" typedef.
            pITables->GetTableInfo(TBL_TypeDef, &sizeRec, &count, NULL, NULL, NULL);
            TableSeen(TBL_TypeDef);
            metaSize += size = count * sizeRec;

            WritePerfDataInt("TypeDef (count)", "TypeDef (count)", "count", "count", count);
            WritePerfDataInt("TypeDef (bytes)", "TypeDef (bytes)", "bytes", "bytes", size);
            WritePerfDataInt("interfaces", "interfaces", "count", "count", interfaces);
            WritePerfDataInt("explicitLayout", "explicitLayout", "count", "count", explicitLayout);

            sprintf_s(buf, MAX_MEMBER_LENGTH, "  TypeDef       - %4d (%d bytes)", count, size);
            sprintf_s(szString,SZSTRING_SIZE,"// %-38s %d interfaces, %d explicit layout", buf, interfaces, explicitLayout);
            printLine(GUICookie,szStr);
        }
    }

    pITables->GetTableInfo(TBL_TypeRef, &sizeRec, &count, NULL, NULL, NULL);
    TableSeen(TBL_TypeRef);
    if (count > 0)
    {
        metaSize += size = count * sizeRec;                                      \
        WritePerfDataInt("TypeRef (count)", "TypeRef (count)", "count", "count", count);
        WritePerfDataInt("TypeRef (bytes)", "TypeRef (bytes)", "bytes", "bytes", size);
        sprintf_s(szString,SZSTRING_SIZE,"//   %-14s- %4d (%d bytes)", "TypeRef", count, size); \
        printLine(GUICookie,szStr);
    }

    if ((count = g_pImport->GetCountWithTokenKind(mdtMethodDef)) > 0)
    {
        int     flags, abstract = 0, native = 0;
        for (i=1; i <= count; ++i)
        {
            if (FAILED(g_pImport->GetMethodDefProps(TokenFromRid(i, mdtMethodDef), (DWORD *)&flags)))
            {
                sprintf_s(szString, SZSTRING_SIZE, "// Invalid MethodDef %08X record", TokenFromRid(i, mdtMethodDef));
                printLine(GUICookie, szStr);
                return;
            }
            if (flags & mdAbstract) ++abstract;
        }
        pITables->GetTableInfo(TBL_Method, &sizeRec, NULL, NULL, NULL, NULL);
        TableSeen(TBL_Method);
        if (count > 0)
        {
            metaSize += size = count * sizeRec;

            WritePerfDataInt("MethodDef (count)", "MethodDef (count)", "count", "count", count);
            WritePerfDataInt("MethodDef (bytes)", "MethodDef (bytes)", "bytes", "bytes", size);
            WritePerfDataInt("abstract", "abstract", "count", "count", abstract);
            WritePerfDataInt("native", "native", "count", "count", native);
            WritePerfDataInt("methodBodies", "methodBodies", "count", "count", methodBodies);

            sprintf_s(buf, MAX_MEMBER_LENGTH, "  MethodDef     - %4d (%d bytes)", count, size);
            sprintf_s(szString,SZSTRING_SIZE,"// %-38s %d abstract, %d native, %d bodies", buf, abstract, native, methodBodies);
            printLine(GUICookie,szStr);
        }
    }

    if ((count = g_pImport->GetCountWithTokenKind(mdtFieldDef)) > 0)
    {
        int     flags, constants = 0;

        for (i=1; i <= count; ++i)
        {
            if (FAILED(g_pImport->GetFieldDefProps(TokenFromRid(i, mdtFieldDef), (DWORD *)&flags)))
            {
                sprintf_s(szString, SZSTRING_SIZE, "// Invalid FieldDef %08X record", TokenFromRid(i, mdtFieldDef));
                printLine(GUICookie, szStr);
                return;
            }
            if ((flags & (fdStatic|fdInitOnly)) == (fdStatic|fdInitOnly)) ++constants;
        }
        pITables->GetTableInfo(TBL_Field, &sizeRec, NULL, NULL, NULL, NULL);
        metaSize += size = count * sizeRec;

        WritePerfDataInt("FieldDef (count)", "FieldDef (count)", "count", "count", count);
        WritePerfDataInt("FieldDef (bytes)", "FieldDef (bytes)", "bytes", "bytes", size);
        WritePerfDataInt("constant", "constant", "count", "count", constants);

        sprintf_s(buf, MAX_MEMBER_LENGTH, "  FieldDef      - %4d (%d bytes)", count, size);
        sprintf_s(szString,SZSTRING_SIZE,"// %-38s %d constant", buf, constants);
        printLine(GUICookie,szStr);
        TableSeen(TBL_Field);
    }

    DumpTable(TBL_MemberRef,          "MemberRef",              GUICookie);
    DumpTable(TBL_Param,              "ParamDef",               GUICookie);
    DumpTable(TBL_MethodImpl,         "MethodImpl",             GUICookie);
    DumpTable(TBL_Constant,           "Constant",               GUICookie);
    DumpTable(TBL_CustomAttribute,    "CustomAttribute",        GUICookie);
    DumpTable(TBL_FieldMarshal,       "NativeType",             GUICookie);
    DumpTable(TBL_ClassLayout,        "ClassLayout",            GUICookie);
    DumpTable(TBL_FieldLayout,        "FieldLayout",            GUICookie);
    DumpTable(TBL_StandAloneSig,      "StandAloneSig",          GUICookie);
    DumpTable(TBL_InterfaceImpl,      "InterfaceImpl",          GUICookie);
    DumpTable(TBL_PropertyMap,        "PropertyMap",            GUICookie);
    DumpTable(TBL_Property,           "Property",               GUICookie);
    DumpTable(TBL_MethodSemantics,    "MethodSemantic",         GUICookie);
    DumpTable(TBL_DeclSecurity,       "Security",               GUICookie);
    DumpTable(TBL_TypeSpec,           "TypeSpec",               GUICookie);
    DumpTable(TBL_ModuleRef,          "ModuleRef",              GUICookie);
    DumpTable(TBL_Assembly,           "Assembly",               GUICookie);
    DumpTable(TBL_AssemblyProcessor,  "AssemblyProcessor",      GUICookie);
    DumpTable(TBL_AssemblyOS,         "AssemblyOS",             GUICookie);
    DumpTable(TBL_AssemblyRef,        "AssemblyRef",            GUICookie);
    DumpTable(TBL_AssemblyRefProcessor, "AssemblyRefProcessor", GUICookie);
    DumpTable(TBL_AssemblyRefOS,      "AssemblyRefOS",          GUICookie);
    DumpTable(TBL_File,               "File",                   GUICookie);
    DumpTable(TBL_ExportedType,       "ExportedType",           GUICookie);
    DumpTable(TBL_ManifestResource,   "ManifestResource",       GUICookie);
    DumpTable(TBL_NestedClass,        "NestedClass",            GUICookie);

    // Rest of the tables.
    pITables->GetNumTables(&count);
    for (i=0; i<count; ++i)
    {
        if (!IsTableSeen(i))
            DumpTable(i, NULL, GUICookie);
    }

    // String heap
    pITables->GetStringHeapSize(&sizeRec);
    if (sizeRec > 0)
    {
        metaSize += sizeRec;
        WritePerfDataInt("Strings", "Strings", "bytes", "bytes",sizeRec);
        sprintf_s(szString,SZSTRING_SIZE,"//   Strings       - %5d bytes", sizeRec);
        printLine(GUICookie,szStr);
    }
    // Blob heap
    pITables->GetBlobHeapSize(&sizeRec);
    if (sizeRec > 0)
    {
        metaSize += sizeRec;
        WritePerfDataInt("Blobs", "Blobs", "bytes", "bytes",sizeRec);
        sprintf_s(szString,SZSTRING_SIZE,"//   Blobs         - %5d bytes", sizeRec);
        printLine(GUICookie,szStr);
    }
    // User String Heap
    pITables->GetUserStringHeapSize(&sizeRec);
    if (sizeRec > 0)
    {
        metaSize += sizeRec;
        WritePerfDataInt("UserStrings", "UserStrings", "bytes", "bytes",sizeRec);
        sprintf_s(szString,SZSTRING_SIZE,"//   UserStrings   - %5d bytes", sizeRec);
        printLine(GUICookie,szStr);
    }
    // Guid heap
    pITables->GetGuidHeapSize(&sizeRec);
    if (sizeRec > 0)
    {
        metaSize += sizeRec;
        WritePerfDataInt("Guids", "Guids", "bytes", "bytes", sizeRec);
        sprintf_s(szString,SZSTRING_SIZE,"//   Guids         - %5d bytes", sizeRec);
        printLine(GUICookie,szStr);
    }

    if (g_cbMetaData - metaSize > 0)
    {
        WritePerfDataInt("Uncategorized", "Uncategorized", "bytes", "bytes",g_cbMetaData - metaSize);
        sprintf_s(szString,SZSTRING_SIZE,"//   Uncategorized - %5d bytes", g_cbMetaData - metaSize);
        printLine(GUICookie,szStr);
    }

    if (miscCOMPlusSize != 0)
    {
        WritePerfDataInt("CLR additional info", "CLR additional info", "bytes", "bytes", miscCOMPlusSize);
        sprintf_s(szString,SZSTRING_SIZE,"// CLR additional info : %d", miscCOMPlusSize);
        printLine(GUICookie,"");
        printLine(GUICookie,szStr);

        if (CORHeader->CodeManagerTable.Size != 0)
        {
            WritePerfDataInt("CodeManagerTable", "CodeManagerTable", "bytes", "bytes", VAL32(CORHeader->CodeManagerTable.Size));
            sprintf_s(szString,SZSTRING_SIZE,"//   CodeManagerTable  - %d", VAL32(CORHeader->CodeManagerTable.Size));
            printLine(GUICookie,szStr);
        }

        if (CORHeader->VTableFixups.Size != 0)
        {
            WritePerfDataInt("VTableFixups", "VTableFixups", "bytes", "bytes", VAL32(CORHeader->VTableFixups.Size));
            sprintf_s(szString,SZSTRING_SIZE,"//   VTableFixups      - %d", VAL32(CORHeader->VTableFixups.Size));
            printLine(GUICookie,szStr);
        }

        if (CORHeader->Resources.Size != 0)
        {
            WritePerfDataInt("Resources", "Resources", "bytes", "bytes", VAL32(CORHeader->Resources.Size));
            sprintf_s(szString,SZSTRING_SIZE,"//   Resources         - %d", VAL32(CORHeader->Resources.Size));
            printLine(GUICookie,szStr);
        }
    }
    WritePerfDataInt("CLR method headers", "CLR method headers", "count", "count", methodHeaderSize);
    sprintf_s(szString,SZSTRING_SIZE,"// CLR method headers : %d", methodHeaderSize);
    printLine(GUICookie,"");
    printLine(GUICookie,szStr);
    WritePerfDataInt("Num.of method bodies", "Num.of method bodies", "count", "count",methodBodies);
    sprintf_s(szString,SZSTRING_SIZE,"//   Num.of method bodies  - %d", methodBodies);
    printLine(GUICookie,szStr);
    WritePerfDataInt("Num.of fat headers", "Num.of fat headers", "count", "count", fatHeaders);
    sprintf_s(szString,SZSTRING_SIZE,"//   Num.of fat headers    - %d", fatHeaders);
    printLine(GUICookie,szStr);
    WritePerfDataInt("Num.of tiny headers", "Num.of tiny headers", "count", "count", tinyHeaders);
    sprintf_s(szString,SZSTRING_SIZE,"//   Num.of tiny headers   - %d", tinyHeaders);
    printLine(GUICookie,szStr);

    if (deprecatedHeaders > 0) {
        WritePerfDataInt("Num.of old headers", "Num.of old headers", "count", "count", deprecatedHeaders);
        sprintf_s(szString,SZSTRING_SIZE,"//   Num.of old headers    - %d", deprecatedHeaders);
        printLine(GUICookie,szStr);
    }

    if (fatSections != 0 || smallSections != 0) {
        WritePerfDataInt("Num.of fat sections", "Num.of fat sections", "count", "count", fatSections);
        sprintf_s(szString,SZSTRING_SIZE,"//   Num.of fat sections   - %d", fatSections);
        printLine(GUICookie,szStr);

        WritePerfDataInt("Num.of small section", "Num.of small section", "count", "count", smallSections);
        sprintf_s(szString,SZSTRING_SIZE,"//   Num.of small sections - %d", smallSections);
        printLine(GUICookie,szStr);
    }

    WritePerfDataInt("Managed code", "Managed code", "bytes", "bytes", methodBodySize);
    sprintf_s(szString,SZSTRING_SIZE,"// Managed code : %d", methodBodySize);
    printLine(GUICookie,"");
    printLine(GUICookie,szStr);

    if (methodBodies != 0) {
        WritePerfDataInt("Ave method size", "Ave method size", "bytes", "bytes", methodBodySize / methodBodies);
        sprintf_s(szString,SZSTRING_SIZE,"//   Ave method size - %d", methodBodySize / methodBodies);
        printLine(GUICookie,szStr);
    }

    if (pITables)
        pITables->Release();

    if(g_fDumpToPerfWriter)
        CloseHandle((char*) g_PerfDataFilePtr);
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

void DumpHexbytes(__inout __nullterminated char* szptr,BYTE *pb, DWORD fromPtr, DWORD toPtr, DWORD limPtr)
{
    char sz[256];
    int k = 0,i;
    DWORD curPtr = 0;
    bool printsz = FALSE;
    BYTE zero = 0;
    *szptr = 0;
    for(i = 0,k = 0,curPtr=fromPtr; curPtr < toPtr; i++,k++,curPtr++,pb++)
    {

        if(k == 16)
        {
            if(printsz) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("  // %s"),sz);
            printLine(g_pFile,szString);
            szptr = &szString[0];
            szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s                ",g_szAsmCodeIndent);
            k = 0;
            printsz = FALSE;
        }
        if(curPtr >= limPtr) pb = &zero;    // at limPtr and after, pad with 0
        else
        {
            PAL_CPP_TRY
            {
                sz[k] = *pb; // check the ptr validity
            }
            PAL_CPP_CATCH_ALL
            {
                pb = &zero;
            } PAL_CPP_ENDTRY;
        }
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," %2.2X", *pb);
        if(isprint(*pb))
        {
            if(g_fDumpRTF)
            {
                if((*pb == '\\')||(*pb=='{')||(*pb=='}')) sz[k++]='\\';
                sz[k] = *pb;
            }
            else if(g_fDumpHTML)
            {
                if(*pb == '<') { sz[k] = 0; strcat_s(sz,256-k,LTN()); k+=(int)(strlen(LTN())); }
                else if(*pb == '>') { sz[k] = 0; strcat_s(sz,256-k,GTN()); k+=(int)(strlen(GTN())); }
            }
            else sz[k] = *pb;
            printsz = TRUE;
        }
        else
        {
            sz[k] = '.';
        }
        sz[k+1] = 0;
    }
    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),") ");
    if(printsz)
    {
        for(i = k; i < 16; i++) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"   ");
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("// %s"),sz);
    }
    printLine(g_pFile,szString);
}

struct  VTableEntry
{
    DWORD   dwAddr;
    WORD    wCount;
    WORD    wType;
};

struct ExpDirTable
{
    DWORD   dwFlags;
    DWORD   dwDateTime;
    WORD    wVMajor;
    WORD    wVMinor;
    DWORD   dwNameRVA;
    DWORD   dwOrdinalBase;
    DWORD   dwNumATEntries;
    DWORD   dwNumNamePtrs;
    DWORD   dwAddrTableRVA;
    DWORD   dwNamePtrRVA;
    DWORD   dwOrdTableRVA;
};

void DumpEATEntries(void* GUICookie,
    IMAGE_NT_HEADERS32 *pNTHeader32, IMAGE_OPTIONAL_HEADER32 *pOptHeader32,
    IMAGE_NT_HEADERS64 *pNTHeader64, IMAGE_OPTIONAL_HEADER64 *pOptHeader64)
{
    IMAGE_DATA_DIRECTORY *pExportDir = NULL;
    IMAGE_SECTION_HEADER *pSecHdr = NULL;
    DWORD i,j,N;
    BOOL bpOpt = FALSE;

    if (g_pPELoader->IsPE32())
    {
        pExportDir = pOptHeader32->DataDirectory;
        pSecHdr = IMAGE_FIRST_SECTION(pNTHeader32);
        N = VAL16(pNTHeader32->FileHeader.NumberOfSections);

        if (pOptHeader32->NumberOfRvaAndSizes)
            bpOpt = TRUE;
    }
    else
    {
        pExportDir = pOptHeader64->DataDirectory;
        pSecHdr = IMAGE_FIRST_SECTION(pNTHeader64);
        N = VAL16(pNTHeader64->FileHeader.NumberOfSections);

        if (pOptHeader64->NumberOfRvaAndSizes)
            bpOpt = TRUE;

    }
    if(bpOpt)
    {
        ExpDirTable *pExpTable = NULL;
        if(pExportDir->Size)
        {
#ifdef _DEBUG
            printLine(GUICookie,COMMENT((char*)0)); // start multiline comment
            sprintf_s(szString,SZSTRING_SIZE,"// Export dir VA=%X size=%X ",VAL32(pExportDir->VirtualAddress),VAL32(pExportDir->Size));
            printLine(GUICookie,szString);
#endif
            DWORD vaExpTable = VAL32(pExportDir->VirtualAddress);
            for (i=0; i < N; i++,pSecHdr++)
            {
                if((vaExpTable >= VAL32(pSecHdr->VirtualAddress))&&
                    (vaExpTable < VAL32(pSecHdr->VirtualAddress)+VAL32(pSecHdr->Misc.VirtualSize)))
                {
                    pExpTable = (ExpDirTable*)( g_pPELoader->base()
                        + VAL32(pSecHdr->PointerToRawData)
                        + vaExpTable - VAL32(pSecHdr->VirtualAddress));
#ifdef _DEBUG
                    sprintf_s(szString,SZSTRING_SIZE,"// in section '%s': VA=%X Misc.VS=%X PRD=%X ",(char*)(pSecHdr->Name),
                        VAL32(pSecHdr->VirtualAddress),VAL32(pSecHdr->Misc.VirtualSize),VAL32(pSecHdr->PointerToRawData));
                    printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// Export Directory Table:"); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// dwFlags = %X",VAL32(pExpTable->dwFlags)); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// dwDateTime = %X",VAL32(pExpTable->dwDateTime)); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// wVMajor = %X",VAL16(pExpTable->wVMajor)); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// wVMinor = %X",VAL16(pExpTable->wVMinor)); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// dwNameRVA = %X",VAL32(pExpTable->dwNameRVA)); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// dwOrdinalBase = %X",VAL32(pExpTable->dwOrdinalBase)); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// dwNumATEntries = %X",VAL32(pExpTable->dwNumATEntries)); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// dwNumNamePtrs = %X",VAL32(pExpTable->dwNumNamePtrs)); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// dwAddrTableRVA = %X",VAL32(pExpTable->dwAddrTableRVA)); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// dwNamePtrRVA = %X",VAL32(pExpTable->dwNamePtrRVA)); printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"// dwOrdTableRVA = %X",VAL32(pExpTable->dwOrdTableRVA)); printLine(GUICookie,szString);
                    if(pExpTable->dwNameRVA)
                    {
                        char*   szName;
                        if(g_pPELoader->getVAforRVA(VAL32(pExpTable->dwNameRVA), (void **) &szName))
                            sprintf_s(szString,SZSTRING_SIZE,"// DLL Name: '%s'",szName);
                        else
                            sprintf_s(szString,SZSTRING_SIZE,"// DLL Name: BAD RVA: 0x%8.8X",VAL32(pExpTable->dwNameRVA));

                        printLine(GUICookie,szString);
                    }
#endif
                    if(pExpTable->dwNumATEntries && pExpTable->dwAddrTableRVA)
                    {
                        DWORD* pExpAddr = NULL;
                        BYTE *pCont = NULL;
                        DWORD dwTokRVA;
                        mdToken* pTok;
                        g_pPELoader->getVAforRVA(VAL32(pExpTable->dwAddrTableRVA), (void **) &pExpAddr);
#ifdef _DEBUG
                        sprintf_s(szString,SZSTRING_SIZE,"// Export Address Table:"); printLine(GUICookie,szString);
#endif
                        g_nEATableRef = VAL32(pExpTable->dwNumATEntries);
                        if (g_prEATableRef == NULL)
                        {
                            g_prEATableRef = new DynamicArray<EATableRef>;
                        }

                        (*g_prEATableRef)[g_nEATableRef].tkTok = 0; // to avoid multiple reallocations of DynamicArray
                        for(j=0; j < VAL32(pExpTable->dwNumATEntries); j++,pExpAddr++)
                        {
                            g_pPELoader->getVAforRVA(VAL32(*pExpAddr), (void **) &pCont);
#ifdef _DEBUG
                            sprintf_s(szString,SZSTRING_SIZE,"// [%d]: RVA=%X VA=%p(",j,VAL32(*pExpAddr),pCont);
                            DumpByteArray(szString,pCont,16,GUICookie);
                            printLine(GUICookie,szString);
#endif
                            (*g_prEATableRef)[j].tkTok = 0;

                            if(g_pPELoader->IsPE32())
                            {
                                dwTokRVA = VAL32(*((DWORD*)(pCont+2))); // first two bytes - JumpIndirect (0x25FF)
                                dwTokRVA -= VAL32((DWORD)pOptHeader32->ImageBase);
                            }
                            else
                            {
                                ULONGLONG ullTokRVA;
                                if(pNTHeader64->FileHeader.Machine == IMAGE_FILE_MACHINE_IA64)
                                    ullTokRVA = VAL64(*((ULONGLONG*)(pCont+8)));
                                else
                                    ullTokRVA = VAL64(*((ULONGLONG*)(pCont+2)));

                                dwTokRVA =(DWORD)(ullTokRVA - VAL64((DWORD)pOptHeader64->ImageBase));
                            }
                            if(g_pPELoader->getVAforRVA(dwTokRVA,(void**)&pTok))
                                (*g_prEATableRef)[j].tkTok = VAL32(*pTok);

                            (*g_prEATableRef)[j].pszName = NULL;

                        }
                    }
                    if(pExpTable->dwNumNamePtrs && pExpTable->dwNamePtrRVA && pExpTable->dwOrdTableRVA)
                    {
                        DWORD*  pNamePtr = NULL;
                        WORD*   pOrd     = NULL;
                        char*   szName   = NULL;
                        g_pPELoader->getVAforRVA(VAL32(pExpTable->dwNamePtrRVA), (void **) &pNamePtr);
                        g_pPELoader->getVAforRVA(VAL32(pExpTable->dwOrdTableRVA), (void **) &pOrd);
#ifdef _DEBUG
                        sprintf_s(szString,SZSTRING_SIZE,"// Export Names:"); printLine(GUICookie,szString);
#endif
                        for(j=0; j < VAL32(pExpTable->dwNumATEntries); j++,pNamePtr++,pOrd++)
                        {
                            g_pPELoader->getVAforRVA(VAL32(*pNamePtr), (void **) &szName);
#ifdef _DEBUG
                            sprintf_s(szString,SZSTRING_SIZE,"// [%d]: NamePtr=%X Ord=%X Name='%s'",j,VAL32(*pNamePtr),*pOrd,szName);
                            printLine(GUICookie,szString);
#endif
                            (*g_prEATableRef)[VAL16(*pOrd)].pszName = szName;
                        }
                    }
                    g_nEATableBase = pExpTable->dwOrdinalBase;
                    break;
                }
            }
#ifdef _DEBUG
            printLine(GUICookie,COMMENT((char*)-1)); // end multiline comment
#endif
        }
    }
}
// helper to avoid mixing of SEH and stack objects with destructors
void DumpEATEntriesWrapper(void* GUICookie,
    IMAGE_NT_HEADERS32 *pNTHeader32, IMAGE_OPTIONAL_HEADER32 *pOptHeader32,
    IMAGE_NT_HEADERS64 *pNTHeader64, IMAGE_OPTIONAL_HEADER64 *pOptHeader64)
{
    PAL_CPP_TRY
    {
        DumpEATEntries(GUICookie, pNTHeader32, pOptHeader32, pNTHeader64, pOptHeader64);
    }
    PAL_CPP_CATCH_ALL
    {
        printError(GUICookie,"// ERROR READING EXPORT ADDRESS TABLE");
        if (g_prEATableRef != NULL)
        {
            SDELETE(g_prEATableRef);
        }
        g_nEATableRef = 0;
    }
    PAL_CPP_ENDTRY
}

void DumpVtable(void* GUICookie)
{
    // VTable : primary processing
    DWORD  pVTable=0;
    VTableEntry*    pVTE;
    DWORD i,j,k;
    char* szptr;

    IMAGE_NT_HEADERS32 *pNTHeader32 = NULL;
    IMAGE_OPTIONAL_HEADER32 *pOptHeader32 = NULL;

    IMAGE_NT_HEADERS64 *pNTHeader64 = NULL;
    IMAGE_OPTIONAL_HEADER64 *pOptHeader64 = NULL;

    if (g_pPELoader->IsPE32())
    {
        pNTHeader32 = g_pPELoader->ntHeaders32();
        pOptHeader32 = &pNTHeader32->OptionalHeader;

        sprintf_s(szString,SZSTRING_SIZE,"%s%s 0x%08x", g_szAsmCodeIndent,KEYWORD(".imagebase"),VAL32(pOptHeader32->ImageBase));
        printLine(GUICookie,szString);
        j = VAL16(pOptHeader32->Subsystem);
        sprintf_s(szString,SZSTRING_SIZE,"%s%s 0x%08x", g_szAsmCodeIndent,KEYWORD(".file alignment"),VAL32(pOptHeader32->FileAlignment));
        printLine(GUICookie,szString);
        sprintf_s(szString,SZSTRING_SIZE,"%s%s 0x%08x", g_szAsmCodeIndent,KEYWORD(".stackreserve"),VAL32(pOptHeader32->SizeOfStackReserve));
        printLine(GUICookie,szString);
    }
    else
    {
        pNTHeader64 = g_pPELoader->ntHeaders64();
        pOptHeader64 = &pNTHeader64->OptionalHeader;

        sprintf_s(szString,SZSTRING_SIZE,"%s%s 0x%016I64x", g_szAsmCodeIndent,KEYWORD(".imagebase"),VAL64(pOptHeader64->ImageBase));
        printLine(GUICookie,szString);
        j = VAL16(pOptHeader64->Subsystem);
        sprintf_s(szString,SZSTRING_SIZE,"%s%s 0x%08x", g_szAsmCodeIndent,KEYWORD(".file alignment"),VAL32(pOptHeader64->FileAlignment));
        printLine(GUICookie,szString);
        sprintf_s(szString,SZSTRING_SIZE,"%s%s 0x%016I64x", g_szAsmCodeIndent,KEYWORD(".stackreserve"),VAL64(pOptHeader64->SizeOfStackReserve));
        printLine(GUICookie,szString);
    }
    szptr = &szString[0];
    szptr += sprintf_s(szString,SZSTRING_SIZE,"%s%s 0x%04x", g_szAsmCodeIndent,KEYWORD(".subsystem"),j);
    {
        const char* psz[15] = {"// UNKNOWN",
                         "// NATIVE",
                         "// WINDOWS_GUI",
                         "// WINDOWS_CUI",
                         "// <illegal value>",
                         "// OS2_CUI",
                         "// <illegal value>",
                         "// POSIX_CUI",
                         "// NATIVE_WINDOWS",
                         "// WINDOWS_CE_GUI",
                         "// EFI_APPLICATION",
                         "// EFI_BOOT_SERVICE_DRIVER",
                         "// EFI_RUNTIME_DRIVER",
                         "// EFI_ROM",
                         "// XBOX"
                        };
        if(j > 14) j = 4; // <illegal value>
        sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"       %s",COMMENT(psz[j]));
    }
    printLine(GUICookie,szString);

    szptr = &szString[0];
    i = (DWORD)VAL32(g_CORHeader->Flags);
    szptr += sprintf_s(szString,SZSTRING_SIZE,"%s%s 0x%08x", g_szAsmCodeIndent,KEYWORD(".corflags"),i);
    if(i != 0)
    {
        char sz[256], *szp = sz;
        szp += sprintf_s(szp,256,"    // ");
        if(i & COMIMAGE_FLAGS_ILONLY) szp += sprintf_s(szp,256-(szp-sz)," ILONLY");
        if(COR_IS_32BIT_REQUIRED(i))
            szp += sprintf_s(szp,256-(szp-sz)," 32BITREQUIRED");
        if(COR_IS_32BIT_PREFERRED(i))
            szp += sprintf_s(szp,256-(szp-sz)," 32BITPREFERRED");
        if(i & COMIMAGE_FLAGS_IL_LIBRARY) szp += sprintf_s(szp,256-(szp-sz)," IL_LIBRARY");
        if(i & COMIMAGE_FLAGS_TRACKDEBUGDATA) szp += sprintf_s(szp,256-(szp-sz)," TRACKDEBUGDATA");
        szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT(sz));
    }
    printLine(GUICookie,szString);

    sprintf_s(szString,SZSTRING_SIZE,"%s// Image base: 0x%p",g_szAsmCodeIndent,g_pPELoader->base());
    printLine(GUICookie,COMMENT(szString));

    DumpEATEntriesWrapper(GUICookie, pNTHeader32, pOptHeader32, pNTHeader64, pOptHeader64);

    g_nVTableRef = 0;
    if(VAL32(g_CORHeader->VTableFixups.Size))
    {
        IMAGE_SECTION_HEADER *pSecHdr = NULL;
        DWORD dwNumberOfSections;

        if (g_pPELoader->IsPE32())
        {
            pSecHdr = IMAGE_FIRST_SECTION(g_pPELoader->ntHeaders32());
            dwNumberOfSections = VAL16(g_pPELoader->ntHeaders32()->FileHeader.NumberOfSections);
        }
        else
        {
            pSecHdr = IMAGE_FIRST_SECTION(g_pPELoader->ntHeaders64());
            dwNumberOfSections = VAL16(g_pPELoader->ntHeaders64()->FileHeader.NumberOfSections);
        }

        pVTable = VAL32(g_CORHeader->VTableFixups.VirtualAddress);

        for (i=0; i < dwNumberOfSections; i++,pSecHdr++)
        {
            if(((DWORD)pVTable >= VAL32(pSecHdr->VirtualAddress))&&
                ((DWORD)pVTable < VAL32(pSecHdr->VirtualAddress)+VAL32(pSecHdr->Misc.VirtualSize)))
            {
                pVTE = (VTableEntry*)( g_pPELoader->base()
                    + VAL32(pSecHdr->PointerToRawData)
                    + pVTable - VAL32(pSecHdr->VirtualAddress));
                for(j=VAL32(g_CORHeader->VTableFixups.Size),k=0; j > 0; pVTE++, j-=sizeof(VTableEntry),k++)
                {
                    szptr = &szString[0];
                    szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s [%d] ",g_szAsmCodeIndent,KEYWORD(".vtfixup"),VAL16(pVTE->wCount));
                    DWORD dwSize = VAL16(pVTE->wCount) * 4;
                    WORD wType = VAL16(pVTE->wType);
                    if(wType & COR_VTABLE_32BIT)
                        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("int32 "));
                    else if(wType & COR_VTABLE_64BIT)
                    {
                        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("int64 "));
                        dwSize <<= 1;
                    }
                    if(wType & COR_VTABLE_FROM_UNMANAGED)
                        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("fromunmanaged "));
                    if(wType & COR_VTABLE_CALL_MOST_DERIVED)
                        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("callmostderived "));
                    if(wType & 0x8 /*COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN*/)
                        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("retainappdomain "));
                    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("at "));
                    szptr = DumpDataPtr(szptr,VAL32(pVTE->dwAddr), dwSize);
                    // Walk every v-table fixup entry and dump the slots.
                    {
                        BYTE *pSlot;
                        if (g_pPELoader->getVAforRVA(VAL32(pVTE->dwAddr), (void **) &pSlot))
                        {
                            char* szptr0 = szptr;
                            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," //");
                            for (WORD iSlot=0;  iSlot<VAL16(pVTE->wCount);  iSlot++)
                            {
                                mdMethodDef tkMethod = VAL32(*(DWORD *) pSlot);
                                if (VAL16(pVTE->wType) & COR_VTABLE_32BIT)
                                {
                                    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," %08X", VAL32(*(DWORD *)pSlot));
                                    pSlot += sizeof(DWORD);
                                }
                                else
                                {
                                    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," %016I64X", VAL64(*(unsigned __int64 *)pSlot));
                                    pSlot += sizeof(unsigned __int64);
                                }
                                if (g_prVTableRef == NULL)
                                {
                                    g_prVTableRef = new DynamicArray<VTableRef>;
                                }
                                (*g_prVTableRef)[g_nVTableRef].tkTok = tkMethod;
                                (*g_prVTableRef)[g_nVTableRef].wEntry = (WORD)k;
                                (*g_prVTableRef)[g_nVTableRef].wSlot = iSlot;
                                g_nVTableRef++;

                                //ValidateToken(tkMethod, mdtMethodDef);
                            }
                            sprintf_s(szptr0,SZSTRING_REMAINING_SIZE(szptr0),COMMENT(szptr0));
                        }
                        else
                            szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr)," %s",ERRORMSG(RstrUTF(IDS_E_BOGUSRVA)));
                    }
                    printLine(GUICookie,szString);
                }
                break;
            }
        }
    }
}
// MetaInfo integration:
void DumpMI(_In_ __nullterminated const char *str)
{
    static BOOL fInit = TRUE;
    static char* szStr = &szString[0];
    static void* GUICookie;
    char* pch;
    // Reset
    if(str == (char*)-1)
    {
        fInit = TRUE;
        return;
    }
    // Init
    if(fInit)
    {
        strcpy_s(szString,5,"// ");
        fInit = FALSE;
        GUICookie = (void*)str;

        return;
    }
    // Normal work
    strcat_s(szString,SZSTRING_SIZE,str);
    if((pch = strchr(szStr,'\n')))
    {
        *pch = 0;
        printLine(GUICookie,szStr);
        pch++;
        memcpy(&szString[3], pch, strlen(pch)+1);
    }
}

void DumpMetaInfo(_In_ __nullterminated const WCHAR* pwzFileName, _In_opt_z_ const char* pszObjFileName, void* GUICookie)
{
    const WCHAR* pch = wcsrchr(pwzFileName,L'.');

    DumpMI((char*)GUICookie); // initialize the print function for DumpMetaInfo

    if(pch && (!_wcsicmp(pch+1,W("lib")) || !_wcsicmp(pch+1,W("obj"))))
    {   // This works only when all the rest does not
        // Init and run.
        if (MetaDataGetDispenser(CLSID_CorMetaDataDispenser,
            IID_IMetaDataDispenserEx, (void **)&g_pDisp))
                {
                    WCHAR *pwzObjFileName=NULL;
                    if (pszObjFileName)
                    {
                        int nLength = (int) strlen(pszObjFileName)+1;
                        pwzObjFileName = new WCHAR[nLength];
                        memset(pwzObjFileName,0,sizeof(WCHAR)*nLength);
                        WszMultiByteToWideChar(CP_UTF8,0,pszObjFileName,-1,pwzObjFileName,nLength);
                    }
                    DisplayFile((WCHAR*)pwzFileName, true, g_ulMetaInfoFilter, pwzObjFileName, DumpMI);
                    g_pDisp->Release();
                    g_pDisp = NULL;
                    if (pwzObjFileName) VDELETE(pwzObjFileName);
                }
    }
    else
    {
        HRESULT hr = S_OK;
        if(g_pDisp == NULL)
        {
            hr = MetaDataGetDispenser(CLSID_CorMetaDataDispenser,
                IID_IMetaDataDispenserEx, (void **)&g_pDisp);
        }
        if(SUCCEEDED(hr))
        {
            g_ValModuleType = ValidatorModuleTypePE;
            if(g_pAssemblyImport==NULL) g_pAssemblyImport = GetAssemblyImport(NULL);
            printLine(GUICookie,RstrUTF(IDS_E_MISTART));
            //MDInfo metaDataInfo(g_pPubImport, g_pAssemblyImport, (LPCWSTR)pwzFileName, DumpMI, g_ulMetaInfoFilter);
            MDInfo metaDataInfo(g_pDisp,(LPCWSTR)pwzFileName, DumpMI, g_ulMetaInfoFilter);
            metaDataInfo.DisplayMD();
            printLine(GUICookie,RstrUTF(IDS_E_MIEND));
        }
    }
    DumpMI((char*)-1); // reset the print function for DumpMetaInfo
}

void DumpPreamble()
{
    printLine(g_pFile,"");
    if(g_fDumpHTML)
    {
        printLine(g_pFile, "<FONT SIZE=4><B>");
    }
    else if(g_fDumpRTF)
    {
    }
    sprintf_s(szString,SZSTRING_SIZE,"//  Microsoft (R) .NET IL Disassembler.  Version " CLR_PRODUCT_VERSION);
    printLine(g_pFile,COMMENT(szString));
    if(g_fDumpHTML)
    {
        printLine(g_pFile, "</B></FONT>");
    }
    else if(g_fDumpRTF)
    {
    }
    printLine(g_pFile,"");
    if(g_fLimitedVisibility || (!g_fShowCA) || (!g_fDumpAsmCode)
        || (g_Mode & (MODE_DUMP_CLASS | MODE_DUMP_CLASS_METHOD | MODE_DUMP_CLASS_METHOD_SIG)))
    {
        printLine(g_pFile,"");
        printLine(g_pFile,COMMENT(RstrUTF(IDS_E_PARTDASM)));
        printLine(g_pFile,"");
    }

    if(g_fLimitedVisibility)
    {
        strcpy_s(szString, SZSTRING_SIZE, RstrUTF(IDS_E_ONLYITEMS));
        if(!g_fHidePub) strcat_s(szString, SZSTRING_SIZE," Public");
        if(!g_fHidePriv) strcat_s(szString, SZSTRING_SIZE," Private");
        if(!g_fHideFam) strcat_s(szString, SZSTRING_SIZE," Family");
        if(!g_fHideAsm) strcat_s(szString, SZSTRING_SIZE," Assembly");
        if(!g_fHideFAA) strcat_s(szString, SZSTRING_SIZE," FamilyANDAssembly");
        if(!g_fHidePrivScope) strcat_s(szString, SZSTRING_SIZE," PrivateScope");
        printLine(g_pFile,COMMENT(szString));
    }
}

void DumpSummary()
{
    ULONG i;
    const char      *pcClass,*pcNS,*pcMember, *pcSig;
    char szFQN[4096];
    HENUMInternal   hEnum;
    mdToken tkMember;
    CQuickBytes     qbMemberSig;
    PCCOR_SIGNATURE pComSig;
    ULONG           cComSig;
    DWORD dwAttrs;
    mdToken tkEventType;

    printLine(g_pFile,"//============ S U M M A R Y =================================");
    if (SUCCEEDED(g_pImport->EnumGlobalFunctionsInit(&hEnum)))
    {
        while(g_pImport->EnumNext(&hEnum, &tkMember))
        {
            if (FAILED(g_pImport->GetNameOfMethodDef(tkMember, &pcMember)) ||
                FAILED(g_pImport->GetSigOfMethodDef(tkMember, &cComSig, &pComSig)))
            {
                sprintf_s(szString, SZSTRING_SIZE, "// ERROR in the method record %08X", tkMember);
                printLine(g_pFile, szString);
                continue;
            }
            qbMemberSig.Shrink(0);
            pcSig = cComSig ? PrettyPrintSig(pComSig, cComSig, "", &qbMemberSig, g_pImport,NULL) : "NO SIGNATURE";
            PREFIX_ASSUME(ProperName((char*)pcMember) != 0);
            sprintf_s(szString,SZSTRING_SIZE,"// %08X [GLM] %s : %s", tkMember,ProperName((char*)pcMember),pcSig);
            printLine(g_pFile,szString);
        }
    }
    g_pImport->EnumClose(&hEnum);
    if (SUCCEEDED(g_pImport->EnumGlobalFieldsInit(&hEnum)))
    {
        while(g_pImport->EnumNext(&hEnum, &tkMember))
        {
            if (FAILED(g_pImport->GetNameOfFieldDef(tkMember, &pcMember)) ||
                FAILED(g_pImport->GetSigOfFieldDef(tkMember, &cComSig, &pComSig)))
            {
                sprintf_s(szString, SZSTRING_SIZE, "// ERROR in the field record %08X", tkMember);
                printLine(g_pFile, szString);
                continue;
            }
            qbMemberSig.Shrink(0);
            pcSig = cComSig ? PrettyPrintSig(pComSig, cComSig, "", &qbMemberSig, g_pImport,NULL) : "NO SIGNATURE";
            PREFIX_ASSUME(ProperName((char*)pcMember) != 0);
            sprintf_s(szString,SZSTRING_SIZE,"// %08X [GLF] %s : %s", tkMember,ProperName((char*)pcMember),pcSig);
            printLine(g_pFile,szString);
        }
    }
    g_pImport->EnumClose(&hEnum);

    for (i = 0; i < g_NumClasses; i++)
    {
        if (FAILED(g_pImport->GetNameOfTypeDef(g_cl_list[i], &pcClass, &pcNS)))
        {
            sprintf_s(szString, SZSTRING_SIZE, "// ERROR in the TypeDef record %08X", g_cl_list[i]);
            printLine(g_pFile, szString);
            continue;
        }
        PREFIX_ASSUME(ProperName((char*)pcClass) != 0);
        if(*pcNS) sprintf_s(szFQN,4096,"%s.%s", ProperName((char*)pcNS),ProperName((char*)pcClass));
        else strcpy_s(szFQN,4096,ProperName((char*)pcClass));
        sprintf_s(szString,SZSTRING_SIZE,"// %08X [CLS] %s", g_cl_list[i],szFQN);
        printLine(g_pFile,szString);
        if(SUCCEEDED(g_pImport->EnumInit(mdtMethodDef, g_cl_list[i], &hEnum)))
        {
            while(g_pImport->EnumNext(&hEnum, &tkMember))
            {
                if (FAILED(g_pImport->GetNameOfMethodDef(tkMember, &pcMember)) ||
                    FAILED(g_pImport->GetSigOfMethodDef(tkMember, &cComSig, &pComSig)))
                {
                    sprintf_s(szString, SZSTRING_SIZE, "// ERROR in the method record %08X", tkMember);
                    printLine(g_pFile, szString);
                    continue;
                }
                qbMemberSig.Shrink(0);
                pcSig = cComSig ? PrettyPrintSig(pComSig, cComSig, "", &qbMemberSig, g_pImport,NULL) : "NO SIGNATURE";
                PREFIX_ASSUME(ProperName((char*)pcMember) != 0);
                sprintf_s(szString,SZSTRING_SIZE,"// %08X [MET] %s::%s : %s", tkMember,szFQN,ProperName((char*)pcMember),pcSig);
                printLine(g_pFile,szString);
            }
        }
        g_pImport->EnumClose(&hEnum);
        if(SUCCEEDED(g_pImport->EnumInit(mdtFieldDef, g_cl_list[i], &hEnum)))
        {
            while(g_pImport->EnumNext(&hEnum, &tkMember))
            {
                if (FAILED(g_pImport->GetNameOfFieldDef(tkMember, &pcMember)) ||
                    FAILED(g_pImport->GetSigOfFieldDef(tkMember, &cComSig, &pComSig)))
                {
                    sprintf_s(szString, SZSTRING_SIZE, "// ERROR in the field record %08X", tkMember);
                    printLine(g_pFile, szString);
                    continue;
                }
                qbMemberSig.Shrink(0);
                pcSig = cComSig ? PrettyPrintSig(pComSig, cComSig, "", &qbMemberSig, g_pImport,NULL) : "NO SIGNATURE";
                PREFIX_ASSUME(ProperName((char*)pcMember) != 0);
                sprintf_s(szString,SZSTRING_SIZE,"// %08X [FLD] %s::%s : %s", tkMember,szFQN,ProperName((char*)pcMember),pcSig);
                printLine(g_pFile,szString);
            }
        }
        g_pImport->EnumClose(&hEnum);
        if(SUCCEEDED(g_pImport->EnumInit(mdtEvent, g_cl_list[i], &hEnum)))
        {
            while(g_pImport->EnumNext(&hEnum, &tkMember))
            {
                if (FAILED(g_pImport->GetEventProps(tkMember,&pcMember,&dwAttrs,&tkEventType)))
                {
                    sprintf_s(szString, SZSTRING_SIZE, "// Invalid Event %08X record", tkMember);
                    printLine(g_pFile, szString);
                    continue;
                }
                qbMemberSig.Shrink(0);
                pcSig = "NO TYPE";
                if(RidFromToken(tkEventType))
                {
                        switch(TypeFromToken(tkEventType))
                        {
                                case mdtTypeRef:
                                case mdtTypeDef:
                                case mdtTypeSpec:
                                        pcSig = PrettyPrintClass(&qbMemberSig,tkEventType,g_pImport);
                                    break;
                                default:
                                    break;
                        }
                }
                PREFIX_ASSUME(ProperName((char*)pcMember) != 0);
                sprintf_s(szString,SZSTRING_SIZE,"// %08X [EVT] %s::%s : %s", tkMember,szFQN,ProperName((char*)pcMember),pcSig);
                printLine(g_pFile,szString);
            }
        }
        g_pImport->EnumClose(&hEnum);
        if(SUCCEEDED(g_pImport->EnumInit(mdtProperty, g_cl_list[i], &hEnum)))
        {
            while(g_pImport->EnumNext(&hEnum, &tkMember))
            {
                if (FAILED(g_pImport->GetPropertyProps(tkMember,&pcMember,&dwAttrs,&pComSig,&cComSig)))
                {
                    sprintf_s(szString, SZSTRING_SIZE, "// Invalid Property %08X record", tkMember);
                    printLine(g_pFile, szString);
                    continue;
                }
                qbMemberSig.Shrink(0);
                pcSig = cComSig ? PrettyPrintSig(pComSig, cComSig, "", &qbMemberSig, g_pImport,NULL) : "NO SIGNATURE";
                PREFIX_ASSUME(ProperName((char*)pcMember) != 0);
                sprintf_s(szString,SZSTRING_SIZE,"// %08X [PRO] %s::%s : %s", tkMember,szFQN,ProperName((char*)pcMember),pcSig);
                printLine(g_pFile,szString);
            }
        }
        g_pImport->EnumClose(&hEnum);
    }
    printLine(g_pFile,"//=============== END SUMMARY ==================================");
}
void DumpRTFPrefix(void* GUICookie,BOOL fFontDefault)
{
    g_fDumpRTF = FALSE;
    printLine(GUICookie,"{\\rtf1\\ansi");
    if(fFontDefault)
        printLine(GUICookie,"{\\fonttbl{\\f0\\fmodern\\fprq1\\fcharset1 Courier New;}{\\f1\\fswiss\\fcharset1 Arial;}}");
    printLine(GUICookie,"{\\colortbl ;\\red0\\green0\\blue128;\\red0\\green128\\blue0;\\red255\\green0\\blue0;}");
    printLine(GUICookie,"\\viewkind4\\uc1\\pard\\f0\\fs20");
    g_fDumpRTF = TRUE;
}
void DumpRTFPostfix(void* GUICookie)
{
    g_fDumpRTF = FALSE;
    printLine(GUICookie,"}");
    g_fDumpRTF = TRUE;
}
mdToken ClassOf(mdToken tok)
{
    mdToken retval=0;
    switch(TypeFromToken(tok))
    {
        case  mdtTypeDef:
        case mdtTypeRef:
        case mdtTypeSpec:
            retval = tok;
            break;

        case mdtFieldDef:
        case mdtMethodDef:
        case mdtMemberRef:
            if (FAILED(g_pImport->GetParentToken(tok, &retval)))
            {
                retval = mdTokenNil;
            }
            else
            {
                retval = ClassOf(retval);
            }
            break;

        default:
            break;
    }
    return retval;
}
void DumpRefs(BOOL fClassesOnly)
{
    CQuickBytes out;
    DynamicArray<TokPair>    *refs = g_refs;
    TokPair                 *newrefs = NULL;
    mdToken tkThisUser,tkThisRef;
    mdToken tkLastUser = 0xFFFFFFFF, tkLastRef=0xFFFFFFFF;
    DWORD i=0,j=0;

    g_refs = NULL;
    printLine(g_pFile,COMMENT((char*)0));
    printLine(g_pFile,"//============ R E F E R E N C E S ===========================");
    strcpy_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"//    ");
    if(fClassesOnly && g_NumRefs)
    {
        if((newrefs = new TokPair[g_NumRefs]))
        {
            for(i=0; i<g_NumRefs; i++)
            {
                newrefs[i].tkUser = tkThisUser = ClassOf((*refs)[i].tkUser);
                newrefs[i].tkRef  = tkThisRef  = ClassOf((*refs)[i].tkRef);
                if(!tkThisUser) continue;
                if(!tkThisRef) continue;
                if(tkThisUser == tkThisRef) continue;
                for(j = 0; j<i; j++)
                {
                    if((newrefs[j].tkUser==tkThisUser)&&(newrefs[j].tkRef==tkThisRef))
                    {
                        newrefs[i].tkRef = 0;
                        break;
                    }
                }
            }
        }
        else fClassesOnly = FALSE;
    }
    for(i = 0; i <g_NumRefs; i++)
    {
        if(fClassesOnly)
        {
            tkThisUser = newrefs[i].tkUser;
            tkThisRef  = newrefs[i].tkRef;
        }
        else
        {
            tkThisUser = (*refs)[i].tkUser;
            tkThisRef  = (*refs)[i].tkRef;
        }
        if(!tkThisUser) continue;
        if(!tkThisRef) continue;
        if(tkThisUser == tkThisRef) continue;
        if((tkThisUser==tkLastUser)&&(tkThisRef==tkLastRef)) continue;

        strcpy_s(szString, SZSTRING_SIZE,g_szAsmCodeIndent);
        if(tkThisUser != tkLastUser)
        {
            PrettyPrintToken(szString, tkThisUser, g_pImport,g_pFile,0); //TypeDef,TypeRef,TypeSpec,MethodDef,FieldDef,MemberRef,MethodSpec,String
            strcat_s(szString, SZSTRING_SIZE, "   references   ");
            printLine(g_pFile,szString);
            tkLastUser = tkThisUser;
        }
        strcpy_s(szString, SZSTRING_SIZE,g_szAsmCodeIndent);
        strcat_s(szString, SZSTRING_SIZE,"          -   ");
        PrettyPrintToken(szString, tkThisRef, g_pImport,g_pFile,0); //TypeDef,TypeRef,TypeSpec,MethodDef,FieldDef,MemberRef,MethodSpec,String
        printLine(g_pFile,szString);
        tkLastRef = tkThisRef;

    }

    printLine(g_pFile,"//=============== END REFERENCES =============================");
    printLine(g_pFile,COMMENT((char*)-1));
    g_refs = refs;
    if(newrefs) VDELETE(newrefs);
}

void CloseNamespace(__inout __nullterminated char* szString)
{
    if(strlen(g_szNamespace))
    {
        char* szptr = &szString[0];
        if(g_szAsmCodeIndent[0]) g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s%s ",g_szAsmCodeIndent, UNSCOPE());
        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("// end of namespace %s"),ProperName(g_szNamespace));
        printLine(g_pFile,szString);
        printLine(g_pFile,"");
        g_szNamespace[0] = 0;
    }
}

FILE* OpenOutput(_In_ __nullterminated const WCHAR* wzFileName)
{
    FILE*   pfile = NULL;
        if(g_uCodePage == 0xFFFFFFFF) _wfopen_s(&pfile,wzFileName,W("wb"));
        else _wfopen_s(&pfile,wzFileName,W("wt"));

    if(pfile)
    {
        if(g_uCodePage == CP_UTF8) fwrite("\357\273\277",3,1,pfile);
        else if(g_uCodePage == 0xFFFFFFFF) fwrite("\377\376",2,1,pfile);
    }
    return pfile;
}

FILE* OpenOutput(_In_ __nullterminated const char* szFileName)
{
    return OpenOutput(UtfToUnicode(szFileName));
}

//
// Init PELoader, dump file header info
//
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
BOOL DumpFile()
{
    BOOL        fSuccess = FALSE;
    static WCHAR       wzInputFileName[MAX_FILENAME_LENGTH];
    static char     szFilenameANSI[MAX_FILENAME_LENGTH*3];
    IMetaDataDispenser *pMetaDataDispenser = NULL;
    const char *pszFilename = g_szInputFile;
    const DWORD openFlags = ofRead | (g_fProject ? 0 : ofNoTransform);

    {
        if(g_fDumpHTML)
        {
            printLine(g_pFile, "<HTML>");
            printLine(g_pFile, "<HEAD>");
            sprintf_s(szString,SZSTRING_SIZE,"<TITLE> %s - IL DASM</TITLE>",g_szInputFile);
            printLine(g_pFile, szString);
            printLine(g_pFile, "</HEAD>");
            printLine(g_pFile, "<BODY>");
            printLine(g_pFile, "<FONT SIZE=3 FACE=\"Arial\">");
            printLine(g_pFile, "<PRE>");
        }
        else if(g_fDumpRTF)
        {
            DumpRTFPrefix(g_pFile,TRUE);
        }
        DumpPreamble();
    }
    {
        char* pch = strrchr(g_szInputFile,'.');
        if(pch && (!_stricmp(pch+1,"lib") || !_stricmp(pch+1,"obj")))
        {
            DumpMetaInfo(g_wszFullInputFile,g_pszObjFileName,g_pFile);
            return FALSE;
        }
    }

    if(g_pPELoader) goto DoneInitialization; // skip initialization, it's already done

    g_pPELoader = new PELoader();
    if (g_pPELoader == NULL)
    {
        printError(g_pFile,RstrUTF(IDS_E_INITLDR));
        goto exit;
    }

    memset(wzInputFileName,0,sizeof(WCHAR)*MAX_FILENAME_LENGTH);
    WszMultiByteToWideChar(CP_UTF8,0,pszFilename,-1,wzInputFileName,MAX_FILENAME_LENGTH);
    memset(szFilenameANSI,0,MAX_FILENAME_LENGTH*3);
    WszWideCharToMultiByte(g_uConsoleCP,0,wzInputFileName,-1,szFilenameANSI,MAX_FILENAME_LENGTH*3,NULL,NULL);
        fSuccess = g_pPELoader->open(wzInputFileName);

    if (fSuccess == FALSE)
    {
        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_FILEOPEN), pszFilename);
        printError(g_pFile,szString);
        SDELETE(g_pPELoader);
        g_pPELoader = NULL;
        goto exit;
    }
    fSuccess = FALSE;

    if (g_pPELoader->getCOMHeader(&g_CORHeader) == FALSE)
    {
        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_NOCORHDR), pszFilename);
        printError(g_pFile,szString);
        if (g_fDumpHeader)
            DumpHeader(g_CORHeader,g_pFile);
        goto exit;
    }

    if (VAL16(g_CORHeader->MajorRuntimeVersion) == 1 || VAL16(g_CORHeader->MajorRuntimeVersion) > COR_VERSION_MAJOR)
    {
        sprintf_s(szString,SZSTRING_SIZE,"CORHeader->MajorRuntimeVersion = %d",VAL16(g_CORHeader->MajorRuntimeVersion));
        printError(g_pFile,szString);
        printError(g_pFile,RstrUTF(IDS_E_BADCORHDR));
        goto exit;
    }
    g_tkEntryPoint = VAL32(IMAGE_COR20_HEADER_FIELD(*g_CORHeader, EntryPointToken)); // integration with MetaInfo


    {
    if (g_pPELoader->getVAforRVA(VAL32(g_CORHeader->MetaData.VirtualAddress),&g_pMetaData) == FALSE)
    {
        printError(g_pFile, RstrUTF(IDS_E_OPENMD));
        if (g_fDumpHeader)
            DumpHeader(g_CORHeader, g_pFile);
        goto exit;
    }
        g_cbMetaData = VAL32(g_CORHeader->MetaData.Size);
    }

    if (FAILED(GetMetaDataInternalInterface(
        (BYTE *)g_pMetaData,
        g_cbMetaData,
        openFlags,
        IID_IMDInternalImport,
        (LPVOID *)&g_pImport)))
    {
        if (g_fDumpHeader)
            DumpHeader(g_CORHeader, g_pFile);
        printError(g_pFile, RstrUTF(IDS_E_OPENMD));
        goto exit;
    }

    TokenSigInit(g_pImport);
    if (FAILED(MetaDataGetDispenser(CLSID_CorMetaDataDispenser, IID_IMetaDataDispenser, (LPVOID*)&pMetaDataDispenser)))
    {
        if (g_fDumpHeader)
            DumpHeader(g_CORHeader, g_pFile);
        printError(g_pFile, RstrUTF(IDS_E_OPENMD));
        goto exit;
    }
    if (FAILED(pMetaDataDispenser->OpenScopeOnMemory(g_pMetaData, g_cbMetaData, openFlags, IID_IMetaDataImport2, (LPUNKNOWN *)&g_pPubImport )))
    {
        if (g_fDumpHeader)
            DumpHeader(g_CORHeader, g_pFile);
        printError(g_pFile, RstrUTF(IDS_E_OPENMD));
        goto exit;
    }

    if((g_uNCA = g_pImport->GetCountWithTokenKind(mdtCustomAttribute)))
    {
        g_rchCA = new char[g_uNCA+1];
        _ASSERTE(g_rchCA);
    }

    EnumClasses();
    EnumTypedefs();

DoneInitialization:
    if(g_uNCA)
    {
        _ASSERTE(g_rchCA);
        memset(g_rchCA,0,g_uNCA+1);
    }

    {
        // Dump the CLR header info if requested.
        printLine(g_pFile,COMMENT((char*)0)); // start multiline comment
        if (g_fDumpHeader)
        {
            DumpHeader(g_CORHeader,g_pFile);
            DumpHeaderDetails(g_CORHeader,g_pFile);
        }
        else
            DumpVTables(g_CORHeader,g_pFile);
        if (g_fDumpStats)
            DumpStatistics(g_CORHeader,g_pFile);

        if(g_fDumpClassList) PrintClassList();
        // MetaInfo integration:
        if(g_fDumpMetaInfo) DumpMetaInfo(g_wszFullInputFile,NULL,g_pFile);

        if(g_fDumpSummary) DumpSummary();
        printLine(g_pFile,COMMENT((char*)-1)); // end multiline comment

        if(g_fShowRefs) g_refs = new DynamicArray<TokPair>;

        if (g_fDumpAsmCode)
        {
            g_szNamespace[0] = 0;
            if(g_tkClassToDump) //g_tkClassToDump is set in EnumClasses
            {
                DumpClass(TopEncloser(g_tkClassToDump), VAL32(IMAGE_COR20_HEADER_FIELD(*g_CORHeader, EntryPointToken)),g_pFile,7); //7-dump everything at once
                CloseNamespace(szString);
                goto ReportAndExit;
            }
            {
                HENUMInternal   hEnumMethod;
                ULONG           ulNumGlobalFunc=0;
                if (SUCCEEDED(g_pImport->EnumGlobalFunctionsInit(&hEnumMethod)))
                {
                    ulNumGlobalFunc = g_pImport->EnumGetCount(&hEnumMethod);
                    g_pImport->EnumClose(&hEnumMethod);
                }

            }
            //DumpVtable(g_pFile);
            DumpMscorlib(g_pFile);
            if(g_fDumpTypeList) DumpTypelist(g_pFile);
            DumpManifest(g_pFile);
            DumpTypedefs(g_pFile);
            /* First dump the classes w/o members*/
            if(g_fForwardDecl && g_NumClasses)
            {
                printLine(g_pFile,COMMENT("//"));
                printLine(g_pFile,COMMENT("// ============== CLASS STRUCTURE DECLARATION =================="));
                printLine(g_pFile,COMMENT("//"));
                for (DWORD i = 0; i < g_NumClasses; i++)
                {
                    if(g_cl_enclosing[i] == mdTypeDefNil) // nested classes are dumped within enclosing ones
                    {
                        DumpClass(g_cl_list[i], VAL32(IMAGE_COR20_HEADER_FIELD(*g_CORHeader, EntryPointToken)),g_pFile,2); // 2=header+nested classes
                    }
                }
                CloseNamespace(szString);
                printLine(g_pFile,"");
                printLine(g_pFile,COMMENT("// ============================================================="));
                printLine(g_pFile,"");
            }
            /* Second, dump the global fields and methods */
            DumpGlobalFields();
            DumpGlobalMethods(VAL32(IMAGE_COR20_HEADER_FIELD(*g_CORHeader, EntryPointToken)));
            /* Third, dump the classes with members */
            if(g_NumClasses)
            {
                printLine(g_pFile,"");
                printLine(g_pFile,COMMENT("// =============== CLASS MEMBERS DECLARATION ==================="));
                if(g_fForwardDecl)
                {
                    printLine(g_pFile,COMMENT("//   note that class flags, 'extends' and 'implements' clauses"));
                    printLine(g_pFile,COMMENT("//          are provided here for information only"));
                }
                printLine(g_pFile,"");
                for (DWORD i = 0; i < g_NumClasses; i++)
                {
                    if(g_cl_enclosing[i] == mdTypeDefNil) // nested classes are dumped within enclosing ones
                    {
                        DumpClass(g_cl_list[i], VAL32(IMAGE_COR20_HEADER_FIELD(*g_CORHeader, EntryPointToken)),g_pFile,7); //7=everything
                    }
                }
                CloseNamespace(szString);
                printLine(g_pFile,"");
                printLine(g_pFile,COMMENT("// ============================================================="));
                printLine(g_pFile,"");
            }
            if(g_fShowCA)
            {
                if(g_uNCA)  _ASSERTE(g_rchCA);
                for(DWORD i=1; i<= g_uNCA; i++)
                {
                    if(g_rchCA[i] == 0) DumpCustomAttribute(TokenFromRid(i,mdtCustomAttribute),g_pFile,true);
                }
            }


            // If there were "ldptr", dump the .rdata section with labels
            if(g_iPtrCount)
            {
                //first, sort the pointers
                int i,j;
                bool swapped;
                do {
                    swapped = FALSE;

                    for(i = 1; i < g_iPtrCount; i++)
                    {
                        if((*g_pPtrTags)[i-1] > (*g_pPtrTags)[i])
                        {
                            j = (*g_pPtrTags)[i-1];
                            (*g_pPtrTags)[i-1] = (*g_pPtrTags)[i];
                            (*g_pPtrTags)[i] = j;
                            j = (*g_pPtrSize)[i-1];
                            (*g_pPtrSize)[i-1] = (*g_pPtrSize)[i];
                            (*g_pPtrSize)[i] = j;
                            swapped = TRUE;
                        }
                    }
                } while(swapped);

                //second, dump data for each ptr as binarray

                IMAGE_SECTION_HEADER *pSecHdr = NULL;
                if(g_pPELoader->IsPE32())
                    pSecHdr = IMAGE_FIRST_SECTION(g_pPELoader->ntHeaders32());
                else
                    pSecHdr = IMAGE_FIRST_SECTION(g_pPELoader->ntHeaders64());

                DWORD dwNumberOfSections;
                if(g_pPELoader->IsPE32())
                    dwNumberOfSections = VAL16(g_pPELoader->ntHeaders32()->FileHeader.NumberOfSections);
                else
                    dwNumberOfSections = VAL16(g_pPELoader->ntHeaders64()->FileHeader.NumberOfSections);

                DWORD fromPtr,toPtr,limPtr;
                char* szptr;
                for(j = 0; j < g_iPtrCount; j++)
                {
                    BYTE *pb;

                    fromPtr = (*g_pPtrTags)[j];
                    for (i=0; i < (int)dwNumberOfSections; i++,pSecHdr++)
                    {
                        if((fromPtr >= VAL32(pSecHdr->VirtualAddress))&&
                            (fromPtr < VAL32(pSecHdr->VirtualAddress)+VAL32(pSecHdr->Misc.VirtualSize))) break;
                    }
                    if(i == (int)dwNumberOfSections)
                    {
                        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_ROGUEPTR), fromPtr);
                        printLine(g_pFile,szString);
                        break;
                    }
                    // OK, now we have the section; what about end of BLOB?
                    const char* szTls = "D_";
                    if(strcmp((char*)(pSecHdr->Name),".tls")==0) szTls = "T_";
                    else if(strcmp((char*)(pSecHdr->Name),".text")==0) szTls = "I_";
                    if(j == g_iPtrCount-1)
                    {
                        toPtr = VAL32(pSecHdr->VirtualAddress)+VAL32(pSecHdr->Misc.VirtualSize);
                    }
                    else
                    {
                        toPtr = (*g_pPtrTags)[j+1];
                        if(toPtr > VAL32(pSecHdr->VirtualAddress)+VAL32(pSecHdr->Misc.VirtualSize))
                        {
                            toPtr = VAL32(pSecHdr->VirtualAddress)+VAL32(pSecHdr->Misc.VirtualSize);
                        }
                    }
                    if(toPtr - fromPtr > (*g_pPtrSize)[j]) toPtr = fromPtr + (*g_pPtrSize)[j];
                    limPtr = toPtr; // at limPtr and after, pad with 0
                    if(limPtr > VAL32(pSecHdr->VirtualAddress)+VAL32(pSecHdr->SizeOfRawData))
                        limPtr = VAL32(pSecHdr->VirtualAddress)+VAL32(pSecHdr->SizeOfRawData);
                PrintBlob:
                    szptr = szString;
                    szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".data"));
                    if(*szTls=='T') szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("tls "));
                    else if(*szTls=='I') szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("cil "));
                    if(fromPtr >= limPtr)
                    {   // uninitialized data
                        sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s%8.8X = %s[%d]",szTls,fromPtr,KEYWORD("int8"),toPtr-fromPtr);
                        printLine(g_pFile,szString);
                    }
                    else
                    {   // initialized data
                        szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s%8.8X = %s (",szTls,fromPtr,KEYWORD("bytearray"));
                        printLine(g_pFile,szString);
                        szptr = szString;
                        szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s                ",g_szAsmCodeIndent);
                        pb =  g_pPELoader->base()
                                + VAL32(pSecHdr->PointerToRawData)
                                + fromPtr - VAL32(pSecHdr->VirtualAddress);
                        // now fromPtr is the beginning of the BLOB, and toPtr is [exclusive] end of it
                        DumpHexbytes(szptr, pb, fromPtr, toPtr, limPtr);
                    }
                    // to preserve alignment, dump filler if any
                    if(limPtr == toPtr) // don't need filler if it's the last item in section
                    {
                        if((j < g_iPtrCount-1)&&(toPtr < (DWORD)((*g_pPtrTags)[j+1])))
                        {
                            DWORD align;
                            DWORD stptr = (DWORD)(*g_pPtrTags)[j+1];
                            for(align = 1; (align & stptr)==0; align = align << 1);
                            align -= 1;
                            if(toPtr & align)
                            {
                                fromPtr = toPtr;
                                toPtr = (toPtr + align)&~align;
                                goto PrintBlob;
                            }
                        }
                    }
                }
            }
ReportAndExit:
            printLine(g_pFile,COMMENT(RstrUTF(IDS_E_DASMOK)));
            fSuccess = TRUE;
        }
        fSuccess = TRUE;
#ifndef TARGET_UNIX
        if(g_pFile) // dump .RES file (if any), if not to console
        {
            WCHAR wzResFileName[2048], *pwc;
            memset(wzResFileName,0,sizeof(wzResFileName));
            WszMultiByteToWideChar(CP_UTF8,0,g_szOutputFile,-1,wzResFileName,2048);
            pwc = wcsrchr(wzResFileName,L'.');
            if(pwc == NULL) pwc = &wzResFileName[wcslen(wzResFileName)];
            wcscpy_s(pwc, 2048 - (pwc - wzResFileName), L".res");
            DWORD ret = DumpResourceToFile(wzResFileName);
            switch(ret)
            {
                case 0: szString[0] = 0; break;
                case 1: sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_W_CREATEDW32RES)/*"// WARNING: Created Win32 resource file %ls"*/,
                                UnicodeToUtf(wzResFileName)); break;
                case 0xDFFFFFFF: sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_CORRUPTW32RES)/*"// ERROR: Corrupt Win32 resources"*/); break;
                case 0xEFFFFFFF: sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_CANTOPENW32RES)/*"// ERROR: Unable to open file %ls"*/,
                                         UnicodeToUtf(wzResFileName)); break;
                case 0xFFFFFFFF: sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_CANTACCESSW32RES)/*"// ERROR: Unable to access Win32 resources"*/); break;
            }
            if(szString[0])
            {
                if(ret == 1) printLine(g_pFile,COMMENT(szString));
                else printError(g_pFile,szString);
            }
        }
#endif
        if(g_fShowRefs) DumpRefs(TRUE);
        if(g_fDumpHTML)
        {
            printLine(g_pFile, "</PRE>");
            printLine(g_pFile, "</BODY>");
            printLine(g_pFile, "</HTML>");
        }
        else if(g_fDumpRTF)
        {
            DumpRTFPostfix(g_pFile);
        }

        if(g_pFile)
        {
            fclose(g_pFile);
            g_pFile = NULL;
        }
    }

exit:
    if (pMetaDataDispenser)
        pMetaDataDispenser->Release();
    return fSuccess;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#ifdef _MSC_VER
#pragma warning(default : 4640)
#endif
