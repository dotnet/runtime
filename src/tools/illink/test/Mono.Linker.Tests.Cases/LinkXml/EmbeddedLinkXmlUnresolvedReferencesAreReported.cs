using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("EmbeddedLinkXmlUnresolvedReferencesAreReported.xml")]
	[LogContains ("warning IL2008: Could not resolve type 'UnknownType'")]
	[LogContains ("warning IL2012: Could not find field 'System.String FieldWithSignature' in type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2012: Could not find field 'UnknownField' in type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2009: Could not find method 'System.Void MethodWithSignature()' in type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2009: Could not find method 'UnknownMethod' in type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2016: Could not find event 'System.ResolveEventHandler EventWithSignature' in type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2016: Could not find event 'UnknownEvent' in type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2017: Could not find property 'System.String PropertyWithSignature' in type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2017: Could not find property 'UnknownProperty' in type 'Mono.Linker.Tests.Cases.LinkXml.EmbeddedLinkXmlUnresolvedReferencesAreReported.TestType'")]
	[LogContains ("warning IL2044: Could not find any type in namespace 'UnknownNamespace'")]
	class EmbeddedLinkXmlUnresolvedReferencesAreReported
	{
		public static void Main ()
		{
		}

		[Kept]
		class TestType
		{
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
