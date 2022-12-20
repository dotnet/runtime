// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// Asserts that the given documentation signature string resolves to the
	// member with this attribute.
	public class ExpectResolvedDocumentationSignatureAttribute : BaseMemberAssertionAttribute
	{
		public ExpectResolvedDocumentationSignatureAttribute (string input)
		{
		}
	}
}
