// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// FILE: profiler.cpp
//

//

//
// ======================================================================================

#include "common.h"

#ifdef PROFILING_SUPPORTED
#include "proftoeeinterfaceimpl.h"

MethodDesc *FunctionIdToMethodDesc(FunctionID functionID);

// TODO: move these to some common.h file
// FLAGS
#define PROFILE_ENTER        0x1
#define PROFILE_LEAVE        0x2
#define PROFILE_TAILCALL     0x4

typedef struct _PROFILE_PLATFORM_SPECIFIC_DATA
{
    UINT32      r0;         // Keep r0 & r1 contiguous to make returning 64-bit results easier
    UINT32      r1;
    void       *R11;
    void       *Pc;
    union                   // Float arg registers as 32-bit (s0-s15) and 64-bit (d0-d7)
    {
        UINT32  s[16];
        UINT64  d[8];
    };
    FunctionID  functionId;
    void       *probeSp;    // stack pointer of managed function
    void       *profiledSp; // location of arguments on stack
    LPVOID      hiddenArg;
    UINT32      flags;
} PROFILE_PLATFORM_SPECIFIC_DATA, *PPROFILE_PLATFORM_SPECIFIC_DATA;


/*
 * ProfileGetIPFromPlatformSpecificHandle
 *
 * This routine takes the platformSpecificHandle and retrieves from it the
 * IP value.
 *
 * Parameters:
 *    handle - the platformSpecificHandle passed to ProfileEnter/Leave/Tailcall
 *
 * Returns:
 *    The IP value stored in the handle.
 */
UINT_PTR ProfileGetIPFromPlatformSpecificHandle(void *handle)
{
    LIMITED_METHOD_CONTRACT;

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = (PROFILE_PLATFORM_SPECIFIC_DATA*)handle;
    return (UINT_PTR)pData->Pc;
}


/*
 * ProfileSetFunctionIDInPlatformSpecificHandle
 *
 * This routine takes the platformSpecificHandle and functionID, and assign
 * functionID to functionID field of platformSpecificHandle.
 *
 * Parameters:
 *    pPlatformSpecificHandle - the platformSpecificHandle passed to ProfileEnter/Leave/Tailcall
 *    functionID - the FunctionID to be assigned
 *
 * Returns:
 *    None
 */
void ProfileSetFunctionIDInPlatformSpecificHandle(void * pPlatformSpecificHandle, FunctionID functionID)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pPlatformSpecificHandle != NULL);
    _ASSERTE(functionID != NULL);

    PROFILE_PLATFORM_SPECIFIC_DATA * pData = reinterpret_cast<PROFILE_PLATFORM_SPECIFIC_DATA *>(pPlatformSpecificHandle);
    pData->functionId = functionID;
}

/*
 * ProfileArgIterator::ProfileArgIterator
 *
 * Constructor.  Does almost nothing.  Init must be called after construction.
 *
 */
ProfileArgIterator::ProfileArgIterator(MetaSig * pSig, void * platformSpecificHandle)
    : m_argIterator(pSig)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pSig != NULL);
    _ASSERTE(platformSpecificHandle != NULL);

    m_handle = platformSpecificHandle;
    PROFILE_PLATFORM_SPECIFIC_DATA* pData = (PROFILE_PLATFORM_SPECIFIC_DATA*)m_handle;

    // unwind a frame and get the SP for the profiled method to make sure it matches
    // what the JIT gave us
#ifdef _DEBUG
    {
/*
    Foo() {
        Bar();
    }

Stack for the above call will look as follows (stack growing downwards):

   |
   |        Stack Args for Foo        |
   |           pre spill r0-r3           |
   |                   LR                    |
   |                  R11                   |
   |            Locals of Foo            |
   |        Stack Args for Bar        |
   | pre spill r0-r3                     | __________this Sp value is saved in profiledSP
   | LR                                      |
   | R11                                    |
   | Satck saved in prolog of Bar |  _______ call to profiler hook is made here_____this Sp value is saved in probeSP
   |                                           |


*/

        // setup the context to represent the frame that called ProfileEnterNaked
        CONTEXT ctx;
        memset(&ctx, 0, sizeof(CONTEXT));
        ctx.Sp = (UINT)pData->probeSp;
        ctx.R11 = (UINT)pData->R11;
        ctx.Pc = (UINT)pData->Pc;
        // For some functions which do localloc, sp is saved in r9. In order to perform unwinding for functions r9 must be set in the context.
        // r9 is stored at offset (sizeof(PROFILE_PLATFORM_SPECIFIC_DATA) (this also includes the padding done for 8-byte stack alignment) + size required for (r0,r3)) bytes from pData
        ctx.R9 = *((UINT*)pData + (sizeof(PROFILE_PLATFORM_SPECIFIC_DATA) + 8)/4);

        // walk up a frame to the caller frame (called the managed method which
        // called ProfileEnterNaked)
        Thread::VirtualUnwindCallFrame(&ctx);

        // add the prespill register(r0-r3) size to get the stack pointer of previous function
        _ASSERTE(pData->profiledSp == (void*)(ctx.Sp - 4*4) || pData->profiledSp == (void*)(ctx.Sp - 6*4));
    }
#endif // _DEBUG

    // Get the hidden arg if there is one
    MethodDesc * pMD = FunctionIdToMethodDesc(pData->functionId);

    if ( (pData->hiddenArg == NULL)                                         &&
         (pMD->RequiresInstArg() || pMD->AcquiresInstMethodTableFromThis()) )
    {
        // In the enter probe, the JIT may not have pushed the generics token onto the stack yet.
        // Luckily, we can inspect the registers reliably at this point.
        if (pData->flags & PROFILE_ENTER)
        {
            _ASSERTE(!((pData->flags & PROFILE_LEAVE) || (pData->flags & PROFILE_TAILCALL)));

            if (pMD->AcquiresInstMethodTableFromThis())
            {
                pData->hiddenArg = GetThis();
            }
            else
            {
                // The param type arg comes after the return buffer argument and the "this" pointer.
                int     index = 0;

                if (m_argIterator.HasThis())
                {
                    index++;
                }

                if (m_argIterator.HasRetBuffArg())
                {
                    index++;
                }

                pData->hiddenArg = *(LPVOID*)((LPBYTE)pData->profiledSp + (index * sizeof(SIZE_T)));
            }
        }
        else
        {
            EECodeInfo codeInfo((PCODE)pData->Pc);

            // We want to pass the caller SP here.
            pData->hiddenArg = EECodeManager::GetExactGenericsToken((SIZE_T)(pData->profiledSp), &codeInfo);
        }
    }
}


/*
 * ProfileArgIterator::~ProfileArgIterator
 *
 * Destructor, releases all resources.
 *
 */
ProfileArgIterator::~ProfileArgIterator()
{
    LIMITED_METHOD_CONTRACT;

    m_handle = NULL;
}


/*
 * ProfileArgIterator::GetNextArgAddr
 *
 * After initialization, this method is called repeatedly until it
 * returns NULL to get the address of each arg.  Note: this address
 * could be anywhere on the stack.
 *
 * Returns:
 *    Address of the argument, or NULL if iteration is complete.
 */
LPVOID ProfileArgIterator::GetNextArgAddr()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_handle != NULL);

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = (PROFILE_PLATFORM_SPECIFIC_DATA*)m_handle;

    if ((pData->flags & PROFILE_LEAVE) || (pData->flags & PROFILE_TAILCALL))
    {
        _ASSERTE(!"GetNextArgAddr() - arguments are not available in leave and tailcall probes");
        return NULL;
    }

    int argOffset = m_argIterator.GetNextOffset();

    // argOffset of TransitionBlock::InvalidOffset indicates that we're done
    if (argOffset == TransitionBlock::InvalidOffset)
    {
        return NULL;
    }

    if (TransitionBlock::IsFloatArgumentRegisterOffset(argOffset))
    {
        // Arguments which land up in floating point registers are contained entirely within those
        // registers (they're never split onto the stack).
        return ((BYTE *)&pData->d) + (argOffset - TransitionBlock::GetOffsetOfFloatArgumentRegisters());
    }

    // Argument lives in one or more general registers (and possibly overflows onto the stack).
    return (LPBYTE)pData->profiledSp + (argOffset - TransitionBlock::GetOffsetOfArgumentRegisters());
}

/*
 * ProfileArgIterator::GetHiddenArgValue
 *
 * Called after initialization, any number of times, to retrieve any
 * hidden argument, so that resolution for Generics can be done.
 *
 * Parameters:
 *    None.
 *
 * Returns:
 *    Value of the hidden parameter, or NULL if none exists.
 */
LPVOID ProfileArgIterator::GetHiddenArgValue(void)
{
    LIMITED_METHOD_CONTRACT;

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = (PROFILE_PLATFORM_SPECIFIC_DATA*)m_handle;

    return pData->hiddenArg;
}

/*
 * ProfileArgIterator::GetThis
 *
 * Called after initialization, any number of times, to retrieve any
 * 'this' pointer.
 *
 * Parameters:
 *    None.
 *
 * Returns:
 *    Address of the 'this', or NULL if none exists.
 */
LPVOID ProfileArgIterator::GetThis(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = (PROFILE_PLATFORM_SPECIFIC_DATA*)m_handle;
    MethodDesc * pMD = FunctionIdToMethodDesc(pData->functionId);

    // We guarantee to return the correct "this" pointer in the enter probe.
    // For the leave and tailcall probes, we only return a valid "this" pointer if it is the generics token.
    if (pData->hiddenArg != NULL)
    {
        if (pMD->AcquiresInstMethodTableFromThis())
        {
            return pData->hiddenArg;
        }
    }

    if (pData->flags & PROFILE_ENTER)
    {
        if (m_argIterator.HasThis())
        {
            return *(LPVOID*)((LPBYTE)pData->profiledSp);
        }
    }

    return NULL;
}

/*
 * ProfileArgIterator::GetReturnBufferAddr
 *
 * Called after initialization, any number of times, to retrieve the
 * address of the return buffer.  NULL indicates no return value.
 *
 * Parameters:
 *    None.
 *
 * Returns:
 *    Address of the return buffer, or NULL if none exists.
 */
LPVOID ProfileArgIterator::GetReturnBufferAddr(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = (PROFILE_PLATFORM_SPECIFIC_DATA*)m_handle;
    MethodDesc * pMD = FunctionIdToMethodDesc(pData->functionId);

    if (m_argIterator.HasRetBuffArg())
    {
        return (LPVOID)pData->r0;
    }

    if (m_argIterator.GetFPReturnSize() != 0)
        return &pData->d[0];

    if (m_argIterator.GetSig()->GetReturnType() != ELEMENT_TYPE_VOID)
        return &pData->r0;
    else
        return NULL;
}

#endif // PROFILING_SUPPORTED
