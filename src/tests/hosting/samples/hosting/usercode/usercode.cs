// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;

public class EventSink
{
    static public void Click(int x, int y)
    {
        Console.WriteLine("[User Event Handler] Event called with " + x + ":" + y);
    }
}
