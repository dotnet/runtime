// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// NamespaceUtil.cpp
//

//
// Helpers for converting namespace separators.
//
//*****************************************************************************
#include "stdafx.h"
#include "corhdr.h"
#include "corhlpr.h"
#include "sstring.h"
#include "utilcode.h"

#ifndef _ASSERTE
#define _ASSERTE(foo)
#endif

#include "nsutilpriv.h"


//*****************************************************************************
// Determine how many chars large a fully qualified name would be given the
// two parts of the name.  The return value includes room for every character
// in both names, as well as room for the separator and a final terminator.
//*****************************************************************************
int ns::GetFullLength(                  // Number of chars in full name.
    const WCHAR *szNameSpace,           // Namspace for value.
    const WCHAR *szName)                // Name of value.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    int iLen = 1;                       // Null terminator.
    if (szNameSpace)
        iLen += (int)u16_strlen(szNameSpace);
    if (szName)
        iLen += (int)u16_strlen(szName);
    if (szNameSpace && *szNameSpace && szName && *szName)
        ++iLen;
    return iLen;
}   //int ns::GetFullLength()

int ns::GetFullLength(                  // Number of chars in full name.
    LPCUTF8     szNameSpace,            // Namspace for value.
    LPCUTF8     szName)                 // Name of value.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;


    int iLen = 1;
    if (szNameSpace)
        iLen += (int)strlen(szNameSpace);
    if (szName)
        iLen += (int)strlen(szName);
    if (szNameSpace && *szNameSpace && szName && *szName)
        ++iLen;
    return iLen;
}   //int ns::GetFullLength()


//*****************************************************************************
// Scan the string from the rear looking for the first valid separator.  If
// found, return a pointer to it.  Else return null.  This code is smart enough
// to skip over special sequences, such as:
//      a.b..ctor
//         ^
//         |
// The ".ctor" is considered one token.
//*****************************************************************************
WCHAR *ns::FindSep(                     // Pointer to separator or null.
    const WCHAR *szPath)                // The path to look in.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE(szPath);
    WCHAR *ptr = (WCHAR*)u16_strrchr(szPath, NAMESPACE_SEPARATOR_WCHAR);
    if((ptr == NULL) || (ptr == szPath)) return NULL;
    if(*(ptr - 1) == NAMESPACE_SEPARATOR_WCHAR) // here ptr is at least szPath+1
        --ptr;
    return ptr;
}   //WCHAR *ns::FindSep()

//<TODO>@todo: this isn't dbcs safe if this were ansi, but this is utf8.  Still an issue?</TODO>
LPUTF8 ns::FindSep(                     // Pointer to separator or null.
    LPCUTF8     szPath)                 // The path to look in.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    _ASSERTE(szPath);
    LPUTF8 ptr = const_cast<LPUTF8>(strrchr(szPath, NAMESPACE_SEPARATOR_CHAR));
    if((ptr == NULL) || (ptr == szPath)) return NULL;
    if(*(ptr - 1) == NAMESPACE_SEPARATOR_CHAR) // here ptr is at least szPath+1
        --ptr;
    return ptr;
}   //LPUTF8 ns::FindSep()



//*****************************************************************************
// Take a path and find the last separator (nsFindSep), and then replace the
// separator with a '\0' and return a pointer to the name.  So for example:
//      a.b.c
// becomes two strings "a.b" and "c" and the return value points to "c".
//*****************************************************************************
WCHAR *ns::SplitInline(                 // Pointer to name portion.
    __inout __inout_z WCHAR       *szPath)           // The path to split.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    WCHAR *ptr = ns::FindSep(szPath);
    if (ptr)
    {
        *ptr = 0;
        ++ptr;
    }
    return ptr;
}   // WCHAR *ns::SplitInline()

LPUTF8 ns::SplitInline(                 // Pointer to name portion.
    __inout __inout_z LPUTF8  szPath)                 // The path to split.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    LPUTF8 ptr = ns::FindSep(szPath);
    if (ptr)
    {
        *ptr = 0;
        ++ptr;
    }
    return ptr;
}   // LPUTF8 ns::SplitInline()

void ns::SplitInline(
    __inout __inout_z LPWSTR  szPath,                 // Path to split.
    LPCWSTR     &szNameSpace,           // Return pointer to namespace.
    LPCWSTR     &szName)                // Return pointer to name.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    WCHAR *ptr = SplitInline(szPath);
    if (ptr)
    {
        szNameSpace = szPath;
        szName = ptr;
    }
    else
    {
        szNameSpace = 0;
        szName = szPath;
    }
}   // void ns::SplitInline()

void ns::SplitInline(
    __inout __inout_z LPUTF8  szPath,                 // Path to split.
    LPCUTF8     &szNameSpace,           // Return pointer to namespace.
    LPCUTF8     &szName)                // Return pointer to name.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    LPUTF8 ptr = SplitInline(szPath);
    if (ptr)
    {
        szNameSpace = szPath;
        szName = ptr;
    }
    else
    {
        szNameSpace = 0;
        szName = szPath;
    }
}   // void ns::SplitInline()


//*****************************************************************************
// Split the last parsable element from the end of the string as the name,
// the first part as the namespace.
//*****************************************************************************
int ns::SplitPath(                      // true ok, false trunction.
    const WCHAR *szPath,                // Path to split.
    _Out_writes_(cchNameSpace) WCHAR *szNameSpace,           // Output for namespace value.
    int         cchNameSpace,           // Max chars for output.
    _Out_writes_(cchName)      WCHAR *szName,                // Output for name.
    int         cchName)                // Max chars for output.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    const WCHAR *ptr = ns::FindSep(szPath);
    size_t iLen = (ptr) ? ptr - szPath : 0;
    size_t iCopyMax;
    int brtn = true;
    if (szNameSpace && cchNameSpace)
    {
        _ASSERTE(cchNameSpace > 1);
        iCopyMax = cchNameSpace - 1;
        iCopyMax = min(iCopyMax, iLen);
        wcsncpy_s(szNameSpace, cchNameSpace, szPath, iCopyMax);
        szNameSpace[iCopyMax] = 0;

        if (iLen >= (size_t)cchNameSpace)
            brtn = false;
    }

    if (szName && cchName)
    {
        _ASSERTE(cchName > 1);
        iCopyMax = cchName - 1;
        if (ptr)
            ++ptr;
        else
            ptr = szPath;
        iLen = (int)u16_strlen(ptr);
        iCopyMax = min(iCopyMax, iLen);
        wcsncpy_s(szName, cchName, ptr, iCopyMax);
        szName[iCopyMax] = 0;

        if (iLen >= (size_t)cchName)
            brtn = false;
    }
    return brtn;
}   // int ns::SplitPath()


int ns::SplitPath(                      // true ok, false trunction.
    LPCUTF8     szPath,                 // Path to split.
    _Out_writes_opt_ (cchNameSpace) LPUTF8      szNameSpace,            // Output for namespace value.
    int         cchNameSpace,           // Max chars for output.
    _Out_writes_opt_ (cchName) LPUTF8      szName,                 // Output for name.
    int         cchName)                // Max chars for output.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    LPCUTF8 ptr = ns::FindSep(szPath);
    size_t iLen = (ptr) ? ptr - szPath : 0;
    size_t iCopyMax;
    int brtn = true;
    if (szNameSpace && cchNameSpace)
    {
        _ASSERTE(cchNameSpace > 1);
        iCopyMax = cchNameSpace-1;
        iCopyMax = min(iCopyMax, iLen);
        strncpy_s(szNameSpace, cchNameSpace, szPath, iCopyMax);
        szNameSpace[iCopyMax] = 0;

        if (iLen >= (size_t)cchNameSpace)
            brtn = false;
    }

    if (szName && cchName)
    {
        _ASSERTE(cchName > 1);
        iCopyMax = cchName-1;
        if (ptr)
            ++ptr;
        else
            ptr = szPath;
        iLen = (int)strlen(ptr);
        iCopyMax = min(iCopyMax, iLen);
        strncpy_s(szName, cchName, ptr, iCopyMax);
        szName[iCopyMax] = 0;

        if (iLen >= (size_t)cchName)
            brtn = false;
    }
    return brtn;
}   // int ns::SplitPath()


//*****************************************************************************
// Take two values and put them together in a fully qualified path using the
// correct separator.
//*****************************************************************************
int ns::MakePath(                       // true ok, false truncation.
    _Out_writes_(cchChars) WCHAR       *szOut,                 // output path for name.
    int         cchChars,               // max chars for output path.
    const WCHAR *szNameSpace,           // Namespace.
    const WCHAR *szName)                // Name.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (cchChars < 1)
        return false;

    if (szOut)
        *szOut = 0;
    else
        return false;

    if (szNameSpace && *szNameSpace != W('\0'))
    {
        if (wcsncpy_s(szOut, cchChars, szNameSpace, _TRUNCATE) == STRUNCATE)
            return false;

        // Add namespace separator if a non-empty name was supplied
        if (szName && *szName != W('\0'))
        {
            if (wcsncat_s(szOut, cchChars, NAMESPACE_SEPARATOR_WSTR, _TRUNCATE) == STRUNCATE)
            {
                return false;
            }
        }
    }

    if (szName && *szName)
    {
        if (wcsncat_s(szOut, cchChars, szName, _TRUNCATE) == STRUNCATE)
            return false;
    }

    return true;
}   // int ns::MakePath()

int ns::MakePath(                       // true ok, false truncation.
    _Out_writes_(cchChars) LPUTF8      szOut,                  // output path for name.
    int         cchChars,               // max chars for output path.
    LPCUTF8     szNameSpace,            // Namespace.
    LPCUTF8     szName)                 // Name.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (cchChars < 1)
        return false;

    if (szOut)
        *szOut = 0;
    else
        return false;

    if (szNameSpace && *szNameSpace != W('\0'))
    {
        if (strncpy_s(szOut, cchChars, szNameSpace, _TRUNCATE) == STRUNCATE)
            return false;

        // Add namespace separator if a non-empty name was supplied
        if (szName && *szName != W('\0'))
        {
            if (strncat_s(szOut, cchChars, NAMESPACE_SEPARATOR_STR, _TRUNCATE) == STRUNCATE)
            {
                return false;
            }
        }
    }

    if (szName && *szName)
    {
        if (strncat_s(szOut, cchChars, szName, _TRUNCATE) == STRUNCATE)
            return false;
    }

    return true;

}   // int ns::MakePath()

int ns::MakePath(                       // true ok, false truncation.
    _Out_writes_(cchChars) WCHAR       *szOut,                 // output path for name.
    int         cchChars,               // max chars for output path.
    LPCUTF8     szNamespace,            // Namespace.
    LPCUTF8     szName)                 // Name.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (cchChars < 1)
        return false;

    if (szOut)
        *szOut = 0;
    else
        return false;

    if (szNamespace != NULL && *szNamespace != '\0')
    {
        if (cchChars < 2)
            return false;

        int count;

        // We use cBuffer - 2 to account for the '.' and at least a 1 character name below.
        count = WszMultiByteToWideChar(CP_UTF8, 0, szNamespace, -1, szOut, cchChars-2);
        if (count == 0)
            return false; // Supply a bigger buffer!

        // buffer access is bounded: WszMultiByteToWideChar returns 0 if access doesn't fit in range
#ifdef _PREFAST_
        #pragma warning( suppress: 26015 )
#endif
        szOut[count-1] = NAMESPACE_SEPARATOR_WCHAR;
        szOut += count;
        cchChars -= count;
    }

    if (((cchChars == 0) && (szName != NULL) && (*szName != '\0')) ||
        (WszMultiByteToWideChar(CP_UTF8, 0, szName, -1, szOut, cchChars) == 0))
        return false; // supply a bigger buffer!
    return true;
}   // int ns::MakePath()

int ns::MakePath(                       // true ok, false out of memory
    CQuickBytes &qb,                    // Where to put results.
    LPCUTF8     szNameSpace,            // Namespace for name.
    LPCUTF8     szName)                 // Final part of name.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    int iLen = 2;
    if (szNameSpace)
        iLen += (int)strlen(szNameSpace);
    if (szName)
        iLen += (int)strlen(szName);
    LPUTF8 szOut = (LPUTF8) qb.AllocNoThrow(iLen);
    if (!szOut)
        return false;
    return ns::MakePath(szOut, iLen, szNameSpace, szName);
}   // int ns::MakePath()

int ns::MakePath(                       // true ok, false out of memory
    CQuickArray<WCHAR> &qa,             // Where to put results.
    LPCUTF8            szNameSpace,     // Namespace for name.
    LPCUTF8            szName)          // Final part of name.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    int iLen = 2;
    if (szNameSpace)
        iLen += (int)strlen(szNameSpace);
    if (szName)
        iLen += (int)strlen(szName);
    WCHAR *szOut = (WCHAR *) qa.AllocNoThrow(iLen);
    if (!szOut)
        return false;
    return ns::MakePath(szOut, iLen, szNameSpace, szName);
}   // int ns::MakePath()

int ns::MakePath(                       // true ok, false out of memory
    CQuickBytes &qb,                    // Where to put results.
    const WCHAR *szNameSpace,           // Namespace for name.
    const WCHAR *szName)                // Final part of name.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    int iLen = 2;
    if (szNameSpace)
        iLen += (int)u16_strlen(szNameSpace);
    if (szName)
        iLen += (int)u16_strlen(szName);
    WCHAR *szOut = (WCHAR *) qb.AllocNoThrow(iLen * sizeof(WCHAR));
    if (!szOut)
        return false;
    return ns::MakePath(szOut, iLen, szNameSpace, szName);
}   // int ns::MakePath()

void ns::MakePath(                      // throws on out of memory
    SString       &ssBuf,               // Where to put results.
    const SString &ssNameSpace,         // Namespace for name.
    const SString &ssName)              // Final part of name.
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    ssBuf.Clear();

    if (!ssNameSpace.IsEmpty())
    {
        if (ssName.IsEmpty())
        {
            ssBuf.Set(ssNameSpace);
        }
        else
        {
            SString s(SString::Literal, NAMESPACE_SEPARATOR_WSTR);
            ssBuf.Set(ssNameSpace, s);
        }
    }

    if (!ssName.IsEmpty())
    {
        ssBuf.Append(ssName);
    }
}

bool ns::MakeAssemblyQualifiedName(                                        // true ok, false truncation
                                   _Out_writes_(dwBuffer) WCHAR* pBuffer,  // Buffer to receive the results
                                   int    dwBuffer,                        // Number of characters total in buffer
                                   const WCHAR *szTypeName,                // Namespace for name.
                                   int   dwTypeName,                       // Number of characters (not including null)
                                   const WCHAR *szAssemblyName,            // Final part of name.
                                   int   dwAssemblyName)                   // Number of characters (not including null)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (dwBuffer < 2)
        return false;

    int iCopyMax = 0;
    _ASSERTE(pBuffer);
    *pBuffer = NULL;

    if (szTypeName && *szTypeName != W('\0'))
    {
        _ASSERTE(dwTypeName > 0);
        iCopyMax = min(dwBuffer-1, dwTypeName);
        wcsncpy_s(pBuffer, dwBuffer, szTypeName, iCopyMax);
        dwBuffer -= iCopyMax;
    }

    if (szAssemblyName && *szAssemblyName != W('\0'))
    {

        if(dwBuffer < ASSEMBLY_SEPARATOR_LEN)
            return false;

        for(DWORD i = 0; i < ASSEMBLY_SEPARATOR_LEN; i++)
            pBuffer[iCopyMax+i] = ASSEMBLY_SEPARATOR_WSTR[i];

        dwBuffer -= ASSEMBLY_SEPARATOR_LEN;
        if(dwBuffer == 0)
            return false;

        int iCur = iCopyMax + ASSEMBLY_SEPARATOR_LEN;
        _ASSERTE(dwAssemblyName > 0);
        iCopyMax = min(dwBuffer-1, dwAssemblyName);
        wcsncpy_s(pBuffer + iCur, dwBuffer, szAssemblyName, iCopyMax);
        pBuffer[iCur + iCopyMax] = W('\0');

        if (iCopyMax < dwAssemblyName)
            return false;
    }
    else {
        if(dwBuffer == 0) {
            PREFIX_ASSUME(iCopyMax > 0);
            pBuffer[iCopyMax-1] = W('\0');
            return false;
        }
        else
            pBuffer[iCopyMax] = W('\0');
    }

    return true;
}   // int ns::MakePath()

bool ns::MakeAssemblyQualifiedName(                                        // true ok, false out of memory
                                   CQuickBytes &qb,                        // Where to put results.
                                   const WCHAR *szTypeName,                // Namespace for name.
                                   const WCHAR *szAssemblyName)            // Final part of name.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    int iTypeName = 0;
    int iAssemblyName = 0;
    if (szTypeName)
        iTypeName = (int)u16_strlen(szTypeName);
    if (szAssemblyName)
        iAssemblyName = (int)u16_strlen(szAssemblyName);

    int iLen = ASSEMBLY_SEPARATOR_LEN + iTypeName + iAssemblyName + 1; // Space for null terminator
    WCHAR *szOut = (WCHAR *) qb.AllocNoThrow(iLen * sizeof(WCHAR));
    if (!szOut)
        return false;

    bool ret;
    ret = ns::MakeAssemblyQualifiedName(szOut, iLen, szTypeName, iTypeName, szAssemblyName, iAssemblyName);
    _ASSERTE(ret);
    return true;
}

int ns::MakeNestedTypeName(             // true ok, false out of memory
    CQuickBytes &qb,                    // Where to put results.
    LPCUTF8     szEnclosingName,        // Full name for enclosing type
    LPCUTF8     szNestedName)           // Full name for nested type
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    _ASSERTE(szEnclosingName && szNestedName);
    int iLen = 2;
    iLen += (int)strlen(szEnclosingName);
    iLen += (int)strlen(szNestedName);
    LPUTF8 szOut = (LPUTF8) qb.AllocNoThrow(iLen);
    if (!szOut)
        return false;
    return ns::MakeNestedTypeName(szOut, iLen, szEnclosingName, szNestedName);
}   // int ns::MakeNestedTypeName()

int ns::MakeNestedTypeName(             // true ok, false truncation.
    _Out_writes_ (cchChars) LPUTF8      szOut,                  // output path for name.
    int         cchChars,               // max chars for output path.
    LPCUTF8     szEnclosingName,        // Full name for enclosing type
    LPCUTF8     szNestedName)           // Full name for nested type
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (cchChars < 1)
        return false;

    int iCopyMax = 0, iLen;
    int brtn = true;
    *szOut = 0;

    iLen = (int)strlen(szEnclosingName);
    iCopyMax = min(cchChars-1, iLen);
    strncpy_s(szOut, cchChars, szEnclosingName, iCopyMax);

    if (iLen >= cchChars)
        brtn =  false;

    szOut[iCopyMax] = NESTED_SEPARATOR_CHAR;
    int iCur = iCopyMax+1; // iCopyMax characters + nested_separator_char
    cchChars -= iCur;
    if(cchChars == 0)
        return false;

    iLen = (int)strlen(szNestedName);
    iCopyMax = min(cchChars-1, iLen);
    strncpy_s(&szOut[iCur], cchChars, szNestedName, iCopyMax);
    szOut[iCur + iCopyMax] = 0;

    if (iLen >= cchChars)
        brtn = false;

    return brtn;
}   // int ns::MakeNestedTypeName()

void ns::MakeNestedTypeName(            // throws on out of memory
    SString        &ssBuf,              // output path for name.
    const SString  &ssEnclosingName,    // Full name for enclosing type
    const SString  &ssNestedName)       // Full name for nested type
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;

    ssBuf.Clear();

    ssBuf.Append(ssEnclosingName);
    ssBuf.Append(NESTED_SEPARATOR_WCHAR);
    ssBuf.Append(ssNestedName);
}

