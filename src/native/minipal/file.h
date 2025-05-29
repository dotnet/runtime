// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_FILE_H
#define HAVE_MINIPAL_FILE_H

#include <stdbool.h>
#include <stdint.h>
#include <minipal/types.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef struct minipal_file_attr_
{
    uint64_t size;
    uint64_t lastWriteTime; // Windows FILETIME precision
} minipal_file_attr_t;

bool minipal_file_get_attributes_utf16(const CHAR16_T* path, minipal_file_attr_t* attributes);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* HAVE_MINIPAL_FILE_H */
