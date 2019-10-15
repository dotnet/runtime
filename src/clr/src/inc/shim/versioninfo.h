// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// versioninfo.h
//
// representation of version#
// 
// Note: must be platform independent
//
// ======================================================================================

#ifndef VERSIONINFO_H
#define VERSIONINFO_H


struct VersionInfo
{
protected:
    unsigned short m_wMajor;
    unsigned short m_wMinor;
    unsigned short m_wBuild;
    unsigned short m_wRevision;
public:
    VersionInfo();
    VersionInfo( const unsigned short major,
                            const unsigned short minor,
                            const unsigned short build,
                            const unsigned short revision
                            );
    unsigned short Major() const;
    unsigned short Minor() const;
    unsigned short Build() const;
    unsigned short Revision() const;

    // 1 if "this" is bigger, -1 if "this' is smaller 
    int Compare(const VersionInfo& version) const;

    // on success returns count of  numbers read (<=4), otherwise -1
    static int Parse(   LPCTSTR szString,  
                                VersionInfo* result);
};

#include "versioninfo.inl"

#endif	
