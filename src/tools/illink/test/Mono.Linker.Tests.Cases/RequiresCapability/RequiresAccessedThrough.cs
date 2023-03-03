// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RequiresAccessedThrough
	{
		public static void Main ()
		{
			TestRequiresOnlyThroughReflection ();
			AccessedThroughReflectionOnGenericType<TestType>.Test ();
			AccessThroughSpecialAttribute.Test ();
			AccessThroughPInvoke.Test ();
			AccessThroughNewConstraint.Test<TestType> ();
			AccessThroughNewConstraint.TestNewConstraintOnTypeParameter<TestType> ();
			AccessThroughNewConstraint.TestNewConstraintOnTypeParameterOfStaticType<TestType> ();
			AccessThroughNewConstraint.TestNewConstraintOnTypeParameterInAnnotatedMethod ();
			AccessThroughNewConstraint.TestNewConstraintOnTypeParameterInAnnotatedType ();
			AccessThroughLdToken.Test ();
		}

		class TestType { }

		[RequiresUnreferencedCode ("Message for --RequiresOnlyThroughReflection--")]
		[RequiresDynamicCode ("Message for --RequiresOnlyThroughReflection--")]
		[RequiresAssemblyFiles ("Message for --RequiresOnlyThroughReflection--")]
		static void RequiresOnlyThroughReflection ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresOnlyThroughReflection--")]
		static void TestRequiresOnlyThroughReflection ()
		{
			typeof (RequiresAccessedThrough)
				.GetMethod (nameof (RequiresOnlyThroughReflection), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		class AccessedThroughReflectionOnGenericType<T>
		{
			[RequiresUnreferencedCode ("Message for --GenericType.RequiresOnlyThroughReflection--")]
			[RequiresDynamicCode ("Message for --GenericType.RequiresOnlyThroughReflection--")]
			[RequiresAssemblyFiles ("Message for --GenericType.RequiresOnlyThroughReflection--")]
			public static void RequiresOnlyThroughReflection ()
			{
			}

			[ExpectedWarning ("IL2026", "--GenericType.RequiresOnlyThroughReflection--")]
			public static void Test ()
			{
				typeof (AccessedThroughReflectionOnGenericType<T>)
					.GetMethod (nameof (RequiresOnlyThroughReflection))
					.Invoke (null, new object[0]);
			}
		}

		class AccessThroughSpecialAttribute
		{
			// https://github.com/dotnet/linker/issues/1873
			// [ExpectedWarning ("IL2026", "--DebuggerProxyType.Method--")]
			[DebuggerDisplay ("Some{*}value")]
			class TypeWithDebuggerDisplay
			{
				[RequiresUnreferencedCode ("Message for --DebuggerProxyType.Method--")]
				[RequiresDynamicCode ("Message for --DebuggerProxyType.Method--")]
				[RequiresAssemblyFiles ("Message for --DebuggerProxyType.Method--")]
				public void Method ()
				{
				}
			}

			public static void Test ()
			{
				var _ = new TypeWithDebuggerDisplay ();
			}
		}

		class AccessThroughPInvoke
		{
			class PInvokeReturnType
			{
				[RequiresUnreferencedCode ("Message for --PInvokeReturnType.ctor--")]
				[RequiresDynamicCode ("Message for --PInvokeReturnType.ctor--")]
				[RequiresAssemblyFiles ("Message for --PInvokeReturnType.ctor--")]
				public PInvokeReturnType () { }
			}

			// https://github.com/mono/linker/issues/2116
			[ExpectedWarning ("IL2026", "--PInvokeReturnType.ctor--", ProducedBy = ProducedBy.Trimmer)]
			[DllImport ("nonexistent")]
			static extern PInvokeReturnType PInvokeReturnsType ();

			// Analyzer doesn't support IL2050 yet
			[ExpectedWarning ("IL2050", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
			public static void Test ()
			{
				PInvokeReturnsType ();
			}
		}

		class AccessThroughNewConstraint
		{
			class NewConstraintTestType
			{
				[RequiresUnreferencedCode ("Message for --NewConstraintTestType.ctor--")]
				[RequiresAssemblyFiles ("Message for --NewConstraintTestType.ctor--")]
				[RequiresDynamicCode ("Message for --NewConstraintTestType.ctor--")]
				public NewConstraintTestType () { }
			}


			[RequiresUnreferencedCode ("Message for --NewConstraintTestAnnotatedType--")]
			class NewConstraintTestAnnotatedType
			{
			}

			static void GenericMethod<T> () where T : new() { }

			// NativeAOT doesnt generate warnings when marking generic constraints
			// https://github.com/dotnet/runtime/issues/68688
			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer | ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "--NewConstraintTestAnnotatedType--", ProducedBy = ProducedBy.Analyzer | ProducedBy.Trimmer)]
			[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
			public static void Test<T> () where T : new()
			{
				GenericMethod<NewConstraintTestType> ();
				GenericMethod<NewConstraintTestAnnotatedType> ();
				GenericMethod<T> (); // should not crash analyzer
			}

			static class NewConstraintOnTypeParameterOfStaticType<T> where T : new()
			{
				public static void DoNothing () { }
			}

			class NewConstraintOnTypeParameter<T> where T : new()
			{
			}

			// NativeAOT doesnt generate warnings when marking generic constraints
			// https://github.com/dotnet/runtime/issues/68688
			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer | ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "--NewConstraintTestAnnotatedType--", ProducedBy = ProducedBy.Analyzer | ProducedBy.Trimmer)]
			[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
			public static void TestNewConstraintOnTypeParameter<T> () where T : new()
			{
				_ = new NewConstraintOnTypeParameter<NewConstraintTestType> ();
				_ = new NewConstraintOnTypeParameter<NewConstraintTestAnnotatedType> ();
				_ = new NewConstraintOnTypeParameter<T> (); // should not crash analyzer
			}

			[ExpectedWarning ("IL2026", "--AnnotatedMethod--")]
			[ExpectedWarning ("IL3002", "--AnnotatedMethod--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3050", "--AnnotatedMethod--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			public static void TestNewConstraintOnTypeParameterInAnnotatedMethod ()
			{
				AnnotatedMethod ();
			}

			[RequiresUnreferencedCode ("--AnnotatedMethod--")]
			[RequiresAssemblyFiles ("--AnnotatedMethod--")]
			[RequiresDynamicCode ("--AnnotatedMethod--")]
			static void AnnotatedMethod ()
			{
				_ = new NewConstraintOnTypeParameter<NewConstraintTestType> ();
				_ = new NewConstraintOnTypeParameter<NewConstraintTestAnnotatedType> ();
			}

			[ExpectedWarning ("IL2026", "--AnnotatedType--")]
			public static void TestNewConstraintOnTypeParameterInAnnotatedType ()
			{
				AnnotatedType.Method ();
			}

			[RequiresUnreferencedCode ("--AnnotatedType--")]
			class AnnotatedType
			{
				[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
				public static void Method ()
				{
					_ = new NewConstraintOnTypeParameter<NewConstraintTestType> ();
					_ = new NewConstraintOnTypeParameter<NewConstraintTestAnnotatedType> ();
				}
			}

			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer | ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "--NewConstraintTestAnnotatedType--", ProducedBy = ProducedBy.Analyzer | ProducedBy.Trimmer)]
			[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
			public static void TestNewConstraintOnTypeParameterOfStaticType<T> () where T : new()
			{
				NewConstraintOnTypeParameterOfStaticType<NewConstraintTestType>.DoNothing ();
				NewConstraintOnTypeParameterOfStaticType<NewConstraintTestAnnotatedType>.DoNothing ();
				NewConstraintOnTypeParameterOfStaticType<T>.DoNothing (); // should not crash analyzer
			}
		}

		class AccessThroughLdToken
		{
			static bool PropertyWithLdToken {
				[RequiresUnreferencedCode ("Message for --PropertyWithLdToken.get--")]
				[RequiresAssemblyFiles ("Message for --PropertyWithLdToken.get--")]
				[RequiresDynamicCode ("Message for --PropertyWithLdToken.get--")]
				get {
					return false;
				}
			}

			// NativeAOT should produce diagnostics when using Func
			// https://github.com/dotnet/runtime/issues/73321
			[ExpectedWarning ("IL2026", "--PropertyWithLdToken.get--")]
			[ExpectedWarning ("IL2026", "--PropertyWithLdToken.get--", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL3002", "--PropertyWithLdToken.get--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--PropertyWithLdToken.get--", ProducedBy = ProducedBy.Analyzer)]
			public static void Test ()
			{
				Expression<Func<bool>> getter = () => PropertyWithLdToken;
			}
		}
	}
}
