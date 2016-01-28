// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef CFI_H_
#define CFI_H_

#define DWARF_REG_ILLEGAL -1
enum CFI_OPCODE
{
   CFI_ADJUST_CFA_OFFSET,    // Offset is adjusted relative to the current one.
   CFI_DEF_CFA_REGISTER,     // New register is used to compute CFA
   CFI_REL_OFFSET            // Register is saved at offset from the current CFA
};

struct CFI_CODE
{
    unsigned char CodeOffset;// Offset from the start of code the frame covers.
    unsigned char CfiOpCode;
    short DwarfReg;          // Dwarf register number. 0~32 for x64.
    int Offset;
    CFI_CODE(unsigned char codeOffset, unsigned char cfiOpcode,
        short dwarfReg, int offset)
        : CodeOffset(codeOffset)
        , CfiOpCode(cfiOpcode)
        , DwarfReg(dwarfReg)
        , Offset(offset)
    {}
};
typedef CFI_CODE* PCFI_CODE;

#endif // CFI_H

