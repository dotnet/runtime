// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Simple OSR test case -- long running loop in Main

public class MainLoop
{
   [Fact]
   public static int TestEntryPoint()
   {
       long result = 0;
       for (int i = 0; i < 1_000_000; i++)
       {
           result += (long)i;
       }
       return result == 499999500000 ? 100 : -1;
   }  
}
