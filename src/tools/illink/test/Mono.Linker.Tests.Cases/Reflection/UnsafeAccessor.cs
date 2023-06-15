// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
				extern static DefaultConstructorTarget InvokeDefaultConstructor ();

				[Kept]
				public static void Test ()
				{
					InvokeDefaultConstructor ();
				}
			}

			[Kept]
			class ConstructorWithParameter
			{
				[Kept]
				class ConstructorWithParameterTarget
				{
					[Kept] // BUG - method overload resolution doesn't work yet
					private ConstructorWithParameterTarget () { }

					[Kept]
					private ConstructorWithParameterTarget (int i) { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
				extern static ConstructorWithParameterTarget InvokeConstructorWithParameter (int i);

				[Kept]
				public static void Test ()
				{
					InvokeConstructorWithParameter (42);
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
					private MethodWithoutParametersTarget () { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.StaticMethod)]
				extern static void InvokeMethodWithoutParameters (MethodWithoutParametersTarget target);

				[Kept]
				public static void Test ()
				{
					InvokeMethodWithoutParameters (null);
				}
			}

			[Kept]
			class MethodWithParameter
			{
				[Kept]
				class MethodWithParameterTarget
				{
					[Kept] // BUG - method overload resolution doesn't work yet
					private MethodWithParameterTarget () { }

					[Kept]
					private MethodWithParameterTarget (int i) { }
				}

				[Kept]
				[KeptAttributeAttribute (typeof (UnsafeAccessorAttribute))]
				[UnsafeAccessor (UnsafeAccessorKind.Constructor)]
				extern static void InvokeMethodWithParameter (MethodWithParameterTarget target, int i);

				[Kept]
				public static void Test ()
				{
					InvokeMethodWithParameter (null, 42);
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
