using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	public class LocalDataFlowKeptMembers
	{
		public static void Main ()
		{
			// These behave as expected
			TestBranchMergeGoto ();
			TestBranchMergeIf ();
			TestBranchMergeIfElse ();
			TestBranchMergeSwitch ();

			// These are overly conservative (keep members on the wrong types)
			// Only illustrate a few cases to keep it concise.
			TestBranchGoto ();
			TestBranchIf ();
			TestBranchIfElse ();
		}

		[Kept]
		public static void TestBranchMergeGoto ()
		{
			Type t = typeof (BranchMergeGotoType1);
			if (String.Empty.Length == 0)
				goto End;
			t = typeof (BranchMergeGotoType2);

		End:
			RequirePublicFields (t); // keeps fields for both types
		}

		[Kept]
		class BranchMergeGotoType1
		{
			[Kept]
			public string field;
		}

		[Kept]
		class BranchMergeGotoType2
		{
			[Kept]
			public string field;
		}

		[Kept]
		public static void TestBranchMergeIf ()
		{
			Type t = typeof (BranchMergeIfType1);
			if (String.Empty.Length == 0)
				t = typeof (BranchMergeIfType2);

			RequirePublicFields (t); // keeps fields for both types
		}

		[Kept]
		class BranchMergeIfType1
		{
			[Kept]
			public string field;
		}

		[Kept]
		class BranchMergeIfType2
		{
			[Kept]
			public string field;
		}

		[Kept]
		public static void TestBranchMergeIfElse ()
		{
			Type t = null;
			if (String.Empty.Length == 0) {
				t = typeof (BranchMergeIfElseType1);
			} else {
				t = typeof (BranchMergeIfElseType2);
			}
			RequirePublicFields (t); // keeps fields for both types
		}

		[Kept]
		class BranchMergeIfElseType1
		{
			[Kept]
			public string field;
		}

		[Kept]
		class BranchMergeIfElseType2
		{
			[Kept]
			public string field;
		}

		[Kept]
		static int _switchOnField;

		[Kept]
		public static void TestBranchMergeSwitch ()
		{
			Type t = null;
			switch (_switchOnField) {
			case 0:
				t = typeof (BranchMergeSwitchType0);
				break;
			case 1:
				t = typeof (BranchMergeSwitchType1);
				break;
			case 2:
				t = typeof (BranchMergeSwitchType2);
				break;
			case 3:
				t = typeof (BranchMergeSwitchType3);
				break;
			}
			RequirePublicFields (t); // keeps fields for all types
		}

		[Kept]
		public static void TestBranchGoto ()
		{
			Type t = typeof (BranchGotoType1);
			if (String.Empty.Length == 0)
				goto End;
			t = typeof (BranchGotoType2);
			RequirePublicFields (t);
		End:
			return;
		}

		[Kept]
		class BranchGotoType1
		{
			[Kept] // unnecessary
			public string field;
		}

		[Kept]
		class BranchGotoType2
		{
			[Kept]
			public string field;
		}

		[Kept]
		public static void TestBranchIf ()
		{
			Type t = typeof (BranchIfType1);
			if (String.Empty.Length == 0) {
				t = typeof (BranchIfType2);
				RequirePublicFields (t);
			}
		}

		[Kept]
		class BranchIfType1
		{
			[Kept] // unneccessary
			public string field;
		}

		[Kept]
		class BranchIfType2
		{
			[Kept]
			public string field;
		}

		[Kept]
		public static void TestBranchIfElse ()
		{
			Type t;
			if (String.Empty.Length == 0) {
				// because this branch *happens* to come first in IL, we will only see one value
				t = typeof (BranchIfElseTypeWithMethods);
				RequirePublicMethods (t); // this works
			} else {
				// because this branch *happens* to come second in IL, we will see the merged value for str
				t = typeof (BranchIfElseTypeWithFields);
				RequirePublicFields (t); // keeps field on BranchIfElseTypeWithMethods
			}
		}

		[Kept]
		class BranchIfElseTypeWithMethods
		{
			[Kept]
			public void Method () { }
			[Kept] // unnecessary
			public string field;
		}

		[Kept]
		class BranchIfElseTypeWithFields
		{
			public void Method () { }
			[Kept]
			public string field;
		}

		[Kept]
		class BranchMergeSwitchType0
		{
			[Kept]
			public string field;
		}

		[Kept]
		class BranchMergeSwitchType1
		{
			[Kept]
			public string field;
		}

		[Kept]
		class BranchMergeSwitchType2
		{
			[Kept]
			public string field;
		}

		[Kept]
		class BranchMergeSwitchType3
		{
			[Kept]
			public string field;
		}


		[Kept]
		public static void RequirePublicFields (
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
			Type type)
		{
		}

		[Kept]
		public static void RequirePublicMethods (
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			Type type)
		{
		}
	}
}
