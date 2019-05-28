using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.CommandLine.Mvid {
	public class DefaultMvidBehavior {
		public static void Main ()
		{
			Method ();
		}

		[Kept]
		static void Method ()
		{
		}
	}
}