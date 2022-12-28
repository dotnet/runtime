using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("StubBodyUnsafe.xml")]
	[SetupCompileArgument ("/unsafe")]
	[SkipPeVerify]
	public class StubBodyUnsafe
	{
		public static unsafe void Main ()
		{
			TestMethod_1 ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"ldc.i4.0",
				"conv.i",
				"ret",
			})]
		static unsafe byte* TestMethod_1 ()
		{
			throw new NotImplementedException ();
		}
	}
}