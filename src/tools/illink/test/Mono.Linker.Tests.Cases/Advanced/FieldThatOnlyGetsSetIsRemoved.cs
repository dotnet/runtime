using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Advanced
{
	[IgnoreTestCase ("We cannot do this yet")]
	class FieldThatOnlyGetsSetIsRemoved
	{
		public static void Main ()
		{
			new B ().Method ();
		}

		[KeptMember (".ctor()")]
		class B
		{
			public int _unused = 3;

			[Kept]
			public void Method ()
			{
			}
		}
	}
}