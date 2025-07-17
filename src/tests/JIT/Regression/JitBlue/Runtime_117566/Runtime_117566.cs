// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Failure calling a synchronized method in an assembly loaded in a collectible AssemblyLoadContext:
//
// Unhandled exception. System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
//  ---> System.NullReferenceException: Object reference not set to an instance of an object.
//    at System.RuntimeTypeHandle.GetRuntimeTypeFromHandle(IntPtr handle)
//    at Runtime_117566.Synchronized()
//    at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(System.Object, IntPtr*)
//    at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(System.Object, System.Reflection.BindingFlags)
//    at System.Reflection.RuntimeMethodInfo.Invoke(System.Object, System.Reflection.BindingFlags, System.Reflection.Binder, System.Object[], System.Globalization.CultureInfo)
//    at System.Reflection.MethodBase.Invoke(System.Object, System.Object[])
//    at Runtime_117566.TestEntryPoint()

using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_117566
{
    [Fact]
    public static void TestEntryPoint()
    {
        var context = new AssemblyLoadContext("CollectibleALC", isCollectible: true);
        Assembly assembly = context.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);

        var method = assembly.GetType(nameof(Runtime_117566)).GetMethod(nameof(Synchronized), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        method?.Invoke(null, []);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void Synchronized()
    { }
}
