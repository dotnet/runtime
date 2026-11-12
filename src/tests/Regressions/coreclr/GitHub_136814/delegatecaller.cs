// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// Test-only signature types, so no other test in a merged runner can load them first and hide the bug.
public enum CallbackValue { Default }

public enum KindCallbackValue { Default }

public struct ReferenceCallbackValue
{
    public object Value;
}

public delegate int Callback(CallbackValue? value);

public delegate int KindCallback(KindCallbackValue? value);

public delegate object ReferenceCallback(ReferenceCallbackValue value);

public static class DelegateCaller
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Invoke(Callback callback)
    {
        return callback(null);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Invoke(KindCallback callback)
    {
        return callback(null);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Invoke(ReferenceCallback callback)
    {
        object instance = new object();
        WeakReference reference = new WeakReference(instance);
        object result = callback(new ReferenceCallbackValue { Value = instance });
        return result is not null && object.ReferenceEquals(result, reference.Target) ? 100 : 101;
    }
}
