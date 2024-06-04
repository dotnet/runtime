// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestCaseLinkerOptions
	{
		public string TrimMode;
		public string DefaultAssembliesAction;
		public List<KeyValuePair<string, string>> AssembliesAction = new List<KeyValuePair<string, string>> ();

		public string Il8n;
		public bool IgnoreDescriptors;
		public bool IgnoreSubstitutions;
		public bool IgnoreLinkAttributes;
		public string LinkSymbols;
		public bool SkipUnresolved;
		public bool StripDescriptors;
		public bool StripSubstitutions;
		public bool StripLinkAttributes;
		public bool DumpDependencies;

		public List<KeyValuePair<string, string[]>> AdditionalArguments = new List<KeyValuePair<string, string[]>> ();

		public List<string> Descriptors = new List<string> ();

		public List<string> Substitutions = new List<string> ();

		public List<string> LinkAttributes = new List<string> ();
	}
}