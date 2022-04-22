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
// ============================================================================

#ifndef __EVENTTRACEPRIV_H__
#define __EVENTTRACEPRIV_H__

#ifdef FEATURE_REDHAWK
#include "holder.h"
#endif // FEATURE_REDHAWK

#ifndef _countof
#define _countof(_array) (sizeof(_array)/sizeof(_array[0]))
#endif

const UINT cbMaxEtwEvent = 64 * 1024;

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
#ifdef FEATURE_REDHAWK
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

#ifdef FEATURE_REDHAWK
    // If > 1 type parameter, this is an array of their MethodTable*'s
    NewArrayHolder<ULONGLONG> rgTypeParameters;

    // If exactly one type parameter, this is its MethodTable*.  (If != 1 type parameter,
    // this is 0.)
    ULONGLONG ullSingleTypeParameter;
#else   // FEATURE_REDHAWK
    StackSString sName;
    StackSArray<ULONGLONG> rgTypeParameters;
#endif // FEATURE_REDHAWK
};

// Encapsulates all the type event batching we need to do. This is used by
// ETW::TypeSystemLog, which calls LogTypeAndParameters for each type to be logged.
// BulkTypeEventLogger will batch each type and its generic type parameters, and flush to
// ETW as necessary. ETW::TypeSystemLog also calls FireBulkTypeEvent directly to force a
// flush (e.g., once at end of GC heap traversal, or on each object allocation).
class BulkTypeEventLogger
{
private:

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

#ifdef FEATURE_REDHAWK
    int LogSingleType(MethodTable * pEEType);
#else
    int LogSingleType(TypeHandle th);
#endif

public:
    BulkTypeEventLogger() :
        m_nBulkTypeValueCount(0),
        m_nBulkTypeValueByteCount(0)
    {
        LIMITED_METHOD_CONTRACT;
    }

    void LogTypeAndParameters(ULONGLONG thAsAddr, ETW::TypeSystemLog::TypeLogBehavior typeLogBehavior);
    void FireBulkTypeEvent();
    void Cleanup();
};

#endif // __EVENTTRACEPRIV_H__
