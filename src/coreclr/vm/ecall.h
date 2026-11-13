// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ECALL.H -
//
// Handles our private native calling interface.
//




#ifndef _ECALL_H_
#define _ECALL_H_

#include "fcall.h"

class MethodDesc;

//=======================================================================
// Collects code and data pertaining to the ECall interface.
//=======================================================================
class ECall
{
    public:
        static PCODE GetFCallImpl(MethodDesc* pMD, bool throwForInvalidFCall = true, bool* pHasManagedImpl = nullptr);
        static void DynamicallyAssignFCallImpl(PCODE impl, DWORD index);

        static void PopulateManagedStringConstructors();

#define _DYNAMICALLY_ASSIGNED_FCALLS_BASE() \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(FastAllocateString,                RhpNewVariableSizeObject) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharArrayManaged,              NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharArrayStartLengthManaged,   NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharCountManaged,              NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharPtrManaged,                NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorCharPtrStartLengthManaged,     NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorReadOnlySpanOfCharManaged,     NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorSBytePtrManaged,               NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorSBytePtrStartLengthManaged,    NULL) \
    DYNAMICALLY_ASSIGNED_FCALL_IMPL(CtorSBytePtrStartLengthEncodingManaged, NULL) \

#define DYNAMICALLY_ASSIGNED_FCALLS() _DYNAMICALLY_ASSIGNED_FCALLS_BASE()

        enum
        {
            #undef DYNAMICALLY_ASSIGNED_FCALL_IMPL
            #define DYNAMICALLY_ASSIGNED_FCALL_IMPL(id,defaultimpl) id,

            DYNAMICALLY_ASSIGNED_FCALLS()

            NUM_DYNAMICALLY_ASSIGNED_FCALL_IMPLEMENTATIONS,
            InvalidDynamicFCallId = -1
        };
};

extern "C" FCDECL1(VOID, FCComCtor, LPVOID pV);

extern "C"
{
    FCDECL1(void, GCFrameRegistration_RegisterForGCReporting, GCFrame*);
    FCDECL1(void, GCFrameRegistration_UnregisterForGCReporting, GCFrame*);
}

#endif // _ECALL_H_
