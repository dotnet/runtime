// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke.Dependencies;

public class AssemblyWithoutDisableRuntimeMarshalling
{
	[DllImport ("Unused")]
	public static extern B SomeMethod ();

	public class B;
}
