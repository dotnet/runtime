// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
