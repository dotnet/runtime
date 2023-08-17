// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4732FA5D-C105-4A23-87A7-58DCEDD4A9B3")]
    internal partial interface IStatelessCallerAllocatedBufferMarshalling
    {
        void Method(StatelessCallerAllocatedBufferType param);
        void MethodIn(in StatelessCallerAllocatedBufferType param);
        void MethodOut(out StatelessCallerAllocatedBufferType param);
        void MethodRef(ref StatelessCallerAllocatedBufferType param);
        StatelessCallerAllocatedBufferType Return();
        [PreserveSig]
        StatelessCallerAllocatedBufferType ReturnPreserveSig();
    }

    [GeneratedComClass]
    internal partial class StatelessCallerAllocatedBufferMarshalling : IStatelessCallerAllocatedBufferMarshalling
    {
        public void Method(StatelessCallerAllocatedBufferType param) { }
        public void MethodIn(in StatelessCallerAllocatedBufferType param) { }
        public void MethodOut(out StatelessCallerAllocatedBufferType param) { param = new StatelessCallerAllocatedBufferType { I = 20 }; }
        public void MethodRef(ref StatelessCallerAllocatedBufferType param) { param = new StatelessCallerAllocatedBufferType { I = 200 }; }
        public StatelessCallerAllocatedBufferType Return() => new StatelessCallerAllocatedBufferType() { I = 201 };
        public StatelessCallerAllocatedBufferType ReturnPreserveSig() => new StatelessCallerAllocatedBufferType() { I = 202 };
    }

    [NativeMarshalling(typeof(StatelessCallerAllocatedBufferTypeMarshaller))]
    internal class StatelessCallerAllocatedBufferType
    {
        public int I;
    }

    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.Default, typeof(StatelessCallerAllocatedBufferTypeMarshaller))]
    internal static unsafe class StatelessCallerAllocatedBufferTypeMarshaller
    {
        static HashSet<nint> _ptrs = new();
        public static int FreeCount { get; private set; }
        public static int BufferSize => sizeof(int);
        public static int* ConvertToUnmanaged(StatelessCallerAllocatedBufferType managed, Span<int> buffer)
        {
            buffer[0] = managed.I;
            return (int*)Unsafe.AsPointer(ref buffer[0]);
        }

        public static StatelessCallerAllocatedBufferType ConvertToManaged(int* unmanaged)
        {
            return new StatelessCallerAllocatedBufferType() { I = *unmanaged };
        }

        public static void Free(int* unmanaged)
        {
            FreeCount++;
            if (_ptrs.Contains((nint)unmanaged))
            {
                Marshal.FreeHGlobal((nint)unmanaged);
                _ptrs.Remove((nint)unmanaged);
            }
        }

        public static int* ConvertToUnmanaged(StatelessCallerAllocatedBufferType managed)
        {
            nint ptr = Marshal.AllocHGlobal(sizeof(int));
            _ptrs.Add(ptr);
            *(int*)ptr = managed.I;
            return (int*)ptr;
        }
    }
}
