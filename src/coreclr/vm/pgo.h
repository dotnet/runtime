// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef PGO_H
#define PGO_H

// PgoManager handles in-process and out of band profile data for jitted code.
class PgoManager
{
#ifdef FEATURE_PGO

public:

    static void Initialize();
    static void Shutdown();

#endif // FEATURE_PGO

public:

    // Allocate a profile block count buffer for a method
    static HRESULT allocMethodBlockCounts(MethodDesc* pMD, UINT32 count,
        ICorJitInfo::BlockCounts** pBlockCounts, unsigned ilSize);

    // Retrieve the profile block count buffer for a method
    static HRESULT getMethodBlockCounts(MethodDesc* pMD, unsigned ilSize, UINT32* pCount,
        ICorJitInfo::BlockCounts** pBlockCounts, UINT32* pNumRuns);

    // Retrieve the most likely class for a particular call
    static CORINFO_CLASS_HANDLE getLikelyClass(MethodDesc* pMD, unsigned ilSize, unsigned ilOffset, UINT32* pLikelihood, UINT32* pNumberOfClasses);

    // Verify address in bounds
    static void VerifyAddress(void* address);

#ifdef FEATURE_PGO

private:

    enum
    {
        // Number of ICorJitInfo::BlockCount records in the global slab.
        // Currently 4MB for a 64 bit system.
        //
        BUFFER_SIZE      = 8 * 64 * 1024,
        MIN_RECORD_COUNT = 3,
        MAX_RECORD_COUNT = BUFFER_SIZE
    };

    struct Header
    {
        unsigned recordCount;
        unsigned token;
        unsigned hash;
        unsigned ilSize;
    };

private:

    static void ReadPgoData();
    static void WritePgoData();

private:

    // Global slab holding all pgo data
    static ICorJitInfo::BlockCounts* s_PgoData;

    // Index of next free entry in the global slab
    static unsigned volatile s_PgoIndex;

    // Formatting strings for file input/output
    static const char* const s_FileHeaderString;
    static const char* const s_FileTrailerString;
    static const char* const s_MethodHeaderString;
    static const char* const s_RecordString;
    static const char* const s_ClassProfileHeader;
    static const char* const s_ClassProfileEntry;

#endif // FEATURE_PGO
};

#endif // PGO_H
