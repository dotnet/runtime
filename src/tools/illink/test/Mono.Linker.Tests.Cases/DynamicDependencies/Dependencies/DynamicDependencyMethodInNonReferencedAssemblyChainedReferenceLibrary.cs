namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
{
	public class DynamicDependencyMethodInNonReferencedAssemblyChainedReferenceLibrary : DynamicDependencyMethodInNonReferencedAssemblyBase
	{
		public override string Method ()
		{
			DynamicDependencyMethodInNonReferencedAssemblyChainedLibrary.Dependency ();
			return "Dependency";
		}
	}
}