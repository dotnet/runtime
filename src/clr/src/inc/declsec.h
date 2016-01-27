// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * COM+99 Declarative Security Header
 *
 * HISTORY: Created, 4/15/98  
 */

#ifndef _DECLSEC_H
#define _DECLSEC_H
//
// PSECURITY_PROPS and PSECURITY_VALUES are opaque types (void*s) defined in cor.h
// so that cor.h does not need to know about these structures.  This file relates
// the opaque types in cor.h to concrete types, which are also defined here. 
//
// a PSECURITY_PROPS is a pSecurityProperties
// a PSECURITY_VALUE is a pSecurityValue
//

#include "cor.h"

// First, some flag values

#define  DECLSEC_DEMANDS                    0x00000001
#define  DECLSEC_ASSERTIONS                 0x00000002
#define  DECLSEC_DENIALS                    0x00000004
#define  DECLSEC_INHERIT_CHECKS             0x00000008
#define  DECLSEC_LINK_CHECKS                0x00000010
#define  DECLSEC_PERMITONLY                 0x00000020
#define  DECLSEC_REQUESTS                   0x00000040
#define  DECLSEC_UNMNGD_ACCESS_DEMAND       0x00000080	// Used by PInvoke/Interop
#define  DECLSEC_NONCAS_DEMANDS             0x00000100
#define  DECLSEC_NONCAS_LINK_DEMANDS        0x00000200
#define  DECLSEC_NONCAS_INHERITANCE         0x00000400
#define  DECLSEC_LINK_CHECKS_HPONLY         0x00000800  // If the DECLSEC_LINK_CHECKS flag is set due to HPA (and not due to any CAS linkdemand), this flag is set

#define  DECLSEC_NULL_OFFSET        16

#define  DECLSEC_NULL_INHERIT_CHECKS        (DECLSEC_INHERIT_CHECKS         << DECLSEC_NULL_OFFSET)
#define  DECLSEC_NULL_LINK_CHECKS           (DECLSEC_LINK_CHECKS            << DECLSEC_NULL_OFFSET)

#define  DECLSEC_RUNTIME_ACTIONS        (DECLSEC_DEMANDS        | \
                                         DECLSEC_NONCAS_DEMANDS | \
                                         DECLSEC_ASSERTIONS     | \
                                         DECLSEC_DENIALS        | \
                                         DECLSEC_PERMITONLY     | \
                                         DECLSEC_UNMNGD_ACCESS_DEMAND)

#define  DECLSEC_FRAME_ACTIONS          (DECLSEC_ASSERTIONS | \
                                         DECLSEC_DENIALS    | \
                                         DECLSEC_PERMITONLY)

#define  DECLSEC_OVERRIDES              (DECLSEC_DENIALS    | \
                                         DECLSEC_PERMITONLY)   

#define  DECLSEC_NON_RUNTIME_ACTIONS    (DECLSEC_REQUESTS               | \
                                         DECLSEC_INHERIT_CHECKS         | \
                                         DECLSEC_LINK_CHECKS            | \
                                         DECLSEC_NONCAS_LINK_DEMANDS    | \
                                         DECLSEC_NONCAS_INHERITANCE)

#define  BIT_TST(I,B)  ((I) &    (B))
#define  BIT_SET(I,B)  ((I) |=   (B))
#define  BIT_CLR(I,B)  ((I) &= (~(B)))

class LoaderHeap;

class SecurityProperties
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif
private:
    DWORD   dwFlags    ;
//    PermList    plDemands ;
    
public:
    void *operator new(size_t size, LoaderHeap *pHeap);
    void operator delete(void *pMem);

    SecurityProperties ()   
    {
        LIMITED_METHOD_CONTRACT;
        dwFlags = 0 ;
    }
    SecurityProperties(DWORD _dwFlags)
    {
        LIMITED_METHOD_CONTRACT;
        dwFlags = _dwFlags;
    }
    ~SecurityProperties ()  
    {
        LIMITED_METHOD_CONTRACT;
        dwFlags = 0 ;
    }
    inline BOOL FDemandsOnly()
    {
        LIMITED_METHOD_CONTRACT;
        return ( (dwFlags & ~(DECLSEC_DEMANDS|DECLSEC_UNMNGD_ACCESS_DEMAND)) == 0);
    }
    inline BOOL FDeclarationsExist() 
    {
        LIMITED_METHOD_CONTRACT;
        return dwFlags;
    }
    inline BOOL FDemandsExist() 
    {
        LIMITED_METHOD_CONTRACT;
        return BIT_TST(dwFlags, DECLSEC_DEMANDS);
    }
    inline void SetDemandsExist()
    {
        LIMITED_METHOD_CONTRACT;
        BIT_SET(dwFlags, DECLSEC_DEMANDS);
    }
    inline void ResetDemandsExist() 
    {  
        LIMITED_METHOD_CONTRACT;
        BIT_CLR(dwFlags, DECLSEC_DEMANDS);
    }

    inline BOOL FAssertionsExist()
    {
        LIMITED_METHOD_CONTRACT;
        return BIT_TST(dwFlags, DECLSEC_ASSERTIONS);
    }
    inline void SetAssertionsExist() 
    {
        LIMITED_METHOD_CONTRACT;
        BIT_SET(dwFlags, DECLSEC_ASSERTIONS);
    }
    inline void ResetAssertionsExist()
    {
        LIMITED_METHOD_CONTRACT;
        BIT_CLR(dwFlags, DECLSEC_ASSERTIONS);
    }

    inline BOOL FDenialsExist()
    {
        LIMITED_METHOD_CONTRACT;
        return BIT_TST(dwFlags, DECLSEC_DENIALS);
    }
    inline void SetDenialsExist()
    {
        LIMITED_METHOD_CONTRACT;
        BIT_SET(dwFlags, DECLSEC_DENIALS);
    }
    inline void ResetDenialsExist()
    {
        LIMITED_METHOD_CONTRACT;
        BIT_CLR(dwFlags, DECLSEC_DENIALS);
    }

    inline BOOL FInherit_ChecksExist()
    {
        LIMITED_METHOD_CONTRACT;
        return BIT_TST(dwFlags, DECLSEC_INHERIT_CHECKS);
    }
    inline void SetInherit_ChecksExist()
    {
        LIMITED_METHOD_CONTRACT;
        BIT_SET(dwFlags, DECLSEC_INHERIT_CHECKS);
    }
    inline void ResetInherit_ChecksExist()
    {
        LIMITED_METHOD_CONTRACT;
        BIT_CLR(dwFlags, DECLSEC_INHERIT_CHECKS);
    }

    // The class requires an inheritance check only if there are inherit checks and
    // they aren't null.
    inline BOOL RequiresCasInheritanceCheck () {LIMITED_METHOD_CONTRACT; return (dwFlags & (DECLSEC_INHERIT_CHECKS | DECLSEC_NULL_INHERIT_CHECKS))
                                                    == DECLSEC_INHERIT_CHECKS ;}

    inline BOOL RequiresNonCasInheritanceCheck () {LIMITED_METHOD_CONTRACT; return dwFlags & DECLSEC_NONCAS_INHERITANCE;}


    inline BOOL RequiresInheritanceCheck () {WRAPPER_NO_CONTRACT; return (RequiresCasInheritanceCheck() ||
                                                     RequiresNonCasInheritanceCheck()) ;}

    inline BOOL FLink_ChecksExist()
    {
        LIMITED_METHOD_CONTRACT;
        return BIT_TST(dwFlags, DECLSEC_LINK_CHECKS);
    }
    inline void SetLink_ChecksExist()
    {
        LIMITED_METHOD_CONTRACT;
        BIT_SET(dwFlags, DECLSEC_LINK_CHECKS);
    }
    inline void ResetLink_ChecksExist()
    {
        LIMITED_METHOD_CONTRACT;
        BIT_CLR(dwFlags, DECLSEC_LINK_CHECKS);
    }

    inline BOOL RequiresCasLinktimeCheck () {LIMITED_METHOD_CONTRACT; return (dwFlags & (DECLSEC_LINK_CHECKS | DECLSEC_NULL_LINK_CHECKS))
                                                 == DECLSEC_LINK_CHECKS ;}

    inline BOOL RequiresNonCasLinktimeCheck () {LIMITED_METHOD_CONTRACT; return (dwFlags & DECLSEC_NONCAS_LINK_DEMANDS);}


    inline BOOL RequiresLinktimeCheck    () {WRAPPER_NO_CONTRACT; return RequiresCasLinktimeCheck() ||
                                                    RequiresNonCasLinktimeCheck();}
    inline BOOL RequiresLinkTimeCheckHostProtectionOnly () {LIMITED_METHOD_CONTRACT; return (dwFlags & DECLSEC_LINK_CHECKS_HPONLY);}

    inline BOOL FPermitOnlyExist()
    {
        LIMITED_METHOD_CONTRACT;
        return BIT_TST(dwFlags, DECLSEC_PERMITONLY);
    }
    inline void SetPermitOnlyExist()
    {
        LIMITED_METHOD_CONTRACT;
        BIT_SET(dwFlags, DECLSEC_PERMITONLY);
    }
    inline void ResetPermitOnlyExist()
    {
        LIMITED_METHOD_CONTRACT;
        BIT_CLR(dwFlags, DECLSEC_PERMITONLY);
    }

    inline void SetFlags(DWORD dw)
    { 
        LIMITED_METHOD_CONTRACT;
        dwFlags = dw;
    }

    inline void SetFlags(DWORD dw, DWORD dwNull)
    {
        LIMITED_METHOD_CONTRACT;

        dwFlags = (dw | (dwNull << DECLSEC_NULL_OFFSET));
    }

    inline DWORD GetRuntimeActions()              
    { 
        LIMITED_METHOD_CONTRACT;

        return dwFlags & DECLSEC_RUNTIME_ACTIONS;
    }

    inline DWORD GetNullRuntimeActions()        
    {
        LIMITED_METHOD_CONTRACT;

        return (dwFlags >> DECLSEC_NULL_OFFSET) & DECLSEC_RUNTIME_ACTIONS;
    }
} ;

typedef SecurityProperties * PSecurityProperties, ** PpSecurityProperties ;

#endif
