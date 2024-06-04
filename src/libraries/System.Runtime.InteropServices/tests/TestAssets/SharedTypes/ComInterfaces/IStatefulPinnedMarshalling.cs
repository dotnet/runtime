// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4731FA5D-C103-4A22-87A1-58DCEDD4A9B3")]
    internal partial interface IStatefulPinnedMarshalling
    {
        void Method(StatefulPinnedType param);
        void MethodIn(in StatefulPinnedType param);
        void MethodOut(out StatefulPinnedType param);
        void MethodRef(ref StatefulPinnedType param);
        StatefulPinnedType Return();
    }

    [GeneratedComClass]
    internal partial class StatefulPinnedMarshalling : IStatefulPinnedMarshalling
    {
        public void Method(StatefulPinnedType param) { param.I = 100; }
        public void MethodIn(in StatefulPinnedType param) { param.I = 101; }
        public void MethodOut(out StatefulPinnedType param) => param = new StatefulPinnedType() { I = 102 };
        public void MethodRef(ref StatefulPinnedType param) { param = new StatefulPinnedType() { I = 103 }; }
        public StatefulPinnedType Return() => new StatefulPinnedType() { I = 104 };
    }

    [NativeMarshalling(typeof(StatefulPinnedTypeMarshaller))]
    internal class StatefulPinnedType
    {
        public int I;
    }

    internal unsafe struct StatefulPinnedNative
    {
        public int I;
    }

    [CustomMarshaller(typeof(StatefulPinnedType), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
    [CustomMarshaller(typeof(StatefulPinnedType), MarshalMode.UnmanagedToManagedOut, typeof(UnmanagedToManagedOut))]
    [CustomMarshaller(typeof(StatefulPinnedType), MarshalMode.UnmanagedToManagedIn, typeof(UnmanagedToManagedIn))]
    [CustomMarshaller(typeof(StatefulPinnedType), MarshalMode.ManagedToUnmanagedOut, typeof(ManagedToUnmanagedOut))]
    [CustomMarshaller(typeof(StatefulPinnedType), MarshalMode.ManagedToUnmanagedRef, typeof(ManagedToUnmanagedRef))]
    [CustomMarshaller(typeof(StatefulPinnedType), MarshalMode.UnmanagedToManagedRef, typeof(UnmanagedToManagedRef))]
    internal unsafe static class StatefulPinnedTypeMarshaller
    {
        public ref struct ManagedToUnmanagedIn
        {
            static bool s_mustPin;
            public static void DisableNonPinnedPath() => s_mustPin = true;
            public static void EnableNonPinnedPath() => s_mustPin = false;

            StatefulPinnedType _managed;
            bool _hasManaged;
            Span<byte> buffer;
            nint _ptr;
            bool _canFree;
            bool _isPinned;
            ref StatefulPinnedNative _refNativeStruct;

            public void FromManaged(StatefulPinnedType managed)
            {
                _hasManaged = true;
                _managed = managed;
            }

            public ref StatefulPinnedNative GetPinnableReference()
            {
                if (!_hasManaged)
                    throw new InvalidOperationException();
                buffer = new byte[sizeof(StatefulPinnedNative)];
                _isPinned = true;
                _refNativeStruct = ref MemoryMarshal.AsRef<StatefulPinnedNative>(buffer);
                return ref _refNativeStruct;
            }

            public StatefulPinnedNative* ToUnmanaged()
            {
                if (!_hasManaged)
                    throw new InvalidOperationException();

                _canFree = true;
                if (_isPinned)
                {
                    // Unsafe.AsPointer is safe, because the result from GetPinnableReference is pinned
                    _refNativeStruct = new StatefulPinnedNative() { I = _managed.I };
                    return (StatefulPinnedNative*)Unsafe.AsPointer(ref _refNativeStruct);
                }

                if (s_mustPin)
                    throw new InvalidOperationException("Expected to pin, but is instead converting with default ToUnmanaged.");

                _ptr = Marshal.AllocHGlobal(sizeof(StatefulPinnedNative));
                *(StatefulPinnedNative*)_ptr = new StatefulPinnedNative() { I = _managed.I };
                return (StatefulPinnedNative*)_ptr;
            }

            public void Free()
            {
                if (!_canFree)
                    throw new InvalidOperationException();

                if (!_isPinned && _ptr != 0)
                {
                    Marshal.FreeHGlobal(_ptr);
                }
            }
        }

        public struct ManagedToUnmanagedOut
        {
            StatefulPinnedNative* _unmanaged;
            bool _hasUnmanaged;

            public void FromUnmanaged(StatefulPinnedNative* unmanaged)
            {
                _unmanaged = unmanaged;
                _hasUnmanaged = true;
            }

            public StatefulPinnedType ToManaged()
            {
                if (!_hasUnmanaged)
                    throw new InvalidOperationException();
                return new StatefulPinnedType() { I = _unmanaged->I };
            }

            public void Free()
            {
                if (!_hasUnmanaged)
                    throw new InvalidOperationException();
                var ptr = (nint)_unmanaged;
                if (ptr != 0)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        public struct UnmanagedToManagedIn
        {
            StatefulPinnedNative* _unmanaged;
            bool _hasUnmanaged;
            public void FromUnmanaged(StatefulPinnedNative* unmanaged)
            {
                _unmanaged = unmanaged;
                _hasUnmanaged = true;
            }

            public StatefulPinnedType ToManaged()
            {
                if (!_hasUnmanaged)
                    throw new InvalidOperationException();
                return new StatefulPinnedType() { I = _unmanaged->I };
            }

            public void Free()
            {
            }
        }

        public struct UnmanagedToManagedOut
        {
            StatefulPinnedType _managed;
            bool _hasManaged;
            nint _ptr;

            public void FromManaged(StatefulPinnedType managed)
            {
                _hasManaged = true;
                _managed = managed;
            }

            public StatefulPinnedNative* ToUnmanaged()
            {
                if (!_hasManaged)
                    throw new InvalidOperationException();
                _ptr = Marshal.AllocHGlobal(sizeof(StatefulPinnedNative));
                *(StatefulPinnedNative*)_ptr = new StatefulPinnedNative() { I = _managed.I };
                return (StatefulPinnedNative*)_ptr;
            }

            public void Free()
            {
            }
        }


        public struct ManagedToUnmanagedRef
        {
            StatefulPinnedNative* _unmanaged;
            bool _hasUnmanaged;
            public void FromUnmanaged(StatefulPinnedNative* unmanaged)
            {
                _unmanaged = unmanaged;
                _hasUnmanaged = true;
            }

            public StatefulPinnedType ToManaged()
            {
                if (!_hasUnmanaged)
                    throw new InvalidOperationException();
                return new StatefulPinnedType() { I = _unmanaged->I };
            }

            StatefulPinnedType _managed;
            bool _hasManaged;
            nint _ptr;
            bool _hasAllocated;

            public void FromManaged(StatefulPinnedType managed)
            {
                _hasManaged = true;
                _managed = managed;
            }

            public StatefulPinnedNative* ToUnmanaged()
            {
                if (!_hasManaged)
                    throw new InvalidOperationException();
                _ptr = Marshal.AllocHGlobal(sizeof(StatefulPinnedNative));
                _hasAllocated = true;
                *(StatefulPinnedNative*)_ptr = new StatefulPinnedNative() { I = _managed.I };
                return (StatefulPinnedNative*)_ptr;
            }

            public void Free()
            {
                if (_hasUnmanaged)
                {
                    Marshal.FreeHGlobal((nint)_unmanaged);
                }
                else if (_hasAllocated)
                {
                    Marshal.FreeHGlobal(_ptr);
                }
            }
        }
        public struct UnmanagedToManagedRef
        {
            StatefulPinnedNative* _unmanaged;
            bool _hasUnmanaged;
            public void FromUnmanaged(StatefulPinnedNative* unmanaged)
            {
                _unmanaged = unmanaged;
                _hasUnmanaged = true;
            }

            public StatefulPinnedType ToManaged()
            {
                if (!_hasUnmanaged)
                    throw new InvalidOperationException();
                return new StatefulPinnedType() { I = _unmanaged->I };
            }

            StatefulPinnedType _managed;
            bool _hasManaged;
            nint _ptr;
            bool _hasAllocated;

            public void FromManaged(StatefulPinnedType managed)
            {
                _hasManaged = true;
                _managed = managed;
            }

            public StatefulPinnedNative* ToUnmanaged()
            {
                if (!_hasManaged)
                    throw new InvalidOperationException();
                _ptr = Marshal.AllocHGlobal(sizeof(StatefulPinnedNative));
                _hasAllocated = true;
                *(StatefulPinnedNative*)_ptr = new StatefulPinnedNative() { I = _managed.I };
                return (StatefulPinnedNative*)_ptr;
            }

            public void Free()
            {
                if (_hasAllocated && _hasUnmanaged)
                {
                    Marshal.FreeHGlobal((nint)_unmanaged);
                }
            }
        }
    }
}
