// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Diagnostics
{
	public static class Debugger
	{
		public static readonly string DefaultCategory = "";

		public static bool IsAttached => IsAttached_internal ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static bool IsAttached_internal ();

		public static void Break ()
		{
			// The JIT inserts a breakpoint on the caller.
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public static extern bool IsLogging();

		public static bool Launch ()
		{
			throw new NotImplementedException ();
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void Log_icall (int level, ref string category, ref string message);

		public static void Log (int level, string category, string message)
		{
			Log_icall (level, ref category, ref message);
		}

		public static void NotifyOfCrossThreadDependency ()
		{
		}
	}
}
