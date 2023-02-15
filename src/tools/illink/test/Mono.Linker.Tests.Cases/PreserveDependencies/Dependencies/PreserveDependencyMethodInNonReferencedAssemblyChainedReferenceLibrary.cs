namespace Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies
{
	public class PreserveDependencyMethodInNonReferencedAssemblyChainedReferenceLibrary : PreserveDependencyMethodInNonReferencedAssemblyBase
	{
		public override string Method ()
		{
			PreserveDependencyMethodInNonReferencedAssemblyChainedLibrary.Dependency ();
			return "Dependency";
		}
	}
}