// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
#include <assert.h>
#include "sos.h"
#include "safemath.h"


// This is the increment for the segment lookup data
const int nSegLookupStgIncrement = 100;

#define CCH_STRING_PREFIX_SUMMARY 64

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to update GC heap statistics.             *  
*                                                                      *
\**********************************************************************/
void HeapStat::Add(DWORD_PTR aData, DWORD aSize)
{
    if (head == 0)
    {
        head = new Node();
        if (head == NULL)
        {
            ReportOOM();
            ControlC = TRUE;
            return;
        }
        
        if (bHasStrings)
        {
            size_t capacity_pNew = _wcslen((WCHAR*)aData) + 1;
            WCHAR *pNew = new WCHAR[capacity_pNew];
            if (pNew == NULL)
            {
               ReportOOM();               
               ControlC = TRUE;
               return;
            }
            wcscpy_s(pNew, capacity_pNew, (WCHAR*)aData);
            aData = (DWORD_PTR)pNew;            
        }

        head->data = aData;
    }
    Node *walk = head;
    int cmp = 0;

    for (;;)
    {
        if (IsInterrupt())
            return;
        
        cmp = CompareData(aData, walk->data);            

        if (cmp == 0)
            break;
        
        if (cmp < 0)
        {
            if (walk->left == NULL)
                break;
            walk = walk->left;
        }
        else
        {
            if (walk->right == NULL)
                break;
            walk = walk->right;
        }
    }

    if (cmp == 0)
    {
        walk->count ++;
        walk->totalSize += aSize;
    }
    else
    {
        Node *node = new Node();
        if (node == NULL)
        {
            ReportOOM();                
            ControlC = TRUE;
            return;
        }

        if (bHasStrings)
        {
            size_t capacity_pNew = _wcslen((WCHAR*)aData) + 1;
            WCHAR *pNew = new WCHAR[capacity_pNew];
            if (pNew == NULL)
            {
               ReportOOM();
               ControlC = TRUE;
               return;
            }
            wcscpy_s(pNew, capacity_pNew, (WCHAR*)aData);
            aData = (DWORD_PTR)pNew;            
        }
        
        node->data = aData;       
        node->totalSize = aSize;
        node->count ++;

        if (cmp < 0)
        {
            walk->left = node;
        }
        else
        {
            walk->right = node;
        }
    }
}
/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function compares two nodes in the tree.     *  
*                                                                      *
\**********************************************************************/
int HeapStat::CompareData(DWORD_PTR d1, DWORD_PTR d2)
{
    if (bHasStrings)
        return _wcscmp((WCHAR*)d1, (WCHAR*)d2);

    if (d1 > d2)
        return 1;

    if (d1 < d2)
        return -1;

    return 0;   
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to sort all entries in the heap stat.     *  
*                                                                      *
\**********************************************************************/
void HeapStat::Sort ()
{
    Node *root = head;
    head = NULL;
    ReverseLeftMost (root);

    Node *sortRoot = NULL;
    while (head)
    {
        Node *tmp = head;
        head = head->left;
        if (tmp->right)
            ReverseLeftMost (tmp->right);
        // add tmp
        tmp->right = NULL;
        tmp->left = NULL;
        SortAdd (sortRoot, tmp);
    }
    head = sortRoot;

    Linearize();

    //reverse the order
    root = head;
    head = NULL;
    sortRoot = NULL;
    while (root)
    {
        Node *tmp = root->right;
        root->left = NULL;
        root->right = NULL;
        LinearAdd (sortRoot, root);
        root = tmp;
    }
    head = sortRoot;
}

void HeapStat::Linearize()
{
    // Change binary tree to a linear tree
    Node *root = head;
    head = NULL;
    ReverseLeftMost (root);
    Node *sortRoot = NULL;
    while (head)
    {
        Node *tmp = head;
        head = head->left;
        if (tmp->right)
            ReverseLeftMost (tmp->right);
        // add tmp
        tmp->right = NULL;
        tmp->left = NULL;
        LinearAdd (sortRoot, tmp);
    }
    head = sortRoot;
    fLinear = TRUE;
}

void HeapStat::ReverseLeftMost (Node *root)
{
    while (root)
    {
        Node *tmp = root->left;
        root->left = head;
        head = root;
        root = tmp;
    }
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to help to sort heap stat.                *  
*                                                                      *
\**********************************************************************/
void HeapStat::SortAdd (Node *&root, Node *entry)
{
    if (root == NULL)
    {
        root = entry;
    }
    else
    {
        Node *parent = root;
        Node *ptr = root;
        while (ptr)
        {
            parent = ptr;
            if (ptr->totalSize < entry->totalSize)
                ptr = ptr->right;
            else
                ptr = ptr->left;
        }
        if (parent->totalSize < entry->totalSize)
            parent->right = entry;
        else
            parent->left = entry;
    }
}

void HeapStat::LinearAdd(Node *&root, Node *entry)
{
    if (root == NULL)
    {
        root = entry;
    }
    else
    {
        entry->right = root;
        root = entry;
    }
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to print GC heap statistics.              *  
*                                                                      *
\**********************************************************************/
void HeapStat::Print(const char* label /* = NULL */)
{
    if (label == NULL)
    {
        label = "Statistics:\n";
    }
    ExtOut(label);
    if (bHasStrings)
        ExtOut("%8s %12s %s\n", "Count", "TotalSize", "String Value");
    else
        ExtOut("%" POINTERSIZE "s %8s %12s %s\n","MT", "Count", "TotalSize", "Class Name");

    Node *root = head;
    int ncount = 0;
    while (root)
    {
        if (IsInterrupt())
            return;
        
        ncount += root->count;

        if (bHasStrings)
        {
            ExtOut("%8d %12I64u \"%S\"\n", root->count, (unsigned __int64)root->totalSize, root->data);
        }
        else
        {
            DMLOut("%s %8d %12I64u ", DMLDumpHeapMT(root->data), root->count, (unsigned __int64)root->totalSize);
            if (IsMTForFreeObj(root->data))
            {
                ExtOut("%9s\n", "Free");
            }
            else
            {
                wcscpy_s(g_mdName, mdNameLen, W("UNKNOWN"));
                NameForMT_s((DWORD_PTR) root->data, g_mdName, mdNameLen);
                ExtOut("%S\n", g_mdName);
            }
        }
        root = root->right;
        
    }
    ExtOut ("Total %d objects\n", ncount);
}

void HeapStat::Delete()
{
    if (head == NULL)
        return;

    // Ensure the data structure is already linearized.
    if (!fLinear)
        Linearize();

    while (head)
    {
        // The list is linearized on such that the left node is always null.
        Node *tmp = head;
        head = head->right;

        if (bHasStrings)
            delete[] ((WCHAR*)tmp->data);
        delete tmp;
    }

    // return to default state
    bHasStrings = FALSE;
    fLinear = FALSE;
}

// -----------------------------------------------------------------------
//
// MethodTableCache implementation
//
// Used during heap traversals for quick object size computation
//
MethodTableInfo* MethodTableCache::Lookup (DWORD_PTR aData)
{
    Node** addHere = &head;
    if (head != 0) {
        Node *walk = head;
        int cmp = 0;

        for (;;)
        {
            cmp = CompareData(aData, walk->data);            

            if (cmp == 0)
                return &walk->info;
            
            if (cmp < 0)
            {
                if (walk->left == NULL) 
                {
                    addHere = &walk->left;
                    break;
                }
                walk = walk->left;
            }
            else
            {
                if (walk->right == NULL)
                {
                    addHere = &walk->right;
                    break;
                }
                walk = walk->right;
            }
        }
    }
    Node* newNode = new Node(aData);
    if (newNode == NULL)
    {
        ReportOOM();
        return NULL;
    }
    *addHere = newNode;
    return &newNode->info;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function compares two nodes in the tree.     *  
*                                                                      *
\**********************************************************************/
int MethodTableCache::CompareData(DWORD_PTR d1, DWORD_PTR d2)
{
    if (d1 > d2)
        return 1;

    if (d1 < d2)
        return -1;

    return 0;   
}

void MethodTableCache::ReverseLeftMost (Node *root)
{
    if (root)
    {
        if (root->left) ReverseLeftMost(root->left);
        if (root->right) ReverseLeftMost(root->right);
        delete root;
    }
}

void MethodTableCache::Clear()
{
    Node *root = head;
    head = NULL;
    ReverseLeftMost (root);
}

MethodTableCache g_special_mtCache;

size_t Align (size_t nbytes)
{
    return (nbytes + ALIGNCONST) & ~ALIGNCONST;
}

size_t AlignLarge(size_t nbytes)
{
    return (nbytes + ALIGNCONSTLARGE) & ~ALIGNCONSTLARGE;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    Print the gc heap info.                                           *  
*                                                                      *
\**********************************************************************/
void GCPrintGenerationInfo(const DacpGcHeapDetails &heap)
{
    UINT n;
    for (n = 0; n <= GetMaxGeneration(); n ++)
    {
        if (IsInterrupt())
            return;
        ExtOut("generation %d starts at 0x%p\n",
                 n, SOS_PTR(heap.generation_table[n].allocation_start));
    }

    // We also need to look at the gen0 alloc context.
    ExtOut("ephemeral segment allocation context: ");
    if (heap.generation_table[0].allocContextPtr)
    {
        ExtOut("(0x%p, 0x%p)\n",
            SOS_PTR(heap.generation_table[0].allocContextPtr),
            SOS_PTR(heap.generation_table[0].allocContextLimit + Align(min_obj_size)));
    }
    else
    {
        ExtOut("none\n");
    }
}


void GCPrintSegmentInfo(const DacpGcHeapDetails &heap, DWORD_PTR &total_size)
{
    DWORD_PTR dwAddrSeg;       
    DacpHeapSegmentData segment;
    
    dwAddrSeg = (DWORD_PTR)heap.generation_table[GetMaxGeneration()].start_segment;        
    total_size = 0;
    // the loop below will terminate, because we retrieved at most nMaxHeapSegmentCount segments
    while (dwAddrSeg != (DWORD_PTR)heap.generation_table[0].start_segment)
    {
        if (IsInterrupt())
            return;
        if (segment.Request(g_sos, dwAddrSeg, heap) != S_OK)
        {
            ExtOut("Error requesting heap segment %p\n", SOS_PTR(dwAddrSeg));
            return;
        }
        ExtOut("%p  %p  %p  0x%" POINTERSIZE_TYPE "x(%" POINTERSIZE_TYPE "d)\n", SOS_PTR(dwAddrSeg),
                 SOS_PTR(segment.mem), SOS_PTR(segment.allocated),
                 (ULONG_PTR)(segment.allocated - segment.mem),
                 (ULONG_PTR)(segment.allocated - segment.mem));
        total_size += (DWORD_PTR) (segment.allocated - segment.mem);
        dwAddrSeg = (DWORD_PTR)segment.next;
    }

    if (segment.Request(g_sos, dwAddrSeg, heap) != S_OK)
    {
        ExtOut("Error requesting heap segment %p\n", SOS_PTR(dwAddrSeg));
        return;
    }
    
    DWORD_PTR end = (DWORD_PTR)heap.alloc_allocated;
    ExtOut("%p  %p  %p  0x%" POINTERSIZE_TYPE "x(%" POINTERSIZE_TYPE "d)\n", SOS_PTR(dwAddrSeg),
             SOS_PTR(segment.mem), SOS_PTR(end),
             (ULONG_PTR)(end - (DWORD_PTR)segment.mem),
             (ULONG_PTR)(end - (DWORD_PTR)segment.mem));
    
    total_size += end - (DWORD_PTR)segment.mem;

}


void GCPrintLargeHeapSegmentInfo(const DacpGcHeapDetails &heap, DWORD_PTR &total_size)
{
    DWORD_PTR dwAddrSeg;
    DacpHeapSegmentData segment;
    dwAddrSeg = (DWORD_PTR)heap.generation_table[GetMaxGeneration()+1].start_segment;

    // total_size = 0;
    // the loop below will terminate, because we retrieved at most nMaxHeapSegmentCount segments
    while (dwAddrSeg != NULL)
    {
        if (IsInterrupt())
            return;
        if (segment.Request(g_sos, dwAddrSeg, heap) != S_OK)
        {
            ExtOut("Error requesting heap segment %p\n", SOS_PTR(dwAddrSeg));
            return;
        }
        ExtOut("%p  %p  %p  0x%" POINTERSIZE_TYPE "x(%" POINTERSIZE_TYPE "d)\n", SOS_PTR(dwAddrSeg),
                 SOS_PTR(segment.mem), SOS_PTR(segment.allocated),
                 (ULONG_PTR)(segment.allocated - segment.mem),
                 segment.allocated - segment.mem);
        total_size += (DWORD_PTR) (segment.allocated - segment.mem);
        dwAddrSeg = (DWORD_PTR)segment.next;
    }
}

void GCHeapInfo(const DacpGcHeapDetails &heap, DWORD_PTR &total_size)
{
    GCPrintGenerationInfo(heap);
    ExtOut("%" POINTERSIZE "s  %" POINTERSIZE "s  %" POINTERSIZE "s  %" POINTERSIZE "s\n", "segment", "begin", "allocated", "size");
    GCPrintSegmentInfo(heap, total_size);
    ExtOut("Large object heap starts at 0x%p\n",
                  SOS_PTR(heap.generation_table[GetMaxGeneration()+1].allocation_start));
    ExtOut("%" POINTERSIZE "s  %" POINTERSIZE "s  %" POINTERSIZE "s  %" POINTERSIZE "s\n", "segment", "begin", "allocated", "size");
    GCPrintLargeHeapSegmentInfo(heap,total_size);
}

BOOL GCObjInGeneration(TADDR taddrObj, const DacpGcHeapDetails &heap, 
    const TADDR_SEGINFO& /*seg*/, int& gen, TADDR_RANGE& allocCtx)
{
    gen = -1;
    for (UINT n = 0; n <= GetMaxGeneration(); n ++)
    {
        if (taddrObj >= TO_TADDR(heap.generation_table[n].allocation_start))
        {
            gen = n;
            break;
        }
    }

    // We also need to look at the gen0 alloc context.
    if (heap.generation_table[0].allocContextPtr 
        && taddrObj >= TO_TADDR(heap.generation_table[0].allocContextPtr) 
        && taddrObj < TO_TADDR(heap.generation_table[0].allocContextLimit) + Align(min_obj_size))
    {
        gen = 0;
        allocCtx.start = (TADDR)heap.generation_table[0].allocContextPtr;
        allocCtx.end   = (TADDR)heap.generation_table[0].allocContextLimit;
    }
    else
    {
        allocCtx.start = allocCtx.end = 0;
    }
    return (gen != -1);
}


BOOL GCObjInSegment(TADDR taddrObj, const DacpGcHeapDetails &heap, 
    TADDR_SEGINFO& rngSeg, int& gen, TADDR_RANGE& allocCtx)
{
    TADDR taddrSeg;       
    DacpHeapSegmentData dacpSeg;

    taddrSeg = (TADDR)heap.generation_table[GetMaxGeneration()].start_segment;        
    // the loop below will terminate, because we retrieved at most nMaxHeapSegmentCount segments
    while (taddrSeg != (TADDR)heap.generation_table[0].start_segment)
    {
        if (IsInterrupt())
            return FALSE;
        if (dacpSeg.Request(g_sos, taddrSeg, heap) != S_OK)
        {
            ExtOut("Error requesting heap segment %p\n", SOS_PTR(taddrSeg));
            return FALSE;
        }
        if (taddrObj >= TO_TADDR(dacpSeg.mem) && taddrObj < TO_TADDR(dacpSeg.allocated))
        {
            rngSeg.segAddr = (TADDR)dacpSeg.segmentAddr;
            rngSeg.start   = (TADDR)dacpSeg.mem;
            rngSeg.end     = (TADDR)dacpSeg.allocated;
            gen = 2;
            allocCtx.start = allocCtx.end = 0;
            return TRUE;
        }
        taddrSeg = (TADDR)dacpSeg.next;
    }

    // the ephemeral segment
    if (dacpSeg.Request(g_sos, taddrSeg, heap) != S_OK)
    {
        ExtOut("Error requesting heap segment %p\n", SOS_PTR(taddrSeg));
        return FALSE;
    }

    if (taddrObj >= TO_TADDR(dacpSeg.mem) && taddrObj < TO_TADDR(heap.alloc_allocated))
    {
        if (GCObjInGeneration(taddrObj, heap, rngSeg, gen, allocCtx))
        {
            rngSeg.segAddr = (TADDR)dacpSeg.segmentAddr;
            rngSeg.start   = (TADDR)dacpSeg.mem;
            rngSeg.end     = (TADDR)heap.alloc_allocated;
            return TRUE;
        }
    }

    return FALSE;
}

BOOL GCObjInLargeSegment(TADDR taddrObj, const DacpGcHeapDetails &heap, TADDR_SEGINFO& rngSeg)
{
    TADDR taddrSeg;
    DacpHeapSegmentData dacpSeg;
    taddrSeg = (TADDR)heap.generation_table[GetMaxGeneration()+1].start_segment;

    // the loop below will terminate, because we retrieved at most nMaxHeapSegmentCount segments
    while (taddrSeg != NULL)
    {
        if (IsInterrupt())
            return FALSE;
        if (dacpSeg.Request(g_sos, taddrSeg, heap) != S_OK)
        {
            ExtOut("Error requesting heap segment %p\n", SOS_PTR(taddrSeg));
            return FALSE;
        }
        if (taddrObj >= TO_TADDR(dacpSeg.mem) && taddrObj && taddrObj < TO_TADDR(dacpSeg.allocated))
        {
            rngSeg.segAddr = (TADDR)dacpSeg.segmentAddr;
            rngSeg.start   = (TADDR)dacpSeg.mem;
            rngSeg.end     = (TADDR)dacpSeg.allocated;
            return TRUE;
        }
        taddrSeg = (TADDR)dacpSeg.next;
    }
    return FALSE;
}

BOOL GCObjInHeap(TADDR taddrObj, const DacpGcHeapDetails &heap, 
    TADDR_SEGINFO& rngSeg, int& gen, TADDR_RANGE& allocCtx, BOOL &bLarge)
{
    if (GCObjInSegment(taddrObj, heap, rngSeg, gen, allocCtx))
    {
        bLarge = FALSE;
        return TRUE;
    }
    if (GCObjInLargeSegment(taddrObj, heap, rngSeg))
    {
        bLarge = TRUE;
        gen = GetMaxGeneration()+1;
        allocCtx.start = allocCtx.end = 0;
        return TRUE;
    }
    return FALSE;
}

#ifndef FEATURE_PAL
// this function updates genUsage to reflect statistics from the range defined by [start, end)
void GCGenUsageStats(TADDR start, TADDR end, const std::unordered_set<TADDR> &liveObjs,
    const DacpGcHeapDetails &heap, BOOL bLarge, const AllocInfo *pAllocInfo, GenUsageStat *genUsage)
{
    // if this is an empty segment or generation return
    if (start >= end)
    {
        return;
    }

    // otherwise it should start with a valid object
    _ASSERTE(sos::IsObject(start));

    // update the "allocd" field
    genUsage->allocd += end - start;

    size_t objSize = 0;
    for  (TADDR taddrObj = start; taddrObj < end; taddrObj += objSize)
    {
        TADDR  taddrMT;

        move_xp(taddrMT, taddrObj);
        taddrMT &= ~3;

        // skip allocation contexts
        if (!bLarge)
        {       
            // Is this the beginning of an allocation context?
            int i;
            for (i = 0; i < pAllocInfo->num; i ++)
            {
                if (taddrObj == (TADDR)pAllocInfo->array[i].alloc_ptr)
                {
                    ExtDbgOut("Skipping allocation context: [%#p-%#p)\n", 
                        SOS_PTR(pAllocInfo->array[i].alloc_ptr), SOS_PTR(pAllocInfo->array[i].alloc_limit));
                    taddrObj =
                        (TADDR)pAllocInfo->array[i].alloc_limit + Align(min_obj_size);
                    break;
                }
            }
            if (i < pAllocInfo->num)
            {
                // we already adjusted taddrObj, so reset objSize
                objSize = 0;
                continue;
            }

            // We also need to look at the gen0 alloc context.
            if (taddrObj == (DWORD_PTR) heap.generation_table[0].allocContextPtr)
            {
                taddrObj = (DWORD_PTR) heap.generation_table[0].allocContextLimit + Align(min_obj_size);
                // we already adjusted taddrObj, so reset objSize
                objSize = 0;
                continue;
            }

            // Are we at the end of gen 0?
            if (taddrObj == end - Align(min_obj_size))
            {
                objSize = 0;
                break;
            }
        }

        BOOL bContainsPointers;
        BOOL bMTOk = GetSizeEfficient(taddrObj, taddrMT, bLarge, objSize, bContainsPointers);
        if (!bMTOk)
        {
            ExtErr("bad object: %#p - bad MT %#p\n", SOS_PTR(taddrObj), SOS_PTR(taddrMT));
            // set objSize to size_t to look for the next valid MT
            objSize = sizeof(TADDR);
            continue;
        }

        // at this point we should have a valid objSize, and there whould be no 
        // integer overflow when moving on to next object in heap
        _ASSERTE(objSize > 0 && taddrObj < taddrObj + objSize);
        if (objSize == 0 || taddrObj > taddrObj + objSize)
        {
            break;
        }

        if (IsMTForFreeObj(taddrMT))
        {
            genUsage->freed += objSize;
        }
        else if (!(liveObjs.empty()) && liveObjs.find(taddrObj) == liveObjs.end())
        {
            genUsage->unrooted += objSize;
        }
    }
}
#endif // !FEATURE_PAL

BOOL GCHeapUsageStats(const DacpGcHeapDetails& heap, BOOL bIncUnreachable, HeapUsageStat *hpUsage)
{
    memset(hpUsage, 0, sizeof(*hpUsage));

    AllocInfo allocInfo;
    allocInfo.Init();

    // 1. Start with small object segments
    TADDR taddrSeg;       
    DacpHeapSegmentData dacpSeg;

    taddrSeg = (TADDR)heap.generation_table[GetMaxGeneration()].start_segment;

#ifndef FEATURE_PAL
    // this will create the bitmap of rooted objects only if bIncUnreachable is true
    GCRootImpl gcroot;
    std::unordered_set<TADDR> emptyLiveObjs;
    const std::unordered_set<TADDR> &liveObjs = (bIncUnreachable ? gcroot.GetLiveObjects() : emptyLiveObjs);
    
    // 1a. enumerate all non-ephemeral segments
    while (taddrSeg != (TADDR)heap.generation_table[0].start_segment)
    {
        if (IsInterrupt())
            return FALSE;

        if (dacpSeg.Request(g_sos, taddrSeg, heap) != S_OK)
        {
            ExtErr("Error requesting heap segment %p\n", SOS_PTR(taddrSeg));
            return FALSE;
        }
        GCGenUsageStats((TADDR)dacpSeg.mem, (TADDR)dacpSeg.allocated, liveObjs, heap, FALSE, &allocInfo, &hpUsage->genUsage[2]);
        taddrSeg = (TADDR)dacpSeg.next;
    }
#endif

    // 1b. now handle the ephemeral segment
    if (dacpSeg.Request(g_sos, taddrSeg, heap) != S_OK)
    {
        ExtErr("Error requesting heap segment %p\n", SOS_PTR(taddrSeg));
        return FALSE;
    }

    TADDR endGen = TO_TADDR(heap.alloc_allocated);
    for (UINT n = 0; n <= GetMaxGeneration(); n ++)
    {
        TADDR startGen;
        // gen 2 starts at the beginning of the segment
        if (n == GetMaxGeneration())
        {
            startGen = TO_TADDR(dacpSeg.mem);
        }
        else
        {
            startGen = TO_TADDR(heap.generation_table[n].allocation_start);
        }

#ifndef FEATURE_PAL
        GCGenUsageStats(startGen, endGen, liveObjs, heap, FALSE, &allocInfo, &hpUsage->genUsage[n]);
#endif
        endGen = startGen;
    }

    // 2. Now process LOH
    taddrSeg = (TADDR) heap.generation_table[GetMaxGeneration()+1].start_segment;
    while (taddrSeg != NULL)
    {
        if (IsInterrupt())
            return FALSE;

        if (dacpSeg.Request(g_sos, taddrSeg, heap) != S_OK)
        {
            ExtErr("Error requesting heap segment %p\n", SOS_PTR(taddrSeg));
            return FALSE;
        }

#ifndef FEATURE_PAL
        GCGenUsageStats((TADDR) dacpSeg.mem, (TADDR) dacpSeg.allocated, liveObjs, heap, TRUE, NULL, &hpUsage->genUsage[3]);
#endif
        taddrSeg = (TADDR)dacpSeg.next;
    }

    return TRUE;
}

DWORD GetNumComponents(TADDR obj)
{
    // The number of components is always the second pointer in the object.
    DWORD Value = NULL;
    HRESULT hr = MOVE(Value, obj + sizeof(size_t));

    // If we fail to read out the number of components, let's assume 0 so we don't try to
    // read further data from the object.
    if (FAILED(hr))
        return 0;

    // The component size on a String does not contain the trailing NULL character,
    // so we must add that ourselves.
    if(IsStringObject(obj))
        return Value+1;

    return Value;
}

BOOL GetSizeEfficient(DWORD_PTR dwAddrCurrObj, 
    DWORD_PTR dwAddrMethTable, BOOL bLarge, size_t& s, BOOL& bContainsPointers)
{
    // Remove lower bits in case we are in mark phase
    dwAddrMethTable = dwAddrMethTable & ~3;
    MethodTableInfo* info = g_special_mtCache.Lookup(dwAddrMethTable);
    if (!info->IsInitialized())        // An uninitialized entry
    {
        // this is the first time we see this method table, so we need to get the information
        // from the target
        DacpMethodTableData dmtd;
        // see code:ClrDataAccess::RequestMethodTableData for details
        if (dmtd.Request(g_sos,dwAddrMethTable) != S_OK)
            return FALSE;

        info->BaseSize = dmtd.BaseSize;
        info->ComponentSize = dmtd.ComponentSize;
        info->bContainsPointers = dmtd.bContainsPointers;
    }
        
    bContainsPointers = info->bContainsPointers;
    s = info->BaseSize;

    if (info->ComponentSize)
    {
        // this is an array, so the size has to include the size of the components. We read the number
        // of components from the target and multiply by the component size to get the size.
        s += info->ComponentSize*GetNumComponents(dwAddrCurrObj);
    }

    // On x64 we do an optimization to save 4 bytes in almost every string we create    
    // IMPORTANT: This cannot be done in ObjectSize, which is a wrapper to this function,
    //                    because we must Align only after these changes are made
#ifdef _TARGET_WIN64_
    // Pad to min object size if necessary
    if (s < min_obj_size)
        s = min_obj_size;
#endif // _TARGET_WIN64_

    s = (bLarge ? AlignLarge(s) : Align (s));
    return TRUE;
}

// This function expects stat to be valid, and ready to get statistics.
void GatherOneHeapFinalization(DacpGcHeapDetails& heapDetails, HeapStat *stat, BOOL bAllReady, BOOL bShort)
{
    DWORD_PTR dwAddr=0;    
    UINT m;

    if (!bShort)
    {
        for (m = 0; m <= GetMaxGeneration(); m ++)
        {
            if (IsInterrupt())
                return;
            
            ExtOut("generation %d has %d finalizable objects ", m, 
                (SegQueueLimit(heapDetails,gen_segment(m)) - SegQueue(heapDetails,gen_segment(m))) / sizeof(size_t));
            
            ExtOut ("(%p->%p)\n",
                SOS_PTR(SegQueue(heapDetails,gen_segment(m))),
                SOS_PTR(SegQueueLimit(heapDetails,gen_segment(m))));
        }
    }
#ifndef FEATURE_PAL
    if (bAllReady)
    {
        if (!bShort)
        {
            ExtOut ("Finalizable but not rooted:  ");
        }

        TADDR rngStart = (TADDR)SegQueue(heapDetails, gen_segment(GetMaxGeneration()));
        TADDR rngEnd   = (TADDR)SegQueueLimit(heapDetails, gen_segment(0));

        PrintNotReachableInRange(rngStart, rngEnd, TRUE, bAllReady ? stat : NULL, bShort);
    }
#endif

    if (!bShort)
    {
        ExtOut ("Ready for finalization %d objects ",
                (SegQueueLimit(heapDetails,FinalizerListSeg)-SegQueue(heapDetails,CriticalFinalizerListSeg)) / sizeof(size_t));
        ExtOut ("(%p->%p)\n",
                SOS_PTR(SegQueue(heapDetails,CriticalFinalizerListSeg)),
                SOS_PTR(SegQueueLimit(heapDetails,FinalizerListSeg)));
    }

    // if bAllReady we only count objects that are ready for finalization,
    // otherwise we count all finalizable objects.
    TADDR taddrLowerLimit = (bAllReady ? (TADDR)SegQueue(heapDetails, CriticalFinalizerListSeg) :
        (DWORD_PTR)SegQueue(heapDetails, gen_segment(GetMaxGeneration())));
    for (dwAddr = taddrLowerLimit;
         dwAddr < (DWORD_PTR)SegQueueLimit(heapDetails, FinalizerListSeg);
         dwAddr += sizeof (dwAddr)) 
    {
        if (IsInterrupt())
        {
            return;
        }
        
        DWORD_PTR objAddr = NULL, 
                  MTAddr = NULL;

        if (SUCCEEDED(MOVE(objAddr, dwAddr)) && SUCCEEDED(GetMTOfObject(objAddr, &MTAddr)) && MTAddr) 
        {
            if (bShort)
            {
                DMLOut("%s\n", DMLObject(objAddr));
            }
            else
            {
                size_t s = ObjectSize(objAddr);
                stat->Add(MTAddr, (DWORD)s);
            }
        }
    }
}

BOOL GCHeapTraverse(const DacpGcHeapDetails &heap, AllocInfo* pallocInfo, VISITGCHEAPFUNC pFunc, LPVOID token, BOOL verify)
{
    DWORD_PTR begin_youngest;
    DWORD_PTR end_youngest;    
    begin_youngest = (DWORD_PTR)heap.generation_table[0].allocation_start;
    DWORD_PTR dwAddr = (DWORD_PTR)heap.ephemeral_heap_segment;
    DacpHeapSegmentData segment;    
    
    end_youngest = (DWORD_PTR)heap.alloc_allocated;

    DWORD_PTR dwAddrSeg = (DWORD_PTR)heap.generation_table[GetMaxGeneration()].start_segment;
    dwAddr = dwAddrSeg;

    if (segment.Request(g_sos, dwAddr, heap) != S_OK)
    {
        ExtOut("Error requesting heap segment %p\n", SOS_PTR(dwAddr));
        return FALSE;
    }    
    
    // DWORD_PTR dwAddrCurrObj = (DWORD_PTR)heap.generation_table[GetMaxGeneration()].allocation_start;            
    DWORD_PTR dwAddrCurrObj = (DWORD_PTR)segment.mem;
    
    size_t s, sPrev=0;
    BOOL bPrevFree=FALSE;
    DWORD_PTR dwAddrMethTable;
    DWORD_PTR dwAddrPrevObj=0;

    while(1)
    {
        if (IsInterrupt())
        {
            ExtOut("<heap walk interrupted>\n");
            return FALSE;
        }
        DWORD_PTR end_of_segment = (DWORD_PTR)segment.allocated;
        if (dwAddrSeg == (DWORD_PTR)heap.ephemeral_heap_segment)
        {
            end_of_segment = end_youngest;
            if (dwAddrCurrObj - SIZEOF_OBJHEADER == end_youngest - Align(min_obj_size))
                break;
        }
        if (dwAddrCurrObj >= (DWORD_PTR)end_of_segment)
        {
            if (dwAddrCurrObj > (DWORD_PTR)end_of_segment)
            {
                ExtOut ("curr_object: %p > heap_segment_allocated (seg: %p)\n",
                         SOS_PTR(dwAddrCurrObj), SOS_PTR(dwAddrSeg));
                if (dwAddrPrevObj) {
                    ExtOut ("Last good object: %p\n", SOS_PTR(dwAddrPrevObj));
                }
                return FALSE;
            }
            dwAddrSeg = (DWORD_PTR)segment.next;
            if (dwAddrSeg)
            {
                dwAddr = dwAddrSeg;
                if (segment.Request(g_sos, dwAddr, heap) != S_OK)
                {
                    ExtOut("Error requesting heap segment %p\n", SOS_PTR(dwAddr));
                    return FALSE;
                }
                dwAddrCurrObj = (DWORD_PTR)segment.mem;
                continue;
            }
            else
                break;  // Done Verifying Heap
        }

        if (dwAddrSeg == (DWORD_PTR)heap.ephemeral_heap_segment
            && dwAddrCurrObj >= end_youngest)
        {
            if (dwAddrCurrObj > end_youngest)
            {
                // prev_object length is too long
                ExtOut("curr_object: %p > end_youngest: %p\n",
                         SOS_PTR(dwAddrCurrObj), SOS_PTR(end_youngest));
                if (dwAddrPrevObj) {
                    DMLOut("Last good object: %s\n", DMLObject(dwAddrPrevObj));
                }
                return FALSE;
            }
            return FALSE;
        }

        if (FAILED(GetMTOfObject(dwAddrCurrObj, &dwAddrMethTable)))
        {
            return FALSE;
        }
        
        dwAddrMethTable = dwAddrMethTable & ~3;
        if (dwAddrMethTable == 0)
        {
            // Is this the beginning of an allocation context?
            int i;
            for (i = 0; i < pallocInfo->num; i ++)
            {
                if (dwAddrCurrObj == (DWORD_PTR)pallocInfo->array[i].alloc_ptr)
                {
                    dwAddrCurrObj =
                        (DWORD_PTR)pallocInfo->array[i].alloc_limit + Align(min_obj_size);
                    break;
                }
            }
            if (i < pallocInfo->num)
                continue;

            // We also need to look at the gen0 alloc context.
            if (dwAddrCurrObj == (DWORD_PTR) heap.generation_table[0].allocContextPtr)
            {
                dwAddrCurrObj = (DWORD_PTR) heap.generation_table[0].allocContextLimit + Align(min_obj_size);
                continue;
            }
        }

        BOOL bContainsPointers;
        BOOL bMTOk = GetSizeEfficient(dwAddrCurrObj, dwAddrMethTable, FALSE, s, bContainsPointers);
        if (verify && bMTOk)
             bMTOk = VerifyObject (heap, dwAddrCurrObj, dwAddrMethTable, s, TRUE);
        if (!bMTOk)
        {
            DMLOut("curr_object:      %s\n", DMLListNearObj(dwAddrCurrObj));
            if (dwAddrPrevObj)
                DMLOut("Last good object: %s\n", DMLObject(dwAddrPrevObj));
            
            ExtOut ("----------------\n");            
            return FALSE;
        }

        pFunc (dwAddrCurrObj, s, dwAddrMethTable, token);

        // We believe we did this alignment in ObjectSize above.
        assert((s & ALIGNCONST) == 0);
        dwAddrPrevObj = dwAddrCurrObj;
        sPrev = s;
        bPrevFree = IsMTForFreeObj(dwAddrMethTable);
        
        dwAddrCurrObj += s;
    }

    // Now for the large object generation:
    dwAddrSeg = (DWORD_PTR)heap.generation_table[GetMaxGeneration()+1].start_segment;
    dwAddr = dwAddrSeg;    
    
    if (segment.Request(g_sos, dwAddr, heap) != S_OK)
    {
        ExtOut("Error requesting heap segment %p\n", SOS_PTR(dwAddr));
        return FALSE;
    }

    // dwAddrCurrObj = (DWORD_PTR)heap.generation_table[GetMaxGeneration()+1].allocation_start;            
    dwAddrCurrObj = (DWORD_PTR)segment.mem;
    
    dwAddrPrevObj=0;

    while(1)
    {
        if (IsInterrupt())
        {
            ExtOut("<heap traverse interrupted>\n");
            return FALSE;
        }

        DWORD_PTR end_of_segment = (DWORD_PTR)segment.allocated;

        if (dwAddrCurrObj >= (DWORD_PTR)end_of_segment)
        {
            if (dwAddrCurrObj > (DWORD_PTR)end_of_segment)
            {
                ExtOut("curr_object: %p > heap_segment_allocated (seg: %p)\n",
                         SOS_PTR(dwAddrCurrObj), SOS_PTR(dwAddrSeg));
                if (dwAddrPrevObj) {
                    ExtOut("Last good object: %p\n", SOS_PTR(dwAddrPrevObj));
                }
                return FALSE;
            }
            dwAddrSeg = (DWORD_PTR)segment.next;
            if (dwAddrSeg)
            {
                dwAddr = dwAddrSeg;
                if (segment.Request(g_sos, dwAddr, heap) != S_OK)
                {
                    ExtOut("Error requesting heap segment %p\n", SOS_PTR(dwAddr));
                    return FALSE;
                }
                dwAddrCurrObj = (DWORD_PTR)segment.mem;
                continue;
            }
            else
                break;  // Done Verifying Heap
        }

        if (FAILED(GetMTOfObject(dwAddrCurrObj, &dwAddrMethTable)))
        {
            return FALSE;
        }

        dwAddrMethTable = dwAddrMethTable & ~3;        
        BOOL bContainsPointers;
        BOOL bMTOk = GetSizeEfficient(dwAddrCurrObj, dwAddrMethTable, TRUE, s, bContainsPointers);
        if (verify && bMTOk)
            bMTOk = VerifyObject (heap, dwAddrCurrObj, dwAddrMethTable, s, TRUE);
        if (!bMTOk)
        {
            DMLOut("curr_object:      %s\n", DMLListNearObj(dwAddrCurrObj));

            if (dwAddrPrevObj)
                DMLOut("Last good object: %s\n", dwAddrPrevObj);
            
            ExtOut ("----------------\n");            
            return FALSE;
        }

        pFunc (dwAddrCurrObj, s, dwAddrMethTable, token);
        
        // We believe we did this alignment in ObjectSize above.
        assert((s & ALIGNCONSTLARGE) == 0);        
        dwAddrPrevObj = dwAddrCurrObj;
        dwAddrCurrObj += s;
    }

    return TRUE;
}

BOOL GCHeapsTraverse(VISITGCHEAPFUNC pFunc, LPVOID token, BOOL verify)
{
    // Obtain allocation context for each managed thread.    
    AllocInfo allocInfo;
    allocInfo.Init();

    if (!IsServerBuild())
    {
        DacpGcHeapDetails heapDetails;
        if (heapDetails.Request(g_sos) != S_OK)
        {
            ExtOut("Error requesting gc heap details\n");
            return FALSE;
        }

        return GCHeapTraverse (heapDetails, &allocInfo, pFunc, token, verify);
    }
    else
    {
        DacpGcHeapData gcheap;
        if (gcheap.Request(g_sos) != S_OK)
        {
            ExtOut("Error requesting GC Heap data\n");
            return FALSE;
        }

        DWORD dwAllocSize;
        DWORD dwNHeaps = gcheap.HeapCount;
        if (!ClrSafeInt<DWORD>::multiply(sizeof(CLRDATA_ADDRESS), dwNHeaps, dwAllocSize))
        {
            ExtOut("Failed to get GCHeaps:  integer overflow error\n");
            return FALSE;
        }
        CLRDATA_ADDRESS *heapAddrs = (CLRDATA_ADDRESS*)alloca(dwAllocSize);
        if (g_sos->GetGCHeapList(dwNHeaps, heapAddrs, NULL) != S_OK)
        {
            ExtOut("Failed to get GCHeaps\n");
            return FALSE;
        }
 
        DWORD n;
        for (n = 0; n < dwNHeaps; n ++)
        {
            DacpGcHeapDetails heapDetails;
            if (heapDetails.Request(g_sos, heapAddrs[n]) != S_OK)
            {
                ExtOut("Error requesting details\n");
                return FALSE;
            }

            if (!GCHeapTraverse (heapDetails, &allocInfo, pFunc, token, verify))
            {
                ExtOut("Traversing a gc heap failed\n");
                return FALSE;
            }
        }
    }

    return TRUE;
}

GCHeapSnapshot::GCHeapSnapshot() 
{ 
    m_isBuilt = FALSE; 
    m_heapDetails = NULL;    
}

///////////////////////////////////////////////////////////
SegmentLookup::SegmentLookup() 
{ 
    m_iSegmentsSize = m_iSegmentCount = 0; 

    m_segments = new DacpHeapSegmentData[nSegLookupStgIncrement];
    if (m_segments == NULL)
    {
        ReportOOM();
    }
    else 
    {
        m_iSegmentsSize = nSegLookupStgIncrement;
    }
}

BOOL SegmentLookup::AddSegment(DacpHeapSegmentData *pData)
{
    // appends the address of a new (initialized) instance of DacpHeapSegmentData to the list of segments
    // (m_segments) adding  space for a segment when necessary. 
    // @todo Microsoft: The field name m_iSegmentSize is a little misleading. It's not the size in bytes, 
    // but the number of elements allocated for the array. It probably should have been named something like 
    // m_iMaxSegments instead. 
    if (m_iSegmentCount >= m_iSegmentsSize)
    {
        // expand buffer--allocate enough space to hold the elements we already have plus nSegLookupStgIncrement
        // more elements
        DacpHeapSegmentData *pNewBuffer = new DacpHeapSegmentData[m_iSegmentsSize+nSegLookupStgIncrement];
        if (pNewBuffer==NULL)
            return FALSE;

        // copy the old elements into the new array
        memcpy(pNewBuffer, m_segments, sizeof(DacpHeapSegmentData)*m_iSegmentsSize);
        
        // record the new number of elements available
        m_iSegmentsSize+=nSegLookupStgIncrement;

        // delete the old array
        delete [] m_segments;

        // set m_segments to point to the new array
        m_segments = pNewBuffer;
    }
     
    // add pData to the array
    m_segments[m_iSegmentCount++] = *pData;        
    
    return TRUE;
}

SegmentLookup::~SegmentLookup()
{
    if (m_segments)
    {
        delete [] m_segments;
        m_segments = NULL;
    }
}

void SegmentLookup::Clear()
{
    m_iSegmentCount = 0;
}

CLRDATA_ADDRESS SegmentLookup::GetHeap(CLRDATA_ADDRESS object, BOOL& bFound)
{
    CLRDATA_ADDRESS ret = NULL;
    bFound = FALSE;
    
    // Visit our segments
    for (int i=0; i<m_iSegmentCount; i++)
    {
        if (TO_TADDR(m_segments[i].mem) <= TO_TADDR(object) && 
            TO_TADDR(m_segments[i].highAllocMark) > TO_TADDR(object))
        {
            ret = m_segments[i].gc_heap;
            bFound = TRUE;
            break;
        }
    }    

    return ret;
}

///////////////////////////////////////////////////////////////////////////

BOOL GCHeapSnapshot::Build()
{    
    Clear();
    
    m_isBuilt = FALSE;

    ///- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// 1. Get some basic information such as the heap type (SVR or WKS), how many heaps there are, mode and max generation
    /// (See code:ClrDataAccess::RequestGCHeapData)
    ///- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    if (m_gcheap.Request(g_sos) != S_OK)
    {
        ExtOut("Error requesting GC Heap data\n");
        return FALSE;
    }

    ArrayHolder<CLRDATA_ADDRESS> heapAddrs = NULL;

    ///- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// 2. Get a list of the addresses of the heaps when we have multiple heaps in server mode
    ///- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    if (m_gcheap.bServerMode)
    {
        UINT AllocSize;
        // allocate an array to hold the starting addresses of each heap when we're in server mode
        if (!ClrSafeInt<UINT>::multiply(sizeof(CLRDATA_ADDRESS), m_gcheap.HeapCount, AllocSize) ||
            (heapAddrs = new CLRDATA_ADDRESS [m_gcheap.HeapCount]) == NULL)
        {
            ReportOOM();                
            return FALSE;
        }

        // and initialize it with their addresses (see code:ClrDataAccess::RequestGCHeapList
        // for details)
        if (g_sos->GetGCHeapList(m_gcheap.HeapCount, heapAddrs, NULL) != S_OK)
        {
            ExtOut("Failed to get GCHeaps\n");
            return FALSE;
        }
    }

    ///- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    ///  3. Get some necessary information about each heap, such as the card table location, the generation
    ///  table, the heap bounds, etc., and retrieve the heap segments
    ///- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
 
    // allocate an array to hold the information
    m_heapDetails = new DacpGcHeapDetails[m_gcheap.HeapCount];

    if (m_heapDetails == NULL)
    {
        ReportOOM();        
        return FALSE;
    }
    
    // get the heap information for each heap
    // See code:ClrDataAccess::RequestGCHeapDetails for details
    for (UINT n = 0; n < m_gcheap.HeapCount; n ++)
    {        
        if (m_gcheap.bServerMode)
        {
            if (m_heapDetails[n].Request(g_sos, heapAddrs[n]) != S_OK)
            {
                ExtOut("Error requesting details\n");
                return FALSE;
            }
        }
        else
        {
            if (m_heapDetails[n].Request(g_sos) != S_OK)
            {
                ExtOut("Error requesting details\n");
                return FALSE;
            }
        }

        // now get information about the heap segments for this heap
        if (!AddSegments(m_heapDetails[n]))
        {
            ExtOut("Failed to retrieve segments for gc heap\n");
            return FALSE;
        }
    }

    m_isBuilt = TRUE;
    return TRUE; 
}

BOOL GCHeapSnapshot::AddSegments(DacpGcHeapDetails& details)
{
    int n = 0;
    DacpHeapSegmentData segment;
    
    // This array of two addresses gives us access to all the segments. The generation segments are linked
    // to each other, starting with the maxGeneration segment. The second address gives us the large object heap.
    CLRDATA_ADDRESS AddrSegs[] =
    {
        details.generation_table[GetMaxGeneration()].start_segment,
        details.generation_table[GetMaxGeneration()+1].start_segment // large object heap
    };

    // this loop will get information for all the heap segments in this heap. The outer loop iterates once
    // for the "normal" generation segments and once for the large object heap. The inner loop follows the chain
    // of segments rooted at AddrSegs[i]
    for (unsigned int i = 0; i < sizeof(AddrSegs)/sizeof(AddrSegs[0]); ++i)
    {
        CLRDATA_ADDRESS AddrSeg = AddrSegs[i];
        
        while (AddrSeg != NULL)
        {
            if (IsInterrupt())
            {
                return FALSE;
            }
            // Initialize segment by copying fields from the target's heap segment at AddrSeg. 
            // See code:ClrDataAccess::RequestGCHeapSegment for details. 
            if (segment.Request(g_sos, AddrSeg, details) != S_OK)
            {
                ExtOut("Error requesting heap segment %p\n", SOS_PTR(AddrSeg));
                return FALSE;
            }
            if (n++ > nMaxHeapSegmentCount) // that would be insane
            {
                ExtOut("More than %d heap segments, there must be an error\n", nMaxHeapSegmentCount);
                return FALSE;
            }

            // add the new segment to the array of segments. This will expand the array if necessary
            if (!m_segments.AddSegment(&segment))
            {
                ExtOut("strike: Failed to store segment\n");
                return FALSE;
            }        
            // get the next segment in the chain
            AddrSeg = segment.next;
        }
    }
    
    return TRUE;
}

void GCHeapSnapshot::Clear()
{
    if (m_heapDetails != NULL)
    {
        delete [] m_heapDetails;
        m_heapDetails = NULL;
    }

    m_segments.Clear();
    
    m_isBuilt = FALSE;
}

GCHeapSnapshot g_snapshot;

DacpGcHeapDetails *GCHeapSnapshot::GetHeap(CLRDATA_ADDRESS objectPointer)
{
    // We need bFound because heap will be NULL if we are Workstation Mode.
    // We still need a way to know if the address was found in our segment 
    // list.
    BOOL bFound = FALSE;
    CLRDATA_ADDRESS heap = m_segments.GetHeap(objectPointer, bFound);
    if (heap)
    {
        for (UINT i=0; i<m_gcheap.HeapCount; i++)
        {
            if (m_heapDetails[i].heapAddr == heap)
                return m_heapDetails + i;
        }    
    }
    else if (!m_gcheap.bServerMode)
    {
        if (bFound)
        {
            return m_heapDetails;
        }
    }
    
    // Not found
    return NULL;
}

// TODO: Do we need to handle the LOH here?
int GCHeapSnapshot::GetGeneration(CLRDATA_ADDRESS objectPointer)
{
    DacpGcHeapDetails *pDetails = GetHeap(objectPointer);
    if (pDetails == NULL)
    {
        ExtOut("Object %p has no generation\n", SOS_PTR(objectPointer));
        return 0;
    }

    TADDR taObj = TO_TADDR(objectPointer);
    // The DAC doesn't fill the generation table with true CLRDATA_ADDRESS values
    // but rather with ULONG64 values (i.e. non-sign-extended 64-bit values)
    // We use the TO_TADDR below to ensure we won't break if this will ever 
    // be fixed in the DAC.
    if (taObj >= TO_TADDR(pDetails->generation_table[0].allocation_start) &&
        taObj <= TO_TADDR(pDetails->alloc_allocated))
        return 0;

    if (taObj >= TO_TADDR(pDetails->generation_table[1].allocation_start) &&
        taObj <= TO_TADDR(pDetails->generation_table[0].allocation_start))
        return 1;
    
    return 2;
}


DWORD_PTR g_trav_totalSize = 0;
DWORD_PTR g_trav_wastedSize = 0;

void LoaderHeapTraverse(CLRDATA_ADDRESS blockData,size_t blockSize,BOOL blockIsCurrentBlock)
{
    DWORD_PTR dwAddr1;
    DWORD_PTR curSize = 0;
    char ch;
    for (dwAddr1 = (DWORD_PTR)blockData;
         dwAddr1 < (DWORD_PTR)blockData + blockSize;
         dwAddr1 += OSPageSize())
    {
        if (IsInterrupt())
            break;
        if (SafeReadMemory(dwAddr1, &ch, sizeof(ch), NULL))
        {
            curSize += OSPageSize();
        }
        else
            break;
    }
    
    if (!blockIsCurrentBlock)
    {
        g_trav_wastedSize  += blockSize  - curSize;
    }
    
    g_trav_totalSize += curSize;
    ExtOut("%p(%x:%x) ", SOS_PTR(blockData), blockSize, curSize);
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function prints out the size for various heaps.              *
*    total - the total size of the heap                                *
*    wasted - the amount of size wasted by the heap.                   *
*                                                                      *
\**********************************************************************/
void PrintHeapSize(DWORD_PTR total, DWORD_PTR wasted)
{
    ExtOut("Size: 0x%" POINTERSIZE_TYPE "x (%" POINTERSIZE_TYPE "lu) bytes", total, total);
    if (wasted)
        ExtOut(" total, 0x%" POINTERSIZE_TYPE "x (%" POINTERSIZE_TYPE "lu) bytes wasted", wasted,  wasted);    
    ExtOut(".\n");
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function prints out the size information for the JIT heap.   *
*                                                                      *
*    Returns: The size of this heap.                                   *
*                                                                      *
\**********************************************************************/
DWORD_PTR JitHeapInfo()
{
    // walk ExecutionManager__m_pJitList
    unsigned int count = 0;
    if (FAILED(g_sos->GetJitManagerList(0, NULL, &count)))
    {
        ExtOut("Unable to get JIT info\n");
        return 0;
    }

    ArrayHolder<DacpJitManagerInfo> pArray = new DacpJitManagerInfo[count];
    if (pArray==NULL)
    {
        ReportOOM();        
        return 0;
    }

    if (g_sos->GetJitManagerList(count, pArray, NULL) != S_OK)
    {
        ExtOut("Unable to get array of JIT Managers\n");
        return 0;
    }

    DWORD_PTR totalSize = 0;
    DWORD_PTR wasted = 0;

    for (unsigned int n=0; n < count; n++)
    {
        if (IsInterrupt())
            break;

        if (IsMiIL(pArray[n].codeType)) // JIT
        {
            unsigned int heapCount = 0;
            if (FAILED(g_sos->GetCodeHeapList(pArray[n].managerAddr, 0, NULL, &heapCount)))
            {
                ExtOut("Error getting EEJitManager code heaps\n");
                break;
            }

            if (heapCount > 0)
            {
                ArrayHolder<DacpJitCodeHeapInfo> codeHeapInfo = new DacpJitCodeHeapInfo[heapCount];
                if (codeHeapInfo == NULL)
                {
                    ReportOOM();                        
                    break;
                }

                if (g_sos->GetCodeHeapList(pArray[n].managerAddr, heapCount, codeHeapInfo, NULL) != S_OK)
                {
                    ExtOut("Unable to get code heap info\n");
                    break;
                }

                for (unsigned int iHeaps = 0; iHeaps < heapCount; iHeaps++)
                {
                    if (IsInterrupt())
                        break;

                    if (codeHeapInfo[iHeaps].codeHeapType == CODEHEAP_LOADER)
                    {
                        ExtOut("LoaderCodeHeap:    ");
                        totalSize += LoaderHeapInfo(codeHeapInfo[iHeaps].LoaderHeap, &wasted);
                    }
                    else if (codeHeapInfo[iHeaps].codeHeapType == CODEHEAP_HOST)
                    {
                        ExtOut("HostCodeHeap:      ");
                        ExtOut("%p ", SOS_PTR(codeHeapInfo[iHeaps].HostData.baseAddr));
                        DWORD dwSize = (DWORD)(codeHeapInfo[iHeaps].HostData.currentAddr - codeHeapInfo[iHeaps].HostData.baseAddr);
                        PrintHeapSize(dwSize, 0);
                        totalSize += dwSize;
                    }
                }
            }
        }
        else if (!IsMiNative(pArray[n].codeType)) // ignore native heaps for now
        {
            ExtOut("Unknown Jit encountered, ignored\n");
        }
    }

    ExtOut("Total size:        ");
    PrintHeapSize(totalSize, wasted);

    return totalSize;
}


/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function prints out the loader heap info for a single AD.    *
*    pLoaderHeapAddr - pointer to the loader heap                      *
*    wasted - a pointer to store the number of bytes wasted in this    *
*             VSDHeap (this pointer can be NULL)                       *
*                                                                      *
*    Returns: The size of this heap.                                   *
*                                                                      *
\**********************************************************************/
DWORD_PTR LoaderHeapInfo(CLRDATA_ADDRESS pLoaderHeapAddr, DWORD_PTR *wasted)
{
    g_trav_totalSize = 0;
    g_trav_wastedSize = 0;

    if (pLoaderHeapAddr)
        g_sos->TraverseLoaderHeap(pLoaderHeapAddr, LoaderHeapTraverse);

    PrintHeapSize(g_trav_totalSize, g_trav_wastedSize);
    
    if (wasted)
        *wasted += g_trav_wastedSize;
    return g_trav_totalSize;
}


/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function prints out the heap info for a single VSDHeap.      *
*    name - the name to print                                          *
*    type - the type of heap                                           *
*    appDomain - the app domain in which this resides                  *
*    wasted - a pointer to store the number of bytes wasted in this    *
*             VSDHeap (this pointer can be NULL)                       *
*                                                                      *
*    Returns: The size of this heap.                                   *
*                                                                      *
\**********************************************************************/
static DWORD_PTR PrintOneVSDHeap(const char *name, VCSHeapType type, CLRDATA_ADDRESS appDomain, DWORD_PTR *wasted)
{
    g_trav_totalSize = 0; g_trav_wastedSize = 0;

    ExtOut(name);
    g_sos->TraverseVirtCallStubHeap(appDomain, type, LoaderHeapTraverse);

    PrintHeapSize(g_trav_totalSize, g_trav_wastedSize);
    if (wasted)
        *wasted += g_trav_wastedSize;
    return g_trav_totalSize;
}


/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function prints out the heap info for VSDHeaps.              *
*    appDomain - The AppDomain to print info for.                      *
*    wasted - a pointer to store the number of bytes wasted in this    *
*             AppDomain (this pointer can be NULL)                     *
*                                                                      *
*    Returns: The size of this heap.                                   *
*                                                                      *
\**********************************************************************/
DWORD_PTR VSDHeapInfo(CLRDATA_ADDRESS appDomain, DWORD_PTR *wasted)
{
    DWORD_PTR totalSize = 0;

    if (appDomain)
    {
        totalSize += PrintOneVSDHeap("  IndcellHeap:     ", IndcellHeap, appDomain, wasted);
        totalSize += PrintOneVSDHeap("  LookupHeap:      ", LookupHeap, appDomain, wasted);
        totalSize += PrintOneVSDHeap("  ResolveHeap:     ", ResolveHeap, appDomain, wasted);
        totalSize += PrintOneVSDHeap("  DispatchHeap:    ", DispatchHeap, appDomain, wasted);
        totalSize += PrintOneVSDHeap("  CacheEntryHeap:  ", CacheEntryHeap, appDomain, wasted);
    }

    return totalSize;
}


/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*  This function prints out the heap info for a domain                 *
*    name - the name of the domain (to be printed)                     *
*    adPtr - a pointer to the AppDomain to print info about            *
*    outSize - a pointer to an int to store the size at (this may be   *
*              NULL)                                                   *
*    outWasted - a pointer to an int to store the number of bytes this *
*                domain is wasting (this may be NULL)                  *
*                                                                      *
*    returns: SUCCESS if we successfully printed out the domain heap   *
*             info, FAILED otherwise; if FAILED, outSize and           *
*             outWasted are untouched.                                 *
*                                                                      *
\**********************************************************************/
HRESULT PrintDomainHeapInfo(const char *name, CLRDATA_ADDRESS adPtr, DWORD_PTR *outSize, DWORD_PTR *outWasted)
{
    DacpAppDomainData appDomain;
    HRESULT hr = appDomain.Request(g_sos, adPtr);
    if (FAILED(hr))
    {
        ExtOut("Unable to get information for %s.\n", name);
        return hr;
    }

    ExtOut("--------------------------------------\n");
    
    const int column = 19;
    ExtOut("%s:", name);
    WhitespaceOut(column - (int)strlen(name) - 1);
    DMLOut("%s\n", DMLDomain(adPtr));

    DWORD_PTR domainHeapSize = 0;
    DWORD_PTR wasted = 0;

    ExtOut("LowFrequencyHeap:  ");
    domainHeapSize += LoaderHeapInfo(appDomain.pLowFrequencyHeap, &wasted);

    ExtOut("HighFrequencyHeap: ");
    domainHeapSize += LoaderHeapInfo(appDomain.pHighFrequencyHeap, &wasted);

    ExtOut("StubHeap:          ");
    domainHeapSize += LoaderHeapInfo(appDomain.pStubHeap, &wasted);

    ExtOut("Virtual Call Stub Heap:\n");
    domainHeapSize += VSDHeapInfo(appDomain.AppDomainPtr, &wasted);

    ExtOut("Total size:        ");
    PrintHeapSize(domainHeapSize, wasted);

    if (outSize)
        *outSize += domainHeapSize;
    if (outWasted)
        *outWasted += wasted;

    return hr;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function prints out the heap info for a list of modules.     *
*    moduleList - an array of modules                                  *
*    count - the number of modules in moduleList                       *
*    type - the type of heap                                           *
*    outWasted - a pointer to store the number of bytes wasted in this *
*                heap (this pointer can be NULL)                       *
*                                                                      *
*    Returns: The size of this heap.                                   *
*                                                                      *
\**********************************************************************/
DWORD_PTR PrintModuleHeapInfo(__out_ecount(count) DWORD_PTR *moduleList, int count, ModuleHeapType type, DWORD_PTR *outWasted)
{
    DWORD_PTR toReturn = 0;
    DWORD_PTR wasted = 0;
    
    if (IsMiniDumpFile())
    {
        ExtOut("<no information>\n");
    }
    else
    {
        DWORD_PTR thunkHeapSize = 0;    

        for (int i = 0; i < count; i++)
        {
            CLRDATA_ADDRESS addr = moduleList[i];
            DacpModuleData dmd;
            if (dmd.Request(g_sos, addr) != S_OK)
            {
                ExtOut("Unable to read module %p\n", SOS_PTR(addr));
            }
            else
            {
                DMLOut("Module %s: ", DMLModule(addr));
                CLRDATA_ADDRESS heap = type == ModuleHeapType_ThunkHeap ? dmd.pThunkHeap : dmd.pLookupTableHeap;
                thunkHeapSize += LoaderHeapInfo(heap, &wasted);
            }
        }

        ExtOut("Total size:      " WIN86_8SPACES);
        PrintHeapSize(thunkHeapSize, wasted);

        toReturn = thunkHeapSize;
    }

    if (outWasted)
        *outWasted += wasted;

    return toReturn;
}
