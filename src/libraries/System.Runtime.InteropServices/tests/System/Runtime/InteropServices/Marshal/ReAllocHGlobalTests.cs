// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ReAllocHGlobalTests
    {
        [Fact]
        public void ReAllocHGlobal_Invoke_DataCopied()
        {
            const int Size = 3;
            IntPtr p1 = Marshal.AllocHGlobal((IntPtr)Size);
            IntPtr p2 = p1;
            try
            {
                for (int i = 0; i < Size; i++)
                {
                    Marshal.WriteByte(p1 + i, (byte)i);
                }

                int add = 1;
                do
                {
                    p2 = Marshal.ReAllocHGlobal(p2, (IntPtr)(Size + add));
                    for (int i = 0; i < Size; i++)
                    {
                        Assert.Equal((byte)i, Marshal.ReadByte(p2 + i));
                    }

                    add++;
                }
                while (p2 == p1); // stop once we've validated moved case
            }
            finally
            {
                Marshal.FreeHGlobal(p2);
            }
        }
    }
}
