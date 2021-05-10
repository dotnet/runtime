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

    TOCElementNode(int number, __int64 offset) : Next(nullptr), tocElement(number, offset)
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
        mc->dumpMethodMD5HashToBuffer(nxt->tocElement.Hash, MD5_HASH_BUFFER_SIZE);

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
    HANDLE hFileOut =
        CreateFileA(nameOfOutput, GENERIC_WRITE, FILE_SHARE_WRITE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFileOut == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open input 1 '%s'. GetLastError()=%u", nameOfOutput, GetLastError());
        return -1;
    }

    DWORD written;
    // Write out the signature "INDX" and then the element count
    LARGE_INTEGER token;
    token.u.LowPart  = *(const int*)"INDX"; // cuz Type Safety is for languages that have good IO facilities
    token.u.HighPart = savedCount;
    if (!WriteFile(hFileOut, &token, sizeof(token), &written, nullptr) || written != sizeof(token))
    {
        LogError("Failed to write index header. GetLastError()=%u", GetLastError());
    }

    // Now just dump sizeof(TOCElement) byte chunks into the file.
    // I could probably do this more efficiently, but I don't think it matters
    DWORD chunkSize = sizeof(TOCElement);
    for (curElem = head; curElem != nullptr; curElem = curElem->Next)
    {
        if (!WriteFile(hFileOut, &curElem->tocElement, chunkSize, &written, nullptr) || written != chunkSize)
        {
            LogError("Failed to write index element '%d'. GetLastError()=%u", curElem->tocElement.Number,
                     GetLastError());
            return -1;
        }
    }
    // Now write out a final "INDX" to flag the end of the file...
    if (!WriteFile(hFileOut, &token.u.LowPart, sizeof(token.u.LowPart), &written, nullptr) ||
        (written != sizeof(token.u.LowPart)))
    {
        LogError("Failed to write index terminal. GetLastError()=%u", GetLastError());
    }

    LogInfo("Loaded %d, added %d to Table of Contents", mci.MethodContextNumber(), savedCount);

    if (CloseHandle(hFileOut) == 0)
    {
        LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
        return -1;
    }

    if (!mci.Destroy())
        return -1;

    return 0;
}
