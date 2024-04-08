// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

// Structs come from from System.Net.Sockets tests
internal struct FakeArraySegment
{
	public byte[] Array;
	public int Offset;
	public int Count;

	public ArraySegment<byte> ToActual()
	{
		ArraySegmentWrapper wrapper = default(ArraySegmentWrapper);
		wrapper.Fake = this;
		return wrapper.Actual;
	}
}

[StructLayout(LayoutKind.Explicit)]
internal struct ArraySegmentWrapper
{
	[FieldOffset(0)] public ArraySegment<byte> Actual;
	[FieldOffset(0)] public FakeArraySegment Fake;
}

public class Test_ExplicitLayoutWithArraySegment
{
	private void Run()
    {
		var fakeArraySegment = new FakeArraySegment() { Array = new byte[10], Offset = 0, Count = 10 };
		ArraySegment<byte> internalBuffer = fakeArraySegment.ToActual();
    }

	[Fact]
	public static int TestEntryPoint()
	{
		try
		{
			var testInstance = new Test_ExplicitLayoutWithArraySegment();
			testInstance.Run();
		}
		catch (TypeLoadException e)
		{
			Console.WriteLine("FAIL: Caught TypeLoadException: " + e.Message);
			return 101;
		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL: Caught unexpected exception: " + e.Message);
			return 101;
		}

		return 100;
	}
}
