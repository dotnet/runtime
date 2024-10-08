// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.Generic;

#pragma warning disable CS8632

namespace Sample;

public partial class TestClass
{
    private static readonly ParentClass _parent = new();
    private static readonly HashSet<ChildClass> _objects = [];

    public static int Main(string[] args)
    {
        //GC.AddMemoryPressure(1024*1024*512);
        var tm = GetStats();
        Console.WriteLine($"TotalMemory: {tm}");
        return 0;
    }

    public static long GetStats()
    {
        var tm = GC.GetTotalMemory(forceFullCollection: false);
        // Console.WriteLine($"TotalMemory: {tm}");

        //var mi = GC.GetGCMemoryInfo();
        //Console.WriteLine($"HighMemoryLoadThresholdBytes: {mi.HighMemoryLoadThresholdBytes}");
        //Console.WriteLine($"TotalAvailableMemoryBytes: {mi.TotalAvailableMemoryBytes}");
        return tm;
    }

    [JSExport]
    [return: JSMarshalAs<JSType.Number>]
    public static long AllocateObjects()
    {
        for (int i = 0; i < 10; i++)
        {
            var child = new ChildClass(_parent);
            _objects.Add(child);
        }

        return GetStats();
    }

    [JSExport]
    public static void DisposeObjects()
    {
        foreach (var child in _objects)
        {
            child.Dispose();
        }
        _objects.Clear();
    }

    [JSExport]
    [return: JSMarshalAs<JSType.Number>]
    public static long ForceGC()
    {
        GC.Collect();
        return GetStats();
    }
}

public sealed class ChildClass : IDisposable
{
    private readonly ParentClass _parent;
    private byte[] _junk = new byte[250_000];

    public ChildClass(ParentClass parent)
    {
        _parent = parent;
        _parent.PropertyChanged += OnPropertyChanged;
    }

    public void Dispose()
    {
        _parent.PropertyChanged -= OnPropertyChanged;
        _junk=null;
        GC.SuppressFinalize(this);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
    }
}

public sealed class ParentClass
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyChilderen()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Foo"));
    }
}
