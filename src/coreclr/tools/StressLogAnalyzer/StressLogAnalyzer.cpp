// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <ctype.h>
#include <stdio.h>
#include <stdlib.h>
#include <windows.h>

#include "assert.h"

#define MEMORY_MAPPED_STRESSLOG

int ParseCommandLine(wchar_t* s, wchar_t** argv, int maxArgc)
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

int wmain(int argc, wchar_t *argv[])
{
    if (argc < 2 || wcscmp(argv[1], L"-?") == 0)
    {
        printf("Usage: StressLog <log file> <options>\n");
        printf("       StressLog <log file> -? for list of options\n");
        return 1;
    }

    HANDLE file = CreateFile(argv[1], GENERIC_READ, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
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
    void* baseAddress = MapViewOfFileEx(map, FILE_MAP_READ, 0, 0, size, (void*)0x400000000000);
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
    wchar_t* largv[128];
    memset(largv, 0, sizeof(largv));
    while (true)
    {
        typedef int ProcessStresslog(void* baseAddress, int argc, wchar_t* argv[]);

        HMODULE plugin = LoadLibrary(L"StressLogPlugin.dll");

        if (plugin == nullptr)
        {
            printf("could not load StressLogPlugin.dll");
            return 1;
        }

        ProcessStresslog* processStressLog = (ProcessStresslog*)GetProcAddress(plugin, "ProcessStresslog");
        if (processStressLog == nullptr)
        {
            printf("could not find entry point ProcessStresslog in StressLogPlugin.dll");
            return 1;
        }

        int error = processStressLog(baseAddress, argc, argv);

        FreeLibrary(plugin);

        if (error != 0)
        {
            printf("error %d occurred\n", error);
        }

        bool runAgain = false;
        wchar_t s[1024];
        while (true)
        {
            printf("'q' to quit, 'r' to run again\n>");
            if (fgetws(s, 1023, stdin) == nullptr)
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
            int largc = ParseCommandLine(&s[1], largv, _countof(largv));
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
}
