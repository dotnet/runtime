// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/MultipleMethodsUseSameAsyncStateMachine.il" })]
	[ExpectedNoWarnings]
	[LogContains ("IL2107.*<M>g__LocalFunction().*M().*both associated with state machine type.*<StateMachine>d", regexMatch: true)]
	class MultipleMethodsUseSameAsyncStateMachine
	{
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			Dependencies.MultipleMethodsUseSameAsyncStateMachine.M();
#endif
		}
	}
}
