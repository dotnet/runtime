// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//
#pragma once
/*************************************************************************
* DEFINITIONS OF NEW TYPES 
*************************************************************************/
#if !defined(__midl)
#define __$compname_props \
        __type_has_adt_prop(compname,nullterminated) \
        __type_has_adt_prop(compname,valid_schars) \
        __type_has_adt_prop(compname,correct_len) \
        __nullterminated
#if defined(UNICODE) || defined(_UNICODE)
#define  __$TCHAR unsigned short
#else
#define  __$TCHAR char
#endif
// DEVDIV: Windows SDK Vista RTM update: Ignore C4956 warning when compiling with /clr:safe flag
#ifdef _MSC_VER
#pragma warning( disable : 4956 )
#endif // _MSC_VER
typedef __$compname_props char* ValidCompNameA;
typedef __$compname_props unsigned short* ValidCompNameW;
typedef __$compname_props const unsigned short* ConstValidCompNameW;
typedef __$compname_props  __$TCHAR* SAL_ValidCompNameT;
typedef __$compname_props const  __$TCHAR* SAL_ConstValidCompNameT;
#ifdef _MSC_VER
#pragma warning(default:4956)
#endif // _MSC_VER
#undef __$compname_props
#undef __$TCHAR
#endif

/*************************************************************************
* DEFINITIONS OF INLINE FUNCTIONS FOR CASTING TO THE NEW TYPES : USER
*************************************************************************/
#if (_MSC_VER >= 1000) && !defined(__midl) && defined(_PREFAST_)
#ifdef  __cplusplus
extern "C" {
#endif
void __inline __nothrow __SAL_ValidCompNameA(__out_has_type_adt_props(ValidCompNameA) const void *expr) { expr;}
void __inline __nothrow __SAL_ValidCompNameW(__out_has_type_adt_props(ValidCompNameW) const void *expr) { expr;}
#ifdef  __cplusplus
}
#endif
#define __assume_ValidCompNameA(expr) __SAL_ValidCompNameA(expr)
#define __assume_ValidCompNameW(expr) __SAL_ValidCompNameW(expr)
#else
#define __assume_ValidCompNameA(expr) 
#define __assume_ValidCompNameW(expr)
#endif


