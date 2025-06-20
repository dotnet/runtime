// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// NibbleStream reader and writer.


#ifndef _NIBBLESTREAM_H_
#define _NIBBLESTREAM_H_

#include "contract.h"
#include "sigbuilder.h"

typedef BYTE NIBBLE;

//-----------------------------------------------------------------------------
// Helpers for compression routines.
//-----------------------------------------------------------------------------
// This class allows variable-length compression of DWORDs.
//
// A value can be stored using one or more nibbles. 3 bits of a nibble are used
// to store 3 bits of the value, and the top bit indicates if  the following nibble
// contains rest of the value. If the top bit is not set, then this
// nibble is the last part of the value.
// The higher bits of the value are written out first, and the lowest 3 bits
// are written out last.
//
// In the encoded stream of bytes, the lower nibble of a byte is used before
// the high nibble.
//
// A binary value ABCDEFGHI (where A is the highest bit) is encoded as
// the follow two bytes : 1DEF1ABC XXXX0GHI
//
// Examples :
// 0            => X0
// 1            => X1
//
// 7            => X7
// 8            => 09
// 9            => 19
//
// 0x3F (63)    => 7F
// 0x40 (64)    => F9 X0
// 0x41 (65)    => F9 X1
//
// 0x1FF (511)  => FF X7
// 0x200 (512)  => 89 08
// 0x201 (513)  => 89 18

class NibbleWriter
{
public:
    NibbleWriter()
    {
        LIMITED_METHOD_CONTRACT;

        m_fPending = false;
    }

    void Flush()
    {
        if (m_fPending)
        {
            m_SigBuilder.AppendByte(m_PendingNibble);
            m_fPending = false;
        }
    }

    PVOID GetBlob(DWORD * pdwLength)
    {
        return m_SigBuilder.GetSignature(pdwLength);
    }

    DWORD GetBlobLength()
    {
        return m_SigBuilder.GetSignatureLength();
    }

//.............................................................................
// Writer methods
//.............................................................................


    // Write a single nibble to the stream.
    void WriteNibble(NIBBLE i)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(i <= 0xF);

        if (m_fPending)
        {
            // Use the high nibble after the low nibble is used
            m_SigBuilder.AppendByte(m_PendingNibble | (i << 4));
            m_fPending = false;
        }
        else
        {
            // Use the low nibble first
            m_PendingNibble = i;
            m_fPending = true;
        }
    }

    // Write an unsigned int via variable length nibble encoding.
    // We use the bit scheme:
    // 0ABC (if 0 <= dw <= 0x7)
    // 1ABC 0DEF (if 0 <= dw <= 0x7f)
    // 1ABC 1DEF 0GHI (if 0 <= dw <= 0x7FF)
    // etc..

    void WriteEncodedU32(DWORD dw)
    {
        WRAPPER_NO_CONTRACT;

        // Fast path for common small inputs
        if (dw <= 63)
        {
            if (dw > 7)
            {
                WriteNibble((NIBBLE) ((dw >> 3) | 8));
            }

            WriteNibble((NIBBLE) (dw & 7));
            return;
        }

        // Note we must write this out with the low terminating nibble (0ABC) last b/c the
        // reader gets nibbles in the same order we write them.
        int i = 0;
        while ((dw >> i) > 7)
        {
            i+= 3;
        }
        while(i > 0)
        {
            WriteNibble((NIBBLE) ((dw >> i) & 0x7) | 0x8);
            i -= 3;
        }
        WriteNibble((NIBBLE) dw & 0x7);
    }

    // Write a signed 32 bit value.
    void WriteEncodedI32(int x)
    {
        WRAPPER_NO_CONTRACT;

        DWORD dw = (x < 0) ? (((-x) << 1) + 1) : (x << 1);
        WriteEncodedU32(dw);
    };

    void WriteUnencodedU32(uint32_t x)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        for (int i = 0; i < 8; i++)
        {
            WriteNibble(static_cast<NIBBLE>(x & 0b1111));
            x >>= 4;
        }
    }

    void WriteRawByte(uint8_t b)
    {
        Flush();
        m_SigBuilder.AppendByte(b);
    }

protected:
    NIBBLE m_PendingNibble;     // Pending value, not yet written out.
    bool m_fPending;

    // SigBuilder is a convenient helper class for writing out small blobs
    SigBuilder m_SigBuilder;
};

//-----------------------------------------------------------------------------

class NibbleReader
{
public:
#ifdef BIGENDIAN
    typedef uint8_t NibbleChunkType; // Alternatively we could byteswap the data after we load it, but I don't have a convenient helper here, so we just use a byte type
#else
#ifdef HOST_64BIT
    typedef uint64_t NibbleChunkType;
#else
    typedef uint32_t NibbleChunkType;
#endif // HOST_64BIT
#endif // !BIGENDIAN
    NibbleReader(PTR_BYTE pBuffer, size_t size)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        _ASSERTE(pBuffer != NULL);

        TADDR pBufferAddr = dac_cast<TADDR>(pBuffer);
        TADDR pBufferChunkAddr = AlignDown(pBufferAddr, sizeof(NibbleChunkType));

        m_pNibblesBuffer = dac_cast<DPTR(NibbleChunkType)>(pBufferChunkAddr);
        m_curNibbleData = m_pNibblesBuffer[0];
        m_cNibbleMapOffset = (pBufferAddr - pBufferChunkAddr);
        m_curNibbleData >>= 8 * m_cNibbleMapOffset; // Adjust to the first nibble in the first chunk
        m_nibblesInCurrentNibbleData = (uint32_t)((sizeof(NibbleChunkType) * 2) - m_cNibbleMapOffset * 2); // Calculate how many nibbles are in the first chunk

        m_cNibbleChunksConsumed = 1;

        if (size >= 0xFFFFFFFF)
        {
            m_cNibbleChunksTotal = 0xFFFFFFFF;
        }
        else
        {
            if ((m_nibblesInCurrentNibbleData / 2) >= size)
            {
                m_cNibbleChunksTotal = 1; // No more chunks to read, we have enough nibbles in the first chunk
            }
            else
            {
                m_cNibbleChunksTotal = 1 + AlignUp((size * 2 - m_nibblesInCurrentNibbleData), sizeof(NibbleChunkType) * 2)/(sizeof(NibbleChunkType) * 2);
            }
        }
    }

    // Get the index of the next Byte.
    // This tells us how many bytes (rounding up to whole bytes) have been read.
    // This is can be used to extract raw byte data that may be embedded on a byte boundary in the nibble stream.
    size_t GetNextByteIndex()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        size_t nextNibbleChunkOffset = m_cNibbleChunksConsumed * sizeof(NibbleChunkType) - m_cNibbleMapOffset;
        size_t result = nextNibbleChunkOffset - m_nibblesInCurrentNibbleData / 2;

        return result;
    }

    NIBBLE ReadNibble()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        if (m_nibblesInCurrentNibbleData == 0)
        {
            // We have consumed all nibbles in the current nibble data.
            // Move to the next chunk of nibbles.
            if (m_cNibbleChunksConsumed >= m_cNibbleChunksTotal)
            {
                // No more nibbles left to read.
                EX_THROW(HRException, (E_INVALIDARG));
            }

            m_curNibbleData = m_pNibblesBuffer[m_cNibbleChunksConsumed++];
            m_nibblesInCurrentNibbleData = (sizeof(NibbleChunkType) * 2);
        }

        m_nibblesInCurrentNibbleData--;
        NIBBLE i = (NIBBLE) (m_curNibbleData & 0xF);
        m_curNibbleData >>= 4; // Shift right to get the next nibble for the next call

        return i;
    }

    // Read an unsigned int that was encoded via variable length nibble encoding
    // from NibbleWriter::WriteEncodedU32.
    DWORD ReadEncodedU32()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        DWORD dw =0;

#if defined(_DEBUG) || defined(DACCESS_COMPILE)
        int dwCount = 0;
#endif

        // The encoding is variably lengthed, with the high-bit of every nibble indicating whether
        // there is another nibble in the value.  Each nibble contributes 3 bits to the value.
        NIBBLE n;
        do
        {
#if defined(_DEBUG) || defined(DACCESS_COMPILE)
            // If we've already read 11 nibbles (with 3 bits of usable data each), then we
            // should be done reading a 32-bit integer.
            // Avoid working with corrupted data and potentially long loops by failing
            if(dwCount > 11)
            {
                _ASSERTE_MSG(false, "Corrupt nibble stream - value exceeded 32-bits in size");
#ifdef DACCESS_COMPILE
                DacError(CORDBG_E_TARGET_INCONSISTENT);
#endif
            }
            dwCount++;
#endif

            n = ReadNibble();
            dw = (dw << 3) + (n & 0x7);
        } while((n & 0x8) > 0);

        return dw;
    }

    FORCEINLINE NIBBLE ReadNibble_NoThrow()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (m_nibblesInCurrentNibbleData == 0)
        {
            // We have consumed all nibbles in the current nibble data.
            // Move to the next chunk of nibbles.
            if (m_cNibbleChunksConsumed < m_cNibbleChunksTotal)
            {
                // Read the next nibble chunk. If we're past the end, we'll just skip
                // that, and the nibble data will just be 0.
                m_curNibbleData = m_pNibblesBuffer[m_cNibbleChunksConsumed++];
            }

            m_nibblesInCurrentNibbleData = (sizeof(NibbleChunkType) * 2);
        }

        m_nibblesInCurrentNibbleData--;
        NIBBLE i = (NIBBLE) (m_curNibbleData & 0xF);
        m_curNibbleData >>= 4; // Shift right to get the next nibble for the next call

        return i;
    }

    // Read an unsigned int that was encoded via variable length nibble encoding
    // from NibbleWriter::WriteEncodedU32.
    DWORD ReadEncodedU32_NoThrow()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        DWORD dw = 0;

#if defined(_DEBUG) || defined(DACCESS_COMPILE)
        int dwCount = 0;
#endif

        // The encoding is variably lengthed, with the high-bit of every nibble indicating whether
        // there is another nibble in the value.  Each nibble contributes 3 bits to the value.
        NIBBLE n;
        do
        {
#if defined(_DEBUG) || defined(DACCESS_COMPILE)
            // If we've already read 11 nibbles (with 3 bits of usable data each), then we
            // should be done reading a 32-bit integer.
            // Avoid working with corrupted data and potentially long loops by failing
            if(dwCount > 11)
            {
                _ASSERTE_MSG(false, "Corrupt nibble stream - value exceeded 32-bits in size");
#ifdef DACCESS_COMPILE
                DacError(CORDBG_E_TARGET_INCONSISTENT);
#endif
            }
            dwCount++;
#endif

            n = ReadNibble_NoThrow();
            dw = (dw << 3) + (n & 0x7);
        } while((n & 0x8) > 0);

        return dw;
    }
    int ReadEncodedI32()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        DWORD dw = ReadEncodedU32();
        int x = dw >> 1;
        return (dw & 1) ? (-x) : (x);
    }

    int ReadEncodedI32_NoThrow()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        DWORD dw = ReadEncodedU32_NoThrow();
        int x = dw >> 1;
        return (dw & 1) ? (-x) : (x);
    }

    DWORD ReadUnencodedU32()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        DWORD result = 0;

        for (int i = 0; i < 8; i++)
        {
            result |= static_cast<DWORD>(ReadNibble()) << (i * 4);
        }

        return result;
    }

    DWORD ReadUnencodedU32_NoThrow()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        DWORD result = 0;

        for (int i = 0; i < 8; i++)
        {
            result |= static_cast<DWORD>(ReadNibble_NoThrow()) << (i * 4);
        }

        return result;
    }

protected:
    DPTR(NibbleChunkType) m_pNibblesBuffer;
    size_t m_cNibbleChunksTotal; // size of buffer remaining
    size_t m_cNibbleChunksConsumed; // How many chunks of nibbles have we consumed?
    size_t m_cNibbleMapOffset; // Offset in the nibble stream the nibble stream started
    NibbleChunkType m_curNibbleData;
    uint32_t m_nibblesInCurrentNibbleData;
};



#endif // _NIBBLESTREAM_H_
