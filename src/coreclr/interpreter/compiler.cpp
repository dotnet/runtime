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
    StackTypeVT  // VT
};

static const InterpType g_interpTypeFromStackType[] =
{
    InterpTypeI4, // I4,
    InterpTypeI8, // I8,
    InterpTypeR4, // R4,
    InterpTypeR8, // R8,
    InterpTypeO,  // O,
    InterpTypeVT, // VT,
    InterpTypeI,  // MP,
    InterpTypeI,  // F
};

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

// Allocator for potentially larger chunks of data, that we might want to free
// eagerly, before method is finished compiling, to prevent excessive memory usage.
void* InterpCompiler::AllocTemporary(size_t numBytes)
{
    return malloc(numBytes);
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
    ins->ilOffset = -1;
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
    return g_interpOpLen[ins->opcode];
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
                    while (*callArgs != -1)
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


InterpBasicBlock* InterpCompiler::AllocBB()
{
    InterpBasicBlock *bb = (InterpBasicBlock*)AllocMemPool(sizeof(InterpBasicBlock));
    memset(bb, 0, sizeof(InterpBasicBlock));
    bb->ilOffset = -1;
    bb->nativeOffset = -1;
    bb->stackHeight = -1;
    bb->index = m_BBCount++;
    return bb;
}

InterpBasicBlock* InterpCompiler::GetBB(int32_t ilOffset)
{
    InterpBasicBlock *bb = m_ppOffsetToBB [ilOffset];

    if (!bb)
    {
        bb = AllocBB ();

        bb->ilOffset = ilOffset;
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
            return INTOP_MOV_P;
        case InterpTypeVT:
            return INTOP_MOV_VT;
        default:
            assert(0);
    }
    return -1;
}

int32_t InterpCompiler::CreateVarExplicit(InterpType mt, CORINFO_CLASS_HANDLE clsHnd, int size)
{
    if (m_varsSize == m_varsCapacity) {
        m_varsCapacity *= 2;
        if (m_varsCapacity == 0)
            m_varsCapacity = 16;
        m_pVars = (InterpVar*) ReallocTemporary(m_pVars, m_varsCapacity * sizeof(InterpVar));
    }
    InterpVar *var = &m_pVars[m_varsSize];

    var->mt = mt;
    var->clsHnd = clsHnd;
    var->size = size;
    var->indirects = 0;
    var->offset = -1;
    var->liveStart = -1;

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
            goto exit;                      \
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

void InterpCompiler::PushType(StackType stackType, CORINFO_CLASS_HANDLE clsHnd)
{
    // We don't really care about the exact size for non-valuetypes
    PushTypeExplicit(stackType, clsHnd, INTERP_STACK_SLOT_SIZE);
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

int32_t* InterpCompiler::EmitCodeIns(int32_t *ip, InterpInst *ins)
{
    int32_t opcode = ins->opcode;
    int32_t *startIp = ip;

    *ip++ = opcode;

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

    return ip;
}


void InterpCompiler::EmitCode()
{
    int32_t codeSize = ComputeCodeSize();
    m_pMethodCode = (int32_t*)AllocMethodData(codeSize * sizeof(int32_t));

    int32_t *ip = m_pMethodCode;
    for (InterpBasicBlock *bb = m_pEntryBB; bb != NULL; bb = bb->pNextBB)
    {
        bb->nativeOffset = (int32_t)(ip - m_pMethodCode);
        for (InterpInst *ins = bb->pFirstIns; ins != NULL; ins = ins->pNext)
        {
            ip = EmitCodeIns(ip, ins);
        }
    }

    m_MethodCodeSize = (int32_t)(ip - m_pMethodCode);
}

InterpMethod* InterpCompiler::CreateInterpMethod()
{
    InterpMethod *pMethod = new InterpMethod(m_methodHnd, m_totalVarsStackSize);

    return pMethod;
}

int32_t* InterpCompiler::GetCode(int32_t *pCodeSize)
{
    *pCodeSize = m_MethodCodeSize;
    return m_pMethodCode;
}

InterpCompiler::InterpCompiler(COMP_HANDLE compHnd,
                               CORINFO_METHOD_INFO* methodInfo)
{
    m_methodHnd = methodInfo->ftn;
    m_compHnd = compHnd;
    m_methodInfo = methodInfo;
}

InterpMethod* InterpCompiler::CompileMethod()
{
    GenerateCode(m_methodInfo);

    AllocOffsets();

    EmitCode();

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
    sp->Init(type);
    int32_t var = CreateVarExplicit(g_interpTypeFromStackType[type], NULL, INTERP_STACK_SLOT_SIZE);
    sp->var = var;
    newInst->SetDVar(var);
}

int InterpCompiler::GenerateCode(CORINFO_METHOD_INFO* methodInfo)
{
    m_ip = methodInfo->ILCode;
    uint8_t *codeEnd = m_ip + methodInfo->ILCodeSize;

    m_ppOffsetToBB = (InterpBasicBlock**)AllocMemPool(sizeof(InterpBasicBlock*) * (methodInfo->ILCodeSize + 1));
    m_stackCapacity = methodInfo->maxStack + 1;
    m_pStackBase = m_pStackPointer = (StackInfo*)AllocTemporary(sizeof(StackInfo) * m_stackCapacity);

    m_pCBB = m_pEntryBB = AllocBB();

    while (m_ip < codeEnd)
    {
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
                PushType(StackTypeI4, NULL);
                m_pLastIns->SetDVar(m_pStackPointer[-1].var);
                m_ip++;
                break;
            case CEE_LDC_I4_S:
                AddIns(INTOP_LDC_I4);
                m_pLastIns->data[0] = (int8_t)m_ip[1];
                PushType(StackTypeI4, NULL);
                m_pLastIns->SetDVar(m_pStackPointer[-1].var);
                m_ip += 2;
                break;
            case CEE_RET:
            {
                CORINFO_SIG_INFO sig = methodInfo->args;
                if (sig.retType == CORINFO_TYPE_VOID)
                {
                    AddIns(INTOP_RET_VOID);
                }
                else if (sig.retType == CORINFO_TYPE_INT)
                {
                    CHECK_STACK(1);
                    AddIns(INTOP_RET);
                    m_pStackPointer--;
                    m_pLastIns->SetSVar(m_pStackPointer[0].var);
                }
                else
                {
                    // FIXME
                    assert(0);
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
                case StackTypeMP:
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
                case StackTypeMP:
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
                case StackTypeMP:
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
                case StackTypeMP:
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
                case StackTypeMP:
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
                case StackTypeMP:
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
            default:
                assert(0);
                break;
        }
    }

exit:
    return CORJIT_OK;
}
