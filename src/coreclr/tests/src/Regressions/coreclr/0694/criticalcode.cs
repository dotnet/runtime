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