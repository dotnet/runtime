// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the ExpectedWarning attributes.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class LocalDataFlow
	{
		public static void Main ()
		{
			// These behave as expected
			TestBranchMergeGoto ();
			TestBranchMergeIf ();
			TestBranchMergeNullCoalesce ();
			TestBranchMergeNullCoalescingAssignment ();
			TestBranchMergeNullCoalescingAssignmentComplex ();
			TestBranchMergeDiscardNullCoalesce ();
			TestBranchMergeIfElse ();
			TestBranchMergeSwitch ();
			TestBranchMergeTry ();
			TestBranchMergeCatch ();

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
			TestBranchMergeFinally ();

			// These are missing warnings (potential failure at runtime)
			TestBackwardsEdgeGoto ();
			TestBackwardsEdgeLoop ();

			TestNoWarningsInRUCMethod ();
			TestNoWarningsInRUCType ();

			// These are probably just bugs
			TestBackwardEdgeWithLdElem ();
		}

		[ExpectedWarning ("IL2072",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields) + "(String)")]
		[ExpectedWarning ("IL2072",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicMethods) + "(String)")]
		public static void TestBranchMergeGoto ()
		{
			string str = GetWithPublicMethods ();
			if (String.Empty.Length == 0)
				goto End;
			str = GetWithPublicFields ();

		End:
			str.RequiresPublicFields (); // warns for GetWithPublicMethods
			str.RequiresPublicMethods (); // warns for GetWithPublicFields
		}

		[ExpectedWarning ("IL2072",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields) + "(String)")]
		[ExpectedWarning ("IL2072",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicMethods) + "(String)")]
		public static void TestBranchMergeIf ()
		{
			string str = GetWithPublicMethods ();
			if (String.Empty.Length == 0)
				str = GetWithPublicFields ();

			str.RequiresPublicFields (); // warns for GetWithPublicMethods
			str.RequiresPublicMethods (); // warns for GetWithPublicFields
		}

		[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowStringExtensions.RequiresAll))]
		[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowStringExtensions.RequiresAll))]
		public static void TestBranchMergeNullCoalesce ()
		{
			string str = GetWithPublicMethods () ?? GetWithPublicFields ();

			str.RequiresAll ();
		}

		[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowStringExtensions.RequiresAll))]
		[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowStringExtensions.RequiresAll))]
		public static void TestBranchMergeNullCoalescingAssignment ()
		{
			string str = GetWithPublicMethods ();
			str ??= GetWithPublicFields ();

			str.RequiresAll ();
		}

		[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowStringExtensions.RequiresAll))]
		[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowStringExtensions.RequiresAll))]
		[ExpectedWarning ("IL2072", nameof (GetWithPublicConstructors), nameof (DataFlowStringExtensions.RequiresAll))]
		public static void TestBranchMergeNullCoalescingAssignmentComplex ()
		{
			string str = GetWithPublicMethods ();
			str ??= GetWithPublicFields () ?? GetWithPublicConstructors ();

			str.RequiresAll ();
		}

		[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowStringExtensions.RequiresAll))]
		[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowStringExtensions.RequiresAll))]
		public static void TestBranchMergeDiscardNullCoalesce ()
		{
			(_ = GetWithPublicMethods () ?? GetWithPublicFields ()).RequiresAll ();
		}

		[ExpectedWarning ("IL2072",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields) + "(String)")]
		[ExpectedWarning ("IL2072",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicMethods) + "(String)")]
		public static void TestBranchMergeIfElse ()
		{
			string str = null;
			if (String.Empty.Length == 0) {
				str = GetWithPublicFields ();
			} else {
				str = GetWithPublicMethods ();
			}
			str.RequiresPublicFields (); // warns for GetWithPublicMethods
			str.RequiresPublicMethods (); // warns for GetWithPublicFields
		}

		static int _switchOnField;

		[ExpectedWarning ("IL2072",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicFields()",
				nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresNonPublicMethods) + "(String)")]
		[ExpectedWarning ("IL2072",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicMethods()",
				nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresNonPublicMethods) + "(String)")]
		[ExpectedWarning ("IL2072",
				"Mono.Linker.Tests.Cases.DataFlow.LocalDataFlow.GetWithPublicConstructors()",
				nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresNonPublicMethods) + "(String)")]
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

			str.RequiresNonPublicMethods (); // warns for GetWithPublicFields, GetWithPublicMethods, and GetWithPublicConstructors
		}


		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields) + "(String)",
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicMethods) + "(String)",
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicFields) + "()")]
		public static void TestBranchMergeTry ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;

				str = GetWithPublicFields ();
			} catch {
			}

			str.RequiresPublicFields (); // warns for GetWithPublicMethods
			str.RequiresPublicMethods (); // warns for GetWithPublicFields
		}


		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields) + "(String)",
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicMethods) + "(String)",
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicFields) + "()")]
		public static void TestBranchMergeCatch ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
				str = GetWithPublicFields ();
			}

			str.RequiresPublicFields (); // warns for GetWithPublicMethods
			str.RequiresPublicMethods (); // warns for GetWithPublicFields
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicMethods) + "(String)",
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicFields) + "()")]
		// ILLink produces extraneous warnings
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields) + "(String)",
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicMethods) + "()", Tool.Trimmer, "")]
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
			str.RequiresPublicFields (); // should not warn
			str.RequiresPublicMethods (); // should warn
		}

		// Analyzer gets this right (no warning), but trimmer merges all branches going forward.
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields) + "(String)", Tool.Trimmer, "")]
		public static void TestBranchGoto ()
		{
			string str = GetWithPublicMethods ();
			if (String.Empty.Length == 0)
				goto End;
			str = GetWithPublicFields ();
			str.RequiresPublicFields (); // produces a warning
		End:
			return;
		}

		// Analyzer gets this right (no warning), but trimmer merges all branches going forward.
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields), Tool.Trimmer, "")]
		public static void TestBranchIf ()
		{
			string str = GetWithPublicMethods ();
			if (String.Empty.Length == 0) {
				str = GetWithPublicFields (); // dataflow will merge this with the value from the previous basic block
				str.RequiresPublicFields (); // produces a warning (technically it should not)
			}
		}

		// Analyzer gets this right (no warning), but trimmer merges all branches going forward.
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields), Tool.Trimmer, "")]
		public static void TestBranchIfElse ()
		{
			string str;
			if (String.Empty.Length == 0) {
				// because this branch *happens* to come first in IL, we will only see one value
				str = GetWithPublicMethods ();
				str.RequiresPublicMethods (); // this works
			} else {
				// because this branch *happens* to come second in IL, we will see the merged value for str
				str = GetWithPublicFields ();
				str.RequiresPublicFields (); // produces a warning
			}
		}

		// Analyzer gets this right (no warning), but trimmer merges all branches going forward.
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresNonPublicMethods) + "(String)", Tool.Trimmer, "")]
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicMethods) + "(String)", Tool.Trimmer, "")]
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicMethods) + "(String)", Tool.Trimmer, "")]
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicConstructors) + "(String)", Tool.Trimmer, "")]
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicConstructors) + "(String)", Tool.Trimmer, "")]
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicConstructors) + "(String)", Tool.Trimmer, "")]
		public static void TestBranchSwitch ()
		{
			string str = null;
			switch (_switchOnField) {
			case 0:
				str = GetWithPublicFields ();
				str.RequiresPublicFields (); // success
				break;
			case 1:
				str = GetWithNonPublicMethods ();
				str.RequiresNonPublicMethods (); // warns for GetWithPublicFields
				break;
			case 2:
				str = GetWithPublicMethods ();
				str.RequiresPublicMethods (); // warns for GetWithPublicFields and GetWithNonPublicMethods
				break;
			case 3:
				str = GetWithPublicConstructors ();
				str.RequiresPublicConstructors (); // warns for GetWithPublicFields, GetWithNonPublicMethods, GetWithPublicMethods
				break;
			}
		}

		// Analyzer gets this right (no warning), but trimmer merges all branches going forward.
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields),
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicMethods) + "()", Tool.Trimmer, "")]
		public static void TestBranchTry ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;

				str = GetWithPublicFields ();
				str.RequiresPublicFields (); // warns for GetWithPublicMethods
			} catch {
			}
		}

		// Analyzer gets this right (no warning), but trimmer merges all branches going forward.
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields),
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicMethods) + "()", Tool.Trimmer, "")]
		public static void TestBranchCatch ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
				str = GetWithPublicFields ();
				str.RequiresPublicFields (); // warns for GetWithPublicMethods
			}
		}

		// Analyzer gets this right (no warning), but trimmer merges all branches going forward.
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicFields),
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicMethods) + "()", Tool.Trimmer, "")]
		public static void TestBranchFinally ()
		{
			string str = GetWithPublicMethods ();
			try {
				if (String.Empty.Length == 0)
					throw null;
			} catch {
			} finally {
				str = GetWithPublicFields ();
				str.RequiresPublicFields (); // warns for GetWithPublicMethods
			}
		}

		// Analyzer gets this right, but ILLink doesn't consider backwards branches.
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicMethods) + "(String)",
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicFields) + "()", Tool.Analyzer, "")]
		public static void TestBackwardsEdgeLoop ()
		{
			string str = GetWithPublicMethods ();
			string prev = null;
			for (int i = 0; i < 5; i++) {
				prev = str; // dataflow will only consider the first reaching definition of "str" above
				str = GetWithPublicFields (); // dataflow will merge values to track both possible annotation kinds
			}

			// str.RequiresPublicMethods (); // this would produce a warning for the value that comes from GetWithPublicFields, as expected
			prev.RequiresPublicMethods (); // this produces no warning, even though "prev" will have the value from GetWithPublicFields!
		}

		// Analyzer gets this right, but ILLink doesn't consider backwards branches.
		[ExpectedWarning ("IL2072", nameof (DataFlowStringExtensions) + "." + nameof (DataFlowStringExtensions.RequiresPublicMethods) + "(String)",
			nameof (LocalDataFlow) + "." + nameof (GetWithPublicFields) + "()", Tool.Analyzer, "")]
		public static void TestBackwardsEdgeGoto ()
		{
			string str = null;
			goto ForwardTarget;
		BackwardTarget:
			str.RequiresPublicMethods (); // should warn for the value that comes from GetWithPublicFields, but it doesn't.
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
			GetWithPublicMethods ().RequiresAll ();
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
				GetWithPublicMethods ().RequiresAll ();
			}

			public void InstanceMethod ()
			{
				GetWithPublicMethods ().RequiresAll ();
			}

			public virtual void VirtualMethod ()
			{
				GetWithPublicMethods ().RequiresAll ();
			}
		}

		// Analyzer doesn't see through foreach over array at all -  will not warn
		[ExpectedWarning ("IL2063", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/96646")] // The types loaded from the array don't have annotations, so the "return" should warn
		[ExpectedWarning ("IL2073", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/96646")] // Analyzer tracks resultType as the value from IEnumerable.Current.get()
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		public static Type TestBackwardEdgeWithLdElem (Type[] types = null)
		{
			Type resultType = null;
			foreach (var type in types) {
				resultType = type;
			}

			return resultType;
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
