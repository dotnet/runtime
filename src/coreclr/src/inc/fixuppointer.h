//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
//  - GetValueAtPtr/SetValueAtPtr: Static version of GetValue/SetValue. It is 
//    meant to simplify access to arrays of RelativePointers.
//  - GetValueMaybeNullAtPtr/SetValueMaybeNullAtPtr
template<typename PTR_TYPE>
class RelativePointer
{
public:
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

    // Set encoded value of the pointer. Assumes that the value is not NULL.
    void SetValue(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(addr != NULL);
        m_delta = dac_cast<TADDR>(addr) - base;
    }

#ifndef DACCESS_COMPILE
    // Set encoded value of the pointer. Assumes that the value is not NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    FORCEINLINE void SetValue(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        return SetValue((TADDR)this, addr);
    }
#endif

    // Static version of SetValue. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static void SetValueAtPtr(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        dac_cast<DPTR(RelativePointer<PTR_TYPE>)>(base)->SetValue(base, addr);
    }

    // Set encoded value of the pointer. The value can be NULL.
    void SetValueMaybeNull(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        if (addr == NULL) m_delta = NULL; else SetValue(base, addr);
    }

#ifndef DACCESS_COMPILE
    // Set encoded value of the pointer. The value can be NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    FORCEINLINE void SetValueMaybeNull(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        return SetValueMaybeNull((TADDR)this, addr);
    }
#endif

    // Static version of SetValueMaybeNull. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static void SetValueMaybeNullAtPtr(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        dac_cast<DPTR(RelativePointer<PTR_TYPE>)>(base)->SetValueMaybeNull(base, addr);
    }

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

    // Returns the pointer to the indirection cell.
    PTR_TYPE * GetValuePtr() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        TADDR addr = m_addr;
        if ((addr & FIXUP_POINTER_INDIRECTION) != 0)
            return dac_cast<DPTR(PTR_TYPE)>(addr - FIXUP_POINTER_INDIRECTION);
        return (PTR_TYPE *)&m_addr;
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

    void SetValue(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        m_addr = dac_cast<TADDR>(addr);
    }

private:
    TADDR m_addr;
};

//----------------------------------------------------------------------------
// RelativeFixupPointer is combination of RelativePointer and FixupPointer
template<typename PTR_TYPE>
class RelativeFixupPointer
{
public:
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

    // Set encoded value of the pointer. Assumes that the value is not NULL.
    void SetValue(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(addr != NULL);
        m_delta = dac_cast<TADDR>(addr) - base;
    }

#ifndef DACCESS_COMPILE
    // Set encoded value of the pointer. Assumes that the value is not NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    FORCEINLINE void SetValue(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        return SetValue((TADDR)this, addr);
    }
#endif

    // Static version of SetValue. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static void SetValueAtPtr(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        dac_cast<DPTR(RelativeFixupPointer<PTR_TYPE>)>(base)->SetValue(base, addr);
    }

    // Set encoded value of the pointer. The value can be NULL.
    void SetValueMaybeNull(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        if (addr == NULL) m_delta = NULL; else SetValue(base, addr);
    }

#ifndef DACCESS_COMPILE
    // Set encoded value of the pointer. The value can be NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    FORCEINLINE void SetValueMaybeNull(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        return SetValueMaybeNull((TADDR)this, addr);
    }
#endif

    // Static version of SetValueMaybeNull. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static void SetValueMaybeNullAtPtr(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        dac_cast<DPTR(RelativeFixupPointer<PTR_TYPE>)>(base)->SetValueMaybeNull(base, addr);
    }

    // Returns the pointer to the indirection cell.
    PTR_TYPE * GetValuePtr(TADDR base) const
    {
        LIMITED_METHOD_CONTRACT;
        TADDR addr = base + m_delta;
        _ASSERTE((addr & FIXUP_POINTER_INDIRECTION) != 0);
        return dac_cast<DPTR(PTR_TYPE)>(addr - FIXUP_POINTER_INDIRECTION);
    }

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

private:
#ifndef DACCESS_COMPILE
    Volatile<TADDR> m_delta;
#else
    TADDR m_delta;
#endif
};

// Fixup used for RelativePointer
#define IMAGE_REL_BASED_RelativePointer IMAGE_REL_BASED_RELPTR

#else // FEATURE_PREJIT

//----------------------------------------------------------------------------
// PlainPointer is simple pointer wrapper to support compilation without indirections
// This is useful for building size-constrained runtime without NGen support.
template<typename PTR_TYPE>
class PlainPointer
{
public:
    // Returns whether the encoded pointer is NULL.
    BOOL IsNull() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_ptr == NULL;
    }

    // Returns whether the indirection cell contain fixup that has not been converted to real pointer yet.
    BOOL IsTagged() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_ptr & 1;
    }

    // Returns value of the encoded pointer.
    PTR_TYPE GetValue() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<PTR_TYPE>(m_ptr);
    }

    // Returns the pointer to the indirection cell.
    PTR_TYPE * GetValuePtr() const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (PTR_TYPE *)&m_ptr;
    }

    // Returns value of the encoded pointer. Assumes that the pointer is not NULL.
    PTR_TYPE GetValue(TADDR base) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        PRECONDITION(!IsNull());
        return dac_cast<PTR_TYPE>(m_ptr);
    }

    // Static version of GetValue. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static PTR_TYPE GetValueAtPtr(TADDR base)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<DPTR(PlainPointer<PTR_TYPE>)>(base)->GetValue(base);
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

    void SetValue(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        m_ptr = dac_cast<TADDR>(addr);
    }

    // Set encoded value of the pointer. Assumes that the value is not NULL.
    void SetValue(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(addr != NULL);
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

#ifndef DACCESS_COMPILE
    // Set encoded value of the pointer. The value can be NULL.
    // Does not need explicit base and thus can be used in non-DAC builds only.
    FORCEINLINE void SetValueMaybeNull(PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        return SetValueMaybeNull((TADDR)this, addr);
    }
#endif

    // Static version of SetValueMaybeNull. It is meant to simplify access to arrays of pointers.
    FORCEINLINE static void SetValueMaybeNullAtPtr(TADDR base, PTR_TYPE addr)
    {
        LIMITED_METHOD_CONTRACT;
        dac_cast<DPTR(PlainPointer<PTR_TYPE>)>(base)->SetValueMaybeNull(base, addr);
    }

private:
    TADDR m_ptr;
};

#define FixupPointer PlainPointer
#define RelativePointer PlainPointer
#define RelativeFixupPointer PlainPointer

#endif // FEATURE_PREJIT

//----------------------------------------------------------------------------
// RelativePointer32 is pointer encoded as relative 32-bit offset. It is used
// to reduce both the size of the pointer itself as well as size of relocation 
// section for pointers that live exlusively in NGen images.
template<typename PTR_TYPE>
class RelativePointer32
{
public:
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

#endif //_FIXUPPOINTER_H
