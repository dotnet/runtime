using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	public class LocalDataFlow
	{
		public static void Main ()
		{
			// These behave as expected
			TestBranchMergeGoto ();
			TestBranchMergeIf ();
			TestBranchMergeIfElse ();
			TestBranchMergeSwitch ();
			TestBranchMergeTry ();
			TestBranchMergeCatch ();
			TestBranchMergeFinally ();

			// The remaining tests illustrate current limitations of the analysis
			// that we might be able to lift in the future.

			// These are overly conservative (extraneous warnings)
			// 	 https://github.com/dotnet/linker/issues/2550
			TestBranchGoto ();
			TestBranchIf ();
			TestBranchIfElse ();
			TestBranchSwitch ();
			TestBranchTry ();
			TestBranchCatch ();
			TestBranchFinally ();

			// These are missing warnings (potential failure at runtime)
			TestBackwardsEdgeGoto ();
			TestBackwardsEdgeLoop ();

			TestNoWarningsInRUCMethod ();
			TestNoWarningsInRUCType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicFields(String)"})]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicMethods), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicMethods(String)"})]
		public static void TestBranchMergeGoto ()
		{
			string str = GetWithPublicMethods ();
			if (String.Empty.Length == 0)
				goto End;
			str = GetWithPublicFields ();

		End:
			RequirePublicFields (str); // warns for GetWithPublicMethods
			RequirePublicMethods (str); // warns for GetWithPublicFields
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicFields(String)" })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicMethods), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicMethods(String)" })]
		public static void TestBranchMergeIf ()
		{
			string str = GetWithPublicMethods ();
			if (String.Empty.Length == 0)
				str = GetWithPublicFields ();

			RequirePublicFields (str); // warns for GetWithPublicMethods
			RequirePublicMethods (str); // warns for GetWithPublicFields
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicFields(String)" })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicMethods), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicMethods(String)" })]
		public static void TestBranchMergeIfElse ()
		{
			string str = null;
			if (String.Empty.Length == 0) {
				str = GetWithPublicFields ();
			} else {
				str = GetWithPublicMethods ();
			}
			RequirePublicFields (str); // warns for GetWithPublicMethods
			RequirePublicMethods (str); // warns for GetWithPublicFields
		}

		static int _switchOnField;

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireNonPublicMethods), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequireNonPublicMethods(String)" })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireNonPublicMethods), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequireNonPublicMethods(String)" })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireNonPublicMethods), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicConstructors()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequireNonPublicMethods(String)" })]
		public static void TestBranchMergeSwitch ()
		{
			string str = null;
			switch (_switchOnField) {
			case 0:
				str = GetWithPublicFields ();
				break;
			case 1:
				str = GetWithNonPublicMethods ();
				break;
			case 2:
				str = GetWithPublicMethods ();
				break;
			case 3:
				str = GetWithPublicConstructors ();
				break;
			}
			RequireNonPublicMethods (str); // warns for GetWithPublicFields, GetWithPublicMethods, and GetWithPublicConstructors
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicFields(String)" })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicMethods), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicMethods(String)" })]
		public static void TestBranchMergeTry ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;

				str = GetWithPublicFields ();
			} catch {
			}
			RequirePublicFields (str); // warns for GetWithPublicMethods
			RequirePublicMethods (str); // warns for GetWithPublicFields
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicFields(String)" })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicMethods), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicMethods(String)" })]
		public static void TestBranchMergeCatch ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
				str = GetWithPublicFields ();
			}
			RequirePublicFields (str); // warns for GetWithPublicMethods
			RequirePublicMethods (str); // warns for GetWithPublicFields
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicFields(String)" })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicMethods), new Type[] { typeof (string) },
			messageCode: "IL2072", message: new string[] {
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				"type",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.RequirePublicMethods(String)" })]
		public static void TestBranchMergeFinally ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
			} finally {
				str = GetWithPublicFields ();
			}
			RequirePublicFields (str); // warns for GetWithPublicMethods
			RequirePublicMethods (str); // warns for GetWithPublicFields
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) }, messageCode: "IL2072")]
		public static void TestBranchGoto ()
		{
			string str = GetWithPublicMethods ();
			if (String.Empty.Length == 0)
				goto End;
			str = GetWithPublicFields ();
			RequirePublicFields (str); // produces a warning
		End:
			return;
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) }, messageCode: "IL2072")]
		public static void TestBranchIf ()
		{
			string str = GetWithPublicMethods ();
			if (String.Empty.Length == 0) {
				str = GetWithPublicFields (); // dataflow will merge this with the value from the previous basic block
				RequirePublicFields (str); // produces a warning (technically it should not)
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) }, messageCode: "IL2072")]
		public static void TestBranchIfElse ()
		{
			string str;
			if (String.Empty.Length == 0) {
				// because this branch *happens* to come first in IL, we will only see one value
				str = GetWithPublicMethods ();
				RequirePublicMethods (str); // this works
			} else {
				// because this branch *happens* to come second in IL, we will see the merged value for str
				str = GetWithPublicFields ();
				RequirePublicFields (str); // produces a warning
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireNonPublicMethods), new Type[] { typeof (string) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicMethods), new Type[] { typeof (string) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicMethods), new Type[] { typeof (string) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (string) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (string) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (string) }, messageCode: "IL2072")]
		public static void TestBranchSwitch ()
		{
			string str = null;
			switch (_switchOnField) {
			case 0:
				str = GetWithPublicFields ();
				RequirePublicFields (str); // success
				break;
			case 1:
				str = GetWithNonPublicMethods ();
				RequireNonPublicMethods (str); // warns for GetWithPublicFields
				break;
			case 2:
				str = GetWithPublicMethods ();
				RequirePublicMethods (str); // warns for GetWithPublicFields and GetWithNonPublicMethods
				break;
			case 3:
				str = GetWithPublicConstructors ();
				RequirePublicConstructors (str); // warns for GetWithPublicFields, GetWithNonPublicMethods, GetWithPublicMethods
				break;
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) }, messageCode: "IL2072")]
		public static void TestBranchTry ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;

				str = GetWithPublicFields ();
				RequirePublicFields (str); // warns for GetWithPublicMethods
			} catch {
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) }, messageCode: "IL2072")]
		public static void TestBranchCatch ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
				str = GetWithPublicFields ();
				RequirePublicFields (str); // warns for GetWithPublicMethods
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicFields), new Type[] { typeof (string) }, messageCode: "IL2072")]
		public static void TestBranchFinally ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
			} finally {
				str = GetWithPublicFields ();
				RequirePublicFields (str); // warns for GetWithPublicMethods
			}
		}

		[RecognizedReflectionAccessPattern]
		public static void TestBackwardsEdgeLoop ()
		{
			string str = GetWithPublicMethods ();
			string prev = null;
			for (int i = 0; i < 5; i++) {
				prev = str; // dataflow will only consider the first reaching definition of "str" above
				str = GetWithPublicFields (); // dataflow will merge values to track both possible annotation kinds
			}

			// RequirePublicMethods (str); // this would produce a warning for the value that comes from GetWithPublicFields, as expected
			RequirePublicMethods (prev); // this produces no warning, even though "prev" will have the value from GetWithPublicFields!
		}

		[RecognizedReflectionAccessPattern]
		public static void TestBackwardsEdgeGoto ()
		{
			string str = null;
			goto ForwardTarget;
		BackwardTarget:
			RequirePublicMethods (str); // should warn for the value that comes from GetWithPublicFields, but it doesn't.
			return;

		ForwardTarget:
			str = GetWithPublicFields ();
			goto BackwardTarget;
		}

		[ExpectedWarning ("IL2026", nameof (RUCMethod), "message")]
		public static void TestNoWarningsInRUCMethod ()
		{
			RUCMethod ();
		}

		[RequiresUnreferencedCode ("message")]
		public static void RUCMethod ()
		{
			RequireAll (GetWithPublicMethods ());
		}

		[ExpectedWarning ("IL2026", nameof (RUCType) + "." + nameof (RUCType), "message")]
		[ExpectedWarning ("IL2026", nameof (RUCType.StaticMethod), "message")]
		public static void TestNoWarningsInRUCType ()
		{
			RUCType.StaticMethod ();
			var rucType = new RUCType ();
			rucType.InstanceMethod ();
			rucType.VirtualMethod ();
		}

		[RequiresUnreferencedCode ("message")]
		public class RUCType
		{
			public static void StaticMethod ()
			{
				RequireAll (GetWithPublicMethods ());
			}

			public void InstanceMethod ()
			{
				RequireAll (GetWithPublicMethods ());
			}

			public virtual void VirtualMethod ()
			{
				RequireAll (GetWithPublicMethods ());
			}
		}

		public static void RequireAll (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			string type)
		{
		}

		public static void RequirePublicFields (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
			string type)
		{
		}

		public static void RequireNonPublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
			string type)
		{
		}
		public static void RequirePublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			string type)
		{
		}

		public static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			string type)
		{
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		public static string GetWithPublicFields ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
		public static string GetWithNonPublicMethods ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		public static string GetWithPublicMethods ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		public static string GetWithPublicConstructors ()
		{
			return null;
		}
	}
}
