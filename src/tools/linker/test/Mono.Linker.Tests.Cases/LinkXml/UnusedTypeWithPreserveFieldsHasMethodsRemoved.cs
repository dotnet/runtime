using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedTypeWithPreserveFieldsHasMethodsRemoved {
		public static void Main ()
		{
		}

		[Kept]
		class Unused {
			[Kept]
			public int Field1;

			[Kept]
			private int Field2;

			[Kept]
			internal int Field3;

			[Kept]
			public static int Field4;

			[Kept]
			private static int Field5;

			[Kept]
			internal static int Field6;

			public string Property1 { get; set; }
			private string Property2 { get; set; }
			internal string Property3 { get; set; }
			public static string Property4 { get; set; }
			private static string Property5 { get; set; }
			internal static string Property6 { get; set; }

			[Kept]
			public void PreservedMethod ()
			{
			}

			public void Method1 ()
			{
			}

			private void Method2 ()
			{
			}

			internal void Method3 ()
			{
			}

			public static void Method4 ()
			{
			}

			private static void Method5 ()
			{
			}

			internal static void Method6 ()
			{
			}
		}
	}
}