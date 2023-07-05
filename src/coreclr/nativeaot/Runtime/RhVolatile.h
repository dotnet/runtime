// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __VOLATILE_H__
#define __VOLATILE_H__

template<typename T>
inline
T VolatileLoadWithoutBarrier(T const* pt)
{
    T val = *(T volatile const*)pt;
    return val;
}

template<typename T>
inline
void VolatileStoreWithoutBarrier(T* pt, T val)
{
    *(T volatile*)pt = val;
}

template<typename T>
inline
void VolatileStoreWithoutBarrier(T* pt, nullptr_t val)
{
    *(T volatile*)pt = val;
}

#endif // __VOLATILE_H__
