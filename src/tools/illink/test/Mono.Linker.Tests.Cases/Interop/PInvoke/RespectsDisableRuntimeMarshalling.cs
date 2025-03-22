// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Interop.PInvoke.Dependencies;

#if INCLUDE_DISABLE_RUNTIME_MARSHALLING_ATTRIBUTE
[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]
#endif

[assembly: KeptAttributeAttribute(typeof(DisableRuntimeMarshallingAttribute))]

namespace Mono.Linker.Tests.Cases.Interop.PInvoke;

[SetupCompileBefore ("WithoutDisable.dll", new [] { typeof(AssemblyWithoutDisableRuntimeMarshalling) })]
[Define ("INCLUDE_DISABLE_RUNTIME_MARSHALLING_ATTRIBUTE")]
[KeptModuleReference ("Unused")]
[KeptMemberInAssembly ("WithoutDisable.dll", typeof (AssemblyWithoutDisableRuntimeMarshalling.B), ".ctor()")]
public class RespectsDisableRuntimeMarshalling
{
	public static void Main ()
	{
		var a = SomeMethod ();
		var b = AssemblyWithoutDisableRuntimeMarshalling.SomeMethod ();
	}

	// The .ctor() will not be marked due to DisableRuntimeMarshalling
	[Kept]
	class A;

	[Kept]
	[DllImport ("Unused")]
	static extern A SomeMethod ();
}
