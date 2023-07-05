// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[module: UnconditionalSuppressMessage ("Test", "IL2026", Scope = "module", Target = null)]

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[LogDoesNotContain ("IL2026")]
	class ModuleSuppressionWithModuleScopeNullTarget
	{
		static void Main ()
		{
			TriggerWarning ();
		}

		[RequiresUnreferencedCode ("TriggerWarning")]
		public static Type TriggerWarning ()
		{
			return typeof (ModuleSuppressionWithModuleScopeNullTarget);
		}
	}
}
