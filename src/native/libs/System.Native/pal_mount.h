// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

/**
 * Struct to describe the amount of free space and total space on a given mount point
 */
typedef struct
{
    uint64_t AvailableFreeSpace;
    uint64_t TotalFreeSpace;
    uint64_t TotalSize;
} MountPointInformation;

/**
 * Function pointer to call back into C# when we find a mount point via GetAllMountPoints.
 * Using the callback pattern allows us to limit the number of allocs we do and makes it
 * cleaner on the managed side since we don't have to worry about cleaning up any unmanaged memory.
 */
typedef void (*MountPointFound)(void* context, const char* name);

/**
 * Gets the space information for the given mount point and populates the input struct with the data.
 */
PALEXPORT int32_t SystemNative_GetSpaceInfoForMountPoint(const char* name, MountPointInformation* mpi);

/**
 * Enumerate all mount points on the system and call the input
 * function pointer once-per-mount-point to prevent heap allocs
 * as much as possible.
 */
PALEXPORT int32_t SystemNative_GetAllMountPoints(MountPointFound onFound, void* context);
