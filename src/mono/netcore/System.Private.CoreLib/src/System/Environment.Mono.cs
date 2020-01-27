// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
	partial class Environment
	{
		public static int CurrentManagedThreadId => Thread.CurrentThread.ManagedThreadId;

		public extern static int ExitCode {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			set;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern int GetProcessorCount ();

		public static string StackTrace {
			[MethodImpl (MethodImplOptions.NoInlining)] // Prevent inlining from affecting where the stacktrace starts
			get => new StackTrace (true).ToString (System.Diagnostics.StackTrace.TraceFormat.Normal);
		}

		public extern static int TickCount {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}

		public extern static long TickCount64 {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static void Exit (int exitCode);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static string[] GetCommandLineArgs ();

		public static void FailFast (string message)
		{
			FailFast (message, null, null);
		}

		public static void FailFast(string message, Exception exception)
		{
			FailFast (message, exception, null);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static void FailFast (string message, Exception exception, string errorSource);
	}

#region referencesource dependencies - to be removed

	partial class Environment
	{
		internal static string GetResourceString (string key)
		{
			return key;
		}

		internal static string GetResourceString (string key, CultureInfo culture)
		{
			return key;
		}

		internal static string GetResourceString (string key, params object[] values)
		{
			return string.Format (CultureInfo.InvariantCulture, key, values);
		}
	}
#endregion
}
