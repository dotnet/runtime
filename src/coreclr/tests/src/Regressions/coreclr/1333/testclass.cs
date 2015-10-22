using System;
using System.Collections.Generic;
using System.Security;

public class A
{
    public virtual object GetService(Type serviceType)
    {
        return this;
    }
}

[SecurityCritical]
public class Foo : A   
{
    public override object GetService(Type serviceType)
    {
        return this;
    }
}

public class Class1
{
    [SecuritySafeCritical]
    public static int Main()
    {
        try 
        {
            Foo f = new Foo();
            object o = f.GetService(null);                     // <-- MethodAccessException
            Console.WriteLine("Success.");
            return 100;
        }
        catch (Exception e) 
        {
            Console.WriteLine("Error: " + e);
            return 101;
        }
    }
}
