using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UnusedBodyGetsRemoved
	{
		public static void Main ()
		{
			UnusedBodyType unusedBody = null;
			if (unusedBody != null) {
				unusedBody.UnusedBody();
				unusedBody.UnusedBody2();
			}
		}

		class UnusedBodyType
		{
			[Kept]
			[ExpectedInstructionSequence(new[] {
				"ldnull",
				"throw"
			})]
			public void UnusedBody () => DoSomethingExpensive ();

			[Kept]
			[ExpectedInstructionSequence(new[] {
				"ldnull",
				"throw"
			})]
			public void UnusedBody2 () => DoSomethingExpensive ();

			static void DoSomethingExpensive () { }
		}
	}
}
