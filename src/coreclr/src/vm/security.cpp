//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 

// 


#include "common.h"

#include "security.h"
#include "securitydescriptor.h"
#include "securitydescriptorappdomain.h"
#include "securitydescriptorassembly.h"

IApplicationSecurityDescriptor * Security::CreateApplicationSecurityDescriptor(AppDomain * pDomain)
{
    WRAPPER_NO_CONTRACT;
    
    return static_cast<IApplicationSecurityDescriptor*>(new ApplicationSecurityDescriptor(pDomain));
}    

IAssemblySecurityDescriptor* Security::CreateAssemblySecurityDescriptor(AppDomain *pDomain, DomainAssembly *pAssembly, LoaderAllocator *pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;

    return static_cast<IAssemblySecurityDescriptor*>(new AssemblySecurityDescriptor(pDomain, pAssembly, pLoaderAllocator));
}

ISharedSecurityDescriptor* Security::CreateSharedSecurityDescriptor(Assembly* pAssembly)
{
    WRAPPER_NO_CONTRACT;

    return static_cast<ISharedSecurityDescriptor*>(new SharedSecurityDescriptor(pAssembly));
}

#ifndef FEATURE_CORECLR
IPEFileSecurityDescriptor* Security::CreatePEFileSecurityDescriptor(AppDomain* pDomain, PEFile *pPEFile)
{
    WRAPPER_NO_CONTRACT;

    return static_cast<IPEFileSecurityDescriptor*>(new PEFileSecurityDescriptor(pDomain, pPEFile));
}
#endif

//---------------------------------------------------------------------------------------
//
// Determine if security checks should be bypassed for a method because the method is
// being used by a profiler.
//
// Profilers often do things like inject unverifiable IL or P/Invoke which won't be allowed
// if they're working with a transparent method.  This hook allows those checks to be
// suppressed if we're currently profiling.
//
// Arguments:
//    pMD - Method we're checking to see if security checks may be bypassed for
//

BOOL Security::BypassSecurityChecksForProfiler(MethodDesc *pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

#if defined(PROFILING_SUPPORTED) && !defined(CROSSGEN_COMPILE)
    return CORProfilerPresent() &&
        CORProfilerBypassSecurityChecks() &&
        pMD->GetAssembly()->GetSecurityDescriptor()->IsFullyTrusted();
#else
    return FALSE;
#endif
}
