// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

//
// Provides a mechanism to store an array of DWORD-typed fields in a space-efficient manner. There are some
// caveats:
//  1) Fields can be written and read in an uncompressed form (a simple array of DWORD values) until the
//     PackFields() method is invoked. Once this method has been invoked (and returned true) fields have been
//     compacted and must not be modified again. That is, the primary usage of this class is to store a set of
//     initialized-once fields.
//  2) The compaction algorithm relies on the fields containing small values (such as counts). Avoid storing
//     fields that have special sentinel values (such as all bits set) which will frequently set high order
//     bits.
//  3) An instance of PackedDWORDFields will take up a fixed quantity of memory equivalent to an array of
//     DWORD fields. If PackFields() returns true then the fields values frozen at the time of the call have
//     been compressed into a fewer number of bytes in-place. This smaller size will always be a multiple of
//     sizeof(DWORD) in length and is reported by GetPackedSize(). If a PackedDWORDFields structure is being
//     declared as a field inside another structure it is typically wise to place the field last to take
//     advantage of this size reduction (e.g. when saving the outer structure into an ngen image). If
//     PackFields() returns false then the fields remain unpacked and unchanged.
//  4) The caller retains the responsibility of recording whether an instance of PackedDWORDFields is in the
//     packed or unpacked state. This is important since incorrect behavior will result if the wrong methods
//     are used for the current state (e.g. calling GetUnpackedField() on a packed instance). This is not done
//     automatically since there are no bits free to store the state. However, under a debug build correct
//     usage will be checked (at the expensive of extra storage space).
//  5) The space saving made come at a runtime CPU cost to access the fields. Do not use this mechanism to
//     compact fields that must be read on a perfomance critical path. If unsure, measure the performance of
//     this solution before committing to it.
//
// ============================================================================

// Describe an array of FIELD_COUNT DWORDs. Each entry is addressed via a zero-based index and is expected to
// frequently contain a small integer and remain frozen after initialization.
template <DWORD FIELD_COUNT>
class PackedDWORDFields
{
    // Some constants to make the code a little more readable.
    enum Constants
    {
        kMaxLengthBits  = 5,    // Number of bits needed to express the maximum length of a field (32-bits)
        kBitsPerDWORD   = 32,   // Number of bits in a DWORD
    };

public:
    // Fields are all initialized to zero.
    PackedDWORDFields()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        memset(m_rgUnpackedFields, 0, sizeof(m_rgUnpackedFields));
#ifdef _DEBUG
        memset(m_rgDebugUnpackedFields, 0, sizeof(m_rgDebugUnpackedFields));
        m_fFieldsPacked = false;
#endif // _DEBUG
    }

    // Get the value of the given field when the structure is in its unpacked state.
    DWORD GetUnpackedField(DWORD dwFieldIndex)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        _ASSERTE(dwFieldIndex < FIELD_COUNT);
        _ASSERTE(!m_fFieldsPacked);

        return m_rgUnpackedFields[dwFieldIndex];
    }

    // Set the value of the given field when the structure is in its unpacked state. Setting field values
    // multiple times is allowed but only until a successful call to PackFields is made.
    void SetUnpackedField(DWORD dwFieldIndex, DWORD dwValue)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(dwFieldIndex < FIELD_COUNT);
        _ASSERTE(!m_fFieldsPacked);

        m_rgUnpackedFields[dwFieldIndex] = dwValue;

#ifdef _DEBUG
        m_rgDebugUnpackedFields[dwFieldIndex] = dwValue;
#endif // _DEBUG
    }

    // Attempt to optimize the set of fields given their current values. Returns false if compaction wouldn't
    // achieve any space savings (in this case the structure remains in the unpacked state and the caller can
    // continue to use the *UnpackedField methods above or even re-attempt PackFields() with different field
    // values). If true is returned the data has been compacted into a smaller amount of space (this will
    // always be a multiple of sizeof(DWORD) in size). This size can be queried using GetPackedSize() below.
    // Once PackFields() has returned true fields can no longer be modified and field values must be retrieved
    // via GetPackedField() rather than GetUnpackedField().
    bool PackFields()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        // Can't re-pack a packed structure.
        _ASSERTE(!m_fFieldsPacked);

        // First compute the number of bits of space we'd need for a packed representation. Do this before
        // making any changes since sometimes we'd end up expanding the data instead and in this case we wish
        // to return false and make no updates to the structure.

        // There's a fixed overhead of kMaxLengthBits for each field (we store the packed fields as a
        // bit-stream that alternates between a field length (of kMaxLengthBits) followed by a variable length
        // bitfield containing the field value.
        DWORD dwTotalPackedBits = FIELD_COUNT * kMaxLengthBits;

        // For each field calculate excatly how many bits we'd need to store the field value and add this to
        // the total.
        for (DWORD i = 0; i < FIELD_COUNT; i++)
            dwTotalPackedBits += BitsRequired(m_rgUnpackedFields[i]);

        // Now we have the total is it smaller than a simple array of DWORDs?
        if (dwTotalPackedBits >= (FIELD_COUNT * kBitsPerDWORD))
            return false;

        // Compaction will save us space. We're committed to implementing that compaction now.

        // Work from a copy of the unpacked fields since we're about to start modifying the space in which
        // they're currently stored.
        DWORD rgUnpackedFields[FIELD_COUNT];
        memcpy(rgUnpackedFields, m_rgUnpackedFields, sizeof(rgUnpackedFields));

        // Start writing a stream of bits. For each field write a fixed sized header describing the number of
        // bits required to express the field followed by the field value itself.
        DWORD dwOffset = 0;
        for (DWORD i = 0; i < FIELD_COUNT; i++)
        {
            // Find the minimal number of bits required to encode the current field's value.
            DWORD dwFieldLength = BitsRequired(rgUnpackedFields[i]);
            _ASSERTE(dwFieldLength > 0 && dwFieldLength <= kBitsPerDWORD);

            // Write the size field. Note that we store the size biased by one. That is, a field length of one
            // is encoded as zero. We do this so we can express a range of field sizes from 1 through 32,
            // emcompassing the worst case scenario (a full 32 bits). It comes at the cost of not being able
            // to encode zero-valued fields with zero bits. Is this is deemed an important optimization in the
            // future we could always given up on a simple linear mapping of the size field and use a lookup
            // table to map values encoded into the real sizes. Experiments with EEClass packed fields over
            // CoreLib show that this currently doesn't yield us much benefit, primarily due to the DWORD
            // round-up size semantic, which implies we'd need a lot more optimization than this to reduce the
            // average structure size below the next DWORD threshhold.
            BitVectorSet(dwOffset, kMaxLengthBits, dwFieldLength - 1);
            dwOffset += kMaxLengthBits;

            // Write the field value itself.
            BitVectorSet(dwOffset, dwFieldLength, rgUnpackedFields[i]);
            dwOffset += dwFieldLength;
        }

#ifdef _DEBUG
        m_fFieldsPacked = true;
#endif // _DEBUG

        // Compaction was successful.
        return true;
    }

    // Return the size in bytes of a compacted structure (it is illegal to call this on an uncompacted
    // structure).
    DWORD GetPackedSize()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        _ASSERTE(m_fFieldsPacked);

        // Walk the field stream reading header (which are fixed size) and then using the value of the headers
        // to skip the field value.
        DWORD cBits = 0;
        for (DWORD i = 0; i < FIELD_COUNT; i++)
            cBits += kMaxLengthBits + BitVectorGet(cBits, kMaxLengthBits) + 1; // +1 since size is [1,32] not [0,31]

        // Compute the number of DWORDs needed to store the bits of the encoding.
        // static_cast would not be necessary if ALIGN_UP were templated like FitsIn.
        DWORD cDWORDs = static_cast<DWORD>(ALIGN_UP(cBits, kBitsPerDWORD)) / kBitsPerDWORD;

        // Return the total structure size.
        return offsetof(PackedDWORDFields<FIELD_COUNT>, m_rgPackedFields) + (cDWORDs * sizeof(DWORD));
    }

    // Get the value of a packed field. Illegal to call this on an uncompacted structure.
    DWORD GetPackedField(DWORD dwFieldIndex)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        _ASSERTE(dwFieldIndex < FIELD_COUNT);
        _ASSERTE(m_fFieldsPacked);

        // Walk past all the predecessor fields.
        DWORD dwOffset = 0;
        for (DWORD i = 0; i < dwFieldIndex; i++)
            dwOffset += kMaxLengthBits + BitVectorGet(dwOffset, kMaxLengthBits) + 1; // +1 since size is [1,32] not [0,31]

        // The next kMaxLengthBits bits contain the length in bits of the field we want (-1 due to the way we
        // encode the length).
        DWORD dwFieldLength = BitVectorGet(dwOffset, kMaxLengthBits) + 1;
        dwOffset += kMaxLengthBits;

        // Grab the field value.
        DWORD dwReturn = BitVectorGet(dwOffset, dwFieldLength);

        // On debug builds ensure the encoded field value is the same as the original unpacked version.
        _ASSERTE(dwReturn == m_rgDebugUnpackedFields[dwFieldIndex]);
        return dwReturn;
    }

private:
    // Return the minimum number of bits required to encode a DWORD value by stripping out the
    // most-significant leading zero bits). Returns a value between 1 and 32 inclusive (we never encode
    // anything with zero bits).
    DWORD BitsRequired(DWORD dwValue)
    {
        LIMITED_METHOD_CONTRACT;

        // Starting with a bit-mask of the most significant bit and iterating over masks for successively less
        // significant bits, stop as soon as the mask co-incides with a set bit in the value. Simultaneously
        // we're counting down the bits required to express the range of values implied by seeing the
        // corresponding bit set in the value (e.g. when we're testing the high bit we know we'd need 32-bits
        // to encode the range of values that have this bit set). Stop when we get to one bit (we never return
        // 0 bits required, even for an input value of 0).
        DWORD dwMask = 0x80000000;
        DWORD cBits = 32;
        while (cBits > 1)
        {
            if (dwValue & dwMask)
                return cBits;

            dwMask >>= 1;
            cBits--;
        }

        return 1;
    }

    // Set the dwLength bits at m_rgPackedFields + dwOffset bits to the value dwValue.
    void BitVectorSet(DWORD dwOffset, DWORD dwLength, DWORD dwValue)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(dwLength > 0 && dwLength <= kBitsPerDWORD); // Can set at most one DWORD at a time
        _ASSERTE((dwLength == kBitsPerDWORD) || (dwValue < (1U << dwLength)));  // Value had better fit in the given length

        // Calculate the start and end naturally aligned DWORDs into which the value will go.
        DWORD dwStartBlock = dwOffset / kBitsPerDWORD;
        DWORD dwEndBlock = (dwOffset + dwLength - 1) / kBitsPerDWORD;
        if (dwStartBlock == dwEndBlock)
        {
            // Easy case: the new value fits entirely within one aligned DWORD. Compute the number of bits
            // we'll need to shift the input value (to the left) and a mask of the bits that will be set in
            // the destination DWORD.
            DWORD dwValueShift = dwOffset % kBitsPerDWORD;
            DWORD dwValueMask = ((1U << dwLength) - 1) << dwValueShift;

            m_rgPackedFields[dwStartBlock] &= ~dwValueMask;             // Zero the target bits
            m_rgPackedFields[dwStartBlock] |= dwValue << dwValueShift;  // Or in the new value (suitably shifted)
        }
        else
        {
            // Hard case: the new value is split across two DWORDs (two DWORDs is the max as the new value can
            // be at most DWORD-sized itself). For simplicity we'll simply break this into two separate
            // non-spanning sets. We can revisit this in the future if the perf is a problem.
            DWORD dwInitialBits = kBitsPerDWORD - (dwOffset % kBitsPerDWORD);   // Number of bits to set in the first DWORD
            DWORD dwInitialMask = (1U << dwInitialBits) - 1;                    // Mask covering those value bits

            // Set the portion of the value residing in the first DWORD.
            BitVectorSet(dwOffset, dwInitialBits, dwValue & dwInitialMask);

            // And then the remainder in the second DWORD.
            BitVectorSet(dwOffset + dwInitialBits, dwLength - dwInitialBits, dwValue >> dwInitialBits);
        }

        _ASSERTE(BitVectorGet(dwOffset, dwLength) == dwValue);
    }

    // Get the dwLength bits at m_rgPackedFields + dwOffset bits. Value is zero-extended to DWORD size.
    DWORD BitVectorGet(DWORD dwOffset, DWORD dwLength)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        _ASSERTE(dwLength > 0 && dwLength <= kBitsPerDWORD);    // Can get at most one DWORD at a time

        // Calculate the start and end naturally aligned DWORDs from which the value will come.
        DWORD dwStartBlock = dwOffset / kBitsPerDWORD;
        DWORD dwEndBlock = (dwOffset + dwLength - 1) / kBitsPerDWORD;
        if (dwStartBlock == dwEndBlock)
        {
            // Easy case: the new value fits entirely within one aligned DWORD. Compute the number of bits
            // we'll need to shift the extracted value (to the right) and a mask of the bits that will be
            // extracted in the destination DWORD.
            DWORD dwValueShift = dwOffset % kBitsPerDWORD;
            DWORD dwValueMask = ((1U << dwLength) - 1) << dwValueShift;

            // Mask out the bits we want and shift them down into the bottom of the result DWORD.
            return (m_rgPackedFields[dwStartBlock] & dwValueMask) >> dwValueShift;
        }
        else
        {
            // Hard case: the return value is split across two DWORDs (two DWORDs is the max as the new value
            // can be at most DWORD-sized itself). For simplicity we'll simply break this into two separate
            // non-spanning gets and stitch the result together from that. We can revisit this in the future
            // if the perf is a problem.
            DWORD dwInitialBits = kBitsPerDWORD - (dwOffset % kBitsPerDWORD);   // Number of bits to get in the first DWORD
            DWORD dwReturn;

            // Get the initial (low-order) bits from the first DWORD.
            dwReturn = BitVectorGet(dwOffset, dwInitialBits);

            // Get the remaining bits from the second DWORD. These bits will need to be shifted to the left
            // (past the bits we've already read) before being OR'd into the result.
            dwReturn |= BitVectorGet(dwOffset + dwInitialBits, dwLength - dwInitialBits) << dwInitialBits;

            return dwReturn;
        }
    }

#ifdef _DEBUG
    DWORD       m_rgDebugUnpackedFields[FIELD_COUNT];   // A copy of the unpacked fields so we can validate
                                                        // packed reads
    bool        m_fFieldsPacked;                        // The current packed/unpacked state so we can check
                                                        // the right methods are being called
#endif // _DEBUG

    union
    {
        DWORD   m_rgUnpackedFields[FIELD_COUNT];        // The fields in their unpacked state
        DWORD   m_rgPackedFields[1];                    // The first DWORD block of fields in the packed state
    };
};
