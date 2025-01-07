// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

/*
public interface IFinalize
{
    ~IFinalize();
}

public class Finalize : IFinalize
{
    ~Finalize(){ Console.WriteLine("IFinalize");}
}
*/
//Test cases:
//  Finalizers can not have a protection level (i.e public, protected, internal, protected internal, private)
//  Types in namespace can only be public or internal
// Negative:
//  Must do checks with reflection as types are scanned at assembly load time resulting in AssemblyLoad failures
//  Public class with finalizer
//  internal class with finalizer
//  Class with finalizer in Abstract base classes
//  Wrapper classes with child classes with finalizers
//      protected, protected internal, private
// Positive:
//  Platform types with finalizers
//  Non platform type that derives from abstract platform with finalizer
// Variations:
//  Reflection load
//  Startup class
//  Attributes?

public class Finalizer
{
    public Finalizer() { }
    ~Finalizer() { Console.WriteLine("In Finalizer"); }
}

public class FinalizerWrapperProtected
{
    public FinalizerWrapperProtected()
    {
        FinalizerProtected fp = new FinalizerProtected();
    }

    protected class FinalizerProtected
    {
        public FinalizerProtected() { }
        ~FinalizerProtected() { Console.WriteLine("In FinalizerProtected"); }
    }
}

internal class FinalizerInternal
{
    public FinalizerInternal() { }
    ~FinalizerInternal() { Console.WriteLine("In FinalizerInternal"); }
}

public class FinalizerWrapperProtectedInternal
{
    public FinalizerWrapperProtectedInternal()
    {
        FinalizerProtectedInternal fp = new FinalizerProtectedInternal();
    }

    protected internal class FinalizerProtectedInternal
    {
        public FinalizerProtectedInternal() { }
        ~FinalizerProtectedInternal() { Console.WriteLine("In FinalizerProtectedInternal"); }
    }
}
public class FinalizerWrapperPrivate
{
    public FinalizerWrapperPrivate()
    {
        FinalizerPrivate fp = new FinalizerPrivate();
    }
    private class FinalizerPrivate
    {
        public FinalizerPrivate() { }
        ~FinalizerPrivate() { Console.WriteLine("In FinalizerProtectedInternal"); }
    }
}

public abstract class FinalizerBase
{
    public FinalizerBase() { }
    ~FinalizerBase() { Console.WriteLine("In FinalizerBase"); }
}

public class FinalizerAbstract : FinalizerBase
{
    public FinalizerAbstract() { }
}

public class FinalizerGeneric<T>
{
    public FinalizerGeneric(){}
    ~FinalizerGeneric(){}
}

public class FinalizerWrapperGeneric
{
    public FinalizerWrapperGeneric()
    {
        FinalizerPrivate<int> fp = new FinalizerPrivate<int>();
    }

    private class FinalizerPrivate<T>
    {
        public FinalizerPrivate() { }
        ~FinalizerPrivate() { }
    }
}

public class Gen<T>
{
    public Gen() { }
}

public class Test
{
    [Fact]
    public static int TestEntryPoint()
    {
        return (new Test()).RunTest();
    }

    private int RunTest()
    {
        try
        {
            RunFinalizer();
            RunFinalizerWrapperProtected();
            RunFinalizerInternal();
            RunFinalizerWrapperProtectedInternal();
            RunFinalizerWrapperPrivate();
            RunFinalizerAbstract();
            RunFinalizerGeneric();
            RunFinalizerWrapperGeneric();
            RunGeneric();

        }
        catch (Exception e)
        {
            Console.WriteLine("ERROR - Caught unexpected exception");
            Console.WriteLine(e);
            return 1;
        }
        Console.WriteLine("Test PASSED");
        return 100;
    }

    private void RunFinalizer()
    {
        Finalizer f = new Finalizer();
    }
    private void RunFinalizerWrapperProtected()
    {
        FinalizerWrapperProtected f = new FinalizerWrapperProtected();
    }
    private void RunFinalizerInternal()
    {
        FinalizerInternal f = new FinalizerInternal();
    }
    private void RunFinalizerWrapperProtectedInternal()
    {
        FinalizerWrapperProtectedInternal f = new FinalizerWrapperProtectedInternal();
    }
    private void RunFinalizerWrapperPrivate()
    {
        FinalizerWrapperPrivate f = new FinalizerWrapperPrivate();
    }
    private void RunFinalizerAbstract()
    {
        FinalizerAbstract f = new FinalizerAbstract();
    }
    private void RunFinalizerGeneric()
    {
        FinalizerGeneric<int> f = new FinalizerGeneric<int>();
    }
    private void RunFinalizerWrapperGeneric()
    {
        FinalizerWrapperGeneric f = new FinalizerWrapperGeneric();
    }
    private void RunGeneric()
    {
        Gen<Finalizer> gen = new Gen<Finalizer>();        
    }
}
