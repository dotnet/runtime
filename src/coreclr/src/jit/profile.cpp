// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "profile.h"

#if defined(JIT_ADHOC_PROFILE)

ICorJitInfo::BlockCounts* Profile::s_ProfileData;
unsigned                  Profile::s_ProfileIndex;
const char* const         Profile::s_FileHeaderString  = "*** START Jit profile data, max index = %u ***\n";
const char* const         Profile::s_FileTrailerString = "*** END Jit profile data ***\n";
const char* const         Profile::s_MethodHeaderString =
    "@@@ token 0x%08X hash 0x%08X ilSize 0x%08X records 0x%08X index %u\n";
const char* const Profile::s_RecordString = "ilOffs %u count %u\n";

//------------------------------------------------------------------------
// allocateProfileData: allocate in-memory profile data buffer
//
// todo: multithreaded protection
// todo: persistent failure
//
void Profile::allocateProfileData(unsigned maxIndex)
{
    if (s_ProfileData != nullptr)
    {
        return;
    }

    ICorJitInfo::BlockCounts* const profileBuffer =
        new (HostAllocator::getHostAllocator()) ICorJitInfo::BlockCounts[maxIndex];

    if (profileBuffer == nullptr)
    {
        return;
    }

    s_ProfileData  = profileBuffer;
    s_ProfileIndex = 0;
}

//------------------------------------------------------------------------
// allocateMethodData: allocate in-memory profile buffer for a method
//
// Argments:
//    recordCount - number of records needed for the method, including
//                     the header record(s)
//
// Returns:
//    profile buffer to use for the method
//
ICorJitInfo::BlockCounts* Profile::allocateMethodData(unsigned recordCount)
{
    allocateProfileData();

    if (s_ProfileData == nullptr)
    {
        return nullptr;
    }

    unsigned methodIndex = 0;

    // Look for space in the profile buffer for this method.
    // Note other jit invocations may be vying for space concurrently.
    //
    while (true)
    {
        const unsigned oldIndex = s_ProfileIndex;
        const unsigned newIndex = oldIndex + recordCount;

        // If there is no room left for this method,
        // that's ok, we just won't profile this method.
        //
        if (newIndex >= BUFFER_SIZE)
        {
            return nullptr;
        }

        const unsigned updatedIndex = InterlockedCompareExchangeT(&s_ProfileIndex, newIndex, oldIndex);

        if (updatedIndex == oldIndex)
        {
            // Found space
            methodIndex = oldIndex;
            break;
        }
    }

    return &s_ProfileData[methodIndex];
}

//------------------------------------------------------------------------
// writeProfileData: save in-process profile data to a text file
//
// Notes:
//   When the jit is collecting in-process profile data, profile data is
//   saved to the file specified by JitWriteProfileData.
//
void Profile::writeProfileData()
{
    if (s_ProfileData == nullptr)
    {
        return;
    }

    if (JitConfig.JitWriteProfileData() == nullptr)
    {
        return;
    }

    FILE* const profileDataFile = _wfopen(JitConfig.JitWriteProfileData(), W("w"));

    if (profileDataFile == nullptr)
    {
        return;
    }

    fprintf(profileDataFile, s_FileHeaderString, s_ProfileIndex);
    unsigned       index    = 0;
    const unsigned maxIndex = s_ProfileIndex;

    while (index < maxIndex)
    {
        const Header* const header = (Header*)&s_ProfileData[index];

        // Sanity checks -- note asserting here leads to hangs at runtime.
        if ((header->recordCount < MIN_RECORD_COUNT) || (header->recordCount > MAX_RECORD_COUNT))
        {
            fprintf(profileDataFile, "Unreasonable record count %u at index %u\n", header[0], index);
            break;
        }

        fprintf(profileDataFile, s_MethodHeaderString, header->token, header->hash, header->ilSize, header->recordCount,
                index);

        index += 2;

        ICorJitInfo::BlockCounts* records     = &s_ProfileData[index];
        unsigned                  recordCount = header->recordCount - 2;
        unsigned                  lastOffset  = 0;
        for (unsigned i = 0; i < recordCount; i++)
        {
            const unsigned thisOffset = records[i].ILOffset;
            assert((thisOffset > lastOffset) || (lastOffset == 0));
            lastOffset = thisOffset;

            // We probably could suppress writing zero records here,
            // if we default to zero fill when recovering data and
            // propagating to the flow graph.
            fprintf(profileDataFile, s_RecordString, records[i].ILOffset, records[i].ExecutionCount);
        }

        index += recordCount;
    }

    fprintf(profileDataFile, s_FileTrailerString);
    fclose(profileDataFile);
}

//------------------------------------------------------------------------
// readProfileData: read profile data from a text file
//
// Notes:
//   This method allocates and fills in the internal profile buffer by
//   reading information from a text file.
//
//   If the jit is also collecting profile data then reading profile
//   data is inhibited.
//
void Profile::readProfileData()
{
    // If we're already writing profile data or doing tiered pgo, defer reading data.
    //
    if ((JitConfig.JitWriteProfileData() != nullptr) || (JitConfig.JitTieredPGO() > 0))
    {
        return;
    }

    // If we've already read profile data, we're done.
    //
    if (s_ProfileData != nullptr)
    {
        assert(s_ProfileIndex >= MIN_RECORD_COUNT);
        return;
    }

    FILE* const profileDataFile = _wfopen(JitConfig.JitReadProfileData(), W("r"));

    if (profileDataFile == nullptr)
    {
        return;
    }

    char     buffer[256];
    unsigned maxIndex = 0;

    // Header must be first line
    //
    if (fgets(buffer, sizeof(buffer), profileDataFile) == nullptr)
    {
        return;
    }

    if (sscanf_s(buffer, s_FileHeaderString, &maxIndex) != 1)
    {
        return;
    }

    // Sanity check
    //
    if ((maxIndex == 0) || (maxIndex >= MAX_RECORD_COUNT))
    {
        return;
    }

    // Allocate the profile data
    //
    Profile::allocateProfileData(maxIndex);

    if (s_ProfileData == nullptr)
    {
        return;
    }

    // Fill in the data
    //
    unsigned index   = 0;
    unsigned methods = 0;

    bool failed = false;
    while (!failed)
    {
        if (fgets(buffer, sizeof(buffer), profileDataFile) == nullptr)
        {
            break;
        }

        // Find the next method entry line
        //
        unsigned recordCount = 0;
        unsigned token       = 0;
        unsigned hash        = 0;
        unsigned ilSize      = 0;
        unsigned rIndex      = 0;

        if (sscanf_s(buffer, s_MethodHeaderString, &token, &hash, &ilSize, &recordCount, &rIndex) != 5)
        {
            continue;
        }

        assert(index == rIndex);
        methods++;

        Profile::Header* const header = (Profile::Header*)&s_ProfileData[index];

        header->recordCount = recordCount;
        header->token       = token;
        header->hash        = hash;
        header->ilSize      = ilSize;

        // Sanity check
        //
        if ((recordCount < MIN_RECORD_COUNT) || (recordCount > MAX_RECORD_COUNT))
        {
            failed = true;
            break;
        }

        index += 2;

        // Read il data
        //
        for (unsigned i = 0; i < recordCount - 2; i++)
        {
            if (fgets(buffer, sizeof(buffer), profileDataFile) == nullptr)
            {
                failed = true;
                break;
            }

            if (sscanf_s(buffer, Profile::s_RecordString, &s_ProfileData[index].ILOffset,
                         &s_ProfileData[index].ExecutionCount) != 2)
            {
                failed = true;
                break;
            }

            index++;
        }
    }

    // JITDUMPs here not all that useful.
    if (failed)
    {
        // todo: inhibit other attempts
        JITDUMP("Failed to parse profile data from %ws : record %u line '%s'\n", JitConfig.JitReadProfileData(), index,
                buffer);
    }
    else
    {
        JITDUMP("Profile Data read from %s successfully, %u records %u methods\n", JitConfig.JitReadProfileData(),
                maxIndex, methods);

        s_ProfileIndex = maxIndex;
    }
}

#endif // #if defined(JIT_ADHOC_PROFILE)
