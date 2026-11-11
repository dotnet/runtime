// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Decoding of a faulting x64 DIV / IDIV instruction, shared by CoreCLR and NativeAOT.
// The including file must provide _ASSERTE.

#ifndef _DIV_INSTRUCTION_DECODER_H_
#define _DIV_INSTRUCTION_DECODER_H_

#include <stdint.h>
#include <string.h>

// Instruction bytes and memory operands are not necessarily aligned.
template <typename T>
inline T ReadUnaligned(const void* address)
{
    T value;
    memcpy(&value, address, sizeof(T));
    return value;
}

// Get value of an instruction operand represented by the ModR/M field
// Parameters:
//  uint8_t rex :           REX prefix, 0 if there was none. Bits 4 and 5 carry the APX B4 and X4 extensions.
//  uint8_t* ip :           instruction pointer pointing to the ModR/M field
//  getRegister :           callable returning the value of the register with the given index
//  bool is8Bit :           true if the operand size is 8 bit
//  bool hasOpSizePrefix :  true if the instruction has op size prefix (0x66)
template <typename TGetRegister>
uint64_t GetModRMOperandValue(uint8_t rex, uint8_t* ip, TGetRegister getRegister, bool is8Bit, bool hasOpSizePrefix)
{
    uint64_t result;
    uint64_t resultReg;

    uint8_t rex_b = (rex & 0x1);         // high bit to modrm r/m field or SIB base field
    uint8_t rex_x = (rex & 0x2) >> 1;    // high bit to sib index field
    uint8_t rex_r = (rex & 0x4) >> 2;    // high bit to modrm reg field
    uint8_t rex_w = (rex & 0x8) >> 3;    // 1 = 64 bit operand size, 0 = operand size determined by hasOpSizePrefix
    uint8_t rex_b4 = (rex & 0x10) >> 4;  // APX: 5th bit to modrm r/m field or SIB base field
    uint8_t rex_x4 = (rex & 0x20) >> 5;  // APX: 5th bit to sib index field

    uint8_t modrm = *ip++;

    _ASSERTE(modrm != 0);

    uint8_t mod = (modrm & 0xC0) >> 6;
    uint8_t reg = (modrm & 0x38) >> 3;
    uint8_t rm = (modrm & 0x07);

    reg |= (rex_r << 3);
    uint8_t rmIndex = rm | (rex_b << 3) | (rex_b4 << 4);

    // 8 bit idiv without the REX prefix uses registers AH, CH, DH, BH for rm 4..8
    // which is an exception from the regular register indexes.
    bool isAhChDhBh = is8Bit && (rex == 0) && (rm >= 4);

    // See: Tables A-15,16,17 in AMD Dev Manual 3 for information
    //      about how the ModRM/SIB/REX bytes interact.

    switch (mod)
    {
    case 0:
    case 1:
    case 2:
        if (rm == 4) // we have an SIB byte following
        {
            //
            // Get values from the SIB byte
            //
            uint8_t sib = *ip++;

            _ASSERTE(sib != 0);

            uint8_t ss = (sib & 0xC0) >> 6;
            uint8_t index = (sib & 0x38) >> 3;
            uint8_t base = (sib & 0x07);

            index |= (rex_x << 3) | (rex_x4 << 4);
            base |= (rex_b << 3) | (rex_b4 << 4);

            //
            // Get starting value
            //
            if ((mod == 0) && (base == 5))
            {
                result = 0;
            }
            else
            {
                result = getRegister(base);
            }

            //
            // Add in the [index]
            //
            if (index != 4)
            {
                result += getRegister(index) << ss;
            }

            //
            // Finally add in the offset
            //
            if (mod == 0)
            {
                if (base == 5)
                {
                    result += ReadUnaligned<int32_t>(ip);
                }
            }
            else if (mod == 1)
            {
                result += ReadUnaligned<int8_t>(ip);
            }
            else // mod == 2
            {
                result += ReadUnaligned<int32_t>(ip);
            }

        }
        else
        {
            //
            // Get the value we need from the register.
            //

            // Check for RIP-relative addressing mode.
            if ((mod == 0) && (rm == 5))
            {
                result = (uint64_t)ip + sizeof(int32_t) + ReadUnaligned<int32_t>(ip);
            }
            else
            {
                result = getRegister(rmIndex);

                if (mod == 1)
                {
                    result += ReadUnaligned<int8_t>(ip);
                }
                else if (mod == 2)
                {
                    result += ReadUnaligned<int32_t>(ip);
                }
            }
        }

        break;

    case 3:
    default:
        // The operand is stored in a register.
        if (isAhChDhBh)
        {
            // 8 bit idiv without the REX prefix uses registers AH, CH, DH or BH for rm 4..8.
            // So we shift the register index to get the real register index.
            rmIndex -= 4;
        }

        resultReg = getRegister(rmIndex);
        result = (uint64_t)&resultReg;

        if (isAhChDhBh)
        {
            // Move one byte higher to get an address of the AH, CH, DH or BH
            result++;
        }

        break;

    }

    //
    // Now dereference thru the result to get the resulting value.
    //
    if (is8Bit)
    {
        result = ReadUnaligned<uint8_t>((const void*)result);
    }
    else if (rex_w != 0)
    {
        result = ReadUnaligned<uint64_t>((const void*)result);
    }
    else if (hasOpSizePrefix)
    {
        result = ReadUnaligned<uint16_t>((const void*)result);
    }
    else
    {
        result = ReadUnaligned<uint32_t>((const void*)result);
    }

    return result;
}

// Skip all prefixes until the instruction code or the REX prefix is found
// Parameters:
//  uint8_t** ip :          Pointer to the current instruction pointer. Updated
//                          as the function walks the codes.
//  bool* hasOpSizePrefix : Pointer to bool, on exit set to true if a op size prefix
//                          was found.
// Return value :
//  Code of the REX prefix or the instruction code after the prefixes.
inline uint8_t SkipPrefixes(uint8_t **ip, bool* hasOpSizePrefix)
{
    *hasOpSizePrefix = false;

    while (true)
    {
        uint8_t code = *(*ip)++;

        switch (code)
        {
        case 0x66: // Operand-Size
            *hasOpSizePrefix = true;
            break;

            // Segment overrides
        case 0x26: // ES
        case 0x2E: // CS
        case 0x36: // SS
        case 0x3E: // DS
        case 0x64: // FS
        case 0x65: // GS

            // Size overrides
        case 0x67: // Address-Size

            // Lock
        case 0xf0:

            // String REP prefixes
        case 0xf2: // REPNE/REPNZ
        case 0xf3:
            break;

        default:
            // Return address of the nonprefix code
            return code;
        }
    }
}

// Check if the divisor of the DIV / IDIV instruction at ip is non-zero. The x64
// processor generates the same exception for division by zero and division
// overflow, so a division by zero exception with a non-zero divisor is an overflow.
// Parameters:
//  uint8_t* ip :           address of the faulting instruction
//  getRegister :           callable returning the value of the register with the given index;
//                          indices 16..31 (R16..R31) are only used by APX encoded instructions
// Return value :
//  true if the divisor is non-zero, i.e. the division error was an overflow
template <typename TGetRegister>
bool DivInstructionHasNonZeroDivisor(uint8_t* ip, TGetRegister getRegister)
{
    uint8_t rex = 0;
    bool hasOpSizePrefix = false;

    uint8_t code = SkipPrefixes(&ip, &hasOpSizePrefix);

    // Details on REX2 and promoted EVEX can be found in Intel Advanced Performance Extensions (APX) Architecture specification 3.1.2.
    // The EVEX prefix (0x62) can encode legacy IDIV/DIV in APX map 4.
    // EVEX format: 0x62 P0 P1 P2 opcode ModRM ...
    //   P0[7:5] = ~R3:~X3:~B3 (inverted register extension bits)
    //   P0[4]   = ~R4 (inverted)
    //   P0[3]   = B4 (not inverted, extends B to 5 bits for APX)
    //   P0[2:0] = mmm (map: 100 = map 4 for legacy promoted)
    //   P1[7]   = W (operand size, not inverted)
    //   P1[1:0] = pp (00=none, 01=66, 10=F3, 11=F2)
    if (code == 0x62)
    {
        uint8_t p0 = *ip++;
        uint8_t p1 = *ip++;
        uint8_t p2 = *ip++; // P2 (contains NF, ND bits - not needed for operand decoding)
        (void)p2;

        // Only map 4 (APX legacy promoted) can encode IDIV/DIV.
        // If this is a different EVEX map, we cannot decode it — treat as not an overflow.
        if ((p0 & 0x07) != 0x04)
        {
            _ASSERTE(!"Unexpected EVEX map for legacy DIV/IDIV decoding");
            return false;
        }

        // Extract register extension bits from EVEX (inverted in P0, not inverted for W in P1)
        uint8_t evex_b3 = (~p0 >> 5) & 1;
        uint8_t evex_x3 = (~p0 >> 6) & 1;
        uint8_t evex_b4 = (p0 >> 3) & 1;   // P0[3] = B4 (not inverted for APX)
        uint8_t evex_x4 = (~p1 >> 2) & 1;  // P1[2] = ~X4 (inverted for APX legacy promoted)
        uint8_t evex_w = (p1 >> 7) & 1;

        // Construct a REX-equivalent value for GetModRMOperandValue.
        // Bits [3:0] = W:R:X3:B3, Bits [5:4] = X4:B4
        // Use 0x40 base to ensure rex != 0 (disables AH/CH/DH/BH interpretation for 8-bit ops).
        rex = 0x40 | (evex_w << 3) | (evex_x3 << 1) | evex_b3 | (evex_b4 << 4) | (evex_x4 << 5);

        // Check pp field for operand-size prefix equivalent (pp=01 means 0x66 prefix)
        if ((p1 & 0x03) == 0x01)
        {
            hasOpSizePrefix = true;
        }

        code = *ip++;
    }
    // The REX2 prefix (0xD5) is a 2-byte prefix: 0xD5 followed by a payload byte.
    // Payload format: [M:R4:X4:B4:W:R3:X3:B3]
    //   M    = map select (0 = map 0, 1 = map 1)
    //   Bits [3:0] = W:R3:X3:B3 (same layout as REX lower 4 bits)
    //   Bits [6:4] = R4:X4:B4 (extend register indices to 5 bits)
    else if (code == 0xD5)
    {
        uint8_t payload = *ip++;

        // Only map 0 (M=0) can encode IDIV/DIV (opcodes F6/F7).
        // If M=1 (map 1, i.e. 0F-prefixed), this is not an IDIV/DIV instruction.
        if ((payload & 0x80) != 0)
        {
            _ASSERTE(!"Invalid instruction (expected IDIV or DIV)");
            return false;
        }

        // Construct a REX-equivalent value.
        // Lower 4 bits (W:R3:X3:B3) have the same layout as the REX prefix bits.
        // Additionally pack B4 into bit 4 and X4 into bit 5 for GetModRMOperandValue.
        uint8_t rex2_b4 = (payload >> 4) & 1;
        uint8_t rex2_x4 = (payload >> 5) & 1;
        rex = 0x40 | (payload & 0x0F) | (rex2_b4 << 4) | (rex2_x4 << 5);

        code = *ip++;
    }
    // The REX prefix must directly precede the instruction code
    else if ((code & 0xF0) == 0x40)
    {
        rex = code;
        code = *ip++;
    }

    uint64_t divisor = 0;

    // Check if the instruction is IDIV or DIV. The instruction code includes the three
    // 'reg' bits in the ModRM byte. These are 7 for IDIV and 6 for DIV
    uint8_t regBits = (*ip & 0x38) >> 3;
    if ((code == 0xF7 || code == 0xF6) && (regBits == 7 || regBits == 6))
    {
        bool is8Bit = (code == 0xF6);
        divisor = GetModRMOperandValue(rex, ip, getRegister, is8Bit, hasOpSizePrefix);
    }
    else
    {
        _ASSERTE(!"Invalid instruction (expected IDIV or DIV)");
    }

    // If the division operand is zero, it was division by zero. Otherwise the failure
    // must have been an overflow.
    return divisor != 0;
}

#endif // _DIV_INSTRUCTION_DECODER_H_
