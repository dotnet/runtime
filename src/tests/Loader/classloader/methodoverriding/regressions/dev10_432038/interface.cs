// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public interface IFoo
{
	int A();
}

public interface IBar<T>
{
    int A<U>();
}
