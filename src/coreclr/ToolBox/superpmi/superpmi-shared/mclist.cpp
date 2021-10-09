// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// MCList.h - MethodContext List utility class
//----------------------------------------------------------

#include "standardpch.h"
#include "mclist.h"
#include "logging.h"

bool MCList::processArgAsMCL(char* input, int* count, int** list)
{
    // If it contains only '0-9', '-', ',' try to see it as a range list, else try to load as a file
    bool isRangeList = true;

    size_t len = strlen(input);

    for (unsigned int i = 0; (i < len) && isRangeList; i++)
    {
        if ((input[i] != '-') && (input[i] != ',') && (!isdigit((unsigned char)input[i])))
            isRangeList = false;
    }

    if (isRangeList)
    {
        // Count items
        *count              = 0;
        unsigned rangeStart = 0;
        bool     inRange    = false;
        unsigned scratch    = 0;
        bool     foundDigit = false;

        char* tail = input + len;

        for (char* head = input; head <= tail; head++)
        {
            scratch    = 0;
            foundDigit = false;
            while ((head <= tail) && (isdigit((unsigned char)*head)))
            {
                scratch    = (scratch * 10) + ((*head) - '0');
                foundDigit = true;
                head++;
            }
            if (foundDigit)
            {
                if (inRange)
                {
                    inRange = false;
                    if (rangeStart >= scratch)
                    {
                        LogError("Invalid range in '%s'", input);
                        return false;
                    }
                    (*count) += scratch - rangeStart;
                }
                else
                {
                    rangeStart = scratch;
                    (*count)++;
                }
            }
            if (*head == '-')
                inRange = true;
        }

        if (*count == 0)
        {
            LogError("Didn't find a list!");
            return false;
        }

        inRange    = false;
        rangeStart = 0;

        int* ll   = new int[*count];
        *list     = ll;
        int index = 0;
        ll[index] = 0;

        for (char* head = input; head <= tail; head++)
        {
            scratch    = 0;
            foundDigit = false;
            while ((head <= tail) && (isdigit((unsigned char)*head)))
            {
                scratch    = (scratch * 10) + ((*head) - '0');
                foundDigit = true;
                head++;
            }
            if (foundDigit)
            {
                if (inRange)
                {
                    inRange = false;
                    for (unsigned int i = rangeStart + 1; i <= scratch; i++)
                        ll[index++]     = i;
                }
                else
                {
                    rangeStart  = scratch;
                    ll[index++] = scratch;
                }
            }
            if (*head == '-')
                inRange = true;
        }
        if (inRange)
        {
            LogError("Found invalid external range in '%s'", input);
            return false;
        }
        goto checkMCL;
    }
    else
    {
        char* lastdot = strrchr(input, '.');
        if (lastdot != nullptr && _stricmp(lastdot, ".mcl") == 0)
        {
            // Read MCLFile
            if (!getLineData(input, count, list))
                return false;
            if (*count >= 0)
                goto checkMCL;
        }
        return false;
    }

checkMCL: // check that mcl list is increasing only
    int* ll = (*list);
    if (ll[0] == 0)
    {
        LogError("MCL list needs to start from 1!");
        return false;
    }
    for (int i = 1; i < *count; i++)
    {
        if (ll[i - 1] >= ll[i])
        {
            LogError("MCL list must be increasing.. found %d -> %d", ll[i - 1], ll[i]);
            return false;
        }
    }
    return true;
}

// Returns true on success, false on failure.
// On success, sets *pIndexCount to the number of indices read, and *pIndexes to a new array with all the indices read.
// The caller must free the memory with delete[].
/* static */
bool MCList::getLineData(const char* nameOfInput, /* OUT */ int* pIndexCount, /* OUT */ int** pIndexes)
{
    HANDLE hFile = CreateFileA(nameOfInput, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
                               FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        LogError("Unable to open '%s'. GetLastError()=%u", nameOfInput, GetLastError());
        return false;
    }
    LARGE_INTEGER DataTemp;
    if (!GetFileSizeEx(hFile, &DataTemp))
    {
        LogError("GetFileSizeEx failed. GetLastError()=%u", GetLastError());
        return false;
    }

    if (DataTemp.QuadPart > MAXMCLFILESIZE)
    {
        LogError("Size %d exceeds max size of %d", DataTemp.QuadPart, MAXMCLFILESIZE);
        return false;
    }

    int   sz   = DataTemp.u.LowPart;
    char* buff = new char[sz];
    DWORD bytesRead;
    if (ReadFile(hFile, buff, sz, &bytesRead, nullptr) == 0)
    {
        LogError("ReadFile failed. GetLastError()=%u", GetLastError());
        delete[] buff;
        return false;
    }
    if (!CloseHandle(hFile))
    {
        LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
        delete[] buff;
        return false;
    }

    // Count the lines. Note that the last line better be terminated by a newline.
    int lineCount = 0;
    for (int i = 0; i < sz; i++)
    {
        if (buff[i] == '\n')
        {
            lineCount++;
        }
    }

    int* indexes    = new int[lineCount];
    int  indexCount = 0;
    int  i          = 0;
    while (i < sz)
    {
        // seek the first number on the line. This will skip empty lines and lines with no digits.
        while (!isdigit((unsigned char)buff[i]))
            i++;
        // read in the number
        indexes[indexCount++] = atoi(&buff[i]);
        // seek to the start of next line
        while ((i < sz) && (buff[i] != '\n'))
            i++;
        i++;
    }
    delete[] buff;

    *pIndexCount = indexCount;
    *pIndexes    = indexes;
    return true;
}

void MCList::InitializeMCL(char* filename)
{
    hMCLFile = CreateFileA(filename, GENERIC_WRITE, FILE_SHARE_WRITE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hMCLFile == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open output file '%s'. GetLastError()=%u", filename, GetLastError());
    }
}

void MCList::AddMethodToMCL(int methodIndex)
{
    if (hMCLFile != INVALID_HANDLE_VALUE)
    {
        char  strMethodIndex[12];
        DWORD charCount    = 0;
        DWORD bytesWritten = 0;

        charCount = sprintf_s(strMethodIndex, sizeof(strMethodIndex), "%d\r\n", methodIndex);

        if (!WriteFile(hMCLFile, strMethodIndex, charCount, &bytesWritten, nullptr) || bytesWritten != charCount)
        {
            LogError("Failed to write method index '%d'. GetLastError()=%u", strMethodIndex, GetLastError());
        }
    }
}

void MCList::CloseMCL()
{
    if (hMCLFile != INVALID_HANDLE_VALUE)
    {
        if (CloseHandle(hMCLFile) == 0)
        {
            LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
        }
    }
}
