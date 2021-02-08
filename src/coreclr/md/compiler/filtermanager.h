// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// FilterManager.h
//

//
// Contains utility code for MD directory
//
//*****************************************************************************
#ifndef __FilterManager__h__
#define __FilterManager__h__




//*********************************************************************
// FilterManager Class
//*********************************************************************
class FilterManager
{
public:
        FilterManager(CMiniMdRW *pMiniMd) {m_pMiniMd = pMiniMd; hasModuleBeenMarked = false; hasAssemblyBeenMarked = false;}
        ~FilterManager() {};

        HRESULT Mark(mdToken tk);

    // Unmark helper
    HRESULT UnmarkTypeDef(mdTypeDef td);
    HRESULT MarkNewUserString(mdString str);


private:
        HRESULT MarkCustomAttribute(mdCustomAttribute cv);
        HRESULT MarkDeclSecurity(mdPermission pe);
        HRESULT MarkStandAloneSig(mdSignature sig);
        HRESULT MarkTypeSpec(mdTypeSpec ts);
        HRESULT MarkTypeRef(mdTypeRef tr);
        HRESULT MarkMemberRef(mdMemberRef mr);
        HRESULT MarkModuleRef(mdModuleRef mr);
        HRESULT MarkAssemblyRef(mdAssemblyRef ar);
        HRESULT MarkModule(mdModule mo);
    HRESULT MarkAssembly(mdAssembly as);
        HRESULT MarkInterfaceImpls(mdTypeDef    td);
    HRESULT MarkUserString(mdString str);

    HRESULT MarkMethodSpec(mdMethodSpec ms);

        HRESULT MarkCustomAttributesWithParentToken(mdToken tkParent);
        HRESULT MarkDeclSecuritiesWithParentToken(mdToken tkParent);
        HRESULT MarkMemberRefsWithParentToken(mdToken tk);

        HRESULT MarkParam(mdParamDef pd);
        HRESULT MarkMethod(mdMethodDef md);
        HRESULT MarkField(mdFieldDef fd);
        HRESULT MarkEvent(mdEvent ev);
        HRESULT MarkProperty(mdProperty pr);

        HRESULT MarkParamsWithParentToken(mdMethodDef md);
        HRESULT MarkMethodsWithParentToken(mdTypeDef td);
    HRESULT MarkMethodImplsWithParentToken(mdTypeDef td);
        HRESULT MarkFieldsWithParentToken(mdTypeDef td);
        HRESULT MarkEventsWithParentToken(mdTypeDef td);
        HRESULT MarkPropertiesWithParentToken(mdTypeDef td);

    HRESULT MarkGenericParamWithParentToken(mdToken tk);


        HRESULT MarkTypeDef(mdTypeDef td);


        // <TODO>We don't want to keep track the debug info with bits because these are going away...</TODO>
        HRESULT MarkMethodDebugInfo(mdMethodDef md);

        // walk the signature and mark all of the embedded TypeDef or TypeRef
        HRESULT MarkSignature(PCCOR_SIGNATURE pbSig, ULONG cbSig, ULONG *pcbUsed);
        HRESULT MarkFieldSignature(PCCOR_SIGNATURE pbSig, ULONG cbSig, ULONG *pcbUsed);


private:
        CMiniMdRW       *m_pMiniMd;
    bool        hasModuleBeenMarked;
    bool        hasAssemblyBeenMarked;
};

#endif // __FilterManager__h__
