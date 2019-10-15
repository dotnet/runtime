// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

public class ClassFromA : IGetTypeFromC
{
    private readonly ClassFromC _fromC;
    public ClassFromA()
    {
        this._fromC = new ClassFromC();
    }
    
    public object GetTypeFromC()
    {
        return this._fromC.GetType();
    }
}

public class ValidRegistrationTypeCallbacks : IValidateRegistrationCallbacks
{
    [ComRegisterFunctionAttribute]
    public static void RegisterFunction(Type t) => s_didRegister = true;

    [ComUnregisterFunctionAttribute]
    public static void UnregisterFunction(Type t) => s_didUnregister = true;

    private static bool s_didRegister = false;
    private static bool s_didUnregister = false;

    bool IValidateRegistrationCallbacks.DidRegister() => s_didRegister;

    bool IValidateRegistrationCallbacks.DidUnregister() => s_didUnregister;

    void IValidateRegistrationCallbacks.Reset()
    {
        s_didRegister = false;
        s_didUnregister = false;
    }
}

public class ValidRegistrationStringCallbacks : IValidateRegistrationCallbacks
{
    [ComRegisterFunctionAttribute]
    public static void RegisterFunction(string t) => s_didRegister = true;

    [ComUnregisterFunctionAttribute]
    public static void UnregisterFunction(string t) => s_didUnregister = true;

    private static bool s_didRegister = false;
    private static bool s_didUnregister = false;

    bool IValidateRegistrationCallbacks.DidRegister() => s_didRegister;

    bool IValidateRegistrationCallbacks.DidUnregister() => s_didUnregister;

    void IValidateRegistrationCallbacks.Reset()
    {
        s_didRegister = false;
        s_didUnregister = false;
    }
}

public class NoRegistrationCallbacks : IValidateRegistrationCallbacks
{
    // Not attributed function
    public static void RegisterFunction(Type t) => s_didRegister = true;

    // Not attributed function
    public static void UnregisterFunction(Type t) => s_didRegister = true;

    private static bool s_didRegister = false;
    private static bool s_didUnregister = false;

    bool IValidateRegistrationCallbacks.DidRegister() => s_didRegister;

    bool IValidateRegistrationCallbacks.DidUnregister() => s_didUnregister;

    void IValidateRegistrationCallbacks.Reset()
    {
        s_didRegister = false;
        s_didUnregister = false;
    }
}

public class InvalidArgRegistrationCallbacks : IValidateRegistrationCallbacks
{
    [ComRegisterFunctionAttribute]
    public static void RegisterFunction(int i) => throw new Exception();

    [ComUnregisterFunctionAttribute]
    public static void UnregisterFunction(int i) => throw new Exception();

    bool IValidateRegistrationCallbacks.DidRegister() => throw new NotImplementedException();

    bool IValidateRegistrationCallbacks.DidUnregister() => throw new NotImplementedException();

    void IValidateRegistrationCallbacks.Reset() => throw new NotImplementedException();
}

public class InvalidInstanceRegistrationCallbacks : IValidateRegistrationCallbacks
{
    [ComRegisterFunctionAttribute]
    public void RegisterFunction(Type t) => throw new Exception();

    [ComUnregisterFunctionAttribute]
    public void UnregisterFunction(Type t) => throw new Exception();

    bool IValidateRegistrationCallbacks.DidRegister() => throw new NotImplementedException();

    bool IValidateRegistrationCallbacks.DidUnregister() => throw new NotImplementedException();

    void IValidateRegistrationCallbacks.Reset() => throw new NotImplementedException();
}

public class MultipleRegistrationCallbacks : IValidateRegistrationCallbacks
{
    [ComRegisterFunctionAttribute]
    public static void RegisterFunction(string t) { }

    [ComUnregisterFunctionAttribute]
    public static void UnregisterFunction(string t) { }

    [ComRegisterFunctionAttribute]
    public static void RegisterFunction2(string t) { }

    [ComUnregisterFunctionAttribute]
    public static void UnregisterFunction2(string t) { }

    bool IValidateRegistrationCallbacks.DidRegister() => throw new NotImplementedException();

    bool IValidateRegistrationCallbacks.DidUnregister() => throw new NotImplementedException();

    void IValidateRegistrationCallbacks.Reset() => throw new NotImplementedException();
}