// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (
		AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Field,
		AllowMultiple = true,
		Inherited = false)]
	public class LogDoesNotContainAttribute : EnableLoggerAttribute
	{
		public LogDoesNotContainAttribute (string message, bool regexMatch = false)
		{
			if (string.IsNullOrEmpty (message))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (message));
		}

		/// <summary>
		/// Property used by the result checkers of trimmer and analyzers to determine whether
		/// the tool should have produced the specified warning on the annotated member.
		/// </summary>
		public ProducedBy ProducedBy { get; set; } = ProducedBy.TrimmerAnalyzerAndNativeAot;
	}
}
