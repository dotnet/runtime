// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;

using Debug = System.Diagnostics.Debug;

// Rewrite of src\coreclr\vm\frozenobjectheap.cpp in C#

namespace Internal.Runtime
{
    internal unsafe partial class FrozenObjectHeapManager
    {
        public static readonly FrozenObjectHeapManager Instance = new FrozenObjectHeapManager();

        private readonly Lock m_Crst = new Lock(useTrivialWaits: true);
        private FrozenObjectSegment m_CurrentSegment;

        // Default size to reserve for a frozen segment
        private const nuint FOH_SEGMENT_DEFAULT_SIZE = 4 * 1024 * 1024;
        // Size to commit on demand in that reserved space
        private const nuint FOH_COMMIT_SIZE = 64 * 1024;

        public T? TryAllocateObject<T>() where T : class
        {
            MethodTable* pMT = MethodTable.Of<T>();
            return Unsafe.As<T?>(TryAllocateObject(pMT, pMT->BaseSize));
        }

        private object? TryAllocateObject(MethodTable* type, nuint objectSize)
        {
            HalfBakedObject* obj = null;

            using (m_Crst.EnterScope())
            {
                Debug.Assert(type != null);
                // _ASSERT(FOH_COMMIT_SIZE >= MIN_OBJECT_SIZE);

                // Currently we don't support frozen objects with special alignment requirements
                // TODO: We should also give up on arrays of doubles on 32-bit platforms.
                // (we currently never allocate them on frozen segments)
#if FEATURE_64BIT_ALIGNMENT
                if (type->RequiresAlign8)
                {
                    // Align8 objects are not supported yet
                    return null;
                }
#endif

                // NOTE: objectSize is expected be the full size including header
                // _ASSERT(objectSize >= MIN_OBJECT_SIZE);

                if (objectSize > FOH_COMMIT_SIZE)
                {
                    // The current design doesn't allow objects larger than FOH_COMMIT_SIZE and
                    // since FrozenObjectHeap is just an optimization, let's not fill it with huge objects.
                    return null;
                }

                obj = m_CurrentSegment == null ? null : m_CurrentSegment.TryAllocateObject(type, objectSize);
                // obj is nullptr if the current segment is full or hasn't been allocated yet
                if (obj == null)
                {
                    nuint newSegmentSize = FOH_SEGMENT_DEFAULT_SIZE;
                    if (m_CurrentSegment != null)
                    {
                        // Double the reserved size to reduce the number of frozen segments in apps with lots of frozen objects
                        // Use the same size in case if prevSegmentSize*2 operation overflows.
                        nuint prevSegmentSize = m_CurrentSegment.m_Size;
                        newSegmentSize = Math.Max(prevSegmentSize, prevSegmentSize * 2);
                    }

                    m_CurrentSegment = new FrozenObjectSegment(newSegmentSize);

                    // Try again
                    obj = m_CurrentSegment.TryAllocateObject(type, objectSize);

                    // This time it's not expected to be null
                    Debug.Assert(obj != null);
                }
            } // end of m_Crst lock

            IntPtr result = (IntPtr)obj;

            return Unsafe.As<IntPtr, object>(ref result);
        }

        private class FrozenObjectSegment
        {
            // Start of the reserved memory, the first object starts at "m_pStart + sizeof(ObjHeader)" (its pMT)
            private byte* m_pStart;

            // Pointer to the end of the current segment, ready to be used as a pMT for a new object
            // meaning that "m_pCurrent - sizeof(ObjHeader)" is the actual start of the new object (header).
            //
            // m_pCurrent <= m_SizeCommitted
            public byte* m_pCurrent;

            // Memory committed in the current segment
            //
            // m_SizeCommitted <= m_pStart + FOH_SIZE_RESERVED
            public nuint m_SizeCommitted;

            // Total memory reserved for the current segment
            public nuint m_Size;

            private IntPtr m_SegmentHandle;

            public FrozenObjectSegment(nuint sizeHint)
            {
                m_Size = sizeHint;

                Debug.Assert(m_Size > FOH_COMMIT_SIZE);
                Debug.Assert(m_Size % FOH_COMMIT_SIZE == 0);

                void* alloc = ClrVirtualReserve(m_Size);
                if (alloc == null)
                {
                    // Try again with the default FOH size
                    if (m_Size > FOH_SEGMENT_DEFAULT_SIZE)
                    {
                        m_Size = FOH_SEGMENT_DEFAULT_SIZE;
                        Debug.Assert(m_Size > FOH_COMMIT_SIZE);
                        Debug.Assert(m_Size % FOH_COMMIT_SIZE == 0);
                        alloc = ClrVirtualReserve(m_Size);
                    }

                    if (alloc == null)
                    {
                        throw new OutOfMemoryException();
                    }
                }

                // Commit a chunk in advance
                m_pStart = (byte*)ClrVirtualCommit(alloc, FOH_COMMIT_SIZE);
                if (m_pStart == null)
                {
                    ClrVirtualFree(alloc, m_Size);
                    throw new OutOfMemoryException();
                }

                m_pCurrent = m_pStart + sizeof(ObjHeader);

                m_SegmentHandle = RuntimeImports.RhRegisterFrozenSegment(m_pStart, (nuint)m_pCurrent - (nuint)m_pStart, FOH_COMMIT_SIZE, m_Size);
                if (m_SegmentHandle == IntPtr.Zero)
                {
                    ClrVirtualFree(alloc, m_Size);
                    throw new OutOfMemoryException();
                }

                m_SizeCommitted = FOH_COMMIT_SIZE;
            }

            public HalfBakedObject* TryAllocateObject(MethodTable* type, nuint objectSize)
            {
                Debug.Assert((m_pStart != null) && (m_Size > 0));
                //_ASSERT(IS_ALIGNED(m_pCurrent, DATA_ALIGNMENT));
                //_ASSERT(IS_ALIGNED(objectSize, DATA_ALIGNMENT));
                Debug.Assert(objectSize <= FOH_COMMIT_SIZE);
                Debug.Assert(m_pCurrent >= m_pStart + sizeof(ObjHeader));

                nuint spaceUsed = (nuint)(m_pCurrent - m_pStart);
                nuint spaceLeft = m_Size - spaceUsed;

                Debug.Assert(spaceUsed >= (nuint)sizeof(ObjHeader));
                Debug.Assert(spaceLeft >= (nuint)sizeof(ObjHeader));

                // Test if we have a room for the given object (including extra sizeof(ObjHeader) for next object)
                if (spaceLeft - (nuint)sizeof(ObjHeader) < objectSize)
                {
                    return null;
                }

                // Check if we need to commit a new chunk
                if (spaceUsed + objectSize + (nuint)sizeof(ObjHeader) > m_SizeCommitted)
                {
                    // Make sure we don't go out of bounds during this commit
                    Debug.Assert(m_SizeCommitted + FOH_COMMIT_SIZE <= m_Size);

                    if (ClrVirtualCommit(m_pStart + m_SizeCommitted, FOH_COMMIT_SIZE) == null)
                    {
                        throw new OutOfMemoryException();
                    }
                    m_SizeCommitted += FOH_COMMIT_SIZE;
                }

                HalfBakedObject* obj = (HalfBakedObject*)m_pCurrent;
                obj->SetMethodTable(type);

                m_pCurrent += objectSize;

                RuntimeImports.RhUpdateFrozenSegment(m_SegmentHandle, m_pCurrent, m_pStart + m_SizeCommitted);

                return obj;
            }
        }

        private struct HalfBakedObject
        {
            private MethodTable* _methodTable;
            public void SetMethodTable(MethodTable* methodTable) => _methodTable = methodTable;
        }
    }
}
