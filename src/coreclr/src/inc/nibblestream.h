// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
            m_SigBuilder.AppendByte(m_PendingNibble);
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
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

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
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

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
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;
        
        DWORD dw = (x < 0) ? (((-x) << 1) + 1) : (x << 1);
        WriteEncodedU32(dw);
    };

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
    NibbleReader(PTR_BYTE pBuffer, size_t size)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        _ASSERTE(pBuffer != NULL);
        
        m_pBuffer = pBuffer;
        m_cBytes = size;
        m_cNibble = 0;
    }

    // Get the index of the next Byte.
    // This tells us how many bytes (rounding up to whole bytes) have been read.
    // This is can be used to extract raw byte data that may be embedded on a byte boundary in the nibble stream.
    size_t GetNextByteIndex()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return (m_cNibble + 1) / 2;
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
        
        NIBBLE i = 0;
        // Bufer should have been allocated large enough to hold data.
        if (!(m_cNibble / 2 < m_cBytes))
        {
            // We should never get here in a normal retail scenario.
            // We could wind up here if somebody provided us invalid data (maybe by corrupting an ngenned image).
            EX_THROW(HRException, (E_INVALIDARG));
        }
        
        BYTE p = m_pBuffer[m_cNibble / 2];
        if ((m_cNibble & 1) == 0)
        {
            // Read the low nibble first
            i = (NIBBLE) (p & 0xF);
        }
        else
        {
            // Read the high nibble after the low nibble has been read
            i = (NIBBLE) (p >> 4) & 0xF;
        }
        m_cNibble++;

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

protected:
    PTR_BYTE m_pBuffer;
    size_t m_cBytes; // size of buffer.
    size_t m_cNibble; // Which nibble are we at?
};



#endif // _NIBBLESTREAM_H_
