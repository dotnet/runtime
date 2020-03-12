// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
	public sealed class PreAllocatedOverlapped : System.IDisposable
	{
		[System.CLSCompliantAttribute(false)]
		public PreAllocatedOverlapped(System.Threading.IOCompletionCallback callback, object state, object pinData) { }
		public void Dispose() { }
	}
}
