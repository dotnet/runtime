namespace Mono.Linker.Tests.Cases.Reflection.Dependencies
{
	public class AssemblyImportedViaReflectionWithDerivedType_Reflect : AssemblyImportedViaReflectionWithDerivedType_Base
	{
		public override string Method ()
		{
			return "Reflect";
		}
	}
}