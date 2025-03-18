// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COMPILER_H_
#define _COMPILER_H_

#include "intops.h"
#include "datastructs.h"

// Types that can exist on the IL execution stack. They are used only during
// IL import compilation stage.
enum StackType {
    StackTypeI4 = 0,
    StackTypeI8,
    StackTypeR4,
    StackTypeR8,
    StackTypeO,
    StackTypeVT,
    StackTypeByRef,
    StackTypeF,
#ifdef TARGET_64BIT
    StackTypeI = StackTypeI8
#else
    StackTypeI = StackTypeI4
#endif
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
    InterpTypeByRef,
    InterpTypeVoid,
#ifdef TARGET_64BIT
    InterpTypeI = InterpTypeI8
#else
    InterpTypeI = InterpTypeI4
#endif
};

#ifdef DEBUG
#define INTERP_DUMP(...)            \
    {                               \
        if (m_verbose)              \
            printf(__VA_ARGS__);    \
    }
#else
#define INTERP_DUMP(...)
#endif

struct InterpInst;
struct InterpBasicBlock;

struct InterpCallInfo
{
    // For call instructions, this represents an array of all call arg vars
    // in the order they are pushed to the stack. This makes it easy to find
    // all source vars for these types of opcodes. This is terminated with -1.
    int32_t *pCallArgs;
    int32_t callOffset;
    union {
        // Array of call dependencies that need to be resolved before
        TSList<InterpInst*> *callDeps;
        // Stack end offset of call arguments
        int32_t callEndOffset;
    };
};

enum InterpInstFlags
{
    INTERP_INST_FLAG_CALL               = 0x01,
    // Flag used internally by the var offset allocator
    INTERP_INST_FLAG_ACTIVE_CALL        = 0x02
};

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
#define CALL_ARGS_TERMINATOR -1

struct StackInfo;

enum InterpBBState
{
    BBStateNotEmitted,
    BBStateEmitting,
    BBStateEmitted
};

struct InterpBasicBlock
{
    int32_t index;
    int32_t ilOffset, nativeOffset;
    int32_t stackHeight;
    StackInfo *pStackState;

    InterpInst *pFirstIns, *pLastIns;
    InterpBasicBlock *pNextBB;

    int inCount, outCount;
    InterpBasicBlock **ppInBBs;
    InterpBasicBlock **ppOutBBs;

    InterpBBState emitState;

    InterpBasicBlock(int32_t index) : InterpBasicBlock(index, 0) { }

    InterpBasicBlock(int32_t index, int32_t ilOffset)
    {
        this->index = index;
        this->ilOffset = ilOffset;
        nativeOffset = -1;
        stackHeight = -1;

        pFirstIns = pLastIns = NULL;
        pNextBB = NULL;

        inCount = 0;
        outCount = 0;

        emitState = BBStateNotEmitted;
    }
};

struct InterpVar
{
    CORINFO_CLASS_HANDLE clsHnd;
    InterpType interpType;
    int indirects;
    int offset;
    int size;
    // live_start and live_end are used by the offset allocator
    int liveStart;
    int liveEnd;
    // index of first basic block where this var is used
    int bbIndex;
    // If var is callArgs, this is the call instruction using it.
    // Only used by the var offset allocator
    InterpInst *call;

    unsigned int callArgs : 1; // Var used as argument to a call
    unsigned int noCallArgs : 1; // Var can't be used as argument to a call, needs to be copied to temp
    unsigned int global : 1; // Dedicated stack offset throughout method execution
    unsigned int ILGlobal : 1; // Args and IL locals
    unsigned int alive : 1; // Used internally by the var offset allocator

    InterpVar(InterpType interpType, CORINFO_CLASS_HANDLE clsHnd, int size)
    {
        this->interpType = interpType;
        this->clsHnd = clsHnd;
        this->size = size;
        offset = -1;
        liveStart = -1;
        bbIndex = -1;
        indirects = 0;

        callArgs = false;
        noCallArgs = false;
        global = false;
        ILGlobal = false;
        alive = false;
    }
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

    StackInfo(StackType type)
    {
        this->type = type;
        clsHnd = NULL;
        size = 0;
        var = -1;
    }
};

enum RelocType
{
    RelocLongBranch,
    RelocSwitch
};

struct Reloc
{
    RelocType type;
    // For branch relocation, how many sVar slots to skip
    int skip;
    // Base offset that the relative offset to be embedded in IR applies to
    int32_t offset;
    InterpBasicBlock *pTargetBB;

    Reloc(RelocType type, int32_t offset, InterpBasicBlock *pTargetBB, int skip)
    {
        this->type = type;
        this->offset = offset;
        this->pTargetBB = pTargetBB;
        this->skip = skip;
    }
};

typedef class ICorJitInfo* COMP_HANDLE;

class InterpCompiler
{
private:
    CORINFO_METHOD_HANDLE m_methodHnd;
    CORINFO_MODULE_HANDLE m_compScopeHnd;
    COMP_HANDLE m_compHnd;
    CORINFO_METHOD_INFO* m_methodInfo;
    bool m_verbose;

    static int32_t InterpGetMovForType(InterpType interpType, bool signExtend);

    uint8_t* m_ip;
    uint8_t* m_pILCode;
    int32_t m_ILCodeSize;
    int32_t m_currentILOffset;

    // This represents a mapping from indexes to pointer sized data. During compilation, an
    // instruction can request an index for some data (like a MethodDesc pointer), that it
    // will then embed in the instruction stream. The data item table will be referenced
    // from the interpreter code header during execution.
    // FIXME during compilation this should be a hashtable for fast lookup of duplicates
    TArray<void*> m_dataItems;
    int32_t GetDataItemIndex(void* data);
    int32_t GetMethodDataItemIndex(CORINFO_METHOD_HANDLE mHandle);

    int GenerateCode(CORINFO_METHOD_INFO* methodInfo);

    void* AllocMethodData(size_t numBytes);
    // FIXME Mempool allocation currently leaks. We need to add an allocator and then
    // free all memory when method is finished compilling.
    void* AllocMemPool(size_t numBytes);
    void* AllocMemPool0(size_t numBytes);
    void* AllocTemporary(size_t numBytes);
    void* AllocTemporary0(size_t numBytes);
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

    InterpBasicBlock*   AllocBB(int32_t ilOffset);
    InterpBasicBlock*   GetBB(int32_t ilOffset);
    void                LinkBBs(InterpBasicBlock *from, InterpBasicBlock *to);
    void                UnlinkBBs(InterpBasicBlock *from, InterpBasicBlock *to);

    void    EmitBranch(InterpOpcode opcode, int ilOffset);
    void    EmitOneArgBranch(InterpOpcode opcode, int ilOffset, int insSize);
    void    EmitTwoArgBranch(InterpOpcode opcode, int ilOffset, int insSize);

    void    EmitBBEndVarMoves(InterpBasicBlock *pTargetBB);
    void    InitBBStackState(InterpBasicBlock *pBB);
    void    UnlinkUnreachableBBlocks();

    // Vars
    InterpVar *m_pVars = NULL;
    int32_t m_varsSize = 0;
    int32_t m_varsCapacity = 0;

    int32_t CreateVarExplicit(InterpType interpType, CORINFO_CLASS_HANDLE clsHnd, int size);

    int32_t m_totalVarsStackSize;
    int32_t m_paramAreaOffset = 0;
    int32_t m_ILLocalsOffset, m_ILLocalsSize;
    void    AllocVarOffsetCB(int *pVar, void *pData);
    int32_t AllocVarOffset(int var, int32_t *pPos);

    int32_t GetInterpTypeStackSize(CORINFO_CLASS_HANDLE clsHnd, InterpType interpType, int32_t *pAlign);
    void    CreateILVars();

    // Stack
    StackInfo *m_pStackPointer, *m_pStackBase;
    int32_t m_stackCapacity;
    bool m_hasInvalidCode = false;

    bool CheckStackHelper(int n);
    void EnsureStack(int additional);
    void PushTypeExplicit(StackType stackType, CORINFO_CLASS_HANDLE clsHnd, int size);
    void PushStackType(StackType stackType, CORINFO_CLASS_HANDLE clsHnd);
    void PushInterpType(InterpType interpType, CORINFO_CLASS_HANDLE clsHnd);
    void PushTypeVT(CORINFO_CLASS_HANDLE clsHnd, int size);

    // Code emit
    void    EmitConv(StackInfo *sp, InterpInst *prevIns, StackType type, InterpOpcode convOp);
    void    EmitLoadVar(int var);
    void    EmitStoreVar(int var);
    void    EmitBinaryArithmeticOp(int32_t opBase);
    void    EmitUnaryArithmeticOp(int32_t opBase);
    void    EmitShiftOp(int32_t opBase);
    void    EmitCompareOp(int32_t opBase);
    void    EmitCall(CORINFO_CLASS_HANDLE constrainedClass, bool readonly, bool tailcall);
    bool    EmitCallIntrinsics(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO sig);

    // Var Offset allocator
    TArray<InterpInst*> *m_pActiveCalls;
    TArray<int32_t> *m_pActiveVars;
    TSList<InterpInst*> *m_pDeferredCalls;

    int32_t AllocGlobalVarOffset(int var);
    void    SetVarLiveRange(int32_t var, int insIndex);
    void    SetVarLiveRangeCB(int32_t *pVar, void *pData);
    void    InitializeGlobalVar(int32_t var, int bbIndex);
    void    InitializeGlobalVarCB(int32_t *pVar, void *pData);
    void    InitializeGlobalVars();
    void    EndActiveCall(InterpInst *call);
    void    CompactActiveVars(int32_t *current_offset);

    // Passes
    int32_t* m_pMethodCode;
    int32_t m_methodCodeSize; // code size measured in int32_t slots, instead of bytes

    void AllocOffsets();
    int32_t ComputeCodeSize();
    void EmitCode();
    int32_t* EmitCodeIns(int32_t *ip, InterpInst *pIns, TArray<Reloc*> *relocs);
    void PatchRelocations(TArray<Reloc*> *relocs);
    InterpMethod* CreateInterpMethod();
    bool CreateBasicBlocks(CORINFO_METHOD_INFO* methodInfo);

    // Debug
    void PrintClassName(CORINFO_CLASS_HANDLE cls);
    void PrintMethodName(CORINFO_METHOD_HANDLE method);
    void PrintCode();
    void PrintBBCode(InterpBasicBlock *pBB);
    void PrintIns(InterpInst *ins);
    void PrintInsData(InterpInst *ins, int32_t offset, const int32_t *pData, int32_t opcode);
    void PrintCompiledCode();
    void PrintCompiledIns(const int32_t *ip, const int32_t *start);
public:

    InterpCompiler(COMP_HANDLE compHnd, CORINFO_METHOD_INFO* methodInfo, bool verbose);

    InterpMethod* CompileMethod();

    int32_t* GetCode(int32_t *pCodeSize);
};

#endif //_COMPILER_H_
