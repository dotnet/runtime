// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Server.Contract;

[ComVisible(true)]
[Guid("C2CF7A31-FA94-48E2-AB89-64E6B4378EF5")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEventRemovalTesting
{
    void ConfigureRemoval(bool throwOnce, TestingEvents callback);
    int GetRemoveCount();
}

[ComVisible(true)]
[Guid(Server.Contract.Guids.EventTesting)]
[ComSourceInterfaces(typeof(Server.Contract.TestingEvents))]
public class EventTesting : Server.Contract.IEventTesting, IEventRemovalTesting
{
    private event OnEventHandler _onEvent;
    private TestingEvents _removeCallback;
    private int _throwOnRemove;
    private int _removeCount;

    public delegate void OnEventHandler(string msg);
    public event OnEventHandler OnEvent
    {
        add => _onEvent += value;
        remove
        {
            Interlocked.Increment(ref _removeCount);
            if (Interlocked.Exchange(ref _throwOnRemove, 0) != 0)
                throw new COMException("The first removal failed.", unchecked((int)0x80004005));

            _removeCallback?.OnEvent("remove");
            _onEvent -= value;
        }
    }

    public void ConfigureRemoval(bool throwOnce, TestingEvents callback)
    {
        _throwOnRemove = throwOnce ? 1 : 0;
        _removeCallback = callback;
    }

    public int GetRemoveCount() => Volatile.Read(ref _removeCount);

    public void FireEvent()
    {
        var h = _onEvent;
        if (h != null)
        {
            h(nameof(FireEvent));
        }
    }
}
