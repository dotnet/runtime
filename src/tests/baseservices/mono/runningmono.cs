// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;
using TestLibrary;

namespace TestRunningMono
{
    public class Program
    {
        [ActiveIssue("This test is to verify we are running mono, and therefore only makes sense on mono.", TestRuntimes.CoreCLR)]
        [Fact]
        public static int TestEntryPoint()
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

