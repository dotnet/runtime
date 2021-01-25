// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// OSR entry in a try region

class MainLoopTry
{
   public static int Main()
   {
       Console.WriteLine($"starting sum");
       int result = 0;
       try 
       {
           for (int i = 0; i < 1_000_000; i++)
           {
               result += i;
           }
       }
       finally
       {
           Console.WriteLine($"done, sum is {result}");
       }
       return result == 1783293664 ? 100 : -1;
   }  
}
