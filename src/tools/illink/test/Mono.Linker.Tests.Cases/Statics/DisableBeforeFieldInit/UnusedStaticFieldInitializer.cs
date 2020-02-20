using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Statics.DisableBeforeFieldInit
{
	[SetupLinkerArgument ("--disable-opt", "BeforeFieldInit", "test")]
	public class UnusedStaticFieldInitializer
	{
		public static void Main ()
		{
			C.Foo ();
		}

		[KeptMember (".cctor()")]
		static class C
		{
			[Kept]
			public static object o = new object ();

			[Kept]
			public static void Foo ()
			{
			}
		}
	}
}