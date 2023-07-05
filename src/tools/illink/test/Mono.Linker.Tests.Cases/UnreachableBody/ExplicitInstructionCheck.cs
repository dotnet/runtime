using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]
	public class ExplicitInstructionCheck
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
			[ExpectedInstructionSequence (new[]
			{
				"ldstr 'Linked away'",
				"newobj System.Void System.NotSupportedException::.ctor(System.String)",
				"throw"
			})]
			public void Method ()
			{
				UsedByMethod ();
			}

			void UsedByMethod ()
			{
			}
		}
	}
}