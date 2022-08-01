// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: assembler.cpp
//

//

#include "ilasmpch.h"

#include "assembler.h"
#include "binstr.h"
#include "nvpair.h"

#define FAIL_UNLESS(x, y) if (!(x)) { report->error y; return; }

/**************************************************************************/
void Assembler::StartNameSpace(_In_ __nullterminated char* name)
{
    m_NSstack.PUSH(m_szNamespace);
    m_szNamespace = name;
    unsigned L = (unsigned)strlen(m_szFullNS);
    unsigned l = (unsigned)strlen(name);
    if(L+l+1 >= m_ulFullNSLen)
    {
        char* pch = new char[((L+l)/MAX_NAMESPACE_LENGTH + 1)*MAX_NAMESPACE_LENGTH];
        if(pch)
        {
            memcpy(pch,m_szFullNS,L+1);
            delete [] m_szFullNS;
            m_szFullNS = pch;
            m_ulFullNSLen = ((L+l)/MAX_NAMESPACE_LENGTH + 1)*MAX_NAMESPACE_LENGTH;
        }
        else report->error("Failed to reallocate the NameSpace buffer\n");
    }
    if(L) m_szFullNS[L] = NAMESPACE_SEPARATOR_CHAR;
    else L = 0xFFFFFFFF;
    memcpy(&m_szFullNS[L+1],m_szNamespace, l+1);
}

/**************************************************************************/
void Assembler::EndNameSpace()
{
    char *p = &m_szFullNS[strlen(m_szFullNS)-strlen(m_szNamespace)];
    if(p > m_szFullNS) p--;
    *p = 0;
    delete [] m_szNamespace;
    if((m_szNamespace = m_NSstack.POP())==NULL)
    {
        m_szNamespace = new char[2];
        m_szNamespace[0] = 0;
    }
}

/**************************************************************************/
void    Assembler::ClearImplList(void)
{
    while(m_nImplList) m_crImplList[--m_nImplList] = mdTypeRefNil;
}
/**************************************************************************/
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22008) // "Suppress PREfast warnings about integer overflow"
#endif
void    Assembler::AddToImplList(mdToken tk)
{
    if(m_nImplList+1 >= m_nImplListSize)
    {
        mdToken *ptr = new mdToken[m_nImplListSize + MAX_INTERFACES_IMPLEMENTED];
        if(ptr == NULL)
        {
            report->error("Failed to reallocate Impl List from %d to %d bytes\n",
                m_nImplListSize*sizeof(mdToken),
                (m_nImplListSize+MAX_INTERFACES_IMPLEMENTED)*sizeof(mdToken));
            return;
        }
        memcpy(ptr,m_crImplList,m_nImplList*sizeof(mdToken));
        delete m_crImplList;
        m_crImplList = ptr;
        m_nImplListSize += MAX_INTERFACES_IMPLEMENTED;
    }
    m_crImplList[m_nImplList++] = tk;
    m_crImplList[m_nImplList] = mdTypeRefNil;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

void    Assembler::ClearBoundList(void)
{
    m_TyParList = NULL;
}
/**************************************************************************/
mdToken Assembler::ResolveClassRef(mdToken tkResScope, _In_ __nullterminated const char *pszFullClassName, Class** ppClass)
{
    Class *pClass = NULL;
    mdToken tkRet = mdTokenNil;
    mdToken *ptkSpecial = NULL;

    if(pszFullClassName == NULL) return mdTokenNil;

    switch(strlen(pszFullClassName))
    {
        case 11:
            if(strcmp(pszFullClassName,"System.Enum")==0) ptkSpecial = &m_tkSysEnum;
            break;
        case 13:
            if(strcmp(pszFullClassName,"System.Object")==0) ptkSpecial = &m_tkSysObject;
            else if(strcmp(pszFullClassName,"System.String")==0) ptkSpecial = &m_tkSysString;
            break;
        case 16:
            if(strcmp(pszFullClassName,"System.ValueType")==0) ptkSpecial = &m_tkSysValue;
            break;
    }
    if(ptkSpecial) // special token
    {
        if(*ptkSpecial) // already resolved
        {
            tkRet = *ptkSpecial;
            if(ppClass)
            {
                if(TypeFromToken(tkRet)==mdtTypeDef)
                    *ppClass = m_lstClass.PEEK(RidFromToken(tkRet)-1);
                else *ppClass = NULL;
            }
            return tkRet;
        }
        else  // needs to be resolved
            if(!m_fIsMscorlib) tkResScope = GetBaseAsmRef();
    }
    if(tkResScope == 1)
    {
        if((pClass = FindCreateClass(pszFullClassName)) != NULL) tkRet = pClass->m_cl;
    }
    else
    {
        tkRet = MakeTypeRef(tkResScope, pszFullClassName);
        pClass = NULL;
    }
    if(ppClass) *ppClass = pClass;
    if(ptkSpecial) *ptkSpecial = tkRet;
    return tkRet;
}

class TypeSpecContainer
{
private:
    // Contain a BinStr
    unsigned __int8 *ptr_;
    unsigned len_;
    // Hash the BinStr, just for speed of lookup
    unsigned hash_;
    // The value we're looking for
    mdToken token_;
public:
    // Constructor for a 'lookup' object
    TypeSpecContainer(BinStr *typeSpec) :
        ptr_(typeSpec->ptr()),
        len_(typeSpec->length()),
        hash_(typeSpec->length()),
        token_(mdTokenNil)
    {
        for (unsigned i = 0; i < len_; i++)
            hash_ = (hash_ * 257) ^ ((i + 1) * (ptr_[i] ^ 0xA5));
    }
    // Constructor for a 'permanent' object
    // Don't bother re-hashing, since we will always have already constructed the lookup object
    TypeSpecContainer(const TypeSpecContainer &t, mdToken tk) :
        ptr_(new unsigned __int8[t.len_]),
        len_(t.len_),
        hash_(t.hash_),
        token_(tk)
    {
        _ASSERT(tk != mdTokenNil);
        _ASSERT(t.token_ == mdTokenNil);
        memcpy(ptr_, t.ptr_, len_);
    }
    ~TypeSpecContainer()
    {
        if (token_ != mdTokenNil)
            // delete any memory for a 'permanent' object
            delete[] ptr_;
    }
    // this is the operator for a RBTREE
    int ComparedTo(TypeSpecContainer *t) const
    {
        // If they don't hash the same, just diff the hashes
        if (hash_ != t->hash_)
            return hash_ - t->hash_;
        if (len_ != t->len_)
            return len_ - t->len_;
        return memcmp(ptr_, t->ptr_, len_);
    }
    // The only public data we need
    mdToken Token() const { return token_; }
};

static RBTREE<TypeSpecContainer> typeSpecCache;

extern FIFO<char> TyParFixupList;

/**************************************************************************/
mdToken Assembler::ResolveTypeSpec(BinStr* typeSpec)
{
    mdToken tk;

    // It is safe to use the cache only if there are no pending fixups
    if (TyParFixupList.COUNT() != 0)
    {
        if (FAILED(m_pEmitter->GetTokenFromTypeSpec(typeSpec->ptr(), typeSpec->length(), &tk)))
            return mdTokenNil;
        return tk;
    }

    TypeSpecContainer tsc(typeSpec);

    // GetTokenFromTypeSpec is a linear search through an unsorted list
    // Instead of doing that all the time, look this thing up in a cache
    TypeSpecContainer *res = typeSpecCache.FIND(&tsc);
    if (res != NULL)
    {
#ifdef _DEBUG
        // Verify that the cache is in sync with the master copy in metadata
        PCOR_SIGNATURE pSig;
        ULONG cSig;
        m_pImporter->GetTypeSpecFromToken(res->Token(),(PCCOR_SIGNATURE*)&pSig,&cSig);
        _ASSERTE(typeSpec->length() == cSig);
        _ASSERTE(memcmp(typeSpec->ptr(), pSig, cSig) == 0);
#endif

        return res->Token();
    }

    if (FAILED(m_pEmitter->GetTokenFromTypeSpec(typeSpec->ptr(), typeSpec->length(), &tk)))
        return mdTokenNil;

    typeSpecCache.PUSH(new TypeSpecContainer(tsc, tk));
    return tk;
}

/**************************************************************************/
mdToken Assembler::GetAsmRef(_In_ __nullterminated const char* szName)
{
    mdToken tkResScope = 0;
    if(strcmp(szName,"*")==0) tkResScope = mdTokenNil;
    else
    {
        tkResScope = m_pManifest->GetAsmRefTokByName(szName);
        if(RidFromToken(tkResScope)==0)
        {
            // emit the AssemblyRef
            // if it's not self, try to get attributes with Autodetect
            unsigned L = (unsigned)strlen(szName)+1;
            char *sz = new char[L];
            if(sz)
            {
                memcpy(sz,szName,L);
                AsmManAssembly *pAsmRef = m_pManifest->m_pCurAsmRef;
                m_pManifest->StartAssembly(sz,NULL,0,TRUE);
                if(RidFromToken(m_pManifest->GetAsmTokByName(szName))==0)
                {
                    report->warn("Reference to undeclared extern assembly '%s'. Attempting autodetect\n",szName);
                    m_pManifest->SetAssemblyAutodetect();
                }
                m_pManifest->EndAssembly();
                tkResScope = m_pManifest->GetAsmRefTokByName(szName);
                m_pManifest->m_pCurAsmRef = pAsmRef;
            }
            else
                report->error("\nOut of memory!\n");
        }
    }
    return tkResScope;
}

mdToken Assembler::GetBaseAsmRef()
{
    // First we check for "System.Private.CoreLib" as the base or System assembly
    //
    AsmManAssembly* coreLibAsm = m_pManifest->GetAsmRefByAsmName("System.Private.CoreLib");
    if(coreLibAsm != NULL)
    {
        return GetAsmRef(coreLibAsm->szAlias ? coreLibAsm->szAlias : coreLibAsm->szName);
    }

    AsmManAssembly* sysRuntime = m_pManifest->GetAsmRefByAsmName("System.Runtime");
    if(sysRuntime != NULL)
    {
        return GetAsmRef(sysRuntime->szAlias ? sysRuntime->szAlias : sysRuntime->szName);
    }

    AsmManAssembly* mscorlibAsm = m_pManifest->GetAsmRefByAsmName("mscorlib");
    if(mscorlibAsm != NULL)
    {
        return GetAsmRef(mscorlibAsm->szAlias ? mscorlibAsm->szAlias : mscorlibAsm->szName);
    }

    AsmManAssembly* netstandardAsm = m_pManifest->GetAsmRefByAsmName("netstandard");
    if (netstandardAsm != NULL)
    {
        return GetAsmRef(netstandardAsm->szAlias ? netstandardAsm->szAlias : netstandardAsm->szName);
    }

    return GetAsmRef("mscorlib");
}

mdToken Assembler::GetInterfaceImpl(mdToken tsClass, mdToken tsInterface)
{
    mdToken result = mdTokenNil;
    HCORENUM iiEnum = 0;
    ULONG actualInterfaces;
    mdInterfaceImpl impls;

    while (SUCCEEDED(m_pImporter->EnumInterfaceImpls(&iiEnum, tsClass, &impls, 1, &actualInterfaces)))
    {
        if (actualInterfaces == 1)
        {
            mdToken classToken, interfaceToken;
            if (FAILED(m_pImporter->GetInterfaceImplProps(impls, &classToken, &interfaceToken)))
                break;
            if (classToken == tsClass && interfaceToken == tsInterface)
            {
                result = impls;
                break;
            }
        }
    }
    m_pImporter->CloseEnum(iiEnum);
    return result;
}

/**************************************************************************/
mdToken Assembler::GetModRef(_In_ __nullterminated char* szName)
{
    mdToken tkResScope = 0;
    if(!strcmp(szName,m_szScopeName))
            tkResScope = 1; // scope is "this module"
    else
    {
        ImportDescriptor*   pID;
        int i = 0;
        tkResScope = mdModuleRefNil;
        DWORD L = (DWORD)strlen(szName);
        while((pID=m_ImportList.PEEK(i++)))
        {
            if(pID->dwDllName != L) continue;
            if((L > 0) && (strcmp(pID->szDllName,szName)!=0)) continue;
            tkResScope = pID->mrDll;
            break;
        }
        if(RidFromToken(tkResScope)==0)
            report->error("Undefined module ref '%s'\n",szName);
    }
    return tkResScope;
}
/**************************************************************************/
mdToken Assembler::MakeTypeRef(mdToken tkResScope, LPCUTF8 pszFullClassName)
{
    mdToken tkRet = mdTokenNil;
    if(pszFullClassName && *pszFullClassName)
    {
        LPCUTF8 pc;
        if((pc = strrchr(pszFullClassName,NESTING_SEP))) // scope: enclosing class
        {
            LPUTF8 szScopeName;
            DWORD L = (DWORD)(pc-pszFullClassName);
            if((szScopeName = new char[L+1]) != NULL)
            {
                memcpy(szScopeName,pszFullClassName,L);
                szScopeName[L] = 0;
                tkResScope = MakeTypeRef(tkResScope,szScopeName);
                delete [] szScopeName;
            }
            else
                report->error("\nOut of memory!\n");
            pc++;
        }
        else pc = pszFullClassName;
        if(*pc)
        {
            // convert name to widechar
            WszMultiByteToWideChar(g_uCodePage,0,pc,-1,wzUniBuf,dwUniBuf);
            if(FAILED(m_pEmitter->DefineTypeRefByName(tkResScope, wzUniBuf, &tkRet))) tkRet = mdTokenNil;
        }
    }
    return tkRet;
}
/**************************************************************************/

DWORD Assembler::CheckClassFlagsIfNested(Class* pEncloser, DWORD attr)
{
    DWORD wasAttr = attr;
    if(pEncloser && (!IsTdNested(attr)))
    {
        if(OnErrGo)
            report->error("Nested class has non-nested visibility (0x%08X)\n",attr);
        else
        {
            attr &= ~tdVisibilityMask;
            attr |= (IsTdPublic(wasAttr) ? tdNestedPublic : tdNestedPrivate);
            report->warn("Nested class has non-nested visibility (0x%08X), changed to nested (0x%08X)\n",wasAttr,attr);
        }
    }
    else if((pEncloser==NULL) && IsTdNested(attr))
    {
        if(OnErrGo)
            report->error("Non-nested class has nested visibility (0x%08X)\n",attr);
        else
        {
            attr &= ~tdVisibilityMask;
            attr |= (IsTdNestedPublic(wasAttr) ? tdPublic : tdNotPublic);
            report->warn("Non-nested class has nested visibility (0x%08X), changed to non-nested (0x%08X)\n",wasAttr,attr);
        }
    }
    return attr;
}

/**************************************************************************/

void Assembler::StartClass(_In_ __nullterminated char* name, DWORD attr, TyParList *typars)
{
    Class *pEnclosingClass = m_pCurClass;
    char *szFQN;
    ULONG LL;

    m_TyParList = typars;

    if (m_pCurMethod != NULL)
    {
        report->error("Class cannot be declared within a method scope\n");
    }
    if(pEnclosingClass)
    {
        LL = pEnclosingClass->m_dwFQN+(ULONG)strlen(name)+2;
        if((szFQN = new char[LL]))
            sprintf_s(szFQN,LL,"%s%c%s",pEnclosingClass->m_szFQN,NESTING_SEP,name);
        else
            report->error("\nOut of memory!\n");
    }
    else
    {
        unsigned L = (unsigned)strlen(m_szFullNS);
        unsigned LLL = (unsigned)strlen(name);
        LL = L + LLL + (L ? 2 : 1);
        if((szFQN = new char[LL]))
        {
            if(L) sprintf_s(szFQN,LL,"%s.%s",m_szFullNS,name);
            else memcpy(szFQN,name,LL);
            if(LL > MAX_CLASSNAME_LENGTH)
            {
                report->error("Full class name too long (%d characters, %d allowed).\n",LL-1,MAX_CLASSNAME_LENGTH-1);
            }
        }
        else
            report->error("\nOut of memory!\n");
    }
    if(szFQN == NULL) return;

    mdToken tkThis;
    if(m_fIsMscorlib)
        tkThis = ResolveClassRef(1,szFQN,&m_pCurClass); // boils down to FindCreateClass(szFQN)
    else
    {
        m_pCurClass = FindCreateClass(szFQN);
        tkThis = m_pCurClass->m_cl;
    }
    if(m_pCurClass->m_bIsMaster)
    {
        m_pCurClass->m_Attr = CheckClassFlagsIfNested(pEnclosingClass, attr);

        if (m_TyParList)
        {
            m_pCurClass->m_NumTyPars = m_TyParList->ToArray(&(m_pCurClass->m_TyPars));
            delete m_TyParList;
            m_TyParList = NULL;
            RecordTypeConstraints(&m_pCurClass->m_GPCList, m_pCurClass->m_NumTyPars, m_pCurClass->m_TyPars);
        }
        else m_pCurClass->m_NumTyPars = 0;
        m_pCurClass->m_pEncloser = pEnclosingClass;
    } // end if(old class) else
    m_tkCurrentCVOwner = 0;
    m_CustomDescrListStack.PUSH(m_pCustomDescrList);
    m_pCustomDescrList = &(m_pCurClass->m_CustDList);

    m_ClassStack.PUSH(pEnclosingClass);
    ClearBoundList();
}

/**************************************************************************/

void Assembler::AddClass()
{
    mdTypeRef   crExtends = mdTypeRefNil;
    BOOL bIsEnum = FALSE;
    BOOL bIsValueType = FALSE;

    if(m_pCurClass->m_bIsMaster)
    {
        DWORD attr = m_pCurClass->m_Attr;
        if(!IsNilToken(m_crExtends))
        {
            // has a superclass
            if(IsTdInterface(attr)) report->error("Base class in interface\n");
            bIsValueType = (m_crExtends == m_tkSysValue)&&(m_pCurClass->m_cl != m_tkSysEnum);
            bIsEnum = (m_crExtends == m_tkSysEnum);
            crExtends = m_crExtends;
        }
        else
        {
            bIsEnum = ((attr & 0x40000000) != 0);
            bIsValueType = ((attr & 0x80000000) != 0);
        }
        attr &= 0x3FFFFFFF;
        if (m_fAutoInheritFromObject && (crExtends == mdTypeRefNil) && (!IsTdInterface(attr)))
        {
            mdToken tkMscorlib = m_fIsMscorlib ? 1 : GetBaseAsmRef();
            crExtends = bIsEnum ?
                ResolveClassRef(tkMscorlib,"System.Enum",NULL)
                :( bIsValueType ?
                    ResolveClassRef(tkMscorlib,"System.ValueType",NULL)
                    : ResolveClassRef(tkMscorlib, "System.Object",NULL));
        }
        m_pCurClass->m_Attr = attr;
        m_pCurClass->m_crExtends = (m_pCurClass->m_cl == m_tkSysObject)? mdTypeRefNil : crExtends;

        if ((m_pCurClass->m_dwNumInterfaces = m_nImplList) != NULL)
        {
            if(bIsEnum) report->error("Enum implementing interface(s)\n");
            if((m_pCurClass->m_crImplements = new mdTypeRef[m_nImplList+1]) != NULL)
                memcpy(m_pCurClass->m_crImplements, m_crImplList, (m_nImplList+1)*sizeof(mdTypeRef));
            else
            {
                report->error("Failed to allocate Impl List for class '%s'\n", m_pCurClass->m_szFQN);
                m_pCurClass->m_dwNumInterfaces = 0;
            }
        }
        else m_pCurClass->m_crImplements = NULL;
        if(bIsValueType)
        {
            if(!IsTdSealed(attr))
            {
                if(OnErrGo) report->error("Non-sealed value class\n");
                else
                {
                    report->warn("Non-sealed value class, made sealed\n");
                    m_pCurClass->m_Attr |= tdSealed;
                }
            }
        }
        m_pCurClass->m_bIsMaster = FALSE;
    } // end if(old class) else
    ClearImplList();
    m_crExtends = mdTypeRefNil;
}

/**************************************************************************/
void Assembler::EndClass()
{
    m_pCurClass = m_ClassStack.POP();
    m_tkCurrentCVOwner = 0;
    m_pCustomDescrList = m_CustomDescrListStack.POP();
}

/**************************************************************************/
void Assembler::SetPinvoke(BinStr* DllName, int Ordinal, BinStr* Alias, int Attrs)
{
    if(m_pPInvoke) delete m_pPInvoke;
    if(DllName->length())
    {
        if((m_pPInvoke = new PInvokeDescriptor))
        {
            unsigned l;
            ImportDescriptor* pID;
            if((pID = EmitImport(DllName)))
            {
                m_pPInvoke->mrDll = pID->mrDll;
                m_pPInvoke->szAlias = NULL;
                if(Alias)
                {
                    l = Alias->length();
                    if((m_pPInvoke->szAlias = new char[l+1]))
                    {
                        memcpy(m_pPInvoke->szAlias,Alias->ptr(),l);
                        m_pPInvoke->szAlias[l] = 0;
                    }
                    else report->error("\nOut of memory!\n");
                }
                m_pPInvoke->dwAttrs = (DWORD)Attrs;
            }
            else
            {
                delete m_pPInvoke;
                m_pPInvoke = NULL;
                report->error("PInvoke refers to undefined imported DLL\n");
            }
        }
        else
            report->error("Failed to allocate PInvokeDescriptor\n");
    }
    else
    {
        m_pPInvoke = NULL; // No DLL name, it's "local" (IJW) PInvoke
        report->error("Local (embedded native) PInvoke method, the resulting PE file is unusable\n");
    }
    if(DllName) delete DllName;
    if(Alias) delete Alias;
}

/**************************************************************************/
void Assembler::StartMethod(_In_ __nullterminated char* name, BinStr* sig, CorMethodAttr flags, BinStr* retMarshal, DWORD retAttr, TyParList *typars)
{
    if (m_pCurMethod != NULL)
    {
        report->error("Cannot declare a method '%s' within another method\n",name);
    }
    if (!m_fInitialisedMetaData)
    {
        if (FAILED(InitMetaData())) // impl. see WRITER.CPP
        {
            _ASSERTE(0);
        }
    }
    size_t namelen = strlen(name);
    if(namelen >= MAX_CLASSNAME_LENGTH)
    {
        char c = name[MAX_CLASSNAME_LENGTH-1];
        name[MAX_CLASSNAME_LENGTH-1] = 0;
        report->error("Method '%s...' -- name too long (%d characters).\n",name,namelen);
        name[MAX_CLASSNAME_LENGTH-1] = c;
    }
    if (!(flags & mdStatic))
        *(sig->ptr()) |= IMAGE_CEE_CS_CALLCONV_HASTHIS;
    else if(*(sig->ptr()) & (IMAGE_CEE_CS_CALLCONV_HASTHIS | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS))
    {
        if(OnErrGo) report->error("Method '%s' -- both static and instance\n", name);
        else
        {
            report->warn("Method '%s' -- both static and instance, set to static\n", name);
            *(sig->ptr()) &= ~(IMAGE_CEE_CS_CALLCONV_HASTHIS | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS);
        }
    }

    if(!IsMdPrivateScope(flags))
    {
        Method* pMethod;
        Class* pClass = (m_pCurClass ? m_pCurClass : m_pModuleClass);
        DWORD L = (DWORD)strlen(name);
        for(int j=0; (pMethod = pClass->m_MethodList.PEEK(j)); j++)
        {
            if( (pMethod->m_dwName == L) &&
                (!strcmp(pMethod->m_szName,name)) &&
                (pMethod->m_dwMethodCSig == sig->length())  &&
                (!memcmp(pMethod->m_pMethodSig,sig->ptr(),sig->length()))
                &&(!IsMdPrivateScope(pMethod->m_Attr)))
            {
                if(m_fTolerateDupMethods)
                {
                    // reset for new body
                    pMethod->m_lstFixup.RESET(true);
                    //pMethod->m_lstLabel.RESET(true);
                    m_lstLabel.RESET(true);
                    pMethod->m_Locals.RESET(true);
                    delArgNameList(pMethod->m_firstArgName);
                    delArgNameList(pMethod->m_firstVarName);
                    pMethod->m_pCurrScope = &(pMethod->m_MainScope);
                    pMethod->m_pCurrScope->Reset();
                    pMethod->m_firstArgName = getArgNameList();
                    pMethod->m_dwNumExceptions = 0;
                    pMethod->m_dwNumEndfilters = 0;
                    if(pMethod->m_pRetMarshal) delete pMethod->m_pRetMarshal;
                    if(pMethod->m_pRetValue) delete pMethod->m_pRetValue;

                    pMethod->m_MethodImplDList.RESET(false); // ptrs in m_MethodImplDList are dups of those in Assembler

                    pMethod->m_CustomDescrList.RESET(true);

                    if(pMethod->m_fEntryPoint)
                    {
                        pMethod->m_fEntryPoint = FALSE;
                        m_fEntryPointPresent = FALSE;
                    }

                    if(pMethod->m_pbsBody)
                    {
                        // no need to remove relevant MemberRef Fixups from the Assembler list:
                        // their m_fNew flag is set to FALSE anyway.
                        // Just get rid of old method body
                        delete pMethod->m_pbsBody;
                        pMethod->m_pbsBody = NULL;
                    }

                    pMethod->m_fNewBody = TRUE;
                    m_pCurMethod = pMethod;
                }
                else
                    report->error("Duplicate method declaration\n");
                break;
            }
        }
    }
    if(m_pCurMethod == NULL)
    {
        if(m_pCurClass)
        { // instance method
            if(IsMdAbstract(flags) && !IsTdAbstract(m_pCurClass->m_Attr))
            {
                report->error("Abstract method '%s' in non-abstract class '%s'\n",name,m_pCurClass->m_szFQN);
            }
            if(m_pCurClass->m_crExtends == m_tkSysEnum) report->error("Method in enum\n");

            if(!strcmp(name,COR_CTOR_METHOD_NAME))
            {
                flags = (CorMethodAttr)(flags | mdSpecialName);
                if(IsTdInterface(m_pCurClass->m_Attr)) report->error("Instance constructor in interface\n");
            }
            m_pCurMethod = new Method(this, m_pCurClass, name, sig, flags);
        }
        else
        {
            if(IsMdAbstract(flags))
            {
                if(OnErrGo) report->error("Global method '%s' can't be abstract\n",name);
                else
                {
                    report->warn("Global method '%s' can't be abstract, flag removed\n",name);
                    flags = (CorMethodAttr)(((int) flags) &~mdAbstract);
                }
            }
            if(!IsMdStatic(flags))
            {
                if(OnErrGo) report->error("Non-static global method '%s'\n",name);
                else
                {
                    report->warn("Non-static global method '%s', made static\n",name);
                    flags = (CorMethodAttr)(flags | mdStatic);
                    *((BYTE*)(sig->ptr())) &= ~(IMAGE_CEE_CS_CALLCONV_HASTHIS | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS);
                }
            }
            m_pCurMethod = new Method(this, m_pCurClass, name, sig, flags);
            if (m_pCurMethod)
            {
                m_pCurMethod->SetIsGlobalMethod();
                if (m_fInitialisedMetaData == FALSE) InitMetaData();
            }
        }
        if(m_pCurMethod)
        {
            if(!OnErrGo)
            {
                if(m_pCurMethod->m_firstArgName)
                {
                    for(ARG_NAME_LIST *pAN=m_pCurMethod->m_firstArgName; pAN; pAN = pAN->pNext)
                    {
                        if(pAN->dwName)
                        {
                            int k = m_pCurMethod->findArgNum(pAN->pNext,pAN->szName,pAN->dwName);
                            if(k >= 0)
                                report->warn("Duplicate param name '%s' in method '%s'\n",pAN->szName,name);
                        }
                    }
                }
            }
            m_pCurMethod->m_pRetMarshal = retMarshal;
            m_pCurMethod->m_dwRetAttr = retAttr;
            m_tkCurrentCVOwner = 0;
            m_CustomDescrListStack.PUSH(m_pCustomDescrList);
            m_pCustomDescrList = &(m_pCurMethod->m_CustomDescrList);
            m_pCurMethod->m_MainScope.dwStart = m_CurPC;
            if (typars)
            {
                m_pCurMethod->m_NumTyPars = typars->ToArray(&(m_pCurMethod->m_TyPars));
                delete typars;
                m_TyParList = NULL;
                RecordTypeConstraints(&m_pCurMethod->m_GPCList, m_pCurMethod->m_NumTyPars, m_pCurMethod->m_TyPars);
            }
            else m_pCurMethod->m_NumTyPars = 0;
        }
        else report->error("Failed to allocate Method class\n");
    } // end if new method
}

/**************************************************************************/
void Assembler::EndMethod()
{

    if(m_pCurMethod->m_pCurrScope != &(m_pCurMethod->m_MainScope))
    {
        report->error("Invalid lexical scope structure in method %s\n",m_pCurMethod->m_szName);
    }
    m_pCurMethod->m_pCurrScope->dwEnd = m_CurPC;
    if (DoFixups(m_pCurMethod)) AddMethod(m_pCurMethod); //AddMethod - see ASSEM.CPP
    else
    {
        report->error("Method '%s' compilation failed.\n",m_pCurMethod->m_szName);
    }
    //m_pCurMethod->m_lstLabel.RESET(true);
    m_lstLabel.RESET(true);
    m_tkCurrentCVOwner = 0;
    m_pCustomDescrList = m_CustomDescrListStack.POP();
    ResetForNextMethod(); // see ASSEM.CPP
}
/**************************************************************************/
/* rvaLabel is the optional label that indicates this field points at a particular RVA */
void Assembler::AddField(__inout_z __inout char* name, BinStr* sig, CorFieldAttr flags, _In_ __nullterminated char* rvaLabel, BinStr* pVal, ULONG ulOffset)
{
    FieldDescriptor*    pFD;
    ULONG   i,n;
    mdToken tkParent = mdTokenNil;
    Class* pClass;

    if (m_pCurMethod)
        report->error("Field cannot be declared within a method\n");

    if(strlen(name) >= MAX_CLASSNAME_LENGTH)
    {
        char c = name[MAX_CLASSNAME_LENGTH-1];
        name[MAX_CLASSNAME_LENGTH-1] = 0;
        report->error("Field '%s...' -- name too long (%d characters).\n",name,strlen(name));
        name[MAX_CLASSNAME_LENGTH-1] = c;
    }

    if(sig && (sig->length() >= 2))
    {
        if(sig->ptr()[1] == ELEMENT_TYPE_VOID)
            report->error("Illegal use of type 'void'\n");
    }

    if (m_pCurClass)
    {
        tkParent = m_pCurClass->m_cl;

        if(IsTdInterface(m_pCurClass->m_Attr))
        {
            if(!IsFdStatic(flags))
            {
                report->warn("Instance field in interface (CLS violation)\n");
                if(!IsFdPublic(flags)) report->error("Non-public instance field in interface\n");
            }
        }
    }
    else
    {
        if(ulOffset != 0xFFFFFFFF)
        {
            report->warn("Offset in global field '%s' is ignored\n",name);
            ulOffset = 0xFFFFFFFF;
        }
        if(!IsFdStatic(flags))
        {
            if(OnErrGo) report->error("Non-static global field\n");
            else
            {
                report->warn("Non-static global field, made static\n");
                flags = (CorFieldAttr)(flags | fdStatic);
            }
        }
    }
    pClass = (m_pCurClass ? m_pCurClass : m_pModuleClass);
    n = pClass->m_FieldDList.COUNT();
    DWORD L = (DWORD)strlen(name);
    for(i = 0; i < n; i++)
    {
        pFD = pClass->m_FieldDList.PEEK(i);
        if((pFD->m_tdClass == tkParent)&&(L==pFD->m_dwName)&&(!strcmp(pFD->m_szName,name))
            &&(pFD->m_pbsSig->length() == sig->length())
            &&(memcmp(pFD->m_pbsSig->ptr(),sig->ptr(),sig->length())==0))
        {
            report->error("Duplicate field declaration: '%s'\n",name);
            break;
        }
    }
    if (rvaLabel && !IsFdStatic(flags))
        report->error("Only static fields can have 'at' clauses\n");

    if(i >= n)
    {
        if((pFD = new FieldDescriptor))
        {
            pFD->m_tdClass = tkParent;
            pFD->m_szName = name;
            pFD->m_dwName = L;
            pFD->m_fdFieldTok = mdTokenNil;
            if((pFD->m_ulOffset = ulOffset) != 0xFFFFFFFF) pClass->m_dwNumFieldsWithOffset++;
            pFD->m_rvaLabel = rvaLabel;
            pFD->m_pbsSig = sig;
            pFD->m_pClass = pClass;
            pFD->m_pbsValue = pVal;
            pFD->m_pbsMarshal = m_pMarshal;
            pFD->m_pPInvoke = m_pPInvoke;
            pFD->m_dwAttr = flags;

            m_tkCurrentCVOwner = 0;
            m_pCustomDescrList = &(pFD->m_CustomDescrList);

            pClass->m_FieldDList.PUSH(pFD);
            pClass->m_fNewMembers = TRUE;
        }
        else
            report->error("Failed to allocate Field Descriptor\n");
    }
    else
    {
        if(pVal) delete pVal;
        if(m_pPInvoke) delete m_pPInvoke;
        if(m_pMarshal) delete m_pMarshal;
        delete name;
    }
    m_pPInvoke = NULL;
    m_pMarshal = NULL;
}

BOOL Assembler::EmitField(FieldDescriptor* pFD)
{
    WCHAR*   wzFieldName=&wzUniBuf[0];
    HRESULT hr;
    DWORD   cSig;
    COR_SIGNATURE* mySig;
    mdFieldDef mb;
    BYTE    ValType = ELEMENT_TYPE_VOID;
    void * pValue = NULL;
    unsigned lVal = 0;
    BOOL ret = TRUE;

    cSig = pFD->m_pbsSig->length();
    mySig = (COR_SIGNATURE*)(pFD->m_pbsSig->ptr());

    WszMultiByteToWideChar(g_uCodePage,0,pFD->m_szName,-1,wzFieldName,dwUniBuf); //int)cFieldNameLength);
    if(IsFdPrivateScope(pFD->m_dwAttr))
    {
        WCHAR* p = wcsstr(wzFieldName,W("$PST04"));
        if(p) *p = 0;
    }

    if(pFD->m_pbsValue && pFD->m_pbsValue->length())
    {
        ValType = *(pFD->m_pbsValue->ptr());
        lVal = pFD->m_pbsValue->length() - 1; // 1 is type byte
        pValue = (void*)(pFD->m_pbsValue->ptr() + 1);
        if(ValType == ELEMENT_TYPE_STRING)
        {
            //while(lVal % sizeof(WCHAR)) { pFD->m_pbsValue->appendInt8(0); lVal++; }
            lVal /= sizeof(WCHAR);

#if defined(ALIGN_ACCESS) || BIGENDIAN
            void* pValueTemp = _alloca(lVal * sizeof(WCHAR));
            memcpy(pValueTemp, pValue, lVal * sizeof(WCHAR));
            pValue = pValueTemp;

            SwapStringLength((WCHAR*)pValue, lVal);
#endif
        }
    }

    hr = m_pEmitter->DefineField(
        pFD->m_tdClass,
        wzFieldName,
        pFD->m_dwAttr,
        mySig,
        cSig,
        ValType,
        pValue,
        lVal,
        &mb
    );
    if (FAILED(hr))
    {
        report->error("Failed to define field '%s' (HRESULT=0x%08X)\n",pFD->m_szName,hr);
        ret = FALSE;
    }
    else
    {
        //--------------------------------------------------------------------------------
        if(IsFdPinvokeImpl(pFD->m_dwAttr)&&(pFD->m_pPInvoke))
        {
            if(pFD->m_pPInvoke->szAlias == NULL) pFD->m_pPInvoke->szAlias = pFD->m_szName;
            if(FAILED(EmitPinvokeMap(mb,pFD->m_pPInvoke)))
            {
                report->error("Failed to define PInvoke map of .field '%s'\n",pFD->m_szName);
                ret = FALSE;
            }
        }
        //--------------------------------------------------------------------------
        if(pFD->m_pbsMarshal)
        {
            if(FAILED(hr = m_pEmitter->SetFieldMarshal (
                                        mb,                     // [IN] given a fieldDef or paramDef token
                        (PCCOR_SIGNATURE)(pFD->m_pbsMarshal->ptr()),   // [IN] native type specification
                                        pFD->m_pbsMarshal->length())))  // [IN] count of bytes of pvNativeType
            {
                report->error("Failed to set field marshaling for '%s' (HRESULT=0x%08X)\n",pFD->m_szName,hr);
                ret = FALSE;
            }
        }
        //--------------------------------------------------------------------------------
        // Set the RVA to a dummy value.  later it will be fixed
        // up to be something correct, but if we don't emit something
        // the size of the meta-data will not be correct
        if (pFD->m_rvaLabel)
        {
            m_fHaveFieldsWithRvas = TRUE;
            hr = m_pEmitter->SetFieldRVA(mb, 0xCCCCCCCC);
            if (FAILED(hr))
            {
                report->error("Failed to set RVA for field '%s' (HRESULT=0x%08X)\n",pFD->m_szName,hr);
                ret = FALSE;
            }
        }
        //--------------------------------------------------------------------------------
        EmitCustomAttributes(mb, &(pFD->m_CustomDescrList));

    }
    pFD->m_fdFieldTok = mb;
    return ret;
}

/**************************************************************************/
void Assembler::EmitByte(int val)
{
    char ch = (char)val;
    //if((val < -128)||(val > 127))
   //       report->warn("Emitting 0x%X as a byte: data truncated to 0x%X\n",(unsigned)val,(BYTE)ch);
    EmitBytes((BYTE *)&ch,1);
}

/**************************************************************************/
void Assembler::NewSEHDescriptor(void) //sets m_SEHD
{
    m_SEHDstack.PUSH(m_SEHD);
    m_SEHD = new SEH_Descriptor;
    if(m_SEHD == NULL) report->error("Failed to allocate SEH descriptor\n");
}
/**************************************************************************/
void Assembler::SetTryLabels(_In_ __nullterminated char * szFrom, _In_ __nullterminated char *szTo)
{
    if(!m_SEHD) return;
    Label *pLbl = m_pCurMethod->FindLabel(szFrom);
    if(pLbl)
    {
        m_SEHD->tryFrom = pLbl->m_PC;
        if((pLbl = m_pCurMethod->FindLabel(szTo)))  m_SEHD->tryTo = pLbl->m_PC; //FindLabel: Method.CPP
        else report->error("Undefined 2nd label in 'try <label> to <label>'\n");
    }
    else report->error("Undefined 1st label in 'try <label> to <label>'\n");
}
/**************************************************************************/
void Assembler::SetFilterLabel(_In_ __nullterminated char *szFilter)
{
    if(!m_SEHD) return;
    Label *pLbl = m_pCurMethod->FindLabel(szFilter);
    if(pLbl)    m_SEHD->sehFilter = pLbl->m_PC;
    else report->error("Undefined label in 'filter <label>'\n");
}
/**************************************************************************/
void Assembler::SetCatchClass(mdToken catchClass)
{
    if(!m_SEHD) return;
    m_SEHD->cException = catchClass;

}
/**************************************************************************/
void Assembler::SetHandlerLabels(_In_ __nullterminated char *szHandlerFrom, _In_ __nullterminated char *szHandlerTo)
{
    if(!m_SEHD) return;
    Label *pLbl = m_pCurMethod->FindLabel(szHandlerFrom);
    if(pLbl)
    {
        m_SEHD->sehHandler = pLbl->m_PC;
        if(szHandlerTo)
        {
            pLbl = m_pCurMethod->FindLabel(szHandlerTo);
            if(pLbl)
            {
                m_SEHD->sehHandlerTo = pLbl->m_PC;
                return;
            }
        }
        else
        {
            m_SEHD->sehHandlerTo = m_SEHD->sehHandler - 1;
            return;
        }
    }
    report->error("Undefined label in 'handler <label> to <label>'\n");
}
/**************************************************************************/
void Assembler::EmitTry(void) //enum CorExceptionFlag kind, char* beginLabel, char* endLabel, char* handleLabel, char* filterOrClass)
{
    if(m_SEHD)
    {
        bool isFilter=(m_SEHD->sehClause == COR_ILEXCEPTION_CLAUSE_FILTER),
             isFault=(m_SEHD->sehClause == COR_ILEXCEPTION_CLAUSE_FAULT),
             isFinally=(m_SEHD->sehClause == COR_ILEXCEPTION_CLAUSE_FINALLY);

        AddException(m_SEHD->tryFrom, m_SEHD->tryTo, m_SEHD->sehHandler, m_SEHD->sehHandlerTo,
            m_SEHD->cException, isFilter, isFault, isFinally);
    }
    else report->error("Attempt to EmitTry with NULL SEH descriptor\n");
}
/**************************************************************************/

void Assembler::AddException(DWORD pcStart, DWORD pcEnd, DWORD pcHandler, DWORD pcHandlerTo, mdTypeRef crException, BOOL isFilter, BOOL isFault, BOOL isFinally)
{
    if (m_pCurMethod == NULL)
    {
        report->error("Exceptions can be declared only when in a method scope\n");
        return;
    }

    if (m_pCurMethod->m_dwNumExceptions >= m_pCurMethod->m_dwMaxNumExceptions)
    {
        COR_ILMETHOD_SECT_EH_CLAUSE_FAT *ptr =
            new COR_ILMETHOD_SECT_EH_CLAUSE_FAT[m_pCurMethod->m_dwMaxNumExceptions+MAX_EXCEPTIONS];
        if(ptr == NULL)
        {
            report->error("Failed to reallocate SEH buffer\n");
            return;
        }
        memcpy(ptr,m_pCurMethod->m_ExceptionList,m_pCurMethod->m_dwNumExceptions*sizeof(COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
        delete [] m_pCurMethod->m_ExceptionList;
        m_pCurMethod->m_ExceptionList = ptr;
        m_pCurMethod->m_dwMaxNumExceptions += MAX_EXCEPTIONS;
    }

    COR_ILMETHOD_SECT_EH_CLAUSE_FAT *clause = &m_pCurMethod->m_ExceptionList[m_pCurMethod->m_dwNumExceptions];
    clause->SetTryOffset(pcStart);
    clause->SetTryLength(pcEnd - pcStart);
    clause->SetHandlerOffset(pcHandler);
    clause->SetHandlerLength(pcHandlerTo - pcHandler);
    clause->SetClassToken(crException);

    int flags = COR_ILEXCEPTION_CLAUSE_OFFSETLEN;
    if (isFilter) {
        flags |= COR_ILEXCEPTION_CLAUSE_FILTER;
    }
    if (isFault) {
        flags |= COR_ILEXCEPTION_CLAUSE_FAULT;
    }
    if (isFinally) {
        flags |= COR_ILEXCEPTION_CLAUSE_FINALLY;
    }
    clause->SetFlags((CorExceptionFlag)flags);

    m_pCurMethod->m_dwNumExceptions++;
}

/**************************************************************************/
void Assembler::EmitMaxStack(unsigned val)
{
    if(val > 0xFFFF) report->warn(".maxstack parameter exceeds 65535, truncated to %d\n",val&0xFFFF);
    if (m_pCurMethod) m_pCurMethod->m_MaxStack = val&0xFFFF;
    else  report->error(".maxstack can be used only within a method scope\n");
}

/**************************************************************************/
void Assembler::EmitLocals(BinStr* sig)
{
    if(sig)
    {
        if (m_pCurMethod)
        {
            ARG_NAME_LIST   *pAN, *pList= getArgNameList();
            if(pList)
            {
                VarDescr* pVD = NULL;
                for(pAN=pList; pAN; pAN = pAN->pNext)
                {
                    if(pAN->dwAttr == 0) pAN->dwAttr = m_pCurMethod->m_Locals.COUNT() +1;
                    (pAN->dwAttr)--;
                    if((pVD = m_pCurMethod->m_Locals.PEEK(pAN->dwAttr)))
                    {
                        if(pVD->bInScope)
                        {
                            report->warn("Local var slot %d is in use\n",pAN->dwAttr);
                        }
                        if(pVD->pbsSig && ((pVD->pbsSig->length() != pAN->pSig->length()) ||
                            (memcmp(pVD->pbsSig->ptr(),pAN->pSig->ptr(),pVD->pbsSig->length()))))
                        {
                            report->error("Local var slot %d: type conflict\n",pAN->dwAttr);
                        }
                    }
                    else
                    { // create new entry:
                        for(unsigned n = m_pCurMethod->m_Locals.COUNT(); n <= pAN->dwAttr; n++)
                        {
                            pVD = new VarDescr;
                            if(pVD != NULL) m_pCurMethod->m_Locals.PUSH(pVD);
                            else
                            {
                                report->error("Out of memory allocating local var descriptor\n");
                                delete sig;
                                return;
                            }
                        }
                    }
                    pVD->dwSlot = pAN->dwAttr;
                    pVD->pbsSig = pAN->pSig;
                    pVD->bInScope = TRUE;
                }
                if(pVD->pbsSig && (pVD->pbsSig->length() == 1))
                {
                    if(pVD->pbsSig->ptr()[0] == ELEMENT_TYPE_VOID)
                        report->error("Illegal local var type: 'void'\n");
                }
                m_pCurMethod->m_pCurrScope->pLocals =
                    m_pCurMethod->catArgNameList(m_pCurMethod->m_pCurrScope->pLocals, pList);
            }
        }
        else    report->error(".locals can be used only within a method scope\n");
        delete sig;
    }
    else report->error("Attempt to EmitLocals with NULL argument\n");
}

/**************************************************************************/
void Assembler::EmitEntryPoint()
{
    if (m_pCurMethod)
    {
        if(!m_fEntryPointPresent)
        {
            if(IsMdStatic(m_pCurMethod->m_Attr))
            {
                m_pCurMethod->m_fEntryPoint = TRUE;
                m_fEntryPointPresent = TRUE;
            }
            else report->error("Non-static method as entry point\n");
        }
        else report->error("Multiple .entrypoint declarations\n");
    }
    else report->error(".entrypoint can be used only within a method scope\n");
}

/**************************************************************************/
void Assembler::EmitZeroInit()
{
    if (m_pCurMethod) m_pCurMethod->m_Flags |= CorILMethod_InitLocals;
    else report->error(".zeroinit can be used only within a method scope\n");
}

/**************************************************************************/
void Assembler::SetImplAttr(unsigned short attrval)
{
    if (m_pCurMethod)
    {
        if(IsMiNative(attrval)||IsMiOPTIL(attrval)||IsMiUnmanaged(attrval))
            report->error("Cannot compile native/unmanaged method\n");
        m_pCurMethod->m_wImplAttr = attrval;
    }
}

/**************************************************************************/
void Assembler::EmitData(_In_opt_ void *buffer, unsigned len)
{
    if (len != 0)
    {
        void* ptr;
        HRESULT hr = m_pCeeFileGen->GetSectionBlock(m_pCurSection, len, 1, &ptr);
        if (FAILED(hr))
        {
            report->error("Could not extend data section (out of memory?)");
            exit(1);
        }

        if (buffer != NULL)
        {
            memcpy(ptr, buffer, len);
        }
        else
        {
            memset(ptr, 0, len);
        }
    }
}

/**************************************************************************/
void Assembler::EmitDD(_In_ __nullterminated char *str)
{
    DWORD       dwAddr = 0;
    GlobalLabel *pLabel = FindGlobalLabel(str);

    ULONG loc;
    HRESULT hr = m_pCeeFileGen->GetSectionDataLen(m_pCurSection, &loc);
    _ASSERTE(SUCCEEDED(hr));

    DWORD* ptr;
    DWORD sizeofptr = (DWORD)((m_dwCeeFileFlags & ICEE_CREATE_FILE_PE32) ? sizeof(DWORD) : sizeof(__int64));
    hr = m_pCeeFileGen->GetSectionBlock(m_pCurSection, sizeofptr, 1, (void**) &ptr);
    if (FAILED(hr))
    {
        report->error("Could not extend data section (out of memory?)");
        exit(1);
    }

    if (pLabel != 0) {
        dwAddr = pLabel->m_GlobalOffset;
        if (pLabel->m_Section != m_pGlobalDataSection) {
            report->error("For '&label', label must be in data section");
            m_State = STATE_FAIL;
            }
        }
    else
        AddDeferredGlobalFixup(str, (BYTE*) ptr);

    hr = m_pCeeFileGen->AddSectionReloc(m_pCurSection, loc, m_pGlobalDataSection, srRelocHighLow);
    _ASSERTE(SUCCEEDED(hr));
    if(m_dwCeeFileFlags & ICEE_CREATE_FILE_STRIP_RELOCS)
    {
        report->error("Base relocations are emitted, while /STRIPRELOC option has been specified");
    }
    if(m_dwCeeFileFlags & ICEE_CREATE_FILE_PE32)
    {
        m_dwComImageFlags &= ~COMIMAGE_FLAGS_ILONLY;
        if (m_dwCeeFileFlags & ICEE_CREATE_MACHINE_I386)
            COR_SET_32BIT_REQUIRED(m_dwComImageFlags);
        *ptr = dwAddr;
    }
    else
    {
        m_dwComImageFlags &= ~COMIMAGE_FLAGS_ILONLY;
        *((__int64*)ptr) = (__int64)dwAddr;
    }
}

/**************************************************************************/
GlobalLabel *Assembler::FindGlobalLabel(LPCUTF8 pszName)
{
    GlobalLabel lSearch(pszName,0,NULL), *pL;
    pL =  m_lstGlobalLabel.FIND(&lSearch);
    lSearch.m_szName = NULL;
    return pL;
    //return  m_lstGlobalLabel.FIND(pszName);
}

/**************************************************************************/

GlobalFixup *Assembler::AddDeferredGlobalFixup(_In_ __nullterminated char *pszLabel, BYTE* pReference)
{
    GlobalFixup *pNew = new GlobalFixup(pszLabel, (BYTE*) pReference);
    if (pNew == NULL)
    {
        report->error("Failed to allocate global fixup\n");
        m_State = STATE_FAIL;
    }
    else
        m_lstGlobalFixup.PUSH(pNew);

    return pNew;
}

/**************************************************************************/
void Assembler::AddDeferredILFixup(ILFixupType Kind)
{
    _ASSERTE(Kind != ilGlobal);
  AddDeferredILFixup(Kind, NULL);
}
/**************************************************************************/

void Assembler::AddDeferredILFixup(ILFixupType Kind,
                                   GlobalFixup *GFixup)
{
    ILFixup *pNew = new ILFixup(m_CurPC, Kind, GFixup);

    _ASSERTE(m_pCurMethod != NULL);
    if (pNew == NULL)
    {
        report->error("Failed to allocate IL fixup\n");
        m_State = STATE_FAIL;
    }
    else
        m_pCurMethod->m_lstILFixup.PUSH(pNew);
}

/**************************************************************************/
void Assembler::EmitDataString(BinStr* str)
{
    if(str)
    {
        str->appendInt8(0);
        DWORD   DataLen = str->length();
        char    *pb = (char*)(str->ptr());
        WCHAR   *UnicodeString = (DataLen >= dwUniBuf) ? new WCHAR[DataLen] : &wzUniBuf[0];

        if(UnicodeString)
        {
            WszMultiByteToWideChar(g_uCodePage,0,pb,-1,UnicodeString,DataLen);
            EmitData(UnicodeString,DataLen*sizeof(WCHAR));
            if(DataLen >= dwUniBuf) delete [] UnicodeString;
        }
        else report->error("\nOut of memory!\n");
        delete str;
    }
}



/**************************************************************************/
unsigned Assembler::OpcodeLen(Instr* instr)
{
    return (m_fStdMapping ? OpcodeInfo[instr->opcode].Len : 3);
}
/**************************************************************************/
void Assembler::EmitOpcode(Instr* instr)
{
    if(m_fGeneratePDB &&
       ((instr->linenum != m_ulLastDebugLine)
        ||(instr->column != m_ulLastDebugColumn)
        ||(instr->linenum_end != m_ulLastDebugLineEnd)
        ||(instr->column_end != m_ulLastDebugColumnEnd)))
    {
        if(m_pCurMethod)
        {
            LinePC *pLPC = new LinePC;
            if(pLPC)
            {
                pLPC->Line = instr->linenum;
                pLPC->Column = instr->column;
                pLPC->LineEnd = instr->linenum_end;
                pLPC->ColumnEnd = instr->column_end;
                pLPC->PC = m_CurPC;

                pLPC->pOwnerDocument = instr->pOwnerDocument;
                if (0xfeefee == instr->linenum &&
                    0xfeefee == instr->linenum_end &&
                    0 == instr->column &&
                    0 == instr->column_end)
                {
                    pLPC->IsHidden = TRUE;
                }
                else
                {
                    pLPC->IsHidden = FALSE;
                }

                m_pCurMethod->m_LinePCList.PUSH(pLPC);
            }
            else report->error("\nOut of memory!\n");
        }
        m_ulLastDebugLine = instr->linenum;
        m_ulLastDebugColumn = instr->column;
        m_ulLastDebugLineEnd = instr->linenum_end;
        m_ulLastDebugColumnEnd = instr->column_end;
    }
    if(instr->opcode == CEE_ENDFILTER)
    {
        if(m_pCurMethod)
        {
            if(m_pCurMethod->m_dwNumEndfilters >= m_pCurMethod->m_dwMaxNumEndfilters)
            {
                DWORD *pdw = new DWORD[m_pCurMethod->m_dwMaxNumEndfilters+MAX_EXCEPTIONS];
                if(pdw == NULL)
                {
                    report->error("Failed to reallocate auxiliary SEH buffer\n");
                    instr->opcode = -1;
                    return;
                }
                memcpy(pdw,m_pCurMethod->m_EndfilterOffsetList,m_pCurMethod->m_dwNumEndfilters*sizeof(DWORD));
                delete m_pCurMethod->m_EndfilterOffsetList;
                m_pCurMethod->m_EndfilterOffsetList = pdw;
                m_pCurMethod->m_dwMaxNumEndfilters += MAX_EXCEPTIONS;
            }
            m_pCurMethod->m_EndfilterOffsetList[m_pCurMethod->m_dwNumEndfilters++] = m_CurPC+2;
        }
    }
    if (m_fStdMapping)
    {
        if (OpcodeInfo[instr->opcode].Len == 2)
            EmitByte(OpcodeInfo[instr->opcode].Std1);
        EmitByte(OpcodeInfo[instr->opcode].Std2);
    }
    else
    {
        unsigned short us = (unsigned short)instr->opcode;
        EmitByte(REFPRE);
        EmitBytes((BYTE *)&us,2);
    }
    instr->opcode = -1;
}

/**************************************************************************/
//void Assembler::OptimizeInstr(Instr* instr, int var)
//{

//}
/**************************************************************************/
unsigned Assembler::ShortOf(unsigned opcode)
{
    unsigned retcode;
    switch(opcode)
    {
        case CEE_LDARG:     retcode=CEE_LDARG_S;    break;
        case CEE_LDARGA:    retcode=CEE_LDARGA_S;   break;
        case CEE_STARG:     retcode=CEE_STARG_S;    break;

        case CEE_LDLOC:     retcode=CEE_LDLOC_S;    break;
        case CEE_LDLOCA:    retcode=CEE_LDLOCA_S;   break;
        case CEE_STLOC:     retcode=CEE_STLOC_S;    break;

        case CEE_BR:        retcode=CEE_BR_S;       break;
        case CEE_BRFALSE:   retcode=CEE_BRFALSE_S;  break;
        case CEE_BRTRUE:    retcode=CEE_BRTRUE_S;   break;
        case CEE_BEQ:       retcode=CEE_BEQ_S;      break;
        case CEE_BGE:       retcode=CEE_BGE_S;      break;
        case CEE_BGT:       retcode=CEE_BGT_S;      break;
        case CEE_BLE:       retcode=CEE_BLE_S;      break;
        case CEE_BLT:       retcode=CEE_BLT_S;      break;
        case CEE_BNE_UN:    retcode=CEE_BNE_UN_S;   break;
        case CEE_BGE_UN:    retcode=CEE_BGE_UN_S;   break;
        case CEE_BGT_UN:    retcode=CEE_BGT_UN_S;   break;
        case CEE_BLE_UN:    retcode=CEE_BLE_UN_S;   break;
        case CEE_BLT_UN:    retcode=CEE_BLT_UN_S;   break;
        case CEE_LEAVE:     retcode=CEE_LEAVE_S;    break;

        case CEE_LDC_I4:    retcode=CEE_LDC_I4_S;   break;
        case CEE_LDC_R8:    retcode=CEE_LDC_R4;     break;


        default: retcode = opcode;  break;
    }
    return retcode;
}

/**************************************************************************/
void Assembler::EmitInstrVar(Instr* instr, int var)
{
    unsigned opc = instr->opcode;
    if(m_fOptimize)
    {
        if(var < 4)
        {
            switch(opc)
            {
                case CEE_LDARG:
                case CEE_LDARG_S: opc = CEE_LDARG_0 + var; break;

                case CEE_LDLOC:
                case CEE_LDLOC_S: opc = CEE_LDLOC_0 + var; break;

                case CEE_STLOC:
                case CEE_STLOC_S: opc = CEE_STLOC_0 + var; break;

                default: break;
            }
            if(opc != (unsigned) instr->opcode)
            {
                instr->opcode = opc;
                EmitOpcode(instr);
                return;
            }
        }
        if(var <= 0xFF)
        {
            opc = instr->opcode = ShortOf(opc);
        }
    }
    EmitOpcode(instr);
    if (isShort(opc))
    {
        EmitByte(var);
    }
    else
    {
        short sh = (short)var;
        EmitBytes((BYTE *)&sh,2);
    }
}

/**************************************************************************/
void Assembler::EmitInstrVarByName(Instr* instr, _In_ __nullterminated char* label)
{
    int idx = -1, nArgVarFlag=0;
    switch(instr->opcode)
    {
        case CEE_LDARGA:
        case CEE_LDARGA_S:
        case CEE_LDARG:
        case CEE_LDARG_S:
        case CEE_STARG:
        case CEE_STARG_S:
            nArgVarFlag++;
            FALLTHROUGH;
        case CEE_LDLOCA:
        case CEE_LDLOCA_S:
        case CEE_LDLOC:
        case CEE_LDLOC_S:
        case CEE_STLOC:
        case CEE_STLOC_S:

            if(m_pCurMethod)
            {
                DWORD L = (DWORD)strlen(label);
                if(nArgVarFlag == 1)
                {
                    idx = m_pCurMethod->findArgNum(m_pCurMethod->m_firstArgName,label,L);
                }
                else
                {
                    for(Scope* pSC = m_pCurMethod->m_pCurrScope; pSC; pSC=pSC->pSuperScope)
                    {
                        idx = m_pCurMethod->findLocSlot(pSC->pLocals,label,L);
                        if(idx >= 0) break;
                    }
                }
                if(idx >= 0) EmitInstrVar(instr,
                    ((nArgVarFlag==0)||(m_pCurMethod->m_Attr & mdStatic))? idx : idx+1);
                else    report->error("Undeclared identifier %s\n",label);
            }
            else
                report->error("Instructions can be used only when in a method scope\n");
            break;
        default:
            report->error("Named argument illegal for this instruction\n");
    }
    instr->opcode = -1; // in case we got here with error
}

/**************************************************************************/
void Assembler::EmitInstrI(Instr* instr, int val)
{
    int opc = instr->opcode;
    if(m_fOptimize)
    {
        if((val >= -1)&&(val <= 8))
        {
            switch(opc)
            {
                case CEE_LDC_I4:
                case CEE_LDC_I4_S: opc = CEE_LDC_I4_M1 + (val+1); break;

                default: break;
            }
            if(opc != instr->opcode)
            {
                instr->opcode = opc;
                EmitOpcode(instr);
                return;
            }
        }
        if((-128 <= val)&&(val <= 127))
        {
            opc = instr->opcode = ShortOf(opc);
        }
    }
    EmitOpcode(instr);
    if (isShort(opc))
    {
        EmitByte(val);
    }
    else
    {
        int i = val;
        EmitBytes((BYTE *)&i,sizeof(int));
    }
}

/**************************************************************************/
void Assembler::EmitInstrI8(Instr* instr, __int64* val)
{
    EmitOpcode(instr);
    EmitBytes((BYTE *)val, sizeof(__int64));
    delete val;
}

/**************************************************************************/
void Assembler::EmitInstrR(Instr* instr, double* pval)
{
    unsigned opc = instr->opcode;
    EmitOpcode(instr);
    if (isShort(opc))
    {
        float val = (float)*pval;
        EmitBytes((BYTE *)&val, sizeof(float));
    }
    else
        EmitBytes((BYTE *)pval, sizeof(double));
}

/**************************************************************************/
void Assembler::EmitInstrBrTarget(Instr* instr, _In_ __nullterminated char* label)
{
    Label * pLabel = m_pCurMethod->FindLabel(label);
    int offset=0;
    if (pLabel == NULL) // branching forward -- no optimization
    {
        BYTE pcrelsize = 1+(isShort(instr->opcode) ? 1 : 4); //size of the instruction plus argument
        AddDeferredFixup(label, m_pCurOutputPos+1,
                                       (m_CurPC + pcrelsize), pcrelsize-1);
    }
    else
    {
        offset = pLabel->m_PC - m_CurPC;
        if(m_fOptimize)
        {
            if((-128 <= offset-5)&&(offset-2 <= 127)) //need to take into account the argument size (worst cases)
            {
                instr->opcode = ShortOf(instr->opcode);
            }
        }
        if(isShort(instr->opcode))
        {
            offset -= 2;
            if((-128 > offset)||(offset > 127))
                report->error("Offset too large for short branching instruction, truncated\n");
        }
        else
            offset -= 5;
        delete [] label;
    }
    int opc = instr->opcode;
    EmitOpcode(instr);
    if(isShort(opc))  EmitByte(offset);
    else              EmitBytes((BYTE *)&offset,4);
}
/**************************************************************************/
void Assembler::AddDeferredFixup(_In_ __nullterminated char *pszLabel, BYTE *pBytes, DWORD RelativeToPC, BYTE FixupSize)
{
    Fixup *pNew = new Fixup(pszLabel, pBytes, RelativeToPC, FixupSize);

    if (pNew == NULL)
    {
        report->error("Failed to allocate deferred fixup\n");
        m_State = STATE_FAIL;
    }
    else
        m_pCurMethod->m_lstFixup.PUSH(pNew);
}
/**************************************************************************/
void Assembler::EmitInstrBrOffset(Instr* instr, int offset)
{
    unsigned opc=instr->opcode;
    if(m_fOptimize)
    {
        if((-128 <= offset)&&(offset <= 127))
        {
            opc = instr->opcode = ShortOf(opc);
        }
    }
    EmitOpcode(instr);
    if(isShort(opc))    EmitByte(offset);
    else
    {
        int i = offset;
        EmitBytes((BYTE *)&i,4);
    }
}

/**************************************************************************/
mdToken Assembler::MakeMemberRef(mdToken cr, _In_ __nullterminated char* pszMemberName, BinStr* sig)
{
    DWORD           cSig = sig->length();
    COR_SIGNATURE*  mySig = (COR_SIGNATURE *)(sig->ptr());
    mdToken         mr = mdMemberRefNil;
    Class*          pClass = NULL;
    if(cr == 0x00000001) cr = mdTokenNil; // Module -> nil for globals
    if(TypeFromToken(cr) == mdtTypeDef) pClass = m_lstClass.PEEK(RidFromToken(cr)-1);
    if((TypeFromToken(cr) == mdtTypeDef)||(cr == mdTokenNil))
    {
        MemberRefDescriptor* pMRD = new MemberRefDescriptor;
        if(pMRD)
        {
            pMRD->m_tdClass = cr;
            pMRD->m_pClass = pClass;
            pMRD->m_szName = pszMemberName;
            pMRD->m_dwName = (DWORD)strlen(pszMemberName);
            pMRD->m_pSigBinStr = sig;
            pMRD->m_tkResolved = 0;
            if(*(sig->ptr())== IMAGE_CEE_CS_CALLCONV_FIELD)
            {
                m_LocalFieldRefDList.PUSH(pMRD);
                mr = 0x98000000 | m_LocalFieldRefDList.COUNT();
            }
            else
            {
                m_LocalMethodRefDList.PUSH(pMRD);
                mr = 0x99000000 | m_LocalMethodRefDList.COUNT();
            }
        }
        else
        {
            report->error("Failed to allocate MemberRef Descriptor\n");
            return 0;
        }
    }
    else
    {
        WszMultiByteToWideChar(g_uCodePage,0,pszMemberName,-1,wzUniBuf,dwUniBuf);

        if(cr == mdTokenNil) cr = mdTypeRefNil;
        if(TypeFromToken(cr) == mdtAssemblyRef)
        {
            report->error("Cross-assembly global references are not supported ('%s')\n", pszMemberName);
            mr = 0;
        }
        else
        {
            HRESULT hr = m_pEmitter->DefineMemberRef(cr, wzUniBuf, mySig, cSig, &mr);
            if(FAILED(hr))
            {
                report->error("Unable to define member reference '%s'\n", pszMemberName);
                mr = 0;
            }
        }
        delete [] pszMemberName;
        delete sig;
    }
    return mr;
}
/**************************************************************************/
void Assembler::SetMemberRefFixup(mdToken tk, unsigned opcode_len)
{
    if(opcode_len)
    {
        switch(TypeFromToken(tk))
        {
            case 0x98000000:
            case 0x99000000:
            case 0x9A000000:
                if(m_pCurMethod != NULL)
                    m_pCurMethod->m_LocalMemberRefFixupList.PUSH(
                            new LocalMemberRefFixup(tk,(size_t)(m_CurPC + opcode_len)));
                break;
        }
    }
}

/**************************************************************************/
mdToken Assembler::MakeMethodSpec(mdToken tkParent, BinStr* sig)
{
    DWORD           cSig = sig->length();
    COR_SIGNATURE*  mySig = (COR_SIGNATURE *)(sig->ptr());
    mdMethodSpec mi = mdMethodSpecNil;
    if(TypeFromToken(tkParent) == 0x99000000) // Local MemberRef: postpone until resolved
    {
        MemberRefDescriptor* pMRD = new MemberRefDescriptor;
        if(pMRD)
        {
            memset(pMRD,0,sizeof(MemberRefDescriptor));
            pMRD->m_tdClass = tkParent;
            pMRD->m_pSigBinStr = sig;
            m_MethodSpecList.PUSH(pMRD);
            mi = 0x9A000000 | m_MethodSpecList.COUNT();
        }
        else
        {
            report->error("Failed to allocate MemberRef Descriptor\n");
            return 0;
        }
    }
    else
    {
        HRESULT hr = m_pEmitter->DefineMethodSpec(tkParent, mySig, cSig, &mi);
        if(FAILED(hr))
        {
            report->error("Unable to define method instantiation");
            return 0;
        }
    }
    return mi;
}

/**************************************************************************/
void Assembler::EndEvent(void)
{
    Class* pClass = (m_pCurClass ? m_pCurClass : m_pModuleClass);
    if(m_pCurEvent->m_tkAddOn == 0)
        report->error("Event %s of class %s has no Add method. Event not emitted.",
                      m_pCurEvent->m_szName,pClass->m_szFQN);
    else if(m_pCurEvent->m_tkRemoveOn == 0)
        report->error("Event %s of class %s has no Remove method. Event not emitted.",
                      m_pCurEvent->m_szName,pClass->m_szFQN);
    else
    {
        pClass->m_EventDList.PUSH(m_pCurEvent);
        pClass->m_fNewMembers = TRUE;
    }
    m_pCurEvent = NULL;
    m_tkCurrentCVOwner = 0;
    m_pCustomDescrList = m_CustomDescrListStack.POP();
}

void Assembler::ResetEvent(__inout_z __inout char* szName, mdToken typeSpec, DWORD dwAttr)
{
    if(strlen(szName) >= MAX_CLASSNAME_LENGTH)
    {
        char c = szName[MAX_CLASSNAME_LENGTH-1];
        szName[MAX_CLASSNAME_LENGTH-1] = 0;
        report->error("Event '%s...' -- name too long (%d characters).\n",szName,strlen(szName));
        szName[MAX_CLASSNAME_LENGTH-1] = c;
    }
    if((m_pCurEvent = new EventDescriptor))
    {
        memset(m_pCurEvent,0,sizeof(EventDescriptor));
        m_pCurEvent->m_tdClass = m_pCurClass->m_cl;
        m_pCurEvent->m_szName = szName;
        m_pCurEvent->m_dwAttr = dwAttr;
        m_pCurEvent->m_tkEventType = typeSpec;
        m_pCurEvent->m_fNew = TRUE;
        m_tkCurrentCVOwner = 0;
        m_CustomDescrListStack.PUSH(m_pCustomDescrList);
        m_pCustomDescrList = &(m_pCurEvent->m_CustomDescrList);
    }
    else report->error("Failed to allocate Event Descriptor\n");
}

void Assembler::SetEventMethod(int MethodCode, mdToken tk)
{
    switch(MethodCode)
    {
        case 0:
            m_pCurEvent->m_tkAddOn = tk;
            break;
        case 1:
            m_pCurEvent->m_tkRemoveOn = tk;
            break;
        case 2:
            m_pCurEvent->m_tkFire = tk;
            break;
        case 3:
            m_pCurEvent->m_tklOthers.PUSH((mdToken*)(UINT_PTR)tk);
            break;
    }
}
/**************************************************************************/

void Assembler::EndProp(void)
{
    Class* pClass = (m_pCurClass ? m_pCurClass : m_pModuleClass);
    pClass->m_PropDList.PUSH(m_pCurProp);
    pClass->m_fNewMembers = TRUE;
    m_pCurProp = NULL;
    m_tkCurrentCVOwner = 0;
    m_pCustomDescrList = m_CustomDescrListStack.POP();
}

void Assembler::ResetProp(__inout_z __inout char * szName, BinStr* bsType, DWORD dwAttr, BinStr* pValue)
{
    DWORD           cSig = bsType->length();
    COR_SIGNATURE*  mySig = (COR_SIGNATURE *)(bsType->ptr());

    if(strlen(szName) >= MAX_CLASSNAME_LENGTH)
    {
        char c = szName[MAX_CLASSNAME_LENGTH-1];
        szName[MAX_CLASSNAME_LENGTH-1] = 0;
        report->error("Property '%s...' -- name too long (%d characters).\n",szName,strlen(szName));
        szName[MAX_CLASSNAME_LENGTH-1] = c;
    }
    m_pCurProp = new PropDescriptor;
    if(m_pCurProp == NULL)
    {
        report->error("Failed to allocate Property Descriptor\n");
        return;
    }
    memset(m_pCurProp,0,sizeof(PropDescriptor));
    m_pCurProp->m_tdClass = m_pCurClass->m_cl;
    m_pCurProp->m_szName = szName;
    m_pCurProp->m_dwAttr = dwAttr;
    m_pCurProp->m_fNew = TRUE;

    m_pCurProp->m_pSig = new COR_SIGNATURE[cSig];
    if(m_pCurProp->m_pSig == NULL)
    {
        report->error("\nOut of memory!\n");
        return;
    }
    memcpy(m_pCurProp->m_pSig,mySig,cSig);
    m_pCurProp->m_dwCSig = cSig;

    if(pValue && pValue->length())
    {
        BYTE* pch = pValue->ptr();
        m_pCurProp->m_dwCPlusTypeFlag = (DWORD)(*pch);
        m_pCurProp->m_cbValue = pValue->length() - 1;
        m_pCurProp->m_pValue = (PVOID)(pch+1);
        if(m_pCurProp->m_dwCPlusTypeFlag == ELEMENT_TYPE_STRING) m_pCurProp->m_cbValue /= sizeof(WCHAR);
        m_pCurProp->m_dwAttr |= prHasDefault;
    }
    else
    {
        m_pCurProp->m_dwCPlusTypeFlag = ELEMENT_TYPE_VOID;
        m_pCurProp->m_pValue = NULL;
        m_pCurProp->m_cbValue = 0;
    }
    m_tkCurrentCVOwner = 0;
    m_CustomDescrListStack.PUSH(m_pCustomDescrList);
    m_pCustomDescrList = &(m_pCurProp->m_CustomDescrList);
}

void Assembler::SetPropMethod(int MethodCode, mdToken tk)
{
    switch(MethodCode)
    {
        case 0:
            m_pCurProp->m_tkSet = tk;
            break;
        case 1:
            m_pCurProp->m_tkGet = tk;
            break;
        case 2:
            m_pCurProp->m_tklOthers.PUSH((mdToken*)(UINT_PTR)tk);
            break;
    }
}

/**************************************************************************/
void Assembler::EmitInstrStringLiteral(Instr* instr, BinStr* literal, BOOL ConvertToUnicode, BOOL Swap /*=FALSE*/)
{
    DWORD   DataLen = literal->length(),L;
    unsigned __int8 *pb = literal->ptr();
    HRESULT hr = S_OK;
    mdToken tk;
    WCHAR   *UnicodeString;
    if(DataLen == 0)
    {
        //report->warn("Zero length string emitted\n");
        ConvertToUnicode = FALSE;
    }
    if(ConvertToUnicode)
    {
        UnicodeString = (DataLen >= dwUniBuf) ? new WCHAR[DataLen+1] : &wzUniBuf[0];
        literal->appendInt8(0);
        pb = literal->ptr();
        // convert string to Unicode
        L = UnicodeString ? WszMultiByteToWideChar(g_uCodePage,0,(char*)pb,-1,UnicodeString,DataLen+1) : 0;
        if(L == 0)
        {
            const char* sz=NULL;
            DWORD dw;
            switch(dw=GetLastError())
            {
                case ERROR_INSUFFICIENT_BUFFER: sz = "ERROR_INSUFFICIENT_BUFFER"; break;
                case ERROR_INVALID_FLAGS:       sz = "ERROR_INVALID_FLAGS"; break;
                case ERROR_INVALID_PARAMETER:   sz = "ERROR_INVALID_PARAMETER"; break;
                case ERROR_NO_UNICODE_TRANSLATION: sz = "ERROR_NO_UNICODE_TRANSLATION"; break;
            }
            if(sz)  report->error("Failed to convert string '%s' to Unicode: %s\n",(char*)pb,sz);
            else    report->error("Failed to convert string '%s' to Unicode: error 0x%08X\n",(char*)pb,dw);
            goto OuttaHere;
        }
        L--;
    }
    else
    {
        if(DataLen & 1)
        {
            literal->appendInt8(0);
            pb = literal->ptr();
            DataLen++;
        }
        UnicodeString = (WCHAR*)pb;
        L = DataLen/sizeof(WCHAR);

#if BIGENDIAN
        if (Swap)
            SwapStringLength(UnicodeString, L);
#endif
    }
    // Add the string data to the metadata, which will fold dupes.
    hr = m_pEmitter->DefineUserString(
        UnicodeString,
        L,
        &tk
    );
    if (FAILED(hr))
    {
        report->error("Failed to add user string using DefineUserString, hr=0x%08x, data: '%S'\n",
               hr, UnicodeString);
    }
    else
    {
        EmitOpcode(instr);

        EmitBytes((BYTE *)&tk,sizeof(mdToken));
    }
OuttaHere:
    delete literal;
    if(((void*)UnicodeString != (void*)pb)&&(DataLen >= dwUniBuf)) delete [] UnicodeString;
    instr->opcode = -1; // in case we got here with error
}

/**************************************************************************/
void Assembler::EmitInstrSig(Instr* instr, BinStr* sig)
{
    mdSignature MetadataToken;
    DWORD       cSig = sig->length();
    COR_SIGNATURE* mySig = (COR_SIGNATURE *)(sig->ptr());

    if (FAILED(m_pEmitter->GetTokenFromSig(mySig, cSig, &MetadataToken)))
    {
        report->error("Unable to convert signature to metadata token.\n");
    }
    else
    {
        EmitOpcode(instr);
        EmitBytes((BYTE *)&MetadataToken, sizeof(mdSignature));
    }
    delete sig;
    instr->opcode = -1; // in case we got here with error
}

/**************************************************************************/
void Assembler::EmitInstrSwitch(Instr* instr, Labels* targets)
{
    Labels  *pLbls;
    int     NumLabels;
    Label   *pLabel;
    UINT    offset;

    EmitOpcode(instr);

    // count # labels
    for(pLbls = targets, NumLabels = 0; pLbls; pLbls = pLbls->Next, NumLabels++);

    EmitBytes((BYTE *)&NumLabels,sizeof(int));
    DWORD PC_nextInstr = m_CurPC + 4*NumLabels;
    for(pLbls = targets; pLbls; pLbls = pLbls->Next)
    {
        if(pLbls->isLabel)
        {
            if((pLabel = m_pCurMethod->FindLabel(pLbls->Label)))
            {
                offset = pLabel->m_PC - PC_nextInstr;
                if (m_fDisplayTraceOutput) report->msg("%d\n", offset);
            }
            else
            {
                // defer until we find the label
                AddDeferredFixup(pLbls->Label, m_pCurOutputPos, PC_nextInstr, 4 /* pcrelsize */ );
                offset = 0;
                pLbls->Label = NULL;
                if (m_fDisplayTraceOutput) report->msg("forward label %s\n", pLbls->Label);
            }
        }
        else
        {
            offset = (UINT)(UINT_PTR)pLbls->Label;
            if (m_fDisplayTraceOutput) report->msg("%d\n", offset);
        }
        EmitBytes((BYTE *)&offset, sizeof(UINT));
    }
    delete targets;
}

/**************************************************************************/
void Assembler::EmitLabel(_In_ __nullterminated char* label)
{
    _ASSERTE(m_pCurMethod);
    AddLabel(m_CurPC, label);
}
/**************************************************************************/
void Assembler::EmitDataLabel(_In_ __nullterminated char* label)
{
    AddGlobalLabel(label, m_pCurSection);
}

/**************************************************************************/
void Assembler::EmitBytes(BYTE *p, unsigned len)
{
    if(m_pCurOutputPos + len >= m_pEndOutputPos)
    {
        size_t buflen = m_pEndOutputPos - m_pOutputBuffer;
        size_t newlen = buflen+(len/OUTPUT_BUFFER_INCREMENT + 1)*OUTPUT_BUFFER_INCREMENT;
        BYTE *pb = new BYTE[newlen];
        if(pb == NULL)
        {
            report->error("Failed to extend output buffer from %d to %d bytes. Aborting\n",
                buflen, newlen);
            exit(1);
        }
        size_t delta = pb - m_pOutputBuffer;
        int i;
        Fixup* pSearch;
        GlobalFixup *pGSearch;
        for (i=0; (pSearch = m_pCurMethod->m_lstFixup.PEEK(i)); i++) pSearch->m_pBytes += delta;
        for (i=0; (pGSearch = m_lstGlobalFixup.PEEK(i)); i++) //need to move only those pointing to output buffer
        {
            if((pGSearch->m_pReference >= m_pOutputBuffer)&&(pGSearch->m_pReference <= m_pEndOutputPos))
                pGSearch->m_pReference += delta;
        }


        memcpy(pb,m_pOutputBuffer,m_CurPC);
        delete [] m_pOutputBuffer;
        m_pOutputBuffer = pb;
        m_pCurOutputPos = &m_pOutputBuffer[m_CurPC];
        m_pEndOutputPos = &m_pOutputBuffer[newlen];

    }

    switch (len)
    {
    case 1:
        *m_pCurOutputPos = *p;
        break;
    case 2:
        SET_UNALIGNED_VAL16(m_pCurOutputPos, GET_UNALIGNED_16(p));
        break;
    case 4:
        SET_UNALIGNED_VAL32(m_pCurOutputPos, GET_UNALIGNED_32(p));
        break;
    case 8:
        SET_UNALIGNED_VAL64(m_pCurOutputPos, GET_UNALIGNED_64(p));
        break;
    default:
        _ASSERTE(!"NYI");
        break;
    }

    m_pCurOutputPos += len;
    m_CurPC += len;
}
/**************************************************************************/
BinStr* Assembler::EncodeSecAttr(_In_ __nullterminated char* szReflName, BinStr* pbsSecAttrBlob, unsigned nProps)
{
    unsigned cnt;

    // build the blob As BinStr
    unsigned L = (unsigned) strlen(szReflName);
    BYTE* pb = NULL;
    BinStr* pbsRet = new BinStr();
    // encode the Reflection name length
    cnt = CorSigCompressData(L, pbsRet->getBuff(5));
    pbsRet->remove(5 - cnt);
    //put the name in
    if((pb = pbsRet->getBuff(L)) != NULL)
        memcpy(pb,szReflName,L);
    // find out the size of compressed nProps
    cnt = CorSigCompressData(nProps, pbsRet->getBuff(5));
    pbsRet->remove(5);
    // encode blob size
    unsigned nSize = cnt + pbsSecAttrBlob->length();
    cnt = CorSigCompressData(nSize, pbsRet->getBuff(5));
    pbsRet->remove(5 - cnt);
    // actually encode nProps
    cnt = CorSigCompressData(nProps, pbsRet->getBuff(5));
    pbsRet->remove(5 - cnt);
    // append the props/values blob
    pbsRet->append(pbsSecAttrBlob);
    delete pbsSecAttrBlob;
    return pbsRet;
}
/**************************************************************************/
void Assembler::EmitSecurityInfo(mdToken            token,
                                 PermissionDecl*    pPermissions,
                                 PermissionSetDecl* pPermissionSets)
{
    PermissionDecl     *pPerm, *pPermNext;
    PermissionSetDecl  *pPset, *pPsetNext;
    unsigned            uCount = 0;
    COR_SECATTR        *pAttrs;
    unsigned            i;
    unsigned            uLength;
    mdTypeRef           tkTypeRef;
    BinStr             *pSig;
    char               *szMemberName;
    DWORD               dwErrorIndex = 0;

    if (pPermissions) {

        for (pPerm = pPermissions; pPerm; pPerm = pPerm->m_Next)
            uCount++;

        _ASSERTE(uCount > 0);
        // uCount is expected to be positive all the time. The if statement is here to please prefast.
        if (uCount > 0)
        {
            if((pAttrs = new COR_SECATTR[uCount])==NULL)
            {
                report->error("\nOut of memory!\n");
                return;
            }

            mdToken tkMscorlib = m_fIsMscorlib ? 1 : GetAsmRef("mscorlib");
            tkTypeRef = ResolveClassRef(tkMscorlib,"System.Security.Permissions.SecurityAction", NULL);
            for (pPerm = pPermissions, i = 0; pPerm; pPerm = pPermNext, i++) {
                pPermNext = pPerm->m_Next;

                pSig = new BinStr();
                pSig->appendInt8(IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS);
                pSig->appendInt8(1);
                pSig->appendInt8(ELEMENT_TYPE_VOID);
                pSig->appendInt8(ELEMENT_TYPE_VALUETYPE);
                uLength = CorSigCompressToken(tkTypeRef, pSig->getBuff(5));
                pSig->remove(5 - uLength);

                uLength = (unsigned)strlen(COR_CTOR_METHOD_NAME) + 1;
                if((szMemberName = new char[uLength]))
                {
                    memcpy(szMemberName, COR_CTOR_METHOD_NAME, uLength);
                    pAttrs[i].tkCtor = MakeMemberRef(pPerm->m_TypeSpec, szMemberName, pSig);
                    pAttrs[i].pCustomAttribute = (const void *)pPerm->m_Blob;
                    pAttrs[i].cbCustomAttribute = pPerm->m_BlobLength;
                }
                else report->error("\nOut of memory!\n");
            }

            if (FAILED(m_pEmitter->DefineSecurityAttributeSet(token,
                                                       pAttrs,
                                                       uCount,
                                                       &dwErrorIndex)))
            {
                _ASSERT(uCount >= dwErrorIndex);
                if (dwErrorIndex == uCount)
                {
                    report->error("Failed to define security attribute set for 0x%08X\n", token);
                }
                else
                {
                    report->error("Failed to define security attribute set for 0x%08X\n  (error in permission %u)\n",
                                  token, uCount - dwErrorIndex);
                }
            }
            delete [] pAttrs;
            for (pPerm = pPermissions, i = 0; pPerm; pPerm = pPermNext, i++) {
                pPermNext = pPerm->m_Next;
                delete pPerm;
            }
        }
    }

    for (pPset = pPermissionSets; pPset; pPset = pPsetNext) {
        pPsetNext = pPset->m_Next;
        if(FAILED(m_pEmitter->DefinePermissionSet(token,
                                           pPset->m_Action,
                                           pPset->m_Value->ptr(),
                                           pPset->m_Value->length(),
                                           NULL)))
            report->error("Failed to define security permission set for 0x%08X\n", token);
        delete pPset;
    }
}

void Assembler::AddMethodImpl(mdToken tkImplementedTypeSpec, _In_ __nullterminated char* szImplementedName, BinStr* pImplementedSig,
                  mdToken tkImplementingTypeSpec, _In_opt_z_ char* szImplementingName, BinStr* pImplementingSig)
{
    if(m_pCurClass)
    {
        MethodImplDescriptor*   pMID = new MethodImplDescriptor;
        if(pMID == NULL)
        {
            report->error("Failed to allocate MethodImpl Descriptor\n");
            return;
        }
        pMID->m_fNew = TRUE;
        pMID->m_tkDefiningClass = m_pCurClass->m_cl;
        if(szImplementingName) //called from class scope, overriding method specified
        {
            pMID->m_tkImplementedMethod = MakeMemberRef(tkImplementedTypeSpec,szImplementedName,pImplementedSig);
            pMID->m_tkImplementingMethod = MakeMemberRef(tkImplementingTypeSpec,szImplementingName,pImplementingSig);
        }
        else    //called from method scope, use current method as overriding
        {
            if(m_pCurMethod)
            {
                if (pImplementedSig == NULL)
                {
                    pImplementedSig = new BinStr();
                    memcpy(pImplementedSig->getBuff(m_pCurMethod->m_dwMethodCSig), m_pCurMethod->m_pMethodSig,m_pCurMethod->m_dwMethodCSig);
                }
                pMID->m_tkImplementedMethod = MakeMemberRef(tkImplementedTypeSpec,szImplementedName,pImplementedSig);
                pMID->m_tkImplementingMethod = 0;

                m_pCurMethod->m_MethodImplDList.PUSH(pMID); // copy goes to method's own list (ptr only)
            }
            else
            {
                report->error("No overriding method specified");
                delete pMID;
                return;
            }
        }
        m_MethodImplDList.PUSH(pMID);
    }
    else
        report->error(".override directive outside class scope");
}
// source file name paraphernalia
void Assembler::SetSourceFileName(_In_ __nullterminated char* szName)
{
    if(szName)
    {
        if(*szName)
        {
            if(strcmp(m_szSourceFileName,szName))
            {
                strcpy_s(m_szSourceFileName,MAX_FILENAME_LENGTH*3+1,szName);
                WszMultiByteToWideChar(g_uCodePage,0,szName,-1,m_wzSourceFileName,MAX_FILENAME_LENGTH);
            }
            if(m_fGeneratePDB)
            {
                if (FAILED(m_pPortablePdbWriter->DefineDocument(szName, &m_guidLang)))
                {
                    report->error("Failed to define a document: '%s'", szName);
                }
                delete[] szName;
            }
            else delete [] szName;
        }
        else delete [] szName;
    }
}
void Assembler::SetSourceFileName(BinStr* pbsName)
{
    ULONG L;
    if(pbsName && (L = (ULONG)(pbsName->length())))
    {
        pbsName->appendInt8(0);
        char* sz = new char[L+1];
        memcpy(sz,pbsName->ptr(),L+1);
        SetSourceFileName(sz);
        delete pbsName;
    }
}

// Portable PDB paraphernalia
void Assembler::SetPdbFileName(_In_ __nullterminated char* szName)
{
    if (szName)
    {
        if (*szName)
        {
            strcpy_s(m_szPdbFileName, MAX_FILENAME_LENGTH * 3 + 1, szName);
            WszMultiByteToWideChar(g_uCodePage, 0, szName, -1, m_wzPdbFileName, MAX_FILENAME_LENGTH);
        }
    }
}
HRESULT Assembler::SavePdbFile()
{
    HRESULT hr = S_OK;
    mdMethodDef entryPoint;

    if (FAILED(hr = (m_pPortablePdbWriter == NULL ? E_FAIL : S_OK))) goto exit;
    if (FAILED(hr = (m_pPortablePdbWriter->GetEmitter() == NULL ? E_FAIL : S_OK))) goto exit;
    if (FAILED(hr = m_pCeeFileGen->GetEntryPoint(m_pCeeFile, &entryPoint))) goto exit;
    if (FAILED(hr = m_pPortablePdbWriter->BuildPdbStream(m_pEmitter, entryPoint))) goto exit;
    if (FAILED(hr = m_pPortablePdbWriter->GetEmitter()->Save(m_wzPdbFileName, NULL))) goto exit;

exit:
    return hr;
}

// This method is called after we have parsed the generic type parameters for either
// a generic class or a generic method.  It calls CheckAddGenericParamConstraint on
// each generic parameter constraint that was recorded.
//
void Assembler::RecordTypeConstraints(GenericParamConstraintList* pGPCList, int numTyPars, TyParDescr* tyPars)
{
    if (numTyPars > 0)
    {
        for (int i = 0; i < numTyPars; i++)
        {
            // Decode any type constraints held by tyPars[i].Bounds()
            BinStr* typeConstraints = tyPars[i].Bounds();
            _ASSERTE((typeConstraints->length() % 4) == 0);
            int numConstraints = (typeConstraints->length() / 4) - 1;
            if (numConstraints > 0)
            {
                mdToken* ptk = (mdToken*)typeConstraints->ptr();
                for (int j = 0; j < numConstraints; j++)
                {
                    mdToken tkTypeConstraint = ptk[j];

                    // pass false for isParamDirective, these constraints are from the class or method definition
                    //
                    CheckAddGenericParamConstraint(pGPCList, i, tkTypeConstraint, false);
                }
            }
        }
    }
}

// AddGenericParamConstraint is called when we have a .param constraint directive after a class definition
//
void Assembler::AddGenericParamConstraint(int index, char * pStrGenericParam, mdToken tkTypeConstraint)
{
    if (!m_pCurClass)
    {
        report->error(".parm constraint directive outside class scope");
        return;
    }
    if (index > 0)
    {
        if (pStrGenericParam != 0)
        {
            report->error("LOGIC ERROR - we have both an index and a pStrGenericParam");
            return;
        }
        if (index > (int)m_pCurClass->m_NumTyPars)
        {
            report->error("Type parameter index out of range\n");
            return;
        }
        index = index - 1;
    }
    else  // index was 0, so a name must be supplied by pStrGenericParam
    {
        if (pStrGenericParam == 0)
        {
            report->error("LOGIC ERROR - we have neither an index or a pStrGenericParam");
            return;
        }
        index = m_pCurClass->FindTyPar(pStrGenericParam);
        if (index == -1)
        {
            report->error("Type parameter '%s' undefined\n", pStrGenericParam);
            return;
        }
    }

    // pass true for isParamDirective, we are parsing a .param directive for a class here
    //
    CheckAddGenericParamConstraint(&m_pCurClass->m_GPCList, index, tkTypeConstraint, true);
}

// CheckAddGenericParamConstraint is called when we have to handle a generic parameter constraint
// When parsing a generic class/method definition isParamDirective is false - we have a generic type constaint
// for this case we do not setup m_pCustomDescrList as a .custom after a generic class/method definition is
// for the class/method
// When isParamDirective is true, we have a .param constraint directive and we will setup m_pCustomDescrList
// and any subsequent .custom is for the generic parameter constrant
//
void Assembler::CheckAddGenericParamConstraint(GenericParamConstraintList* pGPCList, int index, mdToken tkTypeConstraint, bool isParamDirective)
{
    _ASSERTE(tkTypeConstraint != 0);
    _ASSERTE(index >= 0);

    // Look for an existing match in m_pCurClass->m_GPCList
    //
    // Iterate the GenericParamConstraints list
    //
    bool match = false;
    GenericParamConstraintDescriptor *pGPC = nullptr;
    for (int listIndex = 0; (pGPC = pGPCList->PEEK(listIndex)) != nullptr; listIndex++)
    {
        int curParamIndex = pGPC->GetParamIndex();
        if (curParamIndex == index)
        {
            mdToken curTypeConstraint = pGPC->GetTypeConstraint();
            if (curTypeConstraint == tkTypeConstraint)
            {
                match = true;
                break;
            }
        }
    }

    if (match)
    {
        // Found an existing generic parameter constraint
        //
        if (isParamDirective)
        {
            // Setup the custom descr list so that we can record
            // custom attributes on this generic param constraint
            //
            m_pCustomDescrList = pGPC->CAList();
        }
    }
    else
    {
        // not found - add it to our pGPCList
        //
        GenericParamConstraintDescriptor* pNewGPCDescr = new GenericParamConstraintDescriptor();
        pNewGPCDescr->Init(index, tkTypeConstraint);
        pGPCList->PUSH(pNewGPCDescr);
        if (isParamDirective)
        {
            // Setup the custom descr list so that we can record
            // custom attributes on this generic param constraint
            //
            m_pCustomDescrList = pNewGPCDescr->CAList();
        }
    }
}

// Emit the proper metadata for the generic parameter type constraints
// This will create one GenericParamConstraint tokens for each type constraint.
// Finally associate any custom attributes with their GenericParamConstraint
// and emit them as well
//
void Assembler::EmitGenericParamConstraints(int numTyPars, TyParDescr* pTyPars, mdToken tkOwner, GenericParamConstraintList* pGPCL)
{
    // If we haver no generic parameters, or a null or empty list of generic param constraints
    // then we can early out and just return.
    //
    if ((numTyPars == 0) || (pGPCL == NULL) || (pGPCL->COUNT() == 0))
    {
        return;
    }

    int* nConstraintsArr = new int[numTyPars];
    int* nConstraintIndexArr = new int[numTyPars];
    mdToken** pConstraintsArr = new mdToken*[numTyPars];
    mdGenericParamConstraint** pGPConstraintsArr = new mdToken*[numTyPars];

    // Zero initialize the arrays that we just created
    int paramIndex;
    for (paramIndex = 0; paramIndex < numTyPars; paramIndex++)
    {
        nConstraintsArr[paramIndex] = 0;
        nConstraintIndexArr[paramIndex] = 0;
        pConstraintsArr[paramIndex] = nullptr;
        pGPConstraintsArr[paramIndex] = nullptr;
    }

    // Set all the owner tokens and calculate the number of constraints for each type parameter
    GenericParamConstraintDescriptor *pGPC;
    int listIndex;
    for (listIndex = 0; (pGPC = pGPCL->PEEK(listIndex)) != nullptr; listIndex++)
    {
        pGPC->SetOwner(tkOwner);
        paramIndex = pGPC->GetParamIndex();
        nConstraintsArr[paramIndex]++;
    }

    // Allocate an appropriately sized array of type constraints for each generic type param
    // If the generic type param has no type constraints we will just leave the value
    // of pConstraintsArr[paramIndex] (and pGPConstraintsArr[]) as nullptr
    for (paramIndex = 0; paramIndex < numTyPars; paramIndex++)
    {
        // How many constraints are there for this generic parameter?
        int currNumConstraints = nConstraintsArr[paramIndex];
        if (currNumConstraints > 0)
        {
            // We are required to have an extra mdTokenNil as the last element in the array
            int currConstraintArraySize = currNumConstraints + 1;

            mdToken* currConstraintsArr   = new mdToken[currConstraintArraySize];
            mdToken* currGPConstraintsArr = new mdToken[currConstraintArraySize];

            // initialize this new array to all mdTokenNil
            for (int i = 0; i < currConstraintArraySize; i++)
            {
                currConstraintsArr[i] = mdTokenNil;
                currGPConstraintsArr[i] = mdTokenNil;
            }

            // Assign these empty arrays to the proper elements of pConstraintsArr[] and pConstraintsArr[]
            pConstraintsArr[paramIndex] = currConstraintsArr;
            pGPConstraintsArr[paramIndex] = currGPConstraintsArr;
        }
    }

    // Iterate the GenericParamConstraints list again and
    // record the type constraints in the pConstraintsArr[][]
    for (listIndex = 0; (pGPC = pGPCL->PEEK(listIndex)) != nullptr; listIndex++)
    {
        paramIndex = pGPC->GetParamIndex();

        mdToken currTypeConstraint = pGPC->GetTypeConstraint();
        mdToken* currConstraintArr = pConstraintsArr[paramIndex];
        _ASSERTE(currConstraintArr != nullptr);
        int constraintArrayLast = nConstraintsArr[paramIndex];
        int currConstraintArrIndex = nConstraintIndexArr[paramIndex];
        _ASSERTE(currConstraintArrIndex < constraintArrayLast);

        currConstraintArr[currConstraintArrIndex++] = currTypeConstraint;
        _ASSERTE(currConstraintArr[currConstraintArrIndex] == mdTokenNil);   // the last element must be mdTokenNil
        nConstraintIndexArr[paramIndex] = currConstraintArrIndex;
    }

    // Next emit the metadata for the Generic parameter type constraints
    //
    for (paramIndex = 0; paramIndex < numTyPars; paramIndex++)
    {
        int currParamNumConstraints = nConstraintIndexArr[paramIndex];

        if (currParamNumConstraints > 0)
        {
            mdGenericParam tkGenericParam     = pTyPars[paramIndex].Token();
            DWORD          paramAttrs         = pTyPars[paramIndex].Attrs();
            ULONG          currNumConstraints = (ULONG) nConstraintsArr[paramIndex];
            mdToken*       currConstraintArr  = pConstraintsArr[paramIndex];
            mdGenericParamConstraint* currGPConstraintArr = pGPConstraintsArr[paramIndex];

            // call SetGenericParamProps for each generic parameter that has a non-zero count of constraints
            // to record each generic parameters tyupe constraints.
            //
            // Pass the paramAttrs, these contain values in CorGenericParamAttr such as:
            //    gpReferenceTypeConstraint        = 0x0004,  // type argument must be a reference type
            //    gpNotNullableValueTypeConstraint = 0x0008,  // type argument must be a value type but not Nullable
            //    gpDefaultConstructorConstraint   = 0x0010,  // type argument must have a public default constructor
            //
            // This Metadata operation will also create a new GenericParamConstraint token
            // for each of the generic parameters type constraints.
            //
            if (FAILED(m_pEmitter->SetGenericParamProps(tkGenericParam, paramAttrs, NULL, 0, currConstraintArr)))
            {
                report->error("Failed in SetGenericParamProp");
            }
            else
            {
                // We now need to fetch the values of the new GenericParamConstraint tokens
                // that were created by the call to SetGenericParamProps
                //
                // These tokens are the token owners for the custom attributes
                // such as NulableAttribute and TupleElementNamesAttribute
                //
                HCORENUM hEnum = NULL;
                ULONG uCount = 0;
                if (FAILED(m_pImporter->EnumGenericParamConstraints(&hEnum, tkGenericParam, currGPConstraintArr, (ULONG)currNumConstraints, &uCount)))
                {
                    report->error("Failed in EnumGenericParamConstraints");
                }
                else
                {
                    _ASSERTE(uCount == currNumConstraints);
                    m_pImporter->CloseEnum(hEnum);
                }
            }
        }
    }

    // Emit any Custom Attributes associated with these Generic Paaram Constraints
    //
    while ((pGPC = pGPCL->POP()))
    {
        paramIndex = pGPC->GetParamIndex();

        mdToken tkTypeConstraint = pGPC->GetTypeConstraint();
        int currNumConstraints = nConstraintsArr[paramIndex];
        mdToken* currConstraintArr = pConstraintsArr[paramIndex];
        mdGenericParamConstraint* currGPConstraintArr = pGPConstraintsArr[paramIndex];
        mdGenericParamConstraint  tkOwnerOfCA = mdTokenNil;

        // find the matching type constraint and fetch the GenericParamConstraint
        //
        for (int i = 0; i < currNumConstraints; i++)
        {
            if (currConstraintArr[i] == tkTypeConstraint)
            {
                tkOwnerOfCA = currGPConstraintArr[i];
                break;
            }
        }
        _ASSERTE(tkOwnerOfCA != mdTokenNil);

        // Record the Generic Param Constraint token
        // and supply it as the owner for the list of Custom attributes.
        //
        pGPC->Token(tkOwnerOfCA);
        EmitCustomAttributes(tkOwnerOfCA, pGPC->CAList());
    }
}
