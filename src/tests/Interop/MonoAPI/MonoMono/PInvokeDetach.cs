// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//
// pinvoke-detach-1.cs:
//
//   Test attaching and detaching a new thread from native.
//  If everything is working, this should not hang on shutdown.
using System;
using System.Threading;
using System.Runtime.InteropServices;

using Xunit;

namespace MonoAPI.Tests.MonoMono.PInvokeDetach;

public class MonoPInvokeCallbackAttribute : Attribute {
    public MonoPInvokeCallbackAttribute (Type delegateType) { }
}

public class PInvokeDetach {
    const string TestNamespace = "MonoAPI.Tests.MonoMono.PInvokeDetach";
    const string TestName = nameof (PInvokeDetach);

    [Fact]
    public static void TestEntryPoint()
    {
        MonoAPI.Tests.MonoAPISupport.Setup();
        int result;
        result = test_0_attach_invoke_foreign_thread ();
        Assert.Equal(0, result);
        result = test_0_attach_invoke_foreign_thread_delegate ();
        Assert.Equal(0, result);
        result = test_0_attach_invoke_block_foreign_thread ();
        Assert.Equal(0, result);
        result = test_0_attach_invoke_block_foreign_thread_delegate ();
        Assert.Equal(0, result);
    }

    public delegate void VoidVoidDelegate ();

    static int was_called;

    [MonoPInvokeCallback (typeof (VoidVoidDelegate))]
    private static void MethodInvokedFromNative ()
    {
        was_called++;
    }

    [DllImport (MonoAPISupport.TestLibName, EntryPoint="mono_test_attach_invoke_foreign_thread")]
    public static extern bool mono_test_attach_invoke_foreign_thread (string assm_name, string name_space, string class_name, string method_name, VoidVoidDelegate del);

    public static int test_0_attach_invoke_foreign_thread ()
    {
        was_called = 0;
        bool skipped = mono_test_attach_invoke_foreign_thread (typeof (PInvokeDetach).Assembly.Location, TestNamespace, TestName, nameof(MethodInvokedFromNative), null);
        GC.Collect (); // should not hang waiting for the foreign thread
        return skipped || was_called == 5 ? 0 : 1;
    }

    static int was_called_del;

    [MonoPInvokeCallback (typeof (VoidVoidDelegate))]
    private static void MethodInvokedFromNative_del ()
    {
        was_called_del++;
    }

    public static int test_0_attach_invoke_foreign_thread_delegate ()
    {
        var del = new VoidVoidDelegate (MethodInvokedFromNative_del);
        was_called_del = 0;
        bool skipped = mono_test_attach_invoke_foreign_thread (null, null, null, null, del);
        GC.Collect (); // should not hang waiting for the foreign thread
        return skipped || was_called_del == 5 ? 0 : 1;
    }

    [MonoPInvokeCallback (typeof (VoidVoidDelegate))]
    private static void MethodInvokedFromNative2 ()
    {
    }

    [DllImport (MonoAPISupport.TestLibName, EntryPoint="mono_test_attach_invoke_block_foreign_thread")]
    public static extern bool mono_test_attach_invoke_block_foreign_thread (string assm_name, string name_space, string class_name, string method_name, VoidVoidDelegate del);

    public static int test_0_attach_invoke_block_foreign_thread ()
    {
        bool skipped = mono_test_attach_invoke_block_foreign_thread (typeof (PInvokeDetach).Assembly.Location, TestNamespace, TestName, nameof(MethodInvokedFromNative2), null);
        GC.Collect (); // should not hang waiting for the foreign thread
        return 0; // really we succeed if the app can shut down without hanging
    }

    // This one fails because we haven't fully detached, so shutdown is waiting for the thread
    public static int test_0_attach_invoke_block_foreign_thread_delegate ()
    {
        var del = new VoidVoidDelegate (MethodInvokedFromNative2);
        bool skipped = mono_test_attach_invoke_block_foreign_thread (null, null, null, null, del);
        GC.Collect (); // should not hang waiting for the foreign thread
        return 0; // really we succeed if the app can shut down without hanging
    }

}
