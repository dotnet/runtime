// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***************************************************************************/
/*                           BitVector.cpp                                 */
/***************************************************************************/
//  Routines to support a growable bitvector
/***************************************************************************/

#include "stdafx.h"
#include <memory.h>

#include "utilcode.h"

#include "bitvector.h"

#if USE_BITVECTOR

int  BitVector::NumBits() const
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    int count = 0;
    ChunkType hiChunk;

    if (isBig())
    {
        unsigned maxNonZero = 0;
        for (unsigned i=1; (i < m_vals.GetLength()); i++)
        {
            if (m_vals.m_chunks[i] != 0)
            {
                maxNonZero = i;
            }
        }
        count = (maxNonZero * CHUNK_BITS) - 1;
        hiChunk = m_vals.m_chunks[maxNonZero];
    }
    else
    {
        hiChunk = m_val;
    }

    while (hiChunk > 0)
    {
        hiChunk <<= 1;
        count++;
    }

    _ASSERTE(count >= 0);
    return count;
}

void BitVector::doBigInit(ChunkType arg)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    m_vals.m_chunks[0] = arg;
    m_vals.SetLength(1);
}

void BitVector::doBigInit(const BitVector& arg)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (arg.isBig())
    {
        memcpy(m_vals.m_chunks, arg.m_vals.m_chunks, (sizeof(ChunkType) * arg.m_vals.GetLength()));
        m_vals.SetLength(arg.m_vals.GetLength());
    }
    else
    {
        m_val = arg.m_val;
    }
}

void BitVector::doBigLeftShiftAssign(unsigned shift)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    if ((m_val == 0) || (shift == 0))     // Zero is a special case, don't need to do anything
        return;

    unsigned numWords = shift / CHUNK_BITS;
    unsigned numBits  = shift % CHUNK_BITS;

    //
    // Change to Big representation
    //
    toBig();

    int       from    = m_vals.GetLength()-1;
    int       to      = from + numWords;
    unsigned  newlen  = to + 1;

    ChunkType topBits = 0;
    if (numBits > 0)
    {
        topBits = m_vals.m_chunks[from] >> (CHUNK_BITS - numBits);
    }

    if (topBits != 0 || numWords != 0)
    {
        if (topBits != 0)
        {
            m_vals.m_chunks[newlen] = topBits;
            newlen++;
        }
        m_vals.SetLength(newlen);
    }

    while (to >= 0)
    {
        m_vals.m_chunks[to] = (from >= 0) ? (m_vals.m_chunks[from] << numBits) : 0;
        from--;

        if ((from >= 0) && (numBits > 0))
        {
            m_vals.m_chunks[to] |= m_vals.m_chunks[from] >> (CHUNK_BITS - numBits);
        }
        to--;
    }

    // Convert back to small format if necessary
    if ((newlen == 1) && (m_vals.m_chunks[0] <= MaxVal))
    {
        m_val = ChunkType(m_vals.m_chunks[0] << 1);
    }
}

void BitVector::doBigRightShiftAssign(unsigned shift)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    if ((m_val == 0) || (shift == 0))     // Zero is a special case, don't need to do anything
        return;

    unsigned   numWords = shift / CHUNK_BITS;
    unsigned   numBits  = shift % CHUNK_BITS;

    //
    // Change to Big representation
    //
    toBig();

    unsigned  from   = numWords;
    unsigned  to     = 0;
    unsigned  len    = m_vals.GetLength();
    unsigned  newlen = len - numWords;

    if (from >= len)
    {
        // we always encode zero in short form
        m_val = 0;
    }
    else
    {
        m_vals.m_chunks[to] = (m_vals.m_chunks[from] >> numBits);
        from++;

        while (from < len)
        {
            if (numBits > 0)
            {
                m_vals.m_chunks[to] |= m_vals.m_chunks[from] << (CHUNK_BITS - numBits);
            }
            to++;

            m_vals.m_chunks[to] = (m_vals.m_chunks[from] >> numBits);
            from++;
        }

        if ((newlen > 1) && (m_vals.m_chunks[newlen-1] == 0))
        {
            newlen--;
        }

        m_vals.SetLength(newlen);

        // Convert back to small format if necessary
        if ((newlen == 1) && (m_vals.m_chunks[0] <= MaxVal))
        {
            m_val = ChunkType(m_vals.m_chunks[0] << 1);
        }
    }
}

void BitVector::doBigAndAssign(const BitVector& arg)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    //
    // Change to Big representation
    //
    toBig();

    if (arg.isBig())
    {
        bool     isZero = true;   // until proven otherwise
        unsigned myLen  = m_vals.GetLength();
        unsigned argLen = arg.m_vals.GetLength();

        if (myLen > argLen)
        {
            // shrink our length to match argLen
            m_vals.SetLength(argLen);
            myLen = argLen;
        }

        for (unsigned i = 0; (i < myLen); i++)
        {
            ChunkType curChunk = m_vals.m_chunks[i] & arg.m_vals.m_chunks[i];
            m_vals.m_chunks[i] = curChunk;
            if (curChunk != 0)
                isZero = false;
        }

        if (isZero)
        {
            // we always encode zero in short form
            m_val = 0;
        }
    }
    else
    {
        m_val = (m_vals.m_chunks[0] << 1) & arg.m_val;
    }
}

void BitVector::doBigOrAssign(const BitVector& arg)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    //
    // Change to Big representation
    //
    toBig();

    if (arg.isBig())
    {
        unsigned myLen  = m_vals.GetLength();
        unsigned argLen = arg.m_vals.GetLength();

        if (myLen < argLen)
        {
            // expand our length to match argLen and zero init
            memset(m_vals.m_chunks + myLen, 0, sizeof(ChunkType) * (argLen - myLen));
            m_vals.SetLength(argLen);
            myLen = argLen;
        }

        for(unsigned i = 0; ((i < myLen) && (i < argLen)); i++)
        {
            m_vals.m_chunks[i] |= arg.m_vals.m_chunks[i];
        }
    }
    else
    {
        m_vals.m_chunks[0] |= arg.smallBits();
    }
}

void BitVector::doBigDiffAssign(const BitVector& arg)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    //
    // Change to Big representation
    //
    toBig();

    unsigned myLen  = m_vals.GetLength();
    unsigned argLen = arg.m_vals.GetLength();
    bool     isZero = true;                    // until proven otherwise

    for (unsigned i = 0; (i < myLen); i++)
    {
        ChunkType nextChunk = m_vals.m_chunks[i];
        if (i < argLen)
        {
            nextChunk &= ~arg.m_vals.m_chunks[i];
            m_vals.m_chunks[i] = nextChunk;
        }
        else if (i == 0)
        {
            nextChunk &= ~arg.smallBits();
            m_vals.m_chunks[i] = nextChunk;
        }

        if (nextChunk != 0)
            isZero = false;
    }

    if (isZero)
    {
        // we always encode zero in short form
        m_val = 0;
    }
}

BOOL BitVector::doBigEquals(const BitVector& arg) const
{
    CONTRACT(BOOL)
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    unsigned myLen  = m_vals.GetLength();
    unsigned argLen = arg.m_vals.GetLength();
    unsigned maxLen = (myLen >= argLen) ? myLen : argLen;

    for (unsigned i=0; (i < maxLen); i++)
    {
        ChunkType myVal  = 0;
        ChunkType argVal = 0;

        if (i < myLen)
            myVal = m_vals.m_chunks[i];

        if (i < argLen)
            argVal = arg.m_vals.m_chunks[i];

        if (i == 0)
        {
            if (myLen == 0)
                myVal = smallBits();
            if (argLen == 0)
                argVal = arg.smallBits();
        }

        if (myVal != argVal)
            RETURN false;
    }
    RETURN true;
}

BOOL BitVector::doBigIntersect(const BitVector& arg) const
{
    CONTRACT(BOOL)
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    unsigned myLen  = m_vals.GetLength();
    unsigned argLen = arg.m_vals.GetLength();
    unsigned minLen = (myLen <= argLen) ? myLen : argLen;

    for (unsigned i=0; (i <= minLen); i++)
    {
        ChunkType myVal  = 0;
        ChunkType argVal = 0;

        if (i < myLen)
            myVal = m_vals.m_chunks[i];

        if (i < argLen)
            argVal = arg.m_vals.m_chunks[i];

        if (i == 0)
        {
            if (myLen == 0)
                myVal = smallBits();
            if (argLen == 0)
                argVal = arg.smallBits();
        }

        if ((myVal & argVal) != 0)
            RETURN true;
    }
    RETURN false;
}

#endif // USE_BITVECTOR
