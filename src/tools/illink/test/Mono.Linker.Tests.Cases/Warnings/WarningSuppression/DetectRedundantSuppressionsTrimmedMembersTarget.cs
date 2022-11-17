// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: UnconditionalSuppressMessage ("Test", "IL2071",
	Scope = "type",
	Target = "T:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.UnusedTypeWithRedundantSuppression")]
[assembly: UnconditionalSuppressMessage ("Test", "IL2071",
	Scope = "member",
	Target = "P:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.UnusedTypeWithMembers.UnusedPropertyWithSuppression")]
[assembly: UnconditionalSuppressMessage ("Test", "IL2071",
	Scope = "member",
	Target = "E:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.UnusedTypeWithMembers.UnusedEventWithSuppression")]
[assembly: UnconditionalSuppressMessage ("Test", "IL2071",
	Scope = "member",
	Target = "M:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.UnusedTypeWithMembers.UnusedMethodWithSuppression")]

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	class DetectRedundantSuppressionsTrimmedMembersTarget
	{
		[ExpectedWarning ("IL2072")]
		static void Main ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (DetectRedundantSuppressionsTrimmedMembersTarget);
		}
	}

	class UnusedTypeWithRedundantSuppression
	{
	}

	class UnusedTypeWithMembers
	{
		int UnusedPropertyWithSuppression { get; set; }

		event EventHandler<EventArgs> UnusedEventWithSuppression {
			add { }
			remove { }
		}

		void UnusedMethodWithSuppression () { }
	}
}
