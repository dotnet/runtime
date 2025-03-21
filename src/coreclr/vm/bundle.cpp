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

Bundle::Bundle(LPCSTR bundlePath, BundleProbeFn *probe, ExternalAssemblyProbeFn* externalAssemblyProbe)
    : m_probe(probe)
    , m_externalAssemblyProbe(externalAssemblyProbe)
    , m_basePathLength(0)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_probe != nullptr || m_externalAssemblyProbe != nullptr);

    // On Android this is not a real path, but rather the application's package name
    m_path.SetUTF8(bundlePath);
#if !defined(TARGET_ANDROID)
    // The bundle-base path is the directory containing the single-file bundle.
    // When the Probe() function searches within the bundle, it masks out the basePath from the assembly-path (if found).

    LPCSTR pos = strrchr(bundlePath, DIRECTORY_SEPARATOR_CHAR_A);
    _ASSERTE(pos != nullptr);
    size_t baseLen = pos - bundlePath + 1; // Include DIRECTORY_SEPARATOR_CHAR_A in m_basePath
    m_basePath.SetUTF8(bundlePath, (COUNT_T)baseLen);
    m_basePathLength = (COUNT_T)baseLen;
#endif // !TARGET_ANDROID
}

BundleFileLocation Bundle::Probe(const SString& path, bool pathIsBundleRelative) const
{
    STANDARD_VM_CONTRACT;

    // Skip over m_base_path, if any. For example:
    //    Bundle.Probe("lib.dll") => m_probe("lib.dll")
    //    Bundle.Probe("path/to/exe/lib.dll") => m_probe("lib.dll")
    //    Bundle.Probe("path/to/exe/and/some/more/lib.dll") => m_probe("and/some/more/lib.dll")

    StackSString pathBuffer;
    pathBuffer.SetAndConvertToUTF8(path.GetUnicode());
    LPCSTR utf8Path(pathBuffer.GetUTF8());

    if (!pathIsBundleRelative)
    {
#ifdef TARGET_UNIX
        if (u16_strncmp(m_basePath, path, m_basePath.GetCount()) == 0)
#else
        if (_wcsnicmp(m_basePath, path, m_basePath.GetCount()) == 0)
#endif // TARGET_UNIX
        {
            utf8Path += m_basePathLength; // m_basePath includes count for DIRECTORY_SEPARATOR_CHAR_W
        }
        else
        {
            // This is not a file within the bundle
            return BundleFileLocation::Invalid();
        }
    }

    if (m_probe != nullptr)
    {
        BundleFileLocation loc;
        INT64 fileSize = 0;
        INT64 compressedSize = 0;
        if (m_probe(utf8Path, &loc.Offset, &fileSize, &compressedSize))
        {
            // Found assembly in bundle
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
    }

    if (m_externalAssemblyProbe != nullptr)
    {
        BundleFileLocation loc;
        if (m_externalAssemblyProbe(utf8Path, &loc.DataStart, &loc.Size))
        {
            // Found via external assembly probe
            return loc;
        }
    }

    return BundleFileLocation::Invalid();
}

BundleFileLocation Bundle::ProbeAppBundle(const SString& path, bool pathIsBundleRelative)
{
    STANDARD_VM_CONTRACT;

    return AppIsBundle() ? AppBundle->Probe(path, pathIsBundleRelative) : BundleFileLocation::Invalid();
}
