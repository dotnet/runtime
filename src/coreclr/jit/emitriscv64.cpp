// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                             emitriscv64.cpp                               XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_RISCV64)

/*****************************************************************************/
/*****************************************************************************/

#include "instr.h"
#include "emit.h"
#include "codegen.h"

/*****************************************************************************/

const instruction emitJumpKindInstructions[] = {
    INS_nop,

#define JMP_SMALL(en, rev, ins) INS_##ins,
#include "emitjmps.h"
};

const emitJumpKind emitReverseJumpKinds[] = {
    EJ_NONE,

#define JMP_SMALL(en, rev, ins) EJ_##rev,
#include "emitjmps.h"
};

/*****************************************************************************
 * Look up the instruction for a jump kind
 */

/*static*/ instruction emitter::emitJumpKindToIns(emitJumpKind jumpKind)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    assert((unsigned)jumpKind < ArrLen(emitJumpKindInstructions));
    return emitJumpKindInstructions[jumpKind];
}

/*****************************************************************************
* Look up the jump kind for an instruction. It better be a conditional
* branch instruction with a jump kind!
*/

/*static*/ emitJumpKind emitter::emitInsToJumpKind(instruction ins)
{
    NYI_RISCV64("emitInsToJumpKind-----unimplemented on RISCV64 yet----");
    return EJ_NONE;
}

/*****************************************************************************
 * Reverse the conditional jump
 */

/*static*/ emitJumpKind emitter::emitReverseJumpKind(emitJumpKind jumpKind)
{
    assert(jumpKind < EJ_COUNT);
    return emitReverseJumpKinds[jumpKind];
}

/*****************************************************************************
 *
 *  Return the allocated size (in bytes) of the given instruction descriptor.
 */

size_t emitter::emitSizeOfInsDsc(instrDesc* id)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
}

inline bool emitter::emitInsMayWriteToGCReg(instruction ins)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
}

bool emitter::emitInsWritesToLclVarStackLoc(instrDesc* id)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
}

#define LD 1
#define ST 2

// clang-format off
/*static*/ const BYTE CodeGenInterface::instInfo[] =
{
    #define INST(id, nm, info, e1) info,
    #include "instrs.h"
};
// clang-format on

//------------------------------------------------------------------------
// emitInsLoad: Returns true if the instruction is some kind of load instruction.
//
bool emitter::emitInsIsLoad(instruction ins)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
}

//------------------------------------------------------------------------
// emitInsIsStore: Returns true if the instruction is some kind of store instruction.
//
bool emitter::emitInsIsStore(instruction ins)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
}

//-------------------------------------------------------------------------
// emitInsIsLoadOrStore: Returns true if the instruction is some kind of load/store instruction.
//
bool emitter::emitInsIsLoadOrStore(instruction ins)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
}

/*****************************************************************************
 *
 *  Returns the specific encoding of the given CPU instruction.
 */

inline emitter::code_t emitter::emitInsCode(instruction ins /*, insFormat fmt*/)
{
    code_t code = BAD_CODE;

    // clang-format off
    const static code_t insCode[] =
    {
        #define INST(id, nm, info, e1) e1,
        #include "instrs.h"
    };
    // clang-format on

    code = insCode[ins];

    assert((code != BAD_CODE));

    return code;
}

/****************************************************************************
 *
 *  Add an instruction with no operands.
 */

void emitter::emitIns(instruction ins)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *  emitter::emitIns_S_R() and emitter::emitIns_R_S():
 *
 *  Add an Load/Store instruction(s): base+offset and base-addr-computing if needed.
 *  For referencing a stack-based local variable and a register
 *
 */
void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*
 *  Special notes for `offs`, please see the comment for `emitter::emitIns_S_R`.
 */
void emitter::emitIns_R_S(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction with a single immediate value.
 */

void emitter::emitIns_I(instruction ins, emitAttr attr, ssize_t imm)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void emitter::emitIns_I_I(instruction ins, emitAttr attr, ssize_t cc, ssize_t offs)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing a register and a constant.
 */

void emitter::emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm, insOpts opt /* = INS_OPTS_NONE */)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

//------------------------------------------------------------------------
// emitIns_Mov: Emits a move instruction
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    dstReg    -- The destination register
//    srcReg    -- The source register
//    canSkip   -- true if the move can be elided when dstReg == srcReg, otherwise false
//    insOpts   -- The instruction options
//
void emitter::emitIns_Mov(
    instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip, insOpts opt /* = INS_OPTS_NONE */)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers
 */

void emitter::emitIns_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insOpts opt /* = INS_OPTS_NONE */)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and a constant.
 */

void emitter::emitIns_R_R_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insOpts opt /* = INS_OPTS_NONE */)
{
    code_t code = emitInsCode(ins);
    if (INS_addi == ins || INS_slli == ins)
    {
        code |= reg1 << 15;          // rs1
        code |= reg2 << 7;           // rd
        code |= imm << 20;           // imm
    }
    else
    {
        fprintf(stderr, "[CLAMP] %x\n", code);
        _ASSERTE(!"TODO RISCV64 NYI");
    }
    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaSetInstrEncode(code);
    id->idCodeSize(4);
}

/*****************************************************************************
 *
 *  Add an instruction referencing three registers.
 */

void emitter::emitIns_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, insOpts opt) /* = INS_OPTS_NONE */
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing three registers and a constant.
 */

void emitter::emitIns_R_R_R_I(instruction ins,
                              emitAttr    attr,
                              regNumber   reg1,
                              regNumber   reg2,
                              regNumber   reg3,
                              ssize_t     imm,
                              insOpts     opt /* = INS_OPTS_NONE */,
                              emitAttr    attrReg2 /* = EA_UNKNOWN */)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and two constants.
 */

void emitter::emitIns_R_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int imm1, int imm2, insOpts opt)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing four registers.
 */

void emitter::emitIns_R_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, regNumber reg4)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction with a register + static member operands.
 *  Constant is stored into JIT data which is adjacent to code.
 *
 */
void emitter::emitIns_R_C(
    instruction ins, emitAttr attr, regNumber reg, regNumber addrReg, CORINFO_FIELD_HANDLE fldHnd, int offs)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void emitter::emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs)
{
    NYI_RISCV64("emitIns_R_AR-----unimplemented/unused on RISCV64 yet----");
}

// This computes address from the immediate which is relocatable.
void emitter::emitIns_R_AI(instruction ins,
                           emitAttr    attr,
                           regNumber   reg,
                           ssize_t addr DEBUGARG(size_t targetHandle) DEBUGARG(GenTreeFlags gtFlags))
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Record that a jump instruction uses the short encoding
 *
 */
void emitter::emitSetShortJump(instrDescJmp* id)
{
    // TODO-RISCV64: maybe delete it on future.
    NYI_RISCV64("emitSetShortJump-----unimplemented/unused on RISCV64 yet----");
}

/*****************************************************************************
 *
 *  Add a label instruction.
 */

void emitter::emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void emitter::emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    NYI_RISCV64("emitIns_J_R-----unimplemented/unused on RISCV64 yet----");
}

void emitter::emitIns_J(instruction ins, BasicBlock* dst, int instrCount)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void emitter::emitIns_J_cond_la(instruction ins, BasicBlock* dst, regNumber reg1, regNumber reg2)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void emitter::emitIns_I_la(emitAttr size, regNumber reg, ssize_t imm)
{
    assert(!EA_IS_RELOC(size));
    assert(isGeneralRegister(reg));
    if (0 == ((imm + 0x800) >> 32)) {
        if (((imm + 0x800) >> 12) != 0)
        {
            emitIns_R_I(INS_lui, size, reg, ((imm + 0x800) >> 12));
            if ((imm & 0xFFF) != 0)
            {
                emitIns_R_R_I(INS_addi, size, reg, reg, imm & 0xFFF);
            }
        }
        else
        {
            emitIns_R_R_I(INS_addi, size, REG_R0, reg, imm & 0xFFF);
        }
    }
    else
    {
        UINT32 upper = imm >> 32;
        if (((upper + 0x800) >> 12) != 0)
        {
            emitIns_R_I(INS_lui, size, reg, ((upper + 0x800) >> 12));
        }
        if ((upper & 0xFFF) != 0)
        {
            emitIns_R_R_I(INS_addi, size, REG_R0, reg, upper & 0xFFF);
        }
        UINT32 lower = (imm << 32) >> 32;
        UINT32 shift = 0;
        for (int i = 32; i >= 0; i -= 11)
        {
            shift += i > 11 ? 11 : i;
            UINT32 current = lower >> (i < 11 ? 0 : i - 11);
            if (current != 0)
            {
                emitIns_R_R_I(INS_slli, size, reg, reg, shift);
                emitIns_R_R_I(INS_addi, size, reg, reg, current & 0x7FF);
                shift = 0;
            }
        }
        if (shift)
        {
            emitIns_R_R_I(INS_slli, size, reg, reg, shift);
        }
    }
}

/*****************************************************************************
 *
 *  Add a call instruction (direct or indirect).
 *      argSize<0 means that the caller will pop the arguments
 *
 * The other arguments are interpreted depending on callType as shown:
 * Unless otherwise specified, ireg,xreg,xmul,disp should have default values.
 *
 * EC_FUNC_TOKEN       : addr is the method address
 *
 * If callType is one of these emitCallTypes, addr has to be NULL.
 * EC_INDIR_R          : "call ireg".
 *
 */

void emitter::emitIns_Call(EmitCallType          callType,
                           CORINFO_METHOD_HANDLE methHnd,
                           INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                           void*    addr,
                           ssize_t  argSize,
                           emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                           VARSET_VALARG_TP ptrVars,
                           regMaskTP        gcrefRegs,
                           regMaskTP        byrefRegs,
                           const DebugInfo& di /* = DebugInfo() */,
                           regNumber        ireg /* = REG_NA */,
                           regNumber        xreg /* = REG_NA */,
                           unsigned         xmul /* = 0     */,
                           ssize_t          disp /* = 0     */,
                           bool             isJump /* = false */)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Output a call instruction.
 */

unsigned emitter::emitOutputCall(insGroup* ig, BYTE* dst, instrDesc* id, code_t code)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
}

void emitter::emitJumpDistBind()
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 instruction
 */

/*static*/ unsigned emitter::emitOutput_Instr(BYTE* dst, code_t code)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
}

/*****************************************************************************
*
 *  Append the machine code corresponding to the given instruction descriptor
 *  to the code block at '*dp'; the base of the code block is 'bp', and 'ig'
 *  is the instruction group that contains the instruction. Updates '*dp' to
 *  point past the generated code, and returns the size of the instruction
 *  descriptor in bytes.
 */

size_t emitter::emitOutputInstr(insGroup* ig, instrDesc* id, BYTE** dp)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
}

/*****************************************************************************/
/*****************************************************************************/

#ifdef DEBUG

// clang-format off
static const char* const RegNames[] =
{
    #define REGDEF(name, rnum, mask, sname) sname,
    #include "register.h"
};
// clang-format on

//----------------------------------------------------------------------------------------
// Disassemble the given instruction.
// The `emitter::emitDisInsName` is focused on the most important for debugging.
// So it implemented as far as simply and independently which is very useful for
// porting easily to the release mode.
//
// Arguments:
//    code - The instruction's encoding.
//    addr - The address of the code.
//    id   - The instrDesc of the code if needed.
//
// Note:
//    The length of the instruction's name include aligned space is 13.
//

void emitter::emitDisInsName(code_t code, const BYTE* addr, instrDesc* id)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Display (optionally) the instruction encoding in hex
 */

void emitter::emitDispInsHex(instrDesc* id, BYTE* code, size_t sz)
{
    // We do not display the instruction hex if we want diff-able disassembly
    if (!emitComp->opts.disDiffable)
    {
        if (sz == 4)
        {
            printf("  %08X    ", (*((code_t*)code)));
        }
        else
        {
            assert(sz == 0);
            printf("              ");
        }
    }
}

void emitter::emitDispIns(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig)
{
    // LA implements this similar by `emitter::emitDisInsName`.
    // For LA maybe the `emitDispIns` is over complicate.
    // The `emitter::emitDisInsName` is focused on the most important for debugging.
    NYI_RISCV64("LA not used the emitter::emitDispIns");
}

/*****************************************************************************
 *
 *  Display a stack frame reference.
 */

void emitter::emitDispFrameRef(int varx, int disp, int offs, bool asmfm)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

#endif // DEBUG

// Generate code for a load or store operation with a potentially complex addressing mode
// This method handles the case of a GT_IND with contained GT_LEA op1 of the x86 form [base + index*sccale + offset]
//
void emitter::emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

// The callee must call genConsumeReg() for any non-contained srcs
// and genProduceReg() for any non-contained dsts.

regNumber emitter::emitInsBinary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src)
{
    NYI_RISCV64("emitInsBinary-----unused");
    return REG_R0;
}

// The callee must call genConsumeReg() for any non-contained srcs
// and genProduceReg() for any non-contained dsts.
regNumber emitter::emitInsTernary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src1, GenTree* src2)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return REG_R0;
    // dst can only be a reg
}

unsigned emitter::get_curTotalCodeSize()
{
    return emitTotalCodeSize;
}

#if defined(DEBUG) || defined(LATE_DISASM)

//----------------------------------------------------------------------------------------
// getInsExecutionCharacteristics:
//    Returns the current instruction execution characteristics
//
// Arguments:
//    id  - The current instruction descriptor to be evaluated
//
// Return Value:
//    A struct containing the current instruction execution characteristics
//
// Notes:
//    The instruction latencies and throughput values returned by this function
//    are NOT accurate and just a function feature.
emitter::insExecutionCharacteristics emitter::getInsExecutionCharacteristics(instrDesc* id)
{
    insExecutionCharacteristics result;

    // TODO-RISCV64: support this function.
    result.insThroughput       = PERFSCORE_THROUGHPUT_ZERO;
    result.insLatency          = PERFSCORE_LATENCY_ZERO;
    result.insMemoryAccessKind = PERFSCORE_MEMORY_NONE;

    return result;
}

#endif // defined(DEBUG) || defined(LATE_DISASM)

#ifdef DEBUG
//------------------------------------------------------------------------
// emitRegName: Returns a general-purpose register name or SIMD and floating-point scalar register name.
//
// TODO-RISCV64: supporting SIMD.
// Arguments:
//    reg - A general-purpose register orfloating-point register.
//    size - unused parameter.
//    varName - unused parameter.
//
// Return value:
//    A string that represents a general-purpose register name or floating-point scalar register name.
//
const char* emitter::emitRegName(regNumber reg, emitAttr size, bool varName)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return nullptr;
}
#endif

//------------------------------------------------------------------------
// IsMovInstruction: Determines whether a give instruction is a move instruction
//
// Arguments:
//    ins       -- The instruction being checked
//
bool emitter::IsMovInstruction(instruction ins)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
}

#endif // defined(TARGET_RISCV64)
