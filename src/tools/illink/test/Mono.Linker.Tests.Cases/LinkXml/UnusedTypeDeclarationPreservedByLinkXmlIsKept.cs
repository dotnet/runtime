using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UnusedTypeDeclarationPreservedByLinkXmlIsKept.xml")]
	public class UnusedTypeDeclarationPreservedByLinkXmlIsKept
	{
		public static void Main ()
		{
		}
	}

	[Kept]
	[KeptBaseType (typeof (UnusedTypeDeclarationPreservedByLinkXmlIsKeptUnusedTypeBase))]
	class UnusedTypeDeclarationPreservedByLinkXmlIsKeptUnusedType : UnusedTypeDeclarationPreservedByLinkXmlIsKeptUnusedTypeBase
	{
		int field;
		static void Method ()
		{
		}

		string Prop { get; set; }
	}

	[Kept]
	class UnusedTypeDeclarationPreservedByLinkXmlIsKeptUnusedTypeBase
	{
	}
}
