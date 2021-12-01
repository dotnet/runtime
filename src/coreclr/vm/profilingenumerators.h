// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// FILE: ProfilingEnumerators.h
//
// All enumerators returned by the profiling API to enumerate objects or to catch up on
// the current CLR state (usually for attaching profilers) are defined in
// ProfilingEnumerators.h,cpp.
//
// This header file contains the base enumerator template implementation, plus the
// definitions of the derived enumerators.
//


#ifndef __PROFILINGENUMERATORS_H__
#define __PROFILINGENUMERATORS_H__


//---------------------------------------------------------------------------------------
//
// ProfilerEnum
//
// This class is a one-size-fits-all implementation for COM style enumerators
//
// Template parameters:
//      EnumInterface -- the parent interface for this enumerator
//                       (e.g., ICorProfilerObjectEnum)
//      Element -- the type of the objects this enumerator returns.
//
//
template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
class ProfilerEnum : public EnumInterface
{
public:
    ProfilerEnum(CDynArray< Element >* elements);
    ProfilerEnum();
    virtual ~ProfilerEnum();

    // IUnknown functions

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, void** pInterface);
    virtual ULONG STDMETHODCALLTYPE AddRef();
    virtual ULONG STDMETHODCALLTYPE Release();


    // This template assumes that the enumerator confors to the interface
    //
    // (this matches the IEnumXXX interface documented in MSDN)

    virtual HRESULT STDMETHODCALLTYPE Skip(ULONG count);
    virtual HRESULT STDMETHODCALLTYPE Reset();
    virtual HRESULT STDMETHODCALLTYPE Clone(EnumInterface** ppEnum);
    virtual HRESULT STDMETHODCALLTYPE GetCount(ULONG *count);
    virtual HRESULT STDMETHODCALLTYPE Next(ULONG count,
        Element elements[],
        ULONG* elementsFetched);


protected:
    ULONG m_currentElement;

    CDynArray< Element > m_elements;

    LONG m_refCount;
};

//
//
//  ProfilerEnum implementation
//
//


//
// ProfilerEnum::ProfilerEnum
//
// Description
//      The enumerator constructor
//
// Parameters
//      elements -- the array of elements in the enumeration.
//
// Notes
//      The enumerator does NOT take ownership of data in the array of elements;
//      it maintains its own private copy.
//
// <TODO>
// nickbe 12/12/2003 11:31:34
//
// If someone comes back and complains that the enumerators are too slow or use
// too much memory, I can reference count or otherwise garbage collect the data
// used by the enumerators
// </TODO>
//
//
template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::ProfilerEnum(CDynArray< Element >* elements) :
    m_currentElement(0),
    m_refCount(1)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    const ULONG count = elements->Count();
    m_elements.AllocateBlockThrowing(count);

    for (ULONG i = 0; i < count; ++i)
    {
        m_elements[i] = (*elements)[i];
    }
}

template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::ProfilerEnum() :
    m_currentElement(0),
    m_refCount(1)
{
}


//
// ProfilerEnum::ProfileEnum
//
// Description
//      Destructor for enumerators
//
// Parameters
//      None
//
// Returns
//      None
//
template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::~ProfilerEnum()
{
}

//
// ProfilerEnum::QueryInterface
//
// Description
//      dynamically cast this object to a specific interface.
//
// Parameters
//      id          -- the interface ID requested
//      ppInterface -- [out] pointer to the appropriate interface
//
// Returns
//      S_OK            -- if the QueryInterface succeeded
//      E_NOINTERFACE   -- if the enumerator does not implement the requested interface
//

template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
HRESULT
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::QueryInterface(REFIID id, void** pInterface)
{
    if (IID_EnumInterface == id)
    {
        *pInterface = static_cast< EnumInterface* >(this);
    }
    else if (IID_IUnknown == id)
    {
        *pInterface = static_cast< IUnknown* >(this);
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    this->AddRef();
    return S_OK;
}

template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
ULONG
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::AddRef()
{
    return InterlockedIncrement(&m_refCount);
}

template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
ULONG
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::Release()
{
    ULONG refCount = InterlockedDecrement(&m_refCount);

    if (0 == refCount)
    {
        delete this;
    }

    return refCount;
}

//
// ProfilerEnum::Next
//
// Description
//    Retrieves elements from the enumeration and advances the enumerator
//
// Parameters
//    elementsRequested -- the number of elements to read
//    elements -- [out] an array to store the retrieved elements
//    elementsFetched -- [out] the number of elements actually retrieved
//
//
// Returns
//    S_OK -- elementedRequested was fully satisfied
//    S_FALSE -- less than elementsRequested were returned
//    E_INVALIDARG
//
// Notes
//    if elementsRequested is 1 and elementsFetched is NULL, the enumerator will
//    try to advance 1 item and return S_OK if it is successful
//

template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
HRESULT
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::Next(ULONG elementsRequested,
                                                                Element elements[],
                                                                ULONG* elementsFetched)
{
    // sanity check the location of the iterator
    _ASSERTE(0 <= m_currentElement);
    _ASSERTE(m_currentElement <= static_cast< ULONG >(m_elements.Count()));

    // It's illegal to try and advance more than one element without giving a
    // legitimate pointer for elementsRequested
    if ((NULL == elementsFetched) && (1 < elementsRequested))
    {
        return E_INVALIDARG;
    }

    //  If, for some reason, you ask for zero elements, well, we'll just tell
    //  you that's fine.
    if (0 == elementsRequested)
    {
        if (NULL != elementsFetched)
        {
            *elementsFetched = 0;
        }

        return S_OK;
    }

    if (elements == NULL)
    {
        return E_INVALIDARG;
    }

    // okay, enough with the corner cases.

    // We don't want to walk past the end of our array, so figure out how far we
    // need to walk.
    const ULONG elementsToCopy = min(elementsRequested, m_elements.Count() - m_currentElement);

    for (ULONG i = 0; i < elementsToCopy; ++i)
    {
        elements[i] = m_elements[m_currentElement + i];
    }

    // advance the enumerator
    m_currentElement += elementsToCopy;

    // sanity check that we haven't gone any further than we were supposed to
    _ASSERTE(0 <= m_currentElement);
    _ASSERTE(m_currentElement <= static_cast< ULONG >(m_elements.Count()));


    if (NULL != elementsFetched)
    {
        *elementsFetched = elementsToCopy;
    }

    if (elementsToCopy < elementsRequested)
    {
        return S_FALSE;
    }

    return S_OK;
}


//
// ProfilerEnum:GetCount
//
// Description
//   Computes the number of elements remaining in the enumeration
//
// Parameters
//   count -- [out] the number of element remaining in the enumeration
//
// Returns
//   S_OK
//   E_INVALIDARG -- if count is an invalid pointer
//
//

template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
HRESULT
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::GetCount(ULONG* count)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (NULL == count)
    {
        return E_INVALIDARG;
    }

    *count = m_elements.Count() - m_currentElement;

    return S_OK;
}

//
// ProfilerEnum::Skip
//
// Description
//   Advances the enumerator without retrieving any elements.
//
// Parameters
//   count  -- number of elements to skip
//
// Returns
//   S_OK     -- if the number of elements skipped was equal to count
//   S_FALSE  -- if the number of elements skipped was less than count
//
//
// TODO
//
// The API for IEnumXXX listed in MSDN here is broken. We should really have an
// out parameter that represents the number of elements actually skipped ... all
// though you could theoretically work that number out by calling GetCount()
// before and after calling Skip()
//
//
template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
HRESULT
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::Skip(ULONG count)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    const ULONG elementsToSkip = min(count, m_elements.Count() - m_currentElement);
    m_currentElement += elementsToSkip;

    if (elementsToSkip < count)
    {
        return S_FALSE;
    }

    return S_OK;
}



//
// ProfilerEnum::Reset
//
// Description
//  Returns the enumerator to the beginning of the enumeration
//
// Parameters
//  None
//
// Returns
//  S_OK -- always (function never fails)
//
//

template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
HRESULT
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::Reset()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_currentElement = 0;
    return S_OK;
}

//
// ProfilerEnum::Clone
//
// Description
//  Creates a copy of this enumerator.
//
// Parameters
//  None
//
// Returns
//   S_OK           -- if copying is successful
//   E_OUTOFMEMORY  -- if OOM occurs
//   E_INVALIDARG   -- if pInterface is an invalid pointer
//

template< typename EnumInterface, REFIID IID_EnumInterface, typename Element >
HRESULT
ProfilerEnum< EnumInterface, IID_EnumInterface, Element >::Clone(EnumInterface** pInterface)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

    }
    CONTRACTL_END;

    if (pInterface == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        *pInterface = new ProfilerEnum< EnumInterface, IID_EnumInterface, Element >(&m_elements);
    }
    EX_CATCH
    {
        *pInterface = NULL;
        hr = E_OUTOFMEMORY;
    }
    EX_END_CATCH(RethrowTerminalExceptions)

    return hr;
}

// ---------------------------------------------------------------------------------------
// Enumerators have their base class defined here, as an instantiation of ProfilerEnum
// ---------------------------------------------------------------------------------------

typedef ProfilerEnum< ICorProfilerObjectEnum, IID_ICorProfilerObjectEnum, ObjectID > ProfilerObjectEnum;
typedef ProfilerEnum< ICorProfilerFunctionEnum, IID_ICorProfilerFunctionEnum, COR_PRF_FUNCTION > ProfilerFunctionEnumBase;
typedef ProfilerEnum< ICorProfilerModuleEnum, IID_ICorProfilerModuleEnum, ModuleID > ProfilerModuleEnumBase;
typedef ProfilerEnum< ICorProfilerThreadEnum, IID_ICorProfilerThreadEnum, ThreadID > ProfilerThreadEnumBase;
typedef ProfilerEnum< ICorProfilerMethodEnum, IID_ICorProfilerMethodEnum, COR_PRF_METHOD > ProfilerMethodEnum;

// ---------------------------------------------------------------------------------------
// This class derives from the template enumerator instantiation, and provides specific
// code to populate the enumerator with the function list

class ProfilerFunctionEnum : public ProfilerFunctionEnumBase
{
public:
    BOOL Init(BOOL fWithReJITIDs = FALSE);
};


// ---------------------------------------------------------------------------------------
// This class derives from the template enumerator instantiation, and provides specific
// code to populate the enumerator with the module list

class ProfilerModuleEnum : public ProfilerModuleEnumBase
{
public:
    HRESULT Init();
    HRESULT AddUnsharedModulesFromAppDomain(AppDomain * pAppDomain);
    HRESULT AddUnsharedModule(Module * pModule);
};


class IterateAppDomainContainingModule
{
public:
    IterateAppDomainContainingModule(Module * pModule, ULONG32 cAppDomainIds, ULONG32 * pcAppDomainIds, AppDomainID * pAppDomainIds)
        : m_pModule(pModule), m_cAppDomainIds(cAppDomainIds), m_pcAppDomainIds(pcAppDomainIds), m_rgAppDomainIds(pAppDomainIds), m_index(0)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE((pModule != NULL) &&
                 ((m_rgAppDomainIds != NULL) || (m_cAppDomainIds == 0)) &&
                 (m_pcAppDomainIds != NULL));
    }

    HRESULT PopulateArray();

    HRESULT AddAppDomainContainingModule(AppDomain * pAppDomain);

private:
    Module *      m_pModule;
    ULONG32       m_cAppDomainIds;
    ULONG32 *     m_pcAppDomainIds;
    AppDomainID * m_rgAppDomainIds;
    ULONG32       m_index;
};


// ---------------------------------------------------------------------------------------
// This class derives from the template enumerator instantiation, and provides specific
// code to populate the enumerator with the thread store
class ProfilerThreadEnum : public ProfilerThreadEnumBase
{

public :
    HRESULT Init();
};

#endif //__PROFILINGENUMERATORS_H__
