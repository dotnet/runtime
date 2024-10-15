// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;

internal static class Program
{
     public static int Foo(string[] args)
     {
         int sum = 5;
         for (int i = 0; args != null && i < args.Length; i++)
         {
             sum += int.Parse(args[i]);
         }
         return sum;
    }
}
