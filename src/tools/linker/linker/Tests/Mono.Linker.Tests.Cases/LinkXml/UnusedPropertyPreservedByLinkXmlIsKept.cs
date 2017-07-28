using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedPropertyPreservedByLinkXmlIsKept {
		public static void Main ()
		{
		}

		[Kept]
		class Unused {
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

			public int NotPreservedProperty { get; set; }
		}
	}
}