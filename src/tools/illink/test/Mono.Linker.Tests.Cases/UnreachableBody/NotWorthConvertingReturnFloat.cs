using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]
	public class NotWorthConvertingReturnFloat
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
			public float Method ()
			{
				return 1.0f;
			}
		}
	}
}