// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyName.inl
//


//
// Implements the inlined methods of AssemblyName class
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_NAME_INL__
#define __BINDER__ASSEMBLY_NAME_INL__

const SString &AssemblyName::GetSimpleName()
{
    return m_simpleName;
}

void AssemblyName::SetSimpleName(SString &simpleName)
{
    m_simpleName.Set(simpleName);
    SetHave(AssemblyIdentity::IDENTITY_FLAG_SIMPLE_NAME);
}

AssemblyVersion *AssemblyName::GetVersion()
{
    return &m_version;
}

void AssemblyName::SetVersion(AssemblyVersion *pAssemblyVersion)
{
    m_version.SetVersion(pAssemblyVersion);
}

SString &AssemblyName::GetCulture()
{
    return m_cultureOrLanguage;
}

void AssemblyName::SetCulture(SString &culture)
{
    m_cultureOrLanguage.Set(culture);
    SetHave(AssemblyIdentity::IDENTITY_FLAG_CULTURE);
}

SBuffer &AssemblyName::GetPublicKeyTokenBLOB()
{
    return m_publicKeyOrTokenBLOB;
}

PEKIND AssemblyName::GetArchitecture()
{
    return m_kProcessorArchitecture;
}

void AssemblyName::SetArchitecture(PEKIND kArchitecture)
{
    m_kProcessorArchitecture = kArchitecture;

    if (kArchitecture != peNone)
    {
        SetHave(AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE);
    }
    else
    {
        SetClear(AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE);
    }
}

AssemblyContentType AssemblyName::GetContentType()
{
    return m_kContentType;
}

void AssemblyName::SetContentType(AssemblyContentType kContentType)
{
    m_kContentType = kContentType;

    if (kContentType != AssemblyContentType_Default)
    {
        SetHave(AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE);
    }
    else
    {
        SetClear(AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE);
    }
}

BOOL AssemblyName::GetIsRetargetable()
{
    return m_dwIdentityFlags & AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE;
}

void AssemblyName::SetIsRetargetable(BOOL fIsRetargetable)
{
    if (fIsRetargetable)
    {
        m_dwNameFlags |= NAME_FLAG_RETARGETABLE;
        SetHave(AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE);
    }
    else
    {
        m_dwNameFlags &= ~NAME_FLAG_RETARGETABLE;
        SetClear(AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE);
    }
}

BOOL AssemblyName::GetIsDefinition()
{
    return ((m_dwNameFlags & NAME_FLAG_DEFINITION) != 0);
}

void AssemblyName::SetIsDefinition(BOOL fIsDefinition)
{
    if (fIsDefinition)
    {
        m_dwNameFlags |= NAME_FLAG_DEFINITION;
    }
    else
    {
        m_dwNameFlags &= ~NAME_FLAG_DEFINITION;
    }
}

#endif
