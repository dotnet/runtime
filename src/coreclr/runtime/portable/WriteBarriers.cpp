// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>

EXTERN_C FCDECL2(VOID, JIT_WriteBarrier, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("JIT_WriteBarrier is not yet implemented");
}

EXTERN_C FCDECL2(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("JIT_CheckedWriteBarrier is not yet implemented");
}

EXTERN_C void STDCALL JIT_ByRefWriteBarrier()
{
    PORTABILITY_ASSERT("JIT_ByRefWriteBarrier is not yet implemented");
}

EXTERN_C FCDECL2(VOID, RhpAssignRef, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("RhpAssignRef is not yet implemented");
}

EXTERN_C FCDECL2(VOID, RhpCheckedAssignRef, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("RhpCheckedAssignRef is not yet implemented");
}

EXTERN_C FCDECL2(VOID, RhpByRefAssignRef, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("RhpByRefAssignRef is not yet implemented");
}
