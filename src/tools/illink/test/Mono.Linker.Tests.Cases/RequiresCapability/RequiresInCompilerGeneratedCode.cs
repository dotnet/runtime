// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class RequiresInCompilerGeneratedCode
	{
		public static void Main ()
		{
			WarnInIteratorBody.Test ();
			SuppressInIteratorBody.Test ();

			WarnInAsyncBody.Test ();
			SuppressInAsyncBody.Test ();

			WarnInAsyncIteratorBody.Test ();
			SuppressInAsyncIteratorBody.Test ();

			WarnInLocalFunction.Test ();
			SuppressInLocalFunction.Test ();
			WarnInNonNestedLocalFunctionTest ();
			SuppressInNonNestedLocalFunctionTest ();

			WarnInLambda.Test ();
			SuppressInLambda.Test ();

			WarnInComplex.Test ();
			SuppressInComplex.Test ();

			StateMachinesOnlyReferencedViaReflection.Test ();
			LocalFunctionsReferencedViaReflection.Test ();
			LambdasReferencedViaReflection.Test ();

			ComplexCases.AsyncBodyCallingMethodWithRequires.Test ();
			ComplexCases.GenericAsyncBodyCallingMethodWithRequires.Test ();
			ComplexCases.GenericAsyncEnumerableBodyCallingRequiresWithAnnotations.Test ();

			RUCOnDelegateCacheFields.Test ();
		}

		class WarnInIteratorBody
		{
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			static IEnumerable<int> TestCallBeforeYieldReturn ()
			{
				MethodWithRequires ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			static IEnumerable<int> TestCallAfterYieldReturn ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			static IEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 1;
			}

#if !RELEASE
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
#else
			// In release mode, the compiler optimizes away the unused Action (and reference to MethodWithRequires)
#endif
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
			static IEnumerable<int> TestLdftn ()
			{
				yield return 0;
				yield return 1;
				var action = new Action (MethodWithRequires);
			}

			// Cannot annotate fields either with RUC nor RAF therefore the warning persists
			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static IEnumerable<int> TestLazyDelegate ()
			{
				yield return 0;
				yield return 1;
				_ = _default.Value;
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				yield return 0;
				yield return 1;
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInIteratorBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestCall ()
			{
				MethodWithRequires ();
				yield return 0;
				MethodWithRequires ();
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestLdftn ()
			{
				yield return 0;
				yield return 1;
				var action = new Action (MethodWithRequires);
			}

			// Cannot annotate fields either with RUC nor RAF therefore the warning persists
			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static IEnumerable<int> TestLazyDelegate ()
			{
				yield return 0;
				yield return 1;
				_ = _default.Value;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				yield return 0;
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestGenericMethodParameterRequirement<TUnknown> ()
			{
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInAsyncBody
		{
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			static async void TestCallBeforeYieldReturn ()
			{
				MethodWithRequires ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			static async void TestCallAfterYieldReturn ()
			{
				await MethodAsync ();
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			static async void TestReflectionAccess ()
			{
				await MethodAsync ();
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

#if !RELEASE
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
#endif
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
			static async void TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (MethodWithRequires);
			}

			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static async void TestLazyDelegate ()
			{
				await MethodAsync ();
				_ = _default.Value;
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInAsyncBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestCall ()
			{
				MethodWithRequires ();
				await MethodAsync ();
				MethodWithRequires ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestReflectionAccess ()
			{
				await MethodAsync ();
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (MethodWithRequires);
			}

			// Cannot annotate fields either with RUC nor RAF therefore the warning persists
			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static async void TestLazyDelegate ()
			{
				await MethodAsync ();
				_ = _default.Value;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInAsyncIteratorBody
		{
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			static async IAsyncEnumerable<int> TestCallBeforeYieldReturn ()
			{
				await MethodAsync ();
				MethodWithRequires ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			static async IAsyncEnumerable<int> TestCallAfterYieldReturn ()
			{
				yield return 0;
				MethodWithRequires ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			static async IAsyncEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				await MethodAsync ();
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
				yield return 1;
			}

#if !RELEASE
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Trimmer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
#endif
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
			static async IAsyncEnumerable<int> TestLdftn ()
			{
				await MethodAsync ();
				yield return 0;
				var action = new Action (MethodWithRequires);
			}

			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static async IAsyncEnumerable<int> TestLazyDelegate ()
			{
				await MethodAsync ();
				yield return 0;
				_ = _default.Value;
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = Tool.NativeAot)]
			static async IAsyncEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				yield return 0;
				await MethodAsync ();
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInAsyncIteratorBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestCall ()
			{
				MethodWithRequires ();
				await MethodAsync ();
				yield return 0;
				MethodWithRequires ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestReflectionAccess ()
			{
				await MethodAsync ();
				yield return 0;
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (MethodWithRequires);
				yield return 0;
			}

			// Cannot annotate fields either with RUC nor RAF therefore the warning persists
			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static async IAsyncEnumerable<int> TestLazyDelegate ()
			{
				await MethodAsync ();
				yield return 0;
				_ = _default.Value;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				yield return 0;
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				await MethodAsync ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestGenericMethodParameterRequirement<TUnknown> ()
			{
				yield return 0;
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				yield return 0;
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInLocalFunction
		{
			static void TestCall ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				void LocalFunction () => MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--LocalFunctionWithRequires--")]
			[ExpectedWarning ("IL3002", "--LocalFunctionWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--LocalFunctionWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestLocalFunctionWithRequires ()
			{
				LocalFunction ();

				[RequiresUnreferencedCode ("--LocalFunctionWithRequires--")]
				[RequiresAssemblyFiles ("--LocalFunctionWithRequires--")]
				[RequiresDynamicCode ("--LocalFunctionWithRequires--")]
				void LocalFunction () => MethodWithRequires ();
			}

			static void TestCallUnused ()
			{
				// No warning for code in unused local functions.
				void LocalFunction () => MethodWithRequires ();
			}

			static void TestCallWithClosure (int p = 0)
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				void LocalFunction ()
				{
					p++;
					MethodWithRequires ();
				}
			}

			static void TestCallWithClosureUnused (int p = 0)
			{
				// No warning for code in unused local functions.
				void LocalFunction ()
				{
					p++;
					MethodWithRequires ();
				}
			}

			static void TestReflectionAccess ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				void LocalFunction () => typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			static void TestLdftn ()
			{
				LocalFunction ();

#if !RELEASE
				[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
#endif
				[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
				void LocalFunction ()
				{
					var action = new Action (MethodWithRequires);
				}
			}

			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static void TestLazyDelegate ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					_ = _default.Value;
				}
			}

			static void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--TypeWithMethodWithRequires.MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--TypeWithMethodWithRequires.MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				void LocalFunction () => typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
			}

			public static void Test ()
			{
				TestCall ();
				TestLocalFunctionWithRequires ();
				TestCallUnused ();
				TestCallWithClosure ();
				TestCallWithClosureUnused ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInLocalFunction
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCall ()
			{
				LocalFunction ();

				void LocalFunction () => MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallFromNestedLocalFunction ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					NestedLocalFunction ();

					void NestedLocalFunction () => MethodWithRequires ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallWithClosure (int p = 0)
			{
				LocalFunction ();

				void LocalFunction ()
				{
					p++;
					MethodWithRequires ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestReflectionAccess ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestLdftn ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					var action = new Action (MethodWithRequires);
				}
			}

			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static void TestLazyDelegate ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					_ = _default.Value;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				LocalFunction ();

				void LocalFunction () => unknownType.RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				void LocalFunction () => MethodWithGenericWhichRequiresMethods<TUnknown> ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				void LocalFunction () => new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericLocalFunction<TUnknown> ()
			{
				LocalFunction<TUnknown> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				{
					typeof (T).RequiresPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericLocalFunctionInner<TUnknown> ()
			{
				LocalFunction<TUnknown> ();

				void LocalFunction<TSecond> ()
				{
					typeof (TUnknown).RequiresPublicMethods ();
					typeof (TSecond).RequiresPublicMethods ();
				}
			}

			static void TestGenericLocalFunctionWithAnnotations<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
			{
				LocalFunction<TPublicMethods> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TInnerPublicMethods> ()
				{
					typeof (TPublicMethods).RequiresPublicMethods ();
					typeof (TInnerPublicMethods).RequiresPublicMethods ();
				}
			}

			static void TestGenericLocalFunctionWithAnnotationsAndClosure<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> (int p = 0)
			{
				LocalFunction<TPublicMethods> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TInnerPublicMethods> ()
				{
					p++;
					typeof (TPublicMethods).RequiresPublicMethods ();
					typeof (TInnerPublicMethods).RequiresPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallMethodWithRequiresInLtftnLocalFunction ()
			{
				var _ = new Action (LocalFunction);

				void LocalFunction () => MethodWithRequires ();
			}

			class DynamicallyAccessedLocalFunction
			{
				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				public static void TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ()
				{
					typeof (DynamicallyAccessedLocalFunction).RequiresNonPublicMethods ();

					LocalFunction ();

					void LocalFunction () => MethodWithRequires ();
				}
			}

			class DynamicallyAccessedLocalFunctionUnusedShouldWarn
			{
				[ExpectedWarning ("IL2118", nameof (TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction), "LocalFunction", ProducedBy = Tool.Trimmer)]
				public static void TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ()
				{
					typeof (DynamicallyAccessedLocalFunctionUnusedShouldWarn).RequiresNonPublicMethods ();

					// ILLink isn't able to figure out which user method this local function
					// belongs to, but it doesn't warn because it is only accessed via reflection.
					// Instead this warns on the reflection access.
					// No warning for code in unused local functions.
					void LocalFunction () => MethodWithRequires ();
				}
			}

			class DynamicallyAccessedLocalFunctionUnusedShouldSuppress
			{
				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				public static void TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ()
				{
					typeof (DynamicallyAccessedLocalFunctionUnusedShouldSuppress).RequiresNonPublicMethods ();

					// ILLink isn't able to figure out which user method this local function
					// belongs to, but it doesn't warn because it is only accessed via reflection,
					// in a RUC scope.
					void LocalFunction () => MethodWithRequires ();
				}
			}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestSuppressionOnLocalFunction ()
			{
				LocalFunction (); // This will produce a warning since the local function has Requires on it

				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				void LocalFunction (Type unknownType = null)
				{
					MethodWithRequires ();
					unknownType.RequiresNonPublicMethods ();
				}
			}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestSuppressionOnLocalFunctionWithAssignment ()
			{
				LocalFunction (); // This will produce a warning since the local function has Requires on it

				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				void LocalFunction (Type unknownType = null)
				{
					MethodWithRequires ();
					typeWithNonPublicMethods = unknownType;
				}
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			static Type typeWithNonPublicMethods;

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestSuppressionOnLocalFunctionWithNestedLocalFunction ()
			{
				LocalFunction (); // This will produce a warning since the local function has Requires on it

				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				void LocalFunction ()
				{
					NestedLocalFunction ();

					// ILLink doesn't have enough information to associate the Requires on LocalFunction
					// with this nested local function.
					[ExpectedWarning ("IL2026", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
					[ExpectedWarning ("IL3002", ProducedBy = Tool.NativeAot)]
					[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)]
					void NestedLocalFunction () => MethodWithRequires ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestSuppressionOnOuterAndLocalFunction ()
			{
				LocalFunction ();

				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				void LocalFunction (Type unknownType = null)
				{
					MethodWithRequires ();
					unknownType.RequiresNonPublicMethods ();
				}
			}

			class TestSuppressionOnOuterWithSameName
			{
				[ExpectedWarning ("IL2026", nameof (Outer) + "()")]
				[ExpectedWarning ("IL3002", nameof (Outer) + "()", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", nameof (Outer) + "()", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				public static void Test ()
				{
					Outer ();
					Outer (0);
				}

				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				static void Outer ()
				{
					// Even though this method has the same name as Outer(int i),
					// it should not suppress warnings originating from compiler-generated
					// code for the lambda contained in Outer(int i).
				}

				static void Outer (int i)
				{
					LocalFunction ();

					[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
					[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					void LocalFunction () => MethodWithRequires ();
				}
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestCall ();
				TestCallFromNestedLocalFunction ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestMethodParameterWithRequirements ();
				TestDynamicallyAccessedMethod ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
				TestGenericLocalFunction<TestType> ();
				TestGenericLocalFunctionInner<TestType> ();
				TestGenericLocalFunctionWithAnnotations<TestType> ();
				TestGenericLocalFunctionWithAnnotationsAndClosure<TestType> ();
				TestCallMethodWithRequiresInLtftnLocalFunction ();
				DynamicallyAccessedLocalFunction.TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ();
				DynamicallyAccessedLocalFunctionUnusedShouldWarn.TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ();
				DynamicallyAccessedLocalFunctionUnusedShouldSuppress.TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ();
				TestSuppressionOnLocalFunction ();
				TestSuppressionOnLocalFunctionWithAssignment ();
				TestSuppressionOnLocalFunctionWithNestedLocalFunction ();
				TestSuppressionOnOuterAndLocalFunction ();
				TestSuppressionOnOuterWithSameName.Test ();
			}
		}

		static void WarnInNonNestedLocalFunctionTest ()
		{
			LocalFunction ();

			[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void LocalFunction () => MethodWithRequires ();
		}

		[ExpectedWarning ("IL2026", "--MethodWithNonNestedLocalFunction--")]
		[ExpectedWarning ("IL3002", "--MethodWithNonNestedLocalFunction--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--MethodWithNonNestedLocalFunction--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		static void SuppressInNonNestedLocalFunctionTest ()
		{
			MethodWithNonNestedLocalFunction ();
		}

		[RequiresUnreferencedCode ("--MethodWithNonNestedLocalFunction--")]
		[RequiresAssemblyFiles ("--MethodWithNonNestedLocalFunction--")]
		[RequiresDynamicCode ("--MethodWithNonNestedLocalFunction--")]
		static void MethodWithNonNestedLocalFunction ()
		{
			LocalFunction ();

			static void LocalFunction () => MethodWithRequires ();
		}

		class WarnInLambda
		{
			static void TestCall ()
			{
				Action lambda =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				() => MethodWithRequires ();

				lambda ();
			}

			[ExpectedWarning ("IL2026", "--LambdaWithRequires--")]
			[ExpectedWarning ("IL3002", "--LambdaWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--LambdaWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestLambdaWithRequires ()
			{
				Action lambda =
					[RequiresUnreferencedCodeAttribute ("--LambdaWithRequires--")]
				[RequiresAssemblyFiles ("--LambdaWithRequires--")]
				[RequiresDynamicCode ("--LambdaWithRequires--")]
				() => MethodWithRequires ();

				lambda ();
			}

			static void TestCallUnused ()
			{
				Action _ =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				() => MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--LambdaWithRequires--")]
			[ExpectedWarning ("IL3002", "--LambdaWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--LambdaWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestLambdaWithRequiresUnused ()
			{
				Action _ =
					[RequiresUnreferencedCode ("--LambdaWithRequires--")]
				[RequiresAssemblyFiles ("--LambdaWithRequires--")]
				[RequiresDynamicCode ("--LambdaWithRequires--")]
				() => MethodWithRequires ();
			}

			static void TestCallWithClosure (int p = 0)
			{
				Action lambda =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				() => {
					p++;
					MethodWithRequires ();
				};

				lambda ();
			}

			static void TestCallWithClosureUnused (int p = 0)
			{
				Action _ =
#if !RELEASE
				[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
#endif
				[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
				() => {
					p++;
					MethodWithRequires ();
				};
			}

			static void TestReflectionAccess ()
			{
				Action _ =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				() => {
					typeof (RequiresInCompilerGeneratedCode)
						.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
						.Invoke (null, new object[] { });
				};
			}

			static void TestLdftn ()
			{
				Action _ =
#if !RELEASE
				[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
#endif
				[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer)]
				() => {
					var action = new Action (MethodWithRequires);
				};
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static void TestLazyDelegate ()
			{
				Action _ = () => {
					var action = _default.Value;
				};
			}

			static void TestDynamicallyAccessedMethod ()
			{
				Action _ =
				[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--TypeWithMethodWithRequires.MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--TypeWithMethodWithRequires.MethodWithRequires--", ProducedBy = Tool.NativeAot)]
				() => {
					typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				};
			}

			public static void Test ()
			{
				TestCall ();
				TestCallUnused ();
				TestLambdaWithRequires ();
				TestLambdaWithRequiresUnused ();
				TestCallWithClosure ();
				TestCallWithClosureUnused ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInLambda
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCall ()
			{
				Action _ =
				() => MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallFromNestedLambda ()
			{
				Action lambda = () => {
					Action nestedLambda = () => MethodWithRequires ();
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallWithReflectionAnalysisWarning ()
			{
				// This should not produce warning because the Requires
				Action<Type> _ =
				(t) => t.RequiresPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallWithClosure (int p = 0)
			{
				Action _ =
				() => {
					p++;
					MethodWithRequires ();
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestReflectionAccess ()
			{
				Action _ =
				() => {
					typeof (RequiresInCompilerGeneratedCode)
						.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
						.Invoke (null, new object[] { });
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestLdftn ()
			{
				Action _ =
				() => {
					var action = new Action (MethodWithRequires);
				};
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequiresAndReturns--", CompilerGeneratedCode = true, ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static void TestLazyDelegate ()
			{
				Action _ = () => {
					var action = _default.Value;
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestDynamicallyAccessedMethod ()
			{
				Action _ =
				() => {
					typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				Action _ =
				() => unknownType.RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				Action _ =
				() => {
					MethodWithGenericWhichRequiresMethods<TUnknown> ();
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				Action _ = () => {
					new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				};
			}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestSuppressionOnLambda ()
			{
				var lambda =
				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				() => MethodWithRequires ();

				lambda (); // This will produce a warning since the lambda has Requires on it
			}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestSuppressionOnLambdaWithNestedLambda ()
			{
				var lambda =
				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				() => {
					// ILLink doesn't try to associate the Requires on lambda with this nested
					// lambda. It would be possible to do this because the declaration site will contain
					// an IL reference to the generated lambda method, unlike local functions.
					// However, we don't make this association, for consistency with local functions.
					var nestedLambda =
					[ExpectedWarning ("IL2026", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
					[ExpectedWarning ("IL3002", ProducedBy = Tool.NativeAot | Tool.NativeAot)]
					[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot | Tool.NativeAot)]
					() => MethodWithRequires ();
				};

				lambda (); // This will produce a warning since the lambda has Requires on it
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestSuppressionOnOuterAndLambda ()
			{
				var lambda =
				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				(Type unknownType) => {
					MethodWithRequires ();
					unknownType.RequiresNonPublicMethods ();
				};

				lambda (null);
			}

			class TestSuppressionOnOuterWithSameName
			{
				[ExpectedWarning ("IL2026", nameof (Outer) + "()")]
				[ExpectedWarning ("IL3002", nameof (Outer) + "()", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", nameof (Outer) + "()", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				public static void Test ()
				{
					Outer ();
					Outer (0);
				}

				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				static void Outer ()
				{
					// Even though this method has the same name as Outer(int i),
					// it should not suppress warnings originating from compiler-generated
					// code for the lambda contained in Outer(int i).
				}

				static void Outer (int i)
				{
					var lambda =
					[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
					[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					() => MethodWithRequires ();

					lambda ();
				}
			}


			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestCall ();
				TestCallFromNestedLambda ();
				TestCallWithReflectionAnalysisWarning ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
				TestSuppressionOnLambda ();
				TestSuppressionOnLambdaWithNestedLambda ();
				TestSuppressionOnOuterAndLambda ();
				TestSuppressionOnOuterWithSameName.Test ();
			}
		}

		class WarnInComplex
		{
			static async void TestIteratorLocalFunctionInAsync ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

#if !NATIVEAOT
			// BUG: https://github.com/dotnet/runtime/issues/77455
			// NativeAot produces an incorrect warning pointing to the InstantiatedMethod, which isn't associated
			// with the calling context.
			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				yield return 1;
				MethodWithGenericWhichRequiresMethods<TypeWithMethodWithRequires> ();
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericTypeParameterInIterator ()
			{
				yield return 1;
				new TypeWithGenericWhichRequiresMethods<TypeWithMethodWithRequires> ();
			}
#endif

			static void TestLocalFunctionInIteratorLocalFunction ()
			{
				IteratorLocalFunction ();

				IEnumerable<int> IteratorLocalFunction ()
				{
					yield return 0;
					LocalFunction ();
					yield return 1;

					[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
					void LocalFunction () => MethodWithRequires ();
				}
			}

			static void TestLocalFunctionCalledFromIteratorLocalFunctionAndMethod ()
			{
				IteratorLocalFunction ();

				IEnumerable<int> IteratorLocalFunction ()
				{
					yield return 0;
					LocalFunction ();
					yield return 1;
				}

				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				void LocalFunction () => MethodWithRequires ();
			}

			public static void Test ()
			{
				TestIteratorLocalFunctionInAsync ();
#if !NATIVEAOT
				TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ();
				TestDynamicallyAccessedMethodViaGenericTypeParameterInIterator ();
#endif
				TestLocalFunctionInIteratorLocalFunction ();
				TestLocalFunctionCalledFromIteratorLocalFunctionAndMethod ();
			}
		}

		class SuppressInComplex
		{

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestIteratorLocalFunction ()
			{
				LocalFunction ();

				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestAsyncLocalFunction ()
			{
				LocalFunction ();

				async Task<int> LocalFunction ()
				{
					await MethodAsync ();
					MethodWithRequires ();
					return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestIteratorLocalFunctionWithClosure (int p = 0)
			{
				LocalFunction ();

				IEnumerable<int> LocalFunction ()
				{
					p++;
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestAsyncLocalFunctionWithClosure (int p = 0)
			{
				LocalFunction ();

				async Task<int> LocalFunction ()
				{
					p++;
					await MethodAsync ();
					MethodWithRequires ();
					return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallToLocalFunctionInIteratorLocalFunctionWithClosure (int p = 0)
			{
				LocalFunction ();

				IEnumerable<int> LocalFunction ()
				{
					p++;
					yield return 0;
					LocalFunction2 ();
					yield return 1;

					void LocalFunction2 ()
					{
						MethodWithRequires ();
					}
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestAsyncLambda ()
			{
				Func<Task<int>> _ = async Task<int> () => {
					await MethodAsync ();
					MethodWithRequires ();
					return 1;
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestAsyncLambdaWithClosure (int p = 0)
			{
				Func<Task<int>> _ = async Task<int> () => {
					p++;
					await MethodAsync ();
					MethodWithRequires ();
					return 1;
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestLambdaInAsyncLambdaWithClosure (int p = 0)
			{
				Func<Task<int>> _ = async Task<int> () => {
					p++;
					await MethodAsync ();
					var lambda = () => MethodWithRequires ();
					return 1;
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestIteratorLocalFunctionInAsync ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				[RequiresUnreferencedCode ("Suppress in local function")]
				[RequiresAssemblyFiles ("Suppress in local function")]
				[RequiresDynamicCode ("Suppress in local function")]
				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestIteratorLocalFunctionInAsyncWithoutInner ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

#if !NATIVEAOT
			// BUG: https://github.com/dotnet/runtime/issues/77455
			// NativeAot produces an incorrect warning pointing to the InstantiatedMethod, which isn't associated
			// with the calling context.
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				MethodWithGenericWhichRequiresMethods<TypeWithMethodWithRequires> ();
				yield return 0;
			}
#endif

			[ExpectedWarning ("IL2026", "--IteratorLocalFunction--")]
			[ExpectedWarning ("IL3002", "--IteratorLocalFunction--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--IteratorLocalFunction--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestLocalFunctionInIteratorLocalFunction ()
			{
				IteratorLocalFunction ();

				[RequiresUnreferencedCode ("--IteratorLocalFunction--")]
				[RequiresAssemblyFiles ("--IteratorLocalFunction--")]
				[RequiresDynamicCode ("--IteratorLocalFunction--")]
				IEnumerable<int> IteratorLocalFunction ()
				{
					yield return 0;
					LocalFunction ();
					MethodWithRequires ();
					yield return 1;

					// Trimmer doesn't try to associate LocalFunction with IteratorLocalFunction
					[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.NativeAot)]
					void LocalFunction () => MethodWithRequires ();
				}
			}

			[ExpectedWarning ("IL2026", "--IteratorLocalFunction--")]
			[ExpectedWarning ("IL3002", "--IteratorLocalFunction--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--IteratorLocalFunction--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestLocalFunctionCalledFromIteratorLocalFunctionAndMethod ()
			{
				IteratorLocalFunction ();
				LocalFunction ();

				[RequiresUnreferencedCode ("--IteratorLocalFunction--")]
				[RequiresAssemblyFiles ("--IteratorLocalFunction--")]
				[RequiresDynamicCode ("--IteratorLocalFunction--")]
				IEnumerable<int> IteratorLocalFunction ()
				{
					yield return 0;
					LocalFunction ();
					MethodWithRequires ();
					yield return 1;
				}

				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				void LocalFunction () => MethodWithRequires ();
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestIteratorLocalFunction ();
				TestAsyncLocalFunction ();
				TestIteratorLocalFunctionWithClosure ();
				TestAsyncLocalFunctionWithClosure ();
				TestCallToLocalFunctionInIteratorLocalFunctionWithClosure ();
				TestAsyncLambda ();
				TestAsyncLambdaWithClosure ();
				TestLambdaInAsyncLambdaWithClosure ();
				TestIteratorLocalFunctionInAsync ();
				TestIteratorLocalFunctionInAsyncWithoutInner ();
#if !NATIVEAOT
				TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ();
#endif
				TestLocalFunctionInIteratorLocalFunction ();
				TestLocalFunctionCalledFromIteratorLocalFunctionAndMethod ();
			}
		}

		class StateMachinesOnlyReferencedViaReflection
		{
			[RequiresUnreferencedCode ("--TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress--")]
			[RequiresAssemblyFiles ("--TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress--")]
			[RequiresDynamicCode ("--TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress--")]
			static IEnumerable<int> TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("--TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress--")]
			[RequiresAssemblyFiles ("--TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress--")]
			[RequiresDynamicCode ("--TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress--")]
			static async void TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress ()
			{
				await MethodAsync ();
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			static IEnumerable<int> TestIteratorOnlyReferencedViaReflectionWhichShouldWarn ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.Analyzer | Tool.NativeAot, CompilerGeneratedCode = true)]
			static async void TestAsyncOnlyReferencedViaReflectionWhichShouldWarn ()
			{
				await MethodAsync ();
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress--")]
			[ExpectedWarning ("IL3002", "--TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress--")]
			[ExpectedWarning ("IL3002", "--TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2118", nameof (StateMachinesOnlyReferencedViaReflection), "<" + nameof (TestAsyncOnlyReferencedViaReflectionWhichShouldWarn) + ">", "MoveNext()",
				ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (StateMachinesOnlyReferencedViaReflection), "<" + nameof (TestIteratorOnlyReferencedViaReflectionWhichShouldWarn) + ">", "MoveNext()",
				ProducedBy = Tool.Trimmer)]
			static void TestAll ()
			{
				typeof (StateMachinesOnlyReferencedViaReflection).RequiresAll ();
			}

			[ExpectedWarning ("IL2026", "--TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress--")]
			[ExpectedWarning ("IL3002", "--TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress--")]
			[ExpectedWarning ("IL3002", "--TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress--", ProducedBy = Tool.NativeAot)]
			// NonPublicMethods doesn't warn for members emitted into compiler-generated state machine types.
			static void TestNonPublicMethods ()
			{
				typeof (StateMachinesOnlyReferencedViaReflection).RequiresNonPublicMethods ();
			}

			public static void Test ()
			{
				TestAll ();
				TestNonPublicMethods ();
			}
		}

		class LocalFunctionsReferencedViaReflection
		{
			[ExpectedWarning ("IL2026", "--TestLocalFunctionWithRequires--")]
			[ExpectedWarning ("IL3002", "--TestLocalFunctionWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLocalFunctionWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestLocalFunctionWithRequires ()
			{
				LocalFunction ();

				[RequiresUnreferencedCode ("--TestLocalFunctionWithRequires--")]
				[RequiresAssemblyFiles ("--TestLocalFunctionWithRequires--")]
				[RequiresDynamicCode ("--TestLocalFunctionWithRequires--")]
				void LocalFunction () => MethodWithRequires ();
			}

			static void TestLocalFunctionWithRequiresOnlyAccessedViaReflection ()
			{
				[RequiresUnreferencedCode ("--TestLocalFunctionWithRequiresOnlyAccessedViaReflection--")]
				[RequiresAssemblyFiles ("--TestLocalFunctionWithRequiresOnlyAccessedViaReflection--")]
				[RequiresDynamicCode ("--TestLocalFunctionWithRequiresOnlyAccessedViaReflection--")]
				void LocalFunction () => MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "LocalFunction")]
			[ExpectedWarning ("IL3002", "LocalFunction", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "LocalFunction", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestLocalFunctionWithClosureWithRequires (int p = 0)
			{
				LocalFunction ();

				[RequiresUnreferencedCode ("--TestLocalFunctionWithClosureWithRequires--")]
				[RequiresAssemblyFiles ("--TestLocalFunctionWithClosureWithRequires--")]
				[RequiresDynamicCode ("--TestLocalFunctionWithClosureWithRequires--")]
				void LocalFunction ()
				{
					p++;
					MethodWithRequires ();
				}
			}

			[RequiresUnreferencedCode ("--TestLocalFunctionInMethodWithRequires--")]
			[RequiresAssemblyFiles ("--TestLocalFunctionInMethodWithRequires--")]
			[RequiresDynamicCode ("--TestLocalFunctionInMethodWithRequires--")]
			static void TestLocalFunctionInMethodWithRequires ()
			{
				LocalFunction ();

				void LocalFunction () => MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("--TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection--")]
			[RequiresAssemblyFiles ("--TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection--")]
			[RequiresDynamicCode ("--TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection--")]
			static void TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection ()
			{
				// This is unused, except for the reflection access. Don't warn about MethodWithRequires
				// because the reflection access will warn about accessing compiler-generated code.
				void LocalFunction () => MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("--TestLocalFunctionWithClosureInMethodWithRequires--")]
			[RequiresAssemblyFiles ("--TestLocalFunctionWithClosureInMethodWithRequires--")]
			[RequiresDynamicCode ("--TestLocalFunctionWithClosureInMethodWithRequires--")]
			static void TestLocalFunctionWithClosureInMethodWithRequires (int p = 0)
			{
				LocalFunction ();

				void LocalFunction ()
				{
					p++;
					MethodWithRequires ();
				}
			}

			// Warnings for Reflection access to methods with Requires
			[ExpectedWarning ("IL2026", "--TestLocalFunctionInMethodWithRequires--")]
			[ExpectedWarning ("IL3002", "--TestLocalFunctionInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLocalFunctionInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--TestLocalFunctionWithClosureInMethodWithRequires--")]
			[ExpectedWarning ("IL3002", "--TestLocalFunctionWithClosureInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLocalFunctionWithClosureInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection--")]
			[ExpectedWarning ("IL3002", "--TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionsReferencedViaReflection), nameof (TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection),
				ProducedBy = Tool.Trimmer)]
			static void TestAll ()
			{
				typeof (LocalFunctionsReferencedViaReflection).RequiresAll ();
			}

			// Warnings for Reflection access to methods with Requires
			[ExpectedWarning ("IL2026", "--TestLocalFunctionInMethodWithRequires--")]
			[ExpectedWarning ("IL3002", "--TestLocalFunctionInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLocalFunctionInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--TestLocalFunctionWithClosureInMethodWithRequires--")]
			[ExpectedWarning ("IL3002", "--TestLocalFunctionWithClosureInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLocalFunctionWithClosureInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection--")]
			[ExpectedWarning ("IL3002", "--TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionsReferencedViaReflection), "<" + nameof (TestLocalFunctionInMethodWithRequiresOnlyAccessedViaReflection) + ">",
				ProducedBy = Tool.Trimmer)]
			static void TestNonPublicMethods ()
			{
				typeof (LocalFunctionsReferencedViaReflection).RequiresNonPublicMethods ();
			}

			public static void Test ()
			{
				TestAll ();
				TestNonPublicMethods ();
			}
		}

		class LambdasReferencedViaReflection
		{
			[ExpectedWarning ("IL2026", "--TestLambdaWithRequires--")]
			[ExpectedWarning ("IL3002", "--TestLambdaWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLambdaWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestLambdaWithRequires ()
			{
				var lambda =
				[RequiresUnreferencedCode ("--TestLambdaWithRequires--")]
				[RequiresAssemblyFiles ("--TestLambdaWithRequires--")]
				[RequiresDynamicCode ("--TestLambdaWithRequires--")]
				() => MethodWithRequires ();

				lambda ();
			}

			[ExpectedWarning ("IL2026", "Lambda")]
			[ExpectedWarning ("IL3002", "Lambda", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "Lambda", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestLambdaWithClosureWithRequires (int p = 0)
			{
				var lambda =
				[RequiresUnreferencedCode ("--TestLambdaWithClosureWithRequires--")]
				[RequiresAssemblyFiles ("--TestLambdaWithClosureWithRequires--")]
				[RequiresDynamicCode ("--TestLambdaWithClosureWithRequires--")]
				() => {
					p++;
					MethodWithRequires ();
				};

				lambda ();
			}

			[RequiresUnreferencedCode ("--TestLambdaInMethodWithRequires--")]
			[RequiresAssemblyFiles ("--TestLambdaInMethodWithRequires--")]
			[RequiresDynamicCode ("--TestLambdaInMethodWithRequires--")]
			static void TestLambdaInMethodWithRequires ()
			{
				var lambda = () => MethodWithRequires ();

				lambda ();
			}

			[RequiresUnreferencedCode ("--TestLambdaWithClosureInMethodWithRequires--")]
			[RequiresAssemblyFiles ("--TestLambdaWithClosureInMethodWithRequires--")]
			[RequiresDynamicCode ("--TestLambdaWithClosureInMethodWithRequires--")]
			static void TestLambdaWithClosureInMethodWithRequires (int p = 0)
			{
				var lambda = () => {
					p++;
					MethodWithRequires ();
				};

				lambda ();
			}

			// Warnings for Reflection access to methods with Requires
			[ExpectedWarning ("IL2026", "--TestLambdaInMethodWithRequires--")]
			[ExpectedWarning ("IL3002", "--TestLambdaInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLambdaInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--TestLambdaWithClosureInMethodWithRequires--")]
			[ExpectedWarning ("IL3002", "--TestLambdaWithClosureInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLambdaWithClosureInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			static void TestAll ()
			{
				typeof (LambdasReferencedViaReflection).RequiresAll ();
			}

			// Warnings for Reflection access to methods with Requires
			[ExpectedWarning ("IL2026", "--TestLambdaInMethodWithRequires--")]
			[ExpectedWarning ("IL3002", "--TestLambdaInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLambdaInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--TestLambdaWithClosureInMethodWithRequires--")]
			[ExpectedWarning ("IL3002", "--TestLambdaWithClosureInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--TestLambdaWithClosureInMethodWithRequires--", ProducedBy = Tool.NativeAot)]
			static void TestNonPublicMethods ()
			{
				typeof (LambdasReferencedViaReflection).RequiresNonPublicMethods ();
			}

			public static void Test ()
			{
				TestAll ();
				TestNonPublicMethods ();
			}
		}

		class ComplexCases
		{
			public class AsyncBodyCallingMethodWithRequires
			{
				[RequiresUnreferencedCode ("")]
				[RequiresAssemblyFiles ("")]
				[RequiresDynamicCode ("")]
				static Task<object> MethodWithRequiresAsync (Type type)
				{
					return Task.FromResult (new object ());
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				[RequiresAssemblyFiles ("ParentSuppression")]
				[RequiresDynamicCode ("ParentSuppression")]
				static async Task<object> AsyncMethodCallingRequires (Type type)
				{
					using (var diposable = await GetDisposableAsync ()) {
						return await MethodWithRequiresAsync (type);
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				[ExpectedWarning ("IL3002", "ParentSuppression", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "ParentSuppression", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				public static void Test ()
				{
					AsyncMethodCallingRequires (typeof (object));
				}
			}

			public class GenericAsyncBodyCallingMethodWithRequires
			{
				[RequiresUnreferencedCode ("")]
				[RequiresAssemblyFiles ("")]
				[RequiresDynamicCode ("")]
				static ValueTask<TValue> MethodWithRequiresAsync<TValue> ()
				{
					return ValueTask.FromResult (default (TValue));
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				[RequiresAssemblyFiles ("ParentSuppression")]
				[RequiresDynamicCode ("ParentSuppression")]
				static async Task<T> AsyncMethodCallingRequires<T> ()
				{
					using (var disposable = await GetDisposableAsync ()) {
						return await MethodWithRequiresAsync<T> ();
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				[ExpectedWarning ("IL3002", "ParentSuppression", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "ParentSuppression", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				public static void Test ()
				{
					AsyncMethodCallingRequires<object> ();
				}
			}

			public class GenericAsyncEnumerableBodyCallingRequiresWithAnnotations
			{
				class RequiresOnCtor
				{
					[RequiresUnreferencedCode ("")]
					[RequiresAssemblyFiles ("")]
					[RequiresDynamicCode ("")]
					public RequiresOnCtor ()
					{
					}
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				[RequiresAssemblyFiles ("ParentSuppression")]
				[RequiresDynamicCode ("ParentSuppression")]
				static IAsyncEnumerable<TValue> AsyncEnumMethodCallingRequires<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TValue> ()
				{
					return CreateAsync ();

					[RequiresUnreferencedCode ("")]
					[RequiresAssemblyFiles]
					[RequiresDynamicCode ("")]
					static async IAsyncEnumerable<TValue> CreateAsync ()
					{
						await MethodAsync ();
						new RequiresOnCtor ();
						yield return default (TValue);
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				[ExpectedWarning ("IL3002", "ParentSuppression", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "ParentSuppression", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				public static void Test ()
				{
					AsyncEnumMethodCallingRequires<object> ();
				}
			}

			class Disposable : IDisposable { public void Dispose () { } }

			static Task<Disposable> GetDisposableAsync () { return Task.FromResult (new Disposable ()); }
		}

		class RUCOnDelegateCacheFields
		{
			[RequiresUnreferencedCode ("--RUCOnDelegateCacheFields.TargetType--")]
			class TargetType
			{
				public static void Init ()
				{
					var Action = () => { };
				}
			}

			[ExpectedWarning ("IL2026", nameof (TargetType) + "()")] // .ctor
			[ExpectedWarning ("IL2026", nameof (TargetType.Init) + "()")] // Init method
			public static void Test ()
			{
				typeof (TargetType).RequiresAll ();
			}
		}

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}

		[RequiresUnreferencedCode ("--MethodWithRequires--")]
		[RequiresAssemblyFiles ("--MethodWithRequires--")]
		[RequiresDynamicCode ("--MethodWithRequires--")]
		static void MethodWithRequires ()
		{
		}

		class TypeWithMethodWithRequires
		{
			[RequiresUnreferencedCode ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			[RequiresAssemblyFiles ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			[RequiresDynamicCode ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			static void MethodWithRequires ()
			{
			}
		}

		[RequiresUnreferencedCode ("Message from --MethodWithRequiresAndReturns--")]
		[RequiresAssemblyFiles ("Message from --MethodWithRequiresAndReturns--")]
		[RequiresDynamicCode ("Message from --MethodWithRequiresAndReturns--")]
		public static string MethodWithRequiresAndReturns ()
		{
			return "string";
		}

		static void MethodWithGenericWhichRequiresMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] T> ()
		{
		}

		class TypeWithGenericWhichRequiresMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] T>
		{
		}

		class TypeWithGenericWhichRequiresNonPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)] T> { }

		class TestType { }
	}
}
