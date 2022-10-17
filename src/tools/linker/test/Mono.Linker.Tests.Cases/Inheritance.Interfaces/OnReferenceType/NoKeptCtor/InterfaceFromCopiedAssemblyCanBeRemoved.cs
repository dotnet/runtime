using System.Collections;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor
{
	public class InterfaceFromCopiedAssemblyCanBeRemoved
	{
		public static void Main ()
		{
			Foo.StaticMethod ();
		}

		[Kept]
		class Foo : IComparer
		{
			public int Compare (object x, object y)
			{
				throw new System.NotImplementedException ();
			}

			[Kept]
			public static void StaticMethod ()
			{
			}
		}
	}
}