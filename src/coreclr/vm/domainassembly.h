// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// DomainAssembly.h
//

#ifndef _DOMAINASSEMBLY_H_
#define _DOMAINASSEMBLY_H_

template<typename T> struct cdac_data;

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

    PEAssembly* GetPEAssembly();

    Assembly* GetAssembly()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pAssembly;
    }

private:
    // ------------------------------------------------------------
    // Loader API
    // ------------------------------------------------------------

    friend class AppDomain;
    friend class Assembly;

    DomainAssembly(PEAssembly* pPEAssembly, LoaderAllocator* pLoaderAllocator, AllocMemTracker* memTracker);

    PTR_Assembly                m_pAssembly;

    friend struct ::cdac_data<DomainAssembly>;
};

template<>
struct cdac_data<DomainAssembly>
{
    static constexpr size_t Assembly = offsetof(DomainAssembly, m_pAssembly);
};

#endif  // _DOMAINASSEMBLY_H_
