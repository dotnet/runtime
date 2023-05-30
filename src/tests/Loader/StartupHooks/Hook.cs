// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;

public class Hook
{
    public static Hook Basic = new Hook(nameof(Basic), usePathAsValue: false);
    public static Hook PrivateInitialize = new Hook(nameof(PrivateInitialize), Path.Combine(AppContext.BaseDirectory, "private"));

    public static Hook InstanceMethod = new Hook(nameof(InstanceMethod));
    public static Hook MultipleIncorrectSignatures = new Hook(nameof(MultipleIncorrectSignatures));
    public static Hook NoInitializeMethod = new Hook(nameof(NoInitializeMethod));
    public static Hook NonVoidReturn = new Hook(nameof(NonVoidReturn));
    public static Hook NotParameterless = new Hook(nameof(NotParameterless));

    private string directory;

    public Hook(string name, bool usePathAsValue = true)
        : this(name, AppContext.BaseDirectory, usePathAsValue)
    { }

    public Hook(string name, string directory, bool usePathAsValue = true)
    {
        Name = name;
        AssemblyPath = Path.Combine(directory, $"{name}.dll");
        Value = usePathAsValue ? AssemblyPath : Name;
    }

    public string Name { get; }
    public string Value { get; }

    private string AssemblyPath { get; }

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
        set
        {
            if (TryGetCallCountProperty(out PropertyInfo callCount))
            {
                delegate*<int, void> setCallCount = (delegate*<int, void>)callCount.SetMethod.MethodHandle.GetFunctionPointer();
                setCallCount(value);
            }
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