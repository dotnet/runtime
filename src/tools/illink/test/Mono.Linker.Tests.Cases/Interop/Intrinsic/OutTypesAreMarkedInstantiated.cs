// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.Intrinsic;

public class OutTypesAreMarkedInstantiated
{
	public static void Main ()
	{
		FooRefParameter refParmaeter = null;
		FakeIntrinsicMethod (null, ref refParmaeter, out FooOutParameter outParameter);
		UsedToMarkMethods (null, null, null, null);
	}

	[Kept]
	[KeptAttributeAttribute (typeof(IntrinsicAttribute))]
	[Intrinsic]
	public static FooReturn FakeIntrinsicMethod (FooNormalParameter normal, ref FooRefParameter @ref, out FooOutParameter @out)
	{
		@out = null;
		return null;
	}

	[Kept]
	static void UsedToMarkMethods (FooReturn f, FooNormalParameter n, FooRefParameter r, FooOutParameter o)
	{
		f.Method ();
		n.Method ();
		r.Method ();
		o.Method ();
	}
}

public class FooReturn
{
	// This method should not have it's body modified because the linker should consider it as instantiated
	[Kept]
	public void Method ()
	{
		throw new System.NotImplementedException ();
	}
}

public class FooOutParameter
{
	// This method should not have it's body modified because the linker should consider it as instantiated
	[Kept]
	public void Method ()
	{
		throw new System.NotImplementedException ();
	}
}

public class FooRefParameter
{
	// This method should not have it's body modified because the linker should consider it as instantiated
	[Kept]
	public void Method ()
	{
		throw new System.NotImplementedException ();
	}
}

public class FooNormalParameter
{
	[Kept]
	// This parameter will not be marked as instantiated because it is not an out or ref parameter.  This will lead to the linker applying the
	// unreachable bodies optimization
	[ExpectBodyModified]
	public void Method ()
	{
		throw new System.NotImplementedException ();
	}
}
