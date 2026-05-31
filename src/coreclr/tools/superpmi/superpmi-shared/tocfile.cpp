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
    FILE* fpIndex = fopen(inputFileName, "rb");
    if (fpIndex == NULL)
    {
        LogError("Failed to open file '%s'. errno=%d", inputFileName, errno);
        return;
    }

    // Now read the index file
    struct
    {
        uint32_t sig;
        uint32_t count;
    } header;
    const char sig[] = "INDX";
    if (fread(&header, sizeof(header), 1, fpIndex) != 0 || memcmp(&header.sig, sig, sizeof(header.sig)) != 0)
    {
        fclose(fpIndex);
        LogWarning("The index file %s is invalid: it seems to be missing the starting sentinel/length", inputFileName);
        return;
    }

    this->m_tocCount = header.count;
    this->m_tocArray = new TOCElement[this->m_tocCount];

    // Read the whole array
    size_t read = fread(&this->m_tocArray[0], sizeof(TOCElement), this->m_tocCount, fpIndex);
    if (read != this->m_tocCount * sizeof(TOCElement))
    {
        fclose(fpIndex);
        this->Clear();
        LogWarning("The index file %s is invalid: it appears to be truncated.", inputFileName);
        return;
    }

    // Get the last 4 byte token
    uint32_t sentinel;
    read = fread(&sentinel, sizeof(sentinel), 1, fpIndex);
    if ((read != sizeof(sentinel)) || (sentinel != header.sig))
    {
        fclose(fpIndex);
        this->Clear();
        LogWarning("The index file %s is invalid: it appears to be missing the ending sentinel.", inputFileName);
        return;
    }

    fclose(fpIndex);

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
