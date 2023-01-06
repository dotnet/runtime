// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        Amd64 SIMD Code Generator                          XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#pragma warning(disable : 4310) // cast truncates constant value - happens for (int8_t)SHUFFLE_ZXXX
#endif

#ifdef TARGET_XARCH
#ifdef FEATURE_SIMD

#include "emit.h"
#include "codegen.h"
#include "sideeffects.h"
#include "lower.h"
#include "gcinfo.h"
#include "gcinfoencoder.h"

// Instruction immediates

// Insertps:
// - bits 6 and 7 of the immediate indicate which source item to select (0..3)
// - bits 4 and 5 of the immediate indicate which target item to insert into (0..3)
// - bits 0 to 3 of the immediate indicate which target item to zero
#define INSERTPS_SOURCE_SELECT(i) ((i) << 6)
#define INSERTPS_TARGET_SELECT(i) ((i) << 4)
#define INSERTPS_ZERO(i) (1 << (i))

// ROUNDPS/PD:
// - Bit 0 through 1 - Rounding mode
//   * 0b00 - Round to nearest (even)
//   * 0b01 - Round toward Neg. Infinity
//   * 0b10 - Round toward Pos. Infinity
//   * 0b11 - Round toward zero (Truncate)
// - Bit 2 - Source of rounding control, 0b0 for immediate.
// - Bit 3 - Precision exception, 0b1 to ignore. (We don't raise FP exceptions)
#define ROUNDPS_TO_NEAREST_IMM 0b1000
#define ROUNDPS_TOWARD_NEGATIVE_INFINITY_IMM 0b1001
#define ROUNDPS_TOWARD_POSITIVE_INFINITY_IMM 0b1010
#define ROUNDPS_TOWARD_ZERO_IMM 0b1011

//-----------------------------------------------------------------------------
// genStoreIndTypeSIMD12: store indirect a TYP_SIMD12 (i.e. Vector3) to memory.
// Since Vector3 is not a hardware supported write size, it is performed
// as two writes: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node that is attempting to store indirect
//
//
// Return Value:
//    None.
//
void CodeGen::genStoreIndTypeSIMD12(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_STOREIND);

    GenTree* addr = treeNode->AsOp()->gtOp1;
    GenTree* data = treeNode->AsOp()->gtOp2;

    // addr and data should not be contained.
    assert(!data->isContained());
    assert(!addr->isContained());

#ifdef DEBUG
    // Should not require a write barrier
    GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(treeNode->AsStoreInd());
    assert(writeBarrierForm == GCInfo::WBF_NoBarrier);
#endif

    // Need an additional Xmm register to extract upper 4 bytes from data.
    regNumber tmpReg = treeNode->GetSingleTempReg();

    genConsumeOperands(treeNode->AsOp());

    // 8-byte write
    GetEmitter()->emitIns_AR_R(ins_Store(TYP_DOUBLE), EA_8BYTE, data->GetRegNum(), addr->GetRegNum(), 0);

    // Extract upper 4-bytes from data
    GetEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(TYP_SIMD16), tmpReg, data->GetRegNum(), 0x02);

    // 4-byte write
    GetEmitter()->emitIns_AR_R(ins_Store(TYP_FLOAT), EA_4BYTE, tmpReg, addr->GetRegNum(), 8);
}

//-----------------------------------------------------------------------------
// genLoadIndTypeSIMD12: load indirect a TYP_SIMD12 (i.e. Vector3) value.
// Since Vector3 is not a hardware supported write size, it is performed
// as two loads: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node of GT_IND
//
//
// Return Value:
//    None.
//
void CodeGen::genLoadIndTypeSIMD12(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_IND);

    regNumber targetReg = treeNode->GetRegNum();
    GenTree*  op1       = treeNode->AsOp()->gtOp1;
    assert(!op1->isContained());
    regNumber operandReg = genConsumeReg(op1);

    // Need an additional Xmm register to read upper 4 bytes, which is different from targetReg
    regNumber tmpReg = treeNode->GetSingleTempReg();
    assert(tmpReg != targetReg);

    // Load upper 4 bytes in tmpReg
    GetEmitter()->emitIns_R_AR(ins_Load(TYP_FLOAT), EA_4BYTE, tmpReg, operandReg, 8);

    // Load lower 8 bytes in targetReg
    GetEmitter()->emitIns_R_AR(ins_Load(TYP_DOUBLE), EA_8BYTE, targetReg, operandReg, 0);

    // combine upper 4 bytes and lower 8 bytes in targetReg
    GetEmitter()->emitIns_R_R_I(INS_shufps, emitActualTypeSize(TYP_SIMD16), targetReg, tmpReg, (int8_t)SHUFFLE_YXYX);

    genProduceReg(treeNode);
}

//-----------------------------------------------------------------------------
// genStoreLclTypeSIMD12: store a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported write size, it is performed
// as two stores: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node that is attempting to store TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genStoreLclTypeSIMD12(GenTree* treeNode)
{
    assert((treeNode->OperGet() == GT_STORE_LCL_FLD) || (treeNode->OperGet() == GT_STORE_LCL_VAR));

    const GenTreeLclVarCommon* lclVar = treeNode->AsLclVarCommon();
    unsigned                   offs   = lclVar->GetLclOffs();
    unsigned                   varNum = lclVar->GetLclNum();
    assert(varNum < compiler->lvaCount);

    GenTree* op1 = lclVar->gtOp1;

    assert(!op1->isContained());
    regNumber targetReg  = treeNode->GetRegNum();
    regNumber operandReg = genConsumeReg(op1);

    if (targetReg != REG_NA)
    {
        assert(!GetEmitter()->isGeneralRegister(targetReg));

        inst_Mov(treeNode->TypeGet(), targetReg, operandReg, /* canSkip */ true);

        genProduceReg(treeNode);
    }
    else
    {
        // store lower 8 bytes
        GetEmitter()->emitIns_S_R(ins_Store(TYP_DOUBLE), EA_8BYTE, operandReg, varNum, offs);

        if (!op1->IsVectorZero())
        {
            regNumber tmpReg = treeNode->GetSingleTempReg();

            // Extract upper 4-bytes from operandReg
            GetEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(TYP_SIMD16), tmpReg, operandReg, 0x02);

            operandReg = tmpReg;
        }

        // Store upper 4 bytes
        GetEmitter()->emitIns_S_R(ins_Store(TYP_FLOAT), EA_4BYTE, operandReg, varNum, offs + 8);

        // Update the life of treeNode
        genUpdateLife(treeNode);

        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        varDsc->SetRegNum(REG_STK);
    }
}

//-----------------------------------------------------------------------------
// genLoadLclTypeSIMD12: load a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported read size, it is performed
// as two reads: 4 byte followed by 8 byte.
//
// Arguments:
//    treeNode - tree node that is attempting to load TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genLoadLclTypeSIMD12(GenTree* treeNode)
{
    assert((treeNode->OperGet() == GT_LCL_FLD) || (treeNode->OperGet() == GT_LCL_VAR));

    const GenTreeLclVarCommon* lclVar    = treeNode->AsLclVarCommon();
    regNumber                  targetReg = lclVar->GetRegNum();
    unsigned                   offs      = lclVar->GetLclOffs();
    unsigned                   varNum    = lclVar->GetLclNum();
    assert(varNum < compiler->lvaCount);

    // Need an additional Xmm register that is different from targetReg to read upper 4 bytes.
    regNumber tmpReg = treeNode->GetSingleTempReg();
    assert(tmpReg != targetReg);

    // Read upper 4 bytes to tmpReg
    GetEmitter()->emitIns_R_S(ins_Move_Extend(TYP_FLOAT, false), EA_4BYTE, tmpReg, varNum, offs + 8);

    // Read lower 8 bytes to targetReg
    GetEmitter()->emitIns_R_S(ins_Move_Extend(TYP_DOUBLE, false), EA_8BYTE, targetReg, varNum, offs);

    // combine upper 4 bytes and lower 8 bytes in targetReg
    GetEmitter()->emitIns_R_R_I(INS_shufps, emitActualTypeSize(TYP_SIMD16), targetReg, tmpReg, (int8_t)SHUFFLE_YXYX);

    genProduceReg(treeNode);
}

#ifdef TARGET_X86

//-----------------------------------------------------------------------------
// genStoreSIMD12ToStack: store a TYP_SIMD12 (i.e. Vector3) type field to the stack.
// Since Vector3 is not a hardware supported write size, it is performed
// as two stores: 8 byte followed by 4-byte. The stack is assumed to have
// already been adjusted.
//
// Arguments:
//    operandReg - the xmm register containing the SIMD12 to store.
//    tmpReg - an xmm register that can be used as a temporary for the operation.
//
// Return Value:
//    None.
//
void CodeGen::genStoreSIMD12ToStack(regNumber operandReg, regNumber tmpReg)
{
    assert(genIsValidFloatReg(operandReg));
    assert(genIsValidFloatReg(tmpReg));

    // 8-byte write
    GetEmitter()->emitIns_AR_R(ins_Store(TYP_DOUBLE), EA_8BYTE, operandReg, REG_SPBASE, 0);

    // Extract upper 4-bytes from data
    GetEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(TYP_SIMD16), tmpReg, operandReg, 0x02);

    // 4-byte write
    GetEmitter()->emitIns_AR_R(ins_Store(TYP_FLOAT), EA_4BYTE, tmpReg, REG_SPBASE, 8);
}

//-----------------------------------------------------------------------------
// genPutArgStkSIMD12: store a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported write size, it is performed
// as two stores: 8 byte followed by 4-byte. The stack is assumed to have
// already been adjusted.
//
// Arguments:
//    treeNode - tree node that is attempting to store TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genPutArgStkSIMD12(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_PUTARG_STK);

    GenTree* op1 = treeNode->AsOp()->gtOp1;
    assert(!op1->isContained());
    regNumber operandReg = genConsumeReg(op1);

    // Need an additional Xmm register to extract upper 4 bytes from data.
    regNumber tmpReg = treeNode->GetSingleTempReg();

    genStoreSIMD12ToStack(operandReg, tmpReg);
}

#endif // TARGET_X86

//-----------------------------------------------------------------------------
// genSIMDIntrinsicUpperSave: save the upper half of a TYP_SIMD32 vector to
//                            the given register, if any, or to memory.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
// Notes:
//    The upper half of all AVX registers is volatile, even the callee-save registers.
//    When a 32-byte SIMD value is live across a call, the register allocator will use this intrinsic
//    to cause the upper half to be saved.  It will first attempt to find another, unused, callee-save
//    register.  If such a register cannot be found, it will save the upper half to the upper half
//    of the localVar's home location.
//    (Note that if there are no caller-save registers available, the entire 32 byte
//    value will be spilled to the stack.)
//
void CodeGen::genSIMDIntrinsicUpperSave(GenTreeSIMD* simdNode)
{
    assert(simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicUpperSave);

    GenTree* op1 = simdNode->Op(1);
    assert(op1->IsLocal() && op1->TypeGet() == TYP_SIMD32);
    regNumber targetReg = simdNode->GetRegNum();
    regNumber op1Reg    = genConsumeReg(op1);
    assert(op1Reg != REG_NA);
    if (targetReg != REG_NA)
    {
        GetEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, targetReg, op1Reg, 0x01);
        genProduceReg(simdNode);
    }
    else
    {
        // The localVar must have a stack home.
        unsigned   varNum = op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        assert(varDsc->lvOnFrame);
        // We want to store this to the upper 16 bytes of this localVar's home.
        int offs = 16;

        GetEmitter()->emitIns_S_R_I(INS_vextractf128, EA_32BYTE, varNum, offs, op1Reg, 0x01);
    }
}

//-----------------------------------------------------------------------------
// genSIMDIntrinsicUpperRestore: Restore the upper half of a TYP_SIMD32 vector to
//                               the given register, if any, or to memory.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
// Notes:
//    For consistency with genSIMDIntrinsicUpperSave, and to ensure that lclVar nodes always
//    have their home register, this node has its targetReg on the lclVar child, and its source
//    on the simdNode.
//
void CodeGen::genSIMDIntrinsicUpperRestore(GenTreeSIMD* simdNode)
{
    assert(simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicUpperRestore);

    GenTree* op1 = simdNode->Op(1);
    assert(op1->IsLocal() && op1->TypeGet() == TYP_SIMD32);
    regNumber srcReg    = simdNode->GetRegNum();
    regNumber lclVarReg = genConsumeReg(op1);
    assert(lclVarReg != REG_NA);
    if (srcReg != REG_NA)
    {
        GetEmitter()->emitIns_R_R_R_I(INS_vinsertf128, EA_32BYTE, lclVarReg, lclVarReg, srcReg, 0x01);
    }
    else
    {
        // The localVar must have a stack home.
        unsigned   varNum = op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        assert(varDsc->lvOnFrame);
        // We will load this from the upper 16 bytes of this localVar's home.
        int offs = 16;
        GetEmitter()->emitIns_R_R_S_I(INS_vinsertf128, EA_32BYTE, lclVarReg, lclVarReg, varNum, offs, 0x01);
    }
}

//------------------------------------------------------------------------
// genSIMDIntrinsic: Generate code for a SIMD Intrinsic.  This is the main
// routine which in turn calls appropriate genSIMDIntrinsicXXX() routine.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
// Notes:
//    Currently, we only recognize SIMDVector<float> and SIMDVector<int>, and
//    a limited set of methods.
//
void CodeGen::genSIMDIntrinsic(GenTreeSIMD* simdNode)
{
    // NYI for unsupported base types
    if (!varTypeIsArithmetic(simdNode->GetSimdBaseType()))
    {
        noway_assert(!"SIMD intrinsic with unsupported base type.");
    }

    switch (simdNode->GetSIMDIntrinsicId())
    {
        case SIMDIntrinsicUpperSave:
            genSIMDIntrinsicUpperSave(simdNode);
            break;
        case SIMDIntrinsicUpperRestore:
            genSIMDIntrinsicUpperRestore(simdNode);
            break;

        default:
            noway_assert(!"Unimplemented SIMD intrinsic.");
            unreached();
    }
}

#endif // FEATURE_SIMD
#endif // TARGET_XARCH
