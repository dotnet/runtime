using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[CoreLink ("copy")]
	[KeptAssembly ("mscorlib.dll")]
	// These types are normally removed when the core libraries are linked
	[KeptTypeInAssembly ("mscorlib.dll", typeof (ConsoleKeyInfo))]
	[KeptTypeInAssembly ("mscorlib.dll", typeof (System.Collections.ObjectModel.KeyedCollection<,>))]

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	class CopyOfCoreLibrariesKeepsUnusedTypes
	{
		public static void Main()
		{
		}
	}
}
