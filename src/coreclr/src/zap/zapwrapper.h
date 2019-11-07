// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapWrapper.h
//

//
// ZapNode that wraps EE datastructure for zapping
//
// ======================================================================================

#ifndef __ZAPWRAPPER_H__
#define __ZAPWRAPPER_H__

//
// When generating compiled code for IL, the compiler will need to embed
// handles, which are pointers to various data structures. The data
// structures may either not exist, or we may not have some information
// we need for optimal code gen.
//
// In such cases, we use placeholders. Compiled code embed pointers
// to placeholders, which then have rich information about the
// referenced data structure.
//
// Once information is finally available for the exact code required,
// ZapWrapper::Resolve makes the place holder to point to the intended target.
//

class ZapWrapper : public ZapNode
{
    PVOID m_handle;

public:
    ZapWrapper(PVOID handle)
        : m_handle(handle)
    {
    }

    ZapWrapper()
    {
    }

    void SetHandle(PVOID handle)
    {
        _ASSERTE(m_handle == NULL);
        _ASSERTE(handle != NULL);
        m_handle = handle;
    }

    PVOID GetHandle()
    {
        return m_handle;
    }

    virtual ZapNode * GetBase()
    {
        return this;
    }

    virtual void Resolve(ZapImage * pImage)
    {
    }
};

class ZapWrapperTable
{
    class WrapperTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ZapWrapper *> >
    {
    public:
        typedef PVOID key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e->GetHandle();
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return k1 == k2;
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k;
        }

        static element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
    };

    typedef SHash< WrapperTraits > WrapperTable;

    WrapperTable m_entries;
    ZapImage * m_pImage;

    //
    // Helper for inserting actual implementations of placeholders into hashtable
    //
    template < typename impl, ZapNodeType type >
    ZapNode * GetPlaceHolder(PVOID handle)
    {
        ZapWrapper * pPlaceHolder = m_entries.Lookup(handle);

        if (pPlaceHolder != NULL)
        {
            _ASSERTE(pPlaceHolder->GetType() == type);
            return pPlaceHolder;
        }

        pPlaceHolder = new (m_pImage->GetHeap()) impl();
        _ASSERTE(pPlaceHolder->GetType() == type);
        pPlaceHolder->SetHandle(handle);
        m_entries.Add(pPlaceHolder);
        return pPlaceHolder;
    }

public:
    ZapWrapperTable(ZapImage * pImage)
        : m_pImage(pImage)
    {
    }

    void Preallocate(COUNT_T cbILImage)
    {
        PREALLOCATE_HASHTABLE(ZapWrapperTable::m_entries, 0.0013, cbILImage);
    }

    ZapNode * GetMethodHandle(CORINFO_METHOD_HANDLE handle);
    ZapNode * GetClassHandle(CORINFO_CLASS_HANDLE handle);
    ZapNode * GetFieldHandle(CORINFO_FIELD_HANDLE handle);
    ZapNode * GetAddrOfPInvokeFixup(CORINFO_METHOD_HANDLE handle);
    ZapNode * GetGenericHandle(CORINFO_GENERIC_HANDLE handle);
    ZapNode * GetModuleIDHandle(CORINFO_MODULE_HANDLE handle);

    ZapNode * GetStub(void * pStub);

    void Resolve();
};

#endif // __ZAPWRAPPER_H__
