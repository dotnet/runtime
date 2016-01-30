// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"

/**
 * Struct to describe the amount of free space and total space on a given mount point
 */
struct MountPointInformation
{
    uint64_t AvailableFreeSpace;
    uint64_t TotalFreeSpace;
    uint64_t TotalSize;
};

/**
 * Function pointer to call back into C# when we find a mount point via GetAllMountPoints.
 * Using the callback pattern allows us to limit the number of allocs we do and makes it
 * cleaner on the managed side since we don't have to worry about cleaning up any unmanaged memory.
 */
typedef void (*MountPointFound)(const char* name);

/**
 * Gets the space information for the given mount point and populates the input struct with the data.
 */
extern "C" int32_t SystemNative_GetSpaceInfoForMountPoint(const char* name, MountPointInformation* mpi);

/**
 * Gets the format information about the given mount point.
 * We separate format info from space info because format information is given back differently per-platform
 * so keep the space information simple (above) and do all the platform logic here.
 * Ubuntu (and most Linux systems) will provide the mount point format as a long int in statfs while
 * OS X and BSD-like systems will provide the information in a char buffer.
 * Since C# is much better at enum and string handling, pass either the char buffer or the long type
 * back, depending on what the platform gives us, and let C# reason on it in an easy way.
 */
extern "C" int32_t
SystemNative_GetFormatInfoForMountPoint(const char* name, char* formatNameBuffer, int32_t bufferLength, int64_t* formatType);

/**
 * Enumerate all mount points on the system and call the input
 * function pointer once-per-mount-point to prevent heap allocs
 * as much as possible.
 */
extern "C" int32_t SystemNative_GetAllMountPoints(MountPointFound onFound);
