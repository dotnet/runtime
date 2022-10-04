// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __ARGDESTINATION_H__
#define __ARGDESTINATION_H__

// The ArgDestination class represents a destination location of an argument.
class ArgDestination
{
    // Base address to which the m_offset is applied to get the actual argument location.
    PTR_VOID m_base;
    // Offset of the argument relative to the m_base. On AMD64 on Unix, it can have a special
    // value that represent a struct that contain both general purpose and floating point fields
    // passed in registers.
    int m_offset;
    // For structs passed in registers, this member points to an ArgLocDesc that contains
    // details on the layout of the struct in general purpose and floating point registers.
    ArgLocDesc* m_argLocDescForStructInRegs;

public:

    // Construct the ArgDestination
    ArgDestination(PTR_VOID base, int offset, ArgLocDesc* argLocDescForStructInRegs)
    :   m_base(base),
        m_offset(offset),
        m_argLocDescForStructInRegs(argLocDescForStructInRegs)
    {
        LIMITED_METHOD_CONTRACT;
#if defined(UNIX_AMD64_ABI)
        _ASSERTE((argLocDescForStructInRegs != NULL) || (offset != TransitionBlock::StructInRegsOffset));
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
        // This assert is not interesting on arm64/loongarch64. argLocDescForStructInRegs could be
        // initialized if the args are being enregistered.
#else
        _ASSERTE(argLocDescForStructInRegs == NULL);
#endif
    }

    // Get argument destination address for arguments that are not structs passed in registers.
    PTR_VOID GetDestinationAddress()
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<PTR_VOID>(dac_cast<TADDR>(m_base) + m_offset);
    }

#if defined(TARGET_ARM64)
#ifndef DACCESS_COMPILE

    // Returns true if the ArgDestination represents an HFA struct
    bool IsHFA()
    {
        return m_argLocDescForStructInRegs != NULL;
    }

    // Copy struct argument into registers described by the current ArgDestination.
    // Arguments:
    //  src = source data of the structure
    //  fieldBytes - size of the structure
    void CopyHFAStructToRegister(void *src, int fieldBytes)
    {
        // We are copying a float, double or vector HFA/HVA and need to
        // enregister each field.

        int floatRegCount = m_argLocDescForStructInRegs->m_cFloatReg;
        int hfaFieldSize = m_argLocDescForStructInRegs->m_hfaFieldSize;
        UINT64* dest = (UINT64*) this->GetDestinationAddress();

        for (int i = 0; i < floatRegCount; ++i)
        {
            // Copy 4 or 8 bytes from src.
            UINT64 val = (hfaFieldSize == 4) ? *((UINT32*)src) : *((UINT64*)src);
            // Always store 8 bytes
            *(dest++) = val;
            // Either zero the next 8 bytes or get the next 8 bytes from src for 16-byte vector.
            *(dest++) = (hfaFieldSize == 16) ? *((UINT64*)src + 1) : 0;

            // Increment src by the appropriate amount.
            src = (void*)((char*)src + hfaFieldSize);
        }
    }

#endif // !DACCESS_COMPILE
#endif // defined(TARGET_ARM64)

#if defined(TARGET_LOONGARCH64)
    bool IsStructPassedInRegs()
    {
        return m_argLocDescForStructInRegs != NULL;
    }

#ifndef DACCESS_COMPILE
    // Copy struct argument into registers described by the current ArgDestination.
    // Arguments:
    //  src = source data of the structure
    //  fieldBytes - size of the structure
    //  destOffset - nonzero when copying values into Nullable<T>, it is the offset
    //               of the T value inside of the Nullable<T>
    void CopyStructToRegisters(void *src, int fieldBytes, int destOffset)
    {
        _ASSERTE(IsStructPassedInRegs());
        _ASSERTE(fieldBytes <= 16);

        int argOfs = TransitionBlock::GetOffsetOfFloatArgumentRegisters() + m_argLocDescForStructInRegs->m_idxFloatReg * 8;

        if (m_argLocDescForStructInRegs->m_structFields == STRUCT_FLOAT_FIELD_ONLY_TWO)
        { // struct with two floats.
            _ASSERTE(m_argLocDescForStructInRegs->m_cFloatReg == 2);
            _ASSERTE(m_argLocDescForStructInRegs->m_cGenReg == 0);
            *(INT64*)((char*)m_base + argOfs) = *(INT32*)src;
            *(INT64*)((char*)m_base + argOfs + 8) = *((INT32*)src + 1);
        }
        else if ((m_argLocDescForStructInRegs->m_structFields & STRUCT_FLOAT_FIELD_FIRST) != 0)
        { // the first field is float or double.
            _ASSERTE(m_argLocDescForStructInRegs->m_cFloatReg == 1);
            _ASSERTE(m_argLocDescForStructInRegs->m_cGenReg == 1);
            _ASSERTE((m_argLocDescForStructInRegs->m_structFields & STRUCT_FLOAT_FIELD_SECOND) == 0);//the second field is integer.

            if ((m_argLocDescForStructInRegs->m_structFields & STRUCT_FIRST_FIELD_SIZE_IS8) == 0)
            {
                *(INT64*)((char*)m_base + argOfs) = *(INT32*)src; // the first field is float
            }
            else
            {
                *(UINT64*)((char*)m_base + argOfs) = *(UINT64*)src; // the first field is double.
            }

            argOfs = TransitionBlock::GetOffsetOfArgumentRegisters() + m_argLocDescForStructInRegs->m_idxGenReg * 8;
            if ((m_argLocDescForStructInRegs->m_structFields & STRUCT_HAS_8BYTES_FIELDS_MASK) != 0)
            {
                *(UINT64*)((char*)m_base + argOfs) = *((UINT64*)src + 1);
            }
            else
            {
                *(INT64*)((char*)m_base + argOfs) = *((INT32*)src + 1); // the second field is int32.
            }
        }
        else if ((m_argLocDescForStructInRegs->m_structFields & STRUCT_FLOAT_FIELD_SECOND) != 0)
        { // the second field is float or double.
            _ASSERTE(m_argLocDescForStructInRegs->m_cFloatReg == 1);
            _ASSERTE(m_argLocDescForStructInRegs->m_cGenReg == 1);
            _ASSERTE((m_argLocDescForStructInRegs->m_structFields & STRUCT_FLOAT_FIELD_FIRST) == 0);//the first field is integer.

            // destOffset - nonzero when copying values into Nullable<T>, it is the offset of the T value inside of the Nullable<T>.
            // here the first field maybe Nullable.
            if ((m_argLocDescForStructInRegs->m_structFields & STRUCT_HAS_8BYTES_FIELDS_MASK) == 0)
            {
                // the second field is float.
                *(INT64*)((char*)m_base + argOfs) = destOffset == 0 ? *((INT32*)src + 1) : *(INT32*)src;
            }
            else
            {
                // the second field is double.
                *(UINT64*)((char*)m_base + argOfs) = destOffset == 0 ? *((UINT64*)src + 1) : *(UINT64*)src;
            }

            if (0 == destOffset)
            {
                // NOTE: here ignoring the first size.
                argOfs = TransitionBlock::GetOffsetOfArgumentRegisters() + m_argLocDescForStructInRegs->m_idxGenReg * 8;
                *(UINT64*)((char*)m_base + argOfs) = *(UINT64*)src;
            }
        }
        else
        {
            _ASSERTE(!"---------UNReachable-------LoongArch64!!!");
        }
    }
#endif // !DACCESS_COMPILE

    PTR_VOID GetStructGenRegDestinationAddress()
    {
        _ASSERTE(IsStructPassedInRegs());
        int argOfs = TransitionBlock::GetOffsetOfArgumentRegisters() + m_argLocDescForStructInRegs->m_idxGenReg * 8;
        return dac_cast<PTR_VOID>(dac_cast<TADDR>(m_base) + argOfs);
    }
#endif // defined(TARGET_LOONGARCH64)

#if defined(UNIX_AMD64_ABI)

    // Returns true if the ArgDestination represents a struct passed in registers.
    bool IsStructPassedInRegs()
    {
        LIMITED_METHOD_CONTRACT;
        return m_offset == TransitionBlock::StructInRegsOffset;
    }

    // Get destination address for floating point fields of a struct passed in registers.
    PTR_VOID GetStructFloatRegDestinationAddress()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsStructPassedInRegs());
        int offset = TransitionBlock::GetOffsetOfFloatArgumentRegisters() + m_argLocDescForStructInRegs->m_idxFloatReg * 16;
        return dac_cast<PTR_VOID>(dac_cast<TADDR>(m_base) + offset);
    }

    // Get destination address for non-floating point fields of a struct passed in registers.
    PTR_VOID GetStructGenRegDestinationAddress()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsStructPassedInRegs());
        int offset = TransitionBlock::GetOffsetOfArgumentRegisters() + m_argLocDescForStructInRegs->m_idxGenReg * 8;
        return dac_cast<PTR_VOID>(dac_cast<TADDR>(m_base) + offset);
    }

#ifndef DACCESS_COMPILE
    // Zero struct argument stored in registers described by the current ArgDestination.
    // Arguments:
    //  fieldBytes - size of the structure
    void ZeroStructInRegisters(int fieldBytes)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_FORBID_FAULT;
        STATIC_CONTRACT_MODE_COOPERATIVE;

        // To zero the struct, we create a zero filled array of large enough size and
        // then copy it to the registers. It is implemented this way to keep the complexity
        // of dealing with the eightbyte classification in single function.
        // This function is used rarely and so the overhead of reading the zeros from
        // the stack is negligible.
        long long zeros[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS] = {};
        _ASSERTE(sizeof(zeros) >= (size_t)fieldBytes);

        CopyStructToRegisters(zeros, fieldBytes, 0);
    }

    // Copy struct argument into registers described by the current ArgDestination.
    // Arguments:
    //  src = source data of the structure
    //  fieldBytes - size of the structure
    //  destOffset - nonzero when copying values into Nullable<T>, it is the offset
    //               of the T value inside of the Nullable<T>
    void CopyStructToRegisters(void *src, int fieldBytes, int destOffset)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_FORBID_FAULT;
        STATIC_CONTRACT_MODE_COOPERATIVE;

        _ASSERTE(IsStructPassedInRegs());

        BYTE* genRegDest = (BYTE*)GetStructGenRegDestinationAddress() + destOffset;
        BYTE* floatRegDest = (BYTE*)GetStructFloatRegDestinationAddress();
        INDEBUG(int remainingBytes = fieldBytes;)

        EEClass* eeClass = m_argLocDescForStructInRegs->m_eeClass;
        _ASSERTE(eeClass != NULL);

        // We start at the first eightByte that the destOffset didn't skip completely.
        for (int i = destOffset / 8; i < eeClass->GetNumberEightBytes(); i++)
        {
            int eightByteSize = eeClass->GetEightByteSize(i);
            SystemVClassificationType eightByteClassification = eeClass->GetEightByteClassification(i);

            // Adjust the size of the first eightByte by the destOffset
            eightByteSize -= (destOffset & 7);
            destOffset = 0;

            _ASSERTE(remainingBytes >= eightByteSize);

            if (eightByteClassification == SystemVClassificationTypeSSE)
            {
                if (eightByteSize == 8)
                {
                    *(UINT64*)floatRegDest = *(UINT64*)src;
                }
                else
                {
                    _ASSERTE(eightByteSize == 4);
                    *(UINT32*)floatRegDest = *(UINT32*)src;
                }
                floatRegDest += 16;
            }
            else
            {
                if (eightByteSize == 8)
                {
                    _ASSERTE((eightByteClassification == SystemVClassificationTypeInteger) ||
                             (eightByteClassification == SystemVClassificationTypeIntegerReference) ||
                             (eightByteClassification == SystemVClassificationTypeIntegerByRef));

                    _ASSERTE(IS_ALIGNED((SIZE_T)genRegDest, 8));
                    *(UINT64*)genRegDest = *(UINT64*)src;
                }
                else
                {
                    _ASSERTE(eightByteClassification == SystemVClassificationTypeInteger);
                    memcpyNoGCRefs(genRegDest, src, eightByteSize);
                }

                genRegDest += eightByteSize;
            }

            src = (BYTE*)src + eightByteSize;
            INDEBUG(remainingBytes -= eightByteSize;)
        }

        _ASSERTE(remainingBytes == 0);
    }

#endif //DACCESS_COMPILE

    // Report managed object pointers in the struct in registers
    // Arguments:
    //  fn - promotion function to apply to each managed object pointer
    //  sc - scan context to pass to the promotion function
    //  fieldBytes - size of the structure
    void ReportPointersFromStructInRegisters(promote_func *fn, ScanContext *sc, int fieldBytes)
    {
        LIMITED_METHOD_CONTRACT;

       _ASSERTE(IsStructPassedInRegs());

        TADDR genRegDest = dac_cast<TADDR>(GetStructGenRegDestinationAddress());
        INDEBUG(int remainingBytes = fieldBytes;)

        EEClass* eeClass = m_argLocDescForStructInRegs->m_eeClass;
        _ASSERTE(eeClass != NULL);

        for (int i = 0; i < eeClass->GetNumberEightBytes(); i++)
        {
            int eightByteSize = eeClass->GetEightByteSize(i);
            SystemVClassificationType eightByteClassification = eeClass->GetEightByteClassification(i);

            _ASSERTE(remainingBytes >= eightByteSize);

            if (eightByteClassification != SystemVClassificationTypeSSE)
            {
                if ((eightByteClassification == SystemVClassificationTypeIntegerReference) ||
                    (eightByteClassification == SystemVClassificationTypeIntegerByRef))
                {
                    _ASSERTE(eightByteSize == 8);
                    _ASSERTE(IS_ALIGNED((SIZE_T)genRegDest, 8));

                    uint32_t flags = eightByteClassification == SystemVClassificationTypeIntegerByRef ? GC_CALL_INTERIOR : 0;
                    (*fn)(dac_cast<PTR_PTR_Object>(genRegDest), sc, flags);
                }

                genRegDest += eightByteSize;
            }

            INDEBUG(remainingBytes -= eightByteSize;)
        }

        _ASSERTE(remainingBytes == 0);
    }

#endif // UNIX_AMD64_ABI

};

#endif // __ARGDESTINATION_H__
