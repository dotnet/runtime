//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//



#ifndef __hostexecutioncontext_h__
#define __hostexecutioncontext_h__

#ifdef FEATURE_CAS_POLICY

class HostExecutionContextManager
{
public:
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    static IHostSecurityContext*  m_pRestrictedHostContext;
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    static void InitializeRestrictedContext();
    static void SetHostRestrictedContext();
    	
    static FCDECL0(FC_BOOL_RET, HostPresent);	
    static FCDECL1(HRESULT, ReleaseSecurityContext, LPVOID handle);
    static FCDECL1(HRESULT, CaptureSecurityContext, SafeHandle* hTokenUNSAFE);	
    static FCDECL2(HRESULT, CloneSecurityContext, SafeHandle* hTokenUNSAFE, SafeHandle* hTokenClonedUNSAFE);	
    static FCDECL3(HRESULT, SetSecurityContext, SafeHandle* hTokenUNSAFE, CLR_BOOL fReturnPrevious, SafeHandle* hTokenPreviousUNSAFE);	    
};
#endif // #ifdef FEATURE_CAS_POLICY
#endif // __hostexecutioncontext_h__

