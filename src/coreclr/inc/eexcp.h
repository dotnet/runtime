// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __eexcp_h__
#define __eexcp_h__

#include "corhlpr.h"
#include "daccess.h"

struct EE_ILEXCEPTION_CLAUSE;
typedef DPTR(EE_ILEXCEPTION_CLAUSE) PTR_EE_ILEXCEPTION_CLAUSE;

// The exception handling sub-system needs to keep track of EH clause that is handling given exception.
// PTR_EXCEPTION_CLAUSE_TOKEN is opaque pointer that uniquely identifies
// exception handling clause. It abstracts away encoding differences of EH clauses between JIT and NGen.
typedef PTR_VOID PTR_EXCEPTION_CLAUSE_TOKEN;

struct EE_ILEXCEPTION_CLAUSE  {
    //Flags is not marked as volatile since it is always accessed
    //    from within a critical section
    CorExceptionFlag    Flags;
    DWORD               TryStartPC;
    DWORD               TryEndPC;
    DWORD               HandlerStartPC;
    DWORD               HandlerEndPC;
    union {
        void*           TypeHandle;
        mdToken         ClassToken;
        DWORD           FilterOffset;
    };
};

struct EE_ILEXCEPTION;
typedef DPTR(EE_ILEXCEPTION) PTR_EE_ILEXCEPTION;

struct EE_ILEXCEPTION : public COR_ILMETHOD_SECT_FAT
{
    EE_ILEXCEPTION_CLAUSE Clauses[1];     // actually variable size

    void Init(unsigned ehCount)
    {
        LIMITED_METHOD_CONTRACT;

        SetKind(CorILMethod_Sect_FatFormat);
        SetDataSize((unsigned)sizeof(EE_ILEXCEPTION_CLAUSE) * ehCount);
    }

    unsigned EHCount() const
    {
        LIMITED_METHOD_CONTRACT;

        return GetDataSize() / (DWORD) sizeof(EE_ILEXCEPTION_CLAUSE);
    }

    static unsigned Size(unsigned ehCount)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(ehCount > 0);

        return (offsetof(EE_ILEXCEPTION, Clauses) + sizeof(EE_ILEXCEPTION_CLAUSE) * ehCount);
    }
    EE_ILEXCEPTION_CLAUSE *EHClause(unsigned i)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return &(PTR_EE_ILEXCEPTION_CLAUSE(PTR_HOST_MEMBER_TADDR(EE_ILEXCEPTION,this,Clauses))[i]);
    }
};

#define COR_ILEXCEPTION_CLAUSE_CACHED_CLASS     0x10000000

inline BOOL HasCachedTypeHandle(EE_ILEXCEPTION_CLAUSE *EHClause)
{
    _ASSERTE(sizeof(EHClause->Flags) == sizeof(DWORD));
    return (EHClause->Flags & COR_ILEXCEPTION_CLAUSE_CACHED_CLASS);
}

inline void SetHasCachedTypeHandle(EE_ILEXCEPTION_CLAUSE *EHClause)
{
    _ASSERTE(! HasCachedTypeHandle(EHClause));
    EHClause->Flags = (CorExceptionFlag)(EHClause->Flags | COR_ILEXCEPTION_CLAUSE_CACHED_CLASS);
}

inline BOOL IsFinally(EE_ILEXCEPTION_CLAUSE *EHClause)
{
    LIMITED_METHOD_CONTRACT;

    return (EHClause->Flags & COR_ILEXCEPTION_CLAUSE_FINALLY);
}

inline BOOL IsFault(EE_ILEXCEPTION_CLAUSE *EHClause)
{
    LIMITED_METHOD_CONTRACT;

    return (EHClause->Flags & COR_ILEXCEPTION_CLAUSE_FAULT);
}

inline BOOL IsFaultOrFinally(EE_ILEXCEPTION_CLAUSE *EHClause)
{
    return IsFault(EHClause) || IsFinally(EHClause);
}

inline BOOL IsFilterHandler(EE_ILEXCEPTION_CLAUSE *EHClause)
{
    LIMITED_METHOD_CONTRACT;

    return EHClause->Flags & COR_ILEXCEPTION_CLAUSE_FILTER;
}

inline BOOL IsTypedHandler(EE_ILEXCEPTION_CLAUSE *EHClause)
{
    return ! (IsFilterHandler(EHClause) || IsFaultOrFinally(EHClause));
}

inline BOOL IsDuplicateClause(EE_ILEXCEPTION_CLAUSE* pEHClause)
{
    return pEHClause->Flags & COR_ILEXCEPTION_CLAUSE_DUPLICATED;
}

#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
// Finally is the only EH construct that can be part of the execution as being fall-through.
//
// "Cloned" finally is a contruct that represents a finally block that is used as
// fall through for normal try-block execution. Such a "cloned" finally will:
//
// 1) Have its try-clause's Start and End PC the same as its handler's start PC (i.e. will have
//    zero length try block), AND
// 2) Is marked duplicate
//
// Because of their fall-through nature, JIT guarantees that only finally constructs can be cloned,
// and not catch or fault (since they cannot be fallen through but are invoked as funclets).
//
// The cloned finally construct is also used to mark "call to finally" thunks that are not within
// the EH region protected by the finally, and also not within the enclosing region. This is done
// to prevent ThreadAbortException from creating an infinite loop of calling the same finally.
inline BOOL IsClonedFinally(EE_ILEXCEPTION_CLAUSE* pEHClause)
{
    return ((pEHClause->TryStartPC == pEHClause->TryEndPC) &&
            (pEHClause->TryStartPC == pEHClause->HandlerStartPC) &&
            IsFinally(pEHClause) && IsDuplicateClause(pEHClause));
}
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)

#endif // __eexcp_h__

