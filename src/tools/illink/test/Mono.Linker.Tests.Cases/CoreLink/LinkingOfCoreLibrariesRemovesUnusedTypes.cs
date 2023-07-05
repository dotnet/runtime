using System;
using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "Not important for .NET Core build")]
	[SetupLinkerTrimMode ("link")]
	[Reference ("System.dll")]

	[KeptAssembly (PlatformAssemblies.CoreLib)]
	// We can't check everything that should be removed, but we should be able to check a few niche things that
	// we know should be removed which will at least verify that the core library was processed
	[KeptTypeInAssembly (PlatformAssemblies.CoreLib, typeof (System.Collections.Generic.IEnumerable<>))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (System.Resources.ResourceWriter))]
	[KeptAssembly ("System.dll")]
	[KeptTypeInAssembly ("System.dll", typeof (System.Collections.Generic.SortedList<,>))]
	[RemovedTypeInAssembly ("System.dll", typeof (System.Collections.Generic.SortedDictionary<,>))]

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]

	//  All sorts of stuff is flagged as invalid even in the original System.dll and System.Configuration.dll for mono class libraries
	[SkipPeVerify ("System.dll")]
	[SkipPeVerify ("System.Configuration.dll")]
	// System.Core.dll is referenced by System.dll in the .NET FW class libraries. Our GetType reflection marking code
	// detects a GetType("SHA256CryptoServiceProvider") in System.dll, which then causes a type in System.Core.dll to be marked.
	// PeVerify fails on the original GAC copy of System.Core.dll so it's expected that it will also fail on the stripped version we output
	[SkipPeVerify ("System.Core.dll")]
	class LinkingOfCoreLibrariesRemovesUnusedTypes
	{
		public static void Main ()
		{
			// Use something from system that would normally be removed if we didn't use it
			OtherMethods2 (new SortedList<string, string> ());
		}

		[Kept]
		static void OtherMethods2 (SortedList<string, string> list)
		{
		}
	}
}
