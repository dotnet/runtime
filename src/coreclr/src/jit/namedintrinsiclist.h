// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _NAMEDINTRINSICLIST_H_
#define _NAMEDINTRINSICLIST_H_

// Named jit intrinsics

enum NamedIntrinsic
{
    NI_Illegal      = 0,
    NI_Enum_HasFlag = 1,
    NI_MathF_Round  = 2,
    NI_Math_Round   = 3
};

#endif // _NAMEDINTRINSICLIST_H_
