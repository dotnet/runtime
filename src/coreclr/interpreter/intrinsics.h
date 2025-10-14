// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __INTERPRETER_INTRINSICS_H__
#define __INTERPRETER_INTRINSICS_H__

#include "../jit/namedintrinsiclist.h"

NamedIntrinsic GetNamedIntrinsic(COMP_HANDLE compHnd, CORINFO_METHOD_HANDLE compMethod, CORINFO_METHOD_HANDLE method);

template <typename T, int size>
inline constexpr unsigned ArrLen(T (&)[size])
{
    return size;
}

#endif // __INTERPRETER_INTRINSICS_H__
