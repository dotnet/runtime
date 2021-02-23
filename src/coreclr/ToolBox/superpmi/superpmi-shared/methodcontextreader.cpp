// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// MethodContextReader.cpp - Abstraction for reading MethodContexts
//                         Should eventually support multithreading
//----------------------------------------------------------

#include "standardpch.h"
#include "tocfile.h"
#include "methodcontextreader.h"
#include "methodcontext.h"
#include "logging.h"
#include "runtimedetails.h"

// Just a helper...
HANDLE MethodContextReader::OpenFile(const char* inputFile, DWORD flags)
{
    HANDLE fileHandle =
        CreateFileA(inputFile, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | flags, NULL);
    if (fileHandle == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open file '%s'. GetLastError()=%u", inputFile, GetLastError());
    }
    return fileHandle;
}

static std::string to_lower(const std::string& input)
{
    std::string res = input;
    std::transform(input.cbegin(), input.cend(), res.begin(), tolower);
    return res;
}

// Looks for a file named foo.origSuffix.newSuffix or foo.newSuffix
// but only if foo.origSuffix exists.
//
// Note: filename extensions must be lower-case, even on case-sensitive file systems!
std::string MethodContextReader::CheckForPairedFile(const std::string& fileName,
                                                    const std::string& origSuffix,
                                                    const std::string& newSuffix)
{
    std::string tmp = to_lower(origSuffix);

    // First, check to see if foo.origSuffix exists
    size_t suffix_offset = fileName.find_last_of('.');
    if ((SSIZE_T)suffix_offset <= 0 || (tmp != to_lower(fileName.substr(suffix_offset))) ||
        (GetFileAttributesA(fileName.c_str()) == INVALID_FILE_ATTRIBUTES))
        return std::string();

    // next, check foo.orig.new from foo.orig
    tmp = fileName + newSuffix;
    if (GetFileAttributesA(tmp.c_str()) != INVALID_FILE_ATTRIBUTES)
        return tmp;

    // Now let's check for foo.new from foo.new.orig
    tmp = fileName.substr(0, suffix_offset);
    if (GetFileAttributesA(tmp.c_str()) != INVALID_FILE_ATTRIBUTES)
        return tmp;

    // Finally, lets try foo.orig from foo.new
    tmp += newSuffix;
    if (GetFileAttributesA(tmp.c_str()) != INVALID_FILE_ATTRIBUTES)
        return tmp;

    return std::string();
}

MethodContextReader::MethodContextReader(
    const char* inputFileName, const int* indexes, int indexCount, char* hash, int offset, int increment)
    : fileHandle(INVALID_HANDLE_VALUE)
    , fileSize(0)
    , curMCIndex(0)
    , Indexes(indexes)
    , IndexCount(indexCount)
    , curIndexPos(0)
    , Hash(hash)
    , curTOCIndex(0)
    , Offset(offset)
    , Increment(increment)
{
    this->mutex = CreateMutexW(NULL, FALSE, nullptr);

    std::string tocFileName, mchFileName;

    // First, check to see if they passed an MCH file (look for a paired MCT file)
    tocFileName = MethodContextReader::CheckForPairedFile(inputFileName, ".mch", ".mct");
    if (!tocFileName.empty())
    {
        mchFileName = inputFileName;
    }
    else
    {
        // Okay, it wasn't an MCH file, let's check to see if it was an MCT file
        // so check for a paired MCH file instead
        mchFileName = MethodContextReader::CheckForPairedFile(inputFileName, ".mct", ".mch");
        if (!mchFileName.empty())
        {
            tocFileName = inputFileName;
        }
        else
        {
            mchFileName = inputFileName;
        }
    }

    if (!tocFileName.empty())
        this->tocFile.LoadToc(tocFileName.c_str());

    // we'll get here even if we don't have a valid index file
    this->fileHandle = OpenFile(mchFileName.c_str(), (this->hasTOC() && this->hasIndex()) ? FILE_ATTRIBUTE_NORMAL
                                                                                          : FILE_FLAG_SEQUENTIAL_SCAN);
    if (this->fileHandle != INVALID_HANDLE_VALUE)
    {
        GetFileSizeEx(this->fileHandle, (PLARGE_INTEGER) & this->fileSize);
    }

    ReadExcludedMethods(mchFileName);
}

MethodContextReader::~MethodContextReader()
{
    if (fileHandle != INVALID_HANDLE_VALUE)
    {
        CloseHandle(this->fileHandle);
    }

    CloseHandle(this->mutex);

    CleanExcludedMethods();
}

bool MethodContextReader::AcquireLock()
{
    DWORD res = WaitForSingleObject(this->mutex, INFINITE);
    return (res == WAIT_OBJECT_0);
}

void MethodContextReader::ReleaseLock()
{
    ReleaseMutex(this->mutex);
}

bool MethodContextReader::atEof()
{
    __int64 pos = 0;
    SetFilePointerEx(this->fileHandle, *(PLARGE_INTEGER)&pos, (PLARGE_INTEGER)&pos,
                     FILE_CURRENT); // LARGE_INTEGER is a crime against humanity
    return pos == this->fileSize;
}

MethodContextBuffer MethodContextReader::ReadMethodContextNoLock(bool justSkip)
{
    DWORD        bytesRead;
    char         buff[512];
    unsigned int totalLen = 0;
    if (atEof())
    {
        return MethodContextBuffer();
    }
    Assert(ReadFile(this->fileHandle, buff, 2 + sizeof(unsigned int), &bytesRead, NULL) == TRUE);
    AssertMsg((buff[0] == 'm') && (buff[1] == 'c'), "Didn't find magic number");
    memcpy(&totalLen, &buff[2], sizeof(unsigned int));
    if (justSkip)
    {
        __int64 pos = totalLen + 2;
        // Just move the file pointer ahead the correct number of bytes
        AssertMsg(SetFilePointerEx(this->fileHandle, *(PLARGE_INTEGER)&pos, (PLARGE_INTEGER)&pos, FILE_CURRENT) == TRUE,
                  "SetFilePointerEx failed (Error %X)", GetLastError());

        // Increment curMCIndex as we advanced the file pointer by another MC
        ++curMCIndex;

        return MethodContextBuffer(0);
    }
    else
    {
        unsigned char* buff2 = new unsigned char[totalLen + 2]; // total + End Canary
        Assert(ReadFile(this->fileHandle, buff2, totalLen + 2, &bytesRead, NULL) == TRUE);

        // Increment curMCIndex as we read another MC
        ++curMCIndex;

        return MethodContextBuffer(buff2, totalLen);
    }
}

MethodContextBuffer MethodContextReader::ReadMethodContext(bool acquireLock, bool justSkip)
{
    if (acquireLock && !this->AcquireLock())
    {
        LogError("Can't acquire the reader lock!");
        return MethodContextBuffer(-1);
    }

    struct Param
    {
        MethodContextReader* pThis;
        MethodContextBuffer  ret;
        bool                 justSkip;
    } param;
    param.pThis    = this;
    param.ret      = MethodContextBuffer(-2);
    param.justSkip = justSkip;

    PAL_TRY(Param*, pParam, &param)
    {
        pParam->ret = pParam->pThis->ReadMethodContextNoLock(pParam->justSkip);
    }
    PAL_FINALLY
    {
        this->ReleaseLock();
    }
    PAL_ENDTRY

    return param.ret;
}

// Read a method context buffer from the ContextCollection
// (either a hive [single] or an index)
MethodContextBuffer MethodContextReader::GetNextMethodContext()
{
    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        MethodContextReader* pThis;
        MethodContextBuffer  mcb;
    } param;
    param.pThis = this;

    PAL_TRY(Param*, pParam, &param)
    {
        pParam->mcb = pParam->pThis->GetNextMethodContextHelper();
    }
    PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CaptureExceptionAndStop)
    {
        LogError("Method %d is of low integrity.", GetMethodContextIndex());
        param.mcb = MethodContextBuffer(-1);
    }
    PAL_ENDTRY

    return param.mcb;
}

MethodContextBuffer MethodContextReader::GetNextMethodContextHelper()
{
    // If we have an offset/increment combo
    if (this->Offset > 0 && this->Increment > 0)
        return GetNextMethodContextFromOffsetIncrement();

    // If we have an index
    if (this->hasIndex())
    {
        if (this->curIndexPos < this->IndexCount)
        {
            // If we are not done with all of them
            return GetNextMethodContextFromIndexes();
        }
        else // We are done with all of them, return
            return MethodContextBuffer();
    }

    // If we have a hash
    if (this->Hash != nullptr)
        return GetNextMethodContextFromHash();

    // If we don't have any of these options return all MCs one by one
    return this->ReadMethodContext(true);
}

// Read a method context buffer from the ContextCollection using Indexes
MethodContextBuffer MethodContextReader::GetNextMethodContextFromIndexes()
{
    // Assert if we don't have an Index or we are done with all the indexes
    Assert(this->hasIndex() && this->curIndexPos < this->IndexCount);

    if (this->hasTOC())
    {
        // If we have an index & we have a TOC, we can just jump to that method!
        return this->GetSpecificMethodContext(this->Indexes[this->curIndexPos++]);
    }
    else
    {
        // Find the current method (either #0, or the previous index)
        int curMethod = this->curIndexPos ? this->Indexes[this->curIndexPos - 1] : 0;
        // Get the next method
        int nextMethod = this->Indexes[this->curIndexPos++];
        // Skip over methods until we get to the right now
        while (++curMethod < nextMethod)
        {
            // Skip a method context
            MethodContextBuffer mcb = this->ReadMethodContext(true, true);
            if (mcb.allDone() || mcb.Error())
                return mcb;
        }
    }
    return this->ReadMethodContext(true);
}

// Read a method context buffer from the ContextCollection using Hash
MethodContextBuffer MethodContextReader::GetNextMethodContextFromHash()
{
    // Assert if we don't have a valid hash
    Assert(this->Hash != nullptr);

    if (this->hasTOC())
    {
        // We have a TOC so lets go through the TOCElements
        // one-by-one till we find a matching hash
        for (; curTOCIndex < (int)this->tocFile.GetTocCount(); curTOCIndex++)
        {
            if (_strnicmp(this->Hash, this->tocFile.GetElementPtr(curTOCIndex)->Hash, MD5_HASH_BUFFER_SIZE) == 0)
            {
                // We found a match, return this specific method
                return this->GetSpecificMethodContext(this->tocFile.GetElementPtr(curTOCIndex++)->Number);
            }
        }

        // No more matches in the TOC for our hash value
        return MethodContextBuffer();
    }
    else
    {
        // Keep reading all MCs until we hit a match
        // or we reach the end or hit an error
        while (true)
        {
            // Read a method context
            // we can't skip because we need to calculate hashes
            MethodContextBuffer mcb = this->ReadMethodContext(true, false);
            if (mcb.allDone() || mcb.Error())
                return mcb;

            char mcHash[MD5_HASH_BUFFER_SIZE];

            // Create a temporary copy of mcb.buff plus ending 2-byte canary
            // this will get freed up by MethodContext constructor
            unsigned char* buff = new unsigned char[mcb.size + 2];
            memcpy(buff, mcb.buff, mcb.size + 2);

            MethodContext* mc;

            if (!MethodContext::Initialize(-1, buff, mcb.size, &mc))
                return MethodContextBuffer(-1);

            mc->dumpMethodMD5HashToBuffer(mcHash, MD5_HASH_BUFFER_SIZE);
            delete mc;

            if (_strnicmp(this->Hash, mcHash, MD5_HASH_BUFFER_SIZE) == 0)
            {
                // We found a match, return this specific method
                return mcb;
            }
        }
    }

    // We should never get here under normal conditions
    AssertMsg(true, "Unexpected condition hit while reading input file.");
    return MethodContextBuffer(-1);
}

// Read a method context buffer from the ContextCollection using offset/increment
MethodContextBuffer MethodContextReader::GetNextMethodContextFromOffsetIncrement()
{
    // Assert if we don't have a valid increment/offset combo
    Assert(this->Offset > 0 && this->Increment > 0);

    int methodNumber = this->curMCIndex > 0 ? this->curMCIndex + this->Increment : this->Offset;

    if (this->hasTOC())
    {
        // Check if we are within the TOC
        if ((int)this->tocFile.GetTocCount() >= methodNumber)
        {
            // We have a TOC so we can request a specific method context
            return this->GetSpecificMethodContext(methodNumber);
        }
        else
            return MethodContextBuffer();
    }
    else
    {
        // Keep skipping MCs until we get to the one we need to return
        while (this->curMCIndex + 1 < methodNumber)
        {
            // skip over a method
            MethodContextBuffer mcb = this->ReadMethodContext(true, true);
            if (mcb.allDone() || mcb.Error())
                return mcb;
        }
    }
    return this->ReadMethodContext(true);
}

bool MethodContextReader::hasIndex()
{
    return this->IndexCount > 0;
}

bool MethodContextReader::hasTOC()
{
    return this->tocFile.GetTocCount() > 0;
}

bool MethodContextReader::isValid()
{
    return this->fileHandle != INVALID_HANDLE_VALUE && this->mutex != INVALID_HANDLE_VALUE;
}

double MethodContextReader::PercentComplete()
{
    if (this->hasIndex() && this->hasTOC())
    {
        // Best estimate I can come up with...
        return 100.0 * (double)this->curIndexPos / (double)this->IndexCount;
    }
    this->AcquireLock();
    __int64 pos = 0;
    SetFilePointerEx(this->fileHandle, *(PLARGE_INTEGER)&pos, (PLARGE_INTEGER)&pos, FILE_CURRENT);
    this->ReleaseLock();
    return 100.0 * (double)pos / (double)this->fileSize;
}

// Binary search to get this method number from the index
// Returns -1 for not found, or -2 for not indexed
// Interview question alert: hurray for CLR headers incompatibility with STL :-(
// Note that TOC is 0 based and MC# are 1 based!
__int64 MethodContextReader::GetOffset(unsigned int methodNumber)
{
    if (!this->hasTOC())
        return -2;
    size_t high = this->tocFile.GetTocCount() - 1;
    size_t low  = 0;
    while (low <= high)
    {
        size_t       pos = (high + low) / 2;
        unsigned int num = this->tocFile.GetElementPtr(pos)->Number;
        if (num == methodNumber)
            return this->tocFile.GetElementPtr(pos)->Offset;
        if (num > methodNumber)
            high = pos - 1;
        else
            low = pos + 1;
    }
    return -1;
}

MethodContextBuffer MethodContextReader::GetSpecificMethodContext(unsigned int methodNumber)
{
    __int64 pos = this->GetOffset(methodNumber);
    if (pos < 0)
    {
        return MethodContextBuffer(-3);
    }

    // Take the IO lock before we set the file pointer, so we can do this on multiple threads
    if (!this->AcquireLock())
    {
        return MethodContextBuffer(-2);
    }
    if (SetFilePointerEx(this->fileHandle, *(PLARGE_INTEGER)&pos, (PLARGE_INTEGER)&pos, FILE_BEGIN))
    {
        // ReadMethodContext will release the lock, but we already acquired it
        MethodContextBuffer mcb = this->ReadMethodContext(false);

        // The curMCIndex value updated by ReadMethodContext() is incorrect
        // since we are repositioning the file pointer we need to update it
        curMCIndex = methodNumber;

        return mcb;
    }
    else
    {
        // Don't forget to release the lock!
        this->ReleaseLock();
        return MethodContextBuffer(-4);
    }
}

// Read the file with excluded methods hashes and save them.
void MethodContextReader::ReadExcludedMethods(std::string mchFileName)
{
    excludedMethodsList = nullptr;

    size_t suffix_offset = mchFileName.find_last_of('.');
    if (suffix_offset == std::string::npos)
    {
        LogError("Failed to get file extension from %s", mchFileName.c_str());
        return;
    }
    std::string suffix          = mchFileName.substr(suffix_offset);
    std::string excludeFileName = MethodContextReader::CheckForPairedFile(mchFileName, suffix.c_str(), ".exc");

    if (excludeFileName.empty())
    {
        return;
    }
    HANDLE excludeFileHandle = OpenFile(excludeFileName.c_str());
    if (excludeFileHandle != INVALID_HANDLE_VALUE)
    {
        __int64 excludeFileSizeLong;
        GetFileSizeEx(excludeFileHandle, (PLARGE_INTEGER)&excludeFileSizeLong);
        unsigned excludeFileSize = (unsigned)excludeFileSizeLong;

        char* buffer = new char[excludeFileSize + 1];
        DWORD bytesRead;
        bool  success = (ReadFile(excludeFileHandle, buffer, excludeFileSize, &bytesRead, NULL) == TRUE);
        CloseHandle(excludeFileHandle);

        if (!success || excludeFileSize != bytesRead)
        {
            LogError("Failed to read the exclude file.");
            delete[] buffer;
            return;
        }

        buffer[excludeFileSize] = 0;

        int counter = 0;

        char* curr = buffer;
        while (*curr != 0)
        {
            while (isspace(*curr))
            {
                curr++;
            }

            std::string hash;
            while (*curr != 0 && !isspace(*curr))
            {
                hash += *curr;
                curr++;
            }

            if (hash.length() == MD5_HASH_BUFFER_SIZE - 1)
            {
                StringList* node    = new StringList();
                node->hash          = hash;
                node->next          = excludedMethodsList;
                excludedMethodsList = node;
                counter++;
            }
            else
            {
                LogInfo("The exclude file contains wrong values: %s.", hash.c_str());
            }
        }
        delete[] buffer;
        LogInfo("Exclude file %s contains %d methods.", excludeFileName.c_str(), counter);
    }
}

// Free memory used for excluded methods.
void MethodContextReader::CleanExcludedMethods()
{
    while (excludedMethodsList != nullptr)
    {
        StringList* next = excludedMethodsList->next;
        delete excludedMethodsList;
        excludedMethodsList = next;
    }
}

// Return should this method context be excluded from the replay or not.
bool MethodContextReader::IsMethodExcluded(MethodContext* mc)
{
    if (excludedMethodsList != nullptr)
    {
        char md5HashBuf[MD5_HASH_BUFFER_SIZE] = {0};
        mc->dumpMethodMD5HashToBuffer(md5HashBuf, MD5_HASH_BUFFER_SIZE);
        for (StringList* node = excludedMethodsList; node != nullptr; node = node->next)
        {
            if (strcmp(node->hash.c_str(), md5HashBuf) == 0)
            {
                return true;
            }
        }
    }
    return false;
}
