// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "sha1.h"
#include <stdint.h>
#include <limits.h>
#include <assert.h>

typedef struct {
        uint32_t magic_sha1;    // Magic value for A_SHA_CTX
        uint32_t awaiting_data[16];
                             // Data awaiting full 512-bit block.
                             // Length (nbit_total[0] % 512) bits.
                             // Unused part of buffer (at end) is zero
        uint32_t partial_hash[5];
                             // Hash through last full block
        uint32_t nbit_total[2];
                             // Total length of message so far
                             // (bits, mod 2^64)
} SHA1_CTX;


#if !defined(_MSC_VER)
#if !__has_builtin(_rotl) && !defined(_rotl)
inline static
unsigned int _rotl(unsigned int value, int shift)
{
    unsigned int retval = 0;

    shift &= 0x1f;
    retval = (value << shift) | (value >> (sizeof(int) * CHAR_BIT - shift));
    return retval;
}
#endif // !__has_builtin(_rotl)
#endif // !_MSC_VER

#define MIN(a,b) (((a) < (b)) ? (a) : (b))

#define ROTATE32L(x,n) _rotl(x,n)
#define SHAVE32(x)     (uint32_t)(x)

/*
     Update the SHA-1 hash from a fresh 64 bytes of data.
*/
static void SHA1_block(SHA1_CTX *ctx)
{
    static const uint32_t sha1_round1 = 0x5A827999u;
    static const uint32_t sha1_round2 = 0x6ED9EBA1u;
    static const uint32_t sha1_round3 = 0x8F1BBCDCu;
    static const uint32_t sha1_round4 = 0xCA62C1D6u;

    uint32_t a = ctx->partial_hash[0], b = ctx->partial_hash[1];
    uint32_t c = ctx->partial_hash[2], d = ctx->partial_hash[3];
    uint32_t e = ctx->partial_hash[4];
    uint32_t  msg80[80];
    int i;

    // OACR note:
    // Loop conditions are using (i <= limit - increment) instead of (i < limit) to satisfy OACR. When the increment is greater
    // than 1, OACR incorrectly thinks that the max value of 'i' is (limit - 1).

    for (i = 0; i < 16; i++) {   // Copy to local array, zero original
                                  // Extend length to 80
        const uint32_t datval = ctx->awaiting_data[i];
        ctx->awaiting_data[i] = 0;
        msg80[i] = datval;
    }

    for (i = 16; i <= 80 - 2; i += 2) {
        const uint32_t temp1 =    msg80[i-3] ^ msg80[i-8]
                        ^ msg80[i-14] ^ msg80[i-16];
        const uint32_t temp2 =    msg80[i-2] ^ msg80[i-7]
                        ^ msg80[i-13] ^ msg80[i-15];
        msg80[i  ] = ROTATE32L(temp1, 1);
        msg80[i+1] = ROTATE32L(temp2, 1);
    }

#define ROUND1(B, C, D) (((D) ^ ((B) & ((C) ^ (D)))) + sha1_round1)
                        //  Equivalent to (B & C) | (~B & D).
                        //  (check cases B = 0 and B = 1)
#define ROUND2(B, C, D) (((B) ^ (C) ^ (D)) + sha1_round2)

#define ROUND3(B, C, D) ((((C) & ((B) | (D))) | ((B) & (D))) + sha1_round3)

#define ROUND4(B, C, D) (((B) ^ (C) ^ (D)) + sha1_round4)

// Round 1
    for (i = 0; i <= 20 - 5; i += 5) {
        e += ROTATE32L(a, 5) + ROUND1(b, c, d) + msg80[i];
        b = ROTATE32L(b, 30);

        d += ROTATE32L(e, 5) + ROUND1(a, b, c) + msg80[i+1];
        a = ROTATE32L(a, 30);

        c += ROTATE32L(d, 5) + ROUND1(e, a, b) + msg80[i+2];
        e = ROTATE32L(e, 30);

        b += ROTATE32L(c, 5) + ROUND1(d, e, a) + msg80[i+3];
        d = ROTATE32L(d, 30);

        a += ROTATE32L(b, 5) + ROUND1(c, d, e) + msg80[i+4];
        c = ROTATE32L(c, 30);
#if 0
        printf("i = %ld %08lx %08lx %08lx %08lx %08lx\n",
            i, a, b, c, d, e);
#endif
    } // for i

// Round 2
    for (i = 20; i <= 40 - 5; i += 5) {
        e += ROTATE32L(a, 5) + ROUND2(b, c, d) + msg80[i];
        b = ROTATE32L(b, 30);

        d += ROTATE32L(e, 5) + ROUND2(a, b, c) + msg80[i+1];
        a = ROTATE32L(a, 30);

        c += ROTATE32L(d, 5) + ROUND2(e, a, b) + msg80[i+2];
        e = ROTATE32L(e, 30);

        b += ROTATE32L(c, 5) + ROUND2(d, e, a) + msg80[i+3];
        d = ROTATE32L(d, 30);

        a += ROTATE32L(b, 5) + ROUND2(c, d, e) + msg80[i+4];
        c = ROTATE32L(c, 30);
    } // for i

// Round 3
    for (i = 40; i <= 60 - 5; i += 5) {
        e += ROTATE32L(a, 5) + ROUND3(b, c, d) + msg80[i];
        b = ROTATE32L(b, 30);

        d += ROTATE32L(e, 5) + ROUND3(a, b, c) + msg80[i+1];
        a = ROTATE32L(a, 30);

        c += ROTATE32L(d, 5) + ROUND3(e, a, b) + msg80[i+2];
        e = ROTATE32L(e, 30);

        b += ROTATE32L(c, 5) + ROUND3(d, e, a) + msg80[i+3];
        d = ROTATE32L(d, 30);

        a += ROTATE32L(b, 5) + ROUND3(c, d, e) + msg80[i+4];
        c = ROTATE32L(c, 30);
    } // for i

// Round 4
    for (i = 60; i <= 80 - 5; i += 5) {
        e += ROTATE32L(a, 5) + ROUND4(b, c, d) + msg80[i];
        b = ROTATE32L(b, 30);

        d += ROTATE32L(e, 5) + ROUND4(a, b, c) + msg80[i+1];
        a = ROTATE32L(a, 30);

        c += ROTATE32L(d, 5) + ROUND4(e, a, b) + msg80[i+2];
        e = ROTATE32L(e, 30);

        b += ROTATE32L(c, 5) + ROUND4(d, e, a) + msg80[i+3];
        d = ROTATE32L(d, 30);

        a += ROTATE32L(b, 5) + ROUND4(c, d, e) + msg80[i+4];
        c = ROTATE32L(c, 30);
    } // for i

#undef ROUND1
#undef ROUND2
#undef ROUND3
#undef ROUND4

    ctx->partial_hash[0] += a;
    ctx->partial_hash[1] += b;
    ctx->partial_hash[2] += c;
    ctx->partial_hash[3] += d;
    ctx->partial_hash[4] += e;
} // end SHA1_block



/*
    Initialize the hash context.
*/
static void SHA1Init(SHA1_CTX *ctx)
{
    ctx->nbit_total[0] = ctx->nbit_total[1] = 0;

    for (uint32_t i = 0; i != 16; i++) {
        ctx->awaiting_data[i] = 0;
    }

    ctx->partial_hash[0] = 0x67452301u;
    ctx->partial_hash[1] = 0xefcdab89u;
    ctx->partial_hash[2] = ~ctx->partial_hash[0];
    ctx->partial_hash[3] = ~ctx->partial_hash[1];
    ctx->partial_hash[4] = 0xc3d2e1f0u;

}

/*
    Append data to a partially hashed SHA-1 message.
*/
static void SHA1Update(
        SHA1_CTX *  ctx,        // IN/OUT
        const uint8_t *    msg,    // IN
        uint32_t           nbyte)  // IN
{
    const uint8_t *fresh_data = msg;
    uint32_t nbyte_left = nbyte;
    uint32_t nbit_occupied = ctx->nbit_total[0] & 511;
    uint32_t *awaiting_data;
    const uint32_t nbitnew_low = SHAVE32(8*nbyte);


    assert((nbit_occupied & 7) == 0);   // Partial bytes not implemented

    ctx->nbit_total[0] += nbitnew_low;
    ctx->nbit_total[1] += (nbyte >> 29)
           + (SHAVE32(ctx->nbit_total[0]) < nbitnew_low);

        /* Advance to word boundary in waiting_data */

    if ((nbit_occupied & 31) != 0) {
        awaiting_data = ctx->awaiting_data + nbit_occupied/32;

        while ((nbit_occupied & 31) != 0 && nbyte_left != 0) {
            nbit_occupied += 8;
            *awaiting_data |= (uint32_t)*fresh_data++
                     << ((-(int)nbit_occupied) & 31);
            nbyte_left--;            // Start at most significant byte
        }
    } // if nbit_occupied

             /* Transfer 4 bytes at a time */

    do {
        const uint32_t nword_occupied = nbit_occupied/32;
        uint32_t nwcopy = MIN(nbyte_left/4, 16 - nword_occupied);
        assert (nbit_occupied <= 512);
        assert ((nbit_occupied & 31) == 0 || nbyte_left == 0);
        awaiting_data = ctx->awaiting_data + nword_occupied;
        nbyte_left -= 4*nwcopy;
        nbit_occupied += 32*nwcopy;

        while (nwcopy != 0) {
            const uint32_t byte0 = (uint32_t)fresh_data[0];
            const uint32_t byte1 = (uint32_t)fresh_data[1];
            const uint32_t byte2 = (uint32_t)fresh_data[2];
            const uint32_t byte3 = (uint32_t)fresh_data[3];
            *awaiting_data++ = byte3 | (byte2 << 8)
                        | (byte1 << 16) | (byte0 << 24);
                             /* Big endian */
            fresh_data += 4;
            nwcopy--;
        }

        if (nbit_occupied == 512) {
            SHA1_block(ctx);
            nbit_occupied = 0;
            awaiting_data -= 16;
            assert(awaiting_data == ctx->awaiting_data);
        }
    } while (nbyte_left >= 4);

    assert (ctx->awaiting_data + nbit_occupied/32
                       == awaiting_data);

    while (nbyte_left != 0) {
        const uint32_t new_byte = (uint32_t)*fresh_data++;

        assert((nbit_occupied & 31) <= 16);
        nbit_occupied += 8;
        *awaiting_data |= new_byte << ((-(int)nbit_occupied) & 31);
        nbyte_left--;
    }

    assert (nbit_occupied == (ctx->nbit_total[0] & 511));
}

/*
        Finish a SHA-1 hash.
*/
static void SHA1Final(
        SHA1_CTX *  ctx,            // IN/OUT
        uint8_t *          digest)     // OUT
{
    const uint32_t nbit0 = ctx->nbit_total[0];
    const uint32_t nbit1 = ctx->nbit_total[1];
    uint32_t  nbit_occupied = nbit0 & 511;
    uint32_t i;

    assert((nbit_occupied & 7) == 0);

    ctx->awaiting_data[nbit_occupied/32]
         |= (uint32_t)0x80 << ((-8-nbit_occupied) & 31);
                          // Append a 1 bit
    nbit_occupied += 8;


    // Append zero bits until length (in bits) is 448 mod 512.
    // Then append the length, in bits.
    // Here we assume the buffer was zeroed earlier.

    if (nbit_occupied > 448) {   // If fewer than 64 bits left
        SHA1_block(ctx);
        nbit_occupied = 0;
    }
    ctx->awaiting_data[14] = nbit1;
    ctx->awaiting_data[15] = nbit0;
    SHA1_block(ctx);

         /* Copy final digest to user-supplied byte array */

    for (i = 0; i != 5; i++) {
        const uint32_t dwi = ctx->partial_hash[i];
        digest[4*i + 0] = (uint8_t)((dwi >> 24) & 255);
        digest[4*i + 1] = (uint8_t)((dwi >> 16) & 255);
        digest[4*i + 2] = (uint8_t)((dwi >>  8) & 255);
        digest[4*i + 3] = (uint8_t)(dwi         & 255);  // Big-endian
    }
}

void minipal_sha1(const void *data, size_t length, uint8_t *hash, size_t hashBufferLength)
{
    assert(hashBufferLength >= SHA1_HASH_SIZE);
    SHA1_CTX ctx;
    SHA1Init(&ctx);
    if (length > UINT32_MAX)
    {
        SHA1Update(&ctx, data, UINT32_MAX);
        data = (uint8_t*)data + UINT32_MAX;
        length -= UINT32_MAX;
        SHA1Update(&ctx, data, (uint32_t)length);
    }
    else
    {
        SHA1Update(&ctx, data, (uint32_t)length);
    }
    SHA1Final(&ctx, hash);
}
