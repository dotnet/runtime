namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies
{
	public class TypeWithBaseInCopiedAssembly4_Base
	{
		public abstract class Base
		{
			public abstract void Method ();
		}

		public class Base2 : Base
		{
			public override void Method ()
			{
			}
		}
	}
}