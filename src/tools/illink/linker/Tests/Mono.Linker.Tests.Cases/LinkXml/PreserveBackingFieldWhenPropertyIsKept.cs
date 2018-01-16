using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	abstract class PreserveBackingFieldWhenPropertyIsKept
	{
		public static void Main ()
		{
			Prop = 1;
		}

		public abstract int Base { set; }

		[Kept]
		[KeptBackingField]
		public static int Prop { get; [Kept] set; }		
	}
}