using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UnusedFieldPreservedByLinkXmlIsKept.xml")]
	class UnusedFieldPreservedByLinkXmlIsKept
	{
		public static void Main ()
		{
		}

		[Kept]
		class Unused
		{
			[Kept]
			private int _preserved;

			[Kept]
			private int _preserved2;

			[Kept]
			private List<int> _preserved3;

			private int _notPreserved;
		}

		[Kept]
		class UnusedWithGenerics<T>
		{
			[Kept]
			private List<T> _preserved1;
		}
	}
}