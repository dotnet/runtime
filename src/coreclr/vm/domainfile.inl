// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

inline Module* DomainFile::GetModule()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(CheckLoaded());

    return m_pModule;
}

inline Assembly* DomainFile::GetAssembly()
{
    LIMITED_METHOD_DAC_CONTRACT;
    CONSISTENCY_CHECK(CheckLoaded());

    return m_pAssembly;
}

inline ULONG DomainFile::HashIdentity()
{
    WRAPPER_NO_CONTRACT;
    return GetPEAssembly()->HashIdentity();
}

inline BOOL DomainFile::IsCollectible()
{
    LIMITED_METHOD_CONTRACT;
    return m_fCollectible;
}

