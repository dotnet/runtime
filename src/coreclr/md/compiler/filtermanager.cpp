// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// FilterManager.cpp
//

//
// contains utility code to MD directory
//
//*****************************************************************************
#include "stdafx.h"
#include "filtermanager.h"

#define IsGlobalTypeDef(td) ((td) == TokenFromRid(mdtTypeDef, 1))

//*****************************************************************************
// Walk up to the containing tree and
// mark the transitive closure of the root token
//*****************************************************************************
HRESULT FilterManager::Mark(mdToken tk)
{
    HRESULT     hr = NOERROR;
    mdTypeDef   td;

    // We hard coded System.Object as mdTypeDefNil
    // The backing Field of property can be NULL as well.
    if (RidFromToken(tk) == mdTokenNil)
        goto ErrExit;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    switch ( TypeFromToken(tk) )
    {
    case mdtTypeDef:
        IfFailGo( MarkTypeDef(tk) );
        break;

    case mdtMethodDef:
        // Get the typedef containing the MethodDef and mark the whole type
        IfFailGo( m_pMiniMd->FindParentOfMethodHelper(tk, &td) );

        // Global function so only mark the function itself and the typedef.
        // Don't call MarkTypeDef. That will trigger all of the global methods/fields
        // marked.
        //
        if (IsGlobalTypeDef(td))
        {
            IfFailGo( m_pMiniMd->GetFilterTable()->MarkTypeDef(td) );
            IfFailGo( MarkMethod(tk) );
        }
        else
        {
            IfFailGo( MarkTypeDef(td) );
        }
        break;

    case mdtFieldDef:
        // Get the typedef containing the FieldDef and mark the whole type
        IfFailGo( m_pMiniMd->FindParentOfFieldHelper(tk, &td) );
        if (IsGlobalTypeDef(td))
        {
            IfFailGo( m_pMiniMd->GetFilterTable()->MarkTypeDef(td) );
            IfFailGo( MarkField(tk) );
        }
        else
        {
            IfFailGo( MarkTypeDef(td) );
        }
        break;

    case mdtMemberRef:
        IfFailGo( MarkMemberRef(tk) );
        break;

    case mdtTypeRef:
        IfFailGo( MarkTypeRef(tk) );
        break;

    case mdtTypeSpec:
        IfFailGo( MarkTypeSpec(tk) );
        break;
    case mdtSignature:
        IfFailGo( MarkStandAloneSig(tk) );
        break;

    case mdtModuleRef:
        IfFailGo( MarkModuleRef(tk) );
        break;

    case mdtAssemblyRef:
        IfFailGo( MarkAssemblyRef(tk) );
        break;

    case mdtModule:
        IfFailGo( MarkModule(tk) );
        break;

    case mdtString:
        IfFailGo( MarkUserString(tk) );
        break;

    case mdtBaseType:
        // don't need to mark any base type.
        break;

    case mdtAssembly:
        IfFailGo( MarkAssembly(tk) );
        break;

    case mdtMethodSpec:
        IfFailGo( MarkMethodSpec(tk) );
        break;

    case mdtProperty:
    case mdtEvent:
    case mdtParamDef:
    case mdtInterfaceImpl:
    default:
        _ASSERTE(!" unknown type!");
        hr = E_INVALIDARG;
        break;
    }
ErrExit:
    return hr;
} // HRESULT FilterManager::Mark()



//*****************************************************************************
// marking only module property
//*****************************************************************************
HRESULT FilterManager::MarkAssembly(mdAssembly as)
{
    HRESULT         hr = NOERROR;

    if (!hasAssemblyBeenMarked)
    {
        hasAssemblyBeenMarked = true;
        IfFailGo( MarkCustomAttributesWithParentToken(as) );
        IfFailGo( MarkDeclSecuritiesWithParentToken(as) );
    }
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkAssembly()


//*****************************************************************************
// marking only module property
//*****************************************************************************
HRESULT FilterManager::MarkModule(mdModule mo)
{
    HRESULT         hr = NOERROR;

    if (!hasModuleBeenMarked)
    {
        hasModuleBeenMarked = true;
        IfFailGo( MarkCustomAttributesWithParentToken(mo) );
    }
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkModule()


//*****************************************************************************
// cascading Mark of a CustomAttribute
//*****************************************************************************
HRESULT FilterManager::MarkCustomAttribute(mdCustomAttribute cv)
{
    HRESULT     hr = NOERROR;
    CustomAttributeRec *pRec;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkCustomAttribute( cv ) );

    // Mark the type (and any family) of the CustomAttribue.
    IfFailGo(m_pMiniMd->GetCustomAttributeRecord(RidFromToken(cv), &pRec));
    IfFailGo( Mark(m_pMiniMd->getTypeOfCustomAttribute(pRec)) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkCustomAttribute()


//*****************************************************************************
// cascading Mark of a DeclSecurity
//*****************************************************************************
HRESULT FilterManager::MarkDeclSecurity(mdPermission pe)
{
    HRESULT     hr = NOERROR;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkDeclSecurity( pe ) );
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkDeclSecurity()



//*****************************************************************************
// cascading Mark of a signature
//*****************************************************************************
HRESULT FilterManager::MarkStandAloneSig(mdSignature sig)
{
    HRESULT         hr = NOERROR;
    StandAloneSigRec    *pRec;
    ULONG           cbSize;
    ULONG           cbUsed;
    PCCOR_SIGNATURE pbSig;
    IHostFilter     *pFilter = m_pMiniMd->GetHostFilter();

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if TypeRef is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsSignatureMarked(sig))
        goto ErrExit;

    // To mark the signature, we will need to mark
    // all of the embedded TypeRef or TypeDef
    //
    IfFailGo( m_pMiniMd->GetFilterTable()->MarkSignature( sig ) );

    if (pFilter)
        pFilter->MarkToken(sig);

    // Walk the signature and mark all of the embedded types
    IfFailGo(m_pMiniMd->GetStandAloneSigRecord(RidFromToken(sig), &pRec));
    IfFailGo(m_pMiniMd->getSignatureOfStandAloneSig(pRec, &pbSig, &cbSize));
    IfFailGo( MarkSignature(pbSig, cbSize, &cbUsed) );

    IfFailGo( MarkCustomAttributesWithParentToken(sig) );
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkStandAloneSig()



//*****************************************************************************
// cascading Mark of a TypeSpec
//*****************************************************************************
HRESULT FilterManager::MarkTypeSpec(mdTypeSpec ts)
{
    HRESULT         hr = NOERROR;
    TypeSpecRec     *pRec;
    ULONG           cbSize;
    ULONG           cbUsed;
    PCCOR_SIGNATURE pbSig;
    IHostFilter     *pFilter = m_pMiniMd->GetHostFilter();

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if TypeRef is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsTypeSpecMarked(ts))
        goto ErrExit;

    // To mark the TypeSpec, we will need to mark
    // all of the embedded TypeRef or TypeDef
    //
    IfFailGo( m_pMiniMd->GetFilterTable()->MarkTypeSpec( ts ) );

    if (pFilter)
        pFilter->MarkToken(ts);

    // Walk the signature and mark all of the embedded types
    IfFailGo(m_pMiniMd->GetTypeSpecRecord(RidFromToken(ts), &pRec));
    IfFailGo(m_pMiniMd->getSignatureOfTypeSpec(pRec, &pbSig, &cbSize));
    IfFailGo( MarkFieldSignature(pbSig, cbSize, &cbUsed) );
    IfFailGo( MarkCustomAttributesWithParentToken(ts) );


ErrExit:
    return hr;
} // HRESULT FilterManager::MarkTypeSpec()




//*****************************************************************************
// cascading Mark of a TypeRef
//*****************************************************************************
HRESULT FilterManager::MarkTypeRef(mdTypeRef tr)
{
    HRESULT         hr = NOERROR;
    TOKENMAP        *tkMap;
    mdTypeDef       td;
    IHostFilter     *pFilter = m_pMiniMd->GetHostFilter();
    TypeRefRec      *pRec;
    mdToken         parentTk;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if TypeRef is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsTypeRefMarked(tr))
        goto ErrExit;

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkTypeRef( tr ) );

    if (pFilter)
        pFilter->MarkToken(tr);

    IfFailGo(m_pMiniMd->GetTypeRefRecord(RidFromToken(tr), &pRec));
    parentTk = m_pMiniMd->getResolutionScopeOfTypeRef(pRec);
    if ( RidFromToken(parentTk) )
    {
        IfFailGo( Mark( parentTk ) );
    }

    tkMap = m_pMiniMd->GetTypeRefToTypeDefMap();
    PREFIX_ASSUME(tkMap != NULL);
    td = *(tkMap->Get(RidFromToken(tr)));
    if ( td != mdTokenNil )
    {
        // TypeRef is referring to a TypeDef within the same module.
        // Mark the TypeDef as well.
        //
        IfFailGo( Mark(td) );
    }

    IfFailGo( MarkCustomAttributesWithParentToken(tr) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkTypeRef()


//*****************************************************************************
// cascading Mark of a MemberRef
//*****************************************************************************
HRESULT FilterManager::MarkMemberRef(mdMemberRef mr)
{
    HRESULT         hr = NOERROR;
    MemberRefRec    *pRec;
    ULONG           cbSize;
    ULONG           cbUsed;
    PCCOR_SIGNATURE pbSig;
    IHostFilter     *pFilter = m_pMiniMd->GetHostFilter();
    mdToken         md;
    TOKENMAP        *tkMap;
    mdToken         tkParent;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if MemberRef is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsMemberRefMarked(mr))
        goto ErrExit;

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkMemberRef( mr ) );

    if (pFilter)
        pFilter->MarkToken(mr);

    IfFailGo(m_pMiniMd->GetMemberRefRecord(RidFromToken(mr), &pRec));

    // we want to mark the parent of MemberRef as well
    tkParent = m_pMiniMd->getClassOfMemberRef(pRec);

    // If the parent is the global TypeDef, mark only the TypeDef itself (low-level function).
    // Other parents, do the transitive mark (ie, the high-level function).
    //
    if (IsGlobalTypeDef(tkParent))
        IfFailGo( m_pMiniMd->GetFilterTable()->MarkTypeDef( tkParent ) );
    else
        IfFailGo( Mark( tkParent ) );

    // Walk the signature and mark all of the embedded types
    IfFailGo(m_pMiniMd->getSignatureOfMemberRef(pRec, &pbSig, &cbSize));
    IfFailGo( MarkSignature(pbSig, cbSize, &cbUsed) );

    tkMap = m_pMiniMd->GetMemberRefToMemberDefMap();
    PREFIX_ASSUME(tkMap != NULL);
    md = *(tkMap->Get(RidFromToken(mr)));           // can be fielddef or methoddef
    if ( RidFromToken(md) != mdTokenNil )
    {
        // MemberRef is referring to either a FieldDef or MethodDef.
        // If it is referring to MethodDef, we have fix the parent of MemberRef to be the MethodDef.
        // However, if it is mapped to a FieldDef, the parent column does not track this information.
        // Therefore we need to mark it explicitly.
        //
        IfFailGo( Mark(md) );
    }

    IfFailGo( MarkCustomAttributesWithParentToken(mr) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkMemberRef()


//*****************************************************************************
// cascading Mark of a UserString
//*****************************************************************************
HRESULT FilterManager::MarkUserString(mdString str)
{
    HRESULT         hr = NOERROR;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if UserString is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsUserStringMarked(str))
        goto ErrExit;

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkUserString( str ) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkUserString()


//*****************************************************************************
// Mark of a new UserString
//*****************************************************************************
HRESULT FilterManager::MarkNewUserString(mdString str)
{
    HRESULT         hr = NOERROR;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkNewUserString( str ) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkUserString()


//*****************************************************************************
// cascading Mark of a MethodSpec
//*****************************************************************************
HRESULT FilterManager::MarkMethodSpec(mdMethodSpec ms)
{
    HRESULT         hr = NOERROR;
    MethodSpecRec   *pRec;
    ULONG           cbSize;
    ULONG           cbUsed;
    PCCOR_SIGNATURE pbSig;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if MethodSpec is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsMethodSpecMarked(ms))
        goto ErrExit;

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkMethodSpec( ms ) );

    // Mark MethodRef or MethodDef and embedded TypeRef and TypeDef tokens

    IfFailGo(m_pMiniMd->GetMethodSpecRecord(RidFromToken(ms), &pRec));

    IfFailGo( Mark(m_pMiniMd->getMethodOfMethodSpec(pRec)) );

    IfFailGo(m_pMiniMd->getInstantiationOfMethodSpec(pRec, &pbSig, &cbSize));
    IfFailGo( MarkSignature(pbSig, cbSize, &cbUsed) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkMethodSpec()


//*****************************************************************************
// cascading Mark of a ModuleRef
//*****************************************************************************
HRESULT FilterManager::MarkModuleRef(mdModuleRef mr)
{
    HRESULT     hr = NOERROR;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if ModuleRef is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsModuleRefMarked(mr))
        goto ErrExit;

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkModuleRef( mr ) );
    IfFailGo( MarkCustomAttributesWithParentToken(mr) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkModuleRef()


//*****************************************************************************
// cascading Mark of a AssemblyRef
//*****************************************************************************
HRESULT FilterManager::MarkAssemblyRef(mdAssemblyRef ar)
{
    HRESULT     hr = NOERROR;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if ModuleREf is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsAssemblyRefMarked(ar))
        goto ErrExit;

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkAssemblyRef( ar ) );
    IfFailGo( MarkCustomAttributesWithParentToken(ar) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkAssemblyRef()


//*****************************************************************************
// cascading Mark of all of the custom values associated with a token
//*****************************************************************************
HRESULT FilterManager::MarkCustomAttributesWithParentToken(mdToken tkParent)
{
    HRESULT     hr = NOERROR;
    RID         ridStart, ridEnd;
    RID         index;
    CustomAttributeRec *pRec;

    if ( m_pMiniMd->IsSorted( TBL_CustomAttribute ) )
    {
        // table is sorted. ridStart to ridEnd - 1 are all CustomAttribute
        // associated with tkParent
        //
        IfFailGo(m_pMiniMd->getCustomAttributeForToken(tkParent, &ridEnd, &ridStart));
        for (index = ridStart; index < ridEnd; index ++ )
        {
            IfFailGo( MarkCustomAttribute( TokenFromRid(index, mdtCustomAttribute) ) );
        }
    }
    else
    {
        // table scan is needed
        ridStart = 1;
        ridEnd = m_pMiniMd->getCountCustomAttributes() + 1;
        for (index = ridStart; index < ridEnd; index ++ )
        {
            IfFailGo(m_pMiniMd->GetCustomAttributeRecord(index, &pRec));
            if ( tkParent == m_pMiniMd->getParentOfCustomAttribute(pRec) )
            {
                // This CustomAttribute is associated with tkParent
                IfFailGo( MarkCustomAttribute( TokenFromRid(index, mdtCustomAttribute) ) );
            }
        }
    }

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkCustomAttributesWithParentToken()


//*****************************************************************************
// cascading Mark of all securities associated with a token
//*****************************************************************************
HRESULT FilterManager::MarkDeclSecuritiesWithParentToken(mdToken tkParent)
{
    HRESULT     hr = NOERROR;
    RID         ridStart, ridEnd;
    RID         index;
    DeclSecurityRec *pRec;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    if ( m_pMiniMd->IsSorted( TBL_DeclSecurity ) )
    {
        // table is sorted. ridStart to ridEnd - 1 are all DeclSecurity
        // associated with tkParent
        //
        IfFailGo(m_pMiniMd->getDeclSecurityForToken(tkParent, &ridEnd, &ridStart));
        for (index = ridStart; index < ridEnd; index ++ )
        {
            IfFailGo( m_pMiniMd->GetFilterTable()->MarkDeclSecurity( TokenFromRid(index, mdtPermission) ) );
        }
    }
    else
    {
        // table scan is needed
        ridStart = 1;
        ridEnd = m_pMiniMd->getCountDeclSecuritys() + 1;
        for (index = ridStart; index < ridEnd; index ++ )
        {
            IfFailGo(m_pMiniMd->GetDeclSecurityRecord(index, &pRec));
            if ( tkParent == m_pMiniMd->getParentOfDeclSecurity(pRec) )
            {
                // This DeclSecurity is associated with tkParent
                IfFailGo( m_pMiniMd->GetFilterTable()->MarkDeclSecurity( TokenFromRid(index, mdtPermission) ) );
            }
        }
    }

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkDeclSecuritiesWithParentToken()


//*****************************************************************************
// cascading Mark of all MemberRefs associated with a parent token
//*****************************************************************************
HRESULT FilterManager::MarkMemberRefsWithParentToken(mdToken tk)
{
    HRESULT     hr = NOERROR;
    RID         ulEnd;
    RID         index;
    mdToken     tkParent;
    MemberRefRec *pRec;

    ulEnd = m_pMiniMd->getCountMemberRefs();

    for (index = 1; index <= ulEnd; index ++ )
    {
        // memberRef table is not sorted. Table scan is needed.
        IfFailGo(m_pMiniMd->GetMemberRefRecord(index, &pRec));
        tkParent = m_pMiniMd->getClassOfMemberRef(pRec);
        if ( tk == tkParent )
        {
            IfFailGo( MarkMemberRef( TokenFromRid(index, mdtMemberRef) ) );
        }
    }
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkMemberRefsWithParentToken()


//*****************************************************************************
// cascading Mark of a ParamDef token
//*****************************************************************************
HRESULT FilterManager::MarkParam(mdParamDef pd)
{
    HRESULT     hr;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkParam( pd ) );

    IfFailGo( MarkCustomAttributesWithParentToken(pd) );
    // Parameter does not have declsecurity
    // IfFailGo( MarkDeclSecuritiesWithParentToken(pd) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkParam()


//*****************************************************************************
// cascading Mark of a method token
//*****************************************************************************
HRESULT FilterManager::MarkMethod(mdMethodDef md)
{
    HRESULT         hr = NOERROR;
    MethodRec       *pRec;
    ULONG           cbSize;
    ULONG           cbUsed;
    PCCOR_SIGNATURE pbSig;
    ULONG           i, iCount;
    ImplMapRec      *pImplMapRec = NULL;
    mdMethodDef     mdImp;
    mdModuleRef     mrImp;
    IHostFilter     *pFilter = m_pMiniMd->GetHostFilter();

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if MethodDef is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsMethodMarked(md))
        goto ErrExit;

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkMethod( md ) );
    if (pFilter)
        pFilter->MarkToken(md);

    IfFailGo( MarkParamsWithParentToken(md) );

    // mark any GenericParam of this Method
    IfFailGo( MarkGenericParamWithParentToken(md) );


    // Walk the signature and mark all of the embedded types
    IfFailGo(m_pMiniMd->GetMethodRecord(RidFromToken(md), &pRec));
    IfFailGo(m_pMiniMd->getSignatureOfMethod(pRec, &pbSig, &cbSize));
    IfFailGo( MarkSignature(pbSig, cbSize, &cbUsed) );

    iCount = m_pMiniMd->getCountImplMaps();

    // loop through all ImplMaps and find the Impl map associated with this method def tokens
    // and mark the Module Ref tokens in the entries
    //
    for (i = 1; i <= iCount; i++)
    {
        IfFailGo(m_pMiniMd->GetImplMapRecord(i, &pImplMapRec));

        // Get the MethodDef that the impl map is associated with
        mdImp = m_pMiniMd->getMemberForwardedOfImplMap(pImplMapRec);

        if (mdImp != md)
        {
            // Impl Map entry does not associated with the method def that we are marking
            continue;
        }

        // Get the ModuleRef token
        mrImp = m_pMiniMd->getImportScopeOfImplMap(pImplMapRec);
        IfFailGo( Mark(mrImp) );
    }

    // We should not mark all of the memberref with the parent of this methoddef token.
    // Because not all of the call sites are needed.
    //
    // IfFailGo( MarkMemberRefsWithParentToken(md) );
    IfFailGo( MarkCustomAttributesWithParentToken(md) );
    IfFailGo( MarkDeclSecuritiesWithParentToken(md) );
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkMethod()


//*****************************************************************************
// cascading Mark of a field token
//*****************************************************************************
HRESULT FilterManager::MarkField(mdFieldDef fd)
{
    HRESULT         hr = NOERROR;
    FieldRec        *pRec;
    ULONG           cbSize;
    ULONG           cbUsed;
    PCCOR_SIGNATURE pbSig;
    IHostFilter     *pFilter = m_pMiniMd->GetHostFilter();

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if FieldDef is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsFieldMarked(fd))
        goto ErrExit;

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkField( fd ) );
    if (pFilter)
        pFilter->MarkToken(fd);

    // We should not mark all of the MemberRef with the parent of this FieldDef token.
    // Because not all of the call sites are needed.
    //

    // Walk the signature and mark all of the embedded types
    IfFailGo(m_pMiniMd->GetFieldRecord(RidFromToken(fd), &pRec));
    IfFailGo(m_pMiniMd->getSignatureOfField(pRec, &pbSig, &cbSize));
    IfFailGo( MarkSignature(pbSig, cbSize, &cbUsed) );

    IfFailGo( MarkCustomAttributesWithParentToken(fd) );
    // IfFailGo( MarkDeclSecuritiesWithParentToken(fd) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkField()


//*****************************************************************************
// cascading Mark of an event token
//*****************************************************************************
HRESULT FilterManager::MarkEvent(mdEvent ev)
{
    HRESULT     hr = NOERROR;
    EventRec    *pRec;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if Event is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsEventMarked(ev))
        goto ErrExit;

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkEvent( ev ) );

    // mark the event type as well
    IfFailGo(m_pMiniMd->GetEventRecord(RidFromToken(ev), &pRec));
    IfFailGo( Mark(m_pMiniMd->getEventTypeOfEvent(pRec)) );

    // Note that we don't need to mark the MethodSemantics. Because the association of MethodSemantics
    // is marked. The Method column can only store MethodDef, ie the MethodDef has the same parent as
    // this Event.

    IfFailGo( MarkCustomAttributesWithParentToken(ev) );
    // IfFailGo( MarkDeclSecuritiesWithParentToken(ev) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkEvent()



//*****************************************************************************
// cascading Mark of a Property token
//*****************************************************************************
HRESULT FilterManager::MarkProperty(mdProperty pr)
{
    HRESULT         hr = NOERROR;
    PropertyRec     *pRec;
    ULONG           cbSize;
    ULONG           cbUsed;
    PCCOR_SIGNATURE pbSig;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if Property is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsPropertyMarked(pr))
        goto ErrExit;

    IfFailGo( m_pMiniMd->GetFilterTable()->MarkProperty( pr ) );

    // marking the backing field, event changing and event changed
    IfFailGo(m_pMiniMd->GetPropertyRecord(RidFromToken(pr), &pRec));

    // Walk the signature and mark all of the embedded types
    IfFailGo(m_pMiniMd->getTypeOfProperty(pRec, &pbSig, &cbSize));
    IfFailGo( MarkSignature(pbSig, cbSize, &cbUsed) );

    // Note that we don't need to mark the MethodSemantics. Because the association of MethodSemantics
    // is marked. The Method column can only store MethodDef, ie the MethodDef has the same parent as
    // this Property.

    IfFailGo( MarkCustomAttributesWithParentToken(pr) );
    // IfFailGo( MarkDeclSecuritiesWithParentToken(pr) );

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkProperty()

//*****************************************************************************
// cascading Mark of all ParamDef associated with a methoddef
//*****************************************************************************
HRESULT FilterManager::MarkParamsWithParentToken(mdMethodDef md)
{
    HRESULT     hr = NOERROR;
    RID         ulStart, ulEnd;
    RID         index;
    MethodRec   *pMethodRec;

    IfFailGo(m_pMiniMd->GetMethodRecord(RidFromToken(md), &pMethodRec));

    // figure out the start rid and end rid of the parameter list of this methoddef
    ulStart = m_pMiniMd->getParamListOfMethod(pMethodRec);
    IfFailGo(m_pMiniMd->getEndParamListOfMethod(RidFromToken(md), &ulEnd));
    for (index = ulStart; index < ulEnd; index ++ )
    {
        RID rid;
        IfFailGo(m_pMiniMd->GetParamRid(index, &rid));
        IfFailGo(MarkParam(TokenFromRid(
            rid,
            mdtParamDef)));
    }
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkParamsWithParentToken()


//*****************************************************************************
// cascading Mark of all methods associated with a TypeDef token
//*****************************************************************************
HRESULT FilterManager::MarkMethodsWithParentToken(mdTypeDef td)
{
    HRESULT     hr = NOERROR;
    RID         ulStart, ulEnd;
    RID         index;
    TypeDefRec  *pTypeDefRec;

    IfFailGo(m_pMiniMd->GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));
    ulStart = m_pMiniMd->getMethodListOfTypeDef( pTypeDefRec );
    IfFailGo(m_pMiniMd->getEndMethodListOfTypeDef(RidFromToken(td), &ulEnd));
    for ( index = ulStart; index < ulEnd; index ++ )
    {
        RID rid;
        IfFailGo(m_pMiniMd->GetMethodRid(index, &rid));
        IfFailGo(MarkMethod(TokenFromRid(
            rid,
            mdtMethodDef)));
    }
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkMethodsWithParentToken()


//*****************************************************************************
// cascading Mark of all MethodImpls associated with a TypeDef token
//*****************************************************************************
HRESULT FilterManager::MarkMethodImplsWithParentToken(mdTypeDef td)
{
    HRESULT     hr = NOERROR;
    RID         index;
    mdToken     tkBody;
    mdToken     tkDecl;
    MethodImplRec *pMethodImplRec;
    HENUMInternal hEnum;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    HENUMInternal::ZeroEnum(&hEnum);
    IfFailGo( m_pMiniMd->FindMethodImplHelper(td, &hEnum) );

    while (HENUMInternal::EnumNext(&hEnum, (mdToken *)&index))
    {
        IfFailGo(m_pMiniMd->GetMethodImplRecord(index, &pMethodImplRec));
        IfFailGo(m_pMiniMd->GetFilterTable()->MarkMethodImpl(index));

        tkBody = m_pMiniMd->getMethodBodyOfMethodImpl(pMethodImplRec);
        IfFailGo( Mark(tkBody) );

        tkDecl = m_pMiniMd->getMethodDeclarationOfMethodImpl(pMethodImplRec);
        IfFailGo( Mark(tkDecl) );
    }
ErrExit:
    HENUMInternal::ClearEnum(&hEnum);
    return hr;
} // HRESULT FilterManager::MarkMethodImplsWithParentToken()


//*****************************************************************************
// cascading Mark of all fields associated with a TypeDef token
//*****************************************************************************
HRESULT FilterManager::MarkFieldsWithParentToken(mdTypeDef td)
{
    HRESULT     hr = NOERROR;
    RID         ulStart, ulEnd;
    RID         index;
    TypeDefRec  *pTypeDefRec;

    IfFailGo(m_pMiniMd->GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));
    ulStart = m_pMiniMd->getFieldListOfTypeDef( pTypeDefRec );
    IfFailGo(m_pMiniMd->getEndFieldListOfTypeDef(RidFromToken(td), &ulEnd));
    for ( index = ulStart; index < ulEnd; index ++ )
    {
        RID rid;
        IfFailGo(m_pMiniMd->GetFieldRid(index, &rid));
        IfFailGo(MarkField(TokenFromRid(
            rid,
            mdtFieldDef)));
    }
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkFieldsWithParentToken()


//*****************************************************************************
// cascading Mark of  all events associated with a TypeDef token
//*****************************************************************************
HRESULT FilterManager::MarkEventsWithParentToken(
    mdTypeDef   td)
{
    HRESULT     hr = NOERROR;
    RID         ridEventMap;
    RID         ulStart, ulEnd;
    RID         index;
    EventMapRec *pEventMapRec;

    // get the starting/ending rid of Events of this typedef
    IfFailGo(m_pMiniMd->FindEventMapFor(RidFromToken(td), &ridEventMap));
    if ( !InvalidRid(ridEventMap) )
    {
        IfFailGo(m_pMiniMd->GetEventMapRecord(ridEventMap, &pEventMapRec));
        ulStart = m_pMiniMd->getEventListOfEventMap( pEventMapRec );
        IfFailGo(m_pMiniMd->getEndEventListOfEventMap(ridEventMap, &ulEnd));
        for ( index = ulStart; index < ulEnd; index ++ )
        {
            RID rid;
            IfFailGo(m_pMiniMd->GetEventRid(index, &rid));
            IfFailGo(MarkEvent(TokenFromRid(
                rid,
                mdtEvent)));
        }
    }
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkEventsWithParentToken()



//*****************************************************************************
// cascading Mark of all properties associated with a TypeDef token
//*****************************************************************************
HRESULT FilterManager::MarkPropertiesWithParentToken(
    mdTypeDef   td)
{
    HRESULT     hr = NOERROR;
    RID         ridPropertyMap;
    RID         ulStart, ulEnd;
    RID         index;
    PropertyMapRec *pPropertyMapRec;

    // get the starting/ending rid of properties of this typedef
    IfFailGo(m_pMiniMd->FindPropertyMapFor(RidFromToken(td), &ridPropertyMap));
    if ( !InvalidRid(ridPropertyMap) )
    {
        IfFailGo(m_pMiniMd->GetPropertyMapRecord(ridPropertyMap, &pPropertyMapRec));
        ulStart = m_pMiniMd->getPropertyListOfPropertyMap( pPropertyMapRec );
        IfFailGo(m_pMiniMd->getEndPropertyListOfPropertyMap(ridPropertyMap, &ulEnd));
        for ( index = ulStart; index < ulEnd; index ++ )
        {
            RID rid;
            IfFailGo(m_pMiniMd->GetPropertyRid(index, &rid));
            IfFailGo(MarkProperty(TokenFromRid(
                rid,
                mdtProperty)));
        }
    }
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkPropertiesWithParentToken()


//*****************************************************************************
// cascading Mark of all GenericPar associated with a TypeDef or MethodDef token
//*****************************************************************************
HRESULT FilterManager::MarkGenericParamWithParentToken(
    mdToken     tk)
{
    HRESULT     hr = NOERROR;
    RID         ulStart, ulEnd;
    RID         index;
    GenericParamRec *pGenericParamRec;
    mdToken     constraint;
    HENUMInternal hEnum;                // To enumerate constraints.

    // Enumerate the GenericPar
    //@todo: Handle the unsorted case.
    IfFailGo( m_pMiniMd->GetGenericParamsForToken(tk, &ulStart, &ulEnd) );

    for (; ulStart < ulEnd; ++ulStart)
    {
        index = m_pMiniMd->GetGenericParamRid(ulStart);
        IfFailGo(m_pMiniMd->GetGenericParamRecord(index, &pGenericParamRec));

        RID ridConstraint;
        IfFailGo( m_pMiniMd->FindGenericParamConstraintHelper(TokenFromRid(ulStart, mdtGenericParam), &hEnum) );
        while (HENUMInternal::EnumNext(&hEnum, (mdToken *) &ridConstraint))
        {
            // Get the constraint.
            GenericParamConstraintRec *pRec;
            IfFailGo(m_pMiniMd->GetGenericParamConstraintRecord(RidFromToken(ridConstraint), &pRec));
            constraint = m_pMiniMd->getConstraintOfGenericParamConstraint(pRec);

            // Mark it.
            IfFailGo( Mark(constraint) );
        }
        HENUMInternal::ClearEnum(&hEnum);
    }

ErrExit:
    HENUMInternal::ClearEnum(&hEnum);

    return hr;
} // HRESULT FilterManager::MarkGenericParamWithParentToken()


//*****************************************************************************
// cascading Mark of an TypeDef token
//*****************************************************************************
HRESULT FilterManager::MarkInterfaceImpls(
    mdTypeDef   td)
{
    HRESULT         hr = NOERROR;
    RID             ridStart, ridEnd;
    RID             i;
    InterfaceImplRec *pRec;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    if ( m_pMiniMd->IsSorted(TBL_InterfaceImpl) )
    {
        IfFailGo(m_pMiniMd->getInterfaceImplsForTypeDef(RidFromToken(td), &ridEnd, &ridStart));
    }
    else
    {
        ridStart = 1;
        ridEnd = m_pMiniMd->getCountInterfaceImpls() + 1;
    }

    // Search for the interfaceimpl with the parent of td
    for (i = ridStart; i < ridEnd; i++)
    {
        IfFailGo(m_pMiniMd->GetInterfaceImplRecord(i, &pRec));
        if ( td != m_pMiniMd->getClassOfInterfaceImpl(pRec) )
            continue;

        // found an InterfaceImpl associate with td. Mark the interface row and the interfaceimpl type
        IfFailGo( m_pMiniMd->GetFilterTable()->MarkInterfaceImpl(TokenFromRid(i, mdtInterfaceImpl)) );
        IfFailGo( MarkCustomAttributesWithParentToken(TokenFromRid(i, mdtInterfaceImpl)) );
        // IfFailGo( MarkDeclSecuritiesWithParentToken(TokenFromRid(i, mdtInterfaceImpl)) );
        IfFailGo( Mark(m_pMiniMd->getInterfaceOfInterfaceImpl(pRec)) );
    }
ErrExit:
    return hr;
} // HRESULT FilterManager::MarkInterfaceImpls()

//*****************************************************************************
// cascading Mark of an TypeDef token
//*****************************************************************************
HRESULT FilterManager::MarkTypeDef(
    mdTypeDef   td)
{
    HRESULT         hr = NOERROR;
    TypeDefRec      *pRec;
    IHostFilter     *pFilter = m_pMiniMd->GetHostFilter();
    DWORD           dwFlags;
    RID             iNester;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if TypeDef is already marked, just return
    if (m_pMiniMd->GetFilterTable()->IsTypeDefMarked(td))
        goto ErrExit;

    // Mark the TypeDef first to avoid duplicate marking
    IfFailGo( m_pMiniMd->GetFilterTable()->MarkTypeDef(td) );
    if (pFilter)
        pFilter->MarkToken(td);

    // We don't need to mark InterfaceImpl but we need to mark the
    // TypeDef/TypeRef associated with InterfaceImpl.
    IfFailGo( MarkInterfaceImpls(td) );

    // mark the base class
    IfFailGo(m_pMiniMd->GetTypeDefRecord(RidFromToken(td), &pRec));
    IfFailGo( Mark(m_pMiniMd->getExtendsOfTypeDef(pRec)) );

    // mark all of the children of this TypeDef
    IfFailGo( MarkMethodsWithParentToken(td) );
    IfFailGo( MarkMethodImplsWithParentToken(td) );
    IfFailGo( MarkFieldsWithParentToken(td) );
    IfFailGo( MarkEventsWithParentToken(td) );
    IfFailGo( MarkPropertiesWithParentToken(td) );

    // mark any GenericParam of this TypeDef
    IfFailGo( MarkGenericParamWithParentToken(td) );

    // mark custom value and permission
    IfFailGo( MarkCustomAttributesWithParentToken(td) );
    IfFailGo( MarkDeclSecuritiesWithParentToken(td) );

    // If the class is a Nested class mark the parent, recursively.
    dwFlags = m_pMiniMd->getFlagsOfTypeDef(pRec);
    if (IsTdNested(dwFlags))
    {
        NestedClassRec      *pNestClassRec;
        IfFailGo(m_pMiniMd->FindNestedClassHelper(td, &iNester));
        if (InvalidRid(iNester))
            IfFailGo(CLDB_E_RECORD_NOTFOUND);
        IfFailGo(m_pMiniMd->GetNestedClassRecord(iNester, &pNestClassRec));
        IfFailGo(MarkTypeDef(m_pMiniMd->getEnclosingClassOfNestedClass(pNestClassRec)));
    }

ErrExit:
    return hr;
} // HRESULT FilterManager::MarkTypeDef()


//*****************************************************************************
// walk signature and mark tokens embedded in the signature
//*****************************************************************************

#define VALIDATE_SIGNATURE_LEN(x)                                   \
    do{ cb = (x);                                               \
        cbUsed += cb; pbSig += cb;                              \
        if (cbUsed > cbSig) IfFailGo(META_E_BAD_SIGNATURE);     \
    }while (0)

#define VALIDATE_SIGNATURE_LEN_HR(x)                                \
    do{ IfFailGo(x);                                            \
        cbUsed += cb; pbSig += cb;                              \
        if (cbUsed > cbSig) IfFailGo(META_E_BAD_SIGNATURE);     \
    }while (0)

HRESULT FilterManager::MarkSignature(
    PCCOR_SIGNATURE pbSig,              // [IN] point to the current byte to visit in the signature
    ULONG       cbSig,                  // [IN] count of bytes available.
    ULONG       *pcbUsed)               // [OUT] count of bytes consumed.
{
    HRESULT     hr = NOERROR;           // A result.
    ULONG       cArg = 0;               // count of arguments in the signature
    ULONG       cTypes = 0;             // Count of argument types in the signature.
    ULONG       cb;                     // Bytes used in a sig element.
    ULONG       cbUsed = 0;             // Total bytes consumed.
    ULONG       callingconv = IMAGE_CEE_CS_CALLCONV_MAX;

    // calling convention
    VALIDATE_SIGNATURE_LEN( CorSigUncompressData(pbSig, &callingconv) );

    if ((callingconv & IMAGE_CEE_CS_CALLCONV_MASK) >= IMAGE_CEE_CS_CALLCONV_MAX)
        IfFailGo(META_E_BAD_SIGNATURE);

    // Field signature is a single element.
    if (isCallConv(callingconv, IMAGE_CEE_CS_CALLCONV_FIELD))
    {
        // It is a FieldDef
        VALIDATE_SIGNATURE_LEN_HR( MarkFieldSignature(pbSig, cbSig - cbUsed, &cb) );
    }
    else
    {
        // If Generic call, get count of type parameters.
        //@TODO: where are the type params?
        if (callingconv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
              VALIDATE_SIGNATURE_LEN( CorSigUncompressData(pbSig, &cTypes) );
        }

        // Count of arguments passed in call.
        VALIDATE_SIGNATURE_LEN( CorSigUncompressData(pbSig, &cArg) );

        // Mark the return type, if there is one (LocalVarSig and GenericInst don't have return types).
        if ( !( isCallConv(callingconv, IMAGE_CEE_CS_CALLCONV_LOCAL_SIG) || isCallConv(callingconv, IMAGE_CEE_CS_CALLCONV_GENERICINST)) )
        {   // process the return type
            VALIDATE_SIGNATURE_LEN_HR( MarkFieldSignature(pbSig, cbSig - cbUsed, &cb) );
        }

        // Iterate over the arguments, and mark each one.
        while (cArg--)
        {
            VALIDATE_SIGNATURE_LEN_HR( MarkFieldSignature(pbSig, cbSig - cbUsed, &cb) );
        }
    }

ErrExit:
    *pcbUsed = cbUsed;
    return hr;
} // HRESULT FilterManager::MarkSignature()


//*****************************************************************************
// walk one type and mark tokens embedded in the signature
//*****************************************************************************
HRESULT FilterManager::MarkFieldSignature(
    PCCOR_SIGNATURE pbSig,              // [IN] point to the current byte to visit in the signature
    ULONG       cbSig,                  // [IN] count of bytes available.
    ULONG       *pcbUsed)               // [OUT] count of bytes consumed.
{
    HRESULT     hr = NOERROR;           // A result.
    ULONG       cb;                     // Bytes in one signature element.
    ULONG       cbUsed = 0;             // Total bytes consumed from signature.
    CorElementType ulElementType;       // ELEMENT_TYPE_xxx from signature.
    ULONG       ulData;                 // Some data (like a count) from the signature.
    ULONG       ulTemp;                 // Unused data.
    mdToken     token;                  // A token from the signature.
    int         iData;                  // Integer data from signature.

    VALIDATE_SIGNATURE_LEN( CorSigUncompressElementType(pbSig, &ulElementType) );

    // Skip the modifiers...
    while (CorIsModifierElementType((CorElementType) ulElementType))
    {
        VALIDATE_SIGNATURE_LEN( CorSigUncompressElementType(pbSig, &ulElementType) );
    }

    // Examine the signature element
    switch (ulElementType)
    {
        case ELEMENT_TYPE_SZARRAY:
            // syntax: SZARRAY <BaseType>

            // conver the base type for the SZARRAY or GENERICARRAY
            VALIDATE_SIGNATURE_LEN_HR( MarkFieldSignature(pbSig, cbSig - cbUsed, &cb) );
            break;

        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
            // syntax: {CMOD_REQD|CMOD_OPT} <token> <signature>

            // now get the embedded token
            VALIDATE_SIGNATURE_LEN( CorSigUncompressToken(pbSig, &token) );

            // Mark the token
            IfFailGo( Mark(token) );

            // mark the base type
            VALIDATE_SIGNATURE_LEN_HR( MarkFieldSignature(pbSig, cbSig - cbUsed, &cb) );
            break;

        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
            // syntax: VAR <index>
            VALIDATE_SIGNATURE_LEN( CorSigUncompressData(pbSig, &ulData) );
            break;

        case ELEMENT_TYPE_ARRAY:
            // syntax: ARRAY BaseType <rank> [i size_1... size_i] [j lowerbound_1 ... lowerbound_j]

            VALIDATE_SIGNATURE_LEN_HR( MarkFieldSignature(pbSig, cbSig - cbUsed, &cb) );

            // Parse for the rank
            VALIDATE_SIGNATURE_LEN( CorSigUncompressData(pbSig, &ulData) );

            // if rank == 0, we are done
            if (ulData == 0)
                break;

            // Any size of dimension specified?
            VALIDATE_SIGNATURE_LEN( CorSigUncompressData(pbSig, &ulData) );

            // Consume sizes of dimension.
            while (ulData--)
            {
                VALIDATE_SIGNATURE_LEN( CorSigUncompressData(pbSig, &ulTemp) );
            }

            // Any lower bounds specified?
            VALIDATE_SIGNATURE_LEN( CorSigUncompressData(pbSig, &ulData) );

            // Consume lower bounds.
            while (ulData--)
            {
                VALIDATE_SIGNATURE_LEN( CorSigUncompressSignedInt(pbSig, &iData) );
            }

            break;

        case ELEMENT_TYPE_FNPTR:
            // function pointer is followed by another complete signature
            VALIDATE_SIGNATURE_LEN_HR( MarkSignature(pbSig, cbSig - cbUsed, &cb) );
            break;

        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
            // syntax: {CLASS | VALUECLASS} <token>
            VALIDATE_SIGNATURE_LEN( CorSigUncompressToken(pbSig, &token) );

            // Mark it.
            IfFailGo( Mark(token) );
            break;

        case ELEMENT_TYPE_GENERICINST:
            // syntax:  ELEMENT_TYPE_GEENRICINST <ELEMENT_TYPE_CLASS | ELEMENT_TYPE_VALUECLASS> <token> <n> <n params>
            VALIDATE_SIGNATURE_LEN_HR( MarkFieldSignature(pbSig, cbSig - cbUsed, &cb) );

            // Get the number of generic parameters
            VALIDATE_SIGNATURE_LEN( CorSigUncompressData(pbSig, &ulData) );

            // Get the generic parameters
            while (ulData--)
            {
                VALIDATE_SIGNATURE_LEN_HR( MarkFieldSignature(pbSig, cbSig - cbUsed, &cb) );
            }
            break;

        default:
            // If valid element (I4, etc), great.  Otherwise, return error.
            if ((ulElementType >= ELEMENT_TYPE_MAX) ||
                (ulElementType == ELEMENT_TYPE_PTR) ||
                (ulElementType == ELEMENT_TYPE_BYREF) ||
                (ulElementType == ELEMENT_TYPE_VALUEARRAY_UNSUPPORTED))
            {
                IfFailGo(META_E_BAD_SIGNATURE);
            }
            break;
    }

ErrExit:
    *pcbUsed = cbUsed;
    return hr;
} // HRESULT FilterManager::MarkFieldSignature()



//*****************************************************************************
//
// Unmark the TypeDef
//
//*****************************************************************************
HRESULT FilterManager::UnmarkTypeDef(
    mdTypeDef       td)
{
    HRESULT         hr = NOERROR;
    TypeDefRec      *pTypeDefRec;
    RID             ridStart, ridEnd;
    RID             index;
    CustomAttributeRec  *pCARec;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME(m_pMiniMd->GetFilterTable() != NULL);

    // if TypeDef is already unmarked, just return
    if (m_pMiniMd->GetFilterTable()->IsTypeDefMarked(td) == false)
        goto ErrExit;

    // Mark the TypeDef first to avoid duplicate marking
    IfFailGo( m_pMiniMd->GetFilterTable()->UnmarkTypeDef(td) );

    // Don't need to unmark InterfaceImpl because the TypeDef is unmarked that will make
    // the InterfaceImpl automatically unmarked.

    // unmark all of the children of this TypeDef
    IfFailGo(m_pMiniMd->GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));

    // unmark the methods
    ridStart = m_pMiniMd->getMethodListOfTypeDef(pTypeDefRec);
    IfFailGo(m_pMiniMd->getEndMethodListOfTypeDef(RidFromToken(td), &ridEnd));
    for ( index = ridStart; index < ridEnd; index ++ )
    {
        RID rid;
        IfFailGo(m_pMiniMd->GetMethodRid(index, &rid));
        IfFailGo(m_pMiniMd->GetFilterTable()->UnmarkMethod(TokenFromRid(
            rid,
            mdtMethodDef)));
    }

    // unmark the fields
    ridStart = m_pMiniMd->getFieldListOfTypeDef(pTypeDefRec);
    IfFailGo(m_pMiniMd->getEndFieldListOfTypeDef(RidFromToken(td), &ridEnd));
    for ( index = ridStart; index < ridEnd; index ++ )
    {
        RID rid;
        IfFailGo(m_pMiniMd->GetFieldRid(index, &rid));
        IfFailGo(m_pMiniMd->GetFilterTable()->UnmarkField(TokenFromRid(
            rid,
            mdtFieldDef)));
    }

    // unmark custom value
    if ( m_pMiniMd->IsSorted( TBL_CustomAttribute ) )
    {
        // table is sorted. ridStart to ridEnd - 1 are all CustomAttribute
        // associated with tkParent
        //
        IfFailGo(m_pMiniMd->getCustomAttributeForToken(td, &ridEnd, &ridStart));
        for (index = ridStart; index < ridEnd; index ++ )
        {
            IfFailGo( m_pMiniMd->GetFilterTable()->UnmarkCustomAttribute( TokenFromRid(index, mdtCustomAttribute) ) );
        }
    }
    else
    {
        // table scan is needed
        ridStart = 1;
        ridEnd = m_pMiniMd->getCountCustomAttributes() + 1;
        for (index = ridStart; index < ridEnd; index ++ )
        {
            IfFailGo(m_pMiniMd->GetCustomAttributeRecord(index, &pCARec));
            if ( td == m_pMiniMd->getParentOfCustomAttribute(pCARec) )
            {
                // This CustomAttribute is associated with tkParent
                IfFailGo( m_pMiniMd->GetFilterTable()->UnmarkCustomAttribute( TokenFromRid(index, mdtCustomAttribute) ) );
            }
        }
    }

    // We don't support nested type!!

ErrExit:
    return hr;

} // HRESULT FilterManager::UnmarkTypeDef()


