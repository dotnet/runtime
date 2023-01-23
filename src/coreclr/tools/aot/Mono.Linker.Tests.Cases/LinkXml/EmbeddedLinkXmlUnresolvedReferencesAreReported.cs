using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("EmbeddedLinkXmlUnresolvedReferencesAreReported.xml")]
	[LogContains ("warning IL2008: Could not resolve type 'UnknownType'")]
	[LogContains ("warning IL2012: Could not find field 'System.String FieldWithSignature' on type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2012: Could not find field 'UnknownField' on type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2009: Could not find method 'System.Void MethodWithSignature()' on type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2009: Could not find method 'UnknownMethod' on type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2016: Could not find event 'System.ResolveEventHandler EventWithSignature' on type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2016: Could not find event 'UnknownEvent' on type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2017: Could not find property 'System.String PropertyWithSignature' on type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2017: Could not find property 'UnknownProperty' on type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[ExpectedWarning ("IL2044", "UnknownNamespace", FileName = "EmbeddedLinkXmlUnresolvedReferencesAreReported.xml")]
	class EmbeddedLinkXmlUnresolvedReferencesAreReported
	{
		public static void Main ()
		{
			new TestType ();
		}

		[Kept]
		class TestType
		{
			[Kept]
			public TestType () { }

			[Kept]
			public int FieldWithSignature;

			[Kept]
			public string MethodWithSignature () { return null; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			event EventHandler EventWithSignature;

			[Kept]
			[KeptBackingField]
			public int PropertyWithSignature {
				[Kept]
				get;
				[Kept]
				set;
			}
		}
	}
}
