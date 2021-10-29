// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestLibrary;

namespace Mono.Linker.Tests.Cases.CppCLI.Dependencies
{
	public static class CallCppCLIFromManaged
	{
		public static void TriggerWarning ()
		{
			TestClass.TriggerWarningFromCppCLI ();
		}
	}
}
