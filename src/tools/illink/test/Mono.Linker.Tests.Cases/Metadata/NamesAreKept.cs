using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Metadata
{
	[SetupLinkerArgument ("--keep-metadata", "parametername")]
	[VerifyMetadataNames]
	public class NamesAreKept
	{
		public static void Main ()
		{
			var n = new N (5);
			N.Foo ("aa");
		}

		class N
		{
			[Kept]
			public N (int arg)
			{
			}

			[Kept]
			public static void Foo (string str)
			{
			}
		}
	}
}
