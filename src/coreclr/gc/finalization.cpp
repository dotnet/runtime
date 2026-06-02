// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

void gc_heap::schedule_finalizer_work (FinalizerWorkItem* callback)
{
    FinalizerWorkItem* prev;
    do
    {
        prev = finalizer_work;
        callback->next = prev;
    }
    while (Interlocked::CompareExchangePointer (&finalizer_work, callback, prev) != prev);

    if (prev == nullptr)
    {
        GCToEEInterface::EnableFinalization(true);
    }
}

#ifdef FEATURE_PREMORTEM_FINALIZATION
inline
unsigned int gen_segment (int gen)
{
    assert (((signed)total_generation_count - gen - 1)>=0);
    return (total_generation_count - gen - 1);
}

bool CFinalize::Initialize()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    const int INITIAL_FINALIZER_ARRAY_SIZE = 100;
    m_Array = new (nothrow)(Object*[INITIAL_FINALIZER_ARRAY_SIZE]);

    if (!m_Array)
    {
        ASSERT (m_Array);
        STRESS_LOG_OOM_STACK(sizeof(Object*[INITIAL_FINALIZER_ARRAY_SIZE]));
        if (GCConfig::GetBreakOnOOM())
        {
            GCToOSInterface::DebugBreak();
        }
        return false;
    }
    m_EndArray = &m_Array[INITIAL_FINALIZER_ARRAY_SIZE];

    for (int i =0; i < FreeList; i++)
    {
        SegQueueLimit (i) = m_Array;
    }
    m_PromotedCount = 0;
    lock = -1;
#ifdef _DEBUG
    lockowner_threadid.Clear();
#endif // _DEBUG

    return true;
}

CFinalize::~CFinalize()
{
    delete[] m_Array;
}

size_t CFinalize::GetPromotedCount ()
{
    return m_PromotedCount;
}

// An explanation of locking for finalization:
//
// Multiple threads allocate objects.  During the allocation, they are serialized by
// the AllocLock above.  But they release that lock before they register the object
// for finalization.  That's because there is much contention for the alloc lock, but
// finalization is presumed to be a rare case.
//
// So registering an object for finalization must be protected by the FinalizeLock.
//
// There is another logical queue that involves finalization.  When objects registered
// for finalization become unreachable, they are moved from the "registered" queue to
// the "unreachable" queue.  Note that this only happens inside a GC, so no other
// threads can be manipulating either queue at that time.  Once the GC is over and
// threads are resumed, the Finalizer thread will dequeue objects from the "unreachable"
// queue and call their finalizers.  This dequeue operation is also protected with
// the finalize lock.
//
// At first, this seems unnecessary.  Only one thread is ever enqueuing or dequeuing
// on the unreachable queue (either the GC thread during a GC or the finalizer thread
// when a GC is not in progress).  The reason we share a lock with threads enqueuing
// on the "registered" queue is that the "registered" and "unreachable" queues are
// interrelated.
//
// They are actually two regions of a longer list, which can only grow at one end.
// So to enqueue an object to the "registered" list, you actually rotate an unreachable
// object at the boundary between the logical queues, out to the other end of the
// unreachable queue -- where all growing takes place.  Then you move the boundary
// pointer so that the gap we created at the boundary is now on the "registered"
// side rather than the "unreachable" side.  Now the object can be placed into the
// "registered" side at that point.  This is much more efficient than doing moves
// of arbitrarily long regions, but it causes the two queues to require a shared lock.
//
// Notice that Enter/LeaveFinalizeLock is not a GC-aware spin lock.  Instead, it relies
// on the fact that the lock will only be taken for a brief period and that it will
// never provoke or allow a GC while the lock is held.  This is critical.  If the
// FinalizeLock used enter_spin_lock (and thus sometimes enters preemptive mode to
// allow a GC), then the Alloc client would have to GC protect a finalizable object
// to protect against that eventuality.  That is too slow!
inline
void CFinalize::EnterFinalizeLock()
{
    _ASSERTE(dbgOnly_IsSpecialEEThread() ||
             GCToEEInterface::GetThread() == 0 ||
             GCToEEInterface::IsPreemptiveGCDisabled());

retry:
    if (Interlocked::CompareExchange(&lock, 0, -1) >= 0)
    {
        unsigned int i = 0;
        while (lock >= 0)
        {
            if (g_num_processors > 1)
            {
                int spin_count = 128 * yp_spin_count_unit;
                for (int j = 0; j < spin_count; j++)
                {
                    if (lock < 0)
                        break;
                    // give the HT neighbor a chance to run
                    YieldProcessor ();
                }
            }
            if (lock < 0)
                break;
            if (++i & 7)
                GCToOSInterface::YieldThread (0);
            else
                GCToOSInterface::Sleep (5);
        }
        goto retry;
    }

#ifdef _DEBUG
    lockowner_threadid.SetToCurrentThread();
#endif // _DEBUG
}

inline
void CFinalize::LeaveFinalizeLock()
{
    _ASSERTE(dbgOnly_IsSpecialEEThread() ||
             GCToEEInterface::GetThread() == 0 ||
             GCToEEInterface::IsPreemptiveGCDisabled());

#ifdef _DEBUG
    lockowner_threadid.Clear();
#endif // _DEBUG
    lock = -1;
}

bool
CFinalize::RegisterForFinalization (int gen, Object* obj, size_t size)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    EnterFinalizeLock();

    // Adjust gen
    unsigned int dest = gen_segment (gen);

    // Adjust boundary for segments so that GC will keep objects alive.
    Object*** s_i = &SegQueue (FreeListSeg);
    if ((*s_i) == SegQueueLimit(FreeListSeg))
    {
        if (!GrowArray())
        {
            LeaveFinalizeLock();
            if (method_table(obj) == NULL)
            {
                // If the object is uninitialized, a valid size should have been passed.
                assert (size >= Align (min_obj_size));
                dprintf (3, (ThreadStressLog::gcMakeUnusedArrayMsg(), (size_t)obj, (size_t)(obj+size)));
                ((CObjectHeader*)obj)->SetFree(size);
            }
            STRESS_LOG_OOM_STACK(0);
            if (GCConfig::GetBreakOnOOM())
            {
                GCToOSInterface::DebugBreak();
            }
            return false;
        }
    }
    Object*** end_si = &SegQueueLimit (dest);
    do
    {
        //is the segment empty?
        if (!(*s_i == *(s_i-1)))
        {
            //no, move the first element of the segment to the (new) last location in the segment
            *(*s_i) = *(*(s_i-1));
        }
        //increment the fill pointer
        (*s_i)++;
        //go to the next segment.
        s_i--;
    } while (s_i > end_si);

    // We have reached the destination segment
    // store the object
    **s_i = obj;
    // increment the fill pointer
    (*s_i)++;

    LeaveFinalizeLock();

    return true;
}

Object*
CFinalize::GetNextFinalizableObject (BOOL only_non_critical)
{
    Object* obj = 0;
    EnterFinalizeLock();

    if (!IsSegEmpty(FinalizerListSeg))
    {
        obj =  *(--SegQueueLimit (FinalizerListSeg));
    }
    else if (!only_non_critical && !IsSegEmpty(CriticalFinalizerListSeg))
    {
        //the FinalizerList is empty, we can adjust both
        // limit instead of moving the object to the free list
        obj =  *(--SegQueueLimit (CriticalFinalizerListSeg));
        --SegQueueLimit (FinalizerListSeg);
    }
    if (obj)
    {
        dprintf (3, ("running finalizer for %p (mt: %p)", obj, method_table (obj)));
    }
    LeaveFinalizeLock();
    return obj;
}

size_t
CFinalize::GetNumberFinalizableObjects()
{
    return SegQueueLimit(FinalizerMaxSeg) - SegQueue(FinalizerStartSeg);
}

void
CFinalize::MoveItem (Object** fromIndex,
                     unsigned int fromSeg,
                     unsigned int toSeg)
{

    int step;
    ASSERT (fromSeg != toSeg);
    if (fromSeg > toSeg)
        step = -1;
    else
        step = +1;
    // Each iteration places the element at the boundary closest to dest
    // and then adjusts the boundary to move that element one segment closer
    // to dest.
    Object** srcIndex = fromIndex;
    for (unsigned int i = fromSeg; i != toSeg; i+= step)
    {
        // Select SegQueue[i] for step==-1, SegQueueLimit[i] for step==1
        Object**& destFill = m_FillPointers[i+(step - 1 )/2];
        // Select SegQueue[i] for step==-1, SegQueueLimit[i]-1 for step==1
        //   (SegQueueLimit[i]-1 is the last entry in segment i)
        Object** destIndex = destFill - (step + 1)/2;
        if (srcIndex != destIndex)
        {
            Object* tmp = *srcIndex;
            *srcIndex = *destIndex;
            *destIndex = tmp;
        }
        destFill -= step;
        srcIndex = destIndex;
    }
}

void
CFinalize::GcScanRoots (promote_func* fn, int hn, ScanContext *pSC)
{
    ScanContext sc;
    if (pSC == 0)
        pSC = &sc;

    pSC->thread_number = hn;

    //scan the finalization queue
    Object** startIndex  = SegQueue (FinalizerStartSeg);
    Object** stopIndex  = SegQueueLimit (FinalizerMaxSeg);

    for (Object** po = startIndex; po < stopIndex; po++)
    {
        Object* o = *po;
        //dprintf (3, ("scan freacheable %zx", (size_t)o));
        dprintf (3, ("scan f %zx", (size_t)o));

        (*fn)(po, pSC, 0);
    }
}

void CFinalize::WalkFReachableObjects (fq_walk_fn fn)
{
    Object** startIndex = SegQueue (FinalizerListSeg);
    Object** stopIndex = SegQueueLimit (FinalizerListSeg);
    for (Object** po = startIndex; po < stopIndex; po++)
    {
        bool isCriticalFinalizer = false;
        fn(isCriticalFinalizer, *po);
    }

    startIndex = SegQueue (CriticalFinalizerListSeg);
    stopIndex = SegQueueLimit (CriticalFinalizerListSeg);
    for (Object** po = startIndex; po < stopIndex; po++)
    {
        bool isCriticalFinalizer = true;
        fn(isCriticalFinalizer, *po);
    }
}

BOOL
CFinalize::ScanForFinalization (promote_func* pfn, int gen, gc_heap* hp)
{
    ScanContext sc;
    sc.promotion = TRUE;
#ifdef MULTIPLE_HEAPS
    sc.thread_number = hp->heap_number;
    sc.thread_count = gc_heap::n_heaps;
#else
    UNREFERENCED_PARAMETER(hp);
    sc.thread_count = 1;
#endif //MULTIPLE_HEAPS

    BOOL finalizedFound = FALSE;

    //start with gen and explore all the younger generations.
    unsigned int startSeg = gen_segment (gen);
    {
        m_PromotedCount = 0;
        for (unsigned int Seg = startSeg; Seg <= gen_segment(0); Seg++)
        {
            Object** endIndex = SegQueue (Seg);
            for (Object** i = SegQueueLimit (Seg)-1; i >= endIndex ;i--)
            {
                CObjectHeader* obj = (CObjectHeader*)*i;
                dprintf (3, ("scanning: %zx", (size_t)obj));
                if (!g_theGCHeap->IsPromoted (obj))
                {
                    dprintf (3, ("freacheable: %zx", (size_t)obj));

                    assert (method_table(obj)->HasFinalizer());

                    if (GCToEEInterface::EagerFinalized(obj))
                    {
                        MoveItem (i, Seg, FreeListSeg);
                    }
                    else if ((obj->GetHeader()->GetBits()) & BIT_SBLK_FINALIZER_RUN)
                    {
                        //remove the object because we don't want to
                        //run the finalizer
                        MoveItem (i, Seg, FreeListSeg);

                        //Reset the bit so it will be put back on the queue
                        //if resurrected and re-registered.
                        obj->GetHeader()->ClrBit (BIT_SBLK_FINALIZER_RUN);

                    }
                    else
                    {
                        m_PromotedCount++;

                        if (method_table(obj)->HasCriticalFinalizer())
                        {
                            MoveItem (i, Seg, CriticalFinalizerListSeg);
                        }
                        else
                        {
                            MoveItem (i, Seg, FinalizerListSeg);
                        }
                    }
                }
#ifdef BACKGROUND_GC
                else
                {
                    if ((gen == max_generation) && (gc_heap::background_running_p()))
                    {
                        // TODO - fix the following line.
                        //assert (gc_heap::background_object_marked ((uint8_t*)obj, FALSE));
                        dprintf (3, ("%zx is marked", (size_t)obj));
                    }
                }
#endif //BACKGROUND_GC
            }
        }
    }
    finalizedFound = !IsSegEmpty(FinalizerListSeg) ||
                     !IsSegEmpty(CriticalFinalizerListSeg);

    if (finalizedFound)
    {
        //Promote the f-reachable objects
        GcScanRoots (pfn,
#ifdef MULTIPLE_HEAPS
                     hp->heap_number
#else
                     0
#endif //MULTIPLE_HEAPS
                     , 0);

        hp->settings.found_finalizers = TRUE;

#ifdef BACKGROUND_GC
        if (hp->settings.concurrent)
        {
            hp->settings.found_finalizers = !(IsSegEmpty(FinalizerListSeg) && IsSegEmpty(CriticalFinalizerListSeg));
        }
#endif //BACKGROUND_GC
        if (hp->settings.concurrent && hp->settings.found_finalizers)
        {
            GCToEEInterface::EnableFinalization(true);
        }
    }

    return finalizedFound;
}

//Relocates all of the objects in the finalization array
void
CFinalize::RelocateFinalizationData (int gen, gc_heap* hp)
{
    ScanContext sc;
    sc.promotion = FALSE;
#ifdef MULTIPLE_HEAPS
    sc.thread_number = hp->heap_number;
    sc.thread_count = gc_heap::n_heaps;
#else
    UNREFERENCED_PARAMETER(hp);
    sc.thread_count = 1;
#endif //MULTIPLE_HEAPS

    unsigned int Seg = gen_segment (gen);

    Object** startIndex = SegQueue (Seg);

    dprintf (3, ("RelocateFinalizationData gen=%d, [%p,%p[", gen, startIndex, SegQueue (FreeList)));

    for (Object** po = startIndex; po < SegQueue (FreeList);po++)
    {
        GCHeap::Relocate (po, &sc);
    }
}

void
CFinalize::UpdatePromotedGenerations (int gen, BOOL gen_0_empty_p)
{
    dprintf(3, ("UpdatePromotedGenerations gen=%d, gen_0_empty_p=%d", gen, gen_0_empty_p));

    // update the generation fill pointers.
    // if gen_0_empty is FALSE, test each object to find out if
    // it was promoted or not
    if (gen_0_empty_p)
    {
        for (int i = min (gen+1, (int)max_generation); i > 0; i--)
        {
            m_FillPointers [gen_segment(i)] = m_FillPointers [gen_segment(i-1)];
        }
    }
    else
    {
        //Look for demoted or promoted objects
        for (int i = gen; i >= 0; i--)
        {
            unsigned int Seg = gen_segment (i);
            Object** startIndex = SegQueue (Seg);

            for (Object** po = startIndex;
                 po < SegQueueLimit (gen_segment(i)); po++)
            {
                int new_gen = g_theGCHeap->WhichGeneration (*po);
                if (new_gen != i)
                {
                    // We never promote objects to a non-GC heap
                    assert (new_gen <= max_generation);

                    dprintf (3, ("Moving object %p->%p from gen %d to gen %d", po, *po, i, new_gen));

                    if (new_gen > i)
                    {
                        //promotion
                        MoveItem (po, gen_segment (i), gen_segment (new_gen));
                    }
                    else
                    {
                        //demotion
                        MoveItem (po, gen_segment (i), gen_segment (new_gen));
                        //back down in order to see all objects.
                        po--;
                    }
                }
            }
        }
    }
}

BOOL
CFinalize::GrowArray()
{
    size_t oldArraySize = (m_EndArray - m_Array);
    size_t newArraySize =  (size_t)(((float)oldArraySize / 10) * 12);

    Object** newArray = new (nothrow) Object*[newArraySize];
    if (!newArray)
    {
        return FALSE;
    }
    memcpy (newArray, m_Array, oldArraySize*sizeof(Object*));

    dprintf (3, ("Grow finalizer array [%p,%p[ -> [%p,%p[", m_Array, m_EndArray, newArray, &m_Array[newArraySize]));

    //adjust the fill pointers
    for (int i = 0; i < FreeList; i++)
    {
        m_FillPointers [i] += (newArray - m_Array);
    }
    delete[] m_Array;
    m_Array = newArray;
    m_EndArray = &m_Array [newArraySize];

    return TRUE;
}

// merge finalization data from another queue into this one
// return false in case of failure - in this case, move no items
bool CFinalize::MergeFinalizationData (CFinalize* other_fq)
{
    // compute how much space we will need for the merged data
    size_t otherNeededArraySize = other_fq->UsedCount();
    if (otherNeededArraySize == 0)
    {
        // the other queue is empty - nothing to do!
        return true;
    }
    size_t thisArraySize = (m_EndArray - m_Array);
    size_t thisNeededArraySize = UsedCount();
    size_t neededArraySize = thisNeededArraySize + otherNeededArraySize;

    Object ** newArray = m_Array;

    // check if the space we have is sufficient
    if (thisArraySize < neededArraySize)
    {
        // if not allocate new array
        newArray = new (nothrow) Object*[neededArraySize];

        // if unsuccessful, return false without changing anything
        if (!newArray)
        {
            dprintf (3, ("ran out of space merging finalization data"));
            return false;
        }
    }

    // Since the target might be the original array (with the original data),
    // the order of copying must not overwrite any data until it has been
    // copied.

    // copy the finalization data from this and the other finalize queue
    for (int i = FreeList - 1; i >= 0; i--)
    {
        size_t thisIndex = SegQueue (i) - m_Array;
        size_t otherIndex = other_fq->SegQueue (i) - other_fq->m_Array;
        size_t thisLimit = SegQueueLimit (i) - m_Array;
        size_t otherLimit = other_fq->SegQueueLimit (i) - other_fq->m_Array;
        size_t thisSize = thisLimit - thisIndex;
        size_t otherSize = otherLimit - otherIndex;

        memmove (&newArray[thisIndex + otherIndex],           &m_Array[thisIndex ], sizeof(newArray[0])*thisSize );
        memmove (&newArray[thisLimit + otherIndex], &other_fq->m_Array[otherIndex], sizeof(newArray[0])*otherSize);
    }

    // adjust the m_FillPointers to reflect the sum of both queues on this queue,
    // and reflect that the other queue is now empty
    for (int i = FreeList - 1; i >= 0; i--)
    {
        size_t thisLimit = SegQueueLimit (i) - m_Array;
        size_t otherLimit = other_fq->SegQueueLimit (i) - other_fq->m_Array;

        SegQueueLimit (i) = &newArray[thisLimit + otherLimit];

        other_fq->SegQueueLimit (i) = other_fq->m_Array;
    }
    if (m_Array != newArray)
    {
        delete[] m_Array;
        m_Array = newArray;
        m_EndArray = &m_Array [neededArraySize];
    }
    return true;
}

// split finalization data from this queue with another queue
// return false in case of failure - in this case, move no items
bool CFinalize::SplitFinalizationData (CFinalize* other_fq)
{
    // the other finalization queue is assumed to be empty at this point
    size_t otherCurrentArraySize = other_fq->UsedCount();
    assert (otherCurrentArraySize == 0);

    size_t thisCurrentArraySize = UsedCount();
    if (thisCurrentArraySize == 0)
    {
        // this queue is empty - nothing to split!
        return true;
    }

    size_t otherNeededArraySize = thisCurrentArraySize / 2;

    // do we have a big enough array allocated on the other queue to move the intended size?
    size_t otherArraySize = other_fq->m_EndArray - other_fq->m_Array;
    if (otherArraySize < otherNeededArraySize)
    {
        // if not, allocate new array
        Object ** newArray = new (nothrow) Object*[otherNeededArraySize];
        if (!newArray)
        {
            // if unsuccessful, return false without changing anything
            return false;
        }
        delete[] other_fq->m_Array;
        other_fq->m_Array = newArray;
        other_fq->m_EndArray = &other_fq->m_Array[otherNeededArraySize];
    }

    // move half of the items in each section over to the other queue
    PTR_PTR_Object newFillPointers[MaxSeg];
    PTR_PTR_Object segQueue = m_Array;
    for (int i = 0; i < FreeList; i++)
    {
        size_t thisIndex = SegQueue (i) - m_Array;
        size_t thisLimit = SegQueueLimit (i) - m_Array;
        size_t thisSize = thisLimit - thisIndex;

        // we move half to the other queue
        size_t otherSize = thisSize / 2;
        size_t otherIndex = other_fq->SegQueue (i) - other_fq->m_Array;
        size_t thisNewSize = thisSize - otherSize;

        memmove (&other_fq->m_Array[otherIndex], &m_Array[thisIndex + thisNewSize], sizeof(other_fq->m_Array[0])*otherSize);
        other_fq->SegQueueLimit (i) = &other_fq->m_Array[otherIndex + otherSize];

        // slide the unmoved half to its new position in the queue
        // (this will delete the moved half once copies and m_FillPointers updates are completed)
        memmove (segQueue, &m_Array[thisIndex], sizeof(m_Array[0])*thisNewSize);
        segQueue += thisNewSize;
        newFillPointers[i] = segQueue;
    }

    // finally update the fill pointers from the new copy we generated
    for (int i = 0; i < MaxSeg; i++)
    {
        m_FillPointers[i] = newFillPointers[i];
    }

    return true;
}

#ifdef VERIFY_HEAP
void CFinalize::CheckFinalizerObjects()
{
    for (int i = 0; i <= max_generation; i++)
    {
        Object **startIndex = SegQueue (gen_segment (i));
        Object **stopIndex  = SegQueueLimit (gen_segment (i));

        for (Object **po = startIndex; po < stopIndex; po++)
        {
            if ((int)g_theGCHeap->WhichGeneration (*po) < i)
                FATAL_GC_ERROR ();
            ((CObjectHeader*)*po)->Validate();
        }
    }
}

#endif //VERIFY_HEAP
#endif //FEATURE_PREMORTEM_FINALIZATION

void gc_heap::walk_finalize_queue (fq_walk_fn fn)
{
#ifdef FEATURE_PREMORTEM_FINALIZATION
    finalize_queue->WalkFReachableObjects (fn);
#endif //FEATURE_PREMORTEM_FINALIZATION
}
