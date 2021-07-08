// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

// A module level suppression with a null scope will be put on the current module.
[module: UnconditionalSuppressMessage ("Test", "IL2026", Scope = null, Target = null)]

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[LogDoesNotContain ("IL2026")]
	class ModuleSuppressionWithNullScopeNullTarget
	{
		static void Main ()
		{
			TriggerWarning ();
		}

		[RequiresUnreferencedCode ("TriggerWarning")]
		public static Type TriggerWarning ()
		{
			return typeof (ModuleSuppressionWithNullScopeNullTarget);
		}
	}
}
