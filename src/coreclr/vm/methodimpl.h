// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

    RelativePointer<PTR_DWORD>            pdwSlots;       // Maintains the slots and tokens in sorted order, the first entry is the size
    RelativePointer<DPTR( RelativePointer<PTR_MethodDesc> )> pImplementedMD;

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
        inline mdToken GetToken()
            { WRAPPER_NO_CONTRACT; CONSISTENCY_CHECK(IsValid()); return m_pImpl->GetTokens()[m_iCur]; }
        inline MethodDesc *GetMethodDesc()
            { WRAPPER_NO_CONTRACT; return m_pImpl->GetMethodDesc(m_iCur, (PTR_MethodDesc) m_pMD); }
    };
#endif // !DACCESS_COMPILE

    inline DPTR(RelativePointer<PTR_MethodDesc>) GetImpMDs()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return RelativePointer<DPTR(RelativePointer<PTR_MethodDesc>)>::GetValueMaybeNullAtPtr(PTR_HOST_MEMBER_TADDR(MethodImpl, this, pImplementedMD));
    }

    inline DPTR(RelativePointer<PTR_MethodDesc>) GetImpMDsNonNull()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return RelativePointer<DPTR(RelativePointer<PTR_MethodDesc>)>::GetValueAtPtr(PTR_HOST_MEMBER_TADDR(MethodImpl, this, pImplementedMD));
    }

    ///////////////////////////////////////////////////////////////////////////////////////
    inline DWORD GetSize()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(this));
        } CONTRACTL_END;

        if(pdwSlots.IsNull())
            return 0;
        else
            return *GetSlotsRawNonNull();
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

        if(pdwSlots.IsNull())
            return NULL;
        else
            return GetSlotsRawNonNull() + 1;
    }

    inline PTR_DWORD GetSlotsRaw()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return RelativePointer<PTR_DWORD>::GetValueMaybeNullAtPtr(PTR_HOST_MEMBER_TADDR(MethodImpl, this, pdwSlots));
    }

    inline PTR_DWORD GetSlotsRawNonNull()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return RelativePointer<PTR_DWORD>::GetValueAtPtr(PTR_HOST_MEMBER_TADDR(MethodImpl, this, pdwSlots));
    }

#ifndef DACCESS_COMPILE

    ///////////////////////////////////////////////////////////////////////////////////////
    inline mdToken* GetTokens()
    {
        CONTRACTL{
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(this));
            SUPPORTS_DAC;
        } CONTRACTL_END;

        if (pdwSlots.IsNull())
            return NULL;
        else
            return (mdToken*)(GetSlotsRawNonNull() + 1 + *GetSlotsRawNonNull());
    }

    ///////////////////////////////////////////////////////////////////////////////////////
    void SetSize(LoaderHeap *pHeap, AllocMemTracker *pamTracker, DWORD size);

    ///////////////////////////////////////////////////////////////////////////////////////
    void SetData(DWORD* slots, mdToken* tokens, RelativePointer<MethodDesc*> * md);

#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    // Returns the method desc for the replaced slot;
    PTR_MethodDesc FindMethodDesc(DWORD slot, PTR_MethodDesc defaultReturn);

    // Returns the method desc for the slot index;
    PTR_MethodDesc GetMethodDesc(DWORD slotIndex, PTR_MethodDesc defaultReturn);

private:
    static const DWORD INVALID_INDEX = (DWORD)(-1);
    DWORD FindSlotIndex(DWORD slot);
#ifndef DACCESS_COMPILE
    MethodDesc* RestoreSlot(DWORD slotIndex, MethodTable *pMT);
#endif

};

#endif // !_METHODIMPL_H
