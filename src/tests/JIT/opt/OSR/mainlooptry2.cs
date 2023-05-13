// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Multiple patchpoints each in a try

public class MainLoopTry2
{
   [Fact]
   public static int TestEntryPoint()
   {
       Console.WriteLine($"starting sum");
       int result = 0;
       try 
       {
           for (int i = 0; i < 1_000; i++)
           {
               int temp = result;
               try 
               {
                   for (int j = 0; j < 1_000; j++)
                   {
                       temp += 1000 * i + j;
                   }
               }
               finally
               {
                   result = temp;
               }
           }
       }
       finally
       {
           Console.WriteLine($"done, sum is {result}");
       }
       return result == 1783293664 ? 100 : -1;
   }  
}
