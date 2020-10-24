// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//repro for devdiv bugs #3422. The orgianal bug caused TypeLoadException.

using System;

interface IFoo { }

interface IGenericBase<T> {
  void M<U>() where U : IGenericBase<T>;
}

abstract class GenericBase<T> : IGenericBase<T> {
  public virtual void M<U>() where U : IGenericBase<T> { }
}

class Derived : GenericBase<IFoo>, IGenericBase<IFoo> {
// If this line is re-added, the dll verifies
//   public override void M<Z>() { }

    static int Main() {
	Console.WriteLine( "Passed" );
	return 100;
    }
}
