// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: daccess.cpp
//

//
// ClrDataAccess implementation.
//
//*****************************************************************************

#include "stdafx.h"
#include <clrdata.h>
#include "typestring.h"
#include "holder.h"
#include "debuginfostore.h"
#include "peimagelayout.inl"
#include "datatargetadapter.h"
#include "readonlydatatargetfacade.h"
#include "metadataexports.h"
#include "excep.h"
#include "debugger.h"
#include "dwreport.h"
#include "primitives.h"
#include "dbgutil.h"

#ifdef USE_DAC_TABLE_RVA
#include <dactablerva.h>
#else
extern "C" bool TryGetSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress);
#endif

#include "dwbucketmanager.hpp"
#include "gcinterface.dac.h"

#include "libhellomanaged.h"

// To include definition of IsThrowableThreadAbortException
// #include <exstatecommon.h>

CRITICAL_SECTION g_dacCritSec;
ClrDataAccess* g_dacImpl;

EXTERN_C
#ifdef TARGET_UNIX
DLLEXPORT // For Win32 PAL LoadLibrary emulation
#endif
BOOL WINAPI DllMain(HANDLE instance, DWORD reason, LPVOID reserved)
{
    static bool g_procInitialized = false;

    switch(reason)
    {
    case DLL_PROCESS_ATTACH:
    {
        if (g_procInitialized)
        {
#ifdef HOST_UNIX
            // Double initialization can happen on Unix
            // in case of manual load of DAC shared lib and calling DllMain
            // not a big deal, we just ignore it.
            return TRUE;
#else
            return FALSE;
#endif
        }

	hellomanaged_Hello();

#ifdef HOST_UNIX
        int err = PAL_InitializeDLL();
        if(err != 0)
        {
            return FALSE;
        }
#endif
        InitializeCriticalSection(&g_dacCritSec);

        g_procInitialized = true;
        break;
    }

    case DLL_PROCESS_DETACH:
        // It's possible for this to be called without ATTACH completing (eg. if it failed)
        if (g_procInitialized)
        {
            DeleteCriticalSection(&g_dacCritSec);
        }
        g_procInitialized = false;
        break;
    }

    return TRUE;
}

HRESULT
ConvertUtf8(_In_ LPCUTF8 utf8,
            ULONG32 bufLen,
            ULONG32* nameLen,
            _Out_writes_to_opt_(bufLen, *nameLen) PWSTR buffer)
{
    if (nameLen)
    {
        *nameLen = WszMultiByteToWideChar(CP_UTF8, 0, utf8, -1, NULL, 0);
        if (!*nameLen)
        {
            return HRESULT_FROM_GetLastError();
        }
    }

    if (buffer && bufLen)
    {
        if (!WszMultiByteToWideChar(CP_UTF8, 0, utf8, -1, buffer, bufLen))
        {
            return HRESULT_FROM_GetLastError();
        }
    }

    return S_OK;
}

HRESULT
AllocUtf8(_In_opt_ LPCWSTR wstr,
          ULONG32 srcChars,
          _Outptr_ LPUTF8* utf8)
{
    ULONG32 chars = WszWideCharToMultiByte(CP_UTF8, 0, wstr, srcChars,
                                           NULL, 0, NULL, NULL);
    if (!chars)
    {
        return HRESULT_FROM_GetLastError();
    }

    // Make sure the converted string is always terminated.
    if (srcChars != (ULONG32)-1)
    {
        if (!ClrSafeInt<ULONG32>::addition(chars, 1, chars))
        {
            return HRESULT_FROM_WIN32(ERROR_ARITHMETIC_OVERFLOW);
        }
    }

    char* mem = new (nothrow) char[chars];
    if (!mem)
    {
        return E_OUTOFMEMORY;
    }

    if (!WszWideCharToMultiByte(CP_UTF8, 0, wstr, srcChars,
                                mem, chars, NULL, NULL))
    {
        HRESULT hr = HRESULT_FROM_GetLastError();
        delete [] mem;
        return hr;
    }

    if (srcChars != (ULONG32)-1)
    {
        mem[chars - 1] = 0;
    }

    *utf8 = mem;
    return S_OK;
}

HRESULT
GetFullClassNameFromMetadata(IMDInternalImport* mdImport,
                             mdTypeDef classToken,
                             ULONG32 bufferChars,
                             _Inout_updates_(bufferChars) LPUTF8 buffer)
{
    HRESULT hr;
    LPCUTF8 baseName, namespaceName;

    IfFailRet(mdImport->GetNameOfTypeDef(classToken, &baseName, &namespaceName));
    return ns::MakePath(buffer, bufferChars, namespaceName, baseName) ?
        S_OK : E_OUTOFMEMORY;
}

HRESULT
GetFullMethodNameFromMetadata(IMDInternalImport* mdImport,
                              mdMethodDef methodToken,
                              ULONG32 bufferChars,
                              _Inout_updates_(bufferChars) LPUTF8 buffer)
{
    HRESULT status;
    HRESULT hr;
    mdTypeDef classToken;
    size_t len;

    if (mdImport->GetParentToken(methodToken, &classToken) == S_OK)
    {
        if ((status =
             GetFullClassNameFromMetadata(mdImport, classToken,
                                          bufferChars, buffer)) != S_OK)
        {
            return status;
        }

        len = strlen(buffer);
        buffer += len;
        bufferChars -= static_cast<ULONG32>(len) + 1;

        if (!bufferChars)
        {
            return E_OUTOFMEMORY;
        }

        *buffer++ = NAMESPACE_SEPARATOR_CHAR;
    }

    LPCUTF8 methodName;
    IfFailRet(mdImport->GetNameOfMethodDef(methodToken, &methodName));

    len = strlen(methodName);
    if (len >= bufferChars)
    {
        return E_OUTOFMEMORY;
    }

    strcpy_s(buffer, bufferChars, methodName);
    return S_OK;
}

HRESULT
SplitFullName(_In_z_ PCWSTR fullName,
              SplitSyntax syntax,
              ULONG32 memberDots,
              _Outptr_opt_ LPUTF8* namespaceName,
              _Outptr_opt_ LPUTF8* typeName,
              _Outptr_opt_ LPUTF8* memberName,
              _Outptr_opt_ LPUTF8* params)
{
    HRESULT status;
    PCWSTR paramsStart, memberStart, memberEnd, typeStart;

    if (!*fullName)
    {
        return E_INVALIDARG;
    }

    //
    // Split off parameters.
    //

    paramsStart = u16_strchr(fullName, W('('));
    if (paramsStart)
    {
        if (syntax != SPLIT_METHOD ||
            paramsStart == fullName)
        {
            return E_INVALIDARG;
        }

        if ((status = AllocUtf8(paramsStart, (ULONG32)-1, params)) != S_OK)
        {
            return status;
        }

        memberEnd = paramsStart - 1;
    }
    else
    {
        *params = NULL;
        memberEnd = fullName + (u16_strlen(fullName) - 1);
    }

    if (syntax != SPLIT_TYPE)
    {
        //
        // Split off member name.
        //

        memberStart = memberEnd;

        while (true)
        {
            while (memberStart >= fullName &&
                   *memberStart != W('.'))
            {
                memberStart--;
            }

            // Some member names (e.g. .ctor and .dtor) have
            // dots, so go back to the first dot.
            while (memberStart > fullName &&
                   memberStart[-1] == W('.'))
            {
                memberStart--;
            }

            if (memberStart <= fullName)
            {
                if (memberDots > 0)
                {
                    // Caller expected dots in the
                    // member name and they weren't found.
                    status = E_INVALIDARG;
                    goto DelParams;
                }

                break;
            }
            else if (memberDots == 0)
            {
                break;
            }

            memberStart--;
            memberDots--;
        }

        memberStart++;
        if (memberStart > memberEnd)
        {
            status = E_INVALIDARG;
            goto DelParams;
        }

        if ((status = AllocUtf8(memberStart, (ULONG32)
                                (memberEnd - memberStart) + 1,
                                memberName)) != S_OK)
        {
            goto DelParams;
        }
    }
    else
    {
        *memberName = NULL;
        memberStart = memberEnd + 2;
    }

    //
    // Split off type name.
    //

    if (memberStart > fullName)
    {
        // Must have at least one character for the type
        // name.  If there was a member name, there must
        // also be a separator.
        if (memberStart < fullName + 2)
        {
            status = E_INVALIDARG;
            goto DelMember;
        }

        typeStart = memberStart - 2;
        while (typeStart >= fullName &&
               *typeStart != W('.'))
        {
            typeStart--;
        }
        typeStart++;

        if ((status = AllocUtf8(typeStart, (ULONG32)
                                (memberStart - typeStart) - 1,
                                typeName)) != S_OK)
        {
            goto DelMember;
        }
    }
    else
    {
        *typeName = NULL;
        typeStart = fullName;
    }

    //
    // Namespace must be the rest.
    //

    if (typeStart > fullName)
    {
        if ((status = AllocUtf8(fullName, (ULONG32)
                                (typeStart - fullName) - 1,
                                namespaceName)) != S_OK)
        {
            goto DelType;
        }
    }
    else
    {
        *namespaceName = NULL;
    }

    return S_OK;

 DelType:
    delete [] (*typeName);
 DelMember:
    delete [] (*memberName);
 DelParams:
    delete [] (*params);
    return status;
}

int
CompareUtf8(_In_ LPCUTF8 str1, _In_ LPCUTF8 str2, _In_ ULONG32 nameFlags)
{
    if (nameFlags & CLRDATA_BYNAME_CASE_INSENSITIVE)
    {
        // XXX Microsoft - Convert to Unicode?
        return SString::_stricmp(str1, str2);
    }

    return strcmp(str1, str2);
}

//----------------------------------------------------------------------------
//
// MetaEnum.
//
//----------------------------------------------------------------------------

HRESULT
MetaEnum::Start(IMDInternalImport* mdImport, ULONG32 kind,
                mdToken container)
{
    HRESULT status;

    switch(kind)
    {
    case mdtTypeDef:
        status = mdImport->EnumTypeDefInit(&m_enum);
        break;
    case mdtMethodDef:
    case mdtFieldDef:
        status = mdImport->EnumInit(kind, container, &m_enum);
        break;
    default:
        return E_INVALIDARG;
    }
    if (status != S_OK)
    {
        return status;
    }

    m_mdImport = mdImport;
    m_kind = kind;

    return S_OK;
}

void
MetaEnum::End(void)
{
    if (!m_mdImport)
    {
        return;
    }

    switch(m_kind)
    {
    case mdtTypeDef:
    case mdtMethodDef:
    case mdtFieldDef:
        m_mdImport->EnumClose(&m_enum);
        break;
    }

    Clear();
}

HRESULT
MetaEnum::NextToken(mdToken* token,
                    _Outptr_opt_result_maybenull_ LPCUTF8* namespaceName,
                    _Outptr_opt_result_maybenull_ LPCUTF8* name)
{
    HRESULT hr;
    if (!m_mdImport)
    {
        return E_INVALIDARG;
    }

    switch(m_kind)
    {
    case mdtTypeDef:
        if (!m_mdImport->EnumNext(&m_enum, token))
        {
            return S_FALSE;
        }
        m_lastToken = *token;
        if (namespaceName || name)
        {
            LPCSTR _name, _namespaceName;

            IfFailRet(m_mdImport->GetNameOfTypeDef(*token, &_name, &_namespaceName));
            if (namespaceName)
            {
                *namespaceName = _namespaceName;
            }
            if (name)
            {
                *name = _name;
            }
        }
        return S_OK;

    case mdtMethodDef:
        if (!m_mdImport->EnumNext(&m_enum, token))
        {
            return S_FALSE;
        }
        m_lastToken = *token;
        if (namespaceName)
        {
            *namespaceName = NULL;
        }
        if (name != NULL)
        {
            IfFailRet(m_mdImport->GetNameOfMethodDef(*token, name));
        }
        return S_OK;

    case mdtFieldDef:
        if (!m_mdImport->EnumNext(&m_enum, token))
        {
            return S_FALSE;
        }
        m_lastToken = *token;
        if (namespaceName)
        {
            *namespaceName = NULL;
        }
        if (name != NULL)
        {
            IfFailRet(m_mdImport->GetNameOfFieldDef(*token, name));
        }
        return S_OK;

    default:
        return E_INVALIDARG;
    }
}

HRESULT
MetaEnum::NextDomainToken(AppDomain** appDomain,
                          mdToken* token)
{
    HRESULT status;

    if (m_appDomain)
    {
        // Use only the caller-provided app domain.
        *appDomain = m_appDomain;
        return NextToken(token, NULL, NULL);
    }

    // Need to fetch a token.
    if ((status = NextToken(token, NULL, NULL)) != S_OK)
    {
        return status;
    }

    *appDomain = AppDomain::GetCurrentDomain();
    *token = m_lastToken;

    return S_OK;
}

HRESULT
MetaEnum::NextTokenByName(_In_opt_ LPCUTF8 namespaceName,
                          _In_opt_ LPCUTF8 name,
                          ULONG32 nameFlags,
                          mdToken* token)
{
    HRESULT status;
    LPCUTF8 tokNamespace, tokName;

    while (true)
    {
        if ((status = NextToken(token, &tokNamespace, &tokName)) != S_OK)
        {
            return status;
        }

        if (namespaceName &&
            (!tokNamespace ||
             CompareUtf8(namespaceName, tokNamespace, nameFlags) != 0))
        {
            continue;
        }
        if (name &&
            (!tokName ||
             CompareUtf8(name, tokName, nameFlags) != 0))
        {
            continue;
        }

        return S_OK;
    }
}

HRESULT
MetaEnum::NextDomainTokenByName(_In_opt_ LPCUTF8 namespaceName,
                                _In_opt_ LPCUTF8 name,
                                ULONG32 nameFlags,
                                AppDomain** appDomain, mdToken* token)
{
    HRESULT status;

    if (m_appDomain)
    {
        // Use only the caller-provided app domain.
        *appDomain = m_appDomain;
        return NextTokenByName(namespaceName, name, nameFlags, token);
    }

    // Need to fetch a token.
    if ((status = NextTokenByName(namespaceName, name, nameFlags,
                                    token)) != S_OK)
    {
        return status;
    }

    *appDomain = AppDomain::GetCurrentDomain();
    *token = m_lastToken;

    return S_OK;
}

HRESULT
MetaEnum::New(Module* mod,
              ULONG32 kind,
              mdToken container,
              IXCLRDataAppDomain* pubAppDomain,
              MetaEnum** metaEnumRet,
              CLRDATA_ENUM* handle)
{
    HRESULT status;
    MetaEnum* metaEnum;

    if (handle)
    {
        *handle = TO_CDENUM(NULL);
    }

    metaEnum = new (nothrow) MetaEnum;
    if (!metaEnum)
    {
        return E_OUTOFMEMORY;
    }

    if ((status = metaEnum->
         Start(mod->GetMDImport(), kind, container)) != S_OK)
    {
        delete metaEnum;
        return status;
    }

    if (pubAppDomain)
    {
        metaEnum->m_appDomain =
            ((ClrDataAppDomain*)pubAppDomain)->GetAppDomain();
    }

    if (metaEnumRet)
    {
        *metaEnumRet = metaEnum;
    }
    if (handle)
    {
        *handle = TO_CDENUM(metaEnum);
    }
    return S_OK;
}

//----------------------------------------------------------------------------
//
// SplitName
//
//----------------------------------------------------------------------------

SplitName::SplitName(SplitSyntax syntax, ULONG32 nameFlags,
                     ULONG32 memberDots)
{
    m_syntax = syntax;
    m_nameFlags = nameFlags;
    m_memberDots = memberDots;

    Clear();
}

void
SplitName::Delete(void)
{
    delete [] m_namespaceName;
    m_namespaceName = NULL;
    delete [] m_typeName;
    m_typeName = NULL;
    delete [] m_memberName;
    m_memberName = NULL;
    delete [] m_params;
    m_params = NULL;
}

void
SplitName::Clear(void)
{
    m_namespaceName = NULL;
    m_typeName = NULL;
    m_typeToken = mdTypeDefNil;
    m_memberName = NULL;
    m_memberToken = mdTokenNil;
    m_params = NULL;

    m_tlsThread = NULL;
    m_metaEnum.m_appDomain = NULL;
    m_module = NULL;
    m_lastField = NULL;
}

HRESULT
SplitName::SplitString(_In_opt_ PCWSTR fullName)
{
    if (m_syntax == SPLIT_NO_NAME)
    {
        if (fullName)
        {
            return E_INVALIDARG;
        }

        return S_OK;
    }
    else if (!fullName)
    {
        return E_INVALIDARG;
    }

    return SplitFullName(fullName,
                         m_syntax,
                         m_memberDots,
                         &m_namespaceName,
                         &m_typeName,
                         &m_memberName,
                         &m_params);
}

FORCEINLINE
WCHAR* wcrscan(LPCWSTR beg, LPCWSTR end, WCHAR ch)
{
    //_ASSERTE(beg <= end);
    WCHAR *p;
    for (p = (WCHAR*)end; p >= beg; --p)
    {
        if (*p == ch)
            break;
    }
    return p;
}

// This functions allocates a new UTF8 string that contains the classname
// lying between the current sepName and the previous sepName.  E.g. for a
// class name of "Outer+middler+inner" when sepName points to the NULL
// terminator this function will return "inner" in pResult and will update
// sepName to point to the second '+' character in the string.  When sepName
// points to the first '+' character this function will return "Outer" in
// pResult and sepName will point one WCHAR before fullName.
HRESULT NextEnclosingClasName(LPCWSTR fullName, _Outref_ LPWSTR& sepName, _Outptr_ LPUTF8 *pResult)
{
    if (sepName < fullName)
    {
        return E_FAIL;
    }
    //_ASSERTE(*sepName == W('\0') || *sepName == W('+') || *sepName == W('/'));

    LPWSTR origInnerName = sepName-1;
    if ((sepName = wcrscan(fullName, origInnerName, W('+'))) < fullName)
    {
        sepName = wcrscan(fullName, origInnerName, W('/'));
    }

    return AllocUtf8(sepName+1, static_cast<ULONG32>(origInnerName-sepName), pResult);
}

bool
SplitName::FindType(IMDInternalImport* mdInternal)
{
    if (m_typeToken != mdTypeDefNil)
    {
        return true;
    }

    if (!m_typeName)
    {
        return false;
    }

    if ((m_namespaceName == NULL || m_namespaceName[0] == '\0')
        && (CompareUtf8(COR_MODULE_CLASS, m_typeName, m_nameFlags)==0))
    {
        m_typeToken = TokenFromRid(1, mdtTypeDef);  // <Module> class always has a RID of 1.
        return true;
    }

    MetaEnum metaEnum;

    if (metaEnum.Start(mdInternal, mdtTypeDef, mdTypeDefNil) != S_OK)
    {
        return false;
    }

    LPUTF8 curClassName;

    ULONG32 length;
    WCHAR   wszName[MAX_CLASS_NAME];
    if (ConvertUtf8(m_typeName, MAX_CLASS_NAME, &length, wszName) != S_OK)
    {
        return false;
    }

    WCHAR *pHead;

Retry:

    pHead = wszName + length;

    if (FAILED(NextEnclosingClasName(wszName, pHead, &curClassName)))
    {
        return false;
    }

    // an inner class has an empty namespace associated with it
    HRESULT hr = metaEnum.NextTokenByName((pHead < wszName) ? m_namespaceName : "",
                                    curClassName,
                                    m_nameFlags,
                                    &m_typeToken);
    delete[] curClassName;

    if (hr != S_OK)
    {
        // if we didn't find a token with the given name
        return false;
    }
    else if (pHead < wszName)
    {
        // if we did find a token, *and* the class name given
        // does not specify any enclosing class, that's it
        return true;
    }
    else
    {
        // restart with innermost class
        pHead = wszName + length;
        mdTypeDef tkInner = m_typeToken;
        mdTypeDef tkOuter;
        BOOL bRetry = FALSE;
        LPUTF8 utf8Name;

        while (
            !bRetry
            && SUCCEEDED(NextEnclosingClasName(wszName, pHead, &utf8Name))
        )
        {
            if (mdInternal->GetNestedClassProps(tkInner, &tkOuter) != S_OK)
                tkOuter = mdTypeDefNil;

            LPCSTR szName, szNS;
            if (FAILED(mdInternal->GetNameOfTypeDef(tkInner, &szName, &szNS)))
            {
                return false;
            }
            bRetry = (CompareUtf8(utf8Name, szName, m_nameFlags) != 0);
            if (!bRetry)
            {
                // if this is outermost class we need to compare namespaces too
                if (tkOuter == mdTypeDefNil)
                {
                    // is this the outermost in the class name, too?
                    if (pHead < wszName
                        && CompareUtf8(m_namespaceName ? m_namespaceName : "", szNS, m_nameFlags) == 0)
                    {
                        delete[] utf8Name;
                        return true;
                    }
                    else
                    {
                        bRetry = TRUE;
                    }
                }
            }
            delete[] utf8Name;
            tkInner = tkOuter;
        }

        goto Retry;
    }

}

bool
SplitName::FindMethod(IMDInternalImport* mdInternal)
{
    if (m_memberToken != mdTokenNil)
    {
        return true;
    }

    if (m_typeToken == mdTypeDefNil ||
        !m_memberName)
    {
        return false;
    }

    ULONG32 EmptySig = 0;

    // XXX Microsoft - Compare using signature when available.
    if (mdInternal->FindMethodDefUsingCompare(m_typeToken,
                                              m_memberName,
                                              (PCCOR_SIGNATURE)&EmptySig,
                                              sizeof(EmptySig),
                                              NULL,
                                              NULL,
                                              &m_memberToken) != S_OK)
    {
        m_memberToken = mdTokenNil;
        return false;
    }

    return true;
}

bool
SplitName::FindField(IMDInternalImport* mdInternal)
{
    if (m_memberToken != mdTokenNil)
    {
        return true;
    }

    if (m_typeToken == mdTypeDefNil ||
        !m_memberName ||
        m_params)
    {
        // Can't have params with a field.
        return false;
    }

    MetaEnum metaEnum;

    if (metaEnum.Start(mdInternal, mdtFieldDef, m_typeToken) != S_OK)
    {
        return false;
    }

    return metaEnum.NextTokenByName(NULL,
                                    m_memberName,
                                    m_nameFlags,
                                    &m_memberToken) == S_OK;
}

HRESULT
SplitName::AllocAndSplitString(_In_opt_ PCWSTR fullName,
                               SplitSyntax syntax,
                               ULONG32 nameFlags,
                               ULONG32 memberDots,
                               SplitName** split)
{
    HRESULT status;

    if (nameFlags & ~(CLRDATA_BYNAME_CASE_SENSITIVE |
                      CLRDATA_BYNAME_CASE_INSENSITIVE))
    {
        return E_INVALIDARG;
    }

    *split = new (nothrow) SplitName(syntax, nameFlags, memberDots);
    if (!*split)
    {
        return E_OUTOFMEMORY;
    }

    if ((status = (*split)->SplitString(fullName)) != S_OK)
    {
        delete (*split);
        return status;
    }

    return S_OK;
}

HRESULT
SplitName::CdStartMethod(_In_opt_ PCWSTR fullName,
                         ULONG32 nameFlags,
                         Module* mod,
                         mdTypeDef typeToken,
                         AppDomain* appDomain,
                         IXCLRDataAppDomain* pubAppDomain,
                         SplitName** splitRet,
                         CLRDATA_ENUM* handle)
{
    HRESULT status;
    SplitName* split;
    ULONG methDots = 0;

    *handle = TO_CDENUM(NULL);

 Retry:
    if ((status = SplitName::
         AllocAndSplitString(fullName, SPLIT_METHOD, nameFlags,
                             methDots, &split)) != S_OK)
    {
        return status;
    }

    if (typeToken == mdTypeDefNil)
    {
        if (!split->FindType(mod->GetMDImport()))
        {
            bool hasNamespace = split->m_namespaceName != NULL;

            delete split;

            //
            // We may have a case where there's an
            // explicitly implemented method which
            // has dots in the name.  If it's possible
            // to move the method name dot split
            // back, go ahead and retry that way.
            //

            if (hasNamespace)
            {
                methDots++;
                goto Retry;
            }

            return E_INVALIDARG;
        }

        typeToken = split->m_typeToken;
    }
    else
    {
        if (split->m_namespaceName || split->m_typeName)
        {
            delete split;
            return E_INVALIDARG;
        }
    }

    if ((status = split->m_metaEnum.
         Start(mod->GetMDImport(), mdtMethodDef, typeToken)) != S_OK)
    {
        delete split;
        return status;
    }

    split->m_metaEnum.m_appDomain = appDomain;
    if (pubAppDomain)
    {
        split->m_metaEnum.m_appDomain =
            ((ClrDataAppDomain*)pubAppDomain)->GetAppDomain();
    }
    split->m_module = mod;

    *handle = TO_CDENUM(split);
    if (splitRet)
    {
        *splitRet = split;
    }
    return S_OK;
}

HRESULT
SplitName::CdNextMethod(CLRDATA_ENUM* handle,
                        mdMethodDef* token)
{
    SplitName* split = FROM_CDENUM(SplitName, *handle);
    if (!split)
    {
        return E_INVALIDARG;
    }

    return split->m_metaEnum.
        NextTokenByName(NULL, split->m_memberName, split->m_nameFlags,
                        token);
}

HRESULT
SplitName::CdNextDomainMethod(CLRDATA_ENUM* handle,
                              AppDomain** appDomain,
                              mdMethodDef* token)
{
    SplitName* split = FROM_CDENUM(SplitName, *handle);
    if (!split)
    {
        return E_INVALIDARG;
    }

    return split->m_metaEnum.
        NextDomainTokenByName(NULL, split->m_memberName, split->m_nameFlags,
                              appDomain, token);
}

HRESULT
SplitName::CdStartField(_In_opt_ PCWSTR fullName,
                        ULONG32 nameFlags,
                        ULONG32 fieldFlags,
                        IXCLRDataTypeInstance* fromTypeInst,
                        TypeHandle typeHandle,
                        Module* mod,
                        mdTypeDef typeToken,
                        ULONG64 objBase,
                        Thread* tlsThread,
                        IXCLRDataTask* pubTlsThread,
                        AppDomain* appDomain,
                        IXCLRDataAppDomain* pubAppDomain,
                        SplitName** splitRet,
                        CLRDATA_ENUM* handle)
{
    HRESULT status;
    SplitName* split;

    *handle = TO_CDENUM(NULL);

    if ((status = SplitName::
         AllocAndSplitString(fullName,
                             fullName ? SPLIT_FIELD : SPLIT_NO_NAME,
                             nameFlags, 0,
                             &split)) != S_OK)
    {
        return status;
    }

    if (typeHandle.IsNull())
    {
        if (typeToken == mdTypeDefNil)
        {
            if (!split->FindType(mod->GetMDImport()))
            {
                status = E_INVALIDARG;
                goto Fail;
            }

            typeToken = split->m_typeToken;
        }
        else
        {
            if (split->m_namespaceName || split->m_typeName)
            {
                status = E_INVALIDARG;
                goto Fail;
            }
        }

        // With phased class loading, this may return a partially-loaded type
        // @todo : does this matter?
        typeHandle = mod->LookupTypeDef(split->m_typeToken);
        if (typeHandle.IsNull())
        {
            status = E_UNEXPECTED;
            goto Fail;
        }
    }

    if ((status = InitFieldIter(&split->m_fieldEnum,
                                typeHandle,
                                true,
                                fieldFlags,
                                fromTypeInst)) != S_OK)
    {
        goto Fail;
    }

    split->m_objBase = objBase;
    split->m_tlsThread = tlsThread;
    if (pubTlsThread)
    {
        split->m_tlsThread = ((ClrDataTask*)pubTlsThread)->GetThread();
    }
    split->m_metaEnum.m_appDomain = appDomain;
    if (pubAppDomain)
    {
        split->m_metaEnum.m_appDomain =
            ((ClrDataAppDomain*)pubAppDomain)->GetAppDomain();
    }
    split->m_module = mod;

    *handle = TO_CDENUM(split);
    if (splitRet)
    {
        *splitRet = split;
    }
    return S_OK;

 Fail:
    delete split;
    return status;
}

HRESULT
SplitName::CdNextField(ClrDataAccess* dac,
                       CLRDATA_ENUM* handle,
                       IXCLRDataTypeDefinition** fieldType,
                       ULONG32* fieldFlags,
                       IXCLRDataValue** value,
                       ULONG32 nameBufRetLen,
                       ULONG32* nameLenRet,
                       _Out_writes_to_opt_(nameBufRetLen, *nameLenRet) WCHAR nameBufRet[  ],
                       IXCLRDataModule** tokenScopeRet,
                       mdFieldDef* tokenRet)
{
    HRESULT status;

    SplitName* split = FROM_CDENUM(SplitName, *handle);
    if (!split)
    {
        return E_INVALIDARG;
    }

    FieldDesc* fieldDesc;

    while ((fieldDesc = split->m_fieldEnum.Next()))
    {
        if (split->m_syntax != SPLIT_NO_NAME)
        {
            LPCUTF8 fieldName;
            if (FAILED(fieldDesc->GetName_NoThrow(&fieldName)) ||
                (split->Compare(split->m_memberName, fieldName) != 0))
            {
                continue;
            }
        }

        split->m_lastField = fieldDesc;

        if (fieldFlags != NULL)
        {
            *fieldFlags =
                GetTypeFieldValueFlags(fieldDesc->GetFieldTypeHandleThrowing(),
                                       fieldDesc,
                                       split->m_fieldEnum.
                                       IsFieldFromParentClass() ?
                                       CLRDATA_FIELD_IS_INHERITED : 0,
                                       false);
        }

        if ((nameBufRetLen != 0) || (nameLenRet != NULL))
        {
            LPCUTF8 szFieldName;
            status = fieldDesc->GetName_NoThrow(&szFieldName);
            if (status != S_OK)
            {
                return status;
            }

            status = ConvertUtf8(
                szFieldName,
                nameBufRetLen,
                nameLenRet,
                nameBufRet);
            if (status != S_OK)
            {
                return status;
            }
        }

        if (tokenScopeRet && !value)
        {
            *tokenScopeRet = new (nothrow)
                ClrDataModule(dac, fieldDesc->GetModule());
            if (!*tokenScopeRet)
            {
                return E_OUTOFMEMORY;
            }
        }

        if (tokenRet)
        {
            *tokenRet = fieldDesc->GetMemberDef();
        }

        if (fieldType)
        {
            TypeHandle fieldTypeHandle = fieldDesc->GetFieldTypeHandleThrowing();
            *fieldType = new (nothrow)
                ClrDataTypeDefinition(dac,
                                      fieldTypeHandle.GetModule(),
                                      fieldTypeHandle.GetMethodTable()->GetCl(),
                                      fieldTypeHandle);
            if (!*fieldType && tokenScopeRet)
            {
                delete (ClrDataModule*)*tokenScopeRet;
            }
            return *fieldType ? S_OK : E_OUTOFMEMORY;
        }

        if (value)
        {
            return ClrDataValue::
                NewFromFieldDesc(dac,
                                 split->m_metaEnum.m_appDomain,
                                 split->m_fieldEnum.IsFieldFromParentClass() ?
                                 CLRDATA_VALUE_IS_INHERITED : 0,
                                 fieldDesc,
                                 split->m_objBase,
                                 split->m_tlsThread,
                                 NULL,
                                 value,
                                 nameBufRetLen,
                                 nameLenRet,
                                 nameBufRet,
                                 tokenScopeRet,
                                 tokenRet);
        }

        return S_OK;
    }

    return S_FALSE;
}

HRESULT
SplitName::CdNextDomainField(ClrDataAccess* dac,
                             CLRDATA_ENUM* handle,
                             IXCLRDataValue** value)
{
    HRESULT status;

    SplitName* split = FROM_CDENUM(SplitName, *handle);
    if (!split)
    {
        return E_INVALIDARG;
    }

    if (split->m_metaEnum.m_appDomain)
    {
        // Use only the caller-provided app domain.
        return CdNextField(dac, handle, NULL, NULL, value,
                           0, NULL, NULL, NULL, NULL);
    }

    // Need to fetch a field.
    if ((status = CdNextField(dac, handle, NULL, NULL, NULL,
                                0, NULL, NULL, NULL, NULL)) != S_OK)
    {
        return status;
    }

    return ClrDataValue::
        NewFromFieldDesc(dac,
                         AppDomain::GetCurrentDomain(),
                         split->m_fieldEnum.IsFieldFromParentClass() ?
                         CLRDATA_VALUE_IS_INHERITED : 0,
                         split->m_lastField,
                         split->m_objBase,
                         split->m_tlsThread,
                         NULL,
                         value,
                         0,
                         NULL,
                         NULL,
                         NULL,
                         NULL);
}

HRESULT
SplitName::CdStartType(_In_opt_ PCWSTR fullName,
                       ULONG32 nameFlags,
                       Module* mod,
                       AppDomain* appDomain,
                       IXCLRDataAppDomain* pubAppDomain,
                       SplitName** splitRet,
                       CLRDATA_ENUM* handle)
{
    HRESULT status;
    SplitName* split;

    *handle = TO_CDENUM(NULL);

    if ((status = SplitName::
         AllocAndSplitString(fullName, SPLIT_TYPE, nameFlags, 0,
                             &split)) != S_OK)
    {
        return status;
    }

    if ((status = split->m_metaEnum.
         Start(mod->GetMDImport(), mdtTypeDef, mdTokenNil)) != S_OK)
    {
        delete split;
        return status;
    }

    split->m_metaEnum.m_appDomain = appDomain;
    if (pubAppDomain)
    {
        split->m_metaEnum.m_appDomain =
            ((ClrDataAppDomain*)pubAppDomain)->GetAppDomain();
    }
    split->m_module = mod;

    *handle = TO_CDENUM(split);
    if (splitRet)
    {
        *splitRet = split;
    }
    return S_OK;
}

HRESULT
SplitName::CdNextType(CLRDATA_ENUM* handle,
                      mdTypeDef* token)
{
    SplitName* split = FROM_CDENUM(SplitName, *handle);
    if (!split)
    {
        return E_INVALIDARG;
    }

    return split->m_metaEnum.
        NextTokenByName(split->m_namespaceName, split->m_typeName,
                        split->m_nameFlags, token);
}

HRESULT
SplitName::CdNextDomainType(CLRDATA_ENUM* handle,
                            AppDomain** appDomain,
                            mdTypeDef* token)
{
    SplitName* split = FROM_CDENUM(SplitName, *handle);
    if (!split)
    {
        return E_INVALIDARG;
    }

    return split->m_metaEnum.
        NextDomainTokenByName(split->m_namespaceName, split->m_typeName,
                              split->m_nameFlags, appDomain, token);
}

//----------------------------------------------------------------------------
//
// DacInstanceManager.
//
// Data retrieved from the target process is cached for two reasons:
//
// 1. It may be necessary to map from the host address back to the target
//    address.  For example, if any code uses a 'this' pointer or
//    takes the address of a field the address has to be translated from
//    host to target.  This requires instances to be held as long as
//    they may be referenced.
//
// 2. Data is often referenced multiple times so caching is an important
//    performance advantage.
//
// Ideally we'd like to implement a simple page cache but this is
// complicated by the fact that user minidump memory can have
// arbitrary granularity and also that the member operator (->)
// needs to return a pointer to an object.  That means that all of
// the data for an object must be sequential and cannot be split
// at page boundaries.
//
// Data can also be accessed with different sizes.  For example,
// a base struct can be accessed, then cast to a derived struct and
// accessed again with the larger derived size.  The cache must
// be able to replace data to maintain the largest amount of data
// touched.
//
// We keep track of each access and the recovered memory for it.
// A hash on target address allows quick access to instance data
// by target address.  The data for each access has a header on it
// for bookkeeping purposes, so host address to target address translation
// is just a matter of backing up to the header and pulling the target
// address from it.  Keeping each access separately allows easy
// replacement by larger accesses.
//
//----------------------------------------------------------------------------

DacInstanceManager::DacInstanceManager(void)
    : m_unusedBlock(NULL)
{
    InitEmpty();
}

DacInstanceManager::~DacInstanceManager(void)
{
    // We are stopping debugging in this case, so don't save any block of memory.
    // Otherwise, there will be a memory leak.
    Flush(false);
}

#if defined(DAC_HASHTABLE)
DAC_INSTANCE*
DacInstanceManager::Add(DAC_INSTANCE* inst)
{
    // Assert that we don't add NULL instances. This allows us to assert that found instances
    // are not NULL in DacInstanceManager::Find
    _ASSERTE(inst != NULL);

    DWORD nHash = DAC_INSTANCE_HASH(inst->addr);
    HashInstanceKeyBlock* block = m_hash[nHash];

    if (!block || block->firstElement == 0)
    {

        HashInstanceKeyBlock* newBlock;
        if (block)
        {
            newBlock = (HashInstanceKeyBlock*) new (nothrow) BYTE[HASH_INSTANCE_BLOCK_ALLOC_SIZE];
        }
        else
        {
            // We allocate one big memory chunk that has a block for every index of the hash table to
            // improve data locality and reduce the number of allocs. In most cases, a hash bucket will
            // use only one block, so improving data locality across blocks (i.e. keeping the buckets of the
            // hash table together) should help.
            newBlock = (HashInstanceKeyBlock*)
                ClrVirtualAlloc(NULL, HASH_INSTANCE_BLOCK_ALLOC_SIZE*ARRAY_SIZE(m_hash), MEM_COMMIT, PAGE_READWRITE);
        }
        if (!newBlock)
        {
            return NULL;
        }
        if (block)
        {
            // We add the newest block to the start of the list assuming that most accesses are for
            // recently added elements.
            newBlock->next = block;
            m_hash[nHash] = newBlock; // The previously allocated block
            newBlock->firstElement = HASH_INSTANCE_BLOCK_NUM_ELEMENTS;
            block = newBlock;
        }
        else
        {
            for (DWORD j = 0; j < ARRAY_SIZE(m_hash); j++)
            {
                m_hash[j] = newBlock;
                newBlock->next = NULL; // The previously allocated block
                newBlock->firstElement = HASH_INSTANCE_BLOCK_NUM_ELEMENTS;
                newBlock = (HashInstanceKeyBlock*) (((BYTE*) newBlock) + HASH_INSTANCE_BLOCK_ALLOC_SIZE);
            }
            block = m_hash[nHash];
        }
    }
    _ASSERTE(block->firstElement > 0);
    block->firstElement--;
    block->instanceKeys[block->firstElement].addr = inst->addr;
    block->instanceKeys[block->firstElement].instance = inst;

    inst->next = NULL;
    return inst;
}
#else //DAC_HASHTABLE
DAC_INSTANCE*
DacInstanceManager::Add(DAC_INSTANCE* inst)
{
    _ASSERTE(inst != NULL);
#ifdef _DEBUG
    bool isInserted = (m_hash.find(inst->addr) == m_hash.end());
#endif //_DEBUG
    DAC_INSTANCE *(&target) = m_hash[inst->addr];
    _ASSERTE(!isInserted || target == NULL);
    if( target != NULL )
    {
        //This is necessary to preserve the semantics of Supersede, however, it
        //is more or less dead code.
        inst->next = target;
        target = inst;

        //verify descending order
        _ASSERTE(inst->size >= target->size);
    }
    else
    {
        target = inst;
    }

    return inst;
}

#endif // #if defined(DAC_HASHTABLE)


DAC_INSTANCE*
DacInstanceManager::Alloc(TADDR addr, ULONG32 size, DAC_USAGE_TYPE usage)
{
    SUPPORTS_DAC_HOST_ONLY;
    DAC_INSTANCE_BLOCK* block;
    DAC_INSTANCE* inst;
    ULONG32 fullSize;

    static_assert_no_msg(sizeof(DAC_INSTANCE_BLOCK) <= DAC_INSTANCE_ALIGN);
    static_assert_no_msg((sizeof(DAC_INSTANCE) & (DAC_INSTANCE_ALIGN - 1)) == 0);

    //
    // All allocated instances must be kept alive as long
    // as anybody may have a host pointer for one of them.
    // This means that we cannot delete an arbitrary instance
    // unless we are sure no pointers exist, which currently
    // is not possible to determine, thus we just hold everything
    // until a Flush.  This greatly simplifies instance allocation
    // as we can then just sweep through large blocks rather
    // than having to use a real allocator.  The only
    // complication is that we need to keep all instance
    // data aligned.  We have guaranteed that the header will
    // preserve alignment of the data following if the header
    // is aligned, so as long as we round up all allocations
    // to a multiple of the alignment size everything just works.
    //

    fullSize = (size + DAC_INSTANCE_ALIGN - 1) & ~(DAC_INSTANCE_ALIGN - 1);
    _ASSERTE(fullSize && fullSize <= 0xffffffff - 2 * sizeof(*inst));
    fullSize += sizeof(*inst);

    //
    // Check for an existing block with space.
    //

    for (block = m_blocks; block; block = block->next)
    {
        if (fullSize <= block->bytesFree)
        {
            break;
        }
    }

    if (!block)
    {
        //
        // No existing block has enough space, so allocate a new
        // one if necessary and link it in.  We know we're allocating large
        // blocks so directly VirtualAlloc.  We save one block through a
        // flush so that we spend less time allocating/deallocating.
        //

        ULONG32 blockSize = fullSize + DAC_INSTANCE_ALIGN;
        if (blockSize < DAC_INSTANCE_BLOCK_ALLOCATION)
        {
            blockSize = DAC_INSTANCE_BLOCK_ALLOCATION;
        }

        // If we have a saved block and it's large enough, use it.
        block = m_unusedBlock;
        if ((block != NULL) &&
            ((block->bytesUsed + block->bytesFree) >= blockSize))
        {
            m_unusedBlock = NULL;

            // Right now, we're locked to DAC_INSTANCE_BLOCK_ALLOCATION but
            // that might change in the future if we decide to do something
            // else with the size guarantee in code:DacInstanceManager::FreeAllBlocks
            blockSize = block->bytesUsed + block->bytesFree;
        }
        else
        {
             block = (DAC_INSTANCE_BLOCK*)
                ClrVirtualAlloc(NULL, blockSize, MEM_COMMIT, PAGE_READWRITE);
        }

        if (!block)
        {
            return NULL;
        }

        // Keep the first aligned unit for the block header.
        block->bytesUsed = DAC_INSTANCE_ALIGN;
        block->bytesFree = blockSize - DAC_INSTANCE_ALIGN;

        block->next = m_blocks;
        m_blocks = block;

        m_blockMemUsage += blockSize;
    }

    inst = (DAC_INSTANCE*)((PBYTE)block + block->bytesUsed);
    block->bytesUsed += fullSize;
    _ASSERTE(block->bytesFree >= fullSize);
    block->bytesFree -= fullSize;

    inst->next = NULL;
    inst->addr = addr;
    inst->size = size;
    inst->sig = DAC_INSTANCE_SIG;
    inst->usage = usage;
    inst->enumMem = 0;
    inst->MDEnumed = 0;

    m_numInst++;
    m_instMemUsage += fullSize;
    return inst;
}

void
DacInstanceManager::ReturnAlloc(DAC_INSTANCE* inst)
{
    SUPPORTS_DAC_HOST_ONLY;
    DAC_INSTANCE_BLOCK* block;
    DAC_INSTANCE_BLOCK * pPrevBlock;
    ULONG32 fullSize;

    //
    // This special routine handles cleanup in
    // cases where an instances has been allocated
    // but must be returned due to a following error.
    // The given instance must be the last instance
    // in an existing block.
    //

    fullSize =
        ((inst->size + DAC_INSTANCE_ALIGN - 1) & ~(DAC_INSTANCE_ALIGN - 1)) +
        sizeof(*inst);

    pPrevBlock = NULL;
    for (block = m_blocks; block; pPrevBlock = block, block = block->next)
    {
        if ((PBYTE)inst == (PBYTE)block + (block->bytesUsed - fullSize))
        {
            break;
        }
    }

    if (!block)
    {
        return;
    }

    block->bytesUsed -= fullSize;
    block->bytesFree += fullSize;
    m_numInst--;
    m_instMemUsage -= fullSize;

    // If the block is empty after returning the specified instance, that means this block was newly created
    // when this instance was allocated.  We have seen cases where we are asked to allocate a
    // large chunk of memory only to fail to read the memory from a dump later on, i.e. when both the target
    // address and the size are invalid.  If we keep the allocation, we'll grow the VM size unnecessarily.
    // Thus, release a block if it's empty and if it's not the default size (to avoid thrashing memory).
    // See Dev10 Dbug 812112 for more information.
    if ((block->bytesUsed == DAC_INSTANCE_ALIGN) &&
        ((block->bytesFree + block->bytesUsed) != DAC_INSTANCE_BLOCK_ALLOCATION))
    {
        // The empty block is at the beginning of the list.
        if (pPrevBlock == NULL)
        {
            m_blocks = block->next;
        }
        else
        {
            _ASSERTE(pPrevBlock->next == block);
            pPrevBlock->next = block->next;
        }
        ClrVirtualFree(block, 0, MEM_RELEASE);
    }
}


#if defined(DAC_HASHTABLE)
DAC_INSTANCE*
DacInstanceManager::Find(TADDR addr)
{

#if defined(DAC_MEASURE_PERF)
    unsigned _int64 nStart, nEnd;
    g_nFindCalls++;
    nStart = GetCycleCount();
#endif // #if defined(DAC_MEASURE_PERF)

    HashInstanceKeyBlock* block = m_hash[DAC_INSTANCE_HASH(addr)];

#if defined(DAC_MEASURE_PERF)
    nEnd = GetCycleCount();
    g_nFindHashTotalTime += nEnd - nStart;
#endif // #if defined(DAC_MEASURE_PERF)

    while (block)
    {
        DWORD nIndex = block->firstElement;
        for (; nIndex < HASH_INSTANCE_BLOCK_NUM_ELEMENTS; nIndex++)
        {
            if (block->instanceKeys[nIndex].addr == addr)
            {
 #if defined(DAC_MEASURE_PERF)
                nEnd = GetCycleCount();
                g_nFindHits++;
                g_nFindTotalTime += nEnd - nStart;
                if (g_nStackWalk) g_nFindStackTotalTime += nEnd - nStart;
#endif // #if defined(DAC_MEASURE_PERF)

                DAC_INSTANCE* inst = block->instanceKeys[nIndex].instance;

                // inst should not be NULL even if the address was superseded. We search
                // the entries in the reverse order they were added. So we should have
                // found the superseding entry before this one. (Of course, if a NULL instance
                // has been added, this assert is meaningless. DacInstanceManager::Add
                // asserts that NULL instances aren't added.)

                _ASSERTE(inst != NULL);

                return inst;
            }
        }
        block = block->next;
    }

#if defined(DAC_MEASURE_PERF)
    nEnd = GetCycleCount();
    g_nFindFails++;
    g_nFindTotalTime += nEnd - nStart;
    if (g_nStackWalk) g_nFindStackTotalTime += nEnd - nStart;
#endif // #if defined(DAC_MEASURE_PERF)

    return NULL;
}
#else //DAC_HASHTABLE
DAC_INSTANCE*
DacInstanceManager::Find(TADDR addr)
{
    DacInstanceHashIterator iter = m_hash.find(addr);
    if( iter == m_hash.end() )
    {
        return NULL;
    }
    else
    {
        return iter->second;
    }
}
#endif // if defined(DAC_HASHTABLE)

HRESULT
DacInstanceManager::Write(DAC_INSTANCE* inst, bool throwEx)
{
    HRESULT status;

    if (inst->usage == DAC_VPTR)
    {
        // Skip over the host-side vtable pointer when
        // writing back.
        status = DacWriteAll(inst->addr + sizeof(TADDR),
                             (PBYTE)(inst + 1) + sizeof(PVOID),
                             inst->size - sizeof(TADDR),
                             throwEx);
    }
    else
    {
        // Write the whole instance back.
        status = DacWriteAll(inst->addr, inst + 1, inst->size, throwEx);
    }

    return status;
}

#if defined(DAC_HASHTABLE)
void
DacInstanceManager::Supersede(DAC_INSTANCE* inst)
{
    _ASSERTE(inst != NULL);

    //
    // This instance has been superseded by a larger
    // one and so must be removed from the hash.  However,
    // code may be holding the instance pointer so it
    // can't just be deleted.  Put it on a list for
    // later cleanup.
    //

    HashInstanceKeyBlock* block = m_hash[DAC_INSTANCE_HASH(inst->addr)];
    while (block)
    {
        DWORD nIndex = block->firstElement;
        for (; nIndex < HASH_INSTANCE_BLOCK_NUM_ELEMENTS; nIndex++)
        {
            if (block->instanceKeys[nIndex].instance == inst)
            {
                block->instanceKeys[nIndex].instance = NULL;
                break;
            }
        }
        if (nIndex < HASH_INSTANCE_BLOCK_NUM_ELEMENTS)
        {
            break;
        }
        block = block->next;
    }

    AddSuperseded(inst);
}
#else //DAC_HASHTABLE
void
DacInstanceManager::Supersede(DAC_INSTANCE* inst)
{
    _ASSERTE(inst != NULL);

    //
    // This instance has been superseded by a larger
    // one and so must be removed from the hash.  However,
    // code may be holding the instance pointer so it
    // can't just be deleted.  Put it on a list for
    // later cleanup.
    //

    DacInstanceHashIterator iter = m_hash.find(inst->addr);
    if( iter == m_hash.end() )
        return;

    DAC_INSTANCE** bucket = &(iter->second);
    DAC_INSTANCE* cur = *bucket;
    DAC_INSTANCE* prev = NULL;
    //walk through the chain looking for this particular instance
    while (cur)
    {
        if (cur == inst)
        {
            if (!prev)
            {
                *bucket = inst->next;
            }
            else
            {
                prev->next = inst->next;
            }
            break;
        }

        prev = cur;
        cur = cur->next;
    }

    AddSuperseded(inst);
}
#endif // if defined(DAC_HASHTABLE)

// This is the default Flush() called when the DAC cache is invalidated,
// e.g. when we continue the debuggee process.  In this case, we want to
// save one block of memory to avoid thrashing.  See the usage of m_unusedBlock
// for more information.
void DacInstanceManager::Flush(void)
{
    Flush(true);
}

void DacInstanceManager::Flush(bool fSaveBlock)
{
    SUPPORTS_DAC_HOST_ONLY;

    //
    // All allocated memory is in the block
    // list, so just free the blocks and
    // forget all the internal pointers.
    //

    while (true)
    {
        FreeAllBlocks(fSaveBlock);

        DAC_INSTANCE_PUSH* push = m_instPushed;
        if (!push)
        {
            break;
        }

        m_instPushed = push->next;
        m_blocks = push->blocks;
        delete push;
    }

    // If we are not saving any memory blocks, then clear the saved buffer block (if any) as well.
    if (!fSaveBlock)
    {
        if (m_unusedBlock != NULL)
        {
            ClrVirtualFree(m_unusedBlock, 0, MEM_RELEASE);
            m_unusedBlock = NULL;
        }
    }

#if defined(DAC_HASHTABLE)
    for (int i = STRING_LENGTH(m_hash); i >= 0; i--)
    {
        HashInstanceKeyBlock* block = m_hash[i];
        HashInstanceKeyBlock* next;
        while (block)
        {
            next = block->next;
            if (next)
            {
                delete [] block;
            }
            else if (i == 0)
            {
                ClrVirtualFree(block, 0, MEM_RELEASE);
            }
            block = next;
        }
    }
#else //DAC_HASHTABLE
    m_hash.clear();
#endif //DAC_HASHTABLE

    InitEmpty();
}

#if defined(DAC_HASHTABLE)
void
DacInstanceManager::ClearEnumMemMarker(void)
{
    ULONG i;
    DAC_INSTANCE* inst;

    for (i = 0; i < ARRAY_SIZE(m_hash); i++)
    {
        HashInstanceKeyBlock* block = m_hash[i];
        while (block)
        {
            DWORD j;
            for (j = block->firstElement; j < HASH_INSTANCE_BLOCK_NUM_ELEMENTS; j++)
            {
                inst = block->instanceKeys[j].instance;
                if (inst != NULL)
                {
                    inst->enumMem = 0;
                }
            }
            block = block->next;
        }
    }
    for (inst = m_superseded; inst; inst = inst->next)
    {
        inst->enumMem = 0;
    }
}
#else //DAC_HASHTABLE
void
DacInstanceManager::ClearEnumMemMarker(void)
{
    ULONG i;
    DAC_INSTANCE* inst;

    DacInstanceHashIterator end = m_hash.end();
    /* REVISIT_TODO Fri 10/20/2006
     * This might have an issue, since it might miss chained entries off of
     * ->next.  However, ->next is going away, and for all intents and
     *  purposes, this never happens.
*/
    for( DacInstanceHashIterator cur = m_hash.begin(); cur != end; ++cur )
    {
        cur->second->enumMem = 0;
    }

    for (inst = m_superseded; inst; inst = inst->next)
    {
        inst->enumMem = 0;
    }
}
#endif // if defined(DAC_HASHTABLE)


#if defined(DAC_HASHTABLE)
//
//
// Iterating through all of the hash entry and report the memory
// instance to minidump
//
// This function returns the total number of bytes that it reported.
//
//
UINT
DacInstanceManager::DumpAllInstances(
    ICLRDataEnumMemoryRegionsCallback *pCallBack)       // memory report call back
{
    ULONG           i;
    DAC_INSTANCE*   inst;
    UINT            cbTotal = 0;

#if defined(DAC_MEASURE_PERF)
   FILE* fp = fopen("c:\\dumpLog.txt", "a");
   int total = 0;
#endif // #if defined(DAC_MEASURE_PERF)

    for (i = 0; i < ARRAY_SIZE(m_hash); i++)
    {

#if defined(DAC_MEASURE_PERF)
      int numInBucket = 0;
#endif // #if defined(DAC_MEASURE_PERF)

        HashInstanceKeyBlock* block = m_hash[i];
        while (block)
        {
            DWORD j;
            for (j = block->firstElement; j < HASH_INSTANCE_BLOCK_NUM_ELEMENTS; j++)
            {
                inst = block->instanceKeys[j].instance;

                // Only report those we intended to.
                // So far, only metadata is excluded!
                //
                if (inst && inst->noReport == 0)
                {
                    cbTotal += inst->size;
                    HRESULT hr = pCallBack->EnumMemoryRegion(TO_CDADDR(inst->addr), inst->size);
                    if (hr == COR_E_OPERATIONCANCELED)
                    {
                        ThrowHR(hr);
                    }
                }

#if defined(DAC_MEASURE_PERF)
                if (inst)
                {
                    numInBucket++;
                }
#endif // #if defined(DAC_MEASURE_PERF)
            }
            block = block->next;
        }

 #if defined(DAC_MEASURE_PERF)
      fprintf(fp, "%4d: %4d%s", i, numInBucket, (i+1)%5?  ";  " : "\n");
        total += numInBucket;
#endif // #if defined(DAC_MEASURE_PERF)

    }

#if defined(DAC_MEASURE_PERF)
    fprintf(fp, "\n\nTotal entries: %d\n\n", total);
    fclose(fp);
#endif // #if defined(DAC_MEASURE_PERF)

    return cbTotal;

}
#else //DAC_HASHTABLE
//
//
// Iterating through all of the hash entry and report the memory
// instance to minidump
//
// This function returns the total number of bytes that it reported.
//
//
UINT
DacInstanceManager::DumpAllInstances(
    ICLRDataEnumMemoryRegionsCallback *pCallBack)       // memory report call back
{
    SUPPORTS_DAC_HOST_ONLY;

    DAC_INSTANCE*   inst;
    UINT            cbTotal = 0;

#if defined(DAC_MEASURE_PERF)
   FILE* fp = fopen("c:\\dumpLog.txt", "a");
#endif // #if defined(DAC_MEASURE_PERF)

#if defined(DAC_MEASURE_PERF)
   int numInBucket = 0;
#endif // #if defined(DAC_MEASURE_PERF)

   DacInstanceHashIterator end = m_hash.end();
   for (DacInstanceHashIterator cur = m_hash.begin(); end != cur; ++cur)
   {
       inst = cur->second;

       // Only report those we intended to.
       // So far, only metadata is excluded!
       //
       if (inst->noReport == 0)
       {
           cbTotal += inst->size;
           HRESULT hr = pCallBack->EnumMemoryRegion(TO_CDADDR(inst->addr), inst->size);
           if (hr == COR_E_OPERATIONCANCELED)
           {
               ThrowHR(hr);
           }
       }

#if defined(DAC_MEASURE_PERF)
       numInBucket++;
#endif // #if defined(DAC_MEASURE_PERF)
   }

#if defined(DAC_MEASURE_PERF)
    fprintf(fp, "\n\nTotal entries: %d\n\n", numInBucket);
    fclose(fp);
#endif // #if defined(DAC_MEASURE_PERF)

    return cbTotal;

}
#endif // if defined(DAC_HASHTABLE)

DAC_INSTANCE_BLOCK*
DacInstanceManager::FindInstanceBlock(DAC_INSTANCE* inst)
{
    for (DAC_INSTANCE_BLOCK* block = m_blocks; block; block = block->next)
    {
        if ((PBYTE)inst >= (PBYTE)block &&
            (PBYTE)inst < (PBYTE)block + block->bytesUsed)
        {
            return block;
        }
    }

    return NULL;
}

// If fSaveBlock is false, free all blocks of allocated memory.  Otherwise,
// free all blocks except the one we save to avoid thrashing memory.
// Callers very frequently flush repeatedly with little memory needed in DAC
// so this avoids wasteful repeated allocations/deallocations.
// There is a very unlikely case that we'll have allocated an extremely large
// block; if this is the only block we will save none since this block will
// remain allocated.
void
DacInstanceManager::FreeAllBlocks(bool fSaveBlock)
{
    DAC_INSTANCE_BLOCK* block;

    while ((block = m_blocks))
    {
        m_blocks = block->next;

        // If we haven't saved our single block yet and this block is the default size
        // then we will save it instead of freeing it.  This avoids saving an unnecessarily large
        // memory block.
        // Do *NOT* trash the byte counts.  code:DacInstanceManager::Alloc
        // depends on them being correct when checking to see if a block is large enough.
        if (fSaveBlock &&
            (m_unusedBlock == NULL) &&
            ((block->bytesFree + block->bytesUsed) == DAC_INSTANCE_BLOCK_ALLOCATION))
        {
            // Just to avoid confusion, since we're keeping it around.
            block->next = NULL;
            m_unusedBlock = block;
        }
        else
        {
            ClrVirtualFree(block, 0, MEM_RELEASE);
        }
    }
}

//----------------------------------------------------------------------------
//
// DacStreamManager.
//
//----------------------------------------------------------------------------

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

namespace serialization { namespace bin {

    //========================================================================
    // Support functions for binary serialization of simple types to a buffer:
    //   - raw_size() returns the size in bytes of the binary representation
    //                of a value.
    //   - raw_serialize() copies the binary representation of a value into a
    //                buffer.
    //   - raw_deserialize() generates a value from its binary representation
    //                in a buffer.
    // Beyond simple types the APIs below support SString instances. SStrings
    // are stored as UTF8 strings.
    //========================================================================

    static const size_t ErrOverflow = (size_t)(-1);

#ifndef TARGET_UNIX

    // Template class is_blittable
    template <typename _Ty, typename Enable = void>
    struct is_blittable
        : std::false_type
    { // determines whether _Ty is blittable
    };

    template <typename _Ty>
    struct is_blittable<_Ty, typename std::enable_if<std::is_arithmetic<_Ty>::value>::type>
        : std::true_type
    { // determines whether _Ty is blittable
    };

    // allow types to declare themselves blittable by including a static bool
    // member "is_blittable".
    template <typename _Ty>
    struct is_blittable<_Ty, typename std::enable_if<_Ty::is_blittable>::type>
        : std::true_type
    { // determines whether _Ty is blittable
    };


    //========================================================================
    // serialization::bin::Traits<T> enables binary serialization and
    //   deserialization of instances of T.
    //========================================================================

    //
    // General specialization for non-blittable types - must be overridden
    // for each specific non-blittable type.
    //
    template <typename T, typename Enable = void>
    class Traits
    {
    public:
        static FORCEINLINE size_t
        raw_size(const T & val)
        {
            static_assert(false, "Non-blittable types need explicit specializations");
        }
    };

    //
    // General type trait supporting serialization/deserialization of blittable
    // type arguments (as defined by the is_blittable<> type traits above).
    //
    template <typename T>
    class Traits<T, typename std::enable_if<is_blittable<T>::value>::type>
    {
#else // TARGET_UNIX
    template <typename T>
    class Traits
    {
#endif // !TARGET_UNIX
    public:
        //
        // raw_size() returns the size in bytes of the binary representation of a
        //                value.
        //
        static FORCEINLINE size_t
        raw_size(const T & val)
        {
            return sizeof(T);
        }

        //
        // raw_serialize() copies the binary representation of a value into a
        //     "dest" buffer that has "destSize" bytes available.
        // Returns raw_size(val), or ErrOverflow if the buffer does not have
        //     enough space to accommodate "val".
        //
        static FORCEINLINE size_t
        raw_serialize(BYTE* dest, size_t destSize, const T & val)
        {
            size_t cnt = raw_size(val);

            if (destSize < cnt)
            {
                return ErrOverflow;
            }

            memcpy_s(dest, destSize, &val, cnt);

            return cnt;
        }

        //
        // raw_deserialize() generates a value "val" from its binary
        //     representation in a buffer "src".
        // Returns raw_size(val), or ErrOverflow if the buffer does not have
        //     enough space to accommodate "val".
        //
        static FORCEINLINE size_t
        raw_deserialize(T & val, const BYTE* src, size_t srcSize)
        {
            size_t cnt = raw_size(*(T*)src);

            if (srcSize < cnt)
            {
                return ErrOverflow;
            }

            memcpy_s(&val, cnt, src, cnt);

            return cnt;
        }

    };

    //
    // Specialization for UTF8 strings
    //
    template<>
    class Traits<LPCUTF8>
    {
    public:
        static FORCEINLINE size_t
        raw_size(const LPCUTF8 & val)
        {
            return strlen(val) + 1;
        }

        static FORCEINLINE size_t
        raw_serialize(BYTE* dest, size_t destSize, const LPCUTF8 & val)
        {
            size_t cnt = raw_size(val);

            if (destSize < cnt)
            {
                return ErrOverflow;
            }

            memcpy_s(dest, destSize, &val, cnt);

            return cnt;
        }

        static FORCEINLINE size_t
        raw_deserialize(LPCUTF8 & val, const BYTE* src, size_t srcSize)
        {
            size_t cnt = strnlen((LPCUTF8)src, srcSize) + 1;

            // assert we found a NULL terminated string at "src"
            if (srcSize < cnt)
            {
                return ErrOverflow;
            }

            // we won't allocate another buffer for this string
            val = (LPCUTF8)src;

            return cnt;
        }

    };

    //
    // Specialization for SString.
    // SString serialization/deserialization is performed to/from a UTF8
    // string.
    //
    template<>
    class Traits<SString>
    {
    public:
        static FORCEINLINE size_t
        raw_size(const SString & val)
        {
            StackSString s;
            val.ConvertToUTF8(s);
            // make sure to include the NULL terminator
            return s.GetCount() + 1;
        }

        static FORCEINLINE size_t
        raw_serialize(BYTE* dest, size_t destSize, const SString & val)
        {
            // instead of calling raw_size() we inline it here, so we can reuse
            // the UTF8 string obtained below as an argument to memcpy.

            StackSString s;
            val.ConvertToUTF8(s);
            // make sure to include the NULL terminator
            size_t cnt = s.GetCount() + 1;

            if (destSize < cnt)
            {
                return ErrOverflow;
            }

            memcpy_s(dest, destSize, s.GetUTF8(), cnt);

            return cnt;
        }

        static FORCEINLINE size_t
        raw_deserialize(SString & val, const BYTE* src, size_t srcSize)
        {
            size_t cnt = strnlen((LPCUTF8)src, srcSize) + 1;

            // assert we found a NULL terminated string at "src"
            if (srcSize < cnt)
            {
                return ErrOverflow;
            }

            // a literal SString avoids a new allocation + copy
            SString sUtf8(SString::Utf8Literal, (LPCUTF8) src);
            sUtf8.ConvertToUnicode(val);

            return cnt;
        }

    };

#ifndef TARGET_UNIX
    //
    // Specialization for SString-derived classes (like SStrings)
    //
    template<typename T>
    class Traits<T, typename std::enable_if<std::is_base_of<SString, T>::value>::type>
        : public Traits<SString>
    {
    };
#endif // !TARGET_UNIX

    //
    // Convenience functions to allow argument type deduction
    //
    template <typename T> FORCEINLINE
    size_t raw_size(const T & val)
    { return Traits<T>::raw_size(val); }

    template <typename T> FORCEINLINE
    size_t raw_serialize(BYTE* dest, size_t destSize, const T & val)
    { return Traits<T>::raw_serialize(dest, destSize, val); }

    template <typename T> FORCEINLINE
    size_t raw_deserialize(T & val, const BYTE* src, size_t srcSize)
    { return Traits<T>::raw_deserialize(val, src, srcSize); }


    enum StreamBuffState
    {
        sbsOK,
        sbsUnrecoverable,
        sbsOOM = sbsUnrecoverable,
    };

    //
    // OStreamBuff - Manages writing to an output buffer
    //
    class OStreamBuff
    {
    public:
        OStreamBuff(BYTE * _buff, size_t _buffsize)
            : buffsize(_buffsize)
            , buff(_buff)
            , crt(0)
            , sbs(sbsOK)
        { }

        template <typename T>
        OStreamBuff& operator << (const T & val)
        {
            if (sbs >= sbsUnrecoverable)
                return *this;

            size_t cnt = raw_serialize(buff+crt, buffsize-crt, val);
            if (cnt == ErrOverflow)
            {
                sbs = sbsOOM;
            }
            else
            {
                crt += cnt;
            }

            return *this;
        }

        inline size_t GetPos() const
        {
            return crt;
        }

        inline BOOL operator!() const
        {
            return sbs >= sbsUnrecoverable;
        }

        inline StreamBuffState State() const
        {
            return sbs;
        }

    private:
        size_t          buffsize; // size of buffer
        BYTE*           buff;     // buffer to stream to
        size_t          crt;      // current offset in buffer
        StreamBuffState sbs;      // current state
    };


    //
    // OStreamBuff - Manages reading from an input buffer
    //
    class IStreamBuff
    {
    public:
        IStreamBuff(const BYTE* _buff, size_t _buffsize)
            : buffsize(_buffsize)
            , buff(_buff)
            , crt(0)
            , sbs(sbsOK)
        { }

        template <typename T>
        IStreamBuff& operator >> (T & val)
        {
            if (sbs >= sbsUnrecoverable)
                return *this;

            size_t cnt = raw_deserialize(val, buff+crt, buffsize-crt);
            if (cnt == ErrOverflow)
            {
                sbs = sbsOOM;
            }
            else
            {
                crt += cnt;
            }

            return *this;
        }

        inline size_t GetPos() const
        {
            return crt;
        }

        inline BOOL operator!() const
        {
            return sbs >= sbsUnrecoverable;
        }

        inline StreamBuffState State() const
        {
            return sbs;
        }

    private:
        size_t          buffsize; // size of buffer
        const BYTE *    buff;     // buffer to read from
        size_t          crt;      // current offset in buffer
        StreamBuffState sbs;      // current state
    };

} }

using serialization::bin::StreamBuffState;
using serialization::bin::IStreamBuff;
using serialization::bin::OStreamBuff;


// Callback function type used by DacStreamManager to coordinate
// amount of available memory between multiple streamable data
// structures (e.g. DacEENamesStreamable)
typedef bool (*Reserve_Fnptr)(DWORD size, void * writeState);


//
// DacEENamesStreamable
//   Stores EE struct* -> Name mappings and streams them to a
//   streambuf when asked
//
class DacEENamesStreamable
{
private:
    // the hash map storing the interesting mappings of EE* -> Names
    MapSHash< TADDR, SString,
              NoRemoveSHashTraits <
                  NonDacAwareSHashTraits< MapSHashTraits <TADDR, SString> >
            > > m_hash;

    Reserve_Fnptr  m_reserveFn;
    void          *m_writeState;

private:
    // signature value in the header in stream
    static const DWORD sig = 0x614e4545; // "EENa" - EE Name

    // header in stream
    struct StreamHeader
    {
        DWORD sig;        // 0x614e4545 == "EENa"
        DWORD cnt;        // count of entries

        static const bool is_blittable = true;
    };

public:
    DacEENamesStreamable()
        : m_reserveFn(NULL)
        , m_writeState(NULL)
    {}

    // Ensures the instance is ready for caching data and later writing
    // its map entries to an OStreamBuff.
    bool PrepareStreamForWriting(Reserve_Fnptr pfn, void * writeState)
    {
        _ASSERTE(pfn != NULL && writeState != NULL);
        m_reserveFn = pfn;
        m_writeState = writeState;

        DWORD size = (DWORD) sizeof(StreamHeader);

        // notify owner to reserve space for a StreamHeader
        return m_reserveFn(size, m_writeState);
    }

    // Adds a new mapping from an EE struct pointer (e.g. MethodDesc*) to
    // its name
    bool AddEEName(TADDR taEE, const SString & eeName)
    {
        _ASSERTE(m_reserveFn != NULL && m_writeState != NULL);

        // as a micro-optimization convert to Utf8 here as both raw_size and
        // raw_serialize are optimized for Utf8...
        StackSString seeName;
        eeName.ConvertToUTF8(seeName);

        DWORD size = (DWORD)(serialization::bin::raw_size(taEE) +
                             serialization::bin::raw_size(seeName));

        // notify owner of the amount of space needed in the buffer
        if (m_reserveFn(size, m_writeState))
        {
            // if there's still space cache the entry in m_hash
            m_hash.AddOrReplace(KeyValuePair<TADDR, SString>(taEE, seeName));
            return true;
        }
        else
        {
            return false;
        }
    }

    // Finds an EE name from a target address of an EE struct (e.g.
    // MethodDesc*)
    bool FindEEName(TADDR taEE, SString & eeName) const
    {
        return m_hash.Lookup(taEE, &eeName) == TRUE;
    }

    void Clear()
    {
        m_hash.RemoveAll();
    }

    // Writes a header and the hash entries to an OStreamBuff
    HRESULT StreamTo(OStreamBuff &out) const
    {
        StreamHeader hdr;
        hdr.sig = sig;
        hdr.cnt = (DWORD) m_hash.GetCount();

        out << hdr;

        auto end = m_hash.End();
        for (auto cur = m_hash.Begin(); end != cur; ++cur)
        {
            out << cur->Key() << cur->Value();
            if (!out)
                return E_FAIL;
        }

        return S_OK;
    }

    // Reads a header and the hash entries from an IStreamBuff
    HRESULT StreamFrom(IStreamBuff &in)
    {
        StreamHeader hdr;

        in >> hdr; // in >> hdr.sig >> hdr.cnt;

        if (hdr.sig != sig)
            return E_FAIL;

        for (size_t i = 0; i < hdr.cnt; ++i)
        {
            TADDR taEE;
            SString eeName;
            in >> taEE >> eeName;

            if (!in)
                return E_FAIL;

            m_hash.AddOrReplace(KeyValuePair<TADDR, SString>(taEE, eeName));
        }

        return S_OK;
    }

};

//================================================================================
// This class enables two scenarios:
//   1. When debugging a triage/mini-dump the class is initialized with a valid
//      buffer in taMiniMetaDataBuff. Afterwards one can call MdCacheGetEEName to
//      retrieve the name associated with a MethodDesc*.
//   2. When generating a dump one must follow this sequence:
//      a. Initialize the DacStreamManager passing a valid (if the current
//         debugging target is a triage/mini-dump) or empty buffer (if the
//         current target is a live processa full or a heap dump)
//      b. Call PrepareStreamsForWriting() before starting enumerating any memory
//      c. Call MdCacheAddEEName() anytime we enumerate an EE structure of interest
//      d. Call EnumStreams() as the last action in the memory enumeration method.
//
class DacStreamManager
{
public:
    enum eReadOrWrite
    {
        eNone,    // the stream doesn't exist (target is a live process/full/heap dump)
        eRO,      // the stream exists and we've read it (target is triage/mini-dump)
        eWO,      // the stream doesn't exist but we're creating it
                  // (e.g. to save a minidump from the current debugging session)
        eRW       // the stream exists but we're generating another triage/mini-dump
    };

    static const DWORD sig = 0x6d727473;        // 'strm'

    struct StreamsHeader
    {
        DWORD dwSig;        // 0x6d727473 == "strm"
        DWORD dwTotalSize;  // total size in bytes
        DWORD dwCntStreams; // number of streams (currently 1)

        static const bool is_blittable = true;
    };

    DacStreamManager(TADDR miniMetaDataBuffAddress, DWORD miniMetaDataBuffSizeMax)
        : m_MiniMetaDataBuffAddress(miniMetaDataBuffAddress)
        , m_MiniMetaDataBuffSizeMax(miniMetaDataBuffSizeMax)
        , m_rawBuffer(NULL)
        , m_cbAvailBuff(0)
        , m_rw(eNone)
        , m_bStreamsRead(FALSE)
        , m_EENames()
    {
        Initialize();
    }

    ~DacStreamManager()
    {
        if (m_rawBuffer != NULL)
        {
            delete [] m_rawBuffer;
        }
    }

    bool PrepareStreamsForWriting()
    {
        if (m_rw == eNone)
            m_rw = eWO;
        else if (m_rw == eRO)
            m_rw = eRW;
        else if (m_rw == eRW)
            /* nothing */;
        else // m_rw == eWO
        {
            // this is a second invocation from a possibly live process
            // clean up the map since the callstacks/exceptions may be different
            m_EENames.Clear();
        }

        // update available count based on the header and footer sizes
        if (m_MiniMetaDataBuffSizeMax < sizeof(StreamsHeader))
            return false;

        m_cbAvailBuff = m_MiniMetaDataBuffSizeMax - sizeof(StreamsHeader);

        // update available count based on each stream's initial needs
        if (!m_EENames.PrepareStreamForWriting(&ReserveInBuffer, this))
            return false;

        return true;
    }

    bool MdCacheAddEEName(TADDR taEEStruct, const SString& name)
    {
        // don't cache unless we enabled "W"riting from a target that does not
        // already have a stream yet
        if (m_rw != eWO)
            return false;

        m_EENames.AddEEName(taEEStruct, name);
        return true;
    }

    HRESULT EnumStreams(IN CLRDataEnumMemoryFlags flags)
    {
        _ASSERTE(flags == CLRDATA_ENUM_MEM_MINI || flags == CLRDATA_ENUM_MEM_TRIAGE);
        _ASSERTE(m_rw == eWO || m_rw == eRW);

        DWORD cbWritten = 0;

        if (m_rw == eWO)
        {
            // only dump the stream is it wasn't already present in the target
            DumpAllStreams(&cbWritten);
        }
        else
        {
            cbWritten = m_MiniMetaDataBuffSizeMax;
        }

        DacEnumMemoryRegion(m_MiniMetaDataBuffAddress, cbWritten, false);
        DacUpdateMemoryRegion(m_MiniMetaDataBuffAddress, cbWritten, m_rawBuffer);

        return S_OK;
    }

    bool MdCacheGetEEName(TADDR taEEStruct, SString & eeName)
    {
        if (!m_bStreamsRead)
        {
            ReadAllStreams();
        }

        if (m_rw == eNone || m_rw == eWO)
        {
            return false;
        }

        return m_EENames.FindEEName(taEEStruct, eeName);
    }

private:
    HRESULT Initialize()
    {
        _ASSERTE(m_rw == eNone);
        _ASSERTE(m_rawBuffer == NULL);

        HRESULT hr = S_OK;

        StreamsHeader hdr;
        DacReadAll(dac_cast<TADDR>(m_MiniMetaDataBuffAddress),
                   &hdr, sizeof(hdr), true);

        // when the DAC looks at a triage dump or minidump generated using
        // a "minimetadata" enabled DAC, buff will point to a serialized
        // representation of a methoddesc->method name hashmap.
        if (hdr.dwSig == sig)
        {
            m_rw = eRO;
            m_MiniMetaDataBuffSizeMax = hdr.dwTotalSize;
            hr = S_OK;
        }
        else
        // when the DAC initializes this for the case where the target is
        // (a) a live process, or (b) a full dump, buff will point to a
        // zero initialized memory region (allocated w/ VirtualAlloc)
        if (hdr.dwSig == 0 && hdr.dwTotalSize == 0 && hdr.dwCntStreams == 0)
        {
            hr = S_OK;
        }
        // otherwise we may have some memory corruption. treat this as
        // a liveprocess/full dump
        else
        {
            hr = S_FALSE;
        }

        BYTE * buff = new BYTE[m_MiniMetaDataBuffSizeMax];
        DacReadAll(dac_cast<TADDR>(m_MiniMetaDataBuffAddress),
                   buff, m_MiniMetaDataBuffSizeMax, true);

        m_rawBuffer = buff;

        return hr;
    }

    HRESULT DumpAllStreams(DWORD * pcbWritten)
    {
        _ASSERTE(m_rw == eWO);

        HRESULT hr = S_OK;

        OStreamBuff out(m_rawBuffer, m_MiniMetaDataBuffSizeMax);

        // write header
        StreamsHeader hdr;
        hdr.dwSig = sig;
        hdr.dwTotalSize = m_MiniMetaDataBuffSizeMax-m_cbAvailBuff; // will update
        hdr.dwCntStreams = 1;

        out << hdr;

        // write MethodDesc->Method name map
        hr = m_EENames.StreamTo(out);

        // wrap up the buffer whether we ecountered an error or not
        size_t cbWritten = out.GetPos();
        cbWritten = ALIGN_UP(cbWritten, sizeof(size_t));

        // patch the dwTotalSize field blitted at the beginning of the buffer
        ((StreamsHeader*)m_rawBuffer)->dwTotalSize = (DWORD) cbWritten;

        if (pcbWritten)
            *pcbWritten = (DWORD) cbWritten;

        return hr;
    }

    HRESULT ReadAllStreams()
    {
        _ASSERTE(!m_bStreamsRead);

        if (m_rw == eNone || m_rw == eWO)
        {
            // no streams to read...
            m_bStreamsRead = TRUE;
            return S_FALSE;
        }

        HRESULT hr = S_OK;

        IStreamBuff in(m_rawBuffer, m_MiniMetaDataBuffSizeMax);

        // read header
        StreamsHeader hdr;
        in >> hdr;
        _ASSERTE(hdr.dwSig == sig);
        _ASSERTE(hdr.dwCntStreams == 1);

        // read EE struct pointer -> EE name map
        m_EENames.Clear();
        hr = m_EENames.StreamFrom(in);

        m_bStreamsRead = TRUE;

        return hr;
    }

    static bool ReserveInBuffer(DWORD size, void * writeState)
    {
        DacStreamManager * pThis = reinterpret_cast<DacStreamManager*>(writeState);
        if (size > pThis->m_cbAvailBuff)
        {
            return false;
        }
        else
        {
            pThis->m_cbAvailBuff -= size;
            return true;
        }
    }

private:
    TADDR                 m_MiniMetaDataBuffAddress;    // TADDR of the buffer
    DWORD                 m_MiniMetaDataBuffSizeMax;    // max size of buffer
    BYTE                * m_rawBuffer;                  // inproc copy of buffer
    DWORD                 m_cbAvailBuff;                // available bytes in buffer
    eReadOrWrite          m_rw;
    BOOL                  m_bStreamsRead;
    DacEENamesStreamable  m_EENames;
};

#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

//----------------------------------------------------------------------------
//
// ClrDataAccess.
//
//----------------------------------------------------------------------------

ClrDataAccess::ClrDataAccess(ICorDebugDataTarget * pTarget, ICLRDataTarget * pLegacyTarget/*=0*/)
{
    SUPPORTS_DAC_HOST_ONLY;     // ctor does no marshalling - don't check with DacCop

    /*
     *  Stash the various forms of the new ICorDebugDataTarget interface
     */
    m_pTarget = pTarget;
    m_pTarget->AddRef();

    HRESULT hr;

    hr = m_pTarget->QueryInterface(__uuidof(ICorDebugMutableDataTarget),
                                (void**)&m_pMutableTarget);

    if (hr != S_OK)
    {
        // Create a target which always fails the write requests with CORDBG_E_TARGET_READONLY
        m_pMutableTarget = new ReadOnlyDataTargetFacade();
        m_pMutableTarget->AddRef();
    }

    /*
     * If we have a legacy target, it means we're providing compatibility for code that used
     * the old ICLRDataTarget interfaces.  There are still a few things (like metadata location,
     * GetImageBase, and VirtualAlloc) that the implementation may use which we haven't superseded
     * in ICorDebugDataTarget, so we still need access to the old target interfaces.
     * Any functionality that does exist in ICorDebugDataTarget is accessed from that interface
     * using the DataTargetAdapter on top of the legacy interface (to unify the calling code).
     * Eventually we may expose all functionality we need using ICorDebug (possibly a private
     * interface for things like VirtualAlloc), at which point we can stop using the legacy interfaces
     * completely (except in the DataTargetAdapter).
     */
    m_pLegacyTarget = NULL;
    m_pLegacyTarget2 = NULL;
    m_pLegacyTarget3 = NULL;
    m_legacyMetaDataLocator = NULL;
    m_target3 = NULL;
    if (pLegacyTarget != NULL)
    {
        m_pLegacyTarget = pLegacyTarget;

        m_pLegacyTarget->AddRef();

        m_pLegacyTarget->QueryInterface(__uuidof(ICLRDataTarget2), (void**)&m_pLegacyTarget2);

        m_pLegacyTarget->QueryInterface(__uuidof(ICLRDataTarget3), (void**)&m_pLegacyTarget3);

        if (pLegacyTarget->QueryInterface(__uuidof(ICLRMetadataLocator),
                                 (void**)&m_legacyMetaDataLocator) != S_OK)
        {
            // The debugger doesn't implement IMetadataLocator. Use
            // IXCLRDataTarget3 if that exists.  Otherwise we don't need it.
            pLegacyTarget->QueryInterface(__uuidof(IXCLRDataTarget3),
                                         (void**)&m_target3);
        }
    }

    m_globalBase = 0;
    m_refs = 1;
    m_instanceAge = 0;
    m_debugMode = GetEnvironmentVariableA("MSCORDACWKS_DEBUG", NULL, 0) != 0;

    m_enumMemCb = NULL;
    m_updateMemCb = NULL;
    m_logMessageCb = NULL;
    m_enumMemFlags = (CLRDataEnumMemoryFlags)-1;    // invalid
    m_jitNotificationTable = NULL;
    m_gcNotificationTable  = NULL;

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
    m_streams = NULL;
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

    // Target consistency checks are disabled by default.
    // See code:ClrDataAccess::SetTargetConsistencyChecks for details.
    m_fEnableTargetConsistencyAsserts = false;

#ifdef _DEBUG
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgDACEnableAssert))
    {
        m_fEnableTargetConsistencyAsserts = true;
    }

    // Verification asserts are disabled by default because some debuggers (cdb/windbg) probe likely locations
    // for DAC and having this assert pop up all the time can be annoying.  We let derived classes enable
    // this if they want.  It can also be overridden at run-time with DOTNET_DbgDACAssertOnMismatch,
    // see ClrDataAccess::VerifyDlls for details.
    m_fEnableDllVerificationAsserts = false;
#endif

}

ClrDataAccess::~ClrDataAccess(void)
{
    SUPPORTS_DAC_HOST_ONLY;

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
    if (m_streams)
    {
        delete m_streams;
    }
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

    delete [] m_jitNotificationTable;
    if (m_pLegacyTarget)
    {
        m_pLegacyTarget->Release();
    }
    if (m_pLegacyTarget2)
    {
        m_pLegacyTarget2->Release();
    }
    if (m_pLegacyTarget3)
    {
        m_pLegacyTarget3->Release();
    }
    if (m_legacyMetaDataLocator)
    {
        m_legacyMetaDataLocator->Release();
    }
    if (m_target3)
    {
        m_target3->Release();
    }
    m_pTarget->Release();
    m_pMutableTarget->Release();
}

STDMETHODIMP
ClrDataAccess::QueryInterface(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface)
{
    void* ifaceRet;

    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataProcess)) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataProcess2)))
    {
        ifaceRet = static_cast<IXCLRDataProcess2*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ICLRDataEnumMemoryRegions)))
    {
        ifaceRet = static_cast<ICLRDataEnumMemoryRegions*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface)))
    {
        ifaceRet = static_cast<ISOSDacInterface*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface2)))
    {
        ifaceRet = static_cast<ISOSDacInterface2*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface3)))
    {
        ifaceRet = static_cast<ISOSDacInterface3*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface4)))
    {
        ifaceRet = static_cast<ISOSDacInterface4*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface5)))
    {
        ifaceRet = static_cast<ISOSDacInterface5*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface6)))
    {
        ifaceRet = static_cast<ISOSDacInterface6*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface7)))
    {
        ifaceRet = static_cast<ISOSDacInterface7*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface8)))
    {
        ifaceRet = static_cast<ISOSDacInterface8*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface9)))
    {
        ifaceRet = static_cast<ISOSDacInterface9*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface10)))
    {
        ifaceRet = static_cast<ISOSDacInterface10*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface11)))
    {
        ifaceRet = static_cast<ISOSDacInterface11*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface12)))
    {
        ifaceRet = static_cast<ISOSDacInterface12*>(this);
    }
    else if (IsEqualIID(interfaceId, __uuidof(ISOSDacInterface13)))
    {
        ifaceRet = static_cast<ISOSDacInterface13*>(this);
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    *iface = ifaceRet;
    return S_OK;
}

STDMETHODIMP_(ULONG)
ClrDataAccess::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataAccess::Release(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::Flush(void)
{
    SUPPORTS_DAC_HOST_ONLY;

    //
    // Free MD import objects.
    //
    m_mdImports.Flush();

    // Free instance memory.
    m_instances.Flush();

    // When the host instance cache is flushed we
    // update the instance age count so that
    // all child objects automatically become
    // invalid.  This prevents them from using
    // any pointers they've kept to host instances
    // which are now gone.
    m_instanceAge++;

    return S_OK;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::StartEnumTasks(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        if (ThreadStore::s_pThreadStore)
        {
            Thread* thread = ThreadStore::GetAllThreadList(NULL, 0, 0);
            *handle = TO_CDENUM(thread);
            status = *handle ? S_OK : S_FALSE;
        }
        else
        {
            status = S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EnumTask(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataTask **task)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        if (*handle)
        {
            Thread* thread = FROM_CDENUM(Thread, *handle);
            *task = new (nothrow) ClrDataTask(this, thread);
            if (*task)
            {
                thread = ThreadStore::GetAllThreadList(thread, 0, 0);
                *handle = TO_CDENUM(thread);
                status = S_OK;
            }
            else
            {
                status = E_OUTOFMEMORY;
            }
        }
        else
        {
            status = S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EndEnumTasks(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // Enumerator holds no resources.
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetTaskByOSThreadID(
    /* [in] */ ULONG32 osThreadID,
    /* [out] */ IXCLRDataTask **task)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        status = E_INVALIDARG;
        Thread* thread = DacGetThread(osThreadID);
        if (thread != NULL)
        {
            *task = new (nothrow) ClrDataTask(this, thread);
            status = *task ? S_OK : E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetTaskByUniqueID(
    /* [in] */ ULONG64 uniqueID,
    /* [out] */ IXCLRDataTask **task)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        Thread* thread = FindClrThreadByTaskId(uniqueID);
        if (thread)
        {
            *task = new (nothrow) ClrDataTask(this, thread);
            status = *task ? S_OK : E_OUTOFMEMORY;
        }
        else
        {
            status = E_INVALIDARG;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // XXX Microsoft - GC check.
        *flags = CLRDATA_PROCESS_DEFAULT;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::IsSameObject(
    /* [in] */ IXCLRDataProcess* process)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        status = m_pTarget == ((ClrDataAccess*)process)->m_pTarget ?
            S_OK : S_FALSE;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetManagedObject(
    /* [out] */ IXCLRDataValue **value)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetDesiredExecutionState(
    /* [out] */ ULONG32 *state)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::SetDesiredExecutionState(
    /* [in] */ ULONG32 state)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetAddressType(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [out] */ CLRDataAddressType* type)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // The only thing that constitutes a failure is some
        // dac failure while checking things.
        status = S_OK;
        TADDR taAddr = CLRDATA_ADDRESS_TO_TADDR(address);
        if (IsPossibleCodeAddress(taAddr) == S_OK)
        {
            if (ExecutionManager::IsManagedCode(taAddr))
            {
                *type = CLRDATA_ADDRESS_MANAGED_METHOD;
                goto Exit;
            }

            if (StubManager::IsStub(taAddr))
            {
                *type = CLRDATA_ADDRESS_RUNTIME_UNMANAGED_STUB;
                goto Exit;
            }
        }

        *type = CLRDATA_ADDRESS_UNRECOGNIZED;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetRuntimeNameByAddress(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [in] */ ULONG32 flags,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *symbolLen,
    /* [size_is][out] */ _Out_writes_bytes_opt_(bufLen) WCHAR symbolBuf[  ],
    /* [out] */ CLRDATA_ADDRESS* displacement)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
#ifdef TARGET_ARM
        address &= ~THUMB_CODE; //workaround for windbg passing in addresses with the THUMB mode bit set
#endif
        status = RawGetMethodName(address, flags, bufLen, symbolLen, symbolBuf,
                                  displacement);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::StartEnumAppDomains(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // Only one app domain - use 1 to indicate there there is a next value
        *handle = 1;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EnumAppDomain(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataAppDomain **appDomain)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        if (*handle == 1)
        {
            *appDomain = new (nothrow)
                ClrDataAppDomain(this, AppDomain::GetCurrentDomain());
            status = *appDomain ? S_OK : E_OUTOFMEMORY;
            *handle = 0;
        }
        else
        {
            status = S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EndEnumAppDomains(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // Nothing to do - we don't actually create an iterator for app domains
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetAppDomainByUniqueID(
    /* [in] */ ULONG64 uniqueID,
    /* [out] */ IXCLRDataAppDomain **appDomain)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        if (uniqueID != DefaultADID)
        {
            status = E_INVALIDARG;
        }
        else
        {
            *appDomain = new (nothrow)
                ClrDataAppDomain(this, AppDomain::GetCurrentDomain());
            status = *appDomain ? S_OK : E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::StartEnumAssemblies(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        ProcessModIter* iter = new (nothrow) ProcessModIter;
        if (iter)
        {
            *handle = TO_CDENUM(iter);
            status = S_OK;
        }
        else
        {
            status = E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EnumAssembly(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataAssembly **assembly)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        ProcessModIter* iter = FROM_CDENUM(ProcessModIter, *handle);
        Assembly* assem;

        if ((assem = iter->NextAssem()))
        {
            *assembly = new (nothrow)
                ClrDataAssembly(this, assem);
            status = *assembly ? S_OK : E_OUTOFMEMORY;
        }
        else
        {
            status = S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EndEnumAssemblies(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        ProcessModIter* iter = FROM_CDENUM(ProcessModIter, handle);
        delete iter;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::StartEnumModules(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        ProcessModIter* iter = new (nothrow) ProcessModIter;
        if (iter)
        {
            *handle = TO_CDENUM(iter);
            status = S_OK;
        }
        else
        {
            status = E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EnumModule(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataModule **mod)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        ProcessModIter* iter = FROM_CDENUM(ProcessModIter, *handle);
        Module* curMod;

        if ((curMod = iter->NextModule()))
        {
            *mod = new (nothrow)
                ClrDataModule(this, curMod);
            status = *mod ? S_OK : E_OUTOFMEMORY;
        }
        else
        {
            status = S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EndEnumModules(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        ProcessModIter* iter = FROM_CDENUM(ProcessModIter, handle);
        delete iter;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetModuleByAddress(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [out] */ IXCLRDataModule** mod)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        ProcessModIter modIter;
        Module* modDef;

        while ((modDef = modIter.NextModule()))
        {
            TADDR base;
            ULONG32 length;
            PEAssembly* pPEAssembly = modDef->GetPEAssembly();

            if ((base = PTR_TO_TADDR(pPEAssembly->GetLoadedImageContents(&length))))
            {
                if (TO_CDADDR(base) <= address &&
                    TO_CDADDR(base + length) > address)
                {
                    break;
                }
            }
        }

        if (modDef)
        {
            *mod = new (nothrow)
                ClrDataModule(this, modDef);
            status = *mod ? S_OK : E_OUTOFMEMORY;
        }
        else
        {
            status = S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::StartEnumMethodDefinitionsByAddress(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [out] */ CLRDATA_ENUM *handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        ProcessModIter modIter;
        Module* modDef;

        while ((modDef = modIter.NextModule()))
        {
            TADDR base;
            ULONG32 length;
            PEAssembly* assembly = modDef->GetPEAssembly();

            if ((base = PTR_TO_TADDR(assembly->GetLoadedImageContents(&length))))
            {
                if (TO_CDADDR(base) <= address &&
                    TO_CDADDR(base + length) > address)
                {
                    break;
                }
            }
        }

        status = EnumMethodDefinitions::
            CdStart(modDef, true, address, handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EnumMethodDefinitionByAddress(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataMethodDefinition **method)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        status = EnumMethodDefinitions::CdNext(this, handle, method);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EndEnumMethodDefinitionsByAddress(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        status = EnumMethodDefinitions::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::StartEnumMethodInstancesByAddress(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [in] */ IXCLRDataAppDomain* appDomain,
    /* [out] */ CLRDATA_ENUM *handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        MethodDesc* methodDesc;

        *handle = 0;
        status = S_FALSE;
        TADDR taddr;
        if( (status = TRY_CLRDATA_ADDRESS_TO_TADDR(address, &taddr)) != S_OK )
        {
            goto Exit;
        }

        if (IsPossibleCodeAddress(taddr) != S_OK)
        {
            goto Exit;
        }

        methodDesc = ExecutionManager::GetCodeMethodDesc(taddr);
        if (!methodDesc)
        {
            goto Exit;
        }

        status = EnumMethodInstances::CdStart(methodDesc, appDomain,
                                              handle);

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EnumMethodInstanceByAddress(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataMethodInstance **method)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        status = EnumMethodInstances::CdNext(this, handle, method);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::EndEnumMethodInstancesByAddress(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        status = EnumMethodInstances::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetDataByAddress(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [in] */ ULONG32 flags,
    /* [in] */ IXCLRDataAppDomain* appDomain,
    /* [in] */ IXCLRDataTask* tlsTask,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR nameBuf[  ],
    /* [out] */ IXCLRDataValue **value,
    /* [out] */ CLRDATA_ADDRESS *displacement)
{
    HRESULT status;

    if (flags != 0)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER();

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetExceptionStateByExceptionRecord(
    /* [in] */ EXCEPTION_RECORD64 *record,
    /* [out] */ IXCLRDataExceptionState **exception)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::TranslateExceptionRecordToNotification(
    /* [in] */ EXCEPTION_RECORD64 *record,
    /* [in] */ IXCLRDataExceptionNotification *notify)
{
    HRESULT status = E_FAIL;
    ClrDataModule* pubModule = NULL;
    ClrDataMethodInstance* pubMethodInst = NULL;
    ClrDataExceptionState* pubExState = NULL;
    GcEvtArgs pubGcEvtArgs = {};
    ULONG32 notifyType = 0;
    DWORD catcherNativeOffset = 0;
    TADDR nativeCodeLocation = NULL;

    DAC_ENTER();

    EX_TRY
    {
        //
        // We cannot hold the dac lock while calling
        // out as the external code can do arbitrary things.
        // Instead we make a pass over the exception
        // information and create all necessary objects.
        // We then leave the lock and make the callbac.
        //

        TADDR exInfo[EXCEPTION_MAXIMUM_PARAMETERS];
        for (UINT i = 0; i < EXCEPTION_MAXIMUM_PARAMETERS; i++)
        {
            exInfo[i] = TO_TADDR(record->ExceptionInformation[i]);
        }

        notifyType = DACNotify::GetType(exInfo);
        switch(notifyType)
        {
        case DACNotify::MODULE_LOAD_NOTIFICATION:
        {
            TADDR modulePtr;

            if (DACNotify::ParseModuleLoadNotification(exInfo, modulePtr))
            {
                Module* clrModule = PTR_Module(modulePtr);
                pubModule = new (nothrow) ClrDataModule(this, clrModule);
                if (pubModule == NULL)
                {
                    status = E_OUTOFMEMORY;
                }
                else
                {
                    status = S_OK;
                }
            }
            break;
        }

        case DACNotify::MODULE_UNLOAD_NOTIFICATION:
        {
            TADDR modulePtr;

            if (DACNotify::ParseModuleUnloadNotification(exInfo, modulePtr))
            {
                Module* clrModule = PTR_Module(modulePtr);
                pubModule = new (nothrow) ClrDataModule(this, clrModule);
                if (pubModule == NULL)
                {
                    status = E_OUTOFMEMORY;
                }
                else
                {
                    status = S_OK;
                }
            }
            break;
        }

        case DACNotify::JIT_NOTIFICATION2:
        {
            TADDR methodDescPtr;

            if(DACNotify::ParseJITNotification(exInfo, methodDescPtr, nativeCodeLocation))
            {
                MethodDesc* methodDesc = PTR_MethodDesc(methodDescPtr);
                AppDomain* appDomain = AppDomain::GetCurrentDomain();

                pubMethodInst =
                    new (nothrow) ClrDataMethodInstance(this,
                                                        appDomain,
                                                        methodDesc);
                if (pubMethodInst == NULL)
                {
                    status = E_OUTOFMEMORY;
                }
                else
                {
                    status = S_OK;
                }
            }
            break;
        }

        case DACNotify::EXCEPTION_NOTIFICATION:
        {
            TADDR threadPtr;

            if (DACNotify::ParseExceptionNotification(exInfo, threadPtr))
            {
                // Translation can only occur at the time of
                // receipt of the notify exception, so we assume
                // that the Thread's current exception state
                // is the state we want.
                status = ClrDataExceptionState::
                    NewFromThread(this,
                                  PTR_Thread(threadPtr),
                                  &pubExState,
                                  NULL);
            }
            break;
        }

        case DACNotify::GC_NOTIFICATION:
        {
            if (DACNotify::ParseGCNotification(exInfo, pubGcEvtArgs))
            {
                status = S_OK;
            }
            break;
        }

        case DACNotify::CATCH_ENTER_NOTIFICATION:
        {
            TADDR methodDescPtr;
            if (DACNotify::ParseExceptionCatcherEnterNotification(exInfo, methodDescPtr, catcherNativeOffset))
            {
                MethodDesc* methodDesc = PTR_MethodDesc(methodDescPtr);
                AppDomain* appDomain = AppDomain::GetCurrentDomain();

                pubMethodInst =
                    new (nothrow) ClrDataMethodInstance(this,
                                                        appDomain,
                                                        methodDesc);
                if (pubMethodInst == NULL)
                {
                    status = E_OUTOFMEMORY;
                }
                else
                {
                    status = S_OK;
                }
            }
            break;
        }

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();

    if (status == S_OK)
    {
        IXCLRDataExceptionNotification2* notify2;

        if (notify->QueryInterface(__uuidof(IXCLRDataExceptionNotification2),
                                   (void**)&notify2) != S_OK)
        {
            notify2 = NULL;
        }

        IXCLRDataExceptionNotification3* notify3;
        if (notify->QueryInterface(__uuidof(IXCLRDataExceptionNotification3),
                                   (void**)&notify3) != S_OK)
        {
            notify3 = NULL;
        }

        IXCLRDataExceptionNotification4* notify4;
        if (notify->QueryInterface(__uuidof(IXCLRDataExceptionNotification4),
                                   (void**)&notify4) != S_OK)
        {
            notify4 = NULL;
        }

        IXCLRDataExceptionNotification5* notify5;
        if (notify->QueryInterface(__uuidof(IXCLRDataExceptionNotification5),
                                   (void**)&notify5) != S_OK)
        {
            notify5 = NULL;
        }

        switch(notifyType)
        {
        case DACNotify::MODULE_LOAD_NOTIFICATION:
            notify->OnModuleLoaded(pubModule);
            break;

        case DACNotify::MODULE_UNLOAD_NOTIFICATION:
            notify->OnModuleUnloaded(pubModule);
            break;

        case DACNotify::JIT_NOTIFICATION2:
            notify->OnCodeGenerated(pubMethodInst);

            if (notify5)
            {
                notify5->OnCodeGenerated2(pubMethodInst, TO_CDADDR(nativeCodeLocation));
            }
            break;

        case DACNotify::EXCEPTION_NOTIFICATION:
            if (notify2)
            {
                notify2->OnException(pubExState);
            }
            else
            {
                status = E_INVALIDARG;
            }
            break;

        case DACNotify::GC_NOTIFICATION:
            if (notify3)
            {
                notify3->OnGcEvent(pubGcEvtArgs);
            }
            break;

        case DACNotify::CATCH_ENTER_NOTIFICATION:
            if (notify4)
            {
                notify4->ExceptionCatcherEnter(pubMethodInst, catcherNativeOffset);
            }
            break;

        default:
            // notifyType has already been validated.
            _ASSERTE(FALSE);
            break;
        }

        if (notify2)
        {
            notify2->Release();
        }
        if (notify3)
        {
            notify3->Release();
        }
        if (notify4)
        {
            notify4->Release();
        }
        if (notify5)
        {
            notify5->Release();
        }
    }

    if (pubModule)
    {
        pubModule->Release();
    }
    if (pubMethodInst)
    {
        pubMethodInst->Release();
    }
    if (pubExState)
    {
        pubExState->Release();
    }

    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::CreateMemoryValue(
    /* [in] */ IXCLRDataAppDomain* appDomain,
    /* [in] */ IXCLRDataTask* tlsTask,
    /* [in] */ IXCLRDataTypeInstance* type,
    /* [in] */ CLRDATA_ADDRESS addr,
    /* [out] */ IXCLRDataValue** value)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        AppDomain* dacDomain;
        Thread* dacThread;
        TypeHandle dacType;
        ULONG32 flags;
        NativeVarLocation loc;

        dacDomain = ((ClrDataAppDomain*)appDomain)->GetAppDomain();
        if (tlsTask)
        {
            dacThread = ((ClrDataTask*)tlsTask)->GetThread();
        }
        else
        {
            dacThread = NULL;
        }
        dacType = ((ClrDataTypeInstance*)type)->GetTypeHandle();

        flags = GetTypeFieldValueFlags(dacType, NULL, 0, false);

        loc.addr = addr;
        loc.size = dacType.GetSize();
        loc.contextReg = false;

        *value = new (nothrow)
            ClrDataValue(this, dacDomain, dacThread, flags,
                         dacType, addr, 1, &loc);
        status = *value ? S_OK : E_OUTOFMEMORY;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::SetAllTypeNotifications(
    /* [in] */ IXCLRDataModule* mod,
    /* [in] */ ULONG32 flags)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::SetAllCodeNotifications(
    /* [in] */ IXCLRDataModule* mod,
    /* [in] */ ULONG32 flags)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        status = E_FAIL;

        if (!IsValidMethodCodeNotification(flags))
        {
            status = E_INVALIDARG;
        }
        else
        {
            JITNotifications jn(GetHostJitNotificationTable());
            if (!jn.IsActive())
            {
                status = E_OUTOFMEMORY;
            }
            else
            {
                BOOL changedTable;
                TADDR modulePtr = mod ?
                    PTR_HOST_TO_TADDR(((ClrDataModule*)mod)->GetModule()) :
                    NULL;

                if (jn.SetAllNotifications(modulePtr, (USHORT)flags, &changedTable))
                {
                    if (!changedTable ||
                        (changedTable && jn.UpdateOutOfProcTable()))
                    {
                        status = S_OK;
                    }
                }
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetTypeNotifications(
    /* [in] */ ULONG32 numTokens,
    /* [in, size_is(numTokens)] */ IXCLRDataModule* mods[],
    /* [in] */ IXCLRDataModule* singleMod,
    /* [in, size_is(numTokens)] */ mdTypeDef tokens[],
    /* [out, size_is(numTokens)] */ ULONG32 flags[])
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::SetTypeNotifications(
    /* [in] */ ULONG32 numTokens,
    /* [in, size_is(numTokens)] */ IXCLRDataModule* mods[],
    /* [in] */ IXCLRDataModule* singleMod,
    /* [in, size_is(numTokens)] */ mdTypeDef tokens[],
    /* [in, size_is(numTokens)] */ ULONG32 flags[],
    /* [in] */ ULONG32 singleFlags)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::GetCodeNotifications(
    /* [in] */ ULONG32 numTokens,
    /* [in, size_is(numTokens)] */ IXCLRDataModule* mods[],
    /* [in] */ IXCLRDataModule* singleMod,
    /* [in, size_is(numTokens)] */ mdMethodDef tokens[],
    /* [out, size_is(numTokens)] */ ULONG32 flags[])
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        if ((flags == NULL || tokens == NULL) ||
            (mods == NULL && singleMod == NULL) ||
            (mods != NULL && singleMod != NULL))
        {
            status = E_INVALIDARG;
        }
        else
        {
            JITNotifications jn(GetHostJitNotificationTable());
            if (!jn.IsActive())
            {
                status = E_OUTOFMEMORY;
            }
            else
            {
                TADDR modulePtr = NULL;
                if (singleMod)
                {
                    modulePtr = PTR_HOST_TO_TADDR(((ClrDataModule*)singleMod)->
                                                  GetModule());
                }

                for (ULONG32 i = 0; i < numTokens; i++)
                {
                    if (singleMod == NULL)
                    {
                        modulePtr =
                            PTR_HOST_TO_TADDR(((ClrDataModule*)mods[i])->
                                              GetModule());
                    }
                    USHORT jt = jn.Requested(modulePtr, tokens[i]);
                    flags[i] = jt;
                }

                status = S_OK;
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::SetCodeNotifications(
    /* [in] */ ULONG32 numTokens,
    /* [in, size_is(numTokens)] */ IXCLRDataModule* mods[],
    /* [in] */ IXCLRDataModule* singleMod,
    /* [in, size_is(numTokens)] */ mdMethodDef tokens[],
    /* [in, size_is(numTokens)] */ ULONG32 flags[],
    /* [in] */ ULONG32 singleFlags)
{
    HRESULT status = E_UNEXPECTED;

    DAC_ENTER();

    EX_TRY
    {
        if ((tokens == NULL) ||
            (mods == NULL && singleMod == NULL) ||
            (mods != NULL && singleMod != NULL))
        {
            status = E_INVALIDARG;
        }
        else
        {
            JITNotifications jn(GetHostJitNotificationTable());
            if (!jn.IsActive() || numTokens > jn.GetTableSize())
            {
                status = E_OUTOFMEMORY;
            }
            else
            {
                BOOL changedTable = FALSE;

                // Are flags valid?
                if (flags)
                {
                    for (ULONG32 check = 0; check < numTokens; check++)
                    {
                        if (!IsValidMethodCodeNotification(flags[check]))
                        {
                            status = E_INVALIDARG;
                            goto Exit;
                        }
                    }
                }
                else if (!IsValidMethodCodeNotification(singleFlags))
                {
                    status = E_INVALIDARG;
                    goto Exit;
                }

                TADDR modulePtr = NULL;
                if (singleMod)
                {
                    modulePtr =
                        PTR_HOST_TO_TADDR(((ClrDataModule*)singleMod)->
                                          GetModule());
                }

                for (ULONG32 i = 0; i < numTokens; i++)
                {
                    if (singleMod == NULL)
                    {
                        modulePtr =
                            PTR_HOST_TO_TADDR(((ClrDataModule*)mods[i])->
                                              GetModule());
                    }

                    USHORT curFlags = jn.Requested(modulePtr, tokens[i]);
                    USHORT setFlags = (USHORT)(flags ? flags[i] : singleFlags);

                    if (curFlags != setFlags)
                    {
                        if (!jn.SetNotification(modulePtr, tokens[i],
                                                setFlags))
                        {
                            status = E_FAIL;
                            goto Exit;
                        }

                        changedTable = TRUE;
                    }
                }

                if (!changedTable ||
                    (changedTable && jn.UpdateOutOfProcTable()))
                {
                    status = S_OK;
                }
            }
        }

Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT
ClrDataAccess::GetOtherNotificationFlags(
    /* [out] */ ULONG32* flags)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        *flags = g_dacNotificationFlags;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT
ClrDataAccess::SetOtherNotificationFlags(
    /* [in] */ ULONG32 flags)
{
    HRESULT status;

    if ((flags & ~(CLRDATA_NOTIFY_ON_MODULE_LOAD |
                   CLRDATA_NOTIFY_ON_MODULE_UNLOAD |
                   CLRDATA_NOTIFY_ON_EXCEPTION |
                   CLRDATA_NOTIFY_ON_EXCEPTION_CATCH_ENTER)) != 0)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER();

    EX_TRY
    {
        g_dacNotificationFlags = flags;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

enum
{
    STUB_BUF_FLAGS_START,

    STUB_BUF_METHOD_JITTED,
    STUB_BUF_FRAME_PUSHED,
    STUB_BUF_STUB_MANAGER_PUSHED,

    STUB_BUF_FLAGS_END,
};

union STUB_BUF
{
    CLRDATA_FOLLOW_STUB_BUFFER apiBuf;
    struct
    {
        ULONG64 flags;
        ULONG64 addr;
        ULONG64 arg1;
    } u;
};

HRESULT
ClrDataAccess::FollowStubStep(
    /* [in] */ Thread* thread,
    /* [in] */ ULONG32 inFlags,
    /* [in] */ TADDR inAddr,
    /* [in] */ union STUB_BUF* inBuffer,
    /* [out] */ TADDR* outAddr,
    /* [out] */ union STUB_BUF* outBuffer,
    /* [out] */ ULONG32* outFlags)
{
    TraceDestination trace;
    bool traceDone = false;
    BYTE* retAddr;
    T_CONTEXT localContext;
    REGDISPLAY regDisp;
    MethodDesc* methodDesc;

    ZeroMemory(outBuffer, sizeof(*outBuffer));

    if (inBuffer)
    {
        switch(inBuffer->u.flags)
        {
        case STUB_BUF_METHOD_JITTED:
            if (inAddr != GFN_TADDR(DACNotifyCompilationFinished))
            {
                return E_INVALIDARG;
            }

            // It's possible that this notification is
            // for a different method, so double-check
            // and recycle the notification if necessary.
            methodDesc = PTR_MethodDesc(CORDB_ADDRESS_TO_TADDR(inBuffer->u.addr));
            if (methodDesc->HasNativeCode())
            {
                *outAddr = methodDesc->GetNativeCode();
                *outFlags = CLRDATA_FOLLOW_STUB_EXIT;
                return S_OK;
            }

            // We didn't end up with native code so try again.
            trace.InitForUnjittedMethod(methodDesc);
            traceDone = true;
            break;

        case STUB_BUF_FRAME_PUSHED:
            if (!thread ||
                inAddr != inBuffer->u.addr)
            {
                return E_INVALIDARG;
            }

            trace.InitForFramePush(CORDB_ADDRESS_TO_TADDR(inBuffer->u.addr));
            DacGetThreadContext(thread, &localContext);
            thread->FillRegDisplay(&regDisp, &localContext);
            if (!thread->GetFrame()->
                TraceFrame(thread,
                           TRUE,
                           &trace,
                           &regDisp))
            {
                return E_FAIL;
            }

            traceDone = true;
            break;

        case STUB_BUF_STUB_MANAGER_PUSHED:
            if (!thread ||
                inAddr != inBuffer->u.addr ||
                !inBuffer->u.arg1)
            {
                return E_INVALIDARG;
            }

            trace.InitForManagerPush(CORDB_ADDRESS_TO_TADDR(inBuffer->u.addr),
                                     PTR_StubManager(CORDB_ADDRESS_TO_TADDR(inBuffer->u.arg1)));
            DacGetThreadContext(thread, &localContext);
            if (!trace.GetStubManager()->
                TraceManager(thread,
                             &trace,
                             &localContext,
                             &retAddr))
            {
                return E_FAIL;
            }

            traceDone = true;
            break;

        default:
            return E_INVALIDARG;
        }
    }

    if ((!traceDone &&
         !StubManager::TraceStub(inAddr, &trace)) ||
        !StubManager::FollowTrace(&trace))
    {
        return E_NOINTERFACE;
    }

    switch(trace.GetTraceType())
    {
    case TRACE_UNMANAGED:
    case TRACE_MANAGED:
        // We've hit non-stub code so we're done.
        *outAddr = trace.GetAddress();
        *outFlags = CLRDATA_FOLLOW_STUB_EXIT;
        break;

    case TRACE_UNJITTED_METHOD:
        // The stub causes jitting, so return
        // the address of the jit-complete routine
        // so that the real native address can
        // be picked up once the JIT is done.

        methodDesc = trace.GetMethodDesc();

        *outAddr = GFN_TADDR(DACNotifyCompilationFinished);
        outBuffer->u.flags = STUB_BUF_METHOD_JITTED;
        outBuffer->u.addr = PTR_HOST_TO_TADDR(methodDesc);
        *outFlags = CLRDATA_FOLLOW_STUB_INTERMEDIATE;
        break;

    case TRACE_FRAME_PUSH:
        if (!thread)
        {
            return E_INVALIDARG;
        }

        *outAddr = trace.GetAddress();
        outBuffer->u.flags = STUB_BUF_FRAME_PUSHED;
        outBuffer->u.addr = trace.GetAddress();
        *outFlags = CLRDATA_FOLLOW_STUB_INTERMEDIATE;
        break;

    case TRACE_MGR_PUSH:
        if (!thread)
        {
            return E_INVALIDARG;
        }

        *outAddr = trace.GetAddress();
        outBuffer->u.flags = STUB_BUF_STUB_MANAGER_PUSHED;
        outBuffer->u.addr = trace.GetAddress();
        outBuffer->u.arg1 = PTR_HOST_TO_TADDR(trace.GetStubManager());
        *outFlags = CLRDATA_FOLLOW_STUB_INTERMEDIATE;
        break;

    default:
        return E_INVALIDARG;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::FollowStub(
    /* [in] */ ULONG32 inFlags,
    /* [in] */ CLRDATA_ADDRESS inAddr,
    /* [in] */ CLRDATA_FOLLOW_STUB_BUFFER* _inBuffer,
    /* [out] */ CLRDATA_ADDRESS* outAddr,
    /* [out] */ CLRDATA_FOLLOW_STUB_BUFFER* _outBuffer,
    /* [out] */ ULONG32* outFlags)
{
    return FollowStub2(NULL, inFlags, inAddr, _inBuffer,
                       outAddr, _outBuffer, outFlags);
}

HRESULT STDMETHODCALLTYPE
ClrDataAccess::FollowStub2(
    /* [in] */ IXCLRDataTask* task,
    /* [in] */ ULONG32 inFlags,
    /* [in] */ CLRDATA_ADDRESS _inAddr,
    /* [in] */ CLRDATA_FOLLOW_STUB_BUFFER* _inBuffer,
    /* [out] */ CLRDATA_ADDRESS* _outAddr,
    /* [out] */ CLRDATA_FOLLOW_STUB_BUFFER* _outBuffer,
    /* [out] */ ULONG32* outFlags)
{
    HRESULT status;

    if ((inFlags & ~(CLRDATA_FOLLOW_STUB_DEFAULT)) != 0)
    {
        return E_INVALIDARG;
    }

    STUB_BUF* inBuffer = (STUB_BUF*)_inBuffer;
    STUB_BUF* outBuffer = (STUB_BUF*)_outBuffer;

    if (inBuffer &&
        (inBuffer->u.flags <= STUB_BUF_FLAGS_START ||
         inBuffer->u.flags >= STUB_BUF_FLAGS_END))
    {
        return E_INVALIDARG;
    }

    DAC_ENTER();

    EX_TRY
    {
        STUB_BUF cycleBuf;
        TADDR inAddr = TO_TADDR(_inAddr);
        TADDR outAddr;
        Thread* thread = task ? ((ClrDataTask*)task)->GetThread() : NULL;
        ULONG32 loops = 4;

        while (true)
        {
            if ((status = FollowStubStep(thread,
                                         inFlags,
                                         inAddr,
                                         inBuffer,
                                         &outAddr,
                                         outBuffer,
                                         outFlags)) != S_OK)
            {
                break;
            }

            // Some stub tracing just requests further iterations
            // of processing, so detect that case and loop.
            if (outAddr != inAddr)
            {
                // We can make forward progress, we're done.
                *_outAddr = TO_CDADDR(outAddr);
                break;
            }

            // We need more processing.  As a protection
            // against infinite loops in corrupted or buggy
            // situations, we only allow this to happen a
            // small number of times.
            if (--loops == 0)
            {
                ZeroMemory(outBuffer, sizeof(*outBuffer));
                status = E_FAIL;
                break;
            }

            cycleBuf = *outBuffer;
            inBuffer = &cycleBuf;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4297)
#endif // _MSC_VER
STDMETHODIMP
ClrDataAccess::GetGcNotification(GcEvtArgs* gcEvtArgs)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        if (gcEvtArgs->typ >= GC_EVENT_TYPE_MAX)
        {
            status = E_INVALIDARG;
        }
        else
        {
            GcNotifications gn(GetHostGcNotificationTable());
            if (!gn.IsActive())
            {
                status = E_OUTOFMEMORY;
            }
            else
            {
                GcEvtArgs *res = gn.GetNotification(*gcEvtArgs);
                if (res != NULL)
                {
                    *gcEvtArgs = *res;
                    status = S_OK;
                }
                else
                {
                    status = E_FAIL;
                }
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

STDMETHODIMP
ClrDataAccess::SetGcNotification(IN GcEvtArgs gcEvtArgs)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        if (gcEvtArgs.typ >= GC_EVENT_TYPE_MAX)
        {
            status = E_INVALIDARG;
        }
        else
        {
            GcNotifications gn(GetHostGcNotificationTable());
            if (!gn.IsActive())
            {
                status = E_OUTOFMEMORY;
            }
            else
            {
                if (gn.SetNotification(gcEvtArgs) && gn.UpdateOutOfProcTable())
                {
                    status = S_OK;
                }
                else
                {
                    status = E_FAIL;
                }
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER

HRESULT
ClrDataAccess::Initialize(void)
{
    HRESULT hr;
    CLRDATA_ADDRESS base = { 0 };

    //
    // We do not currently support cross-platform
    // debugging.  Verify that cross-platform is not
    // being attempted.
    //

    // Determine our platform based on the pre-processor macros set when we were built

#ifdef TARGET_UNIX
    #if defined(TARGET_X86)
        CorDebugPlatform hostPlatform = CORDB_PLATFORM_POSIX_X86;
    #elif defined(TARGET_AMD64)
        CorDebugPlatform hostPlatform = CORDB_PLATFORM_POSIX_AMD64;
    #elif defined(TARGET_ARM)
        CorDebugPlatform hostPlatform = CORDB_PLATFORM_POSIX_ARM;
    #elif defined(TARGET_ARM64)
        CorDebugPlatform hostPlatform = CORDB_PLATFORM_POSIX_ARM64;
    #elif defined(TARGET_LOONGARCH64)
        CorDebugPlatform hostPlatform = CORDB_PLATFORM_POSIX_LOONGARCH64;
    #elif defined(TARGET_RISCV64)
        CorDebugPlatform hostPlatform = CORDB_PLATFORM_POSIX_RISCV64;
    #else
        #error Unknown Processor.
    #endif
#else
    #if defined(TARGET_X86)
        CorDebugPlatform hostPlatform = CORDB_PLATFORM_WINDOWS_X86;
    #elif defined(TARGET_AMD64)
        CorDebugPlatform hostPlatform = CORDB_PLATFORM_WINDOWS_AMD64;
    #elif defined(TARGET_ARM)
        CorDebugPlatform hostPlatform = CORDB_PLATFORM_WINDOWS_ARM;
    #elif defined(TARGET_ARM64)
        CorDebugPlatform hostPlatform = CORDB_PLATFORM_WINDOWS_ARM64;
    #else
        #error Unknown Processor.
    #endif
#endif

    CorDebugPlatform targetPlatform;
    IfFailRet(m_pTarget->GetPlatform(&targetPlatform));

    if (targetPlatform != hostPlatform)
    {
        // DAC fatal error: Platform mismatch - the platform reported by the data target
        // is not what this version of mscordacwks.dll was built for.
        return CORDBG_E_INCOMPATIBLE_PLATFORMS;
    }

    //
    // Get the current DLL base for mscorwks globals.
    // In case of multiple-CLRs, there may be multiple dlls named "mscorwks".
    // code:OpenVirtualProcess can take the base address (clrInstanceId) to select exactly
    // which CLR to is being target. If so, m_globalBase will already be set.
    //

    if (m_globalBase == 0)
    {
        // Caller didn't specify which CLR to debug, we should be using a legacy data target.
        if (m_pLegacyTarget == NULL)
        {
            DacError(E_INVALIDARG);
            UNREACHABLE();
        }

        ReleaseHolder<ICLRRuntimeLocator> pRuntimeLocator(NULL);
        if (m_pLegacyTarget->QueryInterface(__uuidof(ICLRRuntimeLocator), (void**)&pRuntimeLocator) != S_OK || pRuntimeLocator->GetRuntimeBase(&base) != S_OK)
        {
            IfFailRet(m_pLegacyTarget->GetImageBase(TARGET_MAIN_CLR_DLL_NAME_W, &base));
        }

        m_globalBase = TO_TADDR(base);
    }

    // We don't need to try too hard to prevent
    // multiple initializations as each one will
    // copy the same data into the globals and so
    // cannot interfere with each other.
    IfFailRet(GetDacGlobalValues());
    IfFailRet(DacGetHostVtPtrs());

    //
    // DAC is now setup and ready to use
    //

    // Do some validation
    IfFailRet(VerifyDlls());

    return S_OK;
}

Thread*
ClrDataAccess::FindClrThreadByTaskId(ULONG64 taskId)
{
    Thread* thread = NULL;

    if (!ThreadStore::s_pThreadStore)
    {
        return NULL;
    }

    while ((thread = ThreadStore::GetAllThreadList(thread, 0, 0)))
    {
        if (thread->GetThreadId() == (DWORD)taskId)
        {
            return thread;
        }
    }

    return NULL;
}

HRESULT
ClrDataAccess::IsPossibleCodeAddress(IN TADDR address)
{
    SUPPORTS_DAC;
    BYTE testRead;
    ULONG32 testDone;

    // First do a trivial check on the readability of the
    // address.  This makes for quick rejection of bogus
    // addresses that the debugger sends in when searching
    // stacks for return addresses.
    // XXX Microsoft - Will this cause problems in minidumps
    // where it's possible the stub is identifiable but
    // the stub code isn't present?  Yes, but the lack
    // of that code could confuse the walker on its own
    // if it does code analysis.
    if ((m_pTarget->ReadVirtual(address, &testRead, sizeof(testRead),
                               &testDone) != S_OK) ||
        !testDone)
    {
        return E_INVALIDARG;
    }

    return S_OK;
}

HRESULT
ClrDataAccess::GetFullMethodName(
    IN MethodDesc* methodDesc,
    IN ULONG32 symbolChars,
    OUT ULONG32* symbolLen,
    _Out_writes_to_opt_(symbolChars, *symbolLen) LPWSTR symbol
    )
{
    StackSString s;
#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
    PAL_CPP_TRY
    {
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

        TypeString::AppendMethodInternal(s, methodDesc, TypeString::FormatSignature|TypeString::FormatNamespace|TypeString::FormatFullInst);

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
    }
    PAL_CPP_CATCH_ALL
    {
        if (!MdCacheGetEEName(dac_cast<TADDR>(methodDesc), s))
        {
            PAL_CPP_RETHROW;
        }
    }
    PAL_CPP_ENDTRY
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

    if (symbol)
    {
        // Copy as much as we can and truncate the rest.
        wcsncpy_s(symbol, symbolChars, s.GetUnicode(), _TRUNCATE);
    }

    if (symbolLen)
        *symbolLen = s.GetCount() + 1;

    if (symbol != NULL && symbolChars < (s.GetCount() + 1))
        return S_FALSE;
    else
        return S_OK;
}

PCSTR
ClrDataAccess::GetJitHelperName(
    IN TADDR address,
    IN bool dynamicHelpersOnly /*=false*/
    )
{
    const static PCSTR s_rgHelperNames[] = {
#define JITHELPER(code,fn,sig) #code,
#include <jithelpers.h>
    };
    static_assert_no_msg(ARRAY_SIZE(s_rgHelperNames) == CORINFO_HELP_COUNT);

#ifdef TARGET_UNIX
    if (!dynamicHelpersOnly)
#else
    if (!dynamicHelpersOnly && g_runtimeLoadedBaseAddress <= address &&
            address < g_runtimeLoadedBaseAddress + g_runtimeVirtualSize)
#endif // TARGET_UNIX
    {
        // Read the whole table from the target in one shot for better performance
        VMHELPDEF * pTable = static_cast<VMHELPDEF *>(
            PTR_READ(dac_cast<TADDR>(&hlpFuncTable), CORINFO_HELP_COUNT * sizeof(VMHELPDEF)));

        for (int i = 0; i < CORINFO_HELP_COUNT; i++)
        {
            if (address == (TADDR)(pTable[i].pfnHelper))
                return s_rgHelperNames[i];
        }
    }

    // Check if its a dynamically generated JIT helper
    const static CorInfoHelpFunc s_rgDynamicHCallIds[] = {
#define DYNAMICJITHELPER(code, fn, sig) code,
#define JITHELPER(code, fn,sig)
#include <jithelpers.h>
    };

    // Read the whole table from the target in one shot for better performance
    VMHELPDEF * pDynamicTable = static_cast<VMHELPDEF *>(
        PTR_READ(dac_cast<TADDR>(&hlpDynamicFuncTable), DYNAMIC_CORINFO_HELP_COUNT * sizeof(VMHELPDEF)));
    for (unsigned d = 0; d < DYNAMIC_CORINFO_HELP_COUNT; d++)
    {
        if (address == (TADDR)(pDynamicTable[d].pfnHelper))
        {
            return s_rgHelperNames[s_rgDynamicHCallIds[d]];
        }
    }

    return NULL;
}

// This function expects more memory than maybe needed.
static int FormatCLRStubName(
    _In_opt_z_ LPCWSTR stubNameMaybe,
    _In_ TADDR stubAddr,
    _In_ ULONG32 bufLen,
    _Out_ ULONG32 *symbolLen,
    _Out_writes_bytes_opt_(bufLen) WCHAR* symbolBuf)
{
    // Parts needed to construct a name:
    // With stub manager name: "CLRStub[%s]@%p"
    //   No stub manager name: "CLRStub@%p"
    const WCHAR formatName_Prefix[] = W("CLRStub");
    const WCHAR formatName_OpenBracket[] = W("[");
    const WCHAR formatName_CloseBracket[] = W("]");
    const WCHAR formatName_PrefixEnd[] = W("@");

    // Compute the address as a string safely.
    WCHAR addrString[Max64BitHexString + 1];
    FormatInteger(addrString, ARRAY_SIZE(addrString), "%p", stubAddr);
    size_t addStringLen = u16_strlen(addrString);

    // Compute maximum length, include the null terminator.
    size_t formatName_MaxLen = ARRAY_SIZE(formatName_Prefix) // Include trailing null
                                + ARRAY_SIZE(formatName_PrefixEnd) - 1
                                + addStringLen;

    // Consider stub manager name
    size_t stubManagedNameLen = 0;
    if (stubNameMaybe != NULL)
    {
        stubManagedNameLen = u16_strlen(stubNameMaybe);
        formatName_MaxLen += ARRAY_SIZE(formatName_OpenBracket) - 1;
        formatName_MaxLen += ARRAY_SIZE(formatName_CloseBracket) - 1;
    }

    HRESULT hr = S_FALSE;

    // Compute the exact length needed.
    const size_t lenNeeded = formatName_MaxLen + stubManagedNameLen;
    if (lenNeeded <= bufLen)
    {
        size_t written = 0;

        // Set the prefix
        wcscpy_s(symbolBuf, bufLen - written, formatName_Prefix);
        written += ARRAY_SIZE(formatName_Prefix) - 1;

        // Add the name
        if (stubManagedNameLen > 0)
        {
            wcscat_s(symbolBuf, bufLen - written, formatName_OpenBracket);
            written += ARRAY_SIZE(formatName_OpenBracket) - 1;
            wcscat_s(symbolBuf, bufLen - written, stubNameMaybe);
            written += stubManagedNameLen;
            wcscat_s(symbolBuf, bufLen - written, formatName_CloseBracket);
            written += ARRAY_SIZE(formatName_CloseBracket) - 1;
        }

        // Append the prefix end
        wcscat_s(symbolBuf, bufLen - written, formatName_PrefixEnd);
        written += ARRAY_SIZE(formatName_PrefixEnd) - 1;

        // Append the address
        wcscat_s(symbolBuf, bufLen - written, addrString);
        written += addStringLen;

        hr = S_OK;
    }

    if (symbolLen)
    {
        if (!FitsIn<ULONG32>(lenNeeded))
            return COR_E_OVERFLOW;

        *symbolLen = (ULONG32)lenNeeded;
    }

    return hr;
}

HRESULT
ClrDataAccess::RawGetMethodName(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [in] */ ULONG32 flags,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *symbolLen,
    /* [size_is][out] */ _Out_writes_bytes_opt_(bufLen) WCHAR symbolBuf[  ],
    /* [out] */ CLRDATA_ADDRESS* displacement)
{
#ifdef TARGET_ARM
    _ASSERTE((address & THUMB_CODE) == 0);
    address &= ~THUMB_CODE;
#endif

    HRESULT status;

    if (flags != 0)
    {
        return E_INVALIDARG;
    }

    TADDR taddr;
    if( (status = TRY_CLRDATA_ADDRESS_TO_TADDR(address, &taddr)) != S_OK )
    {
        return status;
    }

    if ((status = IsPossibleCodeAddress(taddr)) != S_OK)
    {
        return status;
    }

    PTR_StubManager pStubManager;
    MethodDesc* methodDesc = NULL;

    {
        EECodeInfo codeInfo(TO_TADDR(address));
        if (codeInfo.IsValid())
        {
            if (displacement)
            {
                *displacement = codeInfo.GetRelOffset();
            }

            methodDesc = codeInfo.GetMethodDesc();
            goto NameFromMethodDesc;
        }
    }

    pStubManager = StubManager::FindStubManager(TO_TADDR(address));
    if (pStubManager != NULL)
    {
        if (displacement)
        {
            *displacement = 0;
        }

        //
        // Special-cased stub managers
        //
        if (pStubManager == PrecodeStubManager::g_pManager)
        {
            PCODE alignedAddress = AlignDown(TO_TADDR(address), PRECODE_ALIGNMENT);

#ifdef TARGET_ARM
            alignedAddress += THUMB_CODE;
#endif

            SIZE_T maxPrecodeSize = sizeof(StubPrecode);

#ifdef HAS_THISPTR_RETBUF_PRECODE
            maxPrecodeSize = max(maxPrecodeSize, sizeof(ThisPtrRetBufPrecode));
#endif

            for (SIZE_T i = 0; i < maxPrecodeSize / PRECODE_ALIGNMENT; i++)
            {
                EX_TRY
                {
                    // Try to find matching precode entrypoint
                    Precode* pPrecode = Precode::GetPrecodeFromEntryPoint(alignedAddress, TRUE);
                    if (pPrecode != NULL)
                    {
                        methodDesc = pPrecode->GetMethodDesc();
                        if (methodDesc != NULL)
                        {
                            if (DacValidateMD(methodDesc))
                            {
                                if (displacement)
                                {
                                    *displacement = TO_TADDR(address) - PCODEToPINSTR(alignedAddress);
                                }
                                goto NameFromMethodDesc;
                            }
                        }
                    }
                    alignedAddress -= PRECODE_ALIGNMENT;
                }
                EX_CATCH
                {
                }
                EX_END_CATCH(SwallowAllExceptions)
            }
        }
        else
        if (pStubManager == JumpStubStubManager::g_pManager)
        {
            PCODE pTarget = decodeBackToBackJump(TO_TADDR(address));

            HRESULT hr = GetRuntimeNameByAddress(pTarget, flags, bufLen, symbolLen, symbolBuf, NULL);
            if (SUCCEEDED(hr))
            {
                return hr;
            }

            PCSTR pHelperName = GetJitHelperName(pTarget);
            if (pHelperName != NULL)
            {
                hr = ConvertUtf8(pHelperName, bufLen, symbolLen, symbolBuf);
                if (FAILED(hr))
                    return S_FALSE;

                return hr;
            }
        }

        LPCWSTR wszStubManagerName = pStubManager->GetStubManagerName(TO_TADDR(address));
        _ASSERTE(wszStubManagerName != NULL);

        return FormatCLRStubName(
            wszStubManagerName,
            TO_TADDR(address),
            bufLen,
            symbolLen,
            symbolBuf);
    }

    // Do not waste time looking up name for static helper. Debugger can get the actual name from .pdb.
    PCSTR pHelperName;
    pHelperName = GetJitHelperName(TO_TADDR(address), true /* dynamicHelpersOnly */);
    if (pHelperName != NULL)
    {
        if (displacement)
        {
            *displacement = 0;
        }

        HRESULT hr = ConvertUtf8(pHelperName, bufLen, symbolLen, symbolBuf);
        if (FAILED(hr))
            return S_FALSE;

        return S_OK;
    }

    return E_NOINTERFACE;

NameFromMethodDesc:
    if (methodDesc->GetClassification() == mcDynamic &&
        !methodDesc->GetSig())
    {
        return FormatCLRStubName(
            NULL,
            TO_TADDR(address),
            bufLen,
            symbolLen,
            symbolBuf);
    }

    return GetFullMethodName(methodDesc, bufLen, symbolLen, symbolBuf);
}

HRESULT
ClrDataAccess::GetMethodExtents(MethodDesc* methodDesc,
                                METH_EXTENTS** extents)
{
    CLRDATA_ADDRESS_RANGE* curExtent;

    {
        //
        // Get the information from the methoddesc.
        // We'll go through the CodeManager + JitManagers, so this should work
        // for all types of managed code.
        //

        PCODE methodStart = methodDesc->GetNativeCode();
        if (!methodStart)
        {
            return E_NOINTERFACE;
        }

        EECodeInfo codeInfo(methodStart);
        _ASSERTE(codeInfo.IsValid());

        TADDR codeSize = codeInfo.GetCodeManager()->GetFunctionSize(codeInfo.GetGCInfoToken());

        *extents = new (nothrow) METH_EXTENTS;
        if (!*extents)
        {
            return E_OUTOFMEMORY;
        }

        (*extents)->numExtents = 1;
        curExtent = (*extents)->extents;
        curExtent->startAddress = TO_CDADDR(methodStart);
        curExtent->endAddress =
            curExtent->startAddress + codeSize;
        curExtent++;
    }

    (*extents)->curExtent = 0;

    return S_OK;
}

// Allocator to pass to the debug-info-stores...
BYTE* DebugInfoStoreNew(void * pData, size_t cBytes)
{
    return new (nothrow) BYTE[cBytes];
}

HRESULT
ClrDataAccess::GetMethodVarInfo(MethodDesc* methodDesc,
                                TADDR address,
                                ULONG32* numVarInfo,
                                ICorDebugInfo::NativeVarInfo** varInfo,
                                ULONG32* codeOffset)
{
    SUPPORTS_DAC;
    COUNT_T countNativeVarInfo;
    NewHolder<ICorDebugInfo::NativeVarInfo> nativeVars(NULL);
    TADDR nativeCodeStartAddr;
    if (address != NULL)
    {
        NativeCodeVersion requestedNativeCodeVersion = ExecutionManager::GetNativeCodeVersion(address);
        if (requestedNativeCodeVersion.IsNull() || requestedNativeCodeVersion.GetNativeCode() == NULL)
        {
            return E_INVALIDARG;
        }
        nativeCodeStartAddr = PCODEToPINSTR(requestedNativeCodeVersion.GetNativeCode());
    }
    else
    {
        nativeCodeStartAddr = PCODEToPINSTR(methodDesc->GetNativeCode());
    }

    DebugInfoRequest request;
    request.InitFromStartingAddr(methodDesc, nativeCodeStartAddr);

    BOOL success = DebugInfoManager::GetBoundariesAndVars(
        request,
        DebugInfoStoreNew, NULL, // allocator
        NULL, NULL,
        &countNativeVarInfo, &nativeVars);

    if (!success)
    {
        return E_FAIL;
    }

    if (!nativeVars || !countNativeVarInfo)
    {
        return E_NOINTERFACE;
    }

    *numVarInfo = countNativeVarInfo;
    *varInfo = nativeVars;
    nativeVars.SuppressRelease(); // To prevent NewHolder from releasing the memory

    if (codeOffset)
    {
        *codeOffset = (ULONG32)(address - nativeCodeStartAddr);
    }
    return S_OK;
}

HRESULT
ClrDataAccess::GetMethodNativeMap(MethodDesc* methodDesc,
                                  TADDR address,
                                  ULONG32* numMap,
                                  DebuggerILToNativeMap** map,
                                  bool* mapAllocated,
                                  CLRDATA_ADDRESS* codeStart,
                                  ULONG32* codeOffset)
{
    _ASSERTE((codeOffset == NULL) || (address != NULL));

    // Use the DebugInfoStore to get IL->Native maps.
    // It doesn't matter whether we're jitted, ngenned etc.
    TADDR nativeCodeStartAddr;
    if (address != NULL)
    {
        NativeCodeVersion requestedNativeCodeVersion = ExecutionManager::GetNativeCodeVersion(address);
        if (requestedNativeCodeVersion.IsNull() || requestedNativeCodeVersion.GetNativeCode() == NULL)
        {
            return E_INVALIDARG;
        }
        nativeCodeStartAddr = PCODEToPINSTR(requestedNativeCodeVersion.GetNativeCode());
    }
    else
    {
        nativeCodeStartAddr = PCODEToPINSTR(methodDesc->GetNativeCode());
    }

    DebugInfoRequest request;
    request.InitFromStartingAddr(methodDesc, nativeCodeStartAddr);

    // Bounds info.
    ULONG32 countMapCopy;
    NewArrayHolder<ICorDebugInfo::OffsetMapping> mapCopy(NULL);

    BOOL success = DebugInfoManager::GetBoundariesAndVars(
        request,
        DebugInfoStoreNew, NULL, // allocator
        &countMapCopy, &mapCopy,
        NULL, NULL);

    if (!success)
    {
        return E_FAIL;
    }

    // Need to convert map formats.
    *numMap = countMapCopy;

    *map = new (nothrow) DebuggerILToNativeMap[countMapCopy];
    if (!*map)
    {
        return E_OUTOFMEMORY;
    }

    ULONG32 i;
    for (i = 0; i < *numMap; i++)
    {
        (*map)[i].ilOffset = mapCopy[i].ilOffset;
        (*map)[i].nativeStartOffset = mapCopy[i].nativeOffset;
        if (i > 0)
        {
            (*map)[i - 1].nativeEndOffset = (*map)[i].nativeStartOffset;
        }
        (*map)[i].source = mapCopy[i].source;
    }
    if (*numMap >= 1)
    {
        (*map)[i - 1].nativeEndOffset = 0;
    }


    // Update varion out params.
    if (codeStart)
    {
        *codeStart = TO_CDADDR(nativeCodeStartAddr);
    }
    if (codeOffset)
    {
        *codeOffset = (ULONG32)(address - nativeCodeStartAddr);
    }

    *mapAllocated = true;
    return S_OK;
}

// Get the MethodDesc for a function
// Arguments:
//    Input:
//       pModule   - pointer to the module for the function
//       memberRef - metadata token for the function
// Return Value:
//       MethodDesc for the function
MethodDesc * ClrDataAccess::FindLoadedMethodRefOrDef(Module* pModule,
    mdToken memberRef)
{
    CONTRACT(MethodDesc *)
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pModule));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // Must have a MemberRef or a MethodDef
    mdToken tkType = TypeFromToken(memberRef);
    _ASSERTE((tkType == mdtMemberRef) || (tkType == mdtMethodDef));

    if (tkType == mdtMemberRef)
    {
        RETURN pModule->LookupMemberRefAsMethod(memberRef);
    }

    RETURN pModule->LookupMethodDef(memberRef);
} // FindLoadedMethodRefOrDef

//
// ReportMem - report a region of memory for dump gathering
//
// If you specify that you expect success, any failure will cause ReportMem to
// return false.  If you do not expect success, true is always returned.
// This function only throws when all dump collection should be cancelled.
//
// Arguments:
//     addr - the starting target address for the memory to report
//     size - the length (in bytes) to report
//     fExpectSuccess - if true (the default), then we expect that this region of memory
//                      should be fully readable.  Any read errors indicate a corrupt target.
//
bool ClrDataAccess::ReportMem(TADDR addr, TSIZE_T size, bool fExpectSuccess /*= true*/)
{
    SUPPORTS_DAC_HOST_ONLY;

    // This block of code is to help debugging blocks that we report
    // to minidump/heapdump. You can set break point here to view the static
    // variable to figure out the size of blocks that we are reporting.
    // Most useful is set conditional break point to catch large chuck of
    // memory. We will leave it here for all builds.
    //
    static TADDR debugAddr;
    static TSIZE_T debugSize;
    debugAddr = addr;
    debugSize = size;

    HRESULT status;
    if (!addr || addr == (TADDR)-1 || !size)
    {
        if (fExpectSuccess)
            return false;
        else
            return true;
    }

    //
    // Try and sanity-check the reported region of memory
    //
#ifdef _DEBUG
    // in debug builds, sanity-check all reports
    const TSIZE_T k_minSizeToCheck = 1;
#else
    // in retail builds, only sanity-check larger chunks which have the potential to waste a
    // lot of time and/or space.  This avoids the overhead of checking for the majority of
    // memory regions (which are small).
    const TSIZE_T k_minSizeToCheck = 1024;
#endif
    if (size >= k_minSizeToCheck)
    {
        if (!IsFullyReadable(addr, size))
        {
            if (fExpectSuccess)
            {
                // We're reporting bogus memory, so the target must be corrupt (or there is a issue). We should abort
                // reporting and continue with the next data structure (where the exception is caught),
                // just like we would for a DAC read error (otherwise we might do something stupid
                // like get into an infinite loop, or otherwise waste time with corrupt data).
                TARGET_CONSISTENCY_CHECK(false, "Found unreadable memory while reporting memory regions for dump gathering");
                return false;
            }
        }
    }

    // Minidumps should never contain data structures that are anywhere near 4MB.  If we see this, it's
    // probably due to memory corruption.  To keep the dump small, we'll truncate the block.  Note that
    // the size to which the block is truncated is pretty unique, so should be good evidence in a dump
    // that this has happened.
    // Note that it's hard to say what a good value would be here, or whether we should dump any of the
    // data structure at all.  Hopefully experience will help guide this going forward.
    // @dbgtodo : Extend dump-gathering API to allow a dump-log to be included.
    const TSIZE_T kMaxMiniDumpRegion = 4*1024*1024 - 3;    // 4MB-3
    if (size > kMaxMiniDumpRegion && (m_enumMemFlags == CLRDATA_ENUM_MEM_MINI || m_enumMemFlags == CLRDATA_ENUM_MEM_TRIAGE))
    {
        TARGET_CONSISTENCY_CHECK( false, "Dump target consistency failure - truncating minidump data structure");
        size = kMaxMiniDumpRegion;
    }

    // track the total memory reported.
    m_cbMemoryReported += size;

    // ICLRData APIs take only 32-bit sizes.  In practice this will almost always be sufficient, but
    // in theory we might have some >4GB ranges on large 64-bit processes doing a heap dump
    // (for example, the code:LoaderHeap).  If necessary, break up the reporting into maximum 4GB
    // chunks so we can use the existing API.
    // @dbgtodo : ICorDebugDataTarget should probably use 64-bit sizes
    while (size)
    {
        ULONG32 enumSize;
        if (size > UINT32_MAX)
        {
            enumSize = UINT32_MAX;
        }
        else
        {
            enumSize = (ULONG32)size;
        }

        // Actually perform the memory reporting callback
        status = m_enumMemCb->EnumMemoryRegion(TO_CDADDR(addr), enumSize);
        if (status != S_OK)
        {
            // If dump generation was cancelled, allow us to throw upstack so we'll actually quit.
            if ((fExpectSuccess) && (status != COR_E_OPERATIONCANCELED))
                return false;
        }

        // If the return value of EnumMemoryRegion is COR_E_OPERATIONCANCELED,
        // it means that user has requested that the minidump gathering be canceled.
        // To do this we throw an exception which is caught in EnumMemoryRegionsWrapper.
        if (status == COR_E_OPERATIONCANCELED)
        {
            ThrowHR(status);
        }

        // Move onto the next chunk (if any)
        size -= enumSize;
        addr += enumSize;
    }

    return true;
}


//
// DacUpdateMemoryRegion - updates/poisons a region of memory of generated dump
//
// Parameters:
//   addr           - target address of the beginning of the memory region
//   bufferSize     - number of bytes to update/poison
//   buffer         - data to be written at given target address
//
bool ClrDataAccess::DacUpdateMemoryRegion(TADDR addr, TSIZE_T bufferSize, BYTE* buffer)
{
    SUPPORTS_DAC_HOST_ONLY;

    HRESULT status;
    if (!addr || addr == (TADDR)-1 || !bufferSize)
    {
        return false;
    }

    // track the total memory reported.
    m_cbMemoryReported += bufferSize;

    if (m_updateMemCb == NULL)
    {
        return false;
    }

    // Actually perform the memory updating callback
    status = m_updateMemCb->UpdateMemoryRegion(TO_CDADDR(addr), (ULONG32)bufferSize, buffer);
    if (status != S_OK)
    {
        return false;
    }

    return true;
}

//
// DacLogMessage - logs a message to an external DAC client
//
// Parameters:
//   message - message to log
//
void DacLogMessage(LPCSTR format, ...)
{
    SUPPORTS_DAC_HOST_ONLY;

    if (g_dacImpl->IsLogMessageEnabled())
    {
        va_list args;
        va_start(args, format);
        char buffer[1024];
        _vsnprintf_s(buffer, sizeof(buffer), _TRUNCATE, format, args);
        g_dacImpl->LogMessage(buffer);
        va_end(args);
    }
}

//
// Check whether a region of target memory is fully readable.
//
// Arguments:
//     addr    The base target address of the region
//     size    The size of the region to analyze
//
// Return value:
//     True if the entire regions appears to be readable, false otherwise.
//
// Notes:
//     The motivation here is that reporting large regions of unmapped address space to dbgeng can result in
//     it taking a long time trying to identify a valid subrange.  This can happen when the target
//     memory is corrupt, and we enumerate a data structure with a dynamic size.  Ideally we would just spec
//     the ICLRDataEnumMemoryRegionsCallback API to require the client to fail if it detects an unmapped
//     memory address in the region.  However, we can't change the existing dbgeng code, so for now we'll
//     rely on this heuristic here.
//     @dbgtodo : Try and get the dbg team to change their EnumMemoryRegion behavior.  See DevDiv Bugs 6265
//
bool ClrDataAccess::IsFullyReadable(TADDR taBase, TSIZE_T dwSize)
{
    // The only way we have to verify that a memory region is readable is to try reading it in it's
    // entirety.  This is potentially expensive, so we'll rely on a heuristic that spot-checks various
    // points in the region.

    // Ensure we've got something to check
    if( dwSize == 0 )
        return true;

    // Check for overflow
    TADDR taEnd = DacTAddrOffset(taBase, dwSize, 1);

    // Loop through using expontential growth, being sure to check both the first and last byte
    TADDR taCurr = taBase;
    TSIZE_T dwInc = 4096;
    bool bDone = false;
    while (!bDone)
    {
        // Try and read a byte from the target.  Note that we don't use PTR_BYTE here because we don't want
        // the overhead of inserting entries into the DAC instance cache.
        BYTE b;
        ULONG32 dwBytesRead;
        HRESULT hr = m_pTarget->ReadVirtual(taCurr, &b, 1, &dwBytesRead);
        if( hr != S_OK || dwBytesRead < 1 )
        {
            return false;
        }

        if (taEnd - taCurr <= 1)
        {
            // We just read the last byte so we're done
            _ASSERTE( taCurr = taEnd - 1 );
            bDone = true;
        }
        else if (dwInc == 0 || dwInc >= taEnd - taCurr)
        {
            // we've reached the end of the exponential series, check the last byte
            taCurr = taEnd - 1;
        }
        else
        {
            // advance current pointer (subtraction above ensures this won't overflow)
            taCurr += dwInc;

            // double the increment for next time (or set to 0 if it's already the max)
            dwInc <<= 1;
        }
    }
    return true;
}

JITNotification*
ClrDataAccess::GetHostJitNotificationTable()
{
    if (m_jitNotificationTable == NULL)
    {
        m_jitNotificationTable =
            JITNotifications::InitializeNotificationTable(1000);
    }

    return m_jitNotificationTable;
}

GcNotification*
ClrDataAccess::GetHostGcNotificationTable()
{
    if (m_gcNotificationTable == NULL)
    {
        m_gcNotificationTable =
            GcNotifications::InitializeNotificationTable(128);
    }

    return m_gcNotificationTable;
}

/* static */ bool
ClrDataAccess::GetMetaDataFileInfoFromPEFile(PEAssembly *pPEAssembly,
                                             DWORD &dwTimeStamp,
                                             DWORD &dwSize,
                                             DWORD &dwDataSize,
                                             DWORD &dwRvaHint,
                                             bool  &isNGEN,
                                             _Out_writes_(cchFilePath) LPWSTR wszFilePath,
                                             const DWORD cchFilePath)
{
    SUPPORTS_DAC_HOST_ONLY;
    PEImage *mdImage = NULL;
    PEImageLayout   *layout = NULL;
    IMAGE_DATA_DIRECTORY *pDir = NULL;
    COUNT_T uniPathChars = 0;

    isNGEN = false;
    if (pDir == NULL || pDir->Size == 0)
    {
        mdImage = pPEAssembly->GetPEImage();
        if (mdImage != NULL)
        {
            layout = mdImage->GetLoadedLayout();
            pDir = &layout->GetCorHeader()->MetaData;

            // In IL image case, we do not have any hint to IL metadata since it is stored
            // in the corheader.
            //
            dwRvaHint = 0;
            dwDataSize = pDir->Size;
        }
        else
        {
            return false;
        }
    }

    // Do not fail if path can not be read. Triage dumps don't have paths and we want to fallback
    // on searching metadata from IL image.
    mdImage->GetPath().DacGetUnicode(cchFilePath, wszFilePath, &uniPathChars);

    if (!mdImage->HasNTHeaders() ||
        !mdImage->HasCorHeader() ||
        !mdImage->HasLoadedLayout() ||
        (uniPathChars > cchFilePath))
    {
        return false;
    }

    // It is possible that the module is in-memory. That is the wszFilePath here is empty.
    // We will try to use the module name instead in this case for hosting debugger
    // to find match.
    if (u16_strlen(wszFilePath) == 0)
    {
        mdImage->GetModuleFileNameHintForDAC().DacGetUnicode(cchFilePath, wszFilePath, &uniPathChars);
        if (uniPathChars > cchFilePath)
        {
            return false;
        }
    }

    dwTimeStamp = layout->GetTimeDateStamp();
    dwSize = (ULONG32)layout->GetVirtualSize();

    return true;
}

/* static */
bool ClrDataAccess::GetILImageInfoFromNgenPEFile(PEAssembly *pPEAssembly,
                                                 DWORD &dwTimeStamp,
                                                 DWORD &dwSize,
                                                 _Out_writes_(cchFilePath) LPWSTR wszFilePath,
                                                 const DWORD cchFilePath)
{
    SUPPORTS_DAC_HOST_ONLY;
    DWORD dwWritten = 0;

    // use the IL File name
    if (!pPEAssembly->GetPath().DacGetUnicode(cchFilePath, wszFilePath, (COUNT_T *)(&dwWritten)))
    {
        // Use DAC hint to retrieve the IL name.
        pPEAssembly->GetModuleFileNameHint().DacGetUnicode(cchFilePath, wszFilePath, (COUNT_T *)(&dwWritten));
    }
    dwTimeStamp = 0;
    dwSize = 0;

    return true;
}

void *
ClrDataAccess::GetMetaDataFromHost(PEAssembly* pPEAssembly,
                                   bool* isAlternate)
{
    DWORD imageTimestamp, imageSize, dataSize;
    void* buffer = NULL;
    WCHAR uniPath[MAX_LONGPATH] = {0};
    bool isAlt = false;
    bool isNGEN = false;
    DAC_INSTANCE* inst = NULL;
    HRESULT  hr = S_OK;
    DWORD ulRvaHint;
    //
    // We always ask for the IL image metadata,
    // as we expect that to be more
    // available than others.  The drawback is that
    // there may be differences between the IL image
    // metadata and native image metadata, so we
    // have to mark such alternate metadata so that
    // we can fail unsupported usage of it.
    //

    // Microsoft - above comment seems to be an unimplemented thing.
    // The DAC_MD_IMPORT.isAlternate field gets ultimately set, but
    // on the searching I did, I cannot find any usage of it
    // other than in the ctor.  Should we be doing something, or should
    // we remove this comment and the isAlternate field?
    // It's possible that test will want us to track whether we have
    // an IL image's metadata loaded against an NGEN'ed image
    // so the field remains for now.

    if (!ClrDataAccess::GetMetaDataFileInfoFromPEFile(
            pPEAssembly,
            imageTimestamp,
            imageSize,
            dataSize,
            ulRvaHint,
            isNGEN,
            uniPath,
            ARRAY_SIZE(uniPath)))
    {
        return NULL;
    }

    // try direct match for the image that is loaded into the managed process
    pPEAssembly->GetLoadedMetadata((COUNT_T *)(&dataSize));

    DWORD allocSize = 0;
    if (!ClrSafeInt<DWORD>::addition(dataSize, sizeof(DAC_INSTANCE), allocSize))
    {
        DacError(HRESULT_FROM_WIN32(ERROR_ARITHMETIC_OVERFLOW));
    }

    inst = m_instances.Alloc(0, allocSize, DAC_DPTR);
    if (!inst)
    {
        DacError(E_OUTOFMEMORY);
        return NULL;
    }

    buffer = (void*)(inst + 1);

    // APIs implemented by hosting debugger.  It can use the path/filename, timestamp, and
    // pPEAssembly size to find an exact match for the image.  If that fails for an ngen'ed image,
    // we can request the IL image which it came from.
    if (m_legacyMetaDataLocator)
    {
        // Legacy API implemented by hosting debugger.
        hr = m_legacyMetaDataLocator->GetMetadata(
            uniPath,
            imageTimestamp,
            imageSize,
            NULL,           // MVID - not used yet
            ulRvaHint,
            0,              // flags - reserved for future.
            dataSize,
            (BYTE*)buffer,
            NULL);
    }
    else
    {
        hr = m_target3->GetMetaData(
            uniPath,
            imageTimestamp,
            imageSize,
            NULL,           // MVID - not used yet
            ulRvaHint,
            0,              // flags - reserved for future.
            dataSize,
            (BYTE*)buffer,
            NULL);
    }
    if (FAILED(hr) && isNGEN)
    {
        // We failed to locate the ngen'ed image. We should try to
        // find the matching IL image
        //
        isAlt = true;
        if (!ClrDataAccess::GetILImageInfoFromNgenPEFile(
                pPEAssembly,
                imageTimestamp,
                imageSize,
                uniPath,
                ARRAY_SIZE(uniPath)))
        {
            goto ErrExit;
        }

        const WCHAR* ilExtension = W("dll");
        WCHAR ngenImageName[MAX_LONGPATH] = {0};
        if (wcscpy_s(ngenImageName, ARRAY_SIZE(ngenImageName), uniPath) != 0)
        {
            goto ErrExit;
        }
        if (wcscpy_s(uniPath, ARRAY_SIZE(uniPath), ngenImageName) != 0)
        {
            goto ErrExit;
        }

        // RVA size in ngen image and IL image is the same. Because the only
        // different is in RVA. That is 4 bytes column fixed.
        //

        // try again
        if (m_legacyMetaDataLocator)
        {
            hr = m_legacyMetaDataLocator->GetMetadata(
                uniPath,
                imageTimestamp,
                imageSize,
                NULL,           // MVID - not used yet
                0,              // pass zero hint here... important
                0,              // flags - reserved for future.
                dataSize,
                (BYTE*)buffer,
                NULL);
        }
        else
        {
            hr = m_target3->GetMetaData(
                uniPath,
                imageTimestamp,
                imageSize,
                NULL,           // MVID - not used yet
                0,              // pass zero hint here... important
                0,              // flags - reserved for future.
                dataSize,
                (BYTE*)buffer,
                NULL);
        }
    }

    if (FAILED(hr))
    {
        goto ErrExit;
    }

    *isAlternate = isAlt;
    m_instances.AddSuperseded(inst);
    return buffer;

ErrExit:
    if (inst != NULL)
    {
        m_instances.ReturnAlloc(inst);
    }
    return NULL;
}


//++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Given a PEAssembly or a ReflectionModule try to find the corresponding metadata
// We will first ask debugger to locate it. If fail, we will try
// to get it from the target process
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++
IMDInternalImport*
ClrDataAccess::GetMDImport(const PEAssembly* pPEAssembly, const ReflectionModule* reflectionModule, bool throwEx)
{
    HRESULT     status;
    PTR_CVOID mdBaseTarget = NULL;
    COUNT_T     mdSize;
    IMDInternalImport* mdImport = NULL;
    PVOID       mdBaseHost = NULL;
    bool        isAlternate = false;

    _ASSERTE((pPEAssembly == NULL && reflectionModule != NULL) || (pPEAssembly != NULL && reflectionModule == NULL));
    TADDR     peAssemblyAddr = (pPEAssembly != NULL) ? dac_cast<TADDR>(pPEAssembly) : dac_cast<TADDR>(reflectionModule);

    //
    // Look for one we've already created.
    //
    mdImport = m_mdImports.Get(peAssemblyAddr);
    if (mdImport != NULL)
    {
        return mdImport;
    }

    if (pPEAssembly != NULL)
    {
        // Get the metadata size
        mdBaseTarget = const_cast<PEAssembly*>(pPEAssembly)->GetLoadedMetadata(&mdSize);
    }
    else if (reflectionModule != NULL)
    {
        // Get the metadata
        PTR_SBuffer metadataBuffer = reflectionModule->GetDynamicMetadataBuffer();
        if (metadataBuffer != PTR_NULL)
        {
            mdBaseTarget = dac_cast<PTR_CVOID>((metadataBuffer->DacGetRawBuffer()).StartAddress());
            mdSize = metadataBuffer->GetSize();
        }
        else
        {
            if (throwEx)
            {
                DacError(E_FAIL);
            }
            return NULL;
        }
    }
    else
    {
        if (throwEx)
        {
            DacError(E_FAIL);
        }
        return NULL;
    }

    if (mdBaseTarget == PTR_NULL)
    {
        mdBaseHost = NULL;
    }
    else
    {

        //
        // Maybe the target process has the metadata
        // Find out where the metadata for the image is
        // in the target's memory.
        //
        //
        // Read the metadata into the host process. Make sure pass in false in the last
        // parameter. This is only matters when producing skinny mini-dump. This will
        // prevent metadata gets reported into mini-dump.
        //
        mdBaseHost = DacInstantiateTypeByAddressNoReport(dac_cast<TADDR>(mdBaseTarget), mdSize,
                                                 false);
    }

    // Try to see if debugger can locate it
    if (pPEAssembly != NULL && mdBaseHost == NULL && (m_target3 || m_legacyMetaDataLocator))
    {
        // We couldn't read the metadata from memory.  Ask
        // the target for metadata as it may be able to
        // provide it from some alternate means.
        mdBaseHost = GetMetaDataFromHost(const_cast<PEAssembly *>(pPEAssembly), &isAlternate);
    }

    if (mdBaseHost == NULL)
    {
        // cannot locate metadata anywhere
        if (throwEx)
        {
            DacError(E_INVALIDARG);
        }
        return NULL;
    }

    //
    // Open the MD interface on the host copy of the metadata.
    //

    status = GetMDInternalInterface(mdBaseHost, mdSize, ofRead,
                                    IID_IMDInternalImport,
                                    (void**)&mdImport);
    if (status != S_OK)
    {
        if (throwEx)
        {
            DacError(status);
        }
        return NULL;
    }

    //
    // Remember the object for this module for
    // possible later use.
    // The m_mdImports list does get cleaned up by calls to ClrDataAccess::Flush,
    // i.e. every time the process changes state.

    if (m_mdImports.Add(peAssemblyAddr, mdImport, isAlternate) == NULL)
    {
        mdImport->Release();
        DacError(E_OUTOFMEMORY);
    }

    return mdImport;
}


//
// Set whether inconsistencies in the target should raise asserts.
// This overrides the default initial setting.
//
// Arguments:
//     fEnableAsserts - whether ASSERTs in dacized code should be enabled
//

void ClrDataAccess::SetTargetConsistencyChecks(bool fEnableAsserts)
{
    LIMITED_METHOD_DAC_CONTRACT;
    m_fEnableTargetConsistencyAsserts = fEnableAsserts;
}

//
// Get whether inconsistencies in the target should raise asserts.
//
// Return value:
//     whether ASSERTs in dacized code should be enabled
//
// Notes:
//     The implementation of ASSERT accesses this via code:DacTargetConsistencyAssertsEnabled
//
//     By default, this is disabled, unless DOTNET_DbgDACEnableAssert is set (see code:ClrDataAccess::ClrDataAccess).
//     This is necessary for compatibility.  For example, SOS expects to be able to scan for
//     valid MethodTables etc. (which may cause ASSERTs), and also doesn't want ASSERTs when working
//     with targets with corrupted memory.
//
//     Calling code:ClrDataAccess::SetTargetConsistencyChecks overrides the default setting.
//
bool ClrDataAccess::TargetConsistencyAssertsEnabled()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_fEnableTargetConsistencyAsserts;
}

//
// VerifyDlls - Validate that the mscorwks in the target matches this version of mscordacwks
// Only done on Windows and Mac builds at the moment.
// See code:CordbProcess::CordbProcess#DBIVersionChecking for more information regarding version checking.
//
HRESULT ClrDataAccess::VerifyDlls()
{
    return S_OK;
}

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

void ClrDataAccess::InitStreamsForWriting(IN CLRDataEnumMemoryFlags flags)
{
    // enforce this should only be called when generating triage and mini-dumps
    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
        return;

    EX_TRY
    {
        if (m_streams == NULL)
            m_streams = new DacStreamManager(g_MiniMetaDataBuffAddress, g_MiniMetaDataBuffMaxSize);

        if (!m_streams->PrepareStreamsForWriting())
        {
            delete m_streams;
            m_streams = NULL;
        }
    }
    EX_CATCH
    {
        if (m_streams != NULL)
        {
            delete m_streams;
            m_streams = NULL;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)
}

bool ClrDataAccess::MdCacheAddEEName(TADDR taEEStruct, const SString& name)
{
    bool result = false;
    EX_TRY
    {
        if (m_streams != NULL)
            result = m_streams->MdCacheAddEEName(taEEStruct, name);
    }
    EX_CATCH
    {
        result = false;
    }
    EX_END_CATCH(SwallowAllExceptions)

    return result;
}

void ClrDataAccess::EnumStreams(IN CLRDataEnumMemoryFlags flags)
{
    // enforce this should only be called when generating triage and mini-dumps
    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
        return;

    EX_TRY
    {
        if (m_streams != NULL)
            m_streams->EnumStreams(flags);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)
}

bool ClrDataAccess::MdCacheGetEEName(TADDR taEEStruct, SString & eeName)
{
    bool result = false;
    EX_TRY
    {
        if (m_streams == NULL)
            m_streams = new DacStreamManager(g_MiniMetaDataBuffAddress, g_MiniMetaDataBuffMaxSize);

        result = m_streams->MdCacheGetEEName(taEEStruct, eeName);
    }
    EX_CATCH
    {
        result = false;
    }
    EX_END_CATCH(SwallowAllExceptions)

    return result;
}

#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

HRESULT
GetDacTableAddress(ICorDebugDataTarget* dataTarget, ULONG64 baseAddress, PULONG64 dacTableAddress)
{
#ifdef USE_DAC_TABLE_RVA
#ifdef DAC_TABLE_SIZE
    if (DAC_TABLE_SIZE != sizeof(DacGlobals))
    {
        return E_INVALIDARG;
    }
#endif
    // On MacOS, FreeBSD or NetBSD use the RVA include file
    *dacTableAddress = baseAddress + DAC_TABLE_RVA;
#else
    // Otherwise, try to get the dac table address via the export symbol
    if (!TryGetSymbol(dataTarget, baseAddress, DACCESS_TABLE_SYMBOL, dacTableAddress))
    {
        return CORDBG_E_MISSING_DEBUGGER_EXPORTS;
    }
#endif
    return S_OK;
}

HRESULT
ClrDataAccess::GetDacGlobalValues()
{
    ULONG64 dacTableAddress;
    HRESULT hr = GetDacTableAddress(m_pTarget, m_globalBase, &dacTableAddress);
    if (FAILED(hr))
    {
        return hr;
    }
    if (FAILED(ReadFromDataTarget(m_pTarget, dacTableAddress, (BYTE*)&m_dacGlobals, sizeof(m_dacGlobals))))
    {
        return CORDBG_E_MISSING_DEBUGGER_EXPORTS;
    }
    if (m_dacGlobals.ThreadStore__s_pThreadStore == NULL)
    {
        return CORDBG_E_UNSUPPORTED;
    }
    return S_OK;
}

//----------------------------------------------------------------------------
//
// IsExceptionFromManagedCode - report if pExceptionRecord points to an exception belonging to the current runtime
//
// Arguments:
//    pExceptionRecord - the exception record
//
// Return Value:
//    TRUE if it is
//    Otherwise, FALSE
//
//----------------------------------------------------------------------------
BOOL ClrDataAccess::IsExceptionFromManagedCode(EXCEPTION_RECORD* pExceptionRecord)
{
    DAC_ENTER();

    BOOL flag = FALSE;

    if (::IsExceptionFromManagedCode(pExceptionRecord))
    {
        flag = TRUE;
    }

    DAC_LEAVE();

    return flag;
}

#ifndef TARGET_UNIX

//----------------------------------------------------------------------------
//
// GetWatsonBuckets - retrieve Watson buckets from the specified thread
//
// Arguments:
//    dwThreadId - the thread ID
//    pGM - pointer to the space to store retrieved Watson buckets
//
// Return Value:
//    S_OK if the operation is successful.
//    or S_FALSE if Watson buckets cannot be found
//    else detailed error code.
//
//----------------------------------------------------------------------------
HRESULT ClrDataAccess::GetWatsonBuckets(DWORD dwThreadId, GenericModeBlock * pGM)
{
    _ASSERTE((dwThreadId != 0) && (pGM != NULL));
    if ((dwThreadId == 0) || (pGM == NULL))
    {
        return E_INVALIDARG;
    }

    DAC_ENTER();

    Thread * pThread = DacGetThread(dwThreadId);
    _ASSERTE(pThread != NULL);

    HRESULT hr = E_UNEXPECTED;

    if (pThread != NULL)
    {
        hr = GetClrWatsonBucketsWorker(pThread, pGM);
    }

    DAC_LEAVE();
    return hr;
}

#endif // TARGET_UNIX

//----------------------------------------------------------------------------
//
// CLRDataAccessCreateInstance - create and initialize a ClrDataAccess object
//
// Arguments:
//    pLegacyTarget - data target object
//    pClrDataAccess - ClrDataAccess object
//
// Return Value:
//    S_OK on success, else detailed error code.
//
//----------------------------------------------------------------------------
STDAPI CLRDataAccessCreateInstance(ICLRDataTarget * pLegacyTarget,
                                   ClrDataAccess ** pClrDataAccess)
{
    if ((pLegacyTarget == NULL) || (pClrDataAccess == NULL))
    {
        return E_INVALIDARG;
    }

    *pClrDataAccess = NULL;

    // Create an adapter which implements the new ICorDebugDataTarget interfaces using
    // a legacy implementation of ICLRDataTarget
    // ClrDataAccess will take a take a ref on this and delete it when it's released.
    DataTargetAdapter * pDtAdapter = new (nothrow) DataTargetAdapter(pLegacyTarget);
    if (!pDtAdapter)
    {
        return E_OUTOFMEMORY;
    }

    ClrDataAccess* dacClass = new (nothrow) ClrDataAccess(pDtAdapter, pLegacyTarget);
    if (!dacClass)
    {
        delete pDtAdapter;
        return E_OUTOFMEMORY;
    }

    HRESULT hr = dacClass->Initialize();
    if (FAILED(hr))
    {
        dacClass->Release();
        return hr;
    }

    *pClrDataAccess = dacClass;
    return S_OK;
}


//----------------------------------------------------------------------------
//
// CLRDataCreateInstance.
// Creates the IXClrData object
// This is the legacy entrypoint to DAC, used by dbgeng/dbghelp (windbg, SOS, watson, etc).
//
//----------------------------------------------------------------------------
STDAPI
DLLEXPORT
CLRDataCreateInstance(REFIID iid,
                      ICLRDataTarget * pLegacyTarget,
                      void ** iface)
{
    if ((pLegacyTarget == NULL) || (iface == NULL))
    {
        return E_INVALIDARG;
    }

    *iface = NULL;
    ClrDataAccess * pClrDataAccess;
    HRESULT hr = CLRDataAccessCreateInstance(pLegacyTarget, &pClrDataAccess);
    if (hr != S_OK)
    {
        return hr;
    }
#ifdef LOGGING
    InitializeLogging();
#endif
    hr = pClrDataAccess->QueryInterface(iid, iface);

    pClrDataAccess->Release();
    return hr;
}


//----------------------------------------------------------------------------
//
// OutOfProcessExceptionEventGetProcessIdAndThreadId - get ProcessID and ThreadID
//
// Arguments:
//    hProcess - process handle
//    hThread - thread handle
//    pPId - pointer to DWORD to store ProcessID
//    pThreadId - pointer to DWORD to store ThreadID
//
// Return Value:
//    TRUE if the operation is successful.
//    FALSE if it fails
//
//----------------------------------------------------------------------------
BOOL OutOfProcessExceptionEventGetProcessIdAndThreadId(HANDLE hProcess, HANDLE hThread, DWORD * pPId, DWORD * pThreadId)
{
    _ASSERTE((pPId != NULL) && (pThreadId != NULL));

#ifdef TARGET_UNIX
    // UNIXTODO: mikem 1/13/15 Need appropriate PAL functions for getting ids
    *pPId = (DWORD)(SIZE_T)hProcess;
    *pThreadId = (DWORD)(SIZE_T)hThread;
#else
    *pPId = GetProcessIdOfThread(hThread);
    *pThreadId = GetThreadId(hThread);
#endif // TARGET_UNIX
    return TRUE;
}

// WER_RUNTIME_EXCEPTION_INFORMATION will be available from Win7 SDK once Win7 SDK is released.
#if !defined(WER_RUNTIME_EXCEPTION_INFORMATION)
typedef struct _WER_RUNTIME_EXCEPTION_INFORMATION
{
    DWORD dwSize;
    HANDLE hProcess;
    HANDLE hThread;
    EXCEPTION_RECORD exceptionRecord;
    CONTEXT context;
} WER_RUNTIME_EXCEPTION_INFORMATION, * PWER_RUNTIME_EXCEPTION_INFORMATION;
#endif // !defined(WER_RUNTIME_EXCEPTION_INFORMATION)


#ifndef TARGET_UNIX

//----------------------------------------------------------------------------
//
// OutOfProcessExceptionEventGetWatsonBucket - retrieve Watson buckets if it is a managed exception
//
// Arguments:
//    pContext - the context passed at helper module registration
//    pExceptionInformation - structure that contains information about the crash
//    pGM - pointer to the space to store retrieved Watson buckets
//
// Return Value:
//    S_OK if the operation is successful.
//    or S_FALSE if it is not a managed exception or Watson buckets cannot be found
//    else detailed error code.
//
//----------------------------------------------------------------------------
STDAPI OutOfProcessExceptionEventGetWatsonBucket(_In_ PDWORD pContext,
                                                 _In_ const PWER_RUNTIME_EXCEPTION_INFORMATION pExceptionInformation,
                                                 _Out_ GenericModeBlock * pGMB)
{
    HANDLE hProcess = pExceptionInformation->hProcess;
    HANDLE hThread  = pExceptionInformation->hThread;
    DWORD PId, ThreadId;

    if (!OutOfProcessExceptionEventGetProcessIdAndThreadId(hProcess, hThread, &PId, &ThreadId))
    {
        return E_FAIL;
    }

    CLRDATA_ADDRESS baseAddressOfRuntime = (CLRDATA_ADDRESS)pContext;
    NewHolder<LiveProcDataTarget> dataTarget(NULL);

    dataTarget = new (nothrow) LiveProcDataTarget(hProcess, PId, baseAddressOfRuntime);
    if (dataTarget == NULL)
    {
        return E_OUTOFMEMORY;
    }

    NewHolder<ClrDataAccess> pClrDataAccess(NULL);

    HRESULT hr = CLRDataAccessCreateInstance(dataTarget, &pClrDataAccess);
    if (hr != S_OK)
    {
        if (hr == S_FALSE)
        {
            return E_FAIL;
        }
        else
        {
            return hr;
        }
    }

    if (!pClrDataAccess->IsExceptionFromManagedCode(&pExceptionInformation->exceptionRecord))
    {
        return S_FALSE;
    }

    return pClrDataAccess->GetWatsonBuckets(ThreadId, pGMB);
}

//----------------------------------------------------------------------------
//
// OutOfProcessExceptionEventCallback - claim the ownership of this event if current
//                                      runtime threw the unhandled exception
//
// Arguments:
//    pContext - the context passed at helper module registration
//    pExceptionInformation - structure that contains information about the crash
//    pbOwnershipClaimed - output parameter for claiming the ownership of this event
//    pwszEventName - name of the event. If this is NULL, pchSize cannot be NULL.
//                    This parameter is valid only if * pbOwnershipClaimed is TRUE.
//    pchSize - the size of the buffer pointed by pwszEventName
//    pdwSignatureCount - the count of signature parameters. Valid values range from
//                        0 to 10. If the value returned is greater than 10, only the
//                        1st 10 parameters are used for bucketing parameters. This
//                        parameter is valid only if * pbOwnershipClaimed is TRUE.
//
// Return Value:
//    S_OK on success, else detailed error code.
//
// Note:
//    This is the 1st function that is called into by WER. This API through its out
//    parameters, tells WER as to whether or not it is claiming the crash. If it does
//    claim the crash, WER uses the event name specified in the string pointed to by
//    pwszEventName for error reporting. WER then proceed to call the
//    OutOfProcessExceptionEventSignatureCallback to get the bucketing parameters from
//    the helper dll.
//
//    This function follows the multiple call paradigms. WER may call into this function
//    with *pwszEventName pointer set to NULL. This is to indicate to the function, that
//    WER wants to know the buffer size needed by the function to populate the string
//    into the buffer. The function should return E_INSUFFICIENTBUFFER with the needed
//    buffer size in *pchSize. WER shall then allocate a buffer of size *pchSize for
//    pwszEventName and then call this function again at which point the function should
//    populate the string and return S_OK.
//
//    Note that *pdOwnershipClaimed should be set to TRUE everytime this function is called
//    for the helper dll to claim ownership of bucketing.
//
//    The Win7 WER spec is at
//    http://windows/windows7/docs/COSD%20Documents/Fundamentals/Feedback%20Services%20and%20Platforms/WER-CLR%20Integration%20Dev%20Spec.docx
//
//    !!!READ THIS!!!
//    Since this is called by external modules it's important that we don't let any exceptions leak out (see Win8 95224).
//
//----------------------------------------------------------------------------
STDAPI OutOfProcessExceptionEventCallback(_In_ PDWORD pContext,
                                          _In_ const PWER_RUNTIME_EXCEPTION_INFORMATION pExceptionInformation,
                                          _Out_ BOOL * pbOwnershipClaimed,
                                          _Out_writes_(*pchSize) PWSTR pwszEventName,
                                          __inout PDWORD pchSize,
                                          _Out_ PDWORD pdwSignatureCount)
{
    SUPPORTS_DAC_HOST_ONLY;

    if ((pContext == NULL) ||
        (pExceptionInformation == NULL) ||
        (pExceptionInformation->dwSize < sizeof(WER_RUNTIME_EXCEPTION_INFORMATION)) ||
        (pbOwnershipClaimed == NULL) ||
        (pchSize == NULL) ||
        (pdwSignatureCount == NULL))
    {
        return E_INVALIDARG;
    }

    *pbOwnershipClaimed = FALSE;

    GenericModeBlock gmb;
    HRESULT hr = E_FAIL;

    EX_TRY
    {
        // get Watson buckets if it is a managed exception
        hr = OutOfProcessExceptionEventGetWatsonBucket(pContext, pExceptionInformation, &gmb);
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK)
    {
        // S_FALSE means either it is not a managed exception or we do not have Watson buckets.
        // Since we have set pbOwnershipClaimed to FALSE, we return S_OK to WER.
        if (hr == S_FALSE)
        {
            hr = S_OK;
        }

        return hr;
    }

    if ((pwszEventName == NULL) || (*pchSize <= u16_strlen(gmb.wzEventTypeName)))
    {
        *pchSize = static_cast<DWORD>(u16_strlen(gmb.wzEventTypeName)) + 1;
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    // copy custom event name
    wcscpy_s(pwszEventName, *pchSize, gmb.wzEventTypeName);
    *pdwSignatureCount = GetCountBucketParamsForEvent(gmb.wzEventTypeName);
    *pbOwnershipClaimed = TRUE;

    return S_OK;
}


//----------------------------------------------------------------------------
//
// OutOfProcessExceptionEventCallback - provide custom Watson buckets
//
// Arguments:
//    pContext - the context passed at helper module registration
//    pExceptionInformation - structure that contains information about the crash
//    dwIndex - the index of the bucketing parameter being requested. Valid values are
//              from 0 to 9
//    pwszName - pointer to the name of the bucketing parameter
//    pchName - pointer to character count of the pwszName buffer. If pwszName points to
//              null, *pchName represents the buffer size (represented in number of characters)
//              needed to populate the name in pwszName.
//    pwszValue - pointer to the value of the pwszName bucketing parameter
//    pchValue - pointer to the character count of the pwszValue buffer. If pwszValue points
//               to null, *pchValue represents the buffer size (represented in number of
//               characters) needed to populate the value in pwszValue.
//
// Return Value:
//    S_OK on success, else detailed error code.
//
// Note:
//    This function is called by WER only if the call to OutOfProcessExceptionEventCallback()
//    was successful and the value of *pbOwnershipClaimed was TRUE. This function is called
//    pdwSignatureCount times to collect the bucketing parameters from the helper dll.
//
//    This function also follows the multiple call paradigm as described for the
//    OutOfProcessExceptionEventCallback() function. The buffer sizes needed for
//    this function are of the pwszName and pwszValue buffers.
//
//    !!!READ THIS!!!
//    Since this is called by external modules it's important that we don't let any exceptions leak out (see Win8 95224).
//
//----------------------------------------------------------------------------
STDAPI OutOfProcessExceptionEventSignatureCallback(_In_ PDWORD pContext,
                                                   _In_ const PWER_RUNTIME_EXCEPTION_INFORMATION pExceptionInformation,
                                                   _In_ DWORD dwIndex,
                                                   _Out_writes_(*pchName) PWSTR pwszName,
                                                   __inout PDWORD pchName,
                                                   _Out_writes_(*pchValue) PWSTR pwszValue,
                                                   __inout PDWORD pchValue)
{
    SUPPORTS_DAC_HOST_ONLY;

    if ((pContext == NULL) ||
        (pExceptionInformation == NULL) ||
        (pExceptionInformation->dwSize < sizeof(WER_RUNTIME_EXCEPTION_INFORMATION)) ||
        (pchName == NULL) ||
        (pchValue == NULL))
    {
        return E_INVALIDARG;
    }

    if ((pwszName == NULL) || (*pchName == 0))
    {
        *pchName = 1;
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    GenericModeBlock gmb;
    const PWSTR pwszBucketValues[] = {gmb.wzP1,
                                      gmb.wzP2,
                                      gmb.wzP3,
                                      gmb.wzP4,
                                      gmb.wzP5,
                                      gmb.wzP6,
                                      gmb.wzP7,
                                      gmb.wzP8,
                                      gmb.wzP9,
                                      gmb.wzP10};

    HRESULT hr = E_FAIL;

    EX_TRY
    {
        // get Watson buckets if it is a managed exception
        hr = OutOfProcessExceptionEventGetWatsonBucket(pContext, pExceptionInformation, &gmb);
    }
    EX_CATCH_HRESULT(hr);

    // it's possible for the OS to kill
    // the faulting process before WER crash reporting has completed.
    _ASSERTE(hr == S_OK || hr == CORDBG_E_READVIRTUAL_FAILURE);

    if (hr != S_OK)
    {
        // S_FALSE means either it is not a managed exception or we do not have Watson buckets.
        // Either case is a logic error becuase this function is called by WER only if the call
        // to OutOfProcessExceptionEventCallback() was successful and the value of
        // *pbOwnershipClaimed was TRUE.
        if (hr == S_FALSE)
        {
            hr = E_FAIL;
        }

        return hr;
    }

    DWORD paramCount = GetCountBucketParamsForEvent(gmb.wzEventTypeName);

    if (dwIndex >= paramCount)
    {
        _ASSERTE(!"dwIndex is out of range");
        return E_INVALIDARG;
    }

    // Return pwszName as an emptry string to let WER use localized version of "Parameter n"
    *pwszName = W('\0');

    if ((pwszValue == NULL) || (*pchValue <= u16_strlen(pwszBucketValues[dwIndex])))
    {
        *pchValue = static_cast<DWORD>(u16_strlen(pwszBucketValues[dwIndex]))+ 1;
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    // copy custom Watson bucket value
    wcscpy_s(pwszValue, *pchValue, pwszBucketValues[dwIndex]);

    return S_OK;
}

#endif // TARGET_UNIX

//----------------------------------------------------------------------------
//
// OutOfProcessExceptionEventCallback - provide custom debugger launch string
//
// Arguments:
//    pContext - the context passed at helper module registration
//    pExceptionInformation - structure that contains information about the crash
//    pbCustomDebuggerNeeded - pointer to a BOOL. If this BOOL is set to TRUE, then
//                             a custom debugger launch option is needed by the
//                             process. In that case, the subsequent parameters will
//                             be meaningfully used. If this is FALSE, the subsequent
//                             parameters will be ignored.
//    pwszDebuggerLaunch - pointer to a string that will be used to launch the debugger,
//                         if the debugger is launched. The value of this string overrides
//                         the default debugger launch string used by WER.
//    pchSize - pointer to the character count of the pwszDebuggerLaunch  buffer. If
//              pwszDebuggerLaunch points to null, *pchSize represents the buffer size
//              (represented in number of characters) needed to populate the debugger
//              launch string in pwszDebuggerLaunch.
//    pbAutoLaunchDebugger - pointer to a BOOL. If this BOOL is set to TRUE, WER will
//                           directly launch the debugger. If set to FALSE, WER will show
//                           the debug option to the user in the WER UI.
//
// Return Value:
//    S_OK on success, else detailed error code.
//
// Note:
//    This function is called into by WER only if the call to OutOfProcessExceptionEventCallback()
//    was successful and the value of *pbOwnershipClaimed was TRUE. This function allows the helper
//    dll to customize the debugger launch options including the launch string.
//
//    This function also follows the multiple call paradigm as described for the
//    OutOfProcessExceptionEventCallback() function. The buffer sizes needed for
//    this function are of the pwszName and pwszValue buffers.
//
//----------------------------------------------------------------------------
STDAPI OutOfProcessExceptionEventDebuggerLaunchCallback(_In_ PDWORD pContext,
                                                        _In_ const PWER_RUNTIME_EXCEPTION_INFORMATION pExceptionInformation,
                                                        _Out_ BOOL * pbCustomDebuggerNeeded,
                                                        _Out_writes_opt_(*pchSize) PWSTR pwszDebuggerLaunch,
                                                        __inout PDWORD pchSize,
                                                        _Out_ BOOL * pbAutoLaunchDebugger)
{
    SUPPORTS_DAC_HOST_ONLY;

    if ((pContext == NULL) ||
        (pExceptionInformation == NULL) ||
        (pExceptionInformation->dwSize < sizeof(WER_RUNTIME_EXCEPTION_INFORMATION)) ||
        (pbCustomDebuggerNeeded == NULL) ||
        (pwszDebuggerLaunch == NULL) ||
        (pchSize == NULL) ||
        (pbAutoLaunchDebugger == NULL))
    {
        return E_INVALIDARG;
    }

    // Starting from CLRv4 managed debugger string and setting are unified with native debuggers.
    // There is no need to provide custom debugger string for WER.
    *pbCustomDebuggerNeeded = FALSE;

    return S_OK;
}

// DacHandleEnum

#include "comcallablewrapper.h"

DacHandleWalker::DacHandleWalker()
    : mDac(0),  m_instanceAge(0),  mTypeMask(0), mGenerationFilter(-1), mEnumerated(false), mIteratorIndex(0)
{
    SUPPORTS_DAC;
}

DacHandleWalker::~DacHandleWalker()
{
    SUPPORTS_DAC;
}

HRESULT DacHandleWalker::Init(ClrDataAccess *dac, UINT types[], UINT typeCount)
{
    SUPPORTS_DAC;

    if (dac == NULL || types == NULL)
        return E_POINTER;

    mDac = dac;
    m_instanceAge = dac->m_instanceAge;

    return Init(BuildTypemask(types, typeCount));
}

HRESULT DacHandleWalker::Init(ClrDataAccess *dac, UINT types[], UINT typeCount, int gen)
{
    SUPPORTS_DAC;

    if (gen < 0 || gen > (int)*g_gcDacGlobals->max_gen)
        return E_INVALIDARG;

    mGenerationFilter = gen;

    return Init(dac, types, typeCount);
}

HRESULT DacHandleWalker::Init(UINT32 typemask)
{
    SUPPORTS_DAC;

    mTypeMask = typemask;

    return S_OK;
}

UINT32 DacHandleWalker::BuildTypemask(UINT types[], UINT typeCount)
{
    SUPPORTS_DAC;

    UINT32 mask = 0;

    for (UINT i = 0; i < typeCount; ++i)
    {
        _ASSERTE(types[i] < 32);
        mask |= (1 << types[i]);
    }

    return mask;
}

HRESULT DacHandleWalker::Next(unsigned int count,
             SOSHandleData handles[],
             unsigned int *pFetched)
{
    SUPPORTS_DAC;

    if (handles == NULL || pFetched == NULL)
        return E_POINTER;

    SOSHelperEnter();

    if (!mEnumerated)
        WalkHandles();

    unsigned int i = 0;
    while (i < count && mIteratorIndex < mList.GetCount())
    {
        handles[i++] = mList.Get(mIteratorIndex++);
    }

    *pFetched = i;

    hr = mIteratorIndex < mList.GetCount() ? S_FALSE : S_OK;

    SOSHelperLeave();

    return hr;
}

void DacHandleWalker::WalkHandles()
{
    SUPPORTS_DAC;

    if (mEnumerated)
        return;

    mEnumerated = true;

    // The table slots are based on the number of GC heaps in the process.
    int max_slots = 1;

#ifdef FEATURE_SVR_GC
    if (GCHeapUtilities::IsServerHeap())
        max_slots = GCHeapCount();
#endif // FEATURE_SVR_GC

    DacHandleWalkerParam param(&mList);


    for (dac_handle_table_map *map = g_gcDacGlobals->handle_table_map; SUCCEEDED(param.Result) && map; map = map->pNext)
    {
        for (int i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE && SUCCEEDED(param.Result); ++i)
        {
            if (map->pBuckets[i] != NULL)
            {
                for (int j = 0; j < max_slots && SUCCEEDED(param.Result); ++j)
                {
                    DPTR(dac_handle_table) hTable = map->pBuckets[i]->pTable[j];
                    if (hTable)
                    {
                        // handleType is the integer from 1 -> HANDLE_MAX_INTERNAL_TYPES based on the requested handle kinds to walk.
                        UINT32 handleType = 0;
                        for (UINT32 mask = mTypeMask; mask; mask >>= 1, handleType++)
                        {
                            if (mask & 1)
                            {
                                PTR_AppDomain pDomain = AppDomain::GetCurrentDomain();
                                param.AppDomain = TO_CDADDR(pDomain.GetAddr());
                                param.Type = handleType;

                                // Either enumerate the handles regularly, or walk the handle
                                // table as the GC does if a generation filter was requested.
                                if (mGenerationFilter != -1)
                                {
                                    HndScanHandlesForGC(hTable, EnumCallback, (LPARAM)&param, 0, &handleType, 1, mGenerationFilter, *g_gcDacGlobals->max_gen, 0);
                                }
                                else
                                {
                                    HndEnumHandles(hTable, &handleType, 1, EnumCallback, (LPARAM)&param, 0, false);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}


HRESULT DacHandleWalker::Skip(unsigned int count)
{
    mIteratorIndex += count;
    return S_OK;
}

HRESULT DacHandleWalker::Reset()
{
    mIteratorIndex = 0;
    return S_OK;
}

HRESULT DacHandleWalker::GetCount(unsigned int *pCount)
{
    if (!pCount)
        return E_POINTER;

    SOSHelperEnter();

    if (!mEnumerated)
        WalkHandles();

    *pCount = mList.GetCount();

    SOSHelperLeave();
    return hr;
}

void DacHandleWalker::GetRefCountedHandleInfo(
    OBJECTREF oref, unsigned int uType,
    unsigned int *pRefCount, unsigned int *pJupiterRefCount, BOOL *pIsPegged, BOOL *pIsStrong)
{
    SUPPORTS_DAC;

    if (pJupiterRefCount)
        *pJupiterRefCount = 0;

    if (pIsPegged)
        *pIsPegged = FALSE;

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS) || defined(FEATURE_OBJCMARSHAL)
    if (uType == HNDTYPE_REFCOUNTED)
    {
#if defined(FEATURE_COMINTEROP)
        // get refcount from the CCW
        PTR_ComCallWrapper pWrap = ComCallWrapper::GetWrapperForObject(oref);
        if (pWrap != NULL)
        {
            if (pRefCount)
                *pRefCount = (unsigned int)pWrap->GetRefCount();

            if (pIsStrong)
                *pIsStrong = pWrap->IsWrapperActive();

            return;
        }
#endif
#if defined(FEATURE_OBJCMARSHAL)
        // [TODO] FEATURE_OBJCMARSHAL
#endif // FEATURE_OBJCMARSHAL
    }
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS || FEATURE_OBJCMARSHAL

    if (pRefCount)
        *pRefCount = 0;

    if (pIsStrong)
        *pIsStrong = FALSE;
}

void CALLBACK DacHandleWalker::EnumCallback(PTR_UNCHECKED_OBJECTREF handle, uintptr_t *pExtraInfo, uintptr_t param1, uintptr_t param2)
{
    SUPPORTS_DAC;

    DacHandleWalkerParam *param = (DacHandleWalkerParam *)param1;
    DacReferenceList<SOSHandleData> *list = param->List;

    // If we failed on a previous call (OOM) don't keep trying to allocate, it's not going to work.
    if (FAILED(param->Result) || !list)
        return;

    // Fill the current handle.
    SOSHandleData data = {0};

    data.Handle = TO_CDADDR(handle.GetAddr());
    data.Type = param->Type;
    if (param->Type == HNDTYPE_DEPENDENT)
        data.Secondary = GetDependentHandleSecondary(handle.GetAddr()).GetAddr();
    else
        data.Secondary = 0;
    data.AppDomain = param->AppDomain;
    GetRefCountedHandleInfo((OBJECTREF)*handle, param->Type, &data.RefCount, &data.JupiterRefCount, &data.IsPegged, &data.StrongReference);
    data.StrongReference |= (BOOL)IsAlwaysStrongReference(param->Type);

    if (!list->Add(data))
        param->Result = E_OUTOFMEMORY;
}

DacStackReferenceWalker::DacStackReferenceWalker(ClrDataAccess *dac, DWORD osThreadID, bool resolveInteriorPointers)
    : mDac(dac), m_instanceAge(dac ? dac->m_instanceAge : 0), mThread(0), mErrors(0),
      mResolvePointers(resolveInteriorPointers), mEnumerated(false), mIteratorIndex(0)
{
    Thread *curr = NULL;

    for (curr = ThreadStore::GetThreadList(curr);
         curr;
         curr = ThreadStore::GetThreadList(curr))
     {
        if (curr->GetOSThreadId() == osThreadID)
        {
            mThread = curr;
            break;
        }
     }
}

HRESULT DacStackReferenceWalker::Init()
{
    if (!mThread)
        return E_INVALIDARG;

    if (mResolvePointers)
        return mHeap.Init();

    return S_OK;
}

HRESULT STDMETHODCALLTYPE DacStackReferenceWalker::Skip(unsigned int count)
{
    mIteratorIndex += count;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE DacStackReferenceWalker::Reset()
{
    mIteratorIndex = 0;
    return S_OK;
}

HRESULT DacStackReferenceWalker::GetCount(unsigned int *pCount)
{
    if (!pCount)
        return E_POINTER;

    SOSHelperEnter();

    if (!mEnumerated)
        WalkStack();

    *pCount = mList.GetCount();

    SOSHelperLeave();
    return hr;
}

HRESULT DacStackReferenceWalker::Next(unsigned int count,
                                      SOSStackRefData stackRefs[],
                                      unsigned int *pFetched)
{
    if (stackRefs == NULL || pFetched == NULL)
        return E_POINTER;

    SOSHelperEnter();

    if (!mEnumerated)
        WalkStack();

    unsigned int i = 0;
    while (i < count && mIteratorIndex < mList.GetCount())
    {
        stackRefs[i++] = mList.Get(mIteratorIndex++);
    }

    *pFetched = i;

    hr = mIteratorIndex < mList.GetCount() ? S_FALSE : S_OK;

    SOSHelperLeave();
    return hr;
}


void DacStackReferenceWalker::WalkStack()
{
    if (mEnumerated)
        return;

    _ASSERTE(mList.GetCount() == 0);
    _ASSERTE(mThread);

    class ProfilerFilterContextHolder
    {
        Thread* m_pThread;

    public:
        ProfilerFilterContextHolder() : m_pThread(NULL)
        {
        }

        void Activate(Thread* pThread)
        {
            m_pThread = pThread;
        }

        ~ProfilerFilterContextHolder()
        {
            if (m_pThread != NULL)
                m_pThread->SetProfilerFilterContext(NULL);
        }
    };

    ProfilerFilterContextHolder contextHolder;
    T_CONTEXT ctx;

    // Get the current thread's context and set that as the filter context
    if (mThread->GetFilterContext() == NULL && mThread->GetProfilerFilterContext() == NULL)
    {
        mDac->m_pTarget->GetThreadContext(mThread->GetOSThreadId(), CONTEXT_FULL, sizeof(DT_CONTEXT), (BYTE*)&ctx);
        mThread->SetProfilerFilterContext(&ctx);
        contextHolder.Activate(mThread);
    }

    // Setup GCCONTEXT structs for the stackwalk.
    GCCONTEXT gcctx = {0};
    DacScanContext dsc(this, &mList, mResolvePointers);
    dsc.pEnumFunc = DacStackReferenceWalker::GCEnumCallback;
    gcctx.f = DacStackReferenceWalker::GCReportCallback;
    gcctx.sc = &dsc;

    // Walk the stack, set mEnumerated to true to ensure we don't do it again.
    unsigned int flagsStackWalk = ALLOW_INVALID_OBJECTS|ALLOW_ASYNC_STACK_WALK|SKIP_GSCOOKIE_CHECK;

#if defined(FEATURE_EH_FUNCLETS)
    flagsStackWalk |= GC_FUNCLET_REFERENCE_REPORTING;
#endif // defined(FEATURE_EH_FUNCLETS)

    // Pre-set mEnumerated in case we hit an unexpected exception.  We don't want to
    // keep walking stack frames if we hit a failure
    mEnumerated = true;
    mThread->StackWalkFrames(DacStackReferenceWalker::Callback, &gcctx, flagsStackWalk);
}

HRESULT DacStackReferenceWalker::EnumerateErrors(ISOSStackRefErrorEnum **ppEnum)
{
    if (!ppEnum)
        return E_POINTER;

    SOSHelperEnter();

    if (!mEnumerated)
        WalkStack();

    DacStackReferenceErrorEnum *pEnum = new DacStackReferenceErrorEnum(this, mErrors);
    hr = pEnum->QueryInterface(__uuidof(ISOSStackRefErrorEnum), (void**)ppEnum);

    SOSHelperLeave();
    return hr;
}

CLRDATA_ADDRESS DacStackReferenceWalker::ReadPointer(TADDR addr)
{
    ULONG32 bytesRead = 0;
    TADDR result = 0;
    HRESULT hr = mDac->m_pTarget->ReadVirtual(addr, (BYTE*)&result, sizeof(TADDR), &bytesRead);

    if (FAILED(hr) || (bytesRead != sizeof(TADDR)))
        return (CLRDATA_ADDRESS)~0;

    return TO_CDADDR(result);
}


void DacStackReferenceWalker::GCEnumCallback(LPVOID hCallback, OBJECTREF *pObject, uint32_t flags, DacSlotLocation loc)
{
    GCCONTEXT *gcctx = (GCCONTEXT *)hCallback;
    DacScanContext *dsc = (DacScanContext*)gcctx->sc;

    if (dsc->pList == nullptr)
        return;

    // Yuck.  The GcInfoDecoder reports a local pointer for registers (as it's reading out of the REGDISPLAY
    // in the stack walk), and it reports a TADDR for stack locations.  This is architecturally difficulty
    // to fix, so we are leaving it for now.
    TADDR addr = 0;
    TADDR obj = 0;

    if (loc.targetPtr)
    {
        addr = (TADDR)pObject;
        obj = TO_TADDR(dsc->pWalker->ReadPointer((CORDB_ADDRESS)addr));
    }
    else
    {
        obj = pObject->GetAddr();
    }

    if (flags & GC_CALL_INTERIOR && dsc->resolvePointers)
    {
        CORDB_ADDRESS fixed_obj = 0;
        HRESULT hr = dsc->pWalker->mHeap.ListNearObjects((CORDB_ADDRESS)obj, NULL, &fixed_obj, NULL);

        // Interior pointers need not lie on the manage heap at all.  When this happens, ListNearObjects
        // will return E_FAIL.  In this case, we need to be sure to not include this stack slot in our
        // enumeration because ICorDebug expects every location enumerated by this API to point to a
        // valid object.
        if (FAILED(hr))
            return;

        obj = TO_TADDR(fixed_obj);
    }

    // Report the object and where it was found.
    SOSStackRefData data = {0};

    data.HasRegisterInformation = true;
    data.Register = loc.reg;
    data.Offset = loc.regOffset;
    data.Address = TO_CDADDR(addr);
    data.Object = TO_CDADDR(obj);
    data.Flags = flags;

    // Report the frame that the data came from.
    data.StackPointer = TO_CDADDR(dsc->sp);

    if (dsc->pFrame)
    {
        data.SourceType = SOS_StackSourceFrame;
        data.Source = dac_cast<PTR_Frame>(dsc->pFrame).GetAddr();
    }
    else
    {
        data.SourceType = SOS_StackSourceIP;
        data.Source = TO_CDADDR(dsc->pc);
    }

    dsc->pList->Add(data);
}

void DacStackReferenceWalker::GCReportCallback(PTR_PTR_Object ppObj, ScanContext *sc, uint32_t flags)
{
    DacScanContext *dsc = (DacScanContext*)sc;
    CLRDATA_ADDRESS obj = dsc->pWalker->ReadPointer(ppObj.GetAddr());

    if (dsc->pList == nullptr)
        return;

    if (flags & GC_CALL_INTERIOR && dsc->resolvePointers)
    {
        CORDB_ADDRESS fixed_addr = 0;
        HRESULT hr = dsc->pWalker->mHeap.ListNearObjects((CORDB_ADDRESS)obj, NULL, &fixed_addr, NULL);

        // If we failed...oh well, SOS won't mind.  We'll just report the interior pointer as is.
        if (SUCCEEDED(hr))
            obj = TO_CDADDR(fixed_addr);
    }

    SOSStackRefData data = {0};
    data.HasRegisterInformation = false;
    data.Register = 0;
    data.Offset = 0;
    data.Address = ppObj.GetAddr();
    data.Object = obj;
    data.Flags = flags;
    data.StackPointer = TO_CDADDR(dsc->sp);

    if (dsc->pFrame)
    {
        data.SourceType = SOS_StackSourceFrame;
        data.Source = dac_cast<PTR_Frame>(dsc->pFrame).GetAddr();
    }
    else
    {
        data.SourceType = SOS_StackSourceIP;
        data.Source = TO_CDADDR(dsc->pc);
    }

    dsc->pList->Add(data);
}

StackWalkAction DacStackReferenceWalker::Callback(CrawlFrame *pCF, VOID *pData)
{
    //
    // KEEP IN SYNC WITH GcStackCrawlCallBack in vm\gcscan.cpp
    //

    GCCONTEXT *gcctx = (GCCONTEXT*)pData;
    DacScanContext *dsc = (DacScanContext*)gcctx->sc;

    MethodDesc *pMD = pCF->GetFunction();
    gcctx->sc->pMD = pMD;

    PREGDISPLAY pRD = pCF->GetRegisterSet();
    dsc->sp = (TADDR)GetRegdisplaySP(pRD);;
    dsc->pc = PCODEToPINSTR(GetControlPC(pRD));

    ResetPointerHolder<CrawlFrame*> rph(&gcctx->cf);
    gcctx->cf = pCF;

    bool fReportGCReferences = true;
#if defined(FEATURE_EH_FUNCLETS)
    // On Win64 and ARM, we may have unwound this crawlFrame and thus, shouldn't report the invalid
    // references it may contain.
    // todo.
    fReportGCReferences = pCF->ShouldCrawlframeReportGCReferences();
#endif // defined(FEATURE_EH_FUNCLETS)

    Frame *pFrame = ((DacScanContext*)gcctx->sc)->pFrame = pCF->GetFrame();

    EX_TRY
    {
        if (fReportGCReferences)
        {
            if (pCF->IsFrameless())
            {
                ICodeManager * pCM = pCF->GetCodeManager();
                _ASSERTE(pCM != NULL);

                unsigned flags = pCF->GetCodeManagerFlags();

                pCM->EnumGcRefs(pCF->GetRegisterSet(),
                                pCF->GetCodeInfo(),
                                flags,
                                dsc->pEnumFunc,
                                pData);
            }
            else
            {
                pFrame->GcScanRoots(gcctx->f, gcctx->sc);
            }
        }
    }
    EX_CATCH
    {
        SOSStackErrorList *err = new SOSStackErrorList;
        err->pNext = NULL;

        if (pFrame)
        {
            err->error.SourceType = SOS_StackSourceFrame;
            err->error.Source = dac_cast<PTR_Frame>(pFrame).GetAddr();
        }
        else
        {
            err->error.SourceType = SOS_StackSourceIP;
            err->error.Source = TO_CDADDR(dsc->pc);
        }

        if (dsc->pWalker->mErrors == NULL)
        {
            dsc->pWalker->mErrors = err;
        }
        else
        {
            // This exception case should be non-existent.  It only happens when there is either
            // a clr!Frame on the callstack which is not properly dac-ized, or when a call down
            // EnumGcRefs causes a data read exception.  Since this is so rare, we don't worry
            // about making this code very efficient.
            SOSStackErrorList *curr = dsc->pWalker->mErrors;
            while (curr->pNext)
                curr = curr->pNext;

            curr->pNext = err;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

#if 0
    // todo

    // If we're executing a LCG dynamic method then we must promote the associated resolver to ensure it
    // doesn't get collected and yank the method code out from under us).

    // Be careful to only promote the reference -- we can also be called to relocate the reference and
    // that can lead to all sorts of problems since we could be racing for the relocation with the long
    // weak handle we recover the reference from. Promoting the reference is enough, the handle in the
    // reference will be relocated properly as long as we keep it alive till the end of the collection
    // as long as the reference is actually maintained by the long weak handle.
    if (pMD)
    {
        BOOL fMaybeCollectibleMethod = TRUE;

        // If this is a frameless method then the jitmanager can answer the question of whether
        // or not this is LCG simply by looking at the heap where the code lives, however there
        // is also the prestub case where we need to explicitly look at the MD for stuff that isn't
        // ngen'd
        if (pCF->IsFrameless() && pMD->IsLCGMethod())
        {
            fMaybeCollectibleMethod = ExecutionManager::IsCollectibleMethod(pCF->GetMethodToken());
        }

        if (fMaybeCollectibleMethod && pMD->IsLCGMethod())
        {
            PTR_Object obj = OBJECTREFToObject(pMD->AsDynamicMethodDesc()->GetLCGMethodResolver()->GetManagedResolver());
            dsc->pWalker->ReportObject(obj);
        }
        else
        {
            if (fMaybeCollectibleMethod)
            {
                PTR_Object obj = pMD->GetLoaderAllocator()->GetExposedObject();
                dsc->pWalker->ReportObject(obj);
            }

            if (fReportGCReferences)
            {
                GenericParamContextType paramContextType = GENERIC_PARAM_CONTEXT_NONE;

                if (pCF->IsFrameless())
                {
                    // We need to grab the Context Type here because there are cases where the MethodDesc
                    // is shared, and thus indicates there should be an instantion argument, but the JIT
                    // was still allowed to optimize it away and we won't grab it below because we're not
                    // reporting any references from this frame.
                    paramContextType = pCF->GetCodeManager()->GetParamContextType(pCF->GetRegisterSet(), pCF->GetCodeInfo());
                }
                else
                {
                    if (pMD->RequiresInstMethodDescArg())
                        paramContextType = GENERIC_PARAM_CONTEXT_METHODDESC;
                    else if (pMD->RequiresInstMethodTableArg())
                        paramContextType = GENERIC_PARAM_CONTEXT_METHODTABLE;
                }

                // Handle the case where the method is a static shared generic method and we need to keep the type of the generic parameters alive
                if (paramContextType == GENERIC_PARAM_CONTEXT_METHODDESC)
                {
                    MethodDesc *pMDReal = dac_cast<PTR_MethodDesc>(pCF->GetParamTypeArg());
                    _ASSERTE((pMDReal != NULL) || !pCF->IsFrameless());
                    if (pMDReal != NULL)
                    {
                        PTR_Object obj = pMDReal->GetLoaderAllocator()->GetExposedObject();
                        dsc->pWalker->ReportObject(obj);
                    }
                }
                else if (paramContextType == GENERIC_PARAM_CONTEXT_METHODTABLE)
                {
                    MethodTable *pMTReal = dac_cast<PTR_MethodTable>(pCF->GetParamTypeArg());
                    _ASSERTE((pMTReal != NULL) || !pCF->IsFrameless());
                    if (pMTReal != NULL)
                    {
                        PTR_Object obj = pMTReal->GetLoaderAllocator()->GetExposedObject();
                        dsc->pWalker->ReportObject(obj);
                    }
                }
            }
        }
    }
#endif

    return SWA_CONTINUE;
}


DacStackReferenceErrorEnum::DacStackReferenceErrorEnum(DacStackReferenceWalker *pEnum, SOSStackErrorList *pErrors)
    : mEnum(pEnum), mHead(pErrors), mCurr(pErrors)
{
    _ASSERTE(mEnum);

    if (mHead != NULL)
        mEnum->AddRef();
}

DacStackReferenceErrorEnum::~DacStackReferenceErrorEnum()
{
    if (mHead)
        mEnum->Release();
}

HRESULT DacStackReferenceErrorEnum::Skip(unsigned int count)
{
    unsigned int i = 0;
    for (i = 0; i < count && mCurr; ++i)
        mCurr = mCurr->pNext;

    return i < count ? S_FALSE : S_OK;
}

HRESULT DacStackReferenceErrorEnum::Reset()
{
    mCurr = mHead;

    return S_OK;
}

HRESULT DacStackReferenceErrorEnum::GetCount(unsigned int *pCount)
{
    SOSStackErrorList *curr = mHead;
    unsigned int count = 0;

    while (curr)
    {
        curr = curr->pNext;
        count++;
    }

    *pCount = count;
    return S_OK;
}

HRESULT DacStackReferenceErrorEnum::Next(unsigned int count, SOSStackRefError ref[], unsigned int *pFetched)
{
    if (pFetched == NULL || ref == NULL)
        return E_POINTER;

    unsigned int i;
    for (i = 0; i < count && mCurr; ++i, mCurr = mCurr->pNext)
        ref[i] = mCurr->error;

    *pFetched = i;
    return i < count ? S_FALSE : S_OK;
}


HRESULT DacMemoryEnumerator::Skip(unsigned int count)
{
    mIteratorIndex += count;
    return S_OK;
}

HRESULT DacMemoryEnumerator::Reset()
{
    mIteratorIndex = 0;
    return S_OK;
}

HRESULT DacMemoryEnumerator::GetCount(unsigned int* pCount)
{
    if (!pCount)
        return E_POINTER;

    mRegions.GetCount();
    return S_OK;
}

HRESULT DacMemoryEnumerator::Next(unsigned int count, SOSMemoryRegion regions[], unsigned int* pFetched)
{
    if (!pFetched)
        return E_POINTER;

    if (!regions)
        return E_POINTER;

    unsigned int i = 0;
    while (i < count && mIteratorIndex < mRegions.GetCount())
    {
        regions[i++] = mRegions.Get(mIteratorIndex++);
    }

    *pFetched = i;
    return i < count ? S_FALSE : S_OK;
}


HRESULT DacGCBookkeepingEnumerator::Init()
{
    if (g_gcDacGlobals->bookkeeping_start == nullptr)
        return E_FAIL;

    TADDR ctiAddr = TO_TADDR(*g_gcDacGlobals->bookkeeping_start);
    if (ctiAddr == 0)
        return E_FAIL;

    DPTR(dac_card_table_info) card_table_info(ctiAddr);

    SOSMemoryRegion mem = {0};
    if (card_table_info->recount && card_table_info->size)
    {
        mem.Start = card_table_info.GetAddr();
        mem.Size = card_table_info->size;
        mRegions.Add(mem);
    }

    size_t card_table_info_size = g_gcDacGlobals->card_table_info_size;
    TADDR next = card_table_info->next_card_table;

    // Cap the number of regions we will walk in case we have run into some kind of
    // memory corruption.  We shouldn't have more than a few linked card tables anyway.
    int maxRegions = 32;

    // This loop is effectively "while (next != 0)" but with an added check to make
    // sure we don't underflow next when subtracting card_table_info_size if we encounter
    // a bad pointer.
    while (next > card_table_info_size)
    {
        DPTR(dac_card_table_info) ct(next - card_table_info_size);

        if (ct->recount && ct->size)
        {
            mem = {0};
            mem.Start = ct.GetAddr();
            mem.Size = ct->size;
            mRegions.Add(mem);
        }

        next = ct->next_card_table;
        if (next == card_table_info->next_card_table)
            break;

        if (--maxRegions <= 0)
            break;
    }

    return S_OK;
}


HRESULT DacHandleTableMemoryEnumerator::Init()
{
    int max_slots = 1;

#ifdef FEATURE_SVR_GC
    if (GCHeapUtilities::IsServerHeap())
        max_slots = GCHeapCount();
#endif // FEATURE_SVR_GC

    // Cap the number of regions we will walk in case we hit an infinite loop due
    // to memory corruption
    int maxRegions = 8192;

    for (dac_handle_table_map *map = g_gcDacGlobals->handle_table_map; map && maxRegions >= 0; map = map->pNext, maxRegions--)
    {
        for (int i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; ++i)
        {
            if (map->pBuckets[i] != NULL)
            {
                for (int j = 0; j < max_slots ; ++j)
                {
                    DPTR(dac_handle_table) pTable = map->pBuckets[i]->pTable[j];
                    DPTR(dac_handle_table_segment) pFirstSegment = pTable->pSegmentList;
                    DPTR(dac_handle_table_segment) curr = pFirstSegment;

                    do
                    {
                        SOSMemoryRegion mem = {0};
                        mem.Start = curr.GetAddr();
                        mem.Size = HANDLE_SEGMENT_SIZE;
                        mem.Heap = j; // heap number

                        mRegions.Add(mem);

                        curr = curr->pNextSegment;
                    } while (curr != nullptr && curr != pFirstSegment);
                }
            }
        }
    }

    return S_OK;
}

void DacFreeRegionEnumerator::AddSingleSegment(const dac_heap_segment &curr, FreeRegionKind kind, int heap)
{
    SOSMemoryRegion mem = {0};
    mem.Start = TO_CDADDR(curr.mem);
    mem.ExtraData = (CLRDATA_ADDRESS)kind;
    mem.Heap = heap;

    if (curr.mem < curr.committed)
        mem.Size = TO_CDADDR(curr.committed) - mem.Start;

    if (mem.Start)
        mRegions.Add(mem);
}

void DacFreeRegionEnumerator::AddSegmentList(DPTR(dac_heap_segment) start, FreeRegionKind kind, int heap)
{
    int iterationMax = 2048;

    DPTR(dac_heap_segment) curr = start;
    while (curr != nullptr)
    {
        AddSingleSegment(*curr, kind, heap);

        curr = curr->next;
        if (curr == start)
            break;

        if (iterationMax-- <= 0)
            break;
    }
}

void DacFreeRegionEnumerator::AddFreeList(DPTR(dac_region_free_list) free_list, FreeRegionKind kind)
{
    if (free_list != nullptr)
    {
        AddSegmentList(free_list->head_free_region, kind);
    }
}

HRESULT DacFreeRegionEnumerator::Init()
{
    // Cap the number of free regions we will walk at a sensible number.  This is to protect against
    // memory corruption, un-initialized data, or just a bug.
    int count_free_region_kinds = g_gcDacGlobals->count_free_region_kinds;
    count_free_region_kinds = min(count_free_region_kinds, 16);

    unsigned int index = 0;
    if (g_gcDacGlobals->global_free_huge_regions != nullptr)
    {
        DPTR(dac_region_free_list) global_free_huge_regions(g_gcDacGlobals->global_free_huge_regions);
        AddFreeList(global_free_huge_regions, FreeRegionKind::FreeGlobalHugeRegion);
    }

    if (g_gcDacGlobals->global_regions_to_decommit != nullptr)
    {
        DPTR(dac_region_free_list) regionList(g_gcDacGlobals->global_regions_to_decommit);
        if (regionList != nullptr)
            for (int i = 0; i < count_free_region_kinds; i++, regionList++)
                AddFreeList(regionList, FreeRegionKind::FreeGlobalRegion);
    }

#if defined(FEATURE_SVR_GC)
    if (GCHeapUtilities::IsServerHeap())
    {
        AddServerRegions();
    }
    else
#endif //FEATURE_SVR_GC
    {
        DPTR(dac_region_free_list) regionList(g_gcDacGlobals->free_regions);
        if (regionList != nullptr)
            for (int i = 0; i < count_free_region_kinds; i++, regionList++)
                AddFreeList(regionList, FreeRegionKind::FreeRegion);

        if (g_gcDacGlobals->freeable_soh_segment != nullptr)
        {
            DPTR(DPTR(dac_heap_segment)) freeable_soh_segment_ptr(g_gcDacGlobals->freeable_soh_segment);
            if (freeable_soh_segment_ptr != nullptr)
                AddSegmentList(*freeable_soh_segment_ptr, FreeRegionKind::FreeSohSegment);
        }

        if (g_gcDacGlobals->freeable_uoh_segment != nullptr)
        {
            DPTR(DPTR(dac_heap_segment)) freeable_uoh_segment_ptr(g_gcDacGlobals->freeable_uoh_segment);
            if (freeable_uoh_segment_ptr != nullptr)
                AddSegmentList(*freeable_uoh_segment_ptr, FreeRegionKind::FreeUohSegment);
        }
    }

    return S_OK;
}
