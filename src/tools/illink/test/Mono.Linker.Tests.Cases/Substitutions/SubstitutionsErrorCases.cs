using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("SubstitutionsErrorCases.xml")]

	[ExpectedWarning ("IL2010", "TestMethod_1", "stub", FileName = "SubstitutionsErrorCases.xml")]
	[ExpectedWarning ("IL2011", "TestMethod_2", "noaction", FileName = "SubstitutionsErrorCases.xml")]
	[ExpectedWarning ("IL2013", "SubstitutionsErrorCases::InstanceField", FileName = "SubstitutionsErrorCases.xml")]
	[ExpectedWarning ("IL2014", "SubstitutionsErrorCases::IntField", FileName = "SubstitutionsErrorCases.xml")]
	[ExpectedWarning ("IL2015", "SubstitutionsErrorCases::IntField", "NonNumber", FileName = "SubstitutionsErrorCases.xml")]
	[ExpectedWarning ("IL2007", "NonExistentAssembly", FileName = "SubstitutionsErrorCases.xml")]

	[KeptMember (".ctor()")]
	class SubstitutionsErrorCases
	{
		public static void Main ()
		{
			TestMethod_1 ();
			TestMethod_2 ();

			var instance = new SubstitutionsErrorCases ();
			instance.InstanceField = 42;
			IntField = 42;
		}

		[Kept]
		public static int TestMethod_1 () { return 42; }

		[Kept]
		public static int TestMethod_2 () { return 42; }

		[Kept]
		public int InstanceField;

		[Kept]
		public static int IntField;
	}
}
