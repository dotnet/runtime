// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COMPILER_H_
#define _COMPILER_H_

#include "intops.h"

// Types that can exist on the IL execution stack. They are used only during
// IL import compilation stage.
enum StackType {
    StackTypeI4 = 0,
    StackTypeI8,
    StackTypeR4,
    StackTypeR8,
    StackTypeO,
    StackTypeVT,
    StackTypeMP,
    StackTypeF
};

// Types relevant for interpreter vars and opcodes. They are used in the final
// stages of the codegen and can be used during execution.
enum InterpType {
    InterpTypeI1 = 0,
    InterpTypeU1,
    InterpTypeI2,
    InterpTypeU2,
    InterpTypeI4,
    InterpTypeI8,
    InterpTypeR4,
    InterpTypeR8,
    InterpTypeO,
    InterpTypeVT,
    InterpTypeVOID,
#ifdef TARGET_64BIT
    InterpTypeI = InterpTypeI8
#else
    InterpTypeI = InterpTypeI4
#endif
};

struct InterpCallInfo
{
    // For call instructions, this represents an array of all call arg vars
    // in the order they are pushed to the stack. This makes it easy to find
    // all source vars for these types of opcodes. This is terminated with -1.
    int *pCallArgs;
    int callOffset;
};

struct InterpBasicBlock;

struct InterpInst
{
    InterpInst *pNext, *pPrev;
    union
    {
        InterpBasicBlock *pTargetBB; // target basic block for branch instructions
        InterpBasicBlock **ppTargetBBTable; // basic block table for switch instruction
        InterpCallInfo *pCallInfo; // additional information for call instructions 
    } info;

    int32_t opcode;
    int32_t ilOffset;
    uint32_t flags;
    int32_t dVar;
    int32_t sVars[3]; // Currently all instructions have at most 3 sregs

    int32_t data[];

    void SetDVar(int32_t dv)
    {
        dVar = dv;
    }

    void SetSVar(int32_t sv1)
    {
        sVars[0] = sv1;
    }

    void SetSVars2(int32_t sv1, int32_t sv2)
    {
        sVars[0] = sv1;
        sVars[1] = sv2;
    }

    void SetSVars3(int32_t sv1, int32_t sv2, int32_t sv3)
    {
        sVars[0] = sv1;
        sVars[1] = sv2;
        sVars[2] = sv3;
    }
};

#define CALL_ARGS_SVAR  -2


struct InterpBasicBlock
{
    int32_t index;
    int32_t ilOffset, nativeOffset;
    int32_t stackHeight;

    InterpInst *pFirstIns, *pLastIns;
    InterpBasicBlock *pNextBB;

    int inCount, outCount;
    InterpBasicBlock **ppInBBs;
    InterpBasicBlock **ppOutBBs;
};

struct InterpVar
{
    CORINFO_CLASS_HANDLE clsHnd;
    InterpType mt;
    int indirects;
    int offset;
    int size;
    // live_start and live_end are used by the offset allocator
    int liveStart;
    int liveEnd;
};

struct StackInfo
{
    StackType type;
    CORINFO_CLASS_HANDLE clsHnd;
    // Size that this value will occupy on the interpreter stack. It is a multiple
    // of INTERP_STACK_SLOT_SIZE
    int size;

    // The var associated with the value of this stack entry. Every time we push on
    // the stack a new var is created.
    int var;
};

typedef class ICorJitInfo* COMP_HANDLE;

class InterpCompiler
{
private:
    CORINFO_METHOD_HANDLE m_methodHnd;
    COMP_HANDLE m_compHnd;
    CORINFO_METHOD_INFO* m_methodInfo;

    int GenerateCode(CORINFO_METHOD_INFO* methodInfo);

    void* AllocMethodData(size_t numBytes);
    void* AllocMemPool(size_t numBytes);
    void* AllocTemporary(size_t numBytes);
    void* ReallocTemporary(void* ptr, size_t numBytes);
    void  FreeTemporary(void* ptr);

    // Instructions
    InterpBasicBlock *m_pCBB, *m_pEntryBB;
    InterpInst* m_pLastIns;

    int32_t     GetInsLength(InterpInst *pIns);
    bool        InsIsNop(InterpInst *pIns);
    InterpInst* AddIns(int opcode);
    InterpInst* NewIns(int opcode, int len);
    InterpInst* AddInsExplicit(int opcode, int dataLen);
    InterpInst* InsertInsBB(InterpBasicBlock *pBB, InterpInst *pPrevIns, int opcode);
    InterpInst* InsertIns(InterpInst *pPrevIns, int opcode);
    InterpInst* FirstRealIns(InterpBasicBlock *pBB);
    InterpInst* NextRealIns(InterpInst *pIns);
    InterpInst* PrevRealIns(InterpInst *pIns);
    void        ClearIns(InterpInst *pIns);

    void        ForEachInsSVar(InterpInst *ins, void *pData, void (InterpCompiler::*callback)(int*, void*));
    void        ForEachInsVar(InterpInst *ins, void *pData, void (InterpCompiler::*callback)(int*, void*));

    // Basic blocks
    int m_BBCount = 0;
    InterpBasicBlock**  m_ppOffsetToBB;

    InterpBasicBlock*   AllocBB();
    InterpBasicBlock*   GetBB(int32_t ilOffset);
    void                LinkBBs(InterpBasicBlock *from, InterpBasicBlock *to);
    void                UnlinkBBs(InterpBasicBlock *from, InterpBasicBlock *to);

    // Vars
    InterpVar *m_pVars = NULL;
    int32_t m_varsSize = 0;
    int32_t m_varsCapacity = 0;

    int32_t CreateVarExplicit(InterpType mt, CORINFO_CLASS_HANDLE clsHnd, int size);

    int32_t m_totalVarsStackSize = 0;
    int32_t m_paramAreaOffset = 0;
    void    AllocVarOffsetCB(int *pVar, void *pData);
    int32_t AllocVarOffset(int var, int32_t *pPos);


    // Stack
    StackInfo *m_pStackPointer, *m_pStackBase;
    int32_t m_stackCapacity;
    bool m_hasInvalidCode = false;

    bool CheckStackHelper(int n);
    void EnsureStack(int additional);
    void PushTypeExplicit(StackType stackType, CORINFO_CLASS_HANDLE clsHnd, int size);
    void PushType(StackType stackType, CORINFO_CLASS_HANDLE clsHnd);
    void PushTypeVT(CORINFO_CLASS_HANDLE clsHnd, int size);

    // Passes
    int32_t* m_pMethodCode;
    int32_t m_MethodCodeSize; // in int32_t

    void AllocOffsets();
    int32_t ComputeCodeSize();
    void EmitCode();
    int32_t* EmitCodeIns(int32_t *ip, InterpInst *pIns);
    InterpMethod* CreateInterpMethod();
public:

    InterpCompiler(COMP_HANDLE compHnd, CORINFO_METHOD_INFO* methodInfo);

    InterpMethod* CompileMethod();

    int32_t* GetCode(int32_t *pCodeSize);
};

#endif //_COMPILER_H_
