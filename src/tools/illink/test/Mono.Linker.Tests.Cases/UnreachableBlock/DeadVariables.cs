using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class DeadVariables
	{
		public static void Main ()
		{
			Test_1 ();
			Test_2 (4);
			Test_3 ();
		}

		[Kept]
		[ExpectBodyModified]
		[ExpectedLocalsSequence (new string[0])]
		static void Test_1 ()
		{
			if (!AlwaysTrue) {
				int var = 1;
				Console.WriteLine (var);
			}
		}

		[Kept]
		[ExpectBodyModified]
		[ExpectedLocalsSequence (new string[] { "System.Object", "System.Int32" })]
		static int Test_2 (int arg)
		{
			if (!AlwaysTrue) {
				long var = 3;
				Console.WriteLine (var++);
				return (int) var;
			}

			{
				int b = arg;
				Console.WriteLine (b++);
				return b;
			}
		}

		[Kept]
		[ExpectBodyModified]
		[ExpectedLocalsSequence (new string[] { "System.Int32", "System.DateTime", "System.DateTimeOffset", "System.DateTimeOffset" })]
		static int Test_3 ()
		{
			var b = 3;
			var c = new DateTime ();
			var d = new DateTimeOffset ();
			var e = new DateTimeOffset ();

			if (!AlwaysTrue) {
				int a = b;
				ref int var = ref a;
			}

			Console.WriteLine (b.ToString (), c, d, e);

			return 2;
		}

		static bool AlwaysTrue {
			get {
				return true;
			}
		}
	}
}