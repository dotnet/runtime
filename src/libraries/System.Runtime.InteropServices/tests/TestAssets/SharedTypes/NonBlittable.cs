// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using static SharedTypes.IntWrapperWithNotificationMarshaller;

namespace SharedTypes
{
    [NativeMarshalling(typeof(StringContainerMarshaller))]
    public struct StringContainer
    {
        public string str1;
        public string str2;
    }

    [CustomMarshaller(typeof(StringContainer), MarshalMode.ManagedToUnmanagedIn, typeof(In))]
    [CustomMarshaller(typeof(StringContainer), MarshalMode.ManagedToUnmanagedRef, typeof(Ref))]
    [CustomMarshaller(typeof(StringContainer), MarshalMode.ManagedToUnmanagedOut, typeof(Out))]
    public static class StringContainerMarshaller
    {
        public struct StringContainerNative
        {
            public IntPtr str1;
            public IntPtr str2;
        }

        public static class In
        {
            public static StringContainerNative ConvertToUnmanaged(StringContainer managed)
                => Ref.ConvertToUnmanaged(managed);

            public static void Free(StringContainerNative unmanaged)
                => Ref.Free(unmanaged);
        }

        public static class Ref
        {
            public static StringContainerNative ConvertToUnmanaged(StringContainer managed)
            {
                return new StringContainerNative
                {
                    str1 = Marshal.StringToCoTaskMemUTF8(managed.str1),
                    str2 = Marshal.StringToCoTaskMemUTF8(managed.str2)
                };
            }

            public static StringContainer ConvertToManaged(StringContainerNative unmanaged)
            {
                return new StringContainer
                {
                    str1 = Marshal.PtrToStringUTF8(unmanaged.str1),
                    str2 = Marshal.PtrToStringUTF8(unmanaged.str2)
                };
            }

            public static void Free(StringContainerNative unmanaged)
            {
                Marshal.FreeCoTaskMem(unmanaged.str1);
                Marshal.FreeCoTaskMem(unmanaged.str2);
            }
        }

        public static class Out
        {
            public static StringContainer ConvertToManaged(StringContainerNative unmanaged)
                => Ref.ConvertToManaged(unmanaged);

            public static void Free(StringContainerNative unmanaged)
                => Ref.Free(unmanaged);
        }
    }

    [CustomMarshaller(typeof(double), MarshalMode.ManagedToUnmanagedIn, typeof(DoubleToBytesBigEndianMarshaller))]
    public static unsafe class DoubleToBytesBigEndianMarshaller
    {
        public const int BufferSize = 8;

        public static byte* ConvertToUnmanaged(double managed, Span<byte> buffer)
        {
            // Unsafe.AsPointer is safe since buffer must be pinned
            BinaryPrimitives.WriteDoubleBigEndian(buffer, managed);
            return (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
        }
    }

    [CustomMarshaller(typeof(double), MarshalMode.ManagedToUnmanagedIn, typeof(DoubleToLongMarshaller))]
    public static class DoubleToLongMarshaller
    {
        public static long ConvertToUnmanaged(double managed)
        {
            return MemoryMarshal.Cast<double, long>(MemoryMarshal.CreateSpan(ref managed, 1))[0];
        }
    }

    [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedOut, typeof(ExceptionOnUnmarshal))]
    public static class ExceptionOnUnmarshal
    {
        public static int ConvertToManaged(int unmanaged) => throw new Exception();
    }

    [NativeMarshalling(typeof(BoolStructMarshaller))]
    public struct BoolStruct
    {
        public bool b1;
        public bool b2;
        public bool b3;
    }

    [CustomMarshaller(typeof(BoolStruct), MarshalMode.Default, typeof(BoolStructMarshaller))]
    public static class BoolStructMarshaller
    {
        public struct BoolStructNative
        {
            public byte b1;
            public byte b2;
            public byte b3;
        }

        public static BoolStructNative ConvertToUnmanaged(BoolStruct managed)
        {
            return new BoolStructNative
            {
                b1 = (byte)(managed.b1 ? 1 : 0),
                b2 = (byte)(managed.b2 ? 1 : 0),
                b3 = (byte)(managed.b3 ? 1 : 0)
            };
        }

        public static BoolStruct ConvertToManaged(BoolStructNative unmanaged)
        {
            return new BoolStruct
            {
                b1 = unmanaged.b1 != 0,
                b2 = unmanaged.b2 != 0,
                b3 = unmanaged.b3 != 0
            };
        }
    }

    [NativeMarshalling(typeof(IntStructWrapperMarshaller))]
    public struct IntStructWrapper
    {
        public int Value;
    }

    [CustomMarshaller(typeof(IntStructWrapper), MarshalMode.Default, typeof(IntStructWrapperMarshaller))]
    public static class IntStructWrapperMarshaller
    {
        public static IntStructWrapperNative ConvertToUnmanaged(IntStructWrapper managed)
        {
            return new() { value = managed.Value };
        }
        public static IntStructWrapper ConvertToManaged(IntStructWrapperNative unmanaged)
        {
            return new() { Value = unmanaged.value };
        }
    }

    public struct IntStructWrapperNative
    {
        public int value;
    }

    [NativeMarshalling(typeof(IntWrapperMarshaller))]
    public class IntWrapper
    {
        public int i;

        public ref int GetPinnableReference() => ref i;
    }

    [CustomMarshaller(typeof(IntWrapper), MarshalMode.Default, typeof(IntWrapperMarshaller))]
    public static unsafe class IntWrapperMarshaller
    {
        public static int* ConvertToUnmanaged(IntWrapper managed)
        {
            int* ret = (int*)Marshal.AllocCoTaskMem(sizeof(int));
            *ret = managed.i;
            return ret;
        }

        public static IntWrapper ConvertToManaged(int* unmanaged)
        {
            return new IntWrapper { i = *unmanaged };
        }

        public static void Free(int* unmanaged)
        {
            Marshal.FreeCoTaskMem((IntPtr)unmanaged);
        }
    }

    [CustomMarshaller(typeof(IntWrapper), MarshalMode.Default, typeof(IntWrapperMarshallerWithFreeCounts))]
    public static unsafe class IntWrapperMarshallerWithFreeCounts
    {
        [ThreadStatic]
        public static int NumCallsToFree = 0;

        public static int* ConvertToUnmanaged(IntWrapper managed)
        {
            int* ret = (int*)Marshal.AllocCoTaskMem(sizeof(int));
            *ret = managed.i;
            return ret;
        }

        public static IntWrapper ConvertToManaged(int* unmanaged)
        {
            return new IntWrapper { i = *unmanaged };
        }

        public static void Free(int* unmanaged)
        {
            NumCallsToFree++;
            Marshal.FreeCoTaskMem((IntPtr)unmanaged);
        }
    }

    [CustomMarshaller(typeof(IntWrapper), MarshalMode.Default, typeof(Marshaller))]
    public static unsafe class IntWrapperMarshallerStateful
    {
        public struct Marshaller
        {
            private IntWrapper managed;
            private int* native;
            public void FromManaged(IntWrapper wrapper)
            {
                managed = wrapper;
            }

            public int* ToUnmanaged()
            {
                native = (int*)Marshal.AllocCoTaskMem(sizeof(int));
                *native = managed.i;
                return native;
            }

            public void FromUnmanaged(int* value)
            {
                native = value;
            }

            public IntWrapper ToManaged() => managed = new IntWrapper() { i = *native };

            public void Free()
            {
                Marshal.FreeCoTaskMem((IntPtr)native);
            }
        }
    }

    [NativeMarshalling(typeof(IntWrapperWithoutGetPinnableReferenceMarshaller))]
    public class IntWrapperWithoutGetPinnableReference
    {
        public int i;
    }

    [CustomMarshaller(typeof(IntWrapperWithoutGetPinnableReference), MarshalMode.Default, typeof(IntWrapperWithoutGetPinnableReferenceMarshaller))]
    public static unsafe class IntWrapperWithoutGetPinnableReferenceMarshaller
    {
        public static int* ConvertToUnmanaged(IntWrapperWithoutGetPinnableReference managed)
        {
            int* ret = (int*)Marshal.AllocCoTaskMem(sizeof(int));
            *ret = managed.i;
            return ret;
        }

        public static IntWrapperWithoutGetPinnableReference ConvertToManaged(int* unmanaged)
        {
            return new IntWrapperWithoutGetPinnableReference { i = *unmanaged };
        }

        public static ref int GetPinnableReference(IntWrapperWithoutGetPinnableReference wrapper) => ref wrapper.i;

        public static void Free(int* unmanaged)
        {
            Marshal.FreeCoTaskMem((IntPtr)unmanaged);
        }
    }

    [CustomMarshaller(typeof(IntWrapperWithoutGetPinnableReference), MarshalMode.ManagedToUnmanagedIn, typeof(StatelessGetPinnableReference))]
    public static unsafe class IntWrapperWithoutGetPinnableReferenceStatefulMarshaller
    {
        public struct StatelessGetPinnableReference
        {
            // We explicitly throw here as we're expecting to use the stateless GetPinnableReference method
            public void FromManaged(IntWrapperWithoutGetPinnableReference managed) => throw new NotImplementedException();

            public int* ToUnmanaged() => throw new NotImplementedException();

            public static ref int GetPinnableReference(IntWrapperWithoutGetPinnableReference wrapper) => ref wrapper.i;
        }
    }

    [CustomMarshaller(typeof(IntWrapperWithoutGetPinnableReference), MarshalMode.ManagedToUnmanagedIn, typeof(StatefulGetPinnableReference))]
    public static unsafe class IntWrapperWithoutGetPinnableReferenceStatefulNoAllocMarshaller
    {
        public struct StatefulGetPinnableReference
        {
            private IntWrapperWithoutGetPinnableReference _managed;
            public void FromManaged(IntWrapperWithoutGetPinnableReference managed) => _managed = managed;

            // Unsafe.AsPointer is safe since buffer must be pinned
            public int* ToUnmanaged() => (int*)Unsafe.AsPointer(ref _managed.i);

            public ref int GetPinnableReference() => ref _managed.i;
        }
    }

    [NativeMarshalling(typeof(IntWrapperWithNotificationMarshaller))]
    public struct IntWrapperWithNotification
    {
        [ThreadStatic]
        public static int NumInvokeSucceededOnUninitialized = 0;

        private bool initialized;
        public int Value;
        public event EventHandler InvokeSucceeded;

        public IntWrapperWithNotification()
        {
            initialized = true;
        }

        public void RaiseInvokeSucceeded()
        {
            if (!initialized)
            {
                NumInvokeSucceededOnUninitialized++;
            }
            InvokeSucceeded?.Invoke(this, EventArgs.Empty);
        }
    }

    [CustomMarshaller(typeof(IntWrapperWithNotification), MarshalMode.Default, typeof(Marshaller))]
    public static class IntWrapperWithNotificationMarshaller
    {
        public struct Marshaller
        {
            private IntWrapperWithNotification _managed;

            public void FromManaged(IntWrapperWithNotification managed) =>_managed = managed;

            public int ToUnmanaged() => _managed.Value;

            public void FromUnmanaged(int i) => _managed.Value = i;

            public IntWrapperWithNotification ToManaged() => _managed;

            public void OnInvoked() => _managed.RaiseInvokeSucceeded();
        }
    }

    [CustomMarshaller(typeof(BoolStruct), MarshalMode.Default, typeof(Marshaller))]
    public static class BoolStructMarshallerStateful
    {
        public struct BoolStructNative
        {
            public byte b1;
            public byte b2;
            public byte b3;
        }

        public struct Marshaller
        {
            private BoolStructNative _boolStructNative;
            public void FromManaged(BoolStruct managed)
            {
                _boolStructNative = new BoolStructNative
                {
                    b1 = (byte)(managed.b1 ? 1 : 0),
                    b2 = (byte)(managed.b2 ? 1 : 0),
                    b3 = (byte)(managed.b3 ? 1 : 0)
                };
            }

            public BoolStructNative ToUnmanaged() => _boolStructNative;

            public void FromUnmanaged(BoolStructNative value) => _boolStructNative = value;

            public BoolStruct ToManaged()
            {
                return new BoolStruct
                {
                    b1 = _boolStructNative.b1 != 0,
                    b2 = _boolStructNative.b2 != 0,
                    b3 = _boolStructNative.b3 != 0
                };
            }
        }
    }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(List<>), MarshalMode.Default, typeof(ListMarshaller<,>))]
    public unsafe static class ListMarshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        public static byte* AllocateContainerForUnmanagedElements(List<T> managed, out int numElements)
        {
            if (managed is null)
            {
                numElements = 0;
                return null;
            }

            numElements = managed.Count;

            // Always allocate at least one byte when the list is zero-length.
            int spaceToAllocate = Math.Max(checked(sizeof(TUnmanagedElement) * numElements), 1);
            return (byte*)Marshal.AllocCoTaskMem(spaceToAllocate);
        }

        public static ReadOnlySpan<T> GetManagedValuesSource(List<T> managed)
            => CollectionsMarshal.AsSpan(managed);

        public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(byte* unmanaged, int numElements)
            => new Span<TUnmanagedElement>(unmanaged, numElements);

        public static List<T> AllocateContainerForManagedElements(byte* unmanaged, int length)
        {
            if (unmanaged is null)
                return null;

            var list = new List<T>(length);
            for (int i = 0; i < length; i++)
            {
                list.Add(default);
            }

            return list;
        }

        public static Span<T> GetManagedValuesDestination(List<T> managed)
            => CollectionsMarshal.AsSpan(managed);

        public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(byte* nativeValue, int numElements)
            => new ReadOnlySpan<TUnmanagedElement>(nativeValue, numElements);

        public static void Free(byte* unmanaged)
            => Marshal.FreeCoTaskMem((IntPtr)unmanaged);
    }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(List<>), MarshalMode.ManagedToUnmanagedIn, typeof(ListMarshallerWithBuffer<,>))]
    public unsafe static class ListMarshallerWithBuffer<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        public static int BufferSize { get; } = 0x200;

        public static byte* AllocateContainerForUnmanagedElements(List<T> managed, Span<byte> buffer, out int numElements)
        {
            if (managed is null)
            {
                numElements = 0;
                return null;
            }

            numElements = managed.Count;

            int spaceRequired = checked(sizeof(TUnmanagedElement) * numElements);
            if (spaceRequired > buffer.Length)
                throw new InvalidOperationException();

            // Unsafe.AsPointer is safe since buffer must be pinned
            return (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
        }

        public static ReadOnlySpan<T> GetManagedValuesSource(List<T> managed)
            => CollectionsMarshal.AsSpan(managed);

        public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(byte* unmanaged, int numElements)
            => new Span<TUnmanagedElement>(unmanaged, numElements);
    }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(List<>), MarshalMode.Default, typeof(ListMarshallerStateful<,>.Marshaller))]
    public unsafe static class ListMarshallerStateful<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        public ref struct Marshaller
        {
            private List<T> _list;
            private IntPtr _allocatedMemory;
            private Span<TUnmanagedElement> _span;

            public static int BufferSize { get; } = 0x200;

            public void FromManaged(List<T> managed)
            {
                FromManaged(managed, Span<TUnmanagedElement>.Empty);
            }

            public void FromManaged(List<T> managed, Span<TUnmanagedElement> buffer)
            {
                _allocatedMemory = default;
                if (managed is null)
                {
                    _list = null;
                    _span = default;
                    return;
                }

                _list = managed;
                // Always allocate at least one byte when the list is zero-length.
                int countToAllocate = Math.Max(managed.Count, 1);
                if (countToAllocate <= buffer.Length)
                {
                    _span = buffer[0..countToAllocate];
                }
                else
                {
                    _allocatedMemory = Marshal.AllocCoTaskMem(countToAllocate * sizeof(TUnmanagedElement));
                    _span = new Span<TUnmanagedElement>((void*)_allocatedMemory, managed.Count);
                }
            }

            public ReadOnlySpan<T> GetManagedValuesSource() => CollectionsMarshal.AsSpan(_list);

            public Span<TUnmanagedElement> GetUnmanagedValuesDestination() => _span;

            public ref TUnmanagedElement GetPinnableReference() => ref MemoryMarshal.GetReference(_span);

            // Unsafe.AsPointer is safe since buffer must be pinned
            public byte* ToUnmanaged() => (byte*)Unsafe.AsPointer(ref GetPinnableReference());

            public Span<T> GetManagedValuesDestination(int length)
            {
                if (_allocatedMemory == IntPtr.Zero)
                {
                    _list = null;
                    return default;
                }

                _list = new List<T>(length);
                for (int i = 0; i < length; i++)
                {
                    _list.Add(default);
                }
                return CollectionsMarshal.AsSpan(_list);
            }

            public ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(int length)
            {
                if (_allocatedMemory == IntPtr.Zero)
                    return default;

                return _span = new Span<TUnmanagedElement>((void*)_allocatedMemory, length);
            }

            public void FromUnmanaged(byte* value)
            {
                _allocatedMemory = (IntPtr)value;
            }

            public List<T> ToManaged() => _list;

            public void Free() => Marshal.FreeCoTaskMem(_allocatedMemory);
        }
    }

    [CustomMarshaller(typeof(List<>), MarshalMode.Default, typeof(ListMarshallerWithPinning<,>))]
    [ContiguousCollectionMarshaller]
    public unsafe static class ListMarshallerWithPinning<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        public static byte* AllocateContainerForUnmanagedElements(List<T> managed, out int numElements)
        {
            if (managed is null)
            {
                numElements = 0;
                return null;
            }

            numElements = managed.Count;

            // Always allocate at least one byte when the list is zero-length.
            int spaceToAllocate = Math.Max(checked(sizeof(TUnmanagedElement) * numElements), 1);
            return (byte*)Marshal.AllocCoTaskMem(spaceToAllocate);
        }

        public static ReadOnlySpan<T> GetManagedValuesSource(List<T> managed)
            => CollectionsMarshal.AsSpan(managed);

        public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(byte* unmanaged, int numElements)
            => new Span<TUnmanagedElement>(unmanaged, numElements);

        public static List<T> AllocateContainerForManagedElements(byte* unmanaged, int length)
        {
            if (unmanaged is null)
                return null;

            var list = new List<T>(length);
            for (int i = 0; i < length; i++)
            {
                list.Add(default);
            }

            return list;
        }

        public static Span<T> GetManagedValuesDestination(List<T> managed)
            => CollectionsMarshal.AsSpan(managed);

        public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(byte* nativeValue, int numElements)
            => new ReadOnlySpan<TUnmanagedElement>(nativeValue, numElements);

        public static void Free(byte* unmanaged)
            => Marshal.FreeCoTaskMem((IntPtr)unmanaged);

        public static ref T GetPinnableReference(List<T> managed)
        {
            if (managed is null)
            {
                return ref Unsafe.NullRef<T>();
            }
            return ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(managed));
        }
    }
}
