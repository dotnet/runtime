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
    HANDLE hIndex = CreateFileA(inputFileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
                                FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hIndex == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open file '%s'. GetLastError()=%u", inputFileName, GetLastError());
        return;
    }

    // Now read the index file
    LARGE_INTEGER val; // I'm abusing LARGE_INTEGER here...
    DWORD         read;
    if (!ReadFile(hIndex, &val, sizeof(val), &read, nullptr) || (val.u.LowPart != *(DWORD*)("INDX")))
    {
        CloseHandle(hIndex);
        LogWarning("The index file %s is invalid: it seems to be missing the starting sentinel/length", inputFileName);
        return;
    }

    this->m_tocCount = val.u.HighPart;
    this->m_tocArray = new TOCElement[this->m_tocCount];

    // Read the whole array
    if (!ReadFile(hIndex, &this->m_tocArray[0], (DWORD)(this->m_tocCount * sizeof(TOCElement)), &read, nullptr) ||
        (read != (DWORD)(this->m_tocCount * sizeof(TOCElement))))
    {
        CloseHandle(hIndex);
        this->Clear();
        LogWarning("The index file %s is invalid: it appears to be truncated.", inputFileName);
        return;
    }

    // Get the last 4 byte token (more abuse of LARGE_INTEGER)
    if (!ReadFile(hIndex, &val.u.HighPart, sizeof(DWORD), &read, nullptr) || (read != sizeof(DWORD)) ||
        (val.u.LowPart != (DWORD)val.u.HighPart))
    {
        CloseHandle(hIndex);
        this->Clear();
        LogWarning("The index file %s is invalid: it appears to be missing the ending sentinel.", inputFileName);
        return;
    }

    CloseHandle(hIndex);

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
