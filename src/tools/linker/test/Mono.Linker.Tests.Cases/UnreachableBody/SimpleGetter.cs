using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.UnreachableBody {
	public class SimpleGetter {
		public static void Main()
		{
			UsedToMarkMethod (null);
		}

		[Kept]
		static void UsedToMarkMethod (Foo f)
		{
			var tmp = f.Property;
		}

		[Kept]
		class Foo {
			[Kept]
			public string Property { [Kept] [ExpectBodyModified] get; set; }
		}
	}
}