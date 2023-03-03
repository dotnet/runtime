namespace Mono.Linker.Tests.Cases.Reflection.Dependencies
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
