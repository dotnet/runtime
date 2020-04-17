using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Runtime.CompilerServices;

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
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithMethods()' " +
			"with dynamically accessed member kinds 'Methods' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireFields(System.String)' " +
			"which requires dynamically accessed member kinds `Fields`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Fields'.")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireMethods), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithFields()' " +
			"with dynamically accessed member kinds 'Fields' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireMethods(System.String)' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		public static void TestBranchMergeGoto ()
		{
			string str = GetWithMethods ();
			if (String.Empty.Length == 0)
				goto End;
			str = GetWithFields ();

			End:
			RequireFields (str); // warns for GetWithMethods
			RequireMethods (str); // warns for GetWithFields
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithMethods()' " +
			"with dynamically accessed member kinds 'Methods' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireFields(System.String)' " +
			"which requires dynamically accessed member kinds `Fields`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Fields'.")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireMethods), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithFields()' " +
			"with dynamically accessed member kinds 'Fields' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireMethods(System.String)' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		public static void TestBranchMergeIf ()
		{
			string str = GetWithMethods ();
			if (String.Empty.Length == 0)
				str = GetWithFields ();

			RequireFields (str); // warns for GetWithMethods
			RequireMethods (str); // warns for GetWithFields
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithMethods()' " +
			"with dynamically accessed member kinds 'Methods' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireFields(System.String)' " +
			"which requires dynamically accessed member kinds `Fields`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Fields'.")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireMethods), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithFields()' " +
			"with dynamically accessed member kinds 'Fields' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireMethods(System.String)' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		public static void TestBranchMergeIfElse ()
		{
			string str = null;
			if (String.Empty.Length == 0) {
				str = GetWithFields ();
			} else {
				str = GetWithMethods ();
			}
			RequireFields (str); // warns for GetWithMethods
			RequireMethods (str); // warns for GetWithFields
		}

		static int _switchOnField;

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireMethods), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithFields()' " +
			"with dynamically accessed member kinds 'Fields' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireMethods(System.String)' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireMethods), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithPublicMethods()' " +
			"with dynamically accessed member kinds 'PublicMethods' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireMethods(System.String)' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireMethods), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithConstructors()' " +
			"with dynamically accessed member kinds 'Constructors' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireMethods(System.String)' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		public static void TestBranchMergeSwitch ()
		{
			string str = null;
			switch (_switchOnField) {
			case 0:
				str = GetWithFields ();
				break;
			case 1:
				str = GetWithMethods ();
				break;
			case 2:
				str = GetWithPublicMethods ();
				break;
			case 3:
				str = GetWithConstructors ();
				break;
			}
			RequireMethods (str); // warns for GetWithFields, GetWithPublicMethods, and GetWithConstructors
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithMethods()' " +
			"with dynamically accessed member kinds 'Methods' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireFields(System.String)' " +
			"which requires dynamically accessed member kinds `Fields`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Fields'.")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireMethods), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithFields()' " +
			"with dynamically accessed member kinds 'Fields' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireMethods(System.String)' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		public static void TestBranchMergeTry ()
		{
			string str = GetWithMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;

				str = GetWithFields ();
			} catch {
			}
			RequireFields (str); // warns for GetWithMethods
			RequireMethods (str); // warns for GetWithFields
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithMethods()' " +
			"with dynamically accessed member kinds 'Methods' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireFields(System.String)' " +
			"which requires dynamically accessed member kinds `Fields`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Fields'.")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireMethods), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithFields()' " +
			"with dynamically accessed member kinds 'Fields' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireMethods(System.String)' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		public static void TestBranchMergeCatch ()
		{
			string str = GetWithMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
				str = GetWithFields ();
			}
			RequireFields (str); // warns for GetWithMethods
			RequireMethods (str); // warns for GetWithFields
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithMethods()' " +
			"with dynamically accessed member kinds 'Methods' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireFields(System.String)' " +
			"which requires dynamically accessed member kinds `Fields`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Fields'.")]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireMethods), new Type [] { typeof (string) },
			"The return value of method 'System.String Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::GetWithFields()' " +
			"with dynamically accessed member kinds 'Fields' " +
			"is passed into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow::RequireMethods(System.String)' " +
			"which requires dynamically accessed member kinds `Methods`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Methods'.")]
		public static void TestBranchMergeFinally ()
		{
			string str = GetWithMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
			} finally {
				str = GetWithFields ();
			}
			RequireFields (str); // warns for GetWithMethods
			RequireMethods (str); // warns for GetWithFields
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) })]
		public static void TestBranchGoto ()
		{
			string str = GetWithMethods ();
			if (String.Empty.Length == 0)
				goto End;
			str = GetWithFields ();
			RequireFields (str); // produces a warning
			End:
			return;
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) })]
		public static void TestBranchIf ()
		{
			string str = GetWithMethods ();
			if (String.Empty.Length == 0) {
				str = GetWithFields (); // dataflow will merge this with the value from the previous basic block
				RequireFields (str); // produces a warning
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) })]
		public static void TestBranchIfElse ()
		{
			string str;
			if (String.Empty.Length == 0) {
				// because this branch *happens* to come first in IL, we will only see one value
				str = GetWithMethods ();
				RequireMethods (str); // this works
			} else {
				// because this branch *happens* to come second in IL, we will see the merged value for str
				str = GetWithFields ();
				RequireFields (str); // produces a warning
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireMethods), new Type [] { typeof (string) })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequirePublicMethods), new Type [] { typeof (string) })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireConstructors), new Type [] { typeof (string) })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireConstructors), new Type [] { typeof (string) })]
		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireConstructors), new Type [] { typeof (string) })]
		public static void TestBranchSwitch ()
		{
			string str = null;
			switch (_switchOnField) {
			case 0:
				str = GetWithFields ();
				RequireFields (str); // success
				break;
			case 1:
				str = GetWithMethods ();
				RequireMethods (str); // warns for GetWithFields
				break;
			case 2:
				str = GetWithPublicMethods ();
				RequirePublicMethods (str); // warns for GetWithFields
				break;
			case 3:
				str = GetWithConstructors ();
				RequireConstructors (str); // warns for GetWithFields, GetWithMethods, GetWithPublicMethods
				break;
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) })]
		public static void TestBranchTry ()
		{
			string str = GetWithMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;

				str = GetWithFields ();
				RequireFields (str); // warns for GetWithMethods
			} catch {
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) })]
		public static void TestBranchCatch ()
		{
			string str = GetWithMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
				str = GetWithFields ();
				RequireFields (str); // warns for GetWithMethods
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (LocalDataFlow), nameof (RequireFields), new Type [] { typeof (string) })]
		public static void TestBranchFinally ()
		{
			string str = GetWithMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
			} finally {
				str = GetWithFields ();
				RequireFields (str); // warns for GetWithMethods
			}
		}

		[RecognizedReflectionAccessPattern]
		public static void TestBackwardsEdgeLoop ()
		{
			string str = GetWithMethods ();
			string prev = null;
			for (int i = 0; i < 5; i++) {
				prev = str; // dataflow will only consider the first reaching definition of "str" above
				str = GetWithFields (); // dataflow will merge values to track both possible annotation kinds
			}

			// RequireMethods (str); // this would produce a warning for the value that comes from GetWithFields, as expected
			RequireMethods (prev); // this produces no warning, even though "prev" will have the value from GetWithFields!
		}

		[RecognizedReflectionAccessPattern]
		public static void TestBackwardsEdgeGoto ()
		{
			string str = null;
			goto ForwardTarget;
			BackwardTarget:
			RequireMethods (str); // should warn for the value that comes from GetWithFields, but it doesn't.
			return;

			ForwardTarget:
			str = GetWithFields ();
			goto BackwardTarget;
		}

		public static void RequireFields (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Fields)]
			string type)
		{
		}

		public static void RequireMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Methods)]
			string type)
		{
		}
		public static void RequirePublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicMethods)]
			string type)
		{
		}

		public static void RequireConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			string type)
		{
		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Fields)]
		public static string GetWithFields () {
			return null;
		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Methods)]
		public static string GetWithMethods () {
			return null;
		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicMethods)]
		public static string GetWithPublicMethods () {
			return null;
		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
		public static string GetWithConstructors () {
			return null;
		}
	}
}
