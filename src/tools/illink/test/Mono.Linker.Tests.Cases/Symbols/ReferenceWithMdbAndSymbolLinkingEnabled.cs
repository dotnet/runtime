using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[IgnoreTestCase ("Test relies on checked-in binaries: https://github.com/dotnet/runtime/issues/78344")]
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "mdb files are not supported with .NET Core")]
	[Reference ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll")]
	[ReferenceDependency ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll.mdb")]
	[SetupLinkerLinkSymbols ("true")]

	[KeptSymbols ("LibraryWithMdb.dll")]

	[KeptMemberInAssembly ("LibraryWithMdb.dll", typeof (LibraryWithMdb), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithMdb.dll", typeof (LibraryWithMdb), "NotUsed()")]
	public class ReferenceWithMdbAndSymbolLinkingEnabled
	{
		static void Main ()
		{
			// Use some stuff so that we can verify that the linker output correct results
			SomeMethod ();

			LibraryWithMdb.SomeMethod ();
		}

		[Kept]
		static void SomeMethod ()
		{
		}

		static void NotUsed ()
		{
		}
	}
}