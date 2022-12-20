// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eventtracepriv.h
//
// Contains some private definitions used by eventrace.cpp, but that aren't needed by
// clients of eventtrace.cpp, and thus don't belong in eventtrace.h. Also, since
// inclusions of this file are tightly controlled (basically just by eventtrace.cpp), we
// can assume some classes are defined that aren't necessarily defined when eventtrace.h
// is #included (e.g., StackSString and StackSArray).
//

//

//
// ============================================================================

#ifndef __EVENTTRACEPRIV_H__
#define __EVENTTRACEPRIV_H__

// ETW has a limitation of 64K for TOTAL event Size, however there is overhead associated with
// the event headers.   It is unclear exactly how much that is, but 1K should be sufficiently
// far away to avoid problems without sacrificing the perf of bulk processing.
const UINT cbMaxEtwEvent = 63 * 1024;

//---------------------------------------------------------------------------------------
// C++ copies of ETW structures
//---------------------------------------------------------------------------------------

// !!!!!!! NOTE !!!!!!!!
// The EventStruct* structs are described in the ETW manifest event templates, and the
// LAYOUT MUST MATCH THE MANIFEST EXACTLY!
// !!!!!!! NOTE !!!!!!!!

#pragma pack(push, 1)

struct EventStructGCBulkRootEdgeValue
{
    LPVOID RootedNodeAddress;
    BYTE GCRootKind;
    DWORD GCRootFlag;
    LPVOID GCRootID;
};

struct EventStructGCBulkRootConditionalWeakTableElementEdgeValue
{
    LPVOID GCKeyNodeID;
    LPVOID GCValueNodeID;
    LPVOID GCRootID;
};

struct EventStructGCBulkNodeValue
{
    LPVOID Address;
    ULONGLONG Size;
    ULONGLONG TypeID;
    ULONGLONG EdgeCount;
};

struct EventStructGCBulkEdgeValue
{
    LPVOID Value;
    ULONG ReferencingFieldID;
};

struct EventStructGCBulkSurvivingObjectRangesValue
{
    LPVOID RangeBase;
    ULONGLONG RangeLength;
};

struct EventStructGCBulkMovedObjectRangesValue
{
    LPVOID OldRangeBase;
    LPVOID NewRangeBase;
    ULONGLONG RangeLength;
};

struct EventStructStaticBulkFixedSizeData
{
    ULONGLONG TypeID;
    ULONGLONG Address;
    ULONGLONG Value;
    ULONG Flags;
};

// This only contains the fixed-size data at the top of each struct in
// the bulk type event.  These fields must still match exactly the initial
// fields of the struct described in the manifest.
struct EventStructBulkTypeFixedSizedData
{
    ULONGLONG TypeID;
    ULONGLONG ModuleID;
    ULONG TypeNameID;
    ULONG Flags;
    BYTE CorElementType;
};

struct EventStaticEntry
{
    ULONGLONG GCRootID;
    ULONGLONG ObjectID;
    ULONGLONG TypeID;
    ULONG Flags;
    WCHAR Name[0];

    // Writes one EventStaticEntry to the buffer specified by ptr.  Since we don't actually know how large the event will be,
    // this write may fail if the remaining buffer is not large enough.  This function returns the number of bytes written
    // on success (return is >= 0), and -1 on failure.  If we return -1, the caller is expected to flush the current buffer
    // and try again.
    static int WriteEntry(BYTE *ptr, int sizeRemaining, ULONGLONG addr, ULONGLONG obj, ULONGLONG typeId, ULONG flags, FieldDesc *fieldDesc)
    {
        WRAPPER_NO_CONTRACT;

        // sizeRemaining must be larger than the structure + 1 wchar for the struct and
        // null terminator of Name.  We will do a better bounds check when we know the
        // size of the field name.
        if (sizeRemaining < (int)(sizeof(EventStaticEntry) + sizeof(WCHAR)))
            return -1;

        // The location in the structure to write to.  We won't actually write here unless we have sufficient buffer.
        WCHAR *name = (WCHAR *)(ptr + offsetof(EventStaticEntry, Name));
        int len = 0;

        LPCUTF8 utf8Name = 0;
        if (SUCCEEDED(fieldDesc->GetName_NoThrow(&utf8Name)))
        {
            len = MultiByteToWideChar(CP_ACP, 0, utf8Name, -1, name, sizeRemaining - sizeof(EventStaticEntry));
            if (len <= 0)
            {
                // We will ignore corrupted/bad metadata here and only emit names for fields which are
                // up to 255 characters (and also don't fit in the buffer).
                if (GetLastError() == ERROR_INSUFFICIENT_BUFFER && sizeRemaining < 256)
                    return -1; // nothing written, insufficient buffer.  Flush and try again.

                // If the name is larger than 255 or we have some other error converting the string,
                // just emit an empty string.
                len = 1;
                name[0] = 0;
            }
        }
        else
        {
            // Couldn't get the name for some reason, just emit an empty string.
            len = 1;
            name[0] = 0;
        }

        // At this point we should have written something to the name buffer.
        _ASSERTE(len > 0);

        // At this point we've written the field name (even if it's just an empty string).
        // Write the rest of the fields to the buffer and return the total size.
        EventStaticEntry *entry = (EventStaticEntry*)ptr;
        entry->GCRootID = addr;
        entry->ObjectID = obj;
        entry->TypeID = typeId;
        entry->Flags = flags;

        return sizeof(EventStaticEntry) + len * sizeof(WCHAR);
    }
};

struct EventRCWEntry
{
    ULONGLONG ObjectID;
    ULONGLONG TypeID;
    ULONGLONG IUnk;
    ULONGLONG VTable;
    ULONG RefCount;
    ULONG Flags;
};


struct EventCCWEntry
{
    enum CCWFlags
    {
        Strong = 0x1,
        Pegged = 0x2
    };

    ULONGLONG RootID;
    ULONGLONG ObjectID;
    ULONGLONG TypeID;
    ULONGLONG IUnk;
    ULONG RefCount;
    ULONG JupiterRefCount;
    ULONG Flags;
};

#pragma pack(pop)

// Represents one instance of the Value struct inside a single BulkType event
class BulkTypeValue
{
public:
    BulkTypeValue();
    void Clear();

    // How many bytes will this BulkTypeValue take up when written into the actual ETW
    // event?
    int GetByteCountInEvent()
    {
        return
            sizeof(fixedSizedData) +
            sizeof(cTypeParameters) +
#ifdef FEATURE_NATIVEAOT
            sizeof(WCHAR) +                                 // No name in event, so just the null terminator
            cTypeParameters * sizeof(ULONGLONG);            // Type parameters
#else
            (sName.GetCount() + 1) * sizeof(WCHAR) +        // Size of name, including null terminator
            rgTypeParameters.GetCount() * sizeof(ULONGLONG);// Type parameters
#endif
    }

    EventStructBulkTypeFixedSizedData fixedSizedData;

    // Below are the remainder of each struct in the bulk type event (i.e., the
    // variable-sized data). The var-sized fields are copied into the event individually
    // (not directly), so they don't need to have the same layout as in the ETW manifest

    // This is really a denorm of the size already stored in rgTypeParameters, but we
    // need a persistent place to stash this away so EventDataDescCreate & EventWrite
    // have a reliable place to copy it from.  This is filled in at the last minute,
    // when sending the event.
    ULONG cTypeParameters;

#ifdef FEATURE_NATIVEAOT
    // If > 1 type parameter, this is an array of their EEType*'s
    NewArrayHolder<ULONGLONG> rgTypeParameters;

    // If exactly one type parameter, this is its EEType*.  (If != 1 type parameter,
    // this is 0.)
    ULONGLONG ullSingleTypeParameter;
#else   // FEATURE_NATIVEAOT
    StackSString sName;
    StackSArray<ULONGLONG> rgTypeParameters;
#endif // FEATURE_NATIVEAOT
};

// Encapsulates all the type event batching we need to do. This is used by
// ETW::TypeSystemLog, which calls LogTypeAndParameters for each type to be logged.
// BulkTypeEventLogger will batch each type and its generic type parameters, and flush to
// ETW as necessary. ETW::TypeSystemLog also calls FireBulkTypeEvent directly to force a
// flush (e.g., once at end of GC heap traversal, or on each object allocation).
class BulkTypeEventLogger
{
private:

    // The maximum event size, and the size of the buffer that we allocate to hold the event contents.
    static const size_t kSizeOfEventBuffer = 65536;

    // Estimate of how many bytes we can squeeze in the event data for the value struct
    // array.  (Intentionally overestimate the size of the non-array parts to keep it safe.)
    static const int kMaxBytesTypeValues = (cbMaxEtwEvent - 0x30);

    // Estimate of how many type value elements we can put into the struct array, while
    // staying under the ETW event size limit. Note that this is impossible to calculate
    // perfectly, since each element of the struct array has variable size.
    //
    // In addition to the byte-size limit per event, Windows always forces on us a
    // max-number-of-descriptors per event, which in the case of BulkType, will kick in
    // far sooner. There's a max number of 128 descriptors allowed per event. 2 are used
    // for Count + ClrInstanceID. Then 4 per batched value. (Might actually be 3 if there
    // are no type parameters to log, but let's overestimate at 4 per value).
    static const int kMaxCountTypeValues = (128 - 2) / 4;
    // Note: This results in a relatively small batch (about 31 types per event). We
    // could increase this substantially by creating a single, contiguous buffer, which
    // would let us max out the number of type values to batch by allowing the byte-size
    // limit to kick in before the max-descriptor limit. We could esimate that as
    // follows:
    //
    //     static const int kMaxCountTypeValues = kMaxBytesTypeValues /
    //        (sizeof(EventStructBulkTypeFixedSizedData) +
    //         200 * sizeof(WCHAR) +       // Assume 199 + 1 terminating-NULL character in type name
    //         sizeof(UINT) +              // Type parameter count
    //         10 * sizeof(ULONGLONG));    // Assume 10 type parameters
    //
    // The downside, though, is that we would have to do a lot more copying to fill out
    // that buffer before sending the event. It's unclear that increasing the batch size
    // is enough of a win to offset all the extra buffer copying. So for now, we'll keep
    // the batch size low and avoid extra copying.

    // How many types have we batched?
    int m_nBulkTypeValueCount;

    // What is the byte size of all the types we've batched?
    int m_nBulkTypeValueByteCount;

    // List of types we've batched.
    BulkTypeValue m_rgBulkTypeValues[kMaxCountTypeValues];

    BYTE *m_pBulkTypeEventBuffer;

#ifdef FEATURE_NATIVEAOT
    int LogSingleType(EEType * pEEType);
#else
    int LogSingleType(TypeHandle th);
#endif

public:
    BulkTypeEventLogger() :
        m_nBulkTypeValueCount(0),
        m_nBulkTypeValueByteCount(0)
        , m_pBulkTypeEventBuffer(NULL)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_pBulkTypeEventBuffer = new (nothrow) BYTE[kSizeOfEventBuffer];
    }

    ~BulkTypeEventLogger()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        delete[] m_pBulkTypeEventBuffer;
        m_pBulkTypeEventBuffer = NULL;
    }

    void LogTypeAndParameters(ULONGLONG thAsAddr, ETW::TypeSystemLog::TypeLogBehavior typeLogBehavior);
    void FireBulkTypeEvent();
};


// Does all logging for RCWs and CCWs in the process.  We walk RCWs by enumerating all syncblocks in
// the process and seeing if they have associated interop information.  We enumerate all CCWs in the
// process from the RefCount handles on the handle table.
class BulkComLogger
{
public:
    // If typeLogger is non-null, we will log out the types via the logger, otherwise no type
    // information will be logged.
    BulkComLogger(BulkTypeEventLogger *typeLogger);
    ~BulkComLogger();

    // Walks all RCW/CCW objects.
    void LogAllComObjects();

    // Forces a flush of all ETW events not yet fired.
    void FireBulkComEvent();

private:
    // Writes one RCW to the RCW buffer.  May or may not fire the event.
    void WriteRcw(RCW *rcw, Object *obj);

    // Writes one CCW to the CCW buffer.  May or may not fire the event.
    void WriteCcw(ComCallWrapper *ccw, Object **handle, Object *obj);

    // Forces a flush of all RCW ETW events not yet fired.
    void FlushRcw();

    // Forces a flush of all CCW ETW events not yet fired.
    void FlushCcw();

    // Callback used during handle table enumeration.
    static void HandleWalkCallback(PTR_UNCHECKED_OBJECTREF pref, uintptr_t *pExtraInfo, uintptr_t param1, uintptr_t param2);

    // Used during CCW enumeration to keep track of all object handles which point to a CCW.
    void AddCcwHandle(Object **handle);

private:
    struct CCWEnumerationEntry
    {
        CCWEnumerationEntry *Next;
        int Count;
        Object **Handles[64];

        CCWEnumerationEntry() : Next(0), Count(0)
        {
        }
    };

private:
    // The maximum number of RCW/CCW events we can batch up based on the max size of an ETW event.
    static const int kMaxRcwCount = (cbMaxEtwEvent - 0x30) / sizeof(EventRCWEntry);
    static const int kMaxCcwCount = (cbMaxEtwEvent - 0x30) / sizeof(EventCCWEntry);

    int m_currRcw;  // The current number of batched (but not emitted) RCW events.
    int m_currCcw;  // The current number of batched (but not emitted) CCW events.

    BulkTypeEventLogger *m_typeLogger;  // Type logger to emit type data for.

    EventRCWEntry *m_etwRcwData;  // RCW buffer.
    EventCCWEntry *m_etwCcwData;  // CCW buffer.

    CCWEnumerationEntry *m_enumResult;
};


// Does bulk static variable ETW logging.
class BulkStaticsLogger
{
public:
    BulkStaticsLogger(BulkTypeEventLogger *typeLogger);
    ~BulkStaticsLogger();

    // Walk all static variables in the process and write them to the buffer, firing ETW events
    // as we reach the max buffer size.
    void LogAllStatics();

    // Force a flush of the static data, firing an ETW event for any not yet written.
    void FireBulkStaticsEvent();

private:
    // Write a single static variable to the log.
    void WriteEntry(AppDomain *domain, Object **address, Object *obj, FieldDesc *fieldDesc);

private:
    // The maximum bytes we can emit in the statics buffer.
    static const int kMaxBytesValues = (cbMaxEtwEvent - 0x30);

    BYTE *m_buffer;         // Buffer to queue up statics in
    int m_used;             // The amount of bytes used in m_buffer.
    int m_count;            // The number of statics currently written to m_buffer.
    AppDomain *m_domain;    // The current AppDomain m_buffer contains statics for.
    BulkTypeEventLogger *m_typeLogger;  // The type logger used to emit type data as we encounter it.
};



#endif // __EVENTTRACEPRIV_H__

