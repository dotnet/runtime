namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
{
	public class AssemblyDependency
	{
		public AssemblyDependency ()
		{
		}

		public static void UsedToKeepReferenceAtCompileTime ()
		{
		}

		class TypeThatIsUsedViaReflection
		{
		}
	}
}
