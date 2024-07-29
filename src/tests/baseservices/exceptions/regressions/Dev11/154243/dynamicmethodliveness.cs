// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// regression test for Devdiv bug 154243
// the test passes if we get to Done.
// with the bug present test was getting assert dialog box with message that Consistency check failed: FAILED: state.fFound

using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

public class My {

    static Exception theException = new Exception();

    static void Thrower() {
        for (int j = 0; j <= 100; j++)
        {
            try {
                throw theException;
            }
            catch {
            }
        }
    }

    static void Dynamizer() {
        for (int j = 0; j <= 100; j++)
        {
             DynamicMethod method = EmitDynamicMethod(typeof(My).GetMethod("Noop"));
             ((Action)method.CreateDelegate(typeof(Action)))();    
        }
    }

    static DynamicMethod EmitDynamicMethod(MethodInfo callee)
    {
        DynamicMethod method = new DynamicMethod(
            "MyMethod", 
            typeof(void), 
            new Type[0], 
            typeof(My).GetTypeInfo().Module);

        ILGenerator il = method.GetILGenerator();
        for (int i = 0; i < 5; i++)
            il.Emit(OpCodes.Call, callee);
        il.Emit(OpCodes.Ret);

        return method;
    }

    public static void ThrowException() {
        throw theException;
    }

    public static void Noop() {
    }

    static void DoStuff() {
        DynamicMethod method = EmitDynamicMethod(typeof(My).GetMethod("ThrowException"));
        for (int i = 0; i < 20; i++)
             method = EmitDynamicMethod(method);
        ((Action)method.CreateDelegate(typeof(Action)))();
    }

    [Fact]
    public static void TestEntryPoint()
    {
        new Thread(Thrower).Start();

        new Thread(Dynamizer).Start();

        Thread.Sleep(100);
        Console.WriteLine("TestCase Started");
        for (int j=0;j<=100;j++) {             
            Console.WriteLine("Counter = " + j.ToString());
             try {
                 try {
                     
                     DoStuff();                                          
                 }
                 finally {
                     Console.WriteLine("Sleeping");
                     Thread.Sleep(100);
                     Console.WriteLine("Running GC");
                     GC.Collect();
                     Console.WriteLine("Waiting for finalizers...");
                     for (int i = 0; i < 10; i++) GC.WaitForPendingFinalizers();
                     Console.WriteLine("Running GC");
                     GC.Collect();
                 }           
             }
             catch (Exception) 
             {
             }
        }
        Console.WriteLine("Test case Pass");
    }
}
