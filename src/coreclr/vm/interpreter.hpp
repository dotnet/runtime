// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



// This file contains bodies of inline methods.

#ifndef INTERPRETER_HPP_DEFINED
#define INTERPRETER_HPP_DEFINED 1

#include "interpreter.h"
#if INTERP_ILCYCLE_PROFILE
#include "cycletimer.h"
#endif // INTERP_ILCYCLE_PROFILE

#if INTERP_TRACING
// static
FILE* Interpreter::GetLogFile()
{
    if (s_InterpreterLogFile == NULL)
    {
        static ConfigString fileName;
        LPWSTR fn = fileName.val(CLRConfig::INTERNAL_InterpreterLogFile);
        if (fn == NULL)
        {
            s_InterpreterLogFile = stdout;
        }
        else
        {
            s_InterpreterLogFile = _wfopen(fn, W("a"));
        }
    }
    return s_InterpreterLogFile;
}
#endif // INTERP_TRACING

inline void Interpreter::LdFromMemAddr(void* addr, InterpreterType tp)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    unsigned stackHt = m_curStackHt;

    OpStackTypeSet(stackHt, tp.StackNormalize());

    size_t sz = tp.Size(&m_interpCeeInfo);
    if (tp.IsStruct())
    {
        if (tp.IsLargeStruct(&m_interpCeeInfo))
        {
            // Large struct case.
            void* ptr = LargeStructOperandStackPush(sz);
            memcpy(ptr, addr, sz);
            OpStackSet<void*>(stackHt, ptr);
        }
        else
        {
            OpStackSet<INT64>(stackHt, GetSmallStructValue(addr, sz));
        }
        m_curStackHt = stackHt + 1;
        return;
    }

    // Otherwise...

    // The compiler seems to compile this switch statement into an "if
    // cascade" anyway, but in a non-optimal order (one that's good for
    // code density, but doesn't match the actual usage frequency,
    // which, in fairness, it would have no clue about.  So we might
    // as well do our own "if cascade" in the order we believe is
    // likely to be optimal (at least on 32-bit systems).
    if (sz == 4)
    {
        OpStackSet<INT32>(stackHt, *reinterpret_cast<INT32*>(addr));
    }
    else if (sz == 1)
    {
        CorInfoType cit = tp.ToCorInfoType();
        if (CorInfoTypeIsUnsigned(cit))
        {
            OpStackSet<UINT32>(stackHt, *reinterpret_cast<UINT8*>(addr));
        }
        else
        {
            OpStackSet<INT32>(stackHt, *reinterpret_cast<INT8*>(addr));
        }
    }
    else if (sz == 8)
    {
        OpStackSet<INT64>(stackHt, *reinterpret_cast<INT64*>(addr));
    }
    else
    {
        assert(sz == 2); // only remaining case.
        CorInfoType cit = tp.ToCorInfoType();
        if (CorInfoTypeIsUnsigned(cit))
        {
            OpStackSet<UINT32>(stackHt, *reinterpret_cast<UINT16*>(addr));
        }
        else
        {
            OpStackSet<INT32>(stackHt, *reinterpret_cast<INT16*>(addr));
        }
    }
    m_curStackHt = stackHt + 1;
}

inline void Interpreter::LdLoc(int locNum)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    if (locNum >= m_methInfo->m_numLocals)
    {
        COMPlusThrow(kVerificationException);
    }

    unsigned stackHt = m_curStackHt;
    GCX_FORBID();
    OpStackSet<INT64>(stackHt, *FixedSizeLocalSlot(locNum));
    InterpreterType tp = m_methInfo->m_localDescs[locNum].m_typeStackNormal;
    OpStackTypeSet(stackHt, tp);
    m_curStackHt = stackHt + 1;
    m_orOfPushedInterpreterTypes |= static_cast<size_t>(tp.AsRaw());
}

void Interpreter::StLoc(int locNum)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    assert(m_curStackHt >= 1);

    if (locNum >= m_methInfo->m_numLocals)
    {
        COMPlusThrow(kVerificationException);
    }

    // Don't decrement "m_curStackHt" early -- if we do, then we'll have a potential GC hole, if
    // the top-of-stack value is a GC ref.
    unsigned ind = m_curStackHt - 1;
    InterpreterType tp = m_methInfo->m_localDescs[locNum].m_typeStackNormal;

#ifdef _DEBUG
    if (!tp.Matches(OpStackTypeGet(ind), &m_interpCeeInfo))
    {
        if (!s_InterpreterLooseRules ||
            // We copy a 64 bit value, otherwise some of the below conditions should end up as casts.
            !((tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_NATIVEINT && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_INT) ||
            (tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_INT && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_NATIVEINT) ||
            (tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_INT && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_LONG) ||
            (tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_LONG && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_INT) ||
            (tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_LONG && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_BYREF) ||
            (tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_INT && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_BYREF) ||
            (tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_NATIVEINT && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_BYREF) ||
            (tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_BYREF && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_LONG) ||
            (tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_BYREF && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_CLASS) ||
            (tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_FLOAT && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_DOUBLE) ||
            (tp.ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_DOUBLE && OpStackTypeGet(ind).ToCorInfoTypeShifted() == CORINFO_TYPE_SHIFTED_FLOAT)))
        {
            VerificationError("StLoc requires types to match.");
        }
    }
#endif

    if (tp.IsLargeStruct(&m_interpCeeInfo))
    {
        size_t sz = tp.Size(&m_interpCeeInfo); // TODO: note that tp.IsLargeStruct() above just called tp.Size(), so this is duplicate work unless the optimizer inlines and CSEs the calls.

        // The operand stack entry is a pointer to a corresponding entry in the large struct stack.
        // There will be a large struct location for the local as well.
        BYTE* addr = LargeStructLocalSlot(locNum);

        // Now, before we copy from the large struct stack to "addr", we have a problem.
        // We've optimized "ldloc" to just copy the fixed size entry for the local onto the ostack.
        // But this might mean that there are pointers to "addr" already on the stack, as stand-ins for
        // the value they point to.  If we overwrite that value, we've inadvertently modified the ostack.
        // So we first "normalize" the ostack wrt "addr", ensuring that any entries containing addr
        // have large-struct slots allocated for them, and the values are copied there.
        OpStackNormalize();

        // Now we can do the copy.
        void* srcAddr = OpStackGet<void*>(ind);
        memcpy(addr, srcAddr, sz);
        LargeStructOperandStackPop(sz, srcAddr);
    }
    else
    {
        // Otherwise, we just copy the full stack entry.
        *FixedSizeLocalSlot(locNum) = OpStackGet<INT64>(ind);
    }

    m_curStackHt = ind;

#if INTERP_TRACING
    // The value of the locals has changed; print them.
    if (s_TraceInterpreterILFlag.val(CLRConfig::INTERNAL_TraceInterpreterIL))
    {
        PrintLocals();
    }
#endif // _DEBUG
}

void Interpreter::StToLocalMemAddr(void* addr, InterpreterType tp)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    assert(m_curStackHt >= 1);
    m_curStackHt--;

    size_t sz = tp.Size(&m_interpCeeInfo);
    if (tp.IsStruct())
    {
        if (tp.IsLargeStruct(&m_interpCeeInfo))
        {
            // Large struct case.
            void* srcAddr = OpStackGet<void*>(m_curStackHt);
            memcpy(addr, srcAddr, sz);
            LargeStructOperandStackPop(sz, srcAddr);
        }
        else
        {
            memcpy(addr, OpStackGetAddr(m_curStackHt, sz), sz);
        }
        return;
    }

    // Note: this implementation assumes a little-endian architecture.
    if (sz == 4)
    {
        *reinterpret_cast<INT32*>(addr) = OpStackGet<INT32>(m_curStackHt);
    }
    else if (sz == 1)
    {
        *reinterpret_cast<INT8*>(addr) = OpStackGet<INT8>(m_curStackHt);
    }
    else if (sz == 8)
    {
        *reinterpret_cast<INT64*>(addr) = OpStackGet<INT64>(m_curStackHt);
    }
    else
    {
        assert(sz == 2);
        *reinterpret_cast<INT16*>(addr) = OpStackGet<INT16>(m_curStackHt);
    }
}

template<int op, typename T, bool IsIntType, CorInfoType cit, bool TypeIsUnchanged>
void Interpreter::BinaryArithOpWork(T val1, T val2)
{
    T res;
    if (op == BA_Add)
    {
        res = val1 + val2;
    }
    else if (op == BA_Sub)
    {
        res = val1 - val2;
    }
    else if (op == BA_Mul)
    {
        res = val1 * val2;
    }
    else
    {
        assert(op == BA_Div || op == BA_Rem);
        if (IsIntType)
        {
            if (val2 == 0)
            {
                ThrowDivideByZero();
            }
            else if (val2 == -1 && val1 == static_cast<T>(((UINT64)1) << (sizeof(T)*8 - 1))) // min int / -1 is not representable.
            {
                ThrowSysArithException();
            }
        }
        // Otherwise...
        if (op == BA_Div)
        {
            res = val1 / val2;
        }
        else
        {
            res = RemFunc(val1, val2);
        }
    }

    unsigned residx = m_curStackHt - 2;
    OpStackSet<T>(residx, res);
    if (!TypeIsUnchanged)
    {
        OpStackTypeSet(residx, InterpreterType(cit));
    }
}

void Interpreter::BrOnValueTakeBranch(bool shouldBranch, int targetLen)
{
    if  (shouldBranch)
    {
        int offset;
        if (targetLen == 1)
        {
            // BYTE is unsigned...
            offset = getI1(m_ILCodePtr + 1);
        }
        else
        {
            offset = getI4LittleEndian(m_ILCodePtr + 1);
        }
        // 1 is the size of the current instruction; offset is relative to start of next.
        if (offset < 0)
        {
            // Backwards branch; enable caching.
            BackwardsBranchActions(offset);
        }
        ExecuteBranch(m_ILCodePtr + 1 + targetLen + offset);
    }
    else
    {
        m_ILCodePtr += targetLen + 1;
    }
}

extern size_t CorInfoTypeSizeArray[];

size_t CorInfoTypeSize(CorInfoType cit)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE_MSG(cit != CORINFO_TYPE_VALUECLASS, "Precondition");

    size_t res = CorInfoTypeSizeArray[cit];

    _ASSERTE_MSG(res != 0, "Other illegal input");

    return res;
}

void Interpreter::StaticFldAddr(CORINFO_ACCESS_FLAGS accessFlgs,
                                /*out (byref)*/void** pStaticFieldAddr,
                                /*out*/InterpreterType* pit, /*out*/UINT* pFldSize, /*out*/bool* pManagedMem)
{
    unsigned ilOffset = CurOffset();

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionOpportunities[RTK_SFldAddr]);
#endif // INTERP_TRACING

    StaticFieldCacheEntry* cacheEntry = NULL;
    if (s_InterpreterUseCaching) cacheEntry = GetCachedStaticField(ilOffset);
    if (cacheEntry == NULL)
    {
        bool doCaching = StaticFldAddrWork(accessFlgs, pStaticFieldAddr, pit, pFldSize, pManagedMem);
        if (s_InterpreterUseCaching && doCaching)
        {
            cacheEntry = new StaticFieldCacheEntry(*pStaticFieldAddr, *pFldSize, *pit);
            CacheStaticField(ilOffset, cacheEntry);
        }
    }
    else
    {
        // Reenable this if you want to check this (#ifdef _DEBUG).  Was interfering with some statistics
        // gathering I was doing in debug builds.
#if 0
        // Make sure the caching works correctly.
        StaticFldAddrWork(accessFlgs, pStaticFieldAddr, pit, pFldSize, pManagedMem);
        assert(*pStaticFieldAddr == cacheEntry->m_srcPtr && *pit == cacheEntry->m_it && *pFldSize == cacheEntry->m_sz);
#else
        // If we do the call above, it takes care of this.
        m_ILCodePtr += 5;  // In the case above, the call to StaticFldAddr increments the code pointer.
#endif
        *pStaticFieldAddr = cacheEntry->m_srcPtr;
        *pit = cacheEntry->m_it;
        *pFldSize = cacheEntry->m_sz;
        *pManagedMem = true; // Or else it wouldn't have been cached.
    }
}

void Interpreter::ResolveToken(CORINFO_RESOLVED_TOKEN* resTok, mdToken token, CorInfoTokenKind tokenType InterpTracingArg(ResolveTokenKind rtk))
{
    resTok->tokenContext = GetPreciseGenericsContext();
    resTok->tokenScope = m_methInfo->m_module;
    resTok->token = token;
    resTok->tokenType = tokenType;
#if INTERP_ILCYCLE_PROFILE
    unsigned __int64 startCycles;
    bool b = CycleTimer::GetThreadCyclesS(&startCycles); assert(b);
#endif // INTERP_ILCYCLE_PROFILE
    m_interpCeeInfo.resolveToken(resTok);
#if 1
    if (resTok->tokenType == CORINFO_TOKENKIND_Method)
    {
        MethodDesc* pMD = reinterpret_cast<MethodDesc*>(resTok->hMethod);
        MethodTable* pMT = GetMethodTableFromClsHnd(resTok->hClass);

        if (pMD->GetMethodTable() != pMT)
        {
            // Find the method on exactClass corresponding to methToCall.
            pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
                        pMD,             // pPrimaryMD
                        pMT,             // pExactMT
                        FALSE,           // forceBoxedEntryPoint
                        pMD->GetMethodInstantiation(),  // methodInst
                        FALSE,           // allowInstParam
                        TRUE);           // forceRemotableMethod (to get maximally specific).
            resTok->hMethod = reinterpret_cast<CORINFO_METHOD_HANDLE>(pMD);
        }
    }
#endif
#if INTERP_ILCYCLE_PROFILE
    unsigned __int64 endCycles;
    b = CycleTimer::GetThreadCyclesS(&endCycles); assert(b);
    m_exemptCycles += (endCycles - startCycles);
#endif // INTERP_ILCYCLE_PROFILE

#if INTERP_TRACING
    InterlockedIncrement(&s_tokenResolutionCalls[rtk]);
#endif // INTERP_TRACING
}

FieldDesc* Interpreter::FindField(unsigned metaTok InterpTracingArg(ResolveTokenKind rtk))
{
    CORINFO_RESOLVED_TOKEN fldTok;
    ResolveToken(&fldTok, metaTok, CORINFO_TOKENKIND_Field InterpTracingArg(rtk));
    return (FieldDesc*)fldTok.hField;
}

CORINFO_CLASS_HANDLE Interpreter::FindClass(unsigned metaTok InterpTracingArg(ResolveTokenKind rtk))
{
    CORINFO_RESOLVED_TOKEN clsTok;
    ResolveToken(&clsTok, metaTok, CORINFO_TOKENKIND_Class InterpTracingArg(rtk));
    return clsTok.hClass;
}

void Interpreter::ThrowOnInvalidPointer(void* ptr)
{
    if (ptr == NULL)
        ThrowNullPointerException();

    BOOL good = TRUE;

    EX_TRY
    {
        AVInRuntimeImplOkayHolder AVOkay;
        good = *(BOOL*)ptr;

        // This conditional forces the dereference to occur; it also
        // ensures that good == TRUE if the dereference succeeds.
        if (!good)
            good = TRUE;
    }
    EX_CATCH
    {
        good = FALSE;
    }
    EX_END_CATCH(SwallowAllExceptions)

    if (!good)
        ThrowNullPointerException();
}

#endif // INTERPRETER_HPP_DEFINED
