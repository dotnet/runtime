// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
	partial class WaitHandle
	{
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		unsafe static extern int Wait_internal(IntPtr* handles, int numHandles, bool waitAll, int ms);

		static int WaitOneCore (IntPtr waitHandle, int millisecondsTimeout)
		{
			unsafe {
				return Wait_internal (&waitHandle, 1, false, millisecondsTimeout);
			}
		}

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern int SignalAndWait_Internal (IntPtr waitHandleToSignal, IntPtr waitHandleToWaitOn, int millisecondsTimeout);

		const int ERROR_TOO_MANY_POSTS = 0x12A;
		const int ERROR_NOT_OWNED_BY_CALLER = 0x12B;

		static int SignalAndWaitCore (IntPtr waitHandleToSignal, IntPtr waitHandleToWaitOn, int millisecondsTimeout)
		{
			int ret = SignalAndWait_Internal (waitHandleToSignal, waitHandleToWaitOn, millisecondsTimeout);
			if (ret == ERROR_TOO_MANY_POSTS)
				throw new InvalidOperationException (SR.Threading_WaitHandleTooManyPosts);
			if (ret == ERROR_NOT_OWNED_BY_CALLER)
				throw new ApplicationException("Attempt to release mutex not owned by caller");
			return ret;
		}

		internal static int WaitMultipleIgnoringSyncContext (Span<IntPtr> waitHandles, bool waitAll, int millisecondsTimeout)
		{
			unsafe {
				fixed (IntPtr* handles = &MemoryMarshal.GetReference (waitHandles)) {
					return Wait_internal (handles, waitHandles.Length, waitAll, millisecondsTimeout);
				}
			}
		}
	}
}
