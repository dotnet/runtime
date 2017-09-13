// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*****************************************************************************\
*                                                                             *
* FixupPointer.h -  Fixup pointer holder types                                *
*                                                                             *
\*****************************************************************************/

#ifndef _FIXUPPOINTER_H
#define _FIXUPPOINTER_H

#include "daccess.h"

#ifdef FEATURE_PREJIT

//----------------------------------------------------------------------------
// RelativePointer is pointer encoded as relative offset. It is used to reduce size of
// relocation section in NGen images. Conversion from/to RelativePointer needs 
// address of the pointer ("this") converted to TADDR passed in from outside. 
// Converting "this" to TADDR is not possible in the DAC transparently because
// DAC is based on exact pointers, not ranges.
// There are several flavors of conversions from/to RelativePointer:
//  - GetValue/SetValue: The most common version. Assumes that the pointer is not NULL.
//  - GetValueMaybeNull/SetValueMaybeNull: Pointer can be NULL.
//  - GetValueAtPtr: Static version of GetValue. It is
//    meant to simplify access to arrays of RelativePointers.
//  - GetValueMaybeNullAtPtr
template<typename PTR_TYPE>
class RelativePointer
{
public:

    static constexpr bool isRelative = true;
    typedef PTR_TYPE type;

#ifndef DACCESS_COMPILE
    RelativePointer()
    {
        m_delta = (TADDR)NULL;

        _ASSERTE (IsNull());
    }
#else // DACCESS_COMPILE
    RelativePointer() =delete;
#endif // DACCESS_COMPILE

    // Implicit copy/move is not allowed
    // Bitwise copy is implemented by BitwiseCopyTo method
    RelativePointer<PTR_TYPE>(const RelativePointer<PTR_TYPE> &) =delete;
    RelativePointer<PTR_TYPE>(RelativePointer<PTR_TYPE> &&) =delete;
    RelativePointer<PTR_TYPE>& operator = (const RelativePointer<PTR_TYPE> &) =delete;
    RelativePointer<PTR_TYPE>& operator = (RelativePointer<PTR_TYPE> &&) =delete;

    // Returns whether the encoded pointer is NULL.
    BOOL IsNull() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        // Pointer pointing to itself is treated as NULL
        return m_delta == (TADDR)NULL;
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

#ifndef DACCESS_COMPILE
    // Set encoded value of the pointer. Assumes that the value is not NULL.
    FORCEINLINE void SetValue(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(addr != NULL);
        m_delta = (TADDR)addr - (TADDR)this;
    }

    // Set encoded value of the pointer. The value can be NULL.
    void SetValueMaybeNull(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        if (addr == NULL)
            m_delta = NULL;
        else
            m_delta = (TADDR)addr - (TADDR)base;
    }

    // Set encoded value of the pointer. The value can be NULL.
    FORCEINLINE void SetValueMaybeNull(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        SetValueMaybeNull((TADDR)this, addr);
    }

    FORCEINLINE void SetValueVolatile(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        SetValue(addr);
    }
#endif

#ifndef DACCESS_COMPILE
    void BitwiseCopyTo(RelativePointer<PTR_TYPE> &dest) const
    {
        dest.m_delta = m_delta;
    }
#endif // DACCESS_COMPILE

private:
#ifndef DACCESS_COMPILE
    Volatile<TADDR> m_delta;
#else
    TADDR m_delta;
#endif
};

//----------------------------------------------------------------------------
// FixupPointer is pointer with optional indirection. It is used to reduce number
// of private pages in NGen images - cross-module pointers that written to at runtime 
// are packed together and accessed via indirection.
//
// The direct flavor (lowest bit of m_addr is cleared) is user for intra-module pointers
// in NGen images, and in datastructuters allocated at runtime.
//
// The indirect mode (lowest bit of m_addr is set) is used for cross-module pointers
// in NGen images.
//

// Friendly name for lowest bit that marks the indirection
#define FIXUP_POINTER_INDIRECTION 1

template<typename PTR_TYPE>
class FixupPointer
{
public:

    static constexpr bool isRelative = false;
    typedef PTR_TYPE type;

    // Returns whether the encoded pointer is NULL.
    BOOL IsNull() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_addr == 0;
    }

    // Returns whether the indirection cell contain fixup that has not been converted to real pointer yet.
    FORCEINLINE BOOL IsTagged() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        TADDR addr = m_addr;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
             return (*PTR_TADDR(addr - FIXUP_POINTER_INDIRECTION) & 1) != 0;
        return FALSE;
    }

    // Returns value of the encoded pointer.
    FORCEINLINE PTR_TYPE GetValue() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        TADDR addr = m_addr;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
            addr = *PTR_TADDR(addr - FIXUP_POINTER_INDIRECTION);
        return dac_cast<PTR_TYPE>(addr);
    }

    // Returns value of the encoded pointer.
    FORCEINLINE PTR_TYPE GetValueMaybeNull() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetValue();
    }

#ifndef DACCESS_COMPILE
    // Returns the pointer to the indirection cell.
    PTR_TYPE * GetValuePtr() const
    {
        LIMITED_METHOD_CONTRACT;
        TADDR addr = m_addr;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
            return (PTR_TYPE *)(addr - FIXUP_POINTER_INDIRECTION);
        return (PTR_TYPE *)&m_addr;
    }
#endif // !DACCESS_COMPILE

    // Static version of GetValue. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static PTR_TYPE GetValueAtPtr(TADDR base)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(FixupPointer<PTR_TYPE>)>(base)->GetValue();
    }

    // Static version of GetValueMaybeNull. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static PTR_TYPE GetValueMaybeNullAtPtr(TADDR base)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(FixupPointer<PTR_TYPE>)>(base)->GetValueMaybeNull();
    }

    // Returns value of the encoded pointer.
    // Allows the value to be tagged.
    FORCEINLINE TADDR GetValueMaybeTagged() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        TADDR addr = m_addr;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
            addr = *PTR_TADDR(addr - FIXUP_POINTER_INDIRECTION);
        return addr;
    }

#ifndef DACCESS_COMPILE
    void SetValue(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        m_addr = dac_cast<TADDR>(addr);
    }

    void SetValueMaybeNull(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        SetValue(addr);
    }
#endif // !DACCESS_COMPILE

private:
    TADDR m_addr;
};

//----------------------------------------------------------------------------
// RelativeFixupPointer is combination of RelativePointer and FixupPointer
template<typename PTR_TYPE>
class RelativeFixupPointer
{
public:

    static constexpr bool isRelative = true;
    typedef PTR_TYPE type;

#ifndef DACCESS_COMPILE
    RelativeFixupPointer()
    {
        SetValueMaybeNull(NULL);
    }
#else // DACCESS_COMPILE
    RelativeFixupPointer() =delete;
#endif // DACCESS_COMPILE

    // Implicit copy/move is not allowed
    RelativeFixupPointer<PTR_TYPE>(const RelativeFixupPointer<PTR_TYPE> &) =delete;
    RelativeFixupPointer<PTR_TYPE>(RelativeFixupPointer<PTR_TYPE> &&) =delete;
    RelativeFixupPointer<PTR_TYPE>& operator = (const RelativeFixupPointer<PTR_TYPE> &) =delete;
    RelativeFixupPointer<PTR_TYPE>& operator = (RelativeFixupPointer<PTR_TYPE> &&) =delete;

    // Returns whether the encoded pointer is NULL.
    BOOL IsNull() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        // Pointer pointing to itself is treated as NULL
        return m_delta == (TADDR)NULL;
    }

    // Returns whether the indirection cell contain fixup that has not been converted to real pointer yet.
    FORCEINLINE BOOL IsTagged(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        TADDR addr = base + m_delta;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
             return (*PTR_TADDR(addr - FIXUP_POINTER_INDIRECTION) & 1) != 0;
        return FALSE;
    }

#ifndef DACCESS_COMPILE
    // Returns whether the indirection cell contain fixup that has not been converted to real pointer yet.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    FORCEINLINE BOOL IsTagged() const
    {
        LIMITED_METHOD_CONTRACT;
        return IsTagged((TADDR)this);
    }
#endif // !DACCESS_COMPILE

    // Returns value of the encoded pointer. Assumes that the pointer is not NULL.
    FORCEINLINE PTR_TYPE GetValue(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        PRECONDITION(!IsNull());
        PRECONDITION(!IsTagged(base));
        TADDR addr = base + m_delta;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
            addr = *PTR_TADDR(addr - FIXUP_POINTER_INDIRECTION);
        return dac_cast<PTR_TYPE>(addr);
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
        return dac_cast<DPTR(RelativeFixupPointer<PTR_TYPE>)>(base)->GetValue(base);
    }

    // Returns value of the encoded pointer. The pointer can be NULL.
    PTR_TYPE GetValueMaybeNull(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        PRECONDITION(!IsTagged(base));

        // Cache local copy of delta to avoid races when the value is changing under us.
        TADDR delta = m_delta;

        if (delta == 0)
            return NULL;

        TADDR addr = base + delta;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
            addr = *PTR_TADDR(addr - FIXUP_POINTER_INDIRECTION);
        return dac_cast<PTR_TYPE>(addr);
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
        return dac_cast<DPTR(RelativeFixupPointer<PTR_TYPE>)>(base)->GetValueMaybeNull(base);
    }

#ifndef DACCESS_COMPILE
    // Set encoded value of the pointer. Assumes that the value is not NULL.
    FORCEINLINE void SetValue(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(addr != NULL);
        m_delta = dac_cast<TADDR>(addr) - (TADDR)this;
    }

    // Set encoded value of the pointer. The value can be NULL.
    void SetValueMaybeNull(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        if (addr == NULL)
            m_delta = NULL;
        else
            m_delta = dac_cast<TADDR>(addr) - (TADDR)base;
    }

    // Set encoded value of the pointer. The value can be NULL.
    FORCEINLINE void SetValueMaybeNull(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        SetValueMaybeNull((TADDR)this, addr);
    }
#endif

#ifndef DACCESS_COMPILE
    // Returns the pointer to the indirection cell.
    PTR_TYPE * GetValuePtr() const
    {
        LIMITED_METHOD_CONTRACT;
        TADDR addr = ((TADDR)this) + m_delta;
        _ASSERTE((addr & FIXUP_POINTER_INDIRECTION) != 0);
        return (PTR_TYPE *)(addr - FIXUP_POINTER_INDIRECTION);
    }
#endif // !DACCESS_COMPILE

    // Returns value of the encoded pointer. Assumes that the pointer is not NULL. 
    // Allows the value to be tagged.
    FORCEINLINE TADDR GetValueMaybeTagged(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        PRECONDITION(!IsNull());
        TADDR addr = base + m_delta;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
            addr = *PTR_TADDR(addr - FIXUP_POINTER_INDIRECTION);
        return addr;
    }

    // Returns whether pointer is indirect. Assumes that the value is not NULL.
    bool IsIndirectPtr(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        PRECONDITION(!IsNull());

        TADDR addr = base + m_delta;

        return (addr & FIXUP_POINTER_INDIRECTION) != 0;
    }

#ifndef DACCESS_COMPILE
    // Returns whether pointer is indirect. Assumes that the value is not NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    bool IsIndirectPtr() const
    {
        LIMITED_METHOD_CONTRACT;
        return IsIndirectPtr((TADDR)this);
    }
#endif

    // Returns whether pointer is indirect. The value can be NULL.
    bool IsIndirectPtrMaybeNull(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (m_delta == 0)
            return false;

        return IsIndirectPtr(base);
    }

#ifndef DACCESS_COMPILE
    // Returns whether pointer is indirect. The value can be NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    bool IsIndirectPtrMaybeNull() const
    {
        LIMITED_METHOD_CONTRACT;
        return IsIndirectPtrMaybeNull((TADDR)this);
    }
#endif

private:
#ifndef DACCESS_COMPILE
    Volatile<TADDR> m_delta;
#else
    TADDR m_delta;
#endif
};

// Fixup used for RelativePointer
#define IMAGE_REL_BASED_RelativePointer IMAGE_REL_BASED_RELPTR

#endif // FEATURE_PREJIT

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
        return IsTagged();
    }

    // Returns whether the indirection cell contain fixup that has not been converted to real pointer yet.
    BOOL IsTagged() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        TADDR addr = m_ptr;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
             return (*PTR_TADDR(addr - FIXUP_POINTER_INDIRECTION) & 1) != 0;
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
        TADDR addr = m_ptr;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
            return (PTR_TYPE *)(addr - FIXUP_POINTER_INDIRECTION);
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

    // Returns whether pointer is indirect. Assumes that the value is not NULL.
    bool IsIndirectPtr(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_ptr & FIXUP_POINTER_INDIRECTION) != 0;
    }

#ifndef DACCESS_COMPILE
    // Returns whether pointer is indirect. Assumes that the value is not NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    bool IsIndirectPtr() const
    {
        LIMITED_METHOD_CONTRACT;
        return IsIndirectPtr((TADDR)this);
    }
#endif

    // Returns whether pointer is indirect. The value can be NULL.
    bool IsIndirectPtrMaybeNull(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return IsIndirectPtr(base);
    }

#ifndef DACCESS_COMPILE
    // Returns whether pointer is indirect. The value can be NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    bool IsIndirectPtrMaybeNull() const
    {
        LIMITED_METHOD_CONTRACT;
        return IsIndirectPtrMaybeNull((TADDR)this);
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

private:
    TADDR m_ptr;
};

#ifndef FEATURE_PREJIT

#define FixupPointer PlainPointer
#define RelativePointer PlainPointer
#define RelativeFixupPointer PlainPointer

#endif // !FEATURE_PREJIT

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

template<typename T, typename PT>
typename PT::type
ReadPointerMaybeNull(const T *base, const PT T::* pPointerFieldMember)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return ReadPointer<true>(base, pPointerFieldMember);
}

template<typename T, typename PT>
typename PT::type
ReadPointer(const T *base, const PT T::* pPointerFieldMember)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return ReadPointer<false>(base, pPointerFieldMember);
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
