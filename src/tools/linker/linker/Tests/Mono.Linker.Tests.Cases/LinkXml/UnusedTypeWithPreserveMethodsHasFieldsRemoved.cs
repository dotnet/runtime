using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedTypeWithPreserveMethodsHasFieldsRemoved {
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Unused {
			public int Field1;
			private int Field2;
			internal int Field3;
			public static int Field4;
			private static int Field5;
			internal static int Field6;

			[Kept]
			[KeptBackingField]
			public string Property1 { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			private string Property2 { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			internal string Property3 { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			public static string Property4 { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			private static string Property5 { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			internal static string Property6 { [Kept] get; [Kept] set; }

			[Kept]
			public void Method1 ()
			{
			}

			[Kept]
			private void Method2 ()
			{
			}

			[Kept]
			internal void Method3 ()
			{
			}

			[Kept]
			public static void Method4 ()
			{
			}

			[Kept]
			private static void Method5 ()
			{
			}

			[Kept]
			internal static void Method6 ()
			{
			}
		}
	}
}