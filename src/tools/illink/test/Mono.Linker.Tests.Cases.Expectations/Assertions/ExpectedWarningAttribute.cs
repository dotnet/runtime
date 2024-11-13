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
	/// An attribute applied to a member to indicate that a warning is expected in ideal behavior, and is present in all tools
	/// </summary>
	public class ExpectedWarningAttribute : EnableLoggerAttribute
	{
		public ExpectedWarningAttribute (string warningCode, string[] messageContains, Tool producedBy, string issueLinkOrReason)
		{
		}

		public ExpectedWarningAttribute (string warningCode, string messageContains, Tool producedBy, string issueLinkOrReason)
		{
		}

		public ExpectedWarningAttribute (string warningCode, string messageContains, string messageContains2, Tool producedBy, string issueLinkOrReason)
		{
		}

		public ExpectedWarningAttribute (string warningCode, Tool producedBy, string issueLinkOrReason)
		{
		}

		public ExpectedWarningAttribute (string warningCode, params string[] messageContains)
		{
		}

		public string FileName { get; set; }
		public int SourceLine { get; set; }
		public int SourceColumn { get; set; }

		public bool CompilerGeneratedCode { get; set; }
	}
}
