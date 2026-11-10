// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime
{
    internal unsafe class FrozenObjectHeapManager
    {
        public static readonly FrozenObjectHeapManager Instance = new FrozenObjectHeapManager();

        private readonly Lock m_Crst = new Lock(useTrivialWaits: true);
        private FrozenObjectSegment m_CurrentSegment;

        private const nuint FOH_SEGMENT_DEFAULT_SIZE = 4 * 1024;

        public T? TryAllocateObject<T>() where T : class
        {
            MethodTable* pMT = MethodTable.Of<T>();
            return Unsafe.As<T>(TryAllocateObject(pMT, pMT->BaseSize));
        }

        private object? TryAllocateObject(MethodTable* type, nuint objectSize)
        {
            HalfBakedObject* obj = null;

            using (m_Crst.EnterScope())
            {
                Debug.Assert(type != null);
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

                if (objectSize > FOH_SEGMENT_DEFAULT_SIZE - 2 * (nuint)sizeof(ObjHeader))
                {
                    // Leave room for the first and next object headers in a default-sized segment.
                    return null;
                }

                obj = m_CurrentSegment == null ? null : m_CurrentSegment.TryAllocateObject(type, objectSize);
                // obj is nullptr if the current segment is full or hasn't been allocated yet
                if (obj == null)
                {
                    nuint newSegmentSize = FOH_SEGMENT_DEFAULT_SIZE;
                    if (m_CurrentSegment != null)
                    {
                        // Double the size to reduce the number of frozen segments in apps with lots of frozen objects
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
            // Start of the allocated memory, the first object starts at "m_pStart + sizeof(ObjHeader)" (its pMT)
            private byte* m_pStart;

            // Pointer to the end of the current segment, ready to be used as a pMT for a new object
            // meaning that "m_pCurrent - sizeof(ObjHeader)" is the actual start of the new object (header).
            public byte* m_pCurrent;

            // Total memory allocated for the current segment
            public nuint m_Size;

            private IntPtr m_SegmentHandle;

            public FrozenObjectSegment(nuint sizeHint)
            {
                m_Size = sizeHint;

                Debug.Assert(m_Size >= FOH_SEGMENT_DEFAULT_SIZE);

                m_pStart = (byte*)NativeMemory.AllocZeroed(m_Size);

                m_pCurrent = m_pStart + sizeof(ObjHeader);

                m_SegmentHandle = RuntimeImports.RhRegisterFrozenSegment(m_pStart, (nuint)m_pCurrent - (nuint)m_pStart, m_Size, m_Size);
                if (m_SegmentHandle == IntPtr.Zero)
                {
                    NativeMemory.Free(m_pStart);
                    throw new OutOfMemoryException();
                }
            }

            public HalfBakedObject* TryAllocateObject(MethodTable* type, nuint objectSize)
            {
                Debug.Assert((m_pStart != null) && (m_Size > 0));
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

                HalfBakedObject* obj = (HalfBakedObject*)m_pCurrent;
                obj->SetMethodTable(type);

                m_pCurrent += objectSize;

                RuntimeImports.RhUpdateFrozenSegment(m_SegmentHandle, m_pCurrent, m_pStart + m_Size);

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
