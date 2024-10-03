// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// DomainAssembly.h
//

// --------------------------------------------------------------------------------


#ifndef _DOMAINASSEMBLY_H_
#define _DOMAINASSEMBLY_H_

// --------------------------------------------------------------------------------
// Required headers
// --------------------------------------------------------------------------------

// --------------------------------------------------------------------------------
// Forward class declarations
// --------------------------------------------------------------------------------
class AppDomain;
class DomainAssembly;
class Assembly;
class Module;
class DynamicMethodTable;

enum FileLoadLevel
{
    // These states are tracked by FileLoadLock

    // Note: This enum must match the static array fileLoadLevelName[]
    //       which contains the printable names of the enum values

    // Note that semantics here are description is the LAST step done, not what is
    // currently being done.

    FILE_LOAD_CREATE,
    FILE_LOAD_BEGIN,
    FILE_LOAD_BEFORE_TYPE_LOAD,
    FILE_LOAD_EAGER_FIXUPS,
    FILE_LOAD_DELIVER_EVENTS,
    FILE_LOAD_VTABLE_FIXUPS,
    FILE_LOADED,                    // Loaded by not yet active
    FILE_ACTIVE                     // Fully active (constructors run & security checked)
};

enum NotificationStatus
{
	NOT_NOTIFIED=0,
	PROFILER_NOTIFIED=1,
	DEBUGGER_NEEDNOTIFICATION=2,
	DEBUGGER_NOTIFIED=4
};

// --------------------------------------------------------------------------------
// DomainAssembly represents an assembly loaded (or being loaded) into an app domain.  It
// is guaranteed to be unique per file per app domain.
// --------------------------------------------------------------------------------

class DomainAssembly final
{
public:

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

#ifndef DACCESS_COMPILE
    ~DomainAssembly();
    DomainAssembly() {LIMITED_METHOD_CONTRACT;};
#endif

    PEAssembly *GetPEAssembly()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return PTR_PEAssembly(m_pPEAssembly);
    }

    Assembly* GetAssembly()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pAssembly;
    }

    Module* GetModule()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pModule;
    }

    LPCUTF8 GetSimpleName()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetSimpleName();
    }

#ifdef LOGGING
    LPCUTF8 GetDebugName()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetDebugName();
    }
#endif

    BOOL IsCollectible()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fCollectible;
    }

    // ------------------------------------------------------------
    // Other public APIs
    // ------------------------------------------------------------

#ifndef DACCESS_COMPILE
    BOOL Equals(DomainAssembly *pAssembly) { WRAPPER_NO_CONTRACT; return GetPEAssembly()->Equals(pAssembly->GetPEAssembly()); }
    BOOL Equals(PEAssembly *pPEAssembly) { WRAPPER_NO_CONTRACT; return GetPEAssembly()->Equals(pPEAssembly); }
#endif // DACCESS_COMPILE

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    DomainAssembly* GetNextDomainAssemblyInSameALC()
    {
        return m_NextDomainAssemblyInSameALC;
    }

    void SetNextDomainAssemblyInSameALC(DomainAssembly* domainAssembly)
    {
        _ASSERTE(m_NextDomainAssemblyInSameALC == NULL);
        m_NextDomainAssemblyInSameALC = domainAssembly;
    }

    LoaderAllocator* GetLoaderAllocator()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLoaderAllocator;
    }

 private:
    // ------------------------------------------------------------
    // Loader API
    // ------------------------------------------------------------

    friend class AppDomain;
    friend class Assembly;

    DomainAssembly(PEAssembly* pPEAssembly, LoaderAllocator* pLoaderAllocator, AllocMemTracker* memTracker);

private:
    // ------------------------------------------------------------
    // Instance data
    // ------------------------------------------------------------

    PTR_Assembly                m_pAssembly;
    PTR_PEAssembly              m_pPEAssembly;
    PTR_Module                  m_pModule;

    BOOL                        m_fCollectible;
    DomainAssembly*             m_NextDomainAssemblyInSameALC;
    PTR_LoaderAllocator         m_pLoaderAllocator;
};

#endif  // _DOMAINASSEMBLY_H_
