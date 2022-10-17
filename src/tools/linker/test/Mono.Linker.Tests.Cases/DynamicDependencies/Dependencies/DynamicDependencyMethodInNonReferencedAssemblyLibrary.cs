namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
{
	public class DynamicDependencyMethodInNonReferencedAssemblyLibrary : DynamicDependencyMethodInNonReferencedAssemblyBase
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