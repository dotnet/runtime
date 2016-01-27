// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//--------------------------------------------------------------------------
// aptca.h
//
// Functions for handling allow partially trusted callers assemblies
//
// This should be the only interface for talking about the APTCA-ness of an assembly, and even then should
// be used only from very select areas of the CLR that absolutely need to know the information.  For
// instance:
// 
//   * the class loader (for code sharing and formatting exception messages)
//   * NGEN (for determining if a native image is valid)
//   * security attribute processing code (for obvious reasons)
//   
// may use this interface.  Nearly every other section of the code should simply be relying on the
// ModuleSecurityDescriptor for the assembly in question.  And no other sections of the code should be
// directly asking questions like "is this assembly conditional APTCA" ... we explicitly want to hide that
// information away behind the final assembly security attribute computation as much as possible.
// 
// In particular, no code should be making security enforcement decisions based upon conditional APTCA, and
// instead should rely on the existing transparency / legacy APTCA enforcement.  This means that once the
// security system, JIT, and class loader have finished setting up an assembly's APTCA attributes, there
// should be no further questions asked about the particular APTCA attribute applied to the assembly.
// 
// Put another way, once an assembly is loaded, the APTCA kill bit and conditional APTCA enabled / disabled
// decision for an assembly should evaporate away, and all assemblies should look as if they either have a
// full APTCA attribute (in the not-killbitted / conditional APTCA enabled case) or no APTCA attribute at
// all (killbitted or conditional APTCA disabled).
//

// 
//--------------------------------------------------------------------------


#ifndef __APTCA_H__
#define __APTCA_H__

#ifndef FEATURE_APTCA
#error FEATURE_APTCA is required for this file
#endif // FEATURE_APTCA

#include "securitymeta.h"

class ConditionalAptcaCache
{
public:
    typedef enum
    {
        kUnknown,           // No cached state
        kEnabled,           // The assembly is enabled in this domain
        kDisabled,          // The assembly is disabled in this domain
        kNotCAptca,         // The assembly is not conditionally APTCA
    }
    State;

    typedef enum
    {
        kDomainStateUnknown,    // The domain state is not yet initialized
        kAllEnabled,            // All assemblies in the domain are enabled
        kSomeEnabled,           // Some assemblies in the domain are enabled
        kAllDisabled,           // All assemblies in the domain are disabled
    }
    DomainState;

    ConditionalAptcaCache(AppDomain *pAppDomain);
    ~ConditionalAptcaCache();

    State GetCachedState(PTR_PEImage pImage);
    void SetCachedState(PTR_PEImage pImage, State state);

    DomainState GetConditionalAptcaDomainState();
    void SetCanonicalConditionalAptcaList(LPCWSTR wszCanonicalConditionalAptcaList);

    static bool ConsiderFullTrustConditionalAptcaLists();

private:
    ConditionalAptcaCache(ConditionalAptcaCache &other); // not implemented - used to prevent compiler generating a copy constructor
    ConditionalAptcaCache& operator=(const ConditionalAptcaCache &other); // not implemented - used to prevent compiler generating an assignment operator

private:
    AppDomain *m_pAppDomain;

    bool      m_canonicalListIsNull;
    SString   m_canonicalList;
    DomainState m_domainState;
};

// Determine if the AppDomain can share an assembly or if APTCA restrictions prevent sharing
bool DomainCanShareAptcaAssembly(DomainAssembly *pDomainAssembly);

// Get an exception string indicating how to enable a conditional APTCA assembly if it was disabled and
// caused an exception
SString GetConditionalAptcaAccessExceptionContext(Assembly *pTargetAssembly);

// Get an exception string indicating that 
SString GetConditionalAptcaSharingExceptionContext(Assembly *pTargetAssembly);

// Get an exception string indicating that an assembly was on the kill bit list if it caused an exception
SString GetAptcaKillBitAccessExceptionContext(Assembly *pTargetAssembly);

// Determine if a native image is OK to use from an APTCA perspective (it and its dependencies all have the
// same APTCA-ness now as at NGEN time)
bool NativeImageHasValidAptcaDependencies(PEImage *pNativeImage, DomainAssembly *pDomainAssembly);

// Process an assembly's real APTCA flags to determine if the assembly should be considered APTCA or not
TokenSecurityDescriptorFlags ProcessAssemblyAptcaFlags(DomainAssembly *pDomainAssembly, TokenSecurityDescriptorFlags tokenFlags);

#endif // __APTCA_H__
