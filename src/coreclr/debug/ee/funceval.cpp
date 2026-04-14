// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// File: funceval.cpp
//
// funceval.cpp - Debugger func-eval routines.
//
// ****************************************************************************
// Putting code & #includes, #defines, etc, before the stdafx.h will
// cause the code,etc, to be silently ignored

#include "stdafx.h"
#include "debugdebugger.h"
#include "../inc/common.h"
#include "eeconfig.h" // This is here even for retail & free builds...

#include "vars.hpp"
#include "threads.h"
#include "appdomain.inl"
#include <limits.h>
#include "ilformatter.h"

#ifndef DACCESS_COMPILE

//
// This is the main file for processing func-evals.  The primary path is
// GCProtectArgsAndDoNormalFuncEval() → DoNormalFuncEval(), which builds a
// managed object[] of boxed arguments and invokes the target method via
// MethodBase.Invoke through an [UnmanagedCallersOnly] trampoline (UCOA pattern).
//
// The two other corner cases (create-string and create-array) are handled in
// FuncEvalHijackWorker() and are straightforward.
//
// DoNormalFuncEval:
//   1. Resolves generic type arguments.
//   2. Gathers signature type info for each argument (GatherFuncEvalArgInfo).
//   3. GC-protects argument addresses as interior pointers.
//   4. Reads and boxes each argument directly via ReadAndBoxArgValue.
//   5. Invokes the target method via the UCOA trampoline (InvokeFuncEval).
//   6. Unpacks the return value.
//   7. Writes back byref arguments to their original locations.
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
#if !defined(HOST_64BIT)
    case ELEMENT_TYPE_I:
#endif
    case ELEMENT_TYPE_I4:
        *(int*)pDst = (int)srcValue;
        break;
#if !defined(HOST_64BIT)
    case ELEMENT_TYPE_U:
#endif
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_R4:
        *(unsigned*)pDst = (unsigned)srcValue;
        break;
#if defined(HOST_64BIT)
    case ELEMENT_TYPE_I:
#endif
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_R8:
        *(INT64*)pDst = (INT64)srcValue;
        break;

#if defined(HOST_64BIT)
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

#if defined(TARGET_X86)
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

#elif defined(TARGET_AMD64)
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

#elif defined(TARGET_ARM64)
        // fall through
        case REGISTER_ARM64_X0:
        case REGISTER_ARM64_X1:
        case REGISTER_ARM64_X2:
        case REGISTER_ARM64_X3:
        case REGISTER_ARM64_X4:
        case REGISTER_ARM64_X5:
        case REGISTER_ARM64_X6:
        case REGISTER_ARM64_X7:
        case REGISTER_ARM64_X8:
        case REGISTER_ARM64_X9:
        case REGISTER_ARM64_X10:
        case REGISTER_ARM64_X11:
        case REGISTER_ARM64_X12:
        case REGISTER_ARM64_X13:
        case REGISTER_ARM64_X14:
        case REGISTER_ARM64_X15:
        case REGISTER_ARM64_X16:
        case REGISTER_ARM64_X17:
        case REGISTER_ARM64_X18:
        case REGISTER_ARM64_X19:
        case REGISTER_ARM64_X20:
        case REGISTER_ARM64_X21:
        case REGISTER_ARM64_X22:
        case REGISTER_ARM64_X23:
        case REGISTER_ARM64_X24:
        case REGISTER_ARM64_X25:
        case REGISTER_ARM64_X26:
        case REGISTER_ARM64_X27:
        case REGISTER_ARM64_X28:
            ret = pDE->m_context.X[reg - REGISTER_ARM64_X0];
            break;

        case REGISTER_ARM64_LR:
            ret = pDE->m_context.Lr;
            break;

        case REGISTER_ARM64_V0:
        case REGISTER_ARM64_V1:
        case REGISTER_ARM64_V2:
        case REGISTER_ARM64_V3:
        case REGISTER_ARM64_V4:
        case REGISTER_ARM64_V5:
        case REGISTER_ARM64_V6:
        case REGISTER_ARM64_V7:
        case REGISTER_ARM64_V8:
        case REGISTER_ARM64_V9:
        case REGISTER_ARM64_V10:
        case REGISTER_ARM64_V11:
        case REGISTER_ARM64_V12:
        case REGISTER_ARM64_V13:
        case REGISTER_ARM64_V14:
        case REGISTER_ARM64_V15:
        case REGISTER_ARM64_V16:
        case REGISTER_ARM64_V17:
        case REGISTER_ARM64_V18:
        case REGISTER_ARM64_V19:
        case REGISTER_ARM64_V20:
        case REGISTER_ARM64_V21:
        case REGISTER_ARM64_V22:
        case REGISTER_ARM64_V23:
        case REGISTER_ARM64_V24:
        case REGISTER_ARM64_V25:
        case REGISTER_ARM64_V26:
        case REGISTER_ARM64_V27:
        case REGISTER_ARM64_V28:
        case REGISTER_ARM64_V29:
        case REGISTER_ARM64_V30:
        case REGISTER_ARM64_V31:
            ret = FPSpillToR8(&pDE->m_context.V[reg - REGISTER_ARM64_V0]);
            break;

#endif // !TARGET_X86 && !TARGET_AMD64 && !TARGET_ARM64
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

#ifdef TARGET_X86
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

#elif defined(TARGET_AMD64)
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

#elif defined(TARGET_ARM64)
        // fall through
        case REGISTER_ARM64_X0:
        case REGISTER_ARM64_X1:
        case REGISTER_ARM64_X2:
        case REGISTER_ARM64_X3:
        case REGISTER_ARM64_X4:
        case REGISTER_ARM64_X5:
        case REGISTER_ARM64_X6:
        case REGISTER_ARM64_X7:
        case REGISTER_ARM64_X8:
        case REGISTER_ARM64_X9:
        case REGISTER_ARM64_X10:
        case REGISTER_ARM64_X11:
        case REGISTER_ARM64_X12:
        case REGISTER_ARM64_X13:
        case REGISTER_ARM64_X14:
        case REGISTER_ARM64_X15:
        case REGISTER_ARM64_X16:
        case REGISTER_ARM64_X17:
        case REGISTER_ARM64_X18:
        case REGISTER_ARM64_X19:
        case REGISTER_ARM64_X20:
        case REGISTER_ARM64_X21:
        case REGISTER_ARM64_X22:
        case REGISTER_ARM64_X23:
        case REGISTER_ARM64_X24:
        case REGISTER_ARM64_X25:
        case REGISTER_ARM64_X26:
        case REGISTER_ARM64_X27:
        case REGISTER_ARM64_X28:
            pDE->m_context.X[reg - REGISTER_ARM64_X0] = newValue;
            break;

        case REGISTER_ARM64_LR:
            pDE->m_context.Lr = newValue;
            break;

        case REGISTER_ARM64_V0:
        case REGISTER_ARM64_V1:
        case REGISTER_ARM64_V2:
        case REGISTER_ARM64_V3:
        case REGISTER_ARM64_V4:
        case REGISTER_ARM64_V5:
        case REGISTER_ARM64_V6:
        case REGISTER_ARM64_V7:
        case REGISTER_ARM64_V8:
        case REGISTER_ARM64_V9:
        case REGISTER_ARM64_V10:
        case REGISTER_ARM64_V11:
        case REGISTER_ARM64_V12:
        case REGISTER_ARM64_V13:
        case REGISTER_ARM64_V14:
        case REGISTER_ARM64_V15:
        case REGISTER_ARM64_V16:
        case REGISTER_ARM64_V17:
        case REGISTER_ARM64_V18:
        case REGISTER_ARM64_V19:
        case REGISTER_ARM64_V20:
        case REGISTER_ARM64_V21:
        case REGISTER_ARM64_V22:
        case REGISTER_ARM64_V23:
        case REGISTER_ARM64_V24:
        case REGISTER_ARM64_V25:
        case REGISTER_ARM64_V26:
        case REGISTER_ARM64_V27:
        case REGISTER_ARM64_V28:
        case REGISTER_ARM64_V29:
        case REGISTER_ARM64_V30:
        case REGISTER_ARM64_V31:
            R8ToFPSpill(&pDE->m_context.V[reg - REGISTER_ARM64_V0], newValue);
            break;

#endif // !TARGET_X86 && !TARGET_AMD64 && !TARGET_ARM64
        default:
            _ASSERT(!"Invalid register number!");
        }
    }
}

/*
 * GetRegisterValueAndReturnAddress
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
                                              SIZE_T *pSizeTBuf)
{
    LIMITED_METHOD_CONTRACT;

    PVOID pAddr;

#if !defined(HOST_64BIT)
    pAddr = pInt64Buf;
    DWORD *pLow = (DWORD*)(pInt64Buf);
    DWORD *pHigh  = pLow + 1;
#endif // HOST_64BIT

    switch (pFEAD->argHome.kind)
    {
#if !defined(HOST_64BIT)
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
#endif // HOST_64BIT

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

static CorDebugRegister GetArgAddrFromReg( DebuggerIPCE_FuncEvalArgData *pFEAD)
{
    CorDebugRegister retval = REGISTER_INSTRUCTION_POINTER; // good as default as any
#if defined(HOST_64BIT)
    retval = (pFEAD->argHome.kind == RAK_REG ?
              pFEAD->argHome.reg1 :
              (CorDebugRegister)((int)REGISTER_IA64_F0 + pFEAD->argHome.floatIndex));
#else  // !HOST_64BIT
    retval = pFEAD->argHome.reg1;
#endif // !HOST_64BIT
    return retval;
}

//
// Given info about a register-homed byref argument, write back the modified value
// from the managed object[] result into the proper register.
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

#if defined(HOST_64BIT)
            source = (INT64)maybeInteriorPtrArg;
#else  // !HOST_64BIT
            source = bufferByRefArg;
#endif // !HOST_64BIT

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
#if !defined(HOST_64BIT)
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
#else // HOST_64BIT
                // The only types we use are RAK_REG and RAK_FLOAT, and both of them can be 4 or 8 bytes.
                _ASSERTE((pFEAD->argHome.kind == RAK_REG) || (pFEAD->argHome.kind == RAK_FLOAT));

                SetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, source);
#endif // HOST_64BIT
            }
        }
        break;

    default:
        // literal values smaller than 8 bytes and "special types" (e.g. object, array, string, etc.)
        {
            SIZE_T source;

#ifdef TARGET_X86
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
#ifdef TARGET_X86
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
                         BIT64_ONLY(|| (pFEAD->argHome.kind == RAK_FLOAT)));

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
        COMPlusThrow(kArgumentException, W("Arg_NoDefCTorWithoutTypeName"));
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
        _ASSERTE(pDE->m_aborting != DebuggerEval::FE_ABORT_NONE);
        //
        // Reset the abort request.
        //
        pDE->m_thread->ResetAbort();

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
        // The result is the exception object.
        //
        pDE->m_result[0] = ObjToArgSlot(ppException);

        pDE->m_resultType = ppException->GetTypeHandle();
        OBJECTHANDLE oh = AppDomain::GetCurrentDomain()->CreateStrongHandle(ArgSlotToObj(pDE->m_result[0]));
        pDE->m_result[0] = (ARG_SLOT)(LONG_PTR)oh;
        pDE->m_vmObjectHandle = VMPTR_OBJECTHANDLE::MakePtr(oh);

        pDE->m_retValueBoxing = Debugger::NoValueTypeBoxing;

        LOG((LF_CORDB, LL_EVERYTHING, "D::FEHW - Exception for the user.\n"));
    }
}

/*
 * ReadAndBoxArgValue
 *
 * Reads an argument value from wherever the debugger stored it (memory address,
 * literal data, or register) and returns it as a boxed managed object.
 * This replaces the old multi-step GCProtectAllPassedArgs + CopyArgsToBuffer pipeline
 * with direct read-and-box, which is correct because MethodBase.Invoke is arch-neutral.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *    pFEAD - Information about this particular argument from the debugger.
 *    pFEArgInfo - Signature-level type information for this argument (may be NULL for 'this').
 *    pArgAddr - GC-protected copy of pFEAD->argAddr (may have been updated by GC), or NULL.
 *    pFallbackMT - MethodTable to use for boxing when pFEArgInfo is NULL (e.g. for value-type 'this').
 *
 * Returns:
 *    A boxed OBJECTREF for the argument value.
 */
static OBJECTREF ReadAndBoxArgValue(DebuggerEval *pDE,
                                    DebuggerIPCE_FuncEvalArgData *pFEAD,
                                    FuncEvalArgInfo *pFEArgInfo,
                                    void *pArgAddr,
                                    MethodTable *pFallbackMT = NULL)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // Reference types (CLASS, OBJECT, STRING, ARRAY, SZARRAY)
    if (IsElementTypeSpecial(pFEAD->argElementType))
    {
        if (pFEAD->argIsHandleValue)
        {
            OBJECTHANDLE oh = (OBJECTHANDLE)(pFEAD->argAddr);
            return ObjectFromHandle(oh);
        }
        else if (pFEAD->argAddr != NULL)
        {
            return *(OBJECTREF *)pArgAddr;
        }
        else if (pFEAD->argIsLiteral)
        {
            OBJECTREF v = NULL;
            _ASSERTE(sizeof(pFEAD->argLiteralData) >= sizeof(OBJECTREF));
            memcpy(&v, pFEAD->argLiteralData, sizeof(v));
            return v;
        }
        else
        {
            _ASSERTE(pFEAD->argHome.kind == RAK_REG);
            SIZE_T v = GetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);
            return (OBJECTREF)v;
        }
    }

    // Value types
    if (pFEAD->argElementType == ELEMENT_TYPE_VALUETYPE)
    {
        void *pData = NULL;
        INT64 bigVal = 0;
        SIZE_T regVal = 0;

        if (pFEAD->argAddr != NULL)
        {
            pData = pArgAddr;
        }
        else
        {
            pData = GetRegisterValueAndReturnAddress(pDE, pFEAD, &bigVal, &regVal);
            if (pData == NULL)
                COMPlusThrow(kArgumentNullException);
        }

        _ASSERTE(pData != NULL);
        MethodTable *pMT = (pFEArgInfo != NULL) ? pFEArgInfo->sigTypeHandle.GetMethodTable() : pFallbackMT;
        if (pMT != NULL)
            return pMT->Box(pData);
        return NULL;
    }

    // Primitives (I4, U4, I8, R8, BOOLEAN, CHAR, etc.)
    {
        INT64 rawValue = 0;

        if (pFEAD->argAddr != NULL)
        {
            unsigned size = g_pEEInterface->GetSizeForCorElementType(pFEAD->argElementType);
            memcpy(&rawValue, pArgAddr, min(size, (unsigned)sizeof(rawValue)));
        }
        else if (pFEAD->argIsLiteral)
        {
            memcpy(&rawValue, pFEAD->argLiteralData, sizeof(rawValue));
        }
        else
        {
            SIZE_T v = GetRegisterValue(pDE, pFEAD->argHome.reg1, pFEAD->argHome.reg1Addr, pFEAD->argHome.reg1Value);
            rawValue = (INT64)v;
        }

        CorElementType sigType = ELEMENT_TYPE_END;
        if (pFEArgInfo != NULL)
        {
            sigType = pFEArgInfo->argSigType;
            if (sigType == ELEMENT_TYPE_BYREF)
                sigType = pFEArgInfo->byrefArgSigType;
        }
        if (sigType == ELEMENT_TYPE_END)
            sigType = pFEAD->argElementType;

        MethodTable *pMT = CoreLibBinder::GetElementType(sigType);
        if (pMT != NULL)
            return pMT->Box(&rawValue);
        return NULL;
    }
}

/*
 * DoNormalFuncEval
 *
 * Performs a func-eval by building a managed object[] and invoking via
 * MethodBase.Invoke through a managed [UnmanagedCallersOnly] trampoline.
 * This replaces the old CDW (CallDescrWorker) path, eliminating arch-specific
 * calling convention details since reflection invoke is arch-neutral.
 *
 * Parameters:
 *    pDE - pointer to the DebuggerEval object being processed.
 *
 * Returns:
 *    None.
 */
static void DoNormalFuncEval(DebuggerEval *pDE)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    ResolveFuncEvalGenericArgInfo(pDE);

    MetaSig mSig(pDE->m_md);

    BYTE callingconvention = mSig.GetCallingConvention();
    if (!isCallConv(callingconvention, IMAGE_CEE_CS_CALLCONV_DEFAULT))
    {
        COMPlusThrow(kArgumentException, W("Argument_CORDBBadVarArgCallConv"));
    }

    BOOL staticMethod = pDE->m_md->IsStatic();
    _ASSERTE((pDE->m_evalType == DB_IPCE_FET_NORMAL) || !staticMethod);

    OBJECTREF newObj = NULL;
    GCPROTECT_BEGIN(newObj);

    SIZE_T allocArgCnt;

    if (pDE->m_evalType == DB_IPCE_FET_NEW_OBJECT)
    {
        ValidateFuncEvalReturnType(DB_IPCE_FET_NEW_OBJECT, pDE->m_resultType.GetMethodTable());
        pDE->m_resultType.GetMethodTable()->EnsureInstanceActive();
        newObj = AllocateObject(pDE->m_resultType.GetMethodTable());
        allocArgCnt = pDE->m_argCount + 1;
    }
    else
    {
        allocArgCnt = pDE->m_argCount;
    }

    if (allocArgCnt != (mSig.NumFixedArgs() + (staticMethod ? 0 : 1)))
    {
        COMPlusThrow(kTargetParameterCountException, W("Arg_ParmCnt"));
    }

    DebuggerIPCE_FuncEvalArgData *argData = pDE->GetArgData();

    // GC-protect all arg addresses as interior pointers before any GC-triggering
    // operations (GatherFuncEvalArgInfo walks the signature, GetRetTypeHandleThrowing
    // resolves types — both can trigger GC). Some argAddr values may point into
    // managed objects on the GC heap (e.g. fields of heap-allocated objects).
    // Protecting them ensures the GC updates these pointers if objects move.
    SIZE_T cbAllocSize;
    if (!(ClrSafeInt<SIZE_T>::multiply(pDE->m_argCount, sizeof(void *), cbAllocSize)) ||
        (cbAllocSize != (size_t)(cbAllocSize)))
    {
        ThrowHR(COR_E_OVERFLOW);
    }
    void **pArgAddrs = (void **)_alloca(cbAllocSize);
    memset(pArgAddrs, 0, cbAllocSize);
    for (unsigned i = 0; i < pDE->m_argCount; i++)
    {
        if (argData[i].argAddr != NULL)
            pArgAddrs[i] = (void *)(argData[i].argAddr);
    }
    GCPROTECT_BEGININTERIOR_ARRAY(*pArgAddrs, pDE->m_argCount);

    // Gather signature type info for each argument.
    if (!(ClrSafeInt<SIZE_T>::multiply(pDE->m_argCount, sizeof(FuncEvalArgInfo), cbAllocSize)) ||
        (cbAllocSize != (size_t)(cbAllocSize)))
    {
        ThrowHR(COR_E_OVERFLOW);
    }
    FuncEvalArgInfo *pFEArgInfo = (FuncEvalArgInfo *)_alloca(cbAllocSize);
    memset(pFEArgInfo, 0, cbAllocSize);
    GatherFuncEvalArgInfo(pDE, mSig, argData, pFEArgInfo);

    // Set the result type for NORMAL evals.
    if (pDE->m_evalType != DB_IPCE_FET_NEW_OBJECT)
    {
        pDE->m_resultType = mSig.GetRetTypeHandleThrowing();
    }

    struct
    {
        PTRARRAYREF argsArray;
        OBJECTREF   thisArg;
        OBJECTREF   resultObj;
    } ucoGc;
    ucoGc.argsArray = NULL;
    ucoGc.thisArg = NULL;
    ucoGc.resultObj = NULL;
    GCPROTECT_BEGIN(ucoGc);

    UINT methodArgCount = mSig.NumFixedArgs();
    unsigned firstArgIndex = 0;

    // Build 'this'
    if (pDE->m_evalType == DB_IPCE_FET_NEW_OBJECT)
    {
        ucoGc.thisArg = newObj;
    }
    else if (!staticMethod && pDE->m_argCount > 0)
    {
        firstArgIndex = 1;
        ucoGc.thisArg = ReadAndBoxArgValue(pDE, &argData[0], NULL, pArgAddrs[0], pDE->m_md->GetMethodTable());
    }

    // Build managed object[] from the remaining arguments.
    if (methodArgCount > 0)
    {
        ucoGc.argsArray = (PTRARRAYREF)AllocateObjectArray(methodArgCount, g_pObjectClass);

        for (unsigned i = 0; i < methodArgCount; i++)
        {
            unsigned srcIndex = firstArgIndex + i;
            OBJECTREF argObj = ReadAndBoxArgValue(pDE, &argData[srcIndex], &pFEArgInfo[srcIndex], pArgAddrs[srcIndex]);
            ucoGc.argsArray->SetAt(i, argObj);
        }
    }

    // Invoke via UnmanagedCallersOnly trampoline into managed MethodBase.Invoke.
    struct
    {
        MethodDesc  *pMD;
        MethodTable *pOwnerMT;
        OBJECTREF   *pThisObj;
        PTRARRAYREF *pArgs;
        INT32        isNewObj;
    } invokeArgs;

    invokeArgs.pMD = pDE->m_md;
    invokeArgs.pOwnerMT = pDE->m_ownerTypeHandle.GetMethodTable();
    invokeArgs.pThisObj = &ucoGc.thisArg;
    invokeArgs.pArgs = &ucoGc.argsArray;
    invokeArgs.isNewObj = (pDE->m_evalType == DB_IPCE_FET_NEW_OBJECT) ? 1 : 0;

    LOG((LF_CORDB, LL_EVERYTHING,
         "Func eval for %s::%s via UCOA\n",
         pDE->m_md->m_pszDebugClassName,
         pDE->m_md->m_pszDebugMethodName));

    UnmanagedCallersOnlyCaller funcEvalInvoker(METHOD__RUNTIME_HELPERS__INVOKE_FUNC_EVAL);
    funcEvalInvoker.InvokeThrowing(&invokeArgs, &ucoGc.resultObj);

    // Unpack results.
    LOG((LF_CORDB, LL_EVERYTHING, "FuncEval call has returned\n"));

    if (pDE->m_evalType == DB_IPCE_FET_NEW_OBJECT)
    {
        pDE->m_result[0] = ObjToArgSlot(newObj);
        pDE->m_retValueBoxing = Debugger::AllBoxed;
    }
    else if (ucoGc.resultObj != NULL)
    {
        CorElementType retET = pDE->m_resultType.GetSignatureCorElementType();

        if (retET == ELEMENT_TYPE_VALUETYPE || IsElementTypeSpecial(retET))
        {
            // True value types (structs) and reference types — return as boxed object.
            pDE->m_result[0] = ObjToArgSlot(ucoGc.resultObj);
            pDE->m_retValueBoxing = Debugger::AllBoxed;
        }
        else
        {
            // Primitive return (bool, int, float, etc.) — unbox into m_result.
            memset(pDE->m_result, 0, sizeof(pDE->m_result));
            void *pRetData = ucoGc.resultObj->GetData();
            unsigned retSize = ucoGc.resultObj->GetMethodTable()->GetNumInstanceFieldBytes();
            memcpy(pDE->m_result, pRetData, min(retSize, (unsigned)sizeof(pDE->m_result)));
            pDE->m_retValueBoxing = Debugger::OnlyPrimitivesUnboxed;
        }
    }
    else
    {
        memset(pDE->m_result, 0, sizeof(pDE->m_result));
        pDE->m_retValueBoxing = Debugger::OnlyPrimitivesUnboxed;
    }

    pDE->m_successful = true;

    // Create strong handle to prevent GC collection of object results.
    {
        if (pDE->m_retValueBoxing == Debugger::AllBoxed)
        {
            LOG((LF_CORDB, LL_EVERYTHING, "Creating strong handle for boxed DoNormalFuncEval result.\n"));
            OBJECTHANDLE oh = AppDomain::GetCurrentDomain()->CreateStrongHandle(ArgSlotToObj(pDE->m_result[0]));
            pDE->m_result[0] = (INT64)(LONG_PTR)oh;
            pDE->m_vmObjectHandle = VMPTR_OBJECTHANDLE::MakePtr(oh);
        }
    }

    // Byref writeback: copy modified args from managed object[] back to
    // original locations so the debugger sees updated values.
    if (methodArgCount > 0 && ucoGc.argsArray != NULL)
    {
        mSig.Reset();

        unsigned currArgIndex = 0;
        if (!staticMethod)
            currArgIndex = 1;

        for (unsigned argIdx = 0; argIdx < methodArgCount; argIdx++, currArgIndex++)
        {
            CorElementType argSigType = mSig.NextArgNormalized();
            _ASSERTE(argSigType != ELEMENT_TYPE_END);

            if (argSigType != ELEMENT_TYPE_BYREF)
                continue;

            TypeHandle byrefClass = TypeHandle();
            CorElementType byrefArgSigType = mSig.GetByRefType(&byrefClass);
            OBJECTREF modifiedArg = Nullable::NormalizeBox(ucoGc.argsArray->GetAt(argIdx));
            DebuggerIPCE_FuncEvalArgData *pFEAD = &argData[currArgIndex];

            if (pFEAD->argIsLiteral)
            {
                if (modifiedArg != NULL && !IsElementTypeSpecial(byrefArgSigType))
                {
                    void *pData = modifiedArg->GetData();
                    unsigned size = modifiedArg->GetMethodTable()->GetNumInstanceFieldBytes();
                    memcpy(pFEAD->argLiteralData, pData, min(size, (unsigned)sizeof(pFEAD->argLiteralData)));
                }
            }
            else if (pFEAD->argAddr != NULL)
            {
                void *pOrigAddr = pArgAddrs[currArgIndex];
                if (pOrigAddr != NULL)
                {
                    if (IsElementTypeSpecial(byrefArgSigType))
                    {
                        SetObjectReference((OBJECTREF *)pOrigAddr, modifiedArg);
                    }
                    else if (byrefArgSigType == ELEMENT_TYPE_VALUETYPE && modifiedArg != NULL)
                    {
                        CopyValueClass(pOrigAddr, modifiedArg->GetData(), byrefClass.GetMethodTable());
                    }
                    else if (modifiedArg != NULL)
                    {
                        void *pData = modifiedArg->GetData();
                        unsigned size = modifiedArg->GetMethodTable()->GetNumInstanceFieldBytes();
                        memcpy(pOrigAddr, pData, min(size, (unsigned)sizeof(INT64)));
                    }
                }
            }
            else
            {
                // Register-homed byref — extract raw value and write back
                INT64 rawVal = 0;
                if (modifiedArg != NULL && !IsElementTypeSpecial(byrefArgSigType))
                {
                    void *pData = modifiedArg->GetData();
                    unsigned size = modifiedArg->GetMethodTable()->GetNumInstanceFieldBytes();
                    memcpy(&rawVal, pData, min(size, (unsigned)sizeof(rawVal)));
                }
                SetFuncEvalByRefArgValue(pDE, pFEAD, byrefArgSigType,
                                         rawVal, (void *)(SIZE_T)rawVal, NULL, modifiedArg);
            }
        }
    }

    GCPROTECT_END();    // ucoGc
    GCPROTECT_END();    // pArgAddrs
    GCPROTECT_END();    // newObj
}

/*
 * GCProtectArgsAndDoNormalFuncEval
 *
 * Primary entrypoint for normal func-evals.  Wraps DoNormalFuncEval in
 * an exception handler so that thrown exceptions become the func-eval result.
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

    EX_TRY
    {
        DoNormalFuncEval(pDE);
    }
    EX_CATCH
    {
        OBJECTREF ppException = GET_THROWABLE();
        GCX_FORBID();
        RecordFuncEvalException(pDE, ppException);
    }
    EX_END_CATCH

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
                OBJECTHANDLE oh = AppDomain::GetCurrentDomain()->CreateStrongHandle(newObj);
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
                OBJECTHANDLE oh = AppDomain::GetCurrentDomain()->CreateStrongHandle(newObj);
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
                OBJECTHANDLE oh = AppDomain::GetCurrentDomain()->CreateStrongHandle(newObj);
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
    // the funceval.
    EX_END_CATCH

    GCPROTECT_END();
}

//
// FuncEvalHijackWorker is the function that managed threads start executing in order to perform a function
// evaluation. Control is transferred here on the proper thread by hijacking that that's IP to this method in
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
    // We cannot scope the following in a GCX_FORBID(), but we would like to.  But we need the frames on the
    // stack here, so they must never go out of scope.
    //

    //
    // Push our FuncEvalFrame. The return address is equal to the IP in the saved context in the DebuggerEval. The
    // m_Datum becomes the ptr to the DebuggerEval. The frame address also serves as the address of the catch-handler-found.
    //
    FuncEvalFrame FEFrame(pDE, GetIP(&pDE->m_context), true);
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
        // noone else should be requesting aborts,
        // so this must be our request that did not have a chance to run.
        _ASSERTE((pDE->m_aborting != DebuggerEval::FE_ABORT_NONE) && !pDE->m_aborted);

        //
        // Reset the abort request if a func-eval abort was submitted, but the func-eval completed
        // before the abort could take place, we want to make sure we do not throw an abort exception
        // in this case.
        //
        pDE->m_thread->ResetAbort();
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
#ifdef TARGET_ARM
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

            if ((AppDomain::GetCurrentDomain() != NULL) && AppDomain::GetCurrentDomain()->IsDebuggerAttached())
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

#if !defined(TARGET_UNIX) && !defined(TARGET_X86)

EXTERN_C EXCEPTION_DISPOSITION
FuncEvalHijackPersonalityRoutine(IN     PEXCEPTION_RECORD   pExceptionRecord,
                                 IN     PVOID               pEstablisherFrame,
                                 IN OUT PCONTEXT            pContextRecord,
                                 IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                                )
{
    // The offset of the DebuggerEval pointer relative to the establisher frame.
    SIZE_T debuggerEvalPtrOffset = 0;
#if defined(TARGET_AMD64)
    // On AMD64 the establisher frame is the SP of FuncEvalHijack itself.
    // In FuncEvalHijack we store RCX at the current SP.
    debuggerEvalPtrOffset = 0;
#elif defined(TARGET_ARM)
    // On ARM the establisher frame is the SP of the FuncEvalHijack's caller.
    // In FuncEvalHijack we allocate 8 bytes of stack space and then store R0 at the current SP, so if we subtract 8 from
    // the establisher frame we can get the stack location where R0 was stored.
    debuggerEvalPtrOffset = 8;
#elif defined(TARGET_ARM64)
    // On ARM64 the establisher frame is the SP of the FuncEvalHijack's caller.
    // In FuncEvalHijack we allocate 32 bytes of stack space and then store X0 at the current SP + 16, so if we subtract 16 from
    // the establisher frame we can get the stack location where X0 was stored.
    debuggerEvalPtrOffset = 16;
#else
    _ASSERTE(!"NYI - FuncEvalHijackPersonalityRoutine()");
#endif

    DebuggerEval* pDE = *(DebuggerEval**)(pDispatcherContext->EstablisherFrame - debuggerEvalPtrOffset);
    FixupDispatcherContext(pDispatcherContext, &(pDE->m_context));

    // Returning ExceptionCollidedUnwind will cause the OS to take our new context record and
    // dispatcher context and restart the exception dispatching on this call frame, which is
    // exactly the behavior we want.
    return ExceptionCollidedUnwind;
}

#endif // !TARGET_UNIX && !TARGET_X86

#endif // ifndef DACCESS_COMPILE
