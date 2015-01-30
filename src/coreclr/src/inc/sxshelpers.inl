//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#ifndef _SXSHELPERS_INL_
#define _SXSHELPERS_INL_

AssemblyVersion::AssemblyVersion()
:_major(0)
,_minor(0)
,_build(0)
,_revision(0)
{
    LIMITED_METHOD_CONTRACT;

}

AssemblyVersion::AssemblyVersion(AssemblyVersion& version)
{
    LIMITED_METHOD_CONTRACT;

    _major = version._major;
    _minor = version._minor;
    _build = version._build;
    _revision = version._revision;
}

HRESULT AssemblyVersion::Init(WORD major, WORD minor, WORD build, WORD revision)
{
    LIMITED_METHOD_CONTRACT;

    _major = major;
    _minor = minor;
    _build = build;
    _revision = revision;

    return S_OK;
}


void AssemblyVersion::SetBuild(WORD build)
{
    LIMITED_METHOD_CONTRACT;
    
    _build = build;
}

void AssemblyVersion::SetRevision(WORD revision)
{
    LIMITED_METHOD_CONTRACT;
    
    _revision = revision;
}

AssemblyVersion& AssemblyVersion::operator=(const AssemblyVersion& version)
{
    LIMITED_METHOD_CONTRACT;

    _major = version._major;
    _minor = version._minor;
    _build = version._build;
    _revision = version._revision;

    return *this;
}

BOOL operator<(const AssemblyVersion& version1,
               const AssemblyVersion& version2)
{
    WRAPPER_NO_CONTRACT;

    return !operator>=(version1, version2);
}


#endif /* _SXSHELPERS_INL_ */
