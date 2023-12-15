using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
	[SetupCompileBefore ("CustomStepAfterMark.dll", new[] { "Dependencies/PreserveMethodsSubStep.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "+MarkStep:PreserveMethodsSubStep,CustomStepAfterMark.dll")]
	public class CustomStepCanPreserveMethodsAfterMark
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

		// Annotations.Mark in a CustomStep before MarkStep will not necessarily keep a method,
		// if it belongs to an unmarked type.
		static class UnusedType
		{
			public static void MarkedMethod () { }
		}

	}
}