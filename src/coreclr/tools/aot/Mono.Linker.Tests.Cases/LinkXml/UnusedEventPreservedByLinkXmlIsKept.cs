using System;

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UnusedEventPreservedByLinkXmlIsKept.xml")]
	class UnusedEventPreservedByLinkXmlIsKept
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
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> Preserved;

			[Kept]
			public event EventHandler<EventArgs> Preserved1 { [Kept] add { } [Kept] remove { } }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler Preserved2;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler Preserved3;

			public event EventHandler<EventArgs> NotPreserved;
		}
	}
}