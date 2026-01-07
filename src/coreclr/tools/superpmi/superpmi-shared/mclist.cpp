// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// MCList.h - MethodContext List utility class
//----------------------------------------------------------

#include "standardpch.h"
#include "mclist.h"
#include "logging.h"

bool MCList::processArgAsMCL(char* input, std::vector<int>& list)
{
    std::vector<int> l;

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
        unsigned rangeStart = 0;
        bool     inRange    = false;
        unsigned scratch    = 0;
        bool     foundDigit = false;

        char* tail = input + len;

        inRange    = false;
        rangeStart = 0;

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
                        l.push_back(i);
                }
                else
                {
                    rangeStart = scratch;
                    l.push_back(scratch);
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

        if (l.empty())
        {
            LogError("Didn't find a list!");
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
            if (!getLineData(input, l))
                return false;
            if (l.size() >= 0)
                goto checkMCL;
        }
        return false;
    }

checkMCL: // check that mcl list is increasing only
    if (l[0] != 1)
    {
        LogError("MCL list needs to start from 1!");
        return false;
    }
    for (size_t i = 1; i < l.size(); i++)
    {
        if (l[i - 1] >= l[i])
        {
            LogError("MCL list must be increasing.. found %d -> %d", l[i - 1], l[i]);
            return false;
        }
    }

    list = std::move(l);
    return true;
}

// Returns true on success, false on failure.
/* static */
bool MCList::getLineData(const char* nameOfInput, std::vector<int>& indexes)
{
    FILE* fp = fopen(nameOfInput, "r");
    if (fp == NULL)
    {
        LogError("Unable to open '%s'. errno=%d", nameOfInput, errno);
        return false;
    }

    int value;
    std::vector<int> l;
    while (fscanf(fp, "%d", &value) > 0)
    {
        l.push_back(value);
    }

    if (fclose(fp) != 0)
    {
        LogError("fclose failed. errno=%d", errno);
        return false;
    }

    indexes = std::move(l);
    return true;
}

void MCList::InitializeMCL(char* filename)
{
    fpMCLFile = fopen(filename, "w");
    if (fpMCLFile == NULL)
    {
        LogError("Failed to open output file '%s'. errno=%d", filename, errno);
    }
}

void MCList::AddMethodToMCL(int methodIndex)
{
    if (fpMCLFile != NULL)
    {
        if (fprintf(fpMCLFile, "%d\n", methodIndex) != 0)
        {
            LogError("Failed to write method index '%d'. errno=%d", methodIndex, errno);
        }
    }
}

void MCList::CloseMCL()
{
    if (fpMCLFile != NULL)
    {
        if (fclose(fpMCLFile) == 0)
        {
            LogError("fclose failed. errno=%d", errno);
        }
    }

    fpMCLFile = NULL;
}
