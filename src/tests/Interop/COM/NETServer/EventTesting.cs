// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using Server.Contract;

[ComVisible(true)]
[Guid(Server.Contract.Guids.EventTesting)]
[ComSourceInterfaces(typeof(Server.Contract.TestingEvents))]
public class EventTesting : Server.Contract.IEventTesting
{
    public delegate void OnEventHandler(string msg);
    public event OnEventHandler OnEvent;

    public void FireEvent()
    {
        var h = this.OnEvent;
        if (h != null)
        {
            h(nameof(FireEvent));
        }
    }
}
