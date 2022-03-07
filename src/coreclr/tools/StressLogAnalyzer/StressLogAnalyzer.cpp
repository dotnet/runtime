// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <ctype.h>
#include <stdio.h>
#include <stdlib.h>
#include <windows.h>

#include "assert.h"

#include <minipal/utils.h>

#define MEMORY_MAPPED_STRESSLOG

#ifdef HOST_WINDOWS
#define MEMORY_MAPPED_STRESSLOG_BASE_ADDRESS (void*)0x400000000000
#else
#define MEMORY_MAPPED_STRESSLOG_BASE_ADDRESS nullptr
#endif

// This macro is used to standardize the wide character string literals between UNIX and Windows.
// Unix L"" is UTF32, and on windows it's UTF16.  Because of built-in assumptions on the size
// of string literals, it's important to match behaviour between Unix and Windows.  Unix will be defined
// as u"" (char16_t)
#ifdef TARGET_UNIX
#define W(str)  u##str
#else // TARGET_UNIX
#define W(str)  L##str
#endif // TARGET_UNIX

int ParseCommandLine(char* s, char** argv, int maxArgc)
{
    int argc = 0;
    bool prevWasSpace = true;
    bool insideString = false;
    while (*s)
    {
        if (!insideString)
        {
            if (isspace(*s))
            {
                *s = '\0';
                prevWasSpace = true;
            }
            else if (prevWasSpace)
            {
                // argument begins here
                if (argc < maxArgc - 1)
                {
                    argv[argc++] = s;
                }
                prevWasSpace = false;
            }
        }
        if (*s == '"')
        {
            insideString = !insideString;
        }
        else if (*s == '\\' && s[1] != '\0')
        {
            s++;
        }
        s++;
    }
    if (argc > 0)
    {
        argv[argc] = nullptr;
    }
    return argc;
}

int ProcessStressLog(void* baseAddress, int argc, char* argv[]);

int main(int argc, char *argv[])
{
#ifdef HOST_UNIX
    int exitCode = PAL_Initialize(argc, argv);
    if (exitCode != 0)
    {
        fprintf(stderr, "PAL initialization FAILED %d\n", exitCode);
        return exitCode;
    }
#endif

    if (argc < 2 || strcmp(argv[1], "-?") == 0)
    {
        printf("Usage: StressLog <log file> <options>\n");
        printf("       StressLog <log file> -? for list of options\n");
        return 1;
    }
    WCHAR filename[MAX_PATH];
    if (MultiByteToWideChar(CP_ACP, 0, argv[1], -1, filename, MAX_PATH) == 0)
        return 1;

    HANDLE file = CreateFile(filename, GENERIC_READ, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (file == INVALID_HANDLE_VALUE)
    {
        printf("file not found\n");
        return 1;
    }
    LARGE_INTEGER lsize;
    if (!GetFileSizeEx(file, &lsize))
    {
        printf("could not get file size\n");
        return 1;
    }
    size_t size = lsize.QuadPart;
#define USE_FILE_MAPPING
#ifdef USE_FILE_MAPPING
    HANDLE map = CreateFileMapping(file, NULL, PAGE_READONLY, (DWORD)(size >> 32), (DWORD)size, NULL);
    if (map == nullptr)
    {
        printf("could not create file mapping\n");
        return 1;
    }
    void* baseAddress = MapViewOfFileEx(map, FILE_MAP_READ, 0, 0, size, MEMORY_MAPPED_STRESSLOG_BASE_ADDRESS);
    if (baseAddress == nullptr)
    {
        printf("could not map view of file\n");
        return 1;
    }
#else
    void* baseAddress = VirtualAlloc((void*)0x400000000000, size, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    size_t remainingSize = size;
    const size_t maxReadSize = 0x80000000;
    char* readPtr = (char*)baseAddress;
    while (remainingSize >= maxReadSize)
    {
        DWORD sizeRead = 0;
        BOOL success = ReadFile(file, readPtr, maxReadSize, &sizeRead, NULL);
        if (!success || sizeRead != maxReadSize)
        {
            printf("oops, reading the file didn't work\n");
            return 1;
        }
        remainingSize -= maxReadSize;
        readPtr += maxReadSize;
    }
    if (remainingSize > 0)
    {
        DWORD sizeRead = 0;
        BOOL success = ReadFile(file, readPtr, remainingSize, &sizeRead, NULL);
        if (!success || sizeRead != remainingSize)
        {
            printf("oops, reading the file didn't work\n");
            return 1;
        }
    }
#endif
    argc -= 2;
    argv += 2;
    char* largv[128];
    memset(largv, 0, sizeof(largv));
    while (true)
    {
        int error = ProcessStressLog(baseAddress, argc, argv);

        if (error != 0)
        {
            printf("error %d occurred\n", error);
        }

        bool runAgain = false;
        char s[1024];
        while (true)
        {
            printf("'q' to quit, 'r' to run again\n>");
            if (fgets(s, 1023, stdin) == nullptr)
                continue;
            switch (s[0])
            {
            case 'r':
            case 'R':
                runAgain = true;
                break;

            case 'q':
            case 'Q':
                break;

            default:
                continue;
            }
            break;
        }
        if (runAgain)
        {
            int largc = ParseCommandLine(&s[1], largv, ARRAY_SIZE(largv));
            if (largc > 0)
            {
                argc = largc;
                argv = largv;
            }
        }
        else
        {
            break;
        }
    }

    return 0;
}
