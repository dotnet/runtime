// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

// Module suppressions that have scope argument `type` or `member` will always cause the value of the target parameter
// to be parsed using the DocumentationSignatureParser, this parser rules where will the suppression be put depending upon the
// prefix used in the fully qualified member specified for the target.
[module: UnconditionalSuppressMessage ("Test", "IL2026",
	Scope = "type", Target = "M:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.TargettedModuleSuppressionWithUnmatchedScope.Main()")]
// The target of this suppression will be ignored since the suppression was put on a higher scope -- this suppression will be
// put on the module.
[module: UnconditionalSuppressMessage ("Test", "IL2072",
	Scope = "module", Target = "T:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.TargettedModuleSuppressionWithUnmatchedScope")]
[module: UnconditionalSuppressMessage ("Test", "IL2026",
	Target = "M:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.TargettedModuleSuppressionWithUnmatchedScope.Main()")]

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[LogContains ("warning IL2108: Invalid scope '' used in 'UnconditionalSuppressMessageAttribute'")]
	[LogDoesNotContain ("IL2026")]
	[LogDoesNotContain ("IL2072")]
	class TargettedModuleSuppressionWithUnmatchedScope
	{
		static void Main ()
		{
			// IL2072
			Expression.Call (TriggerWarning (), "", Type.EmptyTypes);
			// IL2026
			TriggerWarning ();
		}

		[RequiresUnreferencedCode ("TriggerWarning")]
		public static Type TriggerWarning ()
		{
			return typeof (TargettedModuleSuppressionWithUnmatchedScope);
		}
	}
}
