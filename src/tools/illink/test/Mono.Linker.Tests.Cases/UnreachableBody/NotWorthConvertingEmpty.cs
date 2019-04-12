using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.UnreachableBody {
	/// <summary>
	/// Stubbing an empty method would result in more instructions.  It's more size efficient to just leave it alone
	/// </summary>
	public class NotWorthConvertingEmpty {
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
			public void Method ()
			{
			}
		}
	}
}