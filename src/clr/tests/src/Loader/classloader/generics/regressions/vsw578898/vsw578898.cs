// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

interface IFoo { }

interface IGenericBase<T> {
  void M<U>() where U : IGenericBase<T>;
}

abstract class GenericBase<T> : IGenericBase<T> {
  public virtual void M<U>() where U : IGenericBase<T> { }
}

class Derived : GenericBase<IFoo>, IGenericBase<IFoo> {

}
