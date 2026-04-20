// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class Object;

#include "daccess.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "CommonMacros.inl"
#include "GCMemoryHelpers.inl"

#include "wasm.h"

FCIMPL_NO_SS(void, RhpAssignRef, Object** dst, Object* ref)
{
    // Note: the caller (codegen) is responsible for null checking.
    *dst = ref;
    InlineWriteBarrier(dst, ref);
}
FCIMPLEND

FCIMPL_NO_SS(void, RhpCheckedAssignRef, Object** dst, Object* ref)
{
    // Note: the caller (codegen) is responsible for null checking.
    *dst = ref;
    InlineCheckedWriteBarrier(dst, ref);
}
FCIMPLEND

FCIMPL2(void, RhpAssignRefWithShadowStack, Object** dst, Object* ref)
{
    // This "overload" is only used by the class library.
    ASSERT(dst != nullptr);
    return RhpAssignRef(dst, ref);
}
FCIMPLEND
