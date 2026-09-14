// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

public unsafe class Program
{
    private const int Arm64AtomicGranuleSize = 16;
    private const int AllocationLimit = 100_000;

    [Fact]
    public static void TestEntryPoint()
    {
        VerifyReflectionAccess(
            new PackedInt16 { Value = 0x1234 },
            nameof(PackedInt16.Value),
            expectedOffset: 7,
            initialValue: (short)0x1234,
            replacementValue: (short)-1234);

        VerifyReflectionAccess(
            new PackedInt32 { Value = 0x12345678 },
            nameof(PackedInt32.Value),
            expectedOffset: 5,
            initialValue: 0x12345678,
            replacementValue: -123456789);

        VerifyReflectionAccess(
            new PackedInt64 { Value = 0x0123456789abcdef },
            nameof(PackedInt64.Value),
            expectedOffset: 1,
            initialValue: 0x0123456789abcdef,
            replacementValue: -0x0123456789abcdef);

        VerifyReflectionAccess(
            new PackedSingle { Value = 1.25f },
            nameof(PackedSingle.Value),
            expectedOffset: 5,
            initialValue: 1.25f,
            replacementValue: -42.5f);

        VerifyReflectionAccess(
            new PackedEnum { Value = Int32Enum.Initial },
            nameof(PackedEnum.Value),
            expectedOffset: 5,
            initialValue: Int32Enum.Initial,
            replacementValue: Int32Enum.Replacement);

        VerifyReflectionAccess(
            new PackedDouble { Value = 1.25 },
            nameof(PackedDouble.Value),
            expectedOffset: 1,
            initialValue: 1.25,
            replacementValue: -42.5);

        VerifyReflectionAccess(
            new PackedIntPtr { Value = (nint)0x12345678 },
            nameof(PackedIntPtr.Value),
            expectedOffset: 1,
            initialValue: (nint)0x12345678,
            replacementValue: (nint)(-0x12345678));

        VerifyReflectionAccess(
            new PackedFunctionPointer { Value = (delegate*<void>)0x12345678 },
            nameof(PackedFunctionPointer.Value),
            expectedOffset: 1,
            initialValue: (nint)0x12345678,
            replacementValue: (nint)(-0x12345678));

        VerifyAlignedReflectionAccesses();
        VerifyFirstSetReflectionAccess();
        VerifyStaticReflectionAccesses();
        VerifyDirectReflectionAccess();
        VerifyAlignedDirectReflectionAccess();
        VerifyEnumDirectReflectionAccess();
        VerifyFunctionPointerDirectReflectionAccess();
        VerifyIntPtrDirectReflectionAccess();
        VerifyPointerDirectReflectionAccess();
        VerifyPointerReflectionAccess();
        VerifyValueTypeEquals();
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
    public static void Int64AtAlignedOffsetOn32Bit()
    {
        const long InitialValue = 0x0123456789abcdef;
        const long ReplacementValue = -0x0123456789abcdef;

        FieldInfo field = typeof(PackedInt64AtOffsetZero).GetField(nameof(PackedInt64AtOffsetZero.Value)) ??
            throw new InvalidOperationException($"Field {nameof(PackedInt64AtOffsetZero.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<PackedInt64AtOffsetZero>(nameof(PackedInt64AtOffsetZero.Value)).ToInt32();

        Assert.Equal(0, fieldOffset);

        RunWithPinnedBoxAtMisalignedAddress(
            new PackedInt64AtOffsetZero { Value = InitialValue },
            fieldOffset,
            sizeof(long),
            boxed =>
            {
                Assert.Equal(InitialValue, (long)field.GetValue(boxed)!);
                Assert.Equal(InitialValue, (long)field.GetValue(boxed)!);
                field.SetValue(boxed, ReplacementValue);
                Assert.Equal(ReplacementValue, (long)field.GetValue(boxed)!);
            });
    }

    private static void VerifyReflectionAccess<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TStruct,
        TField>(
        TStruct value,
        string fieldName,
        int expectedOffset,
        TField initialValue,
        TField replacementValue)
        where TStruct : struct
        where TField : struct
    {
        FieldInfo field = typeof(TStruct).GetField(fieldName) ?? throw new InvalidOperationException($"Field {fieldName} was not found.");
        int fieldOffset = Marshal.OffsetOf<TStruct>(fieldName).ToInt32();

        Assert.Equal(expectedOffset, fieldOffset);

        RunWithPinnedBoxAtReflectionSensitiveAddress(value, fieldOffset, Unsafe.SizeOf<TField>(), boxed =>
        {
            Assert.Equal(initialValue, (TField)field.GetValue(boxed)!);
            Assert.Equal(initialValue, (TField)field.GetValue(boxed)!);
            field.SetValue(boxed, replacementValue);
            Assert.Equal(replacementValue, (TField)field.GetValue(boxed)!);
        });
    }

    private static void VerifyAlignedReflectionAccesses()
    {
        VerifyAlignedReflectionAccess(
            new AlignedByte { Value = 0x12 },
            nameof(AlignedByte.Value),
            initialValue: (byte)0x12,
            replacementValue: (byte)0xfe);

        VerifyAlignedReflectionAccess(
            new AlignedInt16 { Value = 0x1234 },
            nameof(AlignedInt16.Value),
            initialValue: (short)0x1234,
            replacementValue: (short)-1234);

        VerifyAlignedReflectionAccess(
            new AlignedInt32 { Value = 0x12345678 },
            nameof(AlignedInt32.Value),
            initialValue: 0x12345678,
            replacementValue: -123456789);

        VerifyAlignedReflectionAccess(
            new AlignedInt64 { Value = 0x0123456789abcdef },
            nameof(AlignedInt64.Value),
            initialValue: 0x0123456789abcdef,
            replacementValue: -0x0123456789abcdef);

        VerifyAlignedReflectionAccess(
            new AlignedEnum { Value = Int32Enum.Initial },
            nameof(AlignedEnum.Value),
            initialValue: Int32Enum.Initial,
            replacementValue: Int32Enum.Replacement);

        VerifyAlignedReflectionAccess(
            new AlignedIntPtr { Value = (nint)0x12345678 },
            nameof(AlignedIntPtr.Value),
            initialValue: (nint)0x12345678,
            replacementValue: (nint)(-0x12345678));

        VerifyAlignedPointerReflectionAccess();
        VerifyAlignedFunctionPointerReflectionAccess();
    }

    private static void VerifyAlignedReflectionAccess<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TStruct,
        TField>(
        TStruct value,
        string fieldName,
        TField initialValue,
        TField replacementValue)
        where TStruct : struct
        where TField : struct
    {
        FieldInfo field = typeof(TStruct).GetField(fieldName) ?? throw new InvalidOperationException($"Field {fieldName} was not found.");
        int fieldOffset = Marshal.OffsetOf<TStruct>(fieldName).ToInt32();

        RunWithPinnedBoxAtAlignedAddress(value, fieldOffset, Unsafe.SizeOf<TField>(), boxed =>
        {
            Assert.Equal(initialValue, (TField)field.GetValue(boxed)!);
            Assert.Equal(initialValue, (TField)field.GetValue(boxed)!);
            field.SetValue(boxed, replacementValue);
            Assert.Equal(replacementValue, (TField)field.GetValue(boxed)!);
        });
    }

    private static void VerifyFirstSetReflectionAccess()
    {
        const int FirstValue = 0x12345678;
        const int SecondValue = -123456789;

        FieldInfo field = typeof(AlignedInt32SetFirst).GetField(nameof(AlignedInt32SetFirst.Value)) ??
            throw new InvalidOperationException($"Field {nameof(AlignedInt32SetFirst.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<AlignedInt32SetFirst>(nameof(AlignedInt32SetFirst.Value)).ToInt32();

        RunWithPinnedBoxAtAlignedAddress(new AlignedInt32SetFirst(), fieldOffset, sizeof(int), boxed =>
        {
            field.SetValue(boxed, FirstValue);
            Assert.Equal(FirstValue, (int)field.GetValue(boxed)!);
            field.SetValue(boxed, SecondValue);
            Assert.Equal(SecondValue, (int)field.GetValue(boxed)!);
        });
    }

    private static void VerifyStaticReflectionAccesses()
    {
        VerifyStaticReflectionAccess(nameof(StaticFields.ByteValue), (byte)0x12, (byte)0xfe);
        VerifyStaticReflectionAccess(nameof(StaticFields.Int16Value), (short)0x1234, (short)-1234);
        VerifyStaticReflectionAccess(nameof(StaticFields.Int32Value), 0x12345678, -123456789);
        VerifyStaticReflectionAccess(nameof(StaticFields.Int64Value), 0x0123456789abcdef, -0x0123456789abcdef);
        VerifyStaticReflectionAccess(nameof(StaticFields.EnumValue), Int32Enum.Initial, Int32Enum.Replacement);
        VerifyStaticReflectionAccess(nameof(StaticFields.IntPtrValue), (nint)0x12345678, (nint)(-0x12345678));
        VerifyStaticPointerReflectionAccess();
        VerifyStaticFunctionPointerReflectionAccess();

        FieldInfo setFirst = typeof(StaticFields).GetField(nameof(StaticFields.SetFirstValue)) ??
            throw new InvalidOperationException($"Field {nameof(StaticFields.SetFirstValue)} was not found.");
        setFirst.SetValue(null, 0x12345678);
        Assert.Equal(0x12345678, (int)setFirst.GetValue(null)!);
        setFirst.SetValue(null, -123456789);
        Assert.Equal(-123456789, (int)setFirst.GetValue(null)!);

        long target = 0;
        TypedReference reference = __makeref(target);
        FieldInfo direct = typeof(StaticFields).GetField(nameof(StaticFields.DirectValue)) ??
            throw new InvalidOperationException($"Field {nameof(StaticFields.DirectValue)} was not found.");
        Assert.Equal(0x0123456789abcdef, (long)direct.GetValueDirect(reference));
        Assert.Equal(0x0123456789abcdef, (long)direct.GetValueDirect(reference));
        direct.SetValueDirect(reference, -0x0123456789abcdef);
        Assert.Equal(-0x0123456789abcdef, (long)direct.GetValueDirect(reference));

        FieldInfo directByte = typeof(StaticFields).GetField(nameof(StaticFields.DirectByteValue)) ??
            throw new InvalidOperationException($"Field {nameof(StaticFields.DirectByteValue)} was not found.");
        Assert.Equal((byte)0x12, (byte)directByte.GetValueDirect(reference));
        directByte.SetValueDirect(reference, (byte)0xfe);
        Assert.Equal((byte)0xfe, (byte)directByte.GetValueDirect(reference));

        FieldInfo directPointer = typeof(StaticFields).GetField(nameof(StaticFields.DirectPointerValue)) ??
            throw new InvalidOperationException($"Field {nameof(StaticFields.DirectPointerValue)} was not found.");
        Assert.Equal((nint)0x12345678, (nint)Pointer.Unbox(directPointer.GetValueDirect(reference)));
        directPointer.SetValueDirect(reference, Pointer.Box((void*)(nint)(-0x12345678), typeof(int*)));
        Assert.Equal((nint)(-0x12345678), (nint)Pointer.Unbox(directPointer.GetValueDirect(reference)));

        FieldInfo directFunctionPointer = typeof(StaticFields).GetField(nameof(StaticFields.DirectFunctionPointerValue)) ??
            throw new InvalidOperationException($"Field {nameof(StaticFields.DirectFunctionPointerValue)} was not found.");
        Assert.Equal((nint)0x12345678, (nint)directFunctionPointer.GetValueDirect(reference));
        directFunctionPointer.SetValueDirect(reference, (nint)(-0x12345678));
        Assert.Equal((nint)(-0x12345678), (nint)directFunctionPointer.GetValueDirect(reference));
    }

    private static void VerifyStaticReflectionAccess<TField>(string fieldName, TField initialValue, TField replacementValue)
        where TField : struct
    {
        FieldInfo field = typeof(StaticFields).GetField(fieldName) ??
            throw new InvalidOperationException($"Field {fieldName} was not found.");

        Assert.Equal(initialValue, (TField)field.GetValue(null)!);
        Assert.Equal(initialValue, (TField)field.GetValue(null)!);
        field.SetValue(null, replacementValue);
        Assert.Equal(replacementValue, (TField)field.GetValue(null)!);
    }

    private static void VerifyDirectReflectionAccess()
    {
        const short InitialValue = 0x1234;
        const short ReplacementValue = -1234;

        FieldInfo field = typeof(PackedInt16).GetField(nameof(PackedInt16.Value)) ??
            throw new InvalidOperationException($"Field {nameof(PackedInt16.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<PackedInt16>(nameof(PackedInt16.Value)).ToInt32();

        RunWithPinnedBoxAtReflectionSensitiveAddress(
            new PackedInt16 { Value = InitialValue },
            fieldOffset,
            sizeof(short),
            boxed =>
            {
                ref PackedInt16 value = ref Unsafe.Unbox<PackedInt16>(boxed);
                TypedReference reference = __makeref(value);

                Assert.Equal(InitialValue, (short)field.GetValueDirect(reference));
                Assert.Equal(InitialValue, (short)field.GetValueDirect(reference));
                field.SetValueDirect(reference, ReplacementValue);
                Assert.Equal(ReplacementValue, value.Value);
            });
    }

    private static void VerifyAlignedDirectReflectionAccess()
    {
        const int InitialValue = 0x12345678;
        const int FirstReplacement = -123456789;
        const int SecondReplacement = 0x10203040;

        FieldInfo field = typeof(AlignedInt32).GetField(nameof(AlignedInt32.Value)) ??
            throw new InvalidOperationException($"Field {nameof(AlignedInt32.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<AlignedInt32>(nameof(AlignedInt32.Value)).ToInt32();

        RunWithPinnedBoxAtAlignedAddress(
            new AlignedInt32 { Value = InitialValue },
            fieldOffset,
            sizeof(int),
            boxed =>
            {
                ref AlignedInt32 value = ref Unsafe.Unbox<AlignedInt32>(boxed);
                TypedReference reference = __makeref(value);

                Assert.Equal(InitialValue, (int)field.GetValueDirect(reference));
                Assert.Equal(InitialValue, (int)field.GetValueDirect(reference));
                field.SetValueDirect(reference, FirstReplacement);
                Assert.Equal(FirstReplacement, value.Value);
                field.SetValueDirect(reference, SecondReplacement);
                Assert.Equal(SecondReplacement, value.Value);
            });
    }

    private static void VerifyEnumDirectReflectionAccess()
    {
        FieldInfo field = typeof(PackedEnum).GetField(nameof(PackedEnum.Value)) ??
            throw new InvalidOperationException($"Field {nameof(PackedEnum.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<PackedEnum>(nameof(PackedEnum.Value)).ToInt32();

        RunWithPinnedBoxAtReflectionSensitiveAddress(
            new PackedEnum { Value = Int32Enum.Initial },
            fieldOffset,
            sizeof(int),
            boxed =>
            {
                ref PackedEnum value = ref Unsafe.Unbox<PackedEnum>(boxed);
                TypedReference reference = __makeref(value);

                Assert.Equal(Int32Enum.Initial, (Int32Enum)field.GetValueDirect(reference));
                Assert.Equal(Int32Enum.Initial, (Int32Enum)field.GetValueDirect(reference));
                field.SetValueDirect(reference, Int32Enum.Replacement);
                Assert.Equal(Int32Enum.Replacement, value.Value);
            });
    }

    private static void VerifyPointerReflectionAccess()
    {
        const nint InitialValue = (nint)0x12345678;
        const nint ReplacementValue = (nint)(-0x12345678);

        FieldInfo field = typeof(PackedPointer).GetField(nameof(PackedPointer.Value)) ??
            throw new InvalidOperationException($"Field {nameof(PackedPointer.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<PackedPointer>(nameof(PackedPointer.Value)).ToInt32();

        Assert.Equal(1, fieldOffset);

        RunWithPinnedBoxAtReflectionSensitiveAddress(
            new PackedPointer { Value = (int*)InitialValue },
            fieldOffset,
            IntPtr.Size,
            boxed =>
            {
                Assert.Equal(InitialValue, (nint)Pointer.Unbox(field.GetValue(boxed)!));
                Assert.Equal(InitialValue, (nint)Pointer.Unbox(field.GetValue(boxed)!));
                field.SetValue(boxed, Pointer.Box((void*)ReplacementValue, typeof(int*)));
                Assert.Equal(ReplacementValue, (nint)Pointer.Unbox(field.GetValue(boxed)!));
            });
    }

    private static void VerifyPointerDirectReflectionAccess()
    {
        const nint InitialValue = (nint)0x12345678;
        const nint ReplacementValue = (nint)(-0x12345678);

        FieldInfo field = typeof(PackedPointer).GetField(nameof(PackedPointer.Value)) ??
            throw new InvalidOperationException($"Field {nameof(PackedPointer.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<PackedPointer>(nameof(PackedPointer.Value)).ToInt32();

        RunWithPinnedBoxAtReflectionSensitiveAddress(
            new PackedPointer { Value = (int*)InitialValue },
            fieldOffset,
            IntPtr.Size,
            boxed =>
            {
                ref PackedPointer value = ref Unsafe.Unbox<PackedPointer>(boxed);
                TypedReference reference = __makeref(value);

                Assert.Equal(InitialValue, (nint)Pointer.Unbox(field.GetValueDirect(reference)));
                Assert.Equal(InitialValue, (nint)Pointer.Unbox(field.GetValueDirect(reference)));
                field.SetValueDirect(reference, Pointer.Box((void*)ReplacementValue, typeof(int*)));
                Assert.Equal(ReplacementValue, (nint)value.Value);
            });
    }

    private static void VerifyAlignedPointerReflectionAccess()
    {
        const nint InitialValue = (nint)0x12345678;
        const nint ReplacementValue = (nint)(-0x12345678);

        FieldInfo field = typeof(AlignedPointer).GetField(nameof(AlignedPointer.Value)) ??
            throw new InvalidOperationException($"Field {nameof(AlignedPointer.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<AlignedPointer>(nameof(AlignedPointer.Value)).ToInt32();

        RunWithPinnedBoxAtAlignedAddress(
            new AlignedPointer { Value = (int*)InitialValue },
            fieldOffset,
            IntPtr.Size,
            boxed =>
            {
                Assert.Equal(InitialValue, (nint)Pointer.Unbox(field.GetValue(boxed)!));
                Assert.Equal(InitialValue, (nint)Pointer.Unbox(field.GetValue(boxed)!));
                field.SetValue(boxed, Pointer.Box((void*)ReplacementValue, typeof(int*)));
                Assert.Equal(ReplacementValue, (nint)Pointer.Unbox(field.GetValue(boxed)!));
            });
    }

    private static void VerifyAlignedFunctionPointerReflectionAccess()
    {
        const nint InitialValue = (nint)0x12345678;
        const nint ReplacementValue = (nint)(-0x12345678);

        FieldInfo field = typeof(AlignedFunctionPointer).GetField(nameof(AlignedFunctionPointer.Value)) ??
            throw new InvalidOperationException($"Field {nameof(AlignedFunctionPointer.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<AlignedFunctionPointer>(nameof(AlignedFunctionPointer.Value)).ToInt32();

        RunWithPinnedBoxAtAlignedAddress(
            new AlignedFunctionPointer { Value = (delegate*<void>)InitialValue },
            fieldOffset,
            IntPtr.Size,
            boxed =>
            {
                Assert.Equal(InitialValue, (nint)field.GetValue(boxed)!);
                Assert.Equal(InitialValue, (nint)field.GetValue(boxed)!);
                field.SetValue(boxed, ReplacementValue);
                Assert.Equal(ReplacementValue, (nint)field.GetValue(boxed)!);
            });
    }

    private static void VerifyStaticPointerReflectionAccess()
    {
        const nint InitialValue = (nint)0x12345678;
        const nint ReplacementValue = (nint)(-0x12345678);

        FieldInfo field = typeof(StaticFields).GetField(nameof(StaticFields.PointerValue)) ??
            throw new InvalidOperationException($"Field {nameof(StaticFields.PointerValue)} was not found.");

        Assert.Equal(InitialValue, (nint)Pointer.Unbox(field.GetValue(null)!));
        Assert.Equal(InitialValue, (nint)Pointer.Unbox(field.GetValue(null)!));
        field.SetValue(null, Pointer.Box((void*)ReplacementValue, typeof(int*)));
        Assert.Equal(ReplacementValue, (nint)Pointer.Unbox(field.GetValue(null)!));
    }

    private static void VerifyStaticFunctionPointerReflectionAccess()
    {
        const nint InitialValue = (nint)0x12345678;
        const nint ReplacementValue = (nint)(-0x12345678);

        FieldInfo field = typeof(StaticFields).GetField(nameof(StaticFields.FunctionPointerValue)) ??
            throw new InvalidOperationException($"Field {nameof(StaticFields.FunctionPointerValue)} was not found.");

        Assert.Equal(InitialValue, (nint)field.GetValue(null)!);
        Assert.Equal(InitialValue, (nint)field.GetValue(null)!);
        field.SetValue(null, ReplacementValue);
        Assert.Equal(ReplacementValue, (nint)field.GetValue(null)!);
    }

    private static void VerifyFunctionPointerDirectReflectionAccess()
    {
        const nint InitialValue = (nint)0x12345678;
        const nint ReplacementValue = (nint)(-0x12345678);

        FieldInfo field = typeof(PackedFunctionPointer).GetField(nameof(PackedFunctionPointer.Value)) ??
            throw new InvalidOperationException($"Field {nameof(PackedFunctionPointer.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<PackedFunctionPointer>(nameof(PackedFunctionPointer.Value)).ToInt32();

        RunWithPinnedBoxAtReflectionSensitiveAddress(
            new PackedFunctionPointer { Value = (delegate*<void>)InitialValue },
            fieldOffset,
            IntPtr.Size,
            boxed =>
            {
                ref PackedFunctionPointer value = ref Unsafe.Unbox<PackedFunctionPointer>(boxed);
                TypedReference reference = __makeref(value);

                Assert.Equal(InitialValue, (nint)field.GetValueDirect(reference));
                field.SetValueDirect(reference, ReplacementValue);
                Assert.Equal(ReplacementValue, (nint)value.Value);
                Assert.Equal(ReplacementValue, (nint)field.GetValueDirect(reference));
            });
    }

    private static void VerifyIntPtrDirectReflectionAccess()
    {
        const nint InitialValue = (nint)0x12345678;
        const nint ReplacementValue = (nint)(-0x12345678);

        FieldInfo field = typeof(PackedIntPtr).GetField(nameof(PackedIntPtr.Value)) ??
            throw new InvalidOperationException($"Field {nameof(PackedIntPtr.Value)} was not found.");
        int fieldOffset = Marshal.OffsetOf<PackedIntPtr>(nameof(PackedIntPtr.Value)).ToInt32();

        RunWithPinnedBoxAtReflectionSensitiveAddress(
            new PackedIntPtr { Value = InitialValue },
            fieldOffset,
            IntPtr.Size,
            boxed =>
            {
                ref PackedIntPtr value = ref Unsafe.Unbox<PackedIntPtr>(boxed);
                TypedReference reference = __makeref(value);

                Assert.Equal(InitialValue, (nint)field.GetValueDirect(reference));
                Assert.Equal(InitialValue, (nint)field.GetValueDirect(reference));
                field.SetValueDirect(reference, ReplacementValue);
                Assert.Equal(ReplacementValue, value.Value);
            });
    }

    private static void VerifyValueTypeEquals()
    {
        PackedDouble value = new PackedDouble { Value = 1.25 };
        int fieldOffset = Marshal.OffsetOf<PackedDouble>(nameof(PackedDouble.Value)).ToInt32();

        RunWithPinnedBoxAtReflectionSensitiveAddress(value, fieldOffset, sizeof(double), boxed =>
        {
            Assert.True(boxed.Equals((object)value));
        });
    }

    private static void RunWithPinnedBoxAtReflectionSensitiveAddress<TStruct>(
        TStruct value,
        int fieldOffset,
        int fieldSize,
        Action<object> action)
        where TStruct : struct
    {
        RunWithPinnedBox(
            value,
            fieldOffset,
            fieldAddress => RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ?
                (fieldAddress & (Arm64AtomicGranuleSize - 1)) + fieldSize > Arm64AtomicGranuleSize :
                (fieldAddress & (fieldSize - 1)) != 0,
            action);
    }

    private static void RunWithPinnedBoxAtMisalignedAddress<TStruct>(
        TStruct value,
        int fieldOffset,
        int fieldSize,
        Action<object> action)
        where TStruct : struct
    {
        RunWithPinnedBox(value, fieldOffset, fieldAddress => (fieldAddress & (fieldSize - 1)) != 0, action);
    }

    private static void RunWithPinnedBoxAtAlignedAddress<TStruct>(
        TStruct value,
        int fieldOffset,
        int fieldSize,
        Action<object> action)
        where TStruct : struct
    {
        RunWithPinnedBox(value, fieldOffset, fieldAddress => (fieldAddress & (fieldSize - 1)) == 0, action);
    }

    private static void RunWithPinnedBox<TStruct>(
        TStruct value,
        int fieldOffset,
        Func<long, bool> isRequestedAddress,
        Action<object> action)
        where TStruct : struct
    {
        List<GCHandle> handles = new();

        try
        {
            for (int i = 0; i < AllocationLimit; i++)
            {
                object boxed = value;
                GCHandle handle = GCHandle.Alloc(boxed, GCHandleType.Pinned);
                handles.Add(handle);

                long fieldAddress = handle.AddrOfPinnedObject().ToInt64() + fieldOffset;
                if (isRequestedAddress(fieldAddress))
                {
                    action(boxed);
                    return;
                }
            }
        }
        finally
        {
            foreach (GCHandle handle in handles)
            {
                handle.Free();
            }
        }

        throw new InvalidOperationException("Unable to allocate a boxed value with the requested field placement.");
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PackedInt16
    {
        public byte Padding0;
        public byte Padding1;
        public byte Padding2;
        public byte Padding3;
        public byte Padding4;
        public byte Padding5;
        public byte Padding6;
        public short Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PackedInt32
    {
        public byte Padding0;
        public byte Padding1;
        public byte Padding2;
        public byte Padding3;
        public byte Padding4;
        public int Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PackedInt64
    {
        public byte Padding;
        public long Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PackedInt64AtOffsetZero
    {
        public long Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PackedSingle
    {
        public byte Padding0;
        public byte Padding1;
        public byte Padding2;
        public byte Padding3;
        public byte Padding4;
        public float Value;
    }

    private enum Int32Enum
    {
        Initial = 0x12345678,
        Replacement = -123456789,
    }

    private struct AlignedByte
    {
        public byte Value;
    }

    private struct AlignedInt16
    {
        public short Value;
    }

    private struct AlignedInt32
    {
        public int Value;
    }

    private struct AlignedInt32SetFirst
    {
        public int Value;
    }

    private struct AlignedInt64
    {
        public long Value;
    }

    private struct AlignedEnum
    {
        public Int32Enum Value;
    }

    private struct AlignedIntPtr
    {
        public nint Value;
    }

    private struct AlignedPointer
    {
        public int* Value;
    }

    private struct AlignedFunctionPointer
    {
        public delegate*<void> Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PackedEnum
    {
        public byte Padding0;
        public byte Padding1;
        public byte Padding2;
        public byte Padding3;
        public byte Padding4;
        public Int32Enum Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PackedDouble
    {
        public byte Padding;
        public double Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PackedIntPtr
    {
        public byte Padding;
        public nint Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PackedPointer
    {
        public byte Padding;
        public int* Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PackedFunctionPointer
    {
        public byte Padding;
        public delegate*<void> Value;
    }

    private static class StaticFields
    {
        public static byte ByteValue = 0x12;
        public static short Int16Value = 0x1234;
        public static int Int32Value = 0x12345678;
        public static long Int64Value = 0x0123456789abcdef;
        public static Int32Enum EnumValue = Int32Enum.Initial;
        public static nint IntPtrValue = (nint)0x12345678;
        public static int* PointerValue = (int*)0x12345678;
        public static delegate*<void> FunctionPointerValue = (delegate*<void>)0x12345678;
        public static int SetFirstValue;
        public static byte DirectByteValue = 0x12;
        public static long DirectValue = 0x0123456789abcdef;
        public static int* DirectPointerValue = (int*)0x12345678;
        public static delegate*<void> DirectFunctionPointerValue = (delegate*<void>)0x12345678;
    }
}
