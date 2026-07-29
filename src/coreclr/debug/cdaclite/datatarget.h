// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// datatarget.h
//
// The data-target source: how cdac-lite reads the target process/dump memory.
//
//  * DataTargetAdapter exposes the classic ICLRDataTarget (handed to
//    CLRDataCreateInstance by dbghelp / createdump) as the ICorDebugDataTarget
//    that dbgutil's TryGetSymbol requires.
//  * ReadFromDataTarget is the memory-read callback the data layer (cdac::Target,
//    cdac::DataDescriptor) uses; its 'context' is the ICLRDataTarget.
//*****************************************************************************

#ifndef CDACLITE_DATATARGET_H
#define CDACLITE_DATATARGET_H

#include <windows.h>
#include <cor.h>
#include <clrdata.h>
#include <cordebug.h>
#include <stdint.h>

namespace cdac
{
    // Adapter that exposes an ICLRDataTarget as the ICorDebugDataTarget required by
    // dbgutil's TryGetSymbol. Only ReadVirtual is exercised, but the full vtable is
    // implemented defensively. Stack-lifetime: AddRef/Release count but never free.
    class DataTargetAdapter : public ICorDebugDataTarget
    {
    private:
        LONG m_ref;
        ICLRDataTarget* m_target;

    public:
        DataTargetAdapter(ICLRDataTarget* target)
            : m_ref(1), m_target(target)
        {
        }

        // IUnknown
        STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject)
        {
            if (ppvObject == nullptr)
            {
                return E_POINTER;
            }
            if (riid == IID_IUnknown || riid == __uuidof(ICorDebugDataTarget))
            {
                *ppvObject = static_cast<ICorDebugDataTarget*>(this);
                AddRef();
                return S_OK;
            }
            *ppvObject = nullptr;
            return E_NOINTERFACE;
        }

        STDMETHOD_(ULONG, AddRef)()
        {
            return InterlockedIncrement(&m_ref);
        }

        STDMETHOD_(ULONG, Release)()
        {
            // Stack-allocated; do not delete.
            return InterlockedDecrement(&m_ref);
        }

        // ICorDebugDataTarget
        STDMETHOD(GetPlatform)(CorDebugPlatform* pTargetPlatform)
        {
            // Not needed by TryGetSymbol (architecture is inferred from the PE header).
            return E_NOTIMPL;
        }

        STDMETHOD(ReadVirtual)(CORDB_ADDRESS address, BYTE* pBuffer, ULONG32 bytesRequested, ULONG32* pBytesRead)
        {
            return m_target->ReadVirtual((CLRDATA_ADDRESS)address, pBuffer, bytesRequested, pBytesRead);
        }

        STDMETHOD(GetThreadContext)(DWORD dwThreadID, ULONG32 contextFlags, ULONG32 contextSize, BYTE* pContext)
        {
            return E_NOTIMPL;
        }
    };

    // Memory-read callback used by the data layer (cdac::Target / cdac::DataDescriptor).
    // 'context' is the ICLRDataTarget. Returns true only if all 'size' bytes were read.
    bool ReadFromDataTarget(void* context, uint64_t address, void* buffer, uint32_t size);
}

#endif // CDACLITE_DATATARGET_H
