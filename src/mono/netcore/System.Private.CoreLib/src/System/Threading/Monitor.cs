// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Threading
{
	public static class Monitor
	{
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public static extern void Enter (object obj);

		public static void Enter (object obj, ref bool lockTaken)
		{
			if (lockTaken)
				throw new ArgumentException (SR.Argument_MustBeFalse, nameof (lockTaken));

			ReliableEnter (obj, ref lockTaken);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public static extern void Exit (object obj);

		public static bool TryEnter (object obj)
		{
			bool lockTaken = false;
			TryEnter (obj, 0, ref lockTaken);
			return lockTaken;
		}

		public static void TryEnter (object obj, ref bool lockTaken)
		{
			if (lockTaken)
				throw new ArgumentException (SR.Argument_MustBeFalse, nameof (lockTaken));

			ReliableEnterTimeout (obj, 0, ref lockTaken);
		}

		public static bool TryEnter (object obj, int millisecondsTimeout)
		{
			bool lockTaken = false;
			TryEnter (obj, millisecondsTimeout, ref lockTaken);
			return lockTaken;
		}

		static int MillisecondsTimeoutFromTimeSpan (TimeSpan timeout)
		{
			long tm = (long) timeout.TotalMilliseconds;
			if (tm < -1 || tm > (long) int.MaxValue)
				throw new ArgumentOutOfRangeException (nameof (timeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
			return (int) tm;
		}

		public static bool TryEnter (object obj, TimeSpan timeout)
		{
			return TryEnter (obj, MillisecondsTimeoutFromTimeSpan (timeout));
		}

		public static void TryEnter (object obj, int millisecondsTimeout, ref bool lockTaken)
		{
			if (lockTaken)
				throw new ArgumentException (SR.Argument_MustBeFalse, nameof (lockTaken));
			ReliableEnterTimeout (obj, millisecondsTimeout, ref lockTaken);
		}

		public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken)
		{
			if (lockTaken)
				throw new ArgumentException (SR.Argument_MustBeFalse, nameof (lockTaken));
			ReliableEnterTimeout (obj, MillisecondsTimeoutFromTimeSpan (timeout), ref lockTaken);
		}

		public static bool IsEntered (object obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			return IsEnteredNative (obj);
		}

		public static bool Wait (object obj, int millisecondsTimeout, bool exitContext)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			return ObjWait (exitContext, millisecondsTimeout, obj);
		}

		public static bool Wait (object obj, TimeSpan timeout, bool exitContext) => Wait (obj, MillisecondsTimeoutFromTimeSpan (timeout), exitContext);

		public static bool Wait (object obj, int millisecondsTimeout) => Wait (obj, millisecondsTimeout, false);

		public static bool Wait(object obj, TimeSpan timeout) => Wait (obj, MillisecondsTimeoutFromTimeSpan (timeout), false);

		public static bool Wait(object obj) => Wait (obj, Timeout.Infinite, false);

		public static void Pulse (object obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			ObjPulse (obj);
		}

		public static void PulseAll (object obj)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));
			ObjPulseAll (obj);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static bool Monitor_test_synchronised (object obj);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void Monitor_pulse (object obj);

		static void ObjPulse (object obj)
		{
			if (!Monitor_test_synchronised (obj))
				throw new SynchronizationLockException ("Object is not synchronized");

			Monitor_pulse (obj);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void Monitor_pulse_all (object obj);

		static void ObjPulseAll (object obj)
		{
			if (!Monitor_test_synchronised (obj))
				throw new SynchronizationLockException ("Object is not synchronized");

			Monitor_pulse_all (obj);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool Monitor_wait (object obj, int ms, bool allowInterruption);

		static bool ObjWait (bool exitContext, int millisecondsTimeout, object obj)
		{
			if (millisecondsTimeout < 0 && millisecondsTimeout != (int) Timeout.Infinite)
				throw new ArgumentOutOfRangeException ("millisecondsTimeout");
			if (!Monitor_test_synchronised (obj))
				throw new SynchronizationLockException ("Object is not synchronized");

			return Monitor_wait (obj, millisecondsTimeout, true);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static void try_enter_with_atomic_var (object obj, int millisecondsTimeout, bool allowInterruption, ref bool lockTaken);

		static void ReliableEnterTimeout (object obj, int timeout, ref bool lockTaken)
		{
			if (obj == null)
				throw new ArgumentNullException (nameof (obj));

			if (timeout < 0 && timeout != (int) Timeout.Infinite)
				throw new ArgumentOutOfRangeException (nameof (timeout));

			try_enter_with_atomic_var (obj, timeout, true, ref lockTaken);
		}

		static void ReliableEnter (object obj, ref bool lockTaken)
		{
			ReliableEnterTimeout (obj, (int) Timeout.Infinite, ref lockTaken);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static bool Monitor_test_owner (object obj);

		static bool IsEnteredNative (object obj)
		{
			return Monitor_test_owner (obj);
		}
		
		public extern static long LockContentionCount
		{
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}
	}
}
