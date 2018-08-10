namespace Mono.Linker.Tests.Cases.Advanced.Dependencies {
	public class PreserveDependencyMethodInNonReferencedAssemblyLibrary : PreserveDependencyMethodInNonReferencedAssemblyBase {
		public override string Method ()
		{
			return "Dependency";
		}
	}
}