// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "jitinterface.h"
#include "codeman.h"
#include "method.hpp"
#include "class.h"
#include "object.h"
#include "field.h"
#include "stublink.h"
#include "virtualcallstub.h"
#include "corjit.h"
#include "eeconfig.h"
#include "excep.h"
#include "log.h"
#include "excep.h"
#include "float.h"      // for isnan
#include "dbginterface.h"
#include "dllimport.h"
#include "gcheaputilities.h"
#include "comdelegate.h"
#include "corprof.h"
#include "eeprofinterfaces.h"
#include "dynamicinterfacecastable.h"
#include "comsynchronizable.h"

#ifndef TARGET_UNIX
// Included for referencing __report_gsfailure
#include "process.h"
#endif // !TARGET_UNIX

#ifdef PROFILING_SUPPORTED
#include "proftoeeinterfaceimpl.h"
#endif
#include "ecall.h"
#include "generics.h"
#include "typestring.h"
#include "typedesc.h"
#include "genericdict.h"
#include "array.h"
#include "debuginfostore.h"
#include "safemath.h"
#include "threadstatics.h"

#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER

#include "runtimehandles.h"
#include "castcache.h"
#include "onstackreplacement.h"
#include "pgo.h"
#include "pgo_formatprocessing.h"

#ifndef FEATURE_EH_FUNCLETS
#include "excep.h"
#endif
#include "exinfo.h"
#include "arraynative.inl"

using std::isfinite;
using std::isnan;

//========================================================================
//
// This file contains implementation of all JIT helpers. The helpers are
// divided into following categories:
//
//      INTEGER ARITHMETIC HELPERS
//      FLOATING POINT HELPERS
//      INSTANCE FIELD HELPERS
//      STATIC FIELD HELPERS
//      SHARED STATIC FIELD HELPERS
//      CASTING HELPERS
//      ALLOCATION HELPERS
//      STRING HELPERS
//      ARRAY HELPERS
//      VALUETYPE/BYREF HELPERS
//      GENERICS HELPERS
//      EXCEPTION HELPERS
//      DEBUGGER/PROFILER HELPERS
//      GC HELPERS
//      INTEROP HELPERS
//
//========================================================================



//========================================================================
//
//      INTEGER ARITHMETIC HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

//
// helper macro to multiply two 32-bit uints
//
#define Mul32x32To64(a, b)  ((UINT64)((UINT32)(a)) * (UINT64)((UINT32)(b)))

//
// helper macro to get high 32-bit of 64-bit int
//
#define Hi32Bits(a)         ((UINT32)((UINT64)(a) >> 32))

//
// helper macro to check whether 64-bit signed int fits into 32-bit signed (compiles into one 32-bit compare)
//
#define Is32BitSigned(a)    (Hi32Bits(a) == Hi32Bits((INT64)(INT32)(a)))

#if !defined(TARGET_X86) || defined(TARGET_UNIX)
/*********************************************************************/
HCIMPL2_VV(INT64, JIT_LMul, INT64 val1, INT64 val2)
{
    FCALL_CONTRACT;

    UINT32 val1High = Hi32Bits(val1);
    UINT32 val2High = Hi32Bits(val2);

    if ((val1High == 0) && (val2High == 0))
        return Mul32x32To64(val1, val2);

    return (val1 * val2);
}
HCIMPLEND
#endif // !TARGET_X86 || TARGET_UNIX

/*********************************************************************/
HCIMPL2(INT32, JIT_Div, INT32 dividend, INT32 divisor)
{
    FCALL_CONTRACT;

    RuntimeExceptionKind ehKind;

    if (((UINT32) (divisor + 1)) <= 1)  // Unsigned test for divisor in [-1 .. 0]
    {
        if (divisor == 0)
        {
            ehKind = kDivideByZeroException;
            goto ThrowExcep;
        }
        else if (divisor == -1)
        {
            if (dividend == INT32_MIN)
            {
                ehKind = kOverflowException;
                goto ThrowExcep;
            }
            return -dividend;
        }
    }

    return(dividend / divisor);

ThrowExcep:
    FCThrow(ehKind);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2(INT32, JIT_Mod, INT32 dividend, INT32 divisor)
{
    FCALL_CONTRACT;

    RuntimeExceptionKind ehKind;

    if (((UINT32) (divisor + 1)) <= 1)  // Unsigned test for divisor in [-1 .. 0]
    {
        if (divisor == 0)
        {
            ehKind = kDivideByZeroException;
            goto ThrowExcep;
        }
        else if (divisor == -1)
        {
            if (dividend == INT32_MIN)
            {
                ehKind = kOverflowException;
                goto ThrowExcep;
            }
            return 0;
        }
    }

    return(dividend % divisor);

ThrowExcep:
    FCThrow(ehKind);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2(UINT32, JIT_UDiv, UINT32 dividend, UINT32 divisor)
{
    FCALL_CONTRACT;

    if (divisor == 0)
        FCThrow(kDivideByZeroException);

    return(dividend / divisor);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2(UINT32, JIT_UMod, UINT32 dividend, UINT32 divisor)
{
    FCALL_CONTRACT;

    if (divisor == 0)
        FCThrow(kDivideByZeroException);

    return(dividend % divisor);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(INT64, JIT_LDiv, INT64 dividend, INT64 divisor)
{
    FCALL_CONTRACT;

    RuntimeExceptionKind ehKind;

    if (Is32BitSigned(divisor))
    {
        if ((INT32)divisor == 0)
        {
            ehKind = kDivideByZeroException;
            goto ThrowExcep;
        }

        if ((INT32)divisor == -1)
        {
            if ((UINT64) dividend == UI64(0x8000000000000000))
            {
                ehKind = kOverflowException;
                goto ThrowExcep;
            }
            return -dividend;
        }

        // Check for -ive or +ive numbers in the range -2**31 to 2**31
        if (Is32BitSigned(dividend))
            return((INT32)dividend / (INT32)divisor);
    }

    // For all other combinations fallback to int64 div.
    return(dividend / divisor);

ThrowExcep:
    FCThrow(ehKind);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(INT64, JIT_LMod, INT64 dividend, INT64 divisor)
{
    FCALL_CONTRACT;

    RuntimeExceptionKind ehKind;

    if (Is32BitSigned(divisor))
    {
        if ((INT32)divisor == 0)
        {
            ehKind = kDivideByZeroException;
            goto ThrowExcep;
        }

        if ((INT32)divisor == -1)
        {
            // <TODO>TODO, we really should remove this as it lengthens the code path
            // and the spec really says that it should not throw an exception. </TODO>
            if ((UINT64) dividend == UI64(0x8000000000000000))
            {
                ehKind = kOverflowException;
                goto ThrowExcep;
            }
            return 0;
        }

        // Check for -ive or +ive numbers in the range -2**31 to 2**31
        if (Is32BitSigned(dividend))
            return((INT32)dividend % (INT32)divisor);
    }

    // For all other combinations fallback to int64 div.
    return(dividend % divisor);

ThrowExcep:
    FCThrow(ehKind);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_ULDiv, UINT64 dividend, UINT64 divisor)
{
    FCALL_CONTRACT;

    if (Hi32Bits(divisor) == 0)
    {
        if ((UINT32)(divisor) == 0)
        FCThrow(kDivideByZeroException);

        if (Hi32Bits(dividend) == 0)
            return((UINT32)dividend / (UINT32)divisor);
    }

    return(dividend / divisor);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_ULMod, UINT64 dividend, UINT64 divisor)
{
    FCALL_CONTRACT;

    if (Hi32Bits(divisor) == 0)
    {
        if ((UINT32)(divisor) == 0)
        FCThrow(kDivideByZeroException);

        if (Hi32Bits(dividend) == 0)
            return((UINT32)dividend % (UINT32)divisor);
    }

    return(dividend % divisor);
}
HCIMPLEND

#if !defined(HOST_64BIT) && !defined(TARGET_X86)
/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_LLsh, UINT64 num, int shift)
{
    FCALL_CONTRACT;
    return num << (shift & 0x3F);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(INT64, JIT_LRsh, INT64 num, int shift)
{
    FCALL_CONTRACT;
    return num >> (shift & 0x3F);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_LRsz, UINT64 num, int shift)
{
    FCALL_CONTRACT;
    return num >> (shift & 0x3F);
}
HCIMPLEND
#endif // !HOST_64BIT && !TARGET_X86

#include <optdefault.h>


//========================================================================
//
//      FLOATING POINT HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

/*********************************************************************/
HCIMPL1_V(double, JIT_ULng2Dbl, uint64_t val)
{
    FCALL_CONTRACT;
    return (double)val;
}
HCIMPLEND

/*********************************************************************/
HCIMPL1_V(double, JIT_Lng2Dbl, int64_t val)
{
    FCALL_CONTRACT;
    return (double)val;
}
HCIMPLEND

/*********************************************************************/
HCIMPL1_V(int64_t, JIT_Dbl2Lng, double val)
{
    FCALL_CONTRACT;

#if defined(TARGET_X86) || defined(TARGET_AMD64) || defined(TARGET_ARM)
    const double int64_min = -2147483648.0 * 4294967296.0;
    const double int64_max = 2147483648.0 * 4294967296.0;
    return (val != val) ? 0 : (val <= int64_min) ? INT64_MIN : (val >= int64_max) ? INT64_MAX : (int64_t)val;
#else
    return (int64_t)val;
#endif
}
HCIMPLEND

/*********************************************************************/
HCIMPL1_V(uint32_t, JIT_Dbl2UInt, double val)
{
    FCALL_CONTRACT;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    const double uint_max = 4294967295.0;
    // Note that this expression also works properly for val = NaN case
    return (val >= 0) ? ((val >= uint_max) ? UINT32_MAX : (uint32_t)val) : 0;
#else
    return (uint32_t)val;
#endif
}
HCIMPLEND

/*********************************************************************/
HCIMPL1_V(int32_t, JIT_Dbl2Int, double val)
{
    FCALL_CONTRACT;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    const double int32_min = -2147483648.0;
    const double int32_max_plus_1 = 2147483648.0;
    return (val != val) ? 0 : (val <= int32_min) ? INT32_MIN : (val >= int32_max_plus_1) ? INT32_MAX : (int32_t)val;
#else
    return (int32_t)val;
#endif
}
HCIMPLEND

/*********************************************************************/
HCIMPL1_V(uint64_t, JIT_Dbl2ULng, double val)
{
    FCALL_CONTRACT;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    const double uint64_max_plus_1 = 4294967296.0 * 4294967296.0;
    // Note that this expression also works properly for val = NaN case
    return (val >= 0) ? ((val >= uint64_max_plus_1) ? UINT64_MAX : (uint64_t)val) : 0;
#else
    return (uint64_t)val;
#endif
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(float, JIT_FltRem, float dividend, float divisor)
{
    FCALL_CONTRACT;

    return fmodf(dividend, divisor);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(double, JIT_DblRem, double dividend, double divisor)
{
    FCALL_CONTRACT;

    return fmod(dividend, divisor);
}
HCIMPLEND

#include <optdefault.h>


//========================================================================
//
//      INSTANCE FIELD HELPERS
//
//========================================================================

/*********************************************************************/
// Returns the address of the instance field in the object (This is an interior
// pointer and the caller has to use it appropriately) or a static field.
// obj can be either a reference or a byref
HCIMPL2(void*, JIT_GetFieldAddr_Framed, Object *obj, FieldDesc* pFD)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
    } CONTRACTL_END;

    void * fldAddr = NULL;
    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    HELPER_METHOD_FRAME_BEGIN_RET_1(objRef);

    if (!pFD->IsStatic() && objRef == NULL)
        COMPlusThrow(kNullReferenceException);

    fldAddr = pFD->GetAddress(OBJECTREFToObject(objRef));

    HELPER_METHOD_FRAME_END();

    return fldAddr;
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL2(void*, JIT_GetFieldAddr, Object *obj, FieldDesc* pFD)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
    } CONTRACTL_END;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetFieldAddr_Framed, obj, pFD);
    }

    return pFD->GetAddressGuaranteedInHeap(obj);
}
HCIMPLEND
#include <optdefault.h>

#include <optsmallperfcritical.h>
HCIMPL1(void*, JIT_GetStaticFieldAddr, FieldDesc* pFD)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
    } CONTRACTL_END;

    // [TODO] Only handling EnC for now
    _ASSERTE(pFD->IsEnCNew());

    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetFieldAddr_Framed, NULL, pFD);
    }
}
HCIMPLEND
#include <optdefault.h>

// Helper for the managed InitClass implementations
extern "C" void QCALLTYPE InitClassHelper(MethodTable* pMT)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    _ASSERTE(pMT->IsFullyLoaded());
    pMT->EnsureInstanceActive();
    pMT->CheckRunClassInitThrowing();
    END_QCALL;
}

//========================================================================
//
//      SHARED STATIC FIELD HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

// No constructor version of JIT_GetSharedNonGCStaticBase.  Does not check if class has
// been initialized.
HCIMPL1(void*, JIT_GetNonGCStaticBaseNoCtor_Portable, MethodTable* pMT)
{
    FCALL_CONTRACT;

    return pMT->GetDynamicStaticsInfo()->GetNonGCStaticsPointerAssumeIsInited();
}
HCIMPLEND

// No constructor version of JIT_GetSharedNonGCStaticBase.  Does not check if class has
// been initialized.
HCIMPL1(void*, JIT_GetDynamicNonGCStaticBaseNoCtor_Portable, DynamicStaticsInfo* pDynamicStaticsInfo)
{
    FCALL_CONTRACT;

    return pDynamicStaticsInfo->GetNonGCStaticsPointerAssumeIsInited();
}
HCIMPLEND

// No constructor version of JIT_GetSharedGCStaticBase.  Does not check if class has been
// initialized.
HCIMPL1(void*, JIT_GetGCStaticBaseNoCtor_Portable, MethodTable* pMT)
{
    FCALL_CONTRACT;

    return pMT->GetDynamicStaticsInfo()->GetGCStaticsPointerAssumeIsInited();
}
HCIMPLEND

// No constructor version of JIT_GetSharedGCStaticBase.  Does not check if class has been
// initialized.
HCIMPL1(void*, JIT_GetDynamicGCStaticBaseNoCtor_Portable, DynamicStaticsInfo* pDynamicStaticsInfo)
{
    FCALL_CONTRACT;

    return pDynamicStaticsInfo->GetGCStaticsPointerAssumeIsInited();
}
HCIMPLEND

#include <optdefault.h>

//========================================================================
//
//      THREAD STATIC FIELD HELPERS
//
//========================================================================

// Define the t_ThreadStatics variable here, so that these helpers can use
// the most optimal TLS access pattern for the platform when inlining the
// GetThreadLocalStaticBaseIfExistsAndInitialized function
#ifdef _MSC_VER
__declspec(selectany) __declspec(thread)  ThreadLocalData t_ThreadStatics;
#else
__thread ThreadLocalData t_ThreadStatics;
#endif // _MSC_VER

extern "C" void QCALLTYPE GetThreadStaticsByMethodTable(QCall::ByteRefOnStack refHandle, MethodTable* pMT, bool gcStatic)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    pMT->CheckRunClassInitThrowing();

    GCX_COOP();
    if (gcStatic)
    {
        refHandle.Set(pMT->GetGCThreadStaticsBasePointer());
    }
    else
    {
        refHandle.Set(pMT->GetNonGCThreadStaticsBasePointer());
    }

    END_QCALL;
}

extern "C" void QCALLTYPE GetThreadStaticsByIndex(QCall::ByteRefOnStack refHandle, uint32_t staticBlockIndex, bool gcStatic)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    TLSIndex tlsIndex(staticBlockIndex);
    // Check if the class constructor needs to be run
    MethodTable *pMT = LookupMethodTableForThreadStaticKnownToBeAllocated(tlsIndex);
    pMT->CheckRunClassInitThrowing();

    GCX_COOP();
    if (gcStatic)
    {
        refHandle.Set(pMT->GetGCThreadStaticsBasePointer());
    }
    else
    {
        refHandle.Set(pMT->GetNonGCThreadStaticsBasePointer());
    }

    END_QCALL;
}

// *** This helper corresponds CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED2.
HCIMPL1(void*, JIT_GetNonGCThreadStaticBaseOptimized2, UINT32 staticBlockIndex)
{
    FCALL_CONTRACT;

    return ((BYTE*)&t_ThreadStatics) + staticBlockIndex;
}
HCIMPLEND

#include <optdefault.h>

//========================================================================
//
//      STATIC FIELD DYNAMIC HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>
HCIMPL1_RAW(TADDR, JIT_StaticFieldAddress_Dynamic, StaticFieldAddressArgs * pArgs)
{
    FCALL_CONTRACT;

    TADDR base = HCCALL1(pArgs->staticBaseHelper, pArgs->arg0);
    return base + pArgs->offset;
}
HCIMPLEND_RAW
#include <optdefault.h>

#include <optsmallperfcritical.h>
HCIMPL1_RAW(TADDR, JIT_StaticFieldAddressUnbox_Dynamic, StaticFieldAddressArgs * pArgs)
{
    FCALL_CONTRACT;

    TADDR base = HCCALL1(pArgs->staticBaseHelper, pArgs->arg0);
    return *(TADDR *)(base + pArgs->offset) + Object::GetOffsetOfFirstField();
}
HCIMPLEND_RAW
#include <optdefault.h>

//========================================================================
//
//      CASTING HELPERS
//
//========================================================================

BOOL ObjIsInstanceOfCore(Object *pObject, TypeHandle toTypeHnd, BOOL throwCastException)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObject));
    } CONTRACTL_END;

    BOOL fCast = FALSE;
    MethodTable* pMT = pObject->GetMethodTable();

    OBJECTREF obj = ObjectToOBJECTREF(pObject);
    GCPROTECT_BEGIN(obj);

    // we check nullable case first because it is not cacheable.
    // object castability and type castability disagree on T --> Nullable<T>,
    // so we can't put this in the cache
    if (Nullable::IsNullableForType(toTypeHnd, pMT))
    {
        // allow an object of type T to be cast to Nullable<T> (they have the same representation)
        fCast = TRUE;
    }
    else if (toTypeHnd.IsTypeDesc())
    {
        CastCache::TryAddToCache(pMT, toTypeHnd, FALSE);
        fCast = FALSE;
    }
    else if (pMT->CanCastTo(toTypeHnd.AsMethodTable(), /* pVisited */ NULL))
    {
        fCast = TRUE;
    }
    else if (toTypeHnd.IsInterface())
    {
#ifdef FEATURE_COMINTEROP
        // If we are casting a COM object from interface then we need to do a check to see
        // if it implements the interface.
        if (pMT->IsComObjectType())
        {
            fCast = ComObject::SupportsInterface(obj, toTypeHnd.AsMethodTable());
        }
        else
#endif // FEATURE_COMINTEROP
        if (pMT->IsIDynamicInterfaceCastable())
        {
            fCast = DynamicInterfaceCastable::IsInstanceOf(&obj, toTypeHnd, throwCastException);
        }
    }

    if (!fCast && throwCastException)
    {
        COMPlusThrowInvalidCastException(&obj, toTypeHnd);
    }

    GCPROTECT_END(); // obj

    return(fCast);
}

BOOL ObjIsInstanceOf(Object* pObject, TypeHandle toTypeHnd, BOOL throwCastException)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObject));
    } CONTRACTL_END;

    MethodTable* pMT = pObject->GetMethodTable();
    TypeHandle::CastResult result = CastCache::TryGetFromCache(pMT, toTypeHnd);

    if (result == TypeHandle::CanCast ||
        (result == TypeHandle::CannotCast && !throwCastException))
    {
        return (BOOL)result;
    }

    return ObjIsInstanceOfCore(pObject, toTypeHnd, throwCastException);
}

HCIMPL2(Object*, ChkCastAny_NoCacheLookup, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;

    // This case should be handled by frameless helper
    _ASSERTE(obj != NULL);

    OBJECTREF oref = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(oref);

    TypeHandle clsHnd(type);

    HELPER_METHOD_FRAME_BEGIN_RET_1(oref);
    if (!ObjIsInstanceOfCore(OBJECTREFToObject(oref), clsHnd, TRUE))
    {
        UNREACHABLE(); //ObjIsInstanceOf will throw if cast can't be done
    }
    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(oref);
}
HCIMPLEND

HCIMPL2(Object*, IsInstanceOfAny_NoCacheLookup, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;

    // This case should be handled by frameless helper
    _ASSERTE(obj != NULL);

    OBJECTREF oref = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(oref);

    TypeHandle clsHnd(type);

    HELPER_METHOD_FRAME_BEGIN_RET_1(oref);
    if (!ObjIsInstanceOfCore(OBJECTREFToObject(oref), clsHnd))
        oref = NULL;
    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(oref);
}
HCIMPLEND

//========================================================================
//
//      ALLOCATION HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

//*************************************************************
// Allocation fast path for typical objects
//
HCIMPL1_RAW(Object*, JIT_NewS_MP_FastPortable, CORINFO_CLASS_HANDLE typeHnd_)
{
    CONTRACTL {
        THROWS;
        DISABLED(GC_TRIGGERS);
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());
    ee_alloc_context *eeAllocContext = &t_runtime_thread_locals.alloc_context;
    gc_alloc_context *allocContext = &eeAllocContext->m_GCAllocContext;

    TypeHandle typeHandle(typeHnd_);
    _ASSERTE(!typeHandle.IsTypeDesc()); // heap objects must have method tables
    MethodTable *methodTable = typeHandle.AsMethodTable();

    SIZE_T size = methodTable->GetBaseSize();
    _ASSERTE(size % DATA_ALIGNMENT == 0);

    BYTE *allocPtr = allocContext->alloc_ptr;
    _ASSERTE(allocPtr <= eeAllocContext->getCombinedLimit());
    if (size > static_cast<SIZE_T>(eeAllocContext->getCombinedLimit() - allocPtr))
    {
        // Tail call to the slow helper
        return HCCALL1(JIT_New, typeHnd_);
    }

    allocContext->alloc_ptr = allocPtr + size;

    _ASSERTE(allocPtr != nullptr);
    Object *object = reinterpret_cast<Object *>(allocPtr);
    _ASSERTE(object->HasEmptySyncBlockInfo());
    object->SetMethodTable(methodTable);

    return object;
}
HCIMPLEND_RAW

#include <optdefault.h>

/*************************************************************/
HCIMPL1(Object*, JIT_New, CORINFO_CLASS_HANDLE typeHnd_)
{
    FCALL_CONTRACT;

    OBJECTREF newobj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    TypeHandle typeHnd(typeHnd_);

    _ASSERTE(!typeHnd.IsTypeDesc());  // heap objects must have method tables
    MethodTable *pMT = typeHnd.AsMethodTable();

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GetThread()->DisableStressHeap();
    }
#endif // _DEBUG

    newobj = AllocateObject(pMT);

    HELPER_METHOD_FRAME_END();
    return(OBJECTREFToObject(newobj));
}
HCIMPLEND

/*************************************************************/
HCIMPL1(Object*, JIT_NewMaybeFrozen, CORINFO_CLASS_HANDLE typeHnd_)
{
    FCALL_CONTRACT;

    OBJECTREF newobj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    TypeHandle typeHnd(typeHnd_);

    _ASSERTE(!typeHnd.IsTypeDesc());  // heap objects must have method tables
    MethodTable* pMT = typeHnd.AsMethodTable();

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GetThread()->DisableStressHeap();
    }
#endif // _DEBUG

    newobj = TryAllocateFrozenObject(pMT);
    if (newobj == NULL)
    {
        // Fallback to normal heap allocation.
        newobj = AllocateObject(pMT);
    }

    HELPER_METHOD_FRAME_END();
    return(OBJECTREFToObject(newobj));
}
HCIMPLEND


//========================================================================
//
//      STRING HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

//*************************************************************
// Allocation fast path for typical objects
//
HCIMPL1_RAW(StringObject*, AllocateString_MP_FastPortable, DWORD stringLength)
{
    CONTRACTL {
        THROWS;
        DISABLED(GC_TRIGGERS);
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());

    // Instead of doing elaborate overflow checks, we just limit the number of elements. This will avoid all overflow
    // problems, as well as making sure big string objects are correctly allocated in the big object heap.
    if (stringLength >= (LARGE_OBJECT_SIZE - 256) / sizeof(WCHAR))
    {
        // Tail call to the slow helper
        return HCCALL1(FramedAllocateString, stringLength);
    }

    ee_alloc_context *eeAllocContext = &t_runtime_thread_locals.alloc_context;
    gc_alloc_context *allocContext = &eeAllocContext->m_GCAllocContext;

    SIZE_T totalSize = StringObject::GetSize(stringLength);

    // The method table's base size includes space for a terminating null character
    _ASSERTE(totalSize >= g_pStringClass->GetBaseSize());
    _ASSERTE((totalSize - g_pStringClass->GetBaseSize()) / sizeof(WCHAR) == stringLength);

    SIZE_T alignedTotalSize = ALIGN_UP(totalSize, DATA_ALIGNMENT);
    _ASSERTE(alignedTotalSize >= totalSize);
    totalSize = alignedTotalSize;

    BYTE *allocPtr = allocContext->alloc_ptr;
    _ASSERTE(allocPtr <= eeAllocContext->getCombinedLimit());
    if (totalSize > static_cast<SIZE_T>(eeAllocContext->getCombinedLimit() - allocPtr))
    {
        // Tail call to the slow helper
        return HCCALL1(FramedAllocateString, stringLength);
    }
    allocContext->alloc_ptr = allocPtr + totalSize;

    _ASSERTE(allocPtr != nullptr);
    StringObject *stringObject = reinterpret_cast<StringObject *>(allocPtr);
    stringObject->SetMethodTable(g_pStringClass);
    stringObject->SetStringLength(stringLength);
    _ASSERTE(stringObject->GetBuffer()[stringLength] == W('\0'));

    return stringObject;
}
HCIMPLEND_RAW

#include <optdefault.h>

HCIMPL1(StringObject*, FramedAllocateString, DWORD stringLength)
{
    FCALL_CONTRACT;

    STRINGREF result = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    result = AllocateString(stringLength);

    HELPER_METHOD_FRAME_END();
    return((StringObject*) OBJECTREFToObject(result));
}
HCIMPLEND

/*********************************************************************/
OBJECTHANDLE ConstructStringLiteral(CORINFO_MODULE_HANDLE scopeHnd, mdToken metaTok, void** ppPinnedString)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    _ASSERTE(TypeFromToken(metaTok) == mdtString);

    Module* module = GetModule(scopeHnd);
    return module->ResolveStringRef(metaTok, ppPinnedString);
}

/*********************************************************************/
HCIMPL2(Object *, JIT_StrCns, unsigned rid, CORINFO_MODULE_HANDLE scopeHnd)
{
    FCALL_CONTRACT;

    OBJECTHANDLE hndStr = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    // Retrieve the handle to the COM+ string object.
    hndStr = ConstructStringLiteral(scopeHnd, RidToToken(rid, mdtString));
    HELPER_METHOD_FRAME_END();

    // Don't use ObjectFromHandle; this isn't a real handle
    return *(Object**)hndStr;
}
HCIMPLEND


//========================================================================
//
//      ARRAY HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

//*************************************************************
// Array allocation fast path for arrays of value type elements
//
HCIMPL2_RAW(Object*, JIT_NewArr1VC_MP_FastPortable, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
{
    CONTRACTL {
        THROWS;
        DISABLED(GC_TRIGGERS);
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());

    // Do a conservative check here.  This is to avoid overflow while doing the calculations.  We don't
    // have to worry about "large" objects, since the allocation quantum is never big enough for
    // LARGE_OBJECT_SIZE.
    //
    // For Value Classes, this needs to be 2^16 - slack (2^32 / max component size),
    // The slack includes the size for the array header and round-up ; for alignment.  Use 256 for the
    // slack value out of laziness.
    SIZE_T componentCount = static_cast<SIZE_T>(size);
    if (componentCount >= static_cast<SIZE_T>(65535 - 256))
    {
        // Tail call to the slow helper
        return HCCALL2(JIT_NewArr1, arrayMT, size);
    }

    ee_alloc_context* eeAllocContext = &t_runtime_thread_locals.alloc_context;
    gc_alloc_context* allocContext = &eeAllocContext->m_GCAllocContext;

    MethodTable *pArrayMT = (MethodTable *)arrayMT;

    _ASSERTE(pArrayMT->HasComponentSize());
    SIZE_T componentSize = pArrayMT->RawGetComponentSize();
    SIZE_T totalSize = componentCount * componentSize;
    _ASSERTE(totalSize / componentSize == componentCount);

    SIZE_T baseSize = pArrayMT->GetBaseSize();
    totalSize += baseSize;
    _ASSERTE(totalSize >= baseSize);

    SIZE_T alignedTotalSize = ALIGN_UP(totalSize, DATA_ALIGNMENT);
    _ASSERTE(alignedTotalSize >= totalSize);
    totalSize = alignedTotalSize;

    BYTE *allocPtr = allocContext->alloc_ptr;
    _ASSERTE(allocPtr <= eeAllocContext->getCombinedLimit());
    if (totalSize > static_cast<SIZE_T>(eeAllocContext->getCombinedLimit() - allocPtr))
    {
        // Tail call to the slow helper
        return HCCALL2(JIT_NewArr1, arrayMT, size);
    }
    allocContext->alloc_ptr = allocPtr + totalSize;

    _ASSERTE(allocPtr != nullptr);
    ArrayBase *array = reinterpret_cast<ArrayBase *>(allocPtr);
    array->SetMethodTable(pArrayMT);
    _ASSERTE(static_cast<DWORD>(componentCount) == componentCount);
    array->m_NumComponents = static_cast<DWORD>(componentCount);

    return array;
}
HCIMPLEND_RAW

//*************************************************************
// Array allocation fast path for arrays of object elements
//
HCIMPL2_RAW(Object*, JIT_NewArr1OBJ_MP_FastPortable, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
{
    CONTRACTL {
        THROWS;
        DISABLED(GC_TRIGGERS);
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());

    // Make sure that the total size cannot reach LARGE_OBJECT_SIZE, which also allows us to avoid overflow checks. The
    // "256" slack is to cover the array header size and round-up, using a constant value here out of laziness.
    SIZE_T componentCount = static_cast<SIZE_T>(size);
    if (componentCount >= static_cast<SIZE_T>((LARGE_OBJECT_SIZE - 256) / sizeof(void *)))
    {
        // Tail call to the slow helper
        return HCCALL2(JIT_NewArr1, arrayMT, size);
    }

    SIZE_T totalSize = componentCount * sizeof(void *);
    _ASSERTE(totalSize / sizeof(void *) == componentCount);

    MethodTable *pArrayMT = (MethodTable *)arrayMT;

    SIZE_T baseSize = pArrayMT->GetBaseSize();
    totalSize += baseSize;
    _ASSERTE(totalSize >= baseSize);

    _ASSERTE(ALIGN_UP(totalSize, DATA_ALIGNMENT) == totalSize);

    ee_alloc_context* eeAllocContext = &t_runtime_thread_locals.alloc_context;
    gc_alloc_context* allocContext = &eeAllocContext->m_GCAllocContext;
    BYTE *allocPtr = allocContext->alloc_ptr;
    _ASSERTE(allocPtr <= eeAllocContext->getCombinedLimit());
    if (totalSize > static_cast<SIZE_T>(eeAllocContext->getCombinedLimit() - allocPtr))
    {
        // Tail call to the slow helper
        return HCCALL2(JIT_NewArr1, arrayMT, size);
    }
    allocContext->alloc_ptr = allocPtr + totalSize;

    _ASSERTE(allocPtr != nullptr);
    ArrayBase *array = reinterpret_cast<ArrayBase *>(allocPtr);
    array->SetMethodTable(pArrayMT);
    _ASSERTE(static_cast<DWORD>(componentCount) == componentCount);
    array->m_NumComponents = static_cast<DWORD>(componentCount);

    return array;
}
HCIMPLEND_RAW

#include <optdefault.h>

/*************************************************************/
HCIMPL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
{
    FCALL_CONTRACT;

    OBJECTREF newArray = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    MethodTable *pArrayMT = (MethodTable *)arrayMT;

    _ASSERTE(pArrayMT->IsFullyLoaded());
    _ASSERTE(pArrayMT->IsArray());
    _ASSERTE(!pArrayMT->IsMultiDimArray());

    if (size < 0)
        COMPlusThrow(kOverflowException);

#ifdef HOST_64BIT
    // Even though ECMA allows using a native int as the argument to newarr instruction
    // (therefore size is INT_PTR), ArrayBase::m_NumComponents is 32-bit, so even on 64-bit
    // platforms we can't create an array whose size exceeds 32 bits.
    if (size > INT_MAX)
        EX_THROW(EEMessageException, (kOverflowException, IDS_EE_ARRAY_DIMENSIONS_EXCEEDED));
#endif

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GetThread()->DisableStressHeap();
    }
#endif // _DEBUG

    newArray = AllocateSzArray(pArrayMT, (INT32)size);
    HELPER_METHOD_FRAME_END();

    return(OBJECTREFToObject(newArray));
}
HCIMPLEND


/*************************************************************/
HCIMPL2(Object*, JIT_NewArr1MaybeFrozen, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
{
    FCALL_CONTRACT;

    OBJECTREF newArray = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    MethodTable* pArrayMT = (MethodTable*)arrayMT;

    _ASSERTE(pArrayMT->IsFullyLoaded());
    _ASSERTE(pArrayMT->IsArray());
    _ASSERTE(!pArrayMT->IsMultiDimArray());

    if (size < 0)
        COMPlusThrow(kOverflowException);

#ifdef HOST_64BIT
    // Even though ECMA allows using a native int as the argument to newarr instruction
    // (therefore size is INT_PTR), ArrayBase::m_NumComponents is 32-bit, so even on 64-bit
    // platforms we can't create an array whose size exceeds 32 bits.
    if (size > INT_MAX)
        EX_THROW(EEMessageException, (kOverflowException, IDS_EE_ARRAY_DIMENSIONS_EXCEEDED));
#endif

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GetThread()->DisableStressHeap();
    }
#endif // _DEBUG

    newArray = TryAllocateFrozenSzArray(pArrayMT, (INT32)size);
    if (newArray == NULL)
    {
        // Fallback to default heap allocation
        newArray = AllocateSzArray(pArrayMT, (INT32)size);
    }
    _ASSERTE(newArray != NULL);

    HELPER_METHOD_FRAME_END();

    return(OBJECTREFToObject(newArray));
}
HCIMPLEND

/*************************************************************/
HCIMPL3(Object*, JIT_NewMDArr, CORINFO_CLASS_HANDLE classHnd, unsigned dwNumArgs, INT32 * pArgList)
{
    FCALL_CONTRACT;

    OBJECTREF    ret = 0;
    HELPER_METHOD_FRAME_BEGIN_RET_1(ret);    // Set up a frame

    TypeHandle typeHnd(classHnd);
    _ASSERTE(typeHnd.IsFullyLoaded());
    _ASSERTE(typeHnd.GetMethodTable()->IsArray());

    ret = AllocateArrayEx(typeHnd, pArgList, dwNumArgs);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(ret);
}
HCIMPLEND

#include <optdefault.h>

//========================================================================
//
//      VALUETYPE/BYREF HELPERS
//
//========================================================================
/*************************************************************/
HCIMPL2_RAW(Object*, JIT_Box_MP_FastPortable, CORINFO_CLASS_HANDLE type, void* unboxedData)
{
    CONTRACTL {
        THROWS;
        DISABLED(GC_TRIGGERS);
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    if (unboxedData == nullptr)
    {
        // Tail call to the slow helper
        return HCCALL2(JIT_Box, type, unboxedData);
    }

    _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());
    ee_alloc_context* eeAllocContext = &t_runtime_thread_locals.alloc_context;
    gc_alloc_context* allocContext = &eeAllocContext->m_GCAllocContext;

    TypeHandle typeHandle(type);
    _ASSERTE(!typeHandle.IsTypeDesc()); // heap objects must have method tables
    MethodTable *methodTable = typeHandle.AsMethodTable();
    // The fast helper should never be called for nullable types.
    _ASSERTE(!methodTable->IsNullable());

#ifdef FEATURE_64BIT_ALIGNMENT
    if (methodTable->RequiresAlign8())
    {
        return HCCALL2(JIT_Box, type, unboxedData);
    }
#endif

    SIZE_T size = methodTable->GetBaseSize();
    _ASSERTE(size % DATA_ALIGNMENT == 0);

    BYTE *allocPtr = allocContext->alloc_ptr;
    _ASSERTE(allocPtr <= eeAllocContext->getCombinedLimit());
    if (size > static_cast<SIZE_T>(eeAllocContext->getCombinedLimit() - allocPtr))
    {
        // Tail call to the slow helper
        return HCCALL2(JIT_Box, type, unboxedData);
    }

    allocContext->alloc_ptr = allocPtr + size;

    _ASSERTE(allocPtr != nullptr);
    Object *object = reinterpret_cast<Object *>(allocPtr);
    _ASSERTE(object->HasEmptySyncBlockInfo());
    object->SetMethodTable(methodTable);

    // Copy the data into the object
    CopyValueClass(object->UnBox(), unboxedData, methodTable);

    return object;
}
HCIMPLEND_RAW

/*************************************************************/
HCIMPL2(Object*, JIT_Box, CORINFO_CLASS_HANDLE type, void* unboxedData)
{
    FCALL_CONTRACT;

    OBJECTREF newobj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();    // Set up a frame
    GCPROTECT_BEGININTERIOR(unboxedData);
    HELPER_METHOD_POLL();

    // A null can be passed for boxing of a null ref.
    if (unboxedData == NULL)
        COMPlusThrow(kNullReferenceException);

    TypeHandle clsHnd(type);

    _ASSERTE(!clsHnd.IsTypeDesc());  // boxable types have method tables

    MethodTable *pMT = clsHnd.AsMethodTable();

    _ASSERTE(pMT->IsFullyLoaded());

    _ASSERTE (pMT->IsValueType() && !pMT->IsByRefLike());

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GetThread()->DisableStressHeap();
    }
#endif // _DEBUG

    newobj = pMT->FastBox(&unboxedData);

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();
    return(OBJECTREFToObject(newobj));
}
HCIMPLEND

/*************************************************************/
HCIMPL2_IV(LPVOID, JIT_GetRefAny, CORINFO_CLASS_HANDLE type, TypedByRef typedByRef)
{
    FCALL_CONTRACT;

    TypeHandle clsHnd(type);
    // <TODO>@TODO right now we check for precisely the correct type.
    // do we want to allow inheritance?  (watch out since value
    // classes inherit from object but do not normal object layout).</TODO>
    if (clsHnd != typedByRef.type) {
        FCThrow(kInvalidCastException);
    }

    return(typedByRef.data);
}
HCIMPLEND


/*************************************************************/
HCIMPL2(BOOL, JIT_IsInstanceOfException, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;
    TypeHandle clsHnd(type);
    return ExceptionIsOfRightType(clsHnd, obj->GetTypeHandle());
}
HCIMPLEND

extern "C" void QCALLTYPE ThrowInvalidCastException(CORINFO_CLASS_HANDLE pTargetType, CORINFO_CLASS_HANDLE pSourceType)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    TypeHandle targetType(pTargetType);
    TypeHandle sourceType(pSourceType);

    GCX_COOP();

    COMPlusThrowInvalidCastException(sourceType, targetType);

    END_QCALL;
}

//========================================================================
//
//      GENERICS HELPERS
//
//========================================================================

extern "C" CORINFO_GENERIC_HANDLE QCALLTYPE GenericHandleWorker(MethodDesc * pMD, MethodTable * pMT, LPVOID signature, DWORD dictionaryIndexAndSlot, Module* pModule)
{
    QCALL_CONTRACT;

    CORINFO_GENERIC_HANDLE result = NULL;

    BEGIN_QCALL;

    _ASSERTE(pMT != NULL || pMD != NULL);
    _ASSERTE(pMT == NULL || pMD == NULL);

    uint32_t dictionaryIndex = 0;
    MethodTable * pDeclaringMT = NULL;

    if (pMT != NULL)
    {
        if (pModule != NULL)
        {
#ifdef _DEBUG
            // Only in R2R mode are the module, dictionary index and dictionary slot provided as an input
            _ASSERTE(dictionaryIndexAndSlot != (DWORD)-1);
            _ASSERT(ReadyToRunInfo::IsNativeImageSharedBy(pModule, ExecutionManager::FindReadyToRunModule(dac_cast<TADDR>(signature))));
#endif
            dictionaryIndex = (dictionaryIndexAndSlot >> 16);
        }
        else
        {
            SigPointer ptr((PCCOR_SIGNATURE)signature);

            uint32_t kind; // DictionaryEntryKind
            IfFailThrow(ptr.GetData(&kind));

            // We need to normalize the class passed in (if any) for reliability purposes. That's because preparation of a code region that
            // contains these handle lookups depends on being able to predict exactly which lookups are required (so we can pre-cache the
            // answers and remove any possibility of failure at runtime). This is hard to do if the lookup (in this case the lookup of the
            // dictionary overflow cache) is keyed off the somewhat arbitrary type of the instance on which the call is made (we'd need to
            // prepare for every possible derived type of the type containing the method). So instead we have to locate the exactly
            // instantiated (non-shared) super-type of the class passed in.

            _ASSERTE(dictionaryIndexAndSlot == (DWORD)-1);
            IfFailThrow(ptr.GetData(&dictionaryIndex));
        }

        pDeclaringMT = pMT;
        while (true)
        {
            MethodTable * pParentMT = pDeclaringMT->GetParentMethodTable();
            if (pParentMT->GetNumDicts() <= dictionaryIndex)
                break;
            pDeclaringMT = pParentMT;
        }
    }

    DictionaryEntry * pSlot;
    result = (CORINFO_GENERIC_HANDLE)Dictionary::PopulateEntry(pMD, pDeclaringMT, signature, FALSE, &pSlot, dictionaryIndexAndSlot, pModule);

    if (pMT != NULL && pDeclaringMT != pMT)
    {
        // If the dictionary on the base type got expanded, update the current type's base type dictionary
        // pointer to use the new one on the base type.

        Dictionary* pMTDictionary = pMT->GetPerInstInfo()[dictionaryIndex];
        Dictionary* pDeclaringMTDictionary = pDeclaringMT->GetPerInstInfo()[dictionaryIndex];
        if (pMTDictionary != pDeclaringMTDictionary)
        {
            TypeHandle** pPerInstInfo = (TypeHandle**)pMT->GetPerInstInfo();
            InterlockedExchangeT(pPerInstInfo + dictionaryIndex, (TypeHandle*)pDeclaringMTDictionary);
        }
    }

    END_QCALL;

    return result;
} // GenericHandleWorker

FieldDesc* g_pVirtualFunctionPointerCache;

void FlushGenericCache(PTR_GenericCacheStruct genericCache)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    int32_t lastSize = genericCache->CacheElementCount();
    if (lastSize < genericCache->GetInitialCacheSize())
    {
        lastSize = genericCache->GetInitialCacheSize();
    }

    // store the last size to use when creating a new table
    // it is just a hint, not needed for correctness, so no synchronization
    // with the writing of the table
    genericCache->SetLastFlushSize(lastSize);
    // flushing is just replacing the table with a sentinel.
    genericCache->SetTable(genericCache->GetSentinelTable());
}

void FlushVirtualFunctionPointerCaches()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    FieldDesc *virtualCache = VolatileLoad(&g_pVirtualFunctionPointerCache);

    if (virtualCache != NULL)
    {
        // We can't use GetCurrentStaticAddress, as that may throw, since it will attempt to
        // allocate memory for statics if that hasn't happened yet. But, since we force the
        // statics memory to be allocated before initializing g_pVirtualFunctionPointerCache
        // we can safely use the combo of GetBase and GetStaticAddress here.
        FlushGenericCache((PTR_GenericCacheStruct)virtualCache->GetStaticAddress(virtualCache->GetBase()));
    }
}

/*********************************************************************/
// Resolve a virtual method at run-time, either because of
// aggressive backpatching or because the call is to a generic
// method which is itself virtual.
//
// classHnd is the actual run-time type for the call is made. (May be NULL for cases where methodHnd describes an interface)
// methodHnd is the exact (instantiated) method descriptor corresponding to the
// static method signature (i.e. might be for a superclass of classHnd)

// slow helper to call from the fast one
extern "C" void* QCALLTYPE ResolveVirtualFunctionPointer(QCall::ObjectHandleOnStack obj,
                                                       CORINFO_CLASS_HANDLE classHnd,
                                                       CORINFO_METHOD_HANDLE methodHnd)
{
    QCALL_CONTRACT;

    // The address of the method that's returned.
    CORINFO_MethodPtr   addr = NULL;

    BEGIN_QCALL;

    if (VolatileLoadWithoutBarrier(&g_pVirtualFunctionPointerCache) == NULL)
    {
        {
            GCX_COOP();
            CoreLibBinder::GetClass(CLASS__VIRTUALDISPATCHHELPERS)->CheckRunClassInitThrowing();
        }

        VolatileStore(&g_pVirtualFunctionPointerCache, CoreLibBinder::GetField(FIELD__VIRTUALDISPATCHHELPERS__CACHE));
#ifdef DEBUG
        FieldDesc *virtualCache = VolatileLoad(&g_pVirtualFunctionPointerCache);
        GenericCacheStruct::ValidateLayout(virtualCache->GetApproxFieldTypeHandleThrowing().GetMethodTable());
#endif
    }

    GCX_COOP();
    OBJECTREF objRef = obj.Get();
    GCPROTECT_BEGIN(objRef);

    if (objRef == NULL)
        COMPlusThrow(kNullReferenceException);

    // This is the static method descriptor describing the call.
    // It is not the destination of the call, which we must compute.
    MethodDesc* pStaticMD = (MethodDesc*) methodHnd;
    TypeHandle staticTH(classHnd);

    if (staticTH.IsNull())
    {
        // This may be NULL on input for cases where the methodHnd is not an interface method, or if getting the method table from the
        // MethodDesc will return an exact type.
        if (pStaticMD->IsInterface())
        {
            staticTH = pStaticMD->GetMethodTable();
            _ASSERTE(!staticTH.IsCanonicalSubtype());
        }
    }

    pStaticMD->CheckRestore();

    // ReadyToRun: If the method was compiled using ldvirtftn to reference a non-virtual method
    // resolve without using the VirtualizedCode call path here.
    // This can happen if the method was converted from virtual to non-virtual after the R2R image was created.
    // While this is not a common scenario and is documented as a breaking change, we should still handle it
    // as we have no good scheme for reporting an actionable error here.
    if (!pStaticMD->IsVtableMethod())
    {
        addr = (CORINFO_MethodPtr) pStaticMD->GetMultiCallableAddrOfCode();
        _ASSERTE(addr);
    }
    else
    {
        // This is the new way of resolving a virtual call, including generic virtual methods.
        // The code is now also used by reflection, remoting etc.
        addr = (CORINFO_MethodPtr) pStaticMD->GetMultiCallableAddrOfVirtualizedCode(&objRef, staticTH);
        _ASSERTE(addr);
    }

    GCPROTECT_END();
    END_QCALL;

    return addr;
}

HCIMPL3(void, Jit_NativeMemSet, void* pDest, int value, size_t length)
{
    _ASSERTE(pDest != nullptr);
    FCALL_CONTRACT;
    memset(pDest, value, length);
}
HCIMPLEND

HCIMPL1(Object*, JIT_GetRuntimeFieldStub, CORINFO_FIELD_HANDLE field)
{
    FCALL_CONTRACT;

    OBJECTREF stubRuntimeField = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    FieldDesc *pField = (FieldDesc *)field;
    stubRuntimeField = (OBJECTREF)pField->GetStubFieldInfo();

    HELPER_METHOD_FRAME_END();

    return (OBJECTREFToObject(stubRuntimeField));
}
HCIMPLEND

HCIMPL1(Object*, JIT_GetRuntimeMethodStub, CORINFO_METHOD_HANDLE method)
{
    FCALL_CONTRACT;

    OBJECTREF stubRuntimeMethod = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    MethodDesc *pMethod = (MethodDesc *)method;
    stubRuntimeMethod = (OBJECTREF)pMethod->GetStubMethodInfo();

    HELPER_METHOD_FRAME_END();

    return (OBJECTREFToObject(stubRuntimeMethod));
}
HCIMPLEND


NOINLINE HCIMPL1(Object*, JIT_GetRuntimeType_Framed, CORINFO_CLASS_HANDLE type)
{
    FCALL_CONTRACT;

    TypeHandle typeHandle(type);

    // Array/other type handle case.
    OBJECTREF refType = typeHandle.GetManagedClassObjectIfExists();
    if (refType == NULL)
    {
        HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
        refType = typeHandle.GetManagedClassObject();
        HELPER_METHOD_FRAME_END();
    }

    return OBJECTREFToObject(refType);
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL1(Object*, JIT_GetRuntimeType, CORINFO_CLASS_HANDLE type)
{
    FCALL_CONTRACT;

    TypeHandle typeHnd(type);

    if (!typeHnd.IsTypeDesc())
    {
        // Most common... and fastest case
        OBJECTREF typePtr = typeHnd.AsMethodTable()->GetManagedClassObjectIfExists();
        if (typePtr != NULL)
        {
            return OBJECTREFToObject(typePtr);
        }
    }

    ENDFORBIDGC();
    return HCCALL1(JIT_GetRuntimeType_Framed, type);
}
HCIMPLEND

HCIMPL1(Object*, JIT_GetRuntimeType_MaybeNull, CORINFO_CLASS_HANDLE type)
{
    FCALL_CONTRACT;

    if (type == NULL)
        return NULL;;

    ENDFORBIDGC();
    return HCCALL1(JIT_GetRuntimeType, type);
}
HCIMPLEND
#include <optdefault.h>


// Helper for synchronized static methods in shared generics code
#include <optsmallperfcritical.h>
HCIMPL1(CORINFO_CLASS_HANDLE, JIT_GetClassFromMethodParam, CORINFO_METHOD_HANDLE methHnd_)
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(methHnd_ != NULL);
    } CONTRACTL_END;

    MethodDesc *  pMD = (MethodDesc*)  methHnd_;

    MethodTable * pMT = pMD->GetMethodTable();
    _ASSERTE(!pMT->IsSharedByGenericInstantiations());

    return((CORINFO_CLASS_HANDLE)pMT);
HCIMPLEND
#include <optdefault.h>



//========================================================================
//
//      MONITOR HELPERS
//
//========================================================================

/*********************************************************************/
NOINLINE static void JIT_MonEnter_Helper(Object* obj, BYTE* pbLockTaken, LPVOID __me)
{
    FC_INNER_PROLOG_NO_ME_SETUP();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objRef);

    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    GCPROTECT_BEGININTERIOR(pbLockTaken);

    if (GET_THREAD()->CatchAtSafePoint())
    {
        GET_THREAD()->PulseGCMode();
    }
    objRef->EnterObjMonitor();

    if (pbLockTaken != 0) *pbLockTaken = 1;

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

/*********************************************************************/
#include <optsmallperfcritical.h>

HCIMPL_MONHELPER(JIT_MonEnterWorker_Portable, Object* obj)
{
    FCALL_CONTRACT;

    if (obj != nullptr && obj->TryEnterObjMonitorSpinHelper())
    {
        MONHELPER_STATE(*pbLockTaken = 1);
        return;
    }

    FC_INNER_RETURN_VOID(JIT_MonEnter_Helper(obj, MONHELPER_ARG, GetEEFuncEntryPointMacro(JIT_MonEnter)));
}
HCIMPLEND

HCIMPL1(void, JIT_MonEnter_Portable, Object* obj)
{
    FCALL_CONTRACT;

    if (obj != nullptr && obj->TryEnterObjMonitorSpinHelper())
    {
        return;
    }

    FC_INNER_RETURN_VOID(JIT_MonEnter_Helper(obj, NULL, GetEEFuncEntryPointMacro(JIT_MonEnter)));
}
HCIMPLEND

HCIMPL2(void, JIT_MonReliableEnter_Portable, Object* obj, BYTE* pbLockTaken)
{
    FCALL_CONTRACT;

    if (obj != nullptr && obj->TryEnterObjMonitorSpinHelper())
    {
        *pbLockTaken = 1;
        return;
    }

    FC_INNER_RETURN_VOID(JIT_MonEnter_Helper(obj, pbLockTaken, GetEEFuncEntryPointMacro(JIT_MonReliableEnter)));
}
HCIMPLEND

#include <optdefault.h>


/*********************************************************************/
NOINLINE static void JIT_MonTryEnter_Helper(Object* obj, INT32 timeOut, BYTE* pbLockTaken)
{
    FC_INNER_PROLOG(JIT_MonTryEnter);

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objRef);

    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    if (timeOut < -1)
        COMPlusThrow(kArgumentOutOfRangeException);

    GCPROTECT_BEGININTERIOR(pbLockTaken);

    if (GET_THREAD()->CatchAtSafePoint())
    {
        GET_THREAD()->PulseGCMode();
    }

    BOOL result = objRef->TryEnterObjMonitor(timeOut);
    *pbLockTaken = result != FALSE;

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

#include <optsmallperfcritical.h>
HCIMPL3(void, JIT_MonTryEnter_Portable, Object* obj, INT32 timeOut, BYTE* pbLockTaken)
{
    FCALL_CONTRACT;

    AwareLock::EnterHelperResult result;
    Thread * pCurThread;

    if (obj == NULL)
    {
        goto FramedLockHelper;
    }

    if (timeOut < -1)
    {
        goto FramedLockHelper;
    }

    pCurThread = GetThread();

    if (pCurThread->CatchAtSafePoint())
    {
        goto FramedLockHelper;
    }

    result = obj->EnterObjMonitorHelper(pCurThread);
    if (result == AwareLock::EnterHelperResult_Entered)
    {
        *pbLockTaken = 1;
        return;
    }
    if (result == AwareLock::EnterHelperResult_Contention)
    {
        if (timeOut == 0)
        {
            return;
        }

        result = obj->EnterObjMonitorHelperSpin(pCurThread);
        if (result == AwareLock::EnterHelperResult_Entered)
        {
            *pbLockTaken = 1;
            return;
        }
    }

FramedLockHelper:
    FC_INNER_RETURN_VOID(JIT_MonTryEnter_Helper(obj, timeOut, pbLockTaken));
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
NOINLINE static void JIT_MonExit_Helper(Object* obj, BYTE* pbLockTaken)
{
    FC_INNER_PROLOG(JIT_MonExit);

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT|Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objRef);

    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    if (!objRef->LeaveObjMonitor())
        COMPlusThrow(kSynchronizationLockException);

    if (pbLockTaken != 0) *pbLockTaken = 0;

    if (GET_THREAD()->IsAbortRequested()) {
        GET_THREAD()->HandleThreadAbort();
    }

    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

NOINLINE static void JIT_MonExit_Signal(Object* obj)
{
    FC_INNER_PROLOG(JIT_MonExit);

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT|Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objRef);

    // Signal the event
    SyncBlock *psb = objRef->PassiveGetSyncBlock();
    if (psb != NULL)
        psb->QuickGetMonitor()->Signal();

    if (GET_THREAD()->IsAbortRequested()) {
        GET_THREAD()->HandleThreadAbort();
    }

    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

#include <optsmallperfcritical.h>
FCIMPL1(void, JIT_MonExit_Portable, Object* obj)
{
    FCALL_CONTRACT;

    AwareLock::LeaveHelperAction action;

    if (obj == NULL)
    {
        goto FramedLockHelper;
    }

    // Handle the simple case without erecting helper frame
    action = obj->LeaveObjMonitorHelper(GetThread());
    if (action == AwareLock::LeaveHelperAction_None)
    {
        return;
    }
    if (action == AwareLock::LeaveHelperAction_Signal)
    {
        FC_INNER_RETURN_VOID(JIT_MonExit_Signal(obj));
    }

FramedLockHelper:
    FC_INNER_RETURN_VOID(JIT_MonExit_Helper(obj, NULL));
}
HCIMPLEND

HCIMPL_MONHELPER(JIT_MonExitWorker_Portable, Object* obj)
{
    FCALL_CONTRACT;

    MONHELPER_STATE(_ASSERTE(pbLockTaken != NULL));
    MONHELPER_STATE(if (*pbLockTaken == 0) return;)

    AwareLock::LeaveHelperAction action;

    if (obj == NULL)
    {
        goto FramedLockHelper;
    }

    // Handle the simple case without erecting helper frame
    action = obj->LeaveObjMonitorHelper(GetThread());
    if (action == AwareLock::LeaveHelperAction_None)
    {
        MONHELPER_STATE(*pbLockTaken = 0;)
        return;
    }
    if (action == AwareLock::LeaveHelperAction_Signal)
    {
        MONHELPER_STATE(*pbLockTaken = 0;)
        FC_INNER_RETURN_VOID(JIT_MonExit_Signal(obj));
    }

FramedLockHelper:
    FC_INNER_RETURN_VOID(JIT_MonExit_Helper(obj, MONHELPER_ARG));
}
HCIMPLEND
#include <optdefault.h>

//========================================================================
//
//      EXCEPTION HELPERS
//
//========================================================================

// In general, we want to use COMPlusThrow to throw exceptions.  However,
// the IL_Throw helper is a special case.  Here, we're called from
// managed code.  We have a guarantee that the first FS:0 handler
// is our COMPlusFrameHandler.  We could call COMPlusThrow(), which pushes
// another handler, but there is a significant (10% on JGFExceptionBench)
// performance gain if we avoid this by calling RaiseTheException()
// directly.
//

/*************************************************************/

HCIMPL1(void, IL_Throw,  Object* obj)
{
    FCALL_CONTRACT;

    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    OBJECTREF oref = ObjectToOBJECTREF(obj);

#ifdef FEATURE_EH_FUNCLETS
    if (g_isNewExceptionHandlingEnabled)
    {
        Thread *pThread = GetThread();

        FrameWithCookie<SoftwareExceptionFrame> exceptionFrame;
        *(&exceptionFrame)->GetGSCookiePtr() = GetProcessGSCookie();
        RtlCaptureContext(exceptionFrame.GetContext());
        exceptionFrame.InitAndLink(pThread);

        FC_CAN_TRIGGER_GC();

        if (oref == 0)
            DispatchManagedException(kNullReferenceException);
        else
        if (!IsException(oref->GetMethodTable()))
        {
            GCPROTECT_BEGIN(oref);

            WrapNonCompliantException(&oref);

            GCPROTECT_END();
        }
        else
        {   // We know that the object derives from System.Exception

            // If the flag indicating ForeignExceptionRaise has been set,
            // then do not clear the "_stackTrace" field of the exception object.
            if (pThread->GetExceptionState()->IsRaisingForeignException())
            {
                ((EXCEPTIONREF)oref)->SetStackTraceString(NULL);
            }
            else
            {
                ((EXCEPTIONREF)oref)->ClearStackTracePreservingRemoteStackTrace();
            }
        }

        DispatchManagedException(oref, exceptionFrame.GetContext());
        FC_CAN_TRIGGER_GC_END();
        UNREACHABLE();
    }
#endif // FEATURE_EH_FUNCLETS

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

#if defined(_DEBUG) && defined(TARGET_X86)
    __helperframe.InsureInit(NULL);
    g_ExceptionEIP = (LPVOID)__helperframe.GetReturnAddress();
#endif // defined(_DEBUG) && defined(TARGET_X86)

    if (oref == 0)
        COMPlusThrow(kNullReferenceException);
    else
    if (!IsException(oref->GetMethodTable()))
    {
        GCPROTECT_BEGIN(oref);

        WrapNonCompliantException(&oref);

        GCPROTECT_END();
    }
    else
    {   // We know that the object derives from System.Exception

        // If the flag indicating ForeignExceptionRaise has been set,
        // then do not clear the "_stackTrace" field of the exception object.
        if (GetThread()->GetExceptionState()->IsRaisingForeignException())
        {
            ((EXCEPTIONREF)oref)->SetStackTraceString(NULL);
        }
        else
        {
            ((EXCEPTIONREF)oref)->ClearStackTracePreservingRemoteStackTrace();
        }
    }

    RaiseTheExceptionInternalOnly(oref, FALSE);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*************************************************************/

HCIMPL0(void, IL_Rethrow)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

#ifdef FEATURE_EH_FUNCLETS
    if (g_isNewExceptionHandlingEnabled)
    {
        Thread *pThread = GetThread();

        FrameWithCookie<SoftwareExceptionFrame> exceptionFrame;
        *(&exceptionFrame)->GetGSCookiePtr() = GetProcessGSCookie();
        RtlCaptureContext(exceptionFrame.GetContext());
        exceptionFrame.InitAndLink(pThread);

        ExInfo *pActiveExInfo = (ExInfo*)pThread->GetExceptionState()->GetCurrentExceptionTracker();

        ExInfo exInfo(pThread, pActiveExInfo->m_ptrs.ExceptionRecord, exceptionFrame.GetContext(), ExKind::None);

        FC_CAN_TRIGGER_GC();

        GCPROTECT_BEGIN(exInfo.m_exception);
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__EH__RH_RETHROW);
        DECLARE_ARGHOLDER_ARRAY(args, 2);

        args[ARGNUM_0] = PTR_TO_ARGHOLDER(pActiveExInfo);
        args[ARGNUM_1] = PTR_TO_ARGHOLDER(&exInfo);

        pThread->IncPreventAbort();

        //Ex.RhRethrow(ref ExInfo activeExInfo, ref ExInfo exInfo)
        CALL_MANAGED_METHOD_NORET(args)
        GCPROTECT_END();

        FC_CAN_TRIGGER_GC_END();
        UNREACHABLE();
    }
#endif

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    OBJECTREF throwable = GetThread()->GetThrowable();
    if (throwable != NULL)
    {
        RaiseTheExceptionInternalOnly(throwable, TRUE);
    }
    else
    {
        // This can only be the result of bad IL (or some internal EE failure).
        _ASSERTE(!"No throwable on rethrow");
        RealCOMPlusThrow(kInvalidProgramException, (UINT)IDS_EE_RETHROW_NOT_ALLOWED);
    }

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

#ifndef STATUS_STACK_BUFFER_OVERRUN  // Not defined yet in CESDK includes
# define STATUS_STACK_BUFFER_OVERRUN      ((NTSTATUS)0xC0000409L)
#endif

/*********************************************************************
 * Kill process without using any potentially corrupted data:
 *      o Do not throw an exception
 *      o Do not call any indirect/virtual functions
 *      o Do not depend on any global data
 *
 * This function is used by the security checks for unsafe buffers (VC's -GS checks)
 */

void DoJITFailFast ()
{
    CONTRACTL {
        MODE_ANY;
        WRAPPER(GC_TRIGGERS);
        WRAPPER(THROWS);
    } CONTRACTL_END;

    LOG((LF_ALWAYS, LL_FATALERROR, "Unsafe buffer security check failure: Buffer overrun detected"));

#ifdef _DEBUG
    if (g_pConfig->fAssertOnFailFast())
        _ASSERTE(!"About to FailFast. set DOTNET_AssertOnFailFast=0 if this is expected");
#endif

#ifndef TARGET_UNIX
    // Use the function provided by the C runtime.
    //
    // Ideally, this function is called directly from managed code so
    // that the address of the managed function will be included in the
    // error log. However, this function is also used by the stackwalker.
    // To keep things simple, we just call it from here.
#if defined(TARGET_X86)
    __report_gsfailure();
#else // !defined(TARGET_X86)
    // On AMD64/IA64/ARM, we need to pass a stack cookie, which will be saved in the context record
    // that is used to raise the buffer-overrun exception by __report_gsfailure.
    __report_gsfailure((ULONG_PTR)0);
#endif // defined(TARGET_X86)
#else // TARGET_UNIX
    if(ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, FailFast))
    {
        // Fire an ETW FailFast event
        FireEtwFailFast(W("Unsafe buffer security check failure: Buffer overrun detected"),
                       (const PVOID)GetThread()->GetFrame()->GetIP(),
                       STATUS_STACK_BUFFER_OVERRUN,
                       COR_E_EXECUTIONENGINE,
                       GetClrInstanceId());
    }

    CrashDumpAndTerminateProcess(STATUS_STACK_BUFFER_OVERRUN);
#endif // !TARGET_UNIX
}

HCIMPL0(void, JIT_FailFast)
{
    FCALL_CONTRACT;
    DoJITFailFast ();
}
HCIMPLEND

HCIMPL2(void, JIT_ThrowMethodAccessException, CORINFO_METHOD_HANDLE caller, CORINFO_METHOD_HANDLE callee)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    MethodDesc* pCallerMD = GetMethod(caller);

    _ASSERTE(pCallerMD != NULL);
    AccessCheckContext accessContext(pCallerMD);

    ThrowMethodAccessException(&accessContext, GetMethod(callee));

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

HCIMPL2(void, JIT_ThrowFieldAccessException, CORINFO_METHOD_HANDLE caller, CORINFO_FIELD_HANDLE callee)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    MethodDesc* pCallerMD = GetMethod(caller);

    _ASSERTE(pCallerMD != NULL);
    AccessCheckContext accessContext(pCallerMD);

    ThrowFieldAccessException(&accessContext, reinterpret_cast<FieldDesc *>(callee));

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND;

HCIMPL2(void, JIT_ThrowClassAccessException, CORINFO_METHOD_HANDLE caller, CORINFO_CLASS_HANDLE callee)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    MethodDesc* pCallerMD = GetMethod(caller);

    _ASSERTE(pCallerMD != NULL);
    AccessCheckContext accessContext(pCallerMD);

    ThrowTypeAccessException(&accessContext, TypeHandle(callee).GetMethodTable());

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND;

//========================================================================
//
//      DEBUGGER/PROFILER HELPERS
//
//========================================================================

/*********************************************************************/
// Called by the JIT whenever a cee_break instruction should be executed.
//
HCIMPL0(void, JIT_UserBreakpoint)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame

#ifdef DEBUGGING_SUPPORTED
    FrameWithCookie<DebuggerExitFrame> __def;

    MethodDescCallSite debuggerBreak(METHOD__DEBUGGER__BREAK);

    debuggerBreak.Call((ARG_SLOT*)NULL);

    __def.Pop();
#else // !DEBUGGING_SUPPORTED
    _ASSERTE(!"JIT_UserBreakpoint called, but debugging support is not available in this build.");
#endif // !DEBUGGING_SUPPORTED

    HELPER_METHOD_FRAME_END_POLL();
}
HCIMPLEND

#if defined(_MSC_VER)
// VC++ Compiler intrinsic.
extern "C" void * _ReturnAddress(void);
#endif

/*********************************************************************/
// Callback for Just-My-Code probe
// Probe looks like:
//  if (*pFlag != 0) call JIT_DbgIsJustMyCode
// So this is only called if the flag (obtained by GetJMCFlagAddr) is
// non-zero.
HCIMPL0(void, JIT_DbgIsJustMyCode)
{
    FCALL_CONTRACT;

    // We need to get both the ip of the managed function this probe is in
    // (which will be our return address) and the frame pointer for that
    // function (since we can't get it later because we're pushing unmanaged
    // frames on the stack).
    void * ip = NULL;

    // <NOTE>
    // In order for the return address to be correct, we must NOT call any
    // function before calling _ReturnAddress().
    // </NOTE>
    ip = _ReturnAddress();

    _ASSERTE(ip != NULL);

    // Call into debugger proper
    g_pDebugInterface->OnMethodEnter(ip);

    return;
}
HCIMPLEND

#ifdef PROFILING_SUPPORTED

//---------------------------------------------------------------------------------------
//
// Sets the profiler's enter/leave/tailcall hooks into the JIT's dynamic helper
// function table.
//
// Arguments:
//      pFuncEnter - Enter hook
//      pFuncLeave - Leave hook
//      pFuncTailcall - Tailcall hook
//
//      For each hook parameter, if NULL is passed in, that will cause the JIT
//      to insert calls to its default stub replacement for that hook, which
//      just does a ret.
//
// Return Value:
//      HRESULT indicating success or failure
//
// Notes:
//      On IA64, this will allocate space for stubs to update GP, and that
//      allocation may take locks and may throw on failure.  Callers be warned.
//

HRESULT EEToProfInterfaceImpl::SetEnterLeaveFunctionHooksForJit(FunctionEnter3 * pFuncEnter,
                                                                FunctionLeave3 * pFuncLeave,
                                                                FunctionTailcall3 * pFuncTailcall)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    SetJitHelperFunction(
        CORINFO_HELP_PROF_FCN_ENTER,
        (pFuncEnter == NULL) ?
            reinterpret_cast<void *>(JIT_ProfilerEnterLeaveTailcallStub) :
            reinterpret_cast<void *>(pFuncEnter));

    SetJitHelperFunction(
        CORINFO_HELP_PROF_FCN_LEAVE,
        (pFuncLeave == NULL) ?
            reinterpret_cast<void *>(JIT_ProfilerEnterLeaveTailcallStub) :
            reinterpret_cast<void *>(pFuncLeave));

    SetJitHelperFunction(
        CORINFO_HELP_PROF_FCN_TAILCALL,
        (pFuncTailcall == NULL) ?
            reinterpret_cast<void *>(JIT_ProfilerEnterLeaveTailcallStub) :
            reinterpret_cast<void *>(pFuncTailcall));

    return (S_OK);
}
#endif // PROFILING_SUPPORTED




//========================================================================
//
//      GC HELPERS
//
//========================================================================

/*************************************************************/
// Slow helper to tailcall from the fast one
NOINLINE HCIMPL0(void, JIT_PollGC_Framed)
{
    BEGIN_PRESERVE_LAST_ERROR;

    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame
#ifdef _DEBUG
    BOOL GCOnTransition = FALSE;
    if (g_pConfig->FastGCStressLevel()) {
        GCOnTransition = GC_ON_TRANSITIONS (FALSE);
    }
#endif // _DEBUG
    CommonTripThread();         // Indicate we are at a GC safe point
#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GC_ON_TRANSITIONS (GCOnTransition);
    }
#endif // _DEBUG
    HELPER_METHOD_FRAME_END();
    END_PRESERVE_LAST_ERROR;
}
HCIMPLEND

HCIMPL0(VOID, JIT_PollGC)
{
    FCALL_CONTRACT;

    // As long as we can have GCPOLL_CALL polls, it would not hurt to check the trap flag.
    if (!g_TrapReturningThreads)
        return;

    // Does someone want this thread stopped?
    if (!GetThread()->CatchAtSafePoint())
        return;

    // Tailcall to the slow helper
    ENDFORBIDGC();
    HCCALL0(JIT_PollGC_Framed);
}
HCIMPLEND


/*************************************************************/
// This helper is similar to JIT_RareDisableHelper, but has more operations
// tailored to the post-pinvoke operations.
extern "C" FCDECL0(VOID, JIT_PInvokeEndRarePath);

HCIMPL0(void, JIT_PInvokeEndRarePath)
{
    BEGIN_PRESERVE_LAST_ERROR;

    FCALL_CONTRACT;

    Thread *thread = GetThread();

    // We need to disable the implicit FORBID GC region that exists inside an FCALL
    // in order to call RareDisablePreemptiveGC().
    FC_CAN_TRIGGER_GC();
    thread->RareDisablePreemptiveGC();
    FC_CAN_TRIGGER_GC_END();

    FC_GC_POLL_NOT_NEEDED();

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame
    thread->HandleThreadAbort();
    HELPER_METHOD_FRAME_END();

    thread->m_pFrame->Pop(thread);

    END_PRESERVE_LAST_ERROR;
}
HCIMPLEND

/*************************************************************/
// For an inlined N/Direct call (and possibly for other places that need this service)
// we have noticed that the returning thread should trap for one reason or another.
// ECall sets up the frame.

extern "C" FCDECL0(VOID, JIT_RareDisableHelper);

#if defined(TARGET_ARM) || defined(TARGET_AMD64)
// The JIT expects this helper to preserve the return value on AMD64 and ARM. We should eventually
// switch other platforms to the same convention since it produces smaller code.
extern "C" FCDECL0(VOID, JIT_RareDisableHelperWorker);

HCIMPL0(void, JIT_RareDisableHelperWorker)
#else
HCIMPL0(void, JIT_RareDisableHelper)
#endif
{
    // We do this here (before we set up a frame), because the following scenario
    // We are in the process of doing an inlined pinvoke.  Since we are in preemtive
    // mode, the thread is allowed to continue.  The thread continues and gets a context
    // switch just after it has cleared the preemptive mode bit but before it gets
    // to this helper.    When we do our stack crawl now, we think this thread is
    // in cooperative mode (and believed that it was suspended in the SuspendEE), so
    // we do a getthreadcontext (on the unsuspended thread!) and get an EIP in jitted code.
    // and proceed.   Assume the crawl of jitted frames is proceeding on the other thread
    // when this thread wakes up and sets up a frame.   Eventually the other thread
    // runs out of jitted frames and sees the frame we just established.  This causes
    // an assert in the stack crawling code.  If this assert is ignored, however, we
    // will end up scanning the jitted frames twice, which will lead to GC holes
    //
    // <TODO>TODO:  It would be MUCH more robust if we should remember which threads
    // we suspended in the SuspendEE, and only even consider using EIP if it was suspended
    // in the first phase.
    //      </TODO>

    BEGIN_PRESERVE_LAST_ERROR;

    FCALL_CONTRACT;

    Thread *thread = GetThread();

    // We need to disable the implicit FORBID GC region that exists inside an FCALL
    // in order to call RareDisablePreemptiveGC().
    FC_CAN_TRIGGER_GC();
    thread->RareDisablePreemptiveGC();
    FC_CAN_TRIGGER_GC_END();

    FC_GC_POLL_NOT_NEEDED();

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame
    thread->HandleThreadAbort();
    HELPER_METHOD_FRAME_END();

    END_PRESERVE_LAST_ERROR;
}
HCIMPLEND

/*********************************************************************/
// This is called by the JIT after every instruction in fully interruptible
// code to make certain our GC tracking is OK
HCIMPL0(VOID, JIT_StressGC_NOP)
{
    FCALL_CONTRACT;
}
HCIMPLEND


HCIMPL0(VOID, JIT_StressGC)
{
    FCALL_CONTRACT;

#ifdef _DEBUG
    HELPER_METHOD_FRAME_BEGIN_0();    // Set up a frame

    bool fSkipGC = false;

    if (!fSkipGC)
        GCHeapUtilities::GetGCHeap()->GarbageCollect();

// <TODO>@TODO: the following ifdef is in error, but if corrected the
// compiler complains about the *__ms->pRetAddr() saying machine state
// doesn't allow -></TODO>
#ifdef _X86
                // Get the machine state, (from HELPER_METHOD_FRAME_BEGIN)
                // and wack our return address to a nop function
        BYTE* retInstrs = ((BYTE*) *__ms->pRetAddr()) - 4;
        _ASSERTE(retInstrs[-1] == 0xE8);                // it is a call instruction
                // Wack it to point to the JITStressGCNop instead
        InterlockedExchange((LONG*) retInstrs), (LONG) JIT_StressGC_NOP);
#endif // _X86

    HELPER_METHOD_FRAME_END();
#endif // _DEBUG
}
HCIMPLEND



FCIMPL0(INT32, JIT_GetCurrentManagedThreadId)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    Thread * pThread = GetThread();
    return pThread->GetThreadId();
}
FCIMPLEND


/*********************************************************************/
/* we don't use HCIMPL macros because we don't want the overhead even in debug mode */

HCIMPL1_RAW(Object*, JIT_CheckObj, Object* obj)
{
    FCALL_CONTRACT;

    if (obj != 0) {
        MethodTable* pMT = obj->GetMethodTable();
        if (!pMT->ValidateWithPossibleAV()) {
            _ASSERTE_ALL_BUILDS(!"Bad Method Table");
        }
    }
    return obj;
}
HCIMPLEND_RAW

static int loopChoice = 0;

// This function supports a JIT mode in which we're debugging the mechanism for loop cloning.
// We want to clone loops, then make a semi-random choice, on each execution of the loop,
// whether to run the original loop or the cloned copy.  We do this by incrementing the contents
// of a memory location, and testing whether the result is odd or even.  The "loopChoice" variable
// above provides that memory location, and this JIT helper merely informs the JIT of the address of
// "loopChoice".
HCIMPL0(void*, JIT_LoopCloneChoiceAddr)
{
     CONTRACTL {
        FCALL_CHECK;
     } CONTRACTL_END;

     return &loopChoice;
}
HCIMPLEND

// Prints a message that loop cloning optimization has occurred.
HCIMPL0(void, JIT_DebugLogLoopCloning)
{
     CONTRACTL {
        FCALL_CHECK;
     } CONTRACTL_END;

#ifdef _DEBUG
     printf(">> Logging loop cloning optimization\n");
#endif
}
HCIMPLEND

#ifdef FEATURE_ON_STACK_REPLACEMENT

// Helper method to jit the OSR version of a method.
//
// Returns the address of the jitted code.
// Returns NULL if osr method can't be created.
static PCODE JitPatchpointWorker(MethodDesc* pMD, EECodeInfo& codeInfo, int ilOffset)
{
    STANDARD_VM_CONTRACT;
    PCODE osrVariant = (PCODE)NULL;

    // Fetch the patchpoint info for the current method
    EEJitManager* jitMgr = ExecutionManager::GetEEJitManager();
    CodeHeader* codeHdr = jitMgr->GetCodeHeaderFromStartAddress(codeInfo.GetStartAddress());
    PTR_BYTE debugInfo = codeHdr->GetDebugInfo();
    PatchpointInfo* patchpointInfo = CompressDebugInfo::RestorePatchpointInfo(debugInfo);

    if (patchpointInfo == NULL)
    {
        // Unexpected, but not fatal
        STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "JitPatchpointWorker: failed to restore patchpoint info for Method=0x%pM\n", pMD);
        return (PCODE)NULL;
    }

    // Set up a new native code version for the OSR variant of this method.
    NativeCodeVersion osrNativeCodeVersion;
    {
        CodeVersionManager::LockHolder codeVersioningLockHolder;

        NativeCodeVersion currentNativeCodeVersion = codeInfo.GetNativeCodeVersion();
        ILCodeVersion ilCodeVersion = currentNativeCodeVersion.GetILCodeVersion();
        HRESULT hr = ilCodeVersion.AddNativeCodeVersion(pMD, NativeCodeVersion::OptimizationTier1OSR, &osrNativeCodeVersion, patchpointInfo, ilOffset);
        if (FAILED(hr))
        {
            // Unexpected, but not fatal
            STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "JitPatchpointWorker: failed to add native code version for Method=0x%pM\n", pMD);
            return (PCODE)NULL;
        }
    }

    // Invoke the jit to compile the OSR version
    LOG((LF_TIEREDCOMPILATION, LL_INFO10, "JitPatchpointWorker: creating OSR version of Method=0x%pM (%s::%s) at offset %d\n",
        pMD, pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, ilOffset));

    PrepareCodeConfigBuffer configBuffer(osrNativeCodeVersion);
    PrepareCodeConfig *config = configBuffer.GetConfig();
    osrVariant = pMD->PrepareCode(config);

    return osrVariant;
}

// Helper method wrapper to set up a frame so we can invoke methods that might GC
HCIMPL3(PCODE, JIT_Patchpoint_Framed, MethodDesc* pMD, EECodeInfo& codeInfo, int ilOffset)
{
    PCODE result = (PCODE)NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    GCX_PREEMP();
    result = JitPatchpointWorker(pMD, codeInfo, ilOffset);

    HELPER_METHOD_FRAME_END();

    return result;
}
HCIMPLEND

// Jit helper invoked at a patchpoint.
//
// Checks to see if this is a known patchpoint, if not,
// an entry is added to the patchpoint table.
//
// When the patchpoint has been hit often enough to trigger
// a transition, create an OSR method.
//
// Currently, counter is a pointer into the Tier0 method stack
// frame so we have exclusive access.

void JIT_Patchpoint(int* counter, int ilOffset)
{
    // BEGIN_PRESERVE_LAST_ERROR;
    DWORD dwLastError = ::GetLastError();

    // This method may not return normally
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    // Patchpoint identity is the helper return address
    PCODE ip = (PCODE)_ReturnAddress();

    // Fetch or setup patchpoint info for this patchpoint.
    EECodeInfo codeInfo(ip);
    MethodDesc* pMD = codeInfo.GetMethodDesc();
    LoaderAllocator* allocator = pMD->GetLoaderAllocator();
    OnStackReplacementManager* manager = allocator->GetOnStackReplacementManager();
    PerPatchpointInfo * ppInfo = manager->GetPerPatchpointInfo(ip);
    PCODE osrMethodCode = (PCODE)NULL;
    bool isNewMethod = false;

    // In the current prototype, counter is shared by all patchpoints
    // in a method, so no matter what happens below, we don't want to
    // impair those other patchpoints.
    //
    // One might be tempted, for instance, to set the counter for
    // invalid or ignored patchpoints to some high value to reduce
    // the amount of back and forth with the runtime, but this would
    // lock out other patchpoints in the method.
    //
    // So we always reset the counter to the bump value.
    //
    // In the prototype, counter is a location in a stack frame,
    // so we can update it without worrying about other threads.
    const int counterBump = g_pConfig->OSR_CounterBump();
    *counter = counterBump;

#ifdef _DEBUG
    const int ppId = ppInfo->m_patchpointId;
#endif

    // Is this a patchpoint that was previously marked as invalid? If so, just return to the Tier0 method.
    if ((ppInfo->m_flags & PerPatchpointInfo::patchpoint_invalid) == PerPatchpointInfo::patchpoint_invalid)
    {
        LOG((LF_TIEREDCOMPILATION, LL_INFO1000, "Jit_Patchpoint: invalid patchpoint [%d] (0x%p) in Method=0x%pM (%s::%s) at offset %d\n",
                ppId, ip, pMD, pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, ilOffset));

        goto DONE;
    }

    // See if we have an OSR method for this patchpoint.
    osrMethodCode = ppInfo->m_osrMethodCode;

    if (osrMethodCode == (PCODE)NULL)
    {
        // No OSR method yet, let's see if we should create one.
        //
        // First, optionally ignore some patchpoints to increase
        // coverage (stress mode).
        //
        // Because there are multiple patchpoints in a method, and
        // each OSR method covers the remainder of the method from
        // that point until the method returns, if we trigger on an
        // early patchpoint in a method, we may never see triggers on
        // a later one.

#ifdef _DEBUG
        const int lowId = g_pConfig->OSR_LowId();
        const int highId = g_pConfig->OSR_HighId();

        if ((ppId < lowId) || (ppId > highId))
        {
            LOG((LF_TIEREDCOMPILATION, LL_INFO10, "Jit_Patchpoint: ignoring patchpoint [%d] (0x%p) in Method=0x%pM (%s::%s) at offset %d\n",
                    ppId, ip, pMD, pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, ilOffset));
            goto DONE;
        }
#endif

        // Second, only request the OSR method if this patchpoint has
        // been hit often enough.
        //
        // Note the initial invocation of the helper depends on the
        // initial counter value baked into jitted code (call this J);
        // subsequent invocations depend on the counter bump (call
        // this B).
        //
        // J and B may differ, so the total number of loop iterations
        // before an OSR method is created is:
        //
        // J, if hitLimit <= 1;
        // J + (hitLimit-1)* B, if hitLimit > 1;
        //
        // Current thinking is:
        //
        // J should be in the range of tens to hundreds, so that newly
        // called Tier0 methods that already have OSR methods
        // available can transition to OSR methods quickly, but
        // methods called only a few times do not invoke this
        // helper and so create PerPatchpoint runtime state.
        //
        // B should be in the range of hundreds to thousands, so that
        // we're not too eager to create OSR methods (since there is
        // some jit cost), but are eager enough to transition before
        // we run too much Tier0 code.
        //
        const int hitLimit = g_pConfig->OSR_HitLimit();
        const int hitCount = InterlockedIncrement(&ppInfo->m_patchpointCount);
        const int hitLogLevel = (hitCount == 1) ? LL_INFO10 : LL_INFO1000;

        LOG((LF_TIEREDCOMPILATION, hitLogLevel, "Jit_Patchpoint: patchpoint [%d] (0x%p) hit %d in Method=0x%pM (%s::%s) [il offset %d] (limit %d)\n",
            ppId, ip, hitCount, pMD, pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, ilOffset, hitLimit));

        // Defer, if we haven't yet reached the limit
        if (hitCount < hitLimit)
        {
            goto DONE;
        }

        // Third, make sure no other thread is trying to create the OSR method.
        LONG oldFlags = ppInfo->m_flags;
        if ((oldFlags & PerPatchpointInfo::patchpoint_triggered) == PerPatchpointInfo::patchpoint_triggered)
        {
            LOG((LF_TIEREDCOMPILATION, LL_INFO1000, "Jit_Patchpoint: AWAITING OSR method for patchpoint [%d] (0x%p)\n", ppId, ip));
            goto DONE;
        }

        LONG newFlags = oldFlags | PerPatchpointInfo::patchpoint_triggered;
        BOOL triggerTransition = InterlockedCompareExchange(&ppInfo->m_flags, newFlags, oldFlags) == oldFlags;

        if (!triggerTransition)
        {
            LOG((LF_TIEREDCOMPILATION, LL_INFO1000, "Jit_Patchpoint: (lost race) AWAITING OSR method for patchpoint [%d] (0x%p)\n", ppId, ip));
            goto DONE;
        }

        // Time to create the OSR method.
        //
        // We currently do this synchronously. We could instead queue
        // up a request on some worker thread, like we do for
        // rejitting, and return control to the Tier0 method. It may
        // eventually return here, if the patchpoint is hit often
        // enough.
        //
        // There is a chance the async version will create methods
        // that are never used (just like there is a chance that Tier1
        // methods are ever called).
        //
        // In this prototype we want to expose bugs in the jitted code
        // for OSR methods, so we stick with synchronous creation.
        LOG((LF_TIEREDCOMPILATION, LL_INFO10, "Jit_Patchpoint: patchpoint [%d] (0x%p) TRIGGER at count %d\n", ppId, ip, hitCount));

        // Invoke the helper to build the OSR method
        osrMethodCode = HCCALL3(JIT_Patchpoint_Framed, pMD, codeInfo, ilOffset);

        // If that failed, mark the patchpoint as invalid.
        if (osrMethodCode == (PCODE)NULL)
        {
            // Unexpected, but not fatal
            STRESS_LOG3(LF_TIEREDCOMPILATION, LL_WARNING, "Jit_Patchpoint: patchpoint (0x%p) OSR method creation failed,"
                " marking patchpoint invalid for Method=0x%pM il offset %d\n", ip, pMD, ilOffset);

            InterlockedOr(&ppInfo->m_flags, (LONG)PerPatchpointInfo::patchpoint_invalid);
            goto DONE;
        }

        // We've successfully created the osr method; make it available.
        _ASSERTE(ppInfo->m_osrMethodCode == (PCODE)NULL);
        ppInfo->m_osrMethodCode = osrMethodCode;
        isNewMethod = true;
    }

    // If we get here, we have code to transition to...
    _ASSERTE(osrMethodCode != (PCODE)NULL);

    {
        Thread *pThread = GetThread();

#ifdef FEATURE_HIJACK
        // We can't crawl the stack of a thread that currently has a hijack pending
        // (since the hijack routine won't be recognized by any code manager). So we
        // Undo any hijack, the EE will re-attempt it later.
        pThread->UnhijackThread();
#endif

        // Find context for the original method
        CONTEXT *pFrameContext = NULL;
#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
        DWORD contextSize = 0;
        ULONG64 xStateCompactionMask = 0;
        DWORD contextFlags = CONTEXT_FULL;
        if (Thread::AreShadowStacksEnabled())
        {
            xStateCompactionMask = XSTATE_MASK_CET_U;
            contextFlags |= CONTEXT_XSTATE;
        }

        // The initialize call should fail but return contextSize
        BOOL success = g_pfnInitializeContext2 ?
            g_pfnInitializeContext2(NULL, contextFlags, NULL, &contextSize, xStateCompactionMask) :
            InitializeContext(NULL, contextFlags, NULL, &contextSize);

        _ASSERTE(!success && (GetLastError() == ERROR_INSUFFICIENT_BUFFER));

        PVOID pBuffer = _alloca(contextSize);
        success = g_pfnInitializeContext2 ?
            g_pfnInitializeContext2(pBuffer, contextFlags, &pFrameContext, &contextSize, xStateCompactionMask) :
            InitializeContext(pBuffer, contextFlags, &pFrameContext, &contextSize);
        _ASSERTE(success);
#else // TARGET_WINDOWS && TARGET_AMD64
        CONTEXT frameContext;
        frameContext.ContextFlags = CONTEXT_FULL;
        pFrameContext = &frameContext;
#endif // TARGET_WINDOWS && TARGET_AMD64

        // Find context for the original method
        RtlCaptureContext(pFrameContext);

#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
        if (Thread::AreShadowStacksEnabled())
        {
            pFrameContext->ContextFlags |= CONTEXT_XSTATE;
            SetXStateFeaturesMask(pFrameContext, xStateCompactionMask);
            SetSSP(pFrameContext, _rdsspq());
        }
#endif // TARGET_WINDOWS && TARGET_AMD64

        // Walk back to the original method frame
        pThread->VirtualUnwindToFirstManagedCallFrame(pFrameContext);

        // Remember original method FP and SP because new method will inherit them.
        UINT_PTR currentSP = GetSP(pFrameContext);
        UINT_PTR currentFP = GetFP(pFrameContext);

        // We expect to be back at the right IP
        if ((UINT_PTR)ip != GetIP(pFrameContext))
        {
            // Should be fatal
            STRESS_LOG2(LF_TIEREDCOMPILATION, LL_FATALERROR, "Jit_Patchpoint: patchpoint (0x%p) TRANSITION"
                " unexpected context IP 0x%p\n", ip, GetIP(pFrameContext));
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }

        // Now unwind back to the original method caller frame.
        EECodeInfo callerCodeInfo(GetIP(pFrameContext));
        ULONG_PTR establisherFrame = 0;
        PVOID handlerData = NULL;
        RtlVirtualUnwind(UNW_FLAG_NHANDLER, callerCodeInfo.GetModuleBase(), GetIP(pFrameContext), callerCodeInfo.GetFunctionEntry(),
            pFrameContext, &handlerData, &establisherFrame, NULL);

        // Now, set FP and SP back to the values they had just before this helper was called,
        // since the new method must have access to the original method frame.
        //
        // TODO: if we access the patchpointInfo here, we can read out the FP-SP delta from there and
        // use that to adjust the stack, likely saving some stack space.

#if defined(TARGET_AMD64)
        // If calls push the return address, we need to simulate that here, so the OSR
        // method sees the "expected" SP misalgnment on entry.
        _ASSERTE(currentSP % 16 == 0);
        currentSP -= 8;

#if defined(TARGET_WINDOWS)
        DWORD64 ssp = GetSSP(pFrameContext);
        if (ssp != 0)
        {
            SetSSP(pFrameContext, ssp - 8);
        }
#endif // TARGET_WINDOWS

        pFrameContext->Rbp = currentFP;
#endif // TARGET_AMD64

        SetSP(pFrameContext, currentSP);

        // Note we can get here w/o triggering, if there is an existing OSR method and
        // we hit the patchpoint.
        const int transitionLogLevel = isNewMethod ? LL_INFO10 : LL_INFO1000;
        LOG((LF_TIEREDCOMPILATION, transitionLogLevel, "Jit_Patchpoint: patchpoint [%d] (0x%p) TRANSITION to ip 0x%p\n", ppId, ip, osrMethodCode));

        // Install new entry point as IP
        SetIP(pFrameContext, osrMethodCode);

#ifdef _DEBUG
        // Keep this context around to aid in debugging OSR transition problems
        static CONTEXT s_lastOSRTransitionContext;
        s_lastOSRTransitionContext = *pFrameContext;
#endif

        // Restore last error (since call below does not return)
        // END_PRESERVE_LAST_ERROR;
        ::SetLastError(dwLastError);

        // Transition!
        ClrRestoreNonvolatileContext(pFrameContext);
    }

 DONE:

    // END_PRESERVE_LAST_ERROR;
    ::SetLastError(dwLastError);
}

// Jit helper invoked at a partial compilation patchpoint.
//
// Similar to Jit_Patchpoint, but invoked when execution
// reaches a point in a method where the continuation
// was never jitted (eg an exceptional path).
//
// Unlike regular patchpoints, partial compilation patchpoints
// must always transition.
//
HCIMPL1(VOID, JIT_PartialCompilationPatchpoint, int ilOffset)
{
    FCALL_CONTRACT;

    // BEGIN_PRESERVE_LAST_ERROR;
    DWORD dwLastError = ::GetLastError();
    PerPatchpointInfo* ppInfo = NULL;
    bool isNewMethod = false;
    CONTEXT* pFrameContext = NULL;

#if !defined(TARGET_WINDOWS) || !defined(TARGET_AMD64)
    CONTEXT originalFrameContext;
    originalFrameContext.ContextFlags = CONTEXT_FULL;
    pFrameContext = &originalFrameContext;
#endif

    // Patchpoint identity is the helper return address
    PCODE ip = (PCODE)_ReturnAddress();

#ifdef _DEBUG
    // Friendly ID number
    int ppId = 0;
#endif

    HELPER_METHOD_FRAME_BEGIN_0();

    // Fetch or setup patchpoint info for this patchpoint.
    EECodeInfo codeInfo(ip);
    MethodDesc* pMD = codeInfo.GetMethodDesc();
    LoaderAllocator* allocator = pMD->GetLoaderAllocator();
    OnStackReplacementManager* manager = allocator->GetOnStackReplacementManager();
    ppInfo = manager->GetPerPatchpointInfo(ip);

#ifdef _DEBUG
    ppId = ppInfo->m_patchpointId;
#endif

    // See if we have an OSR method for this patchpoint.
    DWORD backoffs = 0;

    // Enable GC while we jit or wait for the continuation to be jitted.
    {
        GCX_PREEMP();

        while (ppInfo->m_osrMethodCode == (PCODE)NULL)
        {
            // Invalid patchpoints are fatal, for partial compilation patchpoints
            //
            if ((ppInfo->m_flags & PerPatchpointInfo::patchpoint_invalid) == PerPatchpointInfo::patchpoint_invalid)
            {
                LOG((LF_TIEREDCOMPILATION, LL_FATALERROR, "Jit_PartialCompilationPatchpoint: invalid patchpoint [%d] (0x%p) in Method=0x%pM (%s::%s) at offset %d\n",
                        ppId, ip, pMD, pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, ilOffset));
                EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
            }

            // Make sure no other thread is trying to create the OSR method.
            //
            LONG oldFlags = ppInfo->m_flags;
            if ((oldFlags & PerPatchpointInfo::patchpoint_triggered) == PerPatchpointInfo::patchpoint_triggered)
            {
                LOG((LF_TIEREDCOMPILATION, LL_INFO1000, "Jit_PartialCompilationPatchpoint: AWAITING OSR method for patchpoint [%d] (0x%p)\n", ppId, ip));
                __SwitchToThread(0, backoffs++);
                continue;
            }

            // Make sure we win the race to create the OSR method
            //
            LONG newFlags = ppInfo->m_flags | PerPatchpointInfo::patchpoint_triggered;
            BOOL triggerTransition = InterlockedCompareExchange(&ppInfo->m_flags, newFlags, oldFlags) == oldFlags;

            if (!triggerTransition)
            {
                LOG((LF_TIEREDCOMPILATION, LL_INFO1000, "Jit_PartialCompilationPatchpoint: (lost race) AWAITING OSR method for patchpoint [%d] (0x%p)\n", ppId, ip));
                __SwitchToThread(0, backoffs++);
                continue;
            }

            // Invoke the helper to build the OSR method
            //
            // TODO: may not want to optimize this part of the method, if it's truly partial compilation
            // and can't possibly rejoin into the main flow.
            //
            // (but consider: throw path in method with try/catch, OSR method will contain more than just the throw?)
            //
            LOG((LF_TIEREDCOMPILATION, LL_INFO10, "Jit_PartialCompilationPatchpoint: patchpoint [%d] (0x%p) TRIGGER\n", ppId, ip));
            PCODE newMethodCode = JitPatchpointWorker(pMD, codeInfo, ilOffset);

            // If that failed, mark the patchpoint as invalid.
            // This is fatal, for partial compilation patchpoints
            //
            if (newMethodCode == (PCODE)NULL)
            {
                STRESS_LOG3(LF_TIEREDCOMPILATION, LL_WARNING, "Jit_PartialCompilationPatchpoint: patchpoint (0x%p) OSR method creation failed,"
                    " marking patchpoint invalid for Method=0x%pM il offset %d\n", ip, pMD, ilOffset);
                InterlockedOr(&ppInfo->m_flags, (LONG)PerPatchpointInfo::patchpoint_invalid);
                EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
                break;
            }

            // We've successfully created the osr method; make it available.
            _ASSERTE(ppInfo->m_osrMethodCode == (PCODE)NULL);
            ppInfo->m_osrMethodCode = newMethodCode;
            isNewMethod = true;
        }
    }

    // If we get here, we have code to transition to...
    PCODE osrMethodCode = ppInfo->m_osrMethodCode;
    _ASSERTE(osrMethodCode != (PCODE)NULL);

    Thread *pThread = GetThread();

#ifdef FEATURE_HIJACK
    // We can't crawl the stack of a thread that currently has a hijack pending
    // (since the hijack routine won't be recognized by any code manager). So we
    // Undo any hijack, the EE will re-attempt it later.
    pThread->UnhijackThread();
#endif

    // Find context for the original method
#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
    DWORD contextSize = 0;
    ULONG64 xStateCompactionMask = 0;
    DWORD contextFlags = CONTEXT_FULL;
    if (Thread::AreShadowStacksEnabled())
    {
        xStateCompactionMask = XSTATE_MASK_CET_U;
        contextFlags |= CONTEXT_XSTATE;
    }

    // The initialize call should fail but return contextSize
    BOOL success = g_pfnInitializeContext2 ?
        g_pfnInitializeContext2(NULL, contextFlags, NULL, &contextSize, xStateCompactionMask) :
        InitializeContext(NULL, contextFlags, NULL, &contextSize);

    _ASSERTE(!success && (GetLastError() == ERROR_INSUFFICIENT_BUFFER));

    PVOID pBuffer = _alloca(contextSize);
    success = g_pfnInitializeContext2 ?
        g_pfnInitializeContext2(pBuffer, contextFlags, &pFrameContext, &contextSize, xStateCompactionMask) :
        InitializeContext(pBuffer, contextFlags, &pFrameContext, &contextSize);
    _ASSERTE(success);
#else // TARGET_WINDOWS && TARGET_AMD64
    _ASSERTE(pFrameContext != nullptr);
#endif // TARGET_WINDOWS && TARGET_AMD64

    // Find context for the original method
    RtlCaptureContext(pFrameContext);

#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
    if (Thread::AreShadowStacksEnabled())
    {
        pFrameContext->ContextFlags |= CONTEXT_XSTATE;
        SetXStateFeaturesMask(pFrameContext, xStateCompactionMask);
        SetSSP(pFrameContext, _rdsspq());
    }
#endif // TARGET_WINDOWS && TARGET_AMD64

    // Walk back to the original method frame
    Thread::VirtualUnwindToFirstManagedCallFrame(pFrameContext);

    // Remember original method FP and SP because new method will inherit them.
    UINT_PTR currentSP = GetSP(pFrameContext);
    UINT_PTR currentFP = GetFP(pFrameContext);

    // We expect to be back at the right IP
    if ((UINT_PTR)ip != GetIP(pFrameContext))
    {
        // Should be fatal
        STRESS_LOG2(LF_TIEREDCOMPILATION, LL_INFO10, "Jit_PartialCompilationPatchpoint: patchpoint (0x%p) TRANSITION"
            " unexpected context IP 0x%p\n", ip, GetIP(pFrameContext));
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    }

    // Now unwind back to the original method caller frame.
    EECodeInfo callerCodeInfo(GetIP(pFrameContext));
    ULONG_PTR establisherFrame = 0;
    PVOID handlerData = NULL;
    RtlVirtualUnwind(UNW_FLAG_NHANDLER, callerCodeInfo.GetModuleBase(), GetIP(pFrameContext), callerCodeInfo.GetFunctionEntry(),
        pFrameContext, &handlerData, &establisherFrame, NULL);

    // Now, set FP and SP back to the values they had just before this helper was called,
    // since the new method must have access to the original method frame.
    //
    // TODO: if we access the patchpointInfo here, we can read out the FP-SP delta from there and
    // use that to adjust the stack, likely saving some stack space.

#if defined(TARGET_AMD64)
    // If calls push the return address, we need to simulate that here, so the OSR
    // method sees the "expected" SP misalgnment on entry.
    _ASSERTE(currentSP % 16 == 0);
    currentSP -= 8;

#if defined(TARGET_WINDOWS)
    DWORD64 ssp = GetSSP(pFrameContext);
    if (ssp != 0)
    {
        SetSSP(pFrameContext, ssp - 8);
    }
#endif // TARGET_WINDOWS

    pFrameContext->Rbp = currentFP;
#endif // TARGET_AMD64

    SetSP(pFrameContext, currentSP);

    // Note we can get here w/o triggering, if there is an existing OSR method and
    // we hit the patchpoint.
    const int transitionLogLevel = isNewMethod ? LL_INFO10 : LL_INFO1000;
    LOG((LF_TIEREDCOMPILATION, transitionLogLevel, "Jit_PartialCompilationPatchpoint: patchpoint [%d] (0x%p) TRANSITION to ip 0x%p\n", ppId, ip, osrMethodCode));

    // Install new entry point as IP
    SetIP(pFrameContext, osrMethodCode);

    // This method doesn't return normally so we have to manually restore things.
    HELPER_METHOD_FRAME_END();
    ENDFORBIDGC();
    ::SetLastError(dwLastError);

    // Transition!
    __asan_handle_no_return();
    ClrRestoreNonvolatileContext(pFrameContext);
}
HCIMPLEND

#else

void JIT_Patchpoint(int* counter, int ilOffset)
{
    // Stub version if OSR feature is disabled
    //
    // Should not be called.

    UNREACHABLE();
}

HCIMPL1(VOID, JIT_PartialCompilationPatchpoint, int ilOffset)
{
    // Stub version if OSR feature is disabled
    //
    // Should not be called.

    UNREACHABLE();
}
HCIMPLEND

#endif // FEATURE_ON_STACK_REPLACEMENT

static unsigned HandleHistogramProfileRand()
{
    // Generate a random number (xorshift32)
    //
    // Intentionally simple for faster random. It's stored in TLS to avoid
    // multithread contention.
    //
    static thread_local unsigned s_rng = 100;

    unsigned x = s_rng;
    x ^= x << 13;
    x ^= x >> 17;
    x ^= x << 5;
    s_rng = x;
    return x;
}

template<typename T>
FORCEINLINE static bool CheckSample(T* pIndex, size_t* sampleIndex)
{
    const unsigned S = ICorJitInfo::HandleHistogram32::SIZE;
    const unsigned N = ICorJitInfo::HandleHistogram32::SAMPLE_INTERVAL;
    static_assert_no_msg(N >= S);
    static_assert_no_msg((std::is_same<T, uint32_t>::value || std::is_same<T, uint64_t>::value));

    // If table is not yet full, just add entries in
    // and increment the table index.
    //
    T const index = *pIndex;

    if (index < S)
    {
        *sampleIndex = static_cast<size_t>(index);
        *pIndex = index + 1;
        return true;
    }

    unsigned const x = HandleHistogramProfileRand();

    // N is the sampling window size,
    // it should be larger than the table size.
    //
    // If we let N == count then we are building an entire
    // run sample -- probability of update decreases over time.
    // Would be a good strategy for an AOT profiler.
    //
    // But for TieredPGO we would prefer something that is more
    // weighted to recent observations.
    //
    // For S=4, N=128, we'll sample (on average) every 32nd call.
    //
    if ((x % N) >= S)
    {
        return false;
    }

    *sampleIndex = static_cast<size_t>(x % S);
    return true;
}

HCIMPL2(void, JIT_ValueProfile32, intptr_t val, ICorJitInfo::ValueHistogram32* valueProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    size_t sampleIndex;
    if (!CheckSample(&valueProfile->Count, &sampleIndex))
    {
        return;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(valueProfile);
    PgoManager::VerifyAddress(valueProfile + 1);
#endif

    valueProfile->ValueTable[sampleIndex] = val;
}
HCIMPLEND

HCIMPL2(void, JIT_ValueProfile64, intptr_t val, ICorJitInfo::ValueHistogram64* valueProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    size_t sampleIndex;
    if (!CheckSample(&valueProfile->Count, &sampleIndex))
    {
        return;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(valueProfile);
    PgoManager::VerifyAddress(valueProfile + 1);
#endif

    valueProfile->ValueTable[sampleIndex] = val;
}
HCIMPLEND

HCIMPL2(void, JIT_ClassProfile32, Object *obj, ICorJitInfo::HandleHistogram32* classProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t sampleIndex;
    if (!CheckSample(&classProfile->Count, &sampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodTable* pMT = objRef->GetMethodTable();

    // If the object class is collectible, record an unknown typehandle.
    // We do this instead of recording NULL so that we won't over-estimate
    // the likelihood of known type handles.
    //
    if (pMT->Collectible())
    {
        pMT = (MethodTable*)DEFAULT_UNKNOWN_HANDLE;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(classProfile);
    PgoManager::VerifyAddress(classProfile + 1);
#endif

    classProfile->HandleTable[sampleIndex] = (CORINFO_CLASS_HANDLE)pMT;
}
HCIMPLEND

// Version of helper above used when the count is 64-bit
HCIMPL2(void, JIT_ClassProfile64, Object *obj, ICorJitInfo::HandleHistogram64* classProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t sampleIndex;
    if (!CheckSample(&classProfile->Count, &sampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodTable* pMT = objRef->GetMethodTable();

    if (pMT->Collectible())
    {
        pMT = (MethodTable*)DEFAULT_UNKNOWN_HANDLE;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(classProfile);
    PgoManager::VerifyAddress(classProfile + 1);
#endif

    classProfile->HandleTable[sampleIndex] = (CORINFO_CLASS_HANDLE)pMT;
}
HCIMPLEND

HCIMPL2(void, JIT_DelegateProfile32, Object *obj, ICorJitInfo::HandleHistogram32* methodProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t methodSampleIndex;
    if (!CheckSample(&methodProfile->Count, &methodSampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodTable* pMT = objRef->GetMethodTable();

    _ASSERTE(pMT->IsDelegate());

    // Resolve method. We handle only the common "direct" delegate as that is
    // in any case the only one we can reasonably do GDV for. For instance,
    // open delegates are filtered out here, and many cases with inner
    // "complicated" logic as well (e.g. static functions, multicast, unmanaged
    // functions).
    //
    MethodDesc* pRecordedMD = (MethodDesc*)DEFAULT_UNKNOWN_HANDLE;
    DELEGATEREF del = (DELEGATEREF)objRef;
    if ((del->GetInvocationCount() == 0) && (del->GetMethodPtrAux() == (PCODE)NULL))
    {
        MethodDesc* pMD = NonVirtualEntry2MethodDesc(del->GetMethodPtr());
        if ((pMD != nullptr) && !pMD->GetLoaderAllocator()->IsCollectible() && !pMD->IsDynamicMethod())
        {
            pRecordedMD = pMD;
        }
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(methodProfile);
    PgoManager::VerifyAddress(methodProfile + 1);
#endif

    // If table is not yet full, just add entries in.
    //
    methodProfile->HandleTable[methodSampleIndex] = (CORINFO_METHOD_HANDLE)pRecordedMD;
}
HCIMPLEND

// Version of helper above used when the count is 64-bit
HCIMPL2(void, JIT_DelegateProfile64, Object *obj, ICorJitInfo::HandleHistogram64* methodProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t methodSampleIndex;
    if (!CheckSample(&methodProfile->Count, &methodSampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodTable* pMT = objRef->GetMethodTable();

    _ASSERTE(pMT->IsDelegate());

    // Resolve method. We handle only the common "direct" delegate as that is
    // in any case the only one we can reasonably do GDV for. For instance,
    // open delegates are filtered out here, and many cases with inner
    // "complicated" logic as well (e.g. static functions, multicast, unmanaged
    // functions).
    //
    MethodDesc* pRecordedMD = (MethodDesc*)DEFAULT_UNKNOWN_HANDLE;
    DELEGATEREF del = (DELEGATEREF)objRef;
    if ((del->GetInvocationCount() == 0) && (del->GetMethodPtrAux() == (PCODE)NULL))
    {
        MethodDesc* pMD = NonVirtualEntry2MethodDesc(del->GetMethodPtr());
        if ((pMD != nullptr) && !pMD->GetLoaderAllocator()->IsCollectible() && !pMD->IsDynamicMethod())
        {
            pRecordedMD = pMD;
        }
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(methodProfile);
    PgoManager::VerifyAddress(methodProfile + 1);
#endif

    // If table is not yet full, just add entries in.
    //
    methodProfile->HandleTable[methodSampleIndex] = (CORINFO_METHOD_HANDLE)pRecordedMD;
}
HCIMPLEND

HCIMPL3(void, JIT_VTableProfile32, Object* obj, CORINFO_METHOD_HANDLE baseMethod, ICorJitInfo::HandleHistogram32* methodProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t methodSampleIndex;
    if (!CheckSample(&methodProfile->Count, &methodSampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodDesc* pBaseMD = GetMethod(baseMethod);

    // Method better be virtual
    _ASSERTE(pBaseMD->IsVirtual());

    // We do not expect to see interface methods here as we cannot efficiently
    // use method handle information for these anyway.
    _ASSERTE(!pBaseMD->IsInterface());

    // Shouldn't be doing this for instantiated methods as they live elsewhere
    _ASSERTE(!pBaseMD->HasMethodInstantiation());

    MethodTable* pMT = objRef->GetMethodTable();

    // Resolve method
    WORD slot = pBaseMD->GetSlot();
    _ASSERTE(slot < pBaseMD->GetMethodTable()->GetNumVirtuals());

    MethodDesc* pMD = pMT->GetMethodDescForSlot_NoThrow(slot);

    MethodDesc* pRecordedMD = (MethodDesc*)DEFAULT_UNKNOWN_HANDLE;
    if (!pMD->GetLoaderAllocator()->IsCollectible() && !pMD->IsDynamicMethod())
    {
        pRecordedMD = pMD;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(methodProfile);
    PgoManager::VerifyAddress(methodProfile + 1);
#endif

    methodProfile->HandleTable[methodSampleIndex] = (CORINFO_METHOD_HANDLE)pRecordedMD;
}
HCIMPLEND

HCIMPL3(void, JIT_VTableProfile64, Object* obj, CORINFO_METHOD_HANDLE baseMethod, ICorJitInfo::HandleHistogram64* methodProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t methodSampleIndex;
    if (!CheckSample(&methodProfile->Count, &methodSampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodDesc* pBaseMD = GetMethod(baseMethod);

    // Method better be virtual
    _ASSERTE(pBaseMD->IsVirtual());

    // We do not expect to see interface methods here as we cannot efficiently
    // use method handle information for these anyway.
    _ASSERTE(!pBaseMD->IsInterface());

    // Shouldn't be doing this for instantiated methods as they live elsewhere
    _ASSERTE(!pBaseMD->HasMethodInstantiation());

    MethodTable* pMT = objRef->GetMethodTable();

    // Resolve method
    WORD slot = pBaseMD->GetSlot();
    _ASSERTE(slot < pBaseMD->GetMethodTable()->GetNumVirtuals());

    MethodDesc* pMD = pMT->GetMethodDescForSlot_NoThrow(slot);

    MethodDesc* pRecordedMD = (MethodDesc*)DEFAULT_UNKNOWN_HANDLE;
    if (!pMD->GetLoaderAllocator()->IsCollectible() && !pMD->IsDynamicMethod())
    {
        pRecordedMD = pMD;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(methodProfile);
    PgoManager::VerifyAddress(methodProfile + 1);
#endif

    methodProfile->HandleTable[methodSampleIndex] = (CORINFO_METHOD_HANDLE)pRecordedMD;
}
HCIMPLEND

// Helpers for scalable approximate counters
//
// Here threshold = 13 means we count accurately up to 2^13 = 8192 and
// then start counting probabilistically.
//
// See docs/design/features/ScalableApproximateCounting.md
//
HCIMPL1(void, JIT_CountProfile32, volatile LONG* pCounter)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    LONG count = *pCounter;
    LONG delta = 1;
    DWORD threshold = g_pConfig->TieredPGO_ScalableCountThreshold();

    if (count >= 1 << threshold)
    {
        DWORD logCount;
        BitScanReverse(&logCount, count);

        delta = 1 << (logCount - (threshold - 1));
        const unsigned rand = HandleHistogramProfileRand();
        const bool update = (rand & (delta - 1)) == 0;
        if (!update)
        {
            return;
        }
    }

    InterlockedAdd(pCounter, delta);
}
HCIMPLEND

HCIMPL1(void, JIT_CountProfile64, volatile LONG64* pCounter)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    LONG64 count = *pCounter;
    LONG64 delta = 1;
    DWORD threshold = g_pConfig->TieredPGO_ScalableCountThreshold();

    if (count >= 1 << threshold)
    {
        DWORD logCount;
        BitScanReverse64(&logCount, count);

        delta = 1LL << (logCount - (threshold - 1));
        const unsigned rand = HandleHistogramProfileRand();
        const bool update = (rand & (delta - 1)) == 0;
        if (!update)
        {
            return;
        }
    }

    InterlockedAdd64(pCounter, delta);
}
HCIMPLEND

//========================================================================
//
//      INTEROP HELPERS
//
//========================================================================

#ifdef HOST_64BIT

/**********************************************************************/
/* Fills out portions of an InlinedCallFrame for JIT64    */
/* The idea here is to allocate and initialize the frame to only once, */
/* regardless of how many PInvokes there are in the method            */
Thread * JIT_InitPInvokeFrame(InlinedCallFrame *pFrame)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    Thread *pThread = GetThread();

    // The JIT messed up and is initializing a frame that is already live on the stack?!?!?!?!
    _ASSERTE(pFrame != pThread->GetFrame());

    pFrame->Init();
    pFrame->m_Next = pThread->GetFrame();

    return pThread;
}

#endif // HOST_64BIT

EXTERN_C void JIT_PInvokeBegin(InlinedCallFrame* pFrame);
EXTERN_C void JIT_PInvokeEnd(InlinedCallFrame* pFrame);

// Forward declaration
EXTERN_C void STDCALL ReversePInvokeBadTransition();

#ifndef FEATURE_EH_FUNCLETS
EXCEPTION_HANDLER_DECL(FastNExportExceptHandler);
#endif

// This is a slower version of the reverse PInvoke enter function.
NOINLINE static void JIT_ReversePInvokeEnterRare(ReversePInvokeFrame* frame, void* returnAddr, UMEntryThunk* pThunk = NULL)
{
    _ASSERTE(frame != NULL);

    Thread* thread = GetThreadNULLOk();
    if (thread == NULL)
        CREATETHREAD_IF_NULL_FAILFAST(thread, W("Failed to setup new thread during reverse P/Invoke"));

    // Verify the current thread isn't in COOP mode.
    if (thread->PreemptiveGCDisabled())
        ReversePInvokeBadTransition();

    frame->currentThread = thread;

#ifdef PROFILING_SUPPORTED
        if (CORProfilerTrackTransitions())
        {
            ProfilerUnmanagedToManagedTransitionMD(frame->pMD, COR_PRF_TRANSITION_CALL);
        }
#endif

    thread->DisablePreemptiveGC();
#ifdef DEBUGGING_SUPPORTED
    // If the debugger is attached, we use this opportunity to see if
    // we're disabling preemptive GC on the way into the runtime from
    // unmanaged code. We end up here because
    // Increment/DecrementTraceCallCount() will bump
    // g_TrapReturningThreads for us.
    if (CORDebuggerTraceCall())
        g_pDebugInterface->TraceCall(pThunk ? (const BYTE*)pThunk->GetManagedTarget() : (const BYTE*)returnAddr);
#endif // DEBUGGING_SUPPORTED
}

NOINLINE static void JIT_ReversePInvokeEnterRare2(ReversePInvokeFrame* frame, void* returnAddr, UMEntryThunk* pThunk = NULL)
{
    frame->currentThread->RareDisablePreemptiveGC();
#ifdef DEBUGGING_SUPPORTED
    // If the debugger is attached, we use this opportunity to see if
    // we're disabling preemptive GC on the way into the runtime from
    // unmanaged code. We end up here because
    // Increment/DecrementTraceCallCount() will bump
    // g_TrapReturningThreads for us.
    if (CORDebuggerTraceCall())
        g_pDebugInterface->TraceCall(pThunk ? (const BYTE*)pThunk->GetManagedTarget() : (const BYTE*)returnAddr);
#endif // DEBUGGING_SUPPORTED
}

// The following JIT_ReversePInvoke helpers are special.
// They handle setting up Reverse P/Invoke calls and transitioning back to unmanaged code.
// As a result, we may not have a thread in JIT_ReversePInvokeEnter and we will be in the wrong GC mode for the HCALL prolog.
// Additionally, we set up and tear down SEH handlers when we're on x86, so we can't use dynamic contracts anyway.
// As a result, we specially decorate this method to have the correct calling convention
// and argument ordering for an HCALL, but we don't use the HCALL macros and contracts
// since this method doesn't follow the contracts.
HCIMPL3_RAW(void, JIT_ReversePInvokeEnterTrackTransitions, ReversePInvokeFrame* frame, CORINFO_METHOD_HANDLE handle, void* secretArg)
{
    _ASSERTE(frame != NULL && handle != NULL);

    MethodDesc* pMD = GetMethod(handle);
    if (pMD->IsILStub() && secretArg != NULL)
    {
        pMD = ((UMEntryThunk*)secretArg)->GetMethod();
    }
    frame->pMD = pMD;

    Thread* thread = GetThreadNULLOk();

    // If a thread instance exists and is in the
    // correct GC mode attempt a quick transition.
    if (thread != NULL
        && !thread->PreemptiveGCDisabled())
    {
        frame->currentThread = thread;

#ifdef PROFILING_SUPPORTED
        if (CORProfilerTrackTransitions())
        {
            ProfilerUnmanagedToManagedTransitionMD(frame->pMD, COR_PRF_TRANSITION_CALL);
        }
#endif

        // Manually inline the fast path in Thread::DisablePreemptiveGC().
        thread->m_fPreemptiveGCDisabled.StoreWithoutBarrier(1);
        if (g_TrapReturningThreads != 0)
        {
            // If we're in an IL stub, we want to trace the address of the target method,
            // not the next instruction in the stub.
            JIT_ReversePInvokeEnterRare2(frame, _ReturnAddress(), GetMethod(handle)->IsILStub() ? (UMEntryThunk*)secretArg : (UMEntryThunk*)NULL);
        }
    }
    else
    {
        // If we're in an IL stub, we want to trace the address of the target method,
        // not the next instruction in the stub.
        JIT_ReversePInvokeEnterRare(frame, _ReturnAddress(), GetMethod(handle)->IsILStub() ? (UMEntryThunk*)secretArg  : (UMEntryThunk*)NULL);
    }

#ifndef FEATURE_EH_FUNCLETS
    frame->record.m_pEntryFrame = frame->currentThread->GetFrame();
    frame->record.m_ExReg.Handler = (PEXCEPTION_ROUTINE)FastNExportExceptHandler;
    INSTALL_EXCEPTION_HANDLING_RECORD(&frame->record.m_ExReg);
#endif
}
HCIMPLEND_RAW

HCIMPL1_RAW(void, JIT_ReversePInvokeEnter, ReversePInvokeFrame* frame)
{
    _ASSERTE(frame != NULL);

    Thread* thread = GetThreadNULLOk();

    // If a thread instance exists and is in the
    // correct GC mode attempt a quick transition.
    if (thread != NULL
        && !thread->PreemptiveGCDisabled())
    {
        frame->currentThread = thread;

        // Manually inline the fast path in Thread::DisablePreemptiveGC().
        thread->m_fPreemptiveGCDisabled.StoreWithoutBarrier(1);
        if (g_TrapReturningThreads != 0)
        {
            JIT_ReversePInvokeEnterRare2(frame, _ReturnAddress());
        }
    }
    else
    {
        JIT_ReversePInvokeEnterRare(frame, _ReturnAddress());
    }

#ifndef FEATURE_EH_FUNCLETS
    frame->record.m_pEntryFrame = frame->currentThread->GetFrame();
    frame->record.m_ExReg.Handler = (PEXCEPTION_ROUTINE)FastNExportExceptHandler;
    INSTALL_EXCEPTION_HANDLING_RECORD(&frame->record.m_ExReg);
#endif
}
HCIMPLEND_RAW

HCIMPL1_RAW(void, JIT_ReversePInvokeExitTrackTransitions, ReversePInvokeFrame* frame)
{
    _ASSERTE(frame != NULL);
    _ASSERTE(frame->currentThread == GetThread());

    // Manually inline the fast path in Thread::EnablePreemptiveGC().
    // This is a trade off with GC suspend performance. We are opting
    // to make this exit faster.
    frame->currentThread->m_fPreemptiveGCDisabled.StoreWithoutBarrier(0);

#ifndef FEATURE_EH_FUNCLETS
    UNINSTALL_EXCEPTION_HANDLING_RECORD(&frame->record.m_ExReg);
#endif

#ifdef PROFILING_SUPPORTED
    if (CORProfilerTrackTransitions())
    {
        ProfilerManagedToUnmanagedTransitionMD(frame->pMD, COR_PRF_TRANSITION_RETURN);
    }
#endif
}
HCIMPLEND_RAW

HCIMPL1_RAW(void, JIT_ReversePInvokeExit, ReversePInvokeFrame* frame)
{
    _ASSERTE(frame != NULL);
    _ASSERTE(frame->currentThread == GetThread());

    // Manually inline the fast path in Thread::EnablePreemptiveGC().
    // This is a trade off with GC suspend performance. We are opting
    // to make this exit faster.
    frame->currentThread->m_fPreemptiveGCDisabled.StoreWithoutBarrier(0);

#ifndef FEATURE_EH_FUNCLETS
    UNINSTALL_EXCEPTION_HANDLING_RECORD(&frame->record.m_ExReg);
#endif
}
HCIMPLEND_RAW

// These two do take args but have a custom calling convention.
EXTERN_C void JIT_ValidateIndirectCall();
EXTERN_C void JIT_DispatchIndirectCall();

//========================================================================
//
//      JIT HELPERS INITIALIZATION
//
//========================================================================

// verify consistency of jithelpers.h and corinfo.h
enum __CorInfoHelpFunc {
#define JITHELPER(code, pfnHelper, sig) __##code,
#include "jithelpers.h"
};
#define JITHELPER(code, pfnHelper, sig) C_ASSERT((int)__##code == (int)code);
#include "jithelpers.h"

#ifdef _DEBUG
#define HELPERDEF(code, lpv, sig) { (LPVOID)(lpv), #code },
#else // !_DEBUG
#define HELPERDEF(code, lpv, sig) { (LPVOID)(lpv) },
#endif // !_DEBUG

// static helpers - constant array
const VMHELPDEF hlpFuncTable[CORINFO_HELP_COUNT] =
{
#define JITHELPER(code, pfnHelper, binderId) HELPERDEF(code, pfnHelper, binderId)
#define DYNAMICJITHELPER(code, pfnHelper, binderId) HELPERDEF(code, 1 + DYNAMIC_##code, binderId)
#include "jithelpers.h"
};

// dynamic helpers - filled in at runtime - See definition of DynamicCorInfoHelpFunc.
VMHELPDEF hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_COUNT] =
{
#define JITHELPER(code, pfnHelper, binderId)
#define DYNAMICJITHELPER(code, pfnHelper, binderId) HELPERDEF(DYNAMIC_ ## code, pfnHelper, binderId)
#include "jithelpers.h"
};

// dynamic helpers to Binder ID mapping - See definition of DynamicCorInfoHelpFunc.
static const BinderMethodID hlpDynamicToBinderMap[DYNAMIC_CORINFO_HELP_COUNT] =
{
#define JITHELPER(code, pfnHelper, binderId)
#define DYNAMICJITHELPER(code, pfnHelper, binderId) (pfnHelper != NULL) ? (BinderMethodID)METHOD__NIL : (BinderMethodID)binderId, // If pre-compiled code is provided for a jit helper, prefer that over the IL implementation
#include "jithelpers.h"
};

// Set the JIT helper function in the helper table
// Handles the case where the function does not reside in mscorwks.dll

void _SetJitHelperFunction(DynamicCorInfoHelpFunc ftnNum, void * pFunc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(ftnNum < DYNAMIC_CORINFO_HELP_COUNT);

    LOG((LF_JIT, LL_INFO1000000, "Setting JIT dynamic helper %3d (%s) to %p\n",
        ftnNum, hlpDynamicFuncTable[ftnNum].name, pFunc));

    hlpDynamicFuncTable[ftnNum].pfnHelper = (void*)pFunc;
}

VMHELPDEF LoadDynamicJitHelper(DynamicCorInfoHelpFunc ftnNum, MethodDesc** methodDesc)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(ftnNum < DYNAMIC_CORINFO_HELP_COUNT);

    MethodDesc* pMD = NULL;
    void* helper = VolatileLoad(&hlpDynamicFuncTable[ftnNum].pfnHelper);
    if (helper == NULL)
    {
        BinderMethodID binderId = hlpDynamicToBinderMap[ftnNum];

        LOG((LF_JIT, LL_INFO1000000, "Loading JIT dynamic helper %3d (%s) to binderID %u\n",
            ftnNum, hlpDynamicFuncTable[ftnNum].name, binderId));

        if (binderId == METHOD__NIL)
            return {};

        pMD = CoreLibBinder::GetMethod(binderId);
        PCODE pFunc = pMD->GetMultiCallableAddrOfCode();
        InterlockedCompareExchangeT<void*>(&hlpDynamicFuncTable[ftnNum].pfnHelper, (void*)pFunc, nullptr);
    }

    // If the caller wants the MethodDesc, we may need to try and load it.
    if (methodDesc != NULL)
    {
        if (pMD == NULL)
        {
            BinderMethodID binderId = hlpDynamicToBinderMap[ftnNum];
            pMD = binderId != METHOD__NIL
                ? CoreLibBinder::GetMethod(binderId)
                : NULL;
        }
        *methodDesc = pMD;
    }

    return hlpDynamicFuncTable[ftnNum];
}

bool HasILBasedDynamicJitHelper(DynamicCorInfoHelpFunc ftnNum)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(ftnNum < DYNAMIC_CORINFO_HELP_COUNT);

    return (METHOD__NIL != hlpDynamicToBinderMap[ftnNum]);
}

bool IndirectionAllowedForJitHelper(CorInfoHelpFunc ftnNum)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(ftnNum < CORINFO_HELP_COUNT);

    if (
#define DYNAMICJITHELPER(code,fn,binderId)
#define JITHELPER(code,fn,binderId)
#define DYNAMICJITHELPER_NOINDIRECT(code,fn,binderId) (code == ftnNum) ||
#include "jithelpers.h"
        false)
    {
        return false;
    }
    
    return true;
}

/*********************************************************************/
// Initialize the part of the JIT helpers that require much of the
// EE infrastructure to be in place.
/*********************************************************************/
void InitJITHelpers2()
{
    STANDARD_VM_CONTRACT;

#if defined(TARGET_X86) || defined(TARGET_ARM)
    SetJitHelperFunction(CORINFO_HELP_INIT_PINVOKE_FRAME, (void *)GenerateInitPInvokeFrameHelper()->GetEntryPoint());
#endif // TARGET_X86 || TARGET_ARM
}
