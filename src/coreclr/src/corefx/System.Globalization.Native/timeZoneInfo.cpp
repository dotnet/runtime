// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
