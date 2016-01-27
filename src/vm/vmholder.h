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
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        value->Release();
        END_SO_TOLERANT_CODE_CALLING_HOST;

    }
}

NEW_WRAPPER_TEMPLATE1(HostComHolder, DoTheReleaseHost<_TYPE>);

#endif
