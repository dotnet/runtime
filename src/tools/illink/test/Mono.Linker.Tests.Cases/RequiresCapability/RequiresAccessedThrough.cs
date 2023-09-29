// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
			AccessThroughDelegate.Test ();
			AccessThroughUnsafeAccessor.Test ();
		}

		class TestType { }

		[RequiresUnreferencedCode ("Message for --RequiresOnlyThroughReflection--")]
		[RequiresDynamicCode ("Message for --RequiresOnlyThroughReflection--")]
		[RequiresAssemblyFiles ("Message for --RequiresOnlyThroughReflection--")]
		static void RequiresOnlyThroughReflection ()
		{
		}

		// https://github.com/dotnet/linker/issues/2739 - the discussion there explains why (at least for now) we don't produce
		// RAF and RDC warnings from the analyzer in these cases.
		[ExpectedWarning ("IL2026", "--RequiresOnlyThroughReflection--")]
		[ExpectedWarning ("IL3002", "--RequiresOnlyThroughReflection--", ProducedBy = Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--RequiresOnlyThroughReflection--", ProducedBy = Tool.NativeAot)]
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
			[ExpectedWarning ("IL3002", "--GenericType.RequiresOnlyThroughReflection--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--GenericType.RequiresOnlyThroughReflection--", ProducedBy = Tool.NativeAot)]
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
			[ExpectedWarning ("IL2026", "--PInvokeReturnType.ctor--", ProducedBy = Tool.Trimmer)]
			[DllImport ("nonexistent")]
			static extern PInvokeReturnType PInvokeReturnsType ();

			// Analyzer doesn't support IL2050 yet
			[ExpectedWarning ("IL2050", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--")]
			[ExpectedWarning ("IL2026", "--NewConstraintTestAnnotatedType--")]
			[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--NewConstraintTestType.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
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

			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--")]
			[ExpectedWarning ("IL2026", "--NewConstraintTestAnnotatedType--")]
			[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--NewConstraintTestType.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static void TestNewConstraintOnTypeParameter<T> () where T : new()
			{
				_ = new NewConstraintOnTypeParameter<NewConstraintTestType> ();
				_ = new NewConstraintOnTypeParameter<NewConstraintTestAnnotatedType> ();
				_ = new NewConstraintOnTypeParameter<T> (); // should not crash analyzer
			}

			[ExpectedWarning ("IL2026", "--AnnotatedMethod--")]
			[ExpectedWarning ("IL3002", "--AnnotatedMethod--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--AnnotatedMethod--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
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
				[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				[ExpectedWarning ("IL3050", "--NewConstraintTestType.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
				public static void Method ()
				{
					_ = new NewConstraintOnTypeParameter<NewConstraintTestType> ();
					_ = new NewConstraintOnTypeParameter<NewConstraintTestAnnotatedType> ();
				}
			}

			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--")]
			[ExpectedWarning ("IL2026", "--NewConstraintTestAnnotatedType--")]
			[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--NewConstraintTestType.ctor--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
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

			[ExpectedWarning ("IL2026", "--PropertyWithLdToken.get--")]
			[ExpectedWarning ("IL2026", "--PropertyWithLdToken.get--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", "--PropertyWithLdToken.get--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", "--PropertyWithLdToken.get--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--PropertyWithLdToken.get--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--PropertyWithLdToken.get--", ProducedBy = Tool.NativeAot)]
			static void TestPropertyLdToken ()
			{
				Expression<Func<bool>> getter = () => PropertyWithLdToken;
			}

			[RequiresUnreferencedCode ("Message for --MethodWithLdToken--")]
			[RequiresAssemblyFiles ("Message for --MethodWithLdToken--")]
			[RequiresDynamicCode ("Message for --MethodWithLdToken--")]
			static void MethodWithLdToken ()
			{
			}

			[ExpectedWarning ("IL2026", "--MethodWithLdToken--")]
			[ExpectedWarning ("IL3002", "--MethodWithLdToken--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithLdToken--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestMethodLdToken ()
			{
				Expression<Action> e = () => MethodWithLdToken ();
			}

			[RequiresUnreferencedCode ("--FieldWithLdToken--")]
			[RequiresDynamicCode ("--FieldWithLdToken--")]
			class FieldWithLdTokenType
			{
				public static int Field = 0;
			}

			[ExpectedWarning ("IL2026", "--FieldWithLdToken--")]
			[ExpectedWarning ("IL3050", "--FieldWithLdToken--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestFieldLdToken ()
			{
				Expression<Func<int>> f = () => FieldWithLdTokenType.Field;
			}

			public static void Test ()
			{
				TestPropertyLdToken ();
				TestMethodLdToken ();
				TestFieldLdToken ();
			}
		}

		class AccessThroughDelegate
		{
			[RequiresUnreferencedCode ("Message for --MethodWithDelegate--")]
			[RequiresAssemblyFiles ("Message for --MethodWithDelegate--")]
			[RequiresDynamicCode ("Message for --MethodWithDelegate--")]
			static void MethodWithDelegate ()
			{
			}

			[ExpectedWarning ("IL2026", "--MethodWithDelegate--")]
			[ExpectedWarning ("IL3002", "--MethodWithDelegate--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithDelegate--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestMethodWithDelegate ()
			{
				Action a = MethodWithDelegate;
			}

			[ExpectedWarning ("IL2026", "--LambdaThroughDelegate--")]
			[ExpectedWarning ("IL3002", "--LambdaThroughDelegate--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--LambdaThroughDelegate--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void LambdaThroughDelegate ()
			{
				Action a =
				[RequiresUnreferencedCode ("--LambdaThroughDelegate--")]
				[RequiresAssemblyFiles ("--LambdaThroughDelegate--")]
				[RequiresDynamicCode ("--LambdaThroughDelegate--")]
				() => { };

				a ();
			}

			[ExpectedWarning ("IL2026", "--LocalFunctionThroughDelegate--")]
			[ExpectedWarning ("IL3002", "--LocalFunctionThroughDelegate--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--LocalFunctionThroughDelegate--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void LocalFunctionThroughDelegate ()
			{
				Action a = Local;

				[RequiresUnreferencedCode ("--LocalFunctionThroughDelegate--")]
				[RequiresAssemblyFiles ("--LocalFunctionThroughDelegate--")]
				[RequiresDynamicCode ("--LocalFunctionThroughDelegate--")]
				void Local ()
				{ }
			}

			public static void Test ()
			{
				TestMethodWithDelegate ();
				LambdaThroughDelegate ();
				LocalFunctionThroughDelegate ();
			}
		}

		class AccessThroughUnsafeAccessor
		{
			// Analyzer has no support for UnsafeAccessor right now

			class Target
			{
				[RequiresUnreferencedCode ("--Target..ctor--")]
				[RequiresAssemblyFiles ("--Target..ctor--")]
				[RequiresDynamicCode ("--Target..ctor--")]
				private Target (int i) { }

				[RequiresUnreferencedCode ("--Target.MethodRequires--")]
				[RequiresAssemblyFiles ("--Target.MethodRequires--")]
				[RequiresDynamicCode ("--Target.MethodRequires--")]
				private static void MethodRequires () { }
			}

			[ExpectedWarning ("IL2026", "--Target.MethodRequires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", "--Target.MethodRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--Target.MethodRequires--", ProducedBy = Tool.NativeAot)]
			[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
			extern static void MethodRequires (Target target);

			[ExpectedWarning ("IL2026", "--Target..ctor--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", "--Target..ctor--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--Target..ctor--", ProducedBy = Tool.NativeAot)]
			[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
			extern static Target Constructor (int i);

			[RequiresUnreferencedCode ("--TargetWitRequires--")]
			class TargetWithRequires
			{
				private TargetWithRequires () { }

				private static void StaticMethod () { }

				private void InstanceMethod () { }

				private static int StaticField;

				private int InstanceField;
			}

			[ExpectedWarning ("IL2026", "--TargetWitRequires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
			extern static TargetWithRequires TargetRequiresConstructor ();

			[ExpectedWarning ("IL2026", "--TargetWitRequires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "StaticMethod")]
			extern static void TargetRequiresStaticMethod (TargetWithRequires target);

			// For trimmer this is a reflection access to an instance method - and as such it must warn (since it's in theory possible
			// to invoke the method via reflection on a null instance)
			// For NativeAOT this is a direct call to an instance method (there's no reflection involved) and as such it doesn't need to warn
			[ExpectedWarning ("IL2026", "--TargetWitRequires--", ProducedBy = Tool.Trimmer)]
			[UnsafeAccessor (UnsafeAccessorKind.Method, Name = "InstanceMethod")]
			extern static void TargetRequiresInstanceMethod (TargetWithRequires target);

			[ExpectedWarning ("IL2026", "--TargetWitRequires--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[UnsafeAccessor (UnsafeAccessorKind.StaticField, Name = "StaticField")]
			extern static ref int TargetRequiresStaticField (TargetWithRequires target);

			// Access to instance fields never produces these warnings due to RUC on type
			[UnsafeAccessor (UnsafeAccessorKind.Field, Name = "InstanceField")]
			extern static ref int TargetRequiresInstanceField (TargetWithRequires target);

			public static void Test ()
			{
				MethodRequires (null);
				Constructor (0);

				TargetRequiresConstructor ();

				TargetWithRequires targetWithRequires = TargetRequiresConstructor ();
				TargetRequiresStaticMethod (targetWithRequires);
				TargetRequiresInstanceMethod (targetWithRequires);
				TargetRequiresStaticField (targetWithRequires);
				TargetRequiresInstanceField (targetWithRequires);
			}
		}
	}
}
