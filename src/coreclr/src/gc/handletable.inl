// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

#ifndef _HANDLETABLE_INL
#define _HANDLETABLE_INL

inline void HndAssignHandle(OBJECTHANDLE handle, OBJECTREF objref)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // sanity
    _ASSERTE(handle);

    // unwrap the objectref we were given
    _UNCHECKED_OBJECTREF value = OBJECTREF_TO_UNCHECKED_OBJECTREF(objref);

    HndLogSetEvent(handle, value);

    // if we are doing a non-NULL pointer store then invoke the write-barrier
    if (value)
        HndWriteBarrier(handle, objref);

    // store the pointer
    *(_UNCHECKED_OBJECTREF *)handle = value;
}

inline void* HndInterlockedCompareExchangeHandle(OBJECTHANDLE handle, OBJECTREF objref, OBJECTREF oldObjref)
{
    WRAPPER_NO_CONTRACT;

    // sanity
    _ASSERTE(handle);

    // unwrap the objectref we were given
    _UNCHECKED_OBJECTREF value = OBJECTREF_TO_UNCHECKED_OBJECTREF(objref);
    _UNCHECKED_OBJECTREF oldValue = OBJECTREF_TO_UNCHECKED_OBJECTREF(oldObjref);

    // if we are doing a non-NULL pointer store then invoke the write-barrier
    if (value)
        HndWriteBarrier(handle, objref);

    // store the pointer

    void* ret = Interlocked::CompareExchangePointer(reinterpret_cast<_UNCHECKED_OBJECTREF volatile*>(handle), value, oldValue);

    if (ret == oldValue)
        HndLogSetEvent(handle, value);

    return ret;
}

inline BOOL HndFirstAssignHandle(OBJECTHANDLE handle, OBJECTREF objref)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // sanity
    _ASSERTE(handle);

    // unwrap the objectref we were given
    _UNCHECKED_OBJECTREF value = OBJECTREF_TO_UNCHECKED_OBJECTREF(objref);
    _UNCHECKED_OBJECTREF null = NULL;

    // store the pointer if we are the first ones here
    BOOL success = (NULL == Interlocked::CompareExchangePointer(reinterpret_cast<_UNCHECKED_OBJECTREF volatile*>(handle),
                                                                value,
                                                                null));

    // if we successfully did a non-NULL pointer store then invoke the write-barrier
    if (success)
    {
        if (value)
            HndWriteBarrier(handle, objref);

        HndLogSetEvent(handle, value);
    }

    // return our result
    return success;
}

#endif // _HANDLETABLE_INL
