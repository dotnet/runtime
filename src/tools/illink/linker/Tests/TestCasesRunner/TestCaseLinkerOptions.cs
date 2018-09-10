using System;
using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestCaseLinkerOptions
	{
		public string CoreAssembliesAction;
		public string UserAssembliesAction;
		public List<KeyValuePair<string, string>> AssembliesAction = new List<KeyValuePair<string, string>> ();

		public string Il8n;
		public bool IncludeBlacklistStep;
		public string KeepTypeForwarderOnlyAssemblies;
		public string KeepDebugMembers;
		public string LinkSymbols;
		public bool SkipUnresolved;
		public bool StripResources;

		public List<KeyValuePair<string, string[]>> AdditionalArguments = new List<KeyValuePair<string, string[]>> ();
	}
}