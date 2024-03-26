// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "eventtrace.h"
#include "winbase.h"
#include "contract.h"
#include "ex.h"
#include "dbginterface.h"
#include "finalizerthread.h"
#include "clrversion.h"
#include "typestring.h"

#ifdef FEATURE_COMINTEROP
#include "comcallablewrapper.h"
#include "runtimecallablewrapper.h"
#endif

#include "eventtracepriv.h"

//---------------------------------------------------------------------------------------
// BulkComLogger: Batches up and logs RCW and CCW
//---------------------------------------------------------------------------------------

BulkComLogger::BulkComLogger(BulkTypeEventLogger *typeLogger)
    : m_currRcw(0), m_currCcw(0), m_typeLogger(typeLogger), m_etwRcwData(0), m_etwCcwData(0), m_enumResult(0)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_etwRcwData = new EventRCWEntry[kMaxRcwCount];
    m_etwCcwData = new EventCCWEntry[kMaxCcwCount];
}

BulkComLogger::~BulkComLogger()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    FireBulkComEvent();

    if (m_etwRcwData)
        delete [] m_etwRcwData;

    if (m_etwCcwData)
        delete [] m_etwCcwData;

    if (m_enumResult)
    {
        CCWEnumerationEntry *curr = m_enumResult;
        while (curr)
        {
            CCWEnumerationEntry *next = curr->Next;
            delete curr;
            curr = next;
        }
    }
}

void BulkComLogger::FireBulkComEvent()
{
    WRAPPER_NO_CONTRACT;

    FlushRcw();
    FlushCcw();
}

void BulkComLogger::WriteRcw(RCW *pRcw, Object *obj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pRcw != NULL);
        PRECONDITION(obj != NULL);
    }
    CONTRACTL_END;

    _ASSERTE(m_currRcw < kMaxRcwCount);

#ifdef FEATURE_COMINTEROP
    TypeHandle typeHandle = obj->GetGCSafeTypeHandleIfPossible();
    if (typeHandle == NULL)
    {
        return;
    }
    EventRCWEntry &rcw = m_etwRcwData[m_currRcw];
    rcw.ObjectID = (ULONGLONG)obj;
    rcw.TypeID = (ULONGLONG)typeHandle.AsTAddr();
    rcw.IUnk = (ULONGLONG)pRcw->GetIUnknown_NoAddRef();
    rcw.VTable = (ULONGLONG)pRcw->GetVTablePtr();
    rcw.RefCount = pRcw->GetRefCount();
    rcw.Flags = 0;

    if (++m_currRcw >= kMaxRcwCount)
        FlushRcw();
#endif
}

void BulkComLogger::FlushRcw()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(m_currRcw <= kMaxRcwCount);

    if (m_currRcw == 0)
        return;

    if (m_typeLogger)
    {
        for (int i = 0; i < m_currRcw; ++i)
            ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(m_typeLogger, m_etwRcwData[i].TypeID, ETW::TypeSystemLog::kTypeLogBehaviorTakeLockAndLogIfFirstTime);
    }

    unsigned short instance = GetClrInstanceId();

#if !defined(HOST_UNIX)
    EVENT_DATA_DESCRIPTOR eventData[3];
    EventDataDescCreate(&eventData[0], &m_currRcw, sizeof(const unsigned int));
    EventDataDescCreate(&eventData[1], &instance, sizeof(const unsigned short));
    EventDataDescCreate(&eventData[2], m_etwRcwData, sizeof(EventRCWEntry) * m_currRcw);

    ULONG result = EventWrite(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRCW, ARRAY_SIZE(eventData), eventData);
#else
    ULONG result = FireEtXplatGCBulkRCW(m_currRcw, instance, sizeof(EventRCWEntry) * m_currRcw, m_etwRcwData);
#endif // !defined(HOST_UNIX)
    result |= EventPipeWriteEventGCBulkRCW(m_currRcw, instance, sizeof(EventRCWEntry) * m_currRcw, m_etwRcwData);

    _ASSERTE(result == ERROR_SUCCESS);

    m_currRcw = 0;
}

void BulkComLogger::WriteCcw(ComCallWrapper *pCcw, Object **handle, Object *obj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(handle != NULL);
        PRECONDITION(obj != NULL);
    }
    CONTRACTL_END;

    _ASSERTE(m_currCcw < kMaxCcwCount);

#ifdef FEATURE_COMINTEROP
    IUnknown *iUnk = NULL;
    int refCount = 0;
    ULONG flags = 0;

    if (pCcw)
    {
        iUnk = pCcw->GetOuter();
        if (iUnk == NULL)
            iUnk = pCcw->GetBasicIP(true);

        refCount = pCcw->GetRefCount();

        if (pCcw->IsWrapperActive())
            flags |= EventCCWEntry::Strong;
    }

    TypeHandle typeHandle = obj->GetGCSafeTypeHandleIfPossible();
    if (typeHandle == NULL)
    {
        return;
    }

    EventCCWEntry &ccw = m_etwCcwData[m_currCcw++];
    ccw.RootID = (ULONGLONG)handle;
    ccw.ObjectID = (ULONGLONG)obj;
    ccw.TypeID = (ULONGLONG)typeHandle.AsTAddr();
    ccw.IUnk = (ULONGLONG)iUnk;
    ccw.RefCount = refCount;
    ccw.JupiterRefCount = 0;
    ccw.Flags = flags;

    if (m_currCcw >= kMaxCcwCount)
        FlushCcw();
#endif
}

void BulkComLogger::FlushCcw()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(m_currCcw <= kMaxCcwCount);

    if (m_currCcw == 0)
        return;

    if (m_typeLogger)
    {
        for (int i = 0; i < m_currCcw; ++i)
            ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(m_typeLogger, m_etwCcwData[i].TypeID, ETW::TypeSystemLog::kTypeLogBehaviorTakeLockAndLogIfFirstTime);
    }

    unsigned short instance = GetClrInstanceId();

#if !defined(HOST_UNIX)
    EVENT_DATA_DESCRIPTOR eventData[3];
    EventDataDescCreate(&eventData[0], &m_currCcw, sizeof(const unsigned int));
    EventDataDescCreate(&eventData[1], &instance, sizeof(const unsigned short));
    EventDataDescCreate(&eventData[2], m_etwCcwData, sizeof(EventCCWEntry) * m_currCcw);

    ULONG result = EventWrite(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRootCCW, ARRAY_SIZE(eventData), eventData);
#else
    ULONG result = FireEtXplatGCBulkRootCCW(m_currCcw, instance, sizeof(EventCCWEntry) * m_currCcw, m_etwCcwData);
#endif //!defined(HOST_UNIX)
    result |= EventPipeWriteEventGCBulkRootCCW(m_currCcw, instance, sizeof(EventCCWEntry) * m_currCcw, m_etwCcwData);

    _ASSERTE(result == ERROR_SUCCESS);

    m_currCcw = 0;
}

void BulkComLogger::LogAllComObjects()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    SyncBlockCache *cache = SyncBlockCache::GetSyncBlockCache();
    if (cache == NULL)
        return;

    int count = cache->GetTableEntryCount();
    SyncTableEntry *table = SyncTableEntry::GetSyncTableEntry();

    for (int i = 0; i < count; ++i)
    {
        SyncTableEntry &entry = table[i];
        Object *obj = entry.m_Object.Load();
        if (obj && entry.m_SyncBlock)
        {
            InteropSyncBlockInfo *interop = entry.m_SyncBlock->GetInteropInfoNoCreate();
            if (interop)
            {
                RCW *rcw = interop->GetRawRCW();
                if (rcw)
                    WriteRcw(rcw, obj);
            }
        }
    }

    // We need to do work in HandleWalkCallback which may trigger a GC.  We cannot do this while
    // enumerating the handle table.  Instead, we will build a list of RefCount handles we found
    // during the handle table enumeration first (m_enumResult) during this enumeration:
    GCHandleUtilities::GetGCHandleManager()->TraceRefCountedHandles(BulkComLogger::HandleWalkCallback, uintptr_t(this), 0);

    // Now that we have all of the object handles, we will walk all of the handles and write the
    // etw events.
    for (CCWEnumerationEntry *curr = m_enumResult; curr; curr = curr->Next)
    {
        for (int i = 0; i < curr->Count; ++i)
        {
            Object **handle = curr->Handles[i];

            Object *obj = NULL;
            if (handle == NULL || (obj = *handle) == 0)
                return;

            ObjHeader *header = obj->GetHeader();
            _ASSERTE(header != NULL);

            // We can catch the refcount handle too early where we don't have a CCW, WriteCCW
            // handles this case.  We still report the refcount handle without the CCW data.
            ComCallWrapper *ccw = NULL;

            // Checking the index ensures that the syncblock is already created.  The
            // PassiveGetSyncBlock function does not check bounds, so we have to be sure
            // the SyncBlock was already created.
            int index = header->GetHeaderSyncBlockIndex();
            if (index > 0)
            {
                SyncBlock *syncBlk = header->PassiveGetSyncBlock();
                InteropSyncBlockInfo *interop = syncBlk->GetInteropInfoNoCreate();
                if (interop)
                    ccw = interop->GetCCW();
            }

            WriteCcw(ccw, handle, obj);
        }
    }

#endif

}

void BulkComLogger::HandleWalkCallback(Object **handle, uintptr_t *pExtraInfo, uintptr_t param1, uintptr_t param2)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(param1 != NULL);   // Should be the "this" pointer for BulkComLogger.
        PRECONDITION(param2 == 0);      // This is set by Ref_TraceRefCountHandles.
    }
    CONTRACTL_END;

    // Simple sanity check to ensure the parameters are what we expect them to be.
    _ASSERTE(param2 == 0);

    if (handle != NULL)
        ((BulkComLogger*)param1)->AddCcwHandle(handle);
}



// Used during CCW enumeration to keep track of all object handles which point to a CCW.
void BulkComLogger::AddCcwHandle(Object **handle)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(handle != NULL);
    }
    CONTRACTL_END;

    if (m_enumResult == NULL)
        m_enumResult = new CCWEnumerationEntry;

    CCWEnumerationEntry *curr = m_enumResult;
    while (curr->Next)
        curr = curr->Next;

    if (curr->Count == ARRAY_SIZE(curr->Handles))
    {
        curr->Next = new CCWEnumerationEntry;
        curr = curr->Next;
    }

    curr->Handles[curr->Count++] = handle;
}




//---------------------------------------------------------------------------------------
// BulkStaticsLogger: Batches up and logs static variable roots
//---------------------------------------------------------------------------------------



#include "domainassembly.h"

BulkStaticsLogger::BulkStaticsLogger(BulkTypeEventLogger *typeLogger)
    : m_buffer(0), m_used(0), m_count(0), m_domain(0), m_typeLogger(typeLogger)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_buffer = new BYTE[kMaxBytesValues];
}

BulkStaticsLogger::~BulkStaticsLogger()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_used > 0)
        FireBulkStaticsEvent();

    if (m_buffer)
        delete[] m_buffer;
}

void BulkStaticsLogger::FireBulkStaticsEvent()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_used <= 0 || m_count <= 0)
        return;

    _ASSERTE(m_domain != NULL);

    unsigned short instance = GetClrInstanceId();
    unsigned __int64 appDomain = (unsigned __int64)m_domain;

#if !defined(HOST_UNIX)
    EVENT_DATA_DESCRIPTOR eventData[4];
    EventDataDescCreate(&eventData[0], &m_count, sizeof(const unsigned int)  );
    EventDataDescCreate(&eventData[1], &appDomain, sizeof(unsigned __int64)  );
    EventDataDescCreate(&eventData[2], &instance, sizeof(const unsigned short)  );
    EventDataDescCreate(&eventData[3], m_buffer, m_used);

    ULONG result = EventWrite(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRootStaticVar, ARRAY_SIZE(eventData), eventData);
#else
    ULONG result = FireEtXplatGCBulkRootStaticVar(m_count, appDomain, instance, m_used, m_buffer);
#endif //!defined(HOST_UNIX)
    result |= EventPipeWriteEventGCBulkRootStaticVar(m_count, appDomain, instance, m_used, m_buffer);

    _ASSERTE(result == ERROR_SUCCESS);

    m_used = 0;
    m_count = 0;
}

void BulkStaticsLogger::WriteEntry(AppDomain *domain, Object **address, Object *obj, FieldDesc *fieldDesc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(domain != NULL);
        PRECONDITION(address != NULL);
        PRECONDITION(obj != NULL);
        PRECONDITION(fieldDesc != NULL);
    }
    CONTRACTL_END;

    // Each bulk statics event is for one AppDomain.  If we are now inspecting a new domain,
    // we need to flush the built up events now.
    if (m_domain != domain)
    {
        if (m_domain != NULL)
            FireBulkStaticsEvent();

        m_domain = domain;
    }

    TypeHandle typeHandle = obj->GetGCSafeTypeHandleIfPossible();
    if (typeHandle == NULL)
    {
        return;
    }
    ULONGLONG th = (ULONGLONG)typeHandle.AsTAddr();
    ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(m_typeLogger, th, ETW::TypeSystemLog::kTypeLogBehaviorTakeLockAndLogIfFirstTime);

    // We should have at least 512 characters remaining in the buffer here.
    int remaining = kMaxBytesValues - m_used;
    _ASSERTE(kMaxBytesValues - m_used > 512);

    int len = EventStaticEntry::WriteEntry(m_buffer + m_used, remaining, (ULONGLONG)address,
                                           (ULONGLONG)obj, th, 0, fieldDesc);

    // 512 bytes was not enough buffer?  This shouldn't happen, so we'll skip emitting the
    // event on error.
    if (len > 0)
    {
        m_used += len;
        m_count++;
    }

    // When we are close to running out of buffer, emit the event.
    if (kMaxBytesValues - m_used < 512)
        FireBulkStaticsEvent();
}

void BulkStaticsLogger::LogAllStatics()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    {
        // TODO: This code does not appear to find all generic instantiations of types, and thus does not log ALL statics
        AppDomain *domain = ::GetAppDomain(); // There is only 1 AppDomain, so no iterator here.

        AppDomain::AssemblyIterator assemblyIter = domain->IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded|kIncludeExecution));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
        while (assemblyIter.Next(pDomainAssembly.This()))
        {
            // Make sure the assembly is loaded.
            if (!pDomainAssembly->IsLoaded())
                continue;

            CollectibleAssemblyHolder<Assembly *> pAssembly = pDomainAssembly->GetAssembly();
            // Get the domain module from the module/appdomain pair.
            Module *module = pDomainAssembly->GetModule();
            if (module == NULL)
                continue;

            DomainAssembly *domainAssembly = module->GetDomainAssembly();
            if (domainAssembly == NULL)
                continue;

            // Ensure the module has fully loaded.
            if (!domainAssembly->IsActive())
                continue;

            // Now iterate all types with
            LookupMap<PTR_MethodTable>::Iterator mtIter = module->EnumerateTypeDefs();
            while (mtIter.Next())
            {
                // I don't think mt can be null here, but the dac does a null check...
                // IsFullyLoaded should be equivalent to 'GetLoadLevel() == CLASS_LOADED'
                MethodTable *mt = mtIter.GetElement();
                if (mt == NULL || !mt->IsFullyLoaded())
                    continue;

                EEClass *cls = mt->GetClass();
                _ASSERTE(cls != NULL);

                if (cls->GetNumStaticFields() <= 0)
                    continue;

                ApproxFieldDescIterator fieldIter(mt, ApproxFieldDescIterator::STATIC_FIELDS);
                for (FieldDesc *field = fieldIter.Next(); field != NULL; field = fieldIter.Next())
                {
                    // Don't want thread local
                    _ASSERTE(field->IsStatic());
                    if (field->IsSpecialStatic() || field->IsEnCNew())
                        continue;

                    // Static valuetype values are boxed.
                    CorElementType fieldType = field->GetFieldType();
                    if (fieldType != ELEMENT_TYPE_CLASS && fieldType != ELEMENT_TYPE_VALUETYPE)
                        continue;

                    BYTE *base = field->GetBase();
                    if (base == NULL)
                        continue;

                    Object **address = (Object**)field->GetStaticAddressHandle(base);
                    Object *obj = NULL;
                    if (address == NULL || ((obj = *address) == NULL))
                        continue;

                    WriteEntry(domain, address, *address, field);
                } // foreach static field
            }
        } // foreach domain assembly
    } // foreach AppDomain
} // BulkStaticsLogger::LogAllStatics


//---------------------------------------------------------------------------------------
// BulkTypeValue / BulkTypeEventLogger: These take care of batching up types so they can
// be logged via ETW in bulk
//---------------------------------------------------------------------------------------

BulkTypeValue::BulkTypeValue()
    : cTypeParameters(0)
#ifdef FEATURE_NATIVEAOT
    , ullSingleTypeParameter(0)
#else // FEATURE_NATIVEAOT
    , sName()
#endif // FEATURE_NATIVEAOT
    , rgTypeParameters()
{
    LIMITED_METHOD_CONTRACT;
    ZeroMemory(&fixedSizedData, sizeof(fixedSizedData));
}

//---------------------------------------------------------------------------------------
//
// Clears a BulkTypeValue so it can be reused after the buffer is flushed to ETW
//

void BulkTypeValue::Clear()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ZeroMemory(&fixedSizedData, sizeof(fixedSizedData));
    cTypeParameters = 0;
#ifdef FEATURE_NATIVEAOT
    ullSingleTypeParameter = 0;
    rgTypeParameters.Release();
#else // FEATURE_NATIVEAOT
    sName.Clear();
    rgTypeParameters.Clear();
#endif // FEATURE_NATIVEAOT
}

//---------------------------------------------------------------------------------------
//
// Fire an ETW event for all the types we batched so far, and then reset our state
// so we can start batching new types at the beginning of the array.
//

void BulkTypeEventLogger::FireBulkTypeEvent()
{
    LIMITED_METHOD_CONTRACT;

    if (m_nBulkTypeValueCount == 0)
    {
        // No types were batched up, so nothing to send
        return;
    }
    UINT16 nClrInstanceID = GetClrInstanceId();

    if(m_pBulkTypeEventBuffer == NULL)
    {
        // The buffer could not be allocated when this object was created, so bail.
        return;
    }

    UINT iSize = 0;

    for (int iTypeData = 0; iTypeData < m_nBulkTypeValueCount; iTypeData++)
    {
        BulkTypeValue& target = m_rgBulkTypeValues[iTypeData];

        // Do fixed-size data as one bulk copy
        memcpy(
                m_pBulkTypeEventBuffer + iSize,
                &(target.fixedSizedData),
                sizeof(target.fixedSizedData));
        iSize += sizeof(target.fixedSizedData);

        // Do var-sized data individually per field

        LPCWSTR wszName = target.sName.GetUnicode();
        if (wszName == NULL)
        {
            m_pBulkTypeEventBuffer[iSize++] = 0;
            m_pBulkTypeEventBuffer[iSize++] = 0;
        }
        else
        {
            UINT nameSize = (target.sName.GetCount() + 1) * sizeof(WCHAR);
            memcpy(m_pBulkTypeEventBuffer + iSize, wszName, nameSize);
            iSize += nameSize;
        }

        // Type parameter count
        ULONG params = target.rgTypeParameters.GetCount();

        ULONG *ptrInt = (ULONG*)(m_pBulkTypeEventBuffer + iSize);
        *ptrInt = params;
        iSize += 4;

        target.cTypeParameters = params;

        // Type parameter array
        if (target.cTypeParameters > 0)
        {
            memcpy(m_pBulkTypeEventBuffer + iSize, target.rgTypeParameters.GetElements(), sizeof(ULONGLONG) * target.cTypeParameters);
            iSize += sizeof(ULONGLONG) * target.cTypeParameters;
        }
    }

    FireEtwBulkType(m_nBulkTypeValueCount, GetClrInstanceId(), iSize, m_pBulkTypeEventBuffer);

    // Reset state
    m_nBulkTypeValueCount = 0;
    m_nBulkTypeValueByteCount = 0;
}

#ifndef FEATURE_NATIVEAOT

//---------------------------------------------------------------------------------------
//
// Batches a single type into the array, flushing the array to ETW if it fills up. Most
// interaction with the type system (to analyze the type) is done here. This does not
// recursively batch up any parameter types (for arrays or generics), but does add their
// TypeHandles to the rgTypeParameters array. LogTypeAndParameters is responsible for
// initiating any recursive calls to deal with type parameters.
//
// Arguments:
//      th - TypeHandle to batch
//
// Return Value:
//      Index into array of where this type got batched. -1 if there was a failure.
//

int BulkTypeEventLogger::LogSingleType(TypeHandle th)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;  // some of the type system stuff can take locks
    }
    CONTRACTL_END;

    // If there's no room for another type, flush what we've got
    if (m_nBulkTypeValueCount == ARRAY_SIZE(m_rgBulkTypeValues))
    {
        FireBulkTypeEvent();
    }

    _ASSERTE(m_nBulkTypeValueCount < (int)ARRAY_SIZE(m_rgBulkTypeValues));

    BulkTypeValue * pVal = &m_rgBulkTypeValues[m_nBulkTypeValueCount];

    // Clear out pVal before filling it out (array elements can get reused if there
    // are enough types that we need to flush to multiple events).  Clearing the
    // contained SBuffer can throw, so deal with exceptions
    BOOL fSucceeded = FALSE;
    EX_TRY
    {
        pVal->Clear();
        fSucceeded = TRUE;
    }
    EX_CATCH
    {
        fSucceeded = FALSE;
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    if (!fSucceeded)
        return -1;

    pVal->fixedSizedData.TypeID = (ULONGLONG) th.AsTAddr();
    pVal->fixedSizedData.ModuleID = (ULONGLONG) (TADDR) th.GetModule();
    pVal->fixedSizedData.TypeNameID = (th.GetMethodTable() == NULL) ? 0 : th.GetCl();
    pVal->fixedSizedData.Flags = 0;
    pVal->fixedSizedData.CorElementType = (BYTE) th.GetInternalCorElementType();

    if (th.IsArray())
    {
        // Normal typedesc array
        pVal->fixedSizedData.Flags |= kEtwTypeFlagsArray;
        if (pVal->fixedSizedData.CorElementType == ELEMENT_TYPE_ARRAY)
        {
            // Multidimensional arrays set the rank bits, SzArrays do not set the rank bits
            unsigned rank = th.GetRank();
            if (rank < kEtwTypeFlagsArrayRankMax)
            {
                // Only ranks less than kEtwTypeFlagsArrayRankMax are supported.
                // Fortunately kEtwTypeFlagsArrayRankMax should be greater than the
                // number of ranks the type loader will support
                rank <<= kEtwTypeFlagsArrayRankShift;
                _ASSERTE((rank & kEtwTypeFlagsArrayRankMask) == rank);
                pVal->fixedSizedData.Flags |= rank;
            }
        }
        // Fetch TypeHandle of array elements
        fSucceeded = FALSE;
        EX_TRY
        {
            pVal->rgTypeParameters.Append((ULONGLONG) th.GetArrayElementTypeHandle().AsTAddr());
            fSucceeded = TRUE;
        }
        EX_CATCH
        {
            fSucceeded = FALSE;
        }
        EX_END_CATCH(RethrowTerminalExceptions);
        if (!fSucceeded)
            return -1;
    }
    else if (th.IsTypeDesc())
    {
        // Non-array Typedescs
        PTR_TypeDesc pTypeDesc = th.AsTypeDesc();
        if (pTypeDesc->HasTypeParam())
        {
            fSucceeded = FALSE;
            EX_TRY
            {
                pVal->rgTypeParameters.Append((ULONGLONG) pTypeDesc->GetTypeParam().AsTAddr());
                fSucceeded = TRUE;
            }
            EX_CATCH
            {
                fSucceeded = FALSE;
            }
            EX_END_CATCH(RethrowTerminalExceptions);
            if (!fSucceeded)
                return -1;
        }
    }
    else
    {
        // Non-array MethodTable

        PTR_MethodTable pMT = th.AsMethodTable();

        // Make CorElementType more specific if this is a string MT
        if (pMT->IsString())
        {
            pVal->fixedSizedData.CorElementType = ELEMENT_TYPE_STRING;
        }
        else if (pMT->IsObjectClass())
        {
            pVal->fixedSizedData.CorElementType = ELEMENT_TYPE_OBJECT;
        }

        // Generic arguments
        DWORD cTypeParameters = pMT->GetNumGenericArgs();
        if (cTypeParameters > 0)
        {
            Instantiation inst = pMT->GetInstantiation();
            fSucceeded = FALSE;
            EX_TRY
            {
                for (DWORD i=0; i < cTypeParameters; i++)
                {
                    pVal->rgTypeParameters.Append((ULONGLONG) inst[i].AsTAddr());
                }
                fSucceeded = TRUE;
            }
            EX_CATCH
            {
                fSucceeded = FALSE;
            }
            EX_END_CATCH(RethrowTerminalExceptions);
            if (!fSucceeded)
                return -1;
        }

        if (pMT->HasFinalizer())
        {
            pVal->fixedSizedData.Flags |= kEtwTypeFlagsFinalizable;
        }
        if (pMT->IsDelegate())
        {
            pVal->fixedSizedData.Flags |= kEtwTypeFlagsDelegate;
        }
        if (pMT->IsComObjectType())
        {
            pVal->fixedSizedData.Flags |= kEtwTypeFlagsExternallyImplementedCOMObject;
        }
    }

    // If the profiler wants it, construct a name.  Always normalize the string (even if
    // type names are not requested) so that calls to sName.GetCount() can't throw
    EX_TRY
    {
        if (ETW_TRACING_CATEGORY_ENABLED(
            MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
            TRACE_LEVEL_INFORMATION,
            CLR_GCHEAPANDTYPENAMES_KEYWORD))
        {
            th.GetName(pVal->sName);
        }
        pVal->sName.Normalize();
    }
    EX_CATCH
    {
        // If this failed, the name remains empty, which is ok; the event just
        // won't have a name in it.
        pVal->sName.Clear();
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    // Now that we know the full size of this type's data, see if it fits in our
    // batch or whether we need to flush

    int cbVal = pVal->GetByteCountInEvent();
    if (cbVal > kMaxBytesTypeValues)
    {
        pVal->sName.Clear();
        cbVal = pVal->GetByteCountInEvent();

        if (cbVal > kMaxBytesTypeValues)
        {
            // This type is apparently so huge, it's too big to squeeze into an event, even
            // if it were the only type batched in the whole event.  Bail
            _ASSERTE(!"Type too big to log via ETW");
            return -1;
        }
    }

    if (m_nBulkTypeValueByteCount + cbVal > kMaxBytesTypeValues)
    {
        // Although this type fits into the array, its size is so big that the entire
        // array can't be logged via ETW. So flush the array, and start over by
        // calling ourselves--this refetches the type info and puts it at the
        // beginning of the array.  Since we know this type is small enough to be
        // batched into an event on its own, this recursive call will not try to
        // call itself again.
        FireBulkTypeEvent();
        return LogSingleType(th);
    }

    // The type fits into the batch, so update our state
    m_nBulkTypeValueCount++;
    m_nBulkTypeValueByteCount += cbVal;
    return m_nBulkTypeValueCount - 1;       // Index of type we just added
}

//---------------------------------------------------------------------------------------
//
// High-level method to batch a type and (recursively) its type parameters, flushing to
// ETW as needed.  This is called by (static)
// ETW::TypeSystemLog::LogTypeAndParametersIfNecessary, which is what clients use to log
// type events
//
// Arguments:
//      * thAsAddr - Type to batch
//      * typeLogBehavior - Reminder of whether the type system log lock is held
//          (useful if we need to recursively call back into TypeSystemLog), and whether
//          we even care to check if the type was already logged
//

void BulkTypeEventLogger::LogTypeAndParameters(ULONGLONG thAsAddr, ETW::TypeSystemLog::TypeLogBehavior typeLogBehavior)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;  // LogSingleType can take locks
    }
    CONTRACTL_END;

    TypeHandle th = TypeHandle::FromTAddr((TADDR) thAsAddr);

    // Batch up this type.  This grabs useful info about the type, including any
    // type parameters it may have, and sticks it in m_rgBulkTypeValues
    int iBulkTypeEventData = LogSingleType(th);
    if (iBulkTypeEventData == -1)
    {
        // There was a failure trying to log the type, so don't bother with its type
        // parameters
        return;
    }

    // Look at the type info we just batched, so we can get the type parameters
    BulkTypeValue * pVal = &m_rgBulkTypeValues[iBulkTypeEventData];

    // We're about to recursively call ourselves for the type parameters, so make a
    // local copy of their type handles first (else, as we log them we could flush
    // and clear out m_rgBulkTypeValues, thus trashing pVal)

    StackSArray<ULONGLONG> rgTypeParameters;
    DWORD cParams = pVal->rgTypeParameters.GetCount();

    BOOL fSucceeded = FALSE;
    EX_TRY
    {
        for (COUNT_T i = 0; i < cParams; i++)
        {
            rgTypeParameters.Append(pVal->rgTypeParameters[i]);
        }
        fSucceeded = TRUE;
    }
    EX_CATCH
    {
        fSucceeded = FALSE;
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    if (!fSucceeded)
        return;

    // Before we recurse, adjust the special-cased type-log behavior that allows a
    // top-level type to be logged without lookup, but still requires lookups to avoid
    // dupes of type parameters
    if (typeLogBehavior == ETW::TypeSystemLog::kTypeLogBehaviorAlwaysLogTopLevelType)
        typeLogBehavior = ETW::TypeSystemLog::kTypeLogBehaviorTakeLockAndLogIfFirstTime;

    // Recursively log any referenced parameter types
    for (COUNT_T i=0; i < cParams; i++)
    {
        ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(this, rgTypeParameters[i], typeLogBehavior);
    }
}

#endif // FEATURE_NATIVEAOT
