using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("RemoveBody.xml")]
	public class RemoveBody
	{
		public static void Main ()
		{
			new RemoveBody ();
			new NestedType (5);

			TestMethod_1 ();
			TestMethod_2<int> ();
		}

		struct NestedType
		{
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldstr 'Linked away'",
				"newobj System.Void System.NotSupportedException::.ctor(System.String)",
				"throw",
			})]
			public NestedType (int arg)
			{
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"ldarg.0",
				"call System.Void System.Object::.ctor()",
				"ldstr 'Linked away'",
				"newobj System.Void System.NotSupportedException::.ctor(System.String)",
				"throw",
			})]
		public RemoveBody ()
		{
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"ldstr 'Linked away'",
				"newobj System.Void System.NotSupportedException::.ctor(System.String)",
				"throw",
			})]
		static void TestMethod_1 ()
		{
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"ldstr 'Linked away'",
				"newobj System.Void System.NotSupportedException::.ctor(System.String)",
				"throw",
			})]
		[ExpectLocalsModified]
		static T TestMethod_2<T> ()
		{
			return default (T);
		}
	}
}