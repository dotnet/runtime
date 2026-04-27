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
    if (emitIsSmallInsDsc(id))
        return SMALL_IDSC_SIZE;

    // For PPC64LE, we don't use instruction formats yet, so we check the descriptor type directly
    // Check if this is a jump instruction
    if (id->idIns() == INS_b || id->idIns() == INS_beq || id->idIns() == INS_bne ||
        id->idIns() == INS_blt || id->idIns() == INS_bge || id->idIns() == INS_bgt || id->idIns() == INS_ble)
    {
        return sizeof(instrDescJmp);
    }

    // Check if this is a call instruction
    bool isCallIns = (id->idIns() == INS_bl) || (id->idIns() == INS_blr) ||
                     (id->idIns() == INS_bctrl);
    
    if (isCallIns)
    {
        if (id->idIsLargeCall())
        {
            /* Must be a "fat" call descriptor */
            return sizeof(instrDescCGCA);
        }
        else
        {
            assert(!id->idIsLargeDsp());
            assert(!id->idIsLargeCns());
            return sizeof(instrDesc);
        }
    }

    // Handle other descriptor types based on flags
    if (id->idIsLargeCns())
    {
        if (id->idIsLargeDsp())
        {
            return sizeof(instrDescCnsDsp);
        }
        else
        {
            return sizeof(instrDescCns);
        }
    }
    else
    {
        if (id->idIsLargeDsp())
        {
            return sizeof(instrDescDsp);
        }
        else
        {
            return sizeof(instrDesc);
        }
    }
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
    assert(offs >= 0);

    emitAttr size = EA_SIZE(attr);

    /* Figure out the variable's frame position */
    bool    FPbased;
    int     base = emitComp->lvaFrameAddress(varx, &FPbased);
    int     disp = base + offs;
    ssize_t imm  = disp;

    // Use frame pointer or stack pointer as base register
    regNumber reg2 = FPbased ? REG_FPBASE : REG_SPBASE;

    /* Validate the instruction form and operand sizes */
    switch (ins)
    {
        case INS_lbz:
            // Load byte and zero-extend - D-form instruction
            assert(isGeneralRegister(reg1));
            assert(size == EA_1BYTE);
            break;

        case INS_lhz:
            // Load halfword and zero-extend - D-form instruction
            assert(isGeneralRegister(reg1));
            assert(size == EA_2BYTE);
            break;

        case INS_lha:
            // Load halfword and sign-extend - D-form instruction
            assert(isGeneralRegister(reg1));
            assert(size == EA_2BYTE);
            break;

        case INS_lwz:
            // Load word and zero-extend - D-form instruction
            assert(isGeneralRegister(reg1));
            assert(size == EA_4BYTE);
            break;

        case INS_lwa:
            // Load word and sign-extend - DS-form instruction (must be 4-byte aligned)
            assert(isGeneralRegister(reg1));
            assert(size == EA_4BYTE);
            assert((imm & 0x3) == 0);
            break;

        case INS_ld:
            // Load doubleword - DS-form instruction (must be 4-byte aligned)
            assert(isGeneralRegister(reg1));
            assert(size == EA_8BYTE);
            assert((imm & 0x3) == 0);
            break;

        default:
            NYI("emitIns_R_S");
            return;
    }

    // Validate immediate range for the selected instruction form.
    if ((ins == INS_lwa) || (ins == INS_ld))
    {
        // DS-form: displacement must be 4-byte aligned and fit the form-specific range.
        assert(imm >= -32768 && imm <= 32764);
        assert((imm & 0x3) == 0);
    }
    else
    {
        // D-form: 16-bit signed immediate.
        assert(imm >= -32768 && imm <= 32767);
    }

    // Create instruction descriptor with immediate offset.
    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
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
    // For PowerPC, a move is redundant if source and destination are the same
    // PowerPC uses 'mr' (move register) instruction which is actually 'or rA, rS, rS'
    return (dst == src);
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

            // PowerPC uses 'mr' (move register) which is actually 'or rA, rS, rS'
            // Just call emitIns_R_R to emit the move
            emitIns_R_R(ins, attr, dstReg, srcReg, opt);
     break;
     
 default:
     printf("DEBUG: emitIns_Mov - unhandled instruction %d\n", ins);
     assert(!"Unhandled move instruction");
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
    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idReg1(reg);
    
    // Set instruction format based on instruction type
    insFormat fmt = IF_NONE;
    switch (ins)
    {
        case INS_li:
            fmt = IF_RI_1A;  // li rD, imm16
            break;
        case INS_lis:
            fmt = IF_RI_1B;  // lis rD, imm16
            break;
        case INS_cmpwi:
            fmt = IF_RRI_1A; // cmpwi crD, rA, imm16
            break;
        default:
            fmt = IF_RI_1C;  // Generic register-immediate (addi, etc.)
            break;
    }
    id->idInsFmt(fmt);
    
    appendToCurIG(id);
}



 /*
 *  Add an instruction referencing two registers
 */

void emitter::emitIns_R_R(instruction     ins,
			  emitAttr        attr,
			  regNumber       reg1,
			  regNumber       reg2,
			  insOpts         opt /* = INS_OPTS_NONE */,
			  insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    // Note: For PPC64LE, we allow move instructions to be handled here
    // emitIns_Mov() will call this function after checking for redundant moves
    
    emitAttr  size     = EA_SIZE(attr);
    emitAttr  elemsize = EA_UNKNOWN;
    insFormat fmt      = IF_NONE;

    switch (ins)
    {
        case INS_mov:
            // Move register - mr rA, rS (actually or rA, rS, rS)
            fmt = IF_RR_1A;  // Will set proper format later
            break;
            
	case INS_cmpd:
    	case INS_cmpw:
            // Comparison instructions - these are X-form instructions
            // cmpd rA, rB compares two registers
            fmt = IF_RR_2B;  // Will set proper format later
            break;
            
	default:
	    fmt = IF_RR_2A;
	    break;
    }

    // Create instruction descriptor AFTER the switch (like ARM64 does)
    instrDesc* id = emitNewInstr(attr);
    
    id->idIns(ins);
    
    id->idReg1(reg1);
    id->idReg2(reg2);
    // TODO TARGET_POWERPC64 - Not using Instruction Formats yet
    id->idInsFmt(fmt);
    
    dispIns(id);
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
	
    // Debug: Print when we see negative offsets with r31
    if (reg2 == REG_R31 && imm < 0 && (ins == INS_stw || ins == INS_lwz))
    {
        printf("DEBUG: %s with negative offset %d from r31\n", 
               ins == INS_stw ? "stw" : "lwz", (int)imm);
    }
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
    // Set instruction format based on instruction type
    insFormat fmt = IF_NONE;
    switch (ins)
    {
        case INS_lwz:
            fmt = IF_LS_2A;  // lwz rD, disp(rA)
            break;
        case INS_stw:
            fmt = IF_LS_2B;  // stw rS, disp(rA)
            break;
        case INS_ld:
            fmt = IF_LS_2C;  // ld rD, disp(rA)
            break;
        case INS_std:
            fmt = IF_LS_2D;  // std rS, disp(rA)
            break;
        case INS_lwa:
            fmt = IF_LS_2E;  // lwa rD, disp(rA)
            break;
        case INS_stdu:
            fmt = IF_LS_2F;  // stdu rS, disp(rA)
            break;
        case INS_addi:
            fmt = IF_RI_1C;  // addi rD, rA, imm16
            break;
        case INS_ori:
        case INS_oris:
            fmt = IF_RI_1D;  // ori rD, rA, imm16
            break;
        default:
            fmt = IF_RI_1C;  // Generic register-register-immediate
            break;
    }
    id->idInsFmt(fmt);
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
    
    // Set instruction format
    insFormat fmt = IF_NONE;
    switch (ins)
    {
        case INS_nop:
            fmt = IF_SR_1D;  // nop
            break;
        case INS_blr:
            fmt = IF_SR_1C;  // blr
            break;
        default:
            fmt = IF_NONE;
            break;
    }
    id->idInsFmt(fmt);

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
    // For now, just create the instruction descriptor
    // We'll implement actual encoding later
    instrDesc* id = emitNewInstr(attr);
    id->idIns(ins);
    id->idReg1(reg);
    
    // Set instruction format based on instruction type
    insFormat fmt = IF_NONE;
    switch (ins)
    {
        case INS_mflr:
            fmt = IF_SR_1A;  // mflr rD
            break;
        case INS_mtlr:
            fmt = IF_SR_1B;  // mtlr rS
            break;
        case INS_blr:
            fmt = IF_SR_1C;  // blr
            break;
        default:
            fmt = IF_NONE;
            break;
    }
    id->idInsFmt(fmt);
    
    appendToCurIG(id);
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
	switch (ins)
    {
        // Load instructions
        case INS_lbz:   // Load Byte and Zero
        case INS_lhz:   // Load Halfword and Zero
        case INS_lha:   // Load Halfword Algebraic
        case INS_lwz:   // Load Word and Zero
        case INS_lwa:   // Load Word Algebraic
        case INS_ld:    // Load Doubleword
        case INS_lfd:   // Load Floating-Point Double

        // Store instructions
        case INS_stb:   // Store Byte
        case INS_sth:   // Store Halfword
        case INS_stw:   // Store Word
        case INS_std:   // Store Doubleword
        case INS_stdu:  // Store Doubleword with Update
        case INS_stfd:  // Store Floating-Point Double
            return true;

        default:
            return false;
    }
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
	// WORKAROUND: Contained address case not yet implemented
        // This typically occurs with complex address calculations or global variables
        // TODO: Implement proper contained address handling
        printf("WARNING: Skipping contained address load/store\n");
        return;
    }
    else
    {
#ifdef DEBUG
        if (addr->OperIs(GT_LCL_ADDR))
        {
            LclVarDsc* varDsc = emitComp->lvaGetDesc(addr->AsLclVarCommon());
            assert(!varDsc->lvTracked);
        }
#endif // DEBUG

        // Then load/store dataReg from/to [addrReg] with offset 0
        // IMPORTANT: In PowerPC, R0 as base register means "address 0", not "contents of R0"
        // This is a PowerPC ISA quirk that must be handled carefully
	regNumber baseReg = addr->GetRegNum();
        if (baseReg == REG_R0)
        {
	    // WORKAROUND: Skip loads/stores with R0 as base register
            // TODO: Move address to another register before load/store
            printf("WARNING: Skipping load/store with R0 base (likely invalid address)\n");
            return;
        }

        emitIns_R_R_I(ins, attr, dataReg, baseReg, 0);
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
    
    instruction ins = id->idIns();
    switch (ins)
    {
       case INS_mov:
           // mr rA, rS - Move Register
           ppc_mr(dstRW, id->idReg1(), id->idReg2());
           break;

       case INS_movi:
           abort();
           break;

       case INS_nop:
           ppc_nop (dstRW);
           break;

       case INS_trap:
           ppc_trap (dstRW);
           break;

       case INS_blr:
           ppc_blr (dstRW);
           break;

       case INS_mflr:
           // mflr rD - Move from Link Register
           ppc_mflr (dstRW, id->idReg1());
           break;

       case INS_mtlr:
           // mtlr rS - Move to Link Register
           ppc_mtlr (dstRW, id->idReg1());
           break;

       case INS_bctr:
           // bctr - Branch to Count Register (unconditional)
           ppc_bcctr (dstRW, 20, 0);
           break;

       case INS_bctrl:
           // bctrl - Branch to Count Register and Link
           ppc_bcctrl (dstRW, 20, 0);
           break;

       case INS_bl:

	   //WORKAROUND: Helper call infrastructure not yet implemented
           // bl instruction requires proper offset calculation to helper functions
           // For now, replace with nop to avoid infinite loops
           // TODO: Implement proper helper call mechanism
           ppc_nop(dstRW);
    	   break;
       case INS_addi:
           // addi rD, rA, SIMM
           ppc_addi (dstRW, id->idReg1(), id->idReg2(), emitGetInsSC(id));
           break;

       case INS_li:
           // li rD, value (pseudo-op: addi rD, 0, value)
           ppc_li (dstRW, id->idReg1(), emitGetInsSC(id));
           break;

       case INS_lis:
           // lis rD, value (pseudo-op: addis rD, 0, value)
           ppc_lis (dstRW, id->idReg1(), emitGetInsSC(id));
           break;

       case INS_ori:
           // ori rA, rS, UIMM
           ppc_ori (dstRW, id->idReg2(), id->idReg1(), emitGetInsSC(id));
           break;

       case INS_oris:
           // oris rA, rS, UIMM
           ppc_oris (dstRW, id->idReg2(), id->idReg1(), emitGetInsSC(id));
           break;

       case INS_sldi:
           // sldi rA, rS, n (pseudo-op: rldicr)
           ppc_sldi (dstRW, id->idReg1(), id->idReg2(), emitGetInsSC(id));
           break;

       case INS_cmpw:
           // cmpw crD, rA, rB (cmp with L=0)
           // Assuming crD is in idReg1, rA in idReg2, rB in idReg3
           ppc_cmpw (dstRW, 0, id->idReg1(), id->idReg2());
           break;

       case INS_cmpd:
           // cmpd crD, rA, rB (cmp with L=1)
           ppc_cmpd (dstRW, 0, id->idReg1(), id->idReg2());
           break;

       case INS_cmpwi:
           // cmpwi crD, rA, SIMM (cmpi with L=0)
           ppc_cmpwi (dstRW, 0, id->idReg1(), emitGetInsSC(id));
           break;

       case INS_cmpdi:
           // cmpdi crD, rA, SIMM (cmpi with L=1)
           ppc_cmpdi (dstRW, 0, id->idReg1(), emitGetInsSC(id));
           break;

       case INS_lbz:
           // lbz rD, d(rA) - Load Byte and Zero
           ppc_lbz (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_lhz:
           // lhz rD, d(rA) - Load Halfword and Zero
           ppc_lhz (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_lha:
           // lha rD, d(rA) - Load Halfword Algebraic
           ppc_lha (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_lwz:
           // lwz rD, d(rA) - Load Word and Zero
           ppc_lwz (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_lwa:
           // lwa rD, ds(rA) - Load Word Algebraic
           ppc_lwa (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_ld:
           // ld rD, ds(rA) - Load Doubleword
           ppc_ld (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_lfd:
           // lfd fD, d(rA) - Load Floating-Point Double
           ppc_lfd (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_stfd:
           // stfd fS, d(rA) - Store Floating-Point Double
           ppc_stfd (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_stb:
           // stb rS, d(rA) - Store Byte
           ppc_stb (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_sth:
           // sth rS, d(rA) - Store Halfword
           ppc_sth (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_stw:
           // stw rS, d(rA) - Store Word
           ppc_stw (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_std:
           // std rS, ds(rA) - Store Doubleword
           ppc_std (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;

       case INS_stdu:
           // stdu rS, ds(rA) - Store Doubleword with Update
           ppc_stdu (dstRW, id->idReg1(), emitGetInsSC(id), id->idReg2());
           break;
       case INS_b:
           // b target - Unconditional Branch
           {
               instrDescJmp* jmp = (instrDescJmp*)id;
               int offset = (int)jmp->idjOffs / 4; // Convert bytes to instructions
               ppc_b (dstRW, offset);
           }
           break;

       case INS_beq:
           // beq target - Branch if Equal
           {
               instrDescJmp* jmp = (instrDescJmp*)id;
               int offset = (int)jmp->idjOffs / 4; // Convert bytes to instructions
               ppc_bc (dstRW, PPC_BR_TRUE, PPC_BR_EQ, offset);
           }
           break;

       case INS_bne:
           // bne target - Branch if Not Equal
           {
               instrDescJmp* jmp = (instrDescJmp*)id;
               int offset = (int)jmp->idjOffs / 4;
               ppc_bc (dstRW, PPC_BR_FALSE, PPC_BR_EQ, offset);
           }
           break;

       case INS_blt:
           // blt target - Branch if Less Than
           {
               instrDescJmp* jmp = (instrDescJmp*)id;
               int offset = (int)jmp->idjOffs / 4;
               ppc_bc (dstRW, PPC_BR_TRUE, PPC_BR_LT, offset);
           }
           break;

       case INS_bge:
           // bge target - Branch if Greater Than or Equal
           {
               instrDescJmp* jmp = (instrDescJmp*)id;
               int offset = (int)jmp->idjOffs / 4;
               ppc_bc (dstRW, PPC_BR_FALSE, PPC_BR_LT, offset);
           }
           break;

       case INS_bgt:
           // bgt target - Branch if Greater Than
           {
               instrDescJmp* jmp = (instrDescJmp*)id;
               int offset = (int)jmp->idjOffs / 4;
               ppc_bc (dstRW, PPC_BR_TRUE, PPC_BR_GT, offset);
           }
           break;

       case INS_ble:
           // ble target - Branch if Less Than or Equal
           {
               instrDescJmp* jmp = (instrDescJmp*)id;
               int offset = (int)jmp->idjOffs / 4;
               ppc_bc (dstRW, PPC_BR_FALSE, PPC_BR_GT, offset);
           }
           break;
    
       default:
           _ASSERTE(!"NYI");
    }

    dst = dstRW - writeableOffset;
    *dp = dst;
    return emitSizeOfInsDsc(id);
}
#ifdef DEBUG
//------------------------------------------------------------------------
// emitDisInsName: Display the instruction name for the given instruction.
//
const char* emitter::emitDisInsName(code_t code, const BYTE* addr, instrDesc* id)
{
    instruction ins = id->idIns();
    
    switch (ins)
    {
        case INS_li:      return "li      ";
        case INS_lis:     return "lis     ";
        case INS_ori:     return "ori     ";
        case INS_addi:    return "addi    ";
        case INS_mov:     return "mr      ";
        case INS_stw:     return "stw     ";
        case INS_lwz:     return "lwz     ";
        case INS_lwa:     return "lwa     ";
        case INS_std:     return "std     ";
        case INS_ld:      return "ld      ";
        case INS_stdu:    return "stdu    ";
        case INS_cmpw:    return "cmpw    ";
        case INS_cmpwi:   return "cmpwi   ";
        case INS_beq:     return "beq     ";
        case INS_bne:     return "bne     ";
        case INS_b:       return "b       ";
        case INS_bl:      return "bl      ";
        case INS_blr:     return "blr     ";
        case INS_mflr:    return "mflr    ";
        case INS_mtlr:    return "mtlr    ";
        case INS_nop:     return "nop     ";
        case INS_sldi:    return "sldi    ";
        case INS_oris:    return "oris    ";
        
        default:
            return "???     ";
    }
}
void emitter::emitIns_J(instruction ins, BasicBlock* dst, int instrCount)
{
    insFormat fmt = IF_NONE;

    if (dst != nullptr)
    {
        assert(dst->HasFlag(BBF_HAS_LABEL));
    }
    else
    {
        // When dst is NULL, instrCount must be provided for backward branches
        assert(instrCount != 0);
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
        // Handle backward branch with instruction count (used in prolog for stack probing loop)
        // instrCount is negative, indicating how many instructions to branch back
        id->idAddr()->iiaSetInstrCount(instrCount);
        id->idjKeepLong = false;
        /* This jump must be short */
        emitSetShortJump(id);
        id->idSetIsBound();
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
    if (!emitComp->verbose)
        return;

    code_t code = 0;
    if (pCode != nullptr && sz == 4)
    {
        code = *((code_t*)pCode);
    }

    if (doffs)
    {
        printf(" ?????????");
    }
    else
    {
        printf("            ");
    }

    if (code != 0)
    {
        printf("  %08X", code);
    }
    else
    {
        printf("          ");
    }

    const char* insName = emitDisInsName(code, pCode, id);
    printf("  %s", insName);

    instruction ins = id->idIns();
    
    switch (ins)
    {
        case INS_li:
        case INS_lis:
            printf("r%d, %d", id->idReg1(), (int)emitGetInsSC(id));
            break;
            
        case INS_ori:
        case INS_addi:
        case INS_oris:
        case INS_sldi:
            printf("r%d, r%d, %d", id->idReg1(), id->idReg2(), (int)emitGetInsSC(id));
            break;
            
        case INS_mov:
        case INS_cmpw:
            printf("r%d, r%d", id->idReg1(), id->idReg2());
            break;
            
        case INS_stw:
        case INS_lwz:
        case INS_lwa:
        case INS_std:
        case INS_ld:
        case INS_stdu:
            printf("r%d, %d(r%d)", id->idReg1(), (int)emitGetInsSC(id), id->idReg2());
            break;
            
        case INS_cmpwi:
            printf("cr0, r%d, %d", id->idReg1(), (int)emitGetInsSC(id));
            break;
            
        case INS_beq:
        case INS_bne:
        case INS_b:
            if (id->idIsBound())
            {
                instrDescJmp* jmp = (instrDescJmp*)id;
                printf("%+d", (int)jmp->idjOffs);
            }
            else
            {
                printf("<unbound>");
            }
            break;
            
        case INS_mflr:
        case INS_mtlr:
            printf("r%d", id->idReg1());
            break;
            
        case INS_blr:
        case INS_nop:
            break;
            
        default:
            printf("???");
            break;
    }
    
    printf("\n");
}

//------------------------------------------------------------------------
// emitInsSize: Returns the size in bytes of the given instruction descriptor
//
size_t emitter::emitInsSize(instrDesc* id)
{
    return 4; // All PowerPC instructions are 4 bytes
}

/*****************************************************************************
 *
 *  Bind jump distances for PowerPC64LE
 *  PowerPC has fixed-size instructions (4 bytes each), which simplifies this
 */

void emitter::emitJumpDistBind()
{
#ifdef DEBUG
    if (emitComp->verbose)
    {
        printf("*************** In emitJumpDistBind() for PowerPC64LE\n");
    }
#endif

    instrDescJmp* jmp;
    
    // Walk through all jump instructions
    for (jmp = emitJumpList; jmp; jmp = jmp->idjNext)
    {
        insGroup* jmpIG = jmp->idjIG;
        insGroup* tgtIG;
        
        // Get the target instruction group
        if (jmp->idIsBound())
        {
            // Backward branch - target is already bound
            tgtIG = jmpIG;
        }
        else
        {
            // Forward branch - get target from basic block
            BasicBlock* tgtBlock = jmp->idAddr()->iiaBBlabel;
            tgtIG = (insGroup*)emitCodeGetCookie(tgtBlock);
        }
        
        // Calculate source and destination offsets
        UNATIVE_OFFSET srcOffs = jmpIG->igOffs + jmp->idjOffs;
        UNATIVE_OFFSET dstOffs;
        
        if (jmp->idIsBound())
        {
            // Backward branch using instruction count
            int instrCount = jmp->idAddr()->iiaGetInstrCount();
            dstOffs = srcOffs + (instrCount * 4); // Each instruction is 4 bytes
        }
        else
        {
            dstOffs = tgtIG->igOffs;
        }
        
        // Calculate relative offset (in bytes)
        NATIVE_OFFSET jmpDist = (NATIVE_OFFSET)(dstOffs - srcOffs);
        
        // PowerPC branches encode offset in instructions (divide by 4)
        // B-form conditional branches: 14-bit signed field (±32KB range)
        // I-form unconditional branch: 24-bit signed field (±32MB range)
        
        instruction ins = jmp->idIns();
        
        // Check if offset is in range
        if (ins == INS_b || ins == INS_bl)
        {
            // I-form: 24-bit signed, word-aligned
            assert((jmpDist >= -0x2000000) && (jmpDist < 0x2000000));
            assert((jmpDist & 3) == 0); // Must be word-aligned
        }
        else
        {
            // B-form conditional: 14-bit signed, word-aligned  
            assert((jmpDist >= -0x8000) && (jmpDist < 0x8000));
            assert((jmpDist & 3) == 0); // Must be word-aligned
        }
        
        // Store the distance in the jump descriptor
        jmp->idjOffs = (unsigned short)jmpDist;
        
#ifdef DEBUG
        if (emitComp->verbose)
        {
            printf("Jump at offset 0x%04X to offset 0x%04X, distance = %d bytes\n",
                   srcOffs, dstOffs, jmpDist);
        }
#endif
    }
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
#endif
#endif //TARGET_POWERPC64
