// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (
		AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Event,
		AllowMultiple = true,
		Inherited = false)]
	/// <summary>
	/// An attribute applied to a member to indicate that a warning is raised in tests, but should not be present in ideal behavior
	/// </summary>
	public class UnexpectedWarningAttribute : ExpectedWarningAttribute
	{
		public UnexpectedWarningAttribute (string warningCode, string[] messageContains, Tool producedBy, string issueLinkOrReason)
			: base (warningCode, messageContains, producedBy, issueLinkOrReason) { }
		public UnexpectedWarningAttribute (string warningCode, string messageContains, Tool producedBy, string issueLinkOrReason)
			: base (warningCode, messageContains, producedBy, issueLinkOrReason) { }
		public UnexpectedWarningAttribute (string warningCode, string messageContains, string messageContains2, Tool producedBy, string issueLinkOrReason)
			: base (warningCode, messageContains, messageContains2, producedBy, issueLinkOrReason) { }
		public UnexpectedWarningAttribute (string warningCode, Tool producedBy, string issueLinkOrReason)
			: base (warningCode, producedBy, issueLinkOrReason) { }
	}
}
