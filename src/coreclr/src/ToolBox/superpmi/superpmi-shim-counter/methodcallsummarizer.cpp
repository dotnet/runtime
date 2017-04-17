//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "methodcallsummarizer.h"

MethodCallSummarizer::MethodCallSummarizer(WCHAR* logPath)
{
    numNames = 0;
    names    = nullptr;
    counts   = nullptr;

    WCHAR* ExecutableName = GetCommandLineW();
    WCHAR* quote1         = NULL;

    // if there are any quotes in filename convert them to spaces.
    while ((quote1 = wcsstr(ExecutableName, W("\""))) != NULL)
        *quote1 = W(' ');

    // remove any illegal or annoying characters from file name by converting them to underscores
    while ((quote1 = wcspbrk(ExecutableName, W("=<>:\"/\\|?! *.,"))) != NULL)
        *quote1 = W('_');

    const WCHAR* DataFileExtension       = W(".csv");
    size_t       ExecutableNameLength    = wcslen(ExecutableName);
    size_t       DataFileExtensionLength = wcslen(DataFileExtension);
    size_t       logPathLength           = wcslen(logPath);

    unsigned int randNumber = 0;
    WCHAR        RandNumberString[9];
    RandNumberString[0]     = L'\0';
    size_t RandNumberLength = 0;

    size_t dataFileNameLength =
        logPathLength + 1 + ExecutableNameLength + 1 + RandNumberLength + 1 + DataFileExtensionLength + 1;

    const size_t MaxAcceptablePathLength =
        MAX_PATH - 20; // subtract 20 to leave buffer, for possible random number addition
    if (dataFileNameLength >= MaxAcceptablePathLength)
    {
        // The path name is too long; creating the file will fail. This can happen because we use the command line,
        // which for ngen includes lots of environment variables, for example.

        // Assume (!) the extra space is all in the ExecutableName, so shorten that.
        ExecutableNameLength -= dataFileNameLength - MaxAcceptablePathLength;

        dataFileNameLength = MaxAcceptablePathLength;

#ifdef FEATURE_PAL
        PAL_Random(/* bStrong */ FALSE, &randNumber, sizeof(randNumber));
#else  // !FEATURE_PAL
        rand_s(&randNumber);
#endif // !FEATURE_PAL

        RandNumberLength = 9; // 8 hex digits + null
        swprintf_s(RandNumberString, RandNumberLength, W("%08X"), randNumber);

        dataFileNameLength += RandNumberLength - 1;
    }

    dataFileName    = new WCHAR[dataFileNameLength];
    dataFileName[0] = 0;
    wcsncat_s(dataFileName, dataFileNameLength, logPath, logPathLength);
    wcsncat_s(dataFileName, dataFileNameLength, W("\\\0"), 1);
    wcsncat_s(dataFileName, dataFileNameLength, ExecutableName, ExecutableNameLength);

    if (RandNumberLength > 0)
    {
        wcsncat_s(dataFileName, dataFileNameLength, RandNumberString, RandNumberLength);
    }

    wcsncat_s(dataFileName, dataFileNameLength, DataFileExtension, DataFileExtensionLength);
}

// lots of ways will be faster.. this happens to be decently simple and good enough for the task at hand and nicely
// sorts the output. in this approach the most commonly added items are at the top of the list... 60% landed in the first
// three slots in short runs
void MethodCallSummarizer::AddCall(const char* name)
{
    // if we can find it already in our list, increment the count
    for (int i = 0; i < numNames; i++)
    {
        if (strcmp(name, names[i]) == 0)
        {
            counts[i]++;
            for (i = 1; i < numNames; i++)
                if (counts[i] > counts[i - 1])
                {
                    unsigned int tempui = counts[i - 1];
                    counts[i - 1]       = counts[i];
                    counts[i]           = tempui;
                    char* tempc         = names[i - 1];
                    names[i - 1]        = names[i];
                    names[i]            = tempc;
                }
            return;
        }
    }

    // else we didn't find it, so add it
    char**        tnames  = names;
    unsigned int* tcounts = counts;

    names = new char*[numNames + 1];
    if (tnames != nullptr)
    {
        memcpy(names, tnames, numNames * sizeof(char*));
        delete tnames;
    }

    size_t tlen     = strlen(name);
    names[numNames] = new char[tlen + 1];
    memcpy(names[numNames], name, tlen + 1);

    counts = new unsigned int[numNames + 1];
    if (tcounts != nullptr)
    {
        memcpy(counts, tcounts, numNames * sizeof(unsigned int));
        delete tcounts;
    }
    counts[numNames] = 1;

    numNames++;
}

void MethodCallSummarizer::SaveTextFile()
{
    char   buff[512];
    DWORD  bytesWritten = 0;
    HANDLE hFile        = CreateFileW(dataFileName, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                               FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);

    DWORD len = (DWORD)sprintf_s(buff, 512, "FunctionName,Count\n");
    WriteFile(hFile, buff, len, &bytesWritten, NULL);

    for (int i = 0; i < numNames; i++)
    {
        len = sprintf_s(buff, 512, "%s,%u\n", names[i], counts[i]);
        WriteFile(hFile, buff, len, &bytesWritten, NULL);
    }
    CloseHandle(hFile);
}
