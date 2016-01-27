// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
    m_dwMajor = dwMajor;
    m_dwMinor = dwMinor;
}

void AssemblyVersion::SetServiceVersion(DWORD dwBuild,
                                        DWORD dwRevision)
{
    m_dwBuild = dwBuild;
    m_dwRevision = dwRevision;
}

BOOL AssemblyVersion::SetVersion(LPCWSTR pwzVersionStr)
{
    SmallStackSString versionString(pwzVersionStr);

    return TextualIdentityParser::ParseVersion(versionString, this);
}

void AssemblyVersion::SetVersion(AssemblyVersion *pAssemblyVersion)
{
    m_dwMajor = pAssemblyVersion->GetMajor();
    m_dwMinor = pAssemblyVersion->GetMinor();
    m_dwBuild = pAssemblyVersion->GetBuild();
    m_dwRevision = pAssemblyVersion->GetRevision();
}

BOOL AssemblyVersion::IsLargerFeatureVersion(AssemblyVersion *pAssemblyVersion)
{
    BOOL result = FALSE;

    if (GetMajor() > pAssemblyVersion->GetMajor())
    {
        result = TRUE;
    }
    else if ((GetMajor() == pAssemblyVersion->GetMajor()) &&
             (GetMinor() > pAssemblyVersion->GetMinor()))
    {
        result = TRUE;
    }

    return result;
}

BOOL AssemblyVersion::IsEqualFeatureVersion(AssemblyVersion *pAssemblyVersion)
{
    BOOL result = FALSE;

    if ((GetMajor() == pAssemblyVersion->GetMajor()) &&
        (GetMinor() == pAssemblyVersion->GetMinor()))
    {
        result = TRUE;
    }

    return result;
}

BOOL AssemblyVersion::IsSmallerFeatureVersion(AssemblyVersion *pAssemblyVersion)
{
    BOOL result = FALSE;

    if (GetMajor() < pAssemblyVersion->GetMajor())
    {
        result = TRUE;
    }
    else if ((GetMajor() == pAssemblyVersion->GetMajor()) &&
             (GetMinor() < pAssemblyVersion->GetMinor()))
    {
        result = TRUE;
    }

    return result;
}

BOOL AssemblyVersion::IsEqualServiceVersion(AssemblyVersion *pAssemblyVersion)
{
    BOOL result = FALSE;

    if ((GetBuild() == pAssemblyVersion->GetBuild()) &&
        (GetRevision() == pAssemblyVersion->GetRevision()))
    {
        result = TRUE;
    }

    return result;
}

BOOL AssemblyVersion::IsLargerServiceVersion(AssemblyVersion *pAssemblyVersion)
{
    BOOL result = FALSE;

    if (GetBuild() > pAssemblyVersion->GetBuild())
    {
        result = TRUE;
    }
    else if ((GetBuild() == pAssemblyVersion->GetBuild()) &&
             (GetRevision() > pAssemblyVersion->GetRevision()))
    {
        result = TRUE;
    }

    return result;
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

BOOL AssemblyVersion::IsSmallerOrEqual(AssemblyVersion *pAssemblyVersion)
{
    return (Equals(pAssemblyVersion) ||
            IsSmallerFeatureVersion(pAssemblyVersion) ||
            (IsEqualFeatureVersion(pAssemblyVersion) &&
             !IsLargerServiceVersion(pAssemblyVersion)));
}

BOOL AssemblyVersion::IsLargerOrEqual(AssemblyVersion *pAssemblyVersion)
{
    return (Equals(pAssemblyVersion) ||
            IsLargerFeatureVersion(pAssemblyVersion) ||
            (IsEqualFeatureVersion(pAssemblyVersion) &&
             IsLargerServiceVersion(pAssemblyVersion)));
}

#endif
