// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
			StaticFieldAccess.Test ();
			InstanceFieldAccess.Test ();
			InheritanceTest.Test ();
		}

		// Trimmer doesn't use method overload resolution for UnsafeAccessor and instead marks entire method groups (by name)
		// NativeAOT on the other hand performs exact resolution
		// This difference is currently by design - Mono.Cecil's method resolution is problematic and has bugs. It's also not extensible
		//   and we would need that to correctly implement the desired behavior around custom modifiers. So for now we decided to not
		//   duplicate the logic to tweak it and will just mark entire method groups.

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

					[Kept (By = Tool.Trimmer)]
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
						var instance = (AccessCtorAsMethod) RuntimeHelpers.GetUninitializedObject (typeof (AccessCtorAsMethod));
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
					[Kept (By = Tool.Trimmer)]
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
			class ConstructorOnValueType
			{
				[Kept]
				struct ConstructorOnValueTypeTarget
				{
					[Kept]
					public ConstructorOnValueTypeTarget () { }

					[Kept]
					public ConstructorOnValueTypeTarget (int i) { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
				extern static ConstructorOnValueTypeTarget InvokeDefaultConstructor ();

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method)]
				extern static void InvokeConstructorAsMethod (ref ConstructorOnValueTypeTarget target, int i);

				[Kept]
				struct ConstructorAsMethodOnValueWithoutRefTarget
				{
					[Kept] // This is actually always kept by RuntimeHelpers.GetUninitializedObject - annotation
					public ConstructorAsMethodOnValueWithoutRefTarget (int i) { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method)]
				extern static void InvokeConstructorAsMethodWithoutRef (ConstructorAsMethodOnValueWithoutRefTarget target, int i);

				[Kept]
				public static void Test ()
				{
					InvokeDefaultConstructor ();

					var instance = (ConstructorOnValueTypeTarget) RuntimeHelpers.GetUninitializedObject (typeof (ConstructorOnValueTypeTarget));
					InvokeConstructorAsMethod (ref instance, 0);

					var instanceWithoutRef = (ConstructorAsMethodOnValueWithoutRefTarget) RuntimeHelpers.GetUninitializedObject (typeof (ConstructorAsMethodOnValueWithoutRefTarget));
					InvokeConstructorAsMethodWithoutRef (instanceWithoutRef, 0);
				}
			}

			[Kept]
			public static void Test ()
			{
				DefaultConstructor.Test ();
				ConstructorWithParameter.Test ();
				ConstructorOnValueType.Test ();
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
				extern static void DifferentName (MethodWithoutParametersTarget target);

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
					[Kept (By = Tool.Trimmer)]
					private static void MethodWithOverloads () { }

					[Kept]
					private static void MethodWithOverloads (int i) { }

					[Kept (By = Tool.Trimmer)]
					private static void MethodWithGenericAndSpecificOverload (object o) { }

					[Kept]
					private static void MethodWithGenericAndSpecificOverload (string o) { }

					[Kept (By = Tool.Trimmer)]
					private static void MethodWithThreeInheritanceOverloads (SuperBase o) { }
					[Kept]
					private static void MethodWithThreeInheritanceOverloads (Base o) { }
					[Kept (By = Tool.Trimmer)]
					private static void MethodWithThreeInheritanceOverloads (Derived o) { }

					[Kept (By = Tool.Trimmer)]
					private static void MethodWithImperfectMatch (SuperBase o) { }
					[Kept (By = Tool.Trimmer)]
					private static void MethodWithImperfectMatch (Derived o) { }

					[Kept]
					private static string MoreParameters (string s, ref string sr, in string si) => s;

					[Kept (By = Tool.Trimmer)]
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
			class MethodOnValueType
			{
				[Kept]
				struct MethodOnValueTypeTarget
				{
					[Kept]
					private static void Method () { }

					[Kept]
					private static void MethodCalledWithoutRef () { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "Method")]
				extern static void InvokeMethod (ref MethodOnValueTypeTarget target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "MethodCalledWithoutRef")]
				extern static void InvokeMethodWithoutRef (MethodOnValueTypeTarget target);

				[Kept]
				public static void Test ()
				{
					MethodOnValueTypeTarget instance = new MethodOnValueTypeTarget ();
					InvokeMethod (ref instance);
					InvokeMethodWithoutRef (instance);
				}
			}

			[Kept]
			public static void Test ()
			{
				MethodWithoutParameters.Test ();
				MethodWithParameter.Test ();
				MethodOnValueType.Test ();
			}
		}

		class InstanceMethodAccess
		{
			[Kept]
			class MethodWithoutParameters
			{
				[Kept]
				[KeptMember (".ctor()")]
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
					[Kept (By = Tool.Trimmer)]
					private void MethodWithOverloads () { }

					[Kept]
					private void MethodWithOverloads (int i) { }

					[Kept (By = Tool.Trimmer)]
					private void MethodWithGenericAndSpecificOverload (object o) { }

					[Kept]
					private void MethodWithGenericAndSpecificOverload (string o) { }

					[Kept (By = Tool.Trimmer)]
					private void MethodWithThreeInheritanceOverloads (SuperBase o) { }
					[Kept]
					private void MethodWithThreeInheritanceOverloads (Base o) { }
					[Kept (By = Tool.Trimmer)]
					private void MethodWithThreeInheritanceOverloads (Derived o) { }

					[Kept (By = Tool.Trimmer)]
					private void MethodWithImperfectMatch (SuperBase o) { }
					[Kept (By = Tool.Trimmer)]
					private void MethodWithImperfectMatch (Derived o) { }

					[Kept]
					private string MoreParameters (string s, ref string sr, in string si) => s;

					[Kept (By = Tool.Trimmer)]
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
					[Kept (By = Tool.Trimmer)]
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
			class MethodOnValueType
			{
				[Kept]
				struct MethodOnValueTypeTarget
				{
					[Kept]
					private void Method () { }

					private void MethodCalledWithoutRef () { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method, Name = "Method")]
				extern static void InvokeMethod (ref MethodOnValueTypeTarget target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Method, Name = "MethodCalledWithoutRef")]
				extern static void InvokeMethodWithoutRef (MethodOnValueTypeTarget target);

				[Kept]
				public static void Test ()
				{
					MethodOnValueTypeTarget instance = new MethodOnValueTypeTarget ();
					InvokeMethod (ref instance);
					InvokeMethodWithoutRef (instance);
				}
			}

			[Kept]
			public static void Test ()
			{
				MethodWithoutParameters.Test ();
				MethodWithParameter.Test ();
				CustomModifiersTest.Test ();
				MethodOnValueType.Test ();
			}
		}

		// We currently don't track fields in NativeAOT testing infra
		[Kept]
		class StaticFieldAccess
		{
			[Kept]
			[KeptMember (".ctor()")]
			class StaticFieldTarget
			{
				[Kept (By = Tool.Trimmer)]
				private static int Field;

				[Kept (By = Tool.Trimmer)]
				private static int FieldWithDifferentType;

				private static int FieldWithVoidType;

				[Kept (By = Tool.Trimmer)]
				private static int FieldByName;

				private static int FieldWithParameters;

				private int InstanceField;

				private static int ExistingField;

				private static string FieldWithoutRef;

				[Kept (By = Tool.Trimmer)]
				private static string FieldWithRef;
			}

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
			extern static ref int Field (StaticFieldTarget target);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			// Verify that declaring more parameters means the accessor is ignored
			[UnsafeAccessor (UnsafeAccessorKind.StaticField, Name = "FieldWithParameters")]
			extern static ref int FieldWithParameters (StaticFieldTarget target, int i);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			// Verify that access the field with different type still marks it (same as method overload resolution problems)
			[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
			extern static ref string FieldWithDifferentType (StaticFieldTarget target);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			// Verify that access the field with different type still marks it (same as method overload resolution problems)
			[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
			extern static void FieldWithVoidType (StaticFieldTarget target);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.StaticField, Name = "FieldByName")]
			extern static ref int FieldByName (StaticFieldTarget target);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.StaticField, Name = "InstanceField")]
			extern static ref int InstanceFieldAsStaticField (StaticFieldTarget target);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
			extern static ref int NonExistentField (StaticFieldTarget target);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
			extern static string FieldWithoutRef (StaticFieldTarget target);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
			extern static ref string FieldWithRef (StaticFieldTarget target);

			[Kept]
			class FieldOnValueType
			{
				[Kept (By = Tool.Trimmer)]
				struct Target
				{
					[Kept (By = Tool.Trimmer)]
					private static int Field;

					[Kept (By = Tool.Trimmer)]
					private static int FieldWithoutRefOnThis;

					private static int FieldWithoutRefOnReturn;
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
				extern static ref int Field (ref Target target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
				extern static ref int FieldWithoutRefOnThis (Target target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticField)]
				extern static int FieldWithoutRefOnReturn (ref Target target);

				[Kept]
				public static void Test ()
				{
					Target target = new Target ();
					Field (ref target);
					FieldWithoutRefOnThis (target);
					FieldWithoutRefOnReturn (ref target);
				}
			}

			[Kept]
			public static void Test ()
			{
				Field (null);
				FieldWithParameters (null, 0);
				FieldWithDifferentType (null);
				FieldWithVoidType (null);
				FieldByName (null);
				NonExistentField (null);
				FieldWithoutRef (null);
				FieldWithRef (null);

				StaticFieldTarget target = new StaticFieldTarget ();
				InstanceFieldAsStaticField (target);

				FieldOnValueType.Test ();
			}
		}

		[Kept]
		class InstanceFieldAccess
		{
			[Kept]
			[KeptMember (".ctor()")]
			class InstanceFieldTarget
			{
				[Kept (By = Tool.Trimmer)]
				private int Field;

				private static int StaticField;
			}

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.Field)]
			extern static ref int Field (InstanceFieldTarget target);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.Field, Name = "StaticField")]
			extern static ref int StaticFieldAsInstanceField (InstanceFieldTarget target);

			[Kept]
			class FieldOnValueType
			{
				[Kept (By = Tool.Trimmer)]
				[StructLayout (LayoutKind.Auto)] // Otherwise trimmer will keep all the fields
				struct Target
				{
					[Kept (By = Tool.Trimmer)]
					private int Field;

					private int FieldWithoutRefOnThis;

					private int FieldWithoutRefOnReturn;
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Field)]
				extern static ref int Field (ref Target target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Field)]
				extern static ref int FieldWithoutRefOnThis (Target target);

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Field)]
				extern static int FieldWithoutRefOnReturn (ref Target target);

				[Kept]
				public static void Test ()
				{
					Target target = new Target ();
					Field (ref target);
					FieldWithoutRefOnThis (target);
					FieldWithoutRefOnReturn (ref target);
				}
			}

			[Kept]
			public static void Test ()
			{
				InstanceFieldTarget target = new InstanceFieldTarget ();
				Field (target);
				StaticFieldAsInstanceField (target);
				FieldOnValueType.Test ();
			}
		}

		[Kept]
		class InheritanceTest
		{
			[Kept]
			[KeptMember (".ctor()")]
			class InheritanceTargetBase
			{
				private void OnBase () { }

				private void OnBoth () { }

				public void PublicOnBase () { }

				public void PublicOnBoth () { }

				private int FieldOnBase;

				public int PublicFieldOnBase;
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (InheritanceTargetBase))]
			class InheritanceTargetDerived : InheritanceTargetBase
			{
				[Kept]
				private void OnDerived () { }

				[Kept (By = Tool.Trimmer)]
				private void OnBoth (string s) { }

				[Kept (By = Tool.Trimmer)]
				public void PublicOnBoth (string s) { }

				[Kept (By = Tool.Trimmer)]
				private int FieldOnDerived;
			}

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.Method)]
			extern static void OnBase (InheritanceTargetDerived t);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.Method)]
			extern static void OnDerived (InheritanceTargetDerived t);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.Method)]
			extern static void OnBoth (InheritanceTargetDerived t);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.Method)]
			extern static void PublicOnBase (InheritanceTargetDerived t);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.Method)]
			extern static void PublicOnBoth (InheritanceTargetDerived t);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.Field)]
			extern static ref int FieldOnBase (InheritanceTargetDerived t);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.Field)]
			extern static ref int FieldOnDerived (InheritanceTargetDerived t);

			[Kept]
			[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
			[UnsafeAccessor (UnsafeAccessorKind.Field)]
			extern static ref int PublicFieldOnBase (InheritanceTargetDerived t);

			[Kept]
			public static void Test ()
			{
				InheritanceTargetDerived derived = new InheritanceTargetDerived ();
				OnBase (derived);
				OnDerived (derived);
				OnBoth (derived);
				PublicOnBase (derived);
				PublicOnBoth (derived);

				FieldOnBase (derived);
				FieldOnDerived (derived);
				PublicFieldOnBase (derived);
			}
		}

		[Kept (By = Tool.Trimmer)] // NativeAOT doesn't preserve base type if it's not used anywhere
		class SuperBase { }

		[Kept (By = Tool.Trimmer)] // NativeAOT won't keep the type since it's only used as a parameter type and never instantiated
		[KeptBaseType (typeof (SuperBase), By = Tool.Trimmer)]
		class Base : SuperBase { }

		[Kept (By = Tool.Trimmer)] // NativeAOT doesn't preserve base type if it's not used anywhere
		[KeptBaseType (typeof (Base), By = Tool.Trimmer)]
		class Derived : Base { }
	}
}
