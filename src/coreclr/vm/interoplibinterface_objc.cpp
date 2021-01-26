// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_OBJCWRAPPERS

// Runtime headers
#include "common.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

BOOL QCALLTYPE ObjCWrappersNative::TrySetGlobalMessageSendCallbacks(
    void* fptr_objc_msgSend,
    void* fptr_objc_msgSend_fpret,
    void* fptr_objc_msgSend_stret,
    void* fptr_objc_msgSendSuper,
    void* fptr_objc_msgSendSuper_stret)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    END_QCALL;

    return FALSE;
}

namespace
{
    void* objc_alloc(void* self, void* sel)
    {
        return NULL;
    }

    void objc_dealloc(void* self, void* sel)
    {

    }
}

void QCALLTYPE ObjCWrappersNative::GetLifetimeMethods(
    void** allocImpl,
    void** deallocImpl)
{
    QCALL_CONTRACT;
    _ASSERTE(allocImpl != NULL && deallocImpl != NULL);

    BEGIN_QCALL;

    // [TODO] Call out to Objective-C bridge binary instead
    *allocImpl = (void*)&objc_alloc;
    *deallocImpl = (void*)&objc_dealloc;

    END_QCALL;
}

#endif // FEATURE_OBJCWRAPPERS
