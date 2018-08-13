namespace Mono.Linker.Tests.Cases.Advanced.Dependencies {
	public class PreserveDependencyMethodInNonReferencedAssemblyChainedReferenceLibrary : PreserveDependencyMethodInNonReferencedAssemblyBase {
		public override string Method ()
		{
			PreserveDependencyMethodInNonReferencedAssemblyChainedLibrary.Dependency ();
			return "Dependency";
		} 
	}
}