// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// User/transparent types for RunClassConstructor tests

using System;
using System.Security;

#if WINCORESYS
[assembly: AllowPartiallyTrustedCallers]
#endif
public class Watcher
{
    public static bool f_hasRun = false;
    public static string lastTypeParameter = "";
}

public class GenericWithCctor<T>
{
    //public static string myTypeName =
      //  typeof(T).FullName;
    static GenericWithCctor()
    {
        Watcher.lastTypeParameter = typeof(T).FullName;
        Watcher.f_hasRun = true;
    }
}
public class NoCctor
{
}

public class NestedContainer
{
    // "nested public"
    public class NestedPublicHasCctor {
       static NestedPublicHasCctor()
        {
            Watcher.f_hasRun = true;
        }
    }
    
    // "nested private"
    private class NestedPrivateHasCctor {
       static NestedPrivateHasCctor()
        {
            Watcher.f_hasRun = true;
        }
    }
    
    // "nested assembly"
    internal class NestedAssemHasCctor
    {
        static NestedAssemHasCctor()
        {
            Watcher.f_hasRun = true;
        }
    }

    // "nested famorassem"
    internal protected class NestedFamOrAssemHasCctor
    {
        static NestedFamOrAssemHasCctor()
        {
            Watcher.f_hasRun = true;
        }
    }

}
class PrivateHasCctor
{
        static PrivateHasCctor()
    {
        Watcher.f_hasRun = true;
    }
}

public class HasCctor
{
    static HasCctor()
    {
        Watcher.f_hasRun = true;
    }
}

public class CriticalCctor
{
    [System.Security.SecurityCritical]
    static CriticalCctor()
    {
        Watcher.f_hasRun = true;
    }
}
// base-class of (...); duplicate of HasCctor so we guarantee that it hasn't
// been initialized when we call its subclass cctor.
public class BaseWithCctor
{
    static BaseWithCctor()
    {
        Watcher.f_hasRun = true;
    }
}

public class CctorInBase : BaseWithCctor
{

}

public class ThrowingCctor
{
    static ThrowingCctor()
    {
        throw new ArgumentException("I have an argument.");
    }

}