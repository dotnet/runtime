// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// file: method.cpp
//

//
#include "ilasmpch.h"
#include "assembler.h"

Method::Method(Assembler *pAssembler, Class *pClass, _In_ __nullterminated char *pszName, BinStr* pbsSig, DWORD Attr)
{

    // default values
    m_pClass        = pClass;
    m_MaxStack      = 8;
    m_Flags         = 0;
    m_LocalsSig     = 0;
    m_dwNumExceptions = 0;
    m_dwNumEndfilters = 0;
    m_firstArgName = NULL;
    m_firstVarName = NULL;
    m_pMethodSig = NULL;
    m_wImplAttr = miIL; //default, if native or optil are not specified
    m_wVTEntry = 0;
    m_wVTSlot = 0;
    m_pAssembler = pAssembler;
    m_pCurrScope = &m_MainScope;
    m_pRetMarshal = NULL;
    m_pRetValue = NULL;
    m_szExportAlias = NULL;
    m_dwExportOrdinal = 0xFFFFFFFF;
    m_ulLines[0]=m_ulLines[1]=0;
    m_ulColumns[0]=m_ulColumns[1]=0;
    m_pbsBody = NULL;
    m_fNewBody = TRUE;
    m_fNew = TRUE;

    // move the PInvoke descriptor (if any) from Assembler
    // (Assembler gets the descriptor BEFORE it calls new Method)
    m_pPInvoke = pAssembler->m_pPInvoke;
    pAssembler->m_pPInvoke = NULL;

    _ASSERTE(pszName);
    if (!pszName) return;

    m_szName = pszName;
    m_dwName = (DWORD)strlen(pszName);

    m_ExceptionList = new COR_ILMETHOD_SECT_EH_CLAUSE_FAT[MAX_EXCEPTIONS];
    m_EndfilterOffsetList = new DWORD[MAX_EXCEPTIONS];
    if((m_ExceptionList==NULL)||(m_EndfilterOffsetList==NULL))
    {
        fprintf(stderr,"\nOutOfMemory!\n");
        return;
    }
    m_dwMaxNumExceptions = MAX_EXCEPTIONS;
    m_dwMaxNumEndfilters = MAX_EXCEPTIONS;

    m_Attr          = Attr;
    if((!strcmp(pszName,COR_CCTOR_METHOD_NAME))||(!strcmp(pszName,COR_CTOR_METHOD_NAME)))
        m_Attr |= mdSpecialName;
    m_fEntryPoint   = FALSE;
    m_fGlobalMethod = FALSE;

    if(pbsSig)
    {
        m_dwMethodCSig = pbsSig->length();
        m_pMethodSig = (COR_SIGNATURE*)(pbsSig->ptr());
        m_pbsMethodSig = pbsSig;
    }

    m_firstArgName = pAssembler->getArgNameList();
    if(pClass == NULL) pClass = pAssembler->m_pModuleClass; // fake "class" <Module>
    pClass->m_MethodList.PUSH(this);
    pClass->m_fNewMembers = TRUE;


    m_pPermissions = NULL;
    m_pPermissionSets = NULL;

    m_TyPars = NULL;
    m_NumTyPars = 0;
}


// lexical scope handling
void Method::OpenScope()
{
    Scope*  psc = new Scope;
    if(psc)
    {
        psc->dwStart = m_pAssembler->m_CurPC;
        psc->pSuperScope = m_pCurrScope;
        m_pCurrScope->SubScope.PUSH(psc);
        m_pCurrScope = psc;
    }
}
void Method::CloseScope()
{
    VarDescr*       pVD;
    ARG_NAME_LIST*  pAN;
    for(pAN=m_pCurrScope->pLocals; pAN; pAN = pAN->pNext)
    {
        if((pVD = m_Locals.PEEK(pAN->dwAttr))) pVD->bInScope = FALSE;
    }
    m_pCurrScope->dwEnd = m_pAssembler->m_CurPC;
    m_pCurrScope = m_pCurrScope->pSuperScope;
}

Label *Method::FindLabel(LPCUTF8 pszName)
{
    Label lSearch(pszName,0), *pL;
    lSearch.m_szName = pszName;
    //pL =  m_lstLabel.FIND(&lSearch);
    pL =  m_pAssembler->m_lstLabel.FIND(&lSearch);
    lSearch.m_szName = NULL;
    return pL;
    //return  m_lstLabel.FIND(pszName);
}


Label *Method::FindLabel(DWORD PC)
{
    Label *pSearch;

    //for (int i = 0; (pSearch = m_lstLabel.PEEK(i)); i++)
    for (int i = 0; (pSearch = m_pAssembler->m_lstLabel.PEEK(i)); i++)
    {
        if (pSearch->m_PC == PC)
            return pSearch;
    }

    return NULL;
}

// Method::AddGenericParamConstraint is called when we have a .param constraint directive after a method definition
// 
void Method::AddGenericParamConstraint(int index, char * pStrGenericParam, mdToken tkTypeConstraint)
{
    if (index > 0)
    {
        if (pStrGenericParam != 0)
        {
            m_pAssembler->report->error("LOGIC ERROR - we have both an index and a pStrGenericParam");
            return;
        }
        if (index > (int)m_NumTyPars)
        {
            m_pAssembler->report->error("Type parameter index out of range: 1.. %d\n", m_NumTyPars);
            return;
        }
        index = index - 1;
    }
    else  // index was 0, so a name must be supplied by pStrGenericParam
    {
        if (pStrGenericParam == 0)
        {
            m_pAssembler->report->error("LOGIC ERROR - we have neither an index or a pStrGenericParam");
            return;
        }
        index = FindTyPar(pStrGenericParam);
        if (index == -1)
        {
            m_pAssembler->report->error("Type parameter '%s' undefined\n", pStrGenericParam);
            return;
        }
    }

    // pass true for isParamDirective, we are parsing a .param directive for a method here
    //
    m_pAssembler->CheckAddGenericParamConstraint(&m_GPCList, index, tkTypeConstraint, true);
}



