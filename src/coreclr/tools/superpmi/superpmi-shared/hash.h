// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// hash.h - Class for hashing a text stream using MD5 hashing
//----------------------------------------------------------
#ifndef _hash
#define _hash

#define MM3_HASH_BYTE_SIZE 16   // MurMurHash3 is 128-bit, so we need 16 bytes to store it
#define MM3_HASH_BUFFER_SIZE 33 // MurMurHash3 is 128-bit, so we need 32 chars + 1 char to store null-terminator

class Hash
{
public:
    static int HashBuffer(BYTE* pBuffer, size_t bufLen, char* hash, size_t hashLen);

private:
    static bool WriteHashValueAsText(const BYTE* pHash, size_t cbHash, char* hashTextBuffer, size_t hashTextBufferLen);
};

#endif
