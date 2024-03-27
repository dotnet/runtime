// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//repro for devdiv bugs #3422. The orgianal bug caused TypeLoadException.

using System;
using Xunit;

public interface IFoo { }

public interface IGenericBase<T> {
  void M<U>() where U : IGenericBase<T>;
}

public abstract class GenericBase<T> : IGenericBase<T> {
  public virtual void M<U>() where U : IGenericBase<T> { }
}

public class Derived : GenericBase<IFoo>, IGenericBase<IFoo> {
// If this line is re-added, the dll verifies
//   public override void M<Z>() { }

    [Fact]
    public static void TestEntryPoint()
    {
    }
}
