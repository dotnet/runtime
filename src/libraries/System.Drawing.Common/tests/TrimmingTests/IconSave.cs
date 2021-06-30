// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.IO;

/// <summary>
/// Tests that Icon.Save works when the Icon is loaded from an IntPtr.
/// This causes COM to be used to save the Icon.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        Icon i = SystemIcons.WinLogo;
        using MemoryStream stream = new();

        i.Save(stream);

        // ensure something was written to the stream
        if (stream.Position == 0)
        {
            return -1;
        }

        return 100;
    }
}
