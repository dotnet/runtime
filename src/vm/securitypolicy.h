// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

//


#ifndef __SECURITYPOLICY_H__
#define __SECURITYPOLICY_H__

#include "crst.h"
#include "objecthandle.h"
#include "securityattributes.h"
#include "securitydeclarativecache.h"
#include "declsec.h"
#include "fcall.h"
#include "qcall.h"
#include "cgensys.h"

#define SPFLAGSASSERTION        0x01
#define SPFLAGSUNMANAGEDCODE    0x02
#define SPFLAGSSKIPVERIFICATION 0x04

#define CORSEC_STACKWALK_HALTED       0x00000001   // Stack walk was halted
#define CORSEC_FT_ASSERT              0x00000004   // Hit a FT-assert during the stackwalk

// Forward declarations to avoid pulling in too many headers.
class Frame;
class FramedMethodFrame;
class ClassLoader;
class Thread;
class CrawlFrame;
class SystemNative;
class NDirect;
class SystemDomain;
class AssemblySecurityDescriptor;
class SharedSecurityDescriptor;
class SecurityStackWalkData;
class DemandStackWalk;
class SecurityDescriptor;
class COMPrincipal;

#define CLR_CASOFF_MUTEX W("Global\\CLR_CASOFF_MUTEX")

// This enumeration must be kept in sync with the managed System.Security.Policy.EvidenceTypeGenerated enum
typedef enum
{
    kAssemblySupplied,              // Evidence supplied by the assembly itself
    kGac,                           // System.Security.Policy.GacInstalled
    kHash,                          // System.Security.Policy.Hash
    kPermissionRequest,             // System.Security.Policy.PermissionRequestEvidence
    kPublisher,                     // System.Security.Policy.Publisher
    kSite,                          // System.Security.Policy.Site
    kStrongName,                    // System.Security.Policy.StrongName
    kUrl,                           // System.Security.Policy.Url
    kZone                           // System.Security.Policy.Zone
}
EvidenceType;

namespace SecurityPolicy
{
    // -----------------------------------------------------------
    // FCalls
    // -----------------------------------------------------------

    BOOL QCALLTYPE IsSameType(LPCWSTR pLeft, LPCWSTR pRight);

    FCDECL1(FC_BOOL_RET, SetThreadSecurity, CLR_BOOL fThreadSecurity);

    void QCALLTYPE GetGrantedPermissions(QCall::ObjectHandleOnStack retGranted, QCall::ObjectHandleOnStack retDenied, QCall::StackCrawlMarkHandle stackmark);



    FCDECL0(FC_BOOL_RET, IsDefaultThreadSecurityInfo);
    void QCALLTYPE _GetLongPathName(LPCWSTR wszPath, QCall::StringHandleOnStack retLongPath);
    
    BOOL QCALLTYPE IsLocalDrive(LPCWSTR wszPath);

    void QCALLTYPE GetDeviceName(LPCWSTR wszDriveLetter, QCall::StringHandleOnStack retDeviceName);

    FCDECL0(VOID, IncrementOverridesCount);

    FCDECL0(VOID, DecrementOverridesCount);

    FCDECL0(VOID, IncrementAssertCount);

    FCDECL0(VOID, DecrementAssertCount);


//private:
    // -----------------------------------------------------------
    // Init methods
    // -----------------------------------------------------------

    // Calls all the security-related init methods
    //   Callers:
    //     EEStartupHelper
    void Start();

    // Calls all the security-related shutdown methods
    //   Callers:
    //     <currently unused> @TODO: shouldn't EEShutDownHelper call this?
    void Stop();



    // -----------------------------------------------------------
    // Policy
    // -----------------------------------------------------------

    // Returns TRUE if the assembly has permission to call unmanaged code
    //   Callers:
    //     CEEInfo::getNewHelper
    //     MakeStubWorker
    //     MethodDesc::DoPrestub
    BOOL CanCallUnmanagedCode(Module *pModule);

    // Throws a security exception
    //   Callers:
    //     JIT_SecurityUnmanagedCodeException
    void CreateSecurityException(__in_z const char *szDemandClass, DWORD dwFlags, OBJECTREF* pThrowable);
    DECLSPEC_NORETURN void ThrowSecurityException(__in_z const char *szDemandClass, DWORD dwFlags);

    BOOL CanSkipVerification(DomainAssembly * pAssembly);

    // Like WszGetLongPathName, but it works with nonexistant files too
    size_t GetLongPathNameHelper( const WCHAR* wszShortPath, SString& wszBuffer);

}

struct SharedPermissionObjects
{
    OBJECTHANDLE        hPermissionObject;  // Commonly used Permission Object
    BinderClassID       idClass;            // ID of class
    BinderMethodID      idConstructor;      // ID of constructor to call      
    DWORD               dwPermissionFlag;   // Flag needed by the constructors (Only a single argument is assumed)
};

/******** Shared Permission Objects related constants *******/
#define NUM_PERM_OBJECTS    (sizeof(g_rPermObjectsTemplate) / sizeof(SharedPermissionObjects))

// Constants to use with SecurityPermission
#define SECURITY_PERMISSION_ASSERTION               1      // SecurityPermission.cs
#define SECURITY_PERMISSION_UNMANAGEDCODE           2      // SecurityPermission.cs
#define SECURITY_PERMISSION_SKIPVERIFICATION        4      // SecurityPermission.cs
#define SECURITY_PERMISSION_CONTROLEVIDENCE         0x20   // SecurityPermission.cs
#define SECURITY_PERMISSION_SERIALIZATIONFORMATTER  0X80   // SecurityPermission.cs
#define SECURITY_PERMISSION_CONTROLPRINCIPAL        0x200  // SecurityPermission.cs
#define SECURITY_PERMISSION_BINDINGREDIRECTS        0X2000 // SecurityPermission.cs

// Constants to use with ReflectionPermission
#define REFLECTION_PERMISSION_TYPEINFO              1      // ReflectionPermission.cs
#define REFLECTION_PERMISSION_MEMBERACCESS          2      // ReflectionPermission.cs
#define REFLECTION_PERMISSION_RESTRICTEDMEMBERACCESS    8      // ReflectionPermission.cs

// PermissionState.Unrestricted
#define PERMISSION_STATE_UNRESTRICTED               1      // PermissionState.cs

// Array index in SharedPermissionObjects array
// Note: these should all be permissions that implement IUnrestrictedPermission.
// Any changes to these must be reflected in bcl\system\security\codeaccesssecurityengine.cs and the above table

// special flags
#define SECURITY_UNMANAGED_CODE                 0
#define SECURITY_SKIP_VER                       1
#define REFLECTION_TYPE_INFO                    2
#define SECURITY_ASSERT                         3
#define REFLECTION_MEMBER_ACCESS                4
#define SECURITY_SERIALIZATION                  5
#define REFLECTION_RESTRICTED_MEMBER_ACCESS     6
#define SECURITY_FULL_TRUST                     7
#define SECURITY_BINDING_REDIRECTS              8

// special permissions
#define UI_PERMISSION                           9
#define ENVIRONMENT_PERMISSION                  10
#define FILEDIALOG_PERMISSION                   11
#define FILEIO_PERMISSION                       12
#define REFLECTION_PERMISSION                   13
#define SECURITY_PERMISSION                     14

// additional special flags
#define SECURITY_CONTROL_EVIDENCE               16
#define SECURITY_CONTROL_PRINCIPAL              17

// Objects corresponding to the above index could be Permission or PermissionSet objects. 
// Helper macro to identify which kind it is. If you're adding to the index above, please update this also.
#define IS_SPECIAL_FLAG_PERMISSION_SET(x)       ((x) == SECURITY_FULL_TRUST)

// Class holding a grab bag of security stuff we need on a per-appdomain basis.
struct SecurityContext
{
    // Cached declarative permissions per method
    EEPtrHashTable m_pCachedMethodPermissionsHash;
    SimpleRWLock * m_prCachedMethodPermissionsLock;
    SecurityDeclarativeCache m_pSecurityDeclarativeCache;
    size_t                      m_nCachedPsetsSize;

    SecurityContext(LoaderHeap* pHeap) :
        m_prCachedMethodPermissionsLock(NULL),
        m_nCachedPsetsSize(0)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        } CONTRACTL_END;
        
        // initialize cache of method-level declarative security permissions
        // Note that the method-level permissions are stored elsewhere
        m_prCachedMethodPermissionsLock = new SimpleRWLock(PREEMPTIVE, LOCK_TYPE_DEFAULT);
        if (!m_pCachedMethodPermissionsHash.Init(100, &g_lockTrustMeIAmThreadSafe))
            ThrowOutOfMemory();

        m_pSecurityDeclarativeCache.Init (pHeap);
    }

    ~SecurityContext()
    {
        CONTRACTL {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        } CONTRACTL_END;
        
        // no need to explicitly delete the cache contents, since they will be deallocated with the AppDomain's heap
        if (m_prCachedMethodPermissionsLock) delete m_prCachedMethodPermissionsLock;        
    }
};

#ifdef _DEBUG

#define DBG_TRACE_METHOD(cf)                                                \
    do {                                                                    \
        MethodDesc * __pFunc = cf -> GetFunction();                         \
        if (__pFunc) {                                                      \
            LOG((LF_SECURITY, LL_INFO1000,                                  \
                 "    Method: %s.%s\n",                                     \
                 (__pFunc->m_pszDebugClassName == NULL) ?                   \
                "<null>" : __pFunc->m_pszDebugClassName,                    \
                 __pFunc->GetName()));                                      \
        }                                                                   \
    } while (false)

#define DBG_TRACE_STACKWALK(msg, verbose) LOG((LF_SECURITY, (verbose) ? LL_INFO10000 : LL_INFO1000, msg))
#else //_DEBUG

#define DBG_TRACE_METHOD(cf)
#define DBG_TRACE_STACKWALK(msg, verbose)

#endif //_DEBUG


#endif // __SECURITYPOLICY_H__
