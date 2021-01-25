// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class Writer
{
    public object Data { get; set; }
    public int Position { get; set; }

   [MethodImpl(MethodImplOptions.NoInlining)]
   Writer()
   {
       Data = new int[] { 100, -1, -2, -3 };
       Position = 4;
   }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ArraySegment<byte> Test()
    {
        var writer = new Writer();
        object temp = writer.Data;
        byte[] data = Unsafe.As<object, byte[]>(ref temp);
        return new ArraySegment<byte>(data, 0, writer.Position);
    }
    
    public static int Main()
    {
        var x = Test();
        return x[0];
    }
}
