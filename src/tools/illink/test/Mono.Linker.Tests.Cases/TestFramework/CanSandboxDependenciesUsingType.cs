using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TestFramework.Dependencies;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	[SandboxDependency (typeof (CanSandboxDependenciesUsingType_Source1))]
	[SandboxDependency (typeof (CanSandboxDependenciesUsingType_Source2.Nested1.Nested2), "NameChanged.cs")]
	public class CanSandboxDependenciesUsingType
	{
		public static void Main ()
		{
			CanSandboxDependenciesUsingType_Source1.Method ();
			CanSandboxDependenciesUsingType_Source2.Nested1.Nested2.Method ();
		}
	}
}