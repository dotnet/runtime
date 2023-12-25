using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class ExceptionalDataFlow
	{
		public static void Main ()
		{
			TryFlowsToFinally ();
			TryFlowsToAfterFinally ();
			MultipleTryExits ();
			MultipleFinallyPaths ();
			FinallyChain ();
			FinallyChainWithPostFinallyState ();
			TryFlowsToCatch ();
			CatchFlowsToFinally ();
			CatchFlowsToAfterTry ();
			CatchFlowsToAfterFinally ();
			FinallyFlowsToAfterFinally ();
			TryFlowsToMultipleCatchAndFinally ();
			NestedWithFinally ();
			ControlFlowsOutOfMultipleFinally ();
			NestedWithCatch ();
			CatchInTry ();
			CatchInTryWithFinally ();
			CatchInFinally ();
			TestCatchesHaveSeparateState ();
			FinallyWithBranchToFirstBlock ();
			FinallyWithBranchToFirstBlockAndEnclosingTryCatchState ();
			CatchWithBranchToFirstBlock ();
			CatchWithBranchToFirstBlockAndReassignment ();
			CatchWithNonSimplePredecessor ();
			FinallyWithNonSimplePredecessor ();
			FinallyInTryWithPredecessor ();
			NestedFinally ();
			ChangeInFinallyNestedInFinally ();
			NestedFinallyWithPredecessor ();
			ExceptionFilter ();
			ExceptionFilterStateChange ();
			ExceptionMultipleFilters ();
			ExceptionFilterWithBranch ();
			ExceptionFilterWithException ();
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void TryFlowsToFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			} finally {
				// methods/fields/properties
				RequireAll (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void TryFlowsToAfterFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			} finally {
				// prevent optimizing this away
				_ = string.Empty;
			}
			// properties
			RequireAll (t);
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicConstructors) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void MultipleTryExits ()
		{
			Type t = GetWithPublicConstructors ();
			for (int i = 0; i < 10; i++) {
				try {
					if (string.Empty.Length == 0) {
						t = GetWithPublicMethods ();
						return;
					}
					if (string.Empty.Length == 1) {
						t = GetWithPublicFields ();
						continue;
					}
					if (string.Empty.Length == 2) {
						t = GetWithPublicProperties ();
						break;
					}
				} finally {
					RequireAll (t);
				}
			}
		}

		// There are multiple paths through the finally to different subsequent blocks.
		// On each path, only one state is possible, but we conservatively merge the (non-exceptional)
		// finally states for each path and expect the warnings to reflect this merged state.
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicEvents) + "()",
			ProducedBy = Tool.Analyzer)]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicEvents) + "()")]

		[ExpectedWarning ("IL2073", nameof (MultipleFinallyPaths) + "()", nameof (GetWithPublicEvents) + "()")]

		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2073", nameof (MultipleFinallyPaths) + "()", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2073", nameof (MultipleFinallyPaths) + "()", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2073", nameof (MultipleFinallyPaths) + "()", nameof (GetWithPublicProperties) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		public static Type MultipleFinallyPaths ()
		{
			Type t = GetWithPublicMethods (); // reaches RequireAll1 and RequireAll2
			while (true) {
				RequireAll1 (t);
				try {
					if (string.Empty.Length == 1) {
						t = GetWithPublicFields (); // reaches RequireAll1 and RequireAll2
						continue;
					}
					if (string.Empty.Length == 0) {
						t = GetWithPublicProperties (); // reaches RequireAll2 only, but the finally mergig means
														// the analysis thinks it can reach RequireAll1.
						break;
					}
					if (string.Empty.Length == 2) {
						t = GetWithPublicEvents (); // reaches return only, but the finally merging means
													// the analysis thinks it can reach RequireAll1 (and hence RequireAll2).
						return t;
					}
				} finally {
					_ = string.Empty;
				}
			}
			RequireAll2 (t); // properties

			throw new Exception ();
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void FinallyChain ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				try {
					t = GetWithPublicProperties ();
				} finally {
					RequireAll1 (t); // fields/properties
				}
			} finally {
				RequireAll2 (t); // methods/fields/properties
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void FinallyChainWithPostFinallyState ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				try {
					t = GetWithPublicProperties ();
				} finally {
					// normal: properties
					// exception: fields/properties
					RequireAll1 (t); // fields/properties
				}
				RequireAll2 (t); // properties
			} finally {
				// normal: properties
				// exception: methods/fields/properties
				RequireAll3 (t); // methods/fields/properties
			}
			RequireAll4 (t);
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void TryFlowsToCatch ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			} catch {
				// methods/fields/properties
				RequireAll (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void CatchFlowsToFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			} finally {
				// methods/fields/properties
				RequireAll (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void CatchFlowsToAfterTry ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			}
			// methods/properties, not fields
			RequireAll (t);
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void CatchFlowsToAfterFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			} finally { }
			// methods/properties, not fields
			RequireAll (t);
		}


		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void FinallyFlowsToAfterFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} finally {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			}
			// properties only
			RequireAll (t);
		}

		public class Exception1 : Exception { }
		public class Exception2 : Exception { }


		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll5) + "(Type)", nameof (GetWithPublicEvents) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicEvents) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll7) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicProperties) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll5) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll5) + "(Type)", nameof (GetWithPublicProperties) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll7) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll7) + "(Type)", nameof (GetWithPublicProperties) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll7) + "(Type)", nameof (GetWithPublicEvents) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicEvents) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]

		public static void TryFlowsToMultipleCatchAndFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				RequireAll1 (t); // fields only
			} catch (Exception1) {
				RequireAll2 (t); // methods/fields
				t = GetWithPublicProperties ();
				RequireAll3 (t); // properties only
			} catch (Exception2) {
				RequireAll4 (t); // methods/fields
				t = GetWithPublicEvents ();
				RequireAll5 (t); // events only
			} finally {
				RequireAll6 (t); // methods/fields/properties/events
				t = GetWithPublicConstructors ();
				RequireAll7 (t); // ctors only
			}
			RequireAll (t);
		}


		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicEvents) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		public static void NestedWithFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				try {
					// fields
					t = GetWithPublicProperties ();
				} finally {
					// fields/properties
					RequireAll1 (t);
					t = GetWithPublicEvents ();
					t = GetWithPublicConstructors ();
				}
				// ctors
				RequireAll2 (t);
			} finally {
				// methods/fields/properties/events/constructors
				RequireAll3 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicEvents) + "()")]
		public static void ControlFlowsOutOfMultipleFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				try {
					try {
						t = GetWithPublicFields ();
					} finally {
						// methods/fields
						RequireAll1 (t);
						t = GetWithPublicProperties ();
					}
				} finally {
					// methods/fields/properties
					RequireAll2 (t);
					t = GetWithPublicEvents ();
				}
			} finally {
				// methods/fields/propreties/events
				RequireAll3 (t);
			}
		}


		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicEvents) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		public static void NestedWithCatch ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				try {
					// fields
					t = GetWithPublicProperties ();
				} catch {
					// fields/properties
					RequireAll1 (t);
					t = GetWithPublicEvents ();
					t = GetWithPublicConstructors ();
				}
				// properties/ctors
				RequireAll2 (t);
			} catch {
				// methods/fields/properties/events/constructors
				RequireAll3 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void CatchInTry ()
		{
			try {
				Type t = GetWithPublicMethods ();
				try {
				} catch {
					t = GetWithPublicFields ();
					RequireAll (t);
				}
			} catch {
			}
		}

		// This tests a case where the catch state was being merged with the containing try state incorrectly.
		// In the bug, the exceptional catch state, which is used in the finally, had too much in it.
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		// The bug was producing this warning:
		// [ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void CatchInTryWithFinally ()
		{
			Type t = GetWithPublicConstructors ();
			try {
				t = GetWithPublicMethods ();
				// methods
				// ex: ctors/methods
				try {
					// methods
					// ex: methods
				} catch {
					// methods
					t = GetWithPublicFields ();
					// fields
					// ex: methods/fields
					RequireAll1 (t);
				} finally {
					// normal state: fields
					// exceptional state: methods/fields
					RequireAll2 (t);
				}
			} catch {
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicConstructors) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicConstructors) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		static void CatchInFinally () {
			Type t = GetWithPublicConstructors ();
			try {
				t = GetWithPublicMethods ();
			} finally {
				try {
					t = GetWithPublicFields ();
				} catch {
					RequireAll1 (t);
				}
				RequireAll2(t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]

		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void TestCatchesHaveSeparateState ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch (Exception1) {
				t = GetWithPublicFields ();
			} catch (Exception2) {
				// methods only!
				RequireAll (t);
			} finally {
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		public static void FinallyWithBranchToFirstBlock ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} finally {
			FinallyStart:
				RequireAll (t);
				t = GetWithPublicFields ();
				goto FinallyStart;
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		public static void FinallyWithBranchToFirstBlockAndEnclosingTryCatchState ()
		{
			try {
				Type t = GetWithPublicProperties ();
				t = GetWithPublicMethods ();
				try {
				} finally {
				FinallyStart:
					// methods/fields
					RequireAll (t);
					t = GetWithPublicFields ();
					goto FinallyStart;
				}
			} finally {
				// An operation just to prevent optimizing away
				// the try/finally.
				_ = String.Empty;
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		public static void CatchWithBranchToFirstBlock ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch {
			CatchStart:
				RequireAll (t);
				t = GetWithPublicFields ();
				goto CatchStart;
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		public static void CatchWithBranchToFirstBlockAndReassignment ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch {
			CatchStart:
				RequireAll (t); // methods/fields, but not properties!
				t = GetWithPublicProperties ();
				t = GetWithPublicFields ();
				goto CatchStart;
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void CatchWithNonSimplePredecessor ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
				try {
					// properties only
				} catch {
					// properties only.
					RequireAll1 (t);
				}
			} catch {
				// methods/fields/properties
				RequireAll2 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void FinallyWithNonSimplePredecessor ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
				try {
					// properties only
				} catch {
					// properties only.
					RequireAll1 (t);
				}
			} finally {
				// methods/fields/properties
				RequireAll2 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void FinallyInTryWithPredecessor ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
				try {
					// properties only
				} finally {
					// properties only.
					RequireAll1 (t);
				}
			} finally {
				// methods/fields/properties
				RequireAll2 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void NestedFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
			} finally {
				try {
					RequireAll1 (t);
					t = GetWithPublicProperties ();
				} finally {
					RequireAll2 (t);
				}
				RequireAll3 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicFields) + "()")]

		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void ChangeInFinallyNestedInFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				RequireAll1 (t);
			} finally {
				try {
					RequireAll2 (t);
				} finally {
					t = GetWithPublicFields ();
				}
				RequireAll3 (t); // fields only
			}
			RequireAll4 (t); // fields only
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void NestedFinallyWithPredecessor ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
			} finally {
				_ = 0; // add an operation so that the try isn't the start of the finally.
				try {
					RequireAll1 (t);
					t = GetWithPublicProperties ();
				} finally {
					RequireAll2 (t);
				}
				RequireAll3 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAllTrue) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAllTrue) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Analyzer)]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		public static void ExceptionFilter ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
			} catch (Exception) when (RequireAllTrue (t)) {
				RequireAll (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAllTrue) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]

		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void ExceptionFilterStateChange ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch (Exception) when (RequireAllTrue (t = GetWithPublicFields ())) {
				RequireAll (t);
			} finally {
				RequireAll2 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAllFalse) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAllTrue) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAllFalse2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAllFalse2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAllFalse2) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll5) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll5) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll5) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()",
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void ExceptionMultipleFilters ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch (Exception) when (RequireAllFalse (t = GetWithPublicFields ())) {
				RequireAll (t);
			} catch (Exception) when (RequireAllTrue (t = GetWithPublicProperties ())) {
				RequireAll2 (t);
			} catch (Exception1) {
				RequireAll3 (t);
			} catch (Exception) when (RequireAllFalse2 (t)) {
				RequireAll4 (t);
			} catch {
				RequireAll5 (t);
			}
			RequireAll6 (t);
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields))]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties))]

		[ExpectedWarning ("IL2072", nameof (RequireAllTrue) + "(Type)", nameof (GetWithPublicMethods))]
		[ExpectedWarning ("IL2072", nameof (RequireAllTrue) + "(Type)", nameof (GetWithPublicFields))]
		[ExpectedWarning ("IL2072", nameof (RequireAllTrue) + "(Type)", nameof (GetWithPublicProperties))]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods))]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields))]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties))]

		// Trimmer merges branches going forward.
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods),
			ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		public static void ExceptionFilterWithBranch ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch (Exception) when (string.Empty.Length == 0 ? (t = GetWithPublicFields ()) == null : (t = GetWithPublicProperties ()) == null) {
				RequireAll (t);
			} catch (Exception) when (RequireAllTrue (t)) {
			} catch {
				RequireAll2 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods))]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields))]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties))]

		[ExpectedWarning ("IL2072", nameof (RequireAllTrue) + "(Type)", nameof (GetWithPublicMethods))]
		[ExpectedWarning ("IL2072", nameof (RequireAllTrue) + "(Type)", nameof (GetWithPublicFields))]
		[ExpectedWarning ("IL2072", nameof (RequireAllTrue) + "(Type)", nameof (GetWithPublicProperties))]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods))]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields))]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties))]
		public static void ExceptionFilterWithException ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch (Exception) when ((t = GetWithPublicFields ()) != null
				? (t = GetWithPublicProperties ()) == null
				: (t = GetWithPublicProperties ()) == null) {
			} catch (Exception1) {
				// An exception thrown from the above filter could result in methods, fields, or properties here,
				// even though a non-exceptional exit from the filter always leaves t with 'properties'.
				RequireAll (t);
			} catch (Exception2) when (RequireAllTrue (t)) {
				// Same as above, with a filter.
				RequireAll2 (t);
			}
		}

		public static bool RequireAllTrue (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
			return true;
		}

		public static bool RequireAllFalse (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
			return true;
		}

		public static bool RequireAllFalse2 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
			return true;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		public static Type GetWithPublicMethods ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		public static Type GetWithPublicFields ()
		{
			return null;
		}
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
		public static Type GetWithPublicProperties ()
		{
			return null;
		}
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
		public static Type GetWithPublicEvents ()
		{
			return null;
		}
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		public static Type GetWithPublicConstructors ()
		{
			return null;
		}

		public static void RequireAll (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll1 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll2 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll3 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll4 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll5 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll6 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll7 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
	}
}
