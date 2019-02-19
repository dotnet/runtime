using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	[KeptMember (".ctor()")]
	public class AssemblyWithPreserveAll {
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class UnusedType {
			[Kept]
			public int UnusedField;

			[Kept]
			[KeptBackingField]
			public int UnusedProperty { [Kept] get; [Kept] set; }
			
			[Kept]
			public void UnusedMethod ()
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			class UnusedNestedType {
			}
		}
	}
}