// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/************************************************************************/
/*             Debug-only routines to display instructions              */
/************************************************************************/

void emitDispInst(instruction ins);

/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
void emitIns(instruction ins);
void emitIns_I(instruction ins, emitAttr attr, cnsval_ssize_t imm);
void emitIns_J(instruction ins, emitAttr attr, cnsval_ssize_t imm, BasicBlock* tgtBlock);
void emitIns_R(instruction ins, emitAttr attr, regNumber reg);
void emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, cnsval_ssize_t imm);
void emitIns_Mov(instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip);
void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2);
void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

/************************************************************************/
/*               Private helpers for instruction output                 */
/************************************************************************/

private:
bool emitInsIsStore(instruction ins);

insFormat emitInsFormat(instruction ins);
