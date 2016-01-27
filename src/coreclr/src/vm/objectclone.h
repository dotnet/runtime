// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: ObjectClone.h
//

// 


#ifndef _OBJECTCLONE_H_
#define _OBJECTCLONE_H_

#ifndef FEATURE_REMOTING
#error FEATURE_REMOTING is not set, please do no include objectclone.h
#endif

#include "invokeutil.h"
#include "runtimehandles.h"

enum QueueType
{
    FIFO,
    LIFO
};

enum ObjectProperties
{
    enum_Array = 0x01,
    enum_NeedsUnboxing = 0x02,
    enum_ISerializableMember = 0x04,        // This is set on member of an ISerializable instance
    enum_Iserializable = 0x08,              // This is set on an ISerializable instance
    enum_IObjRef = 0x10,                    // This is set on an IObjRef instance
};

// This is the base class of all the different records that get 
// stored in different tables during cloning
class QueuedObjectInfo
{
protected:
    BYTE    m_properties;
public:
    QueuedObjectInfo() { LIMITED_METHOD_CONTRACT; m_properties = 0; }
    BOOL    IsArray() { LIMITED_METHOD_CONTRACT; return m_properties & enum_Array; }
    BOOL    NeedsUnboxing() { LIMITED_METHOD_CONTRACT; return m_properties & enum_NeedsUnboxing; }
    void    SetIsArray() { LIMITED_METHOD_CONTRACT; m_properties |= enum_Array; }
    void    SetNeedsUnboxing() { LIMITED_METHOD_CONTRACT; m_properties |= enum_NeedsUnboxing; }
    BOOL    IsISerializableMember() { LIMITED_METHOD_CONTRACT; return m_properties & enum_ISerializableMember; }
    void    SetIsISerializableMember() { LIMITED_METHOD_CONTRACT; m_properties |= enum_ISerializableMember; }
    BOOL    IsISerializableInstance() { LIMITED_METHOD_CONTRACT; return m_properties & enum_Iserializable; }
    void    SetIsISerializableInstance() { LIMITED_METHOD_CONTRACT; m_properties |= enum_Iserializable; }
    BOOL    IsIObjRefInstance() { LIMITED_METHOD_CONTRACT; return m_properties & enum_IObjRef; }
    void    SetIsIObjRefInstance() { LIMITED_METHOD_CONTRACT; m_properties |= enum_IObjRef; }
    virtual DWORD GetSize()
        { 
            LIMITED_METHOD_CONTRACT; 
            STATIC_CONTRACT_SO_TOLERANT;            
            DWORD size = sizeof(QueuedObjectInfo);
#if defined(_WIN64) || defined(ALIGN_ACCESS)
            size = (DWORD)ALIGN_UP(size, sizeof(SIZE_T));
#endif // _WIN64 || ALIGN_ACCESS
            return size;
        }
};

// These are records in QOF. Represents a parent object which has at least one member to
// be marshalled and fixed up.
class ParentInfo : public QueuedObjectInfo
{
    DWORD   m_fixupCount;
    DWORD   m_numSpecialMembers;
    DWORD   m_IserIndexInTSOTable;
    DWORD   m_IObjRefIndexInTSOTable;
    DWORD   m_BoxedValIndexIntoTSOTable;
public:
    ParentInfo(DWORD count) 
        {   
            LIMITED_METHOD_CONTRACT; 
            m_fixupCount = count; 
            m_numSpecialMembers = 0;
            m_IserIndexInTSOTable = (DWORD) -1;
            m_IObjRefIndexInTSOTable = (DWORD) -1;
            m_BoxedValIndexIntoTSOTable = (DWORD) -1;
        }
    DWORD DecrementFixupCount() { LIMITED_METHOD_CONTRACT; return --m_fixupCount; }
    DWORD GetNumSpecialMembers() { LIMITED_METHOD_CONTRACT; return m_numSpecialMembers; }
    DWORD IncrementSpecialMembers() { LIMITED_METHOD_CONTRACT; return ++m_numSpecialMembers; }
    DWORD GetISerIndexIntoTSO() { LIMITED_METHOD_CONTRACT; return m_IserIndexInTSOTable; }
    void SetISerIndexIntoTSO(DWORD index) { LIMITED_METHOD_CONTRACT; m_IserIndexInTSOTable = index; }
    DWORD GetIObjRefIndexIntoTSO() { LIMITED_METHOD_CONTRACT; return m_IObjRefIndexInTSOTable; }
    void SetIObjRefIndexIntoTSO(DWORD index) { LIMITED_METHOD_CONTRACT; m_IObjRefIndexInTSOTable = index; }
    DWORD GetBoxedValIndexIntoTSO() { LIMITED_METHOD_CONTRACT; return m_BoxedValIndexIntoTSOTable; }
    void SetBoxedValIndexIntoTSO(DWORD index) { LIMITED_METHOD_CONTRACT; m_BoxedValIndexIntoTSOTable = index; }
    virtual DWORD GetSize()
        { 
            LIMITED_METHOD_CONTRACT; 
            STATIC_CONTRACT_SO_TOLERANT;
            DWORD size = sizeof(ParentInfo);
#if defined(_WIN64) || defined(ALIGN_ACCESS)
            size = (DWORD)ALIGN_UP(size, sizeof(SIZE_T));
#endif // _WIN64 || ALIGN_ACCESS
            return size;
        }
};

// Represents an object whose parent is a regular object (not an array, not ISerializable etc)
// Contains enough information to fix this object into its parent
class ObjectMemberInfo : public QueuedObjectInfo
{
    FieldDesc   *m_fieldDesc;
public:
    ObjectMemberInfo(FieldDesc *field) { LIMITED_METHOD_CONTRACT; m_fieldDesc = field; }
    FieldDesc *GetFieldDesc() { LIMITED_METHOD_CONTRACT; return m_fieldDesc; }
    VOID  SetFieldDesc(FieldDesc* field) { LIMITED_METHOD_CONTRACT; m_fieldDesc = field; }
    virtual DWORD GetSize()
        { 
            LIMITED_METHOD_CONTRACT; 
            STATIC_CONTRACT_SO_TOLERANT;            
            DWORD size = sizeof(ObjectMemberInfo);
#if defined(_WIN64) || defined(ALIGN_ACCESS)
            size = (DWORD)ALIGN_UP(size, sizeof(SIZE_T));
#endif // _WIN64 || ALIGN_ACCESS
            return size;
        }
};

// Represents an object whose parent is an array 
// Contains index information to fix this object into its parent
class NDimArrayMemberInfo : public QueuedObjectInfo
{
    DWORD   m_numDimensions;
    DWORD   m_index[0];
public:
    NDimArrayMemberInfo(DWORD rank)
        { 
            LIMITED_METHOD_CONTRACT; 
            m_numDimensions = rank;
            SetIsArray();
        }
    virtual DWORD GetSize()
        { 
            LIMITED_METHOD_CONTRACT; 
            STATIC_CONTRACT_SO_TOLERANT;            
            DWORD size = sizeof(NDimArrayMemberInfo) + (sizeof(DWORD) * (m_numDimensions));
#if defined(_WIN64) || defined(ALIGN_ACCESS)
            size = (DWORD)ALIGN_UP(size, sizeof(SIZE_T));
#endif // _WIN64 || ALIGN_ACCESS
            return size;
        }
    DWORD *GetIndices() 
        { LIMITED_METHOD_CONTRACT; return &m_index[0]; }
    void SetIndices(DWORD* indices)
        {
            LIMITED_METHOD_CONTRACT; 
            memcpy(GetIndices(), indices, GetNumDimensions() * sizeof(DWORD));
        }
    DWORD GetNumDimensions()
        { LIMITED_METHOD_CONTRACT; return m_numDimensions; }
    void SetNumDimensions(DWORD rank)
        { LIMITED_METHOD_CONTRACT; m_numDimensions = rank; }
};

// Represents an object whose parent is an ISerializable object 
// Contains index information to fix this object into its parent
class ISerializableMemberInfo : public QueuedObjectInfo
{
    DWORD           m_TIOIndex;
    DWORD           m_fieldIndex;
public:
    ISerializableMemberInfo(DWORD tableIndex, DWORD fieldIndex)
        {
            WRAPPER_NO_CONTRACT; 
            m_TIOIndex = tableIndex;
            m_fieldIndex = fieldIndex;
            SetIsISerializableMember();
        }
    DWORD GetTableIndex() 
        { LIMITED_METHOD_CONTRACT; return m_TIOIndex; }
    DWORD GetFieldIndex()
        { LIMITED_METHOD_CONTRACT; STATIC_CONTRACT_SO_TOLERANT; return m_fieldIndex; }
    virtual DWORD GetSize()
        { 
            LIMITED_METHOD_CONTRACT; 
            STATIC_CONTRACT_SO_TOLERANT;            
            DWORD size = sizeof(ISerializableMemberInfo);
#if defined(_WIN64) || defined(ALIGN_ACCESS)
            size = (DWORD)ALIGN_UP(size, sizeof(SIZE_T));
#endif // _WIN64 || ALIGN_ACCESS
            return size;
        }
};

// Represents a special object (ISerializable, Boxed value type, IObjectReference)
// Entries in TSO are of this type
class SpecialObjectInfo : public QueuedObjectInfo
{
protected:
    DWORD       m_specialObjectProperties;
    int         m_objectId;
    DWORD       m_numSpecialMembers;
    DWORD       m_mappingTableIndex;
public:
    SpecialObjectInfo() 
    { 
        LIMITED_METHOD_CONTRACT; 
        m_specialObjectProperties = 0; 
        m_mappingTableIndex = 0;
        m_numSpecialMembers  = 0;
        m_objectId = 0;
    }
    void SetHasBeenProcessed()  { LIMITED_METHOD_CONTRACT; m_specialObjectProperties |= 0x01; }
    DWORD HasBeenProcessed()    { LIMITED_METHOD_CONTRACT; return m_specialObjectProperties & 0x01; }
    void SetHasFixupInfo()  { LIMITED_METHOD_CONTRACT; m_specialObjectProperties |= 0x02; }
    DWORD HasFixupInfo()    { LIMITED_METHOD_CONTRACT; return m_specialObjectProperties & 0x02; }
    void SetIsRepeatObject()  { LIMITED_METHOD_CONTRACT; m_specialObjectProperties |= 0x04; }
    DWORD IsRepeatObject()    { LIMITED_METHOD_CONTRACT; return m_specialObjectProperties & 0x04; }
    void SetIsBoxedObject()  { LIMITED_METHOD_CONTRACT; m_specialObjectProperties |= 0x08; }
    DWORD IsBoxedObject()    { LIMITED_METHOD_CONTRACT; return m_specialObjectProperties & 0x08; }
    void SetTargetNotISerializable()  { LIMITED_METHOD_CONTRACT; m_specialObjectProperties |= 0x10; }
    DWORD IsTargetNotISerializable()    { LIMITED_METHOD_CONTRACT; return m_specialObjectProperties & 0x10; }
    
    void SetMappingTableIndex(DWORD index)  { LIMITED_METHOD_CONTRACT; m_mappingTableIndex = index; }
    DWORD GetMappingTableIndex()    { LIMITED_METHOD_CONTRACT; return m_mappingTableIndex; }
    DWORD GetNumSpecialMembers() { LIMITED_METHOD_CONTRACT; return m_numSpecialMembers; }
    void SetNumSpecialMembers(DWORD numSpecialMembers) { LIMITED_METHOD_CONTRACT; m_numSpecialMembers = numSpecialMembers;}
    void SetObjectId(int id) { LIMITED_METHOD_CONTRACT; m_objectId = id; }
    int GetObjectId() { LIMITED_METHOD_CONTRACT; return m_objectId; }
};

// Represents a special object (ISerializable)
// Contains the number of IObjRef members it has
class ISerializableInstanceInfo : public SpecialObjectInfo
{
public:
    ISerializableInstanceInfo(int objectId, DWORD numIObjRefMembers)
        {
            LIMITED_METHOD_CONTRACT; 
            m_numSpecialMembers = numIObjRefMembers;
            m_objectId = objectId;
            SetIsISerializableInstance();
        }
    virtual DWORD GetSize()
        { 
            LIMITED_METHOD_CONTRACT; 
            STATIC_CONTRACT_SO_TOLERANT;            
            DWORD size = sizeof(ISerializableInstanceInfo);
#if defined(_WIN64) || defined(ALIGN_ACCESS)
            size = (DWORD)ALIGN_UP(size, sizeof(SIZE_T));
#endif // _WIN64 || ALIGN_ACCESS
            return size;
        }
};

// Represents a special object (IObjectReference)
// Contains fixup information to fix the completed object into its parent
class IObjRefInstanceInfo : public SpecialObjectInfo
{
    DWORD           m_ISerTSOIndex;     // If this is also an Iserializable instance, index of the iser entry in TSO
#if defined(_WIN64) || defined(ALIGN_ACCESS)
    DWORD           m_padding;
#endif // _WIN64 || ALIGN_ACCESS
    BYTE            m_fixupData[0];
public:
    IObjRefInstanceInfo(int objectId, DWORD numIObjRefMembers, DWORD numISerMembers)
        {
            WRAPPER_NO_CONTRACT; 
            static_assert_no_msg((offsetof(IObjRefInstanceInfo, m_fixupData) % sizeof(SIZE_T)) == 0);
            m_numSpecialMembers = numIObjRefMembers + numISerMembers;
            m_ISerTSOIndex = (DWORD) -1;
            m_objectId = objectId;
            SetIsIObjRefInstance();
        }
    DWORD GetISerTSOIndex()   {LIMITED_METHOD_CONTRACT; return m_ISerTSOIndex; }
    void SetISerTSOIndex(DWORD index)
        {   LIMITED_METHOD_CONTRACT; m_ISerTSOIndex = index; }
    void SetFixupInfo(QueuedObjectInfo *pData)
        { 
            WRAPPER_NO_CONTRACT; 
            if (pData->GetSize() > 0)
            {
                SetHasFixupInfo();
                memcpy(m_fixupData, pData, pData->GetSize());
            }
        }
    QueuedObjectInfo *GetFixupInfo()
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return (HasFixupInfo() ? (QueuedObjectInfo *)&m_fixupData[0] : NULL);
    }
    virtual DWORD GetSize()
        { 
            WRAPPER_NO_CONTRACT; 
            STATIC_CONTRACT_SO_TOLERANT;
            DWORD size = sizeof(IObjRefInstanceInfo) + (HasFixupInfo() ? ((QueuedObjectInfo *)&m_fixupData[0])->GetSize() : 0); 
#if defined(_WIN64) || defined(ALIGN_ACCESS)
            size = (DWORD)ALIGN_UP(size, sizeof(SIZE_T));
#endif // _WIN64 || ALIGN_ACCESS
            return size;
        }
};

// Represents a special object (Boxed value type)
// Contains fixup information to fix the completed object into its parent
class ValueTypeInfo : public SpecialObjectInfo
{
protected:
    DWORD           m_ISerTSOIndex;     // If this is also an Iserializable instance, index of the iser entry in TSO
    DWORD           m_IObjRefTSOIndex;  // If this is also an IObjRef instance, index of the iser entry in TSO
    BYTE            m_fixupData[0];
public:
    ValueTypeInfo(int objectId, QueuedObjectInfo *pFixupInfo) 
        { 
            WRAPPER_NO_CONTRACT;
            static_assert_no_msg((offsetof(ValueTypeInfo, m_fixupData) % sizeof(SIZE_T)) == 0);
            m_ISerTSOIndex = (DWORD) -1;
            m_IObjRefTSOIndex = (DWORD) -1;
            m_objectId = objectId;
            SetNeedsUnboxing();
            SetIsBoxedObject();
            SetFixupInfo(pFixupInfo);
        }
    DWORD GetISerTSOIndex()   {LIMITED_METHOD_CONTRACT; return m_ISerTSOIndex; }
    void SetISerTSOIndex(DWORD index)
        {   LIMITED_METHOD_CONTRACT; m_ISerTSOIndex = index; }
    DWORD GetIObjRefTSOIndex()   {LIMITED_METHOD_CONTRACT; return m_IObjRefTSOIndex; }
    void SetIObjRefTSOIndex(DWORD index)
        {   LIMITED_METHOD_CONTRACT; m_IObjRefTSOIndex = index; }
    void SetFixupInfo(QueuedObjectInfo *pData)
        { 
            WRAPPER_NO_CONTRACT;
            if (pData->GetSize() > 0)
            {
                SetHasFixupInfo();
                memcpy(m_fixupData, pData, pData->GetSize());
            }
        }
    QueuedObjectInfo *GetFixupInfo()
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return (HasFixupInfo() ? (QueuedObjectInfo *)&m_fixupData[0] : NULL);
    }
    virtual DWORD GetSize()
        { 
            WRAPPER_NO_CONTRACT; 
            STATIC_CONTRACT_SO_TOLERANT;                                   
            DWORD size = sizeof(ValueTypeInfo) + (HasFixupInfo() ? ((QueuedObjectInfo *)&m_fixupData[0])->GetSize() : 0); 
#if defined(_WIN64) || defined(ALIGN_ACCESS)
            size = (DWORD)ALIGN_UP(size, sizeof(SIZE_T));
#endif // _WIN64 || ALIGN_ACCESS
            return size;
        }
};

// Threshold beyond which the collections switch to using the heap
// STACK_TO_HEAP_THRESHOLD/NUM_SLOTS_PER_BUCKET must be 1 or a prime, because
// it is used in GCSafeObjectHashTable as the number of hash table buckets.
#ifdef _DEBUG
#define STACK_TO_HEAP_THRESHOLD 5
#define QOM_STACK_TO_HEAP_THRESHOLD 5
#define QOF_STACK_TO_HEAP_THRESHOLD 5
#define TSO_STACK_TO_HEAP_THRESHOLD 5
#define TDC_STACK_TO_HEAP_THRESHOLD 5
#define VSC_STACK_TO_HEAP_THRESHOLD 5
#define VDC_STACK_TO_HEAP_THRESHOLD 5
#else
#define STACK_TO_HEAP_THRESHOLD (NUM_SLOTS_PER_BUCKET * 29)
#define QOM_STACK_TO_HEAP_THRESHOLD 100
#define QOF_STACK_TO_HEAP_THRESHOLD 16
#define TSO_STACK_TO_HEAP_THRESHOLD 8
#define TDC_STACK_TO_HEAP_THRESHOLD 8
#define VSC_STACK_TO_HEAP_THRESHOLD 8
#define VDC_STACK_TO_HEAP_THRESHOLD 8
#endif

#define NUM_SLOTS_PER_BUCKET 4

#define MAGIC_FACTOR 12

#define LIFO_QUEUE 1
#define FIFO_QUEUE 2


class GCSafeCollection
{
    VPTR_BASE_VTABLE_CLASS(GCSafeCollection)
protected:
    // AppDomain object leak protection: pointer to predicate which flips to false once we should stop reporting GC references.
    PTR_BOOL         m_pfReportRefs;
    
public:
    GCSafeCollection(){}
    virtual void Cleanup() = 0;
    virtual void ReportGCRefs(promote_func *fn, ScanContext* sc) = 0;
};

typedef VPTR(GCSafeCollection) PTR_GCSafeCollection;

class GCSafeObjectTable : public GCSafeCollection
{
    VPTR_VTABLE_CLASS(GCSafeObjectTable, GCSafeCollection);
protected:

    PTR_OBJECTREF   m_Objects1;
    PTR_OBJECTREF   m_Objects2;
    PTR_OBJECTREF   m_Objects3;

    PTR_DWORD       m_dataIndices;
    PTR_BYTE        m_data;

    DWORD           m_currArraySize;
    // Objects
    DWORD           m_count;
    DWORD           m_head;
    // Data
    DWORD           m_numDataBytes;
    DWORD           m_dataHead;
    
    // LIFO/FIFO
    DWORD           m_QueueType;
    BOOL            m_usingHeap;

    BOOL            m_fCleanedUp;

#ifndef DACCESS_COMPILE
    void EnsureSize(DWORD requiredDataSize);
    void Resize();
#endif

public:
#ifndef DACCESS_COMPILE
    void Init(OBJECTREF *ref1, OBJECTREF *ref2, OBJECTREF *ref3, DWORD *dwIndices, BYTE *bData, DWORD currArraySize, DWORD qType, BOOL *pfReportRefs)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_INTOLERANT;                               
        m_Objects1 = ref1;
        m_Objects2 = ref2;
        m_Objects3 = ref3;
        m_dataIndices = dwIndices;
        m_data = bData;
        m_QueueType = qType;
        m_currArraySize = currArraySize;
        m_usingHeap = FALSE;
        m_count = 0;
        m_head = 0;
        m_numDataBytes = 0;
        m_dataHead = 0;
        _ASSERTE(m_QueueType == LIFO_QUEUE || m_QueueType == FIFO_QUEUE);
        // If this is a lifo queue, then the data indices are definitely needed
        _ASSERTE(m_QueueType != LIFO_QUEUE || m_dataIndices != NULL);
        m_pfReportRefs = pfReportRefs;
        m_fCleanedUp = FALSE;
#ifdef USE_CHECKED_OBJECTREFS
        ZeroMemory(m_Objects1, sizeof(OBJECTREF) * m_currArraySize);
        if (m_Objects2 != NULL)
            ZeroMemory(m_Objects2, sizeof(OBJECTREF) * m_currArraySize);
        if (m_Objects3 != NULL)
            ZeroMemory(m_Objects3, sizeof(OBJECTREF) * m_currArraySize);
        for(DWORD i = 0; i < m_currArraySize; i++)
        {
            Thread::ObjectRefProtected(&m_Objects1[i]);
            if (m_Objects2)
                Thread::ObjectRefProtected(&m_Objects2[i]);
            if (m_Objects3)
                Thread::ObjectRefProtected(&m_Objects3[i]);
        }
#endif
    }
    
    void Init(OBJECTREF *ref1, BYTE *bData, DWORD currArraySize, DWORD qType, BOOL *pfReportRefs)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_INTOLERANT;        
        
        m_Objects1 = ref1;
        m_Objects2 = NULL;
        m_Objects3 = NULL;
        m_dataIndices = NULL;
        m_data = bData;
        m_QueueType = qType;
        m_currArraySize = currArraySize;
        m_usingHeap = FALSE;
        m_count = 0;
        m_head = 0;
        m_numDataBytes = 0;
        m_dataHead = 0;
        _ASSERTE(m_QueueType == LIFO_QUEUE || m_QueueType == FIFO_QUEUE);
        // If this is a lifo queue, then the data indices are definitely needed
        _ASSERTE(m_QueueType != LIFO_QUEUE || m_dataIndices != NULL);
        m_pfReportRefs = pfReportRefs;
        m_fCleanedUp = FALSE;
#ifdef USE_CHECKED_OBJECTREFS
        ZeroMemory(m_Objects1, sizeof(OBJECTREF) * m_currArraySize);
        if (m_Objects2 != NULL)
            ZeroMemory(m_Objects2, sizeof(OBJECTREF) * m_currArraySize);
        if (m_Objects3 != NULL)
            ZeroMemory(m_Objects3, sizeof(OBJECTREF) * m_currArraySize);
        for(DWORD i = 0; i < m_currArraySize; i++)
        {
            Thread::ObjectRefProtected(&m_Objects1[i]);
            if (m_Objects2)
                Thread::ObjectRefProtected(&m_Objects2[i]);
            if (m_Objects3)
                Thread::ObjectRefProtected(&m_Objects3[i]);
        }
#endif
    }

#endif // !DACCESS_COMPILE
   
    virtual void Cleanup()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;        

#ifndef DACCESS_COMPILE
        // Set this first, we must disable GC reporting of our objects before we start ripping down the data structures that record
        // those objects. See ReportGCRefs for a more detailed explanation.
        m_fCleanedUp = TRUE;

        if (m_usingHeap == TRUE)
        {
            if (m_Objects1)
                delete[] m_Objects1;
            m_Objects1 = NULL;
            if (m_Objects2)
                delete[] m_Objects2;
            m_Objects2 = NULL;
            if (m_Objects3)
                delete[] m_Objects3;
            m_Objects3 = NULL;
            if (m_data)
                delete[] m_data;
            m_data = NULL;
            if (m_dataIndices)
                delete[] m_dataIndices;
            m_dataIndices = NULL;
        }

#endif // !DACCESS_COMPILE
    }

    virtual void ReportGCRefs(promote_func *fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;

        // Due to the wacky way that frames are cleaned up (they're popped en masse by exception handling code in finally blocks)
        // it's possible that this object may be destructed before the custom GC frame is popped. If there's a GC in the timing
        // window then we end up here with the object destructed. It happens that the underlying storage (the stack) is still valid
        // since the exception handling code that's doing all this unwinding hasn't actually physically unwound the stack yet, but
        // the destructor has been run and may have left the object in an inconsistent state. To solve this in a really obvious
        // manner (less likely to be broken by a random change in the future) we keep a boolean that's set to true once the object
        // has been destructed (actually, once Cleanup has been called, because that's the destructive part). We don't need to
        // report anything beyond this point (and to do so would be dangerous given the state of the collection).
        if (m_fCleanedUp)
            return;

        // We track a predicate (actually embedded in the cloner which owns us) that tells us whether to report any GC refs at all.
        // This is used as a rip cord if the server appdomain is unloaded while we're processing in it (because this collection can
        // survive for a little while after that which is long enough to cause server objects to outlive their domain).
        if (!*m_pfReportRefs)
        {
            m_count = 0;
            if (m_Objects1)
                ZeroMemory(m_Objects1, m_currArraySize * sizeof(OBJECTREF));
            if (m_Objects2)
                ZeroMemory(m_Objects2, m_currArraySize * sizeof(OBJECTREF));
            if (m_Objects3)
                ZeroMemory(m_Objects3, m_currArraySize * sizeof(OBJECTREF));
            return;
        }

        PTR_PTR_Object pRefs1 = dac_cast<PTR_PTR_Object>(m_Objects1);
        PTR_PTR_Object pRefs2 = dac_cast<PTR_PTR_Object>(m_Objects2);
        PTR_PTR_Object pRefs3 = dac_cast<PTR_PTR_Object>(m_Objects3);

        if (m_QueueType == LIFO_QUEUE)
        {
            for (DWORD i = 0; i < m_count; i++)
            {
                _ASSERTE(i < m_currArraySize);
                if (m_Objects1)
                    (*fn)(pRefs1 + i, sc, 0);
                if (m_Objects2)
                    (*fn)(pRefs2 + i, sc, 0);
                if (m_Objects3)
                    (*fn)(pRefs3 + i, sc, 0);
            }
        }
        else
        {
            for (DWORD i = m_head, count = 0; count < m_count; i++, count++)
            {
                i = i % m_currArraySize;
                if (m_Objects1)
                    (*fn)(pRefs1 + i, sc, 0);
                if (m_Objects2)
                    (*fn)(pRefs2 + i, sc, 0);
                if (m_Objects3)
                    (*fn)(pRefs3 + i, sc, 0);
            }
        }
    }

#ifndef DACCESS_COMPILE
    void Enqueue(OBJECTREF refObj, OBJECTREF refParent, OBJECTREF refAux, QueuedObjectInfo *pQOI);
    void Push(OBJECTREF refObj, OBJECTREF refParent, OBJECTREF refAux, QueuedObjectInfo *pQOI);
    void SetAt(DWORD index, OBJECTREF refObj, OBJECTREF refParent, OBJECTREF refAux, QueuedObjectInfo *pQOI);
    OBJECTREF Dequeue(OBJECTREF *refParent, OBJECTREF *refAux, QueuedObjectInfo **pQOI);
    OBJECTREF Pop(OBJECTREF *refParent, OBJECTREF *refAux, QueuedObjectInfo **pQOI);
    OBJECTREF GetAt(DWORD index, OBJECTREF *refParent, OBJECTREF *refAux, QueuedObjectInfo **pQOI);
    OBJECTREF Peek(OBJECTREF *refParent, OBJECTREF *refAux, QueuedObjectInfo **pQOI);
    void BeginEnumeration(DWORD *dwIndex) 
    { 
        LIMITED_METHOD_CONTRACT;
        if (m_QueueType == LIFO_QUEUE)
            *dwIndex = m_count; 
        else
            *dwIndex = 0;
    }
    
    OBJECTREF GetNext(DWORD *dwIndex, OBJECTREF *refParent, OBJECTREF *refAux, QueuedObjectInfo **pQOI)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        
        OBJECTREF refRet = NULL;
        if (m_QueueType == LIFO_QUEUE)
        {
            if (*dwIndex == 0)
                return NULL;

            (*dwIndex)--;
            refRet = GetAt(*dwIndex, refParent, refAux, pQOI);
        }
        else
        {
            if (*dwIndex == m_count)
                return NULL;

            refRet = GetAt(*dwIndex, refParent, refAux, pQOI);
            (*dwIndex)++;
        }
        return refRet;
    }
    
    DWORD GetCount()  { LIMITED_METHOD_CONTRACT; return m_count; }
#endif // !DACCESS_COMPILE
};

#ifndef DACCESS_COMPILE
class DwordArrayList
{
    DWORD       m_dwordsOnStack[STACK_TO_HEAP_THRESHOLD];
    DWORD       *m_dwords;

    DWORD       m_count;
    DWORD       m_currSize;
public:
    
    void Init()
    {
        LIMITED_METHOD_CONTRACT;
        m_dwords = &m_dwordsOnStack[0];
        m_currSize = STACK_TO_HEAP_THRESHOLD;
        m_count = 0;
    }

    void        Add(DWORD i)
    {
        WRAPPER_NO_CONTRACT;
        EnsureSize();
        m_dwords[m_count++] = i;
    }

    void EnsureSize()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            STATIC_CONTRACT_SO_INTOLERANT;            
        }
        CONTRACTL_END
        if (m_count < m_currSize)
            return;

        DWORD newSize = m_currSize * 2;
        // Does not need a holder because this is the only allocation in this method
        DWORD *pTemp = new DWORD[newSize]; 
        ZeroMemory(pTemp, sizeof(DWORD) * newSize);

        memcpy((BYTE*)pTemp, m_dwords, sizeof(DWORD) * m_currSize);
        if (m_dwords != &m_dwordsOnStack[0])
        {
            delete[] m_dwords;
        }
        m_dwords = pTemp;
        m_count = m_currSize;
        m_currSize = newSize;
    }

    void Cleanup()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;        
        if (m_dwords != &m_dwordsOnStack[0])
        {
            delete[] m_dwords;
            m_dwords = &m_dwordsOnStack[0];
        }
    }

    DWORD GetCount() { LIMITED_METHOD_CONTRACT; return m_count; }
    DWORD GetAt(DWORD index)    
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(index >= 0 && index < m_count);
        return m_dwords[index]; 
    }
};
#endif // !DACCESS_COMPILE

class GCSafeObjectHashTable : public GCSafeCollection
{
    VPTR_VTABLE_CLASS(GCSafeObjectHashTable, GCSafeCollection);
private:
    OBJECTREF     m_objectsOnStack[STACK_TO_HEAP_THRESHOLD];
    OBJECTREF     m_newObjectsOnStack[STACK_TO_HEAP_THRESHOLD];
    DWORD         m_dataOnStack[STACK_TO_HEAP_THRESHOLD];
    DWORD         m_count;
    DWORD         m_currArraySize;
    PTR_int       m_ids;
    PTR_OBJECTREF m_objects;
    PTR_OBJECTREF m_newObjects;
    BOOL          m_fCleanedUp;

#ifndef DACCESS_COMPILE
    void Resize();
    int FindElement(OBJECTREF refObj, BOOL &seenBefore);
#endif // !DACCESS_COMPILE

public:

#ifndef DACCESS_COMPILE
    virtual void Init(BOOL *pfReportRefs)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        ZeroMemory(&m_objectsOnStack[0], sizeof(m_objectsOnStack));
        ZeroMemory(&m_dataOnStack[0], sizeof(m_dataOnStack));
        m_objects = &m_objectsOnStack[0];
        m_newObjects = &m_newObjectsOnStack[0];
        m_currArraySize = STACK_TO_HEAP_THRESHOLD;
        m_count = 0;
        m_ids = (int *) &m_dataOnStack[0];
        m_pfReportRefs = pfReportRefs;
        m_fCleanedUp = FALSE;
#ifdef USE_CHECKED_OBJECTREFS
        ZeroMemory(&m_newObjects[0], sizeof(m_newObjectsOnStack));
        for(DWORD i = 0; i < m_currArraySize; i++)
        {
            Thread::ObjectRefProtected(&m_objects[i]);
            Thread::ObjectRefProtected(&m_newObjects[i]);
        }
#endif
    }
#endif  // !DACCESS_COMPILE

    virtual void Cleanup()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;      

#ifndef DACCESS_COMPILE
        // Set this first, we must disable GC reporting of our objects before we start ripping down the data structures that record
        // those objects. See ReportGCRefs for a more detailed explanation.
        m_fCleanedUp = TRUE;

        if (m_newObjects != &m_newObjectsOnStack[0])
        {
            delete[] m_ids;
            m_ids = (int *) &m_dataOnStack[0];
            delete[] m_newObjects;
            m_newObjects = &m_newObjectsOnStack[0];
            delete[] m_objects;
            m_objects = &m_objectsOnStack[0];
            m_currArraySize = STACK_TO_HEAP_THRESHOLD;
        }
#endif  // !DACCESS_COMPILE
    }

    virtual void ReportGCRefs(promote_func *fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        // Due to the wacky way that frames are cleaned up (they're popped en masse by exception handling code in finally blocks)
        // it's possible that this object may be destructed before the custom GC frame is popped. If there's a GC in the timing
        // window then we end up here with the object destructed. It happens that the underlying storage (the stack) is still valid
        // since the exception handling code that's doing all this unwinding hasn't actually physically unwound the stack yet, but
        // the destructor has been run and may have left the object in an inconsistent state. To solve this in a really obvious
        // manner (less likely to be broken by a random change in the future) we keep a boolean that's set to true once the object
        // has been destructed (actually, once Cleanup has been called, because that's the destructive part). We don't need to
        // report anything beyond this point (and to do so would be dangerous given the state of the collection).
        if (m_fCleanedUp)
            return;

        // We track a predicate (actually embedded in the cloner which owns us) that tells us whether to report any GC refs at all.
        // This is used as a rip cord if the server appdomain is unloaded while we're processing in it (because this collection can
        // survive for a little while after that which is long enough to cause server objects to outlive their domain).
        if (!*m_pfReportRefs)
        {
            m_count = 0;
            ZeroMemory(m_ids, m_currArraySize * sizeof(int));
            ZeroMemory(m_objects, m_currArraySize * sizeof(OBJECTREF));
            ZeroMemory(m_newObjects, m_currArraySize * sizeof(OBJECTREF));
            return;
        }

        PTR_PTR_Object pRefs = dac_cast<PTR_PTR_Object>(m_objects);
        PTR_PTR_Object pNewRefs = dac_cast<PTR_PTR_Object>(m_newObjects);
        
        for (DWORD i = 0; i < m_currArraySize; i++)
        {
            if (m_ids[i] != 0)
            {
                (*fn)(pRefs + i, sc, 0);
                (*fn)(pNewRefs + i, sc, 0);
            }
        }
    }
    
#ifndef DACCESS_COMPILE
    int HasID(OBJECTREF refObj, OBJECTREF *newObj);
    int AddObject(OBJECTREF refObj, OBJECTREF newObj); 
    int UpdateObject(OBJECTREF refObj, OBJECTREF newObj);
#endif // !DACCESS_COMPILE
};

#ifndef DACCESS_COMPILE

enum SpecialObjects
{
    ISerializable = 1,
    IObjectReference,
    BoxedValueType
};

enum CloningContext
{
    CrossAppDomain = 1,
    ObjectFreezer
};

class CrossAppDomainClonerCallback
{
    public:
        OBJECTREF AllocateObject(OBJECTREF, MethodTable * pMT)
        {
            WRAPPER_NO_CONTRACT;
            return pMT->Allocate();
        }

        OBJECTREF AllocateArray(OBJECTREF, TypeHandle arrayType, INT32 *pArgs, DWORD dwNumArgs, BOOL bAllocateInLargeHeap)
        {
            WRAPPER_NO_CONTRACT;
            return ::AllocateArrayEx(arrayType, pArgs, dwNumArgs, bAllocateInLargeHeap DEBUG_ARG(FALSE));
        }

        STRINGREF AllocateString(STRINGREF refSrc)
        {
            LIMITED_METHOD_CONTRACT;
            return refSrc;
        }

        void ValidateFromType(MethodTable *pFromMT)
        {
            WRAPPER_NO_CONTRACT;
            CheckSerializable(pFromMT);
        }
        
        void ValidateToType(MethodTable *pToMT)
        {
            WRAPPER_NO_CONTRACT;
            CheckSerializable(pToMT);
        }
        
        BOOL IsRemotedType(MethodTable *pMT, AppDomain* pFromAD, AppDomain* pToDomain)
        {
            WRAPPER_NO_CONTRACT;
            if ((pMT->IsMarshaledByRef() && pFromAD != pToDomain) ||
                pMT->IsTransparentProxy())
                return TRUE;
            
            return FALSE;
        }
        
        BOOL IsISerializableType(MethodTable *pMT)
        {
            CONTRACTL
            {
                THROWS;
                GC_TRIGGERS;
                MODE_COOPERATIVE;
            }
            CONTRACTL_END;
            return pMT->CanCastToNonVariantInterface(MscorlibBinder::GetClass(CLASS__ISERIALIZABLE));
        }
        
        BOOL IsIObjectReferenceType(MethodTable *pMT)
        {
            CONTRACTL
            {
                THROWS;
                GC_TRIGGERS;
                MODE_COOPERATIVE;
            }
            CONTRACTL_END;
            return pMT->CanCastToNonVariantInterface(MscorlibBinder::GetClass(CLASS__IOBJECTREFERENCE));
        }

        BOOL RequiresDeserializationCallback(MethodTable *pMT)
        {
            CONTRACTL
            {
                THROWS;
                GC_TRIGGERS;
                MODE_COOPERATIVE;
            }
            CONTRACTL_END;
            return pMT->CanCastToNonVariantInterface(MscorlibBinder::GetClass(CLASS__IDESERIALIZATIONCB));
        }

        BOOL RequiresDeepCopy(OBJECTREF refObj)
        {
            LIMITED_METHOD_CONTRACT;
            return TRUE;
        }

    private:
        void CheckSerializable(MethodTable *pCurrMT)
        {
            CONTRACTL
            {
                GC_TRIGGERS;
                MODE_COOPERATIVE;
                THROWS;
            }
            CONTRACTL_END;

            // Checking whether the type is marked as Serializable is not enough, all of its ancestor types must be marked this way
            // also. THe only exception is that any type that also implements ISerializable doesn't require a serializable parent.
            if (pCurrMT->IsSerializable())
            {
                MethodTable *pISerializableMT = MscorlibBinder::GetClass(CLASS__ISERIALIZABLE);
                MethodTable *pMT = pCurrMT;
                for (;;)
                {
                    // We've already checked this particular type is marked Serializable, so if it implements ISerializable then
                    // we're done.
                    if (pMT->ImplementsInterface(pISerializableMT))
                        return;

                    // Else we get the parent type and check it is marked Serializable as well.
                    pMT = pMT->GetParentMethodTable();

                    // If we've run out of parents we're done and the type is serializable.
                    if (pMT == NULL)
                        return;

                    // Otherwise check for the attribute.
                    if (!pMT->IsSerializable())
                        break;
                }
            }
         
            if (pCurrMT->IsMarshaledByRef())
                return;

            if (pCurrMT->IsTransparentProxy())
                return;

            if (pCurrMT->IsEnum())
                return;

            if (pCurrMT->IsDelegate())
                return;

            DefineFullyQualifiedNameForClassW();
            LPCWSTR wszCliTypeName = GetFullyQualifiedNameForClassNestedAwareW(pCurrMT);

            SString ssAssemblyName;
            pCurrMT->GetAssembly()->GetDisplayName(ssAssemblyName);
            COMPlusThrow(kSerializationException, IDS_SERIALIZATION_NONSERTYPE, wszCliTypeName, ssAssemblyName.GetUnicode());
        }
};

class CrossDomainFieldMap
{
    struct FieldMapEntry
    {
        ADID         m_dwSrcDomain;
        ADID         m_dwDstDomain;
        MethodTable *m_pSrcMT;
        MethodTable *m_pDstMT;
        FieldDesc  **m_pFieldMap;

        FieldMapEntry(MethodTable *pSrcMT, MethodTable *pDstMT, FieldDesc **pFieldMap);
        ~FieldMapEntry()
        {
            LIMITED_METHOD_CONTRACT;
            delete [] m_pFieldMap;
        }

        UPTR GetHash()
        {
            LIMITED_METHOD_CONTRACT;
            return (UINT)(SIZE_T)m_pSrcMT + ((UINT)(SIZE_T)m_pDstMT >> 2);
        }
    };

    static PtrHashMap      *s_pFieldMap;
    static SimpleRWLock    *s_pFieldMapLock;

    static BOOL CompareFieldMapEntry(UPTR val1, UPTR val2);

public:
    static void FlushStaleEntries();
    static FieldDesc **LookupOrCreateFieldMapping(MethodTable *pDstMT, MethodTable *pSrcMT);
};

// Currently the object cloner uses DWORDs as indices.  We may have to use QWORDs instead 
// if we start to have extremely large object graphs.
class ObjectClone
{
    OBJECTREF   m_QOMObjects[QOM_STACK_TO_HEAP_THRESHOLD];
    BYTE        m_QOMData[QOM_STACK_TO_HEAP_THRESHOLD * MAGIC_FACTOR];

    OBJECTREF   m_QOFObjects[QOF_STACK_TO_HEAP_THRESHOLD];
    BYTE        m_QOFData[QOF_STACK_TO_HEAP_THRESHOLD * MAGIC_FACTOR];

    OBJECTREF   m_TSOObjects1[TSO_STACK_TO_HEAP_THRESHOLD];
    OBJECTREF   m_TSOObjects2[TSO_STACK_TO_HEAP_THRESHOLD];
    OBJECTREF   m_TSOObjects3[TSO_STACK_TO_HEAP_THRESHOLD];
    BYTE        m_TSOData[TSO_STACK_TO_HEAP_THRESHOLD * MAGIC_FACTOR];
    DWORD       m_TSOIndices[TSO_STACK_TO_HEAP_THRESHOLD];

    OBJECTREF   m_TDCObjects[TDC_STACK_TO_HEAP_THRESHOLD];
    BYTE        m_TDCData[TDC_STACK_TO_HEAP_THRESHOLD * MAGIC_FACTOR];

    OBJECTREF   m_VSCObjects[VSC_STACK_TO_HEAP_THRESHOLD];

    OBJECTREF   m_VDCObjects[VDC_STACK_TO_HEAP_THRESHOLD];

    GCSafeObjectTable                QOM;   // Queue_of_Object_to_be_Marshalled
    GCSafeObjectTable                QOF;   // Queue_of_Objects_to_be_Fixed_Up
    GCSafeObjectHashTable            TOS;   // Table_of_Objects_Seen
    GCSafeObjectTable                TSO;   // Table_of_Special_Objects
    GCSafeObjectTable                TDC;   // Table_of_Deserialization_Callbacks
    GCSafeObjectTable                VSC;   // Vts_Serialization_Callbacks
    GCSafeObjectTable                VDC;   // Vts_Deserialization_Callbacks
    DwordArrayList                   TMappings;

    FrameWithCookie<GCSafeCollectionFrame>  QOM_Protector;
    FrameWithCookie<GCSafeCollectionFrame>  QOF_Protector;
    FrameWithCookie<GCSafeCollectionFrame>  TOS_Protector;
    FrameWithCookie<GCSafeCollectionFrame>  TSO_Protector;
    FrameWithCookie<GCSafeCollectionFrame>  TDC_Protector;
    FrameWithCookie<GCSafeCollectionFrame>  VSC_Protector;
    FrameWithCookie<GCSafeCollectionFrame>  VDC_Protector;
    
    BOOL                m_skipFieldScan;
    
    AppDomain*           m_fromDomain;
    AppDomain*           m_toDomain;
    
    OBJECTREF           m_currObject;   // Updated within the loop in Clone method
    OBJECTREF           m_newObject;    // Updated within the loop in Clone method
    OBJECTREF           m_topObject;
    OBJECTREF           m_fromExecutionContext; // Copy of the execution context on the way in (used during callbacks to the from domain)

    BOOL                m_securityChecked;

    // AppDomain object leak protection: predicate which flips to false once we should stop reporting GC references in the
    // collections this cloner owns.
    BOOL                m_fReportRefs;
    
    CrossAppDomainClonerCallback *m_cbInterface;
    CloningContext      m_context;
        
    PTRARRAYREF AllocateISerializable(int objectId, BOOL bIsRemotingObject);
    void AllocateArray();
    void AllocateObject();

    PTRARRAYREF MakeObjectLookLikeISerializable(int objectId);
    
    void HandleISerializableFixup(OBJECTREF refParent, QueuedObjectInfo *currObjFixupInfo);
    void HandleArrayFixup(OBJECTREF refParent, QueuedObjectInfo *currObjFixupInfo);
    void HandleObjectFixup(OBJECTREF refParent, QueuedObjectInfo *currObjFixupInfo);
    void Fixup(OBJECTREF newObj, OBJECTREF refParent, QueuedObjectInfo *currObjFixupInfo);

    void ScanMemberFields(DWORD IObjRefTSOIndex, DWORD BoxedValTSOIndex);
    DWORD CloneField(FieldDesc *pSrcField, FieldDesc *pDstField);
    static BOOL AreTypesEmittedIdentically(MethodTable *pMT1, MethodTable *pMT2);
    void ScanISerializableMembers(DWORD IObjRefTSOIndex, DWORD ISerTSOIndex, DWORD BoxedValTSOIndex, PTRARRAYREF refValues);
    void ScanArrayMembers();
    Object *GetObjectFromArray(BASEARRAYREF* arrObj, DWORD dwOffset);

    void CompleteValueTypeFields(OBJECTREF newObj, OBJECTREF refParent, QueuedObjectInfo *valTypeInfo);
    void CompleteSpecialObjects();
    void CompleteISerializableObject(OBJECTREF IserObj, OBJECTREF refNames, OBJECTREF refValues, ISerializableInstanceInfo *);
    BOOL CompleteIObjRefObject(OBJECTREF IObjRef, DWORD index, IObjRefInstanceInfo *iorInfo);
    void CompleteIDeserializationCallbacks();
    void CompleteVtsOnDeserializedCallbacks();
    void CompleteVtsOnSerializedCallbacks();
    BOOL CheckForUnresolvedMembers(SpecialObjectInfo *splInfo);

    TypeHandle GetCorrespondingTypeForTargetDomain(TypeHandle thCli);
    MethodTable * GetCorrespondingTypeForTargetDomain(MethodTable * pCliMT);
    TypeHandle GetType(const SString &ssTypeName, const SString &ssAssemName);

    DWORD FindObjectInTSO(int objId, SpecialObjects kind);
    ARG_SLOT HandleFieldTypeMismatch(CorElementType srcTy, CorElementType destTy, void *pData, MethodTable *pSrcMT);
    BOOL IsDelayedFixup(MethodTable *newMT, QueuedObjectInfo *);
    OBJECTREF BoxValueTypeInWrongDomain(OBJECTREF refParent, DWORD offset, MethodTable *pValueTypeMT);    

    BOOL HasVtsCallbacks(MethodTable *pMT, RemotingVtsInfo::VtsCallbackType eCallbackType);
    void InvokeVtsCallbacks(OBJECTREF refTarget, RemotingVtsInfo::VtsCallbackType eCallbackType, AppDomain* pDomain);

    RuntimeMethodHandle::StreamingContextStates GetStreamingContextState()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_context == CrossAppDomain)
            return RuntimeMethodHandle::CONTEXTSTATE_CrossAppDomain;

        if (m_context == ObjectFreezer)
            return RuntimeMethodHandle::CONTEXTSTATE_Other;

        _ASSERTE(!"Should not get here; using the cloner with a context we don't understand");
        return RuntimeMethodHandle::CONTEXTSTATE_Other;
    }
public:
    
    void Init(BOOL bInitialInit)
    {
        WRAPPER_NO_CONTRACT;

        if (bInitialInit)
        {
            TOS.Init(&m_fReportRefs);
            TSO.Init(&m_TSOObjects1[0], &m_TSOObjects2[0], &m_TSOObjects3[0], &m_TSOIndices[0], &m_TSOData[0], TSO_STACK_TO_HEAP_THRESHOLD, LIFO_QUEUE, &m_fReportRefs);
        }
        QOM.Init(&m_QOMObjects[0], &m_QOMData[0], QOM_STACK_TO_HEAP_THRESHOLD, FIFO_QUEUE, &m_fReportRefs);
        QOF.Init(&m_QOFObjects[0], &m_QOFData[0], QOF_STACK_TO_HEAP_THRESHOLD, FIFO_QUEUE, &m_fReportRefs);
        TDC.Init(&m_TDCObjects[0], &m_TDCData[0], TDC_STACK_TO_HEAP_THRESHOLD, FIFO_QUEUE, &m_fReportRefs);
        VSC.Init(&m_VSCObjects[0], NULL, VSC_STACK_TO_HEAP_THRESHOLD, FIFO_QUEUE, &m_fReportRefs);
        VDC.Init(&m_VDCObjects[0], NULL, VDC_STACK_TO_HEAP_THRESHOLD, FIFO_QUEUE, &m_fReportRefs);
        TMappings.Init();
    }
    void Cleanup(BOOL bFinalCleanup)
    {
        WRAPPER_NO_CONTRACT;
        if (bFinalCleanup)
        {
            TOS.Cleanup();
            TSO.Cleanup();
        }

        QOM.Cleanup();
        QOF.Cleanup();
        TDC.Cleanup();
        VSC.Cleanup();
        VDC.Cleanup();
        TMappings.Cleanup();
    }

    ObjectClone(CrossAppDomainClonerCallback *cbInterface, CloningContext cc=CrossAppDomain, BOOL bNeedSecurityCheck = TRUE)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        static_assert_no_msg((offsetof(ObjectClone, m_QOMObjects) % sizeof(SIZE_T)) == 0);
        static_assert_no_msg((offsetof(ObjectClone, m_QOFObjects) % sizeof(SIZE_T)) == 0);
        static_assert_no_msg((offsetof(ObjectClone, m_TSOData)    % sizeof(SIZE_T)) == 0);
        static_assert_no_msg((offsetof(ObjectClone, m_TDCData)    % sizeof(SIZE_T)) == 0);
        static_assert_no_msg((offsetof(ObjectClone, m_VSCObjects) % sizeof(SIZE_T)) == 0);
        static_assert_no_msg((offsetof(ObjectClone, m_VDCObjects) % sizeof(SIZE_T)) == 0);

        m_securityChecked = !bNeedSecurityCheck; 
        m_context = cc;
        m_cbInterface = cbInterface;
        m_fReportRefs = true;

        Init(TRUE);

        // Order of these is important. The frame lowest on the stack (ie declared last inside ObjectClone) has to be pushed first
        (void)new (VDC_Protector.GetGSCookiePtr()) FrameWithCookie<GCSafeCollectionFrame>(&VDC);
        (void)new (VSC_Protector.GetGSCookiePtr()) FrameWithCookie<GCSafeCollectionFrame>(&VSC);
        (void)new (TDC_Protector.GetGSCookiePtr()) FrameWithCookie<GCSafeCollectionFrame>(&TDC);
        (void)new (TSO_Protector.GetGSCookiePtr()) FrameWithCookie<GCSafeCollectionFrame>(&TSO);
        (void)new (TOS_Protector.GetGSCookiePtr()) FrameWithCookie<GCSafeCollectionFrame>(&TOS);
        (void)new (QOF_Protector.GetGSCookiePtr()) FrameWithCookie<GCSafeCollectionFrame>(&QOF);
        (void)new (QOM_Protector.GetGSCookiePtr()) FrameWithCookie<GCSafeCollectionFrame>(&QOM);
    }

    void RemoveGCFrames()
    {
        LIMITED_METHOD_CONTRACT;
        // Order of these is important. The frame highest on the stack has to be pushed first
        QOM_Protector.Pop();
        QOF_Protector.Pop();
        TOS_Protector.Pop();
        TSO_Protector.Pop();
        TDC_Protector.Pop();
        VSC_Protector.Pop();
        VDC_Protector.Pop();
    }

    ~ObjectClone()
    {
        WRAPPER_NO_CONTRACT;
        Cleanup(TRUE);
    }
    
    OBJECTREF Clone(OBJECTREF refObj, AppDomain* fromDomain, AppDomain* toDomain, OBJECTREF refExecutionContext)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        TypeHandle thDummy;
        OBJECTREF refResult = Clone(refObj, thDummy, fromDomain, toDomain, refExecutionContext);
        return refResult;
    }
    
    OBJECTREF Clone(OBJECTREF refObj, 
            TypeHandle expectedType, 
            AppDomain *fromDomain, 
            AppDomain *toDomain, 
            OBJECTREF refExecutionContext);

    static void StopReportingRefs(ObjectClone *pThis)
    {
        pThis->m_fReportRefs = false;
    }
};

typedef Holder<ObjectClone *, DoNothing<ObjectClone*>, ObjectClone::StopReportingRefs> ReportClonerRefsHolder;

#endif
#endif
