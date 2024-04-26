// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (
		AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Event,
		AllowMultiple = true,
		Inherited = false)]
	/// <summary>
	/// An attribute applied to a member to indicate that a warning is expected in ideal behavior, and is present in all tools
	/// </summary>
	public class MissingWarningAttribute : EnableLoggerAttribute
	{
		public MissingWarningAttribute (string warningCode, string[] messageContains, string issueLinkOrReason)
		{
		}

		public MissingWarningAttribute (string warningCode, string messageContains, string issueLinkOrReason)
		{
		}

		public MissingWarningAttribute (string warningCode, string messageContains, string messageContains2, string issueLinkOrReason)
		{
		}

		public MissingWarningAttribute (string warningCode, string issueLinkOrReason)
		{
		}

		public string FileName { get; set; }
		public int SourceLine { get; set; }
		public int SourceColumn { get; set; }

		public bool CompilerGeneratedCode { get; set; }
	}
}
