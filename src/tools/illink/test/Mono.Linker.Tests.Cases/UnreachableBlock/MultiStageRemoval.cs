using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	public class MultiStageRemoval
	{
		public static void Main()
		{
			TestMethod_1 ();
			TestMethod_2 ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestMethod_1 ()
		{
			if (TestProperty_int () == 0)
				NeverReached_1 ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestMethod_2 ()
		{
			if (TestProperty_bool_twice () >= 0)
				NeverReached_2 ();
		}

		[Kept]
		[ExpectBodyModified]
		static int TestProperty_int ()
		{
			if (Prop > 5) {
				return 11;
			}

			return 0;
		}

		[Kept]
		[ExpectBodyModified]
		static int TestProperty_bool_twice ()
		{
			if (PropBool) {
				return -5;
			}

			if (TestProperty_bool_twice () == 4)
				return -1;

			return 0;
		}        

		[Kept]
		static int Prop {
			[Kept]
			get {
				return 9;
			}
		}

		[Kept]
		static bool PropBool {
			[Kept]
			get {
				return true;
			}
		}        
        
		static void NeverReached_1 ()
		{			
		}

		static void NeverReached_2 ()
		{			
		}        
	}
}