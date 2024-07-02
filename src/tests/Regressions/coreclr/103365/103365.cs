// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
https://github.com/dotnet/runtime/issues/103365
When using an interface with a generic out type, an explicit implementation, and a derived class, the base classes implementation is called instead of the derived class when running on Android. Running on Windows yields the expected behavior.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public interface IBaseInterface<out T>
{
    string explicitDeclaration();
}

public class BasicBaseClass : IBaseInterface<BasicBaseClass>
{
    string className = "BasicBaseClass";
    string IBaseInterface<BasicBaseClass>.explicitDeclaration()
    {
        return className;
    }
}

public class BasicDerivedClass : BasicBaseClass, IBaseInterface<BasicDerivedClass>
{
    string className = "BasicDerivedClass";

    string IBaseInterface<BasicDerivedClass>.explicitDeclaration()
    {
        return className;
    }
}

public static class Test_Issue103365
{
    [Fact]
  	public static void Main ()
	{
        var instances = new IBaseInterface<BasicBaseClass>[2];
        instances[0] = new BasicBaseClass();
        instances[1] = new BasicDerivedClass();
        Assert.Equal("BasicBaseClass", instances[0].explicitDeclaration());
        Assert.Equal("BasicDerivedClass", instances[1].explicitDeclaration());
  	}
}

