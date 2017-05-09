using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedTypeWithPreserveNothingHasMembersRemoved {
		public static void Main ()
		{
		}

		[Kept]
		class Unused {
			public int Field1;
			private int Field2;
			internal int Field3;
			public static int Field4;
			private static int Field5;
			internal static int Field6;

			public string Property1 { get; set; }
			private string Property2 { get; set; }
			internal string Property3 { get; set; }
			public static string Property4 { get; set; }
			private static string Property5 { get; set; }
			internal static string Property6 { get; set; }

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