// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*============================================================
**
** Header:  Map associated with a ComMethodTable that contains
**          information on its members.
===========================================================*/

#include "common.h"

#include "commtmemberinfomap.h"
#include "comcallablewrapper.h"
#include "field.h"
#include "caparser.h"

#define BASE_OLEAUT_DISPID 0x60020000

static LPCWSTR szDefaultValue           = W("Value");
static LPCWSTR szGetEnumerator          = W("GetEnumerator");

// ============================================================================
// This structure and class definition are used to implement the hash table
// used to make sure that there are no duplicate class names.
// ============================================================================
struct WSTRHASH : HASHLINK
{
    LPCWSTR     szName;         // Ptr to hashed string.
};

class CWStrHash : public CChainedHash<WSTRHASH>
{
public:
    virtual bool InUse(WSTRHASH *pItem)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pItem));
        }
        CONTRACTL_END;

        return (pItem->szName != NULL);
    }

    virtual void SetFree(WSTRHASH *pItem)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pItem));
        }
        CONTRACTL_END;

        pItem->szName = NULL;
    }

    virtual ULONG Hash(const void *pData)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pData));
        }
        CONTRACTL_END;

        // Do case-insensitive hash
        return (HashiString(reinterpret_cast<LPCWSTR>(pData)));
    }

    virtual int Cmp(const void *pData, void *pItem)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pItem));
            PRECONDITION(CheckPointer(pData));
        }
        CONTRACTL_END;

        return SString::_wcsicmp(reinterpret_cast<LPCWSTR>(pData),reinterpret_cast<WSTRHASH*>(pItem)->szName);
    }
}; // class CWStrHash : public CChainedHash<WSTRHASH>


// ============================================================================
// Token and module pair hashtable.
// ============================================================================
EEHashEntry_t * EEModuleTokenHashTableHelper::AllocateEntry(EEModuleTokenPair *pKey, BOOL bDeepCopy, void *pHeap)
{
    CONTRACT (EEHashEntry_t*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(CONTRACT_RETURN NULL);
        PRECONDITION(CheckPointer(pKey));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    _ASSERTE(!bDeepCopy && "Deep copy is not supported by the EEModuleTokenHashTableHelper");

    EEHashEntry_t *pEntry = (EEHashEntry_t *) new (nothrow) BYTE[SIZEOF_EEHASH_ENTRY + sizeof(EEModuleTokenPair)];
    if (!pEntry)
        RETURN NULL;

    EEModuleTokenPair *pEntryKey = (EEModuleTokenPair *) pEntry->Key;
    pEntryKey->m_tk = pKey->m_tk;
    pEntryKey->m_pModule = pKey->m_pModule;

    RETURN pEntry;
} // EEHashEntry_t * EEModuleTokenHashTableHelper::AllocateEntry()


void EEModuleTokenHashTableHelper::DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap Heap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pEntry));
    }
    CONTRACTL_END;

    delete [] (BYTE*)pEntry;
} // void EEModuleTokenHashTableHelper::DeleteEntry()


BOOL EEModuleTokenHashTableHelper::CompareKeys(EEHashEntry_t *pEntry, EEModuleTokenPair *pKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pEntry));
        PRECONDITION(CheckPointer(pKey));
    }
    CONTRACTL_END;

    EEModuleTokenPair *pEntryKey = (EEModuleTokenPair*) pEntry->Key;

    // Compare the token.
    if (pEntryKey->m_tk != pKey->m_tk)
        return FALSE;

    // Compare the module.
    if (pEntryKey->m_pModule != pKey->m_pModule)
        return FALSE;

    return TRUE;
} // BOOL EEModuleTokenHashTableHelper::CompareKeys()


DWORD EEModuleTokenHashTableHelper::Hash(EEModuleTokenPair *pKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pKey));
    }
    CONTRACTL_END;

    size_t val = (size_t) ((DWORD_PTR)pKey->m_tk + (DWORD_PTR)pKey->m_pModule);
#ifdef TARGET_X86
    return (DWORD)val;
#else
    // @TODO IA64: Is this a good hashing mechanism on IA64?
    return (DWORD)(val >> 3);
#endif
} // DWORD EEModuleTokenHashTableHelper::Hash()


EEModuleTokenPair *EEModuleTokenHashTableHelper::GetKey(EEHashEntry_t *pEntry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pEntry));
    }
    CONTRACTL_END;

    return (EEModuleTokenPair*)pEntry->Key;
} // EEModuleTokenPair *EEModuleTokenHashTableHelper::GetKey()


// ============================================================================
// ComMethodTable member info map.
// ============================================================================
void ComMTMemberInfoMap::Init(size_t sizeOfPtr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;
    BYTE const  *pData;                 // Pointer to a custom attribute blob.
    ULONG       cbData;                 // Size of a custom attribute blob.

    m_bHadDuplicateDispIds = FALSE;

    // See if there is a default property.
    m_DefaultProp[0] = 0; // init to 'none'.
    hr = m_pMT->GetCustomAttribute(
        WellKnownAttribute::DefaultMember, reinterpret_cast<const void**>(&pData), &cbData);

    if (hr == S_OK && cbData > 5 && pData[0] == 1 && pData[1] == 0)
    {
        CustomAttributeParser cap(pData, cbData);

        // Already verified prolog before entering block.
        // Technically, we should have done that but I'm
        // leaving it to avoid causing a breaking change.
        VERIFY(SUCCEEDED(cap.ValidateProlog()));

        LPCUTF8 szString;
        ULONG   cbString;
        if (SUCCEEDED(cap.GetNonNullString(&szString, &cbString)))
        {
            // Copy the data, then null terminate (CA blob's string may not be).
            m_DefaultProp.ReSizeThrows(cbString+1);
            memcpy(m_DefaultProp.Ptr(), szString, cbString);
            m_DefaultProp[cbString] = 0;
        }
    }

    // Set up the properties for the type.
    if (m_pMT->IsInterface())
        SetupPropsForInterface(sizeOfPtr);
    else
        SetupPropsForIClassX(sizeOfPtr);

    // Initiliaze the hashtable.
    m_TokenToComMTMethodPropsMap.Init((DWORD)m_MethodProps.Size(), NULL, NULL);

    // Populate the hashtable that maps from token to member info.
    PopulateMemberHashtable();
} // HRESULT ComMTMemberInfoMap::Init()


ComMTMethodProps *ComMTMemberInfoMap::GetMethodProps(mdToken tk, Module *pModule)
{
    CONTRACT (ComMTMethodProps*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pModule));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    EEModuleTokenPair TokenModulePair(tk, pModule);
    HashDatum Data;

    if (m_TokenToComMTMethodPropsMap.GetValue(&TokenModulePair, &Data))
        RETURN (ComMTMethodProps *)Data;

    RETURN NULL;
} // ComMTMethodProps *ComMTMemberInfoMap::GetMethodProps()


void ComMTMemberInfoMap::SetupPropsForIClassX(size_t sizeOfPtr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ComMethodTable *pCMT;               // ComMethodTable for the Class Vtable.
    MethodDesc  *pMeth;                 // A method descriptor.
    ComCallMethodDesc *pFieldMeth;      // A method descriptor for a field.
    FieldDesc   *pField;                // Actual FieldDesc for field.
    DWORD       nSlots;                 // Number of vtable slots.
    UINT        i;                      // Loop control.
    LPCUTF8     pszName;                // A name in UTF8.
    CQuickArray<WCHAR> rName;           // A name.
    ULONG       dispid;                 // A dispid.
    SHORT       oVftBase;                 // Offset in vtable, if not system defined.
    int                 cVisibleMembers = 0;    // The count of methods that are visible to COM.
    HRESULT             hr              = S_OK; // A result.
    DWORD               dwTIFlags       = 0;    // TypeLib flags.

    // Get the vtable for the class.
    pCMT = ComCallWrapperTemplate::SetupComMethodTableForClass(m_pMT, TRUE);
    nSlots = pCMT->GetSlots();

    // IDispatch derived.
    oVftBase = 7 * (SHORT)sizeOfPtr;

    // Build array of descriptive information.
    m_MethodProps.ReSizeThrows(nSlots);
    for (i=0; i<nSlots; ++i)
    {
        if (pCMT->IsSlotAField(i))
        {
            // Fields better come in pairs.
            _ASSERTE(i < nSlots-1);

            pFieldMeth = pCMT->GetFieldCallMethodDescForSlot(i);
            pField = pFieldMeth->GetFieldDesc();

            DWORD dwFlags;
            IfFailThrow(pField->GetMDImport()->GetFieldDefProps(pField->GetMemberDef(), &dwFlags));
            BOOL bReadOnly = IsFdInitOnly(dwFlags) || IsFdLiteral(dwFlags);
            BOOL bFieldVisibleFromCom = IsMemberVisibleFromCom(pField->GetApproxEnclosingMethodTable(), pField->GetMemberDef(), mdTokenNil);

            // Get the assigned dispid, or DISPID_UNKNOWN.
            hr = pField->GetMDImport()->GetDispIdOfMemberDef(pField->GetMemberDef(), &dispid);

            IfFailThrow(pField->GetMDImport()->GetNameOfFieldDef(pField->GetMemberDef(), &pszName));
            IfFailThrow(Utf2Quick(pszName, rName));
            ULONG cchpName = ((int)wcslen(rName.Ptr())) + 1;
            m_MethodProps[i].pName = reinterpret_cast<WCHAR*>(m_sNames.Alloc(cchpName * sizeof(WCHAR)));

            m_MethodProps[i].pMeth = (MethodDesc*)pFieldMeth;
            // It's safe to do the following case because that FieldSemanticOffset is 100, msSetter = 1, msGetter = 2
            m_MethodProps[i].semantic = static_cast<USHORT>(FieldSemanticOffset + (pFieldMeth->IsFieldGetter() ? msGetter : msSetter));
            m_MethodProps[i].property = mdPropertyNil;
            wcscpy_s(m_MethodProps[i].pName, cchpName, rName.Ptr());
            m_MethodProps[i].dispid = dispid;
            m_MethodProps[i].oVft = 0;
            m_MethodProps[i].bMemberVisible = bFieldVisibleFromCom && (!bReadOnly || pFieldMeth->IsFieldGetter());
            m_MethodProps[i].bFunction2Getter = FALSE;

            ++i;
            pFieldMeth = pCMT->GetFieldCallMethodDescForSlot(i);
            m_MethodProps[i].pMeth = (MethodDesc*)pFieldMeth;
            // It's safe to do the following case because that FieldSemanticOffset is 100, msSetter = 1, msGetter = 2
            m_MethodProps[i].semantic = static_cast<USHORT>(FieldSemanticOffset + (pFieldMeth->IsFieldGetter() ? msGetter : msSetter));
            m_MethodProps[i].property = i - 1;
            m_MethodProps[i].dispid = dispid;
            m_MethodProps[i].oVft = 0;
            m_MethodProps[i].bMemberVisible = bFieldVisibleFromCom && (!bReadOnly || pFieldMeth->IsFieldGetter());
            m_MethodProps[i].bFunction2Getter = FALSE;
        }
        else
        {
            // Retrieve the method desc on the current class. This involves looking up the method
            // desc in the vtable if it is a virtual method.
            pMeth = pCMT->GetMethodDescForSlot(i);
            if (pMeth->IsVirtual())
            {
                WORD wSlot = InteropMethodTableData::GetSlotForMethodDesc(m_pMT, pMeth);
                _ASSERTE(wSlot != MethodTable::NO_SLOT);
                pMeth = m_pMT->GetComInteropData()->pVTable[wSlot].pMD;
            }
            m_MethodProps[i].pMeth = pMeth;

            // Retrieve the properties of the method.
            GetMethodPropsForMeth(pMeth, i, m_MethodProps, m_sNames);

            // Turn off dispids that look system-assigned.
            if (m_MethodProps[i].dispid >= 0x40000000 && m_MethodProps[i].dispid <= 0x7fffffff)
                m_MethodProps[i].dispid = DISPID_UNKNOWN;
        }
    }

    // COM+ supports properties in which the getter and setter have different signatures,
    //  but TypeLibs do not.  Look for mismatched signatures, and break apart the properties.
    for (i=0; i<nSlots; ++i)
    {
        // Is it a property, but not a field?  Fields only have one signature, so they are always OK.
        if (TypeFromToken(m_MethodProps[i].property) != mdtProperty &&
            m_MethodProps[i].semantic < FieldSemanticOffset)
        {
            // Get the indices of the getter and setter.
            size_t ixSet, ixGet;

            if (m_MethodProps[i].semantic == msGetter)
            {
                ixGet = i, ixSet = m_MethodProps[i].property;
            }
            else
            {
                _ASSERTE(m_MethodProps[i].semantic == msSetter);
                ixSet = i, ixGet = m_MethodProps[i].property;
            }

            // Get the signatures.
            PCCOR_SIGNATURE pbGet, pbSet;
            ULONG           cbGet, cbSet;
            pMeth = pCMT->GetMethodDescForSlot((unsigned)ixSet);
            pMeth->GetSig(&pbSet, &cbSet);

            pMeth = pCMT->GetMethodDescForSlot((unsigned)ixGet);
            pMeth->GetSig(&pbGet, &cbGet);

            // Now reuse ixGet, ixSet to index through signature.
            ixGet = ixSet = 0;

            // Eat calling conventions.
            ULONG callconv;
            ixGet += CorSigUncompressData(&pbGet[ixGet], &callconv);
            _ASSERTE((callconv & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_FIELD);
            ixSet += CorSigUncompressData(&pbSet[ixSet], &callconv);
            _ASSERTE((callconv & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_FIELD);

            // Argument count.
            ULONG acGet, acSet;
            ixGet += CorSigUncompressData(&pbGet[ixGet], &acGet);
            ixSet += CorSigUncompressData(&pbSet[ixSet], &acSet);

            // Setter must take exactly on more parameter.
            if (acSet != acGet+1)
                goto UnLink;

            // All matched, so on to next.
            continue;


            // Unlink the properties, and turn them into ordinary functions.
UnLink:
            // Get the indices of the getter and setter (again).
            if (m_MethodProps[i].semantic == msGetter)
                ixGet = i, ixSet = m_MethodProps[i].property;
            else
                ixSet = i, ixGet = m_MethodProps[i].property;

            // Eliminate the semantics.
            m_MethodProps[ixGet].semantic = 0;
            m_MethodProps[ixSet].semantic = 0;

            // Decorate the names.
            // These are the names of properties when properties don't have signatures
            // that match, and the "get" and "set" below don't have to match the CLS
            // property names.  This is an obscure corner case.
            m_MethodProps[i].pName = m_MethodProps[m_MethodProps[i].property].pName;
            WCHAR *pNewName;
            //string length + "get" + null terminator.
            //XXX Fri 11/19/2004 Why is this + 4 rather than +3?
            ULONG cchpNewName = ((int)wcslen(m_MethodProps[ixGet].pName)) + 4 + 1;
            pNewName = reinterpret_cast<WCHAR*>(m_sNames.Alloc(cchpNewName * sizeof(WCHAR)));
            wcscpy_s(pNewName, cchpNewName, W("get"));
            wcscat_s(pNewName, cchpNewName, m_MethodProps[ixGet].pName);
            m_MethodProps[ixGet].pName = pNewName;
            pNewName = reinterpret_cast<WCHAR*>(m_sNames.Alloc((int)((4+wcslen(m_MethodProps[ixSet].pName))*sizeof(WCHAR)+2)));
            wcscpy_s(pNewName, cchpNewName, W("set"));
            wcscat_s(pNewName, cchpNewName, m_MethodProps[ixSet].pName);
            m_MethodProps[ixSet].pName = pNewName;

            // If the methods share a dispid, kill them both.
            if (m_MethodProps[ixGet].dispid == m_MethodProps[ixSet].dispid)
                m_MethodProps[ixGet].dispid = m_MethodProps[ixSet].dispid = DISPID_UNKNOWN;

            // Unlink from each other.
            m_MethodProps[i].property = mdPropertyNil;

        }
    }

    // Assign vtable offsets.
    for (i = 0; i < nSlots; ++i)
    {
        SHORT oVft = oVftBase + static_cast<SHORT>(i * sizeOfPtr);
        m_MethodProps[i].oVft = oVft;
    }

    // Resolve duplicate dispids.
    EliminateDuplicateDispIds(m_MethodProps, nSlots);

    // Pick something for the "Value".
    AssignDefaultMember(m_MethodProps, m_sNames, nSlots);

    // Check to see if there is something to assign DISPID_NEWENUM to.
    AssignNewEnumMember(m_MethodProps, m_sNames, nSlots);

    // Resolve duplicate names.
    EliminateDuplicateNames(m_MethodProps, m_sNames, nSlots);

    // Do some PROPERTYPUT/PROPERTYPUTREF translation.
    FixupPropertyAccessors(m_MethodProps, m_sNames, nSlots);

    // Fix up all properties so that they point to their shared name.
    for (i=0; i<nSlots; ++i)
    {
        if (TypeFromToken(m_MethodProps[i].property) != mdtProperty)
        {
            m_MethodProps[i].pName = m_MethodProps[m_MethodProps[i].property].pName;
            m_MethodProps[i].dispid = m_MethodProps[m_MethodProps[i].property].dispid;
        }
    }

    // Assign default dispids.
    AssignDefaultDispIds();
} // void ComMTMemberInfoMap::SetupPropsForIClassX()


void ComMTMemberInfoMap::SetupPropsForInterface(size_t sizeOfPtr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ULONG       iMD;                      // Loop control.
    SHORT       oVftBase;                 // Offset in vtable, if not system defined.
    CorIfaceAttr ifaceType;               // Is this interface [dual]?
    MethodDesc  *pMeth;                   // A MethodDesc.
    CQuickArray<int> rSlotMap;            // Array to map vtable slots.
    DWORD               nSlots;                                 // Number of vtable slots.
    ULONG               ulComSlotMin    = UINT32_MAX;           // Find first COM+ slot.
    ULONG               ulComSlotMax    = 0;                    // Find last COM+ slot.
    int                 bSlotRemap      = false;                // True if slots need to be mapped, due to holes.
    HRESULT             hr              = S_OK;

    // Retrieve the number of vtable slots the interface has.
    nSlots = m_pMT->GetNumVirtuals();

    // IDispatch, IUnknown derived?
    ifaceType = (m_pMT->IsInterface() ? m_pMT->GetComInterfaceType() : ifDual);
    oVftBase = ComMethodTable::GetNumExtraSlots(ifaceType) * (SHORT)sizeOfPtr;

    // Find lowest slot number.
    for (iMD=0; iMD < nSlots; ++iMD)
    {
        MethodDesc* pMD = m_pMT->GetMethodDescForSlot(iMD);
        _ASSERTE(pMD != NULL);
        ULONG tmp = pMD->GetComSlot();

        if (tmp < ulComSlotMin)
            ulComSlotMin = tmp;
        if (tmp > ulComSlotMax)
            ulComSlotMax = tmp;
    }

    // Used a couple of times.
    MethodTable::MethodIterator it(m_pMT);

    if (ulComSlotMax-ulComSlotMin >= nSlots)
    {
        bSlotRemap = true;

        // Resize the array.
        rSlotMap.ReSizeThrows(ulComSlotMax+1);

        // Init to "slot not used" value of -1.
        memset(rSlotMap.Ptr(), -1, rSlotMap.Size()*sizeof(int));

        // See which vtable slots are used.
        it.MoveToBegin();
        for (; it.IsValid(); it.Next())
        {
            if (it.IsVirtual())
            {
                MethodDesc* pMD = it.GetMethodDesc();
                _ASSERTE(pMD != NULL);
                ULONG tmp = pMD->GetComSlot();
                rSlotMap[tmp] = 0;
            }
        }

        // Assign incrementing table indices to the slots.
        ULONG ix=0;
        for (iMD=0; iMD<=ulComSlotMax; ++iMD)
            if (rSlotMap[iMD] != -1)
                rSlotMap[iMD] = ix++;
    }

    // Iterate over the members in the interface and build the list of methods.
    m_MethodProps.ReSizeThrows(nSlots);
    it.MoveToBegin();
    for (; it.IsValid(); it.Next())
    {
        if (it.IsVirtual())
        {
            pMeth = it.GetMethodDesc();
            if (pMeth != NULL)
            {
                ULONG ixSlot = pMeth->GetComSlot();
                if (bSlotRemap)
                    ixSlot = rSlotMap[ixSlot];
                else
                    ixSlot -= ulComSlotMin;

                m_MethodProps[ixSlot].pMeth = pMeth;
            }
        }
    }

    // Now have a list of methods in vtable order.  Go through and build names, semantic.
    for (iMD=0; iMD < nSlots; ++iMD)
    {
        pMeth = m_MethodProps[iMD].pMeth;
        GetMethodPropsForMeth(pMeth, iMD, m_MethodProps, m_sNames);
    }

    // Assign vtable offsets.
    for (iMD=0; iMD < nSlots; ++iMD)
    {
        SHORT oVft = oVftBase + static_cast<SHORT>((m_MethodProps[iMD].pMeth->GetComSlot() -ulComSlotMin) * sizeOfPtr);
        m_MethodProps[iMD].oVft = oVft;
    }

    // Resolve duplicate dispids.
    EliminateDuplicateDispIds(m_MethodProps, nSlots);

    // Pick something for the "Value".
    AssignDefaultMember(m_MethodProps, m_sNames, nSlots);

    // Check to see if there is something to assign DISPID_NEWENUM to.
    AssignNewEnumMember(m_MethodProps, m_sNames, nSlots);

    // Take care of name collisions due to overloading, inheritance.
    EliminateDuplicateNames(m_MethodProps, m_sNames, nSlots);

    // Do some PROPERTYPUT/PROPERTYPUTREF translation.
    FixupPropertyAccessors(m_MethodProps, m_sNames, nSlots);

    // Fix up all properties so that they point to their shared name.
    for (iMD=0; iMD < m_pMT->GetNumVirtuals(); ++iMD)
    {
        if (TypeFromToken(m_MethodProps[iMD].property) != mdtProperty)
        {
            m_MethodProps[iMD].pName = m_MethodProps[m_MethodProps[iMD].property].pName;
            m_MethodProps[iMD].dispid = m_MethodProps[m_MethodProps[iMD].property].dispid;
        }
    }

    // If the interface is IDispatch based, then assign the default dispids.
    if (IsDispatchBasedItf(ifaceType))
        AssignDefaultDispIds();
} // void ComMTMemberInfoMap::SetupPropsForInterface()


// ============================================================================
// Given a MethodDesc*, get the name of the method or property that the
//  method is a getter/setter for, plus the semantic for getter/setter.
//  In the case of properties, look for a previous getter/setter for this
//  property, and if found, link them, so that only one name participates in
//  name decoration.
// ============================================================================
void ComMTMemberInfoMap::GetMethodPropsForMeth(
    MethodDesc  *pMeth,                 // MethodDesc * for method.
    int         ix,                     // Slot.
    CQuickArray<ComMTMethodProps> &rProps,   // Array of method property information.
    CDescPool   &sNames)                // Pool of possibly decorated names.
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMeth));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;                     // A result.
    LPCUTF8     pszName;                // Name in UTF8.
    CQuickArray<WCHAR> rName;           // Buffer for unicode conversion.
    LPCWSTR     pName;                  // Pointer to a name, after possible substitution.
    mdProperty  pd;                     // Property token.
    LPCUTF8     pPropName;              // Pointer to propterty name.
    ULONG       uSemantic;              // Property semantic.
    ULONG       dispid;                 // A property dispid.

    // Get any user-assigned dispid.
    rProps[ix].dispid = pMeth->GetComDispid();

    // Assume system-defined vtable offsets.
    rProps[ix].oVft = 0;

    // Generally don't munge function into a getter.
    rProps[ix].bFunction2Getter = FALSE;

    // See if there is property information for this member.
    hr = pMeth->GetModule()->GetPropertyInfoForMethodDef(pMeth->GetMemberDef(), &pd, &pPropName, &uSemantic);
    IfFailThrow(hr);

    if (hr == S_OK)
    {
        // There is property information.
        // See if there a method is already associated with this property.
        rProps[ix].property = pd;
        int i;
        for (i=ix-1; i>=0; --i)
        {
            // Same property in same scope?
            if (rProps[i].property == pd &&
                rProps[i].pMeth->GetMDImport() == pMeth->GetMDImport())
            {
                rProps[ix].property = i;
                break;
            }
        }

        // If there wasn't another method for this property, save the name on
        //  this method, for duplicate elimination.
        if (i < 0)
        {
            // Save the name.  Have to convert from UTF8.
            int iLen = WszMultiByteToWideChar(CP_UTF8, 0, pPropName, -1, 0, 0);
            rProps[ix].pName = reinterpret_cast<WCHAR*>(sNames.Alloc(iLen*sizeof(WCHAR)));
            if (rProps[ix].pName == NULL)
            {
                ThrowHR(E_OUTOFMEMORY);
            }
            WszMultiByteToWideChar(CP_UTF8, 0, pPropName, -1, rProps[ix].pName, iLen);

            // Check whether the property has a dispid attribute.
            hr = pMeth->GetMDImport()->GetDispIdOfMemberDef(pd, &dispid);
            if (dispid != DISPID_UNKNOWN)
                rProps[ix].dispid = dispid;

            // If this is the default property, and the method or property doesn't have a dispid already,
            //  use DISPID_DEFAULT.
            if (rProps[ix].dispid == DISPID_UNKNOWN)
            {
                if (strcmp(pPropName, m_DefaultProp.Ptr()) == 0)
                {
                    rProps[ix].dispid = DISPID_VALUE;

                    // Don't want to try to set multiple as default property.
                    m_DefaultProp[0] = 0;
                }
            }
        }

        // Save the semantic.
        rProps[ix].semantic = static_cast<USHORT>(uSemantic);

        // Determine if the property is visible from COM.
        rProps[ix].bMemberVisible = IsMethodVisibleFromCom(pMeth) ? 1 : 0;
    }
    else
    {
        // Not a property, just an ordinary method.
        rProps[ix].property = mdPropertyNil;
        rProps[ix].semantic = 0;

        // Get the name.
        pszName = pMeth->GetName();
        if (pszName == NULL)
            ThrowHR(E_FAIL);

        if (stricmpUTF8(pszName, szInitName) == 0)
        {
            pName = szInitNameUse;
        }
        else
        {
            IfFailThrow(Utf2Quick(pszName, rName));
            pName = rName.Ptr();

            // If this is a "ToString" method, make it a property get.
            if (SString::_wcsicmp(pName, szDefaultToString) == 0)
            {
                rProps[ix].semantic = msGetter;
                rProps[ix].bFunction2Getter = TRUE;
            }
        }

        ULONG len = ((int)wcslen(pName)) + 1;
        rProps[ix].pName = reinterpret_cast<WCHAR*>(sNames.Alloc(len * sizeof(WCHAR)));
        if (rProps[ix].pName == NULL)
        {
            ThrowHR(E_OUTOFMEMORY);
        }
        wcscpy_s(rProps[ix].pName, len, pName);

        // Determine if the method is visible from COM.
        rProps[ix].bMemberVisible = !pMeth->IsArray() && IsMethodVisibleFromCom(pMeth);
    }
} // void ComMTMemberInfoMap::GetMethodPropsForMeth()




// ============================================================================
// Process the function names for an interface, checking for duplicates.  If
//  any duplicates are found, decorate the names with "_n".
//
//  NOTE:   Two implementations are provided, one using nested for-loops and a
//          second which implements a hashtable.  The first is faster when
//          the number of elements is less than 20, otherwise the hashtable
//          is the way to go.  The code-size of the first implementation is 574
//          bytes.  The hashtable code is 1120 bytes.
// ============================================================================
void ComMTMemberInfoMap::EliminateDuplicateNames(
    CQuickArray<ComMTMethodProps> &rProps,   // Array of method property information.
    CDescPool   &sNames,                // Pool of possibly decorated names.
    UINT        nSlots)                 // Count of entries
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CQuickBytes qb;
    UINT        iCur;
    CorIfaceAttr ifaceType;             // VTBL, Dispinterface, IDispatch
    ULONG       cBaseNames;             // Count of names in base interface.
    BOOL        bDup;                   // Is the name a duplicate?
    HRESULT     hr          = S_OK;
    const size_t cchrcName  = MAX_CLASSNAME_LENGTH;
    LPWSTR      rcName      = (LPWSTR)qb.AllocThrows(cchrcName * sizeof(WCHAR));

    // Tables of names of methods on IUnknown, IDispatch.
    static const LPCWSTR rBaseNames_Dispatch[] =
    {
        W("QueryInterface"),
        W("AddRef"),
        W("Release"),
        W("GetTypeInfoCount"),
        W("GetTypeInfo"),
        W("GetIDsOfNames"),
        W("Invoke")
    };

    // Determine which names are in the base interface.
    ifaceType = (m_pMT->IsInterface() ? m_pMT->GetComInterfaceType() : ifDual);
    const LPCWSTR * rBaseNames = (rBaseNames_Dispatch);

    // Is it pure dispinterface?
    if (ifaceType == ifDispatch)
    {
        cBaseNames = 0;
    }
    else
    {
        // Or is it IUnknown, IDispatch, IInspectable derived
        cBaseNames = ComMethodTable::GetNumExtraSlots(ifaceType);
    }

    // we're wasting time if there aren't at least two items!
    if (nSlots < 2 && cBaseNames == 0)
        return;

    else if (nSlots < 20)
    {
        // Eliminate duplicates.
        for (iCur=0; iCur<nSlots; ++iCur)
        {
            UINT iTst, iSuffix, iTry;

            // If a property with an associated (lower indexed) property, don't need to examine it.
            if (TypeFromToken(rProps[iCur].property) != mdtProperty)
                continue;

            // If the member is not visible to COM then we don't need to examine it.
            if (!rProps[iCur].bMemberVisible)
                continue;

            // Check for duplicate with already accepted member names.
            bDup = FALSE;
            for (iTst=0; !bDup && iTst<iCur; ++iTst)
            {
                // If a property with an associated (lower indexed) property, don't need to examine it.
                if (TypeFromToken(rProps[iTst].property) != mdtProperty)
                    continue;

                // If the member is not visible to COM then we don't need to examine it.
                if (!rProps[iTst].bMemberVisible)
                    continue;

                if (SString::_wcsicmp(rProps[iCur].pName, rProps[iTst].pName) == 0)
                    bDup = TRUE;
            }

            // If OK with other members, check with base interface names.
            for (iTst=0; !bDup && iTst<cBaseNames; ++iTst)
            {
                if (SString::_wcsicmp(rProps[iCur].pName, rBaseNames[iTst]) == 0)
                    bDup = TRUE;
            }

            // If the name is a duplicate, decorate it.
            if (bDup)
            {
                // Duplicate.
                DWORD cchName = (DWORD) wcslen(rProps[iCur].pName);
                if (cchName > MAX_CLASSNAME_LENGTH-cchDuplicateDecoration)
                    cchName = MAX_CLASSNAME_LENGTH-cchDuplicateDecoration;

                wcsncpy_s(rcName, cchrcName, rProps[iCur].pName, cchName);
                LPWSTR pSuffix = rcName + cchName;

                for (iSuffix=2; ; ++iSuffix)
                {
                    // Form a new name.
                    _snwprintf_s(pSuffix, cchDuplicateDecoration, _TRUNCATE, szDuplicateDecoration, iSuffix);

                    // Compare against ALL names.
                    for (iTry=0; iTry<nSlots; ++iTry)
                    {
                        // If a property with an associated (lower indexed) property,
                        // or iTry is the same as iCur, don't need to examine it.
                        if (TypeFromToken(rProps[iTry].property) != mdtProperty || iTry == iCur)
                            continue;
                        if (SString::_wcsicmp(rProps[iTry].pName, rcName) == 0)
                            break;
                    }

                    // Did we make it all the way through?  If so, we have a winner.
                    if (iTry == nSlots)
                        break;
                }

                // Remember the new name.
                ULONG len = ((int)wcslen(rcName)) + 1;
                rProps[iCur].pName = reinterpret_cast<WCHAR*>(sNames.Alloc(len * sizeof(WCHAR)));
                if (rProps[iCur].pName == NULL)
                {
                    ThrowHR(E_OUTOFMEMORY);
                }
                wcscpy_s(rProps[iCur].pName, len, rcName);

                // Don't need to look at this iCur any more, since we know it is completely unique.
            }
        }
    }
    else
    {

        CWStrHash   htNames;
        WSTRHASH    *pItem;
        CUnorderedArray<ULONG, 10> uaDuplicates;    // array to keep track of non-unique names

        // Add the base interface names.   Already know there are no duplicates there.
        for (iCur=0; iCur<cBaseNames; ++iCur)
        {
            pItem = htNames.Add(rBaseNames[iCur]);
            IfNullThrow(pItem);
            pItem->szName = rBaseNames[iCur];
        }

        for (iCur=0; iCur<nSlots; iCur++)
        {
            // If a property with an associated (lower indexed) property, don't need to examine it.
            if (TypeFromToken(rProps[iCur].property) != mdtProperty)
                continue;

            // If the member is not visible to COM then we don't need to examine it.
            if (!rProps[iCur].bMemberVisible)
                continue;

            // see if name is already in table
            if (htNames.Find(rProps[iCur].pName) == NULL)
            {
                // name not found, so add it.
                pItem = htNames.Add(rProps[iCur].pName);
                IfNullThrow(pItem);
                pItem->szName = rProps[iCur].pName;
            }
            else
            {
                // name is a duplicate, so keep track of this index for later decoration
                ULONG *piAppend = uaDuplicates.Append();
                IfNullThrow(piAppend);
                *piAppend = iCur;
            }
        }

        ULONG i;
        ULONG iSize = uaDuplicates.Count();
        ULONG *piTable = uaDuplicates.Table();

        for (i = 0; i < iSize; i++)
        {
            // get index to decorate
            iCur = piTable[i];

            // Copy name into local buffer
            DWORD cchName = (DWORD) wcslen(rProps[iCur].pName);
            if (cchName > MAX_CLASSNAME_LENGTH-cchDuplicateDecoration)
                cchName = MAX_CLASSNAME_LENGTH-cchDuplicateDecoration;

            wcsncpy_s(rcName, cchrcName, rProps[iCur].pName, cchName);

            LPWSTR pSuffix = rcName + cchName;
            UINT iSuffix   = 2;

            // We know this is a duplicate, so immediately decorate name.
            do
            {
                _snwprintf_s(pSuffix, cchDuplicateDecoration, _TRUNCATE, szDuplicateDecoration, iSuffix);
                iSuffix++;
                // keep going while we find this name in the hashtable
            } while (htNames.Find(rcName) != NULL);

            // Now rcName has an acceptable (unique) name.  Remember the new name.
            ULONG len = ((int)wcslen(rcName)) + 1;
            rProps[iCur].pName = reinterpret_cast<WCHAR*>(sNames.Alloc(len * sizeof(WCHAR)));
            if (rProps[iCur].pName == NULL)
            {
                ThrowHR(E_OUTOFMEMORY);
            }
            wcscpy_s(rProps[iCur].pName, len, rcName);

            // Stick it in the table.
            pItem = htNames.Add(rProps[iCur].pName);
            IfNullThrow(pItem);
            pItem->szName = rProps[iCur].pName;
        }
    }
} // void ComMTMemberInfoMap::EliminateDuplicateNames()


// ============================================================================
// Process the dispids for an interface, checking for duplicates.  If
//  any duplicates are found, change them to DISPID_UNKNOWN.
// ============================================================================
void ComMTMemberInfoMap::EliminateDuplicateDispIds(
    CQuickArray<ComMTMethodProps> &rProps,   // Array of method property information.
    UINT        nSlots)                 // Count of entries
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT     hr=S_OK;                // A result.
    UINT        ix;                     // Loop control.
    UINT        cDispids = 0;           // Dispids actually assigned.
    CQuickArray<ULONG> rDispid;         // Array of dispids.

    // Count the Dispids.
    for (ix=0; ix<nSlots; ++ix)
    {
        if (TypeFromToken(rProps[ix].property) == mdtProperty && rProps[ix].dispid != DISPID_UNKNOWN && rProps[ix].bMemberVisible)
            ++cDispids;
    }

    // If not at least two, can't be a duplicate.
    if (cDispids < 2)
        return;

    // Make space for the dispids.
    rDispid.ReSizeThrows(cDispids);

    // Collect the Dispids.
    cDispids = 0;
    for (ix=0; ix<nSlots; ++ix)
    {
        if (TypeFromToken(rProps[ix].property) == mdtProperty && rProps[ix].dispid != DISPID_UNKNOWN && rProps[ix].bMemberVisible)
            rDispid[cDispids++] = rProps[ix].dispid;
    }

    // Sort the dispids.  Scope avoids "initialization bypassed by goto" error.
    {
        CQuickSort<ULONG> sorter(rDispid.Ptr(), cDispids);
        sorter.Sort();
    }

    // Look through the sorted dispids, looking for duplicates.
    for (ix=0; ix<cDispids-1; ++ix)
    {
        // If a duplicate is found...
        if (rDispid[ix] == rDispid[ix+1])
        {
            m_bHadDuplicateDispIds = TRUE;

            // iterate over all slots...
            for (UINT iy=0; iy<nSlots; ++iy)
            {
                // and replace every instance of the duplicate dispid with DISPID_UNKNOWN.
                if (rProps[iy].dispid == rDispid[ix])
                {
                    // Mark the dispid so the system will assign one.
                    rProps[iy].dispid = DISPID_UNKNOWN;
                }
            }
        }

        // Skip through the duplicate range.
        while (ix <cDispids-1 && rDispid[ix] == rDispid[ix+1])
            ++ix;
    }
} // HRESULT ComMTMemberInfoMap::EliminateDuplicateDispIds()


// ============================================================================
// Assign a default member based on "Value" or "ToString", unless there is
//  a dispid of 0.
// ============================================================================
void ComMTMemberInfoMap::AssignDefaultMember(
    CQuickArray<ComMTMethodProps> &rProps,   // Array of method property information.
    CDescPool   &sNames,                // Pool of possibly decorated names.
    UINT        nSlots)                 // Count of entries
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    int         ix;                     // Loop control.
    int         defDispid=-1;           // Default via dispid.
    int         defValueProp=-1;        // Default via szDefaultValue on a method.
    int         defValueMeth=-1;        // Default via szDefaultValue on a property.
    int         defToString=-1;         // Default via szDefaultToString.
    int         *pDef=0;                // Pointer to one of the def* variables.
    LPWSTR      pName=NULL;             // Pointer to a name.
    ULONG       cbSig=0;                // Size of Cor signature.
    ULONG       ixSig=0;                // Index into COM+ signature.
    ULONG       callconv=0;             // A member's calling convention.
    ULONG       cParams=0;              // A member's parameter count.
    ULONG       retval=0;               // A default member's return type.
    PCCOR_SIGNATURE pbSig;              // Pointer to Cor signature.

    for (ix=0; ix<(int)nSlots; ++ix)
    {
        // If this is the explicit default, done.
        if (rProps[ix].dispid == DISPID_VALUE)
        {
            defDispid = ix;
            break;
        }

        // If this has an assigned dispid, honor it.
        if (rProps[ix].dispid != DISPID_UNKNOWN)
            continue;

        // Skip linked properties and non-properties.
        if (TypeFromToken(rProps[ix].property) != mdtProperty)
            continue;

        pName = rProps[ix].pName;
        if (SString::_wcsicmp(pName, szDefaultValue) == 0)
        {
            if (rProps[ix].semantic != 0)
                pDef = &defValueProp;
            else
                pDef = &defValueMeth;
        }
        else if (SString::_wcsicmp(pName, szDefaultToString) == 0)
        {
            pDef = &defToString;
        }

        // If a potential match was found, see if it is "simple" enough.  A field is OK;
        //  a property get function is OK if it takes 0 params; a put is OK with 1.
        if (pDef)
        {
            // Fields are by definition simple enough, so only check if some sort of func.
            if (rProps[ix].semantic < FieldSemanticOffset)
            {
                // Get the signature, skip the calling convention, get the param count.
                rProps[ix].pMeth->GetSig(&pbSig, &cbSig);
                ixSig = CorSigUncompressData(pbSig, &callconv);
                _ASSERTE(callconv != IMAGE_CEE_CS_CALLCONV_FIELD);
                ixSig += CorSigUncompressData(&pbSig[ixSig], &cParams);

                // If too many params, don't consider this one any more.
                if (cParams > 1 || (cParams == 1 && rProps[ix].semantic != msSetter))
                    pDef = 0;
            }
            // If we made it through the above checks, save the index of this member.
            if (pDef)
                *pDef = ix, pDef = 0;
        }
    }

    // If there wasn't a DISPID_VALUE already assigned...
    if (defDispid == -1)
    {
        // Was there a "Value" or "ToSTring"
        if (defValueMeth > -1)
            defDispid = defValueMeth;
        else if (defValueProp > -1)
            defDispid = defValueProp;
        else if (defToString > -1)
            defDispid = defToString;

        // Make it the "Value"
        if (defDispid >= 0)
            rProps[defDispid].dispid = DISPID_VALUE;
    }
    else
    {
        // This was a pre-assigned DISPID_VALUE.  If it is a function, try to
        //  turn into a propertyget.
        if (rProps[defDispid].semantic == 0)
        {
            // See if the function returns anything.
            rProps[defDispid].pMeth->GetSig(&pbSig, &cbSig);
            PREFIX_ASSUME(pbSig != NULL);

            ixSig = CorSigUncompressData(pbSig, &callconv);
            _ASSERTE(callconv != IMAGE_CEE_CS_CALLCONV_FIELD);
            ixSig += CorSigUncompressData(&pbSig[ixSig], &cParams);
            ixSig += CorSigUncompressData(&pbSig[ixSig], &retval);
            if (retval != ELEMENT_TYPE_VOID)
            {
                rProps[defDispid].semantic = msGetter;
                rProps[defDispid].bFunction2Getter = TRUE;
            }
        }
    }
} // void ComMTMemberInfoMap::AssignDefaultMember()


// ============================================================================
// Assign a DISPID_NEWENUM member based on "GetEnumerator", unless there is
// already a member with DISPID_NEWENUM.
// ============================================================================
void ComMTMemberInfoMap::AssignNewEnumMember(
    CQuickArray<ComMTMethodProps> &rProps,   // Array of method property information.
    CDescPool   &sNames,                // Pool of possibly decorated names.
    UINT        nSlots)                 // Count of entries
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // HRESULT.
    int         ix;                     // Loop control.
    int         enumDispid=-1;          // Default via dispid.
    int         badEnumDispid=-1;       // Misdefined default via dispid.
    int         enumGetEnumMeth=-1;     // Default via szGetEnumerator on a method.
    int         *pNewEnum=0;            // Pointer to one of the def* variables.
    ULONG       elem;                   // The element type.
    mdToken     tkTypeRef;              // Token for a TypeRef/TypeDef
    LPWSTR      pName;                  // Pointer to a name.
    ULONG       cbSig;                  // Size of Cor signature.
    ULONG       ixSig;                  // Index into COM+ signature.
    ULONG       callconv;               // A member's calling convention.
    ULONG       cParams;                // A member's parameter count.
    MethodDesc  *pMeth;                 // A method desc.
    LPCUTF8     pclsname;               // Class name for ELEMENT_TYPE_CLASS.

    CQuickArray<CHAR> rName;            // Library name.
    PCCOR_SIGNATURE pbSig;              // Pointer to Cor signature.

    for (ix=0; ix<(int)nSlots; ++ix)
    {
        // If we previously found a poorly defined newenum member, we need to clear it.
        if (badEnumDispid != -1)
        {
            rProps[badEnumDispid].dispid = DISPID_UNKNOWN;
            badEnumDispid = -1;
        }

        // In case we have a poorly defined newenum member, we need to remember it.
        if (rProps[ix].dispid == DISPID_NEWENUM)
            badEnumDispid = ix;

        // Only consider method.
        if (rProps[ix].semantic != 0)
            continue;

        // Skip any members that have explicitly assigned DISPID's unless it's the newenum dispid.
        if ((rProps[ix].dispid != DISPID_UNKNOWN) && (rProps[ix].dispid != DISPID_NEWENUM))
            continue;

        // Check to see if the member is GetEnumerator.
        pName = rProps[ix].pName;
        if (SString::_wcsicmp(pName, szGetEnumerator) != 0)
            continue;

        pMeth = rProps[ix].pMeth;

        // Get the signature, skip the calling convention, get the param count.
        pMeth->GetSig(&pbSig, &cbSig);
        PREFIX_ASSUME(pbSig != NULL);

        ixSig = CorSigUncompressData(pbSig, &callconv);
        _ASSERTE(callconv != IMAGE_CEE_CS_CALLCONV_FIELD);
        ixSig += CorSigUncompressData(&pbSig[ixSig], &cParams);

        // If too many params, don't consider this one any more. Also disregard
        // this method if it doesn't have a return type.
        if (cParams != 0 || ixSig >= cbSig)
            continue;

        ixSig += CorSigUncompressData(&pbSig[ixSig], &elem);
        if (elem != ELEMENT_TYPE_CLASS)
            continue;

        // Get the TD/TR.
        ixSig = CorSigUncompressToken(&pbSig[ixSig], &tkTypeRef);

        LPCUTF8 pNS;
        if (TypeFromToken(tkTypeRef) == mdtTypeDef)
        {
            // Get the name of the TypeDef.
            if (FAILED(pMeth->GetMDImport()->GetNameOfTypeDef(tkTypeRef, &pclsname, &pNS)))
            {
                continue;
            }
        }
        else
        {
            // Get the name of the TypeRef.
            _ASSERTE(TypeFromToken(tkTypeRef) == mdtTypeRef);
            if (FAILED(pMeth->GetMDImport()->GetNameOfTypeRef(tkTypeRef, &pNS, &pclsname)))
            {
                continue;
            }
        }

        if (pNS)
        {
            // Pre-pend the namespace to the class name.
            rName.ReSizeThrows((int)(strlen(pclsname)+strlen(pNS)+2));
            strcpy_s(rName.Ptr(), rName.Size(), pNS);
            strcat_s(rName.Ptr(), rName.Size(), NAMESPACE_SEPARATOR_STR);
            strcat_s(rName.Ptr(), rName.Size(), pclsname);
            pclsname = rName.Ptr();
        }

        // Make sure the returned type is an IEnumerator.
        if (stricmpUTF8(pclsname, g_CollectionsEnumeratorClassName) != 0)
            continue;

        // If assigned the newenum dispid, that's it.
        if (rProps[ix].dispid == DISPID_NEWENUM)
        {
            enumDispid = ix;
            break;
        }

        // The method is a valid GetEnumerator method.
        enumGetEnumMeth = ix;
    }

    // If there wasn't a DISPID_NEWENUM already assigned...
    if (enumDispid == -1)
    {
        // If there was a GetEnumerator then give it DISPID_NEWENUM.
        if (enumGetEnumMeth > -1)
            rProps[enumGetEnumMeth].dispid = DISPID_NEWENUM;
    }
} // void ComMTMemberInfoMap::AssignNewEnumMember()

//*****************************************************************************
// Signature utilities.
//*****************************************************************************
class MetaSigExport : public MetaSig
{
public:
    MetaSigExport(MethodDesc *pMD) :
        MetaSig(pMD)
    {
        WRAPPER_NO_CONTRACT;
    }

    BOOL IsVbRefType()
    {
        CONTRACT(BOOL)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACT_END;

        // Get the arg, and skip decorations.
        SigPointer pt = GetArgProps();
        CorElementType mt;
        if (FAILED(pt.PeekElemType(&mt)))
            return FALSE;

        while (mt == ELEMENT_TYPE_BYREF || mt == ELEMENT_TYPE_PTR)
        {
            // Eat the one just examined, and peek at the next one.
            if (FAILED(pt.GetElemType(NULL)) || FAILED(pt.PeekElemType(&mt)))
                return FALSE;
        }

        // Is it just Object?
        if (mt == ELEMENT_TYPE_OBJECT)
            RETURN TRUE;

        // A particular class?
        if (mt == ELEMENT_TYPE_CLASS)
        {
            // Exclude "string".
            if (pt.IsStringType(m_pModule, GetSigTypeContext()))
                RETURN FALSE;
            RETURN TRUE;
        }

        // A particular valuetype?
        if (mt == ELEMENT_TYPE_VALUETYPE)
        {
            // Include "variant".
            if (pt.IsClass(m_pModule, g_VariantClassName, GetSigTypeContext()))
                RETURN TRUE;
            RETURN FALSE;
        }

        // An array, a string, or POD.
        RETURN FALSE;
    }
}; // class MetaSigExport : public MetaSig

// ============================================================================
// For each property set and let functions, determine PROPERTYPUT and
//  PROPERTYPUTREF.
// ============================================================================
void ComMTMemberInfoMap::FixupPropertyAccessors(
    CQuickArray<ComMTMethodProps> &rProps,   // Array of method property information.
    CDescPool   &sNames,                // Pool of possibly decorated names.
    UINT        nSlots)                 // Count of entries
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    UINT        ix;                     // Loop control.
    UINT        j;                      // Inner loop.
    int         iSet;                   // Index of Set method.
    int         iOther;                 // Index of Other method.

    for (ix=0; ix<nSlots; ++ix)
    {
        // Skip linked properties and non-properties.
        if (TypeFromToken(rProps[ix].property) != mdtProperty)
            continue;

        // What is this one?
        switch (rProps[ix].semantic)
        {
        case msSetter:
            iSet = ix;
            iOther = -1;
            break;
        case msOther:
            iOther = ix;
            iSet = -1;
            break;
        default:
            iSet = iOther = -1;
        }

        // Look for the others.
        for (j=ix+1; j<nSlots && (iOther == -1 || iSet == -1); ++j)
        {
            if ((UINT)rProps[j].property == ix)
            {
                // Found one -- what is it?
                switch (rProps[j].semantic)
                {
                case msSetter:
                    _ASSERTE(iSet == -1);
                    iSet = j;
                    break;
                case msOther:
                    _ASSERTE(iOther == -1);
                    iOther = j;
                    break;
                }
            }
        }

        // If both, or neither, or "VB Specific Let" (msOther) only, keep as-is.
        if (((iSet == -1) == (iOther == -1)) || (iSet == -1))
            continue;

        _ASSERTE(iSet != -1 && iOther == -1);

        // Get the signature.
        MethodDesc *pMeth = rProps[iSet].pMeth;
        MetaSigExport msig(pMeth);

        UINT numArgs = msig.NumFixedArgs();
        for (DWORD i = 0; i < numArgs; i++)
            msig.NextArg();

        if (msig.IsVbRefType())
            rProps[iSet].semantic = msSetter;
        else
            rProps[iSet].semantic = msOther;

    }
} // void ComMTMemberInfoMap::FixupPropertyAccessors()


void ComMTMemberInfoMap::AssignDefaultDispIds()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Assign the DISPID's using the same algorithm OLEAUT uses.
    DWORD nSlots = (DWORD)m_MethodProps.Size();
    for (DWORD i = 0; i < nSlots; i++)
    {
        // Retrieve the properties for the current member.
        ComMTMethodProps *pProps = &m_MethodProps[i];

        if (pProps->dispid == DISPID_UNKNOWN)
        {
            if (pProps->semantic > FieldSemanticOffset)
            {
                // We are dealing with a field.
                pProps->dispid = BASE_OLEAUT_DISPID + i;
                m_MethodProps[i + 1].dispid = BASE_OLEAUT_DISPID + i;

                // Skip the next method since field methods always come in pairs.
                _ASSERTE(i + 1 < nSlots && m_MethodProps[i + 1].property == i);
                i++;
            }
            else if (pProps->property == mdPropertyNil)
            {
                // Make sure that this is either a real method or a method transformed into a getter.
                _ASSERTE(pProps->semantic == 0 || pProps->semantic == msGetter);

                // We are dealing with a method.
                pProps->dispid = BASE_OLEAUT_DISPID + i;

            }
            else
            {
                // We are dealing with a property.
                if (TypeFromToken(pProps->property) == mdtProperty)
                {
                    pProps->dispid = BASE_OLEAUT_DISPID + i;
                }
                else
                {
                    pProps->dispid = m_MethodProps[pProps->property].dispid;
                }
            }
        }
    }
} // void ComMTMemberInfoMap::AssignDefaultDispIds()


void ComMTMemberInfoMap::PopulateMemberHashtable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD nSlots = (DWORD)m_MethodProps.Size();

    // Go through the members and add them to the hashtable.
    for (DWORD i = 0; i < nSlots; i++)
    {
        // Retrieve the properties for the current member.
        ComMTMethodProps *pProps = &m_MethodProps[i];

        if (pProps->semantic > FieldSemanticOffset)
        {
            // We are dealing with a field.
            ComCallMethodDesc *pFieldMeth = reinterpret_cast<ComCallMethodDesc*>(pProps->pMeth);
            FieldDesc *pFD = pFieldMeth->GetFieldDesc();

            // Insert the member into the hashtable.
            EEModuleTokenPair Key(pFD->GetMemberDef(), pFD->GetModule());
            m_TokenToComMTMethodPropsMap.InsertValue(&Key, (HashDatum)pProps);

            // Skip the next method since field methods always come in pairs.
            _ASSERTE(i + 1 < nSlots && m_MethodProps[i + 1].property == i);
            i++;
        }
        else if (pProps->property == mdPropertyNil)
        {
            // Make sure that this is either a real method or a method transformed into a getter.
            _ASSERTE(pProps->semantic == 0 || pProps->semantic == msGetter);

            // We are dealing with a method.
            MethodDesc *pMD = pProps->pMeth;
            EEModuleTokenPair Key(pMD->GetMemberDef(), pMD->GetModule());
            m_TokenToComMTMethodPropsMap.InsertValue(&Key, (HashDatum)pProps);
        }
        else
        {
            // We are dealing with a property.
            if (TypeFromToken(pProps->property) == mdtProperty)
            {
                // This is the first method of the property.
                MethodDesc *pMD = pProps->pMeth;
                EEModuleTokenPair Key(pProps->property, pMD->GetModule());
                m_TokenToComMTMethodPropsMap.InsertValue(&Key, (HashDatum)pProps);
            }
        }
    }
} // void ComMTMemberInfoMap::PopulateMemberHashtable()
