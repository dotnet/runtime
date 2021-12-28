// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

inline Module* DomainAssembly::GetModule()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(CheckLoaded());

    return m_pModule;
}

inline Assembly* DomainAssembly::GetAssembly()
{
    LIMITED_METHOD_DAC_CONTRACT;
    CONSISTENCY_CHECK(CheckLoaded());

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

