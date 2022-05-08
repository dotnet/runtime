// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
//

//
// Implementation of CordbEnumerator, a templated COM enumeration pattern on Rs
// types
//*****************************************************************************

#include "rspriv.h"

// This CordbEnumerator is a templated enumerator from which COM enumerators for RS types can quickly be fashioned.
// It uses a private array to store the items it enumerates over so by default it does not reference any
// other RS type except the process it is associated with. The internal storage type does not need to match the
// the item type exposed publically so that you can easily create an enumeration that holds RsSmartPtr<CordbThread>
// but enumerates ICorDebugThread objects as an example. The enumerator has 4 templated parameters which must be
// defined:
//   ElemType: this is the item type used for storage internal to the enumerator. For most Rs objects you will want
//             to use an RsSmartPtr<T> type to ensure the enumerator holds references to the objects it is
//             containing. The enumerator does not do any explicit Add/Release, it just copies items.
//   ElemPublicType: this is the item type exposed publically via the Next enumeration method. Typically this is
//             an ICorDebugX interface type but it can be anything.
//   EnumInterfaceType: this is the COM interface that the instantiated template will implement. It is expected that
//             this interface type follows the standard ICorDebug COM enumerator pattern, that the interface inherits
//             ICorDebugEnum and defines a strongly typed Next, enumerating over the ElemPublicType.
//   GetPublicType: this is a function which converts from ElemType -> ElemPublicType. It is used to produce the
//             elements that Next actually enumerates from the internal data the enumerator stores. Two conversion
//             functions are already defined here for convenience: QueryInterfaceConvert and IdentityConvert. If
//             neither of those suits your needs then you can define your own.
//
// Note: As of right now (10/13/08) most of the ICorDebug enumerators are not implemented using this base class,
// however it might be good if we converged on this solution. There seems to be quite a bit of redundant and
// one-off enumeration code that could be eliminated.
//

// A conversion function that converts from T to U by performing COM QueryInterface
template<typename T, typename U, REFGUID IID_U>
U * QueryInterfaceConvert(T obj)
{
    U* pPublic;
    obj->QueryInterface(IID_U, (void**) &pPublic);
    return pPublic;
}

// A conversion identity function that just returns its argument
template<typename T>
T IdentityConvert(T obj)
{
    return obj;
}

// Constructor for an CordbEnumerator.
// Arguments:
//   pProcess - the CordbProcess with which to associate this enumerator
//   items - the set of items which should be enumerated
//   countItems - the number of items in the array pointed to by items
//
// Note that the items are copied into an internal array, and no reference is kept to the users array.
// Use RsSmartPtr types instead of Rs types directly to keep accurate ref counting for types which need it
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
CordbEnumerator<ElemType,
                ElemPublicType,
                EnumInterfaceType, IID_EnumInterfaceType,
                GetPublicType>::CordbEnumerator(CordbProcess *pProcess,
                                                ElemType *items,
                                                DWORD countItems) :
CordbBase(pProcess, 0, enumCordbEnumerator),
m_countItems(countItems),
m_nextIndex(0)
{
    m_items = new ElemType[countItems];
    for(UINT i = 0; i < countItems; i++)
    {
        m_items[i] = items[i];
    }
}

// Constructor for an CordbEnumerator.
// Arguments:
//   pProcess - the CordbProcess with which to associate this enumerator
//   items - the address of an array of items which should be enumerated
//   countItems - the number of items in the array pointed to by items
//
// Note that the items array is simply taken over, setting *items to NULL.
// Use RsSmartPtr types instead of Rs types directly to keep accurate ref counting for types which need it
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
CordbEnumerator<ElemType,
                ElemPublicType,
                EnumInterfaceType, IID_EnumInterfaceType,
                GetPublicType>::CordbEnumerator(CordbProcess *pProcess,
                                                ElemType **items,
                                                DWORD countItems) :
CordbBase(pProcess, 0, enumCordbEnumerator),
m_countItems(countItems),
m_nextIndex(0)
{
    _ASSERTE(items != NULL);
    m_items = *items;
    *items = NULL;
}

// Destructor
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
CordbEnumerator<ElemType,
                ElemPublicType,
                EnumInterfaceType, IID_EnumInterfaceType,
                GetPublicType>::~CordbEnumerator()
{
    // for now at least all of these enumerators should be in neuter lists and get neutered prior to destruction
    _ASSERTE(IsNeutered());
}

// COM IUnknown::QueryInterface - provides ICorDebugEnum, IUnknown, and templated EnumInterfaceType
//
// Arguments:
//     riid - IID of the interface to query for
//     ppInterface - on output set to a pointer to the desired interface
//
// Return:
//     S_OK for the supported interfaces and E_NOINTERFACE otherwise
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
HRESULT CordbEnumerator<ElemType,
                        ElemPublicType,
                        EnumInterfaceType, IID_EnumInterfaceType,
                        GetPublicType>::QueryInterface(REFIID riid, VOID** ppInterface)
{
    if(riid == __uuidof(ICorDebugEnum))
    {
        *ppInterface = static_cast<ICorDebugEnum*>(this);
        AddRef();
        return S_OK;
    }
    else if(riid == __uuidof(IUnknown))
    {
        *ppInterface = static_cast<IUnknown*>(static_cast<CordbBase*>(this));
        AddRef();
        return S_OK;
    }
    else if(riid == __uuidof(EnumInterfaceType))
    {
        *ppInterface = static_cast<EnumInterfaceType*>(this);
        AddRef();
        return S_OK;
    }
    else
    {
        return E_NOINTERFACE;
    }
}

// COM IUnknown::AddRef()
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
ULONG CordbEnumerator<ElemType,
                      ElemPublicType,
                      EnumInterfaceType, IID_EnumInterfaceType,
                      GetPublicType>::AddRef()
{
    return BaseAddRef();
}

// COM IUnknown::Release()
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
ULONG CordbEnumerator<ElemType,
                      ElemPublicType,
                      EnumInterfaceType, IID_EnumInterfaceType,
                      GetPublicType>::Release()
{
    return BaseRelease();
}

// ICorDebugEnum::Clone
// Makes a duplicate of the enumeration. The internal items are copied by value and there is no explicit reference
// between the new and old enumerations.
//
//  Arguments:
//    ppEnum - on output filled with a duplicate enumeration
//
//  Return:
//    S_OK if the clone was created succesfully, otherwise some appropriate failing HRESULT
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
HRESULT CordbEnumerator<ElemType,
                        ElemPublicType,
                        EnumInterfaceType, IID_EnumInterfaceType,
                        GetPublicType>::Clone(ICorDebugEnum **ppEnum)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppEnum, ICorDebugEnum **);
    HRESULT hr = S_OK;
    EX_TRY
    {
        CordbEnumerator<ElemType, ElemPublicType, EnumInterfaceType, IID_EnumInterfaceType, GetPublicType>* clone =
            new CordbEnumerator<ElemType, ElemPublicType, EnumInterfaceType, IID_EnumInterfaceType, GetPublicType>(
                GetProcess(), m_items, m_countItems);
        clone->QueryInterface(__uuidof(ICorDebugEnum), (void**)ppEnum);
    }
    EX_CATCH_HRESULT(hr)
    {
    }
    return hr;
}

// ICorDebugEnum::GetCount
// Gets the number of items in the list that is being enumerated
//
//   Arguments:
//     pcelt - on return the number of items being enumerated
//
//   Return:
//     S_OK or failing HRESULTS for other error conditions
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
HRESULT CordbEnumerator<ElemType,
                        ElemPublicType,
                        EnumInterfaceType, IID_EnumInterfaceType,
                        GetPublicType>::GetCount(ULONG *pcelt)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pcelt, ULONG *);

    *pcelt = m_countItems;
    return S_OK;
}

// ICorDebugEnum::Reset
// Restarts the enumeration at the beginning of the list
//
//   Return:
//     S_OK or failing HRESULTS for other error conditions
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
HRESULT CordbEnumerator<ElemType,
                        ElemPublicType,
                        EnumInterfaceType, IID_EnumInterfaceType,
                        GetPublicType>::Reset()
{
    FAIL_IF_NEUTERED(this);

    m_nextIndex = 0;
    return S_OK;
}

// ICorDebugEnum::Skip
// Skips over celt items in the enumeration, if celt is greater than the number of remaining items then all
// remaining items are skipped.
//
//   Arguments:
//     celt - number of items to be skipped
//
//   Return:
//     S_OK or failing HRESULTS for other error conditions
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
HRESULT CordbEnumerator<ElemType,
                        ElemPublicType,
                        EnumInterfaceType, IID_EnumInterfaceType,
                        GetPublicType>::Skip(ULONG celt)
{
    FAIL_IF_NEUTERED(this);

    m_nextIndex += celt;
    if(m_nextIndex > m_countItems)
    {
        m_nextIndex = m_countItems;
    }
    return S_OK;
}

// EnumInterfaceType::Next
// Attempts to enumerate the next celt items by copying them in the items array. If fewer than celt
// items remain all remaining items are enumerated. In either case pceltFetched indicates the number
// of items actually fetched.
//
//   Arguments:
//     celt - the number of enumerated items requested
//     items - an array of size celt where the enumerated items will be copied
//     pceltFetched - on return, the actual number of items enumerated
//
//   Return:
//     S_OK if all items could be enumerated, S_FALSE if not all the requested items were enumerated,
//     failing HRESULTS for other error conditions
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
HRESULT CordbEnumerator<ElemType,
                        ElemPublicType,
                        EnumInterfaceType, IID_EnumInterfaceType,
                        GetPublicType>::Next(ULONG celt,
                                             ElemPublicType items[],
                                             ULONG *pceltFetched)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(items, ElemInterfaceType *,
        celt, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pceltFetched, ULONG *);

    if ((pceltFetched == NULL) && (celt != 1))
    {
        return E_INVALIDARG;
    }

    ULONG countFetched;
    for(countFetched = 0; countFetched < celt && m_nextIndex < m_countItems; countFetched++, m_nextIndex++)
    {
        items[countFetched] = GetPublicType(m_items[m_nextIndex]);
    }

    if(pceltFetched != NULL)
    {
        *pceltFetched = countFetched;
    }

    return countFetched == celt ? S_OK : S_FALSE;
}

// Neuter
// neuters the enumerator and deletes the contents (the contents are not explicitly neutered though)
template< typename ElemType,
          typename ElemPublicType,
          typename EnumInterfaceType, REFIID IID_EnumInterfaceType,
          ElemPublicType (*GetPublicType)(ElemType)>
VOID CordbEnumerator<ElemType,
                     ElemPublicType,
                     EnumInterfaceType, IID_EnumInterfaceType,
                     GetPublicType>::Neuter()
{
    delete [] m_items;
    m_items = NULL;
    m_countItems = 0;
    m_nextIndex = 0;
    CordbBase::Neuter();
}
