// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// writer_ENC.cpp
//

//
#include "ilasmpch.h"

#include "assembler.h"

//#include "ceefilegenwriter.h"
#include "strongname.h"

int ist=0;
#define REPT_STEP   //printf("Step %d\n",++ist);

HRESULT Assembler::InitMetaDataForENC(__in __nullterminated WCHAR* wzOrigFileName)
{
    HRESULT             hr = E_FAIL;

    if((wzOrigFileName==NULL)||(*wzOrigFileName == 0)||(m_pDisp==NULL)) return hr;
    if (m_pSymWriter != NULL)
    {
        m_pSymWriter->Close();
        m_pSymWriter->Release();
        m_pSymWriter = NULL;
    }
    if (m_pImporter != NULL)
    {
        m_pImporter->Release();
        m_pImporter = NULL;
    }
    if (m_pEmitter != NULL)
    {
        m_pEmitter->Release();
        m_pEmitter = NULL;
    }
    //WszSetEnvironmentVariable(L"COMP_ENC_OPENSCOPE", wzOrigFileName);
    //hr = m_pDisp->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit2,
    //                    (IUnknown **)&m_pEmitter);

    if((m_pbsMD==NULL)||(m_pbsMD->length()==0))
    {
        _ASSERTE(!"NO BASE METADATA!");
        return E_FAIL;
    }
   
    VARIANT encOption;
    V_VT(&encOption) = VT_UI4;
    V_UI4(&encOption) = MDUpdateENC;
    m_pDisp->SetOption(MetaDataSetENC, &encOption);
    V_UI4(&encOption) = MDErrorOutOfOrderDefault;
    m_pDisp->SetOption(MetaDataErrorIfEmitOutOfOrder, &encOption);
    hr = m_pDisp->OpenScopeOnMemory( m_pbsMD->ptr(), 
                                     m_pbsMD->length(), 
                                     ofWrite, 
                                     IID_IMetaDataEmit2, 
                                     (IUnknown **)&m_pEmitter);
    _ASSERTE(SUCCEEDED(hr));
    if (FAILED(hr))
        goto exit;

    m_pManifest->SetEmitter(m_pEmitter);
    if(FAILED(hr = m_pEmitter->QueryInterface(IID_IMetaDataImport2, (void**)&m_pImporter)))
        goto exit;

    //WszSetEnvironmentVariable(L"COMP_ENC_EMIT", wzOrigFileName);
    if(!Init()) goto exit; // close and re-open CeeFileGen and CeeFile
    hr = S_OK;


exit:
    return hr;
}
/*********************************************************************************/

BOOL Assembler::EmitFieldsMethodsENC(Class* pClass)
{
    unsigned n;
    BOOL ret = TRUE;
    // emit all field definition metadata tokens
    if((pClass->m_FieldDList.COUNT()))
    {
        FieldDescriptor*    pFD;
        int j;
        for(j=0, n=0; (pFD = pClass->m_FieldDList.PEEK(j)); j++) // can't use POP here: we'll need field list for props
        {
            if(pFD->m_fNew)
            {
                if(!EmitField(pFD))
                {
                    if(!OnErrGo) return FALSE;
                    ret = FALSE;
                }
                pFD->m_fNew = FALSE;
                n++;
            }
        }
        if(m_fReportProgress) printf("Fields: %d;\t",n);
    }
    // Fields are emitted; emit the class layout
    {
        COR_FIELD_OFFSET *pOffsets = NULL;
        ULONG ul = pClass->m_ulPack;
        ULONG N = pClass->m_dwNumFieldsWithOffset;

        EmitSecurityInfo(pClass->m_cl,
                         pClass->m_pPermissions,
                         pClass->m_pPermissionSets);
        pClass->m_pPermissions = NULL;
        pClass->m_pPermissionSets = NULL;
        if((pClass->m_ulSize != 0xFFFFFFFF)||(ul != 0)||(N != 0))
        {
            if(IsTdAutoLayout(pClass->m_Attr)) report->warn("Layout specified for auto-layout class\n");
            if((ul > 128)||((ul & (ul-1)) !=0 ))
                report->error("Invalid packing parameter (%d), must be 1,2,4,8...128\n",pClass->m_ulPack);
            if(N)
            {
                pOffsets = new COR_FIELD_OFFSET[N+1];
                ULONG i,j=0;
                FieldDescriptor *pFD;
                for(i=0; (pFD = pClass->m_FieldDList.PEEK(i)); i++)
                {
                    if(pFD->m_ulOffset != 0xFFFFFFFF)
                    {
                        pOffsets[j].ridOfField = RidFromToken(pFD->m_fdFieldTok);
                        pOffsets[j].ulOffset = pFD->m_ulOffset;
                        j++;
                    }
                }
                _ASSERTE(j == N);
                pOffsets[j].ridOfField = mdFieldDefNil;
            }
            m_pEmitter->SetClassLayout   (   
                        pClass->m_cl,       // [IN] typedef 
                        ul,                     // [IN] packing size specified as 1, 2, 4, 8, or 16 
                        pOffsets,               // [IN] array of layout specification   
                        pClass->m_ulSize); // [IN] size of the class   
            if(pOffsets) delete [] pOffsets;
        }
    }
    // emit all method definition metadata tokens
    if((pClass->m_MethodList.COUNT()))
    {
        Method* pMethod;
        int i;

        for(i=0, n=0; (pMethod = pClass->m_MethodList.PEEK(i));i++)
        {
            if(pMethod->m_fNew)
            {
                if(!EmitMethod(pMethod))
                {
                    if(!OnErrGo) return FALSE;
                    ret = FALSE;
                }
                pMethod->m_fNew = FALSE;
                n++;
            }
        }
        if(m_fReportProgress) printf("Methods: %d;\t",n);
    }
    if(m_fReportProgress) printf("\n");
    return ret;
}

BOOL Assembler::EmitEventsPropsENC(Class* pClass)
{
    unsigned n;
    BOOL ret = TRUE;
    // emit all event definition metadata tokens
    if((pClass->m_EventDList.COUNT()))
    {
        EventDescriptor* pED;
        int j;
        for(j=0,n=0; (pED = pClass->m_EventDList.PEEK(j)); j++) // can't use POP here: we'll need event list for props
        {
            if(pED->m_fNew)
            {
                if(!EmitEvent(pED))
                {
                    if(!OnErrGo) return FALSE;
                    ret = FALSE;
                }
                pED->m_fNew = FALSE;
                n++;
            }
        }
        if(m_fReportProgress) printf("Events: %d;\t",n);
    }
    // emit all property definition metadata tokens
    if((pClass->m_PropDList.COUNT()))
    {
        PropDescriptor* pPD;
        int j;

        for(j=0,n=0; (pPD = pClass->m_PropDList.PEEK(j)); j++)
        {
            if(pPD->m_fNew)
            {
                if(!EmitProp(pPD))
                {
                    if(!OnErrGo) return FALSE;
                    ret = FALSE;
                }
                pPD->m_fNew = FALSE;
                n++;
            }
        }
        if(m_fReportProgress) printf("Props: %d;\t",n);
    }
    if(m_fReportProgress) printf("\n");
    return ret;
}

HRESULT Assembler::CreateDeltaFiles(__in __nullterminated WCHAR *pwzOutputFilename)
{
    HRESULT             hr;
    DWORD               mresourceSize = 0;
    BYTE*               mresourceData = NULL;
    WCHAR*              pEnd = NULL;

    if(m_fReportProgress) printf("Creating DMETA,DIL files\n");
    if (!m_pEmitter)
    {
        printf("Error: Cannot create a PE file with no metadata\n");
        return E_FAIL;
    }
REPT_STEP
    if(m_pManifest)
    {
        hr = S_OK;
        if(m_pManifest->m_pAsmEmitter==NULL)
            hr=m_pEmitter->QueryInterface(IID_IMetaDataAssemblyEmit, (void**) &(m_pManifest->m_pAsmEmitter));

        if(SUCCEEDED(hr))
        {
            m_pManifest->EmitAssemblyRefs();
        }
    }
    // Emit classes, class members and globals:
    {
        Class *pSearch;
        int i;
        BOOL    bIsUndefClass = FALSE;
        if(m_fReportProgress)   printf("\nEmitting classes:\n");
        for (i=1; (pSearch = m_lstClass.PEEK(i)); i++)   // 0 is <Module>
        {
            if(pSearch->m_fNew)
            {
                if(m_fReportProgress)
                    printf("Class %d:\t%s\n",i,pSearch->m_szFQN);
                
                if(pSearch->m_bIsMaster)
                {
                    report->msg("Error: Reference to undefined class '%s'\n",pSearch->m_szFQN);
                    bIsUndefClass = TRUE;
                }
                if(!EmitClass(pSearch))
                {
                    if(!OnErrGo) return E_FAIL;
                }
                pSearch->m_fNew = FALSE;
            }
        }
        if(bIsUndefClass && !OnErrGo) return E_FAIL;
        
        if(m_fReportProgress)   printf("\nEmitting fields and methods:\n");
        for (i=0; (pSearch = m_lstClass.PEEK(i)) != NULL; i++)
        {
            if(pSearch->m_fNewMembers)
            {
                if(m_fReportProgress)
                {
                    if(i == 0)  printf("Global \t");
                    else        printf("Class %d\t",i);
                }
                if(!EmitFieldsMethodsENC(pSearch))
                {
                    if(!OnErrGo) return E_FAIL;
                }
            }
        }
    }
REPT_STEP

    // All ref'ed items def'ed in this file are emitted, resolve member refs to member defs:
    hr = ResolveLocalMemberRefs();
    if(FAILED(hr) &&(!OnErrGo)) goto exit;

    // Local member refs resolved, emit events, props and method impls
    {
        Class *pSearch;
        int i;
    
        if(m_fReportProgress)   printf("\nEmitting events and properties:\n");
        for (i=0; (pSearch = m_lstClass.PEEK(i)); i++)
        {
            if(pSearch->m_fNewMembers)
            {
                if(m_fReportProgress)
                {
                    if(i == 0)  printf("Global \t");
                    else        printf("Class %d\t",i);
                }
                if(!EmitEventsPropsENC(pSearch))
                {
                    if(!OnErrGo) return E_FAIL;
                }
                pSearch->m_fNewMembers = FALSE;
            }
        }
    }
    if(m_MethodImplDList.COUNT())
    {
        if(m_fReportProgress) report->msg("Method Implementations (total): %d\n",m_MethodImplDList.COUNT());
        if(!EmitMethodImpls())
        {
            if(!OnErrGo) return E_FAIL;
        }
    }
REPT_STEP
    // Emit the rest of the metadata
    hr = S_OK;
    if(m_pManifest) 
    {
        if (FAILED(hr = m_pManifest->EmitManifest())) goto exit;
    }
    ResolveLocalMemberRefs(); // in case CAs added some
    EmitUnresolvedCustomAttributes();
REPT_STEP

    hr = DoLocalMemberRefFixups();
    if(FAILED(hr) &&(!OnErrGo)) goto exit;

    // Local member refs resolved and fixed up in BinStr method bodies. Emit the bodies to a separate file.
    pEnd = &pwzOutputFilename[wcslen(pwzOutputFilename)];
    {
        Class* pClass;
        Method* pMethod;
        FILE* pF = NULL;
        wcscat_s(pwzOutputFilename,MAX_SCOPE_LENGTH,W(".dil"));
        if(_wfopen_s(&pF,pwzOutputFilename,W("wb"))==0)
        {
            int i,j,L=0,M=0;
            BinStr bsOut;
            for (i=0; (pClass = m_lstClass.PEEK(i)); i++)
            {
                for(j=0; (pMethod = pClass->m_MethodList.PEEK(j)); j++)
                {
                    if(pMethod->m_fNewBody)
                    {
                        L+= pMethod->m_pbsBody->length()+3;
                        M++;
                    }
                }
            }
            bsOut.getBuff(L+sizeof(DWORD)); // to avoid reallocs
            bsOut.remove(L);
            for (i=0; (pClass = m_lstClass.PEEK(i)); i++)
            {
                for(j=0; (pMethod = pClass->m_MethodList.PEEK(j)); j++)
                {
                    if(pMethod->m_fNewBody)
                    {
                        if(!EmitMethodBody(pMethod,&bsOut))
                        {
                            report->msg("Error: failed to emit body of '%s'\n",pMethod->m_szName);
                            hr = E_FAIL;
                            if(!OnErrGo)
                            { 
                                fclose(pF);
                                *pEnd = 0;
                                goto exit;
                            }
                        }
                        pMethod->m_fNewBody = FALSE;
                    }
                }
            }
            *((DWORD*)(bsOut.ptr())) = bsOut.length() - sizeof(DWORD);
            fwrite(bsOut.ptr(),bsOut.length(),1,pF);
            fclose(pF);
        }
        else
            report->msg("Error: failed to open file '%S'\n",pwzOutputFilename);

        *pEnd = 0;
    }
REPT_STEP
    
    //if (DoGlobalFixups() == FALSE)
    //    return E_FAIL;
    
    //if (FAILED(hr=m_pCeeFileGen->SetOutputFileName(m_pCeeFile, pwzOutputFilename))) goto exit;

    // Emit the meta-data to a separate file
    IMetaDataEmit2* pENCEmitter;
    if(FAILED(hr = m_pEmitter->QueryInterface(IID_IMetaDataEmit2, (void**)&pENCEmitter)))
        goto exit;

    DWORD metaDataSize; 
    if (FAILED(hr=pENCEmitter->GetDeltaSaveSize(cssAccurate, &metaDataSize))) goto exit;

    wcscat_s(pwzOutputFilename,MAX_SCOPE_LENGTH,W(".dmeta"));
    pENCEmitter->SaveDelta(pwzOutputFilename,0); // second arg (dwFlags) is not used
    *pEnd = 0;
    pENCEmitter->Release();

    // apply delta to create basis for the next ENC iteration
    if(m_pbsMD)
    {
        IMetaDataEmit2* pBaseMDEmit = NULL;
        if(FAILED(hr = m_pDisp->OpenScopeOnMemory(m_pbsMD->ptr(),
                                                  m_pbsMD->length(),
                                                  ofWrite,
                                                  IID_IMetaDataEmit2,
                                    (IUnknown **)&pBaseMDEmit))) goto exit;
    
        if(FAILED(hr = pBaseMDEmit->ApplyEditAndContinue((IUnknown*)m_pImporter))) goto exit;
        delete m_pbsMD;
        if((m_pbsMD = new BinStr()) != NULL)
        {
            DWORD cb;
            hr = pBaseMDEmit->GetSaveSize(cssAccurate,&cb);
            BYTE* pb = m_pbsMD->getBuff(cb);
            hr = pBaseMDEmit->SaveToMemory(pb,cb);
        }
        pBaseMDEmit->Release();
    }


    // release all interfaces
    if (m_pSymWriter != NULL)
    {
        m_pSymWriter->Close();
        m_pSymWriter->Release();
        m_pSymWriter = NULL;
    }
    if (m_pImporter != NULL)
    {
        m_pImporter->Release();
        m_pImporter = NULL;
    }
    if (m_pEmitter != NULL)
    {
        m_pEmitter->Release();
        m_pEmitter = NULL;
    }

    return S_OK;

REPT_STEP
    
    // set managed resource entry, if any
    if(m_pManifest && m_pManifest->m_dwMResSizeTotal)
    {
        mresourceSize = m_pManifest->m_dwMResSizeTotal;

        if (FAILED(hr=m_pCeeFileGen->GetSectionBlock(m_pILSection, mresourceSize, 
                                            sizeof(DWORD), (void**) &mresourceData))) goto exit; 
        if (FAILED(hr=m_pCeeFileGen->SetManifestEntry(m_pCeeFile, mresourceSize, 0))) goto exit;
    }
REPT_STEP
    /*
    if (m_fWindowsCE)
    {
        if (FAILED(hr=m_pCeeFileGen->SetSubsystem(m_pCeeFile, IMAGE_SUBSYSTEM_WINDOWS_CE_GUI, 2, 10))) goto exit;

        if (FAILED(hr=m_pCeeFileGen->SetImageBase(m_pCeeFile, 0x10000))) goto exit;
    }
    else if(m_dwSubsystem != (DWORD)-1)
    {
        if (FAILED(hr=m_pCeeFileGen->SetSubsystem(m_pCeeFile, m_dwSubsystem, 4, 0))) goto exit;
    }
    
    if (FAILED(hr=m_pCeeFileGen->ClearComImageFlags(m_pCeeFile, COMIMAGE_FLAGS_ILONLY))) goto exit;
    if (FAILED(hr=m_pCeeFileGen->SetComImageFlags(m_pCeeFile, m_dwComImageFlags & ~COMIMAGE_FLAGS_STRONGNAMESIGNED))) goto exit;

    if(m_dwFileAlignment)
    {
        if(FAILED(hr=m_pCeeFileGen->SetFileAlignment(m_pCeeFile, m_dwFileAlignment))) goto exit;
    }
    if(m_stBaseAddress)
    {
        if(FAILED(hr=m_pCeeFileGen->SetImageBase(m_pCeeFile, m_stBaseAddress))) goto exit;
    }
    */
REPT_STEP
        //Compute all the RVAs
    if (FAILED(hr=m_pCeeFileGen->LinkCeeFile(m_pCeeFile))) goto exit;

REPT_STEP
        // Fix up any fields that have RVA associated with them
/*
    if (m_fHaveFieldsWithRvas) {
        hr = S_OK;
        ULONG dataSectionRVA;
        if (FAILED(hr=m_pCeeFileGen->GetSectionRVA(m_pGlobalDataSection, &dataSectionRVA))) goto exit;
        
        ULONG tlsSectionRVA;
        if (FAILED(hr=m_pCeeFileGen->GetSectionRVA(m_pTLSSection, &tlsSectionRVA))) goto exit;

        FieldDescriptor* pListFD;
        Class* pClass;
        for(int i=0; (pClass = m_lstClass.PEEK(i)); i++)
        {
            for(int j=0; (pListFD = pClass->m_FieldDList.PEEK(j)); j++)
            {
                if (pListFD->m_rvaLabel != 0) 
                {
                    DWORD rva;
                    if(*(pListFD->m_rvaLabel)=='@')
                    {
                        rva = (DWORD)atoi(pListFD->m_rvaLabel + 1);
                    }
                    else
                    {
                        GlobalLabel *pLabel = FindGlobalLabel(pListFD->m_rvaLabel);
                        if (pLabel == 0)
                        {
                            report->msg("Error:Could not find label '%s' for the field '%s'\n", pListFD->m_rvaLabel, pListFD->m_szName);
                            hr = E_FAIL;
                            continue;
                        }
                    
                        rva = pLabel->m_GlobalOffset;
                        if (pLabel->m_Section == m_pTLSSection)
                            rva += tlsSectionRVA;
                        else {
                            _ASSERTE(pLabel->m_Section == m_pGlobalDataSection);
                            rva += dataSectionRVA;
                        }
                    }
                    if (FAILED(m_pEmitter->SetFieldRVA(pListFD->m_fdFieldTok, rva))) goto exit;
                }
            }
        }
        if (FAILED(hr)) goto exit;
    }
REPT_STEP
*/

REPT_STEP
    // actually output the resources
    if(mresourceSize && mresourceData)
    {
        size_t i, N = m_pManifest->m_dwMResNum, sizeread, L;
        BYTE    *ptr = (BYTE*)mresourceData;
        BOOL    mrfail = FALSE;
        FILE*   pFile = NULL;
        char sz[2048];
        for(i=0; i < N; i++)
        {
            if(!m_pManifest->m_fMResNew[i]) continue;
            m_pManifest->m_fMResNew[i] = FALSE;
            memset(sz,0,2048);
            WszWideCharToMultiByte(CP_ACP,0,m_pManifest->m_wzMResName[i],-1,sz,2047,NULL,NULL);
            L = m_pManifest->m_dwMResSize[i];
            sizeread = 0;
            memcpy(ptr,&L,sizeof(DWORD));
            ptr += sizeof(DWORD);
            pFile = NULL;
            if(fopen_s(&pFile,sz,"rb")==0)
            {
                sizeread = fread((void *)ptr,1,L,pFile);
                fclose(pFile);
                ptr += sizeread;
            }
            else
            {
                report->msg("Error: failed to open mgd resource file '%ls'\n",m_pManifest->m_wzMResName[i]);
                mrfail = TRUE;
            }
            if(sizeread < L)
            {
                report->msg("Error: failed to read expected %d bytes from mgd resource file '%ls'\n",L,m_pManifest->m_wzMResName[i]);
                mrfail = TRUE;
                L -= sizeread;
                memset(ptr,0,L);
                ptr += L;
            }
        }
        if(mrfail) 
        { 
            hr = E_FAIL;
            goto exit;
        }
    }
REPT_STEP

    // Generate the file -- moved to main
    //if (FAILED(hr=m_pCeeFileGen->GenerateCeeFile(m_pCeeFile))) goto exit;


    hr = S_OK;

exit:
    return hr;
}
