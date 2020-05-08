// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _PROFILE_H_
#define _PROFILE_H_

#if defined(JIT_ADHOC_PROFILE)

// Support for in-memory profile buffer that can
// be saved to a file or restored from a file
//
class Profile
{
public:
    struct Header
    {
        unsigned recordCount;
        unsigned token;
        unsigned hash;
        unsigned ilSize;
    };

    enum
    {
        // Number of ICorJitInfo::BlockCount records in the in-process buffer.
        BUFFER_SIZE      = 64 * 1024,
        MIN_RECORD_COUNT = 3,
        MAX_RECORD_COUNT = BUFFER_SIZE
    };

    static void allocateProfileData(unsigned maxIndex = BUFFER_SIZE);
    static ICorJitInfo::BlockCounts* allocateMethodData(unsigned recordCount);
    static void writeProfileData();
    static void readProfileData();

    static const char* const s_FileHeaderString;
    static const char* const s_FileTrailerString;
    static const char* const s_MethodHeaderString;
    static const char* const s_RecordString;

    // The in-process buffer
    static ICorJitInfo::BlockCounts* s_ProfileData;

    // Next free record slot in the buffer
    static unsigned s_ProfileIndex;
};

#endif // defined(JIT_ADHOC_PROFILE)

#endif // _PROFILE_H_
