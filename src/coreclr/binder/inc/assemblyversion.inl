// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyVersion.inl
//


//
// Implements the inline methods of AssemblyVersion
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_VERSION_INL__
#define __BINDER__ASSEMBLY_VERSION_INL__

AssemblyVersion::AssemblyVersion()
{
    m_dwMajor = m_dwMinor = m_dwBuild = m_dwRevision = static_cast<DWORD>(-1);
}

AssemblyVersion::~AssemblyVersion()
{
    // Noting to do here
}

BOOL AssemblyVersion::HasMajor()
{
    return m_dwMajor != Unspecified;
}

BOOL AssemblyVersion::HasMinor()
{
    return m_dwMinor != Unspecified;
}

BOOL AssemblyVersion::HasBuild()
{
    return m_dwBuild != Unspecified;
}

BOOL AssemblyVersion::HasRevision()
{
    return m_dwRevision != Unspecified;
}

DWORD AssemblyVersion::GetMajor()
{
    return m_dwMajor;
}

DWORD AssemblyVersion::GetMinor()
{
    return m_dwMinor;
}

DWORD AssemblyVersion::GetBuild()
{
    return m_dwBuild;
}

DWORD AssemblyVersion::GetRevision()
{
    return m_dwRevision;
}

void AssemblyVersion::SetFeatureVersion(DWORD dwMajor,
                                        DWORD dwMinor)
{
    // BaseAssemblySpec and AssemblyName properties store uint16 components for the version. Version and AssemblyVersion store
    // int32 or uint32. When the former are initialized from the latter, the components are truncated to uint16 size. When the
    // latter are initialized from the former, they are zero-extended to int32 size. For uint16 components, the max value is
    // used to indicate an unspecified component. For int32 components, -1 is used. Since we're treating the version as an
    // assembly version here, map the uint16 unspecified value to the int32 size.
    m_dwMajor = dwMajor == UnspecifiedShort ? Unspecified : dwMajor;
    m_dwMinor = dwMinor == UnspecifiedShort ? Unspecified : dwMinor;
}

void AssemblyVersion::SetServiceVersion(DWORD dwBuild,
                                        DWORD dwRevision)
{
    // See comment in SetFeatureVersion, the same applies here
    m_dwBuild = dwBuild == UnspecifiedShort ? Unspecified : dwBuild;
    m_dwRevision = dwRevision == UnspecifiedShort ? Unspecified : dwRevision;
}

void AssemblyVersion::SetVersion(AssemblyVersion *pAssemblyVersion)
{
    m_dwMajor = pAssemblyVersion->GetMajor();
    m_dwMinor = pAssemblyVersion->GetMinor();
    m_dwBuild = pAssemblyVersion->GetBuild();
    m_dwRevision = pAssemblyVersion->GetRevision();
}

BOOL AssemblyVersion::Equals(AssemblyVersion *pAssemblyVersion)
{
    BOOL result = FALSE;
    if ((GetMajor() == pAssemblyVersion->GetMajor()) &&
        (GetMinor() == pAssemblyVersion->GetMinor()) &&
        (GetBuild() == pAssemblyVersion->GetBuild()) &&
        (GetRevision() == pAssemblyVersion->GetRevision()))
    {
        result = TRUE;
    }
    return result;
}

#endif
