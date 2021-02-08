// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DBG_PORTABLE_INCLUDED
#define __DBG_PORTABLE_INCLUDED

//
// This header defines the template class Portable<T> which is designed to wrap primitive types in such a way
// that their physical representation is in a canonical format that can be safely transferred between hosts on
// different platforms.
//
// This is achieved by storing the wrapped datum in little-endian format (since most of our platforms are
// little-endian this makes the most sense from a performance perspective). On little-endian platforms the
// wrapper code will become a no-op and get optimized away by the compiler. On big-endian platforms
// assignments to a Portable<T> value will reverse the order of the bytes in the T value and reverse them back
// again on a read.
//
// Portable<T> is typically used to wrap the fields of structures sent directly over a network channel. In
// this fashion many of the values that would otherwise require manual endian-ness fixups are now marshalled
// and unmarshalled transparent right at the network transition.
//
// Care must be taken to identify any code that takes the address of a Portable<T>, since this is not
// generally safe (it could expose naive code to the network encoded form of the datum). In such situations
// the code is normally re-written to create a temporary instance of T on the stack, initialized to the
// correct host value by reading from the Portable<T> field. The address of this variable can now be taken
// safely (assuming its value is required only for some lexically scoped operation). Once the value is no
// longer being used, and if there is a possibility that the value may have been updated, the new value can be
// copied back into the Portable<T> field.
//
// Note that this header uses very basic data types only as it is included from both Win32/PAL code and native
// Mac code.
//

#if BIGENDIAN || __BIG_ENDIAN__
#define DBG_BYTE_SWAP_REQUIRED
#endif

#if defined(_ASSERTE)
#define _PASSERT(_expr) _ASSERTE(_expr)
#elif defined(assert)
#define _PASSERT(_expr) assert(_expr)
#else
#define _PASSERT(_expr)
#endif

// Lowest level helper used to reverse the order of a sequence of bytes, either as an in-place operation or as
// part of a copy.
inline void ByteSwapPrimitive(const void *pSrc, void *pDst, unsigned int cbSize)
{
    _PASSERT(cbSize == 2 || cbSize == 4 || cbSize == 8);

    unsigned char *pbSrc = (unsigned char*)pSrc;
    unsigned char *pbDst = (unsigned char*)pDst;

    for (unsigned int i = 0; i < (cbSize / 2); i++)
    {
        unsigned int j = cbSize - i - 1;
        unsigned char bTemp = pbSrc[i];
        pbDst[i] = pbSrc[j];
        pbDst[j] = bTemp;
    }
}

template <typename T>
class Portable
{
    T m_data;

public:
    // No constructors -- this will be used in unions.

    // Convert data to portable format on assignment.
    T operator = (T value)
    {
        _PASSERT(sizeof(value) <= sizeof(double));
#ifdef DBG_BYTE_SWAP_REQUIRED
        m_data = ByteSwap(value);
#else // DBG_BYTE_SWAP_REQUIRED
        m_data = value;
#endif // DBG_BYTE_SWAP_REQUIRED
        return value;
    }

    // Return data in native format on access.
    operator T () const
    {
#ifdef DBG_BYTE_SWAP_REQUIRED
        return ByteSwap(m_data);
#else // DBG_BYTE_SWAP_REQUIRED
        return m_data;
#endif // DBG_BYTE_SWAP_REQUIRED
    }

    bool operator == (T other) const
    {
#ifdef DBG_BYTE_SWAP_REQUIRED
        return ByteSwap(m_data) == other;
#else // DBG_BYTE_SWAP_REQUIRED
        return m_data == other;
#endif // DBG_BYTE_SWAP_REQUIRED
    }

    bool operator != (T other) const
    {
#ifdef DBG_BYTE_SWAP_REQUIRED
        return ByteSwap(m_data) != other;
#else // DBG_BYTE_SWAP_REQUIRED
        return m_data != other;
#endif // DBG_BYTE_SWAP_REQUIRED
    }

    T Unwrap()
    {
#ifdef DBG_BYTE_SWAP_REQUIRED
        return ByteSwap(m_data);
#else // DBG_BYTE_SWAP_REQUIRED
        return m_data;
#endif // DBG_BYTE_SWAP_REQUIRED
    }

private:
#ifdef DBG_BYTE_SWAP_REQUIRED
    // Big endian helper routine to swap the order of bytes of an arbitrary sized type
    // (though obviously this type must be an integral primitive for this to make any
    // sense).
    static T ByteSwap(T inval)
    {
        if (sizeof(T) > 1)
        {
            T outval;
            ByteSwapPrimitive(&inval, &outval, sizeof(T));
            return outval;
        }
        else
            return inval;
    }
#endif // DBG_BYTE_SWAP_REQUIRED
};

#endif // !__DBG_PORTABLE_INCLUDED
