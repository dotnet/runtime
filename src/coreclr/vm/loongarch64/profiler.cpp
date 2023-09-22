// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef PROFILING_SUPPORTED
#include "proftoeeinterfaceimpl.h"
#include "asmconstants.h"

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
    m_bufferPos = 0;

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
                // On LoongArch64 the generic instantiation parameter comes after the optional "this" pointer.
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

    LPVOID pArg = nullptr;

    if (TransitionBlock::IsFloatArgumentRegisterOffset(argOffset))
    {
        pArg = (LPBYTE)&pData->floatArgumentRegisters + (argOffset - TransitionBlock::GetOffsetOfFloatArgumentRegisters());

        ArgLocDesc* pArgLocDesc = m_argIterator.GetArgLocDescForStructInRegs();

        if (pArgLocDesc)
        {
            if (pArgLocDesc->m_cFloatReg == 1)
            {
                UINT32 bufferPos = m_bufferPos;

                UINT64* dst = (UINT64*)&pData->buffer[bufferPos];
                m_bufferPos += 16;
                if (pArgLocDesc->m_structFields & STRUCT_FLOAT_FIELD_FIRST)
                {
                    *dst++ = *(UINT64*)pArg;
                    *dst = pData->argumentRegisters.a[pArgLocDesc->m_idxGenReg];
                }
                else
                {
                    _ASSERTE(pArgLocDesc->m_structFields & STRUCT_FLOAT_FIELD_SECOND);
                    *dst++ = pData->argumentRegisters.a[pArgLocDesc->m_idxGenReg];
                    *dst = *(UINT64*)pArg;
                }
                return (LPBYTE)&pData->buffer[bufferPos];
            }

            _ASSERTE(pArgLocDesc->m_cFloatReg == 2);
        }

        return pArg;
    }

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
        return (LPVOID)pData->argumentRegisters.a[0];
    }

    UINT fpReturnSize = m_argIterator.GetFPReturnSize();

    if (fpReturnSize != 0)
    {
        if ((fpReturnSize & (UINT)STRUCT_FLOAT_FIELD_ONLY_ONE) || (fpReturnSize & (UINT)STRUCT_FLOAT_FIELD_ONLY_TWO))
        {
            return &pData->floatArgumentRegisters.f[0];
        }
        else
        {
            // If the return type is a structure including floating types and return by floating register.
            // As we shared the scratch space, before calling the GetReturnBufferAddr,
            // Make sure within the PROFILE_LEAVE stage!!!
            _ASSERTE((pData->flags & PROFILE_LEAVE) != 0);

            // using the tail 16 bytes for return structure.
            UINT64* dst = (UINT64*)&pData->buffer[sizeof(pData->buffer) - 16];
            if (fpReturnSize & (UINT)STRUCT_FLOAT_FIELD_FIRST)
            {
                *(double*)dst = pData->floatArgumentRegisters.f[0];
                *(dst + 1) = pData->argumentRegisters.a[0];
            }
            else
            {
                _ASSERTE(fpReturnSize & (UINT)STRUCT_FLOAT_FIELD_SECOND);
                *dst = pData->argumentRegisters.a[0];
                *(double*)(dst + 1) = pData->floatArgumentRegisters.f[0];
            }
            return dst;
        }
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
