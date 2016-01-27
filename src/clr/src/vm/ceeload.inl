// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: CEELOAD.INL
// 

// 
// CEELOAD.INL has inline methods from CEELOAD.H.
// ===========================================================================

#ifndef CEELOAD_INL_
#define CEELOAD_INL_

template<typename TYPE>
inline
TYPE LookupMap<TYPE>::GetValueAt(PTR_TADDR pValue, TADDR* pFlags, TADDR supportedFlags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    TYPE value = RelativePointer<TYPE>::GetValueMaybeNullAtPtr(dac_cast<TADDR>(pValue));

    if (pFlags)
        *pFlags = dac_cast<TADDR>(value) & supportedFlags;

    return (TYPE)(dac_cast<TADDR>(value) & ~supportedFlags);
}

template<typename TYPE>
inline 
void LookupMap<TYPE>::SetValueAt(PTR_TADDR pValue, TYPE value, TADDR flags)
{
    WRAPPER_NO_CONTRACT;

    value = (TYPE)(dac_cast<TADDR>(value) | flags);

    RelativePointer<TYPE>::SetValueAtPtr(dac_cast<TADDR>(pValue), value);
}

#ifndef DACCESS_COMPILE
//
// Specialization of Get/SetValueAt methods to support maps of pointer-sized value types
//
template<>
inline
SIZE_T LookupMap<SIZE_T>::GetValueAt(PTR_TADDR pValue, TADDR* pFlags, TADDR supportedFlags)
{
    WRAPPER_NO_CONTRACT;

    TADDR value = *pValue;

    if (pFlags)
        *pFlags = value & supportedFlags;

    return (value & ~supportedFlags);
}

template<>
inline 
void LookupMap<SIZE_T>::SetValueAt(PTR_TADDR pValue, SIZE_T value, TADDR flags)
{
    WRAPPER_NO_CONTRACT;
    *pValue = value | flags;
}
#endif // DACCESS_COMPILE

//
// Specialization of GetValueAt methods for tables with cross-module references
//
template<> 
inline
PTR_TypeRef LookupMap<PTR_TypeRef>::GetValueAt(PTR_TADDR pValue, TADDR* pFlags, TADDR supportedFlags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // Strip flags before RelativeFixupPointer dereference
    TADDR value = *pValue;

    TADDR flags = (value & supportedFlags);
    value -= flags;
    value = ((RelativeFixupPointer<TADDR>&)(value)).GetValueMaybeNull(dac_cast<TADDR>(pValue));

    if (pFlags)
        *pFlags = flags;

    return dac_cast<PTR_TypeRef>(value);
}

template<> 
inline
PTR_Module LookupMap<PTR_Module>::GetValueAt(PTR_TADDR pValue, TADDR* pFlags, TADDR supportedFlags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // Strip flags before RelativeFixupPointer dereference
    TADDR value = *pValue;

    TADDR flags = (value & supportedFlags);
    value -= flags;
    value = ((RelativeFixupPointer<TADDR>&)(value)).GetValueMaybeNull(dac_cast<TADDR>(pValue));

    if (pFlags)
        *pFlags = flags;

    return dac_cast<PTR_Module>(value);
}

template<> 
inline
PTR_MemberRef LookupMap<PTR_MemberRef>::GetValueAt(PTR_TADDR pValue, TADDR* pFlags, TADDR supportedFlags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // Strip flags before RelativeFixupPointer dereference
    TADDR value = *pValue;

    TADDR flags = (value & supportedFlags);
    value -= flags;
    value = ((RelativeFixupPointer<TADDR>&)(value)).GetValueMaybeNull(dac_cast<TADDR>(pValue));

    if (pFlags)
        *pFlags = flags;

    return dac_cast<PTR_MemberRef>(value);

}

// Retrieve the value associated with a rid
template<typename TYPE>
TYPE LookupMap<TYPE>::GetElement(DWORD rid, TADDR* pFlags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

#ifdef FEATURE_PREJIT
    if (MapIsCompressed())
{
        // Can't access compressed entries directly: we need to go through the special helper. However we
        // must still check the hot cache first (this would normally be done by GetElementPtr() below, but
        // we can't integrate compressed support there since compressed entries don't have addresses, at
        // least not byte-aligned ones).
        PTR_TADDR pHotItemValue = FindHotItemValuePtr(rid);
        if (pHotItemValue)
            return GetValueAt(pHotItemValue, pFlags, supportedFlags);

        TADDR value = GetValueFromCompressedMap(rid);

        if (value == NULL)
        {
            if ((pNext == NULL) || (rid < dwCount))
            {
                if (pFlags)
                    *pFlags = NULL;
                return NULL;
            }

            return dac_cast<DPTR(LookupMap)>(pNext)->GetElement(rid - dwCount, pFlags);
        }

        if (pFlags)
            *pFlags = (value & supportedFlags);

        return (TYPE)(value & ~supportedFlags);
    }
#endif // FEATURE_PREJIT

    PTR_TADDR pElement = GetElementPtr(rid);
    return (pElement != NULL) ? GetValueAt(pElement, pFlags, supportedFlags) : NULL;
}

// Stores an association in a map that has been previously grown to
// the required size. Will never throw or fail.
template<typename TYPE>
void LookupMap<TYPE>::SetElement(DWORD rid, TYPE value, TADDR flags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    BOOL fSuccess;
    fSuccess = TrySetElement(rid, value, flags);
    _ASSERTE(fSuccess);
}


#ifndef DACCESS_COMPILE 

// Try to store an association in a map. Will never throw or fail.
template<typename TYPE>
BOOL LookupMap<TYPE>::TrySetElement(DWORD rid, TYPE value, TADDR flags)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PTR_TADDR pElement = GetElementPtr(rid);
    if (pElement == NULL)
        return FALSE;
#ifdef _DEBUG
    // Once set, the values in LookupMap should be immutable.
    TADDR oldFlags;
    TYPE oldValue = GetValueAt(pElement, &oldFlags, supportedFlags);
    _ASSERTE(oldValue == NULL || (oldValue == value && oldFlags == flags));
#endif
    // Avoid unnecessary writes - do not overwrite existing value
    if (*pElement == NULL)
    {
        if (!EnsureWritablePagesNoThrow(pElement, sizeof (TYPE)))
            return FALSE;
        SetValueAt(pElement, value, flags);
    }
    return TRUE;
}

// Stores an association in a map. Grows the map as necessary.
template<typename TYPE>
void LookupMap<TYPE>::AddElement(Module * pModule, DWORD rid, TYPE value, TADDR flags)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pModule));
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory(););
    }
    CONTRACTL_END;

    PTR_TADDR pElement = GetElementPtr(rid);
    if (pElement == NULL)
        pElement = GrowMap(pModule, rid);
#ifdef _DEBUG
    // Once set, the values in LookupMap should be immutable.
    TADDR oldFlags;
    TYPE oldValue = GetValueAt(pElement, &oldFlags, supportedFlags);
    _ASSERTE(oldValue == NULL || (oldValue == value && oldFlags == flags));
#endif
    // Avoid unnecessary writes - do not overwrite existing value
    if (*pElement == NULL)
    {
        EnsureWritablePages(pElement, sizeof (TYPE));
        SetValueAt(pElement, value, flags);
    }
}


// Ensures that the map has space for this element
template<typename TYPE>
inline 
void LookupMap<TYPE>::EnsureElementCanBeStored(Module * pModule, DWORD rid)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pModule));
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory(););
    }
    CONTRACTL_END;

    // don't attempt to call GetElementPtr for rids inside the compressed portion of
    // a multi-node map
    if (MapIsCompressed() && rid < dwCount)
        return;
    PTR_TADDR pElement = GetElementPtr(rid);
    if (pElement == NULL)
        GrowMap(pModule, rid);

    EnsureWritablePages(pElement, sizeof (TYPE));
}

#endif // DACCESS_COMPILE

// Find the given value in the table and return its RID
template<typename TYPE>
DWORD LookupMap<TYPE>::Find(TYPE value, TADDR* pFlags)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    Iterator it(this);

    DWORD rid = 0;
    while (it.Next())
    {
        TADDR flags;
        if (it.GetElementAndFlags(&flags) == value && (!pFlags || *pFlags == flags))
            return rid;
        rid++;
    }

    return 0;
}

template<typename TYPE>
inline
LookupMap<TYPE>::Iterator::Iterator(LookupMap* map)
{
    LIMITED_METHOD_DAC_CONTRACT;

    m_map = map;
    m_index = (DWORD) -1;
#ifdef FEATURE_PREJIT
    // Compressed map support
    m_currentEntry = 0;
    if (map->pTable != NULL)
        m_tableStream = BitStreamReader(dac_cast<PTR_CBYTE>(map->pTable));
#endif // FEATURE_PREJIT
}
        
template<typename TYPE>
inline BOOL
LookupMap<TYPE>::Iterator::Next()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (!m_map || !m_map->pTable)
    {
        return FALSE;
    }

    m_index++;
    if (m_index == m_map->dwCount)
    {
        m_map = dac_cast<DPTR(LookupMap)>(m_map->pNext);
        if (!m_map || !m_map->pTable)
        {
            return FALSE;
        }
        m_index = 0;
    }

#ifdef FEATURE_PREJIT
    // For a compressed map we need to read the encoded delta for the next entry and apply it to our previous
    // value to obtain the new current value.
    if (m_map->MapIsCompressed())
        m_currentEntry = m_map->GetNextCompressedEntry(&m_tableStream, m_currentEntry);
#endif // FEATURE_PREJIT

    return TRUE;
}

template<typename TYPE>
inline TYPE
LookupMap<TYPE>::Iterator::GetElement(TADDR* pFlags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_PREJIT
    // The current value for a compressed map is actually a map-based RVA. A zero RVA indicates a NULL pointer
    // but otherwise we can recover the full pointer by adding the address of the map we're iterating.
    // Note that most LookupMaps are embedded structures (in Module) so we can't directly dac_cast<TADDR> our
    // "this" pointer for DAC builds. Instead we have to use the slightly slower (in DAC) but more flexible
    // PTR_HOST_INT_TO_TADDR() which copes with interior host pointers.
    if (m_map->MapIsCompressed())
    {
        TADDR value = m_currentEntry ? PTR_HOST_INT_TO_TADDR(m_map) + m_currentEntry : 0;

        if (pFlags)
            *pFlags = (value & m_map->supportedFlags);

        return (TYPE)(value & ~m_map->supportedFlags);
    }
    else
#endif // FEATURE_PREJIT
        return GetValueAt(m_map->GetIndexPtr(m_index), pFlags, m_map->supportedFlags);
}

inline PTR_Assembly Module::GetAssembly() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    
    return m_pAssembly;
}

inline MethodDesc *Module::LookupMethodDef(mdMethodDef token)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END
    
    _ASSERTE(TypeFromToken(token) == mdtMethodDef);
    g_IBCLogger.LogRidMapAccess( MakePair( this, token ) );

    return m_MethodDefToDescMap.GetElement(RidFromToken(token));
}

inline MethodDesc *Module::LookupMemberRefAsMethod(mdMemberRef token)
{
    LIMITED_METHOD_DAC_CONTRACT;
    
    _ASSERTE(TypeFromToken(token) == mdtMemberRef);
    g_IBCLogger.LogRidMapAccess( MakePair( this, token ) );
    BOOL flags = FALSE;
    PTR_MemberRef pMemberRef = m_pMemberRefToDescHashTable->GetValue(token, &flags);
    return flags ? dac_cast<PTR_MethodDesc>(pMemberRef) : NULL;
}

inline Assembly *Module::LookupAssemblyRef(mdAssemblyRef token)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    
    _ASSERTE(TypeFromToken(token) == mdtAssemblyRef);
    PTR_Module module= m_ManifestModuleReferencesMap.GetElement(RidFromToken(token));
    return module?module->GetAssembly():NULL;
}

#ifndef DACCESS_COMPILE
inline void Module::ForceStoreAssemblyRef(mdAssemblyRef token, Assembly *value)
{
    WRAPPER_NO_CONTRACT; // THROWS/GC_NOTRIGGER/INJECT_FAULT()/MODE_ANY
    _ASSERTE(value->GetManifestModule());
    _ASSERTE(TypeFromToken(token) == mdtAssemblyRef);

    m_ManifestModuleReferencesMap.AddElement(this, RidFromToken(token), value->GetManifestModule());
}

inline void Module::StoreAssemblyRef(mdAssemblyRef token, Assembly *value)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(value->GetManifestModule());
    _ASSERTE(TypeFromToken(token) == mdtAssemblyRef);
    m_ManifestModuleReferencesMap.TrySetElement(RidFromToken(token), value->GetManifestModule());
}

inline mdAssemblyRef Module::FindAssemblyRef(Assembly *targetAssembly)
{
    WRAPPER_NO_CONTRACT;

    return m_ManifestModuleReferencesMap.Find(targetAssembly->GetManifestModule()) | mdtAssemblyRef;
}

#endif //DACCESS_COMPILE

inline BOOL Module::IsEditAndContinueCapable() 
{ 
    WRAPPER_NO_CONTRACT; 
    SUPPORTS_DAC;

    BOOL isEnCCapable = IsEditAndContinueCapable(m_pAssembly, m_file);
    
    // for now, Module::IsReflection is equivalent to m_file->IsDynamic,
    // which is checked by IsEditAndContinueCapable(m_pAssembly, m_file)
    _ASSERTE(!isEnCCapable || (!this->IsReflection() && !GetAssembly()->IsDomainNeutral()));

    return isEnCCapable;
}

FORCEINLINE PTR_DomainLocalModule Module::GetDomainLocalModule(AppDomain *pDomain)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (!Module::IsEncodedModuleIndex(GetModuleID()))
    {
        return m_ModuleID;
    }

#if !defined(DACCESS_COMPILE)
    if (pDomain == NULL)
    {
        pDomain = GetAppDomain();
    }
#endif // DACCESS_COMPILE

    // If the module is domain neutral, then you must supply an AppDomain argument.
    // Use GetDomainLocalModule() if you want to rely on the current AppDomain
    _ASSERTE(pDomain != NULL);

    return pDomain->GetDomainLocalBlock()->GetModuleSlot(GetModuleIndex());
}

FORCEINLINE ULONG Module::GetNumberOfActivations()
{
    _ASSERTE(m_Crst.OwnedByCurrentThread());
    return m_dwNumberOfActivations;
}

FORCEINLINE ULONG Module::IncrementNumberOfActivations()
{
    CrstHolder lock(&m_Crst);
    return ++m_dwNumberOfActivations;
}


#ifdef FEATURE_PREJIT

#include "nibblestream.h"

FORCEINLINE BOOL Module::FixupDelayList(TADDR pFixupList)
{
    WRAPPER_NO_CONTRACT;

    COUNT_T nImportSections;
    PTR_CORCOMPILE_IMPORT_SECTION pImportSections = GetImportSections(&nImportSections);

    return FixupDelayListAux(pFixupList, this, &Module::FixupNativeEntry, pImportSections, nImportSections, GetNativeOrReadyToRunImage());
}

template<typename Ptr, typename FixupNativeEntryCallback>
BOOL Module::FixupDelayListAux(TADDR pFixupList,
                               Ptr pThis, FixupNativeEntryCallback pfnCB,
                               PTR_CORCOMPILE_IMPORT_SECTION pImportSections, COUNT_T nImportSections,
                               PEDecoder * pNativeImage)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pFixupList != NULL);
   }
    CONTRACTL_END;

    // Fixup Encoding:
    // ==============
    //
    // The fixup list is sorted in tables. Within each table, the fixups are a
    // sorted list of INDEXes. The first INDEX in each table is encoded entirely, 
    // but the remaining INDEXes store only the delta increment from the previous INDEX.
    // The encoding/compression is done by ZapperModule::CompressFixupList().
    //
    //-------------------------------------------------------------------------
    // Here is the detailed description :
    //
    // The first entry stores the m_pFixupBlob table index.
    //
    // The next entry stores the INDEX into the particular table.
    // An "entry" can be one or more nibbles. 3 bits of a nibble are used 
    // to store the value, and the top bit indicates if  the following nibble 
    // contains rest of the value. If the top bit is not set, then this
    // nibble is the last part of the value.
    //
    // If the next entry is non-0, it is another (delta-encoded relative to the
    // previous INDEX) INDEX belonging  to the same table. If the next entry is 0, 
    // it indicates that all INDEXes in this table are done.
    //
    // When the fixups for the previous table is done, there is entry to
    // indicate the next table (delta-encoded relative to the previous table).
    // If the entry is 0, then it is the end of the entire fixup list.
    //
    //-------------------------------------------------------------------------
    // This is what the fixup list looks like:
    //
    // CorCompileTokenTable index
    //     absolute INDEX
    //     INDEX delta
    //     ...
    //     INDEX delta
    //     0
    // CorCompileTokenTable index delta
    //     absolute INDEX
    //     INDEX delta
    //     ...
    //     INDEX delta
    //     0
    // CorCompileTokenTable index delta
    //     absolute INDEX
    //     INDEX delta
    //     ...
    //     INDEX delta
    //     0
    // 0
    //
    //

    NibbleReader reader(PTR_BYTE(pFixupList), (SIZE_T)-1);

    //
    // The fixups are sorted by the sections they point to.
    // Walk the set of fixups in every sections
    //

    DWORD curTableIndex = reader.ReadEncodedU32();

    while (TRUE)
    {
        // Get the correct section to work with. This is stored in the first two nibbles (first byte)

        _ASSERTE(curTableIndex < nImportSections);
        PTR_CORCOMPILE_IMPORT_SECTION pImportSection = pImportSections + curTableIndex;

        COUNT_T cbData;
        TADDR pData = pNativeImage->GetDirectoryData(&pImportSection->Section, &cbData);

        // Now iterate thru the fixup entries
        SIZE_T fixupIndex = reader.ReadEncodedU32(); // Accumulate the real rva from the delta encoded rva

        while (TRUE)
        {
            CONSISTENCY_CHECK(fixupIndex * sizeof(TADDR) < cbData);

            if (!(pThis->*pfnCB)(pImportSection, fixupIndex, dac_cast<PTR_SIZE_T>(pData + fixupIndex * sizeof(TADDR))))
                return FALSE;

            int delta = reader.ReadEncodedU32();

            // Delta of 0 means end of entries in this table
            if (delta == 0)
                break;

            fixupIndex += delta;
        }

        unsigned tableIndex = reader.ReadEncodedU32();

        if (tableIndex == 0)
            break;

        curTableIndex = curTableIndex + tableIndex;

    } // Done with all entries in this table

    return TRUE;
}

#endif //FEATURE_PREJIT

inline PTR_LoaderAllocator Module::GetLoaderAllocator()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetAssembly()->GetLoaderAllocator();
}

inline MethodTable* Module::GetDynamicClassMT(DWORD dynamicClassID)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_cDynamicEntries > dynamicClassID);
    return m_pDynamicStaticsInfo[dynamicClassID].pEnclosingMT;
}

inline ReJitManager * Module::GetReJitManager()
{
    LIMITED_METHOD_CONTRACT;
    return GetDomain()->GetReJitManager();
}

#endif  // CEELOAD_INL_
