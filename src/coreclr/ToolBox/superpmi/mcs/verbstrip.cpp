// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbstrip.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "errorhandling.h"
#include "methodcontextreader.h"
#include "methodcontextiterator.h"

// verbStrip::DoWork handles both "-copy" and "-strip". These both copy from input file to output file,
// but treat the passed-in indexes in opposite ways.
int verbStrip::DoWork(
    const char* nameOfInput, const char* nameOfOutput, int indexCount, const int* indexes, bool strip, bool stripCR)
{
    if (strip)
        return DoWorkTheOldWay(nameOfInput, nameOfOutput, indexCount, indexes, stripCR);
    SimpleTimer* st1 = new SimpleTimer();

    LogVerbose("Reading from '%s' removing Mc Indexes and writing into '%s'", nameOfInput, nameOfOutput);

    int            loadedCount = 0;
    MethodContext* mc          = nullptr;
    int            savedCount  = 0;
    int            index       = 0;

    // The method context reader handles skipping any unrequested method contexts
    // Used in conjunction with an MCI file, it does a lot less work...
    MethodContextReader* reader = new MethodContextReader(nameOfInput, indexes, indexCount);
    if (!reader->isValid())
    {
        return -1;
    }

    HANDLE hFileOut = CreateFileA(nameOfOutput, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                                  FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileOut == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open input 1 '%s'. GetLastError()=%u", nameOfOutput, GetLastError());
        return -1;
    }

    if (indexCount == -1)
        strip = true; // Copy command with no indexes listed should copy all the inputs...
    while (true)
    {
        MethodContextBuffer mcb = reader->GetNextMethodContext();
        if (mcb.Error())
        {
            return -1;
        }
        else if (mcb.allDone())
        {
            break;
        }

        loadedCount++;
        if ((loadedCount % 500 == 0) && (loadedCount > 0))
        {
            st1->Stop();
            LogVerbose("%2.1f%% - Loaded %d at %d per second", reader->PercentComplete(), loadedCount,
                       (int)((double)500 / st1->GetSeconds()));
            st1->Start();
        }

        if (!MethodContext::Initialize(loadedCount, mcb.buff, mcb.size, &mc))
            return -1;

        if (stripCR)
        {
            delete mc->cr;
            mc->cr = new CompileResult();
        }
        mc->saveToFile(hFileOut);
        savedCount++;
        delete mc;
    }
    if (CloseHandle(hFileOut) == 0)
    {
        LogError("2nd CloseHandle failed. GetLastError()=%u", GetLastError());
        return -1;
    }
    LogInfo("Loaded %d, Saved %d", loadedCount, savedCount);

    return 0;
}

// This is only used for "-strip".
int verbStrip::DoWorkTheOldWay(
    const char* nameOfInput, const char* nameOfOutput, int indexCount, const int* indexes, bool stripCR)
{
    LogVerbose("Reading from '%s' removing MC Indexes and writing into '%s'", nameOfInput, nameOfOutput);

    MethodContextIterator mci(true);
    if (!mci.Initialize(nameOfInput))
        return -1;

    int  savedCount = 0;
    bool write;
    int  index = 0; // Can't use MethodContextIterator indexing, since we want the opposite of that.

    HANDLE hFileOut = CreateFileA(nameOfOutput, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                                  FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileOut == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open input 1 '%s'. GetLastError()=%u", nameOfOutput, GetLastError());
        return -1;
    }

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();

        write = true; // assume we'll write it
        if (index < indexCount)
        {
            if (indexes[index] == mci.MethodContextNumber())
            {
                index++;
                write = false;
            }
        }

        if (write)
        {
            if (stripCR)
            {
                delete mc->cr;
                mc->cr = new CompileResult();
            }
            mc->saveToFile(hFileOut);
            savedCount++;
        }
    }

    if (CloseHandle(hFileOut) == 0)
    {
        LogError("2nd CloseHandle failed. GetLastError()=%u", GetLastError());
        return -1;
    }

    if (index < indexCount)
        LogWarning("Didn't use all of index count input %d < %d  (i.e. didn't see MC #%d)", index, indexCount,
                   indexes[index]);

    LogInfo("Loaded %d, Saved %d", mci.MethodContextNumber(), savedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
