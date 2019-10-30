//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "verbremovedup.h"
#include "simpletimer.h"
#include "lightweightmap.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"

// We use a hash to limit the number of comparisons we need to do.
// The first level key to our hash map is ILCodeSize and the second
// level map key is just an index and the value is an existing MC Hash.

LightWeightMap<int, DenseLightWeightMap<char*>*>* inFile = nullptr;

bool unique(MethodContext* mc)
{
    if (inFile == nullptr)
        inFile = new LightWeightMap<int, DenseLightWeightMap<char*>*>();

    CORINFO_METHOD_INFO newInfo;
    unsigned            newFlags = 0;
    mc->repCompileMethod(&newInfo, &newFlags);

    char* md5Buff = new char[MD5_HASH_BUFFER_SIZE];
    mc->dumpMethodMD5HashToBuffer(md5Buff, MD5_HASH_BUFFER_SIZE);

    if (inFile->GetIndex(newInfo.ILCodeSize) == -1)
        inFile->Add(newInfo.ILCodeSize, new DenseLightWeightMap<char*>());

    DenseLightWeightMap<char*>* ourRank = inFile->Get(newInfo.ILCodeSize);

    for (int i = 0; i < (int)ourRank->GetCount(); i++)
    {
        char* md5Buff2 = ourRank->Get(i);

        if (strncmp(md5Buff, md5Buff2, MD5_HASH_BUFFER_SIZE) == 0)
        {
            delete[] md5Buff;
            return false;
        }
    }

    ourRank->Append(md5Buff);
    return true;
}

LightWeightMap<int, DenseLightWeightMap<MethodContext*>*>* inFileLegacy = nullptr;

bool uniqueLegacy(MethodContext* mc)
{
    if (inFileLegacy == nullptr)
        inFileLegacy = new LightWeightMap<int, DenseLightWeightMap<MethodContext*>*>();

    CORINFO_METHOD_INFO newInfo;
    unsigned            newFlags = 0;
    mc->repCompileMethod(&newInfo, &newFlags);

    if (inFileLegacy->GetIndex(newInfo.ILCodeSize) == -1)
        inFileLegacy->Add(newInfo.ILCodeSize, new DenseLightWeightMap<MethodContext*>());

    DenseLightWeightMap<MethodContext*>* ourRank = inFileLegacy->Get(newInfo.ILCodeSize);

    for (int i = 0; i < (int)ourRank->GetCount(); i++)
    {
        MethodContext* scratch = ourRank->Get(i);
        if (mc->Equal(scratch))
        {
            return false;
        }
    }

    // We store the MethodContext in our map.
    ourRank->Append(mc);
    return true;
}

int verbRemoveDup::DoWork(const char* nameOfInput, const char* nameOfOutput, bool stripCR, bool legacyCompare)
{
    LogVerbose("Removing duplicates from '%s', writing to '%s'", nameOfInput, nameOfOutput);

    MethodContextIterator mci(true);
    if (!mci.Initialize(nameOfInput))
        return -1;

    int savedCount = 0;

    HANDLE hFileOut = CreateFileA(nameOfOutput, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
                                  FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFileOut == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open output '%s'. GetLastError()=%u", nameOfOutput, GetLastError());
        return -1;
    }

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.CurrentTakeOwnership();
        if (stripCR)
        {
            delete mc->cr;
            mc->cr = new CompileResult();
        }
        if (legacyCompare)
        {
            if (uniqueLegacy(mc))
            {
                mc->saveToFile(hFileOut);
                savedCount++;

                // In this case, for the legacy comparer, it has placed the 'mc' in the 'inFileLegacy' table, so we
                // can't delete it.
            }
            else
            {
                delete mc; // we no longer need this
            }
        }
        else
        {
            if (unique(mc))
            {
                mc->saveToFile(hFileOut);
                savedCount++;
            }
            delete mc; // we no longer need this
        }
    }

    // We're leaking 'inFile' or 'inFileLegacy', but the process is going away, so it shouldn't matter.

    if (CloseHandle(hFileOut) == 0)
    {
        LogError("2nd CloseHandle failed. GetLastError()=%u", GetLastError());
        return -1;
    }

    LogInfo("Loaded %d, Saved %d", mci.MethodContextNumber(), savedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
