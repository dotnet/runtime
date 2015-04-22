//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ===========================================================================
// File: perfmap.h
//
#ifndef PERFPID_H
#define PERFPID_H

#include "sstring.h"
#include "fstream.h"

class PerfMap
{
private:
    // The one and only PerfMap for the process.
    static PerfMap * s_Current;

    // The file stream to write the map to.
    CFileStream * m_FileStream;

    // Set to true if an error is encountered when writing to the file.
    bool m_ErrorEncountered;

    // Construct a new map.
    PerfMap(int pid);

    // Clean-up resources.
    ~PerfMap();

    // Does the actual work to log to the map.
    void Log(MethodDesc * pMethod, PCODE pCode, size_t codeSize);

    // Does the actual work to close and flush the map.
    void Close();

public:
    // Initialize the map for the current process.
    static void Initialize();

    // Log a method to the map.
    static void LogMethod(MethodDesc * pMethod, PCODE pCode, size_t codeSize);
   
    // Close the map and flush any remaining data.
    static void Destroy();
};

#endif // PERFPID_H
