// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Libraries
{
	[IgnoreTestCase ("NativeAOT doesn't implement library trimming the same way", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]
	[SetupCompileAsLibrary]
	[SetupLinkerLinkAll]
	[Kept]
	[KeptMember (".ctor()")]
	public class DefaultLibraryLinkBehavior
	{
		// Kept because by default libraries have all members rooted
		[Kept]
		public static void Main ()
		{
			// Main is needed for the test collector to find and treat as a test
		}

		[Kept]
		public void UnusedPublicMethod ()
		{
		}

		[Kept]
		private void UnusedPrivateMethod ()
		{
		}
	}
}
