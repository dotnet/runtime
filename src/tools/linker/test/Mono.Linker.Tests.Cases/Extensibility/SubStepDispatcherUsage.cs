using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
#if !NETCOREAPP
	[IgnoreTestCase ("Can be enabled once MonoBuild produces a dll from which we can grab the types in the Mono.Linker namespace.")]
#else
	[SetupCompileBefore ("MyDispatcher.dll", new [] { "Dependencies/MyDispatcher.cs", "Dependencies/CustomSubStep.cs" }, new [] { "illink.dll" })]
#endif
	[SetupLinkerArgument ("--custom-step", "-MarkStep:MyDispatcher,MyDispatcher.dll")]
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
