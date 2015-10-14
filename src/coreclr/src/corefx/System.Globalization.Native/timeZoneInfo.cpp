//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.
//

#include <stdint.h>
#include <unistd.h>

/*
Function:
ReadLink

Gets the symlink value for the path.
*/
extern "C" int32_t ReadLink(const char* path, char* result, size_t resultCapacity)
{
    ssize_t r = readlink(path, result, resultCapacity - 1); // subtract one to make room for the NULL character

    if (r < 1 || r >= resultCapacity)
        return false;

    result[r] = '\0';
    return true;
}
