// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SetupLinkerSubstitutionFile ("DetectRedundantSuppressionsFeatureSubstitutions.xml")]
	[SetupLinkerArgument ("--feature", "Feature", "false")]
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class DetectRedundantSuppressionsFeatureSubstitutions
	{
		public static void Main ()
		{
			ReportRedundantSuppressionWhenTrimmerIncompatibleCodeDisabled.Test ();
			DoNotReportUsefulSuppressionWhenTrimmerIncompatibleCodeEnabled.Test ();
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (DetectRedundantSuppressionsFeatureSubstitutions);
		}

		public static string TrimmerCompatibleMethod ()
		{
			return "test";
		}

		public static bool IsFeatureEnabled {
			get => throw new NotImplementedException ();
		}

		class ReportRedundantSuppressionWhenTrimmerIncompatibleCodeDisabled
		{
			// The test simulates the following issue.
			// https://github.com/dotnet/linker/issues/2921
			// The suppressed warning is issued in the 'if' branch.
			// With feature switched to false, the linker sees only the 'else' branch.
			// The 'else' branch contains trimmer-compatible code, the linker identifies the suppression as redundant.

			[ExpectedWarning ("IL2121", "IL2072")]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static void Test ()
			{
				if (IsFeatureEnabled) {
					Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				} else {
					TrimmerCompatibleMethod ();
				}
			}
		}

		class DoNotReportUsefulSuppressionWhenTrimmerIncompatibleCodeEnabled
		{
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static void Test ()
			{
				if (!IsFeatureEnabled) {
					Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				} else {
					TrimmerCompatibleMethod ();
				}
			}
		}
	}
}
