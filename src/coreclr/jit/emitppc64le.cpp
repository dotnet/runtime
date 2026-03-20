// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                             emitppc64le.cpp                               XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_POWERPC64)

/*****************************************************************************/
/*****************************************************************************/

#include "instr.h"
#include "emit.h"
#include "codegen.h"

/*****************************************************************************/

#if 0
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
#endif

/*****************************************************************************
 * Look up the instruction for a jump kind
 */

/*static*/ instruction emitter::emitJumpKindToIns(emitJumpKind jumpKind)
{
    _ASSERTE(!"NYI");
}

/*****************************************************************************
 * Look up the jump kind for an instruction. It better be a conditional
 * branch instruction with a jump kind!
 */

/*static*/ emitJumpKind emitter::emitInsToJumpKind(instruction ins)
{
    _ASSERTE(!"NYI");
}

/*****************************************************************************
 * Reverse the conditional jump
 */

/*static*/ emitJumpKind emitter::emitReverseJumpKind(emitJumpKind jumpKind)
{
    _ASSERTE(!"NYI");
}


/*****************************************************************************
 *
 *  Return the allocated size (in bytes) of the given instruction descriptor.
 */

size_t emitter::emitSizeOfInsDsc(instrDesc* id) const
{
    //_ASSERTE(!"NYI");
    return sizeof(instrDesc);
}

#ifdef DEBUG
/*****************************************************************************
 *
 *  The following is called for each recorded instruction -- use for debugging.
 */
void emitter::emitInsSanityCheck(instrDesc* id)
{
   // _ASSERTE(!"NYI"); // will diabsle it now, not to assert for BLR
}
#endif // DEBUG

#if 0
bool emitter::emitInsMayWriteToGCReg(instrDesc* id)
{
    _ASSERTE(!"NYI");
}

bool emitter::emitInsWritesToLclVarStackLoc(instrDesc* id)
{
	    _ASSERTE(!"NYI");
}

bool emitter::emitInsWritesToLclVarStackLocPair(instrDesc* id)
{
	    _ASSERTE(!"NYI");
}

bool emitter::emitInsMayWriteMultipleRegs(instrDesc* id)
{
	    _ASSERTE(!"NYI");
}
#endif

//------------------------------------------------------------------------
// emitRegName: Returns a general-purpose register name or SIMD and floating-point scalar register name.
//
// Arguments:
//    reg - A general-purpose register or SIMD and floating-point register.
//    size - A register size.
//    varName - unused parameter.
//
// Return value:
//    A string that represents a general-purpose register name or SIMD and floating-point scalar register name.
//
const char* emitter::emitRegName(regNumber reg, emitAttr size, bool varName) const
{
   // _ASSERTE(!"NYI");
   return "r?"; //TODO:JK, only for BLR will be changed
}

/*****************************************************************************
*
*  Add an instruction referencing a register and a stack-based local variable.
*/
void emitter::emitIns_R_S(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    //TODO POWERPC64 vikas
    _ASSERTE(!"NYI");
}


/*****************************************************************************
*
*  Add an instruction referencing a stack-based local variable and a register
*/
void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    //TODO POWERPC64 vikas
    _ASSERTE(!"NYI POWERPC64");
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
    //TODO POWERPC64 vikas
    _ASSERTE(!"NYI POWERPC64");
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
 * EC_FUNC_ADDR        : addr is the absolute address of the function
 *
 * If callType is one of these emitCallTypes, addr has to be NULL.
 * EC_INDIR_R          : "call ireg".
 *
 * For ARM xreg, xmul and disp are never used and should always be 0/REG_NA.
 *
 * noSafePoint - force not making this call a safe point in partially interruptible code
 *
 *  Please consult the "debugger team notification" comment in genFnProlog().
 */

void emitter::emitIns_Call(EmitCallType          callType,
                           CORINFO_METHOD_HANDLE methHnd,
                           INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                           void*            addr,
                           ssize_t          argSize,
                           emitAttr         retSize,
                           emitAttr         secondRetSize,
                           VARSET_VALARG_TP ptrVars,
                           regMaskTP        gcrefRegs,
                           regMaskTP        byrefRegs,
                           const DebugInfo& di /* = DebugInfo() */,
                           regNumber        ireg /* = REG_NA */,
                           regNumber        xreg /* = REG_NA */,
                           unsigned         xmul /* = 0     */,
                           ssize_t          disp /* = 0     */,
                           bool             isJump /* = false */,
                           bool             noSafePoint /* = false */)
{
    //TODO POWERPC64 vikas
    _ASSERTE(!"NYI POWERPC64");
}

void emitter::emitIns_R_I(instruction ins,
                          emitAttr    attr,
                          regNumber   reg,
                          ssize_t     imm,
                          insOpts     opt,     /* = INS_OPTS_NONE */
                          insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */
                              DEBUGARG(size_t targetHandle /* = 0 */) DEBUGARG(GenTreeFlags gtFlags /* = GTF_EMPTY */))
{
    //TODO POWERPC64 vikas
    _ASSERTE(!"NYI POWERPC64");
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers
 */

void emitter::emitIns_R_R(instruction     ins,
			  emitAttr        attr,
			  regNumber       reg1,
			  regNumber       reg2,
			  insOpts         opt /* = INS_OPTS_NONE */,
			  insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    _ASSERTE(!"NYI");
}


void emitter::emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs)
{
    //TODO POWERPC64 vikas
    _ASSERTE(!"NYI POWERPC64");
}

void emitter::emitIns_AR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs)
{
    //TODO POWERPC64 vikas
    _ASSERTE(!"NYI POWERPC64");
}

void emitter::emitIns(instruction ins)
{
    instrDesc* id = emitNewInstr(EA_4BYTE);

    id->idIns(ins);

    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction with a single immediate value.
 */

void emitter::emitIns_I(instruction ins, emitAttr attr, ssize_t imm)
{
	    _ASSERTE(!"NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing a single register.
 */

void emitter::emitIns_R(instruction ins, emitAttr attr, regNumber reg, insOpts opt /* = INS_OPTS_NONE */)
{
	    _ASSERTE(!"NYI");
}

// clang-format off 
/*static*/ const BYTE CodeGenInterface::instInfo[] =
{
	    #define INST(id, nm, info, fmt, e1) info,
	    #include "instrs.h"
};
// clang-format on



//------------------------------------------------------------------------
// emitInsIsCompare: Returns true if the instruction is some kind of compare or test instruction.
//
bool emitter::emitInsIsCompare(instruction ins)
{
    //TODO POWERPC64 vikas JK
    //_ASSERTE(!"NYI POWERPC64");
    return false;
}

//------------------------------------------------------------------------
// emitInsIsLoad: Returns true if the instruction is some kind of load instruction.
//
bool emitter::emitInsIsLoad(instruction ins)
{
    //TODO POWERPC64 vikas
    //_ASSERTE(!"NYI POWERPC64");
    return false;
}

//------------------------------------------------------------------------
// emitInsIsStore: Returns true if the instruction is some kind of store instruction.
//
bool emitter::emitInsIsStore(instruction ins)
{
    //TODO POWERPC64 vikas
    //_ASSERTE(!"NYI POWERPC64");
    return false;
}

//------------------------------------------------------------------------
// emitInsIsLoadOrStore: Returns true if the instruction is a load or store instruction.
//
bool emitter::emitInsIsLoadOrStore(instruction ins)
{
    return false;
}
/*****************************************************************************
 *
 *  Record that a jump instruction uses the short encoding
 *
 */
void emitter::emitSetShortJump(instrDescJmp* id)
{
    _ASSERTE(!"NYI");
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
    BYTE* dst = *dp;
    BYTE* dstRW = dst + writeableOffset;
    
    switch (id->idIns())
    {
       case INS_nop:
           ppc_nop (dstRW);
           break;

       case INS_blr:
	   ppc_blr (dstRW);
           break;

       default:
           _ASSERTE(!"NYI");
    }

    dst = dstRW - writeableOffset;
    *dp = dst;
    return emitSizeOfInsDsc(id);
}

/*****************************************************************************
 *
 *  Display (optionally) the instruction encoding in hex
 */

void emitter::emitDispInsHex(instrDesc* id, BYTE* code, size_t sz)
{
    //_ASSERTE(!"NYI");
}

/*****************************************************************************
 *
 *  Wrapper for emitter::emitDispInsHelp() that handles special large jump
 *  pseudo-instruction.
 */

void emitter::emitDispIns(
		    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig)
{
    //_ASSERTE(!"NYI");
}

/*****************************************************************************
 *
 *  Display a stack frame reference.
 */

void emitter::emitDispFrameRef(int varx, int disp, int offs, bool asmfm)
{
    _ASSERTE(!"NYI");
}

// The callee must call genConsumeReg() for any non-contained srcs
// and genProduceReg() for any non-contained dsts.

regNumber emitter::emitInsBinary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src)
{
     _ASSERTE(!"NYI");
}

// The callee must call genConsumeReg() for any non-contained srcs
// and genProduceReg() for any non-contained dsts.

regNumber emitter::emitInsTernary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src1, GenTree* src2)
{
    _ASSERTE(!"NYI");
}

#if defined(DEBUG) || defined(LATE_DISASM)
#if 0
void emitter::getMemoryOperation(instrDesc* id, unsigned* pMemAccessKind, bool* pIsLocalAccess)
{
    //TODO POWERPC64 vikas
    _ASSERTE(!"NYI POWERPC64");
}
#endif

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
//    are from
//
//    The Arm Cortex-A55 Software Optimization Guide:
//    https://static.docs.arm.com/epm128372/20/arm_cortex_a55_software_optimization_guide_v2.pdf
//
emitter::insExecutionCharacteristics emitter::getInsExecutionCharacteristics(instrDesc* id)
{

	insExecutionCharacteristics result;

    // TODO-PPC64LE: support this function.
    	result.insThroughput       = PERFSCORE_THROUGHPUT_ZERO;
    	result.insLatency          = PERFSCORE_LATENCY_ZERO;
    	result.insMemoryAccessKind = PERFSCORE_MEMORY_NONE;

    	return result;
}

#endif // defined(DEBUG) || defined(LATE_DISASM)


#if defined(FEATURE_SIMD)
//-----------------------------------------------------------------------------------
// emitStoreSimd12ToLclOffset: store SIMD12 value from dataReg to varNum+offset.
//
// Arguments:
//     varNum         - the variable on the stack to use as a base;
//     offset         - the offset from the varNum;
//     dataReg        - the src reg with SIMD12 value;
//     tmpRegProvider - a tree to grab a tmp reg from if needed.
//
void emitter::emitStoreSimd12ToLclOffset(unsigned varNum, unsigned offset, regNumber dataReg, GenTree* tmpRegProvider)
{
    _ASSERTE(!"NYI");
}
#endif // FEATURE_SIMD

#endif //TARGET_POWERPC64
