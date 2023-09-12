// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef PROFILING_SUPPORTED
#include "asmconstants.h"
#include "proftoeeinterfaceimpl.h"

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
    : m_argIterator(pSig), m_bufferPos(0)
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

LPVOID ProfileArgIterator::CopyStructFromRegisters(const ArgLocDesc* sir)
{
    struct Func
    {
        static inline const BYTE* postIncrement(const BYTE *&p, int offset)
        {
            const BYTE* orig = p;
            p += offset;
            return orig;
        }
    };

    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_handle);
    PROFILE_PLATFORM_SPECIFIC_DATA* pData = reinterpret_cast<PROFILE_PLATFORM_SPECIFIC_DATA*>(m_handle);

    struct { bool isFloat, is8; } fields[] = {
        { sir->m_structFields & (STRUCT_FLOAT_FIELD_FIRST | STRUCT_FLOAT_FIELD_ONLY_TWO | STRUCT_FLOAT_FIELD_ONLY_ONE),
          sir->m_structFields & STRUCT_FIRST_FIELD_SIZE_IS8 },
        { sir->m_structFields & (STRUCT_FLOAT_FIELD_SECOND | STRUCT_FLOAT_FIELD_ONLY_TWO),
          sir->m_structFields & STRUCT_SECOND_FIELD_SIZE_IS8 },
    };
    int fieldCount = (sir->m_structFields & STRUCT_FLOAT_FIELD_ONLY_ONE) ? 1 : 2;
    UINT64 bufferPosBegin = m_bufferPos;
    const double *fRegBegin = &pData->floatArgumentRegisters.f[sir->m_idxFloatReg], *fReg = fRegBegin;
    const double *fRegEnd = &pData->floatArgumentRegisters.f[0] + NUM_FLOAT_ARGUMENT_REGISTERS;
    const INT64 *aRegBegin = &pData->argumentRegisters.a[sir->m_idxGenReg], *aReg = aRegBegin;
    const INT64 *aRegEnd = &pData->argumentRegisters.a[0] + NUM_ARGUMENT_REGISTERS;
    const BYTE *stackBegin = (BYTE*)pData->profiledSp + sir->m_byteStackIndex, *stack = stackBegin;

    for (int i = 0; i < fieldCount; ++i)
    {
        bool inFloatReg = fields[i].isFloat && fReg < fRegEnd;
        bool inGenReg = aReg < aRegEnd;

        if (fields[i].is8)
        {
            UINT64 alignedTo8 = ALIGN_UP(m_bufferPos, 8);
            _ASSERTE(alignedTo8 + 8 <= sizeof(pData->buffer));
            m_bufferPos = alignedTo8;
            const INT64* src =
                inFloatReg ? (const INT64*)fReg++ :
                inGenReg   ? aReg++ : (const INT64*)Func::postIncrement(stack, 8);
            *((INT64*)&pData->buffer[m_bufferPos]) = *src;
            m_bufferPos += 8;
        }
        else
        {
            _ASSERTE(m_bufferPos + 4 <= sizeof(pData->buffer));
            const INT32* src =
                inFloatReg ? (const INT32*)fReg++ :
                inGenReg   ? (const INT32*)aReg++ : (const INT32*)Func::postIncrement(stack, 4);
            *((INT32*)&pData->buffer[m_bufferPos]) = *src;
            m_bufferPos += 4;
        }
    }
    // Sanity checks, make sure we've run through (and not overrun) all locations from ArgLocDesc
    _ASSERTE(sir->m_cFloatReg < 0 || fReg - fRegBegin == sir->m_cFloatReg);
    _ASSERTE(sir->m_cGenReg < 0   || aReg - aRegBegin == sir->m_cGenReg);
    _ASSERTE(sir->m_byteStackSize < 0 || stack - stackBegin == sir->m_byteStackSize);

    return &pData->buffer[bufferPosBegin];
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

    const ArgLocDesc* sir = m_argIterator.GetArgLocDescForStructInRegs();
    if (sir)
    {
        // If both fields are in registers of same kind (either float or general) and both are 8 bytes, no need to copy.
        // We can get away with returning a ptr to argumentRegisters since the struct would have the same layout.
        if ((sir->m_cFloatReg ^ sir->m_cGenReg) != 2 ||
            (sir->m_structFields & STRUCT_HAS_8BYTES_FIELDS_MASK) != STRUCT_HAS_8BYTES_FIELDS_MASK)
        {
            return CopyStructFromRegisters(sir);
        }
    }

    int argSize = m_argIterator.IsArgPassedByRef() ? sizeof(void*) : m_argIterator.GetArgSize();
    if (TransitionBlock::IsFloatArgumentRegisterOffset(argOffset))
    {
        int offset = argOffset - TransitionBlock::GetOffsetOfFloatArgumentRegisters();
        _ASSERTE(offset + argSize <= sizeof(pData->floatArgumentRegisters));
        return (LPBYTE)&pData->floatArgumentRegisters + offset;
    }

    LPVOID pArg = nullptr;

    if (TransitionBlock::IsArgumentRegisterOffset(argOffset))
    {
        int offset = argOffset - TransitionBlock::GetOffsetOfArgumentRegisters();
        if (offset + argSize > sizeof(pData->argumentRegisters))
        {
            // Struct partially spilled on stack
            const int regIndex = NUM_ARGUMENT_REGISTERS - 1;  // first part of struct must be in last register
            _ASSERTE(regIndex == offset / sizeof(pData->argumentRegisters.a[0]));
            const int neededSpace = 2 * sizeof(INT64);
            _ASSERTE(argSize <= neededSpace);
            _ASSERTE(m_bufferPos + neededSpace <= sizeof(pData->buffer));
            INT64* dest = (INT64*)&pData->buffer[m_bufferPos];
            dest[0] = pData->argumentRegisters.a[regIndex];
            // spilled part must be first on stack (if we copy too much, that's ok)
            dest[1] = *(INT64*)pData->profiledSp;
            m_bufferPos += neededSpace;
            return dest;
        }
        pArg = (LPBYTE)&pData->argumentRegisters + offset;
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
        // On RISC-V the method is not required to preserve the return buffer address passed in a0.
        // However, JIT does that anyway if leave hook needs to be generated.
        _ASSERTE((pData->flags & PROFILE_LEAVE) != 0);
        return (LPVOID)pData->argumentRegisters.a[0];
    }

    UINT fpReturnSize = m_argIterator.GetFPReturnSize();
    if (fpReturnSize)
    {
        if ((fpReturnSize & STRUCT_HAS_8BYTES_FIELDS_MASK) == STRUCT_HAS_8BYTES_FIELDS_MASK ||
            (fpReturnSize & STRUCT_FLOAT_FIELD_ONLY_ONE))
        {
            return &pData->floatArgumentRegisters.f[0];
        }
        ArgLocDesc sir;
        sir.m_idxFloatReg = 0;
        sir.m_cFloatReg = -1;
        sir.m_idxGenReg = 0;
        sir.m_cGenReg = -1;
        sir.m_byteStackIndex = 0;
        sir.m_byteStackSize = -1;
        sir.m_structFields = fpReturnSize;
        return CopyStructFromRegisters(&sir);
    }

    if (!m_argIterator.GetSig()->IsReturnTypeVoid())
    {
        return &pData->argumentRegisters.a[0];
    }

    return nullptr;
}

#endif // PROFILING_SUPPORTED
