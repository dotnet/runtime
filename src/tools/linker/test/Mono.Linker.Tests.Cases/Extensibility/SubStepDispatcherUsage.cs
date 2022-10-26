using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
	[SetupCompileBefore ("MyDispatcherUsage.dll", new[] { "Dependencies/MyDispatcher.cs", "Dependencies/CustomSubStep.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "-MarkStep:MyDispatcher,MyDispatcherUsage.dll")]
	public class SubStepDispatcherUsage
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