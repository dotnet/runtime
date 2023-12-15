// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class LinkAttributesTests : LinkerTestBase
	{
		protected override string TestSuiteName => "LinkAttributes";

		[Fact (Skip = "XML Analyzer not implemented")]
		public Task EmbeddedLinkAttributes ()
		{
			return RunTest (nameof (EmbeddedLinkAttributes));
		}

		[Fact (Skip = "XML Analyzer not implemented")]
		public Task LinkerAttributeRemoval ()
		{
			return RunTest (nameof (LinkerAttributeRemoval));
		}

		[Fact (Skip = "XML Analyzer not implemented")]
		public Task TypedArguments ()
		{
			return RunTest (nameof (TypedArguments));
		}

		[Fact (Skip = "XML Analyzer not implemented")]
		public Task LinkerAttributeRemovalConditional ()
		{
			return RunTest (nameof (LinkerAttributeRemovalConditional));
		}
	}
}