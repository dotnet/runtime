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
#include "argdestination.h"


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
    _ASSERTE(!"S390X:NYI");
    return NULL;
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
    _ASSERTE(!"S390X:NYI");
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
    _ASSERTE(!"S390X:NYI");
}

/*
 * ProfileArgIterator::~ProfileArgIterator
 *
 * Destructor, releases all resources.
 *
 */
ProfileArgIterator::~ProfileArgIterator()
{
    _ASSERTE(!"S390X:NYI");
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
    _ASSERTE(!"S390X:NYI");
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
    _ASSERTE(!"S390X:NYI");
    return NULL;
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
    _ASSERTE(!"S390X:NYI");
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
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

#endif // PROFILING_SUPPORTED
