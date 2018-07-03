// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;

internal static class Program
{
    public static int Main(string[] args)
    {
        int sum = 5;
        for(int i = 0; args != null && i < args.Length; i++)
        {
            sum += int.Parse(args[i]);
        }
        return sum;
    }
}
