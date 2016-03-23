// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: methodimpl.h
//


//

//
// ============================================================================

#ifndef _METHODIMPL_H 
#define _METHODIMPL_H

class MethodDesc;

// <TODO>@TODO: This is very bloated. We need to trim this down alot. However,
// we need to keep it on a 8 byte boundary.</TODO>
class MethodImpl
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

    PTR_DWORD            pdwSlots;       // Maintains the slots in sorted order, the first entry is the size
    DPTR(PTR_MethodDesc) pImplementedMD;

public:

#ifndef DACCESS_COMPILE 
    ///////////////////////////////////////////////////////////////////////////////////////
    class Iterator
    {
    private:
        MethodDesc *m_pMD;
        MethodImpl *m_pImpl;
        DWORD       m_iCur;

    public:
        Iterator(MethodDesc *pMD);
        inline BOOL IsValid()
            { WRAPPER_NO_CONTRACT; return ((m_pImpl != NULL)&& (m_iCur < m_pImpl->GetSize())); }
        inline void Next()
            { WRAPPER_NO_CONTRACT; if (IsValid()) m_iCur++; }
        inline WORD GetSlot()
            { WRAPPER_NO_CONTRACT; CONSISTENCY_CHECK(IsValid()); _ASSERTE(FitsIn<WORD>(m_pImpl->GetSlots()[m_iCur])); return static_cast<WORD>(m_pImpl->GetSlots()[m_iCur]); }
        inline MethodDesc *GetMethodDesc()
            { WRAPPER_NO_CONTRACT; return m_pImpl->FindMethodDesc(GetSlot(), (PTR_MethodDesc) m_pMD); }
    };

    ///////////////////////////////////////////////////////////////////////////////////////
    inline MethodDesc** GetImplementedMDs()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(this));
        } CONTRACTL_END;
        return pImplementedMD;
    }
#endif // !DACCESS_COMPILE

    ///////////////////////////////////////////////////////////////////////////////////////
    inline DWORD GetSize()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(this));
        } CONTRACTL_END;

        if(pdwSlots == NULL)
            return 0;
        else
            return *pdwSlots;
    }

    ///////////////////////////////////////////////////////////////////////////////////////
    inline PTR_DWORD GetSlots()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(this));
            SUPPORTS_DAC;
        } CONTRACTL_END;

        if(pdwSlots == NULL)
            return NULL;
        else
            return pdwSlots + 1;
    }

#ifndef DACCESS_COMPILE 

    ///////////////////////////////////////////////////////////////////////////////////////
    void SetSize(LoaderHeap *pHeap, AllocMemTracker *pamTracker, DWORD size);

    ///////////////////////////////////////////////////////////////////////////////////////
    void SetData(DWORD* slots, MethodDesc** md);

#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE 
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

#ifdef FEATURE_PREJIT 
    void Save(DataImage *image);
    void Fixup(DataImage *image, PVOID p, SSIZE_T offset);
#endif // FEATURE_PREJIT


    // Returns the method desc for the replaced slot;
    PTR_MethodDesc FindMethodDesc(DWORD slot, PTR_MethodDesc defaultReturn);

private:
    static const DWORD INVALID_INDEX = (DWORD)(-1);
    DWORD FindSlotIndex(DWORD slot);
#ifndef DACCESS_COMPILE 
    MethodDesc* RestoreSlot(DWORD slotIndex, MethodTable *pMT);
#endif

};

#endif // !_METHODIMPL_H
