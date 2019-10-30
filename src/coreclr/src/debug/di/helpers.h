// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// helpers.h
// 

//
// public helpers for debugger.
//*****************************************************************************

#ifndef _HELPERS_H
#define _HELPERS_H

//-----------------------------------------------------------------------------
// Smartpointer for internal Addref/Release
// Using Wrapper / Holder infrastructure from src\inc\Holder.h
//-----------------------------------------------------------------------------
template <typename TYPE>
inline void HolderRSRelease(TYPE *value)
{
    _ASSERTE(value != NULL);
    value->InternalRelease();
}

template <typename TYPE>
inline void HolderRSAddRef(TYPE *value)
{
    _ASSERTE(value != NULL);
    value->InternalAddRef();
}

// Smart ptrs for external refs. External refs are important
// b/c they may keep an object alive.
template <typename TYPE>
inline void HolderRSReleaseExternal(TYPE *value)
{
    _ASSERTE(value != NULL);
    value->Release();
}

template <typename TYPE>
inline void HolderRSAddRefExternal(TYPE *value)
{
    _ASSERTE(value != NULL);
    value->AddRef();
}

// The CordbBase::m_pProcess backpointer needs to adjust the external reference count, but manipulate it from
// within the RS. This means we need to skip debugging checks that ensure
// that the external count is only manipulated from outside the RS. Since we're
// skipping these checks, we call this an "Unsafe" pointer.
template <typename TYPE>
inline void HolderRSUnsafeExtRelease(TYPE *value)
{
    _ASSERTE(value != NULL);
    value->BaseRelease();
}
template <typename TYPE>
inline void HolderRSUnsafeExtAddRef(TYPE *value)
{
    _ASSERTE(value != NULL);
    value->BaseAddRef();
}

//-----------------------------------------------------------------------------
// Base Smart pointer implementation.
// This abstracts out the AddRef + Release methods.
//-----------------------------------------------------------------------------
template <typename TYPE, void (*ACQUIREF)(TYPE*), void (*RELEASEF)(TYPE*)>
class BaseSmartPtr
{
public:
    BaseSmartPtr () {
        // Ensure that these smart-ptrs are really ptr-sized.
        static_assert_no_msg(sizeof(*this) == sizeof(void*));
        m_ptr = NULL;
    }
    explicit BaseSmartPtr (TYPE * ptr) : m_ptr(NULL) {
        if (ptr != NULL)
        {
            RawAcquire(ptr);
        }
    }

    ~BaseSmartPtr() {
        Clear();
    }

    FORCEINLINE void Assign(TYPE * ptr)
    {
        // Do the AddRef before the release to avoid the release pinging 0 if we assign to ourself.
        if (ptr != NULL)
        {
            ACQUIREF(ptr);
        }
        if (m_ptr != NULL)
        {
            RELEASEF(m_ptr);
        }
        m_ptr = ptr;
    };

    FORCEINLINE void Clear()
    {
        if (m_ptr != NULL)
        {
            RawRelease();
        }
    }

    FORCEINLINE operator TYPE*() const
    {
        return m_ptr;
    }

    FORCEINLINE TYPE* GetValue() const
    {
        return m_ptr;
    }

    FORCEINLINE TYPE** operator & ()
    {
        // We allow getting the address so we can pass it in as an outparam. 
        // BTW/@TODO: this is a subtle and dangerous thing to do, since it easily leads to situations
        // when pointer gets assigned without the ref counter being incremented.
        // This can cause premature freeing of the object after the pointer dtor was called.

        // But if we have a non-null m_Ptr, then it may get silently overwritten,
        // and thus we'll lose the chance to call release on it.
        // So we'll just avoid that pattern and assert to enforce it.
        _ASSERTE(m_ptr == NULL);
        return &m_ptr;
    }

    // For legacy purposes, some pre smart-pointer code needs to be able to get the
    // address of the pointer. This is needed for RSPtrArray::GetAddrOfIndex.
    FORCEINLINE TYPE** UnsafeGetAddr()
    {
        return &m_ptr;
    }    

    FORCEINLINE TYPE* operator->()
    {
        return m_ptr;
    }

    FORCEINLINE int operator==(TYPE* p)
    {
        return (m_ptr == p);
    }

    FORCEINLINE int operator!= (TYPE* p)
    {
        return (m_ptr != p);
    }

private:
    TYPE * m_ptr;

    // Don't allow copy ctor. Explicitly don't define body to force linker errors if they're called.
    BaseSmartPtr(BaseSmartPtr<TYPE,ACQUIREF,RELEASEF> & other);
    void operator=(BaseSmartPtr<TYPE,ACQUIREF,RELEASEF> & other);

    void RawAcquire(TYPE * p)
    {
        _ASSERTE(m_ptr == NULL);
        m_ptr= p;
        ACQUIREF(m_ptr);
    }
    void RawRelease()
    {
        _ASSERTE(m_ptr != NULL);
        RELEASEF(m_ptr);
        m_ptr = NULL;
    }

};

//-----------------------------------------------------------------------------
// Helper to make it easy to declare new SmartPtrs
//-----------------------------------------------------------------------------
#define DECLARE_MY_NEW_HOLDER(NAME, ADDREF, RELEASE) \
template<typename TYPE> \
class NAME : public BaseSmartPtr<TYPE, ADDREF, RELEASE> { \
public: \
    NAME() { }; \
    NAME(NAME & other) { this->Assign(other.GetValue()); } \
    explicit NAME(TYPE * p) : BaseSmartPtr<TYPE, ADDREF, RELEASE>(p) { }; \
    FORCEINLINE NAME * GetAddr() { return this; } \
    void operator=(NAME & other) { this->Assign(other.GetValue()); } \
}; \

//-----------------------------------------------------------------------------
// Declare the various smart ptrs.
//-----------------------------------------------------------------------------
DECLARE_MY_NEW_HOLDER(RSSmartPtr, HolderRSAddRef, HolderRSRelease);
DECLARE_MY_NEW_HOLDER(RSExtSmartPtr, HolderRSAddRefExternal, HolderRSReleaseExternal); 

// The CordbBase::m_pProcess backpointer needs to adjust the external reference count, but manipulate it from
// within the RS. This means we need to skip debugging checks that ensure
// that the external count is only manipulated from outside the RS. Since we're
// skipping these checks, we call this an "Unsafe" pointer.
// This is purely used by CordbBase::m_pProcess. 
DECLARE_MY_NEW_HOLDER(RSUnsafeExternalSmartPtr, HolderRSUnsafeExtAddRef, HolderRSUnsafeExtRelease); 



#endif // _HELPERS_H

