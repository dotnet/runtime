// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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

NEW_WRAPPER_TEMPLATE1(HostComHolder, DoTheReleaseHost<_TYPE>);

#endif
