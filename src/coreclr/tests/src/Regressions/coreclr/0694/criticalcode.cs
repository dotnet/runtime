// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;


public class BaseClass
{
    [SecuritySafeCritical]
    public virtual bool TreatAsSafe()
    {
        Console.WriteLine("BaseClass");
        return true;
    }
}

public class DerivedClassOverrideTransparent : BaseClass
{
    public override bool TreatAsSafe()
    {
        Console.WriteLine("DerivedClass");
        return true;
    }
}