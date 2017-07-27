using System;
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
	[KeptTypeInAssembly ("System.dll", typeof (Uri))]

	[RemovedTypeInAssembly ("mscorlib.dll", typeof (System.Resources.ResourceWriter))]
	[RemovedTypeInAssembly ("System.dll", typeof (System.CodeDom.Compiler.CodeCompiler))]

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	class LinkingOfCoreLibrariesRemovesUnusedTypes {
		public static void Main ()
		{
			// Use something from system that would normally be removed if we didn't use it
			OtherMethods2 (new Uri ("dont care"));
		}

		[Kept]
		static void OtherMethods2 (Uri uri)
		{
		}
	}
}