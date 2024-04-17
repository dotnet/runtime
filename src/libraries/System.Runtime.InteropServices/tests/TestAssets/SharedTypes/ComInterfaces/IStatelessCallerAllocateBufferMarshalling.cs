// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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

    internal struct StatelessCallerAllocatedBufferNative
    {
        public int I;
    }

    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.ManagedToUnmanagedRef, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.UnmanagedToManagedRef, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.ElementIn, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.ElementOut, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.ElementRef, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.ManagedToUnmanagedOut, typeof(UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.UnmanagedToManagedIn, typeof(UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.UnmanagedToManagedOut, typeof(UnmanagedToManagedOut))]
    internal static unsafe class StatelessCallerAllocatedBufferTypeMarshaller
    {
        static bool _canAllocate = true;
        public static void DisableAllocations() => _canAllocate = false;
        public static void EnableAllocations() => _canAllocate = true;
        public static void AssertAllPointersFreed()
        {
            if (_ptrs.Any()) throw new InvalidOperationException();
        }

        static HashSet<nint> _ptrs = new();

        public static int FreeCount { get; private set; }

        public static class UnmanagedToManaged
        {
            public static StatelessCallerAllocatedBufferType ConvertToManaged(StatelessCallerAllocatedBufferNative* unmanaged)
            {
                return new StatelessCallerAllocatedBufferType() { I = unmanaged->I };
            }

            public static void Free(StatelessCallerAllocatedBufferNative* unmanaged)
            {
                FreeCount++;
                if (_ptrs.Contains((nint)unmanaged))
                {
                    Marshal.FreeHGlobal((nint)unmanaged);
                    _ptrs.Remove((nint)unmanaged);
                }
            }
        }

        public static class ManagedToUnmanagedIn
        {
            public static int BufferSize => sizeof(StatelessCallerAllocatedBufferNative);

            public static StatelessCallerAllocatedBufferNative* ConvertToUnmanaged(StatelessCallerAllocatedBufferType managed, Span<byte> buffer)
            {
                var unmanaged = new StatelessCallerAllocatedBufferNative() { I = managed.I };
                MemoryMarshal.Write(buffer, in unmanaged);
                // Unsafe.AsPointer is safe since buffer is pinned
                return (StatelessCallerAllocatedBufferNative*)Unsafe.AsPointer(ref MemoryMarshal.AsRef<StatelessCallerAllocatedBufferNative>(buffer));
            }

            public static void Free(StatelessCallerAllocatedBufferNative* unmanaged)
            {
                FreeCount++;
                if (_ptrs.Contains((nint)unmanaged))
                {
                    Marshal.FreeHGlobal((nint)unmanaged);
                    _ptrs.Remove((nint)unmanaged);
                }
            }
        }

        public static class UnmanagedToManagedOut
        {
            public static StatelessCallerAllocatedBufferNative* ConvertToUnmanaged(StatelessCallerAllocatedBufferType managed)
            {
                if (!_canAllocate)
                    throw new InvalidOperationException("Marshalling used default ConverToUnmanaged when CallerAllocatedBuffer was expected");
                nint ptr = Marshal.AllocHGlobal(sizeof(StatelessCallerAllocatedBufferNative));
                _ptrs.Add(ptr);
                var structPtr = (StatelessCallerAllocatedBufferNative*)ptr;
                structPtr->I = managed.I;
                return structPtr;
            }

            public static void Free(StatelessCallerAllocatedBufferNative* unmanaged)
            {
                FreeCount++;
                if (_ptrs.Contains((nint)unmanaged))
                {
                    Marshal.FreeHGlobal((nint)unmanaged);
                    _ptrs.Remove((nint)unmanaged);
                }
            }
        }

        public static class Bidirectional
        {
            public static StatelessCallerAllocatedBufferNative* ConvertToUnmanaged(StatelessCallerAllocatedBufferType managed)
            {
                if (!_canAllocate)
                    throw new InvalidOperationException("Marshalling used default ConverToUnmanaged when CallerAllocatedBuffer was expected");
                nint ptr = Marshal.AllocHGlobal(sizeof(StatelessCallerAllocatedBufferNative));
                _ptrs.Add(ptr);
                var structPtr = (StatelessCallerAllocatedBufferNative*)ptr;
                structPtr->I = managed.I;
                return structPtr;
            }

            public static StatelessCallerAllocatedBufferType ConvertToManaged(StatelessCallerAllocatedBufferNative* unmanaged)
            {
                return new StatelessCallerAllocatedBufferType() { I = unmanaged->I };
            }

            public static void Free(StatelessCallerAllocatedBufferNative* unmanaged)
            {
                FreeCount++;
                if (_ptrs.Contains((nint)unmanaged))
                {
                    Marshal.FreeHGlobal((nint)unmanaged);
                    _ptrs.Remove((nint)unmanaged);
                }
            }
        }
    }
}
