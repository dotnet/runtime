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

    TOCElementNode(int number, int64_t offset) : Next(nullptr), tocElement(number, offset) {}
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

    size_t maxLen       = strlen(nameOfInput) + 5;
    char*  nameOfOutput = (char*)_alloca(maxLen);
    strcpy_s(nameOfOutput, maxLen, nameOfInput);
    strcat_s(nameOfOutput, maxLen, ".mct");
    FILE* fpOut = fopen(nameOfOutput, "wb");
    if (fpOut == NULL)
    {
        LogError("Failed to open input 1 '%s'. errno=%d", nameOfOutput, errno);
        return -1;
    }

    size_t written;
    // Write out the signature "INDX" and then the element count
    struct
    {
        uint32_t sig;
        uint32_t count;
    } token;
    const char sig[] = "INDX";
    (void)memcpy(&token.sig , sig, sizeof(token.sig));
    token.count = savedCount;
    if ((written = fwrite(&token, sizeof(token), 1, fpOut)) <= 0 || written != sizeof(token))
    {
        LogError("Failed to write index header. errno=%d", errno);
    }

    // Now just dump sizeof(TOCElement) byte chunks into the file.
    // I could probably do this more efficiently, but I don't think it matters
    for (curElem = head; curElem != nullptr; curElem = curElem->Next)
    {
        if ((written = fwrite(&curElem->tocElement, sizeof(TOCElement), 1, fpOut)) <= 0 || written != sizeof(TOCElement))
        {
            LogError("Failed to write index element '%d'. errno=%d", curElem->tocElement.Number,
                     errno);
            return -1;
        }
    }
    // Now write out a final "INDX" to flag the end of the file...
    if ((written = fwrite(&token.sig, sizeof(token.sig), 1, fpOut)) <= 0 ||
        (written != sizeof(token.sig)))
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
