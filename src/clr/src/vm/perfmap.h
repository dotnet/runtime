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

// Generates a perfmap file.
class PerfMap
{
private:
    // The one and only PerfMap for the process.
    static PerfMap * s_Current;

    // The file stream to write the map to.
    CFileStream * m_FileStream;

    // Set to true if an error is encountered when writing to the file.
    bool m_ErrorEncountered;

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
    void LogMethod(MethodDesc * pMethod, PCODE pCode, size_t codeSize);

    // Does the actual work to log a native image load to the map.
    void LogNativeImage(PEFile * pFile);

    // Get the native image signature and store it as a string.
    static void GetNativeImageSignature(PEFile * pFile, WCHAR * pwszSig, unsigned int nSigSize);

public:
    // Initialize the map for the current process.
    static void Initialize();

    // Log a native image load to the map.
    static void LogNativeImageLoad(PEFile * pFile);

    // Log a JIT compiled method to the map.
    static void LogJITCompiledMethod(MethodDesc * pMethod, PCODE pCode, size_t codeSize);

    // Close the map and flush any remaining data.
    static void Destroy();
};

// Generates a perfmap file for a native image by running crossgen.
class NativeImagePerfMap : PerfMap
{
private:
    // Log a pre-compiled method to the map.
    void LogPreCompiledMethod(MethodDesc * pMethod, PCODE pCode, SIZE_T baseAddr);

public:
    // Construct a new map for a native image.
    NativeImagePerfMap(Assembly * pAssembly, BSTR pDestPath);

    // Log method information for each module.
    void LogDataForModule(Module * pModule);
};

#endif // PERFPID_H
