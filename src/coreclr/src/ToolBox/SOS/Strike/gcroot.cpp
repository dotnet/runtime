// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==

/*
 * This file defines the support classes that allow us to operate on the object graph of the process that SOS
 * is analyzing.
 *
 * The GCRoot algorithm is based on three simple principles:
 *      1.  Only consider an object once.  When we inspect an object, read its references and don't ever touch
 *          it again.  This ensures that our upper bound on the amount of time we spend walking the object
 *          graph very quickly reaches resolution.  The objects we've already inspected (and thus won't inspect
 *          again) is tracked by the mConsidered variable.
 *      2.  Be extremely careful about reads from the target process.  We use a linear cache for reading from
 *          object data.  We also cache everything about the method tables we read out of, as well as caching
 *          the GCDesc which is required to walk the object's references.
 *      3.  Use O(1) data structures for anything perf-critical.  Almost all of the data structures we use to
 *          keep track of data have very fast lookups.  For example, to keep track of the objects we've considered
 *          we use a unordered_set.  Similarly to keep track of MethodTable data we use a unordered_map to track the
 *          mt -> mtinfo mapping.
 */ 

#include "sos.h"
#include "disasm.h"

#ifdef _ASSERTE
#undef _ASSERTE
#endif

#define _ASSERTE(a) {;}

#include "gcdesc.h"

#include "safemath.h"


#undef _ASSERTE

#ifdef _DEBUG
#define _ASSERTE(expr)         \
    do { if (!(expr) ) { ExtErr("_ASSERTE fired:\n\t%s\n", #expr); if (IsDebuggerPresent()) DebugBreak(); } } while (0)
#else
#define _ASSERTE(x)
#endif

inline size_t ALIGN_DOWN( size_t val, size_t alignment )
{
    // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
    _ASSERTE( 0 == (alignment & (alignment - 1)) );
    size_t result = val & ~(alignment - 1);
    return result;
}

inline void* ALIGN_DOWN( void* val, size_t alignment )
{
    return (void*) ALIGN_DOWN( (size_t)val, alignment );
}

LinearReadCache::LinearReadCache(ULONG pageSize)
    : mCurrPageStart(0), mPageSize(pageSize), mCurrPageSize(0), mPage(0)
{
    mPage = new BYTE[pageSize];
    ClearStats();
}

LinearReadCache::~LinearReadCache()
{
    if (mPage)
        delete [] mPage;
}

bool LinearReadCache::MoveToPage(TADDR addr, unsigned int size)
{
    if (size > mPageSize)
        size = mPageSize;

    mCurrPageStart = addr;
    HRESULT hr = g_ExtData->ReadVirtual(mCurrPageStart, mPage, size, &mCurrPageSize);

    if (hr != S_OK)
    {
        mCurrPageStart = 0;
        mCurrPageSize = 0;
        return false;
    }

#ifdef _DEBUG
    mMisses++;
#endif
    return true;
}


static const char *NameForHandle(unsigned int type)
{
    switch (type)
    {
    case 0:
        return "weak short";
    case 1:
        return "weak long";
    case 2:
        return "strong";
    case 3:
        return "pinned";
    case 5:
        return "ref counted";
    case 6:
        return "dependent";
    case 7:
        return "async pinned";
    case 8:
        return "sized ref";
    }

    return "unknown";
}

///////////////////////////////////////////////////////////////////////////////
// GCRoot functions to cleanup data.
///////////////////////////////////////////////////////////////////////////////
void GCRootImpl::ClearSizeData()
{
    mConsidered.clear();
    mSizes.clear();
}

void GCRootImpl::ClearAll()
{
    ClearNodes();

    {
        std::unordered_map<TADDR, MTInfo*>::iterator itr;
        for (itr = mMTs.begin(); itr != mMTs.end(); ++itr)
            delete itr->second;
    }
    
    {
        std::unordered_map<TADDR, RootNode*>::iterator itr;
        for (itr = mTargets.begin(); itr != mTargets.end(); ++itr)
            delete itr->second;
    }

    mMTs.clear();
    mTargets.clear();
    mConsidered.clear();
    mSizes.clear();
    mDependentHandleMap.clear();
    mCache.ClearStats();
    
    mAll = false;
    mSize = false;
}

void GCRootImpl::ClearNodes()
{
    std::list<RootNode*>::iterator itr;

    for  (itr = mCleanupList.begin(); itr != mCleanupList.end(); ++itr)
        delete *itr;

    mCleanupList.clear();
    mRootNewList.clear();
}

GCRootImpl::RootNode *GCRootImpl::NewNode(TADDR obj, MTInfo *mtInfo, bool fromDependent)
{
    // We need to create/destroy a TON of nodes during execution of GCRoot functions.
    // To avoid heap fragmentation (and since it's faster), we don't actually new/delete
    // nodes unless we have to.  Instead we keep a stl list with free nodes to use.
    RootNode *toReturn = NULL;
    
    if (mRootNewList.size())
    {
        toReturn = mRootNewList.back();
        mRootNewList.pop_back();
    }
    else
    {
        toReturn = new RootNode();
        mCleanupList.push_back(toReturn);
    }

    toReturn->Object = obj;
    toReturn->MTInfo = mtInfo;
    toReturn->FromDependentHandle = fromDependent;
    return toReturn;
}

void GCRootImpl::DeleteNode(RootNode *node)
{
    // Add node to the "new list".
    node->Clear();
    mRootNewList.push_back(node);
}

void GCRootImpl::GetDependentHandleMap(std::unordered_map<TADDR, std::list<TADDR>> &map)
{
    unsigned int type = HNDTYPE_DEPENDENT;
    ToRelease<ISOSHandleEnum> handles;
    
    HRESULT hr = g_sos->GetHandleEnumForTypes(&type, 1, &handles);
    
    if (FAILED(hr))
    {
        ExtOut("Failed to walk dependent handles.  GCRoot may miss paths.\n");
        return;
    }
    
    SOSHandleData data[4];
    unsigned int fetched = 0;
    
    do
    {
        hr = handles->Next(_countof(data), data, &fetched);
        
        if (FAILED(hr))
        {
            ExtOut("Error walking dependent handles.  GCRoot may miss paths.\n");
            return;
        }
        
        for (unsigned int i = 0; i < fetched; ++i)
        {
            if (data[i].Secondary != 0)
            {
                TADDR obj = 0;
                TADDR target = TO_TADDR(data[i].Secondary);
                
                MOVE(obj, TO_TADDR(data[i].Handle));
                
                map[obj].push_back(target);
            }
        }
    } while (fetched == _countof(data));
}

///////////////////////////////////////////////////////////////////////////////
// Public functions.
///////////////////////////////////////////////////////////////////////////////
int GCRootImpl::PrintRootsForObject(TADDR target, bool all, bool noStacks)
{
    ClearAll();
    GetDependentHandleMap(mDependentHandleMap);
    
    mAll = all;

    // Add "target" to the mTargets list.
    TADDR mt = ReadPointerCached(target);
    RootNode *node = NewNode(target, GetMTInfo(mt));
    mTargets[target] = node;

    // Look for roots on the HandleTable, FQ, and all threads.
    int count = 0;
    
    if (!noStacks)
        count += PrintRootsOnAllThreads();
    
    count += PrintRootsOnHandleTable();
    count += PrintRootsOnFQ();

    if(count == 0)
    {
        count += PrintRootsOnFQ(true);
        if(count)
        {
            ExtOut("Warning: These roots are from finalizable objects that are not yet ready for finalization.\n");
            ExtOut("This is to handle the case where objects re-register themselves for finalization.\n");
            ExtOut("These roots may be false positives.\n");
        }
    }

    mCache.PrintStats(__FUNCTION__);
    return count;
}


bool GCRootImpl::PrintPathToObject(TADDR root, TADDR target)
{
    ClearAll();
    GetDependentHandleMap(mDependentHandleMap);
    
    // Add "target" to the mTargets list.
    TADDR mt = ReadPointerCached(target);
    RootNode *node = NewNode(target, GetMTInfo(mt));
    mTargets[target] = node;

    // Check to see if the root reaches the target.
    RootNode *path = FindPathToTarget(root);
    if (path)
    {
        ExtOut("%p %S\n", SOS_PTR(path->Object), path->GetTypeName());
        path = path->Next;

        while (path)
        {
            ExtOut("  -> %p %S%s\n",SOS_PTR(path->Object), path->GetTypeName(), path->FromDependentHandle ? " (dependent handle)" : "");
            path = path->Next;
        }
        
        mCache.PrintStats(__FUNCTION__);
        return true;
    }
    
    mCache.PrintStats(__FUNCTION__);
    return false;
}

size_t GCRootImpl::ObjSize(TADDR root)
{
    // Calculates the size of the closure of objects kept alive by root.
    ClearAll();
    GetDependentHandleMap(mDependentHandleMap);
    
    // mSize tells GCRootImpl to build the "mSizes" table with the total size
    // each object roots.
    mSize = true;

    // Note that we are calling the same method as we would to locate the rooting
    // chain for an object, but we haven't added any items to mTargets.  This means
    // the algorithm will scan all objects and never terminate until it has walked
    // all objects in the closure.
    FindPathToTarget(root);
    
    mCache.PrintStats(__FUNCTION__);
    return mSizes[root];
}

void GCRootImpl::ObjSize()
{
    ClearAll();
    GetDependentHandleMap(mDependentHandleMap);
    mSize = true;

    // Walks all roots in the process, and prints out the object size for each one.
    PrintRootsOnAllThreads();
    PrintRootsOnHandleTable();
    PrintRootsOnFQ();
    
    mCache.PrintStats(__FUNCTION__);
}


const std::unordered_set<TADDR> &GCRootImpl::GetLiveObjects(bool excludeFQ)
{
    ClearAll();
    GetDependentHandleMap(mDependentHandleMap);

    // Walk each root in the process without setting a target.  This has the effect of
    // causing us to walk every object in the process, adding them to mConsidered as we
    // go.
    PrintRootsOnAllThreads();
    PrintRootsOnHandleTable();

    if (!excludeFQ)
        PrintRootsOnFQ();

    mCache.PrintStats(__FUNCTION__);
    return mConsidered;
}

int GCRootImpl::FindRoots(int gen, TADDR target)
{
    ClearAll();
    GetDependentHandleMap(mDependentHandleMap);

    if (gen == -1 || ((UINT)gen) == GetMaxGeneration())
    {
        // If this is a gen 2 !FindRoots, just do a regular !GCRoot
        return PrintRootsForObject(target, false, false);
    }
    else
    {
        // Otherwise walk all roots for only the given generation.
        int count = PrintRootsInOlderGen();
        count += PrintRootsOnHandleTable(gen);
        count += PrintRootsOnFQ();
        return count;
    }
}



///////////////////////////////////////////////////////////////////////////////
// GCRoot Methods for printing out results.
///////////////////////////////////////////////////////////////////////////////
void GCRootImpl::ReportSizeInfo(const SOSHandleData &handle, TADDR obj)
{
    // Print size for a handle  (!objsize)
    TADDR mt = ReadPointer(obj);
    MTInfo *mtInfo = GetMTInfo(mt);

    const WCHAR *type = mtInfo ? mtInfo->GetTypeName() : W("unknown type");

    size_t size = mSizes[obj];
    ExtOut("Handle (%s): %p -> %p: %d (0x%x) bytes (%S)\n", NameForHandle(handle.Type), SOS_PTR(handle.Handle),
                                SOS_PTR(obj), size, size, type);
}


void GCRootImpl::ReportSizeInfo(DWORD thread, const SOSStackRefData &stackRef, TADDR obj)
{
    // Print size for a stack root (!objsize)
    WString frame;
    if (stackRef.SourceType == SOS_StackSourceIP)
        frame = MethodNameFromIP(stackRef.Source);
    else
        frame = GetFrameFromAddress(TO_TADDR(stackRef.Source));

    WString regOutput = BuildRegisterOutput(stackRef);

    TADDR mt = ReadPointer(obj);
    MTInfo *mtInfo = GetMTInfo(mt);
    const WCHAR *type = mtInfo ? mtInfo->GetTypeName() : W("unknown type");
    
    size_t size = mSizes[obj];
    ExtOut("Thread %x (%S): %S: %d (0x%x) bytes (%S)\n", thread, frame.c_str(), regOutput.c_str(), size, size, type);
}

void GCRootImpl::ReportOneHandlePath(const SOSHandleData &handle, RootNode *path, bool printHeader)
{
    if (printHeader)
        ExtOut("HandleTable:\n");

    ExtOut("    %p (%s handle)\n", SOS_PTR(handle.Handle), NameForHandle(handle.Type));
    while (path)
    {
        ExtOut("    -> %p %S%s\n", SOS_PTR(path->Object), path->GetTypeName(), path->FromDependentHandle ? " (dependent handle)" : "");
        path = path->Next;
    }

    ExtOut("\n");
}

void GCRootImpl::ReportOnePath(DWORD thread, const SOSStackRefData &stackRef, RootNode *path, bool printThread, bool printFrame)
{
    if (printThread)
        ExtOut("Thread %x:\n", thread);
        
    if (printFrame)
    {
        if (stackRef.SourceType == SOS_StackSourceIP)
        {
            WString methodName = MethodNameFromIP(stackRef.Source);
            ExtOut("    %p %p %S\n", SOS_PTR(stackRef.StackPointer), SOS_PTR(stackRef.Source), methodName.c_str());
        }
        else
        {
            WString frameName = GetFrameFromAddress(TO_TADDR(stackRef.Source));
            ExtOut("    %p %S\n", SOS_PTR(stackRef.Source), frameName.c_str());
        }
    }
    
    WString regOutput = BuildRegisterOutput(stackRef, false);
    ExtOut("        %S\n", regOutput.c_str());
    
    while (path)
    {
        ExtOut("            ->  %p %S%s\n", SOS_PTR(path->Object), path->GetTypeName(), path->FromDependentHandle ? " (dependent handle)" : "");
        path = path->Next;
    }

    ExtOut("\n");
}

void GCRootImpl::ReportOneFQEntry(TADDR root, RootNode *path, bool printHeader)
{
    if (printHeader)
        ExtOut("Finalizer Queue:\n");

    ExtOut("    %p\n", SOS_PTR(root));
    while (path)
    {
        ExtOut("    -> %p %S%s\n", SOS_PTR(path->Object), path->GetTypeName(), path->FromDependentHandle ? " (dependent handle)" : "");
        path = path->Next;
    }

    ExtOut("\n");
}

void GCRootImpl::ReportOlderGenEntry(TADDR root, RootNode *path, bool printHeader)
{
    if (printHeader)
        ExtOut("Older Generation:\n");

    ExtOut("    %p\n", SOS_PTR(root));
    while (path)
    {
        ExtOut("    -> %p %S%s\n", SOS_PTR(path->Object), path->GetTypeName(), path->FromDependentHandle ? " (dependent handle)" : "");
        path = path->Next;
    }

    ExtOut("\n");
}

//////////////////////////////////////////////////////
int GCRootImpl::PrintRootsInOlderGen()
{
    // Use a different linear read cache here instead of polluting the object read cache.
    LinearReadCache cache(512);

    if (!IsServerBuild())
    {
        DacpGcHeapAnalyzeData analyzeData;
        if (analyzeData.Request(g_sos) != S_OK)
        {
            ExtErr("Error requesting gc heap analyze data\n");
            return 0;
        }

        if (!analyzeData.heap_analyze_success)
        {
            ExtOut("Failed to gather needed data, possibly due to memory constraints in the debuggee.\n");
            ExtOut("To try again re-issue the !FindRoots -gen <N> command.\n");
            return 0;
        }

        ExtDbgOut("internal_root_array = %#p\n", SOS_PTR(analyzeData.internal_root_array));
        ExtDbgOut("internal_root_array_index = %#p\n", SOS_PTR(analyzeData.internal_root_array_index));
        
        TADDR start = TO_TADDR(analyzeData.internal_root_array);
        TADDR stop = TO_TADDR(analyzeData.internal_root_array + sizeof(TADDR) * (size_t)analyzeData.internal_root_array_index);

        return PrintRootsInRange(cache, start, stop, &GCRootImpl::ReportOlderGenEntry, true);
    }
    else
    {
        int total = 0;
        DWORD dwAllocSize;
        DWORD dwNHeaps = GetGcHeapCount();
        if (!ClrSafeInt<DWORD>::multiply(sizeof(CLRDATA_ADDRESS), dwNHeaps, dwAllocSize))
        {
            ExtErr("Failed to get GCHeaps:  integer overflow\n");
            return 0;
        }

        CLRDATA_ADDRESS *heapAddrs = (CLRDATA_ADDRESS*)alloca(dwAllocSize);
        if (g_sos->GetGCHeapList(dwNHeaps, heapAddrs, NULL) != S_OK)
        {
            ExtErr("Failed to get GCHeaps\n");
            return 0;
        }

        for (UINT n = 0; n < dwNHeaps; n ++)
        {
            DacpGcHeapAnalyzeData analyzeData;
            if (analyzeData.Request(g_sos, heapAddrs[n]) != S_OK)
            {
                ExtErr("Error requesting gc heap analyze data for heap %p\n", SOS_PTR(heapAddrs[n]));
                continue;
            }

            if (!analyzeData.heap_analyze_success)
            {
                ExtOut("Failed to gather needed data, possibly due to memory constraints in the debuggee.\n");
                ExtOut("To try again re-issue the !FindRoots -gen <N> command.\n");
                continue;
            }

            ExtDbgOut("internal_root_array = %#p\n", SOS_PTR(analyzeData.internal_root_array));
            ExtDbgOut("internal_root_array_index = %#p\n", SOS_PTR(analyzeData.internal_root_array_index));
            
            TADDR start = TO_TADDR(analyzeData.internal_root_array);
            TADDR stop = TO_TADDR(analyzeData.internal_root_array + sizeof(TADDR) * (size_t)analyzeData.internal_root_array_index);

            total += PrintRootsInRange(cache, start, stop, &GCRootImpl::ReportOlderGenEntry, total == 0);
        }

        return total;
    }
}


int GCRootImpl::PrintRootsOnFQ(bool notReadyForFinalization)
{
    // Here are objects kept alive by objects in the finalizer queue.
    DacpGcHeapDetails heapDetails;

    // Use a different linear read cache here instead of polluting the object read cache.
    LinearReadCache cache(512);

    if (!IsServerBuild())
    {
        if (heapDetails.Request(g_sos) != S_OK)
        {
            ExtErr("Error requesting heap data.\n");
            return 0;
        }

        // If we include objects that are not ready for finalization, we may report
        // false positives.  False positives occur if the object is not ready for finalization
        // and does not re-register itself for finalization inside the finalizer.
        TADDR start = 0;
        TADDR stop = 0;
        if(notReadyForFinalization)
        {
            start = TO_TADDR(SegQueue(heapDetails, gen_segment(GetMaxGeneration())));
            stop = TO_TADDR(SegQueueLimit(heapDetails, CriticalFinalizerListSeg));
        }
        else
        {
            start = TO_TADDR(SegQueue(heapDetails, CriticalFinalizerListSeg));
            stop = TO_TADDR(SegQueue(heapDetails, FinalizerListSeg));
        }

        return PrintRootsInRange(cache, start, stop, &GCRootImpl::ReportOneFQEntry, true);
    }
    else
    {
        DWORD dwAllocSize;
        DWORD dwNHeaps = GetGcHeapCount();
        if (!ClrSafeInt<DWORD>::multiply(sizeof(CLRDATA_ADDRESS), dwNHeaps, dwAllocSize))
        {
            ExtErr("Failed to get GCHeaps:  integer overflow\n");
            return 0;
        }

        CLRDATA_ADDRESS *heapAddrs = (CLRDATA_ADDRESS*)alloca(dwAllocSize);
        if (g_sos->GetGCHeapList(dwNHeaps, heapAddrs, NULL) != S_OK)
        {
            ExtErr("Error requesting heap data.\n");
            return 0;
        }

        int total = 0;
        for (UINT n = 0; n < dwNHeaps; n ++)
        {
            if (heapDetails.Request(g_sos, heapAddrs[n]) != S_OK)
            {
                ExtErr("Error requesting heap data for heap %d.\n", n);
                continue;
            }
            
            // If we include objects that are not ready for finalization, we may report
            // false positives.  False positives occur if the object is not ready for finalization
            // and does not re-register itself for finalization inside the finalizer.
            TADDR start = 0;
            TADDR stop = 0;
            if(notReadyForFinalization)
            {
                start = TO_TADDR(SegQueue(heapDetails, gen_segment(GetMaxGeneration())));
                stop = TO_TADDR(SegQueueLimit(heapDetails, CriticalFinalizerListSeg));
            }
            else
            {
                start = TO_TADDR(SegQueue(heapDetails, CriticalFinalizerListSeg));
                stop = TO_TADDR(SegQueueLimit(heapDetails, FinalizerListSeg));
            }
            
            total += PrintRootsInRange(cache, start, stop, &GCRootImpl::ReportOneFQEntry, total == 0);
        }

        return total;
    }
}

int GCRootImpl::PrintRootsInRange(LinearReadCache &cache, TADDR start, TADDR stop, ReportCallback func, bool printHeader)
{
    int total = 0;

    // Walk the range [start, stop) and consider each pointer in the range as a root.
    while (start < stop)
    {
        if (IsInterrupt())
            return total;
        
        // Use the cache parameter here instead of mCache.  If you use mCache it will be reset
        // when calling into FindPathToTarget.
        TADDR root = 0;
        bool res = cache.Read(start, &root, true);

        if (res && root)
        {
            RootNode *path = FindPathToTarget(root);
            if (path)
            {
                func(root, path, printHeader);
                total++;
                printHeader = false;
            }
        }

        start += sizeof(TADDR);
    }

    return total;
}

int GCRootImpl::PrintRootsOnAllThreads()
{
    ArrayHolder<DWORD_PTR> threadList = NULL;
    int numThreads = 0;

    // GetThreadList calls ReportOOM so we don't need to do that here.
    HRESULT hr = GetThreadList(&threadList, &numThreads);
    if (FAILED(hr) || !threadList)
        return 0;
    
    // Walk each thread and process the roots on it.
    int total = 0;
    DacpThreadData vThread;
    for (int i = 0; i < numThreads; i++)
    {
        if (IsInterrupt())
            return total;
        
        if (FAILED(vThread.Request(g_sos, threadList[i])))
            continue;
        
        if (vThread.osThreadId)
            total += PrintRootsOnThread(vThread.osThreadId);
    }
    
    return total;
}

int GCRootImpl::PrintRootsOnThread(DWORD osThreadId)
{
    // Grab all object rootson the thread.
    unsigned int refCount = 0;
    ArrayHolder<SOSStackRefData> refs = NULL;
    
    int total = 0;
    bool first = true;
    if (FAILED(::GetGCRefs(osThreadId, &refs, &refCount, NULL, NULL)))
    {
        ExtOut("Failed to walk thread %x\n", osThreadId);
        return total;
    }

    // Walk each non-null root, find if it contains a path to the target,
    // and if so print it out.
    CLRDATA_ADDRESS prevSource = 0, prevSP = 0;
    for (unsigned int i = 0; i < refCount; ++i)
    {
        if (IsInterrupt())
            return total;
        
        if (refs[i].Object)
        {
            if (mSize)
                ClearSizeData();

            RootNode *path = FindPathToTarget(TO_TADDR(refs[i].Object));
            if (path)
            {
                bool reportFrame = refs[i].Source != prevSource || refs[i].StackPointer != prevSP;
                ReportOnePath(osThreadId, refs[i], path, first, reportFrame);
                first = false;
                total++;
            }
            
            if (mSize)
                ReportSizeInfo(osThreadId, refs[i], TO_TADDR(refs[i].Object));
        }
    }
    
    return total;
}

int GCRootImpl::PrintRootsOnHandleTable(int gen)
{
    // Get handle data.
    ToRelease<ISOSHandleEnum> pEnum = NULL;
    HRESULT hr = S_OK;
    
    if (gen == -1 || (ULONG)gen == GetMaxGeneration())
        hr = g_sos->GetHandleEnum(&pEnum);
    else
        hr = g_sos->GetHandleEnumForGC(gen, &pEnum);

    if (FAILED(hr))
    {
        ExtOut("Failed to walk the HandleTable!\n");
        return 0;
    }
    
    int total = 0;
    unsigned int fetched = 0;
    SOSHandleData handles[8];
    
    bool printHeader = true;
    do
    {
        // Fetch more handles
        hr = pEnum->Next(_countof(handles), handles, &fetched);
        if (FAILED(hr))
        {
            ExtOut("Failed to request more handles.");
            return total;
        }

        // Find rooting info for each handle.
        for (unsigned int i = 0; i < fetched; ++i)
        {
            if (IsInterrupt())
                return total;
            
            // Ignore handles which aren't actually roots.
            if (!handles[i].StrongReference)
                continue;
            
            // clear the size table
            if (mAll)
                ClearSizeData();

            // Get the object the handle points to.
            TADDR root = ReadPointer(TO_TADDR(handles[i].Handle));

            // Only inspect handle if the object is non-null, and not one we've already walked.
            if (root)
            {
                // Find all paths to the object and don't clean up the return value.
                // It's tracked by mCleanupList.
                RootNode *path = FindPathToTarget(root);
                if (path)
                {
                    ReportOneHandlePath(handles[i], path, printHeader);
                    printHeader = false;
                    total++;
                }

                if (mSize)
                    ReportSizeInfo(handles[i], root);
            }
        }
    }
    while (_countof(handles) == fetched);

    return total;
}

GCRootImpl::RootNode *GCRootImpl::FilterRoots(RootNode *&list)
{
    // Filter the list of GC refs:
    //   - Remove objects that we have already considered
    //   - Check to see if we've located the target object (or an object which points to the target).
    RootNode *curr = list;
    RootNode *keep = NULL;
    
    while (curr)
    {
        // We don't check for Control-C in this loop to avoid inconsistent data.
        RootNode *curr_next = curr->Next;

        std::unordered_map<TADDR, RootNode *>::iterator targetItr = mTargets.find(curr->Object);
        if (targetItr != mTargets.end())
        {
            // We found the object we are looking for (or an object which points to it)!
            // Return the target, propogate whether we got the target from a dependent handle.
            targetItr->second->FromDependentHandle = curr->FromDependentHandle;
            return targetItr->second;
        }
        else if (mConsidered.find(curr->Object) != mConsidered.end())
        {
            curr->Remove(list);

            DeleteNode(curr);
        }

        curr = curr_next;
    }

    return NULL;
}

/* This is the core of the GCRoot algorithm.  It is:
 *     1.  Start with a list of "targets" (objects we are trying to find the roots for) and a root
 *         in the process.
 *     2.  Let the root be "curr".
 *     3.  Find all objects curr points to and place them in curr->GCRefs (a linked list).
 *     4.  Walk curr->GCRefs.  If we find any of the "targets", return the current path.  If not,
 *         filter out any objects we've already considered (the mConsidered set).
 *     5.  Look at curr->GCRefs:
 *     5a. If curr->GCRefs is NULL then we have walked all references of this object.  Pop "curr"
 *         from the current path (curr = curr->Prev).  If curr is NULL, we walked all objects and
 *         didn't find a target, return NULL.  If curr is non-null, goto 5.
 *     5b. If curr->GCRefs is non-NULL, pop one entry and push it onto the path (that is:
 *         curr->Next = curr->GCRefs; curr = curr->Next; curr->GCRefs = curr->GCRefs->Next)
 *     6.  Goto 3.
 */
GCRootImpl::RootNode *GCRootImpl::FindPathToTarget(TADDR root)
{
    // Early out.  We may have already looked at this object.
    std::unordered_map<TADDR, RootNode *>::iterator targetItr = mTargets.find(root);
    if (targetItr != mTargets.end())
        return targetItr->second;
    else if (mConsidered.find(root) != mConsidered.end())
        return NULL;
    
    // Add obj as a considered node (since we are considering it now).
    mConsidered.insert(root);

    // Create path.
    RootNode *path = NewNode(root);

    RootNode *curr = path;
    while (curr)
    {
        if (IsInterrupt())
            return NULL;
        
        // If this is a new reference we are walking, we haven't filled the list of objects
        // this one points to.  Update that first.
        if (!curr->FilledRefs)
        {
            // Get the list of GC refs.
            curr->GCRefs = GetGCRefs(path, curr);

            // Filter the refs to remove objects we've already inspected.
            RootNode *foundTarget = FilterRoots(curr->GCRefs);

            // If we've found the target, great!  Return the path to the target.
            if (foundTarget)
            {
                // Link the current to the target.
                curr->Next = foundTarget;
                foundTarget->Prev = curr;
                
                // If the user requested all paths, set each node in the path to be a target.
                // Normally, we don't consider a node we've already seen, which means if we don't
                // get a *completely* unique path, it's not printed out.  By adding each of the
                // nodes in the paths we find as potential targets, it prints out *every* path
                // to the target, including ones with duplicate nodes.  This is much slower.
                if (mAll)
                {
                    RootNode *tmp = path;
                    
                    while (tmp)
                    {
                        if (mTargets.find(tmp->Object) != mTargets.end())
                            break;
                        
                        mTargets[tmp->Object] = tmp;
                        tmp = tmp->Next;
                    }
                }
                
                return path;
            }
        }

        // We have filled the references, now walk them depth-first.
        if (curr->GCRefs)
        {
            RootNode *next = curr->GCRefs;
            curr->GCRefs = next->Next;

            if (mConsidered.find(next->Object) != mConsidered.end())
            {
                // Whoops.  This object was considered deeper down the tree, so we
                // don't need to do it again.  Delete this node and continue looping.
                DeleteNode(next);
            }
            else
            {
                // Clear associations.
                if (curr->GCRefs)
                    curr->GCRefs->Prev = NULL;

                next->Next = NULL;

                // Link curr and next, make next the current node.
                curr->Next = next;
                next->Prev = curr;
                curr = next;
                
                // Finally, insert the current object into the considered set.
                mConsidered.insert(curr->Object);
                // Now the next iteration will operate on "next".
            }
        }
        else
        {
            // This object has no more GCRefs.  We now need to "pop" it from the current path.
            RootNode *tmp = curr;
            curr = curr->Prev;
            DeleteNode(tmp);
        }
    }

    return NULL;
}


GCRootImpl::RootNode *GCRootImpl::GetGCRefs(RootNode *path, RootNode *node)
{
    // If this node doesn't have the method table ready, fill it out
    TADDR obj = node->Object;
    if (!node->MTInfo)
    {
        TADDR mt = ReadPointerCached(obj);
        MTInfo *mtInfo = GetMTInfo(mt);
        node->MTInfo = mtInfo;
    }

    node->FilledRefs = true;
    
    // MTInfo can be null if we encountered an error reading out of the target
    // process, just early out here as if it has no references.
    if (!node->MTInfo)
        return NULL;

    // Only calculate the size if we need it.
    size_t objSize = 0;
    if (mSize || node->MTInfo->ContainsPointers || node->MTInfo->Collectible)
    {
        objSize = GetSizeOfObject(obj, node->MTInfo);
        
        // Update object size list, if requested.
        if (mSize)
        {
            mSizes[obj] = 0;
            
            while (path)
            {
                mSizes[path->Object] += objSize;
                path = path->Next;
            }
        }
    }
    
    // Early out:  If the object doesn't contain any pointers, return.
    if (!node->MTInfo->ContainsPointers && !node->MTInfo->Collectible)
        return NULL;
    
    // Make sure we have the object's data in the cache.
    mCache.EnsureRangeInCache(obj, (unsigned int)objSize);

    // Storage for the gc refs.
    RootNode *refs = NewNode();
    RootNode *curr = refs;

    // Walk the GCDesc, fill "refs" with non-null references.
    CGCDesc *gcdesc = node->MTInfo->GCDesc;
    bool aovc = node->MTInfo->ArrayOfVC;
    for (sos::RefIterator itr(obj, gcdesc, aovc, &mCache); itr; ++itr)
    {
        if (*itr)
        {
            curr->Next = NewNode(*itr);
            curr->Next->Prev = curr;
            curr = curr->Next;
        }
    }
    
    // Add edges from dependent handles.
    std::unordered_map<TADDR, std::list<TADDR>>::iterator itr = mDependentHandleMap.find(obj);
    if (itr != mDependentHandleMap.end())
    {
        for (std::list<TADDR>::iterator litr = itr->second.begin(); litr != itr->second.end(); ++litr)
        {
            curr->Next = NewNode(*litr, NULL, true);
            curr->Next->Prev = curr;
            curr = curr->Next;
        }
    }
    
    // The gcrefs actually start on refs->Next.
    curr = refs;
    refs = refs->Next;
    DeleteNode(curr);

    return refs;
}

DWORD GCRootImpl::GetComponents(TADDR obj, TADDR mt)
{
    // Get the number of components in the object (for arrays and such).
    DWORD Value = 0;

    // If we fail to read out the number of components, let's assume 0 so we don't try to
    // read further data from the object.
    if (!mCache.Read(obj + sizeof(TADDR), &Value, false))
        return 0;

    // The component size on a String does not contain the trailing NULL character,
    // so we must add that ourselves.
    if(TO_TADDR(g_special_usefulGlobals.StringMethodTable) == mt)
        return Value+1;

    return Value;
}

// Get the size of the object.
size_t GCRootImpl::GetSizeOfObject(TADDR obj, MTInfo *info)
{
    size_t res = info->BaseSize;

    if (info->ComponentSize)
    {
        // this is an array, so the size has to include the size of the components. We read the number
        // of components from the target and multiply by the component size to get the size.
        DWORD components = GetComponents(obj, info->MethodTable);
        res += info->ComponentSize * components;
    }

#ifdef _TARGET_WIN64_
    // On x64 we do an optimization to save 4 bytes in almost every string we create, so
    // pad to min object size if necessary.
    if (res < min_obj_size)
        res = min_obj_size;
#endif // _TARGET_WIN64_

    return (res > 0x10000) ? AlignLarge(res) : Align(res);
}

GCRootImpl::MTInfo *GCRootImpl::GetMTInfo(TADDR mt)
{
    // Remove lower bits in case we are in mark phase
    mt &= ~3;

    // Do we already have this MethodTable?
    std::unordered_map<TADDR, MTInfo *>::iterator itr = mMTs.find(mt);

    if (itr != mMTs.end())
        return itr->second;

    MTInfo *curr = new MTInfo;
    curr->MethodTable = mt;

    // Get Base/Component size.
    DacpMethodTableData dmtd;

    if (dmtd.Request(g_sos, mt) != S_OK)
    {
        delete curr;
        return NULL;
    }

    // Fill out size info.
    curr->BaseSize = (size_t)dmtd.BaseSize;
    curr->ComponentSize = (size_t)dmtd.ComponentSize;
    curr->ContainsPointers = dmtd.bContainsPointers ? true : false;

    // The following request doesn't work on older runtimes. For those, the
    // objects would just look like non-collectible, which is acceptable.
    DacpMethodTableCollectibleData dmtcd;
    if (SUCCEEDED(dmtcd.Request(g_sos, mt)))
    {
        curr->Collectible = dmtcd.bCollectible ? true : false;
        curr->LoaderAllocatorObjectHandle = TO_TADDR(dmtcd.LoaderAllocatorObjectHandle);
    }

    // If this method table contains pointers, fill out and cache the GCDesc.
    if (curr->ContainsPointers)
    {
        int nEntries;

        if (FAILED(MOVE(nEntries, mt-sizeof(TADDR))))
        {
            ExtOut("Failed to request number of entries.");
            delete curr;
            return NULL;
        }

        if (nEntries < 0) 
        {
            curr->ArrayOfVC = true;
            nEntries = -nEntries;
        }
        else
        {
            curr->ArrayOfVC = false;
        }

        size_t nSlots = 1 + nEntries * sizeof(CGCDescSeries)/sizeof(TADDR);
        curr->Buffer = new TADDR[nSlots];

        if (curr->Buffer == NULL)
        {        
            ReportOOM();
            delete curr;
            return NULL;
        }

        if (FAILED(g_ExtData->ReadVirtual(TO_CDADDR(mt - nSlots*sizeof(TADDR)), curr->Buffer, (ULONG)(nSlots*sizeof(TADDR)), NULL))) 
        {
            ExtOut("Failed to read GCDesc for MethodTable %p.\n", SOS_PTR(mt));
            delete curr;
            return NULL;
        }

        // Construct the GCDesc map and series.
        curr->GCDesc = (CGCDesc *)(curr->Buffer+nSlots);
    }

    mMTs[mt] = curr;
    return curr;
}


TADDR GCRootImpl::ReadPointer(TADDR location)
{
    // Reads a pointer from the cache, but doesn't update the cache if it wasn't in it.
    TADDR obj = NULL;
    bool res = mCache.Read(location, &obj, false);

    if (!res)
        return NULL;

    return obj;
}

TADDR GCRootImpl::ReadPointerCached(TADDR location)
{
    // Reads a pointer from the cache, but updates the cache if it wasn't in it.
    TADDR obj = NULL;
    bool res = mCache.Read(location, &obj, true);

    if (!res)
        return NULL;

    return obj;
}

///////////////////////////////////////////////////////////////////////////////

UINT FindAllPinnedAndStrong(DWORD_PTR handlearray[], UINT arraySize)
{
    unsigned int fetched = 0;
    SOSHandleData data[64];
    UINT pos = 0;
    
    // We do not call GetHandleEnumByType here with a list of strong handles since we would be
    // statically setting the list of strong handles, which could change in a future release.
    // Instead we rely on the dac to provide whether a handle is strong or not.
    ToRelease<ISOSHandleEnum> handles;
    HRESULT hr = g_sos->GetHandleEnum(&handles);
    if (FAILED(hr))
    {
        // This should basically never happen unless there's an OOM.
        ExtOut("Failed to enumerate GC handles.  HRESULT=%x.\n", hr);
        return 0;
    }
    
    do
    {
        hr = handles->Next(_countof(data), data, &fetched);
        
        if (FAILED(hr))
        {
            ExtOut("Failed to enumerate GC handles.  HRESULT=%x.\n", hr);
            break;
        }
            
        for (unsigned int i = 0; i < fetched; ++i)
        {
            if (pos >= arraySize)
            {
                ExtOut("Buffer overflow while enumerating handles.\n");
                return pos;
            }
            
            if (data[i].StrongReference)
            {
                handlearray[pos++] = (DWORD_PTR)data[i].Handle;
            }
        }
    } while (fetched == _countof(data));
    
    return pos;
}



void PrintNotReachableInRange(TADDR rngStart, TADDR rngEnd, BOOL bExcludeReadyForFinalization, HeapStat* hpstat, BOOL bShort)
{
    GCRootImpl gcroot;
    const std::unordered_set<TADDR> &liveObjs = gcroot.GetLiveObjects(bExcludeReadyForFinalization == TRUE);

    LinearReadCache cache(512);
    cache.EnsureRangeInCache(rngStart, (unsigned int)(rngEnd-rngStart));
    
    for (TADDR p = rngStart; p < rngEnd; p += sizeof(TADDR))
    {
        if (IsInterrupt())
            break;
        
        TADDR header = 0;
        TADDR obj = 0;
        TADDR taddrMT = 0;
        
        bool read = cache.Read(p-sizeof(SIZEOF_OBJHEADER), &header);
        read = read && cache.Read(p, &obj);
        if (read && ((header & BIT_SBLK_FINALIZER_RUN) == 0) && liveObjs.find(obj) == liveObjs.end())
        {
            if (bShort)
            {
                DMLOut("%s\n", DMLObject(obj));
            }
            else
            {
                DMLOut("%s ", DMLObject(obj));
                if (SUCCEEDED(GetMTOfObject(obj, &taddrMT)) && taddrMT) 
                {
                    size_t s = ObjectSize(obj);
                    if (hpstat)
                    {
                        hpstat->Add(taddrMT, (DWORD)s);
                    }
                }
            }
        }
    }
    
    if (!bShort)
        ExtOut("\n");
}


////////////////////////////////////////////////////////////////////////////////
//
// Some defines for cards taken from gc code
//
#define card_word_width ((size_t)32)

// 
// The value of card_size is determined empirically according to the average size of an object
// In the code we also rely on the assumption that one card_table entry (DWORD) covers an entire os page
//
#if defined (_TARGET_WIN64_)
#define card_size ((size_t)(2*DT_GC_PAGE_SIZE/card_word_width))
#else
#define card_size ((size_t)(DT_GC_PAGE_SIZE/card_word_width))
#endif //_TARGET_WIN64_

// so card_size = 128 on x86, 256 on x64

inline
size_t card_word (size_t card)
{
    return card / card_word_width;
}

inline
unsigned card_bit (size_t card)
{
    return (unsigned)(card % card_word_width);
}

inline
size_t card_of ( BYTE* object)
{
    return (size_t)(object) / card_size;
}

BOOL CardIsSet(const DacpGcHeapDetails &heap, TADDR objAddr)
{
    // The card table has to be translated to look at the refcount, etc.
    // g_card_table[card_word(card_of(g_lowest_address))].

    TADDR card_table = TO_TADDR(heap.card_table);
    card_table = card_table + card_word(card_of((BYTE *)heap.lowest_address))*sizeof(DWORD);
    
    do
    {        
        TADDR card_table_lowest_addr;
        TADDR card_table_next;

        if (MOVE(card_table_lowest_addr, ALIGN_DOWN(card_table, 0x1000) + sizeof(PVOID)) != S_OK)
        {
            ExtErr("Error getting card table lowest address\n");
            return FALSE;
        }

        if (MOVE(card_table_next, card_table - sizeof(PVOID)) != S_OK)
        {
            ExtErr("Error getting next card table\n");
            return FALSE;
        }
        
        size_t card = (objAddr - card_table_lowest_addr) / card_size;
        DWORD value;
        if (MOVE(value, card_table + card_word(card)*sizeof(DWORD)) != S_OK)
        {
            ExtErr("Error reading card bits\n");
            return FALSE;
        }
        
        if (value & 1<<card_bit(card))
            return TRUE;
        
        card_table = card_table_next;
    }
    while(card_table);

    return FALSE;
}

BOOL NeedCard(TADDR parent, TADDR child)
{
    int iChildGen = g_snapshot.GetGeneration(child);

    if (iChildGen == 2)
        return FALSE;

    int iParentGen = g_snapshot.GetGeneration(parent);

    return (iChildGen < iParentGen);
}

////////////////////////////////////////////////////////////////////////////////
//
// Some defines for mark_array taken from gc code
//

#define mark_bit_pitch 8
#define mark_word_width 32
#define mark_word_size (mark_word_width * mark_bit_pitch)
#define heap_segment_flags_swept 16

inline
size_t mark_bit_bit_of(CLRDATA_ADDRESS add)
{
    return  (size_t)((add / mark_bit_pitch) % mark_word_width);
}

inline
size_t mark_word_of(CLRDATA_ADDRESS add)
{
    return (size_t)(add / mark_word_size);
}

inline BOOL mark_array_marked(const DacpGcHeapDetails &heap, CLRDATA_ADDRESS add)
{

    DWORD entry = 0;
    HRESULT hr = MOVE(entry, heap.mark_array + sizeof(DWORD) * mark_word_of(add));

    if (FAILED(hr))
        ExtOut("Failed to read card table entry.\n");

    return entry & (1 << mark_bit_bit_of(add));
}

BOOL background_object_marked(const DacpGcHeapDetails &heap, CLRDATA_ADDRESS o)
{
    BOOL m = TRUE;

    if ((o >= heap.background_saved_lowest_address) && (o < heap.background_saved_highest_address))
        m = mark_array_marked(heap, o);

    return m;
}

BOOL fgc_should_consider_object(const DacpGcHeapDetails &heap, 
                                CLRDATA_ADDRESS o, 
                                const DacpHeapSegmentData &seg,
                                BOOL consider_bgc_mark_p, 
                                BOOL check_current_sweep_p, 
                                BOOL check_saved_sweep_p)
{
    // the logic for this function must be kept in sync with the analogous function in gc.cpp
    BOOL no_bgc_mark_p = FALSE;

    if (consider_bgc_mark_p)
    {
        if (check_current_sweep_p && (o < heap.next_sweep_obj))
        {
            no_bgc_mark_p = TRUE;
        }

        if (!no_bgc_mark_p)
        {
            if(check_saved_sweep_p && (o >= heap.saved_sweep_ephemeral_start))
            {
                no_bgc_mark_p = TRUE;
            }

            if (!check_saved_sweep_p)
            {
                CLRDATA_ADDRESS background_allocated = seg.background_allocated;
                if (o >= background_allocated)
                {
                    no_bgc_mark_p = TRUE;
                }
            }
        }
    }
    else
    {
        no_bgc_mark_p = TRUE;
    }

    return no_bgc_mark_p ? TRUE : background_object_marked(heap, o);
}

enum c_gc_state
{
    c_gc_state_marking,
    c_gc_state_planning,
    c_gc_state_free
};

inline BOOL in_range_for_segment(const DacpHeapSegmentData &seg, CLRDATA_ADDRESS addr)
{
    return (addr >= seg.mem) && (addr < seg.reserved);
}

void should_check_bgc_mark(const DacpGcHeapDetails &heap,
                           const DacpHeapSegmentData &seg, 
                           BOOL* consider_bgc_mark_p, 
                           BOOL* check_current_sweep_p,
                           BOOL* check_saved_sweep_p)
{
    // the logic for this function must be kept in sync with the analogous function in gc.cpp
    *consider_bgc_mark_p = FALSE;
    *check_current_sweep_p = FALSE;
    *check_saved_sweep_p = FALSE;

    if (heap.current_c_gc_state == c_gc_state_planning)
    {
        // We are doing the next_sweep_obj comparison here because we have yet to 
        // turn on the swept flag for the segment but in_range_for_segment will return
        // FALSE if the address is the same as reserved.
        if ((seg.flags & heap_segment_flags_swept) || (heap.next_sweep_obj == seg.reserved))
        {
            // this seg was already swept.
        }
        else
        {
            *consider_bgc_mark_p = TRUE;

            if (seg.segmentAddr == heap.saved_sweep_ephemeral_seg)
            {
                *check_saved_sweep_p = TRUE;
            }

            if (in_range_for_segment(seg, heap.next_sweep_obj))
            {
                *check_current_sweep_p = TRUE;
            }
        }
    }
}

// TODO: FACTOR TOGETHER THE OBJECT MEMBER WALKING CODE FROM
// TODO: VerifyObjectMember(), GetListOfRefs(), HeapTraverser::PrintRefs()
BOOL VerifyObjectMember(const DacpGcHeapDetails &heap, DWORD_PTR objAddr)
{
    BOOL ret = TRUE;
    BOOL bCheckCard = TRUE;
    size_t size = 0;
    {
        DWORD_PTR dwAddrCard = objAddr;
        while (dwAddrCard < objAddr + size)
        {
            if (CardIsSet(heap, dwAddrCard))
            {
                bCheckCard = FALSE;
                break;
            }
            dwAddrCard += card_size;
        }
        
        if (bCheckCard)
        {
            dwAddrCard = objAddr + size - 2*sizeof(PVOID);
            if (CardIsSet(heap, dwAddrCard))
            {
                bCheckCard = FALSE;
            }
        }
    }
    
    for (sos::RefIterator itr(TO_TADDR(objAddr)); itr; ++itr)
    {
        TADDR dwAddr1 = (DWORD_PTR)*itr;
        if (dwAddr1)
        {
           TADDR dwChild = dwAddr1;
           // Try something more efficient than IsObject here. Is the methodtable valid?
           size_t s;
           BOOL bPointers;
           TADDR dwAddrMethTable;
           if (FAILED(GetMTOfObject(dwAddr1, &dwAddrMethTable)) ||
                (GetSizeEfficient(dwAddr1, dwAddrMethTable, FALSE, s, bPointers) == FALSE)) 
           {
               DMLOut("object %s: bad member %p at %p\n", DMLObject(objAddr), SOS_PTR(dwAddr1), SOS_PTR(itr.GetOffset()));
               ret = FALSE;
           }

           if (IsMTForFreeObj(dwAddrMethTable))
           {
               DMLOut("object %s contains free object %p at %p\n", DMLObject(objAddr),
                      SOS_PTR(dwAddr1), SOS_PTR(objAddr+itr.GetOffset()));
              ret = FALSE;
           }

           // verify card table
           if (bCheckCard && NeedCard(objAddr+itr.GetOffset(), dwAddr1))
           {
               DMLOut("object %s:%s missing card_table entry for %p\n",
                      DMLObject(objAddr), (dwChild == dwAddr1) ? "" : " maybe",
                      SOS_PTR(objAddr+itr.GetOffset()));
               ret = FALSE;
           }
        }
    }

    return ret;
}

// search for can_verify_deep in gc.cpp for examples of how these functions are used.
BOOL VerifyObject(const DacpGcHeapDetails &heap, const DacpHeapSegmentData &seg, DWORD_PTR objAddr, DWORD_PTR MTAddr, size_t objSize, 
    BOOL bVerifyMember)
{    
    if (IsMTForFreeObj(MTAddr))
    {
        return TRUE;
    }
        
    if (objSize < min_obj_size)
    {
        DMLOut("object %s: size %d too small\n", DMLObject(objAddr), objSize);
        return FALSE;
    }

    // If we requested to verify the object's members, the GC may be in a state where that's not possible.
    // Here we check to see if the object in question needs to have its members updated.  If so, we turn off
    // verification for the object.
    if (bVerifyMember)
    {
        BOOL consider_bgc_mark = FALSE, check_current_sweep = FALSE, check_saved_sweep = FALSE;
        should_check_bgc_mark(heap, seg, &consider_bgc_mark, &check_current_sweep, &check_saved_sweep);
        bVerifyMember = fgc_should_consider_object(heap, objAddr, seg, consider_bgc_mark, check_current_sweep, check_saved_sweep);
    }

    return bVerifyMember ? VerifyObjectMember(heap, objAddr) : TRUE;
}


BOOL FindSegment(const DacpGcHeapDetails &heap, DacpHeapSegmentData &seg, CLRDATA_ADDRESS addr)
{
    CLRDATA_ADDRESS dwAddrSeg = heap.generation_table[GetMaxGeneration()].start_segment;

    // Request the inital segment.
    if (seg.Request(g_sos, dwAddrSeg, heap) != S_OK)
    {
        ExtOut("Error requesting heap segment %p.\n", SOS_PTR(dwAddrSeg));
        return FALSE;
    }

    // Loop while the object is not in range of the segment.
    while (addr < TO_TADDR(seg.mem) || 
           addr >= (dwAddrSeg == heap.ephemeral_heap_segment ? heap.alloc_allocated : TO_TADDR(seg.allocated)))
    {
        // get the next segment
        dwAddrSeg = seg.next;

        // We reached the last segment without finding the object.
        if (dwAddrSeg == NULL)
            return FALSE;

        if (seg.Request(g_sos, dwAddrSeg, heap) != S_OK)
        {
            ExtOut("Error requesting heap segment %p.\n", SOS_PTR(dwAddrSeg));
            return FALSE;
        }
    }

    return TRUE;
}

BOOL VerifyObject(const DacpGcHeapDetails &heap, DWORD_PTR objAddr, DWORD_PTR MTAddr, size_t objSize, BOOL bVerifyMember)
{
    // This is only used by the other VerifyObject function if bVerifyMember is true,
    // so we only initialize it if we need it for verifying object members.
    DacpHeapSegmentData seg;

    if (bVerifyMember)
    {
        // if we fail to find the segment, we cannot verify the object's members
        bVerifyMember = FindSegment(heap, seg, objAddr);
    }

    return VerifyObject(heap, seg, objAddr, MTAddr, objSize, bVerifyMember);
}

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
typedef void (*TYPETREEVISIT)(size_t methodTable, size_t ID, LPVOID token);

// TODO remove this.   MethodTableCache already maps method tables to
// various information.  We don't need TypeTree to do this too.   
// Straightfoward to do, but low priority.  
class TypeTree
{
private:
    size_t methodTable;
    size_t ID;
    TypeTree *pLeft;
    TypeTree *pRight;

public:    
    TypeTree(size_t MT) : methodTable(MT),ID(0),pLeft(NULL),pRight(NULL) { }

    BOOL isIn(size_t MT, size_t *pID)
    {
        TypeTree *pCur = this;

        while (pCur)
        {
            if (MT == pCur->methodTable)
            {
                if (pID)
                    *pID = pCur->ID;
                return TRUE;
            }
            else if (MT < pCur->methodTable)
                pCur = pCur->pLeft;
            else
                pCur = pCur->pRight;            
        }
            
        return FALSE;
    }

    BOOL insert(size_t MT)
    {
        TypeTree *pCur = this;

        while (pCur)
        {
            if (MT == pCur->methodTable)
                return TRUE;
            else if ((MT < pCur->methodTable))
            {
                if (pCur->pLeft)
                    pCur = pCur->pLeft;
                else
                    break;
            }
            else if (pCur->pRight)
                pCur = pCur->pRight;            
            else
                break;
        }        

        // If we got here, we need to append at the current node.
        TypeTree *pNewNode = new TypeTree(MT);
        if (pNewNode == NULL)
            return FALSE;
        
        if (MT < pCur->methodTable)
            pCur->pLeft = pNewNode;
        else
            pCur->pRight = pNewNode;

        return TRUE;
    }

    static void destroy(TypeTree *pStart)
    {
        TypeTree *pCur = pStart;

        if (pCur)
        {
            destroy(pCur->pLeft);
            destroy(pCur->pRight);
            delete [] pCur;
        }        
    }

    static void visit_inorder(TypeTree *pStart, TYPETREEVISIT pFunc, LPVOID token)
    {
        TypeTree *pCur = pStart;

        if (pCur)
        {
            visit_inorder(pCur->pLeft, pFunc, token);
            pFunc (pCur->methodTable, pCur->ID, token);
            visit_inorder(pCur->pRight, pFunc, token);
        }        
    }

    static void setTypeIDs(TypeTree *pStart, size_t *pCurID)
    {
        TypeTree *pCur = pStart;

        if (pCur)
        {
            setTypeIDs(pCur->pLeft, pCurID);
            pCur->ID = *pCurID;
            (*pCurID)++;
            setTypeIDs(pCur->pRight, pCurID);
        }        
    }
    
};

///////////////////////////////////////////////////////////////////////////////
//

HeapTraverser::HeapTraverser(bool verify)
{
    m_format = 0;
    m_file = NULL;
    m_objVisited = 0;
    m_pTypeTree = NULL;
    m_curNID = 1;
    m_verify = verify;
}
    
HeapTraverser::~HeapTraverser() 
{ 
    if (m_pTypeTree) { 
        TypeTree::destroy(m_pTypeTree); 
        m_pTypeTree = NULL;
    } 
}

BOOL HeapTraverser::Initialize()
{
    if (!GCHeapsTraverse (HeapTraverser::GatherTypes, this, m_verify))
    {
        ExtOut("Error during heap traverse\n");
        return FALSE;
    }

    GCRootImpl::GetDependentHandleMap(mDependentHandleMap);

    size_t startID = 1;
    TypeTree::setTypeIDs(m_pTypeTree, &startID);

    return TRUE;
}

BOOL HeapTraverser::CreateReport (FILE *fp, int format)
{
    if (fp == NULL || (format!=FORMAT_XML && format != FORMAT_CLRPROFILER))
    {
        return FALSE;
    }

    m_file = fp;
    m_format = format;

    PrintSection(TYPE_START,TRUE);                     
    
    PrintSection(TYPE_TYPES,TRUE);        
    TypeTree::visit_inorder(m_pTypeTree, HeapTraverser::PrintOutTree, this);
    PrintSection(TYPE_TYPES,FALSE);

    ExtOut("tracing roots...\n");
    PrintSection(TYPE_ROOTS,TRUE);
    PrintRootHead();

    TraceHandles();
    FindGCRootOnStacks();        

    PrintRootTail();
    PrintSection(TYPE_ROOTS,FALSE);
        
        // now print type tree
    PrintSection(TYPE_OBJECTS,TRUE);        
    ExtOut("\nWalking heap...\n");
    m_objVisited = 0; // for UI updates
    GCHeapsTraverse (HeapTraverser::PrintHeap, this, FALSE);       // Never verify on the second pass
    PrintSection(TYPE_OBJECTS,FALSE);        
        
    PrintSection(TYPE_START,FALSE);                     
    
    m_file = NULL;
    return TRUE;
}

void HeapTraverser::insert(size_t mTable)
{
    if (m_pTypeTree == NULL)
    {
        m_pTypeTree = new TypeTree(mTable);
        if (m_pTypeTree == NULL)
        {
            ReportOOM();
            return;
        }
    }
    else
    {
        m_pTypeTree->insert(mTable);
    }
}

size_t HeapTraverser::getID(size_t mTable)
{
    if (m_pTypeTree == NULL)
    {
        return 0;
    }
    // IDs start at 1, so we can return 0 if not found.
    size_t ret;
    if (m_pTypeTree->isIn(mTable,&ret))
    {
        return ret;
    }
    
    return 0;
}

#ifndef FEATURE_PAL
void replace(std::wstring &str, const WCHAR *toReplace, const WCHAR *replaceWith)
{
    const size_t replaceLen = _wcslen(toReplace);
    const size_t replaceWithLen = _wcslen(replaceWith);
    
    size_t i = str.find(toReplace);
    while (i != std::wstring::npos)
    {
        str.replace(i, replaceLen, replaceWith);
        i = str.find(toReplace, i + replaceWithLen);
    }
}
#endif

void HeapTraverser::PrintType(size_t ID,LPCWSTR name)
{
    if (m_format==FORMAT_XML)
    {
#ifndef FEATURE_PAL
        // Sanitize name based on XML spec.
        std::wstring wname = name;
        replace(wname, W("&"), W("&amp;"));
        replace(wname, W("\""), W("&quot;"));
        replace(wname, W("'"), W("&apos;"));
        replace(wname, W("<"), W("&lt;"));
        replace(wname, W(">"), W("&gt;"));
        name = wname.c_str();
#endif
        fprintf(m_file,
            "<type id=\"%d\" name=\"%S\"/>\n",
            ID, name);
    }
    else if (m_format==FORMAT_CLRPROFILER)
    {
        fprintf(m_file,
            "t %d 0 %S\n",
            ID,name);
    }
}

void HeapTraverser::PrintObjectHead(size_t objAddr,size_t typeID,size_t Size)
{
    if (m_format==FORMAT_XML)
    {
        fprintf(m_file,
            "<object address=\"0x%p\" typeid=\"%d\" size=\"%d\">\n",
            (PBYTE)objAddr,typeID, Size);
    }
    else if (m_format==FORMAT_CLRPROFILER)
    {
        fprintf(m_file,
            "n %d 1 %d %d\n",
            m_curNID,typeID,Size);

        fprintf(m_file,
            "! 1 0x%p %d\n",
            (PBYTE)objAddr,m_curNID);

        m_curNID++;
        
        fprintf(m_file,
            "o 0x%p %d %d ",
            (PBYTE)objAddr,typeID,Size);
    }
}

void HeapTraverser::PrintLoaderAllocator(size_t memberValue)
{
    if (m_format == FORMAT_XML)
    {
        fprintf(m_file,
            "    <loaderallocator address=\"0x%p\"/>\n",
            (PBYTE)memberValue);
    }
    else if (m_format == FORMAT_CLRPROFILER)
    {
        fprintf(m_file,
            " 0x%p",
            (PBYTE)memberValue);
    }
}

void HeapTraverser::PrintObjectMember(size_t memberValue, bool dependentHandle)
{
    if (m_format==FORMAT_XML)
    {
        fprintf(m_file,
            "    <member address=\"0x%p\"%s/>\n",
            (PBYTE)memberValue, dependentHandle ? " dependentHandle=\"1\"" : "");
    }
    else if (m_format==FORMAT_CLRPROFILER)
    {
        fprintf(m_file,
            " 0x%p",
            (PBYTE)memberValue);    
    }
}

void HeapTraverser::PrintObjectTail()
{
    if (m_format==FORMAT_XML)
    {
        fprintf(m_file,
            "</object>\n");
    }
    else if (m_format==FORMAT_CLRPROFILER)
    {
        fprintf(m_file,
            "\n");
    }
}

void HeapTraverser::PrintRootHead()
{
    if (m_format==FORMAT_CLRPROFILER)
    {
        fprintf(m_file,
            "r ");
    }
}

void HeapTraverser::PrintRoot(LPCWSTR kind,size_t Value)
{
    if (m_format==FORMAT_XML)
    {
        fprintf(m_file,
            "<root kind=\"%S\" address=\"0x%p\"/>\n",
            kind,
            (PBYTE)Value);
    }
    else if (m_format==FORMAT_CLRPROFILER)
    {
        fprintf(m_file,
            "0x%p ",
            (PBYTE)Value);
    }
}

void HeapTraverser::PrintRootTail()
{
    if (m_format==FORMAT_CLRPROFILER)
    {
        fprintf(m_file,
            "\n");
    }
}

void HeapTraverser::PrintSection(int Type,BOOL bOpening)
{
    const char *const pTypes[] = {"<gcheap>","<types>","<roots>","<objects>"};
    const char *const pTypeEnds[] = {"</gcheap>","</types>","</roots>","</objects>"};

    if (m_format==FORMAT_XML)    
    {
        if ((Type >= 0) && (Type < TYPE_HIGHEST))
        {
            fprintf(m_file,"%s\n",bOpening ? pTypes[Type] : pTypeEnds[Type]);
        }
        else
        {
            ExtOut ("INVALID TYPE %d\n", Type);
        }
    }        
    else if (m_format==FORMAT_CLRPROFILER)
    {
        if ((Type == TYPE_START) && !bOpening) // a final newline is needed
        {
            fprintf(m_file,"\n");
        }
    }
}

void HeapTraverser::FindGCRootOnStacks()
{
    ArrayHolder<DWORD_PTR> threadList = NULL;
    int numThreads = 0;

    // GetThreadList calls ReportOOM so we don't need to do that here.
    HRESULT hr = GetThreadList(&threadList, &numThreads);
    if (FAILED(hr) || !threadList)
    {
        ExtOut("Failed to enumerate threads in the process.\n");
        return;
    }
    
    int total = 0;
    DacpThreadData vThread;
    for (int i = 0; i < numThreads; i++)
    {
        if (FAILED(vThread.Request(g_sos, threadList[i])))
            continue;
        
        if (vThread.osThreadId)
        {
            unsigned int refCount = 0;
            ArrayHolder<SOSStackRefData> refs = NULL;
            
            if (FAILED(::GetGCRefs(vThread.osThreadId, &refs, &refCount, NULL, NULL)))
            {
                ExtOut("Failed to walk thread %x\n", vThread.osThreadId);
                continue;
            }

            for (unsigned int i = 0; i < refCount; ++i)
                if (refs[i].Object)
                    PrintRoot(W("stack"), TO_TADDR(refs[i].Object));
        }
    }
    
}


/* static */ void HeapTraverser::PrintOutTree(size_t methodTable, size_t ID, 
    LPVOID token)
{
    HeapTraverser *pHolder = (HeapTraverser *) token;
    NameForMT_s(methodTable, g_mdName, mdNameLen);
    pHolder->PrintType(ID,g_mdName);
}


/* static */ void HeapTraverser::PrintHeap(DWORD_PTR objAddr,size_t Size,
    DWORD_PTR methodTable, LPVOID token)
{    
    if (!IsMTForFreeObj (methodTable))
    {        
        HeapTraverser *pHolder = (HeapTraverser *) token;
        pHolder->m_objVisited++;
        size_t ID = pHolder->getID(methodTable);

        pHolder->PrintObjectHead(objAddr, ID, Size);
        pHolder->PrintRefs(objAddr, methodTable, Size);
        pHolder->PrintObjectTail();

        if (pHolder->m_objVisited % 1024 == 0) {
            ExtOut(".");
            if (pHolder->m_objVisited % (1024*64) == 0)
                ExtOut("\r\n");
        }
    }
}

void HeapTraverser::TraceHandles()
{
    unsigned int fetched = 0;
    SOSHandleData data[64];
    
    ToRelease<ISOSHandleEnum> handles;
    HRESULT hr = g_sos->GetHandleEnum(&handles);
    if (FAILED(hr))
        return;
    
    do
    {
        hr = handles->Next(_countof(data), data, &fetched);
        
        if (FAILED(hr))
            break;
            
        for (unsigned int i = 0; i < fetched; ++i)
            PrintRoot(W("handle"), (size_t)data[i].Handle);
    } while (fetched == _countof(data));
}

/* static */ void HeapTraverser::GatherTypes(DWORD_PTR objAddr,size_t Size,
    DWORD_PTR methodTable, LPVOID token)
{    
    if (!IsMTForFreeObj (methodTable))
    {
        HeapTraverser *pHolder = (HeapTraverser *) token;
        pHolder->insert(methodTable);
    }
}

void HeapTraverser::PrintRefs(size_t obj, size_t methodTable, size_t size)
{
    DWORD_PTR dwAddr = methodTable;
    
    // TODO: pass info to callback having to lookup the MethodTableInfo again
    MethodTableInfo* info = g_special_mtCache.Lookup((DWORD_PTR)methodTable);
    _ASSERTE(info->IsInitialized());    // This is the second pass, so we should be initialized

    if (!info->bContainsPointers && !info->bCollectible)
        return;
    
    if (info->bContainsPointers)
    {
        // Fetch the GCInfo from the other process 
        CGCDesc *map = info->GCInfo;
        if (map == NULL)
        {
            INT_PTR nEntries;
            move_xp (nEntries, dwAddr-sizeof(PVOID));
            bool arrayOfVC = false;
            if (nEntries<0)
            {
                arrayOfVC = true;
                nEntries = -nEntries;
            }

            size_t nSlots = 1+nEntries*sizeof(CGCDescSeries)/sizeof(DWORD_PTR);
            info->GCInfoBuffer = new DWORD_PTR[nSlots];
            if (info->GCInfoBuffer == NULL)
            {
                ReportOOM();
                return;
            }

            if (FAILED(rvCache->Read(TO_CDADDR(dwAddr - nSlots*sizeof(DWORD_PTR)),
                                            info->GCInfoBuffer, (ULONG) (nSlots*sizeof(DWORD_PTR)), NULL)))
                return;

            map = info->GCInfo = (CGCDesc*)(info->GCInfoBuffer+nSlots);
            info->ArrayOfVC = arrayOfVC;
        }
    }

    mCache.EnsureRangeInCache((TADDR)obj, (unsigned int)size);
    for (sos::RefIterator itr(obj, info->GCInfo, info->ArrayOfVC, &mCache); itr; ++itr)
    {
        if (*itr && (!m_verify || sos::IsObject(*itr)))
        {
            if (itr.IsLoaderAllocator())
            {
                PrintLoaderAllocator(*itr);
            }
            else
            {
                PrintObjectMember(*itr, false);
            }
        }
    }
    
    std::unordered_map<TADDR, std::list<TADDR>>::iterator itr = mDependentHandleMap.find((TADDR)obj);
    if (itr != mDependentHandleMap.end())
    {
        for (std::list<TADDR>::iterator litr = itr->second.begin(); litr != itr->second.end(); ++litr)
        {
            PrintObjectMember(*litr, true);
        }
    }
}


void sos::ObjectIterator::BuildError(char *out, size_t count, const char *format, ...) const
{
    if (out == NULL || count == 0)
        return;

    va_list args;
    va_start(args, format);

    int written = vsprintf_s(out, count, format, args);
    if (written > 0 && mLastObj)
        sprintf_s(out+written, count-written, "\nLast good object: %p.\n", (int*)mLastObj);

    va_end(args);
}

bool sos::ObjectIterator::VerifyObjectMembers(char *reason, size_t count) const
{
    if (!mCurrObj.HasPointers())
        return true;

    size_t size = mCurrObj.GetSize();
    size_t objAddr = (size_t)mCurrObj.GetAddress();
    TADDR mt = mCurrObj.GetMT();

    INT_PTR nEntries;
    MOVE(nEntries, mt-sizeof(PVOID));
    if (nEntries < 0)
        nEntries = -nEntries;

    size_t nSlots = 1 + nEntries * sizeof(CGCDescSeries)/sizeof(DWORD_PTR);
    ArrayHolder<DWORD_PTR> buffer = new DWORD_PTR[nSlots];

    if (FAILED(g_ExtData->ReadVirtual(TO_CDADDR(mt - nSlots*sizeof(DWORD_PTR)),
                                      buffer, (ULONG) (nSlots*sizeof(DWORD_PTR)), NULL)))
    {
        BuildError(reason, count, "Object %s has a bad GCDesc.", DMLObject(objAddr));
        return false;
    }
    
    CGCDesc *map = (CGCDesc *)(buffer+nSlots);            
    CGCDescSeries* cur = map->GetHighestSeries();                           
    CGCDescSeries* last = map->GetLowestSeries();                                                 

    const size_t bufferSize = sizeof(size_t)*128;
    size_t objBuffer[bufferSize/sizeof(size_t)];
    size_t dwBeginAddr = (size_t)objAddr;
    size_t bytesInBuffer = bufferSize;
    if (size < bytesInBuffer)
        bytesInBuffer = size;
    

    if (FAILED(g_ExtData->ReadVirtual(TO_CDADDR(dwBeginAddr), objBuffer, (ULONG) bytesInBuffer,NULL)))
    {
        BuildError(reason, count, "Object %s: Failed to read members.", DMLObject(objAddr));
        return false;
    }

    BOOL bCheckCard = TRUE;
    {
        DWORD_PTR dwAddrCard = (DWORD_PTR)objAddr;
        while (dwAddrCard < objAddr + size)
        {
            if (CardIsSet (mHeaps[mCurrHeap], dwAddrCard))
            {
                bCheckCard = FALSE;
                break;
            }
            dwAddrCard += card_size;
        }
        if (bCheckCard)
        {
            dwAddrCard = objAddr + size - 2*sizeof(PVOID);
            if (CardIsSet (mHeaps[mCurrHeap], dwAddrCard))
            {
                bCheckCard = FALSE;
            }
        }
    }

    if (cur >= last)                                                        
    {                                                                       
        do                                                                  
        {                                                                   
            BYTE** parm = (BYTE**)((objAddr) + cur->GetSeriesOffset());           
            BYTE** ppstop =                                                 
                (BYTE**)((BYTE*)parm + cur->GetSeriesSize() + (size));      
            while (parm < ppstop)                                           
            {
                CheckInterrupt();
                size_t dwAddr1;

                // Do we run out of cache?
                if ((size_t)parm >= dwBeginAddr+bytesInBuffer)
                {
                    // dwBeginAddr += bytesInBuffer;
                    dwBeginAddr = (size_t)parm;
                    if (dwBeginAddr >= objAddr + size)
                    {
                        return true;
                    }
                    bytesInBuffer = bufferSize;
                    if (objAddr+size-dwBeginAddr < bytesInBuffer)
                    {
                        bytesInBuffer = objAddr+size-dwBeginAddr;
                    }
                    if (FAILED(g_ExtData->ReadVirtual(TO_CDADDR(dwBeginAddr), objBuffer, (ULONG) bytesInBuffer, NULL)))
                    {
                       BuildError(reason, count, "Object %s: Failed to read members.", DMLObject(objAddr));
                       return false;
                    }
                }
                dwAddr1 = objBuffer[((size_t)parm-dwBeginAddr)/sizeof(size_t)];
                if (dwAddr1) {
                    DWORD_PTR dwChild = dwAddr1;
                    // Try something more efficient than IsObject here. Is the methodtable valid?
                    size_t s;
                    BOOL bPointers;
                    DWORD_PTR dwAddrMethTable;
                    if (FAILED(GetMTOfObject(dwAddr1, &dwAddrMethTable)) ||
                         (GetSizeEfficient(dwAddr1, dwAddrMethTable, FALSE, s, bPointers) == FALSE)) 
                    {
                        BuildError(reason, count, "object %s: bad member %p at %p", DMLObject(objAddr),
                               SOS_PTR(dwAddr1), SOS_PTR(objAddr+(size_t)parm-objAddr));

                        return false;
                    }
               
                    if (IsMTForFreeObj(dwAddrMethTable))
                    {
                        sos::Throw<HeapCorruption>("object %s contains free object %p at %p", DMLObject(objAddr),
                               SOS_PTR(dwAddr1), SOS_PTR(objAddr+(size_t)parm-objAddr));
                    }
               
                    // verify card table
                    if (bCheckCard && 
                        NeedCard(objAddr+(size_t)parm-objAddr,dwChild))
                    {
                        BuildError(reason, count, "Object %s: %s missing card_table entry for %p",
                                DMLObject(objAddr), (dwChild == dwAddr1)? "" : " maybe",
                                SOS_PTR(objAddr+(size_t)parm-objAddr));

                        return false;
                    }
                }
                parm++;                                   
            }
            cur--;
            CheckInterrupt();

        } while (cur >= last);
    }
    else
    {
        int cnt = (int) map->GetNumSeries();
        BYTE** parm = (BYTE**)((objAddr) + cur->startoffset);
        while ((BYTE*)parm < (BYTE*)((objAddr)+(size)-plug_skew))
        {
            for (int __i = 0; __i > cnt; __i--)
            {
                CheckInterrupt();

                unsigned skip =  cur->val_serie[__i].skip;
                unsigned nptrs = cur->val_serie[__i].nptrs;
                BYTE** ppstop = parm + nptrs;
                do
                {
                    size_t dwAddr1;
                    // Do we run out of cache?
                    if ((size_t)parm >= dwBeginAddr+bytesInBuffer)
                    {
                        // dwBeginAddr += bytesInBuffer;
                        dwBeginAddr = (size_t)parm;
                        if (dwBeginAddr >= objAddr + size)
                            return true;

                        bytesInBuffer = bufferSize;
                        if (objAddr+size-dwBeginAddr < bytesInBuffer)
                            bytesInBuffer = objAddr+size-dwBeginAddr;

                        if (FAILED(g_ExtData->ReadVirtual(TO_CDADDR(dwBeginAddr), objBuffer, (ULONG) bytesInBuffer, NULL)))
                        {
                            BuildError(reason, count, "Object %s: Failed to read members.", DMLObject(objAddr));
                            return false;
                        }
                    }
                    dwAddr1 = objBuffer[((size_t)parm-dwBeginAddr)/sizeof(size_t)];
                    {
                         if (dwAddr1)
                         {
                             DWORD_PTR dwChild = dwAddr1;
                             // Try something more efficient than IsObject here. Is the methodtable valid?
                             size_t s;
                             BOOL bPointers;
                             DWORD_PTR dwAddrMethTable;
                             if (FAILED(GetMTOfObject(dwAddr1, &dwAddrMethTable)) ||
                                  (GetSizeEfficient(dwAddr1, dwAddrMethTable, FALSE, s, bPointers) == FALSE)) 
                             {
                                 BuildError(reason, count, "Object %s: Bad member %p at %p.\n", DMLObject(objAddr),
                                         SOS_PTR(dwAddr1), SOS_PTR(objAddr+(size_t)parm-objAddr));

                                 return false;
                             }

                             if (IsMTForFreeObj(dwAddrMethTable))
                             {
                                 BuildError(reason, count, "Object %s contains free object %p at %p.", DMLObject(objAddr),
                                        SOS_PTR(dwAddr1), SOS_PTR(objAddr+(size_t)parm-objAddr));
                                 return false;
                             }

                             // verify card table
                             if (bCheckCard &&
                                 NeedCard (objAddr+(size_t)parm-objAddr,dwAddr1))
                             {
                                 BuildError(reason, count, "Object %s:%s missing card_table entry for %p",
                                        DMLObject(objAddr), (dwChild == dwAddr1) ? "" : " maybe",
                                        SOS_PTR(objAddr+(size_t)parm-objAddr));

                                 return false;
                             }
                         }
                    }
                   parm++;
                   CheckInterrupt();
                } while (parm < ppstop);
                parm = (BYTE**)((BYTE*)parm + skip);
            }
        }
    }

    return true;
}

bool sos::ObjectIterator::Verify(char *reason, size_t count) const
{
    try
    {
        TADDR mt = mCurrObj.GetMT();

        if (MethodTable::GetFreeMT() == mt)
        {
            return true;
        }

        size_t size = mCurrObj.GetSize();
        if (size < min_obj_size)
        {
            BuildError(reason, count, "Object %s: Size %d is too small.", DMLObject(mCurrObj.GetAddress()), size);
            return false;
        }
        
        if (mCurrObj.GetAddress() + mCurrObj.GetSize() > mSegmentEnd)
        {
            BuildError(reason, count, "Object %s is too large.  End of segment at %p.", DMLObject(mCurrObj), mSegmentEnd);
            return false;
        }
        
        BOOL bVerifyMember = TRUE;

        // If we requested to verify the object's members, the GC may be in a state where that's not possible.
        // Here we check to see if the object in question needs to have its members updated.  If so, we turn off
        // verification for the object.
        BOOL consider_bgc_mark = FALSE, check_current_sweep = FALSE, check_saved_sweep = FALSE;
        should_check_bgc_mark(mHeaps[mCurrHeap], mSegment, &consider_bgc_mark, &check_current_sweep, &check_saved_sweep);
        bVerifyMember = fgc_should_consider_object(mHeaps[mCurrHeap], mCurrObj.GetAddress(), mSegment,
                                                   consider_bgc_mark, check_current_sweep, check_saved_sweep);

        if (bVerifyMember)
            return VerifyObjectMembers(reason, count);
    }
    catch(const sos::Exception &e)
    {
        BuildError(reason, count, e.GetMesssage());
        return false;
    }

    return true;
}

bool sos::ObjectIterator::Verify() const
{
    char *c = NULL;
    return Verify(c, 0);
}
