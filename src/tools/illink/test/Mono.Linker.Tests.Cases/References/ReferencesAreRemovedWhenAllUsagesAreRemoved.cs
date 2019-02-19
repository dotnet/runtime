using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.References {
	[SetupLinkerCoreAction ("link")]
	// Il8n & the blacklist step pollute the results with extra stuff that didn't need to be
	// preserved for this test case so we need to disable them
	[Il8n ("none")]
	[Reference ("System.dll")]
	[RemovedAssembly ("System.dll")]
	[KeptReference ("mscorlib.dll")]
	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify(SkipPeVerifyForToolchian.Pedump)]
	// System.Core.dll is referenced by System.dll in the .NET FW class libraries. Our GetType reflection marking code
	// detects a GetType("SHA256CryptoServiceProvider") in System.dll, which then causes a type in System.Core.dll to be marked.
	// PeVerify fails on the original GAC copy of System.Core.dll so it's expected that it will also fail on the stripped version we output
	[SkipPeVerify ("System.Core.dll")]
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