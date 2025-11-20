// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"

void CodeGen::genGenerateCode(void** codePtr, uint32_t* nativeSizeOfCode)
{
    NYI_WASM("Undef genGenerateCode in codegencommon.cpp and proceed from there");
}

void CodeGen::genSpillVar(GenTree* tree)
{
    NYI_WASM("Put all spillng to memory under '#if HAS_FIXED_REGISTER_SET'");
}

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnEpilog()\n");
    }
#endif // DEBUG

    NYI_WASM("genFnEpilog");
}

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletProlog()\n");
    }
#endif

    NYI_WASM("genFuncletProlog");
}

void CodeGen::genFuncletEpilog()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletEpilog()\n");
    }
#endif

    NYI_WASM("genFuncletEpilog");
}

//---------------------------------------------------------------------
// genTotalFrameSize - return the total size of the linear memory stack frame.
//
// Return value:
//    Total linear memory frame size
//
int CodeGenInterface::genTotalFrameSize() const
{
    assert(compiler->compLclFrameSize >= 0);
    return compiler->compLclFrameSize;
}

//---------------------------------------------------------------------
// genSPtoFPdelta - return the offset from SP to the frame pointer.
// This number is going to be positive, since SP must be at the lowest
// address.
//
// There must be a frame pointer to call this function!
int CodeGenInterface::genSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    NYI_WASM("genSPtoFPdelta");
    return 0;
}

//---------------------------------------------------------------------
// genCallerSPtoFPdelta - return the offset from Caller-SP to the frame pointer.
// This number is going to be negative, since the Caller-SP is at a higher
// address than the frame pointer.
//
// There must be a frame pointer to call this function!
int CodeGenInterface::genCallerSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    NYI_WASM("genCallerSPtoFPdelta");
    return 0;
}

//---------------------------------------------------------------------
// genCallerSPtoInitialSPdelta - return the offset from Caller-SP to Initial SP.
//
// This number will be negative.
int CodeGenInterface::genCallerSPtoInitialSPdelta() const
{
    NYI_WASM("genCallerSPtoInitialSPdelta");
    return 0;
}

void CodeGenInterface::genUpdateVarReg(LclVarDsc* varDsc, GenTree* tree, int regIndex)
{
    NYI_WASM("Move genUpdateVarReg from codegenlinear.cpp to codegencommon.cpp shared code");
}

void CodeGenInterface::genUpdateVarReg(LclVarDsc* varDsc, GenTree* tree)
{
    NYI_WASM("Move genUpdateVarReg from codegenlinear.cpp to codegencommon.cpp shared code");
}

void RegSet::verifyRegUsed(regNumber reg)
{
}
