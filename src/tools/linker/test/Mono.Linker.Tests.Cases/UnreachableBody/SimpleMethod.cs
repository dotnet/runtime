using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.UnreachableBody {
	public class SimpleMethod {
		public static void Main()
		{
			UsedToMarkMethod (null);
		}

		[Kept]
		static void UsedToMarkMethod (Foo f)
		{
			f.Method ();
		}

		[Kept]
		class Foo {
			[Kept]
			[ExpectBodyModified]
			public void Method ()
			{
				UsedByMethod ();
			}

			void UsedByMethod ()
			{
			}
		}
	}
}