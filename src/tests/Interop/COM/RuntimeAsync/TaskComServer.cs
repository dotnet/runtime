// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

[ComVisible(true)]
[Guid("923dd7d3-a3c0-4fe2-9222-170f9d983724")]
public interface ITaskComServer
{
    Task GetTask();
    int GetValue();
}

[ComVisible(true)]
[Guid("9ad32e71-347f-47e7-8fe2-096975e65b00")]
public class TaskComServer : ITaskComServer
{
    public const int ExpectedValue = 12345;

    public Task GetTask()
    {
        // Create a definitely-not-generic Task.
        // Non-blittable generics can't be marshalled,
        // so we can't have Task<T>.
        Task t = new(() => {});
        t.Start();
        return t;
    }

    public int GetValue()
    {
        return ExpectedValue;
    }
}

[ComImport]
[Guid("923dd7d3-a3c0-4fe2-9222-170f9d983724")]
public interface ITaskComServer_Imported
{
    Task GetTask();
    int GetValue();
}

[ComImport]
[Guid("923dd7d3-a3c0-4fe2-9222-170f9d983724")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface ITaskComServer_AsDispatchOnly
{
    Task GetTask();
    int GetValue();
}
