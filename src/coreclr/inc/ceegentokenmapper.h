// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// CeeGenTokenMapper.h
//
// This helper class tracks mapped tokens from their old value to the new value
// which can happen when the data is optimized on save.
//
//*****************************************************************************

#ifndef __CeeGenTokenMapper_h__
#define __CeeGenTokenMapper_h__

#include "utilcode.h"

typedef CDynArray<mdToken> TOKENMAP;

#define INDEX_OF_TYPE(type) ((type) >> 24)
//r#define INDEX_FROM_TYPE(type) case INDEX_OF_TYPE(mdt ## type): return (tkix ## type)

// Define the list of CeeGen tracked tokens
#define CEEGEN_TRACKED_TOKENS()             \
    CEEGEN_TRACKED_TOKEN(TypeDef)           \
    CEEGEN_TRACKED_TOKEN(InterfaceImpl)     \
    CEEGEN_TRACKED_TOKEN(MethodDef)         \
    CEEGEN_TRACKED_TOKEN(TypeRef)           \
    CEEGEN_TRACKED_TOKEN(MemberRef)         \
    CEEGEN_TRACKED_TOKEN(CustomAttribute)   \
    CEEGEN_TRACKED_TOKEN(FieldDef)          \
    CEEGEN_TRACKED_TOKEN(ParamDef)          \
    CEEGEN_TRACKED_TOKEN(File)              \
    CEEGEN_TRACKED_TOKEN(GenericParam)      \

class CCeeGen;

#define CEEGEN_TRACKED_TOKEN(x) tkix ## x,

class CeeGenTokenMapper : public IMapToken
{
friend class CCeeGen;
friend class PESectionMan;
public:
    enum
    {
        CEEGEN_TRACKED_TOKENS()
        MAX_TOKENMAP
    };

    static int IndexForType(mdToken tk);

    CeeGenTokenMapper() : m_pIImport(0), m_cRefs(1) { LIMITED_METHOD_CONTRACT; }
    virtual ~CeeGenTokenMapper() {}

//*****************************************************************************
// IUnknown implementation.
//*****************************************************************************
    virtual ULONG STDMETHODCALLTYPE AddRef()
    {LIMITED_METHOD_CONTRACT;  return ++m_cRefs; }

    virtual ULONG STDMETHODCALLTYPE Release()
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;
        SUPPORTS_DAC_HOST_ONLY;

        ULONG cRefs = --m_cRefs;
        if (cRefs == 0)
        {
            delete this;
        }
        return cRefs;
    }

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, PVOID *ppIUnk);

//*****************************************************************************
// Called by the meta data engine when a token is remapped to a new location.
// This value is recorded in the m_rgMap array based on type and rid of the
// from token value.
//*****************************************************************************
    virtual HRESULT STDMETHODCALLTYPE Map(mdToken tkImp, mdToken tkEmit);

//*****************************************************************************
// Check the given token to see if it has moved to a new location.  If so,
// return true and give back the new token.
//*****************************************************************************
    virtual int HasTokenMoved(mdToken tkFrom, mdToken &tkTo);

    int GetMaxMapSize() const
    { LIMITED_METHOD_CONTRACT; return (MAX_TOKENMAP); }

    IUnknown *GetMapTokenIface() const
    { LIMITED_METHOD_CONTRACT; return ((IUnknown *) this); }


//*****************************************************************************
// Hand out a copy of the meta data information.
//*****************************************************************************
    virtual HRESULT GetMetaData(IMetaDataImport **ppIImport);

protected:
// m_rgMap is an array indexed by token type.  For each type, an array of
// tokens is kept, indexed by from rid.  To see if a token has been moved,
// do a lookup by type to get the right array, then use the from rid to
// find the to rid.
    TOKENMAP    m_rgMap[MAX_TOKENMAP];
    IMetaDataImport *m_pIImport;
    ULONG       m_cRefs;                // Ref count.
};

#endif // __CeeGenTokenMapper_h__
