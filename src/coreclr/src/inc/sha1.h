// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef SHA1_H_
#define SHA1_H_

// Hasher class, performs no allocation and therefore does not throw or return
// errors. Usage is as follows:
//  Create an instance (this initializes the hash).
//  Add one or more blocks of input data using AddData().
//  Retrieve the hash using GetHash(). This can be done as many times as desired
//  until the object is destructed. Once a hash is asked for, further AddData
//  calls will be ignored. There is no way to reset object state (simply
//  destroy the object and create another instead).

#define SHA1_HASH_SIZE 20  // Number of bytes output by SHA-1

typedef struct {
        DWORD magic_sha1;    // Magic value for A_SHA_CTX
        DWORD awaiting_data[16];
                             // Data awaiting full 512-bit block.
                             // Length (nbit_total[0] % 512) bits.
                             // Unused part of buffer (at end) is zero
        DWORD partial_hash[5];
                             // Hash through last full block
        DWORD nbit_total[2];
                             // Total length of message so far
                             // (bits, mod 2^64)
} SHA1_CTX;

class SHA1Hash
{
private:
    SHA1_CTX m_Context;
    BYTE     m_Value[SHA1_HASH_SIZE];
    BOOL     m_fFinalized;

    void SHA1Init(SHA1_CTX*);
    void SHA1Update(SHA1_CTX*, const BYTE*, const DWORD);
    void SHA1Final(SHA1_CTX*, BYTE* digest);

public:
    SHA1Hash();
    void AddData(BYTE *pbData, DWORD cbData);
    BYTE *GetHash();
};

#endif  // SHA1_H_
