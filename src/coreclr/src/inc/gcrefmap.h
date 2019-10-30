// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _GCREFMAP_H_
#define _GCREFMAP_H_

#include "sigbuilder.h"

//
// The GCRef map is used to encode GC type of arguments for callsites. Logically, it is sequence <pos, token> where pos is 
// position of the reference in the stack frame and token is type of GC reference (one of GCREFMAP_XXX values).
//
// - The encoding always starts at the byte boundary. The high order bit of each byte is used to signal end of the encoding 
// stream. The last byte has the high order bit zero. It means that there are 7 useful bits in each byte.
// - "pos" is always encoded as delta from previous pos.
// - The basic encoding unit is two bits. Values 0, 1 and 2 are the common constructs (skip single slot, GC reference, interior 
// pointer). Value 3 means that extended encoding follows. 
// - The extended information is integer encoded in one or more four bit blocks. The high order bit of the four bit block is 
// used to signal the end.
// - For x86, the encoding starts by size of the callee poped stack. The size is encoded using the same mechanism as above (two bit
// basic encoding, with extended encoding for large values).

/////////////////////////////////////////////////////////////////////////////////////
// A utility class to encode sequence of GC summaries for a callsite

class GCRefMapBuilder
{
    int m_PendingByte;  // Pending value, not yet written out

    int m_Bits;         // Number of bits in pending byte. Note that the trailing zero bits are not written out, 
                        // so this can be more than 7.

    int m_Pos;          // Current position

    SigBuilder m_SigBuilder;

    // Append single bit to the stream
    void AppendBit(int bit)
    {
        if (bit != 0)
        {
            while (m_Bits >= 7)
            {
                m_SigBuilder.AppendByte((BYTE)(m_PendingByte | 0x80));
                m_PendingByte = 0;
                m_Bits -= 7;
            }
        
            m_PendingByte |= (1 << m_Bits);
        }

        m_Bits++;
    }

    void AppendTwoBit(int bits)
    {
        AppendBit(bits & 1);
        AppendBit(bits >> 1);
    }

    void AppendInt(int val)
    {
        do {
            AppendBit(val & 1);
            AppendBit((val >> 1) & 1);
            AppendBit((val >> 2) & 1);

            val >>= 3;

            AppendBit((val != 0) ? 1 : 0);
        }
        while (val != 0);
    }

public:
    GCRefMapBuilder()
        : m_PendingByte(0), m_Bits(0), m_Pos(0)
    {
    }

#ifdef _TARGET_X86_
    void WriteStackPop(int stackPop)
    {
        if (stackPop < 3)
        {
            AppendTwoBit(stackPop);
        }
        else
        {
            AppendTwoBit(3);
            AppendInt(stackPop - 3);
        }
    }
#endif

    void WriteToken(int pos, int gcRefMapToken)
    {
        int posDelta = pos - m_Pos;
        m_Pos = pos + 1;

        if (posDelta != 0)
        {
            if (posDelta < 4)
            {
                // Skipping by one slot at a time for small deltas produces smaller encoding.
                while (posDelta > 0)
                {
                    AppendTwoBit(0);
                    posDelta--;
                }
            }
            else
            {
                AppendTwoBit(3);
                AppendInt((posDelta - 4) << 1);
            }
        }

        if (gcRefMapToken < 3)
        {
            AppendTwoBit(gcRefMapToken);
        }
        else
        {
            AppendTwoBit(3);
            AppendInt(((gcRefMapToken - 3) << 1) | 1);
        }
    }

    void Flush()
    {
        if ((m_PendingByte & 0x7F) != 0 || m_Pos == 0)
            m_SigBuilder.AppendByte((BYTE)(m_PendingByte & 0x7F));

        m_PendingByte = 0;
        m_Bits = 0;

        m_Pos = 0;
    }

    PVOID GetBlob(DWORD * pdwLength)
    {
        return m_SigBuilder.GetSignature(pdwLength);
    }

    DWORD GetBlobLength()
    {
        return m_SigBuilder.GetSignatureLength();
    }
};

/////////////////////////////////////////////////////////////////////////////////////
// A utility class to decode a GC summary for a callsite

class GCRefMapDecoder
{
    PTR_BYTE m_pCurrentByte;
    int m_PendingByte;
    int m_Pos;

    FORCEINLINE int GetBit()
    {
        int x = m_PendingByte;
        if (x & 0x80)
        {
            x = *m_pCurrentByte++;
            x |= ((x & 0x80) << 7);
        }
        m_PendingByte = x >> 1;
        return x & 1;
    }

    FORCEINLINE int GetTwoBit()
    {
        int result = GetBit();
        result |= GetBit() << 1;
        return result;
    }

    int GetInt()
    {
        int result = 0;

        int bit = 0;
        do {
            result |= GetBit() << (bit++);
            result |= GetBit() << (bit++);
            result |= GetBit() << (bit++);
        }
        while (GetBit() != 0);

        return result;
    }

public:
    GCRefMapDecoder(PTR_BYTE pBlob)
        : m_pCurrentByte(pBlob), m_PendingByte(0x80), m_Pos(0)
    {
    }

    BOOL AtEnd()
    {
        return m_PendingByte == 0;
    }

#ifdef _TARGET_X86_
    UINT ReadStackPop()
    {
        int x = GetTwoBit();

        if (x == 3)
            x = GetInt() + 3;

        return x;
    }
#endif

    int CurrentPos()
    {
        return m_Pos;
    }

    int ReadToken()
    {
        int val = GetTwoBit();
        if (val == 3)
        {
            int ext = GetInt();
            if ((ext & 1) == 0)
            {
                m_Pos += (ext >> 1) + 4;
                return GCREFMAP_SKIP;
            }
            else
            {
                m_Pos++;
                return (ext >> 1) + 3;
            }
        }
        m_Pos++;
        return val;
    }
};

#endif // _GCREFMAP_H_
