// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_OBJCBRIDGE

// Runtime headers
#include "common.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

#include <pinvokeoverride.h>

#define OBJC_MSGSEND "objc_msgSend"

namespace
{
    BOOL s_msgSendOverridden = FALSE;
    void* s_msgSendOverrides[ObjCBridgeNative::MsgSendFunction::Last + 1] = {};

    const char* ObjectiveCLibrary = "/usr/lib/libobjc.dylib";
    const char* MsgSendEntryPoints[ObjCBridgeNative::MsgSendFunction::Last + 1] =
    {
        OBJC_MSGSEND,
        OBJC_MSGSEND "_fpret",
        OBJC_MSGSEND "_stret",
        OBJC_MSGSEND "Super",
        OBJC_MSGSEND "Super_stret",
    };

    const void* STDMETHODCALLTYPE MessageSendPInvokeOverride(_In_z_ const char* libraryName, _In_z_ const char* entrypointName)
    {
        // All overrides are in libobjc
        if (strcmp(libraryName, ObjectiveCLibrary) != 0)
            return nullptr;

        // All overrides start with objc_msgSend
        if (strncmp(entrypointName, OBJC_MSGSEND, _countof(OBJC_MSGSEND) -1) != 0)
            return nullptr;

        for (int i = 0; i < _countof(MsgSendEntryPoints); ++i)
        {
            void* funcMaybe = s_msgSendOverrides[i];
            if (funcMaybe != nullptr
                && strcmp(entrypointName, MsgSendEntryPoints[i]) == 0)
            {
                return funcMaybe;
            }
        }

        return nullptr;
    }
}

BOOL QCALLTYPE ObjCBridgeNative::TrySetGlobalMessageSendCallback(
    _In_ MsgSendFunction msgSendFunction,
    _In_ void* fptr)
{
    QCALL_CONTRACT;

    bool success;

    BEGIN_QCALL;

    _ASSERTE(msgSendFunction >= 0 && msgSendFunction < _countof(s_msgSendOverrides));
    success = FastInterlockCompareExchangePointer(&s_msgSendOverrides[msgSendFunction], fptr, NULL) == NULL;

    // Set P/Invoke override callback if we haven't already
    if (success && FALSE == FastInterlockCompareExchange((LONG*)&s_msgSendOverridden, TRUE, FALSE))
        PInvokeOverride::SetPInvokeOverride(&MessageSendPInvokeOverride, PInvokeOverride::Source::ObjectiveCInterop);

    END_QCALL;

    return success ? TRUE : FALSE;
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

void QCALLTYPE ObjCBridgeNative::GetLifetimeMethods(
    _Out_ void** allocImpl,
    _Out_ void** deallocImpl)
{
    QCALL_CONTRACT;
    _ASSERTE(allocImpl != NULL && deallocImpl != NULL);

    BEGIN_QCALL;

    // [TODO] Call out to Objective-C bridge binary instead
    *allocImpl = (void*)&objc_alloc;
    *deallocImpl = (void*)&objc_dealloc;

    END_QCALL;
}

#endif // FEATURE_OBJCBRIDGE
