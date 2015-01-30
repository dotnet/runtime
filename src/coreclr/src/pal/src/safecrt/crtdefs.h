//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*crtdefs.h - definitions/declarations common to all CRT
*

*
*Purpose:
*       This file has mostly defines used by the entire CRT.
*
*       [Public]
*
****/

/* Lack of pragma once is deliberate */

/* Define _CRTIMP */
#ifndef _CRTIMP
#ifdef CRTDLL
#define _CRTIMP __declspec(dllexport)
#else  /* CRTDLL */
#ifdef _DLL
#define _CRTIMP __declspec(dllimport)
#else  /* _DLL */
#define _CRTIMP
#endif  /* _DLL */
#endif  /* CRTDLL */
#endif  /* _CRTIMP */

#ifndef _INC_CRTDEFS
#define _INC_CRTDEFS

#ifndef _CRTBLD
/* This version of the header files is NOT for user programs.
 * It is intended for use when building the C runtimes ONLY.
 * The version intended for public use will not have this message.
 */
#error ERROR: Use of C runtime library internal header file.
#endif  /* _CRTBLD */

#ifndef _INTERNAL_IFSTRIP_

/* Turn off cpp overloads internally */
#define _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES 0
#define _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT 0
#define _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES 0

#endif  /* _INTERNAL_IFSTRIP_ */

#if defined (__midl)
/* MIDL does not want to see this stuff */
#undef _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES
#undef _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT
#undef _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES
#define _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES 0
#define _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT 0
#define _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES 0
#endif  /* defined (__midl) */

#if !defined (_WIN32)
#error ERROR: Only Win32 target supported!
#endif  /* !defined (_WIN32) */

#ifdef _CRTBLD
#ifndef _INTERNAL_IFSTRIP_
#ifndef _CRT_NOFORCE_MANIFEST
#define _CRT_FORCE_MANIFEST
#endif  /* _CRT_NOFORCE_MANIFEST */
#endif  /* _INTERNAL_IFSTRIP_ */
#endif  /* _CRTBLD */

#ifdef _CRT_NOFORCE_MANIFEST

#ifdef _CRT_FORCE_MANIFEST
        #pragma message ("_CRT_FORCE_MANIFEST and _CRT_NOFORCE_MANIFEST defined. Define just one")
        #pragma message ("_CRT_FORCE_MANIFEST ignored")
#endif  /* _CRT_FORCE_MANIFEST */

#else  /* _CRT_NOFORCE_MANIFEST */

#if !defined (_CRT_FORCE_MANIFEST) && defined (_VC_NODEFAULTLIB)
        #define _CRT_NOFORCE_MANIFEST
#endif  /* !defined (_CRT_FORCE_MANIFEST) && defined (_VC_NODEFAULTLIB) */

#endif  /* _CRT_NOFORCE_MANIFEST */

#include <sal.h>

#ifdef _DLL

#if !defined (_CRT_NOFORCE_MANIFEST)

#ifdef _DEBUG
#ifdef _CRT_MANIFEST_RETAIL
#define _CRT_MANIFEST_INCONSISTENT
#else  /* _CRT_MANIFEST_RETAIL */
#define _CRT_MANIFEST_DEBUG
#endif  /* _CRT_MANIFEST_RETAIL */
#else  /* _DEBUG */
#ifdef _CRT_MANIFEST_DEBUG
#define _CRT_MANIFEST_INCONSISTENT
#else  /* _CRT_MANIFEST_DEBUG */
#define _CRT_MANIFEST_RETAIL
#endif  /* _CRT_MANIFEST_DEBUG */
#endif  /* _DEBUG */

#ifdef _CRT_MANIFEST_INCONSISTENT
#error You have included some C++/C library header files with _DEBUG defined and some with _DEBUG not defined. This will not work correctly. Please have _DEBUG set or clear consistently.
#endif  /* _CRT_MANIFEST_INCONSISTENT */

#include <crtassem.h>

#ifdef _M_IX86

#ifdef _DEBUG
#pragma comment(linker,"/manifestdependency:\"type='win32' "            \
        "name='" __LIBRARIES_ASSEMBLY_NAME_PREFIX ".DebugCRT' "         \
        "version='" _CRT_ASSEMBLY_VERSION "' "                          \
        "processorArchitecture='x86' "                                  \
        "publicKeyToken='" _VC_ASSEMBLY_PUBLICKEYTOKEN "'\"")
#else  /* _DEBUG */
#pragma comment(linker,"/manifestdependency:\"type='win32' "            \
        "name='" __LIBRARIES_ASSEMBLY_NAME_PREFIX ".CRT' "              \
        "version='" _CRT_ASSEMBLY_VERSION "' "                          \
        "processorArchitecture='x86' "                                  \
        "publicKeyToken='" _VC_ASSEMBLY_PUBLICKEYTOKEN "'\"")
#endif  /* _DEBUG */

#endif  /* _M_IX86 */

#ifdef _M_AMD64

#ifdef _DEBUG
#pragma comment(linker,"/manifestdependency:\"type='win32' "            \
        "name='" __LIBRARIES_ASSEMBLY_NAME_PREFIX ".DebugCRT' "         \
        "version='" _CRT_ASSEMBLY_VERSION "' "                          \
        "processorArchitecture='amd64' "                                \
        "publicKeyToken='" _VC_ASSEMBLY_PUBLICKEYTOKEN "'\"")
#else  /* _DEBUG */
#pragma comment(linker,"/manifestdependency:\"type='win32' "            \
        "name='" __LIBRARIES_ASSEMBLY_NAME_PREFIX ".CRT' "              \
        "version='" _CRT_ASSEMBLY_VERSION "' "                          \
        "processorArchitecture='amd64' "                                \
        "publicKeyToken='" _VC_ASSEMBLY_PUBLICKEYTOKEN "'\"")
#endif  /* _DEBUG */

#endif  /* _M_AMD64 */

#ifdef _M_IA64

#ifdef _DEBUG
#pragma comment(linker,"/manifestdependency:\"type='win32' "            \
        "name='" __LIBRARIES_ASSEMBLY_NAME_PREFIX ".DebugCRT' "         \
        "version='" _CRT_ASSEMBLY_VERSION "' "                          \
        "processorArchitecture='ia64' "                                 \
        "publicKeyToken='" _VC_ASSEMBLY_PUBLICKEYTOKEN "'\"")
#else  /* _DEBUG */
#pragma comment(linker,"/manifestdependency:\"type='win32' "            \
        "name='" __LIBRARIES_ASSEMBLY_NAME_PREFIX ".CRT' "              \
        "version='" _CRT_ASSEMBLY_VERSION "' "                          \
        "processorArchitecture='ia64' "                                 \
        "publicKeyToken='" _VC_ASSEMBLY_PUBLICKEYTOKEN "'\"")
#endif  /* _DEBUG */

#endif  /* _M_IA64 */

#endif  /* !defined (_CRT_NOFORCE_MANIFEST) */

#endif  /* _DLL */

#ifdef _MSC_VER
#undef _CRT_PACKING
#define _CRT_PACKING 8

#pragma pack(push,_CRT_PACKING)
#endif  /* _MSC_VER */

#include <vadefs.h>

#ifdef __cplusplus
extern "C" {
#endif  /* __cplusplus */

/* preprocessor string helpers */
#ifndef _CRT_STRINGIZE
#define __CRT_STRINGIZE(_Value) #_Value
#define _CRT_STRINGIZE(_Value) __CRT_STRINGIZE(_Value)
#endif  /* _CRT_STRINGIZE */

#ifndef _CRT_WIDE
#define __CRT_WIDE(_String) L ## _String
#define _CRT_WIDE(_String) __CRT_WIDE(_String)
#endif  /* _CRT_WIDE */


#if !defined (_W64)
#if !defined(__midl) && (defined(_X86_) || defined(_M_IX86)) && _MSC_VER >= 1300
#define _W64 __w64
#else  /* !defined(__midl) && (defined(_X86_) || defined(_M_IX86)) && _MSC_VER >= 1300 */
#define _W64
#endif  /* !defined(__midl) && (defined(_X86_) || defined(_M_IX86)) && _MSC_VER >= 1300 */
#endif  /* !defined (_W64) */

#ifndef _INTERNAL_IFSTRIP_
/* Define _CRTIMP1 */

#ifndef _CRTIMP1
#ifdef CRTDLL1
#define _CRTIMP1 __declspec(dllexport)
#else  /* CRTDLL1 */
#define _CRTIMP1 _CRTIMP
#endif  /* CRTDLL1 */
#endif  /* _CRTIMP1 */
#endif  /* _INTERNAL_IFSTRIP_ */

/* Define _CRTIMP_NOIA64 */
#ifndef _CRTIMP_NOIA64
#if defined (_M_IA64)
#define _CRTIMP_NOIA64
#else  /* defined (_M_IA64) */
#define _CRTIMP_NOIA64 _CRTIMP
#endif  /* defined (_M_IA64) */
#endif  /* _CRTIMP_NOIA64 */

/* Define _CRTIMP2 */

#ifndef _CRTIMP2
#if defined (CRTDLL2)
#define _CRTIMP2 __declspec(dllexport)
#else  /* defined (CRTDLL2) */
#if defined (_DLL) && !defined (_STATIC_CPPLIB)
#define _CRTIMP2 __declspec(dllimport)
#else  /* defined (_DLL) && !defined (_STATIC_CPPLIB) */
#define _CRTIMP2
#endif  /* defined (_DLL) && !defined (_STATIC_CPPLIB) */
#endif  /* defined (CRTDLL2) */
#endif  /* _CRTIMP2 */

/* Define _CRTIMP_ALTERNATIVE */

#ifndef _INTERNAL_IFSTRIP_
/* This _CRTIMP_ALTERNATIVE is used to mark the secure functions implemented in safecrt.lib, which are
   meant to be available downlevel. In Windows, they will be merged in msvcrt.lib (the import
   library, so Windows should change:
        #define _CRTIMP_ALTERNATIVE __declspec(dllimport)
   to:
        #define _CRTIMP_ALTERNATIVE
   since these function will never be compiled in the msvcrt.dll.
   Here the trick is done based on _CRT_ALTERNATIVE_INLINES which enforce that safecrt.h is included before
   every other standard include files.
 */
#endif  /* _INTERNAL_IFSTRIP_ */
#ifndef _CRTIMP_ALTERNATIVE
#ifdef CRTDLL
#ifdef _SAFECRT_IMPL
    #define _CRTIMP_ALTERNATIVE
#else  /* _SAFECRT_IMPL */
    #define _CRTIMP_ALTERNATIVE __declspec(dllexport)
#endif  /* _SAFECRT_IMPL */
#else  /* CRTDLL */
#ifdef _DLL
#ifdef _CRT_ALTERNATIVE_INLINES
#define _CRTIMP_ALTERNATIVE
#else  /* _CRT_ALTERNATIVE_INLINES */
#define _CRTIMP_ALTERNATIVE _CRTIMP
#define _CRT_ALTERNATIVE_IMPORTED
#endif  /* _CRT_ALTERNATIVE_INLINES */
#else  /* _DLL */
#define _CRTIMP_ALTERNATIVE
#endif  /* _DLL */
#endif  /* CRTDLL */
#endif  /* _CRTIMP_ALTERNATIVE */

/* Define _MRTIMP */

#ifndef _MRTIMP
#ifdef MRTDLL
#if !defined (_M_CEE_PURE)
    #define _MRTIMP __declspec(dllexport)
#else  /* !defined (_M_CEE_PURE) */
    #define _MRTIMP
#endif  /* !defined (_M_CEE_PURE) */
#else  /* MRTDLL */
#define _MRTIMP __declspec(dllimport)
#endif  /* MRTDLL */
#endif  /* _MRTIMP */

/* Define _MRTIMP2 */
#ifndef _MRTIMP2
#if defined (CRTDLL2)
#define _MRTIMP2        __declspec(dllexport)
#elif defined (MRTDLL)
#define _MRTIMP2 _MRTIMP
#else  /* defined (MRTDLL) */

#if defined (_DLL) && !defined (_STATIC_CPPLIB)
#define _MRTIMP2        __declspec(dllimport)

#else  /* defined (_DLL) && !defined (_STATIC_CPPLIB) */
#define _MRTIMP2
#endif  /* defined (_DLL) && !defined (_STATIC_CPPLIB) */

#endif  /* defined (MRTDLL) */
#endif  /* _MRTIMP2 */


#ifndef _MCRTIMP
#if defined (CRTDLL) || defined (MRTDLL)
#define _MCRTIMP __declspec(dllexport)
#else  /* defined (CRTDLL) || defined (MRTDLL) */
#ifdef _DLL
#define _MCRTIMP __declspec(dllimport)
#else  /* _DLL */
#define _MCRTIMP
#endif  /* _DLL */
#endif  /* defined (CRTDLL) || defined (MRTDLL) */
#endif  /* _MCRTIMP */

#ifndef __CLR_OR_THIS_CALL
#if defined (MRTDLL) || defined (_M_CEE_PURE)
#define __CLR_OR_THIS_CALL  __clrcall
#else  /* defined (MRTDLL) || defined (_M_CEE_PURE) */
#define __CLR_OR_THIS_CALL
#endif  /* defined (MRTDLL) || defined (_M_CEE_PURE) */
#endif  /* __CLR_OR_THIS_CALL */

#ifndef __CLRCALL_OR_CDECL
#if defined (MRTDLL) || defined (_M_CEE_PURE)
#define __CLRCALL_OR_CDECL __clrcall
#else  /* defined (MRTDLL) || defined (_M_CEE_PURE) */
#define __CLRCALL_OR_CDECL __cdecl
#endif  /* defined (MRTDLL) || defined (_M_CEE_PURE) */
#endif  /* __CLRCALL_OR_CDECL */

#ifndef _CRTIMP_PURE
#if defined (_M_CEE_PURE) || defined (_STATIC_CPPLIB)
  #define _CRTIMP_PURE
#elif defined (MRTDLL)
  #define _CRTIMP_PURE
#else  /* defined (MRTDLL) */
  #define _CRTIMP_PURE _CRTIMP
#endif  /* defined (MRTDLL) */
#endif  /* _CRTIMP_PURE */

#ifndef _PGLOBAL
#ifdef _M_CEE
#if defined (__cplusplus_cli)
    #define _PGLOBAL __declspec(process)
#else  /* defined (__cplusplus_cli) */
    #define _PGLOBAL
#endif  /* defined (__cplusplus_cli) */
#else  /* _M_CEE */
#define _PGLOBAL
#endif  /* _M_CEE */
#endif  /* _PGLOBAL */

#ifndef _AGLOBAL
#ifdef _M_CEE
#define _AGLOBAL __declspec(appdomain)
#else  /* _M_CEE */
#define _AGLOBAL
#endif  /* _M_CEE */
#endif  /* _AGLOBAL */

/* define a specific constant for mixed mode */
#ifdef _M_CEE
#ifndef _M_CEE_PURE
#define _M_CEE_MIXED
#endif  /* _M_CEE_PURE */
#endif  /* _M_CEE */

/* Define __STDC_SECURE_LIB__ */
#define __STDC_SECURE_LIB__ 200411L

/* Retain__GOT_SECURE_LIB__ for back-compat */
#define __GOT_SECURE_LIB__ __STDC_SECURE_LIB__

/* Default value for __STDC_WANT_SECURE_LIB__ is 1 */
#ifndef __STDC_WANT_SECURE_LIB__
#define __STDC_WANT_SECURE_LIB__ 1
#endif  /* __STDC_WANT_SECURE_LIB__ */

/* Turn off deprecation if __STDC_WANT_SECURE_LIB__ is 0 */
#if !__STDC_WANT_SECURE_LIB__ && !defined (_CRT_SECURE_NO_DEPRECATE)
#define _CRT_SECURE_NO_DEPRECATE
#endif  /* !__STDC_WANT_SECURE_LIB__ && !defined (_CRT_SECURE_NO_DEPRECATE) */

#if _MSC_FULL_VER >= 140050320
#define _CRT_DEPRECATE_TEXT(_Text) __declspec(deprecated(_Text))
#else  /* _MSC_FULL_VER >= 140050320 */
#define _CRT_DEPRECATE_TEXT(_Text) __declspec(deprecated)
#endif  /* _MSC_FULL_VER >= 140050320 */

/* Define _CRT_INSECURE_DEPRECATE */
#ifndef _CRT_INSECURE_DEPRECATE
#ifdef _CRT_SECURE_NO_DEPRECATE
#define _CRT_INSECURE_DEPRECATE(_Replacement)
#else  /* _CRT_SECURE_NO_DEPRECATE */
#define _CRT_INSECURE_DEPRECATE(_Replacement) _CRT_DEPRECATE_TEXT("This function or variable may be unsafe. Consider using " #_Replacement " instead. To disable deprecation, use _CRT_SECURE_NO_DEPRECATE. See online help for details.")
#endif  /* _CRT_SECURE_NO_DEPRECATE */
#endif  /* _CRT_INSECURE_DEPRECATE */

/* Define _CRT_INSECURE_DEPRECATE_MEMORY */
#ifndef _CRT_INSECURE_DEPRECATE_MEMORY
#if !defined (_CRT_SECURE_DEPRECATE_MEMORY)
#define _CRT_INSECURE_DEPRECATE_MEMORY(_Replacement)
#else  /* !defined (_CRT_SECURE_DEPRECATE_MEMORY) */
#define _CRT_INSECURE_DEPRECATE_MEMORY(_Replacement) _CRT_INSECURE_DEPRECATE(_Replacement)
#endif  /* !defined (_CRT_SECURE_DEPRECATE_MEMORY) */
#endif  /* _CRT_INSECURE_DEPRECATE_MEMORY */

/* Define _CRT_INSECURE_DEPRECATE_GLOBALS */
#ifndef _CRT_INSECURE_DEPRECATE_GLOBALS
#if defined (RC_INVOKED)
#define _CRT_INSECURE_DEPRECATE_GLOBALS(_Replacement)
#else  /* defined (RC_INVOKED) */
#if defined (_CRT_SECURE_NO_DEPRECATE_GLOBALS)
#define _CRT_INSECURE_DEPRECATE_GLOBALS(_Replacement)
#else  /* defined (_CRT_SECURE_NO_DEPRECATE_GLOBALS) */
#define _CRT_INSECURE_DEPRECATE_GLOBALS(_Replacement) _CRT_INSECURE_DEPRECATE(_Replacement)
#endif  /* defined (_CRT_SECURE_NO_DEPRECATE_GLOBALS) */
#endif  /* defined (RC_INVOKED) */
#endif  /* _CRT_INSECURE_DEPRECATE_GLOBALS */

/* Define _CRT_MANAGED_HEAP_DEPRECATE */
#ifndef _CRT_MANAGED_HEAP_DEPRECATE
#ifdef _CRT_MANAGED_HEAP_NO_DEPRECATE
#define _CRT_MANAGED_HEAP_DEPRECATE
#else  /* _CRT_MANAGED_HEAP_NO_DEPRECATE */
#if defined (_M_CEE)
#define _CRT_MANAGED_HEAP_DEPRECATE
/* Disabled to allow QA tests to get fixed
_CRT_DEPRECATE_TEXT("Direct heap access is not safely possible from managed code.")
*/
#else  /* defined (_M_CEE) */
#define _CRT_MANAGED_HEAP_DEPRECATE
#endif  /* defined (_M_CEE) */
#endif  /* _CRT_MANAGED_HEAP_NO_DEPRECATE */
#endif  /* _CRT_MANAGED_HEAP_DEPRECATE */

/* _SECURECRT_FILL_BUFFER_PATTERN is the same as _bNoMansLandFill */
#define _SECURECRT_FILL_BUFFER_PATTERN 0xFD

/* obsolete stuff */
#ifdef _CRTBLD
#ifndef _INTERNAL_IFSTRIP_
/* These are still used in the CRT sources only */
#define _CRT_OBSOLETE(_NewItem)
#else  /* _INTERNAL_IFSTRIP_ */

/* Define _CRT_OBSOLETE */
#ifndef _CRT_OBSOLETE
#ifdef _CRT_OBSOLETE_NO_DEPRECATE
#define _CRT_OBSOLETE(_NewItem)
#else  /* _CRT_OBSOLETE_NO_DEPRECATE */
#define _CRT_OBSOLETE(_NewItem) _CRT_DEPRECATE_TEXT("This function or variable has been superceded by newer library or operating system functionality. Consider using" #_NewItem "instead. See online help for details.")
#endif  /* _CRT_OBSOLETE_NO_DEPRECATE */
#endif  /* _CRT_OBSOLETE */

#endif  /* _INTERNAL_IFSTRIP_ */
#endif  /* _CRTBLD */

/* jit64 instrinsic stuff */
#ifndef _CRT_JIT_INTRINSIC
#if defined (_M_CEE) && (defined (_M_AMD64) || defined (_M_IA64))
/* This is only needed when managed code is calling the native APIs, targeting the 64-bit runtime */
#define _CRT_JIT_INTRINSIC __declspec(jitintrinsic)
#else  /* defined (_M_CEE) && (defined (_M_AMD64) || defined (_M_IA64)) */
#define _CRT_JIT_INTRINSIC
#endif  /* defined (_M_CEE) && (defined (_M_AMD64) || defined (_M_IA64)) */
#endif  /* _CRT_JIT_INTRINSIC */

/* Define overload switches */
#if !defined (RC_INVOKED)
#if !defined (_CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES)
  #define _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES 0
#else  /* !defined (_CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES) */
#if !__STDC_WANT_SECURE_LIB__ && _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES
   #error Cannot use Secure CRT C++ overloads when __STDC_WANT_SECURE_LIB__ is 0
#endif  /* !__STDC_WANT_SECURE_LIB__ && _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES */
#endif  /* !defined (_CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES) */
#endif  /* !defined (RC_INVOKED) */

#if !defined (RC_INVOKED)
#if !defined (_CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT)
  /* _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT is ignored if _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES is set to 0 */
  #define _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT 0
#else  /* !defined (_CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT) */
#if !__STDC_WANT_SECURE_LIB__ && _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT
   #error Cannot use Secure CRT C++ overloads when __STDC_WANT_SECURE_LIB__ is 0
#endif  /* !__STDC_WANT_SECURE_LIB__ && _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT */
#endif  /* !defined (_CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT) */
#endif  /* !defined (RC_INVOKED) */

#if !defined (RC_INVOKED)
#if !defined (_CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES)
#if __STDC_WANT_SECURE_LIB__
   #define _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES 1
#else  /* __STDC_WANT_SECURE_LIB__ */
   #define _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES 0
#endif  /* __STDC_WANT_SECURE_LIB__ */
#else  /* !defined (_CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES) */
#if !__STDC_WANT_SECURE_LIB__ && _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES
   #error Cannot use Secure CRT C++ overloads when __STDC_WANT_SECURE_LIB__ is 0
#endif  /* !__STDC_WANT_SECURE_LIB__ && _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES */
#endif  /* !defined (_CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES) */
#endif  /* !defined (RC_INVOKED) */

/* Define _CRT_NONSTDC_DEPRECATE */
#if !defined (_CRT_NONSTDC_DEPRECATE)
#if defined (_CRT_NONSTDC_NO_DEPRECATE)
#define _CRT_NONSTDC_DEPRECATE(_NewName)
#else  /* defined (_CRT_NONSTDC_NO_DEPRECATE) */
#define _CRT_NONSTDC_DEPRECATE(_NewName) _CRT_DEPRECATE_TEXT("The POSIX name for this item is deprecated. Instead, use the ISO C++ conformant name: " #_NewName ". See online help for details.")
#endif  /* defined (_CRT_NONSTDC_NO_DEPRECATE) */
#endif  /* !defined (_CRT_NONSTDC_DEPRECATE) */

#ifndef _SIZE_T_DEFINED
#ifdef _WIN64
typedef unsigned __int64    size_t;
#else  /* _WIN64 */
typedef _W64 unsigned int   size_t;
#endif  /* _WIN64 */
#define _SIZE_T_DEFINED
#endif  /* _SIZE_T_DEFINED */

#if __STDC_WANT_SECURE_LIB__
#ifndef _RSIZE_T_DEFINED
typedef size_t rsize_t;
#define _RSIZE_T_DEFINED
#endif  /* _RSIZE_T_DEFINED */
#endif  /* __STDC_WANT_SECURE_LIB__ */

#ifndef _INTPTR_T_DEFINED
#ifdef _WIN64
typedef __int64             intptr_t;
#else  /* _WIN64 */
typedef _W64 int            intptr_t;
#endif  /* _WIN64 */
#define _INTPTR_T_DEFINED
#endif  /* _INTPTR_T_DEFINED */

#ifndef _UINTPTR_T_DEFINED
#ifdef _WIN64
typedef unsigned __int64    uintptr_t;
#else  /* _WIN64 */
typedef _W64 unsigned int   uintptr_t;
#endif  /* _WIN64 */
#define _UINTPTR_T_DEFINED
#endif  /* _UINTPTR_T_DEFINED */

#ifndef _PTRDIFF_T_DEFINED
#ifdef _WIN64
typedef __int64             ptrdiff_t;
#else  /* _WIN64 */
typedef _W64 int            ptrdiff_t;
#endif  /* _WIN64 */
#define _PTRDIFF_T_DEFINED
#endif  /* _PTRDIFF_T_DEFINED */

#ifndef _WCHAR_T_DEFINED
typedef unsigned short wchar_t;
#define _WCHAR_T_DEFINED
#endif  /* _WCHAR_T_DEFINED */

#ifndef _WCTYPE_T_DEFINED
typedef unsigned short wint_t;
typedef unsigned short wctype_t;
#define _WCTYPE_T_DEFINED
#endif  /* _WCTYPE_T_DEFINED */

#ifndef _VA_LIST_DEFINED
#ifdef _M_CEE_PURE
typedef System::ArgIterator va_list;
#else  /* _M_CEE_PURE */
typedef char *  va_list;
#endif  /* _M_CEE_PURE */
#define _VA_LIST_DEFINED
#endif  /* _VA_LIST_DEFINED */

#ifdef _USE_32BIT_TIME_T
#ifdef _WIN64
#error You cannot use 32-bit time_t (_USE_32BIT_TIME_T) with _WIN64
#undef  _USE_32BIT_TIME_T
#endif  /* _WIN64 */
#else  /* _USE_32BIT_TIME_T */
#if _INTEGRAL_MAX_BITS < 64
#define _USE_32BIT_TIME_T
#endif  /* _INTEGRAL_MAX_BITS < 64 */
#endif  /* _USE_32BIT_TIME_T */

#ifndef _ERRCODE_DEFINED
#define _ERRCODE_DEFINED
/* errcode is deprecated in favor or errno_t, which is part of the standard proposal */
#if !defined (__midl)
_CRT_DEPRECATE_TEXT("This name was supported during some Whidbey pre-releases. Instead, use the standard name errno_t." ) typedef int errcode;
#else  /* !defined (__midl) */
typedef int errcode;
#endif  /* !defined (__midl) */

typedef int errno_t;
#endif  /* _ERRCODE_DEFINED */

#ifndef _TIME32_T_DEFINED
typedef _W64 long __time32_t;   /* 32-bit time value */
#define _TIME32_T_DEFINED
#endif  /* _TIME32_T_DEFINED */

#ifndef _TIME64_T_DEFINED
#if _INTEGRAL_MAX_BITS >= 64
typedef __int64 __time64_t;     /* 64-bit time value */
#endif  /* _INTEGRAL_MAX_BITS >= 64 */
#define _TIME64_T_DEFINED
#endif  /* _TIME64_T_DEFINED */

#ifndef _TIME_T_DEFINED
#ifdef _USE_32BIT_TIME_T
typedef __time32_t time_t;      /* time value */
#else  /* _USE_32BIT_TIME_T */
typedef __time64_t time_t;      /* time value */
#endif  /* _USE_32BIT_TIME_T */
#define _TIME_T_DEFINED         /* avoid multiple def's of time_t */
#endif  /* _TIME_T_DEFINED */

#ifndef _CONST_RETURN
#ifdef __cplusplus
#define _CONST_RETURN  const
#define _CRT_CONST_CORRECT_OVERLOADS
#else  /* __cplusplus */
#define _CONST_RETURN
#endif  /* __cplusplus */
#endif  /* _CONST_RETURN */

#if !defined (UNALIGNED)
#if defined (_M_IA64) || defined (_M_AMD64)
#define UNALIGNED __unaligned
#else  /* defined (_M_IA64) || defined (_M_AMD64) */
#define UNALIGNED
#endif  /* defined (_M_IA64) || defined (_M_AMD64) */
#endif  /* !defined (UNALIGNED) */

#if !defined (_CRT_ALIGN)
#if defined (__midl)
#define _CRT_ALIGN(x)
#else  /* defined (__midl) */
#define _CRT_ALIGN(x) __declspec(align(x))
#endif  /* defined (__midl) */
#endif  /* !defined (_CRT_ALIGN) */

/* Define _CRTNOALIAS, _CRTRESTRICT */

#if _MSC_FULL_VER >= 13102050
#if !defined (_MSC_VER_GREATER_THEN_13102050)
#define _MSC_VER_GREATER_THEN_13102050
#endif  /* !defined (_MSC_VER_GREATER_THEN_13102050) */
#endif  /* _MSC_FULL_VER >= 13102050 */

#if ( defined(_M_IA64) && defined(_MSC_VER_GREATER_THEN_13102050) ) || _MSC_VER >= 1400
#ifndef _CRTNOALIAS
#define _CRTNOALIAS __declspec(noalias)
#endif  /* _CRTNOALIAS */

#ifndef _CRTRESTRICT
#define _CRTRESTRICT __declspec(restrict)
#endif  /* _CRTRESTRICT */

#else  /* ( defined(_M_IA64) && defined(_MSC_VER_GREATER_THEN_13102050) ) || _MSC_VER >= 1400 */

#ifndef _CRTNOALIAS
#define _CRTNOALIAS
#endif  /* _CRTNOALIAS */

#ifndef _CRTRESTRICT
#define _CRTRESTRICT
#endif  /* _CRTRESTRICT */

#endif  /* ( defined(_M_IA64) && defined(_MSC_VER_GREATER_THEN_13102050) ) || _MSC_VER >= 1400 */

/* Define __cdecl for non-Microsoft compilers */
#if (!defined (_MSC_VER) && !defined (__cdecl))
#define __cdecl
#endif  /* (!defined (_MSC_VER) && !defined (__cdecl)) */

#if !defined (__CRTDECL)
#if defined (_M_CEE_PURE)
#define __CRTDECL
#else  /* defined (_M_CEE_PURE) */
#define __CRTDECL   __cdecl
#endif  /* defined (_M_CEE_PURE) */
#endif  /* !defined (__CRTDECL) */

#ifndef _INTERNAL_IFSTRIP_
#define __UPDATE_LOCALE(ptd, ptloci)  if( ( (ptloci) != __ptlocinfo) &&      \
                                          !( (ptd)->_ownlocale & __globallocalestatus)) \
                                      {                                     \
                                          (ptloci) = __updatetlocinfo();    \
                                      }

#define __UPDATE_MBCP(ptd, ptmbci)  if( ( (ptmbci) != __ptmbcinfo) &&      \
                                          !( (ptd)->_ownlocale & __globallocalestatus)) \
                                      {                                     \
                                          (ptmbci) = __updatetmbcinfo();    \
                                      }

/* small function to set global read-only variables */
#ifndef _DEFINE_SET_FUNCTION
#define _DEFINE_SET_FUNCTION(_FuncName, _Type, _VarName) \
    __inline \
    void _FuncName(_Type _Value) \
    { \
        __pragma(warning(push)) \
        __pragma(warning(disable:4996)) \
        _VarName = _Value; \
        __pragma(warning(pop)) \
    }
#endif  /* _DEFINE_SET_FUNCTION */

#endif  /* _INTERNAL_IFSTRIP_ */

#define _ARGMAX 100

/* _TRUNCATE */
#if !defined (_TRUNCATE)
#define _TRUNCATE ((size_t)-1)
#endif  /* !defined (_TRUNCATE) */

/* helper macros for cpp overloads */
#if !defined (RC_INVOKED)
#if defined (__cplusplus) && _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_0(_ReturnType, _FuncName, _DstType, _Dst) \
    extern "C++" \
    { \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size]) \
    { \
        return _FuncName(_Dst, _Size); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_1(_ReturnType, _FuncName, _DstType, _Dst, _TType1, _TArg1) \
    extern "C++" \
    { \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1) \
    { \
        return _FuncName(_Dst, _Size, _TArg1); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_2(_ReturnType, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    extern "C++" \
    { \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2) \
    { \
        return _FuncName(_Dst, _Size, _TArg1, _TArg2); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_3(_ReturnType, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    extern "C++" \
    { \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        return _FuncName(_Dst, _Size, _TArg1, _TArg2, _TArg3); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_4(_ReturnType, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4, _TArg4) \
    extern "C++" \
    { \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3, _TType4 _TArg4) \
    { \
        return _FuncName(_Dst, _Size, _TArg1, _TArg2, _TArg3, _TArg4); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_1_1(_ReturnType, _FuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1) \
    extern "C++" \
    { \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _DstType (&_Dst)[_Size], _TType1 _TArg1) \
    { \
        return _FuncName(_HArg1, _Dst, _Size, _TArg1); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_1_2(_ReturnType, _FuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    extern "C++" \
    { \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _DstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2) \
    { \
        return _FuncName(_HArg1, _Dst, _Size, _TArg1, _TArg2); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_1_3(_ReturnType, _FuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    extern "C++" \
    { \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _DstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        return _FuncName(_HArg1, _Dst, _Size, _TArg1, _TArg2, _TArg3); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_2_0(_ReturnType, _FuncName, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst) \
    extern "C++" \
    { \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _HType2 _HArg2, _DstType (&_Dst)[_Size]) \
    { \
        return _FuncName(_HArg1, _HArg2, _Dst, _Size); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_1_ARGLIST(_ReturnType, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1) \
    extern "C++" \
    { \
        __pragma(warning(push)); \
        __pragma(warning(disable: 4793)); \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg1); \
        return _VFuncName(_Dst, _Size, _TArg1, _ArgList); \
    } \
        __pragma(warning(pop)); \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_2_ARGLIST(_ReturnType, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    extern "C++" \
    { \
        __pragma(warning(push)); \
        __pragma(warning(disable: 4793)); \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg2); \
        return _VFuncName(_Dst, _Size, _TArg1, _TArg2, _ArgList); \
    } \
        __pragma(warning(pop)); \
    }

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_SPLITPATH(_ReturnType, _FuncName, _DstType, _Src) \
    extern "C++" \
    { \
    template <size_t _DriveSize, size_t _DirSize, size_t _NameSize, size_t _ExtSize> \
    inline \
    _ReturnType __CRTDECL _FuncName(__in const _DstType *_Src, __out_ecount_opt(_DriveSize) _DstType (&_Drive)[_DriveSize], __out_ecount_opt(_DirSize) _DstType (&_Dir)[_DirSize], __out_ecount_opt(_NameSize) _DstType (&_Name)[_NameSize], __out_ecount_opt(_ExtSize) _DstType (&_Ext)[_ExtSize]) \
    { \
        return _FuncName(_Src, _Drive, _DriveSize, _Dir, _DirSize, _Name, _NameSize, _Ext, _ExtSize); \
    } \
    }

#else  /* defined (__cplusplus) && _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES */

#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_0(_ReturnType, _FuncName, _DstType, _Dst)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_1(_ReturnType, _FuncName, _DstType, _Dst, _TType1, _TArg1)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_2(_ReturnType, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_3(_ReturnType, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_4(_ReturnType, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4, _TArg4)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_1_1(_ReturnType, _FuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_1_2(_ReturnType, _FuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_1_3(_ReturnType, _FuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_2_0(_ReturnType, _FuncName, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_1_ARGLIST(_ReturnType, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_0_2_ARGLIST(_ReturnType, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)
#define __DEFINE_CPP_OVERLOAD_SECURE_FUNC_SPLITPATH(_ReturnType, _FuncName, _DstType, _Src)

#endif  /* defined (__cplusplus) && _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES */
#endif  /* !defined (RC_INVOKED) */

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_0(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _DstType, _Dst)

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_1(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _DstType, _Dst, _TType1, _TArg1)

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _DstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_3(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_3_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3)

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_4(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4, _TArg4) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_4_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4, _TArg4)

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_1_1(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_1_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1)

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_2_0(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_2_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst)

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_1_ARGLIST(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_1_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _VFuncName, _VFuncName##_s, _DstType, _Dst, _TType1, _TArg1)

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_ARGLIST(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _VFuncName##_s, _DstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_SIZE(_DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_SIZE_EX(_DeclSpec, _FuncName, _FuncName##_s, _DstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_3_SIZE(_DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_3_SIZE_EX(_DeclSpec, _FuncName, _FuncName##_s, _DstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3)


#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_0(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst) \
    __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _DstType, _Dst) \

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_1(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1) \
    __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _DstType, _Dst, _TType1, _TArg1)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _DstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_3(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_3_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_4(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4, _TArg4) \
    __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_4_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4, _TArg4)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_1_1(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1) \
    __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_1_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_2_0(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst) \
    __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_2_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_1_ARGLIST(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1) \
    __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_1_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _FuncName##_s, _VFuncName, _VFuncName##_s, _DstType, _Dst, _TType1, _TArg1)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_SIZE(_DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_SIZE_EX(_DeclSpec, _FuncName, _FuncName##_s, _DstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_3_SIZE(_DeclSpec, _FuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_3_SIZE_EX(_DeclSpec, _FuncName, _FuncName##_s, _DstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3)


#if !defined (RC_INVOKED)
#if defined (__cplusplus) && _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES

#define __RETURN_POLICY_SAME(_FunctionCall, _Dst) return (_FunctionCall)
#define __RETURN_POLICY_DST(_FunctionCall, _Dst) return ((_FunctionCall) == 0 ? _Dst : 0)
#define __RETURN_POLICY_VOID(_FunctionCall, _Dst) (_FunctionCall); return
#define __EMPTY_DECLSPEC

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst) \
    __inline \
    _ReturnType __CRTDECL __insecure_##_FuncName(_DstType *_Dst) \
    { \
        _DeclSpec _ReturnType __cdecl _FuncName(_DstType *); \
        return _FuncName(_Dst); \
    } \
    extern "C++" \
    { \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_T &_Dst) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst)); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(const _T &_Dst) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst)); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_DstType * &_Dst) \
    { \
        return __insecure_##_FuncName(_Dst); \
    } \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size]) \
    { \
        _ReturnPolicy(_SecureFuncName(_Dst, _Size), _Dst); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName<1>(_DstType (&_Dst)[1]) \
    { \
        _ReturnPolicy(_SecureFuncName(_Dst, 1), _Dst); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_0_CGETS(_ReturnType, _DeclSpec, _FuncName, _DstType, _Dst) \
    __inline \
    _ReturnType __CRTDECL __insecure_##_FuncName(_DstType *_Dst) \
    { \
        _DeclSpec _ReturnType __cdecl _FuncName(_DstType *); \
        return _FuncName(_Dst); \
    } \
    extern "C++" \
    { \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) \
    _ReturnType __CRTDECL _FuncName(_T &_Dst) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst)); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) \
    _ReturnType __CRTDECL _FuncName(const _T &_Dst) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst)); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) \
    _ReturnType __CRTDECL _FuncName(_DstType * &_Dst) \
    { \
        return __insecure_##_FuncName(_Dst); \
    } \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size]) \
    { \
        size_t _SizeRead = 0; \
        errno_t _Err = _FuncName##_s(_Dst + 2, (_Size - 2) < ((size_t)_Dst[0]) ? (_Size - 2) : ((size_t)_Dst[0]), &_SizeRead); \
        _Dst[1] = (_DstType)(_SizeRead); \
        return (_Err == 0 ? _Dst + 2 : 0); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) \
    _ReturnType __CRTDECL _FuncName<1>(_DstType (&_Dst)[1]) \
    { \
        return __insecure_##_FuncName((_DstType *)_Dst); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) \
    _ReturnType __CRTDECL _FuncName<2>(_DstType (&_Dst)[2]) \
    { \
        return __insecure_##_FuncName((_DstType *)_Dst); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1) \
    __inline \
    _ReturnType __CRTDECL __insecure_##_FuncName(_DstType *_Dst, _TType1 _TArg1) \
    { \
        _DeclSpec _ReturnType __cdecl _FuncName(_DstType *, _TType1); \
        return _FuncName(_Dst, _TArg1); \
    } \
    extern "C++" \
    { \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_T &_Dst, _TType1 _TArg1) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(const _T &_Dst, _TType1 _TArg1) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_DstType * &_Dst, _TType1 _TArg1) \
    { \
        return __insecure_##_FuncName(_Dst, _TArg1); \
    } \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1) \
    { \
        _ReturnPolicy(_SecureFuncName(_Dst, _Size, _TArg1), _Dst); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName<1>(_DstType (&_Dst)[1], _TType1 _TArg1) \
    { \
        _ReturnPolicy(_SecureFuncName(_Dst, 1, _TArg1), _Dst); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __inline \
    _ReturnType __CRTDECL __insecure_##_FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2) \
    { \
        _DeclSpec _ReturnType __cdecl _FuncName(_DstType *, _TType1, _TType2); \
        return _FuncName(_Dst, _TArg1, _TArg2); \
    } \
    extern "C++" \
    { \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_T &_Dst, _TType1 _TArg1, _TType2 _TArg2) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(const _T &_Dst, _TType1 _TArg1, _TType2 _TArg2) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_DstType * &_Dst, _TType1 _TArg1, _TType2 _TArg2) \
    { \
        return __insecure_##_FuncName(_Dst, _TArg1, _TArg2); \
    } \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_SecureDstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2) \
    { \
        _ReturnPolicy(_SecureFuncName(_Dst, _Size, _TArg1, _TArg2), _Dst); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName<1>(_DstType (&_Dst)[1], _TType1 _TArg1, _TType2 _TArg2) \
    { \
        _ReturnPolicy(_SecureFuncName(_Dst, 1, _TArg1, _TArg2), _Dst); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_3_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    __inline \
    _ReturnType __CRTDECL __insecure_##_FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        _DeclSpec _ReturnType __cdecl _FuncName(_DstType *, _TType1, _TType2, _TType3); \
        return _FuncName(_Dst, _TArg1, _TArg2, _TArg3); \
    } \
    extern "C++" \
    { \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_T &_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2, _TArg3); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(const _T &_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2, _TArg3); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_DstType * &_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        return __insecure_##_FuncName(_Dst, _TArg1, _TArg2, _TArg3); \
    } \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        _ReturnPolicy(_SecureFuncName(_Dst, _Size, _TArg1, _TArg2, _TArg3), _Dst); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName<1>(_DstType (&_Dst)[1], _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        _ReturnPolicy(_SecureFuncName(_Dst, 1, _TArg1, _TArg2, _TArg3), _Dst); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_4_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4, _TArg4) \
    __inline \
    _ReturnType __CRTDECL __insecure_##_FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3, _TType4 _TArg4) \
    { \
        _DeclSpec _ReturnType __cdecl _FuncName(_DstType *, _TType1, _TType2, _TType3, _TType4); \
        return _FuncName(_Dst, _TArg1, _TArg2, _TArg3, _TArg4); \
    } \
    extern "C++" \
    { \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_T &_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3, _TType4 _TArg4) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2, _TArg3, _TArg4); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(const _T &_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3, _TType4 _TArg4) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2, _TArg3, _TArg4); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_DstType * &_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3, _TType4 _TArg4) \
    { \
        return __insecure_##_FuncName(_Dst, _TArg1, _TArg2, _TArg3, _TArg4); \
    } \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3, _TType4 _TArg4) \
    { \
        _ReturnPolicy(_SecureFuncName(_Dst, _Size, _TArg1, _TArg2, _TArg3, _TArg4), _Dst); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName<1>(_DstType (&_Dst)[1], _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3, _TType4 _TArg4) \
    { \
        _ReturnPolicy(_SecureFuncName(_Dst, 1, _TArg1, _TArg2, _TArg3, _TArg4), _Dst); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_1_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1) \
    __inline \
    _ReturnType __CRTDECL __insecure_##_FuncName(_HType1 _HArg1, _DstType *_Dst, _TType1 _TArg1) \
    { \
        _DeclSpec _ReturnType __cdecl _FuncName(_HType1, _DstType *, _TType1); \
        return _FuncName(_HArg1, _Dst, _TArg1); \
    } \
    extern "C++" \
    { \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _T &_Dst, _TType1 _TArg1) \
    { \
        return __insecure_##_FuncName(_HArg1, static_cast<_DstType *>(_Dst), _TArg1); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, const _T &_Dst, _TType1 _TArg1) \
    { \
        return __insecure_##_FuncName(_HArg1, static_cast<_DstType *>(_Dst), _TArg1); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _DstType * &_Dst, _TType1 _TArg1) \
    { \
        return __insecure_##_FuncName(_HArg1, _Dst, _TArg1); \
    } \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _DstType (&_Dst)[_Size], _TType1 _TArg1) \
    { \
        _ReturnPolicy(_SecureFuncName(_HArg1, _Dst, _Size, _TArg1), _Dst); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName<1>(_HType1 _HArg1, _DstType (&_Dst)[1], _TType1 _TArg1) \
    { \
        _ReturnPolicy(_SecureFuncName(_HArg1, _Dst, 1, _TArg1), _Dst); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_2_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst) \
    __inline \
    _ReturnType __CRTDECL __insecure_##_FuncName(_HType1 _HArg1, _HType2 _HArg2, _DstType *_Dst) \
    { \
        _DeclSpec _ReturnType __cdecl _FuncName(_HType1, _HType2, _DstType *); \
        return _FuncName(_HArg1, _HArg2, _Dst); \
    } \
    extern "C++" \
    { \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _HType2 _HArg2, _T &_Dst) \
    { \
        return __insecure_##_FuncName(_HArg1, _HArg2, static_cast<_DstType *>(_Dst)); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _HType2 _HArg2, const _T &_Dst) \
    { \
        return __insecure_##_FuncName(_HArg1, _HArg2, static_cast<_DstType *>(_Dst)); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _HType2 _HArg2, _DstType * &_Dst) \
    { \
        return __insecure_##_FuncName(_HArg1, _HArg2, _Dst); \
    } \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_HType1 _HArg1, _HType2 _HArg2, _DstType (&_Dst)[_Size]) \
    { \
        _ReturnPolicy(_SecureFuncName(_HArg1, _HArg2, _Dst, _Size), _Dst); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName<1>(_HType1 _HArg1, _HType2 _HArg2, _DstType (&_Dst)[1]) \
    { \
        _ReturnPolicy(_SecureFuncName(_HArg1, _HArg2, _Dst, 1), _Dst); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_1_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _VFuncName, _SecureVFuncName, _DstType, _Dst, _TType1, _TArg1) \
    __inline \
    _ReturnType __CRTDECL __insecure_##_VFuncName(_DstType *_Dst, _TType1 _TArg1, va_list _ArgList) \
    { \
        _DeclSpec _ReturnType __cdecl _VFuncName(_DstType *, _TType1, va_list); \
        return _VFuncName(_Dst, _TArg1, _ArgList); \
    } \
    extern "C++" \
    { \
        __pragma(warning(push)); \
        __pragma(warning(disable: 4793)); \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_T &_Dst, _TType1 _TArg1, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg1); \
        return __insecure_##_VFuncName(static_cast<_DstType *>(_Dst), _TArg1, _ArgList); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(const _T &_Dst, _TType1 _TArg1, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg1); \
        return __insecure_##_VFuncName(static_cast<_DstType *>(_Dst), _TArg1, _ArgList); \
    } \
        __pragma(warning(pop)); \
        \
        __pragma(warning(push)); \
        __pragma(warning(disable: 4793)); \
        template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName(_DstType * &_Dst, _TType1 _TArg1, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg1); \
        return __insecure_##_VFuncName(_Dst, _TArg1, _ArgList); \
    } \
        __pragma(warning(pop)); \
        \
        __pragma(warning(push)); \
        __pragma(warning(disable: 4793)); \
        template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg1); \
        _ReturnPolicy(_SecureVFuncName(_Dst, _Size, _TArg1, _ArgList), _Dst); \
    } \
        __pragma(warning(pop)); \
        \
        __pragma(warning(push)); \
        __pragma(warning(disable: 4793)); \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    _ReturnType __CRTDECL _FuncName<1>(_DstType (&_Dst)[1], _TType1 _TArg1, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg1); \
        _ReturnPolicy(_SecureVFuncName(_Dst, 1, _TArg1, _ArgList), _Dst); \
    } \
        __pragma(warning(pop)); \
        \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureVFuncName) \
    _ReturnType __CRTDECL _VFuncName(_T &_Dst, _TType1 _TArg1, va_list _ArgList) \
    { \
        return __insecure_##_VFuncName(static_cast<_DstType *>(_Dst), _TArg1, _ArgList); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureVFuncName) \
    _ReturnType __CRTDECL _VFuncName(const _T &_Dst, _TType1 _TArg1, va_list _ArgList) \
    { \
        return __insecure_##_VFuncName(static_cast<_DstType *>(_Dst), _TArg1, _ArgList); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureVFuncName) \
    _ReturnType __CRTDECL _VFuncName(_DstType *&_Dst, _TType1 _TArg1, va_list _ArgList) \
    { \
        return __insecure_##_VFuncName(_Dst, _TArg1, _ArgList); \
    } \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _VFuncName(_DstType (&_Dst)[_Size], _TType1 _TArg1, va_list _ArgList) \
    { \
        _ReturnPolicy(_SecureVFuncName(_Dst, _Size, _TArg1, _ArgList), _Dst); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureVFuncName) \
    _ReturnType __CRTDECL _VFuncName<1>(_DstType (&_Dst)[1], _TType1 _TArg1, va_list _ArgList) \
    { \
        _ReturnPolicy(_SecureVFuncName(_Dst, 1, _TArg1, _ArgList), _Dst); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _SecureVFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __inline \
    _ReturnType __CRTDECL __insecure_##_VFuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, va_list _ArgList) \
    { \
        _DeclSpec _ReturnType __cdecl _VFuncName(_DstType *, _TType1, _TType2, va_list); \
        return _VFuncName(_Dst, _TArg1, _TArg2, _ArgList); \
    } \
    extern "C++" \
    { \
        __pragma(warning(push)); \
        __pragma(warning(disable: 4793)); \
    template <typename _T> \
    inline \
        _CRT_INSECURE_DEPRECATE(_FuncName##_s) \
    _ReturnType __CRTDECL _FuncName(_T &_Dst, _TType1 _TArg1, _TType2 _TArg2, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg2); \
        return __insecure_##_VFuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2, _ArgList); \
    } \
    template <typename _T> \
    inline \
        _CRT_INSECURE_DEPRECATE(_FuncName##_s) \
    _ReturnType __CRTDECL _FuncName(const _T &_Dst, _TType1 _TArg1, _TType2 _TArg2, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg2); \
        return __insecure_##_VFuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2, _ArgList); \
    } \
        __pragma(warning(pop)); \
        \
        __pragma(warning(push)); \
        __pragma(warning(disable: 4793)); \
    template <> \
    inline \
        _CRT_INSECURE_DEPRECATE(_FuncName##_s) \
    _ReturnType __CRTDECL _FuncName(_DstType * &_Dst, _TType1 _TArg1, _TType2 _TArg2, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg2); \
        return __insecure_##_VFuncName(_Dst, _TArg1, _TArg2, _ArgList); \
    } \
        __pragma(warning(pop)); \
        \
        __pragma(warning(push)); \
        __pragma(warning(disable: 4793)); \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _FuncName(_SecureDstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg2); \
        _ReturnPolicy(_SecureVFuncName(_Dst, _Size, _TArg1, _TArg2, _ArgList), _Dst); \
    } \
        __pragma(warning(pop)); \
        \
        __pragma(warning(push)); \
        __pragma(warning(disable: 4793)); \
    template <> \
    inline \
        _CRT_INSECURE_DEPRECATE(_FuncName##_s) \
    _ReturnType __CRTDECL _FuncName<1>(_DstType (&_Dst)[1], _TType1 _TArg1, _TType2 _TArg2, ...) \
    { \
        va_list _ArgList; \
        _crt_va_start(_ArgList, _TArg2); \
        _ReturnPolicy(_SecureVFuncName(_Dst, 1, _TArg1, _TArg2, _ArgList), _Dst); \
    } \
        __pragma(warning(pop)); \
        \
    template <typename _T> \
    inline \
        _CRT_INSECURE_DEPRECATE(_SecureVFuncName) \
    _ReturnType __CRTDECL _VFuncName(_T &_Dst, _TType1 _TArg1, _TType2 _TArg2, va_list _ArgList) \
    { \
        return __insecure_##_VFuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2, _ArgList); \
    } \
    template <typename _T> \
    inline \
        _CRT_INSECURE_DEPRECATE(_SecureVFuncName) \
    _ReturnType __CRTDECL _VFuncName(const _T &_Dst, _TType1 _TArg1, _TType2 _TArg2, va_list _ArgList) \
    { \
        return __insecure_##_VFuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2, _ArgList); \
    } \
    template <> \
    inline \
        _CRT_INSECURE_DEPRECATE(_SecureVFuncName) \
    _ReturnType __CRTDECL _VFuncName(_DstType *&_Dst, _TType1 _TArg1, _TType2 _TArg2, va_list _ArgList) \
    { \
        return __insecure_##_VFuncName(_Dst, _TArg1, _TArg2, _ArgList); \
    } \
    template <size_t _Size> \
    inline \
    _ReturnType __CRTDECL _VFuncName(_SecureDstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2, va_list _ArgList) \
    { \
        _ReturnPolicy(_SecureVFuncName(_Dst, _Size, _TArg1, _TArg2, _ArgList), _Dst); \
    } \
    template <> \
    inline \
        _CRT_INSECURE_DEPRECATE(_SecureVFuncName) \
    _ReturnType __CRTDECL _VFuncName<1>(_DstType (&_Dst)[1], _TType1 _TArg1, _TType2 _TArg2, va_list _ArgList) \
    { \
        _ReturnPolicy(_SecureVFuncName(_Dst, 1, _TArg1, _TArg2, _ArgList), _Dst); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __inline \
    size_t __CRTDECL __insecure_##_FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2) \
    { \
        _DeclSpec size_t __cdecl _FuncName(_DstType *, _TType1, _TType2); \
        return _FuncName(_Dst, _TArg1, _TArg2); \
    } \
    extern "C++" \
    { \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    size_t __CRTDECL _FuncName(_T &_Dst, _TType1 _TArg1, _TType2 _TArg2) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    size_t __CRTDECL _FuncName(const _T &_Dst, _TType1 _TArg1, _TType2 _TArg2) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    size_t __CRTDECL _FuncName(_DstType * &_Dst, _TType1 _TArg1, _TType2 _TArg2) \
    { \
        return __insecure_##_FuncName(_Dst, _TArg1, _TArg2); \
    } \
    template <size_t _Size> \
    inline \
    size_t __CRTDECL _FuncName(_SecureDstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2) \
    { \
        size_t _Ret = 0; \
        _SecureFuncName(&_Ret, _Dst, _Size, _TArg1, _TArg2); \
        return (_Ret > 0 ? (_Ret - 1) : _Ret); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    size_t __CRTDECL _FuncName<1>(_DstType (&_Dst)[1], _TType1 _TArg1, _TType2 _TArg2) \
    { \
        size_t _Ret = 0; \
        _SecureFuncName(&_Ret, _Dst, 1, _TArg1, _TArg2); \
        return (_Ret > 0 ? (_Ret - 1) : _Ret); \
    } \
    }

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_3_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    __inline \
    size_t __CRTDECL __insecure_##_FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        _DeclSpec size_t __cdecl _FuncName(_DstType *, _TType1, _TType2, _TType3); \
        return _FuncName(_Dst, _TArg1, _TArg2, _TArg3); \
    } \
    extern "C++" \
    { \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    size_t __CRTDECL _FuncName(_T &_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2, _TArg3); \
    } \
    template <typename _T> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    size_t __CRTDECL _FuncName(const _T &_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        return __insecure_##_FuncName(static_cast<_DstType *>(_Dst), _TArg1, _TArg2, _TArg3); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    size_t __CRTDECL _FuncName(_DstType * &_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        return __insecure_##_FuncName(_Dst, _TArg1, _TArg2, _TArg3); \
    } \
    template <size_t _Size> \
    inline \
    size_t __CRTDECL _FuncName(_SecureDstType (&_Dst)[_Size], _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        size_t _Ret = 0; \
        _SecureFuncName(&_Ret, _Dst, _Size, _TArg1, _TArg2, _TArg3); \
        return (_Ret > 0 ? (_Ret - 1) : _Ret); \
    } \
    template <> \
    inline \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) \
    size_t __CRTDECL _FuncName<1>(_DstType (&_Dst)[1], _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3) \
    { \
        size_t _Ret = 0; \
        _SecureFuncName(&_Ret, _Dst, 1, _TArg1, _TArg2, _TArg3); \
        return (_Ret > 0 ? (_Ret - 1) : _Ret); \
    } \
    }

#if !defined (RC_INVOKED) && _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_0_CGETS(_ReturnType, _DeclSpec, _FuncName, _DstType, _Dst) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_0_CGETS(_ReturnType, _DeclSpec, _FuncName, _DstType, _Dst)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_3_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_3_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_4_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_4_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_1_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_1_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_2_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_2_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_1_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _VFuncName, _SecureVFuncName, _DstType, _Dst, _TType1, _TArg1) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_1_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _VFuncName, _SecureVFuncName, _DstType, _Dst, _TType1, _TArg1)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_ARGLIST(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_ARGLIST(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _VFuncName##_s, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2)

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_3_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_3_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3)

#else  /* !defined (RC_INVOKED) && _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT */

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst) \
        _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_0_GETS(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst) \
        _CRT_INSECURE_DEPRECATE(_FuncName##_s) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_3_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_4_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4, _TArg4) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3, _TType4 _TArg4);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_1_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_HType1 _HArg1, _DstType *_Dst, _TType1 _TArg1);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_2_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_HType1 _HArg1, _HType2 _HArg2, _DstType *_Dst);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_1_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName,_VFuncName, _SecureVFuncName, _DstType, _Dst, _TType1, _TArg1) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, ...); \
    _CRT_INSECURE_DEPRECATE(_SecureVFuncName) _DeclSpec _ReturnType __cdecl _VFuncName(_DstType *_Dst, _TType1 _TArg1, va_list _Args);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_ARGLIST(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, ...); \
    _CRT_INSECURE_DEPRECATE(_VFuncName##_s) _DeclSpec _ReturnType __cdecl _VFuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, va_list _Args);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, ...); \
    _CRT_INSECURE_DEPRECATE(_VFuncName##_s) _DeclSpec _ReturnType __cdecl _VFuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, va_list _Args);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec size_t __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_3_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec size_t __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3);

#endif  /* !defined (RC_INVOKED) && _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES_COUNT */

#else  /* defined (__cplusplus) && _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES */

#define __RETURN_POLICY_SAME(_FunctionCall)
#define __RETURN_POLICY_DST(_FunctionCall)
#define __RETURN_POLICY_VOID(_FunctionCall)
#define __EMPTY_DECLSPEC

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_0_CGETS(_ReturnType, _DeclSpec, _FuncName, _DstType, _Dst) \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_3_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_4_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4, _TArg4) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3, _TType4 _TArg4);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_1_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_HType1 _HArg1, _DstType *_Dst, _TType1 _TArg1);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_2_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_HType1 _HArg1, _HType2 _HArg2, _DstType *_Dst);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_1_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _VFuncName, _SecureVFuncName, _DstType, _Dst, _TType1, _TArg1) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, ...); \
    _CRT_INSECURE_DEPRECATE(_SecureVFuncName) _DeclSpec _ReturnType __cdecl _VFuncName(_DstType *_Dst, _TType1 _TArg1, va_list _Args);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _SecureVFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, ...); \
    _CRT_INSECURE_DEPRECATE(_SecureVFuncName) _DeclSpec _ReturnType __cdecl _VFuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, va_list _Args);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_2_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec size_t __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2);

#define __DEFINE_CPP_OVERLOAD_STANDARD_FUNC_0_3_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec size_t __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_0_GETS(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _DstType, _Dst) \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_3_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_4_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3, _TType4, _TArg4) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3, _TType4 _TArg4);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_1_1_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _DstType, _Dst, _TType1, _TArg1) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_HType1 _HArg1, _DstType *_Dst, _TType1 _TArg1);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_2_0_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _HType1, _HArg1, _HType2, _HArg2, _DstType, _Dst) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_HType1 _HArg1, _HType2 _HArg2, _DstType *_Dst);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_1_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _SecureFuncName, _VFuncName, _SecureVFuncName, _DstType, _Dst, _TType1, _TArg1) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, ...); \
    _CRT_INSECURE_DEPRECATE(_SecureVFuncName) _DeclSpec _ReturnType __cdecl _VFuncName(_DstType *_Dst, _TType1 _TArg1, va_list _Args);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_ARGLIST(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, ...); \
    _CRT_INSECURE_DEPRECATE(_VFuncName##_s) _DeclSpec _ReturnType __cdecl _VFuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, va_list _Args);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_ARGLIST_EX(_ReturnType, _ReturnPolicy, _DeclSpec, _FuncName, _VFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_FuncName##_s) _DeclSpec _ReturnType __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, ...); \
    _CRT_INSECURE_DEPRECATE(_VFuncName##_s) _DeclSpec _ReturnType __cdecl _VFuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, va_list _Args);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_2_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec size_t __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2);

#define __DEFINE_CPP_OVERLOAD_STANDARD_NFUNC_0_3_SIZE_EX(_DeclSpec, _FuncName, _SecureFuncName, _SecureDstType, _DstType, _Dst, _TType1, _TArg1, _TType2, _TArg2, _TType3, _TArg3) \
    _CRT_INSECURE_DEPRECATE(_SecureFuncName) _DeclSpec size_t __cdecl _FuncName(_DstType *_Dst, _TType1 _TArg1, _TType2 _TArg2, _TType3 _TArg3);

#endif  /* defined (__cplusplus) && _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES */
#endif  /* !defined (RC_INVOKED) */

struct threadlocaleinfostruct;
struct threadmbcinfostruct;
typedef struct threadlocaleinfostruct * pthreadlocinfo;
typedef struct threadmbcinfostruct * pthreadmbcinfo;
struct __lc_time_data;

typedef struct localeinfo_struct
{
    pthreadlocinfo locinfo;
    pthreadmbcinfo mbcinfo;
} _locale_tstruct, *_locale_t;

#ifndef _TAGLC_ID_DEFINED
typedef struct tagLC_ID {
        unsigned short wLanguage;
        unsigned short wCountry;
        unsigned short wCodePage;
} LC_ID, *LPLC_ID;
#define _TAGLC_ID_DEFINED
#endif  /* _TAGLC_ID_DEFINED */

#ifndef _THREADLOCALEINFO
typedef struct threadlocaleinfostruct {
        int refcount;
        unsigned int lc_codepage;
        unsigned int lc_collate_cp;
        unsigned long lc_handle[6]; /* LCID */
        LC_ID lc_id[6];
        struct {
            char *locale;
            wchar_t *wlocale;
            int *refcount;
            int *wrefcount;
        } lc_category[6];
        int lc_clike;
        int mb_cur_max;
        int * lconv_intl_refcount;
        int * lconv_num_refcount;
        int * lconv_mon_refcount;
        struct lconv * lconv;
        int * ctype1_refcount;
        unsigned short * ctype1;
        const unsigned short * pctype;
        const unsigned char * pclmap;
        const unsigned char * pcumap;
        struct __lc_time_data * lc_time_curr;
} threadlocinfo;
#define _THREADLOCALEINFO
#endif  /* _THREADLOCALEINFO */

#ifdef __cplusplus
}
#endif  /* __cplusplus */

#if defined (_PREFAST_) && defined (_PFT_SHOULD_CHECK_RETURN)
#define __checkReturn_opt __checkReturn
#else  /* defined (_PREFAST_) && defined (_PFT_SHOULD_CHECK_RETURN) */
#define __checkReturn_opt
#endif  /* defined (_PREFAST_) && defined (_PFT_SHOULD_CHECK_RETURN) */

#if defined (_PREFAST_) && defined (_PFT_SHOULD_CHECK_RETURN_WAT)
#define __checkReturn_wat __checkReturn
#else  /* defined (_PREFAST_) && defined (_PFT_SHOULD_CHECK_RETURN_WAT) */
#define __checkReturn_wat
#endif  /* defined (_PREFAST_) && defined (_PFT_SHOULD_CHECK_RETURN_WAT) */

#if !defined (__midl) && !defined (MIDL_PASS) && defined (_PREFAST_)
#define __crt_typefix(ctype)              __declspec("SAL_typefix(" __CRT_STRINGIZE(ctype) ")")
#else  /* !defined (__midl) && !defined (MIDL_PASS) && defined (_PREFAST_) */
#define __crt_typefix(ctype)
#endif  /* !defined (__midl) && !defined (MIDL_PASS) && defined (_PREFAST_) */

#if (defined (__midl))
/* suppress tchar inlines */
#ifndef _NO_INLINING
#define _NO_INLINING
#endif  /* _NO_INLINING */
#endif  /* (defined (__midl)) */

#ifndef _CRT_UNUSED
#define _CRT_UNUSED(x) (void)x
#endif  /* _CRT_UNUSED */

#ifdef _MSC_VER
#pragma pack(pop)
#endif  /* _MSC_VER */

#endif  /* _INC_CRTDEFS */
