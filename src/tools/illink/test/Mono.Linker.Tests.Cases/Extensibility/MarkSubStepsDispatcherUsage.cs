using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
	[SetupCompileBefore ("MyMarkSubStepsDispatcher.dll", new[] { "Dependencies/MyMarkSubStepsDispatcher.cs", "Dependencies/CustomSubStep.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "MyMarkSubStepsDispatcher,MyMarkSubStepsDispatcher.dll")]
	public class MarkSubStepsDispatcherUsage
	{
		public static void Main ()
		{
		}

		[Kept]
		public class NestedType
		{
			public int field;

			public static void SomeMethod ()
			{
			}
		}
	}
}