// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCompileArgument ("/unsafe")]
	[ExpectedNoWarnings]
	unsafe class UnsafeAccessor
	{
		public static void Main ()
		{
			ConstructorAccess.Test ();
			StaticMethodAccess.Test ();
			InstanceMethodAccess.Test ();
		}

		class ConstructorAccess
		{
			[Kept]
			class DefaultConstructor
			{
				[Kept]
				class DefaultConstructorTarget
				{
					[Kept]
					private DefaultConstructorTarget () { }

					private DefaultConstructorTarget (int i) { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
				extern static DefaultConstructorTarget InvokeDefaultConstructor ();

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				// This should not resolve since Name is not allowed for Constructor
				[UnsafeAccessor (UnsafeAccessorKind.Constructor, Name = ".ctor")]
				extern static DefaultConstructorTarget InvokeWithName (int i);

				[Kept]
				class UseLocalFunction
				{
					[Kept]
					private UseLocalFunction () { }

					[Kept]
					public static void Test ()
					{
						InvokeDefaultConstructorLocal ();

						[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
						extern static UseLocalFunction InvokeDefaultConstructorLocal ();
					}
				}

				[Kept]
				class AccessCtorAsMethod
				{
					[Kept]
					private AccessCtorAsMethod () { }

					[Kept]
					[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
					[UnsafeAccessor (UnsafeAccessorKind.Method, Name = ".ctor")]
					extern static void CallPrivateConstructor (AccessCtorAsMethod _this);

					[Kept]
					public static void Test ()
					{
						var instance = (AccessCtorAsMethod)RuntimeHelpers.GetUninitializedObject(typeof(AccessCtorAsMethod));
						CallPrivateConstructor (instance);
					}
				}

				[Kept]
				public static void Test ()
				{
					InvokeDefaultConstructor ();
					InvokeWithName (0);
					UseLocalFunction.Test ();
					AccessCtorAsMethod.Test ();
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			class ConstructorWithParameter
			{
				[Kept]
				class ConstructorWithParameterTarget
				{
					[Kept (By = Tool.NativeAot)] // BUG https://github.com/dotnet/runtime/issues/87881
					private ConstructorWithParameterTarget () { }

					[Kept]
					private ConstructorWithParameterTarget (int i) { }

					[Kept]
					protected ConstructorWithParameterTarget (string s) { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
				extern static ConstructorWithParameterTarget InvokeConstructorWithParameter (int i);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Constructor, Name = "")]
				extern static ConstructorWithParameterTarget InvokeWithEmptyName (string s);

				// Validate that instance methods are ignored
				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
				extern ConstructorWithParameterTarget InvokeOnInstance ();

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				// Test that invoking non-existent constructor doesn't break anything
				[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
				extern static ConstructorWithParameterTarget InvokeNonExistent (double d);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				// Test that invoke without a return type is ignored
				[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
				extern static void InvokeWithoutReturnType ();

				[Kept]
				public static void Test ()
				{
					InvokeConstructorWithParameter (42);
					InvokeWithEmptyName (null);
					(new ConstructorWithParameter ()).InvokeOnInstance ();
					InvokeNonExistent (0);
					InvokeWithoutReturnType ();
				}
			}

			[Kept]
			public static void Test ()
			{
				DefaultConstructor.Test ();
				ConstructorWithParameter.Test ();
			}
		}

		class StaticMethodAccess
		{
			[Kept]
			class MethodWithoutParameters
			{
				[Kept]
				class MethodWithoutParametersTarget
				{
					[Kept]
					private static void TargetMethod () { }

					[Kept]
					internal static void SecondTarget () { }

					private void InstanceTarget () { }

					private static void DifferentName () { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
				extern static void TargetMethod (MethodWithoutParametersTarget target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = nameof (MethodWithoutParametersTarget.SecondTarget))]
				extern static void SpecifyNameParameter (MethodWithoutParametersTarget target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				// StaticMethod kind doesn't work on instance methods
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
				extern static void InstanceTarget (MethodWithoutParametersTarget target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "NonExistingName")]
				extern static void DifferentName(MethodWithoutParametersTarget target);

				[Kept]
				public static void Test ()
				{
					TargetMethod (null);
					SpecifyNameParameter (null);
					InstanceTarget (null);
					DifferentName (null);
				}
			}

			[Kept]
			class MethodWithParameter
			{
				[Kept]
				class MethodWithParameterTarget
				{
					private static void MethodWithOverloads () { }

					[Kept]
					private static void MethodWithOverloads (int i) { }

					private static void MethodWithGenericAndSpecificOverload (object o) { }

					[Kept]
					private static void MethodWithGenericAndSpecificOverload (string o) { }

					private static void MethodWithThreeInheritanceOverloads (SuperBase o) { }
					[Kept]
					private static void MethodWithThreeInheritanceOverloads (Base o) { }
					private static void MethodWithThreeInheritanceOverloads (Derived o) { }

					private static void MethodWithImperfectMatch (SuperBase o) { }
					private static void MethodWithImperfectMatch (Derived o) { }

					[Kept]
					private static string MoreParameters (string s, ref string sr, in string si) => s;

					private static string MoreParametersWithReturnValueMismatch (string s, ref string sr, in string si) => s;
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
				extern static void MethodWithOverloads (MethodWithParameterTarget target, int i);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
				extern static void MethodWithGenericAndSpecificOverload (MethodWithParameterTarget target, string s);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
				extern static void MethodWithThreeInheritanceOverloads (MethodWithParameterTarget target, Base o);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
				extern static void MethodWithImperfectMatch (MethodWithParameterTarget target, Base o);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
				extern static string MoreParameters (MethodWithParameterTarget target, string s, ref string src, in string si);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
				extern static int MoreParametersWithReturnValueMismatch (MethodWithParameterTarget target, string s, ref string src, in string si);

				[Kept]
				public static void Test ()
				{
					MethodWithOverloads (null, 0);
					MethodWithGenericAndSpecificOverload (null, null);
					MethodWithThreeInheritanceOverloads (null, null);
					MethodWithImperfectMatch (null, null);

					string sr = string.Empty;
					MoreParameters (null, null, ref sr, string.Empty);
					MoreParametersWithReturnValueMismatch (null, null, ref sr, string.Empty);
				}
			}

			[Kept]
			public static void Test ()
			{
				MethodWithoutParameters.Test ();
				MethodWithParameter.Test ();
			}
		}

		class InstanceMethodAccess
		{
			[Kept]
			class MethodWithoutParameters
			{
				[Kept]
				[KeptMember(".ctor()")]
				class MethodWithoutParametersTarget
				{
					[Kept]
					private void TargetMethod () { }

					[Kept]
					internal void SecondTarget () { }

					private static void StaticTarget () { }

					private void DifferentName () { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method)]
				extern static void TargetMethod (MethodWithoutParametersTarget target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method, Name = nameof (MethodWithoutParametersTarget.SecondTarget))]
				extern static void SpecifyNameParameter (MethodWithoutParametersTarget target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				// Method kind doesn't work on static methods
				[UnsafeAccessor (UnsafeAccessorKind.Method)]
				extern static void StaticTarget (MethodWithoutParametersTarget target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method, Name = "NonExistingName")]
				extern static void DifferentName (MethodWithoutParametersTarget target);

				[Kept]
				public static void Test ()
				{
					var instance = new MethodWithoutParametersTarget ();
					TargetMethod (null);
					SpecifyNameParameter (null);
					StaticTarget (null);
					DifferentName (null);
				}
			}

			[Kept]
			class MethodWithParameter
			{
				[Kept]
				[KeptMember (".ctor()")]
				class MethodWithParameterTarget
				{
					private void MethodWithOverloads () { }

					[Kept]
					private void MethodWithOverloads (int i) { }

					private void MethodWithGenericAndSpecificOverload (object o) { }

					[Kept]
					private void MethodWithGenericAndSpecificOverload (string o) { }

					private void MethodWithThreeInheritanceOverloads (SuperBase o) { }
					[Kept]
					private void MethodWithThreeInheritanceOverloads (Base o) { }
					private void MethodWithThreeInheritanceOverloads (Derived o) { }

					private void MethodWithImperfectMatch (SuperBase o) { }
					private void MethodWithImperfectMatch (Derived o) { }

					[Kept]
					private string MoreParameters (string s, ref string sr, in string si) => s;

					private string MoreParametersWithReturnValueMismatch (string s, ref string sr, in string si) => s;
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method)]
				extern static void MethodWithOverloads (MethodWithParameterTarget target, int i);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method)]
				extern static void MethodWithGenericAndSpecificOverload (MethodWithParameterTarget target, string s);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method)]
				extern static void MethodWithThreeInheritanceOverloads (MethodWithParameterTarget target, Base o);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method)]
				extern static void MethodWithImperfectMatch (MethodWithParameterTarget target, Base o);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method)]
				extern static string MoreParameters (MethodWithParameterTarget target, string s, ref string src, in string si);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method)]
				extern static int MoreParametersWithReturnValueMismatch (MethodWithParameterTarget target, string s, ref string src, in string si);

				[Kept]
				public static void Test ()
				{
					var instance = new MethodWithParameterTarget ();

					MethodWithOverloads (null, 0);
					MethodWithGenericAndSpecificOverload (null, null);
					MethodWithThreeInheritanceOverloads (null, null);
					MethodWithImperfectMatch (null, null);

					string sr = string.Empty;
					MoreParameters (null, null, ref sr, string.Empty);
					MoreParametersWithReturnValueMismatch (null, null, ref sr, string.Empty);
				}
			}

			[Kept]
			class CustomModifiersTest
			{
				[Kept]
				class CustomModifiersTestTarget
				{
					private static string _Ambiguous (delegate* unmanaged[Cdecl, MemberFunction]<void> fp) => nameof (CallConvCdecl);

					[Kept]
					private static string _Ambiguous (delegate* unmanaged[Stdcall, MemberFunction]<void> fp) => nameof (CallConvStdcall);
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "_Ambiguous")]
				extern static string CallPrivateMethod (CustomModifiersTestTarget d, delegate* unmanaged[Stdcall, MemberFunction]<void> fp);

				[Kept]
				public static void Test ()
				{
					CallPrivateMethod (null, null);
				}
			}

			[Kept]
			public static void Test ()
			{
				MethodWithoutParameters.Test ();
				MethodWithParameter.Test ();
				CustomModifiersTest.Test ();
			}
		}

		[Kept (By = Tool.Trimmer)] // NativeAOT doesn't preserve base type if it's not used anywhere
		class SuperBase { }

		[Kept (By = Tool.Trimmer)] // NativeAOT won't keep the type since it's only used as a parameter type and never instantiated
		[KeptBaseType (typeof (SuperBase), By = Tool.Trimmer)]
		class Base : SuperBase { }

		//[Kept]
		//[KeptBaseType (typeof (Base))]
		class Derived : Base { }
	}
}
