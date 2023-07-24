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

//-----------------------------------------------------------------------------
// genStoreIndTypeSimd12: store indirect a TYP_SIMD12 (i.e. Vector3) to memory.
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
void CodeGen::genStoreIndTypeSimd12(GenTreeStoreInd* treeNode)
{
    assert(treeNode->OperIs(GT_STOREIND));

    // Should not require a write barrier
    assert(gcInfo.gcIsWriteBarrierCandidate(treeNode) == GCInfo::WBF_NoBarrier);

    GenTree* addr = treeNode->Addr();
    genConsumeAddress(addr);

    GenTree*  data    = treeNode->Data();
    regNumber dataReg = genConsumeReg(data);

    if (addr->isContained() && addr->OperIs(GT_LCL_ADDR))
    {
        genEmitStoreLclTypeSimd12(treeNode, addr->AsLclFld()->GetLclNum(), addr->AsLclFld()->GetLclOffs());
        genUpdateLife(treeNode);
        return;
    }

    emitter* emit = GetEmitter();

    // Store lower 8 bytes
    emit->emitInsStoreInd(INS_movsd_simd, EA_8BYTE, treeNode);

    // Update the addr node to offset by 8

    if (treeNode->isIndirAddrMode())
    {
        GenTreeAddrMode* addrMode = addr->AsAddrMode();
        addrMode->SetOffset(addrMode->Offset() + 8);
    }
    else if (addr->IsCnsIntOrI() && addr->isContained())
    {
        GenTreeIntConCommon* icon = addr->AsIntConCommon();
        assert(!icon->ImmedValNeedsReloc(compiler));
        icon->SetIconValue(icon->IconValue() + 8);
    }
    else
    {
        addr = new (compiler, GT_LEA) GenTreeAddrMode(addr->TypeGet(), addr, nullptr, 0, 8);
        addr->SetContained();
    }

    treeNode->Addr() = addr;

    if (data->IsVectorZero())
    {
        // Store upper 4 bytes
        emit->emitInsStoreInd(INS_movss, EA_4BYTE, treeNode);
    }
    else if (compiler->compOpportunisticallyDependsOn(InstructionSet_SSE41))
    {
        // Extract and store upper 4 bytes
        GenTreeStoreInd storeInd = storeIndirForm(TYP_SIMD16, addr, data);
        emit->emitIns_A_R_I(INS_extractps, EA_16BYTE, &storeInd, dataReg, 2);
    }
    else
    {
        regNumber tmpReg = treeNode->GetSingleTempReg();

        // Extract upper 4 bytes from data
        emit->emitIns_R_R(INS_movhlps, EA_16BYTE, tmpReg, dataReg);
        data->SetRegNum(tmpReg);

        // Store upper 4 bytes
        emit->emitInsStoreInd(INS_movss, EA_4BYTE, treeNode);
    }
}

//-----------------------------------------------------------------------------
// genLoadIndTypeSimd12: load indirect a TYP_SIMD12 (i.e. Vector3) value.
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
void CodeGen::genLoadIndTypeSimd12(GenTreeIndir* treeNode)
{
    assert(treeNode->OperIs(GT_IND));

    GenTree* addr = treeNode->Addr();
    genConsumeAddress(addr);

    if (addr->isContained() && addr->OperIs(GT_LCL_ADDR))
    {
        genEmitLoadLclTypeSimd12(treeNode->GetRegNum(), addr->AsLclFld()->GetLclNum(), addr->AsLclFld()->GetLclOffs());
        genProduceReg(treeNode);
        return;
    }

    emitter*  emit     = GetEmitter();
    regNumber tgtReg   = treeNode->GetRegNum();
    bool      useSse41 = compiler->compOpportunisticallyDependsOn(InstructionSet_SSE41);

    if (useSse41)
    {
        // Load lower 8 bytes
        emit->emitInsLoadInd(INS_movsd_simd, EA_8BYTE, tgtReg, treeNode);
    }

    // Update the addr node to offset by 8

    if (treeNode->isIndirAddrMode())
    {
        GenTreeAddrMode* addrMode = addr->AsAddrMode();
        addrMode->SetOffset(addrMode->Offset() + 8);
    }
    else if (addr->IsCnsIntOrI() && addr->isContained())
    {
        GenTreeIntConCommon* icon = addr->AsIntConCommon();
        assert(!icon->ImmedValNeedsReloc(compiler));
        icon->SetIconValue(icon->IconValue() + 8);
    }
    else
    {
        addr = new (compiler, GT_LEA) GenTreeAddrMode(addr->TypeGet(), addr, nullptr, 0, 8);
        addr->SetContained();
    }

    treeNode->Addr() = addr;

    if (useSse41)
    {
        // Load and insert upper 4 bytes, 0x20 inserts to index 2 and 0x8 zeros index 3
        GenTreeIndir indir = indirForm(TYP_SIMD16, addr);
        emit->emitIns_SIMD_R_R_A_I(INS_insertps, EA_16BYTE, tgtReg, tgtReg, &indir, 0x28);
    }
    else
    {
        // Load upper 4 bytes to lower half of tgtReg
        emit->emitInsLoadInd(INS_movss, EA_4BYTE, tgtReg, treeNode);

        // Move upper 4 bytes to upper half of tgtReg
        emit->emitIns_R_R(INS_movlhps, EA_16BYTE, tgtReg, tgtReg);

        // Revert the addr node to the original offset
        // Doing it this way saves us a register and produces smaller code

        if (treeNode->isIndirAddrMode())
        {
            GenTreeAddrMode* addrMode = addr->AsAddrMode();
            addrMode->SetOffset(addrMode->Offset() - 8);
        }
        else if (addr->IsCnsIntOrI() && addr->isContained())
        {
            GenTreeIntConCommon* icon = addr->AsIntConCommon();
            icon->SetIconValue(icon->IconValue() - 8);
        }
        else
        {
            unreached();
        }

        // Load lower 8 bytes into tgtReg, preserving upper 4 bytes
        emit->emitInsLoadInd(INS_movlps, EA_16BYTE, tgtReg, treeNode);
    }

    genProduceReg(treeNode);
}

//-----------------------------------------------------------------------------
// genStoreLclTypeSimd12: store a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported write size, it is performed
// as two stores: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node that is attempting to store TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genStoreLclTypeSimd12(GenTreeLclVarCommon* treeNode)
{
    assert(treeNode->OperIs(GT_STORE_LCL_FLD, GT_STORE_LCL_VAR));

    emitter* emit = GetEmitter();

    unsigned offs   = treeNode->GetLclOffs();
    unsigned varNum = treeNode->GetLclNum();
    assert(varNum < compiler->lvaCount);

    GenTree* data = treeNode->Data();
    assert(!data->isContained());

    regNumber  tgtReg  = treeNode->GetRegNum();
    regNumber  dataReg = genConsumeReg(data);
    LclVarDsc* varDsc  = compiler->lvaGetDesc(varNum);

    if (tgtReg != REG_NA)
    {
        // Simply use mov if we move a SIMD12 reg to another SIMD12 reg
        assert(genIsValidFloatReg(tgtReg));

        inst_Mov(treeNode->TypeGet(), tgtReg, dataReg, /* canSkip */ true);
    }
    else
    {
        genEmitStoreLclTypeSimd12(treeNode, varNum, offs);
    }

    genUpdateLifeStore(treeNode, tgtReg, varDsc);
}

//-----------------------------------------------------------------------------
// genLoadLclTypeSimd12: load a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported read size, it is performed
// as two reads: 4 byte followed by 8 byte.
//
// Arguments:
//    treeNode - tree node that is attempting to load TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genLoadLclTypeSimd12(GenTreeLclVarCommon* treeNode)
{
    assert(treeNode->OperIs(GT_LCL_FLD, GT_LCL_VAR));
    genEmitLoadLclTypeSimd12(treeNode->GetRegNum(), treeNode->GetLclNum(), treeNode->GetLclOffs());
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genEmitStoreLclTypeSimd12: Emit code to store a SIMD12 value to stack.
//
// Arguments:
//    store  - The store node
//    lclNum - Stack local's number
//    offset - Offset to store at
//
void CodeGen::genEmitStoreLclTypeSimd12(GenTree* store, unsigned lclNum, unsigned offset)
{
    assert(store->OperIsLocalStore() || store->OperIs(GT_STOREIND));

    emitter*  emit    = GetEmitter();
    GenTree*  data    = store->Data();
    regNumber dataReg = data->GetRegNum();

    // Store lower 8 bytes
    emit->emitIns_S_R(INS_movsd_simd, EA_8BYTE, dataReg, lclNum, offset);

    if (data->IsVectorZero())
    {
        // Store upper 4 bytes
        emit->emitIns_S_R(INS_movss, EA_4BYTE, dataReg, lclNum, offset + 8);
    }
    else if (compiler->compOpportunisticallyDependsOn(InstructionSet_SSE41))
    {
        // Extract and store upper 4 bytes
        emit->emitIns_S_R_I(INS_extractps, EA_16BYTE, lclNum, offset + 8, dataReg, 2);
    }
    else
    {
        regNumber tmpReg = store->GetSingleTempReg();

        // Extract upper 4 bytes from data
        emit->emitIns_R_R(INS_movhlps, EA_16BYTE, tmpReg, dataReg);

        // Store upper 4 bytes
        emit->emitIns_S_R(INS_movss, EA_4BYTE, tmpReg, lclNum, offset + 8);
    }
}

//------------------------------------------------------------------------
// genEmitLoadLclTypeSimd12: Emit code to load a SIMD12 value from stack.
//
// Arguments:
//    tgtReg - Register to load into
//    lclNum - Stack local's number
//    offset - Offset to load from
//
void CodeGen::genEmitLoadLclTypeSimd12(regNumber tgtReg, unsigned lclNum, unsigned offset)
{
    emitter* emit = GetEmitter();

    if (compiler->compOpportunisticallyDependsOn(InstructionSet_SSE41))
    {
        // Load lower 8 bytes into tgtReg, preserving upper 4 bytes
        emit->emitIns_R_S(INS_movsd_simd, EA_8BYTE, tgtReg, lclNum, offset);

        // Load and insert upper 4 byte, 0x20 inserts to index 2 and 0x8 zeros index 3
        emit->emitIns_SIMD_R_R_S_I(INS_insertps, EA_16BYTE, tgtReg, tgtReg, lclNum, offset + 8, 0x28);
    }
    else
    {
        // Load upper 4 bytes to lower half of tgtReg
        emit->emitIns_R_S(INS_movss, EA_4BYTE, tgtReg, lclNum, offset + 8);

        // Move upper 4 bytes to upper half of tgtReg
        emit->emitIns_R_R(INS_movlhps, EA_16BYTE, tgtReg, tgtReg);

        // Load lower 8 bytes into tgtReg, preserving upper 4 bytes
        emit->emitIns_R_S(INS_movlps, EA_16BYTE, tgtReg, lclNum, offset);
    }
}

#ifdef TARGET_X86
//-----------------------------------------------------------------------------
// genStoreSimd12ToStack: store a TYP_SIMD12 (i.e. Vector3) type field to the stack.
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
void CodeGen::genStoreSimd12ToStack(regNumber dataReg, regNumber tmpReg)
{
    assert(genIsValidFloatReg(dataReg));
    assert(genIsValidFloatReg(tmpReg));

    emitter* emit = GetEmitter();

    // Store lower 8 bytes
    emit->emitIns_AR_R(INS_movsd_simd, EA_8BYTE, dataReg, REG_SPBASE, 0);

    // Extract upper 4 bytes from data
    emit->emitIns_R_R(INS_movhlps, EA_16BYTE, tmpReg, dataReg);

    // Store upper 4 bytes
    emit->emitIns_AR_R(INS_movss, EA_4BYTE, tmpReg, REG_SPBASE, 8);
}

//-----------------------------------------------------------------------------
// genPutArgStkSimd12: store a TYP_SIMD12 (i.e. Vector3) type field.
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
void CodeGen::genPutArgStkSimd12(GenTreePutArgStk* treeNode)
{
    assert(treeNode->OperIs(GT_PUTARG_STK));

    GenTree* data = treeNode->Data();
    assert(!data->isContained());

    regNumber dataReg = genConsumeReg(data);

    // Need an additional Xmm register to extract upper 4 bytes from data.
    regNumber tmpReg = treeNode->GetSingleTempReg();

    genStoreSimd12ToStack(dataReg, tmpReg);
}
#endif // TARGET_X86

//-----------------------------------------------------------------------------
// genSimdUpperSave: save the upper half of a TYP_SIMD32 vector to
//                   the given register, if any, or to memory.
//
// Arguments:
//    node - The GT_INTRINSIC node
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
void CodeGen::genSimdUpperSave(GenTreeIntrinsic* node)
{
    assert(node->gtIntrinsicName == NI_SIMD_UpperSave);

    GenTree* op1 = node->gtGetOp1();
    assert(op1->IsLocal() && op1->TypeIs(TYP_SIMD32, TYP_SIMD64));

    regNumber tgtReg = node->GetRegNum();
    regNumber op1Reg = genConsumeReg(op1);
    assert(op1Reg != REG_NA);

    if (tgtReg != REG_NA)
    {
        // We should never save to register for zmm.
        assert(op1->TypeGet() == TYP_SIMD32);
        GetEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, tgtReg, op1Reg, 0x01);
        genProduceReg(node);
    }
    else
    {
        // The localVar must have a stack home.

        unsigned   varNum = op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        assert(varDsc->lvOnFrame);

        if (op1->TypeGet() == TYP_SIMD32)
        {
            // We want to store this to the upper 16 bytes of this localVar's home.
            int offs = 16;

            GetEmitter()->emitIns_S_R_I(INS_vextractf128, EA_32BYTE, varNum, offs, op1Reg, 0x01);
        }
        else
        {
            assert(op1->TypeGet() == TYP_SIMD64);
            // We will save the whole 64 bytes for zmm.
            GetEmitter()->emitIns_S_R(INS_movups, EA_64BYTE, op1Reg, varNum, 0);
        }
    }
}

//-----------------------------------------------------------------------------
// genSimdUpperRestore: Restore the upper half of a TYP_SIMD32 vector to
//                      the given register, if any, or to memory.
//
// Arguments:
//    node - The GT_INTRINSIC node
//
// Return Value:
//    None.
//
// Notes:
//    For consistency with genSimdUpperSave, and to ensure that lclVar nodes always
//    have their home register, this node has its targetReg on the lclVar child, and its source
//    on the node.
//
void CodeGen::genSimdUpperRestore(GenTreeIntrinsic* node)
{
    assert(node->gtIntrinsicName == NI_SIMD_UpperRestore);

    GenTree* op1 = node->gtGetOp1();
    assert(op1->IsLocal() && op1->TypeIs(TYP_SIMD32, TYP_SIMD64));

    regNumber srcReg    = node->GetRegNum();
    regNumber lclVarReg = genConsumeReg(op1);
    assert(lclVarReg != REG_NA);

    if (srcReg != REG_NA)
    {
        // We should never save to register for zmm.
        assert(op1->TypeGet() == TYP_SIMD32);
        GetEmitter()->emitIns_R_R_R_I(INS_vinsertf128, EA_32BYTE, lclVarReg, lclVarReg, srcReg, 0x01);
    }
    else
    {
        // The localVar must have a stack home.
        unsigned   varNum = op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        assert(varDsc->lvOnFrame);
        if (op1->TypeGet() == TYP_SIMD32)
        {
            // We will load this from the upper 16 bytes of this localVar's home.
            int offs = 16;
            GetEmitter()->emitIns_R_R_S_I(INS_vinsertf128, EA_32BYTE, lclVarReg, lclVarReg, varNum, offs, 0x01);
        }
        else
        {
            assert(op1->TypeGet() == TYP_SIMD64);
            // We will restore the whole 64 bytes for zmm.
            GetEmitter()->emitIns_R_S(INS_movups, EA_64BYTE, lclVarReg, varNum, 0);
        }
    }
}

//-----------------------------------------------------------------------------
// genSimd12UpperClear: Clears the upper 32-bits of a TYP_SIMD12 vector
//
// Arguments:
//    tgtReg - The target register for which to clear the upper bits
//
// Return Value:
//    None.
//
void CodeGen::genSimd12UpperClear(regNumber tgtReg)
{
    assert(genIsValidFloatReg(tgtReg));

    if (compiler->compOpportunisticallyDependsOn(InstructionSet_SSE41))
    {
        // ZMASK:   0b1000 - Preserve element 0, 1, and 2; Zero element 3
        // COUNT_D: 0b11   - Insert into element 3
        // COUNT_S: 0b11   - Insert from element 3

        GetEmitter()->emitIns_SIMD_R_R_R_I(INS_insertps, EA_16BYTE, tgtReg, tgtReg, tgtReg, static_cast<int8_t>(0xF8));
    }
    else
    {
        // Preserve element 0, 1, and 2; Zero element 3

        if (zroSimd12Elm3 == NO_FIELD_HANDLE)
        {
            simd16_t constValue;

            constValue.u32[0] = 0xFFFFFFFF;
            constValue.u32[1] = 0xFFFFFFFF;
            constValue.u32[2] = 0xFFFFFFFF;
            constValue.u32[3] = 0x00000000;

            zroSimd12Elm3 = GetEmitter()->emitSimd16Const(constValue);
        }

        GetEmitter()->emitIns_SIMD_R_R_C(INS_andps, EA_16BYTE, tgtReg, tgtReg, zroSimd12Elm3, 0);
    }
}

#endif // FEATURE_SIMD
#endif // TARGET_XARCH
