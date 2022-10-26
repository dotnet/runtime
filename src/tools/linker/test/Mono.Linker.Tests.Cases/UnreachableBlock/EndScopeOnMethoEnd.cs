// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/EndScopeOnMethod.il" })]
	public class EndScopeOnMethoEnd
	{
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
            // For now just have a method where the try/finally is the last thing in the method (no instruction after the
			// end of finally - Roslyn doesn't seem to produce such method body.
			Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod.TryFinally ();
#endif
		}
	}
}
