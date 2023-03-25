// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class SuppressRequires
	{
		public static void Main ()
		{
			SuppressMethodBodyReferences.Test ();
			SuppressGenericParameters<TestType, TestType>.Test ();
		}

		class SuppressMethodBodyReferences
		{
			static Type _unknownType;
			static Type GetUnknownType () => null;

			[RequiresUnreferencedCode ("Message for --MethodWithRequires--")]
			[RequiresAssemblyFiles ("Message for --MethodWithRequires--")]
			[RequiresDynamicCode ("Message for --MethodWithRequires--")]
			static void MethodWithRequires ()
			{
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type _requiresPublicConstructors;

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			[RequiresDynamicCode ("")]
			static void TestMethodWithRequires ()
			{
				// Normally this would warn, but with the attribute on this method it should be auto-suppressed
				MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			[RequiresDynamicCode ("")]
			static void TestParameter ()
			{
				_unknownType.RequiresPublicMethods ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			[RequiresDynamicCode ("")]
			static void TestReturnValue ()
			{
				GetUnknownType ().RequiresPublicEvents ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			[RequiresDynamicCode ("")]
			static void TestField ()
			{
				_requiresPublicConstructors = _unknownType;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestMethodWithRequires ();
				TestParameter ();
				TestReturnValue ();
				TestField ();
			}
		}

		[ExpectedNoWarnings]
		class SuppressGenericParameters<TUnknown, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TPublicProperties>
		{
			static Type _unknownType;

			static void GenericMethodRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

			class GenericTypeRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> { }

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			[RequiresDynamicCode ("")]
			static void TestGenericMethod ()
			{
				GenericMethodRequiresPublicMethods<TUnknown> ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			[RequiresDynamicCode ("")]
			static void TestGenericMethodMismatch ()
			{
				GenericMethodRequiresPublicMethods<TPublicProperties> ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			[RequiresDynamicCode ("")]
			static void TestGenericType ()
			{
				new GenericTypeRequiresPublicFields<TUnknown> ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			[RequiresDynamicCode ("")]
			static void TestMakeGenericTypeWithStaticTypes ()
			{
				typeof (GenericTypeRequiresPublicFields<>).MakeGenericType (typeof (TUnknown));
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			[RequiresDynamicCode ("")]
			static void TestMakeGenericTypeWithDynamicTypes ()
			{
				typeof (GenericTypeRequiresPublicFields<>).MakeGenericType (_unknownType);
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			[RequiresDynamicCode ("")]
			static void TestMakeGenericMethod ()
			{
				typeof (SuppressGenericParameters<TUnknown, TPublicProperties>)
					.GetMethod ("GenericMethodRequiresPublicMethods", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
					.MakeGenericMethod (typeof (TPublicProperties));
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestGenericMethod ();
				TestGenericMethodMismatch ();
				TestGenericType ();
				TestMakeGenericTypeWithStaticTypes ();
				TestMakeGenericTypeWithDynamicTypes ();
				TestMakeGenericMethod ();
			}
		}

		class TestType { }
	}
}
