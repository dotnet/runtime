// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tricky case for OSR with patchpoint in try region.
//
// If we need to OSR at inner loop head, then both try
// regions need trimming.
//
// Mutually protective try example.

using System;
using Xunit;

class E : Exception {}

public class MainLoopMutuallyProtectiveTry
{
   [Fact]
   public static int TestEntryPoint()
   {
       Console.WriteLine($"starting sum");
       int result = 0;
       try 
       {
           int temp = 0;
           for (int i = 0; i < 1_000; i++)
           {
               for (int j = 0; j < 1_000; j++)
               {
                   temp += 1000 * i + j;
               }
           }
           result = temp;
       }
       catch (E)
       {
       }
       catch (Exception)
       {
       }
       Console.WriteLine($"done, sum is {result}");
       return result == 1783293664 ? 100 : -1;
   }  
}
