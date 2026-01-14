// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/************************************************************************/
/*             Debug-only routines to display instructions              */
/************************************************************************/

#if defined(DEBUG) || defined(LATE_DISASM)
void getInsSveExecutionCharacteristics(instrDesc* id, insExecutionCharacteristics& result);
#endif // defined(DEBUG) || defined(LATE_DISASM)

void emitDispInst(instruction ins);

/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
void emitIns(instruction ins);
void emitIns_I(instruction ins, emitAttr attr, cnsval_ssize_t imm);
void emitIns_I_Ty(instruction ins, unsigned int imm, WasmValueType valType, int offs);
void emitIns_J(instruction ins, emitAttr attr, cnsval_ssize_t imm, BasicBlock* tgtBlock);
void emitIns_S(instruction ins, emitAttr attr, int varx, int offs);
void emitIns_R(instruction ins, emitAttr attr, regNumber reg);

void emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, cnsval_ssize_t imm);
void emitIns_Mov(instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip);
void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2);

void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

static unsigned SizeOfULEB128(uint64_t value);
static unsigned SizeOfSLEB128(int64_t value);

static unsigned emitGetAlignHintLog2(const instrDesc* id);

instrDesc*           emitNewInstrLclVarDecl(emitAttr attr, unsigned int localCount, WasmValueType type, int lclOffset);
static WasmValueType emitGetLclVarDeclType(const instrDesc* id);
static unsigned int  emitGetLclVarDeclCount(const instrDesc* id);

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

insFormat emitInsFormat(instruction ins);

size_t emitOutputULEB128(uint8_t* destination, uint64_t value);
size_t emitOutputSLEB128(uint8_t* destination, int64_t value);
size_t emitRawBytes(uint8_t* destination, const void* source, size_t count);
