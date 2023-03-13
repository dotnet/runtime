namespace Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies
{
	public class PreserveDependencyMethodInNonReferencedAssemblyLibrary : PreserveDependencyMethodInNonReferencedAssemblyBase
	{
		public override string Method ()
		{
			return "Dependency";
		}

		private void UnusedMethod ()
		{
		}
	}
}