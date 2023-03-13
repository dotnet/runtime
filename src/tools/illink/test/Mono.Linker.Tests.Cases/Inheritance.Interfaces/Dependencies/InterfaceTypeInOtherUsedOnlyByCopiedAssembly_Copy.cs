namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies
{
	public class InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Copy
	{
		public static void ToKeepReferenceAtCompileTime ()
		{
		}

		public class A : InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Link.IFoo
		{
			public void Method ()
			{
			}
		}
	}
}