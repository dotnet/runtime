// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbtoc.h"
#include "methodcontext.h"
#include "methodcontextreader.h"
#include "methodcontextiterator.h"
#include "simpletimer.h"

class TOCElementNode
{
public:
    TOCElementNode* Next;
    TOCElement      tocElement;

    TOCElementNode(int number, int64_t offset) : Next(nullptr), tocElement(number, offset)
    {
    }
};

int verbTOC::DoWork(const char* nameOfInput)
{
    LogVerbose("Indexing from '%s' into '%s.mct'", nameOfInput, nameOfInput);

    MethodContextIterator mci;
    if (!mci.Initialize(nameOfInput))
        return -1;

    int savedCount = 0;

    TOCElementNode* head    = nullptr;
    TOCElementNode* curElem = nullptr;

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();

        TOCElementNode* nxt = new TOCElementNode(mci.MethodContextNumber(), mci.CurrentPos());
        mc->dumpMethodHashToBuffer(nxt->tocElement.Hash, MM3_HASH_BUFFER_SIZE);

        if (curElem != nullptr)
        {
            curElem->Next = nxt;
        }
        else
        {
            head = nxt;
        }
        curElem = nxt;
        savedCount++;
    }

    std::string nameOfOutput = std::string(nameOfInput) + ".mct";

    FILE* fpOut;
    if (fopen_s(&fpOut, nameOfOutput.c_str(), "wb") != 0)
    {
        LogError("Failed to open input 1 '%s'. errno=%d", nameOfOutput.c_str(), errno);
        return -1;
    }

    uint32_t header[2];

    // Write out the signature "INDX" and then the element count
    header[0] = *(const uint32_t*)"INDX"; // cuz Type Safety is for languages that have good IO facilities
    header[1] = savedCount;
    size_t written = fwrite(header, 1, sizeof(header), fpOut);
    if (written != sizeof(header))
    {
        LogError("Failed to write index header. errno=%d", errno);
    }

    // Now just dump sizeof(TOCElement) byte chunks into the file.
    // I could probably do this more efficiently, but I don't think it matters
    DWORD chunkSize = sizeof(TOCElement);
    for (curElem = head; curElem != nullptr; curElem = curElem->Next)
    {
        written = fwrite(&curElem->tocElement, 1, chunkSize, fpOut);
        if (written != chunkSize)
        {
            LogError("Failed to write index element '%d'. errno=%d", curElem->tocElement.Number,
                     errno);
            return -1;
        }
    }
    // Now write out a final "INDX" to flag the end of the file...
    written = fwrite(header, 1, sizeof(uint32_t), fpOut);
    if ((written != sizeof(uint32_t)))
    {
        LogError("Failed to write index terminal. errno=%d", errno);
    }

    LogInfo("Loaded %d, added %d to Table of Contents", mci.MethodContextNumber(), savedCount);

    if (fclose(fpOut) != 0)
    {
        LogError("fclose failed. errno=%d", errno);
        return -1;
    }

    if (!mci.Destroy())
        return -1;

    return 0;
}
