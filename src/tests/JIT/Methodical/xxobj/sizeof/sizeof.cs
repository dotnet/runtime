// Licensed to the .NET Foundation under one or more agreements.
using Xunit;
namespace Test_sizeof
{
// The .NET Foundation licenses this file to you under the MIT license.

namespace JitTest
{
	using System;

	struct SimpleStruct
	{
		int m_int;
		uint m_uint;
		byte m_byte;
		sbyte m_sbyte;
		char m_char;
		short m_short;
		ushort m_ushort;
		long m_long;
		ulong m_ulong;
	}

	struct ComplexStruct
	{
		SimpleStruct ss1;
		SimpleStruct ss2;
	}
	
	struct ComplexStruct2
	{
		ComplexStruct x1;
		ComplexStruct x2;
		ComplexStruct x3;
		ComplexStruct x4;
		ComplexStruct x5;
		ComplexStruct x6;
		ComplexStruct x7;
		ComplexStruct x8;
		ComplexStruct x9;
		ComplexStruct x10;
		ComplexStruct x11;
		ComplexStruct x12;
		ComplexStruct x13;
		ComplexStruct x14;
		ComplexStruct x15;
		ComplexStruct x16;
		ComplexStruct x17;
		ComplexStruct x18;
	}

	public struct Test
	{
		[Fact]
		public static unsafe int TestEntryPoint()
		{
			if (sizeof(SimpleStruct) != 32)
			{
				Console.WriteLine("sizeof(SimpleStruct) failed.");
				return 101;
			}
			if (sizeof(ComplexStruct) != 64)
			{
				Console.WriteLine("sizeof(ComplexStruct) failed.");
				return 102;
			}
			if (sizeof(ComplexStruct2) != sizeof(ComplexStruct) * 18)
			{
				Console.WriteLine("sizeof(ComplexStruct2) failed.");
				return 103;
			}
			Console.WriteLine("sizeof passed");
			return 100;
		}
	}
}
}
