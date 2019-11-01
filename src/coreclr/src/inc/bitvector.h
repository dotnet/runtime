// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/***************************************************************************/
/*                           BitVector.h                                   */
/***************************************************************************/
//  Routines to support a growable bitvector
/***************************************************************************/

#ifndef BITVECTOR_H
#define BITVECTOR_H 1


#ifndef LIMITED_METHOD_CONTRACT
#define LIMITED_METHOD_CONTRACT
#define UNDEF_LIMITED_METHOD_CONTRACT
#endif

#ifndef WRAPPER_NO_CONTRACT
#define WRAPPER_NO_CONTRACT
#define UNDEF_WRAPPER_NO_CONTRACT
#endif

#ifndef SUPPORTS_DAC
#define SUPPORTS_DAC
#define UNDEF_SUPPORTS_DAC
#endif

#ifndef _ASSERTE
#define _ASSERTE(x)
#define UNDEF_ASSERTE
#endif

#define USE_BITVECTOR 1
#if USE_BITVECTOR

/* The bitvector class is meant to be a drop in replacement for an integer
   (that is you use it like an integer), however it grows as needed.

   Features:
       plug compatible with normal integers;
       grows as needed
       Optimized for the small case when the vector fits in machine word
       Uses one machine word if vector fits in machine word (minus a bit)

       Some caveates:
           You should use mutator operators  &=, |= ... instead of the 
           non-mutators whenever possible to avoid creating a temps

           Specifically did NOT supply automatic coersions to
           and from short types so that the programmer is aware of
           when code was being injected on his behalf.  The upshot of this
           is that you have to use the  BitVector() toUnsigned() to convert 
*/

/***************************************************************************/

class BitVector {
    // Set this to be unsigned char to do testing, should be UINT_PTR for real life

    typedef UINT_PTR ChunkType;  // The size of integer type that the machine can operate on directly  
//  typedef BYTE ChunkType;      // Use for testing

    // Maximum number of bits in our bitvector
#define MAX_PTRARG_OFS 1024

    enum {
        IS_BIG     = 1,                             // The low bit is used to discrimate m_val and m_vals
        CHUNK_BITS = sizeof(ChunkType)*8,           // The number of bits that we can manipuate as a chunk
        SMALL_BITS = CHUNK_BITS - 1,                // The number of bits we can fit in the small representation
//      SMALL_BITS = 5,                             // TESTING ONLY: The number of bits we can fit in the small representation
        VALS_COUNT = MAX_PTRARG_OFS / CHUNK_BITS,   // The number of ChunkType elements in the Vals array
    };

public:
    BitVector()
    { 
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        m_val = 0;
    }

    BOOL isBig() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return ((m_val & IS_BIG) != 0);
    }

    void toBig()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        if (!isBig())
        {
            doBigInit(smallBits());
        }
    }

    explicit BitVector(ChunkType arg)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (arg > MaxVal)
        {
            doBigInit(arg);
        }
        else 
        {
            m_val = ChunkType(arg << 1);
        }
    }

    BitVector(ChunkType arg, UINT shift)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if ((arg > MaxVal) || (shift >= SMALL_BITS) || (arg > (MaxVal >> shift)))
        {
            doBigInit(arg);
            doBigLeftShiftAssign(shift);
        }
        else
        {
            m_val = ChunkType(arg << (shift+1));
        }
    }

#define CONSTRUCT_ptrArgTP(arg,shift)   BitVector((arg), (shift))

    BitVector(const BitVector& arg)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (arg.isBig())
        {
            doBigInit(arg);
        }
        else
        {
            m_val = arg.m_val;
        }
    }
    
    void operator <<=(unsigned shift)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if ((m_val == 0) || (shift == 0))     // Zero is a special case, don't need to do anything
            return;

        if (isBig() || (shift >= SMALL_BITS) || (m_val > (MaxVal >> (shift-1))))
        {
            doBigLeftShiftAssign(shift);
        }
        else 
        {
            m_val <<= shift;
        }
    }

    void operator >>=(unsigned shift)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (isBig())
        {
            doBigRightShiftAssign(shift);
        }
        else
        {
            m_val >>= shift;
            m_val &= ~IS_BIG;  // clear the isBig bit if it got set
        }
    }
    
    void operator |=(const BitVector& arg)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (((m_val | arg.m_val) & IS_BIG) != 0)
        {
            doBigOrAssign(arg);
        }
        else
        {
            m_val |= arg.m_val;
        }
    }

    // Note that that is set difference, not subtration
    void operator -=(const BitVector& arg)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (((m_val | arg.m_val) & IS_BIG) != 0)
        {
            doBigDiffAssign(arg);
        }
        else
        {
            m_val &= ~arg.m_val;
        }
    }

    void operator &=(const BitVector& arg)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (((m_val | arg.m_val) & IS_BIG) != 0)
        {
            doBigAndAssign(arg);
        }
        else
        {
            m_val &= arg.m_val;
        }
    }

    friend void setDiff(BitVector& target, const BitVector& arg)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        target -= arg;
    }

    friend BOOL intersect(const BitVector& arg1, const BitVector& arg2)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (((arg1.m_val | arg2.m_val) & IS_BIG) != 0)
        {
            return arg1.doBigIntersect(arg2);
        }
        else
        {
            return ((arg1.m_val & arg2.m_val) != 0);
        }
    }
    
    BOOL operator ==(const BitVector& arg) const
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if ((m_val | arg.m_val) & IS_BIG)
        {
            return doBigEquals(arg);
        }
        else
        {
            return m_val == arg.m_val;
        }
    }

    BOOL operator !=(const BitVector& arg) const
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return !(*this == arg);
    }

    friend ChunkType toUnsigned(const BitVector& arg)
    { 
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (arg.isBig())
        {
            return arg.m_vals.m_chunks[0];   // Note truncation
        }
        else 
        {
            return arg.smallBits();
        }
    }

    // Note that we require the invariant that zero is always stored in the
    // small form so that this works bitvector is zero iff (m_val == 0)
    friend BOOL isZero(const BitVector& arg)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return arg.m_val == 0;
    }

    /* currently only used in asserts */
    BitVector operator &(const BitVector& arg) const
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        BitVector ret = *this;
        ret &= arg;
        return ret;
    }

    int  NumBits() const;

private:

    static const ChunkType MaxVal = ((ChunkType)1 << SMALL_BITS) - 1;    // Maximum value that can be stored in m_val

    // This is the structure that we use when the bit vector overflows.  
    // It is a simple vector.  
    struct Vals {
        unsigned m_encodedLength;         // An encoding of the current length of the 'm_chunks' array
        ChunkType m_chunks[VALS_COUNT]; 

        BOOL isBig() const
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;

            return ((m_encodedLength & IS_BIG) != 0);
        }

        unsigned GetLength() const
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;
            
            if (isBig())
            {
                unsigned length = (m_encodedLength >> 1);
                _ASSERTE(length > 0);
                return length;
            }
            else
            {
                return 0;
            }
        }

        void SetLength(unsigned length)
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;

            _ASSERTE(length > 0);
            _ASSERTE(length <= VALS_COUNT);

            m_encodedLength  = (ChunkType) (length << 1);
            m_encodedLength |= (ChunkType) IS_BIG;
         }
    };

    //
    // This is the instance data for the bitvector
    //
    // We discrimininate on this
    union {
        ChunkType m_val;     // if m_val bit 0 is false, then bits 1-N are the bit vector
        Vals      m_vals;    // if m_val bit 1 is true, then use Vals
    };


    ChunkType smallBits() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        _ASSERTE(!isBig());
        return (m_val >> 1);
    }

#ifdef STRIKE
    void doBigInit(ChunkType arg) {}
#else
    void doBigInit(ChunkType arg);
#endif
    void doBigInit(const BitVector& arg);
    void doBigLeftShiftAssign(unsigned arg);
    void doBigRightShiftAssign(unsigned arg);
    void doBigDiffAssign(const BitVector&);
    void doBigAndAssign(const BitVector&);
    void doBigOrAssign(const BitVector& arg);
    BOOL doBigEquals(const BitVector&) const;
    BOOL doBigIntersect(const BitVector&) const;
};

typedef BitVector ptrArgTP;

#else // !USE_BITVECTOR

typedef unsigned __int64 ptrArgTP;

    // Maximum number of bits in our bitvector
#define MAX_PTRARG_OFS (sizeof(ptrArgTP) * 8)

#define CONSTRUCT_ptrArgTP(arg,shift)   (((ptrArgTP) (arg)) << (shift))

inline BOOL isZero(const ptrArgTP& arg)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return (arg == 0);
}

inline ptrArgTP toUnsigned(const ptrArgTP& arg)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return arg;
}

inline void setDiff(ptrArgTP& target, const ptrArgTP& arg)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    target &= ~arg;
}

inline BOOL intersect(const ptrArgTP arg1, const ptrArgTP arg2)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return ((arg1 & arg2) != 0);
}

#endif  // !USE_BITVECTOR

#ifdef UNDEF_LIMITED_METHOD_CONTRACT
#undef LIMITED_METHOD_CONTRACT
#undef UNDEF_LIMITED_METHOD_CONTRACT
#endif

#ifdef UNDEF_WRAPPER_NO_CONTRACT
#undef WRAPPER_NO_CONTRACT
#undef UNDEF_WRAPPER_NO_CONTRACT
#endif

#ifdef UNDEF_SUPPORTS_DAC
#undef SUPPORTS_DAC
#undef UNDEF_SUPPORTS_DAC
#endif

#ifdef UNDEF_ASSERTE
#undef _ASSERTE
#undef UNDEF_ASSERTE
#endif

#endif // BITVECTOR_H
