using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.UnreachableBody {
	public class WorksWithLinkXml {
		public static void Main()
		{
		}

		[Kept]
		class Foo {
			[Kept]
			[ExpectBodyModified]
			public void InstanceMethod ()
			{
				UsedByMethod ();
			}

			void UsedByMethod ()
			{
			}
		}
	}
}