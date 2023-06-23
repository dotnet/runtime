// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;

public class Hook
{
    public static Hook Basic = new Hook(nameof(Basic));

    public Hook(string name)
    {
        Name = name;
        AssemblyPath = Path.Combine(AppContext.BaseDirectory, "hooks", $"{name}.dll");
    }

    public string Name { get; }

    public string AssemblyPath { get; }

    public unsafe int CallCount
    {
        get
        {
            if (TryGetCallCountProperty(out PropertyInfo callCount))
            {
                delegate*<int> getCallCount = (delegate*<int>)callCount.GetMethod.MethodHandle.GetFunctionPointer();
                return getCallCount();
            }

            return 0;
        }
    }

    private bool TryGetCallCountProperty(out PropertyInfo callCount)
    {
        callCount = null;
        Assembly asm = null;
        foreach(Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (loaded.GetName().Name == Name && loaded.Location == AssemblyPath)
            {
                asm = loaded;
                break;
            }
        }

        if (asm == null)
            return false;

        Type hook = asm.GetType("StartupHook");
        if (hook == null)
            return false;

        callCount = hook.GetProperty(nameof(CallCount), BindingFlags.NonPublic | BindingFlags.Static);
        return callCount != null;
    }
}