// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class MethodOutParameterDataFlow
	{
		public static void Main ()
		{
			Type t = null;
			TestInitializedReadFromOutParameter ();
			TestInitializedReadFromOutParameter_PassedTwice ();
			TestUninitializedReadFromOutParameter ();
			TestInitializedReadFromOutParameter_MismatchOnOutput ();
			TestInitializedReadFromOutParameter_MismatchOnOutput_PassedTwice ();
			TestInitializedReadFromOutParameter_MismatchOnInput ();
			TestInitializedReadFromOutParameter_MismatchOnInput_PassedTwice ();
			// Gets Fields
			TestPassingOutParameter_Mismatch (out t);
			t.RequiresPublicFields ();
			// Gets Methods
			TestPassingOutParameter (out t);
			// Needs Methods and gets Methods
			TestAssigningToOutParameter (t, out t);
			t = typeof (int);
			// Needs Fields and gets Methods
			TestAssigningToOutParameter_Mismatch (t, out t);
		}

		static void TestInitializedReadFromOutParameter ()
		{
			Type typeWithMethods = null;
			TryGetAnnotatedValue (out typeWithMethods);
			typeWithMethods.RequiresPublicMethods ();
		}

		static void TestInitializedReadFromOutParameter_PassedTwice ()
		{
			Type typeWithMethods = null;
			TryGetAnnotatedValueFromValue (typeWithMethods, out typeWithMethods);
			typeWithMethods.RequiresPublicMethods ();
		}

		static void TestUninitializedReadFromOutParameter ()
		{
			Type typeWithMethods;
			TryGetAnnotatedValue (out typeWithMethods);
			typeWithMethods.RequiresPublicMethods ();
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestInitializedReadFromOutParameter_MismatchOnOutput ()
		{
			Type typeWithMethods = null;
			TryGetAnnotatedValue (out typeWithMethods);
			typeWithMethods.RequiresPublicFields ();
		}

		// https://github.com/dotnet/linker/issues/2632
		// This test should generate a warning since there's mismatch on annotations
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestInitializedReadFromOutParameter_MismatchOnOutput_PassedTwice ()
		{
			Type typeWithMethods = null;
			TryGetAnnotatedValueFromValue (typeWithMethods, out typeWithMethods);
			typeWithMethods.RequiresPublicFields ();
		}

		// https://github.com/dotnet/linker/issues/2632
		// This warning should not be generated, the value of typeWithMethods should have PublicMethods
		// after the call with out parameter.
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresPublicMethods), ProducedBy = ProducedBy.Analyzer)]
		static void TestInitializedReadFromOutParameter_MismatchOnInput ()
		{
			Type typeWithMethods = GetTypeWithFields ();
			// No warning on out parameter
			TryGetAnnotatedValue (out typeWithMethods);
			typeWithMethods.RequiresPublicMethods ();
		}

		[ExpectedWarning ("IL2072", nameof (TryGetAnnotatedValueFromValue))]
		// https://github.com/dotnet/linker/issues/2632
		// This warning should not be generated, the value of typeWithMethods should have PublicMethods
		// after the call with out parameter.
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresPublicMethods), ProducedBy = ProducedBy.Analyzer)]
		static void TestInitializedReadFromOutParameter_MismatchOnInput_PassedTwice ()
		{
			Type typeWithMethods = GetTypeWithFields ();
			// Warn on first parameter only, not on out parameter
			TryGetAnnotatedValueFromValue (typeWithMethods, out typeWithMethods);
			typeWithMethods.RequiresPublicMethods ();
		}

		static void TestPassingOutParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] out Type typeWithMethods)
		{
			TryGetAnnotatedValue (out typeWithMethods);
		}

		[ExpectedWarning ("IL2067", "typeWithFields", nameof (TryGetAnnotatedValue))]
		static void TestPassingOutParameter_Mismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] out Type typeWithFields)
		{
			TryGetAnnotatedValue (out typeWithFields);
		}

		static void TestAssigningToOutParameter (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type inputTypeWithMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] out Type outputTypeWithMethods)
		{
			outputTypeWithMethods = inputTypeWithMethods;
		}

		[ExpectedWarning ("IL2067", "inputTypeWithFields", "outputTypeWithMethods")]
		static void TestAssigningToOutParameter_Mismatch (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type inputTypeWithFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] out Type outputTypeWithMethods)
		{
			outputTypeWithMethods = inputTypeWithFields;
		}

		static bool TryGetAnnotatedValue ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] out Type typeWithMethods)
		{
			typeWithMethods = null;
			return false;
		}

		static bool TryGetAnnotatedValueFromValue (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type inValue,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] out Type typeWithMethods)
		{
			typeWithMethods = inValue;
			return false;
		}

		[return: DynamicallyAccessedMembersAttribute (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type GetTypeWithFields () => null;

		class TestType
		{
		}
	}
}
