// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test came from https://github.com/dotnet/runtime/issues/21860.
// It tests that we do access overlapping fields with the correct types.
// Especially if the struct was casted by 'Unsafe.As` from a promoted type
// and the promoted type had another field on the same offset but with a different type/size.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;

class TestAssignFieldsBetweenPromotedNotPromotedStructs
{

    struct PrimitiveStruct // a struct of single field of scalar types aligned at their natural boundary.
    {
        public long pointerSizedField;
    }

    struct NonPrimitiveStruct
    {
        public byte a;
        public long b;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct NotPromotedStruct
    {
        [FieldOffset(0)]
        public PrimitiveStruct notPromotedField;
        [FieldOffset(0)]
        public NonPrimitiveStruct anotherOverlappingStruct;

        [FieldOffset(8)]
        public long anotherField;


        public static ref PromotedStruct AsPromotedStructSize20(ref NotPromotedStruct d) => ref Unsafe.As<NotPromotedStruct, PromotedStruct>(ref d);
    }

    [StructLayout(LayoutKind.Explicit)]
    struct PromotedStruct
    {
        [FieldOffset(0)]
        public PrimitiveStruct promotedField;
        [FieldOffset(8)]
        public long anotherField;


        public static ref NotPromotedStruct AsNotPromotedStruct(ref PromotedStruct d) => ref Unsafe.As<PromotedStruct, NotPromotedStruct>(ref d);
    }

    // Some simple tests that check that lcl variables
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestStructCasts()
    {
        PromotedStruct a = new PromotedStruct(); // Addr-exposed, cannot be promoted.promotedField.
        a.promotedField.pointerSizedField = 4;
        a.anotherField = 5;
        NotPromotedStruct b = PromotedStruct.AsNotPromotedStruct(ref a);
        // The cast can be inlined and the field handle will refer to the `PromotedStruct.pointerSizedField`,
        // in this case we can promote it because `NotPromotedStruct.notPromotedField.pointerSizedField` has
        // the same class handle.
        Debug.Assert(b.notPromotedField.pointerSizedField == 0x4);

        NotPromotedStruct c = PromotedStruct.AsNotPromotedStruct(ref a);
        // The cast can be inlined and the field handle will refer to the `PromotedStruct.pointerSizedField`,
        // in this case we cannot promote it because `NotPromotedStruct.anotherOverlappingStruct.a` has
        // a different class handle (`NotPromotedStruct.anotherOverlappingStruct`).
        Debug.Assert(c.anotherOverlappingStruct.a == 0x4);

        Debug.Assert(c.anotherOverlappingStruct.b == 0x5);
    }

    public static int Main()
    {
        TestStructCasts();
        return 100;
    }

}

