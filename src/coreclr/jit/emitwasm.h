// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/************************************************************************/
/*             Debug-only routines to display instructions              */
/************************************************************************/

#if defined(DEBUG) || defined(LATE_DISASM)
void getInsSveExecutionCharacteristics(instrDesc* id, insExecutionCharacteristics& result);
#endif // defined(DEBUG) || defined(LATE_DISASM)

/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
void emitIns(instruction ins);
void emitIns_I(instruction ins, emitAttr attr, ssize_t imm);
void emitIns_R(instruction ins, emitAttr attr, regNumber reg);

void emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm);
void emitIns_Mov(instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip);
void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2);

void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

/************************************************************************/
/*  Private members that deal with target-dependent instr. descriptors  */
/************************************************************************/

private:
instrDesc* emitNewInstrCallDir(
    int argCnt, VARSET_VALARG_TP GCvars, regMaskTP gcrefRegs, regMaskTP byrefRegs, emitAttr retSize, bool hasAsyncRet);

instrDesc* emitNewInstrCallInd(int              argCnt,
                               ssize_t          disp,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr         retSize,
                               bool             hasAsyncRet);

/************************************************************************/
/*               Private helpers for instruction output                 */
/************************************************************************/

private:
bool emitInsIsStore(instruction ins);
