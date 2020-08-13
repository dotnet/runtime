using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("LinkXmlErrorCases.xml")]
	[SetupLinkerArgument ("--skip-unresolved", "true")]

	[ExpectedWarning ("IL2007", "NonExistentAssembly", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2008", "NonExistentType", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2009", "NonExistentMethod", "TypeWithNoMethods", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2012", "NonExistentField", "TypeWithNoFields", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2016", "NonExistentEvent", "TypeWithNoEvents", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2017", "NonExistentProperty", "TypeWithNoProperties", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2018", "SetOnlyProperty", "TypeWithProperties", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2019", "GetOnlyProperty", "TypeWithProperties", FileName = "LinkXmlErrorCases.xml")]

	[ExpectedWarning ("IL2025", "Method", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2025", "Event", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2025", "Field", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2025", "Property", FileName = "LinkXmlErrorCases.xml")]
	class LinkXmlErrorCases
	{
		public static void Main ()
		{
		}

		[Kept]
		[ExpectedWarning ("IL2001", "TypeWithNoFields")]
		class TypeWithNoFields
		{
			private void Method () { }
		}

		[Kept]
		[ExpectedWarning ("IL2002", "TypeWithNoMethods")]
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