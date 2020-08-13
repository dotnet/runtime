// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// UtilCode.h
//
// Utility functions implemented in UtilCode.lib.
//
//*****************************************************************************

#ifndef __UtilCode_h__
#define __UtilCode_h__

#include "crtwrap.h"
#include "winwrap.h"
#include <wchar.h>
#include <stdio.h>
#include <malloc.h>
#include <ole2.h>
#include <oleauto.h>
#include <limits.h>
#include "clrtypes.h"
#include "safewrap.h"
#include "volatile.h"
#include <daccess.h>
#include "clrhost.h"
#include "debugmacros.h"
#include "corhlprpriv.h"
#include "winnls.h"
#include "check.h"
#include "safemath.h"
#include "new.hpp"

#ifdef PAL_STDCPP_COMPAT
#include <type_traits>
#else
#include "clr_std/type_traits"
#endif

#include "contract.h"
#include "entrypoints.h"

#include "clrnt.h"

#include "random.h"

#define WINDOWS_KERNEL32_DLLNAME_A "kernel32"
#define WINDOWS_KERNEL32_DLLNAME_W W("kernel32")

#define CoreLibName_W W("System.Private.CoreLib")
#define CoreLibName_IL_W W("System.Private.CoreLib.dll")
#define CoreLibName_NI_W W("System.Private.CoreLib.ni.dll")
#define CoreLibName_TLB_W W("System.Private.CoreLib.tlb")
#define CoreLibName_A "System.Private.CoreLib"
#define CoreLibName_IL_A "System.Private.CoreLib.dll"
#define CoreLibName_NI_A "System.Private.CoreLib.ni.dll"
#define CoreLibName_TLB_A "System.Private.CoreLib.tlb"
#define CoreLibNameLen 22
#define CoreLibSatelliteName_A "System.Private.CoreLib.resources"
#define CoreLibSatelliteNameLen 32
#define LegacyCoreLibName_A "mscorlib"

class StringArrayList;

#if !defined(_DEBUG_IMPL) && defined(_DEBUG) && !defined(DACCESS_COMPILE)
#define _DEBUG_IMPL 1
#endif

#ifdef TARGET_ARM

// Under ARM we generate code only with Thumb encoding. In order to ensure we execute such code in the correct
// mode we must ensure the low-order bit is set in any code address we'll call as a sub-routine. In C++ this
// is handled automatically for us by the compiler. When generating and working with code generated
// dynamically we have to be careful to set or mask-out this bit as appropriate.
#ifndef THUMB_CODE
#define THUMB_CODE 1
#endif

// Given a WORD extract the bitfield [lowbit, highbit] (i.e. BitExtract(0xffff, 15, 0) == 0xffff).
inline WORD BitExtract(WORD wValue, DWORD highbit, DWORD lowbit)
{
    _ASSERTE((highbit < 16) && (lowbit < 16) && (highbit >= lowbit));
    return (wValue >> lowbit) & ((1 << ((highbit - lowbit) + 1)) - 1);
}

// Determine whether an ARM Thumb mode instruction is 32-bit or 16-bit based on the first WORD of the
// instruction.
inline bool Is32BitInstruction(WORD opcode)
{
    return BitExtract(opcode, 15, 11) >= 0x1d;
}

template <typename ResultType, typename SourceType>
inline ResultType DataPointerToThumbCode(SourceType pCode)
{
    return (ResultType)(((UINT_PTR)pCode) | THUMB_CODE);
}

template <typename ResultType, typename SourceType>
inline ResultType ThumbCodeToDataPointer(SourceType pCode)
{
    return (ResultType)(((UINT_PTR)pCode) & ~THUMB_CODE);
}

#endif // TARGET_ARM

// Convert from a PCODE to the corresponding PINSTR.  On many architectures this will be the identity function;
// on ARM, this will mask off the THUMB bit.
inline TADDR PCODEToPINSTR(PCODE pc)
{
#ifdef TARGET_ARM
    return ThumbCodeToDataPointer<TADDR,PCODE>(pc);
#else
    return dac_cast<PCODE>(pc);
#endif
}

// Convert from a PINSTR to the corresponding PCODE.  On many architectures this will be the identity function;
// on ARM, this will raise the THUMB bit.
inline PCODE PINSTRToPCODE(TADDR addr)
{
#ifdef TARGET_ARM
    return DataPointerToThumbCode<PCODE,TADDR>(addr);
#else
    return dac_cast<PCODE>(addr);
#endif
}

typedef LPCSTR  LPCUTF8;
typedef LPSTR   LPUTF8;

#include "nsutilpriv.h"

#include "stdmacros.h"

//********** Macros. **********************************************************
#ifndef FORCEINLINE
 #if _MSC_VER < 1200
   #define FORCEINLINE inline
 #else
   #define FORCEINLINE __forceinline
 #endif
#endif

#ifndef DEBUG_NOINLINE
#if defined(_DEBUG)
#define DEBUG_NOINLINE NOINLINE
#else
#define DEBUG_NOINLINE
#endif
#endif

#include <stddef.h> // for offsetof

#ifndef NumItems
// Number of elements in a fixed-size array
#define NumItems(s) (sizeof(s) / sizeof(s[0]))
#endif

#ifndef StrLen
// Number of characters in a string literal. Excludes terminating NULL.
#define StrLen(str) (NumItems(str) - 1)
#endif


#define IS_DIGIT(ch) (((ch) >= W('0')) && ((ch) <= W('9')))
#define DIGIT_TO_INT(ch) ((ch) - W('0'))
#define INT_TO_DIGIT(i) ((WCHAR)(W('0') + (i)))

#define IS_HEXDIGIT(ch) ((((ch) >= W('a')) && ((ch) <= W('f'))) || \
                         (((ch) >= W('A')) && ((ch) <= W('F'))))
#define HEXDIGIT_TO_INT(ch) ((towlower(ch) - W('a')) + 10)
#define INT_TO_HEXDIGIT(i) ((WCHAR)(W('a') + ((i) - 10)))


// Helper will 4 byte align a value, rounding up.
#define ALIGN4BYTE(val) (((val) + 3) & ~0x3)

#ifdef  _DEBUG
#define DEBUGARG(x)         , x
#else
#define DEBUGARG(x)
#endif

#ifndef sizeofmember
// Returns the size of a class or struct member.
#define sizeofmember(c,m) (sizeof(((c*)0)->m))
#endif

//=--------------------------------------------------------------------------=
// Prefast helpers.
//

#include "safemath.h"


//=--------------------------------------------------------------------------=
// string helpers.

//
// given and ANSI String, copy it into a wide buffer.
// be careful about scoping when using this macro!
//
// how to use the below two macros:
//
//  ...
//  LPSTR pszA;
//  pszA = MyGetAnsiStringRoutine();
//  MAKE_WIDEPTR_FROMANSI(pwsz, pszA);
//  MyUseWideStringRoutine(pwsz);
//  ...
//
// similarily for MAKE_ANSIPTR_FROMWIDE.  note that the first param does not
// have to be declared, and no clean up must be done.
//

// We'll define an upper limit that allows multiplication by 4 (the max
// bytes/char in UTF-8) but still remains positive, and allows some room for pad.
// Under normal circumstances, we should never get anywhere near this limit.
#define MAKE_MAX_LENGTH 0x1fffff00

#ifndef MAKE_TOOLONGACTION
#define MAKE_TOOLONGACTION ThrowHR(COR_E_OVERFLOW)
#endif

#ifndef MAKE_TRANSLATIONFAILED
#define MAKE_TRANSLATIONFAILED ThrowWin32(ERROR_NO_UNICODE_TRANSLATION)
#endif

// This version throws on conversion errors (ie, no best fit character
// mapping to characters that look similar, and no use of the default char
// ('?') when printing out unrepresentable characters.  Use this method for
// most development in the EE, especially anything like metadata or class
// names.  See the BESTFIT version if you're printing out info to the console.
#define MAKE_MULTIBYTE_FROMWIDE(ptrname, widestr, codepage) \
    int __l##ptrname = (int)wcslen(widestr);        \
    if (__l##ptrname > MAKE_MAX_LENGTH)         \
        MAKE_TOOLONGACTION;                     \
    __l##ptrname = (int)((__l##ptrname + 1) * 2 * sizeof(char)); \
    CQuickBytes __CQuickBytes##ptrname; \
    __CQuickBytes##ptrname.AllocThrows(__l##ptrname); \
    BOOL __b##ptrname; \
    DWORD __cBytes##ptrname = WszWideCharToMultiByte(codepage, WC_NO_BEST_FIT_CHARS, widestr, -1, (LPSTR)__CQuickBytes##ptrname.Ptr(), __l##ptrname, NULL, &__b##ptrname); \
    if (__b##ptrname || (__cBytes##ptrname == 0 && (widestr[0] != W('\0')))) { \
        MAKE_TRANSLATIONFAILED; \
    } \
    LPSTR ptrname = (LPSTR)__CQuickBytes##ptrname.Ptr()

// This version does best fit character mapping and also allows the use
// of the default char ('?') for any Unicode character that isn't
// representable.  This is reasonable for writing to the console, but
// shouldn't be used for most string conversions.
#define MAKE_MULTIBYTE_FROMWIDE_BESTFIT(ptrname, widestr, codepage) \
    int __l##ptrname = (int)wcslen(widestr);        \
    if (__l##ptrname > MAKE_MAX_LENGTH)         \
        MAKE_TOOLONGACTION;                     \
    __l##ptrname = (int)((__l##ptrname + 1) * 2 * sizeof(char)); \
    CQuickBytes __CQuickBytes##ptrname; \
    __CQuickBytes##ptrname.AllocThrows(__l##ptrname); \
    DWORD __cBytes##ptrname = WszWideCharToMultiByte(codepage, 0, widestr, -1, (LPSTR)__CQuickBytes##ptrname.Ptr(), __l##ptrname, NULL, NULL); \
    if (__cBytes##ptrname == 0 && __l##ptrname != 0) { \
        MAKE_TRANSLATIONFAILED; \
    } \
    LPSTR ptrname = (LPSTR)__CQuickBytes##ptrname.Ptr()

// Use for anything critical other than output to console, where weird
// character mappings are unacceptable.
#define MAKE_ANSIPTR_FROMWIDE(ptrname, widestr) MAKE_MULTIBYTE_FROMWIDE(ptrname, widestr, CP_ACP)

// Use for output to the console.
#define MAKE_ANSIPTR_FROMWIDE_BESTFIT(ptrname, widestr) MAKE_MULTIBYTE_FROMWIDE_BESTFIT(ptrname, widestr, CP_ACP)

#define MAKE_WIDEPTR_FROMANSI(ptrname, ansistr) \
    CQuickBytes __qb##ptrname; \
    int __l##ptrname; \
    __l##ptrname = WszMultiByteToWideChar(CP_ACP, 0, ansistr, -1, 0, 0); \
    if (__l##ptrname > MAKE_MAX_LENGTH) \
        MAKE_TOOLONGACTION; \
    LPWSTR ptrname = (LPWSTR) __qb##ptrname.AllocThrows((__l##ptrname+1)*sizeof(WCHAR));  \
    if (WszMultiByteToWideChar(CP_ACP, MB_ERR_INVALID_CHARS, ansistr, -1, ptrname, __l##ptrname) == 0) { \
        MAKE_TRANSLATIONFAILED; \
    }

#define MAKE_WIDEPTR_FROMANSI_NOTHROW(ptrname, ansistr) \
    CQuickBytes __qb##ptrname; \
    LPWSTR ptrname = 0; \
    int __l##ptrname; \
    __l##ptrname = WszMultiByteToWideChar(CP_ACP, 0, ansistr, -1, 0, 0); \
    if (__l##ptrname <= MAKE_MAX_LENGTH) { \
        ptrname = (LPWSTR) __qb##ptrname.AllocNoThrow((__l##ptrname+1)*sizeof(WCHAR));  \
        if (ptrname) { \
            if (WszMultiByteToWideChar(CP_ACP, MB_ERR_INVALID_CHARS, ansistr, -1, ptrname, __l##ptrname) != 0) { \
                ptrname[__l##ptrname] = 0; \
            } else { \
                ptrname = 0; \
            } \
        } \
    }

#define MAKE_UTF8PTR_FROMWIDE(ptrname, widestr) CQuickBytes _##ptrname; _##ptrname.ConvertUnicode_Utf8(widestr); LPSTR ptrname = (LPSTR) _##ptrname.Ptr();

#define MAKE_UTF8PTR_FROMWIDE_NOTHROW(ptrname, widestr) \
    CQuickBytes __qb##ptrname; \
    int __l##ptrname = (int)wcslen(widestr); \
    LPUTF8 ptrname = 0; \
    if (__l##ptrname <= MAKE_MAX_LENGTH) { \
        __l##ptrname = (int)((__l##ptrname + 1) * 2 * sizeof(char)); \
        ptrname = (LPUTF8) __qb##ptrname.AllocNoThrow(__l##ptrname); \
    } \
    if (ptrname) { \
        INT32 __lresult##ptrname=WszWideCharToMultiByte(CP_UTF8, 0, widestr, -1, ptrname, __l##ptrname-1, NULL, NULL); \
        DWORD __dwCaptureLastError##ptrname = ::GetLastError(); \
        if ((__lresult##ptrname==0) && (((LPCWSTR)widestr)[0] != W('\0'))) { \
            if (__dwCaptureLastError##ptrname==ERROR_INSUFFICIENT_BUFFER) { \
                INT32 __lsize##ptrname=WszWideCharToMultiByte(CP_UTF8, 0, widestr, -1, NULL, 0, NULL, NULL); \
                ptrname = (LPSTR) __qb##ptrname .AllocNoThrow(__lsize##ptrname); \
                if (ptrname) { \
                    if (WszWideCharToMultiByte(CP_UTF8, 0, widestr, -1, ptrname, __lsize##ptrname, NULL, NULL) != 0) { \
                        ptrname[__l##ptrname] = 0; \
                    } else { \
                        ptrname = 0; \
                    } \
                } \
            } \
            else { \
                ptrname = 0; \
            } \
        } \
    } \

#define MAKE_WIDEPTR_FROMUTF8N(ptrname, utf8str, n8chrs) \
    CQuickBytes __qb##ptrname; \
    int __l##ptrname; \
    __l##ptrname = WszMultiByteToWideChar(CP_UTF8, 0, utf8str, n8chrs, 0, 0); \
    if (__l##ptrname > MAKE_MAX_LENGTH) \
        MAKE_TOOLONGACTION; \
    LPWSTR ptrname = (LPWSTR) __qb##ptrname .AllocThrows((__l##ptrname+1)*sizeof(WCHAR)); \
    if (0==WszMultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, utf8str, n8chrs, ptrname, __l##ptrname)) { \
        MAKE_TRANSLATIONFAILED; \
    } \
    ptrname[__l##ptrname] = 0;


#define MAKE_WIDEPTR_FROMUTF8(ptrname, utf8str) CQuickBytes _##ptrname;  _##ptrname.ConvertUtf8_Unicode(utf8str); LPCWSTR ptrname = (LPCWSTR) _##ptrname.Ptr();


#define MAKE_WIDEPTR_FROMUTF8N_NOTHROW(ptrname, utf8str, n8chrs) \
    CQuickBytes __qb##ptrname; \
    int __l##ptrname; \
    LPWSTR ptrname = 0; \
    __l##ptrname = WszMultiByteToWideChar(CP_UTF8, 0, utf8str, n8chrs, 0, 0); \
    if (__l##ptrname <= MAKE_MAX_LENGTH) { \
        ptrname = (LPWSTR) __qb##ptrname.AllocNoThrow((__l##ptrname+1)*sizeof(WCHAR));  \
        if (ptrname) { \
            if (WszMultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, utf8str, n8chrs, ptrname, __l##ptrname) != 0) { \
                ptrname[__l##ptrname] = 0; \
            } else { \
                ptrname = 0; \
            } \
        } \
    }

#define MAKE_WIDEPTR_FROMUTF8_NOTHROW(ptrname, utf8str)   MAKE_WIDEPTR_FROMUTF8N_NOTHROW(ptrname, utf8str, -1)

// This method takes the number of characters
#define MAKE_MULTIBYTE_FROMWIDEN(ptrname, widestr, _nCharacters, _pCnt, codepage)        \
    CQuickBytes __qb##ptrname; \
    int __l##ptrname; \
    __l##ptrname = WszWideCharToMultiByte(codepage, WC_NO_BEST_FIT_CHARS, widestr, _nCharacters, NULL, 0, NULL, NULL);           \
    if (__l##ptrname > MAKE_MAX_LENGTH) \
        MAKE_TOOLONGACTION; \
    ptrname = (LPUTF8) __qb##ptrname .AllocThrows(__l##ptrname+1); \
    BOOL __b##ptrname; \
    DWORD _pCnt = WszWideCharToMultiByte(codepage, WC_NO_BEST_FIT_CHARS, widestr, _nCharacters, ptrname, __l##ptrname, NULL, &__b##ptrname);  \
    if (__b##ptrname || (_pCnt == 0 && _nCharacters > 0)) { \
        MAKE_TRANSLATIONFAILED; \
    } \
    ptrname[__l##ptrname] = 0;

#define MAKE_MULTIBYTE_FROMWIDEN_BESTFIT(ptrname, widestr, _nCharacters, _pCnt, codepage)        \
    CQuickBytes __qb##ptrname; \
    int __l##ptrname; \
    __l##ptrname = WszWideCharToMultiByte(codepage, 0, widestr, _nCharacters, NULL, 0, NULL, NULL);           \
    if (__l##ptrname > MAKE_MAX_LENGTH) \
        MAKE_TOOLONGACTION; \
    ptrname = (LPUTF8) __qb##ptrname .AllocThrows(__l##ptrname+1); \
    DWORD _pCnt = WszWideCharToMultiByte(codepage, 0, widestr, _nCharacters, ptrname, __l##ptrname, NULL, NULL);  \
    if (_pCnt == 0 && _nCharacters > 0) { \
        MAKE_TRANSLATIONFAILED; \
    } \
    ptrname[__l##ptrname] = 0;

#define MAKE_ANSIPTR_FROMWIDEN(ptrname, widestr, _nCharacters, _pCnt)        \
       MAKE_MULTIBYTE_FROMWIDEN(ptrname, widestr, _nCharacters, _pCnt, CP_ACP)


inline
LPWSTR DuplicateString(
    LPCWSTR wszString,
    size_t  cchString)
{
    STATIC_CONTRACT_NOTHROW;

    LPWSTR wszDup = NULL;
    if (wszString != NULL)
    {
        wszDup = new (nothrow) WCHAR[cchString + 1];
        if (wszDup != NULL)
        {
            wcscpy_s(wszDup, cchString + 1, wszString);
        }
    }
    return wszDup;
}

inline
LPWSTR DuplicateString(
    LPCWSTR wszString)
{
    STATIC_CONTRACT_NOTHROW;

    if (wszString != NULL)
    {
        return DuplicateString(wszString, wcslen(wszString));
    }
    else
    {
        return NULL;
    }
}

void DECLSPEC_NORETURN ThrowOutOfMemory();

inline
LPWSTR DuplicateStringThrowing(
    LPCWSTR wszString,
    size_t cchString)
{
    STATIC_CONTRACT_THROWS;

    if (wszString == NULL)
        return NULL;

    LPWSTR wszDup = DuplicateString(wszString, cchString);
    if (wszDup == NULL)
        ThrowOutOfMemory();

    return wszDup;
}

inline
LPWSTR DuplicateStringThrowing(
    LPCWSTR wszString)
{
    STATIC_CONTRACT_THROWS;

    if (wszString == NULL)
        return NULL;

    LPWSTR wszDup = DuplicateString(wszString);
    if (wszDup == NULL)
        ThrowOutOfMemory();

    return wszDup;
}


//*****************************************************************************
// Placement new is used to new and object at an exact location.  The pointer
// is simply returned to the caller without actually using the heap.  The
// advantage here is that you cause the ctor() code for the object to be run.
// This is ideal for heaps of C++ objects that need to get init'd multiple times.
// Example:
//      void        *pMem = GetMemFromSomePlace();
//      Foo *p = new (pMem) Foo;
//      DoSomething(p);
//      p->~Foo();
//*****************************************************************************
#ifndef __PLACEMENT_NEW_INLINE
#define __PLACEMENT_NEW_INLINE
inline void *__cdecl operator new(size_t, void *_P)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (_P);
}
#endif // __PLACEMENT_NEW_INLINE


/********************************************************************************/
/* portability helpers */

#ifdef TARGET_64BIT
#define IN_TARGET_64BIT(x)     x
#define IN_TARGET_32BIT(x)
#else
#define IN_TARGET_64BIT(x)
#define IN_TARGET_32BIT(x)     x
#endif

void * __cdecl
operator new(size_t n);

_Ret_bytecap_(n) void * __cdecl
operator new[](size_t n);

void __cdecl
operator delete(void *p) NOEXCEPT;

void __cdecl
operator delete[](void *p) NOEXCEPT;

#ifdef _DEBUG_IMPL
HRESULT _OutOfMemory(LPCSTR szFile, int iLine);
#define OutOfMemory() _OutOfMemory(__FILE__, __LINE__)
#else
inline HRESULT OutOfMemory()
{
    LIMITED_METHOD_CONTRACT;
    return (E_OUTOFMEMORY);
}
#endif

//*****************************************************************************
// Handle accessing localizable resource strings
//*****************************************************************************
typedef LPCWSTR LocaleID;
typedef WCHAR LocaleIDValue[LOCALE_NAME_MAX_LENGTH];

// Notes about the culture callbacks:
// - The language we're operating in can change at *runtime*!
// - A process may operate in *multiple* languages.
//     (ex: Each thread may have it's own language)
// - If we don't care what language we're in (or have no way of knowing),
//     then return a 0-length name and UICULTUREID_DONTCARE for the culture ID.
// - GetCultureName() and the GetCultureId() must be in sync (refer to the
//     same language).
// - We have two functions separate functions for better performance.
//     - The name is used to resolve a directory for MsCorRC.dll.
//     - The id is used as a key to map to a dll hinstance.

// Callback to obtain both the culture name and the culture's parent culture name
typedef HRESULT (*FPGETTHREADUICULTURENAMES)(__inout StringArrayList* pCultureNames);
const LPCWSTR UICULTUREID_DONTCARE = NULL;

typedef int (*FPGETTHREADUICULTUREID)(LocaleIDValue*);

HMODULE CLRLoadLibrary(LPCWSTR lpLibFileName);

HMODULE CLRLoadLibraryEx(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);

BOOL CLRFreeLibrary(HMODULE hModule);

// Load a string using the resources for the current module.
STDAPI UtilLoadStringRC(UINT iResouceID, __out_ecount (iMax) LPWSTR szBuffer, int iMax, int bQuiet=FALSE);

// Specify callbacks so that UtilLoadStringRC can find out which language we're in.
// If no callbacks specified (or both parameters are NULL), we default to the
// resource dll in the root (which is probably english).
void SetResourceCultureCallbacks(
    FPGETTHREADUICULTURENAMES fpGetThreadUICultureNames,
    FPGETTHREADUICULTUREID fpGetThreadUICultureId
);

void GetResourceCultureCallbacks(
        FPGETTHREADUICULTURENAMES* fpGetThreadUICultureNames,
        FPGETTHREADUICULTUREID* fpGetThreadUICultureId
);

//*****************************************************************************
// Use this class by privately deriving from noncopyable to disallow copying of
// your class.
//*****************************************************************************
class noncopyable
{
protected:
    noncopyable()
    {}
    ~noncopyable()
    {}

private:
    noncopyable(const noncopyable&);
    const noncopyable& operator=(const noncopyable&);
};

//*****************************************************************************
// Must associate each handle to an instance of a resource dll with the int
// that it represents
//*****************************************************************************
typedef HINSTANCE HRESOURCEDLL;


class CCulturedHInstance
{
    LocaleIDValue   m_LangId;
    HRESOURCEDLL    m_hInst;
    BOOL            m_fMissing;

public:
    CCulturedHInstance()
    {
        LIMITED_METHOD_CONTRACT;
        m_hInst = NULL;
        m_fMissing = FALSE;
    }

    BOOL HasID(LocaleID id)
    {
        _ASSERTE(m_hInst != NULL || m_fMissing);
        if (id == UICULTUREID_DONTCARE)
            return FALSE;

        return wcscmp(id, m_LangId) == 0;
    }

    HRESOURCEDLL GetLibraryHandle()
    {
        return m_hInst;
    }

    BOOL IsSet()
    {
        return m_hInst != NULL;
    }

    BOOL IsMissing()
    {
        return m_fMissing;
    }

    void SetMissing(LocaleID id)
    {
        _ASSERTE(m_hInst == NULL);
        SetId(id);
        m_fMissing = TRUE;
    }

    void Set(LocaleID id, HRESOURCEDLL hInst)
    {
        _ASSERTE(m_hInst == NULL);
        _ASSERTE(m_fMissing == FALSE);
        SetId(id);
        m_hInst = hInst;
    }
  private:
    void SetId(LocaleID id)
    {
        if (id != UICULTUREID_DONTCARE)
        {
            wcsncpy_s(m_LangId, NumItems(m_LangId), id, NumItems(m_LangId));
            m_LangId[NumItems(m_LangId)-1] = W('\0');
        }
        else
        {
            m_LangId[0] = W('\0');
        }
    }
 };

#ifndef DACCESS_COMPILE
void AddThreadPreferredUILanguages(StringArrayList* pArray);
#endif
//*****************************************************************************
// CCompRC manages string Resource access for COM+. This includes loading
// the MsCorRC.dll for resources as well allowing each thread to use a
// a different localized version.
//*****************************************************************************
class CCompRC
{
public:

    enum ResourceCategory
    {
        // must be present
        Required,

        // present in Desktop CLR and Core CLR + debug pack, an error
        // If missing, get a generic error message instead
        Error,

        // present in Desktop CLR and Core CLR + debug pack, normal operation (e.g tracing)
        // if missing, get a generic "resource not found" message instead
        Debugging,

        // present in Desktop CLR, optional for CoreCLR
        DesktopCLR,

        // might not be present, non essential
        Optional
    };

    CCompRC()
    {
        // This constructor will be fired up on startup. Make sure it doesn't
        // do anything besides zero-out out values.
        m_fpGetThreadUICultureId = NULL;
        m_fpGetThreadUICultureNames = NULL;

        m_pHash = NULL;
        m_nHashSize = 0;
        m_csMap = NULL;
        m_pResourceFile = NULL;
    }// CCompRC

    HRESULT Init(LPCWSTR pResourceFile);
    void Destroy();

    HRESULT LoadString(ResourceCategory eCategory, UINT iResourceID, __out_ecount (iMax) LPWSTR szBuffer, int iMax , int *pcwchUsed=NULL);
    HRESULT LoadString(ResourceCategory eCategory, LocaleID langId, UINT iResourceID, __out_ecount (iMax) LPWSTR szBuffer, int iMax, int *pcwchUsed);

    void SetResourceCultureCallbacks(
        FPGETTHREADUICULTURENAMES fpGetThreadUICultureNames,
        FPGETTHREADUICULTUREID fpGetThreadUICultureId
    );

    void GetResourceCultureCallbacks(
        FPGETTHREADUICULTURENAMES* fpGetThreadUICultureNames,
        FPGETTHREADUICULTUREID* fpGetThreadUICultureId
    );

    // Get the default resource location (mscorrc.dll)
    static CCompRC* GetDefaultResourceDll();

    static void GetDefaultCallbacks(
                    FPGETTHREADUICULTURENAMES* fpGetThreadUICultureNames,
                    FPGETTHREADUICULTUREID* fpGetThreadUICultureId)
    {
        WRAPPER_NO_CONTRACT;
        m_DefaultResourceDll.GetResourceCultureCallbacks(
                    fpGetThreadUICultureNames,
                    fpGetThreadUICultureId);
    }

    static void SetDefaultCallbacks(
                FPGETTHREADUICULTURENAMES fpGetThreadUICultureNames,
                FPGETTHREADUICULTUREID fpGetThreadUICultureId)
    {
        WRAPPER_NO_CONTRACT;
        // Either both are NULL or neither are NULL
        _ASSERTE((fpGetThreadUICultureNames != NULL) ==
                 (fpGetThreadUICultureId != NULL));

        m_DefaultResourceDll.SetResourceCultureCallbacks(
                fpGetThreadUICultureNames,
                fpGetThreadUICultureId);
    }

private:
    HRESULT GetLibrary(LocaleID langId, HRESOURCEDLL* phInst);
#ifndef DACCESS_COMPILE
    HRESULT LoadLibraryHelper(HRESOURCEDLL *pHInst,
                              SString& rcPath);
    HRESULT LoadLibraryThrows(HRESOURCEDLL * pHInst);
    HRESULT LoadLibrary(HRESOURCEDLL * pHInst);
    HRESULT LoadResourceFile(HRESOURCEDLL * pHInst, LPCWSTR lpFileName);
#endif

    // We do not have global constructors any more
    static LONG     m_dwDefaultInitialized;
    static CCompRC  m_DefaultResourceDll;
    static LPCWSTR  m_pDefaultResource;

    // We must map between a thread's int and a dll instance.
    // Since we only expect 1 language almost all of the time, we'll special case
    // that and then use a variable size map for everything else.
    CCulturedHInstance m_Primary;
    CCulturedHInstance * m_pHash;
    int m_nHashSize;

    CRITSEC_COOKIE m_csMap;

    LPCWSTR m_pResourceFile;

    // Main accessors for hash
    HRESOURCEDLL LookupNode(LocaleID langId, BOOL &fMissing);
    HRESULT AddMapNode(LocaleID langId, HRESOURCEDLL hInst, BOOL fMissing = FALSE);

    FPGETTHREADUICULTUREID m_fpGetThreadUICultureId;
    FPGETTHREADUICULTURENAMES m_fpGetThreadUICultureNames;
};

HRESULT UtilLoadResourceString(CCompRC::ResourceCategory eCategory, UINT iResouceID, __out_ecount (iMax) LPWSTR szBuffer, int iMax);


int UtilMessageBox(
                  HWND hWnd,        // Handle to Owner Window
                  UINT uText,       // Resource Identifier for Text message
                  UINT uCaption,    // Resource Identifier for Caption
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive
                  BOOL ShowFileNameInTitle, // Flag to show FileName in Caption
                  ...);             // Additional Arguments

int UtilMessageBoxNonLocalized(
                  HWND hWnd,        // Handle to Owner Window
                  LPCWSTR lpText,    // Resource Identifier for Text message
                  LPCWSTR lpTitle,   // Resource Identifier for Caption
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive
                  BOOL ShowFileNameInTitle, // Flag to show FileName in Caption
                  ...);             // Additional Arguments

int UtilMessageBoxVA(
                  HWND hWnd,        // Handle to Owner Window
                  UINT uText,       // Resource Identifier for Text message
                  UINT uCaption,    // Resource Identifier for Caption
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive
                  BOOL ShowFileNameInTitle, // Flag to show FileName in Caption
                  va_list args);    // Additional Arguments

int UtilMessageBoxNonLocalizedVA(
                  HWND hWnd,        // Handle to Owner Window
                  LPCWSTR lpText,    // Text message
                  LPCWSTR lpCaption, // Caption
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive
                  BOOL ShowFileNameInTitle, // Flag to show FileName in Caption
                  BOOL * pInputFromUser,            // To distinguish between user pressing abort vs. assuming abort.
                  va_list args);    // Additional Arguments

int UtilMessageBoxNonLocalizedVA(
                  HWND hWnd,        // Handle to Owner Window
                  LPCWSTR lpText,    // Text message
                  LPCWSTR lpCaption, // Caption
                  LPCWSTR lpDetails, // Details that may be shown in a collapsed extended area of the dialog (Vista or higher).
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive
                  BOOL ShowFileNameInTitle, // Flag to show FileName in Caption
                  BOOL * pInputFromUser,            // To distinguish between user pressing abort vs. assuming abort.
                  va_list args);    // Additional Arguments

int UtilMessageBoxCatastrophic(
                  UINT uText,       // Text for MessageBox
                  UINT uTitle,      // Title for MessageBox
                  UINT uType,       // Style of MessageBox
                  BOOL ShowFileNameInTitle, // Flag to show FileName in Caption
                  ...);

int UtilMessageBoxCatastrophicNonLocalized(
                  LPCWSTR lpText,    // Text for MessageBox
                  LPCWSTR lpTitle,   // Title for MessageBox
                  UINT uType,       // Style of MessageBox
                  BOOL ShowFileNameInTitle, // Flag to show FileName in Caption
                  ...);

int UtilMessageBoxCatastrophicVA(
                  UINT uText,       // Text for MessageBox
                  UINT uTitle,      // Title for MessageBox
                  UINT uType,       // Style of MessageBox
                  BOOL ShowFileNameInTitle, // Flag to show FileName in Caption
                  va_list args);    // Additional Arguments

int UtilMessageBoxCatastrophicNonLocalizedVA(
                  LPCWSTR lpText,    // Text for MessageBox
                  LPCWSTR lpTitle,   // Title for MessageBox
                  UINT uType,       // Style of MessageBox
                  BOOL ShowFileNameInTitle, // Flag to show FileName in Caption
                  va_list args);    // Additional Arguments


// The HRESULT_FROM_WIN32 macro evaluates its arguments three times.
// <TODO>TODO: All HRESULT_FROM_WIN32(GetLastError()) should be replaced by calls to
//  this helper function avoid code bloat</TODO>
inline HRESULT HRESULT_FROM_GetLastError()
{
    WRAPPER_NO_CONTRACT;
    DWORD dw = GetLastError();
    // Make sure we return a failure
    if (dw == ERROR_SUCCESS)
    {
        _ASSERTE(!"We were expecting to get an error code, but a success code is being returned. Check this code path for Everett!");
        return E_FAIL;
    }
    else
        return HRESULT_FROM_WIN32(dw);
}

inline HRESULT HRESULT_FROM_GetLastErrorNA()
{
    WRAPPER_NO_CONTRACT;
    DWORD dw = GetLastError();
    // Make sure we return a failure
    if (dw == ERROR_SUCCESS)
        return E_FAIL;
    else
        return HRESULT_FROM_WIN32(dw);
}

inline HRESULT BadError(HRESULT hr)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Serious Error");
    return (hr);
}

#define TESTANDRETURN(test, hrVal)              \
{                                               \
    int ___test = (int)(test);                  \
    if (! ___test)                              \
        return hrVal;                           \
}

#define TESTANDRETURNPOINTER(pointer)           \
    TESTANDRETURN((pointer)!=NULL, E_POINTER)

#define TESTANDRETURNMEMORY(pointer)            \
    TESTANDRETURN((pointer)!=NULL, E_OUTOFMEMORY)

#define TESTANDRETURNHR(hr)                     \
    TESTANDRETURN(SUCCEEDED(hr), hr)

#define TESTANDRETURNARG(argtest)               \
    TESTANDRETURN(argtest, E_INVALIDARG)

// Quick validity check for HANDLEs that are returned by Win32 APIs that
// use INVALID_HANDLE_VALUE instead of NULL to indicate an error
inline BOOL IsValidHandle(HANDLE h)
{
    LIMITED_METHOD_CONTRACT;
    return ((h != NULL) && (h != INVALID_HANDLE_VALUE));
}

// Count the bits in a value in order iBits time.
inline int CountBits(int iNum)
{
    LIMITED_METHOD_CONTRACT;
    int iBits;
    for (iBits=0;  iNum;  iBits++)
        iNum = iNum & (iNum - 1);
    return (iBits);
}

#include "bitposition.h"

// Convert the currency to a decimal and canonicalize.
inline void VarDecFromCyCanonicalize(CY cyIn, DECIMAL* dec)
{
    WRAPPER_NO_CONTRACT;

    (*(ULONG*)dec) = 0;
    DECIMAL_HI32(*dec) = 0;
    if (cyIn.int64 == 0) // For compatibility, a currency of 0 emits the Decimal "0.0000" (scale set to 4).
    {
        DECIMAL_SCALE(*dec) = 4;
        DECIMAL_LO32(*dec) = 0;
        DECIMAL_MID32(*dec) = 0;
        return;
    }

    if (cyIn.int64 < 0) {
        DECIMAL_SIGN(*dec) = DECIMAL_NEG;
        cyIn.int64 = -cyIn.int64;
    }

    BYTE scale = 4;
    ULONGLONG absoluteCy = (ULONGLONG)cyIn.int64;
    while (scale != 0 && ((absoluteCy % 10) == 0))
    {
        scale--;
        absoluteCy /= 10;
    }
    DECIMAL_SCALE(*dec) = scale;
    DECIMAL_LO32(*dec) = (ULONG)absoluteCy;
    DECIMAL_MID32(*dec) = (ULONG)(absoluteCy >> 32);
}

//*****************************************************************************
//
// Paths functions. Use these instead of the CRT.
//
//*****************************************************************************
// secure version! Specify the size of the each buffer in count of elements
void    SplitPath(const WCHAR *path,
                  __inout_z __inout_ecount_opt(driveSizeInWords) WCHAR *drive, int driveSizeInWords,
                  __inout_z __inout_ecount_opt(dirSizeInWords) WCHAR *dir, int dirSizeInWords,
                  __inout_z __inout_ecount_opt(fnameSizeInWords) WCHAR *fname, size_t fnameSizeInWords,
                  __inout_z __inout_ecount_opt(extSizeInWords) WCHAR *ext, size_t extSizeInWords);

//*******************************************************************************
// A much more sensible version that just points to each section of the string.
//*******************************************************************************
void    SplitPathInterior(
    __in      LPCWSTR wszPath,
    __out_opt LPCWSTR *pwszDrive,    __out_opt size_t *pcchDrive,
    __out_opt LPCWSTR *pwszDir,      __out_opt size_t *pcchDir,
    __out_opt LPCWSTR *pwszFileName, __out_opt size_t *pcchFileName,
    __out_opt LPCWSTR *pwszExt,      __out_opt size_t *pcchExt);


void    MakePath(__out CQuickWSTR &path,
                 __in LPCWSTR drive,
                 __in LPCWSTR dir,
                 __in LPCWSTR fname,
                 __in LPCWSTR ext);

WCHAR * FullPath(__out_ecount (maxlen) WCHAR *UserBuf, const WCHAR *path, size_t maxlen);

//*****************************************************************************
//
// SString version of the path functions.
//
//*****************************************************************************
void    SplitPath(__in SString const &path,
                  __inout_opt SString *drive,
                  __inout_opt SString *dir,
                  __inout_opt SString *fname,
                  __inout_opt SString *ext);

#if !defined(NO_CLRCONFIG)

//*****************************************************************************
//
// **** REGUTIL - Static helper functions for reading/writing to Windows registry.
//
//*****************************************************************************


class REGUTIL
{
public:
//*****************************************************************************

    enum CORConfigLevel
    {
        COR_CONFIG_ENV          = 0x01,
        COR_CONFIG_USER         = 0x02,
        COR_CONFIG_MACHINE      = 0x04,

        COR_CONFIG_REGISTRY     = (COR_CONFIG_USER|COR_CONFIG_MACHINE),
        COR_CONFIG_ALL          = (COR_CONFIG_ENV|COR_CONFIG_USER|COR_CONFIG_MACHINE),
    };

    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead.
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    //
    static DWORD GetConfigDWORD_DontUse_(
        LPCWSTR        name,
        DWORD          defValue,
        CORConfigLevel level = COR_CONFIG_ALL,
        BOOL           fPrependCOMPLUS = TRUE);

    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead.
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    //
    static HRESULT GetConfigDWORD_DontUse_(
        LPCWSTR name,
        DWORD defValue,
        __out DWORD * result,
        CORConfigLevel level = COR_CONFIG_ALL,
        BOOL fPrependCOMPLUS = TRUE);

    static ULONGLONG GetConfigULONGLONG_DontUse_(
        LPCWSTR        name,
        ULONGLONG      defValue,
        CORConfigLevel level = COR_CONFIG_ALL,
        BOOL           fPrependCOMPLUS = TRUE);

    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead.
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    //
    static DWORD GetConfigFlag_DontUse_(
        LPCWSTR        name,
        DWORD          bitToSet,
        BOOL           defValue = FALSE);

    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead.
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    //
    static LPWSTR GetConfigString_DontUse_(
        LPCWSTR name,
        BOOL fPrependCOMPLUS = TRUE,
        CORConfigLevel level = COR_CONFIG_ALL,
        BOOL fUsePerfCache = TRUE);

    static void   FreeConfigString(__in __in_z LPWSTR name);

private:
    static LPWSTR EnvGetString(LPCWSTR name, BOOL fPrependCOMPLUS);

private:
//*****************************************************************************
// Get either a DWORD or ULONGLONG. Always puts the result in a ULONGLONG that
// you can safely cast to a DWORD if fGetDWORD is TRUE.
//*****************************************************************************
    static HRESULT GetConfigInteger(
        LPCWSTR name,
        ULONGLONG defValue,
        __out ULONGLONG * result,
        BOOL fGetDWORD = TRUE,
        CORConfigLevel level = COR_CONFIG_ALL,
        BOOL fPrependCOMPLUS = TRUE);
public:


//*****************************************************************************
// (Optional) Initialize the config registry cache
// (see ConfigCacheValueNameSeenPerhaps, below.)
//*****************************************************************************
    static void InitOptionalConfigCache();

private:


//*****************************************************************************
// Return TRUE if the registry value name might have been seen in the registry
// at startup;
// return FALSE if the value was definitely not seen at startup.
//
// Perf Optimization for VSWhidbey:113373.
//*****************************************************************************
    static BOOL RegCacheValueNameSeenPerhaps(
        LPCWSTR name);
//*****************************************************************************
// Return TRUE if the environment variable name might have been seen at startup;
// return FALSE if the value was definitely not seen at startup.
//*****************************************************************************
    static BOOL EnvCacheValueNameSeenPerhaps(
        LPCWSTR name);

    static BOOL s_fUseRegCache; // Enable registry cache; if FALSE, CCVNSP
                                 // always returns TRUE.
    static BOOL s_fUseEnvCache; // Enable env cache.

    // Open the .NetFramework keys once and cache the handles
    static HKEY s_hMachineFrameworkKey;
    static HKEY s_hUserFrameworkKey;
};

// need this here because CLRConfig depends on REGUTIL, and ConfigStringHolder depends on CLRConfig
#include "clrconfig.h"

//-----------------------------------------------------------------------------
// Wrapper for configuration strings.
// This serves as a holder to call FreeConfigString.
class ConfigStringHolder
{
public:
    ConfigStringHolder() { m_wszString = NULL; }
    ~ConfigStringHolder()
    {
        Clear();
    }

    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead.
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    //
    void Init_DontUse_(LPCWSTR wszName)
    {
        Clear();
        m_wszString = REGUTIL::GetConfigString_DontUse_(wszName);
    }

    // Free resources.
    void Clear()
    {
        if (m_wszString != NULL)
        {
            REGUTIL::FreeConfigString(m_wszString);
            m_wszString = NULL;
        }
    }

    // Get the string value. NULL if not set.
    LPCWSTR Value()
    {
        return m_wszString;
    }

private:
    LPWSTR m_wszString;
};

#endif // defined(NO_CLRCONFIG)

#include "ostype.h"

#define CLRGetTickCount64() GetTickCount64()

//
// Use this function to initialize the s_CodeAllocHint
// during startup. base is runtime .dll base address,
// size is runtime .dll virtual size.
//
void InitCodeAllocHint(SIZE_T base, SIZE_T size, int randomPageOffset);


//
// Use this function to reset the s_CodeAllocHint
// after unloading an AppDomain
//
void ResetCodeAllocHint();

//
// Returns TRUE if p is located in near clr.dll that allows us
// to use rel32 IP-relative addressing modes.
//
BOOL IsPreferredExecutableRange(void * p);

//
// Allocate free memory that will be used for executable code
// Handles the special requirements that we have on 64-bit platforms
// where we want the executable memory to be located near mscorwks
//
BYTE * ClrVirtualAllocExecutable(SIZE_T dwSize,
                                 DWORD flAllocationType,
                                 DWORD flProtect);

//
// Allocate free memory within the range [pMinAddr..pMaxAddr] using
// ClrVirtualQuery to find free memory and ClrVirtualAlloc to allocate it.
//
BYTE * ClrVirtualAllocWithinRange(const BYTE *pMinAddr,
                                   const BYTE *pMaxAddr,
                                   SIZE_T dwSize,
                                   DWORD flAllocationType,
                                   DWORD flProtect);

//
// Allocate free memory with specific alignment
//
LPVOID ClrVirtualAllocAligned(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect, SIZE_T alignment);

class NumaNodeInfo
{
private:
    static BOOL m_enableGCNumaAware;
    static uint16_t m_nNodes;
    static BOOL InitNumaNodeInfoAPI();

public:
    static BOOL CanEnableGCNumaAware();
    static void InitNumaNodeInfo();

#if !defined(FEATURE_REDHAWK)
public: 	// functions

    static LPVOID VirtualAllocExNuma(HANDLE hProc, LPVOID lpAddr, SIZE_T size,
                                     DWORD allocType, DWORD prot, DWORD node);
#ifdef HOST_WINDOWS
    static BOOL GetNumaProcessorNodeEx(PPROCESSOR_NUMBER proc_no, PUSHORT node_no);
    static bool GetNumaInfo(PUSHORT total_nodes, DWORD* max_procs_per_node);
#else // HOST_WINDOWS
    static BOOL GetNumaProcessorNodeEx(USHORT proc_no, PUSHORT node_no);
#endif // HOST_WINDOWS
#endif
};

#ifdef HOST_WINDOWS

struct CPU_Group_Info
{
    WORD	nr_active;	// at most 64
    WORD	reserved[1];
    WORD	begin;
    WORD	end;
    DWORD_PTR	active_mask;
    DWORD	groupWeight;
    DWORD	activeThreadWeight;
};

class CPUGroupInfo
{
private:
    static LONG m_initialization;
    static WORD m_nGroups;
    static WORD m_nProcessors;
    static BOOL m_enableGCCPUGroups;
    static BOOL m_threadUseAllCpuGroups;
    static BOOL m_threadAssignCpuGroups;
    static WORD m_initialGroup;
    static CPU_Group_Info *m_CPUGroupInfoArray;
    static bool s_hadSingleProcessorAtStartup;

    static BOOL InitCPUGroupInfoArray();
    static BOOL InitCPUGroupInfoRange();
    static void InitCPUGroupInfo();
    static BOOL IsInitialized();

public:
    static void EnsureInitialized();
    static BOOL CanEnableGCCPUGroups();
    static BOOL CanEnableThreadUseAllCpuGroups();
    static BOOL CanAssignCpuGroupsToThreads();
    static WORD GetNumActiveProcessors();
    static void GetGroupForProcessor(WORD processor_number,
		    WORD *group_number, WORD *group_processor_number);
    static DWORD CalculateCurrentProcessorNumber();
    static bool GetCPUGroupInfo(PUSHORT total_groups, DWORD* max_procs_per_group);
    //static void PopulateCPUUsageArray(void * infoBuffer, ULONG infoSize);

#if !defined(FEATURE_REDHAWK)
public:
    static BOOL GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP relationship,
		   SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *slpiex, PDWORD count);
    static BOOL SetThreadGroupAffinity(HANDLE h,
		    const GROUP_AFFINITY *groupAffinity, GROUP_AFFINITY *previousGroupAffinity);
    static BOOL GetThreadGroupAffinity(HANDLE h, GROUP_AFFINITY *groupAffinity);
    static BOOL GetSystemTimes(FILETIME *idleTime, FILETIME *kernelTime, FILETIME *userTime);
    static void ChooseCPUGroupAffinity(GROUP_AFFINITY *gf);
    static void ClearCPUGroupAffinity(GROUP_AFFINITY *gf);
    static BOOL GetCPUGroupRange(WORD group_number, WORD* group_begin, WORD* group_size);
#endif

public:
    static bool HadSingleProcessorAtStartup()
    {
        LIMITED_METHOD_CONTRACT;
        return s_hadSingleProcessorAtStartup;
    }
};

DWORD_PTR GetCurrentProcessCpuMask();

#endif // HOST_WINDOWS

//******************************************************************************
// Returns the number of processors that a process has been configured to run on
//******************************************************************************
int GetCurrentProcessCpuCount();

uint32_t GetOsPageSize();


//*****************************************************************************
// Return != 0 if the bit at the specified index in the array is on and 0 if
// it is off.
//*****************************************************************************
inline int GetBit(PTR_BYTE pcBits,int iBit)
{
    LIMITED_METHOD_CONTRACT;
    return (pcBits[iBit>>3] & (1 << (iBit & 0x7)));
}

#ifdef DACCESS_COMPILE
inline int GetBit(BYTE const * pcBits,int iBit)
{
    WRAPPER_NO_CONTRACT;
    return GetBit(dac_cast<PTR_BYTE>(pcBits), iBit);
}
#endif

//*****************************************************************************
// Set the state of the bit at the specified index based on the value of bOn.
//*****************************************************************************
inline void SetBit(PTR_BYTE pcBits,int iBit,int bOn)
{
    LIMITED_METHOD_CONTRACT;
    if (bOn)
        pcBits[iBit>>3] |= (1 << (iBit & 0x7));
    else
        pcBits[iBit>>3] &= ~(1 << (iBit & 0x7));
}

#ifdef DACCESS_COMPILE
inline void SetBit(BYTE * pcBits,int iBit,int bOn)
{
    WRAPPER_NO_CONTRACT;
    SetBit(dac_cast<PTR_BYTE>(pcBits), iBit, bOn);
}
#endif

template<typename T>
class SimpleListNode
{
public:
    SimpleListNode<T>(const T& _t)
    {
        data = _t;
        next = 0;
    }

    T                  data;
    SimpleListNode<T>* next;
};

template<typename T>
class SimpleList
{
public:
    typedef SimpleListNode<T> NodeType;

    SimpleList<T>()
    {
        head = NULL;
    }

    void LinkHead(NodeType* pNode)
    {
        pNode->next = head;
                      head = pNode;
    }

    NodeType* UnlinkHead()
    {
        NodeType* ret = head;

        if (head)
        {
            head = head->next;
        }
        return ret;
    }

    NodeType* Head()
    {
        return head;
    }

protected:

    NodeType* head;
};


template < typename T, typename U >
struct Pair
{
public:
    typedef Pair< T, U > this_type;
    typedef T first_type;
    typedef U second_type;

    Pair()
    {}

    Pair( T const & t, U const & u )
        : m_first( t )
        , m_second( u )
    { SUPPORTS_DAC; }

    Pair( this_type const & obj )
        : m_first( obj.m_first )
        , m_second( obj.m_second )
    {}

    this_type & operator=( this_type const & obj )
    {
        m_first = obj.m_first;
        m_second = obj.m_second;
        return *this;
    }

    T & First()
    {
        return m_first;
    }

    T const & First() const
    {
        return m_first;
    }

    U & Second()
    {
        return m_second;
    }

    U const & Second() const
    {
        return m_second;
    }

    bool operator==(const Pair& rhs) const
    {
        return ((this->First()  == rhs.First()) &&
                (this->Second() == rhs.Second()));
    }

    bool operator!=(const Pair& rhs) const
    {
        return !(*this == rhs);
    }

private:
    first_type  m_first;
    second_type m_second;
};


template < typename T, typename U >
Pair< T, U > MakePair( T const & t, U const & u )
{
    SUPPORTS_DAC;
    return Pair< T, U >( t, u );
}


//*****************************************************************************
// This class implements a dynamic array of structures for which the order of
// the elements is unimportant.  This means that any item placed in the list
// may be swapped to any other location in the list at any time.  If the order
// of the items you place in the array is important, then use the CStructArray
// class.
//*****************************************************************************

template <class T,
          int iGrowInc,
          class ALLOCATOR>
class CUnorderedArrayWithAllocator
{
    int         m_iCount;               // # of elements used in the list.
    int         m_iSize;                // # of elements allocated in the list.
public:
#ifndef DACCESS_COMPILE
    T           *m_pTable;              // Pointer to the list of elements.
#else
    TADDR        m_pTable;              // Pointer to the list of elements.
#endif

public:

#ifndef DACCESS_COMPILE

    CUnorderedArrayWithAllocator() :
        m_iCount(0),
        m_iSize(0),
        m_pTable(NULL)
    {
        LIMITED_METHOD_CONTRACT;
    }
    ~CUnorderedArrayWithAllocator()
    {
        LIMITED_METHOD_CONTRACT;
        // Free the chunk of memory.
        if (m_pTable != NULL)
            ALLOCATOR::Free(this, m_pTable);
    }

    void Clear()
    {
        WRAPPER_NO_CONTRACT;
        m_iCount = 0;
        if (m_iSize > iGrowInc)
        {
            T* tmp = ALLOCATOR::AllocNoThrow(this, iGrowInc);
            if (tmp) {
                ALLOCATOR::Free(this, m_pTable);
                m_pTable = tmp;
                m_iSize = iGrowInc;
            }
        }
    }

    void Clear(int iFirst, int iCount)
    {
        WRAPPER_NO_CONTRACT;
        int     iSize;

        if (iFirst + iCount < m_iCount)
            memmove(&m_pTable[iFirst], &m_pTable[iFirst + iCount], sizeof(T) * (m_iCount - (iFirst + iCount)));

        m_iCount -= iCount;

        iSize = ((m_iCount / iGrowInc) * iGrowInc) + ((m_iCount % iGrowInc != 0) ? iGrowInc : 0);
        if (m_iSize > iGrowInc && iSize < m_iSize)
        {
            T *tmp = ALLOCATOR::AllocNoThrow(this, iSize);
            if (tmp) {
                memcpy (tmp, m_pTable, iSize * sizeof(T));
                delete [] m_pTable;
                m_pTable = tmp;
                m_iSize = iSize;
            }
        }
        _ASSERTE(m_iCount <= m_iSize);
    }

    T *Table()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_pTable);
    }

    T *Append()
    {
        CONTRACTL {
            NOTHROW;
        } CONTRACTL_END;

        // The array should grow, if we can't fit one more element into the array.
        if (m_iSize <= m_iCount && GrowNoThrow() == NULL)
            return (NULL);
        return (&m_pTable[m_iCount++]);
    }

    T *AppendThrowing()
    {
        CONTRACTL {
            THROWS;
        } CONTRACTL_END;

        // The array should grow, if we can't fit one more element into the array.
        if (m_iSize <= m_iCount)
            Grow();
        return (&m_pTable[m_iCount++]);
    }

    void Delete(const T &Entry)
    {
        LIMITED_METHOD_CONTRACT;
        --m_iCount;
        for (int i=0; i <= m_iCount; ++i)
            if (m_pTable[i] == Entry)
            {
                m_pTable[i] = m_pTable[m_iCount];
                return;
            }

        // Just in case we didn't find it.
        ++m_iCount;
    }

    void DeleteByIndex(int i)
    {
        LIMITED_METHOD_CONTRACT;
        --m_iCount;
        m_pTable[i] = m_pTable[m_iCount];
    }

    void Swap(int i,int j)
    {
        LIMITED_METHOD_CONTRACT;
        T       tmp;

        if (i == j)
            return;
        tmp = m_pTable[i];
        m_pTable[i] = m_pTable[j];
        m_pTable[j] = tmp;
    }

#else

    TADDR Table()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return (m_pTable);
    }

    void EnumMemoryRegions(void)
    {
        SUPPORTS_DAC;
        DacEnumMemoryRegion(m_pTable, m_iCount * sizeof(T));
    }

#endif // #ifndef DACCESS_COMPILE

    USHORT Count()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        _ASSERTE(FitsIn<USHORT>(m_iCount));
        return static_cast<USHORT>(m_iCount);
    }

private:
    T *Grow();
    T *GrowNoThrow();
};


#ifndef DACCESS_COMPILE

//*****************************************************************************
// Increase the size of the array.
//*****************************************************************************
template <class T,
          int iGrowInc,
          class ALLOCATOR>
T *CUnorderedArrayWithAllocator<T,iGrowInc,ALLOCATOR>::GrowNoThrow()  // NULL if can't grow.
{
    WRAPPER_NO_CONTRACT;
    T       *pTemp;

    // try to allocate memory for reallocation.
    if ((pTemp = ALLOCATOR::AllocNoThrow(this, m_iSize+iGrowInc)) == NULL)
        return (NULL);
    memcpy (pTemp, m_pTable, m_iSize*sizeof(T));
    ALLOCATOR::Free(this, m_pTable);
    m_pTable = pTemp;
    m_iSize += iGrowInc;
    _ASSERTE(m_iSize > 0);
    return (pTemp);
}

template <class T,
          int iGrowInc,
          class ALLOCATOR>
T *CUnorderedArrayWithAllocator<T,iGrowInc,ALLOCATOR>::Grow()  // exception if can't grow.
{
    WRAPPER_NO_CONTRACT;
    T       *pTemp;

    // try to allocate memory for reallocation.
    pTemp = ALLOCATOR::AllocThrowing(this, m_iSize+iGrowInc);
    memcpy (pTemp, m_pTable, m_iSize*sizeof(T));
    ALLOCATOR::Free(this, m_pTable);
    m_pTable = pTemp;
    m_iSize += iGrowInc;
    _ASSERTE(m_iSize > 0);
    return (pTemp);
}

#endif // #ifndef DACCESS_COMPILE


template <class T>
class CUnorderedArray__Allocator
{
public:

    static T *AllocThrowing (void*, int nElements)
    {
        return new T[nElements];
    }

    static T *AllocNoThrow (void*, int nElements)
    {
        return new (nothrow) T[nElements];
    }

    static void Free (void*, T *pTable)
    {
        delete [] pTable;
    }
};


template <class T,int iGrowInc>
class CUnorderedArray : public CUnorderedArrayWithAllocator<T, iGrowInc, CUnorderedArray__Allocator<T> >
{
public:

    CUnorderedArray ()
    {
        LIMITED_METHOD_CONTRACT;
    }
};


//Used by the debugger.  Included here in hopes somebody else might, too
typedef CUnorderedArray<SIZE_T, 17> SIZE_T_UNORDERED_ARRAY;


//*****************************************************************************
// This class implements a dynamic array of structures for which the insert
// order is important.  Inserts will slide all elements after the location
// down, deletes slide all values over the deleted item.  If the order of the
// items in the array is unimportant to you, then CUnorderedArray may provide
// the same feature set at lower cost.
//*****************************************************************************
class CStructArray
{
    BYTE        *m_pList;               // Pointer to the list of elements.
    int         m_iCount;               // # of elements used in the list.
    int         m_iSize;                // # of elements allocated in the list.
    int         m_iGrowInc;             // Growth increment.
    short       m_iElemSize;            // Size of an array element.
    bool        m_bFree;                // true if data is automatically maintained.

public:
    CStructArray(short iElemSize, short iGrowInc = 1) :
        m_pList(NULL),
        m_iCount(0),
        m_iSize(0),
        m_iGrowInc(iGrowInc),
        m_iElemSize(iElemSize),
        m_bFree(true)
    {
        LIMITED_METHOD_CONTRACT;
    }
    ~CStructArray()
    {
        WRAPPER_NO_CONTRACT;
        Clear();
    }

    void *Insert(int iIndex);
    void *InsertThrowing(int iIndex);
    void *Append();
    void *AppendThrowing();
    int AllocateBlock(int iCount);
    void AllocateBlockThrowing(int iCount);
    void Delete(int iIndex);
    void *Ptr()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_pList);
    }
    void *Get(int iIndex)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(iIndex < m_iCount);
        return (BYTE*) Ptr() + (iIndex * (size_t)m_iElemSize);
    }
    size_t Size()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_iCount * (size_t)m_iElemSize);
    }
    int Count()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_iCount);
    }
    void Clear();
    void ClearCount()
    {
        LIMITED_METHOD_CONTRACT;
        m_iCount = 0;
    }

    void InitOnMem(short iElemSize, void *pList, int iCount, int iSize, int iGrowInc=1)
    {
        LIMITED_METHOD_CONTRACT;
        m_iElemSize = iElemSize;
        m_iGrowInc = (short) iGrowInc;
        m_pList = (BYTE*)pList;
        m_iCount = iCount;
        m_iSize = iSize;
        m_bFree = false;
    }

private:
    void Grow(int iCount);
};


//*****************************************************************************
// This template simplifies access to a CStructArray by removing void * and
// adding some operator overloads.
//*****************************************************************************
template <class T>
class CDynArray : public CStructArray
{
public:
    CDynArray(short iGrowInc=16) :
        CStructArray(sizeof(T), iGrowInc)
    {
        LIMITED_METHOD_CONTRACT;
    }

    T *Insert(int iIndex)
    {
        WRAPPER_NO_CONTRACT;
        return ((T *)CStructArray::Insert((int)iIndex));
    }

    T *InsertThrowing(int iIndex)
    {
        WRAPPER_NO_CONTRACT;
        return ((T *)CStructArray::InsertThrowing((int)iIndex));
    }

    T *Append()
    {
        WRAPPER_NO_CONTRACT;
        return ((T *)CStructArray::Append());
    }

    T *AppendThrowing()
    {
        WRAPPER_NO_CONTRACT;
        return ((T *)CStructArray::AppendThrowing());
    }

    T *Ptr()
    {
        WRAPPER_NO_CONTRACT;
        return ((T *)CStructArray::Ptr());
    }

    T *Get(int iIndex)
    {
        WRAPPER_NO_CONTRACT;
        return (Ptr() + iIndex);
    }
    T &operator[](int iIndex)
    {
        WRAPPER_NO_CONTRACT;
        return (*(Ptr() + iIndex));
    }
    int ItemIndex(T *p)
    {
        WRAPPER_NO_CONTRACT;
        return (((int)(LONG_PTR)p - (int)(LONG_PTR)Ptr()) / sizeof(T));
    }
    void Move(int iFrom, int iTo)
    {
        WRAPPER_NO_CONTRACT;
        T       tmp;

        _ASSERTE(iFrom >= 0 && iFrom < Count() &&
                 iTo >= 0 && iTo < Count());

        tmp = *(Ptr() + iFrom);
        if (iTo > iFrom)
            memmove(Ptr() + iFrom, Ptr() + iFrom + 1, (iTo - iFrom) * sizeof(T));
        else
            memmove(Ptr() + iTo + 1, Ptr() + iTo, (iFrom - iTo) * sizeof(T));
        *(Ptr() + iTo) = tmp;
    }
};

// Some common arrays.
typedef CDynArray<int> INTARRAY;
typedef CDynArray<short> SHORTARRAY;
typedef CDynArray<int> LONGARRAY;
typedef CDynArray<USHORT> USHORTARRAY;
typedef CDynArray<ULONG> ULONGARRAY;
typedef CDynArray<BYTE> BYTEARRAY;
typedef CDynArray<mdToken> TOKENARRAY;

template <class T> class CStackArray : public CStructArray
{
public:
    CStackArray(short iGrowInc=4) :
        CStructArray(sizeof(T), iGrowInc),
        m_curPos(0)
    {
        LIMITED_METHOD_CONTRACT;
    }

    void Push(T p)
    {
        WRAPPER_NO_CONTRACT;
        // We should only inc m_curPos after we grow the array.
        T *pT = (T *)CStructArray::InsertThrowing(m_curPos);
        m_curPos ++;
        *pT = p;
    }

    T * Pop()
    {
        WRAPPER_NO_CONTRACT;
        T * retPtr;

        _ASSERTE(m_curPos > 0);

        retPtr = (T *)CStructArray::Get(m_curPos-1);
        CStructArray::Delete(m_curPos--);

        return (retPtr);
    }

    int Count()
    {
        LIMITED_METHOD_CONTRACT;
        return(m_curPos);
    }

private:
    int m_curPos;
};


//*****************************************************************************
// This template manages a list of free entries by their 0 based offset.  By
// making it a template, you can use whatever size free chain will match your
// maximum count of items.  -1 is reserved.
//*****************************************************************************
template <class T> class TFreeList
{
public:
    void Init(
        T           *rgList,
        int         iCount)
    {
        LIMITED_METHOD_CONTRACT;
        // Save off values.
        m_rgList = rgList;
        m_iCount = iCount;
        m_iNext = 0;

        // Init free list.
        int i;
        for (i=0;  i<iCount - 1;  i++)
            m_rgList[i] = i + 1;
        m_rgList[i] = (T) -1;
    }

    T GetFreeEntry()                        // Index of free item, or -1.
    {
        LIMITED_METHOD_CONTRACT;
        T           iNext;

        if (m_iNext == (T) -1)
            return (-1);

        iNext = m_iNext;
        m_iNext = m_rgList[m_iNext];
        return (iNext);
    }

    void DelFreeEntry(T iEntry)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(iEntry < m_iCount);
        m_rgList[iEntry] = m_iNext;
        m_iNext = iEntry;
    }

    // This function can only be used when it is guaranteed that the free
    // array is contigous, for example, right after creation to quickly
    // get a range of items from the heap.
    void ReserveRange(int iCount)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(iCount < m_iCount);
        _ASSERTE(m_iNext == 0);
        m_iNext = iCount;
    }

private:
    T           *m_rgList;              // List of free info.
    int         m_iCount;               // How many entries to manage.
    T           m_iNext;                // Next item to get.
};


//*****************************************************************************
//*****************************************************************************
template <class T> class CQuickSort
{
protected:
    T           *m_pBase;                   // Base of array to sort.
private:
    SSIZE_T     m_iCount;                   // How many items in array.
    SSIZE_T     m_iElemSize;                // Size of one element.
public:
    CQuickSort(
        T           *pBase,                 // Address of first element.
        SSIZE_T     iCount) :               // How many there are.
        m_pBase(pBase),
        m_iCount(iCount),
        m_iElemSize(sizeof(T))
    {
        LIMITED_METHOD_DAC_CONTRACT;
    }

//*****************************************************************************
// Call to sort the array.
//*****************************************************************************
    inline void Sort()
    {
        WRAPPER_NO_CONTRACT;
        SortRange(0, m_iCount - 1);
    }

protected:
//*****************************************************************************
// Override this function to do the comparison.
//*****************************************************************************
    virtual FORCEINLINE int Compare(        // -1, 0, or 1
        T           *psFirst,               // First item to compare.
        T           *psSecond)              // Second item to compare.
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (memcmp(psFirst, psSecond, sizeof(T)));
//      return (::Compare(*psFirst, *psSecond));
    }

    virtual FORCEINLINE void Swap(
        SSIZE_T     iFirst,
        SSIZE_T     iSecond)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        if (iFirst == iSecond) return;
        T sTemp( m_pBase[iFirst] );
        m_pBase[iFirst] = m_pBase[iSecond];
        m_pBase[iSecond] = sTemp;
    }

private:
    inline void SortRange(
        SSIZE_T     iLeft,
        SSIZE_T     iRight)
    {
        WRAPPER_NO_CONTRACT;
        SSIZE_T     iLast;
        SSIZE_T     i;                      // loop variable.

        for (;;)
        {
            // if less than two elements you're done.
            if (iLeft >= iRight)
                return;

            // ASSERT that we now have valid indicies.  This is statically provable
            // since this private function is only called with valid indicies,
            // and iLeft and iRight only converge towards eachother.  However,
            // PreFast can't detect this because it doesn't know about our callers.
            COMPILER_ASSUME(iLeft >= 0 && iLeft < m_iCount);
            COMPILER_ASSUME(iRight >= 0 && iRight < m_iCount);

            // The mid-element is the pivot, move it to the left.
            Swap(iLeft, (iLeft + iRight) / 2);
            iLast = iLeft;

            // move everything that is smaller than the pivot to the left.
            for (i = iLeft + 1; i <= iRight; i++)
            {
                if (Compare(&m_pBase[i], &m_pBase[iLeft]) < 0)
                {
                    Swap(i, ++iLast);
                }
            }

            // Put the pivot to the point where it is in between smaller and larger elements.
            Swap(iLeft, iLast);

            // Sort each partition.
            SSIZE_T iLeftLast = iLast - 1;
            SSIZE_T iRightFirst = iLast + 1;
            if (iLeftLast - iLeft < iRight - iRightFirst)
            {   // Left partition is smaller, sort it recursively
                SortRange(iLeft, iLeftLast);
                // Tail call to sort the right (bigger) partition
                iLeft = iRightFirst;
                //iRight = iRight;
                continue;
            }
            else
            {   // Right partition is smaller, sort it recursively
                SortRange(iRightFirst, iRight);
                // Tail call to sort the left (bigger) partition
                //iLeft = iLeft;
                iRight = iLeftLast;
                continue;
            }
        }
    }
};

//*****************************************************************************
// Faster and simpler version of the binary search below.
//*****************************************************************************
template <class T>
const T * BinarySearch(const T * pBase, int iCount, const T & find)
{
    WRAPPER_NO_CONTRACT;

    int iFirst = 0;
    int iLast  = iCount - 1;

    // It is faster to use linear search once we get down to a small number of elements.
    while (iLast - iFirst > 10)
    {
        int iMid = (iLast + iFirst) / 2;

        if (find < pBase[iMid])
            iLast = iMid - 1;
        else
            iFirst = iMid;
    }

    for (int i = iFirst; i <= iLast; i++)
    {
        if (find == pBase[i])
            return &pBase[i];

        if (find < pBase[i])
            break;
    }

    return NULL;
}

//*****************************************************************************
// This template encapsulates a binary search algorithm on the given type
// of data.
//*****************************************************************************
template <class T> class CBinarySearch
{
private:
    const T     *m_pBase;                   // Base of array to sort.
    int         m_iCount;                   // How many items in array.

public:
    CBinarySearch(
        const T     *pBase,                 // Address of first element.
        int         iCount) :               // Value to find.
        m_pBase(pBase),
        m_iCount(iCount)
    {
        LIMITED_METHOD_CONTRACT;
    }

//*****************************************************************************
// Searches for the item passed to ctor.
//*****************************************************************************
    const T *Find(                          // Pointer to found item in array.
        const T     *psFind,                // The key to find.
        int         *piInsert = NULL)       // Index to insert at.
    {
        WRAPPER_NO_CONTRACT;
        int         iMid, iFirst, iLast;    // Loop control.
        int         iCmp;                   // Comparison.

        iFirst = 0;
        iLast = m_iCount - 1;
        while (iFirst <= iLast)
        {
            iMid = (iLast + iFirst) / 2;
            iCmp = Compare(psFind, &m_pBase[iMid]);
            if (iCmp == 0)
            {
                if (piInsert != NULL)
                    *piInsert = iMid;
                return (&m_pBase[iMid]);
            }
            else if (iCmp < 0)
                iLast = iMid - 1;
            else
                iFirst = iMid + 1;
        }
        if (piInsert != NULL)
            *piInsert = iFirst;
        return (NULL);
    }

//*****************************************************************************
// Override this function to do the comparison if a comparison operator is
// not valid for your data type (such as a struct).
//*****************************************************************************
    virtual int Compare(                    // -1, 0, or 1
        const T     *psFirst,               // Key you are looking for.
        const T     *psSecond)              // Item to compare to.
    {
        LIMITED_METHOD_CONTRACT;
        return (memcmp(psFirst, psSecond, sizeof(T)));
//      return (::Compare(*psFirst, *psSecond));
    }
};

//*****************************************************************************
// The information that the hash table implementation stores at the beginning
// of every record that can be but in the hash table.
//*****************************************************************************
typedef DPTR(struct HASHENTRY) PTR_HASHENTRY;
struct HASHENTRY
{
    ULONG      iPrev;                  // Previous bucket in the chain.
    ULONG      iNext;                  // Next bucket in the chain.
};

typedef DPTR(struct FREEHASHENTRY) PTR_FREEHASHENTRY;
struct FREEHASHENTRY : HASHENTRY
{
    ULONG      iFree;
};

//*****************************************************************************
// Used by the FindFirst/FindNextEntry functions.  These api's allow you to
// do a sequential scan of all entries.
//*****************************************************************************
struct HASHFIND
{
    ULONG      iBucket;            // The next bucket to look in.
    ULONG      iNext;
};


//*****************************************************************************
// IMPORTANT: This data structure is deprecated, please do not add any new uses.
// The hashtable implementation that should be used instead is code:SHash.
// If code:SHash does not work for you, talk to mailto:clrdeag.
//*****************************************************************************
// This is a class that implements a chain and bucket hash table.
//
// The data is actually supplied as an array of structures by the user of this class.
// This allows the buckets to use small indices to point to the chain, instead of pointers.
//
// Each entry in the array contains a HASHENTRY structure immediately
// followed by the key used to hash the structure.
//
// The HASHENTRY part of every structure is used to implement the chain of
// entries in a single bucket.
//
// This implementation does not support rehashing the buckets if the table grows
// to big.
// @TODO: Fix this by adding an abstract function Hash() which must be implemented
// by all clients.
//
//*****************************************************************************
class CHashTable
{
    friend class DebuggerRCThread; //RCthread actually needs access to
    //fields of derrived class DebuggerPatchTable

protected:
    TADDR       m_pcEntries;            // Pointer to the array of structs.
    ULONG      m_iEntrySize;           // Size of the structs.

    ULONG      m_iBuckets;             // # of chains we are hashing into.
    PTR_ULONG  m_piBuckets;           // Ptr to the array of bucket chains.

    INDEBUG(unsigned    m_maxSearch;)   // For evaluating perf characteristics

    HASHENTRY *EntryPtr(ULONG iEntry)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (PTR_HASHENTRY(m_pcEntries + (iEntry * (size_t)m_iEntrySize)));
    }

    ULONG     ItemIndex(HASHENTRY *p)
    {
        SUPPORTS_DAC;
        LIMITED_METHOD_CONTRACT;
        return (ULONG)((dac_cast<TADDR>(p) - m_pcEntries) / m_iEntrySize);
    }


public:

    CHashTable(
        ULONG      iBuckets) :         // # of chains we are hashing into.
        m_pcEntries((TADDR)NULL),
        m_iBuckets(iBuckets)
    {
        LIMITED_METHOD_CONTRACT;

        m_piBuckets = NULL;

        INDEBUG(m_maxSearch = 0;)
    }

    CHashTable() :         // # of chains we are hashing into.
        m_pcEntries((TADDR)NULL),
        m_iBuckets(5)
    {
        LIMITED_METHOD_CONTRACT;

        m_piBuckets = NULL;

        INDEBUG(m_maxSearch = 0;)
    }

#ifndef DACCESS_COMPILE

    ~CHashTable()
    {
        LIMITED_METHOD_CONTRACT;
        if (m_piBuckets != NULL)
        {
            delete [] m_piBuckets;
            m_piBuckets = NULL;
        }
    }

//*****************************************************************************
// This is the second part of construction where we do all of the work that
// can fail.  We also take the array of structs here because the calling class
// presumably needs to allocate it in its NewInit.
//*****************************************************************************
    HRESULT NewInit(                    // Return status.
        BYTE        *pcEntries,         // Array of structs we are managing.
        ULONG      iEntrySize);        // Size of the entries.

//*****************************************************************************
// This can be called to change the pointer to the table that the hash table
// is managing.  You might call this if (for example) you realloc the size
// of the table and its pointer is different.
//*****************************************************************************
    void SetTable(
        BYTE        *pcEntries)         // Array of structs we are managing.
    {
        LIMITED_METHOD_CONTRACT;
        m_pcEntries = (TADDR)pcEntries;
    }

//*****************************************************************************
// Clear the hash table as if there were nothing in it.
//*****************************************************************************
    void Clear()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_piBuckets != NULL);
        memset(m_piBuckets, 0xff, m_iBuckets * sizeof(ULONG));
    }

//*****************************************************************************
// Add the struct at the specified index in m_pcEntries to the hash chains.
//*****************************************************************************
    BYTE *Add(                          // New entry.
        ULONG      iHash,              // Hash value of entry to add.
        ULONG      iIndex);            // Index of struct in m_pcEntries.

//*****************************************************************************
// Delete the struct at the specified index in m_pcEntries from the hash chains.
//*****************************************************************************
    void Delete(
        ULONG      iHash,              // Hash value of entry to delete.
        ULONG      iIndex);            // Index of struct in m_pcEntries.

    void Delete(
        ULONG      iHash,              // Hash value of entry to delete.
        HASHENTRY   *psEntry);          // The struct to delete.

//*****************************************************************************
// The item at the specified index has been moved, update the previous and
// next item.
//*****************************************************************************
    void Move(
        ULONG      iHash,              // Hash value for the item.
        ULONG      iNew);              // New location.

#endif // #ifndef DACCESS_COMPILE

//*****************************************************************************
// Return a boolean indicating whether or not this hash table has been inited.
//*****************************************************************************
    int IsInited()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_piBuckets != NULL);
    }

//*****************************************************************************
// Search the hash table for an entry with the specified key value.
//*****************************************************************************
    BYTE *Find(                         // Index of struct in m_pcEntries.
        ULONG      iHash,              // Hash value of the item.
        SIZE_T     key);               // The key to match.

//*****************************************************************************
// Search the hash table for the next entry with the specified key value.
//*****************************************************************************
    ULONG FindNext(                    // Index of struct in m_pcEntries.
        SIZE_T     key,                // The key to match.
        ULONG      iIndex);            // Index of previous match.

//*****************************************************************************
// Returns the first entry in the first hash bucket and inits the search
// struct.  Use the FindNextEntry function to continue walking the list.  The
// return order is not gauranteed.
//*****************************************************************************
    BYTE *FindFirstEntry(               // First entry found, or 0.
        HASHFIND    *psSrch)            // Search object.
    {
        WRAPPER_NO_CONTRACT;
        if (m_piBuckets == 0)
            return (0);
        psSrch->iBucket = 1;
        psSrch->iNext = m_piBuckets[0];
        return (FindNextEntry(psSrch));
    }

//*****************************************************************************
// Returns the next entry in the list.
//*****************************************************************************
    BYTE *FindNextEntry(                // The next entry, or0 for end of list.
        HASHFIND    *psSrch);           // Search object.

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                           ULONG numEntries);
#endif

protected:
    virtual BOOL Cmp(SIZE_T key1, const HASHENTRY * pc2) = 0;
};


class CNewData
{
public:
    static BYTE *Alloc(int iSize, int iMaxSize)
    {
        WRAPPER_NO_CONTRACT;
        return (new BYTE[iSize]);
    }
    static void Free(BYTE *pPtr, int iSize)
    {
        LIMITED_METHOD_CONTRACT;
        delete [] pPtr;
    }
    static BYTE *Grow(BYTE *&pPtr, int iCurSize)
    {
        WRAPPER_NO_CONTRACT;
        BYTE *p;
        S_SIZE_T newSize = S_SIZE_T(iCurSize) + S_SIZE_T(GrowSize(iCurSize));
        //check for overflow
        if(newSize.IsOverflow())
            p = NULL;
        else
            p = new (nothrow) BYTE[newSize.Value()];
        if (p == 0) return (0);
        memcpy (p, pPtr, iCurSize);
        delete [] pPtr;
        pPtr = p;
        return pPtr;
    }
    static void Clean(BYTE * pData, int iSize)
    {
    }
    static int RoundSize(int iSize)
    {
        LIMITED_METHOD_CONTRACT;
        return (iSize);
    }
    static int GrowSize(int iCurSize)
    {
        LIMITED_METHOD_CONTRACT;
        int newSize = (3 * iCurSize) / 2;
        return (newSize < 256) ? 256 : newSize;
    }
};

class CNewDataNoThrow
{
public:
    static BYTE *Alloc(int iSize, int iMaxSize)
    {
        WRAPPER_NO_CONTRACT;
        return (new (nothrow) BYTE[iSize]);
    }
    static void Free(BYTE *pPtr, int iSize)
    {
        LIMITED_METHOD_CONTRACT;
        delete [] pPtr;
    }
    static BYTE *Grow(BYTE *&pPtr, int iCurSize)
    {
        WRAPPER_NO_CONTRACT;
        BYTE *p;
        S_SIZE_T newSize = S_SIZE_T(iCurSize) + S_SIZE_T(GrowSize(iCurSize));
        //check for overflow
        if(newSize.IsOverflow())
            p = NULL;
        else
            p = new (nothrow) BYTE[newSize.Value()];
        if (p == 0) return (0);
        memcpy (p, pPtr, iCurSize);
        delete [] pPtr;
        pPtr = p;
        return pPtr;
    }
    static void Clean(BYTE * pData, int iSize)
    {
    }
    static int RoundSize(int iSize)
    {
        LIMITED_METHOD_CONTRACT;
        return (iSize);
    }
    static int GrowSize(int iCurSize)
    {
        LIMITED_METHOD_CONTRACT;
        int newSize = (3 * iCurSize) / 2;
        return (newSize < 256) ? 256 : newSize;
    }
};


//*****************************************************************************
// IMPORTANT: This data structure is deprecated, please do not add any new uses.
// The hashtable implementation that should be used instead is code:SHash.
// If code:SHash does not work for you, talk to mailto:clrdeag.
//*****************************************************************************
// CHashTable expects the data to be in a single array - this is provided by
// CHashTableAndData.
// The array is allocated using the MemMgr type. CNewData and
// CNewDataNoThrow can be used for this.
//*****************************************************************************
template <class MemMgr>
class CHashTableAndData : public CHashTable
{
public:
    DAC_ALIGNAS(CHashTable)
    ULONG      m_iFree;                // Index into m_pcEntries[] of next available slot
    ULONG      m_iEntries;             // size of m_pcEntries[]

public:

    CHashTableAndData() :
        CHashTable()
    {
        LIMITED_METHOD_CONTRACT;
    }

    CHashTableAndData(
        ULONG      iBuckets) :         // # of chains we are hashing into.
        CHashTable(iBuckets)
    {
        LIMITED_METHOD_CONTRACT;
    }

#ifndef DACCESS_COMPILE

    ~CHashTableAndData()
    {
        WRAPPER_NO_CONTRACT;
        if (m_pcEntries != NULL)
            MemMgr::Free((BYTE*)m_pcEntries, MemMgr::RoundSize(m_iEntries * m_iEntrySize));
    }

//*****************************************************************************
// This is the second part of construction where we do all of the work that
// can fail.  We also take the array of structs here because the calling class
// presumably needs to allocate it in its NewInit.
//*****************************************************************************
    HRESULT NewInit(                    // Return status.
        ULONG      iEntries,           // # of entries.
        ULONG      iEntrySize,         // Size of the entries.
        int         iMaxSize);          // Max size of data.

//*****************************************************************************
// Clear the hash table as if there were nothing in it.
//*****************************************************************************
    void Clear()
    {
        WRAPPER_NO_CONTRACT;
        m_iFree = 0;
        InitFreeChain(0, m_iEntries);
        CHashTable::Clear();
    }

//*****************************************************************************
// Grabs a slot for the new entry to be added.
// The caller should fill in the non-HASHENTRY part of the returned slot
//*****************************************************************************
    BYTE *Add(
        ULONG      iHash)              // Hash value of entry to add.
    {
        WRAPPER_NO_CONTRACT;
        FREEHASHENTRY *psEntry;

        // Make the table bigger if necessary.
        if (m_iFree == UINT32_MAX && !Grow())
            return (NULL);

        // Add the first entry from the free list to the hash chain.
        psEntry = (FREEHASHENTRY *) CHashTable::Add(iHash, m_iFree);
        m_iFree = psEntry->iFree;

        // If we're recycling memory, give our memory-allocator a chance to re-init it.

        // Each entry is prefixed with a header - we don't want to trash that.
        SIZE_T cbHeader = sizeof(FREEHASHENTRY);
        MemMgr::Clean((BYTE*) psEntry + cbHeader, (int) (m_iEntrySize - cbHeader));

        return ((BYTE *) psEntry);
    }

//*****************************************************************************
// Delete the struct at the specified index in m_pcEntries from the hash chains.
//*****************************************************************************
    void Delete(
        ULONG      iHash,              // Hash value of entry to delete.
        ULONG      iIndex)             // Index of struct in m_pcEntries.
    {
        WRAPPER_NO_CONTRACT;
        CHashTable::Delete(iHash, iIndex);
        ((FREEHASHENTRY *) EntryPtr(iIndex))->iFree = m_iFree;
        m_iFree = iIndex;
    }

    void Delete(
        ULONG      iHash,              // Hash value of entry to delete.
        HASHENTRY   *psEntry)           // The struct to delete.
    {
        WRAPPER_NO_CONTRACT;
        CHashTable::Delete(iHash, psEntry);
        ((FREEHASHENTRY *) psEntry)->iFree = m_iFree;
        m_iFree = ItemIndex(psEntry);
    }

#endif // #ifndef DACCESS_COMPILE

    // This is a sad legacy workaround. The debugger's patch table (implemented as this
    // class) is shared across process. We publish the runtime offsets of
    // some key fields. Since those fields are private, we have to provide
    // accessors here. So if you're not using these functions, don't start.
    // We can hopefully remove them.
    // Note that we can't just make RCThread a friend of this class (we tried
    // originally) because the inheritence chain has a private modifier,
    // so DebuggerPatchTable::m_pcEntries is illegal.
    static SIZE_T helper_GetOffsetOfEntries()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(CHashTableAndData, m_pcEntries);
    }

    static SIZE_T helper_GetOffsetOfCount()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(CHashTableAndData, m_iEntries);
    }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
    {
        SUPPORTS_DAC;
        CHashTable::EnumMemoryRegions(flags, m_iEntries);
    }
#endif

private:
    void InitFreeChain(ULONG iStart,ULONG iEnd);
    int Grow();
};

#ifndef DACCESS_COMPILE

//*****************************************************************************
// This is the second part of construction where we do all of the work that
// can fail.  We also take the array of structs here because the calling class
// presumably needs to allocate it in its NewInit.
//*****************************************************************************
template<class MemMgr>
HRESULT CHashTableAndData<MemMgr>::NewInit(// Return status.
    ULONG      iEntries,               // # of entries.
    ULONG      iEntrySize,             // Size of the entries.
    int         iMaxSize)               // Max size of data.
{
    WRAPPER_NO_CONTRACT;
    BYTE        *pcEntries;
    HRESULT     hr;


    // note that this function can throw because it depends on the <M>::Alloc

    // Allocate the memory for the entries.
    if ((pcEntries = MemMgr::Alloc(MemMgr::RoundSize(iEntries * iEntrySize),
                                   MemMgr::RoundSize(iMaxSize))) == 0)
        return (E_OUTOFMEMORY);
    m_iEntries = iEntries;

    // Init the base table.
    if (FAILED(hr = CHashTable::NewInit(pcEntries, iEntrySize)))
        MemMgr::Free(pcEntries, MemMgr::RoundSize(iEntries * iEntrySize));
    else
    {
        // Init the free chain.
        m_iFree = 0;
        InitFreeChain(0, iEntries);
    }
    return (hr);
}

//*****************************************************************************
// Initialize a range of records such that they are linked together to be put
// on the free chain.
//*****************************************************************************
template<class MemMgr>
void CHashTableAndData<MemMgr>::InitFreeChain(
    ULONG      iStart,                 // Index to start initializing.
    ULONG      iEnd)                   // Index to stop initializing
{
    LIMITED_METHOD_CONTRACT;
    BYTE* pcPtr;
    _ASSERTE(iEnd > iStart);

    pcPtr = (BYTE*)m_pcEntries + iStart * (size_t)m_iEntrySize;
    for (++iStart; iStart < iEnd; ++iStart)
    {
        ((FREEHASHENTRY *) pcPtr)->iFree = iStart;
        pcPtr += m_iEntrySize;
    }
    ((FREEHASHENTRY *) pcPtr)->iFree = UINT32_MAX;
}

//*****************************************************************************
// Attempt to increase the amount of space available for the record heap.
//*****************************************************************************
template<class MemMgr>
int CHashTableAndData<MemMgr>::Grow()   // 1 if successful, 0 if not.
{
    WRAPPER_NO_CONTRACT;
    int         iCurSize;               // Current size in bytes.
    int         iEntries;               // New # of entries.

    _ASSERTE(m_pcEntries != NULL);
    _ASSERTE(m_iFree == UINT32_MAX);

    // Compute the current size and new # of entries.
    S_UINT32 iTotEntrySize = S_UINT32(m_iEntries) * S_UINT32(m_iEntrySize);
    if( iTotEntrySize.IsOverflow() )
    {
        _ASSERTE( !"CHashTableAndData overflow!" );
        return (0);
    }
    iCurSize = MemMgr::RoundSize( iTotEntrySize.Value() );
    iEntries = (iCurSize + MemMgr::GrowSize(iCurSize)) / m_iEntrySize;

    if ( (iEntries < 0) || ((ULONG)iEntries <= m_iEntries) )
    {
        _ASSERTE( !"CHashTableAndData overflow!" );
        return (0);
    }

    // Try to expand the array.
    if (MemMgr::Grow(*(BYTE**)&m_pcEntries, iCurSize) == 0)
        return (0);

    // Init the newly allocated space.
    InitFreeChain(m_iEntries, iEntries);
    m_iFree = m_iEntries;
    m_iEntries = iEntries;
    return (1);
}

#endif // #ifndef DACCESS_COMPILE

//*****************************************************************************
//*****************************************************************************

inline COUNT_T HashCOUNT_T(COUNT_T currentHash, COUNT_T data)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return ((currentHash << 5) + currentHash) ^ data;
}

inline COUNT_T HashPtr(COUNT_T currentHash, PTR_VOID ptr)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    return HashCOUNT_T(currentHash, COUNT_T(SIZE_T(dac_cast<TADDR>(ptr))));
}

inline DWORD HashThreeToOne(DWORD a, DWORD b, DWORD c)
{
    LIMITED_METHOD_DAC_CONTRACT;

    /*
    lookup3.c, by Bob Jenkins, May 2006, Public Domain.

    These are functions for producing 32-bit hashes for hash table lookup.
    hashword(), hashlittle(), hashlittle2(), hashbig(), mix(), and final()
    are externally useful functions.  Routines to test the hash are included
    if SELF_TEST is defined.  You can use this free for any purpose.  It's in
    the public domain.  It has no warranty.
    */

    #define rot32(x,k) (((x)<<(k)) | ((x)>>(32-(k))))
    c ^= b; c -= rot32(b,14);
    a ^= c; a -= rot32(c,11);
    b ^= a; b -= rot32(a,25);
    c ^= b; c -= rot32(b,16);
    a ^= c; a -= rot32(c,4);
    b ^= a; b -= rot32(a,14);
    c ^= b; c -= rot32(b,24);

    return c;
}

inline ULONG HashBytes(BYTE const *pbData, size_t iSize)
{
    LIMITED_METHOD_CONTRACT;
    ULONG   hash = 5381;

    BYTE const *pbDataEnd = pbData + iSize;

    for (/**/ ; pbData < pbDataEnd; pbData++)
    {
        hash = ((hash << 5) + hash) ^ *pbData;
    }
    return hash;
}

// Helper function for hashing a string char by char.
inline ULONG HashStringA(LPCSTR szStr)
{
    LIMITED_METHOD_CONTRACT;
    ULONG   hash = 5381;
    int     c;

    while ((c = *szStr) != 0)
    {
        hash = ((hash << 5) + hash) ^ c;
        ++szStr;
    }
    return hash;
}

inline ULONG HashString(LPCWSTR szStr)
{
    LIMITED_METHOD_CONTRACT;
    ULONG   hash = 5381;
    int     c;

    while ((c = *szStr) != 0)
    {
        hash = ((hash << 5) + hash) ^ c;
        ++szStr;
    }
    return hash;
}

inline ULONG HashStringN(LPCWSTR szStr, SIZE_T cchStr)
{
    LIMITED_METHOD_CONTRACT;
    ULONG   hash = 5381;

    // hash the string two characters at a time
    ULONG *ptr = (ULONG *)szStr;

    // we assume that szStr is null-terminated
    _ASSERTE(cchStr <= wcslen(szStr));
    SIZE_T cDwordCount = (cchStr + 1) / 2;

    for (SIZE_T i = 0; i < cDwordCount; i++)
    {
        hash = ((hash << 5) + hash) ^ ptr[i];
    }

    return hash;
}

// Case-insensitive string hash function.
inline ULONG HashiStringA(LPCSTR szStr)
{
    LIMITED_METHOD_CONTRACT;
    ULONG   hash = 5381;
    while (*szStr != 0)
    {
        hash = ((hash << 5) + hash) ^ toupper(*szStr);
        szStr++;
    }
    return hash;
}

// Case-insensitive string hash function.
inline ULONG HashiString(LPCWSTR szStr)
{
    LIMITED_METHOD_CONTRACT;
    ULONG   hash = 5381;
    while (*szStr != 0)
    {
        hash = ((hash << 5) + hash) ^ towupper(*szStr);
        szStr++;
    }
    return hash;
}

// Case-insensitive string hash function.
inline ULONG HashiStringN(LPCWSTR szStr, DWORD count)
{
    LIMITED_METHOD_CONTRACT;
    ULONG   hash = 5381;
    while (*szStr != 0 && count--)
    {
        hash = ((hash << 5) + hash) ^ towupper(*szStr);
        szStr++;
    }
    return hash;
}

// Case-insensitive string hash function when all of the
// characters in the string are known to be below 0x80.
// Knowing this is much more efficient than calling
// towupper above.
inline ULONG HashiStringKnownLower80(LPCWSTR szStr) {
    LIMITED_METHOD_CONTRACT;
    ULONG hash = 5381;
    int c;
    int mask = ~0x20;
    while ((c = *szStr)!=0) {
        //If we have a lowercase character, ANDing off 0x20
        //(mask) will make it an uppercase character.
        if (c>='a' && c<='z') {
            c&=mask;
        }
        hash = ((hash << 5) + hash) ^ c;
        ++szStr;
    }
    return hash;
}

inline ULONG HashiStringNKnownLower80(LPCWSTR szStr, DWORD count) {
    LIMITED_METHOD_CONTRACT;
    ULONG hash = 5381;
    int c;
    int mask = ~0x20;
    while ((c = *szStr) !=0 && count--) {
        //If we have a lowercase character, ANDing off 0x20
        //(mask) will make it an uppercase character.
        if (c>='a' && c<='z') {
            c&=mask;
        }
        hash = ((hash << 5) + hash) ^ c;
        ++szStr;
    }
    return hash;
}

//*****************************************************************************
// IMPORTANT: This data structure is deprecated, please do not add any new uses.
// The hashtable implementation that should be used instead is code:SHash.
// If code:SHash does not work for you, talk to mailto:clrdeag.
//*****************************************************************************
// This class implements a closed hashing table.  Values are hashed to a bucket,
// and if that bucket is full already, then the value is placed in the next
// free bucket starting after the desired target (with wrap around).  If the
// table becomes 75% full, it is grown and rehashed to reduce lookups.  This
// class is best used in a reltively small lookup table where hashing is
// not going to cause many collisions.  By not having the collision chain
// logic, a lot of memory is saved.
//
// The user of the template is required to supply several methods which decide
// how each element can be marked as free, deleted, or used.  It would have
// been possible to write this with more internal logic, but that would require
// either (a) more overhead to add status on top of elements, or (b) hard
// coded types like one for strings, one for ints, etc... This gives you the
// flexibility of adding logic to your type.
//*****************************************************************************
class CClosedHashBase
{
    BYTE *EntryPtr(int iEntry)
    {
        LIMITED_METHOD_CONTRACT;
        return (m_rgData + (iEntry * (size_t)m_iEntrySize));
    }

    BYTE *EntryPtr(int iEntry, BYTE *rgData)
    {
        LIMITED_METHOD_CONTRACT;
        return (rgData + (iEntry * (size_t)m_iEntrySize));
    }

public:
    enum ELEMENTSTATUS
    {
        FREE,                               // Item is not in use right now.
        DELETED,                            // Item is deleted.
        USED                                // Item is in use.
    };

    CClosedHashBase(
        int         iBuckets,               // How many buckets should we start with.
        int         iEntrySize,             // Size of an entry.
        bool        bPerfect) :             // true if bucket size will hash with no collisions.
        m_bPerfect(bPerfect),
        m_iBuckets(iBuckets),
        m_iEntrySize(iEntrySize),
        m_iCount(0),
        m_iCollisions(0),
        m_rgData(0)
    {
        LIMITED_METHOD_CONTRACT;
        m_iSize = iBuckets + 7;
    }

    virtual ~CClosedHashBase()
    {
        WRAPPER_NO_CONTRACT;
        Clear();
    }

    virtual void Clear()
    {
        LIMITED_METHOD_CONTRACT;
        delete [] m_rgData;
        m_iCount = 0;
        m_iCollisions = 0;
        m_rgData = 0;
    }

//*****************************************************************************
// Accessors for getting at the underlying data.  Be careful to use Count()
// only when you want the number of buckets actually used.
//*****************************************************************************

    int Count()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_iCount);
    }

    int Collisions()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_iCollisions);
    }

    int Buckets()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_iBuckets);
    }

    void SetBuckets(int iBuckets, bool bPerfect=false)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_rgData == 0);
        m_iBuckets = iBuckets;
        m_iSize = m_iBuckets + 7;
        m_bPerfect = bPerfect;
    }

    BYTE *Data()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_rgData);
    }

//*****************************************************************************
// Add a new item to hash table given the key value.  If this new entry
// exceeds maximum size, then the table will grow and be re-hashed, which
// may cause a memory error.
//*****************************************************************************
    BYTE *Add(                              // New item to fill out on success.
        void        *pData)                 // The value to hash on.
    {
        WRAPPER_NO_CONTRACT;
        // If we haven't allocated any memory, or it is too small, fix it.
        if (!m_rgData || ((m_iCount + 1) > (m_iSize * 3 / 4) && !m_bPerfect))
        {
            if (!ReHash())
                return (0);
        }

        return (DoAdd(pData, m_rgData, m_iBuckets, m_iSize, m_iCollisions, m_iCount));
    }

//*****************************************************************************
// Delete the given value.  This will simply mark the entry as deleted (in
// order to keep the collision chain intact).  There is an optimization that
// consecutive deleted entries leading up to a free entry are themselves freed
// to reduce collisions later on.
//*****************************************************************************
    void Delete(
        void        *pData);                // Key value to delete.


//*****************************************************************************
//  Callback function passed to DeleteLoop.
//*****************************************************************************
    typedef BOOL (* DELETELOOPFUNC)(        // Delete current item?
         BYTE *pEntry,                      // Bucket entry to evaluate
         void *pCustomizer);                // User-defined value

//*****************************************************************************
// Iterates over all active values, passing each one to pDeleteLoopFunc.
// If pDeleteLoopFunc returns TRUE, the entry is deleted. This is safer
// and faster than using FindNext() and Delete().
//*****************************************************************************
    void DeleteLoop(
        DELETELOOPFUNC pDeleteLoopFunc,     // Decides whether to delete item
        void *pCustomizer);                 // Extra value passed to deletefunc.


//*****************************************************************************
// Lookup a key value and return a pointer to the element if found.
//*****************************************************************************
    BYTE *Find(                             // The item if found, 0 if not.
        void        *pData);                // The key to lookup.

//*****************************************************************************
// Look for an item in the table.  If it isn't found, then create a new one and
// return that.
//*****************************************************************************
    BYTE *FindOrAdd(                        // The item if found, 0 if not.
        void        *pData,                 // The key to lookup.
        bool        &bNew);                 // true if created.

//*****************************************************************************
// The following functions are used to traverse each used entry.  This code
// will skip over deleted and free entries freeing the caller up from such
// logic.
//*****************************************************************************
    BYTE *GetFirst()                        // The first entry, 0 if none.
    {
        WRAPPER_NO_CONTRACT;
        int         i;                      // Loop control.

        // If we've never allocated the table there can't be any to get.
        if (m_rgData == 0)
            return (0);

        // Find the first one.
        for (i=0;  i<m_iSize;  i++)
        {
            if (Status(EntryPtr(i)) != FREE && Status(EntryPtr(i)) != DELETED)
                return (EntryPtr(i));
        }
        return (0);
    }

    BYTE *GetNext(BYTE *Prev)               // The next entry, 0 if done.
    {
        WRAPPER_NO_CONTRACT;
        int         i;                      // Loop control.

        for (i = (int)(((size_t) Prev - (size_t) &m_rgData[0]) / m_iEntrySize) + 1; i<m_iSize;  i++)
        {
            if (Status(EntryPtr(i)) != FREE && Status(EntryPtr(i)) != DELETED)
                return (EntryPtr(i));
        }
        return (0);
    }

private:
//*****************************************************************************
// Hash is called with a pointer to an element in the table.  You must override
// this method and provide a hash algorithm for your element type.
//*****************************************************************************
    virtual unsigned int Hash(             // The key value.
        void const  *pData)=0;              // Raw data to hash.

//*****************************************************************************
// Compare is used in the typical memcmp way, 0 is eqaulity, -1/1 indicate
// direction of miscompare.  In this system everything is always equal or not.
//*****************************************************************************
    virtual unsigned int Compare(          // 0, -1, or 1.
        void const  *pData,                 // Raw key data on lookup.
        BYTE        *pElement)=0;           // The element to compare data against.

//*****************************************************************************
// Return true if the element is free to be used.
//*****************************************************************************
    virtual ELEMENTSTATUS Status(           // The status of the entry.
        BYTE        *pElement)=0;           // The element to check.

//*****************************************************************************
// Sets the status of the given element.
//*****************************************************************************
    virtual void SetStatus(
        BYTE        *pElement,              // The element to set status for.
        ELEMENTSTATUS eStatus)=0;           // New status.

//*****************************************************************************
// Returns the internal key value for an element.
//*****************************************************************************
    virtual void *GetKey(                   // The data to hash on.
        BYTE        *pElement)=0;           // The element to return data ptr for.

//*****************************************************************************
// This helper actually does the add for you.
//*****************************************************************************
    BYTE *DoAdd(void *pData, BYTE *rgData, int &iBuckets, int iSize,
                int &iCollisions, int &iCount);

//*****************************************************************************
// This function is called either to init the table in the first place, or
// to rehash the table if we ran out of room.
//*****************************************************************************
    bool ReHash();                          // true if successful.

//*****************************************************************************
// Walk each item in the table and mark it free.
//*****************************************************************************
    void InitFree(BYTE *ptr, int iSize)
    {
        WRAPPER_NO_CONTRACT;
        int         i;
        for (i=0;  i<iSize;  i++, ptr += m_iEntrySize)
            SetStatus(ptr, FREE);
    }

private:
    bool        m_bPerfect;                 // true if the table size guarantees
                                            //  no collisions.
    int         m_iBuckets;                 // How many buckets do we have.
    int         m_iEntrySize;               // Size of an entry.
    int         m_iSize;                    // How many elements can we have.
    int         m_iCount;                   // How many items cannot be used (NON free, i.e. USED+DELETED).
    int         m_iCollisions;              // How many have we had.
    BYTE        *m_rgData;                  // Data element list.
};

//*****************************************************************************
// IMPORTANT: This data structure is deprecated, please do not add any new uses.
// The hashtable implementation that should be used instead is code:SHash.
// If code:SHash does not work for you, talk to mailto:clrdeag.
//*****************************************************************************
template <class T> class CClosedHash : public CClosedHashBase
{
public:
    CClosedHash(
        int         iBuckets,               // How many buckets should we start with.
        bool        bPerfect=false) :       // true if bucket size will hash with no collisions.
        CClosedHashBase(iBuckets, sizeof(T), bPerfect)
    {
        WRAPPER_NO_CONTRACT;
    }

    T &operator[](int iIndex)
    {
        WRAPPER_NO_CONTRACT;
        return ((T &) *(Data() + (iIndex * sizeof(T))));
    }


//*****************************************************************************
// Add a new item to hash table given the key value.  If this new entry
// exceeds maximum size, then the table will grow and be re-hashed, which
// may cause a memory error.
//*****************************************************************************
    T *Add(                                 // New item to fill out on success.
        void        *pData)                 // The value to hash on.
    {
        WRAPPER_NO_CONTRACT;
        return ((T *) CClosedHashBase::Add(pData));
    }

//*****************************************************************************
// Lookup a key value and return a pointer to the element if found.
//*****************************************************************************
    T *Find(                                // The item if found, 0 if not.
        void        *pData)                 // The key to lookup.
    {
        WRAPPER_NO_CONTRACT;
        return ((T *) CClosedHashBase::Find(pData));
    }

//*****************************************************************************
// Look for an item in the table.  If it isn't found, then create a new one and
// return that.
//*****************************************************************************
    T *FindOrAdd(                           // The item if found, 0 if not.
        void        *pData,                 // The key to lookup.
        bool        &bNew)                  // true if created.
    {
        WRAPPER_NO_CONTRACT;
        return ((T *) CClosedHashBase::FindOrAdd(pData, bNew));
    }


//*****************************************************************************
// The following functions are used to traverse each used entry.  This code
// will skip over deleted and free entries freeing the caller up from such
// logic.
//*****************************************************************************
    T *GetFirst()                           // The first entry, 0 if none.
    {
        WRAPPER_NO_CONTRACT;
        return ((T *) CClosedHashBase::GetFirst());
    }

    T *GetNext(T *Prev)                     // The next entry, 0 if done.
    {
        WRAPPER_NO_CONTRACT;
        return ((T *) CClosedHashBase::GetNext((BYTE *) Prev));
    }
};


//*****************************************************************************
// IMPORTANT: This data structure is deprecated, please do not add any new uses.
// The hashtable implementation that should be used instead is code:SHash.
// If code:SHash does not work for you, talk to mailto:clrdeag.
//*****************************************************************************
// Closed hash with typed parameters.  The derived class is the second
//  parameter to the template.  The derived class must implement:
//    unsigned long Hash(const T *pData);
//    unsigned long Compare(const T *p1, T *p2);
//    ELEMENTSTATUS Status(T *pEntry);
//    void SetStatus(T *pEntry, ELEMENTSTATUS s);
//    void* GetKey(T *pEntry);
//*****************************************************************************
template<class T, class H>class CClosedHashEx : public CClosedHash<T>
{
public:
    CClosedHashEx(
        int         iBuckets,               // How many buckets should we start with.
        bool        bPerfect=false) :       // true if bucket size will hash with no collisions.
        CClosedHash<T> (iBuckets, bPerfect)
    {
        WRAPPER_NO_CONTRACT;
    }

    unsigned int Hash(const void *pData)
    {
        WRAPPER_NO_CONTRACT;
        return static_cast<H*>(this)->Hash((const T*)pData);
    }

    unsigned int Compare(const void *p1, BYTE *p2)
    {
        WRAPPER_NO_CONTRACT;
        return static_cast<H*>(this)->Compare((const T*)p1, (T*)p2);
    }

    typename CClosedHash<T>::ELEMENTSTATUS Status(BYTE *p)
    {
        WRAPPER_NO_CONTRACT;
        return static_cast<H*>(this)->Status((T*)p);
    }

    void SetStatus(BYTE *p, typename CClosedHash<T>::ELEMENTSTATUS s)
    {
        WRAPPER_NO_CONTRACT;
        static_cast<H*>(this)->SetStatus((T*)p, s);
    }

    void* GetKey(BYTE *p)
    {
        WRAPPER_NO_CONTRACT;
        return static_cast<H*>(this)->GetKey((T*)p);
    }
};


//*****************************************************************************
// IMPORTANT: This data structure is deprecated, please do not add any new uses.
// The hashtable implementation that should be used instead is code:SHash.
// If code:SHash does not work for you, talk to mailto:clrdeag.
//*****************************************************************************
// This template is another form of a closed hash table.  It handles collisions
// through a linked chain.  To use it, derive your hashed item from HASHLINK
// and implement the virtual functions required.  1.5 * ibuckets will be
// allocated, with the extra .5 used for collisions.  If you add to the point
// where no free nodes are available, the entire table is grown to make room.
// The advantage to this system is that collisions are always directly known,
// there either is one or there isn't.
//*****************************************************************************
struct HASHLINK
{
    ULONG       iNext;                  // Offset for next entry.
};

template <class T> class CChainedHash
{
    friend class VerifyLayoutsMD;
public:
    CChainedHash(int iBuckets=32) :
        m_rgData(0),
        m_iBuckets(iBuckets),
        m_iCount(0),
        m_iMaxChain(0),
        m_iFree(0)
    {
        LIMITED_METHOD_CONTRACT;
        m_iSize = iBuckets + (iBuckets / 2);
    }

    ~CChainedHash()
    {
        LIMITED_METHOD_CONTRACT;
        if (m_rgData)
            delete [] m_rgData;
    }

    void SetBuckets(int iBuckets)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_rgData == 0);
        // if iBuckets==0, then we'll allocate a zero size array and AV on dereference.
        _ASSERTE(iBuckets > 0);
        m_iBuckets = iBuckets;
        m_iSize = iBuckets + (iBuckets / 2);
    }

    T *Add(void const *pData)
    {
        WRAPPER_NO_CONTRACT;
        ULONG       iHash;
        int         iBucket;
        T           *pItem;

        // Build the list if required.
        if (m_rgData == 0 || m_iFree == 0xffffffff)
        {
            if (!ReHash())
                return (0);
        }

        // Hash the item and pick a bucket.
        iHash = Hash(pData);
        iBucket = iHash % m_iBuckets;

        // Use the bucket if it is free.
        if (InUse(&m_rgData[iBucket]) == false)
        {
            pItem = &m_rgData[iBucket];
            pItem->iNext = 0xffffffff;
        }
        // Else take one off of the free list for use.
        else
        {
            ULONG       iEntry;

            // Pull an item from the free list.
            iEntry = m_iFree;
            pItem = &m_rgData[m_iFree];
            m_iFree = pItem->iNext;

            // Link the new node in after the bucket.
            pItem->iNext = m_rgData[iBucket].iNext;
            m_rgData[iBucket].iNext = iEntry;
        }
        ++m_iCount;
        return (pItem);
    }

    T *Find(void const *pData, bool bAddIfNew=false)
    {
        WRAPPER_NO_CONTRACT;
        ULONG       iHash;
        int         iBucket;
        T           *pItem;

        // Check states for lookup.
        if (m_rgData == 0)
        {
            // If we won't be adding, then we are through.
            if (bAddIfNew == false)
                return (0);

            // Otherwise, create the table.
            if (!ReHash())
                return (0);
        }

        // Hash the item and pick a bucket.
        iHash = Hash(pData);
        iBucket = iHash % m_iBuckets;

        // If it isn't in use, then there it wasn't found.
        if (!InUse(&m_rgData[iBucket]))
        {
            if (bAddIfNew == false)
                pItem = 0;
            else
            {
                pItem = &m_rgData[iBucket];
                pItem->iNext = 0xffffffff;
                ++m_iCount;
            }
        }
        // Scan the list for the one we want.
        else
        {
            ULONG iChain = 0;
            for (pItem=(T *) &m_rgData[iBucket];  pItem;  pItem=GetNext(pItem))
            {
                if (Cmp(pData, pItem) == 0)
                    break;
                ++iChain;
            }

            if (!pItem && bAddIfNew)
            {
                ULONG       iEntry;

                // Record maximum chain length.
                if (iChain > m_iMaxChain)
                    m_iMaxChain = iChain;

                // Now need more room.
                if (m_iFree == 0xffffffff)
                {
                    if (!ReHash())
                        return (0);
                }

                // Pull an item from the free list.
                iEntry = m_iFree;
                pItem = &m_rgData[m_iFree];
                m_iFree = pItem->iNext;

                // Link the new node in after the bucket.
                pItem->iNext = m_rgData[iBucket].iNext;
                m_rgData[iBucket].iNext = iEntry;
                ++m_iCount;
            }
        }
        return (pItem);
    }

    int Count()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_iCount);
    }

    int Buckets()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_iBuckets);
    }

    ULONG MaxChainLength()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_iMaxChain);
    }

    virtual void Clear()
    {
        LIMITED_METHOD_CONTRACT;
        // Free up the memory.
        if (m_rgData)
        {
            delete [] m_rgData;
            m_rgData = 0;
        }

        m_rgData = 0;
        m_iFree = 0;
        m_iCount = 0;
        m_iMaxChain = 0;
    }

    virtual bool InUse(T *pItem)=0;
    virtual void SetFree(T *pItem)=0;
    virtual ULONG Hash(void const *pData)=0;
    virtual int Cmp(void const *pData, void *pItem)=0;
private:
    inline T *GetNext(T *pItem)
    {
        LIMITED_METHOD_CONTRACT;
        if (pItem->iNext != 0xffffffff)
            return ((T *) &m_rgData[pItem->iNext]);
        return (0);
    }

    bool ReHash()
    {
        WRAPPER_NO_CONTRACT;
        T           *rgTemp;
        int         iNewSize;

        // If this is a first time allocation, then just malloc it.
        if (!m_rgData)
        {
            if ((m_rgData = new (nothrow) T[m_iSize]) == 0)
                return (false);

            int i;
            for (i=0;  i<m_iSize;  i++)
                SetFree(&m_rgData[i]);

            m_iFree = m_iBuckets;
            for (i=m_iBuckets;  i<m_iSize;  i++)
                ((T *) &m_rgData[i])->iNext = i + 1;
            ((T *) &m_rgData[m_iSize - 1])->iNext = 0xffffffff;
            return (true);
        }

        // Otherwise we need more room on the free chain, so allocate some.
        iNewSize = m_iSize + (m_iSize / 2);

        // Allocate/realloc memory.
        if ((rgTemp = new (nothrow) T[iNewSize]) == 0)
            return (false);

        memcpy (rgTemp,m_rgData,m_iSize*sizeof(T));
        delete [] m_rgData;

        // Init new entries, save the new free chain, and reset internals.
        m_iFree = m_iSize;
        for (int i=m_iFree;  i<iNewSize;  i++)
        {
            SetFree(&rgTemp[i]);
            ((T *) &rgTemp[i])->iNext = i + 1;
        }
        ((T *) &rgTemp[iNewSize - 1])->iNext = 0xffffffff;

        m_rgData = rgTemp;
        m_iSize = iNewSize;
        return (true);
    }

private:
    T           *m_rgData;              // Data to store items in.
    int         m_iBuckets;             // How many buckets we want.
    int         m_iSize;                // How many are allocated.
    int         m_iCount;               // How many are we using.
    ULONG       m_iMaxChain;            // Max chain length.
    ULONG       m_iFree;                // Free chain.
};


//*****************************************************************************
//
//********** String helper functions.
//
//*****************************************************************************

//*****************************************************************************
// Checks if string length exceeds the specified limit
//*****************************************************************************
inline BOOL IsStrLongerThan(__in __in_z char* pstr, unsigned N)
{
    LIMITED_METHOD_CONTRACT;
    unsigned i = 0;
    if(pstr)
    {
        for(i=0; (i < N)&&(pstr[i]); i++);
    }
    return (i >= N);
}


//*****************************************************************************
// Class to parse a list of simple assembly names and then find a match
//*****************************************************************************

class AssemblyNamesList
{
    struct AssemblyName
    {
        LPUTF8          m_assemblyName;
        AssemblyName   *m_next;         // Next name
    };

    AssemblyName       *m_pNames;       // List of names

public:

    bool IsInList(LPCUTF8 assemblyName);

    bool IsEmpty()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pNames == 0;
    }

    AssemblyNamesList(__in LPWSTR list);
    ~AssemblyNamesList();
};

//*****************************************************************************
// Class to parse a list of method names and then find a match
//*****************************************************************************

struct CORINFO_SIG_INFO;

class MethodNamesListBase
{
    struct MethodName
    {
        LPUTF8      methodName;     // NULL means wildcard
        LPUTF8      className;      // NULL means wildcard
        int         numArgs;        // number of args for the method, -1 is wildcard
        MethodName *next;           // Next name
    };

    MethodName     *pNames;         // List of names

    bool IsInList(LPCUTF8 methodName, LPCUTF8 className, int numArgs);

public:
    void Init()
    {
        LIMITED_METHOD_CONTRACT;
        pNames = 0;
    }

    void Init(__in __in_z LPWSTR list)
    {
        WRAPPER_NO_CONTRACT;
        pNames = 0;
        Insert(list);
    }

    void Destroy();

    void Insert(__in __in_z LPWSTR list);

    bool IsInList(LPCUTF8 methodName, LPCUTF8 className, PCCOR_SIGNATURE sig = NULL);
    bool IsInList(LPCUTF8 methodName, LPCUTF8 className, CORINFO_SIG_INFO* pSigInfo);
    bool IsEmpty()
    {
        LIMITED_METHOD_CONTRACT;
        return pNames == 0;
    }
};

class MethodNamesList : public MethodNamesListBase
{
public:
    MethodNamesList()
    {
        WRAPPER_NO_CONTRACT;
        Init();
    }

    MethodNamesList(__in LPWSTR list)
    {
        WRAPPER_NO_CONTRACT;
        Init(list);
    }

    ~MethodNamesList()
    {
        WRAPPER_NO_CONTRACT;
        Destroy();
    }
};

#if !defined(NO_CLRCONFIG)

/**************************************************************************/
/* simple wrappers around the REGUTIL and MethodNameList routines that make
   the lookup lazy */

/* to be used as static variable - no constructor/destructor, assumes zero
   initialized memory */

class ConfigDWORD
{
public:
    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead.
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    //
    inline DWORD val_DontUse_(__in __in_z LPCWSTR keyName, DWORD defaultVal=0)
    {
        WRAPPER_NO_CONTRACT;
        // make sure that the memory was zero initialized
        _ASSERTE(m_inited == 0 || m_inited == 1);

        if (!m_inited) init_DontUse_(keyName, defaultVal);
        return m_value;
    }
    inline DWORD val(const CLRConfig::ConfigDWORDInfo & info)
    {
        WRAPPER_NO_CONTRACT;
        // make sure that the memory was zero initialized
        _ASSERTE(m_inited == 0 || m_inited == 1);

        if (!m_inited) init(info);
        return m_value;
    }

private:
    void init_DontUse_(__in __in_z LPCWSTR keyName, DWORD defaultVal=0);
    void init(const CLRConfig::ConfigDWORDInfo & info);

private:
    DWORD  m_value;
    BYTE m_inited;
};

/**************************************************************************/
class ConfigString
{
public:
    inline LPWSTR val(const CLRConfig::ConfigStringInfo & info)
    {
        WRAPPER_NO_CONTRACT;
        // make sure that the memory was zero initialized
        _ASSERTE(m_inited == 0 || m_inited == 1);

        if (!m_inited) init(info);
        return m_value;
    }

    bool isInitialized()
    {
        WRAPPER_NO_CONTRACT;

        // make sure that the memory was zero initialized
        _ASSERTE(m_inited == 0 || m_inited == 1);

        return m_inited == 1;
    }

private:
    void init(const CLRConfig::ConfigStringInfo & info);

private:
    LPWSTR m_value;
    BYTE m_inited;
};

/**************************************************************************/
class ConfigMethodSet
{
public:
    bool isEmpty()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(m_inited == 1);
        return m_list.IsEmpty();
    }

    bool contains(LPCUTF8 methodName, LPCUTF8 className, PCCOR_SIGNATURE sig = NULL);
    bool contains(LPCUTF8 methodName, LPCUTF8 className, CORINFO_SIG_INFO* pSigInfo);

    inline void ensureInit(const CLRConfig::ConfigStringInfo & info)
    {
        WRAPPER_NO_CONTRACT;
        // make sure that the memory was zero initialized
        _ASSERTE(m_inited == 0 || m_inited == 1);

        if (!m_inited) init(info);
    }

private:
    void init(const CLRConfig::ConfigStringInfo & info);

private:
    MethodNamesListBase m_list;

    BYTE m_inited;
};

#endif // !defined(NO_CLRCONFIG)

//*****************************************************************************
// Convert a pointer to a string into a GUID.
//*****************************************************************************
HRESULT LPCSTRToGuid(                   // Return status.
    LPCSTR      szGuid,                 // String to convert.
    GUID        *psGuid);               // Buffer for converted GUID.

//*****************************************************************************
// Convert a GUID into a pointer to a string
//*****************************************************************************
int GuidToLPWSTR(                  // Return status.
    GUID        Guid,                  // [IN] The GUID to convert.
    __out_ecount (cchGuid) LPWSTR szGuid, // [OUT] String into which the GUID is stored
    DWORD       cchGuid);              // [IN] Size in wide chars of szGuid

//*****************************************************************************
// Parse a Wide char string into a GUID
//*****************************************************************************
BOOL LPWSTRToGuid(
    GUID      * Guid,                         // [OUT] The GUID to fill in
    __in_ecount(cchGuid)   LPCWSTR szGuid,    // [IN] String to parse
    DWORD       cchGuid);                     // [IN] Count in wchars in string

typedef VPTR(class RangeList) PTR_RangeList;

class RangeList
{
  public:
    VPTR_BASE_CONCRETE_VTABLE_CLASS(RangeList)

#ifndef DACCESS_COMPILE
    RangeList();
    ~RangeList();
#else
    RangeList()
    {
        LIMITED_METHOD_CONTRACT;
    }
#endif

    // Wrappers to make the virtual calls DAC-safe.
    BOOL AddRange(const BYTE *start, const BYTE *end, void *id)
    {
        return this->AddRangeWorker(start, end, id);
    }

    void RemoveRanges(void *id, const BYTE *start = NULL, const BYTE *end = NULL)
    {
        return this->RemoveRangesWorker(id, start, end);
    }

    BOOL IsInRange(TADDR address, TADDR *pID = NULL)
    {
        SUPPORTS_DAC;

        return this->IsInRangeWorker(address, pID);
    }

#ifndef DACCESS_COMPILE

    // You can overload these two for synchronization (as LockedRangeList does)
    virtual BOOL AddRangeWorker(const BYTE *start, const BYTE *end, void *id);
    // If both "start" and "end" are NULL, then this method deletes all ranges with
    // the given id (i.e. the original behaviour).  Otherwise, it ignores the given
    // id and deletes all ranges falling in the region [start, end).
    virtual void RemoveRangesWorker(void *id, const BYTE *start = NULL, const BYTE *end = NULL);
#else
    virtual BOOL AddRangeWorker(const BYTE *start, const BYTE *end, void *id)
    {
        return TRUE;
    }
    virtual void RemoveRangesWorker(void *id, const BYTE *start = NULL, const BYTE *end = NULL) { }
#endif // !DACCESS_COMPILE

    virtual BOOL IsInRangeWorker(TADDR address, TADDR *pID = NULL);

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(enum CLRDataEnumMemoryFlags flags);
#endif

    enum
    {
        RANGE_COUNT = 10
    };


  private:
    struct Range
    {
        TADDR start;
        TADDR end;
        TADDR id;
    };

    struct RangeListBlock
    {
        Range                ranges[RANGE_COUNT];
        DPTR(RangeListBlock) next;

#ifdef DACCESS_COMPILE
        void EnumMemoryRegions(enum CLRDataEnumMemoryFlags flags);
#endif

    };

    void InitBlock(RangeListBlock *block);

    RangeListBlock       m_starterBlock;
    DPTR(RangeListBlock) m_firstEmptyBlock;
    TADDR                m_firstEmptyRange;
};


//
// A private function to do the equavilent of a CoCreateInstance in
// cases where we can't make the real call. Use this when, for
// instance, you need to create a symbol reader in the Runtime but
// we're not CoInitialized. Obviously, this is only good for COM
// objects for which CoCreateInstance is just a glorified
// find-and-load-me operation.
//

HRESULT FakeCoCreateInstanceEx(REFCLSID       rclsid,
                               LPCWSTR        wszDllPath,
                               REFIID         riid,
                               void **        ppv,
                               HMODULE *      phmodDll);

// Provided for backward compatibility and for code that doesn't need the HMODULE of the
// DLL that was loaded to create the COM object.  See comment at implementation of
// code:FakeCoCreateInstanceEx for more details.
inline HRESULT FakeCoCreateInstance(REFCLSID   rclsid,
                                    REFIID     riid,
                                    void **    ppv)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    return FakeCoCreateInstanceEx(rclsid, NULL, riid, ppv, NULL);
};

//*****************************************************************************
// Gets the directory based on the location of the module. This routine
// is called at COR setup time. Set is called during EEStartup and by the
// MetaData dispenser.
//*****************************************************************************
HRESULT GetInternalSystemDirectory(__out_ecount_part_opt(*pdwLength,*pdwLength) LPWSTR buffer, __inout DWORD* pdwLength);
LPCWSTR GetInternalSystemDirectory(__out_opt DWORD * pdwLength = NULL);

//*****************************************************************************
// This function validates the given Method/Field/Standalone signature. (util.cpp)
//*****************************************************************************
struct IMDInternalImport;
HRESULT validateTokenSig(
    mdToken             tk,                     // [IN] Token whose signature needs to be validated.
    PCCOR_SIGNATURE     pbSig,                  // [IN] Signature.
    ULONG               cbSig,                  // [IN] Size in bytes of the signature.
    DWORD               dwFlags,                // [IN] Method flags.
    IMDInternalImport*  pImport);               // [IN] Internal MD Import interface ptr

//*****************************************************************************
// Determine the version number of the runtime that was used to build the
// specified image. The pMetadata pointer passed in is the pointer to the
// metadata contained in the image.
//*****************************************************************************
HRESULT GetImageRuntimeVersionString(PVOID pMetaData, LPCSTR* pString);

//*****************************************************************************
// The registry keys and values that contain the information regarding
// the default registered unmanaged debugger.
//*****************************************************************************

#define kDebugApplicationsPoliciesKey W("SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Error Reporting\\DebugApplications")
#define kDebugApplicationsKey  W("SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\DebugApplications")

#define kUnmanagedDebuggerKey W("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug")
#define kUnmanagedDebuggerValue W("Debugger")
#define kUnmanagedDebuggerAutoValue W("Auto")
#define kUnmanagedDebuggerAutoExclusionListKey W("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug\\AutoExclusionList")

BOOL GetRegistryLongValue(HKEY    hKeyParent,              // Parent key.
                          LPCWSTR szKey,                   // Key name to look at.
                          LPCWSTR szName,                  // Name of value to get.
                          long    *pValue,                 // Put value here, if found.
                          BOOL    fReadNonVirtualizedKey); // Whether to read 64-bit hive on WOW64

HRESULT GetCurrentModuleFileName(SString& pBuffer);

//*****************************************************************************
// Retrieve information regarding what registered default debugger
//*****************************************************************************
void GetDebuggerSettingInfo(SString &debuggerKeyValue, BOOL *pfAuto);
HRESULT GetDebuggerSettingInfoWorker(__out_ecount_part_opt(*pcchDebuggerString, *pcchDebuggerString) LPWSTR wszDebuggerString, DWORD * pcchDebuggerString, BOOL * pfAuto);

void TrimWhiteSpace(__inout_ecount(*pcch)  LPCWSTR *pwsz, __inout LPDWORD pcch);


//*****************************************************************************
// Convert a UTF8 string to Unicode, into a CQuickArray<WCHAR>.
//*****************************************************************************
HRESULT Utf2Quick(
    LPCUTF8     pStr,                   // The string to convert.
    CQuickArray<WCHAR> &rStr,           // The QuickArray<WCHAR> to convert it into.
    int         iCurLen = 0);           // Initial characters in the array to leave (default 0).

//*****************************************************************************
//  Extract the movl 64-bit unsigned immediate from an IA64 bundle
//  (Format X2)
//*****************************************************************************
UINT64 GetIA64Imm64(UINT64 * pBundle);
UINT64 GetIA64Imm64(UINT64 qword0, UINT64 qword1);

//*****************************************************************************
//  Deposit the movl 64-bit unsigned immediate into an IA64 bundle
//  (Format X2)
//*****************************************************************************
void PutIA64Imm64(UINT64 * pBundle, UINT64 imm64);

//*****************************************************************************
//  Extract the IP-Relative signed 25-bit immediate from an IA64 bundle
//  (Formats B1, B2 or B3)
//  Note that due to branch target alignment requirements
//       the lowest four bits in the result will always be zero.
//*****************************************************************************
INT32 GetIA64Rel25(UINT64 * pBundle, UINT32 slot);
INT32 GetIA64Rel25(UINT64 qword0, UINT64 qword1, UINT32 slot);

//*****************************************************************************
//  Deposit the IP-Relative signed 25-bit immediate into an IA64 bundle
//  (Formats B1, B2 or B3)
//  Note that due to branch target alignment requirements
//       the lowest four bits are required to be zero.
//*****************************************************************************
void PutIA64Rel25(UINT64 * pBundle, UINT32 slot, INT32 imm25);

//*****************************************************************************
//  Extract the IP-Relative signed 64-bit immediate from an IA64 bundle
//  (Formats X3 or X4)
//*****************************************************************************
INT64 GetIA64Rel64(UINT64 * pBundle);
INT64 GetIA64Rel64(UINT64 qword0, UINT64 qword1);

//*****************************************************************************
//  Deposit the IP-Relative signed 64-bit immediate into a IA64 bundle
//  (Formats X3 or X4)
//*****************************************************************************
void PutIA64Rel64(UINT64 * pBundle, INT64 imm64);

//*****************************************************************************
//  Extract the 32-bit immediate from movw/movt Thumb2 sequence
//*****************************************************************************
UINT32 GetThumb2Mov32(UINT16 * p);

//*****************************************************************************
//  Deposit the 32-bit immediate into movw/movt Thumb2 sequence
//*****************************************************************************
void PutThumb2Mov32(UINT16 * p, UINT32 imm32);

//*****************************************************************************
//  Extract the 24-bit rel offset from bl instruction
//*****************************************************************************
INT32 GetThumb2BlRel24(UINT16 * p);

//*****************************************************************************
//  Extract the 24-bit rel offset from bl instruction
//*****************************************************************************
void PutThumb2BlRel24(UINT16 * p, INT32 imm24);

//*****************************************************************************
//  Extract the PC-Relative offset from a b or bl instruction
//*****************************************************************************
INT32 GetArm64Rel28(UINT32 * pCode);

//*****************************************************************************
//  Extract the PC-Relative page address from an adrp instruction
//*****************************************************************************
INT32 GetArm64Rel21(UINT32 * pCode);

//*****************************************************************************
//  Extract the page offset from an add instruction
//*****************************************************************************
INT32 GetArm64Rel12(UINT32 * pCode);

//*****************************************************************************
//  Deposit the PC-Relative offset 'imm28' into a b or bl instruction
//*****************************************************************************
void PutArm64Rel28(UINT32 * pCode, INT32 imm28);

//*****************************************************************************
//  Deposit the PC-Relative page address 'imm21' into an adrp instruction
//*****************************************************************************
void PutArm64Rel21(UINT32 * pCode, INT32 imm21);

//*****************************************************************************
//  Deposit the page offset 'imm12' into an add instruction
//*****************************************************************************
void PutArm64Rel12(UINT32 * pCode, INT32 imm12);

//*****************************************************************************
// Returns whether the offset fits into bl instruction
//*****************************************************************************
inline bool FitsInThumb2BlRel24(INT32 imm24)
{
    return ((imm24 << 7) >> 7) == imm24;
}

//*****************************************************************************
// Returns whether the offset fits into an Arm64 b or bl instruction
//*****************************************************************************
inline bool FitsInRel28(INT32 val32)
{
    return (val32 >= -0x08000000) && (val32 < 0x08000000);
}

//*****************************************************************************
// Returns whether the offset fits into an Arm64 adrp instruction
//*****************************************************************************
inline bool FitsInRel21(INT32 val32)
{
    return (val32 >= 0) && (val32 <= 0x001FFFFF);
}

//*****************************************************************************
// Returns whether the offset fits into an Arm64 add instruction
//*****************************************************************************
inline bool FitsInRel12(INT32 val32)
{
    return (val32 >= 0) && (val32 <= 0x00000FFF);
}

//*****************************************************************************
// Returns whether the offset fits into an Arm64 b or bl instruction
//*****************************************************************************
inline bool FitsInRel28(INT64 val64)
{
    return (val64 >= -0x08000000LL) && (val64 < 0x08000000LL);
}

//*****************************************************************************
// Splits a command line into argc/argv lists, using the VC7 parsing rules.
// This functions interface mimics the CommandLineToArgvW api.
// If function fails, returns NULL.
// If function suceeds, call delete [] on return pointer when done.
//*****************************************************************************
LPWSTR *SegmentCommandLine(LPCWSTR lpCmdLine, DWORD *pNumArgs);

//
// TEB access can be dangerous when using fibers because a fiber may
// run on multiple threads.  If the TEB pointer is retrieved and saved
// and then a fiber is moved to a different thread, when it accesses
// the saved TEB pointer, it will be looking at the TEB state for a
// different fiber.
//
// These accessors serve the purpose of retrieving information from the
// TEB in a manner that ensures that the current fiber will not switch
// threads while the access is occuring.
//
class ClrTeb
{
public:
#if defined(HOST_UNIX)

    // returns pointer that uniquely identifies the fiber
    static void* GetFiberPtrId()
    {
        LIMITED_METHOD_CONTRACT;
        // not fiber for HOST_UNIX - use the regular thread ID
        return (void *)(size_t)GetCurrentThreadId();
    }

    static void* InvalidFiberPtrId()
    {
        return NULL;
    }

    static void* GetStackBase()
    {
        return PAL_GetStackBase();
    }

    static void* GetStackLimit()
    {
        return PAL_GetStackLimit();
    }

#else // HOST_UNIX

    // returns pointer that uniquely identifies the fiber
    static void* GetFiberPtrId()
    {
        LIMITED_METHOD_CONTRACT;
        // stackbase is the unique fiber identifier
        return NtCurrentTeb()->NtTib.StackBase;
    }

    static void* GetStackBase()
    {
        LIMITED_METHOD_CONTRACT;
        return NtCurrentTeb()->NtTib.StackBase;
    }

    static void* GetStackLimit()
    {
        LIMITED_METHOD_CONTRACT;
        return NtCurrentTeb()->NtTib.StackLimit;
    }

    // Please don't start to use this method unless you absolutely have to.
    // The reason why this is added is for WIN64 to support LEGACY PE-style TLS
    // variables.  On X86 it is supported by the JIT compilers themselves.  On
    // WIN64 we build more logic into the JIT helper for accessing fields.
    static void* GetLegacyThreadLocalStoragePointer()
    {
        LIMITED_METHOD_CONTRACT;
        return NtCurrentTeb()->ThreadLocalStoragePointer;
    }

    static void* GetOleReservedPtr()
    {
        LIMITED_METHOD_CONTRACT;
        return NtCurrentTeb()->ReservedForOle;
    }

    static void* GetProcessEnvironmentBlock()
    {
        LIMITED_METHOD_CONTRACT;
        return NtCurrentTeb()->ProcessEnvironmentBlock;
    }


    static void* InvalidFiberPtrId()
    {
        return (void*) 1;
    }
#endif // HOST_UNIX
};

#if !defined(DACCESS_COMPILE)

extern thread_local size_t t_ThreadType;

// check if current thread is a GC thread (concurrent or server)
inline BOOL IsGCSpecialThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    return !!(t_ThreadType & ThreadType_GC);
}

// check if current thread is a Gate thread
inline BOOL IsGateSpecialThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_Gate);
}

// check if current thread is a Timer thread
inline BOOL IsTimerSpecialThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_Timer);
}

// check if current thread is a debugger helper thread
inline BOOL IsDbgHelperSpecialThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_DbgHelper);
}

// check if current thread is a debugger helper thread
inline BOOL IsETWRundownSpecialThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_ETWRundownThread);
}

// check if current thread is a generic instantiation lookup compare thread
inline BOOL IsGenericInstantiationLookupCompareThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_GenericInstantiationCompare);
}

// check if current thread is a thread which is performing shutdown
inline BOOL IsShutdownSpecialThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_Shutdown);
}

inline BOOL IsThreadPoolIOCompletionSpecialThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_Threadpool_IOCompletion);
}

inline BOOL IsThreadPoolWorkerSpecialThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_Threadpool_Worker);
}

inline BOOL IsWaitSpecialThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_Wait);
}

// check if current thread is a thread which is performing shutdown
inline BOOL IsSuspendEEThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_DynamicSuspendEE);
}

inline BOOL IsFinalizerThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_Finalizer);
}

inline BOOL IsShutdownHelperThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_ShutdownHelper);
}

inline BOOL IsProfilerAttachThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    return !!(t_ThreadType & ThreadType_ProfAPI_Attach);
}

// set special type for current thread
void ClrFlsSetThreadType(TlsThreadTypeFlag flag);
void ClrFlsClearThreadType(TlsThreadTypeFlag flag);

#endif //!DACCESS_COMPILE

HRESULT SetThreadName(HANDLE hThread, PCWSTR lpThreadDescription);

inline BOOL IsGCThread ()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_SUPPORTS_DAC;

#if !defined(DACCESS_COMPILE)
    return IsGCSpecialThread () || IsSuspendEEThread ();
#else
    return FALSE;
#endif
}

class ClrFlsThreadTypeSwitch
{
public:
    ClrFlsThreadTypeSwitch (TlsThreadTypeFlag flag)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_MODE_ANY;

#ifndef DACCESS_COMPILE
        m_flag = flag;
        m_fPreviouslySet = (t_ThreadType & flag);

        // In debug builds, remember the full group of flags that were set at the time
        // the constructor was called.  This will be used in ASSERTs in the destructor
        INDEBUG(m_nPreviousFlagGroup = t_ThreadType);

        if (!m_fPreviouslySet)
        {
            ClrFlsSetThreadType(flag);
        }
#endif // DACCESS_COMPILE
    }

    ~ClrFlsThreadTypeSwitch ()
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_MODE_ANY;

#ifndef DACCESS_COMPILE
        // This holder should only be used to set (and thus restore) ONE thread type flag
        // at a time. If more than that one flag was modified since this holder was
        // instantiated, then this holder still restores only the flag it knows about. To
        // prevent confusion, assert if some other flag was modified, so the user doesn't
        // expect the holder to restore the entire original set of flags.
        //
        // The expression below says that the only difference between the previous flag
        // group and the current flag group should be m_flag (or no difference at all, if
        // m_flag's state didn't actually change).
        _ASSERTE(((m_nPreviousFlagGroup ^ t_ThreadType) | (size_t) m_flag) == (size_t) m_flag);

        if (m_fPreviouslySet)
        {
            ClrFlsSetThreadType(m_flag);
        }
        else
        {
            ClrFlsClearThreadType(m_flag);
        }
#endif // DACCESS_COMPILE
    }

private:
    TlsThreadTypeFlag m_flag;
    BOOL m_fPreviouslySet;
    INDEBUG(size_t m_nPreviousFlagGroup);
};

//*********************************************************************************

#include "contract.inl"

namespace util
{
    //  compare adapters
    //

    template < typename T >
    struct less
    {
        bool operator()( T const & first, T const & second ) const
        {
            return first < second;
        }
    };

    template < typename T >
    struct greater
    {
        bool operator()( T const & first, T const & second ) const
        {
            return first > second;
        }
    };


    //  sort adapters
    //

    template< typename Iter, typename Pred >
    void sort( Iter begin, Iter end, Pred pred );

    template< typename T, typename Pred >
    void sort( T * begin, T * end, Pred pred )
    {
        struct sort_helper : CQuickSort< T >
        {
            sort_helper( T * begin, T * end, Pred pred )
                : CQuickSort< T >( begin, end - begin )
                , m_pred( pred )
            {}

            virtual int Compare( T * first, T * second )
            {
                return m_pred( *first, *second ) ? -1
                            : ( m_pred( *second, *first ) ? 1 : 0 );
            }

            Pred m_pred;
        };

        sort_helper sort_obj( begin, end, pred );
        sort_obj.Sort();
    }


    template < typename Iter >
    void sort( Iter begin, Iter end );

    template < typename T >
    void sort( T * begin, T * end )
    {
        util::sort( begin, end, util::less< T >() );
    }


    // binary search adapters
    //

    template < typename Iter, typename T, typename Pred >
    Iter lower_bound( Iter begin, Iter end, T const & val, Pred pred );

    template < typename T, typename Pred >
    T * lower_bound( T * begin, T * end, T const & val, Pred pred )
    {
        for (; begin != end; )
        {
            T * mid = begin + ( end - begin ) / 2;
            if ( pred( *mid, val ) )
                begin = ++mid;
            else
                end = mid;
        }

        return begin;
    }


    template < typename Iter, typename T >
    Iter lower_bound( Iter begin, Iter end, T const & val );

    template < typename T >
    T * lower_bound( T * begin, T * end, T const & val )
    {
        return util::lower_bound( begin, end, val, util::less< T >() );
    }
}


/* ------------------------------------------------------------------------ *
 * Overloaded operators for the executable heap
 * ------------------------------------------------------------------------ */

#ifdef HOST_WINDOWS

struct CExecutable { int x; };
extern const CExecutable executable;

void * __cdecl operator new(size_t n, const CExecutable&);
void * __cdecl operator new[](size_t n, const CExecutable&);
void * __cdecl operator new(size_t n, const CExecutable&, const NoThrow&);
void * __cdecl operator new[](size_t n, const CExecutable&, const NoThrow&);


//
// Executable heap delete to match the executable heap new above.
//
template<class T> void DeleteExecutable(T *p)
{
    if (p != NULL)
    {
        p->T::~T();

        HeapFree(ClrGetProcessExecutableHeap(), 0, p);
    }
}

#endif // HOST_WINDOWS

INDEBUG(BOOL DbgIsExecutable(LPVOID lpMem, SIZE_T length);)

BOOL NoGuiOnAssert();
#ifdef _DEBUG
VOID TerminateOnAssert();
#endif // _DEBUG


BOOL ThreadWillCreateGuardPage(SIZE_T sizeReservedStack, SIZE_T sizeCommitedStack);

FORCEINLINE void HolderSysFreeString(BSTR str) { CONTRACT_VIOLATION(ThrowsViolation); SysFreeString(str); }

typedef Wrapper<BSTR, DoNothing, HolderSysFreeString> BSTRHolder;

// HMODULE_TGT represents a handle to a module in the target process.  In non-DAC builds this is identical
// to HMODULE (HINSTANCE), which is the base address of the module.  In DAC builds this must be a target address,
// and so is represented by TADDR.

#ifdef DACCESS_COMPILE
typedef TADDR HMODULE_TGT;
#else
typedef HMODULE HMODULE_TGT;
#endif

BOOL IsIPInModule(HMODULE_TGT hModule, PCODE ip);

extern HINSTANCE g_hmodCoreCLR;

namespace UtilCode
{
    // These are type-safe versions of Interlocked[Compare]Exchange
    // They avoid invoking struct cast operations via reinterpreting
    // the struct's address as a LONG* or LONGLONG* and dereferencing it.
    //
    // If we had a global ::operator & (unary), we would love to use that
    // to ensure we were not also accidentally getting a structs's provided
    // operator &. TODO: probe with a static_assert?

    template <typename T, int SIZE = sizeof(T)>
    struct InterlockedCompareExchangeHelper;

    template <typename T>
    struct InterlockedCompareExchangeHelper<T, sizeof(LONG)>
    {
        static inline T InterlockedExchange(
            T volatile * target,
            T            value)
        {
            static_assert_no_msg(sizeof(T) == sizeof(LONG));
            LONG res = ::InterlockedExchange(
                reinterpret_cast<LONG volatile *>(target),
                *reinterpret_cast<LONG *>(/*::operator*/&(value)));
            return *reinterpret_cast<T*>(&res);
        }

        static inline T InterlockedCompareExchange(
            T volatile * destination,
            T            exchange,
            T            comparand)
        {
            static_assert_no_msg(sizeof(T) == sizeof(LONG));
            LONG res = ::InterlockedCompareExchange(
                reinterpret_cast<LONG volatile *>(destination),
                *reinterpret_cast<LONG*>(/*::operator*/&(exchange)),
                *reinterpret_cast<LONG*>(/*::operator*/&(comparand)));
            return *reinterpret_cast<T*>(&res);
        }
    };

    template <typename T>
    struct InterlockedCompareExchangeHelper<T, sizeof(LONGLONG)>
    {
        static inline T InterlockedExchange(
            T volatile * target,
            T            value)
        {
            static_assert_no_msg(sizeof(T) == sizeof(LONGLONG));
            LONGLONG res = ::InterlockedExchange64(
                reinterpret_cast<LONGLONG volatile *>(target),
                *reinterpret_cast<LONGLONG *>(/*::operator*/&(value)));
            return *reinterpret_cast<T*>(&res);
        }

        static inline T InterlockedCompareExchange(
            T volatile * destination,
            T            exchange,
            T            comparand)
        {
            static_assert_no_msg(sizeof(T) == sizeof(LONGLONG));
            LONGLONG res = ::InterlockedCompareExchange64(
                reinterpret_cast<LONGLONG volatile *>(destination),
                *reinterpret_cast<LONGLONG*>(/*::operator*/&(exchange)),
                *reinterpret_cast<LONGLONG*>(/*::operator*/&(comparand)));
            return *reinterpret_cast<T*>(&res);
        }
    };
}

template <typename T>
inline T InterlockedExchangeT(
    T volatile * target,
    T            value)
{
    return ::UtilCode::InterlockedCompareExchangeHelper<T>::InterlockedExchange(
        target, value);
}

template <typename T>
inline T InterlockedCompareExchangeT(
    T volatile * destination,
    T            exchange,
    T            comparand)
{
    return ::UtilCode::InterlockedCompareExchangeHelper<T>::InterlockedCompareExchange(
        destination, exchange, comparand);
}

// Pointer variants for Interlocked[Compare]ExchangePointer
// If the underlying type is a const type, we have to remove its constness
// since Interlocked[Compare]ExchangePointer doesn't take const void * arguments.
template <typename T>
inline T* InterlockedExchangeT(
    T* volatile * target,
    T*            value)
{
    //STATIC_ASSERT(value == 0);
    typedef typename std::remove_const<T>::type * non_const_ptr_t;
    return reinterpret_cast<T*>(InterlockedExchangePointer(
        reinterpret_cast<PVOID volatile *>(const_cast<non_const_ptr_t volatile *>(target)),
        reinterpret_cast<PVOID>(const_cast<non_const_ptr_t>(value))));
}

template <typename T>
inline T* InterlockedCompareExchangeT(
    T* volatile * destination,
    T*            exchange,
    T*            comparand)
{
    //STATIC_ASSERT(exchange == 0);
    typedef typename std::remove_const<T>::type * non_const_ptr_t;
    return reinterpret_cast<T*>(InterlockedCompareExchangePointer(
        reinterpret_cast<PVOID volatile *>(const_cast<non_const_ptr_t volatile *>(destination)),
        reinterpret_cast<PVOID>(const_cast<non_const_ptr_t>(exchange)),
        reinterpret_cast<PVOID>(const_cast<non_const_ptr_t>(comparand))));
}

// NULL pointer variants of the above to avoid having to cast NULL
// to the appropriate pointer type.
template <typename T>
inline T* InterlockedExchangeT(
    T* volatile *  target,
    std::nullptr_t value) // When nullptr is provided as argument.
{
    //STATIC_ASSERT(value == 0);
    return InterlockedExchangeT(target, static_cast<T*>(value));
}

template <typename T>
inline T* InterlockedCompareExchangeT(
    T* volatile *  destination,
    std::nullptr_t exchange,  // When nullptr is provided as argument.
    T*             comparand)
{
    //STATIC_ASSERT(exchange == 0);
    return InterlockedCompareExchangeT(destination, static_cast<T*>(exchange), comparand);
}

template <typename T>
inline T* InterlockedCompareExchangeT(
    T* volatile *  destination,
    T*             exchange,
    std::nullptr_t comparand) // When nullptr is provided as argument.
{
    //STATIC_ASSERT(comparand == 0);
    return InterlockedCompareExchangeT(destination, exchange, static_cast<T*>(comparand));
}

// NULL pointer variants of the above to avoid having to cast NULL
// to the appropriate pointer type.
template <typename T>
inline T* InterlockedExchangeT(
    T* volatile *   target,
    int             value) // When NULL is provided as argument.
{
    //STATIC_ASSERT(value == 0);
    return InterlockedExchangeT(target, nullptr);
}

template <typename T>
inline T* InterlockedCompareExchangeT(
    T* volatile *   destination,
    int             exchange,  // When NULL is provided as argument.
    T*              comparand)
{
    //STATIC_ASSERT(exchange == 0);
    return InterlockedCompareExchangeT(destination, nullptr, comparand);
}

template <typename T>
inline T* InterlockedCompareExchangeT(
    T* volatile *   destination,
    T*              exchange,
    int             comparand) // When NULL is provided as argument.
{
    //STATIC_ASSERT(comparand == 0);
    return InterlockedCompareExchangeT(destination, exchange, nullptr);
}

#undef InterlockedExchangePointer
#define InterlockedExchangePointer Use_InterlockedExchangeT
#undef InterlockedCompareExchangePointer
#define InterlockedCompareExchangePointer Use_InterlockedCompareExchangeT

// Returns the directory for HMODULE. So, if HMODULE was for "C:\Dir1\Dir2\Filename.DLL",
// then this would return "C:\Dir1\Dir2\" (note the trailing backslash).
HRESULT GetHModuleDirectory(HMODULE hMod, SString& wszPath);
HRESULT CopySystemDirectory(const SString& pPathString, SString& pbuffer);

HMODULE LoadLocalizedResourceDLLForSDK(_In_z_ LPCWSTR wzResourceDllName, _In_opt_z_ LPCWSTR modulePath=NULL, bool trySelf=true);
// This is a slight variation that can be used for anything else
typedef void* (__cdecl *LocalizedFileHandler)(LPCWSTR);
void* FindLocalizedFile(_In_z_ LPCWSTR wzResourceDllName, LocalizedFileHandler lfh, _In_opt_z_ LPCWSTR modulePath=NULL);


namespace Clr { namespace Util
{

#ifdef FEATURE_COMINTEROP
namespace Com
{
    HRESULT FindInprocServer32UsingCLSID(REFCLSID rclsid, SString & ssInprocServer32Name);
}
#endif // FEATURE_COMINTEROP

}}

inline DWORD GetLoadWithAlteredSearchPathFlag()
{
    LIMITED_METHOD_CONTRACT;
    #ifdef LOAD_WITH_ALTERED_SEARCH_PATH
        return LOAD_WITH_ALTERED_SEARCH_PATH;
    #else
        return 0;
    #endif
}

// clr::SafeAddRef and clr::SafeRelease helpers.
namespace clr
{
    //=================================================================================================================
    template <typename ItfT>
    static inline
    typename std::enable_if< std::is_pointer<ItfT>::value, ItfT >::type
    SafeAddRef(ItfT pItf)
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        if (pItf != nullptr)
        {
            pItf->AddRef();
        }
        return pItf;
    }

    //=================================================================================================================
    template <typename ItfT>
    typename std::enable_if< std::is_pointer<ItfT>::value && std::is_reference<ItfT>::value, ULONG >::type
    SafeRelease(ItfT pItf)
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        ULONG res = 0;
        if (pItf != nullptr)
        {
            res = pItf->Release();
            pItf = nullptr;
        }
        return res;
    }

    //=================================================================================================================
    template <typename ItfT>
    typename std::enable_if< std::is_pointer<ItfT>::value && !std::is_reference<ItfT>::value, ULONG >::type
    SafeRelease(ItfT pItf)
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        ULONG res = 0;
        if (pItf != nullptr)
        {
            res = pItf->Release();
        }
        return res;
    }
}

// clr::SafeDelete
namespace clr
{
    //=================================================================================================================
    template <typename PtrT>
    static inline
    typename std::enable_if< std::is_pointer<PtrT>::value, PtrT >::type
    SafeDelete(PtrT & ptr)
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        if (ptr != nullptr)
        {
            delete ptr;
            ptr = nullptr;
        }
    }
}

// ======================================================================================
// Spinning support (used by VM and by MetaData via file:..\Utilcode\UTSem.cpp)

struct SpinConstants
{
    DWORD dwInitialDuration;
    DWORD dwMaximumDuration;
    DWORD dwBackoffFactor;
    DWORD dwRepetitions;
    DWORD dwMonitorSpinCount;
};

extern SpinConstants g_SpinConstants;

#endif // __UtilCode_h__
