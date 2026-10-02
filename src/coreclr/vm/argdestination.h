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
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
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

#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    bool IsStructPassedInRegs()
    {
        return m_argLocDescForStructInRegs != NULL;
    }

    PTR_VOID GetStructGenRegDestinationAddress()
    {
        _ASSERTE(IsStructPassedInRegs());
        int argOfs = TransitionBlock::GetOffsetOfArgumentRegisters() + m_argLocDescForStructInRegs->m_idxGenReg * 8;
        return dac_cast<PTR_VOID>(dac_cast<TADDR>(m_base) + argOfs);
    }
#endif // defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

#if defined(UNIX_AMD64_ABI)

    // Returns true if the ArgDestination represents a struct passed in registers.
    bool IsStructPassedInRegs()
    {
        LIMITED_METHOD_CONTRACT;
        return m_offset == TransitionBlock::StructInRegsOffset;
    }

    // Get destination address for non-floating point fields of a struct passed in registers.
    PTR_VOID GetStructGenRegDestinationAddress()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsStructPassedInRegs());
        int offset = TransitionBlock::GetOffsetOfArgumentRegisters() + m_argLocDescForStructInRegs->m_idxGenReg * 8;
        return dac_cast<PTR_VOID>(dac_cast<TADDR>(m_base) + offset);
    }

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

        _ASSERTE(m_argLocDescForStructInRegs->m_eightByteInfo.GetNumEightBytes() != 0);

        for (int i = 0; i < m_argLocDescForStructInRegs->m_eightByteInfo.GetNumEightBytes(); i++)
        {
            int eightByteSize = m_argLocDescForStructInRegs->m_eightByteInfo.GetEightByteSize(i);
            SystemVClassificationType eightByteClassification = m_argLocDescForStructInRegs->m_eightByteInfo.GetEightByteClassification(i);

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
