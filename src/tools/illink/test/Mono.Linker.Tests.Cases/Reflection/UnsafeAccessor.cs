// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	class UnsafeAccessor
	{
		public static void Main ()
		{
			ConstructorAccess.Test ();
			StaticMethodAccess.Test ();
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
				public static void Test ()
				{
					InvokeDefaultConstructor ();
					InvokeWithName (0);
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			class ConstructorWithParameter
			{
				[Kept]
				class ConstructorWithParameterTarget
				{
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
				// ??? Should this resolve?
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
				public static void Test ()
				{
					TargetMethod (null);
					SpecifyNameParameter (null);
					InstanceTarget (null);
				}
			}

			[Kept]
			class MethodWithParameter
			{
				[Kept]
				class SuperBase { }

				[Kept]
				[KeptBaseType (typeof (SuperBase))]
				class Base : SuperBase { }

				//[Kept]
				//[KeptBaseType (typeof (Base))]
				class Derived : Base { }

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
				public static void Test ()
				{
					MethodWithOverloads (null, 0);
					MethodWithGenericAndSpecificOverload (null, null);
					MethodWithThreeInheritanceOverloads (null, null);
					MethodWithImperfectMatch (null, null);
				}
			}

			[Kept]
			public static void Test ()
			{
				MethodWithoutParameters.Test ();
				MethodWithParameter.Test ();
			}
		}
	}
}
