// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Simple OSR test case -- nested loop in Main

class MainNestedLoop
{
   public static int Main()
   {
       long result = 0;
       for (int i = 0; i < 1_000; i++)
       {
           for (int j = 0; j < 1_000; j++)
           {
               result += (long)(i * 1_000 + j);
           }
       }

       return result == 499999500000 ? 100 : -1;
   }  
}
