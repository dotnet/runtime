// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Foo
{
	internal virtual void foo () {
	}
}

public class Derived : Foo
{
	void foo2 (Action a) {
		a ();
	}

	internal override void foo () {
		foo2 (base.foo);
	}

	[Fact]
	public static int TestEntryPoint() {
		var d = new Derived ();
		d.foo ();
		return 100;
    }
}
