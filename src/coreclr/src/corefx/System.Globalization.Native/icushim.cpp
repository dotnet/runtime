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

// .x.x.x, considering the max number of decimal digits for each component
static const int MaxICUVersionStringLength = 33;

// Get filename of an ICU library with the requested version in the name
// There are three possible cases of the version components values:
// 1. Only majorVer is not equal to -1 => result is baseFileName.majorver
// 2. Only majorVer and minorVer are not equal to -1 => result is baseFileName.majorver.minorVer
// 3. All components are not equal to -1 => result is baseFileName.majorver.minorVer.subver
void GetVersionedLibFileName(const char* baseFileName, int majorVer, int minorVer, int subVer, char* result)
{
    assert(majorVer != -1);

    int nameLen = sprintf(result, "%s.%d", baseFileName, majorVer);

    if (minorVer != -1)
    {
        nameLen += sprintf(result + nameLen, ".%d", minorVer);
        if (subVer != -1)
        {
            sprintf(result + nameLen, ".%d", subVer);
        }    
    }
}

// Try to open the necessary ICU libraries
bool OpenICULibraries(int majorVer, int minorVer, int subVer)
{
    char libicuucName[64];
    char libicui18nName[64];

    static_assert(sizeof("libicuuc.so") + MaxICUVersionStringLength <= sizeof(libicuucName), "The libicuucName is too small");
    GetVersionedLibFileName("libicuuc.so", majorVer, minorVer, subVer, libicuucName);

    static_assert(sizeof("libicui18n.so") + MaxICUVersionStringLength <= sizeof(libicui18nName), "The libicui18nName is too small");
    GetVersionedLibFileName("libicui18n.so", majorVer, minorVer, subVer, libicui18nName);

    libicuuc = dlopen(libicuucName, RTLD_LAZY);
    if (libicuuc != nullptr)
    {
        libicui18n = dlopen(libicui18nName, RTLD_LAZY);
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
bool FindLibUsingOverride(int* majorVer, int* minorVer, int* subVer)
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
            if (OpenICULibraries(first, second, third))
            {
                *majorVer = first;
                *minorVer = second;
                *subVer = third;
                return true;
            }
        }
    }

    return false;
}

// Select the highest supported version of ICU present on the local machine
// Search for library files with names including the major and minor version.
bool FindLibWithMajorMinorVersion(int* majorVer, int* minorVer)
{
    for (int i = MaxICUVersion; i >= MinICUVersion; i--)
    {
        for (int j = MaxMinorICUVersion; j >= MinMinorICUVersion; j--)
        {
            if (OpenICULibraries(i, j, -1))
            {
                *majorVer = i;
                *minorVer = j;
                return true;
            }
        }
    }

    return false;
}

// Select the highest supported version of ICU present on the local machine
// Search for library files with names including the major, minor and sub version.
bool FindLibWithMajorMinorSubVersion(int* majorVer, int* minorVer, int* subVer)
{
    for (int i = MaxICUVersion; i >= MinICUVersion; i--)
    {
        for (int j = MaxMinorICUVersion; j >= MinMinorICUVersion; j--)
        {
            for (int k = MaxSubICUVersion; k >= MinSubICUVersion; k--)
            {
                if (OpenICULibraries(i, j, k))
                {
                    *majorVer = i;
                    *minorVer = j;
                    *subVer = k;
                    return true;
                }
            }
        }
    }

    return false;
}

// This function is ran at the end of dlopen for the current shared library
__attribute__((constructor))
void InitializeICUShim()
{
    int majorVer = -1;
    int minorVer = -1;
    int subVer = -1;

    if (!FindLibUsingOverride(&majorVer, &minorVer, &subVer) &&
        !FindLibWithMajorMinorVersion(&majorVer, &minorVer) &&
        !FindLibWithMajorMinorSubVersion(&majorVer, &minorVer, &subVer))
    {
        // No usable ICU version found
        fprintf(stderr, "No usable version of the ICU libraries was found\n");
        abort();
    }

    char symbolName[128];
    char symbolVersion[MaxICUVersionStringLength + 1] = "";

    // Find out the format of the version string added to each symbol
    // First try just the unversioned symbol
    if (dlsym(libicuuc, "u_strlen") == nullptr)
    {
        // Now try just the _majorVer added
        sprintf(symbolVersion, "_%d", majorVer);
        sprintf(symbolName, "u_strlen%s", symbolVersion);
        if (dlsym(libicuuc, symbolName) == nullptr)
        {
            // Now try the _majorVer_minorVer added
            sprintf(symbolVersion, "_%d_%d", majorVer, minorVer);
            sprintf(symbolName, "u_strlen%s", symbolVersion);
            if (dlsym(libicuuc, symbolName) == nullptr)
            {
                // Finally, try the _majorVer_minorVer_subVer added
                sprintf(symbolVersion, "_%d_%d_%d", majorVer, minorVer, subVer);
                sprintf(symbolName, "u_strlen%s", symbolVersion);
                if (dlsym(libicuuc, symbolName) == nullptr)
                {
                    fprintf(stderr, "ICU libraries use unknown symbol versioning\n");
                    abort();
                }
            }
        }
    }

    // Get pointers to all the ICU functions that are needed
#define PER_FUNCTION_BLOCK(fn, lib) \
    static_assert((sizeof(#fn) + MaxICUVersionStringLength + 1) <= sizeof(symbolName), "The symbolName is too small for symbol " #fn); \
    sprintf(symbolName, #fn "%s", symbolVersion); \
    fn##_ptr = (decltype(fn)*)dlsym(lib, symbolName); \
    if (fn##_ptr == NULL) { fprintf(stderr, "Cannot get symbol %s from " #lib "\n", symbolName); abort(); }

    FOR_ALL_ICU_FUNCTIONS
#undef PER_FUNCTION_BLOCK
}

__attribute__((destructor))
void ShutdownICUShim()
{
    if (libicuuc != nullptr)
    {
        dlclose(libicuuc);
    }

    if (libicui18n != nullptr)
    {
        dlclose(libicui18n);
    }
}
