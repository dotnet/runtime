using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("PreserveIndividualMembersOfNonRequiredType.xml")]
	class PreserveIndividualMembersOfNonRequiredType
	{
		public static void Main ()
		{
			var t = typeof (Required);
		}

		class Required
		{
			[Kept]
			public Required () { }

			[Kept]
			public int Field1;

			public int Field2;

			[Kept]
			public void Method1 () { }

			public void Method2 () { }

			[Kept]
			[KeptBackingField]
			public int Property1 { [Kept] get; [Kept] set; }

			public int Property2 { get; set; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler Event1;
		}
	}
}
