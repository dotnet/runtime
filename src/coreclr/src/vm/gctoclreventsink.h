// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __GCTOCLREVENTSINK_H__
#define __GCTOCLREVENTSINK_H__

#include "gcinterface.h"

class GCToCLREventSink : public IGCToCLREventSink
{
public:
    void FireDynamicEvent(const char* eventName, void* payload, uint32_t payloadSize);

    void FireGCPerHeapHistory_V3(void *freeListAllocated,
                                 void *freeListRejected,
                                 void *endOfSegAllocated,
                                 void *condemnedAllocated,
                                 void *pinnedAllocated,
                                 void *pinnedAllocatedAdvance,
                                 uint32_t runningFreeListEfficiency,
                                 uint32_t condemnReasons0,
                                 uint32_t condemnReasons1,
                                 uint32_t compactMechanisms,
                                 uint32_t expandMechanisms,
                                 uint32_t heapIndex,
                                 void *extraGen0Commit,
                                 uint32_t count,
                                 uint32_t valuesLen,
                                 void *values);
    void FireBGCBegin();
    void FireBGC1stNonConEnd();
    void FireBGC1stConEnd();
    void FireBGC2ndNonConBegin();
    void FireBGC2ndNonConEnd();
    void FireBGC2ndConBegin();
    void FireBGC2ndConEnd();
    void FireBGCDrainMark(uint64_t objects);
    void FireBGCRevisit(uint64_t pages, uint64_t objects, uint32_t isLarge);
    void FireBGCOverflow(uint64_t min, uint64_t max, uint64_t objects, uint32_t isLarge);
    void FireBGCAllocWaitBegin(uint32_t reason);
    void FireBGCAllocWaitEnd(uint32_t reason);
    void FireGCFullNotify_V1(uint32_t genNumber, uint32_t isAlloc);
    void FireSetGCHandle(void *handleID, void *objectID, uint32_t kind, uint32_t generation, uint64_t appDomainID);
    void FirePrvSetGCHandle(void *handleID, void *objectID, uint32_t kind, uint32_t generation, uint64_t appDomainID);
    void FireDestroyGCHandle(void *handleID);
    void FirePrvDestroyGCHandle(void *handleID);
};

extern GCToCLREventSink g_gcToClrEventSink;

#endif // __GCTOCLREVENTSINK_H__

