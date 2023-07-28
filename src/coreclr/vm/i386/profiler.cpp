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

//
// The following structure is the format on x86 builds of the data
// being passed in platformSpecificHandle for ProfileEnter/Leave/Tailcall
//
typedef struct _PROFILE_PLATFORM_SPECIFIC_DATA
{
    FunctionID functionId;
    DWORD    doubleBuffer1;
    DWORD    doubleBuffer2;
    DWORD    floatBuffer;
    DWORD    floatingPointValuePresent;
    UINT_PTR eax; // eax and edx must be continuous in this structure to make getting 64 bit return values easier.
    UINT_PTR edx;
    UINT_PTR ecx;
    UINT_PTR esp;
    UINT_PTR ip;
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

    return ((PROFILE_PLATFORM_SPECIFIC_DATA *)handle)->ip;
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
ProfileArgIterator::ProfileArgIterator(MetaSig * pMetaSig, void * platformSpecificHandle):
    m_argIterator(pMetaSig)
{
    //
    // It would be really nice to contract this, but the underlying functions are convolutedly
    // contracted.  Basically everything should be loaded by the time the profiler gets a call
    // back, so everything is NOTHROW/NOTRIGGER, but there is not mechanism for saying that the
    // contracts in called functions should be for the best case, not the worst case, now.
    //
    WRAPPER_NO_CONTRACT;

    m_handle = platformSpecificHandle;
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
    //
    // It would be really nice to contract this, but the underlying functions are convolutedly
    // contracted.  Basically everything should be loaded by the time the profiler gets a call
    // back, so everything is NOTHROW/NOTRIGGER, but there is not mechanism for saying that the
    // contracts in called functions should be for the best case, not the worst case, now.
    //
    WRAPPER_NO_CONTRACT;

    int argOffset = m_argIterator.GetNextOffset();

    //
    // Value is enregistered, figure out where and return that.
    //
    PROFILE_PLATFORM_SPECIFIC_DATA *pData = (PROFILE_PLATFORM_SPECIFIC_DATA *)m_handle;

    //
    // Zero indicates the end of the args.
    //
    if (argOffset == TransitionBlock::InvalidOffset)
    {
        return NULL;
    }

    if (pData == NULL)
    {
        //
        // Something wrong.
        //
        _ASSERTE(!"Why do we have a NULL data pointer here?");
        return NULL;
    }

    //
    // If this is not enregistered, return the value
    //
    if (TransitionBlock::IsStackArgumentOffset(argOffset))
    {
        return ((LPBYTE)pData->esp) + (argOffset - TransitionBlock::GetOffsetOfArgs());
    }

    switch (argOffset - TransitionBlock::GetOffsetOfArgumentRegisters())
    {
    case offsetof(ArgumentRegisters, ECX):
        return &(pData->ecx);
    case offsetof(ArgumentRegisters, EDX):
        return &(pData->edx);
    }

    _ASSERTE(!"Arg is an unsaved register!");
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
    //
    // It would be really nice to contract this, but the underlying functions are convolutedly
    // contracted.  Basically everything should be loaded by the time the profiler gets a call
    // back, so everything is NOTHROW/NOTRIGGER, but there is not mechanism for saying that the
    // contracts in called functions should be for the best case, not the worst case, now.
    //
    WRAPPER_NO_CONTRACT;

    PROFILE_PLATFORM_SPECIFIC_DATA *pData = (PROFILE_PLATFORM_SPECIFIC_DATA *)m_handle;

    MethodDesc *pMethodDesc = FunctionIdToMethodDesc(pData->functionId);

    if (!pMethodDesc->RequiresInstArg())
    {
        return NULL;
    }

    //
    // The ArgIterator::GetParamTypeOffset() can only be called after calling GetNextOffset until the
    // entire signature has been walked, but *before* GetNextOffset returns TransitionBlock::InvalidOffset
    // - indicating the end.
    //

    //
    // Get the offset of the hidden arg
    //
    int argOffset = m_argIterator.GetParamTypeArgOffset();

    //
    // If this is not enregistered, return the value
    //
    if (TransitionBlock::IsStackArgumentOffset(argOffset))
    {
        return *(LPVOID *)(((LPBYTE)pData->esp) + (argOffset - TransitionBlock::GetOffsetOfArgs()));
    }

    switch (argOffset - TransitionBlock::GetOffsetOfArgumentRegisters())
    {
    case offsetof(ArgumentRegisters, ECX):
        return (LPVOID)(pData->ecx);
    case offsetof(ArgumentRegisters, EDX):
        return (LPVOID)(pData->edx);
    }

    _ASSERTE(!"Arg is an unsaved register!");
    return NULL;
}

/*
 * ProfileArgIterator::GetThis
 *
 * Called after initialization, any number of times, to retrieve the
 * value of 'this'.
 *
 * Parameters:
 *    None.
 *
 * Returns:
 *    value of the 'this' parameter, or NULL if none exists.
 */
LPVOID ProfileArgIterator::GetThis(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PROFILE_PLATFORM_SPECIFIC_DATA *pData = (PROFILE_PLATFORM_SPECIFIC_DATA *)m_handle;

    if (pData->ip == 0)
    {
        return NULL;
    }

    if (!m_argIterator.HasThis())
    {
        return NULL;
    }

    switch (offsetof(ArgumentRegisters, THIS_REG))
    {
    case offsetof(ArgumentRegisters, ECX):
        return (LPVOID)pData->ecx;

    case offsetof(ArgumentRegisters, EDX):
        return (LPVOID)pData->edx;
    }

    _ASSERTE(!"This is an unsaved register!");
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

    PROFILE_PLATFORM_SPECIFIC_DATA *pData = (PROFILE_PLATFORM_SPECIFIC_DATA *)m_handle;

    if (m_argIterator.HasRetBuffArg())
    {
        return (void *)(pData->eax);
    }

    switch (m_argIterator.GetSig()->GetReturnType())
    {
    case ELEMENT_TYPE_R8:
        _ASSERTE(pData->floatingPointValuePresent);
        return (void *)(&(pData->doubleBuffer1));

    case ELEMENT_TYPE_R4:
        _ASSERTE(pData->floatingPointValuePresent);
        return (void *)(&(pData->floatBuffer));

    default:
        return &(pData->eax);
    }
}

#endif // PROFILING_SUPPORTED

