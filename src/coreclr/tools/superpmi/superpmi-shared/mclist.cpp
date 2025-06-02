// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// MCList.h - MethodContext List utility class
//----------------------------------------------------------

#include "standardpch.h"
#include "mclist.h"
#include "logging.h"
#include <fstream>
#include <sstream>

bool MCList::processArgAsMCL(char* input, int* count, int** list)
{
    // If it contains only '0-9', '-', ',' try to see it as a range list, else try to load as a file
    bool isRangeList = true;

    size_t len = strlen(input);

    for (size_t i = 0; (i < len) && isRangeList; i++)
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
    std::ifstream    fs(nameOfInput);
    std::vector<int> indexes;
    std::string      line;

    while (std::getline(fs, line))
    {
        int n;
        // This will skip empty lines and lines with no digits.
        if (std::istringstream(std::move(line)) >> n)
        {
            indexes.push_back(n);
        }
    }

    *pIndexCount = (int)indexes.size();
    *pIndexes    = new int[indexes.size()];
    std::copy(indexes.begin(), indexes.end(), *pIndexes);
    return true;
}

void MCList::InitializeMCL(char* filename)
{
    if (fopen_s(&fpMCLFile, filename, "w") != 0)
    {
        LogError("Failed to open output file '%s'. errno=%d", filename, errno);
    }
}

void MCList::AddMethodToMCL(int methodIndex)
{
    if (fpMCLFile != NULL)
    {
        if (fprintf(fpMCLFile, "%d\r\n", methodIndex) <= 0)
        {
            LogError("Failed to write method index '%d'. errno=%d", methodIndex, errno);
        }
    }
}

void MCList::CloseMCL()
{
    if (fpMCLFile != NULL)
    {
        if (fclose(fpMCLFile) != 0)
        {
            LogError("fclose failed. errno=%d", errno);
        }
    }
}
