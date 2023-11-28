// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HelloFrozenSegment
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    struct FrozenSegment
    {
        IntPtr underlyingSegment;
        IntPtr underlyingBuffer;

        public FrozenSegment(IntPtr underlyingSegment, IntPtr underlyingBuffer)
        {
            this.underlyingSegment = underlyingSegment;
            this.underlyingBuffer = underlyingBuffer;
        }

        public void Release()
        {
            // Workaround for GitHub 85863
            // We are not aware of anyone calling this API so it's low priority to fix
            // GCHelpers.UnregisterFrozenSegment(this.underlyingSegment);
            // Marshal.FreeHGlobal(this.underlyingBuffer);
        }
    }

    internal static class GCHelpers
    {
        private static MethodInfo s_registerFrozenSegmentMethod;
        private static MethodInfo s_unregisterFrozenSegmentMethod;

        private static MethodInfo RegisterFrozenSegmentMethod
        {
            get
            {
                if (s_registerFrozenSegmentMethod == null)
                {
                    s_registerFrozenSegmentMethod = typeof(GC).GetMethod("_RegisterFrozenSegment", BindingFlags.NonPublic|BindingFlags.Static);
                }

                return s_registerFrozenSegmentMethod;
            }
        }

        private static MethodInfo UnregisterFrozenSegmentMethod
        {
            get
            {
                if (s_unregisterFrozenSegmentMethod == null)
                {
                    s_unregisterFrozenSegmentMethod = typeof(GC).GetMethod("_UnregisterFrozenSegment", BindingFlags.NonPublic|BindingFlags.Static);
                }

                return s_unregisterFrozenSegmentMethod;
            }
        }

        public static IntPtr RegisterFrozenSegment(IntPtr buffer, nint size)
        {
            return (IntPtr)RegisterFrozenSegmentMethod.Invoke(null, new object[]{buffer, size});

        }

        public static void UnregisterFrozenSegment(IntPtr segment)
        {
            UnregisterFrozenSegmentMethod.Invoke(null, new object[]{segment});
        }
    }

    internal unsafe class FrozenSegmentBuilder
    {
        private IntPtr _buffer;
        private IntPtr _allocated;
        private IntPtr _limit;

        // This only work for BaseSize (i.e. not arrays)
        private static unsafe short GetObjectSize(IntPtr methodTable)
        {
            IntPtr pointerToSize = methodTable + 4;
            return *((short*)pointerToSize);
        }

        public FrozenSegmentBuilder(int capacity)
        {
            _buffer = Marshal.AllocHGlobal(capacity);
            for (int i = 0; i < capacity; i++)
            {
                *((byte*)(_buffer + i)) = 0;
            }
            _allocated = _buffer + IntPtr.Size;
            _limit = _buffer + capacity;
        }

        public IntPtr Allocate(IntPtr methodTable)
        {
            if (_allocated == IntPtr.Zero)
            {
                throw new Exception("Segment already built");
            }
            int objectSize = GetObjectSize(methodTable);
            if ((_allocated + objectSize).CompareTo(_limit) > 0)
            {
                throw new Exception("OutOfCapacity");
            }

            IntPtr* pMethodTable = (IntPtr*)_allocated;
            *pMethodTable = methodTable;
            IntPtr result = _allocated;
            _allocated = _allocated + objectSize;
            return result;
        }

        public FrozenSegment GetSegment()
        {
            if (_allocated == IntPtr.Zero)
            {
                throw new Exception("Segment already built");
            }

            nint size = (nint)(_allocated.ToInt64() - _buffer.ToInt64());
            _allocated = IntPtr.Zero;
            IntPtr segment = GCHelpers.RegisterFrozenSegment(_buffer, size);
            return new FrozenSegment(segment, _buffer);
        }
    }

    internal class Node
    {
        public Node next;
        public int number;
    }

    internal static class Program
    {
        private static unsafe int Main()
        {
            // Regression testing for dotnet/runtime #83027
            Node[] firstArray = new Node[30000000]; 
            for (int index = 0; index < firstArray.Length; index++)
            {
                firstArray[index] = new Node();
            }

            IntPtr methodTable = typeof(Node).TypeHandle.Value;

            FrozenSegmentBuilder frozenSegmentBuilder = new FrozenSegmentBuilder(1000);
            IntPtr node1Ptr = frozenSegmentBuilder.Allocate(methodTable);
            IntPtr node2Ptr = frozenSegmentBuilder.Allocate(methodTable);

            FrozenSegment frozenSegment = frozenSegmentBuilder.GetSegment();
            Node root = new Node();
            Node node1 = Unsafe.AsRef<Node>((void*)&node1Ptr);
            Node node2 = Unsafe.AsRef<Node>((void*)&node2Ptr);
            // It is okay for any object to reference a frozen object.
            root.next = node1;
            
            // It is not okay for a frozen object to reference another frozen object
            // This is because the WriteBarrier code may (depending on the pointer
            // value returned by AllocHGlobal) determine node2 to be an ephemeral object 
            // when it isn't.
            // node1.next = node2;

            // It is not okay for a frozen object to reference another object that is not frozen
            // This is because we may miss the marking of the new Node or miss the relocation
            // of the new Node.
            // node2.next = new Node();

            // Making changes to non-GC references is fine
            node1.number = 10086;
            node2.number = 12580;

            GC.Collect();
            node1 = null;
            GC.Collect();
            node2 = null;
            GC.Collect();
            Console.WriteLine(root.next.next != null);
            frozenSegment.Release();
            return 100;
        }
    }
}
