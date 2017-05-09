using System;
using System.Collections;
using System.IO;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[CoreLink ("link")]

	[KeptAssembly ("mscorlib.dll")]
	[KeptMemberInAssembly ("mscorlib.dll", typeof (Stack), ".ctor(System.Int32)", "Pop()", "Push(System.Object)")]
	// We can't check everything that should be removed, but we should be able to check a few niche things that
	// we known should be removed which will at least verify that the core library was processed
	[RemovedMemberInAssembly ("mscorlib.dll", typeof (Stack), ".ctor(System.Collections.ICollection)")]
	class LinkingOfCoreLibrariesRemovesUnusedMethods {
		public static void Main()
		{
			Stack stack = new Stack (2);
			stack.Push (1);
			var val = stack.Pop ();
		}
	}
}
