// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
	// Copied from https://github.com/dotnet/roslyn
	internal static class StackGuard
	{
		public const int MaxUncheckedRecursionDepth = 20;

		/// <summary>
		///     Ensures that the remaining stack space is large enough to execute
		///     the average function.
		/// </summary>
		/// <param name="recursionDepth">how many times the calling function has recursed</param>
		/// <exception cref="InsufficientExecutionStackException">
		///     The available stack space is insufficient to execute
		///     the average function.
		/// </exception>
		[DebuggerStepThrough]
		public static void EnsureSufficientExecutionStack (int recursionDepth)
		{
			if (recursionDepth > MaxUncheckedRecursionDepth) {
				RuntimeHelpers.EnsureSufficientExecutionStack ();
			}
		}
	}
}