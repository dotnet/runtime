using System;

using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedEventPreservedByLinkXmlIsKept {
		public static void Main ()
		{
		}

		[Kept]
		class Unused {
			[Kept]
			[KeptBackingField]
			public event EventHandler<EventArgs> Preserved;

			[Kept]
			public event EventHandler<EventArgs> Preserved1 { [Kept] add { } [Kept] remove { } }

			public event EventHandler<EventArgs> NotPreserved;
		}
	}
}