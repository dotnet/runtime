// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// TOCFile.cpp - Abstraction for reading a TOC file
//----------------------------------------------------------

#include "standardpch.h"
#include "tocfile.h"
#include "logging.h"

// Tries to load a Table of Contents
void TOCFile::LoadToc(const char* inputFileName, bool validate)
{
    FILE* hIndex = fopen(inputFileName, "rb");
    if (hIndex == nullptr)
    {
        LogError("Failed to open file '%s'. errno=%d", inputFileName, errno);
        return;
    }

    // Now read the index file
    LARGE_INTEGER val; // I'm abusing LARGE_INTEGER here...
    if ((fread(&val, 1, sizeof(val), hIndex) != sizeof(val)) || (val.u.LowPart != *(DWORD*)("INDX")))
    {
        fclose(hIndex);
        LogWarning("The index file %s is invalid: it seems to be missing the starting sentinel/length", inputFileName);
        return;
    }

    this->m_tocCount = val.u.HighPart;
    this->m_tocArray = new TOCElement[this->m_tocCount];

    // Read the whole array
    if (fread(&this->m_tocArray[0], 1, this->m_tocCount * sizeof(TOCElement), hIndex) !=
        this->m_tocCount * sizeof(TOCElement))
    {
        fclose(hIndex);
        this->Clear();
        LogWarning("The index file %s is invalid: it appears to be truncated.", inputFileName);
        return;
    }

    // Get the last 4 byte token (more abuse of LARGE_INTEGER)
    if ((fread(&val.u.HighPart, 1, sizeof(DWORD), hIndex) != sizeof(DWORD)) ||
        (val.u.LowPart != (DWORD)val.u.HighPart))
    {
        fclose(hIndex);
        this->Clear();
        LogWarning("The index file %s is invalid: it appears to be missing the ending sentinel.", inputFileName);
        return;
    }

    fclose(hIndex);

    if (validate)
    {
        int lastNum = -1;

        // Quickly validate that the index is sorted
        for (size_t i = 0; i < this->m_tocCount; i++)
        {
            int nextNum = this->m_tocArray[i].Number;
            if (nextNum <= lastNum)
            {
                // It wasn't sorted: abort
                this->Clear();
                LogWarning("The index file %s is invalid: it is not sorted.", inputFileName);
                return;
            }
            lastNum = nextNum;
        }
    }
}
