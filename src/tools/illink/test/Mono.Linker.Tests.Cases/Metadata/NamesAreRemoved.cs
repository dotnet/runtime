using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Metadata
{
	public class NamesAreRemoved
	{
		public static void Main ()
		{
			var n = new N (5);
			N.Foo (null, 1);
		}

		class N
		{
			[Kept]
			public N ([RemovedNameValueAttribute] int arg)
			{
			}

			[Kept]
			public static void Foo ([RemovedNameValueAttribute] string str, [RemovedNameValueAttribute] long _)
			{
			}
		}
	}
}
