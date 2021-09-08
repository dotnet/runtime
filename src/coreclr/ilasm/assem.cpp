// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: assem.cpp
//

//
// COM+ IL assembler
//
#include "ilasmpch.h"

#define INITGUID

#define DECLARE_DATA

#include "assembler.h"

void indexKeywords(Indx* indx); // defined in asmparse.y

unsigned int g_uCodePage = CP_ACP;
unsigned int g_uConsoleCP = CP_ACP;

char g_szSourceFileName[MAX_FILENAME_LENGTH*3];

WCHAR wzUniBuf[dwUniBuf];      // Unicode conversion global buffer

Assembler::Assembler()
{
    m_pDisp = NULL;
    m_pEmitter = NULL;
    m_pImporter = NULL;

    char* pszFQN = new char[16];
    strcpy_s(pszFQN,16,"<Module>");
    m_pModuleClass = new Class(pszFQN);
    m_lstClass.PUSH(m_pModuleClass);
    m_hshClass.PUSH(m_pModuleClass);
    m_pModuleClass->m_cl = mdTokenNil;
    m_pModuleClass->m_bIsMaster = FALSE;

    m_fStdMapping   = FALSE;
    m_fDisplayTraceOutput= FALSE;
    m_fTolerateDupMethods = FALSE;

    m_pCurOutputPos = NULL;

    m_CurPC             = 0;    // PC offset in method
    m_pCurMethod        = NULL;
    m_pCurClass         = NULL;
    m_pCurEvent         = NULL;
    m_pCurProp          = NULL;

    m_wzMetadataVersion = NULL;
    m_wMSVmajor = 0xFFFF;
    m_wMSVminor = 0xFFFF;

    m_wSSVersionMajor = 4;
    m_wSSVersionMinor = 0;
    m_fAppContainer = FALSE;
    m_fHighEntropyVA = FALSE;

    m_pCeeFileGen            = NULL;
    m_pCeeFile               = 0;

    m_pManifest         = NULL;

    m_pCustomDescrList  = NULL;

    m_pGlobalDataSection = NULL;
    m_pILSection = NULL;
    m_pTLSSection = NULL;

    m_fDidCoInitialise = FALSE;

    m_fDLL = FALSE;
    m_fEntryPointPresent = FALSE;
    m_fHaveFieldsWithRvas = FALSE;
    m_fFoldCode = FALSE;
    m_dwMethodsFolded = 0;

    m_szScopeName[0] = 0;
    m_crExtends = mdTypeDefNil;

    m_nImplList = 0;
    m_TyParList = NULL;

    m_SEHD = NULL;
    m_firstArgName = NULL;
    m_lastArgName = NULL;
    m_szNamespace = new char[2];
    m_szNamespace[0] = 0;
    m_NSstack.PUSH(m_szNamespace);

    m_szFullNS = new char[MAX_NAMESPACE_LENGTH];
    memset(m_szFullNS,0,MAX_NAMESPACE_LENGTH);
    m_ulFullNSLen = MAX_NAMESPACE_LENGTH;

    m_State             = STATE_OK;
    m_fInitialisedMetaData = FALSE;
    m_fAutoInheritFromObject = TRUE;

    m_ulLastDebugLine = 0xFFFFFFFF;
    m_ulLastDebugColumn = 0xFFFFFFFF;
    m_ulLastDebugLineEnd = 0xFFFFFFFF;
    m_ulLastDebugColumnEnd = 0xFFFFFFFF;
    m_dwIncludeDebugInfo = 0;
    m_fGeneratePDB = FALSE;
    m_fIsMscorlib = FALSE;
    m_fOptimize = FALSE;
    m_tkSysObject = 0;
    m_tkSysString = 0;
    m_tkSysValue = 0;
    m_tkSysEnum = 0;

    m_pVTable = NULL;
    m_pMarshal = NULL;
    m_pPInvoke = NULL;

    m_fReportProgress = TRUE;
    m_tkCurrentCVOwner = 1; // module
    m_pOutputBuffer = NULL;

    m_dwSubsystem = (DWORD)-1;
    m_dwComImageFlags = COMIMAGE_FLAGS_ILONLY;
    m_dwFileAlignment = 0;
    m_stBaseAddress = 0;
    m_stSizeOfStackReserve = 0;
    m_dwCeeFileFlags = ICEE_CREATE_FILE_PURE_IL;

    g_szSourceFileName[0] = 0;

    m_guidLang = CorSym_LanguageType_ILAssembly;
    m_guidLangVendor = CorSym_LanguageVendor_Microsoft;
    m_guidDoc = CorSym_DocumentType_Text;
    for(int i=0; i<INSTR_POOL_SIZE; i++) m_Instr[i].opcode = -1;
    m_wzResourceFile = NULL;
    m_wzKeySourceName = NULL;
    OnErrGo = false;
    bClock = NULL;

    m_pbsMD = NULL;

    m_pOutputBuffer = new BYTE[OUTPUT_BUFFER_SIZE];

    m_pCurOutputPos = m_pOutputBuffer;
    m_pEndOutputPos = m_pOutputBuffer + OUTPUT_BUFFER_SIZE;

    m_crImplList = new mdTypeRef[MAX_INTERFACES_IMPLEMENTED];
    m_nImplListSize = MAX_INTERFACES_IMPLEMENTED;

    m_pManifest = new AsmMan((void*)this);

    dummyClass = new Class(NULL);
    indexKeywords(&indxKeywords);

    m_pPortablePdbWriter = NULL;
}


Assembler::~Assembler()
{
    if(m_pbsMD) delete m_pbsMD;

    if(m_pMarshal) delete m_pMarshal;
    if(m_pManifest) delete m_pManifest;
    if(m_pPInvoke) delete m_pPInvoke;

    if(m_pVTable) delete m_pVTable;

    m_lstGlobalLabel.RESET(true);
    m_lstGlobalFixup.RESET(true);
    m_hshClass.RESET(false);
    m_lstClass.RESET(true);
    while((m_ClassStack.POP()));
    while(m_CustomDescrListStack.POP());
    m_pCurClass = NULL;
    dummyClass->m_szFQN = NULL;
    delete dummyClass;

    if (m_pOutputBuffer)    delete [] m_pOutputBuffer;
    if (m_crImplList)       delete [] m_crImplList;
    if (m_TyParList)        delete m_TyParList;

    if (m_pCeeFileGen != NULL) {
        if (m_pCeeFile)
            m_pCeeFileGen->DestroyCeeFile(&m_pCeeFile);

        DestroyICeeFileGen(&m_pCeeFileGen);

        m_pCeeFileGen = NULL;
    }

    while((m_szNamespace = m_NSstack.POP())) ;
    delete [] m_szFullNS;

    m_MethodBodyList.RESET(true);

    m_TypeDefDList.RESET(true);

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
    if (m_pPortablePdbWriter != NULL)
    {
        delete m_pPortablePdbWriter;
        m_pPortablePdbWriter = NULL;
    }
    if (m_pDisp != NULL)
    {
        m_pDisp->Release();
        m_pDisp = NULL;
    }
}


BOOL Assembler::Init(BOOL generatePdb)
{
    if (m_pCeeFileGen != NULL) {
        if (m_pCeeFile)
            m_pCeeFileGen->DestroyCeeFile(&m_pCeeFile);

        DestroyICeeFileGen(&m_pCeeFileGen);

        m_pCeeFileGen = NULL;
    }

    if (FAILED(CreateICeeFileGen(&m_pCeeFileGen))) return FALSE;

    if (FAILED(m_pCeeFileGen->CreateCeeFileEx(&m_pCeeFile,(ULONG)m_dwCeeFileFlags))) return FALSE;

    if (FAILED(m_pCeeFileGen->GetSectionCreate(m_pCeeFile, ".il", sdReadOnly, &m_pILSection))) return FALSE;
    if (FAILED(m_pCeeFileGen->GetSectionCreate (m_pCeeFile, ".sdata", sdReadWrite, &m_pGlobalDataSection))) return FALSE;
    if (FAILED(m_pCeeFileGen->GetSectionCreate (m_pCeeFile, ".tls", sdReadWrite, &m_pTLSSection))) return FALSE;

    m_fGeneratePDB = generatePdb;

    return TRUE;
}

void Assembler::SetDLL(BOOL IsDll)
{
    HRESULT OK;
    OK = m_pCeeFileGen->SetDllSwitch(m_pCeeFile, IsDll);
    _ASSERTE(SUCCEEDED(OK));

    m_fDLL = IsDll;
}

void Assembler::ResetArgNameList()
{
    if(m_firstArgName) delArgNameList(m_firstArgName);
    m_firstArgName = NULL;
    m_lastArgName = NULL;
}

void Assembler::ResetForNextMethod()
{

    ResetArgNameList();

    m_CurPC         = 0;
    m_pCurOutputPos = m_pOutputBuffer;
    m_State         = STATE_OK;
    m_pCurMethod = NULL;
}

void Assembler::ResetLineNumbers()
{
    // reset line number information
    m_ulLastDebugLine = 0xFFFFFFFF;
    m_ulLastDebugColumn = 0xFFFFFFFF;
    m_ulLastDebugLineEnd = 0xFFFFFFFF;
    m_ulLastDebugColumnEnd = 0xFFFFFFFF;
}

BOOL Assembler::AddMethod(Method *pMethod)
{
    BOOL                     fIsInterface=FALSE, fIsImport=FALSE;
    ULONG                    PEFileOffset=0;

    _ASSERTE(m_pCeeFileGen != NULL);
    if (pMethod == NULL)
    {
        report->error("pMethod == NULL");
        return FALSE;
    }
    if(pMethod->m_pClass != NULL)
    {
        fIsInterface = IsTdInterface(pMethod->m_pClass->m_Attr);
        fIsImport = IsTdImport(pMethod->m_pClass->m_Attr);
    }
    if(m_CurPC)
    {
        char sz[1024];
        sz[0] = 0;
        if(fIsImport) strcat_s(sz,1024," imported");
        if(IsMdAbstract(pMethod->m_Attr)) strcat_s(sz,1024," abstract");
        if(IsMdPinvokeImpl(pMethod->m_Attr)) strcat_s(sz,1024," pinvoke");
        if(!IsMiIL(pMethod->m_wImplAttr)) strcat_s(sz,1024," non-IL");
        if(IsMiRuntime(pMethod->m_wImplAttr)) strcat_s(sz,1024," runtime-supplied");
        if(IsMiInternalCall(pMethod->m_wImplAttr)) strcat_s(sz,1024," an internal call");
        if(strlen(sz))
        {
            report->error("Method cannot have body if it is%s\n",sz);
        }
    }
    else // method has no body
    {
        if(fIsImport || IsMdAbstract(pMethod->m_Attr) || IsMdPinvokeImpl(pMethod->m_Attr)
           || IsMiRuntime(pMethod->m_wImplAttr) || IsMiInternalCall(pMethod->m_wImplAttr)) return TRUE;
        if(OnErrGo)
        {
            report->error("Method has no body\n");
            return TRUE;
        }
        else
        {
            report->warn("Method has no body, 'ret' emitted\n");
            Instr* pIns = GetInstr();
            if(pIns)
            {
                memset(pIns,0,sizeof(Instr));
                pIns->opcode = CEE_RET;
                EmitOpcode(pIns);
            }
        }
    }

    if(pMethod->m_Locals.COUNT()) pMethod->m_LocalsSig=0x11000001; // placeholder, the real token 2b defined in EmitMethod

    COR_ILMETHOD_FAT fatHeader;
    fatHeader.SetFlags(pMethod->m_Flags);
    fatHeader.SetMaxStack(pMethod->m_MaxStack);
    fatHeader.SetLocalVarSigTok(pMethod->m_LocalsSig);
    fatHeader.SetCodeSize(m_CurPC);
    bool moreSections = (pMethod->m_dwNumExceptions != 0);

    // if max stack is specified <8, force fat header, otherwise (with tiny header) it will default to 8
    if((fatHeader.GetMaxStack() < 8)&&(fatHeader.GetLocalVarSigTok()==0)&&(fatHeader.GetCodeSize()<64)&&(!moreSections))
        fatHeader.SetFlags(fatHeader.GetFlags() | CorILMethod_InitLocals); //forces fat header but does nothing else, since LocalVarSigTok==0

    unsigned codeSize = m_CurPC;
    unsigned codeSizeAligned = codeSize;
    if (moreSections)
        codeSizeAligned = (codeSizeAligned + 3) & ~3;    // to insure EH section aligned

    unsigned headerSize = COR_ILMETHOD::Size(&fatHeader, moreSections);
    unsigned ehSize     = COR_ILMETHOD_SECT_EH::Size(pMethod->m_dwNumExceptions, pMethod->m_ExceptionList);
    unsigned totalSize  = headerSize + codeSizeAligned + ehSize;

    BYTE* outBuff;
    BYTE* endbuf;
    BinStr* pbsBody;
    if((pbsBody = new BinStr())==NULL) return FALSE;
    if((outBuff = pbsBody->getBuff(totalSize))==NULL) return FALSE;
    endbuf = &outBuff[totalSize];

    // Emit the header
    outBuff += COR_ILMETHOD::Emit(headerSize, &fatHeader, moreSections, outBuff);

    pMethod->m_pCode = outBuff;
    pMethod->m_headerOffset= PEFileOffset;
    pMethod->m_methodOffset= PEFileOffset + headerSize;
    pMethod->m_CodeSize = codeSize;

    // Emit the code
    if (codeSizeAligned)
    {
        memset(outBuff,0,codeSizeAligned);
        memcpy(outBuff, m_pOutputBuffer, codeSize);
        outBuff += codeSizeAligned;
    }

    if(pMethod->m_dwNumExceptions)
    {
        // Validate the eh
        COR_ILMETHOD_SECT_EH_CLAUSE_FAT* pEx;
        DWORD   TryEnd,HandlerEnd, dwEx, dwEf;
        for(dwEx = 0, pEx = pMethod->m_ExceptionList; dwEx < pMethod->m_dwNumExceptions; dwEx++, pEx++)
        {
            if(pEx->GetTryOffset() > m_CurPC) // i.e., pMethod->m_CodeSize
            {
                report->error("Invalid SEH clause #%d: Try block starts beyond code size\n",dwEx+1);
            }
            TryEnd = pEx->GetTryOffset()+pEx->GetTryLength();
            if(TryEnd > m_CurPC)
            {
                report->error("Invalid SEH clause #%d: Try block ends beyond code size\n",dwEx+1);
            }
            if(pEx->GetHandlerOffset() > m_CurPC)
            {
                report->error("Invalid SEH clause #%d: Handler block starts beyond code size\n",dwEx+1);
            }
            HandlerEnd = pEx->GetHandlerOffset()+pEx->GetHandlerLength();
            if(HandlerEnd > m_CurPC)
            {
                report->error("Invalid SEH clause #%d: Handler block ends beyond code size\n",dwEx+1);
            }
            if(pEx->Flags & COR_ILEXCEPTION_CLAUSE_FILTER)
            {
                if(!((pEx->GetFilterOffset() >= TryEnd)||(pEx->GetTryOffset() >= HandlerEnd)))
                {
                    report->error("Invalid SEH clause #%d: Try and Filter/Handler blocks overlap\n",dwEx+1);
                }
                for(dwEf = 0; dwEf < pMethod->m_dwNumEndfilters; dwEf++)
                {
                    if(pMethod->m_EndfilterOffsetList[dwEf] == pEx->GetHandlerOffset()) break;
                }
                if(dwEf >= pMethod->m_dwNumEndfilters)
                {
                    report->error("Invalid SEH clause #%d: Filter block separated from Handler, or not ending with endfilter\n",dwEx+1);
                }
            }
            else
            if(!((pEx->GetHandlerOffset() >= TryEnd)||(pEx->GetTryOffset() >= HandlerEnd)))
            {
                report->error("Invalid SEH clause #%d: Try and Handler blocks overlap\n",dwEx+1);
            }

        }
        // Emit the eh
        outBuff += COR_ILMETHOD_SECT_EH::Emit(ehSize, pMethod->m_dwNumExceptions,
                                    pMethod->m_ExceptionList, false, outBuff);
    }
    _ASSERTE(outBuff == endbuf);

    pMethod->m_pbsBody = pbsBody;

    LocalMemberRefFixup*             pMRF;
    while((pMRF = pMethod->m_LocalMemberRefFixupList.POP()))
    {
        pMRF->offset += (size_t)(pMethod->m_pCode);
        m_LocalMemberRefFixupList.PUSH(pMRF); // transfer MRF to assembler's list
    }

    if(m_fReportProgress)
    {
        if (pMethod->IsGlobalMethod())
            report->msg("Assembled global method %s\n", pMethod->m_szName);
        else report->msg("Assembled method %s::%s\n", pMethod->m_pClass->m_szFQN,
                  pMethod->m_szName);
    }
    return TRUE;
}


BOOL Assembler::EmitMethodBody(Method* pMethod, BinStr* pbsOut)
{
    HRESULT hr = S_OK;

    if(pMethod)
    {
        BinStr* pbsBody = pMethod->m_pbsBody;
        unsigned totalSize;
        if(pbsBody && (totalSize = pbsBody->length()))
        {
            unsigned headerSize = pMethod->m_methodOffset-pMethod->m_headerOffset;
            MethodBody* pMB = NULL;
            // ----------emit locals signature-------------------
            unsigned uLocals;
            if((uLocals = pMethod->m_Locals.COUNT()))
            {
                VarDescr* pVD;
                BinStr*   pbsSig = new BinStr();
                unsigned cnt;
                DWORD   cSig;
                const COR_SIGNATURE* mySig;

                pbsSig->appendInt8(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG);
                cnt = CorSigCompressData(uLocals,pbsSig->getBuff(5));
                pbsSig->remove(5-cnt);
                for(cnt = 0; (pVD = pMethod->m_Locals.PEEK(cnt)); cnt++)
                {
                    if(pVD->pbsSig) pbsSig->append(pVD->pbsSig);
                    else
                    {
                        report->error("Undefined type of local var slot %d in method %s\n",cnt,pMethod->m_szName);
                        pbsSig->appendInt8(ELEMENT_TYPE_I4);
                    }
                }

                cSig = pbsSig->length();
                mySig = (const COR_SIGNATURE *)(pbsSig->ptr());

                if (cSig > 1)    // non-empty signature
                {
                    hr = m_pEmitter->GetTokenFromSig(mySig, cSig, &pMethod->m_LocalsSig);
                    _ASSERTE(SUCCEEDED(hr));
                }
                delete pbsSig;
                COR_ILMETHOD_FAT* pFH; // Fat header guaranteed, because there are local vars
                pFH = (COR_ILMETHOD_FAT*)(pMethod->m_pbsBody->ptr());
                pFH->SetLocalVarSigTok(pMethod->m_LocalsSig);
            }

            if(m_fFoldCode)
            {
                for(int k=0; (pMB = m_MethodBodyList.PEEK(k)) != NULL; k++)
                {
                    if((pMB->pbsBody->length() == totalSize)
                      && (memcmp(pMB->pbsBody->ptr(), pbsBody->ptr(),totalSize)==0))
                    break;
                }
                if(pMB)
                {
                    pMethod->m_headerOffset= pMB->RVA;
                    pMethod->m_methodOffset= pMB->RVA + headerSize;
                    pMethod->m_pCode = pMB->pCode;
                    delete pbsBody;
                    pMethod->m_pbsBody = NULL;
                    m_dwMethodsFolded++;
                }
            }
            if(pMB == NULL)
            {
                BYTE* outBuff;
                unsigned align = (headerSize == 1)? 1 : 4;
                ULONG    PEFileOffset, methodRVA;

                if (FAILED(m_pCeeFileGen->GetSectionBlock (m_pILSection, totalSize,
                        align, (void **) &outBuff)))    return FALSE;
                memcpy(outBuff,pbsBody->ptr(),totalSize);
                // The offset where we start, (not where the alignment bytes start!
                if (FAILED(m_pCeeFileGen->GetSectionDataLen (m_pILSection, &PEFileOffset)))
                    return FALSE;
                PEFileOffset -= totalSize;

                pMethod->m_pCode = outBuff + headerSize;
                pMethod->m_headerOffset= PEFileOffset;
                pMethod->m_methodOffset= PEFileOffset + headerSize;
                DoDeferredILFixups(pMethod);

                m_pCeeFileGen->GetMethodRVA(m_pCeeFile, PEFileOffset,&methodRVA);

                pMethod->m_headerOffset= methodRVA;
                pMethod->m_methodOffset= methodRVA + headerSize;
                if(m_fFoldCode)
                {
                    if((pMB = new MethodBody)==NULL) return FALSE;
                    pMB->pbsBody = pbsBody;
                    pMB->RVA = methodRVA;
                    pMB->pCode = pMethod->m_pCode;
                    m_MethodBodyList.PUSH(pMB);
                }
                //else
                //    delete pbsBody;
                //pMethod->m_pbsBody = NULL;
            }
            m_pEmitter->SetRVA(pMethod->m_Tok,pMethod->m_headerOffset);
        }

        if (m_fGeneratePDB)
        {
            if (FAILED(m_pPortablePdbWriter->DefineSequencePoints(pMethod)))
                return FALSE;
            if (FAILED(m_pPortablePdbWriter->DefineLocalScope(pMethod)))
                return FALSE;
        }

        return TRUE;
    }
    else return FALSE;
}

ImportDescriptor* Assembler::EmitImport(BinStr* DllName)
{
    int i = 0, l = 0;
    ImportDescriptor*   pID;
    char* sz=NULL;

    if(DllName) l = DllName->length();  // No zero terminator here!
    if(l)
    {
        sz = (char*)DllName->ptr();
        while((pID=m_ImportList.PEEK(i++)))
        {
            if((pID->dwDllName== (DWORD) l)&& !memcmp(pID->szDllName,sz,l)) return pID;
        }
    }
    else
    {
        while((pID=m_ImportList.PEEK(i++)))
        {
            if(pID->dwDllName==0) return pID;
        }
    }
    if((pID = new ImportDescriptor(sz,l)))
    {
        m_ImportList.PUSH(pID);
        pID->mrDll = TokenFromRid(m_ImportList.COUNT(),mdtModuleRef);
        return pID;
    }
    else report->error("Failed to allocate import descriptor\n");
    return NULL;
}

void Assembler::EmitImports()
{
    WCHAR*               wzDllName=&wzUniBuf[0];
    ImportDescriptor*   pID;
    int i;
    mdToken tk;
    for(i=0; (pID = m_ImportList.PEEK(i)); i++)
    {
        WszMultiByteToWideChar(g_uCodePage,0,pID->szDllName,-1,wzDllName,dwUniBuf-1);
        if(FAILED(m_pEmitter->DefineModuleRef(             // S_OK or error.
                            wzDllName,            // [IN] DLL name
                            &tk)))      // [OUT] returned
            report->error("Failed to define module ref '%s'\n",pID->szDllName);
        else
            _ASSERTE(tk == pID->mrDll);
    }
}

HRESULT Assembler::EmitPinvokeMap(mdToken tk, PInvokeDescriptor* pDescr)
{
    WCHAR*               wzAlias=&wzUniBuf[0];

    if(pDescr->szAlias) WszMultiByteToWideChar(g_uCodePage,0,pDescr->szAlias,-1,wzAlias,dwUniBuf-1);

    return m_pEmitter->DefinePinvokeMap(        // Return code.
                        tk,                     // [IN] FieldDef, MethodDef or MethodImpl.
                        pDescr->dwAttrs,        // [IN] Flags used for mapping.
                        (LPCWSTR)wzAlias,       // [IN] Import name.
                        pDescr->mrDll);         // [IN] ModuleRef token for the target DLL.
}

BOOL Assembler::EmitMethod(Method *pMethod)
{
// Emit the metadata for a method definition
    BOOL                fSuccess = FALSE;
    WCHAR*              wzMemberName=&wzUniBuf[0];
    BOOL                fIsInterface;
    DWORD               cSig;
    ULONG               methodRVA = 0;
    mdMethodDef         MethodToken;
    mdTypeDef           ClassToken = mdTypeDefNil;
    char                *pszMethodName;
    COR_SIGNATURE       *mySig;

    _ASSERTE((m_pCeeFileGen != NULL) && (pMethod != NULL));
    fIsInterface = ((pMethod->m_pClass != NULL) && IsTdInterface(pMethod->m_pClass->m_Attr));


    pszMethodName = pMethod->m_szName;
    mySig = pMethod->m_pMethodSig;
    cSig = pMethod->m_dwMethodCSig;

    // If  this is an instance method, make certain the signature says so

    if (!(pMethod->m_Attr & mdStatic))
        *mySig |= IMAGE_CEE_CS_CALLCONV_HASTHIS;

    ClassToken = (pMethod->IsGlobalMethod())? mdTokenNil
                                    : pMethod->m_pClass->m_cl;
    // Convert name to UNICODE
    WszMultiByteToWideChar(g_uCodePage,0,pszMethodName,-1,wzMemberName,dwUniBuf-1);

    if(IsMdPrivateScope(pMethod->m_Attr))
    {
        WCHAR* p = wcsstr(wzMemberName,W("$PST06"));
        if(p) *p = 0;
    }

    if (FAILED(m_pEmitter->DefineMethod(ClassToken,       // parent class
                                      wzMemberName,     // member name
                                      pMethod->m_Attr & ~mdReservedMask,  // member attributes
                                      mySig, // member signature
                                      cSig,
                                      methodRVA,                // RVA
                                      pMethod->m_wImplAttr,                // implflags
                                      &MethodToken)))
    {
        report->error("Failed to define method '%s'\n",pszMethodName);
        goto exit;
    }
    pMethod->m_Tok = MethodToken;
    //--------------------------------------------------------------------------------
    // the only way to set mdRequireSecObject:
    if(pMethod->m_Attr & mdRequireSecObject)
    {
        mdToken tkPseudoClass;
        if(FAILED(m_pEmitter->DefineTypeRefByName(1, COR_REQUIRES_SECOBJ_ATTRIBUTE, &tkPseudoClass)))
            report->error("Unable to define type reference '%s'\n", COR_REQUIRES_SECOBJ_ATTRIBUTE_ANSI);
        else
        {
            mdToken tkPseudoCtor;
            BYTE bSig[3] = {IMAGE_CEE_CS_CALLCONV_HASTHIS,0,ELEMENT_TYPE_VOID};
            if(FAILED(m_pEmitter->DefineMemberRef(tkPseudoClass, W(".ctor"), (PCCOR_SIGNATURE)bSig, 3, &tkPseudoCtor)))
                report->error("Unable to define member reference '%s::.ctor'\n", COR_REQUIRES_SECOBJ_ATTRIBUTE_ANSI);
            else DefineCV(new CustomDescr(MethodToken,tkPseudoCtor,NULL));
        }
    }

    if (pMethod->m_NumTyPars)
    {
        ULONG i;
        mdToken tkNil = mdTokenNil;
        mdGenericParam tkGP = mdTokenNil;
        for(i = 0; i < pMethod->m_NumTyPars; i++)
        {
            if (FAILED(m_pEmitter->DefineGenericParam(MethodToken, i, pMethod->m_TyPars[i].Attrs(), pMethod->m_TyPars[i].Name(), 0, &tkNil, &tkGP)))
            {
                report->error("Unable to define generic param: %s'\n", pMethod->m_TyPars[i].Name());
            }
            else
            {
                pMethod->m_TyPars[i].Token(tkGP);
                EmitCustomAttributes(tkGP, pMethod->m_TyPars[i].CAList());
            }
        }
        EmitGenericParamConstraints(pMethod->m_NumTyPars, pMethod->m_TyPars, pMethod->m_Tok, &(pMethod->m_GPCList));
    }
    //--------------------------------------------------------------------------------
    EmitSecurityInfo(MethodToken,
                     pMethod->m_pPermissions,
                     pMethod->m_pPermissionSets);
    //--------------------------------------------------------------------------------
    if (pMethod->m_fEntryPoint)
    {
        if(fIsInterface) report->error("Entrypoint in Interface: Method '%s'\n",pszMethodName);

        if (FAILED(m_pCeeFileGen->SetEntryPoint(m_pCeeFile, MethodToken)))
        {
            report->error("Failed to set entry point for method '%s'\n",pszMethodName);
            goto exit;
        }

    }
    //--------------------------------------------------------------------------------
    if(IsMdPinvokeImpl(pMethod->m_Attr))
    {
        if(pMethod->m_pPInvoke)
        {
            HRESULT hr;
            if(pMethod->m_pPInvoke->szAlias == NULL) pMethod->m_pPInvoke->szAlias = pszMethodName;
            hr = EmitPinvokeMap(MethodToken,pMethod->m_pPInvoke);
            if(pMethod->m_pPInvoke->szAlias == pszMethodName) pMethod->m_pPInvoke->szAlias = NULL;

            if(FAILED(hr))
            {
                report->error("Failed to set PInvoke map for method '%s'\n",pszMethodName);
                goto exit;
            }
        }
    }

    { // add parameters to metadata
        void const *pValue=NULL;
        ULONG       cbValue;
        DWORD dwCPlusTypeFlag=0;
        mdParamDef pdef;
        WCHAR* wzParName=&wzUniBuf[0];
        char*  szPhonyName=(char*)&wzUniBuf[dwUniBuf >> 1];
        if(pMethod->m_dwRetAttr || pMethod->m_pRetMarshal || pMethod->m_RetCustDList.COUNT())
        {
            if(pMethod->m_pRetValue)
            {
                dwCPlusTypeFlag= (DWORD)*(pMethod->m_pRetValue->ptr());
                pValue = (void const *)(pMethod->m_pRetValue->ptr()+1);
                cbValue = pMethod->m_pRetValue->length()-1;
                if(dwCPlusTypeFlag == ELEMENT_TYPE_STRING)
                {
                    cbValue /= sizeof(WCHAR);
#if BIGENDIAN
                    void* pValueTemp = _alloca(cbValue * sizeof(WCHAR));
                    memcpy(pValueTemp, pValue, cbValue * sizeof(WCHAR));
                    pValue = pValueTemp;

                    SwapStringLength((WCHAR*)pValue, cbValue);
#endif
                }
            }
            else
            {
                pValue = NULL;
                cbValue = (ULONG)-1;
                dwCPlusTypeFlag=0;
            }
            m_pEmitter->DefineParam(MethodToken,0,NULL,pMethod->m_dwRetAttr,dwCPlusTypeFlag,pValue,cbValue,&pdef);

            if(pMethod->m_pRetMarshal)
            {
                if(FAILED(m_pEmitter->SetFieldMarshal (
                                            pdef,                       // [IN] given a fieldDef or paramDef token
                            (PCCOR_SIGNATURE)(pMethod->m_pRetMarshal->ptr()),   // [IN] native type specification
                                            pMethod->m_pRetMarshal->length())))  // [IN] count of bytes of pvNativeType
                    report->error("Failed to set param marshaling for return\n");

            }
            EmitCustomAttributes(pdef, &(pMethod->m_RetCustDList));
        }
        for(ARG_NAME_LIST *pAN=pMethod->m_firstArgName; pAN; pAN = pAN->pNext)
        {
            if(pAN->nNum >= 65535)
            {
                report->error("Method '%s': Param.sequence number (%d) exceeds 65535, unable to define parameter\n",pszMethodName,pAN->nNum+1);
                continue;
            }
            if(pAN->dwName) strcpy_s(szPhonyName,dwUniBuf >> 1,pAN->szName);
            else sprintf_s(szPhonyName,(dwUniBuf >> 1),"A_%d",pAN->nNum);

            WszMultiByteToWideChar(g_uCodePage,0,szPhonyName,-1,wzParName,dwUniBuf >> 1);

            if(pAN->pValue)
            {
                dwCPlusTypeFlag= (DWORD)*(pAN->pValue->ptr());
                pValue = (void const *)(pAN->pValue->ptr()+1);
                cbValue = pAN->pValue->length()-1;
                if(dwCPlusTypeFlag == ELEMENT_TYPE_STRING)
                {
                    cbValue /= sizeof(WCHAR);
#if BIGENDIAN
                    void* pValueTemp = _alloca(cbValue * sizeof(WCHAR));
                    memcpy(pValueTemp, pValue, cbValue * sizeof(WCHAR));
                    pValue = pValueTemp;

                    SwapStringLength((WCHAR*)pValue, cbValue);
#endif
                }
            }
            else
            {
                pValue = NULL;
                cbValue = (ULONG)-1;
                dwCPlusTypeFlag=0;
            }
            m_pEmitter->DefineParam(MethodToken,pAN->nNum+1,wzParName,pAN->dwAttr,dwCPlusTypeFlag,pValue,cbValue,&pdef);
            if(pAN->pMarshal)
            {
                if(FAILED(m_pEmitter->SetFieldMarshal (
                                            pdef,                       // [IN] given a fieldDef or paramDef token
                            (PCCOR_SIGNATURE)(pAN->pMarshal->ptr()),   // [IN] native type specification
                                            pAN->pMarshal->length())))  // [IN] count of bytes of pvNativeType
                    report->error("Failed to set param marshaling for '%s'\n",pAN->szName);
            }
            EmitCustomAttributes(pdef, &(pAN->CustDList));
        }
    }
    fSuccess = TRUE;
    //--------------------------------------------------------------------------------
    // Update method implementations for this method
    {
        MethodImplDescriptor*   pMID;
        int i;
        for(i=0;(pMID = pMethod->m_MethodImplDList.PEEK(i));i++)
        {
            pMID->m_tkImplementingMethod = MethodToken;
            // don't delete it here, it's still in the general list
        }
    }
    //--------------------------------------------------------------------------------
    EmitCustomAttributes(MethodToken, &(pMethod->m_CustomDescrList));
exit:
    if (fSuccess == FALSE) m_State = STATE_FAIL;
    return fSuccess;
}

BOOL Assembler::EmitMethodImpls()
{
    MethodImplDescriptor*   pMID;
    BOOL ret = TRUE;
    int i;
    for(i=0; (pMID = m_MethodImplDList.PEEK(i)); i++)
    {
        pMID->m_tkImplementingMethod = ResolveLocalMemberRef(pMID->m_tkImplementingMethod);
        pMID->m_tkImplementedMethod = ResolveLocalMemberRef(pMID->m_tkImplementedMethod);
        if(FAILED(m_pEmitter->DefineMethodImpl( pMID->m_tkDefiningClass,
                                                pMID->m_tkImplementingMethod,
                                                pMID->m_tkImplementedMethod)))
        {
            report->error("Failed to define Method Implementation");
            ret = FALSE;
        }
        pMID->m_fNew = FALSE;
    }// end while
    return ret;
}

mdToken Assembler::ResolveLocalMemberRef(mdToken tok)
{
    if(TypeFromToken(tok) == 0x99000000)
    {
        tok = RidFromToken(tok);
        if(tok) tok = m_LocalMethodRefDList.PEEK(tok-1)->m_tkResolved;
    }
    else if(TypeFromToken(tok) == 0x98000000)
    {
        tok = RidFromToken(tok);
        if(tok) tok = m_LocalFieldRefDList.PEEK(tok-1)->m_tkResolved;
    }
    return tok;
}

BOOL Assembler::EmitEvent(EventDescriptor* pED)
{
    mdMethodDef mdAddOn=mdMethodDefNil,
                mdRemoveOn=mdMethodDefNil,
                mdFire=mdMethodDefNil,
                *mdOthers;
    int                 nOthers;
    WCHAR*              wzMemberName=&wzUniBuf[0];

    if(!pED) return FALSE;

    WszMultiByteToWideChar(g_uCodePage,0,pED->m_szName,-1,wzMemberName,dwUniBuf-1);

    mdAddOn = ResolveLocalMemberRef(pED->m_tkAddOn);
    if(TypeFromToken(mdAddOn) != mdtMethodDef)
    {
        report->error("Invalid Add method of event '%s'\n",pED->m_szName);
        return FALSE;
    }
    mdRemoveOn = ResolveLocalMemberRef(pED->m_tkRemoveOn);
    if(TypeFromToken(mdRemoveOn) != mdtMethodDef)
    {
        report->error("Invalid Remove method of event '%s'\n",pED->m_szName);
        return FALSE;
    }
    mdFire = ResolveLocalMemberRef(pED->m_tkFire);
    if((RidFromToken(mdFire)!=0)&&(TypeFromToken(mdFire) != mdtMethodDef))
    {
        report->error("Invalid Fire method of event '%s'\n",pED->m_szName);
        return FALSE;
    }

    nOthers = pED->m_tklOthers.COUNT();
    mdOthers = new mdMethodDef[nOthers+1];
    if(mdOthers == NULL)
    {
        report->error("Failed to allocate Others array for event descriptor\n");
        nOthers = 0;
    }
    for(int j=0; j < nOthers; j++)
    {
        mdOthers[j] = ResolveLocalMemberRef((mdToken)(UINT_PTR)(pED->m_tklOthers.PEEK(j)));     // @WARNING: casting down from 'mdToken*' to 'mdToken'
    }
    mdOthers[nOthers] = mdMethodDefNil; // like null-terminator

    if(FAILED(m_pEmitter->DefineEvent(  pED->m_tdClass,
                                        wzMemberName,
                                        pED->m_dwAttr,
                                        pED->m_tkEventType,
                                        mdAddOn,
                                        mdRemoveOn,
                                        mdFire,
                                        mdOthers,
                                        &(pED->m_edEventTok))))
    {
        report->error("Failed to define event '%s'.\n",pED->m_szName);
        delete [] mdOthers;
        return FALSE;
    }
    EmitCustomAttributes(pED->m_edEventTok, &(pED->m_CustomDescrList));
    return TRUE;
}

BOOL Assembler::EmitProp(PropDescriptor* pPD)
{
    mdMethodDef mdSet, mdGet, *mdOthers;
    int nOthers;
    WCHAR*              wzMemberName=&wzUniBuf[0];

    if(!pPD) return FALSE;

    WszMultiByteToWideChar(g_uCodePage,0,pPD->m_szName,-1,wzMemberName,dwUniBuf-1);

    mdSet = ResolveLocalMemberRef(pPD->m_tkSet);
    if((RidFromToken(mdSet)!=0)&&(TypeFromToken(mdSet) != mdtMethodDef))
    {
        report->error("Invalid Set method of property '%s'\n",pPD->m_szName);
        return FALSE;
    }
    mdGet = ResolveLocalMemberRef(pPD->m_tkGet);
    if((RidFromToken(mdGet)!=0)&&(TypeFromToken(mdGet) != mdtMethodDef))
    {
        report->error("Invalid Get method of property '%s'\n",pPD->m_szName);
        return FALSE;
    }

    nOthers = pPD->m_tklOthers.COUNT();
    mdOthers = new mdMethodDef[nOthers+1];
    if(mdOthers == NULL)
    {
        report->error("Failed to allocate Others array for prop descriptor\n");
        nOthers = 0;
    }
    for(int j=0; j < nOthers; j++)
    {
        mdOthers[j] = ResolveLocalMemberRef((mdToken)(UINT_PTR)(pPD->m_tklOthers.PEEK(j)));     // @WARNING: casting down from 'mdToken*' to 'mdToken'

        if((RidFromToken(mdOthers[j])!=0)&&(TypeFromToken(mdOthers[j]) != mdtMethodDef))
        {
            report->error("Invalid Other method of property '%s'\n",pPD->m_szName);
            delete [] mdOthers;
            return FALSE;
        }

    }
    mdOthers[nOthers] = mdMethodDefNil; // like null-terminator

    void* pValue = pPD->m_pValue;
#if BIGENDIAN
    if (pPD->m_dwCPlusTypeFlag == ELEMENT_TYPE_STRING)
    {
        void* pValueTemp = _alloca(pPD->m_cbValue * sizeof(WCHAR));
        memcpy(pValueTemp, pValue, pPD->m_cbValue * sizeof(WCHAR));
        pValue = pValueTemp;

        SwapStringLength((WCHAR*)pValue, pPD->m_cbValue);
    }
#endif

    if(FAILED(m_pEmitter->DefineProperty(   pPD->m_tdClass,
                                            wzMemberName,
                                            pPD->m_dwAttr,
                                            pPD->m_pSig,
                                            pPD->m_dwCSig,
                                            pPD->m_dwCPlusTypeFlag,
                                            pValue,
                                            pPD->m_cbValue,
                                            mdSet,
                                            mdGet,
                                            mdOthers,
                                            &(pPD->m_pdPropTok))))
    {
        report->error("Failed to define property '%s'.\n",pPD->m_szName);
        delete [] mdOthers;
        return FALSE;
    }
    EmitCustomAttributes(pPD->m_pdPropTok, &(pPD->m_CustomDescrList));
    return TRUE;
}

Class *Assembler::FindCreateClass(__in __nullterminated const char *pszFQN)
{
    Class *pSearch = NULL;

    if(pszFQN)
    {
        dummyClass->m_szFQN = pszFQN;
        dummyClass->m_Hash = hash((BYTE*)pszFQN, (unsigned)strlen(pszFQN), 10);
        pSearch = m_hshClass.FIND(dummyClass);
        dummyClass->m_szFQN = NULL;
        dummyClass->m_Hash = 0;

        if(!pSearch)
        {
            char* pch;
            DWORD dwFQN = (DWORD)strlen(pszFQN);

            Class *pEncloser = NULL;
            char* pszNewFQN = new char[dwFQN+1];
            strcpy_s(pszNewFQN,dwFQN+1,pszFQN);
            if((pch = strrchr(pszNewFQN, NESTING_SEP)) != NULL)
            {
                *pch = 0;
                pEncloser = FindCreateClass(pszNewFQN);
                *pch = NESTING_SEP;
            }
            pSearch = new Class(pszNewFQN);
            if (pSearch == NULL)
                report->error("Failed to create class '%s'\n",pszNewFQN);
            else
            {
                pSearch->m_pEncloser = pEncloser;
                m_lstClass.PUSH(pSearch);
                pSearch->m_cl = mdtTypeDef | m_lstClass.COUNT();
                m_hshClass.PUSH(pSearch);
            }
        }
    }

    return pSearch;
}


BOOL Assembler::EmitClass(Class *pClass)
{
    LPCUTF8              szFullName;
    WCHAR*              wzFullName=&wzUniBuf[0];
    HRESULT             hr = E_FAIL;
    GUID                guid;
    size_t              L;
    mdToken             tok;

    if(pClass == NULL) return FALSE;

    hr = CoCreateGuid(&guid);
    if (FAILED(hr))
    {
        printf("Unable to create GUID\n");
        m_State = STATE_FAIL;
        return FALSE;
    }

    if(pClass->m_pEncloser)
        szFullName = strrchr(pClass->m_szFQN,NESTING_SEP) + 1;
    else
        szFullName = pClass->m_szFQN;

    WszMultiByteToWideChar(g_uCodePage,0,szFullName,-1,wzFullName,dwUniBuf);

    L = wcslen(wzFullName);
    if((L==0)||(wzFullName[L-1]==L'.')) // Missing class name!
    {
        wcscat_s(wzFullName,dwUniBuf,W("$UNNAMED_TYPE$"));
    }

    pClass->m_Attr = CheckClassFlagsIfNested(pClass->m_pEncloser, pClass->m_Attr);

    if (pClass->m_pEncloser)
    {
        hr = m_pEmitter->DefineNestedType( wzFullName,
                                        pClass->m_Attr,      // attributes
                                        pClass->m_crExtends,  // CR extends class
                                        pClass->m_crImplements,// implements
                                        pClass->m_pEncloser->m_cl,  // Enclosing class.
                                        &tok);
    }
    else
    {
        hr = m_pEmitter->DefineTypeDef( wzFullName,
                                        pClass->m_Attr,      // attributes
                                        pClass->m_crExtends,  // CR extends class
                                        pClass->m_crImplements,// implements
                                        &tok);
    }
    _ASSERTE(tok == pClass->m_cl);
    if (FAILED(hr)) goto exit;
    if (pClass->m_NumTyPars)
    {
        ULONG i;
        mdToken tkNil = mdTokenNil;
        mdGenericParam tkGP = mdTokenNil;
        for(i = 0; i < pClass->m_NumTyPars; i++)
        {
            if (FAILED(m_pEmitter->DefineGenericParam(pClass->m_cl, i, pClass->m_TyPars[i].Attrs(), pClass->m_TyPars[i].Name(), 0, &tkNil, &tkGP)))
            {
                report->error("Unable to define generic param: %s'\n", pClass->m_TyPars[i].Name());
            }
            else
            {
                pClass->m_TyPars[i].Token(tkGP);
                EmitCustomAttributes(tkGP, pClass->m_TyPars[i].CAList());
            }
        }
        EmitGenericParamConstraints(pClass->m_NumTyPars, pClass->m_TyPars, pClass->m_cl, &(pClass->m_GPCList));
    }

    EmitCustomAttributes(pClass->m_cl, &(pClass->m_CustDList));
    hr = S_OK;

exit:
    return SUCCEEDED(hr);
}

BOOL Assembler::DoGlobalFixups()
{
    GlobalFixup *pSearch;

    for (int i=0; (pSearch = m_lstGlobalFixup.PEEK(i)); i++)
    {
        GlobalLabel *   pLabel = FindGlobalLabel(pSearch->m_szLabel);
        if (pLabel == NULL)
        {
            report->error("Unable to find forward reference global label '%s'\n",
                pSearch->m_szLabel);

            m_State = STATE_FAIL;
            return FALSE;
        }
        //BYTE * pReference = pSearch->m_pReference;
        //DWORD  GlobalOffset = pLabel->m_GlobalOffset;
        //memcpy(pReference,&GlobalOffset,4);
        SET_UNALIGNED_VAL32(pSearch->m_pReference,pLabel->m_GlobalOffset);
    }

    return TRUE;
}

state_t Assembler::AddGlobalLabel(__in __nullterminated char *pszName, HCEESECTION section)
{
    if (FindGlobalLabel(pszName) != NULL)
    {
        report->error("Duplicate global label '%s'\n", pszName);
        m_State = STATE_FAIL;
        return m_State;
    }

    ULONG GlobalOffset;

    HRESULT hr;
    hr = m_pCeeFileGen->GetSectionDataLen(section, &GlobalOffset);
    _ASSERTE(SUCCEEDED(hr));

    GlobalLabel *pNew = new GlobalLabel(pszName, GlobalOffset, section);
    if (pNew == 0)
    {
        report->error("Failed to allocate global label '%s'\n",pszName);
        m_State = STATE_FAIL;
        return m_State;
    }

    m_lstGlobalLabel.PUSH(pNew);
    return m_State;
}

void Assembler::AddLabel(DWORD CurPC, __in __nullterminated char *pszName)
{
    if (m_pCurMethod->FindLabel(pszName) != NULL)
    {
        report->error("Duplicate label: '%s'\n", pszName);

        m_State = STATE_FAIL;
    }
    else
    {
        Label *pNew = new Label(pszName, CurPC);

        if (pNew != NULL)
            //m_pCurMethod->m_lstLabel.PUSH(pNew);
            m_lstLabel.PUSH(pNew);
        else
        {
            report->error("Failed to allocate label '%s'\n",pszName);
            m_State = STATE_FAIL;
        }
    }
}

void Assembler::DoDeferredILFixups(Method* pMethod)
{ // Now that we know where in the file the code bytes will wind up,
  // we can update the RVAs and offsets.
    ILFixup *pSearch;
    HRESULT hr;
    GlobalFixup *Fix = NULL;
    int i;
    for (i=0;(pSearch = pMethod->m_lstILFixup.PEEK(i));i++)
    {
        switch(pSearch->m_Kind)
        {
            case ilGlobal:
                Fix = pSearch->m_Fixup;
                _ASSERTE(Fix != NULL);
                Fix->m_pReference = pMethod->m_pCode+pSearch->m_OffsetInMethod;
                break;

            case ilToken:
                hr = m_pCeeFileGen->AddSectionReloc(m_pILSection,
                                    pSearch->m_OffsetInMethod+pMethod->m_methodOffset,
                                    m_pILSection,
                                    srRelocMapToken);
                _ASSERTE(SUCCEEDED(hr));
                break;

            case ilRVA:
                hr = m_pCeeFileGen->AddSectionReloc(m_pILSection,
                                    pSearch->m_OffsetInMethod+pMethod->m_methodOffset,
                                    m_pGlobalDataSection,
                                    srRelocAbsolute);
                _ASSERTE(SUCCEEDED(hr));
                break;

            default:
                ;
        }
    }
}
/**************************************************************************/
BOOL Assembler::DoFixups(Method* pMethod)
{
    Fixup *pSearch;

    for (int i=0; (pSearch = pMethod->m_lstFixup.PEEK(i)); i++)
    {
        Label * pLabel = pMethod->FindLabel(pSearch->m_szLabel);
        long    offset;

        if (pLabel == NULL)
        {
            report->error("Unable to find forward reference label '%s' called from PC=%d\n",
                pSearch->m_szLabel, pSearch->m_RelativeToPC);

            //m_State = STATE_FAIL;
            return FALSE;
        }

        offset = pLabel->m_PC - pSearch->m_RelativeToPC;

        if (pSearch->m_FixupSize == 1)
        {
            if (offset > 127 || offset < -128)
            {
                report->error("Offset of forward reference label '%s' called from PC=%d is too large for 1 byte pcrel\n",
                    pLabel->m_szName, pSearch->m_RelativeToPC);

                //m_State = STATE_FAIL;
                return FALSE;
            }

            *pSearch->m_pBytes = (BYTE) offset;
        }
        else if (pSearch->m_FixupSize == 4)
        {
            SET_UNALIGNED_VAL32(pSearch->m_pBytes,offset);
        }
    }

    return TRUE;
}


OPCODE Assembler::DecodeOpcode(const BYTE *pCode, DWORD *pdwLen)
{
    OPCODE opcode;

    *pdwLen = 1;
    opcode = OPCODE(pCode[0]);
    switch(opcode) {
        case CEE_PREFIX1:
            opcode = OPCODE(pCode[1] + 256);
            if (opcode < 0 || opcode >= CEE_COUNT)
                return CEE_COUNT;
            *pdwLen = 2;
            break;

        case CEE_PREFIXREF:
        case CEE_PREFIX2:
        case CEE_PREFIX3:
        case CEE_PREFIX4:
        case CEE_PREFIX5:
        case CEE_PREFIX6:
        case CEE_PREFIX7:
            return CEE_COUNT;
        default:
            break;
    }
    return opcode;
}

char* Assembler::ReflectionNotation(mdToken tk)
{
    char *sz = (char*)&wzUniBuf[dwUniBuf>>1], *pc;
    *sz=0;
    switch(TypeFromToken(tk))
    {
        case mdtTypeDef:
            {
                Class *pClass = m_lstClass.PEEK(RidFromToken(tk)-1);
                if(pClass)
                {
                    strcpy_s(sz,dwUniBuf>>1,pClass->m_szFQN);
                    pc = sz;
                    while((pc = strchr(pc,NESTING_SEP)) != NULL)
                    {
                        *pc = '+';
                        pc++;
                    }
                }
            }
            break;

        case mdtTypeRef:
            {
                ULONG   N;
                mdToken tkResScope;
                if(SUCCEEDED(m_pImporter->GetTypeRefProps(tk,&tkResScope,wzUniBuf,dwUniBuf>>1,&N)))
                {
                    WszWideCharToMultiByte(CP_UTF8,0,wzUniBuf,-1,sz,dwUniBuf>>1,NULL,NULL);
                    if(TypeFromToken(tkResScope)==mdtAssemblyRef)
                    {
                        AsmManAssembly *pAsmRef = m_pManifest->m_AsmRefLst.PEEK(RidFromToken(tkResScope)-1);
                        if(pAsmRef)
                        {
                            pc = &sz[strlen(sz)];
                            pc+=sprintf_s(pc,(dwUniBuf >> 1),", %s, Version=%d.%d.%d.%d, Culture=",pAsmRef->szName,
                                    pAsmRef->usVerMajor,pAsmRef->usVerMinor,pAsmRef->usBuild,pAsmRef->usRevision);
                            ULONG L=0;
                            if(pAsmRef->pLocale && (L=pAsmRef->pLocale->length()))
                            {
                                memcpy(wzUniBuf,pAsmRef->pLocale->ptr(),L);
                                wzUniBuf[L>>1] = 0;
                                WszWideCharToMultiByte(CP_UTF8,0,wzUniBuf,-1,pc,dwUniBuf>>1,NULL,NULL);
                            }
                            else pc+=sprintf_s(pc,(dwUniBuf >> 1),"neutral");
                            pc = &sz[strlen(sz)];
                            if(pAsmRef->pPublicKeyToken && (L=pAsmRef->pPublicKeyToken->length()))
                            {
                                pc+=sprintf_s(pc,(dwUniBuf >> 1),", Publickeytoken=");
                                BYTE* pb = (BYTE*)(pAsmRef->pPublicKeyToken->ptr());
                                for(N=0; N<L; N++,pb++) pc+=sprintf_s(pc,(dwUniBuf >> 1),"%2.2x",*pb);
                            }
                        }
                    }
                }
            }
            break;

        default:
            break;
    }
    return sz;
}

/*
--------------------------------------------------------------------
mix -- mix 3 32-bit values reversibly.
For every delta with one or two bits set, and the deltas of all three
  high bits or all three low bits, whether the original value of a,b,c
  is almost all zero or is uniformly distributed,
* If mix() is run forward or backward, at least 32 bits in a,b,c
  have at least 1/4 probability of changing.
* If mix() is run forward, every bit of c will change between 1/3 and
  2/3 of the time.  (Well, 22/100 and 78/100 for some 2-bit deltas.)
mix() was built out of 36 single-cycle latency instructions in a
  structure that could supported 2x parallelism, like so:
      a -= b;
      a -= c; x = (c>>13);
      b -= c; a ^= x;
      b -= a; x = (a<<8);
      c -= a; b ^= x;
      c -= b; x = (b>>13);
      ...
  Unfortunately, superscalar Pentiums and Sparcs can't take advantage
  of that parallelism.  They've also turned some of those single-cycle
  latency instructions into multi-cycle latency instructions.  Still,
  this is the fastest good hash I could find.  There were about 2^^68
  to choose from.  I only looked at a billion or so.
--------------------------------------------------------------------
*/
#define mix(a,b,c) \
{ \
  a -= b; a -= c; a ^= (c >> 13); \
  b -= c; b -= a; b ^= (a << 8);  \
  c -= a; c -= b; c ^= (b >> 13); \
  a -= b; a -= c; a ^= (c >> 12); \
  b -= c; b -= a; b ^= (a << 16); \
  c -= a; c -= b; c ^= (b >> 5);  \
  a -= b; a -= c; a ^= (c >> 3);  \
  b -= c; b -= a; b ^= (a << 10); \
  c -= a; c -= b; c ^= (b >> 15); \
}

/*
--------------------------------------------------------------------
hash() -- hash a variable-length key into a 32-bit value
  k       : the key (the unaligned variable-length array of bytes)
  len     : the length of the key, counting by bytes
  initval : can be any 4-byte value
Returns a 32-bit value.  Every bit of the key affects every bit of
the return value.  Every 1-bit and 2-bit delta achieves avalanche.
About 6*len+35 instructions.

The best hash table sizes are powers of 2.  There is no need to do
mod a prime (mod is sooo slow!).  If you need less than 32 bits,
use a bitmask.  For example, if you need only 10 bits, do
  h = (h & hashmask(10));
In which case, the hash table should have hashsize(10) elements.

If you are hashing n strings (ub1 **)k, do it like this:
  for (i=0, h=0; i<n; ++i) h = hash( k[i], len[i], h);

By Bob Jenkins, 1996.  bob_jenkins@burtleburtle.net.  You may use this
code any way you wish, private, educational, or commercial.  It's free.

See http://burtleburtle.net/bob/hash/evahash.html
Use for hash table lookup, or anything where one collision in 2^^32 is
acceptable.  Do NOT use for cryptographic purposes.
--------------------------------------------------------------------
*/

unsigned hash(
     __in_ecount(length) const BYTE *k,        /* the key */
     unsigned  length,   /* the length of the key */
     unsigned  initval)  /* the previous hash, or an arbitrary value */
{
   register unsigned a,b,c,len;

   /* Set up the internal state */
   len = length;
   a = b = 0x9e3779b9;  /* the golden ratio; an arbitrary value */
   c = initval;         /* the previous hash value */

   /*---------------------------------------- handle most of the key */
   while (len >= 12)
   {
      a += (k[0] + ((unsigned)k[1] << 8) + ((unsigned)k[2]  << 16) + ((unsigned)k[3]  << 24));
      b += (k[4] + ((unsigned)k[5] << 8) + ((unsigned)k[6]  << 16) + ((unsigned)k[7]  << 24));
      c += (k[8] + ((unsigned)k[9] << 8) + ((unsigned)k[10] << 16) + ((unsigned)k[11] << 24));
      mix(a,b,c);
      k += 12; len -= 12;
   }

   /*------------------------------------- handle the last 11 bytes */
   c += length;
   switch(len)              /* all the case statements fall through */
   {
       case 11: c+=((unsigned)k[10] << 24);
                FALLTHROUGH;
       case 10: c+=((unsigned)k[9] << 16);
                FALLTHROUGH;
       case 9 : c+=((unsigned)k[8] << 8);
                FALLTHROUGH;
          /* the first byte of c is reserved for the length */
       case 8 : b+=((unsigned)k[7] << 24);
                FALLTHROUGH;
       case 7 : b+=((unsigned)k[6] << 16);
                FALLTHROUGH;
       case 6 : b+=((unsigned)k[5] << 8);
                FALLTHROUGH;
       case 5 : b+=k[4];
                FALLTHROUGH;
       case 4 : a+=((unsigned)k[3] << 24);
                FALLTHROUGH;
       case 3 : a+=((unsigned)k[2] << 16);
                FALLTHROUGH;
       case 2 : a+=((unsigned)k[1] << 8);
                FALLTHROUGH;
       case 1 : a+=k[0];
     /* case 0: nothing left to add */
   }
   mix(a,b,c);
   /*-------------------------------------------- report the result */
   return c;
}

