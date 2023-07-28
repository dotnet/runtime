namespace Mono.Linker.Tests.Cases.Statics
{
	class UnusedStaticConstructorGetsRemoved
	{
		public static void Main ()
		{
		}

		static void Dead ()
		{
			new B ();
		}

		class B
		{
			static B ()
			{
			}
		}
	}
}