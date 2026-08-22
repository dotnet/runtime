// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// MCList.h - MethodContext List utility class
//----------------------------------------------------------
#ifndef _MCList
#define _MCList
#define MAXMCLFILESIZE 0xFFFFFF

class MCList
{
public:
    static bool processArgAsMCL(char* input, std::vector<int>& list);

    MCList()
    {
        // Initialize the static file handle
        fpMCLFile = NULL;
    }

    // Methods to create an MCL file
    void InitializeMCL(char* filename);
    void AddMethodToMCL(int methodIndex);
    void CloseMCL();

private:
    static bool getLineData(const char* nameOfInput, std::vector<int>& indexes);

    // File handle for MCL file
    FILE* fpMCLFile;
};
#endif
