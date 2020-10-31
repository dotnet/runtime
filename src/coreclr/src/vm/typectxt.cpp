// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// typectxt.cpp
//

//
// Simple struct to record the data necessary to interpret ELEMENT_TYPE_VAR
// and ELEMENT_TYPE_MVAR within pieces of metadata, in particular within
// signatures parsed by MetaSig and SigPointer.
//



#include "common.h"
#include "method.hpp"
#include "typehandle.h"
#include "field.h"



void SigTypeContext::InitTypeContext(MethodDesc *md, Instantiation exactClassInst, Instantiation exactMethodInst, SigTypeContext *pRes)
{
    LIMITED_METHOD_CONTRACT;
    MethodTable *pMT = md->GetMethodTable();

    if (pMT->IsArray())
    {
        pRes->m_classInst = exactClassInst.IsEmpty() ? pMT->GetClassOrArrayInstantiation() : exactClassInst;
    }
    else
    {
        pRes->m_classInst = exactClassInst;
    }
    pRes->m_methodInst = exactMethodInst;
}

void SigTypeContext::InitTypeContext(MethodDesc *md, SigTypeContext *pRes)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;

        PRECONDITION(CheckPointer(md));
    } CONTRACTL_END;

    MethodTable *pMT = md->GetMethodTable();
    if (pMT->IsArray())
    {
        pRes->m_classInst = pMT->GetClassOrArrayInstantiation();
    }
    else
    {
        pRes->m_classInst = pMT->GetInstantiation();
    }
    pRes->m_methodInst = md->GetMethodInstantiation();
}

void SigTypeContext::InitTypeContext(MethodDesc *md, TypeHandle declaringType, SigTypeContext *pRes)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;

        PRECONDITION(CheckPointer(md));
    } CONTRACTL_END;

    if (declaringType.IsNull())
    {
        SigTypeContext::InitTypeContext(md, pRes);
    }
    else
    {
        MethodTable *pMDMT = md->GetMethodTable();
        if (pMDMT->IsArray())
        {
            pRes->m_classInst = declaringType.GetClassOrArrayInstantiation();
        }
        else
        {
            pRes->m_classInst = declaringType.GetInstantiationOfParentClass(pMDMT);
        }
        pRes->m_methodInst = md->GetMethodInstantiation();
    }
}

#ifndef DACCESS_COMPILE
TypeHandle GetDeclaringMethodTableFromTypeVarTypeDesc(TypeVarTypeDesc *pTypeVar, MethodDesc *pMD)
{
    LIMITED_METHOD_CONTRACT;

    // This currently should only happen in cases where we've already loaded the constraints.
    // Currently, the only known case where use this code is reflection over methods exposed on a TypeVariable.
    _ASSERTE(pTypeVar->ConstraintsLoaded());

    if (pTypeVar->ConstraintsLoaded())
    {
        DWORD cConstraints;
        TypeHandle *pTypeHandles = pTypeVar->GetCachedConstraints(&cConstraints);
        for (DWORD iConstraint = 0; iConstraint < cConstraints; iConstraint++)
        {
            if (pTypeHandles[iConstraint].IsGenericVariable())
            {
                TypeHandle th = GetDeclaringMethodTableFromTypeVarTypeDesc(pTypeHandles[iConstraint].AsGenericVariable(), pMD);
                if (!th.IsNull())
                    return th;
            }
            else
            {
                MethodTable *pMT = pTypeHandles[iConstraint].GetMethodTable();
                while (pMT != NULL)
                {
                    if (pMT == pMD->GetMethodTable())
                    {
                        return TypeHandle(pMT);
                    }

                    pMT = pMT->GetParentMethodTable();
                }
            }
        }
    }
    return TypeHandle();
}

void SigTypeContext::InitTypeContext(MethodDesc *md, TypeHandle declaringType, Instantiation exactMethodInst, SigTypeContext *pRes)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;

        PRECONDITION(CheckPointer(md));
    } CONTRACTL_END;

    if (declaringType.IsNull())
    {
        SigTypeContext::InitTypeContext(md, pRes);
    }
    else
    {
        // <TODO> factor this with the work above </TODO>
        if (declaringType.IsGenericVariable())
        {
            declaringType = GetDeclaringMethodTableFromTypeVarTypeDesc(declaringType.AsGenericVariable(), md);
        }

        if (declaringType.IsNull())
        {
            SigTypeContext::InitTypeContext(md, pRes);
        }
        else
        {
            MethodTable *pMDMT = md->GetMethodTable();
            if (pMDMT->IsArray())
            {
                pRes->m_classInst = declaringType.GetClassOrArrayInstantiation();
            }
            else
            {
                pRes->m_classInst = declaringType.GetInstantiationOfParentClass(pMDMT);
            }
        }
    }
    pRes->m_methodInst = !exactMethodInst.IsEmpty() ? exactMethodInst : md->GetMethodInstantiation();
}
#endif // !DACCESS_COMPILE

void SigTypeContext::InitTypeContext(FieldDesc *pFD, TypeHandle declaringType, SigTypeContext *pRes)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;

        PRECONDITION(CheckPointer(declaringType, NULL_OK));
        PRECONDITION(CheckPointer(pFD));
    } CONTRACTL_END;
    LIMITED_METHOD_CONTRACT;
    InitTypeContext(pFD->GetExactClassInstantiation(declaringType),Instantiation(), pRes);
}


void SigTypeContext::InitTypeContext(TypeHandle th, SigTypeContext *pRes)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    } CONTRACTL_END;

    if (th.IsNull())
    {
        InitTypeContext(pRes);
    }
    else if (th.GetMethodTable()->IsArray())
    {
        InitTypeContext(th.GetMethodTable()->GetClassOrArrayInstantiation(), Instantiation(), pRes);
    }
    else
    {
        InitTypeContext(th.GetInstantiation(), Instantiation(), pRes);
    }
}


const SigTypeContext * SigTypeContext::GetOptionalTypeContext(MethodDesc *md, TypeHandle declaringType, SigTypeContext *pRes)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(md);
    if (md->HasClassOrMethodInstantiation()  || md->GetMethodTable()->IsArray())
    {
        SigTypeContext::InitTypeContext(md, declaringType,pRes);
        return pRes;
    }
    else
    {
        _ASSERTE(pRes->m_classInst.IsEmpty());
        _ASSERTE(pRes->m_methodInst.IsEmpty());
        return NULL;
    }
}

const SigTypeContext * SigTypeContext::GetOptionalTypeContext(TypeHandle th, SigTypeContext *pRes)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE (!th.IsNull());
    if (th.HasInstantiation() || th.GetMethodTable()->IsArray())
    {
        SigTypeContext::InitTypeContext(th,pRes);
        return pRes;
    }
    else
    {
        // It should already have been null-initialized when allocated on the stack.
        _ASSERTE(pRes->m_classInst.IsEmpty());
        _ASSERTE(pRes->m_methodInst.IsEmpty());
        return NULL;
    }
}

BOOL SigTypeContext::IsValidTypeOnlyInstantiationOf(const SigTypeContext *pCtxTypicalMethodInstantiation, const SigTypeContext *pCtxTypeOnlyInstantiation)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Compare class inst counts
    if (pCtxTypicalMethodInstantiation->m_classInst.GetNumArgs() != pCtxTypeOnlyInstantiation->m_classInst.GetNumArgs())
        return FALSE;

    // Compare method inst counts
    if (pCtxTypicalMethodInstantiation->m_methodInst.GetNumArgs() != pCtxTypeOnlyInstantiation->m_methodInst.GetNumArgs())
        return FALSE;

    DWORD i;

    // Ensure that no type variables are part of the instantiation of the generic type
    for (i = 0; i < pCtxTypicalMethodInstantiation->m_classInst.GetNumArgs(); i++) {
        if (pCtxTypeOnlyInstantiation->m_classInst[i].IsGenericVariable())
            return FALSE;
    }

    // Compare method inst values to ensure they represent the same generic method parameters
    for (i = 0; i < pCtxTypicalMethodInstantiation->m_methodInst.GetNumArgs(); i++) {
        _ASSERTE(pCtxTypicalMethodInstantiation->m_methodInst[i].IsGenericVariable());

        if (pCtxTypicalMethodInstantiation->m_methodInst[i] != pCtxTypeOnlyInstantiation->m_methodInst[i])
            return FALSE;
    }

    return TRUE;
}

BOOL SigTypeContext::Equal(const SigTypeContext *pCtx1, const SigTypeContext *pCtx2)
{
    WRAPPER_NO_CONTRACT;

    // Compare class inst counts
    if (pCtx1->m_classInst.GetNumArgs() != pCtx2->m_classInst.GetNumArgs())
        return FALSE;

    // Compare method inst counts
    if (pCtx1->m_methodInst.GetNumArgs() != pCtx2->m_methodInst.GetNumArgs())
        return FALSE;

    DWORD i;

    // Compare class inst values
    for (i = 0; i < pCtx1->m_classInst.GetNumArgs(); i++) {
        if (pCtx1->m_classInst[i] != pCtx2->m_classInst[i])
            return FALSE;
    }

    // Compare method inst values
    for (i = 0; i < pCtx1->m_methodInst.GetNumArgs(); i++) {
        if (pCtx1->m_methodInst[i] != pCtx2->m_methodInst[i])
            return FALSE;
    }

    return TRUE;
}

