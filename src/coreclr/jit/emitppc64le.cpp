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
	assert(jumpKind < EJ_COUNT);
	return emitJumpKindInstructions[jumpKind];
}

/*****************************************************************************
 * Look up the jump kind for an instruction. It better be a conditional
 * branch instruction with a jump kind!
 */

/*static*/ emitJumpKind emitter::emitInsToJumpKind(instruction ins)
{
    switch (ins)
    {
        case INS_b:
            return EJ_jmp;
        case INS_beq:
            return EJ_eq;
        case INS_bne:
            return EJ_ne;
	case INS_blt:
	    return EJ_lt;
	case INS_bge:
	    return EJ_ge;
	case INS_bgt:
	    return EJ_gt;
	case INS_ble:
	    return EJ_le;
        default:
            unreached();
    }
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
    assert(offs >= 0);
    emitAttr size = EA_SIZE(attr);
    insFormat fmt = IF_NONE;

    /* Figure out the variable's frame position */
    bool FPbased;
    int base = emitComp->lvaFrameAddress(varx, &FPbased);
    int disp = base + offs;
    ssize_t imm = disp;

    // Use frame pointer (R31) or stack pointer (R1) as base register
    regNumber reg2 = FPbased ? REG_FPBASE : REG_SPBASE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_stb:
            // Store byte - D-form instruction
            assert(isGeneralRegister(reg1));
            assert(size == EA_1BYTE);
            break;

        case INS_sth:
            // Store halfword - D-form instruction
            assert(isGeneralRegister(reg1));
            assert(size == EA_2BYTE);
            break;

        case INS_stw:
            // Store word - D-form instruction
            assert(isGeneralRegister(reg1));
            assert(size == EA_4BYTE);
            break;

        case INS_std:
            // Store doubleword - DS-form instruction (must be 4-byte aligned)
            assert(isGeneralRegister(reg1));
            assert(size == EA_8BYTE);
            // DS-form requires 4-byte alignment
            assert((imm & 0x3) == 0);
            break;

        default:
            NYI("emitIns_S_R"); // Floating-point stores not yet implemented
            return;
    }

    // Validate immediate range
    if (ins == INS_std)
    {
        // DS-form: 14-bit signed immediate, must be 4-byte aligned
        assert(imm >= -32768 && imm <= 32764);
        assert((imm & 0x3) == 0);
    }
    else
    {
        // D-form: 16-bit signed immediate
        assert(imm >= -32768 && imm <= 32767);
    }

    // Create instruction descriptor with immediate offset
    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    //id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);
    id->idSetIsLclVar();

#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

    dispIns(id);
    appendToCurIG(id);
}

bool emitter::IsRedundantMov(instruction ins, emitAttr size, regNumber dst, regNumber src, bool canSkip)
{
    assert((ins == INS_mov)); //|| (ins == INS_sve_mov));

    if (canSkip && (dst == src))
    {
        // These elisions used to be explicit even when optimizations were disabled
        return true;
    }

    _ASSERTE(!"NYI POWERPC64");
}

//------------------------------------------------------------------------
// IsMovInstruction: Determines whether a give instruction is a move instruction
//
// Arguments:
//    ins       -- The instruction being checked
//
bool emitter::IsMovInstruction(instruction ins)
{
    switch (ins)
    {
        case INS_mov:
	{
	    return true;
        }

        default:
        {
            return false;
        }
    }
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
    assert(IsMovInstruction(ins));

    emitAttr  size     = EA_SIZE(attr);
    emitAttr  elemsize = EA_UNKNOWN;
    insFormat fmt      = IF_NONE;

    switch (ins)
    {
	case INS_mov:
	    assert(insOptsNone(opt));

	    if (IsRedundantMov(ins, size, dstReg, srcReg, canSkip))
            {
                // These instructions have no side effect and can be skipped
                return;
            }

	    _ASSERTE(!"NYI POWERPC64");
	    break;

	default:
	    _ASSERTE(!"NYI POWERPC64");
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
    /* Sanity check the arguments depending on callType */

    assert(callType < EC_COUNT);
    assert((callType != EC_FUNC_TOKEN) || (addr != nullptr && ireg == REG_NA));
    assert(callType != EC_INDIR_R || (addr == nullptr && ireg < REG_COUNT));

    // PPC64LE never uses xreg, xmul, disp
    assert(xreg == REG_NA && xmul == 0 && disp == 0);

    // Our stack level should be always greater than the bytes of arguments we push. Just
    // a sanity test.
    assert((unsigned)std::abs(argSize) <= codeGen->genStackLevel);

    // Trim out any callee-trashed registers from the live set.
    regMaskTP savedSet = emitGetGCRegsSavedOrModified(methHnd);
    gcrefRegs &= savedSet;
    byrefRegs &= savedSet;

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        printf("Call: GCvars=%s ", VarSetOps::ToString(emitComp, ptrVars));
        dumpConvertedVarSet(emitComp, ptrVars);
        printf(", gcrefRegs=");
        printRegMaskInt(gcrefRegs);
        emitDispRegSet(gcrefRegs);
        printf(", byrefRegs=");
        printRegMaskInt(byrefRegs);
        emitDispRegSet(byrefRegs);
        printf("\n");
    }
#endif

    /* Managed RetVal: emit sequence point for the call */
    if (emitComp->opts.compDbgInfo && di.GetLocation().IsValid())
    {
        codeGen->genIPmappingAdd(IPmappingDscKind::Normal, di, false);
    }

    /*
        We need to allocate the appropriate instruction descriptor based
        on whether this is a direct/indirect call, and whether we need to
        record an updated set of live GC variables.
     */
    instrDesc* id;

    assert(argSize % REGSIZE_BYTES == 0);
    int argCnt = (int)(argSize / (int)REGSIZE_BYTES);

    if (callType == EC_INDIR_R)
    {
        /* Indirect call, virtual calls */

        id = emitNewInstrCallInd(argCnt, 0 /* disp */, ptrVars, gcrefRegs, byrefRegs, retSize, secondRetSize);
    }
    else
    {
        /* Helper/static/nonvirtual/function calls (direct or through handle),
           and calls to an absolute addr. */

        assert(callType == EC_FUNC_TOKEN);

        id = emitNewInstrCallDir(argCnt, ptrVars, gcrefRegs, byrefRegs, retSize, secondRetSize);
    }

    /* Update the emitter's live GC ref sets */

    // If the method returns a GC ref, mark RBM_INTRET appropriately
    if (retSize == EA_GCREF)
    {
        gcrefRegs |= RBM_INTRET;
    }
    else if (retSize == EA_BYREF)
    {
        byrefRegs |= RBM_INTRET;
    }

    // If is a multi-register return method is called, mark RBM_INTRET_1 appropriately
    if (secondRetSize == EA_GCREF)
    {
        gcrefRegs |= RBM_INTRET_1;
    }
    else if (secondRetSize == EA_BYREF)
    {
        byrefRegs |= RBM_INTRET_1;
    }

    VarSetOps::Assign(emitComp, emitThisGCrefVars, ptrVars);
    emitThisGCrefRegs = gcrefRegs;
    emitThisByrefRegs = byrefRegs;

    // for the purpose of GC safepointing tail-calls are not real calls
    id->idSetIsNoGC(isJump || noSafePoint || emitNoGChelper(methHnd));

    /* Set the instruction - special case jumping a function */
    instruction ins;

    /* Record the address: method, indirection, or funcptr */

    if (callType == EC_INDIR_R)
    {
        /* This is an indirect call (either a virtual call or func ptr call) */

        // PPC64LE uses:
        // - bctr for tail calls (branch to count register)
        // - bctrl for regular calls (branch to count register and link)
        // The target address is loaded into the count register (CTR) before the call
        if (isJump)
        {
            ins = INS_bctr; // Branch to count register (tail call)
        }
        else
        {
            ins = INS_bctrl; // Branch to count register and link (regular call)
        }

        id->idIns(ins);
        id->idSetIsCallRegPtr();

        assert(xreg == REG_NA);
    }
    else
    {
        /* This is a simple direct call: "call helper/method/addr" */

        assert(callType == EC_FUNC_TOKEN);

        assert(addr != NULL);

        // PPC64LE uses:
        // - b for tail calls (branch)
        // - bl for regular calls (branch and link)
        if (isJump)
        {
            ins = INS_b; // Branch for tail call
        }
        else
        {
            ins = INS_bl; // Branch and link for regular call
        }

        id->idIns(ins);

        id->idAddr()->iiaAddr = (BYTE*)addr;

        if (emitComp->opts.compReloc)
        {
            id->idSetIsDspReloc();
        }
    }

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        if (id->idIsLargeCall())
        {
            printf("[%02u] Rec call GC vars = %s\n", id->idDebugOnlyInfo()->idNum,
                   VarSetOps::ToString(emitComp, ((instrDescCGCA*)id)->idcGCvars));
        }
    }
#endif

    if (m_debugInfoSize > 0)
    {
        INDEBUG(id->idDebugOnlyInfo()->idCallSig = sigInfo);
        id->idDebugOnlyInfo()->idMemCookie = (size_t)methHnd; // method token
    }

#ifdef LATE_DISASM
    if (addr != nullptr)
    {
        codeGen->getDisAssembler().disSetMethod((size_t)addr, methHnd);
    }
#endif // LATE_DISASM

    appendToCurIG(id);
}

// emitIns_valid_imm_for_li: Check if immediate fits in li instruction
bool emitter::emitIns_valid_imm_for_li(ssize_t imm)
{
    // li can encode 16-bit signed immediate
    return (imm >= -32768 && imm <= 32767);
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
    //_ASSERTE(!"NYI POWERPC64");
    
    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idReg1(reg);
    appendToCurIG(id);
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
    if (IsMovInstruction(ins))
    {
        assert(!"Please use emitIns_Mov() to correctly handle move elision");
        emitIns_Mov(ins, attr, reg1, reg2, /* canSkip */ false, opt);
    }

    emitAttr  size     = EA_SIZE(attr);
    emitAttr  elemsize = EA_UNKNOWN;
    insFormat fmt      = IF_NONE;

    switch (ins)
    {
        case INS_lwa:
	    break;
	
	default:
	    abort();
    }

    instrDesc* id = emitNewInstr(size);
    id->idIns(ins);
    appendToCurIG(id);

}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and a constant.
 */

void emitter::emitIns_R_R_I(instruction ins,
                            emitAttr    attr,
                            regNumber   reg1,
                            regNumber   reg2,
                            ssize_t     imm,
                            insOpts     opt /* = INS_OPTS_NONE */)
{
    // Validate registers
    assert(isGeneralRegister(reg1));
    assert(isGeneralRegister(reg2));

    // Validate immediate range based on instruction type
    // D-form instructions use 16-bit signed immediate
    // DS-form instructions use 14-bit aligned immediate
    if (ins == INS_ld || ins == INS_lwa)
    {
        // DS-form: must be 4-byte aligned
        assert((imm & 0x3) == 0);
        assert(imm >= -32768 && imm <= 32764);
    }
    else
    {
        // D-form: 16-bit signed
        assert(imm >= -32768 && imm <= 32767);
    }

    // Create instruction descriptor with immediate value
    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);

    appendToCurIG(id);
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
    instrDesc* id = emitNewInstr(EA_8BYTE);

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

void emitter::emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir)
{
    GenTree* addr = indir->Addr();

    if (addr->isContained())
    {
	abort();
    }
    else
    {
#ifdef DEBUG
        if (addr->OperIs(GT_LCL_ADDR))
        {
            // If the local var is a gcref or byref, the local var better be untracked, because we have
            // no logic here to track local variable lifetime changes, like we do in the contained case
            // above. E.g., for a `str r0,[r1]` for byref `r1` to local `V01`, we won't store the local
            // `V01` and so the emitter can't update the GC lifetime for `V01` if this is a variable birth.
            LclVarDsc* varDsc = emitComp->lvaGetDesc(addr->AsLclVarCommon());
            assert(!varDsc->lvTracked);
        }
#endif // DEBUG

	// Then load/store dataReg from/to [addrReg]
        emitIns_R_R(ins, attr, dataReg, addr->GetRegNum());
    }
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

void emitter::emitIns_J(instruction ins, BasicBlock* dst)
{
    insFormat fmt = IF_NONE;

    if (dst != nullptr)
    {
        assert(dst->HasFlag(BBF_HAS_LABEL));
    }

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_b:
            // Unconditional branch - I-form (24-bit signed offset)
            fmt = IF_NONE; // Will be updated when instruction formats are fully defined
            break;

        case INS_beq:
        case INS_bne:
        case INS_blt:
        case INS_bge:
        case INS_bgt:
        case INS_ble:
            // Conditional branches - B-form (14-bit signed offset)
            // For now, assume these may need to be long jumps
            fmt = IF_NONE; // Will be updated when instruction formats are fully defined
            break;

        default:
            unreached();
            break;
    }

    instrDescJmp* id = emitNewInstrJmp();

    id->idIns(ins);
    // TODO TARGET_POWERPC64 - Not using Instruction Formats yet
    //id->idInsFmt(fmt);
    id->idjShort = false;

    if (dst != nullptr)
    {
        id->idAddr()->iiaBBlabel = dst;

        // The target needs to be relocated if in different regions
        id->idjKeepLong = emitComp->fgInDifferentRegions(emitComp->compCurBB, dst);

#ifdef DEBUG
        if (emitComp->opts.compLongAddress) // Force long branches
        {
            id->idjKeepLong = true;
        }
#endif // DEBUG
    }
    else
    {
        // This should not happen for PPC64LE - all jumps should have a destination
        unreached();
    }

    /* Record the jump's IG and offset within it */
    id->idjIG   = emitCurIG;
    id->idjOffs = emitCurIGsize;

    /* Append this jump to this IG's jump list */
    id->idjNext      = emitCurIGjmpList;
    emitCurIGjmpList = id;

#if EMITTER_STATS
    emitTotalIGjmps++;
#endif

    //dispIns(id);
    appendToCurIG(id);
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
