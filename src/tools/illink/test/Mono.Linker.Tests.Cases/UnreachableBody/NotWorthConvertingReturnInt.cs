using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]
	public class NotWorthConvertingReturnInt
	{
		public static void Main ()
		{
			UsedToMarkMethod (null);
		}

		[Kept]
		static void UsedToMarkMethod (Foo f)
		{
			f.Method ();
			f.Method1 ();
			f.Method2 ();
			f.Method3 ();
			f.Method4 ();
			f.Method5 ();
			f.Method6 ();
			f.Method7 ();
			f.Method8 ();
			f.Method9 ();
		}

		[Kept]
		class Foo
		{
			[Kept]
			public int Method ()
			{
				return 0;
			}

			[Kept]
			public int Method1 ()
			{
				return 1;
			}

			[Kept]
			public int Method2 ()
			{
				return 2;
			}

			[Kept]
			public int Method3 ()
			{
				return 3;
			}

			[Kept]
			public int Method4 ()
			{
				return 4;
			}

			[Kept]
			public int Method5 ()
			{
				return 5;
			}

			[Kept]
			public int Method6 ()
			{
				return 6;
			}

			[Kept]
			public int Method7 ()
			{
				return 7;
			}

			[Kept]
			public int Method8 ()
			{
				return 8;
			}

			[Kept]
			public int Method9 ()
			{
				return 9;
			}
		}
	}
}