// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ---------------------------------------------------------------------------
// typeparse.cpp
// ---------------------------------------------------------------------------
//

//


#include "common.h"
#include "class.h"
#include "typehandle.h"
#include "sstring.h"
#include "typeparse.h"
#include "typestring.h"
#include "assemblynative.hpp"
#include "fstring.h"


//
// TypeName
//
SString* TypeName::ToString(SString* pBuf, BOOL bAssemblySpec, BOOL bSignature, BOOL bGenericArguments)
{
    WRAPPER_NO_CONTRACT;

    PRECONDITION(!bGenericArguments & !bSignature &! bAssemblySpec);

    TypeNameBuilder tnb(pBuf);

    for (COUNT_T i = 0; i < m_names.GetCount(); i ++)
        tnb.AddName(m_names[i]->GetUnicode());

    return pBuf;
}


DWORD TypeName::AddRef()
{
    LIMITED_METHOD_CONTRACT;

    m_count++;

    return m_count;
}

DWORD TypeName::Release()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    m_count--;

    DWORD dwCount = m_count;
    if (dwCount == 0)
        delete this;

    return dwCount;
}

TypeName::~TypeName()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    for(COUNT_T i = 0; i < m_genericArguments.GetCount(); i ++)
        m_genericArguments[i]->Release();
}

#if!defined(CROSSGEN_COMPILE)
SAFEHANDLE TypeName::GetSafeHandle()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    SAFEHANDLE objSafeHandle = NULL;

    GCPROTECT_BEGIN(objSafeHandle);

    objSafeHandle = (SAFEHANDLE)AllocateObject(MscorlibBinder::GetClass(CLASS__SAFE_TYPENAMEPARSER_HANDLE));
    CallDefaultConstructor(objSafeHandle);

    this->AddRef();
    objSafeHandle->SetHandle(this);

    GCPROTECT_END();

    return objSafeHandle;
}

/*static*/
void QCALLTYPE TypeName::QCreateTypeNameParser(LPCWSTR wszTypeName, QCall::ObjectHandleOnStack pHandle, BOOL throwOnError)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    DWORD error = (DWORD)-1;
    ReleaseHolder<TypeName> pTypeName = new TypeName(wszTypeName, &error);
    pTypeName->AddRef();

    if (error == (DWORD)-1)
    {
        GCX_COOP();
        pHandle.Set(pTypeName->GetSafeHandle());
    }
    else
    {
        if (throwOnError)
        {
            StackSString buf;
            StackSString msg(W("typeName@"));
            COUNT_T size = buf.GetUnicodeAllocation();
            _itow_s(error, buf.OpenUnicodeBuffer(size), size, /*radix*/10);
            buf.CloseBuffer();
            msg.Append(buf);
            COMPlusThrowArgumentException(msg.GetUnicode(), NULL);
        }
    }

    END_QCALL;
}

/*static*/
void QCALLTYPE TypeName::QReleaseTypeNameParser(TypeName * pTypeName)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeName));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    pTypeName->Release();

    END_QCALL;
}

/*static*/
void QCALLTYPE TypeName::QGetNames(TypeName * pTypeName, QCall::ObjectHandleOnStack pNames)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeName));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    SArray<SString*> names = pTypeName->GetNames();
    COUNT_T count = names.GetCount();

    GCX_COOP();

    if (count > 0)
    {
        PTRARRAYREF pReturnNames = NULL;

        GCPROTECT_BEGIN(pReturnNames);

        pReturnNames = (PTRARRAYREF)AllocateObjectArray(count, g_pStringClass);

        for (COUNT_T i = 0; i < count; i++)
        {
            STRINGREF str = StringObject::NewString(names[i]->GetUnicode());
            pReturnNames->SetAt(i, str);
        }

        pNames.Set(pReturnNames);

        GCPROTECT_END();
    }
    else
    {
        pNames.Set(NULL);
    }

    END_QCALL;
}

/*static*/
void QCALLTYPE TypeName::QGetTypeArguments(TypeName * pTypeName, QCall::ObjectHandleOnStack pTypeArguments)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeName));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    SArray<TypeName*> arguments = pTypeName->GetGenericArguments();
    COUNT_T count = arguments.GetCount();

    GCX_COOP();

    if (count > 0)
    {
        PTRARRAYREF pReturnArguments = NULL;

        GCPROTECT_BEGIN(pReturnArguments);

        pReturnArguments = (PTRARRAYREF)AllocateObjectArray(count, MscorlibBinder::GetClass(CLASS__SAFE_TYPENAMEPARSER_HANDLE));

        for (COUNT_T i = 0; i < count; i++)
        {
            SAFEHANDLE handle = arguments[i]->GetSafeHandle();
            _ASSERTE(handle != NULL);

            pReturnArguments->SetAt(i, handle);
        }

        pTypeArguments.Set(pReturnArguments);

        GCPROTECT_END();
    }
    else
    {
        pTypeArguments.Set(NULL);
    }

    END_QCALL;
}

/*static*/
void QCALLTYPE TypeName::QGetModifiers(TypeName * pTypeName, QCall::ObjectHandleOnStack pModifiers)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeName));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    SArray<DWORD> modifiers = pTypeName->GetSignature();
    COUNT_T count = modifiers.GetCount();

    GCX_COOP();

    if (count > 0)
    {
        I4ARRAYREF pReturnModifiers = NULL;

        GCPROTECT_BEGIN(pReturnModifiers);

        //TODO: how do we Get
        pReturnModifiers = (I4ARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_I4, count);
        INT32 *pToArray = pReturnModifiers->GetDirectPointerToNonObjectElements();

        for (COUNT_T i = 0; i < count; i++)
        {
            pToArray[i] = modifiers[i];
        }

        pModifiers.Set(pReturnModifiers);

        GCPROTECT_END();
    }
    else
    {
        pModifiers.Set(NULL);
    }

    END_QCALL;
}

/*static*/
void QCALLTYPE TypeName::QGetAssemblyName(TypeName * pTypeName, QCall::StringHandleOnStack pAssemblyName)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeName));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    pAssemblyName.Set(*(pTypeName->GetAssembly()));

    END_QCALL;
}
#endif//!CROSSGEN_COMPILE

//
// TypeName::TypeNameParser
//
#undef IfFailGo
#define IfFailGo(P) if (!P) return FALSE;

TypeName* TypeName::AddGenericArgument()
{
    WRAPPER_NO_CONTRACT;

    TypeName* pGenArg = new TypeName();
    pGenArg->AddRef();

    pGenArg->m_bIsGenericArgument = TRUE;
    return m_genericArguments.AppendEx(pGenArg);
}

TypeName::TypeNameParser::TypeNameTokens TypeName::TypeNameParser::LexAToken(BOOL ignorePlus)
{
    LIMITED_METHOD_CONTRACT;

    if (m_nextToken == TypeNameIdentifier)
        return TypeNamePostIdentifier;

    if (m_nextToken == TypeNameEnd)
        return TypeNameEnd;

    if (*m_itr == W('\0'))
        return TypeNameEnd;

    if (COMCharacter::nativeIsWhiteSpace(*m_itr))
    {
        m_itr++;
        return LexAToken();
    }

    WCHAR c = *m_itr;
    m_itr++;
    switch(c)
    {
        case W(','): return TypeNameComma;
        case W('['): return TypeNameOpenSqBracket;
        case W(']'): return TypeNameCloseSqBracket;
        case W('&'): return TypeNameAmpersand;
        case W('*'): return TypeNameAstrix;
        case W('+'): if (!ignorePlus) return TypeNamePlus;
        case W('\\'):
            m_itr--;
            return TypeNameIdentifier;
    }

    ASSERT(!IsTypeNameReservedChar(c));

    m_itr--;
    return TypeNameIdentifier;
}

BOOL TypeName::TypeNameParser::GetIdentifier(SString* sszId, TypeName::TypeNameParser::TypeNameIdentifiers identifierType)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PRECONDITION(m_currentToken == TypeNameIdentifier && m_nextToken == TypeNamePostIdentifier);

    sszId->Clear();

    LPCWSTR start = m_currentItr;
    InlineSArray<LPCWSTR, 32> m_escape;

    if (identifierType == TypeNameId)
    {
        do
        {
            switch (* m_currentItr ++)
            {
                case W(','):
                case W('['):
                case W(']'):
                case W('&'):
                case W('*'):
                case W('+'):
                case W('\0'):
                    goto done;

                case W('\\'):
                    m_escape.Append(m_currentItr - 1);

                    if (! IsTypeNameReservedChar(*m_currentItr) || *m_currentItr == '\0')
                        return FALSE;

                    m_currentItr++;
                    break;

                default:
                    break;
            }
        }
        while(true);

done:
        m_currentItr--;
    }
    else if (identifierType == TypeNameFusionName)
    {
        while(*m_currentItr != W('\0'))
            m_currentItr++;
    }
    else if (identifierType == TypeNameEmbeddedFusionName)
    {
        for (; (*m_currentItr != W('\0')) && (*m_currentItr != W(']')); m_currentItr++)
        {
            if (*m_currentItr == W('\\'))
            {
                if (*(m_currentItr + 1) == W(']'))
                {
                    m_escape.Append(m_currentItr);
                    m_currentItr ++;
                    continue;
                }
            }

            if (*m_currentItr == '\0')
                return FALSE;
        }
        if (*m_currentItr == W('\0'))
        {
            return FALSE;
        }
    }
    else
        return FALSE;

    sszId->Set(start, (COUNT_T)(m_currentItr - start));

    for (SCOUNT_T i = m_escape.GetCount() - 1; i >= 0; i--)
        sszId->Delete(sszId->Begin() + (SCOUNT_T)(m_escape[i] - start), 1);

    m_itr = m_currentItr;
    m_nextToken = LexAToken();
    return TRUE;
}

BOOL TypeName::TypeNameParser::START()
{
    WRAPPER_NO_CONTRACT;

    NextToken();
    NextToken();
    return AQN();
}

// FULLNAME ',' ASSEMSPEC
// FULLNAME
// /* empty */
BOOL TypeName::TypeNameParser::AQN()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    IfFailGo(TokenIs(TypeNameAQN));

    if (TokenIs(TypeNameEnd))
        return TRUE;

    IfFailGo(FULLNAME());

    if (TokenIs(TypeNameComma))
    {
        NextToken();
        IfFailGo(ASSEMSPEC());
    }

    IfFailGo(TokenIs(TypeNameEnd));

    return TRUE;
}

// fusionName
BOOL TypeName::TypeNameParser::ASSEMSPEC()
{
    WRAPPER_NO_CONTRACT;
    IfFailGo(TokenIs(TypeNameASSEMSPEC));

    GetIdentifier(m_pTypeName->GetAssembly(), TypeNameFusionName);

    NextToken();

    return TRUE;
}

// NAME GENPARAMS QUALIFIER
BOOL TypeName::TypeNameParser::FULLNAME()
{
    WRAPPER_NO_CONTRACT;
    IfFailGo(TokenIs(TypeNameFULLNAME));
    IfFailGo(NAME());

    IfFailGo(GENPARAMS());

    IfFailGo(QUALIFIER());

    return TRUE;
}

// *empty*
// '[' GENARGS ']'
BOOL TypeName::TypeNameParser::GENPARAMS()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!TokenIs(TypeNameGENPARAM))
        return TRUE;

    if (!NextTokenIs(TypeNameGENARGS))
        return TRUE;

    NextToken();
    IfFailGo(GENARGS());

    IfFailGo(TokenIs(TypeNameCloseSqBracket));
    NextToken();

    return TRUE;
}

// GENARG
// GENARG ',' GENARGS
BOOL TypeName::TypeNameParser::GENARGS()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    IfFailGo(TokenIs(TypeNameGENARGS));

    IfFailGo(GENARG());

    if (TokenIs(TypeNameComma))
    {
        NextToken();
        IfFailGo(GENARGS());
    }

    return TRUE;
}

// '[' EAQN ']'
// FULLNAME
BOOL TypeName::TypeNameParser::GENARG()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    IfFailGo(TokenIs(TypeNameGENARG));

    TypeName* pEnclosingTypeName = m_pTypeName;
    m_pTypeName = m_pTypeName->AddGenericArgument();
    {
        if (TokenIs(TypeNameOpenSqBracket))
        {
            NextToken();
            IfFailGo(EAQN());

            IfFailGo(TokenIs(TypeNameCloseSqBracket));
            NextToken();
        }
        else
        {
            IfFailGo(FULLNAME());
        }
    }
    m_pTypeName = pEnclosingTypeName;

    return TRUE;
}

// FULLNAME ',' EASSEMSPEC
// FULLNAME
BOOL TypeName::TypeNameParser::EAQN()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    IfFailGo(TokenIs(TypeNameEAQN));

    IfFailGo(FULLNAME());

    if (TokenIs(TypeNameComma))
    {
        NextToken();
        IfFailGo(EASSEMSPEC());
    }

    return TRUE;
}

// embeddedFusionName
BOOL TypeName::TypeNameParser::EASSEMSPEC()
{
    WRAPPER_NO_CONTRACT;
    IfFailGo(TokenIs(TypeNameEASSEMSPEC));

    GetIdentifier(m_pTypeName->GetAssembly(), TypeNameEmbeddedFusionName);

    NextToken();

    return TRUE;
}

// *empty*
// '&'
// '*' QUALIFIER
// ARRAY QUALIFIER
BOOL TypeName::TypeNameParser::QUALIFIER()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!TokenIs(TypeNameQUALIFIER))
        return TRUE;

    if (TokenIs(TypeNameAmpersand))
    {
        m_pTypeName->SetByRef();

        NextToken();
    }
    else if (TokenIs(TypeNameAstrix))
    {
        m_pTypeName->SetPointer();

        NextToken();
        IfFailGo(QUALIFIER());
    }
    else
    {
        IfFailGo(ARRAY());
        IfFailGo(QUALIFIER());
    }

    return TRUE;
}

// '[' RANK ']'
// '[' '*' ']'
BOOL TypeName::TypeNameParser::ARRAY()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    IfFailGo(TokenIs(TypeNameARRAY));

    NextToken();

    if (TokenIs(TypeNameAstrix))
    {
        m_pTypeName->SetArray(1);

        NextToken();
    }
    else
    {
        DWORD dwRank = 1;
        IfFailGo(RANK(&dwRank));

        if (dwRank == 1)
            m_pTypeName->SetSzArray();
        else
            m_pTypeName->SetArray(dwRank);
    }

    IfFailGo(TokenIs(TypeNameCloseSqBracket));
    NextToken();

    return TRUE;
}

// *empty*
// ',' RANK
BOOL TypeName::TypeNameParser::RANK(DWORD* pdwRank)
{
    WRAPPER_NO_CONTRACT;

    if (!TokenIs(TypeNameRANK))
        return TRUE;

    NextToken();
    *pdwRank = *pdwRank + 1;
    IfFailGo(RANK(pdwRank));

    return TRUE;
}

// id
// id '+' NESTNAME
BOOL TypeName::TypeNameParser::NAME()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    IfFailGo(TokenIs(TypeNameNAME));

    GetIdentifier(m_pTypeName->AddName(), TypeNameId);

    NextToken();

    if (TokenIs(TypeNamePlus))
    {
        NextToken();
        IfFailGo(NESTNAME());
    }

    return TRUE;
}

// id
// id '+' NESTNAME
BOOL TypeName::TypeNameParser::NESTNAME()
{
    WRAPPER_NO_CONTRACT;
    IfFailGo(TokenIs(TypeNameNESTNAME));

    GetIdentifier(m_pTypeName->AddName(), TypeNameId);

    NextToken();
    if (TokenIs(TypeNamePlus))
    {
        NextToken();
        IfFailGo(NESTNAME());
    }

    return TRUE;
}

//--------------------------------------------------------------------------------------------------------------
// This version is used for resolving types named in custom attributes such as those used
// for interop. Thus, it follows a well-known multistage set of rules for determining which
// assembly the type is in. It will also enforce that the requesting assembly has access
// rights to the type being loaded.
//
// The search logic is:
//
//    if szTypeName is ASM-qualified, only that assembly will be searched.
//    if szTypeName is not ASM-qualified, we will search for the types in the following order:
//       - in pRequestingAssembly (if not NULL). pRequestingAssembly is the assembly that contained
//         the custom attribute from which the typename was derived.
//       - in mscorlib.dll
//       - raise an AssemblyResolveEvent() in the current appdomain
//
// pRequestingAssembly may be NULL. In that case, the "visibility" check will simply check that
// the loaded type has public access.
//--------------------------------------------------------------------------------------------------------------
/* public static */
TypeHandle TypeName::GetTypeUsingCASearchRules(LPCUTF8 szTypeName, Assembly *pRequestingAssembly, BOOL *pfNameIsAsmQualified/* = NULL*/, BOOL bDoVisibilityChecks/* = TRUE*/)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    StackSString sszAssemblyQualifiedName(SString::Utf8, szTypeName);
    return GetTypeUsingCASearchRules(sszAssemblyQualifiedName.GetUnicode(), pRequestingAssembly, pfNameIsAsmQualified, bDoVisibilityChecks);
}

TypeHandle TypeName::GetTypeUsingCASearchRules(LPCWSTR szTypeName, Assembly *pRequestingAssembly, BOOL *pfNameIsAsmQualified/* = NULL*/, BOOL bDoVisibilityChecks/* = TRUE*/)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    DWORD error = (DWORD)-1;

    GCX_COOP();
    OBJECTREF keepAlive = NULL;
    TypeHandle th = TypeHandle();

    GCPROTECT_BEGIN(keepAlive);

#ifdef __GNUC__
    // When compiling under GCC we have to use the -fstack-check option to ensure we always spot stack
    // overflow. But this option is intolerant of locals growing too large, so we have to cut back a bit
    // on what we can allocate inline here. Leave the Windows versions alone to retain the perf benefits
    // since we don't have the same constraints.
    NewHolder<TypeName> pTypeName = new TypeName(szTypeName, &error);
#else // __GNUC__
    TypeName typeName(szTypeName, &error);
    TypeName *pTypeName = &typeName;
#endif // __GNUC__

    if (error != (DWORD)-1)
    {
        StackSString buf;
        StackSString msg(W("typeName@"));
        COUNT_T size = buf.GetUnicodeAllocation();
        _itow_s(error,buf.OpenUnicodeBuffer(size),size,10);
        buf.CloseBuffer();
        msg.Append(buf);
        COMPlusThrowArgumentException(msg.GetUnicode(), NULL);
    }

    if (pfNameIsAsmQualified)
    {
        *pfNameIsAsmQualified = TRUE;
        if (pTypeName->GetAssembly()->IsEmpty())
            *pfNameIsAsmQualified = FALSE;
    }

    th = pTypeName->GetTypeWorker(
        /*bThrowIfNotFound = */ TRUE,
        /*bIgnoreCase = */ FALSE,
        /*pAssemblyGetType =*/ NULL,
        /*fEnableCASearchRules = */ TRUE,
        /*fProhibitAsmQualifiedName = */ FALSE,
        pRequestingAssembly,
        nullptr,
        FALSE,
        &keepAlive);

    ASSERT(!th.IsNull());
    LoaderAllocator *pLoaderAllocator = th.GetLoaderAllocator();

    if (pLoaderAllocator->IsCollectible())
    {
        if ((pRequestingAssembly == NULL) || !pRequestingAssembly->GetLoaderAllocator()->IsCollectible())
        {
            COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleBoundNonCollectible"));
        }
        else
        {
            pRequestingAssembly->GetLoaderAllocator()->EnsureReference(pLoaderAllocator);
        }
    }

    GCPROTECT_END();
    return th;
}






//--------------------------------------------------------------------------------------------------------------
// This everything-but-the-kitchen-sink version is what used to be called "GetType()". It exposes all the
// funky knobs needed for implementing the specific requirements of the managed Type.GetType() apis and friends.
//--------------------------------------------------------------------------------------------------------------
/*public static */ TypeHandle TypeName::GetTypeManaged(
    LPCWSTR szTypeName,
    DomainAssembly* pAssemblyGetType,
    BOOL bThrowIfNotFound,
    BOOL bIgnoreCase,
    BOOL bProhibitAsmQualifiedName,
    Assembly* pRequestingAssembly,
    BOOL bLoadTypeFromPartialNameHack,
    OBJECTREF *pKeepAlive,
    ICLRPrivBinder * pPrivHostBinder)
{
    STANDARD_VM_CONTRACT;

    if (!*szTypeName)
      COMPlusThrow(kArgumentException, W("Format_StringZeroLength"));

    DWORD error = (DWORD)-1;

    /* Partial name workaround loading must not load a collectible type */
    if (bLoadTypeFromPartialNameHack)
        pKeepAlive = NULL;

#ifdef __GNUC__
    // When compiling under GCC we have to use the -fstack-check option to ensure we always spot stack
    // overflow. But this option is intolerant of locals growing too large, so we have to cut back a bit
    // on what we can allocate inline here. Leave the Windows versions alone to retain the perf benefits
    // since we don't have the same constraints.
    NewHolder<TypeName> pTypeName = new TypeName(szTypeName, &error);
#else // __GNUC__
    TypeName typeName(szTypeName, &error);
    TypeName *pTypeName = &typeName;
#endif // __GNUC__

    if (error != (DWORD)-1)
    {
        if (!bThrowIfNotFound)
            return TypeHandle();

        StackSString buf;
        StackSString msg(W("typeName@"));
        COUNT_T size = buf.GetUnicodeAllocation();
        _itow_s(error, buf.OpenUnicodeBuffer(size), size, /*radix*/10);
        buf.CloseBuffer();
        msg.Append(buf);
        COMPlusThrowArgumentException(msg.GetUnicode(), NULL);
    }

    BOOL bPeriodPrefix = szTypeName[0] == W('.');

    TypeHandle result = pTypeName->GetTypeWorker(
        bPeriodPrefix ? FALSE : bThrowIfNotFound,
        bIgnoreCase,
        pAssemblyGetType ? pAssemblyGetType->GetAssembly() : NULL,
        /*fEnableCASearchRules = */TRUE,
        bProhibitAsmQualifiedName,
        pRequestingAssembly,
        pPrivHostBinder,
        bLoadTypeFromPartialNameHack,
        pKeepAlive);

    if (bPeriodPrefix && result.IsNull())
    {
        new (pTypeName) TypeName(szTypeName + 1, &error);

        if (error != (DWORD)-1)
        {
            if (!bThrowIfNotFound)
                return TypeHandle();

            StackSString buf;
            StackSString msg(W("typeName@"));
            COUNT_T size = buf.GetUnicodeAllocation();
            _itow_s(error-1,buf.OpenUnicodeBuffer(size),size,10);
            buf.CloseBuffer();
            msg.Append(buf);
            COMPlusThrowArgumentException(msg.GetUnicode(), NULL);
        }

        result = pTypeName->GetTypeWorker(
            bThrowIfNotFound,
            bIgnoreCase,
            pAssemblyGetType ? pAssemblyGetType->GetAssembly() : NULL,
            /*fEnableCASearchRules = */TRUE,
            bProhibitAsmQualifiedName,
            pRequestingAssembly,
            pPrivHostBinder,
            bLoadTypeFromPartialNameHack,
            pKeepAlive);
    }

    return result;
}




//-------------------------------------------------------------------------------------------
// Retrieves a type from an assembly. It requires the caller to know which assembly
// the type is in.
//-------------------------------------------------------------------------------------------
/* public static */ TypeHandle TypeName::GetTypeFromAssembly(LPCWSTR szTypeName, Assembly *pAssembly, BOOL bThrowIfNotFound /*= TRUE*/)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    _ASSERTE(szTypeName != NULL);
    _ASSERTE(pAssembly != NULL);

    if (!*szTypeName)
      COMPlusThrow(kArgumentException, W("Format_StringZeroLength"));

    DWORD error = (DWORD)-1;

#ifdef __GNUC__
    // When compiling under GCC we have to use the -fstack-check option to ensure we always spot stack
    // overflow. But this option is intolerant of locals growing too large, so we have to cut back a bit
    // on what we can allocate inline here. Leave the Windows versions alone to retain the perf benefits
    // since we don't have the same constraints.
    NewHolder<TypeName> pTypeName = new TypeName(szTypeName, &error);
#else // __GNUC__
    TypeName typeName(szTypeName, &error);
    TypeName *pTypeName = &typeName;
#endif // __GNUC__

    if (error != (DWORD)-1)
    {
        StackSString buf;
        StackSString msg(W("typeName@"));
        COUNT_T size = buf.GetUnicodeAllocation();
        _itow_s(error,buf.OpenUnicodeBuffer(size),size,10);
        buf.CloseBuffer();
        msg.Append(buf);
        COMPlusThrowArgumentException(msg.GetUnicode(), NULL);
    }

    // Because the typename can come from untrusted input, we will throw an exception rather than assert.
    // (This also assures that the shipping build does the right thing.)
    if (!(pTypeName->GetAssembly()->IsEmpty()))
    {
        COMPlusThrow(kArgumentException, IDS_EE_CANNOT_HAVE_ASSEMBLY_SPEC);
    }

    return pTypeName->GetTypeWorker(bThrowIfNotFound, /*bIgnoreCase = */FALSE, pAssembly, /*fEnableCASearchRules = */FALSE, FALSE, NULL,
        nullptr, // pPrivHostBinder
        FALSE, NULL /* cannot find a collectible type unless it is in assembly */);
}

//-------------------------------------------------------------------------------------------
// Retrieves a type. Will assert if the name is not fully qualified.
//-------------------------------------------------------------------------------------------
/* public static */ TypeHandle TypeName::GetTypeFromAsmQualifiedName(LPCWSTR szFullyQualifiedName)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    _ASSERTE(szFullyQualifiedName != NULL);

    if (!*szFullyQualifiedName)
      COMPlusThrow(kArgumentException, W("Format_StringZeroLength"));

    DWORD error = (DWORD)-1;

#ifdef __GNUC__
    // When compiling under GCC we have to use the -fstack-check option to ensure we always spot stack
    // overflow. But this option is intolerant of locals growing too large, so we have to cut back a bit
    // on what we can allocate inline here. Leave the Windows versions alone to retain the perf benefits
    // since we don't have the same constraints.
    NewHolder<TypeName> pTypeName = new TypeName(szFullyQualifiedName, &error);
#else // __GNUC__
    TypeName typeName(szFullyQualifiedName, &error);
    TypeName *pTypeName = &typeName;
#endif // __GNUC__

    if (error != (DWORD)-1)
    {
        StackSString buf;
        StackSString msg(W("typeName@"));
        COUNT_T size = buf.GetUnicodeAllocation();
        _itow_s(error,buf.OpenUnicodeBuffer(size),size,10);
        buf.CloseBuffer();
        msg.Append(buf);
        COMPlusThrowArgumentException(msg.GetUnicode(), NULL);
    }

    return pTypeName->GetTypeFromAsm();
}


TypeHandle TypeName::GetTypeFromAsm()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    // Because the typename can come from untrusted input, we will throw an exception rather than assert.
    // (This also assures that the shipping build does the right thing.)
    if (this->GetAssembly()->IsEmpty())
    {
        COMPlusThrow(kArgumentException, IDS_EE_NEEDS_ASSEMBLY_SPEC);
    }

    return this->GetTypeWorker(
        /*bThrowIfNotFound =*/TRUE,
        /*bIgnoreCase = */FALSE,
        NULL,
        /*fEnableCASearchRules = */FALSE,
        FALSE,
        NULL,
        nullptr, // pPrivHostBinder
        FALSE,
        NULL /* cannot find a collectible type */);
}



// -------------------------------------------------------------------------------------------------------------
// This is the "uber" GetType() that all public GetType() funnels through. It's main job is to figure out which
// Assembly to load the type from and then invoke GetTypeHaveAssembly.
//
// It's got a highly baroque interface partly for historical reasons and partly because it's the uber-function
// for all of the possible GetTypes.
// -------------------------------------------------------------------------------------------------------------
/* private instance */ TypeHandle TypeName::GetTypeWorker(
    BOOL bThrowIfNotFound,
    BOOL bIgnoreCase,
    Assembly* pAssemblyGetType,

    BOOL fEnableCASearchRules,
    BOOL bProhibitAsmQualifiedName,
    Assembly* pRequestingAssembly,
    ICLRPrivBinder * pPrivHostBinder,
    BOOL bLoadTypeFromPartialNameHack,
    OBJECTREF *pKeepAlive)
{
    CONTRACT(TypeHandle)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(!(RETVAL.IsNull() && bThrowIfNotFound));
    }
    CONTRACT_END

    GCX_COOP();

    ASSEMBLYREF asmRef = NULL;
    TypeHandle th = TypeHandle();
    GCPROTECT_BEGIN(asmRef);

    // We don't ever want to get anything related to a collectible type in here if we are not able to return one.
    ASSEMBLYREF *pAsmRef = &asmRef;
    if (pKeepAlive == NULL)
        pAsmRef = NULL;

    // An explicit assembly has been specified so look for the type there
    if (!GetAssembly()->IsEmpty())
    {

        if (bProhibitAsmQualifiedName && !m_bIsGenericArgument)
        {
            if (bThrowIfNotFound)
            {
                COMPlusThrow(kArgumentException, IDS_EE_ASSEMBLY_GETTYPE_CANNONT_HAVE_ASSEMBLY_SPEC);
            }
            else
            {
                th = TypeHandle();
                goto Exit;
            }
        }

        SString * pssOuterTypeName = NULL;
        if (GetNames().GetCount() > 0)
        {
            pssOuterTypeName = GetNames()[0];
        }

        // We want to catch the exception if we're going to later try a partial bind.
        if (bLoadTypeFromPartialNameHack)
        {
            EX_TRY
            {
                DomainAssembly *pDomainAssembly = LoadDomainAssembly(GetAssembly(), pRequestingAssembly,
                                                                     pPrivHostBinder,
                                                                     bThrowIfNotFound, pssOuterTypeName);
                if (pDomainAssembly)
                {
                    th = GetTypeHaveAssembly(pDomainAssembly->GetAssembly(), bThrowIfNotFound, bIgnoreCase, pKeepAlive);
                }
            }
            EX_CATCH
            {
                th = TypeHandle();
            }
            EX_END_CATCH(RethrowTransientExceptions);
        }
        else
        {
            DomainAssembly *pDomainAssembly = LoadDomainAssembly(GetAssembly(), pRequestingAssembly,
                                                                 pPrivHostBinder,
                                                                 bThrowIfNotFound, pssOuterTypeName);
            if (pDomainAssembly)
            {
                th = GetTypeHaveAssembly(pDomainAssembly->GetAssembly(), bThrowIfNotFound, bIgnoreCase, pKeepAlive);
            }
        }
    }

    // There's no explicit assembly so look in the assembly specified by the original caller (Assembly.GetType)
    else if (pAssemblyGetType)
    {
        th = GetTypeHaveAssembly(pAssemblyGetType, bThrowIfNotFound, bIgnoreCase, pKeepAlive);
    }

    // Otherwise look in the caller's assembly then the system assembly
    else if (fEnableCASearchRules)
    {
        // Look for type in caller's assembly
        if (pRequestingAssembly)
            th = GetTypeHaveAssembly(pRequestingAssembly, bThrowIfNotFound, bIgnoreCase, pKeepAlive);

        // Look for type in system assembly
        if (th.IsNull())
        {
            if (pRequestingAssembly != SystemDomain::SystemAssembly())
                th = GetTypeHaveAssembly(SystemDomain::SystemAssembly(), bThrowIfNotFound, bIgnoreCase, pKeepAlive);
        }

        // Raise AssemblyResolveEvent to try to resolve assembly
        if (th.IsNull())
        {
            AppDomain *pDomain = (AppDomain *)SystemDomain::GetCurrentDomain();

            if ((BaseDomain*)pDomain != SystemDomain::System())
            {
                TypeNameBuilder tnb;
                for (COUNT_T i = 0; i < GetNames().GetCount(); i ++)
                    tnb.AddName(GetNames()[i]->GetUnicode());

                StackScratchBuffer bufFullName;
                DomainAssembly* pDomainAssembly = pDomain->RaiseTypeResolveEventThrowing(pRequestingAssembly?pRequestingAssembly->GetDomainAssembly():NULL,tnb.GetString()->GetANSI(bufFullName), pAsmRef);
                if (pDomainAssembly)
                    th = GetTypeHaveAssembly(pDomainAssembly->GetAssembly(), bThrowIfNotFound, bIgnoreCase, pKeepAlive);
            }
        }
    }
    else
    {
        _ASSERTE(!"You must pass either a asm-qualified typename or an actual Assembly.");
    }


    if (!th.IsNull() && (!m_genericArguments.IsEmpty() || !m_signature.IsEmpty()))
    {
#ifdef CROSSGEN_COMPILE
        // This method is used to parse type names in custom attributes. We do not support
        // that these custom attributes will contain composed types.
        CrossGenNotSupported("GetTypeWorker");
#else
        struct _gc
        {
            PTRARRAYREF refGenericArguments;
            OBJECTREF keepAlive;
            REFLECTCLASSBASEREF refGenericArg;
        } gc;

        gc.refGenericArguments = NULL;
        gc.keepAlive = NULL;
        gc.refGenericArg = NULL;

        BOOL abortCall = FALSE;

        GCPROTECT_BEGIN(gc);
        INT32 cGenericArgs = m_genericArguments.GetCount();

        if (cGenericArgs > 0)
        {
            TypeHandle arrayHandle = ClassLoader::LoadArrayTypeThrowing(TypeHandle(g_pRuntimeTypeClass), ELEMENT_TYPE_SZARRAY);
            gc.refGenericArguments = (PTRARRAYREF)AllocateSzArray(arrayHandle, cGenericArgs);
        }
        // Instantiate generic arguments
        for (INT32 i = 0; i < cGenericArgs; i++)
        {
            TypeHandle thGenericArg = m_genericArguments[i]->GetTypeWorker(
                bThrowIfNotFound, bIgnoreCase,
                pAssemblyGetType, fEnableCASearchRules, bProhibitAsmQualifiedName, pRequestingAssembly,
                pPrivHostBinder,
                bLoadTypeFromPartialNameHack,
                (pKeepAlive != NULL) ? &gc.keepAlive : NULL /* Only pass a keepalive parameter if we were passed a keepalive parameter */);

            if (thGenericArg.IsNull())
            {
                abortCall = TRUE;
                break;
            }

            gc.refGenericArg = (REFLECTCLASSBASEREF)thGenericArg.GetManagedClassObject();

            gc.refGenericArguments->SetAt(i, gc.refGenericArg);
        }

        MethodDescCallSite getTypeHelper(METHOD__RT_TYPE_HANDLE__GET_TYPE_HELPER);

        ARG_SLOT args[5] = {
            (ARG_SLOT)OBJECTREFToObject(th.GetManagedClassObject()),
            (ARG_SLOT)OBJECTREFToObject(gc.refGenericArguments),
            (ARG_SLOT)(SIZE_T)m_signature.OpenRawBuffer(),
            m_signature.GetCount(),
        };

        REFLECTCLASSBASEREF refType = NULL;

        if (!abortCall)
            refType = (REFLECTCLASSBASEREF)getTypeHelper.Call_RetOBJECTREF(args);

        if (refType != NULL)
        {
            th = refType->GetType();
            if (pKeepAlive)
                *pKeepAlive = refType;
        }
        else
        {
            th = TypeHandle();
        }
        GCPROTECT_END();
#endif // CROSSGEN_COMPILE
    }

    if (th.IsNull() && bThrowIfNotFound)
    {
        StackSString buf;
        LPCWSTR wszName = ToString(&buf)->GetUnicode();
        MAKE_UTF8PTR_FROMWIDE(szName, wszName);

        if (GetAssembly() && !GetAssembly()->IsEmpty())
        {
            ThrowTypeLoadException(NULL, szName, GetAssembly()->GetUnicode(), NULL, IDS_CLASSLOAD_GENERAL);
        }
        else if (pAssemblyGetType)
        {
            pAssemblyGetType->ThrowTypeLoadException(NULL, szName, IDS_CLASSLOAD_GENERAL);
        }
        else if (pRequestingAssembly)
        {
            pRequestingAssembly->ThrowTypeLoadException(NULL, szName, IDS_CLASSLOAD_GENERAL);
        }
        else
        {
            ThrowTypeLoadException(NULL, szName, NULL, NULL, IDS_CLASSLOAD_GENERAL);
        }
    }

Exit:
    ;
    GCPROTECT_END();

    RETURN th;
}

//----------------------------------------------------------------------------------------------------------------
// This is the one that actually loads the type once we've pinned down the Assembly it's in.
//----------------------------------------------------------------------------------------------------------------
/* private */
TypeHandle
TypeName::GetTypeHaveAssemblyHelper(
    Assembly *  pAssembly,
    BOOL        bThrowIfNotFound,
    BOOL        bIgnoreCase,
    OBJECTREF * pKeepAlive,
    BOOL        bRecurse)
{
    WRAPPER_NO_CONTRACT;

    TypeHandle th = TypeHandle();
    SArray<SString *> & names = GetNames();
    Module *      pManifestModule = pAssembly->GetManifestModule();
    Module *      pLookOnlyInModule = NULL;
    ClassLoader * pClassLoader = pAssembly->GetLoader();

    NameHandle typeName(pManifestModule, mdtBaseType);

#ifndef CROSSGEN_COMPILE
    if (pAssembly->IsCollectible())
    {
        if (pKeepAlive == NULL)
        {
            COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleResolveFailure"));
        }
        *pKeepAlive = pAssembly->GetLoaderAllocator()->GetExposedObject();
    }
#endif

    // Set up the name handle
    if (bIgnoreCase)
        typeName.SetCaseInsensitive();

    EX_TRY
    {
        for (COUNT_T i = 0; i < names.GetCount(); i ++)
        {
            // each extra name represents one more level of nesting
            StackSString name(*(names[i]));

            // The type name is expected to be lower-cased by the caller for case-insensitive lookups
            if (bIgnoreCase)
                name.LowerCase();

            StackScratchBuffer buffer;
            typeName.SetName(name.GetUTF8(buffer));

            // typeName.m_pBucket gets set here if the type is found
            // it will be used in the next iteration to look up the nested type
            th = pClassLoader->LoadTypeHandleThrowing(&typeName, CLASS_LOADED, pLookOnlyInModule);

            // DDB 117395: if we didn't find a type, don't bother looking for its nested type
            if (th.IsNull())
                break;

            if (th.GetAssembly() != pAssembly)
            {   // It is forwarded type

                // Use the found assembly class loader for potential nested types search
                // The nested type has to be in the same module as the nesting type, so it doesn't make
                // sense to follow the same chain of type forwarders again for the nested type
                pClassLoader = th.GetAssembly()->GetLoader();
            }

            // Nested types must live in the module of the nesting type
            if ((i == 0) && (names.GetCount() > 1) && (pLookOnlyInModule == NULL))
            {
                Module * pFoundModule = th.GetModule();

                // Ensure that the bucket in the NameHandle is set to a valid bucket for all cases.

                // If the type is in the manifest module, it will always be set correctly,
                // or if the type is forwarded always lookup via the standard logic
                if ((pFoundModule == pManifestModule) || (pFoundModule->GetAssembly() != pAssembly))
                    continue;

                pLookOnlyInModule = pFoundModule;

                // If the type is not in the manifest module, and the nesting type is in the exported
                // types table of the manifest module, but the nested type is not, then unless the bucket
                // is from the actual defining module, then the LoadTypeHandleThrowing logic will fail.
                // To fix this, we must force the loader to record the bucket that refers to the nesting type
                // from within the defining module's available class table.

                // Re-run the LoadTypeHandleThrowing, but force it to only look in the class table for the module which
                // defines the type. This should cause typeName.m_pBucket to be set to the bucket
                // which corresponds to the type in the defining module, instead of potentially in the manifest module.
                i = -1;
                typeName.SetBucket(HashedTypeEntry());
            }
        }

        if (th.IsNull() && bRecurse)
        {
            IMDInternalImport * pManifestImport = pManifestModule->GetMDImport();
            HENUMInternalHolder phEnum(pManifestImport);
            phEnum.EnumInit(mdtFile, mdTokenNil);
            mdToken mdFile;

            while (pManifestImport->EnumNext(&phEnum, &mdFile))
            {
                if (pManifestModule->LookupFile(mdFile))
                    continue;

                pManifestModule->LoadModule(GetAppDomain(), mdFile, FALSE);

                th = GetTypeHaveAssemblyHelper(pAssembly, bThrowIfNotFound, bIgnoreCase, NULL, FALSE);

                if (!th.IsNull())
                    break;
            }
        }
    }
    EX_CATCH
    {
        if (bThrowIfNotFound)
            EX_RETHROW;

        Exception * ex = GET_EXCEPTION();

        // Let non-File-not-found exceptions propagate
        if (EEFileLoadException::GetFileLoadKind(ex->GetHR()) != kFileNotFoundException)
            EX_RETHROW;
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    return th;
} // TypeName::GetTypeHaveAssemblyHelper


DomainAssembly * LoadDomainAssembly(
    SString *  psszAssemblySpec,
    Assembly * pRequestingAssembly,
    ICLRPrivBinder * pPrivHostBinder,
    BOOL       bThrowIfNotFound,
    SString *  pssOuterTypeName)
{
    CONTRACTL
    {
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    AssemblySpec spec;
    DomainAssembly *pDomainAssembly = NULL;

    StackScratchBuffer buffer;
    LPCUTF8 szAssemblySpec = psszAssemblySpec->GetUTF8(buffer);
    IfFailThrow(spec.Init(szAssemblySpec));

    if (spec.IsContentType_WindowsRuntime())
    {
        _ASSERTE(pssOuterTypeName != NULL);
        spec.SetWindowsRuntimeType(*pssOuterTypeName);
    }

    if (pRequestingAssembly)
    {
        GCX_PREEMP();
        spec.SetParentAssembly(pRequestingAssembly->GetDomainAssembly());
    }

    // Have we been passed the reference to the binder against which this load should be triggered?
    // If so, then use it to set the fallback load context binder.
    if (pPrivHostBinder != NULL)
    {
        spec.SetFallbackLoadContextBinderForRequestingAssembly(pPrivHostBinder);
        spec.SetPreferFallbackLoadContextBinder();
    }
    else if (pRequestingAssembly != NULL)
    {
        // If the requesting assembly has Fallback LoadContext binder available,
        // then set it up in the AssemblySpec.
        PEFile *pRequestingAssemblyManifestFile = pRequestingAssembly->GetManifestFile();
        spec.SetFallbackLoadContextBinderForRequestingAssembly(pRequestingAssemblyManifestFile->GetFallbackLoadContextBinder());
    }

    if (bThrowIfNotFound)
    {
        pDomainAssembly = spec.LoadDomainAssembly(FILE_LOADED);
    }
    else
    {
        EX_TRY
        {
            pDomainAssembly = spec.LoadDomainAssembly(FILE_LOADED, bThrowIfNotFound);
        }
        EX_CATCH
        {
            Exception *ex = GET_EXCEPTION();

            // Let non-File-not-found exceptions propagate
            if (EEFileLoadException::GetFileLoadKind(ex->GetHR()) != kFileNotFoundException)
                EX_RETHROW;
        }
        EX_END_CATCH(RethrowTerminalExceptions);
    }

    return pDomainAssembly;
}


