namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies
{
	public class InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Copy
	{
		public static void ToKeepReferenceAtCompileTime ()
		{
		}

		public class A : InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Link.IFoo
		{
			void InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Link.IFoo.Method ()
			{
			}
		}
	}
}