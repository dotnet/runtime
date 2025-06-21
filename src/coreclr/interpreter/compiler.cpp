// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "gcinfoencoder.h"

#include "interpreter.h"
#include "stackmap.h"

#include <inttypes.h>

#include <new> // for std::bad_alloc

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

// Used by assertAbort
thread_local ICorJitInfo* t_InterpJitInfoTls = nullptr;

static const char *g_stackTypeString[] = { "I4", "I8", "R4", "R8", "O ", "VT", "MP", "F " };

/*****************************************************************************/
void AssertOpCodeNotImplemented(const uint8_t *ip, size_t offset)
{
    fprintf(stderr, "IL_%04x %-10s - opcode not supported yet\n",
                (int32_t)(offset),
                CEEOpName(CEEDecodeOpcode(&ip)));
    assert(!"opcode not implemented");
}

// GCInfoEncoder needs an IAllocator implementation. This is a simple one that forwards to the Compiler.
class InterpIAllocator : public IAllocator
{
    InterpCompiler *m_pCompiler;

public:
    InterpIAllocator(InterpCompiler *compiler)
        : m_pCompiler(compiler)
    {
    }

    // Allocates a block of memory at least `sz` in size.
    virtual void* Alloc(size_t sz) override
    {
        return m_pCompiler->AllocMethodData(sz);
    }

    // Allocates a block of memory at least `elems * elemSize` in size.
    virtual void* ArrayAlloc(size_t elems, size_t elemSize) override
    {
        // Ensure that elems * elemSize does not overflow.
        if (elems > (SIZE_MAX / elemSize))
        {
            NOMEM();
        }

        return m_pCompiler->AllocMethodData(elems * elemSize);
    }

    // Frees the block of memory pointed to by p.
    virtual void Free(void* p) override
    {
        // Interpreter-FIXME: m_pCompiler->FreeMethodData
        free(p);
    }
};

// Interpreter-FIXME Use specific allocators for their intended purpose
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
    m_pLastNewIns = ins;
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
        int sVar = m_pStackBase[i].var;
        int dVar = pTargetBB->pStackState[i].var;
        if (sVar != dVar)
        {
            InterpType interpType = m_pVars[sVar].interpType;
            int32_t movOp = InterpGetMovForType(interpType, false);

            AddIns(movOp);
            m_pLastNewIns->SetSVar(sVar);
            m_pLastNewIns->SetDVar(dVar);

            if (interpType == InterpTypeVT)
            {
                assert(m_pVars[sVar].size == m_pVars[dVar].size);
                m_pLastNewIns->data[0] = m_pVars[sVar].size;
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

#define CHECK_STACK(n)         CheckStackHelper(n)
#define INVALID_CODE_RET_VOID  BADCODE("Invalid code detected")

void InterpCompiler::CheckStackHelper(int n)
{
    int32_t currentSize = (int32_t)(m_pStackPointer - m_pStackBase);
    if (currentSize < n)
    {
        BADCODE("Stack underflow");
    }
}

void InterpCompiler::PushTypeExplicit(StackType stackType, CORINFO_CLASS_HANDLE clsHnd, int size)
{
    EnsureStack(1);
    int32_t var = CreateVarExplicit(g_interpTypeFromStackType[stackType], clsHnd, size);
    new (m_pStackPointer) StackInfo(stackType, clsHnd, var);
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

int32_t InterpCompiler::GetLiveStartOffset(int var)
{
    if (m_pVars[var].global)
    {
        return 0;
    }
    else
    {
        assert(m_pVars[var].liveStart != NULL);
        return m_pVars[var].liveStart->nativeOffset;
    }
}

int32_t InterpCompiler::GetLiveEndOffset(int var)
{
    if (m_pVars[var].global)
    {
        return m_methodCodeSize;
    }
    else
    {
        assert(m_pVars[var].liveEnd != NULL);
        return m_pVars[var].liveEnd->nativeOffset + GetInsLength(m_pVars[var].liveEnd);
    }
}

uint32_t InterpCompiler::ConvertOffset(int32_t offset)
{
    // FIXME Once the VM moved the InterpMethod* to code header, we don't need to add a pointer size to the offset
    return offset * sizeof(int32_t) + sizeof(void*);
}

int32_t* InterpCompiler::EmitCodeIns(int32_t *ip, InterpInst *ins, TArray<Reloc*> *relocs)
{
    ins->nativeOffset = (int32_t)(ip - m_pMethodCode);

    int32_t opcode = ins->opcode;
    int32_t *startIp = ip;
    *ip++ = opcode;

    // Set to true if the instruction was completely reverted.
    bool isReverted = false;

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
    else if (InterpOpIsUncondBranch(opcode) || InterpOpIsCondBranch(opcode) || (opcode == INTOP_LEAVE_CATCH) || (opcode == INTOP_CALL_FINALLY))
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
            isReverted = true;
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
    else if (opcode == INTOP_MOV_SRC_OFF)
    {
        // This opcode reuses the MOV opcodes, which are normally used to copy the
        // contents of one var to the other, in order to copy a containing field
        // of the source var (which is a vt) to another var.
        int32_t fOffset = ins->data[0];
        InterpType fType = (InterpType)ins->data[1];
        int32_t fSize = ins->data[2];
        // Revert opcode emit
        ip--;

        int destOffset = m_pVars[ins->dVar].offset;
        int srcOffset = m_pVars[ins->sVars[0]].offset;
        srcOffset += fOffset;
        if (fSize)
            opcode = INTOP_MOV_VT;
        else
            opcode = InterpGetMovForType(fType, true);
        *ip++ = opcode;
        *ip++ = destOffset;
        *ip++ = srcOffset;
        if (opcode == INTOP_MOV_VT)
            *ip++ = fSize;
    }
    else if (opcode == INTOP_LDLOCA)
    {
        // This opcode references a var, int sVars[0], but it is not registered as a source for it
        // aka g_interpOpSVars[INTOP_LDLOCA] is 0.
        *ip++ = m_pVars[ins->dVar].offset;
        *ip++ = m_pVars[ins->sVars[0]].offset;
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

    if ((ins->ilOffset != -1) && !isReverted)
    {
        assert(ins->ilOffset >= 0);
        assert(ins->nativeOffset >= 0);
        uint32_t ilOffset = ins->ilOffset;
        uint32_t nativeOffset = ConvertOffset(ins->nativeOffset);
        if ((m_ILToNativeMapSize == 0) || (m_pILToNativeMap[m_ILToNativeMapSize - 1].ilOffset != ilOffset))
        {
            // This code assumes that instructions for the same IL offset are emitted in a single run without
            // any other IL offsets in between and that they don't repeat again after the run ends.
#ifdef _DEBUG
            for (int i = 0; i < m_ILToNativeMapSize; i++)
            {
                assert(m_pILToNativeMap[i].ilOffset != ilOffset);
            }
#endif // _DEBUG

            // Since we can have at most one entry per IL offset,
            // this map cannot possibly use more entries than the size of the IL code
            assert(m_ILToNativeMapSize < m_ILCodeSize);

            m_pILToNativeMap[m_ILToNativeMapSize].ilOffset = ilOffset;
            m_pILToNativeMap[m_ILToNativeMapSize].nativeOffset = nativeOffset;
            m_ILToNativeMapSize++;
        }
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

int32_t *InterpCompiler::EmitBBCode(int32_t *ip, InterpBasicBlock *bb, TArray<Reloc*> *relocs)
{
    m_pCBB = bb;
    m_pCBB->nativeOffset = (int32_t)(ip - m_pMethodCode);

    for (InterpInst *ins = bb->pFirstIns; ins != NULL; ins = ins->pNext)
    {
        if (InterpOpIsEmitNop(ins->opcode))
        {
            ins->nativeOffset = (int32_t)(ip - m_pMethodCode);
            continue;
        }

        ip = EmitCodeIns(ip, ins, relocs);
    }

    m_pCBB->nativeEndOffset = (int32_t)(ip - m_pMethodCode);

    return ip;
}

void InterpCompiler::EmitCode()
{
    TArray<Reloc*> relocs;
    int32_t codeSize = ComputeCodeSize();
    m_pMethodCode = (int32_t*)AllocMethodData(codeSize * sizeof(int32_t));

    // These will eventually be freed by the VM, and they use the delete [] operator for the deletion.
    m_pILToNativeMap = new ICorDebugInfo::OffsetMapping[m_ILCodeSize];
    ICorDebugInfo::NativeVarInfo* eeVars = NULL;
    if (m_numILVars > 0)
    {
        eeVars = new ICorDebugInfo::NativeVarInfo[m_numILVars];
    }

    // For each BB, compute the number of EH clauses that overlap with it.
    for (unsigned int i = 0; i < m_methodInfo->EHcount; i++)
    {
        CORINFO_EH_CLAUSE clause;
        m_compHnd->getEHinfo(m_methodInfo->ftn, i, &clause);
        for (InterpBasicBlock *bb = m_pEntryBB; bb != NULL; bb = bb->pNextBB)
        {
            if (clause.HandlerOffset <= (uint32_t)bb->ilOffset && (clause.HandlerOffset + clause.HandlerLength) > (uint32_t)bb->ilOffset)
            {
                bb->overlappingEHClauseCount++;
            }

            if (clause.Flags == CORINFO_EH_CLAUSE_FILTER && clause.FilterOffset <= (uint32_t)bb->ilOffset && clause.HandlerOffset > (uint32_t)bb->ilOffset)
            {
                bb->overlappingEHClauseCount++;
            }
        }
    }

    // Emit all the code in waves. First emit all blocks that are not inside any EH clauses.
    // Then emit blocks that are inside of a single EH clause, then ones that are inside of
    // two EH clauses, etc.
    // The goal is to move all clauses to the end of the method code recursively so that
    // no handler is inside of a try block.
    int32_t *ip = m_pMethodCode;
    bool emittedBlock;
    int clauseDepth = 0;
    do
    {
        emittedBlock = false;
        for (InterpBasicBlock *bb = m_pEntryBB; bb != NULL; bb = bb->pNextBB)
        {
            if (bb->overlappingEHClauseCount == clauseDepth)
            {
                ip = EmitBBCode(ip, bb, &relocs);
                emittedBlock = true;
            }
        }
        clauseDepth++;
    }
    while (emittedBlock);

    m_methodCodeSize = (int32_t)(ip - m_pMethodCode);

    PatchRelocations(&relocs);

    int j = 0;
    for (int i = 0; i < m_numILVars; i++)
    {
        assert(m_pVars[i].ILGlobal);
        eeVars[j].startOffset          = ConvertOffset(GetLiveStartOffset(i)); // This is where the variable mapping is start to become valid
        eeVars[j].endOffset            = ConvertOffset(GetLiveEndOffset(i));   // This is where the variable mapping is cease to become valid
        eeVars[j].varNumber            = j;                                    // This is the index of the variable in [arg] + [local]
        eeVars[j].loc.vlType           = ICorDebugInfo::VLT_STK;               // This is a stack slot
        eeVars[j].loc.vlStk.vlsBaseReg = ICorDebugInfo::REGNUM_FP;             // This specifies which register this offset is based off
        eeVars[j].loc.vlStk.vlsOffset  = m_pVars[i].offset;                    // This specifies starting from the offset, how much offset is this from
        j++;
    }

    if (m_numILVars > 0)
    {
        m_compHnd->setVars(m_methodInfo->ftn, m_numILVars, eeVars);
    }
    m_compHnd->setBoundaries(m_methodInfo->ftn, m_ILToNativeMapSize, m_pILToNativeMap);
}

#ifdef FEATURE_INTERPRETER
class InterpGcSlotAllocator
{
    InterpCompiler *m_compiler;
    InterpreterGcInfoEncoder *m_encoder;
    // [pObjects, pByrefs]
    GcSlotId *m_slotTables[2];
    unsigned m_slotTableSize;

#ifdef DEBUG
    bool m_verbose;
#endif

    GcSlotId* LocateGcSlotTableEntry(uint32_t offsetBytes, GcSlotFlags flags)
    {
        GcSlotId *slotTable = m_slotTables[(flags & GC_SLOT_INTERIOR) == GC_SLOT_INTERIOR];
        uint32_t slotIndex = offsetBytes / sizeof(void *);
        assert(slotIndex < m_slotTableSize);
        return &slotTable[slotIndex];
    }

public:
    InterpGcSlotAllocator(InterpCompiler *compiler, InterpreterGcInfoEncoder *encoder)
        : m_compiler(compiler)
        , m_encoder(encoder)
        , m_slotTableSize(compiler->m_totalVarsStackSize / sizeof(void *))
#ifdef DEBUG
        , m_verbose(compiler->m_verbose)
#endif
    {
        for (int i = 0; i < 2; i++)
        {
            m_slotTables[i] = new (compiler) GcSlotId[m_slotTableSize];
            // 0 is a valid slot id so default-initialize all the slots to 0xFFFFFFFF
            memset(m_slotTables[i], 0xFF, sizeof(GcSlotId) * m_slotTableSize);
        }
    }

    void AllocateOrReuseGcSlot(uint32_t offsetBytes, GcSlotFlags flags)
    {
        GcSlotId *pSlot = LocateGcSlotTableEntry(offsetBytes, flags);
        bool allocateNewSlot = *pSlot == ((GcSlotId)-1);

        if (allocateNewSlot)
        {
            // Important to pass GC_FRAMEREG_REL, the default is broken due to GET_CALLER_SP being unimplemented
            *pSlot = m_encoder->GetStackSlotId(offsetBytes, flags, GC_FRAMEREG_REL);
        }
        else
        {
            assert((flags & GC_SLOT_UNTRACKED) == 0);
        }

        INTERP_DUMP(
            "%s %s%sgcslot %u at %u\n",
            allocateNewSlot ? "Allocated" : "Reused",
            (flags & GC_SLOT_UNTRACKED) ? "global " : "",
            (flags & GC_SLOT_INTERIOR) ? "interior " : "",
            *pSlot,
            offsetBytes
        );
    }

    void ReportLiveRange(uint32_t offsetBytes, GcSlotFlags flags, int varIndex)
    {
        GcSlotId *pSlot = LocateGcSlotTableEntry(offsetBytes, flags);
        assert(varIndex < m_compiler->m_varsSize);

        InterpVar *pVar = &m_compiler->m_pVars[varIndex];
        if (pVar->global)
            return;

        GcSlotId slot = *pSlot;
        assert(slot != ((GcSlotId)-1));
        assert(pVar->liveStart);
        assert(pVar->liveEnd);
        uint32_t startOffset = m_compiler->ConvertOffset(m_compiler->GetLiveStartOffset(varIndex)),
            endOffset = m_compiler->ConvertOffset(m_compiler->GetLiveEndOffset(varIndex));
        INTERP_DUMP(
            "Slot %u (%s var #%d offset %u) live [IR_%04x - IR_%04x] [%u - %u]\n",
            slot, pVar->global ? "global" : "local",
            varIndex, pVar->offset,
            m_compiler->GetLiveStartOffset(varIndex), m_compiler->GetLiveEndOffset(varIndex),
            startOffset, endOffset
        );
        m_encoder->SetSlotState(startOffset, slot, GC_SLOT_LIVE);
        m_encoder->SetSlotState(endOffset, slot, GC_SLOT_DEAD);
    }
};
#endif

void InterpCompiler::BuildGCInfo(InterpMethod *pInterpMethod)
{
#ifdef FEATURE_INTERPRETER
    InterpIAllocator* pAllocator = new (this) InterpIAllocator(this);
    InterpreterGcInfoEncoder* gcInfoEncoder = new (this) InterpreterGcInfoEncoder(m_compHnd, m_methodInfo, pAllocator, NOMEM);
    InterpGcSlotAllocator slotAllocator (this, gcInfoEncoder);

    gcInfoEncoder->SetCodeLength(ConvertOffset(m_methodCodeSize));

    INTERP_DUMP("Allocating gcinfo slots for %u vars\n", m_varsSize);

    for (int pass = 0; pass < 2; pass++)
    {
        for (int i = 0; i < m_varsSize; i++)
        {
            InterpVar *pVar = &m_pVars[i];
            GcSlotFlags flags = pVar->global
                ? (GcSlotFlags)GC_SLOT_UNTRACKED
                : (GcSlotFlags)0;

            switch (pVar->interpType) {
                case InterpTypeO:
                    break;
                case InterpTypeByRef:
                    flags = (GcSlotFlags)(flags | GC_SLOT_INTERIOR);
                    break;
                case InterpTypeVT:
                {
                    InterpreterStackMap *stackMap = GetInterpreterStackMap(m_compHnd, pVar->clsHnd);
                    for (unsigned j = 0; j < stackMap->m_slotCount; j++)
                    {
                        InterpreterStackMapSlot slotInfo = stackMap->m_slots[j];
                        unsigned fieldOffset = pVar->offset + slotInfo.m_offsetBytes;
                        GcSlotFlags fieldFlags = (GcSlotFlags)(flags | slotInfo.m_gcSlotFlags);
                        if (pass == 0)
                            slotAllocator.AllocateOrReuseGcSlot(fieldOffset, fieldFlags);
                        else
                            slotAllocator.ReportLiveRange(fieldOffset, fieldFlags, i);
                    }

                    // Don't perform the regular allocateGcSlot call
                    continue;
                }
                default:
                    // Neither an object, interior pointer, or vt, so no slot needed
                    continue;
            }

            if (pass == 0)
                slotAllocator.AllocateOrReuseGcSlot(pVar->offset, flags);
            else
                slotAllocator.ReportLiveRange(pVar->offset, flags, i);
        }

        if (pass == 0)
            gcInfoEncoder->FinalizeSlotIds();
        else
            gcInfoEncoder->Build();
    }

    // GC Encoder automatically puts the GC info in the right spot using ICorJitInfo::allocGCInfo(size_t)
    gcInfoEncoder->Emit();
#endif
}

void InterpCompiler::GetNativeRangeForClause(uint32_t startILOffset, uint32_t endILOffset, int32_t *nativeStartOffset, int32_t* nativeEndOffset)
{
    InterpBasicBlock* pStartBB = m_ppOffsetToBB[startILOffset];
    assert(pStartBB != NULL);

    InterpBasicBlock* pEndBB = pStartBB;
    for (InterpBasicBlock* pBB = pStartBB->pNextBB; (pBB != NULL) && ((uint32_t)pBB->ilOffset < endILOffset); pBB = pBB->pNextBB)
    {
        if ((pBB->clauseType == pStartBB->clauseType) && (pBB->overlappingEHClauseCount == pStartBB->overlappingEHClauseCount))
        {
            pEndBB = pBB;
        }
    }

    *nativeStartOffset = pStartBB->nativeOffset;
    *nativeEndOffset = pEndBB->nativeEndOffset;
}

void InterpCompiler::BuildEHInfo()
{
    uint32_t lastTryILOffset = 0;
    uint32_t lastTryILLength = 0;

    INTERP_DUMP("EH info:\n");

    if (m_methodInfo->EHcount == 0)
    {
        INTERP_DUMP("  None\n");
        return;
    }

    m_compHnd->setEHcount(m_methodInfo->EHcount);
    for (unsigned int i = 0; i < m_methodInfo->EHcount; i++)
    {
        CORINFO_EH_CLAUSE clause;
        CORINFO_EH_CLAUSE nativeClause;

        m_compHnd->getEHinfo(m_methodInfo->ftn, i, &clause);

        int32_t tryStartNativeOffset;
        int32_t tryEndNativeOffset;
        GetNativeRangeForClause(clause.TryOffset, clause.TryOffset + clause.TryLength, &tryStartNativeOffset, &tryEndNativeOffset);

        int32_t handlerStartNativeOffset;
        int32_t handlerEndNativeOffset;
        GetNativeRangeForClause(clause.HandlerOffset, clause.HandlerOffset + clause.HandlerLength, &handlerStartNativeOffset, &handlerEndNativeOffset);

        nativeClause.TryOffset = ConvertOffset(tryStartNativeOffset);
        nativeClause.TryLength = ConvertOffset(tryEndNativeOffset);

        nativeClause.HandlerOffset = ConvertOffset(handlerStartNativeOffset);
        nativeClause.HandlerLength = ConvertOffset(handlerEndNativeOffset);
        InterpBasicBlock* pFilterStartBB = NULL;
        if (clause.Flags == CORINFO_EH_CLAUSE_FILTER)
        {
            pFilterStartBB = m_ppOffsetToBB[clause.FilterOffset];
            nativeClause.FilterOffset = ConvertOffset(pFilterStartBB->nativeOffset);
        }
        else
        {
            nativeClause.ClassToken = clause.ClassToken;
        }

        nativeClause.Flags = clause.Flags;

        // A try region can have multiple catch / filter handlers. All except of the first one need to be marked by
        // the COR_ILEXCEPTION_CLAUSE_SAMETRY flag so that runtime can distinguish this case from a case when
        // the native try region is the same for multiple clauses, but the IL try region is different.
        if ((lastTryILOffset == clause.TryOffset) && (lastTryILLength == clause.TryLength))
        {
            nativeClause.Flags = (CORINFO_EH_CLAUSE_FLAGS)((int)nativeClause.Flags | COR_ILEXCEPTION_CLAUSE_SAMETRY);
        }

        m_compHnd->setEHinfo(i, &nativeClause);

        INTERP_DUMP("  try [IR_%04x(%x), IR_%04x(%x)) ", tryStartNativeOffset, clause.TryOffset, tryEndNativeOffset, clause.TryOffset + clause.TryLength);
        if (clause.Flags == CORINFO_EH_CLAUSE_FILTER)
        {
            INTERP_DUMP("filter IR_%04x(%x), handler [IR_%04x(%x), IR_%04x(%x))%s\n", pFilterStartBB->nativeOffset, clause.FilterOffset, handlerStartNativeOffset, clause.HandlerOffset, handlerEndNativeOffset, clause.HandlerOffset + clause.HandlerLength, ((int)nativeClause.Flags & COR_ILEXCEPTION_CLAUSE_SAMETRY) ? " (same try)" : "");
        }
        else if (nativeClause.Flags == CORINFO_EH_CLAUSE_FINALLY)
        {
            INTERP_DUMP("finally handler [IR_%04x(%x), IR_%04x(%x))\n", handlerStartNativeOffset, clause.HandlerOffset, handlerEndNativeOffset, clause.HandlerOffset + clause.HandlerLength);
        }
        else
        {
            INTERP_DUMP("catch handler [IR_%04x(%x), IR_%04x(%x))%s\n", handlerStartNativeOffset, clause.HandlerOffset, handlerEndNativeOffset, clause.HandlerOffset + clause.HandlerLength, ((int)nativeClause.Flags & COR_ILEXCEPTION_CLAUSE_SAMETRY) ? " (same try)" : "");
        }
    }
}

InterpMethod* InterpCompiler::CreateInterpMethod()
{
    int numDataItems = m_dataItems.GetSize();
    void **pDataItems = (void**)AllocMethodData(numDataItems * sizeof(void*));

    for (int i = 0; i < numDataItems; i++)
        pDataItems[i] = m_dataItems.Get(i);

    bool initLocals = (m_methodInfo->options & CORINFO_OPT_INIT_LOCALS) != 0;

    InterpMethod *pMethod = new InterpMethod(m_methodHnd, m_totalVarsStackSize, pDataItems, initLocals);

    return pMethod;
}

int32_t* InterpCompiler::GetCode(int32_t *pCodeSize)
{
    *pCodeSize = m_methodCodeSize;
    return m_pMethodCode;
}

InterpCompiler::InterpCompiler(COMP_HANDLE compHnd,
                                CORINFO_METHOD_INFO* methodInfo)
    : m_pInitLocalsIns(nullptr)
    , m_globalVarsWithRefsStackTop(0)
{
    // Fill in the thread-local used for assertions
    t_InterpJitInfoTls = compHnd;

    m_methodHnd = methodInfo->ftn;
    m_compScopeHnd = methodInfo->scope;
    m_compHnd = compHnd;
    m_methodInfo = methodInfo;

#ifdef DEBUG

    m_classHnd   = compHnd->getMethodClass(m_methodHnd);

    m_methodName = ::PrintMethodName(compHnd, m_classHnd, m_methodHnd, &m_methodInfo->args,
                            /* includeAssembly */ false,
                            /* includeClass */ true,
                            /* includeClassInstantiation */ true,
                            /* includeMethodInstantiation */ true,
                            /* includeSignature */ true,
                            /* includeReturnType */ false,
                            /* includeThis */ false);

    if (InterpConfig.InterpDump().contains(compHnd, m_methodHnd, m_classHnd, &m_methodInfo->args))
        m_verbose = true;
#endif
}

InterpMethod* InterpCompiler::CompileMethod()
{
#ifdef DEBUG
    if (m_verbose || InterpConfig.InterpList())
    {
        printf("Interpreter compile method %s\n", m_methodName.GetUnderlyingArray());
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
    PatchInitLocals(m_methodInfo);

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

void InterpCompiler::PatchInitLocals(CORINFO_METHOD_INFO* methodInfo)
{
    // We may have global vars containing managed pointers or interior pointers, so we need
    //  to zero the region of the stack containing global vars, not just IL locals. Now that
    //  offset allocation has occurred we know where the global vars end, so we can expand
    //  the initlocals opcode that was originally generated to also zero them.
    int32_t startOffset = m_pInitLocalsIns->data[0];
    int32_t totalSize = m_globalVarsWithRefsStackTop - startOffset;
    if (totalSize > m_pInitLocalsIns->data[1])
    {
        INTERP_DUMP(
            "Expanding initlocals from [%d-%d] to [%d-%d]\n",
            startOffset, startOffset + m_pInitLocalsIns->data[1],
            startOffset, startOffset + totalSize
        );
        m_pInitLocalsIns->data[1] = totalSize;
    }
    else
    {
        INTERP_DUMP(
            "Not expanding initlocals from [%d-%d] for global vars stack top of %d\n",
            startOffset, startOffset + m_pInitLocalsIns->data[1],
            m_globalVarsWithRefsStackTop
        );
    }
}

// Adds a conversion instruction for the value pointed to by sp, also updating the stack information
void InterpCompiler::EmitConv(StackInfo *sp, StackType type, InterpOpcode convOp)
{
    InterpInst *newInst = AddIns(convOp);

    newInst->SetSVar(sp->var);
    int32_t var = CreateVarExplicit(g_interpTypeFromStackType[type], NULL, INTERP_STACK_SLOT_SIZE);
    new (sp) StackInfo(type, NULL, var);
    newInst->SetDVar(var);

    // NOTE: We rely on m_pLastNewIns == newInst upon return from this function. Make sure you preserve that if you change anything.
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
            assert(!"Unimplemented CorInfoType");
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
    bool hasParamArg = m_methodInfo->args.hasTypeArg();
    int paramArgIndex = hasParamArg ? hasThis ? 1 : 0 : INT_MAX;
    int32_t offset;
    int numArgs = hasThis + m_methodInfo->args.numArgs;
    int numILLocals = m_methodInfo->locals.numArgs;
    m_numILVars = numArgs + numILLocals;

    // add some starting extra space for new vars
    m_varsCapacity = m_numILVars + m_methodInfo->EHcount + 64;
    m_pVars = (InterpVar*)AllocTemporary0(m_varsCapacity * sizeof (InterpVar));
    m_varsSize = m_numILVars + hasParamArg;

    offset = 0;

    INTERP_DUMP("\nCreate IL Vars:\n");

    // NOTE: There is special handling for the param arg, which is stored after the IL locals in the m_pVars array.
    // The param arg is not part of the set of arguments defined by the IL method signature, but instead is needed
    // to support shared generics codegen, and to be able to determine which exact intantiation of a method is in use.
    // The param arg is stashed into the m_pVars array at an unnatural index relative to its position in the physical stack
    // so that when parsing the MSIL byte stream it is simple to determine the index of the normal argumentes
    // and IL locals, by just knowing the number of IL defined arguments. This allows all of the special handling for
    // the param arg to be localized to this function, and the small set of helper functions that directly use it.

    CORINFO_ARG_LIST_HANDLE sigArg = m_methodInfo->args.args;

    int argIndexOffset = 0;
    if (hasThis)
    {
        CORINFO_CLASS_HANDLE argClass = m_compHnd->getMethodClass(m_methodInfo->ftn);
        InterpType interpType = m_compHnd->isValueClass(argClass) ? InterpTypeByRef : InterpTypeO;
        CreateNextLocalVar(0, argClass, interpType, &offset);
        argIndexOffset++;
    }

    if (hasParamArg)
    {
        m_paramArgIndex = m_varsSize - 1; // The param arg is stored after the IL locals in the m_pVars array
        CreateNextLocalVar(m_paramArgIndex, NULL, InterpTypeI, &offset);
    }

    for (int i = argIndexOffset; i < numArgs; i++)
    {
        CORINFO_CLASS_HANDLE argClass;
        CorInfoType argCorType = strip(m_compHnd->getArgType(&m_methodInfo->args, sigArg, &argClass));
        InterpType interpType = GetInterpType(argCorType);
        sigArg = m_compHnd->getArgNext(sigArg);
        CreateNextLocalVar(i, argClass, interpType, &offset);
    }
    offset = ALIGN_UP_TO(offset, INTERP_STACK_ALIGNMENT);

    sigArg = m_methodInfo->locals.args;
    m_ILLocalsOffset = offset;
    int index = numArgs;

    for (int i = 0; i < numILLocals; i++) {
        CORINFO_CLASS_HANDLE argClass;
        CorInfoType argCorType = strip(m_compHnd->getArgType(&m_methodInfo->locals, sigArg, &argClass));
        InterpType interpType = GetInterpType(argCorType);
        CreateNextLocalVar(index, argClass, interpType, &offset);
        sigArg = m_compHnd->getArgNext(sigArg);
        index++;
    }

    if (hasParamArg)
    {
        // The param arg is stored after the IL locals in the m_pVars array
        assert(index == m_paramArgIndex);
        index++;
    }

    offset = ALIGN_UP_TO(offset, INTERP_STACK_ALIGNMENT);
    m_ILLocalsSize = offset - m_ILLocalsOffset;

    INTERP_DUMP("\nCreate clause Vars:\n");

    m_clauseVarsIndex = index;

    for (unsigned int i = 0; i < m_methodInfo->EHcount; i++)
    {
        CreateNextLocalVar(index, NULL, InterpTypeO, &offset);
        index++;
    }

    m_totalVarsStackSize = offset;
}

void InterpCompiler::CreateNextLocalVar(int iArgToSet, CORINFO_CLASS_HANDLE argClass, InterpType interpType, int32_t *pOffset)
{
    int32_t align;
    int32_t size = GetInterpTypeStackSize(argClass, interpType, &align);

    new (&m_pVars[iArgToSet]) InterpVar(interpType, argClass, size);

    m_pVars[iArgToSet].global = true;
    m_pVars[iArgToSet].ILGlobal = true;
    m_pVars[iArgToSet].size = size;
    *pOffset = ALIGN_UP_TO(*pOffset, align);
    m_pVars[iArgToSet].offset = *pOffset;
    INTERP_DUMP("alloc arg var %d to offset %d\n", iArgToSet, *pOffset);
    *pOffset += size;
}

// Create finally call island basic blocks for all try regions with finally clauses that the leave exits.
// That means when the leaveOffset is inside the try region and the target is outside of it.
// These finally call island blocks are used for non-exceptional finally execution.
// The linked list of finally call island blocks is stored in the pFinallyCallIslandBB field of the finally basic block.
// The pFinallyCallIslandBB in the actual finally call island block points to the outer try region's finally call island block.
void InterpCompiler::CreateFinallyCallIslandBasicBlocks(CORINFO_METHOD_INFO* methodInfo, int32_t leaveOffset, InterpBasicBlock* pLeaveTargetBB)
{
    bool firstFinallyCallIsland = true;
    InterpBasicBlock* pInnerFinallyCallIslandBB = NULL;
    for (unsigned int i = 0; i < methodInfo->EHcount; i++)
    {
        CORINFO_EH_CLAUSE clause;
        m_compHnd->getEHinfo(methodInfo->ftn, i, &clause);
        if (clause.Flags != CORINFO_EH_CLAUSE_FINALLY)
        {
            continue;
        }

        // Only try regions in which the leave instruction is located are considered.
        if ((uint32_t)leaveOffset < clause.TryOffset || (uint32_t)leaveOffset > (clause.TryOffset + clause.TryLength))
        {
            continue;
        }

        // If the leave target is inside the try region, we don't need to create a finally call island block.
        if ((uint32_t)pLeaveTargetBB->ilOffset >= clause.TryOffset && (uint32_t)pLeaveTargetBB->ilOffset <= (clause.TryOffset + clause.TryLength))
        {
            continue;
        }

        InterpBasicBlock* pHandlerBB = GetBB(clause.HandlerOffset);
        InterpBasicBlock* pFinallyCallIslandBB = NULL;

        InterpBasicBlock** ppLastBBNext = &pHandlerBB->pFinallyCallIslandBB;
        while (*ppLastBBNext != NULL)
        {
            if ((*ppLastBBNext)->pLeaveTargetBB == pLeaveTargetBB)
            {
                // We already have finally call island block for the leave target
                pFinallyCallIslandBB = (*ppLastBBNext);
                break;
            }
            ppLastBBNext = &((*ppLastBBNext)->pNextBB);
        }

        if (pFinallyCallIslandBB == NULL)
        {
            pFinallyCallIslandBB = AllocBB(clause.HandlerOffset + clause.HandlerLength);
            pFinallyCallIslandBB->pLeaveTargetBB = pLeaveTargetBB;
            *ppLastBBNext = pFinallyCallIslandBB;
        }

        if (pInnerFinallyCallIslandBB != NULL)
        {
            pInnerFinallyCallIslandBB->pFinallyCallIslandBB = pFinallyCallIslandBB;
        }
        pInnerFinallyCallIslandBB = pFinallyCallIslandBB;

        if (firstFinallyCallIsland)
        {
            // The leaves table entry points to the first finally call island block
            firstFinallyCallIsland = false;

            LeavesTableEntry leavesEntry;
            leavesEntry.ilOffset = leaveOffset;
            leavesEntry.pFinallyCallIslandBB = pFinallyCallIslandBB;
            m_leavesTable.Add(leavesEntry);
        }
    }
}

void InterpCompiler::CreateBasicBlocks(CORINFO_METHOD_INFO* methodInfo)
{
    int32_t codeSize = methodInfo->ILCodeSize;
    uint8_t *codeStart = methodInfo->ILCode;
    uint8_t *codeEnd = codeStart + codeSize;
    const uint8_t *ip = codeStart;

    m_ppOffsetToBB = (InterpBasicBlock**)AllocMemPool0(sizeof(InterpBasicBlock*) * (methodInfo->ILCodeSize + 1));
    GetBB(0);

    while (ip < codeEnd)
    {
        int32_t insOffset = (int32_t)(ip - codeStart);
        OPCODE opcode = CEEDecodeOpcode(&ip);
        OPCODE_FORMAT opArgs = g_CEEOpArgs[opcode];
        int32_t target;
        InterpBasicBlock *pTargetBB;

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
                BADCODE("ShortInlineBrTarget out of bounds");
            pTargetBB = GetBB(target);
            if (opcode == CEE_LEAVE_S)
            {
                CreateFinallyCallIslandBasicBlocks(methodInfo, insOffset, pTargetBB);
            }
            ip += 2;
            GetBB((int32_t)(ip - codeStart));
            break;
        case InlineBrTarget:
            target = insOffset + 5 + getI4LittleEndian(ip + 1);
            if (target >= codeSize)
                BADCODE("Branch target out of bounds");
            pTargetBB = GetBB(target);
            if (opcode == CEE_LEAVE)
            {
                CreateFinallyCallIslandBasicBlocks(methodInfo, insOffset, pTargetBB);
            }
            ip += 5;
            GetBB((int32_t)(ip - codeStart));
            break;
        case InlineSwitch: {
            uint32_t n = getI4LittleEndian(ip + 1);
            ip += 5;
            insOffset += 5 + 4 * n;
            target = insOffset;
            if (target >= codeSize)
                BADCODE("Switch instruction is too big");
            GetBB(target);
            for (uint32_t i = 0; i < n; i++)
            {
                target = insOffset + getI4LittleEndian(ip);
                if (target >= codeSize)
                    BADCODE("Switch target out of bounds");
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
}

void InterpCompiler::InitializeClauseBuildingBlocks(CORINFO_METHOD_INFO* methodInfo)
{
    int32_t codeSize = methodInfo->ILCodeSize;
    uint8_t *codeStart = methodInfo->ILCode;
    uint8_t *codeEnd = codeStart + codeSize;

    for (unsigned int i = 0; i < methodInfo->EHcount; i++)
    {
        CORINFO_EH_CLAUSE clause;
        m_compHnd->getEHinfo(methodInfo->ftn, i, &clause);

        if ((codeStart + clause.TryOffset) > codeEnd ||
                (codeStart + clause.TryOffset + clause.TryLength) > codeEnd)
        {
            BADCODE("Invalid try region in EH clause");
        }

        InterpBasicBlock* pTryBB = GetBB(clause.TryOffset);

        if ((codeStart + clause.HandlerOffset) > codeEnd ||
                (codeStart + clause.HandlerOffset + clause.HandlerLength) > codeEnd)
        {
            BADCODE("Invalid handler region in EH clause");
        }

        // Find and mark all basic blocks that are part of the try region.
        for (uint32_t j = clause.TryOffset; j < (clause.TryOffset + clause.TryLength); j++)
        {
            InterpBasicBlock* pBB = m_ppOffsetToBB[j];
            if (pBB != NULL && pBB->clauseType == BBClauseNone)
            {
                pBB->clauseType = BBClauseTry;
            }
        }

        InterpBasicBlock* pHandlerBB = GetBB(clause.HandlerOffset);

        // Find and mark all basic blocks that are part of the handler region.
        for (uint32_t j = clause.HandlerOffset; j < (clause.HandlerOffset + clause.HandlerLength); j++)
        {
            InterpBasicBlock* pBB = m_ppOffsetToBB[j];
            if (pBB != NULL && pBB->clauseType == BBClauseNone)
            {
                if ((clause.Flags == CORINFO_EH_CLAUSE_NONE) || (clause.Flags == CORINFO_EH_CLAUSE_FILTER))
                {
                    pBB->clauseType = BBClauseCatch;
                }
                else
                {
                    assert((clause.Flags == CORINFO_EH_CLAUSE_FINALLY) || (clause.Flags == CORINFO_EH_CLAUSE_FAULT));
                    pBB->clauseType = BBClauseFinally;
                }
            }
        }

        if (clause.Flags == CORINFO_EH_CLAUSE_FILTER)
        {
            if ((codeStart + clause.FilterOffset) > codeEnd)
                BADCODE("Invalid filter region in EH clause");

            // The filter funclet is always stored right before its handler funclet.
            // So the filter end offset is equal to the start offset of the handler funclet.
            InterpBasicBlock* pFilterBB = GetBB(clause.FilterOffset);
            pFilterBB->isFilterOrCatchFuncletEntry = true;
            pFilterBB->clauseVarIndex = m_clauseVarsIndex + i;

            // Initialize the filter stack state. It initially contains the exception object.
            pFilterBB->stackHeight = 1;
            pFilterBB->pStackState = (StackInfo*)AllocMemPool(sizeof (StackInfo));
            new (pFilterBB->pStackState) StackInfo(StackTypeO, NULL, pFilterBB->clauseVarIndex);

            // Find and mark all basic blocks that are part of the filter region.
            for (uint32_t j = clause.FilterOffset; j < clause.HandlerOffset; j++)
            {
                InterpBasicBlock* pBB = m_ppOffsetToBB[j];
                if (pBB != NULL && pBB->clauseType == BBClauseNone)
                {
                    pBB->clauseType = BBClauseFilter;
                }
            }
        }
        else if (clause.Flags == CORINFO_EH_CLAUSE_FINALLY|| clause.Flags == CORINFO_EH_CLAUSE_FAULT)
        {
            InterpBasicBlock* pFinallyBB = GetBB(clause.HandlerOffset);

            // Initialize finally handler stack state to empty.
            pFinallyBB->stackHeight = 0;
        }

        if (clause.Flags == CORINFO_EH_CLAUSE_NONE || clause.Flags == CORINFO_EH_CLAUSE_FILTER)
        {
            InterpBasicBlock* pCatchBB = GetBB(clause.HandlerOffset);
            pCatchBB->isFilterOrCatchFuncletEntry = true;
            pCatchBB->clauseVarIndex = m_clauseVarsIndex + i;

            // Initialize the catch / filtered handler stack state. It initially contains the exception object.
            pCatchBB->stackHeight = 1;
            pCatchBB->pStackState = (StackInfo*)AllocMemPool(sizeof (StackInfo));
            new (pCatchBB->pStackState) StackInfo(StackTypeO, NULL, pCatchBB->clauseVarIndex);
        }
    }

    // Now that we have classified all the basic blocks, we can set the clause type for the finally call island blocks.
    // We set it to the same type as the basic block after the finally handler.
    for (unsigned int i = 0; i < methodInfo->EHcount; i++)
    {
        CORINFO_EH_CLAUSE clause;
        m_compHnd->getEHinfo(methodInfo->ftn, i, &clause);

        if (clause.Flags != CORINFO_EH_CLAUSE_FINALLY)
        {
            continue;
        }

        InterpBasicBlock* pFinallyBB = GetBB(clause.HandlerOffset);

        InterpBasicBlock* pFinallyCallIslandBB = pFinallyBB->pFinallyCallIslandBB;
        while (pFinallyCallIslandBB != NULL)
        {
            InterpBasicBlock* pAfterFinallyBB = m_ppOffsetToBB[clause.HandlerOffset + clause.HandlerLength];
            assert(pAfterFinallyBB != NULL);
            pFinallyCallIslandBB->clauseType = pAfterFinallyBB->clauseType;
            pFinallyCallIslandBB = pFinallyCallIslandBB->pNextBB;
        }
    }
}

void InterpCompiler::EmitBranchToBB(InterpOpcode opcode, InterpBasicBlock *pTargetBB)
{
    EmitBBEndVarMoves(pTargetBB);
    InitBBStackState(pTargetBB);

    AddIns(opcode);
    m_pLastNewIns->info.pTargetBB = pTargetBB;
}

// ilOffset represents relative branch offset
void InterpCompiler::EmitBranch(InterpOpcode opcode, int32_t ilOffset)
{
    int32_t target = (int32_t)(m_ip - m_pILCode) + ilOffset;
    if (target < 0 || target >= m_ILCodeSize)
        assert(0);

    // Backwards branch, emit safepoint
    if (ilOffset < 0)
        AddIns(INTOP_SAFEPOINT);

    InterpBasicBlock *pTargetBB = m_ppOffsetToBB[target];
    assert(pTargetBB != NULL);

    EmitBranchToBB(opcode, pTargetBB);
}

void InterpCompiler::EmitOneArgBranch(InterpOpcode opcode, int32_t ilOffset, int insSize)
{
    CHECK_STACK(1);
    StackType argType = (m_pStackPointer[-1].type == StackTypeO || m_pStackPointer[-1].type == StackTypeByRef) ? StackTypeI : m_pStackPointer[-1].type;
    // offset the opcode to obtain the type specific I4/I8/R4/R8 variant.
    InterpOpcode opcodeArgType = (InterpOpcode)(opcode + argType - StackTypeI4);
    m_pStackPointer--;
    if (ilOffset)
    {
        EmitBranch(opcodeArgType, ilOffset + insSize);
        m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
    }
    else
    {
        AddIns(INTOP_NOP);
    }
}

void InterpCompiler::EmitTwoArgBranch(InterpOpcode opcode, int32_t ilOffset, int insSize)
{
    CHECK_STACK(2);
    StackType argType1 = (m_pStackPointer[-1].type == StackTypeO || m_pStackPointer[-1].type == StackTypeByRef) ? StackTypeI : m_pStackPointer[-1].type;
    StackType argType2 = (m_pStackPointer[-2].type == StackTypeO || m_pStackPointer[-2].type == StackTypeByRef) ? StackTypeI : m_pStackPointer[-2].type;

    // Since branch opcodes only compare args of the same type, handle implicit conversions before
    // emitting the conditional branch
    if (argType1 == StackTypeI4 && argType2 == StackTypeI8)
    {
        EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_I4);
        argType1 = StackTypeI8;
    }
    else if (argType1 == StackTypeI8 && argType2 == StackTypeI4)
    {
        EmitConv(m_pStackPointer - 2, StackTypeI8, INTOP_CONV_I8_I4);
    }
    else if (argType1 == StackTypeR4 && argType2 == StackTypeR8)
    {
        EmitConv(m_pStackPointer - 1, StackTypeR8, INTOP_CONV_R8_R4);
        argType1 = StackTypeR8;
    }
    else if (argType1 == StackTypeR8 && argType2 == StackTypeR4)
    {
        EmitConv(m_pStackPointer - 2, StackTypeR8, INTOP_CONV_R8_R4);
    }
    else if (argType1 != argType2)
    {
        BADCODE("Branch compare args must be of the same type");
    }

    // offset the opcode to obtain the type specific I4/I8/R4/R8 variant.
    InterpOpcode opcodeArgType = (InterpOpcode)(opcode + argType1 - StackTypeI4);
    m_pStackPointer -= 2;

    if (ilOffset)
    {
        EmitBranch(opcodeArgType, ilOffset + insSize);
        m_pLastNewIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
    }
    else
    {
        AddIns(INTOP_NOP);
    }
}

void InterpCompiler::EmitLoadVar(int32_t var)
{
    InterpType interpType = m_pVars[var].interpType;
    CORINFO_CLASS_HANDLE clsHnd = m_pVars[var].clsHnd;

    if (m_pCBB->clauseType == BBClauseFilter)
    {
        assert(m_pVars[var].ILGlobal);
        AddIns(INTOP_LOAD_FRAMEVAR);
        PushInterpType(InterpTypeI, NULL);
        m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
        EmitLdind(interpType, clsHnd, m_pVars[var].offset);
        return;
    }

    int32_t size = m_pVars[var].size;

    if (interpType == InterpTypeVT)
        PushTypeVT(clsHnd, size);
    else
        PushInterpType(interpType, clsHnd);

    AddIns(InterpGetMovForType(interpType, true));
    m_pLastNewIns->SetSVar(var);
    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
    if (interpType == InterpTypeVT)
        m_pLastNewIns->data[0] = size;
}

void InterpCompiler::EmitStoreVar(int32_t var)
{
    InterpType interpType = m_pVars[var].interpType;
    CHECK_STACK(1);

    if (m_pCBB->clauseType == BBClauseFilter)
    {
        AddIns(INTOP_LOAD_FRAMEVAR);
        PushInterpType(InterpTypeI, NULL);
        m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
        EmitStind(interpType, m_pVars[var].clsHnd, m_pVars[var].offset, true /* reverseSVarOrder */);
        return;
    }

#ifdef TARGET_64BIT
    // nint and int32 can be used interchangeably. Add implicit conversions.
    if (m_pStackPointer[-1].type == StackTypeI4 && g_stackTypeFromInterpType[interpType] == StackTypeI8)
        EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_I4);
#endif
    if (m_pStackPointer[-1].type == StackTypeR4 && g_stackTypeFromInterpType[interpType] == StackTypeR8)
        EmitConv(m_pStackPointer - 1, StackTypeR8, INTOP_CONV_R8_R4);
    else if (m_pStackPointer[-1].type == StackTypeR8 && g_stackTypeFromInterpType[interpType] == StackTypeR4)
        EmitConv(m_pStackPointer - 1, StackTypeR4, INTOP_CONV_R4_R8);

    m_pStackPointer--;
    AddIns(InterpGetMovForType(interpType, false));
    m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
    m_pLastNewIns->SetDVar(var);
    if (interpType == InterpTypeVT)
        m_pLastNewIns->data[0] = m_pVars[var].size;
}

void InterpCompiler::EmitBinaryArithmeticOp(int32_t opBase)
{
    CHECK_STACK(2);
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
                EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_I4);
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
                EmitConv(m_pStackPointer - 2, StackTypeI8, INTOP_CONV_I8_I4);
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
            EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_I4);
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
#if TARGET_64BIT
        if (type1 == StackTypeI8 && type2 == StackTypeI4)
        {
            EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_I4);
            type2 = StackTypeI8;
        }
        else if (type1 == StackTypeI4 && type2 == StackTypeI8)
        {
            EmitConv(m_pStackPointer - 2, StackTypeI8, INTOP_CONV_I8_I4);
            type1 = StackTypeI8;
        }
#endif
        if (type1 == StackTypeR8 && type2 == StackTypeR4)
        {
            EmitConv(m_pStackPointer - 1, StackTypeR8, INTOP_CONV_R8_R4);
            type2 = StackTypeR8;
        }
        else if (type1 == StackTypeR4 && type2 == StackTypeR8)
        {
            EmitConv(m_pStackPointer - 2, StackTypeR8, INTOP_CONV_R8_R4);
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
    m_pLastNewIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
    PushStackType(typeRes, NULL);
    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
}

void InterpCompiler::EmitUnaryArithmeticOp(int32_t opBase)
{
    CHECK_STACK(1);
    StackType stackType = m_pStackPointer[-1].type;
    int32_t finalOpcode = opBase + (stackType - StackTypeI4);

    if (stackType == StackTypeByRef || stackType == StackTypeO)
        INVALID_CODE_RET_VOID;
    if (opBase == INTOP_NOT_I4 && (stackType != StackTypeI4 && stackType != StackTypeI8))
        INVALID_CODE_RET_VOID;

    m_pStackPointer--;
    AddIns(finalOpcode);
    m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
    PushStackType(stackType, NULL);
    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
}

void InterpCompiler::EmitShiftOp(int32_t opBase)
{
    CHECK_STACK(2);
    StackType stackType = m_pStackPointer[-2].type;
    StackType shiftAmountType = m_pStackPointer[-1].type;
    int32_t typeOffset = stackType - StackTypeI4;
    int32_t finalOpcode = opBase + typeOffset;

    if ((stackType != StackTypeI4 && stackType != StackTypeI8) ||
            (shiftAmountType != StackTypeI4 && shiftAmountType != StackTypeI))
        INVALID_CODE_RET_VOID;

    m_pStackPointer -= 2;
    AddIns(finalOpcode);
    m_pLastNewIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
    PushStackType(stackType, NULL);
    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
}

void InterpCompiler::EmitCompareOp(int32_t opBase)
{
    CHECK_STACK(2);
    if (m_pStackPointer[-1].type == StackTypeO || m_pStackPointer[-1].type == StackTypeByRef)
    {
        AddIns(opBase + StackTypeI - StackTypeI4);
    }
    else
    {
        if (m_pStackPointer[-1].type == StackTypeR4 && m_pStackPointer[-2].type == StackTypeR8)
            EmitConv(m_pStackPointer - 1, StackTypeR8, INTOP_CONV_R8_R4);
        if (m_pStackPointer[-1].type == StackTypeR8 && m_pStackPointer[-2].type == StackTypeR4)
            EmitConv(m_pStackPointer - 2, StackTypeR8, INTOP_CONV_R8_R4);
        AddIns(opBase + m_pStackPointer[-1].type - StackTypeI4);
    }
    m_pStackPointer -= 2;
    m_pLastNewIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
    PushStackType(StackTypeI4, NULL);
    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
}

int32_t InterpCompiler::GetDataItemIndex(void *data)
{
    int32_t index = m_dataItems.Find(data);
    if (index != -1)
        return index;

    return m_dataItems.Add(data);
}

void* InterpCompiler::GetDataItemAtIndex(int32_t index)
{
    if (index < 0 || index >= m_dataItems.GetSize())
    {
        assert(!"Invalid data item index");
        return NULL;
    }
    return m_dataItems.Get(index);
}

int32_t InterpCompiler::GetMethodDataItemIndex(CORINFO_METHOD_HANDLE mHandle)
{
    return GetDataItemIndex((void*)mHandle);
}

int32_t InterpCompiler::GetDataItemIndexForHelperFtn(CorInfoHelpFunc ftn)
{
    // Interpreter-TODO: Find an existing data item index for this helper if possible and reuse it
    CORINFO_CONST_LOOKUP ftnLookup;
    m_compHnd->getHelperFtn(ftn, &ftnLookup);
    void* addr = ftnLookup.addr;
    if (ftnLookup.accessType == IAT_PVALUE)
    {
        addr = (void*)((size_t)addr | INTERP_INDIRECT_HELPER_TAG);
    }
    assert(ftnLookup.accessType == IAT_VALUE || ftnLookup.accessType == IAT_PVALUE);

    return GetDataItemIndex(addr);
}

bool InterpCompiler::EmitCallIntrinsics(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO sig)
{
    const char *className = NULL;
    const char *namespaceName = NULL;
    const char *methodName = m_compHnd->getMethodNameFromMetadata(method, &className, &namespaceName, NULL, 0);

    if (namespaceName && !strcmp(namespaceName, "System"))
    {
        if (className && !strcmp(className, "Environment"))
        {
            if (methodName && !strcmp(methodName, "FailFast"))
            {
                AddIns(INTOP_FAILFAST); // to be removed, not really an intrisic
                m_pStackPointer--;
                return true;
            }
        }
        else if (className && !strcmp(className, "Object"))
        {
            // This is needed at this moment because we don't have support for interop
            // with compiled code, but it might make sense in the future for this to remain
            // in order to avoid redundant interp to jit transition.
            if (methodName && !strcmp(methodName, ".ctor"))
            {
                AddIns(INTOP_NOP);
                m_pStackPointer--;
                return true;
            }
        }
        else if (className && !strcmp(className, "GC"))
        {
            if (methodName && !strcmp(methodName, "Collect"))
            {
                AddIns(INTOP_GC_COLLECT);
                // Not reducing the stack pointer because we expect the version with no arguments
                return true;
            }
        }
        // TODO: Add multi-dimensional array getters and setters
    }

    return false;
}

void InterpCompiler::ResolveToken(uint32_t token, CorInfoTokenKind tokenKind, CORINFO_RESOLVED_TOKEN *pResolvedToken)
{
    pResolvedToken->tokenScope = m_compScopeHnd;
    pResolvedToken->tokenContext = METHOD_BEING_COMPILED_CONTEXT();
    pResolvedToken->token = token;
    pResolvedToken->tokenType = tokenKind;
    m_compHnd->resolveToken(pResolvedToken);
}

CORINFO_METHOD_HANDLE InterpCompiler::ResolveMethodToken(uint32_t token)
{
    CORINFO_RESOLVED_TOKEN resolvedToken;

    ResolveToken(token, CORINFO_TOKENKIND_Method, &resolvedToken);

    return resolvedToken.hMethod;
}

CORINFO_CLASS_HANDLE InterpCompiler::ResolveClassToken(uint32_t token)
{
    CORINFO_RESOLVED_TOKEN resolvedToken;

    ResolveToken(token, CORINFO_TOKENKIND_Class, &resolvedToken);

    return resolvedToken.hClass;
}

CORINFO_CLASS_HANDLE InterpCompiler::getClassFromContext(CORINFO_CONTEXT_HANDLE context)
{
    if (context == METHOD_BEING_COMPILED_CONTEXT())
    {
        return m_compHnd->getMethodClass(m_methodHnd); // This really should be just a field access, but we don't have that field in the InterpCompiler now
    }

    if (((SIZE_T)context & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS)
    {
        return CORINFO_CLASS_HANDLE((SIZE_T)context & ~CORINFO_CONTEXTFLAGS_MASK);
    }
    else
    {
        return m_compHnd->getMethodClass(CORINFO_METHOD_HANDLE((SIZE_T)context & ~CORINFO_CONTEXTFLAGS_MASK));
    }
}

int InterpCompiler::getParamArgIndex()
{
    return m_paramArgIndex;
}

InterpCompiler::InterpEmbedGenericResult InterpCompiler::EmitGenericHandle(CORINFO_RESOLVED_TOKEN* resolvedToken, GenericHandleEmbedOptions options)
{
    CORINFO_GENERICHANDLE_RESULT embedInfo;
    InterpEmbedGenericResult result;
    m_compHnd->embedGenericHandle(resolvedToken, HasFlag(options, GenericHandleEmbedOptions::EmbedParent), m_methodInfo->ftn, &embedInfo);
    if (HasFlag(options, GenericHandleEmbedOptions::VarOnly) || embedInfo.lookup.lookupKind.needsRuntimeLookup)
    {
        result.var = EmitGenericHandleAsVar(embedInfo);
    }
    else
    {
        assert(embedInfo.lookup.constLookup.accessType == IAT_VALUE);
        result.dataItemIndex = GetDataItemIndex(embedInfo.lookup.constLookup.handle);
    }
    return result;
}

void InterpCompiler::EmitPushCORINFO_LOOKUP(const CORINFO_LOOKUP& lookup)
{
    PushStackType(StackTypeI, NULL);
    int resultVar = m_pStackPointer[-1].var;

    CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind = lookup.lookupKind.runtimeLookupKind;
    if (runtimeLookupKind == CORINFO_LOOKUP_METHODPARAM)
    {
        AddIns(INTOP_GENERICLOOKUP_METHOD);
    }
    else if (runtimeLookupKind == CORINFO_LOOKUP_THISOBJ)
    {
        AddIns(INTOP_GENERICLOOKUP_THIS);
    }
    else
    {
        AddIns(INTOP_GENERICLOOKUP_CLASS);
    }
    CORINFO_RUNTIME_LOOKUP *pRuntimeLookup = (CORINFO_RUNTIME_LOOKUP*)AllocMethodData(sizeof(CORINFO_RUNTIME_LOOKUP));
    *pRuntimeLookup = lookup.runtimeLookup;
    m_pLastNewIns->data[0] = GetDataItemIndex(pRuntimeLookup);

    m_pLastNewIns->SetSVar(getParamArgIndex());
    m_pLastNewIns->SetDVar(resultVar);
}

int InterpCompiler::EmitGenericHandleAsVar(const CORINFO_GENERICHANDLE_RESULT &embedInfo)
{
    PushStackType(StackTypeI, NULL);
    int resultVar = m_pStackPointer[-1].var;
    m_pStackPointer--;

    if (embedInfo.lookup.lookupKind.needsRuntimeLookup)
    {
        CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind = embedInfo.lookup.lookupKind.runtimeLookupKind;
        if (runtimeLookupKind == CORINFO_LOOKUP_METHODPARAM)
        {
            AddIns(INTOP_GENERICLOOKUP_METHOD);
        }
        else if (runtimeLookupKind == CORINFO_LOOKUP_THISOBJ)
        {
            AddIns(INTOP_GENERICLOOKUP_THIS);
        }
        else
        {
            AddIns(INTOP_GENERICLOOKUP_CLASS);
        }
        CORINFO_RUNTIME_LOOKUP *pRuntimeLookup = (CORINFO_RUNTIME_LOOKUP*)AllocMethodData(sizeof(CORINFO_RUNTIME_LOOKUP));
        *pRuntimeLookup = embedInfo.lookup.runtimeLookup;
        m_pLastNewIns->data[0] = GetDataItemIndex(pRuntimeLookup);

        m_pLastNewIns->SetSVar(getParamArgIndex());
        m_pLastNewIns->SetDVar(resultVar);
    }
    else
    {
        AddIns(INTOP_LDPTR);
        m_pLastNewIns->SetDVar(resultVar);

        assert(embedInfo.lookup.constLookup.accessType == IAT_VALUE);
        m_pLastNewIns->data[0] = GetDataItemIndex(embedInfo.lookup.constLookup.handle);
    }
    return resultVar;
}

void InterpCompiler::EmitPushLdvirtftn(int thisVar, CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo)
{
    const bool isInterface = (pCallInfo->classFlags & CORINFO_FLG_INTERFACE) == CORINFO_FLG_INTERFACE;

    if ((pCallInfo->methodFlags & CORINFO_FLG_EnC) && !isInterface)
    {
        NO_WAY("Virtual call to a function added via EnC is not supported");
    }

    // Get the exact descriptor for the static callsite
    CORINFO_GENERICHANDLE_RESULT embedInfo;
    m_compHnd->embedGenericHandle(pResolvedToken, true, m_methodInfo->ftn, &embedInfo);
    assert(embedInfo.compileTimeHandle != NULL);
    int typeVar = EmitGenericHandleAsVar(embedInfo);

    m_compHnd->embedGenericHandle(pResolvedToken, false, m_methodInfo->ftn, &embedInfo);
    assert(embedInfo.compileTimeHandle != NULL);
    int methodVar = EmitGenericHandleAsVar(embedInfo);

    CORINFO_METHOD_HANDLE getVirtualFunctionPtrHelper;
    m_compHnd->getHelperFtn(CORINFO_HELP_VIRTUAL_FUNC_PTR, NULL, &getVirtualFunctionPtrHelper);
    assert(getVirtualFunctionPtrHelper != NULL);

    PushInterpType(InterpTypeI, NULL);
    int32_t dVar = m_pStackPointer[-1].var;

    int *callArgs = (int*) AllocMemPool((3 + 1) * sizeof(int));
    callArgs[0] = thisVar;
    callArgs[1] = typeVar;
    callArgs[2] = methodVar;
    callArgs[3] = CALL_ARGS_TERMINATOR;

    AddIns(INTOP_CALL);
    m_pLastNewIns->data[0] = GetMethodDataItemIndex(getVirtualFunctionPtrHelper);
    m_pLastNewIns->SetDVar(dVar);
    m_pLastNewIns->SetSVar(CALL_ARGS_SVAR);
    m_pLastNewIns->flags |= INTERP_INST_FLAG_CALL;
    m_pLastNewIns->info.pCallInfo = (InterpCallInfo*)AllocMemPool0(sizeof (InterpCallInfo));
    m_pLastNewIns->info.pCallInfo->pCallArgs = callArgs;
}

void InterpCompiler::EmitCall(CORINFO_RESOLVED_TOKEN* pConstrainedToken, bool readonly, bool tailcall, bool newObj, bool isCalli)
{
    uint32_t token = getU4LittleEndian(m_ip + 1);
    bool isVirtual = (*m_ip == CEE_CALLVIRT);

    CORINFO_RESOLVED_TOKEN resolvedCallToken;
    CORINFO_CALL_INFO callInfo;
    bool doCallInsteadOfNew = false;

    int callIFunctionPointerVar = -1;
    void* calliCookie = NULL;

    if (isCalli)
    {
        // Suppress uninitialized use warning.
        memset(&resolvedCallToken, 0, sizeof(resolvedCallToken));
        memset(&callInfo, 0, sizeof(callInfo));

        resolvedCallToken.token        = token;
        resolvedCallToken.tokenContext = METHOD_BEING_COMPILED_CONTEXT();
        resolvedCallToken.tokenScope   = m_methodInfo->scope;

        m_compHnd->findSig(m_methodInfo->scope, token, METHOD_BEING_COMPILED_CONTEXT(), &callInfo.sig);

        callIFunctionPointerVar = m_pStackPointer[-1].var;
        m_pStackPointer--;
        calliCookie = m_compHnd->GetCookieForInterpreterCalliSig(&callInfo.sig);
    }
    else
    {
        ResolveToken(token, newObj ? CORINFO_TOKENKIND_NewObj : CORINFO_TOKENKIND_Method, &resolvedCallToken);

        CORINFO_CALLINFO_FLAGS flags = (CORINFO_CALLINFO_FLAGS)(CORINFO_CALLINFO_ALLOWINSTPARAM | CORINFO_CALLINFO_SECURITYCHECKS | CORINFO_CALLINFO_DISALLOW_STUB);
        if (isVirtual)
        flags = (CORINFO_CALLINFO_FLAGS)(flags | CORINFO_CALLINFO_CALLVIRT);

        m_compHnd->getCallInfo(&resolvedCallToken, pConstrainedToken, m_methodInfo->ftn, flags, &callInfo);
        if (EmitCallIntrinsics(callInfo.hMethod, callInfo.sig))
        {
            m_ip += 5;
            return;
        }

        if (callInfo.thisTransform != CORINFO_NO_THIS_TRANSFORM)
        {
            assert(pConstrainedToken != NULL);
            StackInfo *pThisStackInfo = m_pStackPointer - callInfo.sig.numArgs - 1;
            if (callInfo.thisTransform == CORINFO_BOX_THIS)
            {
                EmitBox(pThisStackInfo, pConstrainedToken->hClass, true);
            }
            else
            {
                assert(callInfo.thisTransform == CORINFO_DEREF_THIS);
                AddIns(INTOP_LDIND_I);
                m_pLastNewIns->SetSVar(pThisStackInfo->var);
                m_pLastNewIns->data[0] = 0;
                int32_t var = CreateVarExplicit(InterpTypeO, pConstrainedToken->hClass, INTERP_STACK_SLOT_SIZE);
                new (pThisStackInfo) StackInfo(StackTypeO, pConstrainedToken->hClass, var);
                m_pLastNewIns->SetDVar(pThisStackInfo->var);
            }
        }
    }

    if (newObj && (callInfo.classFlags & CORINFO_FLG_VAROBJSIZE))
    {
        // This is a variable size object which means "System.String".
        // For these, we just call the resolved method directly, but don't actually pass a this pointer to it.
        doCallInsteadOfNew = true;
    }

    // Process sVars
    int numArgsFromStack = callInfo.sig.numArgs + (newObj ? 0 : callInfo.sig.hasThis());
    int newObjThisArgLocation = newObj && !doCallInsteadOfNew ? 0 : INT_MAX;
    int numArgs = numArgsFromStack + (newObjThisArgLocation == 0);
    m_pStackPointer -= numArgsFromStack;

    int extraParamArgLocation = INT_MAX;
    if (callInfo.sig.hasTypeArg())
    {
        extraParamArgLocation = callInfo.sig.hasThis() ? 1 : 0;
        numArgs++;
    }

    int *callArgs = (int*) AllocMemPool((numArgs + 1) * sizeof(int));
    for (int iActualArg = 0, iLogicalArg = 0; iActualArg < numArgs; iActualArg++)
    {
        if (iActualArg == extraParamArgLocation)
        {
            // This is the extra type argument, which is not on the logical IL stack
            // Skip it for now. We will fill it in later.
        }
        else if (iActualArg == newObjThisArgLocation)
        {
            // This is the newObj arg type argument, which is not on the logical IL stack
            // Skip it for now. We will fill it in later.
        }
        else
        {
            callArgs[iActualArg] = m_pStackPointer [iLogicalArg].var;
            iLogicalArg++;
        }
    }
    callArgs[numArgs] = CALL_ARGS_TERMINATOR;

    InterpEmbedGenericResult newObjType;
    int32_t newObjThisVar = -1;
    int32_t newObjDVar = -1;
    InterpType ctorType = InterpTypeO;
    int32_t vtsize = 0;

    if (newObjThisArgLocation != INT_MAX)
    {
        ctorType = GetInterpType(m_compHnd->asCorInfoType(resolvedCallToken.hClass));
        if (ctorType == InterpTypeVT)
        {
            vtsize = m_compHnd->getClassSize(resolvedCallToken.hClass);
            PushTypeVT(resolvedCallToken.hClass, vtsize);
            PushInterpType(InterpTypeByRef, NULL);
        }
        else
        {
            PushInterpType(ctorType, resolvedCallToken.hClass);
            PushInterpType(ctorType, resolvedCallToken.hClass);

            newObjType = EmitGenericHandle(&resolvedCallToken, GenericHandleEmbedOptions::EmbedParent);
        }
        newObjDVar = m_pStackPointer[-2].var;
        newObjThisVar = m_pStackPointer[-1].var;
        m_pStackPointer--;
        // Consider this arg as being defined, although newobj defines it
        AddIns(INTOP_DEF);
        m_pLastNewIns->SetDVar(newObjThisVar);

        callArgs[newObjThisArgLocation] = newObjThisVar;
    }

    if (extraParamArgLocation != INT_MAX)
    {
        int contextParamVar = -1;

        // Instantiated generic method
        CORINFO_CONTEXT_HANDLE exactContextHnd = callInfo.contextHandle;
        if (((SIZE_T)exactContextHnd & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_METHOD)
        {
            assert(exactContextHnd != METHOD_BEING_COMPILED_CONTEXT());

            CORINFO_METHOD_HANDLE exactMethodHandle =
            (CORINFO_METHOD_HANDLE)((SIZE_T)exactContextHnd & ~CORINFO_CONTEXTFLAGS_MASK);

            if (!callInfo.exactContextNeedsRuntimeLookup)
            {
                PushStackType(StackTypeI, NULL);
                m_pStackPointer--;
                contextParamVar = m_pStackPointer[0].var;
                AddIns(INTOP_LDPTR);
                m_pLastNewIns->SetDVar(contextParamVar);
                m_pLastNewIns->data[0] = GetDataItemIndex((void*)exactMethodHandle);
            }
            else
            {
                contextParamVar = EmitGenericHandle(&resolvedCallToken, GenericHandleEmbedOptions::VarOnly).var;
            }
        }

        // otherwise must be an instance method in a generic struct,
        // a static method in a generic type, or a runtime-generated array method
        else
        {
            assert(((SIZE_T)exactContextHnd & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS);
            CORINFO_CLASS_HANDLE exactClassHandle = getClassFromContext(exactContextHnd);

            if ((callInfo.classFlags & CORINFO_FLG_ARRAY) && readonly)
            {
                PushStackType(StackTypeI, NULL);
                m_pStackPointer--;
                contextParamVar = m_pStackPointer[0].var;
                // We indicate "readonly" to the Address operation by using a null
                // instParam.
                AddIns(INTOP_LDPTR);
                m_pLastNewIns->SetDVar(contextParamVar);
                m_pLastNewIns->data[0] = GetDataItemIndex(NULL);
            }
            else if (!callInfo.exactContextNeedsRuntimeLookup)
            {
                PushStackType(StackTypeI, NULL);
                m_pStackPointer--;
                contextParamVar = m_pStackPointer[0].var;
                AddIns(INTOP_LDPTR);
                m_pLastNewIns->SetDVar(contextParamVar);
                m_pLastNewIns->data[0] = GetDataItemIndex((void*)exactClassHandle);
            }
            else
            {
                contextParamVar = EmitGenericHandle(&resolvedCallToken, GenericHandleEmbedOptions::VarOnly | GenericHandleEmbedOptions::EmbedParent).var;
            }
        }
        callArgs[extraParamArgLocation] = contextParamVar;
    }

    // Process dVar
    int32_t dVar;
    if (newObjDVar != -1)
    {
        dVar = newObjDVar;
    }
    else if (doCallInsteadOfNew)
    {
        PushInterpType(InterpTypeO, NULL);
        dVar = m_pStackPointer[-1].var;
    }
    else if (callInfo.sig.retType != CORINFO_TYPE_VOID)
    {
        InterpType interpType = GetInterpType(callInfo.sig.retType);

        if (interpType == InterpTypeVT)
        {
            int32_t size = m_compHnd->getClassSize(callInfo.sig.retTypeClass);
            PushTypeVT(callInfo.sig.retTypeClass, size);
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
    switch (callInfo.kind)
    {
        case CORINFO_CALL:
            if (newObj && !doCallInsteadOfNew)
            {
                if (ctorType == InterpTypeVT)
                {
                    // If this is a newobj for a value type, we need to call the constructor
                    // and then copy the value type to the stack.
                    AddIns(INTOP_NEWOBJ_VT);
                    m_pLastNewIns->data[1] = (int32_t)ALIGN_UP_TO(vtsize, INTERP_STACK_SLOT_SIZE);
                }
                else
                {
                    if (newObjType.var != -1)
                    {
                        // newobj of type known only through a generic dictionary lookup.
                        AddIns(INTOP_NEWOBJ_VAR);
                        m_pLastNewIns->SetSVars2(CALL_ARGS_SVAR, newObjType.var);
                    }
                    else
                    {
                        // Normal newobj call
                        AddIns(INTOP_NEWOBJ);
                        m_pLastNewIns->data[1] = newObjType.dataItemIndex;
                    }
                }
                m_pLastNewIns->data[0] = GetDataItemIndex(callInfo.hMethod);
            }
            else if ((callInfo.classFlags & CORINFO_FLG_ARRAY) && newObj)
            {
                AddIns(INTOP_NEWMDARR);
                m_pLastNewIns->data[0] = GetDataItemIndex(resolvedCallToken.hClass);
                m_pLastNewIns->data[1] = callInfo.sig.numArgs;
            }
            else if (isCalli)
            {
                AddIns(INTOP_CALLI);
                m_pLastNewIns->data[0] = GetDataItemIndex(calliCookie);
                m_pLastNewIns->SetSVars2(CALL_ARGS_SVAR, callIFunctionPointerVar);
            }
            else
            {
                // Normal call
                if (callInfo.nullInstanceCheck)
                {
                    // If the call is a normal call, we need to check for null instance
                    // before the call.
                    // TODO: Add null checking behavior somewhere here!
                }
                AddIns(INTOP_CALL);
                m_pLastNewIns->data[0] = GetMethodDataItemIndex(callInfo.hMethod);
            }
            break;

        case CORINFO_CALL_CODE_POINTER:
        {
            if (callInfo.nullInstanceCheck)
            {
                // If the call is a normal call, we need to check for null instance
                // before the call.
                // TODO: Add null checking behavior somewhere here!
            }

            EmitPushCORINFO_LOOKUP(callInfo.codePointerLookup);
            m_pStackPointer--;
            int codePointerLookupResult = m_pStackPointer[0].var;

            calliCookie = m_compHnd->GetCookieForInterpreterCalliSig(&callInfo.sig);

            AddIns(INTOP_CALLI);
            m_pLastNewIns->data[0] = GetDataItemIndex(calliCookie);
            m_pLastNewIns->SetSVars2(CALL_ARGS_SVAR, codePointerLookupResult);
            break;
        }
        case CORINFO_VIRTUALCALL_VTABLE:
            // Traditional virtual call. In theory we could optimize this to using the vtable
            AddIns(INTOP_CALLVIRT);
            m_pLastNewIns->data[0] = GetDataItemIndex(callInfo.hMethod);
            break;

        case CORINFO_VIRTUALCALL_LDVIRTFTN:
            if (callInfo.sig.sigInst.methInstCount != 0)
            {
                assert(extraParamArgLocation == INT_MAX);
                // We should not have a type argument for the ldvirtftn path since we don't know
                // the exact method to call until the ldvirtftn is resolved, and that only
                // produces a function pointer.

                // We need to copy the this argument to the target function to another var,
                // since var's which are passed to call instructions are destroyed (unless
                // they are global, and there is no reason to promote the this arg to a global)
                AddIns(INTOP_MOV_P);
                m_pLastNewIns->SetSVar(callArgs[0]);
                PushInterpType(InterpTypeO, NULL);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                int thisVarForLdvirtftn = m_pStackPointer[-1].var;
                m_pStackPointer--;

                EmitPushLdvirtftn(thisVarForLdvirtftn, &resolvedCallToken, &callInfo);
                m_pStackPointer--;
                int synthesizedLdvirtftnPtrVar = m_pStackPointer[0].var;

                calliCookie = m_compHnd->GetCookieForInterpreterCalliSig(&callInfo.sig);

                AddIns(INTOP_CALLI);
                m_pLastNewIns->data[0] = GetDataItemIndex(calliCookie);
                m_pLastNewIns->SetSVars2(CALL_ARGS_SVAR, synthesizedLdvirtftnPtrVar);
            }
            else
            {
                AddIns(INTOP_CALLVIRT);
                m_pLastNewIns->data[0] = GetDataItemIndex(callInfo.hMethod);
            }
            break;

        case CORINFO_VIRTUALCALL_STUB:
            // This case should never happen
            assert(!"Unexpected call kind");
        break;
    }

    m_pLastNewIns->SetDVar(dVar);
    m_pLastNewIns->SetSVar(CALL_ARGS_SVAR);

    m_pLastNewIns->flags |= INTERP_INST_FLAG_CALL;
    m_pLastNewIns->info.pCallInfo = (InterpCallInfo*)AllocMemPool0(sizeof (InterpCallInfo));
    m_pLastNewIns->info.pCallInfo->pCallArgs = callArgs;

    m_ip += 5;
}

static int32_t GetLdindForType(InterpType interpType)
{
    switch (interpType)
    {
        case InterpTypeI1: return INTOP_LDIND_I1;
        case InterpTypeU1: return INTOP_LDIND_U1;
        case InterpTypeI2: return INTOP_LDIND_I2;
        case InterpTypeU2: return INTOP_LDIND_U2;
        case InterpTypeI4: return INTOP_LDIND_I4;
        case InterpTypeI8: return INTOP_LDIND_I8;
        case InterpTypeR4: return INTOP_LDIND_R4;
        case InterpTypeR8: return INTOP_LDIND_R8;
        case InterpTypeO: return INTOP_LDIND_I;
        case InterpTypeVT: return INTOP_LDIND_VT;
        case InterpTypeByRef: return INTOP_LDIND_I;
        default:
            assert(0);
    }
    return -1;
}

static int32_t GetStindForType(InterpType interpType)
{
    switch (interpType)
    {
        case InterpTypeI1: return INTOP_STIND_I1;
        case InterpTypeU1: return INTOP_STIND_U1;
        case InterpTypeI2: return INTOP_STIND_I2;
        case InterpTypeU2: return INTOP_STIND_U2;
        case InterpTypeI4: return INTOP_STIND_I4;
        case InterpTypeI8: return INTOP_STIND_I8;
        case InterpTypeR4: return INTOP_STIND_R4;
        case InterpTypeR8: return INTOP_STIND_R8;
        case InterpTypeO: return INTOP_STIND_O;
        case InterpTypeVT: return INTOP_STIND_VT;
        case InterpTypeByRef: return INTOP_STIND_I;
        default:
            assert(0);
    }
    return -1;
}

static int32_t GetStelemForType(InterpType interpType)
{
    switch (interpType)
    {
        case InterpTypeI1: return INTOP_STELEM_I1;
        case InterpTypeU1: return INTOP_STELEM_U1;
        case InterpTypeI2: return INTOP_STELEM_I2;
        case InterpTypeU2: return INTOP_STELEM_U2;
        case InterpTypeI4: return INTOP_STELEM_I4;
        case InterpTypeI8: return INTOP_STELEM_I8;
        case InterpTypeR4: return INTOP_STELEM_R4;
        case InterpTypeR8: return INTOP_STELEM_R8;
        case InterpTypeO: return INTOP_STELEM_REF;
        default:
            assert(0);
    }
    return -1;
}

void InterpCompiler::EmitLdind(InterpType interpType, CORINFO_CLASS_HANDLE clsHnd, int32_t offset)
{
    // Address is at the top of the stack
    m_pStackPointer--;
    int32_t opcode = GetLdindForType(interpType);
    AddIns(opcode);
    m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
    m_pLastNewIns->data[0] = offset;
    if (interpType == InterpTypeVT)
    {
        int size = m_compHnd->getClassSize(clsHnd);
        m_pLastNewIns->data[1] = size;
        PushTypeVT(clsHnd, size);
    }
    else
    {
        PushInterpType(interpType, NULL);
    }
    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
}

void InterpCompiler::EmitStind(InterpType interpType, CORINFO_CLASS_HANDLE clsHnd, int32_t offset, bool reverseSVarOrder)
{
    // stack contains address and then the value to be stored
    // or in the reverse order if the flag is set
    if (interpType == InterpTypeVT)
    {
        if (m_compHnd->getClassAttribs(clsHnd) & CORINFO_FLG_CONTAINS_GC_PTR)
        {
            AddIns(INTOP_STIND_VT);
            m_pLastNewIns->data[1] = GetDataItemIndex(clsHnd);
        }
        else
        {
            AddIns(INTOP_STIND_VT_NOREF);
            m_pLastNewIns->data[1] = m_compHnd->getClassSize(clsHnd);
        }
    }
    else
    {
        AddIns(GetStindForType(interpType));
    }

    m_pLastNewIns->data[0] = offset;

    m_pStackPointer -= 2;
    if (reverseSVarOrder)
        m_pLastNewIns->SetSVars2(m_pStackPointer[1].var, m_pStackPointer[0].var);
    else
        m_pLastNewIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);

}

void InterpCompiler::EmitLdelem(int32_t opcode, InterpType interpType)
{
    m_pStackPointer -= 2;
    AddIns(opcode);
    m_pLastNewIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
    PushInterpType(interpType, NULL);
    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
}

void InterpCompiler::EmitStelem(InterpType interpType)
{
    m_pStackPointer -= 3;
    int32_t opcode = GetStelemForType(interpType);
    AddIns(opcode);
    m_pLastNewIns->SetSVars3(m_pStackPointer[0].var, m_pStackPointer[1].var, m_pStackPointer[2].var);
}

void InterpCompiler::EmitStaticFieldAddress(CORINFO_FIELD_INFO *pFieldInfo, CORINFO_RESOLVED_TOKEN *pResolvedToken)
{
    bool isBoxedStatic  = (pFieldInfo->fieldFlags & CORINFO_FLG_FIELD_STATIC_IN_HEAP) != 0;
    switch (pFieldInfo->fieldAccessor)
    {
        case CORINFO_FIELD_STATIC_ADDRESS:
        case CORINFO_FIELD_STATIC_RVA_ADDRESS:
        {
            // const field address
            assert(pFieldInfo->fieldLookup.accessType == IAT_VALUE);
            AddIns(INTOP_LDPTR);
            PushInterpType(InterpTypeByRef, NULL);
            m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
            m_pLastNewIns->data[0] = GetDataItemIndex(pFieldInfo->fieldLookup.addr);
            break;
        }
        case CORINFO_FIELD_STATIC_TLS_MANAGED:
        case CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER:
        {
            void *helperArg = NULL;
            switch (pFieldInfo->helper)
            {
                case CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED:
                case CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED2:
                case CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED2_NOJITOPT:
                    helperArg = (void*)(size_t)m_compHnd->getThreadLocalFieldInfo(pResolvedToken->hField, false);
                    break;
                case CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED:
                    helperArg = (void*)(size_t)m_compHnd->getThreadLocalFieldInfo(pResolvedToken->hField, true);
                    break;
                case CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR:
                case CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE_NOCTOR:
                case CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE:
                case CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE:
                    helperArg = (void*)m_compHnd->getClassThreadStaticDynamicInfo(pResolvedToken->hClass);
                    break;
                default:
                    // TODO
                    assert(0);
                    break;
            }
            // Call helper to obtain thread static base address
            AddIns(INTOP_CALL_HELPER_PP);
            m_pLastNewIns->data[0] = GetDataItemIndexForHelperFtn(pFieldInfo->helper);
            m_pLastNewIns->data[1] = GetDataItemIndex(helperArg);
            PushInterpType(InterpTypeByRef, NULL);
            m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);

            // Add field offset
            m_pStackPointer--;
            AddIns(INTOP_ADD_P_IMM);
            m_pLastNewIns->data[0] = (int32_t)pFieldInfo->offset;
            m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
            PushInterpType(InterpTypeByRef, NULL);
            m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
            break;
        }
        case CORINFO_FIELD_INTRINSIC_EMPTY_STRING:
        {
            void *emptyString;
            InfoAccessType iat = m_compHnd->emptyStringLiteral(&emptyString);
            assert(iat == IAT_VALUE);
            AddIns(INTOP_LDPTR);
            PushInterpType(InterpTypeO, NULL);
            m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
            m_pLastNewIns->data[0] = GetDataItemIndex(emptyString);
            break;
        }
        default:
            // TODO
            assert(0);
            break;
    }

    if (isBoxedStatic)
    {
        // Obtain boxed instance ref
        m_pStackPointer--;
        AddIns(INTOP_LDIND_I);
        m_pLastNewIns->data[0] = 0;
        m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
        PushInterpType(InterpTypeO, NULL);
        m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);

        // Skip method table word
        m_pStackPointer--;
        AddIns(INTOP_ADD_P_IMM);
        m_pLastNewIns->data[0] = sizeof(void*);
        m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
        PushInterpType(InterpTypeByRef, NULL);
        m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
    }
}

void InterpCompiler::EmitStaticFieldAccess(InterpType interpFieldType, CORINFO_FIELD_INFO *pFieldInfo, CORINFO_RESOLVED_TOKEN *pResolvedToken, bool isLoad)
{
    EmitStaticFieldAddress(pFieldInfo, pResolvedToken);
    if (isLoad)
        EmitLdind(interpFieldType, pFieldInfo->structType, 0);
    else
        EmitStind(interpFieldType, pFieldInfo->structType, 0, true);
}

void InterpCompiler::EmitLdLocA(int32_t var)
{
    if (m_pCBB->clauseType == BBClauseFilter)
    {
        AddIns(INTOP_LOAD_FRAMEVAR);
        PushInterpType(InterpTypeI, NULL);
        m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
        AddIns(INTOP_ADD_P_IMM);
        m_pLastNewIns->data[0] = m_pVars[var].offset;
        m_pLastNewIns->SetSVar(m_pStackPointer[-1].var);
        m_pStackPointer--;
        PushInterpType(InterpTypeByRef, NULL);
        m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
        return;
    }

    AddIns(INTOP_LDLOCA);
    m_pLastNewIns->SetSVar(var);
    PushInterpType(InterpTypeByRef, NULL);
    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
}

void InterpCompiler::EmitBox(StackInfo* pStackInfo, CORINFO_CLASS_HANDLE clsHnd, bool argByRef)
{
    CORINFO_CLASS_HANDLE boxedClsHnd = m_compHnd->getTypeForBox(clsHnd);
    CorInfoHelpFunc helpFunc = m_compHnd->getBoxHelper(clsHnd);
    AddIns(argByRef ? INTOP_BOX_PTR : INTOP_BOX);
    m_pLastNewIns->SetSVar(pStackInfo->var);

    int32_t var = CreateVarExplicit(InterpTypeO, boxedClsHnd, INTERP_STACK_SLOT_SIZE);
    new (pStackInfo) StackInfo(StackTypeO, boxedClsHnd, var);

    m_pLastNewIns->SetDVar(pStackInfo->var);
    m_pLastNewIns->data[0] = GetDataItemIndex(clsHnd);
    m_pLastNewIns->data[1] = GetDataItemIndexForHelperFtn(helpFunc);
}

void InterpCompiler::GenerateCode(CORINFO_METHOD_INFO* methodInfo)
{
    bool readonly = false;
    bool tailcall = false;
    bool volatile_ = false;
    CORINFO_RESOLVED_TOKEN* pConstrainedToken = NULL;
    CORINFO_RESOLVED_TOKEN constrainedToken;
    CORINFO_CALL_INFO callInfo;
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

    InterpBasicBlock *pFirstFuncletBB = NULL;
    InterpBasicBlock *pLastFuncletBB = NULL;

    CreateBasicBlocks(methodInfo);
    InitializeClauseBuildingBlocks(methodInfo);

    m_currentILOffset = -1;

#if DEBUG
    if (InterpConfig.InterpHalt().contains(m_compHnd, m_methodHnd, m_classHnd, &m_methodInfo->args))
        AddIns(INTOP_BREAKPOINT);
#endif

    // We need to always generate this opcode because even if we have no IL locals, we may have
    //  global vars which contain managed pointers or interior pointers
    m_pInitLocalsIns = AddIns(INTOP_INITLOCALS);
    // if (methodInfo->options & CORINFO_OPT_INIT_LOCALS)
    // FIXME: We can't currently skip zeroing locals because we don't have accurate liveness for global refs and byrefs
    m_pInitLocalsIns->data[0] = m_ILLocalsOffset;
    m_pInitLocalsIns->data[1] = m_ILLocalsSize;

    codeEnd = m_ip + m_ILCodeSize;

    // Safepoint at each method entry. This could be done as part of a call, rather than
    // adding an opcode.
    AddIns(INTOP_SAFEPOINT);

    linkBBlocks = true;
    needsRetryEmit = false;

retry_emit:
    emittedBBlocks = false;
    while (m_ip < codeEnd)
    {
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

            InterpBasicBlock *pPrevBB = m_pCBB;

            pPrevBB = GenerateCodeForFinallyCallIslands(pNewBB, pPrevBB);

            if (!pPrevBB->pNextBB)
            {
                INTERP_DUMP("Chaining BB%d -> BB%d\n" , pPrevBB->index, pNewBB->index);
                pPrevBB->pNextBB = pNewBB;
            }

            m_pCBB = pNewBB;
            if (m_pCBB->isFilterOrCatchFuncletEntry && (m_pCBB->emitState == BBStateEmitting))
            {
                AddIns(INTOP_LOAD_EXCEPTION);
                m_pLastNewIns->SetDVar(m_pCBB->clauseVarIndex);
            }
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
                m_pLastNewIns->data[0] = opcode - CEE_LDC_I4_0;
                PushStackType(StackTypeI4, NULL);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                m_ip++;
                break;
            case CEE_LDC_I4_S:
                AddIns(INTOP_LDC_I4);
                m_pLastNewIns->data[0] = (int8_t)m_ip[1];
                PushStackType(StackTypeI4, NULL);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                m_ip += 2;
                break;
            case CEE_LDC_I4:
                AddIns(INTOP_LDC_I4);
                m_pLastNewIns->data[0] = getI4LittleEndian(m_ip + 1);
                PushStackType(StackTypeI4, NULL);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                m_ip += 5;
                break;
            case CEE_LDC_I8:
            {
                int64_t val = getI8LittleEndian(m_ip + 1);
                AddIns(INTOP_LDC_I8);
                PushInterpType(InterpTypeI8, NULL);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                m_pLastNewIns->data[0] = (int32_t)val;
                m_pLastNewIns->data[1] = (int32_t)(val >> 32);
                m_ip += 9;
                break;
            }
            case CEE_LDC_R4:
            {
                int32_t val = getI4LittleEndian(m_ip + 1);
                AddIns(INTOP_LDC_R4);
                PushInterpType(InterpTypeR4, NULL);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                m_pLastNewIns->data[0] = val;
                m_ip += 5;
                break;
            }
            case CEE_LDC_R8:
            {
                int64_t val = getI8LittleEndian(m_ip + 1);
                AddIns(INTOP_LDC_R8);
                PushInterpType(InterpTypeR8, NULL);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                m_pLastNewIns->data[0] = (int32_t)val;
                m_pLastNewIns->data[1] = (int32_t)(val >> 32);
                m_ip += 9;
                break;
            }
            case CEE_LDNULL:
                AddIns(INTOP_LDNULL);
                PushStackType(StackTypeO, NULL);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                m_ip++;
                break;
            case CEE_LDSTR:
            {
                int32_t token = getI4LittleEndian(m_ip + 1);
                void *str;
                InfoAccessType accessType = m_compHnd->constructStringLiteral(m_compScopeHnd, token, &str);
                assert(accessType == IAT_VALUE);
                // str should be forever pinned, so we can include its ref inside interpreter code
                AddIns(INTOP_LDPTR);
                PushInterpType(InterpTypeO, m_compHnd->getBuiltinClass(CLASSID_STRING));
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                m_pLastNewIns->data[0] = GetDataItemIndex(str);
                m_ip += 5;
                break;
            }
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
            case CEE_LDARGA_S:
                EmitLdLocA(m_ip[1]);
                m_ip += 2;
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
            case CEE_LDLOCA_S:
                EmitLdLocA(numArgs + m_ip[1]);
                m_ip += 2;
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

            case CEE_LDOBJ:
            case CEE_STOBJ:
            {
                CHECK_STACK(*m_ip == CEE_LDOBJ ? 1 : 2);
                CORINFO_RESOLVED_TOKEN resolvedToken;
                ResolveToken(getU4LittleEndian(m_ip + 1), CORINFO_TOKENKIND_Class, &resolvedToken);
                InterpType interpType = GetInterpType(m_compHnd->asCorInfoType(resolvedToken.hClass));
                if (*m_ip == CEE_LDOBJ)
                {
                    EmitLdind(interpType, resolvedToken.hClass, 0);
                }
                else
                {
                    EmitStind(interpType, resolvedToken.hClass, 0, false);
                }
                m_ip += 5;
                break;
            }

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
                    m_pLastNewIns->SetSVar(retVar);
                    m_pLastNewIns->data[0] = m_pVars[retVar].size;
                }
                else
                {
                    CHECK_STACK(1);
                    AddIns(INTOP_RET);
                    m_pStackPointer--;
                    m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
                }
                m_ip++;
                break;
            }
            case CEE_CONV_U1:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_U1_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_U1_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_U1_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_U1_I8);
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
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_I1_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_I1_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_I1_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_I1_I8);
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
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_U2_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_U2_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_U2_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_U2_I8);
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
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_I2_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_I2_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_I2_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_I2_I8);
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
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_U8_R8);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_U4_R8);
#endif
                    break;
                case StackTypeR4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_U8_R4);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_U4_R4);
#endif
                    break;
                case StackTypeI4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_I8_U4);
#endif
                    break;
                case StackTypeI8:
#ifndef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_MOV_8);
#endif
                    break;
                case StackTypeByRef:
                case StackTypeO:
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_MOV_8);
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
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_I8_R8);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_I4_R8);
#endif
                    break;
                case StackTypeR4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_I8_R4);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_I4_R4);
#endif
                    break;
                case StackTypeI4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_I8_I4);
#endif
                    break;
                case StackTypeO:
                case StackTypeByRef:
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_MOV_8);
                    break;
                case StackTypeI8:
#ifndef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_MOV_8);
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
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_U4_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_U4_R8);
                    break;
                case StackTypeI4:
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_MOV_8);
                    break;
                case StackTypeByRef:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_MOV_P);
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
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_I4_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_I4_R8);
                    break;
                case StackTypeI4:
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_MOV_8);
                    break;
                case StackTypeByRef:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_MOV_P);
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
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_R8);
                    break;
                case StackTypeI4: {
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_I4);
                    break;
                }
                case StackTypeI8:
                    break;
                case StackTypeByRef:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_MOV_8);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_I4);
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
                    EmitConv(m_pStackPointer - 1, StackTypeR4, INTOP_CONV_R4_R8);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeR4, INTOP_CONV_R4_I8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeR4, INTOP_CONV_R4_I4);
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
                    EmitConv(m_pStackPointer - 1, StackTypeR8, INTOP_CONV_R8_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeR8, INTOP_CONV_R8_I8);
                    break;
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeR8, INTOP_CONV_R8_R4);
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
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_U4);
                    break;
                case StackTypeI8:
                    break;
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_U8_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_U8_R8);
                    break;
                case StackTypeByRef:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_MOV_8);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_U4);
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
                    EmitConv(m_pStackPointer - 1, StackTypeR8, INTOP_CONV_R8_R4);
                    break;
                case StackTypeR8:
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeR8, INTOP_CONV_R_UN_I8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeR8, INTOP_CONV_R_UN_I4);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_OVF_I1:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I1_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I1_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I1_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I1_I8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_OVF_U1:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U1_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U1_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U1_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U1_I8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_OVF_I2:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I2_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I2_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I2_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I2_I8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_OVF_U2:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U2_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U2_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U2_I4);
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U2_I8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_OVF_I4:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I4_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I4_R8);
                    break;
                case StackTypeI4:
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_I4_I8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_OVF_U4:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U4_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U4_R8);
                    break;
                case StackTypeI4:
                    break;
                case StackTypeI8:
                    EmitConv(m_pStackPointer - 1, StackTypeI4, INTOP_CONV_OVF_U4_I8);
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_OVF_I8:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_OVF_I8_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_OVF_I8_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_I4);
                    break;
                case StackTypeI8:
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_OVF_U8:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_OVF_U8_R4);
                    break;
                case StackTypeR8:
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_OVF_U8_R8);
                    break;
                case StackTypeI4:
                    EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_CONV_I8_U4);
                    break;
                case StackTypeI8:
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_OVF_I:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_OVF_I8_R4);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_OVF_I4_R4);
#endif
                    break;
                case StackTypeR8:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_OVF_I8_R8);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_OVF_I4_R8);
#endif
                    break;
                case StackTypeI4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_I8_I4);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_MOV_4);
#endif
                    break;
                case StackTypeI8:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_MOV_8);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_OVF_I4_I8);
#endif
                    break;
                default:
                    assert(0);
                }
                m_ip++;
                break;
            case CEE_CONV_OVF_U:
                CHECK_STACK(1);
                switch (m_pStackPointer[-1].type)
                {
                case StackTypeR4:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_OVF_U8_R4);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_OVF_U4_R4);
#endif
                    break;
                case StackTypeR8:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_OVF_U8_R8);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_OVF_U4_R8);
#endif
                    break;
                case StackTypeI4:
#ifdef TARGET_64BIT
                    // FIXME: Is this the right conv opcode?
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_I8_I4);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_MOV_4);
#endif
                    break;
                case StackTypeI8:
#ifdef TARGET_64BIT
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_MOV_8);
#else
                    EmitConv(m_pStackPointer - 1, StackTypeI, INTOP_CONV_OVF_U4_I8);
#endif
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
                m_pLastNewIns->data[0] = n;
                m_ip += 4;
                const uint8_t *nextIp = m_ip + n * 4;
                m_pStackPointer--;
                m_pLastNewIns->SetSVar(m_pStackPointer->var);
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
                m_pLastNewIns->info.ppTargetBBTable = targetBBTable;
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
            case CEE_ADD_OVF:
                EmitBinaryArithmeticOp(INTOP_ADD_OVF_I4);
                m_ip++;
                break;
            case CEE_ADD_OVF_UN:
                EmitBinaryArithmeticOp(INTOP_ADD_OVF_UN_I4);
                m_ip++;
                break;
            case CEE_SUB:
                EmitBinaryArithmeticOp(INTOP_SUB_I4);
                m_ip++;
                break;
            case CEE_SUB_OVF:
                EmitBinaryArithmeticOp(INTOP_SUB_OVF_I4);
                m_ip++;
                break;
            case CEE_SUB_OVF_UN:
                EmitBinaryArithmeticOp(INTOP_SUB_OVF_UN_I4);
                m_ip++;
                break;
            case CEE_MUL:
                EmitBinaryArithmeticOp(INTOP_MUL_I4);
                m_ip++;
                break;
            case CEE_MUL_OVF:
                EmitBinaryArithmeticOp(INTOP_MUL_OVF_I4);
                m_ip++;
                break;
            case CEE_MUL_OVF_UN:
                EmitBinaryArithmeticOp(INTOP_MUL_OVF_UN_I4);
                m_ip++;
                break;
            case CEE_DIV:
                EmitBinaryArithmeticOp(INTOP_DIV_I4);
                m_ip++;
                break;
            case CEE_DIV_UN:
                EmitBinaryArithmeticOp(INTOP_DIV_UN_I4);
                m_ip++;
                break;
            case CEE_REM:
                EmitBinaryArithmeticOp(INTOP_REM_I4);
                m_ip++;
                break;
            case CEE_REM_UN:
                EmitBinaryArithmeticOp(INTOP_REM_UN_I4);
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
            case CEE_CALLVIRT:
            case CEE_CALL:
                EmitCall(pConstrainedToken, readonly, tailcall, false /*newObj*/, false /*isCalli*/);
                pConstrainedToken = NULL;
                readonly = false;
                tailcall = false;
                break;
            case CEE_CALLI:
                EmitCall(NULL /*pConstrainedToken*/, false /* readonly*/, false /* tailcall*/, false /*newObj*/, true /*isCalli*/);
                pConstrainedToken = NULL;
                readonly = false;
                tailcall = false;
                break;
            case CEE_NEWOBJ:
            {
                EmitCall(NULL /*pConstrainedToken*/, false /* readonly*/, false /* tailcall*/, true /*newObj*/, false /*isCalli*/);
                pConstrainedToken = NULL;
                readonly = false;
                tailcall = false;
                break;
            }
            case CEE_DUP:
            {
                CHECK_STACK(1);
                int32_t svar = m_pStackPointer[-1].var;
                InterpType interpType = m_pVars[svar].interpType;
                if (interpType == InterpTypeVT)
                {
                    int32_t size = m_pVars[svar].size;
                    AddIns(INTOP_MOV_VT);
                    m_pLastNewIns->SetSVar(svar);
                    PushTypeVT(m_pVars[svar].clsHnd, size);
                    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                    m_pLastNewIns->data[0] = size;
                }
                else
                {
                    AddIns(InterpGetMovForType(interpType, false));
                    m_pLastNewIns->SetSVar(svar);
                    PushInterpType(interpType, m_pVars[svar].clsHnd);
                    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                }
                m_ip++;
                break;
            }
            case CEE_POP:
                CHECK_STACK(1);
                AddIns(INTOP_NOP);
                m_pStackPointer--;
                m_ip++;
                break;
            case CEE_LDFLDA:
            {
                CORINFO_RESOLVED_TOKEN resolvedToken;
                CORINFO_FIELD_INFO fieldInfo;
                uint32_t token = getU4LittleEndian(m_ip + 1);
                ResolveToken(token, CORINFO_TOKENKIND_Field, &resolvedToken);
                m_compHnd->getFieldInfo(&resolvedToken, m_methodHnd, CORINFO_ACCESS_ADDRESS, &fieldInfo);

                bool isStatic = !!(fieldInfo.fieldFlags & CORINFO_FLG_FIELD_STATIC);

                if (isStatic)
                {
                    // Pop unused object reference
                    m_pStackPointer--;
                    EmitStaticFieldAddress(&fieldInfo, &resolvedToken);
                }
                else
                {
                    assert(fieldInfo.fieldAccessor == CORINFO_FIELD_INSTANCE);
                    m_pStackPointer--;
                    AddIns(INTOP_LDFLDA);
                    m_pLastNewIns->data[0] = (int32_t)fieldInfo.offset;
                    m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
                    PushInterpType(InterpTypeByRef, NULL);
                    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                }

                m_ip += 5;
                break;
            }
            case CEE_LDFLD:
            {
                CHECK_STACK(1);
                CORINFO_RESOLVED_TOKEN resolvedToken;
                CORINFO_FIELD_INFO fieldInfo;
                uint32_t token = getU4LittleEndian(m_ip + 1);
                ResolveToken(token, CORINFO_TOKENKIND_Field, &resolvedToken);
                m_compHnd->getFieldInfo(&resolvedToken, m_methodHnd, CORINFO_ACCESS_GET, &fieldInfo);

                CorInfoType fieldType = fieldInfo.fieldType;
                bool isStatic = !!(fieldInfo.fieldFlags & CORINFO_FLG_FIELD_STATIC);
                InterpType interpFieldType = GetInterpType(fieldType);

                if (isStatic)
                {
                    // Pop unused object reference
                    m_pStackPointer--;
                    EmitStaticFieldAccess(interpFieldType, &fieldInfo, &resolvedToken, true);
                }
                else
                {
                    assert(fieldInfo.fieldAccessor == CORINFO_FIELD_INSTANCE);
                    m_pStackPointer--;
                    int sizeDataIndexOffset = 0;
                    if (m_pStackPointer[0].type == StackTypeVT)
                    {
                        sizeDataIndexOffset = 1;
                        AddIns(INTOP_MOV_SRC_OFF);
                        m_pLastNewIns->data[1] = interpFieldType;
                    }
                    else
                    {
                        int32_t opcode = GetLdindForType(interpFieldType);
                        AddIns(opcode);
                    }
                    m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
                    m_pLastNewIns->data[0] = (int32_t)fieldInfo.offset;
                    if (interpFieldType == InterpTypeVT)
                    {
                        CORINFO_CLASS_HANDLE fieldClass = fieldInfo.structType;
                        int size = m_compHnd->getClassSize(fieldClass);
                        m_pLastNewIns->data[1 + sizeDataIndexOffset] = size;
                        PushTypeVT(fieldClass, size);
                    }
                    else
                    {
                        PushInterpType(interpFieldType, NULL);
                    }
                    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                }

                m_ip += 5;
                if (volatile_)
                {
                    // Acquire membar
                    AddIns(INTOP_MEMBAR);
                    volatile_ = false;
                }
                break;
            }
            case CEE_STFLD:
            {
                CHECK_STACK(2);
                CORINFO_RESOLVED_TOKEN resolvedToken;
                CORINFO_FIELD_INFO fieldInfo;
                uint32_t token = getU4LittleEndian(m_ip + 1);
                ResolveToken(token, CORINFO_TOKENKIND_Field, &resolvedToken);
                m_compHnd->getFieldInfo(&resolvedToken, m_methodHnd, CORINFO_ACCESS_GET, &fieldInfo);

                CorInfoType fieldType = fieldInfo.fieldType;
                bool isStatic = !!(fieldInfo.fieldFlags & CORINFO_FLG_FIELD_STATIC);
                InterpType interpFieldType = GetInterpType(fieldType);

                if (volatile_)
                {
                    // Release memory barrier
                    AddIns(INTOP_MEMBAR);
                    volatile_ = false;
                }

                if (isStatic)
                {
                    EmitStaticFieldAccess(interpFieldType, &fieldInfo, &resolvedToken, false);
                    // Pop the unused object reference
                    m_pStackPointer--;
                }
                else
                {
                    assert(fieldInfo.fieldAccessor == CORINFO_FIELD_INSTANCE);
                    EmitStind(interpFieldType, fieldInfo.structType, fieldInfo.offset, false);
                }
                m_ip += 5;

                break;
            }
            case CEE_LDSFLDA:
            {
                CORINFO_RESOLVED_TOKEN resolvedToken;
                CORINFO_FIELD_INFO fieldInfo;
                uint32_t token = getU4LittleEndian(m_ip + 1);
                ResolveToken(token, CORINFO_TOKENKIND_Field, &resolvedToken);
                m_compHnd->getFieldInfo(&resolvedToken, m_methodHnd, CORINFO_ACCESS_GET, &fieldInfo);

                EmitStaticFieldAddress(&fieldInfo, &resolvedToken);

                m_ip += 5;
                break;
            }
            case CEE_LDSFLD:
            {
                CORINFO_RESOLVED_TOKEN resolvedToken;
                CORINFO_FIELD_INFO fieldInfo;
                uint32_t token = getU4LittleEndian(m_ip + 1);
                ResolveToken(token, CORINFO_TOKENKIND_Field, &resolvedToken);
                m_compHnd->getFieldInfo(&resolvedToken, m_methodHnd, CORINFO_ACCESS_GET, &fieldInfo);

                CorInfoType fieldType = fieldInfo.fieldType;
                InterpType interpFieldType = GetInterpType(fieldType);

                EmitStaticFieldAccess(interpFieldType, &fieldInfo, &resolvedToken, true);

                if (volatile_)
                {
                    // Acquire memory barrier
                    AddIns(INTOP_MEMBAR);
                    volatile_ = false;
                }
                m_ip += 5;
                break;
            }
            case CEE_STSFLD:
            {
                CHECK_STACK(1);
                CORINFO_RESOLVED_TOKEN resolvedToken;
                CORINFO_FIELD_INFO fieldInfo;
                uint32_t token = getU4LittleEndian(m_ip + 1);
                ResolveToken(token, CORINFO_TOKENKIND_Field, &resolvedToken);
                m_compHnd->getFieldInfo(&resolvedToken, m_methodHnd, CORINFO_ACCESS_GET, &fieldInfo);

                CorInfoType fieldType = fieldInfo.fieldType;
                InterpType interpFieldType = GetInterpType(fieldType);

                if (volatile_)
                {
                    // Release memory barrier
                    AddIns(INTOP_MEMBAR);
                    volatile_ = false;
                }

                EmitStaticFieldAccess(interpFieldType, &fieldInfo, &resolvedToken, false);
                m_ip += 5;
                break;
            }
            case CEE_LDIND_I1:
            case CEE_LDIND_U1:
            case CEE_LDIND_I2:
            case CEE_LDIND_U2:
            case CEE_LDIND_I4:
            case CEE_LDIND_U4:
            case CEE_LDIND_I8:
            case CEE_LDIND_I:
            case CEE_LDIND_R4:
            case CEE_LDIND_R8:
            case CEE_LDIND_REF:
            {
                InterpType interpType = InterpTypeVoid;
                switch(opcode)
                {
                    case CEE_LDIND_I1:
                        interpType = InterpTypeI1;
                        break;
                    case CEE_LDIND_U1:
                        interpType = InterpTypeU1;
                        break;
                    case CEE_LDIND_I2:
                        interpType = InterpTypeI2;
                        break;
                    case CEE_LDIND_U2:
                        interpType = InterpTypeU2;
                        break;
                    case CEE_LDIND_I4:
                    case CEE_LDIND_U4:
                        interpType = InterpTypeI4;
                        break;
                    case CEE_LDIND_I8:
                        interpType = InterpTypeI8;
                        break;
                    case CEE_LDIND_I:
                        interpType = InterpTypeI;
                        break;
                    case CEE_LDIND_R4:
                        interpType = InterpTypeR4;
                        break;
                    case CEE_LDIND_R8:
                        interpType = InterpTypeR8;
                        break;
                    case CEE_LDIND_REF:
                        interpType = InterpTypeO;
                        break;
                    default:
                        assert(0);
                }
                EmitLdind(interpType, NULL, 0);
                if (volatile_)
                {
                    // Acquire memory barrier
                    AddIns(INTOP_MEMBAR);
                    volatile_ = false;
                }
                m_ip++;
                break;
            }
            case CEE_STIND_I1:
            case CEE_STIND_I2:
            case CEE_STIND_I4:
            case CEE_STIND_I8:
            case CEE_STIND_I:
            case CEE_STIND_R4:
            case CEE_STIND_R8:
            case CEE_STIND_REF:
            {
                InterpType interpType = InterpTypeVoid;
                switch(opcode)
                {
                    case CEE_STIND_I1:
                        interpType = InterpTypeI1;
                        break;
                    case CEE_STIND_I2:
                        interpType = InterpTypeI2;
                        break;
                    case CEE_STIND_I4:
                        interpType = InterpTypeI4;
                        break;
                    case CEE_STIND_I8:
                        interpType = InterpTypeI8;
                        break;
                    case CEE_STIND_I:
                        interpType = InterpTypeI;
                        break;
                    case CEE_STIND_R4:
                        interpType = InterpTypeR4;
                        break;
                    case CEE_STIND_R8:
                        interpType = InterpTypeR8;
                        break;
                    case CEE_STIND_REF:
                        interpType = InterpTypeO;
                        break;
                    default:
                        assert(0);
                }
                if (volatile_)
                {
                    // Release memory barrier
                    AddIns(INTOP_MEMBAR);
                    volatile_ = false;
                }
                EmitStind(interpType, NULL, 0, false);
                m_ip++;
                break;
            }
            case CEE_PREFIX1:
                m_ip++;
                switch (*m_ip + 256)
                {
                    case CEE_LDARG:
                        EmitLoadVar(getU2LittleEndian(m_ip + 1));
                        m_ip += 3;
                        break;
                    case CEE_LDARGA:
                        EmitLdLocA(getU2LittleEndian(m_ip + 1));
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
                    case CEE_LDLOCA:
                        EmitLdLocA(numArgs + getU2LittleEndian(m_ip + 1));
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

                        ResolveToken(token, CORINFO_TOKENKIND_Constrained, &constrainedToken);

                        pConstrainedToken = &constrainedToken;
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
                    case CEE_VOLATILE:
                        volatile_ = true;
                        m_ip++;
                        break;
                    case CEE_INITOBJ:
                    {
                        CHECK_STACK(1);
                        CORINFO_CLASS_HANDLE clsHnd = ResolveClassToken(getU4LittleEndian(m_ip + 1));
                        if (m_compHnd->isValueClass(clsHnd))
                        {
                            m_pStackPointer--;
                            AddIns(INTOP_ZEROBLK_IMM);
                            m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
                            m_pLastNewIns->data[0] = m_compHnd->getClassSize(clsHnd);
                        }
                        else
                        {
                            AddIns(INTOP_LDNULL);
                            PushInterpType(InterpTypeO, NULL);
                            m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);

                            AddIns(INTOP_STIND_O);
                            m_pStackPointer -= 2;
                            m_pLastNewIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
                        }
                        m_ip += 5;
                        break;
                    }
                    case CEE_LOCALLOC:
                        CHECK_STACK(1);
#if TARGET_64BIT
                        // Length is natural unsigned int
                        if (m_pStackPointer[-1].type == StackTypeI4)
                        {
                            EmitConv(m_pStackPointer - 1, StackTypeI8, INTOP_MOV_8);
                            m_pStackPointer[-1].type = StackTypeI8;
                        }
#endif
                        AddIns(INTOP_LOCALLOC);
                        m_pStackPointer--;
                        if (m_pStackPointer != m_pStackBase)
                        {
                            BADCODE("CEE_LOCALLOC not at stack base + 1");
                        }

                        m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
                        PushStackType(StackTypeByRef, NULL);
                        m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                        m_ip++;
                        break;
                    case CEE_SIZEOF:
                    {
                        CORINFO_CLASS_HANDLE clsHnd = ResolveClassToken(getU4LittleEndian(m_ip + 1));
                        AddIns(INTOP_LDC_I4);
                        m_pLastNewIns->data[0] = m_compHnd->getClassSize(clsHnd);
                        PushStackType(StackTypeI4, NULL);
                        m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                        m_ip += 5;
                        break;
                    }
                    case CEE_ENDFILTER:
                        AddIns(INTOP_LEAVE_FILTER);
                        m_pStackPointer--;
                        m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
                        m_ip++;
                        linkBBlocks = false;
                        break;
                    case CEE_RETHROW:
                        AddIns(INTOP_RETHROW);
                        m_ip++;
                        linkBBlocks = false;
                        break;

                    case CEE_LDVIRTFTN:
                    {
                        CHECK_STACK(1);
                        CORINFO_RESOLVED_TOKEN resolvedToken;
                        uint32_t token = getU4LittleEndian(m_ip + 1);
                        ResolveToken(token, CORINFO_TOKENKIND_Method, &resolvedToken);

                        memset(&callInfo, 0, sizeof(callInfo));
                        m_compHnd->getCallInfo(&resolvedToken, pConstrainedToken, m_methodInfo->ftn, (CORINFO_CALLINFO_FLAGS)(CORINFO_CALLINFO_SECURITYCHECKS| CORINFO_CALLINFO_LDFTN | CORINFO_CALLINFO_CALLVIRT), &callInfo);
                        pConstrainedToken = NULL;

                        // This check really only applies to intrinsic Array.Address methods
                        if (callInfo.sig.callConv & CORINFO_CALLCONV_PARAMTYPE)
                        {
                            NO_WAY("Currently do not support LDFTN of Parameterized functions");
                        }

                        m_pStackPointer--;
                        int thisVar = m_pStackPointer[0].var;

                        if (callInfo.methodFlags & (CORINFO_FLG_FINAL | CORINFO_FLG_STATIC) || !(callInfo.methodFlags & CORINFO_FLG_VIRTUAL))
                        {
                            goto DO_LDFTN;
                        }
                        
                        EmitPushLdvirtftn(thisVar, &resolvedToken, &callInfo);
                        m_ip += 5;
                        break;
                    }
                    case CEE_LDFTN:
                    {
                        {
                            CORINFO_RESOLVED_TOKEN resolvedToken;
                            uint32_t token = getU4LittleEndian(m_ip + 1);
                            ResolveToken(token, CORINFO_TOKENKIND_Method, &resolvedToken);
                            
                            memset(&callInfo, 0, sizeof(callInfo));
                            m_compHnd->getCallInfo(&resolvedToken, pConstrainedToken, m_methodInfo->ftn, (CORINFO_CALLINFO_FLAGS)(CORINFO_CALLINFO_SECURITYCHECKS| CORINFO_CALLINFO_LDFTN), &callInfo);
                        }
                        pConstrainedToken = NULL;

                        // This check really only applies to intrinsic Array.Address methods
                        if (callInfo.sig.callConv & CORINFO_CALLCONV_PARAMTYPE)
                        {
                            NO_WAY("Currently do not support LDFTN of Parameterized functions");
                        }

DO_LDFTN:
                        if (callInfo.kind == CORINFO_CALL)
                        {
                            CORINFO_CONST_LOOKUP embedInfo;
                            m_compHnd->getFunctionFixedEntryPoint(callInfo.hMethod, true, &embedInfo);

                            switch (embedInfo.accessType)
                            {
                            case IAT_VALUE:
                                AddIns(INTOP_LDPTR);
                                break;
                            case IAT_PVALUE:
                                AddIns(INTOP_LDPTR_DEREF);
                                break;
                            default:
                                assert(!"Unexpected access type for function pointer");
                            }
                            PushInterpType(InterpTypeI, NULL);
                            m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                            m_pLastNewIns->data[0] = GetDataItemIndex(embedInfo.handle);
                        }
                        else
                        {
                            EmitPushCORINFO_LOOKUP(callInfo.codePointerLookup);
                        }

                        m_ip += 5;
                        break;
                    }
                    default:
                    {
                        const uint8_t *ip = m_ip - 1;
                        AssertOpCodeNotImplemented(ip, ip - m_pILCode);
                        break;
                    }
                }
                break;

            case CEE_ENDFINALLY:
            {
                AddIns(INTOP_RET_VOID);
                m_ip++;
                linkBBlocks = false;
                break;
            }
            case CEE_LEAVE:
            case CEE_LEAVE_S:
            {
                int32_t ilOffset = (int32_t)(m_ip - m_pILCode);
                int32_t target = (opcode == CEE_LEAVE) ? ilOffset + 5 + *(int32_t*)(m_ip + 1) : (ilOffset + 2 + (int8_t)m_ip[1]);
                InterpBasicBlock *pTargetBB = m_ppOffsetToBB[target];

                m_pStackPointer = m_pStackBase;

                // The leave will jump:
                // * directly to its target if it doesn't jump out of any try regions with finally.
                // * to a finally call island of the first try region with finally that it jumps out of.

                for (int i = 0; i < m_leavesTable.GetSize(); i++)
                {
                    if (m_leavesTable.Get(i).ilOffset == ilOffset)
                    {
                        // There is a finally call island for this leave, so we will jump to it
                        // instead of the target. The chain of these islands will end up on
                        // the target in the end.
                        // NOTE: we need to use basic block to branch and not an IL offset extracted
                        // from the building block, because the finally call islands share the same IL
                        // offset with another block of original code in front of which it is injected.
                        // The EmitBranch would to that block instead of the finally call island.
                        pTargetBB = m_leavesTable.Get(i).pFinallyCallIslandBB;
                        break;
                    }
                }

                // The leave doesn't jump out of any try region with finally, so we can just emit a branch
                // to the target.
                if (m_pCBB->clauseType == BBClauseCatch)
                {
                    // leave out of catch is different from a leave out of finally. It
                    // exits the catch handler and returns the address of the finally
                    // call island as the continuation address to the EH code.
                    EmitBranchToBB(INTOP_LEAVE_CATCH, pTargetBB);
                }
                else
                {
                    EmitBranchToBB(INTOP_BR, pTargetBB);
                }

                m_ip += (opcode == CEE_LEAVE) ? 5 : 2;
                linkBBlocks = false;
                break;
            }

            case CEE_THROW:
                AddIns(INTOP_THROW);
                m_pLastNewIns->SetSVar(m_pStackPointer[-1].var);
                m_ip += 1;
                linkBBlocks = false;
                break;

            case CEE_BOX:
            {
                CORINFO_CLASS_HANDLE clsHnd = ResolveClassToken(getU4LittleEndian(m_ip + 1));
                CHECK_STACK(1);
                m_pStackPointer -= 1;
                EmitBox(m_pStackPointer, clsHnd, false);
                m_pStackPointer++;
                m_ip += 5;
                break;
            }

            case CEE_UNBOX:
            case CEE_UNBOX_ANY:
            {
                CHECK_STACK(1);
                m_pStackPointer -= 1;
                CORINFO_CLASS_HANDLE clsHnd = ResolveClassToken(getU4LittleEndian(m_ip + 1));
                CorInfoHelpFunc helpFunc = m_compHnd->getUnBoxHelper(clsHnd);
                AddIns(opcode == CEE_UNBOX ? INTOP_UNBOX : INTOP_UNBOX_ANY);
                m_pLastNewIns->SetSVar(m_pStackPointer[0].var);
                if (opcode == CEE_UNBOX)
                    PushStackType(StackTypeI, NULL);
                else
                    PushInterpType(GetInterpType(m_compHnd->asCorInfoType(clsHnd)), clsHnd);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                m_pLastNewIns->data[0] = GetDataItemIndex(clsHnd);
                m_pLastNewIns->data[1] = GetDataItemIndexForHelperFtn(helpFunc);
                m_ip += 5;
                break;
            }
            case CEE_NEWARR:
            {
                CHECK_STACK(1);

                uint32_t token = getU4LittleEndian(m_ip + 1);

                CORINFO_RESOLVED_TOKEN resolvedToken;
                ResolveToken(token, CORINFO_TOKENKIND_Newarr, &resolvedToken);

                CORINFO_CLASS_HANDLE arrayClsHnd = resolvedToken.hClass;
                CorInfoHelpFunc helpFunc = m_compHnd->getNewArrHelper(arrayClsHnd);

                m_pStackPointer--;

                AddIns(INTOP_NEWARR);
                m_pLastNewIns->SetSVar(m_pStackPointer[0].var);

                PushInterpType(InterpTypeO, NULL);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);

                m_pLastNewIns->data[0] = GetDataItemIndex(arrayClsHnd);
                m_pLastNewIns->data[1] = GetDataItemIndexForHelperFtn(helpFunc);

                m_ip += 5;
                break;
            }
            case CEE_LDLEN:
            {
                CHECK_STACK(1);
                EmitLdind(InterpTypeI4, NULL, OFFSETOF__CORINFO_Array__length);
                m_ip++;
                break;
            }
            case CEE_LDELEM_I1:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_I1, InterpTypeI4);
                m_ip++;
                break;
            }
            case CEE_LDELEM_U1:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_U1, InterpTypeI4);
                m_ip++;
                break;
            }
            case CEE_LDELEM_I2:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_I2, InterpTypeI4);
                m_ip++;
                break;
            }
            case CEE_LDELEM_U2:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_U2, InterpTypeI4);
                m_ip++;
                break;
            }
            case CEE_LDELEM_I4:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_I4, InterpTypeI4);
                m_ip++;
                break;
            }
            case CEE_LDELEM_U4:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_I4, InterpTypeI4);
                m_ip++;
                break;
            }
            case CEE_LDELEM_I8:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_I8, InterpTypeI8);
                m_ip++;
                break;
            }
            case CEE_LDELEM_I:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_I, InterpTypeI);
                m_ip++;
                break;
            }
            case CEE_LDELEM_R4:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_R4, InterpTypeR4);
                m_ip++;
                break;
            }
            case CEE_LDELEM_R8:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_R8, InterpTypeR8);
                m_ip++;
                break;
            }
            case CEE_LDELEM_REF:
            {
                CHECK_STACK(2);
                EmitLdelem(INTOP_LDELEM_REF, InterpTypeO);
                m_ip++;
                break;
            }
            case CEE_LDELEM:
            {
                CHECK_STACK(2);

                uint32_t token = getU4LittleEndian(m_ip + 1);
                CORINFO_RESOLVED_TOKEN resolvedToken;
                ResolveToken(token, CORINFO_TOKENKIND_Class, &resolvedToken);

                CORINFO_CLASS_HANDLE elemClsHnd = resolvedToken.hClass;
                CorInfoType elemCorType = m_compHnd->asCorInfoType(elemClsHnd);
                InterpType elemInterpType = GetInterpType(elemCorType);

                switch (elemInterpType)
                {
                    case InterpTypeI1:
                        EmitLdelem(INTOP_LDELEM_I1, InterpTypeI4);
                        break;
                    case InterpTypeU1:
                        EmitLdelem(INTOP_LDELEM_U1, InterpTypeI4);
                        break;
                    case InterpTypeI2:
                        EmitLdelem(INTOP_LDELEM_I2, InterpTypeI4);
                        break;
                    case InterpTypeU2:
                        EmitLdelem(INTOP_LDELEM_U2, InterpTypeI4);
                        break;
                    case InterpTypeI4:
                        EmitLdelem(INTOP_LDELEM_I4, InterpTypeI4);
                        break;
                    case InterpTypeI8:
                        EmitLdelem(INTOP_LDELEM_I8, InterpTypeI8);
                        break;
                    case InterpTypeR4:
                        EmitLdelem(INTOP_LDELEM_R4, InterpTypeR4);
                        break;
                    case InterpTypeR8:
                        EmitLdelem(INTOP_LDELEM_R8, InterpTypeR8);
                        break;
                    case InterpTypeO:
                        EmitLdelem(INTOP_LDELEM_REF, InterpTypeO);
                        break;
                    case InterpTypeVT:
                    {
                        int size = m_compHnd->getClassSize(elemClsHnd);
                        m_pStackPointer -= 2;
                        AddIns(INTOP_LDELEM_VT);
                        m_pLastNewIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
                        PushTypeVT(elemClsHnd, size);
                        m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                        m_pLastNewIns->data[0] = size;
                        break;
                    }
                    default:
                        BADCODE("Unsupported element type for LDELEM");
                }

                m_ip += 5;
                break;
            }
            case CEE_LDELEMA:
            {
                // TODO: Support multi-dimensional arrays
                CHECK_STACK(2);

                uint32_t token = getU4LittleEndian(m_ip + 1);
                CORINFO_RESOLVED_TOKEN resolvedToken;
                ResolveToken(token, CORINFO_TOKENKIND_Class, &resolvedToken);

                CORINFO_CLASS_HANDLE elemClsHnd = resolvedToken.hClass;
                CorInfoType elemCorType = m_compHnd->asCorInfoType(elemClsHnd);

                m_pStackPointer -= 2;
                if (elemCorType == CORINFO_TYPE_CLASS)
                {
                    AddIns(INTOP_LDELEMA_REF);
                    m_pLastNewIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
                    PushInterpType(InterpTypeByRef, elemClsHnd);
                    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                    m_pLastNewIns->data[0] = m_compHnd->getClassSize(elemClsHnd);
                    m_pLastNewIns->data[1] = GetDataItemIndex(elemClsHnd);
                }
                else
                {
                    AddIns(INTOP_LDELEMA);
                    m_pLastNewIns->SetSVars2(m_pStackPointer[0].var, m_pStackPointer[1].var);
                    PushInterpType(InterpTypeByRef, elemClsHnd);
                    m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                    m_pLastNewIns->data[0] = m_compHnd->getClassSize(elemClsHnd);
                }

                m_ip += 5;
                break;
            }
            case CEE_STELEM_I:
            {
                CHECK_STACK(3);
                EmitStelem(InterpTypeI);
                m_ip++;
                break;
            }
            case CEE_STELEM_I1:
            {
                CHECK_STACK(3);
                EmitStelem(InterpTypeI1);
                m_ip++;
                break;
            }
            case CEE_STELEM_I2:
            {
                CHECK_STACK(3);
                EmitStelem(InterpTypeI2);
                m_ip++;
                break;
            }
            case CEE_STELEM_I4:
            {
                CHECK_STACK(3);
                EmitStelem(InterpTypeI4);
                m_ip++;
                break;
            }
            case CEE_STELEM_I8:
            {
                CHECK_STACK(3);
                EmitStelem(InterpTypeI8);
                m_ip++;
                break;
            }
            case CEE_STELEM_R4:
            {
                CHECK_STACK(3);
                EmitStelem(InterpTypeR4);
                m_ip++;
                break;
            }
            case CEE_STELEM_R8:
            {
                CHECK_STACK(3);
                EmitStelem(InterpTypeR8);
                m_ip++;
                break;
            }
            case CEE_STELEM_REF:
            {
                CHECK_STACK(3);
                EmitStelem(InterpTypeO);
                m_ip++;
                break;
            }
            case CEE_STELEM:
            {
                CHECK_STACK(3);

                uint32_t token = getU4LittleEndian(m_ip + 1);
                CORINFO_RESOLVED_TOKEN resolvedToken;
                ResolveToken(token, CORINFO_TOKENKIND_Class, &resolvedToken);

                CORINFO_CLASS_HANDLE elemClsHnd = resolvedToken.hClass;
                CorInfoType elemCorType = m_compHnd->asCorInfoType(elemClsHnd);
                InterpType elemInterpType = GetInterpType(elemCorType);

                switch (elemInterpType)
                {
                    case InterpTypeI1:
                        EmitStelem(InterpTypeI1);
                        break;
                    case InterpTypeU1:
                        EmitStelem(InterpTypeU1);
                        break;
                    case InterpTypeU2:
                        EmitStelem(InterpTypeU2);
                        break;
                    case InterpTypeI2:
                        EmitStelem(InterpTypeI2);
                        break;
                    case InterpTypeI4:
                        EmitStelem(InterpTypeI4);
                        break;
                    case InterpTypeI8:
                        EmitStelem(InterpTypeI8);
                        break;
                    case InterpTypeR4:
                        EmitStelem(InterpTypeR4);
                        break;
                    case InterpTypeR8:
                        EmitStelem(InterpTypeR8);
                        break;
                    case InterpTypeO:
                        EmitStelem(InterpTypeO);
                        break;
                    case InterpTypeVT:
                    {
                        int size = m_compHnd->getClassSize(elemClsHnd);
                        bool hasRefs = (m_compHnd->getClassAttribs(elemClsHnd) & CORINFO_FLG_CONTAINS_GC_PTR) != 0;
                        m_pStackPointer -= 3;
                        if (hasRefs)
                        {
                            AddIns(INTOP_STELEM_VT);
                            m_pLastNewIns->SetSVars3(m_pStackPointer[0].var, m_pStackPointer[1].var, m_pStackPointer[2].var);
                            m_pLastNewIns->data[0] = size;
                            m_pLastNewIns->data[1] = GetDataItemIndex(elemClsHnd);
                        }
                        else
                        {
                            AddIns(INTOP_STELEM_VT_NOREF);
                            m_pLastNewIns->SetSVars3(m_pStackPointer[0].var, m_pStackPointer[1].var, m_pStackPointer[2].var);
                            m_pLastNewIns->data[0] = size;
                        }
                        break;
                    }
                    default:
                        BADCODE("Unsupported element type for STELEM");
                }

                m_ip += 5;
                break;
            }
            case CEE_LDTOKEN:
            {

                CORINFO_RESOLVED_TOKEN resolvedToken;
                ResolveToken(getU4LittleEndian(m_ip + 1), CORINFO_TOKENKIND_Ldtoken, &resolvedToken);

                InterpEmbedGenericResult resolvedEmbedResult = EmitGenericHandle(&resolvedToken, GenericHandleEmbedOptions::None);

                if (resolvedEmbedResult.var != -1)
                {
                    AddIns(INTOP_LDTOKEN_VAR);
                    m_pLastNewIns->SetSVar(resolvedEmbedResult.var);
                }
                else
                {
                    AddIns(INTOP_LDTOKEN);
                    m_pLastNewIns->data[1] = resolvedEmbedResult.dataItemIndex;
                }

                CORINFO_CLASS_HANDLE clsHnd = m_compHnd->getTokenTypeAsHandle(&resolvedToken);
                PushStackType(StackTypeVT, clsHnd);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);

                // see jit/importer.cpp CEE_LDTOKEN
                CorInfoHelpFunc helper;
                if (resolvedToken.hField)
                {
                    helper = CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD;
                }
                else if (resolvedToken.hMethod)
                {
                    helper = CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD;
                }
                else if (resolvedToken.hClass)
                {
                    helper = CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE;
                }
                else
                {
                    helper = CORINFO_HELP_FAIL_FAST;
                    assert(!"Token not resolved or resolved to unexpected type");
                }
                m_pLastNewIns->data[0] = GetDataItemIndexForHelperFtn(helper);

                m_ip += 5;
                break;
            }

            case CEE_ISINST:
            {
                CHECK_STACK(1);
                CORINFO_RESOLVED_TOKEN resolvedToken;
                ResolveToken(getU4LittleEndian(m_ip + 1), CORINFO_TOKENKIND_Casting, &resolvedToken);

                CorInfoHelpFunc castingHelper = m_compHnd->getCastingHelper(&resolvedToken, false /* throwing */);
                AddIns(INTOP_CALL_HELPER_PP_2);
                m_pLastNewIns->data[0] = GetDataItemIndexForHelperFtn(castingHelper);
                m_pLastNewIns->data[1] = GetDataItemIndex(resolvedToken.hClass);
                m_pLastNewIns->SetSVar(m_pStackPointer[-1].var);
                m_pStackPointer--;
                PushInterpType(InterpTypeI, NULL);
                m_pLastNewIns->SetDVar(m_pStackPointer[-1].var);
                m_ip += 5;
                break;
            }
            default:
            {
                AssertOpCodeNotImplemented(m_ip, m_ip - m_pILCode);
                break;
            }
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
}

InterpBasicBlock *InterpCompiler::GenerateCodeForFinallyCallIslands(InterpBasicBlock *pNewBB, InterpBasicBlock *pPrevBB)
{
    InterpBasicBlock *pFinallyCallIslandBB = pNewBB->pFinallyCallIslandBB;

    while (pFinallyCallIslandBB != NULL)
    {
        INTERP_DUMP("Injecting finally call island BB%d\n", pFinallyCallIslandBB->index);
        if (pFinallyCallIslandBB->emitState != BBStateEmitted)
        {
            // Set the finally call island BB as current so that the instructions are emitted into it
            m_pCBB = pFinallyCallIslandBB;
            InitBBStackState(m_pCBB);
            EmitBranchToBB(INTOP_CALL_FINALLY, pNewBB); // The pNewBB is the finally BB
            m_pLastNewIns->ilOffset = -1;
            // Try to get the next finally call island block (for an outer try's finally)
            if (pFinallyCallIslandBB->pFinallyCallIslandBB)
            {
                // Branch to the next finally call island (at an outer try block)
                EmitBranchToBB(INTOP_BR, pFinallyCallIslandBB->pFinallyCallIslandBB);
            }
            else
            {
                // This is the last finally call island, so we need to emit a branch to the leave target
                EmitBranchToBB(INTOP_BR, pFinallyCallIslandBB->pLeaveTargetBB);
            }
            m_pLastNewIns->ilOffset = -1;
            m_pCBB->emitState = BBStateEmitted;
            INTERP_DUMP("Chaining BB%d -> BB%d\n", pPrevBB->index, pFinallyCallIslandBB->index);
        }
        assert(pPrevBB->pNextBB == NULL || pPrevBB->pNextBB == pFinallyCallIslandBB);
        pPrevBB->pNextBB = pFinallyCallIslandBB;
        pPrevBB = pFinallyCallIslandBB;
        pFinallyCallIslandBB = pFinallyCallIslandBB->pNextBB;
    }

    return pPrevBB;
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
    CORINFO_CLASS_HANDLE cls = m_compHnd->getMethodClass(method);

    CORINFO_SIG_INFO sig;
    m_compHnd->getMethodSig(method, &sig, cls);

    TArray<char> methodName = ::PrintMethodName(m_compHnd, cls, method, &sig,
                            /* includeAssembly */ false,
                            /* includeClass */ false,
                            /* includeClassInstantiation */ true,
                            /* includeMethodInstantiation */ true,
                            /* includeSignature */ true,
                            /* includeReturnType */ false,
                            /* includeThis */ false);


    printf(".%s", methodName.GetUnderlyingArray());
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
    {
        PrintIns(ins);
        printf("\n");
    }
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
}

static const char* s_jitHelperNames[CORINFO_HELP_COUNT] = {
#define JITHELPER(code, pfnHelper, binderId)        #code,
#define DYNAMICJITHELPER(code, pfnHelper, binderId) #code,
#include "jithelpers.h"
#include "compiler.h"
};

const char* CorInfoHelperToName(CorInfoHelpFunc helper)
{
    if (helper < 0 || helper >= CORINFO_HELP_COUNT)
        return "UnknownHelper";

    return s_jitHelperNames[helper];
}

void InterpCompiler::PrintInsData(InterpInst *ins, int32_t insOffset, const int32_t *pData, int32_t opcode)
{
    switch (g_interpOpArgType[opcode]) {
        case InterpOpNoArgs:
            break;
        case InterpOpInt:
            printf(" %d", *pData);
            break;
        case InterpOpLongInt:
        {
            int64_t i64 = (int64_t)pData[0] + ((int64_t)pData[1] << 32);
            printf(" %" PRId64, i64);
            break;
        }
        case InterpOpFloat:
        {
            printf(" %g", *(float*)pData);
            break;
        }
        case InterpOpDouble:
        {
            int64_t i64 = (int64_t)pData[0] + ((int64_t)pData[1] << 32);
            printf(" %g", *(double*)&i64);
            break;
        }
        case InterpOpTwoInts:
            printf(" %d,%d", *pData, *(pData + 1));
            break;
        case InterpOpThreeInts:
            printf(" %d,%d,%d", *pData, *(pData + 1), *(pData + 2));
            break;
        case InterpOpBranch:
            if (ins)
                printf(" BB%d", ins->info.pTargetBB->index);
            else
                printf(" IR_%04x", insOffset + *pData);
            break;
        case InterpOpLdPtr:
            {
                printf("%p", (void*)GetDataItemAtIndex(pData[0]));
                break;
            }
        case InterpOpGenericLookup:
            {
                CORINFO_RUNTIME_LOOKUP *pGenericLookup = (CORINFO_RUNTIME_LOOKUP*)GetDataItemAtIndex(pData[0]);
                printf("%s,%p[", CorInfoHelperToName(pGenericLookup->helper), pGenericLookup->signature);
                for (int i = 0; i < pGenericLookup->indirections; i++)
                {
                    if (i > 0)
                        printf(",");

                    if (i == 0 && pGenericLookup->indirectFirstOffset)
                        printf("*");
                    if (i == 1 && pGenericLookup->indirectSecondOffset)
                        printf("*");
                    printf("%d", (int)pGenericLookup->offsets[i]);
                }
                printf("]");
                if (pGenericLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
                {
                    printf(" sizeOffset=%d", (int)pGenericLookup->sizeOffset);
                }
                if (pGenericLookup->testForNull)
                {
                    printf(" testForNull");
                }
            }
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
        case InterpOpMethodHandle:
        {
            CORINFO_METHOD_HANDLE mh = (CORINFO_METHOD_HANDLE)((size_t)m_dataItems.Get(*pData));
            printf(" ");
            PrintMethodName(mh);
            break;
        }
        case InterpOpClassHandle:
        {
            CORINFO_CLASS_HANDLE ch = (CORINFO_CLASS_HANDLE)((size_t)m_dataItems.Get(*pData));
            printf(" ");
            PrintClassName(ch);
            break;
        }
        case InterpOpHelperFtn:
        {
            size_t helperDirectOrIndirect = (size_t)m_dataItems.Get(*pData);
            if (helperDirectOrIndirect & INTERP_INDIRECT_HELPER_TAG)
                printf(" (indirect) %p", (void*)(helperDirectOrIndirect & ~INTERP_INDIRECT_HELPER_TAG));
            else
                printf(" (direct) %p", (void*)helperDirectOrIndirect);
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

extern "C" void assertAbort(const char* why, const char* file, unsigned line)
{
    if (t_InterpJitInfoTls) {
        if (!t_InterpJitInfoTls->doAssert(file, line, why))
            return;
    }

#ifdef _MSC_VER
    __debugbreak();
#else // _MSC_VER
    __builtin_trap();
#endif // _MSC_VER
}
