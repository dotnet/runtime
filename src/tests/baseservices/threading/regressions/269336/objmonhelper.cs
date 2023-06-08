// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

public class ObjMonHelper {
	const string FailMsg = @"Monitor.Enter appears to have mistaken a hash code in an object header for
a valid lock owned by the current thread.";

	[Fact]
	public static int TestEntryPoint() {
		var ok = true;
		var arr = new object[1024*1024];

		// Call GetHashCode to populate the object header with its hash
		for (var i = 0; i < arr.Length; i++) {
			arr[i] = new object();
			arr[i].GetHashCode();
		}

		// Attempt to lock and unlock each object. If the bug is present, the object will appear
		// to be locked by the current thread and Monitor.Enter will incorrectly take a fast path.
		// Monitor.Exit will then correctly take the slow path, find that the object is not locked,
		// and throw.
		try {
			for (var i = 0; i < arr.Length; i++)
				lock (arr[i])
					GC.KeepAlive(arr[i]);
		} catch (SynchronizationLockException) {
			ok = false;
		}

		Console.WriteLine(ok ? "Test passed" : FailMsg);
		return ok ? 100 : -1;
	}
}
