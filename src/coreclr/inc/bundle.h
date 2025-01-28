// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 **                                                                         **
 ** bundle.h - Information about applications bundled as a single-file      **
 **                                                                         **
 *****************************************************************************/

#ifndef _BUNDLE_H_
#define _BUNDLE_H_

#include <sstring.h>
#include "coreclrhost.h"

class Bundle;

struct BundleFileLocation
{
    INT64 Size;
#if defined(TARGET_ANDROID)
    void* DataStart;
    constexpr static INT64 Offset = 0;
    constexpr static INT64 UncompresedSize = 0;
#else
    INT64 Offset;
    INT64 UncompresedSize;
#endif

    BundleFileLocation()
    {
        LIMITED_METHOD_CONTRACT;

        Size = 0;
#if defined(TARGET_ANDROID)
        DataStart = INVALID_HANDLE_VALUE;
#else
        Offset = 0;
        UncompresedSize = 0;
#endif
    }

    static BundleFileLocation Invalid() { LIMITED_METHOD_CONTRACT; return BundleFileLocation(); }

    const SString &Path() const;
#if defined(TARGET_ANDROID)
    const SString &AppName() const;
    bool IsValid() const { LIMITED_METHOD_CONTRACT; return DataStart != nullptr; }
#else // TARGET_ANDROID
    bool IsValid() const { LIMITED_METHOD_CONTRACT; return Offset != 0; }
#endif // !TARGET_ANDROID
};

class Bundle
{
public:
    Bundle(LPCSTR bundlePath, BundleProbeFn *probe);
    BundleFileLocation Probe(const SString& path, bool pathIsBundleRelative = false) const;

    const SString &Path() const { LIMITED_METHOD_CONTRACT; return m_path; }
    const SString &BasePath() const { LIMITED_METHOD_CONTRACT; return m_basePath; }

    static Bundle* AppBundle; // The BundleInfo for the current app, initialized by coreclr_initialize.
    static bool AppIsBundle() { LIMITED_METHOD_CONTRACT; return AppBundle != nullptr; }
    static BundleFileLocation ProbeAppBundle(const SString& path, bool pathIsBundleRelative = false);

private:
#if defined(TARGET_ANDROID)
    SString m_appName;
#endif
    SString m_path; // The path to single-file executable
    BundleProbeFn *m_probe;

    SString m_basePath; // The prefix to denote a path within the bundle
    COUNT_T m_basePathLength = 0;
};

#endif // _BUNDLE_H_
