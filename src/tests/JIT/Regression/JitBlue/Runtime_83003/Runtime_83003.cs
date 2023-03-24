// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class Foo
{
	public virtual void foo () {
	}
}

public class Derived : Foo
{
	void foo2 (Action a) {
		a ();
	}

	public override void foo () {
		foo2 (base.foo);
	}

	public static int Main() {
		var d = new Derived ();
		d.foo ();
		return 100;
    }
}
