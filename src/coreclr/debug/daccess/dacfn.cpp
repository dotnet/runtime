// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: dacfn.cpp
//

//
// Dac function implementations.
//
//*****************************************************************************

#include "stdafx.h"

#include <encee.h>
#include <virtualcallstub.h>
#include "peimagelayout.inl"

#include "gcinterface.h"
#include "gcinterface.dac.h"

DacTableInfo g_dacTableInfo;
DacGlobals g_dacGlobals;

struct DacHostVtPtrs
{
#define VPTR_CLASS(name) PVOID name;
#define VPTR_MULTI_CLASS(name, keyBase) PVOID name##__##keyBase;
#include <vptr_list.h>
#undef VPTR_CLASS
#undef VPTR_MULTI_CLASS
};


const WCHAR *g_dacVtStrings[] =
{
#define VPTR_CLASS(name) W(#name),
#define VPTR_MULTI_CLASS(name, keyBase) W(#name),
#include <vptr_list.h>
#undef VPTR_CLASS
#undef VPTR_MULTI_CLASS
};

DacHostVtPtrs g_dacHostVtPtrs;

HRESULT
DacGetHostVtPtrs(void)
{
#define VPTR_CLASS(name) \
    g_dacHostVtPtrs.name = name::VPtrHostVTable();
#define VPTR_MULTI_CLASS(name, keyBase) \
    g_dacHostVtPtrs.name##__##keyBase = name::VPtrHostVTable();
#include <vptr_list.h>
#undef VPTR_CLASS
#undef VPTR_MULTI_CLASS

    return S_OK;
}

bool
DacExceptionFilter(Exception* ex, ClrDataAccess* access,
                   HRESULT* status)
{
    SUPPORTS_DAC_HOST_ONLY;

    // The DAC support functions throw HRExceptions and
    // the underlying code can throw the normal set of
    // CLR exceptions.  Handle any exception
    // other than an unexpected SEH exception.
    // If we're not debugging, handle SEH exceptions also
    // so that dac absorbs all exceptions by default.
    if ((access && access->m_debugMode) &&
        ex->IsType(SEHException::GetType()))
    {
        // Indicate this exception should be rethrown.
        return FALSE;
    }

    // Indicate this exception is handled.
    // XXX Microsoft - The C++-based EH has broken the ability
    // to get proper SEH results.  Make sure that the
    // error returned is actually an error code as
    // often it's just zero.
    *status = ex->GetHR();
    if (!FAILED(*status))
    {
        *status = E_FAIL;
    }
    return TRUE;
}

void __cdecl
DacWarning(_In_ char* format, ...)
{
    char text[256];
    va_list args;

    va_start(args, format);
    _vsnprintf_s(text, sizeof(text), _TRUNCATE, format, args);
    text[sizeof(text) - 1] = 0;
    va_end(args);
    OutputDebugStringA(text);
}

void
DacNotImpl(void)
{
    EX_THROW(HRException, (E_NOTIMPL));
}

void
DacError(HRESULT err)
{
    EX_THROW(HRException, (err));
}

// Ideally DacNoImpl and DacError would be marked no-return, but that will require changing a bunch of existing
// code to avoid "unreachable code" warnings.
void DECLSPEC_NORETURN
DacError_NoRet(HRESULT err)
{
    EX_THROW(HRException, (err));
}

TADDR
DacGlobalBase(void)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    return g_dacImpl->m_globalBase;
}

HRESULT
DacReadAll(TADDR addr, PVOID buffer, ULONG32 size, bool throwEx)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    ClrSafeInt<TADDR> end = ClrSafeInt<TADDR>(addr) + ClrSafeInt<TADDR>(size);
    if( end.IsOverflow() )
    {
        // Overflow - corrupt data
        DacError(CORDBG_E_TARGET_INCONSISTENT);
    }

    HRESULT status;
    ULONG32 returned;

#if defined(DAC_MEASURE_PERF)
    unsigned __int64  nStart, nEnd;
    nStart = GetCycleCount();
#endif // #if defined(DAC_MEASURE_PERF)

    status = g_dacImpl->m_pTarget->
        ReadVirtual(addr, (PBYTE)buffer, size, &returned);

#if defined(DAC_MEASURE_PERF)
    nEnd = GetCycleCount();
    g_nReadVirtualTotalTime += nEnd - nStart;
#endif // #if defined(DAC_MEASURE_PERF)

    if (status != S_OK)
    {
        // Regardless of what status is, it's very important for dump debugging to
        // always return CORDBG_E_READVIRTUAL_FAILURE.
        if (throwEx)
        {
            DacError(CORDBG_E_READVIRTUAL_FAILURE);
        }
        return CORDBG_E_READVIRTUAL_FAILURE;
    }
    if (returned != size)
    {
        if (throwEx)
        {
            DacError(HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY));
        }
        return HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);
    }

    return S_OK;
}

HRESULT
DacWriteAll(TADDR addr, PVOID buffer, ULONG32 size, bool throwEx)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    HRESULT status;

    status = g_dacImpl->m_pMutableTarget->WriteVirtual(addr, (PBYTE)buffer, size);
    if (status != S_OK)
    {
        if (throwEx)
        {
            DacError(status);
        }
        return status;
    }

    return S_OK;
}

#ifdef TARGET_UNIX

static BOOL DacReadAllAdapter(PVOID address, PVOID buffer, SIZE_T size)
{
    DAC_INSTANCE* inst = g_dacImpl->m_instances.Find((TADDR)address);
    if (inst == nullptr || inst->size < size)
    {
        inst = g_dacImpl->m_instances.Alloc((TADDR)address, (ULONG32)size, DAC_PAL);
        if (inst == nullptr)
        {
            return FALSE;
        }
        inst->noReport = 0;
        HRESULT hr = DacReadAll((TADDR)address, inst + 1, (ULONG32)size, false);
        if (FAILED(hr))
        {
            g_dacImpl->m_instances.ReturnAlloc(inst);
            return FALSE;
        }
        if (!g_dacImpl->m_instances.Add(inst))
        {
            g_dacImpl->m_instances.ReturnAlloc(inst);
            return FALSE;
        }
    }
    memcpy(buffer, inst + 1, size);
    return TRUE;
}

#ifdef HOST_WINDOWS
// For the cross OS dac, we don't have the full pal layer
// Use these minimal prototypes instead of the full pal header
typedef BOOL(*UnwindReadMemoryCallback)(PVOID address, PVOID buffer, SIZE_T size);

extern
BOOL
PAL_VirtualUnwindOutOfProc(PT_CONTEXT context, PT_KNONVOLATILE_CONTEXT_POINTERS contextPointers, PULONG64 functionStart, SIZE_T baseAddress, UnwindReadMemoryCallback readMemoryCallback);
#endif

HRESULT
DacVirtualUnwind(ULONG32 threadId, PT_CONTEXT context, PT_KNONVOLATILE_CONTEXT_POINTERS contextPointers)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    // The DAC code doesn't use these context pointers but zero them out to be safe.
    if (contextPointers != NULL)
    {
        memset(contextPointers, 0, sizeof(T_KNONVOLATILE_CONTEXT_POINTERS));
    }

    HRESULT hr = E_NOINTERFACE;

#ifdef FEATURE_DATATARGET4
    ReleaseHolder<ICorDebugDataTarget4> dt;
    hr = g_dacImpl->m_pTarget->QueryInterface(IID_ICorDebugDataTarget4, (void **)&dt);
    if (SUCCEEDED(hr))
    {
        hr = dt->VirtualUnwind(threadId, sizeof(CONTEXT), (BYTE*)context);
    }
#endif

    if (hr == E_NOINTERFACE || hr == E_NOTIMPL)
    {
        hr = S_OK;

        SIZE_T baseAddress = DacGlobalBase();
        if (baseAddress == 0 || !PAL_VirtualUnwindOutOfProc(context, contextPointers, nullptr, baseAddress, DacReadAllAdapter))
        {
            hr = E_FAIL;
        }
    }

    return hr;
}

#endif // TARGET_UNIX

// DacAllocVirtual - Allocate memory from the target process
// Note: this is only available to clients supporting the legacy
// ICLRDataTarget2 interface.  It's currently used by SOS for notification tables.
HRESULT
DacAllocVirtual(TADDR addr, ULONG32 size,
                ULONG32 typeFlags, ULONG32 protectFlags,
                bool throwEx, TADDR* mem)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    ICLRDataTarget2 * pTarget2 = g_dacImpl->GetLegacyTarget2();
    if (pTarget2 == NULL)
    {
        DacError(E_NOTIMPL);
        UNREACHABLE();
    }

    CLRDATA_ADDRESS cdaMem;
    HRESULT status = pTarget2->AllocVirtual(
        TO_CDADDR(addr), size, typeFlags, protectFlags, &cdaMem);
    if (status != S_OK)
    {
        if (throwEx)
        {
            DacError(status);
            UNREACHABLE();
        }

        return status;
    }

    *mem = CLRDATA_ADDRESS_TO_TADDR(cdaMem);
    return S_OK;
}

// DacFreeVirtual - Free memory from the target process
// Note: this is only available to clients supporting the legacy
// ICLRDataTarget2 interface.  This is not currently used.
HRESULT
DacFreeVirtual(TADDR mem, ULONG32 size, ULONG32 typeFlags,
               bool throwEx)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    ICLRDataTarget2 * pTarget2 = g_dacImpl->GetLegacyTarget2();
    if (pTarget2 == NULL)
    {
        DacError(E_NOTIMPL);
        UNREACHABLE();
    }

    HRESULT status = pTarget2->FreeVirtual(
        TO_CDADDR(mem), size, typeFlags);

    if (status != S_OK && throwEx)
    {
        DacError(status);
        UNREACHABLE();
    }

    return status;
}

PVOID
DacInstantiateTypeByAddressHelper(TADDR addr, ULONG32 size, bool throwEx, bool fReport)
{
#ifdef _PREFIX_

    // Dac accesses are not interesting for PREfix and cause a lot of PREfix noise
    // so we just return the unmodified pointer for our PREFIX builds
    return (PVOID)addr;

#else // !_PREFIX_

    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    // Preserve special pointer values.
    if (!addr || addr == (TADDR)-1)
    {
        return (PVOID)addr;
    }

    // DacInstanceManager::Alloc will assert (with a non-obvious message) on 0-size instances.
    // Fail sooner and more obviously here.
    _ASSERTE_MSG( size > 0, "DAC coding error: instance size cannot be 0" );

    // Do not attempt to allocate more than 64megs for one object instance.  While we should
    // never even come close to this size, in cases of heap corruption or bogus data passed
    // into the dac, we can allocate huge amounts of data if we are unlucky.  This santiy
    // checks the size to ensure we don't allocate gigs of data.
    if (size > 0x4000000)
    {
        if (throwEx)
        {
            DacError(E_OUTOFMEMORY);
        }
        return NULL;
    }

    //
    // Check the cache for an existing DPTR instance.
    // It's possible that a previous access may have been
    // smaller than the current access, so we have to
    // allow an existing instance to be superseded.
    //

    DAC_INSTANCE* inst = g_dacImpl->m_instances.Find(addr);
    DAC_INSTANCE* oldInst = NULL;
    if (inst)
    {
        // If the existing instance is large enough we
        // can reuse it, otherwise we need to promote.
        // We cannot promote a VPTR as the VPTR data
        // has been updated with a host vtable and we
        // don't want to lose that.  This shouldn't
        // happen anyway.
        if (inst->size >= size)
        {
            return inst + 1;
        }
        else
        {
            // Existing instance is too small and must
            // be superseded.
            if (inst->usage == DAC_VPTR)
            {
                // The same address has already been marshalled as a VPTR, now we're trying to marshal as a
                // DPTR.  This is not allowed.
                _ASSERTE_MSG(false, "DAC coding error: DPTR/VPTR usage conflict");
                DacError(E_INVALIDARG);
                UNREACHABLE();
            }

            // Promote the larger instance into the hash
            // in place of the smaller, but keep the
            // smaller instance around in case code still
            // has a pointer to it. But ensure that we can
            // create the larger instance and add it to the
            // hash table before removing the old one.
            oldInst = inst;
        }
    }

    inst = g_dacImpl->m_instances.Alloc(addr, size, DAC_DPTR);
    if (!inst)
    {
        DacError(E_OUTOFMEMORY);
        UNREACHABLE();
    }

    if (fReport == false)
    {
        // mark the bit if necessary
        inst->noReport = 1;
    }
    else
    {
        // clear the bit
        inst->noReport = 0;
    }
    HRESULT status = DacReadAll(addr, inst + 1, size, false);
    if (status != S_OK)
    {
        g_dacImpl->m_instances.ReturnAlloc(inst);
        if (throwEx)
        {
            DacError(status);
        }
        return NULL;
    }

    if (!g_dacImpl->m_instances.Add(inst))
    {
        g_dacImpl->m_instances.ReturnAlloc(inst);
        DacError(E_OUTOFMEMORY);
        UNREACHABLE();
    }

    if (oldInst)
    {
        g_dacImpl->m_instances.Supersede(oldInst);
    }

    return inst + 1;

#endif // !_PREFIX_
}

PVOID   DacInstantiateTypeByAddress(TADDR addr, ULONG32 size, bool throwEx)
{
    return DacInstantiateTypeByAddressHelper(addr, size, throwEx, true);
}

PVOID   DacInstantiateTypeByAddressNoReport(TADDR addr, ULONG32 size, bool throwEx)
{
    return DacInstantiateTypeByAddressHelper(addr, size, throwEx, false);
}


PVOID
DacInstantiateClassByVTable(TADDR addr, ULONG32 minSize, bool throwEx)
{
#ifdef _PREFIX_

    // Dac accesses are not interesting for PREfix and cause a lot of PREfix noise
    // so we just return the unmodified pointer for our PREFIX builds
    return (PVOID)addr;

#else // !_PREFIX_

    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    // Preserve special pointer values.
    if (!addr || addr == (TADDR)-1)
    {
        return (PVOID)addr;
    }

    // Do not attempt to allocate more than 64megs for one object instance.  While we should
    // never even come close to this size, in cases of heap corruption or bogus data passed
    // into the dac, we can allocate huge amounts of data if we are unlucky.  This santiy
    // checks the size to ensure we don't allocate gigs of data.
    if (minSize > 0x4000000)
    {
        if (throwEx)
        {
            DacError(E_OUTOFMEMORY);
        }
        return NULL;
    }

    //
    // Check the cache for an existing VPTR instance.
    // If there is an instance we assume that it's
    // the right object.
    //

    DAC_INSTANCE* inst = g_dacImpl->m_instances.Find(addr);
    DAC_INSTANCE* oldInst = NULL;
    if (inst)
    {
        // If the existing instance is a VPTR we can
        // reuse it, otherwise we need to promote.
        if (inst->usage == DAC_VPTR)
        {
            // Sanity check that the object we're returning is big enough to fill the PTR type it's being
            // accessed with.  For more information, see the similar check below for the case when the
            // object isn't already cached
            _ASSERTE_MSG(inst->size >= minSize, "DAC coding error: Attempt to instantiate a VPTR from an object that is too small");

            return inst + 1;
        }
        else
        {
            // Existing instance is not a match and must
            // be superseded.
            // Promote the new instance into the hash
            // in place of the old, but keep the
            // old instance around in case code still
            // has a pointer to it. But ensure that we can
            // create the larger instance and add it to the
            // hash table before removing the old one.
            oldInst = inst;
        }
    }

    HRESULT status;
    TADDR vtAddr;
    ULONG32 size;
    PVOID hostVtPtr;

    // Read the vtable pointer to get the actual
    // implementation class identity.
    if ((status = DacReadAll(addr, &vtAddr, sizeof(vtAddr), throwEx)) != S_OK)
    {
        return NULL;
    }

    //
    // Instantiate the right class, using the vtable as
    // class identity.
    //

#define VPTR_CLASS(name)                       \
    if (vtAddr == g_dacImpl->m_globalBase +    \
        g_dacGlobals.name##__vtAddr)           \
    {                                          \
        size = sizeof(name);                   \
        hostVtPtr = g_dacHostVtPtrs.name;      \
    }                                          \
    else
#define VPTR_MULTI_CLASS(name, keyBase)        \
    if (vtAddr == g_dacImpl->m_globalBase +    \
        g_dacGlobals.name##__##keyBase##__mvtAddr) \
    {                                          \
        size = sizeof(name);                   \
        hostVtPtr = g_dacHostVtPtrs.name##__##keyBase; \
    }                                          \
    else
#include <vptr_list.h>
#undef VPTR_CLASS
#undef VPTR_MULTI_CLASS

    {
        // Can't identify the vtable pointer.
        if (throwEx)
        {
            _ASSERTE_MSG(false,"DAC coding error: Unrecognized vtable pointer in VPTR marshalling code");
            DacError(E_INVALIDARG);
        }
        return NULL;
    }

    // Sanity check that the object we're returning is big enough to fill the PTR type it's being
    // accessed with.
    // If this is not true, it means the type being marshalled isn't a sub-type (or the same type)
    // as the PTR type it's being used as.  For example, trying to marshal an instance of a SystemDomain
    // object into a PTR_AppDomain will cause this ASSERT to fire (because both SystemDomain and AppDomain
    // derived from BaseDomain, and SystemDomain is smaller than AppDomain).
    _ASSERTE_MSG(size >= minSize, "DAC coding error: Attempt to instantiate a VPTR from an object that is too small");

    inst = g_dacImpl->m_instances.Alloc(addr, size, DAC_VPTR);
    if (!inst)
    {
        DacError(E_OUTOFMEMORY);
        UNREACHABLE();
    }

    // Copy the object contents into the host instance.  Note that this assumes the host and target
    // have the same exact layout.  Specifically, it assumes the host and target vtable pointers are
    // the same size.
    if ((status = DacReadAll(addr, inst + 1, size, false)) != S_OK)
    {
        g_dacImpl->m_instances.ReturnAlloc(inst);
        if (throwEx)
        {
            DacError(status);
        }
        return NULL;
    }

    // We now have a proper target object with a target
    // vtable.  We need to patch the vtable to the appropriate
    // host vtable so that the virtual functions can be
    // called in the host process.
    *(PVOID*)(inst + 1) = hostVtPtr;

    if (!g_dacImpl->m_instances.Add(inst))
    {
        g_dacImpl->m_instances.ReturnAlloc(inst);
        DacError(E_OUTOFMEMORY);
        UNREACHABLE();
    }

    if (oldInst)
    {
        g_dacImpl->m_instances.Supersede(oldInst);
    }
    return inst + 1;

#endif // !_PREFIX_
}

#define LOCAL_STR_BUF 256

PSTR
DacInstantiateStringA(TADDR addr, ULONG32 maxChars, bool throwEx)
{
#ifdef _PREFIX_

    // Dac accesses are not interesting for PREfix and cause a lot of PREfix noise
    // so we just return the unmodified pointer for our PREFIX builds
    return (PSTR)addr;

#else // !_PREFIX_

    HRESULT status;

    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    // Preserve special pointer values.
    if (!addr || addr == (TADDR)-1)
    {
        return (PSTR)addr;
    }


    // Do not attempt to allocate more than 64megs for a string.  While we should
    // never even come close to this size, in cases of heap corruption or bogus data passed
    // into the dac, we can allocate huge amounts of data if we are unlucky.  This santiy
    // checks the size to ensure we don't allocate gigs of data.
    if (maxChars > 0x4000000)
    {
        if (throwEx)
        {
            DacError(E_OUTOFMEMORY);
        }
        return NULL;
    }

    //
    // Look for an existing string instance.
    //

    DAC_INSTANCE* inst = g_dacImpl->m_instances.Find(addr);
    if (inst && inst->usage == DAC_STRA)
    {
        return (PSTR)(inst + 1);
    }

    //
    // Determine the length of the string
    // by iteratively reading blocks and scanning them
    // for a terminator.
    //

    char buf[LOCAL_STR_BUF];
    TADDR scanAddr = addr;
    ULONG32 curBytes = 0;
    ULONG32 returned;

    for (;;)
    {
        status = g_dacImpl->m_pTarget->
            ReadVirtual(scanAddr, (PBYTE)buf, sizeof(buf),
                        &returned);
        if (status != S_OK)
        {
            // We hit invalid memory before finding a terminator.
            if (throwEx)
            {
                DacError(CORDBG_E_READVIRTUAL_FAILURE);
            }
            return NULL;
        }

        PSTR scan = (PSTR)buf;
        PSTR scanEnd = scan + (returned / sizeof(*scan));
        while (scan < scanEnd)
        {
            if (!*scan)
            {
                break;
            }

            scan++;
        }

        if (!*scan)
        {
            // Found a terminator.
            scanAddr += ((scan + 1) - buf) * sizeof(*scan);
            break;
        }

        // Ignore any partial character reads.  The character
        // will be reread on the next loop if necessary.
        returned &= ~(sizeof(buf[0]) - 1);

        // The assumption is that a memory read cannot wrap
        // around the address space, thus if we have read to
        // the top of memory scanAddr cannot wrap farther
        // than to zero.
        curBytes += returned;
        scanAddr += returned;

        if (!scanAddr ||
            (curBytes + sizeof(buf[0]) - 1) / sizeof(buf[0]) >= maxChars)
        {
            // Wrapped around the top of memory or
            // we didn't find a terminator within the given bound.
            if (throwEx)
            {
                DacError(E_INVALIDARG);
            }
            return NULL;
        }
    }

    // Now that we know the length we can create a
    // host copy of the string.
    PSTR retVal = (PSTR)
        DacInstantiateTypeByAddress(addr, (ULONG32)(scanAddr - addr), throwEx);
    if (retVal &&
        (inst = g_dacImpl->m_instances.Find(addr)))
    {
        inst->usage = DAC_STRA;
    }
    return retVal;

#endif // !_PREFIX_
}

PWSTR
DacInstantiateStringW(TADDR addr, ULONG32 maxChars, bool throwEx)
{
#ifdef _PREFIX_

    // Dac accesses are not interesting for PREfix and cause a lot of PREfix noise
    // so we just return the unmodified pointer for our PREFIX builds
    return (PWSTR)addr;

#else // !_PREFIX_

    HRESULT status;

    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    // Preserve special pointer values.
    if (!addr || addr == (TADDR)-1)
    {
        return (PWSTR)addr;
    }

    // Do not attempt to allocate more than 64megs for a string.  While we should
    // never even come close to this size, in cases of heap corruption or bogus data passed
    // into the dac, we can allocate huge amounts of data if we are unlucky.  This santiy
    // checks the size to ensure we don't allocate gigs of data.
    if (maxChars > 0x4000000)
    {
        if (throwEx)
        {
            DacError(E_OUTOFMEMORY);
        }
        return NULL;
    }


    //
    // Look for an existing string instance.
    //

    DAC_INSTANCE* inst = g_dacImpl->m_instances.Find(addr);
    if (inst && inst->usage == DAC_STRW)
    {
        return (PWSTR)(inst + 1);
    }

    //
    // Determine the length of the string
    // by iteratively reading blocks and scanning them
    // for a terminator.
    //

    WCHAR buf[LOCAL_STR_BUF];
    TADDR scanAddr = addr;
    ULONG32 curBytes = 0;
    ULONG32 returned;

    for (;;)
    {
        status = g_dacImpl->m_pTarget->
            ReadVirtual(scanAddr, (PBYTE)buf, sizeof(buf),
                        &returned);
        if (status != S_OK)
        {
            // We hit invalid memory before finding a terminator.
            if (throwEx)
            {
                DacError(CORDBG_E_READVIRTUAL_FAILURE);
            }
            return NULL;
        }

        PWSTR scan = (PWSTR)buf;
        PWSTR scanEnd = scan + (returned / sizeof(*scan));
        while (scan < scanEnd)
        {
            if (!*scan)
            {
                break;
            }

            scan++;
        }

        if (!*scan)
        {
            // Found a terminator.
            scanAddr += ((scan + 1) - buf) * sizeof(*scan);
            break;
        }

        // Ignore any partial character reads.  The character
        // will be reread on the next loop if necessary.
        returned &= ~(sizeof(buf[0]) - 1);

        // The assumption is that a memory read cannot wrap
        // around the address space, thus if we have read to
        // the top of memory scanAddr cannot wrap farther
        // than to zero.
        curBytes += returned;
        scanAddr += returned;

        if (!scanAddr ||
            (curBytes + sizeof(buf[0]) - 1) / sizeof(buf[0]) >= maxChars)
        {
            // Wrapped around the top of memory or
            // we didn't find a terminator within the given bound.
            if (throwEx)
            {
                DacError(E_INVALIDARG);
            }
            return NULL;
        }
    }

    // Now that we know the length we can create a
    // host copy of the string.
    PWSTR retVal = (PWSTR)
        DacInstantiateTypeByAddress(addr, (ULONG32)(scanAddr - addr), throwEx);
    if (retVal &&
        (inst = g_dacImpl->m_instances.Find(addr)))
    {
        inst->usage = DAC_STRW;
    }
    return retVal;

#endif // !_PREFIX_
}

TADDR
DacGetTargetAddrForHostAddr(LPCVOID ptr, bool throwEx)
{
#ifdef _PREFIX_

    // Dac accesses are not interesting for PREfix and cause a lot of PREfix noise
    // so we just return the unmodified pointer for our PREFIX builds
    return (TADDR) ptr;

#else // !_PREFIX_

    // Preserve special pointer values.
    if (ptr == NULL || ((TADDR) ptr == (TADDR)-1))
    {
        return 0;
    }
    else
    {
        TADDR addr = 0;
        HRESULT status = E_FAIL;

        EX_TRY
        {
            DAC_INSTANCE* inst = (DAC_INSTANCE*)ptr - 1;
            if (inst->sig == DAC_INSTANCE_SIG)
            {
                addr = inst->addr;
                status = S_OK;
            }
            else
            {
                status = E_INVALIDARG;
            }
        }
        EX_CATCH
        {
            status = E_INVALIDARG;
        }
        EX_END_CATCH(SwallowAllExceptions)

        if (status != S_OK)
        {
            if (g_dacImpl && g_dacImpl->m_debugMode)
            {
                DebugBreak();
            }

            if (throwEx)
            {
                // This means a pointer was supplied which doesn't actually point to the beginning of
                // a marshalled DAC instance.
                _ASSERTE_MSG(false, "DAC coding error: Attempt to get target address from a host pointer "
                                    "which is not an instance marshalled by DAC!");
                DacError(status);
            }
        }

        return addr;
    }

#endif // !_PREFIX_
}

// Similar to DacGetTargetAddrForHostAddr above except that ptr can represent any pointer within a host data
// structure marshalled from the target (rather than just a pointer to the first field).
TADDR
DacGetTargetAddrForHostInteriorAddr(LPCVOID ptr, bool throwEx)
{
    // Our algorithm for locating the containing DAC instance will search backwards through memory in
    // DAC_INSTANCE_ALIGN increments looking for a valid header. The following constant determines how many of
    // these iterations we'll perform before deciding the caller made a mistake and didn't marshal the
    // containing instance from the target to the host properly. Lower values will determine the maximum
    // offset from the start of a marshalled structure at which an interior pointer can appear. Higher values
    // will bound the amount of time it takes to report an error in the case where code has been incorrectly
    // DAC-ized.
    const DWORD kMaxSearchIterations = 100;

#ifdef _PREFIX_

    // Dac accesses are not interesting for PREfix and cause a lot of PREfix noise
    // so we just return the unmodified pointer for our PREFIX builds
    return (TADDR) ptr;

#else // !_PREFIX_

    // Preserve special pointer values.
    if (ptr == NULL || ((TADDR) ptr == (TADDR)-1))
    {
        return 0;
    }
    else
    {
        TADDR addr = 0;
        HRESULT status = E_FAIL;

        EX_TRY
        {
            // We're going to search backwards through memory from the pointer looking for a valid DAC
            // instance header. Initialize this search pointer to the first legal value it could hold.
            // Intuitively this would be ptr - sizeof(DAC_INSTANCE), but DAC_INSTANCE headers are further
            // constrained to lie on DAC_INSTANCE_ALIGN boundaries. DAC_INSTANCE_ALIGN is large (16 bytes) due
            // to the need to keep the marshalled structure also aligned for any possible need, so we gain
            // considerable performance from only needing to test for DAC_INSTANCE headers at
            // DAC_INSTANCE_ALIGN aligned addresses.
            DAC_INSTANCE * inst = (DAC_INSTANCE*)(((ULONG_PTR)ptr - sizeof(DAC_INSTANCE)) & ~(DAC_INSTANCE_ALIGN - 1));

            // When code is DAC'ized correctly then our search algorithm is guaranteed to terminate safely
            // before reading memory that doesn't belong to the containing DAC instance. Since people do make
            // mistakes we want to limit how long and far we search however. The counter below will let us
            // assert if we've likely tried to locate an interior host pointer in a non-marshalled structure.
            DWORD cIterations = 0;

            bool tryAgain = false;

            // Scan backwards in memory looking for a DAC_INSTANCE header.
            while (true)
            {
                // Step back DAC_INSTANCE_ALIGN bytes at a time (the initialization of inst above guarantees
                // we start with an aligned pointer value. Stop every time our potential DAC_INSTANCE header
                // has a correct signature value.
                while (tryAgain || inst->sig != DAC_INSTANCE_SIG)
                {
                    tryAgain = false;
                    inst = (DAC_INSTANCE*)((BYTE*)inst - DAC_INSTANCE_ALIGN);

                    // If we've searched a lot of memory (currently 100 * 16 == 1600 bytes) without success,
                    // then assume this is due to an issue DAC-izing code (if you really do have a field within a
                    // DAC marshalled structure whose offset is >1600 bytes then feel free to update the
                    // constant at the start of this method).
                    if (++cIterations > kMaxSearchIterations)
                    {
                        status = E_INVALIDARG;
                        break;
                    }
                }

                // Fall through to a DAC error if we searched too long without finding a header candidate.
                if (status == E_INVALIDARG)
                    break;

                // Validate our candidate header by looking up the target address it claims to map in the
                // instance hash. The entry should both exist and correspond exactly to our candidate instance
                // pointer.
                // TODO: but what if the same memory was marshalled more than once (eg. once as a DPTR, once as a VPTR)?
                if (inst == g_dacImpl->m_instances.Find(inst->addr))
                {
                    // We've found a valid DAC instance. Now validate that the marshalled structure it
                    // represents really does enclose the pointer we're asking about. If not, someone hasn't
                    // marshalled a containing structure before trying to map a pointer within that structure
                    // (we've just gone and found the previous, unrelated marshalled structure in host memory).
                    BYTE * parent = (BYTE*)(inst + 1);
                    if (((BYTE*)ptr + sizeof(LPCVOID)) <= (parent + inst->size))
                    {
                        // Everything checks out: we've found a DAC instance header and its address range
                        // encompasses the pointer we're interested in. Compute the corresponding target
                        // address by taking into account the offset of the interior pointer into its
                        // enclosing structure.
                        addr = inst->addr + ((BYTE*)ptr - parent);
                        status = S_OK;
                    }
                    else
                    {
                        // We found a valid DAC instance but it doesn't cover the address range containing our
                        // input pointer. Fall though to report an erroring DAC-izing code.
                        status = E_INVALIDARG;
                    }
                    break;
                }
                else
                {
                    // This must not really be a match, perhaps a coincidence?
                    // Keep searching
                    tryAgain = true;
                }
            }
        }
        EX_CATCH
        {
            status = E_INVALIDARG;
        }
        EX_END_CATCH(SwallowAllExceptions)

        if (status != S_OK)
        {
            if (g_dacImpl && g_dacImpl->m_debugMode)
            {
                DebugBreak();
            }

            if (throwEx)
            {
                // This means a pointer was supplied which doesn't actually point to somewhere in a marshalled
                // DAC instance.
                _ASSERTE_MSG(false, "DAC coding error: Attempt to get target address from a host interior "
                                    "pointer which is not an instance marshalled by DAC!");
                DacError(status);
            }
        }

        return addr;
    }
#endif // !_PREFIX_
}

PWSTR    DacGetVtNameW(TADDR targetVtable)
{
    PWSTR pszRet = NULL;

    ULONG *targ = &g_dacGlobals.EEJitManager__vtAddr;
    ULONG *targStart = targ;
    for (ULONG i = 0; i < sizeof(g_dacHostVtPtrs) / sizeof(PVOID); i++)
    {
        if (targetVtable == (*targ + DacGlobalBase()))
        {
            pszRet = (PWSTR) *(g_dacVtStrings + (targ - targStart));
            break;
        }

        targ++;
    }
    return pszRet;
}

TADDR
DacGetTargetVtForHostVt(LPCVOID vtHost, bool throwEx)
{
    PVOID* host;
    ULONG* targ;
    ULONG i;

    // The host vtable table exactly parallels the
    // target vtable table, so just iterate to a match
    // return the matching entry.
    host = &g_dacHostVtPtrs.EEJitManager;
    targ = &g_dacGlobals.EEJitManager__vtAddr;
    for (i = 0; i < sizeof(g_dacHostVtPtrs) / sizeof(PVOID); i++)
    {
        if (*host == vtHost)
        {
            return *targ + DacGlobalBase();
        }

        host++;
        targ++;
    }

    if (throwEx)
    {
        DacError(E_INVALIDARG);
    }
    return 0;
}

//
// DacEnumMemoryRegion - report a region of memory to the dump generation code
//
// Parameters:
//   addr           - target address of the beginning of the memory region
//   size           - number of bytes to report
//   fExpectSuccess - whether or not ASSERTs should be raised if some memory in this region
//                    is found to be unreadable.  Generally we should only report readable
//                    memory (unless the target is corrupt, in which case we expect asserts
//                    if target consistency checking is enabled).  Reporting memory that
//                    isn't fully readable often indicates an issue that could cause much worse
//                    problems (loss of dump data, long/infinite loops in dump generation),
//                    so we want to try and catch any such usage.  Ocassionally we can't say
//                    for sure how much of the reported region will be readable (eg. for the
//                    LoaderHeap, we only know the length of the allocated address space, not
//                    the size of the commit region for every block).  In these special cases,
//                    we pass false to indicate that we're happy reporting up to the first
//                    unreadable byte.  This should be avoided if at all possible.
//
bool DacEnumMemoryRegion(TADDR addr, TSIZE_T size, bool fExpectSuccess /*=true*/)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    return g_dacImpl->ReportMem(addr, size, fExpectSuccess);
}

//
// DacUpdateMemoryRegion - updates/poisons a region of memory of generated dump
//
// Parameters:
//   addr           - target address of the beginning of the memory region
//   bufferSize     - number of bytes to update/poison
//   buffer         - data to be written at given target address
//
bool DacUpdateMemoryRegion(TADDR addr, TSIZE_T bufferSize, BYTE* buffer)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    return g_dacImpl->DacUpdateMemoryRegion(addr, bufferSize, buffer);
}

HRESULT
DacWriteHostInstance(PVOID host, bool throwEx)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    TADDR addr = DacGetTargetAddrForHostAddr(host, throwEx);
    if (!addr)
    {
        return S_OK;
    }

    DAC_INSTANCE* inst = (DAC_INSTANCE*)host - 1;
    return g_dacImpl->m_instances.Write(inst, throwEx);
}

bool
DacHostPtrHasEnumMark(LPCVOID host)
{
    if (!DacGetTargetAddrForHostAddr(host, false))
    {
        // Make it easy to ignore invalid pointers when enumerating.
        return true;
    }

    DAC_INSTANCE* inst = ((DAC_INSTANCE*)host) - 1;
    bool marked = inst->enumMem ? true : false;
    inst->enumMem = true;
    return marked;
}

bool
DacHasMethodDescBeenEnumerated(LPCVOID pMD)
{
    if (!DacGetTargetAddrForHostAddr(pMD, false))
    {
        // Make it easy to ignore invalid pointers when enumerating.
        return true;
    }

    DAC_INSTANCE* inst = ((DAC_INSTANCE*) pMD) - 1;
    bool MDEnumed = inst->MDEnumed ? true : false;
    return MDEnumed;
}

bool
DacSetMethodDescEnumerated(LPCVOID pMD)
{
    if (!DacGetTargetAddrForHostAddr(pMD, false))
    {
        // Make it easy to ignore invalid pointers when enumerating.
        return true;
    }

    DAC_INSTANCE* inst = ((DAC_INSTANCE*) pMD) - 1;
    bool MDEnumed = inst->MDEnumed ? true : false;
    inst->MDEnumed = true;
    return MDEnumed;
}

// This gets called from DAC-ized code in the VM.
IMDInternalImport*
DacGetMDImport(const PEAssembly* pPEAssembly, bool throwEx)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    return g_dacImpl->GetMDImport(pPEAssembly, throwEx);
}

IMDInternalImport*
DacGetMDImport(const ReflectionModule* reflectionModule, bool throwEx)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    return g_dacImpl->GetMDImport(reflectionModule, throwEx);
}

COR_ILMETHOD*
DacGetIlMethod(TADDR methAddr)
{
    ULONG32 methodSize = static_cast<ULONG32>(PEDecoder::ComputeILMethodSize(methAddr));

    // Sometimes when reading from dumps and inspect NGEN images, but we end up reading metadata from IL image
    // the method RVA could not match and we could read from a random address that will translate in inconsistent
    // IL code header. If we see the size of the code bigger than 64 Megs we are probably reading a bad IL code header.
    // For details see issue DevDiv 273199.
    if (methodSize > 0x4000000)
    {
        DacError(CORDBG_E_TARGET_INCONSISTENT);
        UNREACHABLE();
    }
    return (COR_ILMETHOD*)
        DacInstantiateTypeByAddress(methAddr, methodSize,
                                    true);
}

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
void
DacMdCacheAddEEName(TADDR taEE, const SString& ssEEName)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    g_dacImpl->MdCacheAddEEName(taEE, ssEEName);
}
bool
DacMdCacheGetEEName(TADDR taEE, SString & eeName)
{
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    return g_dacImpl->MdCacheGetEEName(taEE, eeName);
}

#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

PVOID
DacAllocHostOnlyInstance(ULONG32 size, bool throwEx)
{
    SUPPORTS_DAC_HOST_ONLY;
    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    DAC_INSTANCE* inst = g_dacImpl->m_instances.Alloc(0, size, DAC_DPTR);
    if (!inst)
    {
        DacError(E_OUTOFMEMORY);
        UNREACHABLE();
    }

    g_dacImpl->m_instances.AddSuperseded(inst);

    return inst + 1;
}

//
// Queries whether ASSERTs should be raised when inconsistencies in the target are detected
//
// Return Value:
//   true if ASSERTs should be raised in DACized code.
//   false if ASSERTs should be ignored.
//
// Notes:
//   See code:ClrDataAccess::TargetConsistencyAssertsEnabled for details.
bool DacTargetConsistencyAssertsEnabled()
{
    if (!g_dacImpl)
    {
        // No ClrDataAccess instance available (maybe we're still initializing).  Any asserts when this is
        // the case should only be host-asserts (i.e. always bugs), and so we should just return true.
        return true;
    }

    return g_dacImpl->TargetConsistencyAssertsEnabled();
}

//
// DacEnumCodeForStackwalk
// This is a helper function to enumerate the instructions around a call site to aid heuristics
// used by debugger stack walkers.
//
// Arguments:
//     taCallEnd - target address of the instruction just after the call instruction for the stack
//                 frame we want to examine(i.e. the return address for the next frame).
//
// Note that this is shared by our two stackwalks during minidump generation,
// code:Thread::EnumMemoryRegionsWorker and code:ClrDataAccess::EnumMemWalkStackHelper.  Ideally
// we'd only have one stackwalk, but we currently have two different APIs for stackwalking
// (CLR StackFrameIterator and IXCLRDataStackWalk), and we must ensure that the memory needed
// for either is captured in a minidump.  Eventually, all clients should get moved over to the
// arrowhead debugging architecture, at which time we can rip out all the IXCLRData APIs, and
// so this logic could just be private to the EnumMem code for Thread.
//
void DacEnumCodeForStackwalk(TADDR taCallEnd)
{
    if (taCallEnd == 0)
        return;
    //
    // x86 stack walkers often end up having to guess
    // about what's a return address on the stack.
    // Doing so involves looking at the code at the
    // possible call site and seeing if it could
    // reach the callee.  Save enough code and around
    // the call site to allow this with a dump.
    //
    // For whatever reason 64-bit platforms require us to save
    // the instructions around the call sites on the stack as well.
    // Otherwise we cannnot show the stack in a minidump.
    //
    // Note that everything we do here is a heuristic that won't always work in general.
    // Eg., part of the 2xMAX_INSTRUCTION_LENGTH range might not be mapped (we could be
    // right on a page boundary).  More seriously, X86 is not necessarily parsable in reverse
    // (eg. there could be a segment-override prefix in front of the call instruction that
    // we miss).  So we'll dump what we can and ignore any failures.  Ideally we'd better
    // quantify exactly what debuggers need and why, and try and avoid these ugly heuristics.
    // It seems like these heuristics are too tightly coupled to the implementation details
    // of some specific debugger stackwalking algorithm.
    //
    DacEnumMemoryRegion(taCallEnd - MAX_INSTRUCTION_LENGTH, MAX_INSTRUCTION_LENGTH * 2, false);

#if defined(TARGET_X86)
    // If it was an indirect call we also need to save the data indirected through.
    // Note that this only handles absolute indirect calls (ModR/M byte of 0x15), all the other forms of
    // indirect calls are register-relative, and so we'd have to do a much more complicated decoding based
    // on the register context.  Regardless, it seems like this is fundamentally error-prone because it's
    // aways possible that the call instruction was not 6 bytes long, and we could have some other instructions
    // that happen to match the pattern we're looking for.
    PTR_BYTE callCode = PTR_BYTE(taCallEnd - 6);
    PTR_BYTE callMrm = PTR_BYTE(taCallEnd - 5);
    PTR_TADDR callInd = PTR_TADDR(taCallEnd - 4);
    if (callCode.IsValid() &&
        (*callCode == 0xff) &&
        callMrm.IsValid() &&
        (*callMrm == 0x15) &&
        callInd.IsValid())
    {
        DacEnumMemoryRegion(*callInd, sizeof(TADDR), false);
    }
#endif // #ifdef TARGET_X86
}

// ----------------------------------------------------------------------------
// DacReplacePatches
//
// Description:
//    Given the address and the size of a memory range which is stored in the buffer, replace all the patches
//    in the buffer with the real opcodes.  This is especially important on X64 where the unwinder needs to
//    disassemble the native instructions.
//
// Arguments:
//    * range   - the address and the size of the memory range
//    * pBuffer - the buffer containting the memory range
//
// Return Value:
//    Return S_OK if everything succeeds.
//
// Assumptions:
//    * The debuggee has to be stopped.
//
// Notes:
//    * @dbgtodo  ICDProcess - When we DACize code:CordbProcess::ReadMemory,
//        we should change it to use this function.
//

HRESULT DacReplacePatchesInHostMemory(MemoryRange range, PVOID pBuffer)
{
    SUPPORTS_DAC;

    // If the patch table is invalid, then there is no patch to replace.
    if (!DebuggerController::GetPatchTableValid())
    {
        return S_OK;
    }

    HASHFIND info;

    DebuggerPatchTable *      pTable = DebuggerController::GetPatchTable();
    DebuggerControllerPatch * pPatch = pTable->GetFirstPatch(&info);

    // <PERF>
    // The unwinder needs to read the stack very often to restore pushed registers, retrieve the
    // return addres, etc.  However, stack addresses should never be patched.
    // One way to optimize this code is to pass the stack base and the stack limit of the thread to this
    // function and use those two values to filter out stack addresses.
    //
    // Another thing we can do is instead of enumerating the patches, we could enumerate the address.
    // This is more efficient when we have a large number of patches and a small memory range.  Perhaps
    // we could do a hybrid approach, i.e. use the size of the range and the number of patches to dynamically
    // determine which enumeration is more efficient.
    // </PERF>
    while (pPatch != NULL)
    {
        CORDB_ADDRESS patchAddress = (CORDB_ADDRESS)dac_cast<TADDR>(pPatch->address);

        if (patchAddress != NULL)
        {
            PRD_TYPE opcode = pPatch->opcode;

            CORDB_ADDRESS address = (CORDB_ADDRESS)(dac_cast<TADDR>(range.StartAddress()));
            SIZE_T        cbSize  = range.Size();

            // Check if the address of the patch is in the specified memory range.
            if (IsPatchInRequestedRange(address, cbSize, patchAddress))
            {
                // Replace the patch in the buffer with the original opcode.
                CORDbgSetInstructionEx(reinterpret_cast<PBYTE>(pBuffer), address, patchAddress, opcode, cbSize);
            }
        }

        pPatch = pTable->GetNextPatch(&info);
    }

    return S_OK;
}
