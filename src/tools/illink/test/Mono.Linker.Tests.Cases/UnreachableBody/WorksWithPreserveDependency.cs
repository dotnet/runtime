using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "../PreserveDependencies/Dependencies/PreserveDependencyAttribute.cs" })]
	public class WorksWithPreserveDependency
	{
		public static void Main ()
		{
			Foo.StaticMethod ();
		}

		[Kept]
		class Foo
		{
			[Kept]
			[PreserveDependency ("InstanceMethod()")]
			public static void StaticMethod ()
			{
			}

			[Kept]
			[ExpectBodyModified]
			public void InstanceMethod ()
			{
				UsedByMethod ();
			}

			void UsedByMethod ()
			{
			}
		}
	}
}