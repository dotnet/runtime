// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//////////////////////////////////////////////////////////////////////////////

// clang-format off
#if !defined(TARGET_POWERPC64)
#error Unexpected target type
#endif

#ifdef DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////

#undef DEFINE_ID_OPS

enum ID_OPS
{
    ID_OP_NONE, // no additional arguments
    ID_OP_SCNS, // small const  operand (21-bits or less, no reloc)
    ID_OP_JMP,  // local jump
    ID_OP_CALL, // method call
    ID_OP_SPEC, // special handling required
};

//////////////////////////////////////////////////////////////////////////////
#else // !DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////

#ifndef IF_DEF
#error Must define IF_DEF macro before including this file
#endif

//////////////////////////////////////////////////////////////////////////////
//
// enum insFormat   instruction            enum ID_OPS
//                  scheduling
//                  (unused)
//////////////////////////////////////////////////////////////////////////////

//TODO POWERPC64 define instruction format

IF_DEF(NONE, IS_NONE, NONE) //
// Jump/Branch formats
IF_DEF(LABEL, IS_NONE, JMP)    // label
IF_DEF(BI_0, IS_NONE, JMP)     // Branch conditional (beq, bne, etc.)
IF_DEF(BI_1, IS_NONE, JMP)     // Branch unconditional (b)

// Register-Immediate formats
IF_DEF(RI_1A, IS_NONE, SCNS)   // li  rD, imm16
IF_DEF(RI_1B, IS_NONE, SCNS)   // lis rD, imm16
IF_DEF(RI_1C, IS_NONE, SCNS)   // addi rD, rA, imm16
IF_DEF(RI_1D, IS_NONE, SCNS)   // ori rD, rA, imm16

// Register-Register formats
IF_DEF(RR_1A, IS_NONE, NONE)   // mr rD, rA (or rD, rA, rA)
IF_DEF(RR_2A, IS_NONE, NONE)   // add rD, rA, rB
IF_DEF(RR_2B, IS_NONE, NONE)   // cmpw rA, rB

// Register-Register-Immediate formats
IF_DEF(RRI_1A, IS_NONE, SCNS)  // cmpwi crD, rA, imm16

// Load/Store formats
IF_DEF(LS_2A, IS_NONE, SCNS)   // lwz rD, disp(rA)
IF_DEF(LS_2B, IS_NONE, SCNS)   // stw rS, disp(rA)
IF_DEF(LS_2C, IS_NONE, SCNS)   // ld  rD, disp(rA)
IF_DEF(LS_2D, IS_NONE, SCNS)   // std rS, disp(rA)
IF_DEF(LS_2E, IS_NONE, SCNS)   // lwa rD, disp(rA)
IF_DEF(LS_2F, IS_NONE, SCNS)   // stdu rS, disp(rA)

// Special formats
IF_DEF(SR_1A, IS_NONE, NONE)   // mflr rD
IF_DEF(SR_1B, IS_NONE, NONE)   // mtlr rS
IF_DEF(SR_1C, IS_NONE, NONE)   // blr
IF_DEF(SR_1D, IS_NONE, NONE)   // nop

// Call format
IF_DEF(CALL, IS_NONE, CALL)    // bl target

//////////////////////////////////////////////////////////////////////////////
#undef IF_DEF
//////////////////////////////////////////////////////////////////////////////

#endif // !DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////
// clang-format on
