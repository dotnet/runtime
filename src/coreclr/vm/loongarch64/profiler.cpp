// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef PROFILING_SUPPORTED
#include "proftoeeinterfaceimpl.h"

#define PROFILE_ENTER    1
#define PROFILE_LEAVE    2
#define PROFILE_TAILCALL 4

// Scratch space to store HFA return values (max 16 bytes)
#define PROFILE_PLATFORM_SPECIFIC_DATA_BUFFER_SIZE 16

typedef struct _PROFILE_PLATFORM_SPECIFIC_DATA
{
    void*                  Fp;
    void*                  Pc;
    void*                  x8;
    ArgumentRegisters      argumentRegisters;
    FunctionID             functionId;
    FloatArgumentRegisters floatArgumentRegisters;
    void*                  probeSp;
    void*                  profiledSp;
    void*                  hiddenArg;
    UINT32                 flags;
    UINT32                 unused;
    BYTE                   buffer[PROFILE_PLATFORM_SPECIFIC_DATA_BUFFER_SIZE];
} PROFILE_PLATFORM_SPECIFIC_DATA, *PPROFILE_PLATFORM_SPECIFIC_DATA;

UINT_PTR ProfileGetIPFromPlatformSpecificHandle(void* pPlatformSpecificHandle)
{
    LIMITED_METHOD_CONTRACT;

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = reinterpret_cast<PROFILE_PLATFORM_SPECIFIC_DATA*>(pPlatformSpecificHandle);
    return (UINT_PTR)pData->Pc;
}

void ProfileSetFunctionIDInPlatformSpecificHandle(void* pPlatformSpecificHandle, FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pPlatformSpecificHandle != nullptr);
    _ASSERTE(functionId != 0);

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = reinterpret_cast<PROFILE_PLATFORM_SPECIFIC_DATA*>(pPlatformSpecificHandle);
    pData->functionId = functionId;
}

ProfileArgIterator::ProfileArgIterator(MetaSig* pSig, void* pPlatformSpecificHandle)
    : m_argIterator(pSig)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pSig != nullptr);
    _ASSERTE(pPlatformSpecificHandle != nullptr);

    m_handle = pPlatformSpecificHandle;

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = reinterpret_cast<PROFILE_PLATFORM_SPECIFIC_DATA*>(pPlatformSpecificHandle);
#ifdef _DEBUG
    // Unwind a frame and get the SP for the profiled method to make sure it matches
    // what the JIT gave us

    // Setup the context to represent the frame that called ProfileEnterNaked
    CONTEXT ctx;
    memset(&ctx, 0, sizeof(CONTEXT));

    ctx.Sp = (DWORD64)pData->probeSp;
    ctx.Fp = (DWORD64)pData->Fp;
    ctx.Pc = (DWORD64)pData->Pc;

    // Walk up a frame to the caller frame (called the managed method which called ProfileEnterNaked)
    Thread::VirtualUnwindCallFrame(&ctx);

    _ASSERTE(pData->profiledSp == (void*)ctx.Sp);
#endif

    // Get the hidden arg if there is one
    MethodDesc* pMD = FunctionIdToMethodDesc(pData->functionId);

    if ((pData->hiddenArg == nullptr) && (pMD->RequiresInstArg() || pMD->AcquiresInstMethodTableFromThis()))
    {
        if ((pData->flags & PROFILE_ENTER) != 0)
        {
            if (pMD->AcquiresInstMethodTableFromThis())
            {
                pData->hiddenArg = GetThis();
            }
            else
            {
                // On ARM64 the generic instantiation parameter comes after the optional "this" pointer.
                if (m_argIterator.HasThis())
                {
                    pData->hiddenArg = (void*)pData->argumentRegisters.a[1];
                }
                else
                {
                    pData->hiddenArg = (void*)pData->argumentRegisters.a[0];
                }
            }
        }
        else
        {
            EECodeInfo codeInfo((PCODE)pData->Pc);

            // We want to pass the caller SP here.
            pData->hiddenArg = EECodeManager::GetExactGenericsToken((SIZE_T)(pData->profiledSp), &codeInfo);
        }
    }
}

ProfileArgIterator::~ProfileArgIterator()
{
    LIMITED_METHOD_CONTRACT;

    m_handle = nullptr;
}

LPVOID ProfileArgIterator::GetNextArgAddr()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_handle != nullptr);

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = reinterpret_cast<PROFILE_PLATFORM_SPECIFIC_DATA*>(m_handle);

    if ((pData->flags & (PROFILE_LEAVE | PROFILE_TAILCALL)) != 0)
    {
        _ASSERTE(!"GetNextArgAddr() - arguments are not available in leave and tailcall probes");
        return nullptr;
    }

    int argOffset = m_argIterator.GetNextOffset();

    if (argOffset == TransitionBlock::InvalidOffset)
    {
        return nullptr;
    }

    if (TransitionBlock::IsFloatArgumentRegisterOffset(argOffset))
    {
        return (LPBYTE)&pData->floatArgumentRegisters + (argOffset - TransitionBlock::GetOffsetOfFloatArgumentRegisters());
    }

    LPVOID pArg = nullptr;

    if (TransitionBlock::IsArgumentRegisterOffset(argOffset))
    {
        pArg = (LPBYTE)&pData->argumentRegisters + (argOffset - TransitionBlock::GetOffsetOfArgumentRegisters());
    }
    else
    {
        _ASSERTE(TransitionBlock::IsStackArgumentOffset(argOffset));

        pArg = (LPBYTE)pData->profiledSp + (argOffset - TransitionBlock::GetOffsetOfArgs());
    }

    if (m_argIterator.IsArgPassedByRef())
    {
        pArg = *(LPVOID*)pArg;
    }

    return pArg;
}

LPVOID ProfileArgIterator::GetHiddenArgValue(void)
{
    LIMITED_METHOD_CONTRACT;

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = reinterpret_cast<PROFILE_PLATFORM_SPECIFIC_DATA*>(m_handle);

    return pData->hiddenArg;
}

LPVOID ProfileArgIterator::GetThis(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = (PROFILE_PLATFORM_SPECIFIC_DATA*)m_handle;
    MethodDesc* pMD = FunctionIdToMethodDesc(pData->functionId);

    // We guarantee to return the correct "this" pointer in the enter probe.
    // For the leave and tailcall probes, we only return a valid "this" pointer if it is the generics token.
    if (pData->hiddenArg != nullptr)
    {
        if (pMD->AcquiresInstMethodTableFromThis())
        {
            return pData->hiddenArg;
        }
    }

    if ((pData->flags & PROFILE_ENTER) != 0)
    {
        if (m_argIterator.HasThis())
        {
            return (LPVOID)pData->argumentRegisters.a[0];
        }
    }

    return nullptr;
}

LPVOID ProfileArgIterator::GetReturnBufferAddr(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PROFILE_PLATFORM_SPECIFIC_DATA* pData = reinterpret_cast<PROFILE_PLATFORM_SPECIFIC_DATA*>(m_handle);

    if ((pData->flags & PROFILE_TAILCALL) != 0)
    {
        _ASSERTE(!"GetReturnBufferAddr() - return buffer address is not available in tailcall probe");
        return nullptr;
    }

    if (m_argIterator.HasRetBuffArg())
    {
        if ((pData->flags & PROFILE_ENTER) != 0)
        {
            return (LPVOID)pData->x8;
        }
        else
        {
            // On ARM64 there is no requirement for the method to preserve the value stored in x8.
            // In order to workaround this JIT will explicitly return the return buffer address in x0.
            _ASSERTE((pData->flags & PROFILE_LEAVE) != 0);
            return (LPVOID)pData->argumentRegisters.a[0];
        }
    }

    UINT fpReturnSize = m_argIterator.GetFPReturnSize();
    if (fpReturnSize != 0)
    {
        TypeHandle thReturnValueType;
        m_argIterator.GetSig()->GetReturnTypeNormalized(&thReturnValueType);
        if (!thReturnValueType.IsNull() && thReturnValueType.IsHFA())
        {
            UINT hfaFieldSize = fpReturnSize / 4;
            UINT totalSize = m_argIterator.GetSig()->GetReturnTypeSize();
            _ASSERTE(totalSize % hfaFieldSize == 0);
            _ASSERTE(totalSize <= 16);

            BYTE *dest = pData->buffer;
            for (UINT floatRegIdx = 0; floatRegIdx < totalSize / hfaFieldSize; ++floatRegIdx)
            {
                if (hfaFieldSize == 4)
                {
                    *(UINT32*)dest = *(UINT32*)&pData->floatArgumentRegisters.f[floatRegIdx];
                    dest += 4;
                }
                else if (hfaFieldSize == 8)
                {
                    *(UINT64*)dest = *(UINT64*)&pData->floatArgumentRegisters.f[floatRegIdx];
                    dest += 8;
                }
                else
                {
                    _ASSERTE(!"unimplemented on LOONGARCH yet!");
#if 0
                    _ASSERTE(hfaFieldSize == 16);
                    *(NEON128*)dest = pData->floatArgumentRegisters.f[floatRegIdx];
                    dest += 16;
#endif
                }

                if (floatRegIdx > 8)
                {
                    // There's only space for 8 arguments in buffer
                    _ASSERTE(FALSE);
                    break;
                }
            }

            return pData->buffer;
        }

        return &pData->floatArgumentRegisters.f[0];
    }

    if (!m_argIterator.GetSig()->IsReturnTypeVoid())
    {
        return &pData->argumentRegisters.a[0];
    }

    return nullptr;
}

#undef PROFILE_ENTER
#undef PROFILE_LEAVE
#undef PROFILE_TAILCALL

#endif // PROFILING_SUPPORTED
