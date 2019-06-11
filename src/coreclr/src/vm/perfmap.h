// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: perfmap.h
//
#ifndef PERFPID_H
#define PERFPID_H

#include "sstring.h"
#include "fstream.h"

class PerfInfo;

// Generates a perfmap file.
class PerfMap
{
private:
    // The one and only PerfMap for the process.
    static PerfMap * s_Current;

    // Indicates whether optimization tiers should be shown for methods in perf maps
    static bool s_ShowOptimizationTiers;

    // The file stream to write the map to.
    CFileStream * m_FileStream;

    // The perfinfo file to log images to.
    PerfInfo* m_PerfInfo;

    // Set to true if an error is encountered when writing to the file.
    bool m_ErrorEncountered;

    // Set to true if an error is encountered when writing to the file.
    unsigned m_StubsMapped;

    // Construct a new map for the specified pid.
    PerfMap(int pid);

    // Write a line to the map file.
    void WriteLine(SString & line);

protected:
    // Construct a new map without a specified file name.
    // Used for offline creation of NGEN map files.
    PerfMap();

    // Clean-up resources.
    ~PerfMap();

    // Open the perf map file for write.
    void OpenFile(SString& path);

    // Does the actual work to log a method to the map.
    void LogMethod(MethodDesc * pMethod, PCODE pCode, size_t codeSize, const char *optimizationTier);

    // Does the actual work to log an image
    void LogImage(PEFile * pFile);

    // Get the image signature and store it as a string.
    static void GetNativeImageSignature(PEFile * pFile, WCHAR * pwszSig, unsigned int nSigSize);

public:
    // Initialize the map for the current process.
    static void Initialize();

    // Log a native image load to the map.
    static void LogImageLoad(PEFile * pFile);

    // Log a JIT compiled method to the map.
    static void LogJITCompiledMethod(MethodDesc * pMethod, PCODE pCode, size_t codeSize, PrepareCodeConfig *pConfig);

    // Log a set of stub to the map.
    static void LogStubs(const char* stubType, const char* stubOwner, PCODE pCode, size_t codeSize);

    // Close the map and flush any remaining data.
    static void Destroy();
};

// Generates a perfmap file for a native image by running crossgen.
class NativeImagePerfMap : PerfMap
{
private:
    // Log a pre-compiled method to the map.
    void LogPreCompiledMethod(MethodDesc * pMethod, PCODE pCode, SIZE_T baseAddr, const char *optimizationTier);

public:
    // Construct a new map for a native image.
    NativeImagePerfMap(Assembly * pAssembly, BSTR pDestPath);

    // Log method information for each module.
    void LogDataForModule(Module * pModule);
};

#endif // PERFPID_H
