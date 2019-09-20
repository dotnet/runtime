// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading
{
	public static class Interlocked
	{
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int CompareExchange (ref int location1, int value, int comparand);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static void CompareExchange (ref object location1, ref object value, ref object comparand, ref object result);

		public static object CompareExchange (ref object location1, object value, object comparand)
		{
			// This avoids coop handles, esp. on the output which would be particularly inefficient.
			// Passing everything by ref is equivalent to coop handles -- ref to locals at least.
			//
			// location1's treatment is unclear. But note that passing it by handle would be incorrect,
			// as it would use a local alias, which the coop marshaling does, to avoid the unclarity here,
			// that of a ref being to a managed frame vs. a native frame. Perhaps that could be revisited.
			//
			// So there a hole here, that of calling this function with location1 being in a native frame.
			// Usually it will be to a field, static or not, and not even to managed stack.
			//
			// This is usually intrinsified. Ideally it is always intrinisified.
			//
			object result = null;
			CompareExchange (ref location1, ref value, ref comparand, ref result);
			return result;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static float CompareExchange (ref float location1, float value, float comparand);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int Decrement (ref int location);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long Decrement (ref long location);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int Increment (ref int location);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long Increment (ref long location);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int Exchange (ref int location1, int value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static void Exchange (ref object location1, ref object value, ref object result);

		public static object Exchange (ref object location1, object value)
		{
			// See CompareExchange(object) for comments.
			object result = null;
			Exchange (ref location1, ref value, ref result);
			return result;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static float Exchange (ref float location1, float value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long CompareExchange (ref long location1, long value, long comparand);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static IntPtr CompareExchange (ref IntPtr location1, IntPtr value, IntPtr comparand);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static double CompareExchange (ref double location1, double value, double comparand);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		[return: NotNullIfNotNull("location1")]
		extern static void CompareExchange_T<T> (ref T location1, ref T value, ref T comparand, ref T result) where T : class?;

		[return: NotNullIfNotNull("location1")]
		public static T CompareExchange<T> (ref T location1, T value, T comparand) where T : class?
		{
			// Besides avoiding coop handles for efficiency,
			// and correctness, this also appears needed to
			// avoid an assertion failure in the runtime, related to
			// coop handles over generics.
			//
			// See CompareExchange(object) for comments.
			//
			// This is not entirely convincing due to lack of volatile.
			//
#pragma warning disable 8654 // null problems; is there another way?
			T result = null;
#pragma warning restore 8654
			CompareExchange_T (ref location1, ref value, ref comparand, ref result);
			return result;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long Exchange (ref long location1, long value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static IntPtr Exchange (ref IntPtr location1, IntPtr value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static double Exchange (ref double location1, double value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		[return: NotNullIfNotNull("location1")]
		extern static void Exchange_T<T> (ref T location1, ref T value, ref T result) where T : class?;

		[return: NotNullIfNotNull("location1")]
		public static T Exchange<T> (ref T location1, T value) where T : class?
		{
			// See CompareExchange(T) for comments.
			//
			// This is not entirely convincing due to lack of volatile.
			//
#pragma warning disable 8654 // null problems; is there another way?
			T result = null;
#pragma warning restore 8654
			Exchange_T (ref location1, ref value, ref result);
			return result;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long Read (ref long location);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int Add (ref int location1, int value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long Add (ref long location1, long value);

		public static void MemoryBarrier () => Thread.MemoryBarrier ();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void MemoryBarrierProcessWide ();
	}
}
