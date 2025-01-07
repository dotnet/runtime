using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]

	// Using Kept validation on compiler generated code is tricky as we would have to describe
	// all of the compiler generated classes and members which are expected to be kept.
	// So not using that here (at least until we come up with a better way to do this).
	// Instead this test relies on RUC and warnings to detect "kept" and "removed" calls.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class CompilerGeneratedCodeSubstitutions
	{
		static void Main ()
		{
			Lambda.Test ();
			LocalFunction.Test ();
			Iterator.Test ();
			Async.Test ();
		}

		class Lambda
		{
			[ExpectedWarning ("IL2026", "--UsedMethod--", CompilerGeneratedCode = true)]
			static void TestBranchInLambda ()
			{
				var a = () => {
					if (AlwaysFalse) {
						RemovedMethod ();
					} else {
						UsedMethod ();
					}
				};

				a ();
			}

			[ExpectedWarning ("IL2026", "--UsedMethod--", CompilerGeneratedCode = true)]
			static void TestBranchAroundLambda ()
			{
				Action a;
				if (AlwaysFalse) {
					a = () => RemovedMethod ();
				} else {
					a = () => UsedMethod ();
				}

				a ();
			}

			public static void Test ()
			{
				TestBranchInLambda ();
				TestBranchAroundLambda ();
			}
		}

		class LocalFunction
		{
			[ExpectedWarning ("IL2026", "--UsedMethod--", CompilerGeneratedCode = true)]
			static void TestBranchInLocalFunction ()
			{
				void LocalFunction ()
				{
					if (AlwaysFalse) {
						RemovedMethod ();
					} else {
						UsedMethod ();
					}
				}

				LocalFunction ();
			}

			[ExpectedWarning ("IL2026", "--UsedMethod--", CompilerGeneratedCode = true)]
			static void TestBranchAroundLocalFunction ()
			{
				Action a;
				if (AlwaysFalse) {
					void RemovedLocalFunction ()
					{
						RemovedMethod ();
					}

					RemovedLocalFunction ();
				} else {
					void UsedLocalFunction ()
					{
						UsedMethod ();
					}

					UsedLocalFunction ();
				}
			}

			[ExpectedWarning ("IL2026", "--UsedMethod--", CompilerGeneratedCode = true)]
			static void TestBranchAroundUsageOfLocalFunction ()
			{
				Action a;
				if (AlwaysFalse) {
					RemovedLocalFunction ();
				} else {
					UsedLocalFunction ();
				}

				void RemovedLocalFunction ()
				{
					RemovedMethod ();
				}

				void UsedLocalFunction ()
				{
					UsedMethod ();
				}
			}

			public static void Test ()
			{
				TestBranchInLocalFunction ();
				TestBranchAroundLocalFunction ();
				TestBranchAroundUsageOfLocalFunction ();
			}
		}

		class Iterator
		{
			[ExpectedWarning ("IL2026", "--UsedMethod--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestBranchWithNormalCall ()
			{
				if (AlwaysFalse) {
					RemovedMethod ();
				} else {
					UsedMethod ();
				}

				yield return 1;
			}

			[ExpectedWarning ("IL2026", "--UsedMethod--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestBranchWithYieldAfter ()
			{
				if (AlwaysFalse) {
					RemovedMethod ();
					yield return 1;
				} else {
					UsedMethod ();
					yield return 1;
				}

				yield return 1;
			}

			[ExpectedWarning ("IL2026", "--UsedMethod--", CompilerGeneratedCode = true)]
			[UnexpectedWarning ("IL2026", "--RemovedMethod--", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/linker/issues/3087", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestBranchWithYieldBefore ()
			{
				if (AlwaysFalse) {
					yield return 1;
					RemovedMethod ();
				} else {
					yield return 1;
					UsedMethod ();
				}

				yield return 1;
			}

			public static void Test ()
			{
				// Use the IEnumerable to mark the IEnumerable methods
				foreach (var _ in TestBranchWithNormalCall ()) ;
				TestBranchWithYieldAfter ();
				TestBranchWithYieldBefore ();
			}
		}

		class Async
		{
			[ExpectedWarning ("IL2026", "--UsedMethod--", CompilerGeneratedCode = true)]
			static async Task TestBranchWithNormalCall ()
			{
				if (AlwaysFalse) {
					RemovedMethod ();
				} else {
					UsedMethod ();
				}

				await Task.FromResult (0);
			}

			[ExpectedWarning ("IL2026", "--UsedMethod--", CompilerGeneratedCode = true)]
			[UnexpectedWarning ("IL2026", "--RemovedMethod--", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/linker/issues/3087", CompilerGeneratedCode = true)]
			static async Task TestBranchWithNormalCallAfterWAwait ()
			{
				if (AlwaysFalse) {
					await Task.FromResult (0);
					RemovedMethod ();
				} else {
					await Task.FromResult (0);
					UsedMethod ();
				}

				await Task.FromResult (0);
			}

			[ExpectedWarning ("IL2026", "--UsedAsyncMethod--", CompilerGeneratedCode = true)]
			static async Task TestBranchWithAsyncCall ()
			{
				if (AlwaysFalse) {
					await RemovedAsyncMethod ();
				} else {
					await UsedAsyncMethod ();
				}
			}

			public static void Test ()
			{
				TestBranchWithNormalCall ().RunSynchronously (); ;
				TestBranchWithNormalCallAfterWAwait ().RunSynchronously ();
				TestBranchWithAsyncCall ().RunSynchronously ();
			}
		}

		static bool AlwaysFalse => false;

		[RequiresUnreferencedCode ("--UsedAsyncMethod--")]
		static async Task UsedAsyncMethod () => await Task.FromResult (0);

		[RequiresUnreferencedCode ("--RemovedAsyncMethod--")]
		static async Task RemovedAsyncMethod () => await Task.FromResult (-1);

		[RequiresUnreferencedCode ("--UsedMethod--")]
		static void UsedMethod () { }

		[RequiresUnreferencedCode ("--RemovedMethod--")]
		static void RemovedMethod () { }
	}
}
