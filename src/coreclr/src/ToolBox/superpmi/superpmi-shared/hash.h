//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// hash.h - Class for hashing a text stream using MD5 hashing
//----------------------------------------------------------
#ifndef _hash
#define _hash

#define MD5_HASH_BYTE_SIZE 16   // MD5 is 128-bit, so we need 16 bytes to store it
#define MD5_HASH_BUFFER_SIZE 33 // MD5 is 128-bit, so we need 32 chars + 1 char to store null-terminator

class Hash
{
public:

    Hash();
    ~Hash();

    bool Initialize();
    bool Destroy();

    bool IsInitialized()
    {
#ifdef TARGET_UNIX
        return true; // No initialization necessary.
#else // TARGET_UNIX 
        return m_Initialized;
#endif // !TARGET_UNIX 

    }

    int HashBuffer(BYTE* pBuffer, size_t bufLen, char* hash, size_t hashLen);

private:

    bool WriteHashValueAsText(const BYTE* pHash, size_t cbHash, char* hashTextBuffer, size_t hashTextBufferLen);

#ifndef TARGET_UNIX
    bool m_Initialized;
    HCRYPTPROV m_hCryptProv;
#endif // !TARGET_UNIX
};

#endif
