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
    // First check if the small descriptor flag is set
    if (emitIsSmallInsDsc(id))
        return SMALL_IDSC_SIZE;

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
        if (id->idIsLclVarPair())
        {
            return sizeof(instrDescLclVarPairCns);
        }
        else if (id->idIsLargeDsp())
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
        if (id->idIsLclVarPair())
        {
            return sizeof(instrDescLclVarPair);
        }
        else if (id->idIsLargeDsp())
        {
            return sizeof(instrDescDsp);
        }
        else
        {
            // For regular descriptors without large constants or displacements,
            // return the standard instrDesc size
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

        case INS_lfs:
            // Load floating-point single - D-form instruction
            assert(isFloatReg(reg1));
            assert(size == EA_4BYTE);
            break;

        case INS_lfd:
            // Load floating-point double - D-form instruction
            assert(isFloatReg(reg1));
            assert(size == EA_8BYTE);
            break;
	case INS_lea:
	           // Load Effective Address - pseudo-instruction
	           // On PPC64LE, this is implemented as addi (add immediate)
	           // Used for computing addresses: addi targetReg, baseReg, offset
	           assert(isGeneralRegister(reg1));
	           // Size can be EA_4BYTE, EA_8BYTE, or EA_BYREF (EA_PTRSIZE)
	           assert((size == EA_4BYTE) || (size == EA_8BYTE) || (size == EA_BYREF));
	           // Convert INS_lea to INS_addi for actual emission
	           ins = INS_addi;
	           break;

	case INS_addi:
	           // Add immediate - D-form instruction
	           // Used for computing addresses: addi targetReg, baseReg, offset
	           assert(isGeneralRegister(reg1));
	           // Size can be EA_4BYTE or EA_8BYTE (EA_PTRSIZE)
	           assert((size == EA_4BYTE) || (size == EA_8BYTE));
	           break;

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
	           assert((imm & 0x3) == 0);
	           break;

	       case INS_stfs:
	           // Store floating-point single - D-form instruction
	           assert(isFloatReg(reg1));
	           assert(size == EA_4BYTE);
	           break;

	       case INS_stfd:
	           // Store floating-point double - D-form instruction
	           assert(isFloatReg(reg1));
	           assert(size == EA_8BYTE);
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

    // Create instruction descriptor for local variable access.
    // We need to store both the immediate offset (imm) and local variable info (varx, offs).
    // Use emitNewInstrLclVarPair which has space for both constant and address union.
    instrDesc* id = emitNewInstrLclVarPair(attr, imm);

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

        case INS_stfs:
            // Store floating-point single - D-form instruction
            assert(isFloatReg(reg1));
            assert(size == EA_4BYTE);
            break;

        case INS_stfd:
            // Store floating-point double - D-form instruction
            assert(isFloatReg(reg1));
            assert(size == EA_8BYTE);
            break;

        default:
            NYI("emitIns_S_R");
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

    // Create instruction descriptor with local variable address
    // Use emitNewInstrLclVarPair which always creates a descriptor with address union
    // This is necessary because we call id->idAddr() below
    instrDesc* id = emitNewInstrLclVarPair(attr, imm);

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
    assert((ins == INS_mov) || (ins == INS_fmr));

    if (canSkip && (dst == src))
    {
        // These elisions used to be explicit even when optimizations were disabled
        return true;
    }
    // For PowerPC, a move is redundant if source and destination are the same
    // PowerPC uses 'mr' (move register) for integer and 'fmr' for floating-point
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
        case INS_fmr:
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

 case INS_fmr:
     assert(insOptsNone(opt));

     if (IsRedundantMov(ins, size, dstReg, srcReg, canSkip))
            {
                // These instructions have no side effect and can be skipped
                return;
            }

            // PowerPC uses 'fmr' (floating move register)
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

/*****************************************************************************
 *
 *  Calculate the branch offset for a bl instruction
 *  Returns the offset in instruction units (not bytes)
 *  Returns 0 if the offset is out of range
 */
int emitter::getBranchOffset(BYTE* dst, void* target)
{
    // Calculate the byte offset from current instruction to target
    ssize_t byteOffset = (ssize_t)target - (ssize_t)dst;
    
    // Convert to instruction offset (PowerPC instructions are 4 bytes)
    ssize_t instrOffset = byteOffset >> 2;
    
    // Check if offset fits in 24 bits (signed)
    // Range: -8388608 to 8388607 (0x800000 to 0x7FFFFF)
    if (instrOffset >= -0x800000 && instrOffset <= 0x7FFFFF)
    {
        return (int)instrOffset;
    }
    
    // Offset out of range
    return 0;
}

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
    // Use small descriptor for small constants, otherwise use emitNewInstrCns for large constants
    instrDesc* id;
    if (instrDesc::fitsInSmallCns(imm))
    {
        id = emitNewInstrSmall(attr);
        id->idSmallCns(imm);
    }
    else
    {
        id = emitNewInstrCns(attr, imm);
    }

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
        case INS_ori:
        case INS_oris:
	case INS_andi:
        case INS_sldi:
	case INS_srdi:
	    // For ori/oris/sldi/srdi with same source and destination: ori rD, rD, imm
            id->idReg2(reg);  // Set both source and destination to same register
            fmt = IF_RI_1D;   // ori/oris/andi/sldi/srdi rD, rA, imm16
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
            
        case INS_fmr:
            // Floating-point move register - fmr fD, fB
            assert(isFloatReg(reg1));
            assert(isFloatReg(reg2));
            fmt = IF_RR_1A;  // Will set proper format later
            break;
 
        case INS_frsp:
            // Floating-point round to single precision - frsp fD, fB
            assert(isFloatReg(reg1));
            assert(isFloatReg(reg2));
            fmt = IF_RR_1A;
            break;
            
        case INS_fcfid:
        case INS_fcfids:
        case INS_fcfidu:
        case INS_fcfidus:
            // Floating-point convert from integer doubleword
            // fcfid fD, fB - converts signed integer in FP reg to double
            // fcfids fD, fB - converts signed integer in FP reg to single
            // fcfidu fD, fB - converts unsigned integer in FP reg to double
            // fcfidus fD, fB - converts unsigned integer in FP reg to single
            assert(isFloatReg(reg1));
            assert(isFloatReg(reg2));
            fmt = IF_RR_1A;
            break;
            
        case INS_fctiwz:
        case INS_fctidz:
        case INS_fctiwuz:
        case INS_fctiduz:
            // Floating-point convert to integer word/doubleword with round toward zero
            // fctiwz fD, fB - converts double/single to signed 32-bit int
            // fctidz fD, fB - converts double/single to signed 64-bit int
            // fctiwuz fD, fB - converts double/single to unsigned 32-bit int
            // fctiduz fD, fB - converts double/single to unsigned 64-bit int
            assert(isFloatReg(reg1));
            assert(isFloatReg(reg2));
            fmt = IF_RR_1A;
            break;

        case INS_extsb:
	    // Extend Sign Byte - extsb rA, rS
            // Sign-extends an 8-bit value to 64-bit
	    assert(isGeneralRegister(reg1));
	    assert(isGeneralRegister(reg2));
	    fmt = IF_RR_1A;
	    break;

	case INS_extsh:
	    // Extend Sign Halfword - extsh rA, rS
	    // Sign-extends a 16-bit value to 64-bit
	    assert(isGeneralRegister(reg1));
	    assert(isGeneralRegister(reg2));
	    fmt = IF_RR_1A;
	    break;
		       
        case INS_extsw:
            // Extend Sign Word - extsw rA, rS
            // Sign-extends a 32-bit value to 64-bit
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            fmt = IF_RR_1A;
            break;
 
  	case INS_fcmpu:
        case INS_fcmpo:
            // Floating-point comparison - fcmpu/fcmpo crD, fA, fB
            assert(isFloatReg(reg1));
            assert(isFloatReg(reg2));
            fmt = IF_CMP_2A;  // Floating-point comparison format
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
    
    // Validate registers based on instruction type
    if (ins == INS_lfs || ins == INS_lfd || ins == INS_stfs || ins == INS_stfd)
    {
        assert(isFloatReg(reg1));
        assert(isGeneralRegister(reg2));
    }
    else
    {
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegister(reg2));
    }

    // Check if immediate fits in instruction encoding
    bool fitsInImmediate = false;
    
    if (ins == INS_ld || ins == INS_lwa || ins == INS_std)
    {
        // DS-form: 14-bit aligned (must be multiple of 4)
        fitsInImmediate = ((imm & 0x3) == 0) && (imm >= -32768) && (imm <= 32764);
    }
    else if (ins == INS_lwz || ins == INS_lbz || ins == INS_lhz || 
             ins == INS_stw || ins == INS_stb || ins == INS_sth ||
             ins == INS_lfs || ins == INS_lfd || ins == INS_stfs || ins == INS_stfd)
    {
        // D-form load/store: 16-bit signed
        fitsInImmediate = (imm >= -32768) && (imm <= 32767);
    }
    else
    {
        // Other instructions (addi, ori, etc.): 16-bit signed
        fitsInImmediate = (imm >= -32768) && (imm <= 32767);
    }

    // If immediate doesn't fit, use a temporary register
    // IMPORTANT: Only do this for load/store instructions!
    bool isLoadStore = (ins == INS_ld || ins == INS_lwa || ins == INS_std ||
                        ins == INS_lwz || ins == INS_lbz || ins == INS_lhz ||
                        ins == INS_stw || ins == INS_stb || ins == INS_sth ||
                        ins == INS_lfs || ins == INS_lfd || ins == INS_stfs || ins == INS_stfd);
    
    if (!fitsInImmediate && isLoadStore)
    {
        // Use R0 as temporary register for address calculation
        regNumber tempReg = REG_R0;
        
        // Manually emit: lis r0, imm_high
        instrDesc* id1 = emitNewInstrSmall(EA_PTRSIZE);
        id1->idIns(INS_lis);
        id1->idReg1(tempReg);
        id1->idSmallCns((imm >> 16) & 0xFFFF);
        id1->idInsFmt(IF_RI_1B);  // lis format
        dispIns(id1);
        appendToCurIG(id1);
        
        // Manually emit: ori r0, r0, imm_low (if needed)
        if ((imm & 0xFFFF) != 0)
        {
            instrDesc* id2 = emitNewInstrSmall(EA_PTRSIZE);
            id2->idIns(INS_ori);
            id2->idReg1(tempReg);
            id2->idReg2(tempReg);
            id2->idSmallCns(imm & 0xFFFF);
            id2->idInsFmt(IF_RI_1D);  // ori format
            dispIns(id2);
            appendToCurIG(id2);
        }
        
        // Manually emit: add r0, base_reg, r0
        instrDesc* id3 = emitNewInstr(EA_PTRSIZE);
        id3->idIns(INS_add);
        id3->idReg1(tempReg);
        id3->idReg2(reg2);
        id3->idReg3(tempReg);
        id3->idInsFmt(IF_RR_2A);  // add format
        dispIns(id3);
        appendToCurIG(id3);
        
        // Manually emit: load/store with 0 offset from temp register
        instrDesc* id4 = emitNewInstrSmall(attr);
        id4->idIns(ins);
        id4->idReg1(reg1);
        id4->idReg2(tempReg);
        id4->idSmallCns(0);
        
        // Set format based on instruction
        insFormat fmt4 = IF_NONE;
        switch (ins)
        {
            case INS_ld:   fmt4 = IF_LS_2C; break;
            case INS_lwa:  fmt4 = IF_LS_2E; break;
            case INS_std:  fmt4 = IF_LS_2D; break;
            case INS_lwz:  fmt4 = IF_LS_2A; break;
            case INS_stw:  fmt4 = IF_LS_2B; break;
            case INS_lfs:  fmt4 = IF_LS_2G; break;
            case INS_lfd:  fmt4 = IF_LS_2H; break;
            case INS_stfs: fmt4 = IF_LS_2I; break;
            case INS_stfd: fmt4 = IF_LS_2J; break;
            default:       fmt4 = IF_LS_2A; break;  // Default
        }
        id4->idInsFmt(fmt4);
        
        dispIns(id4);
        appendToCurIG(id4);
        return;
    }

    instrDesc* id;
    if (instrDesc::fitsInSmallCns(imm))
    {
        id = emitNewInstrSmall(attr);
        id->idSmallCns(imm);
    }
    else
    {
        id = emitNewInstrCns(attr, imm);
    }

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);
    
    // Set instruction format based on instruction type
    insFormat fmt = IF_NONE;
    switch (ins)
    {
        case INS_ld:   fmt = IF_LS_2C; break;  // ld  rD, disp(rA)
        case INS_lwa:  fmt = IF_LS_2E; break;  // lwa rD, disp(rA)
        case INS_std:  fmt = IF_LS_2D; break;  // std rS, disp(rA)
        case INS_lwz:  fmt = IF_LS_2A; break;  // lwz rD, disp(rA)
        case INS_stw:  fmt = IF_LS_2B; break;  // stw rS, disp(rA)
        case INS_lfs:  fmt = IF_LS_2G; break;  // lfs fD, disp(rA)
        case INS_lfd:  fmt = IF_LS_2H; break;  // lfd fD, disp(rA)
        case INS_stfs: fmt = IF_LS_2I; break;  // stfs fS, disp(rA)
        case INS_stfd: fmt = IF_LS_2J; break;  // stfd fS, disp(rA)
        case INS_addi: fmt = IF_RI_1C; break;  // addi rD, rA, imm16
        case INS_ori:  fmt = IF_RI_1D; break;  // ori rD, rA, imm16
        default:       fmt = IF_LS_2A; break;  // Default to lwz format
    }
    
    id->idInsFmt(fmt);
    
    dispIns(id);
    appendToCurIG(id);
}


/*****************************************************************************
 *
 *  Add an instruction referencing three registers (R1 = R2 op R3)
 *  Used for arithmetic and floating-point operations
 */

void emitter::emitIns_R_R_R(instruction ins,
                            emitAttr    attr,
                            regNumber   reg1,
                            regNumber   reg2,
                            regNumber   reg3,
                            insOpts     opt /* = INS_OPTS_NONE */)
{
    emitAttr size = EA_SIZE(attr);

    // Validate instruction and register types
    switch (ins)
    {
        // Floating-point arithmetic instructions (A-form)
        case INS_fadds:
        case INS_fadd:
        case INS_fsubs:
        case INS_fsub:
        case INS_fmuls:
        case INS_fmul:
        case INS_fdivs:
        case INS_fdiv:
            assert(isFloatReg(reg1));
            assert(isFloatReg(reg2));
            assert(isFloatReg(reg3));
            assert(size == EA_4BYTE || size == EA_8BYTE);
            break;

        // Integer arithmetic instructions (XO-form)
        case INS_add:
        case INS_subf:
        case INS_mulld:
        case INS_mullw:
        case INS_divd:
        case INS_divdu:
        case INS_divw:
        case INS_divwu:
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(size == EA_4BYTE || size == EA_8BYTE);
            break;
        // Logical/Bitwise instructions (X-form)
        case INS_and_ins:
        case INS_or_ins:
        case INS_xor_ins:
        case INS_nor:
        case INS_nand:
        case INS_andc:
        case INS_orc:
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(size == EA_4BYTE || size == EA_8BYTE);
            break;

	// Shift instructions (register-based)
        case INS_sld:
        case INS_srd:
        case INS_srad:
        case INS_slw:
        case INS_srw:
        case INS_sraw:
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(size == EA_4BYTE || size == EA_8BYTE);
            break;
	// Indexed Load/Store instructions (X-form) - Phase 4A
        case INS_lbzx:
        case INS_lhzx:
        case INS_lhax:
        case INS_lwzx:
        case INS_lwax:
        case INS_ldx:
        case INS_stbx:
        case INS_sthx:
        case INS_stwx:
        case INS_stdx:
            assert(isGeneralRegister(reg1));  // Data register
            assert(isGeneralRegister(reg2));  // Base address register
            assert(isGeneralRegister(reg3));  // Index register
            assert(size == EA_1BYTE || size == EA_2BYTE || size == EA_4BYTE || size == EA_8BYTE);
            break;

        // Indexed Floating-Point Load/Store (X-form) - Phase 4A
        case INS_lfsx:
        case INS_lfdx:
        case INS_stfsx:
        case INS_stfdx:
            assert(isFloatReg(reg1));         // FP data register
            assert(isGeneralRegister(reg2));  // Base address register
            assert(isGeneralRegister(reg3));  // Index register
            assert(size == EA_4BYTE || size == EA_8BYTE);
            break;

           
        default:
            NYI("emitIns_R_R_R - unsupported instruction");
            return;
    }

    // Create instruction descriptor
    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idInsOpt(opt);
    
    // Set instruction format based on instruction type
    insFormat fmt = IF_NONE;
    switch (ins)
{
    // Floating-point arithmetic (A-form)
    case INS_fadds:
    case INS_fadd:
    case INS_fsubs:
    case INS_fsub:
    case INS_fmuls:
    case INS_fmul:
    case INS_fdivs:
    case INS_fdiv:
        fmt = IF_RR_2A;  // Use RR_2A format for 3-register FP operations
        break;

    // Integer arithmetic (XO-form)
    case INS_add:
    case INS_subf:
    case INS_mulld:
    case INS_mullw:
    case INS_divd:
    case INS_divdu:
    case INS_divw:
    case INS_divwu:
        fmt = IF_RR_2A;  // Use RR_2A format for 3-register integer operations
        break;

    // Logical/Bitwise (X-form)
    case INS_and_ins:
    case INS_or_ins:
    case INS_xor_ins:
    case INS_nor:
    case INS_nand:
    case INS_andc:
    case INS_orc:
        fmt = IF_RR_2A;  // Use RR_2A format for 3-register logical operations
        break;
    // Shift instructions (X-form)
    case INS_sld:
    case INS_srd:
    case INS_srad:
    case INS_slw:
    case INS_srw:
    case INS_sraw:
        fmt = IF_RR_2A; 
	break;    
    // Indexed Load/Store (X-form) - Phase 4A
    case INS_lbzx:
    case INS_lhzx:
    case INS_lhax:
    case INS_lwzx:
    case INS_lwax:
    case INS_ldx:
    case INS_lfsx:
    case INS_lfdx:
    case INS_stbx:
    case INS_sthx:
    case INS_stwx:
    case INS_stdx:
    case INS_stfsx:
    case INS_stfdx:
        fmt = IF_RR_2A;  // Use same format as other 3-register operations
        break;
    default:
        	assert(!"Unexpected instruction in emitIns_R_R_R");
        	break;
	}

    id->idInsFmt(fmt);  // SET THE FORMAT!

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
        case INS_hwsync:
            fmt = IF_SR_1E;  // hwsync
            break;
        case INS_lwsync:
            fmt = IF_SR_1F;  // lwsync
            break;
        case INS_isync:
            fmt = IF_SR_1G;  // isync
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
        case INS_lfs:   // Load Floating-Point Single
        case INS_lfd:   // Load Floating-Point Double

        // Store instructions
        case INS_stb:   // Store Byte
        case INS_sth:   // Store Halfword
        case INS_stw:   // Store Word
        case INS_std:   // Store Doubleword
        case INS_stdu:  // Store Doubleword with Update
        case INS_stfs:  // Store Floating-Point Single
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
     // Move address from R0 to R9 to avoid PowerPC ISA quirk
            // where R0 as base register means "address 0" instead of "contents of R0"
            emitIns_R_R(INS_mov, EA_PTRSIZE, REG_R9, REG_R0);
            baseReg = REG_R9;
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

       case INS_fmr:
           // fmr fD, fB - Floating Move Register
           ppc_fmr(dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       case INS_frsp:
           // frsp fD, fB - Floating Round to Single Precision
           ppc_frsp(dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       case INS_fcfid:
           // fcfid fD, fB - Floating Convert From Integer Doubleword (signed to double)
           ppc_fcfid(dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       case INS_fcfids:
           // fcfids fD, fB - Floating Convert From Integer Doubleword Single (signed to single)
           ppc_fcfids(dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       case INS_fcfidu:
           // fcfidu fD, fB - Floating Convert From Integer Doubleword Unsigned (unsigned to double)
           ppc_fcfidu(dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       case INS_fcfidus:
           // fcfidus fD, fB - Floating Convert From Integer Doubleword Unsigned Single (unsigned to single)
           ppc_fcfidus(dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       case INS_fctiwz:
           // fctiwz fD, fB - Floating Convert To Integer Word with round toward Zero (to signed 32-bit)
           ppc_fctiwz(dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       case INS_fctidz:
           // fctidz fD, fB - Floating Convert To Integer Doubleword with round toward Zero (to signed 64-bit)
           ppc_fctidz(dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       case INS_fctiwuz:
           // fctiwuz fD, fB - Floating Convert To Integer Word Unsigned with round toward Zero (to unsigned 32-bit)
           ppc_fctiwuz(dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       case INS_fctiduz:
           // fctiduz fD, fB - Floating Convert To Integer Doubleword Unsigned with round toward Zero (to unsigned 64-bit)
           ppc_fctiduz(dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

	case INS_extsb:
	    // extsb rA, rS - Extend Sign Byte (sign-extend 8-bit to 64-bit)
	    ppc_extsb(dstRW, id->idReg1(), id->idReg2());
	    break;

	case INS_extsh:
	    // extsh rA, rS - Extend Sign Halfword (sign-extend 16-bit to 64-bit)
	    ppc_extsh(dstRW, id->idReg1(), id->idReg2());
	    break;

 	case INS_extsw:
           // extsw rA, rS - Extend Sign Word (sign-extend 32-bit to 64-bit)
           ppc_extsw(dstRW, id->idReg1(), id->idReg2());
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

       case INS_hwsync:
           ppc_hwsync (dstRW);
           break;

       case INS_lwsync:
           ppc_lwsync (dstRW);
           break;

       case INS_isync:
           ppc_isync (dstRW);
           break;

       case INS_mflr:
           // mflr rD - Move from Link Register
           ppc_mflr (dstRW, id->idReg1());
           break;

       case INS_mtlr:
           // mtlr rS - Move to Link Register
           ppc_mtlr (dstRW, id->idReg1());
           break;

       case INS_mtctr:
           // mtctr rS - Move to Count Register
           ppc_mtctr (dstRW, id->idReg1());
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
           // bl instruction - Branch and Link (direct call within ±32MB)
           {
               void* target = (void*)id->idAddr()->iiaAddr;
               int offset = getBranchOffset(dst, target);
               
               // At this point, offset should be valid because emitIns_Call
               // should have chosen the trampoline path if needed
               assert(offset != 0 && "bl offset out of range - should use trampoline");
               
               ppc_bl(dstRW, offset);
           }
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

       case INS_andi:
           // andi. rA, rS, UIMM (AND Immediate with record bit)
           ppc_andi (dstRW, id->idReg1(), id->idReg2(), emitGetInsSC(id));
           break;

       case INS_xori:
           // xori rA, rS, UIMM (XOR Immediate)
           ppc_xori (dstRW, id->idReg1(), id->idReg2(), emitGetInsSC(id));
           break;	   

       case INS_sldi:
           // sldi rA, rS, n (pseudo-op: rldicr)
           ppc_sldi (dstRW, id->idReg1(), id->idReg2(), emitGetInsSC(id));
           break;
       case INS_srdi:
           // srdi rA, rS, n (pseudo-op: rldicl)
           ppc_srdi (dstRW, id->idReg1(), id->idReg2(), emitGetInsSC(id));
           break;
       
        // Register-based shifts
	case INS_sld:
    	   ppc_sld(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

	case INS_srd:
           ppc_srd(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

	case INS_srad:
           ppc_srad(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

        case INS_slw:
    	   ppc_slw(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

	case INS_srw:
    	   ppc_srw(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
    	   break;

	case INS_sraw:
    	   ppc_sraw(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
    	   break;

	// Immediate-based shifts
	case INS_sradi:
    	   ppc_sradi(dstRW, id->idReg1(), id->idReg2(), emitGetInsSC(id));
   	   break;

	case INS_slwi:
   	   ppc_slwi(dstRW, id->idReg1(), id->idReg2(), emitGetInsSC(id));
   	   break;

	case INS_srwi:
    	   ppc_srwi(dstRW, id->idReg1(), id->idReg2(), emitGetInsSC(id));
   	   break;

	case INS_srawi:
    	   ppc_srawi(dstRW, id->idReg1(), id->idReg2(), emitGetInsSC(id));
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

       case INS_lfs:
           // lfs fD, d(rA) - Load Floating-Point Single
           ppc_lfs (dstRW, id->idReg1() - REG_F0, emitGetInsSC(id), id->idReg2());
           break;

       case INS_lfd:
           // lfd fD, d(rA) - Load Floating-Point Double
           ppc_lfd (dstRW, id->idReg1() - REG_F0, emitGetInsSC(id), id->idReg2());
           break;

       case INS_stfs:
           // stfs fS, d(rA) - Store Floating-Point Single
           ppc_stfs (dstRW, id->idReg1() - REG_F0, emitGetInsSC(id), id->idReg2());
           break;

       case INS_stfd:
           // stfd fS, d(rA) - Store Floating-Point Double
           ppc_stfd (dstRW, id->idReg1() - REG_F0, emitGetInsSC(id), id->idReg2());
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
       // Floating-point arithmetic instructions
       case INS_fadds:
           // fadds fD, fA, fB - Floating Add Single
           ppc_fadds (dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0, id->idReg3() - REG_F0);
           break;

       case INS_fadd:
           // fadd fD, fA, fB - Floating Add Double
           ppc_fadd (dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0, id->idReg3() - REG_F0);
           break;

       case INS_fsubs:
           // fsubs fD, fA, fB - Floating Subtract Single
           ppc_fsubs (dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0, id->idReg3() - REG_F0);
           break;

       case INS_fsub:
           // fsub fD, fA, fB - Floating Subtract Double
           ppc_fsub (dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0, id->idReg3() - REG_F0);
           break;

       case INS_fmuls:
           // fmuls fD, fA, fC - Floating Multiply Single
           ppc_fmuls (dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0, id->idReg3() - REG_F0);
           break;

       case INS_fmul:
           // fmul fD, fA, fC - Floating Multiply Double
           ppc_fmul (dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0, id->idReg3() - REG_F0);
           break;

       case INS_fdivs:
           // fdivs fD, fA, fB - Floating Divide Single
           ppc_fdivs (dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0, id->idReg3() - REG_F0);
           break;

       case INS_fdiv:
           // fdiv fD, fA, fB - Floating Divide Double
           ppc_fdiv (dstRW, id->idReg1() - REG_F0, id->idReg2() - REG_F0, id->idReg3() - REG_F0);
           break;

	case INS_fcmpu:
           // fcmpu cr0, fA, fB - Floating Compare Unordered
           // Result goes to CR0 (crD = 0)
           ppc_fcmpu (dstRW, 0, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       case INS_fcmpo:
           // fcmpo cr0, fA, fB - Floating Compare Ordered
           // Result goes to CR0 (crD = 0)
           ppc_fcmpo (dstRW, 0, id->idReg1() - REG_F0, id->idReg2() - REG_F0);
           break;

       // Integer arithmetic instructions
       case INS_add:
           // add rD, rA, rB - Add
           ppc_add (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_subf:
           // subf rD, rA, rB - Subtract From (rD = rB - rA)
           ppc_subf (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_mulld:
           // mulld rD, rA, rB - Multiply Low Doubleword
           ppc_mulld (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_mullw:
           // mullw rD, rA, rB - Multiply Low Word
           ppc_mullw (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_divd:
           // divd rD, rA, rB - Divide Doubleword
           ppc_divd (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_divdu:
           // divdu rD, rA, rB - Divide Doubleword Unsigned
           ppc_divdu (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_divw:
           // divw rD, rA, rB - Divide Word
           ppc_divw (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_divwu:
           // divwu rD, rA, rB - Divide Word Unsigned
           ppc_divwu (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       // Logical/Bitwise instructions
       case INS_and_ins:
           // and rA, rS, rB - AND
           ppc_and (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_or_ins:
           // or rA, rS, rB - OR
           ppc_or (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_xor_ins:
           // xor rA, rS, rB - XOR
           ppc_xor (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_nor:
           // nor rA, rS, rB - NOR
           ppc_nor (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_nand:
           // nand rA, rS, rB - NAND
           ppc_nand (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_andc:
           // andc rA, rS, rB - AND with Complement
           ppc_andc (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

       case INS_orc:
           // orc rA, rS, rB - OR with Complement
           ppc_orc (dstRW, id->idReg1(), id->idReg2(), id->idReg3());
           break;

        case INS_lbzx:
            ppc_lbzx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_lhzx:
            ppc_lhzx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_lhax:
            ppc_lhax(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_lwzx:
            ppc_lwzx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_lwax:
            ppc_lwax(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_ldx:
            ppc_ldx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_lfsx:
            ppc_lfsx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_lfdx:
            ppc_lfdx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_stbx:
            ppc_stbx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_sthx:
            ppc_sthx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_stwx:
            ppc_stwx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_stdx:
            ppc_stdx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_stfsx:
            ppc_stfsx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
            break;

        case INS_stfdx:
            ppc_stfdx(dstRW, id->idReg1(), id->idReg2(), id->idReg3());
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
        case INS_hwsync:  return "hwsync  ";
        case INS_lwsync:  return "lwsync  ";
        case INS_isync:   return "isync   ";
        case INS_sldi:    return "sldi    ";
	case INS_srdi:    return "srdi    ";
        case INS_oris:    return "oris    ";
	case INS_andi:    return "andi   ";
        case INS_extsb:   return "extsb   ";
        case INS_extsh:   return "extsh   ";
        case INS_extsw:   return "extsw   ";
        case INS_mtctr:   return "mtctr   ";
	case INS_bctrl:   return "bctrl   ";
	case INS_blt:     return "blt     ";
	case INS_add:     return "add     ";
	case INS_trap:    return "trap    ";
	case INS_movi:    return "movi    ";
	case INS_push:    return "push    ";
	case INS_pop:     return "pop     ";
	case INS_bctr:    return "bctr    ";
        case INS_sld:     return "sld     ";
	case INS_srd:     return "srd     ";
	case INS_srad:    return "srad    ";
	case INS_slw:     return "slw     ";
	case INS_srw:     return "srw     ";
	case INS_sraw:    return "sraw    ";
	case INS_sradi:   return "sradi   ";
	case INS_slwi:    return "slwi    ";
	case INS_srwi:    return "srwi    ";
	case INS_srawi:   return "srawi   ";			  
	case INS_cmpd:    return "cmpd    ";
	case INS_cmpdi:   return "cmpdi   ";
	case INS_lbz:     return "lbz     ";
	case INS_lhz:     return "lhz     ";
	case INS_lha:     return "lha     ";
	case INS_lfs:     return "lfs     ";
	case INS_lfd:     return "lfd     ";
        case INS_stfs:    return "stfs    ";
	case INS_stfd:    return "stfd    ";
	case INS_stb:     return "stb     ";
	case INS_sth:     return "sth     ";			
	case INS_bge:     return "bge     ";
	case INS_bgt:     return "bgt     ";
	case INS_ble:     return "ble     ";
	case INS_fadds:   return "fadds   ";
	case INS_fadd:    return "fadd    ";
	case INS_fsubs:   return "fsubs   ";
	case INS_fsub:    return "fsub    ";
	case INS_fmuls:   return "fmuls   ";
	case INS_fmul:    return "fmul    ";
	case INS_fdivs:   return "fdivs   ";
	case INS_fdiv:    return "fdiv    ";
	case INS_fmr:     return "fmr     ";
	case INS_fcmpu:   return "fcmpu   ";
	case INS_fcmpo:   return "fcmpo   ";
	case INS_frsp:    return "frsp    ";		  
	case INS_fctiwz:  return "fctiwz  ";
	case INS_fctidz:  return "fctidz  ";
	case INS_fctiwuz: return "fctiwuz ";
	case INS_fctiduz: return "fctiduz ";
	case INS_fcfid:   return "fcfid   ";
	case INS_fcfids:  return "fcfids  ";
	case INS_fcfidu:  return "fcfidu  ";
	case INS_fcfidus: return "fcfidus ";
	case INS_subf:    return "subf    ";
	case INS_mulld:   return "mulld   ";
	case INS_mullw:   return "mullw   ";
	case INS_divd:    return "divd    ";
	case INS_divdu:   return "divdu   ";
	case INS_divw:    return "divw    ";
	case INS_divwu:   return "divwu   ";
	case INS_and_ins: return "and     ";
	case INS_or_ins:  return "or      ";
	case INS_xor_ins: return "xor     ";
	case INS_nor:     return "nor     ";
	case INS_nand:    return "nand    ";
	case INS_andc:    return "andc    ";
	case INS_orc:     return "orc     ";
	case INS_xori:    return "xori    ";
	case INS_xoris:   return "xoris   ";
	case INS_lbzx:   return "lbzx";
        case INS_lhzx:   return "lhzx";
        case INS_lhax:   return "lhax";
        case INS_lwzx:   return "lwzx";
        case INS_lwax:   return "lwax";
        case INS_ldx:    return "ldx";
        case INS_lfsx:   return "lfsx";
        case INS_lfdx:   return "lfdx";
        case INS_stbx:   return "stbx";
        case INS_sthx:   return "sthx";
        case INS_stwx:   return "stwx";
        case INS_stdx:   return "stdx";
        case INS_stfsx:  return "stfsx";
        case INS_stfdx:  return "stfdx";			  
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
        case INS_bl:
            // Unconditional branch - I-form (24-bit signed offset)
            // INS_b: branch, INS_bl: branch and link
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

    // emitNewInstrJmp() sets the size (incorrectly) to EA_1BYTE
    // Override it to EA_PTRSIZE so the descriptor is not treated as small
    // This allows access to idAddr() which requires a large descriptor
    id->idOpSize(EA_PTRSIZE);

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


    // Print offset/address
    if (doffs && pCode != nullptr)
    {
        // Print actual code address in hex
        printf("  %08X", (unsigned int)((uintptr_t)pCode & 0xFFFFFFFF));
    }
    else if (doffs)
    {
        // Print offset if we don't have code pointer yet
        printf("  %08X", offset);
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
	case INS_andi:
        case INS_sldi:
	case INS_srdi:
            printf("r%d, r%d, %d", id->idReg1(), id->idReg2(), (int)emitGetInsSC(id));
            break;
            
        case INS_mov:
        case INS_cmpw:
        case INS_extsb:
        case INS_extsh:
        case INS_extsw:
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
        case INS_hwsync:
        case INS_lwsync:
        case INS_isync:
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
