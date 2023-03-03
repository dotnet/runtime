using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	public class InterfaceTypeOnlyUsedHasInterfacesRemoved
	{
		public static void Main ()
		{
			var tmp = typeof (IUsed).ToString ();
		}

		interface IRemovable1
		{
		}

		interface IRemovable2
		{
		}

		[Kept]
		interface IUsed : IRemovable1, IRemovable2
		{
		}
	}
}