﻿using System.Collections;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "Not important for .NET Core build")]
	[SetupLinkerTrimMode ("link")]

	[KeptAssembly (PlatformAssemblies.CoreLib)]
	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (Stack), ".ctor(System.Int32)", "Pop()", "Push(System.Object)")]
	// We can't check everything that should be removed, but we should be able to check a few niche things that
	// we known should be removed which will at least verify that the core library was processed
	[RemovedMemberInAssembly (PlatformAssemblies.CoreLib, typeof (Stack), ".ctor(System.Collections.ICollection)")]

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	class LinkingOfCoreLibrariesRemovesUnusedMethods
	{
		public static void Main ()
		{
			Stack stack = new Stack (2);
			stack.Push (1);
			var val = stack.Pop ();
		}
	}
}
