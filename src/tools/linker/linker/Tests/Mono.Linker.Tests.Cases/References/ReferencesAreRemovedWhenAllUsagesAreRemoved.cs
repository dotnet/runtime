using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.References {
	[CoreLink ("link")]
	[Reference ("System.dll")]
	[RemovedAssembly ("System.dll")]
	class ReferencesAreRemovedWhenAllUsagesAreRemoved {
		public static void Main ()
		{
		}

		private static void Unused ()
		{
			// Use something from System.dll so that we know the input assembly was compiled with the reference
			var uri = new Uri ("w/e");
		}
	}
}