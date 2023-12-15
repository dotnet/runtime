// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __RH_VOLATILE_H__
#define __RH_VOLATILE_H__

template<typename T>
inline
void VolatileStoreWithoutBarrier(T* pt, nullptr_t val)
{
    *(T volatile*)pt = val;
}

#endif // __RH_VOLATILE_H__
