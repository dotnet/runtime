// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ****************************************************************************
// File: funceval.cpp
// 

//
// funceval.cpp - Debugger func-eval routines.
//
// ****************************************************************************
// Putting code & #includes, #defines, etc, before the stdafx.h will
// cause the code,etc, to be silently ignored


#include "stdafx.h"
#include "debugdebugger.h"
#include "../inc/common.h"
#include "perflog.h"
#include "eeconfig.h" // This is here even for retail & free builds...
#include "../../dlls/mscorrc/resource.h"

#include "vars.hpp"
#include "threads.h"
#include "appdomain.inl"
#include <limits.h>
#include "ilformatter.h"

#ifndef DACCESS_COMPILE

//
// This is the main file for processing func-evals.  Nestle in
// with a cup o' tea and read on.
//
// The most common case is handled in GCProtectArgsAndDoNormalFuncEval(), which follows
// all the comments below.  The two other corner cases are handled in
// FuncEvalHijackWorker(), and are extremely straight-forward.
//
// There are several steps to successfully processing a func-eval.  At a
// very high level, the first step is to gather all the information necessary
// to make the call (specifically, gather arg info and method info); the second
// step is to actually make the call to managed code;  finally, the third step
// is to take all results and unpackage them.
//
// The first step (gathering arg and method info) has several critical passes that
// must be made.
//     a) Protect all passed in args from a GC.
//     b) Transition into the appropriate AppDomain if necessary
//     c) Pre-allocate object for 'new' calls and, if necessary, box the 'this' argument. (May cause a GC)
//     d) Gather method info (May cause GC)
//     e) Gather info from runtime about args. (May cause a GC)
//     f) Box args that need to be, GC-protecting the newly boxed items. (May cause a GC)
//     g) Pre-allocate object for return values. (May cause a GC)
//     h) Copy to pBufferForArgsArray all the args.  This array is used to hold values that
//          may need writable memory for ByRef args.
//     i) Create and load pArgumentArray to be passed as the stack for the managed call.
//       NOTE: From the time we load the first argument into the stack we cannot cause a GC
//       as the argument array cannot be GC-protected.
//
// The second step (Making the managed call), is relatively easy, and is a single call.
//
// The third step (unpacking all results), has a couple of passes as well.
//     a) Copy back all resulting values.
//     b) Free all temporary work memory.
//
//
// The most difficult part of doing a func-eval is the first step, since once you
// have everything set up, unpacking and calling are reverse, gc-safe, operations.  Thus,
// elaboration is needed on the first step.
//
// a) Protect all passed in args from a GC.  This must be done in a gc-forbid region,
// and the code path to this function must not trigger a gc either.  In this function five 
// parallel arrays are used:  pObjectRefArray, pMaybeInteriorPtrArray, pByRefMaybeInteriorPtrArray,
// pBufferForArgsArray, and pArguments.
//   pObjectRefArray is used to gc-protect all arguments and results that are objects.
//   pMaybeInteriorPtrArray is used to gc-protect all arguments that might be pointers
//     to an interior of a managed object.
//   pByRefMaybeInteriorPtrArray is similar to pMaybeInteriorPtrArray, except that it protects the
//     address of the arguments instead of the arguments themselves.  This is needed because we may have
//     by ref arguments whose address is an interior pointer into the GC heap.
//   pBufferForArgsArray is used strictly as a buffer for copying primitives
//     that need to be passed as ByRef, or may be enregistered.  This array also holds
//     handles.
// These first two arrays are mutually exclusive, that is, if there is an entry
// in one array at index i, there should be no entry in either of the other arrays at
// the same index.
//   pArguments is used as the complete array of arguments to pass to the managed function.
//
// Unfortunately the necessary information to complete pass (a) perfectly may cause a gc, so
// instead, pass (a) is over-aggressive and protects the following: All object refs into
// pObjectRefArray, and puts all values that could be raw pointers into pMaybeInteriorPtrArray.
//
// b) Discovers the method to be called, and if it is a 'new' allocate an object for the result.
//
// c) Gather information about the method that will be called.
//
// d) Here we gather information from the method signature which tells which args are
// ByRef and various other flags.  We will use this information in later passes.
//
// e) Using the information in pass (c), for each argument: box arguments, placing newly
// boxed items into pObjectRefArray immediately after creating them.
//
// f) Pre-allocate any object for a returned value.
//
// g) Using the information is pass (c), all arguments are copied into a scratch buffer before
// invoking the managed function.
//
// h) pArguments is loaded from the pre-allocated return object, the individual elements
// of the other 3 arrays, and from any non-ByRef literals.  This is the complete stack
// to be passed to the managed function.  For performance increase, it can remove any
// overly aggressive items that were placed in pMaybeInteriorPtrArray.
//

//
// IsElementTypeSpecial()
//
// This is a simple function used to check if a CorElementType needs special handling for func eval.
//
// parameters:   type - the CorElementType which we need to check
//
// return value: true if the specified type needs special handling
//
inline static bool IsElementTypeSpecial(CorElementType type)
{
    LIMITED_METHOD_CONTRACT;

    return ((type == ELEMENT_TYPE_CLASS)   ||
            (type == ELEMENT_TYPE_OBJECT)  ||
            (type == ELEMENT_TYPE_ARRAY)   ||
            (type == ELEMENT_TYPE_SZARRAY) ||
            (type == ELEMENT_TYPE_STRING));
}

//
// GetAndSetLiteralValue()
//
// This helper function extracts the value out of the source pointer while taking into account alignment and size.
// Then it stores the value into the destination pointer, again taking into account alignment and size.
//
// parameters:   pDst    - destination pointer
//               dstType - the CorElementType of the destination value
//               pSrc    - source pointer
//               srcType - the CorElementType of the source value
//
// return value: none
//
inline static void GetAndSetLiteralValue(LPVOID pDst, CorElementType dstType, LPVOID pSrc, CorElementType srcType)
{
    LIMITED_METHOD_CONTRACT;

    UINT64 srcValue;

    // Retrieve the value using the source CorElementType.
    switch (g_pEEInterface->GetSizeForCorElementType(srcType))
    {
    case 1:
        srcValue = (UINT64)*((BYTE*)pSrc);
        break;
    case 2:
        srcValue = (UINT64)*((USHORT*)pSrc);
        break;
    case 4:
        srcValue = (UINT64)*((UINT32*)pSrc);
        break;
    case 8:
        srcValue = (UINT64)*((UINT64*)pSrc);
        break;

    default:
        UNREACHABLE();
    }

    // Cast to the appropriate type using the destination CorElementType.
    switch (dstType)
    {
    case ELEMENT_TYPE_BOOLEAN:
        *(BYTE*)pDst = (BYTE)!!srcValue;
        break;
    case ELEMENT_TYPE_I1:
        *(INT8*)pDst = (INT8)srcValue;
        break;
    case ELEMENT_TYPE_U1:
        *(UINT8*)pDst = (UINT8)srcValue;
        break;
    case ELEMENT_TYPE_I2:
        *(INT16*)pDst = (INT16)srcValue;
        break;
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
        *(UINT16*)pDst = (UINT16)srcValue;
        break;
#if !defined(_WIN64)
    case ELEMENT_TYPE_I:
#endif
    case ELEMENT_TYPE_I4:
        *(int*)pDst = (int)srcValue;
        break;
#if !defined(_WIN64)
    case ELEMENT_TYPE_U:
#endif
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_R4:
        *(unsigned*)pDst = (unsigned)srcValue;
        break;
#if defined(_WIN64)
    case ELEMENT_TYPE_I:
#endif
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_R8:
        *(INT64*)pDst = (INT64)srcValue;
        break;

#if defined(_WIN64)
    case ELEMENT_TYPE_U:
#endif
    case ELEMENT_TYPE_U8:
        *(UINT64*)pDst = (UINT64)srcValue;
        break;
    case ELEMENT_TYPE_FNPTR:
    case ELEMENT_TYPE_PTR:
        *(void **)pDst = (void *)(SIZE_T)srcValue;
        break;

    default:
        UNREACHABLE();
    }

}


//
// Throw on not supported func evals
//
static void ValidateFuncEvalReturnType(DebuggerIPCE_FuncEvalType evalType, MethodTable * pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;
    
    if (pMT == g_pStringClass) 
    {
        if (evalType == DB_IPCE_FET_NEW_OBJECT || evalType == DB_IPCE_FET_NEW_OBJECT_NC) 
        {
            // Cannot call New object on String constructor.
            COMPlusThrow(kArgumentException,W("Argument_CannotCreateString"));
        }
    }
    else if (g_pEEInterface->IsTypedReference(pMT)) 
    {
        // Cannot create typed references through funceval.
        if (evalType == DB_IPCE_FET_NEW_OBJECT || evalType == DB_IPCE_FET_NEW_OBJECT_NC || evalType == DB_IPCE_FET_NORMAL)
        {
            COMPlusThrow(kArgumentException, W("Argument_CannotCreateTypedReference"));
        }
    }
}

//
// Given a register, return the value.
//
static SIZE_T GetRegisterValue(DebuggerEval *pDE, CorDebugRegister reg, void *regAddr, SIZE_T regValue)
{
    LIMITED_METHOD_CONTRACT;

    SIZE_T ret = 0;

    // Check whether the register address is the marker value for a register in a non-leaf frame.  
    // This is related to the funceval breaking change.
    // 
    if (regAddr == CORDB_ADDRESS_TO_PTR(kNonLeafFrameRegAddr))
    {
        ret = regValue;
    }
    else
    {
        switch (reg)
        {
        case REGISTER_STACK_POINTER:
            ret = (SIZE_T)GetSP(&pDE->m_context);
            break;

        case REGISTER_FRAME_POINTER:
            ret = (SIZE_T)GetFP(&pDE->m_context);
            break;

#if defined(_TARGET_X86_)
        case REGISTER_X86_EAX:
            ret = pDE->m_context.Eax;
            break;

        case REGISTER_X86_ECX:
            ret = pDE->m_context.Ecx;
            break;

        case REGISTER_X86_EDX:
            ret = pDE->m_context.Edx;
            break;

        case REGISTER_X86_EBX:
            ret = pDE->m_context.Ebx;
            break;

        case REGISTER_X86_ESI:
            ret = pDE->m_context.Esi;
            break;

        case REGISTER_X86_EDI:
            ret = pDE->m_context.Edi;
            break;

#elif defined(_TARGET_AMD64_)
        case REGISTER_AMD64_RAX:
            ret = pDE->m_context.Rax;
            break;

        case REGISTER_AMD64_RCX:
            ret = pDE->m_context.Rcx;
            break;

        case REGISTER_AMD64_RDX:
            ret = pDE->m_context.Rdx;
            break;

        case REGISTER_AMD64_RBX:
            ret = pDE->m_context.Rbx;
            break;

        case REGISTER_AMD64_RSI:
            ret = pDE->m_context.Rsi;
            break;

        case REGISTER_AMD64_RDI:
            ret = pDE->m_context.Rdi;
            break;

        case REGISTER_AMD64_R8:
            ret = pDE->m_context.R8;
            break;

        case REGISTER_AMD64_R9:
            ret = pDE->m_context.R9;
            break;

        case REGISTER_AMD64_R10:
            ret = pDE->m_context.R10;
            break;

        case REGISTER_AMD64_R11:
            ret = pDE->m_context.R11;
            break;

        case REGISTER_AMD64_R12:
            ret = pDE->m_context.R12;
            break;

        case REGISTER_AMD64_R13:
            ret = pDE->m_context.R13;
            break;

        case REGISTER_AMD64_R14:
            ret = pDE->m_context.R14;
            break;

        case REGISTER_AMD64_R15:
            ret = pDE->m_context.R15;
            break;

        // fall through
        case REGISTER_AMD64_XMM0:
        case REGISTER_AMD64_XMM1:
        case REGISTER_AMD64_XMM2:
        case REGISTER_AMD64_XMM3:
        case REGISTER_AMD64_XMM4:
        case REGISTER_AMD64_XMM5:
        case REGISTER_AMD64_XMM6:
        case REGISTER_AMD64_XMM7:
        case REGISTER_AMD64_XMM8:
        case REGISTER_AMD64_XMM9:
        case REGISTER_AMD64_XMM10:
        case REGISTER_AMD64_XMM11:
        case REGISTER_AMD64_XMM12:
        case REGISTER_AMD64_XMM13:
        case REGISTER_AMD64_XMM14:
        case REGISTER_AMD64_XMM15:
            ret = FPSpillToR8(&(pDE->m_context.Xmm0) + (reg - REGISTER_AMD64_XMM0));
            break;

#endif // !_TARGET_X86_ && !_TARGET_AMD64_
        default:
            _ASSERT(!"Invalid register number!");

        }
    }

    return ret;
}

//
// Given a register, set its value.
//
static void SetRegisterValue(DebuggerEval *pDE, CorDebugRegister reg, void *regAddr, SIZE_T newValue)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    // Check whether the register address is the marker value for a register in a non-leaf frame.  
    // If so, then we can't update the register.  Throw an exception to communicate this error.
    if (regAddr == CORDB_ADDRESS_TO_PTR(kNonLeafFrameRegAddr))
    {
        COMPlusThrowHR(CORDBG_E_FUNC_EVAL_CANNOT_UPDATE_REGISTER_IN_NONLEAF_FRAME);
        return;
    }
    else
    {
        switch (reg)
        {
        case REGISTER_STACK_POINTER:
            SetSP(&pDE->m_context, newValue);
            break;

        case REGISTER_FRAME_POINTER:
            SetFP(&pDE->m_context, newValue);
            break;

#ifdef _TARGET_X86_
        case REGISTER_X86_EAX:
            pDE->m_context.Eax = newValue;
            break;

        case REGISTER_X86_ECX:
            pDE->m_context.Ecx = newValue;
            break;

        case REGISTER_X86_EDX:
            pDE->m_context.Edx = newValue;
            break;

        case REGISTER_X86_EBX:
            pDE->m_context.Ebx = newValue;
            break;

        case REGISTER_X86_ESI:
            pDE->m_context.Esi = newValue;
            break;

        case REGISTER_X86_EDI:
            pDE->m_context.Edi = newValue;
            break;

#elif defined(_TARGET_AMD64_)
        case REGISTER_AMD64_RAX:
            pDE->m_context.Rax = newValue;
            break;

        case REGISTER_AMD64_RCX:
            pDE->m_context.Rcx = newValue;
            break;

        case REGISTER_AMD64_RDX:
            pDE->m_context.Rdx = newValue;
            break;

        case REGISTER_AMD64_RBX:
            pDE->m_context.Rbx = newValue;
            break;

        case REGISTER_AMD64_RSI:
            pDE->m_context.Rsi = newValue;
            break;

        case REGISTER_AMD64_RDI:
            pDE->m_context.Rdi = newValue;
            break;

        case REGISTER_AMD64_R8:
            pDE->m_context.R8= newValue;
            break;

        case REGISTER_AMD64_R9:
            pDE->m_context.R9= newValue;
            break;

        case REGISTER_AMD64_R10:
            pDE->m_context.R10= newValue;
            break;

        case REGISTER_AMD64_R11:
            pDE->m_context.R11 = newValue;
            break;

        case REGISTER_AMD64_R12:
            pDE->m_context.R12 = newValue;
            break;

        case REGISTER_AMD64_R13:
            pDE->m_context.R13 = newValue;
            break;

        case REGISTER_AMD64_R14:
            pDE->m_context.R14 = newValue;
            break;

        case REGISTER_AMD64_R15:
            pDE->m_context.R15 = newValue;
            break;

        // fall through
        case REGISTER_AMD64_XMM0:
        case REGISTER_AMD64_XMM1:
        case REGISTER_AMD64_XMM2:
        case REGISTER_AMD64_XMM3:
        case REGISTER_AMD64_XMM4:
        case REGISTER_AMD64_XMM5:
        case REGISTER_AMD64_XMM6:
        case REGISTER_AMD64_XMM7:
        case REGISTER_AMD64_XMM8:
        case REGISTER_AMD64_XMM9:
        case REGISTER_AMD64_XMM10:
        case REGISTER_AMD64_XMM11:
        case REGISTER_AMD64_XMM12:
        case REGISTER_AMD64_XMM13:
        case REGISTER_AMD64_XMM14:
        case REGISTER_AMD64_XMM15:
            R8ToFPSpill(&(pDE->m_context.Xmm0) + (reg - REGISTER_AMD64_XMM0), newValue);
            break;

#endif // !_TARGET_X86_ && !_TARGET_AMD64_
        default:
            _ASSERT(!"Invalid register number!");

        }
    }
}


/*
 * GetRegsiterValueAndReturnAddress
 *
 * This routine takes out a value from a register, or set of registers, into one of 
 * the given buffers (depending on size), and returns a pointer to the filled in
 * buffer, or NULL on error.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    pFEAD - Information about this particular argument.
 *    pInt64Buf - pointer to a buffer of type INT64
 *    pSizeTBuf - pointer to a buffer of native size type.
 *
 * Returns:
 *    pointer to the filled in buffer, else NULL on error.
 *
 */
static PVOID GetRegisterValueAndReturnAddress(DebuggerEval *pDE,
                                              DebuggerIPCE_FuncEvalArgData *pFEAD,
                                              INT64 *pInt64Buf,
                                              SIZE_T *pSizeTBuf
                                              )
{
    LIMITED_METHOD_CONTRACT;

    PVOID pAddr;

#if !defined(_WIN64)
    pAddr = pInt64Buf;
    DWORD *pLow = (DWORD*)(pInt64Buf);
    DWORD *pHigh  = pLow + 1;
#endif // _WIN64

    switch (pFEAD->argHome.kind)
    {
#if !defined(_WIN64)
    case RAK_REGREG:
        *pLow = GetRegisterValue(pDE, pFEAD->argHome.u.reg2, pFEAD->argHome.u.reg2Addr, pFEAD->argHome.u.reg2Value);
        *pHigh = GetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);
        break;

    case RAK_MEMREG:
        *pLow = GetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);
        *pHigh = *((DWORD*)CORDB_ADDRESS_TO_PTR(pFEAD->argHome.addr));
        break;

    case RAK_REGMEM:
        *pLow = *((DWORD*)CORDB_ADDRESS_TO_PTR(pFEAD->argHome.addr));
        *pHigh = GetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);
        break;
#endif // _WIN64

    case RAK_REG:
        // Simply grab the value out of the proper register.
        *pSizeTBuf = GetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);
        pAddr = pSizeTBuf;
        break;

    default:
        pAddr = NULL;
        break;
    }

    return pAddr;
}

//---------------------------------------------------------------------------------------
//
// Clean up any temporary value class variables we have allocated for the funceval.
//
// Arguments:
//    pStackStructArray - array whose elements track the location and type of the temporary variables
//

void CleanUpTemporaryVariables(ValueClassInfo ** ppProtectedValueClasses)
{
    while (*ppProtectedValueClasses != NULL)
    {
        ValueClassInfo * pValueClassInfo = *ppProtectedValueClasses;
        *ppProtectedValueClasses = pValueClassInfo->pNext;

        DeleteInteropSafe(reinterpret_cast<BYTE *>(pValueClassInfo));
    }
}


#ifdef _DEBUG

//
// Create a parallel array that tracks that we have initialized information in
// each array.
//
#define MAX_DATA_LOCATIONS_TRACKED 100

typedef DWORD DataLocation;

#define DL_NonExistent           0x00
#define DL_ObjectRefArray        0x01
#define DL_MaybeInteriorPtrArray 0x02
#define DL_BufferForArgsArray    0x04
#define DL_All                   0xFF

#endif // _DEBUG


/*
 * GetFuncEvalArgValue
 *
 * This routine is used to fill the pArgument array with the appropriate value.  This function
 * uses the three parallel array entries given, and places the correct value, or reference to
 * the value in pArgument.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    pFEAD - Information about this particular argument.
 *    isByRef - Is the argument being passed ByRef.
 *    fNeedBoxOrUnbox - Did the argument need boxing or unboxing.
 *    argTH - The type handle for the argument.
 *    byrefArgSigType - The signature type of a parameter that isByRef == true.
 *    pArgument - Location to place the reference or value.
 *    pMaybeInteriorPtrArg - A pointer that contains a value that may be pointers to
 *         the interior of a managed object.
 *    pObjectRefArg - A pointer that contains an object ref.  It was built previously.
 *    pBufferArg - A pointer for holding stuff that did not need to be protected.
 *
 * Returns:
 *    None.
 *
 */
static void GetFuncEvalArgValue(DebuggerEval *pDE,
                                DebuggerIPCE_FuncEvalArgData *pFEAD,
                                bool isByRef,
                                bool fNeedBoxOrUnbox,
                                TypeHandle argTH,
                                CorElementType byrefArgSigType,
                                TypeHandle byrefArgTH,
                                ARG_SLOT *pArgument,
                                void *pMaybeInteriorPtrArg,
                                OBJECTREF *pObjectRefArg,
                                INT64 *pBufferArg,
                                ValueClassInfo ** ppProtectedValueClasses,
                                CorElementType argSigType
                                DEBUG_ARG(DataLocation dataLocation)
                               )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE((dataLocation != DL_NonExistent) ||
             (pFEAD->argElementType == ELEMENT_TYPE_VALUETYPE));

    switch (pFEAD->argElementType)
    {
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R8:
        {
            INT64 *pSource;

#if defined(_WIN64)
            _ASSERTE(dataLocation & DL_MaybeInteriorPtrArray);

            pSource = (INT64 *)pMaybeInteriorPtrArg;
#else  // !_WIN64
            _ASSERTE(dataLocation & DL_BufferForArgsArray);

            pSource = pBufferArg;
#endif // !_WIN64

            if (!isByRef)
            {
                *((INT64*)pArgument) = *pSource;
            }
            else
            {
                *pArgument = PtrToArgSlot(pSource);
            }
        }
        break;

    case ELEMENT_TYPE_VALUETYPE:
        {
            SIZE_T v = 0;
            LPVOID pAddr = NULL;
            INT64 bigVal = 0;

            if (pFEAD->argAddr != NULL)
            {
                pAddr = *((void **)pMaybeInteriorPtrArg);
            }
            else
            {
                pAddr = GetRegisterValueAndReturnAddress(pDE, pFEAD, &bigVal, &v);

                if (pAddr == NULL)
                {
                    COMPlusThrow(kArgumentNullException);
                }
            }


            _ASSERTE(pAddr);

            if (!fNeedBoxOrUnbox && !isByRef)
            {
                _ASSERTE(argTH.GetMethodTable());

                unsigned size = argTH.GetMethodTable()->GetNumInstanceFieldBytes();
                if (size <= sizeof(ARG_SLOT)
#if defined(_TARGET_AMD64_)
                    // On AMD64 we pass value types of size which are not powers of 2 by ref.
                    && ((size & (size-1)) == 0)
#endif // _TARGET_AMD64_
                   )
                {
                    memcpyNoGCRefs(ArgSlotEndianessFixup(pArgument, sizeof(LPVOID)), pAddr, size);
                }
                else
                {
                    _ASSERTE(pFEAD->argAddr != NULL);
#if defined(ENREGISTERED_PARAMTYPE_MAXSIZE)
                    if (ArgIterator::IsArgPassedByRef(argTH))
                    {
                        // On X64, by-value value class arguments which are bigger than 8 bytes are passed by reference
                        // according to the native calling convention.  The same goes for value class arguments whose size
                        // is smaller than 8 bytes but not a power of 2.  To avoid side effets, we need to allocate a 
                        // temporary variable and pass that by reference instead. On ARM64, by-value value class 
                        // arguments which are bigger than 16 bytes are passed by reference.
                        _ASSERTE(ppProtectedValueClasses != NULL);

                        BYTE * pTemp = new (interopsafe) BYTE[ALIGN_UP(sizeof(ValueClassInfo), 8) + size];

                        ValueClassInfo * pValueClassInfo = (ValueClassInfo *)pTemp;
                        LPVOID pData = pTemp + ALIGN_UP(sizeof(ValueClassInfo), 8);

                        memcpyNoGCRefs(pData, pAddr, size);
                        *pArgument = PtrToArgSlot(pData);

                        pValueClassInfo->pData = pData;
                        pValueClassInfo->pMT = argTH.GetMethodTable();

                        pValueClassInfo->pNext = *ppProtectedValueClasses;
                        *ppProtectedValueClasses = pValueClassInfo;
                    }
                    else
#endif // ENREGISTERED_PARAMTYPE_MAXSIZE
                    *pArgument = PtrToArgSlot(pAddr);

                }
            }
            else
            {
                if (fNeedBoxOrUnbox)
                {
                    *pArgument = ObjToArgSlot(*pObjectRefArg);
                }
                else
                {
                    if (pFEAD->argAddr)
                    {
                        *pArgument = PtrToArgSlot(pAddr);
                    }
                    else
                    {
                        // The argument is the address of where we're holding the primitive in the PrimitiveArg array. We
                        // stick the real value from the register into the PrimitiveArg array.  It should be in a single 
                        // register since it is pointer-sized.
                        _ASSERTE( pFEAD->argHome.kind == RAK_REG );
                        *pArgument = PtrToArgSlot(pBufferArg);
                        *pBufferArg = (INT64)v;
                    }
                }
            }
        }
        break;

    default:
        // literal values smaller than 8 bytes and "special types" (e.g. object, string, etc.)

        {
            INT64 *pSource;

            INDEBUG(DataLocation expectedLocation);

#ifdef _TARGET_X86_
            if ((pFEAD->argElementType == ELEMENT_TYPE_I4) ||
                (pFEAD->argElementType == ELEMENT_TYPE_U4) ||
                (pFEAD->argElementType == ELEMENT_TYPE_R4))
            {
                INDEBUG(expectedLocation = DL_MaybeInteriorPtrArray);

                pSource = (INT64 *)pMaybeInteriorPtrArg;
            }
            else
#endif
            if (IsElementTypeSpecial(pFEAD->argElementType))
            {
                INDEBUG(expectedLocation = DL_ObjectRefArray);

                pSource = (INT64 *)pObjectRefArg;
            }
            else
            {
                INDEBUG(expectedLocation = DL_BufferForArgsArray);

                pSource = pBufferArg;
            }

            if (pFEAD->argAddr != NULL)
            {
                if (!isByRef)
                {
                    if (pFEAD->argIsHandleValue)
                    {
                        _ASSERTE(dataLocation & DL_BufferForArgsArray);

                        OBJECTHANDLE oh = *((OBJECTHANDLE*)(pBufferArg));  // Always comes from buffer
                        *pArgument = PtrToArgSlot(g_pEEInterface->GetObjectFromHandle(oh));
                    }
                    else
                    {
                        _ASSERTE(dataLocation & expectedLocation);

                        if (pSource != NULL)
                        {
                            *pArgument = *pSource; // may come from either array.
                        }
                        else
                        {
                            *pArgument = NULL;
                        }
                    }
                }
                else
                {
                    if (pFEAD->argIsHandleValue)
                    {
                        _ASSERTE(dataLocation & DL_BufferForArgsArray);

                        *pArgument = *pBufferArg; // Buffer contains the object handle, in this case, so
                                                  // just copy that across.
                    }
                    else
                    {
                        _ASSERTE(dataLocation & expectedLocation);

                        *pArgument = PtrToArgSlot(pSource); // Load the argument with the address of our buffer.
                    }
                }
            }
            else if (pFEAD->argIsLiteral)
            {
                _ASSERTE(dataLocation & expectedLocation);

                if (!isByRef)
                {
                    if (pSource != NULL)
                    {
                        *pArgument = *pSource; // may come from either array.
                    }
                    else
                    {
                        *pArgument = NULL;
                    }
                }
                else
                {
                    *pArgument = PtrToArgSlot(pSource); // Load the argument with the address of our buffer.
                }
            }
            else
            {
                if (!isByRef)
                {
                    if (pSource != NULL)
                    {
                        *pArgument = *pSource; // may come from either array.
                    }
                    else
                    {
                        *pArgument = NULL;
                    }
                }
                else
                {
                    *pArgument = PtrToArgSlot(pSource); // Load the argument with the address of our buffer.
                }
            }

            // If we need to unbox, then unbox the arg now.
            if (fNeedBoxOrUnbox)
            {
                if (!isByRef)
                {
                    // function expects valuetype, argument received is class or object

                    // Take the ObjectRef off the stack.
                    ARG_SLOT oi1 = *pArgument;
                    OBJECTREF o1 = ArgSlotToObj(oi1);

                    // For Nullable types, we need a 'true' nullable to pass to the function, and we do this
                    // by passing a boxed nullable that we unbox.  We allocated this space earlier however we
                    // did not know the data location until just now.  Fill it in with the data and use that
                    // to pass to the function.

                    if (Nullable::IsNullableType(argTH)) 
                    {
                        _ASSERTE(*pObjectRefArg != 0);
                        _ASSERTE((*pObjectRefArg)->GetMethodTable() == argTH.GetMethodTable());
                        if (o1 != *pObjectRefArg) 
                        {
                            Nullable::UnBoxNoCheck((*pObjectRefArg)->GetData(), o1, (*pObjectRefArg)->GetMethodTable());
                            o1 = *pObjectRefArg;
                        }
                    }

                    if (o1 == NULL)
                    {
                        COMPlusThrow(kArgumentNullException);
                    }


                    if (!o1->GetMethodTable()->IsValueType())
                    {
                        COMPlusThrow(kArgumentException, W("Argument_BadObjRef"));
                    }


                    // Unbox the little fella to get a pointer to the raw data.
                    void *pData = o1->GetData();

                    // Get its size to make sure it fits in an ARG_SLOT
                    unsigned size = o1->GetMethodTable()->GetNumInstanceFieldBytes();

                    if (size <= sizeof(ARG_SLOT))
                    {
                        // Its not ByRef, so we need to copy the value class onto the ARG_SLOT.
                        CopyValueClass(ArgSlotEndianessFixup(pArgument, sizeof(LPVOID)), pData, o1->GetMethodTable());
                    }
                    else
                    {
                        // Store pointer to the space in the ARG_SLOT
                        *pArgument = PtrToArgSlot(pData);
                    }
                }
                else
                {
                    // Function expects byref valuetype, argument received is byref class.

                    // Grab the ObjectRef off the stack via the pointer on the stack. Note: the stack has a pointer to the
                    // ObjectRef since the arg was specified as byref.
                    OBJECTREF* op1 = (OBJECTREF*)ArgSlotToPtr(*pArgument);
                    if (op1 == NULL)
                    {
                        COMPlusThrow(kArgumentNullException);
                    }
                    OBJECTREF o1 = *op1;

                    // For Nullable types, we need a 'true' nullable to pass to the function, and we do this
                    // by passing a boxed nullable that we unbox.  We allocated this space earlier however we
                    // did not know the data location until just now.  Fill it in with the data and use that
                    // to pass to the function.

                    if (Nullable::IsNullableType(byrefArgTH)) 
                    {
                         _ASSERTE(*pObjectRefArg != 0 && (*pObjectRefArg)->GetMethodTable() == byrefArgTH.GetMethodTable());
                        if (o1 != *pObjectRefArg) 
                        {
                            Nullable::UnBoxNoCheck((*pObjectRefArg)->GetData(), o1, (*pObjectRefArg)->GetMethodTable());
                            o1 = *pObjectRefArg;
                        }
                    }

                    if (o1 == NULL)
                    {
                        COMPlusThrow(kArgumentNullException);
                    }

                    _ASSERTE(o1->GetMethodTable()->IsValueType());

                    // Unbox the little fella to get a pointer to the raw data.
                    void *pData = o1->GetData();

                    // If it is ByRef, then we just replace the ObjectRef with a pointer to the data.
                    *pArgument = PtrToArgSlot(pData);
                }
            }

            // Validate any objectrefs that are supposed to be on the stack.
            // <TODO>@TODO: Move this to before the boxing/unboxing above</TODO>
            if (!fNeedBoxOrUnbox)
            {
                Object *objPtr;
                if (!isByRef)
                {
                    if (IsElementTypeSpecial(argSigType))
                    {
                        // validate the integrity of the object
                        objPtr = (Object*)ArgSlotToPtr(*pArgument);
                        if (FAILED(ValidateObject(objPtr)))
                        {
                            COMPlusThrow(kArgumentException, W("Argument_BadObjRef"));
                        }
                    }
                }
                else
                {
                    _ASSERTE(argSigType == ELEMENT_TYPE_BYREF);
                    if (IsElementTypeSpecial(byrefArgSigType))
                    {
                        objPtr = *(Object**)(ArgSlotToPtr(*pArgument));
                        if (FAILED(ValidateObject(objPtr)))
                        {
                            COMPlusThrow(kArgumentException, W("Argument_BadObjRef"));
                        }
                    }
                }
            }
        }
    }
}

static CorDebugRegister GetArgAddrFromReg( DebuggerIPCE_FuncEvalArgData *pFEAD)
{
    CorDebugRegister retval = REGISTER_INSTRUCTION_POINTER; // good as default as any
#if defined(_WIN64)
    retval = (pFEAD->argHome.kind == RAK_REG ?
              pFEAD->argHome.reg1 :
              (CorDebugRegister)((int)REGISTER_IA64_F0 + pFEAD->argHome.floatIndex));
#else  // !_WIN64
    retval = pFEAD->argHome.reg1;
#endif // !_WIN64
    return retval;
}

// 
// Given info about a byref argument, retrieve the current value from the pBufferForArgsArray, 
// the pMaybeInteriorPtrArray, the pByRefMaybeInteriorPtrArray, or the pObjectRefArray.  Then 
// place it back into the proper register or address.
//
// Note that we should never use the argAddr of the DebuggerIPCE_FuncEvalArgData in this function
// since the address may be an interior GC pointer and may have been moved by the GC.  Instead,
// use the pByRefMaybeInteriorPtrArray.
//
static void SetFuncEvalByRefArgValue(DebuggerEval *pDE,
                                     DebuggerIPCE_FuncEvalArgData *pFEAD,
                                     CorElementType byrefArgSigType,
                                     INT64 bufferByRefArg,
                                     void *maybeInteriorPtrArg,
                                     void *byRefMaybeInteriorPtrArg,
                                     OBJECTREF objectRefByRefArg)
{
    WRAPPER_NO_CONTRACT;

    switch (pFEAD->argElementType)
    {
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R8:
        // 64bit values
        {
            INT64 source;

#if defined(_WIN64)
            source = (INT64)maybeInteriorPtrArg;
#else  // !_WIN64
            source = bufferByRefArg;
#endif // !_WIN64

            if (pFEAD->argIsLiteral)
            {
                // If this was a literal arg, then copy the updated primitive back into the literal.
                memcpy(pFEAD->argLiteralData, &source, sizeof(pFEAD->argLiteralData));
            }
            else if (pFEAD->argAddr != NULL)
            {
                *((INT64 *)byRefMaybeInteriorPtrArg) = source;
                return;
            }
            else
            {
#if !defined(_WIN64)
                // RAK_REG is the only 4 byte type, all others are 8 byte types.
                _ASSERTE(pFEAD->argHome.kind != RAK_REG);

                SIZE_T *pLow = (SIZE_T*)(&source);
                SIZE_T *pHigh  = pLow + 1;

                switch (pFEAD->argHome.kind)
                {
                case RAK_REGREG:
                    SetRegisterValue(pDE, pFEAD->argHome.u.reg2, pFEAD->argHome.u.reg2Addr, *pLow);
                    SetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, *pHigh);
                    break;

                case RAK_MEMREG:
                    SetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, *pLow);
                    *((SIZE_T*)CORDB_ADDRESS_TO_PTR(pFEAD->argHome.addr)) = *pHigh;
                    break;

                case RAK_REGMEM:
                    *((SIZE_T*)CORDB_ADDRESS_TO_PTR(pFEAD->argHome.addr)) = *pLow;
                    SetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, *pHigh);
                    break;

                default:
                    break;
                }
#else // _WIN64
                // The only types we use are RAK_REG and RAK_FLOAT, and both of them can be 4 or 8 bytes.
                _ASSERTE((pFEAD->argHome.kind == RAK_REG) || (pFEAD->argHome.kind == RAK_FLOAT));

                SetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, source);
#endif // _WIN64
            }
        }
        break;

    default:
        // literal values smaller than 8 bytes and "special types" (e.g. object, array, string, etc.)
        {
            SIZE_T source;

#ifdef _TARGET_X86_
            if ((pFEAD->argElementType == ELEMENT_TYPE_I4) ||
                (pFEAD->argElementType == ELEMENT_TYPE_U4) ||
                (pFEAD->argElementType == ELEMENT_TYPE_R4))
            {
                source = (SIZE_T)maybeInteriorPtrArg;
            }
            else
            {
#endif
                source = (SIZE_T)bufferByRefArg;
#ifdef _TARGET_X86_
            }
#endif

            if (pFEAD->argIsLiteral)
            {
                // If this was a literal arg, then copy the updated primitive back into the literal.
                // The literall buffer is a fixed size (8 bytes), but our source may be 4 or 8 bytes
                // depending on the platform.  To prevent reading past the end of the source, we
                // zero the destination buffer and copy only as many bytes as available.
                memset( pFEAD->argLiteralData, 0, sizeof(pFEAD->argLiteralData) );
                if (IsElementTypeSpecial(pFEAD->argElementType))
                {
                    _ASSERTE( sizeof(pFEAD->argLiteralData) >= sizeof(objectRefByRefArg) );
                    memcpy(pFEAD->argLiteralData, &objectRefByRefArg, sizeof(objectRefByRefArg));
                }
                else
                {
                    _ASSERTE( sizeof(pFEAD->argLiteralData) >= sizeof(source) );
                    memcpy(pFEAD->argLiteralData, &source, sizeof(source));
                }
            }
            else if (pFEAD->argAddr == NULL)
            {
                // If the 32bit value is enregistered, copy it back to the proper regs.

                // RAK_REG is the only valid 4 byte type on WIN32.  On WIN64, both RAK_REG and RAK_FLOAT can be
                // 4 bytes or 8 bytes.
                _ASSERTE((pFEAD->argHome.kind == RAK_REG)
                         WIN64_ONLY(|| (pFEAD->argHome.kind == RAK_FLOAT)));

                CorDebugRegister regNum = GetArgAddrFromReg(pFEAD);

                // Shove the result back into the proper register.
                if (IsElementTypeSpecial(pFEAD->argElementType))
                {
                    SetRegisterValue(pDE, regNum, pFEAD->argHome.reg1Addr, (SIZE_T)ObjToArgSlot(objectRefByRefArg));
                }
                else
                {
                    SetRegisterValue(pDE, regNum, pFEAD->argHome.reg1Addr, (SIZE_T)source);
                }
            }
            else
            {
                // If the result was an object by ref, then copy back the new location of the object (in GC case).
                if (pFEAD->argIsHandleValue) 
                {
                    // do nothing.  The Handle was passed in the pArgument array directly
                }
                else if (IsElementTypeSpecial(pFEAD->argElementType))
                {
                    *((SIZE_T*)byRefMaybeInteriorPtrArg) = (SIZE_T)ObjToArgSlot(objectRefByRefArg);
                }
                else if (pFEAD->argElementType == ELEMENT_TYPE_VALUETYPE)
                {
                    // Do nothing, we passed in the pointer to the valuetype in the pArgument array directly.
                }
                else
                {
                    GetAndSetLiteralValue(byRefMaybeInteriorPtrArg, pFEAD->argElementType, &source, ELEMENT_TYPE_PTR);
                }
            }
        } // end default
    } // end switch
}


/*
 * GCProtectAllPassedArgs
 *
 * This routine is the first step in doing a func-eval.  For a complete overview, see
 * the comments at the top of this file.
 *
 * This routine over-aggressively protects all arguments that may be references to
 * managed objects.  This function cannot crawl the function signature, since doing
 * so may trigger a GC, and thus, we must assume everything is ByRef.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    pObjectRefArray - An array that contains any object refs.  It was built previously.
 *    pMaybeInteriorPtrArray - An array that contains values that may be pointers to
 *         the interior of a managed object.
 *    pBufferForArgsArray - An array for holding stuff that does not need to be protected.
 *         Any handle for the 'this' pointer is put in here for pulling it out later.
 *
 * Returns:
 *    None.
 *
 */
static void GCProtectAllPassedArgs(DebuggerEval *pDE,
                                   OBJECTREF *pObjectRefArray,
                                   void **pMaybeInteriorPtrArray,
                                   void **pByRefMaybeInteriorPtrArray,
                                   INT64 *pBufferForArgsArray
                                   DEBUG_ARG(DataLocation pDataLocationArray[])
                                  )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;


    DebuggerIPCE_FuncEvalArgData *argData = pDE->GetArgData();

    unsigned currArgIndex = 0;

    //
    // Gather all the information for the parameters.
    //
    for ( ; currArgIndex < pDE->m_argCount; currArgIndex++)
    {
        DebuggerIPCE_FuncEvalArgData *pFEAD = &argData[currArgIndex];

        // In case any of the arguments is a by ref argument and points into the GC heap, 
        // we need to GC protect their addresses as well.
        if (pFEAD->argAddr != NULL)
        {
            pByRefMaybeInteriorPtrArray[currArgIndex] = pFEAD->argAddr;
        }

        switch (pFEAD->argElementType)
        {
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R8:
            // 64bit values

#if defined(_WIN64)
            //
            // Only need to worry about protecting if a pointer is a 64 bit quantity.
            //
            _ASSERTE(sizeof(void *) == sizeof(INT64));

            if (pFEAD->argAddr != NULL)
            {
                pMaybeInteriorPtrArray[currArgIndex] = *((void **)(pFEAD->argAddr));
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_MaybeInteriorPtrArray;
                }
#endif
            }
            else if (pFEAD->argIsLiteral)
            {
                _ASSERTE(sizeof(pFEAD->argLiteralData) >= sizeof(void *));

                //
                // If this is a byref literal arg, then it maybe an interior ptr.
                //
                void *v = NULL;
                memcpy(&v, pFEAD->argLiteralData, sizeof(v));
                pMaybeInteriorPtrArray[currArgIndex] = v;
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_MaybeInteriorPtrArray;
                }
#endif
            }
            else
            {
                _ASSERTE((pFEAD->argHome.kind == RAK_REG) || (pFEAD->argHome.kind == RAK_FLOAT));


                CorDebugRegister regNum = GetArgAddrFromReg(pFEAD);
                SIZE_T v = GetRegisterValue(pDE, regNum, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);
                pMaybeInteriorPtrArray[currArgIndex] = (void *)(v);

#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_MaybeInteriorPtrArray;
                }
#endif
            }
#endif // _WIN64
            break;

        case ELEMENT_TYPE_VALUETYPE:
            //
            // If the value type address could be an interior pointer.
            //
            if (pFEAD->argAddr != NULL)
            {
                pMaybeInteriorPtrArray[currArgIndex] = ((void **)(pFEAD->argAddr));
            }

            INDEBUG(pDataLocationArray[currArgIndex] |= DL_MaybeInteriorPtrArray);
            break;

        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:

            if (pFEAD->argAddr != NULL)
            {
                if (pFEAD->argIsHandleValue)
                {
                    OBJECTHANDLE oh = (OBJECTHANDLE)(pFEAD->argAddr);
                    pBufferForArgsArray[currArgIndex] = (INT64)(size_t)oh;

                    INDEBUG(pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray);
                }
                else
                {
                    pObjectRefArray[currArgIndex] = *((OBJECTREF *)(pFEAD->argAddr));

                    INDEBUG(pDataLocationArray[currArgIndex] |= DL_ObjectRefArray);
                }
            }
            else if (pFEAD->argIsLiteral)
            {
                _ASSERTE(sizeof(pFEAD->argLiteralData) >= sizeof(OBJECTREF));
                OBJECTREF v = NULL;
                memcpy(&v, pFEAD->argLiteralData, sizeof(v));
                pObjectRefArray[currArgIndex] = v;
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_ObjectRefArray;
                }
#endif
            }
            else
            {
                // RAK_REG is the only valid pointer-sized type.
                _ASSERTE(pFEAD->argHome.kind == RAK_REG);

                // Simply grab the value out of the proper register.
                SIZE_T v = GetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);

                // The argument is the address.
                pObjectRefArray[currArgIndex] = (OBJECTREF)v;
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_ObjectRefArray;
                }
#endif
            }
            break;

        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_R4:
            // 32bit values

#ifdef _TARGET_X86_
            _ASSERTE(sizeof(void *) == sizeof(INT32));

            if (pFEAD->argAddr != NULL)
            {
                if (pFEAD->argIsHandleValue)
                {
                    //
                    // Ignorable - no need to protect
                    //
                }
                else
                {
                    pMaybeInteriorPtrArray[currArgIndex] = *((void **)(pFEAD->argAddr));
#ifdef _DEBUG
                    if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                    {
                        pDataLocationArray[currArgIndex] |= DL_MaybeInteriorPtrArray;
                    }
#endif
                }
            }
            else if (pFEAD->argIsLiteral)
            {
                _ASSERTE(sizeof(pFEAD->argLiteralData) >= sizeof(INT32));

                //
                // If this is a byref literal arg, then it maybe an interior ptr.
                //
                void *v = NULL;
                memcpy(&v, pFEAD->argLiteralData, sizeof(v));
                pMaybeInteriorPtrArray[currArgIndex] = v;
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_MaybeInteriorPtrArray;
                }
#endif
            }
            else
            {
                // RAK_REG is the only valid 4 byte type on WIN32.
                _ASSERTE(pFEAD->argHome.kind == RAK_REG);

                // Simply grab the value out of the proper register.
                SIZE_T v = GetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);

                // The argument is the address.
                pMaybeInteriorPtrArray[currArgIndex] = (void *)v;
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_MaybeInteriorPtrArray;
                }
#endif
            }
#endif // _TARGET_X86_

        default:
            //
            // Ignorable - no need to protect
            //
            break;
        }
    }
}

/*
 * ResolveFuncEvalGenericArgInfo
 *
 * This function pulls out any generic args and makes sure the method is loaded for it.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *
 * Returns:
 *    None.
 *
 */
void ResolveFuncEvalGenericArgInfo(DebuggerEval *pDE)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;
    
    DebuggerIPCE_TypeArgData *firstdata = pDE->GetTypeArgData();
    unsigned int nGenericArgs = pDE->m_genericArgsCount;
    SIZE_T cbAllocSize;
    if ((!ClrSafeInt<SIZE_T>::multiply(nGenericArgs, sizeof(TypeHandle *), cbAllocSize)) ||
        (cbAllocSize != (size_t)(cbAllocSize)))
    {
        ThrowHR(COR_E_OVERFLOW);
    }
    TypeHandle * pGenericArgs = (nGenericArgs == 0) ? NULL : (TypeHandle *) _alloca(cbAllocSize);

    //
    // Snag the type arguments from the input and get the
    // method desc that corresponds to the instantiated desc.
    //
    Debugger::TypeDataWalk walk(firstdata, pDE->m_genericArgsNodeCount);
    walk.ReadTypeHandles(nGenericArgs, pGenericArgs);
    
    // <TODO>better error message</TODO>
    if (!walk.Finished())
    {
        COMPlusThrow(kArgumentException, W("Argument_InvalidGenericArg"));
    }
    
    // Find the proper MethodDesc that we need to call.
    // Since we're already in the target domain, it can't be unloaded so it's safe to 
    // use domain specific structures like the Module*.
    _ASSERTE( GetAppDomain() == pDE->m_debuggerModule->GetAppDomain() );
    pDE->m_md = g_pEEInterface->LoadMethodDef(pDE->m_debuggerModule->GetRuntimeModule(),
                                              pDE->m_methodToken,
                                              nGenericArgs,
                                              pGenericArgs,
                                              &(pDE->m_ownerTypeHandle));
    
    
    // We better have a MethodDesc at this point.
    _ASSERTE(pDE->m_md != NULL);

    ValidateFuncEvalReturnType(pDE->m_evalType , pDE->m_md->GetMethodTable());
    
    // If this is a new object operation, then we should have a .ctor.
    if ((pDE->m_evalType == DB_IPCE_FET_NEW_OBJECT) && !pDE->m_md->IsCtor())
    {
        COMPlusThrow(kArgumentException, W("Argument_MissingDefaultConstructor"));
    }
    
    pDE->m_md->EnsureActive();
    
    // Run the Class Init for this class, if necessary.
    MethodTable * pOwningMT = pDE->m_ownerTypeHandle.GetMethodTable();
    pOwningMT->EnsureInstanceActive();
    pOwningMT->CheckRunClassInitThrowing();
    
    if (pDE->m_evalType == DB_IPCE_FET_NEW_OBJECT)
    {
        // Work out the exact type of the allocated object
        pDE->m_resultType = (nGenericArgs == 0)
                           ? TypeHandle(pDE->m_md->GetMethodTable())
                           : g_pEEInterface->LoadInstantiation(pDE->m_md->GetModule(), pDE->m_md->GetMethodTable()->GetCl(), nGenericArgs, pGenericArgs);
    }
}


/*
 * BoxFuncEvalThisParameter
 *
 * This function is a helper for DoNormalFuncEval.  It boxes the 'this' parameter if necessary.
 * For example, when  a method Object.ToString is called on a value class like System.DateTime 
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    argData - Array of information about the arguments.
 *    pMaybeInteriorPtrArray - An array that contains values that may be pointers to
 *         the interior of a managed object.
 *    pObjectRef - A GC protected place to put a boxed value, if necessary.
 *
 * Returns:
 *    None
 *
 */
void BoxFuncEvalThisParameter(DebuggerEval *pDE,
                           DebuggerIPCE_FuncEvalArgData *argData,
                           void **pMaybeInteriorPtrArray,
                           OBJECTREF *pObjectRefArg          // out
                           DEBUG_ARG(DataLocation pDataLocationArray[])
                          )
{
    WRAPPER_NO_CONTRACT;

    //
    // See if we have a value type that is going to be passed as a 'this' pointer.
    //
    if ((pDE->m_evalType != DB_IPCE_FET_NEW_OBJECT) &&
        !pDE->m_md->IsStatic() &&
        (pDE->m_argCount > 0))
    {
        // Allocate the space for box nullables.  Nullable parameters need a unboxed 
        // nullable value to point at, where our current representation does not have
        // an unboxed value inside them. Thus we need another buffer to hold it (and
        // gcprotects it.  We used boxed values for this by converting them to 'true'
        // nullable form, calling the function, and in the case of byrefs, converting
        // them back afterward. 

        MethodTable* pMT = pDE->m_md->GetMethodTable();
        if (Nullable::IsNullableType(pMT)) 
        {
            OBJECTREF obj = AllocateObject(pMT);
            if (*pObjectRefArg != NULL) 
            {
                BOOL typesMatch = Nullable::UnBox(obj->GetData(), *pObjectRefArg, pMT);
                (void)typesMatch; //prevent "unused variable" error from GCC
                _ASSERTE(typesMatch);
            }
            *pObjectRefArg = obj;
        }

        if (argData[0].argElementType == ELEMENT_TYPE_VALUETYPE)
        {
            //
            // See if we need to box up the 'this' parameter.
            //
            if (!pDE->m_md->GetMethodTable()->IsValueType())
            {
                DebuggerIPCE_FuncEvalArgData *pFEAD = &argData[0];
                SIZE_T v;
                LPVOID pAddr = NULL;
                INT64 bigVal;

                {
                    GCX_FORBID();    //pAddr is unprotected from the time we initialize it
                    
                    if (pFEAD->argAddr != NULL)
                    {
                        _ASSERTE(pDataLocationArray[0] & DL_MaybeInteriorPtrArray);
                        pAddr = pMaybeInteriorPtrArray[0];
                        INDEBUG(pDataLocationArray[0] &= ~DL_MaybeInteriorPtrArray);
                    }
                    else
                    {

                        pAddr = GetRegisterValueAndReturnAddress(pDE, pFEAD, &bigVal, &v);

                        if (pAddr == NULL)
                        {
                            COMPlusThrow(kArgumentNullException);
                        }
                    }

                    _ASSERTE(pAddr != NULL);
                } //GCX_FORBID
                
                GCPROTECT_BEGININTERIOR(pAddr); //ReadTypeHandle may trigger a GC and move the object that has the value type at pAddr as a field

                //
                // Grab the class of this value type.  If the type is a parameterized
                // struct type then it may not have yet been loaded by the EE (generics
                // code sharing may have meant we have never bothered to create the exact
                // type yet).
                //
                // A buffer should have been allocated for the full struct type
                _ASSERTE(argData[0].fullArgType != NULL);
                Debugger::TypeDataWalk walk((DebuggerIPCE_TypeArgData *) argData[0].fullArgType, argData[0].fullArgTypeNodeCount);

                TypeHandle typeHandle = walk.ReadTypeHandle();

                if (typeHandle.IsNull())
                {
                    COMPlusThrow(kArgumentException, W("Argument_BadObjRef"));
                }
                //
                // Box up this value type
                //
                *pObjectRefArg = typeHandle.GetMethodTable()->Box(pAddr);
                if (Nullable::IsNullableType(typeHandle.GetMethodTable()) && (*pObjectRefArg == NULL))
                {
                    COMPlusThrow(kArgumentNullException);
                }
                GCPROTECT_END();

                INDEBUG(pDataLocationArray[0] |= DL_ObjectRefArray);
            }
        }
    }
}


//
// This is used to store (temporarily) information about the arguments that func-eval
// will pass.  It is used only for the args of the function, not the return buffer nor
// the 'this' pointer, if there is any of either.
//
struct FuncEvalArgInfo
{
    CorElementType argSigType;
    CorElementType byrefArgSigType;
    TypeHandle     byrefArgTypeHandle;
    bool fNeedBoxOrUnbox;
    TypeHandle sigTypeHandle;
};



/*
 * GatherFuncEvalArgInfo
 *
 * This function is a helper for DoNormalFuncEval.  It gathers together all the information
 * necessary to process the arguments.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    mSig - The metadata signature of the fuction to call.
 *    argData - Array of information about the arguments.
 *    pFEArgInfo - An array of structs to hold the argument information.
 *
 * Returns:
 *    None.
 *
 */
void GatherFuncEvalArgInfo(DebuggerEval *pDE,
                           MetaSig mSig,
                           DebuggerIPCE_FuncEvalArgData *argData,
                           FuncEvalArgInfo *pFEArgInfo    // out
                          )
{
    WRAPPER_NO_CONTRACT;

    unsigned currArgIndex = 0;

    if ((pDE->m_evalType == DB_IPCE_FET_NORMAL) && !pDE->m_md->IsStatic())
    {
        //
        // Skip over the 'this' arg, since this function is not supposed to mess with it.
        //
        currArgIndex++;
    }

    //
    // Gather all the information for the parameters.
    //
    for ( ; currArgIndex < pDE->m_argCount; currArgIndex++)
    {
        DebuggerIPCE_FuncEvalArgData *pFEAD = &argData[currArgIndex];

        //
        // Move to the next arg in the signature.
        //
        CorElementType argSigType = mSig.NextArgNormalized();
        _ASSERTE(argSigType != ELEMENT_TYPE_END);

        //
        // If this arg is a byref arg, then we'll need to know what type we're referencing for later...
        //
        TypeHandle byrefTypeHandle = TypeHandle();
        CorElementType byrefArgSigType = ELEMENT_TYPE_END;
        if (argSigType == ELEMENT_TYPE_BYREF)
        {
            byrefArgSigType = mSig.GetByRefType(&byrefTypeHandle);
        }

        //
        // If the sig says class but we've got a value class parameter, then remember that we need to box it.  If
        // the sig says value class, but we've got a boxed value class, then remember that we need to unbox it.
        //
        bool fNeedBoxOrUnbox = ((argSigType == ELEMENT_TYPE_CLASS) && (pFEAD->argElementType == ELEMENT_TYPE_VALUETYPE)) ||
            (((argSigType == ELEMENT_TYPE_VALUETYPE) && ((pFEAD->argElementType == ELEMENT_TYPE_CLASS) || (pFEAD->argElementType == ELEMENT_TYPE_OBJECT))) ||
            // This is when method signature is expecting a BYREF ValueType, yet we receive the boxed valuetype's handle.
            (pFEAD->argElementType == ELEMENT_TYPE_CLASS && argSigType == ELEMENT_TYPE_BYREF && byrefArgSigType == ELEMENT_TYPE_VALUETYPE));

        pFEArgInfo[currArgIndex].argSigType = argSigType;
        pFEArgInfo[currArgIndex].byrefArgSigType = byrefArgSigType;
        pFEArgInfo[currArgIndex].byrefArgTypeHandle = byrefTypeHandle;
        pFEArgInfo[currArgIndex].fNeedBoxOrUnbox = fNeedBoxOrUnbox; 
        pFEArgInfo[currArgIndex].sigTypeHandle = mSig.GetLastTypeHandleThrowing(); 
    } 
} 


/*
 * BoxFuncEvalArguments
 *
 * This function is a helper for DoNormalFuncEval.  It boxes all the arguments that
 * need to be.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    argData - Array of information about the arguments.
 *    pFEArgInfo - An array of structs to hold the argument information.
 *    pMaybeInteriorPtrArray - An array that contains values that may be pointers to
 *         the interior of a managed object.
 *    pObjectRef - A GC protected place to put a boxed value, if necessary.
 *
 * Returns:
 *    None
 *
 */
void BoxFuncEvalArguments(DebuggerEval *pDE,
                          DebuggerIPCE_FuncEvalArgData *argData,
                          FuncEvalArgInfo *pFEArgInfo,
                          void **pMaybeInteriorPtrArray,
                          OBJECTREF *pObjectRef          // out
                          DEBUG_ARG(DataLocation pDataLocationArray[])
                         )
{
    WRAPPER_NO_CONTRACT;

    unsigned currArgIndex = 0;


    if ((pDE->m_evalType == DB_IPCE_FET_NORMAL) && !pDE->m_md->IsStatic())
    {
        //
        // Skip over the 'this' arg, since this function is not supposed to mess with it.
        //
        currArgIndex++;
    }

    //
    // Gather all the information for the parameters.
    //
    for ( ; currArgIndex < pDE->m_argCount; currArgIndex++)
    {
        DebuggerIPCE_FuncEvalArgData *pFEAD = &argData[currArgIndex];

        // Allocate the space for box nullables.  Nullable parameters need a unboxed 
        // nullable value to point at, where our current representation does not have
        // an unboxed value inside them. Thus we need another buffer to hold it (and
        // gcprotects it.  We used boxed values for this by converting them to 'true'
        // nullable form, calling the function, and in the case of byrefs, converting
        // them back afterward. 

        TypeHandle th = pFEArgInfo[currArgIndex].sigTypeHandle;
        if (pFEArgInfo[currArgIndex].argSigType == ELEMENT_TYPE_BYREF)
            th = pFEArgInfo[currArgIndex].byrefArgTypeHandle;

        if (!th.IsNull() && Nullable::IsNullableType(th)) 
        {

            OBJECTREF obj = AllocateObject(th.AsMethodTable());
            if (pObjectRef[currArgIndex] != NULL) 
            {
                BOOL typesMatch = Nullable::UnBox(obj->GetData(), pObjectRef[currArgIndex], th.AsMethodTable());
                (void)typesMatch; //prevent "unused variable" error from GCC
                _ASSERTE(typesMatch);
            }
            pObjectRef[currArgIndex] = obj;
        }

        //
        // Check if we should box this value now
        //
        if ((pFEAD->argElementType == ELEMENT_TYPE_VALUETYPE) &&
            (pFEArgInfo[currArgIndex].argSigType == ELEMENT_TYPE_BYREF) &&
            pFEArgInfo[currArgIndex].fNeedBoxOrUnbox)
        {
            SIZE_T v;
            INT64 bigVal;
            LPVOID pAddr = NULL;

            if (pFEAD->argAddr != NULL)
            {
                _ASSERTE(pDataLocationArray[currArgIndex] & DL_MaybeInteriorPtrArray);
                pAddr = pMaybeInteriorPtrArray[currArgIndex];
                INDEBUG(pDataLocationArray[currArgIndex] &= ~DL_MaybeInteriorPtrArray);
            }
            else
            {

                pAddr = GetRegisterValueAndReturnAddress(pDE, pFEAD, &bigVal, &v);

                if (pAddr == NULL)
                {
                    COMPlusThrow(kArgumentNullException);
                }
            }

            _ASSERTE(pAddr != NULL);

            MethodTable * pMT = pFEArgInfo[currArgIndex].sigTypeHandle.GetMethodTable();

            //
            // Stuff the newly boxed item into our GC-protected array.
            //
            pObjectRef[currArgIndex] = pMT->Box(pAddr);

#ifdef _DEBUG
            if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
            {
                pDataLocationArray[currArgIndex] |= DL_ObjectRefArray;
            }
#endif
        }
    }
}


/*
 * GatherFuncEvalMethodInfo
 *
 * This function is a helper for DoNormalFuncEval.  It gathers together all the information
 * necessary to process the method
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    mSig - The metadata signature of the fuction to call.
 *    argData - Array of information about the arguments.
 *    ppUnboxedMD - Returns a resolve method desc if the original is an unboxing stub.
 *    pObjectRefArray - GC protected array of objects passed to this func-eval call.
 *         used to resolve down to the method target for generics.
 *    pBufferForArgsArray - Array of values not needing gc-protection.  May hold the
 *         handle for the method targer for generics.
 *    pfHasRetBuffArg - TRUE if the function has a return buffer.
 *    pRetValueType - The TypeHandle of the return value.
 *
 *
 * Returns:
 *    None.
 *
 */
void GatherFuncEvalMethodInfo(DebuggerEval *pDE,
                              MetaSig mSig,
                              DebuggerIPCE_FuncEvalArgData *argData,
                              MethodDesc **ppUnboxedMD,
                              OBJECTREF *pObjectRefArray,
                              INT64 *pBufferForArgsArray,
                              BOOL *pfHasRetBuffArg,          // out
                              BOOL *pfHasNonStdByValReturn,   // out
                              TypeHandle *pRetValueType       // out, only if fHasRetBuffArg == true
                              DEBUG_ARG(DataLocation pDataLocationArray[])
                             )
{
    WRAPPER_NO_CONTRACT;

    //
    // If 'this' is a non-static function that points to an unboxing stub, we need to return the
    // unboxed method desc to really call.
    //
    if ((pDE->m_evalType != DB_IPCE_FET_NEW_OBJECT) && !pDE->m_md->IsStatic() && pDE->m_md->IsUnboxingStub())
    {
        *ppUnboxedMD = pDE->m_md->GetMethodTable()->GetUnboxedEntryPointMD(pDE->m_md);
    }

    //
    // Resolve down to the method on the class of the 'this' parameter.
    //
    if ((pDE->m_evalType != DB_IPCE_FET_NEW_OBJECT) && pDE->m_md->IsVtableMethod())
    {
        //
        // Assuming that a constructor can't be an interface method...
        //
        _ASSERTE(pDE->m_evalType == DB_IPCE_FET_NORMAL);

        //
        // We need to go grab the 'this' argument to figure out what class we're headed for...
        //
        if (pDE->m_argCount == 0)
        {
            COMPlusThrow(kArgumentException, W("Argument_BadObjRef"));
        }

        //
        // We should have a valid this pointer.
        // <TODO>@todo: But the check should cover the register kind as well!</TODO>
        //
        if ((argData[0].argHome.kind == RAK_NONE) && (argData[0].argAddr == NULL))
        {
            COMPlusThrow(kArgumentNullException);
        }

        //
        // Assume we can only have this for real objects or boxed value types, not value classes...
        //
        _ASSERTE((argData[0].argElementType == ELEMENT_TYPE_OBJECT) ||
                 (argData[0].argElementType == ELEMENT_TYPE_STRING) ||
                 (argData[0].argElementType == ELEMENT_TYPE_CLASS) ||
                 (argData[0].argElementType == ELEMENT_TYPE_ARRAY) ||
                 (argData[0].argElementType == ELEMENT_TYPE_SZARRAY) ||
                 ((argData[0].argElementType == ELEMENT_TYPE_VALUETYPE) &&
                  (pObjectRefArray[0] != NULL)));

        //
        // Now get the object pointer to our first arg.
        //
        OBJECTREF objRef = NULL;
        GCPROTECT_BEGIN(objRef);

        if (argData[0].argElementType == ELEMENT_TYPE_VALUETYPE)
        {
            //
            // In this case, we know where it is.
            //
            objRef = pObjectRefArray[0];
            _ASSERTE(pDataLocationArray[0] & DL_ObjectRefArray);
        }
        else
        {
            TypeHandle  dummyTH;
            ARG_SLOT    objSlot;

            //
            // Take out the first arg. We're gonna trick GetFuncEvalArgValue by passing in just our
            // object ref as the stack.
            //
            // Note that we are passing ELEMENT_TYPE_END in the last parameter because we want to
            // supress the the valid object ref check.
            //
            GetFuncEvalArgValue(pDE,
                                &(argData[0]),
                                false,
                                false,
                                dummyTH,
                                ELEMENT_TYPE_CLASS,
                                dummyTH,
                                &objSlot,
                                NULL,
                                pObjectRefArray,
                                pBufferForArgsArray,
                                NULL,
                                ELEMENT_TYPE_END
                                DEBUG_ARG(pDataLocationArray[0])
                               );

            objRef = ArgSlotToObj(objSlot);
        }

        //
        // Validate the object
        //
        if (FAILED(ValidateObject(OBJECTREFToObject(objRef))))
        {
            COMPlusThrow(kArgumentException, W("Argument_BadObjRef"));
        }

        //
        // Null isn't valid in this case!
        //
        if (objRef == NULL)
        {
            COMPlusThrow(kArgumentNullException);
        }

        //
        // Make sure that the object supplied is of a type that can call the method supplied.
        //
        if (!g_pEEInterface->ObjIsInstanceOf(OBJECTREFToObject(objRef), pDE->m_ownerTypeHandle))
        {
            COMPlusThrow(kArgumentException, W("Argument_CORDBBadMethod"));
        }

        //
        // Now, find the proper MethodDesc for this interface method based on the object we're invoking the
        // method on.
        //
        pDE->m_targetCodeAddr = pDE->m_md->GetCallTarget(&objRef, pDE->m_ownerTypeHandle);

        GCPROTECT_END();
    }
    else
    {
        pDE->m_targetCodeAddr = pDE->m_md->GetCallTarget(NULL, pDE->m_ownerTypeHandle);
    }

    //
    // Get the resulting type now.  Doing this may trigger a GC or throw.
    //
    if (pDE->m_evalType != DB_IPCE_FET_NEW_OBJECT)
    {
        pDE->m_resultType = mSig.GetRetTypeHandleThrowing();
    }

    //
    // Check if there is an explicit return argument, or if the return type is really a VALUETYPE but our
    // calling convention is passing it in registers. We just need to remember the pretValueClass so
    // that we will box it properly on our way out.
    //
    {
        ArgIterator argit(&mSig);
        *pfHasRetBuffArg = argit.HasRetBuffArg();
        *pfHasNonStdByValReturn = argit.HasNonStandardByvalReturn();
    }

     CorElementType retType           = mSig.GetReturnType();
     CorElementType retTypeNormalized = mSig.GetReturnTypeNormalized();


    if (*pfHasRetBuffArg || *pfHasNonStdByValReturn 
        || ((retType == ELEMENT_TYPE_VALUETYPE) && (retType != retTypeNormalized)))
    {
        *pRetValueType  = mSig.GetRetTypeHandleThrowing();
    }
    else
    {
        //
        // Make sure the caller initialized this value
        //
        _ASSERTE((*pRetValueType).IsNull());
    }
}

/*
 * CopyArgsToBuffer
 *
 * This routine copies all the arguments to a local buffer, so that any one that needs to be
 * passed can be.  Note that this local buffer is NOT GC-protected, and so all the values
 * in the buffer may not be relied on.  You *must* use GetFuncEvalArgValue() to load up the
 * Arguments for the call, because it has the logic to decide which of the parallel arrays to pull
 * from.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    argData - Array of information about the arguments.
 *    pFEArgInfo - An array of structs to hold the argument information. Must have be previously filled in.
 *    pBufferArray - An array to store values.
 *
 * Returns:
 *    None.
 *
 */
void CopyArgsToBuffer(DebuggerEval *pDE,
                      DebuggerIPCE_FuncEvalArgData *argData,
                      FuncEvalArgInfo *pFEArgInfo,
                      INT64 *pBufferArray
                      DEBUG_ARG(DataLocation pDataLocationArray[])
                     )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    unsigned currArgIndex = 0;


    if ((pDE->m_evalType == DB_IPCE_FET_NORMAL) && !pDE->m_md->IsStatic())
    {
        //
        // Skip over the 'this' arg, since this function is not supposed to mess with it.
        //
        currArgIndex++;
    }

    //
    // Spin thru each argument now
    //
    for ( ; currArgIndex < pDE->m_argCount; currArgIndex++)
    {
        DebuggerIPCE_FuncEvalArgData *pFEAD = &argData[currArgIndex];
        BOOL isByRef = (pFEArgInfo[currArgIndex].argSigType == ELEMENT_TYPE_BYREF);
        BOOL fNeedBoxOrUnbox;
        fNeedBoxOrUnbox = pFEArgInfo[currArgIndex].fNeedBoxOrUnbox;


        LOG((LF_CORDB, LL_EVERYTHING, "CATB: currArgIndex=%d\n",
             currArgIndex));
        LOG((LF_CORDB, LL_EVERYTHING,
            "\t: argSigType=0x%x, byrefArgSigType=0x%0x, inType=0x%0x\n",
             pFEArgInfo[currArgIndex].argSigType,
             pFEArgInfo[currArgIndex].byrefArgSigType,
             pFEAD->argElementType));

        INT64 *pDest = &(pBufferArray[currArgIndex]);

        switch (pFEAD->argElementType)
        {
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R8:

            if (pFEAD->argAddr != NULL)
            {
                *pDest = *(INT64*)(pFEAD->argAddr);
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                }
#endif
            }
            else if (pFEAD->argIsLiteral)
            {
                _ASSERTE(sizeof(pFEAD->argLiteralData) >= sizeof(void *));

                // If this is a literal arg, then we just copy the data.
                memcpy(pDest, pFEAD->argLiteralData, sizeof(INT64));
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                }
#endif
            }
            else
            {

#if !defined(_WIN64)
                // RAK_REG is the only 4 byte type, all others are 8 byte types.
                _ASSERTE(pFEAD->argHome.kind != RAK_REG);
                
                INT64 bigVal = 0;
                SIZE_T v;
                INT64 *pAddr;

                pAddr = (INT64*)GetRegisterValueAndReturnAddress(pDE, pFEAD, &bigVal, &v);

                if (pAddr == NULL)
                {
                    COMPlusThrow(kArgumentNullException);
                }

                *pDest = *pAddr;

#else  // _WIN64
                // Both RAK_REG and RAK_FLOAT can be either 4 bytes or 8 bytes.
                _ASSERTE((pFEAD->argHome.kind == RAK_REG) || (pFEAD->argHome.kind == RAK_FLOAT));

                CorDebugRegister regNum = GetArgAddrFromReg(pFEAD);
                *pDest = GetRegisterValue(pDE, regNum, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);
#endif // _WIN64



#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                }
#endif
            }
            break;

        case ELEMENT_TYPE_VALUETYPE:

            //
            // For value types, we dont do anything here, instead delay until GetFuncEvalArgInfo
            //
            break;

        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:

            if (pFEAD->argAddr != NULL)
            {
                if (!isByRef)
                {
                    if (pFEAD->argIsHandleValue)
                    {
                        OBJECTHANDLE oh = (OBJECTHANDLE)(pFEAD->argAddr);
                        *pDest = (INT64)(size_t)oh;
                    }
                    else
                    {
                        *pDest = *((SIZE_T*)(pFEAD->argAddr));
                    }
#ifdef _DEBUG
                    if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                    {
                        pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                    }
#endif
                }
                else
                {
                    if (pFEAD->argIsHandleValue)
                    {
                        *pDest = (INT64)(size_t)(pFEAD->argAddr);
                    }
                    else
                    {
                        *pDest = *(SIZE_T*)(pFEAD->argAddr);
                    }
#ifdef _DEBUG
                    if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                    {
                        pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                    }
#endif
                }
            }
            else if (pFEAD->argIsLiteral)
            {
                _ASSERTE(sizeof(pFEAD->argLiteralData) >= sizeof(INT64));

                // The called function may expect a larger/smaller value than the literal value.
                // So we convert the value to the right type.

                CONSISTENCY_CHECK_MSGF(((pFEArgInfo[currArgIndex].argSigType == ELEMENT_TYPE_CLASS)   ||
                                        (pFEArgInfo[currArgIndex].argSigType == ELEMENT_TYPE_SZARRAY) ||
                                        (pFEArgInfo[currArgIndex].argSigType == ELEMENT_TYPE_ARRAY))  ||
                                       (isByRef && ((pFEArgInfo[currArgIndex].byrefArgSigType == ELEMENT_TYPE_CLASS)   ||
                                                    (pFEArgInfo[currArgIndex].byrefArgSigType == ELEMENT_TYPE_SZARRAY) ||
                                                    (pFEArgInfo[currArgIndex].byrefArgSigType == ELEMENT_TYPE_ARRAY))),
                                       ("argSigType=0x%0x, byrefArgSigType=0x%0x, isByRef=%d",
                                        pFEArgInfo[currArgIndex].argSigType,
                                        pFEArgInfo[currArgIndex].byrefArgSigType,
                                        isByRef));

                LOG((LF_CORDB, LL_EVERYTHING,
                     "argSigType=0x%0x, byrefArgSigType=0x%0x, isByRef=%d\n",
                     pFEArgInfo[currArgIndex].argSigType, pFEArgInfo[currArgIndex].byrefArgSigType, isByRef));

                *(SIZE_T*)pDest = *(SIZE_T*)pFEAD->argLiteralData;
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                }
#endif
            }
            else
            {
                // RAK_REG is the only valid 4 byte type on WIN32.  On WIN64, RAK_REG and RAK_FLOAT
                // can both be either 4 bytes or 8 bytes;
                _ASSERTE((pFEAD->argHome.kind == RAK_REG)
                         WIN64_ONLY(|| (pFEAD->argHome.kind == RAK_FLOAT)));

                CorDebugRegister regNum = GetArgAddrFromReg(pFEAD);

                // Simply grab the value out of the proper register.
                SIZE_T v = GetRegisterValue(pDE, regNum, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);
                *pDest = v;
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                }
#endif
            }
            break;

        default:
            // 4-byte, 2-byte, or 1-byte values

            if (pFEAD->argAddr != NULL)
            {
                if (!isByRef)
                {
                    if (pFEAD->argIsHandleValue)
                    {
                        OBJECTHANDLE oh = (OBJECTHANDLE)(pFEAD->argAddr);
                        *pDest = (INT64)(size_t)oh;
                    }
                    else
                    {
                        GetAndSetLiteralValue(pDest, pFEArgInfo[currArgIndex].argSigType,
                                              pFEAD->argAddr, pFEAD->argElementType);
                    }
#ifdef _DEBUG
                    if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                    {
                        pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                    }
#endif
                }
                else
                {
                    if (pFEAD->argIsHandleValue)
                    {
                        *pDest = (INT64)(size_t)(pFEAD->argAddr);
                    }
                    else
                    {
                        // We have to make sure we only grab the correct size of memory from the source.  On IA64, we
                        // have to make sure we don't cause misaligned data exceptions as well.  Then we put the value
                        // into the pBufferArray.  The reason is that we may be passing in some values by ref to a
                        // function that's expecting something of a bigger size.  Thus, if we don't do this, then we'll
                        // be bashing memory right next to the source value as the function being called acts upon some
                        // bigger value.
                        GetAndSetLiteralValue(pDest, pFEArgInfo[currArgIndex].byrefArgSigType,
                                              pFEAD->argAddr, pFEAD->argElementType);
                    }
#ifdef _DEBUG
                    if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                    {
                        pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                    }
#endif
                }
            }
            else if (pFEAD->argIsLiteral)
            {
                _ASSERTE(sizeof(pFEAD->argLiteralData) >= sizeof(INT32));

                // The called function may expect a larger/smaller value than the literal value,
                // so we convert the value to the right type.

                CONSISTENCY_CHECK_MSGF(
                    ((pFEArgInfo[currArgIndex].argSigType>=ELEMENT_TYPE_BOOLEAN) && (pFEArgInfo[currArgIndex].argSigType<=ELEMENT_TYPE_R8)) ||
                    (pFEArgInfo[currArgIndex].argSigType == ELEMENT_TYPE_PTR) ||
                    (pFEArgInfo[currArgIndex].argSigType == ELEMENT_TYPE_I) ||
                    (pFEArgInfo[currArgIndex].argSigType == ELEMENT_TYPE_U) ||
                    (isByRef && ((pFEArgInfo[currArgIndex].byrefArgSigType>=ELEMENT_TYPE_BOOLEAN) && (pFEArgInfo[currArgIndex].byrefArgSigType<=ELEMENT_TYPE_R8))),
                    ("argSigType=0x%0x, byrefArgSigType=0x%0x, isByRef=%d", pFEArgInfo[currArgIndex].argSigType, pFEArgInfo[currArgIndex].byrefArgSigType, isByRef));

                LOG((LF_CORDB, LL_EVERYTHING,
                     "argSigType=0x%0x, byrefArgSigType=0x%0x, isByRef=%d\n",
                     pFEArgInfo[currArgIndex].argSigType,
                     pFEArgInfo[currArgIndex].byrefArgSigType,
                     isByRef));

                CorElementType relevantType = (isByRef ? pFEArgInfo[currArgIndex].byrefArgSigType : pFEArgInfo[currArgIndex].argSigType);

                GetAndSetLiteralValue(pDest, relevantType, pFEAD->argLiteralData, pFEAD->argElementType);
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                }
#endif
            }
            else
            {
                // RAK_REG is the only valid 4 byte type on WIN32.  On WIN64, RAK_REG and RAK_FLOAT
                // can both be either 4 bytes or 8 bytes;
                _ASSERTE((pFEAD->argHome.kind == RAK_REG)
                         WIN64_ONLY(|| (pFEAD->argHome.kind == RAK_FLOAT)));

                CorDebugRegister regNum = GetArgAddrFromReg(pFEAD);

                // Simply grab the value out of the proper register.
                SIZE_T v = GetRegisterValue(pDE, regNum, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);
                *pDest = v;
#ifdef _DEBUG
                if (currArgIndex < MAX_DATA_LOCATIONS_TRACKED)
                {
                    pDataLocationArray[currArgIndex] |= DL_BufferForArgsArray;
                }
#endif
            }
        }
    }
}


/*
 * PackArgumentArray
 *
 * This routine fills a given array with the correct values for passing to a managed function.
 * It uses various component arrays that contain information to correctly create the argument array.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    argData - Array of information about the arguments.
 *    pUnboxedMD - MethodDesc of the function to call, after unboxing.
 *    RetValueType - Type Handle of the return value of the managed function we will call.
 *    pFEArgInfo - An array of structs to hold the argument information.  Must have be previously filled in.
 *    pObjectRefArray - An array that contains any object refs.  It was built previously.
 *    pMaybeInteriorPtrArray - An array that contains values that may be pointers to
 *         the interior of a managed object.
 *    pBufferForArgsArray - An array that contains values that need writable memory space
 *         for passing ByRef.
 *    newObj - Pre-allocated object for a 'new' call.
 *    pArguments - This array is packed from the above arrays.
 *    ppRetValue - Return value buffer if fRetValueArg is TRUE
 *
 * Returns:
 *    None.
 *
 */
void PackArgumentArray(DebuggerEval *pDE,
                       DebuggerIPCE_FuncEvalArgData *argData,
                       FuncEvalArgInfo *pFEArgInfo,
                       MethodDesc *pUnboxedMD,
                       TypeHandle RetValueType,
                       OBJECTREF *pObjectRefArray,
                       void **pMaybeInteriorPtrArray,
                       INT64 *pBufferForArgsArray,
                       ValueClassInfo ** ppProtectedValueClasses,
                       OBJECTREF newObj,
                       BOOL fRetValueArg,
                       ARG_SLOT *pArguments,
                       PVOID * ppRetValue
                       DEBUG_ARG(DataLocation pDataLocationArray[])
                      )
{
    WRAPPER_NO_CONTRACT;

    GCX_FORBID();

    unsigned currArgIndex = 0;
    unsigned currArgSlot = 0;


    //
    // THIS POINTER (if any)
    // For non-static methods, or when returning a new object,
    // the first arg in the array is 'this' or the new object.
    //
    if (pDE->m_evalType == DB_IPCE_FET_NEW_OBJECT)
    {
        //
        // If this is a new object op, then we need to fill in the 0'th
        // arg slot with the 'this' ptr.
        //
        pArguments[0] = ObjToArgSlot(newObj);

        //
        // If we are invoking a function on a value class, but we have a boxed value class for 'this',
        // then go ahead and unbox it and leave a ref to the value class on the stack as 'this'.
        //
        if (pDE->m_md->GetMethodTable()->IsValueType())
        {
            _ASSERTE(newObj->GetMethodTable()->IsValueType());

            // This is one of those places we use true boxed nullables
            _ASSERTE(!Nullable::IsNullableType(pDE->m_md->GetMethodTable()) ||
                     newObj->GetMethodTable() == pDE->m_md->GetMethodTable());
            void *pData = newObj->GetData();
            pArguments[0] = PtrToArgSlot(pData);
        }

        //
        // Bump up the arg slot
        //
        currArgSlot++;
    }
    else if (!pDE->m_md->IsStatic())
    {
        //
        // Place 'this' first in the array for non-static methods.
        //
        TypeHandle dummyTH;
        bool isByRef = false;
        bool fNeedBoxOrUnbox = false;

        // We had better have an object for a 'this' argument!
        CorElementType et = argData[0].argElementType;

        if (!(IsElementTypeSpecial(et) ||
              et == ELEMENT_TYPE_VALUETYPE))
        {
            COMPlusThrow(kArgumentOutOfRangeException, W("ArgumentOutOfRange_Enum"));
        }

        LOG((LF_CORDB, LL_EVERYTHING, "this: currArgSlot=%d, currArgIndex=%d et=0x%x\n", currArgSlot, currArgIndex, et));

        if (pDE->m_md->GetMethodTable()->IsValueType())
        {
            // For value classes, the 'this' parameter is always passed by reference.
            // However do not unbox if we are calling an unboxing stub.
            if (pDE->m_md == pUnboxedMD)
            {
                // pDE->m_md is expecting an unboxed this pointer. Then we will unbox it.
                isByRef = true;

                // Remember if we need to unbox this parameter, though.
                if ((et == ELEMENT_TYPE_CLASS) || (et == ELEMENT_TYPE_OBJECT))
                {
                    fNeedBoxOrUnbox = true;
                }
            }
        }
        else if (et == ELEMENT_TYPE_VALUETYPE)
        {
            // When the method that we invoking is defined on non value type and we receive the ValueType as input,
            // we are calling methods on System.Object. In this case, we need to box the input ValueType.
            fNeedBoxOrUnbox = true;
        }

        GetFuncEvalArgValue(pDE,
                            &argData[currArgIndex],
                            isByRef,
                            fNeedBoxOrUnbox,
                            dummyTH,
                            ELEMENT_TYPE_CLASS,
                            pDE->m_md->GetMethodTable(),
                            &(pArguments[currArgSlot]),
                            &(pMaybeInteriorPtrArray[currArgIndex]),
                            &(pObjectRefArray[currArgIndex]),
                            &(pBufferForArgsArray[currArgIndex]),
                            NULL,
                            ELEMENT_TYPE_OBJECT
                            DEBUG_ARG((currArgIndex < MAX_DATA_LOCATIONS_TRACKED) ? pDataLocationArray[currArgIndex]
                                                                                  : DL_All)
                            );

        LOG((LF_CORDB, LL_EVERYTHING, "this = 0x%08x\n", ArgSlotToPtr(pArguments[currArgSlot])));

        // We need to check 'this' for a null ref ourselves... NOTE: only do this if we put an object reference on
        // the stack. If we put a byref for a value type, then we don't need to do this!
        if (!isByRef)
        {
            // The this pointer is not a unboxed value type.

            ARG_SLOT oi1 = pArguments[currArgSlot];
            OBJECTREF o1 = ArgSlotToObj(oi1);

            if (FAILED(ValidateObject(OBJECTREFToObject(o1))))
            {
                COMPlusThrow(kArgumentException, W("Argument_BadObjRef"));
            }

            if (OBJECTREFToObject(o1) == NULL)
            {
                COMPlusThrow(kNullReferenceException, W("NullReference_This"));
            }

            // For interface method, we have already done the check early on.
            if (!pDE->m_md->IsInterface())
            {
                // We also need to make sure that the method that we are invoking is either defined on this object or the direct/indirect
                // base objects.
                Object  *objPtr = OBJECTREFToObject(o1);
                MethodTable *pMT = objPtr->GetMethodTable();
                // <TODO> Do this check in the following cases as well... </TODO>
                if (!pMT->IsArray() 
                    && !pDE->m_md->IsSharedByGenericInstantiations())
                {
                    TypeHandle thFrom = TypeHandle(pMT);
                    TypeHandle thTarget = TypeHandle(pDE->m_md->GetMethodTable());
                    //<TODO> What about MaybeCast?</TODO>
                    if (thFrom.CanCastToNoGC(thTarget) == TypeHandle::CannotCast)
                    {
                        COMPlusThrow(kArgumentException, W("Argument_CORDBBadMethod"));
                    }
                }
            }
        }

        //
        // Increment up both arrays.
        //
        currArgSlot++;
        currArgIndex++;
    }

    // Special handling for functions that return value classes.
    if (fRetValueArg)
    {
        LOG((LF_CORDB, LL_EVERYTHING, "retBuff: currArgSlot=%d, currArgIndex=%d\n", currArgSlot, currArgIndex));

        //
        // Allocate buffer for return value and GC protect it in case it contains object references
        //
        unsigned size = RetValueType.GetMethodTable()->GetNumInstanceFieldBytes();

#ifdef FEATURE_HFA
        // The buffer for HFAs has to be always ENREGISTERED_RETURNTYPE_MAXSIZE
        size = max(size, ENREGISTERED_RETURNTYPE_MAXSIZE);
#endif

        BYTE * pTemp = new (interopsafe) BYTE[ALIGN_UP(sizeof(ValueClassInfo), 8) + size];

        ValueClassInfo * pValueClassInfo = (ValueClassInfo *)pTemp;
        LPVOID pData = pTemp + ALIGN_UP(sizeof(ValueClassInfo), 8);

        memset(pData, 0, size);

        pValueClassInfo->pData = pData;
        pValueClassInfo->pMT = RetValueType.GetMethodTable();

        pValueClassInfo->pNext = *ppProtectedValueClasses;
        *ppProtectedValueClasses = pValueClassInfo;

        pArguments[currArgSlot++] = PtrToArgSlot(pData);
        *ppRetValue = pData;
    }

    // REAL ARGUMENTS (if any)
    // Now do the remaining args
    for ( ; currArgIndex < pDE->m_argCount; currArgSlot++, currArgIndex++)
    {
        DebuggerIPCE_FuncEvalArgData *pFEAD = &argData[currArgIndex];

        LOG((LF_CORDB, LL_EVERYTHING, "currArgSlot=%d, currArgIndex=%d\n",
             currArgSlot,
             currArgIndex));
        LOG((LF_CORDB, LL_EVERYTHING,
            "\t: argSigType=0x%x, byrefArgSigType=0x%0x, inType=0x%0x\n",
             pFEArgInfo[currArgIndex].argSigType,
             pFEArgInfo[currArgIndex].byrefArgSigType,
             pFEAD->argElementType));


        GetFuncEvalArgValue(pDE,
                            pFEAD,
                            pFEArgInfo[currArgIndex].argSigType == ELEMENT_TYPE_BYREF,
                            pFEArgInfo[currArgIndex].fNeedBoxOrUnbox,
                            pFEArgInfo[currArgIndex].sigTypeHandle,
                            pFEArgInfo[currArgIndex].byrefArgSigType,
                            pFEArgInfo[currArgIndex].byrefArgTypeHandle,
                            &(pArguments[currArgSlot]),
                            &(pMaybeInteriorPtrArray[currArgIndex]),
                            &(pObjectRefArray[currArgIndex]),
                            &(pBufferForArgsArray[currArgIndex]),
                            ppProtectedValueClasses,
                            pFEArgInfo[currArgIndex].argSigType
                            DEBUG_ARG((currArgIndex < MAX_DATA_LOCATIONS_TRACKED) ? pDataLocationArray[currArgIndex]
                                                                                  : DL_All)
                           );
    }
}

/*
 * UnpackFuncEvalResult
 *
 * This routine takes the resulting object of a func-eval, and does any copying, boxing, unboxing, necessary.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    newObj - Pre-allocated object for NEW_OBJ func-evals.
 *    retObject - Pre-allocated object to be filled in with the info in pRetBuff.
 *    RetValueType - The return type of the function called.
 *    pRetBuff - The raw bytes returned by the func-eval call when there is a return buffer parameter.
 *
 *
 * Returns:
 *    None.
 *
 */
void UnpackFuncEvalResult(DebuggerEval *pDE,
                          OBJECTREF newObj,
                          OBJECTREF retObject,
                          TypeHandle RetValueType,
                          void *pRetBuff
                          )
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        GC_NOTRIGGER;
    }
    CONTRACTL_END;


    // Ah, but if this was a new object op, then the result is really
    // the object we allocated above...
    if (pDE->m_evalType == DB_IPCE_FET_NEW_OBJECT)
    {
        // We purposely do not morph nullables to be boxed Ts here because debugger EE's otherwise
        // have no way of creating true nullables that they need for their own purposes. 
        pDE->m_result[0] = ObjToArgSlot(newObj);
        pDE->m_retValueBoxing = Debugger::AllBoxed;
    }
    else if (!RetValueType.IsNull())
    {
        LOG((LF_CORDB, LL_EVERYTHING, "FuncEval call is saving a boxed VC return value.\n"));

        //
        // We pre-created it above
        //
        _ASSERTE(retObject != NULL);

        // This is one of those places we use true boxed nullables
        _ASSERTE(!Nullable::IsNullableType(RetValueType)||
                 retObject->GetMethodTable() == RetValueType.GetMethodTable());

        if (pRetBuff != NULL)
        {
            // box the object
            CopyValueClass(retObject->GetData(),
                           pRetBuff,
                           RetValueType.GetMethodTable());
        }
        else
        {
            // box the primitive returned, retObject is a true nullable for nullabes, It will be Normalized later
            CopyValueClass(retObject->GetData(),
                           pDE->m_result,
                           RetValueType.GetMethodTable());
        }

        pDE->m_result[0] = ObjToArgSlot(retObject);
        pDE->m_retValueBoxing = Debugger::AllBoxed;
    }
    else
    {
        //
        // Other FuncEvals return primitives as unboxed.
        //
        pDE->m_retValueBoxing = Debugger::OnlyPrimitivesUnboxed;
    }

    LOG((LF_CORDB, LL_INFO10000, "FuncEval call has saved the return value.\n"));
    // No exception, so it worked as far as we're concerned.
    pDE->m_successful = true;

    // If the result is an object, then place the object
    // reference into a strong handle and place the handle into the
    // pDE to protect the result from a collection.
    CorElementType retClassET = pDE->m_resultType.GetSignatureCorElementType();

    if ((pDE->m_retValueBoxing == Debugger::AllBoxed) ||
        !RetValueType.IsNull() ||
        IsElementTypeSpecial(retClassET))
    {
        LOG((LF_CORDB, LL_EVERYTHING, "Creating strong handle for boxed DoNormalFuncEval result.\n"));
        OBJECTHANDLE oh = pDE->m_thread->GetDomain()->CreateStrongHandle(ArgSlotToObj(pDE->m_result[0]));
        pDE->m_result[0] = (INT64)(LONG_PTR)oh;
        pDE->m_vmObjectHandle = VMPTR_OBJECTHANDLE::MakePtr(oh);
    }
}

/*
 * UnpackFuncEvalArguments
 *
 * This routine takes the resulting object of a func-eval, and does any copying, boxing, unboxing, necessary.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    newObj - Pre-allocated object for NEW_OBJ func-evals.
 *    retObject - Pre-allocated object to be filled in with the info in pSource.
 *    RetValueType - The return type of the function called.
 *    pSource - The raw bytes returned by the func-eval call when there is a hidden parameter.
 *
 *
 * Returns:
 *    None.
 *
 */
void UnpackFuncEvalArguments(DebuggerEval *pDE,
                             DebuggerIPCE_FuncEvalArgData *argData,
                             MetaSig mSig,
                             BOOL staticMethod,
                             OBJECTREF *pObjectRefArray,
                             void **pMaybeInteriorPtrArray,
                             void **pByRefMaybeInteriorPtrArray,
                             INT64 *pBufferForArgsArray
                            )
{
    WRAPPER_NO_CONTRACT;

    // Update any enregistered byrefs with their new values from the
    // proper byref temporary array.
    if (pDE->m_argCount > 0)
    {
        mSig.Reset();

        unsigned currArgIndex = 0;

        if ((pDE->m_evalType == DB_IPCE_FET_NORMAL) && !pDE->m_md->IsStatic())
        {
            //
            // Skip over the 'this' arg, since this function is not supposed to mess with it.
            //
            currArgIndex++;
        }

        for (; currArgIndex < pDE->m_argCount; currArgIndex++)
        {
            CorElementType argSigType = mSig.NextArgNormalized();

            LOG((LF_CORDB, LL_EVERYTHING, "currArgIndex=%d argSigType=0x%x\n", currArgIndex, argSigType));

            _ASSERTE(argSigType != ELEMENT_TYPE_END);

            if (argSigType == ELEMENT_TYPE_BYREF)
            {
                TypeHandle byrefClass = TypeHandle();
                CorElementType byrefArgSigType = mSig.GetByRefType(&byrefClass);

                // If these are the true boxed nullables we created in BoxFuncEvalArguments, convert them back
                pObjectRefArray[currArgIndex] = Nullable::NormalizeBox(pObjectRefArray[currArgIndex]);

                LOG((LF_CORDB, LL_EVERYTHING, "DoNormalFuncEval: Updating enregistered byref...\n"));
                SetFuncEvalByRefArgValue(pDE,
                                         &argData[currArgIndex],
                                         byrefArgSigType,
                                         pBufferForArgsArray[currArgIndex],
                                         pMaybeInteriorPtrArray[currArgIndex],
                                         pByRefMaybeInteriorPtrArray[currArgIndex],
                                         pObjectRefArray[currArgIndex]
                                        );
            }
        }
    }
}


/*
 * FuncEvalWrapper
 *
 * Helper function for func-eval. We have to split it out so that we can put a __try / __finally in to
 * notify on a Catch-Handler found.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    pArguments - created stack to pass for the call.
 *    pCatcherStackAddr - stack address to report as the Catch Handler Found location.
 *
 * Returns:
 *    None.
 *
 */
void FuncEvalWrapper(MethodDescCallSite* pMDCS, DebuggerEval *pDE, const ARG_SLOT *pArguments, BYTE *pCatcherStackAddr)
{
    struct Param : NotifyOfCHFFilterWrapperParam
    {
        MethodDescCallSite* pMDCS;
        DebuggerEval *pDE;
        const ARG_SLOT *pArguments;
    }; 
    
    Param param;
    param.pFrame = pCatcherStackAddr; // Inherited from NotifyOfCHFFilterWrapperParam
    param.pMDCS = pMDCS;
    param.pDE = pDE;
    param.pArguments = pArguments;

    PAL_TRY(Param *, pParam, &param)
    {
        pParam->pMDCS->CallWithValueTypes_RetArgSlot(pParam->pArguments, pParam->pDE->m_result, sizeof(pParam->pDE->m_result));
    }
    PAL_EXCEPT_FILTER(NotifyOfCHFFilterWrapper)
    {
        // Should never reach here b/c handler should always continue search.
        _ASSERTE(false);
    }
    PAL_ENDTRY
}

/*
 * RecordFuncEvalException
 *
 * Helper function records the details of an exception that occurred during a FuncEval
 * Note that this should be called from within the target domain of the FuncEval.
 *
 * Parameters:
 *   pDE - pointer to the DebuggerEval object being processed
 *   ppException - the Exception object that was thrown
 *
 * Returns:
 *    None.
 */
static void RecordFuncEvalException(DebuggerEval *pDE,
                             OBJECTREF ppException )
{
    CONTRACTL
    {
        THROWS;         // CreateStrongHandle could throw OOM
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // We got an exception. Make the exception into our result.
    pDE->m_successful = false;
    LOG((LF_CORDB, LL_EVERYTHING, "D::FEHW - Exception during funceval.\n"));

    //
    // Special handling for thread abort exceptions. We need to explicitly reset the
    // abort request on the EE thread, then make sure to place this thread on a thunk
    // that will re-raise the exception when we continue the process. Note: we still
    // pass this thread abort exception up as the result of the eval.
    //
    if (IsExceptionOfType(kThreadAbortException, &ppException))
    {
        if (pDE->m_aborting != DebuggerEval::FE_ABORT_NONE)
        {
            //
            // Reset the abort request.
            //
            pDE->m_thread->UserResetAbort(Thread::TAR_FuncEval);

            //
            // This is the abort we sent down.
            //
            memset(pDE->m_result, 0, sizeof(pDE->m_result));
            pDE->m_resultType = TypeHandle();
            pDE->m_aborted = true;
            pDE->m_retValueBoxing = Debugger::NoValueTypeBoxing;

            LOG((LF_CORDB, LL_EVERYTHING, "D::FEHW - funceval abort exception.\n"));
        }
        else
        {
            //
            // This must have come from somewhere else, remember that we need to
            // rethrow this.
            //
            pDE->m_rethrowAbortException = true;

            //
            // The result is the exception object.
            //
            pDE->m_result[0] = ObjToArgSlot(ppException);

            pDE->m_resultType = ppException->GetTypeHandle();
            OBJECTHANDLE oh = pDE->m_thread->GetDomain()->CreateStrongHandle(ArgSlotToObj(pDE->m_result[0]));
            pDE->m_result[0] = (ARG_SLOT)PTR_TO_CORDB_ADDRESS(oh);
            pDE->m_vmObjectHandle = VMPTR_OBJECTHANDLE::MakePtr(oh);
            pDE->m_retValueBoxing = Debugger::NoValueTypeBoxing;

            LOG((LF_CORDB, LL_EVERYTHING, "D::FEHW - Non-FE abort thread abort..\n"));
        }
    }
    else
    {
        //
        // The result is the exception object.
        //
        pDE->m_result[0] = ObjToArgSlot(ppException);

        pDE->m_resultType = ppException->GetTypeHandle();
        OBJECTHANDLE oh = pDE->m_thread->GetDomain()->CreateStrongHandle(ArgSlotToObj(pDE->m_result[0]));
        pDE->m_result[0] = (ARG_SLOT)(LONG_PTR)oh;
        pDE->m_vmObjectHandle = VMPTR_OBJECTHANDLE::MakePtr(oh);

        pDE->m_retValueBoxing = Debugger::NoValueTypeBoxing;

        LOG((LF_CORDB, LL_EVERYTHING, "D::FEHW - Exception for the user.\n"));
    }
}


/*
 * DoNormalFuncEval
 *
 * Does the main body of work (steps 1c onward) for the normal func-eval algorithm detailed at the
 * top of this file. The args have already been GC protected and we've transitioned into the appropriate
 * domain (steps 1a & 1b).  This has to be a seperate function from GCProtectArgsAndDoNormalFuncEval 
 * because otherwise we can't reliably find the right GCFrames to pop when unwinding the stack due to 
 * an exception on 64-bit platforms (we have some GCFrames outside of the TRY, and some inside, 
 * and they won't necesarily be layed out sequentially on the stack if they are all in the same function).
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    pCatcherStackAddr - stack address to report as the Catch Handler Found location.
 *    pObjectRefArray - An array to hold object ref args. This array is protected from GC's.
 *    pMaybeInteriorPtrArray - An array to hold values that may be pointers into a managed object.  
 *           This array is protected from GCs.
 *    pByRefMaybeInteriorPtrArray - An array to hold values that may be pointers into a managed 
 *           object.  This array is protected from GCs.  This array protects the address of the arguments 
 *           while the pMaybeInteriorPtrArray protects the value of the arguments.  We need to do this 
 *           because of by ref arguments.
 *    pBufferForArgsArray - a buffer of temporary scratch space for things that do not need to be 
 *           protected, or are protected for free (e.g. Handles).
 *    pDataLocationArray - an array of tracking data for debug sanity checks
 *
 * Returns:
 *    None.
 */
static void DoNormalFuncEval( DebuggerEval *pDE,
            BYTE *pCatcherStackAddr,
            OBJECTREF *pObjectRefArray,
            void **pMaybeInteriorPtrArray,
            void **pByRefMaybeInteriorPtrArray,
            INT64 *pBufferForArgsArray,
            ValueClassInfo ** ppProtectedValueClasses
            DEBUG_ARG(DataLocation pDataLocationArray[])
          )
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    //
    // Now that all the args are protected, we can go back and deal with generic args and resolving
    // all their information.
    //
    ResolveFuncEvalGenericArgInfo(pDE);

    //
    // Grab the signature of the method we're working on and do some error checking.
    // Note that if this instantiated generic code, then this will
    // correctly give as an instantiated view of the signature that we can iterate without
    // worrying about generic items in the signature.
    //
    MetaSig mSig(pDE->m_md);

    BYTE callingconvention = mSig.GetCallingConvention();
    if (!isCallConv(callingconvention, IMAGE_CEE_CS_CALLCONV_DEFAULT))
    {
        // We don't support calling vararg!
        COMPlusThrow(kArgumentException, W("Argument_CORDBBadVarArgCallConv"));
    }

    //
    // We'll need to know if this is a static method or not.
    //
    BOOL staticMethod = pDE->m_md->IsStatic();

    _ASSERTE((pDE->m_evalType == DB_IPCE_FET_NORMAL) || !staticMethod);

    //
    // Do Step 1c - Pre-allocate space for new objects.
    //
    OBJECTREF newObj = NULL;
    GCPROTECT_BEGIN(newObj);

    SIZE_T allocArgCnt = 0;

    if (pDE->m_evalType == DB_IPCE_FET_NEW_OBJECT)
    {
        ValidateFuncEvalReturnType(DB_IPCE_FET_NEW_OBJECT, pDE->m_resultType.GetMethodTable());
        pDE->m_resultType.GetMethodTable()->EnsureInstanceActive();
        newObj = AllocateObject(pDE->m_resultType.GetMethodTable());

        //
        // Note: we account for an extra argument in the count passed
        // in. We use this to increase the space allocated for args,
        // and we use it to control the number of args copied into
        // those arrays below. Note: m_argCount already includes space
        // for this.
        //
        allocArgCnt = pDE->m_argCount + 1;
    }
    else
    {
        allocArgCnt = pDE->m_argCount;
    }

    //
    // Validate the argument count with mSig.
    //
    if (allocArgCnt != (mSig.NumFixedArgs() + (staticMethod ? 0 : 1)))
    {
        COMPlusThrow(kTargetParameterCountException, W("Arg_ParmCnt"));
    }

    //
    // Do Step 1d - Gather information about the method that will be called.
    //
    // An array to hold information about the parameters to be passed.  This is
    // all the information we need to gather before entering the GCX_FORBID area.
    //
    DebuggerIPCE_FuncEvalArgData *argData = pDE->GetArgData();

    MethodDesc *pUnboxedMD = pDE->m_md;
    BOOL fHasRetBuffArg;
    BOOL fHasNonStdByValReturn;
    TypeHandle RetValueType;

    BoxFuncEvalThisParameter(pDE,
                             argData,
                             pMaybeInteriorPtrArray,
                             pObjectRefArray
                             DEBUG_ARG(pDataLocationArray)
                             );

    GatherFuncEvalMethodInfo(pDE,
                             mSig,
                             argData,
                             &pUnboxedMD,
                             pObjectRefArray,
                             pBufferForArgsArray,
                             &fHasRetBuffArg,
                             &fHasNonStdByValReturn,
                             &RetValueType
                             DEBUG_ARG(pDataLocationArray)
                            );

    //
    // Do Step 1e - Gather info from runtime about args (may trigger a GC).
    //
    SIZE_T cbAllocSize;
    if (!(ClrSafeInt<SIZE_T>::multiply(pDE->m_argCount, sizeof(FuncEvalArgInfo), cbAllocSize)) ||
        (cbAllocSize != (size_t)(cbAllocSize)))
    {
        ThrowHR(COR_E_OVERFLOW);
    }
    FuncEvalArgInfo * pFEArgInfo = (FuncEvalArgInfo *)_alloca(cbAllocSize);
    memset(pFEArgInfo, 0, cbAllocSize);

    GatherFuncEvalArgInfo(pDE, mSig, argData, pFEArgInfo);

    //
    // Do Step 1f - Box or unbox arguments one at a time, placing newly boxed items into
    // pObjectRefArray immediately after creating them.
    //
    BoxFuncEvalArguments(pDE,
                         argData,
                         pFEArgInfo,
                         pMaybeInteriorPtrArray,
                         pObjectRefArray
                         DEBUG_ARG(pDataLocationArray)
                         );

#ifdef _DEBUG
    if (!RetValueType.IsNull())
    {
        _ASSERTE(RetValueType.IsValueType());
    }
#endif

    //
    // Do Step 1g - Pre-allocate any return value object.
    //
    OBJECTREF retObject = NULL;
    GCPROTECT_BEGIN(retObject);

    if ((pDE->m_evalType != DB_IPCE_FET_NEW_OBJECT) && !RetValueType.IsNull())
    {
        ValidateFuncEvalReturnType(pDE->m_evalType, RetValueType.GetMethodTable());
        RetValueType.GetMethodTable()->EnsureInstanceActive();
        retObject = AllocateObject(RetValueType.GetMethodTable());
    }

    //
    // Do Step 1h - Copy into scratch buffer all enregistered arguments, and
    // ByRef literals.
    //
    CopyArgsToBuffer(pDE,
                     argData,
                     pFEArgInfo,
                     pBufferForArgsArray
                     DEBUG_ARG(pDataLocationArray)
                    );

    //
    // We presume that the function has a return buffer.  This assumption gets squeezed out
    // when we pack the argument array.
    //
    allocArgCnt++;

    LOG((LF_CORDB, LL_EVERYTHING,
         "Func eval for %s::%s: allocArgCnt=%d\n",
         pDE->m_md->m_pszDebugClassName,
         pDE->m_md->m_pszDebugMethodName,
         allocArgCnt));

    MethodDescCallSite funcToEval(pDE->m_md, pDE->m_targetCodeAddr);

    //
    // Do Step 1i - Create and pack argument array for managed function call.
    //
    // Allocate space for argument stack
    //
    if ((!ClrSafeInt<SIZE_T>::multiply(allocArgCnt, sizeof(ARG_SLOT), cbAllocSize)) ||
        (cbAllocSize != (size_t)(cbAllocSize)))
    {
        ThrowHR(COR_E_OVERFLOW);
    }
    ARG_SLOT * pArguments = (ARG_SLOT *)_alloca(cbAllocSize);
    memset(pArguments, 0, cbAllocSize);

    LPVOID pRetBuff = NULL;

    PackArgumentArray(pDE,
                      argData,
                      pFEArgInfo,
                      pUnboxedMD,
                      RetValueType,
                      pObjectRefArray,
                      pMaybeInteriorPtrArray,
                      pBufferForArgsArray,
                      ppProtectedValueClasses,
                      newObj,
#ifdef FEATURE_HFA
                      fHasRetBuffArg || fHasNonStdByValReturn,
#else
                      fHasRetBuffArg,
#endif
                      pArguments,
                      &pRetBuff
                      DEBUG_ARG(pDataLocationArray)
                     );

    //
    //
    // Do Step 2 - Make the call!
    //
    //
    FuncEvalWrapper(&funcToEval, pDE, pArguments, pCatcherStackAddr);
    {

        // We have now entered the zone where taking a GC is fatal until we get the
        // return value all fixed up.
        //
        GCX_FORBID();


        //
        //
        // Do Step 3 - Unpack results and update ByRef arguments.
        //
        //
        //
        LOG((LF_CORDB, LL_EVERYTHING, "FuncEval call has returned\n"));


        // GC still can't happen until we get our return value out half way through the unpack function

        UnpackFuncEvalResult(pDE,
                             newObj,
                             retObject,
                             RetValueType,
                             pRetBuff
                            );
    }

    UnpackFuncEvalArguments(pDE,
                            argData,
                            mSig,
                            staticMethod,
                            pObjectRefArray,
                            pMaybeInteriorPtrArray,
                            pByRefMaybeInteriorPtrArray,
                            pBufferForArgsArray
                           );

    GCPROTECT_END();    // retObject
    GCPROTECT_END();    // newObj
}

/*
 * GCProtectArgsAndDoNormalFuncEval
 *
 * This routine is the primary entrypoint for normal func-evals.  It implements the algorithm 
 * described at the top of this file, doing steps 1a and 1b itself, then calling DoNormalFuncEval
 * to do the rest.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    pCatcherStackAddr - stack address to report as the Catch Handler Found location.
 *
 * Returns:
 *    None.
 *
 */
static void GCProtectArgsAndDoNormalFuncEval(DebuggerEval *pDE,
                             BYTE *pCatcherStackAddr )
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;


    INDEBUG(DataLocation pDataLocationArray[MAX_DATA_LOCATIONS_TRACKED]);

    //
    // An array to hold object ref args. This array is protected from GC's.
    //
    SIZE_T cbAllocSize;
    if ((!ClrSafeInt<SIZE_T>::multiply(pDE->m_argCount, sizeof(OBJECTREF), cbAllocSize)) ||
        (cbAllocSize != (size_t)(cbAllocSize)))
    {
        ThrowHR(COR_E_OVERFLOW);
    }
    OBJECTREF * pObjectRefArray = (OBJECTREF*)_alloca(cbAllocSize);
    memset(pObjectRefArray, 0, cbAllocSize);
    GCPROTECT_ARRAY_BEGIN(*pObjectRefArray, pDE->m_argCount);

    //
    // An array to hold values that may be pointers into a managed object.  This array
    // is protected from GCs.
    //
    if ((!ClrSafeInt<SIZE_T>::multiply(pDE->m_argCount, sizeof(void**), cbAllocSize)) ||
        (cbAllocSize != (size_t)(cbAllocSize)))
    {
        ThrowHR(COR_E_OVERFLOW);
    }
    void ** pMaybeInteriorPtrArray = (void **)_alloca(cbAllocSize);
    memset(pMaybeInteriorPtrArray, 0, cbAllocSize);
    GCPROTECT_BEGININTERIOR_ARRAY(*pMaybeInteriorPtrArray, (UINT)(cbAllocSize/sizeof(OBJECTREF)));

    //
    // An array to hold values that may be pointers into a managed object.  This array
    // is protected from GCs.  This array protects the address of the arguments while the
    // pMaybeInteriorPtrArray protects the value of the arguments.  We need to do this because
    // of by ref arguments.
    //
    if ((!ClrSafeInt<SIZE_T>::multiply(pDE->m_argCount, sizeof(void**), cbAllocSize)) ||
        (cbAllocSize != (size_t)(cbAllocSize)))
    {
        ThrowHR(COR_E_OVERFLOW);
    }
    void ** pByRefMaybeInteriorPtrArray = (void **)_alloca(cbAllocSize);
    memset(pByRefMaybeInteriorPtrArray, 0, cbAllocSize);
    GCPROTECT_BEGININTERIOR_ARRAY(*pByRefMaybeInteriorPtrArray, (UINT)(cbAllocSize/sizeof(OBJECTREF)));

    //
    // A buffer of temporary scratch space for things that do not need to be protected, or
    // are protected for free (e.g. Handles).
    //
    if ((!ClrSafeInt<SIZE_T>::multiply(pDE->m_argCount, sizeof(INT64), cbAllocSize)) ||
        (cbAllocSize != (size_t)(cbAllocSize)))
    {
        ThrowHR(COR_E_OVERFLOW);
    }
    INT64 *pBufferForArgsArray = (INT64*)_alloca(cbAllocSize);
    memset(pBufferForArgsArray, 0, cbAllocSize);

    FrameWithCookie<ProtectValueClassFrame> protectValueClassFrame;

    //
    // Initialize our tracking array
    //
    INDEBUG(memset(pDataLocationArray, 0, sizeof(DataLocation) * (MAX_DATA_LOCATIONS_TRACKED)));

    {
        GCX_FORBID();

        //
        // Do step 1a
        //
        GCProtectAllPassedArgs(pDE,
                               pObjectRefArray,
                               pMaybeInteriorPtrArray,
                               pByRefMaybeInteriorPtrArray,
                               pBufferForArgsArray
                               DEBUG_ARG(pDataLocationArray)
                               );

    }

    //
    // Do step 1b: we can switch domains since everything is now protected.
    // Note that before this point, it's unsafe to rely on pDE->m_module since it may be
    // invalid due to an AD unload.
    // All normal func evals should have an AppDomain specified.
    //

    // Wrap everything in a EX_TRY so we catch any exceptions that could be thrown.
    // Note that we don't let any thrown exceptions cross the AppDomain boundary because we don't 
    // want them to get marshalled.
    EX_TRY
    {
        DoNormalFuncEval( 
            pDE, 
            pCatcherStackAddr,
            pObjectRefArray,
            pMaybeInteriorPtrArray,
            pByRefMaybeInteriorPtrArray,
            pBufferForArgsArray,
            protectValueClassFrame.GetValueClassInfoList()
            DEBUG_ARG(pDataLocationArray)
            );
    }
    EX_CATCH
    {
        // We got an exception. Make the exception into our result.
        OBJECTREF ppException = GET_THROWABLE();
        GCX_FORBID();
        RecordFuncEvalException( pDE, ppException);
    }
    // Note: we need to catch all exceptioins here because they all get reported as the result of
    // the funceval.  If a ThreadAbort occurred other than for a funcEval abort, we'll re-throw it manually.
    EX_END_CATCH(SwallowAllExceptions);

    protectValueClassFrame.Pop();

    CleanUpTemporaryVariables(protectValueClassFrame.GetValueClassInfoList());

    GCPROTECT_END();    // pByRefMaybeInteriorPtrArray
    GCPROTECT_END();    // pMaybeInteriorPtrArray
    GCPROTECT_END();    // pObjectRefArray
    LOG((LF_CORDB, LL_EVERYTHING, "DoNormalFuncEval: returning...\n"));
}


void FuncEvalHijackRealWorker(DebuggerEval *pDE, Thread* pThread, FuncEvalFrame* pFEFrame)
{
    BYTE * pCatcherStackAddr = (BYTE*) pFEFrame;

    // Handle normal func evals in DoNormalFuncEval
    if ((pDE->m_evalType == DB_IPCE_FET_NEW_OBJECT) || (pDE->m_evalType == DB_IPCE_FET_NORMAL))
    {
        GCProtectArgsAndDoNormalFuncEval(pDE, pCatcherStackAddr);
        LOG((LF_CORDB, LL_EVERYTHING, "DoNormalFuncEval has returned.\n"));
        return;
    }
    
    OBJECTREF newObj = NULL;
    GCPROTECT_BEGIN(newObj);

    // Wrap everything in a EX_TRY so we catch any exceptions that could be thrown.
    // Note that we don't let any thrown exceptions cross the AppDomain boundary because we don't 
    // want them to get marshalled.
    EX_TRY
    {
        DebuggerIPCE_TypeArgData *firstdata = pDE->GetTypeArgData();
        DWORD nGenericArgs = pDE->m_genericArgsCount;

        SIZE_T cbAllocSize;
        if ((!ClrSafeInt<SIZE_T>::multiply(nGenericArgs, sizeof(TypeHandle *), cbAllocSize)) ||
            (cbAllocSize != (size_t)(cbAllocSize)))
        {
            ThrowHR(COR_E_OVERFLOW);
        }
        TypeHandle *pGenericArgs = (nGenericArgs == 0) ? NULL : (TypeHandle *) _alloca(cbAllocSize);
        //
        // Snag the type arguments from the input and get the
        // method desc that corresponds to the instantiated desc.
        //
        Debugger::TypeDataWalk walk(firstdata, pDE->m_genericArgsNodeCount);
        walk.ReadTypeHandles(nGenericArgs, pGenericArgs);

        // <TODO>better error message</TODO>
        if (!walk.Finished())
            COMPlusThrow(kArgumentException, W("Argument_InvalidGenericArg"));

        switch (pDE->m_evalType)
        {
        case DB_IPCE_FET_NEW_OBJECT_NC:
            {

                // Find the class.
                TypeHandle thClass = g_pEEInterface->LoadClass(pDE->m_debuggerModule->GetRuntimeModule(),
                                                         pDE->m_classToken);

                if (thClass.IsNull())
                    COMPlusThrow(kArgumentNullException, W("ArgumentNull_Type"));

                // Apply any type arguments
                TypeHandle th =
                    (nGenericArgs == 0)
                    ? thClass
                    : g_pEEInterface->LoadInstantiation(pDE->m_debuggerModule->GetRuntimeModule(),
                                                         pDE->m_classToken, nGenericArgs, pGenericArgs);

                if (th.IsNull() || th.ContainsGenericVariables())
                    COMPlusThrow(kArgumentException, W("Argument_InvalidGenericArg"));

                // Run the Class Init for this type, if necessary.
                MethodTable * pOwningMT = th.GetMethodTable();
                pOwningMT->EnsureInstanceActive();
                pOwningMT->CheckRunClassInitThrowing();

                // Create a new instance of the class

                ValidateFuncEvalReturnType(DB_IPCE_FET_NEW_OBJECT_NC, th.GetMethodTable());

                newObj = AllocateObject(th.GetMethodTable());

                // No exception, so it worked.
                pDE->m_successful = true;

                // So is the result type.
                pDE->m_resultType = th;

                //
                // Box up all returned objects
                //
                pDE->m_retValueBoxing = Debugger::AllBoxed;

                // Make a strong handle for the result.
                OBJECTHANDLE oh = pDE->m_thread->GetDomain()->CreateStrongHandle(newObj);
                pDE->m_result[0] = (ARG_SLOT)(LONG_PTR)oh;
                pDE->m_vmObjectHandle = VMPTR_OBJECTHANDLE::MakePtr(oh);

                break;
            }

        case DB_IPCE_FET_NEW_STRING:
            {
                // Create the string. m_argData is not necessarily null terminated...
                // The numeration parameter represents the string length, not the buffer size, but
                // we have passed the buffer size across to copy our data properly, so must divide back out.
                // NewString will return NULL if pass null, but want an empty string in that case, so
                // just create an EmptyString explicitly.
                if ((pDE->m_argData == NULL) || (pDE->m_stringSize == 0))
                {
                    newObj = StringObject::GetEmptyString();
                }
                else
                {
                    newObj = StringObject::NewString(pDE->GetNewStringArgData(), (int)(pDE->m_stringSize/sizeof(WCHAR)));
                }

                // No exception, so it worked.
                pDE->m_successful = true;

                // Result type is, of course, a string.
                pDE->m_resultType = newObj->GetTypeHandle();

                // Place the result in a strong handle to protect it from a collection.
                OBJECTHANDLE oh = pDE->m_thread->GetDomain()->CreateStrongHandle(newObj);
                pDE->m_result[0] = (ARG_SLOT)(LONG_PTR)oh;
                pDE->m_vmObjectHandle = VMPTR_OBJECTHANDLE::MakePtr(oh);

                break;
            }

        case DB_IPCE_FET_NEW_ARRAY:
            {
                // <TODO>@todo: We're only gonna handle SD arrays for right now.</TODO>
                if (pDE->m_arrayRank > 1)
                    COMPlusThrow(kRankException, W("Rank_MultiDimNotSupported"));

                // Grab the elementType from the arg/data area.
                _ASSERTE(nGenericArgs == 1);
                TypeHandle th = pGenericArgs[0];

                CorElementType et = th.GetSignatureCorElementType();
                // Gotta be a primitive, class, or System.Object.
                if (((et < ELEMENT_TYPE_BOOLEAN) || (et > ELEMENT_TYPE_R8)) &&
                    !IsElementTypeSpecial(et))
                {
                    COMPlusThrow(kArgumentOutOfRangeException, W("ArgumentOutOfRange_Enum"));
                }

                // Grab the dims from the arg/data area.  These come after the type arguments.
                SIZE_T *dims;
                dims = (SIZE_T*) (firstdata + pDE->m_genericArgsNodeCount);

                if (IsElementTypeSpecial(et))
                {
                    newObj = AllocateObjectArray((DWORD)dims[0], th);
                }
                else
                {
                    // Create a simple array. Note: we can only do this type of create here due to the checks above.
                    newObj = AllocatePrimitiveArray(et, (DWORD)dims[0]);
                }

                // No exception, so it worked.
                pDE->m_successful = true;

                // Result type is, of course, the type of the array.
                pDE->m_resultType = newObj->GetTypeHandle();

                // Place the result in a strong handle to protect it from a collection.
                OBJECTHANDLE oh = pDE->m_thread->GetDomain()->CreateStrongHandle(newObj);
                pDE->m_result[0] = (ARG_SLOT)(LONG_PTR)oh;
                pDE->m_vmObjectHandle = VMPTR_OBJECTHANDLE::MakePtr(oh);

                break;
            }

        default:
            _ASSERTE(!"Invalid eval type!");
        }
    }
    EX_CATCH
    {
        // We got an exception. Make the exception into our result.
        OBJECTREF ppException = GET_THROWABLE();
        GCX_FORBID();
        RecordFuncEvalException( pDE, ppException);
    }
    // Note: we need to catch all exceptioins here because they all get reported as the result of
    // the funceval.  If a ThreadAbort occurred other than for a funcEval abort, we'll re-throw it manually.
    EX_END_CATCH(SwallowAllExceptions);

    GCPROTECT_END();
}

//
// FuncEvalHijackWorker is the function that managed threads start executing in order to perform a function
// evaluation. Control is transfered here on the proper thread by hijacking that that's IP to this method in
// Debugger::FuncEvalSetup. This function can also be called directly by a Runtime thread that is stopped sending a
// first or second chance exception to the Right Side.
//
// The DebuggerEval object may get deleted by the helper thread doing a CleanupFuncEval while this thread is blocked
// sending the eval complete.
void * STDCALL FuncEvalHijackWorker(DebuggerEval *pDE)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;

        PRECONDITION(CheckPointer(pDE));
    }
    CONTRACTL_END;



    Thread *pThread = NULL;
    CONTEXT *filterContext = NULL;

    {
        GCX_FORBID();

        LOG((LF_CORDB, LL_INFO100000, "D:FEHW for pDE:%08x evalType:%d\n", pDE, pDE->m_evalType));

        pThread = GetThread();

#ifndef DACCESS_COMPILE
#ifdef _DEBUG
        //
        // Flush all debug tracking information for this thread on object refs as it 
        // only approximates proper tracking and may have stale data, resulting in false
        // positives.  We dont want that as func-eval runs a lot, so flush them now.
        //
        g_pEEInterface->ObjectRefFlush(pThread);
#endif
#endif

        if (!pDE->m_evalDuringException)
        {
            //
            // From this point forward we use FORBID regions to guard against GCs.
            // Refer to code:Debugger::FuncEvalSetup to see the increment was done.
            //
            g_pDebugger->DecThreadsAtUnsafePlaces();
        }

        // Preemptive GC is disabled at the start of this method.
        _ASSERTE(g_pEEInterface->IsPreemptiveGCDisabled());

        DebuggerController::DispatchFuncEvalEnter(pThread);


        // If we've got a filter context still installed, then remove it while we do the work...
        filterContext = g_pEEInterface->GetThreadFilterContext(pDE->m_thread);

        if (filterContext)
        {
            _ASSERTE(pDE->m_evalDuringException);
            g_pEEInterface->SetThreadFilterContext(pDE->m_thread, NULL);
        }

    }

    //
    // Special handling for a re-abort eval. We don't setup a EX_TRY or try to lookup a function to call. All we do
    // is have this thread abort itself.
    //
    if (pDE->m_evalType == DB_IPCE_FET_RE_ABORT)
    {
        //
        // Push our FuncEvalFrame. The return address is equal to the IP in the saved context in the DebuggerEval. The
        // m_Datum becomes the ptr to the DebuggerEval. The frame address also serves as the address of the catch-handler-found.
        //
        FrameWithCookie<FuncEvalFrame> FEFrame(pDE, GetIP(&pDE->m_context), false);
        FEFrame.Push();

        pDE->m_thread->UserAbort(pDE->m_requester, EEPolicy::TA_Safe, INFINITE, Thread::UAC_Normal);
        _ASSERTE(!"Should not return from UserAbort here!");
        return NULL;
    }

    //
    // We cannot scope the following in a GCX_FORBID(), but we would like to.  But we need the frames on the
    // stack here, so they must never go out of scope.
    //

    //
    // Push our FuncEvalFrame. The return address is equal to the IP in the saved context in the DebuggerEval. The
    // m_Datum becomes the ptr to the DebuggerEval. The frame address also serves as the address of the catch-handler-found.
    //
    FrameWithCookie<FuncEvalFrame> FEFrame(pDE, GetIP(&pDE->m_context), true);
    FEFrame.Push();

    // On ARM/ARM64 the single step flag is per-thread and not per context.  We need to make sure that the SS flag is cleared
    // for the funceval, and that the state is back to what it should be after the funceval completes.
#ifdef FEATURE_EMULATE_SINGLESTEP
    bool ssEnabled = pDE->m_thread->IsSingleStepEnabled();
    if (ssEnabled)
        pDE->m_thread->DisableSingleStep();
#endif

    FuncEvalHijackRealWorker(pDE, pThread, &FEFrame);

#ifdef FEATURE_EMULATE_SINGLESTEP
    if (ssEnabled)
        pDE->m_thread->EnableSingleStep();
#endif



    LOG((LF_CORDB, LL_EVERYTHING, "FuncEval has finished its primary work.\n"));

    //
    // The func-eval is now completed, successfully or with failure, aborted or run-to-completion.
    //
    pDE->m_completed = true;

    if (pDE->m_thread->IsAbortRequested())
    {
        //
        // Check if an unmanaged thread tried to also abort this thread while we
        // were doing the func-eval, then that kind we want to rethrow. The check
        // versus m_aborted is for the case where the FE was aborted, we caught that, 
        // then cleared the FEAbort request, but there is still an outstanding abort
        // - then it must be a user abort.
        //
        if ((pDE->m_aborting == DebuggerEval::FE_ABORT_NONE) || pDE->m_aborted)
        {
            pDE->m_rethrowAbortException = true;
        }
        
        //
        // Reset the abort request if a func-eval abort was submitted, but the func-eval completed
        // before the abort could take place, we want to make sure we do not throw an abort exception
        // in this case.
        //
        if (pDE->m_aborting != DebuggerEval::FE_ABORT_NONE)
        {
            pDE->m_thread->UserResetAbort(Thread::TAR_FuncEval);
        }

    }

    // Codepitching can hijack our frame's return address. That means that we'll need to update PC in our saved context
    // so that when its restored, its like we've returned to the codepitching hijack. At this point, the old value of
    // EIP is worthless anyway.
    if (!pDE->m_evalDuringException)
    {
        SetIP(&pDE->m_context, (SIZE_T)FEFrame.GetReturnAddress());
    }

    //
    // Disable all steppers and breakpoints created during the func-eval
    //
    DebuggerController::DispatchFuncEvalExit(pThread);

    void *dest = NULL;

    if (!pDE->m_evalDuringException)
    {
        // Signal to the helper thread that we're done with our func eval.  Start by creating a DebuggerFuncEvalComplete
        // object. Give it an address at which to create the patch, which is a chunk of memory specified by our
        // DebuggerEval big enough to hold a breakpoint instruction.
#ifdef _TARGET_ARM_
        dest = (BYTE*)((DWORD)&(pDE->m_bpInfoSegment->m_breakpointInstruction) | THUMB_CODE);
#else
        dest = &(pDE->m_bpInfoSegment->m_breakpointInstruction);
#endif

        //
        // The created object below sets up itself as a hijack and will destroy itself when the hijack and work
        // is done.
        //

        DebuggerFuncEvalComplete *comp;
        comp = new (interopsafe) DebuggerFuncEvalComplete(pThread, dest);
        _ASSERTE(comp != NULL); // would have thrown

        // Pop the FuncEvalFrame now that we're pretty much done. Make sure we
        // don't pop the frame too early. Because GC can be triggered in our grabbing of
        // Debugger lock. If we pop the FE frame without setting back thread filter context,
        // the frames left uncrawlable.
        //
        FEFrame.Pop();
    }
    else
    {
        // We don't have to setup any special hijacks to return from here when we've been processing during an
        // exception. We just go ahead and send the FuncEvalComplete event over now. Don't forget to enable/disable PGC
        // around the call...
        _ASSERTE(g_pEEInterface->IsPreemptiveGCDisabled());

        if (filterContext != NULL)
        {
            g_pEEInterface->SetThreadFilterContext(pDE->m_thread, filterContext);
        }

        // Pop the FuncEvalFrame now that we're pretty much done.
        FEFrame.Pop();


        {
            //
            // This also grabs the debugger lock, so we can atomically check if a detach has
            // happened.
            //
            SENDIPCEVENT_BEGIN(g_pDebugger, pDE->m_thread);

            if ((pDE->m_thread->GetDomain() != NULL) && pDE->m_thread->GetDomain()->IsDebuggerAttached())
            {

                if (CORDebuggerAttached()) 
                {
                    g_pDebugger->FuncEvalComplete(pDE->m_thread, pDE);

                    g_pDebugger->SyncAllThreads(SENDIPCEVENT_PtrDbgLockHolder);
                }

            }

            SENDIPCEVENT_END;
        }
    }


    // pDE may now point to deleted memory if the helper thread did a CleanupFuncEval while we
    // were blocked waiting for a continue after the func-eval complete.

    // We return the address that we want to resume executing at.
    return dest;

}


#if defined(WIN64EXCEPTIONS) && !defined(FEATURE_PAL)

EXTERN_C EXCEPTION_DISPOSITION
FuncEvalHijackPersonalityRoutine(IN     PEXCEPTION_RECORD   pExceptionRecord
                       WIN64_ARG(IN     ULONG64             MemoryStackFp)
                   NOT_WIN64_ARG(IN     ULONG32             MemoryStackFp),
                                 IN OUT PCONTEXT            pContextRecord,
                                 IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                                )
{
    DebuggerEval* pDE = NULL;
#if defined(_TARGET_AMD64_)
    pDE = *(DebuggerEval**)(pDispatcherContext->EstablisherFrame);
#elif defined(_TARGET_ARM_)
    // on ARM the establisher frame is the SP of the caller of FuncEvalHijack, on other platforms it's FuncEvalHijack's SP.
    // in FuncEvalHijack we allocate 8 bytes of stack space and then store R0 at the current SP, so if we subtract 8 from
    // the establisher frame we can get the stack location where R0 was stored.
    pDE = *(DebuggerEval**)(pDispatcherContext->EstablisherFrame - 8);

#elif defined(_TARGET_ARM64_)
    // on ARM64 the establisher frame is the SP of the caller of FuncEvalHijack.
    // in FuncEvalHijack we allocate 32 bytes of stack space and then store R0 at the current SP + 16, so if we subtract 16 from
    // the establisher frame we can get the stack location where R0 was stored.
    pDE = *(DebuggerEval**)(pDispatcherContext->EstablisherFrame - 16);
#else
    _ASSERTE(!"NYI - FuncEvalHijackPersonalityRoutine()");
#endif

    FixupDispatcherContext(pDispatcherContext, &(pDE->m_context), pContextRecord);

    // Returning ExceptionCollidedUnwind will cause the OS to take our new context record and
    // dispatcher context and restart the exception dispatching on this call frame, which is
    // exactly the behavior we want.
    return ExceptionCollidedUnwind;
}


#endif // WIN64EXCEPTIONS && !FEATURE_PAL

#endif // ifndef DACCESS_COMPILE
