// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test came from https://github.com/dotnet/runtime/issues/21860.
// It tries to access field from a promoted struct with an offset that 
// is not valid for the promoted struct type.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;
using Xunit;

public class TestStructAccessThroughRef
{

    [StructLayout(LayoutKind.Explicit)]
    struct NotPromotedStruct
    {

        [FieldOffset(0)]
        public long anotherField;
        [FieldOffset(4)] // Offset doesn't correspond to a valid offset in PromotedStructSize.
        public long overlappingField;

        public static ref PromotedStruct AsPromotedStructSize20(ref NotPromotedStruct d) => ref Unsafe.As<NotPromotedStruct, PromotedStruct>(ref d);
    }

    [StructLayout(LayoutKind.Explicit)]
    struct PromotedStruct
    {
        [FieldOffset(0)]
        public long anotherField;
        [FieldOffset(8)]
        public int smallField;

        public static ref NotPromotedStruct AsNotPromotedStruct(ref PromotedStruct d) => ref Unsafe.As<PromotedStruct, NotPromotedStruct>(ref d);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestStructCasts()
    {
        PromotedStruct a = new PromotedStruct(); // Addr-exposed, cannot be independent promoted.
        a.anotherField = 5;
        a.smallField = 6;

        Debug.Assert(PromotedStruct.AsNotPromotedStruct(ref a).anotherField == 0x5);

        // This access will ask LclVariable with type of `PromotedStruct` about the field with offset == 4, that doesn't exist there.
        Debug.Assert(PromotedStruct.AsNotPromotedStruct(ref a).overlappingField == 0x600000000);
        a.smallField = 6;
        Debug.Assert(PromotedStruct.AsNotPromotedStruct(ref a).overlappingField == 0x600000000);
        PromotedStruct.AsNotPromotedStruct(ref a).overlappingField = 0x700000000;
        Debug.Assert(a.smallField == 0x7);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        TestStructCasts();
        return 100;
    }

}
