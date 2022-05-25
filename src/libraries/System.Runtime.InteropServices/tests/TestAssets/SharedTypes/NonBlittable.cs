// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
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

    [NativeMarshalling(typeof(WrappedListMarshaller<>))]
    public struct WrappedList<T>
    {
        public WrappedList(List<T> list)
        {
            Wrapped = list;
        }

        public List<T> Wrapped { get; }

        public ref T GetPinnableReference() => ref CollectionsMarshal.AsSpan(Wrapped).GetPinnableReference();
    }

    [CustomTypeMarshaller(typeof(WrappedList<>), CustomTypeMarshallerKind.LinearCollection, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling | CustomTypeMarshallerFeatures.CallerAllocatedBuffer, BufferSize = 0x200)]
    public unsafe ref struct WrappedListMarshaller<T>
    {
        private ListMarshaller<T> _marshaller;

        public WrappedListMarshaller(int sizeOfNativeElement)
            : this()
        {
            this._marshaller = new ListMarshaller<T>(sizeOfNativeElement);
        }

        public WrappedListMarshaller(WrappedList<T> managed, int sizeOfNativeElement)
            : this(managed, Span<byte>.Empty, sizeOfNativeElement)
        {
        }

        public WrappedListMarshaller(WrappedList<T> managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            this._marshaller = new ListMarshaller<T>(managed.Wrapped, stackSpace, sizeOfNativeElement);
        }

        public ReadOnlySpan<T> GetManagedValuesSource() => _marshaller.GetManagedValuesSource();

        public Span<T> GetManagedValuesDestination(int length) => _marshaller.GetManagedValuesDestination(length);

        public Span<byte> GetNativeValuesDestination() => _marshaller.GetNativeValuesDestination();

        public ReadOnlySpan<byte> GetNativeValuesSource(int length) => _marshaller.GetNativeValuesSource(length);

        public ref byte GetPinnableReference() => ref _marshaller.GetPinnableReference();

        public byte* ToNativeValue() => _marshaller.ToNativeValue();

        public void FromNativeValue(byte* value) => _marshaller.FromNativeValue(value);

        public WrappedList<T> ToManaged() => new(_marshaller.ToManaged());

        public void FreeNative() => _marshaller.FreeNative();
    }
}
