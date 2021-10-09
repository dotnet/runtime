// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

inline Module* DomainFile::GetCurrentModule()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return m_pModule;
}

inline Module* DomainFile::GetLoadedModule()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(CheckLoaded());

    return m_pModule;
}

inline Module* DomainFile::GetModule()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    {
        // While executing the consistency check, we will take a lock.
        // But since this is debug-only, we'll allow the lock violation so that
        // CANNOT_TAKE_LOCK callers aren't penalized
        CONTRACT_VIOLATION(TakesLockViolation);
        CONSISTENCY_CHECK(CheckLoadLevel(FILE_LOAD_ALLOCATE));
    }

    return m_pModule;
}

inline Assembly* DomainAssembly::GetCurrentAssembly()
{
    LIMITED_METHOD_CONTRACT;

    return m_pAssembly;
}

inline Assembly* DomainAssembly::GetLoadedAssembly()
{
    LIMITED_METHOD_DAC_CONTRACT;
    CONSISTENCY_CHECK(CheckLoaded());

    return m_pAssembly;
}

inline Assembly* DomainAssembly::GetAssembly()
{
    LIMITED_METHOD_CONTRACT;

    CONSISTENCY_CHECK(CheckLoadLevel(FILE_LOAD_ALLOCATE));
    return m_pAssembly;
}

#ifndef DACCESS_COMPILE
inline void DomainFile::UpdatePEFileWorker(PTR_PEFile pFile)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(CheckPointer(pFile));
    if (pFile==m_pFile)
        return;
    _ASSERTE(m_pOriginalFile==NULL);
    m_pOriginalFile=m_pFile;
    pFile->AddRef();
    m_pFile=pFile;
}

inline void DomainAssembly::UpdatePEFile(PTR_PEFile pFile)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    GetAppDomain()->UpdatePublishHostedAssembly(this, pFile);
}

#endif // DACCESS_COMPILE

inline ULONG DomainAssembly::HashIdentity()
{
    WRAPPER_NO_CONTRACT;
    return GetFile()->HashIdentity();
}

inline BOOL DomainAssembly::IsCollectible()
{
    LIMITED_METHOD_CONTRACT;
    return m_fCollectible;
}

