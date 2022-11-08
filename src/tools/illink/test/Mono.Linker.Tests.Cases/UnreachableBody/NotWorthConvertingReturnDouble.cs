using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]
	public class NotWorthConvertingReturnDouble
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
			public double Method ()
			{
				return 0.0;
			}
		}
	}
}