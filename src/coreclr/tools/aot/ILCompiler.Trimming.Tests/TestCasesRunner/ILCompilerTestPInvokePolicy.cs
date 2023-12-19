// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.TypeSystem;

namespace Mono.Linker.Tests.TestCasesRunner
{
	internal sealed class ILCompilerTestPInvokePolicy : PInvokeILEmitterConfiguration
	{
		public override bool GenerateDirectCall (MethodDesc method, out string? externName)
		{
			externName = method.Name;
			return true;
		}
	}
}
