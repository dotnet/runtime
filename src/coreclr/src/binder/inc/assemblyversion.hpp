// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyVersion.hpp
//


//
// Defines the AssemblyVersion class
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_VERSION_HPP__
#define __BINDER__ASSEMBLY_VERSION_HPP__

#include "bindertypes.hpp"
#include "textualidentityparser.hpp"

namespace BINDER_SPACE
{
    class AssemblyVersion
    {
    private:
        static const DWORD Unspecified = (DWORD)-1;
        static const USHORT UnspecifiedShort = (USHORT)-1;

    public:
        inline AssemblyVersion();
        inline ~AssemblyVersion();

        inline BOOL HasMajor();
        inline BOOL HasMinor();
        inline BOOL HasBuild();
        inline BOOL HasRevision();

        inline DWORD GetMajor();
        inline DWORD GetMinor();
        inline DWORD GetBuild();
        inline DWORD GetRevision();

        inline void SetFeatureVersion(/* in */ DWORD dwMajor,
                                      /* in */ DWORD dwMinor);
        inline void SetServiceVersion(/* in */ DWORD dwBuild,
                                      /* in */ DWORD dwRevision);
        inline void SetVersion(AssemblyVersion *pAssemblyVersion);

        inline BOOL Equals(AssemblyVersion *pAssemblyVersion);
    private:
        DWORD m_dwMajor;
        DWORD m_dwMinor;
        DWORD m_dwBuild;
        DWORD m_dwRevision;
    };

#include "assemblyversion.inl"
};

#endif
