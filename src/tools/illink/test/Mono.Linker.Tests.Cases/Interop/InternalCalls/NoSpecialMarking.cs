// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.InternalCalls;

public class NoSpecialMarking
{
	public static void Main ()
	{
		A a = null;
		SomeMethod (ref a, null, null);
	}

	[Kept]
	class A;

	[Kept]
	class B;

	[Kept]
	class C;

	[Kept]
	class D
	{
		int field1;
		int field2;
	}

	[Kept]
	[MethodImpl (MethodImplOptions.InternalCall)]
	static extern C SomeMethod (ref A a, B b, D d);
}
