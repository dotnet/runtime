using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupLinkerArgument ("--disable-opt", "unreachablebodies")]
	public class CanDisableLazyBodyMarking
	{
		public static void Main ()
		{
			UsedToMarkMethod (null);
		}

		[Kept]
		static void UsedToMarkMethod (Foo f)
		{
			f.Method ();
		}

		[Kept]
		class Foo
		{
			[Kept]
			public void Method ()
			{
				UsedByMethod ();
			}

			[Kept]
			void UsedByMethod ()
			{
			}
		}
	}
}