namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies
{
	public class TypeWithBaseInCopiedAssembly2_Base
	{
		public abstract class Base : IBase
		{
			public abstract void Method ();
		}

		public interface IBase
		{
			void Method ();
		}
	}
}