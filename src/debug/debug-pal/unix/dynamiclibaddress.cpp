//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#include "windefs.h"
#include <stdio.h>


void *GetDynamicLibraryAddressInProcess(DWORD pid, const char *libraryName)
{
#ifdef HAVE_PROCFS_CTL
    // Here we read /proc/<pid>/maps file in order to parse it and figure out what it says 
    // about a library we are looking for. This file looks something like this:
    //
    // [address]      [perms] [offset] [dev] [inode]     [pathname] - HEADER is not preset in an actual file
    //
    // 35b1800000-35b1820000 r-xp 00000000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a1f000-35b1a20000 r--p 0001f000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a20000-35b1a21000 rw-p 00020000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a21000-35b1a22000 rw-p 00000000 00:00 0       [heap]
    // 35b1c00000-35b1dac000 r-xp 00000000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1dac000-35b1fac000 ---p 001ac000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1fac000-35b1fb0000 r--p 001ac000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1fb0000-35b1fb2000 rw-p 001b0000 08:02 135870  /usr/lib64/libc-2.15.so

    void *result = NULL;

    // Making something like: /proc/123/maps
    char mapFileName[100]; 
    int chars = snprintf(mapFileName, sizeof(mapFileName), "/proc/%d/maps", pid);
    _ASSERTE(chars > 0 && chars <= sizeof(mapFileName));

    // Making something like: /libcoreclr.so
    char slashLibName[PATH_MAX]; 
    chars = snprintf(slashLibName, sizeof(slashLibName), "/%s", libraryName);
    _ASSERTE(chars > 0 && chars <= sizeof(mapFileName));
    size_t slashLibNameLen = strlen(slashLibName);

    FILE *mapsFile = fopen(mapFileName, "r");
    if (mapsFile == NULL) 
    {
        return NULL;
    }

    char *line = NULL;
    size_t len = 0;
    ssize_t read;

    // Reading maps file line by line 
    while ((read = getline(&line, &len, mapsFile)) != -1) 
    {
        //Checking if this line ends with /libraryName\n
        const char *expectedLibLocation = line + strlen(line) - 1 - slashLibNameLen;
        if (expectedLibLocation > line && strncmp(expectedLibLocation, slashLibName, slashLibNameLen) == 0)
        {
            void *address1, *address2, *offset;
            // We found a record for our library
            // let's parse address and offset

            if (sscanf(line, "%p-%p %*[-rwxsp] %p", &address1, &address2, &offset) == 3)
            {
                // We were able to read all the info we need
                if (offset == 0)
                {
                    // We found address that corresponds to the very beginning of the lib we're looking for
                    result = address1;
                    break;
                }
            }
        }
    }

    free(line); // We didn't allocate line, but as per contract of getline we should free it
    fclose(mapsFile);
    return result;

#else
    _ASSERTE(!"Not implemented on this platform");
    return NULL;
#endif    
}