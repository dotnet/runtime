// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX               Wasm hardware intrinsic Code Generator                      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef FEATURE_HW_INTRINSICS

#include "codegen.h"

// genHWIntrinsic: Generates the code for a given hardware intrinsic node.
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genHWIntrinsic(GenTreeHWIntrinsic* node)
{
    // emitIns_v128_Imm
    // emitIns_Lane
    // emitIns_Memarg_Lane

    const HWIntrinsic info(node);
    genConsumeMultiOpOperands(node);

    if (info.codeGenIsTableDriven())
    {
        instruction const ins = HWIntrinsicInfo::lookupIns(info.id, info.baseType, m_compiler);
        assert(ins != INS_invalid);
        if (info.category == HW_Category_SIMD)
        {
            GetEmitter()->emitIns(ins);
        }
        else
        {
            NYI_WASM_SIMD("!HW_Category_SIMD");
        }
    }
    else
    {
        NYI_WASM_SIMD("!codeGenIsTableDriven");
    }

    WasmProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
