// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbremovedup.h"
#include "simpletimer.h"
#include "lightweightmap.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "removedup.h"

RemoveDup::~RemoveDup()
{
    if (m_cleanup)
    {
        if (m_inFile != nullptr)
        {
            for (int i = 0; i < (int)m_inFile->GetCount(); i++)
            {
                DenseLightWeightMap<char*>* md5HashMap = m_inFile->GetItem(i);
                if (md5HashMap != nullptr)
                {
                    // go through and delete items
                    for (int j = 0; j < (int)md5HashMap->GetCount(); j++)
                    {
                        char* p = md5HashMap->GetItem(j);
                        delete[] p;
                    }
                    delete md5HashMap;
                }
            }
            delete m_inFile;
            m_inFile = nullptr;
        }
        if (m_inFileLegacy != nullptr)
        {
            for (int i = 0; i < (int)m_inFileLegacy->GetCount(); i++)
            {
                DenseLightWeightMap<MethodContext*>* md5HashMap = m_inFileLegacy->GetItem(i);
                if (md5HashMap != nullptr)
                {
                    // go through and delete items
                    for (int j = 0; j < (int)md5HashMap->GetCount(); j++)
                    {
                        MethodContext* p = md5HashMap->GetItem(j);
                        delete p;
                    }
                    delete md5HashMap;
                }
            }
            delete m_inFileLegacy;
            m_inFileLegacy = nullptr;
        }
    }
}

bool RemoveDup::unique(MethodContext* mc)
{
    if (m_inFile == nullptr)
        m_inFile = new LightWeightMap<int, DenseLightWeightMap<char*>*>();

    CORINFO_METHOD_INFO newInfo;
    unsigned            newFlags = 0;
    mc->repCompileMethod(&newInfo, &newFlags);

    // Assume that there are lots of duplicates, so don't allocate a new buffer for the MD5 hash data
    // until we know we're going to add it to the map.
    char md5Buff[MD5_HASH_BUFFER_SIZE];
    mc->dumpMethodMD5HashToBuffer(md5Buff, MD5_HASH_BUFFER_SIZE, /* ignoreMethodName */ true, &newInfo, newFlags);

    if (m_inFile->GetIndex(newInfo.ILCodeSize) == -1)
        m_inFile->Add(newInfo.ILCodeSize, new DenseLightWeightMap<char*>());

    DenseLightWeightMap<char*>* ourRank = m_inFile->Get(newInfo.ILCodeSize);

    for (unsigned i = 0; i < ourRank->GetCount(); i++)
    {
        char* md5Buff2 = ourRank->Get(i);
        if (strncmp(md5Buff, md5Buff2, MD5_HASH_BUFFER_SIZE) == 0)
        {
            return false;
        }
    }

    char* newmd5Buff = new char[MD5_HASH_BUFFER_SIZE];
    memcpy(newmd5Buff, md5Buff, MD5_HASH_BUFFER_SIZE);
    ourRank->Append(newmd5Buff);
    return true;
}

bool RemoveDup::uniqueLegacy(MethodContext* mc)
{
    if (m_inFileLegacy == nullptr)
        m_inFileLegacy = new LightWeightMap<int, DenseLightWeightMap<MethodContext*>*>();

    CORINFO_METHOD_INFO newInfo;
    unsigned            newFlags = 0;
    mc->repCompileMethod(&newInfo, &newFlags);

    if (m_inFileLegacy->GetIndex(newInfo.ILCodeSize) == -1)
        m_inFileLegacy->Add(newInfo.ILCodeSize, new DenseLightWeightMap<MethodContext*>());

    DenseLightWeightMap<MethodContext*>* ourRank = m_inFileLegacy->Get(newInfo.ILCodeSize);

    for (unsigned i = 0; i < ourRank->GetCount(); i++)
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

bool RemoveDup::CopyAndRemoveDups(const char* nameOfInput, HANDLE hFileOut)
{
    MethodContextIterator mci(/* progressReport */ true);
    if (!mci.Initialize(nameOfInput))
        return false;

    int savedCount = 0;

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.CurrentTakeOwnership();
        if (m_stripCR)
        {
            delete mc->cr;
            mc->cr = new CompileResult();
        }
        if (m_legacyCompare)
        {
            if (uniqueLegacy(mc))
            {
                mc->saveToFile(hFileOut);
                savedCount++;

                // In this case, for the legacy comparer, it has placed the 'mc' in the 'm_inFileLegacy' table, so we
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

    LogInfo("Loaded %d, Saved %d", mci.MethodContextNumber(), savedCount);

    if (!mci.Destroy())
        return false;

    return true;
}
