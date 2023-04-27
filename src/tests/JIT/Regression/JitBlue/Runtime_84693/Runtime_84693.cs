// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
    // This is trying to verify that we zero-extend from the result of "(byte)(-s_2)".
	public class Program
	{
		public static short s_2;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Consume(int x) {}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static int M8(byte arg0)
		{
			s_2 = 1;
			arg0 = (byte)(-s_2);
			var vr1 = arg0 & arg0;
			Consume(vr1);
			return vr1;
		}
	}

	[Fact(Skip = "https://github.com/dotnet/runtime/issues/85081")]
	public static int TestEntryPoint() {
		var result = Test.Program.M8(1);
		if (result != 255)
		{
			return 0;
		}
		return 100;
	}
}
