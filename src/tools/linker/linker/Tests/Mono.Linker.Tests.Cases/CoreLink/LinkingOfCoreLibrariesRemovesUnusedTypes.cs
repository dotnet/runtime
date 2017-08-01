using System;
using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink {
	[CoreLink ("link")]
	[Reference("System.dll")]

	[KeptAssembly ("mscorlib.dll")]
	[KeptAssembly("System.dll")]
	// We can't check everything that should be removed, but we should be able to check a few niche things that
	// we known should be removed which will at least verify that the core library was processed
	[KeptTypeInAssembly ("mscorlib.dll", typeof (System.Collections.Generic.IEnumerable<>))]
	[KeptTypeInAssembly ("System.dll", typeof (System.Collections.Generic.SortedList<,>))]

	[RemovedTypeInAssembly ("mscorlib.dll", typeof (System.Resources.ResourceWriter))]
	[RemovedTypeInAssembly ("System.dll", typeof (System.Collections.Generic.SortedDictionary<,>))]

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]

	//  All sorts of stuff is flagged as invalid even in the original System.dll and System.Configuration.dll for mono class libraries
	[SkipPeVerify("System.dll")]
	[SkipPeVerify("System.Configuration.dll")]
	class LinkingOfCoreLibrariesRemovesUnusedTypes {
		public static void Main ()
		{
			// Use something from system that would normally be removed if we didn't use it
			OtherMethods2 (new SortedList<string, string>());
		}

		[Kept]
		static void OtherMethods2 (SortedList<string, string> list)
		{
		}
	}
}