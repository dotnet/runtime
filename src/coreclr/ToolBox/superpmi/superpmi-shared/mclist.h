//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// MCList.h - MethodContext List utility class
//----------------------------------------------------------
#ifndef _MCList
#define _MCList
#define MAXMCLFILESIZE 0xFFFFFF

class MCList
{
public:
    static bool processArgAsMCL(char* input, int* count, int** list);

    MCList()
    {
        // Initialize the static file handle
        hMCLFile = INVALID_HANDLE_VALUE;
    }

    // Methods to create an MCL file
    void InitializeMCL(char* filename);
    void AddMethodToMCL(int methodIndex);
    void CloseMCL();

private:
    static bool getLineData(const char* nameOfInput, /* OUT */ int* pIndexCount, /* OUT */ int** pIndexes);

    // File handle for MCL file
    HANDLE hMCLFile;
};
#endif
