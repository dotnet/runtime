// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Assembly and Manifest Disassembler
//
#include "ildasmpch.h"

#include "debugmacros.h"
#include "corpriv.h"
#include "dasmenum.hpp"
#include "formattype.h"
#include "dis.h"

#include "ceeload.h"
#include "dynamicarray.h"
#include "resource.h"

#include "clrinternal.h"

#ifndef MAX_LOCALE_NAME
#define MAX_LOCALE_NAME (32)
#endif

extern IMAGE_COR20_HEADER *    g_CORHeader;
extern IMDInternalImport*      g_pImport;
extern PELoader * g_pPELoader;
extern IMetaDataImport2*        g_pPubImport;
extern void *                  g_pMetaData;
IMetaDataAssemblyImport*    g_pAssemblyImport=NULL;
extern BOOL     g_fDumpAsmCode;
extern char     g_szAsmCodeIndent[];
extern char     g_szOutputFile[];
extern BOOL     g_fDumpTokens;
extern DWORD    g_Mode;
extern FILE*    g_pFile;
extern LPCSTR*  rAsmRefName;  // decl. in formatType.cpp -- for AsmRef aliases
extern ULONG    ulNumAsmRefs; // decl. in formatType.cpp -- for AsmRef aliases
extern unsigned g_uConsoleCP;
MTokName*   rFile = NULL;
ULONG   nFiles = 0;
void DumpFiles(void* GUICookie)
{
    static mdFile      rFileTok[4096];
    HCORENUM    hEnum=NULL;
    if(rFile) { VDELETE(rFile); nFiles = 0; }
    if(SUCCEEDED(g_pAssemblyImport->EnumFiles(&hEnum,rFileTok,4096,&nFiles)))
    {
        if(nFiles)
        {
            static WCHAR       wzName[1024];
            ULONG       ulNameLen;
            const void* pHashValue;
            ULONG       cbHashValue;
            DWORD       dwFlags;
            char*       szptr;
            rFile = new MTokName[nFiles];
            for(ULONG ix = 0; ix < nFiles; ix++)
            {
                pHashValue=NULL;
                cbHashValue=0;
                ulNameLen=0;
                if(SUCCEEDED(g_pAssemblyImport->GetFileProps(rFileTok[ix],wzName,1024,&ulNameLen,
                                                            &pHashValue,&cbHashValue,&dwFlags)))
                {
                    szptr = &szString[0];
                    rFile[ix].tok = rFileTok[ix];
                    rFile[ix].name = new WCHAR[ulNameLen+1];
                    memcpy(rFile[ix].name,wzName,ulNameLen*sizeof(WCHAR));
                    rFile[ix].name[ulNameLen] = 0;

                    szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),"%s%s ",g_szAsmCodeIndent,KEYWORD(".file"));
                    if(g_fDumpTokens) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),rFileTok[ix]);
                    if(IsFfContainsNoMetaData(dwFlags)) szptr += sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),KEYWORD("nometadata "));
                    {
                        int L = ulNameLen*3+3;
                        char* sz = new char[L];
                        memset(sz,0,L);
                        WszWideCharToMultiByte(CP_UTF8,0,rFile[ix].name,-1,sz,L,NULL,NULL);
                        strcpy_s(szptr,SZSTRING_REMAINING_SIZE(szptr), ANCHORPT(ProperName(sz),rFileTok[ix]));
                        VDELETE(sz);
                    }
                    printLine(GUICookie,szString);
                    if(VAL32(IMAGE_COR20_HEADER_FIELD(*g_CORHeader, EntryPointToken)) == rFileTok[ix])
                    {
                        printLine(GUICookie, KEYWORD("    .entrypoint"));
                    }
                    if(pHashValue && cbHashValue)
                    {
                        sprintf_s(szString,SZSTRING_SIZE,"    %s = (",KEYWORD(".hash"));
                        DumpByteArray(szString,(BYTE*)pHashValue,cbHashValue,GUICookie);
                        printLine(GUICookie,szString);
                    }
                    DumpCustomAttributes(rFile[ix].tok, GUICookie);
                }
            }
        }
        g_pAssemblyImport->CloseEnum(hEnum);
    }
    else nFiles=0;
}

void DumpAssemblyMetaData(ASSEMBLYMETADATA* pmd, void* GUICookie)
{
    if(pmd)
    {
        sprintf_s(szString,SZSTRING_SIZE,"%s%s %d:%d:%d:%d",g_szAsmCodeIndent,KEYWORD(".ver"),pmd->usMajorVersion,
                pmd->usMinorVersion,pmd->usBuildNumber,pmd->usRevisionNumber);
        printLine(GUICookie,szString);
        if(pmd->szLocale && pmd->cbLocale)
        {
            sprintf_s(szString,SZSTRING_SIZE,"%s%s = (",g_szAsmCodeIndent,KEYWORD(".locale"));
            DumpByteArray(szString,(BYTE*)(pmd->szLocale),pmd->cbLocale*sizeof(WCHAR),GUICookie);
            printLine(GUICookie,szString);
        }
    }
}
void DumpScope(void* GUICookie)
{
    mdModule mdm;
    GUID mvid;
    WCHAR scopeName[1024];
    CHAR guidString[GUID_STR_BUFFER_LEN];
    memset(scopeName,0,1024*sizeof(WCHAR));
    if(SUCCEEDED(g_pPubImport->GetScopeProps( scopeName, 1024, NULL, &mvid))&& scopeName[0])
    {
        {
            UINT32 L = (UINT32)u16_strlen(scopeName)*3+3;
            char* sz = new char[L];
            memset(sz,0,L);
            WszWideCharToMultiByte(CP_UTF8,0,scopeName,-1,sz,L,NULL,NULL);
            sprintf_s(szString,SZSTRING_SIZE,"%s%s %s",g_szAsmCodeIndent,KEYWORD(".module"),ProperName(sz));
            VDELETE(sz);
        }
        printLine(GUICookie,szString);
        GuidToLPSTR(mvid, guidString);
        sprintf_s(szString,SZSTRING_SIZE,COMMENT("%s// MVID: %s"),g_szAsmCodeIndent,guidString);

        printLine(GUICookie,szString);
        if(SUCCEEDED(g_pPubImport->GetModuleFromScope(&mdm)))
        {
            DumpCustomAttributes(mdm, GUICookie);
            DumpPermissions(mdm, GUICookie);
        }
    }
}

void DumpModuleRefs(void *GUICookie)
{
    HCORENUM        hEnum=NULL;
    ULONG           N;
    static mdToken         tk[4096];
    char*           szptr;
    LPCSTR          szName;

    g_pPubImport->EnumModuleRefs(&hEnum,tk,4096,&N);
    for(ULONG i = 0; i < N; i++)
    {
        if(RidFromToken(tk[i]))
        {
            if (FAILED(g_pImport->GetModuleRefProps(tk[i],&szName)))
            {
                continue;
            }
            if (*szName != 0) // ignore the no-name ModuleRef: it's an IJW artifact
            {
                szptr = &szString[0];
                szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s %s",g_szAsmCodeIndent,KEYWORD(".module extern"),
                               ANCHORPT(ProperName((char*)szName),tk[i]));
                if(g_fDumpTokens) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT(" /*%08X*/"),tk[i]);
                printLine(GUICookie,szString);
                DumpCustomAttributes(tk[i], GUICookie);
                DumpPermissions(tk[i], GUICookie);
            }
        }
    }
    g_pPubImport->CloseEnum(hEnum);
}

void DumpAssembly(void* GUICookie, BOOL fFullDump)
{
    mdAssembly  tkAsm;
    if(SUCCEEDED(g_pAssemblyImport->GetAssemblyFromScope(&tkAsm))&&(tkAsm != mdAssemblyNil))
    {
        const void* pPublicKey;
        ULONG       cbPublicKey = 0;
        ULONG       ulHashAlgId;
        WCHAR       wzName[1024];
        ULONG       ulNameLen=0;
        ASSEMBLYMETADATA    md;
        WCHAR       wzLocale[MAX_LOCALE_NAME];
        DWORD       dwFlags;
        char*       szptr;

        md.szLocale = wzLocale;
        md.cbLocale = MAX_LOCALE_NAME;
        md.rProcessor = NULL;
        md.ulProcessor = 0;
        md.rOS = NULL;
        md.ulOS = 0;

        if(SUCCEEDED(g_pAssemblyImport->GetAssemblyProps(            // S_OK or error.
                                                        tkAsm,       // [IN] The Assembly for which to get the properties.
                                                        &pPublicKey, // [OUT] Pointer to the public key.
                                                        &cbPublicKey,// [OUT] Count of bytes in the public key.
                                                        &ulHashAlgId,// [OUT] Hash Algorithm.
                                                        wzName,      // [OUT] Buffer to fill with name.
                                                        1024,        // [IN] Size of buffer in wide chars.
                                                        &ulNameLen,  // [OUT] Actual # of wide chars in name.
                                                        &md,         // [OUT] Assembly MetaData.
                                                        &dwFlags)))  // [OUT] Flags.
        {
            if(ulNameLen >= 1024)
            {
                strcpy_s(szString,SZSTRING_SIZE,RstrUTF(IDS_ASSEMNAMETOOLONG));
                printError(GUICookie,szString);
                ulNameLen = 1023;
            }
            szptr = &szString[0];
            szptr+=sprintf_s(szptr,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".assembly"));
            if(g_fDumpTokens) szptr+=sprintf_s(szptr,SZSTRING_REMAINING_SIZE(szptr),COMMENT("/*%08X*/ "),tkAsm);

            if(IsAfRetargetable(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("retargetable "));
            if(IsAfContentType_WindowsRuntime(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("windowsruntime "));
            if(IsAfPA_NoPlatform(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("noplatform "));
            if(IsAfPA_MSIL(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("cil "));
            if(IsAfPA_x86(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("x86 "));
            if(IsAfPA_AMD64(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("amd64 "));
            if(IsAfPA_ARM(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("arm "));
            if(IsAfPA_ARM64(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("arm64 "));


            wzName[ulNameLen] = 0;
            {
                char* sz = new char[3*ulNameLen+3];
                memset(sz,0,3*ulNameLen+3);
                WszWideCharToMultiByte(CP_UTF8,0,wzName,-1,sz,3*ulNameLen+3,NULL,NULL);
                strcat_s(szString,SZSTRING_SIZE,ANCHORPT(ProperName(sz),tkAsm));
                VDELETE(sz);
            }
            printLine(GUICookie,szString);
            sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,SCOPE());
            printLine(GUICookie,szString);
            strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"  ");
            if(fFullDump)
            {
                DumpCustomAttributes(tkAsm, GUICookie);
                DumpPermissions(tkAsm, GUICookie);
            }

            if(fFullDump)
            {
                if(pPublicKey && cbPublicKey)
                {
                    sprintf_s(szString,SZSTRING_SIZE,"%s%s = (",g_szAsmCodeIndent,KEYWORD(".publickey"));
                    DumpByteArray(szString,(BYTE*)pPublicKey,cbPublicKey,GUICookie);
                    printLine(GUICookie,szString);
                }
                if(ulHashAlgId)
                {
                    sprintf_s(szString,SZSTRING_SIZE,"%s%s 0x%08X",g_szAsmCodeIndent,KEYWORD(".hash algorithm"),ulHashAlgId);
                    printLine(GUICookie,szString);
                }
            }
            DumpAssemblyMetaData(&md,GUICookie);
            g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
            sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,UNSCOPE());
            printLine(GUICookie,szString);
        } //end if(OK(GetAssemblyProps))
    } //end if(OK(GetAssemblyFromScope))
}

MTokName*   rAsmRef = NULL;
ULONG   nAsmRefs = 0;

void DumpAssemblyRefs(void* GUICookie)
{
    static mdAssemblyRef       rAsmRefTok[4096];
    HCORENUM    hEnum=NULL;
    ULONG ix = 0;
    if(rAsmRef) { VDELETE(rAsmRef); nAsmRefs = 0; }
    if(rAsmRefName)
    {
        for(ix=0; ix < ulNumAsmRefs; ix++)
        {
            if(rAsmRefName[ix]) VDELETE(rAsmRefName[ix]);
        }
        VDELETE(rAsmRefName);
        rAsmRefName = NULL;
        ulNumAsmRefs = 0;
    }
    if(SUCCEEDED(g_pAssemblyImport->EnumAssemblyRefs(&hEnum,rAsmRefTok,4096,&nAsmRefs)))
    {
        if(nAsmRefs)
        {
            const void* pPublicKeyOrToken;
            ULONG       cbPublicKeyOrToken = 0;
            const void* pHashValue;
            ULONG       cbHashValue;
            static WCHAR       wzName[1024];
            ULONG       ulNameLen=0;
            ASSEMBLYMETADATA    md;
            WCHAR       wzLocale[MAX_LOCALE_NAME];
            DWORD       dwFlags;

            rAsmRef = new MTokName[nAsmRefs];
            rAsmRefName = new LPCSTR[nAsmRefs];
            ulNumAsmRefs = nAsmRefs;

            for(ix = 0; ix < nAsmRefs; ix++)
            {
                md.szLocale = wzLocale;
                md.cbLocale = MAX_LOCALE_NAME;
                md.rProcessor = NULL;
                md.ulProcessor = 0;
                md.rOS = NULL;
                md.ulOS = 0;

                ulNameLen=cbHashValue=0;
                pHashValue = NULL;
                if(SUCCEEDED(g_pAssemblyImport->GetAssemblyRefProps(            // S_OK or error.
                                                                rAsmRefTok[ix], // [IN] The Assembly for which to get the properties.
                                                                &pPublicKeyOrToken, // [OUT] Pointer to the public key or token.
                                                                &cbPublicKeyOrToken,// [OUT] Count of bytes in the public key or token.
                                                                wzName,      // [OUT] Buffer to fill with name.
                                                                1024,        // [IN] Size of buffer in wide chars.
                                                                &ulNameLen,  // [OUT] Actual # of wide chars in name.
                                                                &md,         // [OUT] Assembly MetaData.
                                                                &pHashValue, // [OUT] Hash blob.
                                                                &cbHashValue,// [OUT] Count of bytes in the hash blob.
                                                                &dwFlags)))  // [OUT] Flags.
                {
                    ULONG ixx;
                    rAsmRef[ix].tok = rAsmRefTok[ix];
                    rAsmRef[ix].name = new WCHAR[ulNameLen+16];
                    memcpy(rAsmRef[ix].name,wzName,ulNameLen*sizeof(WCHAR));
                    rAsmRef[ix].name[ulNameLen] = 0;
                    if(ulNameLen >= 1024)
                    {
                        strcpy_s(szString,SZSTRING_SIZE,RstrUTF(IDS_ASMREFNAMETOOLONG));
                        printError(GUICookie,szString);
                        wzName[1023] = 0;
                    }

                    sprintf_s(szString,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".assembly extern"));
                    if(g_fDumpTokens) sprintf_s(&szString[strlen(szString)],SZSTRING_SIZE-strlen(szString),COMMENT("/*%08X*/ "),rAsmRefTok[ix]);

                    if(IsAfRetargetable(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("retargetable "));
                    if(IsAfContentType_WindowsRuntime(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("windowsruntime "));
                    if(IsAfPA_MSIL(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("cil "));
                    if(IsAfPA_x86(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("x86 "));
                    if(IsAfPA_AMD64(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("amd64 "));
                    if(IsAfPA_ARM(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("arm "));
                    if(IsAfPA_ARM64(dwFlags)) strcat_s(szString,SZSTRING_SIZE,KEYWORD("arm64 "));

                    {
                        char* sz = new char[3*ulNameLen+32];
                        memset(sz,0,3*ulNameLen+3);
                        WszWideCharToMultiByte(CP_UTF8,0,rAsmRef[ix].name,-1,sz,3*ulNameLen+3,NULL,NULL);
                        // check for name duplication and introduce alias if needed
                        for(ixx = 0; ixx < ix; ixx++)
                        {
                            if(!u16_strcmp(rAsmRef[ixx].name,rAsmRef[ix].name)) break;
                        }
                        if(ixx < ix)
                        {
                            strcat_s(szString,SZSTRING_SIZE, ProperName(sz));
                            char* pc=&szString[strlen(szString)];
                            sprintf_s(&sz[strlen(sz)],3*ulNameLen+32-strlen(sz),"_%d",ix);
                            sprintf_s(pc,SZSTRING_REMAINING_SIZE(pc)," %s %s", KEYWORD("as"),ANCHORPT(ProperName(sz),rAsmRefTok[ix]));
                        }
                        else
                            strcat_s(szString,SZSTRING_SIZE, ANCHORPT(ProperName(sz),rAsmRefTok[ix]));
                        rAsmRefName[ix] = sz;
                    }
                    printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,SCOPE());
                    printLine(GUICookie,szString);
                    strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"  ");
                    DumpCustomAttributes(rAsmRefTok[ix], GUICookie);
                    if(pPublicKeyOrToken && cbPublicKeyOrToken)
                    {
                        if (IsAfPublicKey(dwFlags))
                            sprintf_s(szString,SZSTRING_SIZE,"%s%s = (",g_szAsmCodeIndent,KEYWORD(".publickey"));
                        else
                            sprintf_s(szString,SZSTRING_SIZE,"%s%s = (",g_szAsmCodeIndent,KEYWORD(".publickeytoken"));
                        DumpByteArray(szString,(BYTE*)pPublicKeyOrToken,cbPublicKeyOrToken,GUICookie);
                        printLine(GUICookie,szString);
                    }
                    if(pHashValue && cbHashValue)
                    {
                        sprintf_s(szString,SZSTRING_SIZE,"%s%s = (",g_szAsmCodeIndent,KEYWORD(".hash"));
                        DumpByteArray(szString,(BYTE*)pHashValue,cbHashValue,GUICookie);
                        printLine(GUICookie,szString);
                    }
                    DumpAssemblyMetaData(&md,GUICookie);
                    g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
                    sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,UNSCOPE());
                    printLine(GUICookie,szString);
                } //end if(OK(GetAssemblyRefProps))
            }//end for(all assembly refs)
        }//end if(nAsmRefs
        g_pAssemblyImport->CloseEnum(hEnum);
    }//end if OK(EnumAssemblyRefs)
    else nAsmRefs=0;
}

DynamicArray<LocalComTypeDescr*>    *g_pLocalComType = NULL;
ULONG   g_LocalComTypeNum = 0;

void DumpComTypeFQN(
    LocalComTypeDescr*              pCTD,
    __inout __nullterminated char*  szName)
{
    if(TypeFromToken(pCTD->tkImplementation) == mdtExportedType)
    {
        ULONG i;
        for(i = 0; (i < g_LocalComTypeNum) && ((*g_pLocalComType)[i]->tkComTypeTok != pCTD->tkImplementation); i++);
        if(i < g_LocalComTypeNum)
        {
            DumpComTypeFQN((*g_pLocalComType)[i], szName);
            strcat_s(szName, SZSTRING_SIZE, "/");
        }
    }

    UINT32 L = (UINT32)u16_strlen(pCTD->wzName)*3+3;
    char* sz = new char[L];
    memset(sz,0,L);
    WszWideCharToMultiByte(CP_UTF8,0,pCTD->wzName,-1,sz,L,NULL,NULL);
    strcat_s(szName, SZSTRING_SIZE, JUMPPT(ProperName(sz), pCTD->tkComTypeTok));
    VDELETE(sz);
}

void DumpImplementation(mdToken tkImplementation,
                        DWORD dwOffset,
                        __inout __nullterminated char* szString,
                        void* GUICookie)
{
    ULONG i;
    char* pc;
    if(RidFromToken(tkImplementation))
    {
        if(TypeFromToken(tkImplementation) == mdtFile)
        {
            for(i=0; (i < nFiles)&&(rFile[i].tok != tkImplementation); i++);
            if(i < nFiles)
            {
                {
                    UINT32 L = (UINT32)u16_strlen(rFile[i].name)*3+3;
                    char* sz = new char[L];
                    memset(sz,0,L);
                    WszWideCharToMultiByte(CP_UTF8,0,rFile[i].name,-1,sz,L,NULL,NULL);
                    sprintf_s(szString,SZSTRING_SIZE,"%s%s %s",g_szAsmCodeIndent,KEYWORD(".file"),
                            JUMPPT(ProperName(sz),tkImplementation));
                    VDELETE(sz);
                }
                pc=&szString[strlen(szString)];
                if(g_fDumpTokens) pc+=sprintf_s(pc,SZSTRING_REMAINING_SIZE(pc),COMMENT("/*%08X*/ "),tkImplementation);
                if(dwOffset != 0xFFFFFFFF) sprintf_s(pc,SZSTRING_REMAINING_SIZE(pc)," %s 0x%08X",KEYWORD("at"),dwOffset);
                printLine(GUICookie,szString);
            }
        }
        else if(TypeFromToken(tkImplementation) == mdtAssemblyRef)
        {
            for(i=0; (i < nAsmRefs)&&(rAsmRef[i].tok != tkImplementation); i++);
            if(i < nAsmRefs)
            {
                {
                    UINT32 L = (UINT32)u16_strlen(rAsmRef[i].name)*3+3;
                    char* sz = new char[L];
                    memset(sz,0,L);
                    WszWideCharToMultiByte(CP_UTF8,0,rAsmRef[i].name,-1,sz,L,NULL,NULL);
                    sprintf_s(szString,SZSTRING_SIZE,"%s%s %s",g_szAsmCodeIndent,KEYWORD(".assembly extern"),
                            JUMPPT(ProperName(sz),tkImplementation));
                    VDELETE(sz);
                }
                pc=&szString[strlen(szString)];
                if(g_fDumpTokens) sprintf_s(pc,SZSTRING_REMAINING_SIZE(pc),COMMENT(" /*%08X*/ "),tkImplementation);
                printLine(GUICookie,szString);
            }
        }
        else if(TypeFromToken(tkImplementation) == mdtExportedType)
        {
            // Find the type structure corresponding to this token
            for(i=0; (i < g_LocalComTypeNum)&&((*g_pLocalComType)[i]->tkComTypeTok != tkImplementation); i++);
            if(i < g_LocalComTypeNum)
            {
                sprintf_s(szString,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".class extern"));
                DumpComTypeFQN((*g_pLocalComType)[i], szString);

                pc=&szString[strlen(szString)];
                if(g_fDumpTokens) sprintf_s(pc,SZSTRING_REMAINING_SIZE(pc),COMMENT(" /*%08X*/ "),tkImplementation);
                printLine(GUICookie,szString);
            }
        }
    }
}

void DumpComType(LocalComTypeDescr* pCTD,
                 __inout __nullterminated char* szString,
                 void* GUICookie)
{
    if(g_fDumpTokens) sprintf_s(&szString[strlen(szString)],SZSTRING_SIZE-strlen(szString),COMMENT("/*%08X*/ "),pCTD->tkComTypeTok);
    if (IsTdPublic(pCTD->dwFlags))                   strcat_s(szString,SZSTRING_SIZE,KEYWORD("public "));
    if (IsTdForwarder(pCTD->dwFlags))                strcat_s(szString,SZSTRING_SIZE,KEYWORD("forwarder "));
    if (IsTdNestedPublic(pCTD->dwFlags))             strcat_s(szString,SZSTRING_SIZE,KEYWORD("nested public "));
    if (IsTdNestedPrivate(pCTD->dwFlags))            strcat_s(szString,SZSTRING_SIZE,KEYWORD("nested private "));
    if (IsTdNestedFamily(pCTD->dwFlags))             strcat_s(szString,SZSTRING_SIZE,KEYWORD("nested family "));
    if (IsTdNestedAssembly(pCTD->dwFlags))           strcat_s(szString,SZSTRING_SIZE,KEYWORD("nested assembly "));
    if (IsTdNestedFamANDAssem(pCTD->dwFlags))        strcat_s(szString,SZSTRING_SIZE,KEYWORD("nested famandassem "));
    if (IsTdNestedFamORAssem(pCTD->dwFlags))         strcat_s(szString,SZSTRING_SIZE,KEYWORD("nested famorassem "));

    char* pc=&szString[strlen(szString)];
    {
        UINT32 L = (UINT32)u16_strlen(pCTD->wzName)*3+3;
        char* sz = new char[L];
        memset(sz,0,L);
        WszWideCharToMultiByte(CP_UTF8,0,pCTD->wzName,-1,sz,L,NULL,NULL);
        strcpy_s(pc,SZSTRING_REMAINING_SIZE(pc),ANCHORPT(ProperName(sz),pCTD->tkComTypeTok));
        VDELETE(sz);
    }
    printLine(GUICookie,szString);

    sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,SCOPE());
    printLine(GUICookie,szString);
    strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"  ");
    DumpCustomAttributes(pCTD->tkComTypeTok, GUICookie);
    DumpImplementation(pCTD->tkImplementation,0xFFFFFFFF,szString,GUICookie);
    if(RidFromToken(pCTD->tkTypeDef))
    {
        sprintf_s(szString,SZSTRING_SIZE,"%s%s 0x%08X",g_szAsmCodeIndent,KEYWORD(".class"),pCTD->tkTypeDef);
        printLine(GUICookie,szString);
    }
    g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
    sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,UNSCOPE());
    printLine(GUICookie,szString);
}


void DumpComTypes(void* GUICookie)
{
    static mdExportedType  rComTypeTok[4096];
    ULONG           nComTypes;
    HCORENUM    hEnum=NULL;

    g_LocalComTypeNum = 0;
    if(SUCCEEDED(g_pAssemblyImport->EnumExportedTypes(&hEnum,rComTypeTok,4096,&nComTypes)))
    {
        if(nComTypes)
        {
            static WCHAR       wzName[1024];
            ULONG       ulNameLen=0;
            DWORD       dwFlags;
            mdToken     tkImplementation;
            mdTypeDef   tkTypeDef;

            ULONG ix;
            for(ix = 0; ix < nComTypes; ix++)
            {
                ulNameLen=0;
                if(SUCCEEDED(g_pAssemblyImport->GetExportedTypeProps(                    // S_OK or error.
                                                                rComTypeTok[ix],    // [IN] The ComType for which to get the properties.
                                                                wzName,             // [OUT] Buffer to fill with name.
                                                                1024,               // [IN] Size of buffer in wide chars.
                                                                &ulNameLen,         // [OUT] Actual # of wide chars in name.
                                                                &tkImplementation,  // [OUT] mdFile or mdAssemblyRef that provides the ComType.
                                                                &tkTypeDef,         // [OUT] TypeDef token within the file.
                                                                &dwFlags)))         // [OUT] Flags.
                {
                    LocalComTypeDescr* pCTD = new LocalComTypeDescr(rComTypeTok[ix], tkTypeDef, tkImplementation, new WCHAR[ulNameLen+1], dwFlags);
                    memcpy(pCTD->wzName,wzName,ulNameLen*sizeof(WCHAR));
                    pCTD->wzName[ulNameLen] = 0;

                    if (g_pLocalComType == NULL)
                    {
                        g_pLocalComType = new DynamicArray<LocalComTypeDescr*>;
                    }

                    (*g_pLocalComType)[g_LocalComTypeNum] = pCTD;
                    g_LocalComTypeNum++;
                } //end if(OK(GetComTypeProps))
            }//end for(all com types)

            // now, print all "external" com types
            for(ix = 0; ix < nComTypes; ix++)
            {
                tkImplementation = (*g_pLocalComType)[ix]->tkImplementation;
                // ComType of a nested class has its nester's ComType as implementation
                while(TypeFromToken(tkImplementation)==mdtExportedType)
                {
                    unsigned k;
                    for(k=0; k<g_LocalComTypeNum; k++)
                    {
                        if((*g_pLocalComType)[k]->tkComTypeTok == tkImplementation)
                        {
                            tkImplementation = (*g_pLocalComType)[k]->tkImplementation;
                            break;
                        }
                    }
                    if(k==g_LocalComTypeNum) break;
                }
                // At this moment, tkImplementation is impl.of top nester
                if(RidFromToken(tkImplementation))
                {
                    sprintf_s(szString,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".class extern"));
                    DumpComType((*g_pLocalComType)[ix],szString,GUICookie);
                    (*g_pLocalComType)[ix]->tkTypeDef = 0;
                }
            }
        }//end if(nComTypes)
        g_pAssemblyImport->CloseEnum(hEnum);
    }//end if OK(EnumComTypes)
    else nComTypes=0;
}

// Replaces invalid characters and neutralizes reserved file names.
// Returns TRUE if the string was modified, FALSE otherwise.
static BOOL ConvertToLegalFileNameInPlace(__inout LPWSTR wzName)
{
    BOOL fAlias = FALSE;

    // neutralize reserved names
    static const WCHAR * const rwzReserved[] =
    {
        W("COM"), W("LPT"), // '1' - '9' must follow after these
        W("CON"), W("PRN"), W("AUX"), W("NUL")
    };

    for (size_t i = 0; i < (sizeof(rwzReserved) / sizeof(WCHAR *)); i++)
    {
        _ASSERTE(u16_strlen(rwzReserved[i]) == 3);
        if (_wcsnicmp(wzName, rwzReserved[i], 3) == 0)
        {
            LPWSTR pwc = wzName + 3;

            if (i <= 1) // COM, LPT
            {
                if (*pwc >= L'1' && *pwc <= L'9')
                {
                    // skip the digit
                    pwc++;
                }
                else continue;
            }

            // check for . or end of string
            if (*pwc == L'.' || *pwc == 0)
            {
                *wzName = L'_';
                fAlias = TRUE;
                break;
            }
        }
    }

    // replace invalid characters
    for (;; wzName++)
    {
        WCHAR wch = *wzName;

        if (wch > 0 && wch < 32)
        {
            *wzName = '~';
            fAlias = TRUE;
        }
        else
        {
            switch (wch)
            {
#define REPLACE_CH(oldCh, newCh) \
    case oldCh: *wzName = newCh; \
                fAlias = TRUE;   \
                break;

                REPLACE_CH(L':',  L'!')
                REPLACE_CH(L'\\', L'$')
                REPLACE_CH(L',',  L'&') // not necessary but keeping for back compat
                REPLACE_CH(L';',  L'@') // not necessary but keeping for back compat
                REPLACE_CH(L'<',  L'(')
                REPLACE_CH(L'>',  L')')
                REPLACE_CH(L'"',  L'`')
                REPLACE_CH(L'/',  L'_')
                REPLACE_CH(L'|',  L'-')
                REPLACE_CH(L'*',  L'+') // disallowed wildcard
                REPLACE_CH(L'?',  L'=') // disallowed wildcard

                case 0: return fAlias;
#undef REPLACE_CH
            }
        }
    }
}

// Dumps managed resource at pRes + dwOffset to a file.
static void DumpResourceFile(void *GUICookie, BYTE *pRes, DWORD dwOffset, LPCWSTR wzName,
                             LPCWSTR wzFileName, LPCUTF8 sz)
{
    struct Param
    {
        BYTE *pRes;
        DWORD dwOffset;
        LPCUTF8 sz;
        void *GUICookie;
        const WCHAR *wzFileName;
    } param;
    param.pRes = pRes;
    param.dwOffset = dwOffset;
    param.sz = sz;
    param.GUICookie = GUICookie;
    param.wzFileName = wzFileName;

    PAL_TRY(Param *, pParam, &param)
    {
       DWORD L;
       memcpy(&L,pParam->pRes+pParam->dwOffset,sizeof(DWORD));
       sprintf_s(szString,SZSTRING_SIZE,COMMENT("%s// Offset: 0x%8.8X Length: 0x%8.8X"), g_szAsmCodeIndent,pParam->dwOffset,L);
       printLine(pParam->GUICookie,szString);
        if (g_pFile != NULL) // embedded resource -- dump as .resources file
        {
            FILE *pF = NULL;
            _wfopen_s(&pF, pParam->wzFileName, W("wb"));
            if (pF)
            {
                struct Param
                {
                    BYTE *pRes;
                    DWORD dwOffset;
                    DWORD L;
                    FILE *pF;
                    LPCUTF8 sz;
                    void *GUICookie;
                } param;
                param.pRes = pParam->pRes;
                param.dwOffset = pParam->dwOffset;
                param.L = L;
                param.pF = pF;
                param.sz = pParam->sz;
                param.GUICookie = pParam->GUICookie;

                PAL_TRY(Param *, pParam, &param) {
                    fwrite((pParam->pRes+pParam->dwOffset+sizeof(DWORD)),pParam->L,1,pParam->pF);
                    sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_W_CREATEDMRES),g_szAsmCodeIndent,ProperName(pParam->sz));
                    printLine(pParam->GUICookie,COMMENT(szString));
                }
                PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
                {
                    sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_READINGMRES),g_szAsmCodeIndent,ProperName(pParam->sz),pParam->dwOffset);
                    printError(pParam->GUICookie,szString);
                }
                PAL_ENDTRY
                fclose(pF);
            }
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        sprintf_s(szString, SZSTRING_SIZE,
                  "ERROR retrieving/saving embedded managed resource '%s' at offset 0x%8.8X",
                  UnicodeToUtf(wzName), dwOffset);
        printError(GUICookie, szString);
    }
    PAL_ENDTRY
}

void DumpManifestResources(void* GUICookie)
{
    static mdManifestResource      rManResTok[4096];
    ULONG           nManRes = 0;
    HCORENUM    hEnum=NULL;
    BYTE*       pRes = NULL;
    if(SUCCEEDED(g_pAssemblyImport->EnumManifestResources(&hEnum,rManResTok,4096,&nManRes)))
    {
        if(nManRes)
        {
            WCHAR*      wzName = NULL;
            ULONG       ulNameLen=0;
            DWORD       dwFlags;
            static char        sz[4096];
            mdToken     tkImplementation;
            DWORD       dwOffset;

            static WCHAR wzFileName[2048];

            WszMultiByteToWideChar(CP_UTF8,0,g_szOutputFile,-1,wzFileName,2048);
            wzName = (WCHAR*)u16_strrchr(wzFileName,DIRECTORY_SEPARATOR_CHAR_W);
#ifdef HOST_WINDOWS
            if(wzName == NULL) wzName = (WCHAR*)u16_strrchr(wzFileName,':');
#endif
            if (wzName == NULL) wzName = wzFileName;
            else wzName++;

            // remember the file names created so far to avoid duplicates
            CQuickArray<CQuickWSTRBase> qbNameArray;
            if (!qbNameArray.AllocNoThrow(nManRes + 2))
            {
                sprintf_s(szString, SZSTRING_SIZE,
                          "ERROR retrieving/saving embedded managed resource '%s'", UnicodeToUtf(wzName));
                printError(GUICookie, szString);
                return;
            }

#define NAME_ARRAY_ADD(index, str)                                                    \
            {                                                                         \
                size_t __dwBufLen = u16_strlen(str) + 1;                                  \
                                                                                      \
                qbNameArray[index].Init();                                            \
                WCHAR *__wpc = (WCHAR *)qbNameArray[index].AllocNoThrow(__dwBufLen);  \
                if (__wpc) wcscpy_s(__wpc, __dwBufLen, str);                          \
            }

            // add the output file name to avoid conflict between the IL file and a resource file
            NAME_ARRAY_ADD(0, wzName);

            // add the Win32 resource file name to avoid conflict between the native and a managed resource file
            WCHAR *pwc = (WCHAR*)u16_strrchr(wzName, L'.');
            if (pwc == NULL) pwc = &wzName[u16_strlen(wzName)];
            wcscpy_s(pwc, 2048 - (pwc - wzFileName), W(".res"));

            NAME_ARRAY_ADD(1, wzName);

            for(ULONG ix = 0; ix < nManRes; ix++)
            {
                ulNameLen=0;
                if(SUCCEEDED(g_pAssemblyImport->GetManifestResourceProps(           // S_OK or error.
                                                                rManResTok[ix],     // [IN] The ManifestResource for which to get the properties.
                                                                wzName,             // [OUT] Buffer to fill with name.
                                                                1024,               // [IN] Size of buffer in wide chars.
                                                                &ulNameLen,         // [OUT] Actual # of wide chars in name.
                                                                &tkImplementation,  // [OUT] mdFile or mdAssemblyRef that provides the ComType.
                                                                &dwOffset,          // [OUT] Offset to the beginning of the resource within the file.
                                                                &dwFlags)))         // [OUT] Flags.
                {
                    sprintf_s(szString,SZSTRING_SIZE,"%s%s ",g_szAsmCodeIndent,KEYWORD(".mresource"));
                    if(g_fDumpTokens) sprintf_s(&szString[strlen(szString)],SZSTRING_SIZE-strlen(szString),COMMENT("/*%08X*/ "),rManResTok[ix]);
                    if(IsMrPublic(dwFlags))     strcat_s(szString,SZSTRING_SIZE,KEYWORD("public "));
                    if(IsMrPrivate(dwFlags))    strcat_s(szString,SZSTRING_SIZE,KEYWORD("private "));

                    char* pc = szString + strlen(szString);
                    wzName[ulNameLen]=0;

                    WszWideCharToMultiByte(CP_UTF8,0,wzName,-1,sz,sizeof(sz),NULL,NULL);
                    strcpy_s(pc,SZSTRING_REMAINING_SIZE(pc),ProperName(sz));

                    // get rid of invalid characters and reserved names
                    BOOL fAlias = ConvertToLegalFileNameInPlace(wzName);

                    // check for duplicate file name
                    WCHAR *wpc = wzName + u16_strlen(wzName);
                    for (int iIndex = 1;; iIndex++)
                    {
                        BOOL fConflict = FALSE;
                        if (*wzName == 0)
                        {
                            // resource with an empty name
                            fConflict = TRUE;
                        }
                        else
                        {
                            for (ULONG i = 0; i < (ix + 2); i++)
                            {
                                WCHAR *wzPreviousName = (WCHAR *)qbNameArray[i].Ptr();
                                if (wzPreviousName && _wcsicmp(wzName, wzPreviousName) == 0)
                                {
                                    // resource with the same name as another resource
                                    // or with the same name as the output IL/RES file
                                    fConflict = TRUE;
                                    break;
                                }
                            }
                        }

                        // if we have a conflict, add a number suffix to the file name
                        if (!fConflict)
                        {
                            // no conflict
                            break;
                        }

                        WCHAR* next = FormatInteger(wpc, 2048 - (wpc - wzFileName), "%d", iIndex);
                        if (next == wpc)
                        {
                            // Failed to append index
                            break;
                        }

                        // try again with this new number suffix
                        fAlias = TRUE;
                    }

                    // add this unique file name to the list
                    NAME_ARRAY_ADD(ix + 2, wzName);

                    if(fAlias)
                    {
                        // update sz with the aliased name and print the 'as' keyword
                        if (WszWideCharToMultiByte(CP_UTF8, 0, wzName, -1, sz, sizeof(sz), NULL, NULL) == 0)
                        {
                            sz[sizeof(sz) - 1] = 0;
                        }

                        pc=&szString[strlen(szString)];
                        sprintf_s(pc,SZSTRING_REMAINING_SIZE(pc)," %s %s",KEYWORD("as"),ProperName(sz));
                    }

                    printLine(GUICookie,szString);
                    sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,SCOPE());
                    printLine(GUICookie,szString);
                    strcat_s(g_szAsmCodeIndent,MAX_MEMBER_LENGTH,"  ");
                    DumpCustomAttributes(rManResTok[ix], GUICookie);

                    if(tkImplementation == mdFileNil) // embedded resource -- dump as .resources file
                    {
                        if(pRes == NULL)
                        {
                            // get the resource VA
                            if (g_pPELoader->getVAforRVA((DWORD) VAL32(g_CORHeader->Resources.VirtualAddress), (void **) &pRes) == FALSE)
                            {
                                printError(GUICookie,RstrUTF(IDS_E_IMPORTDATA));
                            }
                        }
                        if(pRes)
                        {
                            DumpResourceFile(GUICookie, pRes, dwOffset, wzName, wzFileName, sz);
                        }
                    }
                    else DumpImplementation(tkImplementation,dwOffset,szString,GUICookie);
                    g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-2] = 0;
                    sprintf_s(szString,SZSTRING_SIZE,"%s%s",g_szAsmCodeIndent,UNSCOPE());
                    printLine(GUICookie,szString);
                } //end if(OK(GetManifestResourceProps))
            }//end for(all manifest resources)

#undef NAME_ARRAY_ADD

        }//end if(nManRes)
        g_pAssemblyImport->CloseEnum(hEnum);
    }//end if OK(EnumManifestResources)
    else nManRes=0;
}

IMetaDataAssemblyImport* GetAssemblyImport(void* GUICookie)
{
    struct Param
    {
        void*                    GUICookie;
        IMetaDataAssemblyImport* pAssemblyImport;
        IMDInternalImport*       pImport;
    mdToken                 tkManifest;
    } param;
    param.GUICookie = GUICookie;
    param.pAssemblyImport = NULL;
    param.pImport = NULL;

    HRESULT                 hr;

    hr=g_pPubImport->QueryInterface(IID_IMetaDataAssemblyImport, (void**) &param.pAssemblyImport);
    if(SUCCEEDED(hr))
    {
        static mdAssemblyRef       rAsmRefTok[4096];
        HCORENUM    hEnum=NULL;
        ULONG   nAsmRefs = 0;
        if(SUCCEEDED(param.pAssemblyImport->GetAssemblyFromScope(&param.tkManifest))) return param.pAssemblyImport;
        if(SUCCEEDED(param.pAssemblyImport->EnumAssemblyRefs(&hEnum,rAsmRefTok,4096,&nAsmRefs)))
        {
            param.pAssemblyImport->CloseEnum(hEnum);
            if(nAsmRefs) return param.pAssemblyImport;
        }
        param.pAssemblyImport->Release();
    }
    else
    {
        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_MDAIMPORT),hr);
        printLine(GUICookie,COMMENT(szString));
    }
    param.pAssemblyImport = NULL;
    // OK, let's do it hard way: check if the manifest is hidden somewhere else
    PAL_TRY(Param *, pParam, &param)
    {
        if(g_CORHeader->Resources.Size)
        {
            DWORD*  pdwSize = NULL;
            BYTE*   pbManifest = NULL;
            HRESULT hr;

            pbManifest = (BYTE*)(g_pPELoader->base() + (DWORD)VAL32(g_CORHeader->Resources.VirtualAddress));
            pdwSize = (DWORD*)pbManifest;
            if(pdwSize && *pdwSize)
            {
                pbManifest += sizeof(DWORD);
                if (SUCCEEDED(hr = GetMetaDataInternalInterface(
                    pbManifest,
                    VAL32(*pdwSize),
                    ofRead,
                    IID_IMDInternalImport,
                    (LPVOID *)&pParam->pImport)))
                {
                    if (FAILED(hr = GetMetaDataPublicInterfaceFromInternal(
                        pParam->pImport,
                        IID_IMetaDataAssemblyImport,
                        (LPVOID *)&pParam->pAssemblyImport)))
                    {
                        sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_MDAFROMMDI),hr);
                        printLine(pParam->GUICookie,COMMENT(szString));
                        pParam->pAssemblyImport = NULL;
                    }
                    else
                    {
                        mdAssemblyRef       rAsmRefTok[4096];
                        HCORENUM    hEnum=NULL;
                        ULONG   nAsmRefs = 0;
                        if(FAILED(pParam->pAssemblyImport->GetAssemblyFromScope(&pParam->tkManifest))
                            && (FAILED(pParam->pAssemblyImport->EnumAssemblyRefs(&hEnum,rAsmRefTok,4096,&nAsmRefs))
                                || (nAsmRefs ==0)))
                        {
                            pParam->pAssemblyImport->CloseEnum(hEnum);
                            pParam->pAssemblyImport->Release();
                            pParam->pAssemblyImport = NULL;
                        }
                    }
                    pParam->pImport->Release();
                }
                else
                {
                    sprintf_s(szString,SZSTRING_SIZE,RstrUTF(IDS_E_MDIIMPORT),hr);
                    printLine(pParam->GUICookie,COMMENT(szString));
                }
            }
        }
    } // end try
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if(param.pAssemblyImport) param.pAssemblyImport->Release();
        param.pAssemblyImport = NULL;
        if(param.pImport) param.pImport->Release();
    }
    PAL_ENDTRY
    return param.pAssemblyImport;
}

static void DumpMetadataVersion(void* GUICookie)
{
    LPCSTR pVersionStr;
    if (g_pImport == NULL || FAILED(g_pImport->GetVersionString(&pVersionStr)))
    {
        pVersionStr = "**Unavailable**";
    }
    sprintf_s(szString,SZSTRING_SIZE,"// Metadata version: %s",pVersionStr);
    printLine(GUICookie,szString);
}

void DumpManifest(void* GUICookie)
{
    DumpMetadataVersion(GUICookie);
    DumpModuleRefs(GUICookie);
    if(g_pAssemblyImport==NULL) g_pAssemblyImport = GetAssemblyImport(GUICookie);
    if(g_pAssemblyImport)
    {
        DumpAssemblyRefs(GUICookie);
        DumpAssembly(GUICookie,TRUE);
        DumpFiles(GUICookie);
        DumpComTypes(GUICookie);
        DumpManifestResources(GUICookie);
    }
    else printLine(GUICookie,COMMENT(RstrUTF(IDS_E_NOMANIFEST)));
    DumpScope(GUICookie);
    DumpVtable(GUICookie);

}
