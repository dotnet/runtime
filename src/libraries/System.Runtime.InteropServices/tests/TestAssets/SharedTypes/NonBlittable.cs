// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharedTypes
{
    [NativeMarshalling(typeof(StringContainerNative))]
    public struct StringContainer
    {
        public string str1;
        public string str2;
    }

    [CustomTypeMarshaller(typeof(StringContainer), Features = CustomTypeMarshallerFeatures.UnmanagedResources)]
    public struct StringContainerNative
    {
        public IntPtr str1;
        public IntPtr str2;

        public StringContainerNative(StringContainer managed)
        {
            str1 = Marshal.StringToCoTaskMemUTF8(managed.str1);
            str2 = Marshal.StringToCoTaskMemUTF8(managed.str2);
        }

        public StringContainer ToManaged()
        {
            return new StringContainer
            {
                str1 = Marshal.PtrToStringUTF8(str1),
                str2 = Marshal.PtrToStringUTF8(str2)
            };
        }

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem(str1);
            Marshal.FreeCoTaskMem(str2);
        }
    }

    [CustomTypeMarshaller(typeof(double), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
    public struct DoubleToLongMarshaler
    {
        public long l;

        public DoubleToLongMarshaler(double d)
        {
            l = MemoryMarshal.Cast<double, long>(MemoryMarshal.CreateSpan(ref d, 1))[0];
        }

        public double ToManaged() => MemoryMarshal.Cast<long, double>(MemoryMarshal.CreateSpan(ref l, 1))[0];

        public long ToNativeValue() => l;

        public void FromNativeValue(long value) => l = value;
    }

    [NativeMarshalling(typeof(BoolStructNative))]
    public struct BoolStruct
    {
        public bool b1;
        public bool b2;
        public bool b3;
    }

    [CustomTypeMarshaller(typeof(BoolStruct))]
    public struct BoolStructNative
    {
        public byte b1;
        public byte b2;
        public byte b3;
        public BoolStructNative(BoolStruct bs)
        {
            b1 = (byte)(bs.b1 ? 1 : 0);
            b2 = (byte)(bs.b2 ? 1 : 0);
            b3 = (byte)(bs.b3 ? 1 : 0);
        }

        public BoolStruct ToManaged()
        {
            return new BoolStruct
            {
                b1 = b1 != 0,
                b2 = b2 != 0,
                b3 = b3 != 0
            };
        }
    }

    [NativeMarshalling(typeof(IntWrapperMarshaler))]
    public class IntWrapper
    {
        public int i;

        public ref int GetPinnableReference() => ref i;
    }

    [CustomTypeMarshaller(typeof(IntWrapper), Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling)]
    public unsafe struct IntWrapperMarshaler
    {
        public IntWrapperMarshaler(IntWrapper managed)
        {
            Value = (int*)Marshal.AllocCoTaskMem(sizeof(int));
            *Value = managed.i;
        }

        private int* Value { get; set; }

        public int* ToNativeValue() => Value;
        public void FromNativeValue(int* value) => Value = value;

        public IntWrapper ToManaged() => new IntWrapper { i = *Value };

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem((IntPtr)Value);
        }
    }

    [CustomTypeMarshaller(typeof(string), Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling | CustomTypeMarshallerFeatures.CallerAllocatedBuffer, BufferSize = 0x100)]
    public unsafe ref struct Utf16StringMarshaller
    {
        private ushort* allocated;
        private Span<ushort> span;
        private bool isNullString;

        public Utf16StringMarshaller(string str)
            : this(str, default(Span<ushort>))
        {
        }

        public Utf16StringMarshaller(string str, Span<ushort> buffer)
        {
            isNullString = false;
            if (str is null)
            {
                allocated = null;
                span = default;
                isNullString = true;
            }
            else if ((str.Length + 1) < buffer.Length)
            {
                span = buffer;
                str.AsSpan().CopyTo(MemoryMarshal.Cast<ushort, char>(buffer));
                // Supplied memory is in an undefined state so ensure
                // there is a trailing null in the buffer.
                span[str.Length] = '\0';
                allocated = null;
            }
            else
            {
                allocated = (ushort*)Marshal.StringToCoTaskMemUni(str);
                span = new Span<ushort>(allocated, str.Length + 1);
            }
        }

        public ref ushort GetPinnableReference()
        {
            return ref span.GetPinnableReference();
        }

        public ushort* ToNativeValue() => (ushort*)Unsafe.AsPointer(ref GetPinnableReference());

        public void FromNativeValue(ushort* value)
        {
            allocated = value;
            span = new Span<ushort>(value, value == null ? 0 : FindStringLength(value));
            isNullString = value == null;

            static int FindStringLength(ushort* ptr)
            {
                // Implemented similarly to string.wcslen as we can't access that outside of CoreLib
                var searchSpace = new Span<ushort>(ptr, int.MaxValue);
                return searchSpace.IndexOf((ushort)0);
            }
        }

        public string ToManaged()
        {
            return isNullString ? null : MemoryMarshal.Cast<ushort, char>(span).ToString();
        }

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem((IntPtr)allocated);
        }
    }

    [CustomTypeMarshaller(typeof(string), Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling | CustomTypeMarshallerFeatures.CallerAllocatedBuffer, BufferSize = 0x100)]
    public unsafe ref struct Utf8StringMarshaller
    {
        private byte* allocated;
        private Span<byte> span;

        public Utf8StringMarshaller(string str)
            : this(str, default(Span<byte>))
        { }

        public Utf8StringMarshaller(string str, Span<byte> buffer)
        {
            allocated = null;
            span = default;
            if (str is null)
                return;

            if (buffer.Length >= Encoding.UTF8.GetMaxByteCount(str.Length) + 1) // +1 for null terminator
            {
                int byteCount = Encoding.UTF8.GetBytes(str, buffer);
                buffer[byteCount] = 0; // null-terminate
                span = buffer;
            }
            else
            {
                allocated = (byte*)Marshal.StringToCoTaskMemUTF8(str);
            }
        }

        public byte* ToNativeValue() => allocated != null ? allocated : (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

        public void FromNativeValue(byte* value) => allocated = value;

        public string? ToManaged() => Marshal.PtrToStringUTF8((IntPtr)allocated);

        public void FreeNative() => Marshal.FreeCoTaskMem((IntPtr)allocated);
    }

    [NativeMarshalling(typeof(IntStructWrapperNative))]
    public struct IntStructWrapper
    {
        public int Value;
    }

    [CustomTypeMarshaller(typeof(IntStructWrapper))]
    public struct IntStructWrapperNative
    {
        public int value;
        public IntStructWrapperNative(IntStructWrapper managed)
        {
            value = managed.Value;
        }

        public IntStructWrapper ToManaged() => new IntStructWrapper { Value = value };
    }

    [CustomTypeMarshaller(typeof(List<>), CustomTypeMarshallerKind.LinearCollection, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling | CustomTypeMarshallerFeatures.CallerAllocatedBuffer, BufferSize = 0x200)]
    public unsafe ref struct ListMarshaller<T>
    {
        private List<T> managedList;
        private readonly int sizeOfNativeElement;
        private IntPtr allocatedMemory;

        public ListMarshaller(int sizeOfNativeElement)
            : this()
        {
            this.sizeOfNativeElement = sizeOfNativeElement;
        }

        public ListMarshaller(List<T> managed, int sizeOfNativeElement)
            :this(managed, Span<byte>.Empty, sizeOfNativeElement)
        {
        }

        public ListMarshaller(List<T> managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            allocatedMemory = default;
            this.sizeOfNativeElement = sizeOfNativeElement;
            if (managed is null)
            {
                managedList = null;
                NativeValueStorage = default;
                return;
            }
            managedList = managed;
            // Always allocate at least one byte when the list is zero-length.
            int spaceToAllocate = Math.Max(managed.Count * sizeOfNativeElement, 1);
            if (spaceToAllocate <= stackSpace.Length)
            {
                NativeValueStorage = stackSpace[0..spaceToAllocate];
            }
            else
            {
                allocatedMemory = Marshal.AllocCoTaskMem(spaceToAllocate);
                NativeValueStorage = new Span<byte>((void*)allocatedMemory, spaceToAllocate);
            }
        }

        public ReadOnlySpan<T> GetManagedValuesSource() => CollectionsMarshal.AsSpan(managedList);

        public Span<T> GetManagedValuesDestination(int length)
        {
            if (allocatedMemory == IntPtr.Zero)
            {
                managedList = null;
                return default;
            }
            managedList = new List<T>(length);
            for (int i = 0; i < length; i++)
            {
                managedList.Add(default);
            }
            return CollectionsMarshal.AsSpan(managedList);
        }

        private Span<byte> NativeValueStorage { get; set; }

        public Span<byte> GetNativeValuesDestination() => NativeValueStorage;

        public ReadOnlySpan<byte> GetNativeValuesSource(int length)
        {
            return allocatedMemory == IntPtr.Zero ? default : NativeValueStorage = new Span<byte>((void*)allocatedMemory, length * sizeOfNativeElement);
        }

        public ref byte GetPinnableReference() => ref NativeValueStorage.GetPinnableReference();

        public byte* ToNativeValue() => (byte*)Unsafe.AsPointer(ref GetPinnableReference());

        public void FromNativeValue(byte* value)
        {
            allocatedMemory = (IntPtr)value;
        }

        public List<T> ToManaged() => managedList;

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem(allocatedMemory);
        }
    }
}
