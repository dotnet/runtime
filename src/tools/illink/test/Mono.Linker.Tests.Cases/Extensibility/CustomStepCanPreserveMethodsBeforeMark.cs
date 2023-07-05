using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
	[SetupCompileBefore ("CustomStepBeforeMark.dll", new[] { "Dependencies/PreserveMethodsSubStep.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "-MarkStep:PreserveMethodsSubStep,CustomStepBeforeMark.dll")]
	public class CustomStepCanPreserveMethodsBeforeMark
	{
		public static void Main ()
		{
			UsedType.UsedMethod ();
		}

		[Kept]
		static class UsedType
		{
			[Kept]
			public static void UsedMethod () { }

			[Kept]
			public static void PreservedForType () { }

			[Kept]
			public static void PreservedForMethod_UsedMethod () { }
		}

		// Annotations.Mark in a CustomStep before MarkStep will keep a method,
		// even if it belongs to an otherwise unmarked type.
		[Kept]
		static class UnusedType
		{
			[Kept]
			public static void MarkedMethod () { }
		}

	}
}