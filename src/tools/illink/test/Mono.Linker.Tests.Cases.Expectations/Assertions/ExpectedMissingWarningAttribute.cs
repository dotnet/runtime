// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using BaseExpectedWarningAttribute = Mono.Linker.Tests.Cases.Expectations.Assertions.ExpectedWarningAttribute;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (
		AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Event,
		AllowMultiple = true,
		Inherited = false)]
	/// <summary>
	/// An attribute applied to a member to indicate that a warning is expected in ideal behavior, but is not raised in all tools
	/// </summary>
	public class ExpectedMissingWarningAttribute : BaseExpectedWarningAttribute
	{
		public ExpectedMissingWarningAttribute (string warningCode, params string[] messageContains) : base(warningCode, messageContains)
		{
		}

		/// <summary>
		/// Property used by the result checkers of trimmer and analyzers to determine whether
		/// the tool should have produced the specified warning on the annotated member.
		/// </summary>
		public required Tool ProducedBy { get; set; }
		public /* required */ string IssueLinkOrReason { get; set; }
	}
}
