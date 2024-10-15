// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef MINIPAL_SHA1_H_
#define MINIPAL_SHA1_H_

#include <stdint.h>
#include <stdlib.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

#define SHA1_HASH_SIZE 20  // Number of bytes output by SHA-1

// Fill the hash buffer with the SHA-1 hash of the data.
// This function should only be used for non-cryptographic purposes.
// Cryptographic usages should need to use the platform-native SHA-1 implementations
// for security reasons.
void minipal_sha1(const void *data, size_t length, uint8_t *hash, size_t hashBufferLength);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif  // SHA1_H_
