// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

// Repro case for CoreCLR 7829

class X
{
    private class Buffer : SafeBuffer
    {
        public Buffer()
            : base (false)
        {
            
        }

        protected override bool ReleaseHandle()
        {
            return true;
        }
    }

    public static int Main()
    {
        var buffer = new Buffer();

        try
        {
            buffer.Initialize(uint.MaxValue - 1, uint.MaxValue - 1);
        }
        catch (ArgumentOutOfRangeException)
        {
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        return 101;
    }
}
