// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
    public:
        inline AssemblyVersion();
        inline ~AssemblyVersion();

        inline DWORD GetMajor();
        inline DWORD GetMinor();
        inline DWORD GetBuild();
        inline DWORD GetRevision();

        inline void SetFeatureVersion(/* in */ DWORD dwMajor,
                                      /* in */ DWORD dwMinor);
        inline void SetServiceVersion(/* in */ DWORD dwBuild,
                                      /* in */ DWORD dwRevision);
        inline BOOL SetServiceVersion(/* in */ LPCWSTR pwzVersionStr);
        inline BOOL SetVersion(/* in */ LPCWSTR pwzVersionStr);
        inline void SetVersion(AssemblyVersion *pAssemblyVersion);

        inline BOOL IsLargerFeatureVersion(/* in */ AssemblyVersion *pAssemblyVersion);
        inline BOOL IsEqualFeatureVersion(/* in */ AssemblyVersion *pAssemblyVersion);
        inline BOOL IsSmallerFeatureVersion(/* in */ AssemblyVersion *pAssemblyVersion);
        inline BOOL IsEqualServiceVersion(/* in */ AssemblyVersion *pAssemblyVersion);
        inline BOOL IsLargerServiceVersion(/* in */ AssemblyVersion *pAssemblyVersion);
        inline BOOL Equals(AssemblyVersion *pAssemblyVersion);
        inline BOOL IsSmallerOrEqual(AssemblyVersion *pAssemblyVersion);
        inline BOOL IsLargerOrEqual(AssemblyVersion *pAssemblyVersion);
    protected:
        DWORD m_dwMajor;
        DWORD m_dwMinor;
        DWORD m_dwBuild;
        DWORD m_dwRevision;
    };

#include "assemblyversion.inl"
};

#endif
