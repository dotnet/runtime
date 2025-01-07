// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

[module: UnconditionalSuppressMessage ("Test", "IL2071", Scope = "type", Target = "T:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.DetectRedundantSuppressionsInMembersAndTypesUsingTarget.RedundantSuppressionOnType")]
[module: UnconditionalSuppressMessage ("Test", "IL2071", Scope = "member", Target = "M:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.DetectRedundantSuppressionsInMembersAndTypesUsingTarget.RedundantSuppressionOnMethod.Test")]
[module: UnconditionalSuppressMessage ("Test", "IL2071", Scope = "type", Target = "T:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.DetectRedundantSuppressionsInMembersAndTypesUsingTarget.RedundantSuppressionOnNestedType.NestedType")]
[module: UnconditionalSuppressMessage ("Test", "IL2071", Scope = "member", Target = "M:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.DetectRedundantSuppressionsInMembersAndTypesUsingTarget.RedundantSuppressionOnProperty.get_TrimmerCompatibleProperty")]

// The IL2121 warnings are reported on the suppressions targets.
// When the suppressions are declared on the assembly level, ideally they should also be reported on the assembly level.

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class DetectRedundantSuppressionsInMembersAndTypesUsingTarget
	{
		public static void Main ()
		{
			RedundantSuppressionOnType.Test ();
			RedundantSuppressionOnMethod.Test ();
			RedundantSuppressionOnNestedType.Test ();
			RedundantSuppressionOnProperty.Test ();
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (DetectRedundantSuppressionsInMembersAndTypesUsingTarget);
		}

		public static string TrimmerCompatibleMethod ()
		{
			return "test";
		}

		[ExpectedWarning ("IL2121", "IL2071", Tool.Trimmer, "")]
		public class RedundantSuppressionOnType
		{
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class RedundantSuppressionOnMethod
		{
			[ExpectedWarning ("IL2121", "IL2071", Tool.Trimmer, "")]
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class RedundantSuppressionOnNestedType
		{
			public static void Test ()
			{
				NestedType.TrimmerCompatibleMethod ();
			}

			[ExpectedWarning ("IL2121", "IL2071", Tool.Trimmer, "")]
			public class NestedType
			{
				public static void TrimmerCompatibleMethod ()
				{
					TrimmerCompatibleMethod ();
				}
			}
		}

		public class RedundantSuppressionOnProperty
		{
			public static void Test ()
			{
				var property = TrimmerCompatibleProperty;
			}

			public static string TrimmerCompatibleProperty {
				[ExpectedWarning ("IL2121", "IL2071", Tool.Trimmer, "")]
				get {
					return TrimmerCompatibleMethod ();
				}
			}
		}
	}
}
