// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

// This test case reproduces a race condition involving type initialization (aka, .cctor, aka static constructor).
//
// The idea is that Thread1 initiates a type load and type initialization on MyClass (running of the static constructor/.cctor).
//
// While Thread1 is doing this, Thread2 attempts to access a static member, MyClass.X 
// by invoking static method MyClass.getX().
//
// The failing behavior is that Thread2 is able to access MyClass.X before it is initialized--Thread1 is still busy 
// with type initialization, but hasn't yet initialized MyClass.X.  This is because the prestub for MyClass.getX(), the mechanism that
// would normally trigger the .cctor to be run, was already run by Thread1.  By the time Thread2 hit getX(), there is
// no more prestub to trigger the .cctor--so Thread2 (effectively) assumes that MyClass is already initialized and
// proceeds to access the still uninitialized static member, MyClass.X.
//
// A likely fix for this would be to delay backpatching getX() until after the .cctor has fully completed.
// mwilk. 2/3/04.


public class MyClass{
    private static int X;
    public static int X0;
    static MyClass(){
        X0 = getX();  // expect this to return 0, since this forces a cctor loop.        
        Thread.Sleep(1000*5); // 5 seconds
        X=12;        
    }
    [MethodImpl(MethodImplOptions.NoInlining)] 
    public static int getX(){
        Console.WriteLine("In MyClass.getX(): thread {0}",Thread.CurrentThread.Name);
        return X;
    }
    // invoking this should trigger the cctor
    [MethodImpl(MethodImplOptions.NoInlining)] 
    public static void SomeMethod(){
        Console.WriteLine("In MyClass.SomeMethod(): thread {0}",Thread.CurrentThread.Name);    
    }
}

public class CMain{    
    public static int X_getX;
    
    public static void RunSomeMethod(){
        MyClass.SomeMethod();
    }
    public static void RunGetX(){
        
        X_getX = MyClass.getX(); 
        Console.WriteLine("X_getX: {0}: thread {1}",X_getX,Thread.CurrentThread.Name); 
    } 
    [Fact]
    public static int TestEntryPoint(){
        Thread t1 = new Thread(RunSomeMethod);
        t1.Name = "T1";
        Thread t2 = new Thread(RunGetX);
        t2.Name = "T2";
        
        t1.Start();
        Thread.Sleep(1000*1); // 1 second
        t2.Start();
        
        t2.Join();
        t1.Join();
        
        
        //Console.WriteLine("MyClass.X0: {0}",MyClass.X0);
        if(12==X_getX){
            Console.WriteLine("PASS");
            return 100;
        }
        else{
            Console.WriteLine("FAIL");
            return 101;
        }
    }
}
