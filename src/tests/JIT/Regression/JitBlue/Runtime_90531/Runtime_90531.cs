// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_90531
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct StructA
    {
        [FieldOffset(0)]
        public long Field0;
    }

    public struct StructB
    {
        public int Field0;
        // padded 4
        public StructA Field1;
        // no padding
        public int Field2;
    }

	// Size should be 16 in both 32 and 64 bits win/linux
	// Size should be 12 on 32bits OSX size alignment of long is 4
	[StructLayout (LayoutKind.Explicit)]
	struct TestStruct8 {
		[FieldOffset (0)]
		public int a;
		[FieldOffset (4)]
		public ulong b;
	}

	// Size should be 12 in both 32 and 64 bits
	[StructLayout (LayoutKind.Explicit, Size=12)]
	struct TestStruct9 {
		[FieldOffset (0)]
		public int a;
		[FieldOffset (4)]
		public ulong b;
	}

	// Size should be 16 in both 32 and 64 bits
	// Size should be 12 on 32bits OSX size alignment of long is 4
	[StructLayout (LayoutKind.Explicit)]
	struct TestStruct10 {
		[FieldOffset (0)]
		public int a;
		[FieldOffset (3)]
		public ulong b;
	}

	// Size should be 11 in both 32 and 64 bits
	[StructLayout (LayoutKind.Explicit, Size=11)]
	struct TestStruct11 {
		[FieldOffset (0)]
		public int a;
		[FieldOffset (3)]
		public ulong b;
	}

	[StructLayout (LayoutKind.Explicit, Pack=1)]
	struct TestStruct12 {
		[FieldOffset (0)]
		public short a;
		[FieldOffset (2)]
		public int b;
	}

	// Size should always be 12, since pack = 0, size = 0 and min alignment = 4
	//When pack is not set, we default to 8, so min (8, min alignment) -> 4
	[StructLayout (LayoutKind.Explicit)]
	struct TestStruct13 {
		[FieldOffset(0)]
		int one;
		[FieldOffset(4)]
		int two;
		[FieldOffset(8)]
		int three;
	}

	// Size should always be 12, since pack = 8, size = 0 and min alignment = 4
	//It's aligned to min (pack, min alignment) -> 4
	[StructLayout (LayoutKind.Explicit)]
	struct TestStruct14 {
		[FieldOffset(0)]
		int one;
		[FieldOffset(4)]
		int two;
		[FieldOffset(8)]
		int three;
	}

    [Fact]    
    public unsafe static int EntryPoint()
    {
        void* mem = stackalloc byte[24];
        Marshal.WriteInt32((IntPtr)mem, 0, 1);
        Marshal.WriteInt64((IntPtr)mem, 8, 2);
        Marshal.WriteInt32((IntPtr)mem, 16, 3);

        var s = Marshal.PtrToStructure<StructB>((IntPtr)mem);

        if (s.Field1.Field0 != 2)
            return 101;
        if(Marshal.SizeOf(typeof(TestStruct8)) != 16)
            return 102;
        if(Marshal.SizeOf(typeof(TestStruct9)) != 12)
            return 103;
        if(Marshal.SizeOf(typeof(TestStruct10)) != 16)
            return 104;
        if(Marshal.SizeOf(typeof(TestStruct11)) != 11)
            return 105;
        if(Marshal.SizeOf(typeof(TestStruct12)) != 6)
            return 106;
        return 100;
    }
}
