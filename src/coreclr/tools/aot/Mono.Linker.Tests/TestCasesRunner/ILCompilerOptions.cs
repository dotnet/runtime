// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ILCompilerOptions
	{
		public Dictionary<string, string> InputFilePaths = new Dictionary<string, string> ();
		public Dictionary<string, string> ReferenceFilePaths = new Dictionary<string, string> ();
		public List<string> InitAssemblies = new List<string> ();
		public List<string> TrimAssemblies = new List<string> ();
		public Dictionary<string, bool> FeatureSwitches = new Dictionary<string, bool> ();
	}
}
