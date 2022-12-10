// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// asmman.cpp - manifest info handling (implementation of class AsmMan, see asmman.hpp)
//

//
#include "ilasmpch.h"

#include "assembler.h"
#include "strongnameinternal.h"
#include <limits.h>

extern WCHAR*   pwzInputFiles[];

BinStr* BinStrToUnicode(BinStr* pSource, bool Swap)
{
    if(pSource)
    {
        pSource->appendInt8(0);
        BinStr* tmp = new BinStr();
        char*   pb = (char*)(pSource->ptr());
        int l=pSource->length(), L = sizeof(WCHAR)*l;
        if(tmp)
        {
            WCHAR*  wz = (WCHAR*)(tmp->getBuff(L));
            if(wz)
            {
                memset(wz,0,L);
                WszMultiByteToWideChar(g_uCodePage,0,pb,-1,wz,l);
                tmp->remove(L-(DWORD)wcslen(wz)*sizeof(WCHAR));
#if BIGENDIAN
                if (Swap)
                    SwapStringLength(wz, (DWORD)wcslen(wz));
#endif
                delete pSource;
            }
            else
            {
                delete tmp;
                tmp = NULL;
                fprintf(stderr,"\nOut of memory!\n");
            }
        }
        else
            fprintf(stderr,"\nOut of memory!\n");
        return tmp;
    }
    return NULL;
}

AsmManFile*         AsmMan::GetFileByName(_In_ __nullterminated char* szFileName)
{
    AsmManFile* ret = NULL;
    if(szFileName)
    {
        //AsmManFile X;
        //X.szName = szFileName;
        //ret = m_FileLst.FIND(&X);
        //X.szName = NULL;
        for(int i=0; (ret = m_FileLst.PEEK(i))&&strcmp(ret->szName,szFileName); i++);
    }
    return ret;
}

mdToken             AsmMan::GetFileTokByName(_In_ __nullterminated char* szFileName)
{
    AsmManFile* tmp = GetFileByName(szFileName);
    return(tmp ? tmp->tkTok : mdFileNil);
}

AsmManComType*          AsmMan::GetComTypeByName(_In_opt_z_ char* szComTypeName,
                                                 _In_opt_z_ char* szComEnclosingTypeName)
{
    AsmManComType*  ret = NULL;
    if(szComTypeName)
    {
        //AsmManComType X;
        //X.szName = szComTypeName;
        //ret = m_ComTypeLst.FIND(&X);
        //X.szName = NULL;
        for(int i=0; (ret = m_ComTypeLst.PEEK(i)) != NULL; i++)
        {
            if (strcmp(ret->szName, szComTypeName) == 0)
            {
                if (ret->szComTypeName == NULL && szComEnclosingTypeName == NULL)
                {
                    break;
                }

                if (ret->szComTypeName != NULL && szComEnclosingTypeName != NULL)
                {
                    if (strcmp(ret->szComTypeName, szComEnclosingTypeName) == 0)
                    {
                        break;
                    }
                }
            }
        }
    }
    return ret;
}

mdToken             AsmMan::GetComTypeTokByName(
    _In_opt_z_ char* szComTypeName,
    _In_opt_z_ char* szComEnclosingTypeName)
{
    AsmManComType* tmp = GetComTypeByName(szComTypeName, szComEnclosingTypeName);
    return(tmp ? tmp->tkTok : mdExportedTypeNil);
}

AsmManAssembly*     AsmMan::GetAsmRefByName(_In_ __nullterminated const char* szAsmRefName)
{
    AsmManAssembly* ret = NULL;
    if(szAsmRefName)
    {
        //AsmManAssembly X;
        //X.szAlias = szAsmRefName;
        //ret = m_AsmRefLst.FIND(&X);
        //X.szAlias = NULL;
        DWORD L = (DWORD)strlen(szAsmRefName);
        for(int i=0; (ret = m_AsmRefLst.PEEK(i))&&
            ((ret->dwAlias != L)||strcmp(ret->szAlias,szAsmRefName)); i++);
    }
    return ret;
}
mdToken             AsmMan::GetAsmRefTokByName(_In_ __nullterminated const char* szAsmRefName)
{
    AsmManAssembly* tmp = GetAsmRefByName(szAsmRefName);
    return(tmp ? tmp->tkTok : mdAssemblyRefNil);
}
AsmManAssembly*     AsmMan::GetAsmRefByAsmName(_In_ __nullterminated const char* szAsmName)
{
    AsmManAssembly* ret = NULL;
    if(szAsmName)
    {
        for(int i=0; (ret = m_AsmRefLst.PEEK(i))&&
            (strcmp(ret->szName,szAsmName)); i++);
    }
    return ret;
}

//==============================================================================================================
void    AsmMan::SetModuleName(__inout_opt __nullterminated char* szName)
{
    if(m_szScopeName == NULL)    // ignore all duplicate declarations
    {
        if(szName && *szName)
        {
            ULONG L = (ULONG)strlen(szName);
            if(L >= MAX_SCOPE_LENGTH)
            {
                ((Assembler*)m_pAssembler)->report->warn("Module name too long (%d chars, max.allowed: %d chars), truncated\n",L,MAX_SCOPE_LENGTH-1);
                szName[MAX_SCOPE_LENGTH-1] = 0;
            }
            m_szScopeName = szName;
            strcpy_s(((Assembler*)m_pAssembler)->m_szScopeName, MAX_SCOPE_LENGTH, szName);
        }
    }
}
//==============================================================================================================

void    AsmMan::AddFile(_In_ __nullterminated char* szName, DWORD dwAttr, BinStr* pHashBlob)
{
    AsmManFile* tmp = GetFileByName(szName);
    Assembler* pAsm = (Assembler*)m_pAssembler;
    if(tmp==NULL)
    {
        tmp = new (nothrow) AsmManFile();
        if(tmp==NULL)
        {
            pAsm->report->error("\nOut of memory!\n");
            return;
        }
        if((dwAttr & 0x80000000)!=0) pAsm->m_fEntryPointPresent = TRUE;
        tmp->szName = szName;
        tmp->dwAttr = dwAttr;
        tmp->pHash = pHashBlob;
        tmp->m_fNew = TRUE;
        m_FileLst.PUSH(tmp);
        tmp->tkTok = TokenFromRid(m_FileLst.COUNT(),mdtFile);
    }
    pAsm->m_tkCurrentCVOwner = 0;
    pAsm->m_pCustomDescrList = &(tmp->m_CustomDescrList);
}
//==============================================================================================================

void    AsmMan::EmitFiles()
{
    AsmManFile* tmp;
    Assembler* pAsm = (Assembler*)m_pAssembler;
    int i;
    HRESULT                 hr = S_OK;
    mdToken tk;
    for(i = 0; (tmp=m_FileLst.PEEK(i)) != NULL; i++)
    {
        BOOL    fEntry = ((tmp->dwAttr & 0x80000000)!=0);

        wzUniBuf[0] = 0;

        BYTE*       pHash=NULL;
        DWORD       cbHash= 0;

        if(!tmp->m_fNew) continue;
        tmp->m_fNew = FALSE;

        WszMultiByteToWideChar(g_uCodePage,0,tmp->szName,-1,wzUniBuf,dwUniBuf);
        if(tmp->pHash==NULL) // if hash not explicitly specified
        {
            if(m_pAssembly      // and assembly is defined
                && m_pAssembly->ulHashAlgorithm) // and hash algorithm is defined...
            { // then try to compute it
                {
                    pHash = NULL;
                    cbHash = 0;
                }
            }
        }
        else
        {
            pHash = tmp->pHash->ptr();
            cbHash = tmp->pHash->length();
        }

        hr = m_pAsmEmitter->DefineFile(wzUniBuf,
                                    (const void*)pHash,
                                    cbHash,
                                    tmp->dwAttr & 0x7FFFFFFF,
                                    (mdFile*)&tk);
        _ASSERTE(tk == tmp->tkTok);
        if(FAILED(hr)) report->error("Failed to define file '%s': 0x%08X\n",tmp->szName,hr);
        else
        {
            if(fEntry)
            {
                if (FAILED(pAsm->m_pCeeFileGen->SetEntryPoint(pAsm->m_pCeeFile, tmp->tkTok)))
                {
                    pAsm->report->error("Failed to set external entry point for file '%s'\n",tmp->szName);
                }
            }
            pAsm->EmitCustomAttributes(tmp->tkTok, &(tmp->m_CustomDescrList));
        }
    } //end for(i = 0; tmp=m_FileLst.PEEK(i); i++)
}

void    AsmMan::StartAssembly(_In_ __nullterminated char* szName, _In_opt_z_ char* szAlias, DWORD dwAttr, BOOL isRef)
{
    if(!isRef && (0==strcmp(szName, "mscorlib"))) ((Assembler*)m_pAssembler)->m_fIsMscorlib = TRUE;
    if(!isRef && (m_pAssembly != NULL))
    {
        if(strcmp(szName, m_pAssembly->szName))
            report->error("Multiple assembly declarations\n");
        // if name is the same, just ignore it
        m_pCurAsmRef = NULL;
    }
    else
    {
        if((m_pCurAsmRef = new (nothrow) AsmManAssembly()))
        {
            m_pCurAsmRef->usVerMajor = (USHORT)0xFFFF;
            m_pCurAsmRef->usVerMinor = (USHORT)0xFFFF;
            m_pCurAsmRef->usBuild = (USHORT)0xFFFF;
            m_pCurAsmRef->usRevision = (USHORT)0xFFFF;
            m_pCurAsmRef->szName = szName;
            m_pCurAsmRef->szAlias = szAlias ? szAlias : szName;
            m_pCurAsmRef->dwAlias = (DWORD)strlen(m_pCurAsmRef->szAlias);
            m_pCurAsmRef->dwAttr = dwAttr;
            m_pCurAsmRef->isRef = isRef;
            m_pCurAsmRef->isAutodetect = FALSE;
            m_pCurAsmRef->m_fNew = TRUE;
            if(!isRef) m_pAssembly = m_pCurAsmRef;
        }
        else
            report->error("Failed to allocate AsmManAssembly structure\n");
    }
    ((Assembler*)m_pAssembler)->m_tkCurrentCVOwner = 0;
    ((Assembler*)m_pAssembler)->m_CustomDescrListStack.PUSH(((Assembler*)m_pAssembler)->m_pCustomDescrList);
    ((Assembler*)m_pAssembler)->m_pCustomDescrList = m_pCurAsmRef ? &(m_pCurAsmRef->m_CustomDescrList) : NULL;

}
// copied from asmparse.y
static void corEmitInt(BinStr* buff, unsigned data)
{
    unsigned cnt = CorSigCompressData(data, buff->getBuff(5));
    buff->remove(5 - cnt);
}

void AsmMan::EmitDebuggableAttribute(mdToken tkOwner)
{
    mdToken tkCA;
    Assembler* pAsm = (Assembler*)m_pAssembler;
    mdToken tkTypeSpec, tkMscorlib, tkParamType;
    BinStr  *pbsSig = new BinStr();
    BinStr* bsBytes = new BinStr();;
    char*   szName;
    tkMscorlib = pAsm->m_fIsMscorlib ? 1 : pAsm->GetBaseAsmRef();
    tkTypeSpec = pAsm->ResolveClassRef(tkMscorlib,"System.Diagnostics.DebuggableAttribute",NULL);

    EmitAssemblyRefs(); // just in case we gained 'mscorlib' AsmRef in GetAsmRef above

    BOOL fOldStyle = FALSE;
    if(tkMscorlib == 1)
        fOldStyle = (m_pAssembly->usVerMajor == 1);
    else
    {
        AsmManAssembly *pAssembly = GetAsmRefByName("mscorlib");
        if(pAssembly != NULL)
        {
            fOldStyle = (pAssembly->usVerMajor == 1);
        }
    }

    bsBytes->appendInt8(1);
    bsBytes->appendInt8(0);
    if(fOldStyle)
    {
        pbsSig->appendInt8(IMAGE_CEE_CS_CALLCONV_HASTHIS);
        corEmitInt(pbsSig,2);
        pbsSig->appendInt8(ELEMENT_TYPE_VOID);
        pbsSig->appendInt8(ELEMENT_TYPE_BOOLEAN);
        pbsSig->appendInt8(ELEMENT_TYPE_BOOLEAN);

        //New to old: 0x101->(true,true),0x03->(true,false),0x103->(true,true)+warning
        bsBytes->appendInt8(1);
        bsBytes->appendInt8((pAsm->m_dwIncludeDebugInfo==0x03 ? 0 : 1));
        if(pAsm->m_dwIncludeDebugInfo == 0x103)
        {
            report->warn("\nOption /DEBUG=IMPL is invalid for legacy DebuggableAttribute, /DEBUG used.\n" );
        }
    }
    else
    {
        BinStr  bsSigArg;
        char buffer[80];
        sprintf_s(buffer,80,
                "%s%c%s",
                "System.Diagnostics.DebuggableAttribute",
                NESTING_SEP,
                "DebuggingModes"
               );

        tkParamType = pAsm->ResolveClassRef(tkMscorlib,buffer, NULL);

        bsSigArg.appendInt8(ELEMENT_TYPE_VALUETYPE);

        unsigned cnt = CorSigCompressToken(tkParamType, bsSigArg.getBuff(5));
        bsSigArg.remove(5 - cnt);

        pbsSig->appendInt8(IMAGE_CEE_CS_CALLCONV_HASTHIS);
        corEmitInt(pbsSig,1);
        pbsSig->appendInt8(ELEMENT_TYPE_VOID);
        pbsSig->append(&bsSigArg);

        bsBytes->appendInt32(VAL32(pAsm->m_dwIncludeDebugInfo));
    }
    bsBytes->appendInt8(0);
    bsBytes->appendInt8(0);

    szName = new char[16];
    strcpy_s(szName,16,".ctor");
    tkCA = pAsm->MakeMemberRef(tkTypeSpec,szName,pbsSig);
    pAsm->DefineCV(new CustomDescr(tkOwner,tkCA,bsBytes));
}

void    AsmMan::EndAssembly()
{
    if(m_pCurAsmRef)
    {
        if(m_pCurAsmRef->isRef)
        { // list the assembly ref
            if(GetAsmRefByName(m_pCurAsmRef->szAlias))
            {
                //report->warn("Multiple declarations of Assembly Ref '%s', ignored except the 1st one\n",m_pCurAsmRef->szName);
                delete m_pCurAsmRef;
                m_pCurAsmRef = NULL;
                return;
            }
            m_AsmRefLst.PUSH(m_pCurAsmRef);
            m_pCurAsmRef->tkTok = TokenFromRid(m_AsmRefLst.COUNT(),mdtAssemblyRef);
        }
        else
        {
            HRESULT                 hr = S_OK;
            m_pCurAsmRef->tkTok = TokenFromRid(1,mdtAssembly);

            // Determine the strong name public key. This may have been set
            // via a directive in the source or from the command line (which
            // overrides the directive). From the command line we may have
            // been provided with a file or the name of a CAPI key
            // container. Either may contain a public key or a full key
            // pair.
            if (((Assembler*)m_pAssembler)->m_wzKeySourceName)
            {
                // Key file versus container is determined by the first
                // character of the source ('@' for container).
                if (*(((Assembler*)m_pAssembler)->m_wzKeySourceName) == L'@')
                {
                    report->error("Error: ilasm on CoreCLR does not support getting public key from container.\n");
                    m_pCurAsmRef = NULL;
                    return;
                }
                else
                {
                    // Read public key or key pair from file.
                    HANDLE hFile = WszCreateFile(((Assembler*)m_pAssembler)->m_wzKeySourceName,
                                                 GENERIC_READ,
                                                 FILE_SHARE_READ,
                                                 NULL,
                                                 OPEN_EXISTING,
                                                 FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                                                 NULL);
                    if(hFile == INVALID_HANDLE_VALUE)
                    {
                        hr = GetLastError();
                        report->error("Failed to open key file '%S': 0x%08X\n",((Assembler*)m_pAssembler)->m_wzKeySourceName,hr);
                        m_pCurAsmRef = NULL;
                        return;
                    }

                    // Determine file size and allocate an appropriate buffer.
                    m_sStrongName.m_cbPublicKey = SafeGetFileSize(hFile, NULL);
                    if (m_sStrongName.m_cbPublicKey == 0xffffffff) {
                        report->error("File size too large\n");
                        m_pCurAsmRef = NULL;
                        CloseHandle(hFile);
                        return;
                    }

                    m_sStrongName.m_pbPublicKey = new BYTE[m_sStrongName.m_cbPublicKey];
                    if (m_sStrongName.m_pbPublicKey == NULL) {
                        report->error("Failed to allocate key buffer\n");
                        m_pCurAsmRef = NULL;
                        CloseHandle(hFile);
                        return;
                    }
                    m_sStrongName.m_dwPublicKeyAllocated = AsmManStrongName::AllocatedByNew;

                    // Read the file into the buffer.
                    DWORD dwBytesRead;
                    if (!ReadFile(hFile, m_sStrongName.m_pbPublicKey, m_sStrongName.m_cbPublicKey, &dwBytesRead, NULL)) {
                        hr = GetLastError();
                        report->error("Failed to read key file '%S': 0x%08X\n",((Assembler*)m_pAssembler)->m_wzKeySourceName,hr);
                        m_pCurAsmRef = NULL;
                        CloseHandle(hFile);
                        return;
                    }

                    CloseHandle(hFile);

                    // Guess whether we're full or delay signing based on
                    // whether the blob passed to us looks like a public
                    // key. (I.e. we may just have copied a full key pair
                    // into the public key buffer).
                    if (m_sStrongName.m_cbPublicKey >= sizeof(PublicKeyBlob) &&
                        (offsetof(PublicKeyBlob, PublicKey) +
                         VAL32(((PublicKeyBlob*)m_sStrongName.m_pbPublicKey)->cbPublicKey)) == m_sStrongName.m_cbPublicKey)
                        m_sStrongName.m_fFullSign = FALSE;
                    else
                        m_sStrongName.m_fFullSign = TRUE;

                    // If we really have a key pair, we'll move it into a
                    // key container so the signing code gets the key pair
                    // from a consistent place.
                    if (m_sStrongName.m_fFullSign)
                    {
                        report->error("Error: ilasm on CoreCLR does not support full sign.\n");
                        m_pCurAsmRef = NULL;
                        return;
                    }
                }
            }
            else
            {
                if (m_pAssembly->pPublicKey)
                {
                    m_sStrongName.m_pbPublicKey = m_pAssembly->pPublicKey->ptr();
                    m_sStrongName.m_cbPublicKey = m_pAssembly->pPublicKey->length();
                }
                else
                {
                    m_sStrongName.m_pbPublicKey = NULL;
                    m_sStrongName.m_cbPublicKey = 0;
                }

                m_sStrongName.m_wzKeyContainer = NULL;
                m_sStrongName.m_fFullSign = FALSE;
                m_sStrongName.m_dwPublicKeyAllocated = AsmManStrongName::NotAllocated;
            }
        }
        m_pCurAsmRef = NULL;
    }
    ((Assembler*)m_pAssembler)->m_pCustomDescrList = ((Assembler*)m_pAssembler)->m_CustomDescrListStack.POP();
}

void    FillAssemblyMetadata(AsmManAssembly *pAsm, ASSEMBLYMETADATA *pmd)
{
    pmd->usMajorVersion = pAsm->usVerMajor;
    pmd->usMinorVersion = pAsm->usVerMinor;
    pmd->usBuildNumber = pAsm->usBuild;
    pmd->usRevisionNumber = pAsm->usRevision;
    if(pmd->usMajorVersion == 0xFFFF) pmd->usMajorVersion = 0;
    if(pmd->usMinorVersion == 0xFFFF) pmd->usMinorVersion = 0;
    if(pmd->usBuildNumber == 0xFFFF) pmd->usBuildNumber = 0;
    if(pmd->usRevisionNumber == 0xFFFF) pmd->usRevisionNumber = 0;

    if(pAsm->pLocale != NULL)
    {
        pmd->szLocale = (LPWSTR)(pAsm->pLocale->ptr());
        pmd->cbLocale = pAsm->pLocale->length()/((ULONG)sizeof(WCHAR));
    }
    else
    {
        pmd->szLocale = NULL;
        pmd->cbLocale = 0;
    }

    pmd->rProcessor = NULL;
    pmd->rOS = NULL;
    pmd->ulProcessor = 0;
    pmd->ulOS = 0;
}

void    AsmMan::EmitAssemblyRefs()
{
    int i;
    HRESULT                 hr = S_OK;
    ASSEMBLYMETADATA md;
    mdToken tk;

    for(i=0; (m_pCurAsmRef=m_AsmRefLst.PEEK(i)) != NULL; i++)
    {
        if(!m_pCurAsmRef->m_fNew) continue;
        m_pCurAsmRef->m_fNew = FALSE;

        wzUniBuf[0] = 0;
        FillAssemblyMetadata(m_pCurAsmRef,&md);

        // See if we've got a full public key or the tokenized version (or neither).
        BYTE *pbPublicKeyOrToken = NULL;
        DWORD cbPublicKeyOrToken = 0;
        DWORD dwFlags = m_pCurAsmRef->dwAttr;
        if (m_pCurAsmRef->pPublicKeyToken)
        {
            pbPublicKeyOrToken = m_pCurAsmRef->pPublicKeyToken->ptr();
            cbPublicKeyOrToken = m_pCurAsmRef->pPublicKeyToken->length();

        }
        else if (m_pCurAsmRef->pPublicKey)
        {
            pbPublicKeyOrToken = m_pCurAsmRef->pPublicKey->ptr();
            cbPublicKeyOrToken = m_pCurAsmRef->pPublicKey->length();
            dwFlags |= afPublicKey;
        }
        // Convert name to Unicode
        WszMultiByteToWideChar(g_uCodePage,0,m_pCurAsmRef->szName,-1,wzUniBuf,dwUniBuf);
        hr = m_pAsmEmitter->DefineAssemblyRef(       // S_OK or error.
                    pbPublicKeyOrToken,              // [IN] Public key or token of the assembly.
                    cbPublicKeyOrToken,              // [IN] Count of bytes in the key or token.
                    (LPCWSTR)wzUniBuf,               // [IN] Name of the assembly being referenced.
                    (const ASSEMBLYMETADATA*)&md,    // [IN] Assembly MetaData.
                    (m_pCurAsmRef->pHashBlob ? (const void*)(m_pCurAsmRef->pHashBlob->ptr()) : NULL),           // [IN] Hash Blob.
                    (m_pCurAsmRef->pHashBlob ? m_pCurAsmRef->pHashBlob->length() : 0),            // [IN] Count of bytes in the Hash Blob.
                    dwFlags,                         // [IN] Flags.
                    (mdAssemblyRef*)&tk);         // [OUT] Returned AssemblyRef token.
        if(m_pCurAsmRef->tkTok != tk)
        {
            report->error("AsmRef'%S' tok %8.8X -> %8.8X\n",wzUniBuf,m_pCurAsmRef->tkTok,tk);
        }
        if(FAILED(hr)) report->error("Failed to define assembly ref '%s': 0x%08X\n",m_pCurAsmRef->szName,hr);
        else
        {
            ((Assembler*)m_pAssembler)->EmitCustomAttributes(m_pCurAsmRef->tkTok, &(m_pCurAsmRef->m_CustomDescrList));
        }
    } // end for(i=0; m_pCurAsmRef=m_AsmRefLst.PEEK(i); i++)
}

void    AsmMan::EmitAssembly()
{
    HRESULT                 hr = S_OK;
    ASSEMBLYMETADATA md;

    wzUniBuf[0] = 0;
    if(m_pAssembly == NULL) return;
    if(!m_pAssembly->m_fNew) return;
    m_pAssembly->m_fNew = FALSE;

    FillAssemblyMetadata(m_pAssembly, &md);

    // Convert name to Unicode
    WszMultiByteToWideChar(g_uCodePage,0,m_pAssembly->szName,-1,wzUniBuf,dwUniBuf);

    hr = m_pAsmEmitter->DefineAssembly(              // S_OK or error.
        (const void*)(m_sStrongName.m_pbPublicKey), // [IN] Public key of the assembly.
        m_sStrongName.m_cbPublicKey,                // [IN] Count of bytes in the public key.
        m_pAssembly->ulHashAlgorithm,            // [IN] Hash algorithm used to hash the files.
        (LPCWSTR)wzUniBuf,                 // [IN] Name of the assembly.
        (const ASSEMBLYMETADATA*)&md,  // [IN] Assembly MetaData.
        m_pAssembly->dwAttr,        // [IN] Flags.
        (mdAssembly*)&(m_pAssembly->tkTok));             // [OUT] Returned Assembly token.

    if(FAILED(hr)) report->error("Failed to define assembly '%s': 0x%08X\n",m_pAssembly->szName,hr);
    else
    {
        Assembler* pAsm = ((Assembler*)m_pAssembler);
        pAsm->EmitSecurityInfo(m_pAssembly->tkTok,
                             m_pAssembly->m_pPermissions,
                             m_pAssembly->m_pPermissionSets);
        if(pAsm->m_dwIncludeDebugInfo)
        {
            EmitDebuggableAttribute(m_pAssembly->tkTok);
        }
        pAsm->EmitCustomAttributes(m_pAssembly->tkTok, &(m_pAssembly->m_CustomDescrList));
    }
}

void    AsmMan::SetAssemblyPublicKey(BinStr* pPublicKey)
{
    if(m_pCurAsmRef)
    {
        m_pCurAsmRef->pPublicKey = pPublicKey;
    }
}

void    AsmMan::SetAssemblyPublicKeyToken(BinStr* pPublicKeyToken)
{
    if(m_pCurAsmRef)
    {
        m_pCurAsmRef->pPublicKeyToken = pPublicKeyToken;
    }
}

void    AsmMan::SetAssemblyHashAlg(ULONG ulAlgID)
{
    if(m_pCurAsmRef)
    {
        m_pCurAsmRef->ulHashAlgorithm = ulAlgID;
    }
}

void    AsmMan::SetAssemblyVer(USHORT usMajor, USHORT usMinor, USHORT usBuild, USHORT usRevision)
{
    if(m_pCurAsmRef)
    {
        m_pCurAsmRef->usVerMajor = usMajor;
        m_pCurAsmRef->usVerMinor = usMinor;
        m_pCurAsmRef->usBuild = usBuild;
        m_pCurAsmRef->usRevision = usRevision;
    }
}

void    AsmMan::SetAssemblyLocale(BinStr* pLocale, BOOL bConvertToUnicode)
{
    if(m_pCurAsmRef)
    {
        m_pCurAsmRef->pLocale = bConvertToUnicode ? ::BinStrToUnicode(pLocale) : pLocale;
    }
}

void    AsmMan::SetAssemblyHashBlob(BinStr* pHashBlob)
{
    if(m_pCurAsmRef)
    {
        m_pCurAsmRef->pHashBlob = pHashBlob;
    }
}

void    AsmMan::SetAssemblyAutodetect()
{
    if(m_pCurAsmRef)
    {
        m_pCurAsmRef->isAutodetect = TRUE;
    }
}

void    AsmMan::StartComType(_In_ __nullterminated char* szName, DWORD dwAttr)
{
    if((m_pCurComType = new (nothrow) AsmManComType()))
    {
        m_pCurComType->szName = szName;
        m_pCurComType->dwAttr = dwAttr;
        m_pCurComType->m_fNew = TRUE;
        ((Assembler*)m_pAssembler)->m_tkCurrentCVOwner = 0;
        ((Assembler*)m_pAssembler)->m_CustomDescrListStack.PUSH(((Assembler*)m_pAssembler)->m_pCustomDescrList);
        ((Assembler*)m_pAssembler)->m_pCustomDescrList = &(m_pCurComType->m_CustomDescrList);
    }
    else
        report->error("Failed to allocate AsmManComType structure\n");
}

void    AsmMan::EndComType()
{
    if(m_pCurComType)
    {
        if(m_pAssembler)
        {
            Class* pClass =((Assembler*)m_pAssembler)->m_pCurClass;
            if(pClass)
            {
                m_pCurComType->tkClass = pClass->m_cl;
                if(pClass->m_pEncloser)
                {
                    mdTypeDef tkEncloser = pClass->m_pEncloser->m_cl;
                    AsmManComType* pCT;
                    for(unsigned i=0; (pCT=m_ComTypeLst.PEEK(i)); i++)
                    {
                        if(pCT->tkClass == tkEncloser)
                        {
                            m_pCurComType->szComTypeName = pCT->szName;
                            break;
                        }
                    }
                }
            }
        }

        if (IsTdNested(m_pCurComType->dwAttr) && GetComTypeByName(m_pCurComType->szName, m_pCurComType->szComTypeName) != NULL)
        {
            report->error("Invalid TypeDefID of exported type\n");
            delete m_pCurComType;
        }
        else
        {
            m_ComTypeLst.PUSH(m_pCurComType);
        }

        m_pCurComType = NULL;
        ((Assembler*)m_pAssembler)->m_tkCurrentCVOwner = 0;
        ((Assembler*)m_pAssembler)->m_pCustomDescrList = ((Assembler*)m_pAssembler)->m_CustomDescrListStack.POP();
    }
}

void    AsmMan::SetComTypeFile(_In_ __nullterminated char* szFileName)
{
    if(m_pCurComType)
    {
        m_pCurComType->szFileName = szFileName;
    }
}

void    AsmMan::SetComTypeAsmRef(_In_ __nullterminated char* szAsmRefName)
{
    if(m_pCurComType)
    {
        m_pCurComType->szAsmRefName = szAsmRefName;
    }
}

void    AsmMan::SetComTypeComType(_In_ __nullterminated char* szComTypeName)
{
    if(m_pCurComType)
    {
        m_pCurComType->szComTypeName = szComTypeName;
    }
}
BOOL    AsmMan::SetComTypeImplementationTok(mdToken tk)
{
    if(m_pCurComType)
    {
        switch(TypeFromToken(tk))
        {
        case mdtAssemblyRef:
        case mdtExportedType:
        case mdtFile:
            m_pCurComType->tkImpl = tk;
            return TRUE;
        }
    }
    return FALSE;
}
BOOL    AsmMan::SetComTypeClassTok(mdToken tkClass)
{
    if((m_pCurComType)&&(TypeFromToken(tkClass)==mdtTypeDef))
    {
        m_pCurComType->tkClass = tkClass;
        return TRUE;
    }
    return FALSE;
}

void    AsmMan::StartManifestRes(_In_ __nullterminated char* szName, _In_ __nullterminated char* szAlias, DWORD dwAttr)
{
    if((m_pCurManRes = new (nothrow) AsmManRes()))
    {
        m_pCurManRes->szName = szName;
        m_pCurManRes->szAlias = szAlias;
        m_pCurManRes->dwAttr = dwAttr;
        m_pCurManRes->m_fNew = TRUE;
        ((Assembler*)m_pAssembler)->m_tkCurrentCVOwner = 0;
        ((Assembler*)m_pAssembler)->m_CustomDescrListStack.PUSH(((Assembler*)m_pAssembler)->m_pCustomDescrList);
        ((Assembler*)m_pAssembler)->m_pCustomDescrList = &(m_pCurManRes->m_CustomDescrList);
    }
    else
        report->error("Failed to allocate AsmManRes structure\n");
}

void    AsmMan::EndManifestRes()
{
    if(m_pCurManRes)
    {
        m_ManResLst.PUSH(m_pCurManRes);
        m_pCurManRes = NULL;
        ((Assembler*)m_pAssembler)->m_tkCurrentCVOwner = 0;
        ((Assembler*)m_pAssembler)->m_pCustomDescrList = ((Assembler*)m_pAssembler)->m_CustomDescrListStack.POP();
    }
}


void    AsmMan::SetManifestResFile(_In_ __nullterminated char* szFileName, ULONG ulOffset)
{
    if(m_pCurManRes)
    {
        m_pCurManRes->szFileName = szFileName;
        m_pCurManRes->ulOffset = ulOffset;
    }
}

void    AsmMan::SetManifestResAsmRef(_In_ __nullterminated char* szAsmRefName)
{
    if(m_pCurManRes)
    {
        m_pCurManRes->szAsmRefName = szAsmRefName;
    }
}

HRESULT AsmMan::EmitManifest()
{
    //AsmManAssembly*           pAsmRef;
    AsmManComType*          pComType;
    AsmManRes*              pManRes;
    HRESULT                 hr = S_OK;

    wzUniBuf[0] = 0;

    if(m_pAsmEmitter==NULL)
        hr=m_pEmitter->QueryInterface(IID_IMetaDataAssemblyEmit, (void**) &m_pAsmEmitter);

    if(SUCCEEDED(hr))
    {
        EmitFiles();
        EmitAssembly();

        if((((Assembler*)m_pAssembler)->m_dwIncludeDebugInfo != 0) && (m_pAssembly == NULL))
        {
            mdToken tkOwner, tkMscorlib;
            tkMscorlib = ((Assembler*)m_pAssembler)->GetAsmRef("mscorlib");
            tkOwner = ((Assembler*)m_pAssembler)->ResolveClassRef(tkMscorlib,
                                                                  "System.Runtime.CompilerServices.AssemblyAttributesGoHere",
                                                                  NULL);
            EmitDebuggableAttribute(tkOwner);
        }

        // Emit all com types
        unsigned i;
        for(i = 0; (pComType = m_ComTypeLst.PEEK(i)); i++)
        {
            if(!pComType->m_fNew) continue;
            pComType->m_fNew = FALSE;

            WszMultiByteToWideChar(g_uCodePage,0,pComType->szName,-1,wzUniBuf,dwUniBuf);
            mdToken     tkImplementation = mdTokenNil;
            if(pComType->tkImpl) tkImplementation = pComType->tkImpl;
            else if(pComType->szFileName)
            {
                tkImplementation = GetFileTokByName(pComType->szFileName);
                if(tkImplementation==mdFileNil)
                {
                    report->error("Undefined File '%s' in ExportedType '%s'\n",pComType->szFileName,pComType->szName);
                    if(!((Assembler*)m_pAssembler)->OnErrGo) continue;
                }
            }
            else if(pComType->szAsmRefName)
            {
                tkImplementation = GetAsmRefTokByName(pComType->szAsmRefName);
                if(RidFromToken(tkImplementation)==0)
                {
                    report->error("Undefined AssemblyRef '%s' in ExportedType '%s'\n",pComType->szAsmRefName,pComType->szName);
                    if(!((Assembler*)m_pAssembler)->OnErrGo) continue;
                }
            }
            else if(pComType->szComTypeName)
            {
                char* szLastName = strrchr(pComType->szComTypeName, NESTING_SEP);

                if(szLastName)
                {
                    *szLastName = 0;
                    szLastName ++;
                    tkImplementation = GetComTypeTokByName(szLastName, pComType->szComTypeName);
                    *(szLastName-1) = NESTING_SEP; // not really necessary
                }

                else
                {
                    tkImplementation = GetComTypeTokByName(pComType->szComTypeName);
                }

                if(tkImplementation==mdExportedTypeNil)
                {
                    report->error("Undefined ExportedType '%s' in ExportedType '%s'\n",pComType->szComTypeName,pComType->szName);
                    if(!((Assembler*)m_pAssembler)->OnErrGo) continue;
                }
            }
            else
            {
                report->warn("Undefined implementation in ExportedType '%s' -- ExportType not emitted\n",pComType->szName);
                if(!((Assembler*)m_pAssembler)->OnErrGo) continue;
            }
            hr = m_pAsmEmitter->DefineExportedType(         // S_OK or error.
                    (LPCWSTR)wzUniBuf,                      // [IN] Name of the Com Type.
                    tkImplementation,                       // [IN] mdFile or mdAssemblyRef that provides the ComType.
                    (mdTypeDef)pComType->tkClass,           // [IN] TypeDef token within the file.
                    pComType->dwAttr,                       // [IN] Flags.
                    (mdExportedType*)&(pComType->tkTok));   // [OUT] Returned ComType token.
            if(FAILED(hr)) report->error("Failed to define ExportedType '%s': 0x%08X\n",pComType->szName,hr);
            else
            {
                ((Assembler*)m_pAssembler)->EmitCustomAttributes(pComType->tkTok, &(pComType->m_CustomDescrList));
            }
        }

        // Emit all manifest resources
        m_dwMResSizeTotal = 0;
        m_dwMResNum = 0;
        for(i = 0; (pManRes = m_ManResLst.PEEK(i)); i++)
        {
            BOOL fOK = TRUE;
            mdToken     tkImplementation = mdFileNil;

            if(!pManRes->m_fNew) continue;
            pManRes->m_fNew = FALSE;

            WszMultiByteToWideChar(g_uCodePage,0,pManRes->szAlias,-1,wzUniBuf,dwUniBuf);
            if(pManRes->szAsmRefName)
            {
                tkImplementation = GetAsmRefTokByName(pManRes->szAsmRefName);
                if(RidFromToken(tkImplementation)==0)
                {
                    report->error("Undefined AssemblyRef '%s' in MResource '%s'\n",pManRes->szAsmRefName,pManRes->szName);
                    fOK = FALSE;
                }
            }
            else if(pManRes->szFileName)
            {
                tkImplementation = GetFileTokByName(pManRes->szFileName);
                if(RidFromToken(tkImplementation)==0)
                {
                    report->error("Undefined File '%s' in MResource '%s'\n",pManRes->szFileName,pManRes->szName);
                    fOK = FALSE;
                }
            }
            else // embedded mgd.resource, go after the file
            {
                HANDLE hFile = INVALID_HANDLE_VALUE;
                int j;
                WCHAR   wzFileName[2048];
                WCHAR*  pwz;

                pManRes->ulOffset = m_dwMResSizeTotal;
                for(j=0; (hFile == INVALID_HANDLE_VALUE)&&(pwzInputFiles[j] != NULL); j++)
                {
                    wcscpy_s(wzFileName,2048,pwzInputFiles[j]);
                    pwz = wcsrchr(wzFileName,DIRECTORY_SEPARATOR_CHAR_A);
#ifdef TARGET_WINDOWS
                    if(pwz == NULL) pwz = wcsrchr(wzFileName,':');
#endif
                    if(pwz == NULL) pwz = &wzFileName[0];
                    else pwz++;
                    wcscpy_s(pwz,2048-(pwz-wzFileName),wzUniBuf);
                    hFile = WszCreateFile(wzFileName, GENERIC_READ, FILE_SHARE_READ,
                             0, OPEN_EXISTING, 0, 0);
                }
                if (hFile == INVALID_HANDLE_VALUE)
                {
                    report->error("Failed to open managed resource file '%s'\n",pManRes->szAlias);
                    fOK = FALSE;
                }
                else
                {
                    if (m_dwMResNum >= MAX_MANIFEST_RESOURCES)
                    {
                        report->error("Too many resources (implementation limit: %d); skipping file '%s'\n",MAX_MANIFEST_RESOURCES,pManRes->szAlias);
                        fOK = FALSE;
                    }
                    else
                    {
                        m_dwMResSize[m_dwMResNum] = SafeGetFileSize(hFile,NULL);
                        if(m_dwMResSize[m_dwMResNum] == 0xFFFFFFFF)
                        {
                            report->error("Failed to get size of managed resource file '%s'\n",pManRes->szAlias);
                            fOK = FALSE;
                        }
                        else
                        {
                            m_dwMResSizeTotal += m_dwMResSize[m_dwMResNum]+sizeof(DWORD);
                            m_wzMResName[m_dwMResNum] = new WCHAR[wcslen(wzFileName)+1];
                            wcscpy_s(m_wzMResName[m_dwMResNum],wcslen(wzFileName)+1,wzFileName);
                            m_fMResNew[m_dwMResNum] = TRUE;
                            m_dwMResNum++;
                        }
                    }

                    CloseHandle(hFile);
                }
            }
            if(fOK || ((Assembler*)m_pAssembler)->OnErrGo)
            {
                WszMultiByteToWideChar(g_uCodePage,0,pManRes->szName,-1,wzUniBuf,dwUniBuf);
                hr = m_pAsmEmitter->DefineManifestResource(         // S_OK or error.
                        (LPCWSTR)wzUniBuf,                          // [IN] Name of the resource.
                        tkImplementation,                           // [IN] mdFile or mdAssemblyRef that provides the resource.
                        pManRes->ulOffset,                          // [IN] Offset to the beginning of the resource within the file.
                        pManRes->dwAttr,                            // [IN] Flags.
                        (mdManifestResource*)&(pManRes->tkTok));    // [OUT] Returned ManifestResource token.
                if(FAILED(hr))
                    report->error("Failed to define manifest resource '%s': 0x%08X\n",pManRes->szName,hr);
            }
        }


        m_pAsmEmitter->Release();
        m_pAsmEmitter = NULL;
    }
    else
        report->error("Failed to obtain IMetaDataAssemblyEmit interface: 0x%08X\n",hr);
    return hr;
}

