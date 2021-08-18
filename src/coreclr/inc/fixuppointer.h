// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*****************************************************************************\
*                                                                             *
* FixupPointer.h -  Fixup pointer holder types                                *
*                                                                             *
\*****************************************************************************/

#ifndef _FIXUPPOINTER_H
#define _FIXUPPOINTER_H

#include "daccess.h"

//----------------------------------------------------------------------------
// PlainPointer is simple pointer wrapper to support compilation without indirections
// This is useful for building size-constrained runtime without NGen support.
template<typename PTR_TYPE>
class PlainPointer
{
public:

    static constexpr bool isRelative = false;
    typedef PTR_TYPE type;

    // Returns whether the encoded pointer is NULL.
    BOOL IsNull() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_ptr == NULL;
    }

    // Returns whether the indirection cell contain fixup that has not been converted to real pointer yet.
    BOOL IsTagged(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return FALSE;
    }

    // Returns whether the indirection cell contain fixup that has not been converted to real pointer yet.
    BOOL IsTagged() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return FALSE;
    }

    // Returns value of the encoded pointer.
    PTR_TYPE GetValue() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<PTR_TYPE>(m_ptr);
    }

#ifndef DACCESS_COMPILE
    // Returns the pointer to the indirection cell.
    PTR_TYPE * GetValuePtr() const
    {
        LIMITED_METHOD_CONTRACT;
        return (PTR_TYPE *)&m_ptr;
    }
#endif // !DACCESS_COMPILE

    // Returns value of the encoded pointer. Assumes that the pointer is not NULL.
    PTR_TYPE GetValue(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<PTR_TYPE>(m_ptr);
    }

    // Static version of GetValue. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static PTR_TYPE GetValueAtPtr(TADDR base)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(PlainPointer<PTR_TYPE>)>(base)->GetValue(base);
    }

    // Returns value of the encoded pointer. The pointer can be NULL.
    PTR_TYPE GetValueMaybeNull() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<PTR_TYPE>(m_ptr);
    }

    // Returns value of the encoded pointer. The pointer can be NULL.
    PTR_TYPE GetValueMaybeNull(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<PTR_TYPE>(m_ptr);
    }

    // Static version of GetValueMaybeNull. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static PTR_TYPE GetValueMaybeNullAtPtr(TADDR base)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(PlainPointer<PTR_TYPE>)>(base)->GetValueMaybeNull(base);
    }

    // Returns value of the encoded pointer.
    // Allows the value to be tagged.
    FORCEINLINE TADDR GetValueMaybeTagged() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_ptr;
    }

    // Returns value of the encoded pointer. Assumes that the pointer is not NULL.
    // Allows the value to be tagged.
    FORCEINLINE TADDR GetValueMaybeTagged(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_ptr;
    }

    // Returns whether pointer is indirect. Assumes that the value is not NULL.
    bool IsIndirectPtr(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return FALSE;
    }

#ifndef DACCESS_COMPILE
    // Returns whether pointer is indirect. Assumes that the value is not NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    bool IsIndirectPtr() const
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }
#endif

    // Returns whether pointer is indirect. The value can be NULL.
    bool IsIndirectPtrMaybeNull(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return FALSE;
    }

#ifndef DACCESS_COMPILE
    // Returns whether pointer is indirect. The value can be NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    bool IsIndirectPtrMaybeNull() const
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }
#endif

#ifndef DACCESS_COMPILE
    void SetValue(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        m_ptr = dac_cast<TADDR>(addr);
    }

    // Set encoded value of the pointer. Assumes that the value is not NULL.
    void SetValue(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        m_ptr = dac_cast<TADDR>(addr);
    }

    // Static version of SetValue. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static void SetValueAtPtr(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        dac_cast<DPTR(PlainPointer<PTR_TYPE>)>(base)->SetValue(base, addr);
    }

    // Set encoded value of the pointer. The value can be NULL.
    void SetValueMaybeNull(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        m_ptr = dac_cast<TADDR>(addr);
    }

    // Set encoded value of the pointer. The value can be NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    FORCEINLINE void SetValueMaybeNull(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        return SetValueMaybeNull((TADDR)this, addr);
    }

    // Static version of SetValueMaybeNull. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static void SetValueMaybeNullAtPtr(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        dac_cast<DPTR(PlainPointer<PTR_TYPE>)>(base)->SetValueMaybeNull(base, addr);
    }

    FORCEINLINE void SetValueVolatile(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        VolatileStore((PTR_TYPE *)(&m_ptr), addr);
    }
#endif

    static TADDR GetRelativeMaybeNull(TADDR base, TADDR addr)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return addr;
    }

    static TADDR GetRelative(TADDR base, TADDR addr)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        PRECONDITION(addr != NULL);
        return addr;
    }

private:
    TADDR m_ptr;
};

#define FixupPointer PlainPointer
#define RelativePointer PlainPointer
#define RelativeFixupPointer PlainPointer

//----------------------------------------------------------------------------
// RelativePointer32 is pointer encoded as relative 32-bit offset. It is used
// to reduce both the size of the pointer itself as well as size of relocation
// section for pointers that live exlusively in NGen images.
template<typename PTR_TYPE>
class RelativePointer32
{
public:

    static constexpr bool isRelative = true;
    typedef PTR_TYPE type;

    // Returns whether the encoded pointer is NULL.
    BOOL IsNull() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        // Pointer pointing to itself is treated as NULL
        return m_delta == 0;
    }

    // Returns value of the encoded pointer. Assumes that the pointer is not NULL.
    PTR_TYPE GetValue(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        PRECONDITION(!IsNull());
        return dac_cast<PTR_TYPE>(base + m_delta);
    }

#ifndef DACCESS_COMPILE
    // Returns value of the encoded pointer. Assumes that the pointer is not NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    FORCEINLINE PTR_TYPE GetValue() const
    {
        LIMITED_METHOD_CONTRACT;
        return GetValue((TADDR)this);
    }
#endif

    // Static version of GetValue. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static PTR_TYPE GetValueAtPtr(TADDR base)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(RelativePointer<PTR_TYPE>)>(base)->GetValue(base);
    }

    // Returns value of the encoded pointer. The pointer can be NULL.
    PTR_TYPE GetValueMaybeNull(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // Cache local copy of delta to avoid races when the value is changing under us.
        TADDR delta = m_delta;

        if (delta == 0)
            return NULL;

        return dac_cast<PTR_TYPE>(base + delta);
    }

#ifndef DACCESS_COMPILE
    // Returns value of the encoded pointer. The pointer can be NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    FORCEINLINE PTR_TYPE GetValueMaybeNull() const
    {
        LIMITED_METHOD_CONTRACT;
        return GetValueMaybeNull((TADDR)this);
    }
#endif

    // Static version of GetValueMaybeNull. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static PTR_TYPE GetValueMaybeNullAtPtr(TADDR base)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(RelativePointer<PTR_TYPE>)>(base)->GetValueMaybeNull(base);
    }

private:
    INT32 m_delta;
};

//----------------------------------------------------------------------------
// IndirectPointer is pointer with optional indirection, similar to FixupPointer and RelativeFixupPointer.
//
// In comparison to FixupPointer, IndirectPointer's indirection is handled from outside by isIndirect flag.
// In comparison to RelativeFixupPointer, IndirectPointer's offset is a constant,
// while RelativeFixupPointer's offset is an address.
//
// IndirectPointer can contain NULL only if it is not indirect.
//
template<typename PTR_TYPE>
class IndirectPointer
{
public:

    static constexpr bool isRelative = false;
    typedef PTR_TYPE type;

    // Returns whether the encoded pointer is NULL.
    BOOL IsNull() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_addr == (TADDR)NULL;
    }

    // Returns whether the indirection cell contain fixup that has not been converted to real pointer yet.
    // Uses isIndirect to identify, whether pointer is indirect or not. If it is, uses offset.
    FORCEINLINE BOOL IsTaggedIndirect(TADDR base, bool isIndirect, intptr_t offset) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        TADDR addr = m_addr;
        if (isIndirect)
        {
            _ASSERTE(!IsNull());
            return (*PTR_TADDR(addr + offset) & 1) != 0;
        }
        return FALSE;
    }

    // Returns value of the encoded pointer.
    // Uses isIndirect to identify, whether pointer is indirect or not. If it is, uses offset.
    FORCEINLINE PTR_TYPE GetValueIndirect(bool isIndirect, intptr_t offset) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        TADDR addr = m_addr;
        if (isIndirect)
        {
            _ASSERTE(!IsNull());
            addr = *PTR_TADDR(addr + offset);
        }
        return dac_cast<PTR_TYPE>(addr);
    }

#ifndef DACCESS_COMPILE
    // Returns the pointer to the indirection cell.
    // Uses isIndirect to identify, whether pointer is indirect or not. If it is, uses offset.
    PTR_TYPE * GetValuePtrIndirect(bool isIndirect, intptr_t offset) const
    {
        LIMITED_METHOD_CONTRACT;
        TADDR addr = m_addr;
        if (isIndirect)
        {
            _ASSERTE(!IsNull());
            return (PTR_TYPE *)(addr + offset);
        }
        return (PTR_TYPE *)&m_addr;
    }
#endif // !DACCESS_COMPILE

    // Static version of GetValue. It is meant to simplify access to arrays of pointers.
    // Uses isIndirect to identify, whether pointer is indirect or not. If it is, uses offset.
    FORCEINLINE static PTR_TYPE GetValueAtPtrIndirect(TADDR base, bool isIndirect, intptr_t offset)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(IndirectPointer<PTR_TYPE>)>(base)->GetValueIndirect(isIndirect, offset);
    }

    // Static version of GetValueMaybeNull. It is meant to simplify access to arrays of pointers.
    // Uses isIndirect to identify, whether pointer is indirect or not. If it is, uses offset.
    FORCEINLINE static PTR_TYPE GetValueMaybeNullAtPtrIndirect(TADDR base, bool isIndirect, intptr_t offset)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetValueAtPtrIndirect(base, isIndirect, offset);
    }

#ifndef DACCESS_COMPILE
    // Returns whether pointer is indirect. Assumes that the value is not NULL.
    // Uses isIndirect to identify, whether pointer is indirect or not. If it is, uses offset.
    bool IsIndirectPtrIndirect(bool isIndirect, intptr_t offset) const
    {
        LIMITED_METHOD_CONTRACT;
        if (isIndirect)
            _ASSERTE(!IsNull());
        return isIndirect;
    }

    // Returns whether pointer is indirect. The value can be NULL.
    // Uses isIndirect to identify, whether pointer is indirect or not. If it is, uses offset.
    bool IsIndirectPtrMaybeNullIndirect(bool isIndirect, intptr_t offset) const
    {
        LIMITED_METHOD_CONTRACT;
        return IsIndirectPtrIndirect(isIndirect, offset);
    }
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
    // Set encoded value of the pointer. Assumes that the value is not NULL.
    void SetValue(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        m_addr = dac_cast<TADDR>(addr);
    }

    // Set encoded value of the pointer. The value can be NULL.
    void SetValueMaybeNull(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        SetValue(addr);
    }
#endif // !DACCESS_COMPILE

private:
    TADDR m_addr;
};

template<bool isMaybeNull, typename T, typename PT>
typename PT::type
ReadPointer(const T *base, const PT T::* pPointerFieldMember)
{
    LIMITED_METHOD_DAC_CONTRACT;

    uintptr_t offset = (uintptr_t) &(base->*pPointerFieldMember) - (uintptr_t) base;

    if (isMaybeNull)
    {
        return PT::GetValueMaybeNullAtPtr(dac_cast<TADDR>(base) + offset);
    }
    else
    {
        return PT::GetValueAtPtr(dac_cast<TADDR>(base) + offset);
    }
}

template<bool isMaybeNull, typename T, typename PT>
typename PT::type
ReadPointer(const T *base, const PT T::* pPointerFieldMember, bool isIndirect)
{
    LIMITED_METHOD_DAC_CONTRACT;

    uintptr_t offset = (uintptr_t) &(base->*pPointerFieldMember) - (uintptr_t) base;

    if (isMaybeNull)
    {
        return PT::GetValueMaybeNullAtPtrIndirect(dac_cast<TADDR>(base) + offset, isIndirect, offset);
    }
    else
    {
        return PT::GetValueAtPtrIndirect(dac_cast<TADDR>(base) + offset, isIndirect, offset);
    }
}

template<typename T, typename PT>
typename PT::type
ReadPointerMaybeNull(const T *base, const PT T::* pPointerFieldMember)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return ReadPointer<true>(base, pPointerFieldMember);
}

template<typename T, typename PT>
typename PT::type
ReadPointerMaybeNull(const T *base, const PT T::* pPointerFieldMember, bool isIndirect)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return ReadPointer<true>(base, pPointerFieldMember, isIndirect);
}

template<typename T, typename PT>
typename PT::type
ReadPointer(const T *base, const PT T::* pPointerFieldMember)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return ReadPointer<false>(base, pPointerFieldMember);
}

template<typename T, typename PT>
typename PT::type
ReadPointer(const T *base, const PT T::* pPointerFieldMember, bool isIndirect)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return ReadPointer<false>(base, pPointerFieldMember, isIndirect);
}

template<bool isMaybeNull, typename T, typename C, typename PT>
typename PT::type
ReadPointer(const T *base, const C T::* pFirstPointerFieldMember, const PT C::* pSecondPointerFieldMember)
{
    LIMITED_METHOD_DAC_CONTRACT;

    const PT *ptr = &(base->*pFirstPointerFieldMember.*pSecondPointerFieldMember);
    uintptr_t offset = (uintptr_t) ptr - (uintptr_t) base;

    if (isMaybeNull)
    {
        return PT::GetValueMaybeNullAtPtr(dac_cast<TADDR>(base) + offset);
    }
    else
    {
        return PT::GetValueAtPtr(dac_cast<TADDR>(base) + offset);
    }
}

template<typename T, typename C, typename PT>
typename PT::type
ReadPointerMaybeNull(const T *base, const C T::* pFirstPointerFieldMember, const PT C::* pSecondPointerFieldMember)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return ReadPointer<true>(base, pFirstPointerFieldMember, pSecondPointerFieldMember);
}

template<typename T, typename C, typename PT>
typename PT::type
ReadPointer(const T *base, const C T::* pFirstPointerFieldMember, const PT C::* pSecondPointerFieldMember)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return ReadPointer<false>(base, pFirstPointerFieldMember, pSecondPointerFieldMember);
}

#endif //_FIXUPPOINTER_H
