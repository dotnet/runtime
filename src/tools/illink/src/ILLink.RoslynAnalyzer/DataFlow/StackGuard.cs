// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
