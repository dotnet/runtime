// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// Bundle.cpp
//
// Helpers to access meta-data stored in single-file bundles
//
//*****************************************************************************

#include "common.h"
#include "bundle.h"
#include <utilcode.h>
#include <corhost.h>
#include <sstring.h>

Bundle *Bundle::AppBundle = nullptr;

const SString &BundleFileLocation::Path() const
{
    LIMITED_METHOD_CONTRACT;

    // Currently, there is only one bundle -- the bundle for the main App.
    // Therefore, obtain the path from the global AppBundle.
    // If there is more than one bundle in one application (ex: single file plugins)
    // the BundlePath may be stored in the BundleFileLocation structure.

    _ASSERTE(IsValid());
    _ASSERTE(Bundle::AppBundle != nullptr);

    return Bundle::AppBundle->Path();
}

Bundle::Bundle(LPCSTR bundlePath, BundleProbeFn *probe)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(probe != nullptr);

    m_path.SetUTF8(bundlePath);
    m_probe = probe;

    // The bundle-base path is the directory containing the single-file bundle.
    // When the Probe() function searches within the bundle, it masks out the basePath from the assembly-path (if found).

    LPCSTR pos = strrchr(bundlePath, DIRECTORY_SEPARATOR_CHAR_A);
    _ASSERTE(pos != nullptr);
    size_t baseLen = pos - bundlePath + 1; // Include DIRECTORY_SEPARATOR_CHAR_A in m_basePath
    m_basePath.SetUTF8(bundlePath, (COUNT_T)baseLen);
    m_basePathLength = (COUNT_T)baseLen;
}

BundleFileLocation Bundle::Probe(const SString& path, bool pathIsBundleRelative) const
{
    STANDARD_VM_CONTRACT;

    BundleFileLocation loc;

    // Skip over m_base_path, if any. For example: 
    //    Bundle.Probe("lib.dll") => m_probe("lib.dll")
    //    Bundle.Probe("path/to/exe/lib.dll") => m_probe("lib.dll")
    //    Bundle.Probe("path/to/exe/and/some/more/lib.dll") => m_probe("and/some/more/lib.dll")

    StackScratchBuffer scratchBuffer;
    LPCSTR utf8Path(path.GetUTF8(scratchBuffer));

    if (!pathIsBundleRelative)
    {
#ifdef TARGET_UNIX
        if (wcsncmp(m_basePath, path, m_basePath.GetCount()) == 0)
#else
        if (_wcsnicmp(m_basePath, path, m_basePath.GetCount()) == 0)
#endif // TARGET_UNIX
        {
            utf8Path += m_basePathLength; // m_basePath includes count for DIRECTORY_SEPARATOR_CHAR_W
        }
        else
        {
            // This is not a file within the bundle
            return loc;
        }
    }

    INT64 fileSize = 0;
    INT64 compressedSize = 0;

    m_probe(utf8Path, &loc.Offset, &fileSize, &compressedSize);

    if (compressedSize)
    {
        loc.Size = compressedSize;
        loc.UncompresedSize = fileSize;
    }
    else
    {
        loc.Size = fileSize;
        loc.UncompresedSize = 0;
    }

    return loc;
}

BundleFileLocation Bundle::ProbeAppBundle(const SString& path, bool pathIsBundleRelative)
{
    STANDARD_VM_CONTRACT;

    return AppIsBundle() ? AppBundle->Probe(path, pathIsBundleRelative) : BundleFileLocation::Invalid();
}
