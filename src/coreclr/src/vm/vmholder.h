// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef __VMHOLDER_H_
#define __VMHOLDER_H_

#include "holder.h"

template <typename TYPE>
inline void DoTheReleaseHost(TYPE *value)
{
    if (value)
    {
        value->Release();
    }
}

template<typename _TYPE>
using HostComHolder = SpecializedWrapper<_TYPE, DoTheReleaseHost<_TYPE>>;

#endif
