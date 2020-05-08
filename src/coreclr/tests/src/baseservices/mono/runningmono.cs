// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace TestRunningMono
{
    class Program
    {
        public static int Main(string[] args)
        {
             const int Pass = 100, Fail = 1;
             bool isMono = typeof(object).Assembly.GetType("Mono.RuntimeStructs") != null;

             if(isMono)
             {
                 return Pass;
             }
             else
             {
                 return Fail;
             }
        }
   }
}

