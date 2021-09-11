// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// MethodContextReader.h - Abstraction for reading MethodContexts
//                         Should eventually support multithreading
//----------------------------------------------------------
#ifndef _MethodContextReader
#define _MethodContextReader

#include "methodcontext.h"
#include "tocfile.h"

struct MethodContextReadResult
{
private:
    enum class State
    {
        Blob,
        Error,
        Done,
    };

    State state;
public:
    int error;
    MethodContextBlobInfo info;
    unsigned char* blob;

    MethodContextReadResult()
        : state(State::Error)
        , error(0)
        , info(0, 0, MethodContextCompression::None)
        , blob(nullptr)
    {
    }

    bool IsAllDone()
    {
        return state == State::Done;
    }
    bool IsError()
    {
        return state == State::Error;
    }

    static MethodContextReadResult Done()
    {
        MethodContextReadResult res;
        res.state = State::Done;
        return res;
    }

    static MethodContextReadResult Error(int error)
    {
        MethodContextReadResult res;
        res.state = State::Error;
        res.error = error;
        return res;
    }

    static MethodContextReadResult Blob(const MethodContextBlobInfo& info, unsigned char* blob)
    {
        MethodContextReadResult res;
        res.state = State::Blob;
        res.info = info;
        res.blob = blob;
        return res;
    }
};

// The pack(4) directive is so that each entry is 12 bytes, intead of 16
#pragma pack(push)
#pragma pack(4)
class MethodContextReader
{
private:
    // The MC/MCH file
    HANDLE fileHandle;

    // The size of the MC/MCH file
    __int64 fileSize;

    // Current MC index in the input MC/MCH file
    int curMCIndex;

    // The synchronization mutex
    HANDLE mutex;
    bool   AcquireLock();
    void   ReleaseLock();

    TOCFile tocFile;

    // Method ranges to process
    // If you have an index file, these things get processed
    // much faster, now
    const int* Indexes;
    int        IndexCount;
    int        curIndexPos;

    // Method hash to process
    // If you have an index file, these things get processed
    // much faster, now
    char* Hash;
    int   curTOCIndex;

    // Offset/increment if running in parallel mode
    // If you have an index file, these things get processed
    // much faster, now
    int Offset;
    int Increment;

    struct StringList
    {
        StringList* next;
        std::string hash;
    };
    StringList* excludedMethodsList;

    // Binary search to get this method number from the index
    // Returns -1 for not found, or -2 for not indexed
    __int64 GetOffset(unsigned int methodNumber);

    // Just a helper...
    static HANDLE OpenFile(const char* inputFile, DWORD flags = FILE_ATTRIBUTE_NORMAL);

    MethodContextReadResult ReadMethodContextNoLock(bool justSkip = false);
    MethodContextReadResult ReadMethodContext(bool acquireLock, bool justSkip = false);
    MethodContextReadResult GetSpecificMethodContext(unsigned int methodNumber);

    MethodContextReadResult GetNextMethodContextFromIndexes();
    MethodContextReadResult GetNextMethodContextFromHash();
    MethodContextReadResult GetNextMethodContextFromOffsetIncrement();
    MethodContextReadResult GetNextMethodContextHelper();

    // Looks for a file named foo.origSuffix.newSuffix or foo.newSuffix
    // but only if foo.origSuffix exists
    static std::string CheckForPairedFile(const std::string& fileName,
                                          const std::string& origSuffix,
                                          const std::string& newSuffix);

    // are we're at the end of the file...
    bool atEof();

    // Do we have a valid TOC?
    bool hasTOC();

    // Do we have a valid index?
    bool hasIndex();

    void ReadExcludedMethods(std::string mchFileName);
    void CleanExcludedMethods();

public:
    MethodContextReader(const char* inputFileName,
                        const int*  indexes    = nullptr,
                        int         indexCount = -1,
                        char*       hash       = nullptr,
                        int         offset     = -1,
                        int         increment  = -1);
    ~MethodContextReader();

    // Read a method context buffer from the ContextCollection
    // (either a hive [single] or an index)
    MethodContextReadResult GetNextMethodContext();
    // No C++ exceptions, so the constructor has to always succeed...
    bool   isValid();
    double PercentComplete();

    // Returns the index of the last MethodContext read by GetNextMethodContext
    inline int GetMethodContextIndex()
    {
        return curMCIndex;
    }

    // Return should this method context be excluded from the replay or not.
    bool IsMethodExcluded(MethodContext* mc);
};
#pragma pack(pop)

#endif
