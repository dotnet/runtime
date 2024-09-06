// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mono.Profiler.Log
{
	/// <summary>
	/// Internal calls to match with https://github.com/dotnet/runtime/blob/release/6.0/src/mono/mono/profiler/log.c#L4061-L4097
	/// </summary>
	internal class LogProfiler
	{
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void TriggerHeapshot();

		[DllImport("libSystem.Native")]
		public extern static void mono_profiler_flush_log();
	}
}