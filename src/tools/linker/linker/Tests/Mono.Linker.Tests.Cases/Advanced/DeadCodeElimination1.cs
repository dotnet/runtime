using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Advanced {
	[IgnoreTestCase ("We cannot do this yet")]
	class DeadCodeElimination1 {
		public static void Main ()
		{
			object o = null;
			if (o is B)
				((B) o).Method ();
		}

		class B {
			public void Method ()
			{
			}
		}
	}
}