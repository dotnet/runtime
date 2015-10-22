//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        _ASSERTE((argLocDescForStructInRegs != NULL) || (offset != TransitionBlock::StructInRegsOffset));
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

#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

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
        _ASSERTE(sizeof(zeros) >= fieldBytes);

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
                floatRegDest += 8;
            }
            else
            {
                if (eightByteSize == 8)
                {
                    _ASSERTE((eightByteClassification == SystemVClassificationTypeInteger) ||
                             (eightByteClassification == SystemVClassificationTypeIntegerReference));

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
                if (eightByteClassification == SystemVClassificationTypeIntegerReference)
                {
                    _ASSERTE(eightByteSize == 8);
                    _ASSERTE(IS_ALIGNED((SIZE_T)genRegDest, 8));

                    (*fn)(dac_cast<PTR_PTR_Object>(genRegDest), sc, 0);
                }

                genRegDest += eightByteSize;
            }

            INDEBUG(remainingBytes -= eightByteSize;)
        }

        _ASSERTE(remainingBytes == 0);
    }

#endif // UNIX_AMD64_ABI && FEATURE_UNIX_AMD64_STRUCT_PASSING

};

#endif // __ARGDESTINATION_H__
