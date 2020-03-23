// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

// Multiple patchpoints each in a try

class MainLoopTry2
{
   public static int Main()
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
