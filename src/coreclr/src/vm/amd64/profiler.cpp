// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// FILE: profiler.cpp
//

// 

// 
// ======================================================================================

#include "common.h"

#ifdef PROFILING_SUPPORTED
#include "proftoeeinterfaceimpl.h"
#include "argdestination.h"

MethodDesc *FunctionIdToMethodDesc(FunctionID functionID);

// TODO: move these to some common.h file
// FLAGS
#define PROFILE_ENTER        0x1
#define PROFILE_LEAVE        0x2
#define PROFILE_TAILCALL     0x4

#define PROFILE_PLATFORM_SPECIFIC_DATA_BUFFER_SIZE 16

typedef struct _PROFILE_PLATFORM_SPECIFIC_DATA
{
    FunctionID  functionId;
    void       *rbp;
    void       *probeRsp;
    void       *ip;
    void       *profiledRsp;
    UINT64      rax;
    LPVOID      hiddenArg;
    UINT64      flt0;   // floats stored as doubles
    UINT64      flt1;
    UINT64      flt2;
    UINT64      flt3;
#if defined(UNIX_AMD64_ABI)
    UINT64      flt4;
    UINT64      flt5;
    UINT64      flt6;
    UINT64      flt7;
    UINT64      rdi;
    UINT64      rsi;
    UINT64      rdx;
    UINT64      rcx;
    UINT64      r8;
    UINT64      r9;
#endif
    UINT32      flags;
#if defined(UNIX_AMD64_ABI)
    // A buffer to copy structs in to so they are sequential for GetFunctionEnter3Info.
    UINT64      buffer[PROFILE_PLATFORM_SPECIFIC_DATA_BUFFER_SIZE];
#endif
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
    return (UINT_PTR)pData->ip;
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
 * Constructor. Initializes for arg iteration.
 *
 * Parameters:
 *    pMetaSig - The signature of the method we are going iterate over
 *    platformSpecificHandle - the value passed to ProfileEnter/Leave/Tailcall
 *
 * Returns:
 *    None.
 */
ProfileArgIterator::ProfileArgIterator(MetaSig * pSig, void * platformSpecificHandle) :
    m_argIterator(pSig)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pSig != NULL);
    _ASSERTE(platformSpecificHandle != NULL);

    m_handle = platformSpecificHandle;
    PROFILE_PLATFORM_SPECIFIC_DATA* pData = (PROFILE_PLATFORM_SPECIFIC_DATA*)m_handle;
#ifdef UNIX_AMD64_ABI
    m_bufferPos = 0;
#endif // UNIX_AMD64_ABI

    // unwind a frame and get the Rsp for the profiled method to make sure it matches
    // what the JIT gave us
#ifdef _DEBUG
    {
        // setup the context to represent the frame that called ProfileEnterNaked
        CONTEXT ctx;
        memset(&ctx, 0, sizeof(CONTEXT));
        ctx.Rsp = (UINT64)pData->probeRsp;
        ctx.Rbp = (UINT64)pData->rbp;
        ctx.Rip = (UINT64)pData->ip;

        // walk up a frame to the caller frame (called the managed method which
        // called ProfileEnterNaked)
        Thread::VirtualUnwindCallFrame(&ctx);

        _ASSERTE(pData->profiledRsp == (void*)ctx.Rsp);
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

#ifdef UNIX_AMD64_ABI
                switch (index)
               {
                case 0: pData->hiddenArg = (LPVOID)pData->rdi; break;
                case 1: pData->hiddenArg = (LPVOID)pData->rsi; break;
                case 2: pData->hiddenArg = (LPVOID)pData->rdx; break;
                }
#else
                pData->hiddenArg = *(LPVOID*)((LPBYTE)pData->profiledRsp + (index * sizeof(SIZE_T)));
#endif // UNIX_AMD64_ABI
            }
        }
        else
        {
            EECodeInfo codeInfo((PCODE)pData->ip);

            // We want to pass the caller SP here.
            pData->hiddenArg = EECodeManager::GetExactGenericsToken((SIZE_T)(pData->profiledRsp), &codeInfo);
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

#ifdef UNIX_AMD64_ABI
LPVOID ProfileArgIterator::CopyStructFromRegisters()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_MODE_COOPERATIVE;

 
    PROFILE_PLATFORM_SPECIFIC_DATA* pData = (PROFILE_PLATFORM_SPECIFIC_DATA*)m_handle;
    ArgLocDesc *argLocDesc = m_argIterator.GetArgLocDescForStructInRegs();

    LPVOID dest = (LPVOID)&pData->buffer[m_bufferPos];
    BYTE* genRegSrc = (BYTE*)&pData->rdi + argLocDesc->m_idxGenReg * 8;
    BYTE* floatRegSrc = (BYTE*)&pData->flt0 + argLocDesc->m_idxFloatReg * 8;

    TypeHandle th;
    m_argIterator.GetArgType(&th);
    int fieldBytes = th.AsMethodTable()->GetNumInstanceFieldBytes();
    INDEBUG(int remainingBytes = fieldBytes;)

    EEClass* eeClass = argLocDesc->m_eeClass;
    _ASSERTE(eeClass != NULL);

    for (int i = 0; i < eeClass->GetNumberEightBytes(); i++)
    {
        int eightByteSize = eeClass->GetEightByteSize(i);
        SystemVClassificationType eightByteClassification = eeClass->GetEightByteClassification(i);

        _ASSERTE(remainingBytes >= eightByteSize);

        if (eightByteClassification == SystemVClassificationTypeSSE)
        {
            if (eightByteSize == 8)
            {
                *(UINT64*)dest = *(UINT64*)floatRegSrc ;
            }
            else
            {
                _ASSERTE(eightByteSize == 4);
                *(UINT32*)dest = *(UINT32*)floatRegSrc;
            }
            floatRegSrc += 8;
        }
        else
        {
            if (eightByteSize == 8)
            {
                _ASSERTE((eightByteClassification == SystemVClassificationTypeInteger) ||
                         (eightByteClassification == SystemVClassificationTypeIntegerReference) ||
                         (eightByteClassification == SystemVClassificationTypeIntegerByRef));

                _ASSERTE(IS_ALIGNED((SIZE_T)genRegSrc, 8));
                *(UINT64*)dest = *(UINT64*)genRegSrc;
            }
            else
            {
                _ASSERTE(eightByteClassification == SystemVClassificationTypeInteger);
                memcpyNoGCRefs(dest, genRegSrc, eightByteSize);
            }

            genRegSrc += eightByteSize;
        }

        dest = (BYTE*)dest + eightByteSize;
        INDEBUG(remainingBytes -= eightByteSize;)
    }

    _ASSERTE(remainingBytes == 0);
    LPVOID destOrig = (LPVOID)&pData->buffer[m_bufferPos];
    //Increase bufferPos by ceiling(fieldBytes/8)
    m_bufferPos += (fieldBytes + 7) / 8;
    _ASSERTE(m_bufferPos <= PROFILE_PLATFORM_SPECIFIC_DATA_BUFFER_SIZE);

    return destOrig;
}
#endif // UNIX_AMD64_ABI

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

    // stack args are offset against the profiledRsp
    if (TransitionBlock::IsStackArgumentOffset(argOffset))
    {
        LPVOID pArg = ((LPBYTE)pData->profiledRsp) + (argOffset - TransitionBlock::GetOffsetOfArgs());

        if (m_argIterator.IsArgPassedByRef())
            pArg = *(LPVOID *)pArg;

        return pArg;
    }

    // if we're here we have an enregistered argument
    CorElementType argType = m_argIterator.GetArgType();
#if defined(UNIX_AMD64_ABI)
    if (argOffset == TransitionBlock::StructInRegsOffset)
    {
        LPVOID argPtr = CopyStructFromRegisters();
        return argPtr;
    }
    else
    {
        ArgLocDesc argLocDesc;
        m_argIterator.GetArgLoc(argOffset, &argLocDesc);

        if (argLocDesc.m_cFloatReg > 0)
        {
            return (LPBYTE)&pData->flt0 + (argLocDesc.m_idxFloatReg * 8);
        }
        else
        {
            // Stack arguments and float registers are already dealt with,
            // so it better be a general purpose register
            _ASSERTE(argLocDesc.m_cGenReg > 0);
            return (LPBYTE)&pData->rdi + (argLocDesc.m_idxGenReg * 8);
        }
    }
#else // UNIX_AMD64_ABI
    unsigned int regStructOfs = (argOffset - TransitionBlock::GetOffsetOfArgumentRegisters());
    _ASSERTE(regStructOfs < ARGUMENTREGISTERS_SIZE);

    _ASSERTE(IS_ALIGNED(regStructOfs, sizeof(SLOT)));    
    if (argType == ELEMENT_TYPE_R4 || argType == ELEMENT_TYPE_R8)
    {
        return (LPBYTE)&pData->flt0 + regStructOfs;
    }
    else
    {
        // enregistered args (which are really stack homed) are offset against profiledRsp
        LPVOID pArg = ((LPBYTE)pData->profiledRsp + regStructOfs);

        if (m_argIterator.IsArgPassedByRef())
            pArg = *(LPVOID *)pArg;

        return pArg;
    }
#endif // UNIX_AMD64_ABI

    return NULL;
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
#ifdef UNIX_AMD64_ABI
            return (LPVOID)pData->rdi;
#else
            return *(LPVOID*)((LPBYTE)pData->profiledRsp);
#endif // UNIX_AMD64_ABI
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

    if (m_argIterator.HasRetBuffArg())
    {
        // the JIT64 makes sure that in ret-buf-arg cases where the method is being profiled that
        // rax is setup with the address of caller passed in buffer. this is _questionably_ required
        // by our calling convention, but is required by our profiler spec.
        return (LPVOID)pData->rax;
    }

    CorElementType t = m_argIterator.GetSig()->GetReturnType();    
    if (ELEMENT_TYPE_VOID != t)
    {
        if (ELEMENT_TYPE_R4 == t || ELEMENT_TYPE_R8 == t)
            pData->rax = pData->flt0;

        return &(pData->rax);
    }
    else
        return NULL;
}

#undef PROFILE_ENTER
#undef PROFILE_LEAVE
#undef PROFILE_TAILCALL

#endif // PROFILING_SUPPORTED

