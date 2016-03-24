// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Reflection;
using TestLibrary;

//Test verifying DCR DevDiv 208672. This makes sure Critical delegates can be bound only to Critical method. Critical delegates can't be bound to transparent or safecritical
//methods and critical methods can't be bound by transparent or safecritical delegates

public class BindingTarget
{
    //Test methods
    public static bool TransparentMethod()
    {
        TestFramework.LogInformation("\tTransparentMethod is invoked");
        return true;
    }
    [SecuritySafeCritical]
    public static bool SafeCriticalMethod()
    {
        TestFramework.LogInformation("\tSafeCriticalMethod Method is invoked");
        return true;
    }
    [SecurityCritical]
    public static bool CriticalMethod()
    {
        TestFramework.LogInformation("\tSecurityCriticalMethod Method is invoked");
        return true;
    }
    
 
    //Test methods
    public static bool GenericTransparentMethod<T>()
    {
        TestFramework.LogInformation("\tTransparentMethod is invoked");
        return true;
    }
    [SecuritySafeCritical]
    public static bool GenericSafeCriticalMethod<T>()
    {
        TestFramework.LogInformation("\tSafeCriticalMethod Method is invoked");
        return true;
    }
    [SecurityCritical]
    public static bool GenericCriticalMethod<T>()
    {
        TestFramework.LogInformation("\tSecurityCriticalMethod Method is invoked");
        return true;
    }
}
