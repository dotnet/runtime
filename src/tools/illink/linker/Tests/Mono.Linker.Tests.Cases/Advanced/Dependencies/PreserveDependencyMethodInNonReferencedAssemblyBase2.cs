namespace Mono.Linker.Tests.Cases.Advanced.Dependencies {
	public class PreserveDependencyMethodInNonReferencedAssemblyBase2 : PreserveDependencyMethodInNonReferencedAssemblyBase {
		public override string Method ()
		{
			return "Base2";
		}
	}
}