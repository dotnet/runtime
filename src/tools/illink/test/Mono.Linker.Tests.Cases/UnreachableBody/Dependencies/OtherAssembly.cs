namespace Mono.Linker.Tests.Cases.UnreachableBody.Dependencies
{
	public class OtherAssembly
	{
		public static void UnusedSanityCheck ()
		{
		}

		public class Foo
		{
			public void Method ()
			{
				UsedByMethod ();
			}

			void UsedByMethod ()
			{
			}
		}
	}
}