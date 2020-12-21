// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public class ClassFromB : IGetTypeFromC
{
    private readonly ClassFromC _fromC;
    public ClassFromB()
    {
        this._fromC = new ClassFromC();
    }

    public object GetTypeFromC()
    {
        return this._fromC.GetType();
    }
}

public class RegistrationTypeCallbacksFromB : IValidateRegistrationCallbacks
{
    [ComRegisterFunctionAttribute]
    internal static void RegisterFunction(Type t) => s_didRegister = true;

    [ComUnregisterFunctionAttribute]
    internal static void UnregisterFunction(Type t) => s_didUnregister = true;

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

public class RegistrationStringCallbacksFromB : IValidateRegistrationCallbacks
{
    [ComRegisterFunctionAttribute]
    internal static void RegisterFunction(string t) => s_didRegister = true;

    [ComUnregisterFunctionAttribute]
    internal static void UnregisterFunction(string t) => s_didUnregister = true;

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
