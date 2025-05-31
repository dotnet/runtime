// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// TOCFile.cpp - Abstraction for reading a TOC file
//----------------------------------------------------------

#include "standardpch.h"
#include "tocfile.h"
#include "logging.h"
#include <stdio.h>

// Tries to load a Table of Contents
void TOCFile::LoadToc(const char* inputFileName, bool validate)
{
    FILE* fpIndex = NULL;
    if (fopen_s(&fpIndex, inputFileName, "rb") != 0)
    {
        LogError("Failed to open file '%s'. errno=%d", inputFileName, errno);
        return;
    }

    // Now read the index file
    uint32_t header[2];
    size_t   read;
    if (fread(header, 1, sizeof(header), fpIndex) != 0 || header[0] != *(uint32_t*)("INDX"))
    {
        fclose(fpIndex);
        LogWarning("The index file %s is invalid: it seems to be missing the starting sentinel/length", inputFileName);
        return;
    }

    this->m_tocCount = header[1];
    this->m_tocArray = new TOCElement[this->m_tocCount];

    // Read the whole array
    if ((read = fread(this->m_tocArray, sizeof(TOCElement), this->m_tocCount, fpIndex) != 0) ||
        read != (this->m_tocCount * sizeof(TOCElement)))
    {
        fclose(fpIndex);
        this->Clear();
        LogWarning("The index file %s is invalid: it appears to be truncated.", inputFileName);
        return;
    }

    uint32_t token;

    // Get the last 4 byte token
    if ((read = fread(&token, sizeof(uint32_t), 1, fpIndex) != 0) || (read != sizeof(uint32_t) || (token != header[0])))
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
