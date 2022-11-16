using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor
{
	public class ObjectHardCastedToInterface
	{
		public static void Main ()
		{
			object o = GetAnObject ();
			IFoo i = (IFoo) o;
			UseAnIFoo (i);

			// Here to mark Foo so that we can verify the interface is removed
			Foo.Helper ();
		}

		[Kept]
		static object GetAnObject ()
		{
			return null;
		}

		[Kept]
		static void UseAnIFoo (IFoo arg)
		{
		}

		[Kept]
		class Foo : IFoo
		{
			[Kept]
			public static void Helper ()
			{
			}
		}

		[Kept]
		interface IFoo
		{
		}
	}
}