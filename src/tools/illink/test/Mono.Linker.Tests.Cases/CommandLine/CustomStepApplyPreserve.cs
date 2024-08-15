using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine
{
	[SetupCompileBefore ("CustomApplyPreserveStep.dll", new[] { "Dependencies/CustomApplyPreserveStep.cs" }, new[] { "illink.dll", "Mono.Cecil.dll" }, addAsReference: false)]
	[SetupLinkerArgument ("--custom-step", "-MarkStep:CustomStep.CustomApplyPreserveStep,CustomApplyPreserveStep.dll")]
	[SetupLinkerArgument ("--verbose")]
	public class CustomStepApplyPreserve
	{
		[Kept]
		public class HasPreserveApplied
		{
			[Kept]
			public int Field;

			[Kept]
			public HasPreserveApplied () { }

			[Kept]
			public void Method ()
			{
			}

			public class Nested { }
		}

		[Kept]
		public static void Main ()
		{
			Console.WriteLine (typeof (HasPreserveApplied).FullName);
		}
	}
}
