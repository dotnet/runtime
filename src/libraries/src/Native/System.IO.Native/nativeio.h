//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#pragma once

#include <stdint.h>

extern "C"
{
    struct FileStats
    {
        int32_t Flags;            // flags for testing if some members are present (see values below)
        int32_t Mode;             // protection
        int32_t Uid;              // user ID of owner
        int32_t Gid;              // group ID of owner
        int64_t Size;             // total size, in bytes
        int64_t AccessTime;       // time of last access (atime)
        int64_t ModificationTime; // time of last modification (mtime)
        int64_t StatusChangeTime; // time of last status change (ctime)
        int64_t CreationTime;     // time the file was created (birthtime)
    };

    enum
    {
        FILESTATS_FLAGS_NONE = 0,
        FILESTATS_FLAGS_HAS_CREATION_TIME = 1,
    };

    /**
     * Get file stats from a decriptor. Implemented as shim to fstat(2).
     *
     * Returns 0 for success, -1 for failure. Sets errno on failure.
     */
    int32_t FStat(int32_t fileDescriptor, FileStats* output);

    /**
     * Get file stats from a full path. Implemented as shim to stat(2).
     *
     * Returns 0 for success, -1 for failure. Sets errno on failure.
     */
    int32_t Stat(const char* path, FileStats* output);

    /**
    * Get file stats from a full path. Implemented as shim to lstat(2).
    *
    * Returns 0 for success, -1 for failure. Sets errno on failure.
    */
    int32_t LStat(const char* path, FileStats* output);
}

