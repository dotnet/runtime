// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
	partial class ThreadPool
	{
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void InitializeVMTp (ref bool enableWorkerTracking);

		static void EnsureInitialized ()
		{
			if (!ThreadPoolGlobals.threadPoolInitialized) {
				ThreadPool.InitializeVMTp (ref ThreadPoolGlobals.enableWorkerTracking);
				ThreadPoolGlobals.threadPoolInitialized = true;
			}
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern bool RequestWorkerThread ();

		internal static bool KeepDispatching (int startTickCount) => true;

		internal static void NotifyWorkItemProgress ()
		{
			EnsureInitialized ();
			NotifyWorkItemProgressNative ();
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern void NotifyWorkItemProgressNative ();

		static RegisteredWaitHandle RegisterWaitForSingleObject (WaitHandle waitObject, WaitOrTimerCallback callBack, object state,
			uint millisecondsTimeOutInterval, bool executeOnlyOnce, bool compressStack)
		{
			if (waitObject == null)
				throw new ArgumentNullException ("waitObject");
			if (callBack == null)
				throw new ArgumentNullException ("callBack");
			if (millisecondsTimeOutInterval != Timeout.UnsignedInfinite && millisecondsTimeOutInterval > Int32.MaxValue)
				throw new NotSupportedException ("Timeout is too big. Maximum is Int32.MaxValue");

			RegisteredWaitHandle waiter = new RegisteredWaitHandle (waitObject, callBack, state, new TimeSpan (0, 0, 0, 0, (int) millisecondsTimeOutInterval), executeOnlyOnce);
			if (compressStack)
				QueueUserWorkItem (new WaitCallback (waiter.Wait), null);
			else
				UnsafeQueueUserWorkItem (new WaitCallback (waiter.Wait), null);

			return waiter;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern void ReportThreadStatus (bool isWorking);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern bool NotifyWorkItemComplete ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern bool SetMinThreadsNative (int workerThreads, int completionPortThreads);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern bool SetMaxThreadsNative (int workerThreads, int completionPortThreads);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void GetMinThreadsNative (out int workerThreads, out int completionPortThreads);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void GetMaxThreadsNative (out int workerThreads, out int completionPortThreads);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void GetAvailableThreadsNative (out int workerThreads, out int completionPortThreads);

		[MethodImpl(MethodImplOptions.InternalCall)]
		static extern long GetCompletedWorkItemCount ();

		[MethodImpl(MethodImplOptions.InternalCall)]
		static extern int GetThreadCount ();
		

		public static bool SetMaxThreads (int workerThreads, int completionPortThreads)
		{
			return SetMaxThreadsNative (workerThreads, completionPortThreads);
		}

		public static void GetMaxThreads (out int workerThreads, out int completionPortThreads)
		{
			GetMaxThreadsNative (out workerThreads, out completionPortThreads);
		}

		public static bool SetMinThreads (int workerThreads, int completionPortThreads)
		{
			return SetMinThreadsNative (workerThreads, completionPortThreads);
		}

		public static void GetMinThreads (out int workerThreads, out int completionPortThreads)
		{
			GetMinThreadsNative (out workerThreads, out completionPortThreads);
		}

		public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
		{
			GetAvailableThreadsNative (out workerThreads, out completionPortThreads);
		}

		public static bool BindHandle (IntPtr osHandle) => throw new NotImplementedException ();
		public static bool BindHandle (System.Runtime.InteropServices.SafeHandle osHandle)  => throw new NotImplementedException ();

		[CLSCompliant (false)]
		public static unsafe bool UnsafeQueueNativeOverlapped (NativeOverlapped* overlapped)  => throw new NotImplementedException ();

		static long PendingUnmanagedWorkItemCount => 0;
		
		public static long CompletedWorkItemCount => GetCompletedWorkItemCount ();

		public static int ThreadCount => GetThreadCount ();
	}

	internal static class _ThreadPoolWaitCallback
	{
		// This feature is used by Xamarin.iOS to use an NSAutoreleasePool
		// for every task done by the threadpool.
		static Func<Func<bool>, bool> dispatcher;

		internal static void SetDispatcher (Func<Func<bool>, bool> value)
		{
			dispatcher = value;
		}

		static internal bool PerformWaitCallback ()
		{
			// store locally first to ensure another thread doesn't clear the field between checking for null and using it.
			var dispatcher = _ThreadPoolWaitCallback.dispatcher;
			if (dispatcher != null)
				return dispatcher (ThreadPoolWorkQueue.Dispatch);

			return ThreadPoolWorkQueue.Dispatch ();
		}
	}
}
