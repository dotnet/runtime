using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	[KeptMember (".ctor()")]
	public class UnusedAssemblyWithNoDefinedPreserveHasAllTypesPreserved {
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Unused {
		}
	}
}
