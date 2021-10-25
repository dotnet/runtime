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

inline ULONG DomainAssembly::HashIdentity()
{
    WRAPPER_NO_CONTRACT;
    return GetPEAssembly()->HashIdentity();
}

inline BOOL DomainAssembly::IsCollectible()
{
    LIMITED_METHOD_CONTRACT;
    return m_fCollectible;
}

