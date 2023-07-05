using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Substitutions.Dependencies;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("SubstitutionsErrorCases.xml")]
	[IgnoreSubstitutions (false)]
	[SetupCompileBefore ("library.dll", new string[] { "Dependencies/EmbeddedSubstitutionsErrorCases.cs" },
		resources: new object[] { new string[] { "Dependencies/EmbeddedSubstitutionsErrorCases.xml", "ILLink.Substitutions.xml" } })]

	[ExpectedWarning ("IL2010", "TestMethod_1", "stub", FileName = "SubstitutionsErrorCases.xml", SourceLine = 5, SourceColumn = 8)]
	[ExpectedWarning ("IL2011", "TestMethod_2", "noaction", FileName = "SubstitutionsErrorCases.xml", SourceLine = 6, SourceColumn = 8)]
	[ExpectedWarning ("IL2013", "SubstitutionsErrorCases.InstanceField", FileName = "SubstitutionsErrorCases.xml", SourceLine = 8, SourceColumn = 8)]
	[ExpectedWarning ("IL2014", "SubstitutionsErrorCases.IntField", FileName = "SubstitutionsErrorCases.xml", SourceLine = 9, SourceColumn = 8)]
	[ExpectedWarning ("IL2015", "SubstitutionsErrorCases.IntField", "NonNumber", FileName = "SubstitutionsErrorCases.xml", SourceLine = 10, SourceColumn = 8)]
	[ExpectedWarning ("IL2007", "NonExistentAssembly", FileName = "SubstitutionsErrorCases.xml", SourceLine = 13, SourceColumn = 4)]
	[ExpectedWarning ("IL2100", FileName = "SubstitutionsErrorCases.xml", SourceLine = 15, SourceColumn = 4)]
	[ExpectedWarning ("IL2101", "library", "test", FileName = "ILLink.Substitutions.xml", SourceLine = 3, SourceColumn = 4)]

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

			var _ = new EmbeddedSubstitutionsErrorCases ();
		}

		[Kept]
		public static int TestMethod_1 () { return 42; }

		[Kept]
		public static int TestMethod_2 () { return 42; }

		[Kept]
		public int InstanceField;

		[Kept]
		public static int IntField;

		public class ReferencedFromOtherAssembly
		{
			public static int TestMethod () { return 42; }
		}
	}
}
