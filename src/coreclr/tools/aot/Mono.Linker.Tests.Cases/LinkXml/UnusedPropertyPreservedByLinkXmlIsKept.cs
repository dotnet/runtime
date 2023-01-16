using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UnusedPropertyPreservedByLinkXmlIsKept.xml")]
	class UnusedPropertyPreservedByLinkXmlIsKept
	{
		public static void Main ()
		{
			new Unused (); // Used to avoid lazy body marking
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Unused
		{
			[Kept]
			[KeptBackingField]
			public int PreservedProperty1 { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			public int PreservedProperty2 { [Kept] get; set; }

			[Kept]
			[KeptBackingField]
			public int PreservedProperty3 { get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			public int PreservedProperty4 { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			public int PreservedProperty5 { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			public int PreservedProperty6 { [Kept] get; set; }

			[Kept]
			[KeptBackingField]
			public int PreservedProperty7 { get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			public int PreservedProperty8 { [Kept] get; [Kept] set; }

			public int NotPreservedProperty { get; set; }
		}
	}
}