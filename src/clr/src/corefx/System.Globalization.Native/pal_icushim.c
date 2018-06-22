// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <dlfcn.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <assert.h>

#include "icushim.h"

// Define pointers to all the used ICU functions
#define PER_FUNCTION_BLOCK(fn, lib) decltype(fn)* fn##_ptr;
FOR_ALL_ICU_FUNCTIONS
#undef PER_FUNCTION_BLOCK

static void* libicuuc = nullptr;
static void* libicui18n = nullptr;

#define VERSION_PREFIX_NONE ""
#define VERSION_PREFIX_SUSE "suse"

// .[suse]x.x.x, considering the max number of decimal digits for each component
static const int MaxICUVersionStringLength = sizeof(VERSION_PREFIX_SUSE) + 33;

#ifdef __APPLE__

bool FindICULibs(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
#ifndef OSX_ICU_LIBRARY_PATH
    static_assert(false, "The ICU Library path is not defined");
#endif // OSX_ICU_LIBRARY_PATH

    // Usually OSX_ICU_LIBRARY_PATH is "/usr/lib/libicucore.dylib"
    libicuuc = dlopen(OSX_ICU_LIBRARY_PATH, RTLD_LAZY);
    
    if (libicuuc == nullptr)
    {
        return false;
    }

    // in OSX all ICU APIs exist in the same library libicucore.A.dylib 
    libicui18n = libicuuc;

    return true;
}

#else // __APPLE__

// Version ranges to search for each of the three version components
// The rationale for major version range is that we support versions higher or
// equal to the version we are built against and less or equal to that version
// plus 20 to give us enough headspace. The ICU seems to version about twice
// a year.
static const int MinICUVersion = U_ICU_VERSION_MAJOR_NUM;
static const int MaxICUVersion = MinICUVersion + 20;
static const int MinMinorICUVersion = 1;
static const int MaxMinorICUVersion = 5;
static const int MinSubICUVersion = 1;
static const int MaxSubICUVersion = 5;

// Get filename of an ICU library with the requested version in the name
// There are three possible cases of the version components values:
// 1. Only majorVer is not equal to -1 => result is baseFileName.majorver
// 2. Only majorVer and minorVer are not equal to -1 => result is baseFileName.majorver.minorVer
// 3. All components are not equal to -1 => result is baseFileName.majorver.minorVer.subver
void GetVersionedLibFileName(const char* baseFileName, int majorVer, int minorVer, int subVer, const char* versionPrefix, char* result)
{
    assert(majorVer != -1);

    int nameLen = sprintf(result, "%s.%s%d", baseFileName, versionPrefix, majorVer);

    if (minorVer != -1)
    {
        nameLen += sprintf(result + nameLen, ".%d", minorVer);
        if (subVer != -1)
        {
            sprintf(result + nameLen, ".%d", subVer);
        }    
    }
}

bool FindSymbolVersion(int majorVer, int minorVer, int subVer, char* symbolName, char* symbolVersion)
{
    // Find out the format of the version string added to each symbol
    // First try just the unversioned symbol
    if (dlsym(libicuuc, "u_strlen") == nullptr)
    {
        // Now try just the _majorVer added
        sprintf(symbolVersion, "_%d", majorVer);
        sprintf(symbolName, "u_strlen%s", symbolVersion);
        if ((dlsym(libicuuc, symbolName) == nullptr) && (minorVer != -1))
        {
            // Now try the _majorVer_minorVer added
            sprintf(symbolVersion, "_%d_%d", majorVer, minorVer);
            sprintf(symbolName, "u_strlen%s", symbolVersion);
            if ((dlsym(libicuuc, symbolName) == nullptr) && (subVer != -1))
            {
                // Finally, try the _majorVer_minorVer_subVer added
                sprintf(symbolVersion, "_%d_%d_%d", majorVer, minorVer, subVer);
                sprintf(symbolName, "u_strlen%s", symbolVersion);
                if (dlsym(libicuuc, symbolName) == nullptr)
                {
                    return false;
                }
            }
        }
    }

    return true;
}

// Try to open the necessary ICU libraries
bool OpenICULibraries(int majorVer, int minorVer, int subVer, const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    char libicuucName[64];
    char libicui18nName[64];

    static_assert(sizeof("libicuuc.so") + MaxICUVersionStringLength <= sizeof(libicuucName), "The libicuucName is too small");
    GetVersionedLibFileName("libicuuc.so", majorVer, minorVer, subVer, versionPrefix, libicuucName);

    static_assert(sizeof("libicui18n.so") + MaxICUVersionStringLength <= sizeof(libicui18nName), "The libicui18nName is too small");
    GetVersionedLibFileName("libicui18n.so", majorVer, minorVer, subVer, versionPrefix, libicui18nName);

    libicuuc = dlopen(libicuucName, RTLD_LAZY);
    if (libicuuc != nullptr)
    {
        if (FindSymbolVersion(majorVer, minorVer, subVer, symbolName, symbolVersion))
        {
            libicui18n = dlopen(libicui18nName, RTLD_LAZY);
        }
        if (libicui18n == nullptr)
        {
            dlclose(libicuuc);
            libicuuc = nullptr;
        }
    }

    return libicuuc != nullptr;
}

// Select libraries using the version override specified by the CLR_ICU_VERSION_OVERRIDE
// environment variable.
// The format of the string in this variable is majorVer[.minorVer[.subVer]] (the brackets
// indicate optional parts).
bool FindLibUsingOverride(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    char* versionOverride = getenv("CLR_ICU_VERSION_OVERRIDE");
    if (versionOverride != nullptr)
    {
        int first = -1;
        int second = -1;
        int third = -1;

        int matches = sscanf(versionOverride, "%d.%d.%d", &first, &second, &third);
        if (matches > 0)
        {
            if (OpenICULibraries(first, second, third, versionPrefix, symbolName, symbolVersion))
            {
                return true;
            }
        }
    }

    return false;
}

// Search for library files with names including the major version.
bool FindLibWithMajorVersion(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    // ICU packaging documentation (http://userguide.icu-project.org/packaging)
    // describes applications link against the major (e.g. libicuuc.so.54).

    // Select the version of ICU present at build time.
    if (OpenICULibraries(MinICUVersion, -1, -1, versionPrefix, symbolName, symbolVersion))
    {
        return true;
    }

    // Select the highest supported version of ICU present on the local machine
    for (int i = MaxICUVersion; i > MinICUVersion; i--)
    {
        if (OpenICULibraries(i, -1, -1, versionPrefix, symbolName, symbolVersion))
        {
            return true;
        }
    }

    return false;
}

// Select the highest supported version of ICU present on the local machine
// Search for library files with names including the major and minor version.
bool FindLibWithMajorMinorVersion(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    for (int i = MaxICUVersion; i >= MinICUVersion; i--)
    {
        for (int j = MaxMinorICUVersion; j >= MinMinorICUVersion; j--)
        {
            if (OpenICULibraries(i, j, -1, versionPrefix, symbolName, symbolVersion))
            {
                return true;
            }
        }
    }

    return false;
}

// Select the highest supported version of ICU present on the local machine
// Search for library files with names including the major, minor and sub version.
bool FindLibWithMajorMinorSubVersion(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    for (int i = MaxICUVersion; i >= MinICUVersion; i--)
    {
        for (int j = MaxMinorICUVersion; j >= MinMinorICUVersion; j--)
        {
            for (int k = MaxSubICUVersion; k >= MinSubICUVersion; k--)
            {
                if (OpenICULibraries(i, j, k, versionPrefix, symbolName, symbolVersion))
                {
                    return true;
                }
            }
        }
    }

    return false;
}


bool FindICULibs(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    return FindLibUsingOverride(versionPrefix, symbolName, symbolVersion) ||
           FindLibWithMajorVersion(versionPrefix, symbolName, symbolVersion) ||
           FindLibWithMajorMinorVersion(versionPrefix, symbolName, symbolVersion) ||
           FindLibWithMajorMinorSubVersion(versionPrefix, symbolName, symbolVersion);
}

#endif // __APPLE__

// GlobalizationNative_LoadICU
// This method get called from the managed side during the globalization initialization. 
// This method shouldn't get called at all if we are running in globalization invariant mode
// return 0 if failed to load ICU and 1 otherwise
extern "C" int32_t GlobalizationNative_LoadICU()
{
    char symbolName[128];
    char symbolVersion[MaxICUVersionStringLength + 1] = "";

    if (!FindICULibs(VERSION_PREFIX_NONE, symbolName, symbolVersion))
    {
#ifndef __APPLE__
        if (!FindICULibs(VERSION_PREFIX_SUSE, symbolName, symbolVersion))
#endif
        {
            return 0;
        }
    }

    // Get pointers to all the ICU functions that are needed
#define PER_FUNCTION_BLOCK(fn, lib) \
    static_assert((sizeof(#fn) + MaxICUVersionStringLength + 1) <= sizeof(symbolName), "The symbolName is too small for symbol " #fn); \
    sprintf(symbolName, #fn "%s", symbolVersion); \
    fn##_ptr = (decltype(fn)*)dlsym(lib, symbolName); \
    if (fn##_ptr == NULL) { fprintf(stderr, "Cannot get symbol %s from " #lib "\nError: %s\n", symbolName, dlerror()); abort(); }

    FOR_ALL_ICU_FUNCTIONS
#undef PER_FUNCTION_BLOCK

#ifdef __APPLE__
    // libicui18n initialized with libicuuc so we null it to avoid double closing same handle   
    libicui18n = nullptr;
#endif // __APPLE__

    return 1;
}

// GlobalizationNative_GetICUVersion
// return the current loaded ICU version
extern "C" int32_t GlobalizationNative_GetICUVersion()
{
    int32_t version;
    u_getVersion((uint8_t *) &version);
    return version; 
}

__attribute__((destructor))
void ShutdownICUShim()
{
    if (libicuuc != nullptr)
    {
        dlclose(libicuuc);
        libicuuc = nullptr;
    }

    if (libicui18n != nullptr)
    {
        dlclose(libicui18n);
        libicui18n = nullptr;
    }
}
