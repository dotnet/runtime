// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;


// Test types used to make proxies.
public interface TestType_IHelloService
{
    string Hello(string message);
}

public interface TestType_IGoodbyeService
{
    string Goodbye(string message);
}

// Demonstrates interface implementing multiple other interfaces
public interface TestType_IHelloAndGoodbyeService : TestType_IHelloService, TestType_IGoodbyeService
{
}

// Deliberately contains method with same signature of TestType_IHelloService (see TestType_IHelloService1And2).
public interface TestType_IHelloService2
{
    string Hello(string message);
}

// Demonstrates 2 interfaces containing same method name dispatches to the right one
public interface TestType_IHelloService1And2 : TestType_IHelloService, TestType_IHelloService2
{
}

// Demonstrates methods taking multiple parameters as well as a params parameter
public interface TestType_IMultipleParameterService
{
    double TestMethod(int i, string s, double d);
    object ParamsMethod(params object[] parameters);
}

// Demonstrate a void-returning method and parameterless method
public interface TestType_IOneWay
{
    void OneWay();
}

// Demonstrates proxies can be made for properties.
public interface TestType_IPropertyService
{
    string ReadWrite { get; set; }
}

// Negative -- demonstrates trying to use a class for the interface type for the proxy
public class TestType_ConcreteClass
{
    public string Echo(string s) { return null; }
}

// Negative -- demonstrates base type that is sealed and should generate exception
public sealed class Sealed_TestDispatchProxy : DispatchProxy
{
    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        throw new InvalidOperationException();
    }
}

// This test double creates a proxy instance for the requested 'ProxyT' type.
// When methods are invoked on that proxy, it will call a registered callback.
public class TestDispatchProxy : DispatchProxy
{
    // Gets or sets the Action to invoke when clients call methods on the proxy.
    public Func<MethodInfo, object[], object> CallOnInvoke { get; set; }

    // Gets the proxy itself (which is always 'this')
    public object GetProxy()
    {
        return this;
    }

    // Implementation of DispatchProxy.Invoke() just calls back to given Action
    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        return CallOnInvoke(targetMethod, args);
    }
}

public class TestDispatchProxy2 : TestDispatchProxy
{
}

// Negative test -- demonstrates base type that is abstract
public abstract class Abstract_TestDispatchProxy : DispatchProxy
{
    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        throw new InvalidOperationException();
    }
}

// Negative -- demonstrates base type that has no public default ctor
public class NoDefaultCtor_TestDispatchProxy : DispatchProxy
{
    private NoDefaultCtor_TestDispatchProxy()
    {
    }
    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        throw new InvalidOperationException();
    }
}

