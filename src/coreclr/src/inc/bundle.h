// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 **                                                                         **
 ** bundle.h - Information about applications bundled as a single-file  **
 **                                                                         **
 *****************************************************************************/

#ifndef _BUNDLE_H_
#define _BUNDLE_H_

#include <sstring.h>

class Bundle;

struct BundleFileLocation
{
    INT64 Size;
    INT64 Offset;

    BundleFileLocation()
    { 
        LIMITED_METHOD_CONTRACT;

        Size = 0; 
        Offset = 0; 
    }

    static BundleFileLocation Invalid() { LIMITED_METHOD_CONTRACT; return BundleFileLocation(); }

    const SString &Path() const;

    bool IsValid() const { LIMITED_METHOD_CONTRACT; return Offset != 0; }
};

typedef bool(__stdcall BundleProbe)(LPCWSTR, INT64*, INT64*);

class Bundle
{
public:
    Bundle(LPCWSTR bundlePath, BundleProbe *probe);
    BundleFileLocation Probe(LPCWSTR path, bool pathIsBundleRelative = false) const;

    const SString &Path() const { LIMITED_METHOD_CONTRACT; return m_path; }
    const SString &BasePath() const { LIMITED_METHOD_CONTRACT; return m_basePath; }

    static Bundle* AppBundle; // The BundleInfo for the current app, initialized by coreclr_initialize.
    static bool AppIsBundle() { LIMITED_METHOD_CONTRACT; return AppBundle != nullptr; }
    static BundleFileLocation ProbeAppBundle(LPCWSTR path, bool pathIsBundleRelative = false);

private:

    SString m_path; // The path to single-file executable
    BundleProbe *m_probe;

    SString m_basePath; // The prefix to denote a path within the bundle
};

#endif // _BUNDLE_H_
// EOF =======================================================================
