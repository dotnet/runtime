// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

Console.WriteLine(Program.x[0]);

partial class Program
{
    public static readonly byte[] x = "utf8 literal"u8.ToArray();

    
}
