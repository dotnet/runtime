// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*============================================================
**
** Header:  Map associated with a ComMethodTable that contains
**          information on its members.
===========================================================*/

#ifndef _COMMTMEMBERINFOMAP_H
#define _COMMTMEMBERINFOMAP_H

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "vars.hpp"


// Forward declarations.
struct ComMethodTable;
class MethodDesc;


// Constants.
static const unsigned int   FieldSemanticOffset =       100;
static LPCSTR               szInitName =                COR_CTOR_METHOD_NAME; // not unicode
static LPCWSTR              szInitNameUse =             W("Init");
static LPCWSTR              szDefaultToString =         W("ToString");
static LPCWSTR              szDuplicateDecoration =     W("_%d");
static const int            cchDuplicateDecoration =    10; // max is _16777215 (0xffffff)
static const int            cbDuplicateDecoration =     20;  // max is _16777215 (0xffffff)


// Properties of a method in a ComMethodTable.
struct ComMTMethodProps
{
    MethodDesc* pMeth;              // MethodDesc for the method.
    LPWSTR      pName;              // The method name.  May be a property name.
    mdToken     property;           // Property associated with a name.  May be the token,
                                    //  the index of an associated member, or -1;
    ULONG       dispid;             // The dispid to use for the method.  Get from metadata
                                    //  or determine from "Value" or "ToString".
    USHORT      semantic;           // Semantic of the property, if any.
    SHORT       oVft;               // vtable offset, if not auto-assigned.
    SHORT       bMemberVisible;     // A flag indicating that the member is visible from COM
    SHORT       bFunction2Getter;   // If true, function was munged to getter
};



//*****************************************************************************
// Class to perform memory management for building FuncDesc's etc. for
//  TypeLib creation.  Memory is not moved as the heap is expanded, and
//  all of the allocations are cleaned up in the destructor.
//*****************************************************************************
class CDescPool
{
private:
    struct SegmentNode
    {
        SegmentNode* pNext;
        BYTE* pData;
        ULONG cbSize;
        ULONG cbUsed;
    };

    SegmentNode* m_pHead;
    SegmentNode* m_pCurrent;
    static const ULONG DefaultSegmentSize = 4096;

public:
    CDescPool() : m_pHead(NULL), m_pCurrent(NULL)
    {
        LIMITED_METHOD_CONTRACT;
    }

    ~CDescPool()
    {
        LIMITED_METHOD_CONTRACT;
        SegmentNode* pNode = m_pHead;
        while (pNode != NULL)
        {
            SegmentNode* pNext = pNode->pNext;
            delete[] pNode->pData;
            delete pNode;
            pNode = pNext;
        }
    }

    // Allocate some bytes from the pool.
    BYTE* Alloc(ULONG nBytes)
    {
        CONTRACT (BYTE*)
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        // Check if current segment has enough space
        if (m_pCurrent == NULL || (m_pCurrent->cbSize - m_pCurrent->cbUsed) < nBytes)
        {
            // Allocate a new segment
            ULONG cbSegmentSize = max(DefaultSegmentSize, nBytes);
            SegmentNode* pNewNode = new SegmentNode();
            pNewNode->pData = new BYTE[cbSegmentSize];

            pNewNode->cbSize = cbSegmentSize;
            pNewNode->cbUsed = 0;
            pNewNode->pNext = NULL;

            // Add to the list
            if (m_pHead == NULL)
            {
                m_pHead = pNewNode;
            }
            else
            {
                m_pCurrent->pNext = pNewNode;
            }
            m_pCurrent = pNewNode;
        }

        BYTE* pResult = m_pCurrent->pData + m_pCurrent->cbUsed;
        m_pCurrent->cbUsed += nBytes;
        RETURN pResult;
    }
}; // class CDescPool



// Token and module pair.
class EEModuleTokenPair
{
public:
    mdToken         m_tk;
    Module *        m_pModule;

    EEModuleTokenPair() : m_tk(0), m_pModule(NULL)
    {
        LIMITED_METHOD_CONTRACT;
    }

    EEModuleTokenPair(mdToken tk, Module *pModule) : m_tk(tk), m_pModule(pModule)
    {
        LIMITED_METHOD_CONTRACT;
    }
};



// Token and module pair hashtable helper.
class EEModuleTokenHashTableHelper
{
public:
    static EEHashEntry_t*       AllocateEntry(EEModuleTokenPair* pKey, BOOL bDeepCopy, AllocationHeap Heap);
    static void                 DeleteEntry(EEHashEntry_t* pEntry, AllocationHeap Heap);
    static BOOL                 CompareKeys(EEHashEntry_t* pEntry, EEModuleTokenPair *pKey);
    static DWORD                Hash(EEModuleTokenPair* pKey);
    static EEModuleTokenPair*   GetKey(EEHashEntry_t* pEntry);
};



// Token and module pair hashtable.
typedef EEHashTable<EEModuleTokenPair *, EEModuleTokenHashTableHelper, FALSE> EEModuleTokenHashTable;



// Map associated with a ComMethodTable that contains information on its members.
class ComMTMemberInfoMap
{
public:
    ComMTMemberInfoMap(MethodTable *pMT) : m_pMT(pMT)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_DefaultProp.ReSizeThrows(1);
        m_DefaultProp[0] = 0;
    }

    // Initialize the map.
    void Init(size_t sizeOfPtr);

    // Retrieve the member information for a given token.
    ComMTMethodProps *GetMethodProps(mdToken tk, Module *pModule);

    // Retrieves all the method properties.
    CQuickArray<ComMTMethodProps> &GetMethods()
    {
        LIMITED_METHOD_CONTRACT;
        return m_MethodProps;
    }

    BOOL HadDuplicateDispIds()
    {
        LIMITED_METHOD_CONTRACT;
        return m_bHadDuplicateDispIds;
    }

private:
    // Helper functions.
    void SetupPropsForIClassX(size_t sizeOfPtr);
    void SetupPropsForInterface(size_t sizeOfPtr);
    void GetMethodPropsForMeth(MethodDesc *pMeth, int ix, CQuickArray<ComMTMethodProps> &rProps, CDescPool &sNames);
    void EliminateDuplicateDispIds(CQuickArray<ComMTMethodProps> &rProps, UINT nSlots);
    void EliminateDuplicateNames(CQuickArray<ComMTMethodProps> &rProps, CDescPool &sNames, UINT nSlots);
    void AssignDefaultMember(CQuickArray<ComMTMethodProps> &rProps, CDescPool &sNames, UINT nSlots);
    void AssignNewEnumMember(CQuickArray<ComMTMethodProps> &rProps, CDescPool &sNames, UINT nSlots);
    void FixupPropertyAccessors(CQuickArray<ComMTMethodProps> &rProps, CDescPool &sNames, UINT nSlots);
    void AssignDefaultDispIds();
    void PopulateMemberHashtable();

    EEModuleTokenHashTable          m_TokenToComMTMethodPropsMap;
    CQuickArray<ComMTMethodProps>   m_MethodProps;
    MethodTable *                   m_pMT;
    CQuickArray<CHAR>               m_DefaultProp;
    CDescPool                       m_sNames;
    BOOL                            m_bHadDuplicateDispIds;
};

#endif // _COMMTMEMBERINFOMAP_H







