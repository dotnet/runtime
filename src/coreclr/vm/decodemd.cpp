// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include "common.h"
#include "decodemd.h"

/*
encoding patterns:
    0   10x     110xxx      1110xxxxxxx    11110xxxxxxxxxxxxxxx    111110xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    0   1,2     3-10        11-138         139-32905               32906-big
*/

#define MAX_HEADER 5
#define BASE_0 0
#define BASE_1 1
#define BASE_2 (0x2+0x1)
#define BASE_3 (0x8+0x2+0x1)
#define BASE_4 (0x80+0x8+0x2+0x1)
#define BASE_5 (0x8000+0x80+0x8+0x2+0x1)
#define BASE_6 (0x80000000+0x8000+0x80+0x8+0x2+0x1)
const unsigned decode_base[MAX_HEADER+2] = {BASE_0, BASE_1, BASE_2, BASE_3, BASE_4, BASE_5, BASE_6};
#define BIT_LENGTH_0 0
#define BIT_LENGTH_1 1
#define BIT_LENGTH_2 3
#define BIT_LENGTH_3 7
#define BIT_LENGTH_4 15
#define BIT_LENGTH_5 31
#define BIT_LENGTH_6 63
const unsigned decode_bitlength[MAX_HEADER+2] =
        {
            BIT_LENGTH_0,
            BIT_LENGTH_1,
            BIT_LENGTH_2,
            BIT_LENGTH_3,
            BIT_LENGTH_4,
            BIT_LENGTH_5,
            BIT_LENGTH_6
        };

#define END_DECODED BASE_3
const BYTE decoded_end[1] = {END_DECODED };
const BYTE decoded_0_0_0_0[5] = {0,0,0,0,END_DECODED };
const BYTE decoded_0_1[3] = {0,1,END_DECODED };
const BYTE decoded_0_2[3] = {0,2,END_DECODED };
const BYTE decoded_1_0[3] = {1,0,END_DECODED };
const BYTE decoded_2_0[3] = {2,0,END_DECODED };
const BYTE decoded_1_0_0[4] = {1,0,0,END_DECODED };
const BYTE decoded_2_0_0[4] = {2,0,0,END_DECODED };
#define decoded_0 &decoded_0_0_0_0[3]
#define decoded_0_0 &decoded_0_0_0_0[2]
#define decoded_0_0_0 &decoded_0_0_0_0[1]
#define decoded_1 &decoded_0_1[1]
#define decoded_2 &decoded_0_2[1]
const BYTE decoded_3[2] = {3, END_DECODED };
const BYTE decoded_4[2] = {4, END_DECODED };
const BYTE decoded_5[2] = {5, END_DECODED };
const BYTE decoded_6[2] = {6, END_DECODED };
const BYTE decoded_7[2] = {7, END_DECODED };
const BYTE decoded_8[2] = {8, END_DECODED };
const BYTE decoded_9[2] = {9, END_DECODED };
const BYTE decoded_10[2] = {10, END_DECODED };

#define INBITS(s) ((s) > MAX_HEADER)
#define INHEADER(s) ((s) <= MAX_HEADER)
#define PARTIALBITS(s) (((s)>>8)&0xFF)
#define NUMBERGOTTEN(s) (((s)>>16)&0xFF)
#define HEADER(s) (((s)>>24)&0xFF)
#define DECODING_HEADER(n) n
#define DOING_BITS (MAX_HEADER+1)
#define DECODING_BITS(partial, got, header) (DOING_BITS+((partial)<<8)+((got)<<16)+((header)<<24))
#define DECODING_ERROR ((unsigned) -1)
#define MASK(len) (~(~0u <<(len)))
#define MASK64(len) ((~((~((unsigned __int64)0))<<(len))))
#define BITS_PER_BYTE (sizeof(BYTE)*8)

const Decoder::Decode emptyDecode = {decoded_end, DECODING_HEADER(0)};

const Decoder::Decode transition[6][16] =
{
    //header(0)
  {
    { decoded_0_0_0_0, DECODING_HEADER(0) },    // 0000
    { decoded_0_0_0, DECODING_HEADER(1) },      // 0001
    { decoded_0_0, DECODING_BITS(0,0,1) },      // 0010
    { decoded_0_0, DECODING_HEADER(2) },        // 0011
    { decoded_0_1, DECODING_HEADER(0) },        // 0100
    { decoded_0_2, DECODING_HEADER(0) },        // 0101
    { decoded_0, DECODING_BITS(0,0,2) },        // 0110
    { decoded_0, DECODING_HEADER(3) },          // 0111
    { decoded_1_0, DECODING_HEADER(0) },        // 1000
    { decoded_1, DECODING_HEADER(1) },          // 1001
    { decoded_2_0, DECODING_HEADER(0) },        // 1010
    { decoded_2, DECODING_HEADER(1) },          // 1011
    { decoded_end, DECODING_BITS(0,1,2) },      // 1100
    { decoded_end, DECODING_BITS(1,1,2) },      // 1101
    { decoded_end, DECODING_BITS(0,0,3) },      // 1110
    { decoded_end, DECODING_HEADER(4) },        // 1111
  },
    //header(1)
  {
    { decoded_1_0_0, DECODING_HEADER(0) },      // 1 0000
    { decoded_1_0, DECODING_HEADER(1) },        // 1 0001
    { decoded_1, DECODING_BITS(0,0,1) },        // 1 0010
    { decoded_1, DECODING_HEADER(2) },          // 1 0011
    { decoded_2_0_0, DECODING_HEADER(0) },      // 1 0100
    { decoded_2_0, DECODING_HEADER(1) },        // 1 0101
    { decoded_2, DECODING_BITS(0,0,1) },        // 1 0110
    { decoded_2, DECODING_HEADER(2) },          // 1 0111
    { decoded_end, DECODING_BITS(0,2,2) },      // 1 1000
    { decoded_end, DECODING_BITS(1,2,2) },      // 1 1001
    { decoded_end, DECODING_BITS(2,2,2) },      // 1 1010
    { decoded_end, DECODING_BITS(3,2,2) },      // 1 1011
    { decoded_end, DECODING_BITS(0,1,3) },      // 1 1100
    { decoded_end, DECODING_BITS(1,1,3) },      // 1 1101
    { decoded_end, DECODING_BITS(0,0,4) },      // 1 1110
    { decoded_end, DECODING_HEADER(5) },        // 1 1111
  },
    //header(2)
  {
    { decoded_3, DECODING_HEADER(0) },          // 11 0000
    { decoded_4, DECODING_HEADER(0) },          // 11 0001
    { decoded_5, DECODING_HEADER(0) },          // 11 0010
    { decoded_6, DECODING_HEADER(0) },          // 11 0011
    { decoded_7, DECODING_HEADER(0) },          // 11 0100
    { decoded_8, DECODING_HEADER(0) },          // 11 0101
    { decoded_9, DECODING_HEADER(0) },          // 11 0110
    { decoded_10, DECODING_HEADER(0) },         // 11 0111
    { decoded_end, DECODING_BITS(0,2,3) },      // 11 1000
    { decoded_end, DECODING_BITS(1,2,3) },      // 11 1001
    { decoded_end, DECODING_BITS(2,2,3) },      // 11 1010
    { decoded_end, DECODING_BITS(3,2,3) },      // 11 1011
    { decoded_end, DECODING_BITS(0,1,4) },      // 11 1100
    { decoded_end, DECODING_BITS(1,1,4) },      // 11 1101
    { decoded_end, DECODING_BITS(0,0,5) },      // 11 1110
    { decoded_end, DECODING_ERROR },            // 11 1111
  },
    //header(3)
  {
    { decoded_end, DECODING_BITS(0,3,3) },
    { decoded_end, DECODING_BITS(1,3,3) },
    { decoded_end, DECODING_BITS(2,3,3) },
    { decoded_end, DECODING_BITS(3,3,3) },
    { decoded_end, DECODING_BITS(4,3,3) },
    { decoded_end, DECODING_BITS(5,3,3) },
    { decoded_end, DECODING_BITS(6,3,3) },
    { decoded_end, DECODING_BITS(7,3,3) },
    { decoded_end, DECODING_BITS(0,2,4) },
    { decoded_end, DECODING_BITS(1,2,4) },
    { decoded_end, DECODING_BITS(2,2,4) },
    { decoded_end, DECODING_BITS(3,2,4) },
    { decoded_end, DECODING_BITS(0,1,5) },
    { decoded_end, DECODING_BITS(1,1,5) },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
  },
    //header(4)
  {
    { decoded_end, DECODING_BITS(0,3,4) },
    { decoded_end, DECODING_BITS(1,3,4) },
    { decoded_end, DECODING_BITS(2,3,4) },
    { decoded_end, DECODING_BITS(3,3,4) },
    { decoded_end, DECODING_BITS(4,3,4) },
    { decoded_end, DECODING_BITS(5,3,4) },
    { decoded_end, DECODING_BITS(6,3,4) },
    { decoded_end, DECODING_BITS(7,3,4) },
    { decoded_end, DECODING_BITS(0,2,5) },
    { decoded_end, DECODING_BITS(1,2,5) },
    { decoded_end, DECODING_BITS(2,2,5) },
    { decoded_end, DECODING_BITS(3,2,5) },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
  },
    //header(5)
  {
    { decoded_end, DECODING_BITS(0,3,5) },
    { decoded_end, DECODING_BITS(1,3,5) },
    { decoded_end, DECODING_BITS(2,3,5) },
    { decoded_end, DECODING_BITS(3,3,5) },
    { decoded_end, DECODING_BITS(4,3,5) },
    { decoded_end, DECODING_BITS(5,3,5) },
    { decoded_end, DECODING_BITS(6,3,5) },
    { decoded_end, DECODING_BITS(7,3,5) },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
    { decoded_end, DECODING_ERROR },
  }
};

// --------------------------------------------------------
void Decoder::Nibbles::SetContents( PTR_BYTE bytes)
{
    STATIC_CONTRACT_LEAF;

    next = 2;
    data = bytes;
}

// --------------------------------------------------------
BYTE Decoder::Nibbles::Next()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    BYTE result = Read();
    next++;
    return result;
}

// --------------------------------------------------------
BYTE Decoder::Nibbles::Read()
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;

    if (next >= 2)
    {
        BYTE d = *data++;
        next = 0;
        nibbles[1] = d & 0xF;
        nibbles[0] = d>>4;
    }
    return nibbles[next];
}

// --------------------------------------------------------
unsigned Decoder::Nibbles::Bits(unsigned number)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    unsigned n = number;
    unsigned result = 0;
    while (n >= 4 )
    {
        result = (result<<4) | Next();
        n -= 4;
    }
    if (n > 0)
    {
        BYTE last = Read();
        result = (result<<n) | (last>>(4-n));
        nibbles[next] &= (0xF>>n);
    }
    return result;
}

// --------------------------------------------------------
void Decoder::Init(PTR_BYTE bytes)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

    state = emptyDecode;
    data.SetContents(bytes);
//        signedNumbers = FALSE;
}

// --------------------------------------------------------
Decoder::Decoder(PTR_BYTE bytes)
{
    STATIC_CONTRACT_WRAPPER;
    Init(bytes);
}

// --------------------------------------------------------
Decoder::Decoder()
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
}

// --------------------------------------------------------
unsigned Decoder::Next()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

tryagain:
    unsigned result = *state.decoded;
    if (result != END_DECODED)
    {
        state.decoded++;
        return result;
    }
    if (INHEADER(state.next))
    {
        state = transition[state.next][data.Next()];
        goto tryagain;
    }
    //must be getting bits
    _ASSERTE(INBITS(state.next));
    unsigned index = HEADER(state.next);
    unsigned bitsNeeded = decode_bitlength[index]-NUMBERGOTTEN(state.next);
    result = (PARTIALBITS(state.next)<<bitsNeeded)+data.Bits(bitsNeeded)+decode_base[index];
    state = emptyDecode;
    unsigned skip = bitsNeeded % 4; // this works since we are always 4-bit aligned
    if (skip > 0)
    {
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:26000) // "Suppress PREFast warning about index overflow"
#endif
        // state.next is always 0, because we did "state = emptyDecode;" above
        state = transition[state.next][data.Next()];
#ifdef _PREFAST_
#pragma warning(pop)
#endif
        state.decoded += skip;
    }
    return result;
}

// --------------------------------------------------------
signed Decoder::NextSigned()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    signed v = (signed) Next();
    return (v & 1) ? (v+1)>>1 : -(v>>1);
}

// --------------------------------------------------------
PTR_BYTE Decoder::End()
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;

    return data.data;
}

// --------------------------------------------------------
Encoder::Encoder(BYTE *buffer) : encoding(0), unusedBits(BITS_PER_BYTE),
                         done(FALSE), signedNumbers(FALSE), index(0)
{
    STATIC_CONTRACT_LEAF;

    this->buffer = buffer;
}

// --------------------------------------------------------
void Encoder::ContainsNegatives(BOOL b)
{
    STATIC_CONTRACT_LEAF;

    signedNumbers = b;
}
void Encoder::EncodeSigned(signed value)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;


    if (!signedNumbers)
    {
        _ASSERTE(value>=0);
        Encode(value);
        return;
    }
    unsigned v = (value <= 0 ) ? (-value)<<1 : (value<<1)-1;
    Encode(v);
}

// --------------------------------------------------------
void Encoder::Encode(unsigned value)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;


    if (value < BASE_1)
    {
        Add(0, 1);
        return;
    }
    if (value < BASE_2)
    {
        Add((0x1<<(1+BIT_LENGTH_1))+(value-BASE_1), 2+BIT_LENGTH_1);
        return;
    }
    if (value < BASE_3)
    {
        Add((0x3<<(1+BIT_LENGTH_2))+(value-BASE_2), 3+BIT_LENGTH_2);
        return;
    }
    if (value < BASE_4)
    {
        Add((0x7<<(1+BIT_LENGTH_3))+(value-BASE_3), 4+BIT_LENGTH_3);
        return;
    }
    if (value < BASE_5)
    {
        Add((0xf<<(1+BIT_LENGTH_4))+(value-BASE_4), 5+BIT_LENGTH_4);
        return;
    }
    if (value < BASE_6)
    {
        unsigned __int64 value64 = (unsigned __int64) value;
        Add64((((unsigned __int64)0x1f)<<(1+BIT_LENGTH_5))+(value64-BASE_5), 6+BIT_LENGTH_5);
        return;
    }
    _ASSERTE(!"Too big");
}

// --------------------------------------------------------
void Encoder::Encode(signed value, BOOL isSigned)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (isSigned)
        EncodeSigned(value);
    else
    {
        _ASSERTE(((signed)((unsigned) value)) == value);
        Encode((unsigned) value);
    }
}

// --------------------------------------------------------
void Encoder::Add(unsigned value, unsigned length)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE(!done);
    while (length >= unusedBits)
    {
        length -= unusedBits;
        encoding = (encoding<<unusedBits)+static_cast<BYTE>(value>>(length));
        value = (value & MASK(length));
        if (buffer) buffer[index++] = encoding;
        else index++;
        encoding = 0;
        unusedBits = BITS_PER_BYTE;
    }
    encoding = (encoding<<length)+static_cast<BYTE>(value);
    unusedBits -= length;
}

// --------------------------------------------------------
void Encoder::Add64(unsigned __int64 value, unsigned length)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE(!done);
    while (length >= unusedBits)
    {
        length -= unusedBits;
        encoding = (encoding<<unusedBits)+((BYTE)(value>>(length)));
        value = (value & MASK64(length));
        if (buffer) buffer[index++] = encoding;
        else index++;
        encoding = 0;
        unusedBits = BITS_PER_BYTE;
    }
    encoding = (encoding<<length)+(BYTE)value;
    unusedBits -= length;
}

// --------------------------------------------------------
void Encoder::Done()
{
    LIMITED_METHOD_CONTRACT;

    done = TRUE;
    if (unusedBits == BITS_PER_BYTE) return;
    encoding = (encoding<<unusedBits);
    if (buffer) buffer[index++] = encoding;
    else index++;
}

// --------------------------------------------------------
unsigned Encoder::Contents(BYTE** contents)
{
    STATIC_CONTRACT_LEAF;

    _ASSERTE(done && buffer && contents);
    *contents = buffer;
    return index;
}

// --------------------------------------------------------
unsigned Encoder::Length()
{
    STATIC_CONTRACT_LEAF;

    _ASSERTE(done);
    return index;
}

