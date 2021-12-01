// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// md5.h
//

//
// A pretty fast implementation of MD5
//


#ifndef __MD5_H__
#define __MD5_H__

/////////////////////////////////////////////////////////////////////////////////////
//
// Declaration of the central transform function
//
void __stdcall MD5Transform(ULONG state[4], const ULONG* data);

/////////////////////////////////////////////////////////////////////////////////////

#include <pshpack1.h>


// This structure is used to return the final resulting hash.
//
struct MD5HASHDATA
    {
    union
        {
        BYTE rgb[16];
        struct
            {
            ULONGLONG ullLow;
            ULONGLONG ullHigh;
            } u;
        struct
            {
            ULONG     u0;
            ULONG     u1;
            ULONG     u2;
            ULONG     u3;
            } v;
        };
    };

inline BOOL operator==(const MD5HASHDATA& me, const MD5HASHDATA& him)
    {
    return memcmp(&me, &him, sizeof(MD5HASHDATA)) == 0;
    }

inline BOOL operator!=(const MD5HASHDATA& me, const MD5HASHDATA& him)
    {
    return memcmp(&me, &him, sizeof(MD5HASHDATA)) != 0;
    }


// The engine that carries out the hash
//
class MD5
    {
    // These four values must be contiguous, and in this order
    union
        {
        ULONG       m_state[4];
        struct
            {
            ULONG       m_a;              // state
            ULONG       m_b;              //     ... variables
            ULONG       m_c;              //            ... as found in
            ULONG       m_d;              //                    ... RFC1321
            } u;
        };

    BYTE        m_data[64];       // where to accumulate the data as we are passed it
    ULONGLONG   m_cbitHashed;     // amount of data that we've hashed
    ULONG       m_cbData;         // number of bytes presently in data

    BYTE        m_padding[64];    // padding data, used if length data not = 0 mod 64

public:

    /////////////////////////////////////////////////////////////////////////////////////

    void Hash(const BYTE* pbData, ULONG cbData, MD5HASHDATA* phash, BOOL fConstructed = FALSE)
        {
        Init(fConstructed);
        HashMore(pbData, cbData);
        GetHashValue(phash);
        }

    /////////////////////////////////////////////////////////////////////////////////////

    void Hash(const BYTE* pbData, ULONGLONG cbData, MD5HASHDATA* phash, BOOL fConstructed = FALSE)
        {
        Init(fConstructed);

        ULARGE_INTEGER ul;
        ul.QuadPart = cbData;

        while (ul.u.HighPart)
            {
            ULONG cbHash = 0xFFFFFFFF;                      // Hash as much as we can at once
            HashMore(pbData, cbHash);
            pbData      += cbHash;
            ul.QuadPart -= cbHash;
            }

        HashMore(pbData, ul.u.LowPart);                       // Hash whatever is left

        GetHashValue(phash);
        }

    /////////////////////////////////////////////////////////////////////////////////////

    void Init(BOOL fConstructed = FALSE);

    /////////////////////////////////////////////////////////////////////////////////////

    void HashMore(const void* pvInput, ULONG cbInput);

    /////////////////////////////////////////////////////////////////////////////////////

    void GetHashValue(MD5HASHDATA* phash);

    /////////////////////////////////////////////////////////////////////////////////////

    };

#include <poppack.h>

#endif
