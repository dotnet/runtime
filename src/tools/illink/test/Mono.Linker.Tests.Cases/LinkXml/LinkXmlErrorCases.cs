using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("LinkXmlErrorCases.xml")]
	[SetupLinkerArgument ("--skip-unresolved", "true")]

	[ExpectedWarning ("IL2001", "TypeWithNoFields", FileName = "LinkXmlErrorCases.xml", SourceLine = 3, SourceColumn = 6)]
	[ExpectedWarning ("IL2002", "TypeWithNoMethods", FileName = "LinkXmlErrorCases.xml", SourceLine = 4, SourceColumn = 6)]
	[ExpectedWarning ("IL2007", "NonExistentAssembly", FileName = "LinkXmlErrorCases.xml", SourceLine = 47, SourceColumn = 4)]
	[ExpectedWarning ("IL2008", "NonExistentType", FileName = "LinkXmlErrorCases.xml", SourceLine = 6, SourceColumn = 6)]
	[ExpectedWarning ("IL2009", "NonExistentMethod", "TypeWithNoMethods", FileName = "LinkXmlErrorCases.xml", SourceLine = 9, SourceColumn = 8)]
	[ExpectedWarning ("IL2012", "NonExistentField", "TypeWithNoFields", FileName = "LinkXmlErrorCases.xml", SourceLine = 13, SourceColumn = 8)]
	[ExpectedWarning ("IL2016", "NonExistentEvent", "TypeWithNoEvents", FileName = "LinkXmlErrorCases.xml", SourceLine = 17, SourceColumn = 8)]
	[ExpectedWarning ("IL2017", "NonExistentProperty", "TypeWithNoProperties", FileName = "LinkXmlErrorCases.xml", SourceLine = 21, SourceColumn = 8)]
	[ExpectedWarning ("IL2018", "SetOnlyProperty", "TypeWithProperties", FileName = "LinkXmlErrorCases.xml", SourceLine = 25, SourceColumn = 8)]
	[ExpectedWarning ("IL2019", "GetOnlyProperty", "TypeWithProperties", FileName = "LinkXmlErrorCases.xml", SourceLine = 26, SourceColumn = 8)]

	[ExpectedWarning ("IL2025", "Method", FileName = "LinkXmlErrorCases.xml", SourceLine = 39, SourceColumn = 8)]
	[ExpectedWarning ("IL2025", "Event", FileName = "LinkXmlErrorCases.xml", SourceLine = 40, SourceColumn = 8)]
	[ExpectedWarning ("IL2025", "Field", FileName = "LinkXmlErrorCases.xml", SourceLine = 41, SourceColumn = 8)]
	[ExpectedWarning ("IL2025", "Property", FileName = "LinkXmlErrorCases.xml", SourceLine = 42, SourceColumn = 8)]
	[ExpectedWarning ("IL2100", FileName = "LinkXmlErrorCases.xml", SourceLine = 50, SourceColumn = 4)]
	class LinkXmlErrorCases
	{
		public static void Main ()
		{
		}

		[Kept]
		class TypeWithNoFields
		{
			private void Method () { }
		}

		[Kept]
		struct TypeWithNoMethods
		{
		}

		[Kept]
		struct TypeWithNoEvents
		{
		}

		[Kept]
		struct TypeWithNoProperties
		{
		}

		[Kept]
		class TypeWithProperties
		{
			public bool SetOnlyProperty { set { _ = value; } }
			public bool GetOnlyProperty { get { return false; } }
		}

		[Kept]
		class TypeWithEverything
		{
			[Kept]
			public TypeWithEverything () { }
			[Kept]
			public void Method () { }
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler Event;
			[Kept]
			public int Field;
			[Kept]
			[KeptBackingField]
			public int Property { [Kept] get; [Kept] set; }
		}
	}
}