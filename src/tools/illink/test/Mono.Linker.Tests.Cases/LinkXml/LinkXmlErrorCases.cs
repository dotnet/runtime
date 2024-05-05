using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("LinkXmlErrorCases.xml")]
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[SetupLinkerArgument ("--verbose")]

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
    [LogContains ("Duplicate preserve of 'System.Int32 Mono.Linker.Tests.Cases.LinkXml.LinkXmlErrorCases/TypeWithEverything::Field'", ProducedBy = Tool.Trimmer)]
    [LogContains ("Duplicate preserve of 'Mono.Linker.Tests.Cases.LinkXml.LinkXmlErrorCases.TypeWithEverything.TypeWithEverything()'", ProducedBy = Tool.Trimmer)]
    [LogContains ("Duplicate preserve of 'Mono.Linker.Tests.Cases.LinkXml.LinkXmlErrorCases.TypeWithEverything.Method()'", ProducedBy = Tool.Trimmer)]
    [LogContains ("Duplicate preserve of 'System.EventHandler Mono.Linker.Tests.Cases.LinkXml.LinkXmlErrorCases/TypeWithEverything::Event'", ProducedBy = Tool.Trimmer)]
    [LogContains ("Duplicate preserve of 'Mono.Linker.Tests.Cases.LinkXml.LinkXmlErrorCases.TypeWithEverything.Event.add'", ProducedBy = Tool.Trimmer)]
    [LogContains ("Duplicate preserve of 'Mono.Linker.Tests.Cases.LinkXml.LinkXmlErrorCases.TypeWithEverything.Event.remove'", ProducedBy = Tool.Trimmer)]
    [LogContains ("Duplicate preserve of 'System.Int32 Mono.Linker.Tests.Cases.LinkXml.LinkXmlErrorCases/TypeWithEverything::Property()'", ProducedBy = Tool.Trimmer)]
    [LogContains ("Duplicate preserve of 'Mono.Linker.Tests.Cases.LinkXml.LinkXmlErrorCases.TypeWithEverything.Property.get'", ProducedBy = Tool.Trimmer)]
    [LogContains ("Duplicate preserve of 'Mono.Linker.Tests.Cases.LinkXml.LinkXmlErrorCases.TypeWithEverything.Property.set'", ProducedBy = Tool.Trimmer)]
	// NativeAOT doesn't support wildcard * and will skip usages of it, including if they would warn
	// https://github.com/dotnet/runtime/issues/80466
	[ExpectedWarning ("IL2100", Tool.Trimmer, "", FileName = "LinkXmlErrorCases.xml", SourceLine = 50, SourceColumn = 4)]
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
