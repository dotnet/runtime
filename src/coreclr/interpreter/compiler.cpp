// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "interpreter.h"

static const StackType g_stackTypeFromInterpType[] =
{
    StackTypeI4, // I1
    StackTypeI4, // U1
    StackTypeI4, // I2
    StackTypeI4, // U2
    StackTypeI4, // I4
    StackTypeI8, // I8
    StackTypeR4, // R4
    StackTypeR8, // R8
    StackTypeO,  // O
    StackTypeVT, // VT
    StackTypeByRef, // ByRef
};

static const InterpType g_interpTypeFromStackType[] =
{
    InterpTypeI4,       // I4,
    InterpTypeI8,       // I8,
    InterpTypeR4,       // R4,
    InterpTypeR8,       // R8,
    InterpTypeO,        // O,
    InterpTypeVT,       // VT,
    InterpTypeByRef,    // MP,
    InterpTypeI,        // F
};

static const char *g_stackTypeString[] = { "I4", "I8", "R4", "R8", "O ", "VT", "MP", "F " };

// FIXME Use specific allocators for their intended purpose
// Allocator for data that is kept alive throughout application execution,
// being freed only if the associated method gets freed.
void* InterpCompiler::AllocMethodData(size_t numBytes)
{
    return malloc(numBytes);
}

// Fast allocator for small chunks of memory that can be freed together when the
// method compilation is finished.
void* InterpCompiler::AllocMemPool(size_t numBytes)
{
    return malloc(numBytes);
}

void* InterpCompiler::AllocMemPool0(size_t numBytes)
{
    void *ptr = AllocMemPool(numBytes);
    memset(ptr, 0, numBytes);
    return ptr;
}

// Allocator for potentially larger chunks of data, that we might want to free
// eagerly, before method is finished compiling, to prevent excessive memory usage.
void* InterpCompiler::AllocTemporary(size_t numBytes)
{
    return malloc(numBytes);
}

void* InterpCompiler::AllocTemporary0(size_t numBytes)
{
    void *ptr = AllocTemporary(numBytes);
    memset(ptr, 0, numBytes);
    return ptr;
}

void* InterpCompiler::ReallocTemporary(void* ptr, size_t numBytes)
{
    return realloc(ptr, numBytes);
}

void InterpCompiler::FreeTemporary(void* ptr)
{
    free(ptr);
}

static int GetDataLen(int opcode)
{
    int length = g_interpOpLen[opcode];
    int numSVars = g_interpOpSVars[opcode];
    int numDVars = g_interpOpDVars[opcode];

    return length - 1 - numSVars - numDVars;
}

InterpInst* InterpCompiler::AddIns(int opcode)
{
    return AddInsExplicit(opcode, GetDataLen(opcode));
}

InterpInst* InterpCompiler::AddInsExplicit(int opcode, int dataLen)
{
    InterpInst *ins = NewIns(opcode, dataLen);
    ins->pPrev = m_pCBB->pLastIns;
    if (m_pCBB->pLastIns)
        m_pCBB->pLastIns->pNext = ins;
    else
        m_pCBB->pFirstIns = ins;
    m_pCBB->pLastIns = ins;
    return ins;
}

InterpInst* InterpCompiler::NewIns(int opcode, int dataLen)
{
    int insSize = sizeof(InterpInst) + sizeof(uint32_t) * dataLen;
    InterpInst *ins = (InterpInst*)AllocMemPool(insSize);
    memset(ins, 0, insSize);
    ins->opcode = opcode;
    ins->ilOffset = m_currentILOffset;
    m_pLastIns = ins;
    return ins;
}

InterpInst* InterpCompiler::InsertInsBB(InterpBasicBlock *pBB, InterpInst *pPrevIns, int opcode)
{
    InterpInst *ins = NewIns(opcode, GetDataLen(opcode));

    ins->pPrev = pPrevIns;

    if (pPrevIns)
    {
        ins->pNext = pPrevIns->pNext;
        pPrevIns->pNext = ins;
    }
    else
    {
        ins->pNext = pBB->pFirstIns;
        pBB->pFirstIns = ins;
    }

    if (ins->pNext == NULL)
    {
        pBB->pLastIns = ins;
    }
    else
    {
        ins->pNext->pPrev = ins;
    }

    return ins;
}

// Inserts a new instruction after prevIns. prevIns must be in cbb
InterpInst* InterpCompiler::InsertIns(InterpInst *pPrevIns, int opcode)
{
    return InsertInsBB(m_pCBB, pPrevIns, opcode);
}

InterpInst* InterpCompiler::FirstRealIns(InterpBasicBlock *pBB)
{
    InterpInst *ins = pBB->pFirstIns;
    if (!ins || !InsIsNop(ins))
        return ins;
    while (ins && InsIsNop(ins))
        ins = ins->pNext;
    return ins;
}

InterpInst* InterpCompiler::NextRealIns(InterpInst *ins)
{
    ins = ins->pNext;
    while (ins && InsIsNop(ins))
        ins = ins->pNext;
    return ins;
}

InterpInst* InterpCompiler::PrevRealIns(InterpInst *ins)
{
    ins = ins->pPrev;
    while (ins && InsIsNop(ins))
        ins = ins->pPrev;
    return ins;
}

void InterpCompiler::ClearIns(InterpInst *ins)
{
    ins->opcode = INTOP_NOP;
}

bool InterpCompiler::InsIsNop(InterpInst *ins)
{
    return ins->opcode == INTOP_NOP;
}

int32_t InterpCompiler::GetInsLength(InterpInst *ins)
{
    int len = g_interpOpLen[ins->opcode];
    if (len == 0)
    {
        assert(ins->opcode == INTOP_SWITCH);
        len = 3 + ins->data[0];
    }

    return len;
}

void InterpCompiler::ForEachInsSVar(InterpInst *ins, void *pData, void (InterpCompiler::*callback)(int*, void*))
{
    int numSVars = g_interpOpSVars[ins->opcode];
    if (numSVars)
    {
        for (int i = 0; i < numSVars; i++)
        {
            if (ins->sVars [i] == CALL_ARGS_SVAR)
            {
                if (ins->info.pCallInfo && ins->info.pCallInfo->pCallArgs) {
                    int *callArgs = ins->info.pCallInfo->pCallArgs;
                    while (*callArgs != CALL_ARGS_TERMINATOR)
                    {
                        (this->*callback) (callArgs, pData);
                        callArgs++;
                    }
                }
            }
            else
            {
                (this->*callback) (&ins->sVars[i], pData);
            }
        }
    }
}

void InterpCompiler::ForEachInsVar(InterpInst *ins, void *pData, void (InterpCompiler::*callback)(int*, void*))
{
    ForEachInsSVar(ins, pData, callback);

    if (g_interpOpDVars [ins->opcode])
        (this->*callback) (&ins->dVar, pData);
}


InterpBasicBlock* InterpCompiler::AllocBB(int32_t ilOffset)
{
    InterpBasicBlock *bb = (InterpBasicBlock*)AllocMemPool(sizeof(InterpBasicBlock));

    new (bb) InterpBasicBlock (m_BBCount, ilOffset);
    m_BBCount++;
    return bb;
}

InterpBasicBlock* InterpCompiler::GetBB(int32_t ilOffset)
{
    InterpBasicBlock *bb = m_ppOffsetToBB [ilOffset];

    if (!bb)
    {
        bb = AllocBB(ilOffset);

        m_ppOffsetToBB[ilOffset] = bb;
    }

    return bb;
}

// Same implementation as JIT
static inline uint32_t LeadingZeroCount(uint32_t value)
{
    if (value == 0)
    {
        return 32;
    }

#if defined(_MSC_VER)
    unsigned long result;
    ::_BitScanReverse(&result, value);
    return 31 ^ static_cast<uint32_t>(result);
#else
    int32_t result = __builtin_clz(value);
    return static_cast<uint32_t>(result);
#endif
}


int GetBBLinksCapacity(int links)
{
    if (links <= 2)
        return links;
    // Return the next power of 2 bigger or equal to links
    uint32_t leadingZeroes = LeadingZeroCount(links - 1);
    return 1 << (32 - leadingZeroes);
}


void InterpCompiler::LinkBBs(InterpBasicBlock *from, InterpBasicBlock *to)
{
    int i;
    bool found = false;

    for (i = 0; i < from->outCount; i++)
    {
        if (to == from->ppOutBBs[i])
        {
            found = true;
            break;
        }
    }
    if (!found)
    {
        int prevCapacity = GetBBLinksCapacity(from->outCount);
        int newCapacity = GetBBLinksCapacity(from->outCount + 1);
        if (newCapacity > prevCapacity)
        {
            InterpBasicBlock **newa = (InterpBasicBlock**)AllocMemPool(newCapacity * sizeof(InterpBasicBlock*));
            memcpy(newa, from->ppOutBBs, from->outCount * sizeof(InterpBasicBlock*));
            from->ppOutBBs = newa;
        }
        from->ppOutBBs [from->outCount] = to;
        from->outCount++;
    }

    found = false;
    for (i = 0; i < to->inCount; i++)
    {
        if (from == to->ppInBBs [i])
        {
            found = true;
            break;
        }
    }

    if (!found) {
        int prevCapacity = GetBBLinksCapacity(to->inCount);
        int newCapacity = GetBBLinksCapacity(to->inCount + 1);
        if (newCapacity > prevCapacity) {
            InterpBasicBlock **newa = (InterpBasicBlock**)AllocMemPool(newCapacity * sizeof(InterpBasicBlock*));
            memcpy(newa, to->ppInBBs, to->inCount * sizeof(InterpBasicBlock*));
            to->ppInBBs = newa;
        }
        to->ppInBBs [to->inCount] = from;
        to->inCount++;
    }
}

// array must contain ref
static void RemoveBBRef(InterpBasicBlock **array, InterpBasicBlock *ref, int len)
{
    int i = 0;
    while (array[i] != ref)
    {
        i++;
    }
    i++;
    while (i < len)
    {
        array[i - 1] = array[i];
        i++;
    }
}

void InterpCompiler::UnlinkBBs(InterpBasicBlock *from, InterpBasicBlock *to)
{
    RemoveBBRef(from->ppOutBBs, to, from->outCount);
    from->outCount--;
    RemoveBBRef(to->ppInBBs, from, to->inCount);
    to->inCount--;
}

// These are moves between vars, operating only on the interpreter stack
int32_t InterpCompiler::InterpGetMovForType(InterpType interpType, bool signExtend)
{
    switch (interpType)
    {
        case InterpTypeI1:
        case InterpTypeU1:
        case InterpTypeI2:
        case InterpTypeU2:
            if (signExtend)
                return INTOP_MOV_I4_I1 + interpType;
            else
                return INTOP_MOV_4;
        case InterpTypeI4:
        case InterpTypeR4:
            return INTOP_MOV_4;
        case InterpTypeI8:
        case InterpTypeR8:
            return INTOP_MOV_8;
        case InterpTypeO:
        case InterpTypeByRef:
            return INTOP_MOV_P;
        case InterpTypeVT:
            return INTOP_MOV_VT;
        default:
            assert(0);
    }
    return -1;
}

// This method needs to be called when the current basic blocks ends and execution can
// continue into pTargetBB. When the stack state of a basic block is initialized, the vars
// associated with the stack state are set. When another bblock will continue execution
// into this bblock, it will first have to emit moves from the vars in its stack state
// to the vars of the target bblock stack state.
void InterpCompiler::EmitBBEndVarMoves(InterpBasicBlock *pTargetBB)
{
    if (pTargetBB->stackHeight <= 0)
        return;

    for (int i = 0; i < pTargetBB->stackHeight; i++)
    {
        int sVar = m_pStackPointer[i].var;
        int dVar = pTargetBB->pStackState[i].var;
        if (sVar != dVar)
        {
            InterpType interpType = m_pVars[sVar].interpType;
            int32_t movOp = InterpGetMovForType(interpType, false);

            AddIns(movOp);
            m_pLastIns->SetSVar(m_pStackPointer[i].var);
            m_pLastIns->SetDVar(pTargetBB->pStackState[i].var);

            if (interpType == InterpTypeVT)
            {
                assert(m_pVars[sVar].size == m_pVars[dVar].size);
                m_pLastIns->data[0] = m_pVars[sVar].size;
            }
        }
    }
}

static void MergeStackTypeInfo(StackInfo *pState1, StackInfo *pState2, int len)
{
    // Discard type information if we have type conflicts for stack contents
    for (int i = 0; i < len; i++)
    {
        if (pState1[i].clsHnd != pState2[i].clsHnd)
        {
            pState1[i].clsHnd = NULL;
            pState2[i].clsHnd = NULL;
        }
    }
}

// Initializes stack state at entry to bb, based on the current stack state
void InterpCompiler::InitBBStackState(InterpBasicBlock *pBB)
{
    if (pBB->stackHeight >= 0)
    {
        // Already initialized, update stack information
        MergeStackTypeInfo(m_pStackBase, pBB->pStackState, pBB->stackHeight);
    }
    else
    {
        pBB->stackHeight = (int32_t)(m_pStackPointer - m_pStackBase);
        if (pBB->stackHeight > 0)
        {
            int size = pBB->stackHeight * sizeof (StackInfo);
            pBB->pStackState = (StackInfo*)AllocMemPool(size);
            memcpy (pBB->pStackState, m_pStackBase, size);
        }
    }
}


int32_t InterpCompiler::CreateVarExplicit(InterpType interpType, CORINFO_CLASS_HANDLE clsHnd, int size)
{
    if (m_varsSize == m_varsCapacity) {
        m_varsCapacity *= 2;
        if (m_varsCapacity == 0)
            m_varsCapacity = 16;
        m_pVars = (InterpVar*) ReallocTemporary(m_pVars, m_varsCapacity * sizeof(InterpVar));
    }
    InterpVar *var = &m_pVars[m_varsSize];

    new (var) InterpVar(interpType, clsHnd, size);

    m_varsSize++;
    return m_varsSize - 1;
}

void InterpCompiler::EnsureStack(int additional)
{
    int32_t currentSize = (int32_t)(m_pStackPointer - m_pStackBase);

    if ((additional + currentSize) > m_stackCapacity) {
        m_stackCapacity *= 2;
        m_pStackBase = (StackInfo*)ReallocTemporary (m_pStackBase, m_stackCapacity * sizeof(StackInfo));
        m_pStackPointer = m_pStackBase + currentSize;
    }
}

#define CHECK_STACK(n)                      \
    do                                      \
    {                                       \
        if (!CheckStackHelper (n))          \
            goto exit_bad_code;             \
    } while (0)

#define CHECK_STACK_RET_VOID(n)             \
    do {                                    \
        if (!CheckStackHelper(n))           \
            return;                         \
    } while (0)

#define CHECK_STACK_RET(n, ret)             \
    do {                                    \
        if (!CheckStackHelper(n))           \
            return ret;                     \
    } while (0)

#define INVALID_CODE_RET_VOID               \
    do {                                    \
        m_hasInvalidCode = true;            \
        return;                             \
    } while (0)

bool InterpCompiler::CheckStackHelper(int n)
{
    int32_t currentSize = (int32_t)(m_pStackPointer - m_pStackBase);
    if (currentSize < n)
    {
        m_hasInvalidCode = true;
        return false;
    }
    return true;
}

void InterpCompiler::PushTypeExplicit(StackType stackType, CORINFO_CLASS_HANDLE clsHnd, int size)
{
    EnsureStack(1);
    m_pStackPointer->type = stackType;
    m_pStackPointer->clsHnd = clsHnd;
    m_pStackPointer->size = ALIGN_UP_TO(size, INTERP_STACK_SLOT_SIZE);
    int var = CreateVarExplicit(g_interpTypeFromStackType[stackType], clsHnd, size);
    m_pStackPointer->var = var;
    m_pStackPointer++;
}

void InterpCompiler::PushStackType(StackType stackType, CORINFO_CLASS_HANDLE clsHnd)
{
    // We don't really care about the exact size for non-valuetypes
    PushTypeExplicit(stackType, clsHnd, INTERP_STACK_SLOT_SIZE);
}

void InterpCompiler::PushInterpType(InterpType interpType, CORINFO_CLASS_HANDLE clsHnd)
{
    PushStackType(g_stackTypeFromInterpType[interpType], clsHnd);
}

void InterpCompiler::PushTypeVT(CORINFO_CLASS_HANDLE clsHnd, int size)
{
    PushTypeExplicit(StackTypeVT, clsHnd, size);
}


int32_t InterpCompiler::ComputeCodeSize()
{
    int32_t codeSize = 0;

    for (InterpBasicBlock *bb = m_pEntryBB; bb != NULL; bb = bb->pNextBB)
    {
        for (InterpInst *ins = bb->pFirstIns; ins != NULL; ins = ins->pNext)
        {
            codeSize += GetInsLength(ins);
        }
    }
    return codeSize;
}

int32_t* InterpCompiler::EmitCodeIns(int32_t *ip, InterpInst *ins, TArray<Reloc*> *relocs)
{
    int32_t opcode = ins->opcode;
    int32_t *startIp = ip;

    *ip++ = opcode;

    if (opcode == INTOP_SWITCH)
    {
        int32_t numLabels = ins->data [0];
        *ip++ = m_pVars[ins->sVars[0]].offset;
        *ip++ = numLabels;
        // Add relocation for each label
        for (int32_t i = 0; i < numLabels; i++)
        {
            Reloc *reloc = (Reloc*)AllocMemPool(sizeof(Reloc));
            new (reloc) Reloc(RelocSwitch, (int32_t)(ip - m_pMethodCode), ins->info.ppTargetBBTable[i], 0);
            relocs->Add(reloc);
            *ip++ = (int32_t)0xdeadbeef;
        }
    }
    else if (InterpOpIsUncondBranch(opcode) || InterpOpIsCondBranch(opcode))
    {
        int32_t brBaseOffset = (int32_t)(startIp - m_pMethodCode);
        for (int i = 0; i < g_interpOpSVars[opcode]; i++)
            *ip++ = m_pVars[ins->sVars[i]].offset;

        if (ins->info.pTargetBB->nativeOffset >= 0)
        {
            *ip++ = ins->info.pTargetBB->nativeOffset - brBaseOffset;
        }
        else if (opcode == INTOP_BR && ins->info.pTargetBB == m_pCBB->pNextBB)
        {
            // Ignore branch to the next basic block. Revert the added INTOP_BR.
            ip--;
        }
        else
        {
            // We don't know yet the IR offset of the target, add a reloc instead
            Reloc *reloc = (Reloc*)AllocMemPool(sizeof(Reloc));
            new (reloc) Reloc(RelocLongBranch, brBaseOffset, ins->info.pTargetBB, g_interpOpSVars[opcode]);
            relocs->Add(reloc);
            *ip++ = (int32_t)0xdeadbeef;
        }
    }
    else
    {
        // Default code emit for an instruction. The opcode was already emitted above.
        // We emit the offset for the instruction destination, then for every single source
        // variable we emit another offset. Finally, we will emit any additional data needed
        // by the instruction.
        if (g_interpOpDVars[opcode])
            *ip++ = m_pVars[ins->dVar].offset;

        if (g_interpOpSVars[opcode])
        {
            for (int i = 0; i < g_interpOpSVars[opcode]; i++)
            {
                if (ins->sVars[i] == CALL_ARGS_SVAR)
                {
                    *ip++ = m_paramAreaOffset + ins->info.pCallInfo->callOffset;
                }
                else
                {
                    *ip++ = m_pVars[ins->sVars[i]].offset;
                }
            }
        }

        int left = GetInsLength(ins) - (int32_t)(ip - startIp);
        // Emit the rest of the data
        for (int i = 0; i < left; i++)
            *ip++ = ins->data[i];
    }

    return ip;
}

void InterpCompiler::PatchRelocations(TArray<Reloc*> *relocs)
{
    int32_t size = relocs->GetSize();

    for (int32_t i = 0; i < size; i++)
    {
        Reloc *reloc = relocs->Get(i);
        int32_t offset = reloc->pTargetBB->nativeOffset - reloc->offset;
        int32_t *pSlot = NULL;

        if (reloc->type == RelocLongBranch)
            pSlot = m_pMethodCode + reloc->offset + reloc->skip + 1;
        else if (reloc->type == RelocSwitch)
            pSlot = m_pMethodCode + reloc->offset;
        else
            assert(0);

        assert(*pSlot == (int32_t)0xdeadbeef);
        *pSlot = offset;
    }
}

void InterpCompiler::EmitCode()
{
    TArray<Reloc*> relocs;
    int32_t codeSize = ComputeCodeSize();
    m_pMethodCode = (int32_t*)AllocMethodData(codeSize * sizeof(int32_t));

    int32_t *ip = m_pMethodCode;
    for (InterpBasicBlock *bb = m_pEntryBB; bb != NULL; bb = bb->pNextBB)
    {
        bb->nativeOffset = (int32_t)(ip - m_pMethodCode);
        m_pCBB = bb;
        for (InterpInst *ins = bb->pFirstIns; ins != NULL; ins = ins->pNext)
        {
            ip = EmitCodeIns(ip, ins, &relocs);
        }
    }

    m_methodCodeSize = (int32_t)(ip - m_pMethodCode);

    PatchRelocations(&relocs);
}

InterpMethod* InterpCompiler::CreateInterpMethod()
{
    int numDataItems = m_dataItems.GetSize();
    void **pDataItems = (void**)AllocMethodData(numDataItems * sizeof(void*));

    for (int i = 0; i < numDataItems; i++)
        pDataItems[i] = m_dataItems.Get(i);

    InterpMethod *pMethod = new InterpMethod(m_methodHnd, m_totalVarsStackSize, pDataItems);

    return pMethod;
}

int32_t* InterpCompiler::GetCode(int32_t *pCodeSize)
{
    *pCodeSize = m_methodCodeSize;
    return m_pMethodCode;
}

InterpCompiler::InterpCompiler(COMP_HANDLE compHnd,
                                CORINFO_METHOD_INFO* methodInfo,
                                bool verbose)
{
    m_methodHnd = methodInfo->ftn;
    m_compScopeHnd = methodInfo->scope;
    m_compHnd = compHnd;
    m_methodInfo = methodInfo;
    m_verbose = verbose;
}

InterpMethod* InterpCompiler::CompileMethod()
{
#ifdef DEBUG
    if (m_verbose)
    {
        printf("Interpreter compile method ");
        PrintMethodName(m_methodHnd);
        printf("\n");
    }
#endif

    CreateILVars();

    GenerateCode(m_methodInfo);

#ifdef DEBUG
    if (m_verbose)
    {
        printf("\nUnoptimized IR:\n");
        PrintCode();
    }
#endif

    AllocOffsets();

    EmitCode();

#ifdef DEBUG
    if (m_verbose)
    {
        printf("\nCompiled method: ");
        PrintMethodName(m_methodHnd);
        printf("\nLocals size %d\n", m_totalVarsStackSize);
        PrintCompiledCode();
        printf("\n");
    }
#endif

    return CreateInterpMethod();
}

// Adds a conversion instruction for the value pointed to by sp, also updating the stack information
void InterpCompiler::EmitConv(StackInfo *sp, InterpInst *prevIns, StackType type, InterpOpcode convOp)
{
    InterpInst *newInst;
    if (prevIns)
        newInst = InsertIns(prevIns, convOp);
    else
        newInst = AddIns(convOp);

    newInst->SetSVar(sp->var);
    new (sp) StackInfo(type);
    int32_t var = CreateVarExplicit(g_interpTypeFromStackType[type], NULL, INTERP_STACK_SLOT_SIZE);
    sp->var = var;
    newInst->SetDVar(var);
}

static InterpType GetInterpType(CorInfoType corInfoType)
{
    switch (corInfoType)
    {
        case CORINFO_TYPE_BYTE:
            return InterpTypeI1;
        case CORINFO_TYPE_UBYTE:
        case CORINFO_TYPE_BOOL:
            return InterpTypeU1;
        case CORINFO_TYPE_CHAR:
        case CORINFO_TYPE_USHORT:
            return InterpTypeU2;
        case CORINFO_TYPE_SHORT:
            return InterpTypeI2;
        case CORINFO_TYPE_INT:
        case CORINFO_TYPE_UINT:
            return InterpTypeI4;
        case CORINFO_TYPE_LONG:
        case CORINFO_TYPE_ULONG:
            return InterpTypeI8;
        case CORINFO_TYPE_NATIVEINT:
        case CORINFO_TYPE_NATIVEUINT:
            return InterpTypeI;
        case CORINFO_TYPE_FLOAT:
            return InterpTypeR4;
        case CORINFO_TYPE_DOUBLE:
            return InterpTypeR8;
        case CORINFO_TYPE_STRING:
        case CORINFO_TYPE_CLASS:
            return InterpTypeO;
        case CORINFO_TYPE_PTR:
            return InterpTypeI;
        case CORINFO_TYPE_BYREF:
            return InterpTypeByRef;
        case CORINFO_TYPE_VALUECLASS:
        case CORINFO_TYPE_REFANY:
            return InterpTypeVT;
        case CORINFO_TYPE_VOID:
            return InterpTypeVoid;
        default:
            assert(0);
            break;
    }
    return InterpTypeVoid;
}

int32_t InterpCompiler::GetInterpTypeStackSize(CORINFO_CLASS_HANDLE clsHnd, InterpType interpType, int32_t *pAlign)
{
    int32_t size, align;
    if (interpType == InterpTypeVT)
    {
        size = m_compHnd->getClassSize(clsHnd);
        align = m_compHnd->getClassAlignmentRequirement(clsHnd);

        assert(align <= INTERP_STACK_ALIGNMENT);

        // All vars are stored at 8 byte aligned offsets
        if (align < INTERP_STACK_SLOT_SIZE)
            align = INTERP_STACK_SLOT_SIZE;
    }
    else
    {
        size = INTERP_STACK_SLOT_SIZE; // not really
        align = INTERP_STACK_SLOT_SIZE;
    }
    *pAlign = align;
    return size;
}


void InterpCompiler::CreateILVars()
{
    bool hasThis = m_methodInfo->args.hasThis();
    int32_t offset, size, align;
    int numArgs = hasThis + m_methodInfo->args.numArgs;
    int numILLocals = m_methodInfo->locals.numArgs;
    int numILVars = numArgs + numILLocals;

    // add some starting extra space for new vars
    m_varsCapacity = numILVars + 64;
    m_pVars = (InterpVar*)AllocTemporary0(m_varsCapacity * sizeof (InterpVar));
    m_varsSize = numILVars;

    offset = 0;

    INTERP_DUMP("\nCreate IL Vars:\n");

    CORINFO_ARG_LIST_HANDLE sigArg = m_methodInfo->args.args;
    for (int i = 0; i < numArgs; i++) {
        InterpType interpType;
        CORINFO_CLASS_HANDLE argClass;
        if (hasThis && i == 0)
        {
            argClass = m_compHnd->getMethodClass(m_methodInfo->ftn);
            if (m_compHnd->isValueClass(argClass))
                interpType = InterpTypeByRef;
            else
                interpType = InterpTypeO;
        }
        else
        {
            CorInfoType argCorType;
            argCorType = strip(m_compHnd->getArgType(&m_methodInfo->args, sigArg, &argClass));
            interpType = GetInterpType(argCorType);
            sigArg = m_compHnd->getArgNext(sigArg);
        }
        size = GetInterpTypeStackSize(argClass, interpType, &align);

        new (&m_pVars[i]) InterpVar(interpType, argClass, size);

        m_pVars[i].global = true;
        m_pVars[i].ILGlobal = true;
        m_pVars[i].size = size;
        offset = ALIGN_UP_TO(offset, align);
        m_pVars[i].offset = offset;
        INTERP_DUMP("alloc arg var %d to offset %d\n", i, offset);
        offset += size;
    }

    offset = ALIGN_UP_TO(offset, INTERP_STACK_ALIGNMENT);

    sigArg = m_methodInfo->locals.args;
    m_ILLocalsOffset = offset;
    for (int i = 0; i < numILLocals; i++) {
        int index = numArgs + i;
        InterpType interpType;
        CORINFO_CLASS_HANDLE argClass;

        CorInfoType argCorType = strip(m_compHnd->getArgType(&m_methodInfo->locals, sigArg, &argClass));
        interpType = GetInterpType(argCorType);
        size = GetInterpTypeStackSize(argClass, interpType, &align);

        new (&m_pVars[index]) InterpVar(interpType, argClass, size);

        m_pVars[index].global = true;
        m_pVars[index].ILGlobal = true;
        offset = ALIGN_UP_TO(offset, align);
        m_pVars[index].offset = offset;
        INTERP_DUMP("alloc local var %d to offset %d\n", index, offset);
        offset += size;
        sigArg = m_compHnd->getArgNext(sigArg);
    }
    offset = ALIGN_UP_TO(offset, INTERP_STACK_ALIGNMENT);

    m_ILLocalsSize = offset - m_ILLocalsOffset;
    m_totalVarsStackSize = offset;
}

bool InterpCompiler::CreateBasicBlocks(CORINFO_METHOD_INFO* methodInfo)
{
    int32_t codeSize = methodInfo->ILCodeSize;
    uint8_t *codeStart = methodInfo->ILCode;
    uint8_t *codeEnd = codeStart + codeSize;
    const uint8_t *ip = codeStart;

    m_ppOffsetToBB = (InterpBasicBlock**)AllocMemPool0(sizeof(InterpBasicBlock*) * (methodInfo->ILCodeSize + 1));
    GetBB(0);

    for (unsigned int i = 0; i < methodInfo->EHcount; i++)
    {
        CORINFO_EH_CLAUSE clause;
        m_compHnd->getEHinfo(methodInfo->ftn, i, &clause);

        if ((codeStart + clause.TryOffset) > codeEnd ||
                (codeStart + clause.TryOffset + clause.TryLength) > codeEnd)
        {
            return false;
        }
        GetBB(clause.TryOffset);

        if ((codeStart + clause.HandlerOffset) > codeEnd ||
                (codeStart + clause.HandlerOffset + clause.HandlerLength) > codeEnd)
        {
            return false;
        }
        GetBB(clause.HandlerOffset);

        if (clause.Flags == CORINFO_EH_CLAUSE_FILTER)
        {
            if ((codeStart + clause.FilterOffset) > codeEnd)
                return false;
            GetBB(clause.FilterOffset);
        }
    }

    while (ip < codeEnd)
    {
        int32_t insOffset = (int32_t)(ip - codeStart);
        OPCODE opcode = CEEDecodeOpcode(&ip);
        OPCODE_FORMAT opArgs = g_CEEOpArgs[opcode];
        int32_t target;

        switch (opArgs)
        {
        case InlineNone:
            ip++;
            break;
        case InlineString:
        case InlineType:
        case InlineField:
        case InlineMethod:
        case InlineTok:
        case InlineSig:
        case ShortInlineR:
        case InlineI:
            ip += 5;
            break;
        case InlineVar:
            ip += 3;
            break;
        case ShortInlineVar:
        case ShortInlineI:
            ip += 2;
            break;
        case ShortInlineBrTarget:
            target = insOffset + 2 + (int8_t)ip [1];
            if (target >= codeSize)
                return false;
            GetBB(target);
            ip += 2;
            GetBB((int32_t)(ip - codeStart));
            break;
        case InlineBrTarget:
            target = insOffset + 5 + getI4LittleEndian(ip + 1);
            if (target >= codeSize)
                return false;
            GetBB(target);
            ip += 5;
            GetBB((int32_t)(ip - codeStart));
            break;
        case InlineSwitch: {
            uint32_t n = getI4LittleEndian(ip + 1);
            ip += 5;
            insOffset += 5 + 4 * n;
            target = insOffset;
            if (target >= codeSize)
                return false;
            GetBB(target);
            for (uint32_t i = 0; i < n; i++)
            {
                target = insOffset + getI4LittleEndian(ip);
                if (target >= codeSize)
                    return false;
                GetBB(target);
                ip += 4;
            }
            GetBB((int32_t)(ip - codeStart));
            break;
        }
        case InlineR:
        case InlineI8:
            ip += 9;
            break;
        default:
            assert(0);
        }
        if (opcode == CEE_THROW || opcode == CEE_ENDFINALLY || opcode == CEE_RETHROW)
            GetBB((int32_t)(ip - codeStart));
    }

    return true;
}

// ilOffset represents relative branch offset
void InterpCompiler::EmitBranch(InterpOpcode opcode, int32_t ilOffset)
{
    int32_t target = (int32_t)(m_ip - m_pILCode) + ilOffset;
    if (target < 0 || target >= m_ILCodeSize)
        assert(0);

    InterpBasicBlock *pTargetBB = m_ppOffsetToBB[target];
    assert(pTargetBB != NULL);

    EmitBBEndVarMoves(pTargetBB);
    InitBBStackState(pTargetBB);

    AddIns(opcode);
    m_pLastIns->info.pTargetBB = pTargetBB;
}

void InterpCompiler::EmitOneArgBranch(InterpOpcode opcode, int32_t ilOffset, int insSize)
{
    CHECK_STACK_RET_VOID(1);
    StackType argType = (m_pStackPointer[-1].type == StackTypeO || m_pStackPointer[-1].type == StackTypeByRef) ? StackTypeI : m_pStackPointer[-1].type;
    // offset the opcode to obtain the type specific I4/I8/R4/R8 variant.
    InterpOpcode opcodeArgType = (InterpOpcode)(opcode + argType - StackTypeI4);
    m_pStackPointer--;
    if (ilOffset)
    {
        EmitBranch(opcodeArgType, ilOffset + insSize);
        m_pLastIns->SetSVar(m_pStackPointer[0].var);
    }
    else
    {
        AddIns(INTOP_NOP);
    }
}

void InterpCompiler::EmitTwoArgBranch(InterpOpcode opcode, int32_t ilOffset, int insSize)
{
    CHECK_STACK_RET_VOID(2);
    StackType argType1 = (m_pStackPointer[-1].type == StackTypeO || m_pStackPointer[-1].type == StackTypeByRef) ? StackTypeI : m_pStackPointer[-1].type;
    StackType argType2 = (m_pStackPointer[-2].type == StackTypeO || m_pStackPointer[-2].type == StackTypeByRef) ? StackTypeI : m_pStackPointer[-2].type;

    // Since branch opcodes only compare args of the same type, handle implicit conversions before
    // emitting the conditional branch
    if (argType1 == StackTypeI4 && argType2 == StackTypeI8)
    {
        EmitConv(m_pStackPointer - 1, m_pLastIns, StackTypeI8, INTOP_CONV_I8_I4);
        argType1 = StackTypeI8;
    }
    else if (argType1 == StackTypeI8 && argType2 == StackTypeI4)
    {
        EmitConv(m_pStackPointer - 2, m_pLastIns, StackTypeI8, INTOP_CONV_I8_I4);
    }
    else if (argType1 == StackTypeR4 && argType2 == StackTypeR8)
    {
        EmitConv(m_pStackPointer - 1, m_pLastIns, StackTypeR8, INTOP_CONV_R8_R4);
        argType1 = StackTypeR8;
    }
    else if (argType1 == StackTypeR8 && argType2 == StackTypeR4)
    {
        EmitConv(m_pStackPointer - 2, m_pLastIns, StackTypeR8, INTOP_CONV_R8_R4);
    }
    else if (argType1 != argType2)
    {
        m_hasInvalidCode = true;
        return;
    }

    // offset the opcode to obtain the type specific I4/I8/R4/R8 variant.
    InterpOpcode opcodeArgType = (InterpOpcode)(opcode + argType1 - StackTypeI4);
    m_pStackPointer -= 2;

    if (ilOffset)
    {
        EmitBranch(opcodeArgType, ilOffset + insSize);
        m_pLastIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
    }
    else
    {
        AddIns(INTOP_NOP);
    }
}


void InterpCompiler::EmitLoadVar(int32_t var)
{
    InterpType interpType = m_pVars[var].interpType;
    int32_t size = m_pVars[var].size;
    CORINFO_CLASS_HANDLE clsHnd = m_pVars[var].clsHnd;

    if (interpType == InterpTypeVT)
        PushTypeVT(clsHnd, size);
    else
        PushInterpType(interpType, clsHnd);

    AddIns(InterpGetMovForType(interpType, true));
    m_pLastIns->SetSVar(var);
    m_pLastIns->SetDVar(m_pStackPointer[-1].var);
    if (interpType == InterpTypeVT)
        m_pLastIns->data[0] = size;
}

void InterpCompiler::EmitStoreVar(int32_t var)
{
    InterpType interpType = m_pVars[var].interpType;
    CHECK_STACK_RET_VOID(1);

#ifdef TARGET_64BIT
    // nint and int32 can be used interchangeably. Add implicit conversions.
    if (m_pStackPointer[-1].type == StackTypeI4 && g_stackTypeFromInterpType[interpType] == StackTypeI8)
        EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_I8_I4);
#endif
    if (m_pStackPointer[-1].type == StackTypeR4 && g_stackTypeFromInterpType[interpType] == StackTypeR8)
        EmitConv(m_pStackPointer - 1, NULL, StackTypeR8, INTOP_CONV_R8_R4);
    else if (m_pStackPointer[-1].type == StackTypeR8 && g_stackTypeFromInterpType[interpType] == StackTypeR4)
        EmitConv(m_pStackPointer - 1, NULL, StackTypeR4, INTOP_CONV_R4_R8);

    m_pStackPointer--;
    AddIns(InterpGetMovForType(interpType, false));
    m_pLastIns->SetSVar(m_pStackPointer[0].var);
    m_pLastIns->SetDVar(var);
    if (interpType == InterpTypeVT)
        m_pLastIns->data[0] = m_pVars[var].size;
}

void InterpCompiler::EmitBinaryArithmeticOp(int32_t opBase)
{
    CHECK_STACK_RET_VOID(2);
    StackType type1 = m_pStackPointer[-2].type;
    StackType type2 = m_pStackPointer[-1].type;

    StackType typeRes;

    if (opBase == INTOP_ADD_I4 && (type1 == StackTypeByRef || type2 == StackTypeByRef))
    {
        if (type1 == type2)
            INVALID_CODE_RET_VOID;
        if (type1 == StackTypeByRef)
        {
            if (type2 == StackTypeI4)
            {
#ifdef TARGET_64BIT
                EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_I8_I4);
                type2 = StackTypeI8;
#endif
                typeRes = StackTypeByRef;
            }
            else if (type2 == StackTypeI)
            {
                typeRes = StackTypeByRef;
            }
            else
            {
                INVALID_CODE_RET_VOID;
            }
        }
        else
        {
            // type2 == StackTypeByRef
            if (type1 == StackTypeI4)
            {
#ifdef TARGET_64BIT
                EmitConv(m_pStackPointer - 2, NULL, StackTypeI8, INTOP_CONV_I8_I4);
                type1 = StackTypeI8;
#endif
                typeRes = StackTypeByRef;
            }
            else if (type1 == StackTypeI)
            {
                typeRes = StackTypeByRef;
            }
            else
            {
                INVALID_CODE_RET_VOID;
            }
        }
    }
    else if (opBase == INTOP_SUB_I4 && type1 == StackTypeByRef)
    {
        if (type2 == StackTypeI4)
        {
#ifdef TARGET_64BIT
            EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_I8_I4);
            type2 = StackTypeI8;
#endif
            typeRes = StackTypeByRef;
        }
        else if (type2 == StackTypeI)
        {
            typeRes = StackTypeByRef;
        }
        else if (type2 == StackTypeByRef)
        {
            typeRes = StackTypeI;
        }
        else
        {
            INVALID_CODE_RET_VOID;
        }
    }
    else
    {
#if SIZEOF_VOID_P == 8
        if (type1 == StackTypeI8 && type2 == StackTypeI4)
        {
            EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_I8_I4);
            type2 = StackTypeI8;
        }
        else if (type1 == StackTypeI4 && type2 == StackTypeI8)
        {
            EmitConv(m_pStackPointer - 2, NULL, StackTypeI8, INTOP_CONV_I8_I4);
            type1 = StackTypeI8;
        }
#endif
        if (type1 == StackTypeR8 && type2 == StackTypeR4)
        {
            EmitConv(m_pStackPointer - 1, NULL, StackTypeR8, INTOP_CONV_R8_R4);
            type2 = StackTypeR8;
        }
        else if (type1 == StackTypeR4 && type2 == StackTypeR8)
        {
            EmitConv(m_pStackPointer - 2, NULL, StackTypeR8, INTOP_CONV_R8_R4);
            type1 = StackTypeR8;
        }
        if (type1 != type2)
            INVALID_CODE_RET_VOID;

        typeRes = type1;
    }

    // The argument opcode is for the base _I4 instruction. Depending on the type of the result
    // we compute the specific variant, _I4/_I8/_R4 or R8.
    int32_t typeOffset = ((typeRes == StackTypeByRef) ? StackTypeI : typeRes) - StackTypeI4;
    int32_t finalOpcode = opBase + typeOffset;

    m_pStackPointer -= 2;
    AddIns(finalOpcode);
    m_pLastIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
    PushStackType(typeRes, NULL);
    m_pLastIns->SetDVar(m_pStackPointer[-1].var);
}

void InterpCompiler::EmitUnaryArithmeticOp(int32_t opBase)
{
    CHECK_STACK_RET_VOID(1);
    StackType stackType = m_pStackPointer[-1].type;
    int32_t finalOpcode = opBase + (stackType - StackTypeI4);

    if (stackType == StackTypeByRef || stackType == StackTypeO)
        INVALID_CODE_RET_VOID;
    if (opBase == INTOP_NOT_I4 && (stackType != StackTypeI4 && stackType != StackTypeI8))
        INVALID_CODE_RET_VOID;

    m_pStackPointer--;
    AddIns(finalOpcode);
    m_pLastIns->SetSVar(m_pStackPointer[0].var);
    PushStackType(stackType, NULL);
    m_pLastIns->SetDVar(m_pStackPointer[-1].var);
}

void InterpCompiler::EmitShiftOp(int32_t opBase)
{
    CHECK_STACK_RET_VOID(2);
    StackType stackType = m_pStackPointer[-2].type;
    StackType shiftAmountType = m_pStackPointer[-1].type;
    int32_t typeOffset = stackType - StackTypeI4;
    int32_t finalOpcode = opBase + typeOffset;

    if ((stackType != StackTypeI4 && stackType != StackTypeI8) ||
            (shiftAmountType != StackTypeI4 && shiftAmountType != StackTypeI))
        INVALID_CODE_RET_VOID;

    m_pStackPointer -= 2;
    AddIns(finalOpcode);
    m_pLastIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
    PushStackType(stackType, NULL);
    m_pLastIns->SetDVar(m_pStackPointer[-1].var);
}

void InterpCompiler::EmitCompareOp(int32_t opBase)
{
    CHECK_STACK_RET_VOID(2);
    if (m_pStackPointer[-1].type == StackTypeO || m_pStackPointer[-1].type == StackTypeByRef)
    {
        AddIns(opBase + StackTypeI - StackTypeI4);
    }
    else
    {
        if (m_pStackPointer[-1].type == StackTypeR4 && m_pStackPointer[-2].type == StackTypeR8)
            EmitConv(m_pStackPointer - 1, NULL, StackTypeR8, INTOP_CONV_R8_R4);
        if (m_pStackPointer[-1].type == StackTypeR8 && m_pStackPointer[-2].type == StackTypeR4)
            EmitConv(m_pStackPointer - 2, NULL, StackTypeR8, INTOP_CONV_R8_R4);
        AddIns(opBase + m_pStackPointer[-1].type - StackTypeI4);
    }
    m_pStackPointer -= 2;
    m_pLastIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
    PushStackType(StackTypeI4, NULL);
    m_pLastIns->SetDVar(m_pStackPointer[-1].var);
}

int32_t InterpCompiler::GetDataItemIndex(void *data)
{
    int32_t index = m_dataItems.Find(data);
    if (index != -1)
        return index;

    return m_dataItems.Add(data);
}

int32_t InterpCompiler::GetMethodDataItemIndex(CORINFO_METHOD_HANDLE mHandle)
{
    size_t data = (size_t)mHandle | INTERP_METHOD_DESC_TAG;
    return GetDataItemIndex((void*)data);
}

bool InterpCompiler::EmitCallIntrinsics(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO sig)
{
    const char *className = NULL;
    const char *namespaceName = NULL;
    const char *methodName = m_compHnd->getMethodNameFromMetadata(method, &className, &namespaceName, NULL, 0);
    int32_t opcode = -1;

    if (namespaceName && !strcmp(namespaceName, "System"))
    {
        if (className && !strcmp(className, "Environment"))
        {
            if (methodName && !strcmp(methodName, "FailFast"))
                opcode = INTOP_FAILFAST; // to be removed, not really an intrisic
        }
    }

    if (opcode != -1)
    {
        AddIns(opcode);
        return true;
    }

    return false;
}

void InterpCompiler::EmitCall(CORINFO_CLASS_HANDLE constrainedClass, bool readonly, bool tailcall)
{
    uint32_t token = getU4LittleEndian(m_ip + 1);
    CORINFO_RESOLVED_TOKEN resolvedToken;

    resolvedToken.tokenScope = m_compScopeHnd;
    resolvedToken.tokenContext = METHOD_BEING_COMPILED_CONTEXT();
    resolvedToken.token = token;
    resolvedToken.tokenType = CORINFO_TOKENKIND_Method;
    m_compHnd->resolveToken(&resolvedToken);

    CORINFO_METHOD_HANDLE targetMethod = resolvedToken.hMethod;

    CORINFO_SIG_INFO targetSignature;
    m_compHnd->getMethodSig(targetMethod, &targetSignature);

    if (EmitCallIntrinsics(targetMethod, targetSignature))
    {
        m_ip += 5;
        return;
    }

    // Process sVars
    int numArgs = targetSignature.numArgs + targetSignature.hasThis();
    m_pStackPointer -= numArgs;

    int *callArgs = (int*) AllocMemPool((numArgs + 1) * sizeof(int));
    for (int i = 0; i < numArgs; i++)
        callArgs[i] = m_pStackPointer [i].var;
    callArgs[numArgs] = -1;

    // Process dVar
    int32_t dVar;
    if (targetSignature.retType != CORINFO_TYPE_VOID)
    {
        InterpType interpType = GetInterpType(targetSignature.retType);

        if (interpType == InterpTypeVT)
        {
            int32_t size = m_compHnd->getClassSize(targetSignature.retTypeClass);
            PushTypeVT(targetSignature.retTypeClass, size);
        }
        else
        {
            PushInterpType(interpType, NULL);
        }
        dVar = m_pStackPointer[-1].var;
    }
    else
    {
        // Create a new dummy var to serve as the dVar of the call
        // FIXME Consider adding special dVar type (ex -1), that is
        // resolved to null offset. The opcode shouldn't really write to it
        PushStackType(StackTypeI4, NULL);
        m_pStackPointer--;
        dVar = m_pStackPointer[0].var;
    }

    // Emit call instruction
    AddIns(INTOP_CALL);
    m_pLastIns->SetDVar(dVar);
    m_pLastIns->SetSVar(CALL_ARGS_SVAR);
    m_pLastIns->data[0] = GetMethodDataItemIndex(targetMethod);

    m_pLastIns->flags |= INTERP_INST_FLAG_CALL;
    m_pLastIns->info.pCallInfo = (InterpCallInfo*)AllocMemPool0(sizeof (InterpCallInfo));
    m_pLastIns->info.pCallInfo->pCallArgs = callArgs;

    m_ip += 5;
}

int InterpCompiler::GenerateCode(CORINFO_METHOD_INFO* methodInfo)
{
    bool readonly = false;
    bool tailcall = false;
    CORINFO_CLASS_HANDLE constrainedClass = NULL;
    uint8_t *codeEnd;
    int numArgs = m_methodInfo->args.hasThis() + m_methodInfo->args.numArgs;
    bool emittedBBlocks, linkBBlocks, needsRetryEmit;
    m_ip = m_pILCode = methodInfo->ILCode;
    m_ILCodeSize = (int32_t)methodInfo->ILCodeSize;

    m_stackCapacity = methodInfo->maxStack + 1;
    m_pStackBase = m_pStackPointer = (StackInfo*)AllocTemporary(sizeof(StackInfo) * m_stackCapacity);

    m_pEntryBB = AllocBB(0);
    m_pEntryBB->emitState = BBStateEmitting;
    m_pEntryBB->stackHeight = 0;
    m_pCBB = m_pEntryBB;

    if (!CreateBasicBlocks(methodInfo))
    {
        m_hasInvalidCode = true;
        goto exit_bad_code;
    }

    codeEnd = m_ip + m_ILCodeSize;

    linkBBlocks = true;
    needsRetryEmit = false;
retry_emit:
    emittedBBlocks = false;
    while (m_ip < codeEnd)
    {
        // Check here for every opcode to avoid code bloat
        if (m_hasInvalidCode)
            goto exit_bad_code;

        int32_t insOffset = (int32_t)(m_ip - m_pILCode);
        m_currentILOffset = insOffset;

        InterpBasicBlock *pNewBB = m_ppOffsetToBB[insOffset];
        if (pNewBB != NULL && m_pCBB != pNewBB)
        {
            INTERP_DUMP("BB%d (IL_%04x):\n", pNewBB->index, pNewBB->ilOffset);
            // If we were emitting into previous bblock, we are finished now
            if (m_pCBB->emitState == BBStateEmitting)
                m_pCBB->emitState = BBStateEmitted;
            // If the new bblock was already emitted, skip its instructions
            if (pNewBB->emitState == BBStateEmitted)
            {
                if (linkBBlocks)
                {
                    LinkBBs(m_pCBB, pNewBB);
                    // Further emitting can only start at a point where the bblock is not fallthrough
                    linkBBlocks = false;
                }
                // If the bblock was fully emitted it means we already iterated at least once over
                // all instructions so we have `pNextBB` initialized, unless it is the last bblock.
                // Skip through all emitted bblocks.
                m_pCBB = pNewBB;
                while (m_pCBB->pNextBB && m_pCBB->pNextBB->emitState == BBStateEmitted)
                    m_pCBB = m_pCBB->pNextBB;

                if (m_pCBB->pNextBB)
                    m_ip = m_pILCode + m_pCBB->pNextBB->ilOffset;
                else
                    m_ip = codeEnd;

                continue;
            }
            else
            {
                assert (pNewBB->emitState == BBStateNotEmitted);
            }
            // We are starting a new basic block. Change cbb and link them together
            if (linkBBlocks)
            {
                // By default we link cbb with the new starting bblock, unless the previous
                // instruction is an unconditional branch (BR, LEAVE, ENDFINALLY)
                LinkBBs(m_pCBB, pNewBB);
                EmitBBEndVarMoves(pNewBB);
                pNewBB->emitState = BBStateEmitting;
                emittedBBlocks = true;
                if (pNewBB->stackHeight >= 0)
                {
                    MergeStackTypeInfo(m_pStackBase, pNewBB->pStackState, pNewBB->stackHeight);
                    // This is relevant only for copying the vars associated with the values on the stack
                    memcpy(m_pStackBase, pNewBB->pStackState, pNewBB->stackHeight * sizeof(StackInfo));
                    m_pStackPointer = m_pStackBase + pNewBB->stackHeight;
                }
                else
                {
                    // This bblock has not been branched to yet. Initialize its stack state
                    InitBBStackState(pNewBB);
                }
                // linkBBlocks remains true, which is the default
            }
            else
            {
                if (pNewBB->stackHeight >= 0)
                {
                    // This is relevant only for copying the vars associated with the values on the stack
                    memcpy (m_pStackBase, pNewBB->pStackState, pNewBB->stackHeight * sizeof(StackInfo));
                    m_pStackPointer = m_pStackBase + pNewBB->stackHeight;
                    pNewBB->emitState = BBStateEmitting;
                    emittedBBlocks = true;
                    linkBBlocks = true;
                }
                else
                {
                    INTERP_DUMP("BB%d without initialized stack\n", pNewBB->index);
                    assert(pNewBB->emitState == BBStateNotEmitted);
                    needsRetryEmit = true;
                    // linking to its next bblock, if its the case, will only happen
                    // after we actually emit the bblock
                    linkBBlocks = false;
                    // If we had pNewBB->pNextBB initialized, here we could skip to its il offset directly.
                    // We will just skip all instructions instead, since it doesn't seem that problematic.
                }
            }
            if (!m_pCBB->pNextBB)
                m_pCBB->pNextBB = pNewBB;
            m_pCBB = pNewBB;
        }

        int32_t opcodeSize = CEEOpcodeSize(m_ip, codeEnd);
        if (m_pCBB->emitState != BBStateEmitting)
        {
            // If we are not really emitting, just skip the instructions in the bblock
            m_ip += opcodeSize;
            continue;
        }

        m_ppOffsetToBB[insOffset] = m_pCBB;

#ifdef DEBUG
        if (m_verbose)
        {
            const uint8_t *ip = m_ip;
            printf("IL_%04x %-10s, sp %d, %s",
                (int32_t)(m_ip - m_pILCode),
                CEEOpName(CEEDecodeOpcode(&ip)), (int32_t)(m_pStackPointer - m_pStackBase),
                m_pStackPointer > m_pStackBase ? g_stackTypeString[m_pStackPointer[-1].type] : "  ");
            if (m_pStackPointer > m_pStackBase &&
                    (m_pStackPointer[-1].type == StackTypeO || m_pStackPointer[-1].type == StackTypeVT) &&
                    m_pStackPointer[-1].clsHnd != NULL)
                PrintClassName(m_pStackPointer[-1].clsHnd);
            printf("\n");
        }
#endif

        uint8_t opcode = *m_ip;
        switch (opcode)
        {
            case CEE_NOP:
                m_ip++;
                break;
            case CEE_LDC_I4_M1:
            case CEE_LDC_I4_0:
            case CEE_LDC_I4_1:
            case CEE_LDC_I4_2:
            case CEE_LDC_I4_3:
            case CEE_LDC_I4_4:
            case CEE_LDC_I4_5:
            case CEE_LDC_I4_6:
            case CEE_LDC_I4_7:
            case CEE_LDC_I4_8:
                AddIns(INTOP_LDC_I4);
                m_pLastIns->data[0] = opcode - CEE_LDC_I4_0;
                PushStackType(StackTypeI4, NULL);
                m_pLastIns->SetDVar(m_pStackPointer[-1].var);
                m_ip++;
                break;
            case CEE_LDC_I4_S:
                AddIns(INTOP_LDC_I4);
                m_pLastIns->data[0] = (int8_t)m_ip[1];
                PushStackType(StackTypeI4, NULL);
                m_pLastIns->SetDVar(m_pStackPointer[-1].var);
                m_ip += 2;
                break;
            case CEE_LDC_I4:
                AddIns(INTOP_LDC_I4);
                m_pLastIns->data[0] = getI4LittleEndian(m_ip + 1);
                PushStackType(StackTypeI4, NULL);
                m_pLastIns->SetDVar(m_pStackPointer[-1].var);
                m_ip += 5;
                break;
            case CEE_LDNULL:
                AddIns(INTOP_LDNULL);
                PushStackType(StackTypeO, NULL);
                m_pLastIns->SetDVar(m_pStackPointer[-1].var);
                m_ip++;
                break;

            case CEE_LDARG_S:
                EmitLoadVar(m_ip[1]);
                m_ip += 2;
                break;
            case CEE_LDARG_0:
            case CEE_LDARG_1:
            case CEE_LDARG_2:
            case CEE_LDARG_3:
                EmitLoadVar(*m_ip - CEE_LDARG_0);
                m_ip++;
                break;
            case CEE_STARG_S:
                EmitStoreVar(m_ip[1]);
                m_ip += 2;
                break;
            case CEE_LDLOC_S:
                EmitLoadVar(numArgs + m_ip[1]);
                m_ip += 2;
                break;
            case CEE_LDLOC_0:
            case CEE_LDLOC_1:
            case CEE_LDLOC_2:
            case CEE_LDLOC_3:
                EmitLoadVar(numArgs + *m_ip - CEE_LDLOC_0);
                m_ip++;
                break;
            case CEE_STLOC_S:
                EmitStoreVar(numArgs + m_ip[1]);
                m_ip += 2;
                break;
            case CEE_STLOC_0:
            case CEE_STLOC_1:
            case CEE_STLOC_2:
            case CEE_STLOC_3:
                EmitStoreVar(numArgs + *m_ip - CEE_STLOC_0);
                m_ip++;
                break;

            case CEE_RET:
            {
                CORINFO_SIG_INFO sig = methodInfo->args;
                InterpType retType = GetInterpType(sig.retType);

                if (retType == InterpTypeVoid)
                {
                    AddIns(INTOP_RET_VOID);
                }
                else if (retType == InterpTypeVT)
                {
                    CHECK_STACK(1);
                    AddIns(INTOP_RET_VT);
                    m_pStackPointer--;
                    int32_t retVar = m_pStackPointer[0].var;
                    m_pLastIns->SetSVar(retVar);
                    m_pLastIns->data[0] = m_pVars[retVar].size;
                }
                else
                {
                    CHECK_STACK(1);
                    AddIns(INTOP_RET);
                    m_pStackPointer--;
                    m_pLastIns->SetSVar(m_pStackPointer[0].var);
                }
                m_ip++;
                break;
            }
            case CEE_CONV_U1:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_U1_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_U1_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_U1_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_U1_I8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_I1:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_I1_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_I1_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_I1_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_I1_I8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_U2:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_U2_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_U2_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_U2_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_U2_I8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_I2:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_I2_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_I2_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_I2_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_I2_I8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_U:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR8:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_CONV_U8_R8);
#else
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_CONV_U4_R8);
#endif
                    break;
                case StackTypeR4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_CONV_U8_R4);
#else
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_CONV_U4_R4);
#endif
                    break;
                case StackTypeI4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_CONV_I8_U4);
#endif
                    break;
                case StackTypeI8:
#ifndef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_MOV_8);
#endif
                    break;
                case StackTypeByRef:
                case StackTypeO:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_MOV_8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_I:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR8:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_CONV_I8_R8);
#else
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_CONV_I4_R8);
#endif
                    break;
                case StackTypeR4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_CONV_I8_R4);
#else
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_CONV_I4_R4);
#endif
                    break;
                case StackTypeI4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_CONV_I8_I4);
#endif
                    break;
                case StackTypeO:
                case StackTypeByRef:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_MOV_8);
                    break;
                case StackTypeI8:
#ifndef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI, INTOP_MOV_8);
#endif
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_U4:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_U4_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_U4_R8);
                    break;
                case StackTypeI4:
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_MOV_8);
                    break;
                case StackTypeByRef:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_MOV_P);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_I4:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_I4_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_CONV_I4_R8);
                    break;
                case StackTypeI4:
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_MOV_8);
                    break;
                case StackTypeByRef:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI4, INTOP_MOV_P);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_I8:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_I8_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_I8_R8);
                    break;
                case StackTypeI4: {
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_I8_I4);
                    break;
                }
                case StackTypeI8:
                    break;
                case StackTypeByRef:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_MOV_8);
#else
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_I8_I4);
#endif
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_R4:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeR4, INTOP_CONV_R4_R8);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeR4, INTOP_CONV_R4_I8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeR4, INTOP_CONV_R4_I4);
                    break;
                case StackTypeR4:
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_R8:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeR8, INTOP_CONV_R8_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeR8, INTOP_CONV_R8_I8);
                    break;
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeR8, INTOP_CONV_R8_R4);
                    break;
                case StackTypeR8:
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_U8:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_I8_U4);
                    break;
                case StackTypeI8:
                    break;
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_U8_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_U8_R8);
                    break;
                case StackTypeByRef:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_MOV_8);
#else
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeI8, INTOP_CONV_I8_U4);
#endif
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_R_UN:
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeR8, INTOP_CONV_R8_R4);
                    break;
                case StackTypeR8:
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeR8, INTOP_CONV_R_UN_I8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, NULL, StackTypeR8, INTOP_CONV_R_UN_I4);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_SWITCH:
            {
                m_ip++;
                uint32_t n = getU4LittleEndian(m_ip);
                // Format of switch instruction is opcode + srcVal + n + T1 + T2 + ... + Tn
                AddInsExplicit(INTOP_SWITCH, n + 3);
                m_pLastIns->data[0] = n;
                m_ip += 4;
                const uint8_t *nextIp = m_ip + n * 4;
                m_pStackPointer--;
                m_pLastIns->SetSVar(m_pStackPointer->var);
                InterpBasicBlock **targetBBTable = (InterpBasicBlock**)AllocMemPool(sizeof (InterpBasicBlock*) * n);

                for (uint32_t i = 0; i < n; i++)
                {
                    int32_t offset = getU4LittleEndian(m_ip);
                    uint32_t target = (uint32_t)(nextIp - m_pILCode + offset);
                    InterpBasicBlock *targetBB = m_ppOffsetToBB[target];
                    assert(targetBB);

                    InitBBStackState(targetBB);
                    targetBBTable[i] = targetBB;
                    LinkBBs(m_pCBB, targetBB);
                    m_ip += 4;
                }
                m_pLastIns->info.ppTargetBBTable = targetBBTable;
                break;
            }
            case CEE_BR:
            {
                int32_t offset = getI4LittleEndian(m_ip + 1);
                if (offset)
                {
                    EmitBranch(INTOP_BR, 5 + offset);
                    linkBBlocks = false;
                }
                m_ip += 5;
                break;
            }
            case CEE_BR_S:
            {
                int32_t offset = (int8_t)m_ip [1];
                if (offset)
                {
                    EmitBranch(INTOP_BR, 2 + (int8_t)m_ip [1]);
                    linkBBlocks = false;
                }
                m_ip += 2;
                break;
            }
            case CEE_BRFALSE:
                EmitOneArgBranch(INTOP_BRFALSE_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BRFALSE_S:
                EmitOneArgBranch(INTOP_BRFALSE_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BRTRUE:
                EmitOneArgBranch(INTOP_BRTRUE_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BRTRUE_S:
                EmitOneArgBranch(INTOP_BRTRUE_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BEQ:
                EmitTwoArgBranch(INTOP_BEQ_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BEQ_S:
                EmitTwoArgBranch(INTOP_BEQ_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BGE:
                EmitTwoArgBranch(INTOP_BGE_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BGE_S:
                EmitTwoArgBranch(INTOP_BGE_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BGT:
                EmitTwoArgBranch(INTOP_BGT_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BGT_S:
                EmitTwoArgBranch(INTOP_BGT_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BLT:
                EmitTwoArgBranch(INTOP_BLT_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BLT_S:
                EmitTwoArgBranch(INTOP_BLT_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BLE:
                EmitTwoArgBranch(INTOP_BLE_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BLE_S:
                EmitTwoArgBranch(INTOP_BLE_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BNE_UN:
                EmitTwoArgBranch(INTOP_BNE_UN_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BNE_UN_S:
                EmitTwoArgBranch(INTOP_BNE_UN_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BGE_UN:
                EmitTwoArgBranch(INTOP_BGE_UN_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BGE_UN_S:
                EmitTwoArgBranch(INTOP_BGE_UN_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BGT_UN:
                EmitTwoArgBranch(INTOP_BGT_UN_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BGT_UN_S:
                EmitTwoArgBranch(INTOP_BGT_UN_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BLE_UN:
                EmitTwoArgBranch(INTOP_BLE_UN_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BLE_UN_S:
                EmitTwoArgBranch(INTOP_BLE_UN_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;
            case CEE_BLT_UN:
                EmitTwoArgBranch(INTOP_BLT_UN_I4, getI4LittleEndian(m_ip + 1), 5);
                m_ip += 5;
                break;
            case CEE_BLT_UN_S:
                EmitTwoArgBranch(INTOP_BLT_UN_I4, (int8_t)m_ip [1], 2);
                m_ip += 2;
                break;

            case CEE_ADD:
                EmitBinaryArithmeticOp(INTOP_ADD_I4);
                m_ip++;
                break;
            case CEE_SUB:
                EmitBinaryArithmeticOp(INTOP_SUB_I4);
                m_ip++;
                break;
            case CEE_MUL:
                EmitBinaryArithmeticOp(INTOP_MUL_I4);
                m_ip++;
                break;
            case CEE_AND:
                EmitBinaryArithmeticOp(INTOP_AND_I4);
                m_ip++;
                break;
            case CEE_OR:
                EmitBinaryArithmeticOp(INTOP_OR_I4);
                m_ip++;
                break;
            case CEE_XOR:
                EmitBinaryArithmeticOp(INTOP_XOR_I4);
                m_ip++;
                break;
            case CEE_SHL:
                EmitShiftOp(INTOP_SHL_I4);
                m_ip++;
                break;
            case CEE_SHR:
                EmitShiftOp(INTOP_SHR_I4);
                m_ip++;
                break;
            case CEE_SHR_UN:
                EmitShiftOp(INTOP_SHR_UN_I4);
                m_ip++;
                break;
            case CEE_NEG:
                EmitUnaryArithmeticOp(INTOP_NEG_I4);
                m_ip++;
                break;
            case CEE_NOT:
                EmitUnaryArithmeticOp(INTOP_NOT_I4);
                m_ip++;
                break;
            case CEE_CALL:
                EmitCall(constrainedClass, readonly, tailcall);
                constrainedClass = NULL;
                readonly = false;
                tailcall = false;
                break;

            case CEE_PREFIX1:
                m_ip++;
                switch (*m_ip + 256)
                {
                    case CEE_LDARG:
                        EmitLoadVar(getU2LittleEndian(m_ip + 1));
                        m_ip += 3;
                        break;
                    case CEE_STARG:
                        EmitStoreVar(getU2LittleEndian(m_ip + 1));
                        m_ip += 3;
                        break;
                    case CEE_LDLOC:
                        EmitLoadVar(numArgs + getU2LittleEndian(m_ip + 1));
                        m_ip += 3;
                        break;
                    case CEE_STLOC:
                        EmitStoreVar(numArgs + getU2LittleEndian(m_ip + 1));\
                        m_ip += 3;
                        break;
                    case CEE_CEQ:
                        EmitCompareOp(INTOP_CEQ_I4);
                        m_ip++;
                        break;
                    case CEE_CGT:
                        EmitCompareOp(INTOP_CGT_I4);
                        m_ip++;
                        break;
                    case CEE_CGT_UN:
                        EmitCompareOp(INTOP_CGT_UN_I4);
                        m_ip++;
                        break;
                    case CEE_CLT:
                        EmitCompareOp(INTOP_CLT_I4);
                        m_ip++;
                        break;
                    case CEE_CLT_UN:
                        EmitCompareOp(INTOP_CLT_UN_I4);
                        m_ip++;
                        break;
                    case CEE_CONSTRAINED:
                    {
                        uint32_t token = getU4LittleEndian(m_ip + 1);
                        CORINFO_RESOLVED_TOKEN resolvedToken;

                        resolvedToken.tokenScope = m_compScopeHnd;
                        resolvedToken.tokenContext = METHOD_BEING_COMPILED_CONTEXT();
                        resolvedToken.token = token;
                        resolvedToken.tokenType = CORINFO_TOKENKIND_Constrained;
                        m_compHnd->resolveToken(&resolvedToken);
                        constrainedClass = resolvedToken.hClass;
                        m_ip += 5;
                        break;
                    }
                    case CEE_READONLY:
                        readonly = true;
                        m_ip++;
                        break;
                    case CEE_TAILCALL:
                        tailcall = true;
                        m_ip++;
                        break;
                    default:
                        assert(0);
                        break;
                }
                break;

            default:
                assert(0);
                break;
        }
    }

    if (m_pCBB->emitState == BBStateEmitting)
        m_pCBB->emitState = BBStateEmitted;

    // If no bblocks were emitted during the last iteration, there is no point to try again
    // Some bblocks are just unreachable in the code.
    if (needsRetryEmit && emittedBBlocks)
    {
        m_ip = m_pILCode;
        m_pCBB = m_pEntryBB;

        linkBBlocks = false;
        needsRetryEmit = false;
        INTERP_DUMP("retry emit\n");
        goto retry_emit;
    }

    UnlinkUnreachableBBlocks();

    return CORJIT_OK;
exit_bad_code:
    return CORJIT_BADCODE;
}

void InterpCompiler::UnlinkUnreachableBBlocks()
{
    // Unlink unreachable bblocks, prevBB is always an emitted bblock
    InterpBasicBlock *prevBB = m_pEntryBB;
    InterpBasicBlock *nextBB = prevBB->pNextBB;
    while (nextBB != NULL)
    {
        if (nextBB->emitState == BBStateNotEmitted)
        {
            m_ppOffsetToBB[nextBB->ilOffset] = NULL;
            prevBB->pNextBB = nextBB->pNextBB;
            nextBB = prevBB->pNextBB;
        }
        else
        {
            prevBB = nextBB;
            nextBB = nextBB->pNextBB;
        }
    }
}

void InterpCompiler::PrintClassName(CORINFO_CLASS_HANDLE cls)
{
    char className[100];
    m_compHnd->printClassName(cls, className, 100);
    printf("%s", className);
}

void InterpCompiler::PrintMethodName(CORINFO_METHOD_HANDLE method)
{
    char methodName[100];

    CORINFO_CLASS_HANDLE cls = m_compHnd->getMethodClass(method);
    PrintClassName(cls);

    m_compHnd->printMethodName(method, methodName, 100);
    printf(".%s", methodName);
}

void InterpCompiler::PrintCode()
{
    for (InterpBasicBlock *pBB = m_pEntryBB; pBB != NULL; pBB = pBB->pNextBB)
        PrintBBCode(pBB);
}

void InterpCompiler::PrintBBCode(InterpBasicBlock *pBB)
{
    printf("BB%d:\n", pBB->index);
    for (InterpInst *ins = pBB->pFirstIns; ins != NULL; ins = ins->pNext)
        PrintIns(ins);
}

void InterpCompiler::PrintIns(InterpInst *ins)
{
    int32_t opcode = ins->opcode;
    if (ins->ilOffset == -1)
        printf("IL_----: %-14s", InterpOpName(opcode));
    else
        printf("IL_%04x: %-14s", ins->ilOffset, InterpOpName(opcode));

    if (g_interpOpDVars[opcode] > 0)
        printf(" [%d <-", ins->dVar);
    else
        printf(" [nil <-");

    if (g_interpOpSVars[opcode] > 0)
    {
        for (int i = 0; i < g_interpOpSVars[opcode]; i++)
        {
            if (ins->sVars[i] == CALL_ARGS_SVAR)
            {
                printf(" c:");
                if (ins->info.pCallInfo && ins->info.pCallInfo->pCallArgs)
                {
                    int *callArgs = ins->info.pCallInfo->pCallArgs;
                    while (*callArgs != CALL_ARGS_TERMINATOR)
                    {
                        printf(" %d", *callArgs);
                        callArgs++;
                    }
                }
            }
            else
            {
                printf(" %d", ins->sVars[i]);
            }
        }
        printf("],");
    }
    else
    {
        printf(" nil],");
    }

    // LDLOCA has special semantics, it has data in sVars[0], but it doesn't have any sVars
    if (opcode == INTOP_LDLOCA)
        printf(" %d", ins->sVars[0]);
    else
        PrintInsData(ins, ins->ilOffset, &ins->data[0], ins->opcode);
    printf("\n");
}

void InterpCompiler::PrintInsData(InterpInst *ins, int32_t insOffset, const int32_t *pData, int32_t opcode)
{
    switch (g_interpOpArgType[opcode]) {
        case InterpOpNoArgs:
            break;
        case InterpOpInt:
            printf(" %d", *pData);
            break;
        case InterpOpBranch:
            if (ins)
                printf(" BB%d", ins->info.pTargetBB->index);
            else
                printf(" IR_%04x", insOffset + *pData);
            break;
        case InterpOpSwitch:
        {
            int32_t n = *pData;
            printf(" (");
            for (int i = 0; i < n; i++)
            {
                if (i > 0)
                    printf(", ");

                if (ins)
                    printf("BB%d", ins->info.ppTargetBBTable[i]->index);
                else
                    printf("IR_%04x", insOffset + 3 + i + *(pData + 1 + i));
            }
            printf(")");
            break;
        }
        case InterpOpMethodToken:
        {
            CORINFO_METHOD_HANDLE mh = (CORINFO_METHOD_HANDLE)((size_t)m_dataItems.Get(*pData) & ~INTERP_METHOD_DESC_TAG);
            printf(" ");
            PrintMethodName(mh);
            break;
        }
        default:
            assert(0);
            break;
    }
}

void InterpCompiler::PrintCompiledCode()
{
    const int32_t *ip = m_pMethodCode;
    const int32_t *end = m_pMethodCode + m_methodCodeSize;

    while (ip < end)
    {
        PrintCompiledIns(ip, m_pMethodCode);
        ip = InterpNextOp(ip);
    }
}

void InterpCompiler::PrintCompiledIns(const int32_t *ip, const int32_t *start)
{
    int32_t opcode = *ip;
    int32_t insOffset = (int32_t)(ip - start);

    printf("IR_%04x: %-14s", insOffset, InterpOpName(opcode));
    ip++;

    if (g_interpOpDVars[opcode] > 0)
        printf(" [%d <-", *ip++);
    else
        printf(" [nil <-");

    if (g_interpOpSVars[opcode] > 0)
    {
        for (int i = 0; i < g_interpOpSVars[opcode]; i++)
            printf(" %d", *ip++);
        printf("],");
    }
    else
    {
        printf(" nil],");
    }

    PrintInsData(NULL, insOffset, ip, opcode);
    printf("\n");
}
