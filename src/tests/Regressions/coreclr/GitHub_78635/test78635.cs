// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public abstract class BaseProp
{
}

public class DerivedProp : BaseProp
{
}

public interface ICov
{
    BaseProp GetMyProp ();
}

// Abstract
public abstract class BaseAbstract
{
    public abstract BaseProp GetMyProp ();
}

// TEST1
public class DerivedAbstract1 : BaseAbstract, ICov
{
    public override DerivedProp GetMyProp () { Console.WriteLine ("DERIVED"); return new DerivedProp (); }
}

// TEST2
public class DerivedAbstract21 : BaseAbstract
{
    public override DerivedProp GetMyProp () { Console.WriteLine ("DERIVED21"); return new DerivedProp (); }
}

public class DerivedAbstract22 : DerivedAbstract21, ICov
{
    public override DerivedProp GetMyProp () { Console.WriteLine ("DERIVED22"); return new DerivedProp (); }
}

// TEST3
public abstract class DerivedAbstract31 : BaseAbstract, ICov
{
}

public class DerivedAbstract32 : DerivedAbstract31
{
    public override DerivedProp GetMyProp () { Console.WriteLine ("DERIVED32"); return new DerivedProp (); }
}

// Virtual
public abstract class BaseVirtual
{
    public virtual BaseProp GetMyProp () { Console.WriteLine ("BASE"); return null; }
}

// TEST1
public class DerivedVirtual1 : BaseVirtual, ICov
{
    public override DerivedProp GetMyProp () { Console.WriteLine ("DERIVED"); return new DerivedProp (); }
}

// TEST2
public class DerivedVirtual21 : BaseVirtual
{
    public override DerivedProp GetMyProp () { Console.WriteLine ("DERIVED21"); return new DerivedProp (); }
}

public class DerivedVirtual22 : DerivedVirtual21, ICov
{
    public override DerivedProp GetMyProp () { Console.WriteLine ("DERIVED22"); return new DerivedProp (); }
}

// TEST3
public abstract class DerivedVirtual31 : BaseVirtual, ICov
{
}

public class DerivedVirtual32 : DerivedVirtual31
{
    public override DerivedProp GetMyProp () { Console.WriteLine ("DERIVED32"); return new DerivedProp (); }
}


public class Program {

    public static void TestInstance (ICov derived)
    {
        BaseProp prop = derived.GetMyProp ();
        if (prop == null)
            throw new Exception ("Invalid return");
    }

    public static void Test ()
    {
        TestInstance (new DerivedAbstract1 ());
        TestInstance (new DerivedAbstract22 ());
        TestInstance (new DerivedAbstract32 ());
        TestInstance (new DerivedVirtual1 ());
        TestInstance (new DerivedVirtual22 ());
        TestInstance (new DerivedVirtual32 ());
    }

    public static int Main (string[] args)
    {
        try {
            Test ();
        } catch (Exception e) {
            Console.WriteLine ("NOK");
            Console.WriteLine (e);
            return 1;
        }
        Console.WriteLine ("OK");
        return 100;
    }
}

